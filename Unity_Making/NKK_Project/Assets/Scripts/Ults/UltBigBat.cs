using UnityEngine;

namespace NKK.Ults
{
    // 뱀파이어 쥐 · 어둠의 날개 (웹게임 bigbat): 거대 박쥐로 변신 → 날개가 너무 커서 8자로 폭주 비행, 낮게 날 때 물건을 쓸어감 → 철푸덕
    // 자막: c1 어둠의 날개여 · c2 조종이 안 돼 · c3 브레이크 좀 · c4 (철푸덕)
    public class UltBigBat : UltBase
    {
        const float FLY = 5.7f, BAT_W = 220;
        public override float Dur => 6.2f;
        static readonly Color TRAIL0 = new(0.35f, 0.24f, 0.31f, 0.6f), TRAIL1 = new(0.55f, 0.4f, 0.55f, 0.5f);

        float bx, by, bz = 60, ba; int flap = -1;
        UltProp bat, shadow;

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(1.1f, () => Cap("c2"));
            Beat(3.3f, () => Cap("c3"));
            Beat(5.6f, () => Cap("c4"));
            Smoke(R.x, R.y); Smoke(R.x, R.y);
            Fx?.Burst(R.x, R.y, 30, 16, TRAIL0, Col, 120, 360);
            R.HideBody = true;
            bx = R.x; by = R.y;
            shadow = Prop("dot", bx, by, 0, 160);
            if (shadow != null) { shadow.ground = true; shadow.tint = new Color(0.12f, 0.06f, 0.02f, 0.22f); }
            bat = Prop("giant_bat", bx, by, bz, BAT_W);
        }

        public override void Step(float dt, float k)
        {
            if (T < FLY)
            {
                // 8자 폭주 비행
                float t = T * 1.7f, px = bx, py = by, vx = 0, vy = 0;
                bx = X0 + Mathf.Sin(t) * 330; by = Y0 + Mathf.Sin(t * 2) * 170; bz = 60 + Mathf.Sin(t * 3) * 45;
                M.Stage.Confine(ref bx, ref by, ref vx, ref vy, 40, px, py, 0);
                ba = Mathf.Atan2(by - py, bx - px);
                R.x = bx; R.y = by; R.z = bz; R.face = Mathf.Cos(ba) >= 0 ? 1 : -1;
                if (bz < 70)
                {
                    // 낮게 스칠 때 쓸어감
                    foreach (var it in ItemsIn(bx, by, 90)) FlingItem(it, ba + Rand(-0.8f, 0.8f), 560, 420);
                    foreach (var o in RatsNear(bx, by, 90)) Ragdoll(o, ba + Rand(-1, 1));
                    if (Random.value < 0.5f) Fx?.Dust(bx, by, 1, 1);
                }
                if (Random.value < 0.3f) Fx?.Burst(bx, by, bz, 2, TRAIL0, TRAIL1, 0, 30, 8, 14);
                // 날갯짓마다 바람
                int f = Mathf.FloorToInt(T * 5);
                if (f != flap) { flap = f; if (bz < 80) Fx?.Ring(bx, by, 70, new Color(1, 1, 1, 0.35f), 0.25f); }
                if (bat != null)
                {
                    float h = bat.r && bat.r.sprite ? BAT_W * bat.r.sprite.bounds.size.y / bat.r.sprite.bounds.size.x : BAT_W;
                    bat.flat = 1 - 0.25f * Mathf.Abs(Mathf.Sin(T * 16));
                    bat.x = bx; bat.y = by; bat.z = bz - 50 + h * bat.flat / 2;
                }
                if (shadow != null) { shadow.x = bx; shadow.y = by; shadow.w = 160 * (1 - bz / 400); }
            }
            else if (R.HideBody)
            {
                // 변신 풀림 → 추락
                R.HideBody = false; R.z = 0;
                KillProp(bat); bat = null; KillProp(shadow); shadow = null;
                Smoke(R.x, R.y);
                Fx?.Burst(R.x, R.y, 20, 14, TRAIL0, Col, 150, 380);
                Fx?.Ring(R.x, R.y, 100, Col, 0.3f);
                Fx?.Shake(0.15f);
            }
            else R.UltPose = P(sy: 0.45f, sx: 1.5f, front: -1.4f, farFront: -1.4f, back: 1.4f, farBack: 1.4f, head: 0.2f);
        }

        public override void Finish() { R.HideBody = false; }
    }
}
