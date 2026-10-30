#!/usr/bin/env bash
# 줴리 초필살기 v3 그림 (무성영화 앵콜) Codex — 가로 카페 파노라마 이어 그림 · 컷 한 장 + 연속 프레임 편집 · 셀 시트 · 소품 · 이펙트 · 필름
D=/c/Project/NKK3/NKK_PROJECT/UnityResources/Rats/SuperUlt/jwerry/Sheets/v3; CX=/c/Project/NKK3/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
seed() { python -c "from PIL import Image; p=Image.open('$1.png').convert('RGB').resize((1536,1024)); c=Image.new('RGB',(1536,1024),(255,0,255)); c.paste(p.crop((1024,0,1536,1024)),(0,0)); c.save('$2')"; }
pano() { run pano_a; seed pano_a ref_seed_b.png; run pano_b; seed pano_b ref_seed_c.png; run pano_c; }
pair() { run $1; shift; for n in "$@"; do run $n & done; wait; }
pano &
pair shot_open1 shot_open2 &
pair shot_trip1 shot_trip2 &
pair shot_pie1 shot_pie2 &
pair shot_stage1 shot_stage2 &
pair shot_bow1 shot_bow2 &
run shot_catface &
run shot_card &
wait
pair anim_crun anim_cmove &
run anim_mrun &
run anim_mmove &
run props_flat &
run fx_flat &
run fx_hit &
run film &
wait
echo "$(date +%T) done"
