using NKK.Stage;
using UnityEngine;

namespace NKK.Ults
{
    // 공룡 잠옷 쥐 · 대멸종 (웹게임 meteorself): 운석이 자기 머리에 먼저 떨어짐(납작) → 분노의 발구름 9번 + 주변에 운석 비
    // 자막 c1~c4, c5 = 납작 팝업
    public class UltMeteorSelf : UltBase
    {
        const int STOMPS = 9;
        public override float Dur => 6.2f;
        int n; bool flat;

        public override void Begin()
        {
            Beat(0.4f, () => Cap("c1"));
            Beat(1.0f, () => Cap("c2"));
            Beat(3.0f, () => Cap("c3"));
            Beat(5.4f, () => Cap("c4"));
            // 첫 운석: 자기 머리 위로 (크고 빠르게)
            ItemMgr.DropMeteor(R, R.x, R.y, 110, UltD * 0.3f, null, 1.6f, 0.33f);
        }

        public override void Step(float dt, float k)
        {
            if (T < 0.33f) { R.UltPose = P(head: -0.6f, front: 0.8f, tail: 1); return; }
            if (T < 0.95f)
            {
                // 납작
                R.UltPose = P(sy: 0.28f, sx: 1.7f, front: -1.5f, farFront: -1.5f, back: 1.5f, farBack: 1.5f, head: 0.2f, tail: -0.4f);
                if (!flat) { flat = true; PopupCap("c5", R.x, R.y, Color.white, 18, 1, 20); Fx?.Shake(0.35f); }
                return;
            }
            R.UltPose = P(tilt: -0.6f, front: 2.6f, farFront: 2.4f, head: -0.5f, tail: 1.3f);
            // 분노의 발구름: 0.5초마다, 작게·크게 번갈아 → 마지막이 제일 큼
            bool last = n == STOMPS - 1;
            if (n < STOMPS && T >= 1.2f + n * 0.5f)
            {
                n++;
                float f = last ? 1 : n % 2 == 1 ? 0.55f : 0.8f;
                R.z = 0;
                Shock(R.x, R.y, ULT_R * f, UltD * (last ? 0.5f : 0.25f), Col, 2.4f);
                foreach (var it in ItemsIn(R.x, R.y, ULT_R * f)) if (Random.value < 0.3f) FlingItem(it, Mathf.Atan2(it.y - R.y, it.x - R.x), 300, 500);
                Fx?.Shake(0.25f);
                if (last) { Flash(Col, 0.3f); Fx?.Hitstop(0.08f); }
            }
            R.z = n < STOMPS ? Mathf.Abs(Mathf.Sin((T - 1.2f) / 0.5f * Mathf.PI)) * 50 : 0;
            if (n < STOMPS) Walk(dt, 170);
            // 운석 비: 0.2초마다 주변 물건 (없으면 빈 바닥)에
            if ((hitT -= dt) <= 0 && T < 5.6f)
            {
                hitT = 0.2f;
                var t = Pick(ItemsIn(R.x, R.y, ULT_R));
                float tx = t ? t.x : R.x + Rand(-ULT_R, ULT_R) * 0.7f, ty = t ? t.y : R.y + Rand(-ULT_R, ULT_R) * 0.5f;
                if (!t && !M.Stage.Open.Contains(StageManager.RoomOf(tx, ty))) return;
                ItemMgr.DropMeteor(R, tx, ty, 100, UltD * 0.25f, null, Rand(0.8f, 1.2f), Rand(0.7f, 1f));
            }
        }
    }
}
