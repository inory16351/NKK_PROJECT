"""balance_sim 시간 모델 보정 (2026-10-26) — BalanceProbe 측정(실제 판)과 시뮬 가정의 차이를 식 하나로 맞춤 → Tools/probe_results/sim_calib.json
차이 두 가지
 · 판 안 쥐 수: 시뮬은 훈장 끝 전투력(power_by_tier = 최대 마릿수 포화)을 판 첫 층부터 씀. 실제는 판에서 몇 층 지났는지(n)에 따라 쥐가 불어남 (10~25마리)
 · 전투력 밖 피해: 필살기 · 증강 · 벽 피해 % · 치명타 등 → 후반은 적정의 0.15~0.4배로도 깸
식 두 단계
 ① 실제 찍찍!! = 시뮬 한 마리(power_by_tier × rank_mul ÷ 최대 마릿수×0.8) × exp(k0 + k1·(1−트리) + k2·max(0,훈장−12)/4) × 쥐 수(a0 + a1·n)
    (실측: 쥐 수는 훈장과 거의 무관하게 판 첫 층 ~11 → 8층째 ~20마리. 시뮬 예전 가정 = 최대 마릿수 포화)
 ② 층 시간 = exp(c0) × 경로 벽 합^c1 × (적정 ÷ 실제 찍찍!!)^c2 × exp(c3·보스 + c4·층/100) — 벽 체력 ÷ 피해 꼴
    (예전 '목표 초 × time_mul' 은 층마다 실제 벽 양이 크게 달라 1층 0.62배 10초를 164초로 봤음. 층/100 항 = 후반 필살기·증강 등 전투력 밖 피해)
    실패 층 = 제한시간 × FAIL_X 걸렸다고 보고 같이 맞춤 (성공만 쓰면 낙관으로 치우침) · 경로 벽 합 = 테이블 wall_path (시뮬과 같게)
 맞춘 뒤 각 측정 층이 성공/실패로 맞게 나오는지 셈
사용: python Tools/calib_from_probe.py [round10_hourly.txt round11_calib.csv ...] [--write]"""
import csv, json, math, os, re, sys
sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import balance_sim as B
import tier_gates as TG
import numpy as np

PR = os.path.join(HERE, 'probe_results')
OUT = os.path.join(PR, 'sim_calib.json')
UNIT = {'': 1, 'K': 1e3, 'M': 1e6, 'B': 1e9, 'T': 1e12, 'aa': 1e15, 'ab': 1e18, 'ac': 1e21, 'ad': 1e24, 'ae': 1e27}
POWER = json.load(open(B.POWER, encoding='utf-8'))
TREE_N = {t: len(B.Player(POWER).by_tier.get(t, [])) for t in range(1, 17)}


def num(s):
    m = re.match(r'([0-9.]+)([A-Za-z]*)$', s.strip()); return float(m.group(1)) * UNIT[m.group(2)]


def from_txt(path):
    """BalanceProbe Results 글 (round10 형식: 판 머리 줄 + 층 줄)"""
    pts, tier, tree, n = [], 0, 0, 0
    for line in open(path, encoding='utf-8'):
        h = re.match(r'── 티어 (\d+).*?(\d+)층부터.*지금 트리 (\d+)', line)
        if h: tier, start, tree, n = int(h.group(1)), int(h.group(2)), int(h.group(3)), 0; continue
        pm = re.search(r'경로 벽 배율 합 ([0-9.]+)', line)
        if pm and pts and pts[-1]['ok'] and pts[-1].get('path') is None: pts[-1]['path'] = float(pm.group(1)); continue
        ok = re.match(r'(\d+)층 ([0-9.]+)초 / 제한 ([0-9.]+)초.*쥐 (\d+).*전투력 ([0-9.]+[A-Za-z]*) / 적정 ([0-9.]+[A-Za-z]*)', line)
        bad = re.match(r'(\d+)층 실패 \(제한 ([0-9.]+)초.*쥐 (\d+).*전투력 ([0-9.]+[A-Za-z]*) / 적정 ([0-9.]+[A-Za-z]*)', line)
        if ok:
            f, used, lim, rats, pw, need = int(ok.group(1)), float(ok.group(2)), float(ok.group(3)), int(ok.group(4)), num(ok.group(5)), num(ok.group(6))
            pts.append(dict(tier=tier, tree=tree, n=n, floor=f, used=used, limit=lim, ok=1, rats=rats, pow=pw, need=need)); n += 1
        elif bad:
            f, lim, rats, pw, need = int(bad.group(1)), float(bad.group(2)), int(bad.group(3)), num(bad.group(4)), num(bad.group(5))
            pts.append(dict(tier=tier, tree=tree, n=n, floor=f, used=lim, limit=lim, ok=0, rats=rats, pow=pw, need=need))
    return pts


