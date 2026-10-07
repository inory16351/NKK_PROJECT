using System.Collections.Generic;
using UnityEngine;

namespace NKK.Ults
{
    // 황제 쥐 · 불꽃놀이 (웹게임 fireworks): 하늘 불꽃과 불똥 비, 가끔 옆으로 발사 / c1~c4 자막, c5 뜨거움 팝업
    public class UltFireworks : UltBase
    {
        public override float Dur => 6.2f;
        class Rocket
        {
            public float sx, sy, tx, ty, age, duration;
            public bool wild;
            public Color color;
            public UltProp prop;
        }
        class Flower
        {
            public float x, y, z, age;
            public bool fell;
            public Color color;
            public readonly List<UltProp> rays = new();
        }
        readonly List<Rocket> rockets = new();
        readonly List<Flower> flowers = new();
        static readonly Color[] Colors = {
            new(0.91f, 0.47f, 0.42f), new(0.94f, 0.78f, 0.47f), new(0.62f, 0.84f, 0.66f),
            new(0.66f, 0.83f, 0.86f), new(0.80f, 0.71f, 0.86f), new(0.95f, 0.72f, 0.69f)
        };

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1")); Beat(1.6f, () => Cap("c2"));
            Beat(2.8f, () => Cap("c3")); Beat(4.3f, () => Cap("c4"));
        }

        public override void Step(float dt, float k)
        {
            R.UltPose = P(tilt: -0.5f, front: 2.8f, farFront: 1.2f, head: -0.4f, tail: 1.3f);
            Walk(dt, 100);
            float night = Mathf.Clamp01(T / 0.3f) * Mathf.Clamp01((Dur - T) / 0.38f);
            Flash(new Color(20 / 255f, 22 / 255f, 52 / 255f), 0.42f * night);
            // 기존 꽃부터 갱신해야 새 폭발의 낙하 타이머가 한 프레임 앞서지 않음.
            for (int i = flowers.Count - 1; i >= 0; i--)
            {
                var f = flowers[i]; f.age += dt;
                if (!f.fell && f.age >= 0.45f)
                {
                    Rain(f);
                }
                float age = Mathf.Clamp01(f.age / 0.9f), rad = 120 * Ease(Mathf.Min(1, age * 1.6f));
                for (int j = 0; j < f.rays.Count; j++)
                {
                    var p = f.rays[j]; if (p == null) continue;
                    float a = j / 20f * Mathf.PI * 2;
                    p.x = f.x + Mathf.Cos(a) * rad; p.z = f.z + Mathf.Sin(a) * rad;
                    p.w = 12 * (1 - age) + 2; p.alpha = 1 - age; p.rot = -age * 4;
                }
                if (f.age >= 0.9f) { foreach (var p in f.rays) KillProp(p); flowers.RemoveAt(i); }
            }
            if ((hitT -= dt) <= 0 && T < 5.2f)
            {
                hitT = 0.16f;
                bool wild = Random.value < 0.2f;
                var target = wild ? null : Pick(ItemsIn(R.x, R.y, ULT_R));
                float a = Rand(0, Mathf.PI * 2);
                var rocket = new Rocket { sx = R.x, sy = R.y, tx = target ? target.x : R.x + Mathf.Cos(a) * 320,
                    ty = target ? target.y : R.y + Mathf.Sin(a) * 220, wild = wild, duration = wild ? 0.5f : 0.65f, color = Pick(Colors) };
                rocket.prop = Prop("firework", R.x, R.y, 24, 26);
                rockets.Add(rocket);
                if (OnScreen(R.x, R.y)) Fx?.Dust(R.x, R.y, 2, 0.7f);
            }
            for (int i = rockets.Count - 1; i >= 0; i--)
            {
                var rocket = rockets[i]; rocket.age += dt;
                float e = Mathf.Clamp01(rocket.age / rocket.duration);
                float x = Mathf.Lerp(rocket.sx, rocket.tx, e), y = Mathf.Lerp(rocket.sy, rocket.ty, e);
                float z = rocket.wild ? 22 + Mathf.Sin(e * 25) * 12 : Mathf.Lerp(24, 300, Ease(e));
                if (rocket.prop != null) { rocket.prop.x = x; rocket.prop.y = y; rocket.prop.z = z; }
                if (OnScreen(x, y)) Fx?.Stars(x, y, z, 1, rocket.color, Color.white, 20, 60);
                if (e < 1) continue;
                if (rocket.wild)
                {
                    foreach (var it in ItemsIn(x, y, 90)) FlingItem(it, Rand(0, Mathf.PI * 2), 360, 480);
                    foreach (var rat in RatsNear(x, y, 80)) Ragdoll(rat, Rand(0, Mathf.PI * 2));
                    if (OnScreen(x, y))
                    {
                        Fx?.Stars(x, y, 20, 16, rocket.color, Color.white, 200, 460);
                        Fx?.Ring(x, y, 90, rocket.color, 0.3f); Fx?.Shake(0.06f);
                        PopupCap("c5", x, y, Color.white, 18, 0.7f, 40);
                    }
                }
                else
                {
                    var flower = new Flower { x = x, y = y, z = z, color = rocket.color };
                    for (int j = 0; j < 20; j++)
                    {
                        var p = Prop("star", x, y, z, 14);
                        if (p != null) p.tint = j % 2 == 0 ? Color.white : rocket.color;
                        flower.rays.Add(p);
                    }
                    flowers.Add(flower);
                    Fx?.Stars(x, y, z, 18, rocket.color, Color.white, 80, 160); Fx?.Shake(0.04f);
                }
                KillProp(rocket.prop); rockets.RemoveAt(i);
            }
        }

        public override void Finish()
        {
            // 웹의 later처럼 마지막 불똥도 폭발 0.45초 뒤에 떨어짐.
            foreach (var f in flowers) if (!f.fell)
            {
                var pending = f;
                RatMgr.Later(Mathf.Max(0, 0.45f - f.age), () => { if (R && ItemMgr) Rain(pending); });
            }
        }

        private void Rain(Flower f)
        {
            f.fell = true;
            foreach (var it in ItemsIn(f.x, f.y, 110)) FlingItem(it, Mathf.Atan2(it.y - f.y, it.x - f.x), 300, 460);
            ItemMgr.Aoe(f.x, f.y, 120, UltD * 0.22f, R, false);
            if (OnScreen(f.x, f.y)) { Fx?.Ring(f.x, f.y, 110, f.color, 0.4f); Fx?.Dust(f.x, f.y, 3, 0.7f); }
        }
    }
}
