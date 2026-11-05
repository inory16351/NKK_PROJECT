using NKK.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Rats
{
    // 클릭 총공격 UI (RatManager 가 전투 씬에서 붙임)
    //  ① 쿨타임 게이지: 마우스 포인터를 둘러싼 원. 총공격 직후 빈 원 → 12시부터 시계 방향으로 주황이 참 → 다 차면 치즈색으로 '통' 튀고 사라짐
    //  ② 클릭 위치 표시: 누른 곳으로 화살표 4개가 모여든 뒤, 가운데 납작 원이 돌진 동안 깜빡임 (바닥 기울기 World.TILT)
    // 그림 = Resources/RushUI/ (Codex, UnityResources/Rats/RushUI/)
    public class RushUI : MonoBehaviour
    {
        [Header("쿨타임 게이지")]
        [Tooltip("원 지름 = 커서 크기 × 이 배율")] public float ringScale = 1.75f;
        [Tooltip("다 찼을 때 '통' 튀는 시간 (초)")] public float popTime = 0.45f;
        [Tooltip("원 중심 = 커서 끝점에서 커서 크기 × 이 비율만큼 오른쪽 아래 (커서 그림 한가운데)")] public float centerK = 0.45f;
        [Tooltip("클릭하고 이 초 뒤에 빈 원이 나와 남은 쿨타임 동안 참")] public float gaugeDelay = 0.5f;

        [Header("클릭 위치 표시 (게임 단위)")]
        [Tooltip("화살표가 출발하는 거리 → 모인 거리")] public float arrowFrom = 126, arrowTo = 31.5f;
        [Tooltip("화살표 길이")] public float arrowSize = 40.5f;
        [Tooltip("화살표가 모여드는 시간 (초)")] public float arrowIn = 0.32f;
        [Tooltip("화살표가 남아 있는 시간 (초)")] public float arrowLife = 0.62f;
        [Tooltip("가운데 원 지름")] public float centerSize = 54;
        [Tooltip("클릭 표시 전체가 보이는 시간 (초, 돌진 시간보다 길면 돌진 끝까지)")] public float markTime = 1f;

        RatManager rm;
        Sprite sFrame, sFill, sReady, sKnob, sArrow, sCenter;
        RectTransform ring; Image fill, ready, knob;
        Transform mark; SpriteRenderer[] arrows = new SpriteRenderer[4]; SpriteRenderer center;
        bool wasCooling; float popT = -1;
        CameraController camCtl;

        public static RushUI Attach(RatManager rm, SpriteRenderer oldMarker)
        {
            var go = new GameObject("RushUI");
            var ui = go.AddComponent<RushUI>();
            ui.rm = rm;
            ui.Build(oldMarker);
            return ui;
        }

        static Sprite Load(string n) => Resources.Load<Sprite>("RushUI/" + n);

        void Build(SpriteRenderer oldMarker)
        {
            sFrame = Load("rush_ring_frame"); sFill = Load("rush_ring_fill"); sReady = Load("rush_ring_ready");
            sKnob = Load("rush_knob"); sArrow = Load("rush_arrow"); sCenter = Load("rush_center");

            // ① 화면 맨 위 캔버스 (커서 바로 아래)
            var cgo = new GameObject("RushGauge", typeof(Canvas));
            cgo.transform.SetParent(transform, false);
            var cv = cgo.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 32000;
            ring = new GameObject("Ring", typeof(RectTransform)).GetComponent<RectTransform>();
            ring.SetParent(cgo.transform, false); ring.anchorMin = ring.anchorMax = Vector2.zero;
            Img(ring, "Frame", sFrame);
            fill = Img(ring, "Fill", sFill); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top; fill.fillClockwise = true;
            ready = Img(ring, "Ready", sReady);
            knob = Img(ring, "Knob", sKnob); knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = knob.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ring.gameObject.SetActive(false);

            // ② 바닥에 깔리는 표시 (예전 rushMarker 와 같은 그리기 순서)
            mark = new GameObject("RushMark").transform; mark.SetParent(transform, false);
            center = Spr("Center", sCenter, oldMarker);
            for (int i = 0; i < 4; i++) arrows[i] = Spr("Arrow" + i, sArrow, oldMarker);
            if (oldMarker) oldMarker.enabled = false;
            mark.gameObject.SetActive(false);
        }

        Image Img(RectTransform parent, string n, Sprite s)
        {
            var rt = new GameObject(n, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var im = rt.GetComponent<Image>(); im.sprite = s; im.raycastTarget = false; im.preserveAspect = true;
            return im;
        }

        SpriteRenderer Spr(string n, Sprite s, SpriteRenderer like)
        {
            var sr = new GameObject(n).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(mark, false); sr.sprite = s;
            if (like) { sr.sortingLayerID = like.sortingLayerID; sr.sortingOrder = like.sortingOrder; sr.sharedMaterial = like.sharedMaterial; }
            return sr;
        }

        void LateUpdate()
        {
            if (!rm) { Destroy(gameObject); return; }
            UpdateGauge();
            UpdateMark();
        }

        // ── ① 쿨타임 게이지
        void UpdateGauge()
        {
            var m = UnityEngine.InputSystem.Mouse.current;
            float span = rm.RushCdTotal - gaugeDelay;             // 원이 차는 시간 (클릭 gaugeDelay 초 뒤 ~ 다시 쓸 수 있을 때)
            bool cooling = !rm.RushReady && rm.RushCdAfter > 0 && span > 0 && rm.RushCdLeft <= span;
            if (wasCooling && !cooling && rm.RushReady) popT = 0;      // 방금 다 참 → 통!
            wasCooling = cooling;
            if (popT >= 0) popT += Time.unscaledDeltaTime;
            bool pop = popT >= 0 && popT < popTime;
            bool show = (cooling || pop) && m != null && !GameOver.Active;
            if (ring.gameObject.activeSelf != show) ring.gameObject.SetActive(show);
            if (!show) return;

            float px = CursorSkin.PixelSize, d = px * ringScale;
            Vector2 mp = m.position.ReadValue();
            ring.anchoredPosition = mp + new Vector2(px * centerK, -px * centerK);
            ring.sizeDelta = new Vector2(d, d);

            if (pop)
            {
                float k = popT / popTime;
                ring.localScale = Vector3.one * (1 + 0.25f * Mathf.Sin(Mathf.Min(k * 1.6f, 1) * Mathf.PI));
                float a = 1 - Mathf.Clamp01((k - 0.45f) / 0.55f);
                SetA(a); fill.enabled = false; ready.enabled = true; knob.enabled = false;
                return;
            }
            float f = Mathf.Clamp01(1 - rm.RushCdLeft / span);                     // 빈 원 → 남은 쿨타임 동안 참
            ring.localScale = Vector3.one; SetA(1);
            fill.enabled = f > 0.001f; fill.fillAmount = f; ready.enabled = false;
            knob.enabled = f > 0.001f;
            if (knob.enabled)
            {
                // 진행 끝머리 동그란 점 (원 띠 한가운데)
                float ang = Mathf.PI / 2 - f * Mathf.PI * 2, r = d * 0.5f * 0.86f, ks = d * 0.16f;
                knob.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                knob.rectTransform.sizeDelta = new Vector2(ks, ks);
            }
        }

        void SetA(float a)
        {
            foreach (var im in new[] { fill, ready, knob }) { var c = im.color; c.a = a; im.color = c; }
            var fr = ring.GetChild(0).GetComponent<Image>(); var fc = fr.color; fc.a = a; fr.color = fc;
        }

        // ── ② 클릭 위치 표시
        void UpdateMark()
        {
            float t = rm.RushTime - rm.RushLeft, rt = Mathf.Min(markTime, rm.RushTime);     // t = 돌진 시작부터, rt = 표시가 끝나는 때
            bool on = rm.RushActive && t < rt;
            if (mark.gameObject.activeSelf != on) mark.gameObject.SetActive(on);
            if (!on) return;
            mark.position = World.ToUnity(rm.RushPoint.x, rm.RushPoint.y);
            // 마우스 휠로 넓게 보면 그만큼 키움 → 화면에서는 늘 같은 크기 (기본 시야 = 카메라 viewWidth)
            float zk = 1;
            var cam = Camera.main;
            if (cam && cam.orthographic)
            {
                if (!camCtl) camCtl = cam.GetComponent<CameraController>();
                float vw = camCtl ? camCtl.viewWidth : 1280;
                zk = cam.orthographicSize * 2 * cam.aspect / (vw * World.U);
            }
            mark.localScale = new Vector3(zk, zk * World.TILT, 1);

            // 가운데 납작 원: 모인 뒤 나타나 깜빡, 표시 끝(markTime) 0.3초 전부터 사라짐
            float q = Mathf.Clamp01((t - arrowIn * 0.7f) / 0.15f), fade = t > rt - 0.3f ? Mathf.Clamp01((rt - t) / 0.3f) : 1;
            float pulse = 0.65f + 0.35f * Mathf.Sin(t * 12);
            Size(center, centerSize * (1 + 0.25f * (1 - q)) * World.U);
            center.transform.localPosition = Vector3.zero;
            center.color = new Color(1, 1, 1, q * fade * pulse);

            // 화살표 4개: 바깥에서 빠르게 모인 뒤 살짝 통통, 그리고 사라짐
            bool av = t < arrowLife;
            float p = Mathf.Clamp01(t / arrowIn), e = 1 - Mathf.Pow(1 - p, 3);
            float dist = p < 1 ? Mathf.Lerp(arrowFrom, arrowTo, e) : arrowTo + arrowSize / 6 * Mathf.Sin((t - arrowIn) / (arrowLife - arrowIn) * Mathf.PI);
            float a = t < 0.06f ? t / 0.06f : (t > arrowLife - 0.15f ? (arrowLife - t) / 0.15f : 1);
            for (int i = 0; i < 4; i++)
            {
                var sr = arrows[i]; sr.enabled = av; if (!av) continue;
                float th = i * 90 * Mathf.Deg2Rad;
                // 그림 = 왼쪽을 가리키는 화살표. 끝(왼쪽 끝)이 dist 에 오도록 그림 반 길이만큼 바깥으로
                float half = arrowSize * 0.5f;
                sr.transform.localPosition = new Vector3(Mathf.Cos(th), Mathf.Sin(th), 0) * (dist + half) * World.U;
                sr.transform.localRotation = Quaternion.Euler(0, 0, i * 90);
                Size(sr, arrowSize * World.U);
                sr.color = new Color(1, 1, 1, Mathf.Clamp01(a));
            }
        }

        static void Size(SpriteRenderer sr, float w)
        {
            if (!sr.sprite) return;
            float s = w / sr.sprite.bounds.size.x;
            sr.transform.localScale = new Vector3(s, s, 1);
        }
    }
}
