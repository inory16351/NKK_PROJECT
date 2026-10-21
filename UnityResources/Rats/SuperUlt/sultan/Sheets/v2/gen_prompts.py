"""술탄 쥐 초필살기 v2 = '오일머니 금화 비' 프롬프트 (2026-10-20 사용자: 석유로 큰돈을 버는 장면은 짧게 → 궁전에서 나와 마법 양탄자를 타고 금화를 비처럼 뿌림)\n컷: ① 사막 유정에서 석유가 솟구침 → 기름 방울이 금화로 ② 얼굴 클로즈업: 눈이 금화로 반짝 ③ 궁전 문을 나서는 술탄 → 손을 들어 양탄자 부름 → 타고 솟구침\n    ④ 하늘: 양탄자 위에서 금화 자루를 퍼서 뿌림 (금화 비) → 금화 벽이 화면을 덮음 → 게임 화면 금화 비\n그림체 = 다른 v2 컷신과 같은 플랫 (ref_flat_bg = 로비 배경 · ref_flat_anim = 파라오 셀 프레임 · ref_rig = 게임 파츠). 글자 없음.\n연속 프레임(shot_oil2 · cu_face2)은 1번을 참고로 넣어 '같은 구도에서 바뀐 것만' 편집 → 프레임끼리 겹쳐도 맞음.\n→ UnityResources/Rats/SuperUlt/sultan/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'sultan', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(PH, 'anim_command.png'), 'ref_flat_anim2.png'),
                 (os.path.join(R, 'SuperUlt', 'streamrat', 'Sheets', 'face_close.png'), 'ref_flat_face.png'),
                 (os.path.join(R, 'SuperUlt', 'sultan', 'Sheets', 'ref_char.png'), 'ref_char.png')):
    shutil.copy(src, os.path.join(D, dst))
ims = [Image.open(os.path.join(R, 'Parts', 'sultan', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same sizes and positions, the same colors and flat style{bgkeep}. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = "warm desert sunset — soft peach, apricot and rose sky, warm sand and ochre dunes, cream and white palace walls, turquoise and teal dome tiles, rich gold accents"
BG = ("- {size}, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO people, NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: " + PAL + ".\n")
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the character's in-game sprite parts: copy its colors, shapes and proportions) and the approved flat cutscene art ref_flat_anim / ref_flat_face: "
         "FLAT cartoon storybook look in the Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, small simple black bead eyes with one tiny white highlight, "
         "no black outlines, no gloss, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo.\n- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- CHARACTER "Sultan Hamster": build it EXACTLY from the attached ref_rig and ref_char (use ref_char only for the colors and the character design, NOT its painted look): '
        'a cute chubby golden-orange HAMSTER with a cream-white muzzle, belly and big round CHEEKS, small round pink-lined ears, a pink nose, white whiskers, small black bead eyes, '
        'wearing a big cream-white TURBAN with a blue band, a round gold brooch with a red ruby on the front and a tall white feather plume on top, '
        'and a short deep-red royal CAPE over the back with a gold trim band, hanging gold tassels and a gold clasp with a blue gem; small pink paws; a short fluffy cream tail. Big head, round small body.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta or purple on the subjects. Keep everything fully inside the frame with margin.\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_flat_face.png']
SHEET = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
         "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses.\n")
P = {}
# ① 사막 유정 (짧게, 금화 없이 석유만 — 2026-10-20 사용자 2차): 1 = 작은 유정탑 · 2 = 땅이 들썩 · 3 = 석유 분출 (편집)
P['shot_oil1'] = (BG.format(size='1536x1024 LANDSCAPE') +
                  "- A wide DESERT OIL FIELD at sunset seen from a slightly low angle: one SMALL wooden-and-steel OIL DERRICK tower standing in the CENTER on a low sand dune — the derrick is SMALL: "
                  "its top at about 48% of the image height and its base at about 78%, so most of the picture above it is open sky. Two tiny nodding pump-jacks far at the left and right, rolling sand dunes, "
                  "and far away on the horizon a tiny silhouette of a white palace with turquoise onion domes. Calm moment before the oil strike. NO coins, NO gold anywhere.\n", ['ref_flat_bg.png'])
P['shot_oil2'] = (EDIT.format(src='shot_oil1.png', f='{f}', bgkeep='') +
                  "- The ground RUMBLES just before the oil strike: the derrick shakes (a few short flat shake lines beside it), cracks in the sand around its base, dark puffs of sand dust bursting out at the base, "
                  "and a short fat spurt of glossy black OIL just popping out of the top of the derrick with a few black drops. NO coins, NO gold. Nothing else changes.\n", ['shot_oil1.png'])
P['shot_oil3'] = (EDIT.format(src='shot_oil1.png', f='{f}', bgkeep='') +
                  "- OIL STRIKE! A HUGE thick column of glossy BLACK OIL GUSHES straight UP out of the top of the derrick all the way to the TOP EDGE of the picture (a tall fountain much taller than the derrick), "
                  "spreading at the top like a black umbrella and raining big glossy black OIL DROPS all around, black oil splashes and puddles on the sand around the base, the derrick dripping with oil. "
                  "Flat simple shapes with one white shine on the oil. Absolutely NO coins and NO gold. Nothing else changes.\n", ['shot_oil1.png'])
# ② 얼굴 클로즈업 (배경판 따로): 1 = 흐뭇한 얼굴 · 2 = 눈이 금화로 반짝 (편집)
P['face_bg'] = (BG.format(size='1536x1024 LANDSCAPE') +
                "- A FLAT cut-in backdrop for a dramatic close-up: big flat radial RAYS in rich gold, warm amber and cream radiating from the center, a few round flat gold coin shapes scattered at the edges, "
                "a calm plain middle area where a face will be placed on top. Flat shapes only.\n", ['ref_flat_bg.png'])
P['cu_face1'] = ("- 1536x1024 LANDSCAPE extreme CLOSE-UP of the character's FACE straight from the FRONT, the face and the turban filling most of the height, centered, the top of the red cape collar visible at the bottom edge. Keep the simple bead eyes.\n"
                 + STYLE + CHAR + MAG + "- Expression: a smug satisfied grin, eyes half-closed in pleasure, big round cheeks puffed, one paw raised near the cheek rubbing two fingers together (the 'money' gesture).\n", CREFS)
P['cu_face2'] = (EDIT.format(src='cu_face1.png', f='{f}', bgkeep=', the same solid magenta background') +
                 "- MONEY EYES: both eyes are now wide open and REPLACED by two big shiny round GOLD COINS (flat gold discs with one simple white shine), a huge open-mouth delighted grin, "
                 "the cheeks even more puffed, three small flat gold sparkle stars around the head. Nothing else changes.\n", ['cu_face1.png'])
# ③ 궁전 앞에서 하늘까지 (세로 한 장: 카메라가 양탄자를 따라 아래 → 위로 틸트, 쥐는 따로 — 2026-10-20 사용자 2차: 궁전에 돈이 가득 · 궁전 밖에서 바로 양탄자 타고 올라감)
P['shot_rise'] = (BG.format(size='1024x1536 PORTRAIT') +
                  "- ONE tall vertical picture that a camera tilts UP through, from the bottom to the top:\n"
                  "  BOTTOM 40%: the grand white desert PALACE seen from the FRONT, symmetric, close: a big pointed-arch gate with golden doors wide open, and the palace is OVERFLOWING with TREASURE: "
                  "huge MOUNTAINS of shiny gold COINS piled up in front of the gate and spilling down the steps, open treasure chests full of gold and gems, gold coins pouring out of the windows and the open gate, "
                  "a wide sandstone plaza covered in gold coins at the very bottom;\n"
                  "  MIDDLE: the turquoise ONION DOMES and slim minarets of the palace rising up, with gold coins on the balconies;\n"
                  "  TOP 35%: open peach, apricot and rose sunset SKY with a few big soft flat clouds (no buildings).\n"
                  "- Keep the CENTER column of the picture clear enough (a flying carpet will rise up through it later).\n", ['ref_flat_bg.png'])
P['anim_exit'] = (HEAD.format(f='{f}') + SHEET + "- CAMERA: every frame is a straight FRONT VIEW (the character faces the viewer).\n" + STYLE + CHAR + MAG +
                  "- The Sultan walks out of his palace like a proud king, FULL BODY, FRONT VIEW, standing upright like a person, the cape flowing behind:\n"
                  "  1) striding toward the viewer: the left foot forward, chest puffed out, paws on the hips, chin up, smug closed-eye smile;\n"
                  "  2) the next stride: the right foot forward, one paw waving like greeting a crowd, smug smile;\n"
                  "  3) stopping and RAISING one paw HIGH above the head, snapping the fingers to CALL something from the sky, the other paw on the hip, a big confident open-mouth grin, cape swinging.\n", None)
# ④ 하늘 (양탄자 비행 배경, 가로 와이드)
P['pano_on'] = (BG.format(size='1536x1024 LANDSCAPE') +
                "- A WIDE SKY view high ABOVE a desert city at sunset: the upper two thirds is open sky (peach, apricot and rose bands with a few big soft flat clouds), "
                "the lower third is the desert city seen from above far below: cream-white flat-roofed houses, several turquoise and gold ONION DOMES and minarets, the big white palace in the lower center, "
                "golden sand dunes at the far edges and the horizon. Flat simple shapes, readable, calm (flying characters and coins will be added on top).\n", ['ref_flat_bg.png'])
P['anim_ride'] = (HEAD.format(f='{f}') + SHEET + "- CAMERA: every frame is a three-quarter FRONT view, the carpet flying toward the viewer's RIGHT.\n" + STYLE + CHAR + MAG +
                  "- The Sultan SITS cross-legged on a flying MAGIC CARPET: a rectangular deep-red carpet with a gold geometric border, a turquoise diamond pattern in the middle and gold tassels at the corners, "
                  "the carpet rippling in a gentle wave as it flies. Next to the Sultan on the carpet sits a big open burlap SACK overflowing with shiny round GOLD COINS. Same carpet and sack in all frames:\n"
                  "  1) the Sultan reaches one paw deep into the coin sack, grinning, the other paw holding the carpet edge;\n"
                  "  2) the Sultan FLINGS a big handful of gold coins up and out to the side with one arm swung wide, coins flying from the paw in an arc, joyful open-mouth laugh, cape flapping;\n"
                  "  3) the Sultan throws BOTH arms wide open up in the air laughing, gold coins flying from both paws, eyes closed in bliss, the carpet tilted a little.\n", None)
P['anim_pour'] = (HEAD.format(f='{f}') + SHEET + "- CAMERA: every frame is a three-quarter FRONT view, the carpet flying toward the viewer's RIGHT, seen slightly from below.\n" + STYLE + CHAR + MAG +
                  "- The Sultan SITS on the SAME flying MAGIC CARPET as before (a rectangular deep-red carpet with a gold geometric border, a turquoise diamond pattern in the middle and gold tassels at the corners, rippling) "
                  "and POURS GOLD DOWN: he holds a big burlap SACK of gold coins with both paws and TIPS it over the FRONT EDGE of the carpet so a thick stream of gold COINS pours straight DOWN below the carpet "
                  "(the coin stream starts at the sack mouth and goes down OUT of the bottom of each frame). Laughing happily, cape flapping:\n"
                  "  1) tipping the sack over the edge, the coins starting to pour, big grin;\n"
                  "  2) the sack tipped further, a thick stream of coins pouring down, mouth open laughing, eyes closed;\n"
                  "  3) shaking the sack up and down with gusto, coins bouncing out, a wink. Keep the carpet in the same place in all frames.\n", None)
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, purple or pink on the elements.\n- NO text, NO letters, NO numbers, no symbols printed on the coins.\n")
GOLD = "- Palette: rich gold yellow, warm amber shade, cream-white highlight, a few gems in ruby red, turquoise and royal blue, glossy black for oil.\n"
P['fx_flat'] = (FXS + GOLD +
                "  1. ONE big round GOLD COIN seen from the front: a flat gold disc with a raised rim and one simple white diagonal shine (no symbol)\n"
                "  2. ONE gold coin seen tilted at an angle (a thin gold ellipse showing its edge), spinning\n"
                "  3. a flat four-pointed SPARKLE star in cream white and gold\n"
                "  4. a flat SPLASH of many gold coins bursting outward from a center (a round burst made of 12 coins flying out)\n"
                "  5. ONE faceted ruby-red GEM\n"
                "  6. ONE glossy black OIL DROP, a teardrop pointing UP with one white shine\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (FXS + GOLD +
               "  1. ONE gold coin FALLING fast: a gold coin with a short flat cream-gold motion streak trailing ABOVE it (vertical)\n"
               "  2. a small open burlap SACK of gold coins tipped over, coins spilling\n"
               "  3. a wide WALL of countless gold COINS seen from the front: a thick horizontal band packed with gold coins at all angles, with loose coins dangling from its BOTTOM edge (it will slide down to cover the screen)\n"
               "  4. COIN LANDING frame 1: a small flat gold flash just hitting the ground, a few coins bouncing up\n"
               "  5. COIN LANDING frame 2: a wide crown burst of gold coins and sparkles jumping out\n"
               "  6. COIN LANDING frame 3: the end: a flat little PILE of gold coins on the ground with two small sparkles\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['pour'] = ("- 1536x1024 image, 3 columns side by side: ONE element per column, each the same size, each filling the FULL HEIGHT of the image from the top edge to the bottom edge, none touching each other.\n"
             "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2: simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO painterly texture, NO black outlines.\n"
             "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, purple or pink on the elements.\n- NO text, NO letters, NO numbers.\n" + GOLD +
             "- Three consecutive animation frames (for a loop) of a vertical WATERFALL of shiny GOLD COINS pouring straight DOWN: a long narrow stream of many round gold coins at all angles, "
             "narrow at the TOP and slightly wider toward the BOTTOM, with a few small sparkles; in each frame the coins are at different positions (as if they moved down a little).\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
REFS = {'anim_pour': CREFS + ['ref_flat_anim2.png'], 'anim_exit': CREFS + ['ref_flat_anim2.png'], 'anim_ride': CREFS + ['ref_flat_anim2.png']}
for n, (b, refs) in P.items():
    head = HEAD.format(f=n + '.png') if not b.startswith('Use your built-in') else ''
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(head + b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs or REFS[n]))
open(os.path.join(D, 'run_v2.sh'), 'w', encoding='utf-8', newline='\n').write('''#!/usr/bin/env bash
# 술탄 쥐 초필살기 v2 그림 (오일머니 금화 비) Codex — 1차: 원본 · 배경 · 프레임 · 이펙트 / 2차: 연속 프레임 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/sultan/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
oil() { run shot_oil1; run shot_oil2 & run shot_oil3 & wait; }
face() { run cu_face1; run cu_face2; }
oil & face &
for n in face_bg shot_rise anim_pour pour pano_on anim_ride; do run $n & done; wait
for n in fx_flat fx_hit; do run $n & done; wait; echo DONE
''')
print(sorted(P))
