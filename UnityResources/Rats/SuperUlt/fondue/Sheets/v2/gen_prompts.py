"""치즈 퐁듀 쥐 초필살기 v2 = '트레비 치즈 분수' 프롬프트 (2026-10-20 사용자: 인게임 그림체 유지 · 메이플 아델 오리진식 카메라)
컷: ① 익스트림 클로즈업 아래 → 위 틸트 ② 손 클로즈업 핑거 스냅 ③ 얼굴 클로즈업 윙크 → 선글라스 ④ 급격한 트래킹 줌 아웃 (파노라마)
    ⑤ 트레비 분수 전체 샷 1초 프레임 스탑 ⑥ 분수 안 치즈 폭발 + 주변 크롭 마스크 블러 ⑦ 치즈가 화면을 덮음 → 게임 화면 치즈 비
그림체 = 다른 v2 컷신과 같은 플랫 (ref_flat_bg = 로비 배경 · ref_flat_anim = 파라오 셀 프레임 · ref_rig = 게임 파츠). 글자 없음.
클로즈업 연속 프레임(cu_snap2·3 · cu_face2~4)은 1번 프레임을 참고로 넣어 '같은 구도에서 손 · 표정만' 편집 → 프레임끼리 겹쳐도 맞음.
→ UnityResources/Rats/SuperUlt/fondue/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'fondue', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(PH, 'anim_command.png'), 'ref_flat_anim2.png'),
                 (os.path.join(R, 'SuperUlt', 'streamrat', 'Sheets', 'face_close.png'), 'ref_flat_face.png'),
                 (os.path.join(R, 'SuperUlt', 'fondue', 'Sheets', 'ref_char.png'), 'ref_char.png')):
    shutil.copy(src, os.path.join(D, dst))
ims = [Image.open(os.path.join(R, 'Parts', 'fondue', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same character size and position, the same colors and flat style, the same solid magenta background. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = "warm sunset Rome — soft peach and apricot sky, warm cream travertine stone, turquoise fountain water, a few terracotta and ochre building walls, golden cheese-yellow accents"
BG = ("- {size}, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO people, NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: " + PAL + ".\n")
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the character's in-game sprite parts: copy its colors, shapes and proportions) and the approved flat cutscene art ref_flat_anim / ref_flat_face: "
         "FLAT cartoon storybook look in the Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, small simple black bead eyes with one tiny white highlight, "
         "no black outlines, no gloss, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo.\n- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- CHARACTER "Cheese Fondue Rat": build it EXACTLY from the attached ref_rig and ref_char (use ref_char only for the colors and the character design, NOT its painted look): '
        'a cute chubby GREY RAT with big round pink ears, a pink nose, long white whiskers, small black bead eyes, a blob of melted golden-yellow CHEESE on top of its head dripping down a little, '
        'wearing a TEAL HAWAIIAN SHIRT with big pink hibiscus flowers and small yellow and green leaves, open collar; small grey paws with 4 short pink-tipped fingers; a long pink tail. Big head, small body.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery, no effects. Do not use magenta or purple on the subject. Keep the subject's edges clean.\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_flat_face.png']
P = {}
# ⑤ 트레비 분수 전체 샷 (가로 와이드, 카메라가 얼굴에서 확 물러나 이 그림 전체가 보임) — 가운데 위 바위 꼭대기에 쥐가 섬 (쥐는 따로)
P['pano_on'] = (BG.format(size='1536x1024 LANDSCAPE') +
                "- A grand WIDE establishing shot of a baroque Roman palace FOUNTAIN at sunset, seen straight from the FRONT and slightly from below, perfectly SYMMETRIC, like the famous Trevi Fountain: "
                "a tall cream stone palace facade with columns and a big central arched NICHE, wide rocky cascades of stone below it spilling turquoise water into a huge wide semicircular BASIN of turquoise water at the bottom, "
                "a low stone rim around the basin, warm terracotta buildings at the far left and right edges, a peach sunset sky above. "
                "IMPORTANT: inside the central niche, on top of the central rocks, there is NO statue — an EMPTY flat rock pedestal (a character will stand there later), the pedestal top is at about 45% of the image height, centered. "
                "The basin water fills the lower-middle third of the picture. Keep it simple and readable, big flat shapes.\n", ['ref_flat_bg.png'])
# ①②③ 클로즈업 뒤 배경판 (치즈색 방사형 플랫)
P['face_bg'] = (BG.format(size='1536x1024 LANDSCAPE') +
                "- A FLAT cut-in backdrop for a dramatic close-up: big flat radial RAYS and swirling ribbon bands of melted CHEESE in golden yellow, warm orange and cream, with a few turquoise water-colored bands, "
                "radiating from the center, a calm plain middle area where a face will be placed on top. Flat shapes only.\n", ['ref_flat_bg.png'])
# ① 익스트림 클로즈업: 아래 → 위 틸트용 세로 그림 (배경까지 한 장)
P['body_up'] = ("- 1024x1536 PORTRAIT, one full-frame picture (no transparency).\n" + STYLE + CHAR +
                "- EXTREME CLOSE-UP, dramatic LOW ANGLE (the camera is very close and looks UP at the character): the character's upper body fills the whole frame from the bottom edge to the top edge: "
                "the bottom of the frame is the big Hawaiian SHIRT (belly and the open collar, big pink hibiscus flowers), the middle is the chin and the furry neck seen from below, "
                "the TOP of the frame is the face seen from below (chin, nose and whiskers, small confident smile, eyes looking down at the viewer coolly) and the cheese blob on the head with drips. "
                "Behind it: a flat peach sunset sky with a cream stone baroque palace cornice and column tops seen from below at the top edges. Very confident, cool 'boss' mood.\n",
                ['ref_flat_bg.png'] + CREFS)
# ② 손 클로즈업 핑거 스냅 (3 프레임: 1 = 원본, 2·3 = 편집)
P['cu_snap1'] = ("- 1536x1024 LANDSCAPE extreme CLOSE-UP of ONE paw of the character, filling most of the frame, seen from the side-front, the wrist and the teal Hawaiian shirt sleeve cuff entering from the lower-right corner. "
                 "Like a dramatic anime cut-in of a hand. The paw is raised and READY TO SNAP its fingers: the thumb pressed firmly against the middle finger, the other fingers curled, tense. Only the paw and sleeve, no face.\n"
                 + STYLE + CHAR + MAG, CREFS)
P['cu_snap2'] = (EDIT.format(src='cu_snap1.png', f='{f}') +
                 "- The FINGER SNAP happens: the middle finger has just flicked DOWN hard onto the palm and the thumb slid forward past it. A few short flat motion lines next to the moving finger. Nothing else changes.\n", ['cu_snap1.png'])
P['cu_snap3'] = (EDIT.format(src='cu_snap1.png', f='{f}') +
                 "- AFTER the finger snap: the paw relaxed and slightly open, the index finger pointing forward at the viewer like 'you're next', thumb up, cool. Nothing else changes.\n", ['cu_snap1.png'])
# ③ 얼굴 클로즈업: 윙크 → 선글라스 (4 프레임: 1 = 원본, 2~4 = 편집)
P['cu_face1'] = ("- 1536x1024 LANDSCAPE extreme CLOSE-UP of the character's FACE straight from the FRONT, the face filling most of the height, centered, the top of the shirt collar visible at the bottom edge. Keep the simple bead eyes.\n"
                 + STYLE + CHAR + MAG + "- Expression: a cool confident SMIRK, both eyes open, eyebrows slightly lowered, the cheese blob on the head with one drip.\n", CREFS)
P['cu_face2'] = (EDIT.format(src='cu_face1.png', f='{f}') +
                 "- The character WINKS at the viewer: the left eye (viewer's left) closed in a happy curved line, the right eye open, the smirk turns into a cheeky grin with a tiny sparkle near the closed eye. Nothing else changes.\n", ['cu_face1.png'])
P['cu_face3'] = (EDIT.format(src='cu_face1.png', f='{f}') +
                 "- The character is PUTTING ON black SUNGLASSES: one paw enters from the bottom-right and holds a pair of sleek black wayfarer-style sunglasses by the frame, the sunglasses are half way on, still a bit LOW on the snout below the eyes, tilted slightly; "
                 "both eyes open looking at the viewer, cool grin. Nothing else changes.\n", ['cu_face1.png'])
P['cu_face4'] = (EDIT.format(src='cu_face1.png', f='{f}') +
                 "- The character now WEARS the sleek black wayfarer-style SUNGLASSES properly over both eyes (flat black lenses with one simple white diagonal shine stripe each), no paw visible, "
                 "a big cool confident grin showing a little tooth, chin slightly raised. Nothing else changes.\n", ['cu_face1.png'])
# ④⑤ 분수 꼭대기에 선 쥐 (선글라스, 전신 정면) — 트래킹 줌 아웃의 시작 = 이 쥐 얼굴
P['pose_top'] = ("- 1024x1536 PORTRAIT, the character FULL BODY, FRONT VIEW, standing upright like a person, centered, filling about 85% of the height.\n" + STYLE + CHAR + MAG +
                 "- Wearing sleek black wayfarer-style SUNGLASSES. A triumphant showman pose: both arms spread wide open to the sides with open paws (like presenting a grand show), chest out, cool grin, the long pink tail curling behind, feet together.\n", CREFS)
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, purple or pink on the elements.\n- NO text, NO letters, NO numbers.\n")
CHEESE = "- Palette: golden cheese yellow, warm orange shade, cream highlight, a little turquoise water blue and white.\n"
P['fx_flat'] = (FXS + CHEESE +
                "  1. a flat four-pointed SNAP SPARKLE star in cream white and yellow (the 'click' of a finger snap)\n"
                "  2. a flat thin SHOCK RING, a perfect circle made of a yellow and cream band\n"
                "  3. a flat round CHEESE SPLASH BURST: a golden yellow blob with thick round droplet spikes all around\n"
                "  4. a flat cluster of 7 round melted CHEESE DROPLETS of different sizes\n"
                "  5. a flat cluster of turquoise and white WATER SPRAY droplets\n"
                "  6. a flat vertical SPEED LINE streak in cream white (long thin tapered stripe)\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (FXS + CHEESE +
               "  1. ONE falling melted CHEESE DROP: a big teardrop of golden yellow cheese, vertical, the tip pointing UP with a short drip tail\n"
               "  2. ONE chunky CHEESE WEDGE with round holes, tumbling\n"
               "  3. a wide wavy WALL of melted cheese seen from the front: a horizontal band of golden yellow cheese with big round drips hanging from its BOTTOM edge (it will slide down to cover the screen)\n"
               "  4. CHEESE SPLAT frame 1: a small flat yellow splash just hitting the ground, a few drops jumping up\n"
               "  5. CHEESE SPLAT frame 2: a wide flat yellow crown splash with drops flying out\n"
               "  6. CHEESE SPLAT frame 3: the end: a flat golden cheese puddle on the ground with a few small drops around\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['geyser'] = ("- 1536x1024 image, 3 columns side by side: ONE element per column, each the same scale, each standing on the SAME bottom line (the bottom edge of the frame), none touching each other.\n"
               "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2: simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO painterly texture, NO black outlines.\n"
               "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, purple or pink on the elements.\n- NO text, NO letters, NO numbers.\n" + CHEESE +
               "- Three consecutive animation frames of a huge melted-CHEESE EXPLOSION bursting UP out of fountain water (the base of each is a wide splash of turquoise water and cheese):\n"
               "  1) the start: a short wide dome of golden cheese erupting from the water with a ring of water splash\n"
               "  2) the peak: a TALL thick column of golden cheese shooting up to the top of the frame and bursting outward at the top into big round cheese blobs, water and cheese spraying sideways at the base\n"
               "  3) the fall: the column breaking apart into many big round cheese blobs and ribbons raining down, a wide splash at the base\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
for n, (b, refs) in P.items():
    head = HEAD.format(f=n + '.png') if not b.startswith('Use your built-in') else ''
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(head + b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', encoding='utf-8', newline='\n').write('''#!/usr/bin/env bash
# 치즈 퐁듀 쥐 초필살기 v2 그림 (트레비 치즈 분수) Codex — 1차: 원본 프레임 · 배경 · 이펙트 / 2차: 클로즈업 연속 프레임 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/fondue/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
snap() { run cu_snap1; run cu_snap2 & run cu_snap3 & wait; }
face() { run cu_face1; run cu_face2 & run cu_face3 & run cu_face4 & wait; }
snap & face &
for n in pano_on face_bg body_up pose_top; do run $n & done; wait
for n in fx_flat fx_hit geyser; do run $n & done; wait; echo DONE
''')
print(sorted(P))
