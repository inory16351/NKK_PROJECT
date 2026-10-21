#!/usr/bin/env bash
# 쥐 황제 초필살기 v2 그림 (총병 앞으로 · 장전 · 격발) Codex — 1번 프레임 · 배경 · 이펙트 동시 / 연속 프레임은 1번이 나온 뒤 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/emperor/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
emp() { run cu_emp1; run cu_emp2; }
load() { run cu_load1; run cu_load2; }
emp & load &
for n in bg_field face_bg cu_aim anim_soldier; do run $n & done; wait
for n in fx_flat fx_hit; do run $n & done; wait
for n in bg_front anim_front cu_aimfront cu_empfront fx_front; do run $n & done; wait; echo DONE
