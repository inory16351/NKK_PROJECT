#!/usr/bin/env bash
# 슈퍼 요리사 쥐 초필살기 v2 그림 (라따뚜이 시식) Codex — 1번 프레임 · 배경 · 이펙트 동시 / 연속 프레임은 1번이 나온 뒤 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/starchef/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
taste() { run shot_taste1; run shot_taste2 & run shot_taste3 & wait; }
ride() { run shot_ride1; run shot_ride2 & run shot_ride3 & wait; }
vee() { run shot_v1; run shot_v2; }
taste & ride & vee &
for n in shot_offer cu_dish dish_bg face_bg; do run $n & done; wait
for n in fx_flat props_flat shot_ride0 cu_ratride pose_leap; do run $n & done; wait; echo DONE
