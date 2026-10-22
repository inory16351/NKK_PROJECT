"""보스 개편 Codex 시트 자르기 (2026-10-22). 시트 = UnityResources/Rats/BossRework/Sheets/<시트>.png (Tools/gen_boss_rework_prompts.py 칸 순서)
결과 (원본 보관 → Assets 복사):
  hr1 hr2  사람 보스 (머리·화난·겁먹은·몸통·팔·다리, headScale 0.8) → Humans/Parts/<code>/ → Assets/Art/Rats/Humans/Parts/<code>/
  cr1 cr2  고양이 보스 5열 (Tools/slice_cat_parts.py --boss) + 6열 두 번째 머리 head2.png → Cats/Parts/<code>/ → Assets/Art/Rats/Cats/Parts/<code>/
  cx1 cx2 tt1  전용 리그 보스 파츠 → BossRework/Parts/<code>/ → Assets/Art/Rats/BossParts/<code>/ (붙는 자리 = NKK/Build Boss Part Art)
  pr1      소환물·소품 → BossRework/Props/ → Assets/Art/Rats/BossParts/props/
  rt1      우주복 쥐 1행 = 쥐 파츠 (Tools/slice_rat_parts.py) · 2행 = 정체 파츠 → BossParts/boss_crewmate/
그다음: python Tools/gen_sprite_pivots.py → 유니티 NKK/Reimport Art Sprites · Build Human/Cat/Rat Art Library · Build Boss Part Art
사용: python Tools/slice_boss_rework.py [시트...]  (생략 = 있는 시트 전부)"""
import os, subprocess, sys, tempfile
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta, crop
from slice_human_parts import biggest
from slice_expansion import humans, copy_to, ART, RES

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BR = os.path.join(RES, 'BossRework')
SHEETS = os.path.join(BR, 'Sheets')

HUMAN = {'hr1': ['boss_vmouse', 'boss_dino2', 'boss_trainer'], 'hr2': ['boss_operator', 'boss_deepdiver'], 'hr3': ['boss_joassu']}
CAT = {'cr1': ['-', '-', '-'], 'cr3': ['boss_popcat'], 'cr4': ['boss_union2', '-'], 'cr2': ['boss_goerem', 'boss_dictator', 'boss_gremlin']}
# 전용 리그: 칸 순서대로 (코드, 파츠 이름)
CX1 = [('boss_spider', 'head'), ('boss_spider', 'head2'), ('boss_spider', 'body'), ('boss_spider', 'leg'), ('boss_spider', 'leg2'),
       ('boss_sharkcat', 'head'), ('boss_sharkcat', 'head2'), ('boss_sharkcat', 'body'), ('boss_sharkcat', 'leg'), ('boss_sharkcat', 'fin'),
       ('boss_fishcat', 'head'), ('boss_fishcat', 'head2'), ('boss_fishcat', 'body'), ('boss_fishcat', 'tail'), ('boss_fishcat', 'fin')]
CX2_BOTTOM = [('boss_carrier', 'tower'), ('boss_carrier', 'tower2'), ('boss_carrier', 'jet'), ('boss_carrier', 'jet2')]
TT1 = [('boss_cartgiant', n) for n in ('body', 'butt', 'thigh', 'shin', 'uparm', 'forearm', 'head', 'head2')]
PR1 = ['raptor', 'raptor_pounce', 'excavator_body', 'excavator_arm', 'egg', 'egg_crack', 'cat_turret', 'tentacle', 'egg_shells', 'club', 'rebar', 'cone']
RT1_ROW2 = ['jaw', 'lid', 'portrait', 'tongue', 'sticker']


def part_dirs(code):
    return os.path.join(BR, 'Parts', code), os.path.join(ART, 'BossParts', code)


def save(img, code, name):
    if img is None: print('  빈 칸', code, name); return
    src, dst = part_dirs(code)
    os.makedirs(src, exist_ok=True)
    f = os.path.join(src, name + '.png'); img.save(f); copy_to(f, os.path.join(dst, name + '.png'))


