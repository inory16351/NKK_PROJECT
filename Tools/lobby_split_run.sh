#!/usr/bin/env bash
# 로비 배경 소품 분리: 폴더 하나에 대해 Codex 두 장 (empty · props) 을 동시에 돌림
# 사용: bash Tools/lobby_split_run.sh <작업 폴더>
D="$1"; T="$(cd "$(dirname "$0")" && pwd)"
cd "$D" || exit 1
for k in empty props; do
  ( bash "$T/codex.sh" exec --skip-git-repo-check --ephemeral -s workspace-write -C . -i base.png < "prompt_$k.txt" > "log_$k.txt" 2>&1; echo "$k EXIT $?" ) &
done
wait
ls -la *.png
