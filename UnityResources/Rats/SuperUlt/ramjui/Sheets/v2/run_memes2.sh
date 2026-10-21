#!/usr/bin/env bash
# 람쥐썬더 짤 몽타주 2차: 그림체 4단계 (게임 → 반실사 → 3D 실사풍 → 사진 짤) 16장 Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/ramjui/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
set -- bg_mA1 bg_mA2 bg_mA3 bg_mA4 bg_mA5 bg_mB1 bg_mB2 bg_mB3 bg_mB4 bg_mC1 bg_mC2 bg_mC3 bg_meme15 bg_meme16 bg_meme17 bg_meme18
for n in "${@:1:8}"; do run $n & done; wait
for n in "${@:9}"; do run $n & done; wait; echo DONE
