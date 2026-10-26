"""경제 생성기 (2026-10-12, 층 별 허들 기획서 Ver02 · 상점 · 뽑기 기획서 기준)
기획서의 '표준 플레이' 판 일정(판마다 최고 층)대로 돌았을 때 버는 연구자료·치즈로
 · 훈장 승급 연구자료 비용 (티어 테이블 research_cost) · 훈장 조건 최고 층 (Max_Floor)
 · 공용 스킬 트리 비용 (gen_skill_tree.py 가 읽는 Tools/economy_params.json: RESEARCH · CHEESE_PER_RES · T1_CHEESE_TOTAL)
을 정한다. 즉 '그 훈장을 받을 즈음 번 만큼이 그 훈장 트리 + 다음 승급 값'.
층 연구자료 = 스테이지 테이블 research (적정 찍찍 비례). 층 치즈 수입 = K(층) × 치즈 배율 × 방 수 (K = 측정 round8_econ 에서 맞춘 지수 곡선).
16층부터는 뽑기(연구자료)·17층부터는 상점(치즈)도 써야 하니 훈장 몫을 LATE_SHARE 로 줄임.
사용: python Tools/gen_stage_table.py && python Tools/gen_economy.py && python Tools/gen_skill_tree.py && python Tools/xlsx2json.py"""
import copy, csv, io, json, math, os
import openpyxl

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STAGE = os.path.join(ROOT, 'Data_Table', '스테이지 테이블.xlsx')
TIER = os.path.join(ROOT, 'Data_Table', '티어 테이블.xlsx')
OUT = os.path.join(ROOT, 'Tools', 'economy_params.json')
PROBE = os.path.join(ROOT, 'Tools', 'probe_results', 'round8_econ.csv')

# 표준 플레이 판별 최고(클리어) 층 — 기획서 11장 (1~21판) + 21장 (21~30층 판 수)
RUNS = [4, 4, 5, 7, 9, 11, 13, 15, 15, 16, 16, 17, 17, 17, 18, 18, 18, 19, 19, 19, 20]
for f, n in zip(range(21, 31), [2, 2, 3, 3, 3, 2, 2, 3, 3, 3]):
    RUNS += [f - 1] * (n - 1) + [f]
# 훈장 t 를 받는 클리어 층 (사용자 2026-10-12: 기획서 훈장 5·10·15·19층에 맞춰 재배치)
import tier_gates as TG           # 2026-10-19 재밸런싱: 훈장 조건 층 · 승급 비용 배율 = Tools/tier_gates.py
TIER_CLEAR = {t: TG.GATE[t] - 1 for t in range(2, 9)}   # 예전 {2: 5, 3: 8, 4: 10, 5: 13, 6: 15, 7: 17, 8: 20}      # 8훈장 = 20층 보스 클리어(21층 도달) (2026-10-19: 허들을 19층 → 20층 보스로, 9훈장~ 31·41층과 같은 규칙)
T8_TREE_END = 23                     # 8훈장 트리는 이 층을 깰 때까지 (21~30층 엔드게임) 다 찍게
PROMO_FRAC = {2: 0.34, 3: 0.35, 4: 0.42, 5: 0.47, 6: 0.49, 7: 0.535}   # 그 구간 연구자료 중 다음 승급 몫 (나머지 = 트리). 예전 비율 유지
CHEESE_SHARE = 0.85                  # 치즈 수입 중 트리에 쓰는 몫 (나머지 여유)
LATE_SHARE = 0.5                     # 뽑기·상점이 열린 뒤(16층~) 훈장에 쓰는 몫
# 비용 배율 (2026-10-19, Tools/balance_sim.py 로 맞춤: 기본 제한시간 120초 · 클릭 적극 ~10h / 거의 안 함 ~15h · 허들 시간표). 훈장 t 의 승급 연구자료와 t 트리 비용에 곱함
COST_SCALE = {2: 1.3, 3: 1.3, 4: 1.3, 5: 1.3, 6: 0.9, 7: 0.9, 8: 0.9}   # 2차 (치즈·조각 넣은 뒤) 6~8 ×0.9
SHOP_FLOORS = 3                      # 상점 한 번 새로 고침(6칸) 전부 사는 값 ≈ 층 치즈 수입의 이 배
SHOP_REFRESH_BC = 4800               # 6칸 평균 가격 합 (BC 단위, 상점 테이블 price_cheese_bc)
TREE_UNIT = {2: 108.33, 3: 85.02, 4: 81.06, 5: 79.71, 6: 79.54, 7: 72.97, 8: 74.95}   # gen_skill_tree: 트리 연구자료 합 ÷ RESEARCH[t]


def load_stage():
    ws = openpyxl.load_workbook(STAGE, data_only=True)['Stage']
    keys = [c.value for c in ws[2]]
    rows = {}
    for r in ws.iter_rows(min_row=4, values_only=True):
        if r[0]: rows[int(r[0])] = dict(zip(keys, r))
    return rows


