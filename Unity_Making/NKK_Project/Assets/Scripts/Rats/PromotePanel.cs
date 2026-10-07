using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Rats
{
    // 게임 화면 승급 (웹 승급 의식 패널): 왼쪽 아래 승급 버튼 → 등급 줄 5개 (일반→레어 … 전설→신화) + 일괄 승급.
    // 줄 글 자리표시: {from} {to} 등급 이름 · {have} 지금 마리 수 · {need} 필요 수. 글은 씬 TMP 에 적혀 있음.
    // 판은 멈추지 않음 (웹과 같음). 승급할 수 있는 게 있으면 버튼에 알림 점.
    public class PromotePanel : MonoBehaviour
    {
        public RatManager Rats;
        [Tooltip("여닫는 버튼 (HUD 왼쪽 아래)")] public Button openButton;
        [Tooltip("승급할 수 있을 때 켜지는 알림 점")] public GameObject badge;
        public GameObject panel;
        public Button closeButton, allButton;
        [System.Serializable] public class Row { public TMP_Text text; public Button button; public Image from, to; }
        [Tooltip("등급 줄 (일반 → 레어 … 전설 → 신화 순서)")] public Row[] rows = new Row[5];
        [Tooltip("일괄 승급 결과 배너 ({n} 횟수) · 못 할 때 배너 ({keep} 남길 마리 수)")] public string allDone, allFail;
        public Color okColor = new(0.29f, 0.22f, 0.17f), dimColor = new(0.62f, 0.5f, 0.42f);

        string[] fmt;

        void Awake()
        {
            if (panel) panel.SetActive(false);
            if (openButton) openButton.onClick.AddListener(() => { if (panel) { panel.SetActive(!panel.activeSelf); Refresh(); } });
            if (closeButton) closeButton.onClick.AddListener(() => panel.SetActive(false));
            if (allButton) allButton.onClick.AddListener(PromoteAll);
            fmt = new string[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                if (rows[i].text) fmt[i] = rows[i].text.text;
                int g = i;
                if (rows[i].button) rows[i].button.onClick.AddListener(() => { if (Rats.Promote(g)) Refresh(); });
            }
        }

        static string GradeName(int g) => GameDatabase.Instance.Grades.TryGetValue((Grade)g, out var r) ? r.grade_name : "";
        static Color GradeColor(int g) => GameDatabase.Instance.Grades.TryGetValue((Grade)g, out var r) && ColorUtility.TryParseHtmlString(r.color, out var c) ? c : Color.white;

        float t;
        void Update()
        {
            if ((t -= Time.unscaledDeltaTime) > 0) return;
            t = 0.25f;
            if (badge) badge.SetActive(Rats.CanPromoteAny);
            if (panel && panel.activeSelf) Refresh();
        }

        void Refresh()
        {
            if (!Rats || GameDatabase.Instance == null) return;
            for (int g = 0; g < rows.Length; g++)
            {
                var r = rows[g]; if (r == null) continue;
                bool ok = Rats.CanPromote(g);
                if (r.text)
                {
                    r.text.text = (fmt[g] ?? "").Replace("{from}", GradeName(g)).Replace("{to}", GradeName(g + 1))
                        .Replace("{have}", Rats.CountGrade(g).ToString()).Replace("{need}", Rats.PromoteNeed(g).ToString());
                    r.text.color = ok ? okColor : dimColor;
                }
                if (r.button) r.button.interactable = ok;
                if (r.from) r.from.color = GradeColor(g);
                if (r.to) r.to.color = GradeColor(g + 1);
            }
            if (allButton) allButton.interactable = Rats.CanPromoteAny;
        }

        void PromoteAll()
        {
            int n = Rats.PromoteAll();
            var game = Rats.Game;
            if (game) game.ShowBanner(((n > 0 ? allDone : allFail) ?? "").Replace("{n}", n.ToString()).Replace("{keep}", Rats.promoteKeep.ToString()));
            Refresh();
        }
    }
}
