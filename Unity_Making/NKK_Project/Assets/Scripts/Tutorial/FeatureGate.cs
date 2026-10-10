using UnityEngine;
using UnityEngine.UI;

namespace NKK.Tutorial
{
    // 기능 잠금 표시 (튜토리얼 테이블 Unlock_Id). 잠겨 있으면 숨기거나, 자물쇠를 띄우고 버튼을 막음.
    // 해금은 TutorialManager 가 단계의 unlock 칸으로 함 (Progress.Unlock)
    public class FeatureGate : MonoBehaviour
    {
        [Tooltip("기능 id: promote · power · pop · run · rec · skill · rats · rank · dex")] public string feature;
        [Tooltip("잠겨 있으면 꺼 둘 오브젝트 (예: 게임 HUD 승급 버튼)")] public GameObject[] hideWhenLocked;
        [Tooltip("잠겨 있으면 켤 자물쇠 표시")] public GameObject lockMark;
        [Tooltip("잠겨 있으면 못 누르게 할 버튼")] public Button[] buttons;
        [Tooltip("잠겨 있으면 이 투명도 (CanvasGroup 이 있을 때)")] public CanvasGroup dim;
        [Range(0, 1)] public float lockedAlpha = 0.6f;

        int ver = -1;

        public bool Locked => Progress.I && !Progress.I.IsUnlocked(feature);

        void OnEnable() { ver = -1; Apply(); }
        void Update() { if (Progress.I && Progress.I.UnlockVersion != ver) Apply(); }

        void Apply()
        {
            ver = Progress.I ? Progress.I.UnlockVersion : 0;
            bool locked = Locked;
            if (hideWhenLocked != null) foreach (var g in hideWhenLocked) if (g) g.SetActive(!locked);
            if (lockMark) lockMark.SetActive(locked);
            if (buttons != null) foreach (var b in buttons) if (b) b.interactable = !locked;
            if (dim) dim.alpha = locked ? lockedAlpha : 1;
        }
    }
}
