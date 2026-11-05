"""도토리 · 업적 · 미니게임 보상 · 장식 상점 데이터 (기획서 Ver.05, 2026-11-03)
- 업적 테이블: Achievement 에 분류 · 도토리 · 공개 훈장 · 진행값 방식 칸 추가 + 새 업적 69개 (훈장 16 · 초필살기 15 · 보스 21 · 미니게임 17)
               Achv_Category (분류 기본값) · Achv_Cond_Type 새 조건 5개
- 미니게임 테이블: Minigame_Reward (10층 보상)
- 도토리 테이블 (새 파일): Acorn_Config · Deco_Category (16종) · Deco_Item (36개) · Deco_Lock (잠긴 상품 해금 조건 글)
- 튜토리얼 테이블: 업적 · 상점 안내 단계와 대사 + 미니게임 안내 대사 고침
여러 번 돌려도 같은 결과 (새 줄은 id 로 찾아 덮어씀). 다음 순서: gen_string_table.py → xlsx2json.py
사용: python Tools/gen_acorn_data.py"""
import copy, os
import openpyxl

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'Data_Table')


def P(name): return os.path.join(SRC, name + '.xlsx')


def ensure_cols(ws, cols, before_key='-'):
    """2행 키 칸에 cols [(한글, 키, 자료형)] 가 없으면 메모 칸 앞(없으면 끝)에 끼워 넣음. 키 → 열 번호"""
    keys = [c.value for c in ws[2]]
    for kor, key, typ in cols:
        keys = [c.value for c in ws[2]]
        if key in keys: continue
        at = len(keys) + 1
        for i, k in enumerate(keys):
            if k == before_key: at = i + 1; break
        ws.insert_cols(at)
        for r, v in ((1, kor), (2, key), (3, typ)):
            c = ws.cell(r, at, v)
            src = ws.cell(r, at - 1 if at > 1 else at + 1)
            if src.has_style: c._style = copy.copy(src._style)
    return {c.value: i + 1 for i, c in enumerate(ws[2]) if c.value}


def upsert(ws, col, rows, id_key):
    """rows = [dict] · id_key 값이 같은 줄이 있으면 덮어쓰고 없으면 끝에 추가 (스타일은 4행 복사)"""
    idc = col[id_key]
    where = {ws.cell(r, idc).value: r for r in range(4, ws.max_row + 1) if ws.cell(r, idc).value is not None}
    for d in rows:
        r = where.get(d[id_key])
        if r is None:
            r = ws.max_row + 1
            while r > 4 and ws.cell(r - 1, idc).value is None: r -= 1
            for c in range(1, ws.max_column + 1):
                s = ws.cell(4, c)
                if s.has_style: ws.cell(r, c)._style = copy.copy(s._style)
            where[d[id_key]] = r
        for k, v in d.items():
            if k in col: ws.cell(r, col[k], v)


def sheet(wb, title, head):
    """설정 시트를 통째로 다시 만듦. head = (한글들, 키들, 자료형들)"""
    if title in wb.sheetnames: del wb[title]
    ws = wb.create_sheet(title)
    for r, row in enumerate(head, 1):
        for c, v in enumerate(row, 1): ws.cell(r, c, v)
    return ws


# ─────────────────────────── 업적 ───────────────────────────
TIERS = ['실험체', '치즈 도둑', '하수구 정찰병', '탈출 설계자', '연구소 브로커', '반란 지휘관', '쥐들의 왕', '전설의 탈옥왕',
         '지하 제국 총독', '초월 실험체', '연구소 점령자', '도시의 그림자', '쥐 공화국 의장', '행성 탈출자', '은하 방랑자', '찍찍!! 신화']
TIER_ACHV = ['실험체 0번', '치즈 도둑 데뷔', '손전등 켜고 출발', '설계도는 머릿속에', '뒷거래의 달인', '반란의 깃발', '왕관은 치즈로', '철창은 장식일 뿐',
             '지하 제국 건국', '초월했다, 찍', '연구소에 깃발 꽂기', '도시는 밤에 움직인다', '만장일치 찍찍', '지구는 좁았다', '은하수 산책', '찍찍!! 신화']
