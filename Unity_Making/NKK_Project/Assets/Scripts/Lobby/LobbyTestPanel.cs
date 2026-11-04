using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 로비 테스트(관리자) 패널 (2026-11-04, 사용자: 로비에서도 부를 수 있게). 출시 때 끄기.
    // 게임 씬 테스트 패널과 같은 키(`)로 보이기/숨기기, 숨김 상태도 같은 저장값(nkk_test_panel_hidden)을 씀.
    // 버튼: 도토리 +10,000 · 전부 해제 · 저장값 초기화 (게임 씬 패널과 같은 동작, 로비를 다시 엶)
    public class LobbyTestPanel : MonoBehaviour
    {
        public Button acornButton, unlockAllButton, resetButton;
        public int acornAmount = 10000;
        [Tooltip("보이기/숨기기 키")] public Key toggleKey = Key.Backquote;
        [Tooltip("패널 전체 (숨길 때 투명 · 클릭 안 됨)")] public CanvasGroup group;
        [Tooltip("전부 해제 · 초기화 뒤 다시 열 씬")] public string lobbyScene = "Lobby";
        const string HiddenPref = "nkk_test_panel_hidden";

        void Start()
        {
            if (acornButton) acornButton.onClick.AddListener(() => { if (Progress.I) Progress.I.DevAddAcorn(acornAmount); });
            if (unlockAllButton) unlockAllButton.onClick.AddListener(() => { if (!Progress.I) return; Progress.I.UnlockEverything(); Reload(); });
            if (resetButton) resetButton.onClick.AddListener(() => { if (!Progress.I) return; Progress.I.ResetAll(); Reload(); });
            SetVisible(PlayerPrefs.GetInt(HiddenPref, 0) == 0);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k != null && toggleKey != Key.None && k[toggleKey].wasPressedThisFrame) SetVisible(group && group.alpha < 0.5f);
        }

        void SetVisible(bool on)
        {
            if (!group) return;
            group.alpha = on ? 1 : 0; group.interactable = on; group.blocksRaycasts = on;
            PlayerPrefs.SetInt(HiddenPref, on ? 0 : 1);
        }

        void Reload() { Time.timeScale = 1; UnityEngine.SceneManagement.SceneManager.LoadScene(lobbyScene); }
    }
}
