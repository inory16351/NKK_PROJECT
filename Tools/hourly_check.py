"""1시간 단위 진행 확인 (2026-10-26) — balance_sim 을 판마다 기록해 시간당 새 층 · 승급 시각을 tier_gates.HOUR_FLOORS 목표와 비교.
사용: python Tools/hourly_check.py   (생성 순서를 다 돌린 뒤)"""
import json, os, sys
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import balance_sim as B
import tier_gates as TG


def play(passive):
    power = json.load(open(B.POWER, encoding='utf-8'))
    calib = {}
    cp = os.path.join(B.ROOT, 'Tools', 'probe_results', 'time_calib.json')
    if os.path.exists(cp): calib = {(k if k == 'all' else int(k)): v for k, v in json.load(open(cp)).items()}
    p = B.Player(power, passive, calib); hist = []; promo = {}; runs = []
    while p.maxf <= B.LAST and p.t < 40 * 3600:
        t0, s = p.tier, p.start_floor(); top = p.run() - 1; p.spend(); hist.append((p.t, p.maxf - 1))
        runs.append((round(p.t / 3600, 2), s, top))
        if p.tier != t0: promo[p.tier] = round(p.t / 3600, 2)
    per = []; prev = 0
    for h in range(1, 41):
        best = max([m for t, m in hist if t <= h * 3600] or [0]); per.append(best - prev); prev = best
        if best >= 100: break
    return p, per, promo, runs


def main():
    print('목표 (클릭) 시간당 층:', TG.HOUR_FLOORS)
    for passive in (False, True):
        p, per, promo, runs = play(passive)
        print(('미클릭' if passive else '클릭'), f'{p.t / 3600:.2f}h · 판 {p.runs} · 시간당 층:', per)
        print('   승급:', promo)
        if '--runs' in sys.argv: print('   판 (끝 시각, 시작층, 도달):', runs)


if __name__ == '__main__':
    main()