SUPERS = [(31001, '도시 대정전', '정전입니다 (진짜로)'), (31002, '쥐랜드 불꽃 퍼레이드', '쥐랜드 개장'), (31003, '천하통일 대관식', '짐이 곧 치즈다'),
          (31004, '태양신의 심판', '선크림 필수'), (31005, '원탁의 기사단', '원탁은 치즈 모양'), (31006, '소원 세 개', '세 번째 소원도 치즈'),
          (31007, '모선 강림', '지구 침공 (쥐 쪽에서)'), (31008, '구독자 100만 기념 방송', '구독 · 좋아요 · 찍'), (31009, '람쥐썬더 최종장', '도토리 낙뢰 주의'),
          (31010, '줴리 감사합니다 (앵콜)', '앵콜! 앵콜!'), (31011, '3스타 풀코스', '미슐랭 쥐'), (31012, '사건 종결', '범인은 고양이다, 찍'),
          (31013, '치즈 대홍수', '치즈로 노 젓기'), (31014, '익스플로전 · 극', '폭발은 두 번 예술이다'), (31015, '영원한 겨울', '겨울이 왔다')]
BOSSES = [(5, '강철수 퇴근'), (10, '연구원 하이킥'), (15, '소장님 출근 금지'), (20, '메인쿤도 쥐는 못 이겨'), (25, '밈은 짧고 쥐는 길다'),
          (30, '실험은 실패했다'), (35, '알바는 칼퇴'), (40, '마우스는 쥐 편'), (45, '사육사도 사육당함'), (50, '파업은 다음에'),
          (55, '거미줄은 치즈로 끊는다'), (60, '공사 중단'), (65, '상어도 고양이였다'), (70, '물 밖은 위험해'), (75, '산소통 반납'),
          (80, '힘보다 쪽수'), (85, '함장님 침몰'), (90, '독재는 끝났다'), (94, '자정 넘어 먹이 금지'), (97, '수상한 건 너였어'), (100, '사령관님 해고')]
MINI_FLOOR = ['10층짜리 오락실', '손가락 풀렸다', '30층 찍찍', '벽돌 깨기 장인', '반백 층 돌파', '동전 하나 더', '오락실 단골', '하이스코어 갱신', '마지막 동전', '100층 오락실 정복']
MINI_RAT = [(100, '쥐 100마리 모였다'), (300, '쥐 300 결사대'), (500, '오백 마리 소동'), (1000, '천 마리 행진'), (1500, '오락실이 좁다'), (2000, '이천 마리 대소동'), (3000, '쥐 3천 대군')]
CAT = {'Tier': (20, 2, '훈장 해금 16'), 'Super': (40, 2, '초필살기 첫 사용 15'), 'Boss': (20, 2, '보스 첫 처치 21'), 'Minigame': (30, 10, '쥐돌깨기 최고 층 10 · 쥐 누적 7'), 'Legacy': (0, 2, '기존 필살기 업적 (도토리 없음)')}


def achv_rows():
    out = []
    def row(i, name, desc, ct, target, need, cat, mode, sort, memo):
        acorn, reveal, _ = CAT[cat]
        out.append(dict(achv_id=i, achv_name=name, achv_desc=desc, cond_type=ct, target_id=target, need=need, achv_icon='', sort_order=sort,
                        achv_category=cat, reward_acorn=acorn, reveal_tier=reveal, value_mode=mode, **{'-': memo}))
    for t, (tn, an) in enumerate(zip(TIERS, TIER_ACHV), 1):
        row(40100 + t, an, f"{t}훈장 '{tn}' 달기", 'Tier_Reach', 0, t, 'Tier', 'Max', 100 + t, '1훈장은 안내 때 소급' if t == 1 else '')
    for i, (sid, sname, an) in enumerate(SUPERS, 1):
        row(40200 + i, an, f"'{sname}' 초필살기 쓰기", 'Super_Use', sid, 1, 'Super', 'Once', 200 + i, sname)
    for i, (f, an) in enumerate(BOSSES, 1):
        row(40300 + i, an, f'{f}층 보스 처치', 'Boss_Kill', f, 1, 'Boss', 'Once', 300 + i, '숨은 층' if f in (94, 97) else '')
    for i, an in enumerate(MINI_FLOOR, 1):
        row(40400 + i, an, f'쥐돌깨기 {i * 10}층 도달', 'Mini_Floor', 0, i * 10, 'Minigame', 'Max', 400 + i, '')
    for i, (n, an) in enumerate(MINI_RAT, 11):
        row(40400 + i, an, f'쥐돌깨기에서 쥐 {n:,}마리 모으기', 'Mini_Rat', 0, n, 'Minigame', 'Sum', 400 + i, '')
    return out


