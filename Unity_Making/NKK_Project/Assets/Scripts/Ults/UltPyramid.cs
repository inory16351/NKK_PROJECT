using System.Collections.Generic;
using NKK.Items;
using NKK.Rats;
using UnityEngine;

namespace NKK.Ults
{
    // 파라오 쥐 · 파라오의 피라미드 (웹게임 pyramid): 피라미드가 솟아오름 → 쥐들이 계단에 줄 서서 절, 물건은 계단 타고 꼭대기로 상납
    //   → 벌떡 뒤집어 피라미드 팽이 (휩쓸기) → 비틀비틀 쓰러지며 쿵, 파라오 튕겨 나감
    // 자막: c1 받들라 · c2 다단계 아님 · c3 꼭대기로 · c4 팽이!! · c5 멈출 줄 모름 · c6 어지러워 / c7 상납! 팝업 · c8 파라오 한마디 팝업
    public class UltPyramid : UltBase
    {
        const float W = 320, H = 320, FALL = 5.7f;               // H = 그림 비율 (pyramid.png 정사각)
        public override float Dur => FALL + 0.6f;

        // 피라미드 상태 (웹 s.py)
        float px, py, pvx, pvy, rise, flipK, lift, spin, wob, top, turnT = 0.5f;
        bool said, flipped, spinning, fell; float flyX, flyZ, flyT;
        UltProp pyr, shadow;
        FxManager.Sticker[] swirls;

        // 계단에 선 쥐
        class Step0 { public Rat o; public float kx, ky, kz, dx, z, kd; public int ti; }
        readonly List<Step0> steps = new();
        // 상납 물건
        class Gift { public Item it; public float hx, hy, k0; public int side; }
        readonly List<Gift> tribute = new();

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(0.9f, () => Cap("c2"));
            Beat(1.5f, () => Cap("c3"));
            Beat(2.2f, () => Cap("c4"));
            Beat(4.0f, () => Cap("c5"));
            Beat(FALL + 0.05f, () => Cap("c6"));
            px = R.x; py = R.y;
            shadow = Prop("dot", px, py, 0, 1);
            if (shadow != null) { shadow.ground = true; shadow.flat = 0.26f; shadow.tint = new Color(0.12f, 0.06f, 0.02f, 0.3f); }
            pyr = Prop("pyramid", px, py, 0, W);
            if (pyr != null) pyr.sortBias = -3;

            // 계단 자리: 아래 단부터 4·4·3·3·2·1칸 (한 단 ≈ 높이의 11.8%)
            var slots = new List<(float dx, float z, int ti)>();
            int[] per = { 4, 4, 3, 3, 2, 1 };
            for (int ti = 0; ti < per.Length; ti++)
            {
                int m = per[ti]; float hw = W / 2 * (1 - (ti + 1) / 8f) * 0.85f;
                for (int j = 0; j < m; j++) slots.Add((m == 1 ? 0 : (j / (float)(m - 1) - 0.5f) * 2 * hw, H * 0.118f * (ti + 1), ti));
            }
            var cand = new List<Rat>();
            foreach (var o in RatMgr.Rats) if (o != R && !o.UltOn && OnScreen(o.x, o.y, 40)) cand.Add(o);
            cand.Sort((a, b) => Dist(a.x, a.y, R.x, R.y).CompareTo(Dist(b.x, b.y, R.x, R.y)));
            foreach (var o in cand)
            {
                if (steps.Count >= slots.Count) break;
                if (!GrabRat(o)) continue;
                var sl = slots[steps.Count];
                steps.Add(new Step0 { o = o, kx = o.x, ky = o.y, kz = Mathf.Max(0, o.z), dx = sl.dx, z = sl.z, ti = sl.ti, kd = steps.Count * 0.035f });
            }
            // 가까운 물건은 솟아오를 때 밀려나고, 먼 물건은 상납 행렬로
            var its = ItemsIn(R.x, R.y, ULT_R);
            its.Sort((a, b) => Dist(a.x, a.y, R.x, R.y).CompareTo(Dist(b.x, b.y, R.x, R.y)));
            foreach (var it in its)
            {
                if (Dist(it.x, it.y, R.x, R.y) < W * 0.5f) { FlingItem(it, Mathf.Atan2(it.y - R.y, it.x - R.x), 520, 380); continue; }
                if (tribute.Count < 14 && GrabItem(it)) tribute.Add(new Gift { it = it, hx = it.x, hy = it.y, k0 = 0.95f + tribute.Count * 0.07f, side = it.x > R.x ? 1 : -1 });
            }
        }

