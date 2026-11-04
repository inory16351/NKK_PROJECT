#!/usr/bin/env bash
# 도토리 상점 장식 시트 Codex 돌리기: 폴더마다 prompt.txt + 그 폴더의 ref_*.png · base.png 를 참고로 sheet.png 생성
# 사용: bash Tools/deco_run.sh <폴더> [<폴더> ...]   (전부 동시에)
T="$(cd "$(dirname "$0")" && pwd)"
for D in "$@"; do
  (
    cd "$D" || exit 1
    args=(); for f in base.png ref_*.png; do [ -f "$f" ] && args+=(-i "$f"); done
    bash "$T/codex.sh" exec --skip-git-repo-check --ephemeral -s workspace-write -C . "${args[@]}" < prompt.txt > log.txt 2>&1
    echo "$D EXIT $? $( [ -f sheet.png ] && echo sheet_ok || echo NO_SHEET )"
  ) &
done
wait
