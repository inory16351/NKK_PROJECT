"""차력거인 리그 값 계산 (보스 개편 2026-10-22): 통 그림(tt4_full)에서 찾은 몸통·엉덩이·머리·안장·포탑 자리(match_parts.py JSON) + 통 그림에서 잰 관절
(어깨·팔꿈치·주먹 / 엉덩이·무릎·발목·발끝) → BossPartArt 값 (at, pivot, angle, scale) 출력 + 확인 그림.
사용: python Tools/giant_rig_calc.py <매칭.json> <출력.json> <확인.png>   (파츠 관절 uv 는 아래 상수, 파츠를 바꾸면 다시 잼)"""
import json, math, sys
sys.path.insert(0, 'Tools')
from match_parts import place, to_uv
from PIL import Image
D = 'UnityResources/Rats/BossRework/Parts/boss_cartgiant/'
res = json.load(open(sys.argv[1]))
M = {k: dict(v[0], s=1.0) for k, v in res.items() if k in ('body', 'butt', 'head', 'saddle', 'gunner')}
g2 = dict(M['gunner']); g2['cx'] += 390; g2['cy'] += 10; M['gunner2'] = g2
def limb(part, p0, p1, J0, J1):
    w, h = Image.open(D + part + '.png').size
    a = ((p1[0] - p0[0]) * w, -(p1[1] - p0[1]) * h); d = (J1[0] - J0[0], -(J1[1] - J0[1]))
    s = math.hypot(*d) / math.hypot(*a)
    th = math.degrees(math.atan2(d[1], d[0]) - math.atan2(a[1], a[0]))
    th = (th + 180) % 360 - 180
    m = {'w': w * s, 'h': h * s, 'angle': th, 'cx': 0, 'cy': 0, 's': s}
    px, py = place(m, p0); m['cx'] = J0[0] - px; m['cy'] = J0[1] - py
    return m
UH, TH_H = 1, 1      # tt8: 관절 원 없음 → 자르기 없음
UP, EL = (0.475, 0.22), (0.63, 0.912)      # 윗팔 = 어깨~팔꿈치 (LIMB_CUT 로 자름)
EL_IN = EL
FE, FI = (0.54, 0.12), (0.45, 0.88)
TH, KN = (0.45, 0.17), (0.6, 0.85)
KN_IN = KN
SK, AN = (0.28, 0.12), (0.67, 0.91)
FA, TO = (0.66, 0.21), (0.05, 0.82)
# 가까운 팔꿈치는 통 그림(625,640)보다 위: 자른 윗팔(어깨~팔꿈치)이 짧아 너무 늘어나지 않게
J = {'arm': [(600, 330), (615, 585), (605, 930)], 'armFar': [(480, 430), (455, 650), (410, 900)],
     'leg': [(1080, 480), (975, 770), (1320, 830), (1180, 955)], 'legFar': [(870, 500), (825, 740), (1130, 820), (985, 945)]}
for side in ('', 'Far'):
    a = J['arm' + side]; l = J['leg' + side]
    M['arm' + side] = limb('uparm', UP, EL, a[0], a[1]); M['fore' + side] = limb('forearm', FE, FI, place(M['arm' + side], EL_IN), a[2])
    M['thigh' + side] = limb('thigh', TH, KN, l[0], l[1]); M['shin' + side] = limb('shin', SK, AN, place(M['thigh' + side], KN_IN), l[2]); M['foot' + side] = limb('foot', FA, TO, l[2], l[3])
spr = {'arm': 'uparm', 'armFar': 'uparm', 'fore': 'forearm', 'foreFar': 'forearm', 'thigh': 'thigh', 'thighFar': 'thigh', 'shin': 'shin', 'shinFar': 'shin', 'foot': 'foot', 'footFar': 'foot'}
# (부모, 관절)
T = {'body': (None, (0.5, 0.5)), 'butt': ('body', (0.1, 0.4)), 'head': ('body', (0.76, 0.92)), 'saddle': ('body', (0.5, 0.5)),
     'gunner': ('saddle', (0.75, 0.95)), 'gunner2': ('saddle', (0.75, 0.95)),
     'arm': ('body', UP), 'fore': ('arm', FE), 'armFar': ('body', UP), 'foreFar': ('armFar', FE),  # fore 'at' = EL_IN
     'thigh': ('butt', TH), 'shin': ('thigh', SK), 'thighFar': ('butt', TH), 'shinFar': ('thighFar', SK), 'foot': ('shin', FA), 'footFar': ('shinFar', FA)}
ground = 990
out = {}
for n, (par, piv) in T.items():
    m = M[n]
    if par is None:
        out[n] = ((0.0, round((ground - place(m, piv)[1]) / m['h'], 3)), piv, round(m['angle'], 1), 1.0)
    else:
        pm = M[par]
        at = to_uv(pm, place(m, piv))
        out[n] = (tuple(round(v, 3) for v in at), piv, round(m['angle'] - pm['angle'], 1), round(m['s'] / pm['s'], 3))
for n, v in out.items(): print(n, v)
json.dump({k: [list(v[0]), list(v[1]), v[2], v[3]] for k, v in out.items()}, open(sys.argv[2], 'w'), indent=1)
# 확인 그림: 계산한 자리에 파츠를 놓아 통 그림과 겹침
from match_parts import overlay
from slice_rat_parts import key_magenta
parts = {}; resx = {}
for n in T:
    f = spr.get(n, n); im = Image.open(D + f + '.png').convert('RGBA'); s = M[n]['s']
    if s != 1: im = im.resize((round(im.width * s), round(im.height * s)))
    parts[n] = im; resx[n] = [M[n]]
overlay(key_magenta(Image.open('UnityResources/Rats/BossRework/Sheets/tt4_full.png')), parts, resx, sys.argv[3])
