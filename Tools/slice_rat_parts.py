"""Codex 쥐 파츠 시트(자홍 배경, 5열: 머리·몸통·꼬리·앞다리·뒷다리 / 한 줄 = 한 종) → 파츠 PNG + pivots.json 갱신
사용: python Tools/slice_rat_parts.py <시트.png> <행수> <종id,종id,...|-> [--props 행번호 이름,이름,...]
  종 id 가 '-' 인 행은 건너뜀. --props 는 그 행을 소품으로 잘라 UnityResources/Rats/NewRats/Props/ 에 저장
관절: 다리 = 윗면 가운데, 꼬리 = 왼쪽 끝, 머리 = 아래쪽 목 단면 가운데, 몸통 부착점 = 기존 쥐들 중앙값.
다리 배율: 앞발·뒷발 발끝이 같은 바닥선에 닿게 (앞발 1 기준)."""
import io, json, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, 'UnityResources', 'Rats')
PARTS = ['head', 'torso', 'tail', 'front', 'back']
ANCHOR = {'neck': [0.069, 0.395], 'tail': [0.944, 0.531], 'shoulder': [0.238, 0.448], 'hip': [0.736, 0.325]}   # 기존 쥐 중앙값


def key_magenta(im):
    a = np.array(im.convert('RGB')).astype(np.int32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    # 자홍(255,0,255)에서 멀수록 불투명. 가장자리는 부드럽게
    dist = np.sqrt((r - 255) ** 2 + g ** 2 + (b - 255) ** 2)
    alpha = np.clip((dist - 60) / 60, 0, 1)
    # 자홍이 섞인 가장자리 색 되돌리기 (탈색)
    k = 1 - alpha
    rgb = a.astype(np.float32)
    rgb[..., 0] = np.clip((rgb[..., 0] - 255 * k) / np.maximum(alpha, 1e-3), 0, 255)
    rgb[..., 2] = np.clip((rgb[..., 2] - 255 * k) / np.maximum(alpha, 1e-3), 0, 255)
    out = np.dstack([rgb, alpha * 255]).astype(np.uint8)
    out[alpha <= 0.02] = 0
    return Image.fromarray(out, 'RGBA')


def crop(cell, pad=4):
    a = np.array(cell.getchannel('A')) > 20
    ys, xs = np.where(a)
    if len(xs) == 0: return None
    x0, x1, y0, y1 = max(0, xs.min() - pad), min(cell.width, xs.max() + 1 + pad), max(0, ys.min() - pad), min(cell.height, ys.max() + 1 + pad)
    return cell.crop((x0, y0, x1, y1))


def leg_pivot(im):
    a = np.array(im.getchannel('A')) > 20
    ys, xs = np.where(a)
    band = ys < ys.min() + max(3, int(im.height * 0.06))
    return [round(float(xs[band].mean()) / im.width, 3), round(float(ys.min() + im.height * 0.04) / im.height, 3)]


def tail_pivot(im):
    a = np.array(im.getchannel('A')) > 20
    ys, xs = np.where(a)
    band = xs < xs.min() + max(3, int(im.width * 0.04))
    return [round(float(xs.min() + im.width * 0.03) / im.width, 3), round(float(ys[band].mean()) / im.height, 3)]


def head_pivot(im):
    a = np.array(im.getchannel('A')) > 20
    ys, xs = np.where(a)
    band = ys > ys.max() - max(3, int(im.height * 0.18))
    return [round(float(xs[band].mean()) / im.width, 3), 0.86]


def main():
    sheet, rows, ids = sys.argv[1], int(sys.argv[2]), sys.argv[3].split(',')
    props = None
    if '--props' in sys.argv:
        i = sys.argv.index('--props'); props = (int(sys.argv[i + 1]), sys.argv[i + 2].split(','))
    im = key_magenta(Image.open(sheet))
    W, H = im.size; cw, ch = W / 5, H / rows
    arr0 = np.array(im)
    alpha = arr0[..., 3] > 20
    # 줄마다 그림 덩어리(연결 영역)를 찾아 큰 것 5개를 왼쪽부터 머리·몸통·꼬리·앞다리·뒷다리로 (칸 경계를 넘어도 덩어리째)
    from scipy import ndimage
    near = ndimage.binary_dilation(alpha, iterations=4)           # 가까운 조각(침방울 등)은 같은 덩어리로
    lab, n = ndimage.label(near)
    cells = {}
    for r in range(rows):
        y0, y1 = int(r * ch), int((r + 1) * ch)
        comps = []
        for k in range(1, n + 1):
            ys, xs = np.nonzero((lab == k) & alpha)
            if len(xs) < 200: continue
            cy = ys.mean()
            if y0 <= cy < y1: comps.append((xs.mean(), len(xs), k))
        comps = sorted(sorted(comps, key=lambda c: -c[1])[:5])        # 큰 것 5개 → 왼쪽부터
        for c, (_, _, k) in enumerate(comps): cells[r * 5 + c] = k

    def cell_img(r, c):
        k = cells.get(r * 5 + c)
        if not k: return None
        arr = arr0.copy(); arr[..., 3] = np.where((lab == k) & (arr0[..., 3] > 0), arr[..., 3], 0)
        return crop(Image.fromarray(arr))
    pj_path = os.path.join(RES, 'Parts', 'pivots.json')
    pj = json.load(io.open(pj_path, encoding='utf-8'))
    for r, sid in enumerate(ids):
        if sid == '-': continue
        out = os.path.join(RES, 'Parts', sid); os.makedirs(out, exist_ok=True)
        imgs = {}
        for c, part in enumerate(PARTS):
            p = cell_img(r, c)
            p.save(os.path.join(out, part + '.png')); imgs[part] = p
        piv = {'head': head_pivot(imgs['head']), 'tail': tail_pivot(imgs['tail']), 'front': leg_pivot(imgs['front']), 'back': leg_pivot(imgs['back'])}
        T = imgs['torso']
        f_len = imgs['front'].height * (1 - piv['front'][1]); b_len = imgs['back'].height * (1 - piv['back'][1])
        ground = ANCHOR['shoulder'][1] * T.height + f_len
        back_scale = round((ground - ANCHOR['hip'][1] * T.height) / b_len, 3)
        pj['species'][sid] = {
            'parts': [f'Parts/{sid}/{p}.png' for p in PARTS],
            'pivot': piv, 'pivotUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in piv.items()},
            'torsoAnchor': ANCHOR, 'torsoAnchorUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in ANCHOR.items()},
            'legScale': {'front': 1, 'back': back_scale},
        }
        print(sid, {k: v.size for k, v in imgs.items()}, piv, 'back', back_scale)
    json.dump(pj, io.open(pj_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    if props:
        r, names = props
        out = os.path.join(RES, 'NewRats', 'Props'); os.makedirs(out, exist_ok=True)
        for c, n in enumerate(names):
            if n == '-': continue
            p = cell_img(r, c)
            if p: p.save(os.path.join(out, n + '.png')); print('prop', n, p.size)


if __name__ == '__main__':
    main()
