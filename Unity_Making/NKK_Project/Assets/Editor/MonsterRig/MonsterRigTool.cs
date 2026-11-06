using System;
using System.Collections.Generic;
using System.IO;
using NKK.Data;
using NKK.Hazards;
using NKK.Humans;
using NKK.Rats;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// 몬스터 리깅 툴 (2026-11-06 사용자: 보스 몬스터 그림이 안 맞는 부분을 고치는 툴). 메뉴 NKK/몬스터 리깅 툴.
// 게임과 같은 리그 코드(BossPartRig · RatRig · HumanRig)로 조립해 보여 주고, 끌거나 값을 바꿔 고친 뒤 '게임에 반영' (MonsterRigSave).
// 파츠 보스 = 파츠마다 붙는 자리(at) · 관절(pivot) · 각도 · 크기 · 순서 · 움직임 / 고양이 · 사람 = 몸통 부착점(목 · 어깨 · 엉덩이 · 꼬리) · 그림 관절 · 다리 · 머리 크기.
// 마우스: 왼쪽 끌기 = 파츠 옮기기 · Shift+끌기 = 관절은 두고 그림만 옮기기 · Ctrl+끌기 = 돌리기(파츠 보스) · 휠 = 확대 · 오른쪽/가운데 끌기 = 화면 이동
public partial class MonsterRigTool : EditorWindow
{
    [MenuItem("NKK/몬스터 리깅 툴")]
    static void Open() => GetWindow<MonsterRigTool>("몬스터 리깅");

    // ── 목록 ──
    enum Kind { Part, Cat, Human }
    class Mon { public Kind kind; public string code, name; public bool boss; public float scale = 1; public int floor; public BossRow row; }
    readonly List<Mon> mons = new();
    string search = "";
    bool bossOnly = true;
    Vector2 listScroll, inspScroll;
    Mon cur;

    // ── 고치는 중인 값 (Undo 되도록 ScriptableObject) ──
    MonsterRigWork work;
    string savedJson;           // 마지막으로 불러온 / 반영한 값 (바뀜 표시 · 되돌리기)
    string status = "";

    // ── 미리보기 ──
    PreviewRenderUtility pru;
    GameObject go, ghost;
    readonly Dictionary<SpriteRenderer, string> partOf = new();      // 렌더러 → 파츠 이름 (파츠 보스 = 관절 이름, 고양이 · 사람 = head · torso …)
    readonly Dictionary<Sprite, (Sprite src, string slot)> tempSprites = new();
    Material unlit;
    Vector2 camPos; float camSize = 3; bool fitNext = true;
    enum Pose { Idle, Walk, Attack }
    Pose pose;
    float walkPhase; bool play; double lastTime;
    float openK;
    bool showJoints = true, showGhost = true;
    string sel;                 // 고른 파츠 이름
    GameObject catPrefab, humanPrefab;

    // 끌기
    enum Drag { None, Move, Image, Rotate, Pan }
    Drag drag; Vector2 dragStartMouse; Vector3 lastWorld; float lastAngle;

    // 고양이 · 사람 파츠 → 몸통 부착점 · 관절을 고칠 수 있는 그림 칸
    static readonly string[] CatSlots = { "head", "head2", "tail", "front", "back" };
    static readonly string[] HumanSlots = { "head", "scared", "angry", "arm", "arm2", "leg" };