        public override void Step(float dt, float k)
        {
            if (T < 0.8f) Rise(dt);
            else if (T < 2.2f) Tribute();
            else if (T < 2.5f) Flip();
            else if (T < FALL) Spin(dt);
            else Fall(dt);
            StepRats();
            Draw();
        }

        // ① 모래를 뚫고 솟아오름 (파라오는 꼭대기에 실려 올라감)
        void Rise(float dt)
        {
            rise = Ease(T / 0.8f);
            R.x = px; R.y = py; R.z = (H - 6) * rise; R.UltPose = PoseUp(); R.face = 1;
            if (Random.value < 0.8f) for (int sx = -1; sx <= 1; sx += 2) Fx?.Dust(px + sx * Rand(0, W * 0.5f), py + Rand(-10, 16), 1, 1.2f);
            Fx?.Shake(0.03f);
            if ((hitT -= dt) <= 0) { hitT = 0.12f; Fx?.Burst(px + Rand(-W, W) * 0.45f, py + Rand(-6, 14), 4, 5, C("#e3c46a"), C("#f3e1b8"), 80, 220); }
        }

        // ② 계단의 쥐들은 절, 물건은 계단을 타고 꼭대기로 올라가 상납 (박살 → 치즈)
        void Tribute()
        {
            rise = 1; R.x = px; R.y = py; R.z = H - 6; R.face = Mathf.Sin(Time.time * 4) > 0 ? 1 : -1;
            float w = Mathf.Sin(Time.time * 10) * 0.3f;
            R.UltPose = P(tilt: -0.5f, front: 2.4f + w, farFront: 2.2f - w, back: -0.3f, farBack: 0.3f, head: -0.4f, tail: 1.3f);
            if (!said) { said = true; PopupCap("c8", R.x, R.y, Color.white, 20, 1.1f, H + 40); }
            for (int i = tribute.Count - 1; i >= 0; i--)
            {
                var g = tribute[i]; var it = g.it;
                if (!it || it.State != Item.ItemState.Held) { tribute.RemoveAt(i); continue; }
                float kk = Mathf.Clamp01((T - g.k0) / 0.55f); if (kk <= 0) continue;
                float bx = px + g.side * W * 0.3f;
                if (kk < 0.4f) { float e = Ease(kk / 0.4f); it.x = Mathf.Lerp(g.hx, bx, e); it.y = Mathf.Lerp(g.hy, py + 10, e); it.z = Mathf.Sin(e * Mathf.PI) * 60; }
                else { float e = (kk - 0.4f) / 0.6f; int st = Mathf.FloorToInt(e * 7); it.x = Mathf.Lerp(bx, px, e); it.y = py + 10 - e * 6; it.z = H * 0.118f * st + Mathf.Abs(Mathf.Sin(e * 7 * Mathf.PI)) * 16; }
                if (kk >= 1)
                {
                    tribute.RemoveAt(i); Items.Remove(it);
                    it.z = H; Blast(it, ItemD * 1.5f, Rand(0, Mathf.PI * 2), 160, 60);
                    if (OnScreen(px, py))
                    {
                        PopupCap("c7", px, py, C("#f2c14e"), 18, 0.7f, H + 20);
                        Fx?.Stars(px, py, H, 6, C("#f2c14e"), C("#fff3bf"), 100, 240);
                        Fx?.Ring(px, py, 70, C("#f2c14e"), 0.3f); Fx?.Shake(0.05f);
                    }
                }
            }
        }

        // ③ 벌떡 뒤집기: 계단의 쥐들은 튕겨 나가고 파라오는 뒤집힌 밑면 위로
        void Flip()
        {
            if (!flipped)
            {
                flipped = true;
                foreach (var g in tribute) if (g.it && g.it.State == Item.ItemState.Held) FlingItem(g.it, Rand(0, Mathf.PI * 2), 420, 420);
                tribute.Clear();
                foreach (var s in steps) { if (!s.o) continue; ReleaseRat(s.o); Ragdoll(s.o, Mathf.Atan2(s.o.y - py, s.o.x - px + Rand(-1, 1)), 520, 460); }
                steps.Clear();
                Flash(C("#fff3bf"), 0.25f); Fx?.Shake(0.3f); Fx?.Hitstop(0.05f);
                Fx?.Ring(px, py, W * 0.6f, C("#e3c46a"), 0.4f); Fx?.Dust(px, py, 10, 1.6f);
                Fx?.Burst(px, py, H * 0.5f, 16, C("#e3c46a"), Color.white, 200, 480);
            }
            float e = (T - 2.2f) / 0.3f; flipK = e; lift = Mathf.Sin(e * Mathf.PI) * 120;
            R.x = px; R.y = py; R.z = H + lift + 40 * Mathf.Sin(e * Mathf.PI); R.UltPose = PoseFlail();
        }

