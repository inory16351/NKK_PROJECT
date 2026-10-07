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
            public float x, y, z, age, width, spin, rotation, glitterT;
            public bool fell;
            public Color color;
            public UltProp burst, core, smoke;
        }
        readonly List<Rocket> rockets = new();
        readonly List<Flower> flowers = new();
        static readonly string[] Bursts = { "fw_gold", "fw_pink", "fw_blue", "fw_ring", "fw_mouse" };
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
                float age = Mathf.Clamp01(f.age / 0.9f);
                float pop = f.age < 0.16f ? Mathf.Lerp(0.2f, 1.1f, Ease(f.age / 0.16f))
                    : Mathf.Lerp(1.1f, 0.94f, (f.age - 0.16f) / 0.74f);
                if (f.burst != null)
                {
                    f.burst.w = f.width * pop; f.burst.alpha = 1 - age * age;
                    f.burst.rot = f.rotation + f.age * f.spin;
                }
                if (f.core != null)
                {
                    f.core.w = f.width * 0.42f * pop; f.core.alpha = Mathf.Clamp01(1 - f.age / 0.24f);
                    f.core.rot = -f.age * f.spin;
                }
                if (f.smoke != null)
                {
                    f.smoke.w = f.width * (0.17f + age * 0.28f);
                    f.smoke.z = f.z - 18 + age * 24; f.smoke.x = f.x + age * 20;
                    f.smoke.alpha = Mathf.Sin(age * Mathf.PI) * 0.32f;
                }
                // 잔광의 발생 위치도 아래로 내려가며 중력 파티클로 이어짐.
                if (f.age > 0.12f && (f.glitterT -= dt) <= 0 && OnScreen(f.x, f.y))
                {
                    f.glitterT = 0.1f;
                    float z = Mathf.Max(8, f.z - 180 * age * age);
                    Fx?.Stars(f.x + Rand(-0.4f, 0.4f) * f.width, f.y, z, 3, f.color, Color.white, 15, 50);
                    Fx?.Burst(f.x + Rand(-0.3f, 0.3f) * f.width, f.y, z, 2, f.color, Color.white, 10, 35, 2, 4);
                }
                if (f.age >= 0.9f)
                {
                    KillProp(f.burst); KillProp(f.core); KillProp(f.smoke); flowers.RemoveAt(i);
                }
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
                    Bloom(x, y, z, rocket.color, true);
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
                    Bloom(x, y, z, rocket.color, false);
                    Fx?.Stars(x, y, z, 18, rocket.color, Color.white, 80, 160); Fx?.Shake(0.04f);
                }
                KillProp(rocket.prop); rockets.RemoveAt(i);
            }
        }

        void Bloom(float x, float y, float z, Color color, bool wild)
        {
            var f = new Flower { x = x, y = y, z = z, color = color, fell = wild,
                width = wild ? Rand(110, 155) : Rand(195, 275), spin = Rand(-0.45f, 0.45f), rotation = Rand(-0.2f, 0.2f) };
            f.burst = Prop(Pick(Bursts), x, y, z, f.width * 0.2f);
            f.core = Prop("fw_core", x, y, z, f.width * 0.084f);
            f.smoke = Prop("color_puff", x, y, z - 18, f.width * 0.17f);
            if (f.burst != null) { f.burst.tint = Color.Lerp(Color.white, color, Rand(0.12f, 0.4f)); f.burst.sortBias = 15; }
            if (f.core != null) { f.core.tint = Color.Lerp(Color.white, color, 0.15f); f.core.sortBias = 16; }
            if (f.smoke != null) { f.smoke.tint = color; f.smoke.alpha = 0; f.smoke.sortBias = 14; }
            flowers.Add(f);
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

        public override void Cleanup()
        {
            foreach (var p in new List<UltProp>(Props)) KillProp(p);
            rockets.Clear(); flowers.Clear();
        }
    }
}
