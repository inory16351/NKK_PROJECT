"""초필살기 레이어 컷신 그림 프롬프트 (신화 14종, 햄찌는 손으로 만든 예시) → UnityResources/Rats/SuperUlt/<code>/Sheets/
틀 (사용자 2026-10-17: 햄찌 v3 방향 · 구도·액션·그림은 쥐마다 다르게 · 밈):
  ② 큰 세로 파노라마(pano_a 위 → b → c 아래, 이어 그림) 안을 카메라가 움직이며 pose_intro → 얼굴 클로즈업 face_close
  ③ 위쪽 띠 안 pose_act + 아래 게임 화면으로 fx 'strike' 가 내리꽂힘(impact)
  ④ 파노라마로 돌아와 pose_finish + 밈 (props 'meme' 가 날아가고 밈 글)
그림 (쥐마다): pano_a/b/c (1536×1024, 이어 그림) · [pano_?_alt 상태 바뀐 판 — 있는 쥐만] · pose_intro/act/finish (1024×1536 자홍)
            · face_close (1536×1024) · fx.png (검정 3×2: FX 역할 순) · props.png (자홍 3×2: PROPS 역할 순)
사용: python Tools/gen_superult_prompts.py [code ...]  → 프롬프트 · 참고 그림 · run.sh  (실행 = bash Tools/run_superult_codex.sh <동시 수> <code...>)"""
import json, os, shutil, sys
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
RATS_DIR = os.path.join(ROOT, 'UnityResources', 'Rats')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gen_gacha_intro_prompts import RATS as GACHA

FX = ['strike', 'impact', 'ring', 'streak', 'burst', 'aura']        # fx.png 칸 순서 (역할)
PROPS = ['meme', 'pop', 'float1', 'float2', 'band', 'fg']           # props.png 칸 순서 (역할)

CHAR = dict((k, v[0]) for k, v in GACHA.items())
CHAR['icequeen'] = ('"Ice Queen Rat": a cute MOUSE with blue-grey fur, big pink round ears, pink nose, black bead eyes, a small white snowflake mark on the cheek, '
                    'a sparkling pale-blue ICE CRYSTAL CROWN, a long flowing WHITE and pale-blue CAPE with a snowflake clasp, long pink tail. Elegant, confident, a little smug.')

