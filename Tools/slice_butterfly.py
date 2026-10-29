"""나비쥐 시트 (4열 x 2줄, 자홍 배경) → UnityResources/Rats/Parts/butterfly/{head,tail,wing,wing2}.png + pivots.json 갱신
칸: 1줄 = 머리 · 꼬리 / 2줄 = 큰 귀 날개 · 작은 아래 날개.  사용: python Tools/slice_butterfly.py <시트.png>"""
import io, json, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta, crop, tail_pivot, RES
from scipy import ndimage

im = key_magenta(Image.open(sys.argv[1])); a0 = np.array(im); H, W = a0.shape[:2]
out = os.path.join(RES, 'Parts', 'butterfly'); os.makedirs(out, exist_ok=True)
for f in os.listdir(out):
    if f.endswith('.png'): os.remove(os.path.join(out, f))
near = ndimage.binary_dilation(a0[..., 3] > 20, iterations=4); lab, n = ndimage.label(near)
def cell(r, c):
    x0, x1, y0, y1 = int(c * W / 4), int((c + 1) * W / 4), int(r * H / 2), int((r + 1) * H / 2)
    best, bs = 0, 0
    for k in range(1, n + 1):
        ys, xs = np.nonzero((lab == k) & (a0[..., 3] > 20))
        if len(xs) < 200: continue
        if x0 <= xs.mean() < x1 and y0 <= ys.mean() < y1 and len(xs) > bs: best, bs = k, len(xs)
    if not best: return None
    arr = a0.copy(); arr[..., 3] = np.where((lab == best) & (a0[..., 3] > 0), a0[..., 3], 0)
    return crop(Image.fromarray(arr))
parts = {'head': cell(0, 0), 'tail': cell(0, 1), 'wing': cell(1, 0), 'wing2': cell(1, 1)}
for k, v in parts.items():
    if v is None: print('빈 칸', k); continue
    v.save(os.path.join(out, k + '.png')); print(k, v.size)
pj_path = os.path.join(RES, 'Parts', 'pivots.json'); pj = json.load(io.open(pj_path, encoding='utf-8'))
tp = tail_pivot(parts['tail'])
piv = {'head': [0.5, 0.5], 'tail': tp}
pj['species']['butterfly'] = {'parts': ['Parts/butterfly/%s.png' % p for p in parts], 'pivot': piv, 'pivotUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in piv.items()},
    'torsoAnchor': {'neck': [0.069, 0.395], 'tail': [0.944, 0.531], 'shoulder': [0.238, 0.448], 'hip': [0.736, 0.325]}, 'legScale': {'front': 1, 'back': 1}}
json.dump(pj, io.open(pj_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
