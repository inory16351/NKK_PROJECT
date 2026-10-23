"""초필살기 레이어 컷신 그림 자르기 (2026-10-17): UnityResources/Rats/SuperUlt/<code>/Sheets → <code>/ (PNG)
- bg_*.png · pano_on/off.png · *_face_*.png = 그대로 복사 (pano_a~c 조각은 건너뜀)
- *_pose_*.png          = 자홍 키잉 + 여백 자르기 (캐릭터 한 장)
- face_close.png        = face_bg.png 가 있으면(쥐·배경 따로 그린 v2) 자홍 키잉만, 없으면 그대로
- fx_*.png              = 검은 배경 빛 효과 격자 (FX_GRID 의 열·행·이름) → 밝기 = 투명도
- props.png             = 자홍 키잉 후 덩어리마다 따로 (PROPS 이름, 왼쪽 위부터 읽는 순서 아님 → 크기·위치로 이름 붙임, 아래 표)
사용: python Tools/slice_superult.py <code>   →  다 자른 뒤 Assets/Art/Rats/SuperUlt/<code>/ 로 복사"""
import os, shutil, sys
import numpy as np
from PIL import Image
try: from scipy import ndimage          # 셀 프레임 · 소품 덩어리 나누기에만 씀
except ImportError: ndimage = None
sys.path.insert(0, os.path.dirname(__file__))
from slice_rat_parts import key_magenta
from slice_glow_grid import key_black

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
FX_GRID = {'fx_bolts.png': (3, 2, ['bolt_big', 'bolt_fork', 'ring_shock', 'streak', 'starburst', 'aura'])}
# 소품: 덩어리 중심 (시트 비율 x, y) 에 가장 가까운 이름
PROPS = {'zapham': {'bill': (0.45, 0.18), 'battery': (0.83, 0.13), 'socket': (0.80, 0.45), 'bulb': (0.12, 0.55), 'roof': (0.55, 0.85)}}


def despill(im):
    """자홍 배경에 섞인 빛·불꽃 가장자리의 분홍 번짐 걷기: 초록이 빨강·파랑보다 확 낮은(자홍 쪽) 픽셀만 → 파랑을 낮추고 투명도도 그만큼 낮춤.
    귀·볼 같은 연한 분홍(초록이 그리 낮지 않음)은 그대로."""
    a = np.asarray(im).astype(np.float32)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    m = np.minimum(r, b)
    k = np.clip(((m - g) / np.maximum(m, 1) - 0.12) / 0.55, 0, 1)         # 0 = 괜찮음 · 1 = 거의 자홍
    a[..., 2] = b - (b - np.minimum(b, g * 0.6)) * k
    a[..., 3] = al * (1 - 0.85 * k)
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA')


def trim(im, pad=6):
    a = np.asarray(im)[..., 3]
    ys, xs = np.nonzero(a > 8)
    return im.crop((max(0, xs.min() - pad), max(0, ys.min() - pad), min(im.width, xs.max() + pad + 1), min(im.height, ys.max() + pad + 1)))


GRID_FX = ['strike', 'impact', 'ring', 'streak', 'burst', 'aura']        # gen_superult_prompts.FX
GRID_PROPS = ['meme', 'pop', 'float1', 'float2', 'band', 'fg']           # gen_superult_prompts.PROPS
FLAT_NAMES = {'knight': ['ghostsword', 'pillar', 'arc', 'motes', 'burst', 'ring'],      # fx_flat.png 칸 이름 (쥐마다 그린 것이 다를 때)
              'pharaoh': ['circle', 'sandburst', 'dust', 'bandage', 'sunflash', 'sandring'],
              'bomber': ['circle', 'circle_persp', 'runering', 'circle_small', 'redflash', 'embers'],
              'streamrat': ['heart', 'heartburst', 'note', 'glint', 'neonring', 'confetti'],
              'ramjui': ['bolt', 'forkbolt', 'arc', 'zapburst', 'shockring', 'sparks'],
              'fondue': ['snap', 'shockring', 'splash', 'drops', 'spray', 'streak'],
              'sultan': ['coin', 'coin2', 'sparkle', 'coinburst', 'gem', 'oil'],      # 술탄 v2 (2026-10-20): 금화 · 기운 금화 · 반짝 · 금화 터짐 · 보석 · 기름 방울
              'starchef': ['tuft', 'strands', 'dizzy', 'twinkle', 'smear', 'streak'],
              'alien': ['charge', 'lightring', 'flash', 'fire', 'cloud', 'streak'],      # 외계인 생쥐 v2 (2026-10-21): 에너지 알갱이 · 빛 고리 · 섬광 · 불 · 밤 구름 · 속도선
              'jjdetective': ['sparkle', 'glare', 'bubble', 'dust', 'shockring', 'streak'],      # 명탐정 쥐 v2 (2026-10-21): 반짝 · 안경 번쩍 · 코풍선 · 먼지 · 충격파 고리 · 속도선
              'zapham': ['bolt', 'forkbolt', 'sparks', 'shockring', 'hitstar', 'twinkle'],      # 짜릿햄쥐 v2 (2026-10-21): 백만볼트 · 갈라진 번개 · 스파크 · 충격파 고리 · 박치기 별 · 반짝 ✦
              'parkrat': ['sparkle', 'confetti', 'clap', 'shockring', 'wind', 'streak'],
              'jwerry': ['dust', 'streak', 'stars', 'bonk', 'steam', 'shockring'],      # 줴리 v2 (2026-10-21): 흙먼지 · 속도선 · 기절 별 · 뿅 · 김 · 충격파 고리      # 쥐랜드 관광쥐 v2 (2026-10-21): 유레카 반짝 · 색종이 · 박수 표시 · 충격파 고리 · 돌풍 · 속도선
              'emperor': ['muzzle', 'smoke', 'bullet', 'summon', 'spark', 'streak']}     # 쥐 황제 v2 (2026-10-20): 총구 섬광 · 연기 · 총알 · 소환 연기 · 탄착 불꽃 · 속도선     # 슈퍼 요리사 v2 (2026-10-20): 머리카락 뭉치 · 머리카락 · 기절 별 · 반짝 · 블러 휙 · 속도선     # 치즈 퐁듀 v2 (2026-10-20): 스냅 반짝 · 고리 · 치즈 튐 · 치즈 방울 · 물보라 · 속도선     # 스트리머 v2 (2026-10-20): 하트 · 하트 터짐 · 음표 · 고글 반짝 · 네온 고리 · 색종이    # 폭탄마 v2 (2026-10-20): 빨간 마법진 · 원근 마법진 · 룬 고리 · 작은 마법진 · 붉은 섬광 · 불티
