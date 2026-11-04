using UnityEngine;

namespace NKK
{
    // 마우스 포인터 (쥐 머리 화살표). 설정 에셋 = Resources/CursorSkin.asset (인스펙터에서 그림·끝점 조정).
    // 게임이 켜질 때 저절로 붙어 모든 씬에서 같은 포인터, 마우스를 누르고 있는 동안은 '누름' 그림.
    [CreateAssetMenu(menuName = "NKK/Cursor Skin")]
    public class CursorSkin : ScriptableObject
    {
        [Tooltip("평소 포인터 (텍스처 형식 Cursor, Read/Write 켬)")] public Texture2D normal;
        [Tooltip("누르고 있을 때 포인터 (비우면 평소 그림 그대로)")] public Texture2D pressed;
        [Tooltip("클릭 지점 = 그림 왼쪽 위에서 픽셀 (쥐 코끝)")] public Vector2 hotspot = new(2, 2);
        [Tooltip("하드웨어 포인터를 못 쓰는 환경이면 켬 (소프트웨어 포인터)")] public bool forceSoftware;
        [System.Serializable] public class Deco { public int decoId; public Texture2D normal, pressed; }
        [Tooltip("도토리 상점 마우스 커서 장식 (종류 Cursor) → 그림 (같은 크기 · 같은 끝점)")] public Deco[] decos;

        // 그림이 준비된 커서 장식 id → DecoArt.Ready (상점 '적용하기')
        public static void RegisterReady()
        {
            var s = skin ? skin : Resources.Load<CursorSkin>("CursorSkin");
            if (s && s.decos != null) foreach (var d in s.decos) if (d != null && d.normal) DecoArt.Ready.Add(d.decoId);
        }
        static int equipped;

        static CursorSkin skin;
        static bool down;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            skin = Resources.Load<CursorSkin>("CursorSkin");
            if (!skin || !skin.normal) return;
            down = false; equipped = DecoArt.Equipped("Cursor");
            RegisterReady();
            skin.Apply(false);
            var go = new GameObject("CursorSkin") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }

        void Apply(bool press)
        {
            Texture2D n = normal, pr = pressed;
            if (equipped != 0 && decos != null) foreach (var d in decos) if (d != null && d.decoId == equipped && d.normal) { n = d.normal; pr = d.pressed; break; }
            var tex = press && pr ? pr : n;
            Cursor.SetCursor(tex, hotspot, forceSoftware ? CursorMode.ForceSoftware : CursorMode.Auto);
        }

        // 누름 상태만 지켜봄
        class Runner : MonoBehaviour
        {
            void Update()
            {
                var m = UnityEngine.InputSystem.Mouse.current;
                bool d = m != null && (m.leftButton.isPressed || m.rightButton.isPressed);
                int e = DecoArt.Equipped("Cursor");          // 커서 장식을 바꾸면 바로
                if ((d == down && e == equipped) || !skin) return;
                down = d; equipped = e;
                skin.Apply(d);
            }
            void OnApplicationFocus(bool f) { if (f && skin) skin.Apply(down); }     // 창 바꿨다 오면 다시
        }
    }
}
