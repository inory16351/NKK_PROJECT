"""초필살기 레이어 컷신 — 쥐마다 따로 짠 연출 (사용자 2026-10-18: 셀 애니처럼 배경 + 움직이는 물체 합성 · 그림체만 통일, 카메라 무빙 · 액션 · 이펙트는 쥐마다 개별).
gen_superult_scene.py 가 불러서 C# 줄을 만듦. 좌표: 파노라마는 비율 (x 0~1 왼→오, y 0~1 위→아래), 화면은 px (가운데 0, 위 +, 1920×1080).
공통 시간: ① 0~0.4 (매니저) · ② 0.4~1.9 · ③ 1.9~3.9 (다단 HITS 시각) · ④ 3.9~4.6 · ⑤ ~5.0
도구: Story.cam (가상 카메라 = Rig 줌·기울기·충격 > Track 위치) · Story.w (파노라마 위) · Story.s (화면 위) · Story.band (마스크 띠)
      패턴: rain · rise · sweep · shower · spiral · bounce · burst_ring · stomp · converge · pops · trail"""
import json, math, os, random
from PIL import Image

HITS = [2.0 + 0.2 * i for i in range(10)]
NO_FINISH = {'icequeen': 3.4, 'knight': 3.95}          # ④ 피니시 컷 없는 쥐: ③ 끝(마지막 일격 · 또는 컷신 끝) 시각 = SuperUltStyle.actEnd (2026-10-19 사용자)
WORLD = {'icequeen': dict(cutFade=0.4, worldTime=3.0, freezeAll=1.5, blizzardCount=1, hailCount=3, playRate=0.75),
         'knight': dict(cutFade=0.4, worldTime=1.0, blizzardCount=0, hailCount=0, playRate=0.75, worldWind=(60.0, 260.0), worldDotSize=(16.0, 40.0))}
NO_TEXT = {'knight'}       # 컷신에 글(제목 띠 · 자막 · 히트 수 · 밈 글)을 안 띄우는 쥐 (2026-10-19 사용자)
KNIGHT_LINE = -40          # 기사 칼 각도 (도, UI 반시계 +): 후경 기사 칼날을 잰 값 — 컷신 갈라짐 · 땅 균열 · 게임 화면 파동 전부 이 각도 (2026-10-19 사용자)
IMAGE_FX = {'icequeen': dict(hailFall=['fx_hail_fall', 'fx_hail_fall', 'fx_hail_ball'], hailChunk=['fx_hail_chunk', 'fx_hail_ball'],
                             hailSplash=['fx_hail_splash1', 'fx_hail_splash2', 'fx_hail_splash3']),
            'knight': dict(hailFall=['fx_dots'], hailChunk=['fx_dots'], hailSplash=['fx_boom1', 'fx_boom2', 'fx_boom3'],      # 별 모양 없음 (둥근 빛 알갱이 · 폭발) splashHitSize=240.0, splashFinishSize=400.0,
                           lineSprite='fx_cutline', lineAngle=float(KNIGHT_LINE), lineLength=3000.0, lineThickness=110.0, boomFrames=['fx_boom1', 'fx_boom2', 'fx_boom3'], boomSize=400.0, lineWave='fx_slashwave', lineRipple='fx_ripple', lineOnce=True, boomFinishCount=5)}     # 기사 v3: 화면을 가르는 베기선 → 선을 따라 폭발     # 게임 화면 단계 이펙트 = Codex 그림 (Sheets/v2/hail.png, 벡터 점 대신)     # 컷신 뒤 게임 화면 단계 (다단 · 일격을 게임 화면에서, SuperUltStyle.worldPhase)
EASE = {'Out': 0, 'Linear': 1, 'In': 2, 'InOut': 3, 'Back': 4, 'Snap': 5}


def f(x): return f'{x:.2f}f'


def keys(ks):
    return 'new[] { ' + ', '.join(f'K({f(t)}, {f(x)}, {f(y)}, {f(s)}, {f(r)}, {f(a)}, {EASE[e]})' for t, x, y, s, r, a, e in ks) + ' }'


