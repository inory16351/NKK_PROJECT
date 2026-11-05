"""도토리 · 업적 · 미니게임 (2026-11-03) 글 정리
① --keys : 쥐돌깨기 '턴' → '층' 으로 바꾼 씬 글을 같은 키에서 고침 (다른 언어는 비우고 '번역 필요')
② --en   : 새 글 (테이블 · 씬) 에 영어 채움 (영어 칸이 빈 줄만)
순서: 씬 저장 → --keys → import_scene_strings → gen_string_table → --en → xlsx2json"""
import os, sys
import openpyxl
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
P = os.path.join(ROOT, 'Data_Table', '스트링 테이블.xlsx')
KEYS = {  # 키: (한글, 영어)
    8000572: ('{n}층', 'Floor {n}'),
    8000556: ('최고 {n}층', 'Best {n}F'),
    8000563: ('도달한 층 <b>{turn}</b> (최고 {best}층) · 이번 판 도토리 <b>+{acorn}</b>\n부순 벽돌 {broken} · 모은 치즈 {cheese} · 쥐 {rats}마리 · 가장 높은 등급 {grade} · 증강 {augs}개 · 쥐 누적 {ratsTotal}마리',
              'Reached floor <b>{turn}</b> (best {best}F) · Acorns this run <b>+{acorn}</b>\nBricks broken {broken} · Cheese {cheese} · Rats {rats} · Top grade {grade} · Augments {augs} · Total rats {ratsTotal}'),
    8000587: ('층마다 치즈 + 층 수 × {v}', 'Each floor: cheese + floor × {v}'),
    8000590: ('층 시작마다 무작위 벽돌 {v}개에 번개 (최고 등급 쥐 피해)', 'Each floor: lightning on {v} random bricks (top-grade rat damage)'),
    8000592: ('층마다 가장 아래 벽돌 줄에 기차 (피해 {v})', 'Each floor: a train hits the lowest brick row ({v} dmg)'),
    8000594: ('{v2}층마다 체력이 많은 벽돌 {v}개에 운석 (체력 {v3}%)', 'Every {v2} floors: meteors on the {v} toughest bricks ({v3}% HP)'),
}
EN = {
    '도토리 상점': 'Acorn Shop', '도토리로 아지트 꾸미기': 'Decorate the hideout with acorns',
    '도토리 상점 <size=60%><color=#7a6a5a>업적 · 미니게임으로 모은 도토리로 아지트를 꾸며요 (전투에는 영향 없음)</color></size>':
        'Acorn Shop <size=60%><color=#7a6a5a>Spend acorns from achievements and the minigame on hideout decor (no effect on battles)</color></size>',
    '도토리 {n}': 'Acorns {n}', '아지트': 'Hideout', '뒷골목': 'Back Alley', 'UI · 커서': 'UI · Cursor', '쥐 친구': 'Rat Pals',
    '장식 이름': 'Decor', '보유 중': 'Owned', '사기': 'Buy', '보유': 'Owned', '{n} 모자람': 'Need {n} more', '잠김': 'Locked',
    '※ 장식 그림은 준비 중이에요. 지금은 사서 모아 둘 수만 있어요.': '※ Decor art is coming soon. For now you can only buy and collect it.',
    '{name}\n<size=80%>도토리 {price}개에 살까요?</size>': '{name}\n<size=80%>Buy for {price} acorns?</size>',
    '살래요': 'Buy', '그만둘래요': 'Cancel',
    '전체': 'All', '훈장': 'Badges', '초필살기': 'Supers', '보스': 'Bosses', '미니게임': 'Minigame', '기존': 'Classic',
    '+{n} 도토리': '+{n} acorns', '받음 +{n}': 'Got +{n}', '{date} 달성': 'Done {date}',
    '최고 {best}층 · 쥐 누적 {rats}마리 · 다음 업적: {next} · <color=#c98a3c>10층마다 도토리 +10</color>':
        'Best {best}F · Total rats {rats} · Next: {next} · <color=#c98a3c>+10 acorns every 10 floors</color>',
    '도토리 +{n}': 'Acorns +{n}', '미니게임 업적 전부 달성!': 'All minigame achievements done!',
    '업적 {n}개 달성!': '{n} achievements!', '여러 업적을 한꺼번에 달성했어요': 'You completed several achievements at once',
    '지난 업적 {n}개!': '{n} past achievements!', '안내 전에 달성한 업적 도토리를 한꺼번에 받았어요': 'Acorns for achievements done earlier, all at once',
}

