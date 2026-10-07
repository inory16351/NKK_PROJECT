# jw_tux.png (Codex 자홍 3칸: 몸통·앞다리·뒷다리) → 줴리 원래 파츠와 같은 픽셀 크기로 맞춘 턱시도 파츠 (피벗·관절 비율이 그대로 맞게)
import os, sys
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, r'C:/Project/NKK_PROJECT/Tools')
from slice_rat_parts import key_magenta, crop

ORIG = r'C:/Project/NKK_PROJECT/Unity_Making/NKK_Project/Assets/Art/Rats/Parts/jwerry'
im = key_magenta(Image.open('jw_tux.png'))
cw = im.width // 3
for i, part in enumerate(['torso', 'front', 'back']):
    cell = im.crop((i * cw, 0, (i + 1) * cw, im.height))
    a = np.array(cell.getchannel('A')) > 20
    lab, n = ndimage.label(a)
    big = np.argmax(ndimage.sum(a, lab, range(1, n + 1))) + 1      # 가장 큰 덩어리만
    arr = np.array(cell); arr[lab != big] = 0
    c = crop(Image.fromarray(arr), pad=0)
    o = Image.open(os.path.join(ORIG, part + '.png'))
    c = c.resize(o.size, Image.LANCZOS)
    c.save(os.path.join('..', 'jwt_' + part + '.png'))
    print(part, o.size)
