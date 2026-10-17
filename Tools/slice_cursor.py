# 마우스 포인터 시트 자르기: UnityResources/Rats/Cursor/Sheets/cursor_sheet.png (자홍 배경, 왼쪽 = 평소 · 오른쪽 = 누름)
# → 키잉 → 둘 다 코끝(왼쪽 위 끝점)을 같은 자리에 맞춰 정사각 캔버스 → SIZE px 로 줄임.
# 출력: UnityResources/Rats/Cursor/cursor_normal.png · cursor_pressed.png (+ 큰 원본 _big) · 끝점 픽셀 출력
#   python Tools/slice_cursor.py [SIZE=48]
import sys, os
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from slice_rat_parts import key_magenta

ROOT = os.path.join(os.path.dirname(__file__), '..', 'UnityResources', 'Rats', 'Cursor')
SIZE = int(sys.argv[1]) if len(sys.argv) > 1 else 48
PAD = 0.03          # 코끝 앞 여백 (캔버스 비율)

sheet = key_magenta(Image.open(os.path.join(ROOT, 'Sheets', 'cursor_sheet.png')))
W, H = sheet.size
halves = {'normal': sheet.crop((0, 0, W // 2, H)), 'pressed': sheet.crop((W // 2, 0, W, H))}

# 둘 중 큰 쪽 기준으로 같은 캔버스 크기
info = {}
for k, im in halves.items():
    a = np.array(im.getchannel('A')) > 20
    ys, xs = np.where(a)
    i = np.argmin(xs + ys)                  # 코끝 = 왼쪽 위로 가장 튀어나온 점
    info[k] = (xs[i], ys[i], xs.max(), ys.max())
side = max(max(x1 - tx, y1 - ty) for tx, ty, x1, y1 in info.values())
side = int(side / (1 - PAD))
pad = int(side * PAD)

for k, im in halves.items():
    tx, ty, _, _ = info[k]
    can = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    can.paste(im.crop((tx - pad, ty - pad, tx - pad + side, ty - pad + side)), (0, 0))
    can.save(os.path.join(ROOT, f'cursor_{k}_big.png'))
    can.resize((SIZE, SIZE), Image.LANCZOS).save(os.path.join(ROOT, f'cursor_{k}.png'))
hot = pad * SIZE / side
print(f'{SIZE}px · 끝점(hotspot) = ({hot:.1f}, {hot:.1f}) px')
