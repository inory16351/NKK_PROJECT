using System.Collections.Generic;
using NKK.Rats;
using NKK.Stage;
using UnityEngine;

namespace NKK.Ults
{
    // 산타 생쥐 · (웹게임 flyby, 탈것 sleigh): 썰매 저공비행으로 열린 방들을 이리저리 누비며 선물 폭탄 투하 → 3초에 솟구쳤다 급강하 → 막판에 제자리로 귀환
    //   썰매 앞엔 술 취한 쥐돌프 셋 (갈색쥐 리그 + 빨간 코·사슴뿔 머리띠·방울 목걸이, 가운데 녀석은 술병): 비틀비틀 갈지자, 딸꾹하면 휘청 + 방울·어질어질
    // 자막 c1~c3, c4 = 끝 '(귀환)' 팝업, c5·c6 = 쥐돌프 딸꾹 팝업
    public class UltFlyBy : UltBase
    {
        // 쥐돌프 (썰매 끄는 순록 쥐). 리그는 쥐 프리팹을 복제해 그림만 씀 (게임 쥐 아님)
        class Deer { public Rat r; public float x, y, z, hic, wob, dizzy; public int n; public UltProp rein, glow, nose, cheek, antler, bells, bottle, swirl; }
        const int DEER_N = 8; const float DEER_LEN = 30, DEER_0 = 84, DEER_GAP = 38;
        readonly List<Deer> deer = new();
        readonly List<(UltProp p, float life, float vx)> bubbles = new();
        static readonly Color NoseRed = new(1f, 0.22f, 0.18f), Rosy = new(1f, 0.5f, 0.58f), Rein = new(0.55f, 0.42f, 0.31f), Pink = new(1f, 0.82f, 0.86f);
        const float SLEIGH_W = 110, SPD = 640, TURN = 3.2f, FLY_Z = 70, HOME = 1.3f;
        public override float Dur => 6;
        float x, y, h, z, pz, face = 1, wpT, swoop;
        Vector2 wp;
        UltProp sleigh; Sprite gift;
        static readonly Color Red = new(0.85f, 0.47f, 0.42f), Gold = new(0.89f, 0.77f, 0.42f);

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(3.05f, () => Cap("c2"));
            Beat(5.2f, () => Cap("c3"));
            x = R.x; y = R.y; h = AimMost(); face = Mathf.Cos(h) >= 0 ? 1 : -1;
            Smoke(R.x, R.y); Fx?.Anim("poof", R.x, R.y, 0, 1.2f);
            sleigh = Prop("sleigh", R.x, R.y, 0, SLEIGH_W);
            if (sleigh != null) sleigh.sortBias = -1;            // 쥐 뒤 (쥐가 썰매에 올라탐)
            var g = Prop("gift", R.x, R.y, 0, 1);
            if (g != null) { gift = g.r ? g.r.sprite : null; KillProp(g); }
            NextWaypoint();
            MakeDeer();
        }

        // 다음 목적지: 지금 방 또는 붙어 있는 열린 방 안의 아무 곳 (멀리 있는 곳 우선)
        void NextWaypoint()
        {
            wpT = Rand(1.4f, 2.2f);
            if (T > Dur - HOME) { wp = new Vector2(X0, Y0); return; }
            var c = StageManager.RoomOf(x, y);
            Vector2 best = new(X0, Y0); float bd = -1;
            for (int n = 0; n < 8; n++)
            {
                var d = StageManager.Dirs[Random.Range(0, 4)];
                var room = Random.value < 0.6f && M.Stage.Open.Contains(c + d) ? c + d : c;
                var p = new Vector2((room.x + Rand(0.15f, 0.85f)) * World.RW, (room.y + Rand(0.2f, 0.8f)) * World.RH);
                float dd = Dist(p.x, p.y, x, y);
                if (dd > 380 && dd < 1100) { best = p; break; }
                if (dd > bd) { bd = dd; best = p; }
            }
            wp = best;
        }

