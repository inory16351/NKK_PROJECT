"""증강 테이블 생성기 (2026-10-19) → Data_Table/증강 테이블.xlsx
사용자 결정: 31층 도달 시 해금 → 그 뒤 층 클리어마다 3개 중 1개 · 등급마다 증강 풀이 아예 다름 (롤체·칼바람 증강처럼) ·
 얻으면 지하 '증강 연구소' 보관함에 쌓임 · 판 안 슬롯 최대 5 (공용 스킬 Aug_Slot 로 +) · 합성(같은 등급 N개 → 상승/유지/하락) ·
 장착한 채 층을 깨면 그 증강 한 개마다 강화 포인트(층수 기준) → 아웃게임에서 포인트 + 치즈로 강화 · 41층 도달 시 1개 들고 가기.
효과 = 공용 스킬 효과 타입 그대로 (CommonSkill 이 공용 스킬 노드와 똑같이 더함) — 공용 스킬 테이블 Common_Effect_Type 시트에 뜻.
설명 글 자리표시: {1} {1b} {1c} = 효과1 밸류_01·02·03, {2} {2b} = 효과2. 뒤에 % 를 붙이면 ×100 ({1%} {1b%}). 강화 레벨만큼 커진 값으로 채움.
고친 뒤: python Tools/gen_augment_table.py && python Tools/gen_string_table.py && python Tools/gen_augment_table.py --en && python Tools/xlsx2json.py"""
import copy, os, sys
import openpyxl

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATH = os.path.join(ROOT, 'Data_Table', '증강 테이블.xlsx')
STYLE_SRC = os.path.join(ROOT, 'Data_Table', '사람 테이블.xlsx')
STR = os.path.join(ROOT, 'Data_Table', '스트링 테이블.xlsx')

# 등급: id · 이름 · 영어 · 색 · 합성 재료 수 · 상승/유지/하락 확률 (사용자: 낮은 등급은 재료 많이·상승 확률 낮게, 높은 등급은 재료 적게·상승 확률 높게, 같은 등급 결과 포함)
GRADES = [
    (1, '브론즈', 'Bronze', '#c98a4b', 4, 0.30, 0.70, 0.00),
    (2, '실버', 'Silver', '#b9c3cc', 3, 0.40, 0.45, 0.15),
    (3, '골드', 'Gold', '#f2c14e', 2, 0.50, 0.35, 0.15),
    (4, '프리즘', 'Prism', '#c58cf0', 2, 0.00, 0.85, 0.15),
]
# 층 클리어 때 나오는 등급 가중치 (그 층 이상부터)
OFFER = [(31, 70, 25, 5, 0), (41, 60, 30, 9, 1), (51, 50, 34, 13, 3), (61, 42, 37, 17, 4), (71, 35, 39, 21, 5), (81, 28, 40, 25, 7), (91, 22, 40, 29, 9)]
# 강화 치즈 (2026-10-19 전체 밸런스): 예전 40 × 치즈 배율 = 층 수입의 0.001% 수준 → 층 치즈 수입 기준으로
UP_CHEESE_FLOORS = 0.15                  # Lv0→1 강화 치즈 = 층 하나 치즈 수입의 이 배 (Lv5 까지 합 ≈ 2층)
try:
    sys_path = __import__('sys').path; sys_path.insert(0, __import__('os').path.dirname(__import__('os').path.abspath(__file__)))
    import gen_economy as _ge
    _KA, _KB = _ge.fit_k(); _KB = round(_KB, 4); _UP_CHEESE_BASE = round(_KA * 9 * UP_CHEESE_FLOORS)
except Exception:
    _KB, _UP_CHEESE_BASE = 1.0, 40
