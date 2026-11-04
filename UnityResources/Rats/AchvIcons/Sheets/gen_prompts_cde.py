# 새 업적 69개 (401xx 훈장 · 402xx 초필살기 · 403xx 보스 · 404xx 미니게임) 아이콘 시트 c · d · e 프롬프트 (2026-11-04)
# 메달 틀 · 그림체 = 시트 a · b 와 같음 (참고 그림 achv_icons_a.png)
import os

HERE = os.path.dirname(os.path.abspath(__file__))

EMBLEMS = [
    # 훈장 Tier_Reach (40101~40116)
    (40101, "a small white lab rat head with a round blank ID tag on its ear"),
    (40102, "a cheese wedge peeking out of a small brown thief sack"),
    (40103, "a yellow flashlight with a short pale beam"),
    (40104, "a rolled-up blue blueprint scroll"),
    (40105, "a small brown briefcase with a round gold cheese coin"),
    (40106, "a red rebel flag on a wooden stick"),
    (40107, "a royal crown made of yellow cheese with holes"),
    (40108, "grey prison bars bent wide open"),
    (40109, "a round sewer manhole cover with a tiny gold crown on top"),
    (40110, "a small white rat head with a golden halo ring above it"),
    (40111, "a glass lab flask with a small red flag stuck in it"),
    (40112, "a tiny night city skyline under a crescent moon"),
    (40113, "a wooden judge gavel"),
    (40114, "a small rocket flying away from a blue planet"),
    (40115, "a purple spiral galaxy with a few tiny paw prints"),
    (40116, "a gold trophy cup with two round rat ears"),
    # 초필살기 Super_Use (40201~40215)
    (40201, "a dark light bulb crossed by a yellow lightning bolt"),
    (40202, "a small carousel with a firework burst above it"),
    (40203, "a gold emperor crown on a red cushion"),
    (40204, "a big orange sun next to a small sunscreen bottle"),
    (40205, "a round table shaped like a cheese wheel with a small sword on it"),
    (40206, "a golden magic lamp with three tiny cheese wedges floating above it"),
    (40207, "a big flying saucer mothership above a tiny city"),
    (40208, "a small microphone with a pink heart"),
    (40209, "an acorn struck by a yellow lightning bolt"),
    (40210, "a black top hat with a red rose"),
    (40211, "a silver dish cover (cloche) with three small gold stars above it"),
    (40212, "a magnifying glass over a cat paw print"),
    (40213, "a small wooden rowing boat on a wave of melted yellow cheese"),
    (40214, "a red magic circle under a big orange explosion burst"),
    (40215, "a pale blue snowflake with a small ice crown"),
    # 보스 Boss_Kill (40301~40321)
    (40301, "a navy security guard cap with a black baton"),
    (40302, "a pair of cracked lab goggles"),
    (40303, "a blank ID badge on a lanyard with a red cross mark"),
    (40304, "a giant fluffy orange cat paw"),
    (40305, "a round cat face with a wide open round mouth"),
    (40306, "a test tube with two small cat ears"),
    (40307, "a green dinosaur costume mask head"),
    (40308, "a grey stone computer mouse standing upright"),
    (40309, "a silver whistle next to a curved raptor claw"),
    (40310, "a blank protest picket sign with two cat ears on top"),
    (40311, "a spider web cut by a cheese wedge"),
    (40312, "a yellow excavator bucket"),
    (40313, "a grey shark fin with two small cat ears"),
    (40314, "a fish tail with a small cat head, like a fountain statue"),
    (40315, "a brass deep sea diving helmet"),
    (40316, "a heavy black barbell"),
    (40317, "a grey aircraft carrier ship tilting and sinking into waves"),
    (40318, "a plain grey officer peaked cap knocked over, no insignia"),
    (40319, "an alarm clock with both hands at midnight next to a cat food bowl"),
    (40320, "a red space helmet with a big pale blue visor"),
    (40321, "a white space helmet with cat ears and a small cardboard box"),
    # 미니게임 쥐돌깨기 층 Mini_Floor (40401~40410)
    (40401, "a small stack of colorful bricks with a white ball"),
    (40402, "a flat paddle bar bouncing a white ball"),
    (40403, "colorful bricks breaking apart with a round grey rat-ear ball"),
    (40404, "a broken brick wall with a white ball flying through"),
    (40405, "a brick tower half built"),
    (40406, "one gold arcade coin"),
    (40407, "a small retro arcade cabinet"),
    (40408, "an arcade screen with a big upward arrow"),
    (40409, "a coin slot with one last gold coin going in"),
    (40410, "a red flag on top of a tall brick tower"),
    # 미니게임 쥐 모으기 Mini_Rat (40411~40417)
    (40411, "three small grey rat heads side by side"),
    (40412, "a grey rat head wearing a red-crested Spartan helmet"),
    (40413, "a pile of many small grey rat heads"),
    (40414, "a line of tiny grey rats marching"),
    (40415, "a small arcade cabinet overflowing with grey rats"),
    (40416, "a big crowd of rat heads with a dust cloud"),
    (40417, "a red war banner above a crowd of tiny rats"),
]

SHEETS = [("c", EMBLEMS[0:24]), ("d", EMBLEMS[24:48]), ("e", EMBLEMS[48:69])]

HEAD = """Use your built-in image generation tool to create ONE image and save it as PNG to: achv_icons_{k}.png (in the current working directory). Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.

Image spec:
- 1536x1024, TRANSPARENT background (alpha). No text, no letters, no numbers, no labels, no grid lines, no frames.
- A sheet of {n} game ACHIEVEMENT ICONS in an exact 6 columns x 4 rows grid (each cell 256x256), filled strictly in the listed order left to right, top to bottom{empty}. Each cell has ONE achievement medal centered, filling about 70% of the cell, with an empty margin; medals must NOT touch or cross cell borders.
- EVERY icon uses the SAME medal design as the attached reference sheet: a round gold medal hanging from a short red-and-cream striped ribbon at the top; the medal face is cream with a gold rim. On the medal face draw ONE small emblem (listed below), simple and readable. Only the emblem changes between icons. Same medal size, ribbon and colors as the reference.
- STYLE IS THE MOST IMPORTANT RULE. Match the attached reference image exactly (existing achievement medals in the art style of Untitled Goose Game). Completely FLAT vector shapes, NO outlines or strokes, NO dark contour lines, no glossy highlights, no gradients, no shine, no glow, no motion lines, no drop shadows. A few flat color areas with at most one slightly darker flat tone. Soft muted pastel palette, low saturation, calm picture-book feel. Readable at 64px.

Emblems:
"""

for k, items in SHEETS:
    empty = "" if len(items) == 24 else f" (leave the last {24 - len(items)} cells empty)"
    body = HEAD.format(k=k, n=len(items), empty=empty) + "\n".join(f"{i}) {e}" for i, (_, e) in enumerate(items, 1)) + "\n"
    open(os.path.join(HERE, f"prompt_{k}.txt"), "w", encoding="utf-8", newline="\n").write(body)
    open(os.path.join(HERE, f"names_{k}.txt"), "w", encoding="utf-8", newline="").write(",".join(f"ach_{i}" for i, _ in items))
    print(k, len(items))
