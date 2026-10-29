"""나비쥐 날개 한 쌍 시트 (butterfly_wingpairs.png, 자홍 배경) → Parts/butterfly/{wing_far,wing,wing2_far,wing2}.png
칸이 아니라 덩어리(연결 영역) 4개를 왼쪽부터 (둥근 날개가 칸 경계를 넘어 잘리던 것 수정) · 밑동 돌기 가운데가 그림 가운데에 오게 옆을 채움 (회전 축)
사용: python Tools/slice_butterfly_wings.py <시트.png>"""
import os, sys
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta, crop, RES
im = key_magenta(Image.open(sys.argv[1])); a = np.array(im)
lab, n = ndimage.label(ndimage.binary_dilation(a[..., 3] > 20, iterations=4))
comps = []
for k in range(1, n + 1):
    ys, xs = np.nonzero((lab == k) & (a[..., 3] > 20))
    if len(xs) > 3000: comps.append((xs.mean(), k))
comps = sorted(comps)[:4]
assert len(comps) == 4, len(comps)
for (_, k), name in zip(comps, ['wing_far', 'wing', 'wing2_far', 'wing2']):
    b = a.copy(); b[..., 3] = np.where((lab == k) & (a[..., 3] > 0), a[..., 3], 0)
    p = crop(Image.fromarray(b)); al = np.array(p)[..., 3] > 20
    ys, xs = np.nonzero(al); band = ys > ys.max() - max(4, int(p.height * .05)); sx = xs[band].mean()
    half = max(sx, p.width - sx) + 2
    c = Image.new('RGBA', (int(round(half * 2)), p.height), (0, 0, 0, 0)); c.paste(p, (int(round(half - sx)), 0))
    c.save(os.path.join(RES, 'Parts', 'butterfly', name + '.png')); print(name, p.size, '->', c.size)
