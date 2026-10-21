"""람쥐 초필살기 = 람쥐썬더 짤 몽타주 (2026-10-20 사용자: 처음에 쥐 보여주고 → 실제 짤 그림체의 람쥐썬더 패러디 짤 잔뜩 → 빠른 디졸브 몽타주)
저작권: 나무위키 등 실제 짤은 원작자 사진 합성물 · 남의 게임 그림이 섞여 있어 그대로 쓰지 않음 (사용자 결정: Codex 재현).
→ '2000년대 인터넷 합성 짤' 느낌(실사 다람쥐 사진 + 어설픈 번개 합성)만 따서 새로 그림. 특정 사진 · 히어로 의상 · 로고 · 글자 없음.
결과 = bg_meme01~14.png (slice_superult.py 가 bg_* 를 그대로 복사)"""
import os

D = os.path.dirname(os.path.abspath(__file__))
HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated image. After saving, reply only with the file name.\n\nImage spec:\n")
STYLE = ("- 1536x1024 LANDSCAPE, one full-frame picture, no borders, no panels.\n"
         "- STYLE (MOST IMPORTANT): a funny 2000s Korean internet MEME image ('thunder squirrel' meme): a REAL-LOOKING PHOTOGRAPH of a cute wild squirrel "
         "with deliberately cheesy, amateur photoshopped LIGHTNING effects pasted on top (bright white-blue electric arcs and bolts with a soft glow), "
         "slightly over-sharpened, a bit of old JPEG compression feel, like a meme passed around web forums. Funny and cute, not scary.\n"
         "- NO text, NO letters, NO numbers, NO watermark, NO logos, NO captions, NO speech bubbles. No humans. No real brand or superhero costume.\n")
M = {
    'bg_meme01': "a red squirrel standing straight UPRIGHT on its hind legs in a sunny green park lawn, tiny paws at its chest, while jagged white lightning bolts crackle all around it on the grass (the classic thunder squirrel shot), eye-level photo, shallow depth of field",
    'bg_meme02': "a grey squirrel standing on its hind legs on a rock, BOTH front paws thrust FORWARD toward the camera, thick blue-white lightning beams shooting OUT of its paws, dark stormy sky behind",
    'bg_meme03': "extreme CLOSE-UP of a squirrel face looking straight into the camera, eyes glowing bright electric blue, little electric arcs crackling between its whiskers and ear tufts, dark background",
    'bg_meme04': "a squirrel sitting on a tree branch holding up an ACORN with both paws, a lightning bolt from the sky striking the acorn, which glows brightly, sparks flying, dramatic dusk light",
    'bg_meme05': "a squirrel LEAPING through the air between two trees, body fully stretched, a long bright lightning trail behind it like a comet, motion blur background",
    'bg_meme06': "a squirrel crouched heroically on a mossy log, wearing a tiny round silver viking-style helmet with small white wings and a short red cloth cape, holding a tiny wooden hammer whose head is a big acorn, lightning flashing in a dark cloudy sky behind (generic norse-god parody, no superhero logo)",
    'bg_meme07': "a squirrel in a 'superhero landing' pose on a park path: one knee down, one fist on the ground, head down, with a photoshopped crater, cracked pavement and electric sparks around it (in reality it is just scratching itself, but it looks epic)",
    'bg_meme08': "a squirrel eating a nut with chubby stuffed cheeks, sitting calmly, while a HUGE dramatic lightning storm explodes in the background behind it (funny contrast), wide shot",
    'bg_meme09': "a squirrel on a wooden park bench staring at the camera with photoshopped glowing RED laser-beam eyes (classic 'laser eyes' meme style), red lens flares, stormy background",
    'bg_meme10': "FOUR squirrels standing upright in a row on a lawn like a band, each one with its own lightning bolt striking down on it, a dark dramatic sky, symmetrical composition",
    'bg_meme11': "a squirrel balancing on an old electric POWER LINE between wooden poles, with exaggerated cartoonish electric sparks and arcs fizzing around it, fur sticking up, blue sky",
    'bg_meme12': "a flying-squirrel-like pose: a squirrel gliding toward the camera with all four limbs spread wide like a cape, lightning crackling along its outstretched arms, forest background",
    'bg_meme13': "a 'deep-fried' FLASHING version of the thunder squirrel meme: a squirrel standing upright with arms raised, the whole photo with INVERTED / over-saturated neon colors (cyan, magenta, yellow), heavy glow, extreme lightning everywhere, like a glitchy flashing GIF frame",
    'bg_meme14': "an EPIC wide shot: a tiny squirrel standing on top of a rocky mountain peak with both arms raised to the sky, a gigantic lightning bolt striking down onto it from swirling storm clouds, sunset, movie-poster drama",
}
for n, b in M.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + STYLE + '- SCENE: ' + b + '.\n')
open(os.path.join(D, 'run_memes.sh'), 'w', newline='\n').write('''#!/usr/bin/env bash
# 람쥐썬더 짤 몽타주 그림 14장 (Codex, 참고 그림 없음)
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/ramjui/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
for i in 01 02 03 04 05 06 07; do run bg_meme$i & done; wait
for i in 08 09 10 11 12 13 14; do run bg_meme$i & done; wait; echo DONE
''')
print(sorted(M))
