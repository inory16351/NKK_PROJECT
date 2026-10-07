"""Codex 사람 파츠 시트(자홍 배경, 3열 x 2행: 머리·화난 머리·겁먹은 머리 / 몸통·팔·다리) → 파츠 PNG + Humans/Parts/pivots.json 갱신
사용: python Tools/slice_human_parts.py <시트.png> <사람 code_id>
관절 (웹게임 사람 리그와 같은 형식, 좌상단 기준 0~1):
  머리 = 아래쪽 목 단면 가운데 (y 0.97) · 팔·다리 = 윗면 가운데 (y 0.04)
  몸통 부착점 neck = 윗면 목 가운데 (y 0.03), shoulder = 같은 x (y 0.12), hip = 아랫면 가운데 (y 0.95)"""
import io, json, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta, crop

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, 'UnityResources', 'Rats', 'Humans', 'Parts')
ORDER = [['head', 'angry', 'scared'], ['torso', 'arm', 'leg']]


def biggest(cell):
    # 칸 안에서 가장 큰 덩어리만 (옆 칸 그림이 경계를 살짝 넘어온 조각은 지움)
    from scipy import ndimage
    a = np.array(cell)
    lab, n = ndimage.label(a[..., 3] > 20)
    if n > 1:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        keep = 1 + int(np.argmax(sizes))
        near = ndimage.binary_dilation(lab == keep, iterations=6)   # 땀방울처럼 붙어 있는 작은 조각은 남김
        for k in range(1, n + 1):
            if k != keep and not (near & (lab == k)).any() and sizes[k - 1] < sizes[keep - 1] * 0.5:
                if (lab == k).any():
                    ys, xs = np.where(lab == k)
                    # 칸 가장자리에 닿은 조각만 지움
                    if ys.min() <= 2 or xs.min() <= 2 or ys.max() >= a.shape[0] - 3 or xs.max() >= a.shape[1] - 3:
                        a[lab == k] = 0
    return Image.fromarray(a, 'RGBA')


def band_x(im, top, frac=0.05):
    a = np.array(im.getchannel('A')) > 20
    ys, xs = np.where(a)
    n = max(3, int(im.height * frac))
    sel = ys < ys.min() + n if top else ys > ys.max() - n
    return round(float(xs[sel].mean()) / im.width, 3)


def main(sheet, cid):
    im = key_magenta(Image.open(sheet))
    W, H = im.size
    cw, ch = W // 3, H // 2
    out = os.path.join(RES, cid); os.makedirs(out, exist_ok=True)
    parts = {}
    for r, row in enumerate(ORDER):
        for c, name in enumerate(row):
            p = crop(biggest(im.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))))
            p.save(os.path.join(out, name + '.png')); parts[name] = p
    piv = {k: [band_x(parts[k], False), 0.97] for k in ('head', 'angry', 'scared')}
    piv.update({k: [band_x(parts[k], True), 0.04] for k in ('arm', 'leg')})
    t = parts['torso']; nx = band_x(t, True)
    anchor = {'neck': [nx, 0.03], 'shoulder': [nx, 0.12], 'hip': [band_x(t, False), 0.95]}
    meta = {'size': {k: list(v.size) for k, v in parts.items()}, 'pivot': piv, 'anchor': anchor,
            'pivotUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in piv.items()},
            'anchorUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in anchor.items()}}
    pj = os.path.join(RES, 'pivots.json')
    allm = json.load(io.open(pj, encoding='utf-8'))
    allm[cid] = meta
    json.dump(allm, io.open(pj, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print(cid, json.dumps(meta['size']), json.dumps(anchor))


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
