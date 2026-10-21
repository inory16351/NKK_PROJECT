#!/usr/bin/env bash
# 명탐정 쥐 초필살기 v2 그림 (범인은 바로 너야!) Codex — 컷 한 장 + 연속 프레임 편집 · 추격 골목 판 · 고양이 셀
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/jjdetective/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pair() { run $1; shift; for n in "$@"; do run $n & done; wait; }
pair shot_think1 shot_think2 &
pair shot_culprit1 shot_culprit2 &
pair shot_aim1 shot_aim2 &
pair shot_win1 shot_win2 &
for n in shot_clue1 shot_clue2 shot_clue3 shot_clue4; do run $n & done; wait
for n in shot_point pano_on anim_catrun anim_catko props_flat fx_flat fx_hit; do run $n & done; wait; echo DONE
