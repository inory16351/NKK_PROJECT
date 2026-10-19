#!/usr/bin/env bash
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/knight/Sheets/v12; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" -i ref_blast.png -i ref_frame.png -i ref_light.png -i ref_shot.png < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in fx_smear fx_flamewall; do run $n & done; wait; echo DONE
