using System.Collections.Generic;
using NKK.Hazards;
using NKK.Items;
using NKK.Rats;
using NKK.Stage;
using TMPro;
using UnityEngine;

namespace NKK.Ults
{
    // 줴리 · 줴리 감사합니다 (웹게임 thankyou, "제리 감사합니다" 짤 패러디): ① "궁극기 충전 중" 게이지가 99%에서 멈춤 → 뚝 → 폭주 999%
    //   → ② 막상 나온 건 아재개그 팻말 → ③ 갑분싸 → ④ 관객(쥐들 + 앞줄 턱시도 고양이)이 계란·토마토·슬리퍼 투척
    //   → ⑤ 턱시도 줴리는 뻔뻔하게 좌우 번갈아 90도 인사, 점점 빨라짐(인사마다 펑) → ⑥ 마지막 인사 대폭발 → 아이리스 아웃
    // 무대: 어둠(jw_dark 판 + 스프라이트 마스크로 줴리 둘레 타원 구멍, 줴리 몸은 마스크로 빼서 밝게) + 빛 기둥 ult_spotlight · 극장 커튼·위 장식 · 악기
    //   · 게이지 막대(ult_gauge_bar) · 팻말(jwc_card) · 관객 말풍선(jw_bubble, 9칸 늘이기). 줴리 옷 = 턱시도 파츠(jwt_torso·front·back)로 바꿔 끼움 + 나비넥타이
    // 화면 고정 소품은 웹 화면 좌표(1280×720)를 카메라 보이는 범위에 맞춰 옮김. 글은 전부 자막 시트 (말풍선·게이지·팻말 글 = 월드 글자)
    // 자막: c1 (갑분싸) · c2 야유 · c3~c5 감사합니다 · c6 (…인정이지) · c7 컷인 제목 · c8·c9 게이지 제목 · c10 게이지 {n}% · c11 로딩 중
    //   c12~c23 아재개그 (질문·답 6쌍) · c24~c48 관객 대사 · c49·c50 야유 팝업 · c51 귀뚤 · c52 (빠직) · c53·c54 딱·퍽 · c55~c57 펑 · c58·c59 음표 · c61 끝 인사
    public class UltThankYou : UltBase
    {
        public override float Dur => 11.7f;
        const float T_Q = 4.6f, T_A = 5.5f, T_BOO = 6.7f, T_BOW = 7.1f, T_END = 10.35f;
        static readonly float[] BOWS = { 0.66f, 0.55f, 0.46f, 0.38f, 0.32f, 0.27f, 0.23f, 0.2f, 0.18f };
        static readonly Color GOLD = new(0.95f, 0.76f, 0.31f), CREAM = new(1f, 0.95f, 0.75f), RED = new(0.91f, 0.47f, 0.42f), GRAY = new(0.78f, 0.76f, 0.82f),
            PAPER = new(1f, 0.98f, 0.94f), LILAC = new(0.80f, 0.71f, 0.86f), TOMATO = new(0.85f, 0.28f, 0.23f), INK = new(0.235f, 0.196f, 0.176f),
            WOOD = new(0.54f, 0.35f, 0.2f), BLUE = new(0.87f, 0.94f, 1f);
        // dot 그림(부드러운 원)의 알파 컷 → 반지름(그림 반폭 대비). 컷 0.45 ≈ 0.69
        static readonly float[] SPOT_CUT = { 0.75f, 0.45f, 0.15f };
        const float DOT_R = 0.69f;
        // 말풍선 그림 jw_bubble (552×279): 몸통 0~220px, 꼬리 아래 59px, 모서리 60px. 9칸 테두리 · 픽셀 밀도(꼬리 = 8 게임 단위)
        static readonly Vector4 BUB_BORDER = new(60, 119, 60, 60);
        const float BUB_PPU = 737;
        // 화면 맨 앞 정렬 (WorldCanvas 32500 보다 아래)
        const int L_DARK = 0, L_CONE = 1, L_INST = 2, L_SPLAT = 3, L_CAT = 4, L_CATTIE = 5, L_PROJ = 7, L_BUB = 8, L_CURTAIN = 10, L_VAL = 11, L_GAUGE = 12, L_STICK = 14, L_CARD = 15, L_IRIS = 20;

        class Seat { public Rat o; public float sx0, sy0, seatX, seatY, throwT; public bool dots; }
        class Inst { public UltProp p; public float x, y, w, pop, t0; public int order; }
        class Proj { public string kind; public UltProp p; public float x, y, z0, tx, ty, t, dur, arc, rot, vr, bx, by, a0, fade = 1; public bool hit, blown; }
        class Splat { public UltProp p; public float blown; }
        class Stuck { public UltProp p; public float ox, oz; }
        class Talk { public Rat o; public bool cat; public string key; public float t0, dur = 1.1f; public bool on; public SpriteRenderer bub; public TMP_Text txt; }
        class Shade { public UltProp[] plates; public SpriteMask[] holes; }
        class CatActor { public Cat c; public float x, y, tx, walk, jit; public string mode = "walk"; }

        readonly List<Seat> crowd = new();
        readonly List<Inst> inst = new();
        readonly List<Proj> proj = new();
        readonly List<Splat> splats = new();
        readonly List<Stuck> stuck = new();
        readonly List<Talk> talk = new();
        readonly List<TMP_Text> labels = new();
        readonly HashSet<Item> popped = new();
        readonly HashSet<string> flags = new();

        float stX, stY, dirY, charge, gray, dark, iris, spX, spY, drumT;
        int gag, bowN = -1;
        Rect vr; float S;        // 지금 보이는 범위 · 웹 화면 1px = 게임 단위
        Shade spotShade, irisShade;
        UltProp cone, curtainL, curtainR, valance, gFrame, gFill, card, stick, tie, catTie;
        TMP_Text gTitle, gNum, gLoad, cardTxt, byeTxt;
        CatActor cat;
        readonly List<GameObject> temps = new();                        // 직접 만든 것 (마스크·말풍선) — 끝나면 지움
        readonly List<(SpriteRenderer sr, SpriteMask m)> bodyMasks = new();
        Sprite dotSprite, bubSprite;
        readonly List<(SpriteRenderer sr, Sprite orig, Sprite tux)> tuxSwap = new();     // 턱시도로 바꾼 리그 그림 (끝나면 되돌림)

        public override void Pre() { var s = CapText("c7"); if (!string.IsNullOrEmpty(s)) Title = s; }

