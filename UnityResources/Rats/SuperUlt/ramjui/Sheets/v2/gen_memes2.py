"""람쥐썬더 짤 몽타주 2차 (2026-10-20 사용자: 초반엔 우리 게임 그림체 → 빨라질수록 실사 짤 · 그림체가 점점 실사에 가까워지게 · 그림 더 뽑기)
그림체 4단계 (같은 '번개 다람쥐' 짤 구도를 단계마다 다르게):
  A = 게임 플랫 그림체 (ref_rig · ref_char · ref_flat_anim · ref_flat_bg) → bg_mA1~5
  B = 반실사: 털 질감 있는 동화책 유화풍 → bg_mB1~4
  C = 거의 실사: 고급 3D 애니메이션 영화 렌더 (실감 나는 털) → bg_mC1~3
  D = 사진 합성 짤 (1차 bg_meme01~14 + 새로 bg_meme15~18)
저작권: 실제 짤 · 히어로 의상 · 로고 · 글자 없음 (Codex 재현)."""
import os

D = os.path.dirname(os.path.abspath(__file__))
HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated image. After saving, reply only with the file name.\n\nImage spec:\n")
COMMON = ("- 1536x1024 LANDSCAPE, one full-frame picture with its background, no borders, no panels.\n"
          "- It is one frame of a funny 'thunder squirrel' meme montage: a squirrel with dramatic LIGHTNING / electric arcs.\n"
          "- NO text, NO letters, NO numbers, NO watermark, NO logos, NO speech bubbles. No humans. No real superhero costume or logo.\n")
CHAR = ('- CHARACTER: the game\'s "Ramjui the Thunder Squirrel": build it EXACTLY from the attached ref_rig and ref_char (colors, shapes, proportions): '
        'a cute chubby orange-red squirrel with tall tufted ears (pink inside), cream belly and muzzle, small brown nose, a HUGE fluffy curled tail. Big head, small body.\n')
ST = {
    'A': ("- STYLE (MOST IMPORTANT): EXACTLY this game's own FLAT art: match the attached ref_flat_anim (character frames) and ref_flat_bg (background): "
          "Untitled Goose Game mood, simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, "
          "small simple black bead eyes with one tiny white highlight, no black outlines, no gloss. The lightning is also FLAT (flat zig-zag shapes in pale cyan, white and lemon yellow).\n" + CHAR,
          ['ref_rig.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_flat_bg.png']),
    'B': ("- STYLE (MOST IMPORTANT): a step MORE REALISTIC than a flat cartoon: a detailed children's storybook PAINTING, soft painterly brush strokes, "
          "visible fluffy fur texture, natural light and shadow, a still slightly cute cartoon squirrel (bigger eyes than real, but real squirrel anatomy), painted glowing lightning.\n"
          "- The squirrel is the same orange-red squirrel with tufted ears and cream belly as the attached ref_char, but painted semi-realistically.\n",
          ['ref_char.png']),
    'C': ("- STYLE (MOST IMPORTANT): ALMOST REALISTIC: a high-end 3D animated feature film render: physically realistic fur with individual hairs, real squirrel anatomy and proportions, "
          "cinematic lighting, depth of field, volumetric glow from realistic lightning. Only slightly stylized.\n"
          "- An orange-red squirrel with tufted ears and a cream belly.\n", []),
    'D': ("- STYLE (MOST IMPORTANT): a funny 2000s Korean internet MEME image: a REAL PHOTOGRAPH of a wild squirrel with deliberately cheesy, amateur photoshopped "
          "LIGHTNING effects pasted on top (bright white-blue electric arcs with soft glow), slightly over-sharpened, old JPEG compression feel, like a meme passed around web forums.\n", []),
}
M = {
    # A 게임 그림체 (몽타주 첫머리, 천천히)
    'bg_mA1': ('A', "the classic shot: the squirrel standing UPRIGHT on its hind legs on a flat green park lawn, tiny paws at its chest, surprised face, flat zig-zag lightning bolts crackling all around it on the grass"),
    'bg_mA2': ('A', "the squirrel on a flat round rock with BOTH front paws thrust forward at the viewer, thick flat lightning beams shooting from its paws, flat stormy navy sky"),
    'bg_mA3': ('A', "the squirrel sitting on a flat oak branch holding up a big ACORN with both paws, a flat lightning bolt striking the acorn which glows, flat dusk sky"),
    'bg_mA4': ('A', "the squirrel on a flat wooden park bench staring at the viewer with flat glowing RED laser beams shooting from both eyes (laser-eyes meme), flat storm clouds"),
    'bg_mA5': ('A', "the squirrel LEAPING across the frame between two flat trees, body stretched, a long flat lightning trail behind it like a comet, flat speed lines"),
    # B 반실사 동화책 유화
    'bg_mB1': ('B', "the squirrel standing upright on a sunny lawn with arms raised high, painted lightning bolts striking around it"),
    'bg_mB2': ('B', "close-up of the squirrel's face, eyes glowing electric blue, little painted electric arcs between its whiskers and ear tufts"),
    'bg_mB3': ('B', "the squirrel balancing on an old electric power line, fur standing up, painted sparks fizzing around it, blue sky"),
    'bg_mB4': ('B', "four squirrels standing upright in a row on a lawn, each struck by its own painted lightning bolt, dramatic dark sky"),
    # C 거의 실사 3D 렌더
    'bg_mC1': ('C', "the squirrel in a 'superhero landing' pose on a park path: one knee down, one fist on the cracked pavement, sparks and electric arcs around, dust"),
    'bg_mC2': ('C', "the squirrel crouched heroically on a mossy log with a tiny round silver viking-style helmet with small white wings and a short red cloth cape, holding a tiny hammer whose head is a big acorn, lightning in the dark sky"),
    'bg_mC3': ('C', "the squirrel gliding toward the camera with all four limbs spread wide, electric arcs crackling along its outstretched arms, forest background"),
    # D 사진 합성 짤 (추가)
    'bg_meme15': ('D', "a squirrel sitting on an old beige computer keyboard on a desk, staring at the camera, with photoshopped electric sparks bursting from the keyboard"),
    'bg_meme16': ('D', "a squirrel 'surfing' on top of a giant photoshopped lightning bolt across a stormy sky, arms out for balance, very silly"),
    'bg_meme17': ('D', "a squirrel standing on the hood of a parked car in a rainstorm, arms raised, a photoshopped lightning strike hitting the ground right behind it"),
    'bg_meme18': ('D', "a squirrel looking up in shock, its whole tail fur puffed out and frizzed from electricity, small smoke puffs, photoshopped sparks"),
}
for n, (k, b) in M.items():
    st, refs = ST[k]
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + COMMON + st + '- SCENE: ' + b + '.\n')
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
names = ' '.join(sorted(M))
open(os.path.join(D, 'run_memes2.sh'), 'w', newline='\n').write(f'''#!/usr/bin/env bash
# 람쥐썬더 짤 몽타주 2차: 그림체 4단계 (게임 → 반실사 → 3D 실사풍 → 사진 짤) 16장 Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/ramjui/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() {{ n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${{args[@]}}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && {{ echo "$(date +%T) ok $n"; return; }}; done; echo "$(date +%T) FAIL $n"; }}
set -- {names}
for n in "${{@:1:8}}"; do run $n & done; wait
for n in "${{@:9}}"; do run $n & done; wait; echo DONE
''')
print(sorted(M))
