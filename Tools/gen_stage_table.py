"""스테이지(층) 밸런스 생성기 (2026-10-12: 층 별 허들 기획서 Ver02 수치) → Data_Table/스테이지 테이블.xlsx 의 Stage 시트 (Boss 시트들은 그대로). 그다음 Tools/xlsx2json.py
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

FLOORS = 100                        # 2026-10-19 100층 확장 (기획서 Ver.01 박송호)
POW0 = 400                          # 적정 전투력 기준 (기획서 지수 100 = 400)
# 층별로 따로 정한 적정 (사용자 2026-10-13: 1층만 300, 2층부터는 400 기준 그대로. 측정 round8 — 400 이면 첫 판 2층에서, 전체 260 이면 너무 쉬움)
POW_FIX = {1: 300}
def pow_need(f): return POW_FIX.get(f, POW0 * diff(f))
BOSS_EVERY = 5                      # 보스 층 (5층마다, 사용자 2026-10-12 유지)
# 보스 층 (보스 테이블 floor 와 같게): 5층마다 90층까지 + 91~100 구간은 94·97·100층 (사용자 2026-10-19)
BOSS_FLOORS = set(range(5, 91, 5)) | {94, 97, 100}

# ── 층 별 허들 기획서 Ver02 (박도원, 2026-10-12) — 사용자 2026-10-12: 난이도·방 수·목표 시간은 기획서대로, 보스는 5층마다 그대로 ──
# 층 유형 · 이전 층 대비 적정 찍찍 배율. 21~30층 = 16~20층 배율 두 번 반복 (기획서 21장), 31층~ 도 같은 반복
DOC_MUL = {2: 1.25, 3: 1.25, 4: 1.25, 5: 1.8, **{f: 1.32 for f in range(6, 16)}, 16: 1.75, 17: 1.7, 18: 2.1, 19: 2.2, 20: 2.4}
DOC_TYPE = {1: '도입', 2: '쉬움', 3: '쉬움', 4: '쉬움', 5: '허들', **{f: '보통' for f in range(6, 16)}, 16: '허들', 17: '어려움', 18: '허들', 19: '어려움', 20: '허들'}
for _f in range(21, FLOORS + 1):
    _s = 16 + (_f - 21) % 5
    DOC_MUL[_f] = DOC_MUL[_s]; DOC_TYPE[_f] = DOC_TYPE[_s]
# 31층~ (2026-10-19, 100층 확장 기획서 10장): 1~30층은 그대로, 31층부터 층마다 약 ×1.12 (유형별로 흔들되 5층 합 ≈ 1.12^5)
LATE_MUL = {16: 1.12, 17: 1.08, 18: 1.14, 19: 1.08, 20: 1.16}
for _f in range(31, FLOORS + 1):
    DOC_MUL[_f] = LATE_MUL[16 + (_f - 21) % 5]
DOC_ROOMS = [2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9]          # 1~15층, 16층~ 9개
TYPE_SEC = {'도입': 108, '쉬움': 108, '보통': 108, '어려움': 135, '허들': 162}   # 적정 찍찍일 때 목표 돌파 (제한 180초의 60 · 75 · 90%)
BOSS_FIGHT_SHARE = 0.4               # 보스 층: 목표 시간 중 보스전 몫 (나머지 = 길 뚫기). 보스 테이블 hp_pow_sec 를 이 시간에 맞춤
# 플레이어 기준 전투력 (기획서 23장, 1층 = 100). 21층~ 은 층당 ×1.85 로 이어감 (기획서: 후반 층당 기준 성장 ×1.55 → ×1.90)
DOC_PLAYER = [100, 127, 161, 205, 273, 402, 601, 836, 1120, 1501, 2011, 2694, 3611, 4838, 6483, 9343, 14855, 24852, 43749, 81034]
# 층 건너뛰기 상한 (기획서 26장): 최고 '클리어' 층 → 고를 수 있는 가장 높은 시작 층. 오르는 건 공용 스킬 '스테이지 스킵' 노드 (사용자 2026-10-12)
DOC_SKIP = {5: 3, 6: 4, 7: 4, 8: 5, 9: 6, 10: 6, 11: 7, 12: 8, 13: 9, 14: 9, 15: 10, 16: 10, 17: 10, 18: 10, 19: 10}
RESEARCH0 = 8                        # 층 클리어 연구자료: 1층 8, 적정 찍찍에 비례 (뽑기 기획서 BR 가정)
CHEESE_EXP = 1.25                    # 치즈 배율 = (난이도 지수)^이 값 (예전 치즈 1.5^층 · 체력 1.38^층 관계 유지)


def diff(f):                         # 난이도 지수 (1층 = 1)
    d = 1.0
    for g in range(2, f + 1): d *= DOC_MUL[g]
    return d


def ftype(f): return DOC_TYPE.get(f, '허들')
def is_boss(f): return f in BOSS_FLOORS
def target_sec(f): return TYPE_SEC[ftype(f)]
def player_pow(f):
    if f <= len(DOC_PLAYER): return DOC_PLAYER[f - 1]
    v = DOC_PLAYER[-1] * 1.85 ** (min(f, 30) - len(DOC_PLAYER))
    return v * diff(f) / diff(30) if f > 30 else v          # 31층~ 은 적정 찍찍과 같은 비율 (2026-10-19)
def skip_cap(f):                     # 30층까지 기획서 (최대 15) · 31층~ 최고 클리어 층의 80% (5층 단위, 최대 80 — 100층 확장 기획서 14장)
    if f < 20: return DOC_SKIP.get(f, 1)
    return max(15, min(80, int(f * 0.8) // 5 * 5)) if f > 30 else 15


def rooms(f):
    return DOC_ROOMS[f - 1] if f <= len(DOC_ROOMS) else 9


# ── 벽 체력: 층마다 실제 지형(계단까지 최단 경로)을 계산해서 "경로 벽 배율 합"이 목표 S(f) 가 되게 ──
# 목표: 전투력 = 적정일 때 층 유형별 목표 초 (쉬움·보통 108 · 어려움 135 · 허들 162, 제한 180초), 보스 층은 보스전 몫 빼고
# 측정(BalanceProbe 적정 고정): 걸린 시간 ≈ 경로 벽 배율 합(공용 스킬 벽 체력 감소 적용 후) × SPU[f] (배율 1당 초).
#   SPU 는 지형마다 크게 다름 (복도처럼 바깥벽이 많은 지형은 쥐가 엉뚱한 벽에 부딪혀 느림) → 층별 측정값. 없으면 SPU_DEFAULT
#   층을 다시 재면 SPU 를 고치고 다시 실행.  S = 목표 초 ÷ (SPU × SKILL_WALL_MUL)
# (2026-10-12) 목표 초 = 층 유형별 (TYPE_SEC), 보스 층은 그중 BOSS_FIGHT_SHARE 만큼이 보스전
SPU = {                               # 측정 BalanceProbe (Tools/update_spu.py 로 갱신)
    1: 1.29, 2: 1.75, 3: 1.58, 4: 2.85, 5: 21.2, 6: 8.61, 7: 7.12, 8: 10.0, 9: 16.37, 10: 8.67,
    11: 40.96, 12: 13.0, 13: 4.76, 14: 8.32, 15: 32.44, 16: 1.04, 17: 16.35, 18: 6.71, 19: 5.63, 20: 4.24,
    21: 4.27, 22: 1.82, 23: 4.3, 24: 2.56, 25: 3.08,
}
SPU_DEFAULT = 10.0
SKILL_WALL_MUL = {1: 1.0, 2: 0.97, 3: 0.95, 4: 0.92, 5: 0.9, 6: 0.9, 7: 0.87}   # 그 층을 깰 즈음의 공용 스킬 벽 체력 배율 (8층부터 0.85)
STAIRS_SHARE = 0.5                    # 경로 벽 합 중 계단 방 벽 몫 (나머지는 경로의 일반 벽에 똑같이)
MIN_NORMAL = 0.8                      # 일반 벽 배율 최소


def path_sec(f): return target_sec(f) * (1 - BOSS_FIGHT_SHARE) if is_boss(f) else target_sec(f)
def fight_sec(f): return target_sec(f) * BOSS_FIGHT_SHARE if is_boss(f) else 0


def path_target(f):
    return path_sec(f) / (SPU.get(f, SPU_DEFAULT) * SKILL_WALL_MUL.get(f, 0.85))


# 판마다 지형이 랜덤 (StageManager.randomLayout, 2026-10-08 사용자) → 층별 SPU 는 그 층 지형 하나에 맞춘 값이라 그대로 못 씀.
# 앞뒤 SMOOTH 층 측정값의 중앙값으로 매끄럽게 → wall_path = 경로 벽 배율 합 목표. 게임이 생성된 지형의 계단 거리로 계단/일반 벽에 나눔
SMOOTH = 2
def spu_smooth(f):
    v = sorted(SPU[g] for g in range(f - SMOOTH, f + SMOOTH + 1) if g in SPU)
    return v[len(v) // 2] if v else SPU_DEFAULT


def wall_path(f):
    return round(path_sec(f) / (spu_smooth(f) * SKILL_WALL_MUL.get(f, 0.85)), 2)


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
    return round(max(MIN_NORMAL, S - normal * (d - 1)), 2), round(normal, 2)


def nice(v):
    if v < 100: return round(v, 2)
    mag = 10 ** (int(math.log10(v)) - 2)
    return round(v / mag) * mag


def main():
    wb = openpyxl.load_workbook(PATH)
    st = [copy.copy(wb['Stage'].cell(r, 1)._style) for r in (1, 2, 3, 4)]       # 머리·데이터 칸 스타일 (Stage 시트를 지우기 전에 복사)
    if 'Stage' in wb.sheetnames: del wb['Stage']
    ws = wb.create_sheet('Stage', 0)
    head = ['층', '방 수', '적정 전투력', '물건 체력 배율', '치즈 배율', '계단 벽 배율', '일반 벽 배율', '경로 벽 합', '추가 제한시간', '층 유형', '목표 돌파 (초)', '보스전 목표 (초)', '클리어 연구자료', '건너뛰기 상한', '기준 전투력 (지수)', '메모']
    keys = ['floor', 'rooms', 'pow_need', 'item_hp', 'cheese', 'wall_stairs', 'wall_normal', 'wall_path', 'time_add', 'floor_type', 'target_sec', 'boss_sec', 'research', 'skip_cap', 'player_pow', '-']
    types = ['int', 'int', 'float', 'float', 'float', 'float', 'float', 'float', 'float', 'string', 'float', 'float', 'float', 'int', 'float', '-']
    for c, (h, k, t) in enumerate(zip(head, keys, types), 1):
        for r, v in enumerate((h, k, t), 1): ws.cell(r, c, v)._style = copy.copy(st[r - 1])
    for f in range(1, FLOORS + 1):
        D = diff(f)
        row = [f, rooms(f), nice(pow_need(f)), nice(D), nice(D ** CHEESE_EXP),
               *walls(f), wall_path(f), 0, ftype(f), target_sec(f), round(fight_sec(f), 1), nice(RESEARCH0 * D), skip_cap(f), nice(player_pow(f)),
               ('보스 층 · ' if is_boss(f) else '') + ftype(f)]
        for c, v in enumerate(row, 1): ws.cell(3 + f, c, v)._style = copy.copy(st[3])
    for c, w in enumerate([6, 8, 14, 14, 12, 12, 12, 12, 14, 10, 12, 12, 14, 12, 14, 16], 1): ws.column_dimensions[openpyxl.utils.get_column_letter(c)].width = w
    cd = wb['Column_Desc']
    have = {cd.cell(r, 1).value for r in range(4, cd.max_row + 1)}
    for k, d in [('pow_need', 'Stage: 적정 전투력(찍찍!!) = 무리 공격력 합. 모자라면 벽 피해 = (전투력÷적정)^지수 (계단 방 1.5 · 일반 0.5)'),
                 ('item_hp', 'Stage: 물건·사람·고양이 체력 = 기본(12 × 물건 체력 배율) × 이 값 × 3.6^(방 거리 × 0.1)'),
                 ('cheese', 'Stage: 물건·사람·고양이·보스 치즈 = 기본(3 × 치즈 배율) × 이 값 × 1.8^(방 거리 × 0.1)'),
                 ('wall_stairs / wall_normal', 'Stage: 벽 체력 = 적정 전투력 × 이 값. 생성기가 층 지형(계단까지 경로)을 보고 경로 벽 합이 목표가 되게 정함'),
                 ('wall_path', 'Stage: 계단까지 최단 경로 벽 배율 합 목표. 판마다 지형이 랜덤이라 게임(StageManager)이 계단 거리 d 로 나눔: 일반 = max(0.8, 합×0.5÷(d-1)), 계단 = 나머지. 0 이면 wall_stairs·wall_normal 사용'),
                 ('time_add', 'Stage: 층 제한시간에 더하는 초 (기본 190 + 35 × 방 수 + 보스 층 90)'),
                 ('Stage 생성', 'Tools/gen_stage_table.py 로 생성 (곡선 숫자를 바꾸고 다시 실행)'),
                 ('floor_type', 'Stage: 층 유형 (층 별 허들 기획서 Ver02): 도입·쉬움·보통·어려움·허들. 목표 돌파 시간과 적정 찍찍 배율이 유형별'),
                 ('target_sec / boss_sec', 'Stage: 적정 찍찍일 때 목표 돌파 초 (제한 180초의 60·75·90%) · 그중 보스전 목표 초 (보스 테이블 hp_pow_sec 로 맞춤)'),
                 ('research', 'Stage: 층 클리어(계단) 연구자료 = 8 × 난이도 지수 (Heist). 뽑기 비용 단위 BR = 최고 도달 층의 이 값'),
                 ('skip_cap', 'Stage: 이 층을 최고로 클리어했을 때 고를 수 있는 가장 높은 시작 층 (기획서 26장). 실제 = min(1 + 스테이지 스킵 노드 합, 이 값)'),
                 ('player_pow', 'Stage: 기획서 플레이어 기준 전투력 (1층 = 100 지수). 측정 비교용 (게임은 안 씀)')]:
        if k in have: continue
        r = cd.max_row + 1
        for c, v in enumerate((k, d), 1): cd.cell(r, c, v)._style = copy.copy(cd.cell(r - 1, c)._style)
    wb.save(PATH)
    print('Stage', FLOORS, '층 · 1층', pow_need(1), '· 2층', nice(pow_need(2)), '· 10층', nice(POW0 * diff(10)), '· 20층', nice(POW0 * diff(20)), '· 30층', nice(POW0 * diff(30)))


if __name__ == '__main__':
    main()
