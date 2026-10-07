# Codex 시트(격자) → 칸별 투명 PNG 로 자르기
# 사용: python Tools/slice_sheet.py <시트.png> <열> <행> <출력폴더> <이름1,이름2,...> [--keep-cell] [--blobs] [--pad N]
#   이름이 '-' 이면 그 칸은 건너뜀
#   --keep-cell : 칸 크기 그대로 저장 (애니메이션 프레임처럼 중심을 맞춰야 할 때)
#   --blobs     : 그림이 칸 경계를 넘어도 됨. 그림 덩어리(연결 영역)의 중심이 있는 칸으로 나눔
#                 → 옆 칸 그림 조각이 묻어 들어오거나 잘리지 않음 (아이콘 시트용)
#   기본        : 칸 안의 그림 테두리(알파 기준)만 남기고 여백 pad 픽셀
import os
import sys
from collections import deque

import numpy as np
from PIL import Image


def blob_masks(im, cols, rows, gap=6):
    """알파 덩어리를 찾아 중심이 속한 칸별 마스크(원본 크기 bool 배열)를 돌려줌.
    gap: 이 픽셀 거리 안의 조각(반짝이 등)은 같은 덩어리로 묶음 (축소 격자 기준 근사)"""
    W, H = im.size
    s = 4                                        # 4배 축소 격자에서 연결 영역 계산
    a = np.array(im.getchannel('A').resize((W // s, H // s), Image.BOX)) > 10
    h, w = a.shape
    r = max(1, gap // s)
    d = a.copy()                                 # 팽창: 가까운 조각끼리 붙게
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            d[max(0, dy):h + min(0, dy), max(0, dx):w + min(0, dx)] |= a[max(0, -dy):h + min(0, -dy), max(0, -dx):w + min(0, -dx)]
    lab = np.zeros((h, w), np.int32)
    n = 0
    for y0 in range(h):
        for x0 in range(w):
            if not d[y0, x0] or lab[y0, x0]:
                continue
            n += 1
            q = deque([(y0, x0)])
            lab[y0, x0] = n
            while q:
                y, x = q.popleft()
                for yy, xx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                    if 0 <= yy < h and 0 <= xx < w and d[yy, xx] and not lab[yy, xx]:
                        lab[yy, xx] = n
                        q.append((yy, xx))
    big = np.kron(lab, np.ones((s, s), np.int32))
    full = np.zeros((H, W), np.int32)
    full[:big.shape[0], :big.shape[1]] = big
    gy, gx = np.mgrid[0:H, 0:W]
    grid = np.minimum(rows - 1, (gy / (H / rows)).astype(int)) * cols + np.minimum(cols - 1, (gx / (W / cols)).astype(int))
    masks = {}
    for k in range(1, n + 1):
        ys, xs = np.nonzero((lab == k) & a)      # 실제 그림 픽셀
        if len(xs) == 0:
            continue
        cells = (np.minimum(rows - 1, (ys * s / (H / rows)).astype(int)) * cols + np.minimum(cols - 1, (xs * s / (W / cols)).astype(int)))
        cnt = np.bincount(cells, minlength=rows * cols)
        m = full == k
        if cnt.max() >= 0.85 * cnt.sum():        # 거의 한 칸 = 덩어리째 그 칸으로 (경계 넘은 부분 포함)
            c = int(cnt.argmax())
            masks.setdefault(c, np.zeros((H, W), bool))
            masks[c] |= m
        else:                                    # 옆 그림과 맞닿아 붙은 덩어리 = 칸 경계로 나눔
            for c in np.nonzero(cnt)[0]:
                masks.setdefault(int(c), np.zeros((H, W), bool))
                masks[int(c)] |= m & (grid == c)
    return masks


def main():
    a = sys.argv[1:]
    keep = '--keep-cell' in a
    blobs = '--blobs' in a
    pad = 8
    if '--pad' in a:
        pad = int(a[a.index('--pad') + 1])
    a = [x for i, x in enumerate(a) if not x.startswith('--') and (i == 0 or a[i - 1] != '--pad')]
    src, cols, rows, out, names = a[0], int(a[1]), int(a[2]), a[3], a[4].split(',')
    im = Image.open(src).convert('RGBA')
    W, H = im.size
    cw, ch = W / cols, H / rows
    os.makedirs(out, exist_ok=True)
    masks = blob_masks(im, cols, rows) if blobs else None
    for j in range(rows):
        for i in range(cols):
            k = j * cols + i
            if k >= len(names) or names[k] == '-':
                continue
            if blobs:
                if k not in masks:
                    print('빈 칸:', names[k])
                    continue
                arr = np.array(im)
                arr[..., 3] = np.where(masks[k], arr[..., 3], 0)
                cell = Image.fromarray(arr)
            else:
                cell = im.crop((round(i * cw), round(j * ch), round((i + 1) * cw), round((j + 1) * ch)))
            if not keep:
                # 거의 투명한 픽셀은 무시하고 테두리 계산
                alpha = cell.getchannel('A').point(lambda v: 255 if v > 8 else 0)
                bb = alpha.getbbox()
                if not bb:
                    print('빈 칸:', names[k])
                    continue
                bb = (max(0, bb[0] - pad), max(0, bb[1] - pad), min(cell.width, bb[2] + pad), min(cell.height, bb[3] + pad))
                cell = cell.crop(bb)
            cell.save(os.path.join(out, names[k] + '.png'))
            print(names[k], cell.size)


if __name__ == '__main__':
    main()