        public override void Begin()
        {
            Beat(5.75f, () => Cap("c1"));
            Beat(6.7f, () => Cap("c2"));
            Beat(7.25f, () => Cap("c3"));
            Beat(8.4f, () => Cap("c4"));
            Beat(10.35f, () => Cap("c5"));
            Beat(10.9f, () => Cap("c6"));
            stX = R.x; stY = R.y; spX = stX; spY = stY; R.face = 1; R.z = 0;
            gag = Random.Range(0, 6);
            vr = ViewRect(); S = vr.width / 1280f;
            // 어둠·핀 조명 (화면을 덮는 판 3장, 구멍 크기를 조금씩 달리해 가장자리를 부드럽게)
            dotSprite = SpriteOf("dot");
            spotShade = MakeShade(L_DARK, SPOT_CUT);
            cone = Prop("ult_spotlight", stX, stY, 0, 100); if (cone != null) { cone.alpha = 0; if (cone.r) cone.r.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask; }
            BodyMasks();
            var bs = SpriteOf("jw_bubble");
            if (bs) bubSprite = Sprite.Create(bs.texture, bs.rect, new Vector2(0.5f, 0), BUB_PPU, 0, SpriteMeshType.FullRect, BUB_BORDER);
            // 무대 악기 (인사할 때마다 그쪽 악기가 통 튐)
            var ids = new (string, float, float, float)[] { ("jw_harp", -210, -40, 60), ("jw_violin", -160, -55, 36), ("jw_drum", 150, -50, 58), ("jw_tuba", 200, -38, 58), ("jw_cello", 250, -25, 46), ("jw_trumpet", 135, -12, 38), ("jw_stand", -115, -20, 34) };
            foreach (var (id, dx, dy, w) in ids)
            {
                var p = Prop(id, stX + dx, stY + dy, 0, w); if (p == null) continue;
                p.visible = false;
                inst.Add(new Inst { p = p, x = stX + dx, y = stY + dy, w = w, t0 = Rand(0.1f, 0.5f) });
            }
            inst.Sort((a, b) => a.y.CompareTo(b.y)); for (int i = 0; i < inst.Count; i++) inst[i].order = i;
            // 객석: 무대 앞(열린 쪽)으로 화면 안 쥐들이 걸어와 앉음
            dirY = M.Stage.Open.Contains(StageManager.RoomOf(stX, stY + 220)) ? 1 : -1;
            var vin = ViewRect(20);
            foreach (var o in new List<Rat>(RatMgr.Rats))
            {
                if (crowd.Count >= 24) break;
                if (o == R || o.UltOn || !vin.Contains(new Vector2(o.x, o.y)) || !GrabRat(o)) continue;
                int i = crowd.Count, row = i / 8, col = i % 8;
                var s = new Seat { o = o, sx0 = o.x, sy0 = o.y, throwT = Rand(6.75f, 7.6f) };
                s.seatX = stX + (col - 3.5f) * 50 + Rand(-8, 8) + (row % 2) * 22; s.seatY = stY + dirY * (105 + row * 38);
                float vx = 0, vy = 0; M.Stage.Confine(ref s.seatX, ref s.seatY, ref vx, ref vy, o.Radius + 6, stX, stY, 0);
                crowd.Add(s);
            }
            MakeCat();
            // 턱시도 줴리: 몸통·앞다리·뒷다리 그림을 턱시도 그림으로 (원래 그림과 같은 크기·피벗) + 나비넥타이
            Tuxedo();
            tie = Prop("jwc_bowtie", R.x, R.y, 0, 15 * R.GradeData.size);
            // 극장 커튼 · 위 장식 · 게이지 · 팻말
            curtainL = Prop("jwc_curtain", stX, stY, 0, 200); if (curtainL != null) curtainL.flip = true;
            curtainR = Prop("jwc_curtain", stX, stY, 0, 200);
            valance = Prop("jwc_valance", stX, stY, 0, 800);
            gFrame = Prop("ult_gauge_bar", stX, stY, 0, 100); gFill = Prop("ult_gauge_bar", stX, stY, 0, 100);
            if (gFrame != null) { gFrame.visible = false; gFrame.tint = new Color(0.32f, 0.26f, 0.38f); }
            if (gFill != null) gFill.visible = false;
            stick = Prop("ult_gauge_bar", stX, stY, 0, 100); if (stick != null) { stick.visible = false; stick.tint = WOOD; }
            card = Prop("jwc_card", stX, stY, 0, 100); if (card != null) card.visible = false;
            gTitle = Label(); gNum = Label(); gLoad = Label(); cardTxt = Label(); byeTxt = Label();
        }

        public override void Step(float dt, float k)
        {
            float t = T;
            R.x = stX; R.y = stY; R.z = 0; R.vx = R.vy = 0;
            vr = ViewRect(); S = vr.width / 1280f;
            // 평소 크기 줴리를 카메라로 크게 (몸을 키우지 않음)
            if (M.Cam) M.Cam.ultZoom = t < 10.9f ? 1 + 0.75f * Mathf.Min(1, t / 0.6f) : 1 + 0.75f * Mathf.Max(0, 1 - (t - 10.9f) / 0.4f);
            foreach (var I in inst) I.pop = Mathf.Max(0, I.pop - dt * 4);
            dark = t < 11.2f ? Mathf.Min(0.55f, t * 1.1f) : Mathf.Max(0, dark - dt * 2);
            gray = t > T_A + 0.05f && t < T_BOO ? Mathf.Min(1, (t - T_A) / 0.25f) : Mathf.Max(0, gray - dt * 5);
            iris = t > 10.8f ? Mathf.Min(1, (t - 10.8f) / 0.6f) : 0;
            charge = Gauge(t) / 100;
            bool booing = t > T_BOO && t < T_END;
            StepCrowd(t, dt, booing);
            StepCat(t, dt, booing);
            StepJerry(t, dt);
            spX += (R.x - spX) * Mathf.Min(1, dt * 4); spY += (R.y - spY) * Mathf.Min(1, dt * 4);
            Projectiles(dt);
            StepTalk(t);
            DrawStage(t);
            DrawUi(t);
        }

        // ── 관객 쥐 ──
        void StepCrowd(float t, float dt, bool booing)
        {
            foreach (var s in crowd)
            {
                var o = s.o; if (!o) continue;
                o.UltJit = 0;
                if (t < 0.8f)
                {
                    float e = Smooth(t / 0.8f), nx = Mathf.Lerp(s.sx0, s.seatX, e), ny = Mathf.Lerp(s.sy0, s.seatY, e);
                    o.vx = (nx - o.x) / Mathf.Max(dt, 0.001f); o.vy = (ny - o.y) / Mathf.Max(dt, 0.001f);
                    o.x = nx; o.y = ny; o.face = stX > o.x ? 1 : -1; o.UltPose = null;
                    continue;
                }
                o.vx = o.vy = 0; o.face = stX > o.x ? 1 : -1;
                if (t < 3.3f) { o.z = 0; o.UltPose = P(head: -0.25f, tail: 0.8f + Mathf.Sin(t * 9 + o.x) * 0.3f, front: 0.4f); }                     // 기대감
                else if (t < T_Q) { o.z = Mathf.Abs(Mathf.Sin(t * 14 + o.x)) * 6; o.UltPose = P(tilt: -0.3f, head: -0.4f, tail: 1.2f, front: 1.2f); }    // 두근두근 까치발
                else if (t < T_BOO) { o.z = 0; o.UltPose = P(head: 0.35f, tail: -0.4f); }                                                            // 굳음
                else if (booing)
                {
                    // 야유 + 투척: 던지는 순간 팔 휘두르기
                    float sw = Mathf.Clamp01((t - (s.throwT - 0.25f)) / 0.25f);
                    o.z = 0; o.UltPose = P(tilt: -0.45f, front: sw < 1 ? 2.9f - sw * 0.4f : 1.4f, farFront: 0.6f, back: -0.3f, farBack: 0.3f, head: -0.2f, tail: 1.4f, bob: Mathf.Sin(t * 10 + o.x) * 2);
                    if (t > s.throwT)
                    {
                        s.throwT = t + Rand(0.7f, 1.5f);
                        Throw(o.x, o.y, 30, Pick(new[] { "egg", "egg", "egg", "egg", "tomato", "slipper" }), false);
                        if (Random.value < 0.3f) PopupCap(Pick(new[] { "c49", "c40", "c39", "c50" }), o.x, o.y, RED, 16, 0.8f, 50);
                    }
                }
                else { o.z = 0; o.UltPose = P(head: 0.2f, tail: -0.2f); }
                if (t > T_A + 0.2f && t < T_BOO && !s.dots && Random.value < dt * 1.5f) { s.dots = true; PopupCap("c38", o.x, o.y, GRAY, 18, 0.9f, 50); }
            }
        }

