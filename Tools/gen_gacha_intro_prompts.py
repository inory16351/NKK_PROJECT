"""쥐 고유 컷씬(뽑기, 신화) 프롬프트 · 참고 그림 만들기 → UnityResources/Rats/GachaIntro/<code>/Sheets/
사용: python Tools/gen_gacha_intro_prompts.py [code ...]   (없으면 전부)
그 뒤 Tools/run_gacha_intro_codex.sh 로 Codex 실행. 컷 구성 = ① 배경·상황 ② 대표 기술 장면 ③ 클로즈업 포즈 (아이스 퀸과 같은 틀)"""
import os, sys, shutil
from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), '..', 'UnityResources', 'Rats')
STYLE = os.path.join(ROOT, 'GachaIntro', 'icequeen', 'Sheets', 'ref_style.png')

COMMON = """- 1536x1024 full-frame cutscene illustration (no transparency, no panels, no borders).
- STYLE: exactly the style of the attached ref_style (cozy flat cartoon storybook game art, Untitled Goose Game mood): completely FLAT colors, NO black outlines, no gloss, soft simple shapes, muted palette, soft lighting.
- CHARACTER: {char} The character must look exactly like the attached ref_parts (its head, body and outfit parts from the game), with the same proportions as the game rats (big head, small body), pink ears, pink nose, black bead eyes, long pink tail (unless stated otherwise).
- NO text, NO letters, NO numbers, no speech bubbles, no logos anywhere. {extra}"""

HEAD = """Use your built-in image generation tool to create ONE image and save it as PNG to: {file} (in the current working directory). Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.

Image spec:
- SCENE: {scene}
"""

