using UnityEngine;

namespace NKK.Ults
{
    // 기사 쥐 · 엑스칼리버 (웹게임 excalibur): 바위에 꽂힌 전설의 검 → 끄응…×3 → 바위까지 딸려 나옴 → 그걸로 빙빙 휘두르며 전진 → 바위째 쾅 (바위 박살, 진짜 검만 번쩍)
    // 자막: c1 전설의 검 · c2 바위까지 딸려 나왔다 · c3 그냥 이걸로 친다 · c4 (멈추는 법은 안 배웠다) · c5 (이제야 진짜 검) / c6~c8 = 기합 팝업 (끄응… · 끄으응…! · 끄으으으응!!!)
    public class UltExcalibur : UltBase
    {
        const float PULL = 1.35f, RAISE = 1.8f, SWING = 5.4f, L = 250, SWORD_Z = 26;
        public override float Dur => 6.3f;
        static readonly Color BLADE = new(0.749f, 0.89f, 0.918f), GOLD = new(1f, 0.953f, 0.749f);
        static readonly Color STONE0 = new(0.725f, 0.694f, 0.651f), STONE1 = new(0.557f, 0.541f, 0.518f);

        float stX, stY, sa = -Mathf.PI / 2, tipX, tipY, swayT;
        int half = int.MinValue;
        bool pulled, slam, hasTip;
        UltProp sword;

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(0.35f, () => PopupCap("c6", R.x, R.y, Color.white, 18, 0.45f, 50));
            Beat(0.7f, () => PopupCap("c7", R.x, R.y, Color.white, 20, 0.45f, 50));
            Beat(1.05f, () => PopupCap("c8", R.x, R.y, Color.white, 22, 0.45f, 50));
            Beat(1.4f, () => Cap("c2"));
            Beat(2.1f, () => Cap("c3"));
            Beat(3.7f, () => Cap("c4"));
            Beat(5.8f, () => Cap("c5"));
            stX = R.x - R.face * 46; stY = R.y + 4;
            sword = Prop("excalibur", stX, stY, 0, 120);
            PlaceStanding(0);
            // 하늘에서 내려오는 빛
            Fx?.Beam(stX, stY, 700, stX, stY, 0, new Color(1f, 0.953f, 0.749f, 0.55f), PULL);
        }

        float Aspect => sword != null && sword.r && sword.r.sprite ? sword.r.sprite.bounds.size.y / sword.r.sprite.bounds.size.x : 1.5f;

        // 바위에 꽂힌 채 (바닥 기준)
        void PlaceStanding(float jit)
        {
            if (sword == null) return;
            sword.w = 120; sword.rot = 0; sword.flat = 1;
            sword.x = stX + Rand(-1, 1) * jit; sword.y = stY; sword.z = 120 * Aspect / 2;
        }

        // 쥐 손에서 끝(tx, ty, tz)까지: 손잡이는 쥐 쪽, 바위는 끝 쪽
        void PlaceSwing(float tx, float ty, float tz)
        {
            if (sword == null) return;
            float dx = tx - R.x, dy = (ty - R.y) * World.TILT - (tz - SWORD_Z);
            float len = Mathf.Sqrt(dx * dx + dy * dy), th = Mathf.Atan2(-dy, dx);
            sword.w = (len + 50) / Aspect;
            sword.rot = th + Mathf.PI / 2;                 // 그림 아래쪽(바위)이 바깥을 향함
            sword.x = R.x + (tx - R.x) * 0.55f; sword.y = R.y + (ty - R.y) * 0.55f; sword.z = SWORD_Z + (tz - SWORD_Z) * 0.55f;
            sword.sortBias = ty > R.y ? 1 : -1;
        }

        static float AngDiff(float a, float b) { float d = a - b; return Mathf.Atan2(Mathf.Sin(d), Mathf.Cos(d)); }

