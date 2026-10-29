"""새 쥐 둘 (2026-10-29): 콩쥐팥쥐 kongpat(10088) · 나비쥐 butterfly(10089) 를 쥐 캐릭터 테이블에 추가 (여러 번 돌려도 같은 행을 덮어씀)
순서: 이 스크립트 → gen_string_table.py → (영어 채움) → xlsx2json.py"""
import copy, os, openpyxl
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
P = os.path.join(ROOT, 'Data_Table', '쥐 캐릭터 테이블.xlsx')
wb = openpyxl.load_workbook(P)

def upsert(ws, key, row):
    hdr = [c.value for c in ws[2]]
    kc = hdr.index(key)
    target = None
    for r in ws.iter_rows(min_row=4):
        if r[kc].value == row[key]: target = r[0].row
    if target is None:
        target = ws.max_row + 1
        for c in range(1, ws.max_column + 1):
            src = ws.cell(ws.max_row, c); dst = ws.cell(target, c)
            dst._style = copy.copy(src._style)
    for i, h in enumerate(hdr):
        if h in row: ws.cell(target, i + 1).value = row[h]

CH = [
    dict(character_id=10088, character_name='콩쥐팥쥐', code_id='kongpat', grade='Rare', body_type='Mouse', atk=52, passive_skill=20088, action_skill=21088, ultimate=0, unlock_rank=8,
         character_desc='등을 맞대고 붙어 버린 한복 쌍둥이. 위에 붙은 한 마리는 늘 거꾸로 버둥거리고, 뛰어오르면 공중제비로 위아래가 바뀐다.', asset_folder='Parts/kongpat'),
    dict(character_id=10089, character_name='나비쥐', code_id='butterfly', grade='Rare', body_type='Mouse', atk=46, passive_skill=20089, action_skill=21089, ultimate=0, unlock_rank=8,
         character_desc='커다란 귀를 날개처럼 팔랑이며 땅에서 살짝 떠서 다닌다. 날갯짓 한 번이 나비 효과가 된다.', asset_folder='Parts/butterfly'),
]
SK = [
    dict(skill_id=20088, skill_name='둘이서 두 번', skill_category='Passive', cond1_type='Hit_Item', cond1_value_01=0.3, cond1_value_02=0, cond2_type='None', cond2_value_01=0, cond2_value_02=0,
         effect_type='Double_Bite', value_01=1, value_02=0.08, value_03=0, value_04=0, value_05=0, value_06=0, duration=0, cool_time=0, skill_icon='👯', skill_asset='RatSkillIcons/rs_20088',
         skill_explain='한 번 더 갉을 확률 30% (둘이서 한입씩)'),
    dict(skill_id=21088, skill_name='공중제비 쌍둥이', skill_category='Action', cond1_type='Hit_Item', cond1_value_01=0.035, cond1_value_02=0, cond2_type='None', cond2_value_01=0, cond2_value_02=0,
         effect_type='Jump_Slam', value_01=3, value_02=2.6, value_03=99.5, value_04=0, value_05=0, value_06=0, duration=1.55, cool_time=6, skill_icon='🤸', skill_asset='RatSkillIcons/rs_21088',
         skill_explain='물건에 부딪힐 때 4% 확률로 → 공중제비 쌍둥이: 3번 뛰어올라 공중제비로 위아래를 바꾸며 쾅! 내려찍기 (반경 100)'),
    dict(skill_id=20089, skill_name='나비 효과', skill_category='Passive', cond1_type='Hit_Item', cond1_value_01=1, cond1_value_02=0, cond2_type='None', cond2_value_01=0, cond2_value_02=0,
         effect_type='Crit_Boost', value_01=0.1, value_02=3.7, value_03=0, value_04=0, value_05=0, value_06=0, duration=0, cool_time=0, skill_icon='🦋', skill_asset='RatSkillIcons/rs_20089',
         skill_explain='크리티컬 확률 +10%, 크리티컬 ×3.7'),
    dict(skill_id=21089, skill_name='비늘가루 소나기', skill_category='Action', cond1_type='Interval', cond1_value_01=10, cond1_value_02=0, cond2_type='None', cond2_value_01=0, cond2_value_02=0,
         effect_type='Savory_Smell', value_01=0.3, value_02=300, value_03=5, value_04=0, value_05=0, value_06=0, duration=1.2, cool_time=6, skill_icon='✨', skill_asset='RatSkillIcons/rs_21089',
         skill_explain='10초마다 → 비늘가루 소나기: 반경 300 안 물건이 5초 동안 받는 피해 +30%'),
]
for r in CH: upsert(wb['Character'], 'character_id', r)
for r in SK: upsert(wb['Skill'], 'skill_id', r)
wb.save(P)
print('ok')
