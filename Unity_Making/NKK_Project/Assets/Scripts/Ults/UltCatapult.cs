using System.Collections.Generic;
using UnityEngine;

namespace NKK.Ults
{
    // 바이킹 쥐 · 발할라 직행편 (웹게임 catapult): 투석기에 자기를 장전해서 발사 → 불붙은 바위처럼 날아가 물건 많은 쪽에 쾅 (세 번, 갈수록 크게)
    //   발사마다 함성 · 불꽃 꼬리 · 착지 표시 → 착지 = 운석 크레이터 · 파편 · 연기 · 충격파 세 겹 · 화면 흔들림. 마지막은 승리 포즈
    // 자막: c1 장전 완료 · c2 (안전장치 없음) · c3 한 번 더 · c4 = 착지 팝업 (발할라!!) · c5~c7 = 발사 함성 팝업 (1·2·3번째)
    public class UltCatapult : UltBase
    {
        const float C = 1.3f, CAT_W = 130;
        public override float Dur => 4.5f;
        static readonly Color GOLD = new(1f, 0.953f, 0.749f), FIRE = new(1f, 0.54f, 0.24f), FIRE_Y = new(1f, 0.82f, 0.4f), EMBER = new(0.89f, 0.34f, 0.18f),
            WOOD = new(0.55f, 0.42f, 0.31f), ROCK = new(0.55f, 0.45f, 0.38f);

        class Bit { public UltProp p; public float x, y, z, vx, vy, vz, vr, life, max, w0, w1; public bool fly; }
        readonly List<Bit> bits = new();

        int cyc = -1, catFace = 1;
        float catX, catY, fromX, fromY, toX, toY, kick, lastSx, lastSy;
        bool landed, fired, cheered;
        UltProp cat, target, flame;

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(1.4f, () => Cap("c2"));
            Beat(2.7f, () => Cap("c3"));
            cat = Prop("catapult", R.x, R.y, 0, CAT_W);
            target = Prop("meteor_target", R.x, R.y, 0, 100); if (target != null) { target.ground = true; target.visible = false; target.sortBias = 5; }
            flame = Prop("meteor_flame", R.x, R.y, 0, 40); if (flame != null) { flame.visible = false; flame.sortBias = -1; }
        }

        void PlaceCat(float dt)
        {
            if (cat == null) return;
            float h = cat.r && cat.r.sprite ? CAT_W * cat.r.sprite.bounds.size.y / cat.r.sprite.bounds.size.x : CAT_W;
            cat.flat = 1 - 0.25f * kick;                     // 발사 반동 (찌그러졌다 복귀)
            cat.rot = Mathf.Sin(T * 40) * 0.12f * kick * catFace;
            cat.x = catX; cat.y = catY; cat.z = h * cat.flat / 2; cat.flip = catFace < 0; cat.sortBias = -2;
        }

