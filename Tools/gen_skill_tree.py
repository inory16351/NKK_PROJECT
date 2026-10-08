"""공용 스킬 트리 생성기 → Data_Table/공용 스킬 테이블.xlsx (그다음 Tools/xlsx2json.py)
찍찍!! 훈장(티어)마다 트리 하나. 노드 하나 = 한 번 활성화 (레벨 없음). 활성화하면 이어진 노드가 열림.
배치: 가운데 시작점(훈장 노드)에서 5갈래 — 전투(왼쪽) · 승급·시간·시작 쥐(위) · 자원(오른쪽) · 묘기(아래 왼쪽) · 해금·특수(아래 오른쪽).
효과 값은 같은 효과끼리 '더함' (공격력 % 는 곱하지 않음 → 과하게 커지지 않게). 식은 Common_Effect_Type 시트.
비용: 치즈 = 훈장 기본 × 1.45^(깊이-1) (핵심 ×1.5) · 연구자료 = 2훈장부터 훈장 기본 × 1.3^(깊이-1) (핵심 ×2)
사용: python Tools/gen_skill_tree.py && python Tools/xlsx2json.py"""
import io, os, copy, math
import openpyxl

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'Data_Table', '공용 스킬 테이블.xlsx')
STYLE_SRC = os.path.join(ROOT, 'Data_Table', '사람 테이블.xlsx')

GRADE = {0: '모든', 1: '일반', 2: '레어', 3: '에픽', 4: '유니크', 5: '전설', 6: '신화'}
TRICK = {0: '모든 묘기', 1: '백덤블링', 2: '윈드밀', 3: '트리플 악셀', 4: '쥐 대포알', 5: '쳇바퀴 돌기'}
TRICK_ICON = {0: 'flip', 1: 'flip', 2: 'windmill', 3: 'axel', 4: 'cannon', 5: 'wheelspin'}
NEW_ITEM = {60011: '표본 병', 60017: '현미경', 60020: '원심분리기', 60022: '가스통', 60023: '약품 드럼', 60024: '서버 랙',
            60025: '치즈 금고', 60026: '황금 치즈 트로피', 60027: '실험용 로봇 팔', 60028: '냉동 수면 캡슐',
            60029: '거대 치즈 바퀴', 60030: '금괴 상자', 60031: '데이터 코어', 60032: '다이아몬드 치즈'}

# 칸 배치 (x 오른쪽, y 아래). 부모 = 이 노드를 여는 노드 (R = 가운데)
SLOTS = {
    'C1': (-1, 0, 'R'), 'C2': (-2, 0, 'C1'), 'C3': (-3, 0, 'C2'), 'C4': (-2, -1, 'C2'), 'C5': (-3, -1, 'C4'), 'C6': (-2, 1, 'C2'), 'C7': (-3, 1, 'C6'), 'C8': (-4, 0, 'C3'),
    'G1': (0, -1, 'R'), 'G2': (0, -2, 'G1'), 'G3': (0, -3, 'G2'), 'G4': (-1, -2, 'G2'), 'G5': (1, -2, 'G2'), 'G6': (1, -3, 'G5'), 'G7': (2, -3, 'G6'), 'G8': (0, -4, 'G3'),
    'L1': (1, 0, 'R'), 'L2': (2, 0, 'L1'), 'L3': (3, 0, 'L2'), 'L4': (2, -1, 'L2'), 'L5': (3, -1, 'L4'), 'L6': (3, 1, 'L3'), 'L7': (4, 0, 'L3'), 'L8': (4, 1, 'L6'),
    'K1': (0, 1, 'R'), 'K2': (-1, 2, 'K1'), 'K3': (-2, 2, 'K2'), 'K4': (-1, 3, 'K2'), 'K5': (-2, 3, 'K3'), 'K6': (-3, 2, 'K3'), 'K7': (-3, 3, 'K5'),
    'S1': (1, 2, 'K1'), 'S2': (2, 2, 'S1'), 'S3': (3, 2, 'S2'), 'S4': (1, 3, 'S1'), 'S5': (3, 3, 'S3'), 'S6': (2, 3, 'S4'),
}
BRANCH = {'C': 'Combat', 'G': 'Growth', 'L': 'Loot', 'K': 'Trick', 'S': 'Special'}

