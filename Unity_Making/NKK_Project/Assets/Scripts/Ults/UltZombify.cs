using System.Collections.Generic;
using NKK.Rats;
using UnityEngine;

namespace NKK.Ults
{
    // 좀비 쥐 · 좀비 아포칼립스 (웹게임 zombify): 흐느적 걸으며 0.35초마다 감염 파동 (120 → 840 반경, 7번마다 처음부터)
    //   → 닿은 쥐는 7초 동안 광란 (웹: 좀비 = 초록·팔 앞으로·피해 2배 — 공용 쪽 요청 중, 지금은 광란만)
    // 자막: c1 끄어어 · c2 확산 중 · c3 2차 확산 / c4 끄어… 팝업
    public class UltZombify : UltBase
    {
        const float WAVE = 120, INFECT = 7;
        public override float Dur => 6;
        static readonly Color green = new(0.62f, 0.73f, 0.56f);
        int w;
        readonly HashSet<Rat> infected = new();

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(0.9f, () => Cap("c2"));
            Beat(3.4f, () => Cap("c3"));
            Fx?.Burst(R.x, R.y, 20, 14, green, new Color(0.45f, 0.55f, 0.4f), 80, 260);
        }

        public override void Step(float dt, float k)
        {
            R.UltPose = P(tilt: -0.5f, front: 1.6f + Mathf.Sin(Time.time * 6) * 0.2f, farFront: 1.5f, head: 0.3f + Mathf.Sin(T * 2.3f) * 0.15f, tail: 0.6f);
            R.UltRot = Mathf.Sin(T * 3) * 0.08f;                              // 흐느적흐느적
            Walk(dt, 80);
            if (Random.value < 0.25f) Fx?.Burst(R.x + Rand(-20, 20), R.y, Rand(10, 30), 1, green, new Color(0.45f, 0.55f, 0.4f), 20, 60, 4, 7);
            if ((hitT -= dt) > 0) return;
            hitT = 0.35f; w = w % 7 + 1;                                       // 7번(=840)마다 처음부터 다시 퍼짐 (걸어다니며 새로 감염)
            float r0 = w * WAVE;
            Fx?.Ring(R.x, R.y, r0, new Color(green.r, green.g, green.b, 0.9f), 0.5f);
            if (w == 1) { Fx?.Ring(R.x, R.y, 60, Color.white, 0.3f); Fx?.Shake(0.06f); }
            foreach (var o in RatMgr.Rats)
            {
                if (o == R || infected.Contains(o) || Dist(o.x, o.y, R.x, R.y) >= r0) continue;
                infected.Add(o);
                o.zombie = Mathf.Max(o.zombie, INFECT);
                if (!OnScreen(o.x, o.y)) continue;
                PopupCap("c4", o.x, o.y, green, 16, 0.8f, 40);
                Smoke(o.x, o.y);
                Fx?.Stars(o.x, o.y, 20, 4, green, Color.white, 60, 160);
            }
        }

        public override void Finish() { R.UltRot = 0; }
    }
}
