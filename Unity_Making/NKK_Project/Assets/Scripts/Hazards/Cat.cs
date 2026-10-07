using NKK.Data;
using NKK.Rats;
using UnityEngine;

namespace NKK.Hazards
{
    // 연구소 고양이 (웹게임 hazards.js 이식). 쥐를 쫓다 덮치고, 4~6초마다 품종 스킬.
    // 체력 0 → 날아가서 통통 → 삐져서 도망. 들이받거나 날아온 물건으로 체력을 깎음. 위치는 게임 단위.
    public class Cat : MonoBehaviour
    {
        public enum CState { Prowl, Pounce, Roll, Flung, Leave }

        public RatRig rig;
        [Tooltip("접지 그림자 (자식)")] public SpriteRenderer shadow;

        [Header("상태 (실행 중 확인용)")]
        public string codeId;
        public CState State = CState.Prowl;
        public float x, y, z, vx, vy, vz, hp, hpMax, life;

        public CatCharacterRow Data { get; private set; }
        public CatSkillRow Skill { get; private set; }
        public bool Gone => State == CState.Leave && life <= 0;
        public bool Alive => State != CState.Flung && State != CState.Leave && alpha >= 0.8f;
        public float R => mgr.catRadius;

        CatManager mgr;
        int face = 1, bounces;
        float t, cd, skillT, castT, pounceT, rollT, walk, rot, alpha, jit, hissUntil, value;
        bool critNext, doubleNext, slamming;
        FxManager.HpBar bar;

        public void Init(CatManager m, CatCharacterRow row, CatSkillRow skill, RatArtLibrary.Entry art, float px, float py, float hpValue, float cheese)
        {
            mgr = m; Data = row; Skill = skill; codeId = row.code_id; name = $"Cat_{row.code_id}";
            x = px; y = py; hpMax = hp = hpValue; value = cheese;
            life = row.life_time; cd = 1.5f; skillT = Random.Range(2.5f, 4f);
            rig.Build(art, m.catLength * row.size_mul, row.code_id == "chonk" ? 0.72f : 1, true);
            if (shadow && m.Rats.shadowRoot) { shadow.transform.SetParent(m.Rats.shadowRoot, true); shadow.sortingOrder = m.Rats.shadowSortOrder; }
        }

        void OnDestroy()
        {
            if (FxManager.I) FxManager.I.ReleaseHpBar(bar);
            if (shadow && shadow.transform.parent != transform) Destroy(shadow.gameObject);
        }

        float V(int i) => Skill == null ? 0 : i switch { 1 => Skill.value_01, 2 => Skill.value_02, 3 => Skill.value_03, 4 => Skill.value_04, _ => 0 };

        // ── 피해 ──
        public bool Damage(float dmg, float ang, Rat by = null)
        {
            if (!Alive) return false;
            hp -= dmg * CommonSkill.BossDmgMul; jit = 3;          // 보스 사냥꾼
            if (hp <= 0) { if (by) mgr.Rats.Ults?.Charge(by, CondType.Defeat_Cat); Fling(ang); return true; }
            if (Random.value < 0.25f) FxManager.I?.Popup(x, y, RandomOf("냥!", "캬악!", "냐?!", "하악!"), Color.white, 18, 0.6f, 60);
            return true;
        }

        void Fling(float a)
        {
            hp = 0; State = CState.Flung; vx = Mathf.Cos(a) * 560; vy = Mathf.Sin(a) * 560; vz = 760; bounces = 0; life = 4;
            foreach (var r in mgr.Rats.Rats) r.flee = 0;
            mgr.Game.OnSmash(value, 3);
            var fx = FxManager.I;
            if (fx) { fx.Stars(x, y, 40, 12, Color.white, new Color(0.94f, 0.78f, 0.47f)); fx.Coin(x, y, 5); fx.Shake(0.2f); }
            mgr.Game.ShowBanner("고양이 날려버림!", $"{Data.character_name} 퇴치 · 쥐의 힘을 보여줬다");
        }

