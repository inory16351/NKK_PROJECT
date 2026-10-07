using System.Collections.Generic;
using NKK.Data;
using NKK.Humans;
using NKK.Items;
using UnityEngine;

namespace NKK.Rats
{
    // 종별 성장 적용 + 특수 액션 (웹게임 acts.js 이식).
    // 특수 액션: 쥐 테이블 Skill 시트의 액션 행 (조건 + effect_type + value_01~06, duration, cool_time).
    // 성장 테이블 '특수 액션' 노드가 찍혀야 해금. 화면에 보이는 쥐만 발동, 화면 안 동시 진행은 RatManager.maxVisibleActs 개까지.
    public partial class Rat
    {
        // ── 종별 성장 (Progress 조각 레벨 → 노드 레벨) ──
        Dictionary<GrowthEffectType, int> growth = new();
        int GL(GrowthEffectType e) => growth.TryGetValue(e, out var v) ? v : 0;
        float GV(GrowthEffectType e, int i)
        {
            foreach (var n in GameDatabase.Instance.GrowthNodes) if (n.Effect == e) return i == 1 ? n.value_01 : i == 2 ? n.value_02 : n.value_03;
            return 0;
        }
        public int GrowthLevel { get; private set; }
        bool Awakened => GL(GrowthEffectType.Awaken) > 0;

        public void RefreshGrowth()
        {
            GrowthLevel = Progress.I ? Progress.I.Level(codeId) : 0;
            growth = Progress.I ? Progress.I.Tree(GrowthLevel) : new Dictionary<GrowthEffectType, int>();
        }

        float GrowthAtkMult => Mathf.Pow(GV(GrowthEffectType.Atk_Growth, 1), GL(GrowthEffectType.Atk_Growth)) * (Awakened ? GV(GrowthEffectType.Awaken, 1) : 1);
        float GrowthDashMult => 1 + GV(GrowthEffectType.Dash_Speed_Growth, 1) * GL(GrowthEffectType.Dash_Speed_Growth);
        float GrowthCritAdd => GV(GrowthEffectType.Crit_Chance_Growth, 1) * GL(GrowthEffectType.Crit_Chance_Growth);
        float TrickChanceMult => 1 + GV(GrowthEffectType.Trick_Chance_Growth, 1) * GL(GrowthEffectType.Trick_Chance_Growth);
        int ActLv => GL(GrowthEffectType.Action_Unlock);
        float ActRate => 1 + GV(GrowthEffectType.Action_Unlock, 1) * Mathf.Max(0, ActLv - 1);           // 발동 확률 ×, 주기 ÷
        float ActPower => (1 + GV(GrowthEffectType.Action_Power_Growth, 1) * GL(GrowthEffectType.Action_Power_Growth)) * (Awakened ? GV(GrowthEffectType.Awaken, 3) : 1);
        bool ActAwake => GL(GrowthEffectType.Action_Awaken) > 0;

        // ── 특수 액션 ──
        public RatSkillRow Action { get; private set; }
        public EffectType ActionType { get; private set; }
        public bool Acting => act.on;
        [HideInInspector] public float frenzy;         // 광란: 속도·피해 ×1.5
        [HideInInspector] public float temp;           // 소환된 임시 쥐 남은 시간 (0 = 진짜 쥐)
        [HideInInspector] public bool ghost;

        FxManager.Sticker actFx;      // 액션 동안 붙어 다니는 그림 (회오리)
        struct ActState { public bool on; public float t, dur, hitT, ang, dir, P; public int n; public bool done, x; public Item tgt, held; }
        ActState act;
        float actCD, actT = -1;
        readonly Dictionary<Object, float> actHits = new();

        float AV(int i) => Action != null ? Action.V(i) : 0;
        float AC1 => Action != null ? Action.cond1_value_01 : 0;
        float AC1b => Action != null ? Action.cond1_value_02 : 0;
        float AC2 => Action != null ? Action.cond2_value_01 : 0;
        float AW(int i) { var a = GameDatabase.Instance.ActionAwaken.TryGetValue(ActionType, out var r) ? r : null; return a == null ? 0 : i == 1 ? a.value_01 : i == 2 ? a.value_02 : a.value_03; }

        void InitAction()
        {
            GameDatabase.Instance.RatSkills.TryGetValue(Data.action_skill, out var a);
            Action = a; ActionType = a != null ? a.Effect : EffectType.None;
            RefreshGrowth();
        }

        bool CanAct => ActLv > 0 && Action != null && !act.on && Trick == TrickType.None && temp <= 0 && actCD <= 0 && born >= 1 && OnScreen(-0.02f) && Manager.VisibleActs < Manager.maxVisibleActs;

        // 이벤트형 발동 (조건 타입이 같을 때만, 확률 × 해금 레벨 배율)
        public bool ActTrigger(CondType trig, Item it = null)
        {
            if (Action == null || !CanAct) return false;
            var c1 = Action.Cond1;
            if (trig == CondType.Hit_Item && c1 == CondType.Hit_Item && Action.Cond2 == CondType.Combo_Over && Manager.Game.Combo < AC2) return false;
            if (trig == CondType.Hit_Gold_Item) { if (c1 != CondType.Hit_Gold_Item || it == null || !it.Gold) return false; }
            else if (c1 != trig) return false;
            if (c1 == CondType.Interval || c1 == CondType.Ally_Count || c1 == CondType.Drop_Prop) return false;     // 주기형·소품형은 따로
            if (Random.value >= Mathf.Min(1, AC1 * ActRate)) return false;
            return StartAction();
        }

