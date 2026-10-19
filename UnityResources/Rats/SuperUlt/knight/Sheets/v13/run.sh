#!/usr/bin/env bash
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/knight/Sheets/v13; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; shift; [ -f "$n.png" ] && return 0; args=(); for r in "$@"; do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
run rear_knight ref_rear_knight.png ref_knight_front.png ref_rig.png ref_char.png &
run rear_bg ref_rear_bg.png ref_flat_bg.png ref_meadow.png &
wait; echo DONE
