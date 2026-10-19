"""진행 시뮬레이터 (2026-10-19) — 처음부터 100층 클리어까지 플레이 시간을 어림잡음.
사용자 목표: 클릭 적극 10시간 / 거의 안 함 15시간. 허들(누적): 16층 2h · 30층 4h · 31층(증강) 4h · 50층 6.5h · 70층 9h · 100층 12.5h.

모델
 · 판: 시작 층(스테이지 스킵 노드·건너뛰기 상한) → 층마다 벽 시간 = 목표 초 × 시간 배율(r) × 측정 보정, r = 무리 전투력 ÷ 적정.
   시간 배율 h(r) = TIME_C × r^-TIME_LO (r<1) · TIME_C × r^-TIME_HI (r≥1) — round9 실측(r 0.66~2.35 에서 목표 초의 0.3~1.4배)에 맞춤.
   (예전 벽 공식 그대로의 0.5/r^2.5 + 0.5/r^1.5 는 19층 r 0.70 실측 90초를 280초로 봐서 너무 비관적)
   제한시간(180 + 공용 스킬 시간) 넘으면 그 판 끝 (게임 오버).
 · 무리 전투력 = 훈장별 측정값(BalanceProbe, Tools/probe_results/power_by_tier.json) 을 지금 훈장 트리를 찍은 비율로 보간 × 증강 배율.
 · 수입 = 층 클리어 연구자료(스테이지 테이블) · 치즈 = K(층) × 치즈 배율 × 방 수 (gen_economy 와 같은 식).
 · 판이 끝나면 지금 훈장 트리에서 싼 노드부터 사고, 조건이 되면 훈장 승급. 16층부터는 수입의 LATE_SHARE 만 훈장·트리에 (나머지는 뽑기·상점).
 · 클릭: active = 측정 그대로, passive = 시간 × PASSIVE_SLOW.
사용: python Tools/balance_sim.py [--passive] [--verbose]"""
import json, math, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
D = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Data')
POWER = os.path.join(ROOT, 'Tools', 'probe_results', 'power_by_tier.json')

def J(n): return json.load(open(os.path.join(D, n), encoding='utf-8'))

STAGE = {r['floor']: r for r in J('StageTable.json')['Stage']}
TIER = {r['tier']: r for r in J('TierTable.json')['Tier']}
_RT = J('RatTable.json')['Character']; _GR = {r['grade']: r for r in J('RatGradeTable.json')['Grade']}
GRADES = ['Common', 'Rare', 'Epic', 'Unique', 'Legendary', 'Mythic']
SPECIES = {g: sorted((r['unlock_rank'], r['code_id']) for r in _RT if r['grade'] == g) for g in GRADES}
SKILL = J('CommonSkillTable.json')['Common_Skill']
BOSS_FLOORS = {b['floor'] for b in J('BossTable.json')['Boss'] if b['floor'] > 0}
LAST = max(STAGE)

LATE_SHARE = 0.5          # 16층~ 훈장·트리에 쓰는 몫
PASSIVE_SLOW = 1.35       # 클릭 거의 안 함: 같은 상태에서 층 시간 배율 (측정으로 보정)
FLOOR_OVERHEAD = 6        # 층 사이 연출 (탈취·페이드) 초
RUN_OVERHEAD = 75         # 판마다 로비 (업그레이드·출발) 초
RAMP_SEC = 25             # 판 첫 층: 쥐가 불어나는 동안 손해 보는 초
CHEESE_K = (2.4, 1.05)    # gen_economy fit_k 결과 근사 (치즈 = a × b^(층-1) × 치즈 배율 × 방 수) — 아래 load_k 가 있으면 덮어씀
AUG_PER_SLOT = 0.08       # 증강 1칸당 무리 전투력 + (평균, 31층~) — gen_stage_table.AUG_PER_SLOT 과 같게
TIME_C, TIME_LO, TIME_HI = 0.8, 1.5, 0.5
BASE_TIME = 120           # RunTimer.baseTime (2026-10-19: 180 → 120)
BIRTH_PER_POP = 1.0       # 층 하나 클리어하는 동안 태어나는 쥐 ≈ 최대 마릿수 × 이 값 (round9: 1훈장 6층 판 승급 28번 ≈ 출생 300) — 조각 = 태어난 종 +1
K_FLOOR_CAP = 30          # 치즈 K 곡선(1~20층 측정 맞춤)은 이 층까지만 늘림 (그 뒤 외삽은 폭주)


