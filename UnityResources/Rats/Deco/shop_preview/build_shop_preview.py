"""도토리 상점 '사기 전에 바뀐 모습 보기' 웹 미리보기 (2026-11-06, 게임에는 아직 안 넣음)
장식마다 '지금'(처음 아지트 · 기본 그림) / '산 뒤' 그림을 게임 그림 파일로 조립 → shop_template.html 에 넣어 한 파일로
→ C:/Project/NKK3/preview/shop_buy_preview.html

- 아지트 배경 · 소품 4종: LobbySplit 빈 배경 + lp_<테마>_<소품> (스프라이트 피벗 = 배경 가운데 기준, 게임과 같은 배치)
- 훈장 · 쳇바퀴 · 맨홀 · 뒷골목 건물: 처음 그림 → 장식 그림 (Assets/Art/Rats/Deco/<테마>/), 배경 위 대략 자리
- 뒷골목 배경: lm_bg → lm_bg_<테마> · UI 버튼: UI_Kit → 장식 판 (9-슬라이스) · 커서: Cursor/
- 쥐 친구: 박쥐 = 뒷골목 미리보기 그림 · 햄찌 = 아지트 미리보기 그림 · 마녀 생쥐 = whole.png 를 아지트에
"""
import base64, io, json, os, re
from PIL import Image, ImageDraw, ImageFont, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '../../../..'))            # NKK_PROJECT
ART = os.path.join(ROOT, 'Unity_Making/NKK_Project/Assets/Art')
DATA = os.path.join(ROOT, 'Unity_Making/NKK_Project/Assets/Data')
FRIENDS = os.path.join(ROOT, 'UnityResources/Rats/RatFriends')
FONT = os.path.join(ROOT, 'Unity_Making/NKK_Project/Assets/Fonts/Jua-Regular.ttf')
OUT = 'C:/Project/NKK3/preview/shop_buy_preview.html'
W, H = 640, 360
THEMES = {'Forest': 'forest', 'Neon': 'neon'}

def A(p): return Image.open(os.path.join(ART, p)).convert('RGBA')
def pivot(p):
    m = open(os.path.join(ART, p) + '.meta', encoding='utf-8').read()
    a = re.search(r'spritePivot: \{x: ([-\d.e]+), y: ([-\d.e]+)\}', m)
    return float(a.group(1)), float(a.group(2))

def crop169(im, cx, cy, w):
    """(cx, cy) 가운데, 폭 w 인 16:9 부분 → W×H"""
    h = w * 9 / 16
    x0 = min(max(cx - w / 2, 0), im.width - w); y0 = min(max(cy - h / 2, 0), im.height - h)
    return im.crop((round(x0), round(y0), round(x0 + w), round(y0 + h))).resize((W, H), Image.LANCZOS)

# ── 아지트: 빈 배경 + 소품 ──
PROPS = {'NapBed': 'bed', 'Bookshelf': 'books', 'CheeseStore': 'shelf', 'Table': 'table'}
def prop_box(theme, p):
    f = f'Rats/UI/LobbySplit/lp_{theme}_{p}.png'; im = A(f); px, py = pivot(f)
    x, y = round(768 - px * im.width), round(512 - (1 - py) * im.height)
    return im, (x, y)
def lobby(bg='base', props=None):
    """props: {소품: 테마} — 없는 소품은 bg 테마"""
    img = A(f'Rats/UI/LobbySplit/lobby_{bg}_empty.png')
    for p in ['bed', 'books', 'shelf', 'table']:
        t = (props or {}).get(p, bg); im, xy = prop_box(t, p); img.alpha_composite(im, xy)
    return img
def lobby_view(img): return crop169(img, 768, 512, 1536)

# ── 그림 모음 (같은 그림은 한 번만) ──
IMGS = {}
def put(key, im, q=86):
    if key not in IMGS:
        buf = io.BytesIO(); im.convert('RGB').save(buf, 'JPEG', quality=q)
        IMGS[key] = 'data:image/jpeg;base64,' + base64.b64encode(buf.getvalue()).decode()
    return key

BASE = lobby('base')
put('lobby_base', lobby_view(BASE))

def fit(im, h):  # 높이 h 로
    return im.resize((max(1, round(im.width * h / im.height)), h), Image.LANCZOS)

def paste_c(bg, im, cx, bottom):
    bg.alpha_composite(im, (round(cx - im.width / 2), round(bottom - im.height)))