        public override void Step(float dt, float k)
        {
            if (T < PULL)
            {
                // 끄응… (쥐도 바위도 덜덜)
                R.x = stX + R.face * 46; R.y = stY - 4;
                R.UltPose = P(tilt: 0.35f, front: 1.3f, farFront: 1.1f, back: -0.7f, farBack: -0.5f, head: -0.35f, tail: 1);
                R.UltJit = T > 0.3f ? 2.5f : 0;
                PlaceStanding(T > 0.3f ? 3 : 0);
                if (Random.value < 0.3f) Fx?.Dust(stX, stY, 1, 0.7f);
                return;
            }
            R.UltJit = 0;
            if (!pulled)
            {
                pulled = true; sa = -R.face * 0.2f - Mathf.PI / 2;
                Flash(Color.white, 0.35f); Fx?.Shake(0.2f);
                Fx?.Burst(stX, stY, 10, 14, STONE0, STONE1, 120, 320, 4, 8);
                Fx?.Stars(stX, stY, 60, 12, Color.white, GOLD, 150, 380);
            }
            if (T < RAISE)
            {
                // 번쩍 들어 올림 (바위째)
                R.UltPose = PoseUp();
                PlaceSwing(R.x, R.y, SWORD_Z + 120);
                return;
            }
            if (T < SWING)
            {
                // 빙빙: 검 끝에 바위가 달린 채로 회전하며 전진
                sa += dt * 8.5f; Walk(dt, 150);
                R.UltPose = P(front: 2.4f, farFront: 2.2f, head: -0.3f, tail: 1.3f);
                R.face = Mathf.Cos(sa) >= 0 ? 1 : -1;
                float tx = R.x + Mathf.Cos(sa) * L, ty = R.y + Mathf.Sin(sa) * L * 0.7f;
                PlaceSwing(tx, ty, SWORD_Z);
                // 휘두른 자국
                if (hasTip) Fx?.Beam(tipX, tipY, SWORD_Z, tx, ty, SWORD_Z, new Color(BLADE.r, BLADE.g, BLADE.b, 0.6f), 0.25f);
                tipX = tx; tipY = ty; hasTip = true;
                foreach (var it in ItemsIn(R.x, R.y, L + 40))
                    if (Mathf.Abs(AngDiff(Mathf.Atan2(it.y - R.y, it.x - R.x), sa)) < 0.45f)
                    {
                        FlingItem(it, sa + 1.4f, 640, 460);
                        Fx?.Anim("hit", it.x, it.y, 20, 1.2f);
                    }
                foreach (var o in RatsNear(R.x, R.y, L + 30))
                    if (Mathf.Abs(AngDiff(Mathf.Atan2(o.y - R.y, o.x - R.x), sa)) < 0.4f) Ragdoll(o, sa + 1.4f, 520, 420);
                // 반 바퀴마다 바람 소리 대신 흔들림·먼지
                int h = Mathf.FloorToInt(sa / Mathf.PI);
                if (h != half) { half = h; Fx?.Shake(0.05f); Fx?.Dust(tx, ty, 2, 0.9f); }
                if ((swayT -= dt) <= 0) { swayT = 0.12f; Fx?.Burst(tx, ty, SWORD_Z, 1, STONE0, STONE1, 40, 120, 3, 5); }
                return;
            }
            if (!slam)
            {
                // 바위째로 쾅 내려찍기 → 바위 박살
                slam = true; sa = AimMost();
                float tx = R.x + Mathf.Cos(sa) * 200, ty = R.y + Mathf.Sin(sa) * 140;
                Shock(tx, ty, 240, UltD * 0.8f, BLADE, 3);
                foreach (var it in ItemsIn(tx, ty, 220)) FlingItem(it, Mathf.Atan2(it.y - ty, it.x - tx), 460, 560);
                foreach (var o in RatsNear(tx, ty, 200)) Ragdoll(o, Mathf.Atan2(o.y - ty, o.x - tx), 460, 420);
                Fx?.Spill(tx, ty, 90, new Color(0.235f, 0.196f, 0.176f, 0.35f));
                var crack = Prop("ground_crack", tx, ty, 0, 200); if (crack != null) crack.ground = true;
                Fx?.Burst(tx, ty, 20, 30, STONE0, new Color(0.84f, 0.82f, 0.78f), 200, 600, 5, 11);
                Fx?.Anim("explosion", tx, ty, 0, 1.4f);
                Flash(Color.white, 0.45f); Fx?.Shake(0.5f); Fx?.Hitstop(0.08f);
                KillProp(sword); sword = null;
            }
            // 박살 난 뒤: 빛나는 진짜 검만 번쩍
            R.UltPose = P(tilt: 0.25f, front: 0.6f, farFront: 0.5f, head: 0.4f, tail: 1);
            float bx2 = R.x + Mathf.Cos(sa) * 170, by2 = R.y + Mathf.Sin(sa) * 170 * 0.7f;
            Fx?.Beam(R.x, R.y, SWORD_Z, bx2, by2, SWORD_Z, Color.white, 0.05f);
            if (Random.value < 0.3f) Fx?.Stars(bx2, by2, SWORD_Z, 2, Color.white, BLADE, 40, 140);
        }

        public override void Finish() { R.UltJit = 0; }
    }
}
