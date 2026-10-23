"""쥐 히드라 리그 값 (2026-10-23) → rig_web/hydra_parts.json + 확인 그림.
파츠 = BossRework/Parts/boss_hydra (= rig_web/hydra/). 통 그림(_full) 은 목 7 · 다리 8 · 꼬리 4 배치만 참고 (파츠는 다른 시트라 match 안 됨).
몸통 = body_chaos (박힌 머리 각도 제각각, 사용자 요청) · --body body 로 hy4 몸통.
목 = neck1→neck2→neck3→head 사슬 (피벗 = 마디 오른끝 = 몸 쪽, 다음 마디 at = 왼끝). 목마다 위상·빠르기·흔들림 다르게 (제각각 움직임).
render() = rig_web/hydra.html 과 같은 계산 (hydra_gif.py 도 씀).
사용: python Tools/hydra_rig.py [--body body|body_chaos] [확인.png]"""
import json, math, os, random, sys
import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'UnityResources', 'Rats', 'BossRework', 'rig_web')
DIR = os.path.join(ROOT, 'hydra')
OUT = os.path.join(ROOT, 'hydra_parts.json')

# 목 마디: 원래 그림(neck1~3)은 너무 길고 굵기가 달라 이음새마다 배 줄무늬가 어긋남 → 굵기 같은 neck2 하나로 짧은 마디 3개를 만듦 (make_short):
# 왼쪽(둥근 끝) SEG_LEN 픽셀 + 오른쪽 둥근 끝 CAP 픽셀을 이어 붙임 → 양끝 둥근 짧은 마디. 이음새는 부모 마디 둥근 끝 밑에 숨음.
# 마디 순서 = 몸 쪽이 위 (neck1 > neck2 > neck3 > 머리) → 둥근 끝끼리 겹쳐 관절 원 안 보임.
SEG_SRC = 'neck2'
SEG_LEN = {'neck1': 310, 'neck2': 280, 'neck3': 250}     # 마디 길이 (픽셀, 둥근 끝 포함)
CAP = 50
SEG = {}                                                   # 이름 → (오른쪽 관절 u, 왼쪽 끝 둥근 마개 가운데 u, 가운데 v)
HEAD_PV = (0.93, 0.638)          # 머리 가로 목 그루터기 오른끝 (head_bite 0.653 · angry 0.64 → 거의 같음)
HEAD_SCALE = 0.85                # 그루터기 굵기 96 → 마디 굵기 92 보다 살짝 가늘게 (마디 끝 밑에 숨음)
LEG_PV = (0.55, 0.2)             # 다리 윗끝 털 공 (몸통 밑에 숨김)
LEG_FOOT = 0.97                  # 발바닥 높이
TAIL_PV = (0.08, 0.38)           # 꼬리 뿌리 털 공

