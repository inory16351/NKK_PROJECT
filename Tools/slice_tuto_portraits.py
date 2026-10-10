"""튜토리얼 해설 쥐 초상화 자르기: UnityResources/Rats/TutoPortraits/Sheets/tuto_<쥐>.png (2×2, 자홍 배경)
→ 자홍 키잉 → 칸마다 잘라 → 대사창 쪽(오른쪽)을 보게 좌우 뒤집기(필요한 쥐만) → 같은 크기 캔버스에 아래 가운데 맞춤
→ UnityResources/Rats/TutoPortraits/tuto_<쥐>_<표정>.png + Assets/Art/Rats/TutoPortraits/ 에 복사
칸 순서: 왼쪽 위 normal · 오른쪽 위 happy · 왼쪽 아래 surprised · 오른쪽 아래 panic
사용: python Tools/slice_tuto_portraits.py"""
import os, shutil
from PIL import Image
from slice_rat_parts import key_magenta, crop

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'UnityResources', 'Rats', 'TutoPortraits')
DST = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats', 'TutoPortraits')
FACES = ['normal', 'happy', 'surprised', 'panic']
# 시트에서 왼쪽을 보고 있는 쥐 → 뒤집어서 오른쪽(대사 글 쪽)을 보게
FLIP = {'labrat', 'nerd', 'hero'}
SIZE = 640          # 결과 캔버스 (정사각형)
HEIGHT = 600        # 쥐마다 네 표정 중 가장 큰 그림의 높이를 이 값에 맞춤 (표정끼리 크기 비율은 그대로)

os.makedirs(DST, exist_ok=True)
for code in ['labrat', 'nerd', 'brownrat', 'hero']:
    sheet = os.path.join(SRC, 'Sheets', f'tuto_{code}.png')
    if not os.path.exists(sheet): print('없음:', sheet); continue
    im = key_magenta(Image.open(sheet))
    w, h = im.size
    cells = [crop(im.crop((x * w // 2, y * h // 2, (x + 1) * w // 2, (y + 1) * h // 2)), pad=2) for y in range(2) for x in range(2)]
    k = min(HEIGHT / max(c.height for c in cells), SIZE / max(c.width for c in cells))
    for face, c in zip(FACES, cells):
        c = c.resize((max(1, round(c.width * k)), max(1, round(c.height * k))), Image.LANCZOS)
        if code in FLIP: c = c.transpose(Image.FLIP_LEFT_RIGHT)
        out = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0))
        out.paste(c, ((SIZE - c.width) // 2, SIZE - c.height), c)
        name = f'tuto_{code}_{face}.png'
        out.save(os.path.join(SRC, name))
        shutil.copy(os.path.join(SRC, name), os.path.join(DST, name))
        print(name, c.size)
