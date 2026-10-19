#!/usr/bin/env bash
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/knight/Sheets/v14; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; shift; [ -f "$n.png" ] && return 0; args=(); for r in "$@"; do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
cp ref_open1.png ref_open_a.png
run open1 guide_open1.png ref_open1.png ref_flamewall.png ref_blast.png &
run open2 guide_open2.png ref_open2.png ref_flamewall.png ref_blast.png &
run open3 guide_open3.png ref_open3.png ref_flamewall.png ref_blast.png &
wait; echo DONE
