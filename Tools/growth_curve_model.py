"""성장 요구량 조정안 모델 (단계 공간: 1단계 = x1.3). 층 배율 q^(f-1) 은 찍찍·적정 양쪽에 곱해져 비율에서 지워짐."""
import sys, math, json
sys.stdout.reconfigure(encoding="utf-8")
L = math.log(1.3)
TIME_C, TIME_LO, TIME_HI = 0.8, 1.5, 0.5
LIMIT = 120; FLOOR_OH = 6; RUN_OH = 75; RAMP = 25; PASSIVE = 1.35
def tmul(r): return TIME_C * r ** (-TIME_LO if r < 1 else -TIME_HI)

# ── 결정값 ──
H = {2: 4, 3: 9, 4: 14, 5: 19, 6: 24, 7: 29, 8: 34, 9: 39, 10: 44, 11: 49, 12: 54, 13: 59, 14: 66, 15: 73, 16: 79}   # 훈장 t 해금 허들 (t-1 트리 다 찍고 넘음)
FINAL = [90, 94, 97, 100]                                    # 최종 연전 허들 (16훈장 트리 + 증강 완성)
D = {1: 2, 2: 2, 3: 2, 4: 3, 5: 3, 6: 3, 7: 3, 8: 4, 9: 4, 10: 4, 11: 4, 12: 5, 13: 5, 14: 5, 15: 6, 16: 6}  # 훈장 t 총 단계
JF = 0.45                                                    # 승급 즉시 받는 몫 (나머지 = 트리)
INCOME = 1.4
PRE_CAP = 0.7
TA, TB, TC, HUR_T, LATE_ADD, MID_ADD = 30, 45, 70, 1.2, 30, 15
FINAL_STEPS = 4                                              # 90~100 증강 완성 단계 (16훈장 트리와 별도)
BLOCK = {t: (1 if t <= 5 else 2 if t <= 13 else 3) for t in range(2, 17)}   # 허들에서 막히는 판 (목표)
BLOCK_FINAL = 2
BOSS = set(range(5, 90, 5)) | {90, 94, 97, 100}

def start_cap(rec): return max(1, rec - (3 if rec <= 70 else 5))

# 층 목표 초 (r=1 기준 x0.8 이 실제 시간): 초반 짧게 → 후반 길게
def target(f):
    if f <= 15: base = TA + (TB - TA) * (f - 1) / 14
    elif f <= 70: base = TB + (TC - TB) * (f - 16) / 54
    elif f <= 89: base = TC + 10
    else: base = TC + 20
    hurdle = f in H.values() or f in FINAL
    return base * (HUR_T if hurdle else 1.1 if f in BOSS else 1)
def limit(f): return LIMIT + (LATE_ADD if f >= 71 else MID_ADD * (f - 16) / 54 if f > 16 else 0) + (20 if f in BOSS else 0)

# ── 누적 단계: P_full(t) = 1..t 훈장 전부 ──
Pfull = {}; c = 0
for t in range(1, 17): c += D[t]; Pfull[t] = c
def fail_margin(f, slow=PASSIVE):   # 이 만큼 모자라도 제한시간 안에 깸 (단계, 미클릭 기준)
    return -math.log((TIME_C * target(f) * slow / limit(f)) ** (1 / TIME_LO)) / L

# 층 요구 단계 u_f
u = {}
seg = [(1, 0)] + sorted((h, t) for t, h in H.items())
for t in range(1, 17):
    lo = 1 if t == 1 else H[t] + 1                 # 훈장 t 로 처음 보는 층
    hi = H[t + 1] if t < 16 else 89
    p0 = (Pfull[t - 1] if t > 1 else 0) + (D[t] * JF if t > 1 else 0)   # 승급 직후 힘
    for f in range(lo, hi + 1):
        if t < 16 and f == hi:                      # 허들: 트리 다 찍어야 겨우
            u[f] = Pfull[t] + fail_margin(f) - 0.05
        else:                                       # 일반: 승급 직후보다 1.5단계 낮게 시작 → 트리 90% 지점까지
            x = (f - lo) / max(1, hi - lo)
            u[f] = (p0 - 1.5) + x * (Pfull[t] - 0.1 * D[t] * (1 - JF) - (p0 - 1.5)) + (0.3 if f in BOSS else 0)