def fit_k():
    # 측정: 치즈 = K × 1.5^(층-1)(옛 치즈 배율) × 방 수. K 를 지수 곡선 a × b^(층-1) 로 (1~20층)
    pts = []
    for r in csv.DictReader(open(PROBE)):
        f = int(r['floor'])
        if f <= 20: pts.append((f - 1, math.log(float(r['cheese']) / (1.5 ** (f - 1) * int(r['rooms'])))))
    n = len(pts); sx = sum(p[0] for p in pts); sy = sum(p[1] for p in pts)
    sxx = sum(p[0] ** 2 for p in pts); sxy = sum(p[0] * p[1] for p in pts)
    b = (n * sxy - sx * sy) / (n * sxx - sx * sx); a = (sy - b * sx) / n
    return math.exp(a), math.exp(b)


def main():
    st = load_stage(); ka, kb = fit_k()
    res = lambda f: float(st[f]['research'])
    chz = lambda f: ka * kb ** (f - 1) * float(st[f]['cheese']) * int(st[f]['rooms'])
    run_chz = [sum(chz(f) for f in range(1, b + 1)) for b in RUNS]
    def run_of(clear): return next(i for i, b in enumerate(RUNS) if b >= clear)      # 그 층을 처음 깬 판 (0부터)
    # 1훈장 트리 치즈 = 2훈장 조건 층을 처음 깰 때까지 판들의 치즈 × CHEESE_SHARE (기획서 판 일정)
    params = {'RESEARCH': {1: 0}, 'CHEESE_PER_RES': {}, 'T1_CHEESE_TOTAL': round(sum(run_chz[:run_of(TIER_CLEAR[2]) + 1]) * CHEESE_SHARE, -2)}
    # 2~8훈장 승급 연구자료 = 조건 층 연구자료 × TG.RANK_K (2026-10-19: 9~16훈장과 같은 식. 예전 판 일정 방식은 30층까지만 있어 조건 층을 30층 위로 못 옮김)
    #   트리 연구자료(RESEARCH)·노드 치즈(CHEESE_PER_RES) 는 gen_skill_tree 가 승급 비용 · 층대 치즈 수입 비율로 정함
    promo = {t: res(TG.GATE[t]) * TG.rank_k(t) for t in range(2, 9)}
    report = [f'1훈장 트리 치즈 {params["T1_CHEESE_TOTAL"]:,.0f}']
    json.dump(params, io.open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    # 티어 테이블: 승급 연구자료 · 조건 최고 층 (Max_Floor = 도달 층 = 클리어 + 1)
    wb = openpyxl.load_workbook(TIER); ws = wb['Tier']
    K = [c.value for c in ws[2]]          # 칸 이름으로 찾음 (2026-10-19: 숫자 위치로 쓰다가 _sk 칸이 생긴 뒤 아이콘 칸을 덮어썼음)
    rc = K.index('research_cost')
    for row in ws.iter_rows(min_row=4):
        t = row[0].value
        if not isinstance(t, int) or t < 2 or t > 8: continue          # 9훈장~ = Tools/gen_expansion_data.py (100층 확장)
        v = promo[t]
        row[rc].value = int(round(v, -1 if v > 100 else 0))
        for i in range(1, 5):
            if f'cond{i}_type' in K and row[K.index(f'cond{i}_type')].value == 'Max_Floor': row[K.index(f'cond{i}_value')].value = TG.GATE[t]
            if f'cond{i}_type' in K and row[K.index(f'cond{i}_type')].value == 'Shard_Level_Sum': row[K.index(f'cond{i}_value')].value = TG.SHARD[t]
    wb.save(TIER)
    # 상점 가격 (2026-10-19 전체 밸런스): BC = bc_base × 치즈 배율 × kb^(min(층,30)-1) (BlackMarket.BC). 한 번 새로 고침 상품 전부 ≈ 층 수입 SHOP_FLOORS 배
    sp = os.path.join(ROOT, 'Data_Table', '상점 테이블.xlsx'); swb = openpyxl.load_workbook(sp); sc = swb['Shop_Config']
    rows = {sc.cell(r, 1).value: r for r in range(4, sc.max_row + 1) if sc.cell(r, 1).value}
    vals = {'bc_base': round(ka * 9 * SHOP_FLOORS / SHOP_REFRESH_BC, 2), 'bc_floor_grow': round(kb, 4), 'bc_floor_cap': 30}
    for k, v in vals.items():
        r = rows.get(k)
        if r is None:
            r = sc.max_row + 1
            for c in range(1, sc.max_column + 1): sc.cell(r, c)._style = copy.copy(sc.cell(r - 1, c)._style)
            sc.cell(r, 1, k)
            if sc.max_column >= 3: sc.cell(r, 3, {'bc_floor_grow': '층 치즈 수입 배율 (gen_economy fit_k) — BC 에 곱함', 'bc_floor_cap': '층 수입 배율을 이 층까지만'}.get(k, ''))
        sc.cell(r, 2, v)
    swb.save(sp)
    print('상점', vals)
    print(f'치즈 K = {ka:.0f} × {kb:.3f}^(층-1)')
    print('판별 최고 층', RUNS)
    for s in report: print(s)
    print('승급 연구자료', {t: int(round(v)) for t, v in promo.items()})
    print('→', OUT)


if __name__ == '__main__':
    main()
