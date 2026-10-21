#!/usr/bin/env bash
# 쥐랜드 관광쥐 초필살기 v2 그림 (풍선 펑) Codex — 컷 한 장 + 연속 프레임 편집 · 광장 → 롱샷 아웃페인팅 · 폐허 편집
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/parkrat/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
seed() { python -c "from PIL import Image; p=Image.open('pano_on.png').convert('RGB').resize((1536,1024)); s=0.42; w,h=int(1536*s),int(1024*s); c=Image.new('RGB',(1536,1024),(255,0,255)); c.paste(p.resize((w,h)),((1536-w)//2,(1024-h)//2)); c.save('ref_wide_seed.png')"; }
plaza() { run pano_on; seed; run wide_on & run pano_alt & wait; }
pair() { run $1; shift; for n in "$@"; do run $n & done; wait; }
plaza &
pair shot_think1 shot_think2 &
pair shot_crowd1 shot_crowd1b shot_crowd2 &
pair shot_hand1 shot_hand2 &
for n in shot_parade1 shot_parade2 shot_parade3 anim_pump anim_end; do run $n & done; wait
pair shot_wave_a1 shot_wave_a2 & pair shot_wave_b1 shot_wave_b2 & pair shot_wave_c1 shot_wave_c2 &
for n in props_flat fx_flat pop; do run $n & done; wait; echo DONE
