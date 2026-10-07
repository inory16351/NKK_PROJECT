using System;
using System.Collections.Generic;
using NKK.Data;
using UnityEngine;

namespace NKK
{
    // 저장되는 진행도: 종별 조각 · 조각 강화 레벨 · 만난 종 · 공용 스킬 레벨 · 치즈 · 연구자료 · 티어 · 기록
    // (웹게임 S.shard / S.rlv / S.seen / S.skills / S.cheese / S.research / S.rank / S.maxFloor / S.runs).
    // 조각 강화 레벨 L → 성장 테이블 Growth_Order 순서대로 노드가 자동으로 찍힘 (웹게임 autoTree).
    [DefaultExecutionOrder(-900)]
    public class Progress : MonoBehaviour
    {
        public static Progress I { get; private set; }

        [Header("테스트")]
        [Tooltip("로비가 생기기 전 임시: 조각이 모이면 판 중에 바로 강화 (웹게임은 로비에서 강화)")] public bool autoUpgradeInRun = true;
        [Tooltip("저장 키 (PlayerPrefs)")] public string saveKey = "nkk_progress_v1";
        [Tooltip("테스트: 이 노드 id 들은 활성화된 것으로 침 (저장 안 함)")] public List<int> testSkills = new();
        [Tooltip("테스트: 이 훈장 이하 트리는 전부 활성화된 것으로 침 (0 = 끔)")] public int testSkillTier;

        [Serializable] class Entry { public string code; public int shard, level; }
        [Serializable] public class SkillEntry { public string code; public int level; }
        [Serializable] public class AchvEntry { public int ult; public int count; }       // 필살기 업적: 필살기 id · 달성 횟수
        [Serializable] class SaveData
        {
            public List<Entry> rats = new(); public List<SkillEntry> skills = new(); public List<AchvEntry> achvs = new();
            public List<int> nodes = new();      // 활성화한 공용 스킬 노드 (훈장별 트리). skills 는 예전 레벨식 (안 씀)
            public double cheese, research; public int tier = 1, maxFloor = 1, runs;
        }

        // ── 재화 · 티어 · 기록 (판이 끝나도 남음) ──
        [Header("현재 값 (저장됨, 보기용)")]
        public double cheese;
        [Tooltip("연구자료 (훈장 승급)")] public double research;
        [Tooltip("티어 (훈장)")] public int tier = 1;
        [Tooltip("최고 층 기록")] public int maxFloor = 1;
        [Tooltip("탈출 시도 횟수")] public int runs;

        // 로비 → 게임 씬으로 넘기는 시작 층 (0 = 게임 씬을 바로 켬 → GameManager 인스펙터 값)
        public static int PendingStartFloor;
        public bool SpendCheese(double v) { if (v < 0 || cheese < v) return false; cheese -= v; Save(); return true; }
        public void OnFloorReached(int f) { if (f > maxFloor) { maxFloor = f; Save(); } }

        // 공용 스킬 지도에서 노드 상태: 활성화 · 열림(살 수 있음) · 잠김(이어진 노드가 아직) · 훈장 부족
        public enum SkillState { Owned, Open, Locked, TierLock }

        readonly Dictionary<string, Entry> rats = new();
        readonly HashSet<int> skills = new();
        // 노드가 바뀔 때마다 +1 (CommonSkill 이 효과 합을 다시 계산)
        public int SkillVersion { get; private set; }
        int testHash;
        // ── 업적 (필살기를 끝까지 쓰면 달성. 로비 표시는 나중에 찍찍!! 훈장과 연동) ──
        readonly Dictionary<int, int> achvs = new();
        public event Action<int> AchievementGot;
        public bool HasAchievement(int ultId) => achvs.ContainsKey(ultId);
        public int AchievementCount(int ultId) => achvs.TryGetValue(ultId, out var n) ? n : 0;
        public int AchievementTotal => achvs.Count;
        public IEnumerable<int> Achievements => achvs.Keys;
        // 처음 달성이면 true
        public bool OnAchievement(int ultId)
        {
            bool first = !achvs.ContainsKey(ultId);
            achvs[ultId] = AchievementCount(ultId) + 1;
            Save(); AchievementGot?.Invoke(ultId);
            return first;
        }
        public event Action<int> SkillLeveled;
        readonly Dictionary<int, Dictionary<GrowthEffectType, int>> treeCache = new();
        public event Action<string> RatLeveled;