    void OnEnable()
    {
        work = CreateInstance<MonsterRigWork>(); work.hideFlags = HideFlags.HideAndDontSave;
        pru = new PreviewRenderUtility();
        var cam = pru.camera;
        cam.orthographic = true; cam.nearClipPlane = 0.01f; cam.farClipPlane = 100;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.36f, 0.42f, 0.36f);
        cam.transform.position = new Vector3(0, 0, -20);
        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
        unlit = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
        catPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cat.prefab");
        humanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Human.prefab");
        InitMotion();
        Undo.undoRedoPerformed += OnUndo;
        EditorApplication.update += Tick;
        LoadList();
        if (cur != null) Select(cur);
    }

    void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndo;
        EditorApplication.update -= Tick;
        ClearPreview();
        pru?.Cleanup(); pru = null;
        if (unlit) DestroyImmediate(unlit);
        if (work) DestroyImmediate(work);
        if (mwork) DestroyImmediate(mwork);
    }

    void OnUndo() { Rebuild(); Repaint(); }

    void Tick()
    {
        if (MotionTick()) return;
        if (!play) return;
        double t = EditorApplication.timeSinceStartup;
        if (t - lastTime < 1 / 30.0) return;
        walkPhase += (float)(t - lastTime) * 7; lastTime = t;
        Rebuild(); Repaint();
    }

    // ── 몬스터 목록: 보스 표 + 고양이 · 사람 그림 모음 ──
    [Serializable] class BossWrap { public List<BossRow> Boss; }
    void LoadList()
    {
        mons.Clear();
        var bossCodes = new HashSet<string>();
        var path = "Assets/Data/BossTable.json";
        if (File.Exists(path))
            foreach (var r in JsonUtility.FromJson<BossWrap>(File.ReadAllText(path)).Boss)
            {
                var k = r.rig == "Part" ? Kind.Part : r.rig == "Cat" ? Kind.Cat : Kind.Human;
                if (!bossCodes.Add(k + r.code_id)) continue;
                mons.Add(new Mon { kind = k, code = r.code_id, name = r.boss_name, boss = true, scale = r.scale > 0 ? r.scale : 1, floor = r.floor, row = r });
            }
        var part = AssetDatabase.LoadAssetAtPath<BossPartArt>(MonsterRigSave.PartAsset);
        if (part) foreach (var e in part.entries) if (bossCodes.Add(Kind.Part + e.codeId)) mons.Add(new Mon { kind = Kind.Part, code = e.codeId, name = "(보스 표에 없음)", boss = true });
        var cats = AssetDatabase.LoadAssetAtPath<RatArtLibrary>(MonsterRigSave.CatAsset);
        if (cats) foreach (var e in cats.entries) if (bossCodes.Add(Kind.Cat + e.codeId)) mons.Add(new Mon { kind = Kind.Cat, code = e.codeId, name = "고양이" });
        var hum = AssetDatabase.LoadAssetAtPath<HumanArtLibrary>(MonsterRigSave.HumanAsset);
        if (hum) foreach (var e in hum.entries) if (bossCodes.Add(Kind.Human + e.codeId)) mons.Add(new Mon { kind = Kind.Human, code = e.codeId, name = "사람" });
        mons.Sort((a, b) => a.boss != b.boss ? (a.boss ? -1 : 1) : a.boss ? (a.floor == 0 ? 999 : a.floor).CompareTo(b.floor == 0 ? 999 : b.floor) : string.CompareOrdinal(a.code, b.code));
        if (cur != null) cur = mons.Find(m => m.kind == cur.kind && m.code == cur.code);
    }

    bool RigDirty => cur != null && savedJson != null && EditorJsonUtility.ToJson(work) != savedJson;
    bool Dirty => RigDirty || MotionDirty;

    void Select(Mon m)
    {
        if (Dirty && !EditorUtility.DisplayDialog("몬스터 리깅", $"{cur.code} 에 반영하지 않은 수정이 있어요. 버리고 넘어갈까요?", "버리기", "취소")) return;
        cur = m; sel = null; status = "";
        Undo.ClearUndo(work);
        work.kind = m.kind.ToString(); work.code = m.code; work.pivots.Clear();
        work.part = new BossPartArt.Entry(); work.cat = new RatArtLibrary.Entry(); work.human = new HumanArtLibrary.Entry();
        switch (m.kind)
        {
            case Kind.Part:
                var pa = AssetDatabase.LoadAssetAtPath<BossPartArt>(MonsterRigSave.PartAsset).Get(m.code);
                if (pa != null) work.part = MonsterRigSave.Clone(pa);
                break;
            case Kind.Cat:
                var ca = AssetDatabase.LoadAssetAtPath<RatArtLibrary>(MonsterRigSave.CatAsset).Get(m.code);
                if (ca != null) { work.cat = MonsterRigSave.Clone(ca); foreach (var s in CatSlots) AddPivot(s, CatSprite(work.cat, s)); }
                break;
            case Kind.Human:
                var ha = AssetDatabase.LoadAssetAtPath<HumanArtLibrary>(MonsterRigSave.HumanAsset).Get(m.code);
                if (ha != null) { work.human = MonsterRigSave.Clone(ha); foreach (var s in HumanSlots) AddPivot(s, HumanSprite(work.human, s)); }
                break;
        }
        savedJson = EditorJsonUtility.ToJson(work);
        fitNext = true;
        if (motionMode && m.boss && m.row != null) EnterMotion(); else { motionMode = false; Rebuild(); }
    }

    void AddPivot(string slot, Sprite s) { if (s) work.pivots.Add(new MonsterRigWork.Pivot { part = slot, tl = new Vector2(s.pivot.x / s.rect.width, 1 - s.pivot.y / s.rect.height) }); }

    static Sprite CatSprite(RatArtLibrary.Entry e, string slot) => slot switch { "head" => e.head, "head2" => e.head2, "tail" => e.tail, "front" => e.front, "back" => e.back, "torso" => e.torso, _ => null };
    static Sprite HumanSprite(HumanArtLibrary.Entry e, string slot) => slot switch { "head" => e.head, "scared" => e.scared, "angry" => e.angry, "arm" => e.arm, "arm2" => e.arm2, "leg" => e.leg, "torso" => e.torso, _ => null };

    // ── 미리보기 조립 ──
    void ClearPreview()
    {
        if (go) DestroyImmediate(go);
        if (ghost) DestroyImmediate(ghost);
        foreach (var t in tempSprites.Keys) if (t) DestroyImmediate(t);
        tempSprites.Clear(); partOf.Clear();
    }

    void Rebuild()
    {
        ClearPreview();
        if (cur == null || pru == null) return;
        try
        {
            go = Make(work, partOf);
            if (showGhost && motionMode && curMove != null && mwork.motion.keys.Count > 0) ghost = Make(work, null, true);      // 모션: 코드 기본 모션을 겹쳐 봄
            else if (showGhost && RigDirty)
            {
                var saved = CreateInstance<MonsterRigWork>();
                EditorJsonUtility.FromJsonOverwrite(savedJson, saved);
                ghost = Make(saved, null);
                DestroyImmediate(saved);
                if (ghost)
                {
                    foreach (var r in ghost.GetComponentsInChildren<SpriteRenderer>()) { r.color = new Color(1, 0.55f, 0.55f, 0.35f); if (r.name == "Shadow") r.enabled = false; }
                    var g = ghost.GetComponent<SortingGroup>(); if (g) g.sortingOrder = -10;
                    var mg = go ? go.GetComponent<SortingGroup>() : null; if (mg) mg.sortingOrder = 0;
                }
            }
            if (ghost && motionMode)
            {
                foreach (var r in ghost.GetComponentsInChildren<SpriteRenderer>()) { r.color = new Color(0.55f, 0.75f, 1f, 0.35f); if (r.name == "Shadow") r.enabled = false; }
                var g = ghost.GetComponent<SortingGroup>(); if (g) g.sortingOrder = -10;
            }
        }
        catch (Exception ex) { status = "조립 실패: " + ex.Message; Debug.LogException(ex); }
        foreach (var o in new[] { go, ghost })
        {
            if (!o) continue;
            foreach (var r in o.GetComponentsInChildren<SpriteRenderer>(true)) r.sharedMaterial = unlit;
            pru.AddSingleGO(o);
        }
        SortingGroup.UpdateAllSortingGroups();      // 편집 모드에선 다음 갱신까지 묶음 순서가 안 먹어서 겹친 모습이 위로 올라옴
        if (fitNext && go) { Fit(); fitNext = false; }
    }

    GameObject Make(MonsterRigWork w, Dictionary<SpriteRenderer, string> map, bool codeMotion = false)
    {
        bool moving = pose == Pose.Walk, alt = pose == Pose.Attack;
        bool mot = motionMode && curMove != null;
        if (mot) moving = false;
        switch (cur.kind)
        {
            case Kind.Part:
            {
                if (w.part.parts.Count == 0 || !w.part.parts[0].sprite) return null;
                var o = EditorUtility.CreateGameObjectWithHideFlags("__RigPreview", HideFlags.HideAndDontSave, typeof(SortingGroup), typeof(BossPartRig));
                pru.AddSingleGO(o);      // 미리보기 장면으로 먼저 옮김 (열린 씬을 안 건드리게)
                var pr = o.GetComponent<BossPartRig>();
                pr.Build(w.part, 1); pr.still = true; pr.open = openK;
                if (mot) alt = MotionPart(pr, codeMotion);
                pr.Apply(moving ? walkPhase : 0, moving, alt, 1, -1, 0, 1, 0);
                if (mot) Lift(o);
                if (map != null) foreach (var r in o.GetComponentsInChildren<SpriteRenderer>(true)) map[r] = r.transform.parent.name;
                return o;
            }
            case Kind.Cat:
            {
                if (!catPrefab || !w.cat.torso && !w.cat.single) return null;
                var o = Inst(catPrefab, out RatRig rr);
                var e = MonsterRigSave.Clone(w.cat);
                e.head = Temp(e.head, "head", w); e.head2 = Temp(e.head2, "head2", w); e.tail = Temp(e.tail, "tail", w); e.front = Temp(e.front, "front", w); e.back = Temp(e.back, "back", w);
                float hs = cur.boss ? -1 : cur.code == "chonk" ? 0.72f : 1;
                rr.Build(e, 115 * cur.scale, hs, false);
                var p = new RatRig.Pose { tail = 0.2f, sx = 1, sy = 1 };
                if (moving) { float sn = Mathf.Sin(walkPhase); p.front = sn * 0.5f; p.farBack = sn * 0.45f; p.farFront = -sn * 0.5f; p.back = -sn * 0.45f; }
                if (alt) { p.front = 2.1f; p.farFront = 0.5f; p.tilt = -0.3f; p.head = -0.2f; p.tail = 1.1f; rr.headReplace = e.head2; }
                if (mot) p = MotionCat(rr, e, codeMotion);
                rr.Apply(p, 1, -1, 1, 0);
                if (mot) Lift(o);
                if (map != null)
                {
                    map[rr.head] = "head"; map[rr.torso] = "torso"; map[rr.tail] = "tail";
                    map[rr.front] = map[rr.farFront] = "front"; map[rr.back] = map[rr.farBack] = "back";
                }
                return o;
            }
            default:
            {
                if (!humanPrefab || !w.human.torso) return null;
                var o = Inst(humanPrefab, out HumanRig hr);
                var e = MonsterRigSave.Clone(w.human);
                e.head = Temp(e.head, "head", w); e.scared = Temp(e.scared, "scared", w); e.angry = Temp(e.angry, "angry", w);
                e.arm = Temp(e.arm, "arm", w); e.arm2 = Temp(e.arm2, "arm2", w); e.leg = Temp(e.leg, "leg", w);
                hr.height = 150 * cur.scale;
                hr.Build(e);
                var p = new HumanRig.Pose { sx = 1, sy = 1, armN = 0.25f, armF = -0.2f };
                if (moving) { float sn = Mathf.Sin(walkPhase); p.legN = sn * 0.45f; p.legF = -p.legN; p.armN = -sn * 0.35f; p.armF = -p.armN; }
                if (alt) { p.angry = true; p.armN = 1.2f; p.armF = -1.2f; }
                if (mot) p = MotionHuman(codeMotion);
                hr.Apply(p, 1, -1, 0, 1, 0, 1);
                if (mot) Lift(o);
                if (map != null)
                {
                    map[hr.head] = "head"; map[hr.torso] = "torso"; map[hr.armNear] = "arm"; map[hr.armFar] = e.arm2 ? "arm2" : "arm";
                    map[hr.legNear] = map[hr.legFar] = "leg";
                }
                return o;
            }
        }
    }

    // 프리팹을 복제해 리그 오브젝트만 남김 (게임 오브젝트 스크립트는 편집 모드라 안 돎)
    GameObject Inst<T>(GameObject prefab, out T rig) where T : Component
    {
        var o = pru.InstantiatePrefabInScene(prefab);
        rig = o.GetComponentInChildren<T>(true);
        var g = rig.gameObject;
        if (g != o) { g.transform.SetParent(null, false); DestroyImmediate(o); }
        g.SetActive(true);
        g.hideFlags = HideFlags.HideAndDontSave;
        if (!g.GetComponent<SortingGroup>()) g.AddComponent<SortingGroup>();
        return g;
    }

    // 관절을 바꾼 임시 그림 (같은 텍스처 · 같은 칸, 피벗만 고친 값) — '게임에 반영' 전까지는 .meta 를 안 건드림
    Sprite Temp(Sprite s, string slot, MonsterRigWork w)
    {
        if (!s) return null;
        var pv = w.pivots.Find(x => x.part == slot);
        var tl = pv != null ? pv.tl : new Vector2(s.pivot.x / s.rect.width, 1 - s.pivot.y / s.rect.height);
        // 원본 텍스처 (스프라이트 아틀라스에 묶여 있으면 s.texture 가 아틀라스라 칸이 어긋남)
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(s)) ?? s.texture;
        var rect = tex == s.texture ? s.rect : new Rect(0, 0, tex.width, tex.height);
        var t = Sprite.Create(tex, rect, new Vector2(tl.x, 1 - tl.y), s.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        t.name = s.name; t.hideFlags = HideFlags.HideAndDontSave;
        tempSprites[t] = (s, slot);
        return t;
    }

    void Fit()
    {
        if (!go) return;
        var b = BoundsOf(go);
        camPos = b.center;
        float aspect = Mathf.Max(0.3f, lastRect.width / Mathf.Max(1, lastRect.height));
        camSize = Mathf.Max(b.extents.y, b.extents.x / aspect) * 1.2f + 0.1f;
    }

    static Bounds BoundsOf(GameObject o)
    {
        Bounds b = default; bool first = true;
        foreach (var r in o.GetComponentsInChildren<SpriteRenderer>()) { if (!r.enabled || !r.sprite) continue; if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
        return b;
    }

    // ── 화면 ──
    Rect lastRect = new(0, 0, 600, 600);

    void OnGUI()
    {
        if (work == null || pru == null) { OnEnable(); }
        EditorGUILayout.BeginHorizontal();
        DrawList();
        EditorGUILayout.BeginVertical();
        DrawToolbar();
        var r = GUILayoutUtility.GetRect(200, 10000, 200, 10000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type == EventType.Repaint) lastRect = r;
        DrawPreview(r);
        EditorGUILayout.LabelField(Hint(), EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        DrawInspector();
        EditorGUILayout.EndHorizontal();
    }

    string Hint() => cur == null ? "왼쪽 목록에서 몬스터를 고르세요"
        : motionMode ? "모션: ▶ 재생 · 시간 막대 · 몸을 끌면 지금 시간 키의 관절이 돌아감 (키가 없으면 저절로 생김) · 휠 = 확대 · 오른쪽 끌기 = 화면 이동"
        : cur.kind == Kind.Part ? "끌기 = 파츠 옮기기 · Shift+끌기 = 관절은 두고 그림만 · Ctrl+끌기 = 돌리기 · 휠 = 확대 · 오른쪽 끌기 = 화면 이동 · F = 화면 맞춤"
        : "끌기 = 몸통에 붙는 자리 옮기기 (목 · 어깨 · 엉덩이 · 꼬리, 발이 바닥 기준이라 몸통이 대신 움직일 수 있음) · Shift+끌기 = 그림 관절(그림만 옮기기) · 휠 = 확대 · F = 화면 맞춤";

    void DrawList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(230));
        EditorGUILayout.BeginHorizontal();
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("↻", GUILayout.Width(24))) LoadList();
        EditorGUILayout.EndHorizontal();
        bossOnly = EditorGUILayout.ToggleLeft("보스만", bossOnly);
        listScroll = EditorGUILayout.BeginScrollView(listScroll);
        Kind? head = null; bool? bossHead = null;
        foreach (var m in mons)
        {
            if (bossOnly && !m.boss) continue;
            if (search.Length > 0 && m.code.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 && (m.name ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (bossHead != m.boss || (!m.boss && head != m.kind))
            {
                bossHead = m.boss; head = m.kind;
                EditorGUILayout.LabelField(m.boss ? "보스" : m.kind == Kind.Cat ? "고양이" : m.kind == Kind.Human ? "사람" : "파츠", EditorStyles.boldLabel);
            }
            string kind = m.kind == Kind.Part ? "파츠" : m.kind == Kind.Cat ? "고양이" : "사람";
            string label = m.boss ? $"{(m.floor > 0 ? m.floor + "층 " : "")}{m.name}  ·  {kind}\n<size=9>{m.code}</size>" : m.code;
            var st = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = false };
            if (cur == m) st.normal.textColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button(label, st)) Select(m);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginChangeCheck();
        if (cur != null && cur.boss && cur.row != null)
        {
            bool m = GUILayout.Toolbar(motionMode ? 1 : 0, new[] { "리그", "공격 · 필살기 모션" }, EditorStyles.toolbarButton, GUILayout.Width(170)) == 1;
            if (m != motionMode) { motionMode = m; if (m) EnterMotion(); fitNext = true; }
        }
        else motionMode = false;
        if (motionMode) { MotionToolbar(); if (EditorGUI.EndChangeCheck()) Rebuild(); GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal(); return; }
        pose = (Pose)GUILayout.Toolbar((int)pose, new[] { "대기", "걷기", "공격" }, EditorStyles.toolbarButton, GUILayout.Width(170));
        play = GUILayout.Toggle(play, "▶ 걷기 재생", EditorStyles.toolbarButton, GUILayout.Width(80));
        if (play && pose != Pose.Walk) pose = Pose.Walk;
        if (play) lastTime = lastTime == 0 ? EditorApplication.timeSinceStartup : lastTime; else lastTime = 0;
        if (pose == Pose.Walk && !play) { GUILayout.Label("위상", GUILayout.Width(28)); walkPhase = GUILayout.HorizontalSlider(walkPhase % (Mathf.PI * 2), 0, Mathf.PI * 2, GUILayout.Width(80)); }
        if (cur?.kind == Kind.Part && work.part.parts.Exists(p => p.show != BossPartArt.Show.Always)) { GUILayout.Label("머리 열림", GUILayout.Width(52)); openK = GUILayout.HorizontalSlider(openK, 0, 1, GUILayout.Width(60)); }
        showJoints = GUILayout.Toggle(showJoints, "관절 표시", EditorStyles.toolbarButton, GUILayout.Width(64));
        showGhost = GUILayout.Toggle(showGhost, "반영 전 모습 겹치기", EditorStyles.toolbarButton, GUILayout.Width(120));
        if (EditorGUI.EndChangeCheck()) Rebuild();
        if (GUILayout.Button("화면 맞춤", EditorStyles.toolbarButton, GUILayout.Width(64))) Fit();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    Camera Cam(Rect r)
    {
        var cam = pru.camera;
        cam.orthographicSize = camSize; cam.aspect = r.width / Mathf.Max(1, r.height);
        cam.transform.position = new Vector3(camPos.x, camPos.y, -20);
        return cam;
    }
    Vector2 ToGui(Rect r, Vector3 w) { var v = Cam(r).WorldToViewportPoint(w); return new Vector2(r.x + v.x * r.width, r.y + (1 - v.y) * r.height); }
    Vector3 ToWorld(Rect r, Vector2 g) { var v = Cam(r).ViewportToWorldPoint(new Vector3((g.x - r.x) / r.width, 1 - (g.y - r.y) / r.height, 20)); v.z = 0; return v; }

    void DrawPreview(Rect r)
    {
        var e = Event.current;
        if (e.type == EventType.Repaint)
        {
            Cam(r);
            pru.BeginPreview(r, GUIStyle.none);
            pru.camera.Render();
            GUI.DrawTexture(r, pru.EndPreview(), ScaleMode.StretchToFill, false);
            if (go && showJoints) DrawOverlay(r);
            if (!go) GUI.Label(new Rect(r.x + 10, r.y + 10, 400, 20), cur == null ? "몬스터를 고르세요" : "그림 없음 / 조립 실패", EditorStyles.whiteLabel);
            if (cur != null) GUI.Label(new Rect(r.x + 8, r.yMax - 20, 600, 18), $"{cur.code}  {(Dirty ? "● 반영 안 됨" : "")}  {status}", EditorStyles.whiteMiniLabel);
        }
        HandleMouse(r, e);
    }

    void DrawOverlay(Rect r)
    {
        Handles.BeginGUI();
        foreach (var kv in partOf)
        {
            var sr = kv.Key; if (!sr || !sr.enabled || !sr.sprite) continue;
            bool on = kv.Value == sel;
            if (on)
            {
                // 그림 테두리
                var b = sr.sprite.bounds; var t = sr.transform;
                var pts = new Vector3[5];
                Vector3[] c = { new(b.min.x, b.min.y), new(b.max.x, b.min.y), new(b.max.x, b.max.y), new(b.min.x, b.max.y) };
                for (int i = 0; i < 5; i++) pts[i] = ToGui(r, t.TransformPoint(c[i % 4]));
                Handles.color = new Color(1, 0.85f, 0.2f); Handles.DrawAAPolyLine(2.5f, pts);
            }
            // 관절 (파츠 보스 = 관절 Transform, 고양이 · 사람 = 그림 피벗 = 렌더러 Transform)
            var j = cur.kind == Kind.Part ? sr.transform.parent : sr.transform;
            var g = ToGui(r, j.position);
            Handles.color = on ? new Color(1, 0.3f, 0.2f) : new Color(1, 1, 1, 0.75f);
            Handles.DrawSolidDisc(g, Vector3.forward, on ? 5 : 3);
        }
        // 고양이 · 사람: 고른 파츠가 붙는 몸통 부착점
        if (cur.kind != Kind.Part && sel != null && AnchorName(sel) != null)
        {
            var torso = Renderer("torso");
            if (torso && torso.sprite)
            {
                var a = AnchorOf(AnchorName(sel)); var sp = torso.sprite;
                var lp = new Vector3((a.x * sp.rect.width - sp.pivot.x) / 100f, ((1 - a.y) * sp.rect.height - sp.pivot.y) / 100f, 0);
                var g = ToGui(r, torso.transform.TransformPoint(lp));
                Handles.color = new Color(0.3f, 0.9f, 1f); Handles.DrawWireDisc(g, Vector3.forward, 7); Handles.DrawWireDisc(g, Vector3.forward, 3);
            }
        }
        Handles.EndGUI();
    }

    SpriteRenderer Renderer(string part) { foreach (var kv in partOf) if (kv.Value == part && kv.Key && kv.Key.enabled) return kv.Key; return null; }

    // ── 마우스 ──
    void HandleMouse(Rect r, Event e)
    {
        if (cur == null) return;
        var id = GUIUtility.GetControlID(FocusType.Passive);
        switch (e.GetTypeForControl(id))
        {
            case EventType.ScrollWheel when r.Contains(e.mousePosition):
            {
                var before = ToWorld(r, e.mousePosition);
                camSize = Mathf.Clamp(camSize * (1 + e.delta.y * 0.04f), 0.05f, 100);
                var after = ToWorld(r, e.mousePosition);
                camPos += (Vector2)(before - after);
                e.Use(); Repaint();
                break;
            }
            case EventType.KeyDown when e.keyCode == KeyCode.F: Fit(); e.Use(); Repaint(); break;
            case EventType.MouseDown when r.Contains(e.mousePosition):
            {
                GUIUtility.hotControl = id;
                dragStartMouse = e.mousePosition; lastWorld = ToWorld(r, e.mousePosition);
                if (e.button != 0) { drag = Drag.Pan; e.Use(); break; }
                var hit = Pick(lastWorld);
                if (hit != null) sel = hit;
                drag = Drag.None;
                if (sel != null)
                {
                    drag = motionMode ? Drag.Rotate : e.shift ? Drag.Image : e.control && cur.kind == Kind.Part ? Drag.Rotate : Drag.Move;
                    if (motionMode) { motionDragR = PickRenderer(lastWorld); if (!motionDragR || !BeginMotionDrag()) drag = Drag.None; }
                    Undo.IncrementCurrentGroup();
                    if (drag == Drag.Rotate) lastAngle = AngleAt(r, e.mousePosition);
                }
                e.Use(); Repaint();
                break;
            }
            case EventType.MouseDrag when GUIUtility.hotControl == id:
            {
                var w = ToWorld(r, e.mousePosition);
                if (drag == Drag.Pan) { var d = (Vector2)(ToWorld(r, e.mousePosition - e.delta) - w); camPos += d; }
                else if (drag != Drag.None)
                {
                    Undo.RecordObject(work, "리깅 툴 끌기");
                    if (drag == Drag.Rotate && motionMode) { float a = AngleAt(r, e.mousePosition); MotionRotate(Mathf.DeltaAngle(lastAngle, a)); lastAngle = a; }
                    else if (drag == Drag.Rotate) { float a = AngleAt(r, e.mousePosition); Rotate(Mathf.DeltaAngle(lastAngle, a)); lastAngle = a; }
                    else DragBy(w - lastWorld, drag == Drag.Image);
                    lastWorld = w;
                    Rebuild();
                }
                e.Use(); Repaint();
                break;
            }
            case EventType.MouseUp when GUIUtility.hotControl == id:
                GUIUtility.hotControl = 0;
                if (drag != Drag.None && drag != Drag.Pan) Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                drag = Drag.None; e.Use(); Repaint();
                break;
        }
    }

    float AngleAt(Rect r, Vector2 mouse)
    {
        var j = motionMode ? MotionJoint() : Joint(sel); if (!j) return 0;
        var c = ToGui(r, j.position);
        return -Mathf.Atan2(mouse.y - c.y, mouse.x - c.x) * Mathf.Rad2Deg;
    }

    Transform Joint(string part)
    {
        var sr = Renderer(part); if (!sr) return null;
        return cur.kind == Kind.Part ? sr.transform.parent : sr.transform;
    }

    // 마우스 아래 가장 앞 그림
    string Pick(Vector3 w)
    {
        SpriteRenderer best = null;
        foreach (var kv in partOf)
        {
            var sr = kv.Key; if (!sr || !sr.enabled || !sr.sprite) continue;
            var lp = sr.transform.InverseTransformPoint(w); var b = sr.sprite.bounds;
            if (lp.x < b.min.x || lp.x > b.max.x || lp.y < b.min.y || lp.y > b.max.y) continue;
            if (!best || sr.sortingOrder > best.sortingOrder || sr.sortingOrder == best.sortingOrder && sr.sprite.rect.width * sr.sprite.rect.height < best.sprite.rect.width * best.sprite.rect.height) best = sr;
        }
        return best ? partOf[best] : null;
    }

    void Rotate(float deg)
    {
        var p = work.part.parts.Find(x => x.name == sel); if (p == null) return;
        // 화면 각도 → 관절 기준 각도 (부모가 뒤집혀 있으면 반대)
        var j = Joint(sel); float sign = j && j.parent && j.parent.lossyScale.x * j.parent.lossyScale.y < 0 ? -1 : 1;
        p.angle = Mathf.Round((p.angle + deg * sign) * 10) / 10;
    }

    // 월드에서 움직인 만큼 값을 바꿈
    void DragBy(Vector3 wd, bool image)
    {
        if (cur.kind == Kind.Part)
        {
            int i = work.part.parts.FindIndex(x => x.name == sel); if (i < 0) return;
            var p = work.part.parts[i]; var sr = Renderer(sel); if (!sr) return;
            var joint = sr.transform.parent;
            if (image)
            {
                // 관절은 두고 그림(과 자식 파츠)만: 관절 좌표의 이동 픽셀 → 피벗 반대로
                var d = joint.InverseTransformVector(wd) * 100;
                float w = p.sprite.rect.width, h = p.sprite.rect.height;
                p.pivot = R4(new Vector2(p.pivot.x - d.x / w, p.pivot.y + d.y / h));
            }
            else
            {
                var par = joint.parent;      // 부모 관절 (뿌리면 visual)
                var d = par.InverseTransformVector(wd) * 100;
                var pp = work.part.parts.Find(x => x.name == p.parent);
                if (pp == null || string.IsNullOrEmpty(p.parent))
                {
                    float w = p.sprite.rect.width, h = p.sprite.rect.height;
                    p.at = R4(new Vector2(p.at.x + d.x / w, p.at.y + d.y / h));
                }
                else
                {
                    float w = pp.sprite.rect.width, h = pp.sprite.rect.height;
                    p.at = R4(new Vector2(p.at.x + d.x / w, p.at.y - d.y / h));
                }
            }
            return;
        }
        // 고양이 · 사람
        if (image)
        {
            var sr = Renderer(sel); if (!sr || !tempSprites.TryGetValue(sr.sprite, out var src)) return;
            var pv = work.pivots.Find(x => x.part == src.slot); if (pv == null) return;
            var d = sr.transform.InverseTransformVector(wd) * 100;
            pv.tl = R4(new Vector2(pv.tl.x - d.x / src.src.rect.width, pv.tl.y + d.y / src.src.rect.height));
        }
        else
        {
            var an = AnchorName(sel); var torso = Renderer("torso");
            if (an == null || !torso || !torso.sprite) { status = "몸통은 다른 파츠가 붙는 기준이라 옮길 수 없어요 (Shift+끌기 = 그림 관절)"; return; }
            var d = torso.transform.InverseTransformVector(wd) * 100;
            var a = AnchorOf(an);
            SetAnchor(an, R4(new Vector2(a.x + d.x / torso.sprite.rect.width, a.y - d.y / torso.sprite.rect.height)));
        }
    }

    static Vector2 R4(Vector2 v) => new(Mathf.Round(v.x * 10000) / 10000, Mathf.Round(v.y * 10000) / 10000);

    string AnchorName(string part) => cur.kind == Kind.Cat
        ? part switch { "head" or "head2" => "neck", "tail" => "tailAnchor", "front" => "shoulder", "back" => "hip", _ => null }
        : part switch { "head" or "scared" or "angry" => "neck", "arm" or "arm2" => "shoulder", "leg" => "hip", _ => null };

    Vector2 AnchorOf(string an)
    {
        if (cur.kind == Kind.Cat) return an switch { "neck" => work.cat.neck, "tailAnchor" => work.cat.tailAnchor, "shoulder" => work.cat.shoulder, _ => work.cat.hip };
        return an switch { "neck" => work.human.neck, "shoulder" => work.human.shoulder, _ => work.human.hip };
    }
    void SetAnchor(string an, Vector2 v)
    {
        if (cur.kind == Kind.Cat) { if (an == "neck") work.cat.neck = v; else if (an == "tailAnchor") work.cat.tailAnchor = v; else if (an == "shoulder") work.cat.shoulder = v; else work.cat.hip = v; }
        else { if (an == "neck") work.human.neck = v; else if (an == "shoulder") work.human.shoulder = v; else work.human.hip = v; }
    }

    // ── 오른쪽: 값 · 반영 ──
    void DrawInspector()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(330));
        if (cur == null) { EditorGUILayout.HelpBox("왼쪽에서 몬스터를 고르세요.", MessageType.Info); EditorGUILayout.EndVertical(); return; }
        EditorGUILayout.LabelField(cur.boss ? $"{cur.name}  ({cur.code})" : cur.code, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(cur.kind == Kind.Part ? "파츠 보스 리그 (BossPartArt)" : cur.kind == Kind.Cat ? "고양이 리그 (CatArtLibrary)" : "사람 리그 (HumanArtLibrary)", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = Dirty;
        var bg = GUI.backgroundColor; GUI.backgroundColor = new Color(0.5f, 1f, 0.55f);
        if (GUILayout.Button("게임에 반영", GUILayout.Height(28))) Save();
        GUI.backgroundColor = bg;
        if (motionMode && GUILayout.Button("되돌리기", GUILayout.Height(28), GUILayout.Width(70))) { PickMove(curMove); GUIUtility.ExitGUI(); }
        if (!motionMode && GUILayout.Button("되돌리기", GUILayout.Height(28), GUILayout.Width(70))) { var m = cur; savedJson = EditorJsonUtility.ToJson(work); cur = null; Select(m); }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        inspScroll = EditorGUILayout.BeginScrollView(inspScroll);
        var so = new SerializedObject(work);
        so.Update();
        EditorGUI.BeginChangeCheck();
        if (motionMode) InspectMotion(); else if (cur.kind == Kind.Part) InspectPart(so); else InspectRig(so);
        if (EditorGUI.EndChangeCheck()) { so.ApplyModifiedProperties(); Rebuild(); }
        else so.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void InspectPart(SerializedObject so)
    {
        var e = so.FindProperty("part");
        EditorGUILayout.PropertyField(e.FindPropertyRelative("length"), new GUIContent("길이 (뿌리 그림 가로)"));
        EditorGUILayout.PropertyField(e.FindPropertyRelative("height"), new GUIContent("높이 (말풍선 · 소품)"));
        var parts = e.FindPropertyRelative("parts");
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"파츠 {parts.arraySize}개 (위 = 먼저 조립, 첫 줄 = 뿌리)", EditorStyles.boldLabel);
        for (int i = 0; i < parts.arraySize; i++)
        {
            var p = work.part.parts[i];
            var st = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft };
            if (p.name == sel) st.normal.textColor = st.hover.textColor = new Color(1f, 0.8f, 0.3f);
            string depth = ""; var pp = p; int guard = 0; while (!string.IsNullOrEmpty(pp.parent) && guard++ < 10) { depth += "  "; pp = work.part.parts.Find(x => x.name == pp.parent) ?? new BossPartArt.Part(); }
            if (GUILayout.Button($"{depth}{p.name}   <{p.order}>{(p.far ? " 먼쪽" : "")}{(p.move != BossPartArt.Move.None ? " · " + p.move : "")}", st)) { sel = p.name; Repaint(); }
        }
        int si = work.part.parts.FindIndex(x => x.name == sel);
        if (si < 0) return;
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("고른 파츠: " + sel, EditorStyles.boldLabel);
        var sp = parts.GetArrayElementAtIndex(si);
        foreach (var f in new[] { "name", "sprite", "alt", "parent", "at", "pivot", "angle", "scale", "flipX", "order", "far", "move", "amp", "phase", "speed", "show", "open", "neck", "wind", "strike" })
        {
            var prop = sp.FindPropertyRelative(f);
            if (f == "name")
            {
                EditorGUI.BeginChangeCheck();
                var old = prop.stringValue;
                EditorGUILayout.PropertyField(prop);
                if (EditorGUI.EndChangeCheck() && prop.stringValue != old)
                {
                    // 이름을 바꾸면 자식의 부모 이름도
                    for (int k = 0; k < parts.arraySize; k++) { var pr = parts.GetArrayElementAtIndex(k).FindPropertyRelative("parent"); if (pr.stringValue == old) pr.stringValue = prop.stringValue; }
                    sel = prop.stringValue;
                }
                continue;
            }
            if (f == "neck" || f == "wind" || f == "strike") { if (work.part.parts[si].move != BossPartArt.Move.Neck) continue; }
            if (f == "open" && work.part.parts[si].show == BossPartArt.Show.Always) continue;
            EditorGUILayout.PropertyField(prop);
        }
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"그리는 순서 {work.part.parts[si].order}", GUILayout.Width(110));
        if (GUILayout.Button("▲ 앞으로", EditorStyles.miniButtonLeft)) { Undo.RecordObject(work, "그리는 순서"); work.part.parts[si].order++; so.Update(); GUI.changed = true; }
        if (GUILayout.Button("▼ 뒤로", EditorStyles.miniButtonRight)) { Undo.RecordObject(work, "그리는 순서"); work.part.parts[si].order--; so.Update(); GUI.changed = true; }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("그림 파일 바꾸기…")) ReplaceImage(work.part.parts[si].sprite);
        if (GUILayout.Button("복제")) { Undo.RecordObject(work, "파츠 복제"); var c = MonsterRigSave.Clone(work.part.parts[si]); c.name = Unique(c.name); work.part.parts.Insert(si + 1, c); sel = c.name; so.Update(); Rebuild(); GUIUtility.ExitGUI(); }
        GUI.enabled = si > 0;
        if (GUILayout.Button("삭제"))
        {
            var name = work.part.parts[si].name;
            if (work.part.parts.Exists(x => x.parent == name)) EditorUtility.DisplayDialog("몬스터 리깅", $"{name} 에 붙은 자식 파츠가 있어요. 자식을 먼저 지우거나 다른 부모로 옮기세요.", "확인");
            else { Undo.RecordObject(work, "파츠 삭제"); work.part.parts.RemoveAt(si); sel = null; so.Update(); Rebuild(); GUIUtility.ExitGUI(); }
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("at = 부모 그림 좌상단 기준 0~1 붙는 자리 (뿌리는 발끝에서 뿌리 크기 배율) · pivot = 이 그림의 관절 (좌상단 기준 0~1) · order 클수록 앞.", MessageType.None);
    }

    string Unique(string n) { string s = n + "_2"; int k = 2; while (work.part.parts.Exists(x => x.name == s)) s = n + "_" + (++k); return s; }

    void InspectRig(SerializedObject so)
    {
        bool cat = cur.kind == Kind.Cat;
        var e = so.FindProperty(cat ? "cat" : "human");
        EditorGUILayout.LabelField("몸통에 붙는 자리 (몸통 그림 좌상단 기준 0~1)", EditorStyles.boldLabel);
        Field(e, "neck", "목 (머리)");
        if (cat) Field(e, "tailAnchor", "꼬리");
        Field(e, "shoulder", cat ? "어깨 (앞다리)" : "어깨 (팔)");
        Field(e, "hip", cat ? "엉덩이 (뒷다리)" : "엉덩이 (다리)");
        EditorGUILayout.Space(4);
        if (cat) { Field(e, "legFront", "앞다리 크기"); Field(e, "legBack", "뒷다리 크기"); }
        Field(e, "headScale", cat ? "머리 크기 (0 = 자동)" : "머리 크기");
        if (!cat) Field(e, "front", "정면 몸통");
        if (cat && cur.boss) EditorGUILayout.HelpBox("고양이 보스 머리 크기: 0 = 몸통 폭 × 0.62 이하로 자동 (게임과 같음).", MessageType.None);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("그림 관절 (그림 좌상단 기준 0~1, 이 점이 몸통 자리에 붙음)", EditorStyles.boldLabel);
        foreach (var pv in work.pivots)
        {
            var s = cat ? CatSprite(work.cat, pv.part) : HumanSprite(work.human, pv.part);
            EditorGUILayout.BeginHorizontal();
            var st = new GUIStyle(EditorStyles.label); if (pv.part == sel || sel == "arm" && pv.part == "arm2" && false) st.normal.textColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button(pv.part, st, GUILayout.Width(52))) { sel = pv.part == "head2" || pv.part == "scared" || pv.part == "angry" ? "head" : pv.part; Repaint(); }
            EditorGUI.BeginChangeCheck();
            var v = EditorGUILayout.Vector2Field(GUIContent.none, pv.tl);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(work, "관절"); pv.tl = v; GUI.changed = true; }
            if (GUILayout.Button("그림…", GUILayout.Width(46))) ReplaceImage(s);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("그리는 순서 (클수록 앞 · 굵게 = 이 몬스터만 바꾼 값)", EditorStyles.boldLabel);
        var orders = cat ? work.cat.orders : work.human.orders;
        foreach (var (part, label) in cat ? CatOrders : HumanOrders)
        {
            var rr = RigRenderer(part); int now = rr ? rr.sortingOrder : 0;
            var ov = orders.Find(x => x.part == part);
            int val = ov != null ? ov.order : now, set = val; bool clear = false;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, ov != null ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Width(80));
            set = EditorGUILayout.IntField(val, GUILayout.Width(44));
            if (GUILayout.Button("▲ 앞으로", EditorStyles.miniButtonLeft, GUILayout.Width(62))) set = val + 1;
            if (GUILayout.Button("▼ 뒤로", EditorStyles.miniButtonRight, GUILayout.Width(54))) set = val - 1;
            if (ov != null && GUILayout.Button("기본", EditorStyles.miniButton, GUILayout.Width(38))) clear = true;
            EditorGUILayout.EndHorizontal();
            if (clear) { Undo.RecordObject(work, "그리는 순서"); orders.Remove(ov); GUI.changed = true; }
            else if (set != val) { Undo.RecordObject(work, "그리는 순서"); if (ov == null) orders.Add(ov = new PartOrder { part = part }); ov.order = set; GUI.changed = true; }
        }
        if (orders.Count > 0 && GUILayout.Button("전부 기본 순서로")) { Undo.RecordObject(work, "그리는 순서"); orders.Clear(); GUI.changed = true; }
        if (cat) EditorGUILayout.LabelField("앞발을 치켜들 땐 (공격 자세) 앞다리가 머리 바로 앞으로 올라와요.", EditorStyles.miniLabel);

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(cat
            ? "고양이: 끌기 = 붙는 자리 (머리→목 · 앞다리→어깨 · 뒷다리→엉덩이 · 꼬리), Shift+끌기 = 그 그림의 관절. 먼 쪽 다리 간격 · 그리는 순서는 리그 공통 값 (RatRig)."
            : "사람: 끌기 = 붙는 자리 (머리→목 · 팔→어깨 · 다리→엉덩이), Shift+끌기 = 그 그림의 관절. 다리는 발이 바닥 기준이라 엉덩이를 옮기면 몸통이 움직여요. 팔 · 다리 앞뒤 간격 · 순서는 리그 공통 값 (HumanRig).",
            MessageType.None);
    }

    // 그리는 순서를 고칠 수 있는 렌더러 (RatRig · HumanRig 파츠 이름)
    static readonly (string, string)[] CatOrders = { ("head", "머리"), ("front", "앞다리"), ("back", "뒷다리"), ("torso", "몸통"), ("tail", "꼬리"), ("farFront", "먼 앞다리"), ("farBack", "먼 뒷다리") };
    static readonly (string, string)[] HumanOrders = { ("armNear", "가까운 팔"), ("head", "머리"), ("torso", "몸통"), ("legNear", "가까운 다리"), ("legFar", "먼 다리"), ("armFar", "먼 팔") };
    SpriteRenderer RigRenderer(string part)
    {
        if (!go) return null;
        var rr = go.GetComponent<RatRig>(); if (rr) return rr.PartRenderer(part);
        var hr = go.GetComponent<HumanRig>(); return hr ? hr.PartRenderer(part) : null;
    }

    static void Field(SerializedProperty e, string f, string label) => EditorGUILayout.PropertyField(e.FindPropertyRelative(f), new GUIContent(label));

    void ReplaceImage(Sprite s)
    {
        if (!s) { EditorUtility.DisplayDialog("몬스터 리깅", "이 칸에는 그림이 없어요.", "확인"); return; }
        var file = EditorUtility.OpenFilePanel("새 그림 (PNG, 같은 방향 · 관절 비율 그대로)", Path.GetDirectoryName(Path.GetFullPath(AssetDatabase.GetAssetPath(s))), "png");
        if (string.IsNullOrEmpty(file)) return;
        if (!EditorUtility.DisplayDialog("몬스터 리깅", $"{AssetDatabase.GetAssetPath(s)}\n그림을 {Path.GetFileName(file)} 로 바꿀까요? (바로 게임 그림이 바뀌고, 예전 그림은 UnityResources/Rats/_rigtool_backup 에 남아요)", "바꾸기", "취소")) return;
        status = MonsterRigSave.ReplaceImage(s, file);
        Debug.Log("[리깅 툴] 그림 바꿈: " + status);
        Rebuild(); Repaint();
    }

    void Save()
    {
        if (motionMode && curMove != null)
        {
            try { status = "반영함 · " + SaveMotion(); Debug.Log("[리깅 툴] " + cur.code + " " + status); }
            catch (Exception ex) { status = "반영 실패: " + ex.Message; Debug.LogException(ex); }
            return;
        }
        try
        {
            string msg;
            if (cur.kind == Kind.Part) { MonsterRigSave.SavePart(work.part); msg = "BossPartArt · BossPartArtEdits 에 씀"; }
            else
            {
                var piv = new Dictionary<string, Vector2>();
                // 바뀐 관절만
                foreach (var pv in work.pivots)
                {
                    var s = cur.kind == Kind.Cat ? CatSprite(work.cat, pv.part) : HumanSprite(work.human, pv.part);
                    if (!s) continue;
                    var old = new Vector2(s.pivot.x / s.rect.width, 1 - s.pivot.y / s.rect.height);
                    if ((old - pv.tl).sqrMagnitude > 1e-9f) piv[pv.part] = pv.tl;
                }
                msg = cur.kind == Kind.Cat ? MonsterRigSave.SaveCat(work.cat, piv) : MonsterRigSave.SaveHuman(work.human, piv);
            }
            status = "반영함 · " + msg;
            Debug.Log($"[리깅 툴] {cur.code} 게임에 반영 ({msg})");
            var m = cur; savedJson = EditorJsonUtility.ToJson(work); cur = null; Select(m);
            status = "반영함 · " + msg;
        }
        catch (Exception ex) { status = "반영 실패: " + ex.Message; Debug.LogException(ex); }
    }
}

