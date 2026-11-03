using System;
using System.Collections.Generic;
using NKK.Data;
using UnityEngine;

namespace NKK
{
    // 도토리 · 장식 · 업적 (2026-11-03 신규 재화(도토리) · 업적 · 미니게임 기획서 Ver.05). 저장은 슬롯별 (치즈 · 연구자료와 같이).
    // 도토리 = 수집 전용 재화: 업적 달성(슬롯당 1번) · 쥐돌깨기 10층마다(판마다 반복)로만 들어오고 도토리 상점(장식)으로만 나감.
    // 업적: 조건은 처음부터 쌓이고, 업적 안내(2훈장 · 튜토리얼 l_achv 가 기능 'achv' 해금) 전에는 달성 · 지급을 보류 →
    //       안내 뒤 첫 판정 때 지난 업적을 한꺼번에 정산 (알림 한 장). 분류마다 공개 훈장(reveal_tier) 전에는 '?' · 보류.
    public partial class Progress
    {
        [Serializable] public class DecoEquip { public string cat; public int id; }
        [Serializable] public class AchvAt { public int id; public long at; }

        // ── 도토리 ──
        [Header("도토리 (저장됨, 보기용)")]
        [Tooltip("보유 도토리")] public int acorn;
        [Tooltip("누적 획득 · 출처별 (미니게임 · 업적)")] public int acornTotal, acornMini, acornAchv;
        [Tooltip("쥐돌깨기에서 얻은 쥐 누적 · 판 수")] public int judolRats, judolRuns;
        readonly HashSet<int> decoOwn = new(), ratFriendOn = new(), superUsed = new(), bossKills = new(), achvSeen = new();
        readonly Dictionary<string, int> decoEquip = new();
        readonly Dictionary<int, long> achvAt = new();
        bool achvIntro;

        public const string SrcMini = "mini", SrcAchv = "achv";
        // 도토리가 바뀜 (표시줄 · 상점) · 상한을 넘어 버린 양 (안내용)
        public event Action AcornChanged;
        public event Action<int> AcornOverflow;
        static float Cfg(string k, float d) => GameDatabase.Instance ? GameDatabase.Instance.AcornConfig(k, d) : d;
        public int AcornMax => (int)Cfg("acorn_max", 99999);

        // 지급 (출처 mini · achv). 상한을 넘는 양은 버림. 반환 = 실제로 들어간 양. 바로 저장
        public int AddAcorn(string src, int n, bool save = true)
        {
            if (n <= 0) return 0;
            int got = Mathf.Min(n, Mathf.Max(0, AcornMax - acorn));
            acorn += got; acornTotal += got;
            if (src == SrcMini) acornMini += got; else if (src == SrcAchv) acornAchv += got;
            if (save) Save();
            AcornChanged?.Invoke();
            if (got < n) { Debug.Log($"[Acorn] 상한 {AcornMax} → {n - got} 버림"); AcornOverflow?.Invoke(n - got); }
            return got;
        }
        public bool SpendAcorn(int n)
        {
            if (n < 0 || acorn < n) return false;
            acorn -= n; Save(); AcornChanged?.Invoke();
            return true;
        }

        // ── 장식 (도토리 상점) ── 그림 적용은 아직 없음 (리소스를 만들 때 구현). 지금은 사기 · 보유 · 적용 기록만
        public enum DecoState { Owned, Buyable, Poor, Locked }
        public bool OwnsDeco(int id) => decoOwn.Contains(id);
        // 판매 조건: 그 장식이 바꾸는 시설(기능 id)이 아직 안 열렸으면 잠김
        public bool DecoLocked(DecoItemRow d) => d != null && !string.IsNullOrEmpty(d.sell_feature) && !IsUnlocked(d.sell_feature);
        public DecoState DecoStateOf(DecoItemRow d)
        {
            if (d == null) return DecoState.Locked;
            if (decoOwn.Contains(d.deco_id)) return DecoState.Owned;
            if (DecoLocked(d)) return DecoState.Locked;
            return acorn >= d.price ? DecoState.Buyable : DecoState.Poor;
        }
        public event Action<DecoItemRow> DecoBought;
        public bool BuyDeco(DecoItemRow d)
        {
            if (DecoStateOf(d) != DecoState.Buyable) return false;
            acorn -= d.price; decoOwn.Add(d.deco_id);
            if (d.category == "RatFriend" && ratFriendOn.Count < (int)Cfg("friend_max", 6)) ratFriendOn.Add(d.deco_id);
            Save(); AcornChanged?.Invoke(); DecoBought?.Invoke(d);
            Debug.Log($"[AcornShop] 구매 {d.deco_id} {d.deco_name} −{d.price} (남은 도토리 {acorn})");
            return true;
        }
        public int EquippedDeco(string cat) => decoEquip.TryGetValue(cat, out var id) ? id : 0;      // 0 = 기본
        public int DecoOwnedCount => decoOwn.Count;

