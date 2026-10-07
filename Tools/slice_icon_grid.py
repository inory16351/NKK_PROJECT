"""자홍 배경 아이콘 시트(격자) → 칸마다 PNG (정사각, 가운데 정렬)
사용: python Tools/slice_icon_grid.py <시트.png> <열> <행> <출력폴더> <이름,이름,...|-> [크기=256]
칸 안에서 가장 큰 그림 덩어리(가까운 조각 포함)만 남겨 떨어진 부스러기는 버림."""
import os, sys
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta


def main():
    sheet, cols, rows, out, names = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5].split(',')
    size = int(sys.argv[6]) if len(sys.argv) > 6 else 256
    os.makedirs(out, exist_ok=True)
    im = key_magenta(Image.open(sheet)); W, H = im.size
    for i, name in enumerate(names):
        if name == '-' or i >= cols * rows: continue
        r, c = divmod(i, cols)
        cell = np.array(im.crop((int(c * W / cols), int(r * H / rows), int((c + 1) * W / cols), int((r + 1) * H / rows))))
        solid = cell[..., 3] > 20
        lab, n = ndimage.label(ndimage.binary_dilation(solid, iterations=6))
        if n == 0: print('빈 칸', name); continue
        big = 1 + int(np.argmax(ndimage.sum(solid, lab, range(1, n + 1))))
        cell[..., 3] = np.where(lab == big, cell[..., 3], 0)
        ys, xs = np.nonzero(cell[..., 3] > 20)
        part = Image.fromarray(cell).crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
        k = size * 0.88 / max(part.size)
        part = part.resize((max(1, round(part.width * k)), max(1, round(part.height * k))), Image.LANCZOS)
        canvas = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        canvas.paste(part, ((size - part.width) // 2, (size - part.height) // 2), part)
        canvas.save(os.path.join(out, name + '.png'))
    print('완료', out)


if __name__ == '__main__':
    main()
