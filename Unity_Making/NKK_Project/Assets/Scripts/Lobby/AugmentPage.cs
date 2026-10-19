using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 증강 연구소 페이지 (지하 건물, Page_aug, 2026-10-19). 로직 = Core/Augments.
    //  · 보관함: 얻은 증강 전부 (하나하나 강화 Lv · 포인트) → 고르면 자세히 + 강화(포인트 + 치즈) · 들고 가기(41층 해금)
    //  · 합성: 같은 등급 N개 → 무작위 1개 (상승/유지/하락)
    //  · 도감: 증강 전부, 못 얻은 건 ???
    // 글은 전부 씬 TMP (자리표시만 채움)
    public class AugmentPage : MonoBehaviour
    {
        public LobbyManager manager;

        [Header("보기 바꾸기")]
        public Button storageTab, dexTab;
        public GameObject storageView, dexView;

        [Header("보관함")]
        [Tooltip("카드가 들어갈 곳 (스크롤 Content)")] public Transform storageParent;
        [Tooltip("카드 템플릿 (꺼 둠)")] public AugmentCard cardTemplate;
        [Tooltip("보관함이 비었을 때 글")] public GameObject emptyNote;
        [Tooltip("보관 수 (자리: {n})")] public TMP_Text countText;

        [Header("자세히")]
        public GameObject detailRoot;
        public AugmentCard detail;
        [Tooltip("강화 (글 자리: {pts} {need} {cheese})")] public Button upgradeButton;
        public TMP_Text upgradeLabel;
        [Tooltip("최대 레벨일 때 켜짐")] public GameObject maxNote;
        [Tooltip("들고 가기 버튼 (41층 해금 전엔 숨김)")] public Button carryButton;
        public TMP_Text carryLabel;
        public string carryOnLabel = "들고 가는 중", carryOffLabel = "들고 가기";
        public int carryOnLabelKey, carryOffLabelKey;
        [Tooltip("들고 가기 잠김 안내 (자리: {floor})")] public TMP_Text carryLockNote;

        [Header("합성")]
        public Button synthToggle;
        public GameObject synthOnMark;
        [Tooltip("합성하기 (글 자리: {n} {need})")] public Button synthButton;
        public TMP_Text synthLabel;
        [Tooltip("확률 안내 (자리: {up} {same} {down})")] public TMP_Text synthOdds;
        [Tooltip("합성 결과 알림 (자리: {name} {grade})")] public TMP_Text synthResult;

        [Header("도감")]
        public Transform dexParent;
        [Tooltip("도감 수 (자리: {n} {total})")] public TMP_Text dexCount;

        readonly List<AugmentCard> cards = new(), dexCards = new();
        readonly List<int> synthSel = new();
        int selUid, builtVer = -1; bool synthMode, dexMode; float resultT;
        string upTpl, synthTpl, oddsTpl, resultTpl, countTpl, dexTpl, lockTpl; bool init;

        void Awake()
        {
            if (cardTemplate) cardTemplate.gameObject.SetActive(false);
            if (synthResult) synthResult.gameObject.SetActive(false);
            if (storageTab) storageTab.onClick.AddListener(() => { dexMode = false; Render(); });
            if (dexTab) dexTab.onClick.AddListener(() => { dexMode = true; synthMode = false; Render(); });
            if (upgradeButton) upgradeButton.onClick.AddListener(OnUpgrade);
            if (carryButton) carryButton.onClick.AddListener(OnCarry);
            if (synthToggle) synthToggle.onClick.AddListener(() => { synthMode = !synthMode; synthSel.Clear(); Render(); });
            if (synthButton) synthButton.onClick.AddListener(OnSynth);
        }
        void Init()
        {
            if (init) return; init = true;
            upTpl = upgradeLabel ? LocText.Tpl(upgradeLabel, upgradeLabel.text) : "";
            synthTpl = synthLabel ? LocText.Tpl(synthLabel, synthLabel.text) : "";
            oddsTpl = synthOdds ? LocText.Tpl(synthOdds, synthOdds.text) : "";
            resultTpl = synthResult ? LocText.Tpl(synthResult, synthResult.text) : "";
            countTpl = countText ? LocText.Tpl(countText, countText.text) : "";
            dexTpl = dexCount ? LocText.Tpl(dexCount, dexCount.text) : "";
            lockTpl = carryLockNote ? LocText.Tpl(carryLockNote, carryLockNote.text) : "";
        }

        void OnEnable() => Render();
        void Update()
        {
            if (resultT > 0 && (resultT -= Time.unscaledDeltaTime) <= 0 && synthResult) synthResult.gameObject.SetActive(false);
            if (builtVer != Augments.Version) Render();
        }

        public void Render()
        {
            var db = Data.GameDatabase.Instance; if (!Progress.I || !db) return;
            Init();
            builtVer = Augments.Version;
            if (storageView) storageView.SetActive(!dexMode);
            if (dexView) dexView.SetActive(dexMode);
            if (dexMode) { RenderDex(); return; }

            var st = new List<Progress.AugItem>();
            foreach (var it in Augments.Items) if (Augments.RowOf(it) != null) st.Add(it);
            st.Sort((a, b) => { int g = Augments.RowOf(b).grade.CompareTo(Augments.RowOf(a).grade); if (g != 0) return g; int i = a.id.CompareTo(b.id); return i != 0 ? i : b.lv.CompareTo(a.lv); });
            Fill(cards, storageParent, st.Count);
            for (int i = 0; i < st.Count; i++)
            {
                var c = cards[i]; var it = st[i];
                c.BindItem(it); c.SetNew(it.isNew); c.SetEquipped(it.uid == Augments.Carry);
                c.SetSelected(synthMode ? synthSel.Contains(it.uid) : it.uid == selUid);
                if (c.button) { c.button.onClick.RemoveAllListeners(); int uid = it.uid; c.button.onClick.AddListener(() => OnCard(uid)); }
            }
            if (emptyNote) emptyNote.SetActive(st.Count == 0);
            if (countText) countText.text = countTpl.Replace("{n}", st.Count.ToString());

            var sel = Augments.Item(selUid);
            if (detailRoot) detailRoot.SetActive(sel != null && !synthMode);
            if (sel != null && !synthMode)
            {
                if (detail) { detail.BindItem(sel); detail.SetEquipped(sel.uid == Augments.Carry); }
                bool max = sel.lv >= Augments.MaxLevel;
                if (maxNote) maxNote.SetActive(max);
                if (upgradeButton)
                {
                    upgradeButton.gameObject.SetActive(!max);
                    upgradeButton.interactable = Augments.CanUpgrade(sel);
                    if (upgradeLabel) upgradeLabel.text = upTpl.Replace("{pts}", sel.pts.ToString()).Replace("{need}", Augments.UpPoints(sel.lv).ToString()).Replace("{cheese}", LobbyManager.Fmt(Augments.UpCheese(sel.lv)));
                }
                bool carryOpen = Augments.CarryUnlocked;
                if (carryButton) carryButton.gameObject.SetActive(carryOpen);
                if (carryLabel) carryLabel.text = sel.uid == Augments.Carry ? Loc.Get(carryOnLabelKey, carryOnLabel) : Loc.Get(carryOffLabelKey, carryOffLabel);
                if (carryLockNote) { carryLockNote.gameObject.SetActive(!carryOpen); carryLockNote.text = lockTpl.Replace("{floor}", Augments.CarryFloor.ToString()); }
            }
            if (synthOnMark) synthOnMark.SetActive(synthMode);
            var r0 = synthSel.Count > 0 ? Augments.RowOf(Augments.Item(synthSel[0])) : null;
            int need = r0 != null ? Augments.SynthNeed(r0.grade) : 0;
            if (synthButton)
            {
                synthButton.gameObject.SetActive(synthMode);
                synthButton.interactable = Augments.CanSynth(synthSel);
                if (synthLabel) synthLabel.text = synthTpl.Replace("{n}", synthSel.Count.ToString()).Replace("{need}", need > 0 ? need.ToString() : "-");
            }
            if (synthOdds)
            {
                var g = r0 != null ? Augments.Grade(r0.grade) : null;
                synthOdds.gameObject.SetActive(synthMode && g != null);
                if (g != null)
                {
                    bool top = Augments.Grade(r0.grade + 1) == null, bottom = Augments.Grade(r0.grade - 1) == null;
                    float up = top ? 0 : g.synth_up, down = bottom ? 0 : g.synth_down, same = g.synth_same + (top ? g.synth_up : 0) + (bottom ? g.synth_down : 0);
                    float sum = Mathf.Max(0.0001f, up + same + down);
                    synthOdds.text = oddsTpl.Replace("{up}", Pct(up / sum)).Replace("{same}", Pct(same / sum)).Replace("{down}", Pct(down / sum));
                }
            }
        }
        static string Pct(float v) => Mathf.RoundToInt(v * 100).ToString();

        void RenderDex()
        {
            var db = Data.GameDatabase.Instance;
            var all = new List<Data.AugmentRow>();
            foreach (var a in db.Augments) if (a.enabled != 0) all.Add(a);
            all.Sort((a, b) => a.grade != b.grade ? a.grade.CompareTo(b.grade) : a.augment_id.CompareTo(b.augment_id));
            Fill(dexCards, dexParent, all.Count);
            int seen = 0;
            for (int i = 0; i < all.Count; i++)
            {
                bool rev = Augments.Seen(all[i].augment_id); if (rev) seen++;
                dexCards[i].Bind(all[i], 0, 0, -1, rev);
                if (dexCards[i].button) dexCards[i].button.onClick.RemoveAllListeners();
            }
            if (dexCount) dexCount.text = dexTpl.Replace("{n}", seen.ToString()).Replace("{total}", all.Count.ToString());
        }

        void Fill(List<AugmentCard> list, Transform parent, int count)
        {
            if (!cardTemplate || !parent) return;
            while (list.Count < count) list.Add(Instantiate(cardTemplate, parent));
            for (int i = 0; i < list.Count; i++) list[i].gameObject.SetActive(i < count);
        }

        void OnCard(int uid)
        {
            var it = Augments.Item(uid); if (it == null) return;
            if (synthMode)
            {
                if (synthSel.Contains(uid)) synthSel.Remove(uid);
                else
                {
                    var r0 = synthSel.Count > 0 ? Augments.RowOf(Augments.Item(synthSel[0])) : null; var r = Augments.RowOf(it);
                    if (r0 != null && r0.grade != r.grade) synthSel.Clear();
                    if (synthSel.Count < Augments.SynthNeed(r.grade)) synthSel.Add(uid);
                }
            }
            else { selUid = uid; if (it.isNew) { it.isNew = false; Progress.I.Save(); } }
            SfxManager.Play("ui_click");
            Render();
        }

        void OnUpgrade()
        {
            var it = Augments.Item(selUid);
            if (Augments.Upgrade(it)) SfxManager.Play("ui_buy");
            Render();
        }
        void OnCarry()
        {
            var it = Augments.Item(selUid); if (it == null || !Augments.CarryUnlocked) return;
            Augments.Carry = Augments.Carry == it.uid ? 0 : it.uid;
            SfxManager.Play("ui_click");
            Render();
        }
        void OnSynth()
        {
            if (!Augments.CanSynth(synthSel)) return;
            var res = Augments.Synthesize(new List<int>(synthSel));
            synthSel.Clear(); synthMode = false;
            if (res != null)
            {
                selUid = res.uid;
                var r = Augments.RowOf(res); var g = Augments.Grade(r.grade);
                if (synthResult) { synthResult.text = resultTpl.Replace("{name}", r.aug_name).Replace("{grade}", g != null ? g.grade_name : ""); synthResult.gameObject.SetActive(true); resultT = 2.5f; }
                SfxManager.Play("jgl_unlock");
            }
            Render();
        }
    }
}
