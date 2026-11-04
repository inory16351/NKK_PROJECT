# 시트 f (고친 7개) · g (40306 다시) — 사용: python slice_f.py [f|g]: 메달이 칸보다 커서 격자 대신 덩어리로 자름 (큰 덩어리 7개를 줄 · 왼쪽 순서로) → ../_new/ach_<id>.png
import os, sys
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "_new"); SIZE = 256
K = sys.argv[1] if len(sys.argv) > 1 else "f"
names = open(os.path.join(HERE, f"names_{K}.txt"), encoding="utf-8").read().strip().split(",")
a = np.array(Image.open(os.path.join(HERE, f"achv_icons_{K}.png")).convert("RGBA"))
a[..., 3] = np.where(a[..., 3] < 40, 0, a[..., 3])          # 옅은 번짐 지움
lab, n = ndimage.label(ndimage.binary_dilation(a[..., 3] > 20, iterations=2))
sizes = ndimage.sum(a[..., 3] > 20, lab, range(1, n + 1))
big = [i + 1 for i in np.argsort(sizes)[::-1][:len(names)]]
objs = ndimage.find_objects(lab); boxes = [objs[b - 1] for b in big]
order = sorted(zip(big, boxes), key=lambda t: (round(t[1][0].start / 300), t[1][1].start))
for name, (b, (sy, sx)) in zip(names, order):
    cell = a[sy, sx].copy(); cell[..., 3] = np.where(lab[sy, sx] == b, cell[..., 3], 0)
    part = Image.fromarray(cell); k = SIZE * 0.88 / max(part.size)
    part = part.resize((round(part.width * k), round(part.height * k)), Image.LANCZOS)
    cv = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0)); cv.paste(part, ((SIZE - part.width) // 2, (SIZE - part.height) // 2), part)
    cv.save(os.path.join(OUT, name + ".png")); print(name, part.size)
