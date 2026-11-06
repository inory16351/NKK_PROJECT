using System;
using System.Collections.Generic;
using System.IO;
using NKK.Data;
using NKK.Hazards;
using NKK.Humans;
using NKK.Rats;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 몬스터 리깅 툴 — 보스 공격 · 필살기 모션 (2026-11-06 사용자: 공격 모션 · 필살기 모션도 수정 · 확인).
// 보스의 기술 5칸 (일반 · 두 번째 · 필살기(쿨타임 스킬) · 발악 1 · 발악 2) 을 시간 막대로 재생. 고친 모션이 없으면 게임 코드의 자세 (Boss.CatAttackPose · HumanAttackPose) 그대로.
// 키 프레임으로 고치면 Resources/BossMotion.asset 에 저장 → 게임이 그 기술 동안 키 사이를 이어 씀. 효과(투사체 · 소환)와 판정은 코드 그대로 → '게임에서 보기' 로 확인.
public partial class MonsterRigTool
{
    const string MotionAsset = "Assets/Resources/BossMotion.asset";
    class Move { public string kind, type; public BossAtkRow atk; }
    readonly List<Move> moves = new();
    Move curMove;
    bool motionMode;
    MonsterRigMotionWork mwork;
    string msavedJson;
    float mt; bool mplay = false, mrepeat = true; double mlast;
    int selKey = -1;
    // 끌어서 돌리기
    string motionField; Transform motionJointCache;
    SpriteRenderer motionDragR;
    // 점프 (Boss 기본값 — 게임 씬 값과 다를 수 있음)
    float jumpV = 900, pounceV = 700, gravity = 1800;
    static readonly Dictionary<string, BossAtkRow> atkRows = new();

    bool MotionDirty => motionMode && cur != null && curMove != null && mwork && msavedJson != null && EditorJsonUtility.ToJson(mwork) != msavedJson;

    void InitMotion()
    {
        if (!mwork) { mwork = CreateInstance<MonsterRigMotionWork>(); mwork.hideFlags = HideFlags.HideAndDontSave; }
        atkRows.Clear();
        var path = "Assets/Data/BossTable.json";
        if (File.Exists(path)) foreach (var a in JsonUtility.FromJson<BossTableFile>(File.ReadAllText(path)).Atk_Type) atkRows[a.atk_type] = a;
        // 점프 세기 · 중력: Game 씬의 Boss 값 (씬 파일에서 읽음 — 오브젝트를 만들면 열린 씬이 '바뀜' 표시가 됨)
        try
        {
            var txt = File.ReadAllText("Assets/Scenes/Game.unity");
            int i = txt.IndexOf("\n  pounceV: ", StringComparison.Ordinal);
            if (i > 0)
            {
                int s = txt.LastIndexOf("\n--- ", i, StringComparison.Ordinal), e = txt.IndexOf("\n--- ", i, StringComparison.Ordinal);
                var block = txt.Substring(s, (e > 0 ? e : txt.Length) - s);
                float F(string k, float d) { var m = System.Text.RegularExpressions.Regex.Match(block, "\n  " + k + ": ([-0-9.eE]+)"); return m.Success ? float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : d; }
                jumpV = F("jumpV", jumpV); pounceV = F("pounceV", pounceV); gravity = F("gravity", gravity);
            }
        }
        catch (Exception ex) { Debug.LogWarning("[리깅 툴] 보스 점프 값 못 읽음: " + ex.Message); }
    }

    void EnterMotion()
    {
        if (atkRows.Count == 0) InitMotion();
        moves.Clear();
        var r = cur?.row; if (r == null) { curMove = null; return; }
        var slots = new[] { ("일반 공격", r.atk_type), ("두 번째 공격", r.atk2_type), ("필살기 (쿨타임 스킬)", r.skill_type), ("발악 1", r.special1_type), ("발악 2", r.special2_type) };
        foreach (var (k, t) in slots) if (!string.IsNullOrEmpty(t) && t != "None" && atkRows.TryGetValue(t, out var a) && !moves.Exists(m => m.type == t)) moves.Add(new Move { kind = k, type = t, atk = a });
        PickMove(moves.Count > 0 ? moves[0] : null);
    }

