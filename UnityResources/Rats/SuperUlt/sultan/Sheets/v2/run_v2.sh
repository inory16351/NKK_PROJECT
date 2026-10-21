#!/usr/bin/env bash
# 술탄 쥐 초필살기 v2 그림 (오일머니 금화 비) Codex — 1차: 원본 · 배경 · 프레임 · 이펙트 / 2차: 연속 프레임 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/sultan/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
oil() { run shot_oil1; run shot_oil2 & run shot_oil3 & wait; }
face() { run cu_face1; run cu_face2; }
oil & face &
for n in face_bg shot_rise anim_pour pour pano_on anim_ride; do run $n & done; wait
for n in fx_flat fx_hit; do run $n & done; wait; echo DONE