CONFIG = [
    ('unlock_floor', 31, '이 층에 도달하면 증강 해금. 이 층 이상을 클리어할 때마다 증강 고르기'),
    ('carry_floor', 41, '이 층에 도달하면 보관함 증강 1개를 들고 판 시작 (증강 연구소에서 고름)'),
    ('slot_base', 5, '판 안 장착 슬롯 기본 수 (공용 스킬 Aug_Slot 합만큼 +)'),
    ('offer_count', 3, '층 클리어 때 보여 주는 증강 수 (그중 1개)'),
    ('pt_per_floor', 1, '층 클리어 때 장착한 증강 한 개마다 강화 포인트 = 클리어 층 × 이 값'),
    ('up_max', 5, '강화 최대 레벨'),
    ('up_pct', 0.1, '강화 1레벨마다 효과 값 +이 비율 (Lv 5 = ×1.5)'),
    ('up_pt_base', 150, '강화 포인트 필요 = 기본 × 증가^레벨'),
    ('up_pt_grow', 1.6, ''),
    ('up_cheese_base', _UP_CHEESE_BASE, '강화 치즈 = 기본 × 스테이지 테이블 치즈 배율(최고 도달 층) × 층 배율^(min(층, 상한)-1) × 증가^레벨. 기본 = 층 치즈 수입(방 9개)의 UP_CHEESE_FLOORS 배 (Lv0→1)'),
    ('up_cheese_grow', 1.5, ''),
    ('up_cheese_floor_grow', _KB, '층 수입 배율 (Tools/gen_economy.fit_k, 상점 bc_floor_grow 와 같음)'),
    ('up_cheese_floor_cap', 30, '층 수입 배율을 이 층까지만'),
    ('seen_reveal', 1, '1 = 한 번이라도 얻은 증강만 이름·설명 보임 (나머지 ???)'),
]

