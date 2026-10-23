"""증강 기획서 Ver.04 그림 자르기 (2026-10-23, 시트 = Tools/run_aug_v04_codex.sh).
- aug_icons_e (4x3) → UnityResources/Rats/Augment/aug_*.png + Assets/Art/Augment/
- aug_fx (3x3) → UnityResources/Rats/Augment/fx/<스티커>.png + Assets/Art/FX/aug/ (FxManager 스티커 이름 = 파일 이름)
- rat_suit (1x5) → python Tools/slice_rat_parts.py 로 Parts/skater_suit (스케이터 생쥐 통일 그림)
사용: python Tools/slice_aug_v04.py"""
import os, shutil, subprocess, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, 'UnityResources', 'Rats', 'Augment')
SHEETS = os.path.join(RES, 'Sheets', 'v04')
ART = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art')

ICONS = ['aug_demolish', 'aug_jaw', 'aug_tower', 'aug_cheeserain', 'aug_clones', 'aug_train', 'aug_u_hamster', 'aug_u_panda', 'aug_u_skater', 'aug_u_burger', 'aug_u_fiveg']
FX = ['storm_cloud', 'cheese_cloud', 'scorch', 'toy_train', 'magic_circle', 'garland', 'footprint', 'slot_machine', 'signal_5g']


def cells(im, cols, rows):
    W, H = im.size
    for r in range(rows):
        for c in range(cols):
            yield im.crop((c * W // cols, r * H // rows, (c + 1) * W // cols, (r + 1) * H // rows))


def trim(im, pad=6):
    # 칸 가장자리에 묻은 옆 칸 조각은 버림: 큰 덩어리(가장 큰 것의 4% 이상)만 남김
    from scipy import ndimage
    arr = np.array(im)
    lab, n = ndimage.label(ndimage.binary_dilation(arr[..., 3] > 8, iterations=6))
    if n > 1:
        sizes = ndimage.sum(arr[..., 3] > 8, lab, range(1, n + 1))
        keep = [k + 1 for k, s in enumerate(sizes) if s >= sizes.max() * 0.04]
        arr[..., 3] = np.where(np.isin(lab, keep), arr[..., 3], 0)
        im = Image.fromarray(arr)
    a = np.array(im)[..., 3]
    ys, xs = np.nonzero(a > 8)
    if len(xs) == 0: return None
    return im.crop((max(0, xs.min() - pad), max(0, ys.min() - pad), min(im.width, xs.max() + pad + 1), min(im.height, ys.max() + pad + 1)))


def save(im, name, res_dir, art_dir):
    os.makedirs(res_dir, exist_ok=True); os.makedirs(art_dir, exist_ok=True)
    p = os.path.join(res_dir, name + '.png'); im.save(p)
    shutil.copyfile(p, os.path.join(art_dir, name + '.png'))
    print(name, im.size)


def main():
    s = os.path.join(SHEETS, 'aug_icons_e.png')
    if os.path.exists(s):
        im = key_magenta(Image.open(s))
        for name, c in zip(ICONS, cells(im, 4, 3)):
            t = trim(c)
            if t: save(t, name, RES, os.path.join(ART, 'Augment'))
    s = os.path.join(SHEETS, 'aug_fx.png')
    if os.path.exists(s):
        im = key_magenta(Image.open(s))
        for name, c in zip(FX, cells(im, 3, 3)):
            t = trim(c)
            if t: save(t, name, os.path.join(RES, 'fx'), os.path.join(ART, 'FX', 'aug'))
    s = os.path.join(SHEETS, 'rat_suit.png')
    if os.path.exists(s):
        subprocess.check_call([sys.executable, '-I', os.path.join(ROOT, 'Tools', 'slice_rat_parts.py'), s, '1', 'skater_suit'])
        src = os.path.join(ROOT, 'UnityResources', 'Rats', 'Parts', 'skater_suit'); dst = os.path.join(ART, 'Rats', 'Parts', 'skater_suit')
        os.makedirs(dst, exist_ok=True)
        for f in os.listdir(src): shutil.copyfile(os.path.join(src, f), os.path.join(dst, f))
        print('skater_suit →', dst)


if __name__ == '__main__':
    main()
