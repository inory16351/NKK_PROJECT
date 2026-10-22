#!/usr/bin/env bash
# 보스 개편 Codex 시트 (동시에 N개씩). 사용: bash Tools/run_boss_rework_codex.sh <동시수> [시트...]  (시트 생략 = 전부)
# 프롬프트 = UnityResources/Rats/BossRework/Sheets/prompt_<시트>.txt (Tools/gen_boss_rework_prompts.py), 이미 있는 그림은 건너뜀. 로그 = log_<시트>.txt
cd "$(dirname "$0")/.."
D=UnityResources/Rats/BossRework/Sheets
E=UnityResources/Rats/Expansion100/Sheets
R=$(pwd -W)
N=$1; shift
ALL="hr1 hr2 hr3 cr1 cr2 cr3 cr4 cx1 cx2 cx3 tt1 pr1 rt1 rt2"
LIST=${*:-$ALL}
ref() {   # 시트 종류별 참고 그림
  case $1 in
    hr1) echo "-i $R/$E/ref_boss_humans.png -i $R/$E/hb1.png -i $R/$D/ref_vertical_mouse.png" ;;
    hr*) echo "-i $R/$E/ref_boss_humans.png -i $R/$E/hb1.png" ;;
    cx3) echo "-i $R/$D/cx2.png -i $R/$D/cx3_v1.png" ;;
    cr*|cx*) echo "-i $R/$E/ref_boss_cats.png" ;;
    tt1) echo "-i $R/UnityResources/Rats/Humans/Sheets/sheet_cook_mount.png" ;;
    rt2) echo "-i $R/$D/rt1.png" ;;
    rt1) echo "-i $R/UnityResources/Rats/Sheets/ballerina.png" ;;
    pr1) echo "-i $R/$E/ref_props.png" ;;
    # 기술 그림 br_* (Tools/gen_boss_fx_prompts.py)
    ef1) echo "-i $R/$E/ref_props.png" ;;
    ef2) echo "-i $R/$D/ref_fx_bosses.png -i $R/$D/ref_goerem_rage1_saw.png -i $R/$D/ref_goerem_rage2_bread.png" ;;
    ef3) echo "-i $R/$E/ref_props.png -i $R/$D/ref_fx_bosses.png -i $R/$D/cx4_full.png -i $R/UnityResources/Rats/BossRework/Props/rebar.png" ;;
  esac
}
run() {
  local s=$1
  [ -f "$D/$s.png" ] && { echo "skip $s"; return; }
  # 프로젝트 안에서 돌리면 .codex/config.toml(UnityMCP url)이 전역 설정(command)과 겹쳐 실패 → 프로젝트 밖 임시 폴더에서 그리고 옮김
  local w; w=$(mktemp -d)
  bash Tools/codex.sh exec --skip-git-repo-check --ephemeral -s workspace-write -C "$(cygpath -w "$w")" $(ref $s) < "$D/prompt_$s.txt" > "$D/log_$s.txt" 2>&1
  [ -f "$w/$s.png" ] && mv "$w/$s.png" "$D/$s.png"
  rm -rf "$w"
  [ -f "$D/$s.png" ] && echo "done $s" || echo "FAIL $s"
}
for s in $LIST; do
  while [ "$(jobs -rp | wc -l)" -ge "$N" ]; do sleep 3; done
  run $s &
done
wait
echo "ALL DONE"
