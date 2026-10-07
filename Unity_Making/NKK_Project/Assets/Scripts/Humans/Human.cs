using System.Collections.Generic;
using NKK.Data;
using NKK.Items;
using NKK.Rats;
using UnityEngine;

namespace NKK.Humans
{
    // 연구소 사람 (웹게임 humans.js 이식). 상태: Walk(어슬렁) → Panic(쥐 보고 기겁·도망) → Fly(체력 0 → 병맛 비행)
    // → Splat(벽에 철퍼덕, 한 번) → 땅에 2~3번 통통 → 펑! 하고 사라짐. 위치는 게임 단위.
    public class Human : MonoBehaviour
    {
        public enum HState { Walk, Panic, Fly, Splat, Dead }
        enum FlyStyle { Spin, Swim, Star, Cannon, Flail }

        public HumanRig rig;
        [Tooltip("접지 그림자 (자식)")] public SpriteRenderer shadow;

        [Header("상태 (실행 중 확인용)")]
        public string codeId;
        public HState State = HState.Walk;
        public float x, y, z, vx, vy, vz, hp, hpMax, value;

        public HumanRow Data { get; private set; }
        public float R => mgr.humanRadius;
        public Rat By;
        ItemManager mgr;
        int face = 1, bounces, maxBounce, air;
        float t, walk, rot, vr, sq = 1, appear, jit, alpha = 1, hitT, sayCD;
        bool splatted;
        FlyStyle style;
        readonly HashSet<Object> hitSet = new();
        FxManager.HpBar bar;

        public void Init(ItemManager m, HumanRow row, HumanArtLibrary.Entry art, float px, float py)
        {
            mgr = m; Data = row; codeId = row.code_id; name = $"Human_{row.code_id}";
            x = px; y = py; face = Random.value < 0.5f ? 1 : -1; t = Random.Range(1f, 3f); walk = Random.Range(0f, 6f);
            int zi = m.Game.Floor - 1;
            hpMax = hp = 12 * row.hp_mul * Mathf.Pow(m.itemHpGrow, zi) * m.humanHpMul;
            value = 3 * row.value_mul * Mathf.Pow(m.valueGrow, zi) * m.humanValueMul * CommonSkill.CheeseMul;
            rig.Build(art);
            // 그림자는 사람 정렬 그룹 밖(바닥 바로 위)에 → 실행 중엔 그림자 묶음으로 옮김
            if (shadow && m.Rats && m.Rats.shadowRoot) { shadow.transform.SetParent(m.Rats.shadowRoot, true); shadow.sortingOrder = m.Rats.shadowSortOrder; }
        }

        void OnDestroy()
        {
            if (FxManager.I) FxManager.I.ReleaseHpBar(bar);
            if (shadow && shadow.transform.parent != transform) Destroy(shadow.gameObject);
        }

        void Say(string situation, float chance = 1)
        {
            if (sayCD > 0 || Random.value > chance) return;
            var line = GameDatabase.Instance.HumanLine(situation, Data.human_id);
            if (line != null && FxManager.I) { FxManager.I.Popup(x, y, line, Color.white, 18, 1.1f, mgr.humanHeight + 20); sayCD = 1.2f; }
        }

        // ── 피해 ──
        public bool Damage(float dmg, Rat by, float ang, bool crit = false)
        {
            if (State == HState.Fly || State == HState.Splat || State == HState.Dead || appear < 1) return false;
            hitT = 0.25f; jit = 2;
            hp -= dmg; if (by) By = by;
            if (State == HState.Walk) Panic();
            if (hp > 0)
            {
                vx += Mathf.Cos(ang) * 140; vy += Mathf.Sin(ang) * 140;   // 맞은 방향으로 휘청
                Say("Hit", 0.2f);
                return true;
            }
            Launch(ang, Random.Range(420f, 560f) * (crit ? 1.3f : 1));
            return true;
        }

        // 필살기에 휘말림 (웹게임 blastActor): 피해만큼, 0 이면 멀리 날아감 · 남으면 크게 휘청
        public bool Blast(float ang, float spd, float dmg, Rat by)
        {
            if (State == HState.Fly || State == HState.Splat || State == HState.Dead || appear < 1) return false;
            hp -= dmg; if (by) By = by; hitT = 0.25f; jit = 4;
            if (hp <= 0) { Launch(ang, spd); return true; }
            if (State == HState.Walk) Panic();
            vx += Mathf.Cos(ang) * spd * 0.6f; vy += Mathf.Sin(ang) * spd * 0.6f; vz = Mathf.Max(vz, 320);
            Say("Hit", 0.4f);
            return true;
        }