        // ── 업적 ──
        // 업적 달성 알림 (슬롯당 한 번) · 묶음 알림 (지난 업적 소급 · 한꺼번에 많이: 개수, 도토리 합)
        public event Action<AchievementRow> AchvGot;
        public event Action<int, int> AchvBatch;
        public int AchvIntroTier => (int)Cfg("intro_tier", 2);
        // 업적 안내를 봄 (2훈장 + 기능 achv. 튜토리얼 건너뛴 저장은 2훈장만으로)
        public bool AchvIntroDone => tier >= AchvIntroTier && IsUnlocked("achv");
        public int AchvReward(AchievementRow a) => a == null ? 0 : a.reward_acorn;
        // 분류가 공개됐는지 (업적 탭 '?' · 달성 보류)
        public bool AchvRevealed(AchievementRow a) => a != null && AchvIntroDone && tier >= Mathf.Max(1, a.reveal_tier);
        public bool CategoryRevealed(string cat)
        {
            var db = GameDatabase.Instance;
            int rv = db && db.AchvCategories.TryGetValue(cat, out var c) ? c.reveal : AchvIntroTier;
            return AchvIntroDone && tier >= rv;
        }
        public long AchvTime(int id) => achvAt.TryGetValue(id, out var t) ? t : 0;
        public bool AchvSeen(int id) => achvSeen.Contains(id);
        public void MarkAchvSeen(IEnumerable<int> ids) { bool any = false; foreach (var i in ids) any |= achvSeen.Add(i); if (any) Save(); }

        // 업적 한 줄의 진행값 (조건 타입별): 1회형 = 0/1 · 최댓값형 = 훈장 · 층 · 누적형 = 쥐 · 필살기 횟수
        public int AchvProgress(AchievementRow a)
        {
            if (a == null) return 0;
            switch (a.cond_type)
            {
                case "Ult_Use": return UltUses(a.target_id);
                case "Tier_Reach": return tier;
                case "Super_Use": return superUsed.Contains(a.target_id) ? 1 : 0;
                case "Boss_Kill": return bossKills.Contains(a.target_id) || ClearedFloor >= a.target_id ? 1 : 0;      // 예전 저장 = 그 층까지 클리어했으면 처치
                case "Mini_Floor": return judolBest;
                case "Mini_Rat": return judolRats;
            }
            return 0;
        }
        public int AchvNeed(AchievementRow a) => a == null ? 1 : Mathf.Max(1, a.need);
        bool AchvMet(AchievementRow a) => AchvProgress(a) >= AchvNeed(a);

        // 각 시스템이 부름: 초필살기 첫 사용 (테스트 발동 제외) · 보스 처치 (층)
        public void ReportSuperUse(int superId) { if (superUsed.Add(superId)) { Save(); CheckAchvAll("Super_Use"); } }
        public void ReportBossKill(int floor) { if (bossKills.Add(floor)) { Save(); CheckAchvAll("Boss_Kill"); } }
        // 쥐돌깨기: 얻은 쥐는 모아 두었다가 턴 끝 · 판 끝에 한 번에 저장 (뽑기 100번마다 저장하지 않게)
        int judolPending;
        public void AddJudolRats(int n) { if (n > 0) { judolRats += n; judolPending += n; } }
        public void FlushJudol() { if (judolPending <= 0) return; judolPending = 0; Save(); CheckAchvAll("Mini_Rat"); }
        public void OnJudolRun() { judolRuns++; Save(); }

