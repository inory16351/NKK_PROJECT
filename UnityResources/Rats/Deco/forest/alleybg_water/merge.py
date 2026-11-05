"""sheet.png (Codex, 물만 새로) 의 물 부분 → 지금 도토리 숲 뒷골목 배경 base_forest.png 의 물 자리 (y 756~) 에 붙임 → out/lm_bg_forest.png
(sheet.png = 2차: 나무 반사를 또렷하게, base_v1 기준 · 1차 = sheet_v1.png, 돌 턱이 3픽셀 위라 SY 753 이었음). 물 위쪽은 지금 게임 그림 그대로."""
import os
from PIL import Image, ImageDraw, ImageFont
here = os.path.dirname(os.path.abspath(__file__)); Y, SY = 756, 756
base = Image.open(os.path.join(here, 'base_forest.png')).convert('RGB'); sheet = Image.open(os.path.join(here, 'sheet.png')).convert('RGB').resize(base.size, Image.LANCZOS)
out = base.copy(); out.paste(sheet.crop((0, SY, base.width, base.height)).resize((base.width, base.height - Y), Image.LANCZOS), (0, Y))
os.makedirs(os.path.join(here, 'out'), exist_ok=True); out.save(os.path.join(here, 'out', 'lm_bg_forest.png'))
JUA = os.path.join(here, '../../../../../Unity_Making/NKK_Project/Assets/Fonts/Jua-Regular.ttf'); F = ImageFont.truetype(JUA, 40)
orig = Image.open(os.path.join(here, 'ref_original.png')).convert('RGB')
crop = (0, 560, 1536, 1024); h = crop[3] - crop[1]
pv = Image.new('RGB', (1536, h * 3 + 30), (246, 236, 217)); d = ImageDraw.Draw(pv)
for i, (im, t) in enumerate([(base, '지금 도토리 숲 배경'), (out, '새 물 (나무가 일렁이며 비침)'), (orig, '참고: 원본 배경 물')]):
    pv.paste(im.crop(crop), (0, i * (h + 15))); d.rectangle((10, i * (h + 15) + 10, 30 + d.textlength(t, font=F), i * (h + 15) + 62), fill=(60, 35, 20)); d.text((20, i * (h + 15) + 12), t, font=F, fill=(255, 255, 255))
pv.save('C:/Project/NKK3/preview/alley_forest_water.png'); print('ok')
