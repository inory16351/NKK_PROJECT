using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 탈출 준비실 · 치즈 창고 = 공용 스킬 지도 (웹 buildMap / renderSkillDetail).
    // 지도: 노드는 공용 스킬 테이블 pos_x·pos_y 칸에, 선행 스킬끼리 끈으로 연결. 끌어서 이동 · 휠로 확대.
    // 노드 상태 (Progress.StateOf): 찍음 · 열림 · ?(힌트) · 훈장 부족(자물쇠) · 숨김. 선택된 노드를 한 번 더 누르면 강화.
    // 글은 전부 씬의 TMP 텍스트에 적혀 있고, 코드는 자리표시({lv} {max} {cost} {tier} {req} {need} {branch})와 테이블 값(이름·설명)만 채움.
    public class SkillPage : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        public LobbyManager manager;

        [Header("지도")]
        [Tooltip("지도 보이는 영역 (RectMask2D)")] public RectTransform viewport;
        [Tooltip("노드·끈이 들어가는 판 (이동·확대됨)")] public RectTransform content;
        public RectTransform lineLayer, nodeLayer;
        [Tooltip("칸 간격 (픽셀)")] public float step = 170;
        public Vector2 zoomRange = new(0.5f, 1.5f);
        [Tooltip("휠 한 칸 확대 배율")] public float zoomStep = 1.12f;

        [Header("템플릿 (꺼져 있음)")]
        [Tooltip("노드: 자식 Icon(Image) · Lv · Name · Cost(TMP) · Q(TMP '?') · Lock(Image) · LockText(TMP) · Sel(선택 표시)")] public Button nodeTemplate;
        [Tooltip("핵심 노드 (마름모), 자식 구성 같음")] public Button keyTemplate;
        [Tooltip("끈 (Image, 가로로 늘림)")] public Image lineTemplate;
        public Sprite nodeOff, nodeOn, nodeLock, keyOff, keyOn;
        public Color lineOn = new(0.95f, 0.76f, 0.31f), lineOpen = new(0.85f, 0.75f, 0.6f), lineOff = new(0.55f, 0.47f, 0.4f, 0.6f);

        [Header("상세 카드")]
        public Image detailIcon;
        public TMP_Text detailName, detailLevel, detailExplain, detailReq, detailCost;
        public Image branchRibbon;
        public TMP_Text branchText;
        public Button buyButton;
        public TMP_Text buyLabel;
        [Tooltip("최대 레벨일 때 강화 버튼 글 (자식 텍스트에 적힘)")] public TMP_Text maxWord;
        [Tooltip("? 노드 이름 대신 쓰는 글")] public TMP_Text hintWord;
        [Tooltip("최대 레벨이 없을 때 쓰는 글")] public TMP_Text infWord;
        [Tooltip("훈장 부족 / 선행 부족 안내 글 (자리표시 {tier} · {req} {need})")] public TMP_Text tierReqTemplate, skillReqTemplate;

        string sel = "core";
        float zoom = 1; bool dragging;
        readonly List<GameObject> spawned = new();
        readonly Dictionary<TMP_Text, string> tpl = new();
        readonly Dictionary<string, Sprite> icons = new();

        string T(TMP_Text t) { if (!t) return ""; if (!tpl.TryGetValue(t, out var s)) tpl[t] = s = t.text; return s; }
        void SetT(TMP_Text t, params (string k, object v)[] kv) { if (!t) return; var s = T(t); foreach (var (k, v) in kv) s = s.Replace("{" + k + "}", v.ToString()); t.text = s; }

        void Awake()
        {
            foreach (var t in new Component[] { nodeTemplate, keyTemplate, lineTemplate }) if (t) t.gameObject.SetActive(false);
            if (buyButton) buyButton.onClick.AddListener(Buy);
            foreach (var t in new[] { buyLabel, detailLevel, detailCost, branchText }) T(t);
        }

        Sprite Icon(CommonSkillRow s) => icons.TryGetValue(s.code_id, out var sp) ? sp : null;

        [Tooltip("스킬 아이콘 (코드 id 순서 상관없음, 파일 이름 cs_<코드 id>)")] public Sprite[] iconSprites;
        void CacheIcons() { if (iconSprites == null) return; foreach (var s in iconSprites) if (s && s.name.StartsWith("cs_")) icons[s.name.Substring(3)] = s; }

        Color BranchColor(CommonSkillRow s)
        {
            var db = GameDatabase.Instance;
            return db.SkillBranches.TryGetValue(s.Branch, out var b) && ColorUtility.TryParseHtmlString(b.color, out var c) ? c : Color.white;
        }

        Vector2 At(CommonSkillRow s) => new(s.pos_x * step, -s.pos_y * step);

        // LobbyManager 가 페이지를 열 때 (SendMessage)
        public void Render()
        {
            CacheIcons();
            foreach (var g in spawned) Destroy(g);
            spawned.Clear();
            var p = Progress.I; var db = GameDatabase.Instance; int tier = p ? p.tier : 1;
            // 끈: 선행 → 이 스킬 (둘 다 숨김이 아닐 때)
            foreach (var s in db.CommonSkills)
            {
                if (s.req_skill == 0 || !db.CommonSkillsById.TryGetValue(s.req_skill, out var r)) continue;
                var st = p.StateOf(s, tier); var rs = p.StateOf(r, tier);
                if (st == Progress.SkillState.Hidden || rs == Progress.SkillState.Hidden) continue;
                var l = Instantiate(lineTemplate, lineLayer); l.gameObject.SetActive(true); spawned.Add(l.gameObject);
                Vector2 a = At(r), b = At(s), d = b - a;
                var rt = l.rectTransform; rt.anchoredPosition = (a + b) / 2; rt.sizeDelta = new Vector2(d.magnitude, rt.sizeDelta.y);
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                l.color = st == Progress.SkillState.Owned ? lineOn : st == Progress.SkillState.Open ? lineOpen : lineOff;
            }
            // 노드
            foreach (var s in db.CommonSkills)
            {
                var st = p.StateOf(s, tier);
                if (st == Progress.SkillState.Hidden) continue;
                var b = Instantiate(s.IsKey ? keyTemplate : nodeTemplate, nodeLayer); b.gameObject.SetActive(true); spawned.Add(b.gameObject);
                ((RectTransform)b.transform).anchoredPosition = At(s);
                int lv = p.SkillLv(s);
                var img = b.GetComponent<Image>();
                img.sprite = st == Progress.SkillState.TierLock ? nodeLock : st == Progress.SkillState.Owned ? (s.IsKey ? keyOn : nodeOn) : (s.IsKey ? keyOff : nodeOff);
                img.color = st == Progress.SkillState.Hint ? new Color(1, 1, 1, 0.75f) : Color.white;
                bool show = st == Progress.SkillState.Owned || st == Progress.SkillState.Open;
                Child(b, "Icon", show, c => { var i = c.GetComponent<Image>(); i.sprite = Icon(s); i.color = st == Progress.SkillState.Open && lv == 0 ? new Color(1, 1, 1, 0.85f) : Color.white; });
                Child(b, "Q", st == Progress.SkillState.Hint, null);
                Child(b, "Lock", st == Progress.SkillState.TierLock, null);
                Child(b, "LockText", st == Progress.SkillState.TierLock, c => SetT(c.GetComponent<TMP_Text>(), ("tier", s.unlock_tier)));
                Child(b, "Name", show, c => c.GetComponent<TMP_Text>().text = s.skill_name);
                Child(b, "Lv", show, c => SetT(c.GetComponent<TMP_Text>(), ("lv", lv), ("max", s.Infinite ? (infWord ? infWord.text : "-") : s.max_level.ToString())));
                Child(b, "Cost", show && !p.IsMax(s), c =>
                {
                    var t = c.GetComponent<TMP_Text>(); SetT(t, ("cost", LobbyManager.Fmt(p.SkillCost(s))));
                    t.color = p.CanBuySkill(s) ? new Color(0.29f, 0.22f, 0.17f) : new Color(0.62f, 0.5f, 0.42f);
                });
                Child(b, "Sel", s.code_id == sel, null);
                var code = s.code_id;
                b.onClick.AddListener(() => { if (dragging) return; if (sel == code && st != Progress.SkillState.Hint) Buy(); else { sel = code; Render(); } });
            }
            RenderDetail();
        }

        static void Child(Button b, string name, bool on, System.Action<Transform> set)
        {
            var c = b.transform.Find(name); if (!c) return;
            c.gameObject.SetActive(on); if (on) set?.Invoke(c);
        }

        void RenderDetail()
        {
            var p = Progress.I; var db = GameDatabase.Instance; int tier = p ? p.tier : 1;
            if (!db.CommonSkillsByCode.TryGetValue(sel, out var s)) return;
            var st = p.StateOf(s, tier); int lv = p.SkillLv(s);
            bool hint = st == Progress.SkillState.Hint;
            if (detailIcon) { detailIcon.sprite = Icon(s); detailIcon.color = hint ? new Color(0.3f, 0.25f, 0.2f, 0.6f) : Color.white; }
            if (detailName) detailName.text = hint && hintWord ? hintWord.text : s.skill_name;
            SetT(detailLevel, ("lv", lv), ("max", s.Infinite ? (infWord ? infWord.text : "-") : s.max_level.ToString()));
            if (detailExplain) detailExplain.text = hint ? "" : s.skill_explain;
            if (db.SkillBranches.TryGetValue(s.Branch, out var br)) { SetT(branchText, ("branch", br.branch_name)); if (branchRibbon) branchRibbon.color = BranchColor(s); }
            // 필요 조건 안내
            string req = "";
            if (tier < s.unlock_tier && tierReqTemplate) req = T(tierReqTemplate).Replace("{tier}", s.unlock_tier.ToString());
            else if (s.req_skill != 0 && db.CommonSkillsById.TryGetValue(s.req_skill, out var r) && p.SkillLv(r) < s.req_level && skillReqTemplate)
                req = T(skillReqTemplate).Replace("{req}", r.skill_name).Replace("{need}", s.req_level.ToString());
            if (detailReq) { detailReq.text = req; detailReq.gameObject.SetActive(req != ""); }
            bool max = p.IsMax(s);
            SetT(detailCost, ("cost", max ? "-" : LobbyManager.Fmt(p.SkillCost(s))));
            if (buyButton) buyButton.interactable = p.CanBuySkill(s);
            if (buyLabel) { if (max && maxWord) buyLabel.text = maxWord.text; else SetT(buyLabel, ("cost", LobbyManager.Fmt(p.SkillCost(s)))); }
        }

        void Buy()
        {
            var p = Progress.I; var db = GameDatabase.Instance;
            if (!db.CommonSkillsByCode.TryGetValue(sel, out var s) || !p.BuySkill(s)) return;
            Render();
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