        // ── 앞줄 턱시도 고양이 (= 어그로에 낚인 관객). 고양이 그림 리그만 빌려 씀 (게임 고양이 아님) ──
        void MakeCat()
        {
            var cm = Object.FindFirstObjectByType<CatManager>();
            var art = cm && cm.catArt ? cm.catArt.Get("tuxedo") : null;
            if (!cm || !cm.catPrefab || art == null) return;
            var c = Object.Instantiate(cm.catPrefab, cm.catRoot ? cm.catRoot : cm.transform);
            c.enabled = false; c.name = "UltCat_tuxedo";
            c.rig.Build(art, cm.catLength * 0.9f, 1, true);
            float tx = stX + 115, ty = stY + dirY * 70, vx = 0, vy = 0;
            M.Stage.Confine(ref tx, ref ty, ref vx, ref vy, 40, stX, stY, 0);
            cat = new CatActor { c = c, x = vr.xMax + 120, y = ty, tx = tx };
            catTie = Prop("jwc_bowtie", cat.x, cat.y, 0, 22);
        }

        void StepCat(float t, float dt, bool booing)
        {
            if (cat == null || !cat.c) return;
            cat.jit = 0;
            if (t < 1.0f) { float e = Smooth(t / 1.0f); cat.x = Mathf.Lerp(vr.xMax + 120, cat.tx, e); cat.mode = "walk"; cat.walk += dt * 14; }
            else if (t < T_A + 0.1f) cat.mode = "still";
            else if (t < T_BOO) { cat.mode = "flinch"; cat.jit = 1.5f; if (flags.Add("catMad")) PopupCap("c52", cat.x, cat.y, RED, 34, 1.2f, 110); }
            else if (booing)
            {
                cat.mode = "crack";                                                      // 앞발 들고 부들부들 = 야유
                if (t > 6.85f && flags.Add("catThrow")) { Throw(cat.x, cat.y, 60, "slipper", true); PopupCap("c49", cat.x, cat.y, RED, 22, 1, 120); }
                if (t > 8.9f && flags.Add("catThrow2")) Throw(cat.x, cat.y, 60, "slipper", true);
            }
            else cat.mode = "flinch";
            float tt = Time.time;
            var p = new RatRig.Pose { tail = 0.3f + Mathf.Sin(tt * 3) * 0.2f, sx = 1, sy = 1 };
            switch (cat.mode)
            {
                case "walk": { float s1 = Mathf.Sin(cat.walk); p.front = s1 * 0.5f; p.farBack = s1 * 0.45f; p.farFront = -s1 * 0.5f; p.back = -s1 * 0.45f; p.bob = -Mathf.Abs(Mathf.Cos(cat.walk)) * 2.5f; break; }
                case "still": p.head = 0.1f; p.tail = 0.2f; break;
                case "crack": p.tilt = -0.75f; p.back = 1.1f; p.farBack = 1.1f; p.front = 2.2f + Mathf.Sin(tt * 30) * 0.3f; p.farFront = 2.2f - Mathf.Sin(tt * 30) * 0.3f; p.head = -0.1f; p.tail = 0.5f; break;
                default: p.tilt = -0.9f; p.back = 1.1f; p.farBack = 1.1f; p.front = 2.6f; p.farFront = 2.4f; p.head = -0.5f; p.tail = 1.4f; break;   // flinch
            }
            var c = cat.c;
            c.transform.position = World.ToUnity(cat.x + (cat.jit > 0 ? Rand(-cat.jit, cat.jit) : 0), cat.y);
            c.rig.Apply(p, 1, -1, 1, 31000 + L_CAT * 10);          // 어둠 위 (웹처럼 고양이는 밝게)
            if (c.shadow)
            {
                float w = 40 * 1.6f, sw = c.shadow.sprite ? c.shadow.sprite.bounds.size.x : 1;
                c.shadow.transform.position = World.ToUnity(cat.x, cat.y);
                c.shadow.transform.localScale = new Vector3(w * 2 * World.U / sw, w * 0.6f * 2 * World.TILT * World.U / sw, 1);
            }
            if (catTie != null && c.rig.head)
            {
                var n = World.FromUnity(c.rig.head.transform.position);
                catTie.x = n.x - 4; catTie.y = n.y; catTie.z = -6; OnTop(catTie, L_CATTIE);
            }
        }

