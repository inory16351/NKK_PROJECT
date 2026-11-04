import sys, os
sys.path.insert(0, 'Tools')
from slice_rat_parts import key_magenta, crop
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
im = key_magenta(Image.open(src))
names = ['head', 'torso', 'wing', 'wing_far', 'wing_fold', 'feet']
for i, n in enumerate(names):
    r, c = divmod(i, 3)
    cell = im.crop((c*512, r*512, c*512+512, r*512+512))
    p = crop(cell)
    p.save(f'{out}/{n}.png'); print(n, p.size)
