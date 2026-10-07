using System.Collections.Generic;
using NKK.Items;
using NKK.Rats;
using UnityEngine;

namespace NKK.Ults
{
    // 외계인 쥐 · UFO 대납치 (웹게임 abduct): UFO 빛줄기가 돌아다니며 물건·쥐를 빨아올림 → 5.06초에 공중에서 반납(투하) → 본인도 딸려 올라감
    // 자막: c1 채집 개시 · c2 표본 부족 · c3 반납 · c4 나는 빼줘
    public class UltAbduct : UltBase
    {
        const float GRAB_END = 5.06f, SELF = 5.44f, UFO_Z = 250, BEAM_R = 130;
        public override float Dur => 6.2f;

        float ux, uy, uz = UFO_Z, bx, by, wa, ringT;
        bool dropped, self;
        UltProp ufo, beam, spot;
        static readonly Color green = new(0.62f, 0.84f, 0.66f);

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(2.7f, () => Cap("c2"));
            Beat(5.0f, () => Cap("c3"));
            Beat(5.5f, () => Cap("c4"));
            ux = R.x; uy = R.y - 40; bx = R.x; by = R.y; wa = Rand(0, Mathf.PI * 2);
            spot = Prop("dot", bx, by, 0, 220);
            if (spot != null) { spot.ground = true; spot.flat = 0.5f; spot.tint = new Color(green.r, green.g, green.b, 0.45f); }
            beam = Prop("dot", bx, by, 0, 170);
            if (beam != null) { beam.tint = new Color(green.r, green.g, green.b, 0.32f); beam.sortBias = -5; }
            ufo = Prop("ufo", ux, uy, uz, 150);
            if (ufo != null) ufo.sortBias = 1500;
            Fx?.Ring(bx, by, 160, green, 0.5f); Fx?.Shake(0.15f);
        }

        public override void Step(float dt, float k)
        {
            R.UltPose = P(tilt: -0.5f, front: 2.2f, farFront: 2.4f, back: -0.3f, farBack: 0.3f, head: -0.4f, tail: 1.3f);
            wa += Rand(-2, 2) * dt;
            if (T < GRAB_END)
            {
                // 빛줄기가 방 안을 어슬렁 (벽에 닿으면 반대로)
                float ox = bx, oy = by, vx = Mathf.Cos(wa), vy = Mathf.Sin(wa), vx0 = vx, vy0 = vy;
                bx += vx * 280 * dt; by += vy * 190 * dt;
                M.Stage.Confine(ref bx, ref by, ref vx, ref vy, 90, ox, oy, 1);
                if (vx != vx0 || vy != vy0) wa += Mathf.PI;
                float f = Mathf.Min(1, dt * 5); ux += (bx - ux) * f; uy += (by - 40 - uy) * f;
                foreach (var it in ItemsIn(bx, by, BEAM_R))
                    if (Items.Count < 26 && GrabItem(it)) { Fx?.Stars(it.x, it.y, 10, 4, Color.white, green, 60, 160); }
                foreach (var o in RatsNear(bx, by, BEAM_R))
                    if (Rats.Count < 8 && GrabRat(o)) { Fx?.Stars(o.x, o.y, 10, 5, Color.white, green, 60, 160); }
                if ((ringT -= dt) <= 0) { ringT = 0.25f; Fx?.Ring(bx, by, BEAM_R, new Color(green.r, green.g, green.b, 0.8f), 0.35f); Fx?.Dust(bx, by, 2, 0.8f); }
                if (Random.value < 0.5f) Fx?.Stars(bx + Rand(-60, 60), by + Rand(-30, 30), Rand(0, 120), 1, Color.white, green, 20, 60);
            }
            // 빨려 올라간 것들은 UFO 아래에서 빙글빙글
            int n = Items.Count + Rats.Count, i = 0;
            float lf = Mathf.Min(1, dt * 3);
            foreach (var it in Items)
            {
                float a = Time.time * 3 + i / (float)n * Mathf.PI * 2, r0 = 40 + (i % 3) * 20;
                it.x += (ux + Mathf.Cos(a) * r0 - it.x) * lf; it.y += (uy + 40 + Mathf.Sin(a) * r0 * 0.5f - it.y) * lf;
                it.z = Mathf.Min(200, it.z + dt * 220); it.Rot += dt * 5; i++;
            }
            foreach (var o in Rats)
            {
                float a = Time.time * 3 + i / (float)n * Mathf.PI * 2, r0 = 40 + (i % 3) * 20;
                o.x += (ux + Mathf.Cos(a) * r0 - o.x) * lf; o.y += (uy + 40 + Mathf.Sin(a) * r0 * 0.5f - o.y) * lf;
                o.z = Mathf.Min(200, Mathf.Max(0, o.z) + dt * 220); o.UltPose = PoseFlail(); o.UltRot = Time.time * 4 + i; i++;
            }
            // 반납: 공중에서 전부 투하
            if (T >= GRAB_END && !dropped)
            {
                dropped = true;
                foreach (var it in new List<Item>(Items)) DropItem(it, Rand(-300, 300), Rand(-220, 220), -50);
                foreach (var o in new List<Rat>(Rats)) { ReleaseRat(o); o.vz = 0; }
                Fx?.Burst(ux, uy + 40, 180, 16, green, Color.white, 150, 400); Fx?.Ring(bx, by, 200, green, 0.4f); Fx?.Shake(0.2f);
                Flash(green, 0.2f);
            }
            // 본인도 딸려 올라감 → 마지막엔 UFO 가 떠나 버림
            if (T > SELF)
            {
                if (!self) { self = true; Fx?.Stars(R.x, R.y, 20, 8, Color.white, green, 80, 220); }
                float f = Mathf.Min(1, dt * 4);
                R.x += (ux - R.x) * f; R.y += (uy + 40 - R.y) * f; R.z = Mathf.Min(220, R.z + dt * 500);
                R.UltPose = PoseFlail(); R.UltRot = T * 5;
            }
            if (T > 5.9f) { uz += dt * 1400; ux += dt * 300; }
            Draw();
        }

        void Draw()
        {
            float wob = Mathf.Sin(T * 5) * 6;
            if (ufo != null) { ufo.x = ux; ufo.y = uy; ufo.z = uz + wob; ufo.rot = Mathf.Sin(T * 3) * 0.08f; }
            // 빛줄기: UFO 아래 → 바닥 지점 (채집 중이거나 본인이 끌려갈 때)
            bool on = (!dropped || self) && uz < UFO_Z + 60;
            float gx = self ? R.x : bx, gy = self ? R.y : by;
            if (spot != null) { spot.visible = on; spot.x = gx; spot.y = gy; spot.w = 220 + Mathf.Sin(T * 12) * 14; }
            if (beam != null)
            {
                beam.visible = on;
                float top = uz + wob - 14, hs = (gy - uy) * World.TILT + top, dx = ux - gx;        // 바닥 → UFO 밑 (화면 높이)
                float len = Mathf.Max(20, Mathf.Sqrt(dx * dx + hs * hs));
                beam.x = (gx + ux) / 2; beam.y = gy; beam.z = hs / 2;
                beam.rot = -Mathf.Atan2(dx, hs); beam.flat = len / beam.w * 1.15f;
                beam.alpha = 0.85f + Mathf.Sin(T * 20) * 0.15f;
            }
        }
    }
}
