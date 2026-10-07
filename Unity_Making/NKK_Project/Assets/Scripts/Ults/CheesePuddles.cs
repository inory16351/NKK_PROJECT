using System.Collections.Generic;
using NKK.Items;
using NKK.Rats;
using UnityEngine;

namespace NKK.Ults
{
    // 치즈 퐁듀 쥐 회전회오리 (UltCheeseSpin) 의 치즈 덩어리 · 웅덩이. 필살기가 끝나도 남아서 웅덩이가 다 마르면 스스로 사라짐
    //   Throw: 몸에서 포물선으로 날아가는 치즈 덩어리 + 바닥 낙하 지점 표시 (공룡 쥐 운석처럼) → 착지: 치즈 폭발 · 튀는 방울 · 물건 피해 → 웅덩이
    //   웅덩이를 밟은 다른 쥐: 쥐 대포알처럼 쭉 미끄러지며 데굴데굴 (닿는 물건은 핀볼처럼 튕기며 때림) → 기절 별
    // 그림: cheese_chunk · cheese_splash · cheese_crown · cheese_glob (FX_Cheese) · cheese_puddle (NewRats) · meteor_target (FX_Meteor)
    // 글: c11 = 미끄러질 때 팝업 (Ult_Caption)
    public class CheesePuddles : MonoBehaviour
    {
        const float G = 1400;                       // 치즈 덩어리 중력
        const float LINGER = 4.5f;                  // 필살기 끝난 뒤 웅덩이가 남는 시간 (+0~1.5초)
        const float SLIP_T = 0.85f, SLIP_SPD = 620, STUN_T = 1.4f;
        const int MAX_PUDDLES = 36;
        static readonly Color CHEESE = new(0.99f, 0.84f, 0.29f), CREAM = new(1f, 0.96f, 0.72f), DEEP = new(0.95f, 0.66f, 0.2f);

        UltimateManager M; Rat caster; int ultId;
        bool active = true;                         // 필살기 진행 중 (웅덩이가 안 마름)
        float T;
        public System.Action<Rat> onSlip;           // 쥐가 미끄러질 때 (필살기 쪽 자막)

        class Chunk { public UltProp p, mark; public float x0, y0, z0, tx, ty, t, dur, vz0, w, rad, dmg, spin; public bool orient; }
        class Deco { public UltProp p; public float x, y, z, vx, vy, vz, life, max, w0, w1, spin, grav, lift; }     // lift = 폭 × 이만큼 띄움 (바닥에 선 그림)
        class Puddle { public UltProp p; public float x, y, w, wT, life = -1, aspect = 0.47f; }
        class Slip { public Rat o; public float ang, t; public readonly Dictionary<Item, float> hits = new(); }
        readonly List<Chunk> chunks = new();
        readonly List<Deco> decos = new();
        readonly List<Puddle> puddles = new();
        readonly List<Slip> slips = new();
        readonly Dictionary<Rat, float> slipCD = new();

        public static CheesePuddles Make(UltimateManager m, Rat caster, int ultId)
        {
            var go = new GameObject("CheesePuddles"); go.transform.SetParent(m.transform, false);
            var c = go.AddComponent<CheesePuddles>(); c.M = m; c.caster = caster; c.ultId = ultId;
            return c;
        }

        // 필살기 끝 (끝까지 했든 취소됐든): 이제부터 웅덩이가 마르기 시작
        public void EndUlt()
        {
            active = false; onSlip = null;
            foreach (var p in puddles) if (p.life < 0) p.life = LINGER + Random.Range(0, 1.5f);
        }

