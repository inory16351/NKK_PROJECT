using System.Collections.Generic;
using UnityEngine;

namespace NKK.Data
{
    // 데이터 테이블 JSON(Assets/Data, Tools/xlsx2json.py 출력)을 읽어 id 로 찾게 해 줌.
    // 씬마다 하이라키에 하나 두고 TextAsset 을 인스펙터에서 연결. 씬을 넘어가도 처음 것 하나만 남음.
    [DefaultExecutionOrder(-1000)]
    public class GameDatabase : MonoBehaviour
    {
        public static GameDatabase Instance { get; private set; }

        [Header("테이블 JSON (Assets/Data)")]
        public TextAsset ratTable;
        public TextAsset ratGradeTable;
        public TextAsset ratGrowthTable;
        public TextAsset catTable;
        public TextAsset itemTable;
        public TextAsset tierTable;
        public TextAsset humanTable;
        public TextAsset commonSkillTable;
        public TextAsset stageTable;
        public TextAsset bossTable;
        public TextAsset achievementTable;
        public TextAsset tutorialTable;
        [Tooltip("상점 테이블 (쥐구멍 암시장) · 뽑기 테이블 (유전자 뽑기)")] public TextAsset shopTable, gachaTable;
        [Tooltip("스트링 테이블 (언어, Loc)")] public TextAsset stringTable;

        public readonly Dictionary<int, RatCharacterRow> Rats = new();
        public readonly Dictionary<string, RatCharacterRow> RatsByCode = new();
        public readonly Dictionary<int, RatSkillRow> RatSkills = new();
        public readonly Dictionary<int, RatUltimateRow> Ultimates = new();
        public readonly Dictionary<(int, string), UltCaptionRow> UltCaptions = new();
        public readonly Dictionary<CondType, float> UltCharges = new();
        public readonly Dictionary<Grade, RatGradeRow> Grades = new();
        public readonly List<GrowthNodeRow> GrowthNodes = new();
        public readonly List<GrowthOrderRow> GrowthOrder = new();
        public readonly Dictionary<EffectType, ActionAwakenRow> ActionAwaken = new();
        public readonly Dictionary<int, CatCharacterRow> Cats = new();
        public readonly Dictionary<int, CatSkillRow> CatSkills = new();
        public readonly Dictionary<int, ItemRow> Items = new();
        public readonly Dictionary<string, ItemRow> ItemsByCode = new();
        public readonly List<ZoneRow> Zones = new();
        public readonly List<FurnitureLayoutRow> FurnitureLayouts = new();
        public readonly Dictionary<int, TierRow> Tiers = new();
        public readonly List<HumanRow> Humans = new();
        public readonly List<HumanLineRow> HumanLines = new();
        public readonly List<CommonSkillRow> CommonSkills = new();
        public readonly Dictionary<int, CommonSkillRow> CommonSkillsById = new();
        public readonly Dictionary<int, List<CommonSkillRow>> CommonSkillsByTier = new();
        public readonly Dictionary<SkillBranch, SkillBranchRow> SkillBranches = new();
        public readonly List<StageRow> Stages = new();
        public ShopTableFile Shop = new(); public GachaTableFile Gacha = new();
        readonly Dictionary<string, float> shopCfg = new(), gachaCfg = new();
        public float ShopCfg(string key, float def = 0) => shopCfg.TryGetValue(key, out var v) ? v : def;
        public float GachaCfg(string key, float def = 0) => gachaCfg.TryGetValue(key, out var v) ? v : def;
        public readonly List<BossRow> Bosses = new();
        public readonly List<AchievementRow> Achievements = new();
        public readonly List<BossLineRow> BossLines = new();
        public readonly Dictionary<string, BossAtkRow> BossAtks = new();
        public readonly Dictionary<string, TutoSpeakerRow> TutoSpeakers = new();
        public readonly List<TutoStepRow> TutoSteps = new();
        public readonly Dictionary<string, List<TutoLineRow>> TutoLines = new();
        public readonly List<TutoStoryRow> Story = new();

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            Loc.Changed += Load;
        }
        void OnDestroy() { if (Instance == this) Loc.Changed -= Load; }

