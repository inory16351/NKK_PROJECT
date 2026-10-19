"""100층 확장 (2026-10-19) Codex 시트 프롬프트 생성 → UnityResources/Rats/Expansion100/Sheets/prompt_<시트>.txt
시트 목록 · 칸 순서는 아래 SHEETS (자르기 = Tools/slice_expansion.py 가 같은 목록을 읽음).
사용: python Tools/gen_expansion_prompts.py  →  bash Tools/run_expansion_codex.sh <동시수> [시트...]"""
import io, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'UnityResources', 'Rats', 'Expansion100', 'Sheets')

HEAD = """Use your built-in image generation tool to create ONE image and save it as PNG to: {file} (in the current working directory). Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.

Image spec:
- {size}, solid flat MAGENTA background (#FF00FF) everywhere behind the drawings. No text, no letters, no numbers, no labels, no grid lines, no frames, no shadows on the background.
"""

FLAT = ("- STYLE IS THE MOST IMPORTANT RULE: copy the attached reference exactly: Untitled Goose Game flat look, completely FLAT color shapes, "
        "NO black outlines, NO linework, no gloss, no gradients, no glow, no drop shadows; a few flat color areas with one slightly darker flat tone; soft muted palette.\n")

# ── 구간 물건 (6열 x 3행 = 18칸: 1~10 일반 · 11~13 특수(값진 물건) · 14~18 가구) ──
ITEM_GRID = """- An exact grid of 6 columns x 3 rows (each cell 256x341). Each object centered in its own cell, standing on the bottom third of the cell, with a clear magenta margin; objects must NOT touch or cross cell borders. Fill each cell well (objects about 220px tall), furniture cells may be wider but still inside the cell.
- Soft 3/4 front view of a standing object, same scale feel as the reference lab objects (server rack, cage, desk, fridge). Each must read clearly at small size.
- Funny meme objects must be drawn as plain cartoon objects (no real people, no real logos, no written words unless described as a simple symbol).
"""

