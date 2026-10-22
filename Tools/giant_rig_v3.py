"""차력거인 리그 값 v3 (2026-10-22): 통 그림(tt4_full)에 맞추되 팔다리 크기는 가까운 쪽·먼 쪽 같게.
예전(giant_rig_calc.py)은 관절마다 길이를 맞추느라 먼 윗팔 0.8배 · 먼 정강이 1.5배처럼 늘어나고 줄어 안쪽 팔다리가 이상했음 (사용자 지적).
→ 마디 배율 = 가까운 쪽에서 잰 값 하나 (먼 쪽도 같음), 방향만 통 그림 관절을 따름. 먼 팔·다리는 가려진 어깨·엉덩이 대신
  보이는 팔꿈치·무릎을 기준점으로 놓고 거꾸로 이어 붙임.
몸통·엉덩이·머리·안장·포탑 = match_parts 로 통 그림에서 찾은 자리.
사용: python Tools/giant_rig_v3.py <파츠폴더> <출력.json> [확인.png]
출력 JSON = rig_web/giant.html 이 읽는 형식 {full_at, parts: [...]} (BossPartArtBuilder 줄과 같은 값)"""
import json, math, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from match_parts import find, place, to_uv, rgba
from slice_rat_parts import key_magenta

FULL = 'UnityResources/Rats/BossRework/Sheets/tt4_full.png'
GROUND = 990
D = sys.argv[1]

# 통 그림 관절 (픽셀, y 아래 +). 먼 쪽 어깨·엉덩이는 가려져 있어 방향 잡는 데만 씀 (기준점 = ANCHOR)
# 엉덩이 관절 y=440: 허벅지 윗끝이 팬티 다리 구멍 선에 맞닿음 (410 은 팬티 속으로, 480 은 너무 낮음 — 통 그림 겹쳐 비교)
J = {'arm': [(600, 340), (612, 560), (592, 920)], 'armFar': [(470, 380), (458, 565), (395, 905)],
     'leg': [(1085, 440), (970, 740), (1320, 830), (1180, 955)], 'legFar': [(870, 500), (835, 735), (1130, 820), (985, 945)]}
# 먼 팔·다리 시작 = 가까운 쪽 어깨·엉덩이 관절 옆 (사용자: 오른쪽으로 옮겨 어깨 관절에 붙게) · 마디 방향 = 통 그림의 먼 쪽 · 높이 = 끝(주먹·발끝)이 통 그림 높이
FAR_DX = {'armFar': (25, 'arm'), 'legFar': (-30, 'leg')}


def alpha(name): return np.array(Image.open(os.path.join(D, name + '.png')).convert('RGBA'))[..., 3] > 100


def end_uv(name, top=True, frac=0.04):
    """그림 위끝(또는 아래끝) 가로 가운데 (u, v): 끝에서 frac 만큼 안쪽 줄"""
    a = alpha(name); h, w = a.shape
    rows = np.where(a.any(1))[0]; r = int(rows[0] + frac * h) if top else int(rows[-1] - frac * h)
    xs = np.where(a[r])[0]
    return ((xs[0] + xs[-1]) / 2 / w, r / h)


def toe_uv(name):
    """발끝 = 가장 왼쪽 점"""
    a = alpha(name); h, w = a.shape
    cols = np.where(a.any(0))[0]; c = cols[0] + 4; ys = np.where(a[:, c])[0]
    return (c / w, (ys[0] + ys[-1]) / 2 / h)


# 파츠 그림의 관절 (u, v): (위 관절, 아래 관절)
UV = {'uparm': ((0.475, 0.22), (0.63, 0.912)), 'forearm': ((0.54, 0.12), (0.45, 0.88)),
      'thigh': ((0.45, 0.17), (0.6, 0.85))}
k = end_uv('shin', True, 0.12); UV['shin'] = (k, end_uv('shin', False, 0.03))
UV['foot'] = (end_uv('foot', True, 0.10), toe_uv('foot'))
SEG = {'arm': ['uparm', 'forearm'], 'leg': ['thigh', 'shin', 'foot']}
# 먼 쪽 전용 그림 (있으면): 안쪽에서 본 주먹 · 안쪽 발. 길이는 가까운 쪽 마디와 같게
for f in ('uparmFar', 'forearmFar', 'footFar'):
    if os.path.exists(os.path.join(D, f + '.png')):
        UV[f] = (end_uv(f, True, 0.10), toe_uv(f)) if f == 'footFar' else (end_uv(f, True, 0.10), end_uv(f, False, 0.12 if f == 'forearmFar' else 0.10))
SEG['armFar'] = [f if f in UV else f[:-3] for f in ('uparmFar', 'forearmFar')]
SEG['legFar'] = ['thigh', 'shin', 'footFar' if 'footFar' in UV else 'foot']


