using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 사무라이 쥐 · 벽력일섬 (웹게임 split): 육연·팔연 돌진 뒤 납도와 궤적 폭발 / c1~c5 자막, c6 납도 팝업
    // 그림: FxManager.BoltLine (번개 조각) · Slash (초승달·일자·X 베기) · Spark — FX_Beam 그림이 없으면 예전 줄 모양
    public class UltSplit : UltBase
    {
        public override float Dur => 6.1f;
        readonly List<(Vector2 a, Vector2 b)> bolts = new();
        readonly HashSet<Item> cut = new();
        static readonly Color Gold = new(1, 0.95f, 0.75f), Ember = new(0.94f, 0.78f, 0.47f);    // #fff3bf · #f0c878
        int set, count;
        bool exploded;
        float boomT, drawT;

        public override void Begin()
        {
            Beat(0.05f, () => Cap("c1")); Beat(0.5f, () => Cap("c2"));
            Beat(0.95f, () => Cap("c3")); Beat(1.75f, () => Cap("c4"));
            Beat(2.25f, Click); Beat(2.9f, () => Cap("c5")); Beat(4.92f, Click);
        }
        private void Click() => PopupCap("c6", R.x, R.y, Color.white, 18, 1, 55);

        public override void Step(float dt, float k)
        {
            if (T >= 2.9f && set == 0) { set = 1; count = 0; exploded = false; bolts.Clear(); cut.Clear(); }
            float local = T - (set == 0 ? 0 : 2.9f), charge = set == 0 ? 0.95f : 0.5f;
            int total = set == 0 ? 6 : 8;
            float dashEnd = charge + total * 0.11f;
            if (local < charge)
            {
                R.UltPose = P(front: 0.9f, farFront: 0.6f, back: -1, farBack: -0.8f, head: 0.4f, tail: -0.2f, bob: 7, sy: 0.84f, sx: 1.12f);
                R.UltJit = 1.5f;
                if ((drawT -= dt) <= 0)
                {
                    // 몸에서 튀는 번개 (웹 zap: 4토막·꺾임 8·굵기 3) + 가끔 전기 불꽃
                    drawT = 0.04f; float a = Rand(0, Mathf.PI * 2), d = Rand(20, 60);
                    Fx?.BoltLine(R.x, R.y, 20, R.x + Mathf.Cos(a) * d, R.y, 20 + Mathf.Sin(a) * d, Gold, 0.08f, 3, 4, 8);
                    if (Random.value < 0.35f) Fx?.Spark(R.x + Rand(-24, 24), R.y, Rand(10, 40), Gold, Rand(26, 44), 0.12f);
                }
                return;
            }
            R.UltJit = 0;
            while (count < total && local >= charge + count * 0.11f) { Dash(); count++; }
            if (local < dashEnd)
                R.UltPose = P(front: 1.9f, farFront: 1.3f, back: -1.5f, farBack: -1.3f, head: -0.25f, tail: -0.7f, sx: 1.18f, sy: 0.9f);
            else R.UltPose = local < dashEnd + 0.64f ? P(front: 1.2f, farFront: 0.8f, back: -0.6f, head: -0.1f, tail: -0.4f)
                : P(front: 0.4f, farFront: 0.3f, head: 0.35f, tail: -0.2f);
            if (local >= dashEnd + 0.69f && !exploded) Explode();
            if (!exploded) Flash(new Color(20 / 255f, 18 / 255f, 40 / 255f), 0.32f);
            if ((drawT -= dt) <= 0)
            {
                drawT = 0.05f;
                float glow = exploded ? Mathf.Max(0, 1 - (T - boomT) / 0.9f) * 1.6f : local > dashEnd ? 0.8f + 0.2f * Mathf.Sin(T * 30) : 1;
                if (glow > 0.02f) foreach (var bolt in bolts) DrawBolt(bolt.a, bolt.b, glow);
            }
        }

        private void Dash()
        {
            Item target = null; float best = float.MaxValue;
            foreach (var it in ItemsIn(R.x, R.y, ULT_R))
            {
                float d = Dist(it.x, it.y, R.x, R.y);
                if (!cut.Contains(it) && d < best) { target = it; best = d; }
            }
            float tx = target ? target.x : X0 + Rand(-300, 300), ty = target ? target.y : Y0 + Rand(-220, 220);
            float a = Mathf.Atan2(ty - R.y, tx - R.x) + Rand(-0.2f, 0.2f), length = Dist(tx, ty, R.x, R.y) + 70;
            var start = new Vector2(R.x, R.y);
            MoveRat(R, Mathf.Cos(a) * length, Mathf.Sin(a) * length);
            var end = new Vector2(R.x, R.y); Vector2 delta = end - start;
            float len = Mathf.Max(1, delta.magnitude); Vector2 dir = delta / len, mid = (start + end) * 0.5f;
            foreach (var it in ItemsIn(mid.x, mid.y, len * 0.5f + 40))
            {
                var p = new Vector2(it.x, it.y) - start; float along = Vector2.Dot(p, dir);
                if (along > -20 && along < len + 20 && Mathf.Abs(p.x * dir.y - p.y * dir.x) < it.R + 46) cut.Add(it);
            }
            bolts.Add((start, end)); R.face = dir.x >= 0 ? 1 : -1;
            for (int i = 0; i < 4; i++)
            {
                var p = Vector2.Lerp(start, end, i / 4f);
                Fx?.Stars(p.x, p.y, 14, 2, Gold, Color.white, 10, 35);
            }
            // 베기 연출: 궤적 따라 일자 베기 + 도착점 초승달 + 출발·도착 번개 불꽃
            float ang = Mathf.Atan2(dir.y, dir.x);
            Fx?.Slash(mid.x, mid.y, 18, ang, len + 60, Gold, 0.3f, 1);
            Fx?.Slash(end.x, end.y, 26, ang + Rand(-0.6f, 0.6f), Rand(130, 170), Gold, 0.28f, 0, Random.value < 0.5f);
            Fx?.Spark(start.x, start.y, 16, Gold, 60, 0.18f); Fx?.Spark(end.x, end.y, 20, Color.white, 70, 0.2f);
            Fx?.BoltLine(start.x, start.y, 14, end.x, end.y, 14, Color.white, 0.12f, 9, 9, 22, Ember);
            Flash(Gold, 0.22f); Fx?.Shake(0.12f);
        }

        private void Explode()
        {
            exploded = true; boomT = T;
            foreach (var bolt in bolts)
            {
                var mid = (bolt.a + bolt.b) * 0.5f;
                // 웹의 마무리는 궤적 중점 원 안까지 판정을 넓힘.
                foreach (var it in ItemsIn(mid.x, mid.y, Vector2.Distance(bolt.a, bolt.b) * 0.5f + 50)) cut.Add(it);
                foreach (var rat in RatsNear(mid.x, mid.y, 80)) Ragdoll(rat, Rand(0, Mathf.PI * 2));
                Fx?.Ring(mid.x, mid.y, 80, Gold, 0.4f);
                // 궤적마다 X 베기 + 일자 베기 + 불꽃
                float len = Vector2.Distance(bolt.a, bolt.b), ang = Mathf.Atan2(bolt.b.y - bolt.a.y, bolt.b.x - bolt.a.x);
                Fx?.Slash(mid.x, mid.y, 24, ang + Rand(-0.3f, 0.3f), Mathf.Clamp(len * 0.6f, 120, 260), Gold, 0.45f, 2);
                Fx?.Slash(mid.x, mid.y, 18, ang, len + 90, Color.white, 0.4f, 1);
                Fx?.Spark(mid.x, mid.y, 24, Gold, 120, 0.3f);
            }
            foreach (var it in cut) if (it && it.State == Item.ItemState.Rest)
            {
                FlingItem(it, Rand(0, Mathf.PI * 2), 320, 520);
                if (OnScreen(it.x, it.y)) { Fx?.Stars(it.x, it.y, 20, 8, Gold, Color.white, 160, 420); Fx?.Slash(it.x, it.y, 20, Rand(0, Mathf.PI), Rand(90, 130), Gold, 0.3f, Random.value < 0.5f ? 0 : 1, Random.value < 0.5f); }
            }
            Flash(Color.white, 0.55f); Fx?.Shake(0.45f);
        }

        // 웹 split draw: 꺾인 번개 9토막·꺾임 22, 번진 빛 #f0c878(0.35)·몸통 #fff3bf(0.9)·흰 심지 — 매번 새로 꺾여서 지지직
        private void DrawBolt(Vector2 start, Vector2 end, float glow)
        {
            Color body = Gold; body.a = Mathf.Min(1, 0.9f * glow);
            Color halo = Ember; halo.a = Mathf.Min(1, 0.35f * glow);
            Fx?.BoltLine(start.x, start.y, 14, end.x, end.y, 14, body, 0.07f, 9, 9, 22, halo);
        }
        public override void Finish() { R.UltJit = 0; }
    }
}
