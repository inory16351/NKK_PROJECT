#!/usr/bin/env bash
# 외계인 생쥐 초필살기 v2 그림 (모선 강림 · 레이저 · 쥐 도시 폐허) Codex — 파노라마 a→b→c 이어 그림 + 폐허 편집 · 연속 프레임 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/alien/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
bottom() { python -c "from PIL import Image; im=Image.open('$1.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; }
pano() { run pano_a; run pano_a_alt & bottom pano_a; run pano_b; run pano_b_alt & bottom pano_b; run pano_c; run pano_c_alt; wait; }
ufo() { run ufo1; run ufo2; }
face() { run cu_face1; run cu_face2 & run cu_face3 & wait; }
pano & ufo & face &
for n in face_bg anim_end; do run $n & done; wait
for n in beam boom fx_flat; do run $n & done; wait; echo DONE