TIER_EN = ['Lab Rat', 'Cheese Thief', 'Sewer Scout', 'Escape Planner', 'Lab Broker', 'Rebel Commander', 'King of Rats', 'Legendary Jailbreaker',
           'Underground Governor', 'Transcendent Subject', 'Lab Conqueror', 'Shadow of the City', 'Chair of the Rat Republic', 'Planet Escaper', 'Galaxy Wanderer', 'Squeak!! Legend']
TIER_ACHV_EN = ['Subject Zero', 'Cheese Thief Debut', 'Flashlight On', 'The Plan Is in My Head', 'Master of Back Deals', 'Flag of Rebellion', 'A Crown of Cheese', 'Bars Are Just Decor',
                'Underground Empire', 'Transcended, Squeak', 'Flag on the Lab', 'The City Moves at Night', 'Unanimous Squeak', 'Earth Was Too Small', 'Milky Way Stroll', 'Squeak!! Legend']
SUP_EN = {31001: ('Citywide Blackout', 'Power Outage (For Real)'), 31002: ('Ratland Fireworks Parade', 'Ratland Is Open'), 31003: ('Grand Coronation', 'I Am the Cheese'),
          31004: ('Judgment of the Sun God', 'Sunscreen Required'), 31005: ('Knights of the Round Table', 'The Round Table Is Cheese-Shaped'), 31006: ('Three Wishes', 'Third Wish Is Cheese Too'),
          31007: ('Mothership Descent', 'Invasion (From the Rat Side)'), 31008: ('1M Subscriber Special', 'Like · Subscribe · Squeak'), 31009: ('Ramjui Thunder Finale', 'Beware of Acorn Lightning'),
          31010: ('Thank You Jwerry (Encore)', 'Encore! Encore!'), 31011: ('3-Star Full Course', 'Michelin Rat'), 31012: ('Case Closed', 'The Culprit Is the Cat, Squeak'),
          31013: ('Cheese Flood', 'Rowing Through Cheese'), 31014: ('Explosion · Ultimate', 'Explosions Are Art, Twice'), 31015: ('Eternal Winter', 'Winter Has Come')}
BOSS_EN = ['Kang Clocks Out', 'Researcher High Kick', 'No Work for the Director', 'Even a Maine Coon Loses to Rats', 'Memes Are Short, Rats Are Long', 'The Experiment Failed',
           'Part-Timer Leaves on Time', 'The Mouse Is on Our Side', 'The Keeper Gets Kept', 'Strike Some Other Day', 'Cheese Cuts Webs', 'Construction Halted', 'Sharks Were Cats',
           'Out of Water Is Dangerous', 'Return the Air Tank', 'Numbers Beat Muscle', 'Captain Goes Down', 'Dictatorship Is Over', 'No Feeding After Midnight', 'You Were the Sus One', "Commander, You're Fired"]
MF_EN = ['10-Floor Arcade', 'Fingers Warmed Up', 'Floor 30 Squeak', 'Brick Breaking Pro', 'Halfway to 100', 'One More Coin', 'Arcade Regular', 'New High Score', 'Last Coin', 'Arcade Conquered at 100']
MR_EN = ['100 Rats Assembled', 'The 300 Squad', '500-Rat Ruckus', 'March of a Thousand', 'The Arcade Is Too Small', '2,000-Rat Riot', 'Army of 3,000']
CAT_EN = {'로비 전체 배경': 'Hideout Background', '낮잠침대': 'Nap Bed', '훈장': 'Badges', '회의 테이블': 'Meeting Table', '친구들 책장': 'Friends Bookshelf', '쳇바퀴': 'Wheel',
          '치즈창고': 'Cheese Storage', '뒷골목 입구 맨홀 뚜껑': 'Alley Manhole Cover', '미니게임기': 'Arcade Machine', '암시장': 'Black Market', '실험실': 'Secret Lab',
          '증강연구소': 'Augment Lab', '뒷골목 전체 배경': 'Back Alley Background', 'UI 버튼': 'UI Buttons', '마우스 커서': 'Mouse Cursor', '쥐 친구': 'Rat Pals'}
