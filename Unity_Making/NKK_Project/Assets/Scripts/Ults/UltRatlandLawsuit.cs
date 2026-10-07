using System.Collections.Generic;
using NKK.Items;
using NKK.Rats;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace NKK.Ults
{
    // 쥐랜드 쥐 · 쥐랜드 성 대개장 (웹게임 lawsuit): 사방 포위 → 수갑·압류 → 짐 반출과 성 폭파.
    // 자막 c1~c3 등장, c4 딱지 월드 글자, c5~c7 압류 반응, c8 철거 완료, c9 폭발 팝업, c10~c11 체포.
    public class UltRatlandLawsuit : UltBase
    {
        public override float Dur => 7;
        const float RAID = 0.95f, CLOSE = 2.05f, HAUL = 4.65f, BOOM = 6.05f;
        const int COUNT = 8;
        static readonly Color Red = new(0.88f, 0.24f, 0.25f), Gold = new(1, 0.86f, 0.52f), Blue = new(0.18f, 0.45f, 1);
        class Lawyer
        {
            public UltProp a, b, tag;
            public TMP_Text text;
            public Item item;
            public float sx, sy, tx, ty, arrival, stamp, ix, iy;
            public bool arrived, launched;
        }
        class Puff { public UltProp p; public float vx, vy, vz, width; }
        readonly List<Lawyer> lawyers = new();
        readonly List<Puff> smoke = new();
        readonly HashSet<Item> reserved = new();
        class Prisoner
        {
            public Rat rat;
            public UltProp cuffs;
            public float x, y, caughtAt;
            public bool caught;
        }
        class Officer
        {
            public UltProp stand, run, cuff;
            public Prisoner target;
            public float x, y, angle, start;
            public int side, rank, flank;
        }
        readonly List<Officer> police = new();
        readonly List<Prisoner> prisoners = new();
        readonly List<Image> edgeLights = new();
        readonly UltProp[] sirens = new UltProp[4], tape = new UltProp[4];
        GameObject edgeRoot;
        float sirenT;
        UltProp castle;
        float castleY, dustT;
        bool exploded;

        public override void Begin()
        {
            Cap("c1");
            Beat(0.85f, () => Cap("c2")); Beat(1.35f, () => Cap("c3"));
            Beat(2.35f, () => Cap("c5")); Beat(3.25f, () => Cap("c6"));
            Beat(4.45f, () => Cap("c7"));
            Beat(1.65f, () => PopupCap("c10", X0, Y0, Red, 30, 1, 130));
            castleY = Y0 - 95;
            castle = Prop("ratland_castle", X0, castleY, 0, 360);
            if (castle != null) { castle.flat = 0; castle.sortBias = -20; }
            var view = ViewRect();
            for (int i = 0; i < COUNT; i++)
            {
                var l = new Lawyer { sx = i % 2 == 0 ? view.xMin - 90 : view.xMax + 90,
                    sy = Y0 + (i - 3.5f) * 32, arrival = 2.65f + i * 0.1f };
                l.item = FindItem(l.sx, l.sy);
                l.tx = l.item ? l.item.x : X0 + (i - 3.5f) * 50;
                l.ty = l.item ? l.item.y : Y0 + 60 + i % 2 * 45;
                l.a = Prop("lawyer_rat_point", l.sx, l.sy, 0, 78);
                l.b = Prop("lawyer_rat_doc", l.sx, l.sy, 0, 78);
                if (l.a != null) l.a.visible = false;
                if (l.b != null) l.b.visible = false;
                lawyers.Add(l);
            }
            BeginArrest();
        }

        Item FindItem(float x, float y)
        {
            Item best = null; float distance = float.MaxValue;
            foreach (var it in ItemsIn(X0, Y0, ULT_R))
            {
                if (!it.Appeared || reserved.Contains(it) || !OnScreen(it.x, it.y, 0)) continue;
                float d = Dist(x, y, it.x, it.y);
                if (d < distance) { best = it; distance = d; }
            }
            if (best) reserved.Add(best);
            return best;
        }

        public override void Step(float dt, float k)
        {
            R.UltPose = T < 1.3f ? PoseUp() : T < 4.45f ? P(head: -0.35f, front: 0.7f, tail: -0.2f) : PoseUp();
            R.UltJit = T > 4.45f && !exploded ? 3 : 0;
            if (!exploded && castle != null)
            {
                float rise = Ease(Mathf.Clamp01(T / 1.15f));
                castle.flat = rise;
                castle.x = X0 + (T < 1.15f ? Mathf.Sin(T * 65) * 5 * (1 - rise) : T > 5.5f ? Mathf.Sin(T * 75) * 4 : 0);
                castle.z = GroundLift(castle);
            }
            if (T < 1.15f && (dustT -= dt) <= 0)
            {
                dustT = 0.12f; Fx?.Dust(X0 + Rand(-150, 150), castleY, 5, 1.4f); Fx?.Shake(0.035f);
            }
            StepSirens(dt); StepTape();
            foreach (var p in police) StepOfficer(p, dt);
            for (int i = 0; i < prisoners.Count; i++) StepPrisoner(prisoners[i], i);
            if (T >= 1.15f && T < 4.3f && (dustT -= dt) <= 0)
            {
                dustT = 0.14f;
                for (int i = 0; i < police.Count; i += 4) Fx?.Dust(police[i].x, police[i].y, 1, 0.55f);
            }
            for (int i = 0; i < lawyers.Count; i++) StepLawyer(lawyers[i], i);
            if (T >= BOOM && !exploded) Explode();
            foreach (var s in smoke)
            {
                if (s.p == null) continue;
                float age = Mathf.Max(0, T - BOOM);
                s.p.x += s.vx * dt; s.p.y += s.vy * dt; s.p.z += s.vz * dt;
                s.p.w = s.width * (0.4f + age * 1.4f); s.p.alpha = Mathf.Clamp01(1 - age / (Dur - BOOM)) * 0.8f;
                s.p.rot += dt * 0.3f;
            }
        }

        void StepLawyer(Lawyer l, int index)
        {
            float start = 1.35f + index * 0.035f;
            if (T < start)
            {
                var view = ViewRect(); l.sx = index % 2 == 0 ? view.xMin - 100 : view.xMax + 100; return;
            }
            if (!l.arrived)
            {
                // 다른 행동이 먼저 물건을 날렸으면 아직 바닥에 있는 물건으로 교체.
                if (!l.item || l.item.State != Item.ItemState.Rest)
                    l.item = FindItem(l.tx, l.ty);
                if (l.item) { l.tx = l.item.x; l.ty = l.item.y; }
            }
            float e = Ease(Mathf.Clamp01((T - start) / (l.arrival - start)));
            float side = index % 2 == 0 ? -1 : 1;
            float haul = Ease(Mathf.Clamp01((T - HAUL - index * 0.035f) / 0.7f));
            float x = Mathf.Lerp(l.sx, l.tx + side * 45, e), y = Mathf.Lerp(l.sy, l.ty + 12, e);
            x += side * haul * 145; y += haul * 25;
            bool walking = T < l.arrival || (haul > 0 && haul < 1);
            bool document = T >= 3.25f || (walking && Mathf.FloorToInt(T * 8 + index) % 2 == 0);
            SetActor(l.a, x, y, !document, walking, haul > 0 ? side > 0 : side < 0);
            SetActor(l.b, x, y, document, walking, haul > 0 ? side > 0 : side < 0);
            if (!l.arrived && T >= l.arrival)
            {
                l.arrived = true;
                if (GrabItem(l.item)) { l.ix = l.item.x; l.iy = l.item.y; Stamp(l); }
            }
            if (l.item && Items.Contains(l.item) && !l.launched)
            {
                l.item.x = l.ix + side * haul * 145; l.item.y = l.iy + haul * 25;
                l.item.z = haul * (25 + Mathf.Abs(Mathf.Sin(T * 22 + index)) * 5);
            }
            if (!l.launched && T >= 5.35f + index * 0.07f)
            {
                l.launched = true;
                if (l.item && Items.Contains(l.item))
                {
                    float a = Mathf.Atan2(l.item.y - castleY, l.item.x - X0);
                    FlingItem(l.item, a, 600 + index * 25, 420);
                    PopupCap("c9", l.item.x, l.item.y, Gold, 20, 0.6f, 45);
                }
            }
            UpdateTag(l);
        }

        void SetActor(UltProp p, float x, float y, bool visible, bool walking, bool right)
        {
            if (p == null) return;
            p.x = x; p.y = y; p.flip = right; p.visible = visible;
            p.z = GroundLift(p) + (exploded ? Mathf.Sin(Mathf.Clamp01((T - BOOM) / 0.5f) * Mathf.PI) * 35 : 0) + (walking ? Mathf.Abs(Mathf.Sin(T * 31.4f)) * 5 : 0);
            p.rot = walking ? Mathf.Sin(T * 31.4f) * 0.045f : 0;
            p.alpha = Mathf.Clamp01((Dur - T) / 0.25f);
        }

        void Stamp(Lawyer l)
        {
            l.stamp = T; l.tag = Prop("seizure_tag", l.item.x, l.item.y, 30, 54);
            var tpl = Fx ? Fx.popupTemplate : null;
            if (tpl && l.tag != null)
            {
                l.text = Object.Instantiate(tpl, tpl.transform.parent);
                l.text.name = "UltSeizureLabel"; l.text.text = CapText("c4");
                l.text.fontSize = 18; l.text.color = Color.white; l.text.fontStyle |= FontStyles.Bold;
                l.text.alignment = TextAlignmentOptions.Center; l.text.textWrappingMode = TextWrappingModes.NoWrap;
                l.text.overflowMode = TextOverflowModes.Overflow; l.text.raycastTarget = false;
                l.text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                l.text.gameObject.SetActive(true);
            }
            Fx?.Ring(l.item.x, l.item.y, 42, Red, 0.23f); Fx?.Shake(0.035f);
            Fx?.Burst(l.item.x, l.item.y, 30, 5, Red, Color.white, 35, 95, 2, 4);
        }

        void UpdateTag(Lawyer l)
        {
            if (l.tag == null) return;
            if (!l.item || l.item.State == Item.ItemState.Dead)
            {
                KillProp(l.tag); l.tag = null;
                if (l.text) Object.Destroy(l.text.gameObject);
                l.text = null; return;
            }
            float pop = 1 + 0.5f * (1 - Ease(Mathf.Clamp01((T - l.stamp) / 0.18f)));
            var p = l.tag;
            float height = l.item.R * 1.3f;
            if (l.item.sprite && l.item.sprite.sprite)
            {
                var bounds = l.item.sprite.sprite.bounds;
                height *= bounds.size.y / Mathf.Max(0.001f, bounds.size.x);
            }
            p.x = l.item.x; p.y = l.item.y; p.z = l.item.z - l.item.R * 0.35f + height;
            p.w = 54 * pop; p.rot = -l.item.Rot - 0.12f;
            p.sortBias = 80; p.alpha = Mathf.Clamp01((Dur - T) / 0.3f);
            if (l.text)
            {
                l.text.rectTransform.position = World.ToUnity(p.x, p.y, p.z);
                l.text.rectTransform.localScale = new Vector3(pop, pop, 1);
                l.text.rectTransform.rotation = Quaternion.Euler(0, 0, p.rot * Mathf.Rad2Deg);
                l.text.alpha = p.alpha;
            }
        }

        void Explode()
        {
            exploded = true; Cap("c8"); KillProp(castle); castle = null;
            Shock(X0, castleY, ULT_R, UltD * 0.8f, Gold, 3);
            Fx?.Anim("explosion", X0, castleY, 90, 2.8f);
            Fx?.Burst(X0, castleY, 130, 75, new Color(0.6f, 0.45f, 0.55f), Gold, 230, 650, 9, 23);
            Fx?.Stars(X0, castleY, 100, 35, Gold, Color.white, 220, 580);
            Fx?.Ring(X0, castleY, ULT_R * 1.25f, Red, 0.6f);
            Flash(Gold, 0.55f); Fx?.Shake(0.45f); Fx?.Hitstop(0.07f);
            PopupCap("c9", X0, castleY, Color.white, 36, 0.8f, 160);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2 / 9;
                var p = Prop("color_puff", X0 + Mathf.Cos(a) * 75, castleY + Mathf.Sin(a) * 40, 60 + i % 3 * 45, 60);
                if (p != null) { p.tint = i % 2 == 0 ? Col : Gold; p.sortBias = 40; }
                smoke.Add(new Puff { p = p, vx = Mathf.Cos(a) * 120, vy = Mathf.Sin(a) * 65, vz = 45, width = Rand(95, 145) });
            }
        }


        UltProp HiddenProp(string name, float width)
        {
            var p = Prop(name, X0, Y0, 0, width);
            if (p != null) p.visible = false;
            return p;
        }

        void BeginArrest()
        {
            var nearby = RatsNear(X0, Y0, ULT_R * 0.8f);
            nearby.Sort((a, b) => Dist(a.x, a.y, X0, Y0).CompareTo(Dist(b.x, b.y, X0, Y0)));
            foreach (var rat in nearby)
            {
                if (!OnScreen(rat.x, rat.y, -30) || rat.temp > 0) continue;
                prisoners.Add(new Prisoner { rat = rat });
                if (prisoners.Count == 8) break;
            }
            for (int i = 0; i < 24; i++)
            {
                int side = i / 6, rank = i % 6;
                var p = new Officer { side = side, rank = rank, start = RAID + rank * 0.055f,
                    angle = Mathf.PI + side * Mathf.PI / 2 + (rank - 2.5f) * 0.21f,
                    flank = i % 2 == 0 ? -1 : 1, target = i / 2 < prisoners.Count ? prisoners[i / 2] : null };
                var pos = Entry(side, rank, ViewRect()); p.x = pos.x; p.y = pos.y;
                p.stand = HiddenProp("police_rat_a", 79); p.run = HiddenProp("police_rat_b", 79);
                p.cuff = HiddenProp("police_rat_cuff", 79); police.Add(p);
            }
            for (int i = 0; i < 4; i++)
            {
                sirens[i] = HiddenProp("police_siren", 55);
                tape[i] = HiddenProp("police_tape", 1);
            }
            MakeEdgeLights();
        }

        static Vector2 Entry(int side, int rank, Rect v)
        {
            float lane = (rank + 0.5f) / 6;
            return side switch
            {
                0 => new Vector2(v.xMin - 105, Mathf.Lerp(v.yMin, v.yMax, 1 - lane)),
                1 => new Vector2(Mathf.Lerp(v.xMin, v.xMax, lane), v.yMin - 105),
                2 => new Vector2(v.xMax + 105, Mathf.Lerp(v.yMin, v.yMax, lane)),
                _ => new Vector2(Mathf.Lerp(v.xMin, v.xMax, 1 - lane), v.yMax + 150)
            };
        }

        void StepOfficer(Officer p, float dt)
        {
            if (T < p.start)
            {
                var entry = Entry(p.side, p.rank, ViewRect()); p.x = entry.x; p.y = entry.y; return;
            }
            float close = Ease(Mathf.Clamp01((T - CLOSE) / 1.7f));
            float tx = X0 + Mathf.Cos(p.angle) * Mathf.Lerp(350, 135, close);
            float ty = Y0 + Mathf.Sin(p.angle) * Mathf.Lerp(255, 110, close);
            var target = p.target;
            bool valid = target != null && target.rat && (target.caught || !target.rat.UltOn);
            if (valid && T >= CLOSE)
            {
                float gap = target.rat.Radius + 32;
                tx = target.rat.x + p.flank * gap; ty = target.rat.y + 8;
            }
            float distance = Dist(p.x, p.y, tx, ty);
            var pos = Vector2.MoveTowards(new Vector2(p.x, p.y), new Vector2(tx, ty), (620 + distance * 1.3f) * dt);
            p.x = pos.x; p.y = pos.y;
            bool walking = distance > 7, cuffing = valid && T >= CLOSE && distance < 45;
            bool frame = Mathf.FloorToInt(T * 12 + p.rank) % 2 == 0;
            bool right = (valid && T >= CLOSE ? target.rat.x : X0) > p.x;
            SetActor(p.stand, p.x, p.y, !cuffing && (!walking || frame), walking, right);
            SetActor(p.run, p.x, p.y, !cuffing && walking && !frame, walking, right);
            SetActor(p.cuff, p.x, p.y, cuffing, walking, right);
        }

        void StepPrisoner(Prisoner p, int index)
        {
            var rat = p.rat; if (!rat || rat == R) return;
            if (!p.caught && T >= CLOSE && !rat.UltOn)
            {
                var left = police[index * 2]; var right = police[index * 2 + 1];
                float reach = rat.Radius + 49;
                if (Dist(left.x, left.y, rat.x, rat.y) < reach && Dist(right.x, right.y, rat.x, rat.y) < reach && GrabRat(rat))
                {
                    p.caught = true; p.caughtAt = T; p.x = rat.x; p.y = rat.y;
                    rat.z = rat.vz = rat.vx = rat.vy = 0;
                    p.cuffs = Prop("prop_handcuffs", rat.x, rat.y, 24, Mathf.Clamp(rat.Radius * 1.7f, 28, 48));
                    if (p.cuffs != null) p.cuffs.sortBias = 80;
                    PopupCap("c11", rat.x, rat.y, Color.white, 20, 0.75f, 65);
                    Fx?.Ring(rat.x, rat.y, 46, Blue, 0.25f); Fx?.Dust(rat.x, rat.y, 5, 0.85f);
                    Fx?.Stars(rat.x, rat.y, 25, 6, Color.white, Gold, 70, 170);
                }
            }
            if (!p.caught) return;
            // 좌표·속도는 고정, 자세만 버둥거린다. 수갑은 실제 쥐 앞발을 따라간다.
            rat.x = p.x; rat.y = p.y; rat.z = rat.vz = rat.vx = rat.vy = 0;
            rat.UltJit = 1.1f; rat.UltRot = Mathf.Sin(T * 33 + index) * 0.045f;
            rat.UltPose = P(front: 1.5f, farFront: 1.4f, back: Mathf.Sin(T * 24 + index) * 0.25f,
                farBack: -Mathf.Sin(T * 24 + index) * 0.25f, head: -0.3f, tail: Mathf.Sin(T * 22) * 0.5f);
            if (p.cuffs != null)
            {
                p.cuffs.x = rat.x + rat.face * rat.Radius * 0.4f; p.cuffs.y = rat.y;
                p.cuffs.z = Mathf.Clamp(rat.Radius * 1.15f, 18, 42); p.cuffs.rot = rat.UltRot;
                p.cuffs.flat = 1 + Mathf.Sin(Mathf.Clamp01((T - p.caughtAt) / 0.2f) * Mathf.PI) * 0.45f;
            }
        }

        Vector2 TapeCorner(int i) => i switch
        {
            0 => new Vector2(X0 - 225, castleY - 65), 1 => new Vector2(X0 + 225, castleY - 65),
            2 => new Vector2(X0 + 225, Y0 + 125), _ => new Vector2(X0 - 225, Y0 + 125)
        };

        void StepTape()
        {
            for (int i = 0; i < tape.Length; i++)
            {
                var p = tape[i]; if (p == null) continue;
                float draw = Ease(Mathf.Clamp01((T - 2.5f - i * 0.19f) / 0.4f));
                var a = TapeCorner(i); var b = Vector2.Lerp(a, TapeCorner((i + 1) % 4), draw);
                p.x = (a.x + b.x) * 0.5f; p.y = (a.y + b.y) * 0.5f; p.z = 36;
                float dx = b.x - a.x, dy = -(b.y - a.y) * World.TILT;
                p.w = Mathf.Max(1, Mathf.Sqrt(dx * dx + dy * dy)); p.rot = Mathf.Atan2(dy, dx);
                if (p.r && p.r.sprite) p.flat = 12 * p.r.sprite.bounds.size.x / (p.w * Mathf.Max(0.001f, p.r.sprite.bounds.size.y));
                p.visible = draw > 0; p.sortBias = i == 2 ? 120 : -5;
                float age = Mathf.Max(0, T - BOOM);
                if (exploded) { p.x += (i % 2 == 0 ? -1 : 1) * age * 190; p.z += age * 110; p.rot += age * (i % 2 == 0 ? 3 : -3); }
                p.alpha = Mathf.Clamp01((Dur - T) / 0.35f);
            }
        }

        // 가장자리 띠는 플래시 이미지 자식으로 붙여 카메라 확대와 무관하게 유지한다.
        void MakeEdgeLights()
        {
            Transform parent = M.flash ? M.flash.transform : M.captionSmall && M.captionSmall.canvas ? M.captionSmall.canvas.transform : null;
            if (!parent) return;
            edgeRoot = new GameObject("UltArrestEdges", typeof(RectTransform));
            var root = (RectTransform)edgeRoot.transform; root.SetParent(parent, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            for (int side = 0; side < 4; side++) for (int band = 0; band < 5; band++)
            {
                var go = new GameObject("SirenEdge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var light = go.GetComponent<Image>(); light.raycastTarget = false; light.color = Color.clear;
                var rt = light.rectTransform; rt.SetParent(root, false);
                float lo = band * 0.016f, hi = (band + 1) * 0.016f;
                rt.anchorMin = side switch { 0 => new Vector2(lo, 0), 1 => new Vector2(1 - hi, 0), 2 => new Vector2(0, lo), _ => new Vector2(0, 1 - hi) };
                rt.anchorMax = side switch { 0 => new Vector2(hi, 1), 1 => new Vector2(1 - lo, 1), 2 => new Vector2(1, hi), _ => new Vector2(1, 1 - lo) };
                rt.offsetMin = rt.offsetMax = Vector2.zero; edgeLights.Add(light);
            }
        }

        void StepSirens(float dt)
        {
            float fade = Mathf.Clamp01((T - 0.8f) / 0.3f) * Mathf.Clamp01((Dur - T) / 0.35f);
            bool red = Mathf.FloorToInt(T * 7) % 2 == 0;
            for (int i = 0; i < edgeLights.Count; i++)
            {
                var light = edgeLights[i]; if (!light) continue;
                Color c = ((i / 5) % 2 == 0) == red ? Red : Blue;
                c.a = fade * (0.24f - i % 5 * 0.043f); light.color = c;
            }
            for (int i = 0; i < sirens.Length; i++)
            {
                var p = sirens[i]; if (p == null) continue;
                var corner = TapeCorner(i); p.x = corner.x; p.y = corner.y; p.z = GroundLift(p);
                p.visible = fade > 0; p.alpha = fade; p.tint = Color.Lerp(Color.white, (i % 2 == 0) == red ? Red : Blue, 0.5f);
            }
            if (T >= RAID && !exploded && (sirenT -= dt) <= 0)
            {
                sirenT = 0.28f;
                for (int i = 0; i < 4; i++) { var pos = TapeCorner(i); Fx?.Ring(pos.x, pos.y, 52, (i % 2 == 0) == red ? Red : Blue, 0.25f); }
            }
        }

        static float GroundLift(UltProp p) => p.r && p.r.sprite
            ? -p.r.sprite.bounds.min.y * p.w * p.flat / Mathf.Max(0.001f, p.r.sprite.bounds.size.x) : 0;

        public override void Cleanup()
        {
            foreach (var l in lawyers) if (l.text) Object.Destroy(l.text.gameObject);
            if (edgeRoot) Object.Destroy(edgeRoot); edgeRoot = null; edgeLights.Clear();
            // 취소되더라도 수갑·정지 상태·잡고 있던 짐까지 해제. 매니저 재호출도 안전하다.
            ReleaseAll();
            police.Clear(); prisoners.Clear(); lawyers.Clear(); smoke.Clear(); reserved.Clear(); castle = null;
            if (R) { R.UltJit = 0; R.UltPose = null; }
        }
    }
}