        public void Load()
        {
            Rats.Clear(); RatsByCode.Clear(); RatSkills.Clear(); Ultimates.Clear(); UltCaptions.Clear(); UltCharges.Clear(); Grades.Clear();
            GrowthNodes.Clear(); GrowthOrder.Clear(); ActionAwaken.Clear(); Cats.Clear(); CatSkills.Clear();
            Items.Clear(); ItemsByCode.Clear(); Zones.Clear(); FurnitureLayouts.Clear(); Tiers.Clear(); Humans.Clear(); HumanLines.Clear(); Stages.Clear(); Bosses.Clear(); Achievements.Clear(); BossLines.Clear(); BossAtks.Clear(); TutoSpeakers.Clear(); TutoSteps.Clear(); TutoLines.Clear(); Story.Clear();
            CommonSkills.Clear(); CommonSkillsById.Clear(); CommonSkillsByTier.Clear(); SkillBranches.Clear();
            Loc.Load(stringTable);

            if (ratTable)
            {
                var f = JsonUtility.FromJson<RatTableFile>(ratTable.text);
                foreach (var r in f.Character) { Rats[r.character_id] = r; RatsByCode[r.code_id] = r; }
                foreach (var s in f.Skill) RatSkills[s.skill_id] = s;
                foreach (var u in f.Ultimate) Ultimates[u.ultimate_id] = u;
                if (f.Ult_Caption != null) foreach (var c in f.Ult_Caption) UltCaptions[(c.ultimate_id, c.cap_key)] = c;
                if (f.Ult_Charge != null) foreach (var c in f.Ult_Charge) UltCharges[c.Cond] = c.gauge;
            }
            if (ratGradeTable)
                foreach (var g in JsonUtility.FromJson<RatGradeTableFile>(ratGradeTable.text).Grade) Grades[g.Grade] = g;
            if (ratGrowthTable)
            {
                var f = JsonUtility.FromJson<RatGrowthTableFile>(ratGrowthTable.text);
                GrowthNodes.AddRange(f.Growth_Node);
                GrowthOrder.AddRange(f.Growth_Order);
                foreach (var a in f.Action_Awaken) ActionAwaken[a.Effect] = a;
            }
            if (catTable)
            {
                var f = JsonUtility.FromJson<CatTableFile>(catTable.text);
                foreach (var c in f.Character) Cats[c.character_id] = c;
                foreach (var s in f.Skill) CatSkills[s.skill_id] = s;
            }
            if (itemTable)
            {
                var f = JsonUtility.FromJson<ItemTableFile>(itemTable.text);
                foreach (var it in f.Item) { Items[it.item_id] = it; ItemsByCode[it.code_id] = it; }
                Zones.AddRange(f.Zone);
                Zones.Sort((a, b) => a.from_floor.CompareTo(b.from_floor));
                FurnitureLayouts.AddRange(f.Furniture_Layout);
            }
            if (tierTable) foreach (var t in JsonUtility.FromJson<TierTableFile>(tierTable.text).Tier) Tiers[t.tier] = t;
            if (humanTable) { var f = JsonUtility.FromJson<HumanTableFile>(humanTable.text); Humans.AddRange(f.Human); HumanLines.AddRange(f.Human_Line); }
            if (commonSkillTable)
            {
                var f = JsonUtility.FromJson<CommonSkillTableFile>(commonSkillTable.text);
                foreach (var s in f.Common_Skill)
                {
                    CommonSkills.Add(s); CommonSkillsById[s.skill_id] = s;
                    if (!CommonSkillsByTier.TryGetValue(s.tier, out var l)) CommonSkillsByTier[s.tier] = l = new List<CommonSkillRow>();
                    l.Add(s);
                }
                foreach (var b in f.Branch) SkillBranches[b.Branch] = b;
            }
            if (achievementTable) { var f = JsonUtility.FromJson<AchievementTableFile>(achievementTable.text); if (f.Achievement != null) { Achievements.AddRange(f.Achievement); Achievements.Sort((a, b) => a.sort_order.CompareTo(b.sort_order)); } }
            if (stageTable) { var f = JsonUtility.FromJson<StageTableFile>(stageTable.text); if (f.Stage != null) { Stages.AddRange(f.Stage); Stages.Sort((a, b) => a.floor.CompareTo(b.floor)); } }
            if (bossTable) { var f = JsonUtility.FromJson<BossTableFile>(bossTable.text); if (f.Boss != null) Bosses.AddRange(f.Boss); if (f.Boss_Line != null) BossLines.AddRange(f.Boss_Line); if (f.Atk_Type != null) foreach (var a in f.Atk_Type) BossAtks[a.atk_type] = a; Bosses.Sort((a, b) => a.floor.CompareTo(b.floor)); }
            if (tutorialTable)
            {
                var f = JsonUtility.FromJson<TutorialTableFile>(tutorialTable.text);
                if (f.Speaker != null) foreach (var r in f.Speaker) TutoSpeakers[r.speaker_id] = r;
                if (f.Step != null) { TutoSteps.AddRange(f.Step); TutoSteps.Sort((a, b) => a.sort.CompareTo(b.sort)); }
                if (f.Line != null) { f.Line.Sort((a, b) => a.line_id.CompareTo(b.line_id)); foreach (var r in f.Line) { if (!TutoLines.TryGetValue(r.step_id, out var l)) TutoLines[r.step_id] = l = new List<TutoLineRow>(); l.Add(r); } }
                if (f.Story != null) { Story.AddRange(f.Story); Story.Sort((a, b) => a.line_id.CompareTo(b.line_id)); }
            }
            shopCfg.Clear(); gachaCfg.Clear();
            if (shopTable) { Shop = JsonUtility.FromJson<ShopTableFile>(shopTable.text) ?? new(); if (Shop.Shop_Config != null) foreach (var r in Shop.Shop_Config) shopCfg[r.key] = r.value; }
            if (gachaTable) { Gacha = JsonUtility.FromJson<GachaTableFile>(gachaTable.text) ?? new(); if (Gacha.Gacha_Config != null) foreach (var r in Gacha.Gacha_Config) gachaCfg[r.key] = r.value; }
            // 글 칸을 지금 언어로 (<칸>_sk 키, 한국어면 그대로). 언어를 바꾸면 Load 를 다시 함 (Awake 에서 Loc.Changed 구독)
            if (!Loc.IsKorean)
            {
                Loc.ApplyAll(Rats.Values); Loc.ApplyAll(RatSkills.Values); Loc.ApplyAll(Ultimates.Values); Loc.ApplyAll(UltCaptions.Values); Loc.ApplyAll(Grades.Values);
                Loc.ApplyAll(GrowthNodes); Loc.ApplyAll(Cats.Values); Loc.ApplyAll(CatSkills.Values); Loc.ApplyAll(Items.Values); Loc.ApplyAll(Zones); Loc.ApplyAll(Tiers.Values);
                Loc.ApplyAll(Humans); Loc.ApplyAll(HumanLines); Loc.ApplyAll(CommonSkills); Loc.ApplyAll(SkillBranches.Values); Loc.ApplyAll(Achievements);
                Loc.ApplyAll(Bosses); Loc.ApplyAll(BossLines); Loc.ApplyAll(BossAtks.Values); Loc.ApplyAll(TutoSpeakers.Values); Loc.ApplyAll(Story);
                foreach (var l in TutoLines.Values) Loc.ApplyAll(l);
                if (Shop?.Shop_Goods != null) Loc.ApplyAll(Shop.Shop_Goods);
            }
            Debug.Log($"[GameDatabase] 쥐 {Rats.Count} · 스킬 {RatSkills.Count} · 필살기 {Ultimates.Count} · 등급 {Grades.Count} · 성장 노드 {GrowthNodes.Count} · 고양이 {Cats.Count} · 물건 {Items.Count} · 티어 {Tiers.Count} · 사람 {Humans.Count} · 공용 스킬 {CommonSkills.Count}");
        }