def do_achievement():
    wb = openpyxl.load_workbook(P('업적 테이블'))
    ws = wb['Achievement']
    col = ensure_cols(ws, [('분류', 'achv_category', 'enum'), ('도토리', 'reward_acorn', 'int'), ('공개 훈장', 'reveal_tier', 'int'), ('진행값 방식', 'value_mode', 'enum')])
    # 기존 필살기 업적 = Legacy (도토리 0) · 정렬은 새 분류 뒤로
    for r in range(4, ws.max_row + 1):
        i = ws.cell(r, col['achv_id']).value
        if isinstance(i, int) and i < 40100:
            ws.cell(r, col['achv_category'], 'Legacy'); ws.cell(r, col['reward_acorn'], 0); ws.cell(r, col['reveal_tier'], 2); ws.cell(r, col['value_mode'], 'Sum')
            ws.cell(r, col['sort_order'], 900 + (i - 40000))
    upsert(ws, col, achv_rows(), 'achv_id')
    # 분류 기본값
    cs = sheet(wb, 'Achv_Category', (['분류', '도토리', '공개 훈장', '설명'], ['category', 'acorn', 'reveal', 'desc'], ['enum', 'int', 'int', '-']))
    for k, (a, rv, d) in CAT.items(): cs.append([k, a, rv, d])
    # 조건 타입
    ct = wb['Achv_Cond_Type']
    have = {ct.cell(r, 1).value for r in range(4, ct.max_row + 1)}
    for r in [('Tier_Reach', '도달한 훈장 (Progress.tier). 1훈장은 업적 안내 때 소급', '-', '훈장'),
              ('Super_Use', '초필살기를 처음 씀 (테스트 발동 제외)', '초필살기 id (쥐 캐릭터 테이블 Super_Ult)', '1'),
              ('Boss_Kill', '그 층 보스를 처음 처치 (예전 저장 = 그 층까지 클리어했으면 처치한 것으로)', '보스 층', '1'),
              ('Mini_Floor', '쥐돌깨기 최고 층 (judolBest, 1턴 = 1층)', '-', '층'),
              ('Mini_Rat', '쥐돌깨기에서 얻은 쥐 누적 (judolRats, 시작 쥐 제외)', '-', '마리')]:
        if r[0] not in have: ct.append(list(r))
    wb.save(P('업적 테이블'))
    print('업적 테이블: 새 업적', len(achv_rows()))


# ─────────────────────────── 미니게임 10층 보상 ───────────────────────────
def do_minigame():
    wb = openpyxl.load_workbook(P('미니게임 테이블'))
    ws = sheet(wb, 'Minigame_Reward', (['키', '값', '설명'], ['key', 'value', 'desc'], ['string', 'float', '-']))
    for r in [('floor_step', 10, '몇 층마다 주는지 (1턴 = 1층)'), ('acorn_step', 10, '한 번에 주는 도토리'), ('repeat', 1, '1 = 판마다 다시 받음')]: ws.append(list(r))
    wb.save(P('미니게임 테이블'))
    print('미니게임 테이블: Minigame_Reward')


