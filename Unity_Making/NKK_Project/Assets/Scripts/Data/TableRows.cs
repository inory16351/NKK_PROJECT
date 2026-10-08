using System;
using System.Collections.Generic;
using UnityEngine;

// JSON(Tools/xlsx2json.py 출력)의 행 구조. 필드 이름 = 엑셀 2행 키.
// enum 칼럼은 문자열로 받고, 아래 속성(Grade 등)으로 enum 변환해서 씀.
namespace NKK.Data
{
    static class E
    {
        public static T P<T>(string s) where T : struct => Enum.TryParse(s, out T v) ? v : default;
    }

    [Serializable]
    public class RatCharacterRow
    {
        public int character_id; public string character_name; public string code_id; public string grade; public string body_type;
        public int atk; public int passive_skill; public int action_skill; public int ultimate; public int unlock_rank;
        public string character_desc; public string asset_folder; public string sub_asset_folder;
        public Grade Grade => E.P<Grade>(grade);
        public BodyType Body => E.P<BodyType>(body_type);
    }

    [Serializable]
    public class RatSkillRow
    {
        public int skill_id; public string skill_name; public string skill_category;
        public string cond1_type; public float cond1_value_01, cond1_value_02;
        public string cond2_type; public float cond2_value_01, cond2_value_02;
        public string effect_type; public float value_01, value_02, value_03, value_04, value_05, value_06;
        public float duration, cool_time; public string skill_icon, skill_asset, skill_explain;
        public SkillCategory Category => E.P<SkillCategory>(skill_category);
        public CondType Cond1 => E.P<CondType>(cond1_type);
        public CondType Cond2 => E.P<CondType>(cond2_type);
        public EffectType Effect => E.P<EffectType>(effect_type);
        public float V(int i) => i switch { 1 => value_01, 2 => value_02, 3 => value_03, 4 => value_04, 5 => value_05, 6 => value_06, _ => 0 };
    }

    [Serializable]
    public class RatUltimateRow
    {
        public int ultimate_id; public string ultimate_name, script, dev_desc, asset;
        public int ult_gauge; public string ult_icon, ult_line, ult_achv, ult_color, engine_type;
        public Color Color => ColorUtility.TryParseHtmlString(ult_color, out var c) ? c : Color.white;
    }

    // 필살기 자막 (Ult_Caption): 필살기 코드가 키(c1, c2…)로 부름. {n} 같은 자리는 코드가 채움
    [Serializable] public class UltCaptionRow { public int ultimate_id; public string cap_key, text; public float size; public string color; }
    // 필살기 게이지가 차는 조건 (Ult_Charge): 그 종의 쥐가 cond_type 을 하면 gauge 만큼
    [Serializable] public class UltChargeRow { public string cond_type; public float gauge; public CondType Cond => E.P<CondType>(cond_type); }

    [Serializable]
    public class RatGradeRow
    {
        public string grade, grade_name, color; public float size, move_speed, birth_weight, skill_power, shard_k; public int atk_base;
        // 승급 필요 마릿수 = 올림(promote_base × promote_grow^k), k = 이번 판 승급 횟수. promote_soft 마리를 넘으면 그 뒤로는 promote_grow2 로 완만하게
        public float promote_base, promote_grow, promote_soft, promote_grow2;
        public Grade Grade => E.P<Grade>(grade);
    }

    [Serializable]
    public class GrowthNodeRow
    {
        public int node_id; public string node_name, code_id; public int max_level, req_node, req_level;
        public string effect_type; public float value_01, value_02, value_03;
        public int pos_x, pos_y;                 // 쥐 스킬 트리 창의 칸
        public string node_icon, node_desc;      // 아이콘: 경로 또는 @action · @passive · @ult (그 쥐의 아이콘) / 설명: {v1}~{v3} · {p1}~{p3}(×100)
        public GrowthEffectType Effect => E.P<GrowthEffectType>(effect_type);
    }

    [Serializable] public class GrowthOrderRow { public int order, node_id, is_repeat; }

