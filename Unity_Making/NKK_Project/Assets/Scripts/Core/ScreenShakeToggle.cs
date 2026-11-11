using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 설정 화면의 "화면 흔들림" 줄: 버튼 하나를 누를 때마다 켜짐 ↔ 꺼짐. 버튼 글은 스트링 테이블 (onKey / offKey), 꺼지면 버튼을 흐리게 (언어 버튼의 안 고른 것처럼).
    public class ScreenShakeToggle : MonoBehaviour
    {
        public Button button;
        [Tooltip("버튼 위 상태 글 (켜짐 / 꺼짐)")] public TMP_Text stateText;
        [Tooltip("켜짐 · 꺼짐 스트링 키")] public int onKey = 8000650, offKey = 8000651;
        [Tooltip("켜짐 · 꺼짐 한국어 (테이블에 없을 때)")] public string onKo = "켜짐", offKo = "꺼짐";
        [Tooltip("켜짐 · 꺼짐 버튼 색")] public Color onColor = Color.white, offColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
        [Tooltip("누를 때 소리")] public string clickSfx = "cheese";

        Image bg;

        void Awake()
        {
            if (!button) button = GetComponentInChildren<Button>(true);
            if (button) { bg = button.targetGraphic as Image; button.onClick.AddListener(Flip); }
        }

        void OnEnable() { Loc.Changed += Show; ScreenShake.Changed += OnChanged; Show(); }
        void OnDisable() { Loc.Changed -= Show; ScreenShake.Changed -= OnChanged; }

        void Flip()
        {
            ScreenShake.On = !ScreenShake.On;
            if (!string.IsNullOrEmpty(clickSfx)) SfxManager.Play(clickSfx);
        }

        void OnChanged(bool _) => Show();

        void Show()
        {
            bool on = ScreenShake.On;
            if (stateText) stateText.text = on ? Loc.Get(onKey, onKo) : Loc.Get(offKey, offKo);
            if (bg) bg.color = on ? onColor : offColor;
        }
    }
}