        // 판정: type 이 있으면 그 조건 타입만. 안내 전 · 공개 전 분류는 보류(조건은 계속 쌓임) → 공개되는 순간 몰아서 달성
        public void CheckAchvAll(string type = null)
        {
            var db = GameDatabase.Instance; if (!db || !AchvIntroDone) return;
            bool batch = !achvIntro;          // 안내 뒤 첫 정산 = 지난 업적 소급 (한 장으로)
            var got = new List<AchievementRow>();
            foreach (var a in db.Achievements)
            {
                if (achvOn.Contains(a.achv_id) || (!batch && type != null && a.cond_type != type)) continue;
                if (tier < Mathf.Max(1, a.reveal_tier) || !AchvMet(a)) continue;
                got.Add(a);
            }
            if (!batch && got.Count == 0) return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); int sum = 0;
            foreach (var a in got) { achvOn.Add(a.achv_id); achvAt[a.achv_id] = now; sum += AchvReward(a); }
            achvIntro = true;
            if (sum > 0) AddAcorn(SrcAchv, sum, false);
            Save();
            foreach (var a in got) Debug.Log($"[Achv] 달성 {a.achv_id} {a.achv_name} +{AchvReward(a)}{(batch ? " (소급)" : "")}");
            if (batch) { if (got.Count > 0) AchvBatch?.Invoke(got.Count, sum); }
            else foreach (var a in got) AchvGot?.Invoke(a);
        }
        // 테스트 '전부 해제': 지금 조건이 맞는 업적을 도토리 · 알림 없이 켬 (안내도 본 것으로)
        void GrantAchvSilently()
        {
            var db = GameDatabase.Instance; if (!db) return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var a in db.Achievements) if (!achvOn.Contains(a.achv_id) && AchvMet(a)) { achvOn.Add(a.achv_id); achvAt[a.achv_id] = now; }
            achvIntro = true;
        }

        // ── 저장 ──
        void ClearAcorn()
        {
            acorn = acornTotal = acornMini = acornAchv = judolRats = judolRuns = judolPending = 0; achvIntro = false;
            decoOwn.Clear(); ratFriendOn.Clear(); superUsed.Clear(); bossKills.Clear(); achvSeen.Clear(); decoEquip.Clear(); achvAt.Clear();
        }
        void SaveAcorn(SaveData d)
        {
            d.acorn = acorn; d.acornTotal = acornTotal; d.acornMini = acornMini; d.acornAchv = acornAchv; d.judolRats = judolRats; d.judolRuns = judolRuns; d.achvIntro = achvIntro;
            d.decoOwn.AddRange(decoOwn); d.ratFriendOn.AddRange(ratFriendOn); d.superUsed.AddRange(superUsed); d.bossKills.AddRange(bossKills); d.achvSeen.AddRange(achvSeen);
            foreach (var kv in decoEquip) d.decoEquip.Add(new DecoEquip { cat = kv.Key, id = kv.Value });
            foreach (var kv in achvAt) d.achvAt.Add(new AchvAt { id = kv.Key, at = kv.Value });
        }
        void LoadAcorn(SaveData d)
        {
            acorn = d.acorn; acornTotal = d.acornTotal; acornMini = d.acornMini; acornAchv = d.acornAchv; judolRats = d.judolRats; judolRuns = d.judolRuns; achvIntro = d.achvIntro;
            if (d.decoOwn != null) foreach (var i in d.decoOwn) decoOwn.Add(i);
            if (d.ratFriendOn != null) foreach (var i in d.ratFriendOn) ratFriendOn.Add(i);
            if (d.superUsed != null) foreach (var i in d.superUsed) superUsed.Add(i);
            if (d.bossKills != null) foreach (var i in d.bossKills) bossKills.Add(i);
            if (d.achvSeen != null) foreach (var i in d.achvSeen) achvSeen.Add(i);
            if (d.decoEquip != null) foreach (var e in d.decoEquip) if (!string.IsNullOrEmpty(e.cat)) decoEquip[e.cat] = e.id;
            if (d.achvAt != null) foreach (var e in d.achvAt) achvAt[e.id] = e.at;
            // 예전 저장의 필살기 업적(achvOn)은 그대로 · 기존 achvIntro 가 없으니 안내 뒤 첫 판정에서 남은 것만 소급
        }
    }
}
