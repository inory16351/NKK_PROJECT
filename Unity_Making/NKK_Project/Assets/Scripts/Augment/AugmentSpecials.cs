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
    // 같은 타입을 여러 개 끼면 대부분 따로따로 (간격 효과는 각자 타이머, 확률은 각자 굴림).
    // 게임 코드의 훅: ItemManager.OnSmashed · RatManager.StartRush/Promote · StageManager.BreakWall · RunTimer(0초) · UltimateManager.Stop · Heist · Rat.Damage/LateUpdate
    public class AugmentSpecials : MonoBehaviour
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

        [Header("글 (자리표시 {n})")]
        public string giantPopup = "거대화!!";
        public string rewindBanner = "시간 역행!!";
        public string rewindSub = "+{n}초";
        public string jackpotPopup = "잭팟!! 연구자료 ×{n}";
        public string echoPopup = "메아리!!";
        public int giantPopupKey, rewindBannerKey, rewindSubKey, jackpotPopupKey, echoPopupKey;
        public Color popupColor = new(1f, 0.85f, 0.35f);

        // 상태
        readonly Dictionary<AugSpecialRow, float> timers = new();
        readonly Dictionary<Rat, float> giants = new();       // 쥐 → 남은 시간
        readonly Dictionary<Rat, float> giantMul = new();
        readonly Dictionary<object, float> bladeHit = new();
        readonly List<FxManager.Sticker> blades = new();
        int smashCount, rewindUsed, lastFloor = -1;
        float bladeAng;
        readonly HashSet<Rat> echoed = new();
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

        void Update()
        {
            if (Game && Game.Floor != lastFloor) { lastFloor = Game.Floor; smashCount = 0; bladeHit.Clear(); }
            float dt = Time.deltaTime;
            UpdateGiants(dt);
            UpdateBlades(dt);
            UpdateTwisters(dt);
            if (!Running) return;
            foreach (var e in Sp)
            {
                switch (e.s.type)
                {
                    case "Giant": if (Tick(e.s, V(e, 1), dt)) MakeGiants(e); break;
                    case "Lightning": if (Tick(e.s, V(e, 1), dt)) Lightning(e); break;
                    case "Tornado": if (Tick(e.s, V(e, 1), dt)) SpawnTwister(e); break;
                }
            }
        }
        bool Tick(AugSpecialRow s, float every, float dt)
        {
            timers.TryGetValue(s, out float t); t += dt;
            if (t < Mathf.Max(0.5f, every)) { timers[s] = t; return false; }
            timers[s] = 0; return true;
        }

        // ── 쥐 공격력 · 크기 (Rat.Damage · Rat.LateUpdate) ──
        public static float RatDamageMul(Rat r)
        {
            var me = I; if (!me) return 1;
            float m = 1;
            foreach (var e in Sp) if (e.s.type == "Snowball") m += Mathf.Min(V(e, 2), V(e, 1) * me.smashCount);
            if (r && me.giants.ContainsKey(r) && me.giantMul.TryGetValue(r, out var g)) m *= g;
            return m;
        }
        public static float RatScale(Rat r)
        {
            var me = I; if (!me || !r || !me.giants.TryGetValue(r, out var t)) return 1;
            float k = me.giantScale.TryGetValue(r, out var s) ? s : 1.8f;
            float a = Mathf.Clamp01(Mathf.Min(t, 0.4f) / 0.4f);        // 끝날 때 줄어듦
            float grow = Mathf.Clamp01((me.giantAge.TryGetValue(r, out var age) ? age : 1) / 0.35f);
            return 1 + (k - 1) * Mathf.Min(a, grow);
        }
        readonly Dictionary<Rat, float> giantScale = new(), giantAge = new();

        void MakeGiants((AugSpecialRow s, int lv) e)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(V(e, 2)));
            for (int k = 0; k < n; k++)
            {
                var r = AnyRat(); if (!r || giants.ContainsKey(r)) continue;
                giants[r] = V(e, 5); giantMul[r] = Mathf.Max(1, V(e, 4)); giantScale[r] = Mathf.Max(1, V(e, 3)); giantAge[r] = 0;
                var fx = FxManager.I;
                if (fx) { fx.Ring(r.x, r.y, 120, popupColor, 0.4f); fx.Popup(r.x, r.y, Loc.Get(giantPopupKey, giantPopup), popupColor, 26, 1f, 90); fx.Shake(0.15f); }
                SfxManager.PlayAt("jgl_unlock", r.x, r.y);
            }
        }
        void UpdateGiants(float dt)
        {
            if (giants.Count == 0) return;
            var keys = new List<Rat>(giants.Keys);
            foreach (var r in keys)
            {
                if (!r) { giants.Remove(r); continue; }
                giantAge[r] = (giantAge.TryGetValue(r, out var a) ? a : 0) + dt;
                float t = giants[r] - dt;
                if (t <= 0) { giants.Remove(r); giantMul.Remove(r); giantScale.Remove(r); giantAge.Remove(r); }
                else giants[r] = t;
            }
        }

        // ── 번개 ──
        void Lightning((AugSpecialRow s, int lv) e)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(V(e, 2)));
            float dmg = AvgAtk * V(e, 3);
            var targets = new List<object>();
            if (Boss.Current && Boss.Current.CanHit) targets.Add(Boss.Current);
            var cat = (Cats ? Cats.Current : null); if (cat && cat.Alive && Ults && Ults.OnScreen(cat.x, cat.y)) targets.Add(cat);
            if (Items)
            {
                foreach (var h in Items.Humans) if (h && h.Alive && Ults && Ults.OnScreen(h.x, h.y) && targets.Count < n * 3) targets.Add(h);
                for (int k = 0; k < n * 2; k++) { var it = Items.RandomRestInView(); if (it != null && !targets.Contains(it)) targets.Add(it); }
            }
            for (int k = 0; k < n && targets.Count > 0; k++)
            {
                int idx = Random.Range(0, targets.Count); var o = targets[idx]; targets.RemoveAt(idx);
                if (!RatManager.FoePos(o, out var p, out _)) continue;
                FxManager.I?.Bolt(p.x, p.y, new Color(1f, 0.92f, 0.45f), 1.2f);
                RatManager.FoeHit(o, dmg, AnyRat(), Random.value * Mathf.PI * 2);
            }
            FxManager.I?.Shake(0.12f);
            SfxManager.Play("zap");
        }

        // ── 회전 치즈 칼날 ──
        void UpdateBlades(float dt)
        {
            int want = 0; float rad = 160, dmgK = 1, gap = 0.4f, spin = 0.5f;
            foreach (var e in Sp) if (e.s.type == "Blades") { want += Mathf.RoundToInt(V(e, 1)); rad = Mathf.Max(rad, V(e, 2)); dmgK = Mathf.Max(dmgK, V(e, 3)); gap = V(e, 4); spin = V(e, 5); }
            if (!Rats || !Game || GameOver.Active || (Stage && Stage.Climbing)) want = 0;
            while (blades.Count > want) { blades[^1]?.End(0.2f); blades.RemoveAt(blades.Count - 1); }
            if (want == 0) return;
            // 무리 가운데
            float cx = 0, cy = 0; int n = 0;
            foreach (var r in Rats.Rats) if (r && r.temp <= 0) { cx += r.x; cy += r.y; n++; }
            if (n == 0) return; cx /= n; cy /= n;
            if (!FxManager.Halt) bladeAng += dt * spin * Mathf.PI * 2;
            var fx = FxManager.I;
            while (blades.Count < want) blades.Add(fx ? fx.AddSticker(bladeSticker, cx, cy, 30, bladeSize, 99999, 540) : null);
            float now = Time.time, dmg = AvgAtk * dmgK;
            for (int i = 0; i < blades.Count; i++)
            {
                float a = bladeAng + i * Mathf.PI * 2 / blades.Count;
                float x = cx + Mathf.Cos(a) * rad, y = cy + Mathf.Sin(a) * rad * 0.8f;
                if (blades[i] != null) { blades[i].Move(x, y, 30); blades[i].life = 99999; }
                if (!Running) continue;
                HitAround(x, y, 50, dmg, gap, now, a);
            }
        }
        void HitAround(float x, float y, float r, float dmg, float gap, float now, float ang)
        {
            var by = AnyRat();
            if (Items)
                foreach (var it in Items.InRange(x, y, r + 30))
                    if (it.State == Item.ItemState.Rest && Ready(it, gap, now)) { it.Damage(dmg, by, false, ang); FxManager.I?.Spark(it.x, it.y, 20, Color.white, 40); }
            var cat = (Cats ? Cats.Current : null); if (cat && cat.Alive && cat.BodyDist(x, y) < r && Ready(cat, gap, now)) cat.Damage(dmg, ang, by);
            if (Boss.Current && Boss.Current.CanHit && Vector2.Distance(new Vector2(x, y), new Vector2(Boss.Current.x, Boss.Current.y)) < r + Boss.Current.R && Ready(Boss.Current, gap, now)) Boss.Current.Damage(dmg, by, ang, false);
            if (Items) foreach (var h in Items.Humans) if (h && h.Alive && h.BodyDist(x, y) < r && Ready(h, gap, now)) h.Damage(dmg, by, ang);
        }
        bool Ready(object o, float gap, float now) { if (bladeHit.TryGetValue(o, out var t) && now - t < gap) return false; bladeHit[o] = now; return true; }

        // ── 토네이도 ──
        void SpawnTwister((AugSpecialRow s, int lv) e)
        {
            if (!Stage) return;
            var b = Stage.OpenBounds();
            bool fromLeft = Random.value < 0.5f;
            var t = new Twister { x = fromLeft ? b.xMin + 80 : b.xMax - 80, y = Random.Range(b.yMin + 120, b.yMax - 120), vx = (fromLeft ? 1 : -1) * Mathf.Max(60, V(e, 5)), life = V(e, 2), r = V(e, 3), dmg = AvgAtk * V(e, 4) };
            t.s = FxManager.I ? FxManager.I.AddSticker(tornadoSticker, t.x, t.y, 0, tornadoSize, t.life + 0.3f, 0, false, null, 0.12f) : null;
            twisters.Add(t);
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
                }
                if (t.life <= 0) { t.s?.End(0.3f); twisters.RemoveAt(i); continue; }
                twisters[i] = t;
            }
        }

        // ── 훅: 물건 부숨 (눈덩이 · 세포 분열) ──
        public static void OnItemSmashed(Item it)
        {
            var me = I; if (!me || it == null) return;
            me.smashCount++;
            foreach (var e in Sp)
                if (e.s.type == "Mitosis" && me.Rats && Random.value < V(e, 1) && me.Rats.TempCount < Mathf.RoundToInt(V(e, 3)))
                {
                    var src = it.By && it.By.temp <= 0 ? it.By : me.AnyRat();
                    if (src) { me.Rats.SpawnTemp(src, it.x, it.y, V(e, 2)); FxManager.I?.Hearts(it.x, it.y, 2); }
                }
        }
        // 클릭 총공격 (클릭 폭격)
        public static void OnRush(Vector2 p)
        {
            var me = I; if (!me || !me.Items) return;
            foreach (var e in Sp)
            {
                if (e.s.type != "ClickBomb") continue;
                int n = Mathf.Max(1, Mathf.RoundToInt(V(e, 1))); float spread = V(e, 2), dmg = me.AvgAtk * V(e, 3), rad = Mathf.Max(40, V(e, 4));
                for (int k = 0; k < n; k++)
                {
                    float d = k * 0.12f;
                    me.Rats.Later(d, () =>
                    {
                        var q = p + Random.insideUnitCircle * spread;
                        me.Items.Aoe(q.x, q.y, rad, dmg, me.AnyRat());
                        FxManager.I?.Anim("explosion", q.x, q.y, 0, 0.7f);
                    });
                }
            }
        }
        // 벽 무너짐 (연쇄 붕괴): 남은 벽 체력 깎기 (이걸로는 무너지지 않음 → 연쇄 끝없이 안 감)
        public static void OnWallBroken()
        {
            var me = I; if (!me || !me.Stage) return;
            float frac = 0; foreach (var e in Sp) if (e.s.type == "WallQuake") frac += V(e, 1);
            if (frac <= 0) return;
            foreach (var w in me.Stage.WallsIn(me.Stage.OpenBounds())) me.Stage.ChipWall(w.i, w.j, w.di, w.dj, frac);
            FxManager.I?.Shake(0.2f);
        }
        // 승급 (승급 폭죽)
        public static void OnPromoted(Rat nr)
        {
            var me = I; if (!me || !nr || !me.Items) return;
            foreach (var e in Sp)
            {
                if (e.s.type != "PromoteBoom") continue;
                float rad = V(e, 1), dmg = me.AvgAtk * V(e, 2);
                me.Items.Aoe(nr.x, nr.y, rad, dmg, nr);
                me.Items.BlastActors(nr.x, nr.y, rad, 500, dmg, nr);
                var fx = FxManager.I; if (fx) { fx.Anim("explosion", nr.x, nr.y, 0, 1f); fx.Stars(nr.x, nr.y, 40, 14, Color.white, me.popupColor); fx.Shake(0.2f); }
            }
        }
        // 제한시간 0초 (시간 역행): true 면 게임 오버 대신 시간을 되돌림
        public static bool TryRewind(RunTimer t)
        {
            var me = I; if (!me || !t) return false;
            foreach (var e in Sp)
            {
                if (e.s.type != "Rewind" || me.rewindUsed >= Mathf.RoundToInt(V(e, 2))) continue;
                me.rewindUsed++;
                float add = V(e, 1);
                t.Left = add; t.Max = Mathf.Max(t.Max, add);
                if (me.Game) me.Game.ShowBanner(Loc.Get(me.rewindBannerKey, me.rewindBanner), Loc.Get(me.rewindSubKey, me.rewindSub).Replace("{n}", Mathf.RoundToInt(add).ToString()));
                SfxManager.Play("jgl_unlock");
                return true;
            }
            return false;
        }
        // 필살기 끝 (필살기 메아리): 다른 쥐의 필살기 한 번 더 (메아리로 쓴 필살기는 다시 메아리 안 함)
        public static void OnUltDone(Rat r)
        {
            var me = I; if (!me || !me.Ults || !me.Rats) return;
            if (r && me.echoed.Remove(r)) return;
            float ch = 0; foreach (var e in Sp) if (e.s.type == "UltEcho") ch = 1 - (1 - ch) * (1 - V(e, 1));
            if (ch <= 0 || Random.value >= ch) return;
            var pool = new List<Rat>();
            foreach (var q in me.Rats.Rats) if (q && q != r && q.temp <= 0 && me.Ults.Need(q) > 0) pool.Add(q);
            if (pool.Count == 0) return;
            var t = pool[Random.Range(0, pool.Count)];
            t.ultGauge = me.Ults.Need(t); me.echoed.Add(t); me.Ults.Request(t);
            FxManager.I?.Popup(t.x, t.y, Loc.Get(me.echoPopupKey, me.echoPopup), me.popupColor, 26, 1.1f, 90);
        }
        // 층 클리어 연구자료 (잭팟): 배율 (보통 1)
        public static float JackpotMul(float x, float y)
        {
            var me = I; if (!me) return 1;
            float m = 1;
            foreach (var e in Sp)
                if (e.s.type == "Jackpot" && Random.value < V(e, 1))
                {
                    m *= Mathf.Max(1, V(e, 2));
                    FxManager.I?.Popup(x, y, Loc.Get(me.jackpotPopupKey, me.jackpotPopup).Replace("{n}", Mathf.RoundToInt(m).ToString()), me.popupColor, 34, 1.6f, 120);
                    FxManager.I?.Coin(x, y, 12);
                }
            return m;
        }
    }
}