    [Serializable]
    public class ActionAwakenRow
    {
        public string effect_type; public float value_01, value_02, value_03;
        public EffectType Effect => E.P<EffectType>(effect_type);
    }

    [Serializable]
    public class CatCharacterRow
    {
        public int character_id; public string character_name, code_id, cat_category; public int spawn_floor;
        public float hp_mul, life_time, size_mul; public int skill, crowd_skill; public string character_desc, asset_folder;
        public CatCategory Category => E.P<CatCategory>(cat_category);
    }

    [Serializable]
    public class CatSkillRow
    {
        public int skill_id; public string skill_name;
        public string cond1_type; public float cond1_value_01, cond1_value_02;
        public string cond2_type; public float cond2_value_01, cond2_value_02;
        public string effect_type; public float value_01, value_02, value_03, value_04, value_05, value_06;
        public float duration, cool_time; public string skill_icon, skill_explain;
        public CatEffectType Effect => E.P<CatEffectType>(effect_type);
    }

    // 워크북 단위 래퍼 (JSON 최상위 키 = 시트 이름)
    [Serializable] public class RatTableFile { public List<RatCharacterRow> Character; public List<RatSkillRow> Skill; public List<RatUltimateRow> Ultimate; public List<UltCaptionRow> Ult_Caption; public List<UltChargeRow> Ult_Charge; }
    [Serializable] public class RatGradeTableFile { public List<RatGradeRow> Grade; }
    [Serializable] public class RatGrowthTableFile { public List<GrowthNodeRow> Growth_Node; public List<GrowthOrderRow> Growth_Order; public List<ActionAwakenRow> Action_Awaken; }
    [Serializable] public class CatTableFile { public List<CatCharacterRow> Character; public List<CatSkillRow> Skill; }

    [Serializable]
    public class ItemRow
    {
        public int item_id; public string item_name, code_id, item_category;
        public float radius, hp_mul, value_mul, spawn_weight, heavy; public int is_big, is_sturdy, is_paper;
        public int from_floor, to_floor, unlock_skill;      // 등장 층 · 사라지는 층 (0 = 계속) · 해금 스킬 노드 (0 = 없음)
        public string spill_color; public int drop_01, drop_02, drop_03, drop_04; public string asset;
        public bool IsFurniture => item_category == "Furniture";
        public bool IsSpill => item_category == "Spill";        // 쏟아짐 전용 (큰 물건·가구가 부서질 때만 나옴, 평소 생성 안 됨)
        public IEnumerable<int> Drops() { foreach (var d in new[] { drop_01, drop_02, drop_03, drop_04 }) if (d != 0) yield return d; }
    }

    [Serializable]
    public class ZoneRow
    {
        public int zone_id; public string zone_name; public int from_floor; public string floor_type, tint;
        public int item_01, item_02, item_03, item_04, item_05, item_06, item_07, item_08, item_09, item_10, item_11, item_12, item_13;
        public IEnumerable<int> Items() { foreach (var d in new[] { item_01, item_02, item_03, item_04, item_05, item_06, item_07, item_08, item_09, item_10, item_11, item_12, item_13 }) if (d != 0) yield return d; }
    }

    [Serializable] public class FurnitureLayoutRow { public int layout_id; public string room_theme; public int item_id; public float pos_u, pos_v; }
    [Serializable] public class ItemTableFile { public List<ItemRow> Item; public List<ZoneRow> Zone; public List<FurnitureLayoutRow> Furniture_Layout; }

    // Tools/gen_sprite_pivots.py → Assets/Data/RatRigMeta.json (몸통 부착점 = 몸통 이미지 좌상단 기준 0~1)
    [Serializable] public class RatRigMetaRow { public string code_id; public float[] neck, tail, shoulder, hip; public float leg_front, leg_back; }
    [Serializable] public class RatRigMetaFile { public List<RatRigMetaRow> items; }

    [Serializable]
    public class TierRow
    {
        public int tier; public string tier_name, icon; public int research_cost;
        public string cond1_type; public float cond1_value; public string cond2_type; public float cond2_value; public string cond3_type; public float cond3_value;
        public float birth_grade_k; public int start_rat_count, start_floor_cap; public string badge_asset; public float rat_atk_mul;
    }
    [Serializable] public class TierTableFile { public List<TierRow> Tier; }