# (id, 등급, 이름, 영어, 설명, 영어 설명, 효과1 (타입, v1, v2, v3), 효과2 또는 None, 아이콘)
A = [
    # ── 브론즈: 단순 능력치 ──
    (81101, 1, '날카로운 앞니', 'Sharp Incisors', '쥐 공격력 +{1%}%', 'Rat attack +{1%}%', ('Atk_Pct', 0.12, 0, 0), None, 'aug_tooth'),
    (81102, 1, '치즈 킁킁', 'Cheese Sniffer', '치즈 +{1%}%', 'Cheese +{1%}%', ('Cheese_Pct', 0.25, 0, 0), None, 'aug_cheese'),
    (81103, 1, '야근 수당', 'Overtime Pay', '층 제한시간 +{1}초', 'Floor time limit +{1}s', ('Time_Add', 8, 0, 0), None, 'aug_clock'),
    (81104, 1, '사랑의 둥지', 'Love Nest', '번식 확률 +{1%}%', 'Breeding chance +{1%}%', ('Breed_Chance', 0.04, 0, 0), None, 'aug_heart'),
    (81105, 1, '강철 앞니', 'Steel Teeth', '벽 피해 +{1%}%', 'Wall damage +{1%}%', ('Wall_Dmg_Pct', 0.2, 0, 0), None, 'aug_wall'),
    (81106, 1, '급소 찾기', 'Weak Spot', '치명타 확률 +{1%}%', 'Critical chance +{1%}%', ('Crit_Chance', 0.04, 0, 0), None, 'aug_crit'),
    (81107, 1, '보스 사냥 면허', 'Boss Hunting License', '보스 피해 +{1%}%', 'Boss damage +{1%}%', ('Boss_Dmg_Pct', 0.2, 0, 0), None, 'aug_boss'),
    (81108, 1, '에너지 드링크', 'Energy Drink', '필살기 게이지 충전 +{1%}%', 'Ultimate charge +{1%}%', ('Ult_Gauge_Pct', 0.12, 0, 0), None, 'aug_drink'),
    (81109, 1, '단칸방 확장', 'Room Expansion', '최대 마리 수 +{1}', 'Max rats +{1}', ('Pop_Cap', 6, 0, 0), None, 'aug_nest'),
    (81110, 1, '연구 노트', 'Research Notes', '층 클리어 연구자료 +{1%}%', 'Floor clear Research +{1%}%', ('Research_Pct', 0.15, 0, 0), None, 'aug_note'),
    (81111, 1, '개박하 알레르기', 'Catnip Allergy', '고양이 체력 -{1%}% · 겁먹는 시간 -{1b%}%', 'Cat HP -{1%}% · Fear time -{1b%}%', ('Cat_Hp_Down', 0.15, 0.15, 0), None, 'aug_cat'),
    (81112, 1, '날쌘 발', 'Quick Feet', '이동 속도 +{1%}%', 'Move speed +{1%}%', ('Move_Speed_Pct', 0.1, 0, 0), None, 'aug_feet'),
    # ── 실버: 능력치 강화 + 특수 효과 ──
    (81201, 2, '연타의 달인', 'Combo Master', '부딪힐 때 {1%}% 확률로 {1b}번 더 공격', '{1%}% chance to hit {1b} more times', ('Multi_Hit', 0.12, 2, 0), None, 'aug_combo'),
    (81202, 2, '찌릿찌릿 이빨', 'Zappy Teeth', '물 때 {1%}% 확률로 주변 물건 {1b}개 감전', '{1%}% chance on bite to zap {1b} nearby items', ('Bite_Zap', 0.08, 3, 0.6), None, 'aug_zap'),
    (81203, 2, '쌍둥이 유전자', 'Twin Genes', '쌍둥이 확률 +{1%}%', 'Twin chance +{1%}%', ('Twin_Chance', 0.1, 0, 0), None, 'aug_twin'),
    (81204, 2, '필살 수련', 'Ult Training', '필살기 피해 +{1%}%', 'Ultimate damage +{1%}%', ('Ult_Power_Pct', 0.4, 0, 0), None, 'aug_ult'),
    (81205, 2, '돌격 나팔', 'Charge Bugle', '총공격 시간 +{1}초 · 위력 +{1b} · 쿨타임 -{2}초', 'Rush time +{1}s · power +{1b} · cooldown -{2}s', ('Rush_Up', 1, 0.5, 0), ('Rush_CD', 0.5, 0, 0), 'aug_bugle'),
    (81206, 2, '미다스의 앞니', 'Midas Bite', '황금 물건 확률 +{1%}%', 'Golden item chance +{1%}%', ('Gold_Item', 0.05, 5, 0), None, 'aug_gold'),
    (81207, 2, '치명적 앞니', 'Deadly Fang', '치명타 확률 +{1%}% · 치명타 피해 +{2%}%', 'Crit chance +{1%}% · Crit damage +{2%}%', ('Crit_Chance', 0.06, 0, 0), ('Crit_Dmg', 0.5, 0, 0), 'aug_fang'),
    (81208, 2, '서류 도둑', 'Paper Thief', '층 클리어 연구자료 +{1%}%', 'Floor clear Research +{1%}%', ('Research_Pct', 0.35, 0, 0), None, 'aug_papers'),
    (81209, 2, '부실 공사', 'Shoddy Walls', '벽 체력 -{1%}%', 'Wall HP -{1%}%', ('Wall_Hp_Down', 0.08, 0, 0), None, 'aug_crack'),
    (81210, 2, '프로틴 셰이크', 'Protein Shake', '쥐 공격력 +{1%}%', 'Rat attack +{1%}%', ('Atk_Pct', 0.3, 0, 0), None, 'aug_protein'),
    # ── 특수 효과 (2026-10-19, 칼바람 증강처럼 · 높은 등급 중심, 값은 증강 특수 효과 테이블 → {s1}~{s5}) ──
    (81212, 2, '클릭 폭격', 'Click Barrage', '클릭 총공격 때 누른 곳에 작은 폭발 {s1}번 (공격력 ×{s3})', 'Rush clicks set off {s1} small blasts (attack ×{s3})', ('Special', 83005, 0, 0), None, 'aug_click'),
    (81213, 2, '잭팟', 'Jackpot', '층 클리어 때 {s1%}% 확률로 연구자료 ×{s2}', '{s1%}% chance for ×{s2} Research on floor clear', ('Special', 83012, 0, 0), None, 'aug_jackpot'),
    (81211, 2, '연장 근무', 'Extended Shift', '층 제한시간 +{1}초', 'Floor time limit +{1}s', ('Time_Add', 18, 0, 0), None, 'aug_clock2'),
    # ── 골드: 판을 바꾸는 효과 ──
    (81301, 3, '치즈 운석우', 'Cheese Meteor Shower', '{1}초마다 치즈 운석 (위력 +{1b%}%)', 'Cheese meteor every {1}s (power +{1b%}%)', ('Cheese_Meteor', 10, 0.5, 0), None, 'aug_meteor'),
    (81302, 3, '연쇄 폭발', 'Chain Reaction', '물건이 터질 때 연쇄 폭발 범위 +{1} · 피해 +{1b%}%', 'Item chain blast range +{1} · damage +{1b%}%', ('Chain_Blast', 60, 0.5, 0), None, 'aug_blast'),
    (81303, 3, '탄생 축제', 'Birth Party', '새끼가 태어나면 주변 쥐 {1}초 광란', 'Births send nearby rats into a {1}s frenzy', ('Birth_Frenzy', 4, 250, 1.6), None, 'aug_party'),
    (81304, 3, '시간 도둑', 'Time Thief', '층 제한시간 +{1}초 · 쥐 공격력 +{2%}%', 'Time limit +{1}s · Rat attack +{2%}%', ('Time_Add', 25, 0, 0), ('Atk_Pct', 0.15, 0, 0), 'aug_hourglass'),
    (81305, 3, '쥐 아파트', 'Rat Apartment', '최대 마리 수 +{1} · 번식 확률 +{2%}%', 'Max rats +{1} · Breeding chance +{2%}%', ('Pop_Cap', 15, 0, 0), ('Breed_Chance', 0.05, 0, 0), 'aug_apart'),
    (81306, 3, '보스 킬러', 'Boss Killer', '보스 피해 +{1%}% · 필살기 게이지 +{2%}%', 'Boss damage +{1%}% · Ult charge +{2%}%', ('Boss_Dmg_Pct', 0.6, 0, 0), ('Ult_Gauge_Pct', 0.25, 0, 0), 'aug_skull'),
    (81307, 3, '다다다다', 'Rat-a-tat', '부딪힐 때 {1%}% 확률로 {1b}번 더 공격', '{1%}% chance to hit {1b} more times', ('Multi_Hit', 0.25, 3, 0), None, 'aug_rapid'),
    (81308, 3, '두 배 승급', 'Double Promotion', '승급할 때 {1%}% 확률로 2마리', '{1%}% chance to get 2 rats on promotion', ('Promote_Double', 0.2, 0, 0), None, 'aug_double'),
    (81309, 3, '번개 앞니', 'Thunder Fang', '물 때 {1%}% 확률로 주변 물건 {1b}개 감전', '{1%}% chance on bite to zap {1b} nearby items', ('Bite_Zap', 0.15, 5, 1.0), None, 'aug_thunder'),
    (81310, 3, '눈덩이 굴리기', 'Snowball', '이번 층에서 물건을 부술 때마다 공격력 +{s1%}% (최대 {s2%}%)', 'Each item smashed this floor: attack +{s1%}% (max {s2%}%)', ('Special', 83002, 0, 0), None, 'aug_snowball'),
    (81311, 3, '천둥 번개', 'Thunderstorm', '{s1}초마다 대상 {s2}곳에 번개 (공격력 ×{s3})', 'Lightning hits {s2} targets every {s1}s (attack ×{s3})', ('Special', 83004, 0, 0), None, 'aug_storm'),
    (81312, 3, '세포 분열', 'Mitosis', '물건을 부수면 {s1%}% 확률로 임시 쥐 ({s2}초)', '{s1%}% chance to spawn a temp rat ({s2}s) on smash', ('Special', 83006, 0, 0), None, 'aug_mitosis'),
    (81313, 3, '연쇄 붕괴', 'Domino Walls', '벽 하나가 무너지면 남은 벽 체력 -{s1%}%', 'When a wall falls, other walls lose {s1%}% HP', ('Special', 83010, 0, 0), None, 'aug_domino'),
    (81314, 3, '승급 폭죽', 'Promotion Fireworks', '승급할 때 새 쥐 자리에서 폭발 (공격력 ×{s2})', 'Promotions explode around the new rat (attack ×{s2})', ('Special', 83011, 0, 0), None, 'aug_rankboom'),
    # ── 프리즘: 판도를 뒤집는 효과 ──
    (81401, 4, '자동 필살 장치', 'Auto-Ult Device', '필살기 자동 사용 · 필살기 게이지 +{2%}%', 'Ultimates fire automatically · Ult charge +{2%}%', ('Ult_Auto', 1, 0, 0), ('Ult_Gauge_Pct', 0.5, 0, 0), 'aug_robot'),
    (81402, 4, '쥐 떼 군단', 'Rat Legion', '최대 마리 수 +{1} · 쌍둥이 확률 +{2%}%', 'Max rats +{1} · Twin chance +{2%}%', ('Pop_Cap', 30, 0, 0), ('Twin_Chance', 0.25, 0, 0), 'aug_legion'),
    (81403, 4, '전설의 앞니', 'Legendary Incisors', '쥐 공격력 +{1%}% · 모든 피해 +{2%}%', 'Rat attack +{1%}% · All damage +{2%}%', ('Atk_Pct', 1.0, 0, 0), ('Dmg_Pct', 0.3, 0, 0), 'aug_crown'),
    (81404, 4, '연구소 털이', 'Lab Heist', '층 클리어 연구자료 +{1%}% · 치즈 +{2%}%', 'Floor clear Research +{1%}% · Cheese +{2%}%', ('Research_Pct', 1.0, 0, 0), ('Cheese_Pct', 1.0, 0, 0), 'aug_vault'),
    (81405, 4, '시간 정지', 'Time Stop', '층 제한시간 +{1}초 · 벽 체력 -{2%}%', 'Time limit +{1}s · Wall HP -{2%}%', ('Time_Add', 60, 0, 0), ('Wall_Hp_Down', 0.15, 0, 0), 'aug_stop'),
    (81406, 4, '치즈 종말', 'Cheesepocalypse', '{1}초마다 치즈 운석 (위력 +{1b%}%) · 연쇄 폭발 범위 +{2}', 'Cheese meteor every {1}s (power +{1b%}%) · Chain blast +{2}', ('Cheese_Meteor', 5, 2.0, 0), ('Chain_Blast', 100, 1.0, 0), 'aug_doom'),
    (81407, 4, '필살기 폭주', 'Ult Overdrive', '필살기 피해 +{1%}% · 필살기 게이지 +{2%}%', 'Ultimate damage +{1%}% · Ult charge +{2%}%', ('Ult_Power_Pct', 1.5, 0, 0), ('Ult_Gauge_Pct', 0.3, 0, 0), 'aug_overdrive'),
    (81408, 4, '거대 쥐 출현', 'Rise of the Giants', '{s1}초마다 쥐 {s2}마리가 {s5}초 동안 거대화 (공격력 ×{s4})', 'Every {s1}s, {s2} rats turn giant for {s5}s (attack ×{s4})', ('Special', 83001, 0, 0), None, 'aug_giant'),
    (81409, 4, '회전 치즈 칼날', 'Spinning Cheese Blades', '쥐 무리 둘레를 치즈 칼날 {s1}개가 돌며 공격 (공격력 ×{s3})', '{s1} cheese blades orbit your swarm (attack ×{s3})', ('Special', 83003, 0, 0), None, 'aug_blades'),
    (81410, 4, '시간 역행', 'Time Rewind', '제한시간이 0 이 되면 {s1}초를 되돌림 (판당 {s2}번)', 'When time runs out, rewind {s1}s (×{s2} per run)', ('Special', 83007, 0, 0), None, 'aug_rewind'),
    (81411, 4, '필살기 메아리', 'Ultimate Echo', '필살기가 끝나면 {s1%}% 확률로 다른 쥐의 필살기가 한 번 더', '{s1%}% chance another rat fires its ultimate after one ends', ('Special', 83008, 0, 0), None, 'aug_echo'),
    (81412, 4, '쥐 토네이도', 'Rat Tornado', '{s1}초마다 토네이도가 {s2}초 동안 방을 휩씀 (초당 공격력 ×{s4})', 'Every {s1}s a tornado sweeps the room for {s2}s (attack ×{s4}/s)', ('Special', 83009, 0, 0), None, 'aug_tornado'),
]
# 강화로 커지지 않는 칸 (개수·간격·켜기 같은 값): (효과 타입, 밸류 번호)
NO_SCALE = {('Multi_Hit', 2), ('Bite_Zap', 2), ('Gold_Item', 2), ('Cheese_Meteor', 1), ('Birth_Frenzy', 2), ('Birth_Frenzy', 3), ('Ult_Auto', 1)}