class Story:
    def __init__(self, code, art, root_dir):
        self.code, self.art = code, art
        self.o = []
        self.n = 0
        sj = os.path.join(root_dir, 'UnityResources', 'Rats', 'SuperUlt', code, 'Sheets', 'story.json')
        self.story = json.load(open(sj, encoding='utf-8'))
        self.horiz = self.story.get('orient') == 'h'
        self.has_alt = os.path.exists(os.path.join(art, code, 'pano_alt.png'))
        self.PW, self.PH = self.size('pano_on')
        self.PS = (1500 / self.PH) if self.horiz else (2500 / self.PW)     # 가로 = 높이 1500px · 세로 = 폭 2500px
        self.rnd = random.Random(code)

    # ── 기본 ──
    def size(self, name):
        p = os.path.join(self.art, self.code, name + '.png')
        return Image.open(p).size if os.path.exists(p) else (500, 500)

    def __call__(self, s): self.o.append(s)

    def v(self, prefix='v'):
        self.n += 1; return f'{prefix}{self.n}_{self.code}'

    def P(self, x, y):
        """파노라마 비율 → 파노라마 묶음 안 px"""
        return (x - 0.5) * self.PW * self.PS, (0.5 - y) * self.PH * self.PS

    def hp(self, name, px):
        """그림 높이를 px 로 맞추는 배율"""
        return px / max(1, self.size(name)[1])

    def extra(self, var, fx):
        s = []
        for k, val in fx.items():
            if k == 'kicks': s.append(f'{var}.kicks = new[] {{ {", ".join(f(x) for x in val)} }};')
            else: s.append(f'{var}.{k} = {f(val)};' if isinstance(val, float) else f'{var}.{k} = {val};')
        return ' '.join(s)

    def layer(self, parent, name, spr, scale, ks, **fx):
        var = self.v('l')
        self(f'var {var} = L("{name}", {parent}, "{spr}", {f(scale)}, {keys(ks)}); {self.extra(var, fx)}')
        return var

    def group(self, name, parent, t0, t1=None):
        var = self.v('g')
        ks = [(0, 0, 0, 1, 0, 0, 'Out'), (t0, 0, 0, 1, 0, 1, 'Snap')] + ([(t1, 0, 0, 1, 0, 0, 'Snap')] if t1 else [])
        self(f'var {var} = G("{name}", {parent}, {keys(ks)}).transform;')
        return var

    # ── 카메라: [(t, x비율, y비율, 줌, 기울기, 이징)] → Track (파노라마 레이어를 여기에) ──
    def cam(self, parent, path, kicks=(), power=36, rot=2.5, ktime=0.28, pano=None, swap=None):
        rig, tr = self.v('rig'), self.v('tr')
        rk, tk = [], []
        for t, x, y, z, r, e in path:
            px, py = self.P(x, y)
            rk.append((t, 0, 0, z, r, 1, e)); tk.append((t, -px, -py, 1, 0, 1, e))
        self(f'var {rig} = E("Rig", {parent}, {keys(rk)}); {rig}.kicks = new float[] {{ {", ".join(f(k) for k in kicks)} }}; {rig}.kickPower = {power}; {rig}.kickRot = {f(rot)}; {rig}.kickTime = {f(ktime)};')
        self(f'var {tr} = E("Track", {rig}.transform, {keys(tk)}).transform;')
        pano = pano or 'pano_on'
        if swap and self.has_alt:          # (t0, t1) 동안 켜짐 → 바뀐 판 깜빡이며 전환
            t0, t1 = swap
            self.layer(tr, 'PanoAlt', 'pano_alt', self.PS, [(0, 0, 0, 1, 0, 1, 'Out')])
            self.layer(tr, 'Pano', 'pano_on', self.PS, [(0, 0, 0, 1, 0, 1, 'Out'), (t0, 0, 0, 1, 0, 1, 'Linear'), (t0 + 0.05, 0, 0, 1, 0, 0.3, 'Linear'),
                                                     (t0 + 0.1, 0, 0, 1, 0, 0.8, 'Linear'), (t1, 0, 0, 1, 0, 0, 'Linear')])
        else:
            self.layer(tr, 'Pano', pano if (pano != 'pano_alt' or self.has_alt) else 'pano_on', self.PS, [(0, 0, 0, 1, 0, 1, 'Out')])
        return tr

    def w(self, tr, name, spr, scale, ks, **fx):
        """파노라마 위 레이어: ks = [(t, x비율, y비율, sc, rot, a, e)]"""
        return self.layer(tr, name, spr, scale, [(t, *self.P(x, y), s, r, a, e) for t, x, y, s, r, a, e in ks], **fx)

    def s(self, parent, name, spr, scale, ks, **fx):
        """화면 레이어: ks = [(t, x, y, sc, rot, a, e)] px"""
        return self.layer(parent, name, spr, scale, ks, **fx)

    def band(self, parent, name, size, ks, kicks=HITS, power=14):
        var = self.v('band')
        self(f'var {var} = Go("{name}", {parent}); ((RectTransform){var}.transform).sizeDelta = new Vector2({f(size[0])}, {f(size[1])});')
        self(f'{{ var bim = {var}.AddComponent<UnityEngine.UI.Image>(); bim.raycastTarget = false; {var}.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;')
        self(f'  var bl = {var}.AddComponent<NKK.Ults.SuperUltLayer>(); bl.keys = {keys(ks)}; bl.kicks = new[] {{ {", ".join(f(k) for k in kicks)} }}; bl.kickPower = {power}; bl.kickRot = 1; bl.kickTime = 0.15f; }}')
        return f'{var}.transform'

    def bandpano(self, band, ks, pano=None):
        """띠 안 배경 = 파노라마 (화면 px 키, 파노라마 크기 그대로)"""
        pano = pano or ('pano_alt' if self.has_alt else 'pano_on')
        if pano == 'pano_alt' and not self.has_alt: pano = 'pano_on'
        return self.s(band, 'Sky', pano, self.PS, ks)

    def text(self, parent, name, txt, size, pos, ks, w=1000, back=True, counter=None):
        var = self.v('t')
        self(f'{{ var m = G("{name}", {parent}, null); var mrt = (RectTransform)m.transform; mrt.anchorMin = new Vector2(0.5f, 0.5f); mrt.anchorMax = new Vector2(0.5f, 0.5f); mrt.sizeDelta = new Vector2({w}, 90);')
        if back:
            self(f'  var bk = Go("Back", m.transform).AddComponent<UnityEngine.UI.Image>(); bk.raycastTarget = false; bk.color = new Color(0.08f, 0.06f, 0.12f, 0.85f); bk.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI_Kit/ui_bar_white.png"); bk.type = UnityEngine.UI.Image.Type.Sliced; ((RectTransform)bk.transform).sizeDelta = new Vector2({w - 20}, {int(size * 1.7)});')
        self(f'  var mt = T("Text", m.transform, "{txt}", {size}, new Color(1f, 0.95f, 0.6f, 1f)); ((RectTransform)mt.transform).sizeDelta = new Vector2({w}, {int(size * 1.7)});')
        if counter: self(f'  m.counter = mt; m.count = new Vector2({f(counter[0])}, {f(counter[1])}); m.countTime = new Vector2({f(counter[2])}, {f(counter[3])});')
        self(f'  m.keys = {keys(ks)}; }}')

    def meme(self, parent, t0, side=1, y=465, prop_dy=-240):
        """밈 소품이 날아가고 밈 글 (④, 피니시 컷 없는 쥐는 ③ 끝 무렵 화면 아래)"""
        self.s(parent, 'Meme', 'prop_meme', 0.55, [(t0, 1300 * side, y + prop_dy, 0.8, 25 * side, 1, 'Out'), (t0 + 0.75, -1300 * side, y + prop_dy + 80, 1.1, -40 * side, 1, 'Linear')], jitter=3.0)
        self.text(parent, 'MemeText', self.story['meme'], 48, (0, y), [(t0 + 0.05, 0, y, 0, -2, 0, 'Out'), (t0 + 0.2, 0, y, 1, -2, 1, 'Back')])

    def face(self, parent, mode, t0=1.68, t1=1.9, bg=None):
        """얼굴 클로즈업: full(쾅) · slide(오른쪽에서 밀고 들어옴) · zoom(작게서 확 커짐) · tilt(기울어 덮침)"""
        if mode == 'full':
            ks = [(0, 0, 0, 1, 0, 0, 'Out'), (t0, 0, 0, 1.35, -4, 1, 'Snap'), (t1, 40, 10, 1.0, -2, 1, 'Out')]
        elif mode == 'slide':
            ks = [(0, 2200, 0, 1, 0, 0, 'Out'), (t0, 2200, 0, 1.0, 6, 1, 'Snap'), (t0 + 0.12, 300, 0, 1.0, 6, 1, 'Back'), (t1, 260, 0, 1.03, 6, 1, 'Linear')]
        elif mode == 'zoom':
            ks = [(0, 0, 0, 0.05, 0, 0, 'Out'), (t0, 0, 0, 0.05, 20, 1, 'Snap'), (t0 + 0.14, 0, 0, 1.15, -3, 1, 'Back'), (t1, 0, 0, 1.0, 0, 1, 'Linear')]
        else:
            ks = [(0, 0, 0, 1, 0, 0, 'Out'), (t0, -900, 600, 1.2, -25, 1, 'Snap'), (t0 + 0.12, 0, 0, 1.1, -6, 1, 'Back'), (t1, 0, 0, 1.0, -4, 1, 'Linear')]
        if bg: self.s(parent, 'FaceBg', bg, 1.35, ks, kicks=[t0 + 0.12], kickPower=50, kickTime=0.2)     # 배경 · 쥐 따로 그린 판 (같은 키)
        return self.s(parent, 'FaceClose', 'face_close', 1.35, ks, kicks=[t0 + 0.12], kickPower=50, kickTime=0.2)

    # ── 패턴 (쥐마다 골라 섞음) ──
    def rain(self, parent, name, spr, times, xs, y0, y1, sc=1.2, dur=0.12, rot=0.0, impact=None, isc=0.9, ring=None):
        """위에서 내리꽂힘 (+ 바닥 impact · ring)"""
        for i, (t, x) in enumerate(zip(times, xs)):
            x1 = x + math.tan(math.radians(rot)) * (y0 - y1)
            self.s(parent, f'{name}{i}', spr, sc, [(t - 0.01, x, y0, 1, rot, 0, 'Out'), (t, x, y0, 1, rot, 1, 'Snap'), (t + dur, x1, y1, 1.05, rot, 1, 'In'), (t + dur + 0.17, x1, y1, 1, rot, 0, 'Linear')], flicker=0.35, jitter=4.0)
            if impact: self.s(parent, f'{name}I{i}', impact, isc, [(t + dur - 0.01, x1, y1 - 200, 0, 0, 0, 'Out'), (t + dur, x1, y1 - 200, 0.4, 0, 1, 'Snap'), (t + dur + 0.12, x1, y1 - 200, 1.1, 0, 1, 'Back'), (t + dur + 0.37, x1, y1 - 200, 1.3, 0, 0, 'Linear')], spin=60.0)
            if ring: self.s(parent, f'{name}R{i}', ring, 0.6, [(t + dur, x1, y1 - 220, 0.3, 0, 0.9, 'Out'), (t + dur + 0.32, x1, y1 - 220, 1.5, 0, 0, 'Out')])

    def rise(self, parent, name, spr, times, xs, y0, y1, sc=1.0, dur=0.3, burst=None, bsc=1.0):
        """아래에서 솟구쳐 위에서 터짐 (폭죽 · 기포)"""
        for i, (t, x) in enumerate(zip(times, xs)):
            self.s(parent, f'{name}{i}', spr, sc, [(t - 0.01, x, y0, 0.6, 180, 0, 'Out'), (t, x, y0, 0.8, 180, 1, 'Snap'), (t + dur, x, y1, 1.0, 180, 1, 'Out'), (t + dur + 0.05, x, y1, 0.4, 180, 0, 'Linear')], flicker=0.25)
            if burst: self.s(parent, f'{name}B{i}', burst, bsc, [(t + dur - 0.01, x, y1, 0, 0, 0, 'Out'), (t + dur, x, y1, 0.3, 0, 1, 'Snap'), (t + dur + 0.15, x, y1, 1.2, 0, 1, 'Back'), (t + dur + 0.5, x, y1, 1.5, 20, 0, 'Linear')], spin=40.0)

    def sweep(self, parent, name, spr, t0, t1, pivot, a0, a1, sc=2.0, n=1, gap=0.0):
        """한 점을 축으로 휘두르듯 쓸고 지나감 (빛줄기 · 스포트라이트 · 트랙터 빔)"""
        for i in range(n):
            s0 = t0 + i * gap
            self.s(parent, f'{name}{i}', spr, sc, [(s0 - 0.01, pivot[0], pivot[1], 1, a0, 0, 'Out'), (s0, pivot[0], pivot[1], 1, a0, 0.9, 'Snap'), (t1 + i * gap, pivot[0], pivot[1], 1, a1, 0.9, 'InOut'), (t1 + i * gap + 0.12, pivot[0], pivot[1], 1, a1, 0, 'Linear')], flicker=0.2)

    def shower(self, parent, name, spr, t0, t1, n, x0, x1, y0, y1, sc=0.5):
        """작은 것들이 우수수 (금화 · 꽃잎 · 기부금)"""
        for i in range(n):
            t = t0 + (t1 - t0) * self.rnd.random(); x = self.rnd.uniform(x0, x1); r = self.rnd.uniform(-180, 180)
            self.s(parent, f'{name}{i}', spr, sc * self.rnd.uniform(0.7, 1.2), [(t - 0.01, x, y0, 1, r, 0, 'Out'), (t, x, y0, 1, r, 1, 'Snap'), (t + 0.55, x + self.rnd.uniform(-80, 80), y1, 1, r + 200, 1, 'In'), (t + 0.65, x, y1, 1, r + 220, 0, 'Linear')])

    def spiral(self, parent, name, spr, t0, t1, cx, cy, n=6, r0=500, r1=60, sc=0.5, turns=1.5):
        """빙글빙글 돌며 가운데로 (눈꽃 · 접시 · 모래)"""
        for i in range(n):
            ks = []
            for k in range(9):
                u = k / 8; t = t0 + (t1 - t0) * u; a = 2 * math.pi * (i / n + turns * u); r = r0 + (r1 - r0) * u
                ks.append((t, cx + math.cos(a) * r, cy + math.sin(a) * r * 0.7, sc * (0.7 + 0.5 * u), math.degrees(a), 1 if k < 8 else 0, 'Linear'))
            self.s(parent, f'{name}{i}', spr, 1.0, ks)

    def bounce(self, parent, name, spr, t0, x0, x1, ground, height=600, hops=3, dur=0.6, sc=0.8, impact=None):
        """통통 튀며 지나감 (축구공)"""
        ks = [(t0 - 0.01, x0, ground + height, sc, 0, 0, 'Out')]
        for h in range(hops + 1):
            u = h / hops; t = t0 + dur * u; x = x0 + (x1 - x0) * u
            ks.append((t, x, ground, sc * 1.05, 360 * u * 2, 1, 'In'))
            if h < hops: ks.append((t + dur / hops / 2, x + (x1 - x0) / hops / 2, ground + height * (0.75 ** (h + 1)), sc, 360 * u * 2 + 180, 1, 'Out'))
            if impact: self.s(parent, f'{name}I{h}', impact, 0.6, [(t - 0.01, x, ground - 80, 0, 0, 0, 'Out'), (t, x, ground - 80, 0.4, 0, 1, 'Snap'), (t + 0.25, x, ground - 80, 1.2, 0, 0, 'Out')])
        ks.append((t0 + dur + 0.15, x1, ground, sc, 720, 0, 'Linear'))
        self.s(parent, name, spr, 1.0, ks)

    def burst_ring(self, parent, name, spr, t0, cx, cy, n=8, r=700, sc=0.7, dur=0.45):
        """한 점에서 사방으로 터져 나감"""
        for i in range(n):
            a = 2 * math.pi * i / n + 0.3
            self.s(parent, f'{name}{i}', spr, sc, [(t0 - 0.01, cx, cy, 0.3, math.degrees(a), 0, 'Out'), (t0, cx, cy, 0.4, math.degrees(a), 1, 'Snap'), (t0 + dur, cx + math.cos(a) * r, cy + math.sin(a) * r * 0.75, 1.1, math.degrees(a), 1, 'Out'), (t0 + dur + 0.2, cx + math.cos(a) * r * 1.1, cy + math.sin(a) * r * 0.8, 1.1, math.degrees(a), 0, 'Linear')], flicker=0.2)

    def stomp(self, parent, name, spr, times, pts, sc=1.0, impact=None):
        """크게 떨어져 쿵 (도장 · 프라이팬)"""
        for i, (t, (x, y)) in enumerate(zip(times, pts)):
            self.s(parent, f'{name}{i}', spr, sc, [(t - 0.01, x, y, 3, 30, 0, 'Out'), (t, x, y, 3, 30, 0.7, 'Snap'), (t + 0.1, x, y, 1.0, 0, 1, 'In'), (t + 0.18, x, y, 1.1, 0, 1, 'Back'), (t + 0.5, x, y, 1.0, 0, 0, 'Linear')])
            if impact: self.s(parent, f'{name}I{i}', impact, 0.8, [(t + 0.09, x, y, 0, 0, 0, 'Out'), (t + 0.1, x, y, 0.4, 0, 1, 'Snap'), (t + 0.3, x, y, 1.3, 0, 0, 'Out')])

    def converge(self, tr, name, spr, t0, pts, target, sc=1.1):
        """사방에서 한 점으로 빨려 듦 (파노라마 좌표)"""
        tx, ty = self.P(*target)
        for i, (x, y) in enumerate(pts):
            px, py = self.P(x, y); ang = math.degrees(math.atan2(ty - py, tx - px)) + 180; t = t0 + i * 0.05
            self.layer(tr, f'{name}{i}', spr, sc, [(t, px, py, 0.6, ang, 0, 'Out'), (t + 0.05, px, py, 1, ang, 1, 'Linear'), (t + 0.38, tx, ty, 0.5, ang, 1, 'In'), (t + 0.43, tx, ty, 0.2, ang, 0, 'Linear')], flicker=0.35)

    def pops(self, tr, name, spr, pts, t0, gap=0.07, sc=0.35, up=0.0):
        """파노라마 곳곳에서 펑 튀어나옴"""
        for i, (x, y) in enumerate(pts):
            t = t0 + i * gap
            self.w(tr, f'{name}{i}', spr, sc, [(t - 0.01, x, y, 0, 0, 0, 'Out'), (t, x, y, 0.4, 0, 1, 'Snap'), (t + 0.12, x, y - 0.01, 1.1, 10, 1, 'Back'), (t + 0.5, x + 0.01, y - 0.04 - up, 0.9, 40, 0, 'In')], jitter=3.0)

    def trail(self, tr, name, spr, pts, t0, gap=0.08, sc=0.3):
        """하나씩 차례로 나타나 남음 (발자국)"""
        for i, (x, y) in enumerate(pts):
            t = t0 + i * gap
            self.w(tr, f'{name}{i}', spr, sc, [(t - 0.01, x, y, 0, 0, 0, 'Out'), (t, x, y, 1.2, 0, 1, 'Snap'), (t + 0.1, x, y, 1, 0, 1, 'Out')])

    def speed(self, parent, t0, t1, vertical=True, n=7):
        """속도선 (카메라가 휙 움직일 때)"""
        for i in range(n):
            if vertical:
                x = -840 + i * 280; s = t0 + (i % 3) * 0.04
                self.s(parent, f'Speed{i}', 'fx_streak', 1.0, [(s, x, -700, 1, 90, 0, 'Out'), (s + 0.05, x, -300, 1.2, 90, 0.5, 'Linear'), (t1, x, 900, 1.2, 90, 0, 'Linear')], flicker=0.3)
            else:
                y = -420 + i * 140; s = t0 + (i % 3) * 0.04
                self.s(parent, f'Speed{i}', 'fx_streak', 1.0, [(s, 1200, y, 1, 0, 0, 'Out'), (s + 0.05, 600, y, 1.2, 0, 0.5, 'Linear'), (t1, -1300, y, 1.2, 0, 0, 'Linear')], flicker=0.3)

    def rig(self, parent, name, ks, moves, width=320, face=1, pano=True, **fx):
        """컷신 속 쥐 리그 (SuperUltRig, 관절 애니메이션 — rig.json · rig_* 그림이 있는 쥐만). ks = 자리 키 (pano=True 면 파노라마 비율)
        moves = [(t, 'Skate'|'Cast'|'Leap'|'Spin'|'Land'|'Point'|'Twirl'|'Dash'|'Idle', speed, amount)]"""
        rj = json.load(open(os.path.join(self.art, self.code, 'rig.json'), encoding='utf-8'))
        var = self.v('rig')
        k2 = [(t, *self.P(x, y), sc, r, a, e) for t, x, y, sc, r, a, e in ks] if pano else ks
        self(f'var {var} = E("{name}", {parent}, {keys(k2)}); {self.extra(var, fx)}')
        g = self.v('rg')
        V = lambda v: f'new Vector2({f(v[0])}, {f(v[1])})'
        self(f'{{ var {g} = {var}.gameObject.AddComponent<NKK.Ults.SuperUltRig>(); {g}.torso = S("rig_torso"); {g}.head = S("rig_head"); {g}.front = S("rig_front"); {g}.back = S("rig_back"); {g}.tail = S("rig_tail"); {g}.cape = S("rig_cape");')
        self(f'  {g}.neck = {V(rj["neck"])}; {g}.shoulder = {V(rj["shoulder"])}; {g}.hip = {V(rj["hip"])}; {g}.tailRoot = {V(rj["tailRoot"])}; {g}.capeRoot = {V(rj.get("capeRoot", (0.1, 0.3)))};')
        self(f'  {g}.headPivot = {V(rj["headPivot"])}; {g}.frontPivot = {V(rj["frontPivot"])}; {g}.backPivot = {V(rj["backPivot"])}; {g}.tailPivot = {V(rj["tailPivot"])}; {g}.capePivot = {V(rj.get("capePivot", (0.03, 0.3)))};')
        self(f'  {g}.headScale = {f(rj["headScale"])}; {g}.frontScale = {f(rj["frontScale"])}; {g}.backScale = {f(rj["backScale"])}; {g}.tailScale = {f(rj["tailScale"])}; {g}.capeScale = {f(rj["capeScale"])}; {g}.torsoWidth = {f(width)}; {g}.face = {face};')
        mk = ', '.join(f'new NKK.Ults.SuperUltRig.MoveKey {{ t = {f(t)}, move = NKK.Ults.SuperUltRig.Move.{m}, speed = {f(sp)}, amount = {f(am)} }}' for t, m, sp, am in moves)
        self(f'  {g}.moves = new[] {{ {mk} }}; {g}.Build(); }}')
        return var

    def cel(self, parent, name, frames, ks, height=520, ref='anim_land_3', pano=True, **fx):
        """셀 애니 캐릭터 (2026-10-19 사용자: 리깅 파츠를 바탕으로 그린 동작 프레임을 바꿔 끼움). frames = [(t, 그림)] — 첫 그림부터,
        ks = 발끝 자리 키 (pano=True 면 파노라마 비율). 모든 프레임은 같은 시트 배율 → 픽셀 비율 그대로 (swapResize), 피벗 = 발끝 가운데"""
        k = height / max(1, self.size(ref)[1])
        k2 = [(t, *self.P(x, y), sc, r, a, e) for t, x, y, sc, r, a, e in ks] if pano else ks
        var = self.layer(parent, name, frames[0][1], k, k2, **fx)
        self(f'((RectTransform){var}.transform).pivot = new Vector2(0.5f, 0f); {var}.swapResize = true;')
        sw = ', '.join(f'new NKK.Ults.SuperUltLayer.Swap {{ t = {f(t)}, sprite = S("{spr}") }}' for t, spr in frames[1:])
        if sw: self(f'{var}.swaps = new[] {{ {sw} }};')
        return var

    def pose(self, tr, name, spr, px, ks, **fx):
        """캐릭터 포즈 (높이 px)"""
        return self.w(tr, name, spr, self.hp(spr, px), ks, **fx)


