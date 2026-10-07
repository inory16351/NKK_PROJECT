using NKK.Data;
using NKK.Items;
using UnityEngine;

namespace NKK.Rats
{
    // 쥐 특수 능력 (패시브). 쥐 테이블 Skill 시트의 특수 능력 행(조건 + effect_type + value_01~06)을 읽어서 동작.
    // 값은 테이블에 이미 등급 스킬 위력이 곱해진 상태 (특수 강화 레벨은 이후 성장 단계에서 곱함).
    public partial class Rat
    {
        public RatSkillRow Passive { get; private set; }
        public EffectType PassiveType { get; private set; }

        [HideInInspector] public float buff;      // 주변 리더의 버프 (공격력·속도 +)
        [HideInInspector] public int packN;       // 주변 동료 수 (떼거리)
        float abT;

        float PV(int i) => Passive != null ? Passive.V(i) : 0;
        bool Is(EffectType e) => PassiveType == e;

        void InitPassive()
        {
            GameDatabase.Instance.RatSkills.TryGetValue(Data.passive_skill, out var ps);
            Passive = ps; PassiveType = ps != null ? ps.Effect : EffectType.None;
            abT = Random.Range(0.5f, 3f);
            KnockPower = Is(EffectType.Knockback_Boost) ? PV(1) : 1;
        }

        // 조건 값 (확률·주기·반경). 확률 칸이 0 이면 항상
        float C1 => Passive != null ? Passive.cond1_value_01 : 0;
        float C2 => Passive != null ? Passive.cond2_value_01 : 0;
        bool RollChance() => C1 <= 0 || Random.value < C1;
        bool RollTrick() => Random.value < C1 * TrickChanceMult;      // 묘기 확률 × 재롱 본능

        // ── 상시 배율 (다른 코드가 물어봄) ──
        public float PassiveDamageMult => 1 + buff + (Is(EffectType.Pack_Power) ? packN * PV(1) : 0);
        public float PassiveSpeedMult => 1 + buff;
        float DashSpeedMult => Is(EffectType.Dash_Boost) ? PV(1) : 1;
        float DashHitMult(float speed) =>
            Is(EffectType.Dash_Boost) && speed > PV(3) ? PV(2) :
            Is(EffectType.Pierce_Dash) && speed > PV(2) ? PV(1) : 1;
        float CritChance => Manager.baseCritChance + (Is(EffectType.Crit_Boost) ? PV(1) : 0);
        float CritMult => (Is(EffectType.Crit_Boost) ? PV(2) : Manager.critMultiplier) + CommonSkill.CritDmgAdd;
        float WallMult => Is(EffectType.Wall_Breaker) ? PV(1) : 1;
        public float CheeseMult => Is(EffectType.Cheese_Boost) ? PV(1) : 1;
        public float ChainRadiusAdd => Is(EffectType.Chain_Explosion) ? PV(1) : 0;
        public float ChainDamageAdd => Is(EffectType.Chain_Explosion) ? PV(2) : 0;
        public float BreedCoolDiv => Is(EffectType.Breed_Boost) ? PV(1) : 1;
        public float BreedChanceMul => Is(EffectType.Breed_Boost) ? PV(2) : 1;
        public float BirthBonus => Is(EffectType.Breed_Boost) ? PV(3) : 0;
        bool Pierces => Is(EffectType.Pierce_Dash);
        float SleepChance => Is(EffectType.Snore_Shockwave) ? PV(1) : Manager.sleepChance;
        public bool IsLeader => Is(EffectType.Leader_Aura);
        public bool IsPack => Is(EffectType.Pack_Power);
        public bool HasExtraParcel => Is(EffectType.Extra_Parcel);
        // 치즈 추가 (치즈 퐁듀 쥐): 이 쥐의 액티브·필살기로 부순 물건 치즈 ×(1 + 밸류_01)
        public float SkillKillCheeseMul => Is(EffectType.Skill_Cheese_Bonus) ? 1 + PV(1) : 1;

