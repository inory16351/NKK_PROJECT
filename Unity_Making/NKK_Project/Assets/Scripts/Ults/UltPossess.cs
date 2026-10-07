using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 유령 쥐 · (웹게임 possess): 주변 물건에 빙의 (눈이 생김) → 물건끼리 서로 쫓아다니며 들이받음, 싸움판이 줄면 새로 빙의
    // 자막 c1~c3, c4~c6 = 부딪힐 때 팝업 ('쾅!' '퍽!' '우우!' 중 하나)
    public class UltPossess : UltBase
    {
        public override float Dur => 6.2f;

        class Ghost { public float ph; public Item tgt; public UltProp[] eyes; }
        readonly Dictionary<Item, Ghost> ghosts = new();
        int extra; float reT;
        static readonly string[] Bonk = { "c4", "c5", "c6" };
        static readonly Color Pupil = new(0.11f, 0.09f, 0.15f), Mist = new(0.91f, 0.88f, 0.9f);

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(1.2f, () => Cap("c2"));
            Beat(3.2f, () => Cap("c3"));
            var l = ItemsIn(R.x, R.y, ULT_R);
            for (int i = 0; i < l.Count && i < 28; i++) Possess(l[i], false);
        }

        bool Possess(Item it, bool smoke)
        {
            if (!GrabItem(it)) return false;
            var g = new Ghost { ph = Rand(0, 6), eyes = new UltProp[4] };
            for (int e = 0; e < 2; e++)
            {
                var w = Prop("dot", it.x, it.y, 0, 10); if (w != null) { w.tint = Color.white; w.sortBias = 3; }
                var p = Prop("dot", it.x, it.y, 0, 5); if (p != null) { p.tint = Pupil; p.sortBias = 4; }
                g.eyes[e * 2] = w; g.eyes[e * 2 + 1] = p;
            }
            ghosts[it] = g;
            if (OnScreen(it.x, it.y)) { if (smoke) Smoke(it.x, it.y); Fx?.Stars(it.x, it.y, 20, 3, Color.white, Mist, 80, 200); }
            return true;
        }

        void Unpossess(Item it)
        {
            if (!ghosts.TryGetValue(it, out var g)) return;
            foreach (var p in g.eyes) KillProp(p);
            ghosts.Remove(it);
        }

        public override void Step(float dt, float k)
        {
            float s10 = Mathf.Sin(Time.time * 10) * 0.4f;
            var up = PoseUp(); up.front = 2 + s10; up.farFront = 2 - s10; R.UltPose = up;
            R.z = 20 + Mathf.Sin(Time.time * 4) * 8;
            Walk(dt, 140);                                        // 둥둥 떠다니며 조종
            // 싸움판이 줄면 주변 물건을 새로 빙의시킴 (추가는 최대 14개, 끝나기 1.2초 전까지)
            if (T > 1.5f && T < 5 && (reT -= dt) <= 0)
            {
                reT = 0.7f;
                var l = ItemsIn(R.x, R.y, ULT_R);
                for (int i = 0; i < l.Count && i < 4; i++) if (extra < 14 && Items.Count < 12 && Possess(l[i], true)) extra++;
            }
            foreach (var it in new List<Item>(Items))
            {
                if (!it || !ghosts.TryGetValue(it, out var g)) continue;
                it.z = 26 + Mathf.Sin(Time.time * 6 + g.ph) * 10; it.Rot += dt * 2;
                PlaceEyes(it, g);
                if (T < 0.5f) continue;
                if (!g.tgt || !Items.Contains(g.tgt))
                {
                    var others = new List<Item>(); foreach (var o in Items) if (o != it) others.Add(o);
                    g.tgt = Pick(others);
                }
                var t = g.tgt; if (!t) continue;
                float dx = t.x - it.x, dy = t.y - it.y, d = Mathf.Max(1, Mathf.Sqrt(dx * dx + dy * dy));
                it.x += dx / d * 300 * dt + Mathf.Sin(Time.time * 9 + g.ph) * 60 * dt; it.y += dy / d * 300 * dt;
                if (d < it.R + t.R)
                {
                    float a = Mathf.Atan2(dy, dx), mx = (it.x + t.x) / 2, my = (it.y + t.y) / 2;
                    if (OnScreen(mx, my))
                    {
                        PopupCap(Pick(Bonk), mx, my, Color.white, 20, 0.6f, 50);
                        Fx?.Anim("hit", mx, my, 26, 1.2f); Fx?.Stars(mx, my, 30, 8, Color.white, Mist, 150, 380); Fx?.Ring(mx, my, 70, Mist, 0.25f);
                        Fx?.Shake(0.06f);
                    }
                    Unpossess(it); Unpossess(t);
                    FlingItem(it, a + Mathf.PI, 420, 450); FlingItem(t, a, 420, 450);
                }
            }
            // 날아가 버린 물건의 눈 치우기
            foreach (var it in new List<Item>(ghosts.Keys)) if (!it || !Items.Contains(it)) Unpossess(it);
        }

        void PlaceEyes(Item it, Ghost g)
        {
            float ez = it.z + it.R * 0.9f;
            for (int e = 0; e < 2; e++)
            {
                float ox = e == 0 ? -6 : 6;
                var w = g.eyes[e * 2]; var p = g.eyes[e * 2 + 1];
                if (w != null) { w.x = it.x + ox; w.y = it.y; w.z = ez; }
                if (p != null) { p.x = it.x + ox + Mathf.Sign(ox); p.y = it.y; p.z = ez - 1; }
            }
        }

        public override void Finish()
        {
            foreach (var it in new List<Item>(ghosts.Keys)) Unpossess(it);
            Fx?.Ring(R.x, R.y, 160, Mist, 0.4f);
        }
    }
}
