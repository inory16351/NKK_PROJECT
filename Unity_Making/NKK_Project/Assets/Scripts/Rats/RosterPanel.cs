using System.Collections.Generic;
using NKK.Data;
using NKK.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NKK.Rats
{
    // 쥐 목록 창 (판 안): 지금 있는 쥐를 종별로 묶어 카드로 보여 줌 — 그림 · 이름 · 등급 · 마릿수 · 이번 층 피해 · 피해 순위.
    // HUD 버튼 또는 단축키(기본 R)로 열고 닫음. 열려 있는 동안 게임 멈춤 (FxManager.Halt). 임시 소환 쥐는 뺌.
    // 카드 템플릿 자식: Frame(Image, 등급 색) · Portrait(RatPortrait) · Name · Grade({grade}) · Count({n}) · Damage({n}) · Rank({rank}, 피해 없으면 숨김)
    // 글은 씬 TMP, 코드는 자리표시만 채움.
    public class RosterPanel : MonoBehaviour
    {
        public static bool Pausing { get; private set; }

        public RatManager Rats;
        [Tooltip("여는 버튼 (HUD)")] public Button openButton;
        public GameObject panel;
        public Button closeButton;
        [Tooltip("여닫는 단축키")] public Key hotkey = Key.R;
        [Tooltip("튜토리얼 기능 id — 잠겨 있으면 단축키로도 안 열림 (버튼은 FeatureGate 가 숨김)")] public string feature = "roster";
        [Tooltip("카드 템플릿 (꺼 둠)")] public RectTransform cardTemplate;
        [Tooltip("카드를 넣을 곳 (Grid Layout · ScrollRect 의 Content)")] public RectTransform content;
        [Tooltip("위 요약 ({n} 마릿수 · {max} 최대 · {kinds} 종 수 · {dmg} 이번 층 피해 합)")] public TMP_Text summaryText;
        [Tooltip("정렬 버튼 (등급순 · 마릿수순 · 피해순) — 고른 것은 selected 색")] public Button sortGrade, sortCount, sortDamage;
        public Color sortOn = new(1f, 0.85f, 0.45f), sortOff = Color.white;
        [Tooltip("이번 층 피해가 없는 종의 피해 글 대신 (빈칸 = 0 표시)")] public string noDamage = "";
        [Tooltip("열려 있는 동안 이것들보다 위에 그림 (같은 부모 안 형제)")] public Transform[] drawAbove;
        [Tooltip("휠 한 칸에 목록이 움직이는 거리 (픽셀) — 마우스가 창 밖에 있어도 목록만 움직이고 카메라 줌은 막힘")] public float wheelPixels = 120;

        enum Sort { Grade, Count, Damage }
        Sort sort = Sort.Grade;

        class Group { public string code; public RatCharacterRow row; public int n; public double dmg; }
        readonly List<Group> groups = new();
        readonly List<RectTransform> cards = new();
        readonly Dictionary<TMP_Text, string> tpl = new();
        ScrollRect scroll;

        static GameDatabase DB => GameDatabase.Instance;
        public bool IsOpen => panel && panel.activeSelf;

        void Awake()
        {
            Pausing = false;
            if (cardTemplate) cardTemplate.gameObject.SetActive(false);
            if (panel) panel.SetActive(false);
            if (openButton) openButton.onClick.AddListener(Toggle);
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (sortGrade) sortGrade.onClick.AddListener(() => SetSort(Sort.Grade));
            if (sortCount) sortCount.onClick.AddListener(() => SetSort(Sort.Count));
            if (sortDamage) sortDamage.onClick.AddListener(() => SetSort(Sort.Damage));
            // 휠은 Update 에서 직접 처리 (창 위·밖 같은 빠르기) → ScrollRect 자체 휠은 꺼서 두 번 안 움직이게
            scroll = content ? content.GetComponentInParent<ScrollRect>(true) : null;
            if (scroll) scroll.scrollSensitivity = 0;
        }

        void OnDestroy() => Pausing = false;

        void Update()
        {
            if (IsOpen) Wheel();
            var k = Keyboard.current;
            if (k == null) return;
            bool hot = hotkey != Key.None && k[hotkey].wasPressedThisFrame;
            if (IsOpen && (hot || k.escapeKey.wasPressedThisFrame)) Close();
            else if (!IsOpen && hot && CanOpen()) Open();
        }

        // 다른 멈춤 창 · 컷신 · 결과 화면 중엔 안 열림
        bool CanOpen() => (!Progress.I || Progress.I.IsUnlocked(feature)) && !FxManager.Halt && !GameOver.Active && !Heist.Active && !(NKK.Ults.SuperUltManager.I && NKK.Ults.SuperUltManager.I.Busy);

        public void Toggle() { if (IsOpen) Close(); else if (CanOpen()) Open(); }

        public void Open()
        {
            if (!panel) return;
            panel.SetActive(true); Pausing = true;
            if (drawAbove != null) foreach (var t in drawAbove) if (t && t.parent == transform.parent && t.GetSiblingIndex() > transform.GetSiblingIndex()) transform.SetSiblingIndex(t.GetSiblingIndex());
            Refresh();
        }

        public void Close() { if (panel) panel.SetActive(false); Pausing = false; }

        void SetSort(Sort s) { sort = s; Refresh(); }

        // 휠 위 = 목록 위로, 아래 = 아래로. 내용이 창보다 짧으면 안 움직임
        void Wheel()
        {
            var m = Mouse.current;
            if (m == null || !scroll || !scroll.vertical) return;
            float wheel = m.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) <= 0.01f) return;
            var view = scroll.viewport ? scroll.viewport : (RectTransform)scroll.transform;
            float range = scroll.content.rect.height - view.rect.height;
            if (range <= 0) return;
            scroll.velocity = Vector2.zero;
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + Mathf.Sign(wheel) * wheelPixels / range);
        }

        string T(TMP_Text t) { if (!t) return ""; if (!tpl.TryGetValue(t, out var s)) tpl[t] = s = t.text; return LocText.Tpl(t, s); }
        static TMP_Text Txt(Transform t, string p) => t.Find(p)?.GetComponent<TMP_Text>();
        static Color GradeColor(RatCharacterRow r) { var g = r != null ? DB.GradeOf(r) : null; return g != null && ColorUtility.TryParseHtmlString(g.color, out var c) ? c : Color.white; }

        public void Refresh()
        {
            if (!Rats || DB == null) return;
            // 종별로 묶기
            groups.Clear();
            var byCode = new Dictionary<string, Group>();
            int total = 0;
            foreach (var r in Rats.Rats)
            {
                if (!r || r.temp > 0 || string.IsNullOrEmpty(r.codeId)) continue;
                total++;
                if (!byCode.TryGetValue(r.codeId, out var g)) { g = new Group { code = r.codeId, row = r.Data, dmg = DamageMeter.Of(r.codeId) }; byCode[r.codeId] = g; groups.Add(g); }
                g.n++;
            }
            // 피해 순위 (이번 층, 지금 있는 종끼리)
            var rank = new List<Group>(groups); rank.Sort((a, b) => b.dmg.CompareTo(a.dmg));
            var rankOf = new Dictionary<Group, int>(); for (int i = 0; i < rank.Count; i++) rankOf[rank[i]] = i + 1;
            groups.Sort((a, b) =>
            {
                int c = sort switch
                {
                    Sort.Count => b.n.CompareTo(a.n),
                    Sort.Damage => b.dmg.CompareTo(a.dmg),
                    _ => ((int)(b.row?.Grade ?? 0)).CompareTo((int)(a.row?.Grade ?? 0)),
                };
                if (c == 0) c = b.n.CompareTo(a.n);
                if (c == 0) c = b.dmg.CompareTo(a.dmg);
                return c != 0 ? c : string.CompareOrdinal(a.code, b.code);
            });

            if (summaryText) summaryText.text = T(summaryText).Replace("{n}", total.ToString()).Replace("{max}", Rats.PopCap.ToString())
                .Replace("{kinds}", groups.Count.ToString()).Replace("{dmg}", GameManager.Format(DamageMeter.Total));
            SortColor(sortGrade, sort == Sort.Grade); SortColor(sortCount, sort == Sort.Count); SortColor(sortDamage, sort == Sort.Damage);

            // 카드 (템플릿 복제, 모자라면 더 만들고 남으면 끔)
            if (!cardTemplate || !content) return;
            while (cards.Count < groups.Count) { var c = Instantiate(cardTemplate, content); c.name = "Card" + cards.Count; cards.Add(c); }
            for (int i = 0; i < cards.Count; i++)
            {
                bool on = i < groups.Count; cards[i].gameObject.SetActive(on);
                if (on) Fill(cards[i], groups[i], rankOf[groups[i]]);
            }
        }

        void Fill(RectTransform c, Group g, int rank)
        {
            var gr = g.row != null ? DB.GradeOf(g.row) : null;
            var frame = c.Find("Frame")?.GetComponent<Image>(); if (frame) frame.color = GradeColor(g.row);
            c.Find("Portrait")?.GetComponent<RatPortrait>()?.Show(Rats.artLibrary ? Rats.artLibrary.Get(g.code) : null);
            Set(c, "Name", s => g.row != null ? g.row.character_name : g.code);
            Set(c, "Grade", s => s.Replace("{grade}", gr?.grade_name ?? ""));
            Set(c, "Count", s => s.Replace("{n}", g.n.ToString()));
            Set(c, "Damage", s => g.dmg <= 0 && !string.IsNullOrEmpty(noDamage) ? noDamage : s.Replace("{n}", GameManager.Format(g.dmg)));
            var rk = Txt(c, "Rank");
            if (rk) { rk.gameObject.SetActive(g.dmg > 0); rk.text = T(rk).Replace("{rank}", rank.ToString()); }
        }

        void Set(Transform c, string path, System.Func<string, string> f, Color? col = null)
        {
            var t = Txt(c, path); if (!t) return;
            t.text = f(T(t));
            if (col.HasValue) t.color = col.Value;
        }

        void SortColor(Button b, bool on) { if (b && b.targetGraphic) b.targetGraphic.color = on ? sortOn : sortOff; }
    }
}
