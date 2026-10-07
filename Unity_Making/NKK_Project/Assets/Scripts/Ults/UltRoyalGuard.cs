using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 여왕 쥐 · 여왕 폐하의 근위대 (웹게임 royalguard): 열 맞춘 행진과 마차 행차 → 받들어 총 충격파.
    // 자막 c1 집합, c2 구령, c3~c5 행차, c6 받들어 총, c7 강제 정렬.
    public class UltRoyalGuard : UltBase
    {
        public override float Dur => 6;
        const int COUNT = 10;
        const float MARCH_START = 0.45f, STOP = 4.65f, SALUTE = 5.05f, BEAT = 0.3f;
        static readonly Color Gold = new(1, 0.95f, 0.75f);
        class Guard { public UltProp a, b; public float x, y; public bool fired; }
        readonly List<Guard> guards = new();
        readonly HashSet<Item> shoved = new();
        UltProp carriage;
        float startX, endX, direction, spacing;
        int lastBeat = -1;

        public override void Begin()
        {
            Cap("c1"); Beat(0.6f, () => Cap("c2")); Beat(1.6f, () => Cap("c3"));
            Beat(2.45f, () => Cap("c4")); Beat(3.4f, () => Cap("c5"));
            Beat(STOP, () => Cap("c6")); Beat(5.65f, () => Cap("c7"));
            var view = ViewRect();
            direction = R.face >= 0 ? 1 : -1;
            spacing = Mathf.Clamp(view.width * 0.055f, 42, 65);
            startX = direction > 0 ? view.xMin - 80 : view.xMax + 80;
            endX = direction > 0 ? view.xMax - 100 : view.xMin + 100;
            for (int i = 0; i < COUNT; i++)
            {
                var g = new Guard { x = startX - direction * (i / 2) * spacing, y = Y0 + (i % 2 == 0 ? -38 : 38) };
                g.a = Prop("guard_rat_a", g.x, g.y, 0, 64);
                g.b = Prop("guard_rat_b", g.x, g.y, 0, 64);
                if (g.a != null) g.a.visible = false;
                if (g.b != null) g.b.visible = false;
                guards.Add(g);
            }
            carriage = Prop("royal_carriage", R.x, R.y, 0, 170);
            if (carriage != null) carriage.flip = direction > 0;
        }

        public override void Step(float dt, float k)
        {
            // 행렬이 화면을 가로지르는 동안 카메라는 출발 지점을 유지.
            if (M.Cam) M.Cam.ultFocus = new Vector2(X0, Y0);
            float e = Mathf.Clamp01((T - MARCH_START) / (STOP - MARCH_START));
            float front = Mathf.Lerp(startX, endX, e);
            bool marching = T >= MARCH_START && T < STOP;
            int beat = Mathf.FloorToInt(Mathf.Max(0, T - MARCH_START) / BEAT);
            bool stomp = marching && beat != lastBeat;
            if (stomp) { lastBeat = beat; Fx?.Shake(0.025f); }
            float bounce = marching ? Mathf.Sin(Mathf.Repeat(T - MARCH_START, BEAT) / BEAT * Mathf.PI) * 4 : 0;
            for (int i = 0; i < guards.Count; i++)
            {
                var g = guards[i]; g.x = front - direction * (i / 2) * spacing;
                bool frameA = !marching || beat % 2 == 0;
                DrawGuard(g.a, g, frameA, bounce); DrawGuard(g.b, g, !frameA, bounce);
                if (stomp && OnScreen(g.x, g.y, 0)) Fx?.Dust(g.x, g.y, 2, 0.6f);
                if (marching) PushAhead(g, dt);
                if (!g.fired && T >= SALUTE + (i / 2) * 0.075f)
                {
                    g.fired = true;
                    // 충격파 피해를 먼저 적용해야 바닥 물건이 공중 상태로 바뀌어 피해를 피하지 않음.
                    var targets = ItemsIn(g.x + direction * 60, g.y, 100);
                    Shock(g.x, g.y, 155, UltD * 0.1f, Gold, 0.65f);
                    foreach (var it in targets)
                    {
                        if (!it || (it.State != Item.ItemState.Rest && it.State != Item.ItemState.Fly)) continue;
                        it.SkillHit(0, R); it.Fling(direction * 620, 0, 260);
                    }
                    Fx?.Stars(g.x + direction * 25, g.y, 58, 8, Gold, Col, 160, 360);
                    if (i == 0) { Flash(Gold, 0.3f); Fx?.Shake(0.2f); }
                }
            }
            // 실제 쥐는 열린 방 안에서만 이동하고, 마차는 항상 쥐의 발밑을 따라감.
            float carriageX = front - direction * (spacing * 4 + 115);
            MoveRat(R, carriageX - R.x, Y0 - R.y); R.face = direction > 0 ? 1 : -1;
            R.UltLift = 47 + bounce * 0.35f;
            R.UltPose = P(tilt: -0.7f, front: 2.2f + Mathf.Sin(T * 5) * 0.22f, farFront: 0.5f,
                back: 0.6f, farBack: 0.6f, head: -0.15f, tail: 0.6f);
            if (carriage != null)
            {
                carriage.x = R.x; carriage.y = R.y; carriage.z = GroundLift(carriage) + bounce * 0.35f;
                carriage.sortBias = -5; carriage.rot = marching ? Mathf.Sin(T * 6) * 0.012f : 0;
                carriage.alpha = Mathf.Clamp01((Dur - T) / 0.25f);
            }
        }

        void PushAhead(Guard g, float dt)
        {
            foreach (var it in ItemsIn(g.x + direction * 48, g.y, 90))
            {
                float ahead = (it.x - g.x) * direction;
                if (!it.Appeared || ahead < -it.R || ahead > 105 + it.R || Mathf.Abs(it.y - g.y) > 40 + it.R) continue;
                it.AddPush(direction * 1800 * dt, (g.y - it.y) * dt * 8);
                if (ahead < 32 + it.R && shoved.Add(it))
                {
                    FlingItem(it, direction > 0 ? 0 : Mathf.PI, 370, 140);
                    Fx?.Dust(it.x, it.y, 3, 0.8f);
                }
            }
        }

        void DrawGuard(UltProp p, Guard g, bool visible, float bounce)
        {
            if (p == null) return;
            p.x = g.x; p.y = g.y; p.flip = direction > 0; p.visible = visible && T >= MARCH_START;
            p.z = GroundLift(p) + bounce;
            p.rot = T >= STOP ? -direction * 0.08f * Mathf.Sin(Mathf.Clamp01((T - STOP) / 0.25f) * Mathf.PI) : 0;
            p.alpha = Mathf.Clamp01((Dur - T) / 0.25f);
        }

        static float GroundLift(UltProp p) => p.r && p.r.sprite
            ? -p.r.sprite.bounds.min.y * p.w / Mathf.Max(0.001f, p.r.sprite.bounds.size.x) : 0;

        public override void Cleanup()
        {
            foreach (var p in new List<UltProp>(Props)) KillProp(p);
            guards.Clear(); shoved.Clear(); carriage = null;
            if (R) { R.UltLift = 0; R.UltJit = 0; }
        }
    }
}
