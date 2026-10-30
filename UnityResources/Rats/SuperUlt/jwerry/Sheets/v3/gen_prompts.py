"""줴리 초필살기 v3 = '무성영화 앵콜' 프롬프트 (2026-10-30 사용자: 톰과 제리 연상 연출 교체 → 턱시도 고양이와 찰리 채플린풍 무성영화 슬랩스틱.
고양이는 게임 턱시도 고양이 그림체 그대로 (톰 연상 금지), 채플린 느낌은 무성영화 장치 위주 (떠돌이 차림 금지), 실제 영화 장면 금지, 세피아 · 자막 카드 2~3장)
컷: 카드 '오늘의 앵콜 공연' → ① 카페 테이블: 쥐 신사가 모자 인사 → 케이크 조각을 짊어지고 살금 ② 턱시도 고양이 웨이터 로우 앵글 ③ 추격 1 (빨리감기)
    ④ 지팡이로 발 걸기 → 넘어지며 크림 파이가 공중으로 ⑤ 파이가 얼굴에 철퍼덕 (정지 화면) → 카드 '웨이터는 단단히 화가 났다!' ⑥ 크림 범벅 추격 2
    ⑦ 무대: 덮치는 고양이 위로 모래주머니 쿵 ⑧ 스포트라이트 아래 모자 인사 → 윙크 (장미) → 아이리스 아웃 → 카드 '끝' → 게임 화면: 대상마다 크림 파이 철퍼덕
그림은 게임 플랫 그림체 컬러로 뽑고 세피아는 slice_superult 에서 입힘 (게임 화면 파이 fx_hit 는 컬러 그대로). 글자 없음.
→ UnityResources/Rats/SuperUlt/jwerry/Sheets/v3"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK3\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'jwerry', 'Sheets', 'v3')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(PH, 'anim_command.png'), 'ref_flat_anim2.png'),
                 (os.path.join(R, 'SuperUlt', 'streamrat', 'Sheets', 'face_close.png'), 'ref_flat_face.png')):
    shutil.copy(src, os.path.join(D, dst))


def strip(folder, out):
    ims = [Image.open(os.path.join(folder, n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
    W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
    for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
    W.save(os.path.join(D, out))
strip(os.path.join(R, 'Parts', 'mouse'), 'ref_mouse.png')               # 게임 생쥐 파츠 (그림체 · 비율만, 색은 프롬프트)
strip(os.path.join(R, 'Cats', 'Parts', 'tuxedo'), 'ref_cat.png')       # 게임 턱시도 고양이 파츠 (그림체 · 색 · 비율 그대로)

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same sizes and positions, the same colors and flat style{bgkeep}. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = ("an elegant old-time 1920s cafe-theater — cream and warm ivory walls, soft sage-green wall panels, honey-brown wooden floor and furniture, white tablecloths, "
       "deep wine-red velvet curtains and seats, polished brass lamps, a few potted palms")
CAFE = ("an elegant old-fashioned 1920s CAFE-THEATER seen at a mouse's height (everything is big): tall walls with wide wooden baseboards, honey-brown plank floors, big round tables with long white tablecloths, "
        "all in simple flat shapes")
BG = ("- {size}, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO people, NO text, NO letters, NO numbers, no logos, no posters with writing.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: " + PAL + ".\n")
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_cat (this game's tuxedo cat sprite parts: copy its colors, shapes and proportions), "
         "ref_mouse (this game's mouse sprite parts: copy its drawing style and proportions) and the approved flat cutscene art ref_flat_anim / ref_flat_face, and (for any background) the approved flat background ref_flat_bg: "
         "FLAT cartoon storybook look in the Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, no black outlines, no gloss, "
         "NO painterly brush strokes, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo, NOT in the style of any existing cartoon show. "
         "Silent-comedy slapstick energy (clear readable poses, squash and stretch, big expressions), but the drawing stays as simple as the references. Palette: " + PAL + ".\n"
         "- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- THE MOUSE (the hero, a tiny polite gentleman performer): build it from the attached ref_mouse (the game\'s mouse: same flat style and body shape): '
        'a small slim mouse with soft WARM GREY fur, a cream belly and muzzle, round ears with peach-pink insides, small black bead eyes with a tiny white highlight, a small pink nose, short whiskers, '
        'a thin pink tail. It wears ONLY three things: a slightly worn small black BOWLER HAT, a tiny black BOW TIE, and it carries a thin light-brown bamboo WALKING CANE with a curved crook handle. '
        'NO mustache, NO jacket, NO trousers, NO shoes (bare little mouse feet). Big head, small body, about as tall as a coffee cup. Charming, cheeky, always polite.\n')
CAT = ('- THE CAT (the stern head WAITER): EXACTLY the game\'s TUXEDO CAT from the attached ref_cat — black fur on the back, head top, ears, legs and tail, WHITE muzzle, chest, belly and paws, '
       'light green eyes, a small pink nose, a thin white tail tip, the same rounded sturdy body and the same face shape as ref_cat. It is a big cat, about FOUR times as tall as the mouse. '
       'As a waiter it wears only a small white collar with a tiny black bow tie and carries a folded white napkin over one forearm. '
       'It is NOT grey, NOT blue, NOT lanky, NOT any famous cartoon cat — it is clearly this game\'s black-and-white tuxedo cat. A proud, fussy, easily offended slapstick villain; comic and cute, NOT scary, no blood.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta, pink-purple or violet on the subjects except the small pink ears, nose and tail. Keep everything fully inside the frame with margin.\n"
CREFS = ['ref_mouse.png', 'ref_cat.png', 'ref_flat_anim.png', 'ref_flat_face.png']
SREFS = CREFS + ['ref_flat_bg.png']
SHOT = "- 1536x1024 LANDSCAPE, one full-frame picture WITH background (no transparency, no panels, no borders).\n"
SHEET = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
         "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses.\n")
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers.\n")
FLOOR = ("- SIDE-SCROLLING CHASE STAGE: seen straight from the SIDE at a mouse's eye level. The wooden plank FLOOR is a wide flat EMPTY band across the lower 22% of the image (its top edge at about 78% of the image height), "
         "with the wall's wooden BASEBOARD running along just above it across the whole width. Characters will run along the floor from left to right, so NOTHING stands on the floor band in the middle; tables and furniture only at the back against the wall.\n")
CONT = ("Use your built-in image generation tool to EDIT the attached image {seed} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {seed}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec (OUTPAINTING to the RIGHT):\n"
        "- The LEFT third of {seed} is the right end of the previous background picture. Keep that left third EXACTLY as it is (same pixels, same position, same scale). "
        "Replace ALL the solid magenta area to the right of it by CONTINUING the same scene seamlessly to the right: the floor line, the baseboard, the wall height, the perspective, the lighting, the scale and the flat style "
        "must continue exactly, so the two pictures join into one long horizontal panorama. Result is 1536x1024 landscape, full frame, no borders, no magenta left anywhere.\n")
P = {}
# 가로 카페 파노라마 (쥐 · 고양이 없음): 테이블 → 디저트 카트 · 피아노 → 작은 무대
P['pano_a'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE') + FLOOR +
               "- The DINING ROOM of " + CAFE + ": against the back wall two big round tables with long white tablecloths and bentwood chairs, a tall potted palm, brass wall lamps, "
               "a big arched window with soft afternoon light, a coat stand with a hanging hat at the far left. The scene continues off the right edge.\n", ['ref_flat_bg.png'])
P['pano_b'] = (CONT.format(seed='ref_seed_b.png', f='{f}') + BG.format(size='1536x1024 LANDSCAPE') + FLOOR +
               "- Continuing to the right: the middle of " + CAFE + ": a tall wooden dessert TROLLEY with three shelves of round cakes against the wall, a slim column, an upright piano against the wall (closed lid, no letters), "
               "another round table with a white tablecloth, sage-green wall panels. The scene continues off the right edge.\n", ['ref_seed_b.png', 'ref_flat_bg.png'])
P['pano_c'] = (CONT.format(seed='ref_seed_c.png', f='{f}') + BG.format(size='1536x1024 LANDSCAPE') + FLOOR +
               "- Continuing to the right: the small STAGE corner of " + CAFE + ": from about 60% of the image width a low wooden stage platform (its front edge a little above the floor line) with deep wine-red velvet curtains drawn open at both sides, "
               "a row of small round brass footlights along its front edge, a plain wooden backdrop. The scene ends at the right edge.\n", ['ref_seed_c.png', 'ref_flat_bg.png'])
# ① 카페 테이블: 모자 인사 → 케이크를 짊어지고 살금 (편집)
P['shot_open1'] = (HEAD + SHOT + STYLE + CHAR +
                   "- SCENE, a LOW-ANGLE MEDIUM SHOT on top of a cafe table with a white tablecloth in " + CAFE + " (soft background of the dining room): a tall round cream CAKE on a cake stand stands at the left, "
                   "one big slice cut out and lying on a plate in front. The mouse stands at the right of center facing the viewer, politely LIFTING its bowler hat off its head with one paw in greeting, "
                   "the cane hooked over the other arm, a charming closed-eye smile. The mouse fills about 45% of the image height, lower center.\n", SREFS)
P['shot_open2'] = (EDIT.format(src='shot_open1.png', f='{f}', bgkeep=', the same background, the same cake stand') +
                   "- The mouse has put its hat back on and now carries the big CAKE SLICE on its back (holding it with both paws over its shoulder), tiptoeing sneakily to the right on the tips of its toes, "
                   "looking back at the viewer with a guilty little grin, the cane tucked under one arm. The plate in front is now empty. Nothing else changes.\n", ['shot_open1.png'])
# ② 턱시도 고양이 웨이터 (로우 앵글 · 사선)
P['shot_catface'] = (HEAD + SHOT + STYLE + CAT +
                     "- SCENE, an EXTREME CLOSE-UP LOW-ANGLE shot looking UP at the tuxedo cat waiter's face with a DUTCH TILT (tilted about 15 degrees), a brass ceiling lamp behind its head: "
                     "it holds a round silver TRAY high on one paw with three cream PIES on it, one eyebrow raised high, eyes narrowed in offended suspicion, whiskers bristling, its small black bow tie on the white collar. "
                     "The face and tray fill most of the frame.\n", SREFS)
# 셀 프레임: 쥐 달리기 · 쥐 동작 · 고양이 달리기 · 고양이 동작
P['anim_mrun'] = (HEAD + SHEET + "- CAMERA: every frame is a straight SIDE VIEW, the mouse runs to the RIGHT. All frames share the same ground line.\n" + STYLE + CHAR + MAG +
                  "- A quick little cartoon RUN CYCLE of the mouse fleeing while holding its bowler hat on its head with one paw and the cane in the other, the big CAKE SLICE balanced on its back, a cheeky grin:\n"
                  "  1) legs stretched far apart front and back (reach);\n"
                  "  2) legs gathered under the body, body slightly up (passing);\n"
                  "  3) the back foot pushing off, the front foot landing.\n", CREFS + ['ref_flat_anim2.png'])
P['anim_mmove'] = (HEAD + SHEET + "- CAMERA: every frame is a SIDE VIEW, the mouse faces the RIGHT. All frames share the same ground line.\n" + STYLE + CHAR + MAG +
                   "- Three separate key poses of the mouse (NO cake in these frames):\n"
                   "  1) running to the right while LOOKING BACK over its shoulder at the viewer's left and politely TIPPING its bowler hat with a cheeky wink;\n"
                   "  2) a big graceful LEAP to the right, body stretched in the air, hat held on with one paw, cane raised;\n"
                   "  3) standing and spinning the cane in a fast circle in front of it with a proud little smile.\n", CREFS + ['ref_flat_anim2.png'])
P['anim_crun'] = (HEAD + SHEET + "- CAMERA: every frame is a straight SIDE VIEW, the cat runs to the RIGHT. All frames share the same ground line.\n" + STYLE + CAT + MAG +
                  "- A fussy cartoon CHASE RUN CYCLE of the tuxedo cat waiter running on its HIND legs, holding the round silver TRAY high above its head on one paw with three cream PIES wobbling on it, "
                  "the napkin flapping, an offended frown:\n"
                  "  1) legs stretched far apart front and back (reach);\n"
                  "  2) legs gathered under the body, body bobbing up;\n"
                  "  3) the front foot landing, the back foot kicking off.\n", CREFS + ['ref_flat_anim2.png'])
P['anim_cmove'] = (HEAD + SHEET + "- CAMERA: every frame is a SIDE VIEW, the cat faces and moves to the RIGHT. All frames share the same ground line.\n" + STYLE + CAT + MAG +
                   "- The cat must look EXACTLY like the cat in the attached anim_crun.png (the same black-and-white tuxedo coloring, the same head, collar, bow tie and body proportions), running on its hind legs.\n"
                   "- Three separate key poses of the cat WITHOUT the tray (each well inside its own third, with a wide magenta gap between frames):\n"
                   "  1) running FURIOUS with its whole face and chest covered in thick white CREAM (only its angry eyes show), flat white STEAM puffs from its ears, fists pumping;\n"
                   "  2) a huge flying POUNCE: the whole body stretched horizontally in the air toward the right, both front paws reaching forward with claws out, cream still on its face;\n"
                   "  3) SLIPPING on the floor: both legs spinning in a blur under it, arms flailing, body tilted back, a startled face with cream on it.\n", ['anim_crun.png'] + CREFS)
# ④ 지팡이로 발 걸기 → 넘어지며 파이가 공중으로 (편집)
P['shot_trip1'] = (HEAD + SHOT + STYLE + CHAR + CAT +
                   "- SCENE, a low SIDE VIEW between the tables of " + CAFE + " (white tablecloths hanging down behind): the tuxedo cat waiter strides proudly from the left toward the right, nose in the air, "
                   "the silver tray with three cream pies held high on one paw. At the right, the mouse peeks out from under the hem of a white tablecloth and slyly reaches out its cane so the curved crook "
                   "is just about to HOOK the cat's ankle. The cat is big (about four times the mouse).\n", SREFS)
P['shot_trip2'] = (EDIT.format(src='shot_trip1.png', f='{f}', bgkeep=', the same background') +
                   "- TRIP! The mouse has YANKED the cane: its crook caught the cat's ankle and the cat is FALLING forward flat in mid-air, horizontal, arms flung out, eyes popping in surprise, "
                   "the silver tray flipping away and the three cream pies flying HIGH up into the air above it, short flat motion lines. The mouse holds the cane with both paws, leaning back. Nothing else changes.\n", ['shot_trip1.png'])
# ⑤ 파이가 얼굴에 철퍼덕 (편집)
P['shot_pie1'] = (HEAD + SHOT + STYLE + CAT +
                  "- SCENE, a MEDIUM CLOSE SHOT on the floor of " + CAFE + ": the tuxedo cat waiter sits dazed on the floor after a fall, looking straight UP with its mouth open in a gasp, "
                  "the napkin crooked on its arm; a single cream PIE in a round tin is falling straight down toward its face, near the top of the frame.\n", SREFS)
P['shot_pie2'] = (EDIT.format(src='shot_pie1.png', f='{f}', bgkeep=', the same background') +
                  "- SPLAT! The cream pie has landed squarely on the cat's FACE: the round pie tin stuck on its face, thick white cream splattered all over its head, whiskers and chest, big flat cream drips and a few flat cream blobs flying, "
                  "its paws frozen in the air. At the right edge of the frame the little mouse (warm grey, black bowler hat, bow tie, cane) politely tips its hat to the viewer. Nothing else changes.\n", ['shot_pie1.png'] + CREFS[:1])
# ⑦ 무대: 덮치는 고양이 위로 모래주머니 (편집)
P['shot_stage1'] = (HEAD + SHOT + STYLE + CHAR + CAT +
                    "- SCENE, a wide FRONT VIEW of the small cafe STAGE with deep wine-red velvet curtains open at both sides, brass footlights along the front edge, a plain wooden backdrop: "
                    "a heavy canvas SANDBAG hangs high above the center of the stage on a rope. The cream-covered tuxedo cat (white cream on its face) LEAPS onto the stage from the left, pouncing toward the center, claws out. "
                    "At the right side of the stage the mouse holds the end of the rope with both paws, ready to let go, a sly smile.\n", SREFS)
P['shot_stage2'] = (EDIT.format(src='shot_stage1.png', f='{f}', bgkeep=', the same background, the same stage') +
                    "- THUD! The mouse has let go of the rope: the big SANDBAG has dropped right onto the cat at the center of the stage, the cat squashed flat under it with only its paws, tail and dizzy face sticking out, "
                    "eyes as two spirals, three flat yellow stars above it, flat dust puffs around. The mouse dusts off its paws at the right. Nothing else changes.\n", ['shot_stage1.png'])
# ⑧ 스포트라이트 아래 모자 인사 → 윙크 (편집)
P['shot_bow1'] = (HEAD + SHOT + STYLE + CHAR +
                  "- SCENE, a MEDIUM FRONT SHOT of the stage: one round warm SPOTLIGHT circle on the stage floor and the mouse standing inside it at the center, taking a deep elegant BOW with its bowler hat held out in one paw, "
                  "the cane in the other; the deep red velvet curtains frame the shot at both sides, the rest of the stage is darker. In the dark background on the left lies a big sandbag with a dizzy cat's tail sticking out from under it.\n", SREFS)
P['shot_bow2'] = (EDIT.format(src='shot_bow1.png', f='{f}', bgkeep=', the same background, the same spotlight and curtains') +
                  "- The mouse has straightened up and puts its hat back on with a cheeky WINK to the viewer, holding the cane up in a little salute; a few red ROSES are falling and landing on the stage around it. Nothing else changes.\n", ['shot_bow1.png'])
# 자막 카드 (글은 게임이 얹음)
P['shot_card'] = (HEAD + SHOT +
                  "- A SILENT-FILM INTERTITLE CARD with NO words: a flat solid near-black background filling the whole frame, with an elegant thin cream-white ART DECO ornamental BORDER frame inside the edges "
                  "(double thin lines with small geometric corner ornaments and a small simple ornament at the top and bottom center). The whole middle area is EMPTY plain black for text to be added later. "
                  "Flat, clean, simple shapes. NO text, NO letters, NO numbers.\n", [])
# 소품 · 이펙트 · 필름
P['props_flat'] = (HEAD + FXS.replace('effect art', 'prop art') +
                   "  1. a cream PIE in a round metal tin seen from the side, tall white whipped cream on top\n"
                   "  2. a small black BOWLER HAT\n"
                   "  3. a thin light-brown bamboo WALKING CANE with a curved crook handle\n"
                   "  4. a big triangular CAKE SLICE with cream layers and a strawberry on top\n"
                   "  5. a single red ROSE with a long green stem\n"
                   "  6. a heavy beige canvas SANDBAG tied at the top with a short rope\n", ['ref_flat_fx.png', 'ref_mouse.png'])
P['fx_flat'] = (HEAD + FXS +
                "- Palette: cream white, butter yellow, warm light grey.\n"
                "  1. a flat cartoon DUST PUFF cloud trail (cream and pale grey, three round puffs in a row getting smaller to the left)\n"
                "  2. a long thin horizontal SPEED STREAK (a flat white tapered line, thick at the left, pointed at the right)\n"
                "  3. a ring of three flat yellow DIZZY STARS circling (seen slightly from above, an ellipse)\n"
                "  4. a big flat cartoon CREAM SPLAT (a round white whipped-cream splash with drips and a few flying cream blobs)\n"
                "  5. a flat white STEAM puff shooting up (two round puffs)\n"
                "  6. a wide flat SHOCKWAVE RING seen from the front (a thin wide ellipse of cream-white, about 4 times wider than tall)\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (HEAD + FXS +
               "- Palette: warm golden-brown pie crust, cream white, silver-grey tin, a little strawberry red.\n"
               "  top row (each one alone, centered):\n"
               "  1. a cream PIE in a round tin flying, tilted with the cream side facing DOWN-LEFT (as if thrown)\n"
               "  2. the same cream pie tumbling, tilted the other way\n"
               "  3. a single flat yellow five-pointed cartoon STAR\n"
               "  bottom row = 3 consecutive frames of a PIE SPLAT hit, same size and centered at the same point:\n"
               "  4. a small round cream-white splash with short spikes\n"
               "  5. a bigger flat cream SPLAT burst with drips and the empty tin bouncing off\n"
               "  6. a flat cream splat puddle with a few drips, fading\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['film'] = (HEAD + FXS.replace("Do not use magenta, pink or purple on the elements.", "Use only dark brown-black and cream-white, no magenta, no pink.") +
             "- OLD FILM DAMAGE overlay elements (like scratches on a very old silent movie print), each very simple and flat:\n"
             "  1. one long thin perfectly VERTICAL white scratch line from the top to the bottom of the cell, slightly wavy\n"
             "  2. two thin parallel VERTICAL dark scratch lines from top to bottom, broken in a few places\n"
             "  3. a single curly thin dark HAIR squiggle\n"
             "  4. a loose cluster of small cream-white and dark DUST SPECKS (about 20 tiny round dots of different sizes)\n"
             "  5. a soft irregular round cream-white BURN BLOTCH (a flat light patch with a ragged edge)\n"
             "  6. a small thin dark CROSS-shaped scratch mark and a short diagonal scratch\n", ['ref_flat_fx.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v3.sh'), 'w', encoding='utf-8', newline='\n').write('''#!/usr/bin/env bash
# 줴리 초필살기 v3 그림 (무성영화 앵콜) Codex — 가로 카페 파노라마 이어 그림 · 컷 한 장 + 연속 프레임 편집 · 셀 시트 · 소품 · 이펙트 · 필름
D=/c/Project/NKK3/NKK_PROJECT/UnityResources/Rats/SuperUlt/jwerry/Sheets/v3; CX=/c/Project/NKK3/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
seed() { python -c "from PIL import Image; p=Image.open('$1.png').convert('RGB').resize((1536,1024)); c=Image.new('RGB',(1536,1024),(255,0,255)); c.paste(p.crop((1024,0,1536,1024)),(0,0)); c.save('$2')"; }
pano() { run pano_a; seed pano_a ref_seed_b.png; run pano_b; seed pano_b ref_seed_c.png; run pano_c; }
pair() { run $1; shift; for n in "$@"; do run $n & done; wait; }
pano &
pair shot_open1 shot_open2 &
pair shot_trip1 shot_trip2 &
pair shot_pie1 shot_pie2 &
pair shot_stage1 shot_stage2 &
pair shot_bow1 shot_bow2 &
run shot_catface &
run shot_card &
wait
pair anim_crun anim_cmove &
run anim_mrun &
run anim_mmove &
run props_flat &
run fx_flat &
run fx_hit &
run film &
wait
echo "$(date +%T) done"
''')
print('prompts', len(P))
