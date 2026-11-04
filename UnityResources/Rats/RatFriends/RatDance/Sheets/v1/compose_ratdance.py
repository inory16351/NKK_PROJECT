"""랫 댄스 쥐 조립 미리보기: 가만히 서기 + 3초 춤 (GIF) · 자세 시트"""
import sys, math, os
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__)); D = os.path.dirname(here)
P = {n: Image.open(os.path.join(D, n + '.png')).convert('RGBA') for n in ['head','head_dance','up','low','arm','arm_up','leg','tail']}
MIR = lambda im: im.transpose(Image.FLIP_LEFT_RIGHT)
P['arm_up_r'] = MIR(P['arm_up']); P['leg_r'] = MIR(P['leg'])
# 피벗 (좌상단 기준 0~1)
PIV = {'head': (0.5, 0.96), 'head_dance': (0.5, 0.96), 'up': (0.5, 0.97), 'low': (0.5, 0.08),
       'arm': (0.51, 0.04), 'arm_up': (0.93, 0.80), 'arm_up_r': (0.07, 0.80),
       'leg': (0.647, 0.03), 'leg_r': (0.353, 0.03), 'tail': (0.10, 0.03)}
# 부착점 (부모 그림 기준 0~1)
UP_NECK = (0.5, 0.12); UP_SH_L, UP_SH_R = (0.22, 0.28), (0.78, 0.28)
LOW_HIP_L, LOW_HIP_R, LOW_TAIL = (0.27, 0.80), (0.73, 0.80), (0.62, 0.55)

def rot(v, a):
    r = math.radians(a); return (v[0]*math.cos(r) + v[1]*math.sin(r), -v[0]*math.sin(r) + v[1]*math.cos(r))
def att(name, at, ang, anchor):
    """name 그림을 피벗 at, 각도 ang 으로 둘 때 anchor(0~1) 의 화면 위치"""
    im = P[name]; px, py = PIV[name]
    v = ((anchor[0]-px)*im.width, (anchor[1]-py)*im.height); v = rot(v, ang)
    return (at[0]+v[0], at[1]+v[1])
def put(c, name, at, ang):
    im = P[name]; px, py = PIV[name][0]*im.width, PIV[name][1]*im.height
    R = int(math.hypot(im.width, im.height)) + 4
    big = Image.new('RGBA', (2*R, 2*R)); big.alpha_composite(im, (int(R-px), int(R-py)))
    big = big.rotate(ang, resample=Image.BICUBIC)
    c.alpha_composite(big, (int(at[0]-R), int(at[1]-R)))

def rat(t=None, size=(900, 1300)):
    """t=None → 가만히 서기, t=초 → 춤 (한 박자 0.5초)"""
    c = Image.new('RGBA', size)
    dance = t is not None
    th = (t or 0) * 2*math.pi / 1.0           # 1초에 한 번 좌우 왕복
    s = math.sin(th) if dance else 0
    bob = abs(math.sin(th)) if dance else 0
    waist = (size[0]/2 + 40*s, 680 + 18*bob)  # 허리 (위·아래 몸 이음매)
    a_low = 10*s; a_up = -7*s; a_head = 12*math.sin(th + 0.6) if dance else 0
    low_top = waist
    hipL, hipR = att('low', low_top, a_low, LOW_HIP_L), att('low', low_top, a_low, LOW_HIP_R)
    tailp = att('low', low_top, a_low, LOW_TAIL)
    neck = att('up', waist, a_up, UP_NECK)
    shL, shR = att('up', waist, a_up, UP_SH_L), att('up', waist, a_up, UP_SH_R)
    beat = int((t or 0) / 0.5) % 2 if dance else -1
    # 그리기 순서: 꼬리 → 다리 → 아래 몸 → 팔(몸 뒤) → 위 몸 → 머리
    put(c, 'tail', tailp, a_low - (15*math.sin(th*2) if dance else 0))
    put(c, 'leg', hipL, -a_low*0.9 + (6*bob if dance else 0))
    put(c, 'leg_r', hipR, -a_low*0.9 - (6*bob if dance else 0))
    put(c, 'low', low_top, a_low)
    up_arms, down_arms = [], []
    for side, sh in (('L', shL), ('R', shR)):
        raised = dance and ((beat == 0) == (side == 'L'))
        (up_arms if raised else down_arms).append((side, sh))
    for side, sh in up_arms:
        wig = 14*math.sin(th*4)
        put(c, 'arm_up' if side == 'L' else 'arm_up_r', sh, a_up + (wig if side == 'L' else -wig))
    for side, sh in down_arms:          # 내린 팔은 몸 뒤 (어깨 단면이 몸에 가려짐)
        sw = (25*math.sin(th*2) if dance else 0) + (-6 if side == 'L' else 6) + a_up
        put(c, 'arm', (sh[0] + (8 if side == 'L' else -8), sh[1]), sw if side == 'L' else -sw)
    put(c, 'up', waist, a_up)
    put(c, 'head_dance' if dance else 'head', neck, a_up + a_head)
    return c

if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else D
    poses = [rat()] + [rat(t) for t in (0.0, 0.25, 0.5, 0.75)]
    box = None
    for p in poses:
        b = p.getbbox(); box = b if box is None else (min(box[0],b[0]), min(box[1],b[1]), max(box[2],b[2]), max(box[3],b[3]))
    poses = [p.crop(box) for p in poses]
    W = sum(p.width for p in poses) + 40*(len(poses)+1); sheet = Image.new('RGBA', (W, poses[0].height+80), (225, 214, 196, 255)); x = 40
    for p in poses: sheet.alpha_composite(p, (x, 40)); x += p.width + 40
    sheet.save(os.path.join(out, '_조립_자세.png'))
    # GIF: 가만히 1초 → 클릭 → 춤 3초 → 가만히 1초 (25fps)
    bg = (225, 214, 196); fr = []
    seq = [None]*25 + [i/25 for i in range(75)] + [None]*25
    for t in seq:
        f = Image.new('RGBA', (box[2]-box[0]+40, box[3]-box[1]+40), bg + (255,))
        f.alpha_composite(rat(t).crop(box), (20, 20)); f = f.resize((f.width//2, f.height//2), Image.LANCZOS)
        fr.append(f.convert('RGB').quantize(128))
    fr[0].save(os.path.join(out, '_미리보기_춤.gif'), save_all=True, append_images=fr[1:], duration=40, loop=0)
    print('ok', box)
