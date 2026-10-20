"""파라오 쥐 초필살기 v2 프롬프트 (2026-10-20 사용자: 아누비스 · 미라를 잔뜩 소환 → 미라가 돌격 · 정면샷 · 기사 쥐/아이스 퀸 연출 참고)
플랫 배경(로비 그림체) · 리그 파츠 기준 셀 프레임 · 플랫 이펙트 → UnityResources/Rats/SuperUlt/pharaoh/Sheets/v2"""
import os, shutil
from PIL import Image

R = r'C:\Project\NKK_PROJECT\UnityResources\Rats'
D = os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'v2')
os.makedirs(D, exist_ok=True)
IQ = os.path.join(R, 'SuperUlt', 'icequeen', 'Sheets', 'v2')
KN = os.path.join(R, 'SuperUlt', 'knight', 'Sheets')
for src, dst in ((os.path.join(R, 'UI', 'lobby_bg.png'), 'ref_flat_bg.png'), (os.path.join(IQ, 'fx_flat.png'), 'ref_flat_fx.png'), (os.path.join(KN, 'fx_light.png') if os.path.exists(os.path.join(KN, 'fx_light.png')) else os.path.join(IQ, 'crack.png'), 'ref_flat_fx2.png'),
                 (os.path.join(KN, 'anim_excal.png'), 'ref_flat_anim.png'), (os.path.join(R, 'SuperUlt', 'ref_zap_style.png'), 'ref_zap_style.png'),
                 (os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'ref_char.png'), 'ref_char.png'), (os.path.join(R, 'SuperUlt', 'pharaoh', 'Sheets', 'ref_parts.png'), 'ref_parts.png')):
    shutil.copy(src, os.path.join(D, dst))
# 리그 파츠 한 장 (게임 파츠 그대로)
ims = [Image.open(os.path.join(R, 'Parts', 'pharaoh', n + '.png')).convert('RGBA') for n in ('head', 'torso', 'front', 'back', 'tail')]
W = Image.new('RGB', (sum(i.width for i in ims) + 40, max(i.height for i in ims)), (255, 0, 255)); x = 0
for i in ims: W.paste(i, (x, 0), i); x += i.width + 10
W.save(os.path.join(D, 'ref_rig.png'))

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
BG = ("- 1536x1024 LANDSCAPE, one full-frame background picture (no transparency, no panels, no borders). NO characters, NO animals, NO text, NO letters, NO numbers, no hieroglyph writing, no logos.\n"
      "- STYLE (MOST IMPORTANT): a FLAT 2D game background exactly in the style of the attached ref_flat_bg (the game's own approved background, style only, not its content): "
      "Untitled Goose Game mood, big simple flat color shapes with clean soft edges, flat fills with at most one soft shade step, very little texture, NO black outlines, NO gloss, "
      "NO painterly brush strokes, NOT a detailed matte painting, NOT glowing fantasy concept art, no tiny clutter. Palette: warm sunset desert — apricot, sand gold, terracotta, dusty rose sky, deep teal shadows, lapis blue accents.\n")
V = "- This picture is ONE SEGMENT of a very TALL vertical panorama that a camera tilts through (top -> middle -> bottom). Segments are stacked vertically, so the composition must continue at the top and bottom edges.\n"
CONT = "- CONTINUITY IS CRITICAL: the attached ref_prev_bottom is the BOTTOM STRIP of the segment directly ABOVE this one. The TOP 30% of your picture must continue it seamlessly (same shapes at the same positions, same colors).\n"
SEG = {
    'a': '1 (TOP) of 3: a huge flat SETTING SUN disk low in a dusty rose and apricot sky, a few long flat cloud bands, a faint flat sun-ring around it; at the very bottom the tips of great pyramids start',
    'b': '2 (MIDDLE) of 3: seen FROM THE FRONT, symmetric: a giant central PYRAMID with a grand stone STAIRWAY coming straight down its middle toward the viewer, two rows of jackal-headed guardian STATUES and tall obelisks lining both sides, smaller pyramids far left and right, heat haze bands',
    'c': '3 (BOTTOM) of 3: seen FROM THE FRONT, a wide flat SAND PLAIN in front of the stairway foot, gentle dune ridges in flat bands going toward the viewer, a few half-buried stone blocks at the far sides; the CENTER of the sand plain is EMPTY and wide open (an army will rise there and run toward the viewer)',
}
STYLE = ("- STYLE (MOST IMPORTANT): match EXACTLY the attached ref_zap_style and ref_flat_anim (approved flat cutscene art of this game) and ref_char: FLAT cartoon storybook look in the Untitled Goose Game mood: "
         "simple soft flat shapes, flat colors with only gentle soft shading, very little texture, NO individual fur strands, small simple black bead eyes with one tiny white highlight, "
         "no black outlines, no gloss, NOT a Disney/Pixar/3D look.\n- NO text, NO letters, NO numbers, no hieroglyph writing, no logos, no speech bubbles anywhere.\n")
