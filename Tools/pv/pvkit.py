# -*- coding: utf-8 -*-
"""PV 합성 도구 (프레임마다 PIL 로 그림 → ffmpeg). make_pv2.py 가 씀.

좌표: 화면 픽셀 (0,0 = 왼쪽 위), 기준 해상도 1920×1080. 초안(--draft)이면 S 배율로 전부 줄여 그림.
"""
import math, os, subprocess, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
BW, BH = 1920, 1080          # 기준 해상도


def P(path):
    return path if os.path.isabs(path) else os.path.join(ROOT, path)


# ───────── 이징 ─────────
def clamp(x, a=0.0, b=1.0): return max(a, min(b, x))
def lerp(a, b, k): return a + (b - a) * k
def out_cubic(k): k = clamp(k); return 1 - (1 - k) ** 3
def in_cubic(k): k = clamp(k); return k ** 3
def in_out(k): k = clamp(k); return 3 * k * k - 2 * k * k * k
def out_back(k, s=2.2):
    k = clamp(k) - 1
    return 1 + k * k * ((s + 1) * k + s)
def out_elastic(k):
    k = clamp(k)
    if k in (0, 1): return k
    return 2 ** (-10 * k) * math.sin((k * 10 - 0.75) * (2 * math.pi) / 3) + 1
def seg(t, t0, d):
    """t 가 [t0, t0+d] 에서 0→1"""
    return clamp((t - t0) / d) if d > 0 else (1.0 if t >= t0 else 0.0)


# ───────── 그림 ─────────
_cache = {}


_WM = None


def unwatermark(a):
    """Gemini 그림(2752×1536) 오른쪽 아래 반짝이 워터마크 = 흰색을 알파로 덮은 것 → 거꾸로 풀어 원래 색으로 (글자 위에 겹쳐도 됨)"""
    global _WM
    if a.shape[:2] != (1536, 2752): return a
    if _WM is None: _WM = np.array(Image.open(os.path.join(os.path.dirname(__file__), 'gemini_wm_alpha.png'))).astype(np.float32) / 255
    y0, x0 = 1240, 2455
    h, w = _WM.shape
    al = _WM[..., None]
    reg = a[y0:y0 + h, x0:x0 + w, :3]
    a[y0:y0 + h, x0:x0 + w, :3] = np.clip((reg - 255 * al) / (1 - al), 0, 255)
    return a


def key_magenta(im):
    a = unwatermark(np.array(im.convert('RGB')).astype(np.float32))
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    dist = np.sqrt((r - 255) ** 2 + g ** 2 + (b - 255) ** 2)
    alpha = np.clip((dist - 60) / 60, 0, 1)
    k = 1 - alpha
    rgb = a.copy()
    rgb[..., 0] = np.clip((r - 255 * k) / np.maximum(alpha, 1e-3), 0, 255)
    rgb[..., 2] = np.clip((b - 255 * k) / np.maximum(alpha, 1e-3), 0, 255)
    out = np.dstack([rgb, alpha * 255]).astype(np.uint8)
    out[alpha <= 0.02] = 0
    return Image.fromarray(out, 'RGBA')


def img(path, key=False, crop=None):
    """그림 불러오기 (자홍 키잉 · 잘라내기 · 여백 자르기). 캐시."""
    k = (path, key, crop)
    if k not in _cache:
        im = Image.open(P(path)).convert('RGBA')
        if crop: im = im.crop(crop)
        if key:
            im = key_magenta(im)
            bb = im.getbbox()
            if bb: im = im.crop(bb)
        _cache[k] = im
    return _cache[k]


