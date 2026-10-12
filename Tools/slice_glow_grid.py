"""검은 배경 빛 효과 시트(격자) → 칸마다 PNG. 밝기 = 투명도 (빛 번짐이 자연스럽게 남음), 색은 투명도로 나눠 되돌림.
사용: python Tools/slice_glow_grid.py <시트.png> <열> <행> <출력폴더> <이름,이름,...|->
코드로 그리는 게 아니라 생성된 그림을 키잉만 함 (자홍 키잉 key_magenta 와 같은 역할)."""
import os, sys
import numpy as np
from PIL import Image


def key_black(cell):
    a = np.asarray(cell.convert('RGB')).astype(np.float32) / 255
    alpha = a.max(axis=2)
    alpha = np.clip((alpha - 0.03) / 0.97, 0, 1)            # 거의 검은 바탕 잡티 제거
    rgb = np.clip(a / np.maximum(alpha[..., None], 1e-3), 0, 1)
    out = np.dstack([rgb, alpha]) * 255
    return Image.fromarray(out.astype(np.uint8), 'RGBA')


def main():
    sheet, cols, rows, out, names = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5].split(',')
    os.makedirs(out, exist_ok=True)
    im = Image.open(sheet); W, H = im.size
    for i, name in enumerate(names):
        if name == '-' or i >= cols * rows: continue
        r, c = divmod(i, cols)
        cell = key_black(im.crop((int(c * W / cols) + 3, int(r * H / rows) + 3, int((c + 1) * W / cols) - 3, int((r + 1) * H / rows) - 3)))
        a = np.asarray(cell)[..., 3]
        ys, xs = np.nonzero(a > 6)
        if len(xs) == 0: print('빈 칸', name); continue
        part = cell.crop((max(0, xs.min() - 4), max(0, ys.min() - 4), min(cell.width, xs.max() + 5), min(cell.height, ys.max() + 5)))
        part.save(os.path.join(out, name + '.png'))
        print(name, part.size)


if __name__ == '__main__':
    main()