# code: (캐릭터 묘사, 추가 규칙, [컷1, 컷2, 컷3])
RATS = {
    'zapham': ("\"Zap Hamster\": a chubby golden hamster wearing a yellow electric-mouse COSPLAY HOOD with long pointed ears with brown tips, red round cheek marks, and a big AA BATTERY strapped on its back with a red cable.",
               "Only this one character.",
               ["night thunderstorm on the rooftop of a toy shop; the hamster stands seen from BEHIND at the roof edge, hood ears up, the big battery on its back glowing yellow, a bright lightning bolt cracks across the purple sky, rain streaks. Wide establishing shot.",
                "medium shot: the hamster plugs the red cable from its back battery into its cheek, crackling yellow electricity bursts all around, fur standing on end, eyes squeezed shut with a determined grin, little sparks everywhere, dark blue background.",
                "heroic CLOSE-UP facing the viewer: the hamster makes a peace sign with a cheeky wink, its fur puffed into a frizzy electric afro under the hood, a big yellow zigzag lightning-bolt shape glowing behind it like a halo, sparks. Simple yellow-to-orange radial background."]),
    'parkrat': ("\"Rat-land Tourist\": a grey mouse wearing a souvenir headband with two big round black ears, heart-shaped red sunglasses, a blue-white striped shirt and a pink fanny pack.",
                "Small background characters are allowed only where the scene asks.",
                ["night at a fairy-tale theme park: a pastel castle with pointy towers, colorful fireworks bursting above; the mouse seen from BEHIND holding a bunch of colorful balloons, looking up at the castle. Wide establishing shot.",
                 "a parade: the mouse marches in front waving a little flag and blowing a whistle, followed by a line of tiny rats playing drums and trumpets, confetti and streamers flying, bright carnival lights.",
                 "heroic CLOSE-UP facing the viewer: the mouse proudly points at its round-ear headband with a smug face, sunglasses pushed up on the head, sparkles, the castle silhouette and fireworks forming a halo behind. Simple pink-to-purple radial background."]),
    'emperor': ("\"Rat Emperor\": a grey rat wearing a red and gold imperial hat with a golden knob on top and a yellow imperial robe decorated with blue cloud patterns and a round golden dragon emblem.",
                "Small background rat soldiers are allowed only where the scene asks.",
                ["a grand underground sewer throne hall lit by torches: rows of tiny rat soldiers kneel on both sides of a long red carpet, three round MANHOLE COVERS hang on the far wall like royal banners; the emperor seen from BEHIND walking up the carpet to a throne made of tin cans. Wide establishing shot.",
                 "the emperor stands on a balcony raising a golden scepter, behind him dozens of firework rockets launch into the sewer dome sky in all directions, sparks and smoke trails, rat archers firing below.",
                 "heroic CLOSE-UP facing the viewer: the emperor sits on the throne with a dignified smug smile, holding the scepter, a huge golden MANHOLE COVER glowing behind his head like a sun halo with golden rays. Simple red-to-gold radial background."]),
    'pharaoh': ("\"Pharaoh Mouse\": a light grey-brown mouse wearing a blue and gold striped pharaoh NEMES headdress with a golden cobra on the forehead.",
                "Small background rats are allowed only where the scene asks.",
                ["desert at sunset: a giant PYRAMID made of stacked yellow CHEESE blocks rises against a huge orange sun; the pharaoh mouse stands at its very top, seen from BEHIND, arms spread. Wind blowing sand. Wide establishing shot.",
                 "on the pyramid steps tiny rats carry cheese wedges up as offerings; the pharaoh raises one paw that glows GOLDEN, turning a cheese block into shining gold, golden sparkles swirling.",
                 "heroic CLOSE-UP facing the viewer: the pharaoh holds a crook and flail crossed over the chest, eyes half-closed with a royal smug smile, a big golden SUN DISK with rays glowing behind like a halo. Simple turquoise-to-gold radial background."]),
    'knight': ("\"Knight Rat\": a grey rat in heavy silver PLATE ARMOR with a knight helmet with visor and a big red plume, a brown leather belt.",
               "Only this one character.",
               ["a misty forest clearing at dawn: a legendary sword stuck in a mossy BOULDER glows with a beam of light from the sky; the armored rat approaches from BEHIND, clanking, tiny compared to the stone. Wide establishing shot.",
                "the rat pulls the sword with all its might and the WHOLE BOULDER comes out still attached to the blade, lifted high above its head, the rat straining comically, pebbles and dirt flying, light rays.",
                "heroic CLOSE-UP facing the viewer: the rat with visor up charges forward holding a long LANCE with a red pennant, determined grin, speed lines of light behind, a big shield emblem of a cheese wedge glowing as a halo. Simple sky-blue-to-white radial background."]),
    'sultan': ("\"Sultan Hamster\": a chubby golden hamster wearing a white and blue TURBAN with a red jewel and a white feather, and a red velvet cape with gold trim and jewels.",
               "A blue smoky genie is allowed only where the scene asks.",
               ["night desert: a fairy-tale palace whose domes are shaped like giant SUNFLOWER SEEDS, glowing windows, a big moon; the hamster flies past on a MAGIC CARPET, seen from behind. Wide establishing shot.",
                "the hamster rubs a golden MAGIC LAMP and a huge friendly blue smoky GENIE swirls out of it in a spiral of sparkles, gold coins and gems spinning around in a whirlwind.",
                "heroic CLOSE-UP facing the viewer: the hamster's cheeks are hugely puffed with treasure, gold coins and gems spilling out of its mouth, smug happy closed eyes, gold coins raining down, a golden ornate arch glowing behind. Simple purple-to-gold radial background."]),
    'alien': ("\"Alien Mouse\": a pale GREEN mouse with two thin antennae with little round tips on its head, big black eyes, green body.",
              "Small green alien friends and a flying saucer are allowed only where the scene asks.",
              ["night above a quiet suburban house: a round FLYING SAUCER hovers in a starry sky shining a green tractor BEAM down onto a tiny mouse hole in the wall; the green mouse is seen from BEHIND floating up inside the beam. Wide establishing shot.",
               "inside a kitchen bathed in green light: the alien mouse aims a toy RAY GUN, furniture, a toaster and cups float up into the air in the beam, three tiny green alien friends cheer beside it.",
               "heroic CLOSE-UP facing the viewer: the alien mouse wears a round glass BUBBLE HELMET, strikes a cool pose with the ray gun, antennae glowing, a flying saucer and stars arranged as a halo behind. Simple dark-violet-to-green radial background."]),
    'streamrat': ("\"Streamer Rat\": a brown rat wearing round GLASSES, a green HOODIE with the hood up, big headset headphones with a mic and a tiny webcam on top of the head.",
                  "Only this one character.",
                  ["a dark cozy streamer bedroom at night: three glowing monitors, LED strip lights in pink and blue, a ring light; the rat sits in a big gaming chair seen from BEHIND, headset on, the monitors lighting the room. Wide establishing shot.",
                   "a donation alert EXPLOSION bursts out of the monitor toward the viewer: hearts, coins and confetti; the rat leans back laughing with its mouth wide open, glasses flashing WHITE, paws up.",
                   "heroic CLOSE-UP facing the viewer: the rat's glasses glint with a white flash, crossed TWO toy water pistols in its paws, a confident grin, a big red round 'live' recording dot shape glowing behind like a halo (no letters), neon sparkles. Simple pink-to-blue radial background."]),
    'ramjui': ("\"Squirrel\": a cute orange-red SQUIRREL (not a rat) with big pointy tufted ears, cream belly and a huge fluffy orange tail.",
               "Only this one character.",
               ["a calm sunny forest: the squirrel sits on a tree stump nibbling an acorn, very ordinary and peaceful, BUT on one side of the sky dark storm clouds are rolling in with a faint flash. Wide establishing shot.",
                "the sky is dark: the squirrel raises one front paw high and a BLUE-WHITE LIGHTNING BOLT strikes down into it, its paw crackles with electricity, eyes glowing, tail fur standing up, leaves blasting away.",
                "HERO LANDING CLOSE-UP facing the viewer: the squirrel lands on one knee with one fist on the cracked ground, a giant pillar of blue lightning behind, electric ripples spreading on the ground, fierce cool face. Simple dark-blue radial background with lightning."]),
    'jwerry': ("\"Jwerry\": a round chubby orange-brown MOUSE with a cream face and belly, big round pink ears and a mischievous grin (classic cartoon cat-and-mouse style mouse).",
               "A big grey CAT is allowed only where the scene asks.",
               ["a kitchen floor at night: a big grey cat sleeps in its basket; the round mouse peeks out of a mouse hole in the baseboard with a mischievous grin, holding a feather. Wide establishing shot.",
                "chaos: the mouse runs off carrying a big cheese wedge while the grey cat behind it has slipped on the floor with a frying pan stuck on its face, cups and plates flying, the mouse laughing.",
                "CLOSE-UP on a tiny stage under a warm spotlight: the mouse takes a deep shameless 90-DEGREE BOW with one paw on its chest, red rose petals falling around, a red curtain behind. Simple warm red background."]),
    'starchef': ("\"Star Chef Rat\": a blue-grey rat wearing a tall white CHEF HAT with three little gold stars, a white double-breasted chef jacket with a red neckerchief.",
                 "A human cook (only seen partly, no detailed face) is allowed only where the scene asks.",
                 ["a busy restaurant kitchen at night full of steam and copper pots: a human cook seen from BEHIND stands at the stove, and under the cook's tall toque, the tiny chef rat peeks out from the cook's hair. Wide establishing shot.",
                  "the chef rat sits on top of the cook's head pulling his hair like reins, and the cook's hands chop super fast, vegetables and cheese flying through the air, flames burst from a pan, steam swirls.",
                  "heroic CLOSE-UP facing the viewer: the chef rat proudly presents a giant golden glowing bowl of cheese dish with steam, one paw holding a ladle up, eyes closed smelling it happily, sparkles, golden plates halo behind. Simple warm orange radial background."]),
    'jjdetective': ("\"Squeak Detective\": a brown rat with messy hair tuft, round GOLD GLASSES, a navy blue blazer and a red BOW TIE.",
                    "Only this one character.",
                    ["a foggy city alley at night under a single streetlamp: the detective rat seen from BEHIND holds a magnifying glass over tiny footprints in a puddle, long shadow. Wide establishing shot.",
                     "the detective rat KICKS a soccer ball with glowing power shoes, the ball streaks forward leaving a bright trail, ricocheting off cans and crates, dust and speed lines.",
                     "dramatic CLOSE-UP facing the viewer: the detective points a finger straight at the viewer, glasses flashing white, confident sharp smile, a dramatic dark-blue background with a white flash streak behind. Simple dark blue radial background."]),
    'fondue': ("\"Cheese Fondue Rat\": a grey rat with melted yellow CHEESE dripping on its head, eyes happily closed, wearing a teal HAWAIIAN SHIRT with pink flowers.",
               "Only this one character.",
               ["a tropical beach at sunset with palm trees: a giant fondue pot like a little volcano bubbling with melted cheese on the sand; the rat seen from BEHIND lounging on a beach chair facing it. Wide establishing shot.",
                "the rat SURFS on a huge wave of melted yellow cheese, arms out, cheese fountains spraying everywhere, droplets of cheese flying, bright sky.",
                "heroic CLOSE-UP facing the viewer: the rat spins in a golden TORNADO of melted cheese, holding a long fondue fork high, happy grin, cheese splashes swirling around. Simple orange-to-yellow radial background."]),
    'bomber': ("\"Bomber Rat\": a dark grey rat with a black EYE PATCH (with a small red cross), a sharp fang, wearing a black bomb VEST with round bombs and a bullet belt.",
               "Only this one character.",
               ["a rocky wasteland at dusk: a glowing RED MAGIC CIRCLE spreads on the ground in front of the rat; the rat stands seen from BEHIND with one arm raised, a dark cape flapping, chanting, red light rising. Wide establishing shot.",
                "a GIANT EXPLOSION with a huge mushroom cloud of fire and smoke fills the background, the rat in the foreground with both arms raised in triumph, silhouetted against the blinding orange light, rocks flying.",
                "CLOSE-UP: after the explosion the rat lies face-down flat on the ground, totally exhausted but grinning with satisfaction and giving a thumbs-up, little smoke puffs rising, embers floating. Simple warm orange background."]),
}


