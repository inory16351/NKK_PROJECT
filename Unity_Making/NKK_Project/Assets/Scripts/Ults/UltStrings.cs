using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 찍찍 탐정 (셜록 홈즈 패러디) · 마인드 팰리스 (웹게임 strings): 돋보기로 단서를 훑으며 물건 사이에 빨간 실 → 실 순서대로 연쇄 폭발
    //   3.5초에 "배후는 따로 있다" 두 번째 판 (실 12개, 폭발 조금 약하게)
    // 자막: c1 컵 단서 · c2 화분 단서 · c3 전부 연결 · c4 모리아티 · c5 사건 종결
    public class UltStrings : UltBase
    {
        const float WEB = 1.4f, GAP = 0.11f, LINE_Z = 20, THICK = 7;
        public override float Dur => 6.4f;
        static readonly Color red = new(0.79f, 0.31f, 0.29f), boom = new(0.85f, 0.47f, 0.42f);

        class Pin { public Item it; public float x, y; public bool boom; public UltProp line, dot; }
        readonly List<Pin> path = new();
        float c0, wd; bool again;
        UltProp glass;

        public override void Begin()
        {
            Beat(0.2f, () => Cap("c1"));
            Beat(0.7f, () => Cap("c2"));
            Beat(1.2f, () => Cap("c3"));
            Beat(3.5f, () => Cap("c4"));
            Beat(5.9f, () => Cap("c5"));
            glass = Prop("magnifier", R.x, R.y, 26, 34);
            Web(18);
        }

        // 가까운 물건부터 줄줄이 이어 실 경로를 만듦
        void Web(int max)
        {
            foreach (var p in path) { KillProp(p.line); KillProp(p.dot); }
            path.Clear();
            var pool = ItemsIn(R.x, R.y, ULT_R); if (pool.Count > 36) pool.RemoveRange(36, pool.Count - 36);
            float cx = R.x, cy = R.y;
            while (pool.Count > 0 && path.Count < max)
            {
                int bi = 0; float bd = float.MaxValue;
                for (int i = 0; i < pool.Count; i++) { float d = Dist(pool[i].x, pool[i].y, cx, cy); if (d < bd) { bd = d; bi = i; } }
                var it = pool[bi]; pool.RemoveAt(bi); cx = it.x; cy = it.y;
                var p = new Pin { it = it, x = it.x, y = it.y, line = Prop("dot", it.x, it.y, LINE_Z, 10), dot = Prop("dot", it.x, it.y, LINE_Z, 14) };
                if (p.line != null) { p.line.tint = red; p.line.sortBias = 4000; p.line.visible = false; }
                if (p.dot != null) { p.dot.tint = red; p.dot.sortBias = 4001; p.dot.visible = false; }
                path.Add(p);
            }
            wd = T > 0 ? 0.35f : 0.5f; c0 = T;
            Fx?.Ring(R.x, R.y, 120, red, 0.4f);
        }

        public override void Step(float dt, float k)
        {
            if (T >= 3.5f && !again) { again = true; Web(12); Flash(red, 0.15f); Fx?.Shake(0.1f); }
            float lt = T - c0;
            bool sleuth = lt < WEB;
            R.UltPose = sleuth ? P(front: 1.9f, farFront: 0.4f, head: -0.2f, tail: 1) : PoseUp();
            if (sleuth) Walk(dt, 90);                                         // 실을 걸며 돌아다님
            if (glass != null)
            {
                glass.visible = sleuth; glass.flip = R.face < 0;
                glass.x = R.x + R.face * 22; glass.y = R.y + 1; glass.z = R.z + 26 + Mathf.Sin(T * 8) * 3; glass.rot = Mathf.Sin(T * 5) * 0.2f;
            }
            foreach (var p in path) if (p.it && p.it.State == Item.ItemState.Rest) { p.x = p.it.x; p.y = p.it.y; }
            if (lt > WEB)
                for (int i = 0; i < path.Count; i++)
                {
                    var p = path[i];
                    if (p.boom || lt <= WEB + i * GAP) continue;
                    p.boom = true;
                    if (p.it && p.it.State == Item.ItemState.Rest)
                    {
                        p.it.Damage(UltD * wd, R, true, Rand(0, Mathf.PI * 2));
                        if (p.it.State == Item.ItemState.Rest) FlingItem(p.it, Rand(0, Mathf.PI * 2), 200, 520);
                    }
                    if (OnScreen(p.x, p.y))
                    {
                        Fx?.Ring(p.x, p.y, 50, boom, 0.35f); Fx?.Anim("hit", p.x, p.y, 20, 1.1f);
                        Fx?.Burst(p.x, p.y, 20, 6, red, Color.white, 120, 320); Fx?.Shake(0.05f);
                        if (i == path.Count - 1) { Fx?.Ring(p.x, p.y, 120, Col, 0.45f); Fx?.Hitstop(0.05f); Fx?.Shake(0.2f); }
                    }
                }
            DrawStrings(lt);
        }

        // 빨간 실: 쥐 → 물건1 → 물건2 … (걸리는 중엔 하나씩 늘어남, 터진 구간은 사라짐)
        void DrawStrings(float lt)
        {
            int n = Mathf.Min(path.Count, Mathf.FloorToInt(lt / 1.2f * path.Count) + 1);
            float sx = R.x, sy = R.y;
            for (int i = 0; i < path.Count; i++)
            {
                var p = path[i];
                bool gone = i >= n || (p.boom && lt > WEB + i * GAP + 0.2f);
                if (p.line != null) p.line.visible = !gone;
                if (p.dot != null) p.dot.visible = !gone;
                if (!gone) { Segment(p.line, sx, sy, p.x, p.y); if (p.dot != null) { p.dot.x = p.x; p.dot.y = p.y; } }
                if (i < n) { sx = p.x; sy = p.y; }
            }
        }

        // 두 점 사이 (높이 LINE_Z) 를 잇는 얇은 줄: 둥근 점 그림을 길게 늘여 돌림
        static void Segment(UltProp l, float x1, float y1, float x2, float y2)
        {
            if (l == null) return;
            float dx = x2 - x1, dy = (y2 - y1) * World.TILT, len = Mathf.Max(4, Mathf.Sqrt(dx * dx + dy * dy));
            l.x = (x1 + x2) / 2; l.y = (y1 + y2) / 2; l.z = LINE_Z;
            l.w = len * 1.1f; l.flat = THICK / l.w; l.rot = Mathf.Atan2(-dy, dx);
        }
    }
}
