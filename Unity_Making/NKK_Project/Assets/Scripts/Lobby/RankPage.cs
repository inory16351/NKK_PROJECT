using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 탈출 준비실 · 찍찍!! 훈장 (웹 renderRank) + 업적.
    // 위: 훈장 사다리 8칸 (지금 · 단 것 · 앞으로) → 고르면 아래에 그 훈장 정보
    // 왼쪽 아래: 배지·이름·상태 · 효과(시작 쥐 · 윗등급 배율, 지금 → 그 훈장) · 승급 조건 막대 + 훈장 달기 (티어 테이블 연구자료 + 조건 3개)
    // 가운데 아래: 그 훈장에 오는 친구들 (쥐 테이블 unlock_rank, 안 만난 친구는 그림자 + ???)
    // 오른쪽 아래: 업적 (업적 테이블 — 지금은 필살기 완주 Ult_Use, 나중에 조건 타입을 늘려 확장. 못 한 것은 ???)
    // 글은 씬 TMP (자리표시 {tier} {name} {a} {b} {have} {need} {n} {got} {total} {prev}), 코드는 값만.
    public class RankPage : MonoBehaviour
    {
        public LobbyManager manager;
        public RatArtLibrary artLibrary;
        [Tooltip("훈장 배지 rk_1 ~ rk_8")] public Sprite[] badgeSprites;

        [Header("사다리")]
        [Tooltip("훈장 칸 템플릿 (꺼져 있음): 자식 Badge(Image) · Num · Name · Sel · Cur(지금 표시) · Lock")] public Button rankTemplate;
        public Color rankDone = Color.white, rankNext = new(1, 1, 1, 0.55f);

        [Header("고른 훈장")]
        public Image bigBadge;
        [Tooltip("훈장 {tier} · {name}")] public TMP_Text title;
        public TMP_Text status;
        [Tooltip("상태 글 (씬 Words): 지금 단 훈장 · 이미 달았어요 · 다음 목표! · 더 나중 목표")] public TMP_Text wCur, wDone, wNext, wLater;
        [Tooltip("효과 값 글: 시작 쥐 · 윗등급 배율")] public TMP_Text effStart, effOdds;
        [Tooltip("효과 글 모양 (씬 Words): 같을 때 {a} · 오를 때 {a} → {b} / 시작 쥐 {n} · 배율 {n}")] public TMP_Text wEffSame, wEffUp, wEffRats, wEffOdds;

        [Header("승급 조건")]
        public GameObject reqRoot;
        [Tooltip("조건 줄 템플릿 (꺼져 있음): 자식 Label · Value({have} / {need}) · Fill(Image Filled) · Ok")] public RectTransform reqTemplate;
        [Tooltip("조건 이름 (씬 Words): 연구자료 · 최고 층 · 조각 강화 합 · 스킬 노드")] public TMP_Text wReqResearch, wReqFloor, wReqShard, wReqSkill;
        public Button rankButton;
        [Tooltip("훈장 달기 (연구자료 {n})")] public TMP_Text rankLabel;
        [Tooltip("앞 훈장을 먼저 / 이미 단 훈장 글")] public TMP_Text sideNote;
        [Tooltip("({prev}) 훈장을 먼저 달아야 해요 · 이미 단 훈장이에요")] public TMP_Text wNeedPrev, wAlready;
        public Color okColor = new(0.42f, 0.66f, 0.33f), noColor = new(0.85f, 0.55f, 0.35f);

        [Header("오는 친구들")]
        [Tooltip("칸 템플릿 (꺼져 있음): 자식 Portrait(RatPortrait) · Name · Grade(TMP)")] public RectTransform unlockTemplate;
        public TMP_Text unlockTitle;
        public GameObject unlockEmpty;
        [Tooltip("안 만난 친구 이름")] public TMP_Text wUnknown;
        public Color unknownTint = new(0.25f, 0.2f, 0.18f, 0.85f);

        [Header("업적")]
        [Tooltip("줄 템플릿 (꺼져 있음): 자식 Icon · Name · Title · Count({n})")] public RectTransform achvTemplate;
        [Tooltip("업적 {got} / {total}")] public TMP_Text achvCount;
        public Color achvOff = new(1, 1, 1, 0.35f);

        int selTier;
        readonly List<GameObject> spawned = new(), achvSpawned = new();
        readonly Dictionary<TMP_Text, string> tpl = new();
        string T(TMP_Text t) { if (!t) return ""; if (!tpl.TryGetValue(t, out var s)) tpl[t] = s = t.text; return s; }
        string F(TMP_Text t, params (string k, object v)[] kv) { var s = T(t); foreach (var (k, v) in kv) s = s.Replace("{" + k + "}", v?.ToString()); return s; }
        static TMP_Text Txt(Transform t, string p) => t.Find(p)?.GetComponent<TMP_Text>();

        void Awake()
        {
            foreach (var c in new Component[] { rankTemplate, reqTemplate, unlockTemplate, achvTemplate }) if (c) c.gameObject.SetActive(false);
            foreach (var t in new[] { title, rankLabel, unlockTitle, achvCount }) T(t);
            if (reqTemplate) T(Txt(reqTemplate, "Value"));
            if (achvTemplate) T(Txt(achvTemplate, "Count"));
            if (rankButton) rankButton.onClick.AddListener(RankUp);
        }

        static GameDatabase DB => GameDatabase.Instance;
        int MaxTier => DB.Tiers.Count;
        Sprite Badge(int t) => badgeSprites != null && t >= 1 && t <= badgeSprites.Length ? badgeSprites[t - 1] : null;

        // LobbyManager 가 페이지를 열 때 (SendMessage) → 다음 목표 훈장
        public void Render()
        {
            var p = Progress.I; if (!p || !DB) return;
            selTier = Mathf.Clamp(Mathf.Min(MaxTier, p.tier + 1), 1, MaxTier);
            Draw();
            DrawAchv();
        }

        void Draw()
        {
            foreach (var g in spawned) Destroy(g);
            spawned.Clear();
            var p = Progress.I; var db = DB; int r = p.tier;

            // 사다리
            for (int t = 1; t <= MaxTier; t++)
            {
                if (!db.Tiers.TryGetValue(t, out var tr)) continue;
                var b = Instantiate(rankTemplate, rankTemplate.transform.parent); b.gameObject.SetActive(true); spawned.Add(b.gameObject);
                var bd = b.transform.Find("Badge")?.GetComponent<Image>(); if (bd) { bd.sprite = Badge(t); bd.color = t <= r ? rankDone : rankNext; }
                var num = Txt(b.transform, "Num"); if (num) num.text = t.ToString();
                var nm = Txt(b.transform, "Name"); if (nm) nm.text = tr.tier_name;
                Show(b.transform, "Sel", t == selTier); Show(b.transform, "Cur", t == r); Show(b.transform, "Lock", t > r);
                int tt = t; b.onClick.AddListener(() => { selTier = tt; Draw(); });
            }

            if (!db.Tiers.TryGetValue(selTier, out var sk)) return;
            if (bigBadge) { bigBadge.sprite = Badge(selTier); bigBadge.color = selTier <= r ? rankDone : rankNext; }
            if (title) title.text = F(title, ("tier", selTier), ("name", sk.tier_name));
            if (status) status.text = selTier == r ? T(wCur) : selTier < r ? T(wDone) : selTier == r + 1 ? T(wNext) : T(wLater);

            // 효과: 지금 훈장 → 고른 훈장 (고른 게 더 낮으면 지금 값만)
            db.Tiers.TryGetValue(r, out var cur);
            var to = selTier > r ? sk : cur;
            Eff(effStart, wEffRats, cur?.start_rat_count ?? 0, to?.start_rat_count ?? 0, "0");
            Eff(effOdds, wEffOdds, cur?.birth_grade_k ?? 1, to?.birth_grade_k ?? 1, "0.00");

            // 승급 조건 (바로 다음 훈장만)
            bool next = selTier == r + 1;
            if (reqRoot) reqRoot.SetActive(next);
            if (rankButton) rankButton.gameObject.SetActive(next);
            if (sideNote) { sideNote.gameObject.SetActive(!next); sideNote.text = selTier <= r ? T(wAlready) : F(wNeedPrev, ("prev", selTier - 1)); }
            if (next)
            {
                Req(T(wReqResearch), (float)p.research, sk.research_cost);
                foreach (var (type, need) in new[] { (sk.cond1_type, sk.cond1_value), (sk.cond2_type, sk.cond2_value), (sk.cond3_type, sk.cond3_value) })
                {
                    if (string.IsNullOrEmpty(type) || type == "None") continue;
                    var w = type == "Max_Floor" ? wReqFloor : type == "Shard_Level_Sum" ? wReqShard : wReqSkill;
                    Req(T(w), p.CondValue(type), p.CondNeed(type, need));
                }
                if (rankButton) rankButton.interactable = p.CanRankUp();
                if (rankLabel) rankLabel.text = F(rankLabel, ("n", LobbyManager.Fmt(sk.research_cost)));
            }

            // 이 훈장에 오는 친구들
            if (unlockTitle) unlockTitle.text = F(unlockTitle, ("tier", selTier));
            int cnt = 0;
            var list = new List<RatCharacterRow>(); foreach (var rat in db.Rats.Values) if (rat.unlock_rank == selTier) list.Add(rat);
            list.Sort((a, b) => ((int)a.Grade).CompareTo((int)b.Grade));
            foreach (var rat in list)
            {
                cnt++;
                var c = Instantiate(unlockTemplate, unlockTemplate.parent); c.gameObject.SetActive(true); spawned.Add(c.gameObject);
                bool known = p.Seen(rat.code_id);
                c.Find("Portrait")?.GetComponent<RatPortrait>()?.Show(artLibrary ? artLibrary.Get(rat.code_id) : null, known ? Color.white : unknownTint);
                var nm = Txt(c, "Name"); if (nm) nm.text = known ? rat.character_name : T(wUnknown);
                var gr = Txt(c, "Grade");
                if (gr) { var g = db.GradeOf(rat); gr.text = g?.grade_name; if (g != null && ColorUtility.TryParseHtmlString(g.color, out var col)) gr.color = col; }
            }
            if (unlockEmpty) unlockEmpty.SetActive(cnt == 0);
        }

        void Eff(TMP_Text t, TMP_Text shape, float a, float b, string fmt)
        {
            if (!t) return;
            string A = F(shape, ("n", a.ToString(fmt))), B = F(shape, ("n", b.ToString(fmt)));
            t.text = Mathf.Approximately(a, b) ? F(wEffSame, ("a", A)) : F(wEffUp, ("a", A), ("b", B));
        }

        void Req(string label, float have, float need)
        {
            var row = Instantiate(reqTemplate, reqTemplate.parent); row.gameObject.SetActive(true); spawned.Add(row.gameObject);
            bool ok = have >= need;
            var l = Txt(row, "Label"); if (l) l.text = label;
            var v = Txt(row, "Value"); if (v) { v.text = F(v, ("have", LobbyManager.Fmt(Mathf.Floor(have))), ("need", LobbyManager.Fmt(need))); }
            var f = row.Find("Fill")?.GetComponent<Image>(); if (f) { f.fillAmount = need > 0 ? Mathf.Clamp01(have / need) : 1; f.color = ok ? okColor : noColor; }
            Show(row, "Ok", ok);
        }

        // 업적 (업적 테이블): 달성한 것 먼저. 못 한 것은 이름 ??? + 조건 설명만 (힌트)
        void DrawAchv()
        {
            foreach (var g in achvSpawned) Destroy(g);
            achvSpawned.Clear();
            var p = Progress.I; var db = DB; int got = 0, total = 0;
            var list = new List<AchievementRow>(db.Achievements);
            list.Sort((a, b) => { int c = p.AchvDone(b).CompareTo(p.AchvDone(a)); return c != 0 ? c : a.sort_order.CompareTo(b.sort_order); });
            foreach (var a in list)
            {
                total++; bool has = p.AchvDone(a); if (has) got++;
                var row = Instantiate(achvTemplate, achvTemplate.parent); row.gameObject.SetActive(true); achvSpawned.Add(row.gameObject);
                var ic = row.Find("Icon")?.GetComponent<Image>(); if (ic) { ic.sprite = IconBook.I ? IconBook.I.Get(a.achv_icon) : null; ic.enabled = ic.sprite; ic.color = has ? Color.white : achvOff; }
                var nm = Txt(row, "Name"); if (nm) nm.text = has ? a.achv_name : T(wUnknown);
                var tt = Txt(row, "Title"); if (tt) tt.text = AchvDesc(a);
                var cn = Txt(row, "Count"); if (cn) { cn.gameObject.SetActive(has); cn.text = F(cn, ("n", p.AchvProgress(a))); }
            }
            if (achvCount) achvCount.text = F(achvCount, ("got", got), ("total", total));
        }

        static string AchvDesc(AchievementRow a)
        {
            string s = a.achv_desc ?? "";
            if (s.Contains("{ult}")) s = s.Replace("{ult}", DB.Ultimates.TryGetValue(a.target_id, out var u) ? u.ultimate_name : "");
            return s;
        }

        void RankUp()
        {
            var p = Progress.I;
            if (!p.RankUp()) return;
            selTier = Mathf.Min(MaxTier, p.tier + 1);
            Draw();
            if (manager && manager.home) manager.home.Refresh();
        }

        static void Show(Transform t, string child, bool on) { var c = t.Find(child); if (c) c.gameObject.SetActive(on); }
    }
}
