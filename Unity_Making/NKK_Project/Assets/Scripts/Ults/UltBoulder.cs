using NKK.Stage;
using UnityEngine;

namespace NKK.Ults
{
    // 마법사 쥐 · (웹게임 boulder): 하늘에서 거대 치즈 바퀴가 쿵 → 바퀴가 마법사를 쫓아 굴러다니고, 마법사는 열린 방을 이리저리 허둥지둥 도망 (인디아나 존스)
    // 자막 c1~c4, c5 = 끝 '치즈 대폭발!' 팝업
    public class UltBoulder : UltBase
    {
        const float BR = 78, RUN = 600, HOMING = 1.9f;
        public override float Dur => 6.2f;
        float bx, by, bz, bvx, bvy, bvz, rot, run, rh, wpT, hop, hopV, bumpCD;
        bool landed;
        Vector2 wp;
        UltProp wheel, shadow;
        static readonly Color Cheese = new(0.94f, 0.78f, 0.47f), Cheese2 = new(0.89f, 0.77f, 0.42f), Pale = new(1f, 0.95f, 0.75f);

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(0.9f, () => Cap("c2"));
            Beat(2.4f, () => Cap("c3"));
            Beat(4.3f, () => Cap("c4"));
            bx = R.x + Rand(-60, 60); by = R.y - 20; bz = 1100;
            shadow = Prop("dot", bx, by, 0, BR * 2);
            if (shadow != null) { shadow.ground = true; shadow.flat = 0.47f; shadow.tint = new Color(0.12f, 0.06f, 0.02f, 0.25f); }
            wheel = Prop("cheese_wheel", bx, by, bz, BR * 2.1f);
        }

        // 도망칠 곳: 지금 방·옆 열린 방 안에서 바퀴와 가장 먼 곳 (가끔 엉뚱한 곳 = 허둥지둥)
        void NextWaypoint()
        {
            wpT = Rand(0.7f, 1.4f);
            var c = StageManager.RoomOf(R.x, R.y);
            Vector2 best = new(R.x, R.y); float bs = -1e9f;
            bool silly = Random.value < 0.2f;
            for (int n = 0; n < 10; n++)
            {
                var d = StageManager.Dirs[Random.Range(0, 4)];
                var room = Random.value < 0.5f && M.Stage.Open.Contains(c + d) ? c + d : c;
                var p = new Vector2((room.x + Rand(0.12f, 0.88f)) * World.RW, (room.y + Rand(0.18f, 0.82f)) * World.RH);
                float dm = Dist(p.x, p.y, R.x, R.y);
                if (dm < 200 || dm > 900) continue;
                // 바퀴에서 멀고, 가는 길이 바퀴 쪽이 아닌 곳
                float toB = Vector2.Dot((p - new Vector2(R.x, R.y)).normalized, (new Vector2(bx - R.x, by - R.y)).normalized);
                float score = Dist(p.x, p.y, bx, by) - toB * 400 + (silly ? Rand(0, 800) : 0);
                if (score > bs) { bs = score; best = p; }
            }
            wp = best;
        }

