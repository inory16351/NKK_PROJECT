using System.Collections.Generic;
using NKK.Data;
using UnityEngine;

namespace NKK
{
    // 공용 스킬 효과 계산 (활성화한 노드의 value_01~03 을 효과 타입별로 더함). 식은 공용 스킬 테이블 Common_Effect_Type 시트와 같음.
    // 공격력 % 같은 배율도 노드끼리는 더하기만 함 → 과하게 커지지 않음 (공격력 % 와 피해량 % 는 따로 곱함).
    // 게임 쪽은 여기 값만 곱하거나 더하면 됨 (아무것도 안 찍었으면 배율 1 · 가산 0).
    public static class CommonSkill
    {
        static int ver = -1;
        static GameDatabase dbRef;
        static readonly Dictionary<CommonEffectType, List<CommonSkillRow>> owned = new();
        static readonly List<CommonSkillRow> none = new();

        static void Sync()
        {
            var p = Progress.I; var db = GameDatabase.Instance;
            int v = p ? p.SkillVersion : -2;
            if (v == ver && db == dbRef) return;
            ver = v; dbRef = db; owned.Clear();
            if (!p || !db) return;
            foreach (var s in db.CommonSkills)
            {
                if (s.Effect == CommonEffectType.None || !p.HasSkill(s)) continue;
                if (!owned.TryGetValue(s.Effect, out var l)) owned[s.Effect] = l = new List<CommonSkillRow>();
                l.Add(s);
            }
        }
        static List<CommonSkillRow> Of(CommonEffectType e) { Sync(); return owned.TryGetValue(e, out var l) ? l : none; }
        static bool Has(CommonEffectType e) => Of(e).Count > 0;
        static float Sum(CommonEffectType e, int i = 1) { float s = 0; foreach (var r in Of(e)) s += r.V(i); return s; }
        static float Max(CommonEffectType e, int i, float def = 0) { float m = def; bool any = false; foreach (var r in Of(e)) { m = any ? Mathf.Max(m, r.V(i)) : r.V(i); any = true; } return any ? m : def; }
        // 등급 조건 (밸류_02 = 0 모든 쥐 · 1 일반 ~ 6 신화, 밸류_03 = 1 그 등급 이상). gradeIndex = 0 일반 ~ 5 신화
        static bool GradeOk(CommonSkillRow r, int gradeIndex) { int g = Mathf.RoundToInt(r.value_02); return g == 0 || (r.value_03 >= 1 ? gradeIndex + 1 >= g : gradeIndex + 1 == g); }
        static float SumGrade(CommonEffectType e, int gradeIndex) { float s = 0; foreach (var r in Of(e)) if (GradeOk(r, gradeIndex)) s += r.value_01; return s; }

        // ── 전투 ──
        public static float AtkFlat(int gradeIndex) => SumGrade(CommonEffectType.Atk_Flat, gradeIndex);
        public static float AtkMul(int gradeIndex) => (1 + SumGrade(CommonEffectType.Atk_Pct, gradeIndex)) * (1 + Sum(CommonEffectType.Dmg_Pct));
        public static float WallDmgMul => 1 + Sum(CommonEffectType.Wall_Dmg_Pct);
        public static float CritAdd => Sum(CommonEffectType.Crit_Chance);
        public static float CritDmgAdd => Sum(CommonEffectType.Crit_Dmg);
        public static float MultiHitChance => Sum(CommonEffectType.Multi_Hit);
        public static int MultiHitCount => Mathf.Max(1, Mathf.RoundToInt(Max(CommonEffectType.Multi_Hit, 2, 1)));
        public static float BossDmgMul => 1 + Sum(CommonEffectType.Boss_Dmg_Pct);
        public static float BossTimeAdd => Sum(CommonEffectType.Boss_Dmg_Pct, 2);      // 보스 층 제한시간 + (초)

