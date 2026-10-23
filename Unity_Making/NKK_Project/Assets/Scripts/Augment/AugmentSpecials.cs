using System.Collections.Generic;
using NKK.Data;
using NKK.Hazards;
using NKK.Humans;
using NKK.Items;
using NKK.Rats;
using NKK.Stage;
using NKK.Ults;
using UnityEngine;

namespace NKK
{
    // 증강 특수 효과 (칼바람식, 2026-10-19 — 증강 특수 효과 테이블). 장착한 증강 중 effect1 = Special 인 것만 (Augments.Specials).
    // 2026-10-23 기획서 Ver.04 (기획서/증강 기획서 Ver.04 반영 결정.md): 시각 효과 증강 = 효과 · 상시(작게 1개) · 발동(크고 짧게).
    //   등급이 범위: 실버 = 한 곳 · 골드 = 방 일부 · 프리즘 = 방 전체 (예고 + 흔들림). 등급 통일·쥐 탑·쥐 모습 = AugmentSpecials.Rats.cs
    // 같은 타입을 여러 개 끼면 대부분 따로따로 (간격 효과는 각자 타이머, 확률은 각자 굴림).
    // 게임 코드의 훅: ItemManager.OnSmashed · Rat.Bump · RatManager.HandleInput/Promote/RollSpecies · StageManager.BreakWall · Rat.Damage/LateUpdate
    public partial class AugmentSpecials : MonoBehaviour
    {
        public static AugmentSpecials I { get; private set; }

        [Header("연결 (비우면 씬에서 찾음)")]
        public GameManager Game;
        public StageManager Stage;
        public RatManager Rats;
        public ItemManager Items;
        public UltimateManager Ults;
        public RunTimer Timer;
        public CatManager Cats;

        [Header("그림 (FxManager 스티커 이름)")]
        public string bladeSticker = "cheese_blade";
        public float bladeSize = 70;
        public string tornadoSticker = "tornado";
        public float tornadoSize = 260;
        [Tooltip("예고 원 (바닥에 눕힌 고리, 붉게)")] public string warnSticker = "ring";
        public Color warnColor = new(1f, 0.3f, 0.25f, 0.85f);
        [Tooltip("천둥 번개 먹구름 · 치즈 비 구름 · 그을음 · 장난감 기차 · 분신 마법진")] public string stormCloudSticker = "storm_cloud";
        public string cheeseCloudSticker = "cheese_cloud", scorchSticker = "scorch", trainSticker = "toy_train", cloneCircleSticker = "magic_circle";
        [Tooltip("구름 크기 · 기차 크기 · 운석 그림 (비면 RatManager 치즈 운석)")] public float cloudSize = 520, trainSize = 420;
        [Tooltip("맵에 소환되는 구름 (천둥 번개 · 치즈 비): 떠 있는 높이 · 떠다니는 속도 · 바닥 범위 원 색")] public float cloudHeight = 300, cloudDrift = 40;
        public Color stormRingColor = new(0.25f, 0.3f, 0.5f, 0.45f), rainRingColor = new(1f, 0.82f, 0.3f, 0.45f);
        public Sprite meteorSprite;

        [Header("글 (자리표시 {n})")]
        public string giantPopup = "거대화!!";
        public string jackpotPopup = "잭팟!! 연구자료 ×{n}";
        public string clickBombPopup = "펑!";
        public string wallQuakePopup = "-{n}%";
        public string clonePopup = "분신!!";
        public string midasPopup = "짤랑";
        public string cheeseRainPopup = "치즈 비!! +{n}%";
        public int giantPopupKey, jackpotPopupKey, clickBombPopupKey, wallQuakePopupKey, clonePopupKey, midasPopupKey, cheeseRainPopupKey;
        public Color popupColor = new(1f, 0.85f, 0.35f);

        // 상태
        readonly Dictionary<AugSpecialRow, float> timers = new();
        readonly Dictionary<Rat, float> giants = new();       // 쥐 → 남은 시간
        readonly Dictionary<Rat, float> giantMul = new();
        readonly Dictionary<object, float> bladeHit = new();
        class BladeSpin { public Rat r; public float life, ang, rad, dmg, gap; public readonly List<FxManager.Sticker> s = new(); }
        readonly List<BladeSpin> spins = new();
        int lastFloor = -1;
        struct Twister { public float x, y, vx, life, tick; public float r, dmg; public FxManager.Sticker s; }
        readonly List<Twister> twisters = new();

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }
        void Start()
        {
            if (!Game) Game = FindFirstObjectByType<GameManager>();
            if (!Stage) Stage = FindFirstObjectByType<StageManager>();
            if (!Rats) Rats = FindFirstObjectByType<RatManager>();
            if (!Items) Items = FindFirstObjectByType<ItemManager>();
            if (!Ults) Ults = FindFirstObjectByType<UltimateManager>();
            if (!Timer) Timer = FindFirstObjectByType<RunTimer>();
            if (!Cats) Cats = FindFirstObjectByType<CatManager>();
        }