    void PickMove(Move m)
    {
        curMove = m; selKey = -1; mt = 0;
        Undo.ClearUndo(mwork);
        mwork.motion = new BossMotion.Motion { codeId = cur.code, atkType = m?.type };
        var asset = AssetDatabase.LoadAssetAtPath<BossMotion>(MotionAsset);
        var saved = m != null && asset ? asset.Get(cur.code, m.type) : null;
        if (saved != null) mwork.motion = MonsterRigSave.Clone(saved);
        msavedJson = EditorJsonUtility.ToJson(mwork);
        Rebuild(); Repaint();
    }

    // ── 시간 ──
    BossAtkRow A => curMove?.atk;
    bool Jump => curMove != null && Boss.IsJumpAttack(curMove.type);
    float AirT => Jump ? 2 * (curMove.type == "Pounce" ? pounceV : jumpV) / Mathf.Max(1, gravity) : 0;
    float End => A == null ? 0 : A.windup + Mathf.Max(A.dur, AirT);           // 공격이 끝나는 때 (대략: 덮치기는 착지 때 끝남)
    float Length => Mathf.Max(End, mwork && mwork.motion.keys.Count > 0 ? mwork.motion.Length : 0) + 0.5f;
    bool Up(float t) => Jump && t >= A.windup && t < A.windup + AirT;
    bool HasKeys => mwork && mwork.motion.keys.Count > 0;

    bool MotionTick()
    {
        if (!motionMode || !mplay) return false;
        double now = EditorApplication.timeSinceStartup;
        if (now - mlast < 1 / 30.0) return true;
        mt += (float)(now - mlast); mlast = now;
        if (mt > Length) { if (mrepeat) mt = 0; else { mt = Length; mplay = false; } }
        Rebuild(); Repaint();
        return true;
    }

    void MotionToolbar()
    {
        if (curMove == null) { GUILayout.Label("이 보스는 기술이 없어요"); return; }
        if (GUILayout.Button(mplay ? "❚❚" : "▶", EditorStyles.toolbarButton, GUILayout.Width(28))) { mplay = !mplay; mlast = EditorApplication.timeSinceStartup; if (mplay && mt >= Length - 0.01f) mt = 0; }
        mrepeat = GUILayout.Toggle(mrepeat, "반복", EditorStyles.toolbarButton, GUILayout.Width(36));
        GUILayout.Label($"{mt:0.00}s", GUILayout.Width(44));
        mt = GUILayout.HorizontalSlider(mt, 0, Length, GUILayout.MinWidth(160));
        GUILayout.Label(mt < A.windup ? "준비" : mt < End ? (Up(mt) ? "공중" : "공격") : "끝", GUILayout.Width(32));
        showGhost = GUILayout.Toggle(showGhost, "코드 모션 겹치기", EditorStyles.toolbarButton, GUILayout.Width(100));
        if (GUILayout.Button("화면 맞춤", EditorStyles.toolbarButton, GUILayout.Width(60))) Fit();
    }

    // ── 자세 (미리보기) ──
    RatRig.Pose CodeCat(float t, out bool head2)
    {
        var p = new RatRig.Pose { head = Mathf.Sin(t * 1.6f) * 0.06f, tail = 0.1f + Mathf.Sin(t * 3) * 0.25f, sx = 1, sy = 1 };
        head2 = t < End;
        if (t < End) Boss.CatAttackPose(curMove.type, A, t, Up(t), t, ref p);
        return p;
    }
    HumanRig.Pose CodeHuman(float t)
    {
        var p = new HumanRig.Pose { sx = 1, sy = 1 };
        if (t < End) Boss.HumanAttackPose(curMove.type, A, t, Up(t), ref p);
        return p;
    }

    RatRig.Pose MotionCat(RatRig rr, RatArtLibrary.Entry e, bool code)
    {
        bool h2; RatRig.Pose p;
        if (!code && HasKeys && mt < End) p = BossMotion.Cat(mwork.motion, mt, out h2); else p = CodeCat(mt, out h2);
        var opt = mt < End ? OptHead() : null;      // opt head:그림 (게임: ReworkHead 가 먼저)
        rr.headReplace = opt ? opt : h2 ? e.head2 : null;
        return p;
    }
    HumanRig.Pose MotionHuman(bool code) => !code && HasKeys && mt < End ? BossMotion.Human(mwork.motion, mt) : CodeHuman(mt);
    bool MotionPart(BossPartRig pr, bool code)
    {
        pr.extra.Clear();
        if (mt >= End) return false;
        if (!code && HasKeys) return BossMotion.Part(mwork.motion, mt, pr.extra);
        return true;
    }