        public override void Step(float dt, float k)
        {
            // 목적지 쪽으로 핸들을 꺾으며 + 좌우로 갈지자 (썰매 곡예)
            if ((wpT -= dt) <= 0 || Dist(wp.x, wp.y, x, y) < 120) NextWaypoint();
            if (T > Dur - HOME && wp != new Vector2(X0, Y0)) wp = new Vector2(X0, Y0);
            float want = Mathf.Atan2(wp.y - y, wp.x - x), turn = Mathf.DeltaAngle(h * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float home = Mathf.Clamp01((T - (Dur - HOME)) / HOME), dHome = Dist(X0, Y0, x, y);
            h += Mathf.Clamp(turn, -TURN * dt, TURN * dt) * (1 + home * 2) + Mathf.Sin(T * 6) * 1.3f * dt * (1 - home);
            float spd = SPD * (home > 0 ? Mathf.Clamp(dHome / 300, 0.35f, 1) : 1);
            float px = x, py = y, vx = Mathf.Cos(h) * spd, vy = Mathf.Sin(h) * spd;
            x += vx * dt; y += vy * dt;
            if (M.Stage.Confine(ref x, ref y, ref vx, ref vy, 60, px, py, 1)) { h = Mathf.Atan2(vy, vx); NextWaypoint(); Fx?.Stars(x, y, z + 30, 6, Color.white, Gold, 80, 200); }   // 벽 직전에 홱 꺾음
            float cx = Mathf.Cos(h);
            if (Mathf.Abs(cx) > 0.3f) face = cx > 0 ? 1 : -1;
            // 3초: 한 바퀴 더 → 하늘로 솟구쳤다가 급강하
            float sw = Mathf.Clamp01((T - 2.8f) / 0.8f);
            swoop = sw > 0 && sw < 1 ? Mathf.Sin(sw * Mathf.PI) * 230 : 0;
            float lift = Mathf.Min(1, T / 0.4f) * (1 - Mathf.Clamp01((T - (Dur - 0.35f)) / 0.35f));   // 이륙 · 착륙
            pz = z; z = (FLY_Z + Mathf.Sin(Time.time * 5) * 8) * lift + swoop;
            float pitch = Mathf.Clamp((z - pz) / Mathf.Max(dt, 0.001f) / 900, -0.45f, 0.45f) * face, bank = Mathf.Clamp(turn, -1, 1) * 0.12f;
            R.x = x; R.y = y; R.z = z + 12; R.face = (int)face; R.UltRot = pitch * 0.6f;
            R.UltPose = P(front: 1.2f, farFront: 1, back: 1.3f, farBack: 1.1f, head: -0.2f + Mathf.Sin(Time.time * 9) * 0.1f, tail: 1 + Mathf.Sin(Time.time * 12) * 0.3f, bob: 0);
            if (sleigh != null) { sleigh.x = x; sleigh.y = y; sleigh.z = z + SLEIGH_W * 0.31f; sleigh.flip = face < 0; sleigh.rot = pitch + bank + Mathf.Sin(Time.time * 5) * 0.05f; }
            StepDeer(dt, pitch);
            if (OnScreen(x, y) && Random.value < 0.5f) Fx?.Stars(x - Mathf.Cos(h) * 50, y - Mathf.Sin(h) * 50, z + 20, 1, Color.white, Gold, 40, 120);   // 썰매 반짝이 꼬리
            if (sw > 0.45f && sw < 0.6f && OnScreen(x, y)) { Fx?.Ring(x, y, 120, Gold, 0.3f); Fx?.Shake(0.05f); }   // 급강하 순간
            if ((hitT -= dt) > 0 || lift < 0.5f) return;
            hitT = swoop > 120 ? 0.05f : 0.09f;                  // 솟구칠 땐 융단 폭격
            // 선물 폭탄: 진행 방향으로 살짝 날아가며 떨어짐, 좌우로 흩뿌림
            const float fall = 0.37f;
            float side = Rand(-140, 140);
            ItemMgr.ThrowBomb(R, x + vx * fall * 0.3f - Mathf.Sin(h) * side, y + vy * fall * 0.3f + Mathf.Cos(h) * side, 85, UltD * 0.35f, z, 80, fall, gift);
        }

        // ── 쥐돌프 ──
        void MakeDeer()
        {
            var pf = RatMgr ? RatMgr.ratPrefab : null;
            var art = RatMgr && RatMgr.artLibrary ? RatMgr.artLibrary.Get("brownrat") : null;
            if (!pf || art == null) return;
            for (int i = 0; i < DEER_N; i++)
            {
                var r = Object.Instantiate(pf, M.propRoot ? M.propRoot : M.transform);
                r.enabled = false; r.name = "UltDeer";
                if (r.shadow) r.shadow.enabled = false;
                r.rig.Build(art, DEER_LEN);
                var d = new Deer { r = r, n = i, x = x + face * (DEER_0 + i / 2 * DEER_GAP), y = y, z = 0, hic = Rand(0.7f, 1.6f) + i * 0.4f };
                d.rein = Prop("ult_gauge_bar", x, y, 0, 10); if (d.rein != null) d.rein.tint = Rein;
                d.glow = Prop("dot", x, y, 0, 10); if (d.glow != null) d.glow.tint = NoseRed;
                d.cheek = Prop("dot", x, y, 0, 10); if (d.cheek != null) { d.cheek.tint = Rosy; d.cheek.flat = 0.7f; }
                d.antler = Prop("deer_antlers", x, y, 0, 10);
                d.bells = Prop("deer_bells", x, y, 0, 10);
                d.nose = Prop("deer_nose", x, y, 0, 10);
                if (i == 1) d.bottle = Prop("deer_bottle", x, y, 0, 10);       // 가운데 녀석은 술병을 못 놓음
                deer.Add(d);
                Fx?.Anim("poof", d.x, d.y, 10, 0.8f);
            }
        }

        void StepDeer(float dt, float pitch)
        {
            float t = Time.time, lift = Mathf.Min(1, T / 0.4f);
            foreach (var d in deer)
            {
                if (!d.r) continue;
                int i = d.n / 2, side = d.n % 2 == 0 ? -1 : 1;       // 2마리씩 4줄
                // 썰매 앞 (진행 방향 쪽으로 살짝 끌고 감) + 술 취한 갈지자. 뒤따라오며 출렁 (급강하 때 채찍처럼)
                float lead = DEER_0 + i * DEER_GAP;
                float tx = x + face * lead * Mathf.Max(0.6f, Mathf.Abs(Mathf.Cos(h))) + Mathf.Sin(t * 3.3f + i) * 6;
                float ty = y + Mathf.Sin(h) * lead * 0.7f + side * 14 + Mathf.Sin(t * 2.6f + d.n * 1.9f) * 10;
                float tz = z + 8 + Mathf.Abs(Mathf.Sin(t * 11 + i * 1.3f)) * 7 * lift - 10 * d.wob;
                float kk = Mathf.Min(1, dt * (9 - i * 1.5f));
                d.x += (tx - d.x) * kk; d.y += (ty - d.y) * kk; d.z += (tz - d.z) * Mathf.Min(1, dt * (8 - i * 1.5f));
                // 딸꾹질: 휘청 + 비눗방울 + 어질어질
                d.wob = Mathf.Max(0, d.wob - dt * 2.2f); d.dizzy = Mathf.Max(0, d.dizzy - dt);
                if ((d.hic -= dt) <= 0 && lift >= 1)
                {
                    d.hic = Rand(1.1f, 2.4f); d.wob = 1; d.dizzy = 1.1f;
                    if (OnScreen(d.x, d.y))
                    {
                        if (Random.value < 0.6f) PopupCap(Random.value < 0.6f ? "c5" : "c6", d.x, d.y, Pink, 16, 0.8f, d.z + 40);
                        for (int b = 0; b < 3; b++) Bubble(d);
                    }
                }
                // 자세: 엉망진창 갤럽 (다리 박자 제각각, 고개는 축 늘어져 흔들). 딸꾹하면 버둥
                float ph = t * 16 + i * 1.7f;
                var pose = d.wob > 0.4f ? PoseFlail() : P(front: Mathf.Sin(ph) * 1.2f, farFront: Mathf.Sin(ph + 0.9f) * 1.1f, back: -Mathf.Sin(ph) * 1.1f, farBack: -Mathf.Sin(ph + 1.3f),
                    head: 0.2f + Mathf.Sin(t * 2.2f + i * 1.3f) * 0.35f, tail: 0.6f + Mathf.Sin(t * 7 + i) * 0.5f, tilt: Mathf.Sin(t * 4.1f + i) * 0.12f, bob: -Mathf.Abs(Mathf.Cos(ph)) * 2);
                float rot = pitch * 0.5f + Mathf.Sin(t * 3.7f + i * 2) * 0.12f + Mathf.Sin(d.wob * 18) * 0.3f * d.wob;
                d.r.transform.position = World.ToUnity(d.x, d.y, d.z);
                int so = World.SortOrder(d.y) + 1;
                d.r.rig.Apply(pose, 1, (int)face, 1 - d.wob * 0.15f, so, 0, rot);
                Dress(d, so, t);
            }
            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                var b = bubbles[i]; b.life -= dt;
                if (b.life <= 0 || b.p == null) { KillProp(b.p); bubbles.RemoveAt(i); continue; }
                b.p.x += b.vx * dt; b.p.z += 45 * dt; b.p.w += 8 * dt; b.p.alpha = Mathf.Min(1, b.life / 0.3f) * 0.85f;
                bubbles[i] = b;
            }
        }

