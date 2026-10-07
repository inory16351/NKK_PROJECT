using System.Collections.Generic;
using NKK.Rats;
using NKK.Stage;
using UnityEngine;

namespace NKK.Ults
{
    // 록스타 쥐 · 모시 피트 (웹게임 mosh): 록 무대(스피커·앰프·드럼·조명 트러스) 등장 → 관객 떼창 점프(착지마다 쿵) · 헤드뱅잉
    //   → 기타 박살(불기둥) → 스테이지 다이빙 → 크라우드 서핑 → 관객 기절 / c1~c6 자막, c7~c14 음표·환호·박살 팝업
    public class UltMosh : UltBase
    {
        public override float Dur => 6.2f;
        const float SOLO = 4.2f, PER = 0.4f, SW = 320, DECK = 0.66f;
        class Audience { public Rat rat; public Vector2 start, seat; public float ph; }
        readonly List<Audience> crowd = new();
        UltProp stage, amp, drums, mic, truss, spot, guitar, broken;
        readonly UltProp[] speakers = new UltProp[2], pyros = new UltProp[2];
        readonly float[] pyroT = { -9, -9 };
        float dz, stageH, shakeT, bx, bz, bvx, bvz, brot;
        int beat = -1, dirY;
        bool smashed, landed, fainted;
        static readonly Color Gold = new(1, 0.95f, 0.75f), Fire = new(0.98f, 0.62f, 0.25f), Wood = new(0.55f, 0.38f, 0.25f);
        static readonly Color[] Lights = { new(0.91f, 0.47f, 0.42f, 0.25f), new(0.66f, 0.83f, 0.86f, 0.25f), new(0.94f, 0.78f, 0.47f, 0.25f), new(0.91f, 0.47f, 0.42f, 0.25f) };
        static readonly float[] LampX = { -0.38f, -0.15f, 0.08f, 0.31f };   // 트러스 그림 속 조명 위치 (폭 비율)
        static readonly string[] Notes = { "c7", "c8", "c9" };
        static readonly string[] Cheers = { "c10", "c11", "c12", "c13" };

        // 그림 높이 (폭 × 세로/가로)
        static float H(UltProp p) => p != null && p.r && p.r.sprite ? p.w * p.r.sprite.bounds.size.y / p.r.sprite.bounds.size.x : 0;
        // 바닥(또는 무대 위 base)에 세움 (그림 가운데 기준이라 높이 절반만큼 올림)
        static void Stand(UltProp p, float baseZ) { if (p != null) p.z = baseZ + H(p) * p.flat * 0.5f; }

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1")); Beat(0.9f, () => Cap("c2")); Beat(2.5f, () => Cap("c3"));
            Beat(4.2f, () => Cap("c4")); Beat(4.8f, () => Cap("c5")); Beat(5.7f, () => Cap("c6"));
            dirY = M.Stage.Open.Contains(StageManager.RoomOf(X0, Y0 + 220)) ? 1 : -1;
            foreach (var rat in RatMgr.Rats)
            {
                if (rat == R || !rat || rat.UltOn || !OnScreen(rat.x, rat.y, -20)) continue;
                if (crowd.Count == 27) break;
                if (!GrabRat(rat)) continue;
                int row = crowd.Count / 9, col = crowd.Count % 9;
                float x = X0 + (col - 4) * 58 + Rand(-10, 10) + row % 2 * 26, y = Y0 + dirY * (130 + row * 55), vx = 0, vy = 0;
                M.Stage.Confine(ref x, ref y, ref vx, ref vy, rat.Radius + 6, X0, Y0, 0);
                crowd.Add(new Audience { rat = rat, start = new Vector2(rat.x, rat.y), seat = new Vector2(x, y), ph = Rand(0, 6) });
            }
            // 무대 세트 (UltProps/rock_*)
            stage = Prop("rock_stage", X0, Y0 - 2, 0, SW);
            if (stage != null) stage.sortBias = -2;
            stageH = H(stage); dz = stageH > 0 ? stageH * DECK : 60; Stand(stage, 0);
            for (int i = 0; i < 2; i++)
            {
                int side = i * 2 - 1;
                speakers[i] = Prop("rock_speaker", X0 + side * 210, Y0 - 6, 0, 100);
                if (speakers[i] != null) speakers[i].flip = side > 0;
                Stand(speakers[i], 0);
                pyros[i] = Prop("rock_pyro", X0 + side * 140, Y0 + 2, 0, 46);
                if (pyros[i] != null) { pyros[i].visible = false; pyros[i].sortBias = 4; }
            }
            amp = Prop("rock_amp", X0 - 100, Y0 - 8, 0, 62); Stand(amp, dz);
            drums = Prop("rock_drums", X0 + 92, Y0 - 10, 0, 100); Stand(drums, dz);
            mic = Prop("rock_mic", X0 + 42, Y0 + 3, 0, 24); Stand(mic, dz);
            if (mic != null) mic.sortBias = 2;
            truss = Prop("rock_truss", X0, Y0 - 12, 0, 380); Stand(truss, dz + 210);
            spot = Prop("ult_spotlight", X0, Y0 + 1, 0, 120);
            if (spot != null) { spot.alpha = 0; spot.sortBias = 20; }
            guitar = Prop("guitar", X0 + 15, Y0, dz + 26, 65);
            if (guitar != null) guitar.sortBias = 30;
            // 등장: 무대가 쿵
            Fx?.Dust(X0, Y0, 14, 1.6f); Fx?.Ring(X0, Y0, 220, Gold, 0.4f); Fx?.Shake(0.2f);
        }

