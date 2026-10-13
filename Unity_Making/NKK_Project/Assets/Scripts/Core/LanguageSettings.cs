using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 설정 창 언어 고르기 (타이틀 UI/Settings/Card/Language). 버튼마다 언어 코드(ko · en), 고른 버튼은 색으로 표시.
    public class LanguageSettings : MonoBehaviour
    {
        [System.Serializable]
        public class Option
        {
            [Tooltip("언어 코드: ko · en")] public string lang;
            public Button button;
            [Tooltip("고른 언어일 때 색을 바꿀 그림 (비우면 버튼 그림)")] public Graphic mark;
        }

        public Option[] options;
        public Color onColor = new(0.93f, 0.71f, 0.33f), offColor = Color.white;

        void Awake()
        {
            if (options == null) return;
            foreach (var o in options) { var l = o.lang; if (o.button) o.button.onClick.AddListener(() => { Loc.SetLang(l); SfxManager.Play("ui_click"); }); }
        }

        void OnEnable() { Loc.Changed += Show; Show(); }
        void OnDisable() => Loc.Changed -= Show;

        void Show()
        {
            if (options == null) return;
            foreach (var o in options)
            {
                var g = o.mark ? o.mark : o.button ? o.button.targetGraphic : null;
                if (g) g.color = o.lang == Loc.Lang ? onColor : offColor;
            }
        }
    }
}