STRIP3 = {'beam.png': ['fx_beam', 'fx_beamcore', 'fx_beamhit'], 'mushroom.png': ['fx_mush1', 'fx_mush2', 'fx_mush3'], 'geyser.png': ['fx_geyser1', 'fx_geyser2', 'fx_geyser3'], 'pour.png': ['fx_pour1', 'fx_pour2', 'fx_pour3'], 'balloon.png': ['prop_balloon', 'prop_balloon_tight', 'prop_balloon_burst']}   # 가로 3칸 (자홍, 여백 자름) — 폭탄마 v2 레이저 · 버섯구름 (버섯구름은 같은 바닥선 → 칸 크기 그대로)
GRID6 = {'boom.png': (['boom1', 'boom2', 'boom3', 'shockring', 'debris', 'smoke'], 3), 'pop.png': (['pop1', 'pop2', 'pop3', 'dome', 'debris', 'dustwall'], 3)}    # 자홍 3×2: 앞 n 개 = 프레임 (칸 크기 그대로) · 나머지 = 여백 자름  # 파라오 v2 (2026-10-20): 소환진 · 모래 분수 · 모래 먼지 · 붕대 · 해 섬광 · 모래 고리
HIT_NAMES = {'pharaoh': ['sandstreak', 'scarab', 'puff', 'hit1', 'hit2', 'hit3'], 'streamrat': ['streak', 'coin', 'dot', 'pop1', 'pop2', 'pop3'],
             'ramjui': ['shockwave', 'chunks', 'streak', 'land1', 'land2', 'land3'],
             'fondue': ['drop', 'wedge', 'wall', 'splat1', 'splat2', 'splat3'],
             'sultan': ['coinfall', 'sack', 'wall', 'clink1', 'clink2', 'clink3'],
             'emperor': ['tracer', 'smokeband', 'dust', 'hit1', 'hit2', 'hit3'],
             'zapham': ['boltfall', 'zsparks', 'zstreak', 'hit1', 'hit2', 'hit3'],
             'jjdetective': ['dartfall', 'feather', 'snot', 'hit1', 'hit2', 'hit3'],
             'jwerry': ['panfall', 'pan2', 'star', 'hit1', 'hit2', 'hit3']}       # fx_hit.png 칸 이름 (기본 = 기사: slash · wave · mote · hit1~3)
ROT_RIGHT = {'jjdetective': ['fx_dartfall']}     # 메이드 쥐 칼 비 (2026-10-20)
FLAT_PROPS = {'pharaoh': ['sarcophagus', 'jar', 'scarab', 'obelisk', 'meme', 'fg'], 'streamrat': ['monitor', 'coins', 'gift', 'like', 'meme', 'glowstick'],
              'ramjui': ['hammer', 'acorn', 'helmet', 'polaroid', 'cursor', 'leaf'],
              'starchef': ['hat', 'sweat', 'steam', 'tear', 'meme', 'fg'],
              'jjdetective': ['pawprint', 'hair', 'fishbone', 'scratch', 'bulb', 'dart'],      # 명탐정 쥐 v2: 단서 4 (발자국 · 고양이 털 · 생선 뼈 · 할퀸 자국) · 전구 · 마취침
              'zapham': ['seed', 'cloud', 'cloudstreak', 'smoke', 'dust', 'sweat'],      # 짜릿햄쥐 v2: 해바라기씨 · 구름 · 구름 줄 · 연기 · 흙먼지 · 땀
              'parkrat': ['balloon_old', 'balloon_tight_old', 'bulb', 'flyrat1', 'flyrat2', 'ears'],
              'jwerry': ['pan', 'cheese', 'book', 'plant', 'chair', 'sweat']}     # 줴리 v2: 프라이팬 · 치즈 · 책 · 앞 화분 · 앞 의자 (시차) · 땀     # 쥐랜드 관광쥐 v2: 쥐 머리 풍선 · 터지기 직전 풍선 · 아이디어 전구 · 날아가는 관람객 쥐 2 · 머리띠     # props_flat.png 칸 이름 (기본 = GRID_PROPS)
ANIM_SAMEBOX = {'anim_shock.png', 'anim_hamtease.png', 'anim_soldier.png', 'anim_front.png', 'anim_pump.png', 'anim_jrun.png', 'anim_tomrun.png'}    # 셀 프레임을 같은 틀로 자를 시트 (쥐 황제 v2 병사: 격발 프레임 총구 섬광)
TINT = {'alien': [('fx_cloud', (112, 116, 178), (196, 198, 238))]}
FLIP_X = {'emperor': ['fx_muzzle']}    # 자른 뒤 좌우 뒤집을 그림 (Codex 가 반대 방향으로 그린 것: 쥐 황제 총구 섬광)
EXTRA_GRID = {'emperor': {'fx_front.png': ['fmuzzle', 'fbullet', 'fbullet3q', 'smokering', 'hole', 'radial']},
              'alien': {'fx_v3.png': ['signal', 'jetdot', 'afterburn', 'shard', 'panel', 'tear']}}     # 쥐마다 더 그린 자홍 3×2 시트 → fx_<이름> (여백 자름)
GRID_CRACK = ['crack1', 'crack2', 'crack3', 'shard1', 'shard2', 'shard3']   # crack.png (자홍 3×2): 화면 얼음 금 · 깨진 파편 (2026-10-19 아이스 퀸 v2)


