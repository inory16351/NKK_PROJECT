"""마녀 생쥐 동작 미리보기 (2026-11-04 사용자): 평소 = 공중 제자리에서 둥둥 · 클릭 = 앞(왼쪽)으로 날아갔다가 뒤돌아 제자리로 돌아옴
결과: ../_미리보기_클릭.gif"""
import math, os
from PIL import Image, ImageDraw
from compose_witch import witch, HERE

bg = Image.open(os.path.join(HERE, 'ref_lobby_bg.png')).convert('RGBA').resize((768, 512), Image.LANCZOS)
cursor = Image.open(os.path.join(HERE, '..', '..', '..', 'Cursor', 'cursor_normal_big.png')).convert('RGBA').resize((40, 40), Image.LANCZOS)
cursor_p = Image.open(os.path.join(HERE, '..', '..', '..', 'Cursor', 'cursor_pressed_big.png')).convert('RGBA').resize((40, 40), Image.LANCZOS)
HOME = (470, 130)      # 제자리 (그림 왼쪽 위)
DASH = 300             # 앞으로 가는 거리 (px)
FPS = 25
IDLE, CLICK, OUT, TURN, BACK, TURN2, IDLE2 = 1.6, 0.25, 0.9, 0.35, 1.1, 0.35, 1.2
T = [IDLE, CLICK, OUT, TURN, BACK, TURN2, IDLE2]
total = sum(T)
ease = lambda k: k * k * (3 - 2 * k)

frames = []
n = int(total * FPS)
for i in range(n):
    t = i / FPS
    f = bg.copy()
    bob = 10 * math.sin(t * 2 * math.pi * 0.8)
    face = 1          # 1 = 왼쪽 봄, -1 = 오른쪽 봄
    x = 0.0; tilt = 3 * math.sin(t * 2 * math.pi * 0.8); squash = 1.0; flap = (t * 1.2) % 1
    ph, acc = 0, 0.0
    for j, d in enumerate(T):
        if t < acc + d: ph = j; k = (t - acc) / d; break
        acc += d
    else: ph, k = 6, 1.0
    if ph == 2:                           # 앞으로 (왼쪽)
        x = -DASH * ease(k); tilt = 10; flap = (t * 3) % 1
    elif ph == 3:                         # 뒤돌기 (좌우로 납작해졌다 펼쳐짐)
        x = -DASH; squash = abs(math.cos(k * math.pi)); face = 1 if k < 0.5 else -1
    elif ph == 4:                         # 제자리로 (오른쪽)
        x = -DASH * (1 - ease(k)); face = -1; tilt = 10; flap = (t * 3) % 1
    elif ph == 5:
        squash = abs(math.cos(k * math.pi)); face = -1 if k < 0.5 else 1
    w = witch(flap, 0.42)
    w = w.crop(w.getbbox())
    if face < 0: w = w.transpose(Image.FLIP_LEFT_RIGHT)
    if squash < 0.999: w = w.resize((max(2, int(w.width * max(0.08, squash))), w.height), Image.LANCZOS)
    w = w.rotate(tilt * (1 if face > 0 else -1), resample=Image.BICUBIC, expand=True)
    cx = HOME[0] + 90 + x
    f.alpha_composite(w, (int(cx - w.width / 2), int(HOME[1] + bob)))
    # 마우스: 처음엔 쥐 위로 다가가 누름
    if ph <= 1:
        k0 = min(1.0, t / IDLE)
        mx, my = 700 - (700 - (HOME[0] + 95)) * ease(k0), 420 - (420 - (HOME[1] + 70)) * ease(k0)
        f.alpha_composite(cursor_p if ph == 1 else cursor, (int(mx), int(my)))
        if ph == 1:
            d = ImageDraw.Draw(f); r = 8 + 30 * k
            d.ellipse((mx - r, my - r, mx + r, my + r), outline=(255, 255, 255, int(220 * (1 - k))), width=3)
    frames.append(f.convert('P', palette=Image.ADAPTIVE, colors=255))
frames[0].save(os.path.join(HERE, '..', '_미리보기_클릭.gif'), save_all=True, append_images=frames[1:], duration=int(1000 / FPS), loop=0, disposal=2)
print(len(frames), 'frames')
