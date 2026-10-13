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
        [Tooltip("테스트: 조각이 모이면 판 중에 바로 강화 (평소엔 꺼 둠 — 로비 쳇바퀴 훈련에서 강화)")] public bool autoUpgradeInRun;
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
            public List<int> achvOn = new();     // 업적 스위치: 달성한 업적 id (이 저장 파일 기준, 한 번 켜지면 계속 켜짐)
            public double cheese, research; public int tier = 1, maxFloor = 1, runs;
            public double playSec; public int rankRuns;     // 누적 플레이 시간(초) · 마지막 훈장을 달았을 때의 탈출 시도 횟수 (서버 기록용, 2026-10-13)
            public int floorRuns;                // 최고 층에 처음 닿았을 때의 탈출 시도 횟수 (랭킹 동점 순위용)
            public int ver;                      // 0 = 튜토리얼 전 저장 (그대로 이어 하면 튜토리얼 끝난 것으로 침)
            public List<string> tuto = new();    // 본 튜토리얼 단계 id
            public List<string> unlocks = new(); // 해금한 기능 id (튜토리얼 테이블 Unlock_Id)
            public bool tutoSkip;                // 튜토리얼 전 저장 → 모든 단계·기능을 연 것으로 침
            public string savedAt;               // 마지막 저장 시각 (타이틀 슬롯 표시용)
            public ShopSave shop; public GachaSave gacha;       // 쥐구멍 암시장 · 유전자 뽑기 (2026-10-12)
            public bool infinite;                // 테스트: 치즈·연구자료 무제한 (테스트 패널 "전부 해제")
        }
        // ── 쥐구멍 암시장 (상점 기획서 Shop_Save) · 유전자 뽑기 (뽑기 기획서 Gacha_Save) ──
        [Serializable] public class ShopItem { public int slot, goods; public string code; public bool sold; }
        [Serializable] public class ShopSave
        {
            public List<ShopItem> lineup = new();
            public float playLeft = -1;                    // 다음 입고까지 남은 탈출 시간 (초, 판에서 보낸 시간만 줄어듦. -1 = 아직 없음)
            public int rerollCount, freeLeft, lockedSlot, pity;     // 이번 주기 유료 리롤 횟수 · 남은 무료 리롤 · 고정 슬롯(0 = 없음) · 신화 없는 라인업 연속
            public bool opened;                            // 첫 방문 선물 받음
        }
        [Serializable] public class GachaLog { public long at; public int maxFloor; public string code, grade, source; public int shards; public bool isNew; }
        [Serializable] public class GachaSave
        {
            public int legendPity, mythicPity, total, freeTen = -1;     // 천장 누적 · 누적 뽑기 · 남은 무료 10회 (-1 = 아직 해금 안 됨)
            public string wishLegend, wishMythic;           // 관심 종 (코드)
            public List<GachaLog> log = new();               // 최근 기록
            public List<GachaLog> pending = new();           // 연출 전 확정된 결과 (강제 종료 복구)
            public List<string> introSeen = new();           // 고유 컷씬을 끝까지 본 쥐 (코드) — 본 컷씬은 클릭 한 번으로 건너뛰기 (사용자 2026-10-13)
        }
        public ShopSave shop = new();
        public GachaSave gacha = new();
        // 클리어한 최고 층 (최고 도달 층 - 1)
        public int ClearedFloor => Mathf.Max(0, maxFloor - 1);
        // 조각 지급 (상점·뽑기). 처음 만난 종이면 도감 등록도. 반환 = 처음 만남
        public bool AddShards(string code, int n)
        {
            bool isNew = !rats.ContainsKey(code);
            Get(code).shard += Mathf.Max(0, n);
            return isNew;
        }
        const int SaveVer = 1;

        // ── 재화 · 티어 · 기록 (판이 끝나도 남음) ──
        [Header("현재 값 (저장됨, 보기용)")]
        public double cheese;
        [Tooltip("연구자료 (훈장 승급)")] public double research;
        [Tooltip("티어 (훈장)")] public int tier = 1;
        [Tooltip("최고 층 기록")] public int maxFloor = 1;
        [Tooltip("탈출 시도 횟수")] public int runs;
        [Tooltip("누적 플레이 시간 (초, 타이틀·일시정지·창 비활성 제외)")] public double playSec;
        [Tooltip("마지막 훈장을 달았을 때의 탈출 시도 횟수")] public int rankRuns;
        [Tooltip("최고 층에 처음 닿았을 때의 탈출 시도 횟수 (랭킹 동점 순위)")] public int floorRuns;
        // 최고 층 기록이 오름 (랭킹 갱신)
        public event Action<int> FloorRecord;
        // 방금 단 훈장까지 걸린 시도 횟수 (이전 훈장 이후, RankUp 직후 값)
        public int RunsSinceLastRank { get; private set; }

        // 로비 → 게임 씬으로 넘기는 시작 층 (0 = 게임 씬을 바로 켬 → GameManager 인스펙터 값)
        public static int PendingStartFloor;

        // ── 저장 슬롯 (1~5) ── 슬롯 1 = 예전 저장 키 그대로, 2~5 = 키 뒤에 _s2 …
        public const int SlotCount = 5;
        const string SlotPref = "nkk_slot";
        public static int Slot { get; private set; } = 1;
        string KeyOf(int slot) => slot <= 1 ? saveKey : saveKey + "_s" + slot;
        public bool SlotExists(int slot) => PlayerPrefs.HasKey(KeyOf(slot));
        // 슬롯 요약 (타이틀 화면): 없으면 false
        public bool SlotSummary(int slot, out int sTier, out int sMaxFloor, out int sRuns, out string sSavedAt)
        {
            sTier = sMaxFloor = 1; sRuns = 0; sSavedAt = "";
            var s = PlayerPrefs.GetString(KeyOf(slot), ""); if (string.IsNullOrEmpty(s)) return false;
            var d = JsonUtility.FromJson<SaveData>(s);
            sTier = Mathf.Max(1, d.tier); sMaxFloor = Mathf.Max(1, d.maxFloor); sRuns = d.runs; sSavedAt = d.savedAt ?? "";
            return true;
        }
        // 슬롯 고르기 (이어 하기) / 새로 시작 (그 슬롯을 지우고 처음부터)
        public void UseSlot(int slot) { Slot = Mathf.Clamp(slot, 1, SlotCount); PlayerPrefs.SetInt(SlotPref, Slot); Load(); }
        public void NewGame(int slot) { Slot = Mathf.Clamp(slot, 1, SlotCount); PlayerPrefs.SetInt(SlotPref, Slot); PlayerPrefs.DeleteKey(KeyOf(Slot)); Load(); Save(); }
        public void DeleteSlot(int slot) { PlayerPrefs.DeleteKey(KeyOf(slot)); PlayerPrefs.Save(); }

        // ── 튜토리얼 · 기능 해금 ──
        readonly HashSet<string> tuto = new(), unlocks = new();
        bool tutoSkip;
        // 처음부터 열려 있는 기능 (작전 회의 · 낮잠 침대)
        static readonly string[] OpenAtStart = { "run", "rec", "lb" };     // lb = 랭킹 (MVP, 처음부터 열림)
        // 해금이 바뀔 때마다 +1 (FeatureGate 가 다시 그림)
        public int UnlockVersion { get; private set; }
        public bool TutoSkipped => tutoSkip;
        public void SkipTutorial() { tutoSkip = true; UnlockVersion++; Save(); }      // 밸런스 측정 등: 튜토리얼 안 띄우고 기능 전부 열림
        public bool TutoDone(string stepId) => tutoSkip || tuto.Contains(stepId);
        public void MarkTuto(string stepId) { if (tuto.Add(stepId)) { UnlockVersion++; Save(); } }      // 튜토리얼로 드러나는 기능(뒷골목)이 있어서 FeatureGate 도 다시 그림
        public bool IsUnlocked(string feature)
        {
            // 층으로 열리는 기능 (2026-10-12): 뒷골목·유전자 뽑기 = 16층 클리어 · 쥐구멍 암시장 = 17층 클리어 (튜토리얼 건너뛰기와 상관없음)
            if (feature == "gacha") return GeneGacha.Unlocked;
            // 뒷골목(맨홀)은 16층 클리어 뒤에도 그 튜토리얼이 시작될 때까지 아예 안 보임 (사용자 2026-10-13: 열릴 때 튜토리얼이랑 같이 등장)
            if (feature == "alley") return GeneGacha.Unlocked && (string.IsNullOrEmpty(alleyRevealStep) || TutoDone(alleyRevealStep));
            if (feature == "market") return BlackMarket.Unlocked;
            return string.IsNullOrEmpty(feature) || tutoSkip || unlocks.Contains(feature) || Array.IndexOf(OpenAtStart, feature) >= 0;
        }
        [Tooltip("테스트: 층과 상관없이 뒷골목·뽑기·암시장 열기")] public bool testMarket;
        [Tooltip("뒷골목(맨홀)이 드러나는 튜토리얼 단계 id — 이 단계가 시작돼야 보임 (비우면 16층 클리어만으로)")] public string alleyRevealStep = "l_alley";
        // 쉼표로 여러 개. 새로 열린 게 있으면 true
        public bool Unlock(string features)
        {
            if (string.IsNullOrEmpty(features)) return false;
            bool any = false;
            foreach (var f in features.Split(',')) { var t = f.Trim(); if (t.Length > 0 && unlocks.Add(t)) any = true; }
            if (any) { UnlockVersion++; Save(); }
            return any;
        }
        public bool SpendCheese(double v) { if (v < 0 || cheese < v) return false; cheese -= v; Save(); return true; }
        public void OnFloorReached(int f) { if (f > maxFloor) { maxFloor = f; floorRuns = runs; Save(); FloorRecord?.Invoke(f); } }

        // 공용 스킬 지도에서 노드 상태: 활성화 · 열림(살 수 있음) · 잠김(이어진 노드가 아직) · 훈장 부족
        public enum SkillState { Owned, Open, Locked, TierLock }

        readonly Dictionary<string, Entry> rats = new();
        readonly HashSet<int> skills = new();
        // 노드가 바뀔 때마다 +1 (CommonSkill 이 효과 합을 다시 계산)
        public int SkillVersion { get; private set; }
        int testHash;
        // ── 업적 (업적 테이블 Achievement) ──
        // 저장하는 건 조건의 바탕 기록뿐 (지금: 필살기별 완주 횟수, 저장 이름 achvs 는 예전 그대로). 달성 여부는 업적 테이블 조건으로 계산
        readonly Dictionary<int, int> achvs = new();
        readonly HashSet<int> achvOn = new();         // 업적 스위치 (저장 파일마다)
        public bool AchvSwitch(int achvId) => achvOn.Contains(achvId);
        public int UltUses(int ultId) => achvs.TryGetValue(ultId, out var n) ? n : 0;
        // 업적 테이블 한 줄의 진행 수 (조건 타입별) · 달성 여부
        public int AchvProgress(AchievementRow a) => a == null ? 0 : a.cond_type switch { "Ult_Use" => UltUses(a.target_id), _ => 0 };
        public bool AchvDone(AchievementRow a) => a != null && achvOn.Contains(a.achv_id);       // 달성 = 스위치가 켜짐
        // 업적 달성 알림 — 스위치가 처음 켜질 때 한 번만 (게임 화면 AchievementToast 가 받음)
        public event Action<AchievementRow> AchvGot;

        // 필살기를 끝까지 씀 → Ult_Use 업적 확인
        public void OnUltUsed(int ultId)
        {
            achvs[ultId] = UltUses(ultId) + 1;
            Save();
            CheckAchv("Ult_Use", ultId);
        }
        void CheckAchv(string type, int target)
        {
            var db = GameDatabase.Instance; if (!db) return;
            foreach (var a in db.Achievements)
            {
                if (a.cond_type != type || a.target_id != target) continue;
                if (achvOn.Contains(a.achv_id) || AchvProgress(a) < Mathf.Max(1, a.need)) continue;     // 이미 켜진 스위치 · 조건 미달은 무시
                achvOn.Add(a.achv_id); Save();
                AchvGot?.Invoke(a);
            }
        }
        public event Action<int> SkillLeveled;
        readonly Dictionary<int, Dictionary<GrowthEffectType, int>> treeCache = new();
        public event Action<string> RatLeveled;

        void Awake()
        {
            if (I && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            Slot = Mathf.Clamp(PlayerPrefs.GetInt(SlotPref, 1), 1, SlotCount);
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

        // 레벨 L 까지 찍힌 노드 (효과 타입 → 레벨)
        public Dictionary<GrowthEffectType, int> Tree(int L)
        {
            if (treeCache.TryGetValue(L, out var cached)) return cached;
            var db = GameDatabase.Instance;
            var seq = Seq(L); var res = new Dictionary<GrowthEffectType, int>();
            for (int i = 0; i < L && i < seq.Count; i++) { var e = db.GrowthNodeById(seq[i])?.Effect ?? GrowthEffectType.None; res[e] = (res.TryGetValue(e, out var v) ? v : 0) + 1; }
            treeCache[L] = res;
            return res;
        }

        // 레벨 L 에 찍히는 노드 (1부터) — 쳇바퀴 훈련 화면의 성장 길
        public GrowthNodeRow NodeAt(int L) { var seq = Seq(L); return L >= 1 && L <= seq.Count ? GameDatabase.Instance.GrowthNodeById(seq[L - 1]) : null; }
        // 그 효과 노드가 처음 찍히는 레벨 (없으면 0) — 특수 액션 Lv 3 · 필살기 Lv 7 같은 해금 표시
        public int UnlockLevel(GrowthEffectType e) { for (int L = 1; L <= MaxLevel; L++) if (NodeAt(L)?.Effect == e) return L; return 0; }
        public bool UltUnlocked(string code) => Tree(Level(code)).ContainsKey(GrowthEffectType.Ult_Unlock);

        // 레벨마다 찍히는 노드 id 순서. 앞쪽은 정해진 순서(Growth_Order), 이후 반복 구간을 돌며 찍을 수 있는 것부터
        readonly List<int> seqCache = new();
        List<int> Seq(int L)
        {
            if (seqCache.Count >= L) return seqCache;
            var db = GameDatabase.Instance;
            var lv = new Dictionary<int, int>();
            int LvOf(int id) => lv.TryGetValue(id, out var v) ? v : 0;
            bool Ok(GrowthNodeRow s) => LvOf(s.node_id) < s.max_level && (s.req_node == 0 || LvOf(s.req_node) >= s.req_level);
            var fixedOrder = new List<int>(); var repeat = new List<int>();
            foreach (var o in db.GrowthOrder) (o.is_repeat == 1 ? repeat : fixedOrder).Add(o.node_id);
            seqCache.Clear();
            int i = 0, guard = 0, max = Mathf.Max(L, MaxLevel);
            while (seqCache.Count < max && guard++ < 5000)
            {
                int id = i < fixedOrder.Count ? fixedOrder[i] : repeat.Count > 0 ? repeat[(i - fixedOrder.Count) % repeat.Count] : 0; i++;
                var s = db.GrowthNodeById(id);
                if (s == null || !Ok(s)) { s = null; foreach (var c in db.GrowthNodes) if (Ok(c)) { s = c; break; } }   // 순서상 못 찍으면 찍을 수 있는 아무거나
                if (s == null) break;
                lv[s.node_id] = LvOf(s.node_id) + 1; seqCache.Add(s.node_id);
            }
            return seqCache;
        }

        // 다 같이 훈련: 강화할 수 있는 종을 전부 올릴 수 있는 만큼. 반환 = 올린 횟수
        public int UpgradeAll()
        {
            int n = 0;
            foreach (var r in GameDatabase.Instance.Rats.Values) if (Seen(r.code_id) && r.unlock_rank <= tier) while (TryUpgrade(r)) n++;
            if (n > 0) Save();
            return n;
        }
        public int UpgradableCount { get { int n = 0; foreach (var r in GameDatabase.Instance.Rats.Values) if (Seen(r.code_id) && r.unlock_rank <= tier && CanUpgrade(r)) n++; return n; } }

        // ── 공용 스킬 (훈장별 트리, 노드 하나 = 한 번 활성화) ──
        void Update()
        {
            // 인스펙터 테스트 값이 바뀌면 효과 다시 계산
            int h = testSkillTier * 7919; foreach (var id in testSkills) h = h * 31 + id;
            if (h != testHash) { testHash = h; SkillVersion++; }
            if (BlackMarket.TestOpen != testMarket) { BlackMarket.TestOpen = GeneGacha.TestOpen = testMarket; UnlockVersion++; }
            // 누적 플레이 시간: 타이틀·일시정지·창 비활성 제외
            if (Application.isFocused && !FxManager.Paused && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Title") playSec += Time.unscaledDeltaTime;
            // 무제한: 써서 줄어든 치즈·연구자료를 다시 채움
            if (Infinite) { if (cheese < InfiniteAmount) cheese = InfiniteAmount; if (research < InfiniteAmount) research = InfiniteAmount; }
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
        public int SkillCountIn(int t) { int n = 0; foreach (var s in GameDatabase.Instance.CommonSkills) if (s.tier == t && !s.IsRoot && HasSkill(s)) n++; return n; }     // 그 훈장 트리에서 찍은 수

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
            "Skill_Node_Count" => SkillCountIn(tier),        // 지금 훈장 트리에서 찍은 노드 수
            "Skill_Tree_Complete" => SkillCountUpTo(tier),   // 지금 훈장까지 트리에서 찍은 노드 수 (필요 = 전부)
            _ => 0,
        };
        // 조건 필요값: Skill_Tree_Complete 는 테이블 값 대신 지금 훈장까지 트리 노드 전부
        public float CondNeed(string type, float value) => type == "Skill_Tree_Complete" ? SkillTotalUpTo(tier) : value;
        public int SkillCountUpTo(int t) { int n = 0; foreach (var s in GameDatabase.Instance.CommonSkills) if (s.tier <= t && !s.IsRoot && HasSkill(s)) n++; return n; }
        public int SkillTotalUpTo(int t) { int n = 0; foreach (var s in GameDatabase.Instance.CommonSkills) if (s.tier <= t && !s.IsRoot) n++; return n; }
        public int ShardLevelSum { get { int n = 0; foreach (var e in rats.Values) n += e.level; return n; } }
        public TierRow NextTier => GameDatabase.Instance.Tiers.TryGetValue(tier + 1, out var t) ? t : null;
        public bool CondOk(string type, float need) => string.IsNullOrEmpty(type) || type == "None" || CondValue(type) >= CondNeed(type, need);
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
            RunsSinceLastRank = runs - rankRuns; rankRuns = runs;
            Save(); RankedUp?.Invoke(tier);
            return true;
        }

        public void Save()
        {
            var d = new SaveData { cheese = cheese, research = research, tier = tier, maxFloor = maxFloor, runs = runs, playSec = playSec, rankRuns = rankRuns, floorRuns = floorRuns, ver = SaveVer, tutoSkip = tutoSkip, infinite = Infinite, savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm") }; d.rats.AddRange(rats.Values);
            d.tuto.AddRange(tuto); d.unlocks.AddRange(unlocks);
            d.nodes.AddRange(skills);
            d.achvOn.AddRange(achvOn);
            d.shop = shop; d.gacha = gacha;
            foreach (var kv in achvs) d.achvs.Add(new AchvEntry { ult = kv.Key, count = kv.Value });     // 필살기 완주 횟수
            PlayerPrefs.SetString(KeyOf(Slot), JsonUtility.ToJson(d)); PlayerPrefs.Save();
        }

        void Load()
        {
            rats.Clear(); skills.Clear(); achvs.Clear(); achvOn.Clear(); treeCache.Clear(); cheese = research = 0; tier = maxFloor = 1; runs = 0; playSec = 0; rankRuns = 0; floorRuns = 0;
            tuto.Clear(); unlocks.Clear(); tutoSkip = false; Infinite = false; UnlockVersion++; SkillVersion++; shop = new(); gacha = new();
            var s = PlayerPrefs.GetString(KeyOf(Slot), "");
            if (string.IsNullOrEmpty(s)) return;
            var d = JsonUtility.FromJson<SaveData>(s);
            cheese = d.cheese; research = d.research; tier = Mathf.Max(1, d.tier); maxFloor = Mathf.Max(1, d.maxFloor); runs = d.runs; playSec = d.playSec; rankRuns = d.rankRuns; floorRuns = d.floorRuns > 0 || maxFloor <= 1 ? d.floorRuns : runs;     // 예전 저장 = 지금 시도 횟수로
            foreach (var e in d.rats) rats[e.code] = e;
            if (d.nodes != null) foreach (var id in d.nodes) skills.Add(id);
            SkillVersion++;
            if (d.achvs != null) foreach (var e in d.achvs) achvs[e.ult] = e.count;
            if (d.achvOn != null) foreach (var id in d.achvOn) achvOn.Add(id);
            if (d.tuto != null) foreach (var id in d.tuto) tuto.Add(id);
            if (d.unlocks != null) foreach (var id in d.unlocks) unlocks.Add(id);
            tutoSkip = d.tutoSkip || d.ver == 0;      // 튜토리얼이 생기기 전 저장 = 다 본 것으로
            shop = d.shop ?? new(); gacha = d.gacha ?? new();
            Infinite = false;      // MVP 빌드: 테스트 패널 없음 → 예전 저장의 '전부 해제'(무제한)도 무시 (랭킹·기록 오염 방지)
        }

        [ContextMenu("진행도 초기화")]
        public void ResetAll() { PlayerPrefs.DeleteKey(KeyOf(Slot)); Load(); Save(); }

        // ── 테스트: 지금 슬롯 전부 해제 ── 기능·튜토리얼 전부 열림 · 마지막 층까지 클리어 · 최고 훈장 · 공용 스킬 노드 전부 · 치즈·연구자료 무제한
        public const double InfiniteAmount = 1e15;
        public bool Infinite { get; private set; }
        [ContextMenu("전부 해제 (테스트)")]
        public void UnlockEverything()
        {
            var db = GameDatabase.Instance;
            tutoSkip = true;
            maxFloor = Mathf.Max(maxFloor, Mathf.Min(NKK.Stage.StageManager.LastFloor, 999) + 1);
            if (db)
            {
                foreach (var t in db.Tiers.Keys) tier = Mathf.Max(tier, t);
                foreach (var s in db.CommonSkills) if (!s.IsRoot) skills.Add(s.skill_id);
            }
            Infinite = true; cheese = research = InfiniteAmount;
            treeCache.Clear(); UnlockVersion++; SkillVersion++;
            Save();
        }

        // 시작 층 상한 = min(1 + 스테이지 스킵 노드 합, 기획서 건너뛰기 상한(최고 클리어 층 = maxFloor - 1), 마지막 층) — 사용자 2026-10-12: 상한은 기획서, 올리는 건 스킬 노드
        public int StartFloorCap(int t = 0)
        {
            int doc = GameDatabase.Instance ? GameDatabase.Instance.SkipCapFor(maxFloor - 1) : maxFloor;
            return Mathf.Max(1, Mathf.Min(Mathf.Min(1 + CommonSkill.StageSkip, Mathf.Min(maxFloor, doc)), NKK.Stage.StageManager.LastFloor));
        }
    }
}
