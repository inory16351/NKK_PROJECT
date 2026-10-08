using NKK.Stage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NKK
{
    // 직교 카메라: 1920×1080 기준. 휠 = 확대/축소, 왼쪽 끌기(또는 오른쪽·가운데 버튼) = 이동. 짧은 왼쪽 클릭은 총공격(RatManager).
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public StageManager Stage;
        [Tooltip("기본으로 보이는 가로 폭 (게임 단위). 웹게임 W = 1280")] public float viewWidth = 1280;
        [Tooltip("웹게임 0.45 ~ 1.3")] public float minZoom = 0.45f, maxZoom = 1.3f;
        public float zoom = 1;
        [Tooltip("휠 한 칸당 확대 배율")] public float wheelStep = 0.08f;
        [Tooltip("이 픽셀보다 많이 끌어야 화면 이동")] public float dragPixels = 16;

        [HideInInspector] public Vector3 shakeOffset;     // FxManager 가 넣음
        [Header("필살기 연출 (UltimateManager 가 넣음)")]
        [HideInInspector] public float ultZoom = 1;         // 추가 확대 배율
        [HideInInspector] public bool ultFollow;            // 필살기 쓰는 쥐 따라가기
        [HideInInspector] public Vector2 ultFocus;
        [Tooltip("필살기 쥐 따라가는 빠르기")] public float ultFollowSpeed = 6;
        [Header("미리보기 (보스 층: 계단 방에서 기다리는 보스를 잠깐 비춤)")]
        [Tooltip("가는 시간 · 머무는 시간 · 돌아오는 시간 (초)")] public float peekIn = 0.8f, peekHold = 1.4f, peekOut = 0.7f;
        float peekT = -1, peekWait;
        Vector3 peekFrom, peekTo;
        public bool Peeking => peekT >= 0 || peekWait > 0;
        Vector3 lastShake;
        Camera cam;
        Vector3 dragOrigin; Vector2 pressPos; bool dragging, pressed;

        void Awake() { cam = GetComponent<Camera>(); cam.orthographic = true; }

        void LateUpdate()
        {
            transform.position -= lastShake;               // 지난 프레임 흔들림 빼고 계산
            var m = Mouse.current;
            if (UpdatePeek()) m = null;                    // 미리보기 중엔 끌기·제한 없이 카메라를 옮김
            if (m != null)
            {
                float wheel = m.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f) zoom = Mathf.Clamp(zoom * (wheel > 0 ? 1 + wheelStep : 1 / (1 + wheelStep)), minZoom, maxZoom);
                Vector2 sp = m.position.ReadValue();
                bool held = m.leftButton.isPressed || m.rightButton.isPressed || m.middleButton.isPressed;
                bool down = m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame || m.middleButton.wasPressedThisFrame;
                if (down) { pressed = true; dragging = false; pressPos = sp; dragOrigin = cam.ScreenToWorldPoint(sp); }
                if (pressed && held)
                {
                    if (!dragging && (sp - pressPos).magnitude > dragPixels) dragging = true;
                    if (dragging) transform.position += dragOrigin - cam.ScreenToWorldPoint(sp);
                }
                if (!held) pressed = dragging = false;
            }
            if (ultFollow)
            {
                var f = World.ToUnity(ultFocus.x, ultFocus.y);
                var p = transform.position;
                float k = Mathf.Min(1, Time.unscaledDeltaTime * ultFollowSpeed);
                transform.position = new Vector3(p.x + (f.x - p.x) * k, p.y + (f.y - p.y) * k, p.z);
            }
            // 가로 폭 viewWidth / zoom 이 화면에 꽉 차게
            float w = viewWidth / (zoom * ultZoom) * World.U;
            cam.orthographicSize = w / cam.aspect / 2;
            if (peekT < 0) Clamp();
            lastShake = shakeOffset;
            transform.position += lastShake;
        }

        // 열린 방 바깥으로 너무 나가지 않게
        void Clamp()
        {
            if (!Stage || Stage.Open.Count == 0) return;
            var b = Stage.OpenBounds();
            Vector3 a = World.ToUnity(b.xMin, b.yMax), c = World.ToUnity(b.xMax, b.yMin);
            var p = transform.position;
            float hw = cam.orthographicSize * cam.aspect, hh = cam.orthographicSize;
            p.x = a.x + hw > c.x - hw ? (a.x + c.x) / 2 : Mathf.Clamp(p.x, a.x + hw * 0.5f, c.x - hw * 0.5f);
            p.y = a.y + hh > c.y - hh ? (a.y + c.y) / 2 : Mathf.Clamp(p.y, a.y + hh * 0.5f, c.y - hh * 0.5f);
            transform.position = p;
        }

        // 게임 좌표 (x, y) 를 delay 초 뒤 잠깐 비췄다가 지금 자리로 돌아옴 (열린 방 바깥이어도)
        public void Peek(float x, float y, float delay = 0)
        {
            var c = World.ToUnity(x, y);
            peekTo = new Vector3(c.x, c.y, transform.position.z);
            peekWait = Mathf.Max(0.0001f, delay); peekT = -1;
        }
        public void CancelPeek() { if (peekT >= 0) transform.position = peekFrom; peekT = -1; peekWait = 0; }

        bool UpdatePeek()
        {
            float dt = Time.unscaledDeltaTime;
            if (peekWait > 0 && (peekWait -= dt) <= 0) { peekWait = 0; peekT = 0; peekFrom = transform.position; }
            if (peekT < 0) return false;
            peekT += dt;
            float k;
            if (peekT < peekIn) k = Smooth(peekT / peekIn);
            else if (peekT < peekIn + peekHold) k = 1;
            else if (peekT < peekIn + peekHold + peekOut) k = 1 - Smooth((peekT - peekIn - peekHold) / peekOut);
            else { transform.position = peekFrom; peekT = -1; return false; }
            transform.position = Vector3.Lerp(peekFrom, peekTo, k);
            return true;
        }
        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

        public void CenterOn(float x, float y) { CancelPeek(); var c = World.ToUnity(x, y); transform.position = new Vector3(c.x, c.y, transform.position.z); }
    }
}
