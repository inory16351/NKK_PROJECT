"""초필살기 레이어 컷신 씬 만들기 코드 생성 → 출력 .cs 를 Unity MCP execute_code 로 실행 (Game 씬 열린 상태, Play 밖).
틀 = 햄찌 v3 (사용자 2026-10-17): ② 큰 세로 파노라마 안 가상 카메라 (하늘 → 휙 → 캐릭터 착지 · 펑 · 빨려 드는 힘 · 클로즈업)
  ③ 위쪽 띠(파노라마가 흐름 + 액션 포즈) + **띠 아래 게임 화면으로 strike 가 내리꽂히고 impact** (다단 히트 시각마다)
  ④ 피니시 카메라 (whip_down · pull_back · swing) + strike 기둥 + 피니시 포즈 + 밈 소품·글
그림 = Assets/Art/Rats/SuperUlt/<code>/ (slice_superult.py 결과: pano_on[/pano_alt] · pose_intro/act/finish · face_close · fx_<역할> · prop_<역할>)
사용: python Tools/gen_superult_scene.py <출력.cs> <code ...>      (zapham = 손으로 만든 장면에 ③ 땅 번개만 더함)"""
import json, os, sys
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
ART = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats', 'SuperUlt')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gen_gacha_intro_scene import RATS as GI          # 쥐별 입자 (바람 · 색 · 크기 · 줄무늬 · 흔들림) · 컷2 효과음

# 쥐별 연출 차이: 캐릭터 쪽(L/R) · ③ 띠 배경 흐름(up/down/side) · ③ 등장 방향(L/R) · ④ 카메라(whip_down/pull_back/swing) · 기울기 부호 · 다단 효과음 · 월드 어둠 색
VAR = {
    'parkrat':     ('R', 'side', 'L', 'pull_back', 1, 'combo_word', (0.10, 0.05, 0.18, 0.5)),
    'emperor':     ('L', 'up', 'R', 'whip_down', -1, 'boss_slam', (0.15, 0.05, 0.02, 0.5)),
    'pharaoh':     ('R', 'down', 'L', 'swing', 1, 'laser', (0.25, 0.15, 0.02, 0.45)),
    'knight':      ('L', 'side', 'L', 'whip_down', -1, 'boss_swing', (0.03, 0.05, 0.15, 0.55)),
    'sultan':      ('R', 'up', 'R', 'pull_back', 1, 'cheese', (0.12, 0.03, 0.18, 0.5)),
    'alien':       ('L', 'down', 'L', 'whip_down', 1, 'laser', (0.02, 0.10, 0.04, 0.6)),
    'streamrat':   ('R', 'side', 'R', 'swing', -1, 'combo_word', (0.10, 0.02, 0.15, 0.5)),
    'ramjui':      ('L', 'up', 'L', 'whip_down', -1, 'wall_blast', (0.03, 0.08, 0.04, 0.6)),
    'jwerry':      ('R', 'side', 'L', 'pull_back', 1, 'smash_wood', (0.10, 0.02, 0.04, 0.55)),
    'starchef':    ('L', 'down', 'R', 'swing', 1, 'hit_wood', (0.12, 0.06, 0.02, 0.45)),
    'jjdetective': ('R', 'side', 'L', 'whip_down', -1, 'throw', (0.02, 0.04, 0.10, 0.6)),
    'fondue':      ('L', 'up', 'R', 'pull_back', 1, 'spill', (0.15, 0.10, 0.02, 0.45)),
    'bomber':      ('R', 'down', 'L', 'swing', -1, 'wall_blast', (0.15, 0.03, 0.02, 0.55)),
    'icequeen':    ('L', 'side', 'R', 'whip_down', 1, 'hit_glass', (0.02, 0.06, 0.14, 0.6)),
}
HITS = [2.0 + 0.2 * i for i in range(10)]       # 다단 히트 시각 (Super_Config: t_cutin 1.9 · t_act 3.9 · 10타)