        // ── 줴리 ──
        void StepJerry(float t, float dt)
        {
            if (t < T_Q - 0.4f)
            {
                // ① 어그로: 기 모으기. 99% 에서 멈추면 떨림도 멈칫, 다시 모을 땐 더 크게
                bool stall = t > 2.2f && t < 3.3f, hot = t > 3.3f;
                R.UltJit = stall ? 0.6f : 1 + charge * (hot ? 5 : 3);
                R.UltPose = P(tilt: -0.25f, front: 2.3f + Mathf.Sin(t * 30) * 0.2f, farFront: 2.1f, back: -0.5f, farBack: 0.5f, head: -0.25f, tail: 1.3f, sy: 1 - Mathf.Min(1, charge) * 0.1f);
                if (!stall && Random.value < dt * (hot ? 45 : 20))
                    Fx?.Stars(R.x + Rand(-70, 70), R.y + Rand(-20, 20), Rand(10, 90), 1, hot ? RED : GOLD, hot ? Color.white : CREAM, 20, hot ? 140 : 60);
                if (!stall && (drumT -= dt) <= 0) { drumT = hot ? 0.12f : 0.25f; Fx?.Ring(R.x, R.y, 40 + charge * 30, hot ? RED : GOLD, 0.25f); }   // 드럼 롤 대신
                if (hot) Fx?.Shake(0.03f); else if (t > 1.2f && !stall) Fx?.Shake(0.015f);
                if (t > 3.0f && flags.Add("drop")) Fx?.Dust(R.x, R.y, 4, 0.6f);
                Say("c1", (0.3f, "c24"), (0.8f, "c25"), (1.3f, "c26"), (1.8f, "c27"));
                if (t > 2.3f) Say("c1b", (0, "c28"), (0.35f, "c29"), (0.6f, "c30"), (0.8f, "c31"));
                if (t > 3.35f) Say("c1c", (0, "c32"), (0.3f, "c33"), (0.55f, "c34"), (0.8f, "c35"));
            }
            else if (t < T_Q)
            {
                // 폭발 직전 정적 (0.4초)
                R.UltJit = 0; R.UltPose = P(tilt: -0.35f, front: 2.9f, farFront: 2.7f, back: -0.2f, farBack: 0.2f, head: -0.35f, tail: 1.5f, sy: 0.88f);
                if (flags.Add("hush")) Flash(Color.white, 0.15f);
            }
            else if (t < T_A)
            {
                // ② 팻말 번쩍: 아재개그 질문
                R.UltJit = 0; R.UltPose = P(tilt: -0.35f, front: 2.8f, farFront: 0.3f, back: -0.2f, farBack: 0.2f, head: -0.3f, tail: 1.1f);
                if (flags.Add("reveal")) { Flash(Color.white, 0.25f); Fx?.Shake(0.12f); }
            }
            else if (t < T_BOW)
            {
                // ③ 답 → 갑분싸 → 야유. 줴리는 박수 받을 준비 (뻔뻔한 미소로 꼿꼿이)
                R.UltJit = 0; R.UltPose = BowPose(0, t);
                foreach (var kk in new[] { 5.8f, 6.15f, 6.45f })
                    if (t > kk && flags.Add("cr" + kk)) PopupCap("c51", stX + Rand(-250, 250), stY + Rand(-60, 60), GRAY, 14, 0.7f, 30);
                Say("c2", (0.15f, "c36"), (0.45f, "c28"), (0.7f, "c37"), (1.0f, "c38"));
                if (t > T_BOO && flags.Add("boo")) Fx?.Shake(0.15f);
                if (t > T_BOO) Say("c3", (0, "c39"), (0.15f, "c40"), (0.3f, "c41"));
            }
            else
            {
                // ④⑤ 계란 맞으면서 좌우 번갈아 90도 인사, 점점 빨라짐 → ⑥ 마지막 초고속 인사 대폭발
                int i = 0; float t0 = T_BOW;
                while (i < BOWS.Length - 1 && t > t0 + BOWS[i]) { t0 += BOWS[i]; i++; }
                bool last = t > T_END;
                float ph = last ? 1 : Mathf.Clamp01((t - t0) / BOWS[i]);
                float bend = last ? 1.55f : 1.55f * Mathf.Pow(Mathf.Sin(ph * Mathf.PI), 0.7f);
                R.face = last ? 1 : (i % 2 == 1 ? -1 : 1);
                if (!last && i != bowN && ph > 0.45f)
                {
                    bowN = i; Band(R.face);
                    Fx?.Ring(R.x, R.y, 80 + i * 10, CREAM, 0.3f);       // 박수 소리 대신
                    if (i >= 2)       // 처음 두 번은 인사만 또렷하게
                    {
                        Pop(1 + i / 2, 1);
                        Shock(R.x, R.y, ULT_R * (0.3f + i * 0.05f), UltD * 0.15f, CREAM, 1.2f);
                    }
                }
                if (last && flags.Add("final"))
                {
                    Pop(999, 1.6f); BlastActors(R.x, R.y, ULT_R * 0.8f, 700, UltD);
                    Shock(R.x, R.y, ULT_R * 0.95f, UltD * 0.4f, GOLD, 2.4f);
                    Flash(CREAM, 0.45f); Fx?.Shake(0.4f); Fx?.Hitstop(0.08f); Band(0);
                    Fx?.Anim("explosion", R.x, R.y, 0, 1.4f);
                    Fx?.Stars(R.x, R.y, 40, 30, GOLD, RED, 200, 600); Fx?.Burst(R.x, R.y, 40, 24, LILAC, CREAM, 200, 560, 3, 8);
                    foreach (var p in proj) p.blown = true;          // 던진 것들도 싹 날아감
                    foreach (var sp in splats) sp.blown = 0.01f;
                    foreach (var st in stuck) KillProp(st.p);
                    stuck.Clear();
                }
                R.UltPose = BowPose(bend, t);
                Say("c4", (0.3f, "c42"), (0.8f, "c43"), (1.3f, "c44"), (1.8f, "c45"), (2.4f, "c46"), (3.0f, "c47"), (3.4f, "c48"));
            }
            // 나비넥타이 (목 자리) · 몸에 묻은 계란·토마토 — 줴리 바로 앞에 그림
            int jo = World.SortOrder(R.y);
            if (tie != null && R.rig && R.rig.head)
            {
                var n = World.FromUnity(R.rig.head.transform.position);
                tie.x = n.x; tie.y = n.y; tie.z = -3 * R.GradeData.size; tie.sortBias = jo + 1 - World.SortOrder(tie.y);
            }
            foreach (var st in stuck) if (st.p != null) { st.p.x = R.x + st.ox * R.face; st.p.y = R.y; st.p.z = st.oz; st.p.sortBias = 2; }
        }

        // ── 무대 (월드): 어둠·핀 조명·악기 ──
        void DrawStage(float t)
        {
            float rs = 120 + Mathf.Min(1.5f, charge) * 20;
            SetShade(spotShade, spX, spY, 0, rs, rs * 0.43f, dark, Color.white, L_DARK);
            foreach (var (sr, m) in bodyMasks) if (m) { m.sprite = sr ? sr.sprite : null; m.enabled = sr && sr.enabled && sr.sprite && iris < 0.05f; }
            if (cone != null)
            {
                // 화면 위 바깥에서 줴리 발밑까지 내려오는 빛 기둥
                float top = (spY - vr.yMin) * World.TILT + 200 * S, w = rs * 2.08f;
                float asp = Aspect(cone);
                cone.w = w; cone.flat = top / Mathf.Max(1, w * asp); cone.x = spX; cone.y = spY; cone.z = top * 0.41f;
                cone.alpha = dark * 0.35f; OnTop(cone, L_CONE);
            }
            foreach (var I in inst)
            {
                float ap = Mathf.Clamp01((t - I.t0) / 0.25f);
                I.p.visible = ap > 0;
                float sc = (ap < 1 ? 0.4f + 0.6f * ap : 1) * (1 + I.pop * 0.18f);
                I.p.w = I.w * sc; I.p.flat = 1 - I.pop * 0.1f; I.p.x = I.x; I.p.y = I.y;
                I.p.z = I.p.w * Aspect(I.p) * I.p.flat * 0.5f;       // 바닥 기준으로 튐
                I.p.alpha = Mathf.Min(1, 0.35f + dark);
                OnTop(I.p, L_INST, I.order);
            }
        }

