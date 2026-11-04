import sys, math
from PIL import Image
P = {n: Image.open(f'{n}.png').convert('RGBA') for n in ['head','torso','wing','wing_far','wing_fold','feet']}
# 피벗 (이미지 좌상단 기준 0~1)
PIV = {'head': (0.67, 0.93), 'wing': (0.05, 0.86), 'wing_far': (0.07, 0.86), 'wing_fold': (0.52, 0.05), 'feet': (0.5, 0.02)}
# 몸통 부착점
ANC = {'neck': (0.30, 0.14), 'shoulder': (0.58, 0.16), 'hip': (0.62, 0.90)}
HS, WS = 0.62, 1.3

def put(canvas, img, piv, at, ang=0, sx=1.0):
    w, h = img.size
    if sx != 1.0: img = img.resize((max(1,int(w*sx)), max(1,int(h*sx))), Image.LANCZOS); w, h = img.size
    px, py = piv[0]*w, piv[1]*h
    # 피벗을 가운데로 오게 큰 판에 붙이고 회전
    R = int(math.hypot(w, h)) + 4
    big = Image.new('RGBA', (2*R, 2*R)); big.alpha_composite(img, (int(R-px), int(R-py)))
    big = big.rotate(ang, resample=Image.BICUBIC)
    canvas.alpha_composite(big, (int(at[0]-R), int(at[1]-R)))

def bat(mode, flap=0.0, size=1500):
    c = Image.new('RGBA', (size, size)); t = P['torso']
    tx, ty = size//2 - t.width//2, size//2 - t.height//2 + 40
    A = {k: (tx+v[0]*t.width, ty+v[1]*t.height) for k, v in ANC.items()}
    if mode == 'fly':
        a = 20 - 95*flap                  # flap 0 = 날개 위, 1 = 아래
        put(c, P['wing_far'], PIV['wing_far'], (A['shoulder'][0]+25, A['shoulder'][1]-10), a-10, WS)
        put(c, P['feet'], PIV['feet'], A['hip'], 35, 0.9)
        c.alpha_composite(t, (tx, ty))
        put(c, P['wing'], PIV['wing'], A['shoulder'], a, WS)
        put(c, P['head'], PIV['head'], A['neck'], -5, HS)
    else:                                  # 매달림: 바로 선 모습으로 조립 → 위아래 뒤집기
        put(c, P['feet'], PIV['feet'], A['hip'], 0)
        c.alpha_composite(t, (tx, ty))
        put(c, P['head'], PIV['head'], A['neck'], 0, HS)
        put(c, P['wing_fold'], PIV['wing_fold'], (A['shoulder'][0]-10, A['shoulder'][1]+10), -12, 0.95)
        c = c.transpose(Image.FLIP_TOP_BOTTOM)
    return c.crop(c.getbbox())

out = sys.argv[1]
for n, im in [('hang', bat('hang')), ('fly_up', bat('fly', 0)), ('fly_mid', bat('fly', 0.5)), ('fly_down', bat('fly', 1))]:
    im.save(f'{out}/pose_{n}.png'); print(n, im.size)
