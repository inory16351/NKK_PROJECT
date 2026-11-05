"""번식 탄생 연출 그림 자르기 (2026-11-05). UnityResources/Rats/FX_Birth/Sheets/birth_fx.png (Codex, 자홍 배경)
→ 자홍 키잉 → 영역별로 잘라 UnityResources/Rats/FX_Birth/*.png + Assets/Art/FX_Birth/ 에 복사.
사용: python Tools/slice_birth_fx.py"""
import os, shutil, sys
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta, crop
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'UnityResources/Rats/FX_Birth')
DST = os.path.join(ROOT, 'Unity_Making/NKK_Project/Assets/Art/FX_Birth')
im = key_magenta(Image.open(os.path.join(SRC, 'Sheets/birth_fx.png')))
W, H = im.size
# 영역 (시트 비율): 큰 하트 = 왼쪽 반 · 햇살 = 오른쪽 위 · 반짝이 = 오른쪽 아래 왼쪽 · 작은 하트 = 오른쪽 아래 오른쪽
boxes = {'birth_heart': (0, 0, 0.53, 1), 'birth_rays': (0.6, 0, 1, 0.66), 'birth_sparkle': (0.56, 0.66, 0.78, 1), 'birth_heart_small': (0.78, 0.66, 1, 1)}
os.makedirs(DST, exist_ok=True)
for name, (x0, y0, x1, y1) in boxes.items():
    c = crop(im.crop((int(x0 * W), int(y0 * H), int(x1 * W), int(y1 * H))))
    if name == 'birth_rays':       # 가운데를 그림 가운데로 (돌릴 때 흔들리지 않게): 정사각형으로 맞춤
        s = max(c.size); sq = Image.new('RGBA', (s, s)); sq.paste(c, ((s - c.width) // 2, (s - c.height) // 2)); c = sq
    p = os.path.join(SRC, name + '.png'); c.save(p); shutil.copy(p, os.path.join(DST, name + '.png'))
    print(name, c.size)
