"""보스 개편 (2026-10-22) Codex 시트 프롬프트. 명세 = UnityResources/Rats/BossRework/README.md
결과: UnityResources/Rats/BossRework/Sheets/prompt_<시트>.txt → bash Tools/run_boss_rework_codex.sh <동시수> [시트...]
시트:
  hr1 hr2   사람 리그 (머리·화난 머리·겁먹은 머리·몸통·팔·다리)
  cr1 cr2   고양이 리그 (머리·몸통·꼬리·앞다리·뒷다리·두 번째 머리)
  cx1 cx2   전용 리그 보스 (거미·상어·물고기 / 항공모함)
  tt1       차력거인 (슈퍼 요리사 탈것 리그 변형)
  pr1       소환물·소품
"""
import io, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
D = os.path.join(ROOT, 'UnityResources', 'Rats', 'BossRework', 'Sheets')

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {n}.png (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\n"
        "Image spec:\n- 1536x1024, solid flat MAGENTA background (#FF00FF) everywhere behind the drawings. No text, no letters, no numbers, no labels, "
        "no grid lines, no frames, no shadows on the background.\n")
FLAT = ("- STYLE IS THE MOST IMPORTANT RULE: copy the attached reference exactly: Untitled Goose Game flat look, completely FLAT color shapes, "
        "NO black outlines, NO linework, no gloss, no gradients, no glow, no drop shadows; a few flat color areas with one slightly darker flat tone; soft muted palette. "
        "These are BOSSES of a funny rat game: big, bulky, imposing, but still cute-funny.\n"
        "- Every part is a separate island with clear magenta space around it; nothing touches or crosses into another cell.\n")
PARODY = "- Parody only: no real person's face, no logos, no brand marks, no symbols, no insignia, no text.\n"

HUMAN = ("This is a cut-out PARTS SHEET for a 2D game rig (parts are assembled by code).\n"
         "- Exact layout: {rows} rows x 6 columns. Each ROW is one BOSS. Columns: 1) NORMAL HEAD side view facing LEFT with a short narrow neck stump cut flat "
         "(the nape curves inward, nothing bulges behind); 2) ANGRY SHOUTING head; 3) SCARED head (screaming, sweat drops); 4) TORSO side view, no head/arms/legs; "
         "5) ONE ARM hanging down (rounded sleeve shoulder cap, no cut section); 6) ONE LEG standing, top cut flat.\n"
         "- Copy the attached references exactly for style and part layout (flat paper-doll human boss parts). Strict SIDE VIEW, the character FACES LEFT.\n")
CAT = ("This is a cut-out PARTS SHEET for a 2D game rig (parts are assembled by code).\n"
       "- Exact layout: {rows} row(s) x 6 columns. Each ROW is one boss cat. Columns: 1) HEAD facing LEFT (neck cut flat at the bottom), 2) TORSO seen from the side, "
       "horizontal, head end on the LEFT, no head, no legs, no tail, 3) TAIL starting at its left end (base) curving up to the right, 4) FRONT LEG standing straight, "
       "top cut flat, paw at the bottom, 5) BACK LEG standing, top cut flat, paw at the bottom, 6) a SECOND HEAD of the same cat (expression given per row), same size and angle as column 1.\n"
       "- Match the attached reference exactly for part shapes, proportions, layout and view angle (maine coon, witch cat, space cat bosses).\n")

SHEETS = {}

SHEETS['hr1'] = (HUMAN.format(rows=3) + FLAT + PARODY + """
Rows:
Row 1: "VERTICAL MOUSE": a tall UPRIGHT muscular RAT-MAN (copy the body shape of the attached line drawing ref_vertical_mouse: rat head with big round ears and a long snout, broad upright rat body, long bare tail) with grey-brown fur and pink ears/nose/hands/feet, dressed as a CAVEMAN. Heads = the rat head with one big round ear and long whiskered snout (normal = dumb tough stare / angry = roaring with buck teeth showing / scared = squeaking with sweat drops). Torso = furry rat chest wearing a ragged leopard-pattern fur tunic over one shoulder, with the LONG PINK RAT TAIL growing from the back bottom of the torso and curling behind. Arm = furry rat arm hanging down, the pink hand GRIPPING a big knobby wooden caveman club that points down. Leg = furry rat leg with a long pink rat foot, bare.
Row 2: "T-REX SUIT PART-TIMER": a tired part-timer in a cheap INFLATABLE green T-rex costume. Heads = big green inflatable T-rex head with the person's tired face visible inside the open T-rex mouth (normal = bored dead eyes / angry = yelling / scared = panicking with sweat). Torso = round inflated green dino belly with a pale belly patch and a small white name-tag sticker (blank). Arm = tiny short green dino arm with little claws. Leg = thick green inflatable dino leg with three toe claws.
Row 3: "RAPTOR TRAINER" (action-movie raptor trainer parody): rugged tall man, short brown swept hair, stubble, determined squint. Torso = dark brown leather vest over a grey henley shirt, khaki cargo pants waist. Arm = grey sleeve rolled up, the hand OPEN with the PALM facing forward and fingers spread (the calm-down 'hold up' gesture). Leg = khaki cargo pants with brown work boot.
""")

