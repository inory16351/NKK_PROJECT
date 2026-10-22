"""보스 개편 (2026-10-22) 그림 확인용 보스 행 → 보스 테이블 Boss 시트 (95024~).
행동(평타·스킬·발악)은 사용자가 나중에 전달 → 지금은 floor 0 (층에 안 나옴, 테스트 패널 '◀ 보스 ▶' 로만 부름).
리깅 움직임 확인용 (사용자 2026-10-22): 평타 = 리그별 기본 공격, 쿨타임 스킬·발악 1·2 = 같은 층 지금 보스 것 그대로. 소환물(좋았쓰 고양이·굴착기)은 스킬 없음.
고치기 전 원본은 Data_Table/_backup_20261022/ 에 한 번 복사. 그다음 python Tools/xlsx2json.py
사용: python Tools/gen_boss_rework_data.py"""
import os, shutil, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_expansion_data as G

G.BACKUP = os.path.join(G.DT, '_backup_20261022')

# (boss_id, 이름, code_id, rig, 개편 층, scale, radius_mul)  — rig: Human / Cat / Part (BossPartArt)
ROWS = [
    (95024, '팝 캣', 'boss_popcat', 'Cat', 25, 3.4, 3.4),
    (95025, '괴렘', 'boss_goerem', 'Cat', 30, 4.0, 4.0),
    (95026, '공룡탈 알바', 'boss_dino2', 'Human', 35, 2.9, 2.9),
    (95027, '버티컬 마우스', 'boss_vmouse', 'Human', 40, 2.8, 2.8),
    (95028, '랩터 사육사', 'boss_trainer', 'Human', 45, 2.8, 2.8),
    (95029, '노조 고양이', 'boss_union2', 'Cat', 50, 3.4, 3.4),
    (95030, '거미 고양이', 'boss_spider', 'Part', 55, 1, 3.2),
    (95031, '굴착기 기사', 'boss_operator', 'Human', 60, 2.9, 2.9),
    (95032, '상어 고양이', 'boss_sharkcat', 'Part', 65, 1, 3.2),
    (95033, '머라이언 고양이', 'boss_fishcat', 'Part', 70, 1, 3.0),
    (95034, '심해 잠수부', 'boss_deepdiver', 'Human', 75, 2.9, 2.9),
    (95035, '차력거인', 'boss_cartgiant', 'Part', 80, 1, 3.6),
    (95036, '항공모함 고양이', 'boss_carrier', 'Part', 85, 1, 4.5),
    (95037, '총통 고양이', 'boss_dictator', 'Cat', 90, 3.5, 3.5),
    (95038, '그렘린 고양이', 'boss_gremlin', 'Cat', 94, 3.5, 3.5),
    (95039, '우주복 쥐', 'boss_crewmate', 'Part', 97, 1, 3.2),
    (95040, '좋았쓰 고양이 (소환물)', 'boss_joassu', 'Human', 50, 1.6, 1.6),
    (95041, '굴착기 (탈것)', 'boss_excavator', 'Part', 60, 1, 3.2),
]
ATK = {'Human': ('Swing', 'Stomp'), 'Cat': ('Pounce', 'Hairball'), 'Part': ('Stomp', 'Pounce')}


def main():
    wb, p = G.book('보스 테이블')
    b = G.Sheet(wb['Boss'])
    now = {}      # 지금 층 보스 (floor → 스킬·발악)
    for r in range(4, b.ws.max_row + 1):
        row = {k: b.ws.cell(r, i + 1).value for i, k in enumerate(b.keys)}
        if isinstance(row.get('boss_id'), int) and row['boss_id'] < 95024 and row.get('floor'): now[row['floor']] = row
    tmpl = b.find('boss_id', 95004)
    base = {k: b.ws.cell(tmpl, i + 1).value for i, k in enumerate(b.keys)}
    for bid, name, code, rig, floor, scale, rad in ROWS:
        d = dict(base)
        a1, a2 = ATK[rig]
        d.update(boss_name=name, code_id=code, rig=rig, floor=0, atk_type=a1, atk2_type=a2, atk2_chance=0.4, scale=scale, radius_mul=rad,
                 skill_type='', special1_type='', special2_type='', special_chance=0, skill_cd=8, skill_first=4)
        old = now.get(floor)
        if old and '소환물' not in name and '탈것' not in name:
            d.update({k: old[k] for k in ('skill_type', 'skill_cd', 'skill_first', 'special1_type', 'special2_type', 'special_chance')})
        d['-'] = f'보스 개편 {floor}층 그림 확인용 (2026-10-22, 행동 미정 → floor 0, 테스트 패널로만)'
        d.pop('boss_id', None); d.pop('boss_name_sk', None)
        b.upsert('boss_id', bid, d)
    wb.save(p)
    print('보스 개편 확인용', len(ROWS))


if __name__ == '__main__':
    main()
