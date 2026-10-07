using System.Collections.Generic;
using NKK.Items;
using TMPro;
using UnityEngine;

namespace NKK.Ults
{
    // 쥐랜드 쥐 · 쥐랜드 성 대개장 (웹게임 lawsuit): 성 등장 → 법무팀 압류 → 물건 발사와 성 철거.
    // 자막 c1~c3 등장, c4 딱지 월드 글자, c5~c7 압류 반응, c8 철거 완료, c9 폭발 팝업.
    public class UltRatlandLawsuit : UltBase
    {
        public override float Dur => 6.5f;
        const int COUNT = 8;
        static readonly Color Red = new(0.88f, 0.24f, 0.25f), Gold = new(1, 0.86f, 0.52f);
        class Lawyer
        {
            public UltProp a, b, tag;
            public TMP_Text text;
            public Item item;
            public float sx, sy, tx, ty, arrival, stamp;
            public bool arrived, launched;
        }
        class Puff { public UltProp p; public float vx, vy, vz, width; }
        readonly List<Lawyer> lawyers = new();
        readonly List<Puff> smoke = new();
        readonly HashSet<Item> reserved = new();
        UltProp castle;
        float castleY, dustT;
        bool exploded;

        public override void Begin()
        {
            Cap("c1");
            Beat(0.85f, () => Cap("c2")); Beat(1.35f, () => Cap("c3"));
            Beat(2.45f, () => Cap("c5")); Beat(3.2f, () => Cap("c6"));
            Beat(4.85f, () => Cap("c7")); Beat(5.55f, () => Cap("c8"));
            castleY = Y0 - 95;
            castle = Prop("ratland_castle", X0, castleY, 0, 360);
            if (castle != null) { castle.flat = 0; castle.sortBias = -20; }
            var view = ViewRect();
            for (int i = 0; i < COUNT; i++)
            {
                var l = new Lawyer { sx = i % 2 == 0 ? view.xMin - 90 : view.xMax + 90,
                    sy = Y0 + (i - 3.5f) * 32, arrival = 2.15f + i * 0.11f };
                l.item = FindItem(l.sx, l.sy);
                l.tx = l.item ? l.item.x : X0 + (i - 3.5f) * 50;
                l.ty = l.item ? l.item.y : Y0 + 60 + i % 2 * 45;
                l.a = Prop("lawyer_rat_a", l.sx, l.sy, 0, 66);
                l.b = Prop("lawyer_rat_b", l.sx, l.sy, 0, 66);
                if (l.a != null) l.a.visible = false;
                if (l.b != null) l.b.visible = false;
                lawyers.Add(l);
            }
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
            R.UltPose = T < 1.3f ? PoseUp() : T < 4.8f ? P(head: -0.35f, front: 0.7f, tail: -0.2f) : PoseFlail();
            R.UltJit = T > 4.85f && !exploded ? 3 : 0;
            if (!exploded && castle != null)
            {
                float rise = Ease(Mathf.Clamp01(T / 1.15f));
                castle.flat = rise;
                castle.x = X0 + (T < 1.15f ? Mathf.Sin(T * 65) * 5 * (1 - rise) : 0);
                castle.z = GroundLift(castle);
            }
            if (T < 1.15f && (dustT -= dt) <= 0)
            {
                dustT = 0.12f; Fx?.Dust(X0 + Rand(-150, 150), castleY, 5, 1.4f); Fx?.Shake(0.035f);
            }
            for (int i = 0; i < lawyers.Count; i++) StepLawyer(lawyers[i], i);
            if (T >= 5.35f && !exploded) Explode();
            foreach (var s in smoke)
            {
                if (s.p == null) continue;
                float age = Mathf.Max(0, T - 5.35f);
                s.p.x += s.vx * dt; s.p.y += s.vy * dt; s.p.z += s.vz * dt;
                s.p.w = s.width * (0.4f + age * 1.4f); s.p.alpha = Mathf.Clamp01(1 - age / 1.15f) * 0.8f;
                s.p.rot += dt * 0.3f;
            }
        }

