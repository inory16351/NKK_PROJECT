using UnityEngine;

namespace NKK
{
    // 마우스 포인터 (쥐 머리 화살표). 설정 에셋 = Resources/CursorSkin.asset (인스펙터에서 그림·끝점 조정).
    // 게임이 켜질 때 저절로 붙어 모든 씬에서 같은 포인터, 마우스를 누르고 있는 동안은 '누름' 그림.
    // 그림 원본은 64px, 화면에는 윈도우 기본 커서 크기(32px × 화면 배율)로 줄여서 씀 → 장식마다 크기가 같음.
    [CreateAssetMenu(menuName = "NKK/Cursor Skin")]
    public class CursorSkin : ScriptableObject
    {
        [Tooltip("평소 포인터 (텍스처 형식 Cursor, Read/Write 켬)")] public Texture2D normal;
        [Tooltip("누르고 있을 때 포인터 (비우면 평소 그림 그대로)")] public Texture2D pressed;
        [Tooltip("클릭 지점 = 원본 그림 왼쪽 위에서 픽셀 (쥐 코끝, 줄일 때 같이 줄어듦)")] public Vector2 hotspot = new(2, 2);
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

        // 커서 장식 그림 (평소 · 누름). 0 = 기본 그림 — 상점 미리보기 (ShopPreview)
        public static (Texture2D normal, Texture2D pressed) TexturesFor(int decoId)
        {
            var s = skin ? skin : Resources.Load<CursorSkin>("CursorSkin");
            if (!s) return (null, null);
            if (decoId != 0 && s.decos != null) foreach (var d in s.decos) if (d != null && d.decoId == decoId && d.normal) return (d.normal, d.pressed ? d.pressed : d.normal);
            return (s.normal, s.pressed ? s.pressed : s.normal);
        }

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
            PixelSize = CalcSize();
            Current = tex; Hotspot = hotspot;
            var s = Scaled(tex, PixelSize);
            Cursor.SetCursor(s, hotspot * (PixelSize / (float)tex.width), forceSoftware ? CursorMode.ForceSoftware : CursorMode.Auto);
        }

        // 화면에 보이는 커서 한 변 (픽셀) = 윈도우 기본 커서 32px × 화면 배율 (100% = 32, 150% = 48). 쿨타임 게이지가 이 크기의 외곽선을 따라감
        public static int PixelSize { get; private set; } = 32;
        // 지금 화면에 보이는 커서 원본 그림 (스킨 · 누름 반영) 과 끝점 (원본 픽셀, 왼쪽 위 기준) → 쿨타임 외곽선 (CursorOutline)
        public static Texture2D Current { get; private set; }
        public static Vector2 Hotspot { get; private set; }
        static int CalcSize() { float dpi = Screen.dpi > 0 ? Screen.dpi : 96; return Mathf.Clamp(Mathf.RoundToInt(32 * dpi / 96f), 32, 64); }

        // 원본(64px)을 PixelSize 로 줄인 사본 (크기별 캐시)
        static readonly System.Collections.Generic.Dictionary<(Texture2D, int), Texture2D> scaled = new();
        static Texture2D Scaled(Texture2D src, int size)
        {
            if (!src || src.width == size || !src.isReadable) return src;
            if (scaled.TryGetValue((src, size), out var t) && t) return t;
            t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size]; int k = Mathf.Max(1, Mathf.CeilToInt(src.width / (float)size));
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                // 칸 안을 k×k 로 평균 (알파 곱한 평균 → 테두리 번짐 없음)
                Color sum = default; float a = 0;
                for (int j = 0; j < k; j++) for (int i = 0; i < k; i++)
                {
                    var c = src.GetPixelBilinear((x + (i + 0.5f) / k) / size, (y + (j + 0.5f) / k) / size);
                    sum.r += c.r * c.a; sum.g += c.g * c.a; sum.b += c.b * c.a; a += c.a;
                }
                px[y * size + x] = a > 0 ? new Color(sum.r / a, sum.g / a, sum.b / a, a / (k * k)) : Color.clear;
            }
            t.SetPixels(px); t.Apply(false, false);
            scaled[(src, size)] = t;
            return t;
        }

        // 누름 상태만 지켜봄
        class Runner : MonoBehaviour
        {
            void Update()
            {
                var m = UnityEngine.InputSystem.Mouse.current;
                bool d = m != null && (m.leftButton.isPressed || m.rightButton.isPressed);
                int e = DecoArt.Equipped("Cursor");          // 커서 장식을 바꾸면 바로
                if ((d == down && e == equipped && (Time.frameCount % 60 != 0 || CalcSize() == PixelSize)) || !skin) return;      // 화면 배율이 바뀌어도 (1초마다 확인)
                down = d; equipped = e;
                skin.Apply(d);
            }
            void OnApplicationFocus(bool f) { if (f && skin) skin.Apply(down); }     // 창 바꿨다 오면 다시
        }
    }
}