MISC_EN = {'박쥐': 'Bat', '앙기뮤띠쥐': 'Yippee Mouse', '마녀 생쥐': 'Witch Mouse', '따봉도치': 'Thumbs-Up Hedgehog', '랫 댄스 쥐': 'Rat Dance Rat', '황금 쥐': 'Golden Rat',
           '찍찍!! 훈장이 열리면 팔아요': 'On sale once Squeak!! Badges opens', '친구들!!이 열리면 팔아요': 'On sale once Friends!! opens',
           '쳇바퀴 훈련이 열리면 팔아요': 'On sale once Wheel Training opens', '치즈 창고가 열리면 팔아요': 'On sale once Cheese Storage opens',
           '16층을 클리어하면 뒷골목과 함께 열려요': 'Unlocks with the Back Alley after clearing floor 16',
           '16층을 클리어하면 비밀 실험실과 함께 열려요': 'Unlocks with the Secret Lab after clearing floor 16',
           '17층을 클리어하면 암시장과 함께 열려요': 'Unlocks with the Black Market after clearing floor 17',
           '31층에 도달하면 증강 연구소와 함께 열려요': 'Unlocks with the Augment Lab at floor 31',
           '10훈장을 달면 미니 게임기와 함께 열려요': 'Unlocks with the arcade machine at badge 10'}
LINES_EN = ["Squeak!! Earning that badge unlocked a new logbook! It's called 'Achievements'!",
            'Earn badges, beat bosses, or use Super Ultimates to complete achievements. Each one gives you acorns!',
            "Some are already done! I'll hand over the acorns for past achievements all at once, squeak!",
            "Huh? There's a hole in the wall! They say that's the Acorn Shop!",
            "You can buy decor for the hideout with acorns. It doesn't affect battles, so pick whatever you like!",
            "The achievement list is here on the right. Browse by category and check the acorns you'll get.",
            "Categories that aren't open yet show '?'. They'll open one by one as you earn more badges!",
            'Welcome! Pick decor from the Hideout, Back Alley, UI and Rat Pals sections.',
            "Decor with a lock means that facility isn't open yet. It says below when it opens!",
            'You can collect acorns from achievements, and later from the minigame.',
            'Rat Breaker gives acorns every time you pass 10 floors! Minigame achievements just opened in the Achievements tab, too.']


def tbl_en():
    import importlib.util
    spec = importlib.util.spec_from_file_location('g', os.path.join(ROOT, 'Tools', 'gen_acorn_data.py'))
    g = importlib.util.module_from_spec(spec); spec.loader.exec_module(g)
    e = {}
    for i, (tn, an) in enumerate(zip(g.TIERS, g.TIER_ACHV), 1):
        e[an] = TIER_ACHV_EN[i - 1]; e[f"{i}훈장 '{tn}' 달기"] = f"Earn badge {i} '{TIER_EN[i - 1]}'"
    for sid, sname, an in g.SUPERS:
        e[an] = SUP_EN[sid][1]; e[f"'{sname}' 초필살기 쓰기"] = f"Use the Super Ultimate '{SUP_EN[sid][0]}'"
    for (f, an), en in zip(g.BOSSES, BOSS_EN):
        e[an] = en; e[f'{f}층 보스 처치'] = f'Defeat the floor {f} boss'
    for i, (an, en) in enumerate(zip(g.MINI_FLOOR, MF_EN), 1):
        e[an] = en; e[f'쥐돌깨기 {i * 10}층 도달'] = f'Reach floor {i * 10} in Rat Breaker'
    for (n, an), en in zip(g.MINI_RAT, MR_EN):
        e[an] = en; e[f'쥐돌깨기에서 쥐 {n:,}마리 모으기'] = f'Collect {n:,} rats in Rat Breaker'
    for k, v in CAT_EN.items():
        e[k] = v; e[f'{k} · 도토리 숲'] = f'{v} · Acorn Forest'; e[f'{k} · 네온 오락실'] = f'{v} · Neon Arcade'
    e.update(MISC_EN)
    for l, en in zip(g.LINES, LINES_EN): e[l[4]] = en
    return e


def main(mode):
    wb = openpyxl.load_workbook(P); ws = wb['String']
    col = {c.value: i + 1 for i, c in enumerate(ws[2]) if c.value}
    langs = ['ja', 'zh', 'pt', 'es', 'ru', 'fr', 'de', 'ar']
    n = 0
    if mode == '--keys':
        for r in range(4, ws.max_row + 1):
            k = ws.cell(r, col['string_key']).value
            if k in KEYS:
                ko, en = KEYS[k]; ws.cell(r, col['ko'], ko); ws.cell(r, col['en'], en)
                for l in langs: ws.cell(r, col[l], None)
                ws.cell(r, col['status'], '번역 필요'); n += 1
    else:
        en = dict(EN); en.update(tbl_en())
        for r in range(4, ws.max_row + 1):
            ko = ws.cell(r, col['ko']).value
            if ko in en and not ws.cell(r, col['en']).value:
                ws.cell(r, col['en'], en[ko]); n += 1
    wb.save(P); print(mode, n)


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else '--en')
