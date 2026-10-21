"""짜릿햄쥐 초필살기 v2 = '백만볼트 · 또 당했다~' 프롬프트 (2026-10-21 사용자)
컷: ① 풀밭에서 해바라기씨를 먹는 햄쥐를 동전 고양이(냐옹이 닮은꼴)가 볼을 찌르고 꼬리를 당기며 괴롭힘 ② 햄쥐 얼굴 클로즈업: 그늘진 눈 → 볼이 파지직
    ③ 백만볼트: 노란 번개 발사 → 고양이 감전 (털 곤두섬 ↔ 엑스레이 해골 깜빡) → 까맣게 그을림 ④ 전기를 두른 몸통박치기
    ⑤ 로켓단 퇴장: 고양이가 하늘로 빙글빙글 날아가 점이 되고 반짝 ✦ ⑥ 햄쥐 브이 윙크
그림체 = 다른 v2 컷신과 같은 플랫 (ref_flat_bg = 로비 배경 · ref_flat_anim = 파라오 셀 프레임 · ref_flat_face = 스트리머 클로즈업 · ref_rig = 게임 파츠). 글자 없음.
→ UnityResources/Rats/SuperUlt/zapham/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'zapham', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PK = os.path.join(R, 'SuperUlt', 'parkrat', 'Sheets', 'v2')
for n in ('ref_flat_bg', 'ref_flat_fx', 'ref_flat_fx2', 'ref_flat_anim', 'ref_flat_anim2', 'ref_flat_face'):
    shutil.copy(os.path.join(PK, n + '.png'), os.path.join(D, n + '.png'))
shutil.copy(os.path.join(R, 'SuperUlt', 'zapham', 'Sheets', 'ref_zapham.png'), os.path.join(D, 'ref_char.png'))
ims = [Image.open(os.path.join(R, 'Parts', 'zapham', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
EDIT = ("Use your built-in image generation tool to EDIT the attached image {src} and save the result as a NEW PNG file: {f} (in the current working directory). Do not overwrite {src}. "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nEdit spec:\n"
        "- Keep EVERYTHING identical to {src}: the same canvas size, the same framing, camera angle and zoom, the same sizes and positions, the same colors and flat style{bgkeep}. "
        "Change ONLY what is described below, so the two pictures can be swapped as consecutive animation frames.\n")
PAL = ("a bright sunny countryside afternoon in a cartoon adventure anime mood — clear saturated sky blue, puffy white clouds, fresh grass green meadow, soft blue-green distant mountains, "
       "warm cream path, the hero's sunny yellow and warm orange-brown as the strongest accents, electric lemon-yellow for lightning")
BG = ("- {size}, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO people, NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: " + PAL + ".\n")
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the hero's in-game sprite parts: copy its colors, shapes and proportions) and the approved flat cutscene art ref_flat_anim / ref_flat_face, "
         "and (for any background) the approved flat background ref_flat_bg: FLAT cartoon storybook look in the Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, "
         "no black outlines, no gloss, NO painterly brush strokes, NOT a Disney/Pixar/3D look, NOT anime style, NOT a photo. Palette: " + PAL + ".\n"
         "- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- HERO (an original character of this game, a pet HAMSTER first of all): build it from the attached ref_rig (the in-game sprite parts of this character: copy its colors and proportions): '
        'a chubby round golden-orange Syrian HAMSTER with a big round hamster face, puffy stuffed CHEEK POUCHES, a cream-white muzzle and belly, a pink nose, white whiskers, small black bead eyes, small round PINK hamster ears, '
        'wearing a loose homemade knitted mustard-yellow BEANIE-HOOD (like a cozy winter hat, a bit too big, with two droopy knitted flaps with brown yarn tips flopping down behind its head), '
        'a big AA BATTERY (orange and black) strapped on its back with a brown strap and a red and a black wire, and a stubby hamster tail with a little cardboard zigzag taped on. Short stubby arms and legs, round body. Cute.\n')
CAT = ('- VILLAIN "Coin Cat": a cheeky bipedal cartoon CAT that stands upright on its hind legs like a person, about 1.6 times as tall as the hamster: cream-beige fur, a big round head, big pointed ears with dark brown insides, '
       'a shiny round GOLD COIN charm on its forehead, long thin whiskers sticking out sideways, narrow sly eyes, a wide smug grin with two little fangs, brown paws and feet, a long thin tail that ends in a brown CURL. '
       'Comic rival of a children\'s cartoon, mischievous and silly, NOT scary.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta, pink-purple or violet on the subjects except the small pink noses and paw pads. Keep everything fully inside the frame with margin.\n"
CREFS = ['ref_rig.png', 'ref_flat_anim.png', 'ref_flat_face.png']      # (ref_char = 옛 칠 그림은 안전 필터에 걸려서 뺌, 2026-10-21)
SREFS = CREFS + ['ref_flat_bg.png']
SHOT = "- 1536x1024 LANDSCAPE, one full-frame picture WITH background (no transparency, no panels, no borders).\n"
SHEET = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
         "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses.\n")
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers.\n")
P = {}
# 들판 판 (넓은 하늘 · 먼 산 · 풀밭) · 하늘 판 (날아가는 근접 컷)
P['pano_on'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE') +
                "- A wide peaceful cartoon-adventure MEADOW seen from the side at ground level: a soft green grassy field with a cream dirt path across the lower 30% of the image (two characters will stand on it, left and right of center), "
                "a few flat round bushes and a lone tree at the far left edge, rolling blue-green HILLS and soft distant MOUNTAINS along the middle, and a HUGE open clear sky blue SKY filling the upper 55% of the image "
                "with only a few small flat puffy white clouds near the top right (something will fly away up into the top right sky). The center of the field is completely empty.\n", ['ref_flat_bg.png'])
P['bg_sky'] = (HEAD + BG.format(size='1536x1024 LANDSCAPE') +
               "- High up IN THE SKY, nothing but sky: a clear saturated sky blue background getting slightly deeper blue at the top, with several big flat puffy WHITE CLOUDS of different sizes at the edges and corners "
               "(the middle is open sky). No ground, no mountains, no birds.\n", ['ref_flat_bg.png'])
# ③ 고양이 감전 (셀 프레임, 같은 틀 — 1 ↔ 2 를 빠르게 번갈아) · 이걸 고양이 디자인 참고로 씀
P['anim_shock'] = (HEAD + SHEET + "- CAMERA: every frame is a SIDE VIEW, the cat faces LEFT (toward the hamster off-screen at the left), FULL BODY, the feet on the same ground line, the same size and position in every frame.\n" + STYLE + CAT + MAG +
                   "- The Coin Cat is being ELECTROCUTED by a huge electric attack coming from the left:\n"
                   "  1) frozen stiff in a starfish pose, arms and legs spread wide, every hair standing straight up in spikes, eyes wide open as white circles, mouth wide open screaming, its whole body tinted bright LEMON-YELLOW with a few flat yellow zigzag sparks around it;\n"
                   "  2) EXACTLY the same pose and outline as frame 1 but shown as a classic cartoon X-RAY: the body is a flat solid dark navy silhouette and a simple flat white cartoon SKELETON (round skull with the coin, ribs, arm and leg bones, tail bones) is visible inside, flat white zigzag sparks around;\n"
                   "  3) after the shock: standing limp and CRISPY, the fur BURNT to sooty dark grey-brown and frizzled into a big puffy afro of fur, the coin blackened, eyes as two dizzy swirls, mouth open coughing out a small flat grey puff of smoke, little smoke wisps rising from its head, knees wobbly.\n", CREFS)
P['anim_catfly'] = (HEAD + SHEET.replace('ONE full-body frame per column', 'ONE full-body frame per column (each frame may be rotated)') + STYLE + CAT + MAG +
                    "- Keep the Coin Cat EXACTLY as in the attached anim_shock frame 3 (burnt sooty grey-brown frizzled fur, blackened coin). It has been launched SKY-HIGH by a huge hit and is FLYING AWAY through the air like a cartoon villain blasting off:\n"
                    "  1) flying away diagonally upward, body stretched out flat like a superhero but out of control, limbs flailing, eyes squeezed shut, mouth open wailing, a tear flying off;\n"
                    "  2) TUMBLING head over heels, curled up, spinning, eyes as swirls, tongue out;\n"
                    "  3) seen from behind and a bit below, flying away spread-eagled like a starfish, waving one paw goodbye, getting tiny (the same size as the other frames in the sheet).\n", ['anim_shock.png'] + CREFS[:1] + ['ref_flat_anim.png'])
# ③④⑥ 짜릿햄쥐 셀 프레임
P['anim_volt'] = (HEAD + SHEET + "- CAMERA: every frame is a SIDE VIEW, the hamster faces RIGHT, FULL BODY, the feet on the same ground line.\n" + STYLE + CHAR + MAG +
                  "  1) CHARGING: crouched low on all fours, glaring forward with a fierce frown, fur bristling, cheeks puffed;\n"
                  "  2) RELEASING: rearing up on its hind legs, head thrown back, both arms flung wide, eyes shut tight, mouth open in a big shout (NO effects drawn, effects are added later);\n"
                  "  3) BLAST: leaning forward toward the right, mouth open in a mighty cry, both paws thrust forward to the right like casting a big spell, the battery on its back glowing (NO effects drawn, effects are added later).\n", CREFS + ['ref_flat_anim2.png'])
P['anim_tackle'] = (HEAD + SHEET + "- CAMERA: every frame is a SIDE VIEW, the hamster faces RIGHT, FULL BODY.\n" + STYLE + CHAR + MAG +
                    "  1) WIND-UP: crouched low like a sprinter on the starting line, back leg kicking up dust, determined eyes;\n"
                    "  2) BODY SLAM: launched horizontally like a cannonball to the RIGHT, the body stretched straight and flat, head and shoulder first, short flat white speed lines trailing behind it to the left;\n"
                    "  3) VICTORY: standing proudly on its hind legs facing the viewer in three-quarter view, a cheeky WINK with one eye, a big grin, one paw making a V sign near its face, the other paw on its hip.\n", CREFS + ['ref_flat_anim2.png'])
# ① 괴롭힘 컷 (편집) · ② 얼굴 클로즈업 (편집)
P['shot_tease1'] = (HEAD + SHOT + STYLE + CHAR + CAT +
                    "- Keep the Coin Cat EXACTLY like the attached anim_shock frame 1 design (but normal clean cream fur, normal colors, not electrocuted).\n"
                    "- SCENE, a MEDIUM SHOT on a sunny green meadow (simple flat hills and sky behind): the hamster sits peacefully on the grass in the lower LEFT-center, holding a big striped SUNFLOWER SEED with both paws and nibbling it happily, eyes closed. "
                    "The Coin Cat looms over it from the RIGHT, bending down with a smug sly grin, POKING the hamster's puffy cheek pouch with one claw-tip, the other paw on its own hip. The hamster's cheek squishes in. Both characters are big in the frame.\n", ['anim_shock.png'] + SREFS)
P['shot_tease2'] = (EDIT.format(src='shot_tease1.png', f='{f}', bgkeep=', the same background') +
                    "- The Coin Cat has SNATCHED the sunflower seed and holds it up high out of reach in one paw, and with the other paw it YANKS the hamster's lightning-bolt cardboard tail, laughing mockingly with its eyes closed and tongue out. "
                    "The hamster, still sitting in the same place with its paws now empty, glares sideways with a dark shadow over its eyes, a big flat angry vein mark on its hood, its cheek pouches puffed up in anger. Nothing else changes.\n", ['shot_tease1.png'])
P['shot_glare1'] = (HEAD + SHOT + STYLE + CHAR +
                    "- An EXTREME CLOSE-UP of the hamster's FACE filling the frame (the yellow hood with its brown-tipped ears at the top edges, the round red cheeks big at the left and right), looking straight at the viewer with a menacing glare: "
                    "a dark flat shadow band over the eyes, the small black eyes glinting, mouth a tight frown. Background behind the head: a flat deep navy with a few flat yellow speed lines radiating from the center. Dramatic, but cute.\n", SREFS)
P['shot_glare2'] = (EDIT.format(src='shot_glare1.png', f='{f}', bgkeep=', the same background') +
                    "- The eyes FLASH: both eyes become glowing flat WHITE with tiny pupils, and the two round red cheeks burst with crackling flat LEMON-YELLOW zigzag ELECTRIC sparks shooting outward, the hood ears spring straight up. Nothing else changes.\n", ['shot_glare1.png'])
# 이펙트 · 소품
P['fx_flat'] = (HEAD + FXS +
                "- Palette: lemon yellow, sunny yellow, white, a little orange.\n"
                "  1. a long thick flat lemon-yellow ZIGZAG LIGHTNING BOLT lying HORIZONTALLY (pointing to the right), with a white core line, sharp jagged corners\n"
                "  2. a big flat FORKED lightning bolt (one bolt splitting into three jagged branches), lemon yellow with a white core, vertical, pointing down\n"
                "  3. a small flat cluster of yellow electric SPARKS (short zigzags and little four-pointed sparkles)\n"
                "  4. a wide flat electric SHOCKWAVE RING (a thin yellow and white ring with small zigzags along it), seen straight on\n"
                "  5. a big flat cartoon HIT STAR burst (a white and yellow jagged starburst with an orange edge) for a body slam impact\n"
                "  6. a classic cartoon TWINKLE STAR: a flat white four-pointed star with long thin rays and a tiny yellow center, like the 'ding' twinkle when a villain disappears into the sky\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (HEAD + FXS +
               "- Palette: lemon yellow, sunny yellow, white, a little orange, soft grey smoke.\n"
               "  1. a thin flat lemon-yellow lightning bolt falling straight DOWN (long, vertical, the jagged tip at the bottom)\n"
               "  2. a small flat cluster of yellow electric sparks and tiny zigzags\n"
               "  3. a long thin flat white-yellow SPEED STREAK (tapered, horizontal)\n"
               "  4. a lightning STRIKE hit frame 1: a small flat white-yellow flash with short zigzag spikes on the ground\n"
               "  5. a lightning STRIKE hit frame 2: a bigger jagged yellow electric burst with zigzag arcs spreading out\n"
               "  6. a lightning STRIKE hit frame 3: fading yellow sparks and small flat grey smoke puffs spreading out\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['props_flat'] = (HEAD + FXS.replace('effect art', 'prop art') +
                   "  1. a big striped SUNFLOWER SEED (black and white stripes), flat cartoon\n"
                   "  2. a big flat puffy WHITE CLOUD (soft rounded shape, a light blue-grey underside)\n"
                   "  3. a long thin flat white CLOUD streak (horizontal, wispy, rounded ends)\n"
                   "  4. a flat cartoon SMOKE puff cluster (soft round dark grey and grey puffs)\n"
                   "  5. a flat cartoon DUST cloud (warm cream and light brown round puffs), wide and low\n"
                   "  6. a flat cartoon blue SWEAT DROP / tear\n", ['ref_flat_fx.png', 'ref_rig.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(b.replace('{f}', n + '.png'))
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', encoding='utf-8', newline='\n').write('''#!/usr/bin/env bash
# 짜릿햄쥐 초필살기 v2 그림 (백만볼트 · 또 당했다~) Codex — 고양이 감전 시트(디자인 기준) 먼저 → 괴롭힘 컷 · 날아가는 고양이 / 연속 프레임은 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/zapham/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pair() { run $1; shift; for n in "$@"; do run $n & done; wait; }
cat_chain() { run anim_shock; run anim_catfly & pair shot_tease1 shot_tease2 & wait; }
cat_chain &
pair shot_glare1 shot_glare2 &
for n in pano_on bg_sky anim_volt anim_tackle; do run $n & done
wait
for n in fx_flat fx_hit props_flat; do run $n & done; wait; echo DONE
''')
print(sorted(P))