PRE = r'''
var hud = GameObject.Find("HUD").transform;
var frame = hud.Find("SuperUlt/Cut/Frame");
var scenes = frame.Find("Scenes");
var bigCap = hud.Find("UltCaptionBig").GetComponent<TMPro.TextMeshProUGUI>();
string dir = "";
System.Func<string, Sprite> S = n => AssetDatabase.LoadAssetAtPath<Sprite>(dir + n + ".png");
System.Func<string, Transform, GameObject> Go = (n, p) => { var g = new GameObject(n, typeof(RectTransform)); g.layer = p.gameObject.layer; g.transform.SetParent(p, false); return g; };
System.Action<RectTransform> Stretch = rt => { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; };
System.Func<float, float, float, float, float, float, int, NKK.Ults.SuperUltLayer.Key> K = (t, x, y, sc, rot, a, e) => { var k = new NKK.Ults.SuperUltLayer.Key(); k.t = t; k.pos = new Vector2(x, y); k.scale = sc; k.rot = rot; k.alpha = a; k.ease = (NKK.Ults.SuperUltLayer.Ease)e; return k; };
System.Func<string, Transform, string, float, NKK.Ults.SuperUltLayer.Key[], NKK.Ults.SuperUltLayer> L = (n, p, spr, size, keys) => {
  var g = Go(n, p); var im = g.AddComponent<UnityEngine.UI.Image>(); im.raycastTarget = false; im.sprite = S(spr);
  if (im.sprite) ((RectTransform)g.transform).sizeDelta = im.sprite.rect.size * size; else Debug.LogWarning("[SuperUlt] 그림 없음 " + dir + spr);
  var l = g.AddComponent<NKK.Ults.SuperUltLayer>(); l.keys = keys; return l; };
System.Func<string, Transform, NKK.Ults.SuperUltLayer.Key[], NKK.Ults.SuperUltLayer> G = (n, p, keys) => {
  var g = Go(n, p); Stretch((RectTransform)g.transform); g.AddComponent<CanvasGroup>().blocksRaycasts = false;
  var l = g.AddComponent<NKK.Ults.SuperUltLayer>(); l.keys = keys; return l; };
System.Func<string, Transform, NKK.Ults.SuperUltLayer.Key[], NKK.Ults.SuperUltLayer> E = (n, p, keys) => { var g = Go(n, p); var l = g.AddComponent<NKK.Ults.SuperUltLayer>(); l.keys = keys; return l; };
System.Func<string, Transform, string, float, Color, TMPro.TextMeshProUGUI> T = (n, p, s, size, c) => {
  var t = UnityEngine.Object.Instantiate(bigCap, p, false); t.name = n; t.text = s; t.fontSize = size; t.color = c; t.alpha = 1; t.raycastTarget = false;
  t.alignment = TMPro.TextAlignmentOptions.Center; t.enableWordWrapping = false; t.transform.localScale = Vector3.one;
  var lt = t.GetComponent("LocText"); if (lt) UnityEngine.Object.DestroyImmediate(lt);
  var rt = (RectTransform)t.transform; rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero; return t; };
System.Action<string> Import = d => {
  foreach (var g0 in AssetDatabase.FindAssets("t:Texture2D", new[] { d.TrimEnd('/') }))
  {
    var p = AssetDatabase.GUIDToAssetPath(g0); var ti = (TextureImporter)AssetImporter.GetAtPath(p);
    int want = p.Contains("pano_") ? 4096 : 2048;
    if (ti.textureType != TextureImporterType.Sprite || ti.mipmapEnabled || ti.maxTextureSize != want) { ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.mipmapEnabled = false; ti.maxTextureSize = want; ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
  } };
var mgr = UnityEngine.Object.FindFirstObjectByType<NKK.Ults.SuperUltManager>(FindObjectsInactive.Include);
var styleRoot = hud.Find("SuperUlt/Styles");
System.Func<string, string, NKK.Ults.SuperUltStyle> Style = (name, code) => {
  var o = styleRoot.Find(name); if (o) UnityEngine.Object.DestroyImmediate(o.gameObject);
  var st = Go(name, styleRoot).AddComponent<NKK.Ults.SuperUltStyle>(); st.code = code;
  var list = new System.Collections.Generic.List<NKK.Ults.SuperUltStyle>();
  foreach (var s0 in mgr.styles) if (s0 && s0.code != code) list.Add(s0);
  list.Add(st); mgr.styles = list.ToArray(); EditorUtility.SetDirty(mgr); return st; };
'''
EASE = {'Out': 0, 'Linear': 1, 'In': 2, 'InOut': 3, 'Back': 4, 'Snap': 5}


