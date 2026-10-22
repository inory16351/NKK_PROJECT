"""전용 파츠 보스 리그 → GIF (반응 보기용, 2026-10-22). rig_web/giant.html 과 같은 계산 (Unity BossPartRig 와 같음)을 PIL 로 그대로 조립.
그림은 파츠 PNG 를 붙이기만 함 (새로 그리지 않음). 장면: 이동 → 기관총 사격(공격) → 내려찍기 점프 (보스 값: 걸음 95 · jumpV 900 · 중력 1800)
사용: python Tools/rig_gif.py <리그.json> <출력.gif> [파츠폴더]   예) rig_web/boss_cartgiant_rat.json → 차력거인 쥐 얼굴"""
import json, math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

J = sys.argv[1]; OUT = sys.argv[2]
cfg = json.load(open(J, encoding='utf-8'))
DIR = sys.argv[3] if len(sys.argv) > 3 else os.path.join(os.path.dirname(J), cfg.get('dir', 'parts/'))
PARTS = cfg['parts']
IDLE_WALK, RECOIL_RATE, RECOIL_TILT, FAR = 0.15, 12, 4, 0.77
W, H, FPS = 640, 420, 15
BG = (74, 122, 72, 255)
img = {}
for p in PARTS:
    for k in ('sprite', 'alt'):
        n = p.get(k)
        if n and n not in img: img[n] = Image.open(os.path.join(DIR, n + '.png')).convert('RGBA')


def M(a, b, c, d, e, f): return np.array([[a, c, e], [b, d, f], [0, 0, 1]], float)          # DOMMatrix(a,b,c,d,e,f)
def T(x, y): return M(1, 0, 0, 1, x, y)
def S(sx, sy=None): return M(sx, 0, 0, sx if sy is None else sy, 0, 0)
def R(deg): r = math.radians(deg); return M(math.cos(r), math.sin(r), -math.sin(r), math.cos(r), 0, 0)


def render(mode, t, walk, stop_t, base):
    world, lst = {}, []
    for p in PARTS:
        sp = img.get(p['sprite'])
        if sp is None: continue
        par = world.get(p['parent']) if p['parent'] else None
        a, dx, dy, use = p['angle'], 0, 0, p['sprite']
        ph = p['phase'] * math.pi * 2; mv = p['move']
        if mv == 'Walk': a += p['amp'] * math.sin(walk * p['speed'] + ph) * (1 if mode == 'walk' else IDLE_WALK)
        elif mv == 'Bob': dy = p['amp'] * math.sin(t * 3 * p['speed'] + ph)
        elif mv == 'Sway': a += p['amp'] * math.sin(t * 3 * p['speed'] + ph)
        elif mv == 'Wiggle':
            e, D = t - stop_t, 1 / p['speed']
            if mode == 'attack': a += p['amp'] * (1 - ((t * RECOIL_RATE + p['phase']) % 1))
            elif mode != 'walk' and 0 <= e < D: a += p['amp'] * math.sin(math.pi * 2 * e / D) * (1 - e / D)
        elif mv == 'Recoil':
            if mode == 'attack':
                f = (t * RECOIL_RATE * p['speed'] + p['phase']) % 1
                if f < 0.5 and p.get('alt'): use = p['alt']
                dx = p['amp'] * (1 - f); a += random.uniform(-1, 1) * RECOIL_TILT * (1 - f)
            else: dy = p['amp'] / 3 * math.sin(t * 3 * p['speed'] + ph)
        if mode == 'attack' and p.get('alt') and mv != 'Recoil': use = p['alt']
        if par:
            ps = img[par['p']['sprite']]
            pos = [(p['at'][0] - par['p']['pivot'][0]) * ps.width, (par['p']['pivot'][1] - p['at'][1]) * ps.height]
        else: pos = [p['at'][0] * sp.width, p['at'][1] * sp.height]
        pos = [pos[0] + dx, pos[1] + dy]
        m = (par['m'] if par else base) @ T(pos[0], -pos[1]) @ R(-a) @ S(p['scale'])
        world[p['name']] = {'m': m, 'p': p}
        lst.append((p, m, img.get(use) or sp))
    lst.sort(key=lambda o: o[0]['order'])
    layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for p, m, im in lst:
        pv = p['pivot']
        mm = m @ T(-pv[0] * im.width, -pv[1] * im.height)
        inv = np.linalg.inv(mm)
        src = im
        if p.get('far'):
            arr = np.array(im).astype(np.float32); arr[..., :3] *= FAR; src = Image.fromarray(arr.clip(0, 255).astype(np.uint8))
        t_img = src.transform((W, H), Image.AFFINE, (inv[0, 0], inv[0, 1], inv[0, 2], inv[1, 0], inv[1, 1], inv[1, 2]), resample=Image.BICUBIC)
        layer.alpha_composite(t_img)
    return layer


