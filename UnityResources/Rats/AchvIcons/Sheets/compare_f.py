# 수정 7개 전 · 후 비교 한 장 → ../_new/_비교_수정7.png (윗줄 = 전 _old_v1, 아랫줄 = 후)
import os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
NEW = os.path.join(HERE, "..", "_new")
names = open(os.path.join(HERE, "names_f.txt"), encoding="utf-8").read().strip().split(",")
cw, ch, lw = 180, 200, 60
sheet = Image.new("RGB", (lw + cw * len(names), ch * 2 + 30), (240, 230, 214))
d = ImageDraw.Draw(sheet)
f = ImageFont.truetype("malgun.ttf", 16)
for row, (label, folder) in enumerate([("전", os.path.join(NEW, "_old_v1")), ("후", NEW)]):
    d.text((18, 30 + row * ch + 80), label, fill=(90, 70, 50), font=f)
    for i, n in enumerate(names):
        ic = Image.open(os.path.join(folder, n + ".png")).resize((168, 168), Image.LANCZOS)
        sheet.paste(ic, (lw + i * cw + 6, 30 + row * ch + 6), ic)
for i, n in enumerate(names):
    t = n.replace("ach_", ""); w = d.textlength(t, font=f)
    d.text((lw + i * cw + (cw - w) / 2, 6), t, fill=(90, 70, 50), font=f)
p = os.path.join(NEW, "_비교_수정7.png"); sheet.save(p); print(p)