# code: 파노라마 (위 · 가운데 · 아래 — 아래 c 에는 캐릭터가 설 빈 바닥) · alt(상태 바뀐 판, 없으면 None) · 포즈 3 · 클로즈업 · FX 6 (역할 순) · 소품 6 (역할 순) · 밈 글 · 피니시 자막 보조
S = {
 'parkrat': dict(
   orient='h',
   a='the theme park ENTRANCE: a big pastel gate with balloons, ticket booths and a VERY LONG QUEUE LINE of tiny rat visitors snaking back and forth (meme: endless line)',
   b='the main PARADE STREET in front of a pastel fairy-tale castle, carousel and roller coaster in the background, strings of bulbs, balloons',
   c='the end of the street at a lake with a big ferris wheel and huge colorful FIREWORKS bursting in the night sky',
   alt=None,
   intro='marching proudly while waving a small flag and blowing a whistle, one knee up, holding a bunch of colorful balloons',
   act='flying up joyfully hanging from a big bunch of colorful balloons, kicking the legs, waving one paw, sparkles',
   finish='cheeky victory pose: one paw pushing the heart sunglasses up, the other paw holding a sparkler wand that sprays golden sparks, big grin',
   face='pulling the heart-shaped red sunglasses down with one finger, one eye winking with a star glint, smug grin, firework colors reflected in the lenses',
   fx=['a single colorful FIREWORK SHELL streaking straight DOWN with a long sparkling tail (vertical)', 'a big round FIREWORK BURST (chrysanthemum, gold and pink)', 'a ring of CONFETTI and streamers',
       'a horizontal rainbow sparkle trail', 'a big pink-gold starburst flash', 'a circle of floating hearts and stars around an empty center'],
   props=['a big crumpled theme-park TICKET with a heart stamp and scribble lines instead of words', 'a single colorful BALLOON', 'a round-ears souvenir HEADBAND',
          'a parade float shaped like a giant cake on wheels', 'a little marching-band rat drummer (tiny side character)', 'a strip of colorful bunting flags hanging in a curve'],
   meme='※ 줄 서기 3시간, 퍼레이드 3초', counter=None),
 'emperor': dict(
   a='a golden dawn sky with long red and gold imperial BANNERS waving, cannon smoke puffs, golden firework sparks; at the bottom the curved roofs of an oriental palace with golden ornaments rise in',
   b='a grand ORIENTAL PALACE seen from above: layered red pillars, golden curved roofs, a huge courtyard where rows of tiny rat soldiers stand in formation, red carpets',
   c='the palace main hall stairs: wide stone stairs with a red carpet leading up, two big bronze cannons at the sides, gold lanterns; the lower-middle stair landing is empty and clear',
   alt=None,
   intro='walking majestically with the long robe trailing, one paw behind the back, chin up, smug',
   act='holding up a big golden CROWN high over its head with both paws while rising into the air, golden light rays',
   finish='sitting on a throne made of tin cans and golden cushions, pointing forward with an arrogant smile',
   face='extreme close-up of a smug imperial face, one eyebrow raised, golden light glinting in the eye, imperial hat tassels',
   fx=['a heavy golden CANNONBALL with fire trail falling straight DOWN (vertical)', 'a round golden EXPLOSION burst with red sparks', 'a golden shockwave ring with dragon-scale pattern sparks',
       'a long horizontal golden DRAGON-shaped streak of light (no face details, just flowing shape)', 'a big golden-red starburst', 'a circle of golden coins and sparkles around an empty center'],
   props=['a big imperial DECREE SCROLL unrolled with a red royal seal stamp and scribbles instead of words', 'a small golden CROWN', 'a red paper LANTERN',
          'a big jade royal SEAL stamp', 'a small bronze CANNON', 'a long strip of red and gold palace roof edge for the foreground'],
   meme='※ 대관식 비용은 연구소 법인카드로', counter=None),
 'pharaoh': dict(
   a='a blazing desert sky with a HUGE glowing golden SUN DISK with long rays, heat haze, a few birds; at the bottom the tips of giant PYRAMIDS rise in',
   b='a golden DESERT with giant pyramids, sphinx statue, palm trees by a river, an ancient city of sandstone temples and obelisks',
   c='the top of a sandstone TEMPLE: a wide flat stone platform with carved pillars (no letters, only simple sun shapes), golden braziers with fire; the lower-middle platform area is empty and clear',
   alt=None,
   intro='raising a golden crook staff toward the sun with both paws, headdress fluttering',
   act='floating in the air cross-legged in front of a glowing sun disk, arms spread, golden light beams shooting out',
   finish='cool pose with arms crossed over the chest (pharaoh pose), glowing golden eyes, smug',
   face='close-up of the face under the striped headdress with the golden cobra, eyes glowing bright gold, intense stare, sun glare',
   fx=['a thick vertical golden SUNBEAM shooting straight DOWN', 'a round golden sunburst impact with sand spray', 'a golden sun-disk ring with rays',
       'a horizontal swirling SAND storm streak', 'a blinding white-gold flash starburst', 'a circle of golden sun rays around an empty center'],
   props=['a big tube of SUNSCREEN lotion squirting (no letters)', 'a golden SCARAB beetle charm', 'a golden ANKH-shaped key',
          'a small golden sarcophagus', 'a flying papyrus scroll with simple drawings (no letters)', 'a long strip of sandstone temple ledge for the foreground'],
   meme='※ 자외선 지수: 매우 위험', counter=None),
 'knight': dict(
   orient='h',
   a='the castle GATE with a lowered drawbridge, stone walls, banners and torches',
   b='a TOURNAMENT FIELD with striped tents, colorful banners and spectator stands, a jousting fence',
   c='a misty hill at the forest edge under a dramatic sky with rays of holy light, faint silhouettes of ghostly knight banners on the hill',
   alt=None,
   intro='raising a shining sword high to the sky with both paws, cape and plume blowing, heroic',
   act='charging forward riding on a glowing blue GHOST HORSE, lance pointed forward, plume streaming',
   finish='the final pose after a slash: sword swung down to the side, kneeling on one knee, visor up, determined smile',
   face='close-up through the open visor: determined eyes, a glint on the helmet, red plume flying',
   fx=['a giant glowing blue GHOST SWORD falling straight DOWN point first (vertical)', 'a blue-white holy impact burst with cracks', 'a round blue holy light ring',
       'a horizontal blue sword SLASH arc', 'a white-gold holy starburst', 'a circle of translucent blue ghost knights silhouettes (no faces) around an empty center'],
   props=['a big ROUND TABLE seating chart paper with only circles and lines (no letters)', 'a round knight SHIELD with a cheese emblem', 'a blue GHOST KNIGHT helmet floating',
          'a broken lance', 'a small blue ghost horse', 'a long strip of castle battlement wall for the foreground'],
   meme='※ 원탁이라 상석 다툼 없음 (아마도)', counter=None),
 'sultan': dict(
   orient='h',
   a='golden DESERT DUNES at night with a camel caravan silhouette and a big crescent moon',
   b='a busy BAZAAR street with colorful awnings, carpets hanging, lanterns, pots and spice piles',
   c='the grand golden PALACE gate with onion domes, minarets, fountains and treasure chests by the entrance',
   alt=None,
   intro='rubbing a golden MAGIC LAMP with both paws, purple smoke starting to swirl out, excited face',
   act='riding on a flying MAGIC CARPET at high speed, holding the lamp up, cape flying, jewels sparkling',
   finish='holding up three fingers proudly with a wink, golden coins raining around, cape spread',
   face='close-up with sparkling greedy eyes reflecting gold coins, big grin, turban jewel glowing',
   fx=['a cascade of GOLD COINS falling straight DOWN (vertical stream)', 'a round purple-gold magic SMOKE burst', 'a ring of purple magic smoke and gems',
       'a horizontal trail of magic sparkles and smoke', 'a big gold-purple starburst', 'a circle of glittering gems and stars around an empty center'],
   props=['a WISH LIST paper scroll with three checkboxes and scribbles (no letters), one big red X stamp', 'a golden MAGIC LAMP', 'a big ruby GEM',
          'a flying magic carpet', 'a treasure chest overflowing with gold', 'a long strip of marble balcony railing for the foreground'],
   meme='※ 소원 추가 요청은 반려되었습니다', counter=None),
 'alien': dict(
   a='OUTER SPACE above the clouds: a gigantic round flying-saucer MOTHERSHIP with glowing green lights covering the sky, stars, a planet; at the bottom the top of thick clouds',
   b='a quiet countryside at night under the clouds: farm fields, a red barn, a windmill, a small town, a green glow from above',
   c='a wheat FIELD with a huge mysterious CROP CIRCLE pattern, a fence, a scarecrow; the lower-middle field area is empty and clear',
   alt=None,
   intro='floating up inside a green TRACTOR BEAM with arms spread, antennae glowing',
   act='riding a tiny flying saucer like a surfboard at high speed, pointing a toy ray gun forward, green trails',
   finish='peace sign with a big alien smile, antennae sparking, standing in the crop circle',
   face='close-up of the big black alien eyes reflecting the mothership lights, antennae glowing green, mysterious smile',
   fx=['a thick green TRACTOR BEAM / laser pillar shooting straight DOWN (vertical)', 'a round green energy impact burst', 'a green glowing saucer-shaped energy ring',
       'a horizontal green laser beam', 'a big green-white flash starburst', 'a circle of small floating green orbs around an empty center'],
   props=['a confused black-and-white COW being lifted, floating', 'a tiny flying saucer', 'a toy RAY GUN',
          'a crop circle pattern disk', 'a farm windmill', 'a long strip of wooden farm fence for the foreground'],
   meme='※ 소는 촬영 후 무사히 돌려보냈습니다', counter=None),
 'streamrat': dict(
   orient='h',
   a='the cozy streamer BEDROOM: a bed with plushies, posters without text, fairy lights',
   b='the STREAMING DESK in the middle: gaming chair, several monitors with blank colorful screens, a ring light, microphone arm, keyboard with rainbow lights',
   c='a big window wall showing a neon city at night, a giant blank screen and floating neon heart icons (no logos)',
   alt=None,
   intro='leaning into the microphone shouting excitedly with both paws up, headset on, chat reflected in glasses',
   act='jumping high with a gaming controller raised, hoodie flying, neon hearts bursting around',
   finish='holding up a big shiny GOLDEN AWARD PLAQUE (a plain gold play-button shape with no logo) with tears of joy and a huge grin',
   face='close-up with the round glasses glinting rainbow monitor light, mouth open in a hype scream, headset mic',
   fx=['a giant falling neon HEART icon with a glowing trail (vertical, going down)', 'a round neon pink-cyan pop burst', 'a ring of neon hearts and stars',
       'a horizontal neon equalizer wave streak', 'a big rainbow neon starburst', 'a circle of little thumbs-up and heart icons around an empty center'],
   props=['a big DONATION money envelope with a heart sticker (no text)', 'a neon heart icon', 'a chat bubble with only colorful dots (no letters)',
          'a gaming headset', 'a little camera on a tripod', 'a long strip of desk edge with keyboard and snacks for the foreground'],
   meme='※ 구독 · 좋아요 · 알림 설정까지!', counter='{n}명'),
 'ramjui': dict(
   a='a furious thunderstorm sky over mountains: dark green-purple clouds, many forked LIGHTNING bolts, swirling leaves in the wind; at the bottom the crown of a GIANT OAK TREE rises in',
   b='a giant ancient OAK TREE in a deep forest, huge branches with little squirrel houses, acorns everywhere, rain',
   c='a forest clearing at the foot of the giant oak: big roots, mossy rocks, mushrooms, a tree stump; the lower-middle clearing is empty and clear',
   alt=None,
   intro='holding a glowing ACORN high above the head with both paws, fluffy tail puffed up with static',
   act='spinning in the air like a drill, huge fluffy tail crackling with yellow-green electricity',
   finish='summoning pose: one paw raised to the sky calling a giant lightning bolt, tail standing up, fierce grin',
   face='close-up with sparkling determined eyes, cheek full of acorns, electricity crackling through the fluffy fur',
   fx=['a giant ACORN-shaped thunderbolt falling straight DOWN with electric trail (vertical)', 'a round green-yellow lightning impact burst with leaves', 'a ring of electricity and flying leaves',
       'a horizontal swirling leaf storm streak', 'a big green-yellow starburst', 'a circle of glowing acorns around an empty center'],
   props=['an old squirrel EXERCISE WHEEL (hamster wheel) made of wood', 'a single glowing ACORN', 'a big OAK LEAF',
          'a pile of acorns', 'a pine cone', 'a long strip of mossy tree roots for the foreground'],
   meme='※ 다람쥐 헌 쳇바퀴에 타고파~', counter=None),
 'jwerry': dict(
   orient='h',
   a='BACKSTAGE: ropes, sandbags, costume racks, prop boxes, a mirror with bulbs',
   b='the main STAGE with red velvet curtains half open, golden frame, spotlight circles on the stage floor',
   c='the AUDIENCE hall: rows of red seats with tiny rat audience silhouettes, golden balconies, a huge crystal chandelier',
   alt=None,
   intro='taking a deep elegant bow with one paw on the chest, the other arm spread wide',
   act='dancing a spinning tap dance in mid-air with a top hat and cane, sparkles',
   finish='curtain call: both arms wide open, eyes closed in bliss, roses flying around',
   face='close-up with a mischievous grin and one eye winking, sweat drop of joy, spotlight glow',
   fx=['a white SPOTLIGHT beam cone shining straight DOWN (vertical)', 'a round burst of red ROSE petals', 'a ring of golden applause sparkles',
       'a horizontal ribbon of musical sparkle trail (no notes letters)', 'a big warm white stage flash', 'a circle of roses around an empty center'],
   props=['a big BOUQUET of red roses with a ribbon', 'a single red ROSE', 'a little top hat',
          'a golden microphone on a stand', 'a row of tiny audience rat silhouettes clapping', 'a long strip of stage footlights for the foreground'],
   meme='※ 앵콜 요청은 마음속으로만', counter=None),
 'starchef': dict(
   orient='h',
   a='the PANTRY: shelves of ingredients, big cheese wheels, hanging herbs and sausages, flour sacks',
   b='the open KITCHEN line: stoves with big flames, copper pans, steam clouds, steel counters with plates lined up',
   c='the fancy DINING HALL: a long table with white cloth, candles, a silver cloche, a crystal chandelier',
   alt=None,
   intro='flipping a frying pan with a big FLAMBE fire burst, tongue out in concentration, chef hat tall',
   act='throwing and juggling many plates in the air while spinning, knife and ladle in paws',
   finish='presenting a silver CLOCHE opened to reveal a sparkling cheese steak, one paw bowing, proud wink',
   face='close-up with a perfectionist stare, one eye squinting, tasting spoon in the mouth, steam swirling',
   fx=['a flaming FRYING PAN falling straight DOWN with fire trail (vertical)', 'a round burst of steam and sauce splash with sparkles', 'a spinning ring of plates and sparkles',
       'a horizontal FIRE flambe streak', 'a big golden three-star starburst', 'a circle of floating spices and herbs around an empty center'],
   props=['a fancy restaurant REVIEW card with three big stars (no letters)', 'a plate with a tiny appetizer', 'a bowl of soup',
          'a silver CLOCHE', 'a big cheese wheel', 'a long strip of kitchen counter edge with knives for the foreground'],
   meme='※ 익힘 정도: 완벽하게 이븐함', counter=None),
 'jjdetective': dict(
   orient='h',
   a='the DETECTIVE OFFICE: a desk with a lamp, a corkboard with photos and red strings (no text), a coat rack, rain on the window',
   b='a rainy noir STREET at night: wet cobblestones, a street lamp, an old brick clock tower, fog',
   c='a dark ALLEY crime scene: footprints, police tape (no text), a manhole, trash cans, a spotlight from above',
   alt=None,
   intro='inspecting with a big MAGNIFYING GLASS, crouching low, glasses shining, serious',
   act='doing a powerful flying KICK on a glowing soccer ball, shoes glowing, motion lines',
   finish='pointing straight at the viewer dramatically "you are the culprit" pose, glasses flashing white',
   face='close-up with the round gold glasses flashing white light, a sharp confident smirk, one finger on the glasses',
   fx=['a blazing SOCCER BALL slamming straight DOWN with fire trail (vertical)', 'a round impact burst with cracks and dust', 'a ring of magnifying-glass light rays',
       'a horizontal speed trail of the ball', 'a big white flash starburst', 'a circle of question-mark-like curls (no real letters) around an empty center'],
   props=['a CASE FILE folder with a big red stamp shape and photos (no letters)', 'a soccer ball', 'a magnifying glass',
          'an evidence bag with cheese crumbs', 'a set of footprints', 'a long strip of wet street curb for the foreground'],
   meme='※ 범인은 이 안에 있다!', counter=None),
 'fondue': dict(
   a='a cozy warm KITCHEN ceiling with hanging cheese wheels and salami, steam clouds and floating bread cubes; at the bottom the rim of a GIANT FONDUE POT rises in',
   b='a GIANT bubbling FONDUE POT full of molten golden cheese, long forks with bread sticking out, steam, the kitchen around it',
   c='a kitchen floor beside the pot: checkered tiles with cheese puddles, a fallen bread basket; the lower-middle floor area is empty and clear',
   alt='the same picture but FLOODED with a huge wave of molten golden CHEESE filling the space, glossy cheese everywhere',
   intro='stirring the fondue with a giant fork, melted cheese dripping from the head, blissful closed eyes',
   act='SURFING on a huge wave of molten cheese with a bread-slice surfboard, shirt flapping',
   finish='holding a giant fork with a super long stretched string of cheese, happy face covered in cheese',
   face='close-up of a blissful face with cheese dripping down, eyes closed, mouth open savoring, steam',
   fx=['a big drop of molten CHEESE falling straight DOWN (vertical, glossy)', 'a round cheese SPLASH burst', 'a ring of cheese drops',
       'a horizontal cheese WAVE streak', 'a big golden glossy starburst', 'a circle of floating bread cubes around an empty center'],
   props=['a long fork with a super STRETCHED string of cheese', 'a bread cube on a fork', 'a cheese wheel',
          'a bubbling small fondue pot', 'a surfboard made of bread', 'a long strip of kitchen table edge for the foreground'],
   meme='※ 쭈욱~ 늘어나는 건 국룰', counter=None),
 'bomber': dict(
   a='a crimson dusk sky with GIANT layered red MAGIC CIRCLES glowing (no letters, only geometric runes-like shapes), embers floating, dark clouds; at the bottom rocky mountain peaks rise in',
   b='a wide rocky VALLEY with an old abandoned fortress, cliffs, dry trees, ominous red light from above',
   c='the top of a rocky CLIFF overlooking the valley: flat stone ground with cracks, scattered pebbles, a dead tree; the lower-middle ground is empty and clear',
   alt=None,
   intro='raising a staff with a bomb on top, chanting with the eye patch glowing, cape-like smoke swirling',
   act='floating in front of many layered red magic circles, both paws forward, bombs orbiting',
   finish='COLLAPSED face-down on the ground after the spell, tiny smoke puffs, exhausted but satisfied smile (comedic)',
   face='close-up with the eye patch, one fierce eye glowing red, sharp fang grin, embers',
   fx=['a cartoon round BOMB with lit fuse falling straight DOWN (vertical, fire trail)', 'a huge round EXPLOSION fireball', 'a red magic circle ring',
       'a horizontal fire streak', 'a blinding orange-white starburst', 'a circle of small floating bombs around an empty center'],
   props=['an MP gauge bar showing completely EMPTY (blue frame, no letters)', 'a round cartoon bomb', 'a magic staff with a bomb top',
          'a red magic circle disk', 'a smoking crater', 'a long strip of rocky cliff edge for the foreground'],
   meme='※ 시전 후 MP 0 · 이동 불가', counter=None),
 'icequeen': dict(
   a='a polar night sky with brilliant green-blue AURORA ribbons, snow falling, stars; at the bottom the spires of a glowing ICE CASTLE rise in',
   b='a grand glowing ICE CASTLE made of pale-blue crystals on a snowy mountain, frozen waterfalls, pine trees with snow',
   c='a frozen LAKE in front of the castle: smooth ice surface with cracks and snow drifts, ice crystals; the lower-middle ice is empty and clear',
   alt='the same picture but COMPLETELY FROZEN SOLID: everything covered in thick ice and frost, giant ice crystals, icicles, a white-blue blizzard',
   intro='spreading both paws as frost blooms out from them, cape flowing, crown sparkling',
   act='sliding gracefully on a rising spiral ICE STAIRCASE, cape streaming, snow swirling',
   finish='elegant twirl with the cape spread like a fan, smug closed-eye smile, snowflakes everywhere',
   face='close-up with icy blue sparkling eyes, frost on the eyelashes, smug smile, crown glinting',
   fx=['a giant sharp ICICLE falling straight DOWN (vertical)', 'a round ice SHATTER burst with shards', 'a frost ring with snowflakes',
       'a horizontal blizzard wind streak', 'a big white-blue frost starburst', 'a circle of snowflakes around an empty center'],
   props=['a big ICED AMERICANO cup full of ice cubes with a straw (no logo)', 'a single snowflake', 'an ice crystal',
          'a snowman', 'a frozen fish in an ice block', 'a long strip of snowy ice ledge for the foreground'],
   meme='※ 얼어 죽어도 아이스 아메리카노', counter=None),
}