        public void HitByItem(float dmg, Rat by, float ang)
        {
            if (!Damage(dmg, by, ang) || State != HState.Fly) { vx += Mathf.Cos(ang) * 260; vy += Mathf.Sin(ang) * 260; vz = Mathf.Max(vz, 220); jit = 4; }
            FxManager.I?.Popup(x, y, RandomOf("퍽!", "아야!!", "뿅!", "(맞음)"), Color.white, 20, 0.6f, mgr.humanHeight * 0.8f);
            Say("Item_Hit");
        }

        void Launch(float ang, float speed)
        {
            if (By) mgr.Rats.Ults?.Charge(By, CondType.Defeat_Human);      // 사람 퇴치 → 그 종 필살기 게이지
            State = HState.Fly; maxBounce = 2 + (Random.value < 0.5f ? 1 : 0); bounces = 0;
            vx = Mathf.Cos(ang) * speed * 1.3f; vy = Mathf.Sin(ang) * speed * 1.3f; vz = Random.Range(560f, 760f);
            style = (FlyStyle)Random.Range(0, 5);
            vr = (style == FlyStyle.Swim ? 0 : style == FlyStyle.Star ? Random.Range(3f, 5f) : Random.Range(10f, 16f)) * (Random.value < 0.5f ? -1 : 1);
            if (style == FlyStyle.Swim) rot = (vx >= 0 ? 1 : -1) * Mathf.PI / 2;
            face = vx >= 0 ? 1 : -1; hitSet.Clear(); air = 0; sayCD = 0;
            Say("Fly");
            FxManager.I?.Stars(x, y, 80, 6, Color.white, new Color(1, 0.89f, 0.6f));
        }

        void Panic()
        {
            if (State != HState.Walk && State != HState.Panic) return;
            if (State == HState.Walk) { z = 0; vz = 260; jit = 3; }        // 깜짝! 제자리 점프
            State = HState.Panic; t = Random.Range(3f, 5f);
        }

        // 날린 쥐 공격력 기준 타격력 (물건 flyDmg 와 같은 식)
        float FlyDamage(bool noWeight = false)
        {
            float force = (By ? By.Damage : mgr.avgRatDamage) * mgr.flyForceMul * Mathf.Clamp(Mathf.Sqrt(vx * vx + vy * vy) / 600, 0.6f, 1.4f);
            return ((noWeight ? 0 : hpMax * mgr.flyWeight) + force) * (1 + mgr.flyStyle * Mathf.Min(air, mgr.airMax));
        }

        public void Tick(float dt)
        {
            appear = Mathf.Min(1, appear + dt * 3); hitT = Mathf.Max(0, hitT - dt); jit = Mathf.Max(0, jit - dt * 12); sayCD -= dt;
            float px = x, py = y;
            switch (State)
            {
                case HState.Walk:
                    t -= dt;
                    if (t <= 0) { t = Random.Range(1.5f, 4f); float a = Random.Range(0, Mathf.PI * 2), s = Random.value < 0.3f ? 0 : 55 * Data.speed_mul; vx = Mathf.Cos(a) * s; vy = Mathf.Sin(a) * s; }
                    if (mgr.Rats.NearestRat(x, y, 240)) Panic();
                    break;
                case HState.Panic:
                {
                    t -= dt;
                    var r = mgr.Rats.NearestRat(x, y, 420);
                    if (r)
                    {
                        t = Mathf.Max(t, 1.5f);
                        float a = Mathf.Atan2(y - r.y, x - r.x) + Mathf.Sin(Time.time * 3 + walk) * 0.5f, s = 240 * Data.speed_mul;
                        vx += (Mathf.Cos(a) * s - vx) * Mathf.Min(1, dt * 5); vy += (Mathf.Sin(a) * s - vy) * Mathf.Min(1, dt * 5);
                    }
                    if (Random.value < dt * 0.8f) Say("Panic");
                    if (t <= 0) { State = HState.Walk; t = 2; }
                    break;
                }
                case HState.Fly: FlyStep(dt); break;
                case HState.Splat:
                    t -= dt; vx = vy = 0;
                    if (t < 0.35f) z = Mathf.Max(0, z - dt * 320);
                    if (t <= 0) { State = HState.Fly; vz = 320; vx = -face * 320; vy = Random.Range(-60f, 60f); }   // 벽에서 튕겨 나와 다시 날아감
                    break;
            }
            sq += (1 - sq) * Mathf.Min(1, dt * 8);
            if (State == HState.Walk || State == HState.Panic)
            {
                if (z > 0 || vz > 0) { vz -= 1600 * dt; z = Mathf.Max(0, z + vz * dt); if (z <= 0) vz = 0; }
                x += vx * dt; y += vy * dt;
                mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 0.3f);
                if (Mathf.Abs(vx) > 8) face = vx > 0 ? 1 : -1;
                walk += dt * Mathf.Sqrt(vx * vx + vy * vy) / (State == HState.Panic ? 7 : 14);
            }
        }

