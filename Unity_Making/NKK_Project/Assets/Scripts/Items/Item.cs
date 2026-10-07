using System.Collections.Generic;
using NKK.Data;
using NKK.Humans;
using NKK.Rats;
using UnityEngine;

namespace NKK.Items
{
    // 연구소 물건·가구 하나 (웹게임 makeItem / damageItem / launch / juggle / updateItems / smashItem 이식). 위치는 게임 단위.
    public class Item : MonoBehaviour
    {
        public enum ItemState { Rest, Fly, Dead, Held }      // Held = 필살기가 붙잡고 있음

        public SpriteRenderer sprite;
        [Tooltip("접지 그림자 (자식, 부드러운 타원)")] public SpriteRenderer shadow;
        [Tooltip("형태 그림자 (자식, 그림을 눌러 비스듬히)")] public ShearShadow castShadow;

        [Header("상태 (실행 중 확인용)")]
        public string codeId;
        public float x, y, z, vx, vy, vz, hp, hpMax, value;
        [HideInInspector] public bool ByAction;     // 마지막으로 때린 게 쥐의 특수 액션(스킬)인지 (치즈 추가 패시브)
        public ItemState State = ItemState.Rest;

        public ItemRow Data { get; private set; }
        public float R { get; private set; }          // 충돌 반지름 (테이블 radius × 1.45)
        public Rat By;                                 // 마지막으로 친 쥐
        public bool Crit => crit;
        public int Air => air;
        ItemManager mgr;
        float pvx, pvy, rot, vr, wob, sq = 1, appear, flashT, kp = 1, seed, juggleCD, hpShowT = -99;
        int air, style; bool crit;
        readonly HashSet<Object> hitSet = new();
        FxManager.HpBar bar;

        public void Init(ItemManager m, ItemRow row, Sprite spr, float px, float py, float hpValue, float cheese, bool instant)
        {
            mgr = m; Data = row; codeId = row.code_id; name = $"Item_{row.code_id}";
            x = px; y = py; R = row.radius * m.radiusScale;
            hpMax = hp = hpValue; value = cheese;
            sprite.sprite = spr; rot = Random.Range(0, Mathf.PI * 2); seed = Random.Range(0f, 100f);
            appear = instant ? 1 : 0;
            if (castShadow) castShadow.Setup(spr, R * 2.6f * World.U);
        }

        void OnDestroy() { if (FxManager.I) FxManager.I.ReleaseHpBar(bar); }

        public void Damage(float dmg, Rat by, bool isCrit, float fromAng)
        {
            if (appear < 0.5f) return;
            float ang = fromAng + Random.Range(-0.35f, 0.35f);
            if (State == ItemState.Fly) { if (by) Juggle(ang, dmg, by, isCrit); return; }
            if (State != ItemState.Rest) return;
            if (by) { By = by; ByAction = by.Acting; }
            kp = by ? by.KnockPower : 1;
            if (Data.IsFurniture) dmg *= CommonSkill.FurnitureDmgMul;      // 이삿짐 센터
            hp -= dmg; wob = 1; sq = 1.25f; flashT = 0.08f; hpShowT = Time.time;
            if (hp > 0)
            {
                // 체력이 남으면 맞은 방향으로 조금 밀림 (무거운 건 덜 밀림)
                float push = Mathf.Clamp(90 + 300 * dmg / hpMax, 90, 260) * kp / (Data.is_big == 1 ? 1.8f : Data.is_sturdy == 1 ? 1.3f : 1) / Mathf.Max(1, Data.heavy);
                pvx += Mathf.Cos(ang) * push; pvy += Mathf.Sin(ang) * push;
                return;
            }
            pvx = pvy = 0;
            if (by) style += by.TrickPoints;      // 묘기 중에 쳐서 날리면 묘기 점수
            Launch(ang, Random.Range(320f, 500f) * (isCrit ? 1.3f : 1) * kp, isCrit);
        }

        float HeavyK => Data.heavy > 0 ? 1 / Mathf.Sqrt(Data.heavy) : 1;

        public void Launch(float ang, float speed, bool isCrit)
        {
            State = ItemState.Fly; Survive = false;
            float hv = HeavyK;
            vx = Mathf.Cos(ang) * speed * 1.4f * hv; vy = Mathf.Sin(ang) * speed * 1.4f * hv;
            vz = Random.Range(440f, 620f) * (Data.is_big == 1 ? 0.75f : 1) * hv; vr = Random.Range(12f, 22f) * (Random.value < 0.5f ? -1 : 1);
            sq = 1.6f; crit = isCrit; flashT = 0.1f; hitSet.Clear();
            var fx = FxManager.I; if (!fx) return;
            fx.Hitstop(Data.is_big == 1 ? 0.07f : 0.02f);
            fx.Anim("hit", x, y, 20, isCrit ? 1.1f : 0.6f);
            fx.Stars(x, y, 20, isCrit ? 8 : 4, Color.white, isCrit ? new Color(0.95f, 0.76f, 0.31f) : new Color(1, 0.95f, 0.75f), 150, 380);
            if (isCrit) fx.Popup(x, y, "크리티컬!", new Color(0.95f, 0.76f, 0.31f), 22, 0.8f, 50);
        }

