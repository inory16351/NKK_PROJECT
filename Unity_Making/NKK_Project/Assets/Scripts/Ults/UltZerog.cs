using System.Collections.Generic;
using NKK.Items;
using NKK.Rats;
using UnityEngine;

namespace NKK.Ults
{
    // 우주비행사 쥐 · 무중력 (웹게임 zerog): 물건 40개·쥐 20마리 부유 후 일괄 낙하 / c1 시작, c3 중간, c2 중력 복귀
    public class UltZerog : UltBase
    {
        public override float Dur => 6.2f;
        const float FALL = 5.05f;
        class FloatingItem { public Item item; public float height, phase, vx, vy, x0, y0; }
        class FloatingRat { public Rat rat; public float height, phase; }
        readonly List<FloatingItem> floatingItems = new();
        readonly List<FloatingRat> floatingRats = new();
        bool dropped;

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1")); Beat(FALL * 0.5f, () => Cap("c3")); Beat(FALL, () => Cap("c2"));
            int selected = 0;
            foreach (var it in ItemsIn(R.x, R.y, ULT_R))
            {
                if (selected++ == 40) break;
                if (GrabItem(it)) floatingItems.Add(new FloatingItem { item = it, height = Rand(80, 220), phase = Rand(0, 6),
                    vx = Rand(-30, 30), vy = Rand(-20, 20), x0 = it.x, y0 = it.y });
            }
            selected = 0;
            foreach (var rat in RatsNear(R.x, R.y, ULT_R))
            {
                if (selected++ == 20) break;
                if (GrabRat(rat)) floatingRats.Add(new FloatingRat { rat = rat, height = Rand(80, 220) * 0.7f, phase = Rand(0, 6) });
            }
        }

        public override void Step(float dt, float k)
        {
            float e = Ease(Mathf.Clamp01(T / 0.8f));
            R.UltPose = Swimming(0);
            if (T < FALL)
            {
                R.z = Mathf.Max(0, 30 * e + Mathf.Sin(T * 2) * 8);
                R.UltRot = Mathf.Sin(T) * 0.8f; Walk(dt, 120);
                // 절대 경과 시간으로 왕복시켜 프레임 간격과 무관하게 제자리로 돌아옴.
                float drift = Mathf.Min(T, FALL - T);
                foreach (var f in floatingItems)
                {
                    var it = f.item; if (!it || it.State != Item.ItemState.Held) continue;
                    it.z = Mathf.Max(0, f.height * e + Mathf.Sin(T * 2 + f.phase) * 8);
                    float x = f.x0 + f.vx * drift, y = f.y0 + f.vy * drift, vx = 0, vy = 0;
                    M.Stage.Confine(ref x, ref y, ref vx, ref vy, it.R, it.x, it.y, 0);
                    it.x = x; it.y = y;
                }
                foreach (var f in floatingRats)
                {
                    var rat = f.rat; if (!rat) continue;
                    rat.z = Mathf.Max(0, f.height * e + Mathf.Sin(T * 2 + f.phase) * 6);
                    rat.UltPose = Swimming(f.phase); rat.UltRot = Mathf.Sin(T + f.phase) * 1.2f;
                }
                return;
            }
            R.UltRot = 0; R.z = 0;
            if (dropped) return;
            dropped = true;
            foreach (var f in floatingItems) if (f.item && f.item.State == Item.ItemState.Held)
                DropItem(f.item, Rand(-60, 60), Rand(-40, 40), -150);
            foreach (var f in floatingRats) if (f.rat) { ReleaseRat(f.rat); f.rat.vz = -100; }
            Fx?.Ring(R.x, R.y, ULT_R * 0.55f, Col, 0.45f); Fx?.Shake(0.12f);
        }

        private RatRig.Pose Swimming(float phase) => P(front: Mathf.Sin(T * 3 + phase) * 1.2f,
            farFront: Mathf.Cos(T * 3 + phase) * 1.2f, back: Mathf.Sin(T * 2.5f + phase) * 1.2f,
            farBack: -Mathf.Sin(T * 2.5f) * 1.2f, head: 0.2f, tail: 1);

        public override void Finish() { R.z = 0; R.UltRot = 0; }
    }
}