        // ── 화면 연출 (웹 ui): 갑분싸 회색 · 커튼 · 게이지 · 팻말 · 아이리스 아웃 ──
        void DrawUi(float t)
        {
            if (gray > 0.01f) Flash(new Color(0.36f, 0.35f, 0.4f), gray * 0.45f);
            // 극장 커튼: 가운데에서 양옆으로 걷힘 (폭 230px, 화면 높이 전체)
            float cw = 230, open = Smooth(Mathf.Min(1, t / 0.8f));
            ScreenProp(curtainL, Mathf.Lerp(640 - cw, -cw * 0.55f, open) + cw / 2, 360, cw, 720, L_CURTAIN);
            ScreenProp(curtainR, Mathf.Lerp(640, 1280 - cw * 0.45f, open) + cw / 2, 360, cw, 720, L_CURTAIN);
            ScreenProp(valance, 640, 37, 1320, 90, L_VAL);
            // ① 거창한 충전 게이지 (커튼 위 장식 아래 가운데): 99%에서 멈춤 → 97%로 뚝 → 폭주 999%
            bool gOn = t > 0.2f && t < 4.7f;
            if (gFrame != null) gFrame.visible = gOn; if (gFill != null) gFill.visible = gOn;
            if (gOn)
            {
                float g = Gauge(t); bool over = g > 100;
                float a = Mathf.Min(1, (t - 0.2f) / 0.2f) * (t > 4.5f ? 1 - (t - 4.5f) / 0.2f : 1), gw = 420, gh = 30, gx = 640 + (over ? Rand(-3, 3) : 0), gy = 118;
                ScreenProp(gFrame, gx, gy + 37, gw, gh, L_GAUGE); if (gFrame != null) gFrame.alpha = a;
                float m = gh * 0.2f, fw = (gw - 2 * m) * Mathf.Min(1, g / 100);
                if (gFill != null)
                {
                    gFill.visible = fw > 2;
                    ScreenProp(gFill, gx - gw / 2 + m + fw / 2, gy + 37, fw, gh - 2 * m, L_GAUGE + 1);
                    gFill.alpha = a; gFill.tint = over ? RED : GOLD;
                }
                bool blink = Mathf.FloorToInt(t * (over ? 12 : 6)) % 2 == 1;
                SetLabel(gTitle, CapText(over ? "c9" : "c8"), gx, gy, 30, blink ? GOLD : RED, a);
                SetLabel(gNum, CapText("c10", Mathf.FloorToInt(g)), gx, gy + 37, over ? 20 : 16, Color.white, a);
                SetLabel(gLoad, CapText("c11"), gx, gy + 70, 15, BLUE, t > 2.2f && t < 3.0f && Mathf.FloorToInt(t * 3) % 2 == 1 ? a : 0);
            }
            else { HideLabel(gTitle); HideLabel(gNum); HideLabel(gLoad); }
            // 줴리 화면 위치 (몸 40 위)
            float px = (R.x - vr.xMin) / S, py = ((R.y - vr.yMin) * World.TILT - 40) / S;
            // ②③ 팻말: 질문 → (뒤집어서) 답. 줴리 대사는 "감사합니다" 하나뿐이라 개그는 말 대신 팻말로. 손잡이 막대 → 줴리 손
            bool cOn = t > T_Q && t < T_BOO;
            if (card != null) card.visible = cOn; if (stick != null) stick.visible = cOn;
            if (cOn)
            {
                bool ans = t > T_A; string txt = CapText("c" + (12 + gag * 2 + (ans ? 1 : 0)));
                float sz = ans ? 44 : 28, flip = Mathf.Abs(Mathf.Cos(Mathf.Clamp01((t - 5.4f) / 0.2f) * Mathf.PI)), pop = Mathf.Min(1, (t - T_Q) / 0.12f);
                float bw = TextWidth(cardTxt, txt, sz * S) / S + 44, bh = sz + 30;
                float bx = Mathf.Clamp(px + bw / 2 + 50, bw / 2 + 20, Mathf.Max(bw / 2 + 20, 1280 - bw / 2 - 320)), by = Mathf.Max(bh + 90, py - 70);
                // 막대: 팻말 왼쪽 아래 → 줴리 손
                float sx0 = bx - bw / 2 + 30, sy0 = by + bh / 2, sx1 = px + 20, sy1 = py + 10, len = Mathf.Sqrt((sx1 - sx0) * (sx1 - sx0) + (sy1 - sy0) * (sy1 - sy0));
                ScreenProp(stick, (sx0 + sx1) / 2, (sy0 + sy1) / 2, len, 8, L_STICK);
                if (stick != null) stick.rot = -Mathf.Atan2(sy1 - sy0, sx1 - sx0);
                float fx = pop * Mathf.Max(0.05f, flip);
                ScreenProp(card, bx, by, bw * fx, bh * pop, L_CARD);
                SetLabel(cardTxt, txt, bx, by + 2, sz, INK, 1, fx, pop);
            }
            else HideLabel(cardTxt);
            // 아이리스 아웃 (줴리로 동그랗게 닫힘) → "— 감사합니다 —"
            if (iris > 0)
            {
                irisShade ??= MakeShade(L_IRIS, new[] { 0.45f });
                float hyp = Mathf.Sqrt(1280f * 1280f + 720f * 720f), rad = Mathf.Lerp(hyp, 46, Smooth(Mathf.Min(1, iris / 0.85f))) * S;
                float a = t > 11.5f ? Mathf.Max(0, 1 - (t - 11.5f) / 0.2f) : 1;
                SetShade(irisShade, R.x, R.y, 40, rad, rad, a, new Color(0.72f, 0.72f, 0.66f), L_IRIS);
                if (iris >= 1) SetLabel(byeTxt, CapText("c61"), px, Mathf.Min(720 - 160, py + 90), 26, CREAM, a);
            }
        }

        // 웹 화면 좌표(1280×720 기준 px) → 지금 보이는 범위의 게임 좌표에 소품 맞추기 (w·h 도 px)
        void ScreenProp(UltProp p, float px, float py, float w, float h, int layer)
        {
            if (p == null) return;
            p.x = vr.xMin + px * S; p.y = vr.yMin + py * S / World.TILT; p.z = 0;
            p.w = Mathf.Max(0.01f, w * S); p.flat = h * S / Mathf.Max(0.01f, p.w * Aspect(p));
            OnTop(p, layer);
        }

