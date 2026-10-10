using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 탈출 준비실 · 친구들!! (도감): 쥐 캐릭터 테이블의 모든 친구.
    // 만난 친구 = 그림 · 이름 · 등급 · 설명 · 특수 능력 · 특수 액션 · 필살기 / 아직 못 만난 친구 = 실루엣 · '???' /
    // 훈장이 모자란 친구 = 실루엣 + '{tier}훈장부터' (쥐 unlock_rank). 화면 구성은 쳇바퀴 훈련 페이지와 같음.
    // 글은 전부 씬 TMP (Words 아래 포함), 코드는 자리표시({n} {total} {name} {tier} {lv})와 테이블 값만 채움.
    public class DexPage : MonoBehaviour
    {
        public LobbyManager manager;
        public RatArtLibrary artLibrary;

        [Header("왼쪽: 제목 · 필터 · 목록")]
        [Tooltip("제목 ({n} 만난 수 · {total} 전체)")] public TMP_Text title;
        [Tooltip("필터 칩 템플릿 (꺼져 있음): 자식 Label({name} {n}) · Sel")] public Button filterTemplate;
        [Tooltip("'전체' 칩 이름")] public TMP_Text wAll;
        [Tooltip("카드 템플릿 (꺼져 있음): 자식 Sel · Stripe · Portrait(RatPortrait) · Name · Sub")] public Button cardTemplate;
        public ScrollRect scroll;

        [Header("못 만난 친구")]
        [Tooltip("이름 대신 ('???')")] public TMP_Text wUnknownName;
        [Tooltip("카드 아래 글: 만난 친구 ({grade}) · 못 만남 · 훈장 부족 ({tier})")] public TMP_Text wSubKnown, wSubUnknown, wSubTier;
        [Tooltip("상세 설명: 못 만남 · 훈장 부족 ({tier})")] public TMP_Text wDescUnknown, wDescTier;
        [Tooltip("실루엣 색")] public Color silhouette = new(0.27f, 0.22f, 0.19f);

        [Header("오른쪽: 상세")]
        public GameObject detailRoot;
        public RatPortrait dPortrait;
        public TMP_Text dName, dGrade, dDesc;
        public Image dGradeBg;
        [Tooltip("쳇바퀴 훈련 레벨 ({lv}) — 만난 친구만")] public TMP_Text dLv;
        [Tooltip("특수 능력 · 특수 액션 · 필살기 카드 (만난 친구만)")] public GameObject passiveCard, actionCard, ultCard;
        [Tooltip("이름 글 ({name})")] public TMP_Text pName, aName, uName;
        public TMP_Text pDesc, aDesc, uDesc;
        public Image pIcon, aIcon, uIcon;
        [Tooltip("필살기가 없는 친구일 때")] public GameObject noUlt;

        int filter = -1;        // -1 전체, 아니면 Grade
        string sel, sig;
        readonly List<GameObject> chips = new();
        readonly Dictionary<string, Button> cards = new();
        readonly Dictionary<TMP_Text, string> tpl = new();

        string T(TMP_Text t) { if (!t) return ""; if (!tpl.TryGetValue(t, out var s)) tpl[t] = s = t.text; return s; }
        string F(TMP_Text t, params (string k, object v)[] kv) { var s = T(t); foreach (var (k, v) in kv) s = s.Replace("{" + k + "}", v?.ToString()); return s; }
        static void SetT(TMP_Text t, string s) { if (t) t.text = s; }
        static TMP_Text Txt(Transform t, string path) => t.Find(path)?.GetComponent<TMP_Text>();
        static GameDatabase DB => GameDatabase.Instance;
        static IconBook Icons => IconBook.I;
        static void SetIcon(Image img, Sprite sp) { if (!img) return; img.sprite = sp; img.enabled = sp; }

        // 0 = 만남 · 1 = 아직 못 만남 · 2 = 훈장 부족
        static int State(RatCharacterRow r)
        {
            var p = Progress.I; if (!p) return 1;
            if (r.unlock_rank > p.tier) return 2;
            return p.Seen(r.code_id) ? 0 : 1;
        }
        static Color GradeColor(RatCharacterRow r) { var g = DB.GradeOf(r); return g != null && ColorUtility.TryParseHtmlString(g.color, out var c) ? c : Color.white; }
        RatArtLibrary.Entry Art(RatCharacterRow r) => artLibrary ? artLibrary.Get(r.code_id) : null;

        void Awake()
        {
            foreach (var c in new Component[] { filterTemplate, cardTemplate }) if (c) c.gameObject.SetActive(false);
            foreach (var t in new[] { title, dLv, pName, aName, uName, wSubKnown, wSubTier, wDescTier }) T(t);
            if (filterTemplate) T(Txt(filterTemplate.transform, "Label"));
        }

        // LobbyManager 가 페이지를 열 때 (SendMessage)
        public void Render()
        {
            var p = Progress.I; var db = DB; if (!p || !db) return;
            var all = new List<RatCharacterRow>(db.Rats.Values);
            all.Sort((a, b) => { int c = ((int)a.Grade).CompareTo((int)b.Grade); return c != 0 ? c : a.character_id.CompareTo(b.character_id); });
            int met = 0; foreach (var r in all) if (State(r) == 0) met++;
            SetT(title, F(title, ("n", met), ("total", all.Count)));

            // 필터 칩: 전체 + 등급별 (만난 수 / 전체)
            foreach (var g in chips) Destroy(g);
            chips.Clear();
            if (filterTemplate)
                for (int gi = -1; gi < 6; gi++)
                {
                    int n = 0, tot = 0;
                    foreach (var r in all) if (gi < 0 || (int)r.Grade == gi) { tot++; if (State(r) == 0) n++; }
                    if (tot == 0) continue;
                    db.Grades.TryGetValue((Grade)Mathf.Max(0, gi), out var gr);
                    var b = Instantiate(filterTemplate, filterTemplate.transform.parent); b.gameObject.SetActive(true); chips.Add(b.gameObject);
                    var lab = Txt(b.transform, "Label"); if (lab) lab.text = F(lab, ("name", gi < 0 ? T(wAll) : gr?.grade_name), ("n", n), ("total", tot));
                    var img = b.GetComponent<Image>(); if (img && gi >= 0 && gr != null && ColorUtility.TryParseHtmlString(gr.color, out var c)) img.color = Color.Lerp(Color.white, c, 0.45f);
                    var s = b.transform.Find("Sel"); if (s) s.gameObject.SetActive(filter == gi);
                    int ff = gi; b.onClick.AddListener(() => { filter = ff; Render(); });
                }

            // 목록 (등급 순 · id 순). 상태가 바뀌었으면 카드를 다시 만듦
            var list = all.FindAll(r => filter < 0 || (int)r.Grade == filter);
            string s2 = string.Join(",", list.ConvertAll(r => r.code_id + State(r)));
            if (s2 != sig)
            {
                sig = s2;
                foreach (var c in cards.Values) Destroy(c.gameObject);
                cards.Clear();
                foreach (var r in list)
                {
                    var b = Instantiate(cardTemplate, cardTemplate.transform.parent); b.gameObject.SetActive(true); cards[r.code_id] = b;
                    int st = State(r); var gr = db.GradeOf(r);
                    var stripe = b.transform.Find("Stripe")?.GetComponent<Image>(); if (stripe) stripe.color = GradeColor(r);
                    b.transform.Find("Portrait")?.GetComponent<RatPortrait>()?.Show(Art(r), st == 0 ? Color.white : silhouette);
                    SetT(Txt(b.transform, "Name"), st == 0 ? r.character_name : T(wUnknownName));
                    SetT(Txt(b.transform, "Sub"), st == 0 ? F(wSubKnown, ("grade", gr?.grade_name)) : st == 2 ? F(wSubTier, ("tier", r.unlock_rank)) : T(wSubUnknown));
                    string code = r.code_id;
                    b.onClick.AddListener(() => { sel = code; Render(); });
                }
                if (scroll) scroll.verticalNormalizedPosition = 1;
            }
            foreach (var r in list) { var s = cards[r.code_id].transform.Find("Sel"); if (s) s.gameObject.SetActive(r.code_id == sel); }

            if (string.IsNullOrEmpty(sel) || !db.RatsByCode.TryGetValue(sel, out var cur) || !list.Contains(cur))
            {
                cur = list.Find(r => State(r) == 0) ?? (list.Count > 0 ? list[0] : null);
                sel = cur?.code_id;
                if (cur != null && cards.TryGetValue(sel, out var cb)) { var s = cb.transform.Find("Sel"); if (s) s.gameObject.SetActive(true); }
            }
            Detail(cur);
        }

        void Detail(RatCharacterRow r)
        {
            if (detailRoot) detailRoot.SetActive(r != null);
            if (r == null) return;
            var p = Progress.I; var db = DB; var gr = db.GradeOf(r);
            int st = State(r); bool known = st == 0;
            if (dPortrait) dPortrait.Show(Art(r), known ? Color.white : silhouette);
            SetT(dName, known ? r.character_name : T(wUnknownName));
            SetT(dGrade, gr?.grade_name);
            if (dGradeBg) dGradeBg.color = GradeColor(r);
            SetT(dDesc, known ? r.character_desc : st == 2 ? F(wDescTier, ("tier", r.unlock_rank)) : T(wDescUnknown));
            if (dLv) { dLv.gameObject.SetActive(known); dLv.text = F(dLv, ("lv", p.Level(r.code_id))); }

            db.RatSkills.TryGetValue(r.passive_skill, out var ps); db.RatSkills.TryGetValue(r.action_skill, out var acs);
            var u = db.UltOf(r);
            if (passiveCard) passiveCard.SetActive(known && ps != null);
            if (actionCard) actionCard.SetActive(known && acs != null);
            if (ultCard) ultCard.SetActive(known && u != null);
            if (noUlt) noUlt.SetActive(known && u == null);
            if (!known) return;
            SetT(pName, F(pName, ("name", ps?.skill_name))); SetT(pDesc, ps?.skill_explain);
            SetT(aName, F(aName, ("name", acs?.skill_name))); SetT(aDesc, acs?.skill_explain);
            SetIcon(pIcon, Icons ? Icons.Skill(ps) : null); SetIcon(aIcon, Icons ? Icons.Skill(acs) : null);
            if (u != null) { SetT(uName, F(uName, ("name", u.ultimate_name))); SetT(uDesc, u.dev_desc); SetIcon(uIcon, Icons ? Icons.Ult(u) : null); }
        }
    }
}