    // 점프 공격: 몸을 띄움 (게임 z)
    void Lift(GameObject o)
    {
        if (!Up(mt)) return;
        float tau = mt - A.windup, v = curMove.type == "Pounce" ? pounceV : jumpV;
        float z = Mathf.Max(0, v * tau - gravity * tau * tau / 2);
        o.transform.position += new Vector3(0, z * NKK.World.U, 0);
    }

    // opt 의 head:그림 (보스 밈 그림 이름) → 스프라이트
    Sprite OptHead()
    {
        var opt = A?.opt; if (string.IsNullOrEmpty(opt)) return null;
        foreach (var part in opt.Split(','))
        {
            var p = part.Trim();
            if (!p.StartsWith("head:") && !p.StartsWith("head_keep:")) continue;
            var n = p.Substring(p.IndexOf(':') + 1);
            foreach (var g in AssetDatabase.FindAssets(n + " t:Sprite", new[] { "Assets/Art" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(path) == n) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        return null;
    }

    // ── 키 ──
    BossMotion.Key KeyNow()
    {
        var k = new BossMotion.Key { t = Mathf.Round(mt * 100) / 100 };
        switch (cur.kind)
        {
            case Kind.Cat:
            {
                bool h2; var p = HasKeys ? BossMotion.Cat(mwork.motion, mt, out h2) : CodeCat(mt, out h2);
                k.head = p.head; k.headX = p.headX; k.tail = p.tail; k.front = p.front; k.back = p.back; k.farFront = p.farFront; k.farBack = p.farBack; k.bob = p.bob; k.tilt = p.tilt; k.sx = p.sx; k.sy = p.sy; k.head2 = h2;
                break;
            }
            case Kind.Human:
            {
                var p = HasKeys ? BossMotion.Human(mwork.motion, mt) : CodeHuman(mt);
                k.armN = p.armN; k.armF = p.armF; k.legN = p.legN; k.legF = p.legF; k.lean = p.lean; k.head = p.head; k.bob = p.bob; k.sx = p.sx; k.sy = p.sy; k.angry = p.angry; k.scared = p.scared;
                break;
            }
            default:
            {
                var d = new Dictionary<string, float>();
                k.alt = HasKeys ? BossMotion.Part(mwork.motion, mt, d) : mt < End;
                foreach (var kv in d) if (Mathf.Abs(kv.Value) > 0.01f) k.parts.Add(new BossMotion.PartAngle { part = kv.Key, angle = kv.Value });
                break;
            }
        }
        return k;
    }

    int AddKeyNow()
    {
        var ks = mwork.motion.keys;
        int i = ks.FindIndex(x => Mathf.Abs(x.t - mt) < 0.015f);
        if (i >= 0) return i;
        var k = KeyNow();
        Undo.RecordObject(mwork, "모션 키");
        ks.Add(k); ks.Sort((a, b) => a.t.CompareTo(b.t));
        return ks.IndexOf(k);
    }

    // 코드 모션을 키로 옮김: 준비 시작 · 준비 끝 · 공격 중 0.2초 간격 · 끝
    void KeysFromCode()
    {
        Undo.RecordObject(mwork, "모션 만들기");
        mwork.motion.keys.Clear();
        float keep = mt;
        var ts = new List<float> { 0, Mathf.Max(0, A.windup - 0.02f), A.windup };
        for (float t = A.windup + 0.2f; t < End - 0.05f; t += 0.2f) ts.Add(t);
        ts.Add(Mathf.Max(A.windup, End - 0.02f));
        var made = new List<BossMotion.Key>();          // 키가 다 생긴 뒤에 넣음 (넣는 도중엔 KeyNow 가 코드 모션을 읽게)
        foreach (var t in ts) { mt = t; var k = KeyNow(); k.t = Mathf.Round(t * 100) / 100; if (!made.Exists(x => Mathf.Abs(x.t - k.t) < 0.005f)) made.Add(k); }
        made.Sort((a, b) => a.t.CompareTo(b.t));
        mwork.motion.keys.AddRange(made);
        mt = keep; selKey = -1;
    }

    // ── 끌어서 돌리기 (지금 시간의 키, 없으면 만들어서) ──
    SpriteRenderer PickRenderer(Vector3 w)
    {
        SpriteRenderer best = null;
        foreach (var sr in go ? go.GetComponentsInChildren<SpriteRenderer>() : new SpriteRenderer[0])
        {
            if (!sr.enabled || !sr.sprite || sr.name == "Shadow") continue;
            var lp = sr.transform.InverseTransformPoint(w); var b = sr.sprite.bounds;
            if (lp.x < b.min.x || lp.x > b.max.x || lp.y < b.min.y || lp.y > b.max.y) continue;
            if (!best || sr.sortingOrder > best.sortingOrder) best = sr;
        }
        return best;
    }

    bool BeginMotionDrag()
    {
        motionField = null;
        var r = motionDragR;
        if (cur.kind == Kind.Part) motionField = "part:" + r.transform.parent.name;
        else if (go.GetComponent<RatRig>() is RatRig rr)
            motionField = r == rr.head ? "head" : r == rr.tail ? "tail" : r == rr.front ? "front" : r == rr.back ? "back" : r == rr.farFront ? "farFront" : r == rr.farBack ? "farBack" : r == rr.torso ? "tilt" : null;
        else if (go.GetComponent<HumanRig>() is HumanRig hr)
            motionField = r == hr.head ? "head" : r == hr.armNear ? "armN" : r == hr.armFar ? "armF" : r == hr.legNear ? "legN" : r == hr.legFar ? "legF" : r == hr.torso ? "lean" : null;
        if (motionField == null) return false;
        sel = cur.kind == Kind.Part ? r.transform.parent.name : null;
        selKey = AddKeyNow();
        return true;
    }

    // 돌릴 축 (화면 각도 잴 중심)
    Transform MotionJoint()
    {
        if (!go || motionField == null) return null;
        if (motionField.StartsWith("part:")) { var n = motionField.Substring(5); foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>()) if (sr.transform.parent.name == n) return sr.transform.parent; return null; }
        if (go.GetComponent<RatRig>() is RatRig rr)
            return motionField switch { "head" => rr.head.transform, "tail" => rr.tail.transform, "front" => rr.front.transform, "back" => rr.back.transform, "farFront" => rr.farFront.transform, "farBack" => rr.farBack.transform, _ => rr.body };
        if (go.GetComponent<HumanRig>() is HumanRig hr)
            return motionField switch { "head" => hr.head.transform, "armN" => hr.armNear.transform, "armF" => hr.armFar.transform, "legN" => hr.legNear.transform, "legF" => hr.legFar.transform, _ => hr.body };
        return null;
    }

    // 화면에서 돌린 각도 (°, 반시계 +) → 키 값 (리그마다 방향이 다름: RatRig 다리 = −, 꼬리 · 머리 · 기울기 = + / HumanRig 몸 기울기 = −)
    void MotionRotate(float deg)
    {
        if (selKey < 0 || selKey >= mwork.motion.keys.Count) return;
        Undo.RecordObject(mwork, "모션 돌리기");
        var k = mwork.motion.keys[selKey]; float r = deg * Mathf.Deg2Rad;
        switch (motionField)
        {
            case "head": k.head += r; break;
            case "tail": k.tail += r; break;
            case "tilt": k.tilt += r; break;
            case "front": k.front -= r; break;
            case "back": k.back -= r; break;
            case "farFront": k.farFront -= r; break;
            case "farBack": k.farBack -= r; break;
            case "armN": k.armN += r; break;
            case "armF": k.armF += r; break;
            case "legN": k.legN += r; break;
            case "legF": k.legF += r; break;
            case "lean": k.lean -= r; break;
            default:
                if (motionField != null && motionField.StartsWith("part:"))
                {
                    var n = motionField.Substring(5); var j = MotionJoint();
                    float sign = j && j.parent && j.parent.lossyScale.x * j.parent.lossyScale.y < 0 ? -1 : 1;
                    var pa = k.parts.Find(x => x.part == n); if (pa == null) k.parts.Add(pa = new BossMotion.PartAngle { part = n });
                    pa.angle = Mathf.Round((pa.angle + deg * sign) * 10) / 10;
                }
                break;
        }
    }

    // ── 오른쪽 칸 ──
    Vector2 motionScroll;
    void InspectMotion()
    {
        if (curMove == null) { EditorGUILayout.HelpBox("이 보스는 기술 칸(Atk_Type)이 없어요.", MessageType.Info); return; }
        EditorGUILayout.LabelField("기술", EditorStyles.boldLabel);
        foreach (var m in moves)
        {
            var st = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, richText = true };
            if (m == curMove) st.normal.textColor = st.hover.textColor = new Color(1f, 0.8f, 0.3f);
            var asset = AssetDatabase.LoadAssetAtPath<BossMotion>(MotionAsset);
            bool edited = asset && asset.Get(cur.code, m.type) != null;
            if (GUILayout.Button($"{m.kind} · {(string.IsNullOrEmpty(m.atk.atk_name) ? m.type : m.atk.atk_name)}{(edited ? "  ✎" : "")}", st))
            {
                if (MotionDirty && !EditorUtility.DisplayDialog("몬스터 리깅", "반영하지 않은 모션 수정이 있어요. 버릴까요?", "버리기", "취소")) return;
                PickMove(m); GUIUtility.ExitGUI();
            }
        }
        var a = A;
        EditorGUILayout.LabelField($"{curMove.type} · 준비 {a.windup:0.##}s · 지속 {a.dur:0.##}s{(Jump ? $" · 점프 {AirT:0.##}s" : "")}{(string.IsNullOrEmpty(a.pattern) ? "" : " · " + a.pattern)}", EditorStyles.miniLabel);
        if (!string.IsNullOrEmpty(a.opt)) EditorGUILayout.LabelField("opt: " + a.opt, EditorStyles.miniLabel);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("▶ 게임에서 보기 (플레이 모드로 이 기술 발동)", GUILayout.Height(24))) MonsterRigPlayTest.Run(cur.code, curMove.type);

        EditorGUILayout.Space(6);
        var ks = mwork.motion.keys;
        EditorGUILayout.LabelField(ks.Count == 0 ? "모션: 게임 코드 기본 (고친 키 없음)" : $"모션: 고친 키 {ks.Count}개 (이 기술 동안 코드 자세 대신)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (ks.Count == 0) { if (GUILayout.Button("코드 모션을 키로 만들기")) { KeysFromCode(); Rebuild(); } }
        else
        {
            if (GUILayout.Button("지금 시간에 키 추가")) { selKey = AddKeyNow(); Rebuild(); }
            if (GUILayout.Button("모션 지우기 (기본으로)")) { Undo.RecordObject(mwork, "모션 지우기"); ks.Clear(); selKey = -1; Rebuild(); }
        }
        EditorGUILayout.EndHorizontal();
        if (ks.Count == 0) { EditorGUILayout.HelpBox("키를 만들면 미리보기 몸을 끌어 관절을 돌리거나 값을 바꿔 고칠 수 있어요. 지금 시간에 키가 없으면 끌 때 저절로 생겨요.", MessageType.None); return; }

        EditorGUI.BeginChangeCheck();
        var lp = EditorGUILayout.Toggle(new GUIContent("마지막 키 뒤 반복", "끄면 마지막 자세 유지"), mwork.motion.loop);
        float lf = mwork.motion.loop ? EditorGUILayout.FloatField("반복 시작 (초)", mwork.motion.loopFrom) : mwork.motion.loopFrom;
        if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(mwork, "모션 반복"); mwork.motion.loop = lp; mwork.motion.loopFrom = Mathf.Max(0, lf); Rebuild(); }

        motionScroll = EditorGUILayout.BeginScrollView(motionScroll, GUILayout.MaxHeight(130));
        for (int i = 0; i < ks.Count; i++)
        {
            var st = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft };
            if (i == selKey) st.normal.textColor = st.hover.textColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button($"키 {i + 1}   {ks[i].t:0.00}s{(ks[i].t < a.windup ? "  준비" : ks[i].t < End ? "" : "  끝 뒤")}", st)) { selKey = i; mt = ks[i].t; mplay = false; Rebuild(); }
        }
        EditorGUILayout.EndScrollView();
        if (selKey < 0 || selKey >= ks.Count) return;

        var k = ks[selKey];
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"키 {selKey + 1}", EditorStyles.boldLabel, GUILayout.Width(50));
        if (GUILayout.Button("복제", GUILayout.Width(44))) { Undo.RecordObject(mwork, "키 복제"); var c = MonsterRigSave.Clone(k); c.t = Mathf.Round((k.t + 0.1f) * 100) / 100; ks.Add(c); ks.Sort((x, y) => x.t.CompareTo(y.t)); selKey = ks.IndexOf(c); mt = c.t; Rebuild(); GUIUtility.ExitGUI(); }
        if (GUILayout.Button("삭제", GUILayout.Width(44))) { Undo.RecordObject(mwork, "키 삭제"); ks.RemoveAt(selKey); selKey = -1; Rebuild(); GUIUtility.ExitGUI(); }
        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginChangeCheck();
        var copy = MonsterRigSave.Clone(k);
        copy.t = EditorGUILayout.Slider("시간 (초)", copy.t, 0, Mathf.Max(Length, copy.t));
        switch (cur.kind)
        {
            case Kind.Cat:
                copy.head = Deg("머리", copy.head); copy.front = Deg("앞다리", copy.front); copy.back = Deg("뒷다리", copy.back);
                copy.farFront = Deg("먼 앞다리", copy.farFront); copy.farBack = Deg("먼 뒷다리", copy.farBack); copy.tail = Deg("꼬리", copy.tail); copy.tilt = Deg("몸 기울기", copy.tilt);
                copy.bob = EditorGUILayout.Slider("몸 내림 (px)", copy.bob, -60, 60); copy.headX = EditorGUILayout.Slider("머리 앞뒤 (px)", copy.headX, -60, 60);
                copy.sx = EditorGUILayout.Slider("가로 늘임", copy.sx, 0.5f, 1.5f); copy.sy = EditorGUILayout.Slider("세로 늘임", copy.sy, 0.5f, 1.5f);
                copy.head2 = EditorGUILayout.Toggle("두 번째 머리 (head2)", copy.head2);
                break;
            case Kind.Human:
                copy.armN = Deg("가까운 팔", copy.armN); copy.armF = Deg("먼 팔", copy.armF); copy.legN = Deg("가까운 다리", copy.legN); copy.legF = Deg("먼 다리", copy.legF);
                copy.head = Deg("머리", copy.head); copy.lean = Deg("몸 기울기", copy.lean);
                copy.bob = EditorGUILayout.Slider("몸 들썩 (px)", copy.bob, -60, 60);
                copy.sx = EditorGUILayout.Slider("가로 늘임", copy.sx, 0.5f, 1.5f); copy.sy = EditorGUILayout.Slider("세로 늘임", copy.sy, 0.5f, 1.5f);
                copy.angry = EditorGUILayout.Toggle("화난 머리", copy.angry); copy.scared = EditorGUILayout.Toggle("겁먹은 머리", copy.scared);
                break;
            default:
                copy.alt = EditorGUILayout.Toggle("두 번째 그림 (공격 그림)", copy.alt);
                EditorGUILayout.LabelField("파츠마다 더하는 각도 (°)", EditorStyles.miniBoldLabel);
                foreach (var p in work.part.parts)
                {
                    var pa = copy.parts.Find(x => x.part == p.name);
                    float v = EditorGUILayout.Slider(p.name, pa != null ? pa.angle : 0, -180, 180);
                    if (pa == null && Mathf.Abs(v) > 0.001f) copy.parts.Add(pa = new BossMotion.PartAngle { part = p.name });
                    if (pa != null) pa.angle = v;
                }
                copy.parts.RemoveAll(x => Mathf.Abs(x.angle) < 0.001f);
                break;
        }
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(mwork, "모션 키");
            ks[selKey] = copy; ks.Sort((x, y) => x.t.CompareTo(y.t)); selKey = ks.IndexOf(copy); mt = copy.t;
            Rebuild();
        }
        EditorGUILayout.HelpBox("미리보기에서 몸을 끌면 지금 시간 키의 관절이 돌아가요 (고양이 몸통 = 몸 기울기). 파란 겹침 = 게임 코드 기본 모션. 효과(투사체 · 소환)는 '게임에서 보기' 로 확인.", MessageType.None);
    }

    static float Deg(string label, float rad) => EditorGUILayout.Slider(label + " (°)", rad * Mathf.Rad2Deg, -200, 200) * Mathf.Deg2Rad;

    // ── 반영 ──
    string SaveMotion()
    {
        var asset = AssetDatabase.LoadAssetAtPath<BossMotion>(MotionAsset);
        if (!asset) { Directory.CreateDirectory(Path.GetDirectoryName(MotionAsset)); asset = CreateInstance<BossMotion>(); AssetDatabase.CreateAsset(asset, MotionAsset); }
        Undo.RecordObject(asset, "모션 반영");
        asset.motions.RemoveAll(x => x.codeId == cur.code && x.atkType == curMove.type);
        string msg;
        if (mwork.motion.keys.Count > 0)
        {
            var m = MonsterRigSave.Clone(mwork.motion); m.codeId = cur.code; m.atkType = curMove.type;
            asset.motions.Add(m); msg = $"{curMove.type} 모션 키 {m.keys.Count}개";
        }
        else msg = $"{curMove.type} 모션 지움 (코드 기본으로)";
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        BossMotion.Reload();
        msavedJson = EditorJsonUtility.ToJson(mwork);
        return msg + " → Resources/BossMotion.asset";
    }
}

