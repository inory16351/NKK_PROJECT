"""ratdance_parts_v2.png (자홍, 4x2) → 파츠 PNG (head, head_dance, body, arm, leg, tail) + 참고용 전체 자세"""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../../../../Tools'))
from slice_rat_parts import key_magenta, crop
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__)); out = os.path.dirname(here)
im = key_magenta(Image.open(os.path.join(here, 'ratdance_parts_v2.png')))
names = ['head', 'head_dance', 'body', 'arm', 'leg', 'tail', '_ref_full_pose']
for i, n in enumerate(names):
    r, c = divmod(i, 4)
    p = crop(im.crop((c*384, r*512, c*384+384, r*512+512)))
    p.save(os.path.join(out if not n.startswith('_') else here, n.lstrip('_') + '.png')); print(n, p.size)