HEAD = ("Use your built-in image generation tool to create ONE image and save it as PNG to: {f} (in the current working directory). "
        "Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.\n\nImage spec:\n")
# 그림체 (사용자 2026-10-18: 게임의 플랫한 언타이틀드 구스 게임 그림체를 벗어나지 말 것 — 햄찌 · 뽑기 컷씬 수준. 1차 관광쥐 등은 디즈니풍으로 벗어남 → Sheets/_old_v1)
STYLE = ("- STYLE (MOST IMPORTANT): match EXACTLY the attached ref_zap_style pictures (approved game art) and ref_char: a FLAT cartoon storybook look in the Untitled Goose Game mood — "
         "simple soft flat shapes, flat colors with only gentle soft shading, very little texture, minimal fur detail (NO individual fur strands), "
         "small simple black bead eyes with one tiny white highlight (NOT big sparkly anime eyes), no black outlines, no gloss, no realistic rendering, NOT a Disney/Pixar/3D look. "
         "An energetic dynamic pose is welcome, but keep the drawing as simple as the references.\n- NO text, NO letters, NO numbers, no logos, no speech bubbles anywhere.\n")
MAG = "- Background: ONE solid flat pure magenta #FF00FF everywhere (for chroma keying), no shadow, no gradient. Do not use magenta or purple on the subject. Keep the subject fully inside the frame with margin.\n"


