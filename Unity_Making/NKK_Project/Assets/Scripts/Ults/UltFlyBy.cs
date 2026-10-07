using NKK.Stage;
using UnityEngine;

namespace NKK.Ults
{
    // 산타 생쥐 · (웹게임 flyby, 탈것 sleigh): 썰매 저공비행으로 열린 방들을 이리저리 누비며 선물 폭탄 투하 → 3초에 솟구쳤다 급강하 → 막판에 제자리로 귀환
    // 자막 c1~c3, c4 = 끝 '(귀환)' 팝업
    public class UltFlyBy : UltBase
    {
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
            if (OnScreen(x, y) && Random.value < 0.5f) Fx?.Stars(x - Mathf.Cos(h) * 50, y - Mathf.Sin(h) * 50, z + 20, 1, Color.white, Gold, 40, 120);   // 썰매 반짝이 꼬리
            if (sw > 0.45f && sw < 0.6f && OnScreen(x, y)) { Fx?.Ring(x, y, 120, Gold, 0.3f); Fx?.Shake(0.05f); }   // 급강하 순간
            if ((hitT -= dt) > 0 || lift < 0.5f) return;
            hitT = swoop > 120 ? 0.05f : 0.09f;                  // 솟구칠 땐 융단 폭격
            // 선물 폭탄: 진행 방향으로 살짝 날아가며 떨어짐, 좌우로 흩뿌림
            const float fall = 0.37f;
            float side = Rand(-140, 140);
            ItemMgr.ThrowBomb(R, x + vx * fall * 0.3f - Mathf.Sin(h) * side, y + vy * fall * 0.3f + Mathf.Cos(h) * side, 85, UltD * 0.35f, z, 80, fall, gift);
        }

        public override void Finish()
        {
            KillProp(sleigh); sleigh = null;
            if (Dist(R.x, R.y, X0, Y0) > 60) { Smoke(R.x, R.y); R.x = X0; R.y = Y0; }
            R.z = 0; R.UltRot = 0;
            Smoke(R.x, R.y); Fx?.Anim("poof", R.x, R.y, 0, 1.2f); Fx?.Ring(R.x, R.y, 90, Red, 0.3f);
            PopupCap("c4", R.x, R.y, Color.white, 18, 1, 40);
        }

        public override void Cleanup() { if (R) R.UltRot = 0; }
    }
}