        static List<(AugSpecialRow s, int lv)> Sp => Augments.Specials;
        static float V((AugSpecialRow s, int lv) e, int i) => Augments.SP(e.s, i, e.lv);
        float AvgAtk => Rats && Rats.RealCount > 0 ? Rats.TotalPower() / Rats.RealCount : 10;
        Rat AnyRat()
        {
            if (!Rats || Rats.Rats.Count == 0) return null;
            for (int k = 0; k < 6; k++) { var r = Rats.Rats[Random.Range(0, Rats.Rats.Count)]; if (r && r.temp <= 0) return r; }
            return Rats.Rats[0];
        }
        bool Running => Game && !GameOver.Active && !Heist.Active && !(Stage && Stage.Climbing) && !FxManager.Halt && !FxManager.WorldFreeze;
        Rect View => Ults ? Ults.ViewRect(0) : (Stage ? Stage.OpenBounds() : new Rect(0, 0, World.RW, World.RH));
        static string T(int key, string ko) => Loc.Get(key, ko);

        void Update()
        {
            if (Game && Game.Floor != lastFloor) { lastFloor = Game.Floor; bladeHit.Clear(); EndClouds(); }
            float dt = Time.deltaTime;
            UpdateGiants(dt);
            UpdateBlades(dt);
            UpdateTwisters(dt);
            UpdateEvents(dt);
            UpdateRatSide(dt);          // 등급 통일 · 쥐 탑 · 쥐 모습 (AugmentSpecials.Rats.cs)
            UpdateLooks(dt);            // 맵 위 작은 표시 (5G 신호 · 스파크 · 바퀴 자국)
            if (!Running) return;
            foreach (var e in Sp)
            {
                switch (e.s.type)
                {
                    case "Giant": if (Tick(e.s, V(e, 1), dt)) MakeGiants(e); break;
                    case "Lightning": if (Tick(e.s, V(e, 1), dt)) Lightning(e); break;
                    case "Tornado": if (Tick(e.s, V(e, 1), dt)) WarnThen(RandomOpenPoint(160), V(e, 3), 0.8f, p => SpawnTwister(e, p)); break;
                    case "Clones": if (Tick(e.s, V(e, 1), dt)) Clones(e); break;
                    case "Meteor": if (Tick(e.s, V(e, 1), dt)) Meteor(e); break;
                    case "CheeseRain": if (Tick(e.s, V(e, 1), dt)) CheeseRain(e); break;
                    case "Train": if (Tick(e.s, V(e, 1), dt)) Train(e); break;
                }
            }
        }
        bool Tick(AugSpecialRow s, float every, float dt)
        {
            timers.TryGetValue(s, out float t); t += dt;
            if (t < Mathf.Max(0.5f, every)) { timers[s] = t; return false; }
            timers[s] = 0; return true;
        }
        public static bool Has(string type) { foreach (var e in Sp) if (e.s.type == type) return true; return false; }

