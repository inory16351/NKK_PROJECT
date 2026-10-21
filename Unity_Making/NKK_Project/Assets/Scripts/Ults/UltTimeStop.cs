using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 메이드 쥐 · 살인 인형 (2026-10-20 사용자: 필살기 = 시간 정지 + 칼, 시간 정지는 연출만)
    // ① 회중시계를 꺼내 째깍째깍 → ② "시간이여, 멈춰라!" 화면이 회색으로 (게임은 그대로 흐름 — 연출만)
    // → ③ 메이드가 화면 안 물건·생명체 옆으로 순간이동하며 둘레 공중에 칼을 잔뜩 깔아 둠 (칼끝은 대상 쪽)
    // → ④ "…그리고 시간은 움직인다." 회색이 걷히며 칼이 일제히 꽂힘. 자막 c1~c5 (쥐 테이블 Ult_Caption)
    // 소품 (UltimateManager props, 그림 없으면 효과만): maid_knife(칼끝 오른쪽) · maid_clock(바늘 없는 시계판) · maid_hand_long/short(위를 가리킴, 아래 끝이 축) · maid_clockring(빛 고리)
    public class UltTimeStop : UltBase
    {
        const float STOP = 0.75f, RESUME = 2.75f, FLY = 0.22f;          // 정지 순간 · 재개 순간 · 칼이 날아가는 시간
        const int MAX_TARGETS = 12, MAX_KNIVES = 64, MIN_TARGETS = 6;     // 화면에 대상이 MIN 보다 적으면 바닥 자리를 더해 칼을 깜 (빈 방에서도 연출이 보이게)
        const float KNIFE_W = 82;                                           // 칼 그림 폭 (게임 단위)
        public override float Dur => RESUME + 1.7f;

        static readonly Color Gold = new(0.85f, 0.71f, 0.35f), Silver = new(0.86f, 0.89f, 0.95f), Gray = new(0.42f, 0.42f, 0.5f);

        class Target { public Item item; public int actor; public object who; public float x, y, h; public int knives; }   // actor: 0 물건 · 1 고양이 · 2 보스 · 3 사람 · 4 바닥 자리 (대상이 적을 때)
        class Knife { public Target t; public float ang, dist, born, delay; public UltProp p; public bool hit; }
        readonly List<Target> targets = new();
        readonly List<Knife> knives = new();
        UltProp clock, hourHand, minHand, ring;
        float hourA, minA, blinkT, spawnT;
        int blinkIdx;
        bool stopped, resumed;

        public override void Begin()
        {
            clock = Prop("maid_clock", R.x, R.y, 150, 110);
            hourHand = Prop("maid_hand_short", R.x, R.y, 150, 9);
            minHand = Prop("maid_hand_long", R.x, R.y, 150, 8);
            ring = Prop("maid_clockring", R.x, R.y, 150, 110);
            if (ring != null) ring.alpha = 0;
            hourA = Rand(0, Mathf.PI * 2); minA = Rand(0, Mathf.PI * 2);
            Beat(0.05f, () => Cap("c1"));
            Beat(STOP, () => Cap("c2"));
            Beat(RESUME, () => Cap("c3"));
            Beat(RESUME + 0.8f, () => Cap("c4"));
            Beat(RESUME + 1.3f, () => Cap("c5"));
        }

        public override void Step(float dt, float k)
        {
            R.UltPose = PoseUp(T < STOP ? Mathf.Min(1, T * 3) : 0.6f);
            StepClock(dt);

            if (T < STOP)
            {
                // 째깍째깍: 바늘이 돌고 쥐 둘레로 금빛 알갱이
                if ((hitT -= dt) <= 0) { hitT = 0.25f; SfxManager.PlayAt("ui_click", R.x, R.y, 0.7f, 0.8f); Fx?.Burst(R.x, R.y, 150, 4, Gold, Color.white, 30, 90, 2, 4); }
                return;
            }
            if (!stopped) BeginStop();

            if (T < RESUME)
            {
                Flash(Gray, 0.46f);                                  // 멈춘 세계 = 회색 (계속 켜 둠)
                StepPlace(dt);
                return;
            }
            if (!resumed) BeginResume();
            StepFly(dt);
        }

        // ── 시계: 정지 전엔 돌고, 정지 중엔 멈춤, 재개 순간 빙글 ──
        void StepClock(float dt)
        {
            if (!stopped) { minA -= dt * 9; hourA -= dt * 0.75f; }
            else if (resumed && T < RESUME + 0.5f) { minA -= dt * 40; hourA -= dt * 3.3f; }
            float a = T < 0.3f ? Ease(T / 0.3f) : T > Dur - 0.4f ? Mathf.Clamp01((Dur - T) / 0.4f) : 1;
            float cx = R.x, cy = R.y, cz = 150 + Mathf.Sin(T * 3) * 4;
            Place(clock, cx, cy, cz, 0, a);
            // 바늘: 그림 가운데가 피벗 → 축(아래 끝)이 시계 가운데에 오게 바늘 방향으로 반 길이만큼 밀어 둠
            PlaceHand(hourHand, cx, cy, cz, hourA, a);
            PlaceHand(minHand, cx, cy, cz, minA, a);
            if (ring != null && ring.alpha > 0) { ring.x = cx; ring.y = cy; ring.z = cz; ring.Apply(); }
        }
        void Place(UltProp p, float x, float y, float z, float rot, float a)
        {
            if (p == null) return;
            p.x = x; p.y = y; p.z = z; p.rot = rot; p.alpha = a; p.sortBias = 30; p.Apply();
        }
        void PlaceHand(UltProp p, float x, float y, float z, float rot, float a)
        {
            if (p == null || !p.r || !p.r.sprite) return;
            var b = p.r.sprite.bounds.size;
            float half = p.w * b.y / b.x * 0.5f * 0.85f;                  // 축 원이 그림 아래쪽 안에 있어 조금 덜 밂
            Place(p, x - Mathf.Sin(rot) * half, y, z + Mathf.Cos(rot) * half, rot, a);
            p.sortBias = 31;
            p.Apply();
        }

        // ── ② 시간 정지 ──
        void BeginStop()
        {
            stopped = true;
            Flash(Color.white, 0.8f); Fx?.Hitstop(0.12f); Fx?.Shake(0.25f);
            Fx?.Ring(R.x, R.y, 900, Silver, 0.6f); Fx?.Ring(R.x, R.y, 520, Gold, 0.45f);
            SfxManager.PlayAt("superjump_charge", R.x, R.y);
            if (ring != null) { ring.alpha = 1; ring.w = 110; }
            FindTargets();
            blinkT = 0; spawnT = 0; blinkIdx = 0;
        }

        // 화면 안 대상: 생명체 먼저(고양이 · 보스 · 사람), 그다음 가까운 물건
        void FindTargets()
        {
            targets.Clear();
            var v = ViewRect(-40);
            var cat = RatMgr.Cats ? RatMgr.Cats.Current : null;
            if (cat && cat.Alive && v.Contains(new Vector2(cat.x, cat.y))) targets.Add(new Target { actor = 1, who = cat, x = cat.x, y = cat.y, h = 50, knives = 8 });
            var boss = NKK.Hazards.Boss.Current;
            if (boss && boss.State == NKK.Hazards.Boss.BState.Fight && v.Contains(new Vector2(boss.x, boss.y)))
                targets.Add(new Target { actor = 2, who = boss, x = boss.x, y = boss.y, h = boss.Height * 0.5f, knives = 10 });
            foreach (var h in ItemMgr.Humans)
                if (h && h.Alive && v.Contains(new Vector2(h.x, h.y)) && targets.Count < MAX_TARGETS) targets.Add(new Target { actor = 3, who = h, x = h.x, y = h.y, h = 70, knives = 7 });
            var items = ItemsIn(v.center.x, v.center.y, Mathf.Max(v.width, v.height));
            items.RemoveAll(it => !it.Appeared || !v.Contains(new Vector2(it.x, it.y)));
            items.Sort((a, b) => Dist(a.x, a.y, R.x, R.y).CompareTo(Dist(b.x, b.y, R.x, R.y)));
            foreach (var it in items)
            {
                if (targets.Count >= MAX_TARGETS) break;
                targets.Add(new Target { item = it, x = it.x, y = it.y, h = Mathf.Max(20, it.R * 0.6f), knives = 5 });
            }
            // 대상이 적으면 화면 바닥 곳곳 (열린 방 안)에 칼을 깔아 둠 → 재개 때 그 자리 충격 (물건이 없어도 연출이 비지 않게)
            for (int tries = 0; targets.Count < MIN_TARGETS && tries < 40; tries++)
            {
                float px = Rand(v.xMin + v.width * 0.15f, v.xMax - v.width * 0.1f), py = Rand(v.yMin + v.height * 0.2f, v.yMax - v.height * 0.15f);
                float qx = px, qy = py, vx0 = 0, vy0 = 0; M.Stage.Confine(ref qx, ref qy, ref vx0, ref vy0, 30, R.x, R.y, 0);
                if (Dist(qx, qy, px, py) > 5) continue;
                bool near = false; foreach (var o in targets) if (Dist(o.x, o.y, px, py) < 220) { near = true; break; }
                if (!near) targets.Add(new Target { actor = 4, x = px, y = py, h = 40, knives = 6 });
            }
        }

        // ── ③ 순간이동하며 칼 깔기 ──
        void StepPlace(float dt)
        {
            if (ring != null) { ring.w += dt * 900; ring.alpha = Mathf.Max(0, ring.alpha - dt * 1.2f); }
            if (targets.Count == 0) return;
            float per = (RESUME - STOP - 0.25f) / targets.Count;          // 대상 하나에 쓰는 시간
            if ((blinkT -= dt) <= 0 && blinkIdx < targets.Count)
            {
                var t = targets[blinkIdx++]; blinkT = per;
                Track(t);
                // 대상 옆으로 순간이동 (잔상 반짝)
                Fx?.Stars(R.x, R.y, 20, 6, Silver, Gold, 60, 160);
                int side = R.x < t.x ? -1 : 1;
                MoveRat(R, t.x + side * (t.h + 40) - R.x, t.y + 12 - R.y);
                R.face = -side;
                Fx?.Stars(R.x, R.y, 20, 6, Silver, Gold, 60, 160);
                SfxManager.PlayAt("rat_dash", R.x, R.y, 0.6f, 1.4f);
                // 둘레에 칼 (세로 원, 칼끝은 대상 가운데로)
                int n = Mathf.Min(t.knives, MAX_KNIVES - knives.Count);
                t.knives = n;                                               // 바닥 자리는 이 수만큼 꽂히면 충격
                float a0 = Rand(0, Mathf.PI * 2);
                for (int i = 0; i < n; i++)
                {
                    var kn = new Knife { t = t, ang = a0 + i * Mathf.PI * 2 / n + Rand(-0.15f, 0.15f), dist = t.h + Rand(80, 120), born = T + i * per * 0.6f / n, delay = Rand(0, 0.12f) };
                    kn.p = Prop("maid_knife", t.x, t.y, t.h, KNIFE_W);
                    if (kn.p != null) kn.p.alpha = 0;
                    knives.Add(kn);
                }
            }
            foreach (var kn in knives) PlaceKnife(kn, 0);
        }

        // 대상 위치 따라가기 (정지는 연출만이라 대상은 계속 움직임)
        static void Track(Target t)
        {
            switch (t.actor)
            {
                case 0: if (t.item) { t.x = t.item.x; t.y = t.item.y; } break;
                case 1: { var c = t.who as NKK.Hazards.Cat; if (c) { t.x = c.x; t.y = c.y; } break; }
                case 2: { var b = t.who as NKK.Hazards.Boss; if (b) { t.x = b.x; t.y = b.y; } break; }
                case 3: { var h = t.who as NKK.Humans.Human; if (h) { t.x = h.x; t.y = h.y; } break; }
            }
        }

        // fly = 0(제자리) ~ 1(꽂힘)
        void PlaceKnife(Knife kn, float fly)
        {
            if (kn.p == null) return;
            if (!kn.hit) Track(kn.t);
            float d = kn.dist * (1 - fly) + 6;
            float a = T < kn.born ? 0 : Mathf.Clamp01((T - kn.born) / 0.12f);
            kn.p.x = kn.t.x + Mathf.Cos(kn.ang) * d;
            kn.p.y = kn.t.y;
            kn.p.z = kn.t.h + Mathf.Sin(kn.ang) * d;
            kn.p.rot = kn.ang + Mathf.PI;                                // 칼끝(그림 오른쪽)이 대상 쪽
            kn.p.alpha = kn.hit ? kn.p.alpha : a;
            kn.p.sortBias = Mathf.Sin(kn.ang) < 0 ? 5 : -5;              // 아래쪽 칼은 대상 앞, 위쪽 칼은 뒤
            kn.p.Apply();
        }

        // ── ④ 시간 재개 ──
        void BeginResume()
        {
            resumed = true;
            Flash(Color.white, 0.6f); Fx?.Hitstop(0.08f);
            SfxManager.PlayAt("laser", R.x, R.y, 0.8f, 1.3f);
            if (ring != null) { ring.alpha = 0.9f; ring.w = 900; }
        }

        void StepFly(float dt)
        {
            if (ring != null && ring.alpha > 0) { ring.w = Mathf.Max(110, ring.w - dt * 2400); ring.alpha = Mathf.Max(0, ring.alpha - dt * 1.5f); }
            float tf = T - RESUME;
            foreach (var kn in knives)
            {
                if (kn.p != null && kn.hit) { kn.p.alpha = Mathf.Max(0, kn.p.alpha - dt * 2.5f); }
                float f = Mathf.Clamp01((tf - kn.delay) / FLY);
                PlaceKnife(kn, f * f);
                if (!kn.hit && f >= 1) { kn.hit = true; HitKnife(kn); }
            }
            Fx?.Shake(tf < 0.4f ? 0.04f : 0);
        }

        void HitKnife(Knife kn)
        {
            var t = kn.t;
            if (OnScreen(t.x, t.y)) Fx?.Burst(t.x, t.y, t.h, 3, Silver, Color.white, 60, 200, 2, 5);
            if (t.actor == 0)
            {
                if (t.item && t.item.State == Item.ItemState.Rest) t.item.SkillHit(UltD * 0.3f, R);
            }
            else if (t.actor == 4) { if (--t.knives == 0) Shock(t.x, t.y, 120, UltD * 0.3f, Silver, 1); }      // 바닥 자리: 마지막 칼이 꽂힐 때 작은 충격
            else BlastActors(t.x, t.y, 50, 180, UltD * 0.12f);
            SfxManager.PlayAt("hit_metal", t.x, t.y, 0.35f, Rand(1.1f, 1.5f));
        }

        public override void Finish()
        {
            Fx?.Burst(R.x, R.y, 120, 14, Gold, Silver, 120, 320, 3, 7);
        }
    }
}