        // 공중에 뜬 물건을 또 치면: 저글링 (더 높이, 보너스 치즈)
        public void Juggle(float ang, float dmg, Rat by, bool isCrit)
        {
            if (juggleCD > Time.time || z > 90 || air >= mgr.airMax) return;
            juggleCD = Time.time + 0.18f;
            air++; By = by;
            float bonus = value * 0.15f * air * CommonSkill.AirBonusMul;     // 헤딩 저글링
            mgr.Game.Earn(bonus);
            float hv = HeavyK, sp = Random.Range(300f, 440f) * Mathf.Clamp(Mathf.Sqrt(dmg / hpMax), 0.75f, 2.2f) * (isCrit ? 1.3f : 1) * hv;
            vx = Mathf.Cos(ang) * sp * 1.3f; vy = Mathf.Sin(ang) * sp * 1.3f;
            vz = Mathf.Max(vz, 0) * 0.3f + Random.Range(380f, 520f) * hv; vr = Random.Range(-20f, 20f); sq = 1.45f; hitSet.Clear();
            if (isCrit) crit = true;
            var fx = FxManager.I; if (!fx) return;
            fx.Popup(x, y, $"AIR x{air}", new Color(0.61f, 0.96f, 1f), 16 + air * 2, 0.7f, z + 40);
            fx.Stars(x, y, z, 4, Color.white, new Color(0.61f, 0.96f, 1f));
        }

        // 날아간 물건의 타격력 = (무게 + 받은 힘) × 저글링 보너스 × 크리
        float Force() => (By ? By.Damage : mgr.avgRatDamage) * mgr.flyForceMul * Mathf.Clamp(Mathf.Sqrt(vx * vx + vy * vy) / 600, 0.6f, 1.4f) * kp;
        float Style() => (1 + mgr.flyStyle * (Mathf.Min(air, mgr.airMax) + style)) * (crit ? 2 : 1);

        // 황금 물건: 치즈 ×valueMul, 체력 ×hpMul
        public bool Gold { get; private set; }
        public void MakeGold(float valueMul, float hpMul)
        {
            if (Gold) return;
            Gold = true; value *= valueMul; hpMax *= hpMul; hp *= hpMul;
            var fx = FxManager.I; if (fx) { fx.Stars(x, y, 20, 10, new Color(0.95f, 0.76f, 0.31f), new Color(1, 0.95f, 0.75f)); fx.Popup(x, y, "황금!", new Color(0.95f, 0.76f, 0.31f), 18, 0.8f, 40); }
        }
        public float FlyDamage(bool noWeight = false) => ((noWeight ? 0 : hpMax * mgr.flyWeight) + Force()) * Style();

