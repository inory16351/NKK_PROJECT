"""스트리머 쥐 초필살기 v2 프롬프트 (2026-10-20 사용자: 도네 받는 장면 → 고글 '이이잉' 리액션 + 트래킹 줌 → 제로투 느낌 전신 춤 · 게임 그림체 최대한)
저작권: 특정 캐릭터 외형 · 의상 · 음악 없이 '양팔을 굽혀 엉덩이를 좌우로 흔드는 춤 동작'만 오마주. 글자 · 로고 · 실제 플랫폼 화면 없음.
플랫 배경(로비 그림체) · 정면 · 게임 리그 파츠 기준 셀 프레임 · 플랫 이펙트 → UnityResources/Rats/SuperUlt/streamrat/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'streamrat', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(PH, 'anim_command.png'), 'ref_flat_anim2.png'), (os.path.join(R, 'SuperUlt', 'ref_zap_style.png'), 'ref_zap_style.png'),
                 (os.path.join(R, 'SuperUlt', 'streamrat', 'Sheets', 'ref_char.png'), 'ref_char.png'), (os.path.join(R, 'SuperUlt', 'streamrat', 'Sheets', 'ref_parts.png'), 'ref_parts.png')):
    shutil.copy(src, os.path.join(D, dst))
ims = [Image.open(os.path.join(R, 'Parts', 'streamrat', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
BG = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO text, NO letters, NO numbers, no logos, no real brand or platform UI.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: cozy night streamer room — deep navy and plum walls, neon pink and cyan accent lights, warm lamp yellow.\n")
V = "- This picture is ONE SEGMENT of a very TALL vertical panorama that a camera tilts through (top -> middle -> bottom). Segments are stacked vertically, so the composition must continue at the top and bottom edges.\n"
CONT = "- CONTINUITY IS CRITICAL: the attached ref_prev_bottom is the BOTTOM STRIP of the segment directly ABOVE this one. The TOP 30% of your picture must continue it seamlessly (same shapes at the same positions, same colors).\n"
SEG = {
    'a': '1 (TOP) of 3: the CEILING of a streamer room seen from the front: a long flat neon LED strip in pink and cyan, a string of round fairy lights, two small spotlights pointing down, a few flat sound-foam panels at the top of the wall',
    'b': '2 (MIDDLE) of 3: seen FROM THE FRONT, symmetric: the back WALL of a cozy streamer room: a big flat dark monitor screen in the center (EMPTY dark screen, nothing on it), smaller monitors at both sides, neon pink and cyan light bars, shelves with small round plush toys and a cheese-shaped lamp, a ring light at one side',
    'c': '3 (BOTTOM) of 3: seen FROM THE FRONT: the room FLOOR: a round soft rug in the center (EMPTY, a character will dance there), a gaming chair pushed aside at the left, cables and a small speaker at the right, warm floor glow from the neon',
}
STYLE = ("- STYLE (MOST IMPORTANT): this must look like THIS GAME's own art. Match EXACTLY the attached ref_rig (the character's in-game sprite parts: copy its colors, shapes and proportions) and the approved flat cutscene frames ref_flat_anim / ref_flat_anim2 / ref_zap_style: "
         "FLAT cartoon storybook look in the Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, small simple black bead eyes with one tiny white highlight, "
         "no black outlines, no gloss, NOT a Disney/Pixar/3D look, NOT anime style.\n- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- CHARACTER "Streamer Rat": build him EXACTLY from the attached ref_rig and ref_char / ref_parts: a cute brown MOUSE in a baggy olive-green HOODIE (hood up), big round pink ears poking out, '
        'big green-tinted swim GOGGLES on the face, a black gaming HEADSET with a cyan glowing ear cup and a small mic, a tiny camera on top of the hood, black fingerless gloves, pink paws, long pink tail. Big head, small body. '
        'NO water guns in these frames (hands are free).\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery, no effects. Do not use magenta or purple on the subjects. Keep everything fully inside the frame with margin.\n"
ANIM = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
        "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses.\n")
FRONT = "- CAMERA: every frame is a straight FRONT VIEW (the character faces the viewer / the camera).\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_flat_anim2.png']
P = {}
for k, d in SEG.items():
    P['pano_' + k] = (BG + V + (CONT if k != 'a' else '') + '- SEGMENT ' + d + '.\n', ['ref_flat_bg.png'] + (['ref_prev_bottom.png'] if k != 'a' else []))
P['face_bg'] = (BG + "- A FLAT cut-in backdrop for a dramatic close-up: big flat diagonal speed bands in neon pink, cyan and deep navy, a few flat round light dots, empty middle area where a face will be placed on top.\n", ['ref_flat_bg.png'])
P['face_close'] = ("- 1536x1024 LANDSCAPE extreme CLOSE-UP of the character's FACE straight from the FRONT, face filling most of the height, centered.\n" + STYLE + CHAR + MAG +
                   "- DRAMATIC GOGGLE GLINT reaction (classic cartoon 'serious glasses shine' gag): one paw pushing the goggles up the nose with one finger, BOTH goggle lenses turned completely flat bright WHITE (opaque shine, eyes hidden), "
                   "a diagonal flat white shine streak across each lens, a calm cocky smirk, the headset ear cup glowing cyan.\n", CREFS)
A = {
    'anim_donate': "- FRAMES (left to right), FRONT VIEW, standing: 1) bored: slouching, one paw scratching the head, half-asleep face; "
                   "2) SHOCKED: jumping up with arms flung out, mouth wide open, ears straight up, tail straight up (a big donation just came in); 3) ECSTATIC: both paws on the cheeks, sparkly happy face, knees together, little hop.",
    'anim_goggle': "- FRAMES (left to right), FRONT VIEW: 1) suddenly calm and serious: standing straight, one paw rising to the goggles; "
                   "2) pushing the goggles up the nose with one finger, the lenses turning flat bright WHITE (eyes hidden), smirk; 3) leaning toward the viewer with a cocky smirk, lenses flat WHITE with a diagonal shine streak, other paw on the hip.",
    'anim_dance': "- A cute meme hip-sway DANCE loop (homage to a popular meme dance move only, NOT any specific character, outfit or hairstyle): FULL BODY, FRONT VIEW. In EVERY frame BOTH paws are held BEHIND THE HEAD (hands clasped behind the hood, elbows pointing out wide to both sides, like a stretch), "
                  "and the dance is a big side-to-side HIP SWAY with the upper body and head tilting the opposite way, knees softly bent, feet planted shoulder-width:\n"
                  "  1) FAR LEFT: hips pushed far to the viewer's LEFT, upper body and head tilted to the viewer's RIGHT, right knee bent, playful wink, tail swinging left; "
                  "2) HALF LEFT: hips halfway to the left, body tilt smaller, happy open mouth; 3) CENTER: hips centered, both knees bent a little lower (a small dip), head straight, cheerful grin. Keep feet in the SAME place in all frames.",
    'anim_dance2': "- The SECOND half of the SAME hip-sway DANCE loop (homage only): FULL BODY, FRONT VIEW, BOTH paws held BEHIND THE HEAD with elbows pointing out wide in every frame, feet planted in the SAME place:\n"
                   "  1) HALF RIGHT: hips halfway to the viewer's RIGHT, upper body and head tilted a little to the viewer's LEFT, happy open mouth; 2) FAR RIGHT: hips pushed far to the viewer's RIGHT, upper body and head tilted to the viewer's LEFT, left knee bent, playful wink, tail swinging right; "
                   "3) CENTER rising: hips centered, legs straight, chin up, bright smile. Draw EXACTLY the same character, size and scale as the attached ref_dance_a.",
    'anim_finish': "- FRAMES (left to right), FRONT VIEW: 1) making a FINGER HEART at the viewer with a wink; 2) a cute pose with a peace sign next to the goggles, tongue out; 3) jumping up in joy, both fists up, tail curled.",
}
for n, b in A.items():
    P[n] = (ANIM + FRONT + STYLE + CHAR + MAG + b + "\n", CREFS + (['ref_dance_a.png'] if n == 'anim_dance2' else []))     # 춤 뒤 절반은 앞 절반과 같은 크기 · 자리로
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, purple or hot pink close to magenta on the elements (use coral pink, salmon, cyan, yellow, white instead).\n- NO text, NO letters, NO numbers.\n")
P['fx_flat'] = (FXS + "- Palette: coral pink, salmon, cyan, lemon yellow, cream white.\n"
                "  1. a single flat round HEART in coral pink with a small white highlight\n"
                "  2. a big flat HEART BURST: one big coral heart with 6 small hearts flying out around it\n"
                "  3. a flat MUSIC NOTE pair (two eighth notes joined) in cyan\n"
                "  4. a flat LENS GLINT: a white rounded diamond shine with two thin crossing white light bars (a cartoon glasses shine)\n"
                "  5. a flat NEON RING: a thin cyan ring with a coral inner ring\n"
                "  6. a flat cluster of CONFETTI: small rectangles and circles in coral, cyan and yellow\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (FXS + "- Palette: coral pink, salmon, cyan, lemon yellow, cream white.\n"
               "  1. a flat SPEED LINE streak (a horizontal tapered white and cyan streak)\n"
               "  2. a flat gold DONATION COIN with a little cheese wedge symbol embossed on it (no letters)\n"
               "  3. a small flat round sparkle dot in yellow\n"
               "  4. HEART POP frame 1: a small flat white burst with 3 tiny coral hearts\n"
               "  5. HEART POP frame 2: a bigger flat coral-and-white burst ring with 6 hearts and confetti flying out\n"
               "  6. HEART POP frame 3: the end: a wide thin broken coral ring and a few small hearts, fading shapes\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['props_flat'] = (FXS.replace("effect art", "prop art") +
                   "  1. a big flat computer MONITOR seen from the front showing a cartoon DONATION ALERT graphic: a big gold coin, a cheese wedge and a coral heart with cyan sparkles on a navy screen (icons only, NO letters, NO numbers)\n"
                   "  2. a flat stack of gold DONATION COINS\n"
                   "  3. a small flat wrapped GIFT BOX shaped like a cheese wedge with a coral ribbon\n"
                   "  4. a flat THUMBS-UP icon in a coral circle\n"
                   "  5. a flat notification BELL icon in yellow with a small cyan ring\n"
                   "  6. a flat cyan GLOW STICK\n", ['ref_flat_fx.png', 'ref_flat_bg.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + b)
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', newline='\n').write('''#!/usr/bin/env bash
# 스트리머 쥐 초필살기 v2 그림 (플랫 · 정면 · 도네 · 고글 리액션 · 전신 춤) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/streamrat/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pano() { run pano_a; python -c "from PIL import Image; im=Image.open('pano_a.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_b
  python -c "from PIL import Image; im=Image.open('pano_b.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_c; }
pano &
for n in face_bg face_close anim_donate anim_goggle anim_dance; do run $n & done; wait
for n in anim_finish fx_flat fx_hit props_flat; do run $n & done; wait
[ -f anim_dance.png ] && cp anim_dance.png ref_dance_a.png; run anim_dance2; echo DONE
''')
print(sorted(P))