def stitch_pano(src, out):
    """pano_a~c → pano_on (겹침 자동), 상태 바뀐 판 pano_?_alt → pano_alt (같은 겹침)"""
    from stitch_vertical import find_overlap
    import json
    names = [os.path.join(src, f'pano_{k}.png') for k in 'abc']
    if not all(os.path.exists(n) for n in names): return
    horiz = json.load(open(os.path.join(src, 'story.json'), encoding='utf-8')).get('orient') == 'h'
    tr = (lambda a: a.transpose(1, 0, 2)) if horiz else (lambda a: a)      # 가로 파노라마 = 옆으로 돌려 세로처럼 이어 붙임
    ims = [tr(np.asarray(Image.open(n).convert('RGB')).astype(np.float32)) for n in names]
    ovs = []
    acc = ims[0]
    for im in ims[1:]:
        ov = find_overlap(acc, im)[1]; ovs.append(ov)
        w = np.linspace(0, 1, ov)[:, None, None]
        acc = np.concatenate([acc[:-ov], acc[-ov:] * (1 - w) + im[:ov] * w, im[ov:]], 0)
    Image.fromarray(tr(acc).clip(0, 255).astype(np.uint8)).save(os.path.join(out, 'pano_on.png')); print('pano_on', tr(acc).shape[1], tr(acc).shape[0], '겹침', ovs)
    alts = [os.path.join(src, f'pano_{k}_alt.png') for k in 'abc']
    if all(os.path.exists(n) for n in alts):
        ims = [tr(np.asarray(Image.open(n).convert('RGB').resize((1536, 1024))).astype(np.float32)) for n in alts]
        acc = ims[0]
        for im, ov in zip(ims[1:], ovs):
            w = np.linspace(0, 1, ov)[:, None, None]
            acc = np.concatenate([acc[:-ov], acc[-ov:] * (1 - w) + im[:ov] * w, im[ov:]], 0)
        Image.fromarray(tr(acc).clip(0, 255).astype(np.uint8)).save(os.path.join(out, 'pano_alt.png')); print('pano_alt')


def grid(path, names, keyer, out, prefix):
    sh = Image.open(path); W, H = sh.size
    for i, n in enumerate(names):
        r, c = divmod(i, 3)
        cell = keyer(sh.crop((int(c * W / 3) + 3, int(r * H / 2) + 3, int((c + 1) * W / 3) - 3, int((r + 1) * H / 2) - 3)))
        try: cell = trim(cell, 4)
        except ValueError: print('빈 칸', n); continue
        cell.save(os.path.join(out, prefix + n + '.png')); print(prefix + n, cell.size)