# 훈장별 효과: 칸 → (효과, v1, v2, v3)
T = {}
T[1] = dict(
    C1=('Atk_Flat', 1), C2=('Dmg_Pct', 0.10), C3=('Atk_Flat', 2), C4=('Atk_Flat', 2, 1), C5=('Atk_Flat', 3, 2, 1), C6=('Wall_Dmg_Pct', 0.10), C7=('Wall_Dmg_Pct', 0.15), C8=('Dmg_Pct', 0.15),
    G1=('Start_Rat', 1, 1), G2=('Time_Add', 3), G3=('Breed_Chance', 0.05), G4=('Breed_Chance', 0.10, 10), G5=('Pop_Cap', 10), G6=('Time_Add', 4), G7=('Promote_Double', 0.03, 1), G8=('Breed_Chance', 0.05),
    L1=('Item_Count_Flat', 1), L2=('Cheese_Pct', 0.10), L3=('Item_Count_Flat', 1), L4=('Cheese_Pct', 0.15), L5=('Cheese_Pct', 0.30, 1), L6=('New_Item', 60011), L7=('Cheese_Pct', 0.30, 2), L8=('New_Item', 60025),
    K1=('Trick_Unlock', 1, 0.03), K2=('Trick_Cheese_Pct', 0.20), K3=('Trap_Single_Down', 0.05), K4=('Trick_Chance', 1, 0.02), K5=('Trap_Multi_Down', 0.10), K6=('Trick_Cheese_Pct', 0.40), K7=('Trick_Gauge_Pct', 0.25),
    S1=('Stage_Skip', 2), S2=('Wall_Hp_Down', 0.05), S3=('Rush_Range', 0.15), S4=('Boss_Dmg_Pct', 0.1, 4), S5=('Rocket_CD', 5), S6=('Ult_Gauge_Pct', 0.10),
)
T[2] = dict(
    C1=('Atk_Flat', 2), C2=('Atk_Pct', 0.10), C3=('Crit_Chance', 0.03), C4=('Atk_Flat', 4, 2), C5=('Atk_Pct', 0.15, 2, 1), C6=('Wall_Dmg_Pct', 0.15), C7=('Crit_Dmg', 0.50), C8=('Dmg_Pct', 0.15),
    G1=('Start_Rat', 1, 1), G2=('Time_Add', 5), G3=('Start_Rat', 1, 1), G4=('Breed_Chance', 0.05), G5=('Pop_Cap', 10), G6=('Time_Add', 5), G7=('Promote_Double', 0.03, 1), G8=('Breed_Chance', 0.05),
    L1=('Item_Count_Flat', 1), L2=('Cheese_Pct', 0.15), L3=('Item_Count_Pct', 0.10), L4=('Cheese_Pct', 0.35, 2), L5=('Cheese_Pct', 0.35, 1), L6=('New_Item', 60017), L7=('Item_Count_Pct', 0.15), L8=('New_Item', 60026),
    K1=('Trick_Unlock', 2, 0.02), K2=('Trick_Chance', 2, 0.02), K3=('Trap_Single_Down', 0.05), K4=('Trick_Chance', 1, 0.02), K5=('Trap_Multi_Down', 0.10), K6=('Trick_Cheese_Pct', 0.40), K7=('Trick_Gauge_Pct', 0.25),
    S1=('Stage_Skip', 1), S2=('Wall_Hp_Down', 0.05), S3=('Stage_Skip', 1), S4=('Boss_Dmg_Pct', 0.1, 4), S5=('Ult_Gauge_Pct', 0.15), S6=('Rush_CD', 0.5),
)
T[3] = dict(
    C1=('Atk_Flat', 3), C2=('Atk_Pct', 0.15), C3=('Multi_Hit', 0.05, 1), C4=('Atk_Flat', 6, 3), C5=('Atk_Pct', 0.20, 3, 1), C6=('Wall_Dmg_Pct', 0.20), C7=('Crit_Chance', 0.03), C8=('Dmg_Pct', 0.20),
    G1=('Start_Rat', 1, 1), G2=('Time_Add', 6), G3=('Breed_Chance', 0.05), G4=('Breed_Cool_Pct', 0.15), G5=('Pop_Cap', 15), G6=('Time_Add', 6), G7=('Promote_Double', 0.03, 2), G8=('Promote_Double', 0.02, 1),
    L1=('Item_Count_Flat', 2), L2=('Cheese_Pct', 0.15), L3=('Item_Count_Pct', 0.15), L4=('Cheese_Pct', 0.40, 2), L5=('Cheese_Pct', 0.40, 1), L6=('New_Item', 60020), L7=('Furniture_Pct', 0.30, 0.30), L8=('New_Item', 60027),
    K1=('Trick_Unlock', 3, 0.015), K2=('Trick_Chance', 3, 0.015), K3=('Trap_Single_Down', 0.05), K4=('Trick_Chance', 2, 0.02), K5=('Trap_Multi_Down', 0.05), K6=('Trick_Cheese_Pct', 0.50), K7=('Trick_Gauge_Pct', 0.25),
    S1=('Stage_Skip', 3), S2=('Bite_Zap', 0.05, 2, 0.5), S3=('Rush_Range', 0.15), S4=('Boss_Dmg_Pct', 0.15, 4), S5=('Chain_Blast', 30, 0.20), S6=('Wall_Hp_Down', 0.05),
)
T[4] = dict(
    C1=('Atk_Flat', 5), C2=('Atk_Pct', 0.20), C3=('Crit_Dmg', 0.50), C4=('Atk_Flat', 10, 4), C5=('Atk_Pct', 0.25, 4, 1), C6=('Wall_Dmg_Pct', 0.20), C7=('Boss_Dmg_Pct', 0.25), C8=('Dmg_Pct', 0.20),
    G1=('Start_Rat', 1, 1), G2=('Time_Add', 7), G3=('Start_Rat', 1, 1), G4=('Breed_Chance', 0.05), G5=('Pop_Cap', 15), G6=('Time_Add', 7), G7=('Promote_Double', 0.03, 3), G8=('Breed_Chance', 0.05),
    L1=('Item_Count_Pct', 0.15), L2=('Cheese_Pct', 0.20), L3=('Spawn_Rate_Pct', 0.20), L4=('Cheese_Pct', 0.50, 2), L5=('Cheese_Pct', 0.50, 1), L6=('New_Item', 60022), L7=('Gold_Item', 0.03, 5), L8=('New_Item', 60028),
    K1=('Trick_Unlock', 4, 0.015), K2=('Trick_Chance', 4, 0.015), K3=('Trick_Chance', 0, 0.01), K4=('Trick_Chance', 3, 0.015), K5=('Trap_Single_Down', 0.05), K6=('Trick_Cheese_Pct', 0.50), K7=('Trick_Gauge_Pct', 0.30),
    S1=('Stage_Skip', 2), S2=('Ult_CD', 3), S3=('Stage_Skip', 1), S4=('Boss_Dmg_Pct', 0.15, 4), S5=('Ult_Power_Pct', 0.30), S6=('Rush_Up', 0.5, 0.5),
)
T[5] = dict(
    C1=('Atk_Flat', 8), C2=('Atk_Pct', 0.25), C3=('Multi_Hit', 0.05, 2), C4=('Atk_Flat', 15, 5), C5=('Atk_Pct', 0.30, 5, 1), C6=('Wall_Dmg_Pct', 0.25), C7=('Crit_Chance', 0.04), C8=('Dmg_Pct', 0.25),
    G1=('Start_Rat', 1, 1), G2=('Time_Add', 8), G3=('Breed_Chance', 0.05), G4=('Twin_Chance', 0.05), G5=('Pop_Cap', 20), G6=('Time_Add', 8), G7=('Promote_Double', 0.03, 4), G8=('Mutation_Chance', 0.10),
    L1=('Item_Count_Pct', 0.20), L2=('Cheese_Pct', 0.20), L3=('Spawn_Rate_Pct', 0.20), L4=('Cheese_Pct', 0.60, 2), L5=('Cheese_Pct', 0.60, 1), L6=('New_Item', 60023), L7=('Gold_Item', 0.04), L8=('New_Item', 60029),
    K1=('Trick_Unlock', 5, 0.02), K2=('Trick_Cheese_Pct', 0.50), K3=('Trap_Single_Down', 0.05), K4=('Trick_Chance', 1, 0.02), K5=('Trap_Multi_Down', 0.05), K6=('Trick_Chance', 2, 0.02), K7=('Trick_Gauge_Pct', 0.30),
    S1=('Stage_Skip', 3), S2=('Cheese_Meteor', 12, 0.30), S3=('Rush_Range', 0.20), S4=('Boss_Dmg_Pct', 0.2, 4), S5=('Cheese_Meteor', 0, 0.30), S6=('Rush_CD', 0.5),
)
T[6] = dict(
    C1=('Atk_Flat', 12), C2=('Atk_Pct', 0.30), C3=('Crit_Dmg', 0.75), C4=('Atk_Flat', 22, 6), C5=('Atk_Pct', 0.30, 6, 1), C6=('Wall_Dmg_Pct', 0.25), C7=('Boss_Dmg_Pct', 0.30), C8=('Dmg_Pct', 0.30),
    G1=('Start_Rat', 1, 1), G2=('Time_Add', 9), G3=('Start_Rat', 1, 1), G4=('Breed_Cool_Pct', 0.15), G5=('Pop_Cap', 20), G6=('Time_Add', 9), G7=('Promote_Double', 0.03, 5), G8=('Breed_Chance', 0.05),
    L1=('Item_Count_Pct', 0.20), L2=('Cheese_Pct', 0.25), L3=('Spawn_Rate_Pct', 0.25), L4=('Cheese_Pct', 0.70, 2), L5=('Cheese_Pct', 0.70, 1), L6=('New_Item', 60024), L7=('Gold_Item', 0.04), L8=('New_Item', 60030),
    K1=('Trick_Chance', 0, 0.01), K2=('Trick_Cheese_Pct', 0.60), K3=('Trick_Chance', 3, 0.02), K4=('Trick_Chance', 4, 0.02), K5=('Trap_Multi_Down', 0.05), K6=('Trick_Gauge_Pct', 0.30), K7=('Trick_Chance', 5, 0.02),
    S1=('Stage_Skip', 2), S2=('Ult_Auto', 1), S3=('Stage_Skip', 2), S4=('Boss_Dmg_Pct', 0.2, 4), S5=('Super_Jump_Pct', 0.50), S6=('Ult_CD', 3),
)
T[7] = dict(
    C1=('Atk_Flat', 18), C2=('Atk_Pct', 0.30), C3=('Multi_Hit', 0.05, 2), C4=('Atk_Flat', 30, 5, 1), C5=('Atk_Pct', 0.35, 4, 1), C6=('Wall_Dmg_Pct', 0.30), C7=('Crit_Chance', 0.04), C8=('Dmg_Pct', 0.30),
    G1=('Start_Rat', 1, 1), G2=('Time_Add', 10), G3=('Breed_Chance', 0.05), G4=('Birth_Frenzy', 4, 200, 1.5), G5=('Pop_Cap', 25), G6=('Time_Add', 10), G7=('Promote_Double', 0.03, 0), G8=('Mutation_Chance', 0.15),
    L1=('Item_Count_Pct', 0.25), L2=('Cheese_Pct', 0.25), L3=('Spawn_Rate_Pct', 0.25), L4=('Cheese_Pct', 0.80, 2), L5=('Cheese_Pct', 0.80, 1), L6=('Gold_Item', 0.05), L7=('Air_Cheese_Pct', 0.50), L8=('New_Item', 60031),
    K1=('Trick_Chance', 0, 0.01), K2=('Trick_Cheese_Pct', 0.70), K3=('Trick_Chance', 1, 0.02), K4=('Trick_Chance', 2, 0.02), K5=('Trick_Chance', 3, 0.02), K6=('Trick_Gauge_Pct', 0.40), K7=('Trick_Chance', 4, 0.02),
    S1=('Stage_Skip', 4), S2=('Cat_Hp_Down', 0.30, 0.30), S3=('Rush_Range', 0.20), S4=('Boss_Dmg_Pct', 0.25, 3), S5=('Ult_Power_Pct', 0.50), S6=('Rush_Up', 0.5, 0.5),
)
T[8] = dict(
    C1=('Atk_Flat', 25), C2=('Atk_Pct', 0.40), C3=('Crit_Dmg', 1.00), C4=('Atk_Flat', 40, 6), C5=('Atk_Pct', 0.50, 6), C6=('Wall_Dmg_Pct', 0.30), C7=('Boss_Dmg_Pct', 0.50), C8=('Dmg_Pct', 0.40),
    G1=('Start_Rat', 1, 1), G2=('Time_Add', 11), G3=('Start_Rat', 1, 1), G4=('Twin_Chance', 0.05), G5=('Pop_Cap', 30), G6=('Time_Add', 12), G7=('Promote_Double', 0.04, 0), G8=('Mutation_Chance', 0.15),
    L1=('Item_Count_Pct', 0.30), L2=('Cheese_Pct', 0.30), L3=('Spawn_Rate_Pct', 0.30), L4=('Cheese_Pct', 1.00, 2), L5=('Cheese_Pct', 1.00, 1), L6=('Gold_Item', 0.05), L7=('Combo_Time_Pct', 0.30), L8=('New_Item', 60032),
    K1=('Trick_Chance', 0, 0.02), K2=('Trick_Cheese_Pct', 1.00), K3=('Trick_Chance', 5, 0.02), K4=('Trick_Gauge_Pct', 0.50), K5=('Trap_Single_Down', 0.05), K6=('Trap_Multi_Down', 0.05), K7=('Trick_Chance', 0, 0.01),
    S1=('Stage_Skip', 3), S2=('Ult_CD', 4), S3=('Stage_Skip', 3), S4=('Boss_Dmg_Pct', 0.25, 3), S5=('Rush_CD', 0.5), S6=('Ult_Power_Pct', 0.50),
)