ZONES = {
    'z1_catlab': ('a CAT RESEARCH LAB inside a secret laboratory', [
        'a big ball of pastel yarn', 'a stack of three cat treat (churu) boxes, pink and white, no text', 'a sisal cat scratching post with a small round platform',
        'a single green cucumber lying on its side', 'a feather wand cat toy (a stick with a string and colorful feathers)', 'a glass lab beaker with little cat paw prints stamped on it',
        'an open grey laptop whose screen is full of tiny repeated scribbles (a cat walked on the keyboard), a cat paw print on the keys', 'an empty cardboard box exactly cat-sized, flaps open',
        'one slice of white bread with a perfectly round hole in the middle', 'a tipped-over flower vase lying on its side with a small water puddle and one flower',
        'a golden churu treat tube on a little velvet cushion (valuable)', 'a glass jar shaped with two cat ears on the lid, full of glowing-free golden liquid (valuable)', 'a golden toast-shaped pastry with a short rainbow-stripe tail behind it (valuable, like Nyan Cat)',
        'a tall multi-level cat tower with sisal posts, platforms and a hanging ball (big furniture)', 'a small white dining table with a salad plate on it and a white chair', 'a plastic pet carrier with a wire door',
        'a pink treat vending machine with small paw-shaped buttons (no text)', 'an office desk with a pair of bongo drums on top'],),
    'z2_dino': ('a NATURAL HISTORY / DINOSAUR earth-science laboratory (Jurassic Park mood)', [
        'a spiral ammonite fossil stone', 'a trilobite fossil plate', 'a plaster cast of a big three-toed dinosaur footprint',
        'a flat disc-shaped globe (flat earth model) on a small stand', 'an excavation pickaxe leaning on a small brush', 'a drop of amber with a mosquito inside on a small wooden stand',
        'a speckled dinosaur egg in a nest of straw', 'a small computer monitor showing a pixel-art grey dinosaur on a white screen', 'a plate with three big dinosaur-shaped chicken nuggets', 'a dinosaur skull fossil (raptor skull) on a block',
        'a golden dinosaur egg on a cushion (valuable)', 'a huge chunk of amber with gold bars trapped inside (valuable)', 'a glowing-free burning meteorite rock with small flat orange flames (valuable)',
        'a T-rex skeleton display on a low platform (big furniture, side view, compact)', 'a glass museum display case with small fossils inside', 'a wooden excavation work table with tools and a sieve',
        'a researcher desk with a lamp and papers', 'a small museum snack counter with a register (no text)'],),
    'z3_construction': ('a LAB EXPANSION CONSTRUCTION SITE (Korean construction site mood)', [
        'a yellow safety helmet with a simple thumbs-up sticker', 'an orange traffic cone with white stripes', 'a paper cement sack, grey, slumped',
        'a dented yellow aluminum pot (Korean yangeun pot)', 'a one-wheeled construction wheelbarrow', 'a pair of white cotton work gloves with red rubber palms',
        'a bundle of steel rebar tied with wire', 'a box of instant coffee mix sticks (yellow box, no text)', 'a small A-frame safety sign showing a big zero symbol and a hard hat pictogram (no letters)',
        'a safety poster on a stand showing a muscular shiba dog next to a droopy small shiba dog (no text)',
        'a golden ceremonial shovel with a red ribbon (valuable)', 'a golden rat statue ornament (valuable)', 'a thick yellow pay envelope stuffed with money (valuable)',
        'a site office desk whose corner is on fire with small flat flames while a coffee mug sits calmly on it', 'a metal scaffolding frame section with planks (big furniture)', 'a wooden pallet stacked with bricks and bags',
        'a small yellow mini excavator (big furniture, compact)', 'a canteen cart with a huge tower of stacked metal meal trays'],),
    'z4_ocean': ('an UNDERWATER MARINE research lab mixed with a FISH MARKET', [
        'a branch of pink coral', 'an orange starfish', 'a spiral conch shell',
        'a clay octopus pot with a tentacle peeking out', 'a white styrofoam box with a flatfish on ice', 'a red rubber basin with water and a fish tail',
        'a small chalkboard price sign with only a fish drawing and a question mark (no letters)', 'a singing fish trophy plaque on a wooden board, fish with mouth open', 'a red crab raising both claws up in a dance pose',
        'a glass specimen jar with a droopy pink blobfish inside',
        'a giant open clam with a big pearl (valuable)', 'a whole golden tuna on a small display stand (valuable)', 'a king crab with sparkling diamond gems on its shell (valuable)',
        'a large aquarium tank with water, seaweed and fish (big furniture)', 'a fish market stall with ice and fish on a slanted counter', 'a tall specimen shelf with jars',
        'a seafood chest freezer with a sliding glass lid', 'a coral culture tank with tubes and colorful corals'],),
    'z5_war': ('a WORLD WAR 2 MUSEUM (cartoon, neutral, NO flags, NO national or political symbols, NO swastika, NO real insignia)', [
        'a green steel army helmet', 'a wooden ammo crate with rope handles (no text)', 'an old military field radio with a handset',
        'a stack of olive ration tins', 'a small toy model of a propeller fighter plane', 'a bright red oil drum barrel',
        'a giant keyboard keycap with a big X letter on it', 'a poster on a stand: a cartoon rat flexing its biceps with a red polka-dot bandana (no text)', 'a poster on a stand: a cartoon rat in uniform pointing straight forward (no text)',
        'a white volleyball with a red handprint face wearing a little steel helmet',
        'a big golden medal on a ribbon (valuable)', 'a supply crate hanging under a small parachute (valuable)', 'a shiny golden frying pan (valuable)',
        'a stack of ammo crates in a small depot shelf (big furniture)', 'an operation map table with a map and small wooden markers (no symbols)', 'a field kitchen stove with a big pot',
        'an olive green army jeep (big furniture, compact)', 'a souvenir stand with small toy planes and helmets'],),
    'z6_space': ('a NASA-like SPACE research center', [
        'a white fuel canister with a hazard stripe', 'a small rocket model on a stand', 'a small planet model with rings on a stand',
        'freeze-dried space food pouches', 'a cone-shaped hat folded from tinfoil', 'a small telescope on a tripod',
        'a small satellite dish antenna', 'a white astronaut helmet with gold visor', 'a red telephone with a curly cord',
        'a control monitor showing a green line graph shooting steeply upward',
        'a glowing-free moon rock in a glass case (valuable)', 'a golden record disc (valuable)', 'a tall black rectangular monolith slab with a faint light edge (valuable)',
        'a mission control console with many buttons and screens (big furniture)', 'a small rocket launch pad with a gantry (big furniture, compact)', 'a white zero-gravity training capsule',
        'a planetarium star projector ball on a stand', 'a bookshelf with a few books sticking out unevenly'],),
}