# ══════════════════════ 쥐별 연출 ══════════════════════
# 각 함수: s = Story, R = 루트 묶음. ②(Intro) · ③(Act) · ④(Finish) 를 직접 짬.

def parkrat(s, R):
    """가로: 줄 선 입구 → 퍼레이드 거리 → 호수 불꽃. ② 퍼레이드를 따라 옆으로 훑기 · ③ 아래 띠 + 폭죽이 솟아 게임 화면 위에서 터짐 · ④ 불꽃 피날레 와이드"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.12, 0.55, 1.35, -3, 'Out'), (1.0, 0.36, 0.62, 1.25, 0, 'InOut'), (1.45, 0.52, 0.6, 1.15, 2, 'InOut'), (1.9, 0.56, 0.62, 1.3, 3, 'Linear')], kicks=[0.95])
    s.w(tr, 'Float', 'prop_float2', 0.8, [(0.4, 0.05, 0.78, 1, 0, 1, 'Out'), (1.9, 0.5, 0.78, 1, 0, 1, 'Linear')], jitter=2.0)
    s.pose(tr, 'Pose', 'pose_intro', 620, [(0.4, 0.15, 0.68, 1, -4, 1, 'Out'), (1.9, 0.55, 0.68, 1, 4, 1, 'Linear')], pulse=0.03, pulseHz=3.0)
    s.pops(tr, 'Balloon', 'prop_pop', [(0.2, 0.55), (0.28, 0.5), (0.4, 0.58), (0.47, 0.48), (0.58, 0.55)], 0.7, gap=0.12, up=0.25)
    s.rise(I, 'Fw', 'fx_strike', [0.9, 1.1, 1.3, 1.5], [-600, 500, -100, 300], -500, 330, burst='fx_impact', bsc=1.0)
    s.s(I, 'Bunting', 'prop_fg', 2.2, [(0.4, -300, 470, 1, 0, 1, 'Out'), (1.9, 300, 470, 1, 0, 1, 'Linear')])
    s.face(I, 'slide', 1.62)
    A = s.group('Act', R, 1.9, 3.9)
    b = s.band(A, 'Band', (2500, 460), [(1.9, 0, -700, 1, 4, 1, 'Out'), (2.08, 0, -260, 1, 4, 1, 'Back'), (3.9, 0, -250, 1, 4, 1, 'Linear')])
    s.bandpano(b, [(1.9, 600, s.P(0.5, 0.6)[1] * -1, 1, -4, 1, 'Out'), (3.9, -600, s.P(0.5, 0.6)[1] * -1, 1, -4, 1, 'Linear')])
    s.s(b, 'Pose', 'pose_act', s.hp('pose_act', 600), [(1.9, -1300, -100, 0.9, -8, 1, 'Out'), (2.3, -200, 60, 1, -4, 1, 'Back'), (3.9, 200, 80, 1.05, 4, 1, 'Linear')], jitter=3.0)
    s.s(b, 'Band2', 'prop_band', 0.4, [(2.4, 1300, -60, 0.9, 0, 1, 'Out'), (3.6, -1300, -40, 1, 0, 1, 'Linear')])
    s.rise(A, 'Fw', 'fx_strike', [h - 0.3 for h in HITS], [-700, 400, -250, 650, -500, 150, -800, 520, 0, 760], -200, 380, burst='fx_impact', bsc=1.1)
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.82, 0.5, 1.5, 6, 'Out'), (4.2, 0.8, 0.5, 0.95, -2, 'InOut'), (5.0, 0.8, 0.52, 0.9, 0, 'Out')], kicks=[4.2], power=60)
    s.pose(tr, 'Pose', 'pose_finish', 680, [(3.9, 0.8, 1.2, 1, 0, 1, 'Out'), (4.25, 0.8, 0.68, 1, 0, 1, 'Back'), (5.0, 0.8, 0.67, 1.02, 0, 1, 'Linear')])
    s.burst_ring(F, 'Burst', 'fx_impact', 4.2, 0, 150, n=7, r=750, sc=0.9)
    s.burst_ring(F, 'Heart', 'fx_aura', 4.35, 0, 150, n=5, r=500, sc=0.5)
    s.meme(F, 4.3)


def emperor(s, R):
    """세로: 금빛 하늘 → 궁궐 → 계단. ② 계단 아래에서 위로 걸어 오르는 황제를 쫓으며 대포 · 등불 · ③ 위·아래 두 띠(시네마) + 대포알이 비스듬히 · ④ 옥좌 클로즈업에서 확 물러나며 금 비"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.62, 0.95, 1.7, 0, 'Out'), (1.1, 0.5, 0.82, 1.3, -3, 'InOut'), (1.5, 0.48, 0.58, 0.95, 0, 'InOut'), (1.9, 0.48, 0.74, 1.2, -4, 'Linear')], kicks=[1.12, 1.2, 1.28])
    s.pose(tr, 'Pose', 'pose_intro', 620, [(0.4, 0.7, 0.93, 1, 3, 1, 'Out'), (1.1, 0.5, 0.82, 1, -2, 1, 'Linear'), (1.9, 0.48, 0.8, 1.04, 0, 1, 'Linear')], pulse=0.02, pulseHz=2.5)
    s.stomp(I, 'Cannon', 'fx_impact', [1.12, 1.2, 1.28], [(-650, -250), (650, -200), (0, 380)], sc=1.0)
    s.pops(tr, 'Lantern', 'prop_pop', [(0.2, 0.55), (0.35, 0.45), (0.65, 0.5), (0.8, 0.42), (0.5, 0.36)], 1.2, gap=0.06, up=0.15)
    s.converge(tr, 'Gold', 'fx_streak', 1.3, [(0.1, 0.5), (0.9, 0.48), (0.3, 0.3), (0.7, 0.28)], (0.48, 0.74))
    s.face(I, 'zoom', 1.66)
    A = s.group('Act', R, 1.9, 3.9)
    top = s.band(A, 'BandTop', (2500, 300), [(1.9, 0, 700, 1, 0, 1, 'Out'), (2.05, 0, 390, 1, 0, 1, 'Back'), (3.9, 0, 390, 1, 0, 1, 'Linear')])
    bot = s.band(A, 'BandBot', (2500, 300), [(1.9, 0, -700, 1, 0, 1, 'Out'), (2.05, 0, -390, 1, 0, 1, 'Back'), (3.9, 0, -390, 1, 0, 1, 'Linear')])
    s.bandpano(top, [(1.9, -400, -s.P(0.5, 0.1)[1], 1, 0, 1, 'Out'), (3.9, 400, -s.P(0.5, 0.1)[1], 1, 0, 1, 'Linear')])
    s.bandpano(bot, [(1.9, 400, -s.P(0.5, 0.88)[1], 1, 0, 1, 'Out'), (3.9, -400, -s.P(0.5, 0.88)[1], 1, 0, 1, 'Linear')])
    s.s(top, 'Pose', 'pose_act', s.hp('pose_act', 280), [(1.9, -1400, -20, 0.8, 0, 1, 'Out'), (2.2, -250, 0, 1, 0, 1, 'Back'), (3.9, 250, 0, 1.05, 0, 1, 'Linear')])
    s.s(bot, 'Seal', 'prop_float2', 0.4, [(2.2, -1300, 0, 1, 0, 1, 'Out'), (3.8, 1300, 0, 1, 360, 1, 'Linear')])
    s.rain(A, 'Ball', 'fx_strike', HITS, [-900, -500, -800, -300, -700, -100, -600, 100, -400, 300], 260, -150, sc=1.0, dur=0.14, rot=-28, impact='fx_impact', isc=0.8, ring='fx_ring')
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.5, 0.78, 2.3, 4, 'Out'), (4.15, 0.5, 0.74, 1.0, -1, 'InOut'), (5.0, 0.5, 0.72, 0.9, 0, 'Out')], kicks=[4.15], power=70)
    s.pose(tr, 'Pose', 'pose_finish', 700, [(3.9, 0.5, 0.79, 1, 0, 1, 'Out'), (5.0, 0.5, 0.79, 1.02, 0, 1, 'Linear')])
    s.shower(F, 'Coin', 'fx_aura', 4.1, 4.9, 16, -900, 900, 650, -560, sc=0.25)
    s.s(F, 'Dragon', 'fx_streak', 2.2, [(4.15, -1400, 300, 1, 0, 1, 'Out'), (4.8, 1400, 380, 1.1, 0, 1, 'Linear')], flicker=0.2)
    s.meme(F, 4.3, side=-1)


