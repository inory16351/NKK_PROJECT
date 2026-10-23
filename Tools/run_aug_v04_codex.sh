#!/usr/bin/env bash
# 증강 기획서 Ver.04 그림 (2026-10-23) Codex 시트. 사용: bash Tools/run_aug_v04_codex.sh [시트...]  (생략 = 전부, 동시에 3개)
# 프롬프트 = UnityResources/Rats/Augment/Sheets/v04/prompt_<시트>.txt, 이미 있는 그림은 건너뜀. 로그 = log_<시트>.txt → 자르기 = python Tools/slice_aug_v04.py
cd "$(dirname "$0")/.."
D=UnityResources/Rats/Augment/Sheets/v04
R=$(pwd -W)
LIST=${*:-"aug_icons_e aug_fx rat_suit"}
ref() {
  case $1 in
    # 2026-10-23 다시 (사용자: 그림체가 게임과 너무 다름) → 실제 게임 그림을 모은 참고 그림 (증강 아이콘 · 이펙트 · 스케이터 쥐 파츠). 처음 것 = _v1/
    aug_icons_e) echo "-i $R/$D/ref_icons_game.png" ;;
    aug_fx) echo "-i $R/$D/ref_fx_game.png" ;;
    rat_suit) echo "-i $R/$D/ref_rat_parts.png" ;;
  esac
}
run() {
  local s=$1
  [ -f "$D/$s.png" ] && { echo "skip $s"; return; }
  # 프로젝트 안에서 돌리면 .codex/config.toml(UnityMCP url)이 전역 설정과 겹쳐 실패 → 프로젝트 밖 임시 폴더에서 그리고 옮김
  local w; w=$(mktemp -d)
  bash Tools/codex.sh exec --skip-git-repo-check --ephemeral -s workspace-write -C "$(cygpath -w "$w")" $(ref $s) < "$D/prompt_$s.txt" > "$D/log_$s.txt" 2>&1
  [ -f "$w/$s.png" ] && mv "$w/$s.png" "$D/$s.png"
  rm -rf "$w"
  [ -f "$D/$s.png" ] && echo "done $s" || echo "FAIL $s"
}
for s in $LIST; do run $s & done
wait
