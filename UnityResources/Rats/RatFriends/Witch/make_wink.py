# 마녀 생쥐 윙크 그림 (2026-11-09 사용자: 클릭 5% 윙크 클로즈업)
# 머리 그림(head.png) 픽셀 크기 그대로 (1px = 머리 1px). 피벗 = 그림 가운데 (RatSpriteImporter 기본)
#  wink_eye.png  : 가운데 = 눈. 열린 눈을 머리 색으로 덮고 '>' 감은 눈 + 볼터치
#  wink_spark.png: 가운데 = 도는 중심. 노란 별 두 개
# 웹 미리보기 preview/witch_click/index.src.html 의 눈꺼풀과 같은 모양 · 크기
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.normpath(os.path.join(HERE, '../../../../Unity_Making/NKK_Project/Assets/Art/Rats/RatFriends/Witch'))
SS = 8
HEAD = (209, 208, 208, 255); INK = (30, 26, 28, 255); BLUSH = (244, 163, 184, 191)

def quad(p0, c, p1, n=24):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * c[0] + t * t * p1[0], (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * c[1] + t * t * p1[1]) for t in (i / n for i in range(n + 1))]

# ── 감은 눈 ──
N = 84; C = N / 2
im = Image.new('RGBA', (N * SS, N * SS), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
P = lambda x, y: ((C + x) * SS, (C + y) * SS)
cover = Image.new('L', im.size, 0); ImageDraw.Draw(cover).ellipse([P(-14, -15), P(14, 15)], fill=255)
from PIL import ImageFilter
cover = cover.filter(ImageFilter.GaussianBlur(2.2 * SS))                                # 가장자리를 부드럽게 (머리 결과 섞이게)
core = Image.new('L', im.size, 0); ImageDraw.Draw(core).ellipse([P(-11.5, -12.5), P(11.5, 12.5)], fill=255)
from PIL import ImageChops
cover = ImageChops.lighter(cover, core)                                                 # 열린 눈 (반지름 10.5) 은 꽉 덮음
headl = Image.new('RGBA', im.size, HEAD); headl.putalpha(cover); im.alpha_composite(headl)   # 열린 눈 덮기
blush = Image.new('RGBA', im.size, (0, 0, 0, 0)); ImageDraw.Draw(blush).ellipse([P(-21, 18), P(9, 34)], fill=BLUSH)
im.alpha_composite(blush)
line = quad((-9, -9), (6, -1), (10, 0)) + quad((10, 0), (6, 1), (-9, 9))[1:]
w = int(5.5 * SS)
d.line([P(x, y) for x, y in line], fill=INK, width=w, joint='curve')
for x, y in (line[0], line[-1], (10, 0)):
    px, py = P(x, y); d.ellipse([px - w / 2, py - w / 2, px + w / 2, py + w / 2], fill=INK)
im.resize((N, N), Image.LANCZOS).save(os.path.join(GAME, 'wink_eye.png'))

# ── 반짝이 (별 두 개) ──
N = 64; C = N / 2
im = Image.new('RGBA', (N * SS, N * SS), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
def star(cx, cy, r, col):
    k = r / 18
    pts = [(0, -18), (5, -5), (18, 0), (5, 5), (0, 18), (-5, 5), (-18, 0), (-5, -5)]
    d.polygon([((C + cx + x * k) * SS, (C + cy + y * k) * SS) for x, y in pts], fill=col)
star(-10, -14, 18, (255, 214, 90, 255))
star(10, 14, 10, (255, 244, 194, 255))
im.resize((N, N), Image.LANCZOS).save(os.path.join(GAME, 'wink_spark.png'))
for f in ('wink_eye.png', 'wink_spark.png'):
    Image.open(os.path.join(GAME, f)).save(os.path.join(HERE, f))
print('ok', GAME)
