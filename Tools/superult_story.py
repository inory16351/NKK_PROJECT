"""초필살기 레이어 컷신 — 쥐마다 따로 짠 연출 (사용자 2026-10-18: 셀 애니처럼 배경 + 움직이는 물체 합성 · 그림체만 통일, 카메라 무빙 · 액션 · 이펙트는 쥐마다 개별).
gen_superult_scene.py 가 불러서 C# 줄을 만듦. 좌표: 파노라마는 비율 (x 0~1 왼→오, y 0~1 위→아래), 화면은 px (가운데 0, 위 +, 1920×1080).
공통 시간: ① 0~0.4 (매니저) · ② 0.4~1.9 · ③ 1.9~3.9 (다단 HITS 시각) · ④ 3.9~4.6 · ⑤ ~5.0
도구: Story.cam (가상 카메라 = Rig 줌·기울기·충격 > Track 위치) · Story.w (파노라마 위) · Story.s (화면 위) · Story.band (마스크 띠)
      패턴: rain · rise · sweep · shower · spiral · bounce · burst_ring · stomp · converge · pops · trail"""
import json, math, os, random
from PIL import Image

HITS = [2.0 + 0.2 * i for i in range(10)]
NO_FINISH = {'maid': 3.45, 'icequeen': 3.4, 'knight': 3.95, 'pharaoh': 3.75, 'bomber': 3.05, 'streamrat': 3.6, 'ramjui': 5.61, 'fondue': 5.35, 'starchef': 6.41, 'sultan': 4.15, 'emperor': 5.25}          # ④ 피니시 컷 없는 쥐: ③ 끝(마지막 일격 · 또는 컷신 끝) 시각 = SuperUltStyle.actEnd (2026-10-19 사용자)
WORLD = {'maid': dict(cutFade=0.3, worldTime=2.4, blizzardCount=1, blizzardGap=0.1, hailCount=2, playRate=0.75, worldWind=(-260.0, -1500.0), worldDotSize=(34.0, 62.0)),     # 메이드 쥐: 시간이 다시 흐르면 게임 화면에 칼 비 (2026-10-20)
         'emperor': dict(cutFade=0.3, worldTime=1.5, blizzardCount=0, hailCount=0, playRate=1.0),     # 쥐 황제 v2 정면: 게임 화면 연출 없음 · 컷신 마지막 탄흔(Hold)만 1.5초 남았다 사라짐 (2026-10-20 사용자)
         'icequeen': dict(cutFade=0.4, worldTime=3.0, freezeAll=1.5, blizzardCount=1, hailCount=3, playRate=0.75),
         'pharaoh': dict(cutFade=0.4, worldTime=2.2, blizzardCount=0, hailCount=0, playRate=0.75),     # 미라 쥐들이 왼→오로 달려 지나감 (2026-10-20 사용자: 모래바람 대신)
         'bomber': dict(cutFade=0.15, worldTime=2.2, blizzardCount=0, hailCount=0, playRate=0.75),     # 레이저 컷신 뒤 게임 맵 위 거대 폭발 하나 (2026-10-20 사용자)
         'streamrat': dict(cutFade=0.3, worldTime=2.0, blizzardGap=0.14, blizzardCount=1, hailCount=1, playRate=0.75, worldWind=(0.0, -380.0), worldDotSize=(40.0, 70.0)),     # 하트 · 색종이가 쏟아지며 대상마다 하트 팡 (2026-10-20)
         'ramjui': dict(cutFade=0.3, worldTime=1.6, blizzardCount=0, hailCount=0, playRate=0.75),     # 람쥐썬더 밈 몽타주 뒤 게임 화면: 대상마다 번개 (2026-10-20 초안)
         'fondue': dict(cutFade=0.3, worldTime=2.2, blizzardCount=1, hailCount=3, playRate=1.0, worldWind=(0.0, -420.0), worldDotSize=(26.0, 52.0)),     # 치즈 퐁듀 v2: 치즈 벽이 걷히면 게임 화면에 치즈가 쏟아져 철퍽 (2026-10-20 사용자: 아이스 퀸식)
         'starchef': dict(cutFade=0.3, worldTime=1.6, blizzardCount=0, hailCount=0, playRate=1.0),     # 슈퍼 요리사 v2: 브이 컷 뒤 게임 화면 = 대상마다 머리카락 · 기절 별 (2026-10-20)
         'sultan': dict(cutFade=0.35, worldTime=4.0, blizzardCount=1, hailCount=2, playRate=1.0, worldWind=(-90.0, -950.0), worldDotSize=(30.0, 62.0)),     # 술탄 v2: 컷신 금화 비가 그대로 게임 화면으로 이어져 4초 동안 쏟아지고 대상마다 짤랑 (2026-10-20 사용자 3차)
         'knight': dict(cutFade=0.4, worldTime=1.0, blizzardCount=0, hailCount=0, playRate=0.75, worldWind=(60.0, 260.0), worldDotSize=(16.0, 40.0))}
