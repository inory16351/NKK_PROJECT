#!/usr/bin/env bash
# 세로 파노라마 3장 순서대로 (앞 그림 아래 400px 를 ref_prev_bottom 으로)
cd "$(dirname "$0")"; C=../../../../../Tools/codex.sh
gen() { bash $C exec --skip-git-repo-check --ephemeral -s workspace-write -C . "$@"; }
(gen -i ref_zapham.png < prompt_zap_face_close.txt > log_zap_face_close.txt 2>&1; echo face) &
[ -f pano_a.png ] || gen -i ref_city.png < prompt_pano_a.txt > log_pano_a.txt 2>&1; echo a
python -c "from PIL import Image; im=Image.open('pano_a.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"
[ -f pano_b.png ] || gen -i ref_city.png -i ref_prev_bottom.png < prompt_pano_b.txt > log_pano_b.txt 2>&1; echo b
python -c "from PIL import Image; im=Image.open('pano_b.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"
[ -f pano_c.png ] || gen -i ref_city.png -i ref_prev_bottom.png < prompt_pano_c.txt > log_pano_c.txt 2>&1; echo c
wait; ls pano_*.png zap_face_close.png