# 훈장별 비용 (2026-10-08 경제 개편, 측정 Tools/probe_results/round8_econ.csv)
# 연구자료: 훈장 하나(트리 + 다음 승급)에 그 층대를 1훈장 약 2판 → 8훈장 약 6판 (사용자 2026-10-08, 성장 곡선은 나중에 다시) (층 연구자료 = Heist 8 × 1.25^(층-1), 보스 층 ×3). 깊이마다 1.3배
# 치즈: 노드 치즈 = 노드 연구자료 × CHEESE_PER_RES[훈장] (= 그 층대 치즈÷연구자료 수입 × (트리+승급 연구자료)÷트리 연구자료)
#   → 연구자료가 모자라 더 못 찍을 즈음 치즈도 바닥. 1훈장(연구자료 없음)은 트리 치즈 합 = T1_CHEESE_TOTAL (깊이마다 1.45배로 나눔)
RESEARCH = {1: 0, 2: 0.72, 3: 3.07, 4: 7.71, 5: 13.21, 6: 32.36, 7: 102.98, 8: 858.71}
CHEESE_PER_RES = {2: 2544, 3: 6354, 4: 20367, 5: 98242, 6: 387375, 7: 1914779, 8: 1594900}
T1_CHEESE_TOTAL = 21300
CHEESE = {1: 40}                     # 1훈장 깊이별 모양 (합을 T1_CHEESE_TOTAL 로 맞춤)


