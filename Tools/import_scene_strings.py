"""씬 글 → 스트링 테이블 (80xxxxx). Title · Lobby · Game 씬 YAML 에서 한글이 든 글(TMP m_text + 인스펙터 문자열)을 찾아 String 시트에 넣음.
- 같은 한글은 키 하나 (출처에 쓰인 곳을 모두 적음). 이미 있는 한글이면 출처만 갱신, 씬에서 사라지면 상태 '안 쓰임'.
- 테스트 패널(TestPanel)·테스트 전용 값·오브젝트 이름은 뺌.
사용: python Tools/import_scene_strings.py   (유니티에서 씬을 저장한 뒤)"""
import os, re, sys, yaml
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets').replace(chr(92), '/') + '/'
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_string_table as gst
H = re.compile('[\uac00-\ud7a3]')


class L(yaml.SafeLoader):
    pass


L.add_multi_constructor('', lambda loader, suffix, node: loader.construct_mapping(node, deep=True) if isinstance(node, yaml.MappingNode) else None)


def load(path):
    t = open(path, encoding='utf-8').read()
    objs = {}
    for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- |\Z)', t, flags=re.M | re.S):
        cls, fid, body = m.group(1), m.group(2), m.group(3)
        try:
            d = yaml.load(body, Loader=L)
        except Exception as e:
            continue
        if not d: continue
        (typ, val), = d.items()
        objs[fid] = (typ, val)
    return objs


def walk(v, path, out):
    if isinstance(v, str):
        if H.search(v): out.append((path, v))
    elif isinstance(v, dict):
        for k, x in v.items(): walk(x, f'{path}.{k}' if path else k, out)
    elif isinstance(v, list):
        for i, x in enumerate(v): walk(x, f'{path}[{i}]', out)


def scene(name):
    objs = load(A + 'Scenes/' + name)
    go_name, parent, tr_go = {}, {}, {}
    for fid, (typ, v) in objs.items():
        if typ == 'GameObject': go_name[fid] = v.get('m_Name', '')
    for fid, (typ, v) in objs.items():
        if typ in ('Transform', 'RectTransform') and 'm_GameObject' in v:
            g = str(v['m_GameObject']['fileID']); tr_go[fid] = g
    for fid, (typ, v) in objs.items():
        if typ in ('Transform', 'RectTransform') and fid in tr_go:
            p = str(v.get('m_Father', {}).get('fileID', 0))
            parent[tr_go[fid]] = tr_go.get(p)

    def path(g):
        n = []
        while g:
            n.append(str(go_name.get(g, '?'))); g = parent.get(g)
        return '/'.join(reversed(n))
    res = []
    for fid, (typ, v) in objs.items():
        if typ == 'PrefabInstance':
            for m in (v.get('m_Modification') or {}).get('m_Modifications') or []:
                val = m.get('value')
                if isinstance(val, str) and H.search(val):
                    res.append(dict(scene=name, path='(prefab instance)', cls='PrefabInstance', field=m.get('propertyPath'), text=val, fid=fid))
            continue
        if typ != 'MonoBehaviour' or 'm_GameObject' not in v: continue
        cls = (v.get('m_EditorClassIdentifier') or '').split('::')[-1]
        out = []
        for k, x in v.items():
            if k.startswith('m_') and k != 'm_text': continue
            walk(x, k, out)
        g = str(v['m_GameObject']['fileID'])
        for f, s in out:
            res.append(dict(scene=name, path=path(g), cls=cls, field=f, text=s, fid=fid))
    return res


PRE = 80
SKIP_PATH = ('TestPanel',)
SKIP_FIELD = ('testDownSub',)


def main():
    found = {}
    for sc in ('Title.unity', 'Lobby.unity', 'Game.unity'):
        for r in scene(sc):
            if any(x in r['path'] for x in SKIP_PATH) or r['field'] in SKIP_FIELD: continue
            where = f"{sc.replace('.unity', '')} · {r['path']}" + ('' if r['field'] == 'm_text' else f" · {r['cls'].split('.')[-1]}.{r['field']}")
            found.setdefault(r['text'], []).append(where)
    strings, _ = gst.load_strings()
    mine = {s['ko']: k for k, s in strings.items() if k // gst.STEP == PRE}
    nxt = max([k for k in strings if k // gst.STEP == PRE] + [PRE * gst.STEP])
    added = 0
    for ko, ws in found.items():
        k = mine.get(ko)
        if k is None:
            nxt += 1; k = nxt; added += 1
            strings[k] = dict(string_key=k, ko=ko, en='', source='', status='번역 필요', note='')
        src = ' | '.join(ws[:4]) + (f' 외 {len(ws) - 4}곳' if len(ws) > 4 else '')
        strings[k]['source'] = '씬 · ' + src
        if strings[k]['status'] == '안 쓰임': strings[k]['status'] = '' if strings[k]['en'] else '번역 필요'
    for ko, k in mine.items():
        if ko not in found: strings[k]['status'] = '안 쓰임'
    gst.write(strings)
    print(f'씬 글 {len(found)}개 (새로 {added})')


if __name__ == '__main__':
    main()
