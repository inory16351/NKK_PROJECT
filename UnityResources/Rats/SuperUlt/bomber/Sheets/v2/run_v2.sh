#!/usr/bin/env bash
# 폭탄마 쥐 초필살기 v2 그림 (플랫 · 정면 · 마법진 · 레이저 · 버섯구름) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/bomber/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pano() { run pano_a; python -c "from PIL import Image; im=Image.open('pano_a.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_b
  python -c "from PIL import Image; im=Image.open('pano_b.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_c; }
pano &
for n in face_bg face_close anim_cast anim_fire fx_flat; do run $n & done; wait
for n in beam mushroom boom; do run $n & done; wait; echo DONE
