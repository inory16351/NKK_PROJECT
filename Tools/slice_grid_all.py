"""자홍 배경 격자 시트 → 칸마다 PNG (칸 안 그림 조각을 전부 남김, 여백만 잘라냄)
사용: python Tools/slice_grid_all.py <시트.png> <열> <행> <출력폴더> <이름,이름,...|-> [정사각 크기 (0 = 원래 크기)]
여러 조각으로 된 아이콘(고드름 3개·신호 고리 등)용. 칸 가장자리 4px 은 버림 (옆 칸 번짐)."""
import os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta


def main():
    sheet, cols, rows, out, names = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5].split(',')
    size = int(sys.argv[6]) if len(sys.argv) > 6 else 0
    os.makedirs(out, exist_ok=True)
    im = key_magenta(Image.open(sheet)); W, H = im.size
    for i, name in enumerate(names):
        if name == '-' or i >= cols * rows: continue
        r, c = divmod(i, cols)
        cell = np.array(im.crop((int(c * W / cols) + 4, int(r * H / rows) + 4, int((c + 1) * W / cols) - 4, int((r + 1) * H / rows) - 4)))
        ys, xs = np.nonzero(cell[..., 3] > 20)
        if len(xs) == 0: print('빈 칸', name); continue
        part = Image.fromarray(cell).crop((max(0, xs.min() - 3), max(0, ys.min() - 3), xs.max() + 4, ys.max() + 4))
        if size:
            k = size * 0.88 / max(part.size)
            part = part.resize((max(1, round(part.width * k)), max(1, round(part.height * k))), Image.LANCZOS)
            canvas = Image.new('RGBA', (size, size), (0, 0, 0, 0))
            canvas.paste(part, ((size - part.width) // 2, (size - part.height) // 2), part)
            part = canvas
        part.save(os.path.join(out, name + '.png'))
        print(name, part.size)


if __name__ == '__main__':
    main()
