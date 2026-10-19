#!/usr/bin/env bash
# 아이스 퀸 컷신 동작 프레임 (리그 파츠 기준) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/icequeen/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" -i ref_rig.png -i ref_pose_a.png -i ref_pose_b.png -i ref_char.png < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in anim_land anim_skate anim_spin anim_dash anim_twirl; do run $n & done; wait; echo DONE
