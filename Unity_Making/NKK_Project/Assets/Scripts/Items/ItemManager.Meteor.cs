using System.Collections.Generic;
using UnityEngine;

namespace NKK.Items
{
    // 운석 연출 (메테오 액션 · 치즈 운석 · 공룡 쥐 필살기 운석 비): 비스듬히 떨어지는 불붙은 운석 + 낙하 지점 표시 + 불씨
    // → 착지: 폭발 그림 · 크레이터 자국 · 연기 · 튀는 파편. 그림은 UnityResources/Rats/FX_Meteor (Codex 시트에서 자름)
    public partial class ItemManager
    {
        [Header("운석 이펙트 (FX_Meteor)")]
        [Tooltip("불꼬리 달린 운석 (기본 운석일 때)")] public Sprite meteorFireSprite;
        [Tooltip("불꼬리만 (다른 그림 운석 뒤에 붙임)")] public Sprite meteorFlameSprite;
        public Sprite meteorBurstSprite, meteorCraterSprite, meteorSmokeSprite, meteorDebrisSprite, meteorTargetSprite;
        [Tooltip("떨어지는 시간 (초)")] public float meteorFallTime = 0.9f;
        [Tooltip("기울기: 높이 1 당 옆으로 이동")] public float meteorSlant = 0.45f;
        [Tooltip("운석 그림 폭 (게임 단위, 크기 배율 1)")] public float meteorWidth = 90;
        [Tooltip("크레이터 자국이 남는 시간 (초)")] public float craterLife = 4;
        [Tooltip("파편 수 · 연기 수")] public int debrisCount = 6, smokeCount = 3;
        public Color emberColor0 = new(1f, 0.62f, 0.25f), emberColor1 = new(1f, 0.9f, 0.55f);

        public class MeteorView
        {
            public SpriteRenderer body, flame, target; public float size, T, t, spin; public bool fire;
            public void Destroy() { foreach (var r in new[] { body, flame, target }) if (r) Object.Destroy(r.gameObject); }
        }

        SpriteRenderer NewSprite(string name, Sprite s)
        {
            var go = new GameObject(name); go.transform.SetParent(bombTemplate ? bombTemplate.transform.parent : transform, false);
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = s;
            if (bombTemplate) { r.sortingLayerID = bombTemplate.sortingLayerID; r.sharedMaterial = bombTemplate.sharedMaterial; }
            return r;
        }
        static void SetWidth(SpriteRenderer r, float w, float flat = 1)
        {
            if (!r || !r.sprite) return;
            float k = w * World.U / r.sprite.bounds.size.x;
            r.transform.localScale = new Vector3(k, k * flat, 1);
        }

        void SetupMeteor(Bomb b, Sprite spr, float size, float T)
        {
            float H = b.z;
            b.x += H * meteorSlant; b.vx = -H * meteorSlant / T; b.vy = 0; b.vz = -H / T;
            var v = new MeteorView { size = size, T = T, spin = Random.Range(-260f, -160f) };
            // 기본 운석(이 그림이 없거나 기본 그림)이면 불꼬리 운석 한 장, 아니면 그 그림 + 불꼬리
            bool plain = !spr || (Rats && spr == Rats.defaultMeteor);
            v.fire = plain && meteorFireSprite;
            v.body = NewSprite("Meteor", v.fire ? meteorFireSprite : spr ? spr : meteorFireSprite);
            SetWidth(v.body, meteorWidth * size * (v.fire ? 1.6f : 1));
            if (!v.fire && meteorFlameSprite) { v.flame = NewSprite("MeteorFlame", meteorFlameSprite); SetWidth(v.flame, meteorWidth * size * 0.9f); }
            if (meteorTargetSprite) { v.target = NewSprite("MeteorTarget", meteorTargetSprite); v.target.color = new Color(1, 1, 1, 0); }
            b.mv = v;
            TickMeteor(b, 0);
        }

