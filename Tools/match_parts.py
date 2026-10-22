"""통 그림(참고) 위에서 파츠 자리 찾기 — 리그 값을 통 그림에 맞추는 도구 (보스 개편 2026-10-22)
파츠마다 회전을 바꿔 가며 통 그림과 색 차이가 가장 작은 자리를 찾음 (cv2.matchTemplate + 파츠 알파 마스크).
같은 파츠가 두 번 있으면 (가까운 팔·먼 팔) --twice 로 두 번째 자리도 (첫 자리 주변은 뺌).
출력: JSON {파츠: [{cx, cy, angle, w, h, score}, ...]} (통 그림 픽셀, y 아래 +, angle = 반시계 °) + 겹친 확인 그림
사용: python Tools/match_parts.py <통그림.png> <파츠폴더> <출력.json> <확인.png> name[:2][@x0,y0,x1,y1] ...
  (name:2 = 두 번 찾기 · @영역 = 파츠 가운데가 이 영역 안일 때만, 비슷한 무늬에 끌려갈 때)
점 변환: place(m, (u, v)) = 파츠 그림 좌상단 기준 (u, v) 가 통 그림 어디에 오는지"""
import json, math, os, sys
import cv2
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta

COARSE = 0.5          # 처음엔 반 크기로 넓게 찾고, 그 자리 근처를 원래 크기로 다시
ANGLES = range(-40, 41, 4)


def rgba(im): return np.array(im.convert('RGBA'))


def match(full, part, angles, scale, exclude=None, region=None):
    """full·part = RGBA 배열. 가장 잘 맞는 (점수, 좌상단 x, y, 각도, 회전한 그림 크기)"""
    F = cv2.resize(full, None, fx=scale, fy=scale, interpolation=cv2.INTER_AREA) if scale != 1 else full
    fr = F[..., :3].astype(np.float32)
    best = None
    for a in angles:
        p = Image.fromarray(part).rotate(a, resample=Image.BICUBIC, expand=True)
        if scale != 1: p = p.resize((max(1, int(p.width * scale)), max(1, int(p.height * scale))), Image.LANCZOS)
        P = np.array(p)
        if P.shape[0] >= fr.shape[0] or P.shape[1] >= fr.shape[1]: continue
        m = (P[..., 3] > 128).astype(np.float32)
        n = m.sum()
        if n < 20: continue
        r = cv2.matchTemplate(fr, P[..., :3].astype(np.float32), cv2.TM_SQDIFF, mask=np.dstack([m] * 3)) / n
        r[~np.isfinite(r)] = 1e18
        if exclude is not None or region is not None:
            yy, xx = np.mgrid[0:r.shape[0], 0:r.shape[1]]
            cx, cy = (xx + P.shape[1] / 2) / scale, (yy + P.shape[0] / 2) / scale
            if exclude is not None:
                ex, ey, er = exclude
                r[(cx - ex) ** 2 + (cy - ey) ** 2 < er ** 2] = 1e18
            if region is not None:
                x0, y0, x1, y1 = region
                r[(cx < x0) | (cx > x1) | (cy < y0) | (cy > y1)] = 1e18
        v, _, loc, _ = cv2.minMaxLoc(r)
        if best is None or v < best[0]: best = (v, loc[0] / scale, loc[1] / scale, a, P.shape[1] / scale, P.shape[0] / scale)
    return best


def find(full, part, exclude=None, region=None):
    c = match(full, part, ANGLES, COARSE, exclude, region)
    _, x, y, a, w, h = c
    # 원래 크기로 근처만 다시 (각도 ±3)
    pad = 24
    x0, y0 = max(0, int(x) - pad), max(0, int(y) - pad)
    x1, y1 = min(full.shape[1], int(x + w) + pad), min(full.shape[0], int(y + h) + pad)
    f = match(full[y0:y1, x0:x1], part, range(a - 3, a + 4), 1)
    if f is None: f = c; ox = oy = 0
    else: ox, oy = x0, y0
    v, x, y, a, w, h = f
    return {'cx': x + ox + w / 2, 'cy': y + oy + h / 2, 'angle': a, 'w': part.shape[1], 'h': part.shape[0], 'score': round(float(v), 1)}


def place(m, uv):
    """파츠 그림 좌상단 기준 (u, v) → 통 그림 픽셀"""
    dx, dy = (uv[0] - 0.5) * m['w'], (uv[1] - 0.5) * m['h']
    t = math.radians(m['angle'])
    return m['cx'] + dx * math.cos(t) + dy * math.sin(t), m['cy'] - dx * math.sin(t) + dy * math.cos(t)


def overlay(full_im, parts, res, out):
    base = full_im.convert('RGBA'); base = Image.blend(base, Image.new('RGBA', base.size, (255, 255, 255, 255)), 0.55)
    for name, ms in res.items():
        for i, m in enumerate(ms):
            p = parts[name].rotate(m['angle'], resample=Image.BICUBIC, expand=True)
            a = np.array(p); a[..., 3] = (a[..., 3] * (0.75 if i == 0 else 0.5)).astype(np.uint8); p = Image.fromarray(a)
            base.alpha_composite(p, (int(round(m['cx'] - p.width / 2)), int(round(m['cy'] - p.height / 2))))
    base.save(out)


def main():
    full_path, part_dir, out_json, out_png = sys.argv[1:5]
    full_im = key_magenta(Image.open(full_path)); full = rgba(full_im)
    parts, res = {}, {}
    for arg in sys.argv[5:]:
        arg, _, reg = arg.partition('@')
        region = tuple(float(v) for v in reg.split(',')) if reg else None
        name, _, n = arg.partition(':')
        im = Image.open(os.path.join(part_dir, name + '.png')).convert('RGBA'); parts[name] = im
        P = rgba(im); m1 = find(full, P, region=region); res[name] = [m1]
        if n == '2': res[name].append(find(full, P, (m1['cx'], m1['cy'], min(m1['w'], m1['h']) * 0.45), region))
        print(name, [(round(m['cx']), round(m['cy']), m['angle'], m['score']) for m in res[name]])
    json.dump(res, open(out_json, 'w'), indent=1)
    overlay(full_im, parts, res, out_png)


if __name__ == '__main__':
    main()


def to_uv(m, xy):
    """통 그림 픽셀 → 파츠 그림 좌상단 기준 (u, v) (place 의 반대)"""
    t = math.radians(m['angle']); x, y = xy[0] - m['cx'], xy[1] - m['cy']
    dx, dy = x * math.cos(t) - y * math.sin(t), x * math.sin(t) + y * math.cos(t)
    return dx / m['w'] + 0.5, dy / m['h'] + 0.5


def rig_values(res, joints, ground_y=None):
    """joints = {파츠: (부모 또는 None, 관절 (u, v), [몇 번째 매칭])} → {파츠: (at, pivot, angle)} — BossPartArt 값 (at = 부모 그림 기준, 뿌리는 발끝 원점 배율)"""
    out = {}
    for name, (parent, piv, *k) in joints.items():
        m = res[name][k[0] if k else 0]
        if parent is None:
            # 뿌리: at.y = 관절 높이 (바닥 = ground_y) ÷ 그림 높이
            jx, jy = place(m, piv)
            out[name] = ((0.0, round((ground_y - jy) / m['h'], 3)), piv, m['angle'])
            continue
        pn, *pk = parent.split('#'); pm = res[pn][int(pk[0]) if pk else 0]
        out[name] = (tuple(round(v, 3) for v in to_uv(pm, place(m, piv))), piv, m['angle'] - pm['angle'])
    return out
