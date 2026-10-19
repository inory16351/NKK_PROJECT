#!/usr/bin/env bash
# 기사 쥐 초필살기 v3 그림 (대검 뽑기 · 차징 · 화면 가르기) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/knight/Sheets/v3; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in anim_pull anim_charge anim_cut altar fx_cut; do run $n & done; wait; echo DONE