def nice(v):
    mag = 10 ** max(0, int(math.log10(max(v, 1))) - 1)
    return math.ceil(v / mag) * mag
KEYS = {'Trick_Unlock', 'Stage_Skip', 'New_Item', 'Ult_Auto'}
FIRST_KEYS = {'Crit_Chance', 'Multi_Hit', 'Cheese_Meteor', 'Bite_Zap'}       # 그 효과가 처음 나오는 노드만 핵심


def pct(v): return f'{round(v * 100, 1):g}%'


def name(e, v):
    v1, v2, v3 = (list(v) + [0, 0, 0])[:3]
    g = GRADE.get(int(v2), '')
    gw = '모든' if not v2 else (g + (' 이상' if v3 else ''))
    return {
        'Atk_Flat': f'{gw} 쥐 공격력 +{v1:g}', 'Atk_Pct': f'{gw} 쥐 공격력 +{pct(v1)}', 'Dmg_Pct': f'모든 쥐 피해량 +{pct(v1)}',
        'Wall_Dmg_Pct': f'벽에 주는 피해 +{pct(v1)}', 'Crit_Chance': f'치명타 확률 +{pct(v1)}', 'Crit_Dmg': f'치명타 피해 +{pct(v1)}',
        'Multi_Hit': f'{pct(v1)} 확률로 {int(v2) + 1}번 공격', 'Boss_Dmg_Pct': (f'보스에게 주는 피해 +{pct(v1)} · 보스 층 시간 +{v2:g}초' if v2 else f'보스에게 주는 피해 +{pct(v1)}'),
        'Start_Rat': f'시작 쥐 +{int(v1)}마리 (등급은 훈장 확률)', 'Promote_Double': (f'{g}→{GRADE.get(int(v2) + 1, "")} 승급 때 {pct(v1)} 확률로 2마리' if v2 else f'승급 때 {pct(v1)} 확률로 2마리'),
        'Pop_Cap': f'쥐 최대 마리 수 +{int(v1)}', 'Time_Add': f'층 제한시간 +{v1:g}초',
        'Breed_Chance': (f'쥐 {int(v2)}마리 미만일 때 번식 확률 +{pct(v1)}' if v2 else f'번식 확률 +{pct(v1)}'),
        'Move_Speed_Pct': f'쥐 이동 속도 +{pct(v1)}', 'Breed_Cool_Pct': f'번식 쿨타임 -{pct(v1)}',
        'Item_Count_Flat': f'방마다 물건 +{int(v1)}개', 'Item_Count_Pct': f'나오는 물건 +{pct(v1)}',
        'Cheese_Pct': {0: f'치즈 획득량 +{pct(v1)}', 1: f'생명체 처치 치즈 +{pct(v1)}', 2: f'물건 파괴 치즈 +{pct(v1)}'}.get(int(v2), ''),
        'New_Item': f'신규 물건 출현: {NEW_ITEM.get(int(v1), "")}', 'Rocket_CD': f'로켓배송 쿨타임 -{v1:g}초',
        'Gold_Item': f'황금 물건 확률 +{pct(v1)}', 'Spawn_Rate_Pct': f'물건 보충 속도 +{pct(v1)}', 'Combo_Time_Pct': f'콤보 유지 시간 +{pct(v1)}',
        'Trick_Unlock': f'묘기 해금: {TRICK.get(int(v1), "")}', 'Trick_Chance': f'{TRICK.get(int(v1), "")} 확률 +{pct(v2)}',
        'Trick_Cheese_Pct': f'묘기로 얻는 치즈 +{pct(v1)}', 'Trick_Gauge_Pct': f'묘기 성공 시 필살기 게이지 +{pct(v1)}',
        'Trap_Single_Down': f'쥐덫 등장 확률 -{pct(v1)}', 'Trap_Multi_Down': f'쥐덫 2개 등장 확률 -{pct(v1)}',
        'Stage_Skip': f'스테이지 스킵 +{int(v1)}층', 'Wall_Hp_Down': f'벽 체력 -{pct(v1)}',
        'Ult_Gauge_Pct': f'필살기 게이지 충전 +{pct(v1)}', 'Ult_Power_Pct': f'필살기 위력 +{pct(v1)}', 'Ult_Auto': '필살기 자동 사용',
        'Super_Jump_Pct': f'슈퍼 점프 확률 +{pct(v1)}', 'Bite_Zap': f'전기 이빨: {pct(v1)} 확률로 {int(v2)}개 감전',
        'Chain_Blast': f'연쇄 폭발 범위 +{v1:g} · 피해 +{pct(v2)}', 'Cheese_Meteor': (f'치즈 운석 ({v1:g}초마다)' if v1 else f'치즈 운석 위력 +{pct(v2)}'),
        'Twin_Chance': f'쌍둥이 확률 +{pct(v1)}', 'Mutation_Chance': f'높은 등급 탄생 +{pct(v1)}', 'Birth_Frenzy': f'탄생 축제 {v1:g}초 (주변 쥐 광란)',
        'Furniture_Pct': f'가구 피해 +{pct(v1)} · 가구 치즈 +{pct(v2)}', 'Air_Cheese_Pct': f'공중 충돌 치즈 +{pct(v1)}',
        'Rush_Up': f'총공격 시간 +{v1:g}초 · 위력 +{v2:g}', 'Rush_CD': f'총공격 쿨타임 -{v1:g}초', 'Rush_Range': f'총공격 범위 +{pct(v1)}', 'Ult_CD': f'필살기 쿨타임 -{v1:g}초', 'Cat_Hp_Down': f'고양이 체력 -{pct(v1)} · 겁 -{pct(v2)}',
    }[e]