        // ── 구멍 뚫린 어둠: 화면을 덮는 jw_dark 판 여러 장 + 판마다 타원 마스크(dot, 알파 컷이 달라 구멍 크기가 조금씩 다름) → 이음매 없음 ──
        Shade MakeShade(int layer, float[] cuts)
        {
            var s = new Shade { plates = new UltProp[cuts.Length], holes = new SpriteMask[cuts.Length] };
            for (int i = 0; i < cuts.Length; i++)
            {
                var p = s.plates[i] = Prop("jw_dark", stX, stY, 0, 100);
                if (p == null || !p.r) continue;
                p.r.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                int order = 31000 + layer * 10 + i;
                var m = NewMask("UltHole", dotSprite, cuts[i], p.r.sortingLayerID, order - 1, order);
                if (m) m.transform.SetParent(M.propRoot ? M.propRoot : M.transform, false);
                s.holes[i] = m;
            }
            return s;
        }
        // 구멍 중심 (x, y, 높이 z), 구멍 반지름 rx·ry (게임 단위). alpha = 판을 모두 겹친 어둠 진하기
        void SetShade(Shade s, float x, float y, float z, float rx, float ry, float alpha, Color tint, int layer)
        {
            if (s == null) return;
            int n = s.plates.Length; float a1 = 1 - Mathf.Pow(1 - Mathf.Clamp01(alpha), 1f / n);
            for (int i = 0; i < n; i++)
            {
                var p = s.plates[i]; if (p == null) continue;
                // 화면 전체 + 여유 (흔들림·확대에도 빈틈 없게)
                p.x = vr.center.x; p.y = vr.center.y; p.z = 0; p.w = vr.width * 1.5f;
                p.flat = vr.height * World.TILT * 1.5f / Mathf.Max(1, p.w * Aspect(p));
                p.alpha = a1; p.tint = tint; p.visible = alpha > 0.005f; OnTop(p, layer, i);
                var m = s.holes[i]; if (!m || !dotSprite) continue;
                float k = 2 / DOT_R * World.U / dotSprite.bounds.size.x;
                m.transform.position = World.ToUnity(x, y, z);
                m.transform.localScale = new Vector3(rx * k, ry * k, 1);
            }
        }
        SpriteMask NewMask(string name, Sprite sp, float cut, int layerId, int back, int front)
        {
            if (!sp) return null;
            var go = new GameObject(name); temps.Add(go);
            var m = go.AddComponent<SpriteMask>();
            m.sprite = sp; m.alphaCutoff = cut; m.isCustomRangeActive = true;
            m.frontSortingLayerID = m.backSortingLayerID = layerId; m.backSortingOrder = back; m.frontSortingOrder = front;
            return m;
        }
        // 줴리 몸 모양 마스크 (파츠마다 같은 그림): 어둠 판·빛 기둥에서 줴리 몸을 빼서 웹처럼 줴리는 밝게 (빛 기둥 앞에 서 있음)
        void BodyMasks()
        {
            if (!R.rig) return;
            var p0 = spotShade != null && spotShade.plates.Length > 0 ? spotShade.plates[0] : null;
            int layerId = p0 != null && p0.r ? p0.r.sortingLayerID : 0;
            foreach (var sr in R.rig.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var m = NewMask("UltBodyMask", sr.sprite ? sr.sprite : dotSprite, 0.3f, layerId, 31000 - 1, 31000 + L_CONE * 10 + 5);
                if (!m) continue;
                m.transform.SetParent(sr.transform, false);
                bodyMasks.Add((sr, m));
            }
        }
        void Tuxedo()
        {
            if (!R.rig || R.rig.IsSingle) return;
            var rg = R.rig;
            foreach (var (sr, id) in new[] { (rg.torso, "jwt_torso"), (rg.front, "jwt_front"), (rg.farFront, "jwt_front"), (rg.back, "jwt_back"), (rg.farBack, "jwt_back") })
            {
                var src = SpriteOf(id); if (!sr || !sr.sprite || !src) continue;
                var o = sr.sprite;
                var tux = Sprite.Create(src.texture, src.rect, new Vector2(o.pivot.x / o.rect.width, o.pivot.y / o.rect.height), o.pixelsPerUnit * src.rect.width / o.rect.width, 0, SpriteMeshType.FullRect);
                tuxSwap.Add((sr, o, tux)); sr.sprite = tux;
            }
        }
        Sprite SpriteOf(string name) { var p = M.MakeProp(name); if (p == null) return null; var sp = p.r ? p.r.sprite : null; p.Destroy(); return sp; }

        // ── 관객 대사 = 관객 머리 위 말풍선 (같은 관객이 겹쳐 말하면 최신 것만) ──
        void Say(string flag, params (float d, string key)[] list)
        {
            if (!flags.Add("say_" + flag)) return;
            foreach (var (d, key) in list)
            {
                var tk = new Talk { key = key, t0 = T + d };
                Speaker(tk);
                talk.Add(tk);
            }
        }
        // 다음에 말할 관객: 방금 말한 둘과 떨어진 쥐 (말풍선 안 겹치게). 쥐가 없으면 고양이
        void Speaker(Talk tk)
        {
            var pool = new List<Rat>(); foreach (var s in crowd) if (s.o) pool.Add(s.o);
            if (pool.Count == 0) { tk.cat = cat != null; return; }
            var last = new List<Vector2>();
            for (int i = talk.Count - 1; i >= 0 && last.Count < 2; i--) { var o = talk[i]; if (o.cat && cat != null) last.Add(new Vector2(cat.tx, cat.y)); else if (o.o) last.Add(new Vector2(o.o.x, o.o.y)); }
            var far = pool.FindAll(o => last.TrueForAll(q => Mathf.Abs(q.x - o.x) > 110 || Mathf.Abs(q.y - o.y) > 40));
            tk.o = Pick(far.Count > 0 ? far : pool);
        }
        // 웹 말풍선: 글 13 굵게, 폭 = 글 폭 + 16, 높이 22, 모서리 10, 꼬리 7, 말하는 쥐 머리 52 위 (고양이 120). 0.1초 통 커지고 끝 0.2초 흐려짐
        //   다른 관객 말풍선과 겹치면 위로 쌓음 (먼저 뜬 것이 아래)
        void StepTalk(float t)
        {
            var seen = new HashSet<object>();
            var live = new List<Talk>();
            for (int i = talk.Count - 1; i >= 0; i--)
            {
                var m = talk[i];
                if (!m.on) { if (t < m.t0) continue; m.on = true; m.t0 = t; }
                if (t - m.t0 >= m.dur || (!m.cat && !m.o)) { KillBubble(m); talk.RemoveAt(i); continue; }
                bool show = seen.Add(m.cat ? cat : m.o) && iris < 0.3f;
                if (!m.txt) { m.txt = Label(); m.bub = NewBubble(); }
                if (show) live.Add(m);
                else { if (m.bub) m.bub.enabled = false; HideLabel(m.txt); }
            }
            live.Sort((a, b) => a.t0.CompareTo(b.t0));
            var placed = new List<Vector4>();      // 화면 기준 (가운데 x, 가운데 높이, 폭, 높이)
            const float bh = 22, tail = 8, gap = 4;
            foreach (var m in live)
            {
                float age = t - m.t0, k = Mathf.Min(1, age / 0.1f), fade = Mathf.Clamp01((m.dur - age) / 0.2f);
                string txt = CapText(m.key);
                float x = m.cat ? cat.x : m.o.x, y = m.cat ? cat.y : m.o.y, zc = m.cat ? 120 : m.o.z + 52;     // 말풍선 몸통 가운데 높이
                float bw = TextWidth(m.txt, txt, 13) + 16;
                // 쌓기: 화면 높이 = z − y·TILT
                for (int guard = 0; guard < 8; guard++)
                {
                    float sy = zc - y * World.TILT; bool hit = false;
                    foreach (var q in placed)
                        if (Mathf.Abs(q.x - x) < (q.z + bw) / 2 + gap && Mathf.Abs(q.y - sy) < bh + tail + gap) { zc += q.y - sy + bh + tail + gap; hit = true; break; }
                    if (!hit) break;
                }
                placed.Add(new Vector4(x, zc - y * World.TILT, bw, bh));
                if (m.bub)
                {
                    m.bub.size = new Vector2(bw, bh + tail) * World.U;
                    m.bub.transform.position = World.ToUnity(x, y, zc - (bh / 2 + tail) * k);     // 꼬리 끝 기준
                    m.bub.transform.localScale = new Vector3(k, k, 1);
                    m.bub.sortingOrder = 31000 + L_BUB * 10; m.bub.enabled = fade > 0.01f;
                    var c = Color.white; c.a = fade; m.bub.color = c;
                }
                SetWorldLabel(m.txt, txt, x, y, zc, 13, INK, fade, k, k);
            }
        }
        SpriteRenderer NewBubble()
        {
            var p = M.MakeProp("jw_bubble"); if (p == null || !p.r) return null;
            temps.Add(p.r.gameObject);
            if (bubSprite) { p.r.sprite = bubSprite; p.r.drawMode = SpriteDrawMode.Sliced; }
            return p.r;
        }
        void KillBubble(Talk m)
        {
            if (m.bub) { temps.Remove(m.bub.gameObject); Object.Destroy(m.bub.gameObject); }
            KillLabel(m.txt); m.bub = null; m.txt = null;
        }