        void StepLawyer(Lawyer l, int index)
        {
            float start = 1.35f + index * 0.035f;
            if (!l.arrived)
            {
                // 다른 행동이 먼저 물건을 날렸으면 아직 바닥에 있는 물건으로 교체.
                if (!l.item || l.item.State != Item.ItemState.Rest)
                    l.item = FindItem(l.tx, l.ty);
                if (l.item) { l.tx = l.item.x; l.ty = l.item.y; }
            }
            float e = Mathf.Clamp01((T - start) / (l.arrival - start));
            float side = l.sx < l.tx ? -1 : 1;
            float x = Mathf.Lerp(l.sx, l.tx + side * 45, e), y = Mathf.Lerp(l.sy, l.ty + 12, e);
            bool walking = T < l.arrival || T > 4.6f;
            if (T > 4.6f) x += side * (T - 4.6f) * 470;
            bool frame = Mathf.FloorToInt(T * 10) % 2 == 0;
            SetLawyer(l.a, x, y, T >= start && (!walking || frame), walking, T > 4.6f ? side > 0 : side < 0);
            SetLawyer(l.b, x, y, T >= start && walking && !frame, walking, T > 4.6f ? side > 0 : side < 0);
            if (!l.arrived && T >= l.arrival)
            {
                l.arrived = true;
                if (GrabItem(l.item)) Stamp(l);
            }
            if (!l.launched && T >= 3.85f + index * 0.09f)
            {
                l.launched = true;
                if (l.item && Items.Contains(l.item))
                {
                    float a = Mathf.Atan2(l.item.y - castleY, l.item.x - X0);
                    FlingItem(l.item, a, 520 + index * 25, 420);
                    PopupCap("c9", l.item.x, l.item.y, Gold, 20, 0.6f, 45);
                }
            }
            UpdateTag(l);
        }

        void SetLawyer(UltProp p, float x, float y, bool visible, bool walking, bool right)
        {
            if (p == null) return;
            p.x = x; p.y = y; p.flip = right; p.visible = visible;
            p.z = GroundLift(p) + (walking ? Mathf.Abs(Mathf.Sin(T * 31.4f)) * 5 : 0);
            p.rot = walking ? Mathf.Sin(T * 31.4f) * 0.045f : 0;
            p.alpha = Mathf.Clamp01((5.25f - T) / 0.25f);
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
            p.sortBias = 30; p.alpha = Mathf.Clamp01((Dur - T) / 0.3f);
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
            exploded = true; KillProp(castle); castle = null;
            Shock(X0, castleY, ULT_R, UltD * 0.8f, Gold, 3);
            Fx?.Anim("explosion", X0, castleY, 90, 2.5f);
            Fx?.Burst(X0, castleY, 130, 65, new Color(0.6f, 0.45f, 0.55f), Gold, 230, 650, 9, 23);
            Fx?.Stars(X0, castleY, 100, 35, Gold, Color.white, 220, 580);
            Fx?.Ring(X0, castleY, ULT_R * 1.25f, Red, 0.6f);
            Flash(Gold, 0.55f); Fx?.Shake(0.4f); Fx?.Hitstop(0.07f);
            PopupCap("c9", X0, castleY, Color.white, 36, 0.8f, 160);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2 / 9;
                var p = Prop("color_puff", X0 + Mathf.Cos(a) * 75, castleY + Mathf.Sin(a) * 40, 60 + i % 3 * 45, 60);
                if (p != null) { p.tint = i % 2 == 0 ? Col : Gold; p.sortBias = 40; }
                smoke.Add(new Puff { p = p, vx = Mathf.Cos(a) * 120, vy = Mathf.Sin(a) * 65, vz = 45, width = Rand(95, 145) });
            }
        }

        static float GroundLift(UltProp p) => p.r && p.r.sprite
            ? -p.r.sprite.bounds.min.y * p.w * p.flat / Mathf.Max(0.001f, p.r.sprite.bounds.size.x) : 0;

        public override void Cleanup()
        {
            foreach (var l in lawyers) if (l.text) Object.Destroy(l.text.gameObject);
            foreach (var p in new List<UltProp>(Props)) KillProp(p);
            lawyers.Clear(); smoke.Clear(); reserved.Clear(); castle = null;
            if (R) R.UltJit = 0;
        }
    }
}
