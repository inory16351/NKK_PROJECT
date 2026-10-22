"""보스 개편 기술 그림 br_* (2026-10-22) Codex 시트 프롬프트. 기술 표 = Tools/gen_boss_skills.py (sprite · opt 칸)
결과: UnityResources/Rats/BossRework/Sheets/prompt_<시트>.txt → bash Tools/run_boss_rework_codex.sh <동시수> ef1 ef2 ef3 → python Tools/slice_boss_fx.py
시트:
  ef1  6x3  날아가는·떨어지는 소품 18칸 (HUH 글자 · 빵 4 · 탈 · 산탄 · 마우스 운석 · 철근 가시 · 전자레인지 · 빨간 버튼 · 방독면 · 그림 도구 4)
  ef2  3x2  보스 변신 그림 6칸 (괴렘 '봤구나' 머리 · 괴렘 식빵 · 허 고양이 머리 · 노조 확성기 · 노조 전화 · 공룡탈 쭈그려 담배)
  ef3  2x2  철근 거미줄 · 꼬마 머라옹 · 새끼 그렘린 · 수염
재사용(그리지 않음): br_club = Props/club · br_rat_jet = Parts/boss_carrier/jet2 · br_siren = Rogue/rg_siren · br_smoke = FX_New/color/smoke
  · br_bullet = SuperUlt/emperor/fx_bullet (좌우 뒤집음) · br_vmouse_head = Humans/Parts/boss_vmouse/head
"""
import io, os
from gen_boss_rework_prompts import FLAT, PARODY, D

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {n}.png (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\n"
        "Image spec:\n- 1536x1024, solid flat MAGENTA background (#FF00FF) everywhere behind the drawings. {text} "
        "No grid lines, no frames, no shadows on the background.\n")
NOTEXT = "No text, no letters, no numbers, no labels."
FX = ("- These are cartoon effect props for boss attacks in a funny rat game. Copy the attached reference props exactly for flat style and scale feel.\n"
      "- STYLE IS THE MOST IMPORTANT RULE: Untitled Goose Game flat look, completely FLAT color shapes, NO black outlines, NO linework, no gloss, "
      "no gradients, no glow, no drop shadows; a few flat color areas with one slightly darker flat tone; soft muted palette.\n"
      "- Every object is one separate island with clear magenta space around it; nothing touches or crosses into another cell. No motion lines unless asked.\n"
      "- Thrown / flying objects point or face LEFT (their direction of travel is to the left).\n")

SHEETS = {}

SHEETS['ef1'] = (HEAD.replace('{text}', 'The ONLY text allowed is the three big letter shapes in cells 1-3; nothing else has any writing.') + FX + PARODY.replace('no text.', 'no other text.') + """
- An exact grid of 6 columns x 3 rows (each cell 256x341). Each prop is centered in its cell with a wide magenta margin, about 200px big, not touching cell borders.

Row 1:
 1) a big chunky puffy cartoon letter "H" like a popped popcorn kernel: cream-yellow, lumpy rounded edges, flat, slightly tilted
 2) the same style puffy popcorn letter "U"
 3) the same style puffy popcorn question mark "?"
 4) a crying loaf of white bread (square loaf with golden-brown crust top) that has a cute cat face on its side: closed-curve eyes with blue tear drops, small 'w' mouth, whiskers, facing left
 5) a crying long pizza bread (oval bun topped with red tomato sauce, melted cheese, green peas) with a cute cat face and tiny cat ears at its left end, blue tears
 6) a crying slice of strawberry shortcake (white cream layers, strawberries on top) with a cute cat face on its side, blue tears
Row 2:
 7) a crying chiffon cake (tall golden ring cake with a hole on top) with a cute cat face on its side, blue tears
 8) an empty green inflatable T-REX COSTUME HEAD mask (cheap mascot costume head, open mouth with white teeth, yellow eye), no person inside, tumbling through the air, facing left
 9) one round dark grey lead SHOTGUN PELLET with a short pale flat speed streak trailing to the RIGHT
 10) a big grey ergonomic VERTICAL computer mouse (upright handshake-shaped mouse, scroll wheel, short cable) falling as a meteor diagonally toward the lower LEFT, a flat orange-yellow flame tail behind it toward the upper right
 11) a straight steel construction REBAR spike lying horizontally: ribbed grey steel bar, the LEFT end cut to a sharp point, right end blunt, about 3x longer than thick
 12) a big old silver MICROWAVE OVEN with a dark glass door, a dial panel on the right, slightly dented, little flat yellow sparks
Row 3:
 13) a big round RED emergency push button on a squat yellow-and-black striped base, seen from a 3/4 angle
 14) a small black rubber GAS MASK in strict SIDE VIEW facing LEFT (round glass eye piece, round filter canister at the front-left, strap behind), sized to fit on a small animal's head
 15) an artist's PAINT BRUSH lying horizontally, wooden handle on the right, bristles with red paint pointing LEFT
 16) a wooden artist's PALETTE (kidney shape with thumb hole) with blobs of red, yellow, blue and white paint
 17) a small wooden artist's EASEL (A-frame tripod), folded open, empty
 18) a framed stretched painting CANVAS showing a simple flat landscape (green hills, blue sky, a little house), wooden frame edge
""")