ICON = {'Atk_Flat': 'teeth', 'Atk_Pct': 'gym', 'Dmg_Pct': 'dmg', 'Wall_Dmg_Pct': 'dig', 'Crit_Chance': 'critc', 'Crit_Dmg': 'critdmg', 'Multi_Hit': 'multihit',
        'Boss_Dmg_Pct': 'bossd', 'Start_Rat': 'startrat', 'Promote_Double': 'promote', 'Pop_Cap': 'nest', 'Time_Add': 'clock', 'Breed_Chance': 'breed',
        'Move_Speed_Pct': 'speed', 'Breed_Cool_Pct': 'breed', 'Item_Count_Flat': 'stock', 'Item_Count_Pct': 'stock', 'Cheese_Pct': 'cheese', 'New_Item': 'newitem',
        'Rocket_CD': 'truck', 'Gold_Item': 'goldx', 'Spawn_Rate_Pct': 'spawn', 'Combo_Time_Pct': 'combo', 'Trick_Cheese_Pct': 'trickcheese', 'Trick_Gauge_Pct': 'trickgauge',
        'Trap_Single_Down': 'shield', 'Trap_Multi_Down': 'trapmulti', 'Stage_Skip': 'skip', 'Wall_Hp_Down': 'wallcrack', 'Ult_Gauge_Pct': 'ultcd',
        'Ult_Power_Pct': 'ultcd', 'Ult_Auto': 'autoult', 'Super_Jump_Pct': 'sjump', 'Bite_Zap': 'zap', 'Chain_Blast': 'chainx', 'Cheese_Meteor': 'meteor',
        'Twin_Chance': 'twins', 'Mutation_Chance': 'mutate', 'Birth_Frenzy': 'frenzy', 'Furniture_Pct': 'furnd', 'Air_Cheese_Pct': 'tumble', 'Rush_Up': 'rush', 'Rush_CD': 'rush', 'Rush_Range': 'rush', 'Ult_CD': 'ultcd', 'Cat_Hp_Down': 'catnip'}

