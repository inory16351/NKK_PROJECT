"""줴리 초필살기 v2 = '톰과 줴리 추격전' 프롬프트 (2026-10-21 사용자: 고양이가 쫓아옴 → 지형지물로 반격하며 도망 → 쥐구멍으로 쏙 · 횡방향 추격 · 추격전 카메라)
컷: ① 부엌에서 치즈를 먹다 → 등 뒤 고양이 그림자 ② 고양이 얼굴 (입맛 다심) ③ 추격 1 (부엌, 옆으로) ④ 반격 1: 조리대 위 프라이팬 홈런 (얼굴이 팬 모양)
    ⑤ 추격 2 (거실) ⑥ 반격 2: 문 쾅 → 납작해진 고양이가 종이처럼 떨어짐 ⑦ 추격 3 (복도) ⑧ 반격 3: 카펫 당기기 → 고양이가 공중제비
    ⑨ 마지막 질주 → 쥐구멍으로 다이빙 ⑩ 고양이가 벽에 얼굴부터 쾅 (아코디언) → 주르륵 기절 ⑪ 쥐구멍에서 빼꼼 → 메롱 윙크
그림체 = 다른 v2 컷신과 같은 플랫 (ref_flat_bg = 로비 배경 · ref_flat_anim = 파라오 셀 프레임 · ref_flat_face = 스트리머 클로즈업 · ref_rig = 게임 파츠 · ref_cat = 게임 고양이 파츠). 글자 없음.
컷마다 배경까지 그린 한 장 shot_* (연속 프레임은 1번을 '편집' = 같은 틀) · 가로 집 파노라마 pano_a(부엌) → b(거실) → c(복도 · 쥐구멍): 앞 장 오른쪽 1/3 을 왼쪽에 둔 씨앗을 이어 그림 (stitch_pano 가로)
→ UnityResources/Rats/SuperUlt/jwerry/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'jwerry', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(PH, 'anim_command.png'), 'ref_flat_anim2.png'),
                 (os.path.join(R, 'SuperUlt', 'streamrat', 'Sheets', 'face_close.png'), 'ref_flat_face.png'),
                 (os.path.join(R, 'SuperUlt', 'jwerry', 'Sheets', '_v1_painted', 'ref_char.png'), 'ref_char.png')):
    shutil.copy(src, os.path.join(D, dst))


def strip(folder, out):
    ims = [Image.open(os.path.join(folder, n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
    W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
    for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
    W.save(os.path.join(D, out))
strip(os.path.join(R, 'Parts', 'jwerry'), 'ref_rig.png')
strip(os.path.join(R, 'Cats', 'Parts', 'tuxedo'), 'ref_cat.png')       # 게임 고양이 그림체 (색은 프롬프트대로 다시 칠함)

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same sizes and positions, the same colors and flat style{bgkeep}. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = ("a warm cozy old cartoon house in the afternoon — butter-yellow and cream walls, soft teal and mint wallpaper stripes, warm honey-brown wooden floors, baseboards and furniture, "
       "a few tomato-red accents (rug, tablecloth checks), soft sky blue windows")
HOUSE = ("a cozy old-fashioned cartoon HOUSE seen at a mouse's height (everything is big): tall walls with wide wooden baseboards, honey-brown plank floors, big furniture legs, "
         "all in simple flat shapes")
BG = ("- {size}, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO people, NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: " + PAL + ".\n")
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the mouse's in-game sprite parts: copy its colors, shapes and proportions), ref_cat (this game's cat sprite parts: copy its drawing style and proportions, NOT its colors) "
         "and the approved flat cutscene art ref_flat_anim / ref_flat_face, and (for any background) the approved flat background ref_flat_bg: FLAT cartoon storybook look in the Untitled Goose Game mood, "
         "simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, no black outlines, no gloss, NO painterly brush strokes, "
         "NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo. Classic slapstick cartoon energy (squash and stretch, big readable expressions), but the drawing stays as simple as the references. Palette: " + PAL + ".\n"
         "- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- THE MOUSE "Jwerry": build it EXACTLY from the attached ref_rig and ref_char (use ref_char only for the colors and the character design, NOT its painted look): '
        'a small round chubby ORANGE-BROWN mouse with a cream face, muzzle and belly, big round PINK-lined ears, a small dark brown nose, short whiskers, small black bead eyes with a tiny white highlight, '
        'a mischievous cheeky grin, small cream paws and feet, a long thin pink tail. No clothes. Big head, small round body.\n')
CAT = ('- THE CAT "Tomcat": a big lanky BLUE-GREY house cat, about FOUR times as tall as the little mouse, drawn in the same flat game style as ref_cat: '
       'a lighter grey-white muzzle, chest and paw tips, a pink nose, big yellow-green eyes with black pupils, tall pointed ears with pink insides, long whiskers, a long blue-grey tail with a lighter tip. '
       'A goofy slapstick villain: sly and greedy, then comically hurt. Comic and cute, NOT scary, no blood.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta, pink-purple or violet on the subjects except the pink ears, nose and tail. Keep everything fully inside the frame with margin.\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_cat.png', 'ref_flat_anim.png', 'ref_flat_face.png']
SREFS = CREFS + ['ref_flat_bg.png']
SHOT = "- 1536x1024 LANDSCAPE, one full-frame picture WITH background (no transparency, no panels, no borders).\n"
SHEET = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
         "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses.\n")
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers.\n")
FLOOR = ("- SIDE-SCROLLING CHASE STAGE: seen straight from the SIDE at a mouse's eye level. The wooden plank FLOOR is a wide flat EMPTY band across the lower 22% of the image (its top edge at about 78% of the image height), "
         "with the wall's wooden BASEBOARD running along just above it across the whole width. Characters will run along the floor from left to right, so NOTHING stands on the floor band in the middle; furniture only at the far back against the wall.\n")
CONT = ("Use your built-in image generation tool to EDIT the attached image {seed} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {seed}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec (OUTPAINTING to the RIGHT):\n"
        "- The LEFT third of {seed} is the right end of the previous background picture. Keep that left third EXACTLY as it is (same pixels, same position, same scale). "
        "Replace ALL the solid magenta area to the right of it by CONTINUING the same scene seamlessly to the right: the floor line, the baseboard, the wall height, the perspective, the lighting, the scale and the flat style "
        "must continue exactly, so the two pictures join into one long horizontal panorama. Result is 1536x1024 landscape, full frame, no borders, no magenta left anywhere.\n")
P = {}
# 가로 집 파노라마 (쥐 · 고양이 없음): 부엌 → 거실 → 복도 (오른쪽 끝 쥐구멍)
P['pano_a'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE') + FLOOR +
               "- The KITCHEN of " + HOUSE + ": against the back wall a big cream cooking STOVE with a kettle, a long wooden COUNTER with cupboards, a tall round-cornered mint REFRIGERATOR at the left, "
               "a window with red-checked curtains above the counter, hanging pots and a frying pan, a kitchen table with a red-checked tablecloth at the far right edge (its legs in the back). "
               "The scene continues off the right edge (a doorway into the next room).\n", ['ref_flat_bg.png'])
P['pano_b'] = (CONT.format(seed='ref_seed_b.png', f='{f}') + BG.format(size='1536x1024 LANDSCAPE') + FLOOR +
               "- Continuing to the right: a wide open doorway leads from the kitchen into the cozy LIVING ROOM of " + HOUSE + ": a big soft tomato-red SOFA with round cushions against the wall, "
               "a tall standing lamp, framed pictures on teal striped wallpaper (no faces, no letters), a tall wooden grandfather clock, a closed wooden DOOR in the wall at the right part. "
               "The scene continues off the right edge.\n", ['ref_seed_b.png', 'ref_flat_bg.png'])
P['pano_c'] = (CONT.format(seed='ref_seed_c.png', f='{f}') + BG.format(size='1536x1024 LANDSCAPE') + FLOOR +
               "- Continuing to the right: a long narrow HALLWAY of " + HOUSE + ": a tall wooden BOOKSHELF full of books, a long tomato-red RUNNER RUG on the floor near the left (flat, thin, it does not block the floor), "
               "a coat stand, and at about 78% of the image width a small arched MOUSE HOLE cut into the wooden baseboard right at floor level (a dark rounded arch, about as tall as the baseboard, "
               "with a tiny welcome mat in front of it). The hallway wall ends at the right edge.\n", ['ref_seed_c.png', 'ref_flat_bg.png'])
# ① 부엌에서 치즈 → 고양이 그림자 (편집)
P['shot_open1'] = (HEAD + SHOT + STYLE + CHAR +
                   "- SCENE, a LOW-ANGLE MEDIUM SHOT on the kitchen floor of " + HOUSE + " (big stove and counter behind, soft flat): Jwerry sits on the floor in the center hugging a huge yellow CHEESE WEDGE with holes, "
                   "taking a big happy bite, eyes closed in bliss, cheeks full, little flat crumbs. The mouse fills about 45% of the image height, lower center. Plenty of empty wall behind and above it.\n", SREFS)
P['shot_open2'] = (EDIT.format(src='shot_open1.png', f='{f}', bgkeep=', the same background') +
                   "- A HUGE dark blue-grey SHADOW of a cat falls over the mouse from behind (the shadow shape of a big cat standing on its hind legs, claws raised, covering the wall and the mouse, two glowing yellow-green eyes in the shadow). "
                   "Jwerry FREEZES in shock: eyes wide open as white dots with tiny pupils, whiskers and ears standing straight up, a big flat light-blue sweat drop, the cheese half-bitten in its paws. Nothing else changes.\n", ['shot_open1.png'])
# ② 고양이 얼굴 (입맛 다심, 사선 로우 앵글)
P['shot_catface'] = (HEAD + SHOT + STYLE + CAT +
                     "- SCENE, an EXTREME CLOSE-UP LOW-ANGLE shot looking UP at the Tomcat's face with a DUTCH TILT (tilted about 15 degrees), the kitchen ceiling lamp behind its head: "
                     "a sly greedy grin with sharp cartoon teeth, the pink tongue licking its lips, eyes half-closed and scheming, one paw raised close to the camera with its claws popped out. The face fills most of the frame.\n", SREFS)
# 셀 프레임: 줴리 달리기 · 고양이 달리기 · 줴리 동작 · 고양이 동작
P['anim_jrun'] = (HEAD + SHEET + "- CAMERA: every frame is a straight SIDE VIEW, the mouse runs to the RIGHT. All frames share the same ground line.\n" + STYLE + CHAR + MAG +
                  "- A super-fast cartoon RUN CYCLE of Jwerry fleeing (leaning forward, ears flapping back, tail streaming straight behind, a cheeky determined grin):\n"
                  "  1) legs stretched far apart front and back, body leaning forward (reach);\n"
                  "  2) legs gathered under the body, body slightly up (passing);\n"
                  "  3) the back foot pushing off, the front foot landing, arms pumping.\n", CREFS + ['ref_flat_anim2.png'])
P['anim_tomrun'] = (HEAD + SHEET + "- CAMERA: every frame is a straight SIDE VIEW, the cat runs to the RIGHT. All frames share the same ground line.\n" + STYLE + CAT + MAG +
                    "- A long-legged cartoon CHASE RUN CYCLE of the Tomcat (body stretched forward, front paws reaching ahead with claws out, a greedy grin, tongue flapping, tail streaming behind):\n"
                    "  1) legs stretched far apart front and back (gallop reach);\n"
                    "  2) legs gathered together under the body, back arched up;\n"
                    "  3) the front paws landing, back legs kicking off.\n", CREFS + ['ref_flat_anim2.png'])
P['anim_jmove'] = (HEAD + SHEET + "- CAMERA: every frame is a SIDE VIEW, the mouse faces and moves to the RIGHT. All frames share the same ground line.\n" + STYLE + CHAR + MAG +
                   "- Three separate key poses of Jwerry:\n"
                   "  1) running to the right while LOOKING BACK over its shoulder at the viewer's left, sticking its tongue out and waving one paw (taunting);\n"
                   "  2) a head-first DIVE: the whole body stretched out horizontally in the air like a flying superhero, both arms straight forward to the right, tail straight behind;\n"
                   "  3) a cartoon DASH START: crouched low, legs spinning as a blurry wheel of feet, a small flat dust puff behind, eyes determined.\n", CREFS + ['ref_flat_anim2.png'])
P['anim_tommove'] = (HEAD + SHEET + "- CAMERA: every frame is a SIDE VIEW, the cat faces and moves to the RIGHT. All frames share the same ground line.\n" + STYLE + CAT + MAG +
                     "- The cat must look EXACTLY like the cat in the attached anim_tomrun.png (the same light blue-grey color, the same white muzzle, chest, paws and tail tip, the same head, the same long body proportions and drawing style), "
                     "and it runs on all fours like in anim_tomrun.png unless the pose says otherwise.\n"
                     "- Three separate key poses of the Tomcat (each well inside its own third, with a wide magenta gap between frames):\n"
                     "  1) still running on all fours after the bonk: its whole face is FLATTENED into a flat round disc (the shape of a frying pan's bottom), teeth chattering, little flat yellow stars around its head, but its legs keep running;\n"
                     "  2) running FURIOUS: red-faced, flat white STEAM puffs shooting out of both ears, teeth clenched, fists pumping;\n"
                     "  3) a huge flying POUNCE: the whole body stretched horizontally in the air toward the right, both front paws reaching forward with claws out, mouth wide open.\n", ['anim_tomrun.png'] + CREFS)
# ④ 반격 1: 조리대 위 프라이팬 홈런 (편집)
P['shot_pan1'] = (HEAD + SHOT + STYLE + CHAR + CAT +
                  "- SCENE, a SIDE VIEW of the kitchen of " + HOUSE + ": Jwerry stands on the edge of the wooden COUNTER at the right, gripping the long handle of a big black FRYING PAN with both paws and winding it back over its shoulder like a baseball batter, a cheeky grin. "
                  "At the left, the Tomcat LEAPS up from the floor toward the mouse, mid-air, body stretched, claws out, mouth open, eyes greedy. The cat is big (about four times the mouse).\n", SREFS)
P['shot_pan2'] = (EDIT.format(src='shot_pan1.png', f='{f}', bgkeep=', the same background') +
                  "- BONK! Jwerry has SWUNG the frying pan full force: the pan is now in front of the mouse and SMACKS the Tomcat's face; the cat's whole face is squashed FLAT into the round shape of the pan's bottom, "
                  "its body crumpled in mid-air, eyes popped, a few teeth flying, a big flat cream-and-yellow BONK burst at the hit point with flat yellow stars. The mouse is in follow-through, one foot up. Nothing else changes.\n", ['shot_pan1.png'])
# ⑥ 반격 2: 문 쾅 → 납작 → 종이처럼 떨어짐 (편집)
P['shot_door1'] = (HEAD + SHOT + STYLE + CHAR + CAT +
                   "- SCENE, a LOW three-quarter view in the living room of " + HOUSE + ": a tall wooden DOOR stands wide OPEN toward the camera on the right. Jwerry hides at the right behind the door's edge, holding the door edge with both paws, "
                   "peeking out with a sneaky grin. In the left background the Tomcat charges toward the door at full speed (in perspective, coming closer), claws out, furious.\n", SREFS)
P['shot_door2'] = (EDIT.format(src='shot_door1.png', f='{f}', bgkeep=', the same background') +
                   "- SLAM! Jwerry has swung the door SHUT (the door is now closed, flat across the frame) and the Tomcat has smashed into it: the cat's whole body is FLATTENED paper-thin against the closed door like a pancake in the exact outline of a cat, "
                   "limbs spread, eyes as two X marks, short flat shaking lines around the door. Jwerry stands at the right edge dusting off its paws. Nothing else changes.\n", ['shot_door1.png'])
P['shot_door3'] = (EDIT.format(src='shot_door2.png', f='{f}', bgkeep=', the same background, the same closed door') +
                   "- The paper-thin flattened Tomcat PEELS OFF the door and falls backward like a sheet of paper, curling and fluttering at the top, its flat face dizzy. Jwerry is gone from the frame (only a small flat dust puff where it stood). Nothing else changes.\n", ['shot_door2.png'])
# ⑧ 반격 3: 카펫 당기기 → 공중제비 (편집)
P['shot_rug1'] = (HEAD + SHOT + STYLE + CHAR + CAT +
                  "- SCENE, an eye-level wide SIDE VIEW in the hallway of " + HOUSE + " (a tall bookshelf behind): a long tomato-red RUNNER RUG lies across the floor; the Tomcat runs along the rug from the left toward the right, "
                  "eyes locked on the mouse, steam from its ears. At the right end of the rug Jwerry crouches and grips the rug's edge with both paws, leaning back, ready to YANK it, a sly grin.\n", SREFS)
P['shot_rug2'] = (EDIT.format(src='shot_rug1.png', f='{f}', bgkeep=', the same background') +
                  "- YANK! Jwerry has pulled the rug: the rug whips up in a big wave, the Tomcat is flung high into the air UPSIDE DOWN, legs flailing, eyes popping, mouth open in a comic scream, "
                  "short flat motion lines, a few books tumbling off the bookshelf. Jwerry leans back holding the rug edge, laughing. Nothing else changes.\n", ['shot_rug1.png'])
# ⑩ 벽에 얼굴부터 쾅 (아코디언) → 주르륵 기절 (편집)
P['shot_wall1'] = (HEAD + SHOT + STYLE + CAT +
                   "- SCENE, a close SIDE VIEW at floor level of the hallway wall of " + HOUSE + ": the wooden baseboard with a small arched MOUSE HOLE at the right of center (a dark arch, a tiny welcome mat), "
                   "just the tip of a thin pink mouse tail vanishing into the hole. The Tomcat has CRASHED FACE-FIRST into the wall right beside the hole: its face is squashed flat against the wall, "
                   "its body behind it is crumpled like an ACCORDION (folded zigzag), its tail bent in a zigzag, flat crack lines on the wall around its face, a flat cream burst.\n", SREFS)
P['shot_wall2'] = (EDIT.format(src='shot_wall1.png', f='{f}', bgkeep=', the same background, the same mouse hole') +
                   "- The Tomcat SLIDES DOWN the wall and sits slumped on the floor, totally dizzy: eyes as two spirals, tongue hanging out, a big round bump on its head, three flat yellow STARS circling above it, "
                   "its face still a bit flat; the crack lines stay on the wall. The pink tail tip is gone (inside the hole). Nothing else changes.\n", ['shot_wall1.png'])
# ⑪ 쥐구멍에서 빼꼼 → 메롱 윙크 (편집)
P['shot_hole1'] = (HEAD + SHOT + STYLE + CHAR +
                   "- SCENE, an EXTREME CLOSE-UP straight FRONT VIEW of the small arched MOUSE HOLE in the wooden baseboard (the hole fills the middle of the frame, dark inside with a warm cozy glow deep in), "
                   "a tiny welcome mat in front. Jwerry peeks out of the hole from the waist up, holding the big yellow cheese wedge under one arm, a smug satisfied grin.\n", SREFS)
P['shot_hole2'] = (EDIT.format(src='shot_hole1.png', f='{f}', bgkeep=', the same background, the same mouse hole') +
                   "- Jwerry now WINKS one eye and sticks its tongue out cheekily, pulling down the lower eyelid of the other eye with one finger (a playful 'nyah' face), the cheese still under its arm, "
                   "a couple of flat cream sparkles. Nothing else changes.\n", ['shot_hole1.png'])
# 소품 · 이펙트
P['props_flat'] = (HEAD + FXS.replace('effect art', 'prop art') +
                   "  1. a big black FRYING PAN seen from the side, the long handle pointing to the right\n"
                   "  2. a big yellow CHEESE WEDGE with round holes\n"
                   "  3. a single falling hardcover BOOK, half open (no letters)\n"
                   "  4. a TALL potted house PLANT (a terracotta pot with big flat green leaves), as tall as the cell\n"
                   "  5. a wooden kitchen CHAIR seen from the side, as tall as the cell\n"
                   "  6. a big flat light-blue SWEAT DROP\n", ['ref_flat_fx.png', 'ref_rig.png'])
P['fx_flat'] = (HEAD + FXS +
                "- Palette: cream white, butter yellow, warm light grey, a little tomato red.\n"
                "  1. a flat cartoon DUST PUFF cloud trail (cream and pale grey, three round puffs in a row getting smaller to the left)\n"
                "  2. a long thin horizontal SPEED STREAK (a flat white tapered line, thick at the left, pointed at the right)\n"
                "  3. a ring of three flat yellow DIZZY STARS circling (seen slightly from above, an ellipse)\n"
                "  4. a big flat cartoon BONK burst (a jagged cream-white star burst with a butter-yellow center)\n"
                "  5. a flat white STEAM puff shooting up (two round puffs)\n"
                "  6. a wide flat SHOCKWAVE RING seen from the front (a thin wide ellipse of cream-white, about 4 times wider than tall)\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (HEAD + FXS +
               "- Palette: black and dark grey (pan), cream white, butter yellow.\n"
               "  top row (each one alone, centered):\n"
               "  1. a big black FRYING PAN falling straight DOWN, seen from the side at an angle (the round pan bottom facing DOWN, the handle pointing up-right)\n"
               "  2. the same frying pan tumbling, tilted the other way\n"
               "  3. a single flat yellow five-pointed cartoon STAR\n"
               "  bottom row = 3 consecutive frames of a PAN BONK hit, same size and centered at the same point:\n"
               "  4. a small round cream-white impact flash with short spikes\n"
               "  5. a bigger flat cream and butter-yellow BONK burst with three small yellow stars popping out\n"
               "  6. a ring of three flat yellow DIZZY STARS circling (an ellipse), fading\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', encoding='utf-8', newline='\n').write('''#!/usr/bin/env bash
# 줴리 초필살기 v2 그림 (톰과 줴리 추격전) Codex — 가로 집 파노라마 이어 그림 · 컷 한 장 + 연속 프레임 편집 · 셀 시트
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/jwerry/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
seed() { python -c "from PIL import Image; p=Image.open('$1.png').convert('RGB').resize((1536,1024)); c=Image.new('RGB',(1536,1024),(255,0,255)); c.paste(p.crop((1024,0,1536,1024)),(0,0)); c.save('$2')"; }
pano() { run pano_a; seed pano_a ref_seed_b.png; run pano_b; seed pano_b ref_seed_c.png; run pano_c; }
pair() { run $1; shift; for n in "$@"; do run $n & done; wait; }
chain() { for n in "$@"; do run $n; done; }
pano &
pair shot_open1 shot_open2 &
pair shot_pan1 shot_pan2 &
chain shot_door1 shot_door2 shot_door3 &
pair shot_rug1 shot_rug2 &
pair shot_wall1 shot_wall2 &
pair shot_hole1 shot_hole2 &
wait
for n in shot_catface anim_jrun anim_tomrun anim_jmove anim_tommove props_flat fx_flat fx_hit; do run $n & done; wait; echo DONE
''')
print(sorted(P))
