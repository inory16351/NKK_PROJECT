"""차력거인 쥐 얼굴 머리 (2026-10-22, 저작권 때문에 얼굴만 쥐로). 시트 = UnityResources/Rats/BossRework/Sheets/tt10.png (왼쪽 = 보통, 오른쪽 = 화남)
원래 머리(Parts/boss_cartgiant/head.png · head2.png)와 같은 캔버스 크기 · 같은 목 자리(오른쪽 아래)에 맞춰 넣음 → 리그 피벗 그대로.
결과 = 미리보기 후보 UnityResources/Rats/BossRework/rig_web/parts_rat/ (다른 파츠는 parts/ 복사) — 검토 뒤 --apply 로 Parts/boss_cartgiant + Assets 에 넣음 (원래 머리는 head_human*.png 로 보관)
사용: python Tools/slice_cartgiant_ratface.py [--apply]"""
import os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta, crop

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BR = os.path.join(ROOT, 'UnityResources', 'Rats', 'BossRework')
PARTS = os.path.join(BR, 'Parts', 'boss_cartgiant')
WEB = os.path.join(BR, 'rig_web')
CAND = os.path.join(WEB, 'parts_rat')
ART = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats', 'BossParts', 'boss_cartgiant')


def islands(im, n=2):
    a = np.array(im)
    lab, _ = ndimage.label(ndimage.binary_dilation(a[..., 3] > 20, iterations=6))
    objs = []
    for k, sl in enumerate(ndimage.find_objects(lab), 1):
        m = (lab[sl] == k) & (a[sl][..., 3] > 20)
        if m.sum() < 2000: continue
        objs.append((m.sum(), k, sl))
    objs = sorted(objs, reverse=True)[:n]
    objs.sort(key=lambda o: o[2][1].start)          # 왼쪽부터
    out = []
    for _, k, sl in objs:
        b = a.copy(); b[..., 3] = np.where(lab == k, b[..., 3], 0)
        out.append(crop(Image.fromarray(b), pad=0))
    return out


def bbox(im):
    ys, xs = np.nonzero(np.array(im.getchannel('A')) > 20)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def fit(new, orig):
    """원래 머리 그림의 보이는 범위(bbox) 높이에 맞춰 키우고, bbox 오른쪽 아래(목 자리)를 맞춤"""
    x0, y0, x1, y1 = bbox(orig)
    k = (y1 - y0) / new.height
    nw, nh = max(1, round(new.width * k)), max(1, round(new.height * k))
    s = new.resize((nw, nh), Image.LANCZOS)
    dx = max(0, nw - x1 + 4)                          # 쥐 주둥이가 원래 칸보다 길면 칸을 왼쪽으로 넓힘 (피벗은 PivotX 로 옮김)
    canvas = Image.new('RGBA', (orig.width + dx, orig.height), (0, 0, 0, 0))
    canvas.paste(s, (x1 + dx - nw, y1 - nh), s)
    return canvas, dx


PIVOT = (0.76, 0.92)          # 원래 머리 피벗 (Editor/BossPartArtBuilder.cs · giant_parts.json, 그림 좌상단 기준 0~1)
def pivot_x(w0, dx): return round((PIVOT[0] * w0 + dx) / (w0 + dx), 3)


def main():
    sheet = key_magenta(Image.open(os.path.join(BR, 'Sheets', 'tt10.png')))
    heads = islands(sheet)
    if len(heads) < 2: sys.exit('머리 2개를 못 찾음')
    os.makedirs(CAND, exist_ok=True)
    for f in os.listdir(os.path.join(WEB, 'parts')):
        shutil.copy2(os.path.join(WEB, 'parts', f), os.path.join(CAND, f))
    src = PARTS if not os.path.exists(os.path.join(PARTS, 'head_human.png')) else None
    dxs = []
    for img, name in zip(heads, ('head', 'head2')):
        orig = Image.open(os.path.join(PARTS, (name if src else name.replace('head', 'head_human')) + '.png')).convert('RGBA')
        out, dx = fit(img, orig); dxs.append((orig.width, dx))
        out.save(os.path.join(CAND, name + '.png'))
        print('후보', name, out.size)
    # 두 장 칸 크기를 맞춤 (번갈아 바뀌어도 피벗 같게) → 더 넓은 쪽으로
    w0, dx = max(dxs, key=lambda t: t[1])
    for name in ('head', 'head2'):
        f = os.path.join(CAND, name + '.png'); im = Image.open(f)
        if im.width < w0 + dx: c = Image.new('RGBA', (w0 + dx, im.height), (0, 0, 0, 0)); c.paste(im, (w0 + dx - im.width, 0)); c.save(f)
    px = pivot_x(w0, dx)
    print('새 피벗', (px, PIVOT[1]), '칸 넓힘', dx)
    j = os.path.join(WEB, 'boss_cartgiant_rat.json')
    if os.path.exists(j):
        import json
        d = json.load(open(j, encoding='utf-8'))
        for p in d['parts']:
            if p['name'] == 'head': p['pivot'] = [px, PIVOT[1]]
        json.dump(d, open(j, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    if '--apply' in sys.argv:
        for name in ('head', 'head2'):
            keep = os.path.join(PARTS, name.replace('head', 'head_human') + '.png')
            if not os.path.exists(keep): shutil.copy2(os.path.join(PARTS, name + '.png'), keep)
            shutil.copy2(os.path.join(CAND, name + '.png'), os.path.join(PARTS, name + '.png'))
            shutil.copy2(os.path.join(CAND, name + '.png'), os.path.join(ART, name + '.png'))
            shutil.copy2(os.path.join(CAND, name + '.png'), os.path.join(WEB, 'parts', name + '.png'))
        print('적용: Parts/boss_cartgiant · Assets · rig_web/parts')


if __name__ == '__main__':
    main()
