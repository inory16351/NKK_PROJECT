"""슈퍼 요리사 쥐 초필살기 v2 = '라따뚜이 시식' 프롬프트 (2026-10-20 사용자: 게임 그림체 그대로 · Codex CLI)
컷: ① 쥐 아래 한쪽 무릎 꿇고 고개 숙인 요리사가 요리를 바침 (아래 → 위 페데스탈) ② 벌벌 떨리는 손 위 라따뚜이 클로즈업
    ③ 눈 감은 거만한 쥐가 손가락으로 찍어 맛봄 → 얼굴로 급줌 → 눈 번쩍 ④ 블러 움직임으로 가림 → 사람 머리 위에 올라탄 샷
    ⑤ 머리카락을 마구 뜯어 대머리로 ⑥ 기절해 쓰러진 사람 위에서 브이 하는 쥐 (로우 앵글)
그림체 = 다른 v2 컷신과 같은 플랫 (ref_flat_bg = 로비 배경 · ref_flat_anim = 파라오 셀 프레임 · ref_flat_face = 치즈 퐁듀 클로즈업 · ref_rig = 게임 파츠 · ref_cook = 요리사 게임 파츠). 글자 없음.
연속 프레임(shot_taste2·3 · shot_ride2·3 · shot_v2)은 1번 프레임을 참고로 넣어 '같은 구도에서 표정 · 손만' 편집 → 프레임끼리 겹쳐도 맞음.
→ UnityResources/Rats/SuperUlt/starchef/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'starchef', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'),
                 (os.path.join(R, 'SuperUlt', 'fondue', 'Sheets', 'cu_face1.png'), 'ref_flat_face.png'),
                 (os.path.join(R, 'SuperUlt', 'starchef', 'Sheets', 'ref_char.png'), 'ref_char.png')):
    shutil.copy(src, os.path.join(D, dst))


def strip(paths, out):
    ims = [Image.open(p).convert('RGBA') for p in paths]
    W = Image.new('RGB', (sum(i.width for i in ims) + 10 * len(ims) + 30, max(i.height for i in ims) + 40), (255, 0, 255)); x = 20
    for i in ims: W.paste(i, (x, 20), i); x += i.width + 10
    W.save(os.path.join(D, out))


strip([os.path.join(R, 'Parts', 'starchef', n + '.png') for n in ('head', 'torso', 'front', 'back', 'tail')], 'ref_rig.png')
HU = os.path.join(R, 'Humans', 'Parts')
strip([os.path.join(HU, 'cook', n + '.png') for n in ('head', 'scared', 'torso', 'arm', 'leg')] + [os.path.join(HU, 'cook_mount', n + '.png') for n in ('cry', 'strain')], 'ref_cook.png')

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same characters' size and position, the same background, the same colors and flat style. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = "a warm cozy French restaurant kitchen — cream tiled walls, warm wood, copper pots, soft warm yellow light, a few muted teal and terracotta accents"
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig and ref_cook (the characters' in-game sprite parts: copy their colors, shapes and proportions), "
         "the approved flat cutscene art ref_flat_anim / ref_flat_face, and the approved flat background ref_flat_bg: "
         "FLAT cartoon storybook look in the Untitled Goose Game mood, big simple soft flat shapes, flat colors with only gentle soft shading (at most one shade step), very little texture, NO individual fur or hair strands, "
         "small simple black bead eyes with one tiny white highlight, no black outlines, no gloss, NO painterly brush strokes, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo. Background (if any) is as flat and simple as ref_flat_bg. Palette: " + PAL + ".\n"
         "- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
RAT = ('- CHARACTER "Star Chef Rat": build it EXACTLY from the attached ref_rig (use ref_char only for the design, NOT its painted look): '
       'a cute chubby BLUE-GREY rat with big round pink ears, a pink nose, white whiskers, small black bead eyes, a tall white CHEF HAT with three little GOLD STARS, '
       'a white double-breasted chef jacket with round buttons and a RED neckerchief, small blue-grey paws with pink fingers, a long pink tail. Big head, small body. Smaller than a human hand.\n')
COOK = ('- HUMAN "the Cook": build him EXACTLY from the attached ref_cook (his in-game sprite parts): a young man cook with peach skin, a big round nose, small black bead eyes, '
        'messy short BROWN wavy hair with sideburns, a white double-breasted chef jacket, black-and-white CHECKERED trousers, black shoes. Same flat cartoon look as ref_cook.\n')
DISH = ('- The DISH: a fancy RATATOUILLE (like the famous movie dish): thin round slices of green zucchini, purple eggplant, red tomato and yellow squash fanned in a neat SPIRAL on a puddle of red sauce, '
        'with a little sprig of green basil on top, on a round white plate.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta or purple on the subject (the eggplant is dark violet-brown, not magenta). Keep the subject's edges clean.\n"
CREFS = ['ref_rig.png', 'ref_cook.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_flat_face.png', 'ref_flat_bg.png']
P = {}
# ① 무릎 꿇고 바침 — 세로 긴 그림, 카메라 몸체가 아래(꿇은 요리사 · 접시)에서 위(카운터 위 쥐)로 올라감 (같은 높이 시선 = 틸트 아님)
P['shot_offer'] = ("- 1024x1536 PORTRAIT, one full-frame picture with background (no transparency, no panels, no borders).\n" + STYLE + RAT + COOK + DISH +
                   "- SCENE, side view at eye level (the camera does NOT look up or down): a restaurant kitchen. In the LOWER HALF the Cook KNEELS ON ONE KNEE on the floor, facing RIGHT, his head BOWED deeply (face looking down at the floor, eyes shut, humble, nervous sweat drop), "
                   "his chef hat on his head, both arms raised up high above his bowed head, offering up the plate of ratatouille like a royal tribute. "
                   "In the UPPER part of the picture, on top of a tall wooden kitchen counter on the RIGHT (the counter top at about 30% of the image height), the Star Chef Rat stands upright like a little king, "
                   "both arms crossed, chin raised, eyes CLOSED with a smug, arrogant face, looking down its nose. The raised plate reaches up to about 40% of the image height, just below and in front of the rat. "
                   "Kitchen behind: cream tiles, a hanging rack of copper pots at the top, a stove at the far left. Lots of headroom above the rat (the top 15% is wall and hanging pots).\n", CREFS)
# ①②③ 클로즈업 뒤 배경판
P['face_bg'] = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency). NO characters, NO people, NO text.\n"
                "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (style only): big simple flat color shapes, flat fills, NO outlines, NO gloss, NO painterly brush strokes.\n"
                "- A FLAT cut-in backdrop for a dramatic close-up: big flat radial RAYS in warm golden yellow, tomato red, cream and zucchini green bands radiating from the center, a calm plain middle area. Flat shapes only.\n", ['ref_flat_bg.png'])
# ② 벌벌 떨리는 손 위 라따뚜이 클로즈업 — ①과 같은 측면 시점 (2026-10-20 사용자: 하이앵글 말고 측면 유지 · 연속성). 손 · 접시(자홍) + 같은 주방 배경판 따로 → 손만 떨림
P['cu_dish'] = ("- 1536x1024 LANDSCAPE CLOSE-UP, SIDE VIEW at the height of the plate (the camera looks straight from the side, NOT from above): continuing the shot of ref shot_offer (the Cook kneels facing RIGHT and raises the plate above his head). "
                "The Cook's two raised forearms and HANDS (peach skin, white chef jacket sleeves) enter from the LOWER-LEFT corner, holding the round white plate up high and level, the plate in the center-right of the frame seen from the side "
                "(we see the plate rim in profile and the ratatouille slices standing up in a neat fan on top, red sauce, a basil sprig). "
                "The hands are TREMBLING with fear: little flat shaky motion lines next to the fingers and the plate rim, two small blue sweat drops flying off. Only forearms, hands and the plate, no face.\n" + STYLE + COOK + DISH + MAG, ['shot_offer.png'] + CREFS)
P['dish_bg'] = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency). NO characters, NO people, NO animals, NO text.\n" + STYLE +
                "- The SAME kitchen as the attached shot_offer (same cream tiles, same warm light, same colors), a closer SIDE VIEW at the height just above the Cook's head: cream tiled wall, at the RIGHT edge the top of the tall wooden counter (its top edge at about 60% of the image height), "
                "a few blurred kitchen things on a shelf far behind (a potted basil, wooden spoons in a jar), soft and simple, slightly out of focus (soft flat shapes, depth of field). Nothing in the center-left area (hands and a plate will be placed there).\n", ['shot_offer.png', 'ref_flat_bg.png'])
# ③ 눈 감은 거만한 쥐가 손가락으로 찍어 맛봄 (3 프레임: 1 = 원본, 2·3 = 편집 / 3 = 눈 번쩍)
P['shot_taste1'] = ("- 1536x1024 LANDSCAPE, one full-frame picture with background (no transparency). MEDIUM CLOSE-UP of the Star Chef Rat, chest up, slightly to the LEFT of center, facing the viewer in 3/4 view, the face big (the head about 45% of the image height, the chef hat fully visible). "
                    "The rat's eyes are CLOSED, chin raised high, a very ARROGANT, snobbish expression (one eyebrow up, mouth a small smug pout). "
                    "At the bottom right, the plate of ratatouille is held up into the frame by the Cook's trembling hands (only the hands and sleeves visible, entering from the bottom edge). "
                    "The rat delicately dips ONE fingertip of its right paw into the red sauce of the ratatouille, pinky raised fancily. Behind: a soft flat kitchen background (cream tiles, copper pots), simple.\n"
                    + STYLE + RAT + COOK + DISH, CREFS)
P['shot_taste2'] = (EDIT.format(src='shot_taste1.png', f='{f}') +
                    "- The rat now TASTES: its fingertip (with a dab of red sauce) is in its mouth, licking it, the eyes still CLOSED, chin still raised, a judging, critical expression (eyebrows pulled together, thinking). The plate and hands stay. Nothing else changes.\n", ['shot_taste1.png'])
P['shot_taste3'] = (EDIT.format(src='shot_taste1.png', f='{f}') +
                    "- The rat's eyes SNAP WIDE OPEN: big round wide-open eyes with tiny pupils and one big sparkle each (shocked and fired up), whiskers bristling straight out, ears perked, mouth tight, "
                    "its paw back near its chin (fingertip with a little red sauce). Fierce, intense look straight at the viewer. Nothing else changes.\n", ['shot_taste1.png'])
# ④⑤ 사람 머리 위에 올라탐 → 머리카락 뜯기 → 대머리 (3 프레임)
P['shot_ride1'] = ("- 1536x1024 LANDSCAPE, one full-frame picture with background (no transparency). FRONT VIEW of the Cook from the chest up, centered, his head big (about 45% of the image height), "
                   "NO chef hat on him (it has been knocked off), his messy BROWN wavy hair fully visible. The Star Chef Rat sits on TOP of his head like riding a horse, both little paws GRABBING big tufts of his brown hair, "
                   "its eyes open, a determined, mischievous grin. The Cook is startled: eyes wide, eyebrows up, mouth open in a small 'O', a sweat drop. Behind: a simple flat kitchen background. Slapstick cartoon comedy, nobody gets hurt.\n"
                   + STYLE + RAT + COOK, CREFS)
P['shot_ride2'] = (EDIT.format(src='shot_ride1.png', f='{f}') +
                   "- Slapstick: the rat comically YANKS the hair with both paws, a clump of brown hair coming off in each paw, many little tufts of brown hair flying up into the air around the head, "
                   "the Cook's hair is half gone (big bare patches on top), the Cook yells comically: eyes squeezed shut, big open mouth, cartoon tears streaming. Cartoon comedy, no injury. Nothing else changes.\n", ['shot_ride1.png'])
P['shot_ride3'] = (EDIT.format(src='shot_ride1.png', f='{f}') +
                   "- The Cook is now completely BALD: a shiny smooth round bald head with one simple white shine highlight, not a single hair left (only the sideburns), "
                   "the rat stands proudly on the bald head holding up two big clumps of brown hair in both paws, grinning. The Cook looks stunned and empty: tiny dot eyes, mouth a wobbly line, a single tear. Cartoon comedy. Nothing else changes.\n", ['shot_ride1.png'])
# ⑥ 기절해 쓰러진 사람 위에서 브이 (로우 앵글)
P['shot_v1'] = ("- 1536x1024 LANDSCAPE, one full-frame picture with background (no transparency). EXTREME LOW ANGLE from the floor (worm's-eye view): "
                "the Cook lies flat on his back on the kitchen floor, fainted, seen from his side with his bald shiny head toward the LEFT, his face up: cartoon swirly spiral eyes, tongue out a little, a little ring of yellow stars above his head (cartoon knocked-out, comedic, no injury). "
                "The Star Chef Rat STANDS on the Cook's chest at the center, very big in the frame because the camera is close and low, seen from below: chef hat with three gold stars tall against the ceiling, "
                "one paw making a big V SIGN (peace sign) toward the viewer, the other paw on its hip, one eye winking, a huge triumphant cheeky grin, holding a clump of brown hair under its arm like a trophy. "
                "Behind: the kitchen ceiling seen from below with warm hanging lamps and copper pots, flat and simple. Victory pose.\n" + STYLE + RAT + COOK, CREFS)
P['shot_v2'] = (EDIT.format(src='shot_v1.png', f='{f}') +
                "- The rat's V-sign paw is pushed a little closer to the viewer and a tiny bit bigger, both eyes now closed in a happy upward curve, mouth wide open laughing. Nothing else changes.\n", ['shot_v1.png'])
# ④ 올라타기를 쥐 · 사람 따로 (2026-10-20 사용자: 화면 전환 블러로는 올라타는 느낌이 안 남 → 쥐가 휙 지나간 뒤 머리 위에 나타나게)
#    shot_ride0 = shot_ride1 에서 쥐만 지운 사람 판 (아직 모르는 멍한 얼굴) · cu_ratride = 같은 틀에서 쥐만 남김 (자홍, 자르지 않음 → ride1 과 자리가 같음) · pose_leap = 날아오르는 쥐
P['shot_ride0'] = (EDIT.format(src='shot_ride1.png', f='{f}') +
                   "- REMOVE the rat completely: no rat, no tail, no chef hat with stars on top of his head. Fill in his messy brown wavy hair on the top of his head where the rat was (a full head of hair, a natural flat cartoon shape). "
                   "The Cook does NOT know anything yet: calm, blank face, eyes normal looking forward, eyebrows relaxed, mouth a small closed neutral line, NO sweat drop. Background and everything else unchanged.\n", ['shot_ride1.png'])
P['cu_ratride'] = (EDIT.format(src='shot_ride1.png', f='{f}') +
                   "- Keep ONLY the rat (with its chef hat, paws, tail) exactly where and how it is, the same size and position. Replace EVERYTHING else (the Cook, his hair, the whole background) with ONE solid flat pure magenta #FF00FF (for chroma keying). "
                   "Where the rat's paws grab the hair, show the paws closed holding small brown hair tufts. Clean edges, no shadow.\n", ['shot_ride1.png'])
P['pose_leap'] = ("- 1024x1024 SQUARE, ONE character, full body, filling about 70% of the frame, centered.\n" + STYLE + RAT + MAG +
                  "- POSE: a fast dynamic LEAP / dive flying to the RIGHT, seen from the side: body stretched out horizontally like a superhero flying, both front paws reaching forward (to the right) with fingers spread ready to grab, "
                  "the back legs kicked out behind, the long pink tail streaming behind (to the left), the chef hat pressed back by the wind, ears flapping back, eyes wide open, fierce determined grin. Same character as ref_rig.\n", CREFS)
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, purple or pink on the elements.\n- NO text, NO letters, NO numbers.\n")
P['fx_flat'] = (FXS +
                "  1. a fluffy TUFT of messy brown wavy HAIR (a clump pulled out, flat cartoon, same brown as ref_cook's hair)\n"
                "  2. a few loose curly brown HAIR STRANDS floating (3 simple flat curls)\n"
                "  3. a cartoon DIZZY ring: 5 little yellow stars in an oval ring\n"
                "  4. a flat four-pointed TWINKLE star sparkle in cream white and gold\n"
                "  5. a wide horizontal MOTION SMEAR: a long soft cream-white swoosh band with tapered ends and 3 thin parallel speed stripes (a whip-pan blur)\n"
                "  6. a flat horizontal SPEED LINE streak in cream white (long thin tapered stripe)\n", ['ref_flat_fx.png', 'ref_flat_fx2.png', 'ref_cook.png'])
P['props_flat'] = (FXS.replace('effect art', 'prop art') + COOK +
                   "  1. the Cook's plain white CHEF HAT (no stars), tumbling, tilted\n"
                   "  2. one big blue cartoon SWEAT DROP\n"
                   "  3. a soft wisp of white STEAM rising (flat, 2 curls)\n"
                   "  4. a little cartoon blue TEAR splash (3 drops)\n"
                   "  5. a small pink plastic HAIR COMB, tilted\n"
                   "  6. a hanging rack of three COPPER POTS and a ladle (foreground decoration, hanging from the top)\n", ['ref_flat_fx.png', 'ref_cook.png'])
for n, (b, refs) in P.items():
    head = HEAD.format(f=n + '.png') if not b.startswith('Use your built-in') else ''
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(head + b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
RUN = r'''#!/usr/bin/env bash
# 슈퍼 요리사 쥐 초필살기 v2 그림 (라따뚜이 시식) Codex — 1번 프레임 · 배경 · 이펙트 동시 / 연속 프레임은 1번이 나온 뒤 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/starchef/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
taste() { run shot_taste1; run shot_taste2 & run shot_taste3 & wait; }
ride() { run shot_ride1; run shot_ride2 & run shot_ride3 & wait; }
vee() { run shot_v1; run shot_v2; }
taste & ride & vee &
for n in shot_offer cu_dish dish_bg face_bg; do run $n & done; wait
for n in fx_flat props_flat shot_ride0 cu_ratride pose_leap; do run $n & done; wait; echo DONE
'''
open(os.path.join(D, 'run_v2.sh'), 'w', encoding='utf-8', newline='\n').write(RUN)
print(sorted(P))
