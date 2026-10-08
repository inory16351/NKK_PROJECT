using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 탈출 준비실 · 작전 회의 (웹 renderRun): 시작 층 고르기 → 출발
    // 왼쪽 카드: 층 길(타일) · 구역 · 방 수 · 보스 층 / 오른쪽 카드: 출동 멤버 · 등급 확률 막대 · 기록 · 출발 버튼
    public class RunPage : MonoBehaviour
    {
        public LobbyManager manager;

        [Header("층 길")]
        public Transform tileRow;
        [Tooltip("층 타일 템플릿 (꺼져 있음): 자식 Num · Sub (TMP)")] public Button tileTemplate;
        public Sprite tileNormal, tileOn, tileLock;
        [Tooltip("최고 기록 너머로 잠긴 층을 몇 개 더 보여 줄지")] public int lockedPreview = 3;

        [Header("층 정보")]
        public TMP_Text zoneText;
        public TMP_Text roomsText;
        public TMP_Text bossText;
        [Tooltip("방 수 = min(최대, 기본 + 층 × 증가) (+ 보스 층 1) — StageManager 와 같은 값")] public int roomBase = 3;
        public float roomPerFloor = 0.6f;
        public int roomMax = 9;
        public int bossEvery = 5;

        [Header("출동 멤버")]
        public TMP_Text crewText;
        [Tooltip("확률 줄 템플릿 (꺼져 있음): 자식 Name(TMP) · Fill(Image Filled) · Pct(TMP)")] public RectTransform oddsTemplate;
        public TMP_Text bestText, runsText;
        public Button goButton;
        public TMP_Text goLabel;

        int startFloor = 1;
        readonly List<GameObject> spawned = new();
        readonly Dictionary<TMP_Text, string> tpl = new();
        // 씬에 적힌 글(자리표시 포함)을 처음 한 번 기억해 두고 값만 바꿈
        void SetT(TMP_Text t, params (string key, object val)[] kv)
        {
            if (!t) return;
            if (!tpl.TryGetValue(t, out var s)) tpl[t] = s = t.text;
            foreach (var (k, v) in kv) s = s.Replace("{" + k + "}", v.ToString());
            t.text = s;
        }

        void Awake()
        {
            if (tileTemplate) tileTemplate.gameObject.SetActive(false);
            if (oddsTemplate) oddsTemplate.gameObject.SetActive(false);
            if (goButton) goButton.onClick.AddListener(() => manager.Go(startFloor));
        }

        static string Word(Transform t, string child, string fallback) { var w = t.Find(child)?.GetComponent<TMP_Text>(); return w ? w.text : fallback; }

        int Rooms(int f) => Mathf.Min(roomMax, roomBase + Mathf.FloorToInt(f * roomPerFloor)) + (f % bossEvery == 0 ? 1 : 0);

        // LobbyManager 가 페이지를 열 때 (SendMessage)
        public void Render()
        {
            foreach (var g in spawned) Destroy(g);
            spawned.Clear();
            var p = Progress.I; var db = GameDatabase.Instance;
            int cap = p ? p.StartFloorCap() : 1, best = p ? p.maxFloor : 1, tier = p ? p.tier : 1;
            startFloor = Mathf.Clamp(startFloor, 1, cap);

            // 층 길: 1 ~ max(cap, min(cap + 3, 최고 + 2))
            int n = Mathf.Max(cap, Mathf.Min(cap + lockedPreview, best + 2));
            for (int f = 1; f <= n; f++)
            {
                var b = Instantiate(tileTemplate, tileRow); b.gameObject.SetActive(true); spawned.Add(b.gameObject);
                bool can = f <= cap, on = f == startFloor, boss = f % bossEvery == 0;
                var img = b.GetComponent<Image>(); img.sprite = !can ? tileLock : on ? tileOn : tileNormal;
                b.interactable = can;
                var num = b.transform.Find("Num")?.GetComponent<TMP_Text>(); if (num) num.text = f.ToString();
                var sub = b.transform.Find("Sub")?.GetComponent<TMP_Text>();
                if (sub)        // 타일 아래 글: 구역 이름 끝말 (보스 층·잠긴 층은 템플릿의 BossWord·LockWord 자식 글)
                {
                    var z = db.ZoneOf(f); var parts = z != null ? z.zone_name.Split(' ') : new[] { "" };
                    sub.text = boss ? Word(tileTemplate.transform, "BossWord", "보스") : can ? parts[^1] : Word(tileTemplate.transform, "LockWord", "잠김");
                }
                b.transform.localScale = Vector3.one * (on ? 1.1f : 1);
                int ff = f; b.onClick.AddListener(() => { startFloor = ff; Render(); });
            }

            var zone = db.ZoneOf(startFloor);
            if (zoneText) zoneText.text = zone != null ? zone.zone_name : "";
            SetT(roomsText, ("n", Rooms(startFloor)));
            if (bossText) bossText.gameObject.SetActive(startFloor % bossEvery == 0);

            // 출동 멤버: 티어 시작 마릿수 + 등급 확률
            int crew = (db.Tiers.TryGetValue(tier, out var tr) ? tr.start_rat_count : 6) + CommonSkill.StartRatAdd;
            SetT(crewText, ("n", crew));
            var w = RatManager.GradeWeights(tier); float sum = 0; foreach (var x in w) sum += x;
            for (int g = 0; g < 6; g++)
            {
                if (!db.Grades.TryGetValue((Grade)g, out var gr)) continue;
                var row = Instantiate(oddsTemplate, oddsTemplate.parent); row.gameObject.SetActive(true); spawned.Add(row.gameObject);
                bool open = w[g] > 0; float pr = sum > 0 ? w[g] / sum : 0;
                var name = row.Find("Name")?.GetComponent<TMP_Text>(); if (name) name.text = gr.grade_name;
                var fill = row.Find("Fill")?.GetComponent<Image>();
                if (fill) { fill.fillAmount = open ? Mathf.Max(0.015f, pr) : 0; if (ColorUtility.TryParseHtmlString(gr.color, out var c)) fill.color = c; }
                var pct = row.Find("Pct")?.GetComponent<TMP_Text>(); if (pct) pct.text = !open ? "잠김" : pr >= 0.1f ? $"{pr * 100:0}%" : pr >= 0.01f ? $"{pr * 100:0.0}%" : $"{pr * 100:0.00}%";
                row.SetSiblingIndex(oddsTemplate.GetSiblingIndex() + 1 + g);
            }
            SetT(bestText, ("n", best));
            SetT(runsText, ("n", p ? p.runs : 0));
            SetT(goLabel, ("floor", startFloor));
        }
    }
}