        public void Tick(float dt)
        {
            sq += (1 - sq) * Mathf.Min(1, dt * 10);
            flashT = Mathf.Max(0, flashT - dt);
            if (State == ItemState.Rest)
            {
                wob = Mathf.MoveTowards(wob, 0, dt * 3);
                if (appear < 1) appear = Mathf.Min(1, appear + dt * 4);
                if (pvx != 0 || pvy != 0)
                {
                    float ox = x, oy = y;
                    x += pvx * dt; y += pvy * dt;
                    mgr.Stage.Confine(ref x, ref y, ref pvx, ref pvy, R * 0.8f, ox, oy, 0.4f);
                    float f = Mathf.Max(0, 1 - 6 * dt); pvx *= f; pvy *= f;
                    if (Mathf.Abs(pvx) + Mathf.Abs(pvy) < 4) pvx = pvy = 0;
                }
                return;
            }
            if (State != ItemState.Fly) return;
            float px = x, py = y;
            vz -= World.GZ * dt; x += vx * dt; y += vy * dt; z += vz * dt; rot += vr * dt;
            // 날아간 물건이 막힌 벽을 때리면 벽도 깎임 (쥐가 준 힘만, 무게 제외)
            mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R * 0.7f, px, py, 0.6f, (i, j, di, dj, v) =>
            {
                if (z < 140 && v > 100) { mgr.Stage.DamageWall(i, j, di, dj, FlyDamage(true) * 0.3f, By); FxManager.I?.Dust(x, y, 3, 0.8f); }
            });
            float sp = Mathf.Sqrt(vx * vx + vy * vy);
            // 날아가다 다른 물건을 맞히면 그 물건도 피해 (연쇄)
            if (sp > 150 && z < 60)
                foreach (var o in mgr.InRange(x, y, R + 80))
                {
                    if (o == this || o.State != ItemState.Rest || hitSet.Contains(o)) continue;
                    if (Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) > o.R + R) continue;
                    hitSet.Add(o);
                    mgr.Game.Earn(o.value * 0.3f);
                    if (By) o.By = By;
                    o.Damage(FlyDamage(), null, false, Mathf.Atan2(vy, vx));
                    vx *= 0.8f; vy *= 0.8f;
                }
            // 낮게 날아오면 쥐가 헤딩! 쥐는 통 튀어오르고 물건은 다시 공중으로
            if (z < 50 && juggleCD <= Time.time)
                foreach (var r in mgr.Rats.Rats)
                {
                    if (hitSet.Contains(r) || Vector2.Distance(new Vector2(r.x, r.y), new Vector2(x, y)) > R + r.Radius) continue;
                    hitSet.Add(r); r.Header();
                    Juggle(Mathf.Atan2(y - r.y, x - r.x) + Random.Range(-0.6f, 0.6f), r.Damage, r, false);
                    break;
                }
            // 사람에게 맞음
            foreach (var h in mgr.Humans)
            {
                if (hitSet.Contains(h) || h.State == Human.HState.Fly || h.State == Human.HState.Splat || h.State == Human.HState.Dead) continue;
                if (Vector2.Distance(new Vector2(h.x, h.y), new Vector2(x, y)) > R + h.R || z > mgr.humanHeight) continue;
                hitSet.Add(h);
                h.HitByItem(FlyDamage() * 2, By, Mathf.Atan2(vy, vx));
                vx *= -0.3f; vy *= -0.3f;
                FxManager.I?.Burst(x, y, z, 6, Color.white, new Color(1, 0.89f, 0.6f), 120, 320);
            }
            // 고양이에게 맞음
            var cat = mgr.Cats ? mgr.Cats.Current : null;
            if (cat && !hitSet.Contains(cat) && mgr.Cats.ItemHit(this, FlyDamage())) { hitSet.Add(cat); vx *= -0.3f; vy *= -0.3f; }
            if (z <= 0 && vz < 0) { if (Survive) Land(); else Smash(); }
        }

        // ── 필살기 (웹게임 skillHitItem · grabItem · dropItem) ──
        [HideInInspector] public bool Survive;      // 필살기에 날려졌지만 체력이 남음 → 떨어지면 멀쩡히 착지
        public float Rot { get => rot; set => rot = value; }
        public float Spin { get => vr; set => vr = value; }
        public bool Appeared => appear >= 1;
        public void Squash(float v) => sq = v;
        // 피해만 줌 (날리지 않음). 체력이 0 이하면 true (그 뒤 Fling 하면 떨어질 때 박살)
        public bool SkillHit(float dmg, Rat by)
        {
            if (Data.IsFurniture) dmg *= CommonSkill.FurnitureDmgMul;
            hp -= dmg; hpShowT = Time.time; flashT = 0.08f;
            if (by) { By = by; ByAction = true; }
            Survive = hp > 0;
            return !Survive;
        }
        public void Hold() { State = ItemState.Held; pvx = pvy = 0; wob = 0; }
        // 지금 속도로 날려 보냄 (Survive 면 착지, 아니면 박살)
        public void Fling(float fvx, float fvy, float fvz)
        {
            State = ItemState.Fly; vx = fvx; vy = fvy; vz = fvz; pvx = pvy = 0;
            vr = Random.Range(10f, 22f) * (Random.value < 0.5f ? -1 : 1); sq = 1.4f; hitSet.Clear(); air = Mathf.Max(1, air);
        }
        void Land()
        {
            State = ItemState.Rest; z = 0; vx = vy = vz = 0; vr = 0; Survive = false; air = 0; style = 0; crit = false; sq = 1.35f; wob = 1;
            FxManager.I?.Dust(x, y, 3, 0.8f);
        }

        // 밀기 (소용돌이 등)
        public void AddPush(float dx, float dy) { pvx += dx; pvy += dy; wob = 0.6f; }

        // 쥐가 들어서 던짐 (특수 액션 Throw_Item): kp = 충돌 위력 배율
        public void ThrowBy(Rat by, float ang, float speed, float upV, float power)
        {
            By = by; kp = power; State = ItemState.Fly; Survive = false; hitSet.Clear(); air = 1;
            vx = Mathf.Cos(ang) * speed; vy = Mathf.Sin(ang) * speed; vz = upV; vr = Random.Range(14f, 24f);
        }

        // 공중 물건끼리 부딪힘 (ItemManager 가 짝지어 부름)
        public bool TryCollide(Item b)
        {
            if (hitSet.Contains(b) || Mathf.Abs(z - b.z) > 35 || Vector2.Distance(new Vector2(x, y), new Vector2(b.x, b.y)) > R + b.R) return false;
            hitSet.Add(b); b.hitSet.Add(this);
            float tvx = vx, tvy = vy; vx = b.vx * 0.9f; vy = b.vy * 0.9f; b.vx = tvx * 0.9f; b.vy = tvy * 0.9f;
            vz += 160; b.vz += 160; air++; b.air++;
            mgr.Game.Earn((value + b.value) * 0.5f * CommonSkill.AirCollideCheeseMul);     // 묵직한 한 방
            FxManager.I?.Popup((x + b.x) / 2, (y + b.y) / 2, "쾅!", new Color(1, 0.95f, 0.75f), 22, 0.6f, z + 30);
            FxManager.I?.Stars((x + b.x) / 2, (y + b.y) / 2, z, 6, Color.white, new Color(1, 0.89f, 0.6f));
            return true;
        }

        // 지금 자리(공중 포함)에서 바로 박살 (슈퍼 점프: 웹 skillBlastItem → smashItem)
        public void SmashNow() { if (State == ItemState.Dead) return; State = ItemState.Fly; air = Mathf.Max(air, 2); crit = true; Smash(); }

        void Smash()
        {
            State = ItemState.Dead;
            mgr.OnSmashed(this);
        }

        void LateUpdate()
        {
            if (!mgr || State == ItemState.Dead) return;
            transform.position = World.ToUnity(x, y, z) + new Vector3(0, -R * 0.35f * World.U, 0);
            // 서 있는 스프라이트: 폭 = R × 2.6, 바닥 = 이미지 아래쪽
            float s = appear < 1 ? EaseOutBack(appear) : 1;
            var spr = sprite.sprite;
            float w = R * 2.6f * s * World.U, k = spr ? w / spr.bounds.size.x : 1, h = spr ? spr.bounds.size.y * k : 0;
            var tr = sprite.transform;
            if (State == ItemState.Rest)
            {
                tr.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(Time.time * 30 + seed) * 0.12f * wob * Mathf.Rad2Deg);
                tr.localScale = new Vector3(k * sq, k / sq, 1);
            }
            else
            {
                tr.localRotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
                tr.localScale = new Vector3(k, k, 1);
            }
            tr.localPosition = new Vector3(0, h / 2, 0);
            sprite.sortingOrder = World.SortOrder(y);
            sprite.color = flashT > 0 ? new Color(1, 0.75f, 0.7f) : Gold ? new Color(1f, 0.86f, 0.45f) : Color.white;

            Vector3 ground = World.ToUnity(x, y) + new Vector3(0, -R * 0.35f * World.U, 0);
            if (shadow)
            {
                // 접지 그림자: 그림 폭에 맞춘 납작한 타원, 높이 뜰수록 작고 옅게
                float sk = (1 - Mathf.Min(0.6f, z / 400)) * s, sw = shadow.sprite ? shadow.sprite.bounds.size.x : 1;
                float hw = R * mgr.contactShadowWidth * sk;
                shadow.transform.position = ground + new Vector3(0, R * 0.12f * World.U, 0);
                shadow.transform.localScale = new Vector3(hw * 2 * World.U / sw, hw * 2 * mgr.contactShadowFlat * World.U / sw, 1);
                var c = shadow.color; c.a = mgr.contactShadowAlpha * (1 - Mathf.Min(0.7f, z / 300)); shadow.color = c;
            }
            if (castShadow)
            {
                bool on = State == ItemState.Rest;        // 날아가면 접지 그림자만
                castShadow.gameObject.SetActive(on);
                if (on) { castShadow.transform.position = ground; castShadow.transform.localScale = new Vector3(s * sq, s / sq, 1); }
            }
            // 체력바: 맞은 뒤 잠깐 (바닥에 있고 다쳤을 때)
            var fx = FxManager.I;
            if (fx)
            {
                float age = Time.time - hpShowT;
                bool show = State == ItemState.Rest && hp < hpMax && age < fx.hpBarShowTime;
                if (show) { bar ??= fx.GetHpBar(); fx.ShowHpBar(bar, x, y - R * 0.35f, z + h / World.U + 8, mgr.hpBarWidth, Mathf.Clamp01(hp / hpMax), Mathf.Clamp01((fx.hpBarShowTime - age) * 2)); }
                else if (bar != null) { fx.ReleaseHpBar(bar); bar = null; }
            }
        }

        static float EaseOutBack(float v) { const float c1 = 1.9f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(v - 1, 3) + c1 * Mathf.Pow(v - 1, 2); }
    }
}
