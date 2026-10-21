"""쥐랜드 관광쥐 초필살기 v2 = '풍선 펑' 프롬프트 (2026-10-21 사용자 콘티)
컷: ① 축제 퍼레이드 몽타주 (즐기는 쥐들) ② 풍선을 들고 고민 → 유레카! ③ 에어 펌프로 풍선을 불기 시작 ④ 박수 치는 쥐들 (사선 하이 앵글)
    ⑤ 펌프를 누르는 손 클로즈업 (점점 빨라짐) ⑥ 같은 구도: 박수 치다 당황 ⑦ 점점 커지는 풍선 + 급격한 트래킹 줌 아웃 ⑧ 엄청 커짐 → 프레임 스탑 → 펑
    ⑨ 풍압으로 건물이 다 날아감 (핵폭발 영화 연출식 풍압 컷 여러 개 · 구경하던 쥐들도 날아감) ⑩ 머쓱하게 머리 긁적 → 데헷
그림체 = 다른 v2 컷신과 같은 플랫 (ref_flat_bg = 로비 배경 · ref_flat_anim = 파라오 셀 프레임 · ref_flat_face = 스트리머 클로즈업 · ref_rig = 게임 파츠). 글자 없음.
컷마다 배경까지 그린 한 장 shot_* (연속 프레임은 1번을 '편집' = 같은 틀) · 광장 판 pano_on (쥐 · 풍선 없음) → 롱샷 wide_on (아웃페인팅) · 폐허 pano_alt (편집)
→ UnityResources/Rats/SuperUlt/parkrat/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'parkrat', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(PH, 'anim_command.png'), 'ref_flat_anim2.png'),
                 (os.path.join(R, 'SuperUlt', 'streamrat', 'Sheets', 'face_close.png'), 'ref_flat_face.png'),
                 (os.path.join(R, 'SuperUlt', 'parkrat', 'Sheets', '_v1_painted', 'ref_char.png'), 'ref_char.png')):
    shutil.copy(src, os.path.join(D, dst))
ims = [Image.open(os.path.join(R, 'Parts', 'parkrat', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same sizes and positions, the same colors and flat style{bgkeep}. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = ("a sunny theme-park afternoon — clear soft sky blue, pastel pink and lilac castle towers with cheese-yellow cone roofs, mint and coral shop fronts, butter-yellow and white bunting, "
       "warm cream paving, a few red-and-white stripes")
PARK = ("RATLAND, a cute theme park built by mice: a pastel pink and lilac fairy-tale CASTLE with round towers and yellow cone roofs topped with little round MOUSE-EAR shapes, "
        "candy-striped souvenir shops and popcorn stands with round mouse-ear awnings, a carousel, strings of bunting and round lanterns, balloon sellers, all in simple flat shapes")
BG = ("- {size}, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO people, NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: " + PAL + ".\n")
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the character's in-game sprite parts: copy its colors, shapes and proportions) and the approved flat cutscene art ref_flat_anim / ref_flat_face, "
         "and (for any background) the approved flat background ref_flat_bg: FLAT cartoon storybook look in the Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, "
         "no black outlines, no gloss, NO painterly brush strokes, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo. Palette: " + PAL + ".\n"
         "- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- CHARACTER "Ratland Tourist": build it EXACTLY from the attached ref_rig and ref_char (use ref_char only for the colors and the character design, NOT its painted look): '
        'a cute soft GREY mouse with a cream-white muzzle and belly, big round PINK-lined ears, a pink nose, white whiskers, small black bead eyes, '
        'wearing a black souvenir HEADBAND with two big round black MOUSE EARS on top, red HEART-SHAPED SUNGLASSES pushed up on the headband (not over the eyes), '
        'a blue-and-white horizontally STRIPED T-shirt and a red-pink FANNY PACK across the chest, small pink paws, a long thin pink tail. Big head, small round body.\n')
CROWD = ("- The OTHER MICE (park visitors): many cute flat cartoon mice in the same style but simpler, in assorted fur colors (grey, brown, cream, white, black), "
         "most wearing souvenir mouse-ear headbands, holding balloons, churros, popcorn and cotton candy. They are NOT the main character (no heart sunglasses, no striped shirt).\n")
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta, pink-purple or violet on the subjects except the pink ears, nose and paws. Keep everything fully inside the frame with margin.\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_flat_face.png']
SREFS = CREFS + ['ref_flat_bg.png']
SHOT = "- 1536x1024 LANDSCAPE, one full-frame picture WITH background (no transparency, no panels, no borders).\n"
SHEET = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
         "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses.\n")
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers.\n")
BALLOON = "a big glossy-free flat CORAL-RED rubber balloon shaped like a MOUSE HEAD (a round balloon with two smaller round ears on top, a Ratland souvenir balloon)"
WIND = ("- The BLAST WIND comes from the LEFT (a giant balloon just burst off-screen at the left): everything is pushed hard to the RIGHT, flat white-cream WIND streaks and swirls "
        "sweep from left to right across the whole picture, dust clouds in warm cream and grey roll in from the left.\n")
P = {}
# ① 축제 퍼레이드 몽타주 (컷마다 한 장)
P['shot_parade1'] = (HEAD + SHOT + STYLE + CROWD +
                     "- SCENE, a festive PARADE in " + PARK + " (the castle in the far back). Eye-level side view: a huge colorful PARADE FLOAT shaped like a giant wedge of cheese with a little castle on top rolls from left to right, "
                     "mice in sparkly costumes dance and wave on the float, a mouse brass band marches in front with drums and trumpets, flat confetti and streamers fly in the air, "
                     "happy cheering visitor mice line the bottom edge in the foreground (seen from behind and from the side, waving their paws). Joyful, busy, bright.\n", SREFS)
P['shot_parade2'] = (HEAD + SHOT + STYLE + CROWD +
                     "- SCENE, a close MEDIUM SHOT of a group of very happy visitor mice in " + PARK + " (soft flat background: shops, bunting, the castle far behind): "
                     "a little mouse on its parent's shoulders laughing and holding a bunch of balloons, a couple of mice sharing a big pink cotton candy, a mouse biting a churro, eyes closed in delight, "
                     "everyone wearing mouse-ear headbands, flat confetti falling. Warm, joyful.\n", SREFS)
P['shot_parade3'] = (HEAD + SHOT + STYLE + CROWD +
                     "- SCENE, a wide shot of " + PARK + ": a bright CAROUSEL with cheese-wedge and teacup seats spinning in the middle, mice riding it laughing with paws up, "
                     "the pink castle behind it, a mouse mascot in a big round costume waving to children, balloons rising into the sky, strings of bunting across the top. Festive, bright, full of life.\n", SREFS)
# ② 풍선을 들고 고민 → 유레카 (편집)
P['shot_think1'] = (HEAD + SHOT + STYLE + CHAR +
                    "- SCENE, a MEDIUM SHOT (knees up) of the Ratland Tourist standing in the middle of the park plaza, facing the viewer in three-quarter view, the pink castle and the parade far behind (simple flat, a bit soft). "
                    "In one paw it holds up a small, limp, UN-INFLATED coral-red balloon (a deflated rubber skin with a knot, droopy), looking at it with a puzzled, thinking face: head tilted, eyes looking up, mouth pursed to one side, "
                    "the other paw on its chin. The character fills about 75% of the image height, centered slightly left. Plenty of plain sky at the top right.\n", SREFS)
P['shot_think2'] = (EDIT.format(src='shot_think1.png', f='{f}', bgkeep=', the same background') +
                    "- EUREKA! The Ratland Tourist suddenly gets a brilliant idea: eyes wide open and SPARKLING (two small white star-shaped highlights in the eyes), a huge open-mouth grin, "
                    "the paw that was on the chin now points one finger straight UP in the air, ears perked up, whiskers springing out, the limp balloon still in the other paw. Nothing else changes.\n", ['shot_think1.png'])
# 광장 판 (쥐 · 풍선 없음, 풍선이 커질 자리) → 롱샷 아웃페인팅 · 폐허 편집
P['pano_on'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE') +
                "- The central PLAZA of " + PARK + ", seen straight from the front at ground level: the wide pastel pink and lilac CASTLE in the back center (its base at about 55% of the image height), "
                "candy-striped souvenir shops and a popcorn stand on the left and right sides, round lanterns and bunting strung across, a wide EMPTY cream paved plaza in the lower 40% (a character will stand in the lower center), "
                "plenty of open sky. The center of the plaza is completely empty.\n", ['ref_flat_bg.png'])
P['wide_on'] = (EDIT.format(src='ref_wide_seed.png', f='{f}', bgkeep='').split('Edit spec:')[0] +
                "Edit spec (OUTPAINTING):\n- The small picture in the middle of ref_wide_seed.png is the central plaza of a cute theme park built by mice. Keep that middle picture EXACTLY where it is, at the same size, with the same composition and colors. "
                "Replace ALL the solid magenta area around it by extending the scene outward, so the result is one seamless EXTREME LONG SHOT (very wide establishing shot) of the WHOLE theme park, seen from the front and slightly above.\n"
                "- Around the plaza: the rest of " + PARK + ": more castle towers, a big Ferris wheel on the left, a roller coaster track on the right, rows of candy-striped shops and rooftops, paths with flower beds, trees, "
                "a wide soft sky blue sky with a few flat round clouds across the top, the park entrance gate and paving across the bottom.\n"
                "- NO people, NO characters, NO animals, NO text, NO letters, NO logos.\n"
                "- STYLE (MOST IMPORTANT): exactly the same FLAT 2D game background style as the middle picture and the attached ref_flat_bg: big simple flat color shapes, clean soft edges, at most one soft shade step, "
                "very little texture, NO black outlines, NO painterly brush strokes. Same palette. Result is 1536x1024 landscape, full frame, no borders, no magenta left anywhere.\n", ['ref_wide_seed.png', 'ref_flat_bg.png'])
P['pano_alt'] = (EDIT.format(src='pano_on.png', f='{f}', bgkeep='') +
                 "- Change: the park has been FLATTENED by a giant blast of wind from the center of the plaza: the castle towers are snapped and leaning, the cone roofs blown off, the shops are bare broken frames with their awnings torn away, "
                 "bunting and lantern strings snapped and dangling, piles of rubble, broken planks, popcorn and torn coral-red balloon rubber scraps scattered all over the plaza, a few thin flat dust clouds drifting, "
                 "the sky a little hazy and dusty cream. Still a cheerful flat cartoon (comic destruction, nothing scary), NO characters, NO text.\n", ['pano_on.png'])
# ③ 에어 펌프 (셀 프레임, 같은 틀) · 풍선 (따로)
P['anim_pump'] = (HEAD + SHEET + "- CAMERA: every frame is the same three-quarter view from the front, the character faces slightly to the RIGHT.\n" + STYLE + CHAR + MAG +
                  "- In every frame the Ratland Tourist stands behind a big cartoon FLOOR AIR PUMP (a tall mint-green cylinder pump with a T-shaped handle on top, a wide base under its feet, "
                  "and a short black HOSE from the base that curves to the RIGHT and ends in a small brass NOZZLE pointing straight UP, at knee height, just to the right of the pump). "
                  "NO balloon (the balloon is drawn separately). The pump base, the hose and the nozzle are at EXACTLY the same position and size in all three frames:\n"
                  "  1) both paws gripping the T-handle pulled all the way UP, standing tall, excited grin, eyes sparkling;\n"
                  "  2) pushing the handle HALFWAY down, leaning over it, determined;\n"
                  "  3) the handle pushed all the way DOWN, knees bent, the whole body pressing down with effort, eyes squeezed shut, cheeks puffed.\n", CREFS + ['ref_flat_anim2.png'])
P['props_flat'] = (HEAD + FXS.replace('effect art', 'prop art') +
                   "  1. " + BALLOON + ", fully inflated, perfectly round and plump, with a small tied KNOT at the very bottom center, seen from the front (no string)\n"
                   "  2. the SAME mouse-head balloon OVER-INFLATED and about to burst: stretched very tight and thin, a lighter coral color, a few short flat white STRESS lines near the edges, the knot at the very bottom center\n"
                   "  3. a flat cartoon idea LIGHT BULB (warm yellow bulb with a grey screw base and short yellow rays around it)\n"
                   "  4. a small cartoon visitor MOUSE TUMBLING through the air, blown away: limbs spread wide, eyes as two swirls, mouth open, a souvenir mouse-ear headband flying off its head (brown fur)\n"
                   "  5. another small visitor MOUSE blown away upside down, clutching a churro, cream-white fur, mouth wide open screaming comically\n"
                   "  6. a black souvenir mouse-ear HEADBAND flying by itself\n", ['ref_flat_fx.png', 'ref_rig.png'])
# ④⑥ 박수 치는 쥐들 (사선 하이 앵글) → 손 벌림 (편집) · 당황 (편집, 같은 구도)
P['shot_crowd1'] = (HEAD + SHOT + STYLE + CROWD +
                    "- CAMERA: a HIGH-ANGLE shot looking DOWN on a crowd, with a strong DUTCH TILT (the whole picture tilted about 20 degrees, the horizon diagonal from lower left to upper right). "
                    "SCENE: a packed crowd of about ten visitor mice standing on the cream paved plaza, all looking UP toward the camera (toward something above), big happy smiles, eyes closed in joy, "
                    "all of them CLAPPING their paws together in front of their chests, mouse-ear headbands, a few balloons and churros. Flat paving and a bit of bunting at the edges. The faces are big and readable.\n", SREFS)
P['shot_crowd1b'] = (EDIT.format(src='shot_crowd1.png', f='{f}', bgkeep=', the same background') +
                     "- Mid-clap: every mouse now holds its paws APART (open wide, ready to clap again), still smiling happily. Nothing else changes.\n", ['shot_crowd1.png'])
P['shot_crowd2'] = (EDIT.format(src='shot_crowd1.png', f='{f}', bgkeep=', the same background') +
                    "- PANIC: every mouse STOPS clapping and stares UP in shock: eyes wide open as small round white dots with tiny pupils, mouths open in an O or a wavy worried line, big flat blue SWEAT DROPS on the heads, "
                    "paws frozen in the air or grabbing their cheeks, ears pinned back. A big flat soft CORAL-RED tint and shadow falls over the crowd from above (the giant balloon). Nothing else changes.\n", ['shot_crowd1.png'])
# ⑤ 펌프를 누르는 손 클로즈업 (편집)
P['shot_hand1'] = (HEAD + SHOT + STYLE +
                   "- An EXTREME CLOSE-UP of two small PINK mouse PAWS (with soft grey fur at the wrists and the blue-and-white striped shirt cuffs) gripping the T-shaped HANDLE of a mint-green floor air pump, "
                   "the handle pulled UP near the top of the image, the pump rod and the top of the cylinder below it. Background: a simple soft flat out-of-focus plaza (cream paving and pastel shapes). "
                   "Strong, dynamic, the paws fill the middle of the frame.\n", SREFS)
P['shot_hand2'] = (EDIT.format(src='shot_hand1.png', f='{f}', bgkeep=', the same background') +
                   "- The paws SLAM the handle all the way DOWN (the handle and paws now near the middle of the image, the rod hidden inside the cylinder), short flat white motion lines above the paws, "
                   "a small flat puff of air at the side of the pump. Nothing else changes.\n", ['shot_hand1.png'])
# ⑨ 풍압 컷 (핵폭발 영화식: 멀쩡 → 날아감, 편집)
P['shot_wave_a1'] = (HEAD + SHOT + STYLE +
                     "- A LOW-ANGLE wide shot of the pastel pink and lilac Ratland CASTLE (round towers, yellow cone roofs with little mouse-ear tips, flags on top) against the sky blue sky, "
                     "a calm peaceful moment. NO characters.\n", ['ref_flat_bg.png', 'ref_flat_anim.png'])
P['shot_wave_a2'] = (EDIT.format(src='shot_wave_a1.png', f='{f}', bgkeep='') + WIND +
                     "- The castle is being BLOWN APART: the cone roofs rip off and fly to the right, the towers bend and crack, bricks, flags and tiles fly away to the right, the sky turns hazy cream. Comic cartoon destruction, NO characters.\n", ['shot_wave_a1.png'])
P['shot_wave_b1'] = (HEAD + SHOT + STYLE + CROWD +
                     "- An eye-level shot down a cute SOUVENIR SHOP STREET of Ratland: candy-striped shop fronts with round mouse-ear awnings and hanging signs (no letters), a popcorn cart, a balloon seller's bunch of balloons, "
                     "lantern strings across, three or four visitor mice strolling happily. Calm and sunny.\n", SREFS)
P['shot_wave_b2'] = (EDIT.format(src='shot_wave_b1.png', f='{f}', bgkeep='') + WIND +
                     "- The street is being BLOWN AWAY: awnings rip off and fly to the right, shop fronts tear apart into flying planks, the popcorn cart tumbles through the air spilling popcorn, the balloons fly away, "
                     "the visitor mice are blown off their feet and fly to the right with comic shocked faces. Comic cartoon destruction, nothing scary.\n", ['shot_wave_b1.png'])
P['shot_wave_c1'] = (HEAD + SHOT + STYLE + CROWD +
                     "- A side view of a long ROW of visitor mice sitting and standing along the parade curb, watching happily, paws waving, mouse-ear headbands, a street lamp post in the middle, bunting above. Calm and sunny.\n", SREFS)
P['shot_wave_c2'] = (EDIT.format(src='shot_wave_c1.png', f='{f}', bgkeep='') + WIND +
                     "- EVERY mouse is BLOWN OFF its feet and flies away to the RIGHT: tumbling in the air, eyes as swirls, mouths open, headbands, churros and popcorn flying off; "
                     "one mouse clings to the bending lamp post with both paws, its body flapping horizontally in the wind like a flag. The bunting snaps. Comic cartoon, nothing scary.\n", ['shot_wave_c1.png'])
# ⑩ 머쓱 → 머리 긁적 → 데헷 (셀 프레임)
P['anim_end'] = (HEAD + SHEET + "- CAMERA: every frame is a straight FRONT VIEW (the character faces the viewer), FULL BODY.\n" + STYLE + CHAR + MAG +
                 "- In every frame the Ratland Tourist is a bit SOOTY after the blast: a few flat dark-grey soot smudges on the face and shirt, the fur on the head frizzled into a small puff, the mouse-ear headband crooked, "
                 "a torn little scrap of coral-red balloon rubber hanging from one paw:\n"
                 "  1) standing stiff and stunned, eyes as two blank white dots, mouth a small flat line, a little flat puff of smoke rising from the head;\n"
                 "  2) awkwardly SCRATCHING the back of its head with one paw, an embarrassed sheepish smile, eyes half-closed, a big blue sweat drop on the side of the head;\n"
                 "  3) the cute 'TEHEPERO' pose: winking one eye, the tongue poking out at the corner of the mouth, one paw lightly knocking its own head with a loose fist, the other paw behind its back, one foot kicked up behind.\n", CREFS + ['ref_flat_anim2.png'])
# 이펙트
P['fx_flat'] = (HEAD + FXS +
                "- Palette: cream white, butter yellow, coral red, mint, soft sky blue.\n"
                "  1. a flat four-pointed SPARKLE (cream-white with a butter-yellow center)\n"
                "  2. a small flat cluster of CONFETTI pieces and short curly streamers (coral, mint, yellow, sky blue)\n"
                "  3. flat cartoon CLAP marks: a small burst of five short cream-white lines radiating out\n"
                "  4. a wide flat SHOCKWAVE RING seen from the front (a thin wide ellipse of cream-white and pale sky blue, about 4 times wider than tall)\n"
                "  5. a wide flat cartoon WIND GUST: three long curling cream-white wind streaks swirling from left to right\n"
                "  6. a long thin horizontal SPEED STREAK (a flat white tapered line, thick at the left, pointed at the right)\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['pop'] = (HEAD + FXS +
            "- Palette: coral red (balloon rubber), cream white, butter yellow, warm grey dust.\n"
            "  1. BALLOON POP frame 1: a big round flat cream-white burst flash with short rounded spikes, a few coral-red rubber scraps at its edge\n"
            "  2. BALLOON POP frame 2: a bigger jagged cream and butter-yellow BANG burst with many torn coral-red rubber SHREDS flying outward\n"
            "  3. BALLOON POP frame 3: the end: a ring of torn coral-red rubber shreds and puffy cream air clouds spreading out, fading\n"
            "  4. a huge flat cartoon BLAST DOME seen from the side: a wide half-dome of rolling cream-white and warm grey dust clouds (like a cartoon shock wave dome), flat bottom edge\n"
            "  5. a flat cluster of flying DEBRIS: pastel roof tiles, a yellow cone-roof tip with mouse ears, planks, popcorn and bunting flags\n"
            "  6. a big wide flat rolling DUST WALL: a horizontal band of puffy cream and warm grey dust clouds, rounded top, flat bottom\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
SEED = 0.42                    # 롱샷 판 안 광장 판 크기 (story.json wide)
open(os.path.join(D, 'run_v2.sh'), 'w', encoding='utf-8', newline='\n').write(f'''#!/usr/bin/env bash
# 쥐랜드 관광쥐 초필살기 v2 그림 (풍선 펑) Codex — 컷 한 장 + 연속 프레임 편집 · 광장 → 롱샷 아웃페인팅 · 폐허 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/parkrat/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() {{ n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${{args[@]}}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && {{ echo "$(date +%T) ok $n"; return; }}; done; echo "$(date +%T) FAIL $n"; }}
seed() {{ python -c "from PIL import Image; p=Image.open('pano_on.png').convert('RGB').resize((1536,1024)); s={SEED}; w,h=int(1536*s),int(1024*s); c=Image.new('RGB',(1536,1024),(255,0,255)); c.paste(p.resize((w,h)),((1536-w)//2,(1024-h)//2)); c.save('ref_wide_seed.png')"; }}
plaza() {{ run pano_on; seed; run wide_on & run pano_alt & wait; }}
pair() {{ run $1; shift; for n in "$@"; do run $n & done; wait; }}
plaza &
pair shot_think1 shot_think2 &
pair shot_crowd1 shot_crowd1b shot_crowd2 &
pair shot_hand1 shot_hand2 &
for n in shot_parade1 shot_parade2 shot_parade3 anim_pump anim_end; do run $n & done; wait
pair shot_wave_a1 shot_wave_a2 & pair shot_wave_b1 shot_wave_b2 & pair shot_wave_c1 shot_wave_c2 &
for n in props_flat fx_flat pop; do run $n & done; wait; echo DONE
''')
print(sorted(P))


# ── v3 (2026-10-21 사용자 4차): 디즈니(쥐 머리) 풍선 절대 금지 → 평범한 둥근 풍선 · 풍선이 커질수록 변하는 표정 단계 (컷 교차) ──
#    옛 그림 = old_<이름>.png (쥐 머리 풍선이 든 원본) → 같은 이름으로 '편집' · run_v3.sh
PLAIN = ("- Replace EVERY balloon shaped like a mouse head (a balloon with two round ears on top) with a PLAIN ORDINARY ROUND BALLOON on a thin string (a simple oval rubber party balloon, same color and size, "
         "no ears, no faces, no logos). Do not add any mouse-ear shapes to balloons. Nothing else changes.\n")
FIX = ['shot_parade1', 'shot_parade3', 'shot_think1', 'shot_wave_b1', 'shot_wave_b2', 'shot_wave_c1', 'shot_wave_c2']
Q = {}
for n in FIX:
    Q[n] = (EDIT.format(src=f'old_{n}.png', f='{f}', bgkeep='') + PLAIN, [f'old_{n}.png'])
Q['shot_think2'] = (P['shot_think2'][0], ['shot_think1.png'])      # 유레카 = 새 think1 에서 다시 편집 (같은 틀)
BAL = "a big coral-red PLAIN ROUND rubber party BALLOON (an ordinary oval balloon, slightly taller than wide, NO ears, NO mouse shape, no face, no logo) with a small tied KNOT at the very bottom center, seen from the front, no string"
Q['balloon'] = (HEAD + "- 1536x1024 landscape image: 3 columns, ONE element per column, centered in its third, well inside it, none touching each other or the edges.\n" + FXS.split('\n', 1)[1].replace('effect art', 'prop art') +
                "  1. " + BAL + ", normally inflated, plump\n"
                "  2. the SAME balloon OVER-INFLATED: stretched tight and rounder, a lighter thinner coral color, a few short flat white STRESS lines near the edges, the knot at the very bottom center\n"
                "  3. the SAME balloon about to BURST: stretched extremely thin, pale coral-pink, wobbly bulging outline, many flat white stress lines and two tiny cracks, the knot at the very bottom center\n", ['ref_flat_fx.png'])
CROWD_STAGE = {
    'shot_crowd_q': "- CURIOUS: every mouse has STOPPED clapping (paws lowered or half raised) and looks UP with raised eyebrows and a puzzled little smile, a few heads tilted, mouths small and round. "
                    "A light flat soft CORAL-RED tint and a faint round shadow fall over the TOP edge of the crowd (something big above). Nothing else changes.\n",
    'shot_crowd_worry': "- WORRIED: every mouse looks UP nervously: forced wobbly smiles, eyebrows slanted up in worry, small blue SWEAT DROPS on the foreheads, some leaning back, paws held close to the chest. "
                        "A medium flat CORAL-RED tint and a big round shadow cover the upper half of the crowd. Nothing else changes.\n",
    'shot_crowd_scream': "- TERROR: every mouse SCREAMS with a huge open mouth and tears flying, eyes tiny white dots, paws on their heads or hugging each other, ears flattened, several turning to RUN away, "
                         "headbands falling off, popcorn spilling. A strong flat deep CORAL-RED tint and shadow cover the WHOLE crowd. Nothing else changes.\n",
}
for n, d in CROWD_STAGE.items():
    Q[n] = (EDIT.format(src='shot_crowd1.png', f='{f}', bgkeep=', the same background') + d, ['shot_crowd1.png'])
for n, (b, refs) in Q.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v3.sh'), 'w', encoding='utf-8', newline='\n').write('''#!/usr/bin/env bash
# 쥐랜드 관광쥐 v3: 쥐 머리 풍선 → 평범한 풍선 (편집) · 표정 단계
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/parkrat/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in ''' + ' '.join(FIX) + '''; do [ -f old_$n.png ] || mv $n.png old_$n.png; done
(run shot_think1; run shot_think2) &
for n in shot_parade1 shot_parade3 shot_wave_b1 shot_wave_b2 shot_wave_c1 shot_wave_c2 balloon shot_crowd_q shot_crowd_worry shot_crowd_scream; do run $n & done; wait; echo DONE
''')
print(sorted(Q))