# ─────────────────────────── 도토리 · 장식 상점 ───────────────────────────
# (번호, 종류 enum, 이름, 구역, 바뀌는 곳, 숲 가격, 네온 가격, 판매 조건 기능 id)
DECO = [(1, 'LobbyBg', '로비 전체 배경', 'Lobby', 'LobbyHome 배경판', 220, 300, ''),
        (2, 'NapBed', '낮잠침대', 'Lobby', '침대 그림', 60, 90, ''),
        (3, 'Badge', '훈장', 'Lobby', '윗쪽 들보 훈장', 60, 90, 'rank'),
        (4, 'Table', '회의 테이블', 'Lobby', '탁자 (치즈 · 찻잔 소품 포함)', 60, 90, ''),
        (5, 'Bookshelf', '친구들 책장', 'Lobby', '책장 (도감 버튼)', 60, 90, 'dex'),
        (6, 'Wheel', '쳇바퀴', 'Lobby', '받침대 + 바퀴', 60, 90, 'rats'),
        (7, 'CheeseStore', '치즈창고', 'Lobby', '치즈 창고 (공용 스킬 버튼)', 60, 90, 'skill'),
        (8, 'Manhole', '뒷골목 입구 맨홀 뚜껑', 'Lobby', '맨홀 뚜껑 (뒷골목 버튼)', 40, 60, 'alley'),
        (9, 'Arcade', '미니게임기', 'Alley', '게임기', 60, 90, 'arcade'),
        (10, 'Market', '암시장', 'Alley', '쥐구멍 암시장 가게', 60, 90, 'market'),
        (11, 'Lab', '실험실', 'Alley', '비밀 실험실 (뽑기)', 60, 90, 'gacha'),
        (12, 'AugLab', '증강연구소', 'Alley', '증강 연구소', 60, 90, 'aug'),
        (13, 'AlleyBg', '뒷골목 전체 배경', 'Alley', '하수구 뒷골목 배경판', 220, 300, 'alley'),
        (14, 'UiButton', 'UI 버튼', 'Common', '모든 화면 공통 버튼 틀 · 탭 (9-slice)', 150, 200, ''),
        (15, 'Cursor', '마우스 커서', 'Common', '포인터 평소 · 누름 (CursorSkin)', 30, 50, ''),
        (16, 'RatFriend', '쥐 친구', 'Lobby', '아지트를 돌아다니는 쥐 (전투 x)', 0, 0, '')]
THEMES = [(1, 'Forest', '도토리 숲'), (2, 'Neon', '네온 오락실')]
FRIENDS = [(1, 'bat', '박쥐', 70, '뒷골목 천장에 매달려 있거나 날아다님'), (2, 'thumbs', '앙기뮤띠쥐', 90, '아지트 특정 위치에서 따봉'),
           (3, 'witch', '마녀 생쥐', 110, '빗자루를 타고 날아다님')]   # 따봉도치 · 랫 댄스 쥐 · 황금 쥐 = 2026-11-05 사용자 지시로 삭제
ZONE_DIR = {'Lobby': 'Lobby', 'Alley': 'Alley', 'Common': 'Common'}
# 판매 조건 기능 → 잠긴 상품에 보일 해금 조건 (기능 id = 튜토리얼 Unlock_Id · Progress.IsUnlocked)
LOCKS = [('rank', '찍찍!! 훈장이 열리면 팔아요'), ('dex', '친구들!!이 열리면 팔아요'), ('rats', '쳇바퀴 훈련이 열리면 팔아요'),
         ('skill', '치즈 창고가 열리면 팔아요'), ('alley', '16층을 클리어하면 뒷골목과 함께 열려요'), ('gacha', '16층을 클리어하면 비밀 실험실과 함께 열려요'),
         ('market', '17층을 클리어하면 암시장과 함께 열려요'), ('aug', '31층에 도달하면 증강 연구소와 함께 열려요'), ('arcade', '10훈장을 달면 미니 게임기와 함께 열려요')]


