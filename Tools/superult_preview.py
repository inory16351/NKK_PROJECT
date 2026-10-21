"""초필살기 컷신 미리보기 (Unity 없이): superult_story 연출을 SuperUltLayer.Apply(t) 와 같은 규칙으로 PIL 로 그려 시간별 장면 모음 · GIF 를 만듦 (2026-10-21 외계인 생쥐 v2 초안 확인용).
화면 1920×1080 (가운데 0, 위 +) 을 절반 크기로 그림. 글(text) · 띠 마스크(band)는 안 그림. 흔들림(jitter · kick) · 깜빡임은 고정 난수.
사용: python Tools/superult_preview.py <code> <출력 폴더> [시각 ...]   (시각을 안 주면 0.4~끝 0.1초마다)"""
import math, os, random, re, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
ART = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats', 'SuperUlt')
K_RE = re.compile(r'K\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f, (\d)\)')
SW_RE = re.compile(r't = ([-\d.]+)f, sprite = S\("(\w+)"\)')
SC = 0.5                                                  # 그림 배율 (1920 → 960)


def ease(e, k):
    k = min(1, max(0, k))
    if e == 1: return k
    if e == 2: return k ** 3
    if e == 3: return 4 * k ** 3 if k < 0.5 else 1 - (-2 * k + 2) ** 3 / 2
    if e == 4: c1 = 1.9; return 1 + (c1 + 1) * (k - 1) ** 3 + c1 * (k - 1) ** 2
    if e == 5: return 1 if k >= 1 else 0
    return 1 - (1 - k) ** 3


class Node:
    def __init__(self, name, parent, spr=None, size=1.0, keys=(), group=False):
        self.name, self.parent, self.spr, self.keys, self.group = name, parent, spr, list(keys), group
        self.children = []; self.size = None; self.pivot = (0.5, 0.5); self.swaps = []; self.swapResize = False
        self.fx = dict(spin=0.0, pulse=0.0, pulseHz=6.0, jitter=0.0, flicker=0.0, kicks=[], kickPower=40.0, kickRot=2.0, kickTime=0.3)
        if spr:
            w, h = sprite(spr).size; self.size = (w * size, h * size)
        if parent: parent.children.append(self)

    def apply(self, t, rnd):
        ks = self.keys
        if not ks: return (0, 0), 1, 0, 1
        a = b = ks[0]; k = 0
        if t >= ks[-1][0]: a = b = ks[-1]
        elif t > ks[0][0]:
            for i in range(len(ks) - 1):
                if t < ks[i + 1][0]: a, b = ks[i], ks[i + 1]; k = ease(b[6], (t - a[0]) / max(1e-4, b[0] - a[0])); break
        x = a[1] + (b[1] - a[1]) * k; y = a[2] + (b[2] - a[2]) * k
        sc = a[3] + (b[3] - a[3]) * k; rot = a[4] + (b[4] - a[4]) * k; al = min(1, max(0, a[5] + (b[5] - a[5]) * min(1, max(0, k))))
        f = self.fx
        if f['pulse']: sc *= 1 + f['pulse'] * math.sin(t * f['pulseHz'] * math.pi * 2)
        if f['jitter'] and al > 0: x += rnd.uniform(-1, 1) * f['jitter']; y += rnd.uniform(-1, 1) * f['jitter']
        if f['flicker'] and al > 0: al *= 1 - f['flicker'] * rnd.random()
        kick = max([1 - (t - kt) / f['kickTime'] for kt in f['kicks'] if kt <= t < kt + f['kickTime']] or [0])
        if kick > 0:
            kick *= kick; x += rnd.uniform(-1, 1) * f['kickPower'] * kick; y += rnd.uniform(-1, 1) * f['kickPower'] * kick; rot += rnd.uniform(-1, 1) * f['kickRot'] * kick
        return (x, y), sc, rot + f['spin'] * t, al

    def sprite_at(self, t):
        s = self.spr
        for st, n in self.swaps:
            if t >= st: s = n
        return s