CHAR = ('- CHARACTER "Pharaoh Rat": build him EXACTLY from the attached ref_rig (the game\'s own rig parts) and ref_char / ref_parts: a cute grey-beige MOUSE with big pink ears, '
        'a blue-and-gold striped NEMES headdress with a small golden cobra on the forehead, white linen kilt and a broad blue-gold collar, a gold-and-blue CROOK and a gold FLAIL, pink paws, long pink tail. Big head, small body, sleepy confident half-closed eyes.\n')
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no ground, no scenery, no effects. Do not use magenta or purple on the subjects. Keep everything fully inside the frame with margin.\n"
ANIM = ("- 1536x1024 landscape ANIMATION SHEET: 3 columns, ONE full-body frame per column, all frames the same size and scale, each well inside its third, none touching. "
        "Frames are consecutive key drawings of one motion for a cel-animated skill cutscene, so keep the character IDENTICAL between frames, only the pose changes. Strong, dynamic, readable poses (anticipation, action, follow-through).\n")
FRONT = "- CAMERA: every frame is a straight FRONT VIEW (the character faces the viewer / the camera, symmetric framing), like a dramatic anime front shot.\n"
CREFS = ['ref_rig.png', 'ref_char.png', 'ref_zap_style.png', 'ref_flat_anim.png']
# 소환되는 무리 (사용자: 아누비스 · 미라를 잔뜩) — 귀엽게, 게임 그림체
ANUBIS = ('- SUBJECT "Anubis Guard": a cute summoned guardian in the same flat style: a small upright BLACK JACKAL-headed warrior (tall pointed ears with gold trim, gold collar, white kilt, gold arm bands), '
          'holding a long gold SPEAR with a blue tip, glowing soft gold eyes. Same chibi proportions as the Pharaoh Rat (big head, small body). Cute, not scary.\n')
MUMMY = ('- SUBJECT "Mummy Rat": a cute summoned MUMMY MOUSE in the same flat style: a small mouse fully wrapped in cream-white bandages (flat bands with simple seam lines), '
         'pink ear tips and a pink tail tip peeking out, two round glowing soft gold eyes between the bandages, a few loose bandage ends flapping. Same chibi proportions as the Pharaoh Rat. Cute and goofy, not scary, no gore.\n')
P = {}
for k, d in SEG.items():
    P['pano_' + k] = (BG + V + (CONT if k != 'a' else '') + '- SEGMENT ' + d + '.\n', ['ref_flat_bg.png'] + (['ref_prev_bottom.png'] if k != 'a' else []))
P['face_bg'] = (BG + "- A dramatic FLAT cut-in backdrop for a close-up: big flat sun rays in gold and apricot radiating from the center over a deep lapis-blue, a big flat sun disk ring behind the middle, empty middle area where a face will be placed on top. No stars.\n", ['ref_flat_bg.png'])
P['face_close'] = ("- 1536x1024 LANDSCAPE extreme CLOSE-UP of the character's FACE straight from the FRONT for a skill cut-in, face filling most of the height, centered. Keep the simple bead eyes.\n" + STYLE + CHAR + MAG +
                   "- The half-closed sleepy eyes have just snapped OPEN and glow with a soft flat GOLD light, confident smirk, the cobra on the headdress glints, the crook held up beside the face.\n", CREFS)
