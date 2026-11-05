"""ui4 sheet.png (Codex, 검은 바탕 네온 간판) → out/<이름>_neon.png (게임 그림과 같은 캔버스 크기)
검은 바탕 + 바깥 빛번짐은 밝기 → 투명도 (빛번짐이 부드럽게 비침), 관 안쪽 (몸통 · 관) 은 불투명."""
import json, os
from collections import deque
import numpy as np
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.join(here, '../../../../../Unity_Making/NKK_Project/Assets/Art/Rats/Deco/neon')
sheet = np.array(Image.open(os.path.join(here, 'sheet.png')).convert('RGB')).astype(np.float32)
L = json.load(open(os.path.join(here, 'layout.json'), encoding='utf-8'))
os.makedirs(os.path.join(here, 'out'), exist_ok=True)
STRETCH = True    # 판자 · pill: 같은 비율로 줄이고 가운데 한 줄만 늘림 (안쪽 글자 칸 넓게) · False = 그냥 캔버스 크기로 줄임
KFIT = 0.75      # 판자 · pill 관 굵기 (1 = 높이에 꽉, 작을수록 안쪽이 넓음)
T = 150          # 이보다 어두운 바깥 = 빛번짐 (밝기 → 투명도)
def stretch(im, axis, cuts, extra):
    """axis 0 = 가로 · 1 = 세로: cuts 위치의 한 줄을 늘려 extra 픽셀 넓힘 (나눠서)"""
    if extra <= 0: return im
    w, h = im.size; n = len(cuts); adds = [extra // n + (1 if i < extra % n else 0) for i in range(n)]
    out = Image.new('RGBA', (w + extra, h) if axis == 0 else (w, h + extra)); src = dst = 0
    for c, ad in zip(cuts + [None], adds + [0]):
        end = (w if axis == 0 else h) if c is None else c
        box = (src, 0, end, h) if axis == 0 else (0, src, w, end)
        out.paste(im.crop(box), (dst, 0) if axis == 0 else (0, dst)); dst += end - src
        if c is not None and ad:
            band = im.crop((c, 0, c + 1, h) if axis == 0 else (0, c, w, c + 1)).resize((ad, h) if axis == 0 else (w, ad), Image.BILINEAR)
            out.paste(band, (dst, 0) if axis == 0 else (0, dst)); dst += ad
        src = end
    return out


for it in L:
    name = os.path.basename(it['src']).replace('.png', '_neon.png')
    x0, y0, x1, y1 = it['cell']
    c = sheet[y0:y1, x0:x1]; v = c.max(axis=2)
    ys, xs = np.where(v > 34); c = c[ys.min():ys.max() + 1, xs.min():xs.max() + 1]; v = c.max(axis=2)
    h, w = v.shape          # (v > 34 = 아주 옅은 빛번짐 끝은 잘라 관을 크게 → 글자 들어갈 안쪽 넓게)
    out = np.zeros((h, w), bool); q = deque()        # 가장자리에서 어두운 곳을 따라 채움 = 바깥
    for y in range(h):
        for x in (0, w - 1): q.append((y, x))
    for x in range(w):
        for y in (0, h - 1): q.append((y, x))
    while q:
        y, x = q.popleft()
        if out[y, x] or v[y, x] >= T: continue
        out[y, x] = True
        if y > 0: q.append((y - 1, x))
        if y < h - 1: q.append((y + 1, x))
        if x > 0: q.append((y, x - 1))
        if x < w - 1: q.append((y, x + 1))
    a = np.where(out, np.clip((v - 6) / (T - 6), 0, 1), 1.0)
    a = np.where(out, a ** 0.9, a)
    rgb = np.where(out[..., None], c / np.maximum(a[..., None], 1e-3), c)
    rgba = np.dstack([np.clip(rgb, 0, 255), a * 255]).astype(np.uint8)
    tw, th = Image.open(os.path.join(GAME, name)).size
    im = Image.fromarray(rgba, 'RGBA')
    # 미리 곱한 색으로 줄여야 가장자리 검은 테가 안 생김
    pm = np.array(im).astype(np.float32); pm[..., :3] *= pm[..., 3:] / 255
    pmi = Image.fromarray(pm.astype(np.uint8), 'RGBA')
    if STRETCH and w / h > 1.6:        # 판자 · pill: 같은 비율로 줄이고 (관이 조금 가늘어짐) 가운데만 가로 · 세로로 늘림 → 모서리 그대로 · 글자 들어갈 안쪽 넓게
        k = th / h * (KFIT if 'plank' in name else 0.9); sw, sh = round(w * k), round(h * k); pmi = pmi.resize((sw, sh), Image.LANCZOS)
        # 가로 = 가운데 한 줄을 늘림, 세로 = 점 위 (30%) · 아래 (70%) 두 줄을 반씩 늘림 → 점은 가운데 그대로
        # 가운데 한 줄씩 늘림 (몸통이 고른 곳 → 띠 무늬 없음). 판자는 점 바로 위 · 아래 두 줄을 반씩 늘려 점이 가운데 그대로
        cx, cy = sw // 2, sh // 2; d = round(sh * 0.12) if 'plank' in name else 0
        pmi = stretch(stretch(pmi, 0, [cx], tw - sw), 1, [cy - d, cy + d] if d else [cy], th - sh)
    else: pmi = pmi.resize((tw, th), Image.LANCZOS)
    r = np.array(pmi).astype(np.float32); al = r[..., 3:] / 255; r[..., :3] = np.where(al > 0, r[..., :3] / np.maximum(al, 1e-3), 0)
    Image.fromarray(np.clip(r, 0, 255).astype(np.uint8), 'RGBA').save(os.path.join(here, 'out', name)); print(name, (w, h), '->', (tw, th))
