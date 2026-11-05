"""story_06 손 고침 (2026-11-05 사용자)
red = 왼쪽 철창 무는 쥐: 남는 손 · 팔 삭제 → 왼쪽 철창 1 + 무는 철창 1 (최종 sheet = 원본에서 사용자가 빨간 선으로 표시한 손 2개만 지움 · prompt6 / user_mark.png. try1~4 = 앞선 시도)
blue = 오른쪽 검은 쥐 머리 옆 손 삭제 + 흰 쥐 옆 주인 없는 주먹 팔 삭제 (sheet_try1 · sheet_try2 → sheet = 3차)
Codex 가 다시 그린 크롭에서 고친 부분만 부드러운 테두리로 원본에 붙임 → out/story_06.png"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont
here = os.path.dirname(os.path.abspath(__file__))
FIX = {'red': ((170, 220, 490, 540), (390, 220, 640, 800)), 'blue': ((1060, 340, 1380, 660), (390, 140, 800, 600))}   # 크롭 (원본 좌표) · 붙일 타원 (크롭 1024 좌표)
base = Image.open(os.path.join(here, 'base.png')).convert('RGB'); out = base.copy()
for k, (B, E) in FIX.items():
    crop = Image.open(os.path.join(here, k, 'crop.png')).convert('RGB'); new = Image.open(os.path.join(here, k, 'sheet.png')).convert('RGB').resize(crop.size, Image.LANCZOS)
    m = Image.new('L', crop.size, 0); ImageDraw.Draw(m).ellipse(E, fill=255); m = m.filter(ImageFilter.GaussianBlur(30))
    out.paste(Image.composite(new, crop, m).resize((B[2] - B[0], B[3] - B[1]), Image.LANCZOS), B[:2])
os.makedirs(os.path.join(here, 'out'), exist_ok=True); out.save(os.path.join(here, 'out', 'story_06.png'))
JUA = os.path.join(here, '../../../../Unity_Making/NKK_Project/Assets/Fonts/Jua-Regular.ttf'); F = ImageFont.truetype(JUA, 34)
w = 760; h = round(w * 1024 / 1536)
pv = Image.new('RGB', (w * 2 + 30, h + 420 + 110), (246, 236, 217)); d = ImageDraw.Draw(pv)
for i, (im, t) in enumerate([(base, '지금'), (out, '고침')]):
    pv.paste(im.resize((w, h), Image.LANCZOS), (10 + i * (w + 10), 50)); d.text((14 + i * (w + 10), 8), t, font=F, fill=(91, 58, 34))
x = 10
for k, lab in (('red', '빨간 원'), ('blue', '파란 원')):
    B = FIX[k][0]
    for im in (base, out): pv.paste(im.crop(B).resize((370, 370), Image.LANCZOS), (x, h + 110)); x += 380
    d.text((x - 750, h + 70), lab + ': 지금 | 고침', font=F, fill=(91, 58, 34))
pv.save('C:/Project/NKK3/preview/story06_hands.png'); print('ok')
