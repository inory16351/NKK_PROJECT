using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 쳇바퀴 훈련 · 쥐별 스킬 트리 창 (웹 UI.openTree 종별 트리): 성장 노드(쥐 성장 테이블 Growth_Node pos_x·pos_y 칸)를 선행 노드와 끈으로 잇고,
    // 노드마다 아이콘(IconBook.Node — 특수 액션·특수 능력·필살기 노드는 그 쥐의 아이콘) · 지금 레벨/최대 · 다음에 오르는 훈련 Lv.
    // 노드를 누르면 오른쪽에 설명 (테이블 node_desc + 그 쥐의 스킬 이름·설명) · 이 노드가 오르는 훈련 Lv 목록.
    // 글은 씬 TMP (자리표시 {lv} {max} {next} {list} {name}), 코드는 값만 채움.
    public class RatTreePopup : MonoBehaviour
    {
        public static RatTreePopup Current { get; private set; }

        public GameObject root;
        public Button closeButton, dimButton;
        [Header("머리")]
        public RatPortrait portrait;
        public TMP_Text nameText;
        [Tooltip("Lv {lv}")] public TMP_Text lvText;

        [Header("트리")]
        public RectTransform nodeLayer, lineLayer;
        [Tooltip("노드 템플릿 (꺼져 있음): 자식 Icon(Image) · Name · Lv({lv}/{max}) · Next({next}) · Sel")] public Button nodeTemplate;
        [Tooltip("끈 템플릿 (꺼져 있음)")] public Image lineTemplate;
        [Tooltip("칸 간격 (픽셀)")] public Vector2 step = new(190, 165);
        public Color nodeFull = new(0.98f, 0.82f, 0.42f), nodeSome = new(0.93f, 0.87f, 0.74f), nodeNone = new(0.75f, 0.71f, 0.66f), nodeDead = new(0.75f, 0.71f, 0.66f, 0.45f);
        public Color lineOn = new(0.95f, 0.76f, 0.31f), lineOff = new(0.55f, 0.47f, 0.4f, 0.6f);
        [Tooltip("아직 안 찍힌 노드 아이콘 색")] public Color iconOff = new(1, 1, 1, 0.45f);

        [Header("오른쪽 설명")]
        public Image dIcon;
        public TMP_Text dName, dDesc, dSkill;
        [Tooltip("Lv {lv} / {max}")] public TMP_Text dLevel;
        [Tooltip("훈련 Lv {list} 에 올라요")] public TMP_Text dWhen;
        [Tooltip("다음 훈련 글: 다 찍었을 때 · 이 종은 해당 없음 (씬 Words)")] public TMP_Text wDone, wNoUlt, wNoAction;
        [Tooltip("노드 아래 다음 훈련 글 ({next})")] public TMP_Text wNodeNext;
        [Tooltip("스킬 줄 ({name} · 설명은 다음 줄)")] public TMP_Text wSkillLine;

        RatCharacterRow rat;
        int sel;
        readonly List<GameObject> spawned = new();
        readonly Dictionary<TMP_Text, string> tpl = new();
        string T(TMP_Text t) { if (!t) return ""; if (!tpl.TryGetValue(t, out var s)) tpl[t] = s = t.text; return s; }
        string F(TMP_Text t, params (string k, object v)[] kv) { var s = T(t); foreach (var (k, v) in kv) s = s.Replace("{" + k + "}", v?.ToString()); return s; }
        static TMP_Text Txt(Transform t, string p) => t.Find(p)?.GetComponent<TMP_Text>();

        void Awake()
        {
            if (nodeTemplate) { nodeTemplate.gameObject.SetActive(false); T(Txt(nodeTemplate.transform, "Lv")); }
            if (lineTemplate) lineTemplate.gameObject.SetActive(false);
            foreach (var t in new[] { lvText, dLevel, dWhen, wSkillLine, wNodeNext }) T(t);
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (dimButton) dimButton.onClick.AddListener(Close);

        }

        public bool IsOpen => root && root.activeSelf;

        public void Open(RatCharacterRow r)
        {
            rat = r; if (r == null) return;
            if (sel == 0 || GameDatabase.Instance.GrowthNodeById(sel) == null) sel = GameDatabase.Instance.GrowthNodes.Count > 0 ? GameDatabase.Instance.GrowthNodes[0].node_id : 0;
            if (root) root.SetActive(true);
            Current = this;
            Draw();
        }

        public void Close() { if (root) root.SetActive(false); if (Current == this) Current = null; }

        // 이 쥐에게 의미 없는 노드 (필살기 없는 종의 필살기 해금, 특수 액션 없는 종의 액션 노드)
        bool Dead(GrowthNodeRow n)
        {
            var db = GameDatabase.Instance;
            if (n.Effect == GrowthEffectType.Ult_Unlock) return db.UltOf(rat) == null;
            if (n.node_icon == "@action") return !db.RatSkills.ContainsKey(rat.action_skill);
            return false;
        }

        // 노드가 레벨을 얻는 훈련 Lv 목록 (최대 maxCount 개)
        static List<int> LevelsOf(GrowthNodeRow n)
        {
            var l = new List<int>(); var p = Progress.I;
            for (int L = 1; L <= p.MaxLevel && l.Count < n.max_level; L++) if (p.NodeAt(L)?.node_id == n.node_id) l.Add(L);
            return l;
        }

        void Draw()
        {
            foreach (var g in spawned) Destroy(g);
            spawned.Clear();
            var db = GameDatabase.Instance; var p = Progress.I;
            int L = p.Level(rat.code_id);
            var have = p.Tree(L);
            if (portrait) portrait.Show(LobbyArt(rat));
            if (nameText) nameText.text = rat.character_name;
            if (lvText) lvText.text = F(lvText, ("lv", L));

            // 트리 크기에 맞춰 가운데
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var n in db.GrowthNodes) { minX = Mathf.Min(minX, n.pos_x); maxX = Mathf.Max(maxX, n.pos_x); minY = Mathf.Min(minY, n.pos_y); maxY = Mathf.Max(maxY, n.pos_y); }
            Vector2 At(GrowthNodeRow n) => new((n.pos_x - (minX + maxX) * 0.5f) * step.x, -(n.pos_y - (minY + maxY) * 0.5f) * step.y);
            int LvOf(GrowthNodeRow n) => have.TryGetValue(n.Effect, out var v) ? v : 0;

            // 끈 (선행 노드 → 노드)
            foreach (var n in db.GrowthNodes)
            {
                var from = db.GrowthNodeById(n.req_node); if (from == null) from = n.Effect == GrowthEffectType.Ult_Unlock ? db.GrowthNodes[0] : null;
                if (from == null) continue;
                var line = Instantiate(lineTemplate, lineLayer); line.gameObject.SetActive(true); spawned.Add(line.gameObject);
                Vector2 a = At(from), b = At(n), d = b - a;
                var rt = line.rectTransform; rt.anchoredPosition = (a + b) * 0.5f; rt.sizeDelta = new Vector2(d.magnitude, rt.sizeDelta.y);
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                line.color = LvOf(n) > 0 ? lineOn : lineOff;
            }

            // 노드
            foreach (var n in db.GrowthNodes)
            {
                var b = Instantiate(nodeTemplate, nodeLayer); b.gameObject.SetActive(true); spawned.Add(b.gameObject);
                ((RectTransform)b.transform).anchoredPosition = At(n);
                int lv = LvOf(n); bool dead = Dead(n);
                var img = b.GetComponent<Image>(); if (img) img.color = dead ? nodeDead : lv >= n.max_level ? nodeFull : lv > 0 ? nodeSome : nodeNone;
                var icon = b.transform.Find("Icon")?.GetComponent<Image>();
                if (icon) { icon.sprite = IconBook.I ? IconBook.I.Node(n, rat) : null; icon.enabled = icon.sprite; icon.color = lv > 0 ? Color.white : iconOff; }
                var nm = Txt(b.transform, "Name"); if (nm) nm.text = n.node_name;
                var tl = Txt(b.transform, "Lv"); if (tl) tl.text = F(tl, ("lv", lv), ("max", n.max_level));
                var nx = Txt(b.transform, "Next");
                if (nx)
                {
                    int next = 0; foreach (var x in LevelsOf(n)) if (x > L) { next = x; break; }
                    nx.text = !dead && next > 0 ? F(wNodeNext, ("next", next)) : "";
                }
                var s = b.transform.Find("Sel"); if (s) s.gameObject.SetActive(n.node_id == sel);
                int id = n.node_id; b.onClick.AddListener(() => { sel = id; Draw(); });
            }

            // 오른쪽 설명
            var cur = db.GrowthNodeById(sel); if (cur == null) return;
            int clv = LvOf(cur);
            if (dIcon) { dIcon.sprite = IconBook.I ? IconBook.I.Node(cur, rat) : null; dIcon.enabled = dIcon.sprite; }
            if (dName) dName.text = cur.node_name;
            if (dLevel) dLevel.text = F(dLevel, ("lv", clv), ("max", cur.max_level));
            if (dDesc) dDesc.text = Desc(cur);
            if (dSkill)
            {
                RatSkillRow sk = null; string extra = null;
                if (cur.node_icon == "@action") db.RatSkills.TryGetValue(rat.action_skill, out sk);
                else if (cur.node_icon == "@passive") db.RatSkills.TryGetValue(rat.passive_skill, out sk);
                else if (cur.node_icon == "@ult") { var u = db.UltOf(rat); extra = u != null ? F(wSkillLine, ("name", u.ultimate_name)) + "\n" + u.dev_desc : T(wNoUlt); }
                if (sk != null) extra = F(wSkillLine, ("name", sk.skill_name)) + "\n" + sk.skill_explain;
                else if (cur.node_icon == "@action" && extra == null) extra = T(wNoAction);
                dSkill.gameObject.SetActive(extra != null); dSkill.text = extra ?? "";
            }
            if (dWhen)
            {
                var ls = LevelsOf(cur);
                dWhen.text = Dead(cur) ? "" : ls.Count == 0 ? T(wDone) : F(dWhen, ("list", string.Join(", ", ls)));
            }
        }

        // 테이블 설명: {v1}~{v3} = 값, {p1}~{p3} = 값 × 100
        static string Desc(GrowthNodeRow n)
        {
            string s = n.node_desc ?? "";
            float[] v = { n.value_01, n.value_02, n.value_03 };
            for (int i = 0; i < 3; i++) s = s.Replace("{v" + (i + 1) + "}", v[i].ToString("0.##")).Replace("{p" + (i + 1) + "}", (v[i] * 100).ToString("0.#"));
            return s;
        }

        Rats.RatArtLibrary.Entry LobbyArt(RatCharacterRow r) { var tp = GetComponentInParent<TrainPage>(); return tp && tp.artLibrary ? tp.artLibrary.Get(r.code_id) : null; }
    }
}
