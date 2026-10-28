using System.Collections.Generic;
using NKK.Data;
using NKK.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 탈출 준비실 · 랭킹 탭: 내 순위 + 상위 100 (Leaderboard). 처음 열 때 닉네임이 없으면 닉네임 창.
    // 줄 템플릿(꺼져 있음) 자식: Rank · Name · Floor({n}) · Tier · Runs({n}) (TMP), Me (내 줄 강조, 꺼져 있음)
    // 상태 글: loading · offline · empty 오브젝트를 켜고 끔. 내 줄: myRow (같은 자식) + noNick (닉네임 없음) · outWord (순위 밖일 때 Rank 칸에 쓸 글)
    public class LeaderboardPage : MonoBehaviour
    {
        public LobbyManager manager;

        [Header("목록")]
        [Tooltip("줄 템플릿 (꺼져 있음)")] public RectTransform rowTemplate;
        public ScrollRect scroll;
        public GameObject loading, offline, empty;

        [Header("내 기록")]
        public RectTransform myRow;
        [Tooltip("닉네임이 없을 때 켜는 안내")] public GameObject noNick;
        [Tooltip("상위 목록 밖일 때 Rank 칸에 쓸 글 (꺼진 TMP)")] public TMP_Text outWord;
        public Button nickButton, refreshButton;

        readonly List<GameObject> spawned = new();
        readonly Dictionary<TMP_Text, string> tpl = new();
        bool askedNick;

        // 닉네임의 < > 가 글자 꾸밈 태그로 읽히지 않게
        public static string Safe(string s) => "<noparse>" + (s ?? "").Replace("</noparse>", "") + "</noparse>";

        void Awake()
        {
            if (rowTemplate) rowTemplate.gameObject.SetActive(false);
            if (nickButton) nickButton.onClick.AddListener(() => { if (NickPopup.I) NickPopup.I.Open(_ => Render()); });
            if (refreshButton) refreshButton.onClick.AddListener(() => { if (Leaderboard.I) Leaderboard.I.Refresh(true); });
        }

        void OnEnable() { if (Leaderboard.I) { Leaderboard.I.Changed += Draw; Leaderboard.I.Watching = true; } }
        void OnDisable() { if (Leaderboard.I) { Leaderboard.I.Changed -= Draw; Leaderboard.I.Watching = false; } }

        // LobbyManager 가 페이지를 열 때 (SendMessage)
        public void Render()
        {
            var lb = Leaderboard.I;
            if (lb) { lb.Changed -= Draw; lb.Changed += Draw; lb.Watching = true; }
            if (!askedNick && !Leaderboard.HasNickname && NickPopup.I) { askedNick = true; NickPopup.I.Open(_ => Render()); }
            if (lb) lb.Refresh(); else Draw();
        }

        void Draw()
        {
            if (!this || !gameObject.activeInHierarchy) return;
            foreach (var g in spawned) { g.SetActive(false); Destroy(g); }
            spawned.Clear();
            var lb = Leaderboard.I;
            bool busy = lb && lb.Loading, fail = !lb || lb.Failed;
            var list = lb ? lb.Top : null;
            if (loading) loading.SetActive(busy && list == null);
            if (offline) offline.SetActive(!busy && fail && list == null);
            if (empty) empty.SetActive(!busy && list != null && list.Count == 0);

            var mine = lb ? lb.Mine : null;
            if (list != null)
                foreach (var e in list)
                {
                    var r = Instantiate(rowTemplate, rowTemplate.parent); r.gameObject.SetActive(true); spawned.Add(r.gameObject);
                    Fill(r, e, e.rank.ToString());
                    var me = r.Find("Me"); if (me) me.gameObject.SetActive(mine != null && e.uid == mine.uid);
                }

            // 내 줄
            bool has = Leaderboard.HasNickname;
            if (noNick) noNick.SetActive(!has);
            if (myRow)
            {
                myRow.gameObject.SetActive(has && mine != null);
                if (has && mine != null) Fill(myRow, mine, mine.rank > 0 ? mine.rank.ToString() : outWord ? outWord.text : "-");
            }
            if (scroll && list != null) scroll.verticalNormalizedPosition = 1;
        }

        void Fill(Transform row, Leaderboard.Entry e, string rank)
        {
            Set(row, "Rank", rank);
            Set(row, "Name", Safe(e.nickname));
            Set(row, "Floor", e.bestFloor.ToString());
            Set(row, "Runs", e.runs.ToString());
            var db = GameDatabase.Instance;
            Set(row, "Tier", db && db.Tiers.TryGetValue(e.bestTier, out var t) ? t.tier_name : e.bestTier.ToString());
        }

        // 자식 글: 템플릿 글에 {n} 이 있으면 채우고, 없으면 값 그대로
        void Set(Transform row, string child, string val)
        {
            var t = row.Find(child)?.GetComponent<TMP_Text>(); if (!t) return;
            var src = rowTemplate ? rowTemplate.Find(child)?.GetComponent<TMP_Text>() : null;
            if (!tpl.TryGetValue(src ? src : t, out var s)) tpl[src ? src : t] = s = src ? src.text : t.text;
            t.text = s.Contains("{n}") ? s.Replace("{n}", val) : val;
        }
    }
}