EFFECT_DOC = [
    ('None', '효과 없음 (훈장 시작점)', '-', '-', '-'),
    ('Atk_Flat', '쥐 공격력 + 밸류_01 (기본 공격력에 더함)', '더하는 공격력', '등급 (0 = 모든 쥐, 1 일반 ~ 6 신화)', '1 = 그 등급 이상'),
    ('Atk_Pct', '쥐 공격력 ×(1 + 합). 같은 효과끼리 더함', '증가율', '등급 (0 = 모든 쥐)', '1 = 그 등급 이상'),
    ('Dmg_Pct', '모든 쥐 피해량 ×(1 + 합). 공격력 % 와 따로 곱함 (각각은 더함)', '증가율', '-', '-'),
    ('Wall_Dmg_Pct', '벽에 주는 피해 ×(1 + 합)', '증가율', '-', '-'),
    ('Crit_Chance', '치명타 확률 + 합', '확률', '-', '-'),
    ('Crit_Dmg', '치명타 배율 + 합 (기본 RatManager.critMultiplier)', '배율 증가', '-', '-'),
    ('Multi_Hit', '부딪힐 때 확률 합으로 추가 공격', '확률', '추가 공격 수 (가장 큰 값)', '-'),
    ('Boss_Dmg_Pct', '보스에게 주는 피해 ×(1 + 합) · 보스 층 제한시간 + 밸류_02 합 (초)', '증가율', '보스 층 추가 시간 (초)', '-'),
    ('Start_Rat', '판 시작 쥐 + 밸류_01 마리 (등급은 시작 쥐처럼 지금 훈장의 등급 확률로 뽑음)', '마리 수', '안 씀 (예전 등급 칸)', '-'),
    ('Promote_Double', '승급(같은 등급 N마리 → 윗등급 1마리) 때 확률 합으로 윗등급 쥐가 2마리 나옴. N = 올림(등급 테이블 promote_base × promote_grow^이번 판 승급 횟수)', '확률', '승급하는 등급 (0 = 모든 승급, 1 일반 ~ 5 전설)', '-'),
    ('Pop_Cap', '쥐 최대 마리 수 + 합', '마리 수', '-', '-'),
    ('Time_Add', '층 제한시간 + 합 (초). 기본 RunTimer.baseTime 180초 (모든 층 같음)', '초', '-', '-'),
    ('Breed_Chance', '번식 확률 + 밸류_01 (밸류_02 > 0 이면 쥐가 그 수 미만일 때만)', '확률', '마리 수 조건 (0 = 항상)', '-'),
    ('Move_Speed_Pct', '쥐 이동 속도 ×(1 + 합)', '증가율', '-', '-'),
    ('Breed_Cool_Pct', '번식 쿨타임 ÷(1 + 합)', '감소율', '-', '-'),
    ('Item_Count_Flat', '방마다 물건 상한 + 합', '개수', '-', '-'),
    ('Item_Count_Pct', '방마다 물건 상한 ×(1 + 합)', '증가율', '-', '-'),
    ('Cheese_Pct', '치즈 ×(1 + 합). 대상별로 따로 더함: 0 = 전부 · 1 = 생명체(사람·고양이·보스) · 2 = 물건', '증가율', '대상', '-'),
    ('New_Item', '물건 테이블 skill_unlock = 1 인 물건이 나오기 시작 (모든 구간)', '물건 id', '-', '-'),
    ('Rocket_CD', '로켓배송 간격 - 합 (초)', '초', '-', '-'),
    ('Gold_Item', '황금 물건 확률 + 합, 치즈 배율 = 가장 큰 밸류_02 (없으면 5)', '확률', '치즈 배율', '-'),
    ('Spawn_Rate_Pct', '물건 보충 속도 ×(1 + 합)', '증가율', '-', '-'),
    ('Combo_Time_Pct', '콤보 유지 시간 ×(1 + 합)', '증가율', '-', '-'),
    ('Trick_Unlock', '묘기 해금 (해금 전엔 그 묘기가 안 나옴). 물건에 부딪힐 때 기본 확률 밸류_02', '묘기 (1 백덤블링 · 2 윈드밀 · 3 트리플 악셀 · 4 쥐 대포알 · 5 쳇바퀴 돌기)', '기본 확률', '-'),
    ('Trick_Chance', '묘기 확률 + 밸류_02 (해금된 묘기만)', '묘기 (0 = 해금된 묘기 전부)', '확률', '-'),
    ('Trick_Cheese_Pct', '묘기 중인 쥐가 부순 물건 치즈 ×(1 + 합)', '증가율', '-', '-'),
    ('Trick_Gauge_Pct', '묘기 성공 시 필살기 게이지 ×(1 + 합)', '증가율', '-', '-'),
    ('Trap_Single_Down', '방이 열릴 때 쥐덫이 나올 확률 - 합 (기본 100%)', '감소', '-', '-'),
    ('Trap_Multi_Down', '쥐덫이 2개 나올 확률 - 합 (기본 StageManager.trapTwoChance)', '감소', '-', '-'),
    ('Stage_Skip', '로비에서 고를 수 있는 시작 층 + 합 (최고 기록을 넘지 못함)', '층', '-', '-'),
    ('Wall_Hp_Down', '벽 체력 ×(1 - 합) (최소 0.5)', '감소율', '-', '-'),
    ('Ult_Gauge_Pct', '필살기 게이지 충전 ×(1 + 합)', '증가율', '-', '-'),
    ('Ult_Power_Pct', '필살기 피해 ×(1 + 합)', '증가율', '-', '-'),
    ('Ult_Auto', '필살기 자동 사용', '-', '-', '-'),
    ('Super_Jump_Pct', '슈퍼 점프 확률 ×(1 + 합)', '증가율', '-', '-'),
    ('Bite_Zap', '물 때 확률 합으로 주변 물건 감전', '확률', '감전 개수 (가장 큰 값)', '피해 배율 (가장 큰 값)'),
    ('Chain_Blast', '물건이 터질 때 연쇄 폭발 범위 + 합 · 피해 + 합', '범위', '피해 증가', '-'),
    ('Cheese_Meteor', '치즈 운석: 밸류_01 > 0 인 노드가 있으면 켜짐 (간격 = 가장 작은 밸류_01). 위력 = 무리 평균 힘 × (1 + 밸류_02 합)', '간격 (초)', '위력 증가', '-'),
    ('Twin_Chance', '쌍둥이 확률 + 합', '확률', '-', '-'),
    ('Mutation_Chance', '탄생 등급 배율(티어 테이블 birth_grade_k) + 합', '배율 증가', '-', '-'),
    ('Birth_Frenzy', '탄생 축제: 새끼가 태어나면 주변 쥐 광란 (시간 합 · 반경·배율은 가장 큰 값)', '시간 (초)', '반경', '배율'),
    ('Furniture_Pct', '가구 피해 ×(1 + 밸류_01 합) · 가구 치즈 ×(1 + 밸류_02 합)', '피해 증가', '치즈 증가', '-'),
    ('Air_Cheese_Pct', '공중에서 부딪힌 물건 치즈 ×(1 + 합)', '증가율', '-', '-'),
    ('Ult_CD', '필살기 하나가 끝난 뒤 다음 필살기까지 쿨타임 - 합 (초, 기본 UltimateManager.ultCooldown, 최소 ultCooldownMin)', '초', '-', '-'),
    ('Rush_CD', '클릭 총공격 쿨타임 - 합 (초, 최소 RatManager.rushCooldownMin)', '초', '-', '-'),
    ('Rush_Range', '클릭 총공격에 모이는 범위: 화면 밖으로 화면 크기 × 합 만큼 더 (그 안의 쥐도 돌진)', '비율', '-', '-'),
    ('Rush_Up', '총공격 시간 + 밸류_01 합 · 배율 + 밸류_02 합', '초', '배율', '-'),
    ('Cat_Hp_Down', '고양이 체력 ×(1 - 밸류_01 합) · 쥐가 겁먹는 시간 ×(1 - 밸류_02 합)', '감소율', '감소율', '-'),
]


