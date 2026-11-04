"""마녀 생쥐 조립 · 날기 미리보기 (2026-11-04)
파츠 = ../<이름>.png (witch_parts.png 를 칸별로 키잉 · 자름). 자리 = 완성 그림(whole.png, 318×320) 좌표에 맞춤.
사용: python compose_witch.py check   → _조립_비교.png (완성 그림 | 조립)
      python compose_witch.py gif     → ../_미리보기_날기.gif (아지트 배경 위를 날아감)"""
import math, os, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
P = {n: Image.open(os.path.join(HERE, '..', f'{n}.png')).convert('RGBA') for n in ['whole', 'head', 'hat', 'body', 'arm', 'broom', 'cape', 'tail']}

# 파츠: (크기 배율, 피벗(이미지 좌상단 0~1), 완성 그림에서 피벗 자리, 기본 각도)
L = {
    'tail':  (0.47, (0.02, 0.55), (198, 208), 0),
    'cape':  (0.50, (0.04, 0.12), (150, 156), 0),
    'broom': (1.00, (0.50, 0.50), (156, 254), -4),
    'body':  (0.66, (0.30, 0.04), (112, 160), 0),
    'head':  (0.74, (0.72, 0.94), (160, 170), 0),
    'hat':   (0.675, (0.45, 0.80), (126, 84), -20),
    'arm':   (0.60, (0.70, 0.08), (114, 184), -22),
}
ORDER = ['tail', 'cape', 'broom', 'body', 'head', 'hat', 'arm']


def put(canvas, img, piv, at, ang=0.0, s=1.0):
    if s != 1.0: img = img.resize((max(1, int(img.width * s)), max(1, int(img.height * s))), Image.LANCZOS)
    px, py = piv[0] * img.width, piv[1] * img.height
    R = int(math.hypot(img.width, img.height)) + 4
    big = Image.new('RGBA', (2 * R, 2 * R)); big.alpha_composite(img, (int(R - px), int(R - py)))
    if ang: big = big.rotate(ang, resample=Image.BICUBIC)
    canvas.alpha_composite(big, (int(at[0] - R), int(at[1] - R)))


def witch(t=0.0, scale=1.0, size=(420, 420), off=(50, 50)):
    """t = 0~1 한 바퀴 (펄럭임). 반환 = 완성 그림 좌표 × scale"""
    W, H = int(size[0] * scale), int(size[1] * scale)
    c = Image.new('RGBA', (W, H))
    wave = math.sin(t * 2 * math.pi)
    rot = {'tail': 10 * wave, 'cape': 7 * math.sin(t * 2 * math.pi * 2), 'hat': 3 * math.sin(t * 2 * math.pi + 1.2),
           'head': 2 * math.sin(t * 2 * math.pi + 0.6), 'arm': 0, 'body': 0, 'broom': 0}
    for n in ORDER:
        s, piv, at, ang = L[n]
        put(c, P[n], piv, ((at[0] + off[0]) * scale, (at[1] + off[1]) * scale), ang + rot[n], s * scale)
    return c


if __name__ == '__main__':
    mode = sys.argv[1] if len(sys.argv) > 1 else 'check'
    if mode == 'check':
        a = Image.new('RGBA', (420, 420), (250, 250, 250, 255)); a.alpha_composite(P['whole'], (50, 50))
        b = Image.new('RGBA', (420, 420), (250, 250, 250, 255)); b.alpha_composite(witch(0.0))
        o = Image.new('RGBA', (420, 420), (250, 250, 250, 255)); o.alpha_composite(P['whole'], (50, 50))
        w = witch(0.0); w.putalpha(w.getchannel('A').point(lambda v: v * 0.55)); o.alpha_composite(w)
        s = Image.new('RGBA', (1290, 420), (255, 255, 255, 255))
        for i, im in enumerate([a, b, o]): s.alpha_composite(im, (i * 435, 0))
        s.save(os.path.join(HERE, '_조립_비교.png'))
    else:
        bg = Image.open(os.path.join(HERE, 'ref_lobby_bg.png')).convert('RGBA').resize((768, 512), Image.LANCZOS)
        frames = []
        N = 48
        for i in range(N):
            k = i / N
            f = bg.copy()
            w = witch((k * 4) % 1, 0.42)
            x = int(720 - k * 900)                       # 오른쪽 → 왼쪽
            y = int(150 + 18 * math.sin(k * 2 * math.pi * 2))   # 위아래 둥실
            tilt = 4 * math.cos(k * 2 * math.pi * 2)
            w = w.rotate(tilt, resample=Image.BICUBIC)
            f.alpha_composite(w, (x, y))
            frames.append(f.convert('P', palette=Image.ADAPTIVE, colors=255))
        frames[0].save(os.path.join(HERE, '..', '_미리보기_날기.gif'), save_all=True, append_images=frames[1:], duration=60, loop=0, disposal=2)
        # 제자리 확대판 (펄럭임 확인)
        fr = []
        for i in range(24):
            k = i / 24
            f = Image.new('RGBA', (420, 420), (245, 240, 230, 255))
            w = witch(k).rotate(3 * math.sin(k * 2 * math.pi), resample=Image.BICUBIC)
            f.alpha_composite(w, (0, int(10 * math.sin(k * 2 * math.pi))))
            fr.append(f.convert('P', palette=Image.ADAPTIVE, colors=255))
        fr[0].save(os.path.join(HERE, '..', '_미리보기_펄럭임.gif'), save_all=True, append_images=fr[1:], duration=70, loop=0)
    print('ok')
