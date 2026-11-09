#!/usr/bin/env bash
# 우주복 고양이 몸통 다시 그리기 (Codex 1장). 프로젝트 밖 임시 폴더에서 그리고 옮김 (run_boss_rework_codex.sh 와 같은 이유)
cd "$(dirname "$0")/../../../../../.."
cd NKK_PROJECT 2>/dev/null || true
D=UnityResources/Rats/Cats/Sheets/space_torso
R=$(pwd -W)
w=$(mktemp -d)
bash Tools/codex.sh exec --skip-git-repo-check --ephemeral -s workspace-write -C "$(cygpath -w "$w")" -i "$R/$D/ref_torso_guide.png" -i "$R/$D/ref_space_parts.png" < "$D/prompt_space_torso.txt" > "$D/log_space_torso.txt" 2>&1
[ -f "$w/space_torso.png" ] && mv "$w/space_torso.png" "$D/space_torso.png"
rm -rf "$w"
[ -f "$D/space_torso.png" ] && echo "done" || echo "FAIL"