def blob(im, box, dilate=4, lab=None):
    """칸(box) 안에 '가운데가 있는' 덩어리를 통째로 (칸 경계를 넘어간 꼬리·입·땀방울도 안 잘림). 칸 밖에 가운데가 있는 옆 칸 그림은 뺌"""
    from scipy import ndimage
    a = np.array(im)
    if lab is None: lab, _ = ndimage.label(ndimage.binary_dilation(a[..., 3] > 20, iterations=dilate))
    x0, y0, x1, y1 = box; keep = np.zeros(lab.shape, bool)
    for k, sl in enumerate(ndimage.find_objects(lab), 1):
        if not sl: continue
        m = (lab[sl] == k) & (a[sl][..., 3] > 20)
        ys, xs = np.nonzero(m)
        if len(xs) < 150: continue
        cx, cy = xs.mean() + sl[1].start, ys.mean() + sl[0].start
        if x0 <= cx < x1 and y0 <= cy < y1: keep[sl] |= lab[sl] == k
    out = a.copy(); out[..., 3] = np.where(keep, out[..., 3], 0)
    return crop(Image.fromarray(out))


def cell_imgs(im, cols, rows, y0=0, y1=None, dilate=4):
    from scipy import ndimage
    lab, _ = ndimage.label(ndimage.binary_dilation(np.array(im)[..., 3] > 20, iterations=dilate))
    W = im.width; y1 = im.height if y1 is None else y1; H = y1 - y0
    for r in range(rows):
        for c in range(cols):
            yield blob(im, (c * W / cols, y0 + r * H / rows, (c + 1) * W / cols, y0 + (r + 1) * H / rows), lab=lab)