def build():
    rows = []
    for t in range(1, 9):
        base = 92000 + t * 100
        ids = {'R': base}
        for i, k in enumerate(SLOTS): ids[k] = base + 1 + i
        depth = {'R': 0}
        for k, (x, y, p) in SLOTS.items(): depth[k] = depth[p] + 1
        rows.append(dict(skill_id=base, tier=t, skill_name=f'{t}훈장 트리', branch='Core', is_key=1, pos_x=0, pos_y=0, link_1=0, link_2=0,
                         cost_cheese=0, cost_research=0, effect_type='None', v=(0, 0, 0), asset=f'Rogue/rk_{t}', explain='찍찍!! 훈장을 달면 열리는 트리의 시작점'))
        seen = set()
        for k, (x, y, p) in SLOTS.items():
            spec = T[t].get(k)
            if not spec: continue
            e, v = spec[0], tuple(spec[1:]) + (0,) * (4 - len(spec))
            key = e in KEYS or (e in FIRST_KEYS and e not in seen and all(e not in [s[0] for s in T[tt].values()] for tt in range(1, t)))
            seen.add(e)
            d = depth[k]
            res = 0 if RESEARCH[t] == 0 else math.ceil(RESEARCH[t] * 1.3 ** (d - 1) * (2 if key else 1))
            cheese = CHEESE[1] * 1.45 ** (d - 1) * (1.5 if key else 1) if t == 1 else res * CHEESE_PER_RES[t]
            icon = TRICK_ICON[int(v[0])] if e in ('Trick_Unlock', 'Trick_Chance') else {1: 'cheesecreature', 2: 'cheeseitem'}.get(int(v[1]), 'cheese') if e == 'Cheese_Pct' else ICON[e]
            rows.append(dict(skill_id=ids[k], tier=t, skill_name=name(e, v), branch=BRANCH[k[0]], is_key=1 if key else 0, pos_x=x, pos_y=y,
                             link_1=ids[p], link_2=0, cost_cheese=cheese, cost_research=res, effect_type=e, v=v[:3],
                             asset=f'SkillIcons/cs_{icon}', explain=''))
    t1 = [r for r in rows if r['tier'] == 1 and r['cost_cheese'] > 0]
    k1 = T1_CHEESE_TOTAL / max(1, sum(r['cost_cheese'] for r in t1))
    for r in rows:
        if r['cost_cheese'] > 0: r['cost_cheese'] = nice(r['cost_cheese'] * (k1 if r['tier'] == 1 else 1))
    return rows


