"""Data_Table 의 엑셀 → 유니티 Assets/Data/*.json
엑셀 형식: 1행 한글명 / 2행 키 / 3행 자료형(int·float·string·enum, '-' = 설명 칸) / 4행~ 데이터.
워크북 하나 → JSON 하나: { "<시트 이름>": [ {키: 값, ...}, ... ], ... }
사용: python Tools/xlsx2json.py   (엑셀 수정 후 한 번 실행)"""
import json, io, os, glob
import openpyxl

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'Data_Table')
DST = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Data')
# 엑셀 파일 이름 → JSON 이름 (C# 에서 쓰는 이름)
NAMES = {'쥐 캐릭터 테이블': 'RatTable', '쥐 등급 테이블': 'RatGradeTable', '쥐 성장 테이블': 'RatGrowthTable', '고양이 캐릭터 테이블': 'CatTable', '물건 테이블': 'ItemTable', '티어 테이블': 'TierTable', '사람 테이블': 'HumanTable', '공용 스킬 테이블': 'CommonSkillTable', '스테이지 테이블': 'StageTable'}


def conv(v, t):
    if t == 'int': return int(v or 0)
    if t == 'float': return float(v or 0)
    return '' if v is None else str(v)


os.makedirs(DST, exist_ok=True)
for path in sorted(glob.glob(os.path.join(SRC, '*.xlsx'))):
    base = os.path.splitext(os.path.basename(path))[0]
    if base.startswith('~$'): continue
    name = NAMES.get(base)
    if not name: print('건너뜀 (NAMES 에 이름 없음):', base); continue
    wb = openpyxl.load_workbook(path, data_only=True)
    out = {}
    for ws in wb:
        rows = list(ws.iter_rows(values_only=True))
        keys, types = rows[1], rows[2]
        cols = [(i, k.strip(), t) for i, (k, t) in enumerate(zip(keys, types)) if k and t and t != '-']
        if not cols: continue                     # 설명용 시트 (자료형이 전부 '-')
        out[ws.title] = [{k: conv(r[i], t) for i, k, t in cols} for r in rows[3:] if r and r[0] is not None]
    io.open(os.path.join(DST, name + '.json'), 'w', encoding='utf-8').write(json.dumps(out, ensure_ascii=False, indent=1))
    print(f'{base} -> {name}.json', {k: len(v) for k, v in out.items()})