def main(code):
    src = os.path.join(ROOT, 'UnityResources', 'Rats', 'SuperUlt', code, 'Sheets')
    out = os.path.join(ROOT, 'UnityResources', 'Rats', 'SuperUlt', code)
    if code != 'zapham': stitch_pano(src, out)
    rig_parts(code, src, out)
    for f in sorted(os.listdir(src)):
        p = os.path.join(src, f)
        if not f.endswith('.png') or f.startswith('ref_') or f.startswith('rig_'): continue
        if f in ('pano_on.png', 'pano_off.png', 'pano_alt.png') or '_face_' in f:       # 세로 파노라마 (조각 pano_a~c 를 stitch_vertical 로 붙인 것) · 전체 화면 클로즈업
            shutil.copy(p, os.path.join(out, f)); print(f, '그대로')
        elif f == 'pano_rear_full.png':   # 기사 v13 후경 파노라마 (Sheets/v13 3장 이어 붙임) → pano_rear
            shutil.copy(p, os.path.join(out, 'pano_rear.png')); print('pano_rear', '파노라마')
            im = Image.open(p); w, h = im.size; yy, xx = np.mgrid[0:h, 0:w]          # 크롭형 마스크 블러: 기사 둘레 타원은 선명, 바깥으로 갈수록 흐림 (카메라가 기사를 보므로 화면 가장자리 = 흐림)
            rr = np.sqrt(((xx - 768) / 270) ** 2 + ((yy - 1080) / 210) ** 2)
            blur_copy(im, 16, np.clip((rr - 1) / 1.4, 0, 1)).save(os.path.join(out, 'pano_rear_blur.png')); print('pano_rear_blur')
        elif f.startswith('pano_'): continue
        elif code != 'zapham' and (f.startswith('pose_') or f.startswith('face_')):
            im = Image.open(p)
            if f.startswith('pose_'): im = trim(despill(key_magenta(im)))
            elif f == 'face_close.png' and os.path.exists(os.path.join(src, 'face_bg.png')):     # v2 (2026-10-19): 쥐 · 배경 따로 → 쥐만 키잉 (자르지 않음, face_bg 와 같은 틀)
                im = despill(key_magenta(im))
            im.save(os.path.join(out, f)); print(f, im.size)
        elif code != 'zapham' and f == 'fx.png' and not os.path.exists(os.path.join(src, 'fx_flat.png')):
            grid(p, GRID_FX, key_black, out, 'fx_')
        elif f in EXTRA_GRID.get(code, {}):
            grid(p, EXTRA_GRID[code][f], lambda c: despill(key_magenta(c)), out, 'fx_')
        elif f == 'fx_flat.png':          # 플랫 이펙트 (2026-10-19, 자홍 3×2) — 있으면 fx.png(검은 배경 빛) 대신. 쥐마다 칸 이름이 다르면 FLAT_NAMES
            grid(p, FLAT_NAMES.get(code, GRID_FX), lambda c: despill(key_magenta(c)), out, 'fx_')
        elif f == 'altar.png':            # 기사 v3: 돌 제단 · 대검 · 돌 파편 (자홍 가로 3칸)
            sh = Image.open(p); W3 = sh.width / 3
            for k, n in enumerate(['prop_altar', 'prop_greatsword', 'prop_debris']):
                cell = trim(despill(key_magenta(sh.crop((int(k * W3) + 3, 3, int((k + 1) * W3) - 3, sh.height - 3)))))
                cell.save(os.path.join(out, n + '.png')); print(n, cell.size)
        elif f == 'fx_wave.png':          # 기사 v4: 세로 3줄 = 파동 띠 · 충격파 줄 · 에너지 파편 (가로로 긴 그림, 여백 자름)
            sh = Image.open(p); H3 = sh.height / 3
            for k, n in enumerate(['slashwave', 'ripple', 'shards']):
                cell = trim(despill(key_magenta(sh.crop((3, int(k * H3) + 3, sh.width - 3, int((k + 1) * H3) - 3)))), 4)
                cell.save(os.path.join(out, 'fx_' + n + '.png')); print('fx_' + n, cell.size)
        elif f == 'fx_light.png':         # 기사 v7: 별 모양 없는 빛 (둥근 섬광 · 정면 빔 · 빛 기둥 · 빛 알갱이 · 빛 고리 · 빛 띠)
            grid(p, ['flash', 'beam', 'column', 'dots', 'lightring', 'band'], lambda c: despill(key_magenta(c)), out, 'fx_')
        elif f in ('open1.png', 'open2.png', 'open3.png'):     # 기사 v14: 찢겨 열리는 빛 벽 3프레임 (자홍 = 투명 구멍, 크기 그대로) + 크롭형 마스크 블러 판 open*_blur
            im = despill(key_magenta(Image.open(p))); im.save(os.path.join(out, f))
            a = np.asarray(im).astype(np.float32) / 255; h, w = a.shape[:2]
            pm = Image.fromarray((np.dstack([a[..., :3] * a[..., 3:], a[..., 3:]]) * 255).astype(np.uint8), 'RGBA')       # 미리 곱한 색 (투명 쪽으로 검게 번지지 않게)
            from PIL import ImageFilter
            bl = np.asarray(pm.filter(ImageFilter.GaussianBlur(22))).astype(np.float32) / 255
            yy, xx = np.mgrid[0:h, 0:w]
            rr = np.sqrt(((xx - w * 0.5) / (w * 0.2)) ** 2 + ((yy - h * 0.45) / (h * 0.225)) ** 2)     # 가운데 타원(폭 40% · 높이 45%) = 선명, 바깥으로 갈수록 흐림
            m = np.clip((rr - 1) / 1.6, 0, 1) ** 1.2
            al = bl[..., 3] * m; col = bl[..., :3] / np.maximum(bl[..., 3:], 1e-4)
            Image.fromarray((np.dstack([col, al[..., None]]).clip(0, 1) * 255).astype(np.uint8), 'RGBA').save(os.path.join(out, f[:-4] + '_blur.png')); print(f, '+ blur')
        elif f in ('fx_smear.png', 'fx_flamewall.png'):     # 기사 v12: 칼 잔상 줄무늬 · 갈라지는 빛·불꽃 벽 반쪽 (크기 그대로 — 찢긴 선 위치 계산용)
            despill(key_magenta(Image.open(p))).save(os.path.join(out, f)); print(f, 'v12')
            if f == 'fx_flamewall.png': blur_copy(Image.open(os.path.join(out, f)), 20).save(os.path.join(out, 'fx_flamewall_blur.png')); print('fx_flamewall_blur')
        elif f == 'split_frame.png':      # 기사 v11 후경: 화면 양옆 갈라진 벽 (가운데 자홍 = 투명, 크기 그대로)
            despill(key_magenta(Image.open(p))).save(os.path.join(out, f)); print(f, '양옆 벽')
        elif f in ('rear_knight.png', 'ground_crack.png'):     # 기사 v10 후경: 휘두른 뒤 기사 · 땅 균열 (자홍, 여백 자름)
            im = trim(despill(key_magenta(Image.open(p))), 4); im.save(os.path.join(out, f)); print(f, im.size)
        elif f == 'pano_rear_full.png':   # 기사 v13 후경 파노라마 (Sheets/v13 3장 이어 붙임) → pano_rear
            shutil.copy(p, os.path.join(out, 'pano_rear.png')); print('pano_rear', '파노라마')
            im = Image.open(p); w, h = im.size; yy, xx = np.mgrid[0:h, 0:w]          # 크롭형 마스크 블러: 기사 둘레 타원은 선명, 바깥으로 갈수록 흐림 (카메라가 기사를 보므로 화면 가장자리 = 흐림)
            rr = np.sqrt(((xx - 768) / 270) ** 2 + ((yy - 1080) / 210) ** 2)
            blur_copy(im, 16, np.clip((rr - 1) / 1.4, 0, 1)).save(os.path.join(out, 'pano_rear_blur.png')); print('pano_rear_blur')
        elif f == 'rear.png':             # 스트리머 v2: 뒷모습 통이미지 (한 장 그대로) + 블러 사본 rear_blur (줌 · 회전 전환의 잔상)
            shutil.copy(p, os.path.join(out, f)); blur_copy(Image.open(p), 18).save(os.path.join(out, 'rear_blur.png')); print('rear + rear_blur')
        elif f.startswith(('ufo', 'ship', 'jet_')):     # 외계인 생쥐 v2 · v3: 모선 · 전투기 연속 프레임 (자홍, 같은 틀 · 자르지 않음)
            despill(key_magenta(Image.open(p))).save(os.path.join(out, f)); print(f, '같은 틀 프레임')
        elif f == 'fg_wall.png':          # 외계인 생쥐 v3: 화면 1/3 을 가리는 앞 건물 (자홍, 여백 자름)
            im = trim(despill(key_magenta(Image.open(p))), 2); im.save(os.path.join(out, f)); print(f, im.size)
        elif f.startswith('cu_'):         # 치즈 퐁듀 v2: 전체 화면 클로즈업 연속 프레임 (자홍, face_bg 위에 겹침 · 프레임끼리 같은 틀이라 자르지 않음)
            despill(key_magenta(Image.open(p))).save(os.path.join(out, f)); print(f, '클로즈업 프레임')
        elif f == 'wide_on.png':          # 치즈 퐁듀 v2 롱샷: pano_on 을 가운데에 두고 둘레를 이어 그린 넓은 판 (줌 아웃 끝)
            shutil.copy(p, os.path.join(out, f)); print(f, '그대로')
        elif f.startswith('shot_'):       # 슈퍼 요리사 v2: 배경까지 그린 한 장 컷 (연속 프레임은 같은 틀 · 자르지 않음)
            shutil.copy(p, os.path.join(out, f)); print(f, '컷 한 장')
        elif f == 'body_up.png':          # 치즈 퐁듀 v2: 아래 → 위 틸트용 익스트림 클로즈업 (배경 포함 한 장)
            shutil.copy(p, os.path.join(out, f)); print(f, '그대로')
        elif f == 'rear_bg.png':
            shutil.copy(p, os.path.join(out, f)); print(f, '배경')
        elif f in ('fx_frame.png', 'fx_blast.png'):     # 기사 v8: 탄 가장자리 화면 틀(가운데 자홍 = 투명, 크기 그대로) · 정면 빛 폭발 (여백 자름)
            im = despill(key_magenta(Image.open(p)))
            if f == 'fx_blast.png': im = trim(im, 4)
            im.save(os.path.join(out, f)); print(f, im.size)
        elif f == 'fx_cut.png':           # 기사 v3: 위 줄 = 베기선 · 차징 기운 · 불꽃 (여백 자름) / 아래 줄 = 폭발 3프레임 (칸 크기 그대로)
            sh = Image.open(p); W, H = sh.size
            for i, n in enumerate(['cutline', 'charge', 'sparks', 'boom1', 'boom2', 'boom3']):
                r, c = divmod(i, 3)
                cell = despill(key_magenta(sh.crop((int(c * W / 3) + 3, int(r * H / 2) + 3, int((c + 1) * W / 3) - 3, int((r + 1) * H / 2) - 3))))
                if r == 0: cell = trim(cell, 4)
                cell.save(os.path.join(out, 'fx_' + n + '.png')); print('fx_' + n, cell.size)
        elif f == 'fx_hit.png':           # 타격 그림 (2026-10-19 기사, 자홍 3×2): 위 줄 = 베기 · 빛 물결 · 빛 입자 (여백 자름) / 아래 줄 = 타격 3프레임 (칸 크기 그대로)
            sh = Image.open(p); W, H = sh.size
            for i, n in enumerate(HIT_NAMES.get(code, ['slash', 'wave', 'mote', 'hit1', 'hit2', 'hit3'])):
                r, c = divmod(i, 3)
                cell = despill(key_magenta(sh.crop((int(c * W / 3) + 3, int(r * H / 2) + 3, int((c + 1) * W / 3) - 3, int((r + 1) * H / 2) - 3))))
                if r == 0: cell = trim(cell, 4)
                cell.save(os.path.join(out, 'fx_' + n + '.png')); print('fx_' + n, cell.size)
        elif f == 'props_flat.png':       # 플랫 소품 — 있으면 props.png 대신
            grid(p, FLAT_PROPS.get(code, GRID_PROPS), lambda c: despill(key_magenta(c)), out, 'prop_')
        elif f in STRIP3:
            sh = Image.open(p); W3 = sh.width / 3
            for k, n in enumerate(STRIP3[f]):
                cell = despill(key_magenta(sh.crop((int(k * W3) + 3, 3, int((k + 1) * W3) - 3, sh.height - 3))))
                if not n.startswith(('fx_mush', 'fx_geyser', 'fx_pour')): cell = trim(cell, 4)      # 같은 바닥선 프레임 = 칸 크기 그대로
                cell.save(os.path.join(out, n + '.png')); print(n, cell.size)
                if n == 'fx_beam' and code == 'alien':     # 외계인 생쥐 v2: 마디로 이어 붙일 레이저 = 아래 퍼진 끝을 뺀 곧은 부분
                    seg = cell.crop((0, 0, cell.width, int(cell.height * 0.78))); seg.save(os.path.join(out, 'fx_beamseg.png')); print('fx_beamseg', seg.size)
        elif f in GRID6:
            names, nf = GRID6[f]; sh = Image.open(p); W, H = sh.size
            for i, n in enumerate(names):
                r, c = divmod(i, 3)
                cell = despill(key_magenta(sh.crop((int(c * W / 3) + 3, int(r * H / 2) + 3, int((c + 1) * W / 3) - 3, int((r + 1) * H / 2) - 3))))
                if i >= nf: cell = trim(drop_edge_bits(cell), 4)       # 옆 칸 부스러기(칸 테두리에 닿은 작은 덩어리) 지움
                cell.save(os.path.join(out, 'fx_' + n + '.png')); print('fx_' + n, cell.size)
        elif f == 'army.png':             # 파라오 v2: 정면으로 선 소환 군단 한 장 (자홍, 여백 자름)
            im = trim(despill(key_magenta(Image.open(p))), 4); im.save(os.path.join(out, f)); print(f, im.size)
        elif f.startswith('anim_'):       # 셀 애니 프레임 시트 (자홍 가로 3칸) → anim_<동작>_1~3
            im = despill(key_magenta(Image.open(p))); arr = np.asarray(im); W3 = im.width / 3
            if f == 'anim_pour.png': arr = arr.copy(); arr[int(im.height * 0.67):, :, 3] = 0      # 술탄 v2: 양탄자 밑 금화 줄기는 잘라냄 (따로 그린 fx_pour 가 이어 흐름, 발끝 = 양탄자 바닥)
            lab, n = ndimage.label(ndimage.binary_dilation(arr[..., 3] > 20, iterations=3))      # 덩어리마다 → 가운데가 속한 칸으로 (옆 칸에 걸친 망토·꼬리도 제 프레임에)
            col = np.zeros(n + 1, int) - 1
            for li, sl in enumerate(ndimage.find_objects(lab), 1):
                if (sl[0].stop - sl[0].start) * (sl[1].stop - sl[1].start) < 400: continue
                ys, xs = np.nonzero(lab[sl] == li); col[li] = min(2, int((xs.mean() + sl[1].start) / W3))
            cells = []
            for k in range(3):
                a2 = arr.copy(); a2[..., 3] = np.where(np.isin(lab, np.nonzero(col == k)[0]), arr[..., 3], 0)
                cells.append(a2)
            if f in ANIM_SAMEBOX:         # 세 프레임을 같은 틀로 (칸 기준 같은 자리 · 합친 여백): 프레임마다 여백을 자르면 총구 섬광처럼 한쪽만 늘어난 프레임이 옆으로 튐 (2026-10-20 쥐 황제 병사)
                pad = int(W3 * 0.2); locs = []
                for k, a2 in enumerate(cells):
                    x0 = int(k * W3) - pad; loc = np.zeros((im.height, int(W3) + 2 * pad, 4), np.uint8)
                    s0, s1 = max(0, x0), min(im.width, x0 + loc.shape[1]); loc[:, s0 - x0:s1 - x0] = a2[:, s0:s1]; locs.append(loc)
                al = np.max([l_[..., 3] for l_ in locs], 0); ys, xs = np.nonzero(al > 8)
                box = (max(0, xs.min() - 6), max(0, ys.min() - 6), xs.max() + 7, ys.max() + 7)
                for k, loc in enumerate(locs):
                    cell = Image.fromarray(loc, 'RGBA').crop(box); cell.save(os.path.join(out, f'{f[:-4]}_{k + 1}.png')); print(f'{f[:-4]}_{k + 1}', cell.size, '같은 틀')
            else:
                for k, a2 in enumerate(cells):
                    cell = trim(Image.fromarray(a2, 'RGBA'))
                    cell.save(os.path.join(out, f'{f[:-4]}_{k + 1}.png')); print(f'{f[:-4]}_{k + 1}', cell.size)
        elif f == 'hail.png':             # 우박 그림 이펙트 (2026-10-19, 자홍 3×2): 떨어지는 3종은 여백 자름 · 바닥 충돌 3프레임은 칸 크기 그대로 (프레임끼리 자리 맞춤)
            sh = Image.open(p); W, H = sh.size
            for i, n in enumerate(['hail_fall', 'hail_ball', 'hail_chunk', 'hail_splash1', 'hail_splash2', 'hail_splash3']):
                r, c = divmod(i, 3)
                cell = despill(key_magenta(sh.crop((int(c * W / 3) + 3, int(r * H / 2) + 3, int((c + 1) * W / 3) - 3, int((r + 1) * H / 2) - 3))))
                if r == 0: cell = trim(cell, 4)
                cell.save(os.path.join(out, 'fx_' + n + '.png')); print('fx_' + n, cell.size)
        elif f == 'castle.png':           # 따로 떨어지는 얼음성 (2026-10-19 초필살기 v3) → prop_castle
            im = trim(despill(key_magenta(Image.open(p)))); im.save(os.path.join(out, 'prop_castle.png')); print('prop_castle', im.size)
        elif f == 'crack.png':
            grid(p, GRID_CRACK, lambda c: despill(key_magenta(c)), out, 'fx_')
        elif code != 'zapham' and f == 'props.png' and not os.path.exists(os.path.join(src, 'props_flat.png')):
            grid(p, GRID_PROPS, lambda c: despill(key_magenta(c)), out, 'prop_')
        elif f.startswith('bg_'):
            shutil.copy(p, os.path.join(out, f)); print(f, '배경')
        elif '_pose_' in f:
            im = trim(despill(key_magenta(Image.open(p)))); im.save(os.path.join(out, f)); print(f, im.size)
        elif f in FX_GRID:
            cols, rows, names = FX_GRID[f]
            sh = Image.open(p); W, H = sh.size
            for i, n in enumerate(names):
                r, c = divmod(i, cols)
                cell = key_black(sh.crop((int(c * W / cols) + 3, int(r * H / rows) + 3, int((c + 1) * W / cols) - 3, int((r + 1) * H / rows) - 3)))
                cell = trim(cell, 4); cell.save(os.path.join(out, 'fx_' + n + '.png')); print('fx_' + n, cell.size)
        elif f == 'props.png' and code in PROPS:
            im = despill(key_magenta(Image.open(p))); W, H = im.size
            a = np.asarray(im)[..., 3] > 20
            lab, n = ndimage.label(ndimage.binary_dilation(a, iterations=14))      # 가까운 조각(불꽃·파편)은 한 덩어리로
            boxes = ndimage.find_objects(lab)
            blobs = [(int(((b[0].stop - b[0].start) * (b[1].stop - b[1].start))), b, i + 1) for i, b in enumerate(boxes)]
            blobs = [x for x in blobs if x[0] > 4000]
            for name, (px, py) in PROPS[code].items():
                best = min(blobs, key=lambda x: ((x[1][1].start + x[1][1].stop) / 2 / W - px) ** 2 + ((x[1][0].start + x[1][0].stop) / 2 / H - py) ** 2)
                _, b, li = best
                mask = (lab == li)
                arr = np.asarray(im).copy(); arr[..., 3] = np.where(mask, arr[..., 3], 0)
                part = trim(Image.fromarray(arr, 'RGBA').crop((b[1].start, b[0].start, b[1].stop, b[0].stop)), 2)
                part.save(os.path.join(out, 'prop_' + name + '.png')); print('prop_' + name, part.size)
    mask_blur(src, out)
    soft_blur(src, out)
    inset_frame(src, out)
    zoom_blur(src, out)
    motion_blur(src, out)
    for n, lo, hi in TINT.get(code, []):     # 밝기를 두 색 사이로 다시 칠함 (외계인 생쥐 v2: 회색 구름 → 배경 구름 라벤더)
        q = os.path.join(out, n + '.png')
        if os.path.exists(q):
            a = np.asarray(Image.open(q).convert('RGBA')).astype(np.float32); l = (a[..., :3].mean(2, keepdims=True) - 60) / 170
            a[..., :3] = np.array(lo) + (np.array(hi) - np.array(lo)) * np.clip(l, 0, 1); Image.fromarray(a.clip(0, 255).astype(np.uint8), 'RGBA').save(q); print(n, '색 다시')
    for n in FLIP_X.get(code, []):
        q = os.path.join(out, n + '.png')
        if os.path.exists(q): Image.open(q).transpose(Image.FLIP_LEFT_RIGHT).save(q); print(n, '좌우 뒤집음')
    for n in ROT_RIGHT.get(code, []):     # 아래를 향한 그림 → 오른쪽을 향한 사본 *r (게임 화면 낙하 입자는 그림 +x 를 속도 방향으로 돌림, CutFx)
        q = os.path.join(out, n + '.png')
        if os.path.exists(q): Image.open(q).rotate(90, expand=True).save(os.path.join(out, n + 'r.png')); print(n + 'r', '오른쪽으로 돌린 사본')
    if code in FLAT_NAMES and 'streak' not in FLAT_NAMES[code]:      # 속도선(fx_streak)이 없는 쥐 = 아이스 퀸 플랫 줄무늬를 씀 (흰색, 색 상관없음)
        shutil.copy(os.path.join(ROOT, 'UnityResources', 'Rats', 'SuperUlt', 'icequeen', 'fx_streak.png'), os.path.join(out, 'fx_streak.png'))
    pad4(out)
    dst = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats', 'SuperUlt', code)
    os.makedirs(dst, exist_ok=True)
    for f in os.listdir(out):
        if f.endswith('.png') or f == 'rig.json': shutil.copy(os.path.join(out, f), os.path.join(dst, f))
    print('→', dst)