# 목 7개: 몸통 붙는 자리 (x, y) · 머리 관절 목표 (tx, ty) — 몸통 그림 기준 0~1 (y 아래 +, 몸통 밖은 0 미만·1 초과)
#          · 머리 각도 (°, 반시계 +: 왼쪽 보는 머리는 + = 고개 숙임 / 뒤집은 머리는 + = 고개 듦) · 뒤집기 (머리가 오른쪽 = 뒤를 봄)
#            뒤집기 = 목 사슬 전체를 첫 마디(neck1 flipX)에서 좌우로 뒤집음 → 마디 배 줄무늬도 머리 쪽과 같은 면 (머리만 뒤집으면 목이 180° 돌아 줄무늬가 위로 감)
#          · 모양 S (마디가 한 번 꺾였다 반대로 = 뒤틀림) / C (한쪽으로 휨) — 각도는 solve_neck 이 고름
#          · 목 배율 · 머리 떠다님(몸통 픽셀) · 빠르기 · 위상 · 순서(작을수록 뒤)
#          사용자 (2026-10-23): "목이 한 방향 바라보지 말고 여러 각도로 뒤틀려 있으면 더 자연스러울 듯" → 머리 방향·목 모양 제각각
NECKS = [
    # x     y     tx     ty   머리° 뒤집 모양 배율  떠다님 빠르기 위상 순서
    (0.15, 0.45, 0.00, 0.40, 40, False, 'S', 1.15, 22, 0.85, 0.00, 6),     # 0 왼쪽 아래: 고개 숙여 땅을 노림
    (0.22, 0.32, -0.10, 0.20, -30, False, 'C', 1.18, 18, 1.10, 0.37, 4),   # 1 왼쪽: 고개 듦
    (0.32, 0.25, 0.10, -0.40, 35, False, 'S', 1.20, 24, 0.70, 0.71, 2),    # 2 뒤틀려 아래를 내려봄
    (0.46, 0.18, 0.36, -0.46, -55, False, 'C', 1.28, 20, 0.95, 0.15, 0),   # 3 가운데 가장 높음: 하늘 보고 울부짖음
    (0.60, 0.24, 0.66, -0.46, -12, True, 'S', 1.20, 18, 1.25, 0.58, 1),    # 4 뒤를 봄 (고개 살짝 숙임)
    (0.72, 0.28, 0.90, -0.36, -40, True, 'C', 1.15, 24, 0.80, 0.86, 3),    # 5 갈고리처럼 꺾여 뒤쪽 아래를 봄
    (0.84, 0.40, 1.08, 0.26, 22, True, 'S', 1.10, 18, 1.05, 0.27, 5),      # 6 오른쪽: 뒤를 보며 고개 듦
]
# 목 움직임 = 머리 위치를 먼저 정하고 목 마디 3개가 따라옴 (neck_ik, 유니티 BossPartRig.NeckIK 와 같은 계산).
#   대기 = 머리가 제자리에서 살살 떠다님 (8자) · 물기 = 뒤로 움찔(wind 픽셀) → 머리가 보는 쪽·아래로 찌름(strike 픽셀) · 머리 각도 유지 + 끄덕임
#   (예전: 마디마다 각도를 흔들어 몸 쪽부터 쌓임 → 목 전체가 와이퍼처럼 휘청, 머리도 까딱 — 사용자 "목 움직임이 이상함")
WIND = 70                        # 물기 전 뒤로 (몸통 픽셀)
STRIKE = 230                     # 물 때 앞·아래로
NOD = 4                          # 머리 끄덕임 (°)
IK_LIMIT = 40                    # 마디가 쉬는 각도에서 벗어날 수 있는 만큼 (°)

# 다리 8: (붙는 x, 그림, 먼 쪽?, 걸음 위상). 앞(가까운) 4 = 몸통 위, 먼 4 = 몸통 뒤 어둡게
LEGS = [(0.20, 'legF1', False, 0.0), (0.40, 'legB1', False, 0.5), (0.60, 'legF2', False, 0.0), (0.79, 'legB2', False, 0.5),
        (0.27, 'legF2', True, 0.5), (0.47, 'legB2', True, 0.0), (0.67, 'legF1', True, 0.5), (0.86, 'legB1', True, 0.0)]
LEG_SCALE = 1.25
LEG_AMP = 20

# 꼬리 4: (붙는 y, 각도, 위상) — 몸통 오른끝에서 부채꼴
TAILS = [(0.55, 16, 0.0), (0.62, 4, 0.3), (0.69, -8, 0.6), (0.76, -20, 0.85)]

# 박힌 머리 6 (Twitch): (그림, 표정 바뀜, x, y, 각도, 좌우 뒤집기, 배율, 빠르기, 위상) — 각도·방향 제각각 (사용자)
EMB = [('emb_a', 'emb_c', 0.24, 0.62, 25, False, 0.85, 0.9, 0.10),
       ('emb_b', 'emb_d', 0.45, 0.42, -150, False, 0.80, 1.2, 0.45),
       ('emb_e', None, 0.62, 0.62, 70, True, 0.80, 0.7, 0.80),
       ('emb_d', 'emb_c', 0.82, 0.55, -15, True, 0.85, 1.0, 0.30),
       ('emb_f', None, 0.36, 0.78, 160, True, 0.75, 1.3, 0.65),
       ('emb_c', 'emb_b', 0.70, 0.80, -35, False, 0.75, 0.8, 0.95)]


