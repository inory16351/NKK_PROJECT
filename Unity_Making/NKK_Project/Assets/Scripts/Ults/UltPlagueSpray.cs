using System.Collections.Generic;
using NKK.Humans;
using UnityEngine;

namespace NKK.Ults
{
    // 역병 석궁쥐 · 역병 난사 (웹게임 plaguespray): 투명해져서 킥킥 살금살금 → 물건 많은 쪽으로 석궁 부채꼴 연사 (초록 화살 + 떨어진 자리에 역병 웅덩이)
    // 자막 c1~c4 · 팝업 c5 킥킥
    public class UltPlagueSpray : UltBase
    {
        const float SNEAK = 1.1f, FIRE = 5.4f;
        public override float Dur => 6.2f;
        static readonly Color GREEN = new(0.62f, 0.73f, 0.56f), LIME = new(0.6f, 0.9f, 0.3f), DARKG = new(0.44f, 0.56f, 0.42f);

        class Shot { public float x, y, vx, vy, a, t; public readonly HashSet<Object> hit = new(); public UltProp p; }
        class Pool { public float x, y, t, tick; public UltProp p; }
        readonly List<Shot> shots = new();
        readonly List<Pool> pools = new();
        float aim, sayT; bool aimed, shown;

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(SNEAK, () => Cap("c2"));
            Beat(3.2f, () => Cap("c3"));
            Beat(5.5f, () => Cap("c4"));
            R.ghost = true;
        }

        public override void Step(float dt, float k)
        {
            float t = T;
            if (t < SNEAK)
            {
                // 투명해져서 살금살금
                Walk(dt, 160);
                R.UltPose = P(head: 0.25f, headX: -2, front: 0.4f, tilt: 0.12f, tail: 0.3f, sy: 0.9f);
                if ((sayT -= dt) <= 0 && Random.value < 0.5f) { sayT = 0.3f; if (OnScreen(R.x, R.y)) PopupCap("c5", R.x + Rand(-40, 40), R.y, GREEN, 16, 0.6f, 50); }
                return;
            }
            if (!shown)
            {
                // 짠! 모습 드러냄
                shown = true; Unghost();
                Fx?.Anim("poof", R.x, R.y, 0, 1); Fx?.Ring(R.x, R.y, 120, GREEN, 0.35f); Fx?.Shake(0.1f);
            }
            if (t < FIRE)
            {
                // 석궁 난사: 가장 물건이 많은 쪽으로 부채꼴 연사
                if (!aimed || Random.value < dt * 2) { aimed = true; aim = AimMost(700); }
                R.face = Mathf.Cos(aim) >= 0 ? 1 : -1;
                R.UltPose = P(front: 1.6f + Mathf.Sin(t * 40) * 0.15f, farFront: 1.4f, head: -0.1f, tail: 0.9f, tilt: -0.15f);
                if ((hitT -= dt) <= 0)
                {
                    hitT = 0.045f;
                    float a = aim + Rand(-0.75f, 0.75f), sp = Rand(850, 1000);
                    var s = new Shot { x = R.x + Mathf.Cos(a) * 20, y = R.y + Mathf.Sin(a) * 14, vx = Mathf.Cos(a) * sp, vy = Mathf.Sin(a) * sp, a = a, t = 0.55f };
                    s.p = Prop("ult_bolt", s.x, s.y, 18, 34);
                    if (s.p != null) s.p.rot = Mathf.Atan2(-s.vy * World.TILT, s.vx);
                    shots.Add(s);
                    if (Random.value < 0.5f) Fx?.Burst(s.x, s.y, 20, 1, LIME, GREEN, 40, 120, 2, 4);   // 석궁 불꽃
                    R.UltJit = 0.8f;
                }
            }
            else
            {
                R.UltJit = 0;
                R.UltPose = P(head: -0.3f, front: 2, farFront: 0.3f, tail: 1.2f);   // 킥킥 만족
            }

            // 화살
            var cat = ItemMgr.Cats ? ItemMgr.Cats.Current : null;
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var b = shots[i];
                float px = b.x, py = b.y, vx = b.vx, vy = b.vy;
                b.x += b.vx * dt; b.y += b.vy * dt; b.t -= dt;
                if (M.Stage.Confine(ref b.x, ref b.y, ref vx, ref vy, 4, px, py, 0)) b.t = 0;
                foreach (var it in ItemsIn(b.x, b.y, 18))
                    if (b.hit.Add(it)) { it.Damage(UltD * 0.12f, R, false, b.a); if (OnScreen(it.x, it.y)) Fx?.Burst(it.x, it.y, 16, 3, LIME, GREEN, 60, 180, 2, 5); }
                foreach (var h in ItemMgr.Humans)
                    if (h.State != Human.HState.Fly && h.State != Human.HState.Dead && h.State != Human.HState.Splat && !b.hit.Contains(h) && Dist(h.x, h.y, b.x, b.y) < h.R + 8)
                    { b.hit.Add(h); h.Damage(h.hpMax * 0.3f, R, b.a); Fx?.Anim("hit", h.x, h.y, 40, 0.8f); b.t = 0; }
                if (cat && cat.Alive && !b.hit.Contains(cat) && Dist(cat.x, cat.y, b.x, b.y) < cat.R + 8)
                { b.hit.Add(cat); cat.Damage(UltD * 0.14f, b.a, R); b.t = 0; }
                if (b.p != null) { b.p.x = b.x; b.p.y = b.y; b.p.z = 18; }
                if (b.t <= 0)
                {
                    // 떨어진 자리에 초록 웅덩이 (잠깐 동안 주변을 조금씩 갉음)
                    if (Random.value < 0.35f)
                    {
                        var pl = new Pool { x = b.x, y = b.y, t = 1.4f, p = Prop(Random.value < 0.5f ? "splat_a" : "splat_b", b.x, b.y, 0, Rand(40, 68)) };
                        if (pl.p != null) { pl.p.ground = true; pl.p.tint = GREEN; pl.p.alpha = 0.55f; pl.p.rot = Rand(0, Mathf.PI); }
                        pools.Add(pl);
                        if (OnScreen(b.x, b.y)) Fx?.Burst(b.x, b.y, 6, 4, LIME, GREEN, 40, 120, 3, 6);
                    }
                    KillProp(b.p); shots.RemoveAt(i);
                }
            }

            // 역병 웅덩이
            for (int i = pools.Count - 1; i >= 0; i--)
            {
                var p = pools[i];
                p.t -= dt;
                if ((p.tick -= dt) <= 0)
                {
                    p.tick = 0.3f;
                    ItemMgr.Aoe(p.x, p.y, 40, UltD * 0.05f, R, false);
                    if (OnScreen(p.x, p.y) && Random.value < 0.5f) Fx?.Burst(p.x + Rand(-12, 12), p.y, 4, 2, LIME, DARKG, 10, 40, 3, 6);   // 보글보글
                }
                if (p.p != null) p.p.alpha = 0.55f * Mathf.Min(1, p.t / 0.5f);
                if (p.t <= 0) { KillProp(p.p); pools.RemoveAt(i); }
            }
        }

        public override void Finish() { Unghost(); }
        public override void Cleanup() { Unghost(); }

        // 투명 끝: 반투명으로 남은 그림 알파를 되돌림
        void Unghost()
        {
            if (!R.ghost) return;
            R.ghost = false;
            if (R.rig) foreach (var sr in R.rig.GetComponentsInChildren<SpriteRenderer>(true)) { var c = sr.color; c.a = 1; sr.color = c; }
        }
    }
}