SHEETS['hr2'] = (HUMAN.format(rows=2) + FLAT + PARODY + """
Rows:
Row 1: "EXCAVATOR OPERATOR": a stocky Korean construction site machine operator, sunburned face, thick eyebrows, small mustache, orange hard hat with clear safety goggles pushed up on it. Torso = navy work shirt under a bright orange high-visibility vest with two silver reflective stripes, a white towel hanging around the neck. Arm = navy sleeve with a yellow work glove. Leg = navy work pants with a dusty brown steel-toe work boot.
Row 2: "DEEP-SEA DIVER": a big old-fashioned deep-sea diver. Heads = a ROUND BRASS diving helmet with bolts and a round glass front port, the man's face (bushy beard) visible behind the glass (normal = calm / angry = shouting, glass fogged / scared = screaming, glass slightly cracked, sweat inside). Torso = brass shoulder breastplate (corselet) over a heavy tan canvas diving suit, with a lead weight belt. Arm = puffy tan canvas sleeve with a thick dark rubber glove. Leg = puffy tan canvas leg with a heavy lead diving boot.
""")

SHEETS['cr1'] = (CAT.format(rows=3) + FLAT + PARODY + """
Rows:
Row 1: "POP CAT" (the famous popping-mouth cat meme): orange-and-white short hair cat. IMPORTANT: its HEAD is twisted toward the viewer (almost FRONT VIEW, only slightly turned left) so the meme face reads clearly: big round eyes. Column 1 = mouth CLOSED (small neutral mouth). Column 6 = the exact same head with a HUGE perfectly round open mouth (O shape) mid-'pop'. Torso orange tabby with white belly, orange tail, orange legs with white paws.
Row 2: "UNION CAT": a big grey tabby cat with a red protest headband (blank, no text) and a lazy defiant sleepy face. Torso wearing a red union vest. Grey tabby tail and legs. Column 6 = same head shouting a protest chant with mouth wide open and one eye squinting.
Row 3: "JOASSU CAT" (a fired-up construction worker cat, smaller helper): orange tabby cat wearing a yellow construction hard hat with a white towel around the neck. Torso = navy work jacket with reflective stripes. Orange tail, legs with dusty work-glove paws. Column 1 = proud grin. Column 6 = same head screaming a fierce battle cry, eyes squeezed shut, mouth huge.
""")

SHEETS['cr3'] = (CAT.format(rows=1) + FLAT + PARODY + """
Row 1: "POP CAT" (the famous popping-mouth cat meme), drawn in the SAME 3/4 SIDE PROFILE as the reference cats' heads (facing LEFT), turned toward the viewer only just enough that the open mouth is clearly visible. Do NOT draw a full front view.
- Coat: a mostly WHITE short-haired cat; only the EARS and the back/side of the head have soft grey-brown patches (one darker brown patch behind the cheek). Muzzle, chin, neck, chest, belly, legs and paws WHITE. Tail white with a grey-brown tip; one or two grey-brown patches on the back.
- Small dark round eyes, tiny pink nose, calm blank stare, chin slightly raised.
- Column 1 = mouth CLOSED (tiny cat mouth). Column 6 = the exact same head with a HUGE OPEN OVAL mouth (pink-red inside, darker center, light pink rim) taking up most of the lower face — the meme 'pop'.
""")

SHEETS['cr4'] = (CAT.format(rows=1) + FLAT + PARODY + """
STRICT SIDE VIEW like the reference cats (head in side profile facing LEFT), NOT front view.
Row 1: "UNION CAT": a big grey tabby cat with a red protest headband tied around the head (knot and loose ends at the back, blank, no text), a lazy defiant half-closed eye. Torso wearing a red union vest. Grey tabby tail and legs. Column 6 = same side-view head shouting a protest chant, mouth wide open.
""")

