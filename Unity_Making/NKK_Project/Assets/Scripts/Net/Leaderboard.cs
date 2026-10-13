using System;
using System.Collections.Generic;
using UnityEngine;

namespace NKK.Net
{
    // 랭킹 (Firestore players/{uid}) — 서버 계획.md 3장.
    // 순위 = 최고 층 내림차순 → 그 층에 처음 닿았을 때 시도 횟수 오름차순, 상위 top 개 (색인: bestFloor ↓ · runs ↑).
    // 닉네임을 정해야 올라감. 최고 층·훈장이 오르면 (또는 닉네임을 바꾸면) 내 문서를 덮어씀. 실패하면 dirty 로 남겨 씬이 바뀔 때 다시.
    // 저장 슬롯이 여러 개여도 uid 는 하나 → 이미 보낸 기록보다 낮으면 보낸 기록을 유지 (규칙: 최고 층·훈장은 줄어들 수 없음).
    [RequireComponent(typeof(FirebaseClient))]
    public class Leaderboard : MonoBehaviour
    {
        public static Leaderboard I { get; private set; }

        [Tooltip("랭킹 컬렉션")] public string collection = "players";
        [Tooltip("상위 몇 명")] public int top = 100;
        [Tooltip("조회 결과를 몇 초 동안 다시 쓸지 (읽기 비용)")] public float cacheSec = 300;
        [Tooltip("닉네임 글자 수")] public int nickMin = 2, nickMax = 12;
        [Tooltip("닉네임 금지어 목록 (한 줄에 하나, 대소문자·띄어쓰기 무시하고 포함되면 거절)")] public TextAsset bannedWords;

        const string PrefNick = "nkk_nick", PrefSent = "nkk_lb_sent", PrefDirty = "nkk_lb_dirty";

        [Serializable] public class Entry { public string uid, nickname; public int bestFloor, bestTier, runs, rank; }
        [Serializable] class Sent { public int floor, tier, runs; public string nick; }

        public List<Entry> Top { get; private set; }
        public bool Loading { get; private set; }
        public bool Failed { get; private set; }          // 마지막 조회 실패 (오프라인 등)
        public event Action Changed;                       // 조회가 끝남 · 내 기록을 보냄
        [NonSerialized] public bool Watching;              // 랭킹 페이지가 열려 있음 → 내 기록을 보내면 바로 다시 조회

        FirebaseClient client;
        Progress hooked;
        float fetchedAt = -9999;
        bool pushing;
        string[] banned;

        public static string Nickname => PlayerPrefs.GetString(PrefNick, "");
        public static bool HasNickname => !string.IsNullOrEmpty(Nickname);

        void Awake()
        {
            client = GetComponent<FirebaseClient>();
            if (FirebaseClient.I && FirebaseClient.I != client) return;       // 중복 Server (곧 지워짐)
            I = this;
            var l = new List<string>();
            if (bannedWords) foreach (var w in bannedWords.text.Split('\n')) { var t = Norm(w); if (t.Length > 0 && t[0] != '#') l.Add(t); }
            banned = l.ToArray();
        }

        void OnEnable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnScene;
        void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnScene;
            if (hooked) { hooked.FloorRecord -= OnFloor; hooked.RankedUp -= OnFloor; } hooked = null;
        }
        void OnScene(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => Push();

        void Update()
        {
            if (I != this) return;
            var p = Progress.I;
            if (p && p != hooked)
            {
                if (hooked) { hooked.FloorRecord -= OnFloor; hooked.RankedUp -= OnFloor; }
                p.FloorRecord += OnFloor; p.RankedUp += OnFloor; hooked = p;
                Push();                                     // 슬롯을 불러옴 → 이 슬롯 기록이 더 높으면 보냄
            }
        }
        void OnFloor(int _) => Push();

        // ── 닉네임 ──
        public enum NickError { None, Length, Banned }
        static string Norm(string s) => (s ?? "").Trim().Replace(" ", "").ToLowerInvariant();
        public NickError Check(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length < nickMin || s.Length > nickMax) return NickError.Length;
            var n = Norm(s);
            if (banned != null) foreach (var w in banned) if (n.Contains(w)) return NickError.Banned;
            return NickError.None;
        }
        public bool SetNickname(string s)
        {
            if (Check(s) != NickError.None) return false;
            PlayerPrefs.SetString(PrefNick, s.Trim()); PlayerPrefs.SetInt(PrefDirty, 1); PlayerPrefs.Save();
            Push();
            return true;
        }

        // ── 내 기록 보내기 ──
        Sent LoadSent() { try { return JsonUtility.FromJson<Sent>(PlayerPrefs.GetString(PrefSent, "")) ?? new Sent(); } catch { return new Sent(); } }

