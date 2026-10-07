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
        [Tooltip("테스트: 공용 스킬 레벨 덮어쓰기 (코드 id · 레벨). 저장된 레벨보다 우선")] public List<SkillEntry> testSkillLevels = new();

        [Serializable] class Entry { public string code; public int shard, level; }
        [Serializable] public class SkillEntry { public string code; public int level; }
        [Serializable] public class AchvEntry { public int ult; public int count; }       // 필살기 업적: 필살기 id · 달성 횟수
        [Serializable] class SaveData
        {
            public List<Entry> rats = new(); public List<SkillEntry> skills = new(); public List<AchvEntry> achvs = new();
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

        // 공용 스킬 지도에서 노드 상태 (웹게임 mapState)
        public enum SkillState { Owned, Open, Hint, TierLock, Hidden }

        readonly Dictionary<string, Entry> rats = new();
        readonly Dictionary<string, int> skills = new();
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
        public event Action<string> SkillLeveled;
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

        // ── 공용 스킬 ──
        public int SkillLv(string code)
        {
            foreach (var t in testSkillLevels) if (t.code == code) return t.level;
            return skills.TryGetValue(code, out var v) ? v : 0;
        }
        public int SkillLv(CommonSkillRow s) => s != null ? SkillLv(s.code_id) : 0;
        public int SkillLv(CommonEffectType e) => GameDatabase.Instance.CommonSkillsByEffect.TryGetValue(e, out var s) ? SkillLv(s) : 0;
        public int SkillLevelSum { get { int n = 0; foreach (var s in GameDatabase.Instance.CommonSkills) n += SkillLv(s); return n; } }

        static CommonSkillRow Req(CommonSkillRow s) => s.req_skill != 0 && GameDatabase.Instance.CommonSkillsById.TryGetValue(s.req_skill, out var r) ? r : null;
        bool ReqOk(CommonSkillRow s) { var r = Req(s); return r == null || SkillLv(r) >= s.req_level; }
        public bool IsMax(CommonSkillRow s) => !s.Infinite && SkillLv(s) >= s.max_level;

        // 찍었음 / 찍을 수 있음 / 선행이 모자람(? 로 보임) / 티어 부족 / 숨김
        public SkillState StateOf(CommonSkillRow s, int tier)
        {
            if (SkillLv(s) > 0) return SkillState.Owned;
            var r = Req(s);
            if (tier < s.unlock_tier) return r == null || SkillLv(r) > 0 ? SkillState.TierLock : SkillState.Hidden;
            if (ReqOk(s)) return SkillState.Open;
            var rr = r != null ? Req(r) : null;
            return SkillLv(r) > 0 || rr == null || SkillLv(rr) >= r.req_level ? SkillState.Hint : SkillState.Hidden;
        }

        public double SkillCost(CommonSkillRow s) => s.Cost(SkillLv(s));
        public bool SkillReqMet(CommonSkillRow s) => tier >= s.unlock_tier && ReqOk(s) && !IsMax(s);
        public bool CanBuySkill(CommonSkillRow s) => SkillReqMet(s) && cheese >= SkillCost(s);

        // 한 레벨 올림 (저장된 치즈에서 차감, 판 밖에서만). 성공 여부
        public bool BuySkill(CommonSkillRow s)
        {
            if (!CanBuySkill(s)) return false;
            cheese -= SkillCost(s);
            skills[s.code_id] = (skills.TryGetValue(s.code_id, out var v) ? v : 0) + 1;
            Save(); SkillLeveled?.Invoke(s.code_id);
            return true;
        }

        public void Save()
        {
            var d = new SaveData { cheese = cheese, research = research, tier = tier, maxFloor = maxFloor, runs = runs }; d.rats.AddRange(rats.Values);
            foreach (var kv in skills) d.skills.Add(new SkillEntry { code = kv.Key, level = kv.Value });
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
            if (d.skills != null) foreach (var e in d.skills) skills[e.code] = e.level;
            if (d.achvs != null) foreach (var e in d.achvs) achvs[e.ult] = e.count;
        }

        [ContextMenu("진행도 초기화")]
        public void ResetAll() { rats.Clear(); skills.Clear(); achvs.Clear(); treeCache.Clear(); cheese = research = 0; tier = maxFloor = 1; runs = 0; PlayerPrefs.DeleteKey(saveKey); }

        // 시작 층 최대 = min(티어 테이블 start_floor_cap, 최고 기록) (웹 startFloorCap)
        public int StartFloorCap(int t = 0)
        {
            if (t <= 0) t = tier;
            int cap = GameDatabase.Instance && GameDatabase.Instance.Tiers.TryGetValue(t, out var row) ? row.start_floor_cap : 1;
            return Mathf.Max(1, Mathf.Min(cap, maxFloor));
        }
    }
}
