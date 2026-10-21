"""메이드 쥐 초필살기 '메이드의 세계' 프롬프트 (2026-10-20 사용자: 필살기·초필살기 = 시간 정지 + 칼 모티브)
폭탄마 v2 와 같은 틀: 플랫 배경(로비 그림체) 세로 파노라마 · 정면 · 리그 파츠 기준 셀 프레임 · 플랫 이펙트 → UnityResources/Rats/SuperUlt/maid/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'maid', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(R, 'SuperUlt', 'ref_zap_style.png'), 'ref_zap_style.png'),
                 (os.path.join(R, 'NewRats4', '_메이드쥐_리그_미리보기.png'), 'ref_char.png'), (os.path.join(R, 'NewRats4', 'Sheets', 'ref_maid_concept.png'), 'ref_concept.png')):
    shutil.copy(src, os.path.join(D, dst))
ims = [Image.open(os.path.join(R, 'Parts', 'maid', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
BG = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: elegant moonlit night - deep navy blue, midnight indigo, crimson-red mansion walls, silver moonlight, warm gold clock accents.\n")
V = "- This picture is ONE SEGMENT of a very TALL vertical panorama that a camera tilts through (top -> middle -> bottom). Segments are stacked vertically, so the composition must continue at the top and bottom edges.\n"
CONT = "- CONTINUITY IS CRITICAL: the attached ref_prev_bottom is the BOTTOM STRIP of the segment directly ABOVE this one. The TOP 30% of your picture must continue it seamlessly (same shapes at the same positions, same colors).\n"
SEG = {
    'a': '1 (TOP) of 3: a wide NIGHT SKY in deep navy and indigo with a HUGE pale silver-cream FULL MOON slightly left of center and a few long flat cloud bands; the right part of the sky is open and empty (rings of knives will appear there); at the very bottom the pointed roof tips of a gothic mansion clock tower begin',
    'b': '2 (MIDDLE) of 3: seen FROM THE FRONT, a crimson-red gothic MANSION with a tall CLOCK TOWER in the exact center: on the tower a BIG round clock face (cream face, gold rim, 12 bold dark tick marks, two dark clock hands, NO numbers), dark navy roofs, rows of tall lit windows in warm yellow, symmetrical wings to the left and right',
    'c': '3 (BOTTOM) of 3: seen FROM THE FRONT, the near ground: a wide flat stone BALCONY / terrace of the mansion with a pale stone balustrade along the bottom edge, a flat crimson carpet runner in the middle, two small round topiary bushes at the far left and right; the CENTER of the terrace is EMPTY (a character will stand there)',
}
STYLE = ("- STYLE (MOST IMPORTANT): match EXACTLY the attached ref_zap_style and ref_flat_anim (approved flat cutscene art of this game) and ref_char: FLAT cartoon storybook look in the Untitled Goose Game mood: "
         "simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, small simple black bead eyes with one tiny white highlight, "
         "no black outlines, no gloss, NOT a Disney/Pixar/3D look, NOT anime.\n- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ("- CHARACTER \"Maid Rat\": build her EXACTLY from the attached ref_rig (the game's own rig parts) and ref_char: a cute cream-WHITE RAT with big round pink ears, pink nose, small black bead eyes, "
        "a white frilly maid headdress with a small round GOLD clock ornament in its center, one thin white braid with a small green ribbon bow, a royal-blue maid dress with a white frilly apron and a small green bow at the collar, "
        "a HUGE round GOLD POCKET WATCH (cream face, dark hands, no numbers) hanging from her waist on a gold chain, pink paws, a long pink tail with a royal-blue bow and a tiny gold clock-hand charm. Big head, small body. "
        "The attached ref_concept is ONLY for the mood (silver-haired head maid who stops time and throws knives); do NOT copy its anime look.\n")
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery, no effects. Do not use magenta or purple on the subjects. Keep everything fully inside the frame with margin.\n"
ANIM = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
        "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses. The character stands upright on her hind legs like a cartoon character.\n")
FRONT = "- CAMERA: every frame is a straight FRONT VIEW (the character faces the viewer / the camera), like a dramatic anime front shot.\n"
KNIFE = "small silver throwing knives (slim pointed silver blade, tiny dark guard, short royal-blue handle with a small ring at the end)"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_zap_style.png', 'ref_flat_anim.png', 'ref_concept.png']
FXREFS = ['ref_flat_fx.png', 'ref_flat_fx2.png']
P = {}
for k, d in SEG.items():
    P['pano_' + k] = (BG + V + (CONT if k != 'a' else '') + '- SEGMENT ' + d + '.\n', ['ref_flat_bg.png'] + (['ref_prev_bottom.png'] if k != 'a' else []))
P['face_bg'] = (BG + "- A dramatic FLAT cut-in backdrop for a close-up: big flat radial bands in navy, indigo and silver spreading from the center, behind the middle a HUGE flat gold CLOCK FACE ring with 12 bold tick marks (no numbers) and two long dark clock hands, the middle area is empty where a face will be placed on top. No stars.\n", ['ref_flat_bg.png'])
P['face_close'] = ("- 1536x1024 LANDSCAPE extreme CLOSE-UP of the character's FACE straight from the FRONT for a skill cut-in, face filling most of the height, centered. Keep the simple bead eyes.\n" + STYLE + CHAR + MAG +
                   "- Calm, cool, composed expression with a tiny confident smile; the eyes shine with a small flat RED glint (her time-stop power); the gold clock ornament of the headdress is clearly visible; "
                   f"one paw raised near the cheek holding three {KNIFE} fanned between the fingers.\n", CREFS)
A = {
    'anim_watch': "- FRAMES (left to right), FRONT VIEW: 1) standing elegantly, both paws holding the big gold pocket watch in front of the chest, eyes closed, calm; "
                  "2) one paw lifts the open pocket watch high beside the head, the thumb pressing its crown, eyes open and sharp; "
                  "3) the other paw thrust toward the viewer SNAPPING its fingers (a small flat white snap mark), the watch still held high, dress and apron swept by a sudden wind.",
    'anim_throw': f"- FRAMES (left to right), FRONT VIEW: 1) both arms crossed in front of the chest, each paw holding a fan of four {KNIFE} between the fingers, knees slightly bent, cool look; "
                  "2) both arms flung WIDE open to the sides, the knives just released and flying outward, apron and ears blown back, eyes glinting; "
                  "3) ending pose: an elegant maid CURTSY bow, holding the skirt hem with both paws, eyes closed, a small satisfied smile, the pocket watch swinging at her waist.",
}
for n, b in A.items():
    P[n] = (ANIM + FRONT + STYLE + CHAR + MAG + b + "\n", CREFS)
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers, NO stars.\n")
PAL = "- Palette: silver, pale steel grey, cream white, royal blue handles, warm gold.\n"
P['fx_flat'] = (FXS + PAL +
                "  1. a BIG flat gold CLOCK FACE seen straight from the front WITHOUT clock hands: cream face, thick gold rim, 12 bold dark tick marks (no numbers), a small gold crown knob on top\n"
                "  2. ONE throwing knife seen in strong PERSPECTIVE flying STRAIGHT AT THE VIEWER: the sharp silver blade tip big and close in the center, the guard and blue handle small behind it (foreshortened, almost a diamond shape)\n"
                "  3. ONE silver throwing knife lying perfectly HORIZONTAL, tip pointing RIGHT, long and thin, blue handle with a small ring at the left end\n"
                "  4. a perfect RING of 12 silver throwing knives seen from the front, evenly spaced, all tips pointing INWARD to the empty center (like a clock dial made of knives)\n"
                "  5. a flat round SILVER-WHITE FLASH disk with a cream-white center and short rounded rays\n"
                "  6. a flat cluster of 7 small round silver-white GLINT dots of different sizes (no stars)\n", FXREFS)
P['fx_hit'] = (FXS + PAL +
               "  1. ONE silver throwing knife falling STRAIGHT DOWN, tip pointing DOWN, with two short flat white speed streaks above it\n"
               "  2. TWO silver throwing knives falling down side by side, tips pointing DOWN, slightly different heights\n"
               "  3. a small flat cluster of round silver glints and 3 tiny metal sparks (rounded diamonds)\n"
               "  4. KNIFE IMPACT frame 1: a knife just stabbing into the ground with a small flat cream-white burst around the tip\n"
               "  5. KNIFE IMPACT frame 2: the same knife stuck in the ground with a bigger flat silver-white burst and a few flat ground chips flying\n"
               "  6. KNIFE IMPACT frame 3: the same knife stuck in the ground, the burst fading into small round puffs\n"
               "- Frames 4-6 must be drawn at the SAME scale and the same position inside their cells (the knife stays at the same spot).\n", FXREFS)
P['props_flat'] = (FXS.replace('effect art', 'prop art') + "- Palette: warm gold, cream, silver, royal blue, white, a little green.\n"
                   "  1. a HUGE round GOLD POCKET WATCH seen from the front with its lid open: cream face, two dark hands, 12 bold dark tick marks (no numbers), gold crown knob and a short gold chain\n"
                   "  2. a big flat gold clockwork GEAR (cog wheel) with 12 teeth and a round hole in the center\n"
                   "  3. a white porcelain teacup with a royal-blue rim on a saucer, a flat pale steam curl\n"
                   "  4. a flat bundle of five silver throwing knives held together with a small royal-blue ribbon\n"
                   "  5. a cream paper TIME CARD (office punch card) with a small gold clock symbol in its corner and a few plain lines (NO letters, NO numbers), slightly bent\n"
                   "  6. a small white frilly maid headdress with a gold clock ornament\n", FXREFS)
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + b)
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', newline='\n').write(r'''#!/usr/bin/env bash
# 메이드 쥐 초필살기 v2 그림 (플랫 · 정면 · 시계탑 저택 · 칼) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/maid/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pano() { run pano_a; python -c "from PIL import Image; im=Image.open('pano_a.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_b
  python -c "from PIL import Image; im=Image.open('pano_b.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_c; }
pano &
for n in face_bg face_close anim_watch anim_throw; do run $n & done; wait
for n in fx_flat fx_hit props_flat; do run $n & done; wait; echo DONE
''')
print(sorted(P))
