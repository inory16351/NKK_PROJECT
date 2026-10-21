using System.Collections.Generic;
using NKK.Data;
using NKK.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Rats
{
    // HUD 딜 미터기 (롤체 딜 미터기): 이번 층에 피해를 가장 많이 준 쥐 종 상위 N 개 (화면 오른쪽).
    // 순위가 바뀌면 유튜브 순위 영상(바 차트 레이스)처럼: 올라가는 줄은 커지며 위로 떠서(맨 앞에 그림) 앞질러 가고,
    // 밀려난 줄은 한 박자 늦게 아래로 밀려 내려가며 살짝 더 내려갔다 제자리 (눌림) · ▲/▼ 표시 · 번쩍.
    // 새로 들어온 종은 아래에서 떠오르고, 밀려난 종은 아래로 가라앉으며 사라짐. 숫자는 올라가며 셈.
    // 줄은 하이라키에 미리 만들어 둔 것을 돌려 씀 (보이는 줄 수 + 2개 이상).
    // 줄 자식: Flash(Image) · Portrait(RatPortrait) · Name · Value({n} {p}) · Rank({rank}) · Medal(Image) · Up(Image) · Down(Image) · Bar/Fill(Image)
    public class DamageMeterUI : MonoBehaviour
    {
        public RatManager Rats;
        [Tooltip("돌려 쓰는 줄들 (꺼 두지 말 것, 처음엔 코드가 투명하게 만듦)")] public RectTransform[] rows;
        [Tooltip("보이는 순위 수")] public int showCount = 5;
        [Tooltip("줄 간격 (px, 위 → 아래)")] public float rowGap = 58;
        [Tooltip("순위를 다시 매기는 간격 (초). 너무 짧으면 계속 출렁임")] public float refresh = 0.35f;

        [Header("자리 바꾸기 연출")]
        [Tooltip("자리 바꾸는 시간 (초)")] public float swapTime = 0.5f;
        [Tooltip("올라가는 줄: 가운데에서 커지는 배율 · 왼쪽으로 튀어나오는 거리 (px)")] public float riseScale = 0.12f, riseShift = 16;
        [Tooltip("올라가는 줄: 도착할 때 넘쳤다 돌아오는 정도 (easeOutBack)")] public float riseOvershoot = 1.6f;
        [Tooltip("밀려난 줄: 늦게 출발 (초) · 작아지는 배율 · 오른쪽으로 밀리는 거리 (px) · 흐려짐")] public float pushDelay = 0.1f, pushScale = 0.06f, pushShift = 8, pushDim = 0.35f;
        [Tooltip("밀려난 줄: 목표보다 더 내려갔다 돌아오는 정도 (easeOutBack)")] public float pushOvershoot = 2.2f;
        [Tooltip("들어오기/나가기 시간 (초)")] public float enterTime = 0.45f, leaveTime = 0.35f;
        [Tooltip("▲▼ 표시 시간 (초)")] public float chipTime = 1.2f;
        [Tooltip("올라갈 때 번쩍 (Flash 알파 최대)")] public float flashAlpha = 0.55f;
        [Tooltip("순위 숫자 통통 (배율)")] public float rankPop = 0.45f;
        [Tooltip("막대 길이·숫자 따라가기 빠르기")] public float barSharp = 8;

        [Header("모양")]
        [Tooltip("1·2·3등 표시 그림 (빈칸 = 숫자만)")] public Sprite[] medals = new Sprite[3];
        [Tooltip("막대 색 = 등급 색 (끄면 막대 그림 색 그대로)")] public bool gradeColorBar = true;
        [Tooltip("피해가 하나도 없으면 패널 숨김")] public bool hideWhenEmpty = true;
        [Tooltip("패널 전체 (숨길 때 알파)")] public CanvasGroup group;
        [Tooltip("이번 층 피해 합 ({n}, 비워도 됨)")] public TMP_Text totalText;
        public float panelFade = 5;

        enum Move { None, Rise, Push, Enter, Leave }
        class Slot
        {
            public RectTransform rt; public CanvasGroup cg;
            public RatPortrait portrait; public TMP_Text name, value, rank; public Image medal, up, down, fill, flash;
            public string code; public int target = -1, shownRank = -1;
            public float y, fromY, toY, t, dur, delay; public Move move;
            public float frac, upT, downT, flashT, popT; public double shownVal;
            public string valTpl, rankTpl; public Color upCol, downCol, flashCol;
        }
        readonly List<Slot> slots = new();
        readonly List<KeyValuePair<string, double>> sorted = new();
        readonly HashSet<string> topCodes = new();
        float refreshT; int seenVer = -1; double top = 1; string totalTpl; double totalShown;

        static GameDatabase DB => GameDatabase.Instance;

        void Awake()
        {
            foreach (var r in rows)
            {
                if (!r) continue;
                var s = new Slot { rt = r, cg = r.GetComponent<CanvasGroup>() ?? r.gameObject.AddComponent<CanvasGroup>() };
                s.portrait = r.Find("Portrait")?.GetComponent<RatPortrait>();
                s.name = Txt(r, "Name"); s.value = Txt(r, "Value"); s.rank = Txt(r, "Rank");
                s.medal = Img(r, "Medal"); s.up = Img(r, "Up"); s.down = Img(r, "Down"); s.fill = Img(r, "Bar/Fill"); s.flash = Img(r, "Flash");
                s.valTpl = s.value ? s.value.text : "{n}"; s.rankTpl = s.rank ? s.rank.text : "{rank}";
                if (s.up) { s.upCol = s.up.color; s.up.enabled = false; }
                if (s.down) { s.downCol = s.down.color; s.down.enabled = false; }
                if (s.flash) { s.flashCol = s.flash.color; s.flash.enabled = false; }
                s.cg.alpha = 0; s.cg.blocksRaycasts = false; s.y = Y(showCount); Place(s, 0, 1);
                slots.Add(s);
            }
            if (totalText) totalTpl = totalText.text;
            if (group && hideWhenEmpty) group.alpha = 0;
        }

        static TMP_Text Txt(Transform t, string p) => t.Find(p)?.GetComponent<TMP_Text>();
        static Image Img(Transform t, string p) => t.Find(p)?.GetComponent<Image>();
        float Y(int rank) => -rank * rowGap;
        static void Place(Slot s, float x, float scale) { s.rt.anchoredPosition = new Vector2(x, s.y); s.rt.localScale = Vector3.one * scale; }

        // 끝에서 넘쳤다 돌아옴 (k = 넘치는 정도)
        static float Back(float p, float k) { p -= 1; return 1 + (k + 1) * p * p * p + k * p * p; }
        static float InCubic(float p) => p * p * p;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if ((refreshT -= dt) <= 0 || seenVer < 0) { refreshT = refresh; if (seenVer != DamageMeter.Version) { seenVer = DamageMeter.Version; Rank(); } }
            if (group && hideWhenEmpty) group.alpha = Mathf.MoveTowards(group.alpha, DamageMeter.Total > 0 ? 1 : 0, dt * panelFade);
            float kb = 1 - Mathf.Exp(-barSharp * dt);
            foreach (var s in slots) Animate(s, dt, kb);
            if (totalText && totalTpl != null)
            {
                totalShown = DamageMeter.Total < totalShown ? DamageMeter.Total : totalShown + (DamageMeter.Total - totalShown) * kb;
                totalText.text = totalTpl.Replace("{n}", GameManager.Format(totalShown));
            }
        }

        void Animate(Slot s, float dt, float kb)
        {
            float x = 0, scale = 1, alpha = s.target >= 0 ? 1 : 0;
            if (s.move != Move.None)
            {
                s.t += dt;
                float p = Mathf.Clamp01((s.t - s.delay) / Mathf.Max(0.01f, s.dur)), arc = Mathf.Sin(Mathf.PI * p);
                switch (s.move)
                {
                    case Move.Rise:      // 커지며 떠올라 앞지름
                        s.y = Mathf.LerpUnclamped(s.fromY, s.toY, Back(p, riseOvershoot));
                        scale = 1 + riseScale * arc; x = -riseShift * arc; break;
                    case Move.Push:      // 한 박자 늦게 밀려 내려감 (더 내려갔다 돌아옴)
                        s.y = Mathf.LerpUnclamped(s.fromY, s.toY, Back(p, pushOvershoot));
                        scale = 1 - pushScale * arc; x = pushShift * arc; alpha = 1 - pushDim * arc; break;
                    case Move.Enter:     // 아래에서 떠오름
                        s.y = Mathf.LerpUnclamped(s.fromY, s.toY, Back(p, riseOvershoot)); alpha = p; break;
                    case Move.Leave:     // 가라앉으며 사라짐
                        s.y = Mathf.Lerp(s.fromY, s.toY, InCubic(p)); alpha = 1 - p; break;
                }
                if (p >= 1) { if (s.move == Move.Leave) s.code = null; s.move = Move.None; s.y = s.toY; }
            }
            else if (s.target < 0) alpha = 0;
            s.cg.alpha = alpha; Place(s, x, scale);
            if (s.code == null) return;

            double v = DamageMeter.Of(s.code);
            s.frac = Mathf.Lerp(s.frac, (float)(top > 0 ? v / top : 0), kb);
            if (s.fill) s.fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(s.frac), s.fill.rectTransform.anchorMax.y);
            // 숫자는 올라가며 셈
            s.shownVal = v < s.shownVal ? v : s.shownVal + (v - s.shownVal) * kb;
            if (v - s.shownVal < 0.5) s.shownVal = v;
            if (s.value) s.value.text = s.valTpl.Replace("{n}", GameManager.Format(System.Math.Round(s.shownVal))).Replace("{p}", (DamageMeter.Total > 0 ? v / DamageMeter.Total * 100 : 0).ToString("0"));
            // ▲▼ · 번쩍 · 순위 숫자 통통
            s.upT = Mathf.Max(0, s.upT - dt); s.downT = Mathf.Max(0, s.downT - dt); s.flashT = Mathf.Max(0, s.flashT - dt); s.popT = Mathf.Max(0, s.popT - dt);
            Chip(s.up, s.upT, s.upCol); Chip(s.down, s.downT, s.downCol);
            if (s.flash) { float f = s.flashT / Mathf.Max(0.01f, swapTime); s.flash.enabled = f > 0; var c = s.flashCol; c.a = flashAlpha * f * f; s.flash.color = c; }
            float pop = s.popT / 0.3f;
            if (s.rank) s.rank.transform.localScale = Vector3.one * (1 + rankPop * pop * pop);
            if (s.medal) s.medal.transform.localScale = Vector3.one * (1 + rankPop * pop * pop);
        }

        void Chip(Image img, float t, Color col)
        {
            if (!img) return;
            img.enabled = t > 0; if (t <= 0) return;
            float k = t / chipTime; col.a *= Mathf.Clamp01(k * 3);          // 마지막 1/3 동안 사라짐
            img.color = col;
            img.rectTransform.localScale = Vector3.one * (1 + 0.3f * Mathf.Clamp01((k - 0.8f) * 5));   // 처음에 톡
        }

        // 순위 다시 매기기 → 줄마다 목표 자리 · 연출 시작
        void Rank()
        {
            sorted.Clear();
            foreach (var kv in DamageMeter.Dealt) if (kv.Value > 0) sorted.Add(kv);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            int n = Mathf.Min(showCount, sorted.Count);
            top = n > 0 ? sorted[0].Value : 1;
            topCodes.Clear(); for (int i = 0; i < n; i++) topCodes.Add(sorted[i].Key);

            // 층이 바뀌어 다 비면 바로 지움
            if (n == 0) { foreach (var s in slots) { s.target = -1; s.code = null; s.move = Move.None; s.shownRank = -1; } return; }

            foreach (var s in slots)
                if (s.code != null && s.target >= 0 && !topCodes.Contains(s.code)) { s.target = -1; s.shownRank = -1; Begin(s, Move.Leave, Y(showCount), leaveTime, 0); }

            Slot riser = null;
            for (int i = 0; i < n; i++)
            {
                string code = sorted[i].Key;
                var s = slots.Find(x => x.code == code) ?? Take(code);
                if (s == null) continue;
                int old = s.shownRank; s.target = i;
                if (old == i) continue;
                if (old < 0) Begin(s, Move.Enter, Y(i), enterTime, 0);
                else if (i < old) { Begin(s, Move.Rise, Y(i), swapTime, 0); s.upT = chipTime; s.downT = 0; s.flashT = swapTime; if (riser == null || i < riser.target) riser = s; }
                else { Begin(s, Move.Push, Y(i), swapTime, pushDelay); s.downT = chipTime; s.upT = 0; }
                SetRank(s, i);
            }
            if (riser != null) riser.rt.SetAsLastSibling();          // 앞지르는 줄을 맨 앞에 그림
        }

        void Begin(Slot s, Move m, float toY, float dur, float delay) { s.move = m; s.fromY = s.y; s.toY = toY; s.t = 0; s.dur = dur; s.delay = delay; }

        // 빈 줄을 새 종에 붙임 (아래 칸에서 떠오름)
        Slot Take(string code)
        {
            // 빈 줄이 없으면 순위 밖으로 밀려 사라지는 중인 줄을 가장 투명한 것부터
            Slot s = slots.Find(x => x.code == null);
            if (s == null) foreach (var x in slots) if (!topCodes.Contains(x.code) && (s == null || x.cg.alpha < s.cg.alpha)) s = x;
            if (s == null) return null;
            s.code = code; s.cg.alpha = 0; s.y = Y(showCount); s.frac = 0; s.shownVal = 0; s.shownRank = -1; s.upT = s.downT = s.flashT = 0; s.move = Move.None;
            var row = DB != null && DB.RatsByCode.TryGetValue(code, out var r) ? r : null;
            if (s.name) s.name.text = row != null ? row.character_name : code;
            if (s.portrait) s.portrait.Show(Rats && Rats.artLibrary ? Rats.artLibrary.Get(code) : null);
            if (s.fill && gradeColorBar && row != null) { var g = DB.GradeOf(row); if (g != null && ColorUtility.TryParseHtmlString(g.color, out var c)) s.fill.color = c; }
            return s;
        }

        void SetRank(Slot s, int i)
        {
            s.shownRank = i; s.popT = 0.3f;
            if (s.rank) s.rank.text = s.rankTpl.Replace("{rank}", (i + 1).ToString());
            var m = medals != null && i < medals.Length ? medals[i] : null;
            if (s.medal) { s.medal.sprite = m; s.medal.enabled = m; }
            if (s.rank) s.rank.enabled = !m;
        }
    }
}