        // 몸 (x, y, z) 에서 (tx, ty) 로 dur 초 동안 날아가는 치즈 덩어리. 착지 반경 rad, 물건 피해 dmg
        public void Throw(float x, float y, float z, float tx, float ty, float dur, float rad, float dmg, float size = 1)
        {
            bool chunkArt = Has("cheese_chunk");
            var c = new Chunk { x0 = x, y0 = y, z0 = z, tx = tx, ty = ty, dur = dur, rad = rad, dmg = dmg, w = 34 * size, orient = chunkArt, spin = Random.Range(-14f, 14f) };
            c.vz0 = (0.5f * G * dur * dur - z) / dur;
            c.p = NewProp(chunkArt ? "cheese_chunk" : "cheese_bullet");
            if (c.p != null) { c.p.w = c.w; c.p.sortBias = 20; }
            c.mark = NewProp("meteor_target");
            if (c.mark != null) { c.mark.ground = true; c.mark.tint = new Color(1f, 0.82f, 0.25f); c.mark.alpha = 0; c.mark.sortBias = 5; c.mark.x = tx; c.mark.y = ty; }
            chunks.Add(c);
            StepChunk(c, 0);
        }

        void Update()
        {
            float dt = FxManager.WorldFreeze ? 0 : Mathf.Min(Time.deltaTime, 0.05f);
            if (dt > 0)
            {
                T += dt;
                for (int i = chunks.Count - 1; i >= 0; i--) if (StepChunk(chunks[i], dt)) chunks.RemoveAt(i);
                StepDecos(dt);
                StepPuddles(dt);
                StepSlips(dt);
                CheckSlip();
            }
            foreach (var c in chunks) { c.p?.Apply(); c.mark?.Apply(); }
            foreach (var d in decos) d.p.Apply();
            foreach (var p in puddles) p.p.Apply();
            if (!active && chunks.Count == 0 && decos.Count == 0 && puddles.Count == 0 && slips.Count == 0) Destroy(gameObject);
        }

        void OnDestroy()
        {
            foreach (var c in chunks) { c.p?.Destroy(); c.mark?.Destroy(); }
            foreach (var d in decos) d.p.Destroy();
            foreach (var p in puddles) p.p.Destroy();
            chunks.Clear(); decos.Clear(); puddles.Clear(); slips.Clear();
        }

        // ── 치즈 덩어리: 포물선 → 착지 ──
        bool StepChunk(Chunk c, float dt)
        {
            c.t += dt;
            float k = Mathf.Clamp01(c.t / c.dur);
            float x = Mathf.Lerp(c.x0, c.tx, k), y = Mathf.Lerp(c.y0, c.ty, k), z = c.z0 + c.vz0 * c.t - 0.5f * G * c.t * c.t;
            if (c.p != null)
            {
                c.p.x = x; c.p.y = y; c.p.z = Mathf.Max(0, z);
                if (c.orient)
                {
                    // 그림은 오른쪽 위(45°)로 날아가는 모양 (꼬리가 왼쪽 아래) → 화면상 진행 방향에 맞춤
                    float vx = (c.tx - c.x0) / c.dur, vy = (c.ty - c.y0) / c.dur, vz = c.vz0 - G * c.t;
                    Vector3 vel = World.ToUnity(vx, vy, vz) - World.ToUnity(0, 0, 0);
                    c.p.rot = Mathf.Atan2(vel.y, vel.x) - Mathf.PI / 4;
                }
                else c.p.rot += c.spin * dt;
                c.p.w = c.w * (1 + 0.08f * Mathf.Sin(c.t * 30));
            }
            if (c.mark != null)
            {
                // 낙하 지점: 점점 진해지고 줄어들며 깜빡 (운석 표시처럼)
                c.mark.w = c.rad * 1.5f * Mathf.Lerp(1.2f, 0.85f, k);
                c.mark.alpha = Mathf.Clamp01(k * 3) * (0.35f + 0.35f * Mathf.Abs(Mathf.Sin(c.t * (8 + 10 * k))));
            }
            if (dt > 0 && Random.value < 0.35f && z > 4) Fx?.Burst(x, y, z, 1, CHEESE, CREAM, 10, 50, 3, 6);    // 떨어지는 치즈 방울
            if (k < 1) return false;
            c.p?.Destroy(); c.mark?.Destroy();
            Impact(c.tx, c.ty, c.rad, c.dmg);
            return true;
        }

