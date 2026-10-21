"""람쥐썬더 짤 몽타주 3차 (2026-10-20 사용자: 좀 더 천천히 단계적으로 · 시작 · 끝은 좋은데 중간 반실사 단계가 부족)
2차(gen_memes2.py) 그림체 단계 사이를 메우는 중간 단계:
  A+ = 게임 플랫에 부드러운 음영 · 옅은 털 질감 (A 와 B 사이) → bg_mP1~2
  B+ = 반실사 디지털 페인팅: 실제 다람쥐 비율 · 털을 그림으로 촘촘히 · 아직 '그림' (B 와 C 사이) → bg_mQ1~3
  C+ = 거의 사진인 포토리얼 CG (C 와 D 사이) → bg_mR1~2"""
import os

D = os.path.dirname(os.path.abspath(__file__))
HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated image. After saving, reply only with the file name.\n\nImage spec:\n")
COMMON = ("- 1536x1024 LANDSCAPE, one full-frame picture with its background, no borders, no panels.\n"
          "- It is one frame of a funny 'thunder squirrel' meme montage where the art style gradually turns from a flat cartoon into a real photo: a squirrel with dramatic LIGHTNING / electric arcs.\n"
          "- NO text, NO letters, NO numbers, NO watermark, NO logos, NO speech bubbles. No humans. No real superhero costume or logo.\n")
ST = {
    'P': ("- STYLE (MOST IMPORTANT): ONE SMALL STEP away from the attached flat game art (ref_flat_anim, ref_mA1): keep the same cute cartoon character design, big head and bead eyes from ref_rig / ref_char, "
          "but add SOFT GRADIENT SHADING, gentle rim light, and a LIGHT hint of fur texture on the edges and tail. Still clearly a cartoon, no outlines. Lightning slightly glowing.\n",
          ['ref_rig.png', 'ref_char.png', 'ref_flat_anim.png', 'ref_mA1.png']),
    'Q': ("- STYLE (MOST IMPORTANT): halfway between a cartoon and reality: a SEMI-REALISTIC DIGITAL PAINTING (concept-art illustration). Real squirrel anatomy and proportions "
          "(normal-size head, real squirrel eyes but a bit large and expressive), dense PAINTED fur strokes, painterly background, clearly still a painting, not a photo. "
          "An orange-red squirrel with tufted ears and cream belly like the attached ref_mB1, painted lightning with glow.\n",
          ['ref_mB1.png']),
    'R': ("- STYLE (MOST IMPORTANT): almost a real photo: a PHOTOREALISTIC CGI render indistinguishable at a glance from a wildlife photograph — real fur, real eyes, natural camera depth of field — "
          "only the lightning is a clean, slightly too-perfect CG effect (no amateur photoshop yet). Same subject look as the attached ref_mC1 but more real.\n",
          ['ref_mC1.png']),
}
M = {
    'bg_mP1': ('P', "the squirrel standing upright on a park lawn with both paws raised high, mouth open shouting, lightning bolts striking around it"),
    'bg_mP2': ('P', "the squirrel spinning in mid-air in a heroic jump with lightning swirling around its fluffy tail, stormy sky"),
    'bg_mQ1': ('Q', "the squirrel standing on a tree stump with one paw raised to the sky calling down a lightning bolt, dark clouds, rain"),
    'bg_mQ2': ('Q', "the squirrel holding a glowing electrified acorn like a trophy on a mossy rock, sparks dripping from it"),
    'bg_mQ3': ('Q', "three squirrels standing upright on a fence rail, all charged with electricity, fur standing on end, lightning behind"),
    'bg_mR1': ('R', "the squirrel standing upright on a forest path facing the camera, both paws forward with electric arcs crackling between them, moody light"),
    'bg_mR2': ('R', "the squirrel on a rooftop edge at night during a storm, a huge lightning bolt striking the city behind it, rain droplets in its fur"),
}
for n, (k, b) in M.items():
    st, refs = ST[k]
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + COMMON + st + '- SCENE: ' + b + '.\n')
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_memes3.sh'), 'w', newline='\n').write(f'''#!/usr/bin/env bash
# 람쥐썬더 짤 몽타주 3차: 중간 그림체 단계 7장 Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/ramjui/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
for r in mA1 mB1 mC1; do cp bg_$r.png ref_$r.png; done
run() {{ n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${{args[@]}}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && {{ echo "$(date +%T) ok $n"; return; }}; done; echo "$(date +%T) FAIL $n"; }}
for n in {' '.join(sorted(M))}; do run $n & done; wait; echo DONE
''')
print(sorted(M))