        // 주기형 (Interval · Ally_Count + Interval)
        void ActTick(float dt)
        {
            actCD -= dt;
            if (Action == null || ActLv <= 0) return;
            var c1 = Action.Cond1;
            if (c1 != CondType.Interval && c1 != CondType.Ally_Count) return;
            float cd = c1 == CondType.Interval ? AC1 : AC2;
            if (actT < 0) actT = Random.Range(3f, Mathf.Max(3.1f, cd));
            if ((actT -= dt) > 0) return;
            actT = cd / ActRate * Random.Range(0.8f, 1.2f);
            if (c1 == CondType.Ally_Count)
            {
                int n = 0; foreach (var o in Manager.Rats) if (o != this && Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) < AC1b) n++;
                if (n < AC1) { actT = 2; return; }
            }
            if (CanAct) StartAction(); else actT = 2;
        }

        // 드랍 소품을 주웠을 때 (조건 Drop_Prop)
        public void OnPickup() { if (Action != null && Action.Cond1 == CondType.Drop_Prop && CanAct) StartAction(); }
        public bool DropsProp => Action != null && Action.Cond1 == CondType.Drop_Prop && ActLv > 0;
        public float DropChance => AC1 * ActRate;
        public float PropLife => AC1b;

        public bool StartAction()
        {
            sleep = 0; vx = vy = 0; Trick = TrickType.None; rushT = 0;
            bool x2 = ActAwake;
            float dur = Action.duration;
            switch (ActionType)
            {
                case EffectType.Jump_Slam: case EffectType.Dash_Slash: case EffectType.Rolling_Ball: case EffectType.Throw_Item:
                    if (x2) dur *= AW(2) > 0 ? AW(2) : 1; break;
            }
            act = new ActState { on = true, t = 0, dur = dur, P = ActPower, x = x2, ang = Random.Range(0, Mathf.PI * 2), n = 0 };
            actHits.Clear();
            actCD = Action.cool_time;
            if (ActionType == EffectType.Rolling_Ball) { vx = Mathf.Cos(act.ang) * 620; vy = Mathf.Sin(act.ang) * 620; }
            if (ActionType == EffectType.Tornado) act.dir = Random.Range(0, Mathf.PI * 2);
            var fx = FxManager.I;
            if (fx && ActionType == EffectType.Vortex)
            {
                float R = AV(1) * (x2 ? AW(1) : 1);
                fx.AddSticker("swirl", x, y, 0, R * 2, dur * 0.8f, -600, true, new Color(1, 0.95f, 0.8f, 0.85f));
            }
            if (fx && ActionType == EffectType.Tornado)
            {
                float R = AV(2) * (x2 ? AW(1) : 1);
                actFx = fx.AddSticker("tornado", x, y, R * 0.6f, R * 1.3f, dur, 0, false, new Color(1, 1, 1, 0.85f), 0.07f);
            }
            Manager.Ults?.Charge(this, CondType.Action_Use);
            if (fx) fx.Popup(x, y, Action.skill_name + "!", Data.Grade == Grade.Common ? new Color(1, 0.95f, 0.75f) : Color.Lerp(GradeColor, Color.white, 0.35f), 22, 1.2f, 50 * GradeData.size + 30);
            return true;
        }

        Color GradeColor { get { ColorUtility.TryParseHtmlString(GradeData.color, out var c); return c; } }

        void EndAction()
        {
            if (act.held && act.held.State == Item.ItemState.Rest) act.held.z = 0;
            act.on = false; z = 0; vz = 0; ballScale = 1;
            if (actFx != null) { actFx.End(); actFx = null; }
            LandCheeseLobs();
            StopDash(0.2f, 0.5f);
        }

        float ballScale = 1;

