using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 로비 씬: 아지트(LobbyHome) + 탈출 준비실 (웹 lobby.js 상단 탭 6개).
    //   run 작전 회의 · rank 찍찍!! 훈장 · rats 쳇바퀴 훈련 · skill 치즈 창고 · dex 친구들!! · rec 낮잠 침대
    // 출발 → Progress.PendingStartFloor 에 층을 넣고 Game 씬으로.
    public class LobbyManager : MonoBehaviour
    {
        [System.Serializable]
        public class Page
        {
            public string id;
            [Tooltip("탭 이름")] public string label;
            public Button tab;
            public GameObject root;
        }

        [Header("연결")]
        public LobbyHome home;
        [Tooltip("게임 씬 이름")] public string gameScene = "Game";

        [Header("아지트 화면 UI")]
        public GameObject homeUI;
        public TMP_Text homeCheese, homeResearch;

        [Header("탈출 준비실")]
        public GameObject prep;
        public Button homeButton;
        public TMP_Text prepCheese, prepResearch;
        public List<Page> pages = new();
        [Tooltip("탭: 선택 안 됐을 때 투명도")] public float tabOffAlpha = 0.75f;
        [Tooltip("탭: 선택됐을 때 크기")] public float tabOnScale = 1.12f;

        public string CurrentPage { get; private set; }
        public bool PrepOpen => prep && prep.activeSelf;

        void Awake()
        {
            if (home) { home.manager = this; home.inputBlocked = () => PrepOpen; }
            if (homeButton) homeButton.onClick.AddListener(CloseToHome);
            foreach (var p in pages) { var id = p.id; if (p.tab) p.tab.onClick.AddListener(() => OpenPage(id)); }
            if (prep) prep.SetActive(false);
        }

        // 글 자리표시 채우기: {tier} 훈장 번호 · {tierName} 훈장 이름 · {cheese} · {research} · {floor} 최고 층
        // 글 자체는 씬의 TMP 텍스트에 적혀 있음 (하이라키에서 고침)
        public string FillTokens(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('{') < 0) return s;
            var p = Progress.I; int tier = p ? p.tier : 1;
            var db = GameDatabase.Instance;
            string tierName = db && db.Tiers.TryGetValue(tier, out var tr) ? tr.tier_name : "";
            return s.Replace("{tier}", tier.ToString()).Replace("{tierName}", tierName)
                    .Replace("{cheese}", Fmt(p ? p.cheese : 0)).Replace("{research}", Fmt(p ? p.research : 0)).Replace("{floor}", (p ? p.maxFloor : 1).ToString());
        }

        // 팻말 알림 점 (강화할 수 있는 게 있을 때) — 각 페이지가 생기면 채움
        public bool Alert(string id)
        {
            var p = Progress.I; if (!p || !GameDatabase.Instance) return false;
            if (id == "skill") foreach (var s in GameDatabase.Instance.CommonSkills) if (p.CanBuySkill(s)) return true;
            if (id == "rats") return p.UpgradableCount > 0;
            return false;
        }

        public void OnHot(string id) => OpenPage(id);

        public void OpenPage(string id)
        {
            CurrentPage = id;
            if (prep) prep.SetActive(true);
            if (homeUI) homeUI.SetActive(false);
            foreach (var p in pages)
            {
                bool on = p.id == id;
                if (p.root) { p.root.SetActive(on); if (on) p.root.SendMessage("Render", SendMessageOptions.DontRequireReceiver); }
                if (p.tab)
                {
                    p.tab.transform.localScale = Vector3.one * (on ? tabOnScale : 1);
                    var cg = p.tab.GetComponent<CanvasGroup>(); if (cg) cg.alpha = on ? 1 : tabOffAlpha;
                }
            }
        }

        public void CloseToHome()
        {
            if (prep) prep.SetActive(false);
            if (homeUI) homeUI.SetActive(true);
            if (home) home.Refresh();
        }

        // 출발: 시작 층을 넘기고 게임 씬으로
        public void Go(int floor)
        {
            var p = Progress.I;
            if (p) { p.runs++; p.Save(); }
            Progress.PendingStartFloor = Mathf.Clamp(floor, 1, p ? p.StartFloorCap() : 1);
            SceneManager.LoadScene(gameScene);
        }

        public static string Fmt(double v) => GameManager.Format(v);

        void Update()
        {
            var p = Progress.I;
            string c = Fmt(p ? p.cheese : 0), r = Fmt(p ? p.research : 0);
            if (homeCheese) homeCheese.text = c; if (homeResearch) homeResearch.text = r;
            if (prepCheese) prepCheese.text = c; if (prepResearch) prepResearch.text = r;
            // 탈출 준비실에서 ESC = 아지트로
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame && PrepOpen) CloseToHome();
        }
    }
}
