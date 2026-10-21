"""외계인 생쥐 초필살기 v3 = '모선 강림 (인디펜던스 데이 패러디)' 프롬프트 (2026-10-21 사용자 3차)
컷: ① 건물(화면 1/3) 뒤에 숨어 눈치 보는 스파이 복장 외계인 쥐 · 뒤로 살짝 흐린 떠드는 쥐들 ② 버튼 누르는 손 클로즈업 ③ 우주: 지구에서 신호 → 지구로 다가오는 우주선
    ④ 구름을 뚫고 내려오는 거대 우주선 롱샷 (카메라도 천천히 같이 내려옴) ⑤ 프레임 스탑 → 레이저 · 흔들림 · 크롭 블러 (도시 일부만 부서짐)
    ⑥ 로우 앵글 · 뒤에서 본 전투기 발진 (배경 · 전투기 따로, 소실점) ⑦ 레이저 컷: 점 같은 전투기가 주포 구멍으로 → 레이저가 멈추고 우주선 멈칫 ⑧ 우주선 폭발 + 오른쪽 아래 우는 외계인 쥐 (수어 통역 창처럼)
그림체 = 다른 v2 컷신과 같은 플랫 (ref_flat_bg = 로비 배경 · ref_flat_anim = 파라오 셀 프레임 · ref_flat_face = 스트리머 클로즈업 · ref_rig = 게임 파츠). 글자 없음.
연속 프레임(cu_hand2 · ship2 · jet_back2 · shot_descend_dmg)은 1번을 '편집' · ship_far 는 ship 을 참고.
→ UnityResources/Rats/SuperUlt/alien/Sheets/v3"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'alien', 'Sheets', 'v3')
V2 = os.path.join(R, 'SuperUlt', 'alien', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
for n in ('ref_flat_bg', 'ref_flat_fx', 'ref_flat_fx2', 'ref_flat_anim', 'ref_flat_anim2', 'ref_flat_face', 'ref_char', 'ref_rig'):
    shutil.copy(os.path.join(V2, n + '.png'), os.path.join(D, n + '.png'))
shutil.copy(os.path.join(V2, 'pano_c.png'), os.path.join(D, 'ref_city.png'))          # 쥐 도시 모습 (v2 광장)
ims = [Image.open(os.path.join(R, 'Parts', 'mouse', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_mouse.png'))                                                # 평범한 쥐 (떠드는 쥐들)

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same sizes and positions, the same colors and flat style{bgkeep}. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = "night — deep navy and indigo sky, soft violet clouds, teal and slate-blue buildings, warm butter-yellow lit windows and cheese-yellow accents, small touches of alien lime green"
BG = ("- {size}, one full-frame background picture (no transparency, no panels, no borders). {who}NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: " + PAL + ".\n")
NOONE = "NO characters, NO animals, NO people. "
CITY = ("a cute MOUSE CITY like the attached ref_city: round-topped houses with little round mouse-ear domes on the roofs, tall yellow CHEESE-WEDGE shaped towers with round holes as windows, "
        "arched round mouse-hole doors, string lights between roofs, simple flat shapes")
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the character's in-game sprite parts: copy its colors, shapes and proportions) and the approved flat cutscene art ref_flat_anim / ref_flat_face: "
         "FLAT cartoon storybook look in the Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, "
         "no black outlines, no gloss, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo.\n- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
SPY = ('- CHARACTER "Alien Mouse" in SPY DISGUISE: build the mouse EXACTLY from the attached ref_rig and ref_char (colors and design only): a cute PALE LIME-GREEN MOUSE with big round pink-lined ears, '
       'two thin green ANTENNAE ending in small round PINK bulbs that poke up through two holes in the hat, big shiny round BLACK eyes, a small pink nose, a cream-white muzzle, a long thin pink tail, small pink paws. '
       'DISGUISE: a beige belted TRENCH COAT with the collar turned up, a brown FEDORA hat pulled low, small round black SUNGLASSES, and a chunky black spy WRISTWATCH gadget with a red button on the left wrist. Big head, small round body.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta, pink-purple or violet on the subjects except the pink ears, nose, paws and antenna bulbs. Keep everything fully inside the frame with margin.\n"
MAGO = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta, pink or purple on the object. Keep the object fully inside the frame with a small margin.\n"
FLATO = "- STYLE: FLAT 2D game art like the attached ref_flat_bg and ref_flat_fx (simple flat shapes, flat fills with at most one soft shade step, NO black outlines, NO gloss, NO glow, NOT 3D, NOT realistic). NO text, NO letters, NO numbers, no logos.\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_flat_face.png']
SHEET = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE frame per column, all frames the same size and scale, each well inside its third, none touching. "
         "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose and expression change.\n")
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers, NO stars.\n")
SHIP = ("a COLOSSAL alien CITY-DESTROYER mothership in the spirit of the movie Independence Day but cute flat cartoon: a gigantic flat round DISC, charcoal grey and dark steel-teal hull, "
        "the underside covered with concentric rings of flat panels, ridges and trenches, many tiny lime-green and cream lights in rings, and in the exact CENTER of the underside a round downward-facing main CANNON port (a dark recessed hole with a lime-green rim)")
P = {}
# ① 스파이 컷: 떠드는 쥐들 (배경, 나중에 흐리게) · 앞 건물 (화면 1/3) · 엿보는 외계인 쥐 셀 3프레임
P['bg_chat'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE', who='') +
                "- A cozy night street corner in " + CITY + ", seen at eye level. In the MIDDLE and RIGHT two thirds: five or six ordinary cute MICE (built like the attached ref_mouse: grey-brown, big round ears, pink tails, simple clothes) "
                "standing in two small groups CHATTING and laughing happily, one holding a cup of tea, one gesturing with paws, under a warm street lamp and a cafe awning with string lights. "
                "The LEFT third of the picture is a plain dark shaded sidewalk area (a foreground wall will cover it). The mice are small, about one quarter of the image height.\n", ['ref_flat_bg.png', 'ref_city.png', 'ref_mouse.png', 'ref_flat_anim.png'])
P['fg_wall'] = (HEAD + "- 1024x1536 PORTRAIT, ONE object filling the FULL HEIGHT: the CORNER of a building seen straight from the front at close range, a tall flat plaster-and-brick WALL in dark slate blue and teal (in shadow) "
                "with a vertical drainpipe, one small dark window and a few flat brick shapes; its RIGHT edge is a straight vertical CORNER EDGE running from the top to the bottom of the image; the wall occupies the LEFT 75% of the image. "
                "It is a FOREGROUND element, slightly darker than the background.\n" + FLATO + MAGO, ['ref_flat_bg.png', 'ref_city.png'])
P['anim_spy'] = (HEAD + SHEET + "- CAMERA: every frame is a three-quarter view with the character facing the viewer's RIGHT, UPPER BODY only (from the waist up), leaning to the right as if peeking out from behind a wall corner on its left.\n" + STYLE + SPY + MAG +
                 "  1) cautiously PEEKING: leaning out, one paw gripping the (invisible) wall edge at the left, eyes sliding sideways behind the sunglasses, mouth a tiny worried line, antennae low;\n"
                 "  2) SUSPICIOUS: pulling the sunglasses down the nose with one finger, the big black eyes peering over them to the right, one eyebrow raised;\n"
                 "  3) SLY: a sneaky smirk showing one tooth, lifting the left wrist with the spy WRISTWATCH up near the mouth, antennae perked up.\n", CREFS + ['ref_flat_anim2.png'])
# ② 버튼 누르는 손 클로즈업
P['bg_hand'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE', who=NOONE) +
                "- A FLAT cut-in backdrop for a dramatic close-up: dark navy with big flat diagonal speed bands in slate blue and a few lime-green accents radiating from the center, calm plain middle area. Flat shapes only.\n", ['ref_flat_bg.png'])
P['cu_hand1'] = (HEAD + "- 1536x1024 LANDSCAPE extreme CLOSE-UP: the character's LEFT WRIST with a chunky black spy WRISTWATCH gadget (a round lime-green screen showing a simple flat antenna shape, a big round RED BUTTON on the side), "
                 "the beige trench coat sleeve cuff, the small pink paw; the RIGHT paw's index finger is hovering just above the red button, about to press it. Big and centered, filling most of the frame.\n" + STYLE + SPY + MAG, CREFS)
P['cu_hand2'] = (EDIT.format(src='cu_hand1.png', f='{f}', bgkeep=', the same solid magenta background') +
                 "- The finger PRESSES the red button hard (button pushed in, three short flat impact lines), the watch screen now shines bright LIME GREEN with two flat lime signal arcs coming out of it. Nothing else changes.\n", ['cu_hand1.png'])
# ③ 우주: 지구에서 신호 → 지구로 다가오는 우주선
P['bg_space'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE', who=NOONE) +
                 "- OUTER SPACE: flat deep navy-black space with simple round star dots, a big round flat EARTH in the LOWER-LEFT (about 55% of the image height, partly cut by the bottom-left edge) with flat blue oceans, "
                 "simple green-brown continents and white flat cloud swirls, a thin pale-blue atmosphere rim; a small grey MOON at the upper-left; the UPPER-RIGHT area is open dark space.\n", ['ref_flat_bg.png'])
P['ship'] = (HEAD + "- 1536x1024 LANDSCAPE, ONE object: " + SHIP + ", seen from DIRECTLY BELOW at a steep low angle (we look straight up at its underside, a slightly squashed perfect circle in perspective), "
             "filling almost the full WIDTH of the image (about 94%). Overwhelming, menacing, massive. The cannon is currently dark.\n" + FLATO + MAGO, ['ref_flat_bg.png', 'ref_flat_fx.png'])
P['ship2'] = (EDIT.format(src='ship.png', f='{f}', bgkeep=', the same solid magenta background') +
              "- CHARGING: the central cannon port now glows bright LIME GREEN with a cream-white core, flat lime-green energy arcs around it, the rings of lights all lime green, glowing lime lines along the panel trenches toward the center. Nothing else changes.\n", ['ship.png'])
P['ship_far'] = (HEAD + "- 1536x1024 LANDSCAPE, ONE object: the SAME mothership as the attached ship.png (same design and colors), but seen from the SIDE and slightly ABOVE flying through space toward the lower-left: "
                 "a wide flat disc with a low dome on top, rings of tiny lights on the rim, a short flat lime-green engine glow trail behind it at the upper-right. The disc is about 70% of the image width.\n" + FLATO + MAGO, ['ship.png', 'ref_flat_fx.png'])
# ④⑤⑦ 롱샷: 구름을 뚫고 내려오는 우주선 (우주선은 따로) · 레이저 뒤 일부만 부서진 도시
P['shot_descend'] = (HEAD + BG.format(size='1024x1536 PORTRAIT', who=NOONE) +
                     "- ONE tall vertical LONG SHOT that a camera slowly tilts DOWN through:\n"
                     "  TOP 45%: a churning, ominous wall of huge flat CLOUDS boiling outward from the center, their edges lit in fiery orange and lime green from inside (like the clouds in the movie Independence Day), "
                     "the CENTER of the cloud mass open as a dark hole (a giant ship will come down through it);\n"
                     "  MIDDLE: open dark night sky;\n"
                     "  BOTTOM 30%: a LONG SHOT of " + CITY + " seen from far away at ground level: a wide skyline across the full width with lit windows, the tallest cheese tower in the CENTER.\n", ['ref_flat_bg.png', 'ref_city.png'])
P['shot_descend_dmg'] = (EDIT.format(src='shot_descend.png', f='{f}', bgkeep='') +
                         "- Only the CENTER of the city skyline was hit by a giant laser: the tall cheese tower in the center and two or three buildings beside it are broken and burning (flat orange flames, a glowing lime-green crater glow at their base, "
                         "a thick flat charcoal smoke column rising from the center). The REST of the city on the left and right stays INTACT with lit windows. Nothing else changes. NO characters.\n", ['shot_descend.png'])
# ⑥ 전투기 발진 (로우 앵글 · 뒤에서 · 소실점) — 배경 · 전투기 따로
P['bg_runway'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE', who=NOONE) +
                  "- A LOW-ANGLE view from BEHIND, close to the ground, looking down a long AIRBASE RUNWAY at night with strong one-point PERSPECTIVE: the runway's edges, its dashed center line and two rows of small round runway lights "
                  "all CONVERGE to ONE VANISHING POINT at the exact CENTER of the picture on the horizon (horizon at 50% height); dark hangars and a control tower on both sides also following the perspective; "
                  "far away on the horizon at the vanishing point a tiny silhouette of the mouse city with a thin lime-green laser beam coming down from the sky. The lower center is plain runway (a jet will be placed there).\n", ['ref_flat_bg.png', 'ref_city.png'])
P['jet_back1'] = (HEAD + "- 1536x1024 LANDSCAPE, ONE object: a sleek cartoon FIGHTER JET seen from DIRECTLY BEHIND and slightly BELOW (low angle, symmetric), very big and heroic: two round engine NOZZLES at the bottom center (dark, engines off), "
                  "wide swept wings spread to the left and right, two slanted tail fins, the bubble canopy at the top with the small round helmet of a mouse pilot visible from behind (two round mouse ears on the helmet), "
                  "slate grey and steel blue body with yellow cheese-wedge emblems on the wings (no text). It fills about 85% of the image width.\n" + FLATO + MAGO, ['ref_flat_bg.png', 'ref_flat_fx.png'])
P['jet_back2'] = (EDIT.format(src='jet_back1.png', f='{f}', bgkeep=', the same solid magenta background') +
                  "- AFTERBURNERS ON: both nozzles now blast big flat round flames toward the viewer (cream-white core, orange and blue rings), short flat heat-shimmer lines, the jet slightly lower and leaning forward ready to launch. Nothing else changes.\n", ['jet_back1.png'])
# ⑧ 우는 외계인 쥐 (수어 통역 창)
P['anim_cry'] = (HEAD + SHEET + "- CAMERA: every frame is a straight FRONT VIEW, UPPER BODY (from the chest up), centered.\n" + STYLE + SPY + MAG +
                 "  1) SHOCKED: jaw dropped, eyes huge, the sunglasses slipping off and hanging crooked, antennae straight up;\n"
                 "  2) BAWLING: eyes squeezed shut, mouth wide open crying loudly, two big flat streams of TEARS gushing from the eyes, the hat tilted, antennae drooping;\n"
                 "  3) SOBBING: blowing the nose into a white handkerchief, tears dripping, shoulders hunched, antennae drooping.\n", CREFS)
# 이펙트
P['fx_v3'] = (HEAD + FXS + "- Palette: alien LIME GREEN and mint, cream white, steel grey and charcoal for metal, warm orange for fire, pale sky blue for water.\n"
              "  1. a SIGNAL wave: three flat concentric lime-green ARCS (like a wifi or radio signal symbol) opening UPWARD\n"
              "  2. a TINY fighter jet seen from below as a small simple grey dart shape with a long thin white contrail behind it (pointing UP)\n"
              "  3. a flat AFTERBURNER flame seen from behind: a round cream-white core with an orange ring and a blue outer ring\n"
              "  4. a jagged steel-grey METAL SHARD of the mothership hull with a lime-green light strip on it\n"
              "  5. a bent charcoal hull PANEL chunk with small orange flames on one edge\n"
              "  6. a big flat pale-blue TEAR DROP\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v3.sh'), 'w', encoding='utf-8', newline='\n').write('''#!/usr/bin/env bash
# 외계인 생쥐 초필살기 v3 그림 (인디펜던스 데이 패러디) Codex — 원본 → 편집 순서
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/alien/Sheets/v3; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
ship() { run ship; run ship2 & run ship_far & wait; }
hand() { run cu_hand1; run cu_hand2; }
desc() { run shot_descend; run shot_descend_dmg; }
jet() { run jet_back1; run jet_back2; }
ship & hand & desc & jet &
for n in bg_chat fg_wall anim_spy; do run $n & done; wait
for n in bg_hand bg_space bg_runway anim_cry fx_v3; do run $n & done; wait; echo DONE
''')
print(sorted(P))
