using System.Collections.Generic;
using NKK.Data;
using NKK.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NKK
{
    // 판 안 증강 (Game 씬 HUD/Augment, 2026-10-19 사용자 결정):
    //  · 31층 도달로 해금 → 31층 이상을 클리어하면 다음 층이 시작될 때 3장 중 1장 고르기 (게임 멈춤)
    //  · 얻은 증강은 바로 보관함(증강 연구소)에 쌓임. 장착 슬롯(기본 5 + 공용 스킬)이 비면 장착, 꽉 차면 보관함으로
    //  · 관리 창 (HUD 버튼 / Tab): 슬롯 ↔ 보관함 교체 · 빼기 · 합성 (판 안에서 합성한 재료는 보관함에서도 사라짐)
    //  · 층 클리어 때 장착한 증강마다 강화 포인트 (Heist → Augments.GivePoints)
    public class AugmentManager : MonoBehaviour
    {
        public static AugmentManager I { get; private set; }
        public static bool Pausing { get; private set; }

        [Header("연결")]
        public GameManager Game;
        public StageManager Stage;
        public RunTimer Timer;

        [Header("고르기 창 (층 클리어 뒤)")]
        public GameObject pickPanel;
        [Tooltip("카드 3장 (순서대로)")] public AugmentCard[] pickCards = new AugmentCard[3];
        [Tooltip("제목 (자리: {floor} = 클리어한 층)")] public TMP_Text pickTitle;
        [Tooltip("슬롯이 꽉 찼을 때 켜지는 안내 ('보관함으로 가요')")] public GameObject pickFullNote;

        [Header("HUD 슬롯 줄")]
        [Tooltip("해금 전엔 숨김")] public GameObject barRoot;
        [Tooltip("슬롯 칸 템플릿 (꺼 둠, 슬롯 수만큼 복제)")] public AugmentCard barTemplate;
        [Tooltip("빈 슬롯 칸 템플릿 (꺼 둠)")] public GameObject barEmptyTemplate;
        public Button manageButton;

        [Header("관리 창")]
        public GameObject managePanel;
        [Tooltip("장착 슬롯 카드가 들어갈 곳")] public Transform slotParent;
        [Tooltip("보관함 카드가 들어갈 곳 (스크롤 Content)")] public Transform storageParent;
        [Tooltip("카드 템플릿 (꺼 둠)")] public AugmentCard cardTemplate;
        [Tooltip("빈 슬롯 템플릿 (꺼 둠)")] public GameObject emptySlotTemplate;
        [Tooltip("아직 안 열린 슬롯 미리보기 (공용 스킬로 늘어나는 최대 칸까지 자물쇠, 증강 설정 slot_max = 5)")] public bool showLockedSlots = true;
        [Tooltip("아무것도 안 골랐을 때 자세히 자리에 켜지는 안내")] public GameObject detailHint;
        [Tooltip("보관함이 비었을 때 켜지는 안내")] public GameObject storageEmptyNote;
        [Tooltip("장착 · 빼기 버튼 그림 (비우면 그대로)")] public Sprite equipSprite, unequipSprite;
        [Tooltip("고른 증강 자세히 (카드)")] public AugmentCard detail;
        [Tooltip("슬롯 수 (자리: {n} {max})")] public TMP_Text slotCountText;
        [Tooltip("장착 / 교체할 슬롯 고르기 / 빼기 버튼 (글은 아래 문자열)")] public Button actionButton;
        public TMP_Text actionLabel;
        public string equipLabel = "장착", replaceLabel = "교체할 슬롯 고르기", unequipLabel = "빼기", replacePickLabel = "바꿀 슬롯을 누르세요";
        public int equipLabelKey, replaceLabelKey, unequipLabelKey, replacePickLabelKey;
        [Tooltip("합성 모드 켜기/끄기")] public Button synthToggle;
        [Tooltip("합성 모드일 때 켜짐 (안내 글 등)")] public GameObject synthOnMark;
        [Tooltip("합성하기 (글 자리: {n} {need})")] public Button synthButton;
        public TMP_Text synthLabel;
        [Tooltip("합성 결과 알림 (자리: {name}, 잠깐 켜짐)")] public TMP_Text synthResult;
        public Button closeButton;

        [Header("테스트")]
        [Tooltip("밸런스 측정: 창 없이 가장 높은 등급을 자동으로 고름")] public bool autoPick;
        [Tooltip("층과 상관없이 증강 열기")] public bool testOpen;

        public bool PickShowing => pickPanel && pickPanel.activeSelf;
        int pendingFloor = -1;
        readonly List<AugmentRow> offer = new();
        readonly List<AugmentCard> slotCards = new(), storageCards = new(), barCards = new();
        readonly List<GameObject> emptyCards = new(), barEmpties = new(), lockCards = new();
        readonly List<int> synthSel = new();
        int selUid; bool synthMode, replaceMode;
        int builtVer = -1, barVer = -1; float resultT;
        string pickTitleTpl, synthTpl, resultTpl, countTpl;

        void Awake()
        {
            I = this; Pausing = false;
            Augments.ClearRun();
            if (pickPanel) pickPanel.SetActive(false);
            if (managePanel) managePanel.SetActive(false);
            if (barTemplate) barTemplate.gameObject.SetActive(false);
            if (barEmptyTemplate) barEmptyTemplate.SetActive(false);
            if (cardTemplate) cardTemplate.gameObject.SetActive(false);
            if (emptySlotTemplate) emptySlotTemplate.SetActive(false);
            if (synthResult) synthResult.gameObject.SetActive(false);
            for (int i = 0; i < pickCards.Length; i++) { int k = i; if (pickCards[i] && pickCards[i].button) pickCards[i].button.onClick.AddListener(() => Pick(k)); }
            if (manageButton) manageButton.onClick.AddListener(OpenManage);
            if (closeButton) closeButton.onClick.AddListener(CloseManage);
            if (actionButton) actionButton.onClick.AddListener(OnAction);
            if (synthToggle) synthToggle.onClick.AddListener(() => { synthMode = !synthMode; synthSel.Clear(); replaceMode = false; Refresh(); });
            if (synthButton) synthButton.onClick.AddListener(DoSynth);
        }
        void Start()
        {
            pickTitleTpl = pickTitle ? LocText.Tpl(pickTitle, pickTitle.text) : "";
            synthTpl = synthLabel ? LocText.Tpl(synthLabel, synthLabel.text) : "";
            resultTpl = synthResult ? LocText.Tpl(synthResult, synthResult.text) : "";
            countTpl = slotCountText ? LocText.Tpl(slotCountText, slotCountText.text) : "";
            Augments.EquipCarry();          // 41층 해금: 들고 가는 증강
        }
        void OnDestroy() { if (I == this) { I = null; Pausing = false; } Augments.TestOpen = false; }

        // Heist: 층 클리어 (마지막 층이면 판이 끝나니 안 고름)
        public void OnFloorCleared(int floor)
        {
            if (!Augments.Unlocked || floor < Augments.UnlockFloor || floor >= StageManager.LastFloor) return;
            pendingFloor = floor;
        }

        void Update()
        {
            Augments.TestOpen = testOpen;
            UpdateBar();
            if (resultT > 0 && (resultT -= Time.unscaledDeltaTime) <= 0 && synthResult) synthResult.gameObject.SetActive(false);
            if (pendingFloor > 0 && CanShowPick()) ShowPick();
            var k = Keyboard.current;
            if (k != null && !Pausing && k.tabKey.wasPressedThisFrame && Augments.Unlocked && !GameOver.Active && !Heist.Active) OpenManage();
            else if (k != null && managePanel && managePanel.activeSelf && (k.escapeKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame)) CloseManage();
            if (managePanel && managePanel.activeSelf && builtVer != Augments.Version) Refresh();
        }

        bool CanShowPick() => !Heist.Active && !(Stage && Stage.Climbing) && !GameOver.Active && !NKK.Tutorial.TutorialBox.Showing && !FxManager.Paused
                              && !(managePanel && managePanel.activeSelf) && !(NKK.Ults.SuperUltManager.I && NKK.Ults.SuperUltManager.I.Busy);

        // ── 고르기 ──
        void ShowPick()
        {
            int floor = pendingFloor; pendingFloor = -1;
            offer.Clear(); offer.AddRange(Augments.RollOffer(floor, Augments.OfferCount));
            if (offer.Count == 0) return;
            if (autoPick || !pickPanel)
            {
                int best = 0; for (int i = 1; i < offer.Count; i++) if (offer[i].grade > offer[best].grade) best = i;
                Take(offer[best]); return;
            }
            for (int i = 0; i < pickCards.Length; i++)
            {
                if (!pickCards[i]) continue;
                bool on = i < offer.Count; pickCards[i].gameObject.SetActive(on);
                if (on) pickCards[i].Bind(offer[i], 0, 0, -1, true);
                if (on) pickCards[i].SetNew(!Augments.Seen(offer[i].augment_id));
            }
            if (pickTitle) pickTitle.text = pickTitleTpl.Replace("{floor}", floor.ToString());
            if (pickFullNote) pickFullNote.SetActive(!Augments.HasFreeSlot);
            pickPanel.SetActive(true);
            Pausing = true;
            SfxManager.Play("jgl_unlock");
        }
        void Pick(int i)
        {
            if (i < 0 || i >= offer.Count) return;
            Take(offer[i]);
            if (pickPanel) pickPanel.SetActive(false);
            Pausing = false;
            SfxManager.Play("ui_buy");
        }
        void Take(AugmentRow r)
        {
            float t0 = CommonSkill.TimeAdd;
            var it = Augments.Add(r.augment_id);
            if (it != null && Augments.HasFreeSlot) Augments.Equip(it.uid);
            GiveTimeDelta(t0);
            Debug.Log($"[Augment] {r.aug_name} (등급 {r.grade}) {(it != null && Augments.IsEquipped(it.uid) ? "장착" : "보관함")}");
        }
        // 제한시간 증강은 지금 층에도 바로 (늘어난 만큼 남은 시간 +)
        void GiveTimeDelta(float before)
        {
            float d = CommonSkill.TimeAdd - before;
            if (Timer && Mathf.Abs(d) > 0.01f) { Timer.Max += d; Timer.Left = Mathf.Max(1, Timer.Left + d); }
        }

        // ── HUD 슬롯 줄 ──
        void UpdateBar()
        {
            bool show = Augments.Unlocked || Augments.Equipped.Count > 0;
            if (barRoot && barRoot.activeSelf != show) barRoot.SetActive(show);
            if (!show || barVer == Augments.Version || !barTemplate) return;
            barVer = Augments.Version;
            int n = Augments.SlotCount;
            while (barCards.Count < n) barCards.Add(Instantiate(barTemplate, barTemplate.transform.parent));
            while (barEmpties.Count < n) barEmpties.Add(barEmptyTemplate ? Instantiate(barEmptyTemplate, barEmptyTemplate.transform.parent) : null);
            for (int i = 0; i < barEmpties.Count; i++) if (barEmpties[i]) SlotArt(barEmpties[i], false);
            for (int i = 0; i < barCards.Count; i++)
            {
                bool has = i < Augments.Equipped.Count, inSlot = i < n;
                barCards[i].gameObject.SetActive(inSlot && has);
                if (barEmpties[i]) { barEmpties[i].SetActive(inSlot && !has); barEmpties[i].transform.SetSiblingIndex(barCards[i].transform.GetSiblingIndex() + 1); }
                if (inSlot && has) barCards[i].BindItem(Augments.Item(Augments.Equipped[i]));
            }
        }

        // ── 관리 창 ──
        public void OpenManage()
        {
            if (!managePanel || Pausing || managePanel.activeSelf) return;
            selUid = 0; synthMode = false; replaceMode = false; synthSel.Clear();
            managePanel.SetActive(true);          // 관리 창은 게임을 멈추지 않음 (2026-10-20 사용자: 시간은 자연스럽게 흐르게) — 고르기 창만 멈춤
            Refresh();
        }
        public void CloseManage()
        {
            if (!managePanel || !managePanel.activeSelf) return;
            managePanel.SetActive(false);
        }

        void Refresh()
        {
            builtVer = Augments.Version;
            int n = Augments.SlotCount;
            // 슬롯
            Fill(slotCards, slotParent, Augments.Equipped.Count);
            for (int i = 0; i < slotCards.Count; i++) if (slotCards[i].gameObject.activeSelf) BindCard(slotCards[i], Augments.Item(Augments.Equipped[i]), true);
            while (emptyCards.Count < n) { if (!emptySlotTemplate) break; emptyCards.Add(Instantiate(emptySlotTemplate, slotParent)); }
            for (int i = 0; i < emptyCards.Count; i++) { emptyCards[i].SetActive(i < n - Augments.Equipped.Count); emptyCards[i].transform.SetAsLastSibling(); SlotArt(emptyCards[i], false); }
            int locked = showLockedSlots ? Mathf.Max(0, Mathf.RoundToInt(Augments.Cfg("slot_max", 5)) - n) : 0;
            while (lockCards.Count < locked) { if (!emptySlotTemplate) break; lockCards.Add(Instantiate(emptySlotTemplate, slotParent)); }
            for (int i = 0; i < lockCards.Count; i++) { lockCards[i].SetActive(i < locked); lockCards[i].transform.SetAsLastSibling(); SlotArt(lockCards[i], true); }
            // 보관함 (장착 안 한 것, 높은 등급 · 높은 레벨 먼저)
            var st = new List<Progress.AugItem>();
            foreach (var it in Augments.Items) if (!Augments.IsEquipped(it.uid) && Augments.RowOf(it) != null) st.Add(it);
            st.Sort((a, b) => { int g = Augments.RowOf(b).grade.CompareTo(Augments.RowOf(a).grade); return g != 0 ? g : b.lv.CompareTo(a.lv); });
            Fill(storageCards, storageParent, st.Count);
            if (storageEmptyNote) storageEmptyNote.SetActive(st.Count == 0);
            for (int i = 0; i < st.Count; i++) BindCard(storageCards[i], st[i], false);
            if (slotCountText) slotCountText.text = countTpl.Replace("{n}", Augments.Equipped.Count.ToString()).Replace("{max}", n.ToString());
            // 자세히 · 버튼
            var sel = Augments.Item(selUid);
            if (detail) { detail.gameObject.SetActive(sel != null && !synthMode); if (sel != null) detail.BindItem(sel); }
            if (detailHint) detailHint.SetActive(sel == null && !synthMode);
            bool selEq = sel != null && Augments.IsEquipped(sel.uid);
            if (actionButton)
            {
                actionButton.gameObject.SetActive(sel != null && !synthMode);
                actionButton.interactable = !replaceMode;
                string lab = replaceMode ? Loc.Get(replacePickLabelKey, replacePickLabel) : selEq ? Loc.Get(unequipLabelKey, unequipLabel) : Augments.HasFreeSlot ? Loc.Get(equipLabelKey, equipLabel) : Loc.Get(replaceLabelKey, replaceLabel);
                if (actionLabel) actionLabel.text = lab;
                var bi = actionButton.image; var bs = selEq ? unequipSprite : equipSprite; if (bi && bs) bi.sprite = bs;      // 장착 = 초록 · 빼기 = 빨강
            }
            if (synthOnMark) synthOnMark.SetActive(synthMode);
            int need = SelGradeNeed();
            if (synthButton)
            {
                synthButton.gameObject.SetActive(synthMode);
                synthButton.interactable = Augments.CanSynth(synthSel);
                if (synthLabel) synthLabel.text = synthTpl.Replace("{n}", synthSel.Count.ToString()).Replace("{need}", need > 0 ? need.ToString() : "-");
            }
        }
        int SelGradeNeed() { var r = synthSel.Count > 0 ? Augments.RowOf(Augments.Item(synthSel[0])) : null; return r != null ? Augments.SynthNeed(r.grade) : 0; }

        // 빈 슬롯 · 안 열린 슬롯 그림 (AugmentArt, 9칸 늘리기)
        static void SlotArt(GameObject o, bool locked)
        {
            var art = AugmentArt.I; var im = o ? o.GetComponent<Image>() : null; if (!art || !im) return;
            var s = locked ? art.lockedSlot : art.emptySlot; if (!s) return;
            im.color = Color.white; AugmentCard.SliceFit(im, s);
        }

        void Fill(List<AugmentCard> list, Transform parent, int count)
        {
            if (!cardTemplate || !parent) return;
            while (list.Count < count) { var c = Instantiate(cardTemplate, parent); int idx = list.Count; list.Add(c); }
            for (int i = 0; i < list.Count; i++) list[i].gameObject.SetActive(i < count);
        }
        void BindCard(AugmentCard c, Progress.AugItem it, bool equipped)
        {
            c.BindItem(it);
            c.SetEquipped(false);           // 판 안에선 슬롯 줄이 곧 장착 표시
            c.SetNew(it != null && it.isNew);
            c.SetSelected(it != null && (synthMode ? synthSel.Contains(it.uid) : it.uid == selUid));
            if (c.button) { c.button.onClick.RemoveAllListeners(); int uid = it != null ? it.uid : 0; c.button.onClick.AddListener(() => OnCard(uid, equipped)); }
        }

        void OnCard(int uid, bool equipped)
        {
            var it = Augments.Item(uid); if (it == null) return;
            if (synthMode)
            {
                if (synthSel.Contains(uid)) synthSel.Remove(uid);
                else
                {
                    var r0 = synthSel.Count > 0 ? Augments.RowOf(Augments.Item(synthSel[0])) : null;
                    var r = Augments.RowOf(it);
                    if (r0 != null && r0.grade != r.grade) synthSel.Clear();          // 다른 등급을 누르면 그 등급으로 새로 고르기
                    if (synthSel.Count < Augments.SynthNeed(r.grade)) synthSel.Add(uid);
                }
                Refresh(); return;
            }
            if (replaceMode && equipped && selUid != 0 && !Augments.IsEquipped(selUid))
            {
                float t0 = CommonSkill.TimeAdd;
                Augments.Swap(uid, selUid); replaceMode = false;
                GiveTimeDelta(t0);
                SfxManager.Play("ui_buy");
                Refresh(); return;
            }
            replaceMode = false;
            selUid = uid; it.isNew = false;
            Refresh();
        }

        void OnAction()
        {
            var sel = Augments.Item(selUid); if (sel == null) return;
            float t0 = CommonSkill.TimeAdd;
            if (Augments.IsEquipped(sel.uid)) Augments.Unequip(sel.uid);
            else if (Augments.HasFreeSlot) Augments.Equip(sel.uid);
            else { replaceMode = true; Refresh(); return; }
            GiveTimeDelta(t0);
            Refresh();
        }

        void DoSynth()
        {
            if (!Augments.CanSynth(synthSel)) return;
            float t0 = CommonSkill.TimeAdd;
            var res = Augments.Synthesize(new List<int>(synthSel));
            synthSel.Clear();
            if (res != null)
            {
                if (Augments.HasFreeSlot) Augments.Equip(res.uid);
                selUid = res.uid;
                if (synthResult) { synthResult.text = resultTpl.Replace("{name}", Augments.RowOf(res).aug_name); synthResult.gameObject.SetActive(true); resultT = 2.5f; }
                SfxManager.Play("jgl_unlock");
            }
            GiveTimeDelta(t0);
            synthMode = false;
            Refresh();
        }
    }
}