def from_csv(path):
    """BalanceProbe.RowsCsv (2026-10-26 칸 추가: tier,tree,start,n,rats,pow,need). 제한시간이 없어 실패 층은 balance_sim 제한(기본 + 트리 시간)으로"""
    pts = []
    for r in csv.DictReader(open(path, encoding='utf-8')):
        if 'tier' not in r or not r['tier']: continue
        f, tier = int(float(r['floor'])), int(float(r['tier']))
        pts.append(dict(tier=tier, tree=int(float(r['tree'])), n=int(float(r['n'])), floor=f, used=float(r['used']), limit=None, ok=int(float(r['ok'])),
                        rats=int(float(r['rats'])), pow=float(r['pow']), need=float(r['need']), path=float(r['path']) if r['ok'] != '0' else None))
    return pts


def sim_pow(tier, tree):
    p0, p1 = POWER[str(tier)]['start'], POWER[str(tier)]['end']
    return p0 * (p1 / p0) ** (tree / max(1, TREE_N[tier])) * TG.rank_mul(tier)


def inv_time(x):
    """time_mul(r) = x 인 r (x = 층 시간 ÷ 목표 초)"""
    return (B.TIME_C / x) ** (1 / B.TIME_LO) if x > B.TIME_C else (B.TIME_C / x) ** (1 / B.TIME_HI)


def per_feat(t, frac): return [1.0, 1 - frac, max(0, t - 12) / 4]          # 한 마리 보정: 트리 안 찍은 몫 · 13훈장~ (후반 트리 효과가 측정값보다 큼)
def pop_of(t): return POWER[str(t)].get('pop', 100) * 0.8                      # power_by_tier end 가 가정한 마릿수
def model_pow(t, frac, n, cal):
    a, k = cal['rats'], cal['per_rat']
    rats = min(pop_of(t), a[0] + a[1] * n)
    per = POWER[str(t)]['start'] * (POWER[str(t)]['end'] / POWER[str(t)]['start']) ** frac * TG.rank_mul(t) / pop_of(t)
    return per * math.exp(k[0] + k[1] * (1 - frac) + k[2] * max(0, t - 12) / 4) * rats
def time_feat(f, path, need, pow_):
    return [1.0, math.log(max(0.3, path)), math.log(need / max(1e-9, pow_)), 1.0 if f in B.BOSS_FLOORS else 0.0, f / 100]
def model_time(f, path, need, pow_, cal): return math.exp(float(np.dot(cal['time'], time_feat(f, path, need, pow_))))
FAIL_X = 2.0


def fit(X, y):
    coef, *_ = np.linalg.lstsq(np.array(X), np.array(y), rcond=None); r = np.array(y) - np.array(X) @ coef
    return coef, float(r.std())