# ── 사람 (5열: 머리 · 겁먹은 머리 · 몸통 · 팔 · 다리 / 한 줄 = 한 사람, 4줄) ──
HUMAN_RULES = """- Exact layout: 4 rows x 5 columns (each cell 307x256). Each ROW is one person. Columns left to right: 1) NORMAL HEAD in strict side view facing LEFT, ending in a short narrow neck stump cut flat at the bottom; 2) SCARED HEAD (same person, mouth open screaming, eyebrows up, sweat drops); 3) TORSO seen from the side, no head, no arms, no legs, neck stump on top; 4) ONE ARM hanging straight down, top is a rounded shoulder cap of the sleeve fabric; 5) ONE LEG standing, top cut flat, shoe at the bottom. Every part is a separate island with clear magenta space; nothing crosses into other cells.
- Copy the attached reference parts sheet exactly for part shapes, proportions, layout, side view and flat paper-doll style (researcher, guard, janitor). Same head size and same body proportions.
"""
HUMANS = {
    'h1_catlab': ['a cat-loving lab researcher covered in cat hair: messy hair with a few cat hairs, white lab coat covered in grey fur tufts, lint roller in the hand',
                  'a veterinarian in teal scrubs with a stethoscope', 'a pet food delivery man with an orange cap and orange work jacket, carrying nothing',
                  'an allergic researcher wearing a huge white face mask and goggles, red puffy eyes, white lab coat'],
    'h2_dino': ['a paleontologist with a wide-brim safari hat, khaki shirt and khaki pants', 'a dinosaur handler in a dark green ranger uniform vest with a whistle',
                'a field-trip kid with a yellow cap and a small backpack strap, short pants, small sneakers (child proportions but same part layout)',
                'a chubby computer programmer with glasses, short beard and a hawaiian shirt, wagging finger pose head (normal head looks smug)'],
    'h3_construction': ['an old white-haired site manager forcing a painful smile (normal head), white helmet, light blue work shirt', 'a traffic flagger in a neon yellow reflective vest and white helmet',
                        'a construction worker in a navy work jacket with a towel around the neck, holding nothing', 'a rebar worker with a red helmet, grey work clothes and knee pads'],
    'h4_ocean': ['a marine biologist in a blue research windbreaker with a lanyard', 'a scuba diver in a black wetsuit with a diving mask pushed up on the forehead',
                 'a fish market auctioneer with a white towel headband, green rubber apron and tall boots', 'a sushi restaurant owner with slick black hair, sunglasses, white tight t-shirt (salt bae vibe)'],
    'h5_war': ['an elderly veteran museum guide with a beret, grey cardigan and small medals', 'a costume reenactor in an olive drab vintage soldier uniform (no insignia) with a field cap',
               'a field-trip student in a navy school uniform blazer', 'a radio operator in olive uniform with big headphones'],
    'h6_space': ['a mission controller with a headset, white short-sleeve shirt and a tie', 'an astronaut in a white space suit, helmet off (head shows short hair)',
                 'an astronaut in a white space suit holding nothing, wearing a cap, a film-set worker vibe', 'a balding flight director with a vest, coffee stains on the shirt'],
}