        // 파츠 그림 위 비율 좌표(왼쪽 위 0,0) → 게임 좌표 (바닥 y 기준, 높이 z)
        static Vector3 PartPt(SpriteRenderer sr, float fx, float fy, float gy)
        {
            var s = sr.sprite;
            var u = sr.transform.TransformPoint(new Vector3((fx * s.rect.width - s.pivot.x) / s.pixelsPerUnit, ((1 - fy) * s.rect.height - s.pivot.y) / s.pixelsPerUnit, 0));
            return new Vector3(u.x / World.U, gy, u.y / World.U + gy * World.TILT);
        }
        static void At(UltProp p, Vector3 g, float w, int so, int k)
        {
            if (p == null) return;
            p.x = g.x; p.y = g.y; p.z = g.z; p.w = w; p.sortBias = so + k - World.SortOrder(g.y);
        }

        // 빨간 코(번쩍번쩍) · 발그레 볼 · 사슴뿔 머리띠 · 방울 목걸이 · 술병 · 고삐
        void Dress(Deer d, int so, float t)
        {
            var hd = d.r.rig.head; if (!hd || !hd.sprite) return;
            float hw = hd.sprite.rect.width / hd.sprite.pixelsPerUnit * Mathf.Abs(hd.transform.lossyScale.x) / World.U;
            bool fl = face < 0;
            var nose = PartPt(hd, 0.15f, 0.5f, d.y);
            float pulse = Mathf.Sin(t * 9 + d.n * 2);
            At(d.glow, nose, hw * (0.6f + pulse * 0.12f), so, 1); if (d.glow != null) d.glow.alpha = 0.45f + pulse * 0.2f;
            At(d.nose, nose, hw * 0.2f, so, 3);
            At(d.cheek, PartPt(hd, 0.4f, 0.64f, d.y), hw * 0.26f, so, 2); if (d.cheek != null) d.cheek.alpha = 0.75f;
            var top = PartPt(hd, 0.62f, 0.2f, d.y);
            if (d.antler != null)
            {
                float aw = hw * 0.62f; top.z += aw * 1.19f * 0.26f;           // 머리띠 고리가 정수리에 걸치게
                At(d.antler, top, aw, so, 2); d.antler.flip = fl; d.antler.rot = (fl ? 1 : -1) * 0.15f + Mathf.Sin(t * 5 + d.n) * 0.08f;
            }
            var neck = PartPt(hd, 0.85f, 0.85f, d.y);
            At(d.bells, neck, hw * 0.55f, so, 2); if (d.bells != null) { d.bells.flip = fl; d.bells.rot = Mathf.Sin(t * 14 + d.n) * 0.15f; }
            if (d.bottle != null && d.r.rig.front && d.r.rig.front.sprite)
            {
                At(d.bottle, PartPt(d.r.rig.front, 0.5f, 0.92f, d.y), hw * 0.55f, so, 4);
                d.bottle.flip = fl; d.bottle.rot = Mathf.Sin(t * 16 + 1.7f) * 0.5f;
            }
            // 어질어질 (딸꾹 뒤): 머리 위 소용돌이
            if (d.dizzy > 0)
            {
                d.swirl ??= Prop("swirl", d.x, d.y, 0, 10);
                if (d.swirl != null)
                {
                    var sp = top; sp.z += hw * 0.6f;
                    d.swirl.visible = true; At(d.swirl, sp, hw * 0.7f, so, 5);
                    d.swirl.rot = -t * 9; d.swirl.alpha = Mathf.Min(1, d.dizzy * 2) * 0.8f; d.swirl.tint = new Color(1f, 0.95f, 0.7f);
                }
            }
            else if (d.swirl != null) d.swirl.visible = false;
            // 고삐: 썰매 앞머리 → 목 (썰매 뒤로)
            if (d.rein != null)
            {
                float sx = x + face * 46, sz = z + 40, sy = y;
                float dsx = neck.x - sx, dsy = (neck.z - neck.y * World.TILT) - (sz - sy * World.TILT), len = Mathf.Max(1, Mathf.Sqrt(dsx * dsx + dsy * dsy));
                var r = d.rein; r.x = (sx + neck.x) / 2; r.y = (sy + neck.y) / 2; r.z = (sz + neck.z) / 2;
                r.w = len; r.flat = 2.5f / Mathf.Max(0.01f, len * Aspect(r)); r.rot = Mathf.Atan2(dsy, dsx);
                r.sortBias = World.SortOrder(y) - 2 - World.SortOrder(r.y);
            }
        }