        // ── 승급·시간·시작 쥐 ──
        // 시작 쥐 추가 마릿수 (등급은 시작 쥐처럼 지금 훈장의 등급 확률로 뽑음 — 밸류_02 등급 칸은 안 씀)
        public static int StartRatAdd => Mathf.RoundToInt(Sum(CommonEffectType.Start_Rat));
        // 승급할 때 윗등급 쥐가 2마리 나올 확률 (gradeIndex = 승급하는 쪽 0 일반 → 레어 ..., 밸류_02 = 그 등급 1~5, 0 = 모든 승급)
        public static float PromoteDouble(int gradeIndex) { float s = 0; foreach (var r in Of(CommonEffectType.Promote_Double)) { int g = Mathf.RoundToInt(r.value_02); if (g == 0 || g == gradeIndex + 1) s += r.value_01; } return s; }
        public static int MaxPopAdd => Mathf.RoundToInt(Sum(CommonEffectType.Pop_Cap));
        public static float TimeAdd => Sum(CommonEffectType.Time_Add);
        public static float BreedChanceAdd(int pop) { float s = 0; foreach (var r in Of(CommonEffectType.Breed_Chance)) if (r.value_02 <= 0 || pop < r.value_02) s += r.value_01; return s; }
        public static float MoveSpeedMul => 1 + Sum(CommonEffectType.Move_Speed_Pct);
        public static float StopTimeMul => 1;
        public static float BreedCoolDiv => 1 + Sum(CommonEffectType.Breed_Cool_Pct);
        public static float TwinChance => Sum(CommonEffectType.Twin_Chance);
        public static float MutationAdd => Sum(CommonEffectType.Mutation_Chance);
        public static float FrenzyTime => Sum(CommonEffectType.Birth_Frenzy);
        public static float FrenzyRadius => Max(CommonEffectType.Birth_Frenzy, 2);
        public static float FrenzyMul => Max(CommonEffectType.Birth_Frenzy, 3, 1.5f);

        // ── 자원 파밍 ──
        // 치즈 배율: 전부(밸류_02 0) + 대상(1 생명체 · 2 물건)을 더함
        static float CheeseTarget(int target) { float s = 0; foreach (var r in Of(CommonEffectType.Cheese_Pct)) { int t = Mathf.RoundToInt(r.value_02); if (t == 0 || t == target) s += r.value_01; } return s; }
        public static float CheeseMul => 1 + CheeseTarget(0);
        public static float CreatureCheeseMul => 1 + CheeseTarget(1);
        public static float ItemCheeseMul => 1 + CheeseTarget(2);
        public static float ItemCapMul => 1 + Sum(CommonEffectType.Item_Count_Pct);
        public static int ItemCapAdd => Mathf.RoundToInt(Sum(CommonEffectType.Item_Count_Flat));
        public static bool ItemUnlocked(int nodeId) { if (nodeId == 0) return true; var p = Progress.I; var db = GameDatabase.Instance; return p && db && db.CommonSkillsById.TryGetValue(nodeId, out var s) && p.HasSkill(s); }
        public static float SpawnRateMul => 1 + Sum(CommonEffectType.Spawn_Rate_Pct);
        public static int SpawnBatch => 1;
        public static float ComboTimeMul => 1 + Sum(CommonEffectType.Combo_Time_Pct);
        public static float DeliveryInterval(float baseSec) => Mathf.Max(baseSec * 0.4f, baseSec - Sum(CommonEffectType.Rocket_CD));
        public static int DeliveryCount(int baseCount) => baseCount;
        public static float GoldChance => Sum(CommonEffectType.Gold_Item);
        public static float GoldCheeseMul => Max(CommonEffectType.Gold_Item, 2, 5);
        public static float FurnitureDmgMul => 1 + Sum(CommonEffectType.Furniture_Pct);
        public static float FurnitureCheeseMul => 1 + Sum(CommonEffectType.Furniture_Pct, 2);
        public static float AirCollideCheeseMul => 1 + Sum(CommonEffectType.Air_Cheese_Pct);
        public static float AirBonusMul => 1;

