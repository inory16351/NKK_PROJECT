#!/usr/bin/env bash
# 치즈 퐁듀 쥐 초필살기 v2 그림 (트레비 치즈 분수) Codex — 1차: 원본 프레임 · 배경 · 이펙트 / 2차: 클로즈업 연속 프레임 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/fondue/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
snap() { run cu_snap1; run cu_snap2 & run cu_snap3 & wait; }
face() { run cu_face1; run cu_face2 & run cu_face3 & run cu_face4 & wait; }
snap & face &
for n in pano_on face_bg body_up pose_top; do run $n & done; wait
for n in fx_flat fx_hit geyser; do run $n & done; wait; echo DONE