        public override void Step(float dt, float k)
        {
            kick = Mathf.Max(0, kick - dt * 3);
            PlaceCat(dt);
            StepBits(dt);
            float sc = R.GradeData.size;
            int idx = Mathf.FloorToInt(T / C);
            float ck = (T % C) / C;
            if (idx > 2)
            {
                // 착지 후: 잠깐 철푸덕 → 벌떡 일어나 승리 포즈
                R.z = 0; R.UltRot = 0; R.UltJit = 0;
                float w = T - 3 * C;
                R.UltPose = w < 0.35f ? P(sy: 0.45f, sx: 1.5f, front: -1.4f, farFront: -1.4f, back: 1.4f, farBack: 1.4f, head: 0.2f, tail: -0.3f) : PoseUp(Mathf.Min(1, (w - 0.35f) * 6));
                if (w >= 0.35f && !cheered)
                {
                    cheered = true;
                    if (OnScreen(R.x, R.y)) { Fx?.Stars(R.x, R.y, 40 * sc, 14, GOLD, FIRE_Y, 160, 420); Fx?.Ring(R.x, R.y, 90, GOLD, 0.35f); }
                }
                return;
            }
            if (idx != cyc)
            {
                cyc = idx;
                // 투석기는 쥐 뒤에, 목표는 물건이 많은 쪽 멀리 (갈수록 멀리)
                float a = AimMost(ULT_R + 200), d = Rand(240, 360) + cyc * 50;
                catX = R.x - Mathf.Cos(a) * 30; catY = R.y - Mathf.Sin(a) * 20; catFace = Mathf.Cos(a) >= 0 ? 1 : -1;
                float x = R.x + Mathf.Cos(a) * d, y = R.y + Mathf.Sin(a) * d * 0.7f, vx = 0, vy = 0;
                M.Stage.Confine(ref x, ref y, ref vx, ref vy, 20, R.x, R.y, 0);
                fromX = catX; fromY = catY; toX = x; toY = y; landed = false;
                if (idx > 0 && OnScreen(catX, catY))
                {
                    // 투석기 재배치: 연기 펑
                    Smoke(catX, catY); Fx?.Ring(catX, catY, 70, Color.white, 0.25f); Fx?.Anim("poof", catX, catY, 10, 1.2f);
                }
                PlaceCat(dt);
            }
            if (ck < 0.3f)
            {
                // 장전: 투석기 위에 웅크림 (점점 세게 부들부들) + 착지 표시가 깜빡이며 나타남
                float e = ck / 0.3f;
                R.x = catX; R.y = catY; R.z = 22; R.face = catFace; R.UltRot = 0;
                R.UltPose = P(sy: 0.7f - 0.1f * e, sx: 1.2f + 0.1f * e, front: 0.6f, back: -0.6f, head: 0.4f, tail: -0.5f);
                R.UltJit = e * (2 + cyc);
                ShowTarget(e * 0.6f, 1.3f - 0.2f * e);
                if (Random.value < e * 0.3f) Fx?.Dust(catX, catY, 1, 0.6f);
                return;
            }
            R.UltJit = 0;
            if (ck < 0.78f)
            {
                float e = (ck - 0.3f) / 0.48f, peak = 300 + cyc * 70;
                if (!fired)
                {
                    fired = true; kick = 1; lastSx = R.x; lastSy = 0;
                    if (OnScreen(catX, catY))
                    {
                        Fx?.Dust(catX, catY, 8, 1.4f);
                        Fx?.Burst(catX, catY, 30, 10, WOOD, GOLD, 120, 320, 4, 8);                 // 나무 부스러기
                        Fx?.Burst(catX, catY, 30, 10, Color.white, Col, 150, 380);
                        Fx?.Ring(catX, catY, 90 + cyc * 20, Color.white, 0.3f);
                        Fx?.Shake(0.12f + cyc * 0.04f);
                        PopupCap("c" + (5 + cyc), catX, catY, GOLD, 26 + cyc * 4, 0.9f, 90 * sc);  // 바이킹 함성
                    }
                }
                R.x = Mathf.Lerp(fromX, toX, e); R.y = Mathf.Lerp(fromY, toY, e); R.z = 22 + Mathf.Sin(e * Mathf.PI) * peak;
                R.UltRot = -e * Mathf.PI * 4 * catFace;          // 앞으로 공중제비
                R.UltPose = PoseFlail();
                Flight(dt, sc, e);
                ShowTarget(0.6f + 0.4f * e, 1.1f - 0.35f * e + 0.08f * Mathf.Sin(T * 30));
                return;
            }
            fired = false;
            if (!landed) { landed = true; Land(sc); }
            R.UltPose = P(sy: 0.45f, sx: 1.5f, front: -1.4f, farFront: -1.4f, back: 1.4f, farBack: 1.4f, head: 0.2f, tail: -0.3f);   // 철푸덕
        }