def do_acorn():
    path = P('도토리 테이블')
    wb = openpyxl.load_workbook(path) if os.path.exists(path) else openpyxl.Workbook()
    if 'Sheet' in wb.sheetnames: del wb['Sheet']
    ws = sheet(wb, 'Acorn_Config', (['키', '값', '설명'], ['key', 'value', 'desc'], ['string', 'float', '-']))
    for r in [('intro_tier', 2, '도토리 · 업적 · 상점 안내 훈장'), ('acorn_max', 99999, '도토리 보유 상한'), ('confirm_price', 100, '이 값 이상이면 구매 확인 창'),
              ('themes', 2, '종류마다 테마 수 (기본 제외)'), ('friend_max', 6, '아지트에 함께 보이는 쥐 친구 최대'),
              ('mini_floor_step', 10, '미니게임 보상 층 간격 (Minigame_Reward 와 같게)'), ('mini_acorn_step', 10, '미니게임 보상 도토리 (Minigame_Reward 와 같게)')]: ws.append(list(r))
    ws = sheet(wb, 'Deco_Category', (['번호', '종류', '이름', '이름 키', '구역', '바뀌는 곳'], ['cat_no', 'category', 'cat_name', 'cat_name_sk', 'zone', 'target'], ['int', 'enum', 'string', 'int', 'enum', '-']))
    for no, cat, name, zone, target, *_ in DECO: ws.append([no, cat, name, None, zone, target])
    ws = sheet(wb, 'Deco_Item', (['상품 id', '이름', '이름 키', '종류', '테마', '가격', '판매 조건 (기능 id, 비우면 처음부터)', '에셋', '정렬', '메모'],
                                 ['deco_id', 'deco_name', 'deco_name_sk', 'category', 'theme', 'price', 'sell_feature', 'asset', 'sort_order', '-'],
                                 ['int', 'string', 'int', 'enum', 'enum', 'int', 'string', 'string', 'int', '-']))
    total = 0
    for no, cat, name, zone, target, pf, pn, feat in DECO:
        if cat == 'RatFriend':
            for k, code, fname, price, memo in FRIENDS:
                ws.append([95160 + k, fname, None, cat, '-', price, feat, f'Deco/Rat/{code}', no * 10 + k, memo]); total += price
            continue
        for k, th, thn in THEMES:
            price = pf if k == 1 else pn
            ws.append([95000 + no * 10 + k, f'{name} · {thn}', None, cat, th, price, feat, f'Deco/{ZONE_DIR[zone]}/{cat.lower()}_{th.lower()}', no * 10 + k, '그림 없음 (구현은 리소스 만들 때)']); total += price
    ws = sheet(wb, 'Deco_Lock', (['기능 id', '해금 조건 글', '글 키'], ['feature', 'hint', 'hint_sk'], ['string', 'string', 'int']))
    for f, h in LOCKS: ws.append([f, h, None])
    ws = sheet(wb, 'Theme_Type', (['테마', '이름', '설명'], ['theme', 'name', 'desc'], ['-', '-', '-']))
    for k, th, thn in THEMES: ws.append([th, thn, '가안'])
    ws.append(['-', '없음', '쥐 친구 (테마 없음)'])
    wb.save(path)
    print('도토리 테이블: 장식', ws.max_row, '· 가격 합', total)


