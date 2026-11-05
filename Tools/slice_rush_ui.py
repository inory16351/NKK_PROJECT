# 총공격 UI 시트 자르기: UnityResources/Rats/RushUI/Sheets/rush_ui_sheet.png (Codex, 자홍 배경, 3열 × 2행)
#   1행: 빈 쿨타임 원 · 주황 채움 원 · 노랑 채움 원 / 2행: 화살표(왼쪽) · 작은 점 · 가운데 원
# → 키잉 → 칸마다 그림만 잘라냄. 채움 원 2개는 빈 원의 크림색 띠 모양으로 잘라 (테두리 안쪽에 딱 맞게) 같은 캔버스에 겹침.
# 출력: UnityResources/Rats/RushUI/*.png → Assets/Resources/RushUI/ 로 복사
#   python Tools/slice_rush_ui.py
import os, sys, shutil
import numpy as np
from PIL import Image, ImageFilter
sys.path.insert(0, os.path.dirname(__file__))
from slice_rat_parts import key_magenta, crop

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, '..', 'UnityResources', 'Rats', 'RushUI')
DST = os.path.join(HERE, '..', 'Unity_Making', 'NKK_Project', 'Assets', 'Resources', 'RushUI')

sheet = key_magenta(Image.open(os.path.join(ROOT, 'Sheets', 'rush_ui_sheet.png')))
W, H = sheet.size
cw, ch = W // 3, H // 2
cells = [[crop(sheet.crop((c * cw, r * ch + (30 if r else 0), (c + 1) * cw, (r + 1) * ch)), pad=2) for c in range(3)] for r in range(2)]      # 2행은 위 30px 버림 (윗줄 원 끝자락)

def square(im, side):
    can = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    can.paste(im, ((side - im.width) // 2, (side - im.height) // 2), im)
    return can

RING = 256
frame, fill, ready = cells[0]
side = max(frame.width, frame.height)
frame_sq = square(frame, side)
# 크림색 띠 = 밝은 픽셀 (테두리 갈색은 어두움) → 채움 원을 이 모양으로 자름
fa = np.array(frame_sq).astype(np.float32)
lum = fa[..., :3].mean(axis=2)
track = ((lum > 150) & (fa[..., 3] > 128)).astype(np.uint8) * 255
track = Image.fromarray(track, 'L').filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(0.8))

def clip_to_track(im):
    sq = square(im.resize((side, int(im.height * side / im.width)), Image.LANCZOS), side)
    a = np.array(sq).astype(np.float32)
    a[..., 3] = np.minimum(a[..., 3], np.array(track).astype(np.float32))
    return Image.fromarray(a.astype(np.uint8), 'RGBA')

out = {
    'rush_ring_frame': frame_sq.resize((RING, RING), Image.LANCZOS),
    'rush_ring_fill': clip_to_track(fill).resize((RING, RING), Image.LANCZOS),
    'rush_ring_ready': clip_to_track(ready).resize((RING, RING), Image.LANCZOS),
}
arrow, knob, center = cells[1]
def fit(im, n):
    s = n / max(im.width, im.height)
    return square(im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS), n)
out['rush_arrow'] = fit(arrow, 128)
out['rush_knob'] = fit(knob, 64)
out['rush_center'] = fit(center, 128)

os.makedirs(DST, exist_ok=True)
for k, im in out.items():
    p = os.path.join(ROOT, k + '.png'); im.save(p)
    shutil.copy(p, os.path.join(DST, k + '.png'))
    print(k, im.size)