        void Awake()
        {
            if (I && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            Load();
        }

        Entry Get(string code) { if (!rats.TryGetValue(code, out var e)) rats[code] = e = new Entry { code = code }; return e; }

        public bool Seen(string code) => rats.ContainsKey(code);
        public int Level(string code) => rats.TryGetValue(code, out var e) ? e.level : 0;
        public void SetLevelForTest(string code, int level) { Get(code).level = level; treeCache.Clear(); }      // 밸런스 측정용
        public int Shards(string code) => rats.TryGetValue(code, out var e) ? e.shard : 0;

        // 레벨 L → L+1 에 필요한 조각 = 올림((L+1) × 등급 조각 계수)
        public static int ShardNeed(RatGradeRow g, int L) => Mathf.CeilToInt((L + 1) * (g != null ? g.shard_k : 1));
        public static int ShardsFor(RatGradeRow g, int L) { int n = 0; for (int l = 0; l < L; l++) n += ShardNeed(g, l); return n; }
        public int MaxLevel { get { int m = 0; foreach (var gn in GameDatabase.Instance.GrowthNodes) m += gn.max_level; return m; } }

        // 처음 만나면 기록만, 이미 만난 종이면 조각 +1. 반환: 처음 만남 여부
        public bool OnRatObtained(RatCharacterRow row)
        {
            bool isNew = !rats.ContainsKey(row.code_id);
            var e = Get(row.code_id);
            if (!isNew) e.shard++;
            if (autoUpgradeInRun) while (TryUpgrade(row)) { }
            Save();
            return isNew;
        }

        public bool CanUpgrade(RatCharacterRow row)
        {
            var g = GameDatabase.Instance.GradeOf(row); var e = Get(row.code_id);
            return e.level < MaxLevel && e.shard - ShardsFor(g, e.level) >= ShardNeed(g, e.level);
        }

        public bool TryUpgrade(RatCharacterRow row)
        {
            if (!CanUpgrade(row)) return false;
            Get(row.code_id).level++;
            RatLeveled?.Invoke(row.code_id);
            return true;
        }

        // 레벨 L 까지 찍힌 노드 (효과 타입 → 레벨). 앞쪽은 정해진 순서, 이후 반복 구간을 돌며 찍을 수 있는 것부터
        public Dictionary<GrowthEffectType, int> Tree(int L)
        {
            if (treeCache.TryGetValue(L, out var cached)) return cached;
            var db = GameDatabase.Instance;
            var byId = new Dictionary<int, GrowthNodeRow>(); foreach (var gn in db.GrowthNodes) byId[gn.node_id] = gn;
            var lv = new Dictionary<int, int>();
            int LvOf(int id) => lv.TryGetValue(id, out var v) ? v : 0;
            bool Ok(GrowthNodeRow s) => LvOf(s.node_id) < s.max_level && (s.req_node == 0 || LvOf(s.req_node) >= s.req_level);
            var fixedOrder = new List<int>(); var repeat = new List<int>();
            foreach (var o in db.GrowthOrder) (o.is_repeat == 1 ? repeat : fixedOrder).Add(o.node_id);
            int n = 0, i = 0, guard = 0;
            while (n < L && guard++ < 5000)
            {
                int id = i < fixedOrder.Count ? fixedOrder[i] : repeat.Count > 0 ? repeat[(i - fixedOrder.Count) % repeat.Count] : 0; i++;
                byId.TryGetValue(id, out var s);
                if (s == null || !Ok(s)) { s = null; foreach (var c in db.GrowthNodes) if (Ok(c)) { s = c; break; } }   // 순서상 못 찍으면 찍을 수 있는 아무거나
                if (s == null) break;
                lv[s.node_id] = LvOf(s.node_id) + 1; n++;
            }
            var res = new Dictionary<GrowthEffectType, int>();
            foreach (var kv in lv) res[byId[kv.Key].Effect] = kv.Value;
            treeCache[L] = res;
            return res;
        }

        // ── 공용 스킬 (훈장별 트리, 노드 하나 = 한 번 활성화) ──
        void Update()
        {
            // 인스펙터 테스트 값이 바뀌면 효과 다시 계산
            int h = testSkillTier * 7919; foreach (var id in testSkills) h = h * 31 + id;
            if (h != testHash) { testHash = h; SkillVersion++; }
        }

        // 활성화됨: 시작점은 그 훈장이면 자동, 나머지는 산 노드 (+ 테스트)
        public bool HasSkill(CommonSkillRow s)
        {
            if (s == null) return false;
            if (s.IsRoot) return tier >= s.tier || testSkillTier >= s.tier;
            return skills.Contains(s.skill_id) || testSkills.Contains(s.skill_id) || (testSkillTier > 0 && s.tier <= testSkillTier);
        }
        public bool HasSkill(int id) => GameDatabase.Instance && GameDatabase.Instance.CommonSkillsById.TryGetValue(id, out var s) && HasSkill(s);
        // 활성화한 노드 수 (시작점 제외) — 훈장 승급 조건
        public int SkillCount { get { int n = 0; foreach (var s in GameDatabase.Instance.CommonSkills) if (!s.IsRoot && HasSkill(s)) n++; return n; } }

        bool LinkOwned(CommonSkillRow s)
        {
            var db = GameDatabase.Instance;
            return (s.link_1 != 0 && db.CommonSkillsById.TryGetValue(s.link_1, out var a) && HasSkill(a)) || (s.link_2 != 0 && db.CommonSkillsById.TryGetValue(s.link_2, out var b) && HasSkill(b));
        }

        public SkillState StateOf(CommonSkillRow s)
        {
            if (HasSkill(s)) return SkillState.Owned;
            if (tier < s.tier) return SkillState.TierLock;
            return LinkOwned(s) ? SkillState.Open : SkillState.Locked;
        }
        public bool CanAffordSkill(CommonSkillRow s) => cheese >= s.cost_cheese && research >= s.cost_research;
        public bool CanBuySkill(CommonSkillRow s) => StateOf(s) == SkillState.Open && CanAffordSkill(s);

        // 활성화 (저장된 치즈·연구자료에서 차감, 판 밖에서만). 성공 여부
        public bool BuySkill(CommonSkillRow s)
        {
            if (!CanBuySkill(s)) return false;
            cheese -= s.cost_cheese; research -= s.cost_research;
            skills.Add(s.skill_id); SkillVersion++;
            Save(); SkillLeveled?.Invoke(s.skill_id);
            return true;
        }

        // ── 찍찍!! 훈장 승급 (티어 테이블: 연구자료 비용 + 조건 3개) ──
        public float CondValue(string type) => type switch
        {
            "Max_Floor" => maxFloor,
            "Shard_Level_Sum" => ShardLevelSum,
            "Skill_Node_Count" => SkillCount,
            _ => 0,
        };
        public int ShardLevelSum { get { int n = 0; foreach (var e in rats.Values) n += e.level; return n; } }
        public TierRow NextTier => GameDatabase.Instance.Tiers.TryGetValue(tier + 1, out var t) ? t : null;
        public bool CondOk(string type, float need) => string.IsNullOrEmpty(type) || type == "None" || CondValue(type) >= need;
        public bool CanRankUp()
        {
            var t = NextTier; if (t == null) return false;
            return research >= t.research_cost && CondOk(t.cond1_type, t.cond1_value) && CondOk(t.cond2_type, t.cond2_value) && CondOk(t.cond3_type, t.cond3_value);
        }
        public event Action<int> RankedUp;
        public bool RankUp()
        {
            if (!CanRankUp()) return false;
            research -= NextTier.research_cost; tier++; SkillVersion++;
            Save(); RankedUp?.Invoke(tier);
            return true;
        }

        public void Save()
        {
            var d = new SaveData { cheese = cheese, research = research, tier = tier, maxFloor = maxFloor, runs = runs }; d.rats.AddRange(rats.Values);
            d.nodes.AddRange(skills);
            foreach (var kv in achvs) d.achvs.Add(new AchvEntry { ult = kv.Key, count = kv.Value });
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(d)); PlayerPrefs.Save();
        }

