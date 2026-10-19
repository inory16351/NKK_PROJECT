"""100층 확장 Codex 시트 자르기 (2026-10-19). 시트 = UnityResources/Rats/Expansion100/Sheets/<시트>.png (Tools/gen_expansion_prompts.py 칸 순서)
결과: UnityResources/Rats/Expansion100/<분류>/ (원본 보관) + Assets/Art/Rats/... 로 복사
  z1~z6  물건 18칸 → Expansion100/Items/<code>.png → Assets/Art/Rats/Expansion/Items/
  h1~h6  사람 4명 (머리·겁먹은 머리·몸통·팔·다리) → Humans/Parts/<code>/ + Humans/Parts/pivots.json → Assets/Art/Rats/Humans/Parts/<code>/
  hb1~2  사람 보스 3명 (머리·화난 머리·겁먹은 머리·몸통·팔·다리, headScale 0.8)
  cb1~4  고양이 보스 (Tools/slice_cat_parts.py --boss) → Cats/Parts/<code>/ → Assets/Art/Rats/Cats/Parts/<code>/
  mp1~2  밈 소품 18칸 → Expansion100/Memes/bm_*.png → Assets/Art/Rats/BossMeme/
  badges 훈장 9~16 → Rogue/rk_9~16 · 새 스킬 아이콘 → SkillIcons/cs_autopromote · cs_needdown (Assets/Art/Rats/Rogue · SkillIcons)
  floors · walls 구간 바닥 6 (640×544) · 벽 6 (높이 156) → Assets/Art/Stage/
그다음: python Tools/gen_sprite_pivots.py → 유니티 NKK/Reimport Art Sprites · Build Human/Cat Art Library · ItemManager Fill Item Sprites · Boss Fill Meme Sprites
사용: python Tools/slice_expansion.py [시트...]  (생략 = 있는 시트 전부)"""
import io, json, os, shutil, subprocess, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta, crop
from slice_human_parts import biggest, band_x
import expansion_content as C

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, 'UnityResources', 'Rats')
EXP = os.path.join(RES, 'Expansion100')
SHEETS = os.path.join(EXP, 'Sheets')
ART = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats')
STAGE = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Stage')

MEMES = {'mp1': ['bm_note', 'bm_happy_mini', 'bm_pop_wave', 'bm_cursor', 'bm_rock', 'bm_torch', 'bm_monke', 'bm_meteor', 'bm_raptor', 'bm_dart', 'bm_bucket', 'bm_concrete',
                 'bm_picket', 'bm_megaphone', 'bm_zzz', 'bm_rice_bowl', 'bm_rice_mountain', 'bm_ladle'],
         'mp2': ['bm_bubble', 'bm_wave', 'bm_fish', 'bm_anchor', 'bm_ink', 'bm_baby_shark', 'bm_fin', 'bm_sharknado', 'bm_shell', 'bm_airdrop', 'bm_pencil', 'bm_vent',
                 'bm_sus', 'bm_wrench', 'bm_moon', 'bm_joassu', 'bm_shades', 'bm_stop']}
CAT_ROWS = {'cb1': 'boss_happy,boss_pop,boss_union', 'cb2': 'boss_mermaid,boss_diver,boss_shark', 'cb3': 'boss_turret,boss_ryan,boss_fuhrer', 'cb4': 'boss_lasercat,boss_grumpy,-'}
HB_ROWS = {'hb1': ['boss_caveman', 'boss_keeper', 'boss_dino'], 'hb2': ['boss_kim', 'boss_hamba', 'boss_red']}
ZKEY = {'1': 'catlab', '2': 'dino', '3': 'construction', '4': 'ocean', '5': 'war', '6': 'space'}


def copy_to(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True); shutil.copy2(src, dst)


def cells(sheet, cols, rows, inset=4):
    im = key_magenta(Image.open(sheet)); W, H = im.size
    for r in range(rows):
        for c in range(cols):
            yield r, c, im.crop((int(c * W / cols) + inset, int(r * H / rows) + inset, int((c + 1) * W / cols) - inset, int((r + 1) * H / rows) - inset))


def grid(sheet, cols, rows, names, out_dir, art_dir):
    n = 0
    for r, c, cell in cells(sheet, cols, rows):
        i = r * cols + c
        if i >= len(names) or names[i] == '-': continue
        p = crop(biggest(cell))
        if p is None: print('  빈 칸', names[i]); continue
        os.makedirs(out_dir, exist_ok=True)
        f = os.path.join(out_dir, names[i] + '.png'); p.save(f)
        if art_dir: copy_to(f, os.path.join(art_dir, names[i] + '.png'))
        n += 1
    return n


