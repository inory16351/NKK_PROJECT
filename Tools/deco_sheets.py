"""도토리 상점 장식 그림 (2026-11-04): 원본 그림을 격자로 늘어놓은 참고 시트 → Codex 가 같은 칸에 테마만 바꿔 그림 → 칸별로 키잉·잘라 원본 크기로
사용:
  python Tools/deco_sheets.py ref  <테마> <시트>   → UnityResources/Rats/Deco/<테마>/<시트>/ref_grid.png · layout.json · prompt.txt
  python Tools/deco_sheets.py cut  <테마> <시트> [green]  → out/<원본 이름>_<테마>.png (원본과 같은 캔버스 크기, 투명 배경) · check.png
테마 = forest · neon. 시트 = SHEETS 키."""
import json, os, sys
import numpy as np
from PIL import Image, ImageFilter
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta
from lobby_split_cut import key_green

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art')
OUTROOT = os.path.join(ROOT, 'UnityResources', 'Rats', 'Deco')

# 시트: (캔버스, 열, 행, [원본 경로 (Assets/Art 기준)], 설명)
SHEETS = {
    'badges': ((1536, 1024), 4, 2, [f'Rats/Rogue/rk_{i}.png' for i in range(1, 9)],
               'the 8 rank BADGES/medals of the hideout (rank 1 to 8, left to right, top row then bottom row). Each badge must keep its OWN distinct shape, silhouette and central symbol so the 8 ranks stay clearly different and in the same order of importance (later = fancier); only the materials, colors and small decorations change to the theme'),
    'wheel': ((1536, 1024), 2, 1, ['Rats/Lobby/lb_wheel_ring.png', 'Rats/Lobby/lb_wheel_stand.png'],
              'LEFT = the round hamster-wheel RING (seen from the front: outer rim, spokes and running rungs, the center axle hub; the inside must stay mostly OPEN/see-through so the rat running inside is visible), RIGHT = its STAND (the legs/base that hold the wheel axle). Keep the ring perfectly round with the same diameter and rim thickness, and the stand with the same footprint and axle height'),
    'manhole': ((1536, 1024), 2, 1, ['Rats/Lobby_Market/lobby_manhole_closed.png', 'Rats/Lobby_Market/lobby_manhole_open.png'],
                'LEFT = the sewer MANHOLE in the floor seen at a low angle, CLOSED (lid on), RIGHT = the same manhole OPEN (lid slid aside, dark hole visible). Same ellipse size and angle as the originals'),
    'alley_a': ((1536, 1024), 2, 1, ['Rats/Lobby_Market/lm_arcade.png', 'Rats/Lobby_Market/lm_stall.png'],
                'two buildings/objects of the underground back-alley: LEFT = the MINI ARCADE MACHINE (tall cabinet with a screen, joystick and buttons), RIGHT = the BLACK-MARKET STALL (a small vendor stall with an awning and counter). Keep each one\'s silhouette, proportions, screen/counter positions and size'),
    'alley_b': ((1536, 1024), 2, 1, ['Rats/Lobby_Market/lm_lab.png', 'Rats/Lobby_Market/aug_lab.png'],
                'two buildings of the underground back-alley: LEFT = the SECRET LAB building (tall narrow lab house where rats are bred/gacha), RIGHT = the AUGMENT LAB building (wide lab building). Keep each one\'s silhouette, proportions, door and window positions and size'),
    'ui': ((1536, 1024), 4, 3, ['UI_Kit/ui_plank.png', 'UI_Kit/ui_plank_green.png', 'UI_Kit/ui_plank_red.png', 'UI_Kit/ui_plank_gray.png',
                               'UI_Kit/ui_btn_round.png', 'UI_Kit/ui_btn_round_green.png', 'UI_Kit/ui_pill.png', 'UI_Kit/ui_key.png',
                               '../../../../UnityResources/Rats/Cursor/cursor_normal_big.png', '../../../../UnityResources/Rats/Cursor/cursor_pressed_big.png'],
           'UI BUTTON pieces of the game: row 1 = four wide plank buttons (default / green = confirm / red = cancel / gray = disabled - keep this color meaning recognizable within the theme palette), row 2 = round button, round green button, pill button, square key button, row 3 = mouse cursor NORMAL and mouse cursor PRESSED (a small rat-paw / pointer shape, the click point at the top-left tip). The planks, pill and key are 9-slice stretched in game: keep their edges/corners decorated but the MIDDLE area plain and evenly colored, keep the same rectangle proportions and the same border thickness. Leave the button faces EMPTY (no text, no icons)'),
    'cursor': ((1536, 1024), 2, 1, ['../../../../UnityResources/Rats/Cursor/cursor_normal_big.png', '../../../../UnityResources/Rats/Cursor/cursor_pressed_big.png'],
               'the MOUSE CURSOR of the game: LEFT = cursor NORMAL, RIGHT = cursor PRESSED (shown while the mouse button is held: the same cursor slightly squashed/tilted with a small flat "click" accent). The click point (hotspot) is the SHARP TIP at the TOP-LEFT corner of the shape - the tip must point to the upper-left like a classic arrow pointer and touch the top-left of the object. Keep a thick, clearly contrasting outer edge so the cursor stays readable on both light and dark backgrounds (this is the one exception to the no-outline rule: a flat dark or bright rim is allowed). It must read clearly when shrunk to 48x48 pixels: one bold simple shape, few details'),
    'ui2': ((1536, 1024), 4, 2, ['UI_Kit/ui_plank.png', 'UI_Kit/ui_plank_green.png', 'UI_Kit/ui_plank_red.png', 'UI_Kit/ui_plank_gray.png',
                                'UI_Kit/ui_btn_round.png', 'UI_Kit/ui_btn_round_green.png', 'UI_Kit/ui_pill.png', 'UI_Kit/ui_key.png'],
            'the UI BUTTON pieces of the game (row 1 = four wide plank buttons: default / green = confirm / red = cancel / gray = disabled, row 2 = round button, round green button, pill button, square key button). In the original they all look like WOODEN PLANKS'),
    'ui3': None,
}
SHEETS['ui3'] = SHEETS['ui2']
SHEETS['ui3p'] = ((1536, 1024), 1, 1, ['UI_Kit/ui_plank_red.png'], 'neon red plank → pink (ref = neon ui3 out)')
SHEETS['ui3g'] = ((1536, 1024), 2, 1, ['UI_Kit/ui_plank_green.png', 'UI_Kit/ui_btn_round_green.png'], 'green leaf buttons recolor (ref = ui3 out)')
THEMES = {
    'forest': ('ACORN FOREST', 'cozy autumn acorn forest: light oak and bark wood, acorn caps and acorns, oak leaves, moss, small mushrooms, twine. Soft muted warm palette: cream, tan, oak brown, acorn brown, moss and sage green, soft autumn orange, a little muted mushroom red. NO metal anywhere (no metal plates, bolts or brackets) - use wooden pegs and twine instead'),
    'neon': ('NEON ARCADE', 'retro game arcade at night: dark violet/indigo bodies with flat bright neon tube strokes in hot pink, cyan, lemon yellow and lime green, pixel hearts and pixel stars, arcade buttons, joystick details, small marquee dots. Neon is drawn as flat colored strokes with no glow, no bloom'),
}
STYLE = 'STYLE: Untitled Goose Game flat look, the same as the game. Completely FLAT color shapes, NO black outlines, NO linework, no gloss, no gradients, no glow, no drop shadows, no texture; each object = a few flat color areas with one slightly darker flat tone. No text, no letters, no numbers, no logos, no characters, no animals, no people.'