        void Load()
        {
            rats.Clear(); skills.Clear(); achvs.Clear(); cheese = research = 0; tier = maxFloor = 1; runs = 0;
            var s = PlayerPrefs.GetString(saveKey, "");
            if (string.IsNullOrEmpty(s)) return;
            var d = JsonUtility.FromJson<SaveData>(s);
            cheese = d.cheese; research = d.research; tier = Mathf.Max(1, d.tier); maxFloor = Mathf.Max(1, d.maxFloor); runs = d.runs;
            foreach (var e in d.rats) rats[e.code] = e;
            if (d.nodes != null) foreach (var id in d.nodes) skills.Add(id);
            SkillVersion++;
            if (d.achvs != null) foreach (var e in d.achvs) achvs[e.ult] = e.count;
        }

        [ContextMenu("진행도 초기화")]
        public void ResetAll() { rats.Clear(); skills.Clear(); achvs.Clear(); treeCache.Clear(); cheese = research = 0; tier = maxFloor = 1; runs = 0; SkillVersion++; PlayerPrefs.DeleteKey(saveKey); }

        // 시작 층 최대 = min(1 + 스테이지 스킵 노드 합, 최고 기록)
        public int StartFloorCap(int t = 0) => Mathf.Max(1, Mathf.Min(1 + CommonSkill.StageSkip, maxFloor));
    }
}