        // 단서 수집 (찍찍 탐정): 물건을 부술 때마다 단서 +밸류_01. 단서 밸류_02 개마다 필살기 공이 한 번 더 튕김 (최대 +밸류_03).
        // 단서는 모은 그 쥐만 가짐 (Rat.clues, 그 쥐가 필살기를 쓰면 0)
        public void OnSmashedItem()
        {
            if (Is(EffectType.Clue_Collect) && temp <= 0) clues += PV(1);
        }
        public bool CollectsClues => Is(EffectType.Clue_Collect);
        public int ClueBounces(float clues) => Is(EffectType.Clue_Collect) && PV(2) > 0 ? Mathf.Min(Mathf.RoundToInt(PV(3)), Mathf.FloorToInt(clues / PV(2))) : 0;

        // ── 물건을 갉은 순간 (조건 Hit_Item) ──
        void PassiveOnHit(Item it, float dmg, float ang)
        {
            var fx = FxManager.I;
            switch (PassiveType)
            {
                case EffectType.Double_Bite:
                    if (RollChance()) Manager.Later(PV(2), () => { if (it && it.State == Item.ItemState.Rest) { it.Damage(dmg * PV(1), this, false, ang); bite = 1; } });
                    break;
                case EffectType.Throw_Bomb:
                    if (RollChance()) Manager.Items.ThrowBomb(this, it.x + Random.Range(-20f, 20f), it.y + Random.Range(-20f, 20f), PV(2), dmg * PV(1));
                    break;
                case EffectType.Hit_Shockwave:
                    Manager.Items.Aoe(x, y, PV(2), dmg * PV(1), this);
                    break;
                case EffectType.Trick_Flip: if (RollTrick()) StartTrick(TrickType.Flip, ang); break;
                case EffectType.Trick_Axel: if (RollTrick()) StartTrick(TrickType.Axel, ang); break;
                case EffectType.Trick_Windmill: if (RollTrick()) StartTrick(TrickType.Windmill, ang); break;
                case EffectType.Trick_Cannon: if (RollTrick()) StartTrick(TrickType.Cannon, ang + Mathf.PI + Random.Range(-0.8f, 0.8f)); break;
            }
        }

        // 갉기 직전 (황금은 피해보다 먼저)
        void PassiveBeforeHit(Item it)
        {
            if (Is(EffectType.Make_Gold) && !it.Gold && RollChance()) it.MakeGold(PV(1), PV(2));
        }

