using System;
using UnityEngine;

namespace NKK
{
    // 화면 흔들림 켜기/끄기 (설정 화면). 값은 PlayerPrefs "nkk_screen_shake" (int 1 = 켜짐, 0 = 꺼짐), 슬롯과 상관없는 기기 설정.
    // 끄면 막는 것: 카메라 흔들림 (FxManager.Shake) · 초필살기 화면(UI) 흔들림 · 뽑기 연출 무대 흔들림 · 뽑기 시작 컷 흔들림.
    // 물건·벽·자물쇠처럼 화면 속 하나만 떨리는 연출은 그대로 둠.
    public static class ScreenShake
    {
        public const string Pref = "nkk_screen_shake";
        static int cached = -1;

        public static bool On
        {
            get { if (cached < 0) cached = PlayerPrefs.GetInt(Pref, 1) != 0 ? 1 : 0; return cached == 1; }
            set
            {
                cached = value ? 1 : 0;
                PlayerPrefs.SetInt(Pref, cached); PlayerPrefs.Save();
                Changed?.Invoke(value);
            }
        }

        public static event Action<bool> Changed;
    }
}