        // 턱시도 줴리가 뒷다리로 서서 인사. bend 0 = 꼿꼿이, 1.55 = 90도 (몸은 약 60도까지만, 대신 고개를 푹)
        static RatRig.Pose BowPose(float bend, float t)
        {
            float k = Mathf.Clamp01(bend / 1.55f), tilt = Mathf.Lerp(-1.3f, -0.42f, k), hw = Mathf.Lerp(0.15f, -0.95f, k);
            return P(tilt: tilt, front: -1.2f, farFront: -1.28f, back: tilt, farBack: tilt + 0.1f, head: -tilt - hw, tail: 0.35f + Mathf.Sin(t * 3) * 0.1f);
        }

        // 충전 게이지(%) 대본: 쭉 오르다 99%에서 멈춤 → 97%로 뚝 → 다시 폭주해서 999%
        static float Gauge(float t)
        {
            if (t < 1.4f) return 87 * Smooth(Mathf.Clamp01((t - 0.2f) / 1.2f));
            if (t < 2.2f) return Mathf.Lerp(87, 99, (t - 1.4f) / 0.8f);
            if (t < 3.0f) return 99;
            if (t < 3.35f) return 97;
            if (t < 3.8f) return Mathf.Lerp(97, 100, (t - 3.35f) / 0.45f);
            return Mathf.Min(999, 100 * Mathf.Pow(10, (t - 3.8f) / 0.4f));
        }
        static float Smooth(float x) => x * x * (3 - 2 * x);

        // 줴리가 인사하면 그쪽(side −1 왼쪽 / 1 오른쪽 / 0 전부) 악기들이 통 튀며 음표
        void Band(int side)
        {
            foreach (var I in inst)
            {
                if (side != 0 && Mathf.Sign(I.x - R.x) != side) continue;
                I.pop = 1;
                if (OnScreen(I.x, I.y) && Random.value < 0.7f) PopupCap(Random.value < 0.5f ? "c58" : "c59", I.x + Rand(-10, 10), I.y, Pick(new[] { CREAM, GOLD, LILAC }), 22, 0.7f, 60 + I.w * 0.6f);
            }
        }

        // 인사 한 번 = 가까운 물건부터 n개 펑 (필살기 피해 공식) + 색종이
        void Pop(int n, float big)
        {
            var list = ItemsIn(R.x, R.y, ULT_R);
            list.RemoveAll(it => popped.Contains(it));
            list.Sort((a, b) => Dist(a.x, a.y, R.x, R.y).CompareTo(Dist(b.x, b.y, R.x, R.y)));
            for (int i = 0; i < list.Count && i < n; i++)
            {
                var it = list[i]; popped.Add(it);
                float a = Mathf.Atan2(it.y - R.y, it.x - R.x);
                if (OnScreen(it.x, it.y))
                {
                    Fx?.Stars(it.x, it.y, 20, 10, GOLD, RED, 120, 360); Fx?.Burst(it.x, it.y, 20, 6, CREAM, LILAC, 120, 360, 3, 7);
                    PopupCap("c" + Random.Range(55, 58), it.x, it.y, CREAM, 18, 0.5f, 40);
                }
                DropItem(it, Mathf.Cos(a) * 380, Mathf.Sin(a) * 380, 360, ItemD * 2 * big);
                BlastActors(it.x, it.y, 70, 480, UltD * 0.3f);
            }
            if (n > 2) Fx?.Shake(0.08f);
        }

        // 관객이 줴리에게 던짐 (hit = 몸에 맞음 → 계란·토마토는 몸에 붙음). 슬리퍼는 맞고 떨어짐
        void Throw(float x, float y, float z0, string kind, bool aim)
        {
            bool hit = aim || Random.value < 0.45f;
            var p = Prop("jwb_" + kind, x, y, z0, kind == "slipper" ? 32 : 22);
            proj.Add(new Proj
            {
                kind = kind, p = p, x = x, y = y, z0 = z0, hit = hit,
                tx = hit ? R.x + Rand(-10, 10) : R.x + Rand(-90, 90), ty = hit ? R.y + Rand(-4, 4) : R.y + Rand(-25, 30),
                dur = Rand(0.5f, 0.75f), arc = Rand(90, 150), rot = Rand(0, Mathf.PI * 2), vr = Rand(-14, 14)
            });
        }

        // 투척물 비행 → 줴리 몸에 철퍼덕(붙음) 또는 무대 바닥에 철퍼덕
        void Projectiles(float dt)
        {
            for (int i = proj.Count - 1; i >= 0; i--)
            {
                var p = proj[i];
                if (p.blown)
                {
                    if (p.a0 == 0) p.a0 = Rand(0.01f, Mathf.PI * 2);
                    p.bx += dt * 900 * Mathf.Cos(p.a0); p.by += dt * 500 * Mathf.Sin(p.a0); p.rot += dt * 20; p.fade -= dt * 2;
                }
                else { p.t += dt; p.rot += p.vr * dt; }
                float e = Mathf.Min(1, p.t / p.dur);
                if (p.p != null)
                {
                    p.p.x = Mathf.Lerp(p.x, p.tx, e) + p.bx; p.p.y = Mathf.Lerp(p.y, p.ty, e) + p.by;
                    p.p.z = Mathf.Lerp(p.z0, p.hit ? 30 : 0, e) + Mathf.Sin(e * Mathf.PI) * p.arc; p.p.rot = p.rot; p.p.alpha = Mathf.Max(0, p.fade);
                    OnTop(p.p, L_PROJ);
                }
                if (p.blown) { if (p.fade <= 0) { KillProp(p.p); proj.RemoveAt(i); } continue; }
                if (p.t < p.dur) continue;
                // 철퍼덕
                KillProp(p.p); proj.RemoveAt(i);
                if (p.hit && p.kind != "slipper") AddStuck(p.kind);
                else AddSplat(p.kind, p.tx, p.ty);
                if (p.hit) PopupCap(p.kind == "slipper" ? "c53" : "c54", p.tx, p.ty, Color.white, 18, 0.5f, 50);
                if (p.kind == "egg") Fx?.Burst(p.tx, p.ty, p.hit ? 24 : 4, 5, GOLD, PAPER, 50, 130, 2, 3);          // 노른자·흰자 튐
                else if (p.kind == "tomato") Fx?.Burst(p.tx, p.ty, p.hit ? 30 : 4, 6, TOMATO, new Color(0.72f, 0.2f, 0.16f), 60, 160, 2, 4);
                else Fx?.Dust(p.tx, p.ty, 2, 0.6f);
                if (p.hit) Fx?.Anim("hit", p.tx, p.ty, 20, 0.6f);
            }
            // 바닥 얼룩 (마지막 인사 폭발에 날아감)
            for (int i = splats.Count - 1; i >= 0; i--)
            {
                var s = splats[i]; if (s.p != null) OnTop(s.p, L_SPLAT);
                if (s.blown <= 0) continue;
                s.blown += dt; float a = Mathf.Max(0, 1 - s.blown * 3);
                if (s.p != null) s.p.alpha = a;
                if (a <= 0) { KillProp(s.p); splats.RemoveAt(i); }
            }
        }