        float LampTopX(int i) => X0 + LampX[i] * (truss != null ? truss.w : 380);
        float LampTopZ => truss != null ? truss.z - H(truss) * 0.25f : dz + 200;

        public override void Step(float dt, float k)
        {
            float phase = Mathf.Max(0, T - 0.7f) / PER, pulse = Mathf.Abs(Mathf.Sin(phase * Mathf.PI));
            bool show = T < SOLO + 0.8f;
            // ── 관객 ──
            foreach (var member in crowd)
            {
                var rat = member.rat; if (!rat) continue;
                if (T < 0.8f)
                {
                    rat.face = X0 > rat.x ? 1 : -1;
                    var p = Vector2.Lerp(member.start, member.seat, Ease(T / 0.8f));
                    MoveRat(rat, p.x - rat.x, p.y - rat.y);
                    rat.UltPose = P(front: Mathf.Sin(T * 30), farFront: -Mathf.Sin(T * 30), back: -Mathf.Sin(T * 30), farBack: Mathf.Sin(T * 30));
                }
                else if (fainted)
                {
                    rat.z = 0; rat.UltRot = Mathf.PI * 0.5f * -rat.face;
                    rat.UltPose = P(head: 0.4f, front: 0.3f, farFront: -0.2f, back: 0.4f, farBack: -0.3f, tail: -0.2f);
                }
                else
                {
                    rat.face = X0 > rat.x ? 1 : -1;
                    rat.x = member.seat.x; rat.y = member.seat.y;
                    // 다이빙 쥐가 머리 위로 지나가면 떠받침
                    float under = T > SOLO + 0.9f ? Mathf.Clamp01(1 - Dist(rat.x, rat.y, R.x, R.y) / 90) : 0;
                    rat.z = show ? pulse * 34 : under * 22;
                    float bang = Mathf.Sin(phase * Mathf.PI * 2 + member.ph * 0.3f);    // 헤드뱅잉 (박자에 맞춰)
                    rat.UltPose = show
                        ? P(tilt: -0.5f, front: 2.6f, farFront: 2.4f, back: -0.3f, farBack: 0.3f, head: -0.1f + bang * 0.55f, tail: 1.2f)
                        : P(tilt: -0.6f, front: 2.8f + Mathf.Sin(T * 12 + member.ph) * 0.2f, farFront: 2.8f, head: -0.4f, tail: 1.3f);
                }
            }
            // ── 무대 세트 ──
            float sp = 1 + pulse * 0.1f;
            for (int i = 0; i < 2; i++) if (speakers[i] != null) { speakers[i].w = 100 * sp; Stand(speakers[i], 0); }
            if (drums != null) drums.rot = Mathf.Sin(phase * Mathf.PI * 2) * 0.03f;
            float jit = (shakeT -= dt) > 0 ? Rand(-1, 1) * 6 * shakeT / 0.4f : 0;
            if (stage != null) stage.x = X0 + jit;
            for (int i = 0; i < 2; i++) StepPyro(i, dt);
            if (spot != null)
            {
                spot.alpha = show ? Mathf.Min(0.4f, T * 0.8f) * (0.8f + 0.2f * pulse) : Mathf.Max(0, spot.alpha - dt);
                spot.x = R.x; spot.y = R.y + 1; Stand(spot, T < SOLO ? dz - 10 : R.z - 10);
            }
            // 조명: 트러스 조명에서 흔들리는 색 빛줄기
            if ((hitT -= dt) <= 0 && show)
            {
                hitT = 0.08f;
                for (int i = 0; i < 4; i++)
                {
                    float x = X0 + Mathf.Sin(T * (1.6f + i * 0.5f) + i) * 160;
                    Fx?.Beam(LampTopX(i), Y0 - 12, LampTopZ, x, Y0 + dirY * 140, 0, Lights[i], 0.12f);
                }
            }
            if (broken != null) StepBroken(dt);

            if (T < SOLO)
            {
                // 기타 솔로: 0.7초부터 박자, 2.5초 뒤엔 팔 돌리기(윈드밀) + 더 격한 헤드뱅잉
                R.x = X0; R.y = Y0; R.face = 1; R.z = dz;
                bool wild = T > 2.5f;
                float head = wild ? Mathf.Sin(phase * Mathf.PI * 2) * 0.75f : Mathf.Sin(T * 15) * 0.55f;
                float front = wild ? Mathf.Repeat(T * 16, Mathf.PI * 2) - Mathf.PI : 1.1f + Mathf.Sin(T * 42) * 0.3f;
                R.UltPose = P(tilt: -0.45f + (wild ? pulse * 0.2f : 0), front: front, farFront: 1.9f, back: -0.35f, farBack: 0.35f,
                    head: head, tail: 1.2f + Mathf.Sin(T * 8) * 0.2f, bob: -pulse * 4);
                if (guitar != null) { guitar.rot = -0.4f + Mathf.Sin(T * 42) * 0.12f; guitar.z = dz + 26 + pulse * 4; }
                int index = Mathf.FloorToInt(phase);
                if (T > 0.7f && index != beat) OnBeat(index);
                return;
            }
            if (T < SOLO + 0.4f)
            {
                // 기타 치켜들었다가 무대에 쾅
                R.UltPose = T < SOLO + 0.2f ? P(tilt: -0.6f, front: 2.9f, farFront: 2.7f, head: -0.5f, tail: 1.3f)
                    : P(tilt: 0.35f, front: 0.2f, farFront: 0.1f, head: 0.5f, tail: 1.2f, bob: 4);
                if (guitar != null) { guitar.z = dz + 80; guitar.rot = 1.2f; guitar.x = X0 + 5; }
            }
            if (T >= SOLO + 0.2f && !smashed) Smash();
            if (T < SOLO + 0.4f) return;
            // ── 스테이지 다이빙 → 크라우드 서핑 ──
            Vector2 center = Vector2.zero; int alive = 0;
            foreach (var member in crowd) if (member.rat) { center += member.seat; alive++; }
            center = alive > 0 ? center / alive : new Vector2(X0, Y0 + dirY * 150);
            float e = Mathf.Clamp01((T - SOLO - 0.4f) / 0.5f);
            if (e < 1)
            {
                var p = Vector2.Lerp(new Vector2(X0, Y0), center, e); MoveRat(R, p.x - R.x, p.y - R.y);
                R.z = Mathf.Lerp(dz, 46, e) + Mathf.Sin(e * Mathf.PI) * 170; R.UltRot = e * Mathf.PI * 2 * R.face;
                R.UltPose = P(front: 2.8f, farFront: 2.6f, back: -1.8f, farBack: -1.6f, head: -0.3f, tail: 1);
            }
            else
            {
                if (!landed) Land(center);
                MoveRat(R, center.x + Mathf.Sin((T - SOLO - 0.9f) * 3) * 120 - R.x, center.y - R.y);
                R.UltRot = -Mathf.PI * 0.5f * R.face; R.z = 46 + Mathf.Sin(T * 9) * 6;
                R.UltPose = P(front: 2.6f, farFront: 2.4f, back: -1.5f, farBack: -1.3f, head: -0.2f, tail: 1 + Mathf.Sin(T * 10) * 0.3f);
                if (!fainted && T >= 5.7f) Faint();
            }
        }

