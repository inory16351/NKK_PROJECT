// 데이터 테이블(Data_Table/*.xlsx)의 enum 칼럼 값과 이름이 같아야 함.
namespace NKK.Data
{
    public enum Grade { Common, Rare, Epic, Unique, Legendary, Mythic }
    public enum BodyType { Rat, Mouse, Hamster, Gerbil, Squirrel }
    public enum SkillCategory { Passive, Action }

    public enum CondType
    {
        None, Always, Hit_Item, Hit_Wall, Hit_Gold_Item, Destroy_Item, Dashing, Interval, Ally_Count, Combo_Over,
        Item_In_Range, Birth, Sleep_Start, Parcel_Drop, On_Screen, Drop_Prop,
        // 필살기 게이지 (Ult_Charge) · 기획 조건
        Destroy_Wall, Hit_Boss, Hit_Boss_Count, Game_Start, Stage_Clear, Destroy_Item_Count, Action_Use, Defeat_Human, Defeat_Cat,
    }

    // 쥐 스킬 효과 = 로직 이름 (value_01~06 의미는 테이블 Effect_Type 시트)
    public enum EffectType
    {
        None,
        // 특수 능력 (패시브)
        Pack_Power, Wall_Breaker, Dash_Boost, Knockback_Boost, Breed_Boost, Cheese_Boost, Crit_Boost, Double_Bite, Throw_Bomb,
        Extra_Parcel, Trick_Windmill, Trick_Flip, Trick_Axel, Trick_Cannon, Aoe_Pulse, Chain_Explosion, Teleport_Strike,
        Leader_Aura, Snore_Shockwave, Laser, Pierce_Dash, Make_Gold, Cone_Fire, Hit_Shockwave, Clue_Collect, Skill_Cheese_Bonus,
        // 특수 액션
        Thunder, Gun_Kata, Jump_Slam, Dash_Slash, Barrage, Sonic_Wave, Spin_Beam, Breath, Rolling_Ball, Summon, Vortex,
        Meteor, Midas, Tornado, Dig_Strike, Cheer, Feast, Throw_Item, Truth_Point, Cheese_Fountain,
    }

    public enum GrowthEffectType
    {
        None, Atk_Growth, Dash_Speed_Growth, Action_Unlock, Trick_Chance_Growth, Crit_Chance_Growth, Action_Power_Growth,
        Passive_Power_Growth, Action_Awaken, Awaken, Ult_Unlock,
    }

    public enum CatCategory { Normal, Special }
    public enum CatEffectType { None, Hiss_Fear, Double_Pounce, Crit_Pounce, Jump_Press, Roll_Charge, Crowd_Teleport, Laser_Stun, Roar_Blast, Fireball, Gravity_Wave,
        Crowd_Slam, Crowd_Combo, Crowd_Pinpoint, Crowd_Belly, Crowd_Roll, Crowd_Blink, Crowd_Laser, Crowd_Quake, Crowd_Meteor, Crowd_Vortex }

    // 공용 스킬 트리 (공용 스킬 테이블, 찍찍!! 훈장별 트리) — 가지 = 지도 색
    public enum SkillBranch { Core, Combat, Growth, Loot, Trick, Special }

    // 공용 스킬 효과 = 로직 이름 (value_01~03 의미는 공용 스킬 테이블 Common_Effect_Type 시트). 같은 효과끼리 더함
    public enum CommonEffectType
    {
        None,
        // 전투
        Atk_Flat, Atk_Pct, Dmg_Pct, Wall_Dmg_Pct, Crit_Chance, Crit_Dmg, Multi_Hit, Boss_Dmg_Pct,
        // 승급·시간·시작 쥐
        Start_Rat, Promote_Double, Pop_Cap, Time_Add, Breed_Chance, Move_Speed_Pct, Breed_Cool_Pct,
        // 자원 파밍
        Item_Count_Flat, Item_Count_Pct, Cheese_Pct, New_Item, Rocket_CD, Gold_Item, Spawn_Rate_Pct, Combo_Time_Pct,
        // 묘기
        Trick_Unlock, Trick_Chance, Trick_Cheese_Pct, Trick_Gauge_Pct, Trap_Single_Down, Trap_Multi_Down,
        // 해금·특수
        Stage_Skip, Wall_Hp_Down, Ult_Gauge_Pct, Ult_Power_Pct, Ult_Auto, Super_Jump_Pct,
        Bite_Zap, Chain_Blast, Cheese_Meteor, Twin_Chance, Mutation_Chance, Birth_Frenzy, Furniture_Pct, Air_Cheese_Pct, Rush_Up, Cat_Hp_Down, Rush_CD, Ult_CD, Rush_Range,
    }
}