# 90~100: 16훈장 트리 다 + 증강 완성 단계를 연전 허들 4곳에 나눔
base = Pfull[16]
for i, f0 in enumerate(range(90, 101)):
    pass
steps_at = {90: 1, 94: 2, 97: 3, 100: 4}
prev_h = 89; prev_need = Pfull[16]
for h in FINAL:
    need = Pfull[16] + FINAL_STEPS * steps_at[h] / 4
    for f in range(prev_h + 1, h):
        u[f] = prev_need - 0.8 + 0.3 * (f - prev_h)
    u[h] = need + fail_margin(h) - 0.05
    prev_h, prev_need = h, need

def sim(passive=False):
    slow = PASSIVE if passive else 1
    arrive = None
    t_sec = 0; rec = 0; tier = 1; tree = 0.0; fin = 0.0; runs = 0; log = []; promo = {}; blocked = {}
    while rec < 100 and runs < 2000:
        runs += 1
        p = (Pfull[tier - 1] if tier > 1 else 0) + (D[tier] * JF if tier > 1 else 0) + tree * D[tier] * (1 - JF if tier > 1 else 1) + fin
        s = start_cap(rec) if rec else 1
        run_t = RUN_OH + RAMP; f = s; cleared = 0
        while f <= 100:
            r = 1.3 ** (p - u[f]); ft = target(f) * tmul(r) * slow
            lim = limit(f)
            if ft > lim:
                run_t += lim; blocked[f] = blocked.get(f, 0) + 1; break
            run_t += ft + FLOOR_OH; cleared += 1; f += 1
        rec = max(rec, f - 1); t_sec += run_t
        log.append((runs, round(t_sec / 3600, 2), s, f - 1, tier, round(p, 2)))
        # 로비: 승급 → 트리
        if tier < 16 and rec >= H[tier + 1] and tree >= 0.999:
            tier += 1; tree = 0.0; arrive = None; promo[tier] = round(t_sec / 3600, 2)
        else:
            # 트리 진행: 허들까지 (훈장 구간 판 수) + 허들에서 BLOCK 판 → 판당 몫
            if tier < 16:
                hb = H[tier + 1]
                if f == hb:                                         # 허들에서 막힘 → 남은 트리를 (B-1) 판에 나눠 채움
                    if arrive is None: arrive = tree
                    tree = min(1.0, tree + (1 - arrive) / max(1, BLOCK[tier + 1] - 1) + 1e-9)   # 도착 판 실패 = 1판째
                else:                                               # 허들 전: 깬 층 수만큼 (최대 PRE_CAP)
                    n_floors = hb - (H[tier] if tier > 1 else 0)
                    tree = min(PRE_CAP, tree + cleared / n_floors * INCOME)
            else:
                if tree < 1: tree = min(1.0, tree + max(0.34, cleared / 10 * INCOME * 0.5))
                elif f in FINAL: fin = min(FINAL_STEPS, fin + FINAL_STEPS / 4 / max(1, BLOCK_FINAL - 1) + 1e-9)
    return t_sec / 3600, runs, promo, blocked, log

def report():
    for passive in (False, True):
        h, n, promo, blocked, log = sim(passive)
        print(("미클릭" if passive else "클릭"), f"100층 {h:.2f}h · {n}판")
        print("  승급 시각:", promo)
        print("  막힌 층(판):", {k: v for k, v in sorted(blocked.items()) if v >= 1})
        # 구간별 층당 분
        first = {}
        for (_, th, s, top, tier, p) in log:
            for f in range(1, top + 1): first.setdefault(f, th)
        for a, b in [(1, 15), (16, 70), (71, 80), (81, 89), (90, 100)]:
            t0 = first.get(a - 1, 0) if a > 1 else 0
            if b not in first: print(f'  {a}~{b}층: 못 감'); continue
            print(f"  {a}~{b}층: {first[b]*60 - t0*60:.0f}분 · {((first[b]-t0)*60)/(b-a+1):.1f}분/층 · 누적 {first[b]:.2f}h")
    json.dump({"u": u, "target": {f: target(f) for f in range(1, 101)}, "Pfull": Pfull}, open("model_out.json", "w"))

if __name__ == '__main__': report()
