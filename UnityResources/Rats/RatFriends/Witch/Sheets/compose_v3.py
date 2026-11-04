"""마녀 쥐 v3 (인게임 쥐와 같은 5파츠 + 빗자루) — 빗자루 탄 자세 조립 · 인게임 모습 미리보기 (2026-11-04)
관절 = 마법사 쥐와 같음 (UnityResources/Rats/Parts/pivots.json wizard: pivot · torsoAnchor). 파츠 크기 = 마법사 쥐와 같은 배율 (../*.png).
사용: python compose_v3.py check  → _v3_조립_비교.png (완성 그림 | 조립)
      python compose_v3.py game   → ../_미리보기_인게임.gif (아지트 화면 크기 · 자리 그대로: 둥둥 → 클릭 → 타원 한 바퀴 + 마법가루)"""
import json, math, os, random, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..', '..'))
W = os.path.join(HERE, '..')
P = {n: Image.open(os.path.join(W, f'{n}.png')).convert('RGBA') for n in ['whole', 'head', 'torso', 'front', 'back', 'tail', 'broom']}
SP = json.load(open(os.path.join(ROOT, 'UnityResources', 'Rats', 'Parts', 'pivots.json'), encoding='utf-8'))['species']['wizard']
PIV, ANC = SP['pivot'], SP['torsoAnchor']

# 빗자루 탄 자세 (몸통 기준 각도 · 빗자루 자리)
POSE = dict(body=4, head=2, front=-28, back=-8, tail=-6, broom_dx=-0.28, broom_dy=0.9, broom_ang=-3, broom_s=1.7)


def put(c, img, piv, at, ang=0.0, s=1.0):
    if s != 1.0: img = img.resize((max(1, int(img.width * s)), max(1, int(img.height * s))), Image.LANCZOS)
    px, py = piv[0] * img.width, piv[1] * img.height
    R = int(math.hypot(img.width, img.height)) + 4
    big = Image.new('RGBA', (2 * R, 2 * R)); big.alpha_composite(img, (int(R - px), int(R - py)))
    if ang: big = big.rotate(ang, resample=Image.BICUBIC)
    c.alpha_composite(big, (int(at[0] - R), int(at[1] - R)))


def rider(t=0.0, flying=False):
    """빗자루 탄 마녀 쥐 (파츠 크기 그대로, 왼쪽 봄). t = 흔들림 위상 (초)"""
    c = Image.new('RGBA', (1400, 1000)); tor = P['torso']
    tx, ty = 450, 330
    sp = 3.0 if flying else 1.2
    w = t * sp * 2 * math.pi
    A = {k: (tx + v[0] * tor.width, ty + v[1] * tor.height) for k, v in ANC.items()}
    # 빗자루 (몸통 아래)
    bx, by = tx + tor.width * (0.5 + POSE['broom_dx']), ty + tor.height * POSE['broom_dy']
    put(c, P['tail'], PIV['tail'], A['tail'], POSE['tail'] + 10 * math.sin(w))
    put(c, P['back'], PIV['back'], A['hip'], POSE['back'] + 6 * math.sin(w + 1.0))
    c.alpha_composite(tor, (tx, ty))
    put(c, P['broom'], (0.5, 0.5), (bx, by), POSE['broom_ang'], POSE['broom_s'])
    put(c, P['front'], PIV['front'], A['shoulder'], POSE['front'] + 2 * math.sin(w + 2.0))
    put(c, P['head'], PIV['head'], A['neck'], POSE['head'] + 3 * math.sin(w + 0.6))
    c = c.rotate(POSE['body'], resample=Image.BICUBIC, center=(tx + tor.width / 2, ty + tor.height / 2))
    return c


def bbox_fixed():
    c = rider(0)
    return c.getbbox()


