#!/usr/bin/env bash
# 아이스 퀸 컷신 리그 파츠 (고해상도) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/icequeen/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in rig_torso rig_head rig_legs rig_tail rig_cape; do run $n & done; wait; echo DONE
