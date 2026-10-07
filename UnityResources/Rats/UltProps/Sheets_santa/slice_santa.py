# santa_deer.png (Codex 자홍 2×2 시트) → 소품 4장. key_magenta + 연결 영역 (칸마다 가장 큰 덩어리)
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, r'C:/Project/NKK_PROJECT/Tools')
from slice_rat_parts import key_magenta
im = key_magenta(Image.open('santa_deer.png'))
a = np.array(im.getchannel('A')) > 20
lab, n = ndimage.label(a)
objs = ndimage.find_objects(lab)
comps = sorted([(s, (lab[s] > 0).sum()) for s in objs], key=lambda c: -c[1])[:4]
comps.sort(key=lambda c: (c[0][0].start // 700, c[0][1].start))   # 위→아래, 왼→오른
names = ['deer_antlers', 'deer_nose', 'deer_bottle', 'deer_bells']
for (s, _), name in zip(comps, names):
    y0, y1, x0, x1 = s[0].start, s[0].stop, s[1].start, s[1].stop
    c = im.crop((max(0, x0 - 2), max(0, y0 - 2), x1 + 2, y1 + 2))
    c.save(os.path.join('..', name + '.png')); print(name, c.size)
