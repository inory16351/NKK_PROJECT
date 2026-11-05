"""sheet.png (Codex, 물만 새로) 의 물 부분 (y 763~) → 지금 네온 뒷골목 배경 base_neon.png 에 붙임 → out/lm_bg_neon.png
물 위쪽 (배관 · 가게 · 바닥 · 돌 턱) 은 지금 게임 그림 그대로."""
import os
from PIL import Image, ImageDraw, ImageFont
here = os.path.dirname(os.path.abspath(__file__)); Y = 763
base = Image.open(os.path.join(here, 'base_neon.png')).convert('RGB'); sheet = Image.open(os.path.join(here, 'sheet.png')).convert('RGB').resize(base.size, Image.LANCZOS)
out = base.copy(); out.paste(sheet.crop((0, Y, base.width, base.height)), (0, Y))
os.makedirs(os.path.join(here, 'out'), exist_ok=True); out.save(os.path.join(here, 'out', 'lm_bg_neon.png'))
# 미리보기: 위 = 지금 · 아래 = 새 물 (+ 원본 배경 물 참고)
JUA = os.path.join(here, '../../../../../Unity_Making/NKK_Project/Assets/Fonts/Jua-Regular.ttf'); F = ImageFont.truetype(JUA, 40)
orig = Image.open(os.path.join(here, 'ref_original.png')).convert('RGB')
crop = (0, 560, 1536, 1024); h = crop[3] - crop[1]
pv = Image.new('RGB', (1536, h * 3 + 30), (246, 236, 217)); d = ImageDraw.Draw(pv)
for i, (im, t) in enumerate([(base, '지금 네온 배경'), (out, '새 물 (네온이 비침)'), (orig, '참고: 원본 배경 물')]):
    pv.paste(im.crop(crop), (0, i * (h + 15))); d.rectangle((10, i * (h + 15) + 10, 30 + d.textlength(t, font=F), i * (h + 15) + 62), fill=(30, 20, 50)); d.text((20, i * (h + 15) + 12), t, font=F, fill=(255, 255, 255))
pv.save('C:/Project/NKK3/preview/alley_neon_water.png'); print('ok')