def main():
    wb = openpyxl.load_workbook(STYLE_SRC)
    st = [copy.copy(wb['Human'].cell(r, 1)._style) for r in (1, 2, 3, 4)]
    old = list(wb.sheetnames)

    def sheet(nm, head, keys, types, data, widths):
        ws = wb.create_sheet(nm + '_new')
        for c, (h, k, ty) in enumerate(zip(head, keys, types), 1):
            for r, v in enumerate((h, k, ty), 1): ws.cell(r, c, v)._style = copy.copy(st[r - 1])
        for i, row in enumerate(data):
            for c, v in enumerate(row, 1): ws.cell(4 + i, c, v)._style = copy.copy(st[3])
        for c in range(1, len(head) + 1): ws.column_dimensions[openpyxl.utils.get_column_letter(c)].width = widths.get(c, 12)
        return ws

    rows = build()
    sheet('Common_Skill',
          ['스킬 id', '훈장', '스킬 이름', '가지', '핵심 노드', '위치 x', '위치 y', '이어진 노드 1', '이어진 노드 2', '치즈 비용', '연구자료 비용', '효과 타입', '밸류_01', '밸류_02', '밸류_03', '스킬 에셋', '스킬 설명'],
          ['skill_id', 'tier', 'skill_name', 'branch', 'is_key', 'pos_x', 'pos_y', 'link_1', 'link_2', 'cost_cheese', 'cost_research', 'effect_type', 'value_01', 'value_02', 'value_03', 'skill_asset', 'skill_explain'],
          ['int', 'int', 'string', 'enum', 'int', 'int', 'int', 'int', 'int', 'float', 'float', 'enum', 'float', 'float', 'float', 'string', 'string'],
          [(r['skill_id'], r['tier'], r['skill_name'], r['branch'], r['is_key'], r['pos_x'], r['pos_y'], r['link_1'], r['link_2'], r['cost_cheese'], r['cost_research'],
            r['effect_type'], r['v'][0], r['v'][1], r['v'][2], r['asset'], r['explain']) for r in rows],
          {3: 34, 12: 18, 16: 24, 17: 30})
    sheet('Branch', ['가지', '가지 이름', '색', '설명'], ['branch', 'branch_name', 'color', 'desc'], ['enum', 'string', 'string', '-'],
          [('Core', '훈장', '#f0c878', '가운데 시작점 (훈장을 달면 자동으로 활성화)'), ('Combat', '전투', '#e8786a', '왼쪽: 공격력·피해·벽·치명타'),
           ('Growth', '승급·시간·시작 쥐', '#6fb3e8', '위쪽: 시작 쥐·승급·최대 마리 수·제한시간·번식'), ('Loot', '자원 파밍', '#7cc88a', '오른쪽: 물건·치즈·신규 물건'),
           ('Trick', '묘기', '#a58bd8', '아래 왼쪽: 묘기 해금·확률·묘기 치즈·쥐덫'), ('Special', '해금·특수', '#f0a05a', '아래 오른쪽: 스테이지 스킵·벽/보스 체력·필살기')],
          {2: 18, 4: 50})
    sheet('Common_Effect_Type', ['효과 타입', '정의', '밸류_01 의미', '밸류_02 의미', '밸류_03 의미'], ['effect_type', 'desc', 'value_01', 'value_02', 'value_03'],
          ['enum', '-', '-', '-', '-'], EFFECT_DOC, {1: 18, 2: 80, 3: 30, 4: 30, 5: 24})
    sheet('Column_Desc', ['칼럼', '설명'], ['column', 'desc'], ['-', '-'],
          [('tier', '이 트리가 속한 찍찍!! 훈장 (티어 테이블). 그 훈장이 되어야 트리가 열림'),
           ('is_key', '1 = 핵심 노드 (마름모). 해금형 노드, 비용 더 큼'),
           ('pos_x / pos_y', '지도 칸 (0,0 = 가운데 훈장 시작점, y 가 작을수록 위)'),
           ('link_1 / link_2', '이 노드를 여는 노드 id (둘 중 하나라도 활성화되면 열림). 시작점은 0'),
           ('cost_cheese / cost_research', '한 번 활성화 비용 (판 밖 로비에서). 연구자료는 2훈장부터'),
           ('효과 합산', '같은 효과 타입은 활성화한 노드 값을 더함 (곱하지 않음). Tools/gen_skill_tree.py 로 생성')],
          {2: 90})
    for n in old: del wb[n]
    for ws in wb: ws.title = ws.title.replace('_new', '')
    wb.save(OUT)
    by = {}
    for r in rows: by[r['tier']] = by.get(r['tier'], 0) + 1
    print('노드', len(rows), by)


if __name__ == '__main__':
    main()