# ─────────────────────────── 튜토리얼 ───────────────────────────
STEPS = [
    # step_id, scene, trigger, value, text, c1, c1v, c2, c2v, unlock, action, action_value, sort, desc
    ('l_achv', 'Lobby', 'TierAtLeast', 2, '', 'Unlocked', 'rank', None, None, 'achv,acorn,shop', None, None, 232,
     '2훈장 승급 뒤 아지트: 업적 · 도토리 안내 (기능 achv · acorn · shop 해금, 2훈장 전 업적 소급 정산) — 업적 기획서 Ver.05'),
    ('l_shop', 'Lobby', 'After', 0, 'l_achv', None, None, None, None, None, None, None, 233, 'l_achv 바로 뒤: 아지트 벽 구멍 = 도토리 상점 강조'),
    ('l_achv_page', 'Lobby', 'PageOpen', 0, 'rank', 'Done', 'l_achv', None, None, None, None, None, 234, '업적 안내 뒤 찍찍!! 훈장 페이지를 열면: 업적 목록 · 분류 · 도토리'),
    ('l_shop_page', 'Lobby', 'PageOpen', 0, 'shop', 'Done', 'l_achv', None, None, None, None, None, 235, '도토리 상점 처음 열기: 구역 · 가격 · 잠긴 상품 해금 조건'),
]
LINES = [
    # line_id, step, speaker, face, text, highlight
    (118, 'l_achv', 'nerd', 'surprised', '찍찍!! 훈장을 달았더니 새 기록장이 생겼어! 이름하여 \'업적\'!', ''),
    (119, 'l_achv', 'nerd', 'normal', '훈장을 달거나, 보스를 처치하거나, 초필살기를 쓰면 업적이 달성돼. 업적마다 도토리를 줘!', 'tab_rank'),
    (120, 'l_achv', 'brownrat', 'happy', '벌써 달성한 것도 있대! 지난 업적 도토리도 한꺼번에 챙겨 줄게 찍찍!', ''),
    (121, 'l_shop', 'brownrat', 'surprised', '어? 벽에 구멍이 뚫렸어! 저기가 도토리 상점이래!', 'wallhole'),
    (122, 'l_shop', 'brownrat', 'happy', '도토리로 아지트를 꾸밀 장식을 살 수 있어. 전투에는 영향 없으니까 마음껏 골라!', 'wallhole'),
    (123, 'l_achv_page', 'nerd', 'normal', '여기 오른쪽이 업적 목록이야. 분류별로 보고, 받을 도토리도 확인할 수 있어.', 'achv_list'),
    (124, 'l_achv_page', 'nerd', 'happy', '아직 열리지 않은 분류는 \'?\'로 보여. 훈장을 더 달면 하나씩 열릴 거야!', 'achv_list'),
    (125, 'l_shop_page', 'brownrat', 'happy', '어서 와! 아지트 · 뒷골목 · UI · 쥐 친구 구역에서 장식을 골라 봐.', 'shop_tabs'),
    (126, 'l_shop_page', 'brownrat', 'normal', '자물쇠가 걸린 장식은 아직 그 시설이 안 열린 거야. 아래에 언제 열리는지 적혀 있어!', 'shop_lock'),
    (127, 'l_shop_page', 'nerd', 'normal', '도토리는 업적을 달성하거나, 나중에 열리는 미니게임에서 모을 수 있어.', 'shop_acorn'),
    # 미니게임 안내 (l_arcade_hot) 에 도토리 · 업적 한 줄 추가
    (128, 'l_arcade_hot', 'nerd', 'normal', '쥐돌깨기는 10층을 넘을 때마다 도토리를 줘! 업적 탭에 미니게임 업적도 새로 열렸어.', 'arcade'),
]


def do_tutorial():
    wb = openpyxl.load_workbook(P('튜토리얼 테이블'))
    ws = wb['Step']
    col = {c.value: i + 1 for i, c in enumerate(ws[2]) if c.value}
    keys = ['step_id', 'scene', 'trigger', 'trigger_value', 'trigger_text', 'cond1_type', 'cond1_value', 'cond2_type', 'cond2_value', 'unlock', 'action', 'action_value', 'sort', '-']
    upsert(ws, col, [dict(zip(keys, s)) for s in STEPS], 'step_id')
    ws = wb['Line']
    col = {c.value: i + 1 for i, c in enumerate(ws[2]) if c.value}
    keys = ['line_id', 'step_id', 'speaker_id', 'face', 'text', 'highlight']
    upsert(ws, col, [dict(zip(keys, l)) for l in LINES], 'line_id')
    # 116 줄 (게임기 누르면 미니게임) 뒤에 128 이 오게 → 브로커 117 은 마지막 그대로. 대사 순서는 line_id 순
    ws = wb['Unlock_Id']
    have = {ws.cell(r, 1).value for r in range(4, ws.max_row + 1)}
    for r in [('achv', '로비: 업적 (2훈장 안내 l_achv, 업적 달성 · 도토리 지급 시작)'), ('acorn', '로비: 도토리 표시줄'), ('shop', '로비: 아지트 벽 구멍 도토리 상점')]:
        if r[0] not in have: ws.append(list(r))
    wb.save(P('튜토리얼 테이블'))
    print('튜토리얼 테이블: 단계', len(STEPS), '· 대사', len(LINES))


if __name__ == '__main__':
    do_achievement(); do_minigame(); do_acorn(); do_tutorial()