def make_short(dirs):
    """neck2 → neck1s~3s: 왼쪽 (L - CAP) 픽셀 + 오른쪽 둥근 끝 CAP 픽셀. 관절 = 오른쪽 마개 가운데, 다음 마디 = 왼쪽 마개 가운데"""
    im = Image.open(os.path.join(DIR, SEG_SRC + '.png')).convert('RGBA')
    a = np.array(im)[..., 3] > 100; h = im.height
    cols = np.where(a.any(0))[0]; x0, x1 = int(cols[0]), int(cols[-1]) + 1
    ys = np.where(a[:, (x0 + x1) // 2])[0]; thick = ys[-1] - ys[0]; v = (ys[0] + ys[-1]) / 2 / h
    for n, L in SEG_LEN.items():
        w = x0 + L + (im.width - x1)
        c = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        c.paste(im.crop((0, 0, x0 + L - CAP, h)), (0, 0))
        c.alpha_composite(im.crop((x1 - CAP, 0, im.width, h)), (x0 + L - CAP, 0))
        SEG[n + 's'] = ((x0 + L - thick * 0.5) / w, (x0 + thick * 0.5) / w, v)
        for d in dirs: c.save(os.path.join(d, n + 's.png'))
    return thick


def P(name, sprite, parent, at, pivot, order, move, amp=0, phase=0, speed=1, angle=0, scale=1, **kw):
    d = dict(name=name, sprite=sprite, parent=parent, at=[round(at[0], 3), round(at[1], 3)], pivot=[round(pivot[0], 3), round(pivot[1], 3)],
             order=order, move=move, amp=amp, phase=round(phase % 1, 3), speed=speed, angle=round(angle, 2), scale=round(scale, 3), far=False, alt=None)
    d.update(kw)
    return d


def solve_neck(base, target, hd, flip, shape, sc, bw, bh):
    """마디 3개 절대 각도 [a0, a0+d1, a0+d1+d2] · 길이 배율(sc × 0.85~1.15) 중 머리 관절이 목표에 가장 가까운 것 → ([각도 3], 배율).
    S = d1·d2 부호 반대(한 번 꺾였다 반대로, 각각 15° 넘게) · C = 같은 부호. 마지막 마디는 머리가 보는 쪽으로 이어져야 함
    (마지막 마디 각도 ≈ 머리 각도), 30° 넘게 꺾이면 벌점. 뒤집은 목은 마디가 오른쪽으로 뻗는 몸통 좌표 각도 (neck_ik 의 '실제 각도' 와 같은 뜻)"""
    im_w = {n: Image.open(os.path.join(DIR, n + '.png')).width for n in SEG}
    L0 = np.array([(SEG[n][0] - SEG[n][1]) * im_w[n] for n in ('neck1s', 'neck2s', 'neck3s')])
    px = lambda u, v: ((u - 0.5) * bw, (0.5 - v) * bh)
    (bx, by), (gx, gy) = px(*base), px(*target)
    a0, d1, d2, m = np.meshgrid(np.arange(-180, 181, 3.0), np.arange(-70, 71, 5.0), np.arange(-70, 71, 5.0), np.arange(0.85, 1.151, 0.05), indexing='ij')
    A = [a0, a0 + d1, a0 + d1 + d2]
    D = 1 if flip else -1                     # 그림이 왼쪽으로 뻗음 → 뒤집으면 오른쪽
    x = bx + D * sum(L0[k] * sc * m * np.cos(np.radians(A[k])) for k in range(3))
    y = by + D * sum(L0[k] * sc * m * np.sin(np.radians(A[k])) for k in range(3))
    bend = np.abs((A[2] - hd + 180) % 360 - 180)
    cost = np.hypot(x - gx, y - gy) + np.maximum(0, bend - 30) * 10 + 0.4 * (np.abs(d1) + np.abs(d2)) + np.abs(m - 1) * 40
    if shape == 'S': cost += np.where((d1 * d2 < 0) & (np.abs(d1) >= 15) & (np.abs(d2) >= 15), 0, 400)
    else: cost += np.where(d1 * d2 >= 0, 0, 400)
    i = np.unravel_index(np.argmin(cost), cost.shape)
    return [round(float(A[k][i]), 1) for k in range(3)], round(float(sc * m[i]), 3)


def build(body='body_chaos'):
    make_short([DIR, os.path.join(ROOT, '..', 'Parts', 'boss_hydra')])
    bw, bh = Image.open(os.path.join(DIR, body + '.png')).size
    legh = Image.open(os.path.join(DIR, 'legB1.png')).size[1]
    leg_y = 0.86                                         # 다리 붙는 높이 (몸통 아래쪽)
    ground = (leg_y - 0.5) * bh + (LEG_FOOT - LEG_PV[1]) * legh * LEG_SCALE    # 몸통 가운데 → 발바닥
    parts = [P('body', body, None, (0, ground / bh), (0.5, 0.5), 50, 'Bob', amp=6, speed=1.4)]
    for i, (x, y, tx, ty, hd, flip, shape, sc, amp, spd, ph, od) in enumerate(NECKS):
        ang, sc = solve_neck((x, y), (tx, ty), hd, flip, shape, sc, bw, bh); ang = ang + [hd]
        F = -1 if flip else 1                      # 뒤집은 사슬: 첫 마디 뒤의 상대 각도는 부호 반대 (거울 속에서 돌림)
        prev, prev_abs = 'body', 0
        for k, seg in enumerate(['neck1', 'neck2', 'neck3', 'head']):
            nm = f'n{i}_{seg}' if seg != 'head' else f'n{i}_head'
            sp = seg if seg == 'head' else seg + 's'
            if k == 0: at = (x, y)
            else: ps = SEG[['neck1', 'neck2', 'neck3'][k - 1] + 's']; at = (ps[1], ps[2])
            pv = HEAD_PV if seg == 'head' else (SEG[sp][0], SEG[sp][2])
            # 목 값은 첫 마디(neck1)에: amp = 머리 떠다님 · wind/strike = 물기 거리 · 머리: amp = 끄덕임 °
            extra = dict(neck=i, wind=WIND if k == 0 else 0, strike=STRIKE if k == 0 else 0)
            if seg == 'head': extra.update(alt='head_bite', hurt='head_angry')
            if k == 0 and flip: extra.update(flipX=True)
            parts.append(P(nm, sp, prev, at, pv, 10 + od * 4 + 3 - k, 'Neck', amp=amp if k == 0 else (NOD if seg == 'head' else 0), phase=ph,
                           speed=spd, angle=(ang[k] - prev_abs) * (1 if k == 0 else F), scale=sc if k == 0 else (HEAD_SCALE if seg == 'head' else 1), **extra))
            prev, prev_abs = nm, ang[k]
    for i, (y, ang, ph) in enumerate(TAILS):
        parts.append(P(f'tail{i}', 'tail', 'body', (0.93, y), TAIL_PV, 4 - i % 2, 'Sway', amp=7, phase=ph, speed=0.8 + 0.1 * i, angle=ang, scale=1.1))
    for i, (x, sp, far, ph) in enumerate(LEGS):
        parts.append(P(f'leg{i}' + ('Far' if far else ''), sp, 'body', (x, leg_y - (0.03 if far else 0)), LEG_PV, 2 if far else 60 + i, 'Walk',
                       amp=LEG_AMP, phase=ph, speed=1, angle=0, scale=LEG_SCALE, far=far))
    for i, (sp, alt, x, y, ang, flip, sc, spd, ph) in enumerate(EMB):
        parts.append(P(f'emb{i}', sp, 'body', (x, y), (0.55, 0.55), 51 + i, 'Twitch', amp=9, phase=ph, speed=spd, angle=ang, scale=sc,
                       alt=alt, flipX=flip))
    return parts


# ---------- 그리기 (hydra.html 과 같은 계산) ----------
IDLE_WALK, FAR = 0.15, 0.72
STRIKE_T = (0.3, 0.55)
ORDER7 = [3, 0, 5, 1, 6, 2, 4]


def M(a, b, c, d, e, f): return np.array([[a, c, e], [b, d, f], [0, 0, 1]], float)
def T(x, y): return M(1, 0, 0, 1, x, y)
def S(sx, sy=None): return M(sx, 0, 0, sx if sy is None else sy, 0, 0)
def R(deg): r = math.radians(deg); return M(math.cos(r), math.sin(r), -math.sin(r), math.cos(r), 0, 0)
def smooth(k): return k * k * (3 - 2 * k)


def strike_k(mode, neck, t, P=0.9):
    if mode == 'frenzy': return ((t / (P * 1.4) + neck * 0.11) % 1 + 1) % 1
    if mode != 'bite': return -1
    step = math.floor(t / P); f = t / P - step
    return f if ORDER7[step % 7] == neck else -1


def strike_curve(f):
    if f < 0: return 0, 0, False
    W, L = STRIKE_T
    if f < W: return smooth(f / W), 0, False
    if f < L: k = smooth((f - W) / (L - W)); return 1 - k, k, True
    k = smooth((f - L) / (1 - L)); return 0, 1 - k, k < 0.4


def _wrap(d): return (d + 180) % 360 - 180


def neck_ik(parts, img, mode, t, hk=0.0, bite_p=0.9):
    """목 사슬마다 각도 계산 → {파츠 이름: (각도, 두 번째 그림?)}. 몸통 좌표(픽셀, y 위 +)에서 머리 관절 목표를 정하고
    CCD 로 마디 3개를 돌림 (매 프레임 쉬는 자세에서 시작, 마디마다 쉬는 각도 ±IK_LIMIT). 머리 = 쉬는 절대 각도 + 끄덕임
    첫 마디가 뒤집힌 사슬(flipX, 뒤를 보는 목)은 몸통 좌표의 '실제 각도'로 바꿔 풀고 (마디 방향 x 반대, 둘째부터 상대 각도 부호 반대) 다시 돌려놓음"""
    by = {p['name']: p for p in parts}
    chains = {}
    for p in parts:
        if p['move'] == 'Neck': chains.setdefault(p['neck'], []).append(p)
    out = {}
    walking = mode == 'walk'
    for nk, ch in chains.items():
        n1, head = ch[0], ch[3]
        F = -1 if n1.get('flipX') else 1                                              # 뒤집은 사슬 = 머리가 오른쪽을 봄
        body = by[n1['parent']]; bim = img[body['sprite']]
        base = ((n1['at'][0] - body['pivot'][0]) * bim.width, (body['pivot'][1] - n1['at'][1]) * bim.height)
        L, O, s = [], [], 1.0
        for k in range(3):
            seg, nxt = ch[k], ch[k + 1]; s *= seg['scale']; im = img[seg['sprite']]
            vx = (nxt['at'][0] - seg['pivot'][0]) * im.width * s; vy = (seg['pivot'][1] - nxt['at'][1]) * im.height * s
            L.append(math.hypot(vx, vy)); O.append(math.degrees(math.atan2(vy, vx * F)))
        rest = [c['angle'] * (1 if k == 0 else F) for k, c in enumerate(ch)]      # 실제 각도

        def fk(rel):
            pts, A, x, y = [base], 0.0, base[0], base[1]
            for k in range(3):
                A += rel[k]; d = math.radians(A + O[k]); x += L[k] * math.cos(d); y += L[k] * math.sin(d); pts.append((x, y))
            return pts

        end = fk(rest)[3]; head_abs = sum(rest)
        f = math.radians(head_abs + (180 if F > 0 else 0)); face = (math.cos(f), math.sin(f))          # 머리가 보는 쪽 (그림은 왼쪽을 봄)
        spd = n1['speed'] * (1.5 if walking else 1); ph = n1['phase'] * math.pi * 2; amp = n1['amp'] * (1.3 if walking else 1)
        kw, ks, op = strike_curve(strike_k(mode, nk, t, bite_p))
        wd = (-face[0] * 0.8, -face[1] * 0.8 + 0.6); ld = (face[0] * 0.85, face[1] * 0.85 - 0.5)     # 뒤·위 / 앞·아래
        wl, ll = math.hypot(*wd), math.hypot(*ld)
        back = n1['wind'] * (kw + hk * 0.6)
        tx = end[0] + amp * math.sin(t * 1.7 * spd + ph) + back * wd[0] / wl + n1['strike'] * ks * ld[0] / ll
        ty = end[1] + amp * 0.7 * math.sin(t * 2.6 * spd + ph * 1.7) + back * wd[1] / wl + n1['strike'] * ks * ld[1] / ll
        rel = rest[:3]
        for it in range(8):
            for k in (2, 1, 0):
                pts = fk(rel); e, pk = pts[3], pts[k]
                d = _wrap(math.degrees(math.atan2(ty - pk[1], tx - pk[0]) - math.atan2(e[1] - pk[1], e[0] - pk[0])))
                rel[k] = max(rest[k] - IK_LIMIT, min(rest[k] + IK_LIMIT, rel[k] + d))
        nod = head['amp'] * math.sin(t * 2.1 * spd + ph + 1) + F * (10 * ks - 8 * kw)
        for k in range(3): out[ch[k]['name']] = (rel[k] * (1 if k == 0 else F), False)
        out[head['name']] = ((head_abs + nod - sum(rel)) * F, op)
    return out


class Rig:
    def __init__(self, parts, d=DIR):
        self.parts = parts; self.img = {}
        for p in parts:
            for k in ('sprite', 'alt', 'hurt'):
                n = p.get(k)
                if n and n not in self.img: self.img[n] = Image.open(os.path.join(d, n + '.png')).convert('RGBA')
        self.dark = {}

    def render(self, size, base, mode, t, walk, hk=0.0, bite_p=0.9):
        """base = 리그 원점 → 화면 행렬. mode = idle · walk · bite · frenzy · hit. hk = 맞음 세기 (0~1)"""
        W, H = size
        world, lst = {}, []
        nik = neck_ik(self.parts, self.img, mode, t, hk, bite_p)
        for p in self.parts:
            sp = self.img[p['sprite']]
            par = world.get(p['parent']) if p['parent'] else None
            a, dx, dy, use, stretch = p['angle'], 0, 0, p['sprite'], 1
            ph = p['phase'] * math.pi * 2; mv = p['move']
            if mv == 'Walk': a += p['amp'] * math.sin(walk * p['speed'] + ph) * (1 if mode == 'walk' else IDLE_WALK)
            elif mv == 'Bob':
                dy = p['amp'] * math.sin(t * 3 * p['speed'] + ph) + (p['amp'] * 0.6 * abs(math.sin(walk + ph)) if mode == 'walk' else 0); a += hk * 4
            elif mv == 'Sway': a += p['amp'] * math.sin(t * 3 * p['speed'] + ph)
            elif mv == 'Twitch':
                pulse = max(0, math.sin(t * 1.3 * p['speed'] + ph * 3)) ** 8 + hk + (0.5 if mode == 'frenzy' else 0)
                a += p['amp'] * math.sin(t * 22 + ph) * pulse; dy = p['amp'] * 0.8 * pulse
                if pulse > 0.35 and p.get('alt'): use = p['alt']
            elif mv == 'Neck':
                a, op = nik[p['name']]
                if op and p.get('alt'): use = p['alt']
            if hk > 0.05 and p.get('hurt') and use == p['sprite']: use = p['hurt']
            if par:
                ps = self.img[par['p']['sprite']]
                pos = [(p['at'][0] - par['p']['pivot'][0]) * ps.width * par['stretch'], (par['p']['pivot'][1] - p['at'][1]) * ps.height]
            else: pos = [p['at'][0] * sp.width, p['at'][1] * sp.height]
            pos = [pos[0] + dx, pos[1] + dy]
            sx = -p['scale'] if p.get('flipX') else p['scale']
            m = (par['m'] if par else base) @ T(pos[0], -pos[1]) @ R(-a) @ S(sx, p['scale'])
            world[p['name']] = {'m': m, 'p': p, 'stretch': stretch}
            lst.append((p, m @ S(stretch, 1) if stretch != 1 else m, use))
        self.world = world
        lst.sort(key=lambda o: o[0]['order'])
        layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        for p, m, use in lst:
            im = self.img[use]
            if p.get('far'):
                if use not in self.dark:
                    arr = np.array(im).astype(np.float32); arr[..., :3] *= FAR; self.dark[use] = Image.fromarray(arr.clip(0, 255).astype(np.uint8))
                im = self.dark[use]
            elif hk > 0:
                arr = np.array(im).astype(np.float32); arr[..., :3] *= 1 + hk * 0.35; im = Image.fromarray(arr.clip(0, 255).astype(np.uint8))
            pv = p['pivot']
            inv = np.linalg.inv(m @ T(-pv[0] * im.width, -pv[1] * im.height))
            layer.alpha_composite(im.transform((W, H), Image.AFFINE, tuple(inv[:2].ravel()), resample=Image.BICUBIC))
        return layer


def fv(v):
    v = round(float(v), 3)
    return (str(int(v)) if v == int(v) else repr(v) + 'f')


def cs_lines(parts):
    """BossPartArtBuilder.cs 줄 (hydra.html 의 C# 줄과 같은 형식)"""
    S = lambda n: f'S("boss_hydra/{n}")'
    out = []
    for p in parts:
        s = (f'hydra.parts.Add(P("{p["name"]}", {S(p["sprite"])}, {chr(34) + p["parent"] + chr(34) if p["parent"] else "null"}, '
             f'{fv(p["at"][0])}, {fv(p["at"][1])}, {fv(p["pivot"][0])}, {fv(p["pivot"][1])}, {p["order"]}, M.{p["move"]}, '
             f'{fv(p["amp"])}, {fv(p["phase"])}, {fv(p["speed"])}, {fv(p["angle"])}, {fv(p["scale"])}')
        if p.get('flipX'): s += ', flip: true'
        if p.get('far'): s += ', far: true'
        if p.get('alt'): s += f', alt: {S(p["alt"])}'
        if p['move'] == 'Neck': s += f', neck: {p["neck"]}' + (f', wind: {fv(p["wind"])}, strike: {fv(p["strike"])}' if p['wind'] or p['strike'] else '')
        out.append(s + '));')
    return out


if __name__ == '__main__':
    args = sys.argv[1:]
    body = 'body_chaos'
    cs = '--cs' in args
    if '--body' in args: i = args.index('--body'); body = args[i + 1]; del args[i:i + 2]
    parts = build(body)
    json.dump({'full_at': {'x': 700, 'y': 960, 's': 1}, 'parts': parts}, open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('JSON', OUT, len(parts), '파츠')
    if cs: args.remove('--cs'); print('\n'.join(cs_lines(parts)))
    if args:
        rig = Rig(parts)
        W, H = 2400, 1300
        sheet = Image.new('RGBA', (W * 2, H), (79, 93, 74, 255))
        for j, (mode, t) in enumerate([('idle', 0.0), ('bite', 0.9 * 7 + 0.45)]):
            sheet.alpha_composite(rig.render((W, H), T(W * 0.5, H * 0.95) @ S(0.62), mode, t, t * 3), (W * j, 0))
        sheet.convert('RGB').resize((W, H // 2)).save(args[0])
        print('확인', args[0])
