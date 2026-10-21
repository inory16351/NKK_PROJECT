"""명탐정 쥐 초필살기 v2 = '범인은 바로 너야!' 프롬프트 (2026-10-21 사용자 콘티)
컷: ① 사방에서 단서 조사 몽타주 (발자국 · 할퀸 자국 · 고양이 털 · 생선 뼈) ② 모은 단서를 두고 골똘히 생각 ③ 전구 → 결론 (안경 번쩍)
    ④ 검은 실루엣 고양이를 지목 "범인은 바로 너야!" → 흠칫 → 도망 ⑤ 손목시계 마취총 조준 → 발사 → 꽂혀 기절 ⑥ 기절한 고양이를 밟고 "사건 해결!"
그림체 = 다른 v2 컷신과 같은 플랫 (ref_flat_bg = 로비 배경 · ref_flat_anim = 파라오 셀 프레임 · ref_flat_face = 스트리머 클로즈업 · ref_rig = 게임 파츠). 그림 속 글자 없음 (대사는 컷신 글).
컷마다 배경까지 그린 한 장 shot_* (연속 프레임은 1번을 '편집' = 같은 틀) · 추격 골목 판 pano_on (쥐 · 고양이 없음) · 고양이 셀 anim_catrun · anim_catko
→ UnityResources/Rats/SuperUlt/jjdetective/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'jjdetective', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(PH, 'anim_command.png'), 'ref_flat_anim2.png'),
                 (os.path.join(R, 'SuperUlt', 'streamrat', 'Sheets', 'face_close.png'), 'ref_flat_face.png'),
                 (os.path.join(R, 'SuperUlt', 'jjdetective', 'Sheets', 'ref_char.png'), 'ref_char.png')):
    shutil.copy(src, os.path.join(D, dst))
ims = [Image.open(os.path.join(R, 'Parts', 'jjdetective', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same sizes and positions, the same colors and flat style{bgkeep}. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = ("a moonlit night mystery — deep navy and indigo blues, soft teal shadows, warm butter-yellow lamplight and cheese-yellow accents, cream highlights, "
       "the detective's royal blue jacket and bright red bow tie as the strongest accents")
TOWN = ("a cute little MOUSE TOWN at night: crooked wooden houses with round windows glowing warm yellow, cobblestone lanes, old street lamps, "
        "a small CHEESE SHOP with a cheese-wedge shaped sign (no letters), all in simple flat shapes")
PANTRY = ("the ransacked CHEESE SHOP of a mouse town at night (the crime scene): wooden shelves with round cheese wheels knocked over and half eaten, "
          "a toppled stool, a broken jar, moonlight from a round window making a soft pale stripe on the plank floor, warm lantern light")
BG = ("- {size}, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO people, NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: " + PAL + ".\n")
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the character's in-game sprite parts: copy its colors, shapes and proportions) and the approved flat cutscene art ref_flat_anim / ref_flat_face, "
         "and (for any background) the approved flat background ref_flat_bg: FLAT cartoon storybook look in the Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, "
         "no black outlines, no gloss, NO painterly brush strokes, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo. Palette: " + PAL + ".\n"
         "- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- CHARACTER "Detective Mouse": build it EXACTLY from the attached ref_rig and ref_char (use ref_char only for the colors and the character design, NOT its painted look): '
        'a cute small BROWN mouse with a cream muzzle and belly, a messy tuft of darker brown hair on top of the head, big round PINK-lined ears, a pink nose, white whiskers, '
        'big round thin GOLD-RIMMED GLASSES, a ROYAL BLUE blazer with gold buttons over a white shirt, a bright RED BOW TIE, blue shorts, small pink paws and feet, a long thin pink tail. '
        'A little boy-detective look, clever and confident. On its left wrist a small chunky silver WRISTWATCH. Big head, small round body.\n')
CAT = ('- THE CULPRIT CAT: drawn like the mysterious unknown criminal of a classic detective cartoon: the whole cat is ONE SOLID FLAT PURE BLACK SILHOUETTE with NO inner details at all '
       '(no fur, no shading, no outlines inside), ONLY two big glowing WHITE eyes (and, when it makes an expression, a thin white mouth line or white teeth). '
       'A slim sneaky cat shape: pointed ears, long whiskers drawn as thin black lines, a long curling tail, about TWICE as tall as the little mouse. Comic and cute, NOT scary.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta, pink-purple or violet on the subjects except the pink ears, nose and paws. Keep everything fully inside the frame with margin.\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_flat_face.png']
SREFS = CREFS + ['ref_flat_bg.png']
SHOT = "- 1536x1024 LANDSCAPE, one full-frame picture WITH background (no transparency, no panels, no borders).\n"
SHEET = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
         "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses.\n")
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers.\n")
LENS = "a big round brass MAGNIFYING GLASS"
P = {}
# ① 사방에서 단서 조사 몽타주 (컷마다 한 장, 같은 범행 현장의 다른 곳)
P['shot_clue1'] = (HEAD + SHOT + STYLE + CHAR +
                   "- SCENE, a LOW-ANGLE CLOSE-UP on the plank floor of " + PANTRY + ": in the foreground a clear muddy CAT PAW PRINT on the floor boards (big, four toe pads and a heel pad). "
                   "Right behind it the Detective Mouse kneels and holds " + LENS + " over the paw print; through the lens its glasses and one EYE look hugely magnified and sharp. Focused, serious face. "
                   "Strong, readable, the paw print and the lens fill the middle.\n", SREFS)
P['shot_clue2'] = (HEAD + SHOT + STYLE + CHAR +
                   "- SCENE, in " + PANTRY + ": a round moonlit WINDOW with its wooden frame wide open, three long fresh CLAW SCRATCHES gouged across the window sill and frame. "
                   "The Detective Mouse stands on tiptoe on top of a stack of old books, leaning in to inspect the scratches through " + LENS + ", its other paw touching the scratch, eyebrows raised. "
                   "Medium shot from the side, the moon and the town roofs outside the window.\n", SREFS)
P['shot_clue3'] = (HEAD + SHOT + STYLE + CHAR +
                   "- SCENE, an EXTREME CLOSE-UP: a small pink mouse paw (royal blue sleeve cuff) holds silver TWEEZERS that lift up one single long curly BLACK CAT HAIR, "
                   "held up toward the lantern light; behind it, slightly out of focus, the Detective Mouse's face looks at the hair with narrowed eyes, a light glint on its gold-rimmed glasses. "
                   "Background: soft flat out-of-focus shelves of " + PANTRY + ".\n", SREFS)
P['shot_clue4'] = (HEAD + SHOT + STYLE + CHAR +
                   "- SCENE, a low side view under a wooden table in " + PANTRY + ": the Detective Mouse crawls on its belly under the table holding a small FLASHLIGHT, its flat pale-yellow beam cone "
                   "lights up a clean FISH SKELETON (fish bone with head and tail) lying next to a big cheese wheel with a huge round BITE taken out of it. The mouse's face is surprised, mouth a small O. "
                   "The rest of the room is dark navy.\n", SREFS)
# ② 골똘히 생각 → ③ 유레카 (편집)
P['shot_think1'] = (HEAD + SHOT + STYLE + CHAR +
                    "- SCENE, a MEDIUM SHOT (waist up) of the Detective Mouse THINKING HARD, facing the viewer in three-quarter view, slightly right of center: "
                    "one paw on its chin, the other arm folded under it, eyes narrowed and looking up to one side, mouth pursed, a little crease between the eyebrows. "
                    "Background: an abstract flat dark NAVY and indigo mental space (like the inside of a detective's mind) with a soft round warm spotlight behind the head, "
                    "lots of EMPTY dark space around the head on both sides and above (floating clue icons will be added there later). The character fills about 65% of the image height.\n", SREFS)
P['shot_think2'] = (EDIT.format(src='shot_think1.png', f='{f}', bgkeep=', the same background') +
                    "- EUREKA! The Detective Mouse has solved it: both lenses of its gold-rimmed glasses turn into flat solid bright WHITE disks (the classic detective glasses glare, eyes hidden), "
                    "a confident sly smile, the paw that was on the chin now points one finger straight UP, the hair tuft springs up, ears perked. Nothing else changes.\n", ['shot_think1.png'])
# ④ 지목 "범인은 바로 너야!" → 고양이 흠칫 → 도망 (편집)
P['shot_point'] = (HEAD + SHOT + STYLE + CHAR +
                   "- SCENE, a DRAMATIC LOW-ANGLE MEDIUM CLOSE-UP of the Detective Mouse POINTING hard with one arm stretched toward the RIGHT edge of the picture (the index finger pointing right, big in the foreground), "
                   "body leaning forward on the left half of the image, a fierce confident face, mouth open as if shouting, a sharp white glint on the glasses. "
                   "Background: flat radiating SPEED LINES in navy and royal blue bursting from behind the mouse, with a cream-white burst right behind its head (like ref_char). \n", SREFS)
P['shot_culprit1'] = (HEAD + SHOT + STYLE + CAT +
                      "- SCENE, a moonlit cobblestone back lane of " + TOWN + ", a street lamp casting a warm circle of light: the BLACK SILHOUETTE CAT stands caught in the lamp light, "
                      "turned three-quarter toward the viewer, FLINCHING BACKWARD in shock: its two white eyes wide and round with tiny pupils, a wavy white mouth line, "
                      "ears pinned back, fur bristling along its back and tail (the silhouette edge spiky), a big flat light-blue SWEAT DROP beside its head, a stolen cheese wedge dropping from its paws. "
                      "The cat fills about 70% of the image height, centered. NO mouse in this picture.\n", ['ref_flat_bg.png', 'ref_flat_anim.png', 'ref_flat_face.png'])
P['shot_culprit2'] = (EDIT.format(src='shot_culprit1.png', f='{f}', bgkeep=', the same background and street lamp') +
                      "- The BLACK SILHOUETTE CAT spins around and DASHES AWAY to the RIGHT: body stretched low and long, legs blurred in a run, head looking back over its shoulder with panicked white eyes, "
                      "short flat white motion lines and a flat cream dust puff behind it, the cheese wedge left tumbling in the air. Nothing else changes.\n", ['shot_culprit1.png'])
# ⑤ 추격 골목 판 (쥐 · 고양이 없음) · 고양이 셀 프레임 · 손목시계 마취총 조준 (편집)
P['pano_on'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE') +
                "- A long moonlit cobblestone BACK STREET of " + TOWN + ", seen straight from the SIDE at ground level (a side-scrolling chase stage): a row of crooked mouse houses and shop backs across the whole width, "
                "two old street lamps, crates and barrels, washing lines between windows, a big round full moon and stars in a navy sky in the upper part. "
                "The cobblestone road is a wide flat EMPTY band across the lower 35% of the image (characters will run along it from left to right). Nothing blocks the road.\n", ['ref_flat_bg.png'])
P['anim_catrun'] = (HEAD + SHEET + "- CAMERA: every frame is a straight SIDE VIEW, the cat runs to the RIGHT.\n" + STYLE + CAT + MAG +
                    "- A RUN CYCLE of the BLACK SILHOUETTE CAT fleeing in panic (head turned slightly back, white eyes wide with worry, a small light-blue sweat drop):\n"
                    "  1) legs stretched far apart front and back, body long and low (gallop reach);\n"
                    "  2) legs gathered together under the body, back arched up;\n"
                    "  3) front legs landing, back legs kicking off, tail streaming behind.\n", ['ref_flat_anim.png', 'ref_flat_anim2.png', 'ref_flat_face.png'])
P['anim_catko'] = (HEAD + SHEET + "- CAMERA: every frame is a SIDE VIEW, the cat faces RIGHT. All frames share the same ground line at the bottom.\n" + STYLE + CAT + MAG +
                   "- The BLACK SILHOUETTE CAT is hit in the BACKSIDE by a tiny tranquilizer DART (a small silver dart with a short red feather tail, stuck in its rump):\n"
                   "  1) jumps straight UP in shock, all four legs stiff and spread, every hair standing up (spiky silhouette), white eyes huge, mouth a white zigzag;\n"
                   "  2) wobbling dizzy on wobbly legs, eyes turned into two white SPIRALS, tongue out (white), swaying;\n"
                   "  3) collapsed FLAT on its belly on the ground fast asleep, legs splayed, eyes as two white spirals, a big flat light-blue SNOT BUBBLE from its nose, the dart still in its rump.\n",
                   ['ref_flat_anim.png', 'ref_flat_anim2.png', 'ref_flat_face.png'])
P['shot_aim1'] = (HEAD + SHOT + STYLE + CHAR +
                  "- SCENE, an OVER-THE-SHOULDER CLOSE-UP from behind the Detective Mouse: in the big left foreground its raised left arm (royal blue sleeve, pink paw) with the chunky silver WRISTWATCH, "
                  "the watch's lid flipped OPEN like a tiny gun sight with a small red crosshair lens; the mouse's cheek, ear and gold glasses at the left edge, one eye squinting to aim along the watch. "
                  "Far away down the moonlit cobblestone lane of " + TOWN + " a small BLACK SILHOUETTE CAT (white eyes) runs away toward the right. A thin red aiming line from the watch toward the cat.\n" + CAT, SREFS)
P['shot_aim2'] = (EDIT.format(src='shot_aim1.png', f='{f}', bgkeep=', the same background and the same small cat') +
                  "- FIRE! A small flat white-and-yellow MUZZLE PUFF bursts from the wristwatch sight, a tiny silver DART with a red feather tail flies out toward the cat leaving a thin white streak, "
                  "the arm recoils slightly, the aiming eye wide open with determination. Nothing else changes.\n", ['shot_aim1.png'])
# ⑥ 기절한 고양이를 밟고 사건 해결 (편집)
P['shot_win1'] = (HEAD + SHOT + STYLE + CHAR + CAT +
                  "- SCENE, a slightly LOW-ANGLE shot on the moonlit cobblestone lane of " + TOWN + " under a street lamp (a warm spotlight circle): the BLACK SILHOUETTE CAT lies flat on its belly across the ground, "
                  "knocked out cold (white spiral eyes, white tongue out, a light-blue snot bubble, the little red-feathered dart in its rump). "
                  "The small Detective Mouse stands proudly ON TOP of the cat's back with ONE FOOT planted on it (the other foot on the cat's back too), chest out, "
                  "one finger pushing up its gold-rimmed glasses that flash white, a smug triumphant smile, the stolen cheese wedge tucked under its other arm. "
                  "The mouse is in the center, the cat stretched across the lower part of the image.\n", SREFS)
P['shot_win2'] = (EDIT.format(src='shot_win1.png', f='{f}', bgkeep=', the same background, the same knocked-out cat') +
                  "- The Detective Mouse now WINKS one eye with a big grin and gives a THUMBS-UP toward the viewer with the paw that pushed the glasses, still standing on the cat; "
                  "a few flat cream-yellow sparkles around it. Nothing else changes.\n", ['shot_win1.png'])
# 소품 · 이펙트
P['props_flat'] = (HEAD + FXS.replace('effect art', 'prop art') +
                   "  1. a muddy CAT PAW PRINT (four toe pads and a heel pad, dark brown)\n"
                   "  2. a single long curly BLACK CAT HAIR\n"
                   "  3. a clean white FISH BONE skeleton (head, spine, tail)\n"
                   "  4. three long parallel CLAW SCRATCH marks (dark brown gouges with pale wood edges)\n"
                   "  5. a flat cartoon idea LIGHT BULB (warm yellow bulb with a grey screw base and short yellow rays around it)\n"
                   "  6. a small TRANQUILIZER DART pointing to the RIGHT (a silver needle tip at the right, a small glass body with a light-blue liquid, a short red feather tail at the left)\n", ['ref_flat_fx.png', 'ref_rig.png'])
P['fx_flat'] = (HEAD + FXS +
                "- Palette: cream white, butter yellow, light sky blue, royal blue, a little red.\n"
                "  1. a flat four-pointed SPARKLE (cream-white with a butter-yellow center)\n"
                "  2. a flat diagonal GLASSES GLARE: a short bright white slanted lens flash shape with two thin white streaks\n"
                "  3. a big flat light-blue SNOT BUBBLE (a round shiny-free bubble with one small white highlight)\n"
                "  4. a flat cartoon DUST PUFF cloud (cream and pale grey, three round puffs)\n"
                "  5. a wide flat SHOCKWAVE RING seen from the front (a thin wide ellipse of cream-white and pale sky blue, about 4 times wider than tall)\n"
                "  6. a long thin horizontal SPEED STREAK (a flat white tapered line, thick at the left, pointed at the right)\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (HEAD + FXS +
               "- Palette: silver, red, light sky blue, butter yellow, cream white.\n"
               "  top row (each one alone, centered):\n"
               "  1. a small TRANQUILIZER DART flying straight DOWN (silver needle tip at the BOTTOM, small glass body with light-blue liquid, short red feather tail at the TOP)\n"
               "  2. a little loose RED FEATHER\n"
               "  3. a small flat light-blue SNOT BUBBLE\n"
               "  bottom row = 3 consecutive frames of a DART HIT, same size and centered at the same point:\n"
               "  4. a small round cream-white impact flash with short spikes\n"
               "  5. a bigger flat butter-yellow burst with three small DIZZY STARS starting to circle\n"
               "  6. a ring of three flat yellow DIZZY STARS circling (seen slightly from above, an ellipse) with a small light-blue snot bubble, fading\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', encoding='utf-8', newline='\n').write('''#!/usr/bin/env bash
# 명탐정 쥐 초필살기 v2 그림 (범인은 바로 너야!) Codex — 컷 한 장 + 연속 프레임 편집 · 추격 골목 판 · 고양이 셀
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/jjdetective/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pair() { run $1; shift; for n in "$@"; do run $n & done; wait; }
pair shot_think1 shot_think2 &
pair shot_culprit1 shot_culprit2 &
pair shot_aim1 shot_aim2 &
pair shot_win1 shot_win2 &
for n in shot_clue1 shot_clue2 shot_clue3 shot_clue4; do run $n & done; wait
for n in shot_point pano_on anim_catrun anim_catko props_flat fx_flat fx_hit; do run $n & done; wait; echo DONE
''')
print(sorted(P))
