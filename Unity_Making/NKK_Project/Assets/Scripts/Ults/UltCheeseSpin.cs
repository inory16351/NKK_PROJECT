using UnityEngine;

namespace NKK.Ults
{
    // 치즈 퐁듀 쥐 · 회전회오리 (테스터훈 '치즈분수남' 패러디, 가렌 E 참고): 4초 동안 빙글빙글 돌며 물건 쪽으로 이동, 0.4초마다 주변 타격 ×10
    // 자막: c1 인사 · c2 삭-제 · c3 핵꿀밤 · c4 {n}회전 · c5 양호띠 · c6 Miss · c7 쌉전드 · c8 천국 · c9 컷 · c10 참사 2탄
    public class UltCheeseSpin : UltBase
    {
        const float SPIN = 4, GAP = 0.4f, RAD = 150;
        public override float Dur => SPIN + 1.2f;
        int hits, kills; bool said3, said8;
        readonly UltProp[] orbit = new UltProp[3];

        public override void Begin()
        {
            Beat(0.0f, () => Cap("c1"));
            Beat(0.45f, () => Cap("c2"));
            for (int i = 0; i < orbit.Length; i++) orbit[i] = Prop("cheese_bullet", R.x, R.y, 20, 34);
        }

        public override void Step(float dt, float k)
        {
            if (T < SPIN)
            {
                // 팽이처럼: 좌우가 빠르게 뒤집히며 회전, 살짝 떠서 이동
                R.UltPose = P(front: 1.6f, farFront: -1.4f, back: 1.2f, farBack: -1.2f, tail: 1.5f, head: -0.25f, tilt: 0.15f);
                R.UltSx = Mathf.Cos(T * 28); R.UltLift = 6 + Mathf.Sin(T * 14) * 3;
                Walk(dt, 230);
                for (int i = 0; i < orbit.Length; i++)
                {
                    var p = orbit[i]; if (p == null) continue;
                    float a = T * 9 + i * Mathf.PI * 2 / orbit.Length;
                    p.x = R.x + Mathf.Cos(a) * RAD * 0.75f; p.y = R.y + Mathf.Sin(a) * RAD * 0.45f; p.z = 22 + Mathf.Sin(a * 2) * 6; p.rot = -a;
                }
                if (Random.value < 0.6f) Fx?.Burst(R.x + Rand(-RAD, RAD) * 0.6f, R.y + Rand(-RAD, RAD) * 0.35f, 14, 1, new Color(0.98f, 0.82f, 0.36f), new Color(1f, 0.95f, 0.7f), 120, 260);
                if ((hitT -= dt) <= 0 && hits < 10)
                {
                    hitT = GAP; hits++;
                    foreach (var it in ItemsIn(R.x, R.y, RAD))
                    {
                        float a = Mathf.Atan2(it.y - R.y, it.x - R.x);
                        if (it.SkillHit(UltD * 0.12f, R)) { kills++; FlingItem(it, a, 460, 380); }
                        else it.Damage(0.01f, R, false, a);
                        if (!said3) { said3 = true; Cap("c3"); }
                    }
                    foreach (var o in RatsNear(R.x, R.y, RAD * 0.8f)) Ragdoll(o, Mathf.Atan2(o.y - R.y, o.x - R.x), 300, 260);
                    BlastActors(R.x, R.y, RAD, 520, UltD * 0.15f);
                    Fx?.Ring(R.x, R.y, RAD, new Color(0.98f, 0.82f, 0.36f, 0.9f), 0.3f); Fx?.Dust(R.x, R.y, 3, 0.9f); Fx?.Shake(0.04f);
                    if (hits % 3 == 0) Cap("c4", hits);
                    if (!said8 && kills >= 5) { said8 = true; PopupCap("c8", R.x, R.y, new Color(1, 0.95f, 0.75f), 20, 1.2f, 70); }
                }
                return;
            }
            // 끝: 결과에 따라 한마디 → 컷.
            if (R.UltSx != 1)
            {
                R.UltSx = 1; R.UltLift = 0;
                foreach (var p in orbit) KillProp(p);
                Cap(kills == 0 ? "c6" : kills >= 12 ? "c7" : "c5");
                Fx?.Burst(R.x, R.y, 20, 18, new Color(0.98f, 0.82f, 0.36f), Color.white, 200, 480);
            }
            R.UltPose = P(tilt: -0.3f, front: 2.2f, farFront: 0.4f, head: -0.3f, tail: 1.1f);       // 엄지척
            if (T > SPIN + 0.6f && !cut) { cut = true; Cap("c9"); Cap("c10"); }
        }
        bool cut;
    }
}