        void Impact(float x, float y, float rad, float dmg)
        {
            // 물건·사람 피해
            if (caster && M.Items)
            {
                M.Items.Aoe(x, y, rad, dmg, caster, false);
                M.Items.BlastActors(x, y, rad + 10, 320, dmg * 0.5f, caster);
            }
            AddPuddle(x, y, rad * Random.Range(1.5f, 1.9f));
            if (!M.OnScreen(x, y)) return;
            var fx = Fx;
            if (fx)
            {
                fx.Ring(x, y, rad, new Color(1f, 0.85f, 0.35f, 0.95f), 0.35f); fx.Ring(x, y, rad * 0.55f, Color.white, 0.25f);
                fx.Burst(x, y, 10, 12, CHEESE, CREAM, 160, 440);
                fx.Burst(x, y, 6, 6, DEEP, CHEESE, 80, 220, 5, 10);
                fx.Dust(x, y, 3, 0.8f); fx.Shake(0.025f); fx.Hitstop(0.01f);
            }
            // 치즈 폭발: 확 커졌다 사라짐 (그림 없으면 운석 폭발을 치즈색으로)
            bool splashArt = Has("cheese_splash");
            var s = AddDeco(splashArt ? "cheese_splash" : "meteor_burst", x, y, rad * 0.35f, 0, 0, 0, 0.34f, rad * 0.6f, rad * 2.3f, Random.Range(-2f, 2f), 0);
            if (s != null && !splashArt) s.p.tint = CHEESE;
            // 바닥에서 솟는 치즈 왕관
            if (Has("cheese_crown")) { var cr = AddDeco("cheese_crown", x, y + 1, 0, 0, 0, 0, 0.42f, rad * 0.7f, rad * 1.6f, 0, 0); if (cr != null) { cr.p.sortBias = 25; cr.lift = 0.2f; StepDeco(cr, 0); } }
            // 사방으로 튀는 치즈 방울
            string glob = Has("cheese_glob") ? "cheese_glob" : "cheese_bullet";
            for (int i = 0, n = Random.Range(4, 7); i < n; i++)
            {
                float a = Random.Range(0, Mathf.PI * 2), sp = Random.Range(120f, 280f), w = Random.Range(12f, 22f);
                AddDeco(glob, x, y, 8, Mathf.Cos(a) * sp, Mathf.Sin(a) * sp * 0.8f, Random.Range(260f, 440f), Random.Range(0.6f, 0.9f), w, w, Random.Range(-10f, 10f), G);
            }
        }

        // ── 잠깐 떠 있는 그림 (폭발·왕관·방울) ──
        Deco AddDeco(string name, float x, float y, float z, float vx, float vy, float vz, float life, float w0, float w1, float spin, float grav)
        {
            if (decos.Count >= 90) { decos[0].p.Destroy(); decos.RemoveAt(0); }
            var p = NewProp(name); if (p == null) return null;
            p.rot = grav > 0 || spin != 0 ? Random.Range(0, Mathf.PI * 2) : 0; p.sortBias = 30;
            var d = new Deco { p = p, x = x, y = y, z = z, vx = vx, vy = vy, vz = vz, life = life, max = life, w0 = w0, w1 = w1, spin = spin, grav = grav };
            decos.Add(d); StepDeco(d, 0);
            return d;
        }
        void StepDecos(float dt)
        {
            for (int i = decos.Count - 1; i >= 0; i--)
            {
                var d = decos[i];
                if ((d.life -= dt) <= 0) { d.p.Destroy(); decos.RemoveAt(i); continue; }
                StepDeco(d, dt);
            }
        }
        static void StepDeco(Deco d, float dt)
        {
            d.x += d.vx * dt; d.y += d.vy * dt; d.vz -= d.grav * dt; d.z = Mathf.Max(0, d.z + d.vz * dt);
            if (d.grav > 0 && d.z <= 0) { d.vx *= 0.3f; d.vy *= 0.3f; d.vz = 0; d.spin = 0; }
            float k = 1 - d.life / d.max;
            d.p.x = d.x; d.p.y = d.y; d.p.rot += d.spin * dt;
            d.p.w = Mathf.Lerp(d.w0, d.w1, 1 - (1 - k) * (1 - k));
            d.p.z = d.z + d.p.w * d.lift;
            d.p.alpha = Mathf.Clamp01(d.life / (d.max * 0.5f));
        }