SHEETS['hr3'] = (HUMAN.format(rows=1) + FLAT + PARODY + """
Row 1: "SAFETY-CALL CAT" (parody of the Korean/Japanese 'site cat' meme that points and shouts "Joassu!! / Yoshi!"): a cartoon CAT that STANDS UPRIGHT ON TWO LEGS like a person, drawn with this rig's human part layout (side view facing LEFT).
- Coat: GREY on the top of the head, ears, back, upper arms, outer legs and tail; WHITE face/muzzle, cheeks, chest, belly, paws. Round YELLOW eyes with small black pupils, a tiny pink nose, whiskers, wearing a plain WHITE construction safety HARD HAT (no logo).
- Heads: 1) NORMAL = proud confident open smile with a little pink tongue showing, totally sure of itself. 2) the 'angry' slot = SHOUTING "Joassu!!" — mouth wide open, eyes intense, full confidence. 3) SCARED = shocked, eyes tiny, sweat drops.
- Torso = the cat's upright grey-and-white body (grey back, white belly) with the GREY TAIL with white tip curling out from the lower back, no clothes.
- Arm = grey upper arm with a white paw POINTING FORWARD with one extended finger (the safety-check pointing gesture), the arm hanging down from a rounded shoulder.
- Leg = grey-and-white cat leg standing, white foot paw.
""")

SHEETS['cr2'] =(CAT.format(rows=3) + FLAT + PARODY + """
Rows:
Row 1: "GOEREM": a HUGE, very ROUND, chubby EARLESS white baby cat (absolutely NO ears: a smooth round dumpling-shaped head), tiny black dot eyes, tiny cat mouth, a little uncanny. Cream-white soft fur, very fat round torso, short stubby round tail, short stubby round legs. Column 6 = same earless head with the mouth stretched open unnaturally wide and dark inside (funny cosmic-horror).
Row 2: "DICTATOR CAT": a grey cat with a small black square mustache patch of fur under its nose and a strand of dark fur flopping over its forehead, wearing a grey-green military peaked officer cap and a grey-green military greatcoat torso (plain: NO symbols, NO insignia, NO armband, NO medals). Grey tail, grey legs with black boots. Column 1 = stern. Column 6 = furiously ranting, mouth wide, fist-shaking rage, the forehead fur strand flying.
Row 3: "GREMLIN CAT": a scrawny monster cat: mottled grey-green fur, HUGE pointed bat-like ears, glowing red eyes, wide jagged sharp-toothed grin, a spiky white mohawk stripe from the head down the back. Torso scrawny with the white mohawk stripe. Thin long tail with a tuft. Skinny legs with long hooked claws. Column 6 = screeching with mouth fully open, ears pinned back.
""")

SHEETS['cx1'] = ("This is a cut-out PARTS SHEET for a 2D game rig (parts are assembled by code).\n"
    "- Exact layout: 3 rows x 5 columns. Each ROW is one boss built from the parts listed. All parts in SIDE VIEW facing LEFT unless said otherwise.\n"
    "- Match the attached reference exactly for flat style and boss scale feel (boss cats).\n" + FLAT + PARODY + """
Rows:
Row 1: "SPIDER CAT" (a creepy toy-chimera parody: an animal head on a crawling spider-like junk body, built from construction-site rebar). 1) a grey tabby CAT HEAD, slightly front-facing, one eye normal and one eye a wide staring glassy doll eye, neck cut flat. 2) the same head hissing, mouth open. 3) the BODY HUB: a round lump of grey concrete chunk wrapped with bent rusty steel rebar and a bit of orange safety tape, seen from the side (the legs will be attached around it). 4) ONE long SPIDER LEG made of bent rusty ribbed steel REBAR with two sharp bends (knee joints), top end cut flat, tip pointed. 5) a second shorter rebar leg variant.
Row 2: "SHARK CAT" (the Italian brainrot shark-in-sneakers meme, as a cat): 1) the FRONT of a blue-grey shark (snout and jaw) that has a CAT FACE: cat ears on top, cat eyes, whiskers, a wide toothy shark grin; cut flat where it joins the body. 2) same head with the jaw wide open (chomp). 3) the shark BODY from behind the head to the tail fin, horizontal, with a dorsal fin and white belly, no head. 4) ONE skinny blue-grey LEG standing straight wearing a chunky blue-and-white running SNEAKER (no logo), top cut flat. 5) one side pectoral fin.
Row 3: "FISH CAT": a big fish with a cat's head. 1) an orange tabby CAT HEAD facing left, grumpy, neck cut flat. 2) same head with mouth open gulping like a fish. 3) a fat silver-blue mackerel FISH BODY with scales and stripes, horizontal, head end on the LEFT cut flat, no tail fin. 4) the forked FISH TAIL FIN, base on the left. 5) one fish side fin.
""")