def load_k():
    try:
        sys.path.insert(0, os.path.join(ROOT, 'Tools'))
        import gen_economy
        return gen_economy.fit_k()
    except Exception:
        return CHEESE_K


def time_mul(r):
    return TIME_C * r ** (-TIME_LO if r < 1 else -TIME_HI)


class Player:
    def __init__(self, power, passive=False, calib=None):
        self.tier = 1; self.nodes = set(); self.res = 0.0; self.chz = 0.0; self.maxf = 1
        self.t = 0.0; self.runs = 0; self.power = power; self.passive = passive; self.calib = calib or {}
        self.log = []; self.mile = {}
        self.ka, self.kb = load_k()
        self.by_tier = {}
        self.shard = {}                       # 종 → 조각 (출생 +1)
        for s in SKILL:
            if s['link_1'] == 0 and s['link_2'] == 0: continue
            self.by_tier.setdefault(s['tier'], []).append(s)

    # ── 상태에서 나오는 값 ──
    def sk(self, eff, i=1):
        return sum(s['value_0%d' % i] for t in range(1, self.tier + 1) for s in self.by_tier.get(t, []) if s['skill_id'] in self.nodes and s['effect_type'] == eff)
    def tree_frac(self):
        l = self.by_tier.get(self.tier, [])
        return sum(1 for s in l if s['skill_id'] in self.nodes) / max(1, len(l))
    def pow(self, floor, run_floors):
        p0 = self.power[str(self.tier)]['start']; p1 = self.power[str(self.tier)]['end']
        p = p0 * (p1 / p0) ** self.tree_frac()
        if self.maxf > 31 and floor >= 31:      # 증강 (31층~ 층마다 하나, 슬롯 5 + 노드)
            slots = 5 + self.sk('Aug_Slot')
            p *= 1 + AUG_PER_SLOT * min(slots, max(0, run_floors))
        return p
    def births(self, n_floors):               # 판에서 깬 층 수만큼 출생 → 종마다 조각 (기댓값)
        if n_floors <= 0: return
        B = n_floors * self.power[str(self.tier)].get('pop', 100) * BIRTH_PER_POP
        k = TIER[self.tier]['birth_grade_k']
        w = {g: _GR[g]['birth_weight'] * k ** i for i, g in enumerate(GRADES) if any(u <= self.tier for u, _ in SPECIES[g])}
        tot = sum(w.values())
        for g, wg in w.items():
            sp = [c for u, c in SPECIES[g] if u <= self.tier]
            for c in sp: self.shard[c] = self.shard.get(c, 0) + B * wg / tot / len(sp)
    def shard_level_sum(self):
        n = 0
        for g in GRADES:
            kk = _GR[g]['shard_k']
            for _, c in SPECIES[g]:
                s, L = self.shard.get(c, 0), 0
                while L < 68 and s >= math.ceil((L + 1) * kk): s -= math.ceil((L + 1) * kk); L += 1
                n += L
        return n
    def dex(self): return sum(1 for v in self.shard.values() if v >= 1)
    def start_floor(self):
        cap = STAGE[max(1, self.maxf - 1)]['skip_cap'] if self.maxf > 1 else 1
        return max(1, min(1 + int(self.sk('Stage_Skip')), cap, self.maxf, LAST))
    def limit(self, f): return BASE_TIME + self.sk('Time_Add') + (self.sk('Boss_Dmg_Pct', 2) if f in BOSS_FLOORS else 0)
    def cheese_of(self, f): st = STAGE[f]; return self.ka * self.kb ** (min(f, K_FLOOR_CAP) - 1) * st['cheese'] * st['rooms']

    # ── 한 판 ──
    def run(self):
        f = self.start_floor(); dt = RUN_OVERHEAD; n = 0; res = chz = 0
        while f <= LAST:
            st = STAGE[f]
            r = self.pow(f, n) / st['pow_need']
            sec = st['target_sec'] * time_mul(r) * self.calib.get(f, self.calib.get('all', 1)) * (PASSIVE_SLOW if self.passive else 1)
            if n == 0: sec += RAMP_SEC
            lim = self.limit(f)
            if sec > lim:
                dt += lim; chz += self.cheese_of(f) * 0.6
                break
            dt += sec + FLOOR_OVERHEAD; res += st['research']; chz += self.cheese_of(f); n += 1
            if f + 1 > self.maxf: self.maxf = f + 1
            f += 1
            if f > LAST: break
        self.births(n)
        self.t += dt; self.runs += 1
        share = LATE_SHARE if self.maxf > 16 else 1
        self.res += res * share; self.chz += chz * share
        return f

    def spend(self):
        while True:
            # 훈장 승급
            nt = TIER.get(self.tier + 1)
            if nt and self.ready(nt):
                self.res -= nt['research_cost']; self.tier += 1; continue
            # 트리 (싼 것부터, 열린 칸)
            opts = [s for s in self.by_tier.get(self.tier, []) if s['skill_id'] not in self.nodes and (s['link_1'] in self.nodes or s['link_1'] % 100 == 0 and s['link_1'] // 100 * 100 == s['link_1'])]
            opts = [s for s in opts if s['cost_cheese'] <= self.chz and s['cost_research'] <= self.res]
            if not opts: break
            s = min(opts, key=lambda s: s['cost_cheese'] / max(1, self.chz) + s['cost_research'] / max(1, self.res))
            self.chz -= s['cost_cheese']; self.res -= s['cost_research']; self.nodes.add(s['skill_id'])

    def ready(self, nt):
        if self.res < nt['research_cost']: return False
        for i in range(1, 5):
            ty, v = nt.get('cond%d_type' % i), nt.get('cond%d_value' % i, 0)
            if ty == 'Max_Floor' and self.maxf < v: return False
            if ty == 'Boss_Clear' and self.maxf - 1 < v: return False
            if ty == 'Shard_Level_Sum' and self.shard_level_sum() < v: return False
            if ty == 'Dex_Count' and self.dex() < v: return False
            if ty == 'Skill_Tree_Complete' and any(s['skill_id'] not in self.nodes for t in range(1, self.tier + 1) for s in self.by_tier.get(t, [])): return False
        return True

    def play(self, hours_cap=60, verbose=False):
        marks = [16, 31, 41, 51, 71, 101]
        while self.maxf <= LAST and self.t < hours_cap * 3600:
            before = self.maxf
            reached = self.run(); self.spend()
            for m in marks:
                if m not in self.mile and self.maxf >= m: self.mile[m] = self.t
            if verbose: print(f'{self.t / 3600:5.2f}h 판 {self.runs:3d} 훈장 {self.tier:2d} 트리 {self.tree_frac():.0%} 최고 {self.maxf - 1:3d}층 (이번 {reached - 1}층) 연구 {self.res:.3g} 치즈 {self.chz:.3g}')
        return self


def main():
    power = json.load(open(POWER, encoding='utf-8'))
    calib = {}
    cp = os.path.join(ROOT, 'Tools', 'probe_results', 'time_calib.json')
    if os.path.exists(cp): calib = {(k if k == 'all' else int(k)): v for k, v in json.load(open(cp)).items()}
    for passive in ([True] if '--passive' in sys.argv else [False, True]):
        p = Player(power, passive, calib).play(verbose='--verbose' in sys.argv)
        print(('거의 안 함' if passive else '클릭 적극'), f'총 {p.t / 3600:.1f}시간 · 판 {p.runs} · 훈장 {p.tier} · 최고 {p.maxf - 1}층')
        for m, t in sorted(p.mile.items()): print(f'   {m - 1}층 클리어: {t / 3600:.2f}h')


if __name__ == '__main__':
    main()
