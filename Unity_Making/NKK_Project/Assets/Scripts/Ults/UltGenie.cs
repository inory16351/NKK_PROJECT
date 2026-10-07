using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 술탄 햄스터 · 램프의 지니 (웹게임 genie): 확대 → 실제 물건 복제 → 소원 곡해 폭발 / c1~c6 자막, c7 복제 팝업
    public class UltGenie : UltBase
    {
        public override float Dur => 6.2f;
        class Victim
        {
            public Item item;
            public UltProp enlarged;
            public bool spark;
            public float scale = 1;
        }
        readonly List<Victim> victims = new();
        readonly List<Item> sources = new();
        UltProp genie, lamp, halo;
        int copies;
        bool poof, sourceReady, boom;
        static readonly Color Purple = new(0.80f, 0.71f, 0.86f);

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1")); Beat(1, () => Cap("c2"));
            Beat(1.6f, () => Cap("c3")); Beat(3.1f, () => Cap("c4"));
            Beat(4.6f, () => Cap("c5")); Beat(5.1f, () => Cap("c6"));
            foreach (var it in ItemsIn(R.x, R.y, ULT_R))
            {
                if (victims.Count == 26) break;
                victims.Add(new Victim { item = it });
            }
            genie = Prop("genie", R.x + R.face * 70, R.y, 40, 0);
            lamp = Prop("lamp", R.x - R.face * 14, R.y, 30, 32);
            halo = Prop("puff", R.x + R.face * 70, R.y, 140, 0);
            if (halo != null) { halo.tint = Purple; halo.alpha = 0.35f; halo.sortBias = -5; }
        }

        public override void Step(float dt, float k)
        {
            float gx = R.x + R.face * 70, gy = R.y;
            R.UltPose = T < 0.9f ? P(front: 1 + Mathf.Sin(T * 30) * 0.35f, farFront: 0.9f, head: 0.3f) : PoseUp();
            float growth = Mathf.Clamp01((T - 0.5f) / 0.7f) * (T > 5.1f ? 1 + Mathf.Min(0.5f, (T - 5.1f) * 1.2f) : 1);
            float bob = Mathf.Sin(T * 3) * 8;
            if (genie != null) { genie.x = gx; genie.y = gy; genie.z = 40 - bob + 115 * growth; genie.w = 230 * growth; genie.visible = growth > 0; }
            if (lamp != null) { lamp.x = R.x - R.face * 14; lamp.y = R.y; lamp.flip = R.face < 0; }
            if (halo != null) { halo.x = gx; halo.y = gy; halo.z = 138 * growth - bob; halo.w = 368 * growth; }
            if ((hitT -= dt) <= 0)
            {
                hitT = 0.06f;
                if (T > 0.3f && T < 1.3f) Fx?.Burst(R.x - R.face * 14, R.y, 30, 3, Purple, new Color(0.75f, 0.80f, 0.90f), 80, 180, 10, 18);
                if (growth > 0)
                    Fx?.Beam(R.x - R.face * 14, R.y, 30, gx, gy, 60 - bob, new Color(0.75f, 0.80f, 0.90f, 0.55f), 0.09f);
            }
            if (T > 0.5f && !poof) { poof = true; for (int i = 0; i < 3; i++) Smoke(gx, gy); }
            if (T >= 1.6f && T < 2.4f)
            {
                foreach (var v in victims) if (v.item && v.item.State == Item.ItemState.Rest)
                {
                    v.scale = Mathf.Lerp(1, 1.7f, Ease(Mathf.Clamp01((T - 1.6f) / 0.7f)));
                    if (!v.spark)
                    {
                        v.spark = true; v.enlarged = ItemPicture(v.item);
                        if (OnScreen(v.item.x, v.item.y))
                        {
                            Fx?.Stars(v.item.x, v.item.y, 30, 8, Purple, Color.white, 80, 220);
                            Fx?.Ring(v.item.x, v.item.y, v.item.R * 2, Purple, 0.4f);
                        }
                    }
                }
            }
            if (T >= 3.1f && T < 4.6f)
            {
                if (!sourceReady)
                {
                    sourceReady = true;
                    foreach (var v in victims) if (v.item && v.item.State == Item.ItemState.Rest && sources.Count < 16) sources.Add(v.item);
                }
                while (copies < 16 && copies <= (T - 3.1f) / 0.08f)
                {
                    int index = copies++;
                    if (index >= sources.Count) continue;
                    var src = sources[index]; if (!src || src.State != Item.ItemState.Rest) continue;
                    float x = src.x + Rand(-60, 60), y = src.y + Rand(-40, 40), vx = 0, vy = 0;
                    M.Stage.Confine(ref x, ref y, ref vx, ref vy, src.R, src.x, src.y, 0);
                    var copy = ItemMgr.Spawn(src.Data, x, y, false);
                    if (!copy) continue;
                    victims.Add(new Victim { item = copy, scale = 1.7f, spark = true, enlarged = ItemPicture(copy) });
                    if (OnScreen(x, y)) { Smoke(x, y); if (Random.value < 0.35f) PopupCap("c7", x, y, Color.white, 18, 0.6f, 40); }
                    if (copies % 4 == 1) Fx?.Ring(gx, gy, 300, Purple, 0.6f);
                }
            }
            foreach (var v in victims) UpdatePicture(v);
            if (T > 5.4f && !boom)
            {
                boom = true;
                foreach (var v in victims)
                {
                    KillProp(v.enlarged); v.enlarged = null;
                    var it = v.item; if (!it || it.State != Item.ItemState.Rest) continue;
                    if (OnScreen(it.x, it.y)) Fx?.Stars(it.x, it.y, 20, 8, Purple, Color.white, 150, 420);
                    float a = Rand(0, Mathf.PI * 2), weight = it.Data.heavy > 0 ? 1 / Mathf.Sqrt(it.Data.heavy) : 1;
                    if (it.SkillHit(ItemD * 1.5f, R))
                    {
                        // 즉시 파괴도 매니저의 보상·목록 정리 경로를 통함.
                        it.State = Item.ItemState.Dead; ItemMgr.OnSmashed(it);
                    }
                    else it.Fling(Mathf.Cos(a) * 220 * weight, Mathf.Sin(a) * 220 * weight, 320 * weight);
                }
                for (int i = 0; i < 3; i++) Fx?.Ring(gx, gy, 200 + i * 160, i % 2 == 0 ? Purple : Color.white, 0.6f + i * 0.1f);
                Flash(Purple, 0.5f); Fx?.Shake(0.45f);
            }
        }

        private UltProp ItemPicture(Item it)
        {
            // 반지름은 읽기 전용: 원본 스프라이트를 소품으로 겹쳐 시각적 확대만 적용.
            if (!it.sprite || !it.sprite.sprite) return null;
            var p = Prop("dot", it.x, it.y, 0, it.R * 2.6f);
            if (p != null) { p.r.sprite = it.sprite.sprite; p.sortBias = 1; }
            return p;
        }

        private void UpdatePicture(Victim v)
        {
            if (v.enlarged == null) return;
            var it = v.item;
            if (!it || it.State != Item.ItemState.Rest) { KillProp(v.enlarged); v.enlarged = null; return; }
            var p = v.enlarged;
            p.visible = it.Appeared; p.x = it.x; p.y = it.y; p.w = it.R * 2.6f * v.scale;
            float scale = p.w / p.r.sprite.bounds.size.x;
            p.z = it.z - it.R * 0.35f + (p.r.sprite.bounds.extents.y - p.r.sprite.bounds.center.y) * scale;
            p.tint = it.Gold ? new Color(1, 0.86f, 0.45f) : Color.white;
        }
    }
}
