"""Codex 고양이 파츠 시트(자홍 배경, 5열: 머리·몸통·꼬리·앞다리·뒷다리 / 한 줄 = 한 마리) → UnityResources/Rats/Cats/Parts/<id>/ + Cats/Parts/pivots.json
사용: python Tools/slice_cat_parts.py <시트.png> <행수> <id,id,...|-> [--boss]
  --boss : 몸통이 두꺼운 보스용 — 다리를 몸통 중간(BOSS_SHOULDER_Y·BOSS_HIP_Y)에 붙여 몸통 뒤로 겹치게, 몸통 아래로 보이는 다리 = 몸통 높이 × BOSS_LEG_SHOW
  자르기·관절은 slice_rat_parts.py 와 같음 (덩어리째 자르기, 다리 = 윗면 가운데, 꼬리 = 왼쪽 끝, 머리 = 목 단면 가운데).
  몸통 부착점 = 기존 고양이들 중앙값. 다리 배율 = 기존 고양이들의 (다리 길이 × 배율 ÷ 몸통 높이) 중앙값에 맞춤 (뒷다리는 앞다리와 같은 바닥선).
  그다음: python Tools/gen_sprite_pivots.py → 파츠를 Assets/Art/Rats/Cats/Parts/<id>/ 로 복사 → 유니티 NKK/Reimport Art Sprites · NKK/Build Cat Art Library"""
import io, json, os, sys, statistics as st
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta, crop, leg_pivot, tail_pivot, head_pivot, PARTS

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CATS = os.path.join(ROOT, 'UnityResources', 'Rats', 'Cats', 'Parts')
BOSS_SHOULDER_Y, BOSS_HIP_Y, BOSS_LEG_SHOW = 0.45, 0.5, 0.6


def main():
    sheet, rows, ids = sys.argv[1], int(sys.argv[2]), sys.argv[3].split(',')
    boss = '--boss' in sys.argv
    pj_path = os.path.join(CATS, 'pivots.json')
    pj = json.load(io.open(pj_path, encoding='utf-8'))
    old = [m for m in pj.values() if isinstance(m, dict) and 'anchor' in m]
    anchor = {k: [round(st.median(m['anchor'][k][0] for m in old), 3), round(st.median(m['anchor'][k][1] for m in old), 3)] for k in ('neck', 'tail', 'shoulder', 'hip')}
    # 앞다리 길이(관절 아래) × 배율 ÷ 몸통 높이
    if boss: anchor['shoulder'][1], anchor['hip'][1] = BOSS_SHOULDER_Y, BOSS_HIP_Y
    leg_ratio = st.median(m['size']['front'][1] * (1 - m['pivot']['front'][1]) * m['legScale']['front'] / m['size']['torso'][1] for m in old)

    im = key_magenta(Image.open(sheet))
    arr0 = np.array(im)
    from scipy import ndimage
    # 자홍 테두리 없애기: 알파를 2px 깎고(보스처럼 크게 키우면 분홍 테두리가 보임), 남은 반투명 가장자리의 자홍 기운(R·B > G)을 빼 줌
    a = ndimage.grey_erosion(arr0[..., 3], size=(5, 5))
    edge = (a > 0) & (a < 250)
    rgb = arr0[..., :3].astype(np.int32)
    tint = np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1]
    fix = edge & (tint > 20)
    for c in (0, 2): rgb[..., c] = np.where(fix, np.minimum(rgb[..., c], rgb[..., 1] + 20), rgb[..., c])
    arr0 = np.dstack([rgb.astype(np.uint8), a.astype(np.uint8)])
    alpha = arr0[..., 3] > 20
    lab, n = ndimage.label(ndimage.binary_dilation(alpha, iterations=8))     # 작은 장식(부적 등)도 같은 덩어리로
    H = im.height; ch = H / rows
    for r, cid in enumerate(ids):
        if cid == '-': continue
        y0, y1 = r * ch, (r + 1) * ch
        comps = []
        for k in range(1, n + 1):
            ys, xs = np.nonzero((lab == k) & alpha)
            if len(xs) < 200: continue
            if y0 <= ys.mean() < y1: comps.append((xs.mean(), len(xs), k))
        comps = sorted(sorted(comps, key=lambda c: -c[1])[:5])
        if len(comps) < 5: raise SystemExit(f'{cid}: 파츠 덩어리 {len(comps)}개 (5개 필요)')
        out = os.path.join(CATS, cid); os.makedirs(out, exist_ok=True)
        imgs = {}
        for part, (_, _, k) in zip(PARTS, comps):
            a = arr0.copy(); a[..., 3] = np.where((lab == k) & (arr0[..., 3] > 0), a[..., 3], 0)
            p = crop(Image.fromarray(a)); p.save(os.path.join(out, part + '.png')); imgs[part] = p
        piv = {'head': head_pivot(imgs['head']), 'tail': tail_pivot(imgs['tail']), 'front': leg_pivot(imgs['front']), 'back': leg_pivot(imgs['back'])}
        T = imgs['torso']
        f_len = imgs['front'].height * (1 - piv['front'][1]); b_len = imgs['back'].height * (1 - piv['back'][1])
        front = round((BOSS_LEG_SHOW + 1 - anchor['shoulder'][1]) * T.height / f_len if boss else leg_ratio * T.height / f_len, 3)
        ground = anchor['shoulder'][1] * T.height + f_len * front
        back = round((ground - anchor['hip'][1] * T.height) / b_len, 3)
        pj[cid] = {'size': {k: list(v.size) for k, v in imgs.items()}, 'pivot': piv, 'anchor': anchor, 'legScale': {'front': front, 'back': back},
                   'pivotUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in piv.items()},
                   'anchorUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in anchor.items()}}
        print(cid, {k: v.size for k, v in imgs.items()}, 'legs', front, back)
    json.dump(pj, io.open(pj_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)


if __name__ == '__main__':
    main()
