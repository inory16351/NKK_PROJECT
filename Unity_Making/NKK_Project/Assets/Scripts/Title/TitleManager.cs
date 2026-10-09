using NKK.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NKK.Title
{
    // 타이틀 씬 (빌드 0): 이어 하기 · 처음부터 · 끝내기 → 저장 슬롯 5개 고르기.
    // 이어 하기 = 그 슬롯으로 로비 (한 번도 출발 안 한 슬롯이면 오프닝부터) / 처음부터 = 슬롯을 비우고 오프닝 컷씬 → 바로 1층.
    // 글은 전부 씬 TMP · 인스펙터, 바뀌는 값만 자리표시: {slot} {tier} {tierName} {floor} {runs} {time}
    public class TitleManager : MonoBehaviour
    {
        [System.Serializable]
        public class SlotCard
        {
            public Button button;
            [Tooltip("슬롯 이름 (자리: {slot})")] public TMP_Text title;
            [Tooltip("저장 내용 (자리: {tier} {tierName} {floor} {runs} {time})")] public TMP_Text info;
            [Tooltip("빈 슬롯일 때 켜질 글 / 그림")] public GameObject emptyMark;
            [HideInInspector] public string titleTpl, infoTpl;
        }

        [Header("씬 이름")]
        public string lobbyScene = "Lobby";
        public string gameScene = "Game";

        [Header("첫 화면")]
        public GameObject menu;
        public Button continueButton, newButton, quitButton;

        [Header("슬롯 고르기")]
        public GameObject slotPanel;
        [Tooltip("슬롯 창 제목")] public TMP_Text slotTitle;
        [Tooltip("이어 하기 때 제목 · 처음부터 때 제목")] public string continueTitle, newTitle;
        public Button backButton;
        public SlotCard[] cards = new SlotCard[Progress.SlotCount];

        [Header("덮어쓰기 확인")]
        public GameObject confirm;
        [Tooltip("확인 글 (자리: {slot})")] public TMP_Text confirmText;
        public Button yesButton, noButton;

        [Header("오프닝")]
        public StoryPlayer story;
        [Tooltip("화면 어두워지기 (선택)")] public CanvasGroup fade;

        bool newMode; int pendingSlot; string confirmTpl;

        void Awake()
        {
            if (continueButton) continueButton.onClick.AddListener(() => OpenSlots(false));
            if (newButton) newButton.onClick.AddListener(() => OpenSlots(true));
            if (quitButton) quitButton.onClick.AddListener(Quit);
            if (backButton) backButton.onClick.AddListener(CloseSlots);
            if (yesButton) yesButton.onClick.AddListener(() => { if (confirm) confirm.SetActive(false); StartNew(pendingSlot); });
            if (noButton) noButton.onClick.AddListener(() => { if (confirm) confirm.SetActive(false); });
            for (int i = 0; i < cards.Length; i++)
            {
                var c = cards[i]; if (c == null) continue; int slot = i + 1;
                c.titleTpl = c.title ? c.title.text : ""; c.infoTpl = c.info ? c.info.text : "";
                if (c.button) c.button.onClick.AddListener(() => Pick(slot));
            }
            confirmTpl = confirmText ? confirmText.text : "";
            if (slotPanel) slotPanel.SetActive(false);
            if (confirm) confirm.SetActive(false);
            if (menu) menu.SetActive(true);
        }

        void Start()
        {
            var p = Progress.I;
            bool any = false;
            if (p) for (int s = 1; s <= Progress.SlotCount; s++) if (p.SlotExists(s)) any = true;
            if (continueButton) continueButton.interactable = any;
        }

        void OpenSlots(bool forNew)
        {
            newMode = forNew;
            if (slotTitle) slotTitle.text = forNew ? newTitle : continueTitle;
            Refresh();
            if (slotPanel) slotPanel.SetActive(true);
            if (menu) menu.SetActive(false);
        }

        void CloseSlots()
        {
            if (slotPanel) slotPanel.SetActive(false);
            if (confirm) confirm.SetActive(false);
            if (menu) menu.SetActive(true);
        }

        void Refresh()
        {
            var p = Progress.I; var db = Data.GameDatabase.Instance;
            for (int i = 0; i < cards.Length; i++)
            {
                var c = cards[i]; if (c == null) continue; int slot = i + 1;
                int tier = 1, floor = 1, runs = 0; string time = "";
                bool has = p && p.SlotSummary(slot, out tier, out floor, out runs, out time);
                string tierName = db && db.Tiers.TryGetValue(tier, out var tr) ? tr.tier_name : "";
                if (c.title) c.title.text = c.titleTpl.Replace("{slot}", slot.ToString());
                if (c.info)
                {
                    c.info.gameObject.SetActive(has);
                    c.info.text = c.infoTpl.Replace("{tier}", tier.ToString()).Replace("{tierName}", tierName).Replace("{floor}", floor.ToString())
                                           .Replace("{runs}", runs.ToString()).Replace("{time}", time);
                }
                if (c.emptyMark) c.emptyMark.SetActive(!has);
                if (c.button) c.button.interactable = newMode || has;
            }
        }

        void Pick(int slot)
        {
            var p = Progress.I; if (!p || (story && story.Playing)) return;
            if (newMode)
            {
                if (p.SlotExists(slot))
                {
                    pendingSlot = slot;
                    if (confirmText) confirmText.text = confirmTpl.Replace("{slot}", slot.ToString());
                    if (confirm) confirm.SetActive(true);
                    return;
                }
                StartNew(slot);
                return;
            }
            if (!p.SlotExists(slot)) return;
            p.UseSlot(slot);
            if (p.runs <= 0) PlayStory();                 // 오프닝만 보고 끈 슬롯 → 오프닝부터
            else SceneManager.LoadScene(lobbyScene);
        }

        void StartNew(int slot)
        {
            Progress.I.NewGame(slot);
            PlayStory();
        }

        void PlayStory()
        {
            if (slotPanel) slotPanel.SetActive(false);
            if (menu) menu.SetActive(false);
            if (story) story.Play(GoFirstFloor); else GoFirstFloor();
        }

        // 오프닝 뒤 바로 1층 (첫 출발)
        void GoFirstFloor()
        {
            var p = Progress.I;
            if (p) { p.runs++; p.Save(); }
            Progress.PendingStartFloor = 1;
            SceneManager.LoadScene(gameScene);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null || !k.escapeKey.wasPressedThisFrame || (story && story.Playing)) return;
            if (confirm && confirm.activeSelf) confirm.SetActive(false);
            else if (slotPanel && slotPanel.activeSelf) CloseSlots();
        }
    }
}