def size(name): return Image.open(os.path.join(D, name + '.png')).size


def seg_len(name):
    (u0, v0), (u1, v1) = UV[name]; w, h = size(name)
    return math.hypot((u1 - u0) * w, (v1 - v0) * h)


def put(name, a_uv, b_uv, p0, p1, s):
    """그림 점 a_uv 를 p0 에, a→b 방향을 p0→p1 로, 배율 s (길이는 안 맞춤)"""
    w, h = size(name)
    va = ((b_uv[0] - a_uv[0]) * w, -(b_uv[1] - a_uv[1]) * h); vd = (p1[0] - p0[0], -(p1[1] - p0[1]))
    th = math.degrees(math.atan2(vd[1], vd[0]) - math.atan2(va[1], va[0])); th = (th + 180) % 360 - 180
    m = {'w': w * s, 'h': h * s, 'angle': th, 'cx': 0, 'cy': 0, 's': s}
    px, py = place(m, a_uv); m['cx'] = p0[0] - px; m['cy'] = p0[1] - py
    return m


# 마디 배율 = 가까운 쪽 관절 거리 ÷ 그림 관절 거리 (먼 쪽도 이 값). 발은 두 쪽 평균
def dist(a, b): return math.hypot(b[0] - a[0], b[1] - a[1])
S = {'uparm': dist(*J['arm'][0:2]) / seg_len('uparm'), 'forearm': dist(*J['arm'][1:3]) / seg_len('forearm'),
     'thigh': dist(*J['leg'][0:2]) / seg_len('thigh'), 'shin': dist(*J['leg'][1:3]) / seg_len('shin'),
     'foot': (dist(*J['leg'][2:4]) + dist(*J['legFar'][2:4])) / 2 / seg_len('foot')}
for f in ('uparmFar', 'forearmFar', 'footFar'):
    if f in UV: S[f] = seg_len(f[:-3]) * S[f[:-3]] / seg_len(f)
print('마디 배율', {k: round(v, 3) for k, v in S.items()})

M = {}
full = rgba(key_magenta(Image.open(FULL)))
for n, reg in (('body', None), ('butt', (750, 150, 1300, 650)), ('head', (150, 100, 500, 420)), ('saddle', None), ('gunner', (400, 0, 750, 250))):
    P = rgba(Image.open(os.path.join(D, n + '.png')))
    M[n] = dict(find(full, P, region=reg), s=1.0)
    print(n, round(M[n]['cx']), round(M[n]['cy']), M[n]['angle'], M[n]['score'])
g2 = dict(M['gunner']); g2['cx'] += 390; g2['cy'] += 10; M['gunner2'] = g2

NAMES = {'arm': ['arm', 'fore'], 'armFar': ['armFar', 'foreFar'], 'leg': ['thigh', 'shin', 'foot'], 'legFar': ['thighFar', 'shinFar', 'footFar']}
SPR = {}
for chain, js in J.items():
    segs = SEG.get(chain, SEG[chain.replace('Far', '')])
    L = [seg_len(sg) * S[sg] for sg in segs]
    dirs = []
    for i in range(len(segs)):
        d = (js[i + 1][0] - js[i][0], js[i + 1][1] - js[i][1]); n = math.hypot(*d); dirs.append((d[0] / n, d[1] / n))
    if chain in FAR_DX:
        dx, near = FAR_DX[chain]
        start = (J[near][0][0] + dx, J[near][0][1])
    else: start = js[0]
    pts = [start]
    for i in range(len(segs)): pts.append((pts[i][0] + dirs[i][0] * L[i], pts[i][1] + dirs[i][1] * L[i]))
    if chain in FAR_DX:
        # 끝(주먹·발끝)은 통 그림의 먼 쪽 자리로: 발은 방향 그대로 두고 발목을, 팔은 주먹을 목표로 두 마디 IK (굽는 쪽 = 통 그림 관절 쪽)
        tgt = js[-1] if len(segs) == 2 else (js[-1][0] - dirs[2][0] * L[2], js[-1][1] - dirs[2][1] * L[2])
        a, b = L[0], L[1]; dxy = (tgt[0] - start[0], tgt[1] - start[1]); d = min(math.hypot(*dxy), a + b - 1)
        base = math.atan2(dxy[1], dxy[0]); off = math.acos(max(-1, min(1, (a * a + d * d - b * b) / (2 * a * d))))
        cands = [(start[0] + a * math.cos(base + sg * off), start[1] + a * math.sin(base + sg * off)) for sg in (1, -1)]
        mid = min(cands, key=lambda c: dist(c, js[1]))
        pts = [start, mid, tgt]
        if len(segs) == 3: pts.append(js[-1])
        dirs = [((pts[i + 1][0] - pts[i][0]) / dist(pts[i], pts[i + 1]), (pts[i + 1][1] - pts[i][1]) / dist(pts[i], pts[i + 1])) for i in range(len(segs))]
    for i, sg in enumerate(segs):
        nm = NAMES[chain][i]; SPR[nm] = sg
        M[nm] = put(sg, UV[sg][0], UV[sg][1], pts[i], pts[i + 1], S[sg])
    print(chain, [tuple(round(v) for v in p) for p in pts])