def grid(canvas, cols, rows):
    W, H = canvas
    cw, ch = W // cols, H // rows
    return [(c * cw, r * ch, (c + 1) * cw, (r + 1) * ch) for r in range(rows) for c in range(cols)]


def make_ref(theme, sheet):
    canvas, cols, rows, items, desc = SHEETS[sheet]
    d = os.path.join(OUTROOT, theme, sheet); os.makedirs(d, exist_ok=True)
    ref = Image.new('RGBA', canvas, (255, 0, 255, 255))
    cells = grid(canvas, cols, rows)
    lay = []
    for (x0, y0, x1, y1), p in zip(cells, items):
        im = Image.open(os.path.join(ART, p)).convert('RGBA')
        pad = 0.12
        s = min((x1 - x0) * (1 - pad) / im.width, (y1 - y0) * (1 - pad) / im.height)
        r = im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS)
        ox, oy = x0 + ((x1 - x0) - r.width) // 2, y0 + ((y1 - y0) - r.height) // 2
        ref.alpha_composite(r, (ox, oy))
        lay.append(dict(src=p, cell=[x0, y0, x1, y1], box=[ox, oy, ox + r.width, oy + r.height], size=[im.width, im.height]))
    ref.convert('RGB').save(os.path.join(d, 'ref_grid.png'))
    json.dump(lay, open(os.path.join(d, 'layout.json'), 'w'), indent=1)
    tname, tdesc = THEMES[theme]
    bg = 'pure chroma GREEN #00FF00' if theme == 'neon' else 'pure magenta #FF00FF'
    prompt = f"""Use your built-in image generation tool to create ONE image and save it as PNG to: sheet.png (in the current working directory). Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.

THIS IS A THEMED RESKIN of ref_grid.png ({canvas[0]}x{canvas[1]}). ref_grid.png shows {desc}.
Redraw EVERY object of ref_grid.png in the "{tname}" theme, each one in EXACTLY the same grid cell, at the same position, the same size and the same silhouette/outline as in ref_grid.png (so it can be cut out and swapped 1:1 in the game). Keep how each object is viewed (angle, perspective) and every functional part in the same place.
THEME "{tname}": {tdesc}.
{STYLE}
BACKGROUND: the whole background outside the objects must be ONE solid, completely flat {bg} with no shadows, no floor, no gradient, no halos around the objects. Do not use that background color inside the objects. Keep a clear gap of background between neighbouring objects; never let an object cross into another cell."""
    open(os.path.join(d, 'prompt.txt'), 'w', encoding='utf-8').write(prompt)
    print('ref', d)


