using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NKK
{
    // 판 중 ESC = "정말 포기하고 돌아갈까요? 찍!!?" (웹 quitAsk). 찍!(네) → 치즈 저장하고 아지트(로비)로.
    // 창이 열려 있는 동안 게임 멈춤. 창은 HUD 캔버스 자식 (꺼져 있음).
    public class QuitMenu : MonoBehaviour
    {
        public GameManager Game;
        [Tooltip("포기 창 (전체 화면을 덮는 어두운 판 + 카드)")] public GameObject panel;
        public Button yesButton, noButton;
        [Tooltip("돌아갈 씬 이름")] public string lobbyScene = "Lobby";

        void Awake()
        {
            FxManager.Paused = false;
            if (panel) panel.SetActive(false);
            if (yesButton) yesButton.onClick.AddListener(Quit);
            if (noButton) noButton.onClick.AddListener(Close);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) { if (panel && panel.activeSelf) Close(); else Open(); }
        }

        public void Open() { if (!panel) return; panel.SetActive(true); FxManager.Paused = true; Time.timeScale = 0; }
        public void Close() { if (panel) panel.SetActive(false); FxManager.Paused = false; }

        public void Quit()
        {
            FxManager.Paused = false; Time.timeScale = 1;
            if (Game) Game.SaveProgress();
            SceneManager.LoadScene(lobbyScene);
        }

        void OnDestroy() { FxManager.Paused = false; }
    }
}