        // ── 웅덩이: 가까우면 합쳐서 커짐 ──
        void AddPuddle(float x, float y, float w)
        {
            Puddle near = null; float nd = float.MaxValue;
            foreach (var p in puddles) { float d = Dist(p.x, p.y, x, y); if (d < nd) { nd = d; near = p; } }
            if (near != null && (nd < near.wT * 0.3f || puddles.Count >= MAX_PUDDLES))
            {
                near.wT = Mathf.Min(near.wT + w * 0.25f, 260);
                if (!active) near.life = Mathf.Max(near.life, LINGER);
                return;
            }
            var pr = NewProp("cheese_puddle"); if (pr == null) return;
            pr.ground = true; pr.alpha = 0.92f; pr.flip = Random.value < 0.5f; pr.sortBias = 3; pr.x = x; pr.y = y; pr.w = 0;
            var np = new Puddle { p = pr, x = x, y = y, w = 0, wT = w, life = active ? -1 : LINGER };
            if (pr.r && pr.r.sprite) { var b = pr.r.sprite.bounds.size; np.aspect = b.y / Mathf.Max(0.001f, b.x); }
            puddles.Add(np);
        }
        void StepPuddles(float dt)
        {
            for (int i = puddles.Count - 1; i >= 0; i--)
            {
                var p = puddles[i];
                p.w += (p.wT - p.w) * Mathf.Min(1, dt * 14);                 // 철퍽 퍼짐
                if (p.life >= 0)
                {
                    p.life -= dt;
                    if (p.life <= 0) { p.p.Destroy(); puddles.RemoveAt(i); continue; }
                    if (p.life < 1.2f) { float k = p.life / 1.2f; p.p.alpha = 0.92f * k; p.w = p.wT * (0.75f + 0.25f * k); }  // 마름
                }
                p.p.w = p.w;
            }
        }
        Puddle PuddleAt(float x, float y)
        {
            foreach (var p in puddles)
            {
                if (p.w < 20 || p.p.alpha < 0.3f) continue;
                float rx = p.w * 0.45f, ry = rx * p.aspect * World.TILT, dx = (x - p.x) / rx, dy = (y - p.y) / ry;
                if (dx * dx + dy * dy < 1) return p;
            }
            return null;
        }