        // ── 게임 오버 습격 (웹 updateGameOver 경비원): GameOver 가 Tick 대신 부름. 벽 무시, 목표 쥐로 곧장 ──
        public void BeginRaid() { appear = 1; State = HState.Walk; vx = vy = 0; }
        public void RaidSay(string situation) { sayCD = 0; Say(situation); }
        // 목표에 닿으면 true
        public bool RaidStep(float dt, Rat target, float spd, float reach)
        {
            sayCD -= dt; walk += dt * 12;
            if (!target) { vx = vy = 0; return false; }
            float dx = target.x - x, dy = target.y - y, d = Mathf.Sqrt(dx * dx + dy * dy);
            face = dx >= 0 ? 1 : -1;
            if (d < reach) { vx = vy = 0; return true; }
            vx = dx / d * spd; vy = dy / d * spd; x += vx * dt; y += vy * dt;
            return false;
        }

        void FlyStep(float dt)
        {
            float px = x, py = y;
            vz -= 1500 * dt; x += vx * dt; y += vy * dt; z += vz * dt; rot += vr * dt;
            bool wall = mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R * 0.8f, px, py, 0.3f, (i, j, di, dj, v) => { if (v > 250) mgr.Stage.DamageWall(i, j, di, dj, FlyDamage(true) * 0.5f, By); });
            if (wall && z > 30 && Mathf.Sqrt(vx * vx + vy * vy) > 200 && !splatted)
            {
                // 벽에 철퍼덕! (한 번만)
                splatted = true; State = HState.Splat; t = 0.7f; rot = 0; face = -face; sq = 0.7f;
                FxManager.I?.Shake(0.15f); FxManager.I?.Dust(x, y, 6, 1.2f);
                return;
            }
            // 날아가며 물건·사람을 들이받음 (볼링핀처럼)
            if (z < 120)
            {
                foreach (var o in mgr.InRange(x, y, R + 80))
                {
                    if (o.State != Item.ItemState.Rest || hitSet.Contains(o) || Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) > o.R + R) continue;
                    hitSet.Add(o); mgr.Game.Earn(o.value * 0.3f);
                    if (By) o.By = By;
                    o.Damage(FlyDamage(), null, false, Mathf.Atan2(vy, vx));
                }
                foreach (var o in mgr.Humans)
                {
                    if (o == this || hitSet.Contains(o) || o.State == HState.Fly || o.State == HState.Dead || Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) > o.R + R || Mathf.Abs(o.z - z) > 90) continue;
                    hitSet.Add(o);
                    o.hp = 0; o.By = By; o.Launch(Mathf.Atan2(o.y - y, o.x - x), 480);
                    FxManager.I?.Popup(o.x, o.y, "스트라이크!!", new Color(0.95f, 0.76f, 0.31f), 24, 0.9f, 120);
                }
            }
            // 쥐가 헤딩으로 받아침 → 더 높이 (사람 저글링)
            if (z < 60 && vz < 0)
                foreach (var r in mgr.Rats.Rats)
                {
                    if (air >= mgr.airMax || hitSet.Contains(r) || Vector2.Distance(new Vector2(r.x, r.y), new Vector2(x, y)) > R + r.Radius) continue;
                    hitSet.Add(r); r.Header();
                    vz = Random.Range(420f, 560f); air++; mgr.Game.Earn(value * 0.3f * air);
                    FxManager.I?.Popup(x, y, $"AIR x{air}", new Color(0.61f, 0.96f, 1f), 18 + air * 2, 0.7f, z + 80);
                }
            if (z <= 0 && vz < 0) { z = 0; Bounce(); }
        }

        void Bounce()
        {
            bounces++;
            if (bounces <= maxBounce)
            {
                // 고무공처럼 통통 + 떨어진 자리 충격파 (날린 쥐 공격력 기준)
                vz = Mathf.Max(420, -vz * 0.85f); vx *= 0.95f; vy *= 0.95f; sq = 0.55f; vr *= -0.8f;
                mgr.Shock(x, y, 80, FlyDamage() * 0.4f, By);
                FxManager.I?.Dust(x, y, 5, 1); FxManager.I?.Ring(x, y, 80, Color.white);
                return;
            }
            Poof();
        }

        void Poof()
        {
            State = HState.Dead;
            float gain = value * (1 + 0.5f * Mathf.Min(air, mgr.airMax));
            mgr.Game.OnSmash(gain, 2);
            mgr.Shock(x, y, 110, hpMax * 0.4f, null);
            var fx = FxManager.I; if (!fx) return;
            // 뭉게뭉게 흰 연기 + 색종이 + 별 + "펑!"
            fx.Dust(x, y, 14, 2.2f);
            fx.Anim("poof", x, y, 30, 1.6f);
            fx.Burst(x, y, 60, 18, new Color(1, 0.95f, 0.75f), new Color(0.91f, 0.64f, 0.63f), 120, 360);
            fx.Stars(x, y, 60, 10, Color.white, new Color(1, 0.89f, 0.6f));
            fx.Popup(x, y, GameDatabase.Instance.HumanLine("Poof", 0) ?? "펑!!", new Color(1, 0.95f, 0.75f), 30, 0.9f, 130);
            fx.Popup(x, y, "치즈+" + GameManager.Format(gain * mgr.Game.ComboMult), new Color(0.94f, 0.78f, 0.47f), 28, 1.2f, 80);
            fx.Coin(x, y, 4); fx.Shake(0.12f);
        }

        // 웹게임 humanPose
        HumanRig.Pose MakePose()
        {
            var p = new HumanRig.Pose { sx = 1, sy = 1 };
            float tt = Time.time, sp = Mathf.Sqrt(vx * vx + vy * vy);
            switch (State)
            {
                case HState.Walk:
                    if (sp > 12) { p.legN = Mathf.Sin(walk) * 0.45f; p.legF = -p.legN; p.armN = -Mathf.Sin(walk) * 0.35f; p.armF = -p.armN; p.bob = Mathf.Abs(Mathf.Cos(walk)) * 3; }
                    else p.armN = Mathf.Sin(tt * 1.5f + x) * 0.05f;
                    break;
                case HState.Panic:
                    p.scared = true; p.legN = Mathf.Sin(walk * 2) * 1.2f; p.legF = Mathf.Sin(walk * 2 + Mathf.PI) * 1.2f;
                    p.armN = Mathf.PI * 0.85f + Mathf.Sin(tt * 20) * 0.3f; p.armF = Mathf.PI * 0.8f + Mathf.Cos(tt * 22) * 0.3f; p.lean = -0.25f; p.bob = Mathf.Abs(Mathf.Sin(walk * 2)) * 8;
                    break;
                case HState.Fly:
                    p.scared = true;
                    if (style == FlyStyle.Swim) { float k = Mathf.Sin(tt * 9); p.armN = Mathf.PI * 0.5f + k * 1.2f; p.armF = Mathf.PI * 0.5f - k * 1.2f; p.legN = 0.3f + Mathf.Sin(tt * 9 + 1) * 0.6f; p.legF = -0.3f - Mathf.Sin(tt * 9 + 1) * 0.6f; }
                    else if (style == FlyStyle.Star) { p.armN = 2.3f; p.armF = -2.3f; p.legN = 0.55f; p.legF = -0.55f; }
                    else if (style == FlyStyle.Cannon) { p.armN = 1.9f; p.armF = 1.7f; p.legN = -1.7f; p.legF = -1.5f; p.sx = p.sy = 0.85f; }
                    else { p.armN = Mathf.Sin(tt * 28) * 2; p.armF = Mathf.Cos(tt * 25) * 2; p.legN = Mathf.Sin(tt * 26 + 1) * 1.1f; p.legF = Mathf.Cos(tt * 23) * 1.1f; }
                    break;
                case HState.Splat: p.scared = true; p.armN = 2.4f; p.armF = -2.4f; p.legN = 0.5f; p.legF = -0.5f; p.sx = 1.15f; p.sy = 0.92f; break;
            }
            return p;
        }

        void LateUpdate()
        {
            if (!mgr || State == HState.Dead) return;
            float jx = jit > 0 ? Random.Range(-jit, jit) : 0;
            transform.position = World.ToUnity(x + jx, y, z);
            float s = appear < 1 ? EaseOutBack(appear) : 1;
            bool flying = State == HState.Fly;
            rig.Apply(MakePose(), s, face, flying ? rot : 0, sq, World.SortOrder(y), alpha);
            if (shadow)
            {
                float w = R * 1.5f * (1 - Mathf.Min(0.7f, z / 800)) * s, sw = shadow.sprite ? shadow.sprite.bounds.size.x : 1;
                shadow.transform.position = World.ToUnity(x, y);
                shadow.transform.localScale = new Vector3(w * 2 * World.U / sw, w * World.U * World.TILT / sw, 1);
            }
            // 체력바: 다쳤고 걷거나 도망칠 때
            var fx = FxManager.I;
            if (fx)
            {
                bool show = hp < hpMax && (State == HState.Walk || State == HState.Panic);
                if (show) { bar ??= fx.GetHpBar(); fx.ShowHpBar(bar, x, y, z + mgr.humanHeight + 12, 96, Mathf.Clamp01(hp / hpMax), 1); }
                else if (bar != null) { fx.ReleaseHpBar(bar); bar = null; }
            }
        }

        static string RandomOf(params string[] a) => a[Random.Range(0, a.Length)];
        static float EaseOutBack(float v) { const float c1 = 1.9f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(v - 1, 3) + c1 * Mathf.Pow(v - 1, 2); }
    }
}