def style_of():
    # 스타일은 같은 통합 문서 안에서만 복사됨 → 사람 테이블을 열어 그 안에 새 시트를 만들고 원래 시트는 지움
    wb = openpyxl.load_workbook(STYLE_SRC)
    return wb, [copy.copy(wb['Human'].cell(r, 1)._style) for r in (1, 2, 3, 4)]


def sheet(wb, st, name, head, keys, types, data, widths):
    if name in wb.sheetnames: del wb[name]
    ws = wb.create_sheet(name)
    for c, (h, k, t) in enumerate(zip(head, keys, types), 1):
        for r, v in enumerate((h, k, t), 1): ws.cell(r, c, v)._style = copy.copy(st[r - 1])
    for i, row in enumerate(data):
        for c, v in enumerate(row, 1): ws.cell(4 + i, c, v)._style = copy.copy(st[3])
    for c, w in enumerate(widths, 1): ws.column_dimensions[openpyxl.utils.get_column_letter(c)].width = w
    return ws


def build():
    wb, st = style_of()
    old = list(wb.sheetnames)
    rows = []
    for (i, g, nm, en, d, den, e1, e2, icon) in A:
        e2 = e2 or ('None', 0, 0, 0)
        rows.append([i, g, nm, d, e1[0], e1[1], e1[2], e1[3], e2[0], e2[1], e2[2], e2[3], icon, 1, en])
    sheet(wb, st, 'Augment',
          ['증강 id', '등급', '이름', '설명 (자리표시 {1} {1b} {2} …, % = ×100)', '효과1', '효과1 밸류_01', '효과1 밸류_02', '효과1 밸류_03', '효과2', '효과2 밸류_01', '효과2 밸류_02', '효과2 밸류_03', '아이콘 (Art/Augment)', '나옴 (0 = 안 나옴)', '영어 (번역 참고)'],
          ['augment_id', 'grade', 'aug_name', 'aug_desc', 'effect1', 'e1_v1', 'e1_v2', 'e1_v3', 'effect2', 'e2_v1', 'e2_v2', 'e2_v3', 'icon', 'enabled', '-'],
          ['int', 'int', 'string', 'string', 'string', 'float', 'float', 'float', 'string', 'float', 'float', 'float', 'string', 'int', '-'],
          rows, [10, 6, 18, 40, 16, 10, 10, 10, 16, 10, 10, 10, 16, 8, 24])
    sheet(wb, st, 'Grade',
          ['등급', '이름', '색', '합성 재료 수 (같은 등급)', '상승 확률', '유지 확률', '하락 확률', '영어'],
          ['grade', 'grade_name', 'color', 'synth_need', 'synth_up', 'synth_same', 'synth_down', '-'],
          ['int', 'string', 'string', 'int', 'float', 'float', 'float', '-'],
          [[g, n, c, need, u, s, d, en] for g, n, en, c, need, u, s, d in GRADES], [6, 10, 10, 16, 10, 10, 10, 10])
    sheet(wb, st, 'Offer',
          ['이 층부터 (클리어 층)', '브론즈 가중치', '실버', '골드', '프리즘'],
          ['from_floor', 'w1', 'w2', 'w3', 'w4'], ['int', 'float', 'float', 'float', 'float'],
          [list(o) for o in OFFER], [16, 12, 10, 10, 10])
    sheet(wb, st, 'Aug_Config', ['키', '값', '설명'], ['key', 'value', '-'], ['string', 'float', '-'],
          [[k, v, d] for k, v, d in CONFIG], [18, 10, 70])
    sheet(wb, st, 'No_Scale', ['효과 타입', '밸류 번호', '설명'], ['effect', 'index', '-'], ['string', 'int', '-'],
          [[e, k, '강화해도 안 커지는 칸 (개수·간격·켜기)'] for e, k in sorted(NO_SCALE)], [18, 10, 40])
    ws = sheet(wb, st, 'Column_Desc', ['칸', '설명'], ['-', '-'], ['-', '-'],
               [['생성', 'Tools/gen_augment_table.py 로 생성 (내용은 파이썬 A 목록을 고치고 다시 실행)'],
                ['Special', 'effect1 = Special 이면 e1_v1 = 증강 특수 효과 테이블 special_id (칼바람식 특수 효과, 설명 자리표시 {s1}~{s5})'],
                ['effect1 / effect2', '공용 스킬 효과 타입 (공용 스킬 테이블 Common_Effect_Type 시트). 판 안에서 장착한 증강은 공용 스킬 노드처럼 더해짐. Research_Pct = 층 클리어 연구자료 ×(1 + 합)'],
                ['Grade.synth_*', '같은 등급 synth_need 개 → 무작위 증강 1개, 그 등급에서 상승/유지/하락. 맨 아래 등급의 하락 · 맨 위 등급의 상승은 유지로'],
                ['Offer', '층 클리어 때 3장 등급 가중치 (클리어 층 기준, from_floor 이하 중 가장 높은 줄)']], [20, 100])
    for nm in old: del wb[nm]
    wb.save(PATH)
    print('증강', len(A), '종 →', PATH)


