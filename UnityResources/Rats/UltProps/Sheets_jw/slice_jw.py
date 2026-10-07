# jw_stage.png (Codex 자홍 시트) → 소품 4장. 연결 영역으로 자름. 어둠 판은 가장자리 2px 안쪽으로 딱 맞게 (이어 붙일 때 틈 없게)
import sys, os, json
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, r'C:/Project/NKK_PROJECT/Tools')
from slice_rat_parts import key_magenta
im = key_magenta(Image.open('jw_stage.png'))
a = np.array(im.getchannel('A')) > 20
lab, n = ndimage.label(a)
objs = ndimage.find_objects(lab)
comps = sorted([(s, (lab[s] > 0).sum()) for s in objs], key=lambda c: -c[1])[:4]
comps.sort(key=lambda c: (c[0][0].start // 500, c[0][1].start))   # 위→아래, 왼→오른
names = ['jw_dark_hole', 'jw_dark', 'jw_bubble', 'jw_iris_hole']
info = {}
for (s, _), name in zip(comps, names):
    y0, y1, x0, x1 = s[0].start, s[0].stop, s[1].start, s[1].stop
    inset = 0 if name == 'jw_bubble' else 3
    c = im.crop((x0 + inset, y0 + inset, x1 - inset, y1 - inset))
    if name != 'jw_bubble':   # 판 부분은 완전 불투명으로 (가장자리 탈색 찌꺼기 방지)
        arr = np.array(c); al = arr[..., 3]
        arr[..., 3] = np.where(al > 128, 255, al); c = Image.fromarray(arr)
        hole = np.array(c.getchannel('A')) < 128
        ys, xs = np.where(hole)
        if len(xs) == 0: info[name] = dict(w=c.width, h=c.height)
        else: info[name] = dict(w=c.width, h=c.height, cx=(xs.min() + xs.max() + 1) / 2 / c.width, cy=(ys.min() + ys.max() + 1) / 2 / c.height,
                          hw=(xs.max() - xs.min() + 1) / c.width, hh=(ys.max() - ys.min() + 1) / c.height)
    else: info[name] = dict(w=c.width, h=c.height)
    c.save(os.path.join('..', name + '.png'))
print(json.dumps(info, indent=1))
