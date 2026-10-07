using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 로봇 쥐 · 트랜스폼 (웹게임 transform): 자동차로 변신 → 무면허 폭주·드리프트·벽꽝 → 분해되며 원래대로
    // 자막: c1 변신 · c2 …자동차? · c3 (운전면허 없음) · c4 (과속 단속 카메라) · c5 (분해됨) / c6~c8 = 변신 팝업 (치킹! · 철컥! · 위잉!) · c9 = 벽꽝 팝업 (쾅!) · c10 = 경적 팝업 (빵빵!)
    public class UltTransform : UltBase
    {
        const float MORPH = 0.5f, DRIVE = 5.6f, CAR_W = 175, CAR_R = 44, MAX_SP = 640;
        public override float Dur => 6.2f;
        static readonly Color GOLD = new(1f, 0.953f, 0.749f), BODY = new(0.624f, 0.698f, 0.741f), DARK = new(0.49f, 0.541f, 0.565f), RED = new(0.91f, 0.47f, 0.416f);
        static readonly string[] MORPH_POP = { "c6", "c7", "c8" };

        float cx, cy, cvx, cvy, ca, csp, honk, bumpT;
        bool morphed, broke;
        UltProp car, shadow;

        public override void Begin()
        {
            Beat(0.05f, () => Cap("c1"));
            Beat(0.7f, () => Cap("c2"));
            Beat(1.6f, () => Cap("c3"));
            Beat(3.5f, () => Cap("c4"));
            Beat(5.7f, () => Cap("c5"));
            cx = R.x; cy = R.y; ca = R.face > 0 ? 0 : Mathf.PI;
        }

        Item Nearest(float x, float y, float r0)
        {
            Item best = null; float bd = float.MaxValue;
            foreach (var it in ItemsIn(x, y, r0)) { float d = Dist(it.x, it.y, x, y); if (d < bd) { bd = d; best = it; } }
            return best;
        }

        void PlaceCar()
        {
            if (car != null)
            {
                // 위에서 본 차 그림: 진행 방향(화면 기준)으로 돌림
                car.x = cx; car.y = cy; car.z = 4;
                car.rot = Mathf.Atan2(-Mathf.Sin(ca) * World.TILT, Mathf.Cos(ca));
                car.flat = Mathf.Lerp(1, World.TILT, Mathf.Abs(Mathf.Cos(ca)));
            }
            if (shadow != null) { shadow.x = cx; shadow.y = cy + 4; }
        }

        public override void Step(float dt, float k)
        {
            bumpT -= dt;
            if (T < MORPH)
            {
                // 변신 중: 버둥버둥 + 철컥철컥
                R.UltPose = PoseFlail(); R.UltJit = 3;
                if (Random.value < 0.3f) PopupCap(Pick(MORPH_POP), R.x + Rand(-30, 30), R.y, Color.white, 16, 0.5f, 50);
                if (Random.value < 0.3f) Fx?.Stars(R.x, R.y, 20, 1, Color.white, BODY, 60, 160);
                return;
            }
            if (!morphed && T < DRIVE)
            {
                morphed = true; R.HideBody = true; R.UltJit = 0;
                Smoke(R.x, R.y); Smoke(R.x, R.y);
                Fx?.Burst(R.x, R.y, 16, 12, BODY, Color.white, 120, 320);
                Fx?.Ring(R.x, R.y, 90, Col, 0.3f);
                shadow = Prop("dot", cx, cy, 0, 190);
                if (shadow != null) { shadow.ground = true; shadow.flat = 0.45f; shadow.tint = new Color(0.12f, 0.06f, 0.02f, 0.25f); }
                car = Prop("car", cx, cy, 4, CAR_W);
                PlaceCar();
            }
            if (T < DRIVE)
            {
                // 물건 쪽으로 핸들을 꺾는데 늘 과하게 꺾음 (드리프트)
                var t = Nearest(cx, cy, 520);
                float want = t ? Mathf.Atan2(t.y - cy, t.x - cx) : ca + 0.5f;
                float da = Mathf.Atan2(Mathf.Sin(want - ca), Mathf.Cos(want - ca));
                ca += Mathf.Clamp(da, -1, 1) * dt * 4.5f + Mathf.Sin(T * 7) * dt * 1.5f;
                csp = Mathf.Min(MAX_SP, csp + dt * 900);
                float g = Mathf.Min(1, dt * 3);
                cvx += (Mathf.Cos(ca) * csp - cvx) * g; cvy += (Mathf.Sin(ca) * csp - cvy) * g;
                float px = cx, py = cy;
                cx += cvx * dt; cy += cvy * dt;
                if (M.Stage.Confine(ref cx, ref cy, ref cvx, ref cvy, CAR_R, px, py, 0.7f))
                {
                    // 벽꽝
                    ca = Mathf.Atan2(cvy, cvx); csp *= 0.5f;
                    if (bumpT <= 0)
                    {
                        bumpT = 0.3f;
                        PopupCap("c9", cx, cy, Color.white, 22, 0.6f, 50);
                        Fx?.Shake(0.15f); Fx?.Dust(cx, cy, 5, 1.2f);
                        Fx?.Anim("hit", cx, cy, 20, 1.4f); Fx?.Stars(cx, cy, 20, 8, Color.white, BODY, 120, 300);
                    }
                }
                R.x = cx; R.y = cy; R.z = 0; R.face = Mathf.Cos(ca) >= 0 ? 1 : -1;
                foreach (var it in ItemsIn(cx, cy, 62)) FlingItem(it, ca + Rand(-0.6f, 0.6f), 700, 420);
                foreach (var o in RatsNear(cx, cy, 60)) Ragdoll(o, ca + Rand(-0.8f, 0.8f), 600, 380);
                // 드리프트: 타이어 자국 + 연기
                if (Mathf.Abs(da) > 0.6f)
                {
                    if (Random.value < 0.3f) Fx?.Spill(cx, cy, 6, new Color(0.157f, 0.137f, 0.137f, 0.22f));
                    if (Random.value < 0.4f) Fx?.Dust(cx, cy, 1, 0.8f);
                }
                if ((honk -= dt) <= 0) { honk = Rand(0.6f, 1); PopupCap("c10", cx, cy, GOLD, 20, 0.6f, 70); }
                PlaceCar();
            }
            else if (!broke)
            {
                // 분해 → 원래대로
                broke = true; R.HideBody = false;
                KillProp(car); car = null; KillProp(shadow); shadow = null;
                Fx?.Burst(cx, cy, 20, 26, BODY, RED, 180, 520, 4, 9);
                Fx?.Burst(cx, cy, 20, 12, DARK, Color.white, 150, 420, 4, 9);
                Fx?.Anim("poof", cx, cy, 0, 1.3f);
                Shock(cx, cy, 150, UltD * 0.5f, BODY, 1.5f);
            }
            else R.UltPose = P(sy: 0.85f, head: 0.4f, tail: -0.3f, front: -0.5f, farFront: 0.5f);
        }

        public override void Finish() { R.HideBody = false; R.UltJit = 0; }
    }
}
