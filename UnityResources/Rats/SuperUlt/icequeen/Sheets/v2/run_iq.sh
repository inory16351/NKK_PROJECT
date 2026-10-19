#!/usr/bin/env bash
# 아이스 퀸 v2 그림 Codex 실행 (플랫 배경 · 쥐 따로)
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/icequeen/Sheets/v2
CX=/c/Project/NKK_PROJECT/Tools/codex.sh
run() { n=$1; shift; [ -f "$D/$n.png" ] && return 0
  for a in 1 2; do args=(); for r in "$@"; do args+=(-i "$r"); done
    bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < "$D/prompt_$n.txt" > "$D/log_$n.txt" 2>&1
    [ -f "$D/$n.png" ] && { echo "$(date +%T) ok $n"; return 0; }; echo "$(date +%T) retry $n"; done; echo "$(date +%T) FAIL $n"; }
cd "$D"
R="ref_zap_style.png ref_char.png ref_parts.png"
run pose_intro $R & run pose_act $R & run face_close $R & run face_bg ref_flat_bg.png &
run pano_a ref_flat_bg.png ref_ice_castle.png
run pano_a_alt pano_a.png &
python -c "from PIL import Image; im=Image.open('pano_a.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"
run pano_b ref_flat_bg.png ref_ice_castle.png ref_prev_bottom.png
run pano_b_alt pano_b.png &
python -c "from PIL import Image; im=Image.open('pano_b.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"
run pano_c ref_flat_bg.png ref_prev_bottom.png
run pano_c_alt pano_c.png
wait; echo "$(date +%T) DONE"