SHEETS['ef2'] = (HEAD.format(n='{n}', text=NOTEXT) + FLAT + PARODY + """
- An exact grid of 3 columns x 2 rows (each cell 512x512). Each drawing is centered in its cell with a wide magenta margin, about 400px big.
- These are alternate looks of the boss characters shown in the attached reference strip (same characters, same colors, same flat style, same view angle).
  Reference strip left to right: subject kitty head, subject kitty torso (an earless round white chubby cat), pop cat head, union cat head, union cat torso,
  T-rex suit part-timer head, his torso, vertical mouse head, general cat head, gremlin cat head, rat fighter jet.

Row 1:
 1) SUBJECT KITTY's CREEPY HEAD (replaces its normal head, facing LEFT in 3/4 side view like the reference cat heads): the same cream-white earless chubby cat,
    but the NECK is stretched VERY long and tall (a thick white column), the face sits on top with eerie human-like features: one amber eye and one grey eye,
    a wide unsettling smile with an open dark mouth, long whiskers, a small pink nose. A red ribbon collar with a small bow tied at the BOTTOM of the neck.
    The neck bottom is cut flat. Creepy but still funny.
 2) SUBJECT KITTY TURNED INTO A LOAF OF BREAD: a big square loaf of white bread with a golden-brown crust top, sitting on the ground, the whole loaf IS the cat:
    a sad crying cat face on its LEFT end (closed-curve eyes, blue tear drops, small 'w' mouth, whiskers), no legs.
 3) a 'HUH?' MEME CAT HEAD (replaces the pop cat head, same size and the same 3/4 left side view as the pop cat head in the reference): a grey-brown and white cat
    with HUGE round shocked confused eyes, ears perked up, a small open mouth, head slightly tilted; neck cut flat at the bottom.
Row 2:
 4) UNION CAT STANDING UP on its hind legs, strict side view facing LEFT (same grey tabby, red headband, red vest as the reference), holding a big
    white-and-red MEGAPHONE to its mouth with both front paws, shouting, tail down behind.
 5) the same UNION CAT standing on its hind legs facing LEFT, now wearing a YELLOW construction hard hat over the red headband, holding an old phone handset
    to its ear with one paw, worried sweat drops, the other paw on its hip.
 6) the T-REX SUIT PART-TIMER (same green inflatable dino costume, the person's tired face visible in the dino mouth) SQUATTING low on the ground, side view
    facing LEFT, arms resting on the knees, a small cigarette between the fingers with a thin flat grey smoke wisp, dead-tired existential stare.
""")

SHEETS['ef3'] = (HEAD.format(n='{n}', text=NOTEXT) + FX + PARODY + """
- An exact grid of 2 columns x 2 rows (each cell 768x512). Each drawing is centered in its cell with a wide magenta margin.
- Attached references: effect props (style), the boss strip (gremlin cat head), the Merlion cat boss full body, and a bent rebar spider leg.

 1) (top-left, about 460px wide) a big round SPIDER WEB made of welded grey steel construction REBAR, seen from straight above: 8 straight ribbed rebar spokes
    from the center plus 4 rings of bent rebar, little orange weld blobs at the joints, rough and heavy.
 2) (top-right, about 260px tall) a MINI MER-MEOW: a small cute version of the attached Merlion cat boss (orange cat face with a lion-like mane on a silver fish body
    standing upright, tail curled up behind), facing LEFT, hopping.
 3) (bottom-left, about 240px) a BABY GREMLIN: a small cute-mischievous grey-green gremlin kitten (big bat-like ears, red eyes, toothy grin like the gremlin
    cat head in the reference) running on all fours, facing LEFT.
 4) (bottom-right, about 300px wide) a big fake black curly HANDLEBAR MUSTACHE (bushy, two curled ends), flying, slightly tilted.
""")


if __name__ == '__main__':
    for k, v in SHEETS.items():
        with io.open(os.path.join(D, f'prompt_{k}.txt'), 'w', encoding='utf-8') as f:
            f.write(v.replace('{n}', k))
        print('prompt', k)
