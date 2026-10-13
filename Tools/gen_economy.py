"""경제 생성기 (2026-10-12, 층 별 허들 기획서 Ver02 · 상점 · 뽑기 기획서 기준)
기획서의 '표준 플레이' 판 일정(판마다 최고 층)대로 돌았을 때 버는 연구자료·치즈로
 · 훈장 승급 연구자료 비용 (티어 테이블 research_cost) · 훈장 조건 최고 층 (Max_Floor)
 · 공용 스킬 트리 비용 (gen_skill_tree.py 가 읽는 Tools/economy_params.json: RESEARCH · CHEESE_PER_RES · T1_CHEESE_TOTAL)
을 정한다. 즉 '그 훈장을 받을 즈음 번 만큼이 그 훈장 트리 + 다음 승급 값'.
층 연구자료 = 스테이지 테이블 research (적정 찍찍 비례). 층 치즈 수입 = K(층) × 치즈 배율 × 방 수 (K = 측정 round8_econ 에서 맞춘 지수 곡선).
16층부터는 뽑기(연구자료)·17층부터는 상점(치즈)도 써야 하니 훈장 몫을 LATE_SHARE 로 줄임.
사용: python Tools/gen_stage_table.py && python Tools/gen_economy.py && python Tools/gen_skill_tree.py && python Tools/xlsx2json.py"""
import csv, io, json, math, os
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
TIER_CLEAR = {2: 5, 3: 8, 4: 10, 5: 13, 6: 15, 7: 17, 8: 19}
T8_TREE_END = 23                     # 8훈장 트리는 이 층을 깰 때까지 (21~30층 엔드게임) 다 찍게
PROMO_FRAC = {2: 0.34, 3: 0.35, 4: 0.42, 5: 0.47, 6: 0.49, 7: 0.535}   # 그 구간 연구자료 중 다음 승급 몫 (나머지 = 트리). 예전 비율 유지
CHEESE_SHARE = 0.85                  # 치즈 수입 중 트리에 쓰는 몫 (나머지 여유)
LATE_SHARE = 0.5                     # 뽑기·상점이 열린 뒤(16층~) 훈장에 쓰는 몫
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
    run_res = [sum(res(f) for f in range(1, b + 1)) for b in RUNS]
    run_chz = [sum(chz(f) for f in range(1, b + 1)) for b in RUNS]
    def run_of(clear): return next(i for i, b in enumerate(RUNS) if b >= clear)      # 그 층을 처음 깬 판 (0부터)
    att = {1: -1, **{t: run_of(c) for t, c in TIER_CLEAR.items()}}
    span = {t: (att[t] + 1, (att[t + 1] if t < 8 else run_of(T8_TREE_END)) + 1) for t in range(1, 9)}   # 훈장 t 동안의 판 [a, b)
    params = {'RESEARCH': {1: 0}, 'CHEESE_PER_RES': {}, 'T1_CHEESE_TOTAL': 0}
    promo = {}
    report = []
    for t in range(1, 9):
        a, b = span[t]
        late = LATE_SHARE if t >= 6 else 1          # 6훈장 구간(16층~)부터 뽑기·상점과 나눠 씀
        R = sum(run_res[a:b]) * late; C = sum(run_chz[a:b]) * CHEESE_SHARE * late
        if t == 1:
            promo[2] = R; params['T1_CHEESE_TOTAL'] = round(C, -2)
            report.append(f'훈장 1: 판 {a + 1}~{b} · 연구자료 {R:,.0f} → 2훈장 승급 · 치즈 {C:,.0f} → 1트리')
            continue
        pf = PROMO_FRAC.get(t, 0)
        tree_r = R * (1 - pf)
        if t < 8: promo[t + 1] = R * pf
        params['RESEARCH'][t] = round(tree_r / TREE_UNIT[t], 3)
        params['CHEESE_PER_RES'][t] = round(C / max(1, tree_r))
        report.append(f'훈장 {t}: 판 {a + 1}~{b} · 연구자료 {R:,.0f} (트리 {tree_r:,.0f}' + (f' · {t + 1}훈장 승급 {R * pf:,.0f})' if t < 8 else ')') + f' · 치즈 {C:,.0f}')
    json.dump(params, io.open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    # 티어 테이블: 승급 연구자료 · 조건 최고 층 (Max_Floor = 도달 층 = 클리어 + 1)
    wb = openpyxl.load_workbook(TIER); ws = wb['Tier']
    for row in ws.iter_rows(min_row=4):
        t = row[0].value
        if not isinstance(t, int) or t < 2: continue
        row[3].value = int(round(promo[t], -1 if promo[t] > 100 else 0))
        for ci in (4, 6, 8):
            if row[ci].value == 'Max_Floor': row[ci + 1].value = TIER_CLEAR[t] + 1
    wb.save(TIER)
    print(f'치즈 K = {ka:.0f} × {kb:.3f}^(층-1)')
    print('판별 최고 층', RUNS)
    for s in report: print(s)
    print('승급 연구자료', {t: int(round(v)) for t, v in promo.items()})
    print('→', OUT)


if __name__ == '__main__':
    main()