        // ── 묘기 (1 백덤블링 · 2 윈드밀 · 3 트리플 악셀 · 4 쥐 대포알) ──
        public static bool TrickUnlocked(int trick) { foreach (var r in Of(CommonEffectType.Trick_Unlock)) if (Mathf.RoundToInt(r.value_01) == trick) return true; return false; }
        // 해금 안 됐으면 0. 해금 기본 확률 + 그 묘기·전체 확률 노드 합
        public static float TrickChance(int trick)
        {
            float c = 0; bool on = false;
            foreach (var r in Of(CommonEffectType.Trick_Unlock)) if (Mathf.RoundToInt(r.value_01) == trick) { on = true; c += r.value_02; }
            if (!on) return 0;
            foreach (var r in Of(CommonEffectType.Trick_Chance)) { int t = Mathf.RoundToInt(r.value_01); if (t == 0 || t == trick) c += r.value_02; }
            return c;
        }
        public static float TrickCheeseMul => 1 + Sum(CommonEffectType.Trick_Cheese_Pct);
        public static float TrickGaugeMul => 1 + Sum(CommonEffectType.Trick_Gauge_Pct);
        public static float TrapSingleChance => Mathf.Clamp01(1 - Sum(CommonEffectType.Trap_Single_Down));
        public static float TrapMultiDown => Sum(CommonEffectType.Trap_Multi_Down);
        public static float TrapStunMul => 1;
        public static float TrapCheeseChance => 0;

        // ── 해금·특수 ──
        public static int StageSkip => Mathf.RoundToInt(Sum(CommonEffectType.Stage_Skip));
        public static float WallHpMul => Mathf.Max(0.5f, 1 - Sum(CommonEffectType.Wall_Hp_Down));
        public static float UltGaugeMul => 1 + Sum(CommonEffectType.Ult_Gauge_Pct);
        public static float UltPowerMul => 1 + Sum(CommonEffectType.Ult_Power_Pct);
        public static bool UltAuto => Has(CommonEffectType.Ult_Auto);
        public static float UltCdLess => Sum(CommonEffectType.Ult_CD);          // 필살기 쿨타임 감소 (초)
        public static float SuperJumpMul => 1 + Sum(CommonEffectType.Super_Jump_Pct);
        public static float ZapChance => Sum(CommonEffectType.Bite_Zap);
        public static int ZapTargets => Mathf.RoundToInt(Max(CommonEffectType.Bite_Zap, 2));
        public static float ZapDamageK => Max(CommonEffectType.Bite_Zap, 3);
        public static float ChainRadiusAdd => Sum(CommonEffectType.Chain_Blast);
        public static float ChainDamageAdd => Sum(CommonEffectType.Chain_Blast, 2);
        public static bool MeteorOn { get { foreach (var r in Of(CommonEffectType.Cheese_Meteor)) if (r.value_01 > 0) return true; return false; } }
        public static float MeteorCool { get { float m = float.MaxValue; foreach (var r in Of(CommonEffectType.Cheese_Meteor)) if (r.value_01 > 0) m = Mathf.Min(m, r.value_01); return m < float.MaxValue ? m : 12; } }
        public static float MeteorPowerK => 1 + Sum(CommonEffectType.Cheese_Meteor, 2);
        public static float RushTimeAdd => Sum(CommonEffectType.Rush_Up);
        public static float RushMulAdd => Sum(CommonEffectType.Rush_Up, 2);
        public static float RushCdLess => Sum(CommonEffectType.Rush_CD);          // 총공격 쿨타임 감소 (초)
        public static float CatHpMul => Mathf.Max(0.1f, 1 - Sum(CommonEffectType.Cat_Hp_Down));
        public static float CatFearMul => Mathf.Max(0.1f, 1 - Sum(CommonEffectType.Cat_Hp_Down, 2));
    }
}
