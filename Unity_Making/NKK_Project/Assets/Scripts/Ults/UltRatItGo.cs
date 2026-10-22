using UnityEngine;

namespace NKK.Ults
{
    // 아이스 퀸 쥐 · 렛잇고 (Rat it go) — 캐릭터 상세 기획서 2026-10-12:
    // 얼음성을 맵에 크게 소환하고 눈보라 → 맵의 생명체(고양이 · 보스 · 사람)는 전부 빙결 풀스택(꽁꽁). 눈보라가 끝나도 잠깐 더 얼어 있음.
    // 얼음성은 쥐 자리에 내려앉고(화면을 따라가지 않음) 둘레 물건에 서리 충격. 자막 c1~c4 (쥐 테이블 Ult_Caption). 소품 ice_castle (UltimateManager props, 그림 없으면 효과만)
    public class UltRatItGo : UltBase
    {
        const float LAND = 1.3f, STORM = 4.2f, EXTRA = 1.5f;      // 성 착지 · 눈보라 길이 · 끝나고 더 얼어 있는 시간
        public override float Dur => LAND + STORM + 0.4f;
        float cx, cy, freezeT;
        Rect v;                 // 성·눈보라 범위 = 쥐 중심, 크기는 확대 안 한 화면 (화면을 따라가지 않음, 2026-10-22)
        bool landed;
        UltProp castle;
        static readonly Color Ice = new(0.62f, 0.86f, 1f), Snow = new(0.94f, 0.98f, 1f);

        public override void Begin()
        {
            var v0 = ViewRect(); float z0 = M.Cam ? Mathf.Max(0.01f, M.Cam.ultZoom) : 1;
            cx = R.x; cy = R.y;
            v = new Rect(cx - v0.width * z0 / 2, cy - v0.height * z0 / 2, v0.width * z0, v0.height * z0);
            castle = Prop("ice_castle", cx, cy, 600, v.width * 0.55f);
            Beat(0.1f, () => Cap("c1"));
            Beat(LAND, () => Cap("c2"));
            Beat(LAND + 1.6f, () => Cap("c3"));
            Beat(LAND + STORM - 0.4f, () => Cap("c4"));
        }

        public override void Step(float dt, float k)
        {
            R.UltPose = PoseUp(1); R.UltJit = T < LAND ? 1.5f : 0;
            if (T < LAND)
            {
                // 하늘에서 얼음성이 내려옴 · 쥐 주위로 서리
                float e = Ease(T / LAND);
                if (castle != null) { castle.x = cx; castle.y = cy; castle.z = 600 * (1 - e); castle.alpha = e; castle.Apply(); }
                if ((hitT -= dt) <= 0) { hitT = 0.06f; Fx?.Burst(R.x + Rand(-40, 40), R.y, Rand(10, 60), 2, Ice, Snow, 30, 120, 3, 6); }
                return;
            }
            if (!landed)
            {
                landed = true;
                Flash(Snow, 0.7f); Fx?.Shake(0.5f); Fx?.Hitstop(0.08f);
                float rad = Mathf.Max(v.width, v.height) * 0.6f;
                Shock(cx, cy, rad, UltD * 0.8f, Ice, 3);
                for (int i = 0; i < 3; i++) Fx?.Ring(cx, cy, rad * (0.4f + i * 0.3f), i % 2 == 0 ? Ice : Snow, 0.5f + i * 0.2f);
                SfxManager.PlayAt("superjump_slam", cx, cy);
            }
            // 눈보라: 쥐 둘레(화면 크기)에 눈발 + 맵의 생명체 꽁꽁 유지
            if ((hitT -= dt) <= 0)
            {
                hitT = 0.03f;
                for (int i = 0; i < 3; i++) Fx?.Burst(Rand(v.xMin, v.xMax), Rand(v.yMin, v.yMax), Rand(20, 160), 1, Snow, Ice, 80, 260, 3, 7);
            }
            Fx?.Shake(0.02f);
            if ((freezeT -= dt) <= 0) { freezeT = 0.5f; FreezeAll(LAND + STORM - T + EXTRA); }
        }

        // 맵 전체 생명체 꽁꽁 (풀스택)
        void FreezeAll(float t)
        {
            string pop = RatMgr.freezePopup;
            var cat = RatMgr.Cats ? RatMgr.Cats.Current : null;
            if (cat && cat.Alive) cat.frost.FreezeNow(t, cat.x, cat.y, 60, pop);
            var boss = NKK.Hazards.Boss.Current;
            if (boss && boss.State == NKK.Hazards.Boss.BState.Fight) boss.frost.FreezeNow(t, boss.x, boss.y, boss.Height * 0.6f, pop);
            foreach (var h in ItemMgr.Humans) if (h && h.Alive) h.frost.FreezeNow(t, h.x, h.y, 50, pop);
        }

        public override void Finish()
        {
            Fx?.Burst(cx, cy, 200, 30, Snow, Ice, 200, 600, 4, 10);
        }
    }
}
