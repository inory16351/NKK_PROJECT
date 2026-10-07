using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 찍찍 탐정 · 축구공 킥: 킥력 증강 운동화로 뻥! → 축구공 5개가 사방으로 흩어져 핀볼 난장판
    //   벽·물건·공끼리 튕길 때마다 범퍼처럼 번쩍 (고리·별·흔들림), 맞은 물건은 날아가고 공중 물건은 저글링. 단서 보너스 = 공 추가 + 난장판 시간 연장
    // 자막: c1 준비 · c2 킥 · c3 {n}번째 쿠션(5연타마다) · c4 단서 보너스({n}) · c5 끝 · c6 마무리 · c7 연타 팝업({n}) · c8 공끼리 쾅 · c9 총 연타({n})
    public class UltSoccerKick : UltBase
    {
        const float BALL_R = 22, SPEED = 2900, WIND = 0.6f, GRAV = 2600, TRAIL = 4;
        float dur = 6, chaosEnd;
        public override float Dur => dur;

        class Ball
        {
            public UltProp p; public readonly UltProp[] ghost = new UltProp[(int)TRAIL];
            public float x, y, z, vx, vy, vz, spin, actorCD;
            public readonly List<Vector3> hist = new();
            public readonly Dictionary<Item, float> cd = new();
        }
        readonly List<Ball> balls = new();
        int nBalls, combo, nextMilestone = 5; bool kicked, ending, ended;
        float kx, ky;

        public override void Begin()
        {
            Beat(0.05f, () => Cap("c1"));
            int extra = R.ClueBounces(R.clues);
            // 단서 보너스: 2개마다 공 +1 (최대 +3), 나머지는 난장판 시간 연장
            nBalls = 5 + Mathf.Min(3, extra / 2);
            chaosEnd = WIND + 3.9f + Mathf.Min(0.8f, extra * 0.15f);
            dur = chaosEnd + 1.2f;
            if (extra > 0) Beat(0.35f, () => Cap("c4", extra));
            kx = R.x + R.face * 26; ky = R.y + 4;
            // 킥 전엔 공 하나만 발 앞에
            var b = new Ball { x = kx, y = ky };
            b.p = Prop("soccer_ball", kx, ky, 0, BALL_R * 2.2f);
            balls.Add(b);
        }

        // 가장 튼튼한 물건 쪽 (첫 공 조준용)
        float AimStrongest()
        {
            Item best = null;
            foreach (var it in ItemsIn(kx, ky, ULT_R * 1.3f)) if (!best || it.hp > best.hp) best = it;
            return best ? Mathf.Atan2(best.y - ky, best.x - kx) : Rand(0, Mathf.PI * 2);
        }

        void Kick()
        {
            kicked = true; Cap("c2");
            R.UltPose = P(tilt: 0.2f, front: 2.2f, farFront: 0.3f, back: -0.6f, head: -0.3f, tail: 1.2f);
            float a0 = AimStrongest();
            R.face = Mathf.Cos(a0) >= 0 ? 1 : -1;
            for (int i = 0; i < nBalls; i++)
            {
                var b = i < balls.Count ? balls[i] : new Ball { x = kx, y = ky };
                if (b.p == null) { b.p = Prop("soccer_ball", kx, ky, 0, BALL_R * 2.2f); balls.Add(b); }
                for (int g = 0; g < TRAIL; g++)
                {
                    b.ghost[g] = Prop("soccer_ball", kx, ky, 0, BALL_R * 2.2f * (1 - g * 0.12f));
                    if (b.ghost[g] != null) { b.ghost[g].alpha = 0; b.ghost[g].sortBias = -2 - g; b.ghost[g].tint = Color.Lerp(Color.white, Col, 0.3f + g * 0.15f); }
                }
                // 사방으로 고르게 + 흔들림, 첫 공은 가장 튼튼한 물건으로
                float a = a0 + i * Mathf.PI * 2 / nBalls + (i == 0 ? 0 : Rand(-0.25f, 0.25f));
                float sp = SPEED * Rand(0.9f, 1.1f);
                b.vx = Mathf.Cos(a) * sp; b.vy = Mathf.Sin(a) * sp; b.vz = Rand(250, 520);
            }
            Flash(Color.white, 0.35f);
            Fx?.Burst(kx, ky, 10, 22, Color.white, Col, 250, 620); Fx?.Stars(kx, ky, 20, 14, Color.white, Col, 200, 520);
            Fx?.Ring(kx, ky, 90, Color.white, 0.3f); Fx?.Ring(kx, ky, 160, Col, 0.45f);
            Fx?.Anim("explosion", kx, ky, 0, 0.6f);
            Fx?.Shake(0.25f); Fx?.Hitstop(0.08f);
        }

        // 범퍼 번쩍: 튕길 때마다
        void Bumper(Ball b, float x, float y, float z, float power = 1)
        {
            combo++;
            if (combo >= nextMilestone) { nextMilestone += 5; Cap("c3", combo); Flash(Col, 0.12f); }
            if (!OnScreen(x, y)) return;
            PopupCap("c7", x, y, Color.Lerp(Color.white, Col, Rand(0, 0.6f)), 18 + Mathf.Min(16, combo * 0.6f), 0.55f, z + 40, combo);
            Fx?.Ring(x, y, 40 * power, Color.white, 0.22f); Fx?.Ring(x, y, 75 * power, Col, 0.32f);
            Fx?.Stars(x, y, z + 10, Mathf.RoundToInt(6 * power), Color.white, Col, 160, 420);
            Fx?.Spark(x, y, z + 12, Color.Lerp(Col, Color.white, 0.5f), 70 * power, 0.16f);
            if (Random.value < 0.4f) Fx?.Anim("hit", x, y, z, 0.9f * power);
            Fx?.Shake(0.05f * power); Fx?.Hitstop(0.018f * power);
            b.vz = Mathf.Max(b.vz, Rand(200, 460));
        }

        // 핀볼 범퍼처럼 속도를 되살리고 방향을 살짝 흔듦
        void Kickback(Ball b, float jitter = 0.22f)
        {
            float a = Mathf.Atan2(b.vy, b.vx) + Rand(-jitter, jitter), sp = SPEED * Rand(0.95f, 1.12f);
            b.vx = Mathf.Cos(a) * sp; b.vy = Mathf.Sin(a) * sp;
        }

        // 공 → 물건 반사 (법선 기준)
        static void Reflect(Ball b, float nx, float ny)
        {
            float d = b.vx * nx + b.vy * ny;
            if (d < 0) { b.vx -= 2 * d * nx; b.vy -= 2 * d * ny; }
        }

        public override void Step(float dt, float k)
        {
            if (!kicked)
            {
                // 다리를 뒤로 뺐다가 → 뻥! (발 앞 공이 부르르)
                float e = Mathf.Clamp01(T / WIND);
                R.UltPose = P(tilt: -0.15f * e, front: -1.6f * e, farFront: 0.6f, back: 0.4f, head: -0.2f, tail: 1);
                R.UltJit = e * 2;
                var b0 = balls[0];
                if (b0.p != null) { b0.p.x = kx + Rand(-2, 2) * e; b0.p.y = ky; b0.p.z = 4; b0.p.Apply(); }
                if (T > WIND * 0.5f && Random.value < 0.3f) Fx?.Spark(kx, ky, 10, Col, 40, 0.1f);
                if (T >= WIND) { R.UltJit = 0; Kick(); }
                return;
            }

            // 탐정: 공 쪽을 가리키며 들썩 (5연타마다 주먹 불끈)
            if (T > WIND + 0.25f)
            {
                bool pump = combo > 0 && combo % 5 < 2;
                R.UltPose = pump ? PoseUp(0.8f) : P(tilt: -0.3f, front: 1.9f, farFront: 0.4f, head: Mathf.Sin(T * 9) * 0.15f, tail: 1 + Mathf.Sin(T * 7) * 0.3f, bob: -Mathf.Abs(Mathf.Sin(T * 10)) * 3);
            }

            if (!ending && T >= chaosEnd)
            {
                ending = true; Cap("c5");
                PopupCap("c9", R.x, R.y, Col, 34, 1.4f, 90, combo);
                Fx?.Shake(0.2f); Flash(Col, 0.2f);
            }

            foreach (var b in balls) StepBall(b, dt);
            if (!ending) BallVsBall();

            if (ending && !ended && T > dur - 0.45f)
            {
                ended = true; Cap("c6");
                foreach (var b in balls)
                {
                    Smoke(b.x, b.y); Fx?.Anim("poof", b.x, b.y, b.z, 0.8f);
                    KillProp(b.p); b.p = null;
                    for (int g = 0; g < TRAIL; g++) { KillProp(b.ghost[g]); b.ghost[g] = null; }
                }
            }
        }

        void StepBall(Ball b, float dt)
        {
            if (b.p == null) return;
            foreach (var key in new List<Item>(b.cd.Keys)) if ((b.cd[key] -= dt) <= 0) b.cd.Remove(key);
            b.actorCD -= dt;

            // 벽 쿠션
            float pvx = b.vx, pvy = b.vy;
            BounceMove(ref b.x, ref b.y, ref b.vx, ref b.vy, BALL_R, dt);
            bool wall = Mathf.Sign(pvx) != Mathf.Sign(b.vx) || Mathf.Sign(pvy) != Mathf.Sign(b.vy);
            if (wall && !ending) { Kickback(b); Bumper(b, b.x, b.y, b.z); Fx?.Dust(b.x, b.y, 3, 0.8f); }

            b.vz -= GRAV * dt; b.z += b.vz * dt;
            if (b.z < 0) { b.z = 0; b.vz = Mathf.Abs(b.vz) * 0.55f; if (b.vz > 150 && Random.value < 0.3f) Fx?.Dust(b.x, b.y, 2, 0.6f); }
            if (ending) { float f = Mathf.Max(0, 1 - dt * 3.2f); b.vx *= f; b.vy *= f; }
            float spd = Mathf.Sqrt(b.vx * b.vx + b.vy * b.vy);
            b.spin += spd * dt * 0.03f * (b.vx >= 0 ? -1 : 1);

            if (!ending)
            {
                // 바닥 물건: 세게 맞고 날아감 + 공은 반사
                foreach (var it in ItemsIn(b.x, b.y, BALL_R))
                {
                    if (b.cd.ContainsKey(it)) continue;
                    b.cd[it] = 0.5f;
                    float nx = b.x - it.x, ny = b.y - it.y, nd = Mathf.Max(1, Mathf.Sqrt(nx * nx + ny * ny)); nx /= nd; ny /= nd;
                    float a = Mathf.Atan2(-ny, -nx), fs = Rand(700, 1000);
                    DropItem(it, Mathf.Cos(a) * fs, Mathf.Sin(a) * fs, Rand(420, 620), UltD * 0.3f);
                    Fx?.Burst(it.x, it.y, 20, 8, Color.white, Col, 180, 460);
                    Reflect(b, nx, ny); Kickback(b, 0.35f);
                    b.x = it.x + nx * (it.R + BALL_R + 2); b.y = it.y + ny * (it.R + BALL_R + 2);
                    Bumper(b, it.x, it.y, 20, 1.3f);
                    break;
                }
                // 공중 물건: 저글링 (더 높이)
                foreach (var it in ItemMgr.InRange(b.x, b.y, BALL_R + 80))
                {
                    if (it.State != Item.ItemState.Fly || b.cd.ContainsKey(it) || Mathf.Abs(it.z - b.z) > 70) continue;
                    if (Dist(it.x, it.y, b.x, b.y) > BALL_R + it.R) continue;
                    b.cd[it] = 0.35f;
                    float nx = b.x - it.x, ny = b.y - it.y, nd = Mathf.Max(1, Mathf.Sqrt(nx * nx + ny * ny)); nx /= nd; ny /= nd;
                    it.Damage(UltD * 0.2f, R, true, Mathf.Atan2(-ny, -nx));
                    Reflect(b, nx, ny); Kickback(b, 0.4f);
                    Bumper(b, it.x, it.y, it.z, 1.1f);
                    break;
                }
                // 쥐·사람·고양이 휘말림
                if (b.actorCD <= 0)
                {
                    bool hit = false;
                    foreach (var o in RatsNear(b.x, b.y, BALL_R + 16)) { Ragdoll(o, Mathf.Atan2(b.vy, b.vx) + Rand(-0.5f, 0.5f), 520, 420); hit = true; }
                    if (BlastActors(b.x, b.y, BALL_R + 24, 620, UltD * 0.25f) > 0) hit = true;
                    if (hit) { b.actorCD = 0.3f; Fx?.Anim("hit", b.x, b.y, b.z, 1); Fx?.Shake(0.06f); }
                }
            }

            // 잔상
            b.hist.Insert(0, new Vector3(b.x, b.y, b.z));
            if (b.hist.Count > TRAIL * 2 + 1) b.hist.RemoveAt(b.hist.Count - 1);
            float fast = Mathf.Clamp01(spd / SPEED);
            for (int g = 0; g < TRAIL; g++)
            {
                var gp = b.ghost[g]; if (gp == null) continue;
                int idx = Mathf.Min(b.hist.Count - 1, (g + 1) * 2);
                var h = b.hist[idx];
                gp.x = h.x; gp.y = h.y; gp.z = h.z + 6; gp.rot = b.spin; gp.alpha = fast * (0.45f - g * 0.1f);
                gp.Apply();
            }
            b.p.x = b.x; b.p.y = b.y; b.p.z = b.z + 6; b.p.rot = b.spin; b.p.Apply();
            if (!ending && Random.value < 0.25f * fast && OnScreen(b.x, b.y)) Fx?.Stars(b.x, b.y, b.z + 8, 1, Color.white, Col, 20, 60);
        }

        // 공끼리 부딪힘: 탄성 충돌 (법선 성분 교환)
        void BallVsBall()
        {
            for (int i = 0; i < balls.Count; i++)
                for (int j = i + 1; j < balls.Count; j++)
                {
                    var a = balls[i]; var b = balls[j];
                    if (a.p == null || b.p == null || Mathf.Abs(a.z - b.z) > 40) continue;
                    float nx = b.x - a.x, ny = b.y - a.y, d = Mathf.Sqrt(nx * nx + ny * ny);
                    if (d >= BALL_R * 2 || d < 0.01f) continue;
                    nx /= d; ny /= d;
                    float va = a.vx * nx + a.vy * ny, vb = b.vx * nx + b.vy * ny;
                    if (va - vb <= 0) continue;
                    a.vx += (vb - va) * nx; a.vy += (vb - va) * ny; b.vx += (va - vb) * nx; b.vy += (va - vb) * ny;
                    float push = (BALL_R * 2 - d) * 0.5f + 1;
                    a.x -= nx * push; a.y -= ny * push; b.x += nx * push; b.y += ny * push;
                    Kickback(a, 0.3f); Kickback(b, 0.3f);
                    float mx = (a.x + b.x) * 0.5f, my = (a.y + b.y) * 0.5f, mz = (a.z + b.z) * 0.5f;
                    Bumper(a, mx, my, mz, 1.4f);
                    PopupCap("c8", mx, my, Color.white, 26, 0.6f, mz + 60);
                    Fx?.Anim("explosion", mx, my, mz, 0.35f);
                    Shock(mx, my, 70, UltD * 0.15f, Col, 0.6f);
                }
        }

        public override void Cleanup() { R.UltJit = 0; }
    }
}
