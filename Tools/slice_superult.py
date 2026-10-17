"""초필살기 레이어 컷신 그림 자르기 (2026-10-17): UnityResources/Rats/SuperUlt/<code>/Sheets → <code>/ (PNG)
- bg_*.png · pano_on/off.png · *_face_*.png = 그대로 복사 (pano_a~c 조각은 건너뜀)
- *_pose_*.png          = 자홍 키잉 + 여백 자르기 (캐릭터 한 장)
- fx_*.png              = 검은 배경 빛 효과 격자 (FX_GRID 의 열·행·이름) → 밝기 = 투명도
- props.png             = 자홍 키잉 후 덩어리마다 따로 (PROPS 이름, 왼쪽 위부터 읽는 순서 아님 → 크기·위치로 이름 붙임, 아래 표)
사용: python Tools/slice_superult.py <code>   →  다 자른 뒤 Assets/Art/Rats/SuperUlt/<code>/ 로 복사"""
import os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage
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
    for f in sorted(os.listdir(src)):
        p = os.path.join(src, f)
        if not f.endswith('.png') or f.startswith('ref_'): continue
        if f in ('pano_on.png', 'pano_off.png') or '_face_' in f:       # 세로 파노라마 (조각 pano_a~c 를 stitch_vertical 로 붙인 것) · 전체 화면 클로즈업
            shutil.copy(p, os.path.join(out, f)); print(f, '그대로')
        elif f.startswith('pano_'): continue
        elif code != 'zapham' and (f.startswith('pose_') or f.startswith('face_')):
            im = Image.open(p)
            if f.startswith('pose_'): im = trim(despill(key_magenta(im)))
            im.save(os.path.join(out, f)); print(f, im.size)
        elif code != 'zapham' and f == 'fx.png':
            grid(p, GRID_FX, key_black, out, 'fx_')
        elif code != 'zapham' and f == 'props.png':
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
    dst = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats', 'SuperUlt', code)
    os.makedirs(dst, exist_ok=True)
    for f in os.listdir(out):
        if f.endswith('.png'): shutil.copy(os.path.join(out, f), os.path.join(dst, f))
    print('→', dst)


if __name__ == '__main__':
    main(sys.argv[1])
