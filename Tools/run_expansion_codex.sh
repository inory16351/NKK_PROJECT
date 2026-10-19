#!/usr/bin/env bash
# 100층 확장 Codex 시트 (동시에 N개씩). 사용: bash Tools/run_expansion_codex.sh <동시수> [시트...]  (시트 생략 = 전부)
# 프롬프트 = UnityResources/Rats/Expansion100/Sheets/prompt_<시트>.txt (Tools/gen_expansion_prompts.py), 이미 있는 그림은 건너뜀. 로그 = log_<시트>.txt
cd "$(dirname "$0")/.."
D=UnityResources/Rats/Expansion100/Sheets
N=$1; shift
ALL="z1_catlab z2_dino z3_construction z4_ocean z5_war z6_space h1_catlab h2_dino h3_construction h4_ocean h5_war h6_space hb1 hb2 cb1 cb2 cb3 cb4 mp1 mp2 badges floors walls"
LIST=${*:-$ALL}
ref() {   # 시트 종류별 참고 그림
  case $1 in
    z*) echo "-i $D/ref_items.png" ;;
    h[0-9]*) echo "-i $D/ref_humans.png" ;;
    hb*) echo "-i $D/ref_boss_humans.png -i $D/ref_humans.png" ;;
    cb*) echo "-i $D/ref_boss_cats.png" ;;
    mp*) echo "-i $D/ref_props.png" ;;
    badges) echo "-i $D/ref_badges.png" ;;
    floors) echo "-i $D/ref_floor.png -i $D/ref_floor2.png" ;;
    walls) echo "-i $D/ref_wall.png" ;;
  esac
}
run() {
  local s=$1
  [ -f "$D/$s.png" ] && { echo "skip $s"; return; }
  bash Tools/codex.sh exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" $(ref $s) < "$D/prompt_$s.txt" > "$D/log_$s.txt" 2>&1
  [ -f "$D/$s.png" ] && echo "done $s" || echo "FAIL $s"
}
for s in $LIST; do
  while [ "$(jobs -rp | wc -l)" -ge "$N" ]; do sleep 3; done
  run $s &
done
wait
echo "ALL DONE"
