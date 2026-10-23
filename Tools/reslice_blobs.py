"""칸 경계에서 잘린 그림 다시 자르기 (2026-10-23).
시트를 칸으로 딱 자르면 칸 밖으로 삐져나온 부분이 잘리고 옆 칸 조각이 묻음 →
시트 전체에서 덩어리를 찾고, 덩어리 '가운데'가 들어 있는 칸의 그림으로 통째로 묶어 자름.
사용: python Tools/reslice_blobs.py <시트.png> <열> <행> <칸번호=저장경로[,저장경로2]> ...
  칸번호 = 왼쪽 위부터 0, 저장경로 여러 개면 같은 그림을 모두에 저장 (UnityResources · Assets/Art)
  --key=magenta(기본)|alpha  배경 처리 (alpha = 이미 투명 시트)
  --join=6(기본)  가까운 조각을 잇는 거리, 위아래 그림이 거의 붙어 있으면 0
  잘라 본 뒤 크기가 이상하면(두 그림이 묶임) --join 을 줄여서 다시"""
import os, sys
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta


def blobs_by_cell(im, cols, rows, join=6, min_frac=0.03):
    a = np.array(im)[..., 3] > 8
    lab, n = ndimage.label(ndimage.binary_dilation(a, iterations=join) if join > 0 else a)
    lab = np.where(a, lab, 0)
    H, W = a.shape
    cells = {}
    for k, sl in enumerate(ndimage.find_objects(lab), 1):
        if sl is None: continue
        m = lab[sl] == k
        size = int(m.sum())
        ys, xs = np.nonzero(m)
        cy, cx = sl[0].start + ys.mean(), sl[1].start + xs.mean()
        c, r = min(cols - 1, int(cx * cols / W)), min(rows - 1, int(cy * rows / H))
        cells.setdefault(r * cols + c, []).append((size, k, sl))
    out = {}
    for i, lst in cells.items():
        big = max(s for s, _, _ in lst)
        keep = [(k, sl) for s, k, sl in lst if s >= big * min_frac]
        out[i] = keep
    return lab, out


def cut(im, lab, keep, pad=6):
    arr = np.array(im)
    mask = np.isin(lab, [k for k, _ in keep])
    y0 = min(sl[0].start for _, sl in keep); y1 = max(sl[0].stop for _, sl in keep)
    x0 = min(sl[1].start for _, sl in keep); x1 = max(sl[1].stop for _, sl in keep)
    arr = arr.copy(); arr[..., 3] = np.where(mask, arr[..., 3], 0)
    H, W = mask.shape
    return Image.fromarray(arr).crop((max(0, x0 - pad), max(0, y0 - pad), min(W, x1 + pad), min(H, y1 + pad)))


def main(argv):
    key, join = 'magenta', 6
    args = [a for a in argv if not a.startswith('--')]
    for a in argv:
        if a.startswith('--key='): key = a.split('=', 1)[1]
        if a.startswith('--join='): join = int(a.split('=', 1)[1])      # 위아래 그림이 가까우면 0 (붙어서 한 덩어리가 됨)
    sheet, cols, rows = args[0], int(args[1]), int(args[2])
    im = Image.open(sheet).convert('RGBA')
    if key == 'magenta': im = key_magenta(im)
    lab, cells = blobs_by_cell(im, cols, rows, join)
    for spec in args[3:]:
        i, paths = spec.split('=', 1)
        keep = cells.get(int(i))
        if not keep: print('빈 칸', i); continue
        p = cut(im, lab, keep)
        for path in paths.split(','):
            os.makedirs(os.path.dirname(path), exist_ok=True); p.save(path)
        print(i, os.path.basename(paths.split(',')[0]), p.size)


if __name__ == '__main__':
    main(sys.argv[1:])