def drop_edge_bits(im, frac=0.08):
    """칸 테두리에 닿은 작은 덩어리(옆 칸 그림 부스러기)를 투명하게 — 전체의 frac 보다 작고 테두리에 닿은 것"""
    arr = np.asarray(im).copy(); a = arr[..., 3] > 20
    lab, n = ndimage.label(ndimage.binary_dilation(a, iterations=3))
    if n < 2: return im
    sizes = ndimage.sum(a, lab, range(1, n + 1)); big = sizes.sum()        # 전체 대비 (파편처럼 작은 덩어리 여럿인 그림도)
    h, w = a.shape
    for li, sl in enumerate(ndimage.find_objects(lab), 1):
        edge = sl[0].start == 0 or sl[1].start == 0 or sl[0].stop >= h or sl[1].stop >= w
        if edge and sizes[li - 1] < big * frac: arr[..., 3][lab == li] = 0
    return Image.fromarray(arr, 'RGBA')


def pad4(out):
    """가로·세로를 4의 배수로 (투명 여백을 위 · 오른쪽에): 4의 배수가 아닌 그림은 WebAtlasBuilder 가 폴더 아틀라스(4096)로 묶는데,
    컷신 그림은 커서 아틀라스가 넘쳐 엉뚱한 그림이 나옴 (2026-10-19 기사) → 4의 배수면 아틀라스에서 빠지고 그림마다 따로 압축됨. 발끝(아래) · 왼쪽 기준은 그대로"""
    for f in os.listdir(out):
        if not f.endswith('.png'): continue
        p = os.path.join(out, f); im = Image.open(p)
        w, h = im.size; W, H = (w + 3) // 4 * 4, (h + 3) // 4 * 4
        if (W, H) == (w, h): continue
        im = im.convert('RGBA'); c = Image.new('RGBA', (W, H), (0, 0, 0, 0)); c.paste(im, (0, H - h)); c.save(p)


