"""햄스터 (아지트 장식) 조립: 정면으로 앉아 두 손을 가슴 앞에 든 자세 (사용자 사진 자세)
그리는 순서: 꼬리 → 몸 → 발 → 팔 → 머리. 팔 그림 = 화면 오른쪽 팔 (어깨 단면 오른쪽 위), 왼쪽은 좌우 반전.
check → Sheets/_조립_비교.png (Codex 전체 자세와 나란히) · 아지트 → _미리보기_아지트.png · gif → _미리보기_움직임.gif"""
import math, os, sys
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__)); D = os.path.dirname(here)
P = {n: Image.open(os.path.join(D, n + '.png')).convert('RGBA') for n in ['head', 'head_blink', 'body', 'arm', 'foot', 'tail']}
MIR = lambda im: im.transpose(Image.FLIP_LEFT_RIGHT)
P['arm_l'] = MIR(P['arm']); P['foot_r'] = MIR(P['foot'])
PIV = {'head': (0.5, 0.96), 'head_blink': (0.5, 0.96), 'body': (0.5, 0.5), 'tail': (0.5, 0.92),
       'arm': (0.86, 0.16), 'arm_l': (0.14, 0.16), 'foot': (0.5, 0.06), 'foot_r': (0.5, 0.06)}
# 몸 위 부착점 (몸 그림 좌상단 기준 0~1)
NECK = (0.5, 0.30); SH_L, SH_R = (0.19, 0.44), (0.81, 0.44); FT_L, FT_R = (0.25, 0.80), (0.75, 0.80); TAIL = (0.74, 0.86)
HEAD_K, ARM_K, FOOT_K = 0.95, 0.6, 0.85      # 크기 (몸 대비, Codex 전체 자세 비율)


def rot(v, a):
    r = math.radians(a); return (v[0]*math.cos(r) + v[1]*math.sin(r), -v[0]*math.sin(r) + v[1]*math.cos(r))
def att(name, at, ang, anchor):
    im = P[name]; px, py = PIV[name]
    return tuple(a + b for a, b in zip(at, rot(((anchor[0]-px)*im.width, (anchor[1]-py)*im.height), ang)))
def put(c, name, at, ang, k=1.0):
    im = P[name]
    if k != 1: im = im.resize((round(im.width*k), round(im.height*k)), Image.LANCZOS)
    px, py = PIV[name][0]*im.width, PIV[name][1]*im.height
    R = int(math.hypot(im.width, im.height)) + 4
    big = Image.new('RGBA', (2*R, 2*R)); big.alpha_composite(im, (int(R-px), int(R-py)))
    c.alpha_composite(big.rotate(ang, resample=Image.BICUBIC), (int(at[0]-R), int(at[1]-R)))


def ham(t=0.0, blink=False, cheer=0.0, size=(600, 700)):
    """t = 시간(초, 숨쉬기 · 손 꼼지락), blink = 눈 감은 머리, cheer 0~1 = 신나서 손 들썩 · 몸 통통"""
    c = Image.new('RGBA', size)
    breathe = math.sin(t * 2 * math.pi * 0.6)
    hop = abs(math.sin(t * 2 * math.pi * 2.2)) * 22 * cheer
    center = (size[0]/2, 420 - hop)
    a_body = 2.5 * math.sin(t * 2 * math.pi * 0.35)
    neck = att('body', center, a_body, NECK)
    put(c, 'tail', att('body', center, a_body, TAIL), a_body + 8*math.sin(t*2*math.pi*1.5))
    put(c, 'foot', att('body', center, a_body, FT_L), -6, FOOT_K)          # 발은 몸 뒤 (분홍 발끝만 아래로 보임)
    put(c, 'foot_r', att('body', center, a_body, FT_R), 6, FOOT_K)
    put(c, 'body', center, a_body)
    wig = 6*math.sin(t*2*math.pi*1.2) + 18*cheer*math.sin(t*2*math.pi*4.4)
    put(c, 'arm_l', att('body', center, a_body, SH_L), a_body + 16 - wig, ARM_K)
    put(c, 'arm', att('body', center, a_body, SH_R), a_body - 16 + wig, ARM_K)
    put(c, 'head_blink' if blink else 'head', (neck[0], neck[1] + 1.5*breathe), a_body*1.4 + 3*math.sin(t*2*math.pi*0.5), HEAD_K)
    return c


if __name__ == '__main__':
    mode = sys.argv[1] if len(sys.argv) > 1 else 'all'
    box = ham().getbbox(); box = (box[0]-40, box[1]-50, box[2]+40, box[3]+30)
    # ① 조립 비교 (평소 · 눈 감기 · Codex 전체 자세)
    ref = Image.open(os.path.join(here, 'ref_full_pose.png')).convert('RGBA')
    a, b = ham().crop(box), ham(0.0, True).crop(box)
    s = a.height / ref.height * 0.86; ref = ref.resize((round(ref.width*s), round(ref.height*s)), Image.LANCZOS)
    cmp = Image.new('RGBA', (a.width*2 + ref.width + 80, a.height), (225, 214, 196, 255))
    cmp.alpha_composite(a, (0, 0)); cmp.alpha_composite(b, (a.width, 0)); cmp.alpha_composite(ref, (a.width*2 + 40, (a.height - ref.height)//2 + 20))
    cmp.save(os.path.join(here, '_조립_비교.png'))
    # ② 아지트 배경 위 (빈 배경 1536x1024, 책상 위)
    bg = Image.open(os.path.join(here, 'ref_lobby_bg.png')).convert('RGBA')
    H = 150; h = a.resize((round(a.width*H/a.height), H), Image.LANCZOS)
    scene = bg.copy(); scene.alpha_composite(h, (int(bg.width*0.47 - h.width/2), int(bg.height*0.655 - h.height)))
    scene.save(os.path.join(D, '_미리보기_아지트.png'))
    # ③ 움직임 GIF: 평소 숨쉬기 · 깜빡 → 클릭하면 신나서 들썩 2초
    fr = []
    for i in range(120):
        t = i / 25
        blink = (i % 60) in (20, 21, 22) and i < 60
        cheer = max(0, math.sin(min(1, (t - 2.4) / 2.0) * math.pi)) if t > 2.4 else 0
        f = Image.new('RGBA', (box[2]-box[0], box[3]-box[1]), (225, 214, 196, 255))
        f.alpha_composite(ham(t, blink or cheer > 0.3, cheer).crop(box)); f = f.resize((f.width//2, f.height//2), Image.LANCZOS)
        fr.append(f.convert('RGB').quantize(128))
    fr[0].save(os.path.join(D, '_미리보기_움직임.gif'), save_all=True, append_images=fr[1:], duration=40, loop=0)
    print('ok', box)