SHEETS['cx2'] = ("This is a cut-out PARTS SHEET for a 2D game boss rig (parts are assembled by code).\n"
    "- Layout: TOP BAND (upper 45% of the image) = ONE long part spanning nearly the full width. BOTTOM = 1 row x 4 cells. All side view facing LEFT.\n"
    "- Match the attached reference exactly for flat style and boss scale feel.\n" + FLAT + PARODY + """
Parts:
TOP BAND: "AIRCRAFT CARRIER CAT" hull: a long grey aircraft carrier seen from the side, bow pointing LEFT, a flat flight deck on top with white runway stripes, no island tower (an empty gap on the deck near the right-middle where the tower will be placed), hull with portholes, red waterline, white sea foam along the bottom.
Bottom cell 1: the carrier's ISLAND CONTROL TOWER that IS a CAT HEAD: a big grey cat head (facing left) wearing a grey ship-tower helmet structure with radar dish and antenna on top, stern face, the bottom cut flat (sits on the deck).
Bottom cell 2: the same cat-head tower shouting an order, mouth open.
Bottom cell 3: a small grey JET FIGHTER plane seen from the side facing left whose cockpit/nose is a RAT HEAD (rat with flight goggles sticking out of the nose), funny.
Bottom cell 4: the same rat-head jet with afterburner flame at the back, wings angled (launching).
""")

SHEETS['cx3'] = ("Two separate drawings for a 2D game boss part (the aircraft carrier's island control tower), side by side: LEFT half and RIGHT half of the image, each centered with a wide magenta margin.\n"
    "- Copy the attached reference sheet exactly for flat style, grey ship colors and scale (the top band hull and the two tower drawings).\n" + FLAT + PARODY + """
Make a CAT-HEAD CONTROL TOWER that reads as a CAT FIRST and a ship tower second (the previous attempt looked too much like a plain ship tower).
- Silhouette: a big ROUND CAT HEAD shape (like the cat-head towers in the reference sheet's bottom row), with two LARGE POINTED TRIANGULAR CAT EARS sticking up clearly (steel plates, inner ear a darker grey panel), a chubby cat cheek/muzzle bump, all made of grey ship steel with a few rivets and panel seams.
- Face: TWO separate big ALMOND-SHAPED CAT EYES that are BRIDGE WINDOWS (dark blue glass divided by thin window frames, slanted like cat eyes), a pink-grey triangular cat NOSE plate, a 'w'-shaped cat mouth made of a vent seam, long thin antenna-rod WHISKERS.
- Ship parts are only small accents: a small radar dish and a thin mast with antennas sitting on top of the head BETWEEN the ears, one small row of portholes on the cheek, a flat steel base at the bottom (sits on the deck).
- Side 3/4 view facing LEFT, same size as the reference tower.
LEFT drawing: normal — calm, stern half-lidded window eyes, mouth closed.
RIGHT drawing: shouting an order — the window eyes glowing bright yellow and slanted angrily, the mouth wide open like a hangar door with a dark inside, ears pinned back a little.
""")

SHEETS['tt1'] =("This is a cut-out PARTS SHEET for a 2D game rig, exactly the same part set and layout as the attached reference sheet (a cook crawling on all fours, used as a mount): "
    "row 1 = body (torso, horizontal, head end LEFT), butt (hips with the top of the thighs), thigh, shin with foot; row 2 = upper arm, forearm with hand, head 1, head 2.\n"
    "- 2 rows x 4 columns, same proportions and cut lines as the reference.\n" + FLAT + PARODY + """
Character: "CART GIANT" (a crawling titan parody): a HUGE muscular bare-skinned giant man who walks on all fours. Skin = muscular light tan with darker red muscle-stripe hints at the joints. Long straight dark hair. The BODY must be LONGER and FLATTER on top than the reference: a wide flat back carrying a strapped wooden-and-steel platform saddle (empty, cats will sit on it). The thigh and upper arm parts are thicker. Hands and feet bare and big.
Head 1 = the giant's head (side view facing LEFT, looking down-forward), long dark hair hanging, blank calm stare, big square jaw. Head 2 = same head straining, teeth gritted, eyes wide.
""")

