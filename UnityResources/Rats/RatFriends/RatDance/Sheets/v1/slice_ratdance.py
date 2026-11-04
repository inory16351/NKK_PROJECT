"""ratdance_parts.png (자홍, 4x2) → 파츠 PNG + 위쪽 관절 위치 출력"""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../../../../Tools'))
from slice_rat_parts import key_magenta, crop
from PIL import Image
import numpy as np
here = os.path.dirname(os.path.abspath(__file__)); out = os.path.dirname(here)
im = key_magenta(Image.open(os.path.join(here, 'ratdance_parts.png')))
names = ['head', 'head_dance', 'up', 'low', 'arm', 'arm_up', 'leg', 'tail']
for i, n in enumerate(names):
    r, c = divmod(i, 4)
    p = crop(im.crop((c*384, r*512, c*384+384, r*512+512)))
    p.save(os.path.join(out, n + '.png'))
    a = np.array(p.getchannel('A')) > 128; ys, xs = np.where(a)
    top = xs[ys <= ys.min() + 6].mean() / p.width; bot = xs[ys >= ys.max() - 6].mean() / p.width
    print(n, p.size, 'top x', round(top, 3), 'bottom x', round(bot, 3))
