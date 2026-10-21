"""외계인 생쥐 초필살기 v2 = '모선 강림' 프롬프트 (2026-10-21 사용자: 크고 웅장한 UFO 모양 우주선이 땅에 레이저 발사 · 카메라가 위에서 아래로 레이저와 함께 이동 · 쥐 도시가 레이저로 폐허가 됨)
컷: ① 구름을 가르며 거대 모선 등장 ② 조종석 얼굴 클로즈업: 내려다봄 → 버튼 쾅 → 초록 빛 비친 사악한 웃음 ③ 모선 아랫면 주포 충전
    ④ 발사 → 카메라가 레이저 끝을 따라 하늘 → 구름 → 쥐 도시로 틸트 다운 ⑤ 광장에 꽂혀 폭발 → 도시가 폐허로 (pano_alt) ⑥ 폐허 위 작은 원반 탄 외계인 생쥐 (셀 프레임)
그림체 = 다른 v2 컷신과 같은 플랫 (ref_flat_bg = 로비 배경 · ref_flat_anim = 파라오 셀 프레임 · ref_flat_face = 스트리머 클로즈업 · ref_rig = 게임 파츠). 글자 없음.
세로 파노라마 pano_a~c (앞 장 아래 400px 를 보고 이어 그림) + 폐허 편집본 pano_?_alt (같은 구도) · 연속 프레임(ufo2 · cu_face2·3)은 1번을 '편집'.
→ UnityResources/Rats/SuperUlt/alien/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'alien', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(PH, 'anim_command.png'), 'ref_flat_anim2.png'),
                 (os.path.join(R, 'SuperUlt', 'streamrat', 'Sheets', 'face_close.png'), 'ref_flat_face.png'),
                 (os.path.join(R, 'SuperUlt', 'alien', 'Sheets', 'ref_char.png'), 'ref_char.png')):
    shutil.copy(src, os.path.join(D, dst))
ims = [Image.open(os.path.join(R, 'Parts', 'alien', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same sizes and positions, the same colors and flat style{bgkeep}. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = "night — deep navy and indigo sky, soft violet clouds, teal and slate-blue buildings, warm butter-yellow lit windows and cheese-yellow accents, small touches of alien lime green"
BG = ("- {size}, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO people, NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: " + PAL + ".\n")
V = "- This picture is ONE SEGMENT of a very TALL vertical panorama that a camera tilts through (top -> middle -> bottom). Segments are stacked vertically, so the composition must continue at the top and bottom edges.\n"
CONT = "- CONTINUITY IS CRITICAL: the attached ref_prev_bottom is the BOTTOM STRIP of the segment directly ABOVE this one. The TOP 30% of your picture must continue it seamlessly (same shapes at the same positions, same colors).\n"
CITY = ("a cute MOUSE CITY (a town built by mice): round-topped houses with little round mouse-ear domes on the roofs, tall yellow CHEESE-WEDGE shaped towers with round holes as windows, "
        "arched round mouse-hole doors, thin chimneys, string lights between roofs, all in simple flat shapes")
SEG = {
    'a': '1 (TOP) of 3: high night SKY above the clouds: flat deep navy with simple round star dots and one small crescent moon at the upper left; the CENTER and the upper half of the sky are EMPTY (a giant spaceship will be placed there); '
         'the lower 35% is a thick layer of big puffy flat violet-grey CLOUD tops, with a round open GAP in the clouds in the center',
    'b': '2 (MIDDLE) of 3: BELOW the clouds: the underside of the cloud layer at the top (continuing the clouds), with the same round gap in the center; open dark-blue night sky in the middle; '
         'at the bottom the far SKYLINE of ' + CITY + ' seen from the front, small and far, with lit windows',
    'c': '3 (BOTTOM) of 3: seen from the FRONT and slightly above, the heart of ' + CITY + ', closer and bigger: rows of houses and cheese towers at the left and right, '
         'and in the lower CENTER a wide round town PLAZA with a round cheese-shaped fountain (the plaza center is where a giant laser will hit), a cobblestone street at the very bottom',
}
ALT = {
    'a': 'the night sky is now stained by the attack: big flat dark charcoal SMOKE plumes rise up through the cloud gap, the clouds are lit from below in warm orange and lime green, a few flat embers float. '
         'Keep the stars, the moon and the empty center.',
    'b': 'the city skyline is now RUINED: the buildings are broken into jagged stumps, several flat orange FLAMES, thick flat charcoal SMOKE columns rising into the clouds, '
         'the cloud underside lit orange and lime green, the gap in the clouds wider and glowing.',
    'c': 'the mouse city is now a smoking RUIN after a giant laser strike: the plaza in the center is a huge round glowing lime-green CRATER with molten edges, '
         'the houses and cheese towers around are broken in half, roofs blown off, mouse-ear domes cracked, piles of rubble and cheese chunks on the street, small flat orange FLAMES on the rubble, '
         'flat charcoal smoke columns, scorch marks spreading from the crater, every lit window is now dark. Flat cartoon, not gory, no characters.',
}
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the character's in-game sprite parts: copy its colors, shapes and proportions) and the approved flat cutscene art ref_flat_anim / ref_flat_face: "
         "FLAT cartoon storybook look in the Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, "
         "no black outlines, no gloss, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo.\n- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- CHARACTER "Alien Mouse": build it EXACTLY from the attached ref_rig and ref_char (use ref_char only for the colors and the character design, NOT its painted look): '
        'a cute PALE LIME-GREEN MOUSE with big round pink-lined ears, two thin green ANTENNAE on the head ending in small round PINK bulbs, big shiny round BLACK eyes with one white highlight, '
        'a small pink nose, a cream-white muzzle, a long thin pink tail, small pink paws, wearing a simple cream-white SPACE SUIT with pale-blue bands and small yellow round buttons. Big head, small round body.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta, pink-purple or violet on the subjects except the pink ears, nose, paws and antenna bulbs. Keep everything fully inside the frame with margin.\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_flat_face.png']
SHEET = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
         "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses.\n")
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers, NO stars.\n")
STRIP = ("- 1536x1024 landscape image: 3 columns, ONE element per column, centered in its third, well inside it, none touching each other or the edges.\n" + FXS.split('\n', 1)[1])
LASER = "- Palette: alien LIME GREEN and mint, a cream-white core, a few touches of pale yellow; smoke in charcoal and slate grey; fire in warm orange.\n"
FREFS = ['ref_flat_fx.png', 'ref_flat_fx2.png']
P = {}
# 세로 파노라마 (하늘 → 구름 아래 → 쥐 도시) + 폐허 편집본
for k, d in SEG.items():
    P['pano_' + k] = (HEAD + BG.format(size='1536x1024 LANDSCAPE') + V + (CONT if k != 'a' else '') + '- SEGMENT ' + d + '.\n', ['ref_flat_bg.png'] + (['ref_prev_bottom.png'] if k != 'a' else []))
    P[f'pano_{k}_alt'] = (EDIT.format(src=f'pano_{k}.png', f='{f}', bgkeep='') + '- Change: ' + ALT[k] + ' NO text, NO characters.\n', [f'pano_{k}.png'])
# 거대 모선 (자홍, 따로) — 2 = 주포 충전 (편집)
P['ufo1'] = (HEAD + "- 1536x1024 LANDSCAPE, ONE object: a GIGANTIC, GRAND alien MOTHERSHIP shaped like a classic FLYING SAUCER, seen from the FRONT and slightly from BELOW (we look up at it), perfectly symmetric, "
             "filling almost the full WIDTH of the image (about 92%) with a little margin: a wide layered disc hull in pale silver-teal and slate blue with several stacked rings and flat panel lines, "
             "a big glass DOME on top in pale mint (empty, no one inside), a ring of many round lights around the rim (lime green and cream white), "
             "and in the CENTER of the UNDERSIDE a big round downward-facing CANNON / emitter lens (dark teal with a lime-green ring, currently dim). Massive, heavy, impressive, menacing but still cute flat cartoon.\n"
             "- STYLE: FLAT 2D game art like the attached ref_flat_bg and ref_flat_fx (simple flat shapes, flat fills with at most one soft shade step, NO black outlines, NO gloss, NO glow, NOT 3D, NOT realistic sci-fi).\n"
             "- NO text, NO letters, NO numbers, no logos.\n" + MAG, ['ref_flat_bg.png', 'ref_flat_fx.png', 'ref_char.png'])
P['ufo2'] = (EDIT.format(src='ufo1.png', f='{f}', bgkeep=', the same solid magenta background') +
             "- CHARGING the main cannon: the big lens in the center of the underside now shines bright LIME GREEN with a cream-white center, a few flat lime-green energy arcs crackle around it, "
             "ALL the rim lights are bright lime green, the panel lines on the underside glow lime green. Nothing else changes.\n", ['ufo1.png'])
# 조종석 클로즈업 (배경판 따로 · 쥐는 자홍)
P['face_bg'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE') +
                "- A FLAT cut-in backdrop for a dramatic close-up INSIDE the cockpit of a flying saucer: a big round porthole WINDOW in the middle showing the dark night sky and tiny city lights far below, "
                "curved teal and slate-blue walls, small round lime-green screens and blinking round buttons at the sides. The middle area is calm and plain (a face will be placed on top). Flat shapes only.\n", ['ref_flat_bg.png'])
P['cu_face1'] = (HEAD + "- 1536x1024 LANDSCAPE extreme CLOSE-UP of the character's HEAD and upper body in three-quarter view, looking DOWN toward the lower right (looking down at a city far below), "
                 "the head filling most of the height, centered. At the bottom right, the character's paw rests on top of a big round RED BUTTON on the edge of a teal control console (the console edge crosses the bottom right corner).\n"
                 + STYLE + CHAR + MAG + "- Expression: calm, cold and smug: eyelids half-closed over the big black eyes, a tiny sly smile, antennae relaxed.\n", CREFS)
P['cu_face2'] = (EDIT.format(src='cu_face1.png', f='{f}', bgkeep=', the same solid magenta background') +
                 "- The character SLAMS the red button DOWN with the paw (the paw pressed hard, the button pushed in, three short flat impact lines around it), eyes squeezed shut, mouth wide open shouting, "
                 "the two antennae bent back with their pink bulbs flashing bright. Nothing else changes.\n", ['cu_face1.png'])
P['cu_face3'] = (EDIT.format(src='cu_face1.png', f='{f}', bgkeep=', the same solid magenta background') +
                 "- An EVIL GRIN: the face lit from below by flat LIME-GREEN light (a flat lime-green tint on the lower face and the paw), the big black eyes wide open reflecting two small lime-green dots, "
                 "a wide toothy mischievous grin, the antennae standing straight up with their pink bulbs glowing, the paw still on the red button. Nothing else changes.\n", ['cu_face1.png'])
# 폐허 위 원반 탄 외계인 생쥐 (셀 프레임)
P['anim_end'] = (HEAD + SHEET + "- CAMERA: every frame is a straight FRONT VIEW (the character faces the viewer).\n" + STYLE + CHAR + MAG +
                 "- In every frame the Alien Mouse STANDS on a tiny round FLYING SAUCER (a small pale-blue disc with yellow round lights around the rim, the same saucer in all frames, under the feet), FULL BODY:\n"
                 "  1) arms crossed, chin up, smug half-closed eyes, a red-and-cream toy RAY GUN tucked under one arm;\n"
                 "  2) holding the ray gun up near the face and BLOWING on the end of its barrel (a small flat puff of smoke), eyes closed, satisfied;\n"
                 "  3) a cheeky PEACE SIGN toward the viewer with one paw and a wink, the ray gun in the other paw, antennae bouncing, big open-mouth smile.\n", CREFS + ['ref_flat_anim2.png'])
# 이펙트
P['beam'] = (HEAD + STRIP + LASER +
             "  1. a very TALL straight VERTICAL LASER BEAM (fills almost the full height): a thick flat beam with a cream-white core, a lime-green band and a darker green outer band, straight parallel edges, slightly flared at the bottom\n"
             "  2. a thinner tall vertical BEAM CORE line: cream white with thin mint edges (fills almost the full height)\n"
             "  3. a big flat IMPACT FLASH where the beam hits the ground: a wide dome-shaped cream-white and lime-green burst with rounded energy tongues, seen from the front\n", FREFS)
P['boom'] = (HEAD + FXS + LASER +
             "  1. EXPLOSION frame 1: a small flat cream-white and lime-green energy burst with rounded tongues\n"
             "  2. EXPLOSION frame 2: a bigger round lime-green and orange fireball burst with a few brick and cheese chunks flying out\n"
             "  3. EXPLOSION frame 3: the end: a puffy charcoal and orange smoke cloud with small embers, fading shapes\n"
             "  4. a wide flat SHOCKWAVE RING seen from the front (a thin wide ellipse of cream-white and lime green, about 4 times wider than tall)\n"
             "  5. a flat cluster of flying DEBRIS: broken roof tiles, bricks and small yellow cheese chunks in slate, teal and yellow\n"
             "  6. a big flat puffy SMOKE PUFF in charcoal and slate grey\n", FREFS)
P['fx_flat'] = (HEAD + FXS + LASER +
                "  1. a round ball of gathering ENERGY: a lime-green orb with a cream-white center and a few short flat lime arcs around it\n"
                "  2. a thin perfect lime-green LIGHT RING (a simple circle outline with a few evenly spaced small round dots on it)\n"
                "  3. a flat round FLASH disk: cream white center, a lime-green rim and short rounded rays\n"
                "  4. a flat cartoon FIRE: three rounded orange and yellow flame tongues\n"
                "  5. a big flat puffy CLOUD in soft violet-grey and lavender (a night cloud)\n"
                "  6. a long thin horizontal SPEED STREAK (a flat white tapered line, thick at the left, pointed at the right)\n", FREFS)
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', encoding='utf-8', newline='\n').write('''#!/usr/bin/env bash
# 외계인 생쥐 초필살기 v2 그림 (모선 강림 · 레이저 · 쥐 도시 폐허) Codex — 파노라마 a→b→c 이어 그림 + 폐허 편집 · 연속 프레임 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/alien/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
bottom() { python -c "from PIL import Image; im=Image.open('$1.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; }
pano() { run pano_a; run pano_a_alt & bottom pano_a; run pano_b; run pano_b_alt & bottom pano_b; run pano_c; run pano_c_alt; wait; }
ufo() { run ufo1; run ufo2; }
face() { run cu_face1; run cu_face2 & run cu_face3 & wait; }
pano & ufo & face &
for n in face_bg anim_end; do run $n & done; wait
for n in beam boom fx_flat; do run $n & done; wait; echo DONE
''')
print(sorted(P))
