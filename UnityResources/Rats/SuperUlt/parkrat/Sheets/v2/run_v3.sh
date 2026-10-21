#!/usr/bin/env bash
# 쥐랜드 관광쥐 v3: 쥐 머리 풍선 → 평범한 풍선 (편집) · 표정 단계
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/parkrat/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for n in shot_parade1 shot_parade3 shot_think1 shot_wave_b1 shot_wave_b2 shot_wave_c1 shot_wave_c2; do [ -f old_$n.png ] || mv $n.png old_$n.png; done
(run shot_think1; run shot_think2) &
for n in shot_parade1 shot_parade3 shot_wave_b1 shot_wave_b2 shot_wave_c1 shot_wave_c2 balloon shot_crowd_q shot_crowd_worry shot_crowd_scream; do run $n & done; wait; echo DONE