    [Serializable] public class HumanRow { public int human_id; public string human_name, code_id; public float hp_mul, value_mul, speed_mul; public int from_floor; public string asset_folder; }
    [Serializable] public class HumanLineRow { public int line_id, human_id; public string situation, text; }
    // 공용 스킬 노드 (공용 스킬 테이블 Common_Skill, Tools/gen_skill_tree.py 로 생성).
    // 찍찍!! 훈장(tier)마다 트리 하나, 노드는 한 번만 활성화. link_1/link_2 중 하나라도 활성화되면 열림. 시작점(effect None, 링크 없음)은 그 훈장이면 자동 활성화
    [Serializable]
    public class CommonSkillRow
    {
        public int skill_id, tier; public string skill_name, branch; public int is_key, pos_x, pos_y, link_1, link_2;
        public float cost_cheese, cost_research; public string effect_type;
        public float value_01, value_02, value_03;
        public string skill_asset, skill_explain;
        public SkillBranch Branch => E.P<SkillBranch>(branch);
        public CommonEffectType Effect => E.P<CommonEffectType>(effect_type);
        public bool IsKey => is_key == 1;
        public bool IsRoot => link_1 == 0 && link_2 == 0;
        public float V(int i) => i switch { 1 => value_01, 2 => value_02, 3 => value_03, _ => 0 };
    }

    [Serializable] public class SkillBranchRow { public string branch, branch_name, color; public SkillBranch Branch => E.P<SkillBranch>(branch); }

    [Serializable] public class CommonSkillTableFile { public List<CommonSkillRow> Common_Skill; public List<SkillBranchRow> Branch; }

    [Serializable] public class HumanTableFile { public List<HumanRow> Human; public List<HumanLineRow> Human_Line; }
    // 보스 (보스 테이블 Boss · Boss_Line)
    [Serializable]
    public class BossRow
    {
        public int boss_id; public string boss_name, code_id, rig; public int floor; public string atk_type, atk2_type, color, special1_type, special2_type, skill_type;
        public float skill_cd, skill_first, special_chance, atk2_chance, scale, radius_mul, hp_pow_sec, cheese_mul, move_speed, atk_cd_min, atk_cd_max;
        public bool IsCat => rig == "Cat";
        public Color Color => ColorUtility.TryParseHtmlString(color, out var c) ? c : Color.white;
    }
    [Serializable] public class BossLineRow { public int line_id, boss_id; public string situation, text; }
    // 보스 공격별 수치 (보스 테이블 Atk_Type 시트)
    [Serializable] public class BossAtkRow { public string atk_type; public float radius, stun; public int count; public float windup, dur; public string atk_name, line; }
    // 층 밸런스 (스테이지 테이블 Stage, Tools/gen_stage_table.py 로 생성)
    [Serializable] public class StageRow { public int floor, rooms; public float pow_need, item_hp, cheese, wall_stairs, wall_normal, wall_path, time_add; }     // wall_path = 계단까지 경로 벽 배율 합 (판마다 지형이 바뀌어 StageManager 가 경로에 나눔)
    // 업적 (업적 테이블 Achievement): 조건 타입 cond_type + 대상 target_id + 필요 횟수 need. 설명 {ult} = 대상 필살기 이름
    [Serializable] public class AchievementRow { public int achv_id; public string achv_name, achv_desc, cond_type; public int target_id, need; public string achv_icon; public int sort_order; }
    [Serializable] public class AchievementTableFile { public List<AchievementRow> Achievement; }
    [Serializable] public class StageTableFile { public List<StageRow> Stage; }
    [Serializable] public class BossTableFile { public List<BossRow> Boss; public List<BossLineRow> Boss_Line; public List<BossAtkRow> Atk_Type; }
    [Serializable] public class HumanRigMetaRow { public string code_id; public float[] neck, shoulder, hip; public int front; public float head_scale = 1; }
    [Serializable] public class HumanRigMetaFile { public List<HumanRigMetaRow> items; }
}
