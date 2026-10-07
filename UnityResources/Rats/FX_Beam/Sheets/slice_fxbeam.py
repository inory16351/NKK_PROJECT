import sys, os, uuid, shutil
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, r'C:/Project/NKK_PROJECT/Tools')
from slice_rat_parts import key_magenta

SRC = r'C:/Project/NKK_PROJECT/UnityResources/Rats/FX_Beam'
UN = r'C:/Project/NKK_PROJECT/Unity_Making/NKK_Project/Assets/Art/Rats'
META = UN + '/UltProps/ult_pot.png.meta'
names = ['fx_bolt', 'fx_bolt_seg', 'fx_spark', 'fx_beam_body', 'fx_beam_flare', 'fx_beam_hit', 'fx_slash_arc', 'fx_slash_streak', 'fx_slash_cross']
im = key_magenta(Image.open(SRC + '/Sheets/fx_beam.png'))
A = np.array(im)
W = im.width / 3
lab, n = ndimage.label(A[..., 3] > 20)
cells = {i: np.zeros(A.shape[:2], bool) for i in range(9)}
for i, sl in enumerate(ndimage.find_objects(lab), 1):
    m = lab[sl] == i
    if m.sum() < 30: continue
    cy, cx = ndimage.center_of_mass(lab == i)
    c = int(cy // W) * 3 + int(cx // W)
    cells[c] |= lab == i
outs = []
for c, nm in enumerate(names):
    m = cells[c]
    ys, xs = np.where(m)
    pad = 4
    x0, x1, y0, y1 = xs.min() - pad, xs.max() + 1 + pad, ys.min() - pad, ys.max() + 1 + pad
    B = A.copy(); B[~ndimage.binary_dilation(m, iterations=3)] = 0
    out = Image.fromarray(B[y0:y1, x0:x1], 'RGBA')
    if nm == 'fx_beam_body':
        # 가운데 세로줄 하나를 가로로 복제 → 완전히 균일한 띠 (늘려도 같음)
        a = np.array(out); col = a[:, a.shape[1] // 2:a.shape[1] // 2 + 1]
        out = Image.fromarray(np.repeat(col, 128, axis=1), 'RGBA')
    out.save(f'{SRC}/{nm}.png'); outs.append(out)
    for d in ('FX_Beam', 'FX'):
        os.makedirs(f'{UN}/{d}', exist_ok=True)
        shutil.copy(f'{SRC}/{nm}.png', f'{UN}/{d}/{nm}.png')
        mp = f'{UN}/{d}/{nm}.png.meta'
        if not os.path.exists(mp):
            t = open(META, encoding='utf-8').read().replace('385e8c90aca59b546a851989bd386e67', uuid.uuid4().hex)
            t = t.replace('spriteID: 5e97eb03825dee720800000000000000', 'spriteID: ' + uuid.uuid4().hex)
            open(mp, 'w', encoding='utf-8', newline='\n').write(t)
    print(nm, out.size)
# 미리보기 (어두운 바탕)
pw = 1400; prev = Image.new('RGBA', (pw, 560), (40, 44, 60, 255)); x = y = 10; rh = 0
for o in outs:
    o = o.copy(); o.thumbnail((260, 260))
    if x + o.width > pw: x = 10; y += rh + 10; rh = 0
    prev.alpha_composite(o, (x, y)); x += o.width + 20; rh = max(rh, o.height)
prev.save(SRC + '/_미리보기.png')