_cache = {}
def sprite(name):
    if name not in _cache:
        p = os.path.join(ART, CODE, name + '.png')
        _cache[name] = Image.open(p).convert('RGBA') if os.path.exists(p) else Image.new('RGBA', (8, 8))
    return _cache[name]


def parse(lines):
    """Story 가 만든 C# 줄 → 노드 나무"""
    root = Node('root', None)
    var = {f'root_{CODE}.transform': root}
    for ln in lines:
        m = re.match(r'var (\w+) = L\("(\w+)", ([\w.]+), "(\w+)", ([-\d.]+)f, new\[\] \{ (.*?) \}\);', ln)
        if m:
            v, name, par, spr, size, ks = m.groups()
            n = Node(name, var[par], spr, float(size), [tuple(float(q) for q in g[:6]) + (int(g[6]),) for g in K_RE.findall(ks)])
            var[v] = n; var[v + '.transform'] = n
        m = re.match(r'var (\w+) = G\("(\w+)", ([\w.]+), new\[\] \{ (.*?) \}\)\.transform;', ln)
        if m:
            v, name, par, ks = m.groups()
            var[v] = Node(name, var[par], keys=[tuple(float(q) for q in g[:6]) + (int(g[6]),) for g in K_RE.findall(ks)], group=True)
        m = re.match(r'var (\w+) = E\("(\w+)", ([\w.]+), new\[\] \{ (.*?) \}\)(\.transform)?;', ln)
        if m:
            v, name, par, ks, _ = m.groups()
            n = Node(name, var[par], keys=[tuple(float(q) for q in g[:6]) + (int(g[6]),) for g in K_RE.findall(ks)])
            var[v] = n; var[v + '.transform'] = n
        for v, body in re.findall(r'(?:^|; |\{ )(\w+)\.(kicks = new (?:float)?\[\] \{[^}]*\}|kickPower = [-\d.]+f?|kickRot = [-\d.]+f?|kickTime = [-\d.]+f?|spin = [-\d.]+f|pulse = [-\d.]+f|pulseHz = [-\d.]+f|jitter = [-\d.]+f|flicker = [-\d.]+f)', ln):
            if v not in var: continue
            key, val = body.split(' = ', 1)
            var[v].fx[key] = [float(q) for q in re.findall(r'[-\d.]+', val)] if key == 'kicks' else float(val.rstrip('f'))
        m = re.search(r'\(\(RectTransform\)(\w+)\.transform\)\.pivot = new Vector2\(([-\d.]+)f, ([-\d.]+)f\)', ln)
        if m: var[m.group(1)].pivot = (float(m.group(2)), float(m.group(3)))
        m = re.search(r'\(\(RectTransform\)(\w+)\.transform\)\.sizeDelta = new Vector2\(([-\d.]+)f, ([-\d.]+)f\)', ln)
        if m: var[m.group(1)].size = (float(m.group(2)), float(m.group(3)))
        m = re.match(r'(\w+)\.swaps = new\[\] \{ (.*) \};', ln)
        if m: var[m.group(1)].swaps = [(float(a), b) for a, b in SW_RE.findall(m.group(2))]
        if '.swapResize = true' in ln: var[ln.split('.swapResize')[0].split('; ')[-1]].swapResize = True
        m = re.match(r'(\w+)\.transform\.SetAsFirstSibling\(\);', ln)
        if m:
            n = var[m.group(1)]; n.parent.children.remove(n); n.parent.children.insert(0, n)
    return root


