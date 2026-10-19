"""스트링 테이블 영어 빈칸을 '숫자만 다른 기존 번역'으로 채움 (2026-10-19, 9~16훈장 스킬 이름 등 생성된 글).
예: 기존 '모든 쥐 공격력 +25' = 'All rats ATK +25' → 새 '모든 쥐 공격력 +40' = 'All rats ATK +40'.
틀이 없는 글은 EXTRA 표에서 찾고, 그래도 없으면 그대로 둠 (목록 출력).
사용: python Tools/fill_en_by_pattern.py"""
import os, re
import openpyxl

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATH = os.path.join(ROOT, 'Data_Table', '스트링 테이블.xlsx')
NUM = re.compile(r'\d+(?:\.\d+)?')
EXTRA = [   # (정규식, 영어 틀) — {0} {1} = 숫자 순서
    (r'^(\d+)훈장 트리$', 'Rank {0} Tree'),
    (r'^자동 승급 \((\d+)초마다 일괄 승급\)$', 'Auto Promote (every {0}s)'),
    (r'^승급 필요 수 -(\d+)$', 'Promote cost -{0} rats'),
    (r'^(\d+)% 확률로 3번 공격$', '{0}% chance to attack three times'),
    (r'^(\d+)% 확률로 4번 공격$', '{0}% chance to attack four times'),
]


def main():
    wb = openpyxl.load_workbook(PATH); ws = wb['String']
    K = [c.value for c in ws[2]]
    ki, ei, si = K.index('ko') + 1, K.index('en') + 1, K.index('status') + 1
    tmpl = {}
    for r in range(4, ws.max_row + 1):
        k, e = ws.cell(r, ki).value, ws.cell(r, ei).value
        if not isinstance(k, str) or not isinstance(e, str) or not e: continue
        kn, en = NUM.findall(k), NUM.findall(e)
        if not kn or sorted(kn) != sorted(en): continue
        # 영어 쪽 숫자가 한국어 몇 번째 숫자인지 (같은 값이 여러 번이면 순서대로)
        order, used = [], set()
        for x in en:
            i = next((j for j, y in enumerate(kn) if y == x and j not in used), None)
            if i is None: break
            used.add(i); order.append(i)
        else:
            et = NUM.sub('\0', e)
            tmpl.setdefault(NUM.sub('#', k), (et, order))
    done, miss = 0, []
    for r in range(4, ws.max_row + 1):
        k, e = ws.cell(r, ki).value, ws.cell(r, ei).value
        if not isinstance(k, str) or e: continue
        out = None
        for rx, t in EXTRA:
            m = re.match(rx, k)
            if m: out = t.format(*m.groups()); break
        if out is None:
            kn = NUM.findall(k); t = tmpl.get(NUM.sub('#', k))
            if t:
                et, order = t; parts = et.split('\0')
                out = parts[0] + ''.join(kn[order[i]] + parts[i + 1] for i in range(len(order)))
        if out: ws.cell(r, ei, out); ws.cell(r, si, '번역 완료'); done += 1
        else: miss.append(k)
    wb.save(PATH)
    print('채움', done, '· 남음', len(miss))
    for m in miss[:40]: print('  ', m)


if __name__ == '__main__':
    main()
