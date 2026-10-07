# fx_cheese.png (자홍 2x2) → 칸마다 자홍 빼고 잘라 저장 + Unity 사본 (.meta 는 cheese_puddle 것을 새 guid 로)
import sys, uuid, shutil, os
import numpy as np
from PIL import Image
sys.path.insert(0, r'C:/Project/NKK_PROJECT/Tools')
from slice_rat_parts import key_magenta, crop
from scipy import ndimage

ROOT = r'C:/Project/NKK_PROJECT'
HERE = ROOT + '/UnityResources/Rats/FX_Cheese'
UNITY = ROOT + '/Unity_Making/NKK_Project/Assets/Art/Rats'
names = ['cheese_splash', 'cheese_chunk', 'cheese_crown', 'cheese_glob']
im = key_magenta(Image.open(HERE + '/Sheets/fx_cheese.png'))
W, H = im.size; cw, ch = W // 2, H // 2
outs = []
for i, n in enumerate(names):
    cx, cy = i % 2, i // 2
    cell = im.crop((cx * cw, cy * ch, (cx + 1) * cw, (cy + 1) * ch))
    # 작은 잡티 제거 (연결 영역 중 큰 것만)
    a = np.array(cell)
    lab, k = ndimage.label(a[..., 3] > 20)
    if k > 1:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, k + 1))
        keep = [j + 1 for j, s in enumerate(sizes) if s >= max(60, sizes.max() * 0.01)]
        a[~np.isin(lab, keep) & (a[..., 3] > 0)] = 0
        cell = Image.fromarray(a, 'RGBA')
    c = crop(cell, 4)
    c.save(f'{HERE}/{n}.png'); outs.append(c)
    print(n, c.size)

meta = open(UNITY + '/NewRats/cheese_puddle.png.meta', encoding='utf-8').read()
old = meta.split('guid: ')[1].split('\n')[0].strip()
for d in ['FX_Cheese', 'UltProps']:
    os.makedirs(f'{UNITY}/{d}', exist_ok=True)
    for n in names:
        shutil.copy(f'{HERE}/{n}.png', f'{UNITY}/{d}/{n}.png')
        open(f'{UNITY}/{d}/{n}.png.meta', 'w', encoding='utf-8', newline='\n').write(meta.replace(old, uuid.uuid4().hex))
# 미리보기
pw = sum(o.width for o in outs) + 20 * (len(outs) + 1); ph = max(o.height for o in outs) + 40
pv = Image.new('RGBA', (pw, ph), (58, 58, 72, 255)); x = 20
for o in outs: pv.alpha_composite(o, (x, 20)); x += o.width + 20
pv.save(HERE + '/_미리보기.png')