def render(root, t, W=1920, H=1080):
    rnd = random.Random(int(t * 1000))
    canvas = Image.new('RGBA', (int(W * SC), int(H * SC)), (0, 0, 0, 255))

    def walk(n, M, alpha):
        pos, sc, rot, al = n.apply(t, rnd)
        if n.group:
            alpha *= al
            if alpha <= 0.001: return
        r = math.radians(rot); c, s_ = math.cos(r) * sc, math.sin(r) * sc
        L = np.array([[c, -s_, pos[0]], [s_, c, pos[1]], [0, 0, 1]])
        M2 = M @ L
        if n.spr and n.size and al * alpha > 0.003:
            name = n.sprite_at(t); im = sprite(name)
            w, h = n.size
            if n.swapResize and name != n.spr:
                k = h / max(1, sprite(n.spr).size[1]); w, h = im.size[0] * k, im.size[1] * k
            # 그림 픽셀 (u, v) → 로컬 (x, y): x = (u / iw - px) * w, y = (1 - v / ih - py) * h
            iw, ih = im.size
            A = np.array([[w / iw, 0, -n.pivot[0] * w], [0, -h / ih, (1 - n.pivot[1]) * h], [0, 0, 1]])
            S = np.array([[SC, 0, W * SC / 2], [0, -SC, H * SC / 2], [0, 0, 1]])        # 화면 (가운데 0, 위 +) → 캔버스 px
            F = S @ M2 @ A
            corners = F @ np.array([[0, iw, iw, 0], [0, 0, ih, ih], [1, 1, 1, 1]])
            if corners[0].max() < 0 or corners[0].min() > canvas.width or corners[1].max() < 0 or corners[1].min() > canvas.height: pass
            else:
                if abs(np.linalg.det(F[:2, :2])) > 1e-6:          # 크기 0 (펑 튀어나오기 전) = 안 그림
                    inv = np.linalg.inv(F)
                    lay = im.transform(canvas.size, Image.AFFINE, tuple(inv[:2].flatten()), resample=Image.BILINEAR)
                    a = al * (alpha if not n.group else 1)
                    if a < 0.999:
                        arr = np.asarray(lay).copy(); arr[..., 3] = (arr[..., 3] * a).astype(np.uint8); lay = Image.fromarray(arr, 'RGBA')
                    canvas.alpha_composite(lay)
        for ch in n.children: walk(ch, M2, alpha)

    walk(root, np.eye(3), 1.0)
    return canvas.convert('RGB')


def main():
    global CODE
    CODE, out = sys.argv[1], sys.argv[2]
    ts = [float(x) for x in sys.argv[3:]]
    from superult_story import Story, STORIES, NO_FINISH
    st = Story(CODE, ART, ROOT)
    STORIES[CODE](st, f'root_{CODE}.transform')
    root = parse(st.o)
    end = NO_FINISH.get(CODE, 5.0)
    os.makedirs(out, exist_ok=True)
    if not ts:
        frames = [render(root, 0.4 + i / 30) for i in range(int((end - 0.4) * 30))]
        frames[0].save(os.path.join(out, f'{CODE}_preview.gif'), save_all=True, append_images=frames[1:], duration=33, loop=0)
        print('gif', len(frames))
        ts = [0.5, 0.8, 1.1, 1.5, 1.72, 1.9, 2.2, 2.45, 2.75, 2.95, 3.15, 3.3, 3.55, 3.85, 4.3, 4.7, 5.1, 5.3]
    cells = [render(root, t) for t in ts]
    cw, ch = cells[0].size; cols = 3
    sheet = Image.new('RGB', (cw * cols // 2, ch * ((len(cells) + cols - 1) // cols) // 2), 'white')
    from PIL import ImageDraw
    for i, (t, c) in enumerate(zip(ts, cells)):
        c = c.resize((cw // 2, ch // 2)); ImageDraw.Draw(c).text((6, 4), f'{t:.2f}s', fill=(255, 255, 0))
        sheet.paste(c, ((i % cols) * cw // 2, (i // cols) * ch // 2))
    sheet.save(os.path.join(out, f'{CODE}_caps.png')); print('caps', len(ts))


if __name__ == '__main__':
    main()