        void TickMeteor(Bomb b, float dt)
        {
            var v = b.mv; if (v == null) return;
            v.t += dt;
            float k = Mathf.Clamp01(v.t / v.T);
            int so = World.SortOrder(b.y) + 40;
            if (v.body)
            {
                v.body.transform.position = World.ToUnity(b.x, b.y, b.z);
                v.body.sortingOrder = so;
                if (!v.fire) v.body.transform.Rotate(0, 0, v.spin * dt);
                else
                {
                    // 그림의 불꼬리는 오른쪽 위 45° → 실제 진행 반대 방향에 맞춤 (그림 기준점 = 운석 돌, 임포트 피벗)
                    Vector3 vel = World.ToUnity(b.vx, b.vy, b.vz) - World.ToUnity(0, 0, 0);
                    v.body.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(-vel.y, -vel.x) * Mathf.Rad2Deg - 45);
                }
            }
            if (v.flame)
            {
                // 불꼬리: 운동 반대 방향 (화면상 위·오른쪽)으로
                Vector3 vel = World.ToUnity(b.vx, b.vy, b.vz) - World.ToUnity(0, 0, 0);
                float ang = Mathf.Atan2(-vel.y, -vel.x) * Mathf.Rad2Deg - 90;
                v.flame.transform.position = World.ToUnity(b.x, b.y, b.z);
                v.flame.transform.rotation = Quaternion.Euler(0, 0, ang);
                v.flame.sortingOrder = so - 1;
                float w = meteorWidth * v.size * 0.9f * (1 + 0.08f * Mathf.Sin(v.t * 40));
                SetWidth(v.flame, w);
            }
            if (v.target)
            {
                // 낙하 지점: 점점 진해지고 줄어들며 깜빡
                float w = b.rad * 1.5f * Mathf.Lerp(1.2f, 0.85f, k);
                v.target.transform.position = World.ToUnity(b.x + b.vx * Mathf.Max(0, v.T - v.t), b.y, 0);
                SetWidth(v.target, w, World.TILT);
                v.target.color = new Color(1, 1, 1, Mathf.Clamp01(k * 3) * (0.3f + 0.3f * Mathf.Abs(Mathf.Sin(v.t * (8 + 10 * k)))));
                v.target.sortingOrder = World.SortOrder(b.y - 40);
            }
            // 불씨 (화면 밖이면 생략)
            var fx = FxManager.I;
            if (fx && dt > 0 && Random.value < 0.7f) fx.Burst(b.x + Random.Range(-10f, 10f), b.y, b.z + 10, 1, emberColor0, emberColor1, 20, 90, 3, 7);
        }

        void MeteorImpact(Bomb b)
        {
            var fx = FxManager.I; var v = b.mv;
            float s = v != null ? v.size : 1, R = b.rad;
            if (fx)
            {
                fx.Ring(b.x, b.y, R, new Color(1f, 0.7f, 0.35f, 0.95f), 0.45f); fx.Ring(b.x, b.y, R * 0.55f, Color.white, 0.3f);
                fx.Burst(b.x, b.y, 14, 18, emberColor0, emberColor1, 220, 560);
                fx.Dust(b.x, b.y, 10, 1.6f); fx.Anim("explosion", b.x, b.y, 0, R / 70);
                fx.Shake(0.1f + 0.05f * s); fx.Hitstop(0.035f);
            }
            // 폭발 그림: 확 커졌다가 사라짐
            if (meteorBurstSprite) AddDeco(meteorBurstSprite, b.x, b.y, R * 0.4f, 0, 0, 0, 0.35f, R * 0.9f, R * 2.6f, 0, 0, false, 30);
            // 크레이터 자국 (바닥, 천천히 옅어짐)
            if (meteorCraterSprite) AddDeco(meteorCraterSprite, b.x, b.y, 0, 0, 0, 0, craterLife, R * 1.2f, R * 1.25f, 0, 0, true, -30000);
            // 연기: 둥실 떠오르며 커짐
            if (meteorSmokeSprite) for (int i = 0; i < smokeCount; i++)
                AddDeco(meteorSmokeSprite, b.x + Random.Range(-R, R) * 0.5f, b.y + Random.Range(-R, R) * 0.3f, 10, Random.Range(-30f, 30f), 0, Random.Range(50f, 90f), Random.Range(0.9f, 1.4f), R * 0.7f, R * 1.4f, Random.Range(-30f, 30f), 0, false, 25);
            // 파편: 사방으로 튀었다가 떨어짐
            if (meteorDebrisSprite) for (int i = 0; i < debrisCount; i++)
            {
                float a = Random.Range(0, Mathf.PI * 2), sp = Random.Range(180f, 380f);
                AddDeco(meteorDebrisSprite, b.x, b.y, 10, Mathf.Cos(a) * sp, Mathf.Sin(a) * sp, Random.Range(380f, 620f), Random.Range(0.7f, 1f), R * 0.22f, R * 0.22f, Random.Range(-600f, 600f), 1500, false, 26);
            }
        }

