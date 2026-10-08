"""스테이지(층) 밸런스 생성기 → Data_Table/스테이지 테이블.xlsx 의 Stage 시트 (Boss 시트들은 그대로). 그다음 Tools/xlsx2json.py
층마다: 방 수 · 적정 전투력(찍찍!!) · 물건 체력 배율 · 치즈 배율 · 벽 체력 배율(계단 방 / 일반) · 추가 제한시간.
적정 전투력 = 무리 공격력 합이 이만큼이면 벽 피해 감산이 없음 (모자라면 (전투력÷적정)^지수 만큼 줄어듦, StageManager.wallGate*).
곡선은 아래 숫자로 조절 → 다시 실행. 표에 없는 층(마지막 층 넘어서)은 마지막 두 층 비율로 이어서 계산.
사용: python Tools/gen_stage_table.py && python Tools/xlsx2json.py"""
import os, copy, math
from collections import deque
import numpy as np
import openpyxl

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATH = os.path.join(ROOT, 'Data_Table', '스테이지 테이블.xlsx')

FLOORS = 50
POW0, POW_GROW = 400, 1.38          # 적정 전투력: 1층 · 층마다 곱
HP_GROW = 1.38                      # 물건·사람 체력 배율 (1층 1)
CHEESE_GROW = 1.5                   # 치즈 배율 (1층 1)
BOSS_EVERY = 5


