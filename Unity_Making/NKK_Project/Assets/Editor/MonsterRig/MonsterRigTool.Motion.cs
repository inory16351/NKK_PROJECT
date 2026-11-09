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
// 일반 고양이 · 쥐 (2026-11-09 사용자): 같은 방식 · 같은 에셋. 고양이 = 걷기 · 덮치기 · 스킬 · 무리 스킬 / 쥐 = 걷기 · 깨물기 · 특수 액션 · 필살기 · 묘기 5종 (모션 이름 = BossMotion.CatXxx · RatXxx).
public partial class MonsterRigTool
{
    const string MotionAsset = "Assets/Resources/BossMotion.asset";
    class Move { public string kind, type, play, formHead; public BossAtkRow atk; public RatSkillRow ratAct; }      // type = 저장 이름 · play = '게임에서 보기' 때 발동할 기술 (변신 모션 = 변신 패턴>그 기술, 비면 type) · formHead = 변신 머리 (공격이 끝난 뒤에도)
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
        if (cur != null && !cur.boss) { CreatureMoves(); PickMove(moves.Find(m => !IsLoopType(m.type)) ?? (moves.Count > 0 ? moves[0] : null)); return; }
        var r = cur?.row; if (r == null) { curMove = null; return; }
        var slots = new[] { ("일반 공격", r.atk_type), ("두 번째 공격", r.atk2_type), ("필살기 (쿨타임 스킬)", r.skill_type), ("발악 1", r.special1_type), ("발악 2", r.special2_type) };
        // 걷기 (기술이 아님): 시간 0~1 = 걸음 두 번, 반복. 게임은 걸음 위상(속도에 따라 빨라짐)으로 이 시간을 고름
        moves.Add(new Move { kind = "걷기", type = BossMotion.WalkType, atk = new BossAtkRow { atk_type = BossMotion.WalkType, atk_name = "걸음 두 번 (반복)", windup = 0, dur = 1, opt = "" } });
        foreach (var (k, t) in slots) if (!string.IsNullOrEmpty(t) && t != "None" && atkRows.TryGetValue(t, out var a) && !moves.Exists(m => m.type == t)) moves.Add(new Move { kind = k, type = t, atk = a });
        // 변신 (opt head_keep: 그 패턴 뒤로 머리가 바뀐 채): 변신 형태의 걷기 · 대기 (2026-11-06 사용자: 실험체 고양이 '너.. 봤구나?' 뒤)
        foreach (var m in moves.ToArray())
        {
            var hk = OptValue(m.atk?.opt, "head_keep"); if (hk == null || moves.Exists(x => x.type == BossMotion.FormWalkType)) continue;
            string src = string.IsNullOrEmpty(m.atk.atk_name) ? m.type : m.atk.atk_name;
            moves.Insert(1, new Move { kind = "변신 대기", type = BossMotion.FormIdleType, play = m.type, atk = new BossAtkRow { atk_type = BossMotion.FormIdleType, atk_name = src + " 뒤 · 초 (반복)", windup = 0, dur = BossMotion.IdleLength, opt = "head_keep:" + hk } });
            moves.Insert(1, new Move { kind = "변신 걷기", type = BossMotion.FormWalkType, play = m.type, atk = new BossAtkRow { atk_type = BossMotion.FormWalkType, atk_name = src + " 뒤 · 걸음 두 번 (반복)", windup = 0, dur = 1, opt = "head_keep:" + hk } });
            moves[1].formHead = moves[2].formHead = hk;
            // 변신 뒤 공격 (2026-11-06 사용자): 기술마다 '변신 · ' 칸. 게임 = 변신한 뒤에 시작한 공격이면 이 모션 (없으면 평소 모션)
            foreach (var am in moves.ToArray())
            {
                if (IsLoopType(am.type) || am.formHead != null) continue;
                var fa = MonsterRigSave.Clone(am.atk);
                if (OptValue(fa.opt, "head") == null && OptValue(fa.opt, "head_keep") == null) fa.opt = "head_keep:" + hk + (string.IsNullOrEmpty(fa.opt) ? "" : "," + fa.opt);      // 기술 머리가 없으면 변신 머리
                moves.Add(new Move { kind = "변신 · " + am.kind, type = BossMotion.FormAtk(am.type), play = m.type + ">" + am.type, formHead = hk, atk = fa });
            }
        }
        PickMove(moves.Find(m => !IsLoopType(m.type)) ?? (moves.Count > 0 ? moves[0] : null));      // 처음엔 일반 공격 (걷기 · 변신 모션은 위)
    }

    // 일반 고양이 · 쥐의 동작 목록 (길이 = 게임 값, 스킬 표에서)
    void CreatureMoves()
    {
        Move Add(string kind, string type, string name, float dur) { var m = new Move { kind = kind, type = type, atk = new BossAtkRow { atk_type = type, atk_name = name, windup = 0, dur = dur, opt = "" } }; moves.Add(m); return m; }
        if (cur.kind == Kind.Cat)
        {
            var row = cur.catRow;
            Add("걷기", BossMotion.CatWalk, "걸음 두 번 (반복)", 1);
            Add("공격", BossMotion.CatPounce, "덮치기", 0.35f);
            if (catSkills.TryGetValue(row.skill, out var s))
            {
                Add("스킬", BossMotion.CatCast, s.skill_name, 0.5f);
                if (s.Effect == CatEffectType.Roll_Charge) Add("스킬 구르기", BossMotion.CatRoll, s.skill_name, s.value_01);
            }
            if (catSkills.TryGetValue(row.crowd_skill, out var c))
            {
                var e = c.Effect;
                Add("무리 스킬 웅크림", BossMotion.CatAim, c.skill_name, Mathf.Max(0.15f, c.value_03));
                if (e == CatEffectType.Crowd_Slam || e == CatEffectType.Crowd_Combo || e == CatEffectType.Crowd_Pinpoint || e == CatEffectType.Crowd_Belly || e == CatEffectType.Crowd_Quake)
                    Add("무리 스킬 도약", BossMotion.CatLeap, c.skill_name, (e == CatEffectType.Crowd_Slam || e == CatEffectType.Crowd_Pinpoint || e == CatEffectType.Crowd_Belly) && c.value_04 > 0 ? c.value_04 : 0.35f);      // 기본 체공 = CatManager.slamAir
                if (e == CatEffectType.Crowd_Roll && !moves.Exists(m => m.type == BossMotion.CatRoll)) Add("무리 스킬 구르기", BossMotion.CatRoll, c.skill_name, 1);      // 실제 길이 = 거리 따라
            }
            return;
        }
        var rr = cur.ratRow;
        Add("걷기", BossMotion.RatWalk, "걸음 두 번 (반복)", 1);
        Add("공격", BossMotion.RatBite, "깨물기", 0.2f);
        if (ratSkills.TryGetValue(rr.action_skill, out var a)) Add("특수 액션", BossMotion.RatAct, a.skill_name, Mathf.Max(0.1f, a.duration)).ratAct = a;
        if (ratUlts.TryGetValue(rr.ultimate, out var u)) Add("필살기", BossMotion.RatUlt, u.ultimate_name, UltLength(u));
        foreach (var (t, n) in new[] { (Rat.TrickType.Flip, "백덤블링"), (Rat.TrickType.Windmill, "윈드밀"), (Rat.TrickType.Axel, "트리플 악셀"), (Rat.TrickType.Cannon, "쥐 대포알"), (Rat.TrickType.Wheel, "쳇바퀴 돌기") })
            Add("묘기", Rat.TrickMove(t), n, Rat.TrickDuration(t));
    }

    // 필살기 길이: 그 상황극 클래스의 Dur (게임 상태를 읽어 못 구하면 기본 6초)
    static float UltLength(RatUltimateRow u)
    {
        try
        {
            var t = typeof(NKK.Ults.UltBase).Assembly.GetType("NKK.Ults." + u.script);
            return t != null ? Mathf.Max(0.1f, ((NKK.Ults.UltBase)Activator.CreateInstance(t)).Dur) : 6;
        }
        catch { return 6; }
    }

    // 일반 고양이 · 쥐의 코드 자세 (게임과 같은 함수). 필살기는 상황극이 매 프레임 정해서 미리 볼 수 없음 → 대기 자세
    RatRig.Pose CodeCreature(float t)
    {
        string ty = curMove.type;
        bool on = IsLoop || t < End;
        if (cur.kind == Kind.Cat)
        {
            var c = Cat.IdlePose(t);
            if (on) Cat.MovePose(ty, WalkPhaseAt(t), t, ref c);
            return c;
        }
        var q = new RatRig.Pose { head = Mathf.Sin(t * 1.7f) * 0.05f, tail = 0.15f + Mathf.Sin(t * 2.6f) * 0.15f, sx = 1, sy = 1 };
        if (!on) return q;
        if (ty == BossMotion.RatWalk) RatWalk(ref q, WalkPhaseAt(t));
        else if (ty == BossMotion.RatBite) Rat.BitePose(Mathf.Max(0, 1 - t * 5), ref q);
        else if (ty == BossMotion.RatAct && curMove.ratAct != null)
        {
            var a = curMove.ratAct; var e = a.Effect;
            float dur = Mathf.Max(0.01f, a.duration), k = t / dur, z = 0, bite = 0; bool held = false;
            if (e == EffectType.Jump_Slam) { int hops = Mathf.Max(1, Mathf.RoundToInt(a.V(1))); z = Mathf.Sin((k * hops) % 1 * Mathf.PI) * 75; }
            else if (e == EffectType.Barrage) { float gap = dur * 0.85f / Mathf.Max(1, a.V(1)); bite = Mathf.Max(0, 1 - (t % gap) * 5); }
            else if (e == EffectType.Throw_Item) { float cyc = dur / Mathf.Max(1, a.V(1)); held = (t % cyc) / cyc <= 0.62f; }
            Rat.ActionPoseOf(e, t, z, bite, held, t * 30, k > 0.5f, ref q);
        }
        else if (ty.StartsWith(BossMotion.RatTrickPrefix))
            Rat.TrickPoseOf((Rat.TrickType)Enum.Parse(typeof(Rat.TrickType), ty.Substring(BossMotion.RatTrickPrefix.Length)), t, Mathf.Min(1, t / Mathf.Max(0.01f, A.dur)), ref q);
        return q;
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
    string AT => A?.atk_type;      // 원래 기술 (변신 공격도 같은 기술 자세 · 점프)
    bool Jump => curMove != null && Boss.IsJumpAttack(AT);
    float AirT => Jump ? 2 * (AT == "Pounce" ? pounceV : jumpV) / Mathf.Max(1, gravity) : 0;
    float End => A == null ? 0 : A.windup + Mathf.Max(A.dur, AirT);           // 공격이 끝나는 때 (대략: 덮치기는 착지 때 끝남)
    bool IsWalk => curMove != null && BossMotion.IsWalkMotion(curMove.type);
    bool IsIdle => curMove != null && curMove.type == BossMotion.FormIdleType;      // 대기 (초, 반복)
    bool IsLoop => IsWalk || IsIdle;      // 기술이 아닌 반복 모션: 시간 전체가 '모션 중'
    static bool IsLoopType(string t) => BossMotion.IsWalkMotion(t) || t == BossMotion.FormIdleType;
    bool InMove(float t) => IsLoop || t < End;
    float WalkPhaseAt(float t) => t * BossMotion.WalkPeriod;
    float Length => IsWalk ? 1 : IsIdle ? Mathf.Max(BossMotion.IdleLength, HasKeys ? mwork.motion.Length : 0) : Mathf.Max(End, mwork && mwork.motion.keys.Count > 0 ? mwork.motion.Length : 0) + 0.5f;
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

    // 관절 옮기기 · 돌리기 · 크기 · 순서를 어디에 적용할지 (모션 화면 위 드롭다운, 기억)
    enum Scope { Key, Motion, All }
    static readonly string[] ScopeNames = { "이 키 (프레임)만", "이 모션의 모든 키", "전체 (모든 모션 · 평소 모습)" };
    const string ScopePref = "nkk_rigtool_scope";
    Scope scope { get => (Scope)EditorPrefs.GetInt(ScopePref, 0); set => EditorPrefs.SetInt(ScopePref, (int)value); }

    void MotionToolbar()
    {
        if (curMove == null) { GUILayout.Label("이 보스는 기술이 없어요"); return; }
        GUILayout.Label("적용", GUILayout.Width(28));
        var sc = (Scope)EditorGUILayout.Popup((int)scope, ScopeNames, EditorStyles.toolbarPopup, GUILayout.Width(180));
        if (sc != scope) { scope = sc; status = "적용 범위: " + ScopeNames[(int)sc]; }
        if (GUILayout.Button(mplay ? "❚❚" : "▶", EditorStyles.toolbarButton, GUILayout.Width(28))) { mplay = !mplay; mlast = EditorApplication.timeSinceStartup; if (mplay && mt >= Length - 0.01f) mt = 0; }
        mrepeat = GUILayout.Toggle(mrepeat, "반복", EditorStyles.toolbarButton, GUILayout.Width(36));
        GUILayout.Label($"{mt:0.00}s", GUILayout.Width(44));
        mt = GUILayout.HorizontalSlider(mt, 0, Length, GUILayout.MinWidth(160));
        GUILayout.Label(IsWalk ? "걷기" : IsIdle ? "대기" : mt < A.windup ? "준비" : mt < End ? (Up(mt) ? "공중" : "공격") : "끝", GUILayout.Width(32));
        showGhost = GUILayout.Toggle(showGhost, "코드 모션 겹치기", EditorStyles.toolbarButton, GUILayout.Width(100));
        if (GUILayout.Button("화면 맞춤", EditorStyles.toolbarButton, GUILayout.Width(60))) Fit();
    }

    // ── 자세 (미리보기) ──
    RatRig.Pose CodeCat(float t, out bool head2)
    {
        if (!cur.boss) { head2 = false; return CodeCreature(t); }
        var p = new RatRig.Pose { head = Mathf.Sin(t * 1.6f) * 0.06f, tail = 0.1f + Mathf.Sin(t * 3) * 0.25f, sx = 1, sy = 1 };
        if (IsWalk) { p.head = 0; Boss.CatWalkPose(WalkPhaseAt(t), ref p); head2 = false; return p; }
        if (IsIdle) { head2 = false; return p; }      // 대기 = 게임 코드 자세 (머리 · 꼬리 살랑)
        head2 = t < End;
        if (t < End) Boss.CatAttackPose(AT, A, t, Up(t), t, ref p);
        return p;
    }
    HumanRig.Pose CodeHuman(float t)
    {
        var p = new HumanRig.Pose { sx = 1, sy = 1 };
        if (IsWalk) { Boss.HumanWalkPose(WalkPhaseAt(t), ref p); return p; }
        if (IsIdle) return p;
        if (t < End) Boss.HumanAttackPose(AT, A, t, Up(t), ref p);
        return p;
    }

    RatRig.Pose MotionCat(RatRig rr, RatArtLibrary.Entry e, bool code)
    {
        bool h2; RatRig.Pose p;
        if (!code && HasKeys && InMove(mt)) p = BossMotion.Cat(mwork.motion, mt, out h2); else p = CodeCat(mt, out h2);
        var opt = InMove(mt) ? OptHead() : null;      // opt head:그림 (게임: ReworkHead 가 먼저) · 변신 모션 = 변신 머리
        if (!opt && curMove?.formHead != null) opt = MemeSprite(curMove.formHead);      // 변신 공격이 끝난 뒤 = 변신 머리 그대로
        rr.headReplace = opt ? opt : h2 ? e.head2 : null;
        return p;
    }
    HumanRig.Pose MotionHuman(bool code) => !code && HasKeys && InMove(mt) ? BossMotion.Human(mwork.motion, mt) : CodeHuman(mt);
    // 지금 시간 모션 키의 파츠 위치 · 크기 (코드 모션 · 공격 끝 뒤 = 없음)
    List<BossMotion.PartXf> MotionXf(bool code) => !code && HasKeys && InMove(mt) ? BossMotion.Xf(mwork.motion, mt) : null;

    // 지금 시간 모션 키의 그리는 순서 (코드 모션 · 공격 끝 뒤 = 없음)
    List<PartOrder> MotionOrders(bool code) => !code && HasKeys && InMove(mt) ? BossMotion.Orders(mwork.motion, mt) : null;

    bool MotionPart(BossPartRig pr, bool code)
    {
        pr.extra.Clear(); pr.orderOverride.Clear(); pr.xf.Clear();
        var ol = MotionOrders(code); if (ol != null) foreach (var o in ol) pr.orderOverride[o.part] = o.order;
        var xl = MotionXf(code); if (xl != null) foreach (var x in xl) pr.xf[x.part] = (x.offset, x.scale);
        if (!InMove(mt)) return false;
        if (!code && HasKeys) { bool al = BossMotion.Part(mwork.motion, mt, pr.extra); return al && !IsLoop; }      // 걷기 · 대기 = 두 번째 그림 안 씀 (게임과 같게)
        return !IsLoop;
    }

    // 점프 공격: 몸을 띄움 (게임 z)
    void Lift(GameObject o)
    {
        if (!Up(mt)) return;
        float tau = mt - A.windup, v = AT == "Pounce" ? pounceV : jumpV;
        float z = Mathf.Max(0, v * tau - gravity * tau * tau / 2);
        o.transform.position += new Vector3(0, z * NKK.World.U, 0);
    }

    // opt 의 key:값 (쉼표로 나눔). 없으면 null
    static string OptValue(string opt, string key)
    {
        if (string.IsNullOrEmpty(opt)) return null;
        foreach (var part in opt.Split(',')) { var q = part.Trim(); if (q.StartsWith(key + ":")) return q.Substring(key.Length + 1); }
        return null;
    }

    // opt 의 head:그림 (보스 밈 그림 이름) → 스프라이트
    Sprite OptHead()
    {
        var opt = A?.opt; if (string.IsNullOrEmpty(opt)) return null;
        var h = OptValue(opt, "head") ?? OptValue(opt, "head_keep");      // 기술 머리가 먼저 (게임 ReworkHead 와 같게)
        return h == null ? null : MemeSprite(h);
    }
    static Sprite MemeSprite(string n)      // 보스 밈 그림 이름 → 스프라이트
    {
        foreach (var g in AssetDatabase.FindAssets(n + " t:Sprite", new[] { "Assets/Art" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(path) == n) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    // ── 키 ──
    BossMotion.Key KeyNow()
    {
        var k = new BossMotion.Key { t = Mathf.Round(mt * 100) / 100 };
        // 순서는 앞 키에서 저절로 이어지므로 새 키에는 안 넣음 · 위치 · 크기는 지금 모습 그대로
        if (HasKeys) { var xl = BossMotion.Xf(mwork.motion, mt); if (xl != null) foreach (var x in xl) k.xf.Add(new BossMotion.PartXf { part = x.part, offset = x.offset, scale = x.scale }); }
        switch (cur.kind)
        {
            case Kind.Cat:
            case Kind.Rat:
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
                k.alt = !IsLoop && (HasKeys ? BossMotion.Part(mwork.motion, mt, d) : mt < End);
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
        var ts = new List<float>();
        if (IsWalk) { for (int i = 0; i <= 8; i++) ts.Add(i / 8f); mwork.motion.loop = true; mwork.motion.loopFrom = 0; }      // 걷기: 1/8 마다 (끝 = 처음과 같은 자세 → 이어짐)
        else if (IsIdle) { for (float t = 0; t < BossMotion.IdleLength - 0.01f; t += 0.5f) ts.Add(t); mwork.motion.loop = true; mwork.motion.loopFrom = 0; }      // 대기: 0.5초 마다, 끝 키 = 첫 키 (아래)
        else if (curMove.type == BossMotion.RatUlt) ts.AddRange(new[] { 0, A.dur * 0.5f, Mathf.Max(0, A.dur - 0.02f) });      // 필살기: 코드 자세를 미리 볼 수 없어 대기 자세로 뼈대만
        else
        {
            ts.AddRange(new[] { 0, Mathf.Max(0, A.windup - 0.02f), A.windup });
            for (float t = A.windup + 0.2f; t < End - 0.05f; t += 0.2f) ts.Add(t);
            ts.Add(Mathf.Max(A.windup, End - 0.02f));
        }
        var made = new List<BossMotion.Key>();          // 키가 다 생긴 뒤에 넣음 (넣는 도중엔 KeyNow 가 코드 모션을 읽게)
        foreach (var t in ts) { mt = t; var k = KeyNow(); k.t = Mathf.Round(t * 100) / 100; if (!made.Exists(x => Mathf.Abs(x.t - k.t) < 0.005f)) made.Add(k); }
        made.Sort((a, b) => a.t.CompareTo(b.t));
        if (IsIdle && made.Count > 0) { var last = MonsterRigSave.Clone(made[0]); last.t = BossMotion.IdleLength; made.Add(last); }      // 한 바퀴 끝 = 처음 자세 → 끊김 없이 돎
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
        sel = cur.kind == Kind.Part ? r.transform.parent.name : partOf.TryGetValue(r, out var pn) ? pn : null;      // 옮기기 · 크기 · ] [ 순서 바꿀 파츠
        if (motionField == null && sel == null) return false;
        if (scope == Scope.All) return true;      // 전체 = 리그 기본값을 고침 (키 안 만듦)
        if (!HasKeys) KeysFromCode();      // 키가 없으면 코드 모션을 먼저 키로 (키 하나만 생겨 모션이 한 자세로 굳지 않게)
        if (scope == Scope.Key) selKey = AddKeyNow();
        return true;
    }

    // 끌어서 옮기기 · 크기 (지금 시간 키의 그 파츠). 고양이 · 사람 = 렌더러 하나, 파츠 보스 = 그 파츠 (자식도 같이)
    // 범위 안의 키 (이 키 = 고른 키 · 이 모션 = 모든 키)
    List<BossMotion.Key> ScopeKeys()
    {
        var l = new List<BossMotion.Key>(); var ks = mwork.motion.keys;
        if (scope == Scope.Motion) l.AddRange(ks);
        else if (scope == Scope.Key && selKey >= 0 && selKey < ks.Count) l.Add(ks[selKey]);
        return l;
    }
    static BossMotion.PartXf XfOf(BossMotion.Key k, string part)
    {
        var x = k.xf.Find(v => v.part == part); if (x == null) k.xf.Add(x = new BossMotion.PartXf { part = part, offset = Vector2.zero, scale = 1 });
        return x;
    }
    void MotionMove(Vector3 wd)
    {
        if (scope == Scope.All) { DragBy(wd, false); return; }      // 리그: 파츠 보스 = 붙는 자리, 고양이 · 사람 = 그 파츠 위치
        var sr = cur.kind == Kind.Part ? Renderer(sel) : RigRenderer(sel); if (!sr || string.IsNullOrEmpty(sel)) return;
        var t = cur.kind == Kind.Part ? sr.transform.parent : sr.transform;      // 파츠 보스 = 관절
        Undo.RecordObject(mwork, "모션 옮기기");
        var d = t.parent.InverseTransformVector(wd) * 100;
        foreach (var k in ScopeKeys()) { var x = XfOf(k, sel); x.offset = new Vector2(Mathf.Round((x.offset.x + d.x) * 10) / 10, Mathf.Round((x.offset.y + d.y) * 10) / 10); }
    }
    void MotionScale(float f)
    {
        if (scope == Scope.All) { Resize(f); return; }
        if (string.IsNullOrEmpty(sel)) return;
        Undo.RecordObject(mwork, "모션 크기");
        foreach (var k in ScopeKeys()) { var x = XfOf(k, sel); x.scale = Mathf.Clamp(Mathf.Round(x.scale * f * 1000) / 1000, 0.05f, 10); }
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
        if (scope == Scope.All) { Rotate(deg); return; }      // 리그 기본 각도 (고양이 · 사람 = 그 파츠, 파츠 보스 = angle)
        Undo.RecordObject(mwork, "모션 돌리기");
        foreach (var k in ScopeKeys()) RotateKey(k, deg);
    }
    void RotateKey(BossMotion.Key k, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
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
    // 키 목록 칸 높이 (손잡이로 조절, EditorPrefs 에 기억)
    const string KeyListPref = "nkk_rigtool_keylist_h";
    float KeyListH { get => EditorPrefs.GetFloat(KeyListPref, 260); set => EditorPrefs.SetFloat(KeyListPref, Mathf.Clamp(value, 60, 900)); }
    bool keyListDrag;
    void KeyListHandle()
    {
        var r = GUILayoutUtility.GetRect(10, 12, GUILayout.ExpandWidth(true));
        EditorGUIUtility.AddCursorRect(r, MouseCursor.ResizeVertical);
        var c = new Color(1, 1, 1, keyListDrag ? 0.6f : 0.3f);
        EditorGUI.DrawRect(new Rect(r.center.x - 20, r.center.y - 2, 40, 1), c);
        EditorGUI.DrawRect(new Rect(r.center.x - 20, r.center.y + 1, 40, 1), c);
        var e = Event.current; int id = GUIUtility.GetControlID(FocusType.Passive);
        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown when e.button == 0 && r.Contains(e.mousePosition): GUIUtility.hotControl = id; keyListDrag = true; e.Use(); break;
            case EventType.MouseDrag when GUIUtility.hotControl == id: KeyListH = KeyListH + e.delta.y; e.Use(); Repaint(); break;
            case EventType.MouseUp when GUIUtility.hotControl == id: GUIUtility.hotControl = 0; keyListDrag = false; e.Use(); break;
        }
    }

    void InspectMotion()
    {
        if (curMove == null) { EditorGUILayout.HelpBox("이 몬스터는 기술 칸이 없어요.", MessageType.Info); return; }
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
        if (IsWalk) EditorGUILayout.LabelField((curMove.type == BossMotion.FormWalkType ? "변신 뒤 " : "") + "걷기 · 시간 0~1 = 걸음 두 번 (반복) · 게임은 걷는 빠르기에 맞춰 이 시간을 돎" + (curMove.type == BossMotion.FormWalkType ? " · 없으면 평소 걷기" : ""), EditorStyles.miniLabel);
        else if (IsIdle) EditorGUILayout.LabelField("변신 뒤 대기 (서 있을 때) · 초 · 변신한 때부터 '마지막 키 뒤 반복' 으로 돎 · 코드 모션을 키로 = " + BossMotion.IdleLength.ToString("0") + "초 한 바퀴 · 없으면 코드 대기 자세", EditorStyles.miniLabel);
        else EditorGUILayout.LabelField($"{(curMove.formHead != null ? "변신 뒤 " : "")}{AT} · 준비 {a.windup:0.##}s · 지속 {a.dur:0.##}s{(Jump ? $" · 점프 {AirT:0.##}s" : "")}{(string.IsNullOrEmpty(a.pattern) ? "" : " · " + a.pattern)}", EditorStyles.miniLabel);
        if (!string.IsNullOrEmpty(a.opt)) EditorGUILayout.LabelField("opt: " + a.opt, EditorStyles.miniLabel);
        if (curMove.type == BossMotion.RatUlt) EditorGUILayout.HelpBox("필살기의 코드 자세는 상황극이 매 프레임 정해서 여기서 미리 볼 수 없어요 (겹쳐 보기 · '코드 모션을 키로' = 대기 자세). 키를 만들면 상황극 내내 코드 자세 대신 키를 써요. 소품 · 이동은 '게임에서 보기' 로 확인.", MessageType.None);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("▶ 게임에서 보기 (플레이 모드로 이 기술 발동)", GUILayout.Height(24))) MonsterRigPlayTest.Run(cur.code, curMove.play ?? curMove.type);      // 변신 모션 = 변신 패턴을 발동 (그 뒤로 변신 형태로 걷고 섬)

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

        // 키 목록: 줄을 크게 · 아래 손잡이를 끌어 칸 높이 조절 (높이는 기억)
        const float RowH = 24;
        float listH = Mathf.Min(KeyListH, ks.Count * (RowH + 2) + 6);
        motionScroll = EditorGUILayout.BeginScrollView(motionScroll, GUILayout.Height(listH));
        for (int i = 0; i < ks.Count; i++)
        {
            var st = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, fontSize = 12, fixedHeight = RowH };
            if (i == selKey) st.normal.textColor = st.hover.textColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button($"키 {i + 1}   {ks[i].t:0.00}s{(IsLoop ? "" : ks[i].t < a.windup ? "  준비" : ks[i].t < End ? "" : "  끝 뒤")}", st, GUILayout.Height(RowH))) { selKey = i; mt = ks[i].t; mplay = false; Rebuild(); }
        }
        EditorGUILayout.EndScrollView();
        KeyListHandle();
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
            case Kind.Rat:
                copy.head = Deg("머리", copy.head);
                if (!work.cat.headOnly) { copy.front = Deg("앞다리", copy.front); copy.back = Deg("뒷다리", copy.back); copy.farFront = Deg("먼 앞다리", copy.farFront); copy.farBack = Deg("먼 뒷다리", copy.farBack); }
                copy.tail = Deg("꼬리", copy.tail); copy.tilt = Deg("몸 기울기", copy.tilt);
                copy.bob = EditorGUILayout.Slider("몸 내림 (px)", copy.bob, -60, 60); copy.headX = EditorGUILayout.Slider("머리 앞뒤 (px)", copy.headX, -60, 60);
                copy.sx = EditorGUILayout.Slider("가로 늘임", copy.sx, 0.5f, 1.5f); copy.sy = EditorGUILayout.Slider("세로 늘임", copy.sy, 0.5f, 1.5f);
                if (work.cat.head2) copy.head2 = EditorGUILayout.Toggle("두 번째 머리 (head2)", copy.head2);
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
        // 파츠 위치 · 크기 (이 키, 키 사이를 이음)
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"파츠 위치 · 크기 (이 키 값 · 고치면 적용: {ScopeNames[(int)scope]} · x y = 픽셀, × = 크기 · 굵게 = 바꾼 값)", EditorStyles.miniBoldLabel);
        xfPending.Clear();
        var xslots = new List<(string, string)>();
        if (cur.kind == Kind.Part) foreach (var p in work.part.parts) xslots.Add((p.name, p.name));
        else xslots.AddRange(RigSlots);
        foreach (var (part, label) in xslots)
        {
            var xv = copy.xf.Find(v => v.part == part);
            var st = new GUIStyle(xv != null ? EditorStyles.boldLabel : EditorStyles.label); if (part == sel) st.normal.textColor = new Color(1f, 0.8f, 0.3f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(label, st, GUILayout.Width(90))) sel = part;
            var o = xv != null ? xv.offset : Vector2.zero; float sc = xv != null ? xv.scale : 1;
            EditorGUIUtility.labelWidth = 14;
            var o2 = new Vector2(EditorGUILayout.FloatField("x", o.x, GUILayout.Width(56)), EditorGUILayout.FloatField("y", o.y, GUILayout.Width(56)));
            EditorGUIUtility.labelWidth = 16;
            float sc2 = EditorGUILayout.FloatField("×", sc, GUILayout.Width(52));
            EditorGUIUtility.labelWidth = 0;
            bool clear = xv != null && GUILayout.Button("기본", EditorStyles.miniButton, GUILayout.Width(36));
            EditorGUILayout.EndHorizontal();
            if (clear) { copy.xf.Remove(xv); GUI.changed = true; }
            else if (o2 != o || Mathf.Abs(sc2 - sc) > 1e-5f)
            {
                // 적용 범위: 이 키만 = 그 값 그대로 · 이 모션 = 모든 키에 같은 만큼 · 전체 = 리그 기본값 (끌기와 같은 규칙)
                if (scope == Scope.Key) { if (xv == null) copy.xf.Add(xv = new BossMotion.PartXf { part = part }); xv.offset = o2; xv.scale = Mathf.Max(0.05f, sc2); }
                else xfPending.Add((part, o2 - o, Mathf.Max(0.05f, sc2) / Mathf.Max(0.05f, sc)));
                GUI.changed = true;
            }
        }

        // 그리는 순서 (이 키부터 다음 키 전까지) — 필살기 도중 레이어 바꾸기
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("그리는 순서 (이 키부터 뒤로 이어짐 · 클수록 앞 · 굵게 = 이 키에서 바꾼 값 · '기본' = 앞 키 값을 따름)", EditorStyles.miniBoldLabel);
        var slots = new List<(string, string)>();
        if (cur.kind == Kind.Part) foreach (var p in work.part.parts) slots.Add((p.name, p.name));
        else slots.AddRange(RigSlots);
        foreach (var (part, label) in slots)
        {
            var ov = copy.orders.Find(x => x.part == part);
            var rr = cur.kind == Kind.Part ? Renderer(part) : RigRenderer(part);
            int now = rr ? rr.sortingOrder : 0, val = ov != null ? ov.order : now, set;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, ov != null ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Width(90));
            set = EditorGUILayout.IntField(val, GUILayout.Width(40));
            if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(24))) set = val + 1;
            if (GUILayout.Button("▼", EditorStyles.miniButtonRight, GUILayout.Width(24))) set = val - 1;
            bool clear = ov != null && GUILayout.Button("기본", EditorStyles.miniButton, GUILayout.Width(36));
            EditorGUILayout.EndHorizontal();
            if (clear) { copy.orders.Remove(ov); GUI.changed = true; }
            else if (set != val) { if (ov == null) copy.orders.Add(ov = new PartOrder { part = part }); ov.order = set; GUI.changed = true; }
        }
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(mwork, "모션 키");
            ks[selKey] = copy; ks.Sort((x, y) => x.t.CompareTo(y.t)); selKey = ks.IndexOf(copy); mt = copy.t;
            ApplyXfPending();
            Rebuild();
        }
        EditorGUILayout.HelpBox("미리보기: 끌기 = 파츠 옮기기 · Ctrl+끌기 = 관절 돌리기 (고양이 몸통 = 몸 기울기) · Alt+위아래 = 크기 · ] [ = 순서. 위 '적용' 드롭다운: 이 키만 (지금 시간에 키가 없으면 저절로 생김) · 이 모션의 모든 키 (같은 만큼 한꺼번에) · 전체 (리그 기본값 = 모든 모션 · 평소 모습). 아래 칸의 숫자는 고른 키 하나 (파츠 위치 · 크기 칸은 고치면 적용 범위를 따름: 이 모션 = 모든 키에 같은 만큼, 전체 = 리그 기본값). 파란 겹침 = 게임 코드 기본 모션. 효과(투사체 · 소환)는 '게임에서 보기' 로 확인.", MessageType.None);
    }

    // 키 칸에서 바꾼 파츠 위치 · 크기를 적용 범위대로 (이 모션 = 모든 키에 같은 만큼 더하고 곱함 · 전체 = 리그 기본값, 키는 그대로)
    readonly List<(string part, Vector2 d, float f)> xfPending = new();
    void ApplyXfPending()
    {
        if (xfPending.Count == 0) return;
        if (scope == Scope.Motion)
            foreach (var (part, d, f) in xfPending)
                foreach (var k in mwork.motion.keys)
                {
                    var x = XfOf(k, part);
                    x.offset = new Vector2(Mathf.Round((x.offset.x + d.x) * 10) / 10, Mathf.Round((x.offset.y + d.y) * 10) / 10);
                    x.scale = Mathf.Clamp(Mathf.Round(x.scale * f * 1000) / 1000, 0.05f, 10);
                }
        else if (scope == Scope.All)
        {
            Undo.RecordObject(work, "리그 위치 · 크기");
            var keep = sel;
            foreach (var (part, d, f) in xfPending)
            {
                sel = part;
                if (Mathf.Abs(f - 1) > 1e-5f) Resize(f);
                if (d != Vector2.zero)
                {
                    var sr = cur.kind == Kind.Part ? Renderer(part) : RigRenderer(part); if (!sr) continue;
                    var t = cur.kind == Kind.Part ? sr.transform.parent : sr.transform;      // MotionMove 와 같은 좌표 (부모 기준 픽셀)
                    DragBy(t.parent.TransformVector(d / 100f), false);
                }
            }
            sel = keep;
        }
        xfPending.Clear();
    }

    static float Deg(string label, float rad) => EditorGUILayout.Slider(label + " (°)", rad * Mathf.Rad2Deg, -200, 200) * Mathf.Deg2Rad;

    // ── 반영 ──
    string SaveMotion()
    {
        if (File.Exists(MotionAsset)) AssetDatabase.ImportAsset(MotionAsset);      // 파일이 밖에서 바뀌었으면 먼저 다시 읽음
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
        AssetDatabase.SaveAssetIfDirty(asset);      // 이 에셋만 (다른 에셋의 옛 메모리 값을 덮어쓰지 않게)
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
    static (Boss boss, string type, double t0, bool began) follow;

    // 변신 패턴이 시작했다가 끝나면 (보스가 다시 공격 안 하는 중) 다음 기술을 발동
    static void Follow()
    {
        var f = follow;
        if (!EditorApplication.isPlaying || !f.boss || EditorApplication.timeSinceStartup - f.t0 > 30) { EditorApplication.update -= Follow; return; }
        if (f.boss.Attacking) { follow.began = true; return; }
        if (!f.began) return;
        EditorApplication.update -= Follow;
        bool ok = f.boss.ForceAttack(f.type);
        Debug.Log($"[리깅 툴] 게임에서 보기: 변신 뒤 {f.type} {(ok ? "발동" : "발동 못 함")}");
    }

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

    // 일반 고양이 · 쥐: 불러서 그 동작을 바로 시작 (쥐 = 없으면 15초짜리 임시 쥐, 기록 · 보상 없음). 게임이 아직 준비 안 됐으면 false
    static bool TestCreature(string code, string move)
    {
        var db = GameDatabase.Instance;
        if (db == null || db.RatsByCode.Count == 0) return false;
        bool ok;
        if (move.StartsWith("Cat:"))
        {
            var cm = UnityEngine.Object.FindFirstObjectByType<CatManager>(); if (!cm) return false;
            CatCharacterRow row = null; foreach (var c in db.Cats.Values) if (c.code_id == code) row = c;
            if (row == null) { Debug.LogWarning("[리깅 툴] 고양이 표에 없음: " + code); return true; }
            if (!cm.Current || cm.Current.codeId != code) { cm.Clear(); if (!cm.Spawn(row)) return false; }
            ok = cm.Current.TestMove(move);
        }
        else
        {
            var um = UnityEngine.Object.FindFirstObjectByType<NKK.Ults.UltimateManager>(); if (!um) return false;
            if (move == BossMotion.RatUlt) { if (!um.TestUlt(code)) return false; ok = true; }
            else
            {
                var r = um.TestRat(code); if (!r) return false;
                if (move == BossMotion.RatAct) ok = r.Action != null && r.StartAction();
                else if (move == BossMotion.RatBite) { r.TestBite(); ok = true; }
                else if (move.StartsWith(BossMotion.RatTrickPrefix)) ok = r.StartTrick((Rat.TrickType)Enum.Parse(typeof(Rat.TrickType), move.Substring(BossMotion.RatTrickPrefix.Length)), 0);
                else ok = true;      // 걷기 = 불러 두기만
            }
        }
        Debug.Log($"[리깅 툴] 게임에서 보기: {code} · {move} {(ok ? "발동" : "발동 못 함")}");
        return true;
    }

    static void Wait()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Wait; return; }
        if (EditorApplication.timeSinceStartup - startAt < 1.5) return;          // 게임이 다 켜질 때까지
        var req = SessionState.GetString(Key, "").Split('|');
        if (req.Length >= 2 && (req[1].StartsWith("Cat:") || req[1].StartsWith("Rat:")))
        {
            if (TestCreature(req[0], req[1]) || EditorApplication.timeSinceStartup - startAt > 15) EditorApplication.update -= Wait;      // 준비가 안 됐으면 15초까지 다시
            return;
        }
        var boss = UnityEngine.Object.FindFirstObjectByType<Boss>();
        var db = GameDatabase.Instance;
        if (!boss || db == null || db.Bosses == null || db.Bosses.Count == 0) { if (EditorApplication.timeSinceStartup - startAt > 15) EditorApplication.update -= Wait; return; }
        EditorApplication.update -= Wait;
        var a = SessionState.GetString(Key, "").Split('|'); if (a.Length < 2) return;
        int idx = db.Bosses.FindIndex(b => b.code_id == a[0]);
        if (idx < 0) { Debug.LogWarning("[리깅 툴] 보스 표에 없음: " + a[0]); return; }
        if (boss.State != Boss.BState.Fight || boss.Data != db.Bosses[idx]) boss.TestSpawn(idx);
        boss.hp = boss.hpMax = 1e12f;          // 구경용: 쥐들이 바로 잡지 않게 (테스트 보스라 기록 · 보상 없음)
        var seq = a[1].Split('>');      // 변신 공격: 변신 패턴 → 끝나면 그 기술
        bool ok = a[1] == BossMotion.WalkType || boss.ForceAttack(seq[0]);      // 걷기 = 불러 두기만 (보스가 걸어 다님)
        if (ok && seq.Length > 1) { follow = (boss, seq[1], EditorApplication.timeSinceStartup, false); EditorApplication.update -= Follow; EditorApplication.update += Follow; }
        Debug.Log($"[리깅 툴] 게임에서 보기: {a[0]} · {a[1]} {(ok ? "발동" : "발동 못 함 (보스가 싸우는 중이 아님)")}");
    }
}