def regrid(sheet, rows, cols, y0=0, y1=None, dilate=6):
    """칸 경계를 넘은 그림(몸통 꼬리가 옆 칸 팔에 붙는 등) 방지: 줄마다 덩어리 cols 개를 찾아 x 순서대로 넉넉한 새 칸 가운데에 다시 놓은 시트 (RGB, 자홍 배경)"""
    from scipy import ndimage
    src = Image.open(sheet).convert('RGB'); y1 = src.height if y1 is None else y1
    a = np.array(key_magenta(src))[..., 3] > 20
    lab, n = ndimage.label(ndimage.binary_dilation(a, iterations=dilate))
    objs = ndimage.find_objects(lab)
    h = (y1 - y0) / rows; parts = []
    for r in range(rows):
        comps = [(k + 1, sl) for k, sl in enumerate(objs) if sl and y0 + r * h <= (sl[0].start + sl[0].stop) / 2 < y0 + (r + 1) * h]
        comps = sorted(comps, key=lambda c: -int((lab[c[1]] == c[0]).sum()))[:cols]
        comps.sort(key=lambda c: c[1][1].start)
        if len(comps) < cols: print(f'  {os.path.basename(sheet)} {r + 1}줄: 덩어리 {len(comps)}개')
        row = []
        for k, sl in comps:
            m = Image.fromarray(((lab[sl] == k) * 255).astype(np.uint8))
            bg = Image.new('RGB', m.size, (255, 0, 255)); bg.paste(src.crop((sl[1].start, sl[0].start, sl[1].stop, sl[0].stop)), (0, 0), m)
            row.append(bg)
        parts.append(row)
    cw = max(p.width for row in parts for p in row) + 40; ch = max(p.height for row in parts for p in row) + 40
    out = Image.new('RGB', (cw * cols, ch * rows), (255, 0, 255))
    for r, row in enumerate(parts):
        for c, p in enumerate(row): out.paste(p, (c * cw + (cw - p.width) // 2, r * ch + (ch - p.height) // 2))
    f = os.path.join(tempfile.mkdtemp(), os.path.basename(sheet)); out.save(f)
    return f


def cats(s, sheet):
    from scipy import ndimage
    ids = CAT[s]
    src = Image.open(sheet).convert('RGB'); W, H = src.size
    im = key_magenta(src); a = np.array(im)[..., 3] > 20
    lab, _ = ndimage.label(ndimage.binary_dilation(a, iterations=4))
    # 6열(두 번째 머리) 덩어리만 통째로 지우고 기존 5열 자르기에 넘김 (칸 경계로 자르면 입·수염이 잘리거나 조각이 남음)
    five = np.array(src); heads = {}
    for r, cid in enumerate(ids):
        box = (W * 5 / 6, r * H / len(ids), W, (r + 1) * H / len(ids))
        heads[cid] = blob(im, box, lab=lab)
        for k, sl in enumerate(ndimage.find_objects(lab), 1):
            if not sl: continue
            ys, xs = np.nonzero((lab[sl] == k) & a[sl])
            if len(xs) and box[0] <= xs.mean() + sl[1].start and box[1] <= ys.mean() + sl[0].start < box[3]:
                five[sl][lab[sl] == k] = (255, 0, 255)
    tmp = os.path.join(tempfile.mkdtemp(), s + '.png'); Image.fromarray(five).save(tmp)
    subprocess.check_call([sys.executable, '-I', os.path.join(ROOT, 'Tools', 'slice_cat_parts.py'), tmp, str(len(ids)), ','.join(ids), '--boss'])
    for cid in ids:
        if cid == '-': continue
        d = os.path.join(RES, 'Cats', 'Parts', cid)
        if heads[cid] is not None: heads[cid].save(os.path.join(d, 'head2.png'))
        for f in os.listdir(d): copy_to(os.path.join(d, f), os.path.join(ART, 'Cats', 'Parts', cid, f))
        print('  고양이', cid)


def run(s):
    sheet = os.path.join(SHEETS, s + '.png')
    if not os.path.exists(sheet): return
    print(s)
    if s in HUMAN: humans(regrid(sheet, len(HUMAN[s]), 6), HUMAN[s], ['head', 'angry', 'scared', 'torso', 'arm', 'leg'], 0.8)
    elif s in CAT: cats(s, sheet)
    elif s == 'cx1':
        for (code, name), p in zip(CX1, cell_imgs(key_magenta(Image.open(regrid(sheet, 3, 5, dilate=2))), 5, 3)): save(p, code, name)
    elif s == 'cx2':
        im = key_magenta(Image.open(sheet)); top = int(im.height * 0.45)
        save(blob(im, (0, 0, im.width, top)), 'boss_carrier', 'hull')
        for (code, name), p in zip(CX2_BOTTOM, cell_imgs(im, 4, 1, top)):
            if name.startswith('tower') and os.path.exists(os.path.join(SHEETS, 'cx3.png')): continue      # 관제탑은 cx3 (새 그림) 이 대신함
            save(p, code, name)
    elif s == 'cx3':      # 관제탑 다시 (기계 고양이 머리, 눈 = 창문): 왼쪽 = 보통 · 오른쪽 = 소리침
        for name, p in zip(['tower', 'tower2'], cell_imgs(key_magenta(Image.open(sheet)), 2, 1)): save(p, 'boss_carrier', name)
    elif s == 'rt2':      # 우주복 쥐 위턱 · 아래턱 (정체 드러낼 때 경첩으로 벌어짐)
        for name, p in zip(['upper', 'lower'], cell_imgs(key_magenta(Image.open(sheet)), 2, 1)): save(p, 'boss_crewmate', name)
    elif s == 'tt1':
        for (code, name), p in zip(TT1, cell_imgs(key_magenta(Image.open(regrid(sheet, 2, 4))), 4, 2)): save(p, code, name)
    elif s == 'pr1':
        for name, p in zip(PR1, cell_imgs(key_magenta(Image.open(sheet)), 4, 3)):
            if p is None: continue
            f = os.path.join(BR, 'Props', name + '.png'); os.makedirs(os.path.dirname(f), exist_ok=True); p.save(f)
            copy_to(f, os.path.join(ART, 'BossParts', 'props', name + '.png'))
        print('  소품', len(PR1))
    elif s == 'rt1':
        src = Image.open(sheet).convert('RGB'); W, H = src.size
        top = src.crop((0, 0, W, H // 2)); tmp = os.path.join(tempfile.mkdtemp(), 'rt1_top.png'); top.save(tmp)
        subprocess.check_call([sys.executable, '-I', os.path.join(ROOT, 'Tools', 'slice_rat_parts.py'), tmp, '1', 'crewmate'])
        d = os.path.join(RES, 'Parts', 'crewmate')
        for f in os.listdir(d): copy_to(os.path.join(d, f), os.path.join(ART, 'Parts', 'crewmate', f))
        for name, p in zip(RT1_ROW2, cell_imgs(key_magenta(src), 5, 1, H // 2)): save(p, 'boss_crewmate', name)


if __name__ == '__main__':
    for s in (sys.argv[1:] or ['hr1', 'hr2', 'hr3', 'cr2', 'cr3', 'cr4', 'cx1', 'cx2', 'cx3', 'tt1', 'pr1', 'rt1', 'rt2']): run(s)
