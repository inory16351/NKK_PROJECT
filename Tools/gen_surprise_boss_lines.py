"""깜짝 보스 (94 그렘린 고양이 · 97 우주복 쥐, 2026-10-22) 대사 → 보스 테이블 Situation_Type · Boss_Line (96401~).
Join = 97층 동료 획득 배너 · Disguise = 변장 중 혼잣말 · Reveal = 변신 외침 (Boss.Surprise.cs)
고치기 전 원본은 Data_Table/_backup_20261022/ 에 한 번 복사. 그다음 python Tools/gen_string_table.py → python Tools/xlsx2json.py
사용: python Tools/gen_surprise_boss_lines.py"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_expansion_data as G

G.BACKUP = os.path.join(G.DT, '_backup_20261022')

SITUATIONS = [
    ('Join', '깜짝 보스 (97층): 동료로 들어올 때 배너'),
    ('Disguise', '깜짝 보스: 변장 중 혼잣말 (94 보통 고양이 · 97 동료 쥐)'),
    ('Reveal', '깜짝 보스: 변신할 때 외침 (쥐가 계단에 닿음)'),
]

LINES = {
    95038: {      # 그렘린 고양이 (알 → 보통 고양이)
        'Disguise': [('냐옹~', 'Meow~'), ('골골골…', 'Purr purr…'), ('부비부비~', 'Nuzzle nuzzle~'), ('간식 있어냥?', 'Got snacks, meow?')],
        'Reveal': [('찌지직…!!', 'Kzzzt…!!'), ('자정이 지났다냥…', "It's past midnight, meow…")],
    },
    95039: {      # 우주복 쥐 (동료 → 임포스터)
        'Join': [('신화 우주복 쥐 획득!', 'Mythic Spacesuit Rat acquired!')],
        'Disguise': [('같이 가자 찍!', "Let's go together, squeak!"), ('나 착한 쥐야 찍', "I'm a good rat, squeak"), ('임무 하는 중~', 'Just doing tasks~'), ('통풍구? 몰라 찍', 'Vents? Never heard of them')],
        'Reveal': [('사실 나… 임포스터였어!', 'Actually… I was the impostor!')],
    },
}


def main():
    wb, p = G.book('보스 테이블')
    st = G.Sheet(wb['Situation_Type'])
    for k, desc in SITUATIONS: st.upsert('situation', k, dict(desc=desc))
    bl = G.Sheet(wb['Boss_Line'])
    lid = 96400
    for bid, sit in LINES.items():
        for s, lines in sit.items():
            for ln in lines:
                lid += 1
                bl.upsert('line_id', lid, dict(boss_id=bid, situation=s, text=G.ko(ln)))
    wb.save(p)
    print('깜짝 보스 대사', lid - 96400)
    if '--en' in sys.argv: G.fill_english()


if __name__ == '__main__':
    main()