        // ── 치즈 분수 덩어리: 높이 포물선으로 날아가 떨어진 자리에 범위 피해 + 웅덩이 ──
        class CheeseLob { public float x, y, z, vx, vy, vz, dmg; public SpriteRenderer r; }
        readonly List<CheeseLob> cheeseLobs = new();
        const float LOB_G = 1500;
        void LobCheese(float ang, float dist, float tf, float dmg)
        {
            float tx = x + Mathf.Cos(ang) * dist, ty = y + Mathf.Sin(ang) * dist, z0 = 40;
            var l = new CheeseLob { x = x, y = y, z = z0, vx = (tx - x) / tf, vy = (ty - y) / tf, vz = (0.5f * LOB_G * tf * tf - z0) / tf, dmg = dmg };
            var tpl = Manager.bulletTemplate;
            if (tpl)
            {
                l.r = Instantiate(tpl, tpl.transform.parent); l.r.gameObject.SetActive(true);
                if (Manager.cheeseBulletSprite) l.r.sprite = Manager.cheeseBulletSprite;
                l.r.color = Manager.cheeseBulletColor; l.r.transform.localScale = tpl.transform.localScale * Random.Range(1.4f, 2.1f);
                l.r.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
            }
            cheeseLobs.Add(l);
        }
        void StepCheeseLobs(float dt)
        {
            for (int i = cheeseLobs.Count - 1; i >= 0; i--)
            {
                var l = cheeseLobs[i];
                l.x += l.vx * dt; l.y += l.vy * dt; l.vz -= LOB_G * dt; l.z += l.vz * dt;
                if (l.r) { l.r.transform.position = World.ToUnity(l.x, l.y, l.z); l.r.transform.Rotate(0, 0, 540 * dt); l.r.sortingOrder = World.SortOrder(l.y) + 6; }
                if (l.z <= 0) { SplatCheese(l); cheeseLobs.RemoveAt(i); }
            }
        }
        void LandCheeseLobs() { foreach (var l in cheeseLobs) SplatCheese(l); cheeseLobs.Clear(); }
        void SplatCheese(CheeseLob l)
        {
            if (l.r) Destroy(l.r.gameObject);
            if (!Manager.Stage || Manager.Stage.Open.Contains(NKK.Stage.StageManager.RoomOf(l.x, l.y)))
                Manager.Items.Aoe(l.x, l.y, 55, l.dmg, this, false);
            var fx = FxManager.I; if (!fx) return;
            var ch = Manager.cheeseBulletColor;
            if (fx.AddSticker("cheese_puddle", l.x, l.y, 0, Random.Range(50f, 85f), Random.Range(4f, 7f), 0, true) == null) fx.Spill(l.x, l.y, 26, ch);
            fx.Burst(l.x, l.y, 6, 8, ch, Color.white, 90, 260); fx.Ring(l.x, l.y, 55, new Color(ch.r, ch.g, ch.b, 0.85f), 0.28f);
            fx.Spray(l.x, l.y, 6, Random.Range(0, Mathf.PI * 2), 70, 4, Mathf.PI, ch, Color.white);
        }
        Item NearestItem(float R, System.Func<Item, bool> pred = null)
        {
            Item best = null; float bd = R;
            foreach (var it in Manager.Items.InRange(x, y, R + 40))
            {
                if (it.State != Item.ItemState.Rest || (pred != null && !pred(it))) continue;
                float d = Vector2.Distance(new Vector2(it.x, it.y), new Vector2(x, y)); if (d < bd) { bd = d; best = it; }
            }
            return best;
        }