def pharaoh(s, R):
    """세로: 태양 → 피라미드 → 신전. ② 신전 위에서 천천히 태양까지 올려다봄 → 태양이 번쩍 → 휙 내려와 쾅 · ③ 오른쪽 세로 띠 + 햇빛 기둥이 부채꼴로 쓸고 지나감 · ④ 비스듬히 돌며 태양 고리 · 모래 소용돌이"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.55, 0.86, 1.4, 0, 'Out'), (1.1, 0.5, 0.15, 1.1, 0, 'InOut'), (1.25, 0.5, 0.14, 1.15, 0, 'Linear'), (1.45, 0.55, 0.82, 1.35, 6, 'In'), (1.9, 0.55, 0.82, 1.4, 4, 'Linear')], kicks=[1.45])
    s.w(tr, 'Sun', 'fx_ring', 3.0, [(0.4, 0.5, 0.12, 0.6, 0, 0.8, 'Out'), (1.2, 0.5, 0.12, 1.4, 0, 1, 'Back'), (1.9, 0.5, 0.12, 1.5, 0, 1, 'Linear')], spin=25.0, pulse=0.04)
    s.pose(tr, 'Pose', 'pose_intro', 620, [(0.4, 0.55, 0.81, 1, 0, 1, 'Out'), (1.9, 0.55, 0.81, 1.03, 0, 1, 'Linear')])
    s.s(I, 'Flash', 'fx_burst', 3.5, [(1.2, 0, 0, 0, 0, 0, 'Out'), (1.25, 0, 0, 1.2, 0, 1, 'Snap'), (1.45, 0, 0, 1.6, 30, 0, 'Out')])
    s.pops(tr, 'Scarab', 'prop_pop', [(0.3, 0.7), (0.75, 0.68), (0.4, 0.62), (0.68, 0.6)], 1.48, gap=0.05)
    s.s(I, 'Sunscreen', 'prop_meme', 0.5, [(1.5, 1200, 300, 0.8, 20, 1, 'Out'), (1.9, 500, 330, 1, -10, 1, 'Out')])
    s.face(I, 'full', 1.7)
    A = s.group('Act', R, 1.9, 3.9)
    b = s.band(A, 'Band', (720, 1300), [(1.9, 1300, 0, 1, 0, 1, 'Out'), (2.08, 560, 0, 1, 0, 1, 'Back'), (3.9, 560, 0, 1, 0, 1, 'Linear')])
    s.bandpano(b, [(1.9, 0, -s.P(0.5, 0.5)[1] + 300, 1, 0, 1, 'Out'), (3.9, 0, -s.P(0.5, 0.5)[1] - 300, 1, 0, 1, 'Linear')])
    s.s(b, 'Pose', 'pose_act', s.hp('pose_act', 640), [(1.9, 0, 900, 0.8, 0, 1, 'Out'), (2.25, 0, 60, 1, 0, 1, 'Back'), (3.9, 0, 90, 1.05, 0, 1, 'Linear')], pulse=0.03)
    for i, t0 in enumerate([2.0, 2.6, 3.2]):
        s.sweep(A, f'Beam{i}_', 'fx_strike', t0, t0 + 0.5, (200, 560), -60, 20, sc=2.4)
    s.rain(A, 'Hit', 'fx_burst', HITS, [-800, -300, -600, 0, -700, 100, -450, -150, -850, 50], 250, -250, sc=0.5, dur=0.06, impact='fx_impact', isc=0.7)
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.7, 0.72, 1.4, -18, 'Out'), (4.15, 0.55, 0.78, 1.25, 4, 'Back'), (5.0, 0.55, 0.77, 1.05, 0, 'Out')], kicks=[4.15], power=60)
    s.w(tr, 'SunRing', 'fx_ring', 2.0, [(3.9, 0.55, 0.72, 0, 0, 1, 'Out'), (4.2, 0.55, 0.72, 1.5, 0, 1, 'Back'), (5.0, 0.55, 0.72, 1.7, 0, 0.6, 'Linear')], spin=40.0)
    s.pose(tr, 'Pose', 'pose_finish', 680, [(3.9, 0.55, 1.1, 1, 0, 1, 'Out'), (4.2, 0.55, 0.79, 1, 0, 1, 'Back'), (5.0, 0.55, 0.79, 1.02, 0, 1, 'Linear')])
    s.spiral(F, 'Sand', 'fx_streak', 3.95, 4.9, 0, -50, n=6, r0=900, r1=200, sc=0.6, turns=1.0)
    s.meme(F, 4.3)


def knight(s, R):
    """세로: 노을 하늘 → 돌 원탁 언덕 → 안개 초원. (2026-10-19 v10, 사용자: 엑스칼리버 순서 — 기 모으기 → 얼굴 클로즈업 → 휘두르기 → 후경)
    띠 · 마무리 컷 · 글 · 별 없음 · 리그 파츠 바탕 셀 프레임 · 플랫 그림 · 0.75배속 — 컷 7개:
    ① 0.4 언덕 위 돌 제단에 꽂힌 대검 · 하늘 빛 ② 0.9 손잡이를 잡고 힘껏 → 돌 파편과 함께 뽑아 치켜듦 ③ 1.45 어깨에 메고 에너지 차징
    ④ 2.15 정면: 대검을 머리 뒤로 높이 · 칼날에 빛 기둥 · 빛 알갱이가 모임 → 트래킹 줌으로 얼굴까지 ⑤ 2.75 눈 익스트림 클로즈업
    ⑥ 2.92 정면에서 카메라 쪽으로 휘둘러 내림 · 내려오는 칼을 따라 빛 띠가 화면을 가림
    ⑦ 3.14 후경: 캐릭터가 딛고 선 초원(원근) · 칼끝에서 카메라 쪽으로 땅이 갈라지며 틈에서 빛이 솟음 · 파편 · 흔들림
    → 컷신 걷힘 → 게임 화면: 가로 파동이 한 번 퍼지며 1초에 다단 10타 (worldPhase · lineSprite · lineOnce)"""
    END = NO_FINISH['knight']
    import math

    def cut_flash(g, t0, a=0.55):
        s.s(g, 'CutFlash', 'fx_flash', 6.0, [(t0, 0, 0, 1.0, 0, a, 'Snap'), (t0 + 0.09, 0, 0, 1.3, 0, 0, 'Out')])

    # ① 언덕 위 돌 제단에 꽂힌 대검, 하늘에서 빛이 내려옴 (천천히 다가감)
    g = s.group('Cut1', R, 0.4, 0.9)
    tr = s.cam(g, [(0.4, 0.5, 0.52, 1.3, 0, 'Out'), (0.9, 0.5, 0.56, 1.75, 0, 'InOut')], kicks=[0.4], power=20)
    s.w(tr, 'Beam', 'fx_pillar', 2.2, [(0.4, 0.5, 0.42, 1.0, 0, 0, 'Out'), (0.55, 0.5, 0.42, 1.0, 0, 0.7, 'Out'), (0.9, 0.5, 0.42, 1.05, 0, 0.6, 'Linear')], flicker=0.1)
    s.w(tr, 'Sword', 'prop_greatsword', 0.55, [(0.4, 0.5, 0.565, 1, 0, 1, 'Out')])
    s.w(tr, 'Altar', 'prop_altar', 0.42, [(0.4, 0.5, 0.615, 1, 0, 1, 'Out')])
    s.rise(g, 'Up', 'fx_dots', [0.45 + 0.04 * i for i in range(10)], [s.rnd.uniform(-700, 700) for _ in range(10)], -500, 600, sc=0.45, dur=0.4)
    # ② 손잡이를 잡고 힘껏 → 돌 파편과 함께 뽑아 → 치켜듦
    PULL = 1.12
    g = s.group('Cut2', R, 0.9, 1.45)
    tr = s.cam(g, [(0.9, 0.5, 0.585, 2.4, -3, 'Out'), (PULL, 0.5, 0.585, 2.5, 3, 'Linear'), (1.45, 0.5, 0.57, 2.2, 0, 'Out')], kicks=[PULL, 1.28], power=45, rot=3)
    s.w(tr, 'Altar', 'prop_altar', 0.42, [(0.9, 0.5, 0.615, 1, 0, 1, 'Out')])
    s.cel(tr, 'Knight', [(0.9, 'anim_pull_1'), (PULL, 'anim_pull_2'), (1.28, 'anim_pull_3')],
          [(0.9, 0.5, 0.596, 1, 0, 1, 'Out'), (PULL, 0.5, 0.596, 1.06, 0, 1, 'Back'), (1.28, 0.5, 0.596, 1.08, 0, 1, 'Back'), (1.45, 0.5, 0.596, 1.1, 0, 1, 'Linear')], height=250, ref='anim_pull_1')
    s.burst_ring(g, 'Debris', 'prop_debris', PULL, 0, -80, n=7, r=900, sc=0.4, dur=0.35)
    s.s(g, 'Flash', 'fx_flash', 3.0, [(1.27, 0, 150, 0, 0, 0, 'Out'), (1.28, 0, 150, 0.9, 0, 0.8, 'Snap'), (1.45, 0, 150, 1.3, 20, 0, 'Out')])
    cut_flash(g, 0.9)
    # ③ 대검을 어깨에 메고 에너지 차징: 기운이 점점 커지고 흔들림이 세짐
    T = 1.45
    g = s.group('Cut3', R, T, T + 0.7)
    tr = s.cam(g, [(T, 0.5, 0.875, 1.7, 0, 'Out'), (T + 0.7, 0.5, 0.88, 2.25, 0, 'In')], kicks=[T + 0.25, T + 0.4, T + 0.5, T + 0.58, T + 0.64], power=26, rot=2, ktime=0.12)
    s.w(tr, 'Aura', 'fx_charge', 1.2, [(T, 0.5, 0.86, 0.3, 0, 0, 'Out'), (T + 0.25, 0.5, 0.86, 1.0, 0, 0.9, 'Back'), (T + 0.7, 0.5, 0.86, 1.5, 0, 1, 'Linear')], spin=40.0, pulse=0.06, pulseHz=9)
    s.cel(tr, 'Knight', [(T, 'anim_charge_1'), (T + 0.25, 'anim_charge_2')],
          [(T, 0.5, 0.905, 1, 0, 1, 'Out'), (T + 0.25, 0.5, 0.905, 0.95, 0, 1, 'Back'), (T + 0.7, 0.5, 0.905, 1.0, 0, 1, 'Linear')], height=430, ref='anim_charge_1', jitter=2.0)
    for k in range(3):
        s.converge(tr, f'Spark{k}_', 'fx_dots', T + 0.1 + 0.18 * k, [(0.2, 0.75), (0.8, 0.78), (0.25, 0.95), (0.78, 0.97), (0.5, 0.7)], (0.5, 0.86), sc=0.45)
    s.s(g, 'Ring', 'fx_lightring', 1.4, [(T + 0.25, 0, 0, 0.3, 0, 0.9, 'Snap'), (T + 0.5, 0, 0, 1.5, 0, 0, 'Out')])
    s.s(g, 'Ring2', 'fx_lightring', 1.4, [(T + 0.5, 0, 0, 0.3, 0, 0.9, 'Snap'), (T + 0.7, 0, 0, 1.5, 0, 0, 'Out')])
    cut_flash(g, T)
    # ④ 정면: 대검을 머리 뒤로 높이 든 채 칼날에 빛 기둥 · 빛 알갱이가 모임 (기 모으기) → 트래킹 줌으로 얼굴까지
    F0, ZOOM0, FACE0 = 2.15, 2.6, 2.75
    g = s.group('Cut4', R, F0, FACE0 + 0.02)
    tr = s.cam(g, [(F0, 0.5, 0.86, 1.55, 0, 'Out'), (FACE0, 0.5, 0.86, 1.7, 0, 'Linear')], kicks=[F0 + 0.2], power=14)
    Z = 3.6; fx_, fy_ = 164, -224                     # 기사 얼굴(그림 속 위치 → 화면 (164, -224))이 가운데로 오며 확 커짐
    zv = s.v('zoom')
    s(f'var {zv} = E("Zoom", {g}, {keys([(F0, 0, 0, 1.0, 0, 1, "Out"), (ZOOM0, 0, 0, 1.06, 0, 1, "Out"), (FACE0 + 0.02, -fx_ * Z, -fy_ * Z, Z, 0, 1, "In")])}).transform;')
    # (화면을 가로지르던 사선 빛 기둥은 뺌 — 사용자)
    s.cel(zv, 'Knight', [(F0, 'anim_excal_1')], [(F0, 0, -540, 1, 0, 1, 'Out')], height=760, ref='anim_excal_1', pano=False)
    for k in range(3):
        s.converge(tr, f'Dot{k}_', 'fx_dots', F0 + 0.05 + 0.13 * k, [(0.2, 0.7), (0.8, 0.72), (0.15, 0.9), (0.85, 0.9), (0.5, 0.62), (0.3, 0.6)], (0.47, 0.8), sc=0.4)
    s.s(g, 'Ring', 'fx_lightring', 1.6, [(F0 + 0.2, 0, -460, 0.3, 0, 0.9, 'Snap'), (F0 + 0.45, 0, -460, 1.6, 0, 0, 'Out')])
    s.s(g, 'ZoomStreak', 'fx_frame', 1.35, [(ZOOM0, 0, 0, 1.3, 0, 0, 'Out'), (ZOOM0 + 0.05, 0, 0, 1.15, 0, 0.8, 'Out'), (FACE0 + 0.02, 0, 0, 1.0, 0, 0.9, 'Linear')])
    cut_flash(g, F0)
    # ⑤ 눈 익스트림 클로즈업 + 휘두르는 칼의 잔상 줄무늬가 칼 각도(A)로 얼굴 위를 스침 → 빛 · 불꽃이 화면을 덮음 (UBW 22.3~22.6초)
    #    (2026-10-19 사용자: 프레임 분석대로 · 잔상 줄무늬만 · 갈라지는 방향 = 칼을 휘두른 각도)
    A = KNIGHT_LINE                                   # 칼 각도 (도, UI 반시계 +): 후경 기사 칼날을 재서 -40 (왼쪽 위 → 오른쪽 아래)
    ra = math.radians(A); D = (math.cos(ra), math.sin(ra)); N = (-math.sin(ra), math.cos(ra))      # 칼 방향 · 그 수직 (위쪽)
    SM0, COVER, CUT = 2.9, 3.04, 3.12
    F = s.group('Face', R, FACE0, CUT + 0.02)
    ex, ey = 112 * 1.35, -88 * 1.35
    for nm, spr in (('FaceBg', 'face_bg'), ('FaceClose', 'face_close')):
        s.s(F, nm, spr, 1.35, [(FACE0, -ex * 1.4, -ey * 1.4, 1.4, 0, 1, 'Snap'), (CUT, -ex * 2.8, -ey * 2.8, 2.8, -3, 1, 'Out')], kicks=[FACE0, SM0 + 0.06], kickPower=30, kickTime=0.12)
    for k, (dt, al, off) in enumerate(((0.0, 1.0, 0), (0.025, 0.5, 90), (0.05, 0.3, -110))):      # 잔상 줄무늬 3겹 (뒤 것일수록 늦고 연함 = 블러)
        t0 = SM0 + dt
        x0, y0 = -D[0] * 1900 + N[0] * off, -D[1] * 1900 + N[1] * off
        x1, y1 = D[0] * 1900 + N[0] * off, D[1] * 1900 + N[1] * off
        s.s(F, f'Smear{k}', 'fx_smear', 1.6, [(t0 - 0.01, x0, y0, 1.0, A, 0, 'Out'), (t0, x0, y0, 1.0, A, al, 'Snap'), (t0 + 0.12, x1, y1, 1.15, A, al, 'In'), (t0 + 0.16, x1, y1, 1.15, A, 0, 'Linear')])
    s.s(F, 'CoverBlast', 'fx_blast', 3.0, [(COVER - 0.02, 0, 0, 0.4, A, 0, 'Out'), (COVER, 0, 0, 1.0, A, 1, 'Snap'), (CUT + 0.02, 0, 0, 2.6, A + 6, 1, 'Out')])      # 화면을 덮음 (둥근 섬광 없이)
    # ⑥ 후경 (정지 컷): 큰 세로 파노라마 pano_rear 안 Rig(줌) > Track(보는 곳) — 갈라지는 순간 틸트 다운 + 달리 아웃 반동 → 되돌아옴
    #    (2026-10-19 사용자: 칼끝 균열 · 파편 뺌 · 칼에 빛나는 이펙트 · 가장자리 블러 확실하게)
    PW, PH, PK = 1536, 2360, 1.5625                   # 파노라마 원본 크기 · 배율 (폭 2400 px)
    PP = lambda x, y: ((x - PW / 2) * PK, (PH / 2 - y) * PK)          # 파노라마 픽셀 → Track 안 px
    KH = 470                                          # 기사 키 (레퍼런스처럼 화면 높이 약 40%)
    FX_, FY_ = PP(768, 1215)                          # 기사 발 = 하이앵글 초원 가운데
    LOOK = lambda dy: (-(FX_), -(FY_ + KH * 0.45 + dy))
    RECOIL_Z = [(CUT, 1.14, 0, 'Snap'), (CUT + 0.08, 0.92, -140, 'Out'), (CUT + 0.4, 1.0, -40, 'InOut'), (END + 0.4, 1.04, -20, 'Linear')]   # (t, 줌, 보는 곳 dy (- = 틸트 다운), 이징)
    Rv = s.group('Rear', R, CUT)
    rig = s.v('rrig'); trk = s.v('rtrk')
    s(f'var {rig} = E("Rig", {Rv}, {keys([(t, 0, 0, z, 0, 1, e) for t, z, dy, e in RECOIL_Z])}); {rig}.kicks = new float[] {{ {f(CUT + 0.02)}, {f(CUT + 0.14)} }}; {rig}.kickPower = 45; {rig}.kickRot = 1.5f; {rig}.kickTime = 0.4f;')
    s(f'var {trk} = E("Track", {rig}.transform, {keys([(t, *LOOK(dy), 1, 0, 1, e) for t, z, dy, e in RECOIL_Z])}).transform;')
    s.s(trk, 'Pano', 'pano_rear', PK, [(CUT, 0, 0, 1, 0, 1, 'Out')])
    # 칼 오라 (사용자: 칼과 크기가 딱 맞게 · 노란빛 강하게): rear_knight 칼날 모양을 그대로 따 만든 오라(뒤) · 칼날 빛(위) — 기사와 같은 자리 · 크기, 깜빡이며 맥동
    s.cel(trk, 'BladeAura', [(CUT, 'rear_knight_glow')], [(CUT, FX_, FY_, 1.0, 0, 0, 'Out'), (CUT + 0.05, FX_, FY_, 1.0, 0, 1, 'Snap'), (END + 0.4, FX_, FY_, 1.0, 0, 1, 'Linear')], height=KH, ref='rear_knight', pano=False, flicker=0.15)
    s.cel(trk, 'Knight', [(CUT, 'rear_knight')], [(CUT, FX_, FY_, 1.0, 0, 1, 'Out')], height=KH, ref='rear_knight', pano=False)
    s.cel(trk, 'BladeShine', [(CUT, 'rear_knight_shine')], [(CUT, FX_, FY_, 1.0, 0, 0, 'Out'), (CUT + 0.05, FX_, FY_, 1.0, 0, 0.7, 'Snap'), (CUT + 0.25, FX_, FY_, 1.0, 0, 0.35, 'Out'), (END + 0.4, FX_, FY_, 1.0, 0, 0.3, 'Linear')], height=KH, ref='rear_knight', pano=False, flicker=0.4)
    s.s(trk, 'PanoBlur', 'pano_rear_blur', PK * 1.03, [(CUT, 0, 0, 1, 0, 1, 'Out')])          # 크롭형 마스크 블러 (기사 둘레는 선명, 바깥 = 흐림)
    # ⑦ 덮었던 빛 · 불꽃 벽이 칼 각도(A)를 따라 둘로 갈라지며, 양쪽 화면 가까이 크게 머물다 카메라 쪽으로 다가와 스쳐 지나감
    #    벽은 카메라 바로 앞 = 같은 반동을 시차 1.4배로 · 벽마다 흐린 사본(fx_flamewall_blur)을 크게 겹쳐 가장자리 블러
    K = 2.75; EDGE = 0.15 * 1024 * K                  # 그림 가운데 → 찢긴 선 거리 (px, 배율 1)
    W0 = s.group('Split', R, CUT)
    W = s.v('wcam'); s(f'var {W} = E("WallCam", {W0}, {keys([(t, 0, -dy * 1.4 * z, z, 0, 1, e) for t, z, dy, e in RECOIL_Z])}).transform;')
    s.s(W0, 'SplitLine', 'fx_cutline', 1.0, [(CUT, 0, 0, 0.4, A, 1, 'Snap'), (CUT + 0.08, 0, 0, 1.0, A, 1, 'Out'), (CUT + 0.22, 0, 0, 1.05, A, 0, 'Linear')])
    for side in (1, -1):
        for k, (spr, al, extra, lag, gs) in enumerate((('fx_flamewall_blur', 0.9, 160, 0.03, 1.12), ('fx_flamewall', 1.0, 0, 0.0, 1.0), ('fx_flamewall_blur', 0.55, 80, 0.015, 1.06))):
            ks = []
            for t, d, sc, e in ((CUT, 0, 1.0, 'Snap'), (CUT + 0.14 + lag, 300 + extra, 1.2, 'Out'), (CUT + 0.45 + lag, 520 + extra, 1.45, 'Linear'), (CUT + 0.6 + lag, 1900 + extra, 2.4, 'In')):
                r = EDGE * sc * gs + d
                ks.append((t, N[0] * r * side, N[1] * r * side, sc * gs, A + (0 if side > 0 else 180), al if t < CUT + 0.55 else 0, e))
            s.s(W, f'Wall{"U" if side > 0 else "D"}{k}', spr, K, ks, jitter=3.0 if k != 1 else 1.0)
    return None


def sultan(s, R):
    """가로: 사막 → 시장 → 궁궐. ② 궁궐 앞 램프에서 연기 → 양탄자를 따라 시장을 거꾸로(왼쪽으로) 휙 · ③ 가운데 띠(위·아래로 게임 화면) + 금화 소나기 · ④ 소원 세 번 = 줌 컷 세 번"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.85, 0.7, 1.6, 2, 'Out'), (0.95, 0.83, 0.65, 1.3, 0, 'Linear'), (1.5, 0.25, 0.55, 1.1, -4, 'InOut'), (1.9, 0.2, 0.58, 1.2, -3, 'Linear')], kicks=[0.95])
    s.pose(tr, 'Pose', 'pose_intro', 620, [(0.4, 0.85, 0.68, 1, 0, 1, 'Out'), (1.9, 0.85, 0.68, 1, 0, 1, 'Linear')])
    s.w(tr, 'Smoke', 'fx_impact', 1.8, [(0.7, 0.85, 0.55, 0, 0, 0, 'Out'), (0.95, 0.85, 0.45, 1.3, 0, 1, 'Back'), (1.5, 0.83, 0.35, 1.6, 20, 0, 'Out')], spin=30.0)
    s.w(tr, 'Carpet', 'prop_float2', 0.7, [(0.95, 0.84, 0.5, 0.6, 0, 1, 'Out'), (1.5, 0.25, 0.52, 1.1, -6, 1, 'InOut'), (1.9, 0.18, 0.5, 1.2, -4, 1, 'Linear')], jitter=3.0)
    s.speed(I, 1.0, 1.5, vertical=False)
    s.face(I, 'zoom', 1.62)
    A = s.group('Act', R, 1.9, 3.9)
    b = s.band(A, 'Band', (2500, 420), [(1.9, 0, 0, 0.05, 0, 1, 'Out'), (2.1, 0, 0, 1, 3, 1, 'Back'), (3.9, 0, 0, 1, 3, 1, 'Linear')])
    s.bandpano(b, [(1.9, -1000, -s.P(0.5, 0.45)[1], 1, -3, 1, 'Out'), (3.9, 1000, -s.P(0.5, 0.45)[1], 1, -3, 1, 'Linear')])
    s.s(b, 'Pose', 'pose_act', s.hp('pose_act', 520), [(1.9, 1400, 0, 0.9, 0, 1, 'Out'), (2.3, 200, 0, 1, 0, 1, 'Back'), (3.9, -150, 10, 1.05, 0, 1, 'Linear')], jitter=3.0)
    s.shower(A, 'CoinT', 'fx_strike', 2.0, 3.8, 14, -900, 900, 560, 240, sc=0.4)
    s.shower(A, 'CoinB', 'fx_strike', 2.0, 3.8, 14, -900, 900, -240, -560, sc=0.4)
    s.stomp(A, 'Gem', 'prop_pop', HITS[::2], [(-600, 380), (500, -380), (-200, -380), (700, 380), (100, 380)], sc=0.4, impact='fx_impact')
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.8, 0.62, 1.8, -6, 'Snap'), (4.05, 0.84, 0.66, 1.6, 6, 'Snap'), (4.2, 0.8, 0.66, 1.0, 0, 'Snap'), (5.0, 0.8, 0.66, 0.92, 0, 'Out')], kicks=[3.9, 4.05, 4.2], power=40)
    s.pose(tr, 'Pose', 'pose_finish', 680, [(3.9, 0.8, 0.68, 1, 0, 1, 'Out'), (5.0, 0.8, 0.68, 1.02, 0, 1, 'Linear')])
    for i, t in enumerate([3.9, 4.05, 4.2]):
        s.s(F, f'Wish{i}', 'fx_burst', 0.9, [(t - 0.01, [-500, 500, 0][i], 250, 0, 0, 0, 'Out'), (t, [-500, 500, 0][i], 250, 0.6, 0, 1, 'Snap'), (t + 0.3, [-500, 500, 0][i], 250, 1.4, 30, 0, 'Out')])
    s.shower(F, 'Gold', 'fx_aura', 4.2, 4.9, 12, -900, 900, 650, -560, sc=0.25)
    s.meme(F, 4.3)


