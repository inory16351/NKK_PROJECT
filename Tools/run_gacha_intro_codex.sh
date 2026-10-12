#!/usr/bin/env bash
# 쥐 고유 컷씬 그림을 Codex 로 (동시에 N개씩). 사용: bash Tools/run_gacha_intro_codex.sh <동시수> code...
# 이미 있는 그림은 건너뜀. 로그 = Sheets/log_<code>_cN.txt
cd "$(dirname "$0")/.."
N=$1; shift
jobs_list=()
for code in "$@"; do for i in 1 2 3; do jobs_list+=("$code $i"); done; done
run() {
  local code=$1 i=$2 D=UnityResources/Rats/GachaIntro/$1/Sheets
  [ -f "$D/${code}_c$i.png" ] && return
  bash Tools/codex.sh exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" -i "$D/ref_style.png" -i "$D/ref_parts.png" < "$D/prompt_${code}_c$i.txt" > "$D/log_${code}_c$i.txt" 2>&1
  [ -f "$D/${code}_c$i.png" ] && echo "done $code c$i" || echo "FAIL $code c$i"
}
for j in "${jobs_list[@]}"; do
  while [ "$(jobs -rp | wc -l)" -ge "$N" ]; do sleep 2; done
  run $j &
done
wait
echo "ALL DONE"