A = {
    # 파라오 (정면)
    'anim_summon': ("pharaoh", "- FRAMES (left to right), FRONT VIEW: 1) arms crossed over the chest holding the crook and flail (like the ref_char), eyes closed, calm; "
                    "2) eyes open, crook and flail swung OUT to both sides, knees bent, headdress flaps lifting; 3) BOTH ARMS RAISED HIGH to the sky in a V, crook and flail up, chest out, eyes glowing gold, tail up."),
    'anim_command': ("pharaoh", "- FRAMES (left to right), FRONT VIEW: 1) crook held back over one shoulder, leaning back, smug grin (anticipation); "
                     "2) thrusting the crook straight FORWARD AT THE VIEWER (foreshortened, the crook hook big in front), mouth open shouting a command; 3) follow-through: crook still pointed at the viewer, other paw on hip, chin up, cape-like headdress flaps blowing forward."),
    # 아누비스 (땅에서 솟음, 정면)
    'anim_anubis': ("anubis", "- FRAMES (left to right), FRONT VIEW: 1) only the jackal HEAD and ears poking up out of a small flat sand mound; "
                    "2) rising out of the sand up to the waist, sand pouring off the shoulders, spear coming up; 3) fully standing on top of a sand mound, spear planted, chest out, eyes glowing gold (guard pose)."),
    # 미라 (땅에서 솟음 · 돌격 · 덮침, 정면)
    'anim_mrise': ("mummy", "- FRAMES (left to right), FRONT VIEW: 1) only two bandaged paws poking up out of a small flat sand mound; "
                   "2) half the mummy out of the sand, head up, eyes lighting up, sand falling off; 3) fully out, standing hunched with both arms stretched FORWARD toward the viewer (classic mummy pose), bandage ends flapping."),
    'anim_mrun': ("mummy", "- RUN CYCLE toward the viewer: the mummy is CHARGING STRAIGHT AT THE CAMERA (front view, slightly low angle), 3 frames of a fast goofy run: "
                  "1) left foot forward, both arms stretched straight forward, body leaning in; 2) both feet off the ground mid-stride, bandage ends streaming BACK; 3) right foot forward, arms forward, mouth open yelling. Small sand puffs at the feet."),
    'anim_mleap': ("mummy", "- FRAMES (left to right), FRONT VIEW toward the viewer: 1) crouched low ready to pounce; 2) LEAPING up and forward at the camera, arms spread wide; "
                   "3) very close to the camera, arms and loose bandages spread wide like it is about to hug the screen, body big and foreshortened, goofy determined face."),
}
SUBJ = {'pharaoh': CHAR, 'anubis': ANUBIS, 'mummy': MUMMY}
for n, (who, b) in A.items():
    refs = CREFS if who == 'pharaoh' else ['ref_char.png', 'ref_zap_style.png', 'ref_flat_anim.png']
    P[n] = (ANIM + FRONT + STYLE + SUBJ[who] + MAG + b + "\n", refs)
# 무리 한 장 (정면, 겹쳐 선 군단) — 2026-10-20 사용자: 아누비스는 한 마리만 → 군단은 미라만 (옛 것 = _old/army_with_anubis.png)
P['army'] = ("- 1536x1024 landscape image: a summoned HORDE seen straight FROM THE FRONT, many small figures standing in 3 staggered rows (back row smallest, front row biggest), all facing the viewer: "
             "about 24 Mummy Rats ONLY (no other creatures, no jackals), arms stretched forward, eyes glowing gold, a few with loose bandage ends flapping, goofy happy faces. The figures in each row overlap a little but every figure is readable. No ground, no sand, nothing else.\n"
             + STYLE + MUMMY + MAG + "- Draw every mummy EXACTLY like the attached ref_mummy.\n", ['ref_mummy.png', 'ref_zap_style.png', 'ref_flat_anim.png'])
# 게임 화면 단계: 미라가 화면을 왼→오로 달려 지나감 (옆모습 달리기, 2026-10-20 사용자: 모래바람 대신 미라 돌격으로 마무리)
P['anim_mside'] = (ANIM + STYLE + MUMMY + MAG + "- Draw EXACTLY the same Mummy Rat as the attached ref_mummy (same bandages, ears, eyes, proportions).\n"
                   "- RUN CYCLE in SIDE VIEW facing RIGHT (running to the right), 3 frames of a fast goofy sprint: 1) contact: front foot reaching forward, both arms stretched straight forward (mummy pose), body leaning forward; "
                   "2) passing: legs gathered under the body, slightly up, bandage ends streaming BACK; 3) push-off: back leg pushing, front leg lifted high, arms still forward, mouth open happily yelling. No sand, no dust.\n",
                   ['ref_mummy.png', 'ref_zap_style.png', 'ref_flat_anim.png'])
