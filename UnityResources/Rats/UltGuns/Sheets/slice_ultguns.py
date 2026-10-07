# 자홍 2×2 시트 → 칸마다 가장 큰 연결 영역만 남겨 자르기 (Tools/slice_rat_parts.key_magenta)
import os, sys, shutil
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, r'C:\Project\NKK_PROJECT\Tools')
from slice_rat_parts import key_magenta, crop

here = os.path.dirname(os.path.abspath(__file__))
out = os.path.dirname(here)
unity = r'C:\Project\NKK_PROJECT\Unity_Making\NKK_Project\Assets\Art\Rats\UltProps'
names = ['ult_pistol', 'ult_pistol_gold', 'ult_shell', 'ult_tracer']
im = key_magenta(Image.open(os.path.join(here, 'ult_guns.png')))
cw, ch = im.width // 2, im.height // 2
parts = []
for i, n in enumerate(names):
    cell = im.crop(((i % 2) * cw, (i // 2) * ch, (i % 2 + 1) * cw, (i // 2 + 1) * ch))
    a = np.array(cell)
    lab, k = ndimage.label(a[..., 3] > 20)
    if k > 1:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, k + 1))
        keep = 1 + int(np.argmax(sizes))
        a[lab != keep] = 0
        a[(lab == 0) & (a[..., 3] > 0)] = a[(lab == 0) & (a[..., 3] > 0)]
    c = crop(Image.fromarray(a, 'RGBA'))
    p = os.path.join(out, n + '.png'); c.save(p); shutil.copy(p, os.path.join(unity, n + '.png'))
    parts.append(c); print(n, c.size)
# 미리보기
W = sum(c.width for c in parts) + 20 * (len(parts) + 1); H = max(c.height for c in parts) + 40
pv = Image.new('RGBA', (W, H), (200, 196, 186, 255)); x = 20
for c in parts: pv.alpha_composite(c, (x, 20)); x += c.width + 20
pv.save(os.path.join(out, '_미리보기.png'))
