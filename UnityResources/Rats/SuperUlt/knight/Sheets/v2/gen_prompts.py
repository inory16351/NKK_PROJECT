"""기사 쥐 초필살기 v2 프롬프트 (플랫 배경 · 리그 파츠 기준 셀 프레임 · 플랫 이펙트) → UnityResources/Rats/SuperUlt/knight/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'knight', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
IQ = os.path.join(R, 'SuperUlt', 'icequeen', 'Sheets', 'v2')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(IQ, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(IQ, 'crack.png'), 'ref_flat_fx2.png'),
                 (os.path.join(IQ, 'anim_skate.png'), 'ref_flat_anim.png'), (os.path.join(R, 'SuperUlt', 'ref_zap_style.png'), 'ref_zap_style.png'),
                 (os.path.join(R, 'SuperUlt', 'knight', 'Sheets', 'ref_char.png'), 'ref_char.png'), (os.path.join(R, 'SuperUlt', 'knight', 'Sheets', 'ref_parts.png'), 'ref_parts.png')):
    shutil.copy(src, os.path.join(D, dst))
# 리그 파츠 한 장 (게임 파츠 그대로)
ims = [Image.open(os.path.join(R, 'Parts', 'knight', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
BG = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO text, NO letters, NO numbers, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, NOT glowing fantasy concept art, no tiny clutter. Palette: dusk sky in soft indigo, teal and warm gold, misty blue-greens, grey stone.\n")
V = "- This picture is ONE SEGMENT of a very TALL vertical panorama that a camera tilts through (top -> middle -> bottom). Segments are stacked vertically, so the composition must continue at the top and bottom edges.\n"
CONT = "- CONTINUITY IS CRITICAL: the attached ref_prev_bottom is the BOTTOM STRIP of the segment directly ABOVE this one. The TOP 30% of your picture must continue it seamlessly (same shapes at the same positions, same colors).\n"
SEG = {
    'a': '1 (TOP) of 3: a dramatic DUSK SKY: big flat layered clouds with a round opening in the middle where flat golden rays fall down, a few flat stars; at the very bottom the tops of distant hills start',
    'b': '2 (MIDDLE) of 3: a misty HILLTOP with an ancient ring of standing stones (a round table of stone), tattered flat knight BANNERS on poles around it, rolling misty hills and a far castle silhouette',
    'c': '3 (BOTTOM) of 3: a wide grassy MEADOW below the hill covered with low flat mist bands, a few small rocks and an old sword stuck in a rock at the side; the lower-middle meadow ground is EMPTY and clear (a character will stand there)',
}
STYLE = ("- STYLE (MOST IMPORTANT): match EXACTLY the attached ref_zap_style and ref_flat_anim (approved flat cutscene art of this game) and ref_char: FLAT cartoon storybook look in the Untitled Goose Game mood: "
         "simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, small simple black bead eyes with one tiny white highlight, "
         "no black outlines, no gloss, NOT a Disney/Pixar/3D look.\n- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
CHAR = ('- CHARACTER "Knight Rat": build him EXACTLY from the attached ref_rig (the game\'s own rig parts: grey mouse head in a silver knight helmet with an open visor and a big RED PLUME, silver armored torso, '
        'armored legs with pink feet, long pink tail) and ref_char / ref_parts: a cute grey MOUSE in shiny SILVER plate armor, a red cape, a straight silver SWORD with a gold hilt. Big head, small body.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery, no effects. Do not use magenta or purple on the subject. Keep everything fully inside the frame with margin.\n"
ANIM = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
        "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses (anticipation, action, follow-through), cape exaggerated.\n")
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_zap_style.png', 'ref_flat_anim.png']
P = {}
for k, d in SEG.items():
    P['pano_' + k] = (BG + V + (CONT if k != 'a' else '') + '- SEGMENT ' + d + '.\n', ['ref_flat_bg.png'] + (['ref_prev_bottom.png'] if k != 'a' else []))
P['face_bg'] = (BG + "- A dramatic FLAT cut-in backdrop for a close-up: big flat diagonal light bands in gold and white radiating from the right-center over a deep indigo, a few flat star sparkles, empty middle area where a face will be placed on top.\n", ['ref_flat_bg.png'])
P['face_close'] = ("- 1536x1024 LANDSCAPE CLOSE-UP of the character's head and shoulders for a skill cut-in, face filling most of the height in the right-center. Keep the simple bead eyes.\n" + STYLE + CHAR + MAG +
                   "- The helmet visor is up, the face is determined and brave with a small confident smile, one eye with a tiny star glint, the red plume blowing sideways, the gold sword hilt held up near the face.\n", CREFS)
A = {
    'anim_raise': "- FRAMES (left to right): 1) KNEELING on one knee, head bowed, both paws resting on the sword hilt with the blade planted in the ground; 2) RISING, pulling the sword up out of the ground, looking up, cape lifting; 3) standing tall with the SWORD RAISED STRAIGHT UP to the sky with both paws, chest out, cape and plume blowing up.",
    'anim_swing': "- FRAMES (left to right), side view facing RIGHT: 1) JUMP WIND-UP: leaping up with the sword held far back behind the head with both paws, body arched; 2) mid-air at the top, sword above the head about to come down, cape flaring; 3) the big DOWNWARD SWING follow-through: landed low, sword swung down in front with a flat white motion arc behind it.",
    'anim_slash': "- FRAMES (left to right), side view facing RIGHT: 1) horizontal slash wind-up, sword pulled back to the left side, body twisted, low stance; 2) mid HORIZONTAL SLASH with a wide flat white crescent motion arc; 3) follow-through, sword extended to the right, cape whipping, determined face.",
    'anim_ghost': "- This sheet shows a GHOST KNIGHT RAT (the same knight design but as a flat pale BLUE spirit, only blue and white tones, no red, glowing white eyes) riding a pale blue GHOST HORSE, side view facing RIGHT, 3-frame GALLOP cycle: 1) horse front legs reaching forward, lance pointed forward; 2) all legs gathered under the body; 3) hind legs pushing back, mane and spirit wisps streaming back.",
}
for n, b in A.items():
    P[n] = (ANIM + STYLE + CHAR + MAG + b + "\n", CREFS)
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers.\n")
P['fx_flat'] = (FXS + "- Palette: holy gold, cream white, pale spirit blue.\n"
                "  1. a GIANT GHOST SWORD pointing straight DOWN (vertical, long), flat pale spirit-blue blade with a white edge and a gold hilt, a few flat wisps\n"
                "  2. a vertical HOLY LIGHT PILLAR: a tall straight beam made of flat gold and cream-white stripes, slightly wider at the bottom\n"
                "  3. a big SLASH ARC: a wide flat crescent sword-trail shape in cream white with a gold inner edge\n"
                "  4. a small cluster of 5 flat LIGHT MOTES (little gold diamond and circle sparkles)\n"
                "  5. a big flat STARBURST flash: a gold and white spiky star with 8-10 sharp points\n"
                "  6. a flat RING of light: a thin gold ring with 4 small diamond sparkles on it\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (FXS + "- Palette: holy gold, cream white, pale spirit blue.\n"
               "  1. a long straight diagonal SWORD SLASH streak across the cell (from lower-left to upper-right): a very thin flat cream-white blade-cut line with gold edges, tapered at both ends\n"
               "  2. a horizontal HOLY LIGHT WAVE: a wide flat crescent wave of gold and white light moving to the RIGHT, curved front edge on the right, tapered trailing tail on the left\n"
               "  3. a single small LIGHT MOTE: a flat gold diamond sparkle\n"
               "  4. SLASH IMPACT frame 1: a small flat white X-shaped cut flash with 3 tiny gold sparks\n"
               "  5. SLASH IMPACT frame 2: the impact spreading: a bigger flat white-gold X cut and a small flat ring, 6 sparks flying out\n"
               "  6. SLASH IMPACT frame 3: the end: a wide thin broken ring and a few small sparks, fading shapes\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['props_flat'] = (FXS.replace("effect art", "prop art") +
                   "  1. a ROUND TABLE SEATING CHART paper: a sheet with a big circle and small circles around it and scribble lines (no letters)\n  2. a tattered GHOST BANNER on a pole, pale blue\n  3. a round knight SHIELD with a cheese emblem\n"
                   "  4. a broken jousting LANCE\n  5. a pale blue GHOST HELMET floating\n  6. a long horizontal strip of mossy STONE ledge (foreground edge)\n", ['ref_flat_fx.png', 'ref_flat_bg.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + b)
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', newline='\n').write('''#!/usr/bin/env bash
# 기사 쥐 초필살기 v2 그림 (플랫 · 리그 파츠 기준 셀 프레임) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/knight/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pano() { run pano_a; python -c "from PIL import Image; im=Image.open('pano_a.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_b
  python -c "from PIL import Image; im=Image.open('pano_b.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_c; }
pano &
for n in face_bg face_close anim_raise anim_swing; do run $n & done; wait
for n in anim_slash anim_ghost fx_flat fx_hit props_flat; do run $n & done; wait; echo DONE
''')
print(sorted(P))
