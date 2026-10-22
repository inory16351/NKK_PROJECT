"""보스 개편 기술 그림 br_* 자르기 (2026-10-22). 시트 = UnityResources/Rats/BossRework/Sheets/ef1~3.png (Tools/gen_boss_fx_prompts.py 칸 순서)
결과: UnityResources/Rats/BossRework/FX/br_*.png (원본 보관) → Assets/Art/Rats/BossMeme/br_*.png (보스 인스펙터 Fill Meme Sprites 로 연결)
· 머리 교체 그림(head:/head_keep:) = 원래 머리와 같은 픽셀 크기로 줄이고 관절점(목 아래)을 FX/pivots.json 에 → gen_sprite_pivots.py 가 읽음
· 재사용 그림(REUSE)은 그리지 않고 복사
사용: python Tools/slice_boss_fx.py [시트...]  (생략 = 있는 시트 전부 + 재사용 복사)"""
import io, json, os, sys
import numpy as np
from PIL import Image, ImageOps
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta
from slice_boss_rework import cell_imgs, copy_to, ART, RES, SHEETS

FX = os.path.join(RES, 'BossRework', 'FX')
DST = os.path.join(ART, 'BossMeme')

GRID = {
    'ef1': (6, 3, ['br_huh_h', 'br_huh_u', 'br_huh_q', 'br_bread_loaf', 'br_bread_pizza', 'br_bread_cake',
                   'br_bread_chiffon', 'br_dino_head', 'br_pellet', 'br_vmouse_meteor', 'br_rebar', 'br_microwave',
                   'br_red_button', 'br_gasmask', 'br_brush', 'br_palette', 'br_easel', 'br_canvas']),
    'ef2': (3, 2, ['br_goerem_saw', 'br_goerem_bread', 'br_huh_head', 'br_union_megaphone', 'br_union_phone', 'br_dino_sit']),
    'ef3': (2, 2, ['br_rebar_web', 'br_mini_merlion', 'br_mini_gremlin', 'br_mustache']),
}
# 머리 교체: 이름 → (원래 머리 그림, 맞출 기준 'w' 너비 · 'h' 높이, 배율)
HEADS = {'br_huh_head': ('Cats/Parts/boss_popcat/head.png', 'h', 1.0),
         'br_goerem_saw': ('Cats/Parts/boss_goerem/head.png', 'w', 0.95)}
# 재사용: 이름 → (원본, 좌우 뒤집기) — 발사체 그림은 왼쪽을 봄
REUSE = {'br_club': ('BossRework/Props/club.png', False), 'br_rat_jet': ('BossRework/Parts/boss_carrier/jet2.png', False),
         'br_siren': ('Rogue/rg_siren.png', False), 'br_smoke': ('FX_New/color/smoke.png', False),
         'br_bullet': ('SuperUlt/emperor/fx_bullet.png', True), 'br_vmouse_head': ('Humans/Parts/boss_vmouse/head.png', False)}
MAXW = 512      # 너무 큰 그림은 줄임 (거미줄 등)


def neck_pivot(img):
    """목 아래 관절점 (유니티 좌하단 기준 0~1): 맨 아래 6% 줄의 불투명 가운데 x, 아래에서 4%"""
    a = np.array(img)[..., 3] > 20; h = a.shape[0]
    xs = np.nonzero(a[int(h * 0.94):].any(0))[0]
    return [round(float(xs.mean()) / a.shape[1], 3) if len(xs) else 0.5, 0.04]


def save(img, name, piv):
    if img is None: print('  빈 칸', name); return
    if name in HEADS:
        ref, key, k = HEADS[name]; rw, rh = Image.open(os.path.join(RES, ref)).size
        s = k * (rw / img.width if key == 'w' else rh / img.height)
        img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
        piv[name] = neck_pivot(img)
    elif img.width > MAXW:
        img = img.resize((MAXW, round(img.height * MAXW / img.width)), Image.LANCZOS)
    os.makedirs(FX, exist_ok=True)
    f = os.path.join(FX, name + '.png'); img.save(f); copy_to(f, os.path.join(DST, name + '.png'))
    print('  ', name, img.size)


def main():
    pf = os.path.join(FX, 'pivots.json')
    piv = json.load(io.open(pf, encoding='utf-8')) if os.path.exists(pf) else {}
    todo = sys.argv[1:] or [s for s in GRID if os.path.exists(os.path.join(SHEETS, s + '.png'))]
    for s in todo:
        cols, rows, names = GRID[s]
        im = key_magenta(Image.open(os.path.join(SHEETS, s + '.png')).convert('RGB'))
        print(s)
        for name, img in zip(names, cell_imgs(im, cols, rows)): save(img, name, piv)
    if not sys.argv[1:]:
        for name, (src, flip) in REUSE.items():
            img = Image.open(os.path.join(RES, src)).convert('RGBA')
            save(ImageOps.mirror(img) if flip else img, name, piv)
    os.makedirs(FX, exist_ok=True)
    json.dump(piv, io.open(pf, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)


if __name__ == '__main__':
    main()
