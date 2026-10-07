using System.Collections.Generic;
using NKK.Humans;
using UnityEngine;

namespace NKK.Ults
{
    // 찌릿 햄찌 · 과충전 (웹게임 overcharge): 건전지 충전 부들부들 → 물건·사람·고양이 사이로 연쇄 번개 (한 번에 최대 6번 튐) → 건전지 펑! 본인은 까맣게 탄 아프로
    // 번개 = FxManager.BoltLine (지지직 다시 꺾임) · Crackle (몸 둘레 튐) · Spark
    // 자막 c1~c4 · 팝업 c5~c7 사람 감전 (찌릿!/지지직!/뼈가 보임) · c8 …(지지직)
    public class UltOverCharge : UltBase
    {
        const float CHARGE = 0.8f, BOOM = 5.3f, HOP = 340;
        public override float Dur => 6.2f;
        static readonly Color YEL = new(0.95f, 0.79f, 0.3f), PALE = new(1f, 0.95f, 0.75f), SOOT = new(0.27f, 0.25f, 0.23f);
        UltProp battery, afro;
        bool boom;

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(0.85f, () => Cap("c2"));
            Beat(3f, () => Cap("c3"));
            Beat(5.35f, () => Cap("c4"));
            battery = Prop("ult_battery", R.x, R.y, 80, 22);
            if (battery != null) battery.sortBias = 40;
        }

        public override void Step(float dt, float k)
        {
            float t = T;
            if (battery != null)
            {
                battery.x = R.x - R.face * 4; battery.y = R.y; battery.z = R.z + 82 + Mathf.Sin(t * 12) * 3;
                battery.alpha = 0.55f + 0.4f * Mathf.Sin(t * 40); battery.rot = Mathf.Sin(t * 50) * 0.1f;
                battery.tint = Color.Lerp(Color.white, new Color(1, 0.6f, 0.5f), Mathf.Clamp01((t - 3) / 2.3f));   // 전압 초과 → 벌겋게
            }
            if (t < CHARGE)
            {
                // 충전 중: 점점 세게 부들부들 + 불꽃
                R.UltJit = 2 + t * 4;
                R.UltPose = P(head: -0.2f, front: 0.6f, farFront: 0.5f, tail: 1.2f, tilt: -0.1f);
                if (Random.value < 0.6f) Fx?.Burst(R.x + Rand(-20, 20), R.y, Rand(10, 40), 1, YEL, Color.white, 60, 140, 2, 4);
                // 몸 둘레 지지직 (충전될수록 잦게) + 건전지에서 몸으로 짧은 번개
                if (Random.value < 0.3f + t) Fx?.Crackle(R.x, R.y, 30, 30 + t * 20, YEL, 1, 3);
                if (Random.value < 0.25f) Fx?.BoltLine(R.x - R.face * 4, R.y, R.z + 76, R.x + Rand(-18, 18), R.y, R.z + Rand(10, 34), YEL, 0.07f, 3, 4, 8);
                return;
            }
            if (t < BOOM)
            {
                R.UltJit = 1.5f;
                var up = PoseUp(); up.front = 2.6f + Mathf.Sin(t * 30) * 0.2f; up.farFront = 2.2f; R.UltPose = up;
                Walk(dt, 70);
                if (Random.value < 0.5f) Fx?.Burst(R.x + Rand(-16, 16), R.y, Rand(20, 60), 1, YEL, Color.white, 80, 200, 2, 4);
                if (Random.value < 0.45f) Fx?.Crackle(R.x, R.y, 40, 44, YEL, 1, 3.5f);        // 온몸 지지직
                if (Random.value < 0.3f) Fx?.BoltLine(R.x - R.face * 4, R.y, R.z + 78, R.x + R.face * 10, R.y, R.z + 50, Color.white, 0.06f, 2.5f, 3, 6, A(YEL, 0.5f));   // 건전지 → 치켜든 앞발
                if ((hitT -= dt) <= 0) { hitT = 0.16f; Chain(); }
                return;
            }
            if (!boom) Boom();
            R.UltJit = 0;
            R.UltPose = P(head: 0.3f, front: 0.2f, farFront: 0.1f, tail: -0.4f, tilt: 0.1f, sy: 0.9f);
            if (afro != null) { afro.x = R.x + R.face * 10; afro.y = R.y; afro.z = R.z + 46; afro.sortBias = 30; }
            if (Random.value < 0.15f) Fx?.Burst(R.x + Rand(-10, 10), R.y, 50, 1, SOOT, Color.gray, 10, 40, 6, 10);   // 모락모락 연기
        }

