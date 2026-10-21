#!/usr/bin/env bash
# 외계인 생쥐 초필살기 v3 그림 (인디펜던스 데이 패러디) Codex — 원본 → 편집 순서
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/alien/Sheets/v3; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
ship() { run ship; run ship2 & run ship_far & wait; }
hand() { run cu_hand1; run cu_hand2; }
desc() { run shot_descend; run shot_descend_dmg; }
jet() { run jet_back1; run jet_back2; }
ship & hand & desc & jet &
for n in bg_chat fg_wall anim_spy; do run $n & done; wait
for n in bg_hand bg_space bg_runway anim_cry fx_v3; do run $n & done; wait; echo DONE
