#!/usr/bin/env bash
# 줴리 초필살기 v2 그림 (톰과 줴리 추격전) Codex — 가로 집 파노라마 이어 그림 · 컷 한 장 + 연속 프레임 편집 · 셀 시트
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/jwerry/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
seed() { python -c "from PIL import Image; p=Image.open('$1.png').convert('RGB').resize((1536,1024)); c=Image.new('RGB',(1536,1024),(255,0,255)); c.paste(p.crop((1024,0,1536,1024)),(0,0)); c.save('$2')"; }
pano() { run pano_a; seed pano_a ref_seed_b.png; run pano_b; seed pano_b ref_seed_c.png; run pano_c; }
pair() { run $1; shift; for n in "$@"; do run $n & done; wait; }
chain() { for n in "$@"; do run $n; done; }
pano &
pair shot_open1 shot_open2 &
pair shot_pan1 shot_pan2 &
chain shot_door1 shot_door2 shot_door3 &
pair shot_rug1 shot_rug2 &
pair shot_wall1 shot_wall2 &
pair shot_hole1 shot_hole2 &
wait
for n in shot_catface anim_jrun anim_tomrun anim_jmove anim_tommove props_flat fx_flat fx_hit; do run $n & done; wait; echo DONE