FXS = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE element per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
       "- STYLE (MOST IMPORTANT): FLAT 2D game effect art exactly like the attached ref_flat_fx and ref_flat_fx2 (approved flat effects of this game): simple flat shapes, flat fills with at most one soft shade step, clean edges, NO glow, NO bloom, NO gradients, NO tiny dust, NO painterly texture, NO black outlines.\n"
       "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow. Do not use magenta, pink or purple on the elements.\n- NO text, NO letters, NO numbers, NO stars.\n")
P['fx_flat'] = (FXS + "- Palette: sand gold, warm cream, apricot, lapis blue, deep teal.\n"
                "  1. a SUMMONING CIRCLE seen slightly from above (flat ellipse): a gold ring with a second inner ring and simple abstract geometric marks (dots, triangles, sun disks; NOT letters)\n"
                "  2. a SAND BURST: a flat fountain of sand bursting UP from the ground, chunky flat sand shapes and a few pebbles\n"
                "  3. a big flat SAND DUST CLOUD seen from the front (wide, low, puffy rounded shapes in sand gold and cream)\n"
                "  4. a single loose BANDAGE STRIP flying in a wavy S-curve, cream white\n"
                "  5. a flat round SUN FLASH: a gold disk with flat rays (rounded rays, not a star)\n"
                "  6. a flat SAND RING shockwave: a wide thin ellipse ring of sand seen from the front, a few chunks flying off\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['fx_hit'] = (FXS + "- Palette: sand gold, warm cream, apricot, lapis blue.\n"
               "  1. a long horizontal SAND STREAK (a running dust trail, tapered on the left)\n"
               "  2. a flat SCARAB SPARK: a small gold beetle shape with a little sparkle ring\n"
               "  3. a small round SAND PUFF\n"
               "  4. TACKLE IMPACT frame 1: a small flat cream burst with 3 tiny sand chunks\n"
               "  5. TACKLE IMPACT frame 2: the impact spreading: a bigger flat cream-gold burst, a small flat ring, 6 sand chunks flying out\n"
               "  6. TACKLE IMPACT frame 3: the end: a wide thin broken ring and a few small sand chunks, fading shapes\n", ['ref_flat_fx.png', 'ref_flat_fx2.png'])
P['props_flat'] = (FXS.replace("effect art", "prop art") +
                   "  1. an upright golden SARCOPHAGUS standing, lid slightly open, front view\n  2. a cute canopic JAR with a jackal-head lid\n  3. a big golden SCARAB amulet\n"
                   "  4. a stone OBELISK, front view\n  5. a small flat PYRAMID-shaped sunscreen bottle (comic prop)\n  6. a long horizontal strip of SAND DUNE ridge (foreground edge, wide)\n", ['ref_flat_fx.png', 'ref_flat_bg.png'])
for n, (b, refs) in P.items():
    open(os.path.join(D, f'prompt_{n}.txt'), 'w', encoding='utf-8').write(HEAD.format(f=n + '.png') + b)
    open(os.path.join(D, f'refs_{n}.txt'), 'w').write(' '.join(refs))
open(os.path.join(D, 'run_v2.sh'), 'w', newline='\n').write('''#!/usr/bin/env bash
# 파라오 쥐 초필살기 v2 그림 (플랫 · 정면 · 리그 파츠 기준 셀 프레임 · 아누비스 · 미라) Codex
D=/c/Project/NKK_PROJECT/UnityResources/Rats/SuperUlt/pharaoh/Sheets/v2; CX=/c/Project/NKK_PROJECT/Tools/codex.sh; cd "$D"
run() { n=$1; [ -f "$n.png" ] && return 0; args=(); for r in $(cat refs_$n.txt); do args+=(-i "$r"); done
  for a in 1 2; do bash $CX exec --skip-git-repo-check --ephemeral -s workspace-write -C "$D" "${args[@]}" < prompt_$n.txt > log_$n.txt 2>&1; [ -f "$n.png" ] && { echo "$(date +%T) ok $n"; return; }; done; echo "$(date +%T) FAIL $n"; }
pano() { run pano_a; python -c "from PIL import Image; im=Image.open('pano_a.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_b
  python -c "from PIL import Image; im=Image.open('pano_b.png'); im.crop((0,im.height-400,im.width,im.height)).save('ref_prev_bottom.png')"; run pano_c; }
pano &
for n in face_bg face_close anim_summon anim_command anim_anubis; do run $n & done; wait
for n in anim_mrise anim_mrun anim_mleap army anim_mside fx_flat fx_hit props_flat; do run $n & done; wait; echo DONE
''')
print(sorted(P))
