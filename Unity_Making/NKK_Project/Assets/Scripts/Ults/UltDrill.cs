using NKK.Items;
using NKK.Stage;
using UnityEngine;

namespace NKK.Ults
{
    // 발레리나 생쥐 · 무한 회전 (웹게임 drill): 너무 빨리 돌아서 땅을 뚫고 0.32초마다 여기저기서 솟구침 ×17 → 어지러움
    // 자막: c1 32회전 · c2 멈출 수가 없어 · c3 64회전 · c4 (어지러움)
    public class UltDrill : UltBase
    {
        const int POPS = 17;
        const float CYC = 0.32f, START = 0.5f;
        public override float Dur => 6.3f;
        static readonly Color DIRT0 = new(0.545f, 0.416f, 0.29f), DIRT1 = new(0.663f, 0.541f, 0.416f);

        int pops = -1; float fx, fy, tx, ty;
        readonly bool[] popped = new bool[POPS];

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(0.7f, () => Cap("c2"));
            Beat(3.0f, () => Cap("c3"));
            Beat(5.9f, () => Cap("c4"));
        }

        // 다음 솟구칠 곳: 같은 방 안 가장 가까운 물건 (땅속으로 벽을 넘지 않게), 없으면 방 안 아무 데
        void PickTarget()
        {
            var rm = StageManager.RoomOf(R.x, R.y);
            Item best = null; float bd = float.MaxValue;
            foreach (var it in ItemsIn(R.x, R.y, 560))
            {
                if (StageManager.RoomOf(it.x, it.y) != rm) continue;
                float d = Dist(it.x, it.y, R.x, R.y); if (d < bd) { bd = d; best = it; }
            }
            if (best) { tx = best.x; ty = best.y; return; }
            tx = Mathf.Clamp(X0 + Rand(-200, 200), rm.x * World.RW + 60, (rm.x + 1) * World.RW - 60);
            ty = Mathf.Clamp(Y0 + Rand(-150, 150), rm.y * World.RH + 60, (rm.y + 1) * World.RH - 60);
        }

        public override void Step(float dt, float k)
        {
            R.UltPose = P(front: 2.6f, farFront: 2.6f, back: -0.2f, farBack: 0.2f, head: -0.3f, tail: 0.5f);
            R.UltSx = Mathf.Cos(T * 30);                     // 팽이 회전
            if (T < START) { if (Random.value < 0.6f) Fx?.Dust(R.x, R.y, 1, 1); return; }
            int idx = Mathf.FloorToInt((T - START) / CYC);
            float ck = ((T - START) % CYC) / CYC;
            if (idx >= POPS)
            {
                // 멈춤: 어지러워서 비틀
                R.HideBody = false; R.UltSx = 1;
                R.z = Mathf.Max(0, R.z - dt * 400);
                R.UltPose = P(head: Mathf.Sin(T * 9) * 0.4f, tilt: Mathf.Sin(T * 6) * 0.25f, tail: -0.3f, front: 0.4f, farFront: -0.3f);
                return;
            }
            if (idx != pops) { pops = idx; fx = R.x; fy = R.y; PickTarget(); }
            bool under = ck < 0.6f;
            R.HideBody = under;
            if (under)
            {
                // 땅속 이동: 흙 둔덕이 따라감
                float e = ck / 0.6f;
                R.x = Mathf.Lerp(fx, tx, e); R.y = Mathf.Lerp(fy, ty, e); R.z = 0;
                if (Random.value < 0.7f) Fx?.Dust(R.x, R.y, 1, 0.7f);
                if (Random.value < 0.4f) Fx?.Burst(R.x, R.y, 4, 1, DIRT0, DIRT1, 40, 120, 3, 5);
            }
            else if (!popped[idx])
            {
                // 솟구침!
                popped[idx] = true;
                foreach (var it in ItemsIn(R.x, R.y, 90)) FlingItem(it, Rand(0, Mathf.PI * 2), 220, 650);
                foreach (var o in RatsNear(R.x, R.y, 60)) Ragdoll(o, Rand(0, Mathf.PI * 2), 250, 500);
                if (OnScreen(R.x, R.y))
                {
                    Fx?.Burst(R.x, R.y, 6, 12, DIRT0, Col, 120, 320, 3, 6);
                    Fx?.Dust(R.x, R.y, 6, 1);
                    Fx?.Ring(R.x, R.y, 90, Col, 0.3f);
                    Fx?.Shake(0.05f);
                }
            }
            else R.z = Mathf.Sin((ck - 0.6f) / 0.4f * Mathf.PI) * 70;
        }

        public override void Finish() { R.HideBody = false; R.UltSx = 1; }
    }
}