# ── 사람 보스 (6열: 머리 · 화난 머리 · 겁먹은 머리 · 몸통 · 팔 · 다리 / 3줄) ──
HBOSS_RULES = """- Exact layout: 3 rows x 6 columns (each cell 256x341). Each ROW is one BOSS person (bigger, bulkier, more imposing than normal people). Columns: 1) NORMAL HEAD side view facing LEFT with a short narrow neck stump cut flat; 2) ANGRY SHOUTING head; 3) SCARED head (screaming, sweat drops); 4) TORSO side view, no head/arms/legs; 5) ONE ARM hanging down (rounded sleeve shoulder cap, no cut section); 6) ONE LEG standing, top cut flat. Separate islands, nothing crosses cells.
- Copy the attached references exactly for style and part layout (flat paper-doll human parts).
"""
HBOSS = {
    'hb1': ['a CAVEMAN boss: wild brown hair and beard, bone hair pin, leopard-pattern fur tunic, bare arms and legs, bare feet',
            'a RAPTOR KEEPER boss: short brown hair, rugged face, dark brown leather vest over a grey henley shirt, khaki cargo pants, boots',
            'a PART-TIMER in an INFLATABLE T-REX COSTUME boss: the head is a big green inflatable T-rex head with the person face visible through a mesh window in the neck; torso = round inflated green dinosaur belly; arm = tiny short green dinosaur arm; leg = thick green inflatable dino leg'],
    'hb2': ['a KOREAN CONSTRUCTION WORKER boss "Mr. Kim": yellow safety helmet, tanned face, thick eyebrows, towel around neck, navy work jacket with reflective stripes, work boots',
            'a KOREAN CANTEEN AUNTIE boss: short curly permed hair, red headscarf, pink apron over a floral shirt, rubber gloves, black slippers',
            'a RED SPACESUIT CREWMATE boss: head = round red helmet with a wide pale blue visor (no face visible), torso = puffy red spacesuit with a small backpack, arm = puffy red sleeve, leg = short puffy red leg'],
}

# ── 고양이 보스 (5열: 머리 · 몸통 · 꼬리 · 앞다리 · 뒷다리 / 한 줄 = 한 마리) ──
CBOSS_RULES = """This is a cut-out PARTS SHEET for a 2D game rig (the parts are assembled by code, so each part must be separate and complete).
- Exact layout: {rows} rows x 5 columns. Each ROW is one boss cat. Columns left to right: 1) HEAD in side profile facing LEFT (neck cut flat at the bottom), 2) TORSO seen from the side, horizontal, head end on the LEFT, no head, no legs, no tail, 3) TAIL starting at its left end (base) and curving up to the right, 4) FRONT LEG standing straight, top cut flat, paw at the bottom, 5) BACK LEG standing, top cut flat, paw at the bottom. Every part is a separate island with clear magenta space around it; parts must not touch or cross into other cells.
- Match the attached reference exactly for part shapes, proportions, layout and view angle (maine coon, witch cat, space cat bosses): big, bulky, imposing boss cats.
"""
CBOSS = {
    'cb1': ['"Happy Cat": a fluffy white cat with a small pink tongue sticking out and happy closed eyes; plain white fluffy torso; white fluffy tail; white legs',
            '"Pop Cat": an orange-and-white short hair cat head with a huge perfectly round open mouth (O shape), wide eyes; orange tabby torso; orange tail; orange legs with white paws',
            '"Union Cat": a grey tabby cat with a red protest headband (no text) and a lazy sleepy face; torso wearing a red union vest; grey tabby tail; grey legs'],
    'cb2': ['"Mermaid Cat": a cream cat head with a pink seashell hair clip; torso = cat chest with a seashell top changing into teal fish scales; tail = big teal fish tail fin; front leg = cream cat leg; back leg = small teal fin shaped like a leg',
            '"Diver Cat": head inside an old brass deep-sea diving helmet with a round glass window showing a ginger cat face; torso = thick canvas diving suit with brass collar; tail = ginger tail in a canvas sleeve; legs = heavy diving boots',
            '"Shark Cat": head = grey shark head with big friendly teeth and two small cat ears; torso = grey shark body with white belly and a dorsal fin shaped like a pointed cat ear; tail = shark tail fin; legs = short grey fins shaped like cat legs with paws'],
    'cb3': ['"Turret Cat": a chunky olive-green cat with a small toy tank turret strapped on its back pointing the wrong way (turret is part of the torso), head with a confused face and a small army helmet; olive tail; olive legs',
            '"Private Lion Cat": a cat with a big golden lion mane wearing an olive army helmet (no insignia), proud face; torso in an olive soldier jacket (no insignia); tail with a lion tuft; legs with army boots',
            '"Bunker General Cat": a black-and-white tuxedo cat with a small black mustache-shaped fur marking under the nose and black fur combed to one side, furious face, holding round glasses is NOT needed; torso in a plain grey long officer coat with buttons (absolutely no insignia, no armband, no symbols); grey tail; grey legs with black boots'],
    'cb4': ['"Laser Eyes Cat": a dark navy cat with starry galaxy-pattern fur and glowing-free bright red eyes, intense face; galaxy-pattern torso; galaxy tail; galaxy legs',
            '"Grumpy Space Commander": a cat with a very grumpy face with downturned mouth (grumpy cat look), cream and brown fur, wearing a navy commander space helmet ring with a gold star badge; torso in a navy and gold commander space suit with a short red cape; cream tail; armored navy legs with white paws'],
}

