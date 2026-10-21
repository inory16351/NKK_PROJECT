#!/usr/bin/env bash
# 증강 UI 그림 (슬롯 테두리 · 빈/잠긴 슬롯 · 관리 창 판 · HUD 선반) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/Augment/Sheets/ui; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in aug_slots aug_panel aug_shelf; do run $n & done; wait; echo DONE
