"""기사 쥐 초필살기 v3 프롬프트 (사용자 2026-10-19: 돌 제단의 대검을 뽑아 → 차징 → 위에서 아래로 화면을 가르듯 크게 베기 → 일직선 파동 초토화)
→ UnityResources/Rats/SuperUlt/knight/Sheets/v3 (참고 그림은 v2 것)"""
import os, shutil

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats\SuperUlt\knight\Sheets'
V2, D = os.path.join(R, 'v2'), os.path.join(R, 'v3')
os.makedirs(D, exist_ok=True)
for f in ('ref_flat_bg.png', 'ref_flat_fx.png', 'ref_flat_fx2.png', 'ref_flat_anim.png', 'ref_zap_style.png', 'ref_char.png', 'ref_rig.png', 'anim_raise.png'):
    shutil.copy(os.path.join(V2, f), os.path.join(D, f if f != 'anim_raise.png' else 'ref_knight_anim.png'))
shutil.copy(os.path.join(V2, 'fx_hit.png'), os.path.join(D, 'ref_knight_fx.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
SWORD = ("a GIANT GREATSWORD much bigger than the knight (about 1.6 times his height): a broad straight silver blade with a gold crossguard, a long gold-wrapped grip, "
         "a round blue gem in the guard and simple flat blue rune marks on the blade")
STYLE = ("- STYLE (MOST IMPORTANT): match EXACTLY the attached ref_knight_anim (approved frames of this same knight), ref_flat_anim and ref_zap_style: FLAT cartoon storybook look in the Untitled Goose Game mood: "
         "simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO fur strands, small simple black bead eyes, no black outlines, no gloss, NOT 3D.\n"
         "- NO text, NO letters, NO numbers, no logos, no speech bubbles.\n")
CHAR = ('- CHARACTER "Knight Rat": EXACTLY the same knight as ref_knight_anim / ref_rig / ref_char: cute grey MOUSE in silver plate armor, helmet with open visor and a big RED PLUME, red cape, pink tail. '
        f'In this sheet he wields {SWORD}.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery. Do not use magenta or purple on the subject. Keep everything fully inside the frame with margin.\n"
ANIM = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column (the greatsword included), all frames the same scale, each well inside its third, none touching. "
        "Consecutive key drawings of one motion for a cel-animated skill cutscene: keep the character and the greatsword IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses.\n")
CREFS = ['ref_knight_anim.png', 'ref_rig.png', 'ref_char.png', 'ref_flat_anim.png']
P = {}
P['anim_pull'] = (ANIM + STYLE + CHAR + MAG +
    "- FRAMES (left to right): 1) standing on top of a small grey stone block, both paws gripping the hilt of the greatsword whose blade is stuck deep DOWN into the stone, leaning back and straining hard, eyes squeezed shut; "
    "2) YANKING the greatsword half out of the stone, body arched back, stone chips flying, mouth open shouting; "
    "3) holding the freed greatsword HIGH above his head with both paws in triumph, cape flaring, big proud grin (no stone in this frame).\n", CREFS)
P['anim_charge'] = (ANIM + STYLE + CHAR + MAG +
    "- FRAMES (left to right), side view facing RIGHT: 1) greatsword swung back over his shoulder, wide stance, determined glare; "
    "2) CHARGING ENERGY: deep crouch, the greatsword held low behind, his whole body and the blade outlined by flat gold-white energy shapes, cape and plume blown upward; "
    "3) LEAPING high into the air with the greatsword raised straight above his head with both paws, body stretched, cape streaming down.\n", CREFS)
P['anim_cut'] = (ANIM + STYLE + CHAR + MAG +
    "- FRAMES (left to right), the giant downward DIAGONAL CUT from top-right to bottom-left: 1) mid-air at the top, greatsword above and behind his head, about to slash down; "
    "2) mid-slash, body twisting, the greatsword diagonal in front of him with a long flat white-gold motion arc; "
    "3) landed in a low wide stance, the greatsword swung all the way down to the lower-left with its tip near the ground, a big flat white-gold slash arc trailing, cape whipping.\n", CREFS)
P['altar'] = ("- 1536x1024 image, 3 columns x 1 row: ONE object per column, centered, well inside it, none touching.\n"
    "- STYLE (MOST IMPORTANT): FLAT 2D game art exactly like the attached ref_flat_bg and ref_flat_fx: big simple flat shapes, flat fills with at most one soft shade step, clean edges, NO outlines, NO texture, NO glow.\n" + MAG +
    "  1. an ancient grey STONE ALTAR: a chunky rock pedestal with two worn steps and a simple carved round-table emblem, a narrow SLOT on top where a blade was (EMPTY, no sword), a little moss\n"
    f"  2. the {SWORD}, alone, vertical, pointing straight DOWN\n"
    "  3. a group of 6 broken grey STONE CHUNKS and pebbles flying (debris)\n", ['ref_flat_bg.png', 'ref_flat_fx.png'])
P['fx_cut'] = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered, well inside its cell, none touching.\n"
    "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_knight_fx and ref_flat_fx (approved flat effects of this game): flat shapes, at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO black outlines. "
    "Palette: holy gold, cream white, pale blue, plus a warm orange only for the explosions.\n"
    "- Background: ONE solid flat pure magenta #FF00FF everywhere, no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers.\n"
    "  1. a GIANT straight SLASH LINE: an extremely long thin horizontal blade-cut line across the whole cell width, a cream-white core with gold edges, sharply tapered at both ends (like the screen was cut)\n"
    "  2. an ENERGY CHARGE AURA: a round flat swirl of gold and pale blue energy flames rising around an empty center\n"
    "  3. a flat cluster of gold ENERGY SPARKS converging (small diamonds and streaks pointing to the center)\n"
    "  4. EXPLOSION frame 1: a flat white-gold burst just starting, with a few rock chips\n"
    "  5. EXPLOSION frame 2: a big flat explosion cloud of gold, cream and warm orange with flying rock chips and a flat shock ring\n"
    "  6. EXPLOSION frame 3: the explosion fading into a few flat grey smoke puffs and falling chips\n", ['ref_knight_fx.png', 'ref_flat_fx.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + b)
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v3.sh'), 'w', newline='\n').write('''#!/usr/bin/env bash
# 기사 쥐 초필살기 v3 그림 (대검 뽑기 · 차징 · 화면 가르기) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/knight/Sheets/v3; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in anim_pull anim_charge anim_cut altar fx_cut; do run $n & done; wait; echo DONE
''')
shutil.copy(__file__, os.path.join(D, 'gen_prompts.py'))
print(sorted(P))