        // 착지 표시 (빨간 과녁)
        void ShowTarget(float a, float s)
        {
            if (target == null) return;
            target.visible = true; target.x = toX; target.y = toY; target.z = 0;
            target.w = (90 + cyc * 20) * s; target.alpha = a * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(T * 12)));
        }

        // 날아가는 중: 불붙은 바위처럼 불꽃 꼬리 + 불똥 + 연기
        void Flight(float dt, float sc, float e)
        {
            float cz = R.z + 14 * sc;
            // 화면 속도 방향 (x 오른쪽, 위 +) → 불꽃은 반대쪽으로 뻗음
            float sy = R.z - R.y * World.TILT, vx = (R.x - lastSx) / Mathf.Max(dt, 1e-4f), vy = (sy - lastSy) / Mathf.Max(dt, 1e-4f);
            if (e > 0.02f && flame != null && (vx * vx + vy * vy) > 1)
            {
                float th = Mathf.Atan2(vy, vx), len = (55 + cyc * 14) * sc;
                flame.visible = true; flame.w = len * 0.5f * (0.9f + 0.2f * Random.value);
                flame.rot = th + Mathf.PI / 2;                    // 그림은 불끝이 위 → 진행 반대쪽으로
                flame.x = R.x - Mathf.Cos(th) * len * 0.4f; flame.y = R.y; flame.z = cz - Mathf.Sin(th) * len * 0.4f;
                flame.alpha = Mathf.Min(1, e * 8);
            }
            lastSx = R.x; lastSy = sy;
            if (!OnScreen(R.x, R.y)) return;
            Fx?.Burst(R.x, R.y, cz, 2, FIRE, FIRE_Y, 20, 90, 4, 8);
            if (Random.value < 0.5f) Fx?.Stars(R.x, R.y, cz, 1, FIRE_Y, EMBER, 30, 120);
            if (Random.value < 0.35f) Fx?.Burst(R.x, R.y, cz, 1, new Color(0.4f, 0.37f, 0.36f), new Color(0.6f, 0.58f, 0.56f), 10, 40, 8, 12);   // 연기
            if (e > 0.8f) Fx?.Shake(0.01f + cyc * 0.005f);        // 떨어지는 굉음
        }

        // 쾅: 갈수록 크게 (세 번째가 최대)
        void Land(float sc)
        {
            float x = R.x, y = R.y, pw = 1 + cyc * 0.4f;
            R.z = 0; R.UltRot = 0;
            if (flame != null) flame.visible = false;
            if (target != null) target.visible = false;
            Shock(x, y, 200 + cyc * 30, UltD * 0.6f, Col, 2.4f + cyc * 0.6f);
            foreach (var it in ItemsIn(x, y, 170 + cyc * 20)) FlingItem(it, Mathf.Atan2(it.y - y, it.x - x), 420 + cyc * 60, 520 + cyc * 60);
            foreach (var o in RatsNear(x, y, 170 + cyc * 20)) Ragdoll(o, Mathf.Atan2(o.y - y, o.x - x), 420, 380);
            Fx?.Spill(x, y, 50 + cyc * 15, new Color(0.235f, 0.196f, 0.176f, 0.3f));
            if (!OnScreen(x, y, 300)) return;

            // 땅 자국: 크레이터 + 갈라짐 (끝날 때까지 남음)
            var cr = Prop("meteor_crater", x, y, 0, (130 + cyc * 35) * sc); if (cr != null) { cr.ground = true; cr.sortBias = 1; }
            var ck = Prop("ground_crack", x, y, 0, (110 + cyc * 40) * sc); if (ck != null) { ck.ground = true; ck.rot = Rand(-0.4f, 0.4f); ck.alpha = 0.8f; ck.sortBias = 2; }
            // 폭발 섬광 (부풀며 사라짐) · 연기 뭉게 · 파편
            Grow("meteor_burst", x, y, 30, 50, (200 + cyc * 60) * sc, 0.28f, 8);
            for (int i = 0; i < 4 + cyc * 2; i++)
            {
                float a = Rand(0, Mathf.PI * 2), r = Rand(30, 70);
                Grow("meteor_smoke", x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r * 0.6f, Rand(10, 30), 40, Rand(90, 140) * pw, Rand(0.6f, 0.9f), 0);
            }
            for (int i = 0; i < 6 + cyc * 3; i++)
            {
                float a = Rand(0, Mathf.PI * 2), s = Rand(180, 420) * pw;
                var p = Prop("meteor_debris", x, y, 10, Rand(12, 24) * sc); if (p == null) break;
                bits.Add(new Bit { p = p, x = x, y = y, z = 10, vx = Mathf.Cos(a) * s, vy = Mathf.Sin(a) * s * 0.7f, vz = Rand(350, 650), vr = Rand(-14, 14), life = 1.2f, max = 1.2f, fly = true });
            }
            // 충격파 세 겹 + 불꽃 + 별
            Fx?.Ring(x, y, 120 * pw, Color.white, 0.3f); Fx?.Ring(x, y, 260 * pw, FIRE_Y, 0.45f); Fx?.Ring(x, y, 380 * pw, FIRE, 0.6f);
            Fx?.Burst(x, y, 10, 14 + cyc * 6, FIRE, FIRE_Y, 180, 500, 5, 10);
            Fx?.Burst(x, y, 10, 10 + cyc * 4, ROCK, WOOD, 160, 420, 4, 8);
            Fx?.Stars(x, y, 20, 10 + cyc * 4, GOLD, Color.white, 200, 520);
            Fx?.Dust(x, y, 10 + cyc * 4, 1.4f + cyc * 0.3f);
            Fx?.Anim("explosion", x, y, 10, 1.3f + cyc * 0.4f);
            Fx?.Anim("hit", x, y, 10, 1.6f);
            PopupCap("c4", x, y, GOLD, 26 + cyc * 8, 0.9f + cyc * 0.2f, 70 * sc);
            Flash(FIRE_Y, 0.12f + cyc * 0.08f);
            Fx?.Shake(0.35f + cyc * 0.15f); Fx?.Hitstop(0.05f + cyc * 0.025f);
            // 여진: 잠깐 뒤 고리가 한 번 더 (마지막은 두 번)
            Beat(T + 0.12f, () => { Fx?.Ring(x, y, 320 * pw, Col, 0.4f); Fx?.Dust(x, y, 6, 1.2f); Fx?.Shake(0.1f); });
            if (cyc == 2) Beat(T + 0.28f, () => { Fx?.Ring(x, y, 520, GOLD, 0.5f); Fx?.Burst(x, y, 10, 16, FIRE, GOLD, 200, 560, 5, 10); Fx?.Shake(0.2f); });
        }

        // 부풀며 사라지는 그림 (폭발 섬광·연기)
        void Grow(string name, float x, float y, float z, float w0, float w1, float life, int bias)
        {
            var p = Prop(name, x, y, z, w0); if (p == null) return;
            p.sortBias = bias; p.rot = Rand(-0.3f, 0.3f);
            bits.Add(new Bit { p = p, x = x, y = y, z = z, vz = name == "meteor_smoke" ? 40 : 0, life = life, max = life, w0 = w0, w1 = w1 });
        }

        void StepBits(float dt)
        {
            for (int i = bits.Count - 1; i >= 0; i--)
            {
                var b = bits[i];
                b.life -= dt;
                if (b.fly)
                {
                    // 파편: 포물선 → 바닥에 떨어지면 굴러 멈춤
                    b.vz -= World.GZ * dt; b.x += b.vx * dt; b.y += b.vy * dt; b.z += b.vz * dt;
                    if (b.z <= 0) { b.z = 0; b.vz = Mathf.Abs(b.vz) > 150 ? -b.vz * 0.3f : 0; b.vx *= 0.6f; b.vy *= 0.6f; b.vr *= 0.5f; }
                    b.p.rot += b.vr * dt;
                    b.p.alpha = Mathf.Min(1, b.life * 3);
                }
                else
                {
                    float e = 1 - b.life / b.max;
                    b.z += b.vz * dt;
                    b.p.w = Mathf.Lerp(b.w0, b.w1, Ease(e)); b.p.alpha = 1 - e * e;
                }
                b.p.x = b.x; b.p.y = b.y; b.p.z = b.z + (b.fly ? 4 : 0);
                if (b.life <= 0) { KillProp(b.p); bits.RemoveAt(i); }
            }
        }

        public override void Finish() { R.UltRot = 0; R.UltJit = 0; R.z = 0; }
        public override void Cleanup() { bits.Clear(); R.UltRot = 0; R.UltJit = 0; }
    }
}
