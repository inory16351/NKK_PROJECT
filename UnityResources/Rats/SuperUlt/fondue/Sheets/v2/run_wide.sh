#!/usr/bin/env bash
# 치즈 퐁듀 v2 롱샷: pano_on 을 가운데 1/3 크기로 두고 둘레를 이어 그림 (아웃페인팅)
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/fondue/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
run wide_on; run fountain_on; echo DONE
