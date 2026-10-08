"""UnityResources 의 pivots.json + 웹게임 FRONT_PARTS → 유니티 스프라이트 피벗 목록 (좌하단 기준 0~1)"""
import json, io, os, re
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, 'UnityResources', 'Rats')
OUT = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats', 'sprite_pivots.json')
HTML = os.path.join(ROOT, 'Proto_Game', 'rat-uprising.html')
items = []
def add(path, xy): items.append({'path': 'Assets/Art/Rats/' + path, 'x': round(xy[0], 4), 'y': round(xy[1], 4)})

rats = json.load(io.open(os.path.join(RES, 'Parts', 'pivots.json'), encoding='utf-8'))['species']
for sid, m in rats.items():
    for part, xy in m['pivotUnity'].items(): add(f'Parts/{sid}/{part}.png', xy)
for folder in ('Cats/Parts', 'Humans/Parts'):
    for sid, m in json.load(io.open(os.path.join(RES, *folder.split('/'), 'pivots.json'), encoding='utf-8')).items():
        if not isinstance(m, dict) or 'pivotUnity' not in m: continue
        for part, xy in m['pivotUnity'].items(): add(f'{folder}/{sid}/{part}.png', xy)
# 필살기 앞모습 파츠 (웹게임 FRONT_PARTS: 좌상단 기준 → 뒤집음)
src = io.open(HTML, encoding='utf-8').read()
fp = json.loads(re.search(r'const FRONT_PARTS = (\{.*?\});\n', src).group(1))
for pid, m in fp.items():
    if 'pivot' in m and os.path.exists(os.path.join(RES, 'FrontRig', pid + '.png')):
        add(f'FrontRig/{pid}.png', [m['pivot'][0], 1 - m['pivot'][1]])
json.dump({'items': items}, io.open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
print(len(items), 'pivots ->', OUT)

# ── 쥐 리그 메타 (몸통 부착점 = 몸통 이미지 좌상단 기준 0~1, 다리 배율) → Assets/Data/RatRigMeta.json ──
meta = []
for sid, m in rats.items():
    a = m['torsoAnchor']
    meta.append({'code_id': sid, 'neck': a['neck'], 'tail': a['tail'], 'shoulder': a['shoulder'], 'hip': a['hip'],
                 'leg_front': m.get('legScale', {}).get('front', 1), 'leg_back': m.get('legScale', {}).get('back', 1)})
OUT2 = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Data', 'RatRigMeta.json')
json.dump({'items': meta}, io.open(OUT2, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
print(len(meta), 'rigs ->', OUT2)

# ── 사람 리그 메타 (몸통 부착점 = 몸통 이미지 좌상단 기준 0~1) → Assets/Data/HumanRigMeta.json ──
hm = []
for hid, m in json.load(io.open(os.path.join(RES, 'Humans', 'Parts', 'pivots.json'), encoding='utf-8')).items():
    a = m['anchor']
    # front = 몸통이 정면 그림이면 pivots.json 에 "front": 1 (팔은 몸통 양옆, 다리 벌림, 목 가운데 — HumanRig.Build). 지금은 전부 옆모습
    hm.append({'code_id': hid, 'neck': a['neck'], 'shoulder': a['shoulder'], 'hip': a['hip'], 'front': int(m.get('front', 0)), 'head_scale': float(m.get('headScale', 1))})   # head_scale = 머리 크기 배율 (보스 머리가 몸통만큼 커서 0.85)
OUT3 = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Data', 'HumanRigMeta.json')
json.dump({'items': hm}, io.open(OUT3, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
print(len(hm), 'human rigs ->', OUT3)

# ── 고양이 리그 메타 (쥐와 같은 형식) → Assets/Data/CatRigMeta.json ──
cm = []
for cid, m in json.load(io.open(os.path.join(RES, 'Cats', 'Parts', 'pivots.json'), encoding='utf-8')).items():
    if not isinstance(m, dict) or 'anchor' not in m: continue
    a = m['anchor']
    cm.append({'code_id': cid, 'neck': a['neck'], 'tail': a['tail'], 'shoulder': a['shoulder'], 'hip': a['hip'],
               'leg_front': m.get('legScale', {}).get('front', 1), 'leg_back': m.get('legScale', {}).get('back', 1)})
OUT4 = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Data', 'CatRigMeta.json')
json.dump({'items': cm}, io.open(OUT4, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
print(len(cm), 'cat rigs ->', OUT4)
