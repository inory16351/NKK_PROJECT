"""기사 쥐 v7 (2026-10-19 사용자: 세이버 엑스칼리버 정면샷처럼 · 별 모양 이펙트 없앰) → knight/Sheets/v7"""
import os, shutil
R = r'C:\Project\NKK_PROJECT\UnityResources\Rats\SuperUlt\knight\Sheets'
V6, D = os.path.join(R, 'v6'), os.path.join(R, 'v7')
os.makedirs(D, exist_ok=True)
for f in os.listdir(V6):
    if f.startswith('ref_'): shutil.copy(os.path.join(V6, f), os.path.join(D, f))
shutil.copy(os.path.join(V6, 'anim_swingdown.png'), os.path.join(D, 'ref_knight_swing.png'))
HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
base = open(os.path.join(R, 'v4', 'prompt_anim_cut.txt'), encoding='utf-8').read()
i = base.index('- FRAMES (left to right)')
anim = base[:i].replace('anim_cut.png', 'anim_excal.png').replace(
    "ref_knight_anim (approved frames of this same knight)", "ref_knight_anim, ref_knight_big and ref_knight_swing (approved frames of this same knight with this same enormous greatsword)")
anim += ("- FRONT VIEW, the knight faces the VIEWER head-on, centered in each column, seen from a slightly LOW camera angle, heroic like a legendary sword-release shot. FRAMES (left to right): "
         "1) the enormous greatsword held HIGH and FAR BACK BEHIND his head with both paws (blade pointing up and back over his head), chest out, feet planted wide, cape billowing out to both sides, the blade glowing with flat gold shapes; "
         "2) mid-swing: the greatsword coming over the top of his head and down TOWARD THE VIEWER, the blade strongly foreshortened, both arms forward, a flat gold motion arc above; "
         "3) the greatsword swung all the way DOWN IN FRONT of him toward the viewer, blade pointing down-forward at the camera, deep stance, cape flaring out, determined shout.\n"
         "- NO stars, NO star-shaped sparkles anywhere.\n")
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered, well inside its cell, none touching.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_knight_fx and ref_flat_fx (approved flat effects of this game): flat shapes, at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO black outlines. "
       "Palette: holy gold, cream white, pale blue.\n- ABSOLUTELY NO STAR SHAPES, NO spiky starbursts, NO diamond sparkles, NO cross-shaped twinkles: only round, soft, flowing and ray shapes.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere, no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers.\n")
fx = (FXS +
      "  1. a big round FLAT LIGHT FLASH: 3-4 concentric flat circles from cream-white in the middle to soft gold outside, smooth round edge, no spikes\n"
      "  2. a FRONTAL LIGHT BEAM seen head-on: a big round burst of flat straight light rays and bands radiating from a bright round center toward the viewer, like a beam coming at the camera (rays are long tapered bands, not star points)\n"
      "  3. a tall vertical GOLDEN LIGHT COLUMN rising up from the bottom, made of flat cream and gold vertical bands, soft rounded top\n"
      "  4. a cluster of 8 small round flat LIGHT DOTS (circles of different sizes) in gold and cream\n"
      "  5. a flat LIGHT RING in perspective: a wide thin gold ellipse ring seen slightly from above\n"
      "  6. a wide horizontal flat band of LIGHT with soft wavy edges, cream core and gold edges\n")
P = {'anim_excal': (anim, ['ref_knight_big.png', 'ref_knight_swing.png', 'ref_knight_anim.png', 'ref_rig.png']),
     'fx_light': (fx, ['ref_knight_fx.png', 'ref_flat_fx.png'])}
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + b if not b.startswith('Use your') else b)
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v7.sh'), 'w', newline='\n').write('''#!/usr/bin/env bash
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/knight/Sheets/v7; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in anim_excal fx_light; do run $n & done; wait; echo DONE
''')
shutil.copy(__file__, os.path.join(D, 'gen_prompts.py'))
print(open(os.path.join(D, 'prompt_anim_excal.txt'), encoding='utf-8').read()[:260])