def f(x): return f'{x:.2f}f'


def cs(v):
    """표 값 → C# (정수 · 실수 · (x, y) = Vector2 · 글 = 그림 S() · 글 목록 = 그림 배열)"""
    if isinstance(v, bool): return 'true' if v else 'false'
    if isinstance(v, int): return str(v)
    if isinstance(v, float): return f(v)
    if isinstance(v, tuple): return f'new Vector2({f(v[0])}, {f(v[1])})'
    if isinstance(v, str): return f'S("{v}")'
    return 'new[] { ' + ', '.join(f'S("{n}")' for n in v) + ' }'


def keys(ks):
    """ks = [(t, x, y, sc, rot, a, ease), ...]"""
    return 'new[] { ' + ', '.join(f'K({f(t)}, {f(x)}, {f(y)}, {f(s)}, {f(r)}, {f(a)}, {EASE[e]})' for t, x, y, s, r, a, e in ks) + ' }'


def size(code, name):
    p = os.path.join(ART, code, name + '.png')
    return Image.open(p).size if os.path.exists(p) else (500, 500)


class W:
    def __init__(self): self.o = []
    def __call__(self, s): self.o.append(s)


def rat(code):
    """쥐별 연출 (superult_story.STORIES) + 쥐별 모습(SuperUltStyle)"""
    from superult_story import Story, STORIES, NO_FINISH, WORLD, IMAGE_FX, NO_TEXT
    st = Story(code, ART, ROOT)
    gi = GI[code]
    name, wind, cols, dot, streak, sway, sfx_cut = gi[0], gi[3], gi[4], gi[5], gi[6], gi[7], gi[9]
    sfx_hit, dim = VAR[code][5], VAR[code][6]
    st(f'// ── {code} ({name}) ──')
    st(f'dir = "Assets/Art/Rats/SuperUlt/{code}/"; Import(dir);')
    st(f'{{ var o = scenes.Find("{name}"); if (o) UnityEngine.Object.DestroyImmediate(o.gameObject); }}')
    st(f'var root_{code} = Go("{name}", scenes); Stretch((RectTransform)root_{code}.transform);')
    pts = STORIES[code](st, f'root_{code}.transform')          # 피해 숫자 자리를 돌려주는 쥐만 hitNumbers
    st(f'root_{code}.SetActive(false);')
    pal = ', '.join(f'new Color({f(c[0])}, {f(c[1])}, {f(c[2])}, {f(c[3])})' for c in cols)
    hit = cols[0]
    st(f'{{ var sty = Style("{name}", "{code}"); sty.scene = (RectTransform)root_{code}.transform; sty.titleOut = 1.6f;')
    st(f'  sty.dim = new Color({f(dim[0])}, {f(dim[1])}, {f(dim[2])}, {f(dim[3])}); sty.hitColor = new Color({f(hit[0])}, {f(hit[1])}, {f(hit[2])}, 1f); sty.hitSize = 1.6f; sty.hitLighten = 0.4f; sty.lightenColor = new Color({f(hit[0])}, {f(hit[1])}, {f(hit[2])}, 1f);')
    st(f'  sty.snow = new NKK.Lobby.Blizzard(); sty.snow.wind = new Vector2({f(wind[0])}, {f(wind[1])}); sty.snow.color = new Color({f(cols[0][0])}, {f(cols[0][1])}, {f(cols[0][2])}, {f(cols[0][3])});')
    st(f'  sty.snow.palette = new Color[] {{ {pal} }}; sty.snow.dotSize = new Vector2({f(dot[0])}, {f(dot[1])}); sty.snow.streak = {f(streak)}; sty.snow.sway = {f(sway)}; sty.snow.flakeChance = 0;')
    if code in NO_FINISH: st(f'  sty.noFinishCut = true; sty.actEnd = {f(NO_FINISH[code])}; sty.finishFlash = 0.45f; sty.hitShake = 16; sty.titleOut = 0.95f;')      # ④ 피니시 컷 없음 · 화면 전체 컷신
    if code in NO_TEXT: st('  sty.hideText = true;')      # 컷신 글 없음 (제목 띠 · 자막 · 히트 수)
    if code in WORLD: st('  sty.worldPhase = true; ' + ' '.join(f'sty.{k} = {cs(v)};' for k, v in WORLD[code].items()))
    if code in IMAGE_FX: st('  sty.imageFx = true; ' + ' '.join(f'sty.{k} = {cs(v)};' for k, v in IMAGE_FX[code].items()))
    if pts: st(f'  sty.hitNumbers = true; sty.hitPoints = new[] {{ {", ".join(f"new Vector2({f(x)}, {f(y)})" for x, y in pts)} }};')
    st(f'  sty.fxCut = 40; sty.fxAct = 40; sty.fxFinish = 20; sty.sfxEnter = "jgl_ult_cutin"; sty.sfxCut = "{sfx_cut}"; sty.sfxHit = "{sfx_hit}"; sty.sfxFinish = "wall_blast"; EditorUtility.SetDirty(sty); }}')
    return '\n'.join(st.o)


