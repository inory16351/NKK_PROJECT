using UnityEngine;

namespace NKK.Ults
{
    // 드래곤 쥐 · 드래곤 퐁듀 (웹게임 fondue): 불 대신 치즈 퐁듀 브레스를 좌우로 휘저으며 어슬렁 → 바닥이 퐁듀로 미끌미끌, 물건·쥐가 쭉 미끄러져 날아감
    // 자막: c1 크아앙 · c2 퐁듀 주의 · c3 고소한 냄새 · c4 바닥 전체 퐁듀 / c5 미끄덩! 팝업
    public class UltFondue : UltBase
    {
        const float RANGE = 560, CONE = 0.4f;
        public override float Dur => 6.2f;
        static readonly Color cheese = C("#f2c94c"), cream = C("#fff3bf"), fondue = C("#f0c878");

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(0.7f, () => Cap("c2"));
            Beat(2.2f, () => Cap("c3"));
            Beat(4.2f, () => Cap("c4"));
            R.face = Mathf.Cos(AimMost()) >= 0 ? 1 : -1;
        }

        public override void Step(float dt, float k)
        {
            R.UltPose = P(head: -0.35f, headX: -4, front: 0.4f, farFront: 0.3f, tail: 1.1f, sx: 1.05f);
            Walk(dt, 110);                                                    // 퐁듀를 뿜으며 어슬렁
            if (T < 0.3f || T > 5.4f) return;
            float a = (R.face > 0 ? 0 : Mathf.PI) + Mathf.Sin(T * 3) * 0.7f;
            // 브레스 (입에서 부채꼴로) — 입 위치는 리그 머리 그림에서 (웹게임 ratMouth)
            Mouth(out float mx, out float mz);
            Fx?.Spray(mx, R.y + 1, mz, a, RANGE * 0.83f, 7, 0.35f, cheese, cream);
            if (Random.value < 0.4f) Fx?.Burst(mx + Mathf.Cos(a) * 6, R.y + 1, mz, 2, cheese, fondue, 60, 160, 5, 9);
            if (Random.value < 0.2f) Fx?.Stars(mx, R.y + 1, mz, 1, cream, cheese, 20, 70);          // 입가 반짝
            if ((hitT -= dt) > 0) return;
            hitT = 0.1f;
            // 퐁듀 웅덩이 (바닥 얼룩)
            float d = Rand(80, RANGE), px = R.x + Mathf.Cos(a) * d, py = R.y + Mathf.Sin(a) * d;
            Fx?.Spill(px, py, Rand(30, 60), fondue);
            if (Random.value < 0.3f) Fx?.Burst(px, py, 4, 4, fondue, cream, 60, 180);
            // 부채꼴 안: 물건은 반은 날아가고 반은 쭉 미끄러짐
            foreach (var it in ItemsIn(R.x, R.y, RANGE))
            {
                if (Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(it.y - R.y, it.x - R.x) * Mathf.Rad2Deg, a * Mathf.Rad2Deg)) >= CONE * Mathf.Rad2Deg) continue;
                if (Random.value < 0.5f) FlingItem(it, a + Rand(-0.8f, 0.8f), 760, 140);
                else { float b = a + Rand(-0.6f, 0.6f); it.AddPush(Mathf.Cos(b) * 1300, Mathf.Sin(b) * 1300); Fx?.Dust(it.x, it.y, 1, 0.7f); }
            }
            foreach (var o in RatsNear(R.x, R.y, RANGE))
            {
                if (Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(o.y - R.y, o.x - R.x) * Mathf.Rad2Deg, a * Mathf.Rad2Deg)) >= CONE * Mathf.Rad2Deg || Random.value >= 0.3f) continue;
                Ragdoll(o, a + Rand(-1, 1), 700, 60);
                if (OnScreen(o.x, o.y) && Random.value < 0.3f) PopupCap("c5", o.x, o.y, cream, 16, 0.7f, 40);
            }
        }

        // 입 = 머리 그림의 (0.07, 0.72) 지점 (왼쪽을 보는 그림, 코끝 살짝 아래). 리그가 없으면 몸 앞 고정 위치
        void Mouth(out float mx, out float mz)
        {
            float sc = R.Manager.ratScale * R.GradeData.size;
            var rig = R.rig;
            if (!rig || rig.IsSingle || !rig.head || !rig.head.sprite) { mx = R.x + R.face * 20 * sc; mz = R.z + 13 * sc; return; }
            var b = rig.head.sprite.bounds;
            var w = rig.head.transform.TransformPoint(new Vector3(b.min.x + b.size.x * 0.07f, b.max.y - b.size.y * 0.72f, 0));
            mx = w.x / World.U; mz = w.y / World.U + R.y * World.TILT;
        }

        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
    }
}