def anim_rows(path, n_cols, n_rows):
    """애니메이션 시트 (줄 = 한 캐릭터의 프레임들) → [[프레임...] 줄마다]. 줄 안 프레임들은 같은 영역(합친 여백)으로 잘라서 위치가 안 흔들림"""
    k = ('anim', path, n_cols, n_rows)
    if k not in _cache:
        im = Image.open(P(path)).convert('RGBA')
        W, H = im.size
        rows = []
        for r in range(n_rows):
            cells = [key_magenta(im.crop((c * W // n_cols, r * H // n_rows, (c + 1) * W // n_cols, (r + 1) * H // n_rows))) for c in range(n_cols)]
            bbs = [c.getbbox() for c in cells if c.getbbox()]
            bb = (min(b[0] for b in bbs), min(b[1] for b in bbs), max(b[2] for b in bbs), max(b[3] for b in bbs))
            rows.append([c.crop(bb) for c in cells])
        _cache[k] = rows
    return _cache[k]


def split_sheet(path, n_cols, n_rows=1, key=True):
    """자홍 시트를 칸으로 나눠 (칸마다 키잉 + 여백 자르기) 목록으로"""
    k = ('sheet', path, n_cols, n_rows)
    if k not in _cache:
        im = Image.open(P(path)).convert('RGBA')
        W, H = im.size
        out = []
        for r in range(n_rows):
            for c in range(n_cols):
                cell = im.crop((c * W // n_cols, r * H // n_rows, (c + 1) * W // n_cols, (r + 1) * H // n_rows))
                cell = key_magenta(cell) if key else cell
                bb = cell.getbbox()
                out.append(cell.crop(bb) if bb else cell)
        _cache[k] = out
    return _cache[k]


def blobs(path, min_area=2000):
    """자홍 시트에서 떨어진 덩어리들을 (다른 덩어리 조각은 지우고) 왼쪽→오른쪽·위→아래 순으로"""
    k = ('blobs', path)
    if k not in _cache:
        from scipy import ndimage
        im = key_magenta(Image.open(P(path)))
        al = np.array(im)[..., 3] > 20
        lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=12))
        H = al.shape[0]
        items = []
        for i, sl in enumerate(ndimage.find_objects(lab), 1):
            if sl is None: continue
            m = (lab[sl] == i) & al[sl]
            if m.sum() < min_area: continue
            arr = np.array(im)[sl].copy()
            arr[..., 3] = np.where(m, arr[..., 3], 0)
            items.append((sl[0].start, sl[1].start, Image.fromarray(arr, 'RGBA')))
        rowh = H / 6
        items.sort(key=lambda t: (round(t[0] / rowh), t[1]))
        _cache[k] = [t[2] for t in items]
    return _cache[k]


def split_axis(path, axis=1, min_area=4000, gap=3):
    """자홍 글자 그림을 빈 줄(axis=1 세로 빈칸 = 글자별, axis=0 가로 빈칸 = 줄별)로 나눔. 작은 조각(워터마크)은 지움"""
    k = ('split', path, axis)
    if k not in _cache:
        from scipy import ndimage
        im = key_magenta(Image.open(P(path)))
        arr = np.array(im)
        al = arr[..., 3] > 30
        lab, n = ndimage.label(al)
        sizes = ndimage.sum(al, lab, range(1, n + 1))
        keep = np.isin(lab, [i + 1 for i, v in enumerate(sizes) if v >= min_area])
        arr[..., 3] = np.where(keep, arr[..., 3], 0)
        prof = keep.sum(axis=0 if axis == 1 else 1) > 0
        runs, st = [], None
        for i, v in enumerate(list(prof) + [False]):
            if v and st is None: st = i
            if not v and st is not None:
                runs.append((st, i)); st = None
        # 너무 가까운 조각(간격 < gap)은 합침
        merged = []
        for r in runs:
            if merged and r[0] - merged[-1][1] < gap: merged[-1] = (merged[-1][0], r[1])
            else: merged.append(r)
        out = []
        for a, b in merged:
            sub = arr[:, a:b] if axis == 1 else arr[a:b, :]
            t = Image.fromarray(sub.copy(), 'RGBA'); bb = t.getbbox()
            if bb: out.append(t.crop(bb))
        _cache[k] = out
    return _cache[k]


def split_letters(path, min_core=1500):
    """테두리가 붙은 글자 그림을 글자별로: 글자 속(밝은 채움)을 씨앗으로, 나머지 픽셀은 가장 가까운 씨앗에 붙임.
    세로로 겹치는 씨앗(! 의 막대와 점)은 한 글자로 묶음. 왼쪽→오른쪽"""
    k = ('letters', path)
    if k not in _cache:
        from scipy import ndimage
        im = key_magenta(Image.open(P(path)))
        arr = np.array(im)
        al = arr[..., 3] > 30
        lab0, n0 = ndimage.label(al)
        sz = ndimage.sum(al, lab0, range(1, n0 + 1))
        al &= np.isin(lab0, [i + 1 for i, v in enumerate(sz) if v >= 4000])
        bright = al & (np.maximum(arr[..., 0], arr[..., 1]).astype(int) > 170)
        lab, n = ndimage.label(bright)
        cores = []
        for i, sl in enumerate(ndimage.find_objects(lab), 1):
            if sl is None or (lab[sl] == i).sum() < min_core: continue
            cores.append([sl[1].start, sl[1].stop, [i]])
        cores.sort()
        groups = []
        for c in cores:
            if groups:
                g = groups[-1]; ov = min(g[1], c[1]) - max(g[0], c[0])
                if ov > 0.5 * min(g[1] - g[0], c[1] - c[0]):
                    g[0] = min(g[0], c[0]); g[1] = max(g[1], c[1]); g[2] += c[2]; continue
            groups.append(c)
        gid = np.zeros(lab.shape, np.int32)
        for j, g in enumerate(groups, 1):
            gid[np.isin(lab, g[2])] = j
        _, (iy, ix) = ndimage.distance_transform_edt(gid == 0, return_indices=True)
        near = gid[iy, ix]
        out = []
        for j in range(1, len(groups) + 1):
            m = al & (near == j)
            a = arr.copy(); a[..., 3] = np.where(m, a[..., 3], 0)
            t = Image.fromarray(a, 'RGBA'); bb = t.getbbox()
            if bb: out.append(t.crop(bb))
        _cache[k] = out
    return _cache[k]


class Frame:
    """한 프레임 캔버스. 기준 좌표(1920×1080)로 그리면 S 배율로 줄여 그림"""

    def __init__(self, S):
        self.S = S
        self.W, self.H = int(BW * S), int(BH * S)
        self.im = Image.new('RGBA', (self.W, self.H), (0, 0, 0, 255))
        self.cam = (0.0, 0.0, 1.0, 0.0)     # 흔들림 dx, dy, 줌, 회전 (이 프레임 전체)

    def fill(self, rgb):
        self.im.paste(rgb + (255,) if len(rgb) == 3 else rgb, (0, 0, self.W, self.H))

    def bg(self, im, zoom=1.0, cx=0.5, cy=0.5):
        """배경을 화면에 꽉 차게 (zoom ≥ 1 = 확대, cx·cy = 확대 중심 0~1)"""
        w, h = im.size
        s = max(self.W / w, self.H / h) * zoom
        vw, vh = self.W / s, self.H / s
        x0 = clamp(cx * w - vw / 2, 0, w - vw); y0 = clamp(cy * h - vh / 2, 0, h - vh)
        crop = im.crop((int(x0), int(y0), int(x0 + vw), int(y0 + vh))).resize((self.W, self.H), Image.BILINEAR)
        self.im.alpha_composite(crop.convert('RGBA'))

    def put(self, im, x, y, scale=1.0, rot=0.0, alpha=1.0, ax=0.5, ay=0.5, sx=1.0, sy=1.0, flip=False):
        """그림을 (x,y) 에. scale = 기준 해상도 배율, ax·ay = 그림 안 기준점, rot = 도 (반시계)"""
        if alpha <= 0.01 or scale <= 0.001: return
        S = self.S
        w = max(1, int(im.width * scale * sx * S)); h = max(1, int(im.height * scale * sy * S))
        t = im.resize((w, h), Image.BILINEAR)
        if flip: t = t.transpose(Image.FLIP_LEFT_RIGHT)
        px, py = ax * w, ay * h
        if abs(rot) > 0.05:
            # 기준점 둘레로 회전: 기준점이 가운데 오게 여백을 붙인 뒤 회전
            r = int(math.hypot(max(px, w - px), max(py, h - py))) + 2
            pad = Image.new('RGBA', (2 * r, 2 * r), (0, 0, 0, 0))
            pad.paste(t, (int(r - px), int(r - py)))
            t = pad.rotate(rot, Image.BICUBIC)
            px = py = r
        if alpha < 0.999:
            a = t.getchannel('A').point(lambda v: int(v * alpha))
            t.putalpha(a)
        self.im.alpha_composite(t, (int(x * S - px), int(y * S - py))) if self._inside(t, x * S - px, y * S - py) else self._paste_clip(t, int(x * S - px), int(y * S - py))

    def _inside(self, t, x, y):
        return x >= 0 and y >= 0 and x + t.width <= self.W and y + t.height <= self.H

    def _paste_clip(self, t, x, y):
        x0, y0 = max(0, x), max(0, y)
        x1, y1 = min(self.W, x + t.width), min(self.H, y + t.height)
        if x1 <= x0 or y1 <= y0: return
        self.im.alpha_composite(t.crop((x0 - x, y0 - y, x1 - x, y1 - y)), (x0, y0))

    def overlay(self, rgb, a):
        if a <= 0.01: return
        self.im = Image.blend(self.im, Image.new('RGBA', self.im.size, rgb + (255,)), clamp(a))

    def flash(self, a):
        self.overlay((255, 255, 255), a)

    def iris(self, x, y, r, color=(0, 0, 0)):
        """(x,y) 둘레 반지름 r 원 밖을 검게 (아이리스 아웃)"""
        S = self.S
        m = Image.new('L', self.im.size, 255)
        d = ImageDraw.Draw(m)
        if r > 0: d.ellipse(((x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S), fill=0)
        black = Image.new('RGBA', self.im.size, color + (255,))
        self.im = Image.composite(black, self.im, m)

    def finish(self):
        """카메라 흔들림·줌 적용 → RGB bytes"""
        dx, dy, z, rot = self.cam
        im = self.im
        if z != 1 or dx or dy or rot:
            W, H = self.W, self.H
            im = im.transform((W, H), Image.AFFINE, _affine(W, H, dx * self.S, dy * self.S, z, rot), Image.BILINEAR, fillcolor=(0, 0, 0, 255))
        return im.convert('RGB').tobytes()


def _affine(W, H, dx, dy, z, rot):
    # 출력 → 입력 좌표 (가운데 기준 확대·회전 후 이동)
    c, s = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    cx, cy = W / 2, H / 2
    a, b = c / z, s / z
    d, e = -s / z, c / z
    # x_in = a*(x-cx-dx) + b*(y-cy-dy) + cx
    return (a, b, cx - a * (cx + dx) - b * (cy + dy), d, e, cy - d * (cx + dx) - e * (cy + dy))


def shake(t, t0, dur=0.35, amp=26, seed=1):
    """t0 에 쾅 → 흔들림 (dx, dy, rot)"""
    k = seg(t, t0, dur)
    if k <= 0 or k >= 1: return 0, 0, 0
    f = (1 - k) ** 2
    rnd = random.Random(int((t - t0) * 60) + seed * 999)
    return (rnd.uniform(-1, 1) * amp * f, rnd.uniform(-1, 1) * amp * f, rnd.uniform(-1, 1) * 1.2 * f)


def outline(im, px=14, color=(255, 255, 255)):
    """스티커 테두리 (알파를 px 만큼 키워 뒤에 깖)"""
    k = ('outline', id(im), px, color)
    if k not in _cache:
        pad = px + 2
        base = Image.new('RGBA', (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
        base.paste(im, (pad, pad))
        a = base.getchannel('A').filter(ImageFilter.MaxFilter(px * 2 + 1)).point(lambda v: 255 if v > 30 else 0)
        out = Image.new('RGBA', base.size, color + (0,)); out.putalpha(a)
        out.alpha_composite(base)
        _cache[k] = out
    return _cache[k]


# ───────── 만화 글자 ─────────
FONT = 'Unity_Making/NKK_Project/Assets/Fonts/Jua-Regular.ttf'
CREAM = (255, 246, 214); BROWN = (74, 40, 18); YELLOW = (255, 206, 64); RED = (235, 72, 56); PINK = (255, 128, 160); BLUE = (90, 170, 255)


def word(text, size=200, fill=YELLOW, stroke=BROWN, sw=None, shadow=True, fill2=None):
    """두꺼운 테두리 + 그림자 + (fill2 있으면 위→아래 두 색) 만화 글자 그림"""
    k = ('word', text, size, fill, stroke, sw, shadow, fill2)
    if k in _cache: return _cache[k]
    f = ImageFont.truetype(P(FONT), size)
    sw = sw if sw is not None else max(3, size // 9)
    box = ImageDraw.Draw(Image.new('L', (1, 1))).textbbox((0, 0), text, font=f, stroke_width=sw)
    pad = sw + size // 10
    w, h = box[2] - box[0] + pad * 2, box[3] - box[1] + pad * 2 + size // 10
    ox, oy = pad - box[0], pad - box[1]
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(out)
    if shadow:
        d.text((ox, oy + size // 12), text, font=f, fill=stroke + (255,), stroke_width=sw, stroke_fill=stroke + (255,))
    d.text((ox, oy), text, font=f, fill=stroke + (255,), stroke_width=sw, stroke_fill=stroke + (255,))
    # 글자 속 = 채움 (두 색이면 위가 밝게)
    m = Image.new('L', (w, h), 0)
    ImageDraw.Draw(m).text((ox, oy), text, font=f, fill=255)
    if fill2:
        g = Image.new('RGBA', (w, h))
        gd = ImageDraw.Draw(g)
        top, bot = oy + box[1], oy + box[3]
        for y in range(h):
            k2 = clamp((y - top) / max(1, bot - top))
            gd.line([(0, y), (w, y)], fill=tuple(int(lerp(a, b, k2)) for a, b in zip(fill, fill2)) + (255,))
    else:
        g = Image.new('RGBA', (w, h), fill + (255,))
    out.paste(g, (0, 0), m)
    # 위쪽 살짝 반짝 (만화 광택)
    _cache[k] = out
    return out


def letters(text, size=200, **kw):
    """글자 하나씩 따로 (STEAL — 글자 훔치기, SMASH — 글자 날리기용). [(그림, 글자 가운데 x 오프셋)]"""
    f = ImageFont.truetype(P(FONT), size)
    d = ImageDraw.Draw(Image.new('L', (1, 1)))
    total = d.textlength(text, font=f)
    out, x = [], -total / 2
    for ch in text:
        adv = d.textlength(ch, font=f)
        if ch.strip(): out.append((word(ch, size, **kw), x + adv / 2))
        x += adv
    return out


def slam(fr, im, x, y, t, t0, scale=1.0, rot=0.0, dur=0.22, from_scale=2.6, alpha_in=0.06, **kw):
    """박히는 글자: 크게 → 튕기며 제자리. t0 전엔 안 보임"""
    if t < t0: return False
    k = seg(t, t0, dur)
    s = lerp(from_scale, 1.0, out_back(k, 1.6)) * scale
    fr.put(im, x, y, s, rot, clamp((t - t0) / alpha_in), **kw)
    return True


def pop(fr, im, x, y, t, t0, scale=1.0, rot=0.0, dur=0.35):
    """튀어나오는 글자: 0 → 튕기며 1"""
    if t < t0: return False
    fr.put(im, x, y, out_elastic(seg(t, t0, dur)) * scale, rot)
    return True


# ───────── 벡터 입자 (흩날림) ─────────
class Burst:
    """t0 에 (x,y) 에서 사방으로 흩어지는 조각들 (그림 목록 중 랜덤)"""

    def __init__(self, x, y, t0, sprites, n=14, speed=(900, 1700), life=0.6, scale=(0.25, 0.6), seed=3, spin=True, grav=0, aim=None, spread=360):
        rnd = random.Random(seed)
        self.t0, self.life, self.grav = t0, life, grav
        self.parts = []
        for i in range(n):
            ang = math.radians((aim if aim is not None else 0) + rnd.uniform(-spread / 2, spread / 2)) if aim is not None else rnd.uniform(0, 2 * math.pi)
            v = rnd.uniform(*speed)
            self.parts.append(dict(im=rnd.choice(sprites), x=x, y=y, vx=math.cos(ang) * v, vy=math.sin(ang) * v,
                                   s=rnd.uniform(*scale), r0=rnd.uniform(0, 360), vr=rnd.uniform(-540, 540) if spin else 0,
                                   ang=-math.degrees(ang)))

    def draw(self, fr, t, align=False):
        dt = t - self.t0
        if dt < 0 or dt > self.life: return
        k = dt / self.life
        drag = (1 - math.exp(-3 * dt)) / 3          # 감속
        for p in self.parts:
            x = p['x'] + p['vx'] * drag
            y = p['y'] + p['vy'] * drag + 0.5 * self.grav * dt * dt
            rot = p['ang'] if align else p['r0'] + p['vr'] * dt
            fr.put(p['im'], x, y, p['s'] * (1 - 0.4 * k), rot, 1 - k ** 2)


# ───────── 쥐 리그 (게임 RatRig 와 같은 조립) ─────────
RIG_META = None
PIVOT = {}


def _rig_meta():
    global RIG_META
    if RIG_META is None:
        import json
        d = json.load(open(P('Unity_Making/NKK_Project/Assets/Data/RatRigMeta.json'), encoding='utf-8'))
        RIG_META = {r['code_id']: r for r in d['items']}
    return RIG_META


def _pivot(code, part):
    """스프라이트 .meta 의 피벗 (왼쪽 아래 기준 0~1)"""
    k = (code, part)
    if k not in PIVOT:
        meta = P(f'Unity_Making/NKK_Project/Assets/Art/Rats/Parts/{code}/{part}.png.meta')
        px, py, align = 0.5, 0.5, 0
        for line in open(meta, encoding='utf-8'):
            line = line.strip()
            if line.startswith('alignment:') and align == 0: align = int(line.split(':')[1])
            if line.startswith('spritePivot:'):
                v = line.split('{')[1].split('}')[0]
                kv = dict(s.strip().split(':') for s in v.split(','))
                px, py = float(kv['x']), float(kv['y'])
                break
        PIVOT[k] = (px, py) if align == 9 else (0.5, 0.5)
    return PIVOT[k]


def rat_pose_run(w, amp=1.0, speed_fast=False):
    """게임 Rat.MakePose 달리기 (w = 걸음 위상)"""
    p = dict(head=math.cos(w) * 0.05 - 0.05, headX=0, tail=0.05 + math.sin(w * 0.5) * 0.25,
             front=math.sin(w) * amp, farFront=math.sin(w + 0.6) * amp, back=math.sin(w + math.pi) * amp,
             farBack=math.sin(w + math.pi + 0.6) * amp, bob=-abs(math.sin(w)) * 3.5, tilt=math.sin(w) * 0.06, sx=1 + math.sin(w) * 0.05, sy=1)
    if speed_fast: p.update(head=-0.12, sx=1.1, sy=0.93)
    return p


def rat_image(code, pose, length=300, far_col=(204, 199, 201)):
    """쥐 한 마리를 왼쪽을 보는 그림 하나로 (length = 기준 해상도 px 길이). 반환 (그림, 발 기준점 (x, y))
    게임 RatRig.Build/Apply 와 같은 조립: 엉덩이 = 몸 기울기 축, 회전 부호 = 다리 -p · 꼬리/머리 +p (반시계)"""
    meta = _rig_meta()[code]
    base = f'Unity_Making/NKK_Project/Assets/Art/Rats/Parts/{code}'
    parts = {n: img(f'{base}/{n}.png') for n in ('torso', 'head', 'tail', 'front', 'back')}
    tw, th = parts['torso'].size
    pf, pb = _pivot(code, 'front'), _pivot(code, 'back')
    legF, legB = meta.get('leg_front', 1), meta.get('leg_back', 1)
    legLenF = parts['front'].height * pf[1] * legF
    torsoX, torsoY = -tw * 0.5, -(meta['shoulder'][1] * th + legLenF)

    def place(a): return (torsoX + a[0] * tw, torsoY + a[1] * th)
    hip, neck, tailA, sh = place(meta['hip']), place(meta['neck']), place(meta['tail']), place(meta['shoulder'])
    headScale = min(1, 0.62 * tw / parts['head'].width)
    rig_len = tw + parts['head'].width * headScale * 0.55
    C = int(max(tw, th) * 3)
    can = Image.new('RGBA', (C, C), (0, 0, 0, 0))
    O = (C / 2, C * 0.7)                                   # 발 아래 가운데
    bob = pose.get('bob', 0) * rig_len / 40                 # 게임 단위 (쥐 길이 40 기준) → 리그 px (음수 = 위로)
    tilt = pose.get('tilt', 0)
    ct, st = math.cos(tilt), math.sin(tilt)

    def body_xy(pt):
        x, y = pt[0] - hip[0], pt[1] - hip[1]
        return (hip[0] + x * ct + y * st, hip[1] - x * st + y * ct + bob)

    def draw(im, at, pivot, rot, scale=1.0, tint=None):
        w, h = max(1, int(im.width * scale)), max(1, int(im.height * scale))
        t = im.resize((w, h), Image.BILINEAR)
        if tint:
            arr = np.array(t).astype(np.float32); arr[..., :3] *= np.array(tint) / 255.0
            t = Image.fromarray(arr.astype(np.uint8), 'RGBA')
        px, py = pivot[0] * w, (1 - pivot[1]) * h
        r = int(math.hypot(max(px, w - px), max(py, h - py))) + 2
        pad = Image.new('RGBA', (2 * r, 2 * r), (0, 0, 0, 0)); pad.paste(t, (int(r - px), int(r - py)))
        pad = pad.rotate(math.degrees(rot + tilt), Image.BICUBIC)
        x, y = body_xy(at)
        can.alpha_composite(pad, (int(O[0] + x - r), int(O[1] + y - r)))

    gap = tw * 0.07
    draw(parts['back'], (hip[0] + gap, hip[1]), pb, -pose['farBack'], legB, far_col)
    draw(parts['front'], (sh[0] + gap, sh[1]), pf, -pose['farFront'], legF, far_col)
    draw(parts['tail'], tailA, _pivot(code, 'tail'), pose['tail'])
    draw(parts['torso'], (0, torsoY + th * 0.5), (0.5, 0.5), 0)
    draw(parts['back'], hip, pb, -pose['back'], legB)
    if pose['front'] > 0.9:                                  # 치켜든 앞발은 얼굴 앞으로
        draw(parts['head'], (neck[0] + pose.get('headX', 0), neck[1]), _pivot(code, 'head'), pose['head'], headScale)
        draw(parts['front'], sh, pf, -pose['front'], legF)
    else:
        draw(parts['front'], sh, pf, -pose['front'], legF)
        draw(parts['head'], (neck[0] + pose.get('headX', 0), neck[1]), _pivot(code, 'head'), pose['head'], headScale)
    bb = can.getbbox()
    out = can.crop(bb)
    sx = length / rig_len * pose.get('sx', 1); sy = length / rig_len * pose.get('sy', 1)
    out = out.resize((max(1, int(out.width * sx)), max(1, int(out.height * sy))), Image.BILINEAR)
    return out, ((O[0] - bb[0]) * sx, (O[1] - bb[1]) * sy)


# ───────── 영상 클립 읽기 ─────────
class Clip:
    """영상을 순서대로 읽음 (get(t) = 클립 안 t초 프레임, 앞으로만)"""

    def __init__(self, path, S, fps, start=0.0, speed=1.0):
        self.path, self.S, self.fps, self.start, self.speed = P(path), S, fps, start, speed
        self.W, self.H = int(BW * S), int(BH * S)
        self.proc = None; self.idx = -1; self.cur = None

    def _open(self):
        vf = f"setpts=(PTS-STARTPTS)/{self.speed},fps={self.fps},scale={self.W}:{self.H}:force_original_aspect_ratio=increase,crop={self.W}:{self.H}"
        self.proc = subprocess.Popen(['ffmpeg', '-v', 'error', '-ss', str(self.start), '-i', self.path, '-vf', vf, '-f', 'rawvideo', '-pix_fmt', 'rgba', '-'],
                                     stdout=subprocess.PIPE)

    def get(self, t):
        if not os.path.exists(self.path): return None
        if self.proc is None: self._open()
        want = int(t * self.fps)
        n = self.W * self.H * 4
        while self.idx < want:
            buf = self.proc.stdout.read(n)
            if len(buf) < n: break
            self.cur = Image.frombuffer('RGBA', (self.W, self.H), buf, 'raw', 'RGBA', 0, 1).copy()
            self.idx += 1
        return self.cur

    def close(self):
        if self.proc: self.proc.kill(); self.proc = None