def ground(w, parent, strike, impact, ring, sc, isc):
    """③ 띠 아래 게임 화면: 다단 히트마다 strike 가 띠에서 내리꽂히고 바닥에서 impact · ring"""
    xs = [-620, 380, -150, 700, -820, 250, -400, 560, -50, 820]
    for i, (x, t0) in enumerate(zip(xs, HITS)):
        t0 -= 0.06
        w(f'{{ var s = L("Ground{i}", {parent}, "{strike}", {f(sc)}, {keys([(t0-0.01,x,250,1,0,0,"Out"),(t0,x,250,1,0,1,"Snap"),(t0+0.08,x,-180,1.05,0,1,"In"),(t0+0.25,x,-180,1,0,0,"Linear")])}); s.flicker = 0.35f; s.jitter = 4; }}')
        w(f'{{ var s = L("Impact{i}", {parent}, "{impact}", {f(isc)}, {keys([(t0+0.07,x,-400,0,0,0,"Out"),(t0+0.08,x,-400,0.4,0,1,"Snap"),(t0+0.2,x,-400,1.1,0,1,"Back"),(t0+0.45,x,-400,1.3,0,0,"Linear")])}); s.spin = 60; }}')
        w(f'L("GRing{i}", {parent}, "{ring}", 0.6f, {keys([(t0+0.08,x,-420,0.3,0,0.9,"Out"),(t0+0.4,x,-420,1.5,0,0,"Out")])});')


def zapham_ground():
    """햄찌(손으로 만든 장면)에 ③ 땅 번개만 더함"""
    w = W()
    w('dir = "Assets/Art/Rats/SuperUlt/zapham/";')
    w('var zact = scenes.Find("ZapHam/Act");')
    w('var zl = new System.Collections.Generic.List<Transform>(); foreach (Transform c in zact) if (c.name.StartsWith("Ground") || c.name.StartsWith("Impact") || c.name.StartsWith("GRing")) zl.Add(c); foreach (var c in zl) UnityEngine.Object.DestroyImmediate(c.gameObject);')
    ground(w, 'zact', 'fx_bolt_big', 'fx_starburst', 'fx_ring_shock', 1.25, 0.6)
    return '\n'.join(w.o)


def main():
    out, codes = sys.argv[1], sys.argv[2:]
    body = [PRE]
    for c in codes:
        body.append(zapham_ground() if c == 'zapham' else rat(c))
    body.append('UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);')
    body.append('UnityEditor.SceneManagement.EditorSceneManager.SaveScene(hud.gameObject.scene);')
    body.append(f'return "ok {" ".join(codes)}";')
    import re
    code = '\n'.join(body)
    code = re.sub(r'(\d)\.00f', r'\1f', code); code = re.sub(r'(\.\d)0f', r'\1f', code)      # MCP 실행 길이 제한 5만 자 → 숫자 줄이기
    code = re.sub(r'(?<=[,(]) (?=[-\d])', '', code)
    if len(codes) == 1: code = re.sub(r'\b(\w+?\d+)_' + codes[0] + r'\b', r'\1_z', code)     # 한 마리씩 실행하면 변수 이름 꼬리 줄임
    open(out, 'w', encoding='utf-8').write(code)
    print(out, codes)


if __name__ == '__main__':
    main()