if __name__ == '__main__':
    mode = sys.argv[1] if len(sys.argv) > 1 else 'check'
    BB = bbox_fixed()
    if mode == 'check':
        a = P['whole'].resize((int(P['whole'].width * 1.6), int(P['whole'].height * 1.6)), Image.LANCZOS)
        b = rider(0).crop(BB)
        s = Image.new('RGBA', (a.width + b.width + 60, max(a.height, b.height) + 40), (236, 232, 224, 255))
        s.alpha_composite(a, (20, 20)); s.alpha_composite(b, (a.width + 40, 20))
        s.save(os.path.join(HERE, '_v3_조립_비교.png')); print('ok', b.size)
    else:
        # 아지트 화면 = 빈 배경 + 소품 (게임과 같은 1536×1024, 100px = 1 단위)
        SPL = os.path.join(ROOT, 'UnityResources', 'Rats', 'Lobby_Split', 'base', 'out')
        bg = Image.open(os.path.join(SPL, 'base_empty.png')).convert('RGBA')
        for n in ['bed', 'table', 'books', 'shelf']:
            p = Image.open(os.path.join(SPL, f'base_{n}.png')).convert('RGBA')
            r = json.load(open(os.path.join(SPL, 'rects.json')))['rects'][n]; bg.alpha_composite(p, (r[0], r[1]))
        tw = Image.open(os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'FX', 'Tint', 'twinkle.png')).convert('RGBA')
        st = Image.open(os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'FX', 'Tint', 'star.png')).convert('RGBA')
        COLS = [(255, 235, 140), (217, 191, 255), (255, 191, 230), (255, 255, 255)]
        WIDTH_U = 3.0                         # 게임에서 마녀 폭 (단위) — 지금 마녀(v1)와 비슷하게
        HOME = (0.66 * 1536, 0.31 * 1024)     # LobbyHome P(0.66, 0.31)
        RX, RY, LOOP = 4.0 * 100, 0.85 * 100, 3.6
        FPS = 20; IDLE1, IDLE2 = 1.6, 1.4
        frames = []; dust = []; rnd = random.Random(3)
        face_cur = 1.0
        total = IDLE1 + LOOP + IDLE2
        for i in range(int(total * FPS)):
            t = i / FPS
            f = bg.copy()
            bob = 12 * math.sin(t * 0.8 * 2 * math.pi)
            tilt = 3 * math.sin(t * 0.8 * 2 * math.pi)
            flying = IDLE1 <= t < IDLE1 + LOOP
            ox = oy = 0; vx = vy = 0; face = 1
            if flying:
                k = (t - IDLE1) / LOOP
                th = (k * k * (3 - 2 * k)) * 2 * math.pi
                k2 = min(1, k + 0.01); th2 = (k2 * k2 * (3 - 2 * k2)) * 2 * math.pi
                ox, oy = -RX + RX * math.cos(th), -RY * math.sin(th)
                vx = ((-RX + RX * math.cos(th2)) - ox) / (LOOP * 0.01); vy = (-RY * math.sin(th2) - oy) / (LOOP * 0.01)
                face = 1 if vx < -20 else (-1 if vx > 20 else face_cur and (1 if face_cur > 0 else -1))
                tilt = max(-14, min(14, -vy * 0.06)) * face + 4
                bob *= 0.4
            face_cur += max(-1, min(1, (face - face_cur))) * min(1, 2 / (0.3 * FPS))
            w = rider(t, flying).crop(BB)
            sc = WIDTH_U * 100 / w.width
            w = w.resize((int(w.width * sc), int(w.height * sc)), Image.LANCZOS)
            fx = face_cur if abs(face_cur) > 0.06 else 0.06 * (1 if face > 0 else -1)
            if fx < 0: w = w.transpose(Image.FLIP_LEFT_RIGHT)
            w = w.resize((max(2, int(w.width * abs(fx))), w.height), Image.LANCZOS)
            w = w.rotate(tilt * (1 if fx > 0 else -1), resample=Image.BICUBIC, expand=True)
            cx, cy = HOME[0] + ox, HOME[1] + oy + bob
            # 마법가루: 빗자루 끝 (보는 반대쪽 아래)
            ex, ey = cx + (0.42 * WIDTH_U * 100) * (1 if fx > 0 else -1), cy + 0.25 * WIDTH_U * 100
            for _ in range(int(34 / FPS + rnd.random()) if flying else (1 if rnd.random() < 2 / FPS else 0)):
                dust.append([ex + rnd.uniform(-12, 12), ey + rnd.uniform(-12, 12), rnd.uniform(-50, 50) - vx * 0.15, rnd.uniform(15, 45), 0.0, rnd.uniform(0.9, 1.5), rnd.uniform(16, 32), rnd.choice([tw, st]), rnd.choice(COLS), rnd.uniform(0, 360)])
            keep = []
            for d in dust:
                d[4] += 1 / FPS
                k = d[4] / d[5]
                if k >= 1: continue
                d[3] += 160 / FPS; d[2] *= 1 - 1.5 / FPS
                d[0] += d[2] / FPS; d[1] += d[3] / FPS; d[9] += 120 / FPS
                size = d[6] * (k / 0.15 if k < 0.15 else 1 - (k - 0.15) / 0.85 * 0.6)
                a = 1 if k < 0.6 else 1 - (k - 0.6) / 0.4
                spr = d[7].resize((max(1, int(size)), max(1, int(size))), Image.LANCZOS).rotate(d[9], expand=True)
                col = Image.new('RGBA', spr.size, d[8] + (0,)); col.putalpha(spr.getchannel('A').point(lambda v: int(v * a)))
                tint = Image.composite(Image.new('RGBA', spr.size, d[8] + (255,)), spr, spr.getchannel('A'))
                tint.putalpha(col.getchannel('A'))
                f.alpha_composite(tint, (int(d[0] - spr.width / 2), int(d[1] - spr.height / 2)))
                keep.append(d)
            dust = keep
            f.alpha_composite(w, (int(cx - w.width / 2), int(cy - w.height / 2)))
            if t < IDLE1:   # 클릭 표시
                if t > IDLE1 - 0.35:
                    dr = ImageDraw.Draw(f); r = 10 + 60 * (t - (IDLE1 - 0.35)) / 0.35
                    dr.ellipse((cx - r, cy - r, cx + r, cy + r), outline=(255, 255, 255, 200), width=4)
            frames.append(f.resize((960, 640), Image.LANCZOS).convert('P', palette=Image.ADAPTIVE, colors=255))
        frames[0].save(os.path.join(W, '_미리보기_인게임.gif'), save_all=True, append_images=frames[1:], duration=int(1000 / FPS), loop=0)
        print('frames', len(frames))
