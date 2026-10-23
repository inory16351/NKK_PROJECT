"""쥐 히드라 리그 → GIF (2026-10-23). hydra_rig.Rig.render = rig_web/hydra.html 과 같은 계산 (파츠 PNG 를 붙이기만 함).
장면 (14.5초): 걸음(왼쪽으로) → 대기 → 물기 (한 목씩 7번) → 일제히 물기 → 맞음
사용: python Tools/hydra_gif.py <출력.gif> [--frames 폴더]   (--frames = 확인용 PNG 몇 장)"""
import json, math, os, sys
from PIL import Image, ImageDraw, ImageFilter
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hydra_rig import Rig, OUT as RIG_JSON, T, S

OUT = sys.argv[1]
FR = sys.argv[sys.argv.index('--frames') + 1] if '--frames' in sys.argv else None
cfg = json.load(open(RIG_JSON, encoding='utf-8'))
rig = Rig(cfg['parts'])
W, H, FPS = 720, 500, 12
BG = (79, 93, 74, 255)
Z = 0.225
GROUND = int(H * 0.93)
BITE_P = 0.9

frames, t, walk, x = [], 0.0, 0.0, W * 0.62


def shot(mode, hk=0.0, jit=0):
    f = Image.new('RGBA', (W, H), BG)
    ImageDraw.Draw(f).rectangle((0, GROUND, W, GROUND + 2), fill=(0, 0, 0, 38))
    sh = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse((x - 170, GROUND - 17, x + 170, GROUND + 17), fill=(0, 0, 0, 60))
    f.alpha_composite(sh.filter(ImageFilter.GaussianBlur(6)))
    f.alpha_composite(rig.render((W, H), T(x + jit, GROUND) @ S(Z), mode, t, walk, hk=hk, bite_p=BITE_P))
    frames.append(f.convert('RGB'))


dt = 1 / FPS
for i in range(int(3 * FPS)): t += dt; walk += dt * 3; x -= 25 * dt; shot('walk')       # ① 걸음
for i in range(int(1 * FPS)): t += dt; shot('idle')                                      # ② 대기
t0 = t
for i in range(int(7 * BITE_P * FPS)): t += dt; shot('bite')                             # ③ 한 목씩 물기
for i in range(int(2.5 * FPS)): t += dt; shot('frenzy')                                  # ④ 일제히 물기
for i in range(int(1.2 * FPS)):                                                          # ⑤ 맞음 (움찔 2번)
    t += dt; e = (i * dt) % 0.6; shot('hit', hk=max(0, 1 - e / 0.35), jit=3 if e < 0.1 else 0)
for i in range(int(0.6 * FPS)): t += dt; shot('idle')
# 용량: 프레임 몇 장을 이어 붙여 만든 공통 팔레트 하나로 (디더링 없음 → 플랫 그림이라 깔끔, 프레임끼리 차이만 저장돼 작아짐)
probe = Image.new('RGB', (W, H * 4))
for k in range(4): probe.paste(frames[len(frames) * k // 4], (0, H * k))
pal = probe.quantize(colors=96, method=Image.Quantize.MEDIANCUT)
q = [fr.quantize(palette=pal, dither=Image.Dither.NONE) for fr in frames]
q[0].save(OUT, save_all=True, append_images=q[1:], duration=int(1000 / FPS), loop=0, optimize=True)
print('GIF', OUT, len(frames), '프레임', f'{len(frames) / FPS:.1f}초')
if FR:
    os.makedirs(FR, exist_ok=True)
    for k in range(0, len(frames), 12): frames[k].save(os.path.join(FR, f'f{k:03d}.png'))
