"""증강 UI 그림 자르기 (2026-10-20): UnityResources/Rats/Augment/Sheets/ui/ (Codex, 자홍 배경) → UnityResources/Rats/Augment/ · Assets/Art/Augment/
  aug_slots.png (3×2) → aug_frame_1~4 (브론즈 · 실버 · 골드 · 프리즘) · aug_slot_empty · aug_slot_lock
  aug_panel.png → aug_ui_panel (관리 창 판) · aug_shelf.png → aug_ui_shelf (HUD 슬롯 받침)
9칸 늘리기 테두리 값(px, 왼 · 아래 · 오른 · 위)은 borders.json 에 씀 → 유니티 임포트 때 spriteBorder 로 넣음"""
import json, os, shutil, sys
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from slice_superult import key_magenta, despill, trim

ROOT = os.path.join(os.path.dirname(__file__), '..')
SRC = os.path.join(ROOT, 'UnityResources', 'Rats', 'Augment', 'Sheets', 'ui')
OUT = os.path.join(ROOT, 'UnityResources', 'Rats', 'Augment')
DST = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Augment')
SLOTS = ['aug_frame_1', 'aug_frame_2', 'aug_frame_3', 'aug_frame_4', 'aug_slot_empty', 'aug_slot_lock']


def main():
    borders, made = {}, []
    sh = Image.open(os.path.join(SRC, 'aug_slots.png')); W, H = sh.size
    for i, n in enumerate(SLOTS):
        r, c = divmod(i, 3)
        cell = trim(despill(key_magenta(sh.crop((int(c * W / 3) + 3, int(r * H / 2) + 3, int((c + 1) * W / 3) - 3, int((r + 1) * H / 2) - 3)))), 2)
        cell.save(os.path.join(OUT, n + '.png')); made.append(n)
        b = round(min(cell.size) * 0.24)                     # 테두리 띠 13% + 둥근 모서리 · 장식 → 24% 를 늘리지 않음
        borders[n] = [b, b, b, b]
        print(n, cell.size, 'border', b)
    for src, n, kx, ky in (('aug_panel.png', 'aug_ui_panel', 0.09, 0.12), ('aug_shelf.png', 'aug_ui_shelf', 0.07, 0.45)):
        im = trim(despill(key_magenta(Image.open(os.path.join(SRC, src)))), 2)
        im = im.resize((im.width // 4 * 4, im.height // 4 * 4), Image.LANCZOS)      # 4의 배수 → WebAtlas 폴더 아틀라스에서 빠짐 (큰 그림이 아틀라스를 넘치게 하지 않게)
        im.save(os.path.join(OUT, n + '.png')); made.append(n)
        bx, by = round(im.width * kx), round(im.height * ky)
        borders[n] = [bx, by, bx, by]       # 왼 · 아래 · 오른 · 위 (판 = 처음 그림, 두꺼운 윗판 시안은 Sheets/ui/_old/aug_panel_v2_thick.png — 사용자가 되돌림)
        print(n, im.size, 'border', bx, by)
    for n in made:
        shutil.copy(os.path.join(OUT, n + '.png'), os.path.join(DST, n + '.png'))
    json.dump(borders, open(os.path.join(SRC, 'borders.json'), 'w'), indent=1)
    print('→', DST)


if __name__ == '__main__':
    main()