# 허벅지는 몸통에 붙임 (엉덩이가 씰룩여도 다리는 그대로)
# (부모, 관절 pivot, 순서, 움직임, 흔들림, 위상, 빠르기, 먼 쪽, 두 번째 그림)
T = [('body', None, (0.5, 0.5), 4, 'Bob', 4, 0, 2, False, None),
     ('armFar', 'body', UV[SPR['armFar']][0], -2, 'Walk', 14, 0.5, 1, True, None),
     ('foreFar', 'armFar', UV[SPR['foreFar']][0], -3, 'Walk', 8, 0.75, 1, True, None),
     ('butt', 'body', None, 3, 'Wiggle', 4, 0, 2, False, None),      # 걷다 멈춘 순간 허리 관절 축으로 엉덩이 한 번 출렁 (4°, 0.5초)
     ('thighFar', 'body', UV['thigh'][0], -2, 'Walk', 14, 0, 1, True, None),
     ('shinFar', 'thighFar', UV['shin'][0], -3, 'Walk', 8, 0.25, 1, True, None),
     ('footFar', 'shinFar', UV[SPR['footFar']][0], -4, 'Walk', 6, 0.5, 1, True, None),
     ('thigh', 'body', UV['thigh'][0], 5,      # 바깥 허벅지 = 엉덩이·몸통 위 (사용자)
      'Walk', 14, 0.5, 1, False, None),
     ('shin', 'thigh', UV['shin'][0], 1, 'Walk', 8, 0.75, 1, False, None),
     ('foot', 'shin', UV['foot'][0], 0, 'Walk', 6, 0, 1, False, None),
     ('arm', 'body', UV['uparm'][0], 6, 'Walk', 14, 0, 1, False, None),
     ('fore', 'arm', UV['forearm'][0], 5, 'Walk', 8, 0.25, 1, False, None),
     ('head', 'body', (0.76, 0.92), 7, 'Sway', 3, 0, 1, False, 'head2'),
     ('saddle', 'body', (0.5, 0.5), 8, 'None', 0, 0, 1, False, None),
     ('gunner1', 'saddle', (0.773, 0.95), 9, 'Recoil', 6, 0, 1, False, 'gunner2'),
     ('gunner2', 'saddle', (0.773, 0.95), 9, 'Recoil', 6, 0.5, 1, False, 'gunner2')]
MK = {'gunner1': 'gunner', 'gunner2': 'gunner2'}
parts = []
for name, par, piv, order, move, amp, ph, spd, far, alt in T:
    m = M[MK.get(name, name)]
    if piv is None:      # 엉덩이 관절 = 몸통과 맞닿는 왼쪽 끝 가운데쯤 (몸통 그림 안쪽)
        a = alpha('butt'); cols = np.where(a.any(0))[0]; c = cols[0] + int(0.08 * a.shape[1]); ys = np.where(a[:, c])[0]
        piv = (c / a.shape[1], (ys[0] + ys[-1]) / 2 / a.shape[0])
    if par is None:
        at = (0.0, (GROUND - place(m, piv)[1]) / m['h']); ang, sc = m['angle'], 1.0
    else:
        pm = M[MK.get(par, par)]
        at = to_uv(pm, place(m, piv)); ang = m['angle'] - pm['angle']; sc = m['s'] / pm['s']
    parts.append({'name': name, 'sprite': SPR.get(name, 'gunner' if name.startswith('gunner') else name), 'parent': par,
                  'at': [round(at[0], 3), round(at[1], 3)], 'pivot': [round(piv[0], 3), round(piv[1], 3)], 'order': order,
                  'move': move, 'amp': amp, 'phase': ph, 'speed': spd, 'angle': round(ang, 1), 'scale': round(sc, 3), 'far': far, 'alt': alt})
# 사용자가 확인한 값 유지: 머리 목 = 몸통 왼쪽 위 목 단면 · 앞 포탑 = 안장 0.32 (머리와 안 겹치게)
KEEP = {'head': {'at': [0.12, 0.26], 'angle': -12}, 'gunner1': {'at': [0.32, 0.089]}}
for p in parts: p.update(KEEP.get(p['name'], {}))
bx, _ = place(M['body'], (0.5, 0.5))
json.dump({'full_at': {'x': round(bx, 1), 'y': GROUND}, 'parts': parts}, open(sys.argv[2], 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
for p in parts: print(p['name'], p['at'], p['pivot'], p['angle'], p['scale'])