def char(code): return f"- CHARACTER {CHAR[code]} Must look exactly like the attached ref_char / ref_parts (big head, small body, same outfit and colors).\n"


def pano(code, part, desc, cont):
    if S[code].get('orient') == 'h':      # 가로 파노라마 (왼쪽 → 가운데 → 오른쪽, 카메라가 옆으로 훑음)
        s = ("- 1536x1024 LANDSCAPE, one full-frame painting (no transparency, no panels, no borders). NO text, NO letters, NO numbers, no logos, NO characters except tiny crowds if the scene asks.\n"
             "- STYLE (MOST IMPORTANT): exactly the style and the SAME LOW DETAIL LEVEL as the attached ref_pano_style (approved game background, style only — not its content): "
             "soft painted storybook look with big simple readable shapes, flat color areas with gentle shading, NOT hyper-detailed, NOT a realistic matte painting, no tiny clutter.\n"
             "- This picture is ONE SEGMENT of a very WIDE horizontal panorama that a camera pans through (left -> middle -> right). Segments are placed side by side, so the composition must continue at the LEFT and RIGHT edges. "
             "The BOTTOM 25% of the picture is the floor / ground level running continuously from the left edge to the right edge, with a clear empty area where characters can stand.\n")
        if cont:
            s += ("- CONTINUITY IS CRITICAL: the attached ref_prev_right is the RIGHT STRIP of the segment directly to the LEFT of this one. The LEFT 30% of your picture must continue it seamlessly "
                  "(same shapes at the same positions, same floor line, same colors and lighting).\n")
        return s + f"- SEGMENT {part.replace('TOP', 'LEFT').replace('BOTTOM', 'RIGHT')} of 3: {desc}.\n"
    s = ("- 1536x1024 LANDSCAPE, one full-frame painting (no transparency, no panels, no borders). NO text, NO letters, NO numbers, no logos, NO characters except tiny crowds if the scene asks.\n"
         "- STYLE (MOST IMPORTANT): exactly the style and the SAME LOW DETAIL LEVEL as the attached ref_pano_style (approved game background, style only — not its content): "
         "soft painted storybook look with big simple readable shapes, flat color areas with gentle shading, NOT hyper-detailed, NOT a realistic matte painting, no tiny clutter. Dramatic lighting and color are welcome.\n"
         "- This picture is ONE SEGMENT of a very TALL vertical panorama that a camera tilts through (top -> middle -> bottom). Segments are stacked vertically, so the composition must continue at the top and bottom edges.\n")
    if cont:
        s += ("- CONTINUITY IS CRITICAL: the attached ref_prev_bottom is the BOTTOM STRIP of the segment directly ABOVE this one. The TOP 30% of your picture must continue it seamlessly "
              "(same shapes at the same positions, same colors and lighting).\n")
    s += f"- SEGMENT {part} of 3: {desc}.\n"
    return s