        // 층 → 그 층의 구간 (from_floor 이하 중 가장 높은 것)
        public ZoneRow ZoneOf(int floor) { ZoneRow z = Zones.Count > 0 ? Zones[0] : null; foreach (var o in Zones) if (floor >= o.from_floor) z = o; return z; }

        // 사람 대사: 상황(Panic·Hit·Fly·Poof·Item_Hit) + 사람 id (0 = 공통). 없으면 null
        public string HumanLine(string situation, int humanId)
        {
            var l = new List<string>();
            foreach (var r in HumanLines) if (r.situation == situation && (r.human_id == humanId || r.human_id == 0)) l.Add(r.text);
            return l.Count > 0 ? l[UnityEngine.Random.Range(0, l.Count)] : null;
        }

        // 층 밸런스: 표에 있으면 그 행, 마지막 층을 넘으면 마지막 두 층 비율로 이어서 계산한 행
        // 층 건너뛰기 상한: 최고 클리어 층 c 에서 고를 수 있는 가장 높은 시작 층 (스테이지 테이블 skip_cap, 기획서 26장)
        public int SkipCapFor(int cleared) { if (cleared < 1) return 1; var r = StageOf(cleared); return r != null && r.skip_cap > 0 ? r.skip_cap : 1; }
        public StageRow StageOf(int floor)
        {
            if (Stages.Count == 0) return null;
            floor = Mathf.Max(1, floor);
            foreach (var s in Stages) if (s.floor == floor) return s;
            var last = Stages[^1]; var prev = Stages.Count > 1 ? Stages[^2] : last;
            int n = floor - last.floor;
            float G(float a, float b) => b > 0 && a > 0 ? Mathf.Pow(a / b, n) : 1;
            return new StageRow
            {
                floor = floor, rooms = last.rooms, wall_stairs = last.wall_stairs, wall_normal = last.wall_normal, time_add = last.time_add,
                floor_type = last.floor_type, target_sec = last.target_sec, skip_cap = last.skip_cap, research = last.research * G(last.research, prev.research),
                pow_need = last.pow_need * G(last.pow_need, prev.pow_need), item_hp = last.item_hp * G(last.item_hp, prev.item_hp), cheese = last.cheese * G(last.cheese, prev.cheese),
            };
        }