        // ④ 피라미드 팽이: 뾰족한 끝으로 서서 뱅글뱅글 돌며 휩쓸기 (파라오도 같이 돎)
        void Spin(float dt)
        {
            flipK = 1; lift = 0; spin += dt * 26; wob = Mathf.Sin(T * 7) * 0.12f;
            if (!spinning)
            {
                spinning = true; R.x = px; R.y = py;
                float a = AimMost(800); pvx = Mathf.Cos(a) * 430; pvy = Mathf.Sin(a) * 430;
                if (Fx) swirls = new[] { Fx.AddSticker("swirl", px, py, H * 0.35f, W * 0.7f, FALL - T, -900, true, new Color(1, 1, 1, 0.55f)), Fx.AddSticker("swirl", px, py, H * 0.7f, W * 0.95f, FALL - T, -700, true, new Color(1, 1, 1, 0.45f)) };
            }
            float ovx = pvx, ovy = pvy;
            BounceMove(ref px, ref py, ref pvx, ref pvy, W * 0.3f, dt);
            if (ovx != pvx || ovy != pvy) { Fx?.Shake(0.12f); Fx?.Ring(px, py, W * 0.4f, C("#e3c46a"), 0.3f); Fx?.Dust(px, py, 5, 1.2f); }
            if ((turnT -= dt) <= 0)
            {
                turnT = 0.5f; R.x = px; R.y = py;
                float a = AimMost(700); pvx = Mathf.Lerp(pvx, Mathf.Cos(a) * 430, 0.6f); pvy = Mathf.Lerp(pvy, Mathf.Sin(a) * 430, 0.6f);
            }
            foreach (var it in ItemsIn(px, py, W * 0.42f)) FlingItem(it, Mathf.Atan2(it.y - py, it.x - px) + 1.2f, 680, 420);
            foreach (var o in RatsNear(px, py, W * 0.4f)) Ragdoll(o, Mathf.Atan2(o.y - py, o.x - px) + 1.2f, 560, 420);
            if ((hitT -= dt) <= 0)
            {
                hitT = 0.12f;
                Shock(px, py, W * 0.35f, UltD * 0.08f, C("#e3c46a"), 0.5f);
                Fx?.Dust(px, py, 2, 1); Fx?.Spill(px, py, 22, new Color(0.89f, 0.77f, 0.42f));
            }
            if (swirls != null) for (int i = 0; i < swirls.Length; i++) swirls[i]?.Move(px, py, H * (i == 0 ? 0.35f : 0.7f));
            R.x = px; R.y = py; R.z = H; R.UltPose = PoseFlail();
            R.UltSx = Mathf.Cos(spin) >= 0 ? 1 : -1;
        }

        // ⑤ 비틀비틀 옆으로 쓰러지며 쿵 → 파라오 튕겨 나감
        void Fall(float dt)
        {
            if (swirls != null) { foreach (var s in swirls) s?.End(); swirls = null; }
            R.UltSx = 1;
            float f = Mathf.Max(0, 1 - dt * 6); pvx *= f; pvy *= f;
            BounceMove(ref px, ref py, ref pvx, ref pvy, W * 0.3f, dt);
            float e = Mathf.Clamp01((T - FALL) / 0.3f); top = e * e;
            if (e >= 1 && !fell)
            {
                fell = true;
                float fx = px - H * 0.5f, fy = py;
                Shock(fx, fy, W * 0.8f, UltD * 0.8f, C("#e3c46a"), 3);
                foreach (var it in ItemsIn(fx, fy, W * 0.75f)) FlingItem(it, Mathf.Atan2(it.y - fy, it.x - fx), 480, 560);
                foreach (var o in RatsNear(fx, fy, W * 0.7f)) Ragdoll(o, Mathf.Atan2(o.y - fy, o.x - fx), 480, 420);
                BlastActors(fx, fy, W * 0.8f, 620, UltD * 0.5f);
                Fx?.Burst(fx, fy, 20, 30, C("#e3c46a"), C("#f3e1b8"), 200, 600, 5, 11);
                Fx?.Dust(fx, fy, 14, 2.2f); Fx?.Anim("poof", fx, fy, 0, 2.2f);
                Fx?.Ring(fx, fy, W * 1.1f, C("#d9b27a"), 0.5f);
                Flash(C("#fff3bf"), 0.4f); Fx?.Shake(0.5f); Fx?.Hitstop(0.1f);
                flyX = R.x; flyZ = R.z; flyT = T;
            }
            if (fell) { float kk = Mathf.Clamp01((T - flyT) / 0.3f); R.x = flyX - kk * 160; R.z = Mathf.Max(0, flyZ * (1 - kk) + Mathf.Sin(kk * Mathf.PI) * 90); R.face = -1; }
            else { float a = top * 1.4f; R.x = px - Mathf.Sin(a) * H; R.y = py; R.z = H * Mathf.Cos(a); }
            R.UltPose = PoseFlail();
        }