def alien(s, R):
    """세로: 우주 모선 → 시골 → 밀밭 미스터리 서클. ② 밭에서 소가 빔에 끌려 올라가는 걸 따라 우주까지 올려다봄 · ③ 위쪽 띠(모선) + 트랙터 빔이 좌우로 쓸며 게임 화면을 훑음 · ④ 모선에서 서클로 내리꽂힘 + 고리 여러 겹"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.5, 0.86, 1.3, 0, 'Out'), (1.4, 0.5, 0.14, 1.0, 0, 'InOut'), (1.65, 0.5, 0.12, 1.25, 3, 'Out'), (1.9, 0.5, 0.12, 1.3, 3, 'Linear')], kicks=[1.45])
    s.w(tr, 'Beam', 'fx_strike', 3.0, [(0.45, 0.5, 0.5, 1, 0, 0, 'Out'), (0.6, 0.5, 0.5, 1, 0, 0.8, 'Linear'), (1.9, 0.5, 0.5, 1.05, 0, 0.8, 'Linear')], flicker=0.2)
    s.pose(tr, 'Pose', 'pose_intro', 600, [(0.4, 0.5, 0.8, 1, 0, 1, 'Out'), (1.9, 0.5, 0.78, 1, 0, 1, 'Linear')], pulse=0.03)
    s.w(tr, 'Cow', 'prop_meme', 0.45, [(0.5, 0.52, 0.86, 0.9, 0, 1, 'Out'), (1.45, 0.5, 0.16, 1.1, 360, 1, 'InOut'), (1.6, 0.5, 0.13, 0.4, 400, 0, 'In')])
    s.pops(tr, 'Ufo', 'prop_pop', [(0.3, 0.45), (0.7, 0.4), (0.4, 0.3), (0.62, 0.22)], 0.9, gap=0.1, up=0.1)
    s.speed(I, 0.6, 1.4, vertical=True)
    s.face(I, 'slide', 1.65)
    A = s.group('Act', R, 1.9, 3.9)
    b = s.band(A, 'Band', (2500, 440), [(1.9, 0, 800, 1, 0, 1, 'Out'), (2.1, 0, 300, 1, 0, 1, 'Back'), (3.9, 0, 300, 1, 0, 1, 'Linear')])
    s.bandpano(b, [(1.9, -300, -s.P(0.5, 0.1)[1], 1, 0, 1, 'Out'), (3.9, 300, -s.P(0.5, 0.1)[1], 1, 0, 1, 'Linear')])
    s.s(b, 'Pose', 'pose_act', s.hp('pose_act', 500), [(1.9, -1400, 0, 0.9, -10, 1, 'Out'), (2.3, -300, 20, 1, -6, 1, 'Back'), (3.9, 300, 30, 1.05, -6, 1, 'Linear')], jitter=3.0)
    for i, (t0, a0, a1) in enumerate([(2.0, -40, 40), (2.7, 45, -45), (3.3, -35, 35)]):
        s.sweep(A, f'Tractor{i}_', 'fx_strike', t0, t0 + 0.55, (0, 300), a0, a1, sc=2.6)
    s.rain(A, 'Zap', 'fx_streak', HITS, [-700, 600, -300, 200, -800, 750, -100, 450, -550, 0], 250, -300, sc=0.5, dur=0.08, rot=0, impact='fx_impact', isc=0.7)
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.5, 0.12, 0.9, 0, 'Out'), (4.15, 0.5, 0.8, 1.25, 0, 'InOut'), (5.0, 0.5, 0.79, 1.0, 0, 'Out')], kicks=[4.15], power=70)
    s.pose(tr, 'Pose', 'pose_finish', 680, [(3.9, 0.5, 0.81, 1, 0, 1, 'Out'), (5.0, 0.5, 0.81, 1.02, 0, 1, 'Linear')])
    for i in range(4):
        s.w(tr, f'Ring{i}', 'fx_ring', 1.4, [(4.15 + i * 0.1, 0.5, 0.86, 0.3, 0, 1, 'Out'), (4.6 + i * 0.1, 0.5, 0.86, 2.4, 0, 0, 'Out')])
    s.meme(F, 4.3, side=-1)


def streamrat(s, R):
    """가로: 침실 → 방송 책상 → 네온 창. ② 침실에서 책상으로 부드럽게 다가가며 채팅 말풍선이 줄줄이 솟고 구독자 숫자 · ③ 화면 속 화면(방송 창) + 네온 하트 낙하 · ④ 비스듬히 돌며 무지개 폭발 + 상패"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.15, 0.55, 1.2, 0, 'Out'), (1.3, 0.5, 0.6, 1.45, 0, 'InOut'), (1.9, 0.52, 0.6, 1.55, 3, 'Linear')], kicks=[1.32])
    s.pose(tr, 'Pose', 'pose_intro', 620, [(0.4, 0.5, 0.7, 1, 0, 1, 'Out'), (1.9, 0.5, 0.7, 1.03, 0, 1, 'Linear')], jitter=2.0)
    s.rise(I, 'Chat', 'prop_float1', [0.6 + 0.1 * i for i in range(10)], [-750, 700, -600, 650, -800, 560, -700, 780, -620, 720], -560, 560, sc=0.35, dur=0.6)
    s.pops(tr, 'Heart', 'prop_pop', [(0.4, 0.5), (0.6, 0.45), (0.45, 0.4), (0.55, 0.55)], 1.3, gap=0.06)
    s.text(I, 'Counter', s.story.get('counter') or '{n}', 76, (0, 380), [(0.95, 0, 380, 0, -4, 0, 'Out'), (1.15, 0, 380, 1, -4, 1, 'Back'), (1.62, 0, 380, 1, -4, 1, 'Linear'), (1.7, 0, 380, 1.3, -4, 1, 'Back')], w=760, counter=(999950, 1000000, 1.1, 1.62))
    s.face(I, 'zoom', 1.66)
    A = s.group('Act', R, 1.9, 3.9)
    b = s.band(A, 'Window', (980, 560), [(1.9, 1400, 260, 0.6, 0, 1, 'Out'), (2.1, 440, 240, 1, 0, 1, 'Back'), (3.9, 440, 240, 1, 0, 1, 'Linear')])
    s.bandpano(b, [(1.9, -s.P(0.5, 0.5)[0], -s.P(0.5, 0.5)[1], 1, 0, 1, 'Out'), (3.9, -s.P(0.8, 0.5)[0], -s.P(0.8, 0.5)[1], 1, 0, 1, 'Linear')])
    s.s(b, 'Pose', 'pose_act', s.hp('pose_act', 520), [(1.9, 0, -600, 0.9, 0, 1, 'Out'), (2.3, 0, 0, 1, 0, 1, 'Back'), (3.9, 0, 10, 1.05, 0, 1, 'Linear')], jitter=3.0)
    s.rain(A, 'Heart', 'fx_strike', HITS, [-850, -400, -650, -150, -900, -300, -700, -50, -500, -800], 520, -250, sc=0.9, dur=0.15, impact='fx_impact', isc=0.8)
    s.shower(A, 'Dono', 'prop_band', 2.1, 3.8, 10, 200, 900, -40, -560, sc=0.3)
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.7, 0.45, 1.4, 16, 'Out'), (4.15, 0.52, 0.6, 1.25, -4, 'Back'), (5.0, 0.52, 0.6, 1.05, 0, 'Out')], kicks=[4.15], power=60)
    s.pose(tr, 'Pose', 'pose_finish', 680, [(3.9, 0.52, 1.1, 1, 0, 1, 'Out'), (4.2, 0.52, 0.69, 1, 0, 1, 'Back'), (5.0, 0.52, 0.69, 1.02, 0, 1, 'Linear')])
    s.burst_ring(F, 'Neon', 'fx_burst', 4.15, 0, 50, n=8, r=800, sc=0.6)
    s.burst_ring(F, 'Like', 'fx_aura', 4.3, 0, 50, n=6, r=500, sc=0.45)
    s.meme(F, 4.35)