        // 보스: 그 층에 나오는 보스 (없으면 null)
        // 보스: 그 층 보스. 표에 없는 보스 층(5층마다)은 표의 보스를 층 순서대로 반복 (웹 bossOf)
        public BossRow BossOf(int floor)
        {
            foreach (var b in Bosses) if (b.floor == floor) return b;
            if (Bosses.Count == 0 || floor % 5 != 0 || floor < Bosses[0].floor) return null;
            return Bosses[(floor / 5 - 1) % Bosses.Count];
        }
        public BossAtkRow BossAtk(string type) => !string.IsNullOrEmpty(type) && BossAtks.TryGetValue(type, out var a) ? a : null;
        public string BossLine(string situation, int bossId)
        {
            var l = new List<string>();
            foreach (var r in BossLines) if (r.situation == situation && r.boss_id == bossId) l.Add(r.text);
            return l.Count > 0 ? l[UnityEngine.Random.Range(0, l.Count)] : null;
        }

        public RatUltimateRow UltOf(RatCharacterRow r) => r != null && Ultimates.TryGetValue(r.ultimate, out var u) ? u : null;
        public UltCaptionRow UltCaption(int ultId, string key) => UltCaptions.TryGetValue((ultId, key), out var c) ? c : null;

        public RatGradeRow GradeOf(RatCharacterRow r) => Grades.TryGetValue(r.Grade, out var g) ? g : null;
        public GrowthNodeRow GrowthNodeById(int id) { foreach (var n in GrowthNodes) if (n.node_id == id) return n; return null; }
    }
}
