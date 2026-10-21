"""폭탄마 쥐 초필살기 v2 프롬프트 (2026-10-20 사용자: 빨간 마법진을 여러 겹 쌓아 올림 → 위에서부터 익스플로전 레이저 → 큰 폭발 → 버섯구름)
플랫 배경(로비 그림체) · 정면 · 리그 파츠 기준 셀 프레임 · 플랫 이펙트 → UnityResources/Rats/SuperUlt/bomber/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'bomber', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
IQ = os.path.join(R, 'SuperUlt', 'icequeen', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
PH = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(PH, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png'), 'ref_flat_fx2.png'),
                 (os.path.join(PH, 'anim_summon.png'), 'ref_flat_anim.png'), (os.path.join(R, 'SuperUlt', 'ref_zap_style.png'), 'ref_zap_style.png'),
                 (os.path.join(R, 'SuperUlt', 'bomber', 'Sheets', 'ref_char.png'), 'ref_char.png'), (os.path.join(R, 'SuperUlt', 'bomber', 'Sheets', 'ref_parts.png'), 'ref_parts.png')):
    shutil.copy(src, os.path.join(D, dst))
ims = [Image.open(os.path.join(R, 'Parts', 'bomber', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
BG = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, no tiny clutter. Palette: dramatic dusk — deep crimson, dusty plum, burnt orange, charcoal rock, warm ember accents.\n")
V = "- This picture is ONE SEGMENT of a very TALL vertical panorama that a camera tilts through (top -> middle -> bottom). Segments are stacked vertically, so the composition must continue at the top and bottom edges.\n"
CONT = "- CONTINUITY IS CRITICAL: the attached ref_prev_bottom is the BOTTOM STRIP of the segment directly ABOVE this one. The TOP 30% of your picture must continue it seamlessly (same shapes at the same positions, same colors).\n"
SEG = {
    'a': '1 (TOP) of 3: a wide open DUSK SKY in deep crimson and plum with a few long flat cloud bands, the CENTER of the sky is EMPTY and open (magic circles will be stacked there); at the very bottom the sky turns burnt orange near the horizon',
    'b': '2 (MIDDLE) of 3: seen FROM THE FRONT, a far flat HORIZON of a barren wasteland: low flat mesas and a few distant rocky spires at the left and right edges, the CENTER of the horizon is a wide EMPTY flat plain (a huge explosion will happen there), warm orange haze bands',
    'c': '3 (BOTTOM) of 3: seen FROM THE FRONT, the near ground: the flat edge of a rocky CLIFF / plateau top in charcoal and rust colors where a character will stand in the lower-middle (EMPTY there), a few small rocks and dry grass tufts at the sides',
}
STYLE = ("- STYLE (MOST IMPORTANT): match EXACTLY the attached ref_zap_style and ref_flat_anim (approved flat cutscene art of this game) and ref_char: FLAT cartoon storybook look in the Untitled Goose Game mood: "
         "simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, small simple black bead eyes with one tiny white highlight, "
         "no black outlines, no gloss, NOT a Disney/Pixar/3D look.\n- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- CHARACTER "Bomber Rat": build him EXACTLY from the attached ref_rig (the game\'s own rig parts) and ref_char / ref_parts: a cute dark-grey MOUSE with scruffy fur, big pink ears, '
        'a black EYE PATCH with a red cross over one eye, a big toothy grin, a dark vest with a brown bandolier full of small olive-and-yellow bomb shells, two round black bombs with fuses on the back, pink paws, long pink tail with a leather band. Big head, small body.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery, no effects. Do not use magenta or purple on the subjects. Keep everything fully inside the frame with margin.\n"
ANIM = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
        "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses.\n")
FRONT = "- CAMERA: every frame is a straight FRONT VIEW (the character faces the viewer / the camera), like a dramatic anime front shot.\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_zap_style.png', 'ref_flat_anim.png']
P = {}
for k, d in SEG.items():
    P['pano_' + k] = (BG + V + (CONT if k != 'a' else '') + '- SEGMENT ' + d + '.\n', ['ref_flat_bg.png'] + (['ref_prev_bottom.png'] if k != 'a' else []))
P['face_bg'] = (BG + "- A dramatic FLAT cut-in backdrop for a close-up: big flat radial bands in crimson, orange and dark plum spreading from the center, a big thin flat red ring behind the middle, empty middle area where a face will be placed on top. No stars.\n", ['ref_flat_bg.png'])
P['face_close'] = ("- 1536x1024 LANDSCAPE extreme CLOSE-UP of the character's FACE straight from the FRONT for a skill cut-in, face filling most of the height, centered. Keep the simple bead eye.\n" + STYLE + CHAR + MAG +
                   "- The visible eye shines with a small flat RED glint, a huge maniac-happy toothy grin, ears pinned back by wind, a lit fuse spark near the cheek.\n", CREFS)
A = {
    'anim_cast': "- FRAMES (left to right), FRONT VIEW: 1) feet apart, arms crossed over the bandolier, smug grin, eye closed; "
                 "2) one paw thrust STRAIGHT UP to the sky (fingers spread), the other on the hip, fur and ears lifted by wind, grin; 3) BOTH paws raised high above the head in a V toward the sky, chest out, mouth open shouting, ears and tail blown up.",
    'anim_fire': "- FRAMES (left to right), FRONT VIEW: 1) both paws stretched FORWARD at the viewer with palms open (casting a big blast), knees bent, bracing; "
                 "2) RECOIL: leaning back hard with paws still forward, feet sliding, ears and fur blown back, eye squeezed, teeth clenched in a grin; 3) aftermath: standing proud with a big THUMBS-UP toward the viewer, slightly singed fur with a few flat soot smudges, grin.",
}
for n, b in A.items():
    P[n] = (ANIM + FRONT + STYLE + CHAR + MAG + b + "\n", CREFS)
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers, NO stars.\n")
P['fx_flat'] = (FXS + "- Palette: crimson red, scarlet, warm orange, cream white core. These are RED MAGIC CIRCLES for an explosion spell.\n"
                "  1. a big RED MAGIC CIRCLE seen straight from the FRONT (perfect circle): thick outer ring, a second inner ring, simple abstract geometric marks between them (dots, short bars, triangles; NOT letters), a hexagon inside, a small solid disk in the middle\n"
                "  2. the SAME magic circle seen in PERSPECTIVE from below (a flat wide ELLIPSE, about 3 times wider than tall)\n"
                "  3. a thin outer RUNE RING alone: one thin red ring with evenly spaced small geometric marks (no letters), perfect circle\n"
                "  4. a small simple red magic circle (two rings and a triangle), perfect circle\n"
                "  5. a flat round RED FLASH disk with a cream-white center and short rounded rays\n"
                "  6. a flat cluster of 6 small red-orange EMBER sparks (rounded diamonds)\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
STRIP = ("- 1536x1024 landscape image: 3 columns, ONE element per column, centered in its third, well inside it, none touching each other or the edges.\n" + FXS.split('\n', 1)[1])
P['beam'] = (STRIP + "- Palette: crimson, scarlet, orange, cream white core.\n"
             "  1. a very TALL straight VERTICAL LASER BEAM (fills almost the full height): a thick flat beam with a cream-white core, an orange band and a red outer band, straight parallel edges, slightly flared at the bottom\n"
             "  2. a thinner tall vertical BEAM CORE line: cream white with thin orange edges (fills almost the full height)\n"
             "  3. a big flat IMPACT FLASH where the beam hits the ground: a wide dome-shaped cream-white and orange burst with rounded flame tongues, seen from the front\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['mushroom'] = (STRIP + "- Palette: cream white, warm yellow, orange, crimson, ash grey, charcoal smoke.\n"
                 "- These are 3 GROWTH STAGES of one huge cartoon MUSHROOM CLOUD explosion seen from the front (flat cartoon, like a classic cartoon explosion, NOT realistic, no destruction, no people), all drawn at the SAME scale with the base at the same bottom line:\n"
                 "  1. a big round FIREBALL just lifting off the ground on a short thick column of fire, a wide flat dust skirt at the base\n"
                 "  2. the fireball rising on a taller stem and starting to roll into a mushroom CAP (round puffy layers in orange and yellow, darker smoke at the edges), a ring of smoke around the stem\n"
                 "  3. the FULL MUSHROOM CLOUD: a tall stem and a huge wide rounded puffy cap (yellow-orange core, red and ash-grey outer puffs), a flat smoke ring around the stem, a wide dust skirt at the ground\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['boom'] = (FXS + "- Palette: cream white, warm yellow, orange, crimson, charcoal smoke.\n"
             "  1. EXPLOSION frame 1: a small flat white-yellow burst with rounded flame tongues\n"
             "  2. EXPLOSION frame 2: a bigger round orange-yellow fireball burst with a few rock chunks flying out\n"
             "  3. EXPLOSION frame 3: the end: a puffy grey-orange smoke cloud with small embers, fading shapes\n"
             "  4. a wide flat SHOCKWAVE RING seen from the front (a thin wide ellipse of cream-white and orange, about 4 times wider than tall)\n"
             "  5. a flat cluster of flying ROCK DEBRIS chunks in charcoal and rust\n"
             "  6. a big flat puffy SMOKE PUFF in ash grey and charcoal\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + b)
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', newline='\n').write('''#!/usr/bin/env bash
# 폭탄마 쥐 초필살기 v2 그림 (플랫 · 정면 · 마법진 · 레이저 · 버섯구름) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/bomber/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pano() { run pano_a; python -c "from PIL import Image; im=Image.open('pano_a.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_b
  python -c "from PIL import Image; im=Image.open('pano_b.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_c; }
pano &
for n in face_bg face_close anim_cast anim_fire fx_flat; do run $n & done; wait
for n in beam mushroom boom; do run $n & done; wait; echo DONE
''')
print(sorted(P))