def blur_copy(im, radius, mask=None):
    """흐린 사본 (미리 곱한 색으로 흐려서 투명 쪽이 검게 번지지 않게). mask = 0~1 배열이면 알파에 곱함 (크롭형 마스크 블러)"""
    from PIL import ImageFilter
    a = np.asarray(im.convert('RGBA')).astype(np.float32) / 255
    pm = Image.fromarray((np.dstack([a[..., :3] * a[..., 3:], a[..., 3:]]) * 255).astype(np.uint8), 'RGBA')
    bl = np.asarray(pm.filter(ImageFilter.GaussianBlur(radius))).astype(np.float32) / 255
    al = bl[..., 3] * (mask if mask is not None else 1); col = bl[..., :3] / np.maximum(bl[..., 3:], 1e-4)
    return Image.fromarray((np.dstack([col, al[..., None]]).clip(0, 1) * 255).astype(np.uint8), 'RGBA')


def mask_blur(src, out):
    """story.json maskBlur = {그림 이름: [가운데 x, y, 반지름 x, y (그림 비율), 흐림 px, 어둡게 0~1]} → <이름>_mask
    (크롭형 마스크 블러: 타원 안은 투명 = 선명한 원본이 비침, 바깥으로 갈수록 흐리고 살짝 어두움 → 이펙트 강조, 2026-10-20 치즈 퐁듀: 분수가 터질 때만 켬)"""
    import json
    mb = json.load(open(os.path.join(src, 'story.json'), encoding='utf-8')).get('maskBlur') or {}
    for name, spec in mb.items():
        p = os.path.join(out, name + '.png')
        if not os.path.exists(p): continue
        im = Image.open(p).convert('RGBA'); w, h = im.size
        yy, xx = np.mgrid[0:h, 0:w]
        stages = spec if isinstance(spec[0], list) else [spec]      # 여러 단계 = <이름>_mask1~n (흐림이 가장자리에서 안으로 번져 들어옴, 그림 바꿔 끼우기)
        for k, (cx, cy, rx, ry, rad, dark) in enumerate(stages):
            rr = np.sqrt(((xx - cx * w) / (rx * w)) ** 2 + ((yy - cy * h) / (ry * h)) ** 2)
            m = np.clip((rr - 1) / 0.35, 0, 1) ** 0.8          # 타원 밖으로 빨리 흐려짐 (크롭 느낌)
            a = np.asarray(blur_copy(im, rad, m)).astype(np.float32)
            a[..., :3] *= 1 - dark * m[..., None]
            o = name.replace('_on', '') + '_mask' + (str(k + 1) if len(stages) > 1 else '')
            Image.fromarray(a.clip(0, 255).astype(np.uint8), 'RGBA').save(os.path.join(out, o + '.png')); print(name, '→', o)