def ramjui(s, R):
    """세로: 폭풍 하늘 → 거대 참나무 → 숲 바닥. ② 바닥에서 나무 꼭대기까지 다람쥐를 쫓아 쭉 올라감 (다람쥐도 같이) · ③ 오른쪽으로 기운 띠 + 도토리 벼락이 두 개씩 · 나뭇잎 폭풍이 가로로 · ④ 하늘에서 한 바퀴 돌며(배럴 롤) 숲으로 내리꽂힘"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.5, 0.86, 1.3, 0, 'Out'), (1.4, 0.48, 0.28, 1.15, -5, 'InOut'), (1.9, 0.48, 0.22, 1.25, -6, 'Linear')], kicks=[1.45])
    s.pose(tr, 'Pose', 'pose_intro', 560, [(0.4, 0.5, 0.82, 1, 0, 1, 'Out'), (1.4, 0.48, 0.26, 1, -8, 1, 'InOut'), (1.9, 0.48, 0.22, 1.03, -6, 1, 'Linear')])
    s.w(tr, 'Bolt', 'fx_strike', 2.5, [(1.43, 0.48, 0.08, 1, 0, 0, 'Out'), (1.45, 0.48, 0.08, 1, 0, 1, 'Snap'), (1.7, 0.48, 0.1, 1.1, 0, 0, 'Out')], flicker=0.5)
    s.pops(tr, 'Acorn', 'prop_pop', [(0.3, 0.7), (0.7, 0.6), (0.35, 0.5), (0.66, 0.42), (0.4, 0.32)], 0.6, gap=0.15)
    s.s(I, 'Leaf', 'prop_float1', 0.4, [(0.6, -1200, -300, 1, 0, 1, 'Out'), (1.9, 1200, 400, 1, 720, 1, 'Linear')])
    s.speed(I, 0.5, 1.4, vertical=True)
    s.face(I, 'tilt', 1.66)
    A = s.group('Act', R, 1.9, 3.9)
    b = s.band(A, 'Band', (2700, 440), [(1.9, 1500, 900, 1, 14, 1, 'Out'), (2.1, 0, 240, 1, 14, 1, 'Back'), (3.9, 0, 230, 1, 14, 1, 'Linear')])
    s.bandpano(b, [(1.9, 0, -s.P(0.5, 0.8)[1], 1, -14, 1, 'Out'), (3.9, 0, -s.P(0.5, 0.2)[1], 1, -14, 1, 'InOut')])
    s.s(b, 'Pose', 'pose_act', s.hp('pose_act', 520), [(1.9, 1400, 0, 0.9, 0, 1, 'Out'), (2.3, 200, 0, 1, 0, 1, 'Back'), (3.9, -200, 0, 1.05, 0, 1, 'Linear')], spin=180.0)
    xs = [-700, -500, 300, 500, -200, 0, 650, 850, -850, -650]
    s.rain(A, 'Acorn', 'fx_strike', [HITS[i // 2 * 2] + (i % 2) * 0.04 for i in range(10)], xs, 300, -250, sc=1.0, dur=0.1, impact='fx_impact', isc=0.8)
    s.s(A, 'LeafStorm', 'fx_streak', 2.5, [(2.4, 1600, -200, 1, 180, 0.9, 'Out'), (3.2, -1600, -150, 1, 180, 0.9, 'Linear')], flicker=0.3)
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.48, 0.1, 1.1, -180, 'Out'), (4.2, 0.5, 0.8, 1.3, 0, 'InOut'), (5.0, 0.5, 0.79, 1.02, 0, 'Out')], kicks=[4.2], power=70)
    s.w(tr, 'Pillar', 'fx_strike', 3.0, [(4.0, 0.5, 0.45, 1, 0, 0, 'Out'), (4.02, 0.5, 0.45, 1, 0, 1, 'Snap'), (5.0, 0.5, 0.45, 1, 0, 0.6, 'Linear')], flicker=0.4)
    s.pose(tr, 'Pose', 'pose_finish', 680, [(3.9, 0.5, 0.81, 1, 0, 1, 'Out'), (5.0, 0.5, 0.81, 1.02, 0, 1, 'Linear')])
    s.burst_ring(F, 'Leaf', 'fx_aura', 4.2, 0, -100, n=8, r=800, sc=0.4)
    s.meme(F, 4.3, side=-1)


def jwerry(s, R):
    """가로: 무대 뒤 → 무대 → 객석. ② 무대 뒤에서 무대로 걸어 나가며 스포트라이트들이 한 점에 모임 · 객석에서 장미 · ③ 아래 띠(무대) + 스포트라이트가 위에서 쓸고 장미 소나기 · ④ 객석 시점에서 확 줌 인 + 커튼콜 장미 폭발"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.12, 0.6, 1.4, 0, 'Out'), (1.2, 0.5, 0.62, 1.1, 0, 'InOut'), (1.9, 0.5, 0.64, 1.25, 0, 'Linear')], kicks=[1.25])
    s.pose(tr, 'Pose', 'pose_intro', 620, [(0.4, 0.18, 0.68, 1, 0, 1, 'Out'), (1.2, 0.5, 0.68, 1, 0, 1, 'InOut'), (1.9, 0.5, 0.68, 1.02, 0, 1, 'Linear')])
    for i, (x, a0, a1) in enumerate([(-800, -40, -12), (800, 40, 12), (0, 0, 0)]):
        s.sweep(I, f'Spot{i}_', 'fx_strike', 0.9 + i * 0.1, 1.25, (x, 560), a0, a1, sc=2.2)
    s.shower(I, 'Rose', 'prop_pop', 1.25, 1.85, 10, -800, 800, 600, -300, sc=0.3)
    s.face(I, 'slide', 1.65)
    A = s.group('Act', R, 1.9, 3.9)
    b = s.band(A, 'Band', (2500, 480), [(1.9, 0, -800, 1, -3, 1, 'Out'), (2.1, 0, -250, 1, -3, 1, 'Back'), (3.9, 0, -250, 1, -3, 1, 'Linear')])
    s.bandpano(b, [(1.9, 0, -s.P(0.5, 0.65)[1], 1, 3, 1, 'Out'), (3.9, -400, -s.P(0.5, 0.65)[1], 1, 3, 1, 'Linear')])
    s.s(b, 'Pose', 'pose_act', s.hp('pose_act', 520), [(1.9, 0, -600, 0.8, 0, 1, 'Out'), (2.25, 0, 30, 1, 0, 1, 'Back'), (3.9, 0, 40, 1.05, 0, 1, 'Linear')], spin=60.0)
    for i, t0 in enumerate([2.0, 2.5, 3.0, 3.5]):
        s.sweep(A, f'Spot{i}_', 'fx_strike', t0, t0 + 0.45, ([-700, 700, -300, 300][i], 560), [-35, 35, -20, 20][i], [35, -35, 20, -20][i], sc=2.4)
    s.shower(A, 'Rose', 'fx_impact', 2.0, 3.8, 18, -900, 900, 560, 0, sc=0.3)
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.85, 0.55, 0.85, 0, 'Out'), (4.15, 0.5, 0.6, 1.4, 0, 'In'), (5.0, 0.5, 0.6, 1.25, 0, 'Out')], kicks=[4.15], power=50)
    s.pose(tr, 'Pose', 'pose_finish', 640, [(3.9, 0.5, 0.67, 1, 0, 1, 'Out'), (5.0, 0.5, 0.67, 1.02, 0, 1, 'Linear')])
    s.burst_ring(F, 'Rose', 'fx_aura', 4.15, 0, 0, n=8, r=750, sc=0.5)
    s.s(F, 'Crowd', 'prop_band', 1.4, [(4.2, 0, -800, 1, 0, 1, 'Out'), (4.4, 0, -440, 1, 0, 1, 'Back'), (5.0, 0, -430, 1, 0, 1, 'Linear')], pulse=0.03, pulseHz=4.0)
    s.meme(F, 4.35)