NO_TEXT = {'knight', 'starchef', 'fondue', 'emperor'}       # 컷신에 글(제목 띠 · 자막 · 히트 수 · 밈 글)을 안 띄우는 쥐 (2026-10-19 사용자)
KNIGHT_LINE = -40          # 기사 칼 각도 (도, UI 반시계 +): 후경 기사 칼날을 잰 값 — 컷신 갈라짐 · 땅 균열 · 게임 화면 파동 전부 이 각도 (2026-10-19 사용자)
IMAGE_FX = {'maid': dict(hailFall=['fx_knife_fallr', 'fx_knife_fallr', 'fx_knife_fall2r'], hailChunk=['fx_glint'], hailSplash=['fx_hit1', 'fx_hit2', 'fx_hit3'], splashSize=120.0, splashHitSize=220.0, splashFinishSize=420.0, worldSnowScale=0.3),     # 칼 그림은 칼끝이 오른쪽(=떨어지는 방향)으로 돌린 사본 *r (slice_superult ROT_RIGHT)
            'emperor': dict(hailSplash=[], worldSnowOff=True, holdGroup='"Hold"', holdFade=0.45),     # 탄착 그림 없음 = 게임 화면 이펙트 없음 · 탄흔 묶음을 게임 화면 위에 남김 (SuperUltStyle.holdGroup)
            'pharaoh': dict(hailSplash=['fx_hit1', 'fx_hit2', 'fx_hit3'], splashHitSize=240.0, splashFinishSize=420.0, worldSnowOff=True,
                            runFrames=['anim_mside_1', 'anim_mside_2', 'anim_mside_3'], runGap=0.11, runCount=3, runHeight=(160.0, 160.0), runSpeed=(1500.0, 2100.0), runFrame=0.07),     # 파라오 v2: 게임 화면 = 미라 돌격 · 태클 충돌 (2026-10-20 사용자: 달리는 미라 크기 균등 · 맨 끝 정면 군단 뺌)
            'bomber': dict(hailSplash=[], worldSnowOff=True, blastBeam='fx_beam', blastBeamWidth=360.0, blastBeamTime=0.12, blastFrames=['fx_boom1', 'fx_boom2', 'fx_boom3'], blastSize=3400.0, blastFrame=0.24, mushFrames=[],
                           blastFlash='fx_redflash', blastRing='fx_shockring', blastDebris='fx_debris', blastDebrisCount=16, blastSmoke='fx_smoke', blastSmokeSize=2400.0),     # 버섯구름 없이 맵 전체를 초토화하는 거대 폭발 하나 (2026-10-20 사용자)
            'streamrat': dict(hailFall=['fx_heart', 'fx_confetti', 'fx_heart'], hailChunk=['fx_note', 'fx_coin'], hailSplash=['fx_pop1', 'fx_pop2', 'fx_pop3'], splashSize=200.0, splashHitSize=340.0, splashFinishSize=640.0, buffAll=8.0, worldSnowScale=0.3),     # 마무리에 모든 쥐 광란 8초 (사용자: 파괴 + 모든 쥐 버프)
            'ramjui': dict(hailFall=['fx_bolt', 'fx_forkbolt', 'fx_bolt'], hailChunk=['fx_sparks'], hailSplash=['fx_land1', 'fx_land2', 'fx_land3'], splashHitSize=260.0, splashFinishSize=520.0, worldSnowOff=True),     # 람쥐 v2: 게임 화면 = 번개가 대상마다 내리꽂힘
            'fondue': dict(hailFall=['fx_drop', 'fx_drop', 'fx_wedge'], hailChunk=['fx_wedge', 'fx_drops'], hailSplash=['fx_splat1', 'fx_splat2', 'fx_splat3'], splashSize=150.0, splashHitSize=280.0, splashFinishSize=600.0, worldSnowScale=0.4),
            'starchef': dict(hailFall=['fx_tuft', 'fx_strands', 'fx_tuft'], hailChunk=['fx_strands'], hailSplash=['fx_twinkle', 'fx_dizzy', 'fx_dizzy'], splashHitSize=220.0, splashFinishSize=420.0, worldSnowOff=True),
            'sultan': dict(hailFall=['fx_coin', 'fx_coinfall', 'fx_coin2'], hailChunk=['fx_coin2', 'fx_coin'], hailSplash=['fx_clink1', 'fx_clink2', 'fx_clink3'], splashSize=150.0, splashHitSize=280.0, splashFinishSize=600.0, worldSnowScale=1.4, worldSnowFade=1.2),
            'icequeen': dict(hailFall=['fx_hail_fall', 'fx_hail_fall', 'fx_hail_ball'], hailChunk=['fx_hail_chunk', 'fx_hail_ball'],
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
    def cam(self, parent, path, kicks=(), power=36, rot=2.5, ktime=0.28, pano=None, swap=None, pano_alpha=None):
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
            self.layer(tr, 'Pano', pano if (pano != 'pano_alt' or self.has_alt) else 'pano_on', self.PS, [(t, 0, 0, 1, 0, a, 'Linear') for t, a in (pano_alpha or [(0, 1)])])     # pano_alpha = [(t, 투명도)] (치즈 퐁듀: 줌 아웃 중 롱샷 판으로 넘김)
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

    def text(self, parent, name, txt, size, pos, ks, w=1000, back=True, counter=None, color=(1, 0.95, 0.6)):
        var = self.v('t')
        self(f'{{ var m = G("{name}", {parent}, null); var mrt = (RectTransform)m.transform; mrt.anchorMin = new Vector2(0.5f, 0.5f); mrt.anchorMax = new Vector2(0.5f, 0.5f); mrt.sizeDelta = new Vector2({w}, 90);')
        if back:
            self(f'  var bk = Go("Back", m.transform).AddComponent<UnityEngine.UI.Image>(); bk.raycastTarget = false; bk.color = new Color(0.08f, 0.06f, 0.12f, 0.85f); bk.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI_Kit/ui_bar_white.png"); bk.type = UnityEngine.UI.Image.Type.Sliced; ((RectTransform)bk.transform).sizeDelta = new Vector2({w - 20}, {int(size * 1.7)});')
        self(f'  var mt = T("Text", m.transform, "{txt}", {size}, new Color({f(color[0])}, {f(color[1])}, {f(color[2])}, 1f)); ((RectTransform)mt.transform).sizeDelta = new Vector2({w}, {int(size * 1.7)});')
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
    """총병 앞으로 · 장전 · 격발 — 정면 일제 사격 (2026-10-20 v2, 사용자: 에픽세븐 릴리아스 3스킬 참고 · 총 든 쥐 병사를 잔뜩 소환 → 일제히 난사 ·
    2차: 측면보다 일제히 '정면'으로 발사 · 총알 속도감이 드러나는 카메라 무빙). 띠 · 마무리 컷 없음 · 플랫 · 글 없음 · 1배속 — 컷 6개:
    ① 0.4 황제가 금빛 칼을 높이 듦 ② 1.05 '총병 앞으로': 정면 들판에 병사 세 줄 소환 (뒤 줄부터 · 카메라가 물러남)
    ③ 1.95 '장전': 공이치기 철컥 → 총구가 카메라를 겨눔 (총구로 밀고 들어감) ④ 2.75 '격발': 황제가 칼끝을 카메라로 내지름
    ⑤ 3.05 정면 일제 사격 4번: 쏠 때마다 카메라가 쾅 밀고 들어감 · 총알이 소실점에서 카메라 쪽으로 커지며 날아옴 · 마지막엔 카메라가 대열로 돌진하고 총알이 화면 가장자리로 스쳐 지나감
    ⑥ 4.45 총알이 화면(렌즈)에 박혀 탄흔이 화면을 덮음 → 컷신 걷힘 → 게임 화면 총알 (worldPhase · imageFx · worldWind)"""
    END = NO_FINISH['emperor']
    fa = lambda xs: ', '.join(f'{v:.2f}f' for v in xs)
    K = 1920 * 1.04 / 1536                                # 한 장 컷 배율 (화면을 덮음)
    at = lambda rx, ry, z=1.0: ((rx - 0.5) * 1536 * K * z, (0.5 - ry) * 1024 * K * z)
    VP = (0.0, (0.5 - FRONT_HORIZON) * 1024 * K)          # 소실점 (정면 들판 지평선 가운데, 화면 px)

    def cut_flash(g, t0, a=0.5):
        s.s(g, 'CutFlash', 'fx_summon', 9.0, [(t0, 0, 0, 1.0, 0, a, 'Snap'), (t0 + 0.08, 0, 0, 1.3, 0, 0, 'Out')])

    def frames(g, name, seq, sc, ks, **fx):
        var = s.s(g, name, seq[0][1], sc, ks, **fx)
        sw = ', '.join(f'new NKK.Ults.SuperUltLayer.Swap {{ t = {f(t)}, sprite = S("{spr}") }}' for t, spr in seq[1:])
        if sw: s(f'{var}.swaps = new[] {{ {sw} }};')
        return var

    def cam(g, name, ks):
        """카메라 묶음 (화면 가운데 기준 확대 = 소실점 쪽으로 다가감): ks = [(t, 배율, 이징)] → 소실점이 제자리에 있게 위치 보정"""
        var = s.v('cam')
        s(f'var {var} = G("{name}", {g}, {keys([(t, -VP[0] * (z - 1), -VP[1] * (z - 1), z, 0, 1, e) for t, z, e in ks])}).transform;')
        return var

    # 병사 대열 (화면 px, 발끝 · 정면): 뒤 줄일수록 작고 지평선 가까이 · 줄마다 반 칸 어긋남 — 뒤 → 앞 (그리는 순서)
    RANKS = [(-20, 230, 9), (-200, 370, 7), (-470, 560, 5)]
    soldiers = []
    for r, (y, h, n) in enumerate(RANKS):
        gap = h * 0.72
        for i in range(n):
            soldiers.append((r, i, (i - (n - 1) / 2) * gap + (gap / 4 if r == 1 else 0), y, h))
    # ① 황제가 칼을 높이 듦
    T1 = 1.05
    g = s.group('Cut1', R, 0.4, T1)
    s.s(g, 'Back', 'face_bg', 1.35, [(0.4, 0, 0, 1.0, 0, 1, 'Out'), (T1, 0, 0, 1.06, 4, 1, 'Linear')])
    s.s(g, 'Emp', 'cu_emp1', K, [(0.4, -60, -80, 1.0, 0, 1, 'Out'), (T1, 0, -20, 1.1, 0, 1, 'Linear')])
    sx, sy = at(*SABER_TIP)
    s.s(g, 'Glint', 'fx_spark', 0.8, [(0.7, sx, sy, 0, 0, 0, 'Out'), (0.72, sx, sy, 1.2, 0, 1, 'Back'), (0.95, sx, sy, 0.8, 90, 0, 'Out')])
    # ② 총병 앞으로: 정면 들판에 뒤 줄부터 소환 · 카메라가 물러남
    T2 = 1.95
    g = s.group('Cut2', R, T1, T2)
    c = cam(g, 'Cam', [(T1, 1.25, 'Out'), (T2, 1.0, 'Out')])
    s.s(c, 'Field', 'bg_front', K, [(T1, 0, 0, 1.0, 0, 1, 'Out')])
    D = []
    for r, i, x, y, h in soldiers: D += [x, y, h, T1 + 0.06 + r * 0.16 + abs(i - (RANKS[r][2] - 1) / 2) * 0.04]
    s(f'{{ float[] D = {{ {fa(D)} }}; float rh = S("anim_front_1").rect.height;')
    s(f'  for (int i = 0; i < D.Length; i += 4) {{ float x = D[i], y = D[i + 1], h = D[i + 2], t = D[i + 3];')
    s(f'    var l = L("S" + i / 4, {c}, "anim_front_1", h / rh, new[] {{ K(t - 0.01f, x, y, 0.6f, 0, 0, 0), K(t, x, y, 1.12f, 0, 1, 5), K(t + 0.1f, x, y, 1, 0, 1, 4) }}); ((RectTransform)l.transform).pivot = new Vector2(0.5f, 0f); l.swapResize = true;')
    s(f'    L("Puff" + i / 4, {c}, "fx_summon", h / 300f, new[] {{ K(t - 0.01f, x, y + h * 0.45f, 0.3f, 0, 0, 0), K(t, x, y + h * 0.45f, 0.6f, 0, 1, 5), K(t + 0.3f, x, y + h * 0.5f, 1.4f, 20, 0, 0) }}); }} }}')
    cut_flash(g, T1, 0.4)
    # ③ 장전: 공이치기 철컥 → 총구가 카메라를 겨눔 (총구로 밀고 들어감)
    T3a, CLICK, T3 = 2.4, 2.22, 2.75
    g = s.group('Cut3', R, T2, T3a)
    s.s(g, 'Back', 'face_bg', 1.35, [(T2, 0, 0, 1.1, 0, 1, 'Out'), (T3a, 0, 0, 1.0, 0, 1, 'Linear')])
    frames(g, 'Load', [(T2, 'cu_load1'), (CLICK, 'cu_load2')], K, [(T2, -200, 0, 1.0, 0, 1, 'Out'), (T2 + 0.12, 0, 0, 1.0, 0, 1, 'Out'), (T3a, 0, 0, 1.05, 0, 1, 'Linear')], kicks=[CLICK], kickPower=26, kickTime=0.15)
    hx_, hy_ = at(*HAMMER)
    s.s(g, 'Click', 'fx_spark', 0.6, [(CLICK - 0.01, hx_, hy_, 0, 0, 0, 'Out'), (CLICK, hx_, hy_, 0.8, 0, 1, 'Snap'), (CLICK + 0.15, hx_, hy_, 1.3, 30, 0, 'Out')])
    cut_flash(g, T2, 0.35)
    g = s.group('Cut3b', R, T3a, T3)
    s.s(g, 'Back', 'face_bg', 1.35, [(T3a, 0, 0, 1.0, 0, 1, 'Out'), (T3, 0, 0, 1.1, -3, 1, 'Linear')])
    mx_, my_ = at(*AIM_MUZZLE)
    s.s(g, 'Aim', 'cu_aimfront', K, [(T3a, 0, 0, 1.0, 0, 1, 'Out'), (T3, -mx_ * 0.35, -my_ * 0.35, 1.35, 0, 1, 'In')])
    # ④ 격발: 칼끝을 카메라로 내지름
    SW, T4 = 2.84, 3.05
    g = s.group('Cut4', R, T3, T4)
    s.s(g, 'Back', 'face_bg', 1.35, [(T3, 0, 0, 1.1, 0, 1, 'Out'), (T4, 0, 0, 1.0, 20, 1, 'Linear')])
    frames(g, 'Emp', [(T3, 'cu_emp1'), (SW, 'cu_empfront')], K, [(T3, 0, -20, 1.1, 0, 1, 'Out'), (SW, 0, -20, 1.12, 0, 1, 'Linear'), (SW + 0.01, 0, 0, 1.25, 0, 1, 'Snap'), (T4, 0, 0, 1.12, 0, 1, 'Out')], kicks=[SW], kickPower=44, kickTime=0.2)
    for i in range(12):                                   # 칼끝에서 바깥으로 뻗는 집중선
        a = math.radians(i * 30 + 8); t0 = SW + 0.01 * (i % 3); rot = math.degrees(a)
        s.s(g, f'Thrust{i}', 'fx_radial', 1.6, [(t0, math.cos(a) * 250, math.sin(a) * 160, 0.6, rot, 0, 'Out'), (t0 + 0.02, math.cos(a) * 420, math.sin(a) * 270, 1.0, rot, 0.85, 'Linear'), (T4, math.cos(a) * 1250, math.sin(a) * 800, 1.6, rot, 0, 'Out')])
    cut_flash(g, T3, 0.4)
    # ⑤ 정면 일제 사격 4번 → 카메라 돌진 → ⑥ 탄흔이 화면을 덮음
    VOL = [3.15, 3.5, 3.85, 4.15]; RUSH0, RUSH1 = 4.2, 4.45
    g = s.group('Cut5', R, T4)
    ck = [(T4, 1.0, 'Out')]
    for k, v in enumerate(VOL[:3]):                       # 쏠 때마다 쾅 밀고 들어감 → 살짝 물러남
        z = 1.0 + 0.05 * k
        ck += [(v, z, 'Linear'), (v + 0.06, z + 0.12, 'Out'), (v + 0.28, z + 0.04, 'Out')]
    ck += [(VOL[3], 1.16, 'Linear'), (RUSH0, 1.22, 'Out'), (RUSH1, 2.4, 'In'), (END + 0.4, 3.0, 'Linear')]     # 마지막: 대열로 돌진
    c = cam(g, 'Cam', ck)
    s(f'{{ var gl = {c}.GetComponent<NKK.Ults.SuperUltLayer>(); gl.kicks = new[] {{ {fa([v + 0.01 for v in VOL])} }}; gl.kickPower = 26; gl.kickTime = 0.16f; }}')
    s.s(c, 'Field', 'bg_front', K, [(T4, 0, 0, 1.0, 0, 1, 'Out')])
    fmx, fmy = FRONT_MUZZLE
    D = []
    for r, i, x, y, h in soldiers: D += [x, y, h, (2 - r) * 0.03 + abs(i - (RANKS[r][2] - 1) / 2) * 0.012]
    s(f'{{ float[] D = {{ {fa(D)} }}; float[] V = {{ {fa(VOL)} }}; float rh = S("anim_front_1").rect.height; float vx = {f(VP[0])}, vy = {f(VP[1])};')
    s(f'  for (int i = 0; i < D.Length; i += 4) {{ float x = D[i], y = D[i + 1], h = D[i + 2], ox = x + {f(fmx)} * h, oy = y + {f(fmy)} * h, ex = vx + (ox - vx) * 4.5f, ey = vy + (oy - vy) * 4.5f;')
    s(f'    var sw = new System.Collections.Generic.List<NKK.Ults.SuperUltLayer.Swap>(); var mk = new System.Collections.Generic.List<NKK.Ults.SuperUltLayer.Key> {{ K({f(T4)}, ox, oy, 0, 0, 0, 0) }}; var bk = new System.Collections.Generic.List<NKK.Ults.SuperUltLayer.Key> {{ K({f(T4)}, ox, oy, 0.1f, 0, 0, 0) }};')
    s(f'    foreach (var v in V) {{ float tf = v + D[i + 3]; sw.Add(new NKK.Ults.SuperUltLayer.Swap {{ t = tf, sprite = S("anim_front_3") }}); sw.Add(new NKK.Ults.SuperUltLayer.Swap {{ t = tf + 0.09f, sprite = S("anim_front_2") }});')
    s(f'      mk.Add(K(tf - 0.005f, ox, oy, 0.4f, 0, 0, 5)); mk.Add(K(tf, ox, oy, 1, 0, 1, 5)); mk.Add(K(tf + 0.07f, ox, oy, 1.4f, 30, 0, 0));')
    s(f'      bk.Add(K(tf - 0.005f, ox, oy, 0.1f, 0, 0, 5)); bk.Add(K(tf + 0.01f, ox, oy, 0.15f, 0, 1, 5)); bk.Add(K(tf + 0.13f, ex, ey, 2.6f, 0, 1, 2)); bk.Add(K(tf + 0.14f, ex, ey, 2.6f, 0, 0, 5)); }}')
    s(f'    var l = L("S" + i / 4, {c}, "anim_front_2", h / rh, new[] {{ K({f(T4)}, x, y, 1, 0, 1, 0) }}); ((RectTransform)l.transform).pivot = new Vector2(0.5f, 0f); l.swapResize = true; l.swaps = sw.ToArray();')
    s(f'    L("M" + i / 4, {c}, "fx_fmuzzle", h / 520f, mk.ToArray()); L("B" + i / 4, {c}, "fx_fbullet", h / 1100f, bk.ToArray()); }} }}')
    for k, (y, h, n) in enumerate(RANKS):                 # 줄마다 연기 고리 (쏠 때마다 피어오름, 옅게)
        sk = [(T4, 0, y + h * 0.6, 1, 0, 0, 'Out')]
        for v in VOL: sk += [(v + 0.03, 0, y + h * 0.6, 0.9, 0, 0, 'Snap'), (v + 0.08, 0, y + h * 0.62, 1.0, 0, 0.25, 'Out'), (v + 0.32, 0, y + h * 0.7, 1.2, 0, 0, 'Out')]
        s.s(c, f'Smoke{k}', 'fx_smokeband', 1.9 * h / 320, sk)
    # 화면 공간: 총알이 화면 가장자리로 스쳐 지나감 (카메라 돌진 중 · 소실점에서 바깥으로 커지며)
    D = []
    for i in range(70):
        u = (i / 69) ** 0.8; t0 = VOL[1] + (END - 0.1 - VOL[1]) * u; a = s.rnd.uniform(0, 2 * math.pi)
        r0, r1 = s.rnd.uniform(40, 220), s.rnd.uniform(1150, 1500)
        D += [t0, math.cos(a), math.sin(a) * 0.62, r0, r1, s.rnd.uniform(0.11, 0.17), math.degrees(a) + 30]     # fx_fbullet3q = 오른쪽 아래(-30°)로 날아가는 그림 → 바깥 방향으로 돌림
    s(f'{{ float[] D = {{ {fa(D)} }}; float vx = {f(VP[0])}, vy = {f(VP[1])};')
    s(f'  for (int i = 0; i < D.Length; i += 7) {{ float t = D[i], cx = D[i + 1], cy = D[i + 2], r0 = D[i + 3], r1 = D[i + 4], d = D[i + 5], rot = D[i + 6];')
    s(f'    L("P" + i / 7, {g}, "fx_fbullet3q", 1, new[] {{ K(t - 0.01f, vx + cx * r0, vy + cy * r0, 0.15f, rot, 0, 0), K(t, vx + cx * r0, vy + cy * r0, 0.2f, rot, 1, 5), K(t + d, vx + cx * r1, vy + cy * r1, 2.4f, rot, 1, 2), K(t + d + 0.01f, vx + cx * r1, vy + cy * r1, 2.4f, rot, 0, 5) }}); }} }}')
    for i in range(16):                                   # 돌진 집중선 (바깥으로)
        a = math.radians(i * 22.5 + 5); t0 = RUSH0 + 0.012 * (i % 4); rot = math.degrees(a)
        s.s(g, f'Rush{i}', 'fx_radial', 1.8, [(t0, VP[0] + math.cos(a) * 200, VP[1] + math.sin(a) * 130, 0.5, rot, 0, 'Out'), (t0 + 0.03, VP[0] + math.cos(a) * 380, VP[1] + math.sin(a) * 250, 1.0, rot, 0.8, 'Linear'),
                                                  (RUSH1 + 0.05, VP[0] + math.cos(a) * 1300, VP[1] + math.sin(a) * 850, 1.8, rot, 0, 'In')])
    # ⑥ 탄흔: 총알이 화면(렌즈)에 박힘 (점점 빠르게 · 화면을 덮음) — 루트 바로 아래 Hold 묶음: 컷신이 걷혀도 게임 화면 위에 1.5초 남음 (holdGroup, 2026-10-20 사용자)
    hg = s.group('Hold', R, RUSH1 - 0.02)
    D = []
    for i in range(34):
        u = (i / 33) ** 0.7; D += [RUSH1 + (END - 0.05 - RUSH1) * u, s.rnd.uniform(-880, 880), s.rnd.uniform(-480, 480), s.rnd.uniform(0.7, 1.5), s.rnd.uniform(0, 360)]
    s(f'{{ float[] D = {{ {fa(D)} }}; for (int i = 0; i < D.Length; i += 5) {{ float t = D[i], x = D[i + 1], y = D[i + 2], sc = D[i + 3], r = D[i + 4];')
    s(f'  L("Hole" + i / 5, {hg}, "fx_hole", sc, new[] {{ K(t - 0.01f, x, y, sc * 0.3f, r, 0, 0), K(t, x, y, 1.15f, r, 1, 5), K(t + 0.06f, x, y, 1, r, 1, 0), K({f(END + 0.4)}, x, y, 1, r, 1, 1) }});')
    s(f'  L("HoleFlash" + i / 5, {hg}, "fx_fmuzzle", sc * 0.5f, new[] {{ K(t - 0.01f, x, y, 0.3f, r, 0, 0), K(t, x, y, 0.7f, r, 1, 5), K(t + 0.08f, x, y, 1.2f, r, 0, 0) }}); }} }}')
    s(f'{{ var gl = {g}.GetComponent<NKK.Ults.SuperUltLayer>(); gl.kicks = new[] {{ {fa([RUSH1 + (END - 0.05 - RUSH1) * (i / 7) ** 0.7 for i in range(8)])} }}; gl.kickPower = 30; gl.kickTime = 0.12f; }}')
    s(f'{{ var gl = {hg}.GetComponent<NKK.Ults.SuperUltLayer>(); gl.kicks = new[] {{ {fa([RUSH1 + (END - 0.05 - RUSH1) * (i / 7) ** 0.7 for i in range(8)])} }}; gl.kickPower = 30; gl.kickTime = 0.12f; }}')     # 탄흔 묶음도 같이 흔들림
    cut_flash(g, T4, 0.45)
    return None                                           # 글 없음 (NO_TEXT)


# 쥐 황제 v2 그림 속 자리 (그림 비율 · 그림이 바뀌면 다시 잼)
SABER_TIP = (0.40, 0.04)      # cu_emp1: 높이 든 칼끝
HAMMER = (0.41, 0.37)         # cu_load: 공이치기
MUZZLE = (0.49, 0.63)         # anim_soldier_2: 총구 (측면 · 안 씀)
FRONT_HORIZON = 0.5           # bg_front: 지평선 (그림 높이 비율)
FRONT_MUZZLE = (-0.03, 0.62)   # anim_front_2: 총구 (발끝 가운데 기준 x · 위로 y, 키 비율)
AIM_MUZZLE = (0.52, 0.6)      # cu_aimfront: 총구 구멍


def emperor_v1(s, R):
    """(v1, 2026-10-18 · 안 씀) 세로: 금빛 하늘 → 궁궐 → 계단. ② 계단 아래에서 위로 걸어 오르는 황제를 쫓으며 대포 · 등불 · ③ 위·아래 두 띠(시네마) + 대포알이 비스듬히 · ④ 옥좌 클로즈업에서 확 물러나며 금 비"""
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
    """세로 (정면): 노을 태양 → 피라미드 계단 → 모래 평원. (2026-10-20 v2 초안, 사용자: 아누비스 · 미라를 잔뜩 소환 → 미라가 돌격 · 정면샷 · 기사/아이스 퀸 연출 참고)
    띠 · 마무리 컷 없음 · 플랫 그림 · 리그 파츠 바탕 셀 프레임 · 0.75배속 — 컷 6개:
    ① 0.4 노을 태양에서 피라미드 정면으로 휙 틸트 다운 ② 0.85 계단 아래 정면: 팔짱 → 지팡이 · 도리깨를 펼쳐 → 두 팔을 하늘로 (소환진이 발밑에서 퍼짐)
    ③ 1.4 얼굴 정면 클로즈업 (눈이 금빛으로 번쩍)
    ④ 1.85 소환: 모래 평원 곳곳에서 모래가 터지며 아누비스 · 미라가 줄줄이 솟음 → 카메라가 물러나며 뒤로 군단이 가득 (파라오는 계단 위 가운데)
    ⑤ 2.65 정면 지휘: 지팡이를 카메라 쪽으로 내지름 ⑥ 2.95 미라 돌격: 멀리서 카메라 쪽으로 미라들이 달려와 화면 밖으로 지나감 · 양옆 아누비스 호위 · 모래 먼지
       → 맨 앞 미라가 카메라로 덮쳐 와 모래 먼지로 화면을 덮음
    → 컷신 걷힘 → 게임 화면: 미라 쥐들이 왼→오로 줄줄이 달려 지나감 (runFrames, 모래 입자 없음) · 다단 10타 = 태클 충돌
    (2026-10-20 사용자: 아누비스는 ④ 에서 한 마리만 크게 · ⑥ 옆에서 지켜봄, 미라는 잔뜩)"""
    END = NO_FINISH['pharaoh']

    def cut_flash(g, t0, a=0.55):
        s.s(g, 'CutFlash', 'fx_sunflash', 6.0, [(t0, 0, 0, 1.0, 0, a, 'Snap'), (t0 + 0.09, 0, 0, 1.3, 0, 0, 'Out')])

    def run_frames(t0, t1, gap=0.07, first=0):
        """미라 달리기 3프레임을 t0~t1 동안 반복"""
        fr, t, k = [], t0, first
        while t < t1:
            fr.append((t, f'anim_mrun_{k % 3 + 1}')); t += gap; k += 1
        return fr

    # ① 노을 태양 → 피라미드 정면으로 휙 틸트 다운
    g = s.group('Cut1', R, 0.4, 0.85)
    tr = s.cam(g, [(0.4, 0.58, 0.22, 1.3, 0, 'Out'), (0.62, 0.57, 0.25, 1.25, 0, 'Linear'), (0.85, 0.5, 0.52, 1.05, 0, 'In')], kicks=[0.4], power=20)
    s.w(tr, 'SunRing', 'fx_sunflash', 2.2, [(0.4, 0.62, 0.243, 0.8, 0, 0.6, 'Out'), (0.85, 0.62, 0.243, 1.0, 20, 0.8, 'Linear')], spin=10.0)
    s.speed(g, 0.62, 0.85, vertical=True)
    # ② 계단 아래 정면: 팔짱 → 펼침 → 두 팔을 하늘로 · 발밑 소환진
    S1, S2 = 1.0, 1.14
    g = s.group('Cut2', R, 0.85, 1.4)
    tr = s.cam(g, [(0.85, 0.5, 0.60, 1.9, 0, 'Out'), (1.4, 0.5, 0.595, 2.15, 0, 'Linear')], kicks=[S2], power=34)
    s.w(tr, 'Circle', 'fx_circle', 0.9, [(0.85, 0.5, 0.632, 0.2, 0, 0, 'Out'), (S1, 0.5, 0.632, 0.7, 0, 0.9, 'Back'), (S2, 0.5, 0.632, 1.25, 0, 1, 'Back'), (1.4, 0.5, 0.632, 1.35, 0, 1, 'Linear')], pulse=0.05, pulseHz=8)
    s.cel(tr, 'Pharaoh', [(0.85, 'anim_summon_1'), (S1, 'anim_summon_2'), (S2, 'anim_summon_3')],
          [(0.85, 0.5, 0.635, 1, 0, 1, 'Out'), (S1, 0.5, 0.635, 1.04, 0, 1, 'Back'), (S2, 0.5, 0.635, 1.1, 0, 1, 'Back'), (1.4, 0.5, 0.635, 1.12, 0, 1, 'Linear')], height=300, ref='anim_summon_1')
    s.s(g, 'Sun', 'fx_sunflash', 3.0, [(S2 - 0.01, 0, 260, 0, 0, 0, 'Out'), (S2, 0, 260, 0.8, 0, 0.8, 'Snap'), (1.4, 0, 260, 1.3, 25, 0, 'Out')])
    s.rise(g, 'Sand', 'fx_puff', [S2 + 0.03 * i for i in range(8)], [-760, -540, -320, -110, 110, 320, 540, 760], -560, 420, sc=0.5, dur=0.28)
    cut_flash(g, 0.85)
    # ③ 얼굴 정면 클로즈업 (배경 + 쥐) · 눈 번쩍
    g = s.group('Cut3', R, 1.4, 1.85)
    s.face(g, 'zoom', 1.4, 1.85, bg='face_bg')
    s.s(g, 'EyeFlash', 'fx_sunflash', 0.8, [(1.55, 0, 60, 0, 0, 0, 'Out'), (1.56, 0, 60, 1.0, 0, 0.9, 'Snap'), (1.75, 0, 60, 1.6, 30, 0, 'Out')])
    cut_flash(g, 1.4)
    # ④ 소환: 모래 평원 곳곳에서 모래가 터지며 아누비스 · 미라가 솟음 → 카메라가 물러나며 뒤로 군단이 가득
    T = 1.85
    g = s.group('Cut4', R, T, 2.65)
    tr = s.cam(g, [(T, 0.5, 0.76, 1.7, 0, 'Out'), (2.65, 0.5, 0.70, 1.15, 0, 'InOut')], kicks=[T + 0.05, T + 0.3], power=30, ktime=0.2)
    s.w(tr, 'Pharaoh', 'anim_summon_3', s.hp('anim_summon_3', 120), [(T, 0.5, 0.62, 1, 0, 1, 'Out')])          # 계단 아래 가운데 (작게, 군단 뒤)
    s.w(tr, 'ArmyBack', 'army', s.hp('army', 260), [(T, 0.5, 0.665, 0.9, 0, 0, 'Out'), (T + 0.35, 0.5, 0.665, 0.9, 0, 0, 'Linear'), (T + 0.5, 0.5, 0.66, 1.0, 0, 1, 'Back'), (2.65, 0.5, 0.66, 1.0, 0, 1, 'Linear')])
    rows = [(0.69, 170, 0.0, 6), (0.74, 230, 0.12, 6), (0.80, 320, 0.24, 5)]       # (파노라마 y, 키, 늦게, 수) — 뒤 줄일수록 작고 먼저
    for r, (yy, hh, dt, n) in enumerate(rows):
        for i in range(n):
            x = 0.12 + 0.76 * (i + 0.5 * (r % 2)) / n
            t0 = T + dt + 0.06 * i + s.rnd.uniform(0, 0.04)
            who = 'anim_mrise'                          # 미라는 잔뜩 (2026-10-20 사용자: 아누비스는 한 마리만)
            s.w(tr, f'Burst{r}_{i}', 'fx_sandburst', hh / 900, [(t0 - 0.01, x, yy, 0.3, 0, 0, 'Out'), (t0, x, yy, 0.6, 0, 1, 'Snap'), (t0 + 0.18, x, yy - 0.01, 1.1, 0, 0.9, 'Out'), (t0 + 0.35, x, yy - 0.02, 1.2, 0, 0, 'Linear')])
            s.cel(tr, f'Rise{r}_{i}', [(t0, f'{who}_1'), (t0 + 0.08, f'{who}_2'), (t0 + 0.18, f'{who}_3')],
                  [(t0 - 0.01, x, yy, 1, 0, 0, 'Out'), (t0, x, yy, 0.9, 0, 1, 'Snap'), (t0 + 0.18, x, yy, 1.05, 0, 1, 'Back'), (2.65, x, yy, 1.0, 0, 1, 'Linear')], height=hh, ref=f'{who}_3')
    s.s(g, 'Ring', 'fx_sandring', 2.4, [(T + 0.05, 0, -300, 0.3, 0, 0.9, 'Snap'), (T + 0.45, 0, -300, 1.6, 0, 0, 'Out')])
    AN = T + 0.42                                       # 아누비스 한 마리: 미라들이 다 솟은 뒤 앞줄 가운데에서 크게 (쿵 · 모래 고리)
    s.w(tr, 'AnubisBurst', 'fx_sandburst', 0.5, [(AN - 0.01, 0.5, 0.85, 0.3, 0, 0, 'Out'), (AN, 0.5, 0.85, 0.7, 0, 1, 'Snap'), (AN + 0.2, 0.5, 0.84, 1.3, 0, 0.9, 'Out'), (AN + 0.4, 0.5, 0.83, 1.4, 0, 0, 'Linear')])
    s.cel(tr, 'Anubis', [(AN, 'anim_anubis_1'), (AN + 0.09, 'anim_anubis_2'), (AN + 0.2, 'anim_anubis_3')],
          [(AN - 0.01, 0.5, 0.85, 1, 0, 0, 'Out'), (AN, 0.5, 0.85, 0.9, 0, 1, 'Snap'), (AN + 0.2, 0.5, 0.85, 1.08, 0, 1, 'Back'), (2.65, 0.5, 0.85, 1.0, 0, 1, 'Linear')], height=430, ref='anim_anubis_3')
    s.s(g, 'AnubisRing', 'fx_sandring', 3.0, [(AN + 0.2, 0, -330, 0.4, 0, 0.9, 'Snap'), (AN + 0.55, 0, -330, 1.7, 0, 0, 'Out')])
    s.s(g, 'Dune', 'prop_fg', 2.2, [(T, 0, -470, 1, 0, 1, 'Out'), (2.65, 0, -500, 1.05, 0, 1, 'Linear')])
    cut_flash(g, T)
    # ⑤ 정면 지휘: 지팡이를 카메라 쪽으로 내지름
    C1 = 2.76
    g = s.group('Cut5', R, 2.65, 2.95)
    tr = s.cam(g, [(2.65, 0.5, 0.60, 2.3, 0, 'Out'), (2.95, 0.5, 0.60, 2.6, 0, 'In')], kicks=[C1], power=50, rot=3)
    s.cel(tr, 'Pharaoh', [(2.65, 'anim_command_1'), (C1, 'anim_command_2'), (C1 + 0.1, 'anim_command_3')],
          [(2.65, 0.5, 0.635, 1, 0, 1, 'Out'), (C1, 0.5, 0.635, 1.12, 0, 1, 'Back'), (2.95, 0.5, 0.635, 1.15, 0, 1, 'Linear')], height=330, ref='anim_command_1')
    s.s(g, 'Thrust', 'fx_sunflash', 2.4, [(C1 - 0.01, 0, -40, 0, 0, 0, 'Out'), (C1, 0, -40, 0.6, 0, 0.85, 'Snap'), (2.95, 0, -40, 1.6, 30, 0, 'Out')])
    s.burst_ring(g, 'Band', 'fx_bandage', C1, 0, -40, n=8, r=1100, sc=0.5, dur=0.25)
    cut_flash(g, 2.65)
    # ⑥ 미라 돌격: 멀리서 카메라 쪽으로 (작게 → 크게 · 화면 밖으로) — 원근 = 발끝 y 가 내려오고 x 가 바깥으로 벌어짐
    T = 2.95; LEAP = END - 0.38
    g = s.group('Cut6', R, T)
    tr = s.cam(g, [(T, 0.5, 0.79, 1.35, 0, 'Out'), (END + 0.4, 0.5, 0.82, 1.6, 0, 'In')], kicks=[T + 0.1, T + 0.3, T + 0.5], power=22, ktime=0.18)
    s.cel(tr, 'Anubis', [(T, 'anim_anubis_3')], [(T, 0.83, 0.86, 1, 0, 1, 'Out')], height=360, ref='anim_anubis_3')      # 아누비스 한 마리가 옆에서 지켜봄
    for i in range(22):                                 # 달려오는 미라 (늦게 출발한 것이 위에 그려짐)
        t0 = T + 0.04 * i + s.rnd.uniform(0, 0.03); dur = s.rnd.uniform(0.38, 0.5)
        x0 = s.rnd.uniform(-260, 260); x1 = x0 * 4.2 + (700 if x0 > 0 else -700)
        s.cel(g, f'Run{i}', run_frames(t0, t0 + dur, first=i), [(t0 - 0.01, x0, -40, 0.25, 0, 0, 'Out'), (t0, x0, -40, 0.25, 0, 1, 'Snap'), (t0 + dur, x1, -900, 2.6, 0, 1, 'In')],
              height=320, ref='anim_mrun_1', pano=False)
    # 맨 앞 미라가 카메라로 덮쳐 와 화면을 가림 (모래 먼지 없음 — 2026-10-20 사용자: 모래바람 대신 미라가 뛰어가는 걸로 마무리)
    s.cel(g, 'Leap', [(LEAP, 'anim_mleap_1'), (LEAP + 0.1, 'anim_mleap_2'), (LEAP + 0.22, 'anim_mleap_3')],
          [(LEAP - 0.01, 0, -120, 0.5, 0, 0, 'Out'), (LEAP, 0, -120, 0.5, 0, 1, 'Snap'), (LEAP + 0.1, 0, -260, 0.9, 0, 1, 'Out'), (END, 0, -1300, 3.4, 0, 1, 'In')], height=420, ref='anim_mleap_1', pano=False)
    cut_flash(g, T)
    # 밈 (⑤~⑥ 화면 위)
    G = s.group('Top', R, 2.7)
    s.meme(G, 2.75, side=-1, y=440)
    return None


def pharaoh_v1(s, R):
    """(옛 v1, 안 씀) 세로: 태양 → 피라미드 → 신전. ② 신전 위에서 천천히 태양까지 올려다봄 → 태양이 번쩍 → 휙 내려와 쾅 · ③ 오른쪽 세로 띠 + 햇빛 기둥이 부채꼴로 쓸고 지나감 · ④ 비스듬히 돌며 태양 고리 · 모래 소용돌이"""
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


def sultan_v1(s, R):
    """(옛 것) 가로: 사막 → 시장 → 궁궐. ② 궁궐 앞 램프에서 연기 → 양탄자를 따라 시장을 거꾸로(왼쪽으로) 휙 · ③ 가운데 띠(위·아래로 게임 화면) + 금화 소나기 · ④ 소원 세 번 = 줌 컷 세 번"""
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


def sultan(s, R):
    """가로 (하늘 위 사막 도시 한 장 = 파노라마). (2026-10-20 v2, 사용자: 석유로 큰돈을 버는 건 짧게 → 궁전에서 나와 마법 양탄자를 타고 금화를 비처럼 뿌림
    · 2차: 석유 컷은 금화 없이 석유만 · 유정 작게 · 궁전에 돈이 가득 · 궁전 밖에서 바로 양탄자로 하늘까지 · 양탄자 밑으로 금화를 땅까지 쭉 쏟아부음)
    띠 · 마무리 컷 없음 · 플랫 · 같은 틀 연속 프레임(shot_oil · cu_face) 바꿔 끼우기 · 셀 프레임(anim_ride · anim_pour) · 1배속 — 컷 4개:
    ① 0.4 사막의 작은 유정 → 땅이 들썩 → 검은 석유가 하늘까지 분출 (짧게, 금화 없음) ② 얼굴 클로즈업: 흐뭇 → 눈이 금화로 반짝
    ③ 금화 산이 쌓인 궁전 앞에서 양탄자를 탄 채 떠오름 → 카메라가 따라 아래(궁전) → 위(하늘)로 틸트 (세로 한 장 shot_rise · 속도선)
    ④ 하늘: 도시 위를 날며 자루에서 금화를 퍼 두 손으로 하늘 높이 흩뿌림 → 그 금화가 화면 가득 비처럼 쏟아짐 (순수 금화만 · 지붕마다 짤랑)
    → 컷신 걷힘 → 게임 화면: 금화 비가 쏟아져 대상마다 짤랑 (worldPhase · imageFx)"""
    END = NO_FINISH['sultan']
    K = 1.32                                              # 전체 화면 컷 배율 (1536 → 약 2030px, 화면을 덮음)
    cx = lambda rx: (rx - 0.5) * 1536 * K                 # 그림 비율 → 화면 px
    cy = lambda ry: (0.5 - ry) * 1024 * K

    def cut_flash(g, t0, a=0.55):
        s.s(g, 'CutFlash', 'fx_sparkle', 9.0, [(t0, 0, 0, 1.0, 0, a, 'Snap'), (t0 + 0.09, 0, 0, 1.3, 0, 0, 'Out')])

    def frames(g, name, seq, ks, sc=K, **fx):
        """같은 틀 연속 프레임 (그림만 바꿔 끼움)"""
        var = s.s(g, name, seq[0][1], sc, ks, **fx)
        sw = ', '.join(f'new NKK.Ults.SuperUltLayer.Swap {{ t = {f(t)}, sprite = S("{spr}") }}' for t, spr in seq[1:])
        if sw: s(f'{var}.swaps = new[] {{ {sw} }};')
        return var

    # ① 사막 유정 (짧게, 석유만): 천천히 다가감 → 땅이 들썩 (떨림) → 쾅! 검은 석유가 하늘까지 솟구치고 기름 방울이 쏟아짐
    RUM, OIL, T1 = 0.56, 0.68, 0.98
    gx, gy = cx(GUSH[0]), cy(GUSH[1])                     # 유정탑 꼭대기 (석유가 뿜는 자리)
    g = s.group('Cut1', R, 0.4, T1)
    frames(g, 'Oil', [(0.4, 'shot_oil1'), (RUM, 'shot_oil2'), (OIL, 'shot_oil3')],
           [(0.4, 40, 60, 1.0, 2, 1, 'Out'), (RUM, 70, 40, 1.06, 1, 1, 'InOut'), (OIL, 40, 30, 1.08, 0, 1, 'Linear'), (OIL + 0.05, 0, -60, 1.22, -2, 1, 'Snap'), (T1, -50, -230, 1.3, -4, 1, 'Out')],     # 옆으로 흐르며 다가감 → 분출 순간 확 줌인 + 솟구치는 석유를 따라 위로 틸트
           kicks=[RUM, RUM + 0.06, OIL], kickPower=40, kickTime=0.22)
    s.burst_ring(g, 'OilBurst', 'fx_oil', OIL + 0.02, gx, gy + 60, n=10, r=700, sc=0.3, dur=0.35)
    s.shower(g, 'OilRain', 'fx_oil', OIL + 0.06, T1, 10, -900, 900, 640, -560, sc=0.24)
    # ② 얼굴 클로즈업: 흐뭇하게 돈 세는 손짓 → 눈이 금화로 반짝 (짤랑)
    MONEY, T2 = 1.12, 1.42
    g = s.group('Cut2', R, T1, T2)
    s.s(g, 'Back', 'face_bg', 1.35, [(T1, 0, 0, 1.12, 0, 1, 'Out'), (T2, 0, 0, 1.0, 0, 1, 'Linear')], spin=12.0)
    frames(g, 'Face', [(T1, 'cu_face1'), (MONEY, 'cu_face2')],
           [(T1, -60, 0, 1.15, -7, 1, 'Snap'), (T1 + 0.1, -20, 0, 1.0, 1, 1, 'Back'), (MONEY, 20, -10, 1.02, 3, 1, 'InOut'), (MONEY + 0.05, 0, 20, 1.16, -3, 1, 'Snap'), (T2, 40, 30, 1.22, 2, 1, 'Out')],     # 기울어 들어옴 → 반대로 흔들림 → 금화 눈 순간 줌인
           sc=1.3, kicks=[MONEY], kickPower=30, kickTime=0.2)
    for k, (ex, t0) in enumerate(((EYES[0], MONEY + 0.03), (EYES[1], MONEY + 0.08))):     # 금화 눈 반짝
        x, y = (ex - 0.5) * 1536 * 1.3, (0.5 - EYES[2]) * 1024 * 1.3
        s.s(g, f'Glint{k}', 'fx_sparkle', 0.6, [(t0 - 0.01, x + 60, y + 50, 0, 0, 0, 'Out'), (t0, x + 60, y + 50, 0.7, 0, 1, 'Snap'), (t0 + 0.2, x + 60, y + 50, 1.3, 45, 0, 'Out')])
    s.burst_ring(g, 'Ching', 'fx_coin', MONEY + 0.02, 0, -60, n=10, r=1000, sc=0.32, dur=0.35)
    cut_flash(g, T1, 0.45)
    # ③ 금화가 넘치는 궁전 앞 → 양탄자를 탄 채 떠올라 하늘까지 (세로 한 장을 아래 → 위로 틸트, 점점 빨라짐)
    LIFT, T3 = 1.68, 2.4
    FEET = -420                                           # 양탄자 바닥 화면 높이 (③ · ④ 첫 부분 같은 자리 → 머리 위 여백 일정)
    bw, bh = s.size('shot_rise'); BK = 1920 * 1.04 / bw; PANY = (bh * BK - 1080) / 2
    g = s.group('Cut3', R, T2, T3)
    s.s(g, 'Rise', 'shot_rise', BK, [(T2, 80, PANY + 40, 1.12, 3, 1, 'Out'), (LIFT, 0, PANY - 40, 1.04, 0, 1, 'InOut'), (2.06, -70, PANY * 0.1, 1.08, -3, 1, 'In'), (T3, 30, -PANY, 1.04, 2, 1, 'Out')], kicks=[LIFT], kickPower=22, kickTime=0.2)     # 줌인 상태로 살짝 돌며 시작 → 떠오르면 좌우로 흔들리며 위로 휙
    for k in range(8):                                    # 금화 산 반짝 (틸트 전, 화면 아래)
        x, y, t0 = s.rnd.uniform(-850, 850), s.rnd.uniform(-480, -200), T2 + 0.03 * k
        s.s(g, f'Gleam{k}', 'fx_sparkle', 0.3, [(t0, x, y, 0, 0, 0, 'Out'), (t0 + 0.05, x, y, 0.8, 0, 1, 'Snap'), (t0 + 0.25, x, y, 1.1, 45, 0, 'Out')])
    s.cel(g, 'Rider', [(T2, 'anim_ride_1'), (LIFT, 'anim_ride_3')],
          [(T2, 30, FEET + 20, 1.0, -2, 1, 'Out'), (LIFT, 0, FEET, 1.0, 2, 1, 'InOut'), (LIFT + 0.08, 0, FEET - 20, 1.0, 0, 1, 'Out'), (2.06, -20, FEET, 1.0, 3, 1, 'InOut'), (T3, 10, FEET, 1.0, -2, 1, 'InOut')],     # 떠오른 뒤엔 카메라가 따라감: 화면 자리 · 크기 고정 = 머리 위 여백 일정 (2026-10-20 사용자 6차)
          height=540, ref='anim_ride_1', pano=False, jitter=2.0)
    s.burst_ring(g, 'Lift', 'fx_coin', LIFT + 0.02, 0, -440, n=8, r=700, sc=0.26, dur=0.35)
    s.speed(g, LIFT + 0.1, T3 + 0.05, vertical=True)
    cut_flash(g, T2, 0.45)
    # ④ 하늘: 도시 위를 날며 자루에서 금화를 퍼 두 손으로 하늘 높이 흩뿌림 → 그 금화가 비처럼 쏟아짐 (2026-10-20 사용자 4차: 밑으로 붓기 X · 손으로 위로 뿌리기 · 순수 금화만)
    THROW0, COVER = 2.56, 3.72
    TSPD = 0.7                                            # 던지는 동작 재생 속도 (2026-10-20 사용자 6차: 0.7배)
    TGAP = 0.22 / TSPD
    THROWS = [THROW0 + TGAP * i for i in range(int((END + 0.2 - THROW0) / TGAP) + 1)]
    g = s.group('Cut4', R, T3)
    ZF = 540 / (RIDE_H * s.PH * s.PS)                     # ③ 과 같은 키 (540px) 가 되는 줌 · 발끝을 FEET 에 두는 카메라 높이 차
    FOLLOW = -FEET / (s.PH * s.PS * ZF)
    RK = [(T3, 0.5, 0.64, 1.0, 0), (END + 0.4, 0.5, 0.64, 1.0, 0)]     # 양탄자는 제자리 (2026-10-20 사용자 8차: 양탄자 움직임 없이)

    def rider(t):                                         # 양탄자 자리 (파노라마 비율) — 금화가 손에서 나가는 자리 · 트래킹 줌 중심 계산용
        for (t0, x0, y0, *_), (t1, x1, y1, *_) in zip(RK, RK[1:]):
            if t <= t1: u = (t - t0) / (t1 - t0); return x0 + (x1 - x0) * u, y0 + (y1 - y0) * u
        return RK[-1][1:3]
    def look(t, z, r):                                    # 양탄자를 따라감: 쥐 가운데쯤을 화면 가운데 조금 아래에 (화면 밖으로 안 나가게 가둠)
        rx, ry = rider(t); y = ry - RIDE_H * 0.45 - 60 / (s.PH * s.PS * z)
        hx = max(0, 0.5 - 960 / (s.PW * s.PS * z)); hy = max(0, 0.5 - 540 / (s.PH * s.PS * z))
        return (t, min(max(rx, 0.5 - hx), 0.5 + hx), min(max(y, 0.5 - hy), 0.5 + hy), z, r)
    path = [(T3, 0.5, 0.64 - FOLLOW, ZF, 0, 'Out'), (T3 + 0.15, 0.5, 0.64 - FOLLOW, ZF, 0, 'Linear')]
    z = ZF
    for i, th in enumerate(THROWS):                       # 던질 때마다 줌이 조금씩 더 들어가 그대로 유지 (계단식 확대, 2026-10-20 사용자 7차: 넣었다 빼지 말고)
        if th <= path[-1][0] or th >= END + 0.3: continue
        r = -1.5 if i % 2 == 0 else 1.5                   # 뿌리는 쪽으로 살짝 기울임
        path += [(*look(th, z, -r * 0.5), 'InOut'), (*look(th + 0.12, z * 1.08, r), 'Out')]
        z *= 1.08
    path += [(*look(END + 0.4, z * 1.03, 0), 'Linear')]
    tr = s.cam(g, path, kicks=THROWS, power=10, rot=1.2, ktime=0.12)
    seq = [(T3, 'anim_ride_1')]
    for i, th in enumerate(THROWS):                       # 퍼 담기 → 한 손 휙 / 두 손 활짝 번갈아
        seq += [(th - 0.09 / TSPD, 'anim_ride_1'), (th, 'anim_ride_2' if i % 2 == 0 else 'anim_ride_3')]     # 짝수 = 왼쪽으로 휙 · 홀수 = 오른쪽으로 (양옆 번갈아)
    s.cel(tr, 'Rider', seq, [(t, x, y, sc, r, 1, 'Out' if i < 2 else 'InOut') for i, (t, x, y, sc, r) in enumerate(RK)], height=RIDE_H * s.PH * s.PS, ref='anim_ride_1', jitter=2.5)
    for i, th in enumerate(THROWS):                       # 던질 때마다 손에서 금화가 위로 부채꼴로 솟구쳐 화면 위로 빠짐 (→ 화면 위에서 비로 쏟아짐)
        side = -1 if i % 2 == 0 else 1                    # 양옆으로 한 번씩 번갈아 (왼쪽 → 오른쪽 → …)
        rx, ry = rider(th); hx, hy = rx + side * 0.035, ry - RIDE_H * THROW_PAW
        n = 9
        for k in range(n):
            ang = math.radians(side * (15 + 65 * (k + s.rnd.random()) / n)); up = s.rnd.uniform(0.38, 0.55); r0 = s.rnd.uniform(-180, 180)
            x1, y1 = hx + math.sin(ang) * up * 0.7, hy - math.cos(ang) * up
            spr = ('fx_coin', 'fx_coin2')[k % 2]
            s.w(tr, f'Toss{i}_{k}', spr, s.rnd.uniform(0.18, 0.28), [(th - 0.01, hx, hy, 0.4, r0, 0, 'Out'), (th, hx, hy, 0.5, r0, 1, 'Snap'),
                                                                     (th + s.rnd.uniform(0.4, 0.55) / TSPD, x1, y1, 1.0, r0 + 540, 1, 'Out')])
        s.w(tr, f'Shine{i}', 'fx_sparkle', 0.35, [(th - 0.01, hx, hy, 0, 0, 0, 'Out'), (th, hx, hy, 0.6, 0, 1, 'Snap'), (th + 0.16 / TSPD, hx, hy, 1.2, 40, 0, 'Out')])
    pts = [(THROW0 + 0.45 + 0.06 * i, s.rnd.uniform(0.3, 0.75), s.rnd.uniform(0.8, 0.94)) for i in range(int((END + 0.3 - THROW0 - 0.45) / 0.06))]     # 쏟아진 금화가 지붕마다 짤랑
    for i, (t0, x, y) in enumerate(pts):
        s.w(tr, f'Clink{i}', f'fx_clink{1 + i % 3}', 0.3, [(t0 - 0.01, x, y, 0, 0, 0, 'Out'), (t0, x, y, 0.5, 0, 1, 'Snap'), (t0 + 0.12, x, y - 0.01, 1.1, 0, 1, 'Back'), (t0 + 0.35, x, y - 0.02, 1.0, 0, 0, 'In')])
    # 금화 비 (2026-10-20 사용자 3차: 올라간 뒤엔 금화가 비처럼 막 쏟아짐 → 게임 화면에서도 이어짐): 화면 앞 큰 금화 · 점점 거세짐, 덮는 벽 없이 그대로 걷힘
    def rain(parent, name, n, t0, t1, sc0, sc1, dur0, dur1, w=1100, top=640, bottom=-700, ramp=1.6):
        for i in range(n):
            u = s.rnd.random() ** (1 / ramp)                          # 뒤로 갈수록 빽빽하게
            t = t0 + (t1 - t0) * u; x = s.rnd.uniform(-w, w); dur = s.rnd.uniform(dur0, dur1); r = s.rnd.uniform(-180, 180)
            spr = ('fx_coin', 'fx_coinfall', 'fx_coin2')[i % 3]
            sc = s.rnd.uniform(sc0, sc1); rot = 0 if spr == 'fx_coinfall' else r
            s.s(parent, f'{name}{i}', spr, sc, [(t - 0.01, x, top, 1, rot, 0, 'Out'), (t, x, top, 1, rot, 1, 'Snap'),
                                                 (t + dur, x - 90, bottom, 1, rot + (0 if spr == 'fx_coinfall' else 360), 1, 'Linear')])
    rain(g, 'RainB', 130, THROW0 + 0.3, END + 0.4, 0.12, 0.2, 0.65, 0.9, ramp=1.2)                 # 뒤 (작고 느림)
    rain(g, 'RainF', 170, THROW0 + 0.4, END + 0.4, 0.2, 0.42, 0.38, 0.58, ramp=1.5)                 # 앞 (크고 빠름)
    cut_flash(g, T3, 0.5)
    # 밈 (금화 쏟아붓는 동안 화면 아래)
    G = s.group('Top', R, 2.75, COVER)
    s.text(G, 'MemeText', s.story['meme'], 46, (0, -430), [(2.8, 0, -430, 0, -2, 0, 'Out'), (2.92, 0, -430, 1, -2, 1, 'Back'), (COVER, 0, -430, 1, -2, 1, 'Linear')], w=1200)
    return None


# 술탄 v2 그림 속 자리 (그림 비율 · 그림이 바뀌면 다시 잼)
GUSH = (0.5, 0.4)             # shot_oil: 유정탑 꼭대기 (석유가 뿜는 자리)
EYES = (0.4, 0.6, 0.47)       # cu_face: 왼눈 x · 오른눈 x · 눈 y
RIDE_H = 0.3                  # pano_on: 양탄자 탄 쥐 키 (파노라마 높이 비율)
THROW_PAW = 0.8               # anim_ride: 양탄자 바닥 → 금화를 던지는 손 (탄 쥐 키 비율)



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
    """세로 (정면): 네온 천장 → 방송 방 벽(모니터) → 바닥 러그. (2026-10-20 v2, 사용자: 도네 받는 장면 → 고글 '이이잉' 리액션 + 트래킹 줌 → 제로투 느낌 전신 춤, 게임 그림체 최대한)
    저작권: 특정 캐릭터 · 의상 · 음악 없이 '양팔 굽혀 엉덩이 흔드는 춤 동작'만 오마주. 띠 · 마무리 컷 없음 · 플랫 · 정면 셀 프레임 · 0.75배속 — 컷 5개:
    ① 0.4 뒷모습 통이미지: 컴퓨터 앞에서 방송 중 → 띵! 모니터에 도네 알림 · 동전 → 천천히 줌인 → 쥐 주위를 돌듯 왼쪽으로 휙 패닝 + 블러 · 잔상이 화면을 덮음
    ② 1.12 같은 방향 패닝으로 앞모습이 오른쪽에서 밀려 들어옴 → 고글을 쓱 올리면 렌즈가 하얗게 '이이잉' (패러디) → 트래킹 줌으로 얼굴까지
    ③ 1.75 고글 반짝 얼굴 클로즈업 ④ 2.0 바닥 러그 위 전신: 머리 뒤로 깍지 · 엉덩이를 좌우로 흔드는 8박 춤 (박자마다 카메라 반동 · 하트 · 음표 · 색종이 · 네온 고리)
    ⑤ 3.2 손가락 하트 → 하트가 화면을 덮음
    → 컷신 걷힘 → 게임 화면: 하트 · 색종이가 쏟아지며 대상마다 하트 팡 (파괴) · 마무리에 모든 쥐 광란 버프 (buffAll)"""
    END = NO_FINISH['streamrat']

    def slim(parent, k=0.88):                           # 쥐를 가로로만 줄여 날씬하게 (2026-10-20 사용자) — 레이어 키는 고른 배율이라 부모 묶음에 가로 배율
        v = s.v('slim'); s(f'var {v} = Go("Slim", {parent}); {v}.transform.localScale = new Vector3({k:.2f}f, 1f, 1f);'); return f'{v}.transform'

    def cut_flash(g, t0, a=0.5):
        s.s(g, 'CutFlash', 'fx_heartburst', 8.0, [(t0, 0, 0, 1.0, 0, a, 'Snap'), (t0 + 0.09, 0, 0, 1.3, 0, 0, 'Out')])

    MX, MY = 0.5, 0.524                                  # 벽 가운데 큰 모니터 (파노라마 비율)
    # ① 뒷모습 통이미지: 컴퓨터 앞에서 방송 중 → 띵! 모니터에 도네 알림 → 천천히 줌인 → 카메라가 쥐 주위를 돌듯 왼쪽으로 휙 패닝 + 블러 · 잔상이 화면을 덮음
    #    (2026-10-20 사용자: 화면을 기울이는 회전 말고 패닝으로 뒷모습 → 앞모습)
    RS, M = 1.25, (0, 215)                               # 통이미지 배율 (1536 → 1920) · 그림 속 모니터 자리 (화면 px)
    D0, SP0, SP1 = 0.72, 0.9, 1.12                       # 도네 · 패닝 시작 · 끝 (= 앞모습 전환)
    PAN = 2600                                           # 패닝 거리 (px)

    def pan_keys(lag=0.0, a=1.0):
        """천천히 줌인 → 왼쪽으로 휙 (lag = 잔상 지연)"""
        ks = [(0.4, 0, 0, RS, 0, a, 'Out'), (SP0 + lag, 0, -40, RS * 1.12, 0, a, 'InOut')]
        for u in (0.4, 1.0):
            t = SP0 + lag + (SP1 - SP0) * u
            ks.append((t, -PAN * u ** 1.6, -40, RS * (1.12 + 0.18 * u), 0, a, 'In'))
        return ks
    g = s.group('Cut1', R, 0.4, SP1 + 0.08)
    s.s(g, 'Back', 'face_bg', 1.6, [(0.4, 0, 0, 1, 0, 1, 'Out')])      # 패닝으로 통이미지가 빠진 자리에 게임 화면이 비치지 않게 배경판
    s.s(g, 'Rear', 'rear', 1.0, pan_keys(), kicks=[D0], kickPower=18, kickTime=0.2)
    for k, (lag, al) in enumerate(((0.03, 0.5), (0.06, 0.32), (0.09, 0.2))):     # 잔상: 흐린 사본이 늦게 따라옴
        s.s(g, f'Trail{k}', 'rear_blur', 1.0, [(t, x, y, sc, r, (0 if t <= SP0 + lag else al), e) for t, x, y, sc, r, a, e in pan_keys(lag)])
    s.s(g, 'BlurCover', 'rear_blur', 1.0, [(t, x, y, sc, r, (0 if t <= SP0 + 0.04 else 0.85), e) for t, x, y, sc, r, a, e in pan_keys(0.0)])      # 패닝 중 흐림이 화면을 덮음
    s.s(g, 'Ding', 'fx_heartburst', 1.6, [(D0 - 0.01, M[0], M[1], 0, 0, 0, 'Out'), (D0, M[0], M[1], 0.5, 0, 1, 'Snap'), (D0 + 0.12, M[0], M[1], 1.2, 0, 1, 'Back'), (D0 + 0.3, M[0], M[1], 1.4, 0, 0, 'Out')])
    s.burst_ring(g, 'Coin', 'fx_coin', D0 + 0.02, M[0], M[1], n=10, r=900, sc=0.35, dur=0.4)
    s.rise(g, 'Heart', 'fx_heart', [D0 + 0.04 * i for i in range(6)], [-700, -420, -140, 140, 420, 700], -300, 520, sc=0.3, dur=0.3)
    # ② 같은 방향으로 이어지는 패닝: 앞모습이 오른쪽에서 밀려 들어와 멈춤 (쥐 잔상이 뒤따름) → 고글 쓱 → 렌즈가 하얗게 '이이잉' → 트래킹 줌으로 얼굴까지
    G0, G1, G2, Z0, FACE0 = SP1, 1.3, 1.45, 1.48, 1.75
    IN = 0.2                                             # 밀려 들어오는 시간
    g = s.group('Cut2', R, G0, FACE0 + 0.02)
    s.s(g, 'Back', 'face_bg', 1.6, [(G0, 0, 0, 1, 0, 1, 'Out')])      # 밀려 들어오는 동안 빈 곳 가림
    Z = 3.4; fx_, fy_ = 0, 120                          # 고글 (화면 (0, 120) 근처)
    UP0, UP1 = 180, 120                                 # 줌하는 동안 쥐를 화면 위쪽으로 (px, 2026-10-20 사용자: 조금 더 위로) — 1.6 · 2.2배 단계 · 3.4배 얼굴
    K0, K1 = 1.6, 2.2                                   # 배경 · 쥐를 함께 줌 (2026-10-20 사용자: 모니터 배경과 쥐 크기 차이가 티 안 나게 빠르게 줌) — 시작 1.6배 → G1 2.2배 → 얼굴 3.4배
    pn = s.v('pan')
    s(f'var {pn} = E("Pan", {g}, {keys([(G0, PAN, 0, 1.12, 0, 1, "Snap"), (G0 + IN, 0, 0, 1.0, 0, 1, "Out"), (FACE0, 0, 0, 1.0, 0, 1, "Linear")])}).transform;')
    for k, (lag, al) in enumerate(((0.035, 0.45), (0.07, 0.25))):     # 쥐 잔상 (패닝을 늦게 따라옴)
        lg = s.v('lag')
        s(f'var {lg} = G("PanTrail", {g}, {keys([(G0, PAN - fx_ * K0, -fy_ * K0 + UP0, K0 * 1.12, 0, al, "Snap"), (G0 + IN + lag, -fx_ * K0, -fy_ * K0 + UP0, K0, 0, al, "Out"), (G0 + IN + lag + 0.06, -fx_ * K0, -fy_ * K0 + UP0, K0, 0, 0, "Linear")])}).transform;')      # G = CanvasGroup (투명도가 자식에 먹음)
        s.cel(slim(lg), f'Ghost{k}', [(G0, 'anim_goggle_1')], [(G0, 0, -560, 1, 0, 1, 'Out')], height=760, ref='anim_goggle_1', pano=False)
    zv = s.v('zoom')
    s(f'var {zv} = E("Zoom", {pn}, {keys([(G0, -fx_ * K0, -fy_ * K0 + UP0, K0, 0, 1, "Out"), (G1, -fx_ * K1, -fy_ * K1 + UP0, K1, 0, 1, "InOut"), (FACE0 + 0.02, -fx_ * Z, -fy_ * Z + UP1, Z, 0, 1, "In")])}).transform;')
    tr = s.cam(zv, [(G0, MX, MY + 0.03, 1.5, 0, 'Out'), (FACE0, MX, MY + 0.03, 1.6, 0, 'Linear')], kicks=[G1], power=20)      # 배경도 줌 묶음 안 (쥐와 같이 확대)
    s.cel(slim(zv), 'Rat', [(G0, 'anim_goggle_1'), (G1, 'anim_goggle_2'), (G2, 'anim_goggle_3')], [(G0, 0, -560, 1, 0, 1, 'Out')], height=760, ref='anim_goggle_1', pano=False)
    for k, (dx, t0) in enumerate(((-90, G1 + 0.02), (90, G1 + 0.06), (-90, G2 + 0.04), (90, G2 + 0.08))):     # 렌즈 반짝 (이이잉)
        s.s(zv, f'Glint{k}', 'fx_glint', 0.5, [(t0 - 0.01, fx_ + dx, fy_ + 10, 0, 0, 0, 'Out'), (t0, fx_ + dx, fy_ + 10, 1.0, 0, 1, 'Snap'), (t0 + 0.18, fx_ + dx, fy_ + 10, 1.4, 45, 0, 'Out')])
    s.s(g, 'BlurOut', 'rear_blur', RS * 1.3, [(G0, -PAN, -40, 1.0, 0, 0.85, 'Snap'), (G0 + IN, -PAN * 1.6, -40, 1.05, 0, 0, 'Out')])      # 앞 컷의 흐림이 왼쪽으로 빠지며 이어짐
    # ③ 고글 반짝 얼굴 클로즈업 (배경 + 쥐) · 렌즈 반짝 '이이잉'
    g = s.group('Cut3', R, FACE0, 2.0)
    s.face(g, 'full', FACE0, 2.0, bg='face_bg')
    for k, (x, t0) in enumerate(((-260, FACE0 + 0.05), (260, FACE0 + 0.1), (-260, FACE0 + 0.24), (260, FACE0 + 0.28))):
        s.s(g, f'Glint{k}', 'fx_glint', 1.2, [(t0 - 0.01, x, 90, 0, 0, 0, 'Out'), (t0, x, 90, 1.0, 0, 1, 'Snap'), (t0 + 0.2, x, 90, 1.5, 45, 0, 'Out')])
    # ④ 바닥 러그 위 전신: 머리 뒤로 깍지 · 엉덩이를 좌우로 크게 흔드는 춤 (밈 춤 동작만 오마주, 2026-10-20 사용자 참고 영상)
    #    8박 반복: 좌 끝 → 좌 중간 → 가운데(낮게) → 우 중간 → 우 끝 → 우 중간 → 가운데(높게) → 좌 중간 · 박자마다 카메라 반동
    T0, T1, BEAT = 2.0, 3.2, 0.1
    beats = [T0 + BEAT * i for i in range(int((T1 - T0) / BEAT) + 1)]
    LOOP = ['anim_dance_1', 'anim_dance_2', 'anim_dance_3', 'anim_dance2_1', 'anim_dance2_2', 'anim_dance2_1', 'anim_dance2_3', 'anim_dance_2']
    FY, H = 0.875, 560                                  # 발 (러그 앞쪽) · 키 (화면 높이 약 80%)
    g = s.group('Cut4', R, T0, T1)
    tr = s.cam(g, [(T0, 0.5, 0.79, 1.5, 0, 'Out'), (2.6, 0.5, 0.785, 1.42, 0, 'InOut'), (T1, 0.5, 0.79, 1.5, 0, 'InOut')], kicks=beats[::4], power=12, rot=1.2, ktime=0.12)
    s.w(tr, 'Ring', 'fx_neonring', 1.7, [(T0, 0.5, FY - 0.08, 1.0, 0, 0.55, 'Out'), (T1, 0.5, FY - 0.08, 1.1, 0, 0.55, 'Linear')], spin=40.0, pulse=0.06, pulseHz=1 / (4 * BEAT))
    TILT = {'anim_dance_1': 10, 'anim_dance_2': 5, 'anim_dance_3': 0, 'anim_dance2_1': -5, 'anim_dance2_2': -10, 'anim_dance2_3': 0}     # 발끝 축으로 몸을 좌우로 기울여 흔들림 보강 (도, 2026-10-20 사용자: 더 크게)
    SWAY = {'anim_dance_1': -0.014, 'anim_dance_2': -0.007, 'anim_dance_3': 0, 'anim_dance2_1': 0.007, 'anim_dance2_2': 0.014, 'anim_dance2_3': 0}     # 엉덩이 쪽으로 몸이 좌우로 밀림 (파노라마 비율)
    s.cel(slim(tr), 'Rat', [(t, LOOP[i % len(LOOP)]) for i, t in enumerate(beats)],
          [(t, 0.5 + SWAY[LOOP[i % len(LOOP)]] / 0.88, FY, 1.0 if LOOP[i % len(LOOP)] != 'anim_dance_3' else 0.96, TILT[LOOP[i % len(LOOP)]], 1, 'InOut') for i, t in enumerate(beats)], height=H, ref='anim_dance_3')
    s.rise(g, 'Heart', 'fx_heart', [T0 + 0.12 * i for i in range(10)], [(-1) ** i * s.rnd.uniform(420, 880) for i in range(10)], -560, 600, sc=0.3, dur=0.5)      # 몸 양옆으로 (가운데 쥐를 가리지 않게)
    s.rise(g, 'Note', 'fx_note', [T0 + 0.06 + 0.16 * i for i in range(7)], [(-1) ** (i + 1) * s.rnd.uniform(420, 880) for i in range(7)], -560, 560, sc=0.35, dur=0.5)
    s.shower(g, 'Confetti', 'fx_confetti', T0 + 0.2, T1 - 0.2, 8, -900, 900, 640, -600, sc=0.45)
    for i, t in enumerate(beats[::8]):                  # 한 바퀴마다 분홍 번쩍
        s.s(g, f'Beat{i}', 'fx_heartburst', 7.0, [(t, 0, 0, 1.0, 0, 0.1, 'Snap'), (t + 0.1, 0, 0, 1.2, 0, 0, 'Out')])
    cut_flash(g, T0)
    # ⑤ 손가락 하트 → 하트가 화면을 덮음
    g = s.group('Cut5', R, T1)
    tr = s.cam(g, [(T1, 0.5, 0.80, 1.7, 0, 'Out'), (END + 0.4, 0.5, 0.80, 1.95, 0, 'Linear')], kicks=[T1 + 0.05], power=30)
    s.cel(slim(tr), 'Rat', [(T1, 'anim_finish_1')], [(T1, 0.5, FY, 1, 0, 1, 'Out'), (END, 0.5, FY, 1.05, 0, 1, 'Linear')], height=H, ref='anim_dance_3')
    s.s(g, 'Cover', 'fx_heartburst', 2.0, [(END - 0.2, 0, 60, 0.3, 0, 0, 'Out'), (END - 0.15, 0, 60, 1.0, 0, 1, 'Snap'), (END + 0.4, 0, 60, 8.0, 0, 1, 'In')])
    cut_flash(g, T1)
    G = s.group('Top', R, 2.05)
    s.meme(G, 2.1, side=-1, y=440)
    return None


def ramjui_v1(s, R):
    """(v1, 2026-10-18 · 안 씀) 세로: 폭풍 하늘 → 거대 참나무 → 숲 바닥. ② 바닥에서 나무 꼭대기까지 다람쥐를 쫓아 쭉 올라감 (다람쥐도 같이) · ③ 오른쪽으로 기운 띠 + 도토리 벼락이 두 개씩 · 나뭇잎 폭풍이 가로로 · ④ 하늘에서 한 바퀴 돌며(배럴 롤) 숲으로 내리꽂힘"""
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


def ramjui(s, R):
    """람쥐썬더 짤 몽타주 (2026-10-20 v4, 사용자: 초반엔 우리 게임 그림체 → 빨라질수록 실사 짤 · 그림체가 점점 실사에 가까워지게 · 마지막 전신샷 = 너굴맨 패러디 실사 다람쥐).
    짤 = Codex 재현 (ramjui/Sheets/v2: gen_memes.py · gen_memes2.py · prompt_bg_meme19, 실제 짤은 저작권 때문에 안 씀). 띠 · 마무리 컷 없음:
    ① 0.4 게임 그림체 람쥐: 번개 들판에서 놀람 → 두 손 앞으로 → 두 팔 번쩍
    ② 0.85 짤 몽타주 19장 디졸브, 점점 빨라짐 (0.36초 → 0.10초) · 그림체도 6단계로 천천히 실사로 (2026-10-20 사용자: 중간 반실사 단계가 부족):
       A 게임 플랫 → A+ 플랫 + 부드러운 음영 → B 동화 유화 → B+ 반실사 디지털 페인팅 → C 3D 영화풍 → C+ 포토리얼 CG → D 사진 합성 짤
    ③ 몽타주 끝: 너굴맨 패러디 (흰 스튜디오 실사 다람쥐 전신 · 손 내밂) 정지 + "…람쥐썬더가 처리했으니 안심하라구!" → 번쩍 → 게임 화면 번개"""
    END = NO_FINISH['ramjui']
    cap = s.story.get('captions', {})
    BGS = 1920 / 1536 * 1.06                                   # 가로 통그림 1536 → 화면 꽉 (줌 · 흐름에도 빈틈 없게)

    # ① 게임 그림체 람쥐
    T0, Z2, Z3, T1 = 0.4, 0.55, 0.7, 0.95
    FY, H = -470, 640
    g = s.group('Rat', R, T0, T1)
    pn = s.v('push')
    s(f'var {pn} = E("Push", {g}, {keys([(T0, 0, 0, 1.0, 0, 1, "Out"), (T1, 0, 0, 1.08, 0, 1, "Linear")])}).transform;')
    s.s(pn, 'Field', 'bg_zap', BGS, [(T0, 0, 0, 1, 0, 1, 'Out')])
    s.cel(pn, 'Ramjui', [(T0, 'anim_zap_1'), (Z2, 'anim_zap_2'), (Z3, 'anim_zap_3')],
          [(T0, 0, FY - 40, 0.94, 0, 1, 'Out'), (T0 + 0.08, 0, FY, 1.0, 0, 1, 'Back'), (Z2, 0, FY, 0.97, 0, 1, 'Out'), (Z2 + 0.05, 0, FY, 1.03, 0, 1, 'Back'),
           (Z3, 0, FY, 0.96, 0, 1, 'Out'), (Z3 + 0.06, 0, FY, 1.08, 0, 1, 'Back'), (T1, 0, FY, 1.1, 0, 1, 'Linear')], height=H, ref='anim_zap_1', pano=False)
    s.s(pn, 'Arc', 'fx_arc', 0.8, [(Z2, 0, FY + 300, 0.6, 0, 0, 'Out'), (Z2 + 0.03, 0, FY + 300, 1, 0, 1, 'Snap'), (Z3, 0, FY + H + 40, 1.3, 0, 1, 'Out'), (T1, 0, FY + H + 60, 1.4, 0, 1, 'Linear')], flicker=0.35, jitter=6.0)
    for i, (x, r) in enumerate(((-150, 25), (150, -25))):
        s.s(pn, f'Hand{i}', 'fx_bolt', 1.0, [(Z3 - 0.01, x, FY + H + 380, 1, r, 0, 'Out'), (Z3, x, FY + H + 380, 1, r, 1, 'Snap'), (T1, x, FY + H + 400, 1.05, r, 1, 'Linear')], flicker=0.3, jitter=4.0)

    # ② 짤 몽타주: 그림체가 점점 실사로 · 점점 빨라짐
    ORDER = ['mA1', 'mA2', 'mA4', 'mA5',                       # A 게임 플랫
             'mP1',                                             # A+ 플랫 + 부드러운 음영 (mP2 공중 회전 뺌 — 2026-10-20 사용자)
             'mB1',                                             # B 동화 유화 (mB2 파란 눈 클로즈업 뺌 — 2026-10-20 사용자 · mB4 네 마리 단체 컷 뺌: mQ3 단체 컷과 겹쳐 어색 — 2026-10-20 사용자)
             'mQ1', 'mQ2', 'mQ3',                               # B+ 반실사 디지털 페인팅
             'mC1', 'mC2',                                      # C 3D 영화풍
             'mR1', 'mR2',                                      # C+ 포토리얼 CG
             'meme01', 'meme03', 'meme09', 'meme11', 'meme15', 'meme14']     # D 사진 합성 짤 (키보드 meme15 = 전깃줄 다음 ≈ 4.8~4.9초, 2026-10-20 사용자)
    N = len(ORDER)
    t = 0.85
    for i, m in enumerate(ORDER):
        d = 0.10 + 0.26 * (1 - i / (N - 1)) ** 1.2              # 한 장 보이는 시간 0.36 → 0.10 (2026-10-20 사용자: 전환이 너무 빠름)
        xf = min(0.15, d * 0.5)                                # 디졸브 길이 (빨라질수록 짧게 · 앞쪽은 길게 겹쳐 그림체가 스르르 바뀜)
        a0, a1 = t - xf, t + d + xf
        dx = 40 if i % 2 else -40                              # 방향 번갈아 살짝 흐름
        fx = dict(flicker=0.25) if m == 'meme13' else {}       # 번쩍이는 '찌리찌리' 판
        s.s(R, f'Meme_{m}', f'bg_{m}', BGS, [(a0, -dx, 0, 1.0, 0, 0, 'Out'), (t, -dx * 0.6, 0, 1.025, 0, 1, 'Linear'), (a1, dx, 0, 1.1, 0, 1, 'Linear'), (a1 + 0.02, dx, 0, 1.1, 0, 0, 'Snap')], **fx)
        t += d
    M1 = t

    # ③ 너굴맨 패러디 전신샷 (정지) + 대사 → 번쩍
    g = s.group('Nugul', R, M1 - 0.04)
    s.s(g, 'Studio', 'bg_meme19', BGS, [(M1 - 0.04, 0, 0, 1.08, 0, 0, 'Out'), (M1, 0, 0, 1.04, 0, 1, 'Snap'), (END, 0, 0, 1.0, 0, 1, 'Linear')], kicks=[M1], kickPower=20, kickTime=0.15)
    s.s(g, 'Flash', 'fx_zapburst', 8.0, [(M1, 0, 0, 1.0, 0, 0.6, 'Snap'), (M1 + 0.08, 0, 0, 1.3, 0, 0, 'Out')])
    if 'nugul' in cap:
        y = -400
        s.text(g, 'Line', cap['nugul'], 64, (0, y), [(M1 + 0.06, 0, y, 0, 0, 0, 'Out'), (M1 + 0.07, 0, y, 1.4, 0, 1, 'Snap'), (M1 + 0.17, 0, y, 1.0, 0, 1, 'Back')], w=1500)
    s.s(g, 'Cover', 'fx_zapburst', 2.0, [(END - 0.1, 0, 0, 0.3, 0, 0, 'Out'), (END - 0.07, 0, 0, 1.0, 0, 1, 'Snap'), (END + 0.4, 0, 0, 8.0, 0, 1, 'In')])
    return None


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


def starchef_v1(s, R):
    """(v1, 2026-10-18 · 안 씀) 가로: 식료품 창고 → 주방 → 홀. ② 칸마다 멈추는 휙휙 팬 (창고 치즈 → 주방 불 쇼 → 홀) · ③ 평행한 두 사선 띠 + 프라이팬이 쿵쿵 + 접시가 빙글 · ④ 클로슈 로우앵글 밀어 들어가기 + 별 세 개"""
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


def starchef(s, R):
    """라따뚜이 시식 (2026-10-20 v2, 사용자: 게임 그림체 · Codex). 띠 · 마무리 컷 없음 · 플랫 · 컷마다 한 장 그림(shot_*) + 같은 틀 연속 프레임 바꿔 끼우기 — 컷 6개:
    ① 0.4 쥐 아래 한쪽 무릎 꿇고 고개 숙인 요리사가 요리를 바침: 아래에서 카메라 몸체가 그대로 올라감 (페데스탈 · 앞 냄비 시차)
    ② 벌벌 떨리는 손 위 라따뚜이 클로즈업 ③ 눈 감은 거만한 쥐가 손가락으로 찍어 맛봄 → 얼굴로 급줌 → 눈 번쩍
    ④ 가로 블러 움직임이 화면을 가림 → 사람 머리 위에 올라탄 샷 ⑤ 머리카락을 마구 뜯어 대머리로 ⑥ 기절한 사람 위에서 브이 (로우 앵글) · 글 없음
    → 컷신 걷힘 → 게임 화면: 대상마다 머리카락이 떨어지고 기절 별 (worldPhase · imageFx)"""
    END = 5.4                                             # 늦추기 전 시각 (아래 SLOW 로 늘린 끝 = NO_FINISH 6.41)
    o0 = len(s.o)
    K = 1920 * 1.04 / 1536                                # 가로 한 장 컷 배율 (화면을 덮음)

    def at(rx, ry, z=1.0):
        """가로 컷 그림 비율 → 화면 px (그림이 가운데 · 배율 z)"""
        return (rx - 0.5) * 1536 * K * z, (0.5 - ry) * 1024 * K * z

    def look(rx, ry, z):
        """그림 (rx, ry) 가 화면 가운데 오게 하는 레이어 자리 (가장자리가 드러나지 않게 제한)"""
        x, y = at(rx, ry, z); mx, my = max(0, (1536 * K * z - 1920) / 2), max(0, (1024 * K * z - 1080) / 2)
        return -max(-mx, min(mx, x)), -max(-my, min(my, y))

    def cut_flash(g, t0, a=0.5):
        s.s(g, 'CutFlash', 'fx_twinkle', 12.0, [(t0, 0, 0, 1.0, 0, a, 'Snap'), (t0 + 0.08, 0, 0, 1.3, 0, 0, 'Out')])

    def frames(g, name, seq, sc, ks, **fx):
        var = s.s(g, name, seq[0][1], sc, ks, **fx)
        sw = ', '.join(f'new NKK.Ults.SuperUltLayer.Swap {{ t = {f(t)}, sprite = S("{spr}") }}' for t, spr in seq[1:])
        if sw: s(f'{var}.swaps = new[] {{ {sw} }};')
        return var

    # ① 무릎 꿇고 바침: 아래(고개 숙인 요리사 · 접시) → 위(카운터 위 거만한 쥐), 시선 높이 그대로 몸체만 올라감
    T1 = 1.55
    bw, bh = s.size('shot_offer'); BK = 1920 * 1.04 / bw; PANY = (bh * BK - 1080) / 2
    g = s.group('Cut1', R, 0.4, T1)
    rx, ry = OFFER_RAT; TOPY = -(0.5 - ry) * bh * BK                 # 끝 = 쥐 얼굴 높이가 화면 가운데
    s.s(g, 'Offer', 'shot_offer', BK, [(0.4, 0, PANY, 1, 0, 1, 'Out'), (0.58, 0, PANY - 30, 1, 0, 1, 'Linear'), (1.32, 0, TOPY - 25, 1, 0, 1, 'InOut'), (1.42, 0, TOPY + 6, 1, 0, 1, 'Out'), (T1, 0, TOPY, 1, 0, 1, 'Linear')])
    s.s(g, 'Sweat', 'prop_sweat', 0.35, [(0.45, 260, -150, 0.6, 20, 0, 'Out'), (0.5, 260, -150, 1, 20, 1, 'Back'), (0.75, 300, -320, 1, 30, 0, 'In')])
    for i, (x, sc, k) in enumerate(((-900, 2.0, 1.9), (930, 2.8, 2.6))):     # 앞 냄비: 몸체가 올라가면 더 빨리 내려감 (시차)
        y0 = 1300 + i * 500
        s.s(g, f'Pots{i}', 'prop_fg', sc, [(0.4, x, y0, 1, 0, 1, 'Out'), (0.58, x, y0 - 20, 1, 0, 1, 'Linear'), (1.32, x, y0 - (PANY - TOPY) * k, 1, 0, 1, 'InOut'), (T1, x, y0 - (PANY - TOPY) * k, 1, 0, 1, 'Linear')])
    ox, oy = (rx - 0.5) * bw * BK, 0
    s.s(g, 'Shine', 'fx_twinkle', 0.5, [(1.36, ox - 120, oy + 160, 0, 0, 0, 'Out'), (1.4, ox - 120, oy + 160, 1.1, 0, 1, 'Back'), (T1, ox - 130, oy + 170, 0.9, 50, 0, 'Out')])
    # ② 벌벌 떨리는 손 위 라따뚜이 — ①과 같은 측면 시점 (2026-10-20 사용자: 하이앵글 말고 측면 · 연속성). 같은 주방 배경판은 가만히, 손 · 접시만 떨림 · 천천히 다가감
    T2 = 2.15
    g = s.group('Cut2', R, T1, T2)
    s.s(g, 'Back', 'bg_dish', K, [(T1, 0, 0, 1.0, 0, 1, 'Out'), (T2, 0, 0, 1.04, 0, 1, 'Linear')])
    DY = -170                                             # 손 · 접시를 화면 아래로 (음식이 위에 잘리지 않게)
    s.s(g, 'Dish', 'cu_dish', K, [(T1, 0, DY, 1.0, 0, 1, 'Out'), (T2, 0, DY + 20, 1.08, 0, 1, 'Linear')], jitter=9.0)
    px_, py_ = at(*DISH_TOP); py_ += DY                              # 접시 위 (김이 오르는 자리)
    for i in range(3):
        t0 = T1 + 0.08 + 0.15 * i; x = px_ - 140 + 140 * i
        s.s(g, f'Steam{i}', 'prop_steam', 0.5, [(t0, x, py_, 0.7, 0, 0, 'Out'), (t0 + 0.1, x, py_ + 100, 0.9, 0, 0.8, 'Linear'), (t0 + 0.4, x + 30, py_ + 360, 1.1, 10, 0, 'Linear')])
    for i, (dx, side) in enumerate(((-300, -1), (260, 1), (-160, -1))):
        t0 = T1 + 0.1 + 0.13 * i; x = px_ + dx; y = py_ - 120
        s.s(g, f'Sweat{i}', 'prop_sweat', 0.3, [(t0, x, y, 0.6, side * 20, 0, 'Out'), (t0 + 0.03, x, y, 1, side * 20, 1, 'Snap'), (t0 + 0.3, x + side * 160, y - 260, 1, side * 60, 0, 'In')])
    cut_flash(g, T1, 0.35)
    # ③ 손가락으로 찍어 맛봄 (눈 감음) → 얼굴로 급줌 → 눈 번쩍
    TA, TL, Z0, Z1, TO = T2, 2.5, 2.72, 2.82, 2.86; CUT = 3.05
    fx_, fy_ = TASTE_FACE; ZF = 2.3
    g = s.group('Cut3', R, TA, CUT)
    p0 = look(0.5, 0.5, 1.0); p1 = look(fx_, fy_, 1.12); p2 = look(fx_, fy_, ZF)
    frames(g, 'Taste', [(TA, 'shot_taste1'), (TL, 'shot_taste2'), (TO, 'shot_taste3')], K,
           [(TA, *p0, 1.0, 0, 1, 'Out'), (Z0, *p1, 1.12, 0, 1, 'Linear'), (Z1, *p2, ZF, -2, 1, 'In'), (Z1 + 0.05, *p2, ZF * 1.03, 0, 1, 'Out'), (CUT, *p2, ZF * 1.07, 0, 1, 'Linear')],
           kicks=[TO], kickPower=36, kickTime=0.22)
    for i in range(12):                                   # 급줌 속도선 (가장자리 → 가운데)
        a = math.radians(i * 30 + 15); s0 = Z0 + (i % 3) * 0.015; rot = math.degrees(a) + 90
        s.s(g, f'Zoom{i}', 'fx_streak', 1.3, [(s0, math.cos(a) * 1300, math.sin(a) * 800, 1.2, rot, 0, 'Out'), (s0 + 0.02, math.cos(a) * 1150, math.sin(a) * 700, 1.2, rot, 0.85, 'Linear'), (Z1 + 0.04, math.cos(a) * 480, math.sin(a) * 300, 0.7, rot, 0, 'Out')], flicker=0.2)
    for k, (dx, dy) in enumerate(((-180, -75), (150, -30))):     # 눈 번쩍 (급줌 뒤 화면 px)
        s.s(g, f'Eye{k}', 'fx_twinkle', 0.6, [(TO - 0.01, dx, dy, 0, 0, 0, 'Out'), (TO, dx, dy, 0.6, 0, 1, 'Snap'), (TO + 0.1, dx, dy, 1.4, 30, 1, 'Back'), (TO + 0.3, dx, dy, 1.6, 60, 0, 'Out')])
    # ④ 올라타기 = 쥐 · 사람 따로 (2026-10-20 사용자: 화면 전환 블러로는 올라타는 느낌이 안 남 → 쥐가 휙 지나간 뒤 머리 위에 나타나야)
    #    딱 컷 → 아직 모르는 사람(shot_ride0) → 블러 쥐(pose_leap_mblur)가 왼쪽 아래 → 오른쪽 위로 휙 지나감 → 한 박자 → 쥐(cu_ratride)가 위에서 떨어져 머리에 착지 · 쾅 → 원래 컷(shot_ride1: 놀란 얼굴)으로 바꿔 끼움
    W0, W1, DROP0, LAND = 3.09, 3.21, 3.24, 3.35
    g = s.group('Cut4', R, CUT, 4.45)
    hx, hy = RIDE_HEAD; TR2, TR3, T4 = 3.62, 4.05, 4.45
    HX, HY = at(hx, hy)
    frames(g, 'Ride', [(CUT, 'shot_ride0'), (LAND + 0.02, 'shot_ride1'), (TR2, 'shot_ride2'), (TR3, 'shot_ride3')], K,
           [(CUT, 0, 0, 1.03, 0, 1, 'Out'), (LAND, 0, 0, 1.0, 0, 1, 'Linear'), (TR3, 0, 0, 1.04, 0, 1, 'Linear'), (TR3 + 0.06, 0, 0, 1.1, 0, 1, 'Snap'), (T4, 0, 0, 1.12, 0, 1, 'Linear')],
           kicks=[LAND] + [TR2 + 0.1 * i for i in range(4)] + [TR3], kickPower=34, kickTime=0.14)
    x0, y0, x1, y1 = -1300, -720, HX + 520, 900           # 휙: 왼쪽 아래(접시 쪽) → 머리 위를 스쳐 오른쪽 위로 빠짐
    ang = math.degrees(math.atan2(y1 - y0, x1 - x0))
    lk = s.hp('pose_leap', 640)
    s.s(g, 'Leap', 'pose_leap_mblur', lk, [(W0 - 0.01, x0, y0, 0.9, ang, 0, 'Out'), (W0, x0, y0, 0.9, ang, 1, 'Snap'), (W1, x1, y1, 1.15, ang, 1, 'Linear'), (W1 + 0.01, x1, y1, 1.15, ang, 0, 'Linear')])
    for i in range(6):                                    # 지나간 길을 따라 속도선
        off = (i - 2.5) * 70; t0 = W0 + 0.015 * i
        ox, oy = -math.sin(math.radians(ang)) * off, math.cos(math.radians(ang)) * off
        u0, u1 = 0.15 + 0.05 * (i % 3), 0.75
        s.s(g, f'Speed{i}', 'fx_streak', 2.2, [(t0, x0 + (x1 - x0) * u0 + ox, y0 + (y1 - y0) * u0 + oy, 0.6, ang, 0, 'Out'), (t0 + 0.03, x0 + (x1 - x0) * (u0 + 0.1) + ox, y0 + (y1 - y0) * (u0 + 0.1) + oy, 1.2, ang, 0.7, 'Linear'),
                                                  (W1 + 0.08, x0 + (x1 - x0) * u1 + ox, y0 + (y1 - y0) * u1 + oy, 1.4, ang, 0, 'Out')], flicker=0.3)
    s.s(g, 'Swoosh', 'fx_smear', 3.0, [(W0 + 0.02, (x0 + x1) / 2 - 300, (y0 + y1) / 2 - 250, 0.8, ang, 0, 'Out'), (W0 + 0.05, (x0 + x1) / 2 - 150, (y0 + y1) / 2 - 120, 1.0, ang, 0.45, 'Linear'), (W1 + 0.1, (x0 + x1) / 2 + 250, (y0 + y1) / 2 + 200, 1.2, ang, 0, 'Out')])
    # 위에서 떨어져 착지 (cu_ratride = shot_ride1 과 같은 틀 → 착지 뒤 원래 컷으로 바꿔 끼워도 자리가 같음)
    s.s(g, 'RatDrop', 'cu_ratride', K, [(DROP0 - 0.01, 0, 520, 1, 0, 0, 'Out'), (DROP0, 0, 520, 1, 0, 1, 'Snap'), (LAND, 0, -18, 1, 0, 1, 'Linear'), (LAND + 0.02, 0, 0, 1, 0, 1, 'Out'), (LAND + 0.03, 0, 0, 1, 0, 0, 'Snap')])
    for i in range(3):                                    # 떨어지는 쥐 위 짧은 세로 속도선
        x = HX - 120 + 120 * i
        s.s(g, f'Drop{i}', 'fx_streak', 1.0, [(DROP0, x, HY + 900, 1, 90, 0, 'Out'), (DROP0 + 0.02, x, HY + 700, 1, 90, 0.6, 'Linear'), (LAND, x, HY + 260, 1, 90, 0, 'In')])
    s.burst_ring(g, 'Land', 'fx_strands', LAND, HX, HY, n=6, r=380, sc=0.35, dur=0.3)     # 착지 순간 머리카락이 들썩
    s.s(g, 'LandRing', 'fx_twinkle', 0.9, [(LAND - 0.01, HX, HY + 40, 0, 0, 0, 'Out'), (LAND, HX, HY + 40, 0.6, 0, 1, 'Snap'), (LAND + 0.15, HX, HY + 40, 1.4, 30, 0, 'Out')])
    # ⑤ 머리카락을 마구 뜯음 → 대머리
    for w_ in range(5):                                   # 뜯을 때마다 머리카락 뭉치가 튐
        t0 = TR2 + 0.08 * w_
        for j in range(3):
            a = math.radians(s.rnd.uniform(30, 150)); r = s.rnd.uniform(420, 800)
            spr = 'fx_tuft' if (w_ + j) % 2 else 'fx_strands'
            s.s(g, f'Tuft{w_}_{j}', spr, s.rnd.uniform(0.35, 0.55), [(t0 - 0.01, HX, HY, 0.4, 0, 0, 'Out'), (t0, HX, HY, 0.6, 0, 1, 'Snap'),
                                                                       (t0 + 0.3, HX + math.cos(a) * r, HY + math.sin(a) * r * 0.7, 1, s.rnd.uniform(-200, 200), 1, 'Out'), (t0 + 0.75, HX + math.cos(a) * r * 1.1, HY - 600, 1, s.rnd.uniform(-300, 300), 0, 'In')])
    s.shower(g, 'Fall', 'fx_strands', TR2 + 0.2, T4, 10, -900, 900, 650, -620, sc=0.35)
    s.s(g, 'Bald', 'fx_twinkle', 0.7, [(TR3 + 0.05, HX + 90, HY - 40, 0, 0, 0, 'Out'), (TR3 + 0.08, HX + 90, HY - 40, 1.2, 0, 1, 'Back'), (T4, HX + 90, HY - 40, 1.0, 90, 0.6, 'Linear')], pulse=0.08, pulseHz=6.0)
    for k, side in enumerate((-1, 1)):
        s.s(g, f'Tear{k}', 'prop_tear', 0.4, [(TR3 + 0.1, side * 180, HY - 330, 0.5, 0, 0, 'Out'), (TR3 + 0.15, side * 200, HY - 340, 1, side * 15, 1, 'Back'), (T4, side * 260, HY - 520, 1, side * 30, 0, 'In')])
    # ⑥ 기절한 사람 위에서 브이 (로우 앵글): 아래에서 확 들이밀며 기울기 → 멈춤
    V0, V2 = T4, 4.8
    g = s.group('Cut5', R, V0)
    frames(g, 'Vee', [(V0, 'shot_v1'), (V2, 'shot_v2')], K,
           [(V0, 0, -160, 1.35, -7, 1, 'Out'), (V0 + 0.14, 0, 10, 1.04, 1.5, 1, 'Back'), (V2, 0, 0, 1.05, 0, 1, 'Linear'), (V2 + 0.06, 0, 0, 1.09, 0, 1, 'Snap'), (END + 0.4, 0, 0, 1.13, 0, 1, 'Linear')],
           kicks=[V0 + 0.14, V2], kickPower=34, kickTime=0.2)
    dx_, dy_ = at(*V_DIZZY)
    s.s(g, 'Dizzy', 'fx_dizzy', 0.7, [(V0 + 0.1, dx_, dy_, 0.4, 0, 0, 'Out'), (V0 + 0.2, dx_, dy_, 1, 0, 1, 'Back'), (END + 0.4, dx_, dy_, 1, 0, 1, 'Linear')], spin=180.0, pulse=0.05, pulseHz=3.0)
    vx, vy = at(*V_HAND)
    for k in range(3):                                    # 브이 손 반짝
        t0 = V0 + 0.2 + 0.18 * k; x, y = vx + (-90, 110, 20)[k], vy + (60, 120, 220)[k]
        s.s(g, f'Twinkle{k}', 'fx_twinkle', 0.55, [(t0 - 0.01, x, y, 0, 0, 0, 'Out'), (t0, x, y, 0.5, 0, 1, 'Snap'), (t0 + 0.12, x, y, 1.2, 30, 1, 'Back'), (t0 + 0.4, x, y, 1.0, 70, 0, 'Out')])
    s.shower(g, 'Hair', 'fx_strands', V0 + 0.1, END, 8, -900, 900, 650, -620, sc=0.3)
    cut_flash(g, V0, 0.45)
    s.o[o0:] = [slow_line(l, *SLOW) for l in s.o[o0:]]     # 올라타기부터 끝까지 0.7배속 (2026-10-20 사용자)
    return None                                           # 글 없음 (2026-10-20 사용자: 밈 글 · 제목 띠 · 자막 = NO_TEXT)


SLOW = (3.05, 0.7)            # 슈퍼 요리사: 이 시각(올라타기 딱 컷)부터 끝까지 재생 속도 0.7배 (2026-10-20 사용자)


def slow_line(line, t0, rate):
    """C# 줄 하나의 시간 값을 t0 뒤로 1/rate 배 늘림: 키 K(t, …) · 바꿔 끼우기 t · 충격 시각 kicks. 이 구간 레이어(첫 키 ≥ t0)는 계속 효과(spin · pulseHz)를 rate 배, kickTime 을 1/rate 배"""
    import re
    w = lambda t: t if t < t0 - 1e-6 else t0 + (t - t0) / rate
    num = lambda m: f'{w(float(m.group(1))):.2f}f'
    keys_t = [float(x) for x in re.findall(r'K\(([\d.]+)f', line)]
    late = bool(keys_t) and min(t for t in keys_t if t > 0 or len(keys_t) == 1) >= t0 - 1e-6
    line = re.sub(r'(?<=K\()([\d.]+)f', num, line)
    line = re.sub(r'(?<=Swap \{ t = )([\d.]+)f', num, line)
    line = re.sub(r'(kicks = new(?: float)?\[\] \{ )([^}]*)\}', lambda m: m.group(1) + ', '.join(f'{w(float(x.strip().rstrip("f"))):.2f}f' for x in m.group(2).split(',') if x.strip()) + ' }', line)
    if late:
        line = re.sub(r'(\.(?:spin|pulseHz) = )(-?[\d.]+)f', lambda m: f'{m.group(1)}{float(m.group(2)) * rate:.2f}f', line)
        line = re.sub(r'(\.kickTime = )([\d.]+)f', lambda m: f'{m.group(1)}{float(m.group(2)) / rate:.2f}f', line)
    return line


# 슈퍼 요리사 v2 그림 속 자리 (그림 비율 · 그림이 바뀌면 다시 잼)
OFFER_RAT = (0.83, 0.36)      # shot_offer: 카운터 위 쥐 얼굴 (페데스탈이 멈추는 높이)
TASTE_FACE = (0.38, 0.33)      # shot_taste: 쥐 얼굴 (급줌 끝)
RIDE_HEAD = (0.48, 0.36)       # shot_ride: 머리 꼭대기 (쥐가 뜯는 자리)
V_DIZZY = (0.14, 0.47)        # shot_v: 기절한 사람 머리 위 별
V_HAND = (0.38, 0.25)          # shot_v: 브이 손
DISH_TOP = (0.59, 0.13)       # cu_dish: 접시 위 (김)


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


def fondue_v1(s, R):
    """(v1, 2026-10-18 · 안 씀) 세로 (+ 치즈 홍수 판): 천장 → 거대 퐁듀 냄비 → 바닥. ② 냄비 가까이서 거품이 퐁퐁 → 넘쳐 홍수 판으로 바뀌며 바닥까지 · ③ 아래 띠(서핑) + 거대 치즈 파도가 아래에서 차오름 · ④ 늘어난 치즈 포즈에서 아주 크게 물러남"""
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


def fondue(s, R):
    """가로 (트레비 분수 한 장 = 파노라마). (2026-10-20 v2, 사용자: 인게임 그림체 · 아델 오리진식 트래킹 줌 아웃 · 아이스 퀸식 게임 화면 마무리)
    띠 · 마무리 컷 없음 · 플랫 · 클로즈업은 같은 틀 연속 프레임(cu_*) 바꿔 끼우기 · 1배속 — 컷 7개:
    ① 0.4 익스트림 클로즈업: 아래(셔츠)에서 위(얼굴)로 천천히 틸트 업 ② 손 클로즈업 핑거 스냅 (짧게)
    ③ 얼굴 클로즈업: 윙크 → 선글라스 끼움 ④ 급격한 트래킹 줌 아웃: 분수 꼭대기 쥐 얼굴 → 트레비 분수 전체 (흐린 판 · 모이는 속도선)
    ⑤ 전체 샷 1초 프레임 스탑 ⑥ 분수 수조에서 치즈 폭발 + 터지는 순간 사자후식 방사형 줌 블러(pano_zoom1·2 번갈아) · 충격파 3번 · 바깥으로 뻗는 속도선 ⑦ 치즈 벽이 화면을 덮음
    → 컷신 걷힘 → 게임 화면: 치즈가 쏟아져 대상마다 철퍽 (worldPhase · imageFx)"""
    END = NO_FINISH['fondue']
    CU = 1.3                                              # 클로즈업 배율 (1536 → 약 2000px, 화면을 덮음)
    cx = lambda rx: (rx - 0.5) * 1536 * CU                # 클로즈업 그림 비율 → 화면 px
    cy = lambda ry: (0.5 - ry) * 1024 * CU

    def cut_flash(g, t0, a=0.6):
        s.s(g, 'CutFlash', 'fx_snap', 9.0, [(t0, 0, 0, 1.0, 0, a, 'Snap'), (t0 + 0.09, 0, 0, 1.3, 0, 0, 'Out')])

    def frames(g, name, seq, ks, sc=CU, **fx):
        """같은 틀 연속 프레임 (그림만 바꿔 끼움)"""
        var = s.s(g, name, seq[0][1], sc, ks, **fx)
        sw = ', '.join(f'new NKK.Ults.SuperUltLayer.Swap {{ t = {f(t)}, sprite = S("{spr}") }}' for t, spr in seq[1:])
        if sw: s(f'{var}.swaps = new[] {{ {sw} }};')
        return var

    # ① 익스트림 클로즈업 (아래에서 올려다봄): 셔츠 → 턱 → 얼굴로 천천히 틸트 업
    T1 = 1.4
    bw, bh = s.size('body_up'); BK = 1920 * 1.06 / bw; PANY = (bh * BK - 1080) / 2
    g = s.group('Cut1', R, 0.4, T1)
    s.s(g, 'Body', 'body_up', BK, [(0.4, 0, PANY, 1.0, -2, 1, 'Out'), (T1, 0, -PANY, 1.04, 0, 1, 'InOut')])
    s.rise(g, 'Drop', 'fx_drops', [0.5 + 0.11 * i for i in range(8)], [(-1) ** i * s.rnd.uniform(500, 880) for i in range(8)], -600, 600, sc=0.3, dur=0.6)
    # ② 손 클로즈업 핑거 스냅 (짧게): 힘 줌 → 딱! (반짝 · 고리 · 쾅) → 손가락으로 가리킴
    S0, SN, S2 = T1, 1.62, 1.74; T2 = 1.95
    SX, SY = cx(SNAP[0]), cy(SNAP[1])                     # 엄지 · 중지가 닿는 자리
    g = s.group('Cut2', R, S0, T2)
    s.s(g, 'Back', 'face_bg', 1.35, [(S0, 0, 0, 1.08, 0, 1, 'Out'), (T2, 0, 0, 1.0, 0, 1, 'Linear')], kicks=[SN], kickPower=30, kickTime=0.2)
    frames(g, 'Hand', [(S0, 'cu_snap1'), (SN, 'cu_snap2'), (S2, 'cu_snap3')], [(S0, 80, -60, 0.96, 4, 1, 'Out'), (SN, 0, 0, 1.0, 0, 1, 'In'), (SN + 0.05, 0, 0, 1.05, -2, 1, 'Snap'), (T2, 0, 0, 1.03, -1, 1, 'Out')], kicks=[SN], kickPower=40, kickTime=0.18)
    s.s(g, 'Spark', 'fx_snap', 0.9, [(SN - 0.01, SX, SY, 0, 0, 0, 'Out'), (SN, SX, SY, 0.4, 0, 1, 'Snap'), (SN + 0.08, SX, SY, 1.3, 20, 1, 'Back'), (SN + 0.22, SX, SY, 1.5, 40, 0, 'Out')])
    s.s(g, 'Ring', 'fx_shockring', 0.8, [(SN - 0.01, SX, SY, 0.2, 0, 0, 'Out'), (SN, SX, SY, 0.2, 0, 1, 'Snap'), (SN + 0.25, SX, SY, 1.8, 0, 0, 'Out')])
    s.burst_ring(g, 'SnapDrop', 'fx_drops', SN, SX, SY, n=6, r=700, sc=0.3, dur=0.3)
    cut_flash(g, S0, 0.4)
    # ③ 얼굴 클로즈업: 씩 웃음 → 윙크 → 선글라스를 올려 끼움 → 반짝
    F0, FW, FG1, FG2 = T2, 2.12, 2.38, 2.5; T3 = 2.85
    g = s.group('Cut3', R, F0, T3)
    s.s(g, 'Back', 'face_bg', 1.35, [(F0, 0, 0, 1.15, 0, 1, 'Out'), (T3, 0, 0, 1.0, 0, 1, 'Linear')])
    frames(g, 'Face', [(F0, 'cu_face1'), (FW, 'cu_face2'), (FG1, 'cu_face3'), (FG2, 'cu_face4')],
           [(F0, 0, 0, 1.12, -3, 1, 'Snap'), (F0 + 0.12, 0, 0, 1.0, 0, 1, 'Back'), (FG2, 0, 0, 1.0, 0, 1, 'Linear'), (FG2 + 0.06, 0, 10, 1.04, 0, 1, 'Snap'), (T3, 0, 10, 1.06, 0, 1, 'Linear')],
           kicks=[FG2], kickPower=26, kickTime=0.2)
    ex, ey = cx(EYE[0]), cy(EYE[1])
    s.s(g, 'WinkStar', 'fx_snap', 0.35, [(FW, ex - 120, ey + 90, 0, 0, 0, 'Out'), (FW + 0.01, ex - 120, ey + 90, 0.5, 0, 1, 'Snap'), (FW + 0.1, ex - 140, ey + 110, 1.0, 30, 1, 'Back'), (FW + 0.25, ex - 150, ey + 120, 1.0, 60, 0, 'Out')])
    for k, (dx, t0) in enumerate(((0, FG2 + 0.04), (2 * abs(ex), FG2 + 0.1))):     # 선글라스 렌즈 반짝 (왼 → 오)
        s.s(g, f'Glint{k}', 'fx_snap', 0.55, [(t0 - 0.01, ex + dx, ey + 20, 0, 0, 0, 'Out'), (t0, ex + dx, ey + 20, 0.6, 0, 1, 'Snap'), (t0 + 0.18, ex + dx, ey + 20, 1.3, 45, 0, 'Out')])
    cut_flash(g, F0, 0.45)
    # ④~⑦ 트레비 분수 (한 카메라): 아주 빠른 트래킹 줌 아웃 (가까운 판 → 롱샷 판) → 1초 프레임 스탑 → 치즈 폭발 + 크롭 마스크 블러 → 치즈가 덮음
    #    (2026-10-20 사용자: 거의 롱샷까지 · 훨씬 빨리 · 블러는 줌 아웃이 아니라 분수가 터질 때)
    Z0, Z1 = T3, 3.3                                      # 줌 아웃 시작 · 끝(= 프레임 스탑 시작)
    FR0, FR1 = Z1, Z1 + 1.0                               # 프레임 스탑 (1초) · 끝 = 폭발
    BOOM2, BOOM3, COVER = FR1 + 0.16, FR1 + 0.42, 5.0
    FX, FY, RH = 0.5, TOP_Y, RAT_H                        # 분수 꼭대기 쥐 발끝 (가까운 판 비율) · 키 (가까운 판 높이 비율)
    HEADY = FY - RH * 0.72                                # 쥐 얼굴 (줌 아웃 시작점)
    wd = s.story['wide']; ww_, wh_ = s.size('wide_on')    # 롱샷 판: 가까운 판(pano_on)이 롱샷 판 (x, y) 에 scale 배로 들어 있음
    WK = s.PS / wd['scale']                               # 롱샷 판 배율 (가까운 판과 같은 축척)
    WCY = 0.5 + ((wh_ / 2) - (wd['y'] + 512 * wd['scale'])) / (1024 * wd['scale'])     # 롱샷 판 가운데 (가까운 판 비율)
    WCX = 0.5 + ((ww_ / 2) - (wd['x'] + 768 * wd['scale'])) / (1536 * wd['scale'])
    FULL = 1920 / (s.PW * s.PS) * 1.0                     # 다 빠졌을 때 = 분수 판(pano_on) 전체가 화면에 딱 (2026-10-20 사용자: 분수만 전체로)
    FOC = (0.5, 0.64)                                     # 폭발 때 다가가는 곳 (분수)
    g = s.group('Cut4', R, Z0)
    zs = [(0.0, 5.2), (0.05, 3.3), (0.1, 2.1), (0.15, 1.4), (0.21, 1.0), (0.28, FULL * 0.92), (0.38, FULL * 1.01), (Z1 - Z0, FULL)]     # 배율이 기하급수로 줄어듦 (= 휙 빠지는 느낌) · 살짝 더 빠졌다 분수 전체에 멈춤
    path = []
    for i, (dt, z) in enumerate(zs):
        u = min(1, math.log(5.2 / z) / math.log(5.2 / FULL))       # 줌 진행도 → 보는 곳도 같이 (얼굴 → 분수 가운데)
        path.append((Z0 + dt, FX + (0.5 - FX) * u, HEADY + (0.5 - HEADY) * u, z, 7 * (1 - u), 'Linear' if i else 'Out'))
    path += [(FR1, 0.5, 0.5, FULL, 0, 'Linear'),                                                  # 프레임 스탑 (카메라 · 모든 레이어 정지)
             (FR1 + 0.25, FOC[0], FOC[1], FULL * 1.45, 0, 'Out'), (COVER, FOC[0], FOC[1], FULL * 1.52, 0, 'Linear'), (END + 0.4, FOC[0], FOC[1], FULL * 1.62, 0, 'In')]     # 터지면 수조로 확 다가감 (블러를 가까이서)
    tr = s.cam(g, path, kicks=[Z0, FR1, FR1 + 0.13, FR1 + 0.26], power=50, rot=2.5, ktime=0.32, )     # 롱샷 판(wide_on)은 분수 판 아래에 같은 축척으로 깔림 (살짝 더 빠질 때 가장자리)
    wl = s.w(tr, 'Wide', 'wide_on', WK, [(Z0, WCX, WCY, 1, 0, 1, 'Out')])
    s(f'{wl}.transform.SetAsFirstSibling();')                                                     # 가까운 판 아래
    # 사자후식 크롭 블러 (2026-10-20 사용자): 터지는 순간 폭발 점에서 바깥으로 뻗는 방사형 줌 블러 (가운데 선명) · 세기 두 장을 번갈아 = 파동처럼 떨림
    RX, RY = 0.5, 0.72                                    # 블러 · 파동 중심 (= slice zoomBlur 가운데)
    rk = [(FR1 - 0.01, 0.5, 0.5, 1, 0, 0, 'Out'), (FR1 + 0.02, 0.5, 0.5, 1, 0, 1, 'Out'), (COVER, 0.5, 0.5, 1, 0, 1, 'Linear'), (END + 0.4, 0.5, 0.5, 1, 0, 1, 'Linear')]
    zl = s.w(tr, 'Roar', 'pano_zoom2', s.PS, rk, pulse=0.015, pulseHz=14.0)
    sw = ', '.join(f'new NKK.Ults.SuperUltLayer.Swap {{ t = {f(FR1 + 0.05 * i)}, sprite = S("pano_zoom{1 if i % 2 else 2}") }}' for i in range(1, int((END - FR1) / 0.05)))
    s(f'{zl}.swaps = new[] {{ {sw} }};')
    # ⑥ 치즈 폭발 (수조에서 도시보다 높이 솟구침, 프레임 = 같은 바닥선)
    for k, (gx, gh, lag) in enumerate(GEYSERS):        # 양옆 큰 기둥 + 가운데 낮은 기둥 (쥐를 가리지 않게)
        s.cel(tr, f'Geyser{k}', [(FR1 + lag, 'fx_geyser1'), (BOOM2 + lag, 'fx_geyser2'), (BOOM3 + lag, 'fx_geyser3')],
              [(FR1 + lag - 0.01, gx, BY, 0.4, 0, 0, 'Out'), (FR1 + lag, gx, BY, 0.6, 0, 1, 'Snap'), (BOOM2 + lag, gx, BY, 1.0, 0, 1, 'Back'), (COVER, gx, BY, 1.08, 0, 1, 'Linear')], height=gh * s.PH * s.PS, ref='fx_geyser2', jitter=2.0)
    rv = s.pose(tr, 'Rat', 'pose_top', RH * s.PH * s.PS, [(Z0, FX, FY, 1, 0, 1, 'Out')])
    s(f'((RectTransform){rv}.transform).pivot = new Vector2(0.5f, 0f);')                       # 발끝 기준
    FXK = 1.4                                             # 폭발 이펙트 크기
    s.w(tr, 'Burst', 'fx_splash', 0.7, [(FR1 - 0.01, BX, BY - 0.04, 0, 0, 0, 'Out'), (FR1, BX, BY - 0.04, 0.5, 0, 1, 'Snap'), (FR1 + 0.15, BX, BY - 0.04, 1.6, 0, 1, 'Back'), (FR1 + 0.45, BX, BY - 0.04, 2.0, 20, 0, 'Out')], spin=30.0)
    for k in range(3):                                    # 사자후 파동: 충격파 고리 3번
        t0 = FR1 + 0.13 * k
        s.w(tr, f'BRing{k}', 'fx_shockring', 3.0, [(t0 - 0.01, RX, RY, 0.2, 0, 0, 'Out'), (t0, RX, RY, 0.2, 0, 0.9, 'Snap'), (t0 + 0.45, RX, RY, 3.2, 0, 0, 'Out')])
    for k, side in enumerate((-1, 1, -1, 1)):            # 물보라 양옆
        x1 = BX + side * (0.25 + 0.12 * (k // 2)); t0 = FR1 + 0.03 * k
        s.w(tr, f'Spray{k}', 'fx_spray', 0.8, [(t0, BX, BY, 0.3, 0, 0, 'Out'), (t0 + 0.01, BX, BY, 0.5, side * 20, 1, 'Snap'), (t0 + 0.35, x1, BY - 0.2, 1.2, side * 50, 1, 'Out'), (t0 + 0.6, x1, BY + 0.05, 1.2, side * 70, 0, 'In')])
    for w_ in range(3):                                   # 사자후 속도선: 폭발 점에서 바깥으로 3물결
        for i in range(14):
            ang = math.radians(i * 360 / 14 + w_ * 9); t0 = FR1 + 0.13 * w_ + (i % 2) * 0.02; rot = math.degrees(ang) + 90
            ox, oy = 0, -150                              # 폭발 점 (화면 px, 카메라가 수조로 다가간 뒤)
            s.s(g, f'Roar{w_}_{i}', 'fx_streak', 1.6, [(t0 - 0.01, ox + math.cos(ang) * 200, oy + math.sin(ang) * 200, 0.8, rot, 0, 'Out'), (t0, ox + math.cos(ang) * 200, oy + math.sin(ang) * 200, 0.8, rot, 0.9, 'Snap'),
                                                         (t0 + 0.3, ox + math.cos(ang) * 1300, oy + math.sin(ang) * 900, 1.6, rot, 0, 'Out')])
    s.shower(g, 'Rain', 'fx_drop', BOOM2, COVER, 14, -950, 950, 700, -620, sc=0.4)
    s.burst_ring(g, 'Blob', 'fx_drops', BOOM2, 0, 60, n=10, r=1100, sc=0.45, dur=0.5)
    for i in range(12):                                   # 줌 아웃 속도선: 화면 가장자리에서 가운데로 모임
        a = math.radians(i * 30 + 15); s0 = Z0 + (i % 3) * 0.03; rot = math.degrees(a) + 90
        s.s(g, f'Zoom{i}', 'fx_streak', 1.3, [(s0, math.cos(a) * 1300, math.sin(a) * 800, 1.2, rot, 0, 'Out'), (s0 + 0.04, math.cos(a) * 1100, math.sin(a) * 680, 1.2, rot, 0.8, 'Linear'),
                                                (Z1 - 0.05, math.cos(a) * 420, math.sin(a) * 260, 0.7, rot, 0, 'Out')], flicker=0.2)
    # (멈추는 순간 하얀 번쩍 없음 — 2026-10-20 사용자)
    cut_flash(g, Z0, 0.55)
    # ⑦ 치즈 벽이 위 · 아래에서 화면을 덮음 (→ 걷히면 게임 화면에 치즈가 쏟아짐)
    ww = 4200 / max(1, s.size('fx_wall')[0])
    s.s(g, 'WallTop', 'fx_wall', ww, [(COVER - 0.01, 0, 1500, 1, 0, 0, 'Out'), (COVER, 0, 1500, 1, 0, 1, 'Snap'), (END, 0, 230, 1.05, -2, 1, 'In'), (END + 0.4, 0, 180, 1.1, -2, 1, 'Linear')], jitter=2.0)
    s.s(g, 'WallBottom', 'fx_wall', ww, [(COVER + 0.05, 0, -1500, 1, 180, 0, 'Out'), (COVER + 0.06, 0, -1500, 1, 180, 1, 'Snap'), (END, 0, -230, 1.05, 182, 1, 'In'), (END + 0.4, 0, -180, 1.1, 182, 1, 'Linear')], jitter=2.0)
    s.s(g, 'Splat', 'fx_splash', 1.0, [(END - 0.12, 0, 0, 0.5, 0, 0, 'Out'), (END - 0.08, 0, 0, 2.0, 0, 1, 'Snap'), (END + 0.4, 0, 0, 7.0, 30, 1, 'In')])
    # (글 없음 — 2026-10-20 사용자: 밈 글 · 제목 띠 · 자막 · 히트 수 전부 뺌, NO_TEXT)
    return None


# 치즈 퐁듀 v2 그림 속 자리 (그림 비율 · 그림이 바뀌면 다시 잼)
SNAP = (0.45, 0.28)           # cu_snap: 엄지 · 중지가 닿는 자리
EYE = (0.36, 0.45)            # cu_face: 화면 왼쪽 눈 (윙크하는 눈)
TOP_Y, RAT_H = 0.585, 0.2    # pano_on: 분수 가운데 바위 꼭대기 (쥐 발끝) · 쥐 키
BX, BY = 0.5, 0.8             # pano_on: 수조 가운데 (폭발 바닥)
GEYSERS = [(0.2, 1.0, 0.0), (0.8, 1.0, 0.05), (0.5, 0.42, 0.1)]     # 치즈 기둥 (x · 높이 (파노라마 높이 비율) · 늦게 시작)


def bomber(s, R):
    """세로 (정면): 붉은 노을 하늘 → 황무지 지평선 → 절벽 위. (2026-10-20 v2, 사용자: 빨간 마법진 여러 겹을 쌓아 올림 → 위에서부터 익스플로전 레이저 → 큰 폭발 → 버섯구름)
    띠 · 마무리 컷 없음 · 플랫 그림 · 정면 셀 프레임 · 0.75배속 — 컷 5개:
    ① 0.4 절벽 위 정면: 팔짱 · 불티가 오름 ② 0.85 한 팔을 하늘로 → 두 팔을 하늘로 (발밑 작은 마법진) ③ 1.3 얼굴 정면 클로즈업 (눈이 붉게 번쩍)
    ④ 1.75 지평선에서 하늘로 틸트 업 — 빨간 마법진이 아래부터 한 겹씩 쌓여 올라감 (원근 타원 + 룬 고리 · 돌기)
    ⑤ 2.6 맨 위 마법진에서 번쩍 → 레이저가 마법진을 하나씩 꿰뚫으며 아래로 (카메라가 빛 끝을 따라 틸트 다운)
    → 3.05 레이저가 지평선에 꽂히는 순간 컷신 걷힘 → 게임 화면: 레이저가 맵 가운데로 내리꽂혀 섬광 → 맵 전체를 초토화하는 거대 폭발 하나 (충격파 · 파편 · 연기, 버섯구름 없음 — worldPhase · blast*)
    (2026-10-20 사용자: 캐릭터와 함께 나오는 버섯구름 컷 없음 · 마지막은 컷신이 아니라 게임 맵 위 큰 폭발 하나)"""
    END = NO_FINISH['bomber']
    wsc = lambda name, px: px / max(1, s.size(name)[0])       # 그림 폭을 px 로 맞추는 배율

    def cut_flash(g, t0, a=0.55):
        s.s(g, 'CutFlash', 'fx_redflash', 8.0, [(t0, 0, 0, 1.0, 0, a, 'Snap'), (t0 + 0.09, 0, 0, 1.3, 0, 0, 'Out')])

    SX, SY = 0.5, 0.905                                   # 절벽 위 서는 자리 (파노라마 비율)
    # ① 절벽 위 정면: 팔짱 · 불티가 오름
    g = s.group('Cut1', R, 0.4, 0.85)
    tr = s.cam(g, [(0.4, 0.5, 0.86, 2.0, 0, 'Out'), (0.85, 0.5, 0.86, 2.25, 0, 'Linear')], kicks=[0.4], power=18)
    s.cel(tr, 'Bomber', [(0.4, 'anim_cast_1')], [(0.4, SX, SY, 1, 0, 1, 'Out')], height=300, ref='anim_cast_1')
    s.rise(g, 'Ember', 'fx_embers', [0.45 + 0.04 * i for i in range(9)], [s.rnd.uniform(-800, 800) for _ in range(9)], -560, 600, sc=0.35, dur=0.4)
    # ② 한 팔을 하늘로 → 두 팔을 하늘로 · 발밑 작은 마법진
    C1, C2 = 0.85, 1.02
    g = s.group('Cut2', R, 0.85, 1.3)
    tr = s.cam(g, [(0.85, 0.5, 0.855, 2.4, 0, 'Out'), (1.3, 0.5, 0.85, 2.6, 0, 'Linear')], kicks=[C2], power=34)
    s.w(tr, 'Circle', 'fx_circle_persp', wsc('fx_circle_persp', 260), [(C1, SX, SY, 0.2, 0, 0, 'Out'), (C2, SX, SY, 1.0, 0, 1, 'Back'), (1.3, SX, SY, 1.1, 0, 1, 'Linear')], pulse=0.05, pulseHz=8)
    s.cel(tr, 'Bomber', [(C1, 'anim_cast_2'), (C2, 'anim_cast_3')], [(C1, SX, SY, 1, 0, 1, 'Out'), (C2, SX, SY, 1.08, 0, 1, 'Back'), (1.3, SX, SY, 1.1, 0, 1, 'Linear')], height=300, ref='anim_cast_1')
    s.rise(g, 'Ember', 'fx_embers', [C2 + 0.03 * i for i in range(8)], [-760, -540, -320, -110, 110, 320, 540, 760], -560, 500, sc=0.4, dur=0.28)
    cut_flash(g, 0.85)
    # ③ 얼굴 정면 클로즈업 · 눈 번쩍
    g = s.group('Cut3', R, 1.3, 1.75)
    s.face(g, 'zoom', 1.3, 1.75, bg='face_bg')
    s.s(g, 'EyeFlash', 'fx_redflash', 0.8, [(1.45, -150, 80, 0, 0, 0, 'Out'), (1.46, -150, 80, 1.0, 0, 0.9, 'Snap'), (1.65, -150, 80, 1.6, 30, 0, 'Out')])
    cut_flash(g, 1.3)
    # ④ 지평선에서 하늘로 틸트 업 — 빨간 마법진이 아래부터 한 겹씩 쌓여 올라감
    T = 1.75
    HY = 0.545                                          # 지평선 (레이저가 꽂히는 자리)
    CY = [0.48, 0.40, 0.32, 0.24, 0.165, 0.09]           # 마법진 높이 (아래 → 위)
    CW = [760, 860, 960, 1060, 1180, 1320]               # 폭 (파노라마 px, 위로 갈수록 큼)
    CT = [T + 0.1 + 0.12 * k for k in range(len(CY))]    # 켜지는 시각
    g = s.group('Cut4', R, T, 2.6)
    tr = s.cam(g, [(T, 0.5, 0.6, 1.25, 0, 'Out'), (2.45, 0.5, 0.2, 1.0, 0, 'InOut'), (2.6, 0.5, 0.18, 0.98, 0, 'Linear')], kicks=CT, power=16, ktime=0.15)
    for k, (yy, w, t0) in enumerate(zip(CY, CW, CT)):
        s.w(tr, f'Ring{k}', 'fx_runering', wsc('fx_runering', w * 1.15), [(t0 - 0.01, 0.5, yy, 0.3, 0, 0, 'Out'), (t0, 0.5, yy, 0.6, 0, 0.9, 'Snap'), (t0 + 0.15, 0.5, yy, 1.0, 0, 0.9, 'Back'), (2.6, 0.5, yy, 1.0, 0, 0.8, 'Linear')], spin=-30.0 if k % 2 else 30.0)
        s.w(tr, f'Circle{k}', 'fx_circle_persp', wsc('fx_circle_persp', w), [(t0 - 0.01, 0.5, yy, 0.2, 0, 0, 'Out'), (t0, 0.5, yy, 0.4, 0, 1, 'Snap'), (t0 + 0.14, 0.5, yy, 1.06, 0, 1, 'Back'), (2.6, 0.5, yy, 1.0, 0, 1, 'Linear')], pulse=0.03, pulseHz=6)
        s.w(tr, f'Pop{k}', 'fx_redflash', wsc('fx_redflash', w * 0.5), [(t0 - 0.01, 0.5, yy, 0, 0, 0, 'Out'), (t0, 0.5, yy, 0.6, 0, 0.9, 'Snap'), (t0 + 0.2, 0.5, yy, 1.4, 0, 0, 'Out')])
    s.speed(g, 2.25, 2.6, vertical=True)
    cut_flash(g, T)
    # ⑤ 맨 위 마법진에서 번쩍 → 레이저가 마법진을 하나씩 꿰뚫으며 아래로 (카메라가 빛 끝을 따라 내려감)
    T = 2.6; F0 = 2.72; F1 = 3.0                          # 발사 · 빛 끝이 지평선에 닿음
    g = s.group('Cut5', R, T, 3.05)
    tr = s.cam(g, [(T, 0.5, 0.11, 1.2, 0, 'Out'), (F0, 0.5, 0.11, 1.25, 0, 'Linear'), (F1, 0.5, 0.47, 1.05, 0, 'In'), (3.05, 0.5, 0.48, 1.0, 0, 'Linear')], kicks=[F0], power=50, rot=2)
    top = 0.03
    stops = [top] + CY[::-1] + [HY]                      # 위 → 아래 (마지막 마디 = 맨 아래 마법진 → 지평선)
    tt = lambda yy: F0 + (F1 - F0) * (yy - top) / (HY - top)       # 빛 끝이 yy 에 닿는 시각
    for k, (yy, w) in enumerate(zip(CY, CW)):
        s.w(tr, f'Ring{k}', 'fx_runering', wsc('fx_runering', w * 1.15), [(T, 0.5, yy, 1.0, 0, 0.8, 'Out')], spin=-60.0 if k % 2 else 60.0)
        s.w(tr, f'Circle{k}', 'fx_circle_persp', wsc('fx_circle_persp', w), [(T, 0.5, yy, 1.0, 0, 1, 'Out'), (tt(yy), 0.5, yy, 1.12, 0, 1, 'Snap'), (tt(yy) + 0.1, 0.5, yy, 1.0, 0, 1, 'Out')], pulse=0.05, pulseHz=10)
    for k in range(len(stops) - 1):                      # 레이저 마디: 위 마법진 → 아래 마법진, 빛 끝이 지날 때 나타남
        y0, y1 = stops[k], stops[k + 1]; t0 = tt(y0); ym = (y0 + y1) / 2
        hpx = (y1 - y0) * s.PH * s.PS * 1.15
        s.w(tr, f'Beam{k}', 'fx_beam', s.hp('fx_beam', hpx), [(t0 - 0.01, 0.5, ym, 1, 0, 0, 'Out'), (t0, 0.5, ym, 1, 0, 1, 'Snap'), (3.05, 0.5, ym, 1.05, 0, 1, 'Linear')], flicker=0.15, jitter=3.0)
        s.w(tr, f'Core{k}', 'fx_beamcore', s.hp('fx_beamcore', hpx), [(t0 - 0.01, 0.5, ym, 1, 0, 0, 'Out'), (t0, 0.5, ym, 1, 0, 1, 'Snap'), (3.05, 0.5, ym, 1, 0, 1, 'Linear')], flicker=0.3)
    s.w(tr, 'Fire', 'fx_redflash', wsc('fx_redflash', 900), [(F0 - 0.01, 0.5, top + 0.03, 0, 0, 0, 'Out'), (F0, 0.5, top + 0.03, 1.0, 0, 1, 'Snap'), (F0 + 0.25, 0.5, top + 0.03, 1.5, 30, 0, 'Out')])
    s.speed(g, F0 + 0.05, F1, vertical=True)
    s.s(g, 'Hit', 'fx_redflash', 9.0, [(F1 - 0.01, 0, -300, 0, 0, 0, 'Out'), (F1, 0, -300, 0.8, 0, 1, 'Snap'), (END, 0, 0, 2.2, 0, 1, 'In')])     # 꽂히는 순간 섬광이 화면을 덮으며 → 게임 화면 섬광 · 폭발로 이어짐
    cut_flash(g, T)
    G = s.group('Top', R, 2.6)
    s.meme(G, 2.62, side=-1, y=440)
    return None


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


def maid(s, R):
    """세로 (정면, + 회색 판 pano_alt = 멈춘 세계): 달밤 하늘 → 시계탑 저택 → 발코니. (2026-10-20, 사용자: 초필살기 = 시간 정지 + 칼 모티브)
    띠 · 마무리 컷 없음 · 플랫 그림 · 정면 셀 프레임 · 0.75배속 — 컷 6개:
    ① 0.4 보름달 → 시계탑으로 틸트 다운 (시계 톱니가 돎) ② 0.85 발코니 정면: 회중시계를 가슴에 → 높이 들어 용두를 딸깍
    ③ 1.3 얼굴 클로즈업 (눈이 붉게 · 칼 세 자루) ④ 1.75 손가락 딱 → 세상이 회색으로 멈춤 (pano_alt) · 하늘로 틸트 업하며 칼 고리가 겹겹이 펼쳐짐
    ⑤ 2.55 두 팔을 크게 펼쳐 칼을 흩뿌림 → 화면 가득 칼이 멈춘 채 떠 있음 ⑥ 3.05 '그리고 시간은 움직인다' — 색이 돌아오며 칼이 카메라로 쏟아져 하얗게
    → 3.45 컷신 걷힘 → 게임 화면 칼 비 (다단 10타 · 대상마다 칼 꽂힘 = worldPhase · imageFx)"""
    END = NO_FINISH['maid']
    wsc = lambda name, px: px / max(1, s.size(name)[0])

    def cut_flash(g, t0, a=0.55):
        s.s(g, 'CutFlash', 'fx_flash', 8.0, [(t0, 0, 0, 1.0, 0, a, 'Snap'), (t0 + 0.09, 0, 0, 1.3, 0, 0, 'Out')])

    SX, SY = 0.5, 0.885                    # 발코니 가운데 서는 자리 (파노라마 비율, pano_on 1536×2252)
    CLX, CLY = 0.456, 0.51                 # 시계탑 시계판
    # ① 보름달 → 시계탑으로 틸트 다운 · 톱니가 돎
    g = s.group('Cut1', R, 0.4, 0.85)
    tr = s.cam(g, [(0.4, 0.36, 0.1, 1.3, 3, 'Out'), (0.85, CLX, CLY, 1.5, 0, 'InOut')], kicks=[0.4], power=16)
    s.w(tr, 'Gear', 'prop_gear', wsc('prop_gear', 260), [(0.4, CLX - 0.16, CLY - 0.05, 1, 0, 0.9, 'Out'), (0.85, CLX - 0.16, CLY - 0.05, 1, 0, 0.9, 'Linear')], spin=60.0)
    s.w(tr, 'Gear2', 'prop_gear', wsc('prop_gear', 180), [(0.4, CLX + 0.17, CLY + 0.02, 1, 0, 0.9, 'Out'), (0.85, CLX + 0.17, CLY + 0.02, 1, 0, 0.9, 'Linear')], spin=-90.0)
    s.speed(g, 0.55, 0.85, vertical=True)
    # ② 발코니 정면: 시계를 가슴에 → 높이 들어 딸깍
    C1, C2 = 0.85, 1.05
    g = s.group('Cut2', R, 0.85, 1.3)
    tr = s.cam(g, [(0.85, 0.5, 0.825, 2.0, 0, 'Out'), (1.3, 0.5, 0.82, 2.25, 0, 'Linear')], kicks=[C2], power=26)
    s.cel(tr, 'Maid', [(C1, 'anim_watch_1'), (C2, 'anim_watch_2')], [(C1, SX, SY, 1, 0, 1, 'Out'), (C2, SX, SY, 1.06, 0, 1, 'Back'), (1.3, SX, SY, 1.08, 0, 1, 'Linear')], height=330, ref='anim_watch_1')
    s.s(g, 'Tick', 'fx_glints', 0.9, [(C2 - 0.01, -260, 330, 0, 0, 0, 'Out'), (C2, -260, 330, 0.6, 0, 1, 'Snap'), (C2 + 0.25, -260, 330, 1.3, 40, 0, 'Out')])
    s.s(g, 'Dial', 'fx_clockface', 1.6, [(C1, 0, 0, 0.6, 0, 0, 'Out'), (C2, 0, 0, 1.0, 0, 0.25, 'Back'), (1.3, 0, 0, 1.15, 0, 0.2, 'Linear')], spin=-40.0)
    cut_flash(g, 0.85)
    # ③ 얼굴 클로즈업 · 눈 번쩍
    g = s.group('Cut3', R, 1.3, 1.75)
    s.face(g, 'zoom', 1.3, 1.75, bg='face_bg')
    s.s(g, 'Glint', 'fx_glints', 0.7, [(1.45, -330, 160, 0, 0, 0, 'Out'), (1.46, -330, 160, 0.9, 0, 1, 'Snap'), (1.65, -330, 160, 1.4, 30, 0, 'Out')])
    cut_flash(g, 1.3)
    # ④ 손가락 딱 → 세상이 회색으로 멈춤 · 하늘로 틸트 업하며 칼 고리가 겹겹이
    T = 1.75; SNAP = 1.9
    g = s.group('Cut4', R, T, 2.55)
    tr = s.cam(g, [(T, 0.5, 0.825, 2.0, 0, 'Out'), (SNAP + 0.1, 0.5, 0.82, 2.1, 0, 'Linear'), (2.4, 0.55, 0.2, 1.1, 0, 'InOut'), (2.55, 0.56, 0.18, 1.05, 0, 'Linear')],
               kicks=[SNAP], power=40, swap=(SNAP, SNAP + 0.1))
    s.cel(tr, 'Maid', [(T, 'anim_watch_2'), (SNAP - 0.05, 'anim_watch_3')], [(T, SX, SY, 1.08, 0, 1, 'Out'), (2.55, SX, SY, 1.08, 0, 1, 'Linear')], height=330, ref='anim_watch_1')
    s.s(g, 'Stop', 'fx_flash', 6.0, [(SNAP - 0.01, 0, 0, 0.3, 0, 0, 'Out'), (SNAP, 0, 0, 0.6, 0, 0.9, 'Snap'), (SNAP + 0.3, 0, 0, 1.6, 0, 0, 'Out')])
    RINGS = [(0.62, 0.30, 700, 2.1), (0.42, 0.22, 560, 2.18), (0.70, 0.14, 820, 2.26), (0.50, 0.08, 1000, 2.34)]
    for k, (x, y, w, t0) in enumerate(RINGS):
        s.w(tr, f'KRing{k}', 'fx_knifering', wsc('fx_knifering', w), [(t0 - 0.01, x, y, 0.4, 0, 0, 'Out'), (t0, x, y, 0.6, 0, 1, 'Snap'), (t0 + 0.14, x, y, 1.05, 0, 1, 'Back'), (2.55, x, y, 1.0, 0, 1, 'Linear')], spin=12.0 if k % 2 else -12.0)
    s.speed(g, 2.05, 2.4, vertical=True)
    cut_flash(g, T)
    # ⑤ 두 팔을 펼쳐 칼을 흩뿌림 → 화면 가득 멈춘 칼 (회색 세계)
    T = 2.55; FL = 2.7
    g = s.group('Cut5', R, T, 3.05)
    tr = s.cam(g, [(T, 0.5, 0.83, 2.2, 0, 'Out'), (3.05, 0.5, 0.82, 1.9, 0, 'Linear')], kicks=[FL], power=34, pano='pano_alt')
    s.cel(tr, 'Maid', [(T, 'anim_throw_1'), (FL, 'anim_throw_2')], [(T, SX, SY, 1, 0, 1, 'Out'), (FL, SX, SY, 1.08, 0, 1, 'Back'), (3.05, SX, SY, 1.1, 0, 1, 'Linear')], height=330, ref='anim_throw_1')
    for i in range(18):                    # 손에서 퍼져 나가 그 자리에 멈춰 뜸 (멈춘 시간)
        a = math.radians(-160 + 340 * i / 17 + s.rnd.uniform(-6, 6)); r = s.rnd.uniform(420, 980)
        x, y = math.cos(a) * r * 1.2, math.sin(a) * r * 0.75 + 60
        rot = math.degrees(a) + 180       # 칼끝이 가운데(관객 쪽 표적)로 — 앞모습 칼과 섞음
        spr = 'fx_knife' if i % 3 else 'fx_knife_front'
        s.s(g, f'Knife{i}', spr, 0.32 if spr == 'fx_knife' else 0.22, [(FL - 0.01, 0, 60, 0.2, rot, 0, 'Out'), (FL, 0, 60, 0.4, rot, 1, 'Snap'), (FL + 0.12, x, y, 1.0, rot, 1, 'Out'), (3.05, x * 1.02, y * 1.02, 1.0, rot, 1, 'Linear')])
    cut_flash(g, T)
    # ⑥ 시간이 다시 흐름: 색이 돌아오며 칼이 카메라로 쏟아져 하얗게
    T = 3.05
    g = s.group('Cut6', R, T)
    tr = s.cam(g, [(T, 0.56, 0.2, 1.1, 0, 'Out'), (END + 0.4, 0.56, 0.18, 1.6, 0, 'In')], kicks=[T], power=50, rot=2)
    for k, (x, y, w, _) in enumerate(RINGS):
        s.w(tr, f'KRing{k}', 'fx_knifering', wsc('fx_knifering', w), [(T, x, y, 1.0, 0, 1, 'Out'), (T + 0.25, x, y, 1.8, 0, 0, 'In')])
    for i in range(26):                    # 앞모습 칼이 멀리서 커지며 카메라로
        x, y, t0 = s.rnd.uniform(-900, 900), s.rnd.uniform(-480, 480), T + s.rnd.uniform(0, 0.3)
        s.s(g, f'Rush{i}', 'fx_knife_front', 0.25, [(t0 - 0.01, x * 0.3, y * 0.3, 0.1, 0, 0, 'Out'), (t0, x * 0.3, y * 0.3, 0.15, 0, 1, 'Snap'), (t0 + 0.18, x, y, 2.2, 0, 1, 'In'), (t0 + 0.22, x * 1.2, y * 1.2, 2.8, 0, 0, 'Linear')])
    s.speed(g, T, END, vertical=False)
    s.s(g, 'WhiteOut', 'fx_flash', 9.0, [(T + 0.12, 0, 0, 0.5, 0, 0, 'Out'), (END, 0, 0, 1.4, 0, 0.95, 'In'), (END + 0.4, 0, 0, 1.6, 0, 0.95, 'Linear')])
    cut_flash(g, T, 0.8)
    G = s.group('Top', R, 2.55)
    s.meme(G, 2.6, side=1, y=440)
    return None


STORIES = {f.__name__: f for f in (parkrat, emperor, pharaoh, knight, sultan, alien, streamrat, ramjui, jwerry, starchef, jjdetective, fondue, bomber, icequeen, maid)}