        public override void Step(float dt, float k)
        {
            if (!landed)
            {
                bvz -= 2600 * dt; bz += bvz * dt;
                R.UltPose = P(head: -0.5f, front: 0.4f, tail: 1.2f);     // 멍하니 올려다봄
                if (bz <= 0)
                {
                    bz = 0; landed = true;
                    float a = Mathf.Atan2(R.y - by, R.x - bx); bvx = Mathf.Cos(a) * 420; bvy = Mathf.Sin(a) * 420;
                    Shock(bx, by, 160, UltD * 0.4f, Cheese, 2.5f);
                    Fx?.Anim("explosion", bx, by, 0, 1.6f); Fx?.Burst(bx, by, 30, 20, Cheese, Pale, 200, 560);
                    Flash(Pale, 0.2f); Fx?.Shake(0.4f); Fx?.Hitstop(0.06f);
                    rh = a; hopV = 520; NextWaypoint();                    // 깜짝 놀라 펄쩍
                }
                Sync();
                return;
            }
            // ── 바퀴: 마법사 쪽으로 핸들을 꺾으며 굴러옴 (멀어지면 빨라짐) ──
            float bsp = Mathf.Max(1, Mathf.Sqrt(bvx * bvx + bvy * bvy)), ba = Mathf.Atan2(bvy, bvx);
            float want = Mathf.Atan2(R.y - by, R.x - bx), gap = Dist(bx, by, R.x, R.y);
            ba += Mathf.Clamp(Mathf.DeltaAngle(ba * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad, -HOMING * dt, HOMING * dt);
            bsp = Mathf.MoveTowards(bsp, Mathf.Clamp(440 + (gap - 180) * 0.9f, 420, 760), 500 * dt);
            bvx = Mathf.Cos(ba) * bsp; bvy = Mathf.Sin(ba) * bsp;
            float pvx = bvx, pvy = bvy;
            BounceMove(ref bx, ref by, ref bvx, ref bvy, BR, dt);
            if (Mathf.Sign(pvx) != Mathf.Sign(bvx) || Mathf.Sign(pvy) != Mathf.Sign(bvy)) { Fx?.Ring(bx, by, BR + 40, Cheese, 0.3f); Fx?.Dust(bx, by, 6, 1.3f); Fx?.Shake(0.15f); }   // 벽에 쿵
            rot += bsp / BR * dt * (bvx >= 0 ? 1 : -1);
            Crush(bx, by, BR + 10, bvx, bvy);
            if (Random.value < 0.5f) Fx?.Dust(bx, by, 1, 1);
            if (Random.value < 0.1f) Fx?.Shake(0.03f);

            // ── 마법사: 허둥지둥 도망 (목적지 쪽으로 급히 꺾고, 바퀴가 가까우면 옆으로 홱) ──
            if ((wpT -= dt) <= 0 || Dist(wp.x, wp.y, R.x, R.y) < 70) NextWaypoint();
            float ra = Mathf.Atan2(wp.y - R.y, wp.x - R.x);
            if (gap < BR + 140)
            {
                float away = Mathf.Atan2(R.y - by, R.x - bx);
                ra = Mathf.LerpAngle(ra * Mathf.Rad2Deg, away * Mathf.Rad2Deg, 0.6f) * Mathf.Deg2Rad;
            }
            rh += Mathf.Clamp(Mathf.DeltaAngle(rh * Mathf.Rad2Deg, ra * Mathf.Rad2Deg) * Mathf.Deg2Rad, -8 * dt, 8 * dt) + Mathf.Sin(T * 13) * 2.5f * dt;   // 갈지자
            float rs = RUN * (gap < BR + 100 ? 1.15f : gap > 420 ? 0.75f : 1);        // 멀어지면 살짝 숨 고르기 (쫓기는 맛)
            float ox = R.x, oy = R.y;
            MoveRat(R, Mathf.Cos(rh) * rs * dt, Mathf.Sin(rh) * rs * dt);
            if (Dist(ox, oy, R.x, R.y) < rs * dt * 0.4f) { rh += Mathf.PI * Rand(0.5f, 0.9f) * (Random.value < 0.5f ? 1 : -1); NextWaypoint(); }   // 벽에 막히면 홱 돌아섬
            if (Mathf.Abs(Mathf.Cos(rh)) > 0.2f) R.face = Mathf.Cos(rh) >= 0 ? 1 : -1;
            // 바퀴에 엉덩이를 들이받힘 → 펄쩍
            if ((bumpCD -= dt) <= 0 && gap < BR + 26)
            {
                bumpCD = 0.8f; hopV = 480; rh = Mathf.Atan2(R.y - by, R.x - bx);
                Fx?.Stars(R.x, R.y, 30, 10, Color.white, Pale, 120, 300); Fx?.Shake(0.12f); Fx?.Hitstop(0.03f);
            }
            hopV -= 2200 * dt; hop = Mathf.Max(0, hop + hopV * dt); if (hop <= 0) hopV = 0;
            R.z = hop;
            run += dt * 700 / 9;
            float s = Mathf.Sin(run);
            var pose = PoseFlail();                                      // 팔은 허우적
            pose.back = -s * 1.25f; pose.farBack = Mathf.Sin(run + Mathf.PI + 0.6f) * 1.25f;
            pose.bob = -Mathf.Abs(s) * 3.5f; pose.head = -0.3f + Mathf.Sin(T * 20) * 0.25f; pose.tilt = hop > 0 ? -0.4f : 0.1f; pose.sx = 1.1f; pose.sy = 0.93f;
            R.UltPose = pose;
            if (Random.value < 0.3f) Fx?.Dust(R.x, R.y, 1, 0.6f);
            Sync();
        }

        void Sync()
        {
            if (wheel != null) { wheel.x = bx; wheel.y = by; wheel.z = bz + BR * 0.93f; wheel.rot = -rot; }
            if (shadow != null) { shadow.x = bx; shadow.y = by; shadow.w = BR * 2 * (1 - Mathf.Min(0.6f, bz / 1500)); }
        }

        public override void Finish()
        {
            R.z = 0;
            if (OnScreen(bx, by))
            {
                Fx?.Burst(bx, by, 40, 30, Cheese, Pale, 200, 600, 5, 10); Fx?.Burst(bx, by, 40, 12, Cheese2, Color.white, 150, 400);
                Fx?.Anim("explosion", bx, by, 0, 2); Fx?.Ring(bx, by, 140, Cheese, 0.4f); Fx?.Ring(bx, by, 220, Color.white, 0.5f);
                PopupCap("c5", bx, by, Cheese, 26, 1, 90);
                Fx?.Shake(0.35f); Fx?.Hitstop(0.05f);
            }
            ItemMgr.Aoe(bx, by, 140, UltD * 0.5f, R, false);
            KillProp(wheel); KillProp(shadow); wheel = shadow = null;
        }
    }
}
