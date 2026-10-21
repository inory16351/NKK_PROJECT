#!/usr/bin/env bash
# ¶÷Áã½ã´õ Â© ¸ùÅ¸ÁÖ 3Â÷: Áß°£ ±×¸²Ã¼ ´Ü°è 7Àå Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/ramjui/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
for r in mA1 mB1 mC1; do cp bg_$r.png ref_$r.png; done
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in bg_mP1 bg_mP2 bg_mQ1 bg_mQ2 bg_mQ3 bg_mR1 bg_mR2; do run $n & done; wait; echo DONE