def write(code):
    d = S[code]
    out = os.path.join(RATS_DIR, 'SuperUlt', code, 'Sheets'); os.makedirs(out, exist_ok=True)
    g = os.path.join(RATS_DIR, 'GachaIntro', code)
    # 참고 그림: 그림체 · 캐릭터(뽑기 컷 3 = 정면 클로즈업) · 파츠 · 파노라마 그림체(햄찌 도시)
    shutil.copy(os.path.join(RATS_DIR, 'SuperUlt', 'zapham', 'Sheets', 'ref_style.png'), os.path.join(out, 'ref_style.png'))
    for r in ('ref_zap_style.png', 'ref_zap_props.png', 'ref_pano_style.png'):          # 승인된 햄찌 그림 (그림체 기준)
        shutil.copy(os.path.join(RATS_DIR, 'SuperUlt', r), os.path.join(out, r))
    c3 = [f for f in os.listdir(g) if f.endswith('_c3.png')][0]
    Image.open(os.path.join(g, c3)).convert('RGB').resize((768, 512)).save(os.path.join(out, 'ref_char.png'))
    parts = os.path.join(g, 'Sheets', 'ref_parts.png')
    if not os.path.exists(parts): parts = os.path.join(g, 'Sheets', 'ref_icequeen_parts.png')
    shutil.copy(parts, os.path.join(out, 'ref_parts.png'))
    P = {}
    for part, key, cont in (('1 (TOP)', 'a', False), ('2 (MIDDLE)', 'b', True), ('3 (BOTTOM)', 'c', True)):
        P[f'pano_{key}'] = pano(code, part, d[key], cont)
    if d['alt']:
        for key in 'abc':
            P[f'pano_{key}_alt'] = ("EDIT the attached image pano_" + key + ".png. Keep EXACTLY the same composition, camera, size (1536x1024) and painted style. Change: "
                                    + d['alt'] + ". NO text, NO letters, no characters.\n")
    for k in ('intro', 'act', 'finish'):
        P['pose_' + k] = "- 1024x1536 portrait image, ONE character pose, full body.\n" + STYLE + char(code) + MAG + f"- POSE: {d[k]}. Cute and lively, not scary.\n"
    P['face_close'] = ("- 1536x1024 LANDSCAPE CLOSE-UP of the character's face for a skill cut-in. The face fills most of the width. Keep the simple bead eyes of the references.\n"
                       + STYLE + char(code) + f"- {d['face']}. Dramatic colorful background glow.\n")
    P['fx'] = ("- 1536x1024 image on a PURE BLACK #000000 background (for additive blending), 3 columns x 2 rows grid of SEPARATE glowing effects, each well inside its own cell, none touching. Bright cores, soft painted glow, no outlines.\n"
               + "".join(f"  {i + 1}. {e}\n" for i, e in enumerate(d['fx'])) + "- NO text, NO letters, NO numbers.\n")
    P['props'] = ("- 1536x1024 image, 3 columns x 2 rows grid: ONE object per cell, centered in its cell, well inside it, none touching each other or the edges.\n"
                  + STYLE.replace('ref_zap_style pictures (approved game art) and ref_char', 'ref_zap_props picture (approved game props)') + MAG
                  + "".join(f"  {i + 1}. {e}\n" for i, e in enumerate(d['props'])))
    for name, body in P.items():
        f = name + '.png'
        txt = HEAD.format(f=f) + body if not body.startswith('EDIT') else HEAD.replace('create ONE image', 'EDIT the attached image').format(f=f) + body
        open(os.path.join(out, f'prompt_{name}.txt'), 'w', encoding='utf-8').write(txt)
    json.dump({'meme': d['meme'], 'counter': d['counter'], 'alt': bool(d['alt']), 'orient': d.get('orient', 'v')}, open(os.path.join(out, 'story.json'), 'w', encoding='utf-8'), ensure_ascii=False)
    print(code, len(P), 'prompts')


if __name__ == '__main__':
    for c in (sys.argv[1:] or list(S)): write(c)