def inset_frame(src, out):
    """story.json inset = [폭, 높이] → ui_inset (흰 둥근 테두리, 가운데 투명) · ui_insetbg (가운데 판) — 화면 구석 작은 창 (수어 통역 창 패러디, 2026-10-21 외계인 생쥐 v3)"""
    import json
    from PIL import ImageDraw
    sz = json.load(open(os.path.join(src, 'story.json'), encoding='utf-8')).get('inset')
    if not sz: return
    w, h = sz; r = 28; b = 14
    fr = Image.new('RGBA', (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(fr)
    d.rounded_rectangle((0, 0, w - 1, h - 1), r, fill=(255, 255, 255, 255)); d.rounded_rectangle((b, b, w - 1 - b, h - 1 - b), r - b // 2, fill=(0, 0, 0, 0))
    fr.save(os.path.join(out, 'ui_inset.png'))
    bg = Image.new('RGBA', (w, h), (0, 0, 0, 0)); ImageDraw.Draw(bg).rounded_rectangle((b // 2, b // 2, w - 1 - b // 2, h - 1 - b // 2), r, fill=(206, 222, 238, 255))
    bg.save(os.path.join(out, 'ui_insetbg.png')); print('ui_inset', sz)


def soft_blur(src, out):
    """story.json softBlur = {그림 이름: 흐림 px} → <이름>_soft (살짝 포커스 아웃된 배경 판, 2026-10-21 외계인 생쥐 v3: 건물 뒤 떠드는 쥐들)"""
    import json
    from PIL import ImageFilter
    for name, rad in (json.load(open(os.path.join(src, 'story.json'), encoding='utf-8')).get('softBlur') or {}).items():
        p = os.path.join(out, name + '.png')
        if os.path.exists(p): Image.open(p).convert('RGB').filter(ImageFilter.GaussianBlur(rad)).save(os.path.join(out, name + '_soft.png')); print(name, '→', name + '_soft')


def zoom_blur(src, out):
    """story.json zoomBlur = {그림 이름: [[가운데 x, y (그림 비율), 세기, 선명한 반지름 x, y], ...]} → <이름>_zoom1~n
    (사자후식 방사형 줌 블러: 폭발 점에서 바깥으로 뻗는 흐림 · 가운데는 선명 · 바깥일수록 길게, 2026-10-20 치즈 퐁듀)"""
    import json
    zb = json.load(open(os.path.join(src, 'story.json'), encoding='utf-8')).get('zoomBlur') or {}
    for name, stages in zb.items():
        p = os.path.join(out, name + '.png')
        if not os.path.exists(p): continue
        im = Image.open(p).convert('RGB'); w, h = im.size
        yy, xx = np.mgrid[0:h, 0:w]
        for k, (cx, cy, amt, rx, ry) in enumerate(stages):
            acc = np.zeros((h, w, 3), np.float32); n = 28
            for i in range(n):                                     # 폭발 점을 축으로 조금씩 키운 사본을 겹침 = 바깥으로 뻗는 흐림
                sc = 1 + amt * i / (n - 1); W2, H2 = int(w * sc), int(h * sc)
                big = im.resize((W2, H2), Image.BILINEAR); ox, oy = int(cx * w * (sc - 1)), int(cy * h * (sc - 1))
                acc += np.asarray(big.crop((ox, oy, ox + w, oy + h))).astype(np.float32)
            acc /= n
            rr = np.sqrt(((xx - cx * w) / (rx * w)) ** 2 + ((yy - cy * h) / (ry * h)) ** 2)
            m = np.clip((rr - 1) / 0.5, 0, 1)
            a = np.dstack([acc, m * 255]).clip(0, 255).astype(np.uint8)
            o = name.replace('_on', '') + '_zoom' + str(k + 1)
            Image.fromarray(a, 'RGBA').save(os.path.join(out, o + '.png')); print(name, '→', o)


def motion_blur(src, out):
    """story.json motionBlur = {그림 이름: 흐림 길이 px | [길이, "v"]} → <이름>_mblur (휙 지나가는 블러 사본: 컷 전환 때 원본 위에 겹침, "v" = 세로 — 카메라가 위아래로 휙, 2026-10-20 슈퍼 요리사)"""
    import json
    mb = json.load(open(os.path.join(src, 'story.json'), encoding='utf-8')).get('motionBlur') or {}
    for name, length in mb.items():
        axis = 0 if isinstance(length, list) and 'v' in length[1:] else 1
        if isinstance(length, list): length = length[0]
        p = os.path.join(out, name + '.png')
        if not os.path.exists(p): continue
        src_im = Image.open(p); rgba = src_im.mode == 'RGBA'          # 투명 그림(잘라낸 캐릭터) = 미리 곱한 색으로 흐림 (가장자리가 검게 번지지 않게)
        a = np.asarray(src_im.convert('RGBA' if rgba else 'RGB')).astype(np.float32)
        if rgba: a[..., :3] *= a[..., 3:] / 255
        if rgba:                                                        # 흐림이 그림 밖으로 번질 자리
            pw = [(0, 0), (0, 0), (0, 0)]; pw[axis] = (length // 2 + 4, length // 2 + 4); a = np.pad(a, pw)
        n = 24; acc = np.zeros_like(a)
        for i in range(n):
            dx = int(round((i / (n - 1) - 0.5) * length))
            m = abs(dx); pw = [(0, 0), (0, 0), (0, 0)]; pw[axis] = (m, m)     # 가장자리는 늘여 채움 (반대쪽이 감겨 들어오지 않게)
            b = np.pad(a, pw, mode='constant' if rgba else 'edge'); n_ = a.shape[axis]
            acc += b[m - dx:m - dx + n_] if axis == 0 else b[:, m - dx:m - dx + n_]
        acc /= n
        if rgba: acc[..., :3] = acc[..., :3] / np.maximum(acc[..., 3:], 1) * 255
        Image.fromarray(acc.clip(0, 255).astype(np.uint8), 'RGBA' if rgba else 'RGB').save(os.path.join(out, name + '_mblur.png')); print(name, '→', name + '_mblur')


def rig_parts(code, src, out):
    """컷신 리그 고해상도 파츠 (2026-10-19, rig_torso · rig_head · rig_legs(앞 | 뒤) · rig_tail · rig_cape) → rig_*.png + rig.json
    rig.json = 게임 파츠(Parts/<code>)와 같은 비율이 되게 하는 크기 보정 · 피벗(게임 파츠 meta) · 관절(RatRigMeta)"""
    import json
    if not os.path.exists(os.path.join(src, 'rig_torso.png')): return
    got = {}
    for n in ('torso', 'head', 'tail', 'cape'):
        im = trim(despill(key_magenta(Image.open(os.path.join(src, f'rig_{n}.png')))))
        im.save(os.path.join(out, f'rig_{n}.png')); got[n] = im.size; print('rig_' + n, im.size)
    im = despill(key_magenta(Image.open(os.path.join(src, 'rig_legs.png')))); W = im.width
    for n, box in (('front', (0, 0, W // 2, im.height)), ('back', (W // 2, 0, W, im.height))):
        part = trim(im.crop(box)); part.save(os.path.join(out, f'rig_{n}.png')); got[n] = part.size; print('rig_' + n, part.size)
    art = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats', 'Parts', code)
    meta = [m for m in json.load(open(os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Data', 'RatRigMeta.json'), encoding='utf-8'))['items'] if m['code_id'] == code][0]
    old = {n: Image.open(os.path.join(art, n + '.png')).size for n in ('torso', 'head', 'front', 'back', 'tail')}
    def pivot(n):           # 게임 파츠 meta (왼쪽 아래 기준) → 왼쪽 위 기준
        t = open(os.path.join(art, n + '.png.meta'), encoding='utf-8').read()
        import re
        m = re.search(r'spritePivot: \{x: ([\d.]+), y: ([\d.]+)\}', t); return [float(m.group(1)), round(1 - float(m.group(2)), 3)]
    rel = lambda n: (old[n][0] / old['torso'][0]) / (got[n][0] / got['torso'][0])      # 게임 비율 유지 배율
    head_game = min(1, 0.62 * old['torso'][0] / old['head'][0])                         # RatRig.headWidthRatio
    rig = dict(neck=meta['neck'], shoulder=meta['shoulder'], hip=meta['hip'], tailRoot=meta['tail'],
               headPivot=pivot('head'), frontPivot=pivot('front'), backPivot=pivot('back'), tailPivot=pivot('tail'),
               headScale=round(rel('head') * head_game, 3), frontScale=round(rel('front') * meta['leg_front'], 3), backScale=round(rel('back') * meta['leg_back'], 3),
               tailScale=round(rel('tail'), 3), capeScale=round(1.05 * got['torso'][0] / got['cape'][0], 3))
    json.dump(rig, open(os.path.join(out, 'rig.json'), 'w'), indent=1); print('rig.json', rig)


if __name__ == '__main__':
    main(sys.argv[1])