        // 연쇄 번개: 가장 가까운 물건·사람으로 최대 6번 튐
        void Chain()
        {
            float cx = R.x, cy = R.y, cz = 50;
            var used = new HashSet<Object>();
            bool any = false;
            for (int hop = 0; hop < 6; hop++)
            {
                Object best = null; float bd = HOP, bx = 0, by = 0;
                foreach (var it in ItemsIn(cx, cy, HOP))
                {
                    if (used.Contains(it)) continue;
                    float d = Dist(it.x, it.y, cx, cy); if (d < bd) { bd = d; best = it; bx = it.x; by = it.y; }
                }
                foreach (var h in ItemMgr.Humans)
                {
                    if (used.Contains(h) || h.State == Human.HState.Fly || h.State == Human.HState.Dead || h.State == Human.HState.Splat) continue;
                    float d = Dist(h.x, h.y, cx, cy); if (d < bd) { bd = d; best = h; bx = h.x; by = h.y; }
                }
                if (!best) break;
                used.Add(best);
                float nz = best is Human ? 80 : 14;
                Zap(cx, cy, cz, bx, by, nz, YEL, 0.14f);
                cx = bx; cy = by; cz = nz; any = true;
                if (best is Human hu)
                {
                    hu.Damage(hu.hpMax * 0.45f, R, Rand(0, Mathf.PI * 2));
                    if (OnScreen(cx, cy)) { Fx?.Anim("zap", cx, cy, 30, 1); Fx?.Spark(cx, cy, 80, YEL, 70, 0.16f); Fx?.Crackle(cx, cy, 70, 40, YEL, 2, 3, 0.12f); if (Random.value < 0.4f) PopupCap(Pick(new[] { "c5", "c6", "c7" }), cx, cy, YEL, 18, 0.6f, 120); }
                }
                else if (best is NKK.Items.Item itm)
                {
                    itm.Damage(UltD * 0.25f, R, false, Rand(0, Mathf.PI * 2));
                    if (OnScreen(cx, cy)) { Fx?.Burst(cx, cy, 14, 3, YEL, Color.white, 80, 220, 2, 4); Fx?.Spark(cx, cy, 16, YEL, 55, 0.14f); }
                }
            }
            // 고양이도 찌릿
            var cat = ItemMgr.Cats ? ItemMgr.Cats.Current : null;
            if (cat && cat.Alive && Dist(cat.x, cat.y, R.x, R.y) < 360)
            {
                cat.Damage(UltD * 0.28f, Rand(0, Mathf.PI * 2), R);
                Zap(R.x, R.y, 50, cat.x, cat.y, 30, YEL, 0.14f); any = true;
                if (OnScreen(cat.x, cat.y)) { Fx?.Spark(cat.x, cat.y, 30, YEL, 80, 0.16f); Fx?.Crackle(cat.x, cat.y, 30, 50, YEL, 2, 3, 0.12f); }
            }
            if (any && OnScreen(R.x, R.y)) Fx?.Shake(0.04f);
            // 쥐들은 찌릿해서 폴짝 (다치진 않음)
            foreach (var o in RatsNear(R.x, R.y, 200)) if (o.z <= 0 && Random.value < 0.3f) o.vz = 260;
        }

        // 건전지 펑!
        void Boom()
        {
            boom = true; R.UltJit = 0;
            Shock(R.x, R.y, 320, UltD * 1.2f, YEL, 3);
            foreach (var it in ItemsIn(R.x, R.y, 320)) FlingItem(it, Mathf.Atan2(it.y - R.y, it.x - R.x), 560, 520);
            BlastActors(R.x, R.y, 320, 640, UltD * 2);
            foreach (var o in RatsNear(R.x, R.y, 240)) Ragdoll(o, Mathf.Atan2(o.y - R.y, o.x - R.x), 360, 320);
            Fx?.Anim("explosion", R.x, R.y, 0, 1.6f);
            Fx?.Burst(R.x, R.y, 40, 16, SOOT, new Color(0.45f, 0.42f, 0.4f), 60, 200, 10, 18);
            Fx?.Stars(R.x, R.y, 40, 20, YEL, PALE, 200, 600);
            Fx?.Ring(R.x, R.y, 200, Color.white, 0.4f); Fx?.Ring(R.x, R.y, 380, YEL, 0.6f);
            Flash(PALE, 0.6f); Fx?.Shake(0.5f); Fx?.Hitstop(0.08f);
            KillProp(battery); battery = null;
            afro = Prop("ult_afro", R.x, R.y, 46, 46);
            if (afro == null) { afro = Prop("puff", R.x, R.y, 46, 50); if (afro != null) afro.tint = SOOT; }   // 아프로 그림이 없으면 연기 뭉치를 까맣게
            PopupCap("c8", R.x, R.y, Color.white, 20, 1.2f, 90);
        }

        // 꺾인 번개 줄 (웹 zapPath 6토막·꺾임 14 + drawZap 굵기 7: 번진 빛 · 노랑 몸통 · 흰 심지), 살아 있는 동안 계속 새로 꺾임
        void Zap(float x1, float y1, float z1, float x2, float y2, float z2, Color c, float life)
        {
            Fx?.BoltLine(x1, y1, z1, x2, y2, z2, c, life, 7, 6, 14, A(c, 0.4f), 1);
        }
        static Color A(Color c, float a) { c.a *= a; return c; }
    }
}
