using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NKK.Net
{
    // 내부 지표: 훈장을 달 때마다 기록 1개 (Firestore rankups) — 서버 계획.md 3장.
    // + 초반 기능이 열릴 때 (기본: 쳇바퀴 훈련 rats) 기록 1개 (Firestore milestones) — 훈장 전 초반 유저 흐름 수집.
    // 기록은 고유 id 를 붙여 큐(PlayerPrefs, 웹 = 브라우저 저장소)에 넣고 바로 보냄. 실패·오프라인이면 남겨 두었다가
    // 씬이 바뀔 때(로비 진입 등)·다음 실행 때 다시 보냄. 같은 id 를 다시 보내도 서버에 한 번만 쌓임.
    // Server 오브젝트(FirebaseClient)에 같이 붙임. 설정 "익명 통계 보내기"를 끄면 (Enabled = false) 기록 안 함.
    [RequireComponent(typeof(FirebaseClient))]
    public class Telemetry : MonoBehaviour
    {
        public static Telemetry I { get; private set; }

        [Tooltip("훈장 기록 컬렉션")] public string collection = "rankups";
        [Tooltip("기능 해금 기록 컬렉션")] public string milestoneCollection = "milestones";
        [Tooltip("열릴 때 기록할 기능 id (rats = 쳇바퀴 훈련) — 늘리면 firestore.rules 의 milestones.feature 목록도 같이")] public string[] logFeatures = { "rats" };
        [Tooltip("못 보낸 기록을 최대 몇 개까지 보관")] public int maxQueue = 50;

        const string PrefQueue = "nkk_tele_queue", PrefOff = "nkk_stats_off";
        // 익명 통계 보내기 (설정에서 끔/켬, 기본 켬)
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PrefOff, 0) == 0;
            set { PlayerPrefs.SetInt(PrefOff, value ? 0 : 1); PlayerPrefs.Save(); }
        }

        [Serializable] class Rec
        {
            public string id, ver, build, lang;
            public string feature;           // 비어 있으면 훈장 기록, 있으면 기능 해금 기록
            public int tier, runsTotal, runsSinceLast, maxFloor, playSec, skillNodes, shardSum, gachaPulls;
        }
        [Serializable] class Box { public List<Rec> list = new(); }

        FirebaseClient client;
        Box queue = new();
        Progress hooked;
        bool sending;

        void Awake()
        {
            client = GetComponent<FirebaseClient>();
            if (FirebaseClient.I && FirebaseClient.I != client) return;       // 중복 Server (곧 지워짐)
            I = this;
            try { queue = JsonUtility.FromJson<Box>(PlayerPrefs.GetString(PrefQueue, "")) ?? new Box(); } catch { queue = new Box(); }
            if (queue.list == null) queue.list = new List<Rec>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnScene;
        void OnDisable() { SceneManager.sceneLoaded -= OnScene; Unhook(); }
        void Unhook() { if (hooked) { hooked.RankedUp -= OnRank; hooked.FeatureUnlocked -= OnFeature; } hooked = null; }
        void Start() => Flush();
        void OnScene(Scene s, LoadSceneMode m) => Flush();

        void Update()
        {
            // 진행도(Progress)는 타이틀에서 생김 → 생기면 훈장 승급 · 기능 해금 이벤트에 붙음
            if (I != this) return;
            var p = Progress.I;
            if (p && p != hooked) { Unhook(); p.RankedUp += OnRank; p.FeatureUnlocked += OnFeature; hooked = p; }
        }

        static string Build => Application.platform switch
        {
            RuntimePlatform.WebGLPlayer => "webgl",
            RuntimePlatform.WindowsPlayer => "win",
            _ => Application.isEditor ? "editor" : Application.platform.ToString(),
        };

        void OnRank(int tier)
        {
            var p = Progress.I;
            if (!p || !Enabled || !client || !client.Ready) return;
            queue.list.Add(new Rec
            {
                id = FirebaseClient.NewDocId(), tier = tier,
                runsTotal = p.runs, runsSinceLast = p.RunsSinceLastRank, maxFloor = p.maxFloor, playSec = (int)p.playSec,
                skillNodes = p.SkillCount, shardSum = p.ShardLevelSum, gachaPulls = p.gacha.total,
                ver = Application.version, build = Build, lang = Loc.Lang,
            });
            Push();
        }

        void OnFeature(string feature)
        {
            var p = Progress.I;
            if (!p || !Enabled || !client || !client.Ready || Array.IndexOf(logFeatures, feature) < 0) return;
            queue.list.Add(new Rec
            {
                id = FirebaseClient.NewDocId(), feature = feature, tier = p.tier,
                runsTotal = p.runs, maxFloor = p.maxFloor, playSec = (int)p.playSec,
                ver = Application.version, build = Build, lang = Loc.Lang,
            });
            Push();
        }

        void Push()
        {
            while (queue.list.Count > maxQueue) queue.list.RemoveAt(0);
            Save();
            Flush();
        }

        void Save() { PlayerPrefs.SetString(PrefQueue, JsonUtility.ToJson(queue)); PlayerPrefs.Save(); }

        public void Flush()
        {
            if (I != this || sending || queue.list.Count == 0 || !client || !client.Ready || !Enabled) return;
            StartCoroutine(Send());
        }

        IEnumerator Send()
        {
            sending = true;
            bool authed = false;
            yield return client.EnsureAuth(ok => authed = ok);
            while (authed && queue.list.Count > 0)
            {
                var r = queue.list[0];
                bool milestone = !string.IsNullOrEmpty(r.feature);
                var f = milestone
                    ? new Dictionary<string, object>
                    {
                        ["uid"] = client.Uid, ["feature"] = r.feature, ["tier"] = r.tier, ["runsTotal"] = r.runsTotal,
                        ["maxFloor"] = r.maxFloor, ["playSec"] = r.playSec, ["ver"] = r.ver, ["build"] = r.build, ["lang"] = r.lang,
                    }
                    : new Dictionary<string, object>
                    {
                        ["uid"] = client.Uid, ["tier"] = r.tier, ["runsTotal"] = r.runsTotal, ["runsSinceLast"] = r.runsSinceLast,
                        ["maxFloor"] = r.maxFloor, ["playSec"] = r.playSec, ["skillNodes"] = r.skillNodes, ["shardSum"] = r.shardSum,
                        ["gachaPulls"] = r.gachaPulls, ["ver"] = r.ver, ["build"] = r.build, ["lang"] = r.lang,
                    };
                bool done = false, ok = false;
                client.Create(milestone ? milestoneCollection : collection, f, "at", b => { ok = b; done = true; }, r.id);
                while (!done) yield return null;
                if (!ok) break;                                   // 다음 기회에 (씬 바뀔 때 · 다음 실행)
                queue.list.RemoveAt(0); Save();
            }
            sending = false;
        }

        // 보관 중인 기록 수 (확인용)
        public int Pending => queue.list.Count;
    }
}
