"""기사 쥐 v10 후경 샷 (2026-10-19 사용자: 후경 캐릭터 다시 · 캐릭터가 딛고 선 땅이 있는 풍경 · 그 사이로 땅이 갈라지는 이펙트) → knight/Sheets/v10"""
import os, shutil
R = r'C:\Project\NKK_PROJECT\UnityResources\Rats\SuperUlt\knight\Sheets'
D = os.path.join(R, 'v10'); os.makedirs(D, exist_ok=True)
for src, dst in (('v7/ref_knight_big.png', 'ref_knight_big.png'), ('v7/anim_excal.png', 'ref_knight_front.png'), ('v7/ref_rig.png', 'ref_rig.png'), ('v7/ref_char.png', 'ref_char.png'),
                 ('v7/ref_flat_bg.png', 'ref_flat_bg.png'), ('v7/ref_flat_fx.png', 'ref_flat_fx.png'), ('v7/fx_light.png', 'ref_light.png'), ('pano_c.png', 'ref_meadow.png'), ('pano_b.png', 'ref_hill.png')):
    shutil.copy(os.path.join(R, src), os.path.join(D, dst))
HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
P = {}
P['rear_knight'] = ("- 1024x1536 portrait image, ONE character, full body.\n"
    "- STYLE (MOST IMPORTANT): match EXACTLY the attached ref_knight_front and ref_knight_big (approved frames of this same knight): FLAT cartoon game art in the Untitled Goose Game mood, flat fills with at most one soft shade step, NO black outlines, NO gloss, NO fur strands, small black bead eyes, NOT 3D. No stars anywhere.\n"
    "- CHARACTER: EXACTLY the same \"Knight Rat\" (cute grey mouse in silver plate armor, helmet with open visor and big RED PLUME, red cape, pink tail) with the same ENORMOUS greatsword (broad silver blade with blue rune marks, gold crossguard with a blue gem).\n"
    "- POSE: FRONT VIEW facing the viewer, the moment right AFTER a huge downward swing: the enormous greatsword has been swung straight down in front of him and its blade tip is driven into the ground right in front of his feet (the blade points down toward the viewer and slightly forward), "
    "both paws still gripping the hilt at chest height, low wide powerful stance with knees bent, cape blown wide out to both sides by the shockwave, plume blown back, fierce determined face.\n"
    "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), NO ground, NO shadow, NO effects. Do not use magenta or purple on the character. Keep him fully inside the frame with margin.\n- NO text.\n",
    ['ref_knight_front.png', 'ref_knight_big.png', 'ref_rig.png', 'ref_char.png'])
P['rear_bg'] = ("- 1536x1024 LANDSCAPE, one full-frame background picture. NO characters, NO text, NO letters.\n"
    "- STYLE (MOST IMPORTANT): FLAT 2D game background exactly like the attached ref_flat_bg, ref_meadow and ref_hill (this game's approved flat backgrounds): big simple flat shapes, flat fills with at most one soft shade step, NO outlines, NO painterly texture.\n"
    "- SCENE: a LOW camera standing on a wide grassy MEADOW looking straight ahead: the ground plane stretches from the bottom edge of the picture (close to the camera, big simple grass patches and a few pebbles) back to the HORIZON at about 40% from the top, "
    "clear PERSPECTIVE; on the horizon a misty hill with the ring of standing stones and tattered banners (like ref_hill), a dramatic dusk sky with golden rays above. "
    "The CENTER of the ground (where a character will stand at about 55% from the top, and the strip of ground from there down to the bottom center) is EMPTY flat grass.\n",
    ['ref_flat_bg.png', 'ref_meadow.png', 'ref_hill.png'])
P['ground_crack'] = ("- 1024x1536 portrait image, ONE effect element.\n"
    "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_light and ref_flat_fx (approved flat effects of this game): flat shapes, at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO black outlines. ABSOLUTELY NO STAR SHAPES.\n"
    "- ELEMENT: a GROUND FISSURE seen in PERSPECTIVE from a low camera: a jagged crack in the earth that starts as a thin point at the TOP CENTER of the picture (far away) and runs straight DOWN toward the viewer, getting much WIDER at the bottom (close to the camera), "
    "the crack edges are broken dark brown earth and grass chunks lifted up on both sides, and from inside the crack a bright flat cream-white and gold LIGHT bursts upward in flat flame-like tongues. The whole element fills the full height.\n"
    "- Background: ONE solid flat pure magenta #FF00FF everywhere around the element, no shadow. Do not use magenta, pink or purple on the element.\n- NO text.\n",
    ['ref_light.png', 'ref_flat_fx.png', 'ref_meadow.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + b)
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v10.sh'), 'w', newline='\n').write('''#!/usr/bin/env bash
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/knight/Sheets/v10; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in rear_knight rear_bg ground_crack; do run $n & done; wait; echo DONE
''')
shutil.copy(__file__, os.path.join(D, 'gen_prompts.py'))
print('ok')
