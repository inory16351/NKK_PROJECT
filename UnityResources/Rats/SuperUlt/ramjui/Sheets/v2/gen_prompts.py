"""람쥐 초필살기 v2 = '람쥐썬더 밈 몽타주' 프롬프트 (2026-10-20 사용자: 람쥐썬더 밈 짤을 게임 그림체 몽타주 컷신으로 · 쥐는 셀 애니로 자연스럽게)
밈 출처 (나무위키 '람쥐썬더'): 2007 디시 던파갤 제목낚시 "얘들아 이거 빨리 봐라 → 람쥐썬더!!!" · 번개 합성 다람쥐 짤(두 손 들고 번개) ·
  '찌리찌리' 번쩍이는 업그레이드판 · '토르 램지' 슈퍼히어로 랜딩 + "Bring me Acorn!" (사실은 뒷발로 겨드랑이 긁는 순간포착) · 수고람쥐썬더
저작권: 원본 사진 · 특정 히어로 의상 · 실존 인물 없이 '구도 · 상황'만 게임 그림체로 다시 그림. 글자 없음 (제목 · 대사는 유니티 TMP).
몽타주 컷: ① 낡은 PC 게시글(제목낚시) → ② 고전 람쥐썬더(두 손 번개) → ③ 찌리찌리 → ④ 하늘에서 도토리 망치 슈퍼히어로 랜딩 → ⑤ 반전: 사실 겨드랑이 긁는 중 → ⑥ 클로즈업 '수고람쥐썬더'
→ UnityResources/Rats/SuperUlt/ramjui/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'ramjui', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(PH, 'anim_command.png'), 'ref_flat_anim2.png'), (os.path.join(R, 'SuperUlt', 'ref_zap_style.png'), 'ref_zap_style.png'),
                 (os.path.join(R, 'SuperUlt', 'ramjui', 'Sheets', 'ref_char.png'), 'ref_char.png'), (os.path.join(R, 'SuperUlt', 'ramjui', 'Sheets', 'ref_parts.png'), 'ref_parts.png')):
    shutil.copy(src, os.path.join(D, dst))
ims = [Image.open(os.path.join(R, 'Parts', 'ramjui', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
BG = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO text, NO letters, NO numbers, no logos, no real brand or website UI.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: {pal}.\n")
STORM = "stormy night forest — deep navy and slate blue sky, teal-green oak leaves, warm brown bark, electric pale-cyan and lemon-yellow light accents"
V = "- This picture is ONE SEGMENT of a very TALL vertical panorama that a camera tilts through (top -> middle -> bottom). Segments are stacked vertically, so the composition must continue at the top and bottom edges.\n"
CONT = "- CONTINUITY IS CRITICAL: the attached ref_prev_bottom is the BOTTOM STRIP of the segment directly ABOVE this one. The TOP 30% of your picture must continue it seamlessly (same shapes at the same positions, same colors).\n"
SEG = {
    'a': '1 (TOP) of 3: a wide STORMY NIGHT SKY: big flat swirling storm cloud bands in navy and slate, a few flat pale-cyan light patches inside the clouds, the CENTER of the sky is EMPTY and open (a hero will fall from there)',
    'b': '2 (MIDDLE) of 3: seen FROM THE FRONT, symmetric: the round flat CANOPIES of huge old OAK TREES at the left and right edges, a few flat acorns hanging, an EMPTY gap of open sky in the center between the trees, flat leaves blowing in the wind',
    'c': '3 (BOTTOM) of 3: seen FROM THE FRONT: the forest FLOOR at night: a big flat cut TREE STUMP in the lower-middle (its flat top is EMPTY, a character will stand on it), mossy ground, a few mushrooms and fallen acorns at the sides, flat puddles reflecting cyan light',
}
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the character's in-game sprite parts: copy its colors, shapes and proportions) and the approved flat cutscene frames ref_flat_anim / ref_flat_anim2 / ref_zap_style: "
         "FLAT cartoon storybook look in the Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, small simple black bead eyes with one tiny white highlight, "
         "no black outlines, no gloss, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo.\n- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- CHARACTER "Ramjui the Thunder Squirrel": build it EXACTLY from the attached ref_rig and ref_char / ref_parts (use ref_char only for the colors and the character design, NOT its painted anime look): '
        'a cute chubby orange-red SQUIRREL with tall tufted ears (pink inside), a cream belly and cream muzzle, a small brown nose, a HUGE fluffy curled tail as big as its body, small paws. Big head, small body.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery, no effects, no lightning. Do not use magenta or purple on the subjects. Keep everything fully inside the frame with margin.\n"
ANIM = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
        "Frames are consecutive key drawings of one motion for a cel-animated meme cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses. "
        "Lightning and sparks are NOT drawn here (they are separate effects).\n")
FRONT = "- CAMERA: every frame is a straight FRONT VIEW (the character faces the viewer / the camera).\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_flat_anim2.png']
P = {}
for k, d in SEG.items():
    P['pano_' + k] = (BG.format(pal=STORM) + V + (CONT if k != 'a' else '') + '- SEGMENT ' + d + '.\n', ['ref_flat_bg.png'] + (['ref_prev_bottom.png'] if k != 'a' else []))
# ① 제목낚시: 2007년 갤러리 시절 낡은 컴퓨터 화면 (글 제목은 유니티 TMP 로 화면 위에)
P['bg_post'] = (BG.format(pal="late-night bedroom — dim navy and plum, a warm desk-lamp yellow pool, the cool pale-blue glow of an old monitor") +
                "- An old 2000s beige CRT computer MONITOR seen straight from the FRONT, filling about 70% of the frame, on a messy wooden desk at night (a keyboard, an empty instant-noodle cup, an acorn on the desk). "
                "The monitor SCREEN shows a very simple flat blank old-forum page: a pale grey-blue page with one EMPTY title bar at the top and one big EMPTY white picture box in the middle (completely blank, NO letters, NO icons). "
                "The screen is a big clean flat rectangle so text can be overlaid later.\n", ['ref_flat_bg.png'])
# ②③ 고전 람쥐썬더 짤 배경: 번개 치는 들판 (사진 합성 짤을 플랫으로)
P['bg_zap'] = (BG.format(pal=STORM) + "- A dramatic flat STORM backdrop for a classic meme shot: a low flat grassy forest clearing at the bottom, a few round oak tree silhouettes at the sides, "
               "huge flat storm clouds, and big flat radial light bands in pale cyan and navy spreading from the lower-middle (where a squirrel will stand). The lower-middle is EMPTY.\n", ['ref_flat_bg.png'])
P['face_bg'] = (BG.format(pal=STORM) + "- A FLAT cut-in backdrop for a close-up: big flat zig-zag LIGHTNING-shaped bands in lemon yellow, pale cyan and deep navy radiating from the center, empty middle area where a face will be placed on top.\n", ['ref_flat_bg.png'])
P['face_close'] = ("- 1536x1024 LANDSCAPE extreme CLOSE-UP of the character's FACE straight from the FRONT, face filling most of the height, centered. Keep the simple bead eyes.\n" + STYLE + CHAR + MAG +
                   "- 'Good work, thunder!' farewell: a big proud grin, one eye WINKING, one paw raised to the brow in a cheeky two-finger SALUTE, a tiny acorn tucked behind one ear, the fur slightly puffed up with static.\n", CREFS)
A = {
    # ② 고전 짤: 똑바로 서서 두 손을 들고 번개를 쏘는 다람쥐
    'anim_zap': "- The classic 'thunder squirrel' meme pose, FULL BODY, FRONT VIEW, standing UPRIGHT on the hind legs like a person:\n"
                "  1) surprised: standing straight, both small paws curled in front of the chest, eyes wide, ears up; "
                "2) charging: both paws pushed FORWARD at the viewer with fingers spread, knees bent, tail fluffed up, determined frown; "
                "3) THUNDER: both arms flung HIGH above the head in a V, chest out, mouth wide open shouting, fur and tail puffed up big, feet apart.",
    # ③ 찌리찌리 업그레이드판: 감전된 듯 덜덜 (빠르게 돌림)
    'anim_jiri': "- A comic 'electrified' jitter loop (bzzt bzzt), FULL BODY, FRONT VIEW, standing upright, arms raised, the SAME overall position in all frames so it can loop fast:\n"
                 "  1) arms up, fur standing on end in a big spiky puff, eyes squeezed shut, teeth clenched in a grin; "
                 "2) the same but the body shaken a little to the left, fur spikier, one eye open, tail frizzed into a zig-zag; "
                 "3) the same but shaken to the right, both eyes wide open and dizzy, mouth open, ears crackling straight up.",
    # ④ '토르 램지' 슈퍼히어로 랜딩 + 도토리 망치 (특정 히어로 의상 없음: 작은 날개 투구 · 빨간 천 망토만)
    'anim_land': "- A heroic SUPERHERO LANDING, FULL BODY, FRONT VIEW. The squirrel wears a tiny round silver helmet with two small white wings and a short red cloth cape, and holds a big HAMMER whose head is a giant ACORN (wooden handle):\n"
                 "  1) DIVING down from the sky head first toward the viewer, the acorn hammer held forward in both paws, cape and tail streaming up; "
                 "2) the classic SUPERHERO LANDING: one knee down, one fist pressed to the ground, the acorn hammer planted beside it, head down, cape and tail flaring up; "
                 "3) rising to stand, holding the acorn hammer HIGH toward the sky with one paw, the other paw on the hip, heroic smile.",
    # ⑤ 반전: 사실은 뒷발로 겨드랑이를 긁던 순간 (나무위키 각주)
    'anim_scratch': "- The funny TRUTH behind the heroic pose: the same squirrel (NO helmet, NO cape, NO hammer), FULL BODY, FRONT VIEW, sitting in a low crouch exactly like the landing pose:\n"
                    "  1) crouched low, one hind foot raised and SCRATCHING its own ARMPIT, eyes closed in bliss, tongue tip out; "
                    "2) the same, foot scratching faster, one ear flopped, very happy face; "
                    "3) FROZEN, caught by the camera: hind foot still up in the armpit, eyes wide open staring at the viewer, a flat blue sweat drop, awkward embarrassed smile.",
}
for n, b in A.items():
    P[n] = (ANIM + FRONT + STYLE + CHAR + MAG + b + "\n", CREFS)
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, purple or pink on the elements.\n- NO text, NO letters, NO numbers.\n")
P['fx_flat'] = (FXS + "- Palette: lemon yellow, pale cyan, sky blue, cream white, a little navy.\n"
                "  1. a big flat zig-zag LIGHTNING BOLT, vertical, tall (fills the cell height): cream-white core with a pale cyan edge band\n"
                "  2. a FORKED lightning bolt with 3 branches, vertical, in lemon yellow with a white core\n"
                "  3. a short flat ELECTRIC ARC: a wiggly horizontal zig-zag spark line in pale cyan and white (the arc between two raised paws)\n"
                "  4. a flat round ELECTRIC BURST: a cream-white disk with short jagged yellow rays\n"
                "  5. a flat SHOCK RING: a thin jagged cyan ring with small zig-zag spikes, perfect circle\n"
                "  6. a flat cluster of 6 small yellow SPARKS (tiny zig-zags and rounded diamonds)\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (FXS + "- Palette: lemon yellow, pale cyan, cream white, warm brown, slate grey.\n"
               "  1. a wide flat SHOCKWAVE ring seen in perspective (a thin wide ellipse of cream white and cyan, about 4 times wider than tall)\n"
               "  2. a flat cluster of cracked GROUND CHUNKS and pebbles in brown and slate\n"
               "  3. a flat SPEED LINE streak (a vertical tapered white and cyan streak)\n"
               "  4. LANDING IMPACT frame 1: a small flat white-yellow burst on the ground with a few dirt chunks\n"
               "  5. LANDING IMPACT frame 2: a wide flat dome burst of cream white and cyan with zig-zag lightning edges and flying ground chunks\n"
               "  6. LANDING IMPACT frame 3: the end: a wide low dust cloud in warm grey with small fading sparks\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['props_flat'] = (FXS.replace("effect art", "prop art").replace("Do not use magenta, purple or pink on the elements.", "Do not use magenta or purple on the elements.") +
                   "  1. a big flat HAMMER whose head is a giant ACORN (brown cap, golden nut) with a wooden handle\n"
                   "  2. a single flat ACORN (brown cap, golden nut)\n"
                   "  3. a tiny round silver HELMET with two small white wings\n"
                   "  4. a flat white POLAROID-style photo frame card, slightly tilted, with a big EMPTY flat light-grey picture area (completely blank)\n"
                   "  5. a flat old-computer MOUSE CURSOR arrow in white with a navy border, pointing up-left\n"
                   "  6. a flat oak LEAF in teal green\n", ['ref_flat_fx.png', 'ref_flat_bg.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + b)
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', newline='\n').write('''#!/usr/bin/env bash
# 람쥐 초필살기 v2 그림 (람쥐썬더 밈 몽타주: 제목낚시 PC · 두 손 번개 · 찌리찌리 · 도토리 망치 랜딩 · 겨드랑이 반전 · 수고람쥐썬더) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/ramjui/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pano() { run pano_a; python -c "from PIL import Image; im=Image.open('pano_a.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_b
  python -c "from PIL import Image; im=Image.open('pano_b.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_c; }
pano &
for n in bg_post bg_zap face_bg face_close anim_zap; do run $n & done; wait
for n in anim_jiri anim_land anim_scratch fx_flat fx_hit props_flat; do run $n & done; wait; echo DONE
''')
print(sorted(P))
