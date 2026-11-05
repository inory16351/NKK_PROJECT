"""story_02 가운데 흰 쥐 앞발 3개 → 2개 (Codex sheet.png = crop.png 다시 그림). 앞발 · 가슴 부분만 부드러운 테두리로 원본에 붙임 → out/story_02.png"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont
here = os.path.dirname(os.path.abspath(__file__)); B = (660, 440, 1060, 840)
base = Image.open(os.path.join(here, 'base.png')).convert('RGB')
crop = Image.open(os.path.join(here, 'crop.png')).convert('RGB'); new = Image.open(os.path.join(here, 'sheet.png')).convert('RGB').resize(crop.size, Image.LANCZOS)
mask = Image.new('L', crop.size, 0); ImageDraw.Draw(mask).ellipse((230, 420, 470, 660), fill=255); mask = mask.filter(ImageFilter.GaussianBlur(28))
fixed = Image.composite(new, crop, mask)
out = base.copy(); out.paste(fixed.resize((B[2] - B[0], B[3] - B[1]), Image.LANCZOS), B[:2])
os.makedirs(os.path.join(here, 'out'), exist_ok=True); out.save(os.path.join(here, 'out', 'story_02.png'))
# 미리보기: 전체 (지금 | 고침) + 쥐 확대
JUA = os.path.join(here, '../../../../Unity_Making/NKK_Project/Assets/Fonts/Jua-Regular.ttf'); F = ImageFont.truetype(JUA, 34)
w = 760; a, b = base.resize((w, round(w * 1024 / 1536))), out.resize((w, round(w * 1024 / 1536)))
z = (720, 520, 940, 800); za, zb = base.crop(z).resize((330, 420), Image.LANCZOS), out.crop(z).resize((330, 420), Image.LANCZOS)
pv = Image.new('RGB', (w * 2 + 30, a.height + za.height + 110), (246, 236, 217)); d = ImageDraw.Draw(pv)
for i, (im, t) in enumerate([(a, '지금 (앞발 3개)'), (b, '고침 (앞발 2개)')]):
    pv.paste(im, (10 + i * (w + 10), 50)); d.text((14 + i * (w + 10), 8), t, font=F, fill=(91, 58, 34))
pv.paste(za, (10, a.height + 80)); pv.paste(zb, (360, a.height + 80)); d.text((710, a.height + 90), '확대: 지금 | 고침', font=F, fill=(91, 58, 34))
pv.save('C:/Project/NKK3/preview/story02_paws.png'); print('ok')
