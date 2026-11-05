"""지금 게임 네온 버튼 | ui4 새 판 비교 → C:/Project/NKK3/preview/neon_ui_glow.png"""
import os
from PIL import Image, ImageDraw, ImageFont
here = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.join(here, '../../../../../Unity_Making/NKK_Project/Assets/Art/Rats/Deco/neon')
OLD = os.path.join(here, '_old_assets')      # 적용 전 게임 그림 (비교용)
JUA = os.path.join(here, '../../../../../Unity_Making/NKK_Project/Assets/Fonts/Jua-Regular.ttf')
F = lambda s: ImageFont.truetype(JUA, s)
B = {'ui_plank': (70, 46), 'ui_pill': (54, 49)}
def nine(im, size, b):
    if not b: return im.resize(size, Image.LANCZOS)
    bx, by = b; w, h = im.size; W, H = size; o = Image.new('RGBA', size)
    xs = [(0, bx, 0, bx), (bx, w - bx, bx, W - bx), (w - bx, w, W - bx, W)]; ys = [(0, by, 0, by), (by, h - by, by, H - by), (h - by, h, H - by, H)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 > dx0 and dy1 > dy0: o.alpha_composite(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return o
items = [('ui_plank', '출발하기', (400, 116)), ('ui_plank_green', '살래요', (331, 116)), ('ui_plank_red', '그만둘래요', (324, 116)), ('ui_plank_gray', '잠김', (330, 116)),
         ('ui_btn_round', 'X', None), ('ui_btn_round_green', '+', None), ('ui_pill', '도토리 120', (329, 109)), ('ui_key', 'W', None)]
def panel(src, title):
    bg = Image.open(os.path.join(GAME, 'lm_bg_neon.png')).convert('RGBA')
    bg = bg.resize((1500, round(bg.height * 1500 / bg.width)), Image.LANCZOS).crop((0, 120, 1500, 640))
    dim = Image.new('RGBA', bg.size, (10, 6, 25, 120)); bg.alpha_composite(dim)
    d = ImageDraw.Draw(bg); d.text((24, 14), title, font=F(40), fill=(255, 255, 255))
    x, y = 30, 80
    for i, (n, t, size) in enumerate(items):
        im = Image.open(os.path.join(src, n + '_neon.png')).convert('RGBA')
        if size: im = nine(im, size, B['ui_pill' if 'pill' in n else 'ui_plank'])
        if i == 4: x, y = 30, 260
        bg.alpha_composite(im, (x, y))
        f = F(46 if size else 70); tw = d.textlength(t, font=f)
        d.text((x + im.width / 2 - tw / 2, y + im.height / 2 - (30 if size else 44)), t, font=f, fill=(255, 255, 255) if 'gray' not in n else (170, 165, 190))
        x += im.width + 28
    return bg
a = panel(OLD, '바꾸기 전'); b = panel(os.path.join(here, 'out'), '새 판 (빛번짐, 연한 색)')
# 모서리 4배 확대 (계단 비교)
def zoom(src):
    im = Image.open(os.path.join(src, 'ui_plank_neon.png')).convert('RGBA').crop((0, 0, 110, 116)).resize((330, 348), Image.NEAREST)
    bgc = Image.new('RGBA', im.size, (30, 20, 50, 255)); bgc.alpha_composite(im); return bgc
z = Image.new('RGBA', (740, 420), (246, 236, 217, 255)); dz = ImageDraw.Draw(z)
dz.text((20, 12), '모서리 3배 확대: 지금 | 새 판', font=F(34), fill=(91, 58, 34))
z.alpha_composite(zoom(OLD), (20, 60)); z.alpha_composite(zoom(os.path.join(here, 'out')), (390, 60))
out = Image.new('RGBA', (1500 + 260, a.height * 2 + z.height + 30), (246, 236, 217, 255))
out.alpha_composite(a, (0, 0)); out.alpha_composite(b, (0, a.height + 10)); out.alpha_composite(z, (0, a.height * 2 + 20))
ref = Image.open(os.path.join(here, 'ref_style.png')).convert('RGBA'); ref = ref.resize((round(ref.width * 400 / ref.height), 400), Image.LANCZOS)
out.alpha_composite(ref, (780, a.height * 2 + 30)); ImageDraw.Draw(out).text((780 + ref.width + 20, a.height * 2 + 40), '참고 이미지', font=F(34), fill=(91, 58, 34))
out.convert('RGB').save('C:/Project/NKK3/preview/neon_ui_glow.png'); print(out.size)
