using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 쥐랜드 관광쥐 · 풍선 퍼레이드 (웹게임 balloonparade): 주변 물건에 하트 풍선 3개씩 달아 둥실둥실 (걸으며 더 달아 줌) → 풍선이 펑펑 터지며 와르르
    // 자막 c1~c4 · 팝업 c5 펑!
    public class UltBalloonParade : UltBase
    {
        const float POP = 4.8f;
        public override float Dur => 6.2f;
        static readonly Color PINK = new(0.95f, 0.72f, 0.69f), CREAM = new(1f, 0.95f, 0.75f);
        static readonly Color[] COLS = { new(0.91f, 0.47f, 0.42f), new(0.94f, 0.78f, 0.47f), new(0.62f, 0.84f, 0.66f), new(0.66f, 0.83f, 0.86f), new(0.8f, 0.71f, 0.86f), PINK };

        class Fl { public Item o; public float x0, y0, ph, h, d, t0; public bool popped; public UltProp[] bal; public UltProp shadow; }
        readonly List<Fl> fl = new();
        float addT, confT; int addN;

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(1.4f, () => Cap("c2"));
            Beat(3f, () => Cap("c3"));
            Beat(POP, () => Cap("c4"));
            var list = ItemsIn(R.x, R.y, ULT_R);
            for (int i = 0; i < list.Count && i < 16; i++) Add(list[i], Rand(0, 0.6f), 0);
        }

        void Add(Item it, float d, float t0)
        {
            if (!GrabItem(it)) return;
            var f = new Fl { o = it, x0 = it.x, y0 = it.y, ph = Rand(0, Mathf.PI * 2), h = Rand(80, 140), d = d, t0 = t0, bal = new UltProp[3] };
            int c0 = Random.Range(0, COLS.Length);
            for (int b = 0; b < 3; b++)
            {
                var p = Prop("ult_balloon", it.x, it.y, 0, 40);
                if (p != null) { p.tint = Color.Lerp(COLS[(c0 + b) % COLS.Length], Color.white, 0.25f); p.sortBias = 4 + b; }
                f.bal[b] = p;
            }
            f.shadow = Prop("dot", it.x, it.y, 0, it.R * 2.4f);
            if (f.shadow != null) { f.shadow.ground = true; f.shadow.tint = PINK; f.shadow.alpha = 0.25f; }
            fl.Add(f);
            if (OnScreen(it.x, it.y)) Fx?.Stars(it.x, it.y, 40, 5, PINK, Color.white, 60, 160);
        }

        public override void Step(float dt, float k)
        {
            float t = T;
            Walk(dt, 90);
            // 걸어가며 근처 물건에 풍선 추가 (최대 8개)
            if (t > 0.6f && t < POP - 1.2f && addN < 8 && (addT -= dt) <= 0)
            {
                addT = 0.35f;
                var it = ItemMgr.Nearest(R.x, R.y, 260);
                if (it && it.State == Item.ItemState.Rest) { int n = fl.Count; Add(it, 0, t - 0.2f); if (fl.Count > n) addN++; }
            }
            R.UltPose = P(front: 2.2f + Mathf.Sin(t * 8) * 0.4f, farFront: 0.4f, head: Mathf.Sin(t * 8) * 0.15f, tail: 1);   // 깃발 흔들기
            foreach (var o in RatsNear(R.x, R.y, 400)) o.frenzy = Mathf.Max(o.frenzy, 1);      // 신난 쥐들 광란

            foreach (var f in fl)
            {
                var o = f.o;
                if (t < POP)
                {
                    if (!o || o.State != Item.ItemState.Held) { HideBalloons(f); continue; }
                    float e = Ease(Mathf.Clamp01((t - f.t0 - 0.2f - f.d) / 1.2f));
                    o.x = f.x0 + Mathf.Sin(t * 1.6f + f.ph) * 30 * e; o.y = f.y0 + Mathf.Cos(t * 1.3f + f.ph) * 10 * e; o.z = f.h * e + Mathf.Sin(t * 3 + f.ph) * 8;
                    o.Rot = Mathf.Sin(t * 2 + f.ph) * 0.15f;
                    // 풍선 3개 묶음: 줄 끝이 물건 위에 붙게
                    float top = o.z + o.R * 1.8f, sw = Mathf.Sin(t * 2 + f.ph);
                    for (int b = 0; b < 3; b++)
                    {
                        var p = f.bal[b]; if (p == null) continue;
                        p.x = o.x + (b - 1) * 22 + sw * 10; p.y = o.y; p.z = top + HalfH(p) + (b == 1 ? 20 : 0);
                        p.rot = -(sw * 0.15f + (b - 1) * 0.2f);
                    }
                    if (f.shadow != null) { f.shadow.x = o.x; f.shadow.y = o.y; f.shadow.w = o.R * 2.4f * (1 + e * 0.3f); }
                    if (Random.value < 0.15f) Fx?.Stars(o.x + Rand(-30, 30), o.y, o.z + Rand(40, 120), 1, CREAM, Color.white, 10, 40);
                }
                else if (!f.popped && t > POP + f.d * 0.8f)
                {
                    // 풍선 펑 → 떨어짐
                    f.popped = true;
                    if (o && OnScreen(o.x, o.y))
                    {
                        float bz = o.z + o.R * 1.8f + 50;
                        Fx?.Stars(o.x, o.y, bz, 8, PINK, Color.white, 120, 300);
                        Fx?.Anim("poof", o.x, o.y, bz - 30, 0.6f);
                        PopupCap("c5", o.x, o.y, Color.white, 18, 0.5f, bz + 10);
                        Fx?.Shake(0.03f);
                    }
                    HideBalloons(f);
                    if (o && o.State == Item.ItemState.Held) DropItem(o, Rand(-80, 80), Rand(-60, 60), -80);
                }
            }

            // 색종이 비 + 살짝 파스텔 톤 (웹게임 ui)
            float a = Mathf.Min(1, t / 0.3f) * Mathf.Min(1, (Dur - t) / 0.43f);
            Flash(PINK, 0.12f * a);
            if ((confT -= dt) <= 0)
            {
                confT = 0.05f;
                var v = ViewRect();
                float cx = Rand(v.xMin, v.xMax), cy = Rand(v.yMin, v.yMax);
                var c = COLS[Random.Range(0, COLS.Length)];
                Fx?.Burst(cx, cy, Rand(250, 450), 3, c, COLS[Random.Range(0, COLS.Length)], 20, 80, 6, 11);
            }
        }

        void HideBalloons(Fl f)
        {
            if (f.bal != null) for (int b = 0; b < f.bal.Length; b++) { KillProp(f.bal[b]); f.bal[b] = null; }
            KillProp(f.shadow); f.shadow = null;
        }

        static float HalfH(UltProp p) => p != null && p.r && p.r.sprite ? p.w * p.flat * p.r.sprite.bounds.size.y / p.r.sprite.bounds.size.x * 0.5f : 0;
    }
}
