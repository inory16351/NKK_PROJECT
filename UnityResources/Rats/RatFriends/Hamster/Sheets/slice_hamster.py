"""hamster_parts.png (자홍, 4x2) → 파츠 PNG (head, head_blink, body, arm, foot, tail) + 참고용 전체 자세"""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../../../../Tools'))
from slice_rat_parts import key_magenta, crop
import numpy as np
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__)); out = os.path.dirname(here)
im = key_magenta(Image.open(os.path.join(here, 'hamster_parts.png')))
names = ['head', 'head_blink', 'body', 'arm', 'foot', 'tail', '_ref_full_pose']
for i, n in enumerate(names):
    r, c = divmod(i, 4)
    p = crop(im.crop((c*384, r*512, c*384+384, r*512+512)))
    a = np.array(p).astype(np.int32)                       # 가장자리 자홍빛 지움
    t = (a[..., 0] > a[..., 1] + 40) & (a[..., 2] > a[..., 1] + 40) & (a[..., 3] < 255); a[t, 3] = 0
    p = Image.fromarray(a.astype(np.uint8))
    p.save(os.path.join(out if not n.startswith('_') else here, n.lstrip('_') + '.png')); print(n, p.size)
