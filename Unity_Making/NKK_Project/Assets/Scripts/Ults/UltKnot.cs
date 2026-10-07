using System.Collections.Generic;
using NKK.Rats;
using UnityEngine;

namespace NKK.Ults
{
    // 쥐왕 · (웹게임 knot): 화면 속 쥐들의 꼬리가 엉켜 거대 쥐 공 (실존 현상 '쥐왕') → 통통 튀며 굴러다님, 착지마다 충격파
    // 자막 c1~c4, c5 = 끝 '풀렸다!!' 팝업
    public class UltKnot : UltBase
    {
        const int MAX = 20;
        public override float Dur => 6.3f;

        class K { public Rat o; public float kx, ky, kz, kd; public int i; }
        readonly List<K> knot = new();
        float bx, by, bz, bvx, bvy, bvz, rot, rad = 30;
        int hops; bool gatherFx;
        UltProp tails, shadow;
        static readonly Color Gold = new(0.85f, 0.64f, 0.25f), Tail = new(0.88f, 0.69f, 0.66f), Pale = new(1f, 0.95f, 0.75f);

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(1.3f, () => Cap("c2"));
            Beat(2.8f, () => Cap("c3"));
            Beat(4.6f, () => Cap("c4"));
            // 화면에 보이는 쥐 중 가까운 순으로 최대 20마리 (쥐왕 포함). 반경 제한 없음
            var cand = new List<Rat>();
            foreach (var o in RatMgr.Rats) if (o != R && !o.UltOn && OnScreen(o.x, o.y, 40)) cand.Add(o);
            cand.Sort((p, q) => Dist(p.x, p.y, R.x, R.y).CompareTo(Dist(q.x, q.y, R.x, R.y)));
            var list = new List<Rat> { R };
            foreach (var o in cand) { if (list.Count >= MAX) break; if (GrabRat(o)) list.Add(o); }
            bx = R.x; by = R.y;
            // 가까운 쥐는 살짝 늦게 출발 → 멀리서 날아오는 쥐와 비슷하게 도착
            float far = 1; foreach (var o in list) far = Mathf.Max(far, Dist(o.x, o.y, R.x, R.y));
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                knot.Add(new K { o = o, kx = o.x, ky = o.y, kz = o.z, i = i, kd = 0.25f * (1 - Dist(o.x, o.y, R.x, R.y) / far) * Random.value });
            }
            shadow = Prop("dot", bx, by, 0, 60);
            if (shadow != null) { shadow.ground = true; shadow.flat = 0.47f; shadow.tint = new Color(0.12f, 0.06f, 0.02f, 0.2f); shadow.visible = false; }
            tails = Prop("swirl", bx, by, 30, 60);
            if (tails != null) { tails.tint = Tail; tails.visible = false; }
        }

        public override void Step(float dt, float k)
        {
            int n = knot.Count;
            rad = Mathf.Min(240, 26 + 15 * Mathf.Sqrt(n));
            if (T > 0.9f)
            {
                // 통통 튀며 이동: 착지할 때마다 쿵 + 다음 방향으로 점프 (클수록 천천히 구름)
                bvz -= 2200 * dt; bz += bvz * dt; BounceMove(ref bx, ref by, ref bvx, ref bvy, rad, dt);
                rot += dt * 6 * Mathf.Min(1, 75 / rad);
                if (bz <= 0)
                {
                    bz = 0;
                    var t = ItemMgr.Nearest(bx, by, 460 + rad);
                    float a = t ? Mathf.Atan2(t.y - by, t.x - bx) : Rand(0, Mathf.PI * 2);
                    bvx = Mathf.Cos(a) * 380; bvy = Mathf.Sin(a) * 380; bvz = 820;
                    if (hops++ > 0)
                    {
                        Shock(bx, by, rad + 90, UltD * 0.25f, Gold, 1.8f);
                        Crush(bx, by, rad + 40, 0, 0);
                        Fx?.Ring(bx, by, rad + 140, Pale, 0.4f); Fx?.Burst(bx, by, 10, 10, Gold, Pale, 180, 420);
                        Fx?.Shake(0.2f + Mathf.Min(0.2f, n / 400f)); Fx?.Hitstop(0.03f);
                    }
                }
            }
            // 쥐 공: 피보나치 구 표면에 고르게 (0번 = 쥐왕, 공 앞면 가운데). 모일 땐 포물선으로 슝
            float c = Mathf.Cos(rot), sn = Mathf.Sin(rot);
            foreach (var q in knot)
            {
                var o = q.o; if (!o) continue;
                float dy = 1 - 2 * (q.i + 0.5f) / n, r0 = Mathf.Sqrt(Mathf.Max(0, 1 - dy * dy)), th = q.i * 2.39996f;
                float px = Mathf.Cos(th) * r0, pz = Mathf.Sin(th) * r0, rx = px * c - pz * sn, rz = px * sn + pz * c;
                float e = Ease(Mathf.Clamp01((T - q.kd) / 0.65f)), hop = Mathf.Sin(e * Mathf.PI) * Mathf.Min(260, 80 + Dist(q.kx, q.ky, bx, by) * 0.25f);
                o.x = Mathf.Lerp(q.kx, bx + rx * rad, e); o.y = Mathf.Lerp(q.ky, by - dy * rad * 0.45f, e); o.z = Mathf.Lerp(q.kz, bz + rad + rz * rad, e) + hop;
                o.UltRot = e > 0.6f ? -(Mathf.Atan2(rz, rx) + Mathf.PI / 2) : 0;
                o.UltPose = PoseFlail(); o.face = 1;
            }
            if (!gatherFx && T > 0.75f)
            {
                gatherFx = true;
                Fx?.Ring(bx, by, rad + 30, Gold, 0.35f); Fx?.Dust(bx, by, 8, 1.4f); Fx?.Stars(bx, by, rad, 12, Color.white, Gold, 150, 380);
                Fx?.Shake(0.25f);
            }
            // 엉킨 꼬리 덩어리 (공 가운데) + 그림자
            bool show = T >= 0.7f;
            if (tails != null) { tails.visible = show; tails.x = bx; tails.y = by; tails.z = bz + rad; tails.w = rad * 1.6f; tails.rot = -rot; tails.sortBias = -Mathf.RoundToInt(rad); }
            if (shadow != null) { shadow.visible = show; shadow.x = bx; shadow.y = by; shadow.w = rad * 2; }
        }

        public override void Finish()
        {
            foreach (var q in knot)
            {
                var o = q.o; if (!o || o == R) continue;
                float a = Mathf.Atan2(o.y - by, o.x - bx) + Rand(-0.5f, 0.5f);
                ReleaseRat(o); Ragdoll(o, a, 420, 420);
            }
            R.z = 0; R.UltRot = 0;
            if (OnScreen(bx, by))
            {
                Fx?.Ring(bx, by, 120, Color.white, 0.4f); Fx?.Ring(bx, by, 200, Gold, 0.5f);
                Fx?.Anim("poof", bx, by, 0, 1.5f); Fx?.Stars(bx, by, 40, 14, Color.white, Pale, 150, 420);
                PopupCap("c5", bx, by, Pale, 24, 1, 80);
                Fx?.Shake(0.15f);
            }
        }
    }
}