        // 바닥 자국 하나 (슈퍼 점프 착지 등): 폭 w, life 초 동안 남음
        public void Crater(float x, float y, float w, float life) { if (meteorCraterSprite) AddDeco(meteorCraterSprite, x, y, 0, 0, 0, 0, life, w, w, 0, 0, true, -30000); }

        // ── 잠깐 떠 있는 그림 (폭발·연기·파편·크레이터) ──
        class Deco { public SpriteRenderer r; public float x, y, z, vx, vy, vz, life, max, w0, w1, spin, grav; public bool ground; public int order; }
        readonly List<Deco> decos = new();
        void AddDeco(Sprite s, float x, float y, float z, float vx, float vy, float vz, float life, float w0, float w1, float spin, float grav, bool ground, int order)
        {
            if (decos.Count >= 120) { var o = decos[0]; if (o.r) Destroy(o.r.gameObject); decos.RemoveAt(0); }
            var r = NewSprite("MeteorFx", s);
            r.transform.rotation = Quaternion.Euler(0, 0, ground ? 0 : Random.Range(0, 360f));
            if (ground && FxManager.I && FxManager.I.spillTemplate) { r.sortingLayerID = FxManager.I.spillTemplate.sortingLayerID; r.sharedMaterial = FxManager.I.spillTemplate.sharedMaterial; }
            decos.Add(new Deco { r = r, x = x, y = y, z = z, vx = vx, vy = vy, vz = vz, life = life, max = life, w0 = w0, w1 = w1, spin = spin, grav = grav, ground = ground, order = order });
        }
        void UpdateDecos(float dt)
        {
            for (int i = decos.Count - 1; i >= 0; i--)
            {
                var d = decos[i];
                d.life -= dt;
                if (d.life <= 0 || !d.r) { if (d.r) Destroy(d.r.gameObject); decos.RemoveAt(i); continue; }
                d.x += d.vx * dt; d.y += d.vy * dt; d.vz -= d.grav * dt; d.z = Mathf.Max(0, d.z + d.vz * dt);
                if (d.grav > 0 && d.z <= 0) { d.vx *= 0.5f; d.vy *= 0.5f; d.vz = 0; d.spin *= 0.5f; }
                float k = 1 - d.life / d.max;
                d.r.transform.position = World.ToUnity(d.x, d.y, d.z);
                if (d.spin != 0) d.r.transform.Rotate(0, 0, d.spin * dt);
                SetWidth(d.r, Mathf.Lerp(d.w0, d.w1, 1 - (1 - k) * (1 - k)), d.ground ? World.TILT : 1);
                d.r.sortingOrder = d.ground ? (FxManager.I && FxManager.I.spillTemplate ? FxManager.I.spillTemplate.sortingOrder + 1 : d.order) : World.SortOrder(d.y) + d.order;
                float a = d.ground ? Mathf.Clamp01(d.life / 1.5f) * 0.85f : Mathf.Clamp01(d.life / (d.max * 0.5f));
                var c = d.r.color; c.a = a; d.r.color = c;
            }
        }
        void ClearDecos() { foreach (var d in decos) if (d.r) Destroy(d.r.gameObject); decos.Clear(); }
    }
}