        // 딸꾹 비눗방울 (코에서 퐁퐁)
        void Bubble(Deer d)
        {
            var hd = d.r.rig.head; if (!hd || !hd.sprite) return;
            var g = PartPt(hd, 0.1f, 0.45f, d.y);
            var p = Prop("ring", g.x + face * Rand(0, 6), g.y, g.z + Rand(-2, 6), Rand(5, 10)); if (p == null) return;
            p.tint = Pink; p.sortBias = World.SortOrder(d.y) + 6 - World.SortOrder(p.y);
            bubbles.Add((p, Rand(0.6f, 1f), face * Rand(10, 40) + Rand(-15, 15)));
        }
        static float Aspect(UltProp p) => p != null && p.r && p.r.sprite ? p.r.sprite.bounds.size.y / Mathf.Max(1e-5f, p.r.sprite.bounds.size.x) : 1;

        void KillDeer()
        {
            foreach (var d in deer) if (d.r) { if (OnScreen(d.x, d.y)) Fx?.Anim("poof", d.x, d.y, d.z, 0.8f); Object.Destroy(d.r.gameObject); }
            deer.Clear();
        }

        public override void Finish()
        {
            KillDeer();
            KillProp(sleigh); sleigh = null;
            if (Dist(R.x, R.y, X0, Y0) > 60) { Smoke(R.x, R.y); R.x = X0; R.y = Y0; }
            R.z = 0; R.UltRot = 0;
            Smoke(R.x, R.y); Fx?.Anim("poof", R.x, R.y, 0, 1.2f); Fx?.Ring(R.x, R.y, 90, Red, 0.3f);
            PopupCap("c4", R.x, R.y, Color.white, 18, 1, 40);
        }

        public override void Cleanup()
        {
            foreach (var d in deer) if (d.r) Object.Destroy(d.r.gameObject);
            deer.Clear(); bubbles.Clear();
            if (R) R.UltRot = 0;
        }
    }
}
