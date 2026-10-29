"""새 쥐 파츠 → 유니티 (2026-10-29): UnityResources/Rats/Parts/<id> 를 Assets/Art/Rats/Parts/<id> 로 복사하고,
sprite_pivots.json · RatRigMeta.json 에 그 종만 추가·덮어씀 (gen_sprite_pivots.py 는 지운 쥐를 되살려서 통째로 안 씀)
사용: python Tools/add_rats_to_unity.py kongpat patjwi butterfly   (종별 추가값은 EXTRA)"""
import io, json, os, shutil, sys
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, 'UnityResources', 'Rats')
ART = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats')
DATA = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Data')
EXTRA = {   # 리그 메타 추가값 (RatRigMetaRow): twin = 위에 거꾸로 붙는 쥐 · wing_anchor = 머리 그림 좌상단 기준 · wing_size = 머리 폭 배율 · head_scale = 머리 크기 고정
    'kongpat': {'twin': 'patjwi'},
    'butterfly': {'head_only': 1, 'head_scale': 1.0, 'wing_anchor': [0.62, 0.2], 'wing_size': 1.05, 'wing2_anchor': [0.6, 0.62], 'wing2_size': 0.65},
}
sp = json.load(io.open(os.path.join(RES, 'Parts', 'pivots.json'), encoding='utf-8'))['species']
pv_path = os.path.join(ART, 'sprite_pivots.json'); meta_path = os.path.join(DATA, 'RatRigMeta.json')
pv = json.load(io.open(pv_path, encoding='utf-8')); meta = json.load(io.open(meta_path, encoding='utf-8'))
for sid in sys.argv[1:]:
    src = os.path.join(RES, 'Parts', sid); dst = os.path.join(ART, 'Parts', sid); os.makedirs(dst, exist_ok=True)
    for f in os.listdir(src):
        if f.endswith('.png'): shutil.copy2(os.path.join(src, f), os.path.join(dst, f))
    m = sp[sid]
    pv['items'] = [i for i in pv['items'] if not i['path'].startswith(f'Assets/Art/Rats/Parts/{sid}/')]
    for part, xy in m['pivotUnity'].items(): pv['items'].append({'path': f'Assets/Art/Rats/Parts/{sid}/{part}.png', 'x': round(xy[0], 4), 'y': round(xy[1], 4)})
    a = m['torsoAnchor']
    row = {'code_id': sid, 'neck': a['neck'], 'tail': a['tail'], 'shoulder': a['shoulder'], 'hip': a['hip'],
           'leg_front': m['legScale']['front'], 'leg_back': m['legScale']['back']}
    row.update(EXTRA.get(sid, {}))
    meta['items'] = [i for i in meta['items'] if i['code_id'] != sid] + [row]
    print(sid, row)
json.dump(pv, io.open(pv_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
json.dump(meta, io.open(meta_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