        // ── 미끄러짐 ──
        void CheckSlip()
        {
            if (puddles.Count == 0 || !M.Rats) return;
            foreach (var o in M.Rats.Rats)
            {
                if (!o || o == caster || o.UltOn || o.Held || o.stun > 0 || o.z > 3) continue;
                if (slipCD.TryGetValue(o, out var cd) && cd > T) continue;
                if (PuddleAt(o.x, o.y) == null) continue;
                StartSlip(o);
            }
        }
        void StartSlip(Rat o)
        {
            float sp = Mathf.Sqrt(o.vx * o.vx + o.vy * o.vy);
            float ang = (sp > 30 ? Mathf.Atan2(o.vy, o.vx) : Random.Range(0, Mathf.PI * 2)) + Random.Range(-0.35f, 0.35f);
            o.Ragdoll(ang, SLIP_SPD, 300, SLIP_T + STUN_T);
            slips.Add(new Slip { o = o, ang = ang });
            slipCD[o] = T + SLIP_T + STUN_T + 0.6f;
            if (M.OnScreen(o.x, o.y))
            {
                Fx?.Burst(o.x, o.y, 4, 8, CHEESE, CREAM, 100, 260); Fx?.Dust(o.x, o.y, 2, 0.7f);
                var s = M.CaptionText(ultId, "c11");
                if (!string.IsNullOrEmpty(s)) Fx?.Popup(o.x, o.y, s, CHEESE, 19, 0.9f, 50);
            }
            onSlip?.Invoke(o);
        }
        void StepSlips(float dt)
        {
            for (int i = slips.Count - 1; i >= 0; i--)
            {
                var s = slips[i]; var o = s.o;
                s.t += dt;
                if (!o || o.UltOn) { slips.RemoveAt(i); continue; }
                float k = s.t / SLIP_T;
                if (k >= 1)
                {
                    // 다 미끄러짐 → 철퍼덕, 기절 별
                    o.Stun(STUN_T);
                    if (M.OnScreen(o.x, o.y)) { Fx?.Stars(o.x, o.y, 14, 5, Color.white, CHEESE, 80, 200); Fx?.Dust(o.x, o.y, 3, 0.9f); }
                    slips.RemoveAt(i); continue;
                }
                // 벽에 튕기면 방향 바꿈 (쥐 이동이 벽에서 속도를 뒤집음)
                float cx = Mathf.Cos(s.ang), cy = Mathf.Sin(s.ang);
                if (o.vx * cx + o.vy * cy < 0 && o.vx * o.vx + o.vy * o.vy > 100) { s.ang = Mathf.Atan2(o.vy, o.vx); cx = Mathf.Cos(s.ang); cy = Mathf.Sin(s.ang); }
                // 쥐 대포알처럼: 닿는 물건마다 핀볼처럼 튕기며 때림
                float rad = o.Radius;
                foreach (var it in new List<Item>(M.Items.InRange(o.x, o.y, rad + 60)))
                {
                    if (!it || it.State != Item.ItemState.Rest) continue;
                    float dx = o.x - it.x, dy = o.y - it.y, d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > rad + it.R || (s.hits.TryGetValue(it, out var th) && th > T)) continue;
                    s.hits[it] = T + 0.25f;
                    float nx = dx / (d > 0 ? d : 1), ny = dy / (d > 0 ? d : 1), dot = cx * nx + cy * ny;
                    if (dot < 0) { cx -= 2 * dot * nx; cy -= 2 * dot * ny; s.ang = Mathf.Atan2(cy, cx); }
                    o.x = it.x + nx * (rad + it.R + 1); o.y = it.y + ny * (rad + it.R + 1);
                    it.Damage(o.Damage * 2, o, false, Mathf.Atan2(-ny, -nx));
                    if (M.OnScreen(o.x, o.y)) { Fx?.Stars(it.x, it.y, 12, 4, Color.white, CHEESE, 100, 240); Fx?.Shake(0.02f); }
                }
                float spd = SLIP_SPD * (1 - 0.6f * k);
                o.vx = cx * spd; o.vy = cy * spd;
                // 데굴데굴 통통 튀며 치즈 자국
                if (o.z <= 0 && k < 0.75f) { o.vz = 150; if (M.OnScreen(o.x, o.y)) Fx?.Burst(o.x, o.y, 2, 3, CHEESE, CREAM, 40, 120); }
            }
        }

        // ── 도우미 ──
        static FxManager Fx => FxManager.I;
        static float Dist(float x1, float y1, float x2, float y2) => Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        bool Has(string n) { foreach (var p in M.props) if (p.name == n && p.sprite) return true; return false; }
        UltProp NewProp(string n) => Has(n) ? M.MakeProp(n) : null;
    }
}