# 리그 픽셀 → 게임 단위: 한 번 그려서 키를 잼 (BossPartArt height 360)
Z = 0.23
probe = render('idle', 0, 0, -9, T(W / 2, H * 0.9) @ S(Z))
ys = np.nonzero(np.array(probe)[..., 3] > 20)[0]
px_per_unit = (ys.max() - ys.min()) / 360.0
GROUND = int(H * 0.9)
frames, t = [], 0.0
x = W * 0.72
stop_t = -9


def shot(mode, x, z=0, sq=1.0, jit=0):
    base = T(x + jit, GROUND - z) @ S(Z / math.sqrt(sq), Z * sq)
    f = Image.new('RGBA', (W, H), BG)
    d = ImageDraw.Draw(f)
    d.rectangle((0, GROUND, W, GROUND + 2), fill=(0, 0, 0, 38))
    sh = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    sw = 120 * (1 - min(0.6, z / 400))
    ImageDraw.Draw(sh).ellipse((x - sw, GROUND - sw * 0.18, x + sw, GROUND + sw * 0.18), fill=(0, 0, 0, 60))
    f.alpha_composite(sh.filter(ImageFilter.GaussianBlur(6)))
    f.alpha_composite(render(mode, t, walk, stop_t, base))
    frames.append(f.convert('RGB'))


dt = 1 / FPS
walk = 0.0
speed = 95 * px_per_unit
# ① 이동 (왼쪽으로, 3초)
for i in range(int(3 * FPS)):
    t += dt; walk += dt * 95 / 16; x -= speed * dt; shot('walk', x)
stop_t = t
for i in range(int(0.5 * FPS)): t += dt; shot('idle', x)
# ② 공격: 기관총 사격 (2.5초) — 화난 얼굴 + 포탑 반동
for i in range(int(2.5 * FPS)): t += dt; shot('attack', x)
for i in range(int(0.5 * FPS)): t += dt; shot('idle', x)
# ③ 점프 (내려찍기): 웅크림 덜덜 0.6초 → 왼쪽(보는 쪽)으로 점프 (1초, 높이 225) → 쿵 (납작) → 0.6초
for i in range(int(0.6 * FPS)): t += dt; shot('attack', x, sq=0.92, jit=random.uniform(-3, 3))
x0, x1, TJ = x, x - 170, 2 * 900 / 1800
for i in range(int(TJ * FPS)):
    t += dt; k = (i + 1) / (TJ * FPS); z = (900 * k * TJ - 0.5 * 1800 * (k * TJ) ** 2) * px_per_unit
    shot('attack', x0 + (x1 - x0) * k, z=z)
x = x1; sq = 0.7
for i in range(int(0.8 * FPS)):
    t += dt; sq += (1 - sq) * min(1, dt * 8); shot('attack' if i < 6 else 'idle', x, sq=sq, jit=random.uniform(-6, 6) if i < 4 else 0)
frames[0].save(OUT, save_all=True, append_images=frames[1:], duration=int(1000 / FPS), loop=0, optimize=True)
print('GIF', OUT, len(frames), '프레임', f'{len(frames) / FPS:.1f}초')
