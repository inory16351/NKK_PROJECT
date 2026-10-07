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
        Passive_Power_Growth, Action_Awaken, Awaken,
    }

    public enum CatCategory { Normal, Special }
    public enum CatEffectType { None, Hiss_Fear, Double_Pounce, Crit_Pounce, Jump_Press, Roll_Charge, Crowd_Teleport, Laser_Stun, Roar_Blast, Fireball, Gravity_Wave }

    // 공용 스킬 트리 (공용 스킬 테이블)
    public enum SkillBranch { Core, Gnaw, Pack, Loot, Trick, Escape, Special }

    // 공용 스킬 효과 = 로직 이름 (value_01~06 의미는 공용 스킬 테이블 Common_Effect_Type 시트)
    public enum CommonEffectType
    {
        None,
        All_Power_Cheese, All_Atk_Mul, Crit_Chance_Add, Grade_Atk_Add, Bite_Zap, Chain_Blast, Furniture_Bonus, Cheese_Meteor,
        Air_Collide_Cheese, Boss_Dmg_Add,
        Move_Speed_Add, Max_Pop_Add, Caffeine, Breed_Cool, Twin_Chance, Mutation_Chance, Birth_Frenzy,
        Cheese_Mul, Spawn_Rate, Combo_Time_Add, Item_Cap_Add, Rocket_Delivery, Gold_Item_Chance,
        Backflip_Chance, Triple_Axel_Chance, Cannonball_Chance, Windmill_Chance, Air_Bonus_Add,
        Wall_Dmg_Add, Trap_Safe, Rush_Up, Catnip, Offline_Income,
        Ult_Practice, Super_Jump_Practice, Ult_Auto,
    }
}