SHEETS['pr1'] = ("These are separate cartoon PROPS / SUMMONS for boss skills in a funny rat game.\n"
    "- Exact layout: 3 rows x 4 columns grid (cells 384x341), one drawing centered in each cell with a wide margin. Side view facing LEFT unless said otherwise.\n"
    "- Copy the attached reference props exactly for flat style and scale feel.\n" + FLAT + PARODY + """
Cells (left to right, top to bottom):
1) a VELOCIRAPTOR running (slim, tan with dark brown stripes, long tail straight back), funny-menacing.
2) the same VELOCIRAPTOR leaping/pouncing with claws forward.
3) a yellow EXCAVATOR body seen from the side: cab with a window (empty seat), engine housing and black crawler tracks, NO boom arm attached.
4) the excavator's BOOM ARM: one long yellow bent arm (boom + stick) with hydraulic cylinders, with the BUCKET with teeth at the end, the base end on the right.
5) a big DINOSAUR-sized EGG, cream with brown speckles, standing upright.
6) the same egg with big jagged CRACKS and a small piece missing, a faint red glow inside the crack.
7) a small grey CAT sitting behind a tiny TURRET machine gun (side view, facing left), wearing a little military helmet.
8) a long green ALIEN TENTACLE, curling, with suction cups, base at the bottom cut flat.
9) a pile of three small eggshell FRAGMENTS (cream with brown speckles).
10) a big knobby wooden caveman CLUB with a bone tied on.
11) a bundle of three bent rusty steel REBAR rods, crossing.
12) a red TRAFFIC CONE.
""")

SHEETS['rt1'] = ("This is a cut-out PARTS SHEET for a 2D game RAT rig, exactly the same part set, view and proportions as the attached reference rat sheet.\n"
    "- Exact layout: 2 rows x 5 columns. All side view facing LEFT.\n" + FLAT + PARODY + """
Character: "CREWMATE RAT" (a space-crew game parody, the secret impostor): a rat wearing a puffy RED one-piece spacesuit shaped like the famous bean-shaped crewmates, with a wide pale-blue VISOR, a small backpack, the rat's two round ears poking out on top of the suit, a pink rat tail.
Row 1 (normal rat parts, same as reference columns): 1) HEAD = the red suit helmet with the wide visor (no face visible, just the glossy-flat light-blue visor shape) and two round rat ears on top, neck cut flat. 2) TORSO = red suit body with the little backpack, horizontal, head end LEFT. 3) TAIL = pink rat tail, base on the left. 4) FRONT LEG = short red suit leg with a pink rat paw. 5) BACK LEG = short red suit leg with a pink rat foot.
Row 2 (monster reveal parts): 1) the HEAD LOWER HALF only: the helmet cut horizontally through the middle of the visor, showing inside a huge dark wet mouth with rows of sharp white teeth and a long pink tongue. 2) the HEAD TOP LID only: the top half of the helmet with the upper visor and the ears, cut flat at the bottom (it flips open like a lid). 3) a GACHA PORTRAIT of the crewmate rat in FRONT view, cute innocent pose, waving. 4) a long ALIEN TONGUE-TENTACLE coming out of the mouth, pink-red, curling, with a toothy tip, base on the left. 5) a little red 'sus' bean-shaped crewmate rat silhouette sticker (front view, simple).
""")


SHEETS['rt2'] = ("Two separate cut-out parts for a 2D game rig (the head of the red CREWMATE RAT from the attached reference sheet splits into two jaws like a monster mouth). LEFT half and RIGHT half of the image, each part centered with a wide magenta margin. Side view facing LEFT, same size and colors as the reference head (row 1, column 1).\n"
    + FLAT + PARODY + """
LEFT: the UPPER JAW = the top half of the red suit helmet: the two round rat ears, the wide pale-blue visor, and along its whole bottom edge a row of big sharp white UPPER TEETH hanging down from a dark red gum line; the inside (roof of the mouth) dark red. The back (right side) ends in a rounded hinge where it joins the lower jaw.
RIGHT: the LOWER JAW = the bottom half of the red helmet/neck: along its top edge a row of big sharp white LOWER TEETH pointing up from a dark red gum line, a dark wet mouth floor, a long pink TONGUE lolling out to the left, the bottom cut flat (sits on the neck). The back (right side) ends in a rounded hinge.
When the two parts are put back together closed, they should form a head the same size as the reference head.
""")


def main():
    os.makedirs(D, exist_ok=True)
    for k, body in SHEETS.items():
        with io.open(os.path.join(D, f'prompt_{k}.txt'), 'w', encoding='utf-8', newline='\n') as f:
            f.write(HEAD.format(n=k) + body)
        print('prompt', k)


if __name__ == '__main__':
    main()
