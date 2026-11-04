"""랫 댄스 쥐 v2 조립: 평소 = 가만히 서기, 춤 = 참고 자세(넓게 벌린 굽은 다리 · 옆으로 굽힌 팔 · 기울인 몸)를 좌우 번갈아 반복
s = +1 → 자세 A, s = -1 → 좌우 반대 자세. 춤은 0.5초마다 A ↔ B (부드럽게 넘어가며 박자마다 쿵)"""
import sys, math, os
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__)); D = os.path.dirname(here)
P = {n: Image.open(os.path.join(D, n + '.png')).convert('RGBA') for n in ['head', 'head_dance', 'body', 'arm', 'leg', 'tail']}
MIR = lambda im: im.transpose(Image.FLIP_LEFT_RIGHT)
P['arm_r'] = MIR(P['arm']); P['leg_r'] = MIR(P['leg'])
PIV = {'head': (0.58, 0.95), 'head_dance': (0.58, 0.95), 'body': (0.5, 0.5),
       'arm': (0.96, 0.20), 'arm_r': (0.04, 0.20), 'leg': (0.80, 0.04), 'leg_r': (0.20, 0.04), 'tail': (0.12, 0.96)}
NECK = (0.5, 0.07); SH_L, SH_R = (0.33, 0.27), (0.67, 0.27); HIP_L, HIP_R = (0.30, 0.86), (0.70, 0.86); TAIL = (0.66, 0.84)

def rot(v, a):
    r = math.radians(a); return (v[0]*math.cos(r) + v[1]*math.sin(r), -v[0]*math.sin(r) + v[1]*math.cos(r))
def att(name, at, ang, anchor):
    im = P[name]; px, py = PIV[name]
    return tuple(a + b for a, b in zip(at, rot(((anchor[0]-px)*im.width, (anchor[1]-py)*im.height), ang)))
def put(c, name, at, ang):
    im = P[name]; px, py = PIV[name][0]*im.width, PIV[name][1]*im.height
    R = int(math.hypot(im.width, im.height)) + 4
    big = Image.new('RGBA', (2*R, 2*R)); big.alpha_composite(im, (int(R-px), int(R-py)))
    c.alpha_composite(big.rotate(ang, resample=Image.BICUBIC), (int(at[0]-R), int(at[1]-R)))

def rat(s=None, bounce=0.0, wig=0.0, size=(1000, 1000)):
    """s=None → 평소 서기. s ∈ [-1, 1] → 춤 자세 (A ↔ 좌우 반대). bounce 0~1 = 박자 쿵(몸 내려감), wig = 꼬리·손 흔들림"""
    c = Image.new('RGBA', size); dance = s is not None; k = s or 0
    squat = (60 if dance else 30) + 25*bounce
    center = (size[0]/2 + 30*k, 470 + squat)
    a_body = -9*k                                      # 몸 기울기 (A = 화면 오른쪽으로)
    neck = att('body', center, a_body, NECK)
    sh = {'L': att('body', center, a_body, SH_L), 'R': att('body', center, a_body, SH_R)}
    hip = {'L': att('body', center, a_body, HIP_L), 'R': att('body', center, a_body, HIP_R)}
    tailp = att('body', center, a_body, TAIL)
    if dance:
        # 팔: 기본은 옆으로 굽혀 들기, A 에선 왼팔이 더 높고 오른팔은 조금 낮음
        arm = {'L': -10 - 18*k + 6*wig, 'R': 10 - 18*k - 6*wig}
        # 다리: 넓게 벌려 굽힘, A 에선 왼다리를 더 멀리 뻗음
        leg = {'L': -16 - 14*max(k, 0) + 6*max(-k, 0), 'R': 16 + 14*max(-k, 0) - 6*max(k, 0)}
        a_head = 14*k + a_body*0.3
    else:
        arm = {'L': 38, 'R': -38}                      # 평소: 팔을 몸 옆으로 내림
        leg = {'L': 2, 'R': -2}                        # 평소: 다리 살짝 벌리고 섬
        a_head = 0
    put(c, 'tail', tailp, a_body - 10 + 18*wig)
    put(c, 'leg', hip['L'], a_body*0.3 + leg['L']); put(c, 'leg_r', hip['R'], a_body*0.3 + leg['R'])
    put(c, 'arm', sh['L'], a_body + arm['L']); put(c, 'arm_r', sh['R'], a_body + arm['R'])
    put(c, 'body', center, a_body)
    put(c, 'head_dance' if dance else 'head', neck, a_body + a_head)
    return c

def dance_at(t):
    """춤 t 초: 0.5초 박자마다 A ↔ B, 앞 0.15초 동안 넘어가고 나머지는 멈춤 + 쿵"""
    beat, f = divmod(t / 0.5, 1.0); side = 1 if int(beat) % 2 == 0 else -1
    if f < 0.3: e = f/0.3; e = e*e*(3-2*e); s = -side + (2*side)*e if beat > 0 else side*e
    else: s = side
    bounce = math.sin(min(1, f/0.6)*math.pi) if f > 0.15 else 0
    return rat(s, bounce, math.sin(t*2*math.pi*4))

if __name__ == '__main__':
    poses = [rat(), rat(1), rat(-1), dance_at(0.62)]
    box = None
    for p in poses:
        b = p.getbbox(); box = b if box is None else (min(box[0],b[0]), min(box[1],b[1]), max(box[2],b[2]), max(box[3],b[3]))
    box = (box[0]-20, box[1]-20, box[2]+20, box[3]+20)
    sheet = Image.new('RGBA', ((box[2]-box[0])*len(poses), box[3]-box[1]), (225, 214, 196, 255))
    for i, p in enumerate(poses): sheet.alpha_composite(p.crop(box), (i*(box[2]-box[0]), 0))
    sheet.save(os.path.join(D, '_조립_자세.png'))
    fr = []
    for t in [None]*25 + [i/25 for i in range(75)] + [None]*25:
        f = Image.new('RGBA', (box[2]-box[0], box[3]-box[1]), (225, 214, 196, 255))
        f.alpha_composite((rat() if t is None else dance_at(t)).crop(box)); f = f.resize((f.width//2, f.height//2), Image.LANCZOS)
        fr.append(f.convert('RGB').quantize(128))
    fr[0].save(os.path.join(D, '_미리보기_춤.gif'), save_all=True, append_images=fr[1:], duration=40, loop=0)
    print('ok', box)
