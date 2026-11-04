"""로비 배경 소품 분리 뒷처리: props.png 키잉 → 소품 4종으로 나눠 자름 + 자리(픽셀 상자) json + 확인 그림
사용: python Tools/lobby_split_cut.py <작업 폴더> [<이름 접두어>] [green]   (green = 소품 층 바탕이 초록 #00FF00 — 네온처럼 소품에 분홍·보라가 많을 때)
결과: <폴더>/out/<접두어>_bed.png · _table.png · _books.png · _shelf.png · <접두어>_empty.png · rects.json · check.png(원본 | 빈 배경 + 소품 | 차이)
소품 나누기 = 픽셀 x 위치로 (침대 < 0.205 · 탁자 < 0.49 · 책 < 0.8 · 그 뒤 = 치즈 선반), 작은 찌꺼기는 열기(min→max)로 지움"""
import json, os, sys
import numpy as np
from PIL import Image, ImageChops
from PIL import ImageFilter
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta

NAMES = [('bed', 0.205), ('table', 0.49), ('books', 0.8), ('shelf', 9)]


def key_green(im):
    # key_magenta 의 초록판 (초록(0,255,0)에서 멀수록 불투명 · 가장자리 초록 빼기)
    a = np.array(im.convert('RGB')).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    dist = np.sqrt(r ** 2 + (g - 255) ** 2 + b ** 2)
    alpha = np.clip((dist - 60) / 60, 0, 1)
    k = 1 - alpha
    a[..., 1] = np.clip((g - 255 * k) / np.maximum(alpha, 1e-3), 0, 255)
    out = np.dstack([a, alpha * 255]).astype(np.uint8)
    out[alpha <= 0.02] = 0
    return Image.fromarray(out, 'RGBA')


def main(d, pre, green=False):
    base = Image.open(os.path.join(d, 'base.png')).convert('RGBA')
    W, H = base.size
    props = (key_green if green else key_magenta)(Image.open(os.path.join(d, 'props.png')).resize((W, H), Image.LANCZOS))
    empty = Image.open(os.path.join(d, 'empty.png')).convert('RGBA').resize((W, H), Image.LANCZOS)
    am = props.getchannel('A').point(lambda v: 255 if v > 20 else 0)
    am = am.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.MaxFilter(5))      # 찌꺼기 지우기
    solid = np.array(am) > 0
    xs_all = np.arange(W)[None, :].repeat(H, 0) / W
    groups, lo = {}, 0
    for k, lim in NAMES:
        groups[k] = solid & (xs_all >= lo) & (xs_all < lim); lo = lim
    out = os.path.join(d, 'out'); os.makedirs(out, exist_ok=True)
    pa = np.array(props)
    rects = {}
    for k, m in groups.items():
        ys, xs = np.where(m)
        if len(xs) == 0: print('없음', k); continue
        x0, x1, y0, y1 = max(0, xs.min() - 2), min(W, xs.max() + 3), max(0, ys.min() - 2), min(H, ys.max() + 3)
        dil = np.array(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))) > 0
        cut = pa.copy(); cut[~dil] = 0
        Image.fromarray(cut).crop((x0, y0, x1, y1)).save(os.path.join(out, f'{pre}_{k}.png'))
        rects[k] = [int(x0), int(y0), int(x1), int(y1)]
    empty.convert('RGB').save(os.path.join(out, f'{pre}_empty.png'))
    json.dump(dict(size=[W, H], rects=rects), open(os.path.join(out, 'rects.json'), 'w'), indent=1)
    comp = empty.copy(); comp.alpha_composite(props)
    diff = ImageChops.difference(base.convert('RGB'), comp.convert('RGB')).convert('L').point(lambda v: min(255, v * 3))
    sheet = Image.new('RGB', (W * 3 // 2, H // 2), 'white')
    for i, im in enumerate([base, comp, diff.convert('RGB')]):
        sheet.paste(im.convert('RGB').resize((W // 2, H // 2)), (i * W // 2, 0))
    sheet.save(os.path.join(out, 'check.png'))
    print(pre, rects)


if __name__ == '__main__':
    d = sys.argv[1]
    main(d, sys.argv[2] if len(sys.argv) > 2 else os.path.basename(os.path.normpath(d)), 'green' in sys.argv[3:])