# 쳇바퀴 (받침 위에 바퀴)
def wheel(suffix=''):
    ring = A(f'Rats/Deco/{suffix[1:]}/lb_wheel_ring{suffix}.png' if suffix else 'Rats/Lobby/lb_wheel_ring.png')
    stand = A(f'Rats/Deco/{suffix[1:]}/lb_wheel_stand{suffix}.png' if suffix else 'Rats/Lobby/lb_wheel_stand.png')
    c = Image.new('RGBA', (900, 1000), (0, 0, 0, 0))
    c.alpha_composite(stand, (450 - stand.width // 2, 1000 - stand.height))
    c.alpha_composite(ring, (450 - ring.width // 2, 1000 - stand.height - ring.height // 2 + 40))
    return c.crop(c.getbbox())
def wheel_scene(suffix=''):
    s = BASE.copy(); paste_c(s, fit(wheel(suffix), 330), 1130, 850); return crop169(s, 1060, 640, 900)

def manhole(suffix=''):
    im = A(f'Rats/Deco/{suffix[1:]}/lobby_manhole_closed{suffix}.png' if suffix else 'Rats/Lobby_Market/lobby_manhole_closed.png')
    s = BASE.copy(); paste_c(s, fit(im, 110), 768, 905); return crop169(s, 768, 760, 760)

def badges(suffix=''):
    s = BASE.copy(); x0 = 300
    for i in range(1, 9):
        im = A(f'Rats/Deco/{suffix[1:]}/rk_{i}{suffix}.png' if suffix else f'Rats/Rogue/rk_{i}.png')
        im = fit(im, 92); s.alpha_composite(im, (x0 + (i - 1) * 118, 150))
    return crop169(s, 768, 250, 1100)

ALLEY = {'Arcade': 'lm_arcade', 'Market': 'lm_stall', 'Lab': 'lm_lab', 'AugLab': 'aug_lab'}
ALLEY_H = {'Arcade': 430, 'Market': 250, 'Lab': 430, 'AugLab': 450}
def alley_bg(suffix=''):
    return A(f'Rats/Deco/{suffix[1:]}/lm_bg{suffix}.png' if suffix else 'Rats/Lobby_Market/lm_bg.png')
def alley_piece(cat, suffix=''):
    n = ALLEY[cat]
    im = A(f'Rats/Deco/{suffix[1:]}/{n}{suffix}.png' if suffix else f'Rats/Lobby_Market/{n}.png')
    s = alley_bg(); paste_c(s, fit(im, ALLEY_H[cat]), 768, 760); return crop169(s, 768, 560, 1000)

# UI 버튼: 9-슬라이스로 늘인 판 + 글 (Jua)
def nine(im, w, h, b=(70, 46, 70, 46)):
    l, bt, r, t = b; W0, H0 = im.size
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    xs = [(0, l, 0, l), (l, W0 - r, l, w - r), (W0 - r, W0, w - r, w)]
    ys = [(0, t, 0, t), (t, H0 - bt, t, h - bt), (H0 - bt, H0, h - bt, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 > dx0 and dy1 > dy0:
                out.alpha_composite(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out
FONT_L = ImageFont.truetype(FONT, 40)
def ui_scene(theme=None):
    sfx = f'_{theme}' if theme else ''
    def k(n): return A(f'Rats/Deco/{theme}/{n}{sfx}.png') if theme else A(f'UI_Kit/{n}.png')
    s = crop169(BASE, 768, 512, 1536).resize((1280, 720), Image.LANCZOS).convert('RGBA')
    s = Image.alpha_composite(s, Image.new('RGBA', s.size, (40, 30, 20, 70)))
    d = ImageDraw.Draw(s)
    rows = [('ui_plank', '작전 회의', 360, 120, (180, 110)), ('ui_plank_green', '승급!', 300, 120, (620, 110)),
            ('ui_plank_red', '닫기', 260, 120, (180, 300)), ('ui_plank_gray', '잠김', 260, 120, (500, 300)),
            ('ui_pill', '쥐 목록', 330, 100, (180, 480)), ('ui_key', '', 120, 120, (580, 470)),
            ('ui_btn_round', '', 140, 140, (760, 460)), ('ui_btn_round_green', '', 140, 140, (930, 460))]
    for n, txt, w, h, (x, y) in rows:
        im = k(n)
        im = nine(im, w, h) if n.startswith('ui_plank') or n == 'ui_pill' else im.resize((w, h), Image.LANCZOS)
        s.alpha_composite(im, (x, y))
        if txt:
            tw = d.textlength(txt, font=FONT_L)
            col = (255, 255, 255) if theme == 'neon' else (255, 248, 235)
            d.text((x + (w - tw) / 2, y + h / 2 - 24), txt, font=FONT_L, fill=col, stroke_width=3, stroke_fill=(70, 40, 20) if theme != 'neon' else (30, 10, 60))
    return s.resize((W, H), Image.LANCZOS)

def cursor_scene(theme=None):
    sfx = f'_{theme}' if theme else ''
    s = crop169(BASE, 560, 560, 760).convert('RGBA')
    for i, st in enumerate(['normal', 'pressed']):
        im = A(f'Cursor/cursor_{st}{sfx}.png').resize((128, 128), Image.NEAREST if False else Image.LANCZOS)
        s.alpha_composite(im, (180 + i * 220, 110))
    d = ImageDraw.Draw(s); f = ImageFont.truetype(FONT, 26)
    for i, t in enumerate(['평소', '누를 때']):
        tw = d.textlength(t, font=f); d.text((244 + i * 220 - tw / 2, 262), t, font=f, fill=(255, 250, 240), stroke_width=3, stroke_fill=(60, 40, 20))
    return s

# ── 장식 목록 ──
tab = json.load(open(os.path.join(DATA, 'AcornTable.json'), encoding='utf-8'))
zone_of = {c['category']: c['zone'] for c in tab['Deco_Category']}
cat_name = {c['category']: c['cat_name'] for c in tab['Deco_Category']}
items = []
for d in tab['Deco_Item']:
    cat, th = d['category'], THEMES.get(d['theme'])
    sfx = f'_{th}' if th else ''
    before, after, view = 'lobby_base', None, '아지트가'
    if cat == 'LobbyBg':
        after = put(f'bg_{th}', lobby_view(lobby(th)))
    elif cat in PROPS:
        p = PROPS[cat]; im, (x, y) = prop_box('base', p); cx, cy = x + im.width / 2, y + im.height / 2
        w = min(max(im.width, im.height * 16 / 9) * 2.6, 1000)
        before = put(f'prop_{p}_base', crop169(BASE, cx, cy, w))
        after = put(f'prop_{p}_{th}', crop169(lobby('base', {p: th}), cx, cy, w))
    elif cat == 'Badge':
        before, after = put('badge_base', badges()), put(f'badge_{th}', badges(sfx))
    elif cat == 'Wheel':
        before, after = put('wheel_base', wheel_scene()), put(f'wheel_{th}', wheel_scene(sfx))
    elif cat == 'Manhole':
        before, after = put('manhole_base', manhole()), put(f'manhole_{th}', manhole(sfx))
    elif cat in ALLEY:
        view = '뒷골목이'
        before, after = put(f'{cat}_base', alley_piece(cat)), put(f'{cat}_{th}', alley_piece(cat, sfx))
    elif cat == 'AlleyBg':
        view = '뒷골목이'
        before, after = put('alley_base', crop169(alley_bg(), 768, 512, 1536)), put(f'alley_{th}', crop169(alley_bg(sfx), 768, 512, 1536))
    elif cat == 'UiButton':
        view = '버튼이'
        before, after = put('ui_base', ui_scene()), put(f'ui_{th}', ui_scene(th))
    elif cat == 'Cursor':
        view = '커서가'
        before, after = put('cur_base', cursor_scene()), put(f'cur_{th}', cursor_scene(th))
    elif cat == 'RatFriend':
        a = d['asset']
        if a.endswith('bat'):
            view = '뒷골목이'
            pv = Image.open(os.path.join(FRIENDS, 'Bat/_미리보기_뒷골목.png')).convert('RGBA')
            before = put('bat_before', crop169(alley_bg().resize(pv.size, Image.LANCZOS), pv.width / 2, pv.height / 2, pv.width))
            after = put('bat_after', crop169(pv, pv.width / 2, pv.height / 2, pv.width))
        elif a.endswith('thumbs'):
            pv = Image.open(os.path.join(FRIENDS, 'Hamster/_미리보기_아지트.png')).convert('RGBA')
            emp = A('Rats/UI/LobbySplit/lobby_base_empty.png')
            before = put('ham_before', crop169(emp, 900, 560, 900)); after = put('ham_after', crop169(pv, 900, 560, 900))
        else:
            wi = Image.open(os.path.join(FRIENDS, 'Witch/whole.png')).convert('RGBA')
            s = BASE.copy(); paste_c(s, fit(wi, 230), 1040, 470)
            before = put('witch_before', crop169(BASE, 900, 480, 1000)); after = put('witch_after', crop169(s, 900, 480, 1000))
    if not after: continue
    zone = 'RatFriend' if cat == 'RatFriend' else zone_of.get(cat, '')
    view = view if cat != 'RatFriend' else ('뒷골목' if view.startswith('뒷골목') else '아지트')
    items.append(dict(id=d['deco_id'], name=d['deco_name'], cat=cat, catName=cat_name.get(cat, cat), zone=zone,
                      price=d['price'], before=before, after=after, view=view))

t = open(os.path.join(HERE, 'shop_template.html'), encoding='utf-8').read()
t = t.replace('__IMGS__', json.dumps(IMGS)).replace('__ITEMS__', json.dumps(items, ensure_ascii=False))
os.makedirs(os.path.dirname(OUT), exist_ok=True)
open(OUT, 'w', encoding='utf-8').write(t)
print(OUT, len(items), 'items', len(IMGS), 'imgs', round(len(t) / 1e6, 2), 'MB')
