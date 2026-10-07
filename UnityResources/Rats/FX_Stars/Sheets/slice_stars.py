# stars_sheet.png (Codex 자홍 3×2) → 칸별 가장 큰 덩어리 → 기존 별 PNG 자리에 덮어쓰기
# 새 그림은 원래 그림의 불투명 영역(bbox)에 비율 유지로 맞춰 같은 캔버스 크기·같은 중심에 둠 (.meta·피벗·크기 그대로)
import os, sys, shutil
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, r'C:/Project/NKK_PROJECT/Tools')
from slice_rat_parts import key_magenta, crop

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)                      # UnityResources/Rats/FX_Stars
RES = r'C:/Project/NKK_PROJECT/UnityResources/Rats'
ART = r'C:/Project/NKK_PROJECT/Unity_Making/NKK_Project/Assets/Art'
NAMES = ['star_round', 'twinkle_chunky', 'sparkle_small', 'star_burst', 'twinkle_tall', 'star_yellow']

im = key_magenta(Image.open(os.path.join(HERE, 'stars_sheet.png')))
cw, ch = im.width // 3, im.height // 2
cells = {}
for i, n in enumerate(NAMES):
    c = im.crop(((i % 3) * cw, (i // 3) * ch, (i % 3 + 1) * cw, (i // 3 + 1) * ch))
    a = np.array(c.getchannel('A')) > 20
    lab, k = ndimage.label(a)
    big = np.argmax(ndimage.sum(a, lab, range(1, k + 1))) + 1
    arr = np.array(c); arr[lab != big] = 0
    cells[n] = crop(Image.fromarray(arr), pad=0)
    cells[n].save(os.path.join(ROOT, 'new', n + '.png'))

def fit(src, dst_path):
    o = Image.open(dst_path).convert('RGBA')
    a = np.array(o.getchannel('A')) > 20
    ys, xs = np.where(a)
    bw, bh = xs.max() - xs.min() + 1, ys.max() - ys.min() + 1
    cx, cy = (xs.min() + xs.max() + 1) / 2, (ys.min() + ys.max() + 1) / 2
    s = min(bw / src.width, bh / src.height)
    r = src.resize((max(1, round(src.width * s)), max(1, round(src.height * s))), Image.LANCZOS)
    out = Image.new('RGBA', o.size, (0, 0, 0, 0))
    out.alpha_composite(r, (int(round(cx - r.width / 2)), int(round(cy - r.height / 2))))
    return out

# (새 그림, Unity 파일, UnityResources 사본)
MAP = [
    ('star_round',     ART + '/FX/Tint/star.png',                  RES + '/FX_New/tint/star.png'),
    ('twinkle_chunky', ART + '/FX/Tint/twinkle.png',               RES + '/FX_New/tint/twinkle.png'),
    ('star_round',     ART + '/Generated/star.png',                None),
    ('star_round',     ART + '/Rats/FX_SuperJump/sj_star5.png',    RES + '/FX_SuperJump/sj_star5.png'),
    ('twinkle_tall',   ART + '/Rats/FX_SuperJump/sj_sparkle.png',  RES + '/FX_SuperJump/sj_sparkle.png'),
    ('sparkle_small',  ART + '/Rats/FX_SuperJump/sj_twinkle.png',  RES + '/FX_SuperJump/sj_twinkle.png'),
    ('star_yellow',    ART + '/Rats/FX_SuperJump/sj_star.png',     RES + '/FX_SuperJump/sj_star.png'),
]
for n, dst, res in MAP:
    out = fit(cells[n], dst)
    out.save(dst)
    if res: out.save(res)
    print(n, '->', dst, out.size)