def main():
    files = [a for a in sys.argv[1:] if not a.startswith('-')] or ['round10_hourly.txt']
    pts = []
    for fn in files:
        p = fn if os.path.isabs(fn) else os.path.join(PR, fn)
        pts += from_csv(p) if p.endswith('.csv') else from_txt(p)
    for q in pts:
        q['target'] = B.STAGE[q['floor']]['target_sec']
        q['sim'] = sim_pow(q['tier'], q['tree'])
        if q['limit'] is None and not q['ok']: q['limit'] = q['used']
        q['path_meas'] = q.get('path'); q['path'] = B.STAGE[q['floor']]['wall_path'] * (0.02 if q['floor'] == 1 else 1)      # 1층 = 초반 계단 벽 완화 (실측 0.7 / 테이블 36)      # 시뮬과 같게 테이블 값 (실측 ≈ 테이블 × 0.8~0.87 = 공용 스킬 벽 체력 감소)
    # 첫 판 '쉬운 방' (StageManager.firstRunWallMul, 판 0~1회 · 튜토리얼 라운드) = 실측 벽이 테이블의 30% 미만 → 빼고 맞춤 (시뮬은 2판째부터 기준)
    easy = [q for q in pts if q['path_meas'] and q['path_meas'] < 0.3 * q['path']]
    pts = [q for q in pts if q not in easy]
    if easy: print('쉬운 방 층 뺌:', [(q['tier'], q['floor']) for q in easy])
    # ① 전투력 = 시뮬 한 마리 × 한 마리 보정 × 판 안 쥐 수.  쥐 수 = a0 + a1·n (훈장과 거의 무관, 실측 10.7 → 20.5)
    ca, sa = fit([[1.0, q['n']] for q in pts], [q['rats'] for q in pts])
    cp, sp = fit([per_feat(q['tier'], q['tree'] / max(1, TREE_N[q['tier']])) for q in pts],
                 [math.log(max(1, q['pow']) / max(1, q['rats']) / (q['sim'] / pop_of(q['tier']))) for q in pts])
    cal = {'rats': [float(c) for c in ca], 'per_rat': [float(c) for c in cp]}
    # ② 층 시간 (실측 전투력으로 맞춤)
    ok = [q for q in pts if q['ok']]
    ce, se = fit([time_feat(q['floor'], q['path'], q['need'], q['pow']) for q in pts], [math.log(max(1.0, q['used'] if q['ok'] else q['limit'] * FAIL_X)) for q in pts])
    cal['time'] = [float(c) for c in ce]
    print(f'점 {len(pts)} (성공 {len(ok)})')
    print(f'① 쥐 수 = {ca[0]:.1f} + {ca[1]:.2f}·n (오차 ±{sa:.1f}마리) · 한 마리 log(실측/시뮬) = {cp[0]:.2f} + {cp[1]:.2f}·(1-트리) + {cp[2]:.2f}·max(0,훈장-12)/4  · 오차 ×{math.exp(sp):.2f}')
    print(f'② log 층 시간 = {ce[0]:.2f} + {ce[1]:.2f}·log(경로 벽 합) + {ce[2]:.2f}·log(적정/찍찍) + {ce[3]:.2f}·보스 + {ce[4]:.2f}·층/100  · 오차 ×{math.exp(se):.2f}')
    hit = 0
    for q in pts:
        P = model_pow(q['tier'], q['tree'] / max(1, TREE_N[q['tier']]), q['n'], cal)
        pred = model_time(q['floor'], q['path'], q['need'], P, cal)
        right = (pred <= (q['limit'] or 1e9)) == bool(q['ok']); hit += right; q['pred'] = pred; q['right'] = right; q['P'] = P
    print(f'성공/실패 맞춘 비율 {hit}/{len(pts)}')
    if '-v' in sys.argv:
        for q in pts:
            print(f"  T{q['tier']:>2} 트리{q['tree']:>2} n{q['n']} {q['floor']:>3}층 {'통과' if q['ok'] else '실패'} 실측 {q['used']:5.0f}초 · 예상 {q['pred']:5.0f}초 {'' if q['right'] else '← 틀림'} · 전투력 실측 {q['pow']:.3g} 예상 {q['P']:.3g}")
    if '--write' in sys.argv:
        json.dump({'rats': [round(c, 4) for c in cal['rats']], 'per_rat': [round(c, 4) for c in cal['per_rat']],
                   'pow_form': 'P = power_by_tier(t, tree) * rank_mul / (pop*0.8) * exp(k0 + k1*(1-tree) + k2*max(0,t-12)/4) * min(pop*0.8, a0 + a1*n)',
                   'time': [round(float(c), 4) for c in ce], 'time_form': 'sec = exp(c0 + c1*log(wall_path) + c2*log(need/P) + c3*boss + c4*floor/100)',
                   'files': files, 'points': len(pts), 'hit': hit}, open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
        print('→', OUT)


if __name__ == '__main__':
    main()
