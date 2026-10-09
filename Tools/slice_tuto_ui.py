"""튜토리얼 UI 조각 자르기: UnityResources/Rats/TutoUI/Sheets/tuto_ui.png (자홍 배경, Codex)
→ ui_frame (강조 테두리, 9-슬라이스) · ui_arrow (위를 가리키는 화살표) · ui_lock (자물쇠)
→ UnityResources/Rats/TutoUI/ + Assets/Art/UI_Kit/ 에 복사 (테두리 = 메뉴 NKK/Apply UI Kit Borders)
사용: python Tools/slice_tuto_ui.py"""
import os, shutil
from PIL import Image
from slice_rat_parts import key_magenta, crop

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'UnityResources', 'Rats', 'TutoUI')
DST = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'UI_Kit')
im = key_magenta(Image.open(os.path.join(SRC, 'Sheets', 'tuto_ui.png')))
# 시트 안 위치 (1536×1024): 왼쪽 테두리 · 오른쪽 위 화살표 · 오른쪽 아래 자물쇠. 크기 배율
PIECES = {'ui_frame': ((20, 120, 1080, 900), 0.5), 'ui_arrow': ((1090, 40, 1520, 530), 0.5), 'ui_lock': ((1100, 540, 1500, 960), 0.5)}
for name, (box, k) in PIECES.items():
    c = crop(im.crop(box), pad=2)
    c = c.resize((round(c.width * k), round(c.height * k)), Image.LANCZOS)
    c.save(os.path.join(SRC, name + '.png')); shutil.copy(os.path.join(SRC, name + '.png'), os.path.join(DST, name + '.png'))
    print(name, c.size)