def main():
    codes = sys.argv[1:] or list(RATS)
    for code in codes:
        char, extra, scenes = RATS[code]
        d = os.path.join(ROOT, 'GachaIntro', code, 'Sheets'); os.makedirs(d, exist_ok=True)
        shutil.copy(STYLE, os.path.join(d, 'ref_style.png'))
        # 참고: 게임 파츠 한 줄 (흰 배경)
        parts = [Image.open(os.path.join(ROOT, 'Parts', code, p + '.png')).convert('RGBA') for p in ('head', 'torso', 'front', 'back', 'tail')
                 if os.path.exists(os.path.join(ROOT, 'Parts', code, p + '.png'))]
        for p in parts: p.thumbnail((360, 330))
        sheet = Image.new('RGBA', (sum(p.width for p in parts) + 30 * len(parts), 340), (255, 255, 255, 255))
        x = 15
        for p in parts: sheet.alpha_composite(p, (x, (340 - p.height) // 2)); x += p.width + 30
        sheet.convert('RGB').save(os.path.join(d, 'ref_parts.png'))
        common = COMMON.format(char=char, extra=extra)
        open(os.path.join(d, '_common.txt'), 'w', encoding='utf-8').write(common)
        for i, sc in enumerate(scenes, 1):
            f = f'{code}_c{i}.png'
            open(os.path.join(d, f'prompt_{code}_c{i}.txt'), 'w', encoding='utf-8').write(HEAD.format(file=f, scene=sc) + '\n' + common + '\n')
        print(code, 'ok')


if __name__ == '__main__':
    main()
