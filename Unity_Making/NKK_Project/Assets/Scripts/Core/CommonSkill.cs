using NKK.Data;
using UnityEngine;

namespace NKK
{
    // 공용 스킬 효과 계산 (공용 스킬 테이블 value_01~06 × Progress 레벨). 식은 테이블 Common_Effect_Type 시트와 같음.
    // 게임 쪽은 여기 값만 곱하거나 더하면 됨 (레벨 0 이면 배율 1 · 가산 0).
    public static class CommonSkill
    {
        static CommonSkillRow Row(CommonEffectType e) => GameDatabase.Instance && GameDatabase.Instance.CommonSkillsByEffect.TryGetValue(e, out var s) ? s : null;
        public static int Lv(CommonEffectType e) => Progress.I ? Progress.I.SkillLv(e) : 0;
        static float V(CommonEffectType e, int i) { var r = Row(e); return r != null ? r.V(i) : 0; }
        static float Add(CommonEffectType e, int i = 1) => V(e, i) * Lv(e);              // 밸류 × 레벨
        static float Pow(CommonEffectType e) { int l = Lv(e); return l > 0 ? Mathf.Pow(V(e, 1), l) : 1; }   // 밸류 ^ 레벨

        // ── 갉기 ──
        // 갉는 힘 배율. gradeIndex = 등급 칸 수 (Common 0 ~ Mythic 5)
        public static float AtkMul(int gradeIndex) =>
            (1 + Add(CommonEffectType.All_Power_Cheese)) * Pow(CommonEffectType.All_Atk_Mul) * (1 + Add(CommonEffectType.Grade_Atk_Add) * gradeIndex);
        public static float CritAdd => Add(CommonEffectType.Crit_Chance_Add);
        public static float ZapChance => Add(CommonEffectType.Bite_Zap);
        public static int ZapTargets => Mathf.RoundToInt(V(CommonEffectType.Bite_Zap, 2));
        public static float ZapDamageK => V(CommonEffectType.Bite_Zap, 3);
        public static float ChainRadiusAdd => Add(CommonEffectType.Chain_Blast);
        public static float ChainDamageAdd => Add(CommonEffectType.Chain_Blast, 2);
        public static float FurnitureDmgMul => 1 + Add(CommonEffectType.Furniture_Bonus);
        public static float FurnitureCheeseMul => 1 + Add(CommonEffectType.Furniture_Bonus, 2);
        public static bool MeteorOn => Lv(CommonEffectType.Cheese_Meteor) > 0;
        public static float MeteorCool => Mathf.Max(V(CommonEffectType.Cheese_Meteor, 3), V(CommonEffectType.Cheese_Meteor, 1) - Add(CommonEffectType.Cheese_Meteor, 2));
        public static float MeteorPowerK => V(CommonEffectType.Cheese_Meteor, 4) + Add(CommonEffectType.Cheese_Meteor, 5);
        public static float AirCollideCheeseMul => 1 + Add(CommonEffectType.Air_Collide_Cheese);
        public static float BossDmgMul => 1 + Add(CommonEffectType.Boss_Dmg_Add);

        // ── 무리 ──
        public static float MoveSpeedMul => (1 + Add(CommonEffectType.Move_Speed_Add)) * (1 + Add(CommonEffectType.Caffeine, 2));
        public static int MaxPopAdd => Mathf.RoundToInt(Add(CommonEffectType.Max_Pop_Add));
        public static float StopTimeMul => Mathf.Max(0.1f, 1 - Add(CommonEffectType.Caffeine));
        public static float BreedCoolDiv => 1 + Add(CommonEffectType.Breed_Cool);
        public static float TwinChance => Add(CommonEffectType.Twin_Chance);
        public static float MutationAdd => Add(CommonEffectType.Mutation_Chance);
        public static float FrenzyTime => Add(CommonEffectType.Birth_Frenzy);
        public static float FrenzyRadius => V(CommonEffectType.Birth_Frenzy, 2);
        public static float FrenzyMul => V(CommonEffectType.Birth_Frenzy, 3);

        // ── 물량·치즈 ──
        public static float CheeseMul => (1 + Add(CommonEffectType.All_Power_Cheese, 2)) * Pow(CommonEffectType.Cheese_Mul);
        public static float SpawnRateMul => 1 + Add(CommonEffectType.Spawn_Rate);
        public static int SpawnBatch { get { float k = V(CommonEffectType.Spawn_Rate, 2); return 1 + (k > 0 ? Mathf.FloorToInt(Lv(CommonEffectType.Spawn_Rate) / k) : 0); } }
        public static float ComboTimeMul => 1 + Add(CommonEffectType.Combo_Time_Add);
        public static float ItemCapMul => 1 + Add(CommonEffectType.Item_Cap_Add);
        public static float DeliveryInterval(float baseSec) => Mathf.Max(V(CommonEffectType.Rocket_Delivery, 2), baseSec - Add(CommonEffectType.Rocket_Delivery));
        public static int DeliveryCount(int baseCount) => Mathf.Min(Mathf.RoundToInt(V(CommonEffectType.Rocket_Delivery, 4)), baseCount + Mathf.RoundToInt(Add(CommonEffectType.Rocket_Delivery, 3)));
        public static float GoldChance => Add(CommonEffectType.Gold_Item_Chance);
        public static float GoldCheeseMul => V(CommonEffectType.Gold_Item_Chance, 2);

        // ── 재롱 (묘기 확률) ──
        public static float BackflipAdd => Add(CommonEffectType.Backflip_Chance);
        public static float AxelChance => Add(CommonEffectType.Triple_Axel_Chance);
        public static float CannonChance => Add(CommonEffectType.Cannonball_Chance);
        public static float WindmillChance(bool bigItem) => Add(CommonEffectType.Windmill_Chance) * (bigItem ? V(CommonEffectType.Windmill_Chance, 2) : 1);
        public static float AirBonusMul => 1 + Add(CommonEffectType.Air_Bonus_Add);

        // ── 탈출 ──
        public static float WallDmgMul => 1 + Add(CommonEffectType.Wall_Dmg_Add);
        public static float TrapStunMul => Mathf.Max(0.1f, 1 - Add(CommonEffectType.Trap_Safe));
        public static float TrapCheeseChance { get { int l = Lv(CommonEffectType.Trap_Safe); float s = V(CommonEffectType.Trap_Safe, 2); return l >= s && s > 0 ? (l - s + 1) * V(CommonEffectType.Trap_Safe, 3) : 0; } }
        public static float RushTimeAdd => Add(CommonEffectType.Rush_Up);
        public static float RushMulAdd => Add(CommonEffectType.Rush_Up, 2);
        public static float CatHpMul => Mathf.Max(0.1f, 1 - Add(CommonEffectType.Catnip));
        public static float CatFearMul => Mathf.Max(0.1f, 1 - Add(CommonEffectType.Catnip, 2));
        public static float OfflineAdd => Add(CommonEffectType.Offline_Income);

        // ── 특별 ──
        // 필살기 연습: 게이지가 (1 + 밸류_01 × 레벨) 배 빨리 참 · 필살기 피해 +밸류_02 × 레벨
        public static float UltGaugeMul => 1 + Add(CommonEffectType.Ult_Practice);
        public static float UltPowerMul => 1 + Add(CommonEffectType.Ult_Practice, 2);
        public static bool UltAuto => Lv(CommonEffectType.Ult_Auto) > 0;      // 필살기 자동 사용
        public static float SuperJumpMul => 1 + Add(CommonEffectType.Super_Jump_Practice);
    }
}
