using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 탈출 준비실 · 치즈 창고 = 공용 스킬 지도. 찍찍!! 훈장마다 트리 하나 (위 탭으로 고름).
    // 노드는 공용 스킬 테이블 pos_x·pos_y 칸에, 여는 노드(link_1·link_2)와 끈으로 연결. 끌어서 이동 · 휠로 확대.
    // 노드 상태 (Progress.StateOf): 활성화 · 열림(살 수 있음) · 잠김(이어진 노드 먼저) · 훈장 부족(자물쇠). 선택된 노드를 한 번 더 누르면 활성화.
    // 아직 못 단 훈장 탭을 고르면 아래에 훈장 승급 패널 (연구자료 + 조건 3개). 바로 다음 훈장이면 승급 버튼.
    // 글은 전부 씬의 TMP 텍스트에 적혀 있고, 코드는 자리표시({cost} {res} {state} {tier} {have} {need} {name} {branch})와 테이블 값(이름·설명)만 채움.
    public class SkillPage : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        public LobbyManager manager;

        [Header("지도")]
        [Tooltip("지도 보이는 영역 (RectMask2D)")] public RectTransform viewport;
        [Tooltip("노드·끈이 들어가는 판 (이동·확대됨)")] public RectTransform content;
        public RectTransform lineLayer, nodeLayer;
        [Tooltip("칸 간격 (픽셀)")] public float step = 190;
        public Vector2 zoomRange = new(0.45f, 1.5f);
        [Tooltip("휠 한 칸 확대 배율")] public float zoomStep = 1.12f;

        [Header("훈장 탭 (1~8훈장 순서, 자식 Lock = 못 단 훈장 자물쇠 · Sel = 고른 탭)")]
        public Button[] tierTabs;

        [Header("템플릿 (꺼져 있음)")]
        [Tooltip("노드: 자식 Icon(Image) · Name · Cost(TMP) · Lock(Image) · LockText(TMP) · Sel(선택 표시)")] public Button nodeTemplate;
        [Tooltip("핵심 노드 (마름모), 자식 구성 같음")] public Button keyTemplate;
        [Tooltip("끈 (Image, 가로로 늘림)")] public Image lineTemplate;
        public Sprite nodeOff, nodeOn, nodeLock, keyOff, keyOn;
        public Color lineOn = new(0.95f, 0.76f, 0.31f), lineOpen = new(0.85f, 0.75f, 0.6f), lineOff = new(0.55f, 0.47f, 0.4f, 0.6f);

        [Header("노드 상태 구분 (획득 vs 미획득)")]
        [Tooltip("획득 노드 뒤 금빛 고리 (템플릿 자식 Glow) 색")] public Color glowColor = new(1f, 0.82f, 0.3f, 0.95f);
        [Tooltip("고리 도는 속도 (도/초) · 깜빡임 속도")] public float glowSpin = 40, glowPulse = 3;
        [Tooltip("미획득(열림) 노드 판 색 · 아이콘 색")] public Color openNodeTint = new(0.86f, 0.83f, 0.8f), openIconTint = new(0.72f, 0.68f, 0.64f, 0.9f);
        [Tooltip("잠김·훈장 부족 노드 판 색 · 아이콘 색")] public Color lockNodeTint = new(0.55f, 0.52f, 0.5f, 0.85f), lockIconTint = new(0.35f, 0.33f, 0.31f, 0.6f);
        [Tooltip("노드 이름 글 색: 획득 · 미획득")] public Color nameOwned = new(0.29f, 0.2f, 0.1f), nameOff = new(0.5f, 0.45f, 0.4f);
        readonly List<Image> glows = new();

        [Header("상세 카드")]
        public Image detailIcon;
        public TMP_Text detailName, detailState, detailExplain, detailReq;
        public Image branchRibbon;
        public TMP_Text branchText;
        [Tooltip("비용 줄: 치즈 · 연구자료 (연구자료 0 이면 숨김)")] public TMP_Text costCheese, costResearch;
        public GameObject costResearchRow;
        public Button buyButton;
        public TMP_Text buyLabel;
        [Tooltip("상태 글 (씬 Words): 활성화 · 열림 · 잠김 · 훈장 부족({tier})")] public TMP_Text stOwned, stOpen, stLocked, stTier;
        [Tooltip("버튼 글: 이미 활성화 · 못 삼")] public TMP_Text wOwned, wCant;
        [Tooltip("노드 비용 글 뒤에 붙는 연구자료 ({n})")] public TMP_Text wNodeResearch;
        [Tooltip("시작점 노드 설명 ({tier} {name})")] public TMP_Text wRoot;

        [Header("훈장 승급 패널 (못 단 훈장 탭)")]
        public GameObject rankPanel;
        [Tooltip("제목 ({tier} {name})")] public TMP_Text rankTitle;
        [Tooltip("조건 줄 3개 + 연구자료 줄 (자리 {have} {need}). 조건 이름 글은 씬 Words")] public TMP_Text[] rankConds;
        public TMP_Text rankResearch;
        [Tooltip("조건 이름 글: 최고 층 · 조각 강화 합 · 스킬 노드 수 ({have} {need})")] public TMP_Text cMaxFloor, cShard, cSkill;
        public Button rankButton;
        [Tooltip("앞 훈장을 먼저 달아야 할 때 글 ({tier})")] public TMP_Text rankPrev;
        public Color okColor = new(0.29f, 0.55f, 0.3f), noColor = new(0.75f, 0.35f, 0.3f);

        int viewTier = 1, sel;
        float zoom = 1; bool dragging;
        readonly List<GameObject> spawned = new();
        readonly Dictionary<TMP_Text, string> tpl = new();
        readonly Dictionary<string, Sprite> icons = new();

        string T(TMP_Text t) { if (!t) return ""; if (!tpl.TryGetValue(t, out var s)) tpl[t] = s = t.text; return s; }
        string F(TMP_Text t, params (string k, object v)[] kv) { var s = T(t); foreach (var (k, v) in kv) s = s.Replace("{" + k + "}", v?.ToString()); return s; }
        void SetT(TMP_Text t, params (string k, object v)[] kv) { if (t) t.text = F(t, kv); }

        void Awake()
        {
            foreach (var t in new Component[] { nodeTemplate, keyTemplate, lineTemplate }) if (t) t.gameObject.SetActive(false);
            if (buyButton) buyButton.onClick.AddListener(Buy);
            if (rankButton) rankButton.onClick.AddListener(RankUp);
            if (tierTabs != null) for (int i = 0; i < tierTabs.Length; i++) { int t = i + 1; if (tierTabs[i]) tierTabs[i].onClick.AddListener(() => ShowTier(t)); }
            foreach (var t in new[] { buyLabel, detailState, costCheese, costResearch, branchText, rankTitle, rankResearch, rankPrev }) T(t);
            if (rankConds != null) foreach (var t in rankConds) T(t);
        }

        [Tooltip("스킬 아이콘 (파일 이름 cs_<이름>)")] public Sprite[] iconSprites;
        void CacheIcons() { if (iconSprites == null) return; foreach (var s in iconSprites) if (s) icons[s.name] = s; }
        Sprite Icon(CommonSkillRow s)
        {
            if (s == null || string.IsNullOrEmpty(s.skill_asset)) return null;
            string n = s.skill_asset.Substring(s.skill_asset.LastIndexOf('/') + 1);
            return icons.TryGetValue(n, out var sp) ? sp : null;
        }
        [Tooltip("훈장 배지 (rk_1 ~ rk_8) — 시작점 노드 아이콘")] public Sprite[] badgeSprites;
        Sprite RootIcon(int t) => badgeSprites != null && t - 1 < badgeSprites.Length ? badgeSprites[t - 1] : null;

        Color BranchColor(CommonSkillRow s)
        {
            var db = GameDatabase.Instance;
            return db.SkillBranches.TryGetValue(s.Branch, out var b) && ColorUtility.TryParseHtmlString(b.color, out var c) ? c : Color.white;
        }

        Vector2 At(CommonSkillRow s) => new(s.pos_x * step, -s.pos_y * step);
        static string TierName(int t) => GameDatabase.Instance.Tiers.TryGetValue(t, out var r) ? r.tier_name : "";

        // LobbyManager 가 페이지를 열 때 (SendMessage) → 지금 훈장 트리
        public void Render()
        {
            var p = Progress.I;
            int max = GameDatabase.Instance.CommonSkillsByTier.Count;
            ShowTier(Mathf.Clamp(p ? p.tier : 1, 1, Mathf.Max(1, max)));
        }

        public void ShowTier(int t)
        {
            var db = GameDatabase.Instance;
            bool changed = t != viewTier || sel == 0;
            viewTier = t;
            if (changed && db.CommonSkillsByTier.TryGetValue(t, out var l) && l.Count > 0)
            {
                sel = l[0].skill_id;
                foreach (var s in l) if (s.IsRoot) sel = s.skill_id;
                FitView(l);
            }
            Draw();
        }

        // 트리 전체가 보이게 확대 배율·위치 맞춤
        void FitView(List<CommonSkillRow> l)
        {
            if (!content || !viewport || l.Count == 0) return;
            Vector2 mn = new(float.MaxValue, float.MaxValue), mx = new(float.MinValue, float.MinValue);
            foreach (var s in l) { var a = At(s); mn = Vector2.Min(mn, a); mx = Vector2.Max(mx, a); }
            var size = mx - mn + new Vector2(step * 1.2f, step * 1.4f);
            var vr = viewport.rect;
            zoom = Mathf.Clamp(Mathf.Min(vr.width / size.x, vr.height / size.y), zoomRange.x, zoomRange.y);
            content.localScale = Vector3.one * zoom;
            content.anchoredPosition = -(mn + mx) / 2 * zoom;
        }

        void Draw()
        {
            CacheIcons();
            foreach (var g in spawned) Destroy(g);
            spawned.Clear(); glows.Clear();
            var p = Progress.I; var db = GameDatabase.Instance;
            // 탭
            if (tierTabs != null)
                for (int i = 0; i < tierTabs.Length; i++)
                {
                    var b = tierTabs[i]; if (!b) continue;
                    var lk = b.transform.Find("Lock"); if (lk) lk.gameObject.SetActive(p.tier < i + 1);
                    var se = b.transform.Find("Sel"); if (se) se.gameObject.SetActive(viewTier == i + 1);
                }
            if (!db.CommonSkillsByTier.TryGetValue(viewTier, out var nodes)) nodes = new List<CommonSkillRow>();
            // 끈: 여는 노드 → 이 노드
            foreach (var s in nodes)
                foreach (int lid in new[] { s.link_1, s.link_2 })
                {
                    if (lid == 0 || !db.CommonSkillsById.TryGetValue(lid, out var r)) continue;
                    var st = p.StateOf(s);
                    var l = Instantiate(lineTemplate, lineLayer); l.gameObject.SetActive(true); spawned.Add(l.gameObject);
                    Vector2 a = At(r), b = At(s), d = b - a;
                    var rt = l.rectTransform; rt.anchoredPosition = (a + b) / 2; rt.sizeDelta = new Vector2(d.magnitude, rt.sizeDelta.y);
                    rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    l.color = st == Progress.SkillState.Owned ? lineOn : st == Progress.SkillState.Open ? lineOpen : lineOff;
                }
            // 노드
            foreach (var s in nodes)
            {
                var st = p.StateOf(s);
                bool key = s.IsKey || s.IsRoot;
                var b = Instantiate(key ? keyTemplate : nodeTemplate, nodeLayer); b.gameObject.SetActive(true); spawned.Add(b.gameObject);
                ((RectTransform)b.transform).anchoredPosition = At(s);
                var img = b.GetComponent<Image>();
                img.sprite = st == Progress.SkillState.TierLock ? nodeLock : st == Progress.SkillState.Owned ? (key ? keyOn : nodeOn) : (key ? keyOff : nodeOff);
                bool owned = st == Progress.SkillState.Owned;
                img.color = owned ? Color.white : st == Progress.SkillState.Open ? openNodeTint : lockNodeTint;
                var bc = BranchColor(s);
                Child(b, "Icon", true, c =>
                {
                    var i = c.GetComponent<Image>(); i.sprite = s.IsRoot ? RootIcon(s.tier) : Icon(s);
                    i.color = owned ? Color.white : st == Progress.SkillState.Open ? openIconTint : lockIconTint;
                });
                // 획득 표시: 뒤 금빛 고리(돌며 깜빡) + 반짝이 배지
                Child(b, "Glow", owned, c => { var i = c.GetComponent<Image>(); if (i) { i.color = glowColor; glows.Add(i); } });
                Child(b, "Badge", owned && !s.IsRoot, null);
                Child(b, "Q", false, null);
                Child(b, "Lv", false, null);
                Child(b, "Lock", st == Progress.SkillState.TierLock && s.IsRoot, null);
                Child(b, "LockText", false, null);
                Child(b, "Name", true, c => { var t = c.GetComponent<TMP_Text>(); t.text = s.IsRoot ? F(wRoot, ("tier", s.tier), ("name", TierName(s.tier))) : s.skill_name; t.color = owned || s.IsRoot ? nameOwned : nameOff; });
                Child(b, "Cost", !s.IsRoot && st != Progress.SkillState.Owned, c =>
                {
                    var t = c.GetComponent<TMP_Text>();
                    string res = s.cost_research > 0 && wNodeResearch ? F(wNodeResearch, ("n", LobbyManager.Fmt(s.cost_research))) : "";
                    t.text = F(t, ("cost", LobbyManager.Fmt(s.cost_cheese))) + res;
                    t.color = st == Progress.SkillState.Open && p.CanAffordSkill(s) ? new Color(0.29f, 0.22f, 0.17f) : new Color(0.62f, 0.5f, 0.42f);
                });
                Child(b, "Sel", s.skill_id == sel, c => { var i = c.GetComponent<Image>(); if (i) i.color = bc; });
                int id = s.skill_id;
                b.onClick.AddListener(() => { if (dragging) return; if (sel == id) Buy(); else { sel = id; Draw(); } });
            }
            DrawDetail();
            DrawRank();
        }

        // 획득 노드 고리: 천천히 돌며 깜빡
        void Update()
        {
            if (glows.Count == 0) return;
            float t = Time.unscaledTime, a = glowColor.a * (0.65f + 0.35f * Mathf.Sin(t * glowPulse));
            foreach (var g in glows)
            {
                if (!g) continue;
                g.rectTransform.localRotation = Quaternion.Euler(0, 0, -t * glowSpin);
                var c = glowColor; c.a = a; g.color = c;
            }
        }

        static void Child(Button b, string name, bool on, System.Action<Transform> set)
        {
            var c = b.transform.Find(name); if (!c) return;
            c.gameObject.SetActive(on); if (on) set?.Invoke(c);
        }

        void DrawDetail()
        {
            var p = Progress.I; var db = GameDatabase.Instance;
            if (!db.CommonSkillsById.TryGetValue(sel, out var s)) return;
            var st = p.StateOf(s);
            if (detailIcon) { detailIcon.sprite = s.IsRoot ? RootIcon(s.tier) : Icon(s); detailIcon.color = st == Progress.SkillState.Owned || st == Progress.SkillState.Open ? Color.white : new Color(0.55f, 0.5f, 0.45f, 0.8f); }
            if (detailName) detailName.text = s.IsRoot ? F(wRoot, ("tier", s.tier), ("name", TierName(s.tier))) : s.skill_name;
            if (detailExplain) detailExplain.text = s.skill_explain;
            if (detailState)
                detailState.text = st switch
                {
                    Progress.SkillState.Owned => T(stOwned),
                    Progress.SkillState.Open => T(stOpen),
                    Progress.SkillState.Locked => T(stLocked),
                    _ => F(stTier, ("tier", s.tier)),
                };
            if (db.SkillBranches.TryGetValue(s.Branch, out var br)) { SetT(branchText, ("branch", br.branch_name)); if (branchRibbon) branchRibbon.color = BranchColor(s); }
            if (detailReq) detailReq.gameObject.SetActive(false);
            bool buyable = !s.IsRoot && st != Progress.SkillState.Owned;
            SetT(costCheese, ("cost", LobbyManager.Fmt(s.cost_cheese)));
            SetT(costResearch, ("res", LobbyManager.Fmt(s.cost_research)));
            if (costCheese) costCheese.transform.parent.gameObject.SetActive(buyable);
            if (costResearchRow) costResearchRow.SetActive(buyable && s.cost_research > 0);
            if (costCheese) costCheese.color = p.cheese >= s.cost_cheese ? new Color(0.29f, 0.22f, 0.17f) : noColor;
            if (costResearch) costResearch.color = p.research >= s.cost_research ? new Color(0.29f, 0.22f, 0.17f) : noColor;
            if (buyButton) { buyButton.gameObject.SetActive(!s.IsRoot); buyButton.interactable = p.CanBuySkill(s); }
            if (buyLabel) buyLabel.text = st == Progress.SkillState.Owned ? T(wOwned) : p.CanBuySkill(s) ? T(buyLabel) : T(wCant);
        }

        // 못 단 훈장 탭: 승급 조건 · 연구자료 · 버튼
        void DrawRank()
        {
            var p = Progress.I; var db = GameDatabase.Instance;
            bool show = rankPanel && viewTier > p.tier;
            if (rankPanel) rankPanel.SetActive(show);
            if (!show || !db.Tiers.TryGetValue(viewTier, out var tr)) return;
            SetT(rankTitle, ("tier", viewTier), ("name", tr.tier_name));
            bool next = viewTier == p.tier + 1;
            var conds = new[] { (tr.cond1_type, tr.cond1_value), (tr.cond2_type, tr.cond2_value), (tr.cond3_type, tr.cond3_value) };
            for (int i = 0; rankConds != null && i < rankConds.Length; i++)
            {
                var t = rankConds[i]; if (!t) continue;
                bool on = i < conds.Length && !string.IsNullOrEmpty(conds[i].Item1) && conds[i].Item1 != "None";
                t.gameObject.SetActive(on); if (!on) continue;
                var (type, need) = conds[i];
                var w = type == "Max_Floor" ? cMaxFloor : type == "Shard_Level_Sum" ? cShard : cSkill;
                float have = p.CondValue(type); need = p.CondNeed(type, need);
                t.text = F(w, ("have", Mathf.FloorToInt(have)), ("need", Mathf.RoundToInt(need)));
                t.color = have >= need ? okColor : noColor;
            }
            SetT(rankResearch, ("have", LobbyManager.Fmt(p.research)), ("need", LobbyManager.Fmt(tr.research_cost)));
            if (rankResearch) rankResearch.color = p.research >= tr.research_cost ? okColor : noColor;
            if (rankButton) { rankButton.gameObject.SetActive(next); rankButton.interactable = next && p.CanRankUp(); }
            if (rankPrev) { rankPrev.gameObject.SetActive(!next); SetT(rankPrev, ("tier", viewTier - 1)); }
        }

        void Buy()
        {
            var p = Progress.I; var db = GameDatabase.Instance;
            if (!db.CommonSkillsById.TryGetValue(sel, out var s) || !p.BuySkill(s)) return;
            Draw();
        }

        void RankUp()
        {
            var p = Progress.I;
            if (!p.RankUp()) return;
            ShowTier(p.tier);
        }

        // ── 지도 끌기 · 휠 확대 ──
        public void OnBeginDrag(PointerEventData e) { dragging = true; }
        public void OnDrag(PointerEventData e)
        {
            var cv = GetComponentInParent<Canvas>();
            if (content) content.anchoredPosition += e.delta / (cv ? cv.scaleFactor : 1);
        }
        public void OnEndDrag(PointerEventData e) { Invoke(nameof(EndDrag), 0); }
        void EndDrag() => dragging = false;
        public void OnScroll(PointerEventData e)
        {
            if (!content || !viewport) return;
            float z0 = zoom; zoom = Mathf.Clamp(zoom * (e.scrollDelta.y > 0 ? zoomStep : 1 / zoomStep), zoomRange.x, zoomRange.y);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, e.pressEventCamera, out var m);
            content.anchoredPosition = m - (m - content.anchoredPosition) * (zoom / z0);
            content.localScale = Vector3.one * zoom;
        }
    }
}