def fill_en():
    # gen_string_table 뒤: 증강 이름·설명·등급 이름의 영어 칸 채우기 (번역 필요 → 비움)
    aw = openpyxl.load_workbook(PATH, data_only=True)
    en = {}
    for ws, kcol, ecol in ((aw['Augment'], 'aug_name', None), (aw['Augment'], 'aug_desc', None), (aw['Grade'], 'grade_name', None)):
        K = [c.value for c in ws[2]]
        for r in ws.iter_rows(min_row=4, values_only=True):
            if r[0] is None: continue
            sk = r[K.index(kcol + '_sk')] if kcol + '_sk' in K else None
            if not sk: continue
            if ws.title == 'Augment':
                row = next(a for a in A if a[0] == r[0])
                en[sk] = row[3] if kcol == 'aug_name' else row[5]
            else:
                en[sk] = next(g[2] for g in GRADES if g[0] == r[0])
    sw = openpyxl.load_workbook(STR); ws = sw['String']; K = [c.value for c in ws[2]]
    n = 0
    for r in ws.iter_rows(min_row=4):
        k = r[0].value
        if k in en and r[K.index('en')].value != en[k]:
            r[K.index('en')].value = en[k]; r[K.index('status')].value = None; n += 1
    sw.save(STR)
    print('영어 채움', n)


if __name__ == '__main__':
    if '--en' in sys.argv: fill_en()
    else: build()
