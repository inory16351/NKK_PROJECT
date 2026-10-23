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
    # 2026-10-23 이펙트 다시 (사용자: 광택 그림체 → 플랫) → 실제 게임 바닥 물건 + UI 키트. 광택 판 = _v2/
    aug_fx) echo "-i $R/$D/ref_flat_game.png" ;;
    # 2026-10-23 저녁 (사용자: 증강 아이콘도 그림체가 다름) → 아이콘 32개 플랫으로: 그림체 = ref_flat_game · 내용 = 지금 아이콘을 같은 격자로 모은 ref_icons_f*
    aug_icons_fa|aug_icons_fb|aug_icons_fc) echo "-i $R/$D/ref_flat_game.png -i $R/$D/ref_icons_${1#aug_icons_}.png" ;;
    # 2026-10-23 저녁 (사용자: 겟 투 워크 패러디 → 팔·다리에 롤러스케이트) → 지난 양복 쥐 시트를 참고로 다리만 바꿈. 이전 판 = _v2/
    rat_suit) echo "-i $R/$D/_v2/rat_suit.png" ;;
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