def starchef(s, R):
    """가로: 식료품 창고 → 주방 → 홀. ② 칸마다 멈추는 휙휙 팬 (창고 치즈 → 주방 불 쇼 → 홀) · ③ 평행한 두 사선 띠 + 프라이팬이 쿵쿵 + 접시가 빙글 · ④ 클로슈 로우앵글 밀어 들어가기 + 별 세 개"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.15, 0.6, 1.3, -2, 'Out'), (0.65, 0.15, 0.6, 1.35, -2, 'Linear'), (0.8, 0.5, 0.62, 1.3, 2, 'InOut'), (1.2, 0.5, 0.62, 1.45, 2, 'Linear'), (1.35, 0.85, 0.6, 1.2, -2, 'InOut'), (1.9, 0.85, 0.6, 1.3, -3, 'Linear')], kicks=[0.82, 1.37])
    s.pops(tr, 'Cheese', 'prop_pop', [(0.1, 0.6), (0.2, 0.55)], 0.5)
    s.pose(tr, 'Pose', 'pose_intro', 600, [(0.4, 0.5, 0.68, 1, 0, 1, 'Out'), (1.9, 0.5, 0.68, 1, 0, 1, 'Linear')])
    s.w(tr, 'Fire', 'fx_impact', 1.6, [(0.82, 0.5, 0.5, 0, 0, 0, 'Out'), (0.95, 0.5, 0.45, 1.3, 0, 1, 'Back'), (1.3, 0.5, 0.4, 1.6, 20, 0, 'Out')], spin=40.0)
    s.speed(I, 0.66, 0.8, vertical=False); s.speed(I, 1.21, 1.35, vertical=False)
    s.s(I, 'Plate', 'prop_float1', 0.45, [(1.35, -1200, 300, 1, 0, 1, 'Out'), (1.9, 700, 250, 1, 540, 1, 'Linear')])
    s.face(I, 'full', 1.68)
    A = s.group('Act', R, 1.9, 3.9)
    b1 = s.band(A, 'Band1', (2700, 300), [(1.9, -1500, 600, 1, -10, 1, 'Out'), (2.08, 0, 330, 1, -10, 1, 'Back'), (3.9, 0, 330, 1, -10, 1, 'Linear')])
    b2 = s.band(A, 'Band2', (2700, 220), [(1.9, 1500, 200, 1, -10, 1, 'Out'), (2.15, 0, 50, 1, -10, 1, 'Back'), (3.9, 0, 50, 1, -10, 1, 'Linear')])
    s.bandpano(b1, [(1.9, 0, -s.P(0.5, 0.45)[1], 1, 10, 1, 'Out'), (3.9, -800, -s.P(0.5, 0.45)[1], 1, 10, 1, 'Linear')])
    s.bandpano(b2, [(1.9, -800, -s.P(0.5, 0.75)[1], 1, 10, 1, 'Out'), (3.9, 800, -s.P(0.5, 0.75)[1], 1, 10, 1, 'Linear')])
    s.s(b1, 'Pose', 'pose_act', s.hp('pose_act', 420), [(1.9, -1400, -100, 0.9, 0, 1, 'Out'), (2.3, -100, 0, 1, 0, 1, 'Back'), (3.9, 200, 0, 1.05, 0, 1, 'Linear')], jitter=3.0)
    s.s(b2, 'Plates', 'prop_float1', 0.3, [(2.1, 1400, 0, 1, 0, 1, 'Out'), (3.8, -1400, 0, 1, 900, 1, 'Linear')])
    s.stomp(A, 'Pan', 'fx_strike', HITS, [(-750, -300), (600, -330), (-300, -280), (850, -340), (-850, -320), (250, -300), (-500, -340), (700, -280), (0, -330), (-150, -300)], sc=0.8, impact='fx_impact')
    s.spiral(A, 'Spin', 'fx_ring', 2.2, 3.7, 0, -250, n=5, r0=800, r1=200, sc=0.3, turns=1.2)
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.82, 0.78, 1.0, 6, 'Out'), (4.2, 0.82, 0.62, 1.6, 0, 'InOut'), (5.0, 0.82, 0.62, 1.7, 0, 'Linear')], kicks=[4.2], power=50)
    s.pose(tr, 'Pose', 'pose_finish', 640, [(3.9, 0.82, 0.68, 1, 0, 1, 'Out'), (5.0, 0.82, 0.68, 1.02, 0, 1, 'Linear')])
    for i, x in enumerate([-350, 0, 350]):
        s.s(F, f'Star{i}', 'fx_burst', 0.6, [(4.2 + i * 0.12, x, 330, 0, 0, 0, 'Out'), (4.22 + i * 0.12, x, 330, 1.2, 0, 1, 'Back'), (5.0, x, 330, 1.0, 40, 0.9, 'Linear')], spin=30.0)
    s.meme(F, 4.35, side=-1)


def jjdetective(s, R):
    """가로: 사무실 → 비 오는 거리 → 범행 골목. ② 돋보기가 훑는 대로 천천히 밀고 나가며 발자국이 차례로 · ③ 왼쪽 세로 띠 + 축구공이 통통 튀며 게임 화면을 가로지름 · ④ 손가락 끝으로 급줌(돌리 줌) + 하얀 번쩍"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.15, 0.62, 1.5, 0, 'Out'), (1.6, 0.8, 0.66, 1.3, 0, 'InOut'), (1.9, 0.82, 0.66, 1.45, 3, 'Linear')], kicks=[1.62])
    s.trail(tr, 'Foot', 'prop_fg', [(0.18 + 0.065 * i, 0.9 - (i % 2) * 0.03) for i in range(10)], 0.45, gap=0.11, sc=0.25)
    s.w(tr, 'Lens', 'fx_ring', 0.9, [(0.4, 0.15, 0.7, 1, 0, 0.9, 'Out'), (1.6, 0.8, 0.75, 1, 0, 0.9, 'InOut'), (1.7, 0.8, 0.75, 1.4, 0, 0, 'Out')], pulse=0.05)
    s.pose(tr, 'Pose', 'pose_intro', 600, [(0.4, 0.82, 0.69, 1, 0, 1, 'Out'), (1.9, 0.82, 0.69, 1, 0, 1, 'Linear')])
    s.s(I, 'Spot', 'fx_strike', 2.0, [(1.6, 300, 300, 1, 0, 0, 'Out'), (1.62, 300, 300, 1, 0, 0.9, 'Snap'), (1.9, 300, 300, 1, 0, 0.9, 'Linear')], flicker=0.15)
    s.face(I, 'tilt', 1.7)
    A = s.group('Act', R, 1.9, 3.9)
    b = s.band(A, 'Band', (720, 1300), [(1.9, -1300, 0, 1, 0, 1, 'Out'), (2.08, -560, 0, 1, 0, 1, 'Back'), (3.9, -560, 0, 1, 0, 1, 'Linear')])
    s.bandpano(b, [(1.9, -s.P(0.4, 0.5)[0], -s.P(0.4, 0.5)[1], 1, 0, 1, 'Out'), (3.9, -s.P(0.6, 0.5)[0], -s.P(0.6, 0.5)[1], 1, 0, 1, 'Linear')])
    s.s(b, 'Pose', 'pose_act', s.hp('pose_act', 640), [(1.9, 0, -900, 0.8, 0, 1, 'Out'), (2.25, 0, 0, 1, 0, 1, 'Back'), (3.9, 0, 30, 1.05, 0, 1, 'Linear')], jitter=3.0)
    for i, (t0, y) in enumerate([(2.0, -250), (2.6, -150), (3.2, -320)]):
        s.bounce(A, f'Ball{i}', 'fx_strike', t0, -200, 1000, y, height=500, hops=3, dur=0.6, sc=0.7, impact='fx_impact')
    s.rain(A, 'Hit', 'fx_burst', HITS, [100, 600, 300, 800, 200, 700, 400, 900, 150, 550], 300, -280, sc=0.4, dur=0.06, impact='fx_ring', isc=0.6)
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.82, 0.62, 0.9, 0, 'Out'), (4.12, 0.78, 0.56, 2.2, -4, 'In'), (4.25, 0.79, 0.6, 1.3, 2, 'Back'), (5.0, 0.8, 0.62, 1.15, 0, 'Out')], kicks=[4.12], power=70)
    s.pose(tr, 'Pose', 'pose_finish', 680, [(3.9, 0.82, 0.68, 1, 0, 1, 'Out'), (5.0, 0.82, 0.68, 1.02, 0, 1, 'Linear')])
    s.s(F, 'Flash', 'fx_burst', 4.0, [(4.1, 0, 0, 0, 0, 0, 'Out'), (4.12, 0, 0, 1, 0, 1, 'Snap'), (4.35, 0, 0, 1.5, 20, 0, 'Out')])
    s.meme(F, 4.35)