        // 보낼 기록: 보낸 것과 이 슬롯 중 높은 쪽 (같은 층이면 시도 횟수 적은 쪽)
        Sent Best()
        {
            var s = LoadSent(); var p = Progress.I;
            var b = new Sent { floor = s.floor, tier = s.tier, runs = s.runs, nick = Nickname };
            if (p)
            {
                if (p.maxFloor > b.floor || (p.maxFloor == b.floor && p.floorRuns < b.runs)) { b.floor = p.maxFloor; b.runs = p.floorRuns; }
                b.tier = Mathf.Max(b.tier, p.tier);
            }
            b.floor = Mathf.Max(1, b.floor); b.tier = Mathf.Max(1, b.tier);
            return b;
        }

        public void Push()
        {
            if (I != this || pushing || !HasNickname || !client || !client.Ready) return;
            var b = Best(); var s = LoadSent();
            bool same = b.floor == s.floor && b.tier == s.tier && b.runs == s.runs && b.nick == s.nick;
            if (same && PlayerPrefs.GetInt(PrefDirty, 0) == 0) return;
            pushing = true;
            var f = new Dictionary<string, object> { ["nickname"] = b.nick, ["bestFloor"] = b.floor, ["bestTier"] = b.tier, ["runs"] = b.runs };
            client.Set($"{collection}/{client.Uid}", f, "updatedAt", ok =>
            {
                pushing = false;
                if (!ok) { PlayerPrefs.SetInt(PrefDirty, 1); PlayerPrefs.Save(); return; }
                PlayerPrefs.SetString(PrefSent, JsonUtility.ToJson(b)); PlayerPrefs.SetInt(PrefDirty, 0); PlayerPrefs.Save();
                fetchedAt = -9999;                                  // 내 순위가 바뀌었을 수 있음 → 다음에 열면 새로 조회
                if (Watching) Refresh(); else Changed?.Invoke();
            });
        }

        // ── 상위 목록 ──
        // 내 기록 (보낸 것 기준, 아직 안 보냈으면 지금 슬롯)
        public Entry Mine
        {
            get { var b = Best(); return new Entry { uid = client ? client.Uid : "", nickname = b.nick, bestFloor = b.floor, bestTier = b.tier, runs = b.runs, rank = MyRank }; }
        }
        // 상위 목록 안 내 순위 (없으면 0)
        public int MyRank
        {
            get
            {
                if (Top == null || !client || string.IsNullOrEmpty(client.Uid)) return 0;
                foreach (var e in Top) if (e.uid == client.Uid) return e.rank;
                return 0;
            }
        }

        public void Refresh(bool force = false)
        {
            if (I != this) return;
            if (Loading) return;
            if (!client || !client.Ready) { Failed = true; Changed?.Invoke(); return; }
            if (!force && Top != null && Time.realtimeSinceStartup - fetchedAt < cacheSec) { Changed?.Invoke(); return; }
            Loading = true; Changed?.Invoke();
            string q = "{\"from\":[{\"collectionId\":" + FirebaseClient.Json(collection) + "}]," +
                       "\"orderBy\":[{\"field\":{\"fieldPath\":\"bestFloor\"},\"direction\":\"DESCENDING\"},{\"field\":{\"fieldPath\":\"runs\"},\"direction\":\"ASCENDING\"}]," +
                       "\"limit\":" + top + "}";
            client.Query(q, text =>
            {
                Loading = false;
                if (text == null) { Failed = true; Changed?.Invoke(); return; }
                Failed = false; Top = Parse(text); fetchedAt = Time.realtimeSinceStartup;
                Changed?.Invoke();
            });
        }

        // runQuery 응답: [{"document":{"name":".../players/uid","fields":{"nickname":{"stringValue":…},"bestFloor":{"integerValue":"5"},…}}}, …]
        [Serializable] class SV { public string stringValue; }
        [Serializable] class IV { public string integerValue; }
        [Serializable] class F { public SV nickname; public IV bestFloor, bestTier, runs; }
        [Serializable] class D { public string name; public F fields; }
        [Serializable] class R { public D document; }
        [Serializable] class W { public List<R> r; }
        static int Int(IV v) => v != null && int.TryParse(v.integerValue, out int n) ? n : 0;

        static List<Entry> Parse(string text)
        {
            var list = new List<Entry>();
            W w; try { w = JsonUtility.FromJson<W>("{\"r\":" + text + "}"); } catch { return list; }
            if (w?.r == null) return list;
            foreach (var r in w.r)
            {
                var d = r?.document; if (d == null || string.IsNullOrEmpty(d.name) || d.fields == null) continue;
                list.Add(new Entry
                {
                    uid = d.name.Substring(d.name.LastIndexOf('/') + 1), nickname = d.fields.nickname?.stringValue ?? "",
                    bestFloor = Int(d.fields.bestFloor), bestTier = Int(d.fields.bestTier), runs = Int(d.fields.runs), rank = list.Count + 1,
                });
            }
            return list;
        }
    }
}