        // 박자 = 관객 착지 → 바닥이 쿵. 4박마다 크게 (불기둥)
        void OnBeat(int index)
        {
            beat = index;
            bool big = index % 4 == 3;
            Vector2 c = Vector2.zero; int n = 0;
            foreach (var member in crowd) if (member.rat)
            {
                var rat = member.rat; ItemMgr.Aoe(rat.x, rat.y, 64, UltD * 0.06f, R, false);
                if (OnScreen(rat.x, rat.y) && Random.value < (big ? 0.6f : 0.25f)) Fx?.Dust(rat.x, rat.y, 2, 0.8f);
                c += new Vector2(rat.x, rat.y); n++;
            }
            if (n > 0) { c /= n; Fx?.Ring(c.x, c.y, big ? 300 : 180, new Color(0.95f, 0.86f, 0.75f, 0.6f), 0.3f); }
            for (int i = 0; i < 2; i++) Fx?.Ring(X0 + (i * 2 - 1) * 210, Y0, big ? 130 : 90, Gold, 0.35f);
            if (guitar != null) Fx?.Stars(guitar.x + 10, Y0, guitar.z, big ? 10 : 4, Color.white, Gold, 80, 220);
            PopupCap(Pick(Notes), X0 + (Random.value < 0.5f ? -210 : 210), Y0, Gold, 30, 0.8f, 150);
            if (crowd.Count > 0 && Random.value < 0.6f)
            {
                var rat = Pick(crowd).rat;
                if (rat) PopupCap(Pick(Cheers), rat.x, rat.y, Color.white, 16, 0.7f, 50);
            }
            if (big) { for (int i = 0; i < 2; i++) pyroT[i] = T; Fx?.Shake(0.18f); shakeT = 0.25f; Flash(Fire, 0.12f); }
            else Fx?.Shake(0.08f);
        }