// '게임에서 보기': Game 씬을 플레이해서 이 보스를 테스트 소환 → 이 기술 바로 발동 (UltTestPanel 과 같은 Boss.TestSpawn · ForceAttack). 플레이를 멈추면 원래 씬으로
[InitializeOnLoad]
public static class MonsterRigPlayTest
{
    const string Key = "nkk_rigtool_test", Back = "nkk_rigtool_back";
    static double startAt;

    static MonsterRigPlayTest()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetString(Key, "") != "") { startAt = EditorApplication.timeSinceStartup; EditorApplication.update += Wait; }
            if (s == PlayModeStateChange.EnteredEditMode)
            {
                var back = SessionState.GetString(Back, "");
                SessionState.EraseString(Key); SessionState.EraseString(Back);
                if (back != "" && File.Exists(back)) EditorSceneManager.OpenScene(back);
            }
        };
    }

    public static void Run(string code, string atkType)
    {
        SessionState.SetString(Key, code + "|" + atkType);
        if (EditorApplication.isPlaying) { startAt = EditorApplication.timeSinceStartup - 10; EditorApplication.update -= Wait; EditorApplication.update += Wait; return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { SessionState.EraseString(Key); return; }
        var active = EditorSceneManager.GetActiveScene().path;
        if (active != "Assets/Scenes/Game.unity") { SessionState.SetString(Back, active); EditorSceneManager.OpenScene("Assets/Scenes/Game.unity"); }
        EditorApplication.isPlaying = true;
    }

    static void Wait()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Wait; return; }
        if (EditorApplication.timeSinceStartup - startAt < 1.5) return;          // 게임이 다 켜질 때까지
        var boss = UnityEngine.Object.FindFirstObjectByType<Boss>();
        var db = GameDatabase.Instance;
        if (!boss || db == null || db.Bosses == null || db.Bosses.Count == 0) { if (EditorApplication.timeSinceStartup - startAt > 15) EditorApplication.update -= Wait; return; }
        EditorApplication.update -= Wait;
        var a = SessionState.GetString(Key, "").Split('|'); if (a.Length < 2) return;
        int idx = db.Bosses.FindIndex(b => b.code_id == a[0]);
        if (idx < 0) { Debug.LogWarning("[리깅 툴] 보스 표에 없음: " + a[0]); return; }
        if (boss.State != Boss.BState.Fight || boss.Data != db.Bosses[idx]) boss.TestSpawn(idx);
        boss.hp = boss.hpMax = 1e12f;          // 구경용: 쥐들이 바로 잡지 않게 (테스트 보스라 기록 · 보상 없음)
        bool ok = boss.ForceAttack(a[1]);
        Debug.Log($"[리깅 툴] 게임에서 보기: {a[0]} · {a[1]} {(ok ? "발동" : "발동 못 함 (보스가 싸우는 중이 아님)")}");
    }
}