        // ── 매 프레임 ──
        public void Tick(float dt)
        {
            float px = x, py = y;
            t += dt; life -= dt; cd -= dt; alpha = Mathf.Min(1, alpha + dt * 3); jit = Mathf.Max(0, jit - dt * 12); castT = Mathf.Max(0, castT - dt);
            switch (State)
            {
                case CState.Flung:
                {
                    // 날아감: 빙글빙글 → 땅에 통통 2번 → 도망
                    vz -= 1500 * dt; z += vz * dt; rot += dt * 14; x += vx * dt; y += vy * dt;
                    mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 0.5f);
                    if (z <= 0 && vz < 0)
                    {
                        z = 0; FxManager.I?.Dust(x, y, 6, 1.2f);
                        if (++bounces <= 2) { vz = 420; vx *= 0.6f; vy *= 0.6f; }
                        else { State = CState.Leave; life = 1.6f; rot = 0; float a = Random.Range(0, Mathf.PI * 2); vx = Mathf.Cos(a) * 420; vy = Mathf.Sin(a) * 420; FxManager.I?.Popup(x, y, "(삐짐)", Color.white, 16, 1, 60); }
                    }
                    return;
                }
                case CState.Leave:
                    alpha = Mathf.Min(alpha, life / 1.6f); x += vx * dt; y += vy * dt; walk += dt * 24;
                    return;
                case CState.Roll:
                {
                    // 먼치킨 식빵 굴리기: 몸 말고 직선 돌진, 닿는 쥐 나뒹굴기
                    rollT -= dt; rot += dt * 16 * face;
                    x += vx * dt; y += vy * dt;
                    if (mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 1)) FxManager.I?.Shake(0.05f);
                    foreach (var o in mgr.Rats.Rats)
                        if (o.stun <= 0 && Dist(o.x, o.y) < R + o.Radius + 6) o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x), 480, 360, V(3));
                    if (rollT <= 0) { State = CState.Prowl; rot = 0; vx *= 0.2f; vy *= 0.2f; cd = 0.8f; }
                    break;
                }
                default:
                {
                    // 사냥: 가까운 쥐 쪽으로 살금살금 → 가까우면 달려들기 (+ 품종 스킬)
                    var r = mgr.Rats.NearestRat(x, y, 900);
                    if ((skillT -= dt) <= 0 && State != CState.Pounce) { skillT = Random.Range(Skill.cond1_value_01, Skill.cond1_value_02); CastSkill(r); }
                    if (State == CState.Pounce)
                    {
                        pounceT -= dt; x += vx * dt; y += vy * dt;
                        if (pounceT <= 0)
                        {
                            bool crit = critNext; critNext = false;
                            int n = PounceHit(crit ? V(1) : mgr.pounceRadius, crit ? V(2) : mgr.pounceStun);
                            if (n > 0) { FxManager.I?.Popup(x, y, crit ? "신사의 일격!! 크리티컬!" : "냥냥펀치!!", crit ? new Color(0.95f, 0.76f, 0.31f) : new Color(0.89f, 0.6f, 0.35f), crit ? 28 : 24, 0.8f, 70); FxManager.I?.Shake(crit ? 0.25f : 0.12f); }
                            if (doubleNext && r) { doubleNext = false; float dx = r.x - x, dy = r.y - y, d = Mathf.Max(1, Mathf.Sqrt(dx * dx + dy * dy)); pounceT = 0.3f; vx = dx / d * V(2); vy = dy / d * V(2); vz = 240; FxManager.I?.Popup(x, y, "한 번 더!!", new Color(0.66f, 0.83f, 0.86f), 22, 0.6f, 90); }
                            else { State = CState.Prowl; cd = Random.Range(0.9f, 1.6f); vx *= 0.2f; vy *= 0.2f; }
                        }
                    }
                    else if (r)
                    {
                        float dx = r.x - x, dy = r.y - y, d = Mathf.Max(1, Mathf.Sqrt(dx * dx + dy * dy)), s = d < 320 ? 330 : 150;
                        vx += (dx / d * s - vx) * Mathf.Min(1, dt * 4); vy += (dy / d * s - vy) * Mathf.Min(1, dt * 4);
                        if (d < 130 && cd <= 0) { State = CState.Pounce; pounceT = 0.35f; vx = dx / d * 620; vy = dy / d * 620; vz = 260; }
                    }
                    if (z > 0 || vz > 0) { vz -= 1600 * dt; z = Mathf.Max(0, z + vz * dt); if (z <= 0) { vz = 0; if (slamming) SlamLand(); } }
                    x += vx * dt; y += vy * dt;
                    mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 0.5f);
                    if (life <= 0) { State = CState.Leave; life = 1.6f; float a = Random.Range(0, Mathf.PI * 2); vx = Mathf.Cos(a) * 300; vy = Mathf.Sin(a) * 300; }
                    // 겁먹은 쥐들 (하악질 중엔 범위 넓어짐)
                    float fear = mgr.fearRadius * (hissUntil > Time.time ? V(1) : 1);
                    foreach (var o in mgr.Rats.Rats) if (Dist(o.x, o.y) < fear) o.Scare(x, y, mgr.fearTime * CommonSkill.CatFearMul);
                    if (Random.value < dt * 0.5f) FxManager.I?.Popup(x, y, RandomOf("냐옹~", "냥?", "크르릉…"), Color.white, 16, 0.8f, 80);
                    break;
                }
            }
            if (Mathf.Abs(vx) > 10 && State != CState.Roll) face = vx > 0 ? 1 : -1;
            walk += dt * Mathf.Sqrt(vx * vx + vy * vy) / 12;
        }

        float Dist(float ox, float oy) => Mathf.Sqrt((ox - x) * (ox - x) + (oy - y) * (oy - y));

        int PounceHit(float rad, float stunT)
        {
            int n = 0;
            foreach (var o in mgr.Rats.Rats) if (Dist(o.x, o.y) < rad) { o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x), 460, 380, stunT); n++; }
            return n;
        }

        void SlamLand()
        {
            slamming = false;
            PounceHit(V(1), V(2));
            foreach (var it in mgr.Items.InRange(x, y, V(1) + 40))
                if (it.State == Items.Item.ItemState.Rest && Dist(it.x, it.y) < V(1)) it.Launch(Mathf.Atan2(it.y - y, it.x - x), V(3), false);
            var fx = FxManager.I; if (fx) { fx.Ring(x, y, V(1), new Color(0.89f, 0.6f, 0.35f), 0.5f); fx.Dust(x, y, 10, 1.6f); fx.Anim("poof", x, y, 0, 1.2f); fx.Shake(0.25f); }
        }

        // ── 품종 스킬 (고양이 테이블 effect_type + value_01~04) ──
        void CastSkill(Rat r)
        {
            if (Skill == null) return;
            castT = 0.5f;
            var fx = FxManager.I;
            fx?.Popup(x, y, Skill.skill_name + "!", Data.Category == CatCategory.Special ? new Color(0.8f, 0.71f, 0.86f) : new Color(1, 0.95f, 0.75f), 20, 1, 100);
            switch (Skill.Effect)
            {
                case CatEffectType.Hiss_Fear: hissUntil = Time.time + V(2); fx?.Ring(x, y, mgr.fearRadius * V(1), new Color(0.91f, 0.47f, 0.42f, 0.7f), 0.6f); break;
                case CatEffectType.Double_Pounce: doubleNext = true; cd = 0; break;
                case CatEffectType.Crit_Pounce: critNext = true; cd = 0; break;
                case CatEffectType.Jump_Press: slamming = true; vz = 700; if (r) { vx = (r.x - x) * 0.9f; vy = (r.y - y) * 0.9f; } break;
                case CatEffectType.Roll_Charge:
                    if (r) { float a = Mathf.Atan2(r.y - y, r.x - x); State = CState.Roll; rollT = V(1); vx = Mathf.Cos(a) * V(2); vy = Mathf.Sin(a) * V(2); face = Mathf.Cos(a) >= 0 ? 1 : -1; }
                    break;
                case CatEffectType.Crowd_Teleport:
                {
                    // 쥐가 제일 많은 곳 옆으로 순간이동 → 바로 덮치기
                    Rat best = r; int bn = 0;
                    foreach (var o in mgr.Rats.Rats) { int n = 0; foreach (var q in mgr.Rats.Rats) if (Vector2.Distance(new Vector2(q.x, q.y), new Vector2(o.x, o.y)) < V(1)) n++; if (n > bn) { bn = n; best = o; } }
                    if (best) { fx?.Dust(x, y, 8, 1); x = best.x + Random.Range(-60f, 60f); y = best.y + Random.Range(-40f, 40f); float ox = x, oy = y; mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, best.x, best.y, 0); fx?.Dust(x, y, 8, 1); cd = 0; }
                    break;
                }
                case CatEffectType.Laser_Stun:
                    if (r)
                    {
                        float a = Mathf.Atan2(r.y - y, r.x - x), L = V(1), ux = Mathf.Cos(a), uy = Mathf.Sin(a);
                        foreach (var o in mgr.Rats.Rats) { float px = o.x - x, py = o.y - y, al = px * ux + py * uy; if (al > 0 && al < L && Mathf.Abs(px * uy - py * ux) < V(2)) o.Stun(V(3)); }
                        mgr.Beam(x, y, x + ux * L, y + uy * L);
                    }
                    break;
                case CatEffectType.Roar_Blast:
                {
                    float R0 = V(1);
                    foreach (var o in mgr.Rats.Rats) if (Dist(o.x, o.y) < R0) o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x), V(2), 420, 1.2f);
                    foreach (var it in mgr.Items.InRange(x, y, R0 + 40)) if (it.State == Items.Item.ItemState.Rest && Dist(it.x, it.y) < R0) it.Launch(Mathf.Atan2(it.y - y, it.x - x), V(3), false);
                    if (fx) { fx.Ring(x, y, R0, new Color(0.89f, 0.6f, 0.35f), 0.5f); fx.Ring(x, y, R0 * 0.6f, Color.white, 0.35f); fx.Shake(0.3f); fx.Popup(x, y, "크아아앙!!", Color.white, 30, 0.8f, 110); }
                    break;
                }
                case CatEffectType.Fireball:
                    for (int n = 0; n < Mathf.RoundToInt(V(1)); n++) mgr.QueueFireball(this, 0.15f + n * V(2), V(3));
                    break;
                case CatEffectType.Gravity_Wave:
                    foreach (var o in mgr.Rats.Rats) if (Dist(o.x, o.y) < V(1)) { o.vz = Random.Range(V(3), V(4)); o.Stun(V(2)); }
                    fx?.Ring(x, y, V(1), new Color(0.8f, 0.71f, 0.86f), 0.6f);
                    break;
            }
        }

        // 웹게임 catPose: walk · pounce · crouch · flung · cast
        RatRig.Pose MakePose()
        {
            float tt = Time.time;
            var p = new RatRig.Pose { head = Mathf.Sin(tt * 1.6f) * 0.06f, tail = 0.1f + Mathf.Sin(tt * 3) * 0.25f, sx = 1, sy = 1 };
            if (State == CState.Pounce) { p.front = 1.3f; p.farFront = 1.1f; p.back = -1.1f; p.farBack = -0.9f; p.tilt = -0.25f; p.tail = 1; p.head = -0.15f; }
            else if (State == CState.Roll || (State == CState.Prowl && cd > 0 && cd < 0.4f)) { p.front = 0.3f; p.farFront = 0.3f; p.back = 0.4f; p.farBack = 0.4f; p.tilt = 0.12f; p.bob = 6; p.tail = 0.9f + Mathf.Sin(tt * 20) * 0.2f; }
            else if (State == CState.Flung) { p.front = Mathf.Sin(tt * 30) * 1.4f; p.farFront = Mathf.Cos(tt * 27) * 1.4f; p.back = Mathf.Sin(tt * 28) * 1.2f; p.farBack = Mathf.Cos(tt * 25) * 1.2f; p.tail = Mathf.Sin(tt * 20); p.head = Mathf.Sin(tt * 15) * 0.4f; }
            else if (castT > 0) { p.front = 2.1f; p.farFront = 0.5f; p.tilt = -0.3f; p.head = -0.2f; p.tail = 1.1f; }
            else if (Mathf.Sqrt(vx * vx + vy * vy) > 20) { float s = Mathf.Sin(walk); p.front = s * 0.5f; p.farBack = s * 0.45f; p.farFront = -s * 0.5f; p.back = -s * 0.45f; p.bob = -Mathf.Abs(Mathf.Cos(walk)) * 2.5f; p.tail = Mathf.Sin(walk * 0.5f) * 0.3f + 0.1f; }
            return p;
        }

        void LateUpdate()
        {
            if (!mgr) return;
            float jx = jit > 0 ? Random.Range(-jit, jit) : 0;
            transform.position = World.ToUnity(x + jx, y, z);
            transform.rotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            rig.Apply(MakePose(), 1, face, 1, World.SortOrder(y));
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) { var c = sr.color; c.a = alpha; sr.color = c; }
            if (shadow)
            {
                float w = R * 1.6f * (1 - Mathf.Min(0.7f, z / 500)), sw = shadow.sprite ? shadow.sprite.bounds.size.x : 1;
                shadow.transform.position = World.ToUnity(x, y);
                shadow.transform.localScale = new Vector3(w * 2 * World.U / sw, w * 0.6f * 2 * World.TILT * World.U / sw, 1);
                var c = shadow.color; c.a = 0.35f * alpha; shadow.color = c;
            }
            var fx = FxManager.I;
            if (fx)
            {
                bool show = Alive;
                if (show) { bar ??= fx.GetHpBar(); fx.ShowHpBar(bar, x, y, z + 92, 90, Mathf.Clamp01(hp / hpMax), 1); }
                else if (bar != null) { fx.ReleaseHpBar(bar); bar = null; }
            }
        }

        static string RandomOf(params string[] a) => a[Random.Range(0, a.Length)];
    }
}
