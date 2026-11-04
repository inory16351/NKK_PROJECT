# 시트 c · d · e (6×4) → ../_new/ach_<id>.png (256, 가운데) + ../_new/_미리보기_new.png
# 시트 배경이 투명이면 알파 그대로, 아니면 자홍 키잉. 칸 안 가장 큰 덩어리만 남김 (slice_icon_grid 와 같은 방식)
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "..", "Tools"))
from slice_rat_parts import key_magenta

OUT = os.path.join(HERE, "..", "_new")
SIZE, COLS, ROWS = 256, 6, 4


def load(path):
    im = Image.open(path).convert("RGBA")
    a = np.array(im)[..., 3]
    return im if (a < 10).mean() > 0.3 else key_magenta(im)


def slice_sheet(k):
    path = os.path.join(HERE, f"achv_icons_{k}.png")
    if not os.path.exists(path): print("없음", path); return []
    names = open(os.path.join(HERE, f"names_{k}.txt"), encoding="utf-8").read().strip().split(",")
    im = load(path); W, H = im.size; done = []
    for i, name in enumerate(names):
        r, c = divmod(i, COLS)
        cell = np.array(im.crop((int(c * W / COLS), int(r * H / ROWS), int((c + 1) * W / COLS), int((r + 1) * H / ROWS))))
        solid = cell[..., 3] > 20
        lab, n = ndimage.label(ndimage.binary_dilation(solid, iterations=6))
        if n == 0: print("빈 칸", name); continue
        big = 1 + int(np.argmax(ndimage.sum(solid, lab, range(1, n + 1))))
        cell[..., 3] = np.where(lab == big, cell[..., 3], 0)
        cell[..., 3] = np.where(cell[..., 3] < 40, 0, cell[..., 3])      # 옅은 번짐 지움
        ys, xs = np.nonzero(cell[..., 3] > 20)
        part = Image.fromarray(cell).crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
        k2 = SIZE * 0.88 / max(part.size)
        part = part.resize((max(1, round(part.width * k2)), max(1, round(part.height * k2))), Image.LANCZOS)
        canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
        canvas.paste(part, ((SIZE - part.width) // 2, (SIZE - part.height) // 2), part)
        canvas.save(os.path.join(OUT, name + ".png")); done.append(name)
    print(k, len(done)); return done


def preview(names):
    # 확인용 한 장: 크림 바탕 + 칸 아래 업적 id
    cols = 12; cw, ch = 140, 160
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cw, rows * ch), (240, 230, 214))
    d = ImageDraw.Draw(sheet)
    try: f = ImageFont.truetype("malgun.ttf", 15)
    except OSError: f = ImageFont.load_default()
    for i, n in enumerate(names):
        r, c = divmod(i, cols)
        ic = Image.open(os.path.join(OUT, n + ".png")).resize((124, 124), Image.LANCZOS)
        sheet.paste(ic, (c * cw + 8, r * ch + 4), ic)
        t = n.replace("ach_", ""); w = d.textlength(t, font=f)
        d.text((c * cw + (cw - w) / 2, r * ch + 134), t, fill=(90, 70, 50), font=f)
    p = os.path.join(OUT, "_미리보기_new.png"); sheet.save(p); print("미리보기", p)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    names = []
    for k in (sys.argv[1:] or ["c", "d", "e"]): names += slice_sheet(k)
    if names: preview(sorted(set(names)))      # 시트 f = 고친 7개 (c · d · e 뒤에 덮어씀)