# ── 밈 기술 소품 (6열 x 3행, 그림 하나씩) ──
PROP_GRID = """- An exact grid of 6 columns x 3 rows (each cell 256x341). Each prop is one separate object centered in its cell with a wide magenta margin, about 200px big, not touching cell borders.
- These are cartoon effect props for meme boss attacks in a funny rat game. Copy the attached reference props exactly for flat style and scale feel (coffin, maxwell cat, shark with shoes, money, banana cat, torch).
"""
PROPS = {
    'mp1': ['a pair of black music notes', 'a small white happy cat jumping with its tongue out', 'a round pale-blue sound wave shockwave ring (flat, two concentric arcs)',
            'a big white cartoon mouse-pointer hand cursor clicking (pointing finger)', 'a grey round rock', 'a wooden torch with a flat orange flame',
            'a small brown monkey walking on all fours', 'a huge flaming meteor rock falling diagonally (flat flames)', 'a small green velociraptor running, side view facing left',
            'a tranquilizer dart with a red fluffy tail', 'a yellow excavator bucket scoop, teeth pointing down', 'a flat grey wet concrete puddle seen from above (oval)',
            'a protest picket sign on a wooden stick (blank sign with a big red fist pictogram)', 'a red and white megaphone', 'a sleepy cloud with big Z Z Z shapes',
            'a Korean steel rice bowl with a lid', 'a gigantic heaping mountain of white rice in a steel bowl', 'a big soup ladle'],
    'mp2': ['a shiny round soap bubble', 'a big curling blue ocean wave', 'a big silver mackerel fish',
            'a heavy iron ship anchor', 'a flat black ink splat puddle seen from above (oval)', 'a cute small yellow baby shark swimming, facing left',
            'a grey shark dorsal fin sticking out of a small water ripple', 'a grey tornado funnel with small sharks inside', 'a brass artillery shell standing up',
            'a wooden supply crate under a white parachute', 'a yellow pencil', 'a square metal floor vent grate seen at an angle',
            'a red "SUS" style alarm badge: a red circle with a white exclamation mark', 'a big silver wrench', 'a big pale grey full moon with craters',
            'a yellow safety helmet with a thumbs-up sticker', 'a pair of sunglasses (deal with it pixel style but flat)', 'a bright red stop hand sign on a stick'],
}

# ── 훈장 9~16 + 새 스킬 아이콘 (4열 x 3행) ──
BADGES = ['an underground empire governor badge: a dark stone shield with a golden manhole cover emblem and small gold wings',
          'a transcended subject badge: a purple hexagon medal with a glowing-free white rat silhouette and a DNA helix',
          'a lab occupier badge: a red banner shield with a golden flag planted on a beaker',
          'a city shadow badge: a navy round medal with a silver city skyline and a rat silhouette on a rooftop',
          'a rat republic chairman badge: a green laurel round medal with a golden gavel and a cheese',
          'a planet escaper badge: an orange round medal with a small rocket leaving a planet',
          'a galaxy wanderer badge: a dark violet star-shaped medal with a spiral galaxy',
          'a mythic squeak badge: a big radiant golden crown medal with rainbow gems and a rat crest, the most glorious one',
          'a skill icon: two rats with circular arrows (auto promote)', 'a skill icon: a rat silhouette with a down arrow and minus (fewer needed)',
          'a skill icon: two identical rats sparkling (double promote)', 'a skill icon: a staircase with a fast-forward arrow (stage skip)']
BADGE_GRID = """- An exact grid of 4 columns x 3 rows (each cell 384x341). Each badge/icon centered in its cell, about 280px, not touching cell borders.
- Copy the attached reference rank badges exactly for style (flat medals, shields and ribbons). Row 1-2 are 8 NEW higher rank badges (9 to 16), each more glorious than the last. Row 3 are 4 round skill icons in a matching flat style (simple round medallion with a symbol).
"""

