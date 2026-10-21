#!/usr/bin/env bash
# 슈퍼 요리사 v2 올라타기 분리 (사람 판 · 쥐 판 · 날아오르는 쥐)
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/starchef/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in shot_ride0 cu_ratride pose_leap; do run $n & done; wait; echo DONE