FILL = False


def cut(theme, sheet, green=False):
    d = os.path.join(OUTROOT, theme, sheet)
    lay = json.load(open(os.path.join(d, 'layout.json')))
    canvas = SHEETS[sheet][0]
    im = Image.open(os.path.join(d, 'sheet.png')).convert('RGB').resize(canvas, Image.LANCZOS)
    k = (key_green if (green or theme == 'neon') and 'magenta' not in sys.argv[4:] else key_magenta)(im)   # magenta = 네온인데 자홍 바탕으로 뽑은 시트
    out = os.path.join(d, 'out'); os.makedirs(out, exist_ok=True)
    tiles = []
    for L in lay:
        x0, y0, x1, y1 = L['cell']
        c = k.crop((x0, y0, x1, y1))
        a = c.getchannel('A').point(lambda v: 255 if v > 20 else 0).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))
        bb = a.getbbox()
        if not bb: print('없음', L['src']); continue
        obj = c.crop(bb)
        W, H = L['size']
        can = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        if FILL:      # fill = 원본 그림이 차지한 범위(알파 > 128)에 꼭 맞게 늘림 → UI 글 자리가 원본과 같음
            src = Image.open(os.path.join(ART, L['src'])).convert('RGBA')
            ob = src.getchannel('A').point(lambda v: 255 if v > 128 else 0).getbbox()
            r = obj.resize((ob[2] - ob[0], ob[3] - ob[1]), Image.LANCZOS)
            can.alpha_composite(r, (ob[0], ob[1]))
        else:
            s = min(W / obj.width, H / obj.height)          # 원본 캔버스에 맞춤 (가운데 · 바닥 맞춤)
            r = obj.resize((max(1, round(obj.width * s)), max(1, round(obj.height * s))), Image.LANCZOS)
            can.alpha_composite(r, ((W - r.width) // 2, H - r.height))
        name = os.path.splitext(os.path.basename(L['src']))[0]
        can.save(os.path.join(out, f'{name}_{theme}.png'))
        tiles.append((Image.open(os.path.join(ART, L['src'])).convert('RGBA'), can))
    # 확인 그림: 원본 위 · 새 그림 아래
    th = 220
    row = lambda ims: [i.resize((max(1, int(i.width * th / i.height)), th)) for i in ims]
    a, b = row([t[0] for t in tiles]), row([t[1] for t in tiles])
    Wt = max(sum(i.width + 10 for i in a), sum(i.width + 10 for i in b))
    chk = Image.new('RGBA', (Wt, th * 2 + 10), (70, 70, 70, 255))
    for y, rr in ((0, a), (th + 10, b)):
        x = 0
        for i in rr: chk.alpha_composite(i, (x, y)); x += i.width + 10
    chk.save(os.path.join(d, 'check.png'))
    print('cut', d, len(tiles))


if __name__ == '__main__':
    mode, theme, sheet = sys.argv[1:4]
    FILL = 'fill' in sys.argv[4:]
    if mode == 'ref': make_ref(theme, sheet)
    else:
        globals()['FILL'] = FILL
        cut(theme, sheet, 'green' in sys.argv[4:])