def fondue(s, R):
    """세로 (+ 치즈 홍수 판): 천장 → 거대 퐁듀 냄비 → 바닥. ② 냄비 가까이서 거품이 퐁퐁 → 넘쳐 홍수 판으로 바뀌며 바닥까지 · ③ 아래 띠(서핑) + 거대 치즈 파도가 아래에서 차오름 · ④ 늘어난 치즈 포즈에서 아주 크게 물러남"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.5, 0.45, 1.6, 0, 'Out'), (1.0, 0.5, 0.48, 1.5, 3, 'Linear'), (1.4, 0.5, 0.8, 1.1, -3, 'InOut'), (1.9, 0.5, 0.8, 1.25, -4, 'Linear')], kicks=[1.02], swap=(1.0, 1.35))
    s.rise(I, 'Bubble', 'fx_impact', [0.5 + 0.08 * i for i in range(7)], [-500, 300, -150, 450, -350, 100, 550], -200, 200, sc=0.4, dur=0.3)
    s.pose(tr, 'Pose', 'pose_intro', 600, [(0.4, 0.5, 0.84, 1, 0, 1, 'Out'), (1.9, 0.5, 0.84, 1.03, 0, 1, 'Linear')])
    s.rain(I, 'Drip', 'fx_strike', [1.05, 1.12, 1.2, 1.28], [-600, 400, -200, 700], 560, -100, sc=0.8, dur=0.2)
    s.face(I, 'zoom', 1.66)
    A = s.group('Act', R, 1.9, 3.9)
    b = s.band(A, 'Band', (2500, 460), [(1.9, 0, -800, 1, 3, 1, 'Out'), (2.1, 0, -260, 1, 3, 1, 'Back'), (3.9, 0, -250, 1, 3, 1, 'Linear')])
    s.bandpano(b, [(1.9, 600, -s.P(0.5, 0.75)[1], 1, -3, 1, 'Out'), (3.9, -600, -s.P(0.5, 0.75)[1], 1, -3, 1, 'Linear')])
    s.s(b, 'Pose', 'pose_act', s.hp('pose_act', 520), [(1.9, -1400, -60, 0.9, -6, 1, 'Out'), (2.3, -200, 30, 1, 4, 1, 'Back'), (3.0, 100, 50, 1, -4, 1, 'InOut'), (3.9, 300, 30, 1.05, 4, 1, 'InOut')])
    s.s(A, 'Wave', 'fx_streak', 4.0, [(2.0, -1600, -900, 1, 0, 0.9, 'Out'), (2.9, 0, -400, 1.1, 0, 0.9, 'InOut'), (3.8, 1600, -300, 1, 0, 0.9, 'InOut')])
    s.rain(A, 'Drop', 'fx_strike', HITS, [-800, 200, -400, 650, -650, 400, -100, 800, -900, 0], 560, 0, sc=0.9, dur=0.14, impact='fx_impact', isc=0.8)
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.5, 0.74, 2.5, 6, 'Out'), (4.2, 0.5, 0.7, 0.85, 0, 'InOut'), (5.0, 0.5, 0.7, 0.8, 0, 'Out')], kicks=[4.2], power=50, pano='pano_alt')
    s.pose(tr, 'Pose', 'pose_finish', 700, [(3.9, 0.5, 0.79, 1, 0, 1, 'Out'), (5.0, 0.5, 0.79, 1.02, 0, 1, 'Linear')])
    s.burst_ring(F, 'Splash', 'fx_impact', 4.2, 0, 0, n=8, r=700, sc=0.7)
    s.meme(F, 4.3)


def bomber(s, R):
    """세로: 붉은 하늘 마법진 → 계곡 → 절벽. ② 절벽에서 시작해 마법진이 하나씩 켜지는 대로 하늘로 천천히 올라감 · ③ 위쪽 띠 + 줄지어 떨어지는 폭탄(융단 폭격) · ④ 하늘 마법진 → 거대 폭발로 화면이 하얗게 → 뒤처리: 엎어진 폭탄마"""
    I = s.group('Intro', R, 0.4, 1.9)
    tr = s.cam(I, [(0.4, 0.5, 0.84, 1.3, 0, 'Out'), (1.7, 0.5, 0.2, 1.05, 0, 'InOut'), (1.9, 0.5, 0.18, 1.1, 0, 'Linear')], kicks=[0.8, 1.05, 1.3, 1.55])
    s.pose(tr, 'Pose', 'pose_intro', 620, [(0.4, 0.5, 0.82, 1, 0, 1, 'Out'), (1.9, 0.5, 0.82, 1, 0, 1, 'Linear')], jitter=2.0)
    for i, (t, y) in enumerate([(0.8, 0.62), (1.05, 0.46), (1.3, 0.32), (1.55, 0.18)]):
        s.w(tr, f'Circle{i}', 'fx_ring', 2.4 - i * 0.2, [(t - 0.01, 0.5, y, 0, 0, 0, 'Out'), (t, 0.5, y, 0.3, 0, 1, 'Snap'), (t + 0.2, 0.5, y, 1.2, 0, 1, 'Back'), (1.9, 0.5, y, 1.25, 0, 1, 'Linear')], spin=40.0 * (1 if i % 2 else -1))
    s.converge(tr, 'Ember', 'fx_streak', 1.2, [(0.1, 0.4), (0.9, 0.35), (0.2, 0.15), (0.8, 0.1)], (0.5, 0.18))
    s.face(I, 'full', 1.7)
    A = s.group('Act', R, 1.9, 3.9)
    b = s.band(A, 'Band', (2500, 420), [(1.9, 0, 800, 1, -4, 1, 'Out'), (2.1, 0, 300, 1, -4, 1, 'Back'), (3.9, 0, 300, 1, -4, 1, 'Linear')])
    s.bandpano(b, [(1.9, 0, -s.P(0.5, 0.15)[1], 1, 4, 1, 'Out'), (3.9, 0, -s.P(0.5, 0.25)[1], 1, 4, 1, 'Linear')])
    s.s(b, 'Pose', 'pose_act', s.hp('pose_act', 500), [(1.9, 0, -500, 0.8, 0, 1, 'Out'), (2.25, 0, 20, 1, 0, 1, 'Back'), (3.9, 0, 30, 1.05, 0, 1, 'Linear')], pulse=0.03)
    s.rain(A, 'Bomb', 'fx_strike', HITS, [-900 + 200 * i for i in range(10)], 200, -260, sc=0.9, dur=0.16, impact='fx_impact', isc=1.0, ring='fx_ring')
    F = s.group('Finish', R, 3.9)
    tr = s.cam(F, [(3.9, 0.5, 0.18, 1.2, 0, 'Out'), (4.15, 0.5, 0.2, 1.0, 0, 'Linear'), (4.2, 0.5, 0.84, 1.3, 3, 'Snap'), (5.0, 0.5, 0.84, 1.2, 1, 'Linear')], kicks=[4.05, 4.2], power=80)
    s.s(F, 'Boom', 'fx_impact', 3.0, [(3.95, 0, 300, 0, 0, 0, 'Out'), (4.0, 0, 300, 0.5, 0, 1, 'Snap'), (4.18, 0, 200, 3.0, 20, 1, 'Out'), (4.2, 0, 200, 3.0, 20, 0, 'Snap')])
    s.s(F, 'White', 'fx_burst', 6.0, [(4.12, 0, 0, 0.5, 0, 0, 'Out'), (4.18, 0, 0, 1.5, 0, 1, 'Snap'), (4.4, 0, 0, 2, 0, 0, 'Out')])
    s.pose(tr, 'Pose', 'pose_finish', 420, [(4.2, 0.5, 0.86, 1, 0, 1, 'Out'), (5.0, 0.5, 0.86, 1, 0, 1, 'Linear')], jitter=1.0)
    s.w(tr, 'Smoke', 'prop_fg', 0.6, [(4.2, 0.5, 0.82, 0.8, 0, 1, 'Out'), (5.0, 0.5, 0.78, 1.2, 0, 0.5, 'Linear')])
    s.meme(F, 4.35)


def icequeen(s, R):
    """세로 (+ 얼어붙은 판): 오로라 → 얼음성 → 언 호수. (2026-10-19 v3, 사용자: 렛잇고 필살기의 '얼음성 → 하늘에서 눈보라 광역'을 초필살기로 · 발레는 필살기로)
    플랫 배경 · 큰 파노라마 + 카메라 무빙 · 쥐 = 리깅 파츠 바탕 동작 프레임(anim_*) 셀 애니 · 이펙트도 플랫. 컷 6개:
    ① 0.4 오로라 하늘 휙 틸트 ② 0.85 언 호수: 웅크렸다 두 앞발을 하늘로 (소환) ③ 1.4 얼굴 클로즈업
    ④ 1.9 하늘에서 얼음성이 호수로 쿵 (세상이 얼어붙음) ⑤ 2.5 성에서 눈보라가 하늘로 솟구침 (위로 틸트) ⑥ 3.0 하늘 가득 눈보라가 몰려와 하얗게
    → 3.4 컷신 걷힘 → 게임 화면 전체에 하늘에서 눈보라 3초 (다단 10타 · 대상마다 피해 숫자 · 화면 안 생명체 꽁꽁 = SuperUltStyle.worldPhase)"""
    END = NO_FINISH['icequeen']                     # 컷신 끝 (SuperUltStyle.actEnd)

    def cut_flash(g, t0, a=0.55):                   # 컷이 바뀌는 순간 하얗게 번쩍
        s.s(g, 'CutFlash', 'fx_burst', 6.0, [(t0, 0, 0, 1.0, 0, a, 'Snap'), (t0 + 0.09, 0, 0, 1.3, 0, 0, 'Out')])

    # ① 오로라 하늘 → 아래로 휙
    g = s.group('Cut1', R, 0.4, 0.85)
    s.cam(g, [(0.4, 0.5, 0.08, 1.25, 4, 'Out'), (0.85, 0.5, 0.33, 1.1, -2, 'In')], kicks=[0.4])
    s.speed(g, 0.5, 0.85, vertical=True)
    s.spiral(g, 'Flake', 'prop_pop', 0.45, 0.85, 0, 0, n=6, r0=900, r1=300, sc=0.22, turns=0.8)
    # ② 언 호수: 웅크렸다 → 두 앞발을 하늘로 (얼음성 소환)
    g = s.group('Cut2', R, 0.85, 1.4)
    tr = s.cam(g, [(0.85, 0.45, 0.86, 1.7, -4, 'Out'), (1.4, 0.48, 0.84, 1.9, 2, 'Linear')], kicks=[1.0], power=30)
    s.cel(tr, 'Queen', [(0.85, 'anim_land_2'), (1.0, 'anim_dash_2')],
          [(0.85, 0.47, 0.90, 0.95, 0, 1, 'Out'), (1.0, 0.47, 0.90, 1.1, 0, 1, 'Back'), (1.4, 0.47, 0.90, 1.14, 0, 1, 'Linear')], height=420)
    s.s(g, 'Frost', 'fx_aura', 4.0, [(1.0, 0, 0, 0.5, 0, 0, 'Out'), (1.1, 0, 0, 1.0, 0, 0.7, 'Back'), (1.4, 0, 0, 1.2, 0, 0.5, 'Linear')])
    s.rise(g, 'Up', 'prop_pop', [1.02 + 0.04 * i for i in range(8)], [-700, -500, -300, -100, 100, 300, 500, 700], -500, 700, sc=0.22, dur=0.3)
    cut_flash(g, 0.85)
    # ③ 얼굴 클로즈업 (배경 + 쥐)
    g = s.group('Cut3', R, 1.4, 1.9)
    s.face(g, 'zoom', 1.4, 1.9, bg='face_bg')
    s.spiral(g, 'Flake', 'prop_pop', 1.45, 1.9, 0, 0, n=8, r0=900, r1=400, sc=0.3, turns=0.6)
    cut_flash(g, 1.4)
    # ④ 하늘에서 얼음성이 호수로 쿵 → 세상이 얼어붙음 (파노라마가 얼어붙은 판으로)
    LAND = 2.22
    g = s.group('Cut4', R, 1.9, 2.5)
    # 카메라 = 파노라마 아래 빈 호수만 (파노라마 속 성이 안 보이게) → 떨어진 성이 유일한 성
    tr = s.cam(g, [(1.9, 0.5, 0.89, 1.25, 0, 'Out'), (2.5, 0.5, 0.895, 1.32, 0, 'Linear')], kicks=[LAND], power=60, rot=3, ktime=0.35, swap=(LAND, LAND + 0.12))
    s.w(tr, 'Castle', 'prop_castle', 0.72, [(1.9, 0.5, 0.62, 0.9, 0, 1, 'Out'), (LAND, 0.5, 0.894, 1.0, 0, 1, 'In'), (LAND + 0.06, 0.5, 0.894, 1.06, 0, 1, 'Out'), (2.5, 0.5, 0.894, 1.0, 0, 1, 'Back')])
    s.speed(g, 1.92, LAND, vertical=True)
    s.cel(tr, 'Queen', [(1.9, 'anim_dash_2'), (LAND + 0.05, 'anim_land_3')],
          [(1.9, 0.83, 0.985, 1, 0, 1, 'Out'), (2.5, 0.83, 0.985, 1.03, 0, 1, 'Linear')], height=260)
    s.s(g, 'Slam', 'fx_burst', 3.2, [(LAND - 0.01, 0, -420, 0, 0, 0, 'Out'), (LAND, 0, -420, 1.0, 0, 0.9, 'Snap'), (LAND + 0.35, 0, -420, 1.6, 0, 0, 'Out')])
    s.s(g, 'SlamRing', 'fx_ring', 2.4, [(LAND, 0, -440, 0.3, 0, 0.9, 'Snap'), (LAND + 0.4, 0, -440, 1.8, 0, 0, 'Out')])
    s.burst_ring(g, 'Chunk', 'fx_shard1', LAND, 0, -420, n=10, r=1000, sc=0.6, dur=0.35)
    cut_flash(g, 1.9)
    # ⑤ 성에서 눈보라가 하늘로 솟구침 (카메라가 위로 틸트)
    g = s.group('Cut5', R, 2.5, 3.0)
    tr = s.cam(g, [(2.5, 0.5, 0.58, 1.5, -3, 'Out'), (3.0, 0.5, 0.2, 1.25, 3, 'InOut')], kicks=[2.5], power=24, pano='pano_alt')     # 파노라마 속 성(얼어붙은 판)에서 하늘로
    for i in range(6):
        s.w(tr, f'Storm{i}', 'fx_streak', 1.6, [(2.5 + 0.05 * i, 0.38 + 0.05 * i, 0.62, 0.6, 90, 0, 'Out'), (2.55 + 0.05 * i, 0.38 + 0.05 * i, 0.55, 1.0, 90, 0.9, 'Linear'), (3.0, 0.32 + 0.07 * i, 0.12, 1.4, 90, 0, 'Linear')])
    s.rise(g, 'Up', 'prop_pop', [2.52 + 0.035 * i for i in range(12)], [s.rnd.uniform(-850, 850) for _ in range(12)], -600, 800, sc=0.25, dur=0.32)
    s.speed(g, 2.5, 3.0, vertical=True)
    cut_flash(g, 2.5)
    # ⑥ 하늘에서 우박 섞인 눈보라가 쏟아져 하얗게 (→ 컷신 걷히면 게임 화면 우박)
    g = s.group('Cut6', R, 3.0)
    tr = s.cam(g, [(3.0, 0.5, 0.12, 1.3, 0, 'Out'), (END + 0.4, 0.5, 0.10, 1.9, 0, 'In')], kicks=[3.0, 3.2], power=20, pano='pano_alt')
    for i in range(22):                             # 우박: 얼음 알갱이가 위에서 비스듬히 빠르게 내리꽂힘
        x, t0, sc = s.rnd.uniform(-1000, 1100), 3.0 + s.rnd.uniform(0, 0.38), s.rnd.uniform(0.35, 0.8)
        spr = ('fx_shard2', 'fx_shard3', 'fx_shard3')[i % 3]; rr = s.rnd.uniform(0, 360)
        s.s(g, f'Hail{i}', spr, sc, [(t0, x, 700, 1, rr, 0, 'Snap'), (t0 + 0.01, x, 700, 1, rr, 1, 'Snap'), (t0 + 0.22, x - 260, -620, 1, rr + 200, 1, 'In'), (t0 + 0.24, x - 270, -640, 1, rr + 210, 0, 'Linear')])
    for i in range(5):
        s.s(g, f'HailStreak{i}', 'fx_streak', 1.4, [(3.0 + 0.06 * i, -500 + 260 * i, 900, 1, -72, 0, 'Out'), (3.04 + 0.06 * i, -560 + 260 * i, 600, 1, -72, 0.8, 'Linear'), (3.3 + 0.06 * i, -800 + 260 * i, -700, 1, -72, 0, 'In')])
    s.s(g, 'WhiteOut', 'fx_burst', 8.0, [(3.15, 0, 0, 0.6, 0, 0, 'Out'), (END, 0, 0, 1.4, 0, 0.95, 'In'), (END + 0.4, 0, 0, 1.6, 0, 0.95, 'Linear')])
    cut_flash(g, 3.0)
    # 밈 (⑤~⑥ 화면 위)
    G = s.group('Top', R, 2.5)
    s.meme(G, 2.55, side=-1, y=440)
    return None                                     # 피해 숫자는 게임 화면 단계에서 대상마다 (worldPhase)


STORIES = {f.__name__: f for f in (parkrat, emperor, pharaoh, knight, sultan, alien, streamrat, ramjui, jwerry, starchef, jjdetective, fondue, bomber, icequeen)}
