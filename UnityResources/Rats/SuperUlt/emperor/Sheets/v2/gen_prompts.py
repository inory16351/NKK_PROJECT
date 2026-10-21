"""쥐 황제 초필살기 v2 = '총병 앞으로 · 장전 · 격발' 프롬프트 (2026-10-20 사용자: 에픽세븐 릴리아스 3스킬 참고 · 총 든 쥐 병사를 잔뜩 소환 → 일제히 난사 · 총알이 맵을 덮음 · 왼쪽 → 오른쪽)
컷: ① 황제가 칼을 높이 듦 ② 총병 앞으로: 넓은 들판에 총 든 쥐 병사가 줄줄이 소환 ③ 장전: 손 클로즈업 (공이치기를 당김 → 철컥)
    ④ 격발: 황제가 칼을 앞으로 내리침 ⑤ 일제 사격 난사 (병사 셀 프레임 조준 ↔ 격발, 총구 섬광 · 연기 · 총알) ⑥ 총알이 화면을 덮음 → 게임 화면 총알 비 (왼 → 오)
그림체 = 다른 v2 컷신과 같은 플랫 (ref_flat_bg = 로비 배경 · ref_flat_anim = 파라오 셀 프레임 · ref_flat_face = 치즈 퐁듀 클로즈업 · ref_rig = 황제 게임 파츠). 글자 없음.
→ UnityResources/Rats/SuperUlt/emperor/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'emperor', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'),
                 (os.path.join(R, 'SuperUlt', 'fondue', 'Sheets', 'cu_face1.png'), 'ref_flat_face.png')):
    shutil.copy(src, os.path.join(D, dst))
ims = [Image.open(os.path.join(R, 'Parts', 'emperor', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 10 * len(ims) + 30, max(i.height for i in ims) + 40), (255, 0, 255)); x = 20
for i in ims: W.paste(i, (x, 20), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same character size and position, the same colors and flat style, the same background. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = "imperial East Asian palette — warm golden sunrise sky, crimson red and imperial yellow, jade green accents, dark grey-blue roof tiles, warm earth ground"
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the emperor's in-game sprite parts: copy its colors, shapes and proportions), "
         "the approved flat cutscene art ref_flat_anim / ref_flat_face, and the approved flat background ref_flat_bg: "
         "FLAT cartoon storybook look in the Untitled Goose Game mood, big simple soft flat shapes, flat colors with only gentle soft shading (at most one shade step), very little texture, NO individual fur strands, "
         "small simple black bead eyes with one tiny white highlight, no black outlines, no gloss, NO painterly brush strokes, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo. Palette: " + PAL + ".\n"
         "- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
EMP = ('- CHARACTER "Rat Emperor": build it EXACTLY from the attached ref_rig: a cute chubby GREY rat with big round pink ears, a pink nose, white whiskers, small black bead eyes, '
       'a small round golden imperial hat with a red band and a red knob on top, an imperial YELLOW dragon robe with a red collar trim, a round golden dragon emblem on the chest, little blue cloud patterns and blue wave patterns at the hem, '
       'small grey paws with pink fingers, a long pink tail. Big head, small body.\n')
SOLDIER = ('- CHARACTER "Rat Musketeer" (an imperial guard soldier of the emperor, same art style and proportions as ref_rig): a cute chubby GREY rat with big round pink ears, a pink nose, small black bead eyes, '
           'a wide conical RED soldier hat with a yellow trim and a small red tassel on top, a CRIMSON RED padded soldier tunic with a YELLOW border and a round yellow badge on the chest, dark grey-blue trousers, '
           'a long wooden MUSKET (matchlock rifle) with a brass hammer and a long dark steel barrel, a long pink tail. Big head, small body.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta or purple on the subject. Keep the subject's edges clean.\n"
CREFS = ['ref_rig.png', 'ref_flat_anim.png', 'ref_flat_face.png', 'ref_flat_bg.png']
P = {}
# ②⑤ 넓은 들판 (병사가 줄지어 서는 곳, 쥐 없음)
P['bg_field'] = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency). NO characters, NO animals, NO people, NO text.\n"
                 "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
                 "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, NO painterly brush strokes. Palette: " + PAL + ".\n"
                 "- A wide SIDE-VIEW parade ground in front of an East Asian imperial palace at sunrise: the flat warm earth ground fills the LOWER 45% of the picture (flat and empty, soldiers will stand on it), "
                 "a long low crimson palace wall with dark grey-blue tiled roof running across the middle, a big palace gate tower with golden roofs at the far LEFT, a few red banners on poles, "
                 "a warm golden sky with a few flat clouds above. Simple and readable, big flat shapes, horizon at about 50% of the image height.\n", ['ref_flat_bg.png'])
P['face_bg'] = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency). NO characters, NO text.\n"
                "- STYLE: a FLAT 2D game background in the style of the attached ref_flat_bg (style only): big simple flat color shapes, flat fills, NO outlines, NO gloss, NO painterly brush strokes.\n"
                "- A FLAT cut-in backdrop for a dramatic close-up: big flat radial RAYS in crimson red, imperial yellow and gold, a few jade green bands, radiating from the center, a calm plain middle area. Flat shapes only.\n", ['ref_flat_bg.png'])
# ①④ 황제: 칼을 높이 듦 → 앞으로 내리침 (같은 틀 2 프레임)
P['cu_emp1'] = ("- 1536x1024 LANDSCAPE MEDIUM CLOSE-UP of the Rat Emperor from the waist up, SIDE VIEW facing RIGHT (3/4), slightly LOW ANGLE (heroic), placed in the LEFT-CENTER of the frame. "
                "He raises an ornate golden imperial SABER high above his head with his right paw (the blade pointing up and back), his left paw on his hip, chin up, a stern commanding face, mouth closed, the robe sleeves fluttering.\n"
                + STYLE + EMP + MAG, CREFS)
P['cu_emp2'] = (EDIT.format(src='cu_emp1.png', f='{f}') +
                "- He SWINGS the golden saber DOWN and FORWARD: the saber now points straight ahead to the RIGHT at shoulder height (arm fully stretched forward), "
                "his mouth WIDE OPEN shouting a command, eyebrows down, fierce, the robe sleeve whipping, a few short flat motion lines behind the swing. Nothing else changes.\n", ['cu_emp1.png'])
# ③ 장전: 손 클로즈업 (2 프레임)
P['cu_load1'] = ("- 1536x1024 LANDSCAPE extreme CLOSE-UP, SIDE VIEW: the musket of a Rat Musketeer held horizontally across the frame, the barrel pointing to the RIGHT and out of the frame, "
                 "the soldier's two grey paws (pink fingers) and red sleeves with yellow cuffs gripping it, his THUMB pulling the big brass HAMMER back (half way), the other finger near the trigger. Only paws, sleeves and the musket, no face.\n"
                 + STYLE + SOLDIER + MAG, CREFS)
P['cu_load2'] = (EDIT.format(src='cu_load1.png', f='{f}') +
                 "- The brass hammer is now fully COCKED back (click!), the thumb released, the finger resting on the trigger, a tiny flat white click spark next to the hammer. Nothing else changes.\n", ['cu_load1.png'])
P['cu_aim'] = ("- 1536x1024 LANDSCAPE CLOSE-UP, SIDE VIEW facing RIGHT: a Rat Musketeer AIMING down the long musket barrel: his cheek pressed to the wooden stock, ONE eye closed (the left), the other eye open and focused, "
               "the conical red hat, the barrel stretching out to the RIGHT edge of the frame. Tense and serious. The head is in the LEFT-CENTER of the frame.\n" + STYLE + SOLDIER + MAG, CREFS)
# ②⑤ 병사 셀 프레임: 행진(총을 어깨에) · 조준 · 격발 (같은 크기 · 같은 바닥선)
P['anim_soldier'] = ("- 1536x1024 image, 3 columns side by side: the SAME Rat Musketeer in 3 consecutive animation frames, FULL BODY, SIDE VIEW facing RIGHT, standing upright like a person, "
                     "each in its own column, the SAME size and scale in every column (about 75% of the image height), all standing on the SAME bottom line (feet at about 92% of the image height), none touching each other.\n"
                     + STYLE + SOLDIER + MAG +
                     "  1) MARCH: standing at attention, the musket resting on the right shoulder pointing up and back, chin up, a determined face\n"
                     "  2) AIM: feet apart, the musket raised and LEVELED horizontally pointing to the RIGHT at shoulder height, cheek on the stock, one eye closed\n"
                     "  3) FIRE: the same aiming pose but the musket kicks back a little (recoil), a big flat orange-yellow MUZZLE FLASH burst at the end of the barrel on the right, a small puff of white smoke, the eyes squeezed shut\n",
                     CREFS)
# 정면 사격 (2026-10-20 사용자 2차: 측면보다 일제히 정면으로 발사 · 총알 속도감이 드러나는 카메라 무빙)
P['bg_front'] = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency). NO characters, NO animals, NO people, NO text.\n"
                 "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg and the attached bg_field (same palace, same colors, same flat look): big simple flat color shapes, flat fills with at most one soft shade step, NO black outlines, NO gloss, NO painterly brush strokes. Palette: " + PAL + ".\n"
                 "- A FRONT VIEW (one-point perspective, looking straight ahead) of the same imperial parade ground: the big golden-roofed palace GATE TOWER centered in the far back, the long crimson palace walls going left and right behind it, "
                 "the flat warm earth ground filling the LOWER 55% and receding toward the gate (horizon at about 45% of the image height), a few red banners on poles at both sides, a warm golden sunrise sky. Symmetric, simple, empty ground (soldiers will stand on it).\n", ['ref_flat_bg.png', 'bg_field.png'])
P['anim_front'] = ("- 1536x1024 image, 3 columns side by side: the SAME Rat Musketeer in 3 consecutive animation frames, FULL BODY, FRONT VIEW (facing the viewer straight on), standing upright like a person, "
                   "each in its own column, the SAME size and scale in every column (about 75% of the image height), all standing on the SAME bottom line (feet at about 92% of the image height), none touching each other.\n"
                   + STYLE + SOLDIER + MAG +
                   "  1) READY: standing at attention facing the viewer, the musket held upright in front of the chest, a determined face\n"
                   "  2) AIM: the musket raised to the shoulder and pointed STRAIGHT AT THE VIEWER (strongly foreshortened: we look into the round dark muzzle hole in the middle of the body, the barrel very short), cheek on the stock, one eye closed\n"
                   "  3) FIRE: the same aiming pose, a big ROUND flat orange-yellow MUZZLE FLASH burst bursting toward the viewer right in front of the muzzle (a round star burst with a cream core), a little white smoke, eyes squeezed shut, slight recoil\n",
                   ['anim_soldier.png'] + CREFS)
P['cu_aimfront'] = ("- 1536x1024 LANDSCAPE extreme CLOSE-UP, FRONT VIEW: a Rat Musketeer AIMING his musket STRAIGHT AT THE VIEWER: the round dark MUZZLE hole of the barrel big in the exact CENTER of the frame (strongly foreshortened barrel), "
                    "behind it his face peeking along the stock: one eye closed, the other eye wide and focused at the viewer, the conical red hat on top, his paws on the musket. Tense, dramatic.\n" + STYLE + SOLDIER + MAG, ['cu_aim.png'] + CREFS)
P['cu_empfront'] = ("- 1536x1024 LANDSCAPE MEDIUM CLOSE-UP of the Rat Emperor from the waist up, FRONT VIEW facing the viewer, centered: he thrusts his ornate golden imperial SABER straight forward AT THE VIEWER "
                    "(strongly foreshortened: the blade tip comes toward the camera in the center, the arm stretched forward), mouth WIDE OPEN shouting the order to fire, eyebrows down, fierce, the yellow dragon robe sleeves whipping, a few flat motion lines.\n"
                    + STYLE + EMP + MAG, ['cu_emp2.png'] + CREFS)
P['fx_front'] = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
                 "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and fx_flat (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO painterly texture, NO black outlines.\n"
                 "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, purple or pink on the elements.\n- NO text, NO letters, NO numbers.\n"
                 "  1. a FRONTAL MUZZLE FLASH seen head-on: a ROUND star-shaped burst of orange and yellow fire with a cream white core (symmetric)\n"
                 "  2. ONE BULLET flying straight AT THE VIEWER, seen head-on: a round brass-golden bullet tip in the center with a thin flat round white shock ring around it\n"
                 "  3. ONE BULLET flying toward the viewer and slightly to the side, 3/4 view: a golden bullet with a SHORT tapered yellow-white streak behind it (pointing toward the top-left)\n"
                 "  4. a round RING of white-grey gun SMOKE (a donut of round puffs)\n"
                 "  5. a BULLET HOLE in glass: a small dark round hole with flat white CRACK lines radiating out (cracked glass), on magenta\n"
                 "  6. a thin flat wedge-shaped radial SPEED LINE in cream white (thin at one end, wide at the other)\n", ['ref_flat_fx.png', 'fx_flat.png'])
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, purple or pink on the elements.\n- NO text, NO letters, NO numbers.\n")
P['fx_flat'] = (FXS +
                "  1. a side MUZZLE FLASH: a flat star-shaped burst of orange and yellow fire pointing to the RIGHT (wider on the right), with a cream white core\n"
                "  2. a round puff of flat WHITE-GREY GUN SMOKE (3 overlapping round blobs)\n"
                "  3. ONE flying BULLET seen from the side moving to the RIGHT: a small round golden-brass bullet at the RIGHT end with a long thin tapered flat yellow-white tracer streak behind it (to the left), horizontal\n"
                "  4. a SUMMON PUFF: a flat ring burst of golden and white smoke with a few small gold sparkles (a soldier appears)\n"
                "  5. a small flat bullet HIT SPARK: a jagged white and yellow star burst with 4 short sparks\n"
                "  6. a flat horizontal SPEED LINE streak in cream white (long thin tapered stripe)\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (FXS +
               "  1. a LONG horizontal bullet TRACER: a very long thin flat golden-yellow streak with a bright cream tip on the RIGHT end (fading to the left)\n"
               "  2. a big wide cloud of flat white-grey GUN SMOKE drifting (a horizontal band of round blobs)\n"
               "  3. a small spray of flat brown DUST and pebbles kicked up\n"
               "  4. BULLET IMPACT frame 1: a small flat white-yellow flash just hitting\n"
               "  5. BULLET IMPACT frame 2: a bigger jagged flat yellow-orange burst with sparks flying\n"
               "  6. BULLET IMPACT frame 3: the end: a fading puff of grey smoke with a few small sparks\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
for n, (b, refs) in P.items():
    head = HEAD.format(f=n + '.png') if not b.startswith('Use your built-in') else ''
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(head + b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
RUN = r'''#!/usr/bin/env bash
# 쥐 황제 초필살기 v2 그림 (총병 앞으로 · 장전 · 격발) Codex — 1번 프레임 · 배경 · 이펙트 동시 / 연속 프레임은 1번이 나온 뒤 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/emperor/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
emp() { run cu_emp1; run cu_emp2; }
load() { run cu_load1; run cu_load2; }
emp & load &
for n in bg_field face_bg cu_aim anim_soldier; do run $n & done; wait
for n in fx_flat fx_hit; do run $n & done; wait
for n in bg_front anim_front cu_aimfront cu_empfront fx_front; do run $n & done; wait; echo DONE
'''
open(os.path.join(D, 'run_v2.sh'), 'w', encoding='utf-8', newline='\n').write(RUN)
print(sorted(P))