        // 계단 위 쥐들: 한 명씩 폴짝 뛰어올라 자리 잡고 넙죽 절
        void StepRats()
        {
            foreach (var s in steps)
            {
                var o = s.o; if (!o) continue;
                float e = Ease(Mathf.Clamp01((T - 0.5f - s.kd) / 0.45f));
                o.x = Mathf.Lerp(s.kx, px + s.dx, e); o.y = Mathf.Lerp(s.ky, py + 14 + s.ti * 0.5f, e);
                o.z = Mathf.Lerp(s.kz, s.z * rise, e) + Mathf.Sin(e * Mathf.PI) * 90;
                o.face = s.dx > 0 ? -1 : 1;
                o.UltPose = e >= 1
                    ? (Mathf.Sin(Time.time * 9 + s.dx * 0.05f + s.ti) > 0 ? P(head: 0.6f, headX: -3, front: 1.3f, farFront: 1.2f, tilt: 0.3f, tail: 0.8f) : P(tilt: -0.5f, front: 2.4f, farFront: 2.2f, back: -0.3f, farBack: 0.3f, head: -0.4f, tail: 1.3f))
                    : PoseFlail();
            }
        }

        // 피라미드 그림: 솟음(세로로 자람) → 반 바퀴 뒤집기 → 거꾸로 서서 팽이 (좌우 납작↔펴짐) → 끝을 축으로 왼쪽으로 쓰러짐
        void Draw()
        {
            bool vis = T < FALL + 0.5f;
            float alpha = top >= 1 ? Mathf.Clamp01(1 - (T - FALL - 0.3f) / 0.2f) : 1;
            if (shadow != null)
            {
                float shW = flipK >= 1 ? W * 0.2f : W * 0.5f * rise;
                shadow.x = px; shadow.y = py; shadow.w = Mathf.Max(1, shW) * 2.8f; shadow.alpha = alpha; shadow.visible = vis;
            }
            if (pyr == null) return;
            pyr.visible = vis; pyr.alpha = alpha; pyr.y = py;
            if (flipK <= 0)
            {
                pyr.w = W; pyr.flat = Mathf.Max(0.02f, rise); pyr.flip = false; pyr.rot = 0;
                pyr.x = px + (rise < 1 ? Rand(-2, 2) : 0); pyr.z = H * pyr.flat / 2;
            }
            else if (flipK < 1)
            {
                pyr.w = W; pyr.flat = 1; pyr.flip = false;
                pyr.x = px; pyr.z = H / 2 + lift; pyr.rot = -flipK * Mathf.PI;
            }
            else
            {
                float a = -(wob - top * 1.4f), c = Mathf.Cos(spin), s = 0.35f + 0.65f * Mathf.Abs(c);
                pyr.w = W * s; pyr.flat = -1 / s; pyr.flip = c < 0; pyr.rot = a;
                pyr.x = px - Mathf.Sin(a) * H / 2; pyr.z = Mathf.Cos(a) * H / 2;
            }
        }

        // 웹 skillBlastItem: 피해로 박살이면 바로 깨지고, 버티면 튕겨 나감
        void Blast(Item it, float dmg, float ang, float spd, float vz)
        {
            if (!it || it.State == Item.ItemState.Dead) return;
            if (it.SkillHit(dmg, R)) it.Fling(0, 0, -1);
            else it.Fling(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd, vz);
        }

        public override void Finish() { R.UltSx = 1; R.z = 0; if (swirls != null) foreach (var s in swirls) s?.End(); }

        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
    }
}
