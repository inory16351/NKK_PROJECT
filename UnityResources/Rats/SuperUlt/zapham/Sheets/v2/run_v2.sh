#!/usr/bin/env bash
# 짜릿햄쥐 초필살기 v2 그림 (백만볼트 · 또 당했다~) Codex — 고양이 감전 시트(디자인 기준) 먼저 → 괴롭힘 컷 · 날아가는 고양이 / 연속 프레임은 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/zapham/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pair() { run $1; shift; for n in "$@"; do run $n & done; wait; }
cat_chain() { run anim_shock; run anim_catfly & pair shot_tease1 shot_tease2 & wait; }
cat_chain &
pair shot_glare1 shot_glare2 &
for n in pano_on bg_sky anim_volt anim_tackle; do run $n & done
wait
for n in fx_flat fx_hit props_flat; do run $n & done; wait; echo DONE
