"""스테이지(층) 밸런스 생성기 → Data_Table/스테이지 테이블.xlsx 의 Stage 시트 (Boss 시트들은 그대로). 그다음 Tools/xlsx2json.py
층마다: 방 수 · 적정 전투력(찍찍!!) · 물건 체력 배율 · 치즈 배율 · 벽 체력 배율(계단 방 / 일반) · 추가 제한시간.
적정 전투력 = 무리 공격력 합이 이만큼이면 벽 피해 감산이 없음 (모자라면 (전투력÷적정)^지수 만큼 줄어듦, StageManager.wallGate*).
곡선은 아래 숫자로 조절 → 다시 실행. 표에 없는 층(마지막 층 넘어서)은 마지막 두 층 비율로 이어서 계산.
사용: python Tools/gen_stage_table.py && python Tools/xlsx2json.py"""
import os, copy, math
import openpyxl

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATH = os.path.join(ROOT, 'Data_Table', '스테이지 테이블.xlsx')

FLOORS = 30
POW0, POW_GROW = 400, 1.38          # 적정 전투력: 1층 · 층마다 곱
HP_GROW = 1.38                      # 물건·사람 체력 배율 (1층 1)
CHEESE_GROW = 1.5                   # 치즈 배율 (1층 1)
BOSS_EVERY = 5


def rooms(f):
    return min(9, 3 + (f - 1) // 2) + (1 if f % BOSS_EVERY == 0 else 0)


def wall_stairs(f):                 # 계단 방 벽 = 적정 × 이 값
    return round(min(10, 3 + 0.6 * (f - 1)), 2)


def wall_normal(f):                 # 일반 벽 = 적정 × 이 값 × (1 + 0.25 × 시작 방과의 거리)
    return 1.2


def nice(v):
    if v < 100: return round(v, 2)
    mag = 10 ** (int(math.log10(v)) - 2)
    return round(v / mag) * mag


def main():
    wb = openpyxl.load_workbook(PATH)
    st = [copy.copy(wb['Boss'].cell(r, 1)._style) for r in (1, 2, 3, 4)]
    if 'Stage' in wb.sheetnames: del wb['Stage']
    ws = wb.create_sheet('Stage', 0)
    head = ['층', '방 수', '적정 전투력', '물건 체력 배율', '치즈 배율', '계단 벽 배율', '일반 벽 배율', '추가 제한시간', '메모']
    keys = ['floor', 'rooms', 'pow_need', 'item_hp', 'cheese', 'wall_stairs', 'wall_normal', 'time_add', '-']
    types = ['int', 'int', 'float', 'float', 'float', 'float', 'float', 'float', '-']
    for c, (h, k, t) in enumerate(zip(head, keys, types), 1):
        for r, v in enumerate((h, k, t), 1): ws.cell(r, c, v)._style = copy.copy(st[r - 1])
    for f in range(1, FLOORS + 1):
        row = [f, rooms(f), nice(POW0 * POW_GROW ** (f - 1)), nice(HP_GROW ** (f - 1)), nice(CHEESE_GROW ** (f - 1)),
               wall_stairs(f), wall_normal(f), 0, '보스 층' if f % BOSS_EVERY == 0 else '']
        for c, v in enumerate(row, 1): ws.cell(3 + f, c, v)._style = copy.copy(st[3])
    for c, w in enumerate([6, 8, 14, 14, 12, 12, 12, 14, 12], 1): ws.column_dimensions[openpyxl.utils.get_column_letter(c)].width = w
    cd = wb['Column_Desc']
    have = {cd.cell(r, 1).value for r in range(4, cd.max_row + 1)}
    for k, d in [('pow_need', 'Stage: 적정 전투력(찍찍!!) = 무리 공격력 합. 모자라면 벽 피해 = (전투력÷적정)^지수 (계단 방 1.5 · 일반 0.5)'),
                 ('item_hp', 'Stage: 물건·사람·고양이 체력 = 기본(12 × 물건 체력 배율) × 이 값 × 3.6^(방 거리 × 0.1)'),
                 ('cheese', 'Stage: 물건·사람·고양이·보스 치즈 = 기본(3 × 치즈 배율) × 이 값 × 1.8^(방 거리 × 0.1)'),
                 ('wall_stairs / wall_normal', 'Stage: 벽 체력 = 적정 전투력 × 이 값 (일반 벽은 × (1 + 0.25 × 시작 방 거리))'),
                 ('time_add', 'Stage: 층 제한시간에 더하는 초 (기본 190 + 35 × 방 수 + 보스 층 90)'),
                 ('Stage 생성', 'Tools/gen_stage_table.py 로 생성 (곡선 숫자를 바꾸고 다시 실행)')]:
        if k in have: continue
        r = cd.max_row + 1
        for c, v in enumerate((k, d), 1): cd.cell(r, c, v)._style = copy.copy(cd.cell(r - 1, c)._style)
    wb.save(PATH)
    print('Stage', FLOORS, '층 · 1층', POW0, '· 10층', nice(POW0 * POW_GROW ** 9), '· 20층', nice(POW0 * POW_GROW ** 19))


if __name__ == '__main__':
    main()
