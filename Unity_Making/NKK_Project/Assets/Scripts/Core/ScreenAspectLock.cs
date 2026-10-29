using UnityEngine;

namespace NKK
{
    // PC(Windows) 빌드 화면 비율 16:9 고정. 모니터·창이 16:9가 아니면 그 안에 들어가는 가장 큰 16:9 해상도로 바꿈
    // → 남는 곳은 검은 띠 (21:9 = 좌우, 16:10·4:3 = 위아래). 전체 화면이면 유니티가 띠를 넣어 늘이지 않고 보여 줌.
    // 창 모드에서 창 크기를 바꾸면 손을 뗀 뒤(0.3초) 16:9로 다시 맞춤. 에디터 · 웹 빌드는 손대지 않음.
    public static class ScreenAspectLock
    {
        const float Aspect = 16f / 9f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            Fit();
            var go = new GameObject("ScreenAspectLock") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
#endif
        }

        // 가로 w · 세로 h 안에 들어가는 가장 큰 16:9
        static Vector2Int Inside(int w, int h)
            => (float)w / h > Aspect ? new Vector2Int(Mathf.RoundToInt(h * Aspect), h) : new Vector2Int(w, Mathf.RoundToInt(w / Aspect));

        static void Fit()
        {
            var mode = Screen.fullScreenMode;
            Vector2Int s;
            if (mode == FullScreenMode.Windowed)
            {
                if (Screen.height <= 0 || Mathf.Abs((float)Screen.width / Screen.height - Aspect) < 0.01f) return;
                s = Inside(Screen.width, Screen.height);
            }
            else
            {
                // 전체 화면 = 모니터 해상도 기준 (2560×1080 → 1920×1080, 1920×1200 → 1920×1080)
                var d = Screen.mainWindowDisplayInfo;
                if (d.width <= 0 || d.height <= 0) return;
                s = Inside(d.width, d.height);
            }
            if (Screen.width == s.x && Screen.height == s.y) return;
            Screen.SetResolution(s.x, s.y, mode);
        }

        // 화면 크기 · 모드가 바뀌면 멈춘 뒤 다시 맞춤 (창 끌기 중엔 기다림, Alt+Enter 전환도 여기서)
        class Runner : MonoBehaviour
        {
            int w, h; FullScreenMode m; float still;

            void Update()
            {
                if (Screen.width != w || Screen.height != h || Screen.fullScreenMode != m)
                {
                    w = Screen.width; h = Screen.height; m = Screen.fullScreenMode; still = 0;
                    return;
                }
                if (still < 0 || (still += Time.unscaledDeltaTime) < 0.3f) return;
                still = -1;
                Fit();
            }
        }
    }
}
