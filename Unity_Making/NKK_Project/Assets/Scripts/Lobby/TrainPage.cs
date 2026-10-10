using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 탈출 준비실 · 쳇바퀴 훈련 (웹 renderRats / renderRatDetail): 같은 친구를 또 만나면 조각 +1 → 조각으로 그 종 Lv +1.
    // 레벨마다 성장 테이블 Growth_Order 순서대로 노드가 찍힘 (Progress.NodeAt) — 특수 액션 해금(Action_Unlock) · 필살기 해금(Ult_Unlock) 도 여기서.
    // 왼쪽 카드: 등급 필터 · 다 같이 훈련 · 만난 친구 카드(그림·이름·Lv·조각 막대·훈련 버튼)
    // 오른쪽 카드: 고른 친구 상세 (그림·등급·설명·Lv 막대·다음 성장 길·특수 능력·특수 액션·필살기·훈련 버튼)
    // 글은 전부 씬의 TMP 텍스트에 적혀 있고, 코드는 자리표시({lv} {have} {need} {n} {name})와 테이블 값(이름·설명)만 채움.
    public class TrainPage : MonoBehaviour
    {
        public LobbyManager manager;
        public RatArtLibrary artLibrary;

        [Header("왼쪽: 필터 · 목록")]
        [Tooltip("필터 칩 템플릿 (꺼져 있음): 자식 Label({name} {n}) · Badge(강화 가능 수, 자식 Text {n}) · Sel")] public Button filterTemplate;
        [Tooltip("'전체' 칩 이름 글 (씬 Words)")] public TMP_Text wAll;
        public Button upAllButton;
        [Tooltip("다 같이 훈련 ({n}) / 강화할 게 없을 때 글")] public TMP_Text upAllLabel, wUpAllNone;
        [Tooltip("카드 템플릿 (꺼져 있음): 자식 Stripe(등급 색) · Portrait(RatPortrait) · Name · Lv({lv} {have} {need}) · Fill(Image Filled) · Up(Button) · Sel · Can")] public Button cardTemplate;
        [Tooltip("만렙 카드 Lv 글 ({lv})")] public TMP_Text wCardMax;
        public GameObject emptyText;
        public ScrollRect scroll;

        [Header("오른쪽: 상세")]
        public GameObject detailRoot;
        public RatPortrait dPortrait;
        public TMP_Text dName, dGrade, dDesc;
        public Image dGradeBg;
        [Tooltip("Lv {lv}")] public TMP_Text dLv;
        [Tooltip("다음 레벨까지 조각 {have}/{need} · 만렙 글은 wMax")] public TMP_Text dNext, wMax;
        public Image dFill;
        [Tooltip("성장 길 칩 템플릿 (꺼져 있음): 자식 Lv({lv}) · Name")] public RectTransform pathTemplate;
        [Tooltip("성장 길에 보여 줄 다음 레벨 수")] public int pathCount = 6;
        public Color pathNormal = new(0.93f, 0.87f, 0.74f), pathKey = new(0.98f, 0.8f, 0.4f), pathDim = new(0.8f, 0.77f, 0.72f, 0.6f);
        [Header("특수 능력 · 특수 액션 · 필살기 카드")]
        [Tooltip("이름 글 ({name})")] public TMP_Text pName, aName, uName;
        public TMP_Text pDesc, aDesc, uDesc;
        [Tooltip("Lv {lv}에 해금 (해금 전에만 보임)")] public TMP_Text aLock, uLock;
        public CanvasGroup actionCard, ultCard;
        public Image pIcon, aIcon, uIcon;
        [Tooltip("필살기가 없는 종일 때 보이는 것")] public GameObject noUlt;
        [Tooltip("해금 전 카드 투명도")] public float lockedAlpha = 0.55f;
        public Button trainButton;
        [Tooltip("훈련 버튼 글 ({have} {need}) / 만렙 글")] public TMP_Text trainLabel, wTrainMax;
        [Header("스킬 트리 창")]
        public Button treeButton;
        public RatTreePopup treePopup;

        int filter = -1;        // -1 전체, 아니면 Grade
        string sel, sig;
        readonly List<GameObject> filterChips = new(), pathChips = new();
        readonly Dictionary<string, Button> cards = new();
        readonly Dictionary<TMP_Text, string> tpl = new();

        string T(TMP_Text t) { if (!t) return ""; if (!tpl.TryGetValue(t, out var s)) tpl[t] = s = t.text; return s; }
        string F(TMP_Text t, params (string k, object v)[] kv) { var s = T(t); foreach (var (k, v) in kv) s = s.Replace("{" + k + "}", v?.ToString()); return s; }
        static void SetT(TMP_Text t, string s) { if (t) t.text = s; }
        static TMP_Text Txt(Transform t, string path) => t.Find(path)?.GetComponent<TMP_Text>();

        void Awake()
        {
            foreach (var c in new Component[] { filterTemplate, cardTemplate, pathTemplate }) if (c) c.gameObject.SetActive(false);
            foreach (var t in new[] { upAllLabel, dLv, dNext, pName, aName, uName, aLock, uLock, trainLabel }) T(t);
            if (cardTemplate) { T(Txt(cardTemplate.transform, "Lv")); }
            if (pathTemplate) T(Txt(pathTemplate, "Lv"));
            if (filterTemplate) { T(Txt(filterTemplate.transform, "Label")); T(Txt(filterTemplate.transform, "Badge/Text")); }
            if (upAllButton) upAllButton.onClick.AddListener(UpAll);
            if (trainButton) trainButton.onClick.AddListener(() => Train(sel));
            if (treeButton) treeButton.onClick.AddListener(() => { if (treePopup && DB.RatsByCode.TryGetValue(sel ?? "", out var r)) treePopup.Open(r); });
        }

        static GameDatabase DB => GameDatabase.Instance;
        static bool Known(RatCharacterRow r) { var p = Progress.I; return p && p.Seen(r.code_id) && r.unlock_rank <= p.tier; }
        static bool Can(RatCharacterRow r) => Progress.I && Progress.I.CanUpgrade(r);
        static int MaxLv => Progress.I ? Progress.I.MaxLevel : 0;

        // 지금 레벨 · 다음 레벨까지 모은 조각 · 필요 조각 (만렙이면 need 0)
        static (int L, int have, int need) ShardLevel(RatCharacterRow r)
        {
            var p = Progress.I; var g = DB.GradeOf(r); int L = p.Level(r.code_id);
            if (L >= MaxLv) return (L, 0, 0);
            return (L, p.Shards(r.code_id) - Progress.ShardsFor(g, L), Progress.ShardNeed(g, L));
        }

        static Color GradeColor(RatCharacterRow r) { var g = DB.GradeOf(r); return g != null && ColorUtility.TryParseHtmlString(g.color, out var c) ? c : Color.white; }
        RatArtLibrary.Entry Art(RatCharacterRow r) => artLibrary ? artLibrary.Get(r.code_id) : null;

        // LobbyManager 가 페이지를 열 때 (SendMessage)
        public void Render()
        {
            var p = Progress.I; var db = DB; if (!p || !db) return;
            var seen = new List<RatCharacterRow>(); foreach (var r in db.Rats.Values) if (Known(r)) seen.Add(r);

            // 필터 칩: 전체 + 등급별 (만난 친구 수, 강화 가능 수 배지)
            foreach (var g in filterChips) Destroy(g);
            filterChips.Clear();
            for (int gi = -1; gi < 6; gi++)
            {
                int n = 0, up = 0;
                foreach (var r in seen) if (gi < 0 || (int)r.Grade == gi) { n++; if (Can(r)) up++; }
                if (gi >= 0 && n == 0) continue;
                db.Grades.TryGetValue((Grade)Mathf.Max(0, gi), out var gr);
                var b = Instantiate(filterTemplate, filterTemplate.transform.parent); b.gameObject.SetActive(true); filterChips.Add(b.gameObject);
                var lab = Txt(b.transform, "Label"); if (lab) lab.text = F(lab, ("name", gi < 0 ? T(wAll) : gr?.grade_name), ("n", n));
                var img = b.GetComponent<Image>(); if (img && gi >= 0 && gr != null && ColorUtility.TryParseHtmlString(gr.color, out var c)) img.color = Color.Lerp(Color.white, c, 0.45f);
                var badge = b.transform.Find("Badge"); if (badge) { badge.gameObject.SetActive(up > 0); var bt = Txt(badge, "Text"); if (bt) bt.text = F(bt, ("n", up)); }
                var s = b.transform.Find("Sel"); if (s) s.gameObject.SetActive(filter == gi);
                int ff = gi; b.onClick.AddListener(() => { filter = ff; Render(); });
            }

            // 목록: 강화 가능 먼저, 높은 등급 먼저
            var list = seen.FindAll(r => filter < 0 || (int)r.Grade == filter);
            list.Sort((a, b) => { int c = Can(b).CompareTo(Can(a)); if (c != 0) return c; c = ((int)b.Grade).CompareTo((int)a.Grade); return c != 0 ? c : a.character_id.CompareTo(b.character_id); });
            string s2 = string.Join(",", list.ConvertAll(r => r.code_id));
            if (s2 != sig)
            {
                sig = s2;
                foreach (var c in cards.Values) Destroy(c.gameObject);
                cards.Clear();
                foreach (var r in list)
                {
                    var b = Instantiate(cardTemplate, cardTemplate.transform.parent); b.gameObject.SetActive(true); cards[r.code_id] = b;
                    var stripe = b.transform.Find("Stripe")?.GetComponent<Image>(); if (stripe) stripe.color = GradeColor(r);
                    b.transform.Find("Portrait")?.GetComponent<RatPortrait>()?.Show(Art(r));
                    var nm = Txt(b.transform, "Name"); if (nm) nm.text = r.character_name;
                    string code = r.code_id;
                    b.onClick.AddListener(() => { sel = code; Render(); });
                    var up = b.transform.Find("Up")?.GetComponent<Button>(); if (up) up.onClick.AddListener(() => Train(code));
                }
                if (scroll) scroll.verticalNormalizedPosition = 1;
            }
            if (emptyText) emptyText.SetActive(list.Count == 0);
            foreach (var r in list)
            {
                var b = cards[r.code_id]; var (L, have, need) = ShardLevel(r); bool can = Can(r);
                var lv = Txt(b.transform, "Lv"); if (lv) lv.text = need > 0 ? F(lv, ("lv", L), ("have", Mathf.Max(0, have)), ("need", need)) : F(wCardMax, ("lv", L));
                var fill = b.transform.Find("Fill")?.GetComponent<Image>(); if (fill) fill.fillAmount = need > 0 ? Mathf.Clamp01((float)have / need) : 1;
                var up = b.transform.Find("Up")?.GetComponent<Button>(); if (up) up.interactable = can;
                var s = b.transform.Find("Sel"); if (s) s.gameObject.SetActive(r.code_id == sel);
                var cg = b.transform.Find("Can"); if (cg) cg.gameObject.SetActive(can);
            }

            int all = p.UpgradableCount;
            if (upAllButton) upAllButton.interactable = all > 0;
            SetT(upAllLabel, all > 0 ? F(upAllLabel, ("n", all)) : T(wUpAllNone));

            if (string.IsNullOrEmpty(sel) || !db.RatsByCode.TryGetValue(sel, out var cur) || !Known(cur)) { cur = list.Count > 0 ? list[0] : seen.Count > 0 ? seen[0] : null; sel = cur?.code_id; }
            Detail(cur);
        }

        void Detail(RatCharacterRow r)
        {
            if (detailRoot) detailRoot.SetActive(r != null);
            if (r == null) return;
            var p = Progress.I; var db = DB; var gr = db.GradeOf(r);
            var (L, have, need) = ShardLevel(r);
            if (dPortrait) dPortrait.Show(Art(r));
            SetT(dName, r.character_name);
            SetT(dGrade, gr?.grade_name);
            if (dGradeBg) dGradeBg.color = GradeColor(r);
            SetT(dDesc, r.character_desc);
            SetT(dLv, F(dLv, ("lv", L)));
            SetT(dNext, need > 0 ? F(dNext, ("have", Mathf.Max(0, have)), ("need", need)) : T(wMax));
            if (dFill) dFill.fillAmount = need > 0 ? Mathf.Clamp01((float)have / need) : 1;

            // 특수 능력 (처음부터 켜짐) · 특수 액션 (Action_Unlock 레벨) · 필살기 (Ult_Unlock 레벨)
            db.RatSkills.TryGetValue(r.passive_skill, out var ps); db.RatSkills.TryGetValue(r.action_skill, out var acs);
            SetT(pName, F(pName, ("name", ps?.skill_name))); SetT(pDesc, ps?.skill_explain);
            SetIcon(pIcon, Icons ? Icons.Skill(ps) : null); SetIcon(aIcon, Icons ? Icons.Skill(acs) : null);
            int actLv = p.UnlockLevel(GrowthEffectType.Action_Unlock), ultLv = p.UnlockLevel(GrowthEffectType.Ult_Unlock);
            bool actOn = L >= actLv;
            if (actionCard) { actionCard.gameObject.SetActive(acs != null); actionCard.alpha = actOn ? 1 : lockedAlpha; }
            SetT(aName, F(aName, ("name", acs?.skill_name))); SetT(aDesc, acs?.skill_explain);
            if (aLock) { aLock.gameObject.SetActive(!actOn); aLock.text = F(aLock, ("lv", actLv)); }
            var u = db.UltOf(r); bool ultOn = L >= ultLv;
            if (ultCard) { ultCard.gameObject.SetActive(u != null); ultCard.alpha = ultOn ? 1 : lockedAlpha; }
            if (noUlt) noUlt.SetActive(u == null);
            if (u != null)
            {
                SetT(uName, F(uName, ("name", u.ultimate_name))); SetT(uDesc, u.dev_desc);
                if (uLock) { uLock.gameObject.SetActive(!ultOn); uLock.text = F(uLock, ("lv", ultLv)); }
                SetIcon(uIcon, Icons ? Icons.Ult(u) : null);
            }

            // 성장 길: 다음 레벨들에 찍히는 노드 (해금 노드는 강조, 필살기 없는 종의 필살기 해금은 흐리게)
            foreach (var g in pathChips) Destroy(g);
            pathChips.Clear();
            if (pathTemplate)
                for (int lv = L + 1; lv <= Mathf.Min(MaxLv, L + pathCount); lv++)
                {
                    var n = p.NodeAt(lv); if (n == null) break;
                    var c = Instantiate(pathTemplate, pathTemplate.parent); c.gameObject.SetActive(true); pathChips.Add(c.gameObject);
                    var tl = Txt(c, "Lv"); if (tl) tl.text = F(tl, ("lv", lv));
                    var tn = Txt(c, "Name"); if (tn) tn.text = n.node_name;
                    SetIcon(c.Find("Icon")?.GetComponent<Image>(), Icons ? Icons.Node(n, r) : null);
                    var e = n.Effect;
                    bool key = e == GrowthEffectType.Ult_Unlock || e == GrowthEffectType.Action_Awaken || e == GrowthEffectType.Awaken || (e == GrowthEffectType.Action_Unlock && lv == actLv);
                    bool dim = (e == GrowthEffectType.Ult_Unlock && u == null) || (e == GrowthEffectType.Action_Unlock && acs == null);
                    var img = c.GetComponent<Image>(); if (img) img.color = dim ? pathDim : key ? pathKey : pathNormal;
                }

            bool can = Can(r);
            if (trainButton) trainButton.interactable = can;
            SetT(trainLabel, need > 0 ? F(trainLabel, ("have", Mathf.Max(0, have)), ("need", need)) : T(wTrainMax));
        }

        static IconBook Icons => IconBook.I;
        static void SetIcon(Image img, Sprite sp) { if (!img) return; img.sprite = sp; img.enabled = sp; }

        void Train(string code)
        {
            if (string.IsNullOrEmpty(code) || !DB.RatsByCode.TryGetValue(code, out var r)) return;
            if (!Progress.I.TryUpgrade(r)) { SfxManager.Play("ui_deny"); return; }
            SfxManager.Play("ui_buy");
            Progress.I.Save();
            sel = code;
            if (cards.TryGetValue(code, out var b)) Pop(b.transform);
            Render();
        }

        void UpAll()
        {
            if (Progress.I.UpgradeAll() > 0) Render();
        }

        // 훈련 성공 때 카드 통 튀기
        readonly List<(Transform t, float time)> pops = new();
        void Pop(Transform t) => pops.Add((t, 0));
        void Update()
        {
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var (t, time) = pops[i]; time += Time.unscaledDeltaTime;
                if (!t || time > 0.3f) { if (t) t.localScale = Vector3.one; pops.RemoveAt(i); continue; }
                t.localScale = Vector3.one * (1 + 0.12f * Mathf.Sin(time / 0.3f * Mathf.PI));
                pops[i] = (t, time);
            }
        }

    }
}
