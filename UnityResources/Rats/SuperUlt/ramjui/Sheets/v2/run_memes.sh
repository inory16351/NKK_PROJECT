#!/usr/bin/env bash
# ¶÷Áã½ã´õ Â© ¸ùÅ¸ÁÖ ±×¸² 14Àå (Codex, Âü°í ±×¸² ¾øÀ½)
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/ramjui/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for i in 01 02 03 04 05 06 07; do run bg_meme$i & done; wait
for i in 08 09 10 11 12 13 14; do run bg_meme$i & done; wait; echo DONE