        // ── 주기형 (조건 Interval) ──
        void PassiveTick(float dt)
        {
            if (Passive == null || Passive.Cond1 != CondType.Interval) return;
            if ((abT -= dt) > 0) return;
            abT = C1;
            var fx = FxManager.I;
            switch (PassiveType)
            {
                case EffectType.Aoe_Pulse:
                    Manager.Items.Aoe(x, y, PV(2), Damage * PV(1), this, false);
                    fx?.Ring(x, y, PV(2), codeId == "cosmic" ? new Color(0.8f, 0.71f, 0.86f, 0.8f) : new Color(0.84f, 0.94f, 0.66f, 0.85f), 0.45f);
                    break;
                case EffectType.Teleport_Strike:
                {
                    // 근처 물건 옆으로 순간이동해서 급습
                    var target = Manager.Items.RandomRestInRange(x, y, C2);
                    if (!target) break;
                    fx?.Dust(x, y, 6, 1);
                    float a = Random.Range(0, Mathf.PI * 2), ox = x, oy = y;
                    x = target.x + Mathf.Cos(a) * (target.R + Radius + 2); y = target.y + Mathf.Sin(a) * (target.R + Radius + 2);
                    Manager.Stage.Confine(ref x, ref y, ref vx, ref vy, Radius, ox, oy, 0);
                    fx?.Dust(x, y, 6, 1);
                    face = target.x > x ? 1 : -1; bite = 1;
                    target.Damage(Damage * PV(1), this, false, Mathf.Atan2(target.y - y, target.x - x));
                    break;
                }
                case EffectType.Laser:
                {
                    var best = Manager.Items.Nearest(x, y, C2);
                    if (!best) break;
                    float a = Mathf.Atan2(best.y - y, best.x - x), ux = Mathf.Cos(a), uy = Mathf.Sin(a), len = PV(3);
                    int n = Mathf.RoundToInt(PV(2));
                    var hit = Manager.Items.OnLine(x, y, ux, uy, len, 10, n);
                    foreach (var o in hit) o.Damage(Damage * PV(1), this, false, a);
                    face = ux > 0 ? 1 : -1;
                    var last = hit.Count > 0 ? hit[^1] : best;
                    fx?.Beam(x, y, 20, last.x + ux * 40, last.y + uy * 40, 20, codeId == "alien" ? new Color(0.62f, 0.84f, 0.66f) : new Color(0.91f, 0.47f, 0.42f), 0.22f);
                    break;
                }
                case EffectType.Cone_Fire:
                {
                    float range = PV(2), half = PV(3) * Mathf.Deg2Rad, a0 = (vx != 0 || vy != 0) ? Mathf.Atan2(vy, vx) : (face > 0 ? 0 : Mathf.PI);
                    foreach (var o in Manager.Items.InRange(x, y, range + 60))
                    {
                        if (o.State != Item.ItemState.Rest) continue;
                        float dx = o.x - x, dy = o.y - y, d = Mathf.Sqrt(dx * dx + dy * dy), da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                        if (d < range + o.R && Mathf.Abs(da) < half) o.Damage(Damage * PV(1), this, false, Mathf.Atan2(dy, dx));
                    }
                    face = Mathf.Cos(a0) >= 0 ? 1 : -1;
                    fx?.Spray(x, y, 18, a0, range, 22, half, new Color(0.95f, 0.79f, 0.3f), new Color(1, 0.95f, 0.75f));
                    break;
                }
            }
        }

        // 잠자는 동안 코골이 충격파
        void SnoreTick(float dt)
        {
            if (!Is(EffectType.Snore_Shockwave)) { if (Random.value < dt) FxManager.I?.Popup(x + 10, y, "z", Color.white, 14, 1, 30); return; }
            if ((abT -= dt) > 0) return;
            abT = PV(4);
            Manager.Items.Aoe(x, y, PV(3), Damage * PV(2), this);
            FxManager.I?.Popup(x + 10, y, "ZZZ", new Color(0.75f, 0.89f, 0.92f), 18, 0.8f, 34);
        }

        // 택배 투하 때 화면에 있으면 자기 주변에 추가 배달 (조건 Parcel_Drop + On_Screen)
        public void OnParcelWave()
        {
            if (OnScreen()) ActTrigger(CondType.Parcel_Drop);
            if (!HasExtraParcel || !OnScreen()) return;
            int k = Mathf.RoundToInt(PV(1));
            for (int q = 0; q < k; q++) Manager.Items.DropParcelNear(x, y, PV(2), 420 + Random.Range(0f, 200f));
            string line = codeId == "santa" ? "메리 쥐스마스!" : codeId == "streamrat" ? "흐에~ 후원 감사합니다~!" : "배달 왔습니다~";
            FxManager.I?.Popup(x, y, line, new Color(0.94f, 0.78f, 0.47f), 17, 1.2f, 50);
        }

        public bool OnScreen(float margin = 0.02f)
        {
            var cam = Camera.main; if (!cam) return false;      // 밸런스 측정 중엔 카메라가 꺼져 있음
            var vp = cam.WorldToViewportPoint(transform.position);
            return vp.x > -margin && vp.x < 1 + margin && vp.y > -margin && vp.y < 1 + margin;
        }
    }
}
