using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 천사 생쥐 · 천국의 계단 (웹게임 lemmings): 빛나는 계단이 생기고 물건들이 한 줄로 올라가 꼭대기에서 차례로 투신 (레밍즈)
    // 자막: c1 천국으로 가는 계단 · c2 (한 줄로 서세요) · c3 꼭대기엔 아무것도 없다 · c4 (줄이 줄어들지 않는다) / c5~c8 = 투신 팝업 (야호~ · 뛰어! · 안녕~ · (투신))
    public class UltLemmings : UltBase
    {
        const int STEPS = 7, MAXQ = 33;
        const float SW = 34, SH = 30, GAP = 0.16f, WALK = 0.45f, CLIMB = 0.09f;
        public override float Dur => 6.2f;
        static readonly Color STEP = new(0.949f, 0.851f, 0.541f), GLOW = new(1f, 0.953f, 0.749f);
        static readonly string[] JUMP = { "c5", "c6", "c7", "c8" };

        class Q { public Item it; public float lt, hx, hy; public bool jumped; }
        readonly List<Q> q = new();
        int dir; float bx, by, lastLt, drawT;

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(1.6f, () => Cap("c2"));
            Beat(2.8f, () => Cap("c3"));
            Beat(4.4f, () => Cap("c4"));
            dir = R.face >= 0 ? 1 : -1; bx = R.x + dir * 60; by = R.y;
            var l = ItemsIn(R.x, R.y, ULT_R);
            l.Sort((a, b) => Dist(a.x, a.y, bx, by).CompareTo(Dist(b.x, b.y, bx, by)));
            for (int i = 0; i < l.Count && q.Count < 22; i++) if (GrabItem(l[i])) q.Add(new Q { it = l[i], hx = l[i].x, hy = l[i].y });
            for (int i = 0; i < q.Count; i++) q[i].lt = 0.4f + i * GAP;
            lastLt = q.Count > 0 ? q[^1].lt : 0.2f;
            Fx?.Stars(bx, by, 20, 14, Color.white, GLOW, 120, 320);
            Fx?.Ring(bx, by, 80, GLOW, 0.4f);
        }

        Item NearestToBase()
        {
            Item best = null; float bd = float.MaxValue;
            foreach (var it in ItemsIn(bx, by, ULT_R)) { float d = Dist(it.x, it.y, bx, by); if (d < bd) { bd = d; best = it; } }
            return best;
        }

        // 빛나는 계단 (계단 칸 + 빛 기둥)
        void DrawStairs(float dt)
        {
            if ((drawT -= dt) > 0) return;
            drawT = 0.04f;
            float glow = 0.8f + 0.2f * Mathf.Sin(T * 6);
            for (int i = 0; i < STEPS; i++)
            {
                float x0 = bx + dir * i * SW, x1 = bx + dir * (i + 1) * SW, top = (i + 1) * SH;
                var pc = GLOW; pc.a = 0.3f * glow;
                Fx?.Beam((x0 + x1) / 2, by, 0, (x0 + x1) / 2, by, top - 10, pc, 0.07f);
                var sc = STEP; sc.a = glow;
                Fx?.Beam(x0, by, top - 6, x1, by, top - 6, sc, 0.07f);
            }
        }

        public override void Step(float dt, float k)
        {
            // 줄 끝이 가까워지면 주변 물건이 계속 새로 줄을 섬 (0.16초 간격, 합쳐서 33개까지, 4.9초까지)
            if (q.Count < MAXQ && lastLt < T + 0.3f && lastLt + GAP < 4.9f)
            {
                var it = NearestToBase();
                if (it && GrabItem(it)) { lastLt = Mathf.Max(T, lastLt + GAP); q.Add(new Q { it = it, lt = lastLt, hx = it.x, hy = it.y }); }
            }
            R.UltPose = P(tilt: -0.5f, front: 2.6f + Mathf.Sin(T * 6) * 0.2f, farFront: 2.3f, back: -0.3f, farBack: 0.3f, head: -0.4f, tail: 1.3f);
            R.z = 16 + Mathf.Sin(T * 3) * 6;
            DrawStairs(dt);
            if (Random.value < 0.25f) Fx?.Stars(bx + dir * Rand(0, STEPS * SW), by, Rand(20, STEPS * SH), 1, Color.white, GLOW, 10, 40);
            foreach (var e in q)
            {
                var it = e.it; float t = T - e.lt;
                if (t < 0 || e.jumped || !it || it.State != Item.ItemState.Held) continue;
                if (t < WALK)
                {
                    // 계단 앞으로 총총
                    float s = Ease(t / WALK);
                    it.x = Mathf.Lerp(e.hx, bx, s); it.y = Mathf.Lerp(e.hy, by, s); it.z = Mathf.Sin(s * Mathf.PI) * 30;
                }
                else if (t < WALK + STEPS * CLIMB)
                {
                    // 한 칸씩 오름
                    float c = (t - WALK) / CLIMB, f = c % 1; int n = Mathf.FloorToInt(c);
                    it.x = bx + dir * (n + f) * SW; it.y = by; it.z = n * SH + Mathf.Sin(f * Mathf.PI) * 16 + SH * f;
                }
                else
                {
                    // 꼭대기에서 투신
                    e.jumped = true;
                    float jx = it.x, jy = it.y, jz = it.z;
                    DropItem(it, dir * Rand(120, 260), Rand(-80, 80), 260);
                    if (OnScreen(jx, jy))
                    {
                        Fx?.Stars(jx, jy, jz, 5, Color.white, GLOW, 80, 200);
                        if (Random.value < 0.5f) PopupCap(Pick(JUMP), jx, jy, Color.white, 16, 0.8f, jz + 30);
                    }
                }
            }
        }
    }
}
