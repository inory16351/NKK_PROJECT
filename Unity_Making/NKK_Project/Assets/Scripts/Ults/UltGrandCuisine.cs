using System.Collections.Generic;
using NKK.Humans;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 슈퍼 요리사 쥐 · 그랑 퀴진 (웹게임 grandcuisine, 쥐는 내내 요리사를 탄 채): 주변 물건을 거대 냄비에 차례로 퐁당 → 보글보글 (끓는 중에도 더 넣음) → "완성!" 순간 대폭발
    // 자막 c1~c4 · 팝업 c5~c7 퐁당/풍덩/첨벙 · c8 재료 {n}개 대왕 치즈 요리! · c9~c11 사람 비명 · c12~c15 요리사 한마디
    public class UltGrandCuisine : UltBase
    {
        const float DONE = 4.9f, BOIL = 1.9f, POT_W = 150;
        public override float Dur => 6.2f;
        static readonly Color GOLD = new(0.94f, 0.78f, 0.47f), CREAM = new(1f, 0.95f, 0.75f), ORANGE = new(0.89f, 0.6f, 0.35f), HOT = new(1f, 0.55f, 0.4f);

        class Ing { public Item it; public float x0, y0, t0, h; public bool done; }
        readonly List<Ing> ing = new();
        float px, py, addT, sayT, fireT; int inN, addN; bool boom;
        UltProp pot;

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(BOIL, () => Cap("c2"));
            Beat(3.3f, () => Cap("c3"));
            Beat(DONE, () => Cap("c4"));
            // 냄비: 바라보는 쪽 앞 (열린 방 안으로)
            px = R.x + R.face * 170; py = R.y + 10;
            float vx = 0, vy = 0; M.Stage.Confine(ref px, ref py, ref vx, ref vy, 80, R.x, R.y, 0);
            pot = Prop("ult_pot", px, py, 0, POT_W);      // 거대 냄비 (UltProps/ult_pot)
            if (pot != null) { pot.z = HalfH(pot); pot.sortBias = 6; }
            // 주변 물건 전부 (가까운 순으로 차례차례 날아 들어감)
            var list = ItemsIn(px, py, ULT_R + 200);
            list.Sort((a, b) => Dist(a.x, a.y, px, py).CompareTo(Dist(b.x, b.y, px, py)));
            if (list.Count > 60) list.RemoveRange(60, list.Count - 60);
            for (int i = 0; i < list.Count; i++)
                if (GrabItem(list[i])) ing.Add(new Ing { it = list[i], x0 = list[i].x, y0 = list[i].y, t0 = 0.25f + i * (1.5f / Mathf.Max(1, list.Count)), h = Rand(220, 360) });
            // 주방 주인들 비명
            string[] shout = { "c9", "c10", "c11" };
            foreach (var h in ItemMgr.Humans)
                if (Dist(h.x, h.y, px, py) < ULT_R && h.State != Human.HState.Fly && h.State != Human.HState.Dead)
                    PopupCap(Pick(shout), h.x, h.y, Color.white, 18, 1.4f, 170);
            Fx?.Anim("poof", px, py, 0, 1.4f); Fx?.Ring(px, py, 120, GOLD, 0.4f); Fx?.Shake(0.15f);
        }

        public override void Step(float dt, float k)
        {
            float t = T;
            R.face = px > R.x ? 1 : -1;
            R.MountJit = t < 3.3f ? 1.5f : 0;            // 요리사(탈것): 재료 넣는 동안 덜덜, 대폭발 뒤엔 지쳐 뻗음 (Rat.Mount.cs)
            R.UltPose = P(front: 2.4f + Mathf.Sin(t * 16) * 0.5f, farFront: 2.0f + Mathf.Cos(t * 16) * 0.5f, head: -0.25f, tail: 1.2f, tilt: -0.15f);   // 국자 휘휘
            if (t < DONE && (sayT -= dt) <= 0) { sayT = Rand(0.5f, 0.8f); PopupCap(Pick(new[] { "c12", "c13", "c14", "c15" }), R.x, R.y, Color.white, 18, 0.5f, 70); }

            // 끓는 동안에도 주변 물건을 하나씩 더 퐁당 (최대 20개)
            if (t > BOIL && t < DONE - 0.5f && addN < 20 && (addT -= dt) <= 0)
            {
                addT = 0.2f;
                var it = ItemMgr.Nearest(px, py, ULT_R + 200);
                if (it && GrabItem(it)) { addN++; ing.Add(new Ing { it = it, x0 = it.x, y0 = it.y, t0 = t, h = Rand(220, 360) }); }
            }
            // 재료가 포물선으로 날아 냄비 속으로 (냄비 뒤로 숨음)
            foreach (var g in ing)
            {
                var it = g.it; if (!it || it.State != Item.ItemState.Held || g.done) continue;
                float e = Mathf.Clamp01((t - g.t0) / 0.55f); if (e <= 0) continue;
                it.x = Mathf.Lerp(g.x0, px + Rand(-6, 6), e); it.y = Mathf.Lerp(g.y0, py - 6, e); it.z = Mathf.Sin(e * Mathf.PI) * g.h + e * 110; it.Rot += dt * 14;
                if (e >= 1)
                {
                    g.done = true; inN++; it.z = 50;
                    if (OnScreen(px, py))
                    {
                        Fx?.Burst(px, py, 115, 4, GOLD, CREAM, 60, 180, 3, 6);
                        if (inN % 3 == 0) { PopupCap(Pick(new[] { "c5", "c6", "c7" }), px, py, Color.white, 18, 0.5f, 140); Fx?.Ring(px, py, 70, GOLD, 0.25f); }
                    }
                }
            }
            // 냄비 속 재료는 같이 들썩
            foreach (var g in ing) if (g.done && g.it && g.it.State == Item.ItemState.Held) { g.it.x = px + Rand(-20, 20); g.it.y = py - 6; g.it.z = 50 + Rand(0, 10); }

            float hot = Mathf.Clamp01((t - BOIL) / 1.4f), fill = ing.Count > 0 ? inN / (float)ing.Count : 1;
            if (pot != null && !boom)
            {
                float sh = t > BOIL ? 1 + Mathf.Min(2.4f, t - BOIL) * 3 : 0;
                pot.x = px + Rand(-sh, sh); pot.y = py; pot.z = HalfH(pot) + (t > BOIL ? Mathf.Abs(Mathf.Sin(t * 22)) * sh * 0.6f : 0);
                pot.tint = Color.Lerp(Color.white, HOT, hot * 0.45f);
                pot.flat = 1 + Mathf.Sin(t * 18) * 0.02f * hot;
            }
            // 불 (냄비 밑) + 김 + 보글보글 + 점점 커지는 흔들림
            if (!boom && (fireT -= dt) <= 0)
            {
                fireT = 0.06f;
                Fx?.Burst(px + Rand(-50, 50), py + 4, 4, 1, ORANGE, GOLD, 20, 60, 4, 8);
                if (t > BOIL)
                {
                    Fx?.Burst(px + Rand(-50, 50), py, HalfH(pot) * 2 + 4, 2, Color.white, CREAM, 20, 80, 6, 11);
                    if (Random.value < 0.4f + fill * 0.4f) Fx?.Stars(px + Rand(-40, 40), py, HalfH(pot) * 2, 1, CREAM, GOLD, 20, 70);
                }
            }
            if (t > BOIL && t < DONE) Fx?.Shake(0.01f + Mathf.Min(1.4f, t - BOIL) * 0.01f);
            if (t > DONE - 0.6f && t < DONE && Random.value < 0.4f) Fx?.Ring(px, py, Rand(60, 120), HOT, 0.2f);

            if (t >= DONE && !boom) Boom();
        }

        // 완성 → 펑!! 재료 전부 치즈 폭죽으로 (필살기 피해 ×3, 버틴 재료는 사방으로)
        void Boom()
        {
            boom = true; R.MountTired = true;
            foreach (var g in ing)
            {
                var it = g.it; if (!it || it.State != Item.ItemState.Held) continue;
                it.x = px + Rand(-40, 40); it.y = py + Rand(-30, 30); it.z = 100;
                float a = Rand(0, Mathf.PI * 2);
                DropItem(it, Mathf.Cos(a) * 420, Mathf.Sin(a) * 420, 420, ItemD * 3);
            }
            Shock(px, py, 420, UltD * 1.5f, GOLD, 3);
            foreach (var it in ItemsIn(px, py, 420)) FlingItem(it, Mathf.Atan2(it.y - py, it.x - px), 620, 560);
            BlastActors(px, py, 420, 720, UltD * 3);
            foreach (var o in RatsNear(px, py, 300)) Ragdoll(o, Mathf.Atan2(o.y - py, o.x - px), 420, 420);
            Fx?.Anim("explosion", px, py, 20, 2.2f);
            Fx?.Burst(px, py, 110, 40, GOLD, new Color(0.95f, 0.79f, 0.3f), 150, 520, 6, 11);
            Fx?.Stars(px, py, 120, 40, CREAM, ORANGE, 300, 900);
            Fx?.Ring(px, py, 260, Color.white, 0.4f); Fx?.Ring(px, py, 480, GOLD, 0.6f);
            Fx?.Spill(px, py, 120, GOLD);
            Flash(CREAM, 0.6f); Fx?.Shake(0.55f); Fx?.Hitstop(0.1f);
            PopupCap("c8", px, py, GOLD, 30, 1.6f, 200, inN);
            KillProp(pot); pot = null;
        }

        public override void Finish() { R.MountTired = false; R.MountJit = 0; }

        static float HalfH(UltProp p) => p != null && p.r && p.r.sprite ? p.w * p.flat * p.r.sprite.bounds.size.y / p.r.sprite.bounds.size.x * 0.5f : 0;
    }
}