        // 바닥에 철퍼덕: 계란 = 깨진 계란 그림(이미 비스듬한 시점), 토마토·슬리퍼 = 바닥에 눕힘
        void AddSplat(string kind, float x, float y)
        {
            var s = new Splat();
            if (kind == "egg") { s.p = Prop("egg_splat", x, y, 0, 44); if (s.p != null) s.p.rot = Rand(-0.25f, 0.25f); }
            else
            {
                s.p = Prop(kind == "tomato" ? "jwb_tomato_splat" : "jwb_slipper", x, y, 0, kind == "tomato" ? 44 : 32);
                if (s.p != null) { s.p.ground = true; s.p.flat = 0.65f; s.p.rot = Rand(0, Mathf.PI * 2); }
            }
            splats.Add(s);
            if (splats.Count > 60) { KillProp(splats[0].p); splats.RemoveAt(0); }
        }

        void AddStuck(string kind)
        {
            var s = new Stuck { ox = Rand(-10, 10), oz = Rand(4, 16) * R.GradeData.size + 8 };
            s.p = Prop(kind == "egg" ? "egg_splat" : "jwb_tomato_splat", R.x, R.y, s.oz, kind == "egg" ? 18 : 15);
            if (s.p != null) s.p.rot = Rand(0, Mathf.PI * 2);
            stuck.Add(s);
            if (stuck.Count > 4) { KillProp(stuck[0].p); stuck.RemoveAt(0); }   // 몸에 묻은 건 몇 개만 (줴리가 가려지지 않게)
        }

        // ── 월드 글자 (팝업 글자 틀을 복제, 끝나면 지움) ──
        TMP_Text Label()
        {
            var tpl = Fx ? Fx.popupTemplate : null; if (!tpl) return null;
            var t = Object.Instantiate(tpl, tpl.transform.parent);
            t.gameObject.SetActive(true); t.name = "UltLabel"; t.text = "";
            t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.NoWrap; t.fontStyle |= FontStyles.Bold;
            t.rectTransform.pivot = new Vector2(0.5f, 0.5f);      // 팝업 틀은 아래 기준이라 글이 위로 뜸 → 가운데 기준으로
            t.overflowMode = TextOverflowModes.Overflow;
            t.rectTransform.localRotation = Quaternion.identity;
            labels.Add(t);
            return t;
        }
        void KillLabel(TMP_Text t) { if (!t) return; labels.Remove(t); Object.Destroy(t.gameObject); }
        static void HideLabel(TMP_Text t) { if (t) t.alpha = 0; }
        // 화면 좌표(px) 글자. size = 웹 px
        void SetLabel(TMP_Text t, string s, float px, float py, float size, Color col, float alpha, float sx = 1, float sy = 1)
            => SetWorldLabel(t, s, vr.xMin + px * S, vr.yMin + py * S / World.TILT, 0, size * S, col, alpha, sx, sy);
        static void SetWorldLabel(TMP_Text t, string s, float x, float y, float z, float size, Color col, float alpha, float sx = 1, float sy = 1)
        {
            if (!t) return;
            if (t.text != s) t.text = s;
            t.fontSize = size; col.a = alpha; t.color = col;
            t.rectTransform.position = World.ToUnity(x, y, z);
            t.rectTransform.localScale = new Vector3(Mathf.Max(0.001f, sx), Mathf.Max(0.001f, sy), 1);
        }
        // 글 폭 (게임 단위): 글자 틀로 실제로 재고, 틀이 없으면 추정 (한글 1 · 영숫자 0.55 · 그 밖 0.35 × 크기)
        static float TextWidth(TMP_Text t, string s, float size)
        {
            s ??= "";
            if (t && t.rectTransform.parent)
            {
                float fs = t.fontSize; t.fontSize = size;
                float w = t.GetPreferredValues(s).x * t.rectTransform.parent.lossyScale.x / World.U;
                t.fontSize = fs;
                if (w > 0.01f) return w;
            }
            float e = 0;
            foreach (var ch in s) e += ch >= 0xAC00 && ch <= 0xD7A3 || ch == '\u2026' ? 1f : char.IsLetterOrDigit(ch) ? 0.55f : 0.35f;
            return e * size;
        }

        static float Aspect(UltProp p) => p != null && p.r && p.r.sprite ? p.r.sprite.bounds.size.y / Mathf.Max(1e-5f, p.r.sprite.bounds.size.x) : 1;
        // 화면 맨 앞 (어둠 위 · 글자 아래). layer 순서대로 쌓임
        static void OnTop(UltProp p, int layer, int sub = 0) { if (p != null) p.sortBias = 31000 + layer * 10 + sub - World.SortOrder(p.y); }

        public override void Finish()
        {
            R.z = 0; R.UltJit = 0;
            foreach (var s in crowd) if (s.o) { s.o.z = 0; s.o.UltJit = 0; s.o.vx = s.o.vy = 0; }
        }

        public override void Cleanup()
        {
            foreach (var go in temps) if (go) Object.Destroy(go);
            temps.Clear(); bodyMasks.Clear();
            if (bubSprite) Object.Destroy(bubSprite);
            foreach (var (sr, orig, tux) in tuxSwap) { if (sr && sr.sprite == tux) sr.sprite = orig; if (tux) Object.Destroy(tux); }
            tuxSwap.Clear();
            foreach (var t in labels) if (t) Object.Destroy(t.gameObject);
            labels.Clear();
            if (cat != null && cat.c) { if (cat.c.shadow) Object.Destroy(cat.c.shadow.gameObject); Object.Destroy(cat.c.gameObject); }
            cat = null;
            if (R) { R.UltJit = 0; R.z = 0; }
        }
    }
}