def rooms(f):
    return min(9, 3 + (f - 1) // 2) + (1 if f % BOSS_EVERY == 0 else 0)


# ── 벽 체력: 층마다 실제 지형(계단까지 최단 경로)을 계산해서 "경로 벽 배율 합"이 목표 S(f) 가 되게 ──
# 목표: 전투력 = 적정일 때 제한시간(180초, 보스 층 210초)의 70~80% (TARGET_SEC 135초, 보스 층은 보스전 BOSS_FIGHT_SEC 빼고)
# 측정(BalanceProbe 적정 고정): 걸린 시간 ≈ 경로 벽 배율 합(공용 스킬 벽 체력 감소 적용 후) × SPU[f] (배율 1당 초).
#   SPU 는 지형마다 크게 다름 (복도처럼 바깥벽이 많은 지형은 쥐가 엉뚱한 벽에 부딪혀 느림) → 층별 측정값. 없으면 SPU_DEFAULT
#   층을 다시 재면 SPU 를 고치고 다시 실행.  S = 목표 초 ÷ (SPU × SKILL_WALL_MUL)
TARGET_SEC = 135
BOSS_TOTAL_SEC = 189                  # 보스 층 = 허들: 210초의 90% 로 아슬아슬하게 (사용자 2026-10-08). 길 뚫기는 일반 층과 비슷하게 117초
BOSS_FIGHT_SEC = 72                   # 보스전 목표 (스테이지 테이블 Boss 시트 hp_pow_sec 로 맞춤, Tools/update_spu.py)
SPU = {                               # 측정 BalanceProbe (Tools/update_spu.py 로 갱신)
    1: 1.29, 2: 1.75, 3: 1.58, 4: 2.85, 5: 21.2, 6: 8.61, 7: 7.12, 8: 10.0, 9: 16.37, 10: 8.67,
    11: 40.96, 12: 13.0, 13: 4.76, 14: 8.32, 15: 32.44, 16: 1.04, 17: 16.35, 18: 6.71, 19: 5.63, 20: 4.24,
    21: 4.27, 22: 1.82, 23: 4.3, 24: 2.56, 25: 3.08,
}
SPU_DEFAULT = 10.0
SKILL_WALL_MUL = {1: 1.0, 2: 0.97, 3: 0.95, 4: 0.92, 5: 0.9, 6: 0.9, 7: 0.87}   # 그 층을 깰 즈음의 공용 스킬 벽 체력 배율 (8층부터 0.85)
STAIRS_SHARE = 0.5                    # 경로 벽 합 중 계단 방 벽 몫 (나머지는 경로의 일반 벽에 똑같이)
MIN_NORMAL = 0.8                      # 일반 벽 배율 최소


def path_target(f):
    sec = BOSS_TOTAL_SEC - BOSS_FIGHT_SEC if f % BOSS_EVERY == 0 else TARGET_SEC
    return sec / (SPU.get(f, SPU_DEFAULT) * SKILL_WALL_MUL.get(f, 0.85))


# 판마다 지형이 랜덤 (StageManager.randomLayout, 2026-10-08 사용자) → 층별 SPU 는 그 층 지형 하나에 맞춘 값이라 그대로 못 씀.
# 앞뒤 SMOOTH 층 측정값의 중앙값으로 매끄럽게 → wall_path = 경로 벽 배율 합 목표. 게임이 생성된 지형의 계단 거리로 계단/일반 벽에 나눔
SMOOTH = 2
def spu_smooth(f):
    v = sorted(SPU[g] for g in range(f - SMOOTH, f + SMOOTH + 1) if g in SPU)
    return v[len(v) // 2] if v else SPU_DEFAULT


def wall_path(f):
    sec = BOSS_TOTAL_SEC - BOSS_FIGHT_SEC if f % BOSS_EVERY == 0 else TARGET_SEC
    return round(sec / (spu_smooth(f) * SKILL_WALL_MUL.get(f, 0.85)), 2)


# StageManager.GenLayout / SeededRandom 과 똑같은 지형 (층 번호가 시드). maxRow = StageManager.maxRow
M32 = 0xFFFFFFFF
class Seeded:
    def __init__(self, seed): self.a = seed & M32
    def next(self):
        self.a = (self.a + 0x6D2B79F5) & M32; t = self.a
        t = ((t ^ (t >> 15)) * (t | 1)) & M32
        t ^= (t + (((t ^ (t >> 7)) * (t | 61)) & M32)) & M32
        return float(np.float32(np.float32((t ^ (t >> 14)) & M32) / np.float32(4294967296.0)))

DIRS = [(1, 0), (-1, 0), (0, 1), (0, -1)]
STAIRS_MAX_DIST = 4                   # 계단 방 = 시작 방에서 이 거리 이내 중 가장 먼 방 (StageManager.stairsMaxDist 와 같게)


def stairs_dist(f, n, max_row=3):
    rnd = Seeded(f * 7919 + 17); lst = [(0, 0)]; lay = {(0, 0)}; g = 0
    while len(lst) < n and g < 500:
        g += 1
        b = lst[-1] if rnd.next() < 0.6 else lst[int(math.floor(np.float32(rnd.next()) * np.float32(len(lst))))]
        d = DIRS[int(math.floor(np.float32(rnd.next()) * np.float32(4)))]
        k = (b[0] + d[0], b[1] + d[1])
        if k in lay or abs(k[1]) > max_row: continue
        lay.add(k); lst.append(k)
    dist = {(0, 0): 0}; q = deque([(0, 0)])
    while q:
        c = q.popleft()
        for d in DIRS:
            k = (c[0] + d[0], c[1] + d[1])
            if k in lay and k not in dist: dist[k] = dist[c] + 1; q.append(k)
    best = (0, 0)
    for k, v in dist.items():
        if v > dist[best] and v <= STAIRS_MAX_DIST: best = k
    return dist[best]


def walls(f):                       # (계단 방 벽, 일반 벽) 배율 — 일반 벽은 거리 가중 없음 (StageManager.wallDistK 0)
    S, d = path_target(f), stairs_dist(f, rooms(f))
    if d <= 1: return round(S, 2), 1.2
    normal = max(MIN_NORMAL, S * (1 - STAIRS_SHARE) / (d - 1))
    return round(S - normal * (d - 1), 2), round(normal, 2)


def nice(v):
    if v < 100: return round(v, 2)
    mag = 10 ** (int(math.log10(v)) - 2)
    return round(v / mag) * mag


def main():
    wb = openpyxl.load_workbook(PATH)
    st = [copy.copy(wb['Boss'].cell(r, 1)._style) for r in (1, 2, 3, 4)]
    if 'Stage' in wb.sheetnames: del wb['Stage']
    ws = wb.create_sheet('Stage', 0)
    head = ['층', '방 수', '적정 전투력', '물건 체력 배율', '치즈 배율', '계단 벽 배율', '일반 벽 배율', '경로 벽 합', '추가 제한시간', '메모']
    keys = ['floor', 'rooms', 'pow_need', 'item_hp', 'cheese', 'wall_stairs', 'wall_normal', 'wall_path', 'time_add', '-']
    types = ['int', 'int', 'float', 'float', 'float', 'float', 'float', 'float', 'float', '-']
    for c, (h, k, t) in enumerate(zip(head, keys, types), 1):
        for r, v in enumerate((h, k, t), 1): ws.cell(r, c, v)._style = copy.copy(st[r - 1])
    for f in range(1, FLOORS + 1):
        row = [f, rooms(f), nice(POW0 * POW_GROW ** (f - 1)), nice(HP_GROW ** (f - 1)), nice(CHEESE_GROW ** (f - 1)),
               *walls(f), wall_path(f), 0, '보스 층' if f % BOSS_EVERY == 0 else '']
        for c, v in enumerate(row, 1): ws.cell(3 + f, c, v)._style = copy.copy(st[3])
    for c, w in enumerate([6, 8, 14, 14, 12, 12, 12, 12, 14, 12], 1): ws.column_dimensions[openpyxl.utils.get_column_letter(c)].width = w
    cd = wb['Column_Desc']
    have = {cd.cell(r, 1).value for r in range(4, cd.max_row + 1)}
    for k, d in [('pow_need', 'Stage: 적정 전투력(찍찍!!) = 무리 공격력 합. 모자라면 벽 피해 = (전투력÷적정)^지수 (계단 방 1.5 · 일반 0.5)'),
                 ('item_hp', 'Stage: 물건·사람·고양이 체력 = 기본(12 × 물건 체력 배율) × 이 값 × 3.6^(방 거리 × 0.1)'),
                 ('cheese', 'Stage: 물건·사람·고양이·보스 치즈 = 기본(3 × 치즈 배율) × 이 값 × 1.8^(방 거리 × 0.1)'),
                 ('wall_stairs / wall_normal', 'Stage: 벽 체력 = 적정 전투력 × 이 값. 생성기가 층 지형(계단까지 경로)을 보고 경로 벽 합이 목표가 되게 정함'),
                 ('wall_path', 'Stage: 계단까지 최단 경로 벽 배율 합 목표. 판마다 지형이 랜덤이라 게임(StageManager)이 계단 거리 d 로 나눔: 일반 = max(0.8, 합×0.5÷(d-1)), 계단 = 나머지. 0 이면 wall_stairs·wall_normal 사용'),
                 ('time_add', 'Stage: 층 제한시간에 더하는 초 (기본 190 + 35 × 방 수 + 보스 층 90)'),
                 ('Stage 생성', 'Tools/gen_stage_table.py 로 생성 (곡선 숫자를 바꾸고 다시 실행)')]:
        if k in have: continue
        r = cd.max_row + 1
        for c, v in enumerate((k, d), 1): cd.cell(r, c, v)._style = copy.copy(cd.cell(r - 1, c)._style)
    wb.save(PATH)
    print('Stage', FLOORS, '층 · 1층', POW0, '· 10층', nice(POW0 * POW_GROW ** 9), '· 20층', nice(POW0 * POW_GROW ** 19))


if __name__ == '__main__':
    main()
