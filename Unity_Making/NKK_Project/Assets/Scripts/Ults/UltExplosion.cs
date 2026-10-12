using NKK.Stage;
using UnityEngine;

namespace NKK.Ults
{
    // 폭렬쥐 찍구밍 · 익스플로전 (기획서: 필살기에 올인, 쓰고 나면 반동으로 기절)
    // 영창하는 동안 쥐 무리가 아닌 물건이 가장 많은 쪽 바닥에 붉은 마법진이 커짐 → 초거대 폭발 → 쥐는 털썩 쓰러져 기절 (특수 능력 Ult_Power 밸류_02 초)
    // 자막 c1~c4, c5 = 털썩 팝업. 소품 magic_circle · explosion_big (UltimateManager props, Fill Props)
    public class UltExplosion : UltBase
    {
        const float BOOM = 2.4f;
        public override float Dur => 5.4f;
        float tx, ty; bool boom, fell;
        UltProp circle, blast;

        public override void Begin()
        {
            // 물건이 가장 많은 쪽으로 조금 떨어진 곳 (열린 방 안)
            float a = AimMost(), d = ULT_R * 0.7f;
            tx = R.x + Mathf.Cos(a) * d; ty = R.y + Mathf.Sin(a) * d;
            if (!M.Stage.Open.Contains(StageManager.RoomOf(tx, ty))) { tx = R.x + R.face * 60; ty = R.y; }
            R.face = tx >= R.x ? 1 : -1;
            circle = Prop("magic_circle", tx, ty, 0, 10);
            if (circle != null) { circle.ground = true; circle.alpha = 0.9f; }
            Beat(0.2f, () => Cap("c1"));
            Beat(1.1f, () => Cap("c2"));
            Beat(BOOM - 0.1f, () => Cap("c3"));
            Beat(3.7f, () => Cap("c4"));
        }

        public override void Step(float dt, float k)
        {
            if (T < BOOM)
            {
                // 영창: 앞발을 치켜들고 덜덜, 마법진이 커지며 돎
                float e = T / BOOM;
                R.UltPose = PoseUp(1); R.UltJit = 1 + e * 3;
                if (circle != null) { circle.w = ULT_R * 2.6f * Ease(e); circle.rot = T * 1.2f; circle.Apply(); }
                if ((hitT -= dt) <= 0)
                {
                    hitT = 0.07f;
                    float ra = Rand(0, Mathf.PI * 2), rr = ULT_R * 1.3f * Ease(e);
                    if (OnScreen(tx, ty)) Fx?.Burst(tx + Mathf.Cos(ra) * rr, ty + Mathf.Sin(ra) * rr * 0.6f, 6, 2, new Color(1, 0.3f, 0.25f), new Color(1, 0.75f, 0.45f), 40, 160, 4, 8);
                    Fx?.Spark(R.x, R.y, 46, new Color(1, 0.4f, 0.3f), 30, 0.12f);
                }
                Fx?.Shake(0.01f * e);
                return;
            }
            if (!boom)
            {
                boom = true; R.UltJit = 0;
                float rad = ULT_R * 1.5f;
                Flash(Color.white, 0.8f); Fx?.Hitstop(0.15f); Fx?.Shake(0.9f);
                Shock(tx, ty, rad, UltD * 1.6f, new Color(1, 0.4f, 0.3f), 5);
                foreach (var it in ItemsIn(tx, ty, rad)) FlingItem(it, Mathf.Atan2(it.y - ty, it.x - tx) + Rand(-0.3f, 0.3f), Rand(520, 820), Rand(450, 700));
                BlastActors(tx, ty, rad, 900, UltD * 1.2f);
                foreach (var o in RatsNear(tx, ty, rad)) Ragdoll(o, Mathf.Atan2(o.y - ty, o.x - tx), 520, 420);
                for (int i = 0; i < 3; i++) Fx?.Ring(tx, ty, rad * (0.6f + i * 0.4f), i % 2 == 0 ? new Color(1, 0.45f, 0.3f) : new Color(1, 0.85f, 0.5f), 0.5f + i * 0.2f);
                Fx?.Burst(tx, ty, 30, 40, new Color(1, 0.5f, 0.25f), new Color(1, 0.92f, 0.6f), 300, 900, 8, 16);
                Fx?.Dust(tx, ty, 24, 2.4f);
                SfxManager.PlayAt("wall_blast", tx, ty); SfxManager.PlayAt("superjump_slam", tx, ty);
                KillProp(circle); circle = null;
                blast = Prop("explosion_big", tx, ty, 0, ULT_R);
                // 쥐: 폭풍에 뒤로 밀려남
                float ba = Mathf.Atan2(R.y - ty, R.x - tx);
                MoveRat(R, Mathf.Cos(ba) * 40, Mathf.Sin(ba) * 40);
            }
            float t2 = T - BOOM;
            if (blast != null)
            {
                float e = Mathf.Clamp01(t2 / 1.3f);
                blast.w = ULT_R * (1 + 2.4f * Ease(e)); blast.z = 40 + e * 120; blast.alpha = 1 - e * e; blast.sortBias = 40; blast.Apply();
                if (e >= 1) { KillProp(blast); blast = null; }
            }
            if (t2 < 0.6f) { R.UltPose = PoseFlail(); R.z = Mathf.Sin(t2 / 0.6f * Mathf.PI) * 40; return; }
            // 털썩: 엎어져서 꼼짝 못 함 (가끔 꼬리만 꿈틀)
            R.z = 0;
            if (!fell) { fell = true; PopupCap("c5", R.x, R.y, Color.white, 18, 1, 30); Fx?.Dust(R.x, R.y, 6, 1); }
            R.UltPose = P(head: 0.5f, tail: Mathf.Sin(T * 3) > 0.9f ? 0.4f : -0.3f, front: -1.4f, farFront: -1.3f, back: 1.4f, farBack: 1.3f, sx: 1.25f, sy: 0.6f);
        }

        public override void Cleanup()
        {
            // 반동 기절: 필살기가 풀린 뒤에 걸어야 안 지워짐
            var r = R; float st = r ? r.UltSelfStun : 0;
            if (r && st > 0) RatMgr.Later(0.05f, () => { if (r) r.Stun(st); });
        }
    }
}
