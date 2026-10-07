import sys, numpy as np
sys.path.insert(0, r'C:/Project/NKK_PROJECT/Tools')
from PIL import Image
from scipy import ndimage
from slice_rat_parts import key_magenta, crop
names = ['rock_stage','rock_speaker','rock_amp','rock_truss','rock_drums','rock_guitar_broken','rock_mic','rock_lamp','rock_pyro']
im = key_magenta(Image.open('rock_stage_sheet.png'))
W = im.width // 3
for i, n in enumerate(names):
    cell = im.crop(((i % 3) * W, (i // 3) * W, (i % 3 + 1) * W, (i // 3 + 1) * W))
    a = np.array(cell)
    lab, k = ndimage.label(a[..., 3] > 20)
    if k:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, k + 1))
        keep = [j + 1 for j, s in enumerate(sizes) if s >= max(60, sizes.max() * 0.002)]
        a[~np.isin(lab, keep)] = 0
    c = crop(Image.fromarray(a, 'RGBA'))
    c.save(f'../{n}.png'); print(n, c.size, round(c.height / c.width, 3))