def humans(sheet, ids, order, head_scale=1.0):
    W_COLS = len(order)
    im = key_magenta(Image.open(sheet)); W, H = im.size
    cw, ch = W / W_COLS, H / len(ids)
    pj = os.path.join(RES, 'Humans', 'Parts', 'pivots.json')
    allm = json.load(io.open(pj, encoding='utf-8'))
    for r, cid in enumerate(ids):
        if cid == '-': continue
        out = os.path.join(RES, 'Humans', 'Parts', cid); os.makedirs(out, exist_ok=True)
        parts = {}
        for c, name in enumerate(order):
            p = crop(biggest(im.crop((int(c * cw) + 3, int(r * ch) + 3, int((c + 1) * cw) - 3, int((r + 1) * ch) - 3))))
            if p is None: raise SystemExit(f'{cid} {name}: 빈 칸')
            p.save(os.path.join(out, name + '.png')); parts[name] = p
        if 'angry' not in parts: parts['angry'] = None
        piv = {k: [band_x(parts[k], False), 0.97] for k in ('head', 'angry', 'scared') if parts.get(k)}
        piv.update({k: [band_x(parts[k], True), 0.04] for k in ('arm', 'leg')})
        t = parts['torso']; nx = band_x(t, True)
        anchor = {'neck': [nx, 0.03], 'shoulder': [nx, 0.12], 'hip': [band_x(t, False), 0.95]}
        allm[cid] = {'size': {k: list(v.size) for k, v in parts.items() if v}, 'pivot': piv, 'anchor': anchor,
                     'pivotUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in piv.items()},
                     'anchorUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in anchor.items()}, 'headScale': head_scale}
        dst = os.path.join(ART, 'Humans', 'Parts', cid)
        for f in os.listdir(out): copy_to(os.path.join(out, f), os.path.join(dst, f))
        print('  사람', cid)
    json.dump(allm, io.open(pj, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)


def floors(sheet):
    names = C.FLOOR_ART
    for r, c, cell in cells(sheet, 3, 2, inset=14):
        i = r * 3 + c
        a = np.array(cell); ys, xs = np.nonzero(a[..., 3] > 250)
        if len(xs) == 0: continue
        sub = cell.crop((xs.min() + 4, ys.min() + 4, xs.max() - 4, ys.max() - 4)).convert('RGB').resize((640, 544), Image.LANCZOS)
        f = os.path.join(EXP, 'Stage', names[i] + '.png'); os.makedirs(os.path.dirname(f), exist_ok=True); sub.save(f)
        copy_to(f, os.path.join(STAGE, names[i] + '.png'))
    print('  바닥 6')


def walls(sheet):
    im = key_magenta(Image.open(sheet)); a = np.array(im)[..., 3] > 200
    rows = a.mean(axis=1) > 0.6
    bands, y = [], 0
    while y < len(rows):
        if rows[y]:
            y0 = y
            while y < len(rows) and rows[y]: y += 1
            if y - y0 > 60: bands.append((y0, y))
        y += 1
    for i, (y0, y1) in enumerate(bands[:6]):
        line = a[(y0 + y1) // 2]; xs = np.nonzero(line)[0]
        sub = im.crop((xs.min() + 2, y0 + 1, xs.max() - 2, y1 - 1)).convert('RGB')
        sub = sub.resize((int(sub.width * 156 / sub.height), 156), Image.LANCZOS)
        f = os.path.join(EXP, 'Stage', C.WALL_ART[i] + '.png'); os.makedirs(os.path.dirname(f), exist_ok=True); sub.save(f)
        copy_to(f, os.path.join(STAGE, C.WALL_ART[i] + '.png'))
    print('  벽', len(bands[:6]))


def run(s):
    sheet = os.path.join(SHEETS, s + '.png')
    if not os.path.exists(sheet): return
    print(s)
    if s.startswith('z'):
        zi = int(s[1]); names = [c[0] for c in C.ZONE_ITEMS[zi]]
        print('  물건', grid(sheet, 6, 3, names, os.path.join(EXP, 'Items'), os.path.join(ART, 'Expansion', 'Items')))
    elif s[0] == 'h' and s[1].isdigit():
        zi = int(s[1]); humans(sheet, [h[0] for h in C.ZONE_HUMANS[zi]], ['head', 'scared', 'torso', 'arm', 'leg'])
    elif s in HB_ROWS:
        humans(sheet, HB_ROWS[s], ['head', 'angry', 'scared', 'torso', 'arm', 'leg'], 0.8)
    elif s in CAT_ROWS:
        subprocess.check_call([sys.executable, '-I', os.path.join(ROOT, 'Tools', 'slice_cat_parts.py'), sheet, '3', CAT_ROWS[s], '--boss'])
        for cid in CAT_ROWS[s].split(','):
            if cid == '-': continue
            src = os.path.join(RES, 'Cats', 'Parts', cid)
            for f in os.listdir(src): copy_to(os.path.join(src, f), os.path.join(ART, 'Cats', 'Parts', cid, f))
    elif s in MEMES:
        print('  소품', grid(sheet, 6, 3, MEMES[s], os.path.join(EXP, 'Memes'), os.path.join(ART, 'BossMeme')))
    elif s == 'badges':
        grid(sheet, 4, 3, [f'rk_{t}' for t in range(9, 17)] + ['-'] * 4, os.path.join(EXP, 'Rank'), os.path.join(ART, 'Rogue'))
        grid(sheet, 4, 3, ['-'] * 8 + ['cs_autopromote', 'cs_needdown', 'cs_promote2', 'cs_skip2'], os.path.join(EXP, 'Rank'), os.path.join(ART, 'SkillIcons'))
        print('  훈장 8 · 아이콘 4')
    elif s == 'floors': floors(sheet)
    elif s == 'walls': walls(sheet)


if __name__ == '__main__':
    ALL = ['z1_catlab', 'z2_dino', 'z3_construction', 'z4_ocean', 'z5_war', 'z6_space', 'h1_catlab', 'h2_dino', 'h3_construction', 'h4_ocean', 'h5_war', 'h6_space',
           'hb1', 'hb2', 'cb1', 'cb2', 'cb3', 'cb4', 'mp1', 'mp2', 'badges', 'floors', 'walls']
    for s in (sys.argv[1:] or ALL): run(s)