        // ── 액션 진행 (웹게임 actStep). 피해 계수 = 테이블 값 × 공격력 × 성장 위력 ──
        void ActionStep(float dt)
        {
            act.t += dt; act.hitT -= dt;
            float k = act.t / act.dur, atk = Damage * act.P;
            var fx = FxManager.I; var items = Manager.Items;
            void Drag(float d) { float f = Mathf.Max(0, 1 - d * dt); vx *= f; vy *= f; }
            switch (ActionType)
            {
                case EffectType.Thunder:
                {
                    // 두 앞발을 하늘로 → 주변 물건에 번개 연타
                    Drag(12);
                    int n = Mathf.RoundToInt(AV(1) * (act.x ? AW(1) : 1)); float k0 = 0.3f, per = (0.92f - k0) / n;
                    var zc = new Color(0.75f, 0.91f, 1f);
                    if (fx && k < 0.95f && Random.value < 0.35f && OnScreen()) fx.Crackle(x, y, 46, 26, zc, 1, 2.5f);   // 치켜든 앞발에서 지지직
                    if (k >= k0 && act.n < n && k >= k0 + act.n * per)
                    {
                        act.n++;
                        var t = NearestItem(420, it => !actHits.ContainsKey(it));
                        float bx = t ? t.x : x + Random.Range(-220f, 220f), by = t ? t.y : y + Random.Range(-160f, 160f);
                        if (t) actHits[t] = 1;
                        fx?.Bolt(bx, by, zc, 1 + 0.08f * act.P);      // 웹 strikeBolt w: 1 + 0.08 × 위력
                        items.Aoe(bx, by, AV(4), atk * AV(3), this, false);
                        if (t && t.State == Item.ItemState.Rest)
                        {
                            t.Damage(atk * AV(2), this, true, Mathf.Atan2(t.y - y, t.x - x));
                            if (act.x) foreach (var o in items.Nearby(t, 170, 2))
                            {
                                o.Damage(atk * AV(2) * AW(2) / 3, this, false, Mathf.Atan2(o.y - t.y, o.x - t.x));
                                fx?.BoltLine(t.x, t.y, 16, o.x, o.y, 16, zc, 0.2f, 4, 6, 14); fx?.Spark(o.x, o.y, 16, zc, 50, 0.15f);   // 옆 물건으로 튀는 번개
                            }
                        }
                        if (act.n == 1 || Random.value < 0.3f) fx?.Popup(bx, by, Random.value < 0.5f ? "찌릿!!" : "콰릉!", new Color(0.75f, 0.91f, 1f), 18, 0.6f, 50);
                    }
                    break;
                }
                case EffectType.Gun_Kata:
                    // 빙글빙글 돌며 사방으로 총알
                    Drag(8);
                    if (act.hitT <= 0 && k < 0.92f)
                    {
                        act.hitT = AV(2); act.ang += 2.39996f;
                        int dirs = act.x ? Mathf.RoundToInt(AW(1)) : 1;
                        for (int d = 0; d < dirs; d++) { float a = act.ang + d * Mathf.PI * 2 / dirs; Manager.FireBullet(this, x + Mathf.Cos(a) * 16, y + Mathf.Sin(a) * 16, a, AV(3), atk * AV(1)); }
                        face = Mathf.Cos(act.ang) >= 0 ? 1 : -1;
                    }
                    break;
                case EffectType.Jump_Slam:
                {
                    // n번 콩콩 뛰었다가 쾅
                    int hops = Mathf.RoundToInt(act.x ? AW(1) : AV(1));
                    float hk = (act.t / act.dur * hops) % 1; int idx = Mathf.FloorToInt(act.t / act.dur * hops);
                    z = Mathf.Sin(hk * Mathf.PI) * 75;
                    if (idx != act.n && idx <= hops)
                    {
                        act.n = idx;
                        items.Aoe(x, y, AV(3), atk * AV(2), this);
                        fx?.Dust(x, y, 6, 1.2f); fx?.Shake(0.06f);
                        var t = NearestItem(260);
                        if (t) { float a = Mathf.Atan2(t.y - y, t.x - x); vx = Mathf.Cos(a) * 260; vy = Mathf.Sin(a) * 260; face = Mathf.Cos(a) >= 0 ? 1 : -1; }
                    }
                    Drag(1.5f);
                    break;
                }
                case EffectType.Dash_Slash:
                {
                    // 물건 사이를 순간 돌진: 지나간 선 위의 물건 전부 베기
                    int segs = Mathf.RoundToInt(act.x ? AW(1) : AV(1)); float seg = act.dur / segs; int idx = Mathf.FloorToInt(act.t / seg);
                    if (idx != act.n - 1 && idx < segs && act.t - idx * seg < 0.02f + dt)
                    {
                        act.n = idx + 1;
                        var t = NearestItem(AV(3), it => !actHits.ContainsKey(it)) ?? NearestItem(AV(3));
                        float a = t ? Mathf.Atan2(t.y - y, t.x - x) : Random.Range(0, Mathf.PI * 2), len = t ? Vector2.Distance(new Vector2(t.x, t.y), new Vector2(x, y)) + 50 : 200;
                        float ox = x + Mathf.Cos(a) * len, oy = y + Mathf.Sin(a) * len, dvx = 0, dvy = 0;
                        Manager.Stage.Confine(ref ox, ref oy, ref dvx, ref dvy, Radius, x, y, 0);
                        float L = Mathf.Max(1, Vector2.Distance(new Vector2(ox, oy), new Vector2(x, y))), ux = (ox - x) / L, uy = (oy - y) / L;
                        foreach (var it in items.OnLine(x, y, ux, uy, L + 10, 22, 99)) { actHits[it] = 1; var hitIt = it; Manager.Later(0.12f, () => { if (hitIt) hitIt.Damage(atk * AV(2), this, true, a); }); }
                        fx?.Beam(x, y, 12, ox, oy, 12, Color.white, 0.25f);
                        x = ox; y = oy; face = ux >= 0 ? 1 : -1;
                    }
                    vx = vy = 0;
                    break;
                }
                case EffectType.Barrage:
                {
                    int n = Mathf.RoundToInt(AV(1) * (act.x ? AW(1) : 1)); float gap = act.dur * 0.85f / n;
                    if (act.hitT <= 0 && act.n < n)
                    {
                        act.hitT = gap; act.n++;
                        var t = items.RandomRestInRange(x, y, AV(4));
                        float tx = (t ? t.x : x + Random.Range(-200f, 200f)) + Random.Range(-15f, 15f), ty = (t ? t.y : y + Random.Range(-160f, 160f)) + Random.Range(-15f, 15f);
                        items.ThrowBomb(this, tx, ty, AV(3), atk * AV(2), 24, 420, 0.55f, Manager.ThrowSprite(this));
                        face = tx > x ? 1 : -1; bite = 1;
                    }
                    Drag(8);
                    break;
                }
                case EffectType.Sonic_Wave:
                {
                    // 음파: 둥-둥- 퍼지는 충격파 + 동료 광란
                    float R = AV(3) * (act.x ? AW(1) : 1);
                    if (act.hitT <= 0 && act.n < AV(1))
                    {
                        act.hitT = AV(5); act.n++;
                        items.Aoe(x, y, R, atk * AV(2), this, false);
                        if (AV(4) > 0) foreach (var o in Manager.Rats) if (Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) < R) o.frenzy = Mathf.Max(o.frenzy, AV(4));
                        fx?.Ring(x, y, R, AV(4) > 0 ? new Color(0.94f, 0.78f, 0.47f, 0.95f) : new Color(0.75f, 0.89f, 0.92f, 0.95f), 0.5f); fx?.Shake(0.04f);
                    }
                    vx = vy = 0;
                    break;
                }
                case EffectType.Spin_Beam:
                {
                    // 몸을 돌리며 360° 레이저
                    float a0 = act.ang + k * Mathf.PI * 2, len = AV(2);
                    int dirs = act.x ? Mathf.RoundToInt(AW(1)) : 1;
                    for (int d = 0; d < dirs; d++)
                    {
                        float a = a0 + d * Mathf.PI, ux = Mathf.Cos(a), uy = Mathf.Sin(a);
                        fx?.Beam(x, y, 20, x + ux * len, y + uy * len, 20, codeId == "alien" ? new Color(0.62f, 0.84f, 0.66f) : new Color(0.91f, 0.47f, 0.42f), 0.05f);
                        foreach (var it in items.OnLine(x, y, ux, uy, len, 12, 99))
                        {
                            if (actHits.TryGetValue(it, out var tt) && tt > Time.time) continue;
                            actHits[it] = Time.time + AV(3); it.Damage(atk * AV(1), this, false, a);
                        }
                    }
                    face = Mathf.Cos(a0) >= 0 ? 1 : -1; vx = vy = 0;
                    break;
                }
                case EffectType.Breath:
                {
                    // 부채꼴로 휘두르는 브레스 (각성: 한 바퀴)
                    float range = AV(2), half = AV(4) * Mathf.Deg2Rad;
                    if (act.t <= dt) act.dir = face > 0 ? 0 : Mathf.PI;
                    float a = act.x ? act.dir + k * Mathf.PI * 2 : act.dir + Mathf.Sin(act.t * 5) * 0.8f;
                    vx = vy = 0; face = Mathf.Cos(a) >= 0 ? 1 : -1;
                    if (act.hitT <= 0)
                    {
                        act.hitT = AV(3);
                        foreach (var it in items.InRange(x, y, range + 60))
                        {
                            if (it.State != Item.ItemState.Rest) continue;
                            float dx = it.x - x, dy = it.y - y, d = Mathf.Sqrt(dx * dx + dy * dy), da = Mathf.DeltaAngle(a * Mathf.Rad2Deg, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                            if (d < range + it.R && Mathf.Abs(da) < half) it.Damage(atk * AV(1), this, false, Mathf.Atan2(dy, dx));
                        }
                    }
                    bool water = codeId == "firefighter";
                    fx?.Spray(x, y, 18, a, range, 5, 0.4f, water ? new Color(0.62f, 0.83f, 0.9f) : new Color(0.95f, 0.79f, 0.3f), Color.white);
                    break;
                }
                case EffectType.Rolling_Ball:
                {
                    // 거대한 공이 되어 핀볼처럼
                    ballScale = Mathf.Min(act.x ? AW(1) : AV(2), 1 + act.t * 6);
                    float rad = Radius * ballScale, sp = Mathf.Sqrt(vx * vx + vy * vy);
                    if (sp < AV(3)) { vx *= AV(3) / Mathf.Max(1, sp); vy *= AV(3) / Mathf.Max(1, sp); }
                    foreach (var it in items.InRange(x, y, rad + 60))
                    {
                        if (it.State != Item.ItemState.Rest) continue;
                        float dx = x - it.x, dy = y - it.y, d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > rad + it.R || (actHits.TryGetValue(it, out var tt) && tt > Time.time)) continue;
                        actHits[it] = Time.time + AV(4);
                        float nx = dx / Mathf.Max(0.01f, d), ny = dy / Mathf.Max(0.01f, d), dot = vx * nx + vy * ny;
                        if (dot < 0) { vx -= 2 * dot * nx; vy -= 2 * dot * ny; }
                        it.Damage(atk * AV(1), this, false, Mathf.Atan2(-ny, -nx));
                    }
                    walk += dt * 40;
                    break;
                }
                case EffectType.Summon:
                    vx = vy = 0;
                    if (!act.done && act.t > 0.3f)
                    {
                        act.done = true;
                        int n = Mathf.RoundToInt(AV(1) * (act.x ? AW(1) : 1));
                        for (int i = 0; i < n; i++) { float a = (float)i / n * Mathf.PI * 2; Manager.SpawnTemp(this, x + Mathf.Cos(a) * 40, y + Mathf.Sin(a) * 30, AV(2)); }
                    }
                    break;
                case EffectType.Vortex:
                {
                    // 빨아들이기 → 쾅
                    float R = AV(1) * (act.x ? AW(1) : 1);
                    vx = vy = 0;
                    if (k < 0.8f) items.Pull(x, y, R, 1400 * dt);
                    else if (!act.done)
                    {
                        act.done = true;
                        items.Aoe(x, y, R * AV(3), atk * AV(2), this, false);
                        fx?.Ring(x, y, R * AV(3), new Color(1, 0.95f, 0.75f), 0.5f); fx?.Anim("hit", x, y, 20, R * AV(3) / 90); fx?.Shake(0.15f); fx?.Burst(x, y, 20, 20, Color.white, new Color(1, 0.95f, 0.75f), 200, 500);
                    }
                    break;
                }
                case EffectType.Meteor:
                {
                    int n = Mathf.RoundToInt(AV(1) * (act.x ? AW(1) : 1)); float gap = act.dur * 0.8f / n;
                    vx = vy = 0;
                    if (act.hitT <= 0 && act.n < n)
                    {
                        act.hitT = gap; act.n++;
                        var t = items.RandomRestInRange(x, y, AV(4));
                        float tx = (t ? t.x : x + Random.Range(-260f, 260f)) + Random.Range(-10f, 10f), ty = (t ? t.y : y + Random.Range(-200f, 200f)) + Random.Range(-10f, 10f);
                        items.DropMeteor(this, tx, ty, AV(3), atk * AV(2), Manager.MeteorSprite(this));
                    }
                    break;
                }
                case EffectType.Midas:
                    vx = vy = 0;
                    if (!act.done && act.t > 0.4f)
                    {
                        act.done = true;
                        float R = AV(1) * (act.x ? AW(1) : 1);
                        foreach (var it in items.InRange(x, y, R + 40)) if (it.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(it.x, it.y), new Vector2(x, y)) < R) it.MakeGold(10, 2);
                        fx?.Ring(x, y, R, new Color(0.95f, 0.76f, 0.31f), 0.6f);
                    }
                    break;
                case EffectType.Tornado:
                {
                    // 회오리: 빙빙 돌며 이리저리
                    act.dir += Random.Range(-3f, 3f) * dt;
                    vx = Mathf.Cos(act.dir) * AV(4); vy = Mathf.Sin(act.dir) * AV(4);
                    float R = AV(2) * (act.x ? AW(1) : 1);
                    actFx?.Move(x, y, R * 0.6f);
                    if (act.hitT <= 0) { act.hitT = AV(3); items.Aoe(x, y, R, atk * AV(1), this, false); fx?.Ring(x, y, R, new Color(1, 1, 1, 0.6f), 0.25f); fx?.Dust(x, y, 2, 0.8f); }
                    walk += dt * 40;
                    break;
                }
                case EffectType.Truth_Point:
                {
                    // 진실은 언제나 하나!: 체력이 가장 높은 물건을 손가락으로 가리킴(밸류_03 초) → 공격력 × 밸류_01. 밸류_02 = 찾는 반경. 각성: AW1 개까지
                    vx = vy = 0;
                    if (!act.done && act.t >= (AV(3) > 0 ? AV(3) : act.dur * 0.45f))
                    {
                        act.done = true;
                        float R = AV(2) > 0 ? AV(2) : 900;
                        var list = new List<Item>();
                        foreach (var it in items.InRange(x, y, R)) if (it.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(it.x, it.y), new Vector2(x, y)) < R) list.Add(it);
                        list.Sort((a, b) => b.hp.CompareTo(a.hp));
                        int n = Mathf.Min(list.Count, act.x ? Mathf.Max(1, Mathf.RoundToInt(AW(1))) : 1);
                        for (int i = 0; i < n; i++)
                        {
                            var t = list[i];
                            fx?.Beam(x, y, 26, t.x, t.y, 20, Color.white, 0.3f); fx?.Anim("hit", t.x, t.y, 30, 1.8f);
                            t.Damage(atk * AV(1), this, true, Mathf.Atan2(t.y - y, t.x - x));
                        }
                        if (n > 0) { fx?.Shake(0.12f); fx?.Hitstop(0.06f); }
                    }
                    break;
                }
                case EffectType.Cheese_Fountain:
                {
                    // 치즈 분수: 밸류_01 방향으로 치즈 탄환을 밸류_03 초 간격으로 밸류_02 번 (번마다 밸류_06 도씩 돌림). 탄환 피해 = 공격력 × 밸류_04, 밸류_05 = 탄환 속도. 각성: 횟수 × AW1
                    //   + 번마다 사방 무작위 탄환(속도·사거리 제각각) + 높이 솟구쳐 떨어지는 치즈 덩어리 (떨어진 자리 범위 피해 + 치즈 웅덩이)
                    vx = vy = 0; z = Mathf.Abs(Mathf.Sin(act.t * 10)) * 10;
                    int waves = Mathf.RoundToInt(AV(2) * (act.x && AW(1) > 0 ? AW(1) : 1)), dirs = Mathf.Max(1, Mathf.RoundToInt(AV(1)));
                    float bsp = AV(5) > 0 ? AV(5) : 520, bdmg = atk * AV(4);
                    var ch = Manager.cheeseBulletColor;
                    if (act.n < waves && act.t >= 0.15f + act.n * AV(3))
                    {
                        for (int d = 0; d < dirs; d++)
                        {
                            float an = act.ang + d * Mathf.PI * 2 / dirs + act.n * AV(6) * Mathf.Deg2Rad;
                            Manager.FireBullet(this, x, y, an, bsp, bdmg, 0.45f, true, Manager.cheeseBulletSprite, ch);
                        }
                        // 사방 무작위 탄환 (속도·사거리 제각각)
                        for (int d = 0; d < dirs; d++)
                            Manager.FireBullet(this, x, y, Random.Range(0, Mathf.PI * 2), bsp * Random.Range(0.5f, 1.4f), bdmg * 0.6f, Random.Range(0.3f, 0.8f), true, Manager.cheeseBulletSprite, ch);
                        // 높이 솟구치는 덩어리 (액션 안에 떨어지도록 비행 시간 제한)
                        float left = act.dur - act.t - 0.03f;
                        if (left > 0.3f) for (int l = 0, nl = Random.Range(3, 5); l < nl; l++) LobCheese(Random.Range(0, Mathf.PI * 2), Random.Range(120f, 460f), Mathf.Min(left, Random.Range(0.5f, 0.85f)), bdmg * 0.8f);
                        act.n++;
                        if (fx)
                        {
                            fx.Dust(x, y, 4, 0.9f); fx.Ring(x, y, 70, new Color(ch.r, ch.g, ch.b, 0.8f), 0.3f);
                            fx.Burst(x, y, 40, 10, ch, Color.white, 120, 380); fx.Shake(0.03f);
                        }
                    }
                    // 분수 물줄기: 위로 솟는 치즈 방울 + 사방 분사
                    if (fx && OnScreen())
                    {
                        fx.Burst(x, y, 46 + Random.Range(0f, 20f), 2, ch, new Color(1, 0.95f, 0.75f), 40, 200, 3, 7);
                        if (act.hitT <= 0) { act.hitT = 0.06f; fx.Spray(x, y, 30, Random.Range(0, Mathf.PI * 2), Random.Range(80f, 180f), 5, 0.5f, ch, new Color(1, 0.95f, 0.75f)); }
                    }
                    StepCheeseLobs(dt);
                    break;
                }
                case EffectType.Cheer:
                    // 콩콩 뛰며 응원 → 주변 동료 광란 (각성: 화면 전체)
                    z = Mathf.Abs(Mathf.Sin(act.t * 9)) * 26; vx = vy = 0;
                    if (act.hitT <= 0)
                    {
                        act.hitT = 0.5f;
                        foreach (var o in Manager.Rats) if (act.x ? o.OnScreen() : Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) < AV(2)) o.frenzy = Mathf.Max(o.frenzy, AV(1));
                        fx?.Ring(x, y, act.x ? 400 : AV(2), new Color(0.91f, 0.64f, 0.63f, 0.8f), 0.5f);
                    }
                    break;
                case EffectType.Feast:
                {
                    // 앞의 물건에 달라붙어 초고속 연타
                    var t = act.tgt;
                    if (!t || t.State != Item.ItemState.Rest) t = act.tgt = NearestItem(260);
                    if (t)
                    {
                        float a = Mathf.Atan2(y - t.y, x - t.x), dist = t.R + Radius + 2;
                        x += (t.x + Mathf.Cos(a) * dist - x) * Mathf.Min(1, dt * 14); y += (t.y + Mathf.Sin(a) * dist - y) * Mathf.Min(1, dt * 14);
                        face = t.x > x ? 1 : -1;
                        if (act.hitT <= 0)
                        {
                            act.hitT = act.x ? AW(1) : AV(2); bite = 1;
                            t.Damage(atk * AV(1), this, false, a + Mathf.PI);
                            if (act.n++ % 4 == 0) fx?.Popup(t.x + Random.Range(-14f, 14f), t.y, Random.value < 0.5f ? "냠!" : "다닥!", Color.white, 15, 0.5f, 36);
                        }
                    }
                    vx = vy = 0;
                    break;
                }
                case EffectType.Throw_Item:
                {
                    // 번쩍 들어서 휙! (각성: 3번)
                    int reps = Mathf.RoundToInt(act.x ? AW(1) : AV(1)); float cyc = act.dur / reps, ck = (act.t % cyc) / cyc; int idx = Mathf.FloorToInt(act.t / cyc);
                    vx = vy = 0;
                    if (idx != act.n - 1 && idx < reps) { act.n = idx + 1; act.held = NearestItem(AV(3)); }
                    var h = act.held;
                    if (h && h.State == Item.ItemState.Rest)
                    {
                        float lift = Mathf.Min(1, ck / 0.35f);
                        h.x += (x - h.x) * Mathf.Min(1, dt * 12); h.y += (y - 2 - h.y) * Mathf.Min(1, dt * 12); h.z = lift * (44 + 30 * GradeData.size);
                        if (ck > 0.62f)
                        {
                            // 물건이 많은 쪽으로 던짐
                            float bx = 0, by = 0; foreach (var o in items.InRange(x, y, 500)) { bx += o.x - x; by += o.y - y; }
                            float a = bx != 0 || by != 0 ? Mathf.Atan2(by, bx) : Random.Range(0, Mathf.PI * 2);
                            h.ThrowBy(this, a, 950, 320, AV(2));
                            face = Mathf.Cos(a) >= 0 ? 1 : -1; act.held = null;
                        }
                    }
                    break;
                }
            }
            if (act.t >= act.dur) EndAction();
        }

        // 액션 자세 (웹게임 actPose)
        void ActionPose(ref RatRig.Pose p)
        {
            float t = Time.time, k = act.t / act.dur;
            switch (ActionType)
            {
                case EffectType.Thunder: p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; p.front = 2.95f + Mathf.Sin(t * 50) * 0.05f; p.farFront = 2.8f + Mathf.Cos(t * 50) * 0.05f; p.head = -0.55f; p.tail = 1.4f + Mathf.Sin(t * 20) * 0.1f; break;
                case EffectType.Gun_Kata: p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; p.front = 1.7f + Mathf.Sin(t * 40) * 0.25f; p.farFront = 1.5f - Mathf.Sin(t * 40) * 0.25f; p.head = -0.2f; p.tail = 0.8f + Mathf.Sin(t * 30) * 0.3f; break;
                case EffectType.Jump_Slam: if (z > 10) { p.front = 2.7f; p.farFront = 2.5f; p.back = -1.2f; p.farBack = -1; p.tail = 1.2f; p.head = -0.3f; } else { p.front = 0.3f; p.farFront = 0.2f; p.back = -0.3f; p.farBack = -0.2f; p.head = 0.35f; p.sy = 0.85f; p.sx = 1.12f; } break;
                case EffectType.Dash_Slash: p.front = 1.8f; p.farFront = 1.2f; p.back = -1.4f; p.farBack = -1.2f; p.head = -0.2f; p.tail = -0.6f; p.sx = 1.12f; p.sy = 0.92f; break;
                case EffectType.Barrage: p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; p.front = bite > 0 ? 2.6f * bite : 0.2f; p.farFront = 1.2f; p.head = -0.15f; p.tail = 0.8f; break;
                case EffectType.Sonic_Wave: p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; p.front = 1.6f; p.farFront = 1.3f; p.head = Mathf.Sin(t * 18) * 0.45f; p.tail = 1 + Mathf.Sin(t * 9) * 0.3f; p.bob = -Mathf.Abs(Mathf.Sin(t * 9)) * 3; break;
                case EffectType.Spin_Beam: p.tilt = -0.3f; p.front = 1.1f; p.farFront = 0.9f; p.head = -0.3f; p.tail = 0.9f; break;
                case EffectType.Breath: p.head = -0.35f; p.headX = -4; p.front = 0.4f; p.farFront = 0.3f; p.back = -0.4f; p.tail = 1.1f; p.sx = 1.05f; break;
                case EffectType.Rolling_Ball: p.front = 1.9f; p.farFront = 1.9f; p.back = -1.9f; p.farBack = -1.9f; p.head = 0.8f; p.headX = 4; p.tail = -1.6f; p.sx = p.sy = 0.9f; break;
                case EffectType.Summon: p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; p.front = 2.7f; p.farFront = 2.4f; p.head = -0.35f; p.tail = 1.2f; break;
                case EffectType.Vortex: p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; p.front = 2.3f + Mathf.Sin(t * 20) * 0.3f; p.farFront = 2.1f - Mathf.Sin(t * 20) * 0.3f; p.head = -0.25f; p.tail = Mathf.Sin(t * 16) * 1.3f; break;
                case EffectType.Meteor: p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; p.front = 2.8f; p.farFront = 2.5f; p.head = -0.45f; p.tail = 1.1f; break;
                case EffectType.Midas: p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; p.front = 2.5f; p.farFront = 2.4f; p.head = -0.3f; p.tail = 1.3f; break;
                case EffectType.Tornado: p.front = 1.5f; p.farFront = -1.5f; p.back = 1.2f; p.farBack = -1.2f; p.tail = 1.4f; p.head = -0.2f; break;
                case EffectType.Truth_Point: p.tilt = -0.5f; p.back = -0.25f; p.farBack = 0.25f; p.front = 1.9f; p.farFront = 0.4f; p.head = -0.25f; p.tail = 1.2f; break;
                case EffectType.Cheese_Fountain: { p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; float s2 = Mathf.Sin(t * 12); p.front = 2.5f + s2 * 0.4f; p.farFront = 2.3f - s2 * 0.4f; p.head = -0.4f + Mathf.Sin(t * 20) * 0.08f; p.tail = 1.3f + Mathf.Sin(t * 16) * 0.3f; p.sy = 1 + Mathf.Abs(Mathf.Sin(t * 10)) * 0.08f; p.sx = 2 - p.sy; break; }
                case EffectType.Cheer: { p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; float s = Mathf.Sin(t * 9); p.front = 2.6f * Mathf.Max(0.3f, s); p.farFront = 2.6f * Mathf.Max(0.3f, -s); p.head = -0.3f; p.tail = 1.3f; break; }
                case EffectType.Feast: p.front = 0.9f + Mathf.Sin(t * 50) * 0.5f; p.farFront = 0.8f - Mathf.Sin(t * 50) * 0.5f; p.head = 0.25f; p.tail = 0.6f; break;
                case EffectType.Throw_Item: p.tilt = -0.6f; p.back = -0.25f; p.farBack = 0.25f; p.front = act.held ? 2.9f : 1.4f; p.farFront = act.held ? 2.7f : 1; p.head = -0.35f; p.tail = 1; break;
            }
        }

        // 액션 중 몸 회전 (굴러가는 공·회오리)
        void ActionTransform(ref float rot, ref float sx, ref float sy, ref float lift)
        {
            if (!act.on) return;
            if (ActionType == EffectType.Rolling_Ball) { rot = act.t * 22 * face; sx = sy = 0.8f * ballScale; lift = Mathf.Abs(Mathf.Sin(act.t * 9)) * 10; }
            else if (ActionType == EffectType.Tornado) { rot = act.t * 30; }
        }
    }
}