FLOORS = ['a cat research lab floor: pale mint linoleum tiles with a few faint paw print marks',
          'a natural history museum floor: warm sandstone tiles with faint fossil imprints',
          'a construction site floor: grey concrete with a few cracks and faint yellow chalk marks',
          'an underwater lab floor: blue-teal wet tiles with small sand patches and faint bubbles',
          'a war museum floor: dark wooden plank floor',
          'a space center floor: pale grey metal panels with rivets and faint grid lines']
WALLS = ['cat lab wall: white tiles with a pastel pink band and tiny paw print pattern',
         'museum wall: beige stone wall with a dark wood baseboard',
         'construction wall: plywood boards with a yellow-black hazard stripe band',
         'underwater lab wall: steel wall with round porthole windows showing blue water',
         'war museum wall: olive painted wall with a dark green wainscot',
         'space center wall: white panels with a navy stripe and small blinking light dots']


def write(name, text):
    os.makedirs(OUT, exist_ok=True)
    io.open(os.path.join(OUT, f'prompt_{name}.txt'), 'w', encoding='utf-8').write(text)


def grid_list(items, cols):
    lines = []
    for i, d in enumerate(items):
        if i % cols == 0: lines.append(f'Row {i // cols + 1}:')
        lines.append(f' {i + 1}) {d}')
    return '\n'.join(lines)


def main():
    for k, (desc, items) in ZONES.items():
        write(k, HEAD.format(file=f'{k}.png', size='1536x1024') + ITEM_GRID + FLAT +
              f'- Theme: objects found in {desc}. Cells 1-10 small breakable objects, 11-13 valuable shiny treasures, 14-18 furniture (bigger, sturdy).\n\n' + grid_list(items, 6) + '\n')
    for k, people in HUMANS.items():
        write(k, HEAD.format(file=f'{k}.png', size='1536x1024') + HUMAN_RULES + FLAT + '\nRows:\n' + '\n'.join(f'Row {i + 1}: {p}' for i, p in enumerate(people)) + '\n')
    for k, bosses in HBOSS.items():
        write(k, HEAD.format(file=f'{k}.png', size='1536x1024') + HBOSS_RULES + FLAT + '- Strict SIDE VIEW, the character FACES LEFT.\n\nRows:\n' + '\n'.join(f'Row {i + 1}: {p}' for i, p in enumerate(bosses)) + '\n')
    for k, cats in CBOSS.items():
        rows = '\n'.join(f'Row {i + 1}: {p}' for i, p in enumerate(cats))
        if len(cats) < 3: rows += '\nRow 3: leave completely EMPTY magenta (nothing drawn).'
        write(k, HEAD.format(file=f'{k}.png', size='1536x1024') + CBOSS_RULES.format(rows=3) + FLAT + '\nRows:\n' + rows + '\n')
    for k, props in PROPS.items():
        write(k, HEAD.format(file=f'{k}.png', size='1536x1024') + PROP_GRID + FLAT + '\n' + grid_list(props, 6) + '\n')
    write('badges', HEAD.format(file='badges.png', size='1536x1024') + BADGE_GRID + FLAT + '\n' + grid_list(BADGES, 4) + '\n')
    write('floors', HEAD.format(file='floors.png', size='1536x1024') +
          '- An exact grid of 3 columns x 2 rows (each cell 512x512). Each cell is completely FILLED edge to edge by one flat top-down floor texture (no magenta inside the cells, thin magenta gutters of 8px between cells are fine). Seen straight from above, tile-able look, low detail, soft muted colors.\n'
          '- Copy the attached reference floor textures exactly for style, tile size and calmness (they are backgrounds; keep contrast low so characters stay readable).\n\n' + grid_list(FLOORS, 3) + '\n')
    write('walls', HEAD.format(file='walls.png', size='1536x1024') +
          '- 6 horizontal wall strips stacked from top to bottom, each strip FULL WIDTH (1536 wide) and about 150px tall, separated by magenta gaps of about 20px. Each strip is a flat front-view wall face like the attached reference strip (top light edge, repeating wall pattern, bottom darker baseboard). Repeating horizontally, no objects, no doors.\n'
          '- Copy the attached reference wall strip exactly for style and proportion.\n\n' + '\n'.join(f'Strip {i + 1}: {w}' for i, w in enumerate(WALLS)) + '\n')
    print('prompts →', OUT)


if __name__ == '__main__':
    main()