        // 열린 방 안 무작위 자리 (가장자리 pad 만큼 안쪽, 가능하면 화면 안)
        Vector2 RandomOpenPoint(float pad)
        {
            var b = Stage ? Stage.OpenBounds() : View; var v = View;
            var r = Rect.MinMaxRect(Mathf.Max(b.xMin, v.xMin) + pad, Mathf.Max(b.yMin, v.yMin) + pad, Mathf.Min(b.xMax, v.xMax) - pad, Mathf.Min(b.yMax, v.yMax) - pad);
            if (r.width <= 0 || r.height <= 0) r = b;
            for (int k = 0; k < 12; k++)
            {
                var p = new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax));
                if (!Stage || Stage.Open.Contains(StageManager.RoomOf(p.x, p.y))) return p;
            }
            return r.center;
        }

        // ── 예고: 바닥 붉은 원이 커짐 (프리즘) → t 초 뒤 실행 ──
        class Warn { public Vector2 p; public float t, max, rad; public System.Action<Vector2> go; public FxManager.Sticker s; }
        readonly List<Warn> warns = new();
        void WarnThen(Vector2 p, float rad, float t, System.Action<Vector2> go)
        {
            var w = new Warn { p = p, t = t, max = t, rad = rad, go = go };
            w.s = FxManager.I ? FxManager.I.AddSticker(warnSticker, p.x, p.y, 0, rad * 0.4f, t + 0.1f, 0, true, warnColor) : null;
            warns.Add(w);
        }
        void UpdateWarns(float dt)
        {
            for (int i = warns.Count - 1; i >= 0; i--)
            {
                var w = warns[i];
                if (FxManager.Halt) continue;
                w.t -= dt;
                if (w.s != null) { float k = 1 - Mathf.Clamp01(w.t / w.max); w.s.size = w.rad * 2 * (0.2f + 0.8f * k); w.s.col = new Color(warnColor.r, warnColor.g, warnColor.b, warnColor.a * (0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 14)))); }
                if (w.t > 0) continue;
                w.s?.End(0.05f); warns.RemoveAt(i);
                if (Running) w.go(w.p);
            }
        }

        // ── 쥐 공격력 · 크기 (Rat.Damage · Rat.LateUpdate) ──
        public static float RatDamageMul(Rat r)
        {
            var me = I; if (!me) return 1;
            float m = 1;
            if (r && me.giants.ContainsKey(r) && me.giantMul.TryGetValue(r, out var g)) m *= g;
            return m * TowerMul(r) * UnifyBumpMul(r);
        }
        public static float RatScale(Rat r)
        {
            var me = I; if (!me || !r) return 1;
            float u = UnifyScale(r);       // 등급 통일로 바뀐 쥐는 잘 보이게 크게 (AugmentSpecials.Rats.cs)
            if (!me.giants.TryGetValue(r, out var t)) return u;
            return u * GiantScale(me, r, t);
        }
        static float GiantScale(AugmentSpecials me, Rat r, float t)
        {
            float k = me.giantScale.TryGetValue(r, out var s) ? s : 1.8f;
            float a = Mathf.Clamp01(Mathf.Min(t, 0.4f) / 0.4f);        // 끝날 때 줄어듦
            float grow = Mathf.Clamp01((me.giantAge.TryGetValue(r, out var age) ? age : 1) / 0.5f);   // 0.5초에 걸쳐 커짐 (기획서)
            return 1 + (k - 1) * Mathf.Min(a, grow);
        }
        public static bool IsGiant(Rat r) => I && r && I.giants.ContainsKey(r);
        readonly Dictionary<Rat, float> giantScale = new(), giantAge = new();

        void MakeGiants((AugSpecialRow s, int lv) e)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(V(e, 2)));
            for (int k = 0; k < n; k++)
            {
                var r = AnyRat(); if (!r || giants.ContainsKey(r)) continue;
                SetGiant(r, V(e, 5), V(e, 4), V(e, 3));
                var fx = FxManager.I;
                if (fx) { fx.Ring(r.x, r.y, 120, popupColor, 0.4f); fx.Popup(r.x, r.y, T(giantPopupKey, giantPopup), popupColor, 26, 1f, 90); fx.Shake(0.25f); fx.Dust(r.x, r.y, 10, 2); }
                SfxManager.PlayAt("jgl_unlock", r.x, r.y);
            }
        }
        void SetGiant(Rat r, float life, float mul, float scale) { giants[r] = life; giantMul[r] = Mathf.Max(1, mul); giantScale[r] = Mathf.Max(1, scale); giantAge[r] = 0; }
        void UpdateGiants(float dt)
        {
            if (giants.Count == 0) return;
            var keys = new List<Rat>(giants.Keys);
            var fx = FxManager.I;
            foreach (var r in keys)
            {
                if (!r) { giants.Remove(r); continue; }
                giantAge[r] = (giantAge.TryGetValue(r, out var a) ? a : 0) + dt;
                // 걸음마다 흔들림 0.2초 + 먼지 원 (기획서: 거대 쥐 발동)
                if (fx && r.Speed > 60 && Mathf.Repeat(giantAge[r], 0.35f) < dt && r.OnScreen()) { fx.Shake(0.04f); fx.Dust(r.x, r.y, 2, 1.2f); }
                // 지나간 자리에 발자국 (예전엔 화면 가운데 붙은 그림)
                if (fx && r.Speed > 60 && Mathf.Repeat(giantAge[r], 0.7f) < dt && r.OnScreen()) fx.AddSticker(footprintSticker, r.x, r.y, 0, 34 * RatScale(r), 1.6f, 0, true, new Color(1, 1, 1, 0.35f));
                float t = giants[r] - dt;
                if (t <= 0) { giants.Remove(r); giantMul.Remove(r); giantScale.Remove(r); giantAge.Remove(r); }
                else giants[r] = t;
            }
        }

        // ── 맵에 소환되는 구름 (천둥 번개 · 치즈 비) — 2026-10-23 사용자: 화면 위에 붙이지 말고 맵에 일정 시간마다 소환되어 공격 ──
        //  구름이 대상 근처 하늘에 나타나 천천히 떠다니며 바닥 원(범위) 안을 공격하고 사라짐
        class Cloud { public bool rain; public float x, y, vx, life, rad, dmg, gap, tick; public int bolts; public FxManager.Sticker s, ring; }
        readonly List<Cloud> clouds = new();
        Cloud SummonCloud(bool rain, Vector2 p, float life, float rad, float dmg)
        {
            var c = new Cloud { rain = rain, x = p.x, y = p.y, vx = (Random.value < 0.5f ? 1 : -1) * cloudDrift, life = Mathf.Max(0.5f, life), rad = Mathf.Max(80, rad), dmg = dmg };
            var fx = FxManager.I;
            if (fx)
            {
                c.s = fx.AddSticker(rain ? cheeseCloudSticker : stormCloudSticker, c.x, c.y, cloudHeight, cloudSize, c.life + 1);
                c.ring = fx.AddSticker(warnSticker, c.x, c.y, 0, c.rad * 2, c.life + 1, 0, true, rain ? rainRingColor : stormRingColor);
                fx.Dust(c.x, c.y, 4, 1.4f);
            }
            clouds.Add(c);
            return c;
        }
        // 구름 자리: 보스 → 고양이 → 화면 속 물건 → 열린 방 아무 곳
        Vector2 CloudSpot()
        {
            if (Boss.Current && Boss.Current.CanHit && Ults && Ults.OnScreen(Boss.Current.x, Boss.Current.y)) return new Vector2(Boss.Current.x, Boss.Current.y);
            var cat = Cats ? Cats.Current : null; if (cat && cat.Alive && Ults && Ults.OnScreen(cat.x, cat.y)) return new Vector2(cat.x, cat.y);
            var it = Items ? Items.RandomRestInView() : null;
            return it ? new Vector2(it.x, it.y) : RandomOpenPoint(160);
        }
        void EndClouds()
        {
            foreach (var c in clouds) { c.s?.End(0.4f); c.ring?.End(0.3f); }
            clouds.Clear();
            rainT = 0;
        }
        void UpdateClouds(float dt)
        {
            if (clouds.Count == 0) return;
            var fx = FxManager.I;
            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                var c = clouds[i];
                if (FxManager.Halt) continue;
                c.life -= dt; c.x += c.vx * dt;
                var b = Stage ? Stage.OpenBounds() : View;
                if ((c.x < b.xMin + c.rad * 0.5f && c.vx < 0) || (c.x > b.xMax - c.rad * 0.5f && c.vx > 0)) c.vx = -c.vx;
                c.s?.Move(c.x, c.y, cloudHeight + Mathf.Sin(Time.time * 1.4f + i) * 8);
                c.ring?.Move(c.x, c.y, 0);
                if (c.rain && fx && Random.value < dt * 30)
                {
                    var q = new Vector2(c.x, c.y) + Random.insideUnitCircle * c.rad;
                    fx.Burst(q.x, q.y, cloudHeight * 0.8f, 2, new Color(0.98f, 0.82f, 0.32f), new Color(1f, 0.93f, 0.6f), 10, 40, 6, 10);
                }
                if (Running && (c.tick -= dt) <= 0)
                {
                    if (c.rain) { c.tick = 0.25f; RainHit(c); }
                    else if (c.bolts > 0) { c.tick = c.gap; c.bolts--; Strike(c); }
                }
                if (c.life <= 0 || (Stage && Stage.Climbing)) { c.s?.End(0.4f); c.ring?.End(0.3f); clouds.RemoveAt(i); }
            }
        }

        // ── 천둥 번개 (골드): p1 초마다 대상 근처에 먹구름 소환 → p4 초 동안 바닥 원(반경 p5) 안 대상에 번개 p2 번, 맞은 자리 검게 그을림 ──
        void Lightning((AugSpecialRow s, int lv) e)
        {
            float life = V(e, 4) > 0 ? V(e, 4) : 3;
            var c = SummonCloud(false, CloudSpot(), life, V(e, 5) > 0 ? V(e, 5) : 400, AvgAtk * V(e, 3));
            c.bolts = Mathf.Max(1, Mathf.RoundToInt(V(e, 2)));
            c.tick = Mathf.Min(0.6f, c.life * 0.2f);                    // 구름이 먼저 나타난 뒤 첫 번개
            c.gap = (c.life - c.tick) * 0.85f / c.bolts;
            SfxManager.Play("whoosh");
        }
        void Strike(Cloud c)
        {
            var targets = new List<object>();
            if (Boss.Current && Boss.Current.CanHit && Vector2.Distance(new Vector2(Boss.Current.x, Boss.Current.y), new Vector2(c.x, c.y)) < c.rad + Boss.Current.R) targets.Add(Boss.Current);
            var cat = Cats ? Cats.Current : null; if (cat && cat.Alive && cat.BodyDist(c.x, c.y) < c.rad) targets.Add(cat);
            if (Items)
            {
                foreach (var h in Items.Humans) if (h && h.Alive && h.BodyDist(c.x, c.y) < c.rad) targets.Add(h);
                foreach (var it in Items.InRange(c.x, c.y, c.rad)) if (it.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(it.x, it.y), new Vector2(c.x, c.y)) < c.rad) targets.Add(it);
            }
            var fx = FxManager.I;
            Vector2 p;
            object o = targets.Count > 0 ? targets[Random.Range(0, targets.Count)] : null;
            if (o == null || !RatManager.FoePos(o, out p, out _)) { p = new Vector2(c.x, c.y) + Random.insideUnitCircle * c.rad; o = null; }
            fx?.Bolt(p.x, p.y, new Color(1f, 0.92f, 0.45f), 1.2f, 0.28f, cloudHeight);
            fx?.AddSticker(scorchSticker, p.x, p.y, 0, 90, 3f, 0, true, new Color(1, 1, 1, 0.8f));
            if (o != null) RatManager.FoeHit(o, c.dmg, AnyRat(), Random.value * Mathf.PI * 2);
            fx?.Shake(0.12f);
            SfxManager.PlayAt("zap", p.x, p.y);
        }

        // ── 회전 치즈 칼날 (프리즘): 물건을 칠 때 확률로 (묘기와 따로) 그 쥐 둘레를 칼날이 잠깐 돔 ──
        public static void OnRatHit(Rat r, Item it)
        {
            var me = I; if (!me || !r || !me.Running) return;
            UnifyHitFx(r, it);              // 등급 통일 박치기 연출 (AugmentSpecials.Rats.cs)
            foreach (var e in Sp)
            {
                if (e.s.type != "Blades" || Random.value >= V(e, 1)) continue;
                foreach (var sp in me.spins) if (sp.r == r) { sp.life = Mathf.Max(sp.life, V(e, 5)); goto next; }      // 이미 도는 중이면 시간만
                var b = new BladeSpin { r = r, life = V(e, 5), rad = Mathf.Max(40, V(e, 3)), dmg = me.AvgAtk * V(e, 4), gap = 0.3f };
                int n = Mathf.Max(1, Mathf.RoundToInt(V(e, 2)));
                var fx = FxManager.I;
                for (int i = 0; i < n; i++) b.s.Add(fx ? fx.AddSticker(me.bladeSticker, r.x, r.y, 30, me.bladeSize, 99999, 540) : null);
                me.spins.Add(b);
            next:;
            }
        }
        void UpdateBlades(float dt)
        {
            if (spins.Count == 0) return;
            float now = Time.time;
            var fx = FxManager.I;
            for (int i = spins.Count - 1; i >= 0; i--)
            {
                var b = spins[i];
                if (!FxManager.Halt) { b.life -= dt; b.ang += dt * Mathf.PI * 2 * 0.5f; }      // 2초에 한 바퀴
                if (b.life <= 0 || !b.r || (Stage && Stage.Climbing))
                {
                    foreach (var s in b.s) s?.End(0.2f);
                    spins.RemoveAt(i); continue;
                }
                for (int k = 0; k < b.s.Count; k++)
                {
                    float a = b.ang + k * Mathf.PI * 2 / b.s.Count;
                    float x = b.r.x + Mathf.Cos(a) * b.rad, y = b.r.y + Mathf.Sin(a) * b.rad * 0.8f;
                    b.s[k]?.Move(x, y, 30);        // life 는 건드리지 않음 (max 로 되돌리면 투명도 0)
                    if (fx && Random.value < dt * 20) fx.Burst(x, y, 30, 1, new Color(1f, 0.85f, 0.3f), Color.white, 10, 40, 3, 5);     // 칼날 뒤 노란 궤적
                    if (Running) HitAround(x, y, 45, b.dmg, b.gap, now, a, b.r);
                }
            }
        }
        void HitAround(float x, float y, float r, float dmg, float gap, float now, float ang, Rat by)
        {
            var fx = FxManager.I;
            if (Items)
                foreach (var it in Items.InRange(x, y, r + 30))
                    if (it.State == Item.ItemState.Rest && Ready(it, gap, now)) { it.Damage(dmg, by, false, ang); fx?.Spark(it.x, it.y, 20, Color.white, 40); fx?.Burst(it.x, it.y, 20, 3, new Color(1f, 0.85f, 0.3f), Color.white); }
            var cat = (Cats ? Cats.Current : null); if (cat && cat.Alive && cat.BodyDist(x, y) < r && Ready(cat, gap, now)) cat.Damage(dmg, ang, by);
            if (Boss.Current && Boss.Current.CanHit && Vector2.Distance(new Vector2(x, y), new Vector2(Boss.Current.x, Boss.Current.y)) < r + Boss.Current.R && Ready(Boss.Current, gap, now)) Boss.Current.Damage(dmg, by, ang, false);
            if (Items) foreach (var h in Items.Humans) if (h && h.Alive && h.BodyDist(x, y) < r && Ready(h, gap, now)) h.Damage(dmg, by, ang);
        }
        bool Ready(object o, float gap, float now) { if (bladeHit.TryGetValue(o, out var t) && now - t < gap) return false; bladeHit[o] = now; return true; }

        // ── 토네이도 (프리즘): 예고 뒤 랜덤 자리에서 생겨 돌아다님 ──
        void SpawnTwister((AugSpecialRow s, int lv) e, Vector2 p)
        {
            var t = new Twister { x = p.x, y = p.y, vx = (Random.value < 0.5f ? 1 : -1) * Mathf.Max(60, V(e, 5)), life = V(e, 2), r = V(e, 3), dmg = AvgAtk * V(e, 4) };
            t.s = FxManager.I ? FxManager.I.AddSticker(tornadoSticker, t.x, t.y, 0, tornadoSize, t.life + 0.3f, 0, false, null, 0.12f) : null;
            twisters.Add(t);
            FxManager.I?.Shake(0.3f);
            SfxManager.Play("whoosh");
        }
        void UpdateTwisters(float dt)
        {
            if (twisters.Count == 0) return;
            for (int i = twisters.Count - 1; i >= 0; i--)
            {
                var t = twisters[i];
                if (FxManager.Halt) continue;
                t.life -= dt; t.x += t.vx * dt; t.y += Mathf.Sin(Time.time * 2 + i) * 40 * dt;
                var b = Stage ? Stage.OpenBounds() : new Rect(t.x - 1, t.y - 1, 2, 2);
                if (t.x < b.xMin + 60 || t.x > b.xMax - 60) t.vx = -t.vx;
                t.s?.Move(t.x, t.y, 0);
                if ((t.tick -= dt) <= 0 && Running)
                {
                    t.tick = 0.25f;
                    if (Items) { Items.Pull(t.x, t.y, t.r * 1.4f, 60); Items.Aoe(t.x, t.y, t.r, t.dmg * 0.25f, AnyRat(), false); }
                    var cat = (Cats ? Cats.Current : null); if (cat && cat.Alive && cat.BodyDist(t.x, t.y) < t.r) cat.Damage(t.dmg * 0.25f, Random.value * 6.28f, AnyRat());
                    FxManager.I?.Dust(t.x, t.y, 3, 1.4f);
                    FxManager.I?.Shake(0.03f);
                }
                if (t.life <= 0) { t.s?.End(0.3f); twisters.RemoveAt(i); continue; }
                twisters[i] = t;
            }
        }

        // ── 분신 소환 (프리즘): 가장 강한 쥐의 거대 분신이 방 네 구석에서 (예고 뒤) 나타나 싸움, 인구 안 차지 ──
        void Clones((AugSpecialRow s, int lv) e)
        {
            if (!Rats) return;
            Rat best = null; foreach (var r in Rats.Rats) if (r && r.temp <= 0 && !r.UltOn && (!best || r.Damage > best.Damage)) best = r;
            if (!best) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(V(e, 2)));
            var v = View; var ob = Stage ? Stage.OpenBounds() : v;
            var box = Rect.MinMaxRect(Mathf.Max(v.xMin, ob.xMin) + 140, Mathf.Max(v.yMin, ob.yMin) + 120, Mathf.Min(v.xMax, ob.xMax) - 140, Mathf.Min(v.yMax, ob.yMax) - 120);
            if (box.width <= 0 || box.height <= 0) box = ob;
            float life = V(e, 3), scale = V(e, 4), warn = Mathf.Max(0.1f, V(e, 5));
            var src = best;
            for (int i = 0; i < n; i++)
            {
                var p = new Vector2(i % 2 == 0 ? box.xMin : box.xMax, i / 2 % 2 == 0 ? box.yMin : box.yMax) + Random.insideUnitCircle * 30;
                if (Stage && !Stage.Open.Contains(StageManager.RoomOf(p.x, p.y))) p = RandomOpenPoint(140);
                WarnThen(p, 70, warn, q =>
                {
                    if (!src || !Rats) return;
                    int before = Rats.Rats.Count;
                    Rats.SpawnTemp(src, q.x, q.y, life);
                    if (Rats.Rats.Count <= before) return;
                    var c = Rats.Rats[^1];
                    SetGiant(c, life, 1, scale);           // 거대한 모습 (능력치는 그대로)
                    var fx = FxManager.I;
                    if (fx) { fx.Anim("poof", q.x, q.y, 0, 2.2f); fx.AddSticker(cloneCircleSticker, q.x, q.y, 0, 150, 0.8f, 180, true, new Color(0.8f, 0.6f, 1f, 0.9f)); fx.Popup(q.x, q.y, T(clonePopupKey, clonePopup), new Color(0.85f, 0.7f, 1f), 26, 1, 110); fx.Shake(0.3f); }
                });
            }
            SfxManager.Play("whoosh");
        }

        // ── 운석 소환 (프리즘): 붉은 원 예고 뒤 큰 치즈 운석 (쥐 4배) ──
        void Meteor((AugSpecialRow s, int lv) e)
        {
            if (!Items) return;
            var t = Items.RandomRestInView();
            Vector2 p = t ? new Vector2(t.x, t.y) : RandomOpenPoint(160);
            float rad = V(e, 2), dmg = AvgAtk * V(e, 3), size = V(e, 5);
            WarnThen(p, rad, Mathf.Max(0.1f, V(e, 4)), q =>
            {
                var spr = meteorSprite ? meteorSprite : Rats ? (Rats.cheeseMeteorSprite ? Rats.cheeseMeteorSprite : Rats.defaultMeteor) : null;
                Items.DropMeteor(AnyRat(), q.x, q.y, rad, dmg, spr, Mathf.Max(1, size / 40f), 0.35f);
                Rats?.Later(0.35f, () => { FxManager.I?.Shake(0.6f); Items.BlastActors(q.x, q.y, rad, 450, dmg * 0.5f, AnyRat()); });
            });
        }

        // ── 치즈 비 (골드): p1 초마다 맵에 치즈 구름 소환 → p2 초 동안 바닥 원(반경 p5) 안에 치즈 조각이 쏟아져 초당 평균 공격력 × p4 · 그동안 치즈 +p3 ──
        float rainT, rainBonus;
        public static float CheeseMul => I && I.rainT > 0 ? 1 + I.rainBonus : 1;
        void CheeseRain((AugSpecialRow s, int lv) e)
        {
            rainT = Mathf.Max(rainT, V(e, 2)); rainBonus = V(e, 3);
            var c = SummonCloud(true, CloudSpot(), V(e, 2), V(e, 5) > 0 ? V(e, 5) : 280, AvgAtk * V(e, 4));
            c.tick = 0.4f;
            FxManager.I?.Popup(c.x, c.y, T(cheeseRainPopupKey, cheeseRainPopup).Replace("{n}", Mathf.RoundToInt(rainBonus * 100).ToString()), popupColor, 30, 1.4f, 160);
        }
        void RainHit(Cloud c)
        {
            if (c.dmg <= 0) return;
            float d = c.dmg * 0.25f;            // 0.25초마다 → 초당 dmg
            Items?.Aoe(c.x, c.y, c.rad, d, AnyRat(), false);
            var cat = Cats ? Cats.Current : null; if (cat && cat.Alive && cat.BodyDist(c.x, c.y) < c.rad) cat.Damage(d, Random.value * 6.28f, AnyRat());
        }

        // ── 장난감 기차 (프리즘): 맵 한쪽 끝 → 반대편 끝, 지나간 길의 물건이 튕겨 나감 ──
        class TrainRun { public float x, y, vx, w, dmg, end; public FxManager.Sticker s; public readonly HashSet<object> hit = new(); }
        readonly List<TrainRun> trains = new();
        void Train((AugSpecialRow s, int lv) e)
        {
            if (!Stage) return;
            var b = Stage.OpenBounds(); var v = View;
            float y = Mathf.Clamp(Random.Range(v.yMin + 140, v.yMax - 100), b.yMin + 100, b.yMax - 80);
            bool left = Random.value < 0.5f;
            var t = new TrainRun { x = left ? b.xMin - trainSize : b.xMax + trainSize, y = y, vx = (left ? 1 : -1) * Mathf.Max(200, V(e, 2)), w = V(e, 4), dmg = AvgAtk * V(e, 3), end = left ? b.xMax + trainSize : b.xMin - trainSize };
            t.s = FxManager.I ? FxManager.I.AddSticker(trainSticker, t.x, t.y, 20, trainSize, 99999) : null;
            if (t.s != null) t.s.r.flipX = !left;          // 그림은 오른쪽으로 감
            trains.Add(t);
            SfxManager.Play("whoosh");
            var fx = FxManager.I; if (fx) { var pv = left ? new Vector2(v.xMin + 60, y) : new Vector2(v.xMax - 60, y); fx.Popup(pv.x, pv.y, "!", warnColor, 40, 0.8f, 60); }
        }
        void UpdateTrains(float dt)
        {
            if (trains.Count == 0) return;
            var fx = FxManager.I;
            for (int i = trains.Count - 1; i >= 0; i--)
            {
                var t = trains[i];
                if (FxManager.Halt) continue;
                t.x += t.vx * dt;
                t.s?.Move(t.x, t.y, 20 + Mathf.Abs(Mathf.Sin(Time.time * 18)) * 4);
                if (fx && Random.value < dt * 20) fx.Dust(t.x - Mathf.Sign(t.vx) * trainSize * 0.4f, t.y, 2, 1);
                if (Running && Items)
                {
                    foreach (var it in new List<Item>(Items.InRange(t.x, t.y, t.w + trainSize * 0.5f)))
                    {
                        if (it.State != Item.ItemState.Rest || Mathf.Abs(it.y - t.y) > t.w + it.R || Mathf.Abs(it.x - t.x) > trainSize * 0.5f || !t.hit.Add(it)) continue;
                        float ang = Mathf.Atan2(it.y - t.y + Random.Range(-20f, 20f), t.vx);
                        it.Damage(t.dmg, AnyRat(), false, ang);
                        it.AddPush(Mathf.Sign(t.vx) * 400, (it.y - t.y) * 4);
                    }
                    var cat = Cats ? Cats.Current : null;
                    if (cat && cat.Alive && Mathf.Abs(cat.x - t.x) < trainSize * 0.5f && Mathf.Abs(cat.y - t.y) < t.w && t.hit.Add(cat)) cat.Damage(t.dmg, Mathf.Atan2(0, t.vx), AnyRat());
                    var bo = Boss.Current;
                    if (bo && bo.CanHit && Mathf.Abs(bo.x - t.x) < trainSize * 0.5f + bo.R && Mathf.Abs(bo.y - t.y) < t.w + bo.R && t.hit.Add(bo)) bo.Damage(t.dmg, AnyRat(), Mathf.Atan2(0, t.vx), false);
                    foreach (var h in Items.Humans) if (h && h.Alive && Mathf.Abs(h.x - t.x) < trainSize * 0.5f && Mathf.Abs(h.y - t.y) < t.w && t.hit.Add(h)) h.Blast(Mathf.Atan2(0, t.vx), 500, t.dmg, AnyRat());
                }
                if ((t.vx > 0 && t.x > t.end) || (t.vx < 0 && t.x < t.end)) { t.s?.End(0.1f); trains.RemoveAt(i); }
            }
        }

        void UpdateEvents(float dt)
        {
            UpdateWarns(dt);
            UpdateTrains(dt);
            UpdateClouds(dt);
            if (rainT > 0 && !FxManager.Halt) rainT -= dt;
        }

        // ── 훅: 물건 부숨 (미다스의 앞니: 황금 물건 치즈 +) ──
        public static float SmashCheeseMul(Item it)
        {
            var me = I; if (!me || it == null || !it.Gold) return 1;
            float m = 1;
            foreach (var e in Sp) if (e.s.type == "Midas") m += V(e, 1);
            if (m > 1) { FxManager.I?.Burst(it.x, it.y, 30, 12, new Color(0.98f, 0.8f, 0.3f), new Color(1f, 0.95f, 0.6f), 120, 360); FxManager.I?.Popup(it.x, it.y, T(me.midasPopupKey, me.midasPopup), new Color(0.98f, 0.8f, 0.3f), 20, 0.5f, 50); }
            return m;
        }
        // 황금 물건 반짝임 배율 (Item)
        public static float GoldSparkleMul { get { float m = 1; foreach (var e in Sp) if (e.s.type == "Midas") m = Mathf.Max(m, V(e, 2)); return m; } }

        // 클릭 (총공격 중 누를 때마다, 클릭 폭격): 확률로 누른 곳에 작은 폭발 + "펑!"
        public static void OnClick(Vector2 p)
        {
            var me = I; if (!me || !me.Items || !me.Rats || !me.Rats.RushActive) return;
            foreach (var e in Sp)
            {
                if (e.s.type != "ClickBomb" || Random.value >= V(e, 1)) continue;
                float dmg = me.AvgAtk * V(e, 3), rad = Mathf.Max(40, V(e, 4));
                me.Items.Aoe(p.x, p.y, rad, dmg, me.AnyRat());
                var fx = FxManager.I; if (fx) { fx.Anim("explosion", p.x, p.y, 0, 0.7f); fx.Popup(p.x, p.y, T(me.clickBombPopupKey, me.clickBombPopup), new Color(1f, 0.6f, 0.3f), 22, 0.5f, 40); }
            }
        }
        // 벽 무너짐 (연쇄 붕괴): 강한 진동 + 남은 벽 체력 깎기 (이걸로는 무너지지 않음 → 연쇄 끝없이 안 감)
        public static void OnWallBroken()
        {
            var me = I; if (!me || !me.Stage) return;
            float frac = 0; foreach (var e in Sp) if (e.s.type == "WallQuake") frac += V(e, 1);
            if (frac <= 0) return;
            var fx = FxManager.I; string pop = T(me.wallQuakePopupKey, me.wallQuakePopup).Replace("{n}", Mathf.RoundToInt(frac * 100).ToString());
            int shown = 0;
            foreach (var w in me.Stage.WallsIn(me.Stage.OpenBounds()))
            {
                me.Stage.ChipWall(w.i, w.j, w.di, w.dj, frac);
                var c = new Vector2((w.i + 0.5f + w.di * 0.5f) * World.RW, (w.j + 0.5f + w.dj * 0.5f) * World.RH);      // 방 (i, j) 의 di·dj 쪽 벽 가운데
                if (fx && shown < 12 && me.Ults && me.Ults.OnScreen(c.x, c.y)) { fx.Popup(c.x, c.y, pop, new Color(1f, 0.55f, 0.4f), 22, 0.9f, 60); shown++; }
            }
            fx?.Shake(0.5f);
        }
        // 승급 (승급 폭죽): 폭죽 3발이 솟았다 터짐 → 반경 폭발 · 흔들림
        public static void OnPromoted(Rat nr)
        {
            var me = I; if (!me || !nr || !me.Items || !me.Rats) return;
            foreach (var e in Sp)
            {
                if (e.s.type != "PromoteBoom") continue;
                float rad = V(e, 1), dmg = me.AvgAtk * V(e, 2), x = nr.x, y = nr.y;
                var fx = FxManager.I;
                for (int k = 0; k < 3; k++)
                {
                    float ox = (k - 1) * 40, d = k * 0.1f;
                    for (int s = 0; s < 6; s++) { float z = 30 + s * 40; me.Rats.Later(d + s * 0.05f, () => FxManager.I?.Spark(x + ox, y, z, new Color(1f, 0.8f, 0.4f), 30, 0.12f)); }
                    me.Rats.Later(d + 0.32f, () => FxManager.I?.Stars(x + ox, y, 270, 14, Color.white, new Color(1f, 0.5f + 0.2f * Random.value, 0.4f), 120, 360));
                }
                me.Rats.Later(0.55f, () =>
                {
                    me.Items.Aoe(x, y, rad, dmg, nr);
                    me.Items.BlastActors(x, y, rad, 500, dmg, nr);
                    var f = FxManager.I; if (f) { f.Anim("explosion", x, y, 0, 1f); f.Ring(x, y, rad, me.popupColor, 0.5f); f.Shake(0.3f); }
                });
            }
        }
        // 층 클리어 연구자료 (잭팟): 배율 (보통 1). 슬롯머신 릴이 돌아감 → 당첨이면 777
        public static float JackpotMul(float x, float y)
        {
            var me = I; if (!me) return 1;
            float m = 1;
            foreach (var e in Sp)
            {
                if (e.s.type != "Jackpot") continue;
                bool win = Random.value < V(e, 1);
                me.SpinSlot(win, x, y);
                if (!win) continue;
                m *= Mathf.Max(1, V(e, 2));
                FxManager.I?.Popup(x, y, T(me.jackpotPopupKey, me.jackpotPopup).Replace("{n}", Mathf.RoundToInt(m).ToString()), me.popupColor, 34, 1.6f, 120);
                FxManager.I?.Coin(x, y, 12);
            }
            return m;
        }
    }
}