        // 불기둥: 0.55초 동안 솟았다 꺼짐
        void StepPyro(int i, float dt)
        {
            var p = pyros[i]; if (p == null) return;
            float t = (T - pyroT[i]) / 0.55f;
            p.visible = t >= 0 && t < 1;
            if (!p.visible) return;
            p.flat = Mathf.Sin(Mathf.Min(1, t * 1.6f) * Mathf.PI * 0.5f) * (1 - Mathf.Max(0, t - 0.6f) / 0.4f) * (0.9f + Random.value * 0.2f);
            p.alpha = 1 - Mathf.Max(0, t - 0.7f) / 0.3f;
            Stand(p, dz - 4);
            if (Random.value < 0.5f) Fx?.Stars(p.x, p.y, p.z + H(p) * p.flat * 0.4f, 2, Gold, Fire, 60, 200);
        }

        void Smash()
        {
            smashed = true;
            if (guitar != null) { bx = guitar.x; bz = guitar.z; KillProp(guitar); guitar = null; } else { bx = X0; bz = dz + 30; }
            // 박살 난 기타가 튕겨 나감
            broken = Prop("rock_guitar_broken", bx, Y0 + 4, bz, 70);
            if (broken != null) broken.sortBias = 30;
            bvx = R.face * Rand(220, 320); bvz = 520;
            Shock(X0, Y0, 280, UltD * 0.6f, new Color(0.94f, 0.78f, 0.47f), 2.6f);
            foreach (var it in ItemsIn(X0, Y0, 260)) FlingItem(it, Mathf.Atan2(it.y - Y0, it.x - X0), 380, 520);
            Fx?.Burst(X0, Y0, dz + 10, 24, new Color(0.79f, 0.31f, 0.29f), Gold, 200, 560, 4, 9);
            Fx?.Burst(X0, Y0, dz + 10, 16, Wood, new Color(0.95f, 0.93f, 0.89f), 150, 420, 3, 7);
            Fx?.Anim("explosion", X0, Y0, dz, 1.3f);
            for (int i = 0; i < 2; i++) pyroT[i] = T;
            PopupCap("c14", X0, Y0, Gold, 30, 0.9f, 160); Flash(Color.white, 0.35f); Fx?.Shake(0.4f); Fx?.Hitstop(0.05f); shakeT = 0.4f;
        }

        void StepBroken(float dt)
        {
            if (bz <= 8 && bvz <= 0) { broken.z = 8; broken.rot = 1.5f; return; }
            bvz -= 1800 * dt; bx += bvx * dt; bz = Mathf.Max(8, bz + bvz * dt); brot += dt * 14 * Mathf.Sign(bvx);
            broken.x = bx; broken.z = bz; broken.rot = brot;
            if (bz <= 8) { Fx?.Dust(bx, Y0 + 4, 4, 0.8f); Fx?.Stars(bx, Y0 + 4, 10, 6, Wood, Gold, 80, 200); }
        }

        // 다이빙 착지: 관객 위로 철퍼덕 → 관객들이 받아서 출렁
        void Land(Vector2 c)
        {
            landed = true;
            Shock(c.x, c.y, 180, UltD * 0.3f, Gold, 1.6f);
            Fx?.Stars(c.x, c.y, 60, 14, Color.white, Gold, 120, 320);
            foreach (var member in crowd) if (member.rat && Random.value < 0.4f) PopupCap(Pick(Cheers), member.rat.x, member.rat.y, Color.white, 16, 0.7f, 60);
        }

        // 관객 전원 기절 (발라당)
        void Faint()
        {
            fainted = true;
            foreach (var member in crowd) if (member.rat && OnScreen(member.rat.x, member.rat.y)) Fx?.Stars(member.rat.x, member.rat.y, 30, 3, Color.white, Gold, 40, 120);
            Fx?.Shake(0.15f);
        }

        public override void Finish()
        {
            R.z = 0; R.UltRot = 0;
            foreach (var member in crowd) if (member.rat) { member.rat.z = 0; member.rat.UltRot = 0; }
            Fx?.Dust(X0, Y0, 12, 1.4f);
        }

        public override void Cleanup()
        {
            if (R) R.UltRot = 0;
            foreach (var member in crowd) if (member.rat) member.rat.UltRot = 0;
        }
    }
}
