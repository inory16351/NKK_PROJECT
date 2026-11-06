using System.Collections.Generic;
using NKK.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 도토리 상점 구매 확인 창의 '사기 전 미리보기' (2026-11-06 사용자, 웹 시안 B '밀어서 비교').
    // 사기를 누르면 아지트(또는 뒷골목)를 그 장식으로 잠깐 바꿔 찍고 같은 프레임 안에서 되돌림 (저장 안 함 · 화면에는 안 보임).
    // 지금 모습 위에 산 뒤 모습을 겹치고, 가운데 막대를 좌우로 밀면 왼쪽 = 지금 · 오른쪽 = 산 뒤. 처음 열릴 때 막대가 살짝 흔들림.
    // 찍는 곳: 배경 = 화면 전체 · 물건 하나 = 그 물건 둘레 (focusWidth · focusHeight) · UI 버튼 = 지금 화면 + 견본 버튼 · 커서 = 화면 가운데 + 커서 그림 · 쥐 친구 = 그 친구 둘레.
    public class ShopPreview : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public LobbyHome home;
        public LobbyPan pan;
        [Tooltip("산 뒤 그림 (꽉 채움, 밀기 입력도 받음)")] public RawImage after;
        [Tooltip("지금 그림을 자르는 칸 (RectMask2D, 왼쪽에 붙음 · 폭이 바뀜)")] public RectTransform beforeClip;
        [Tooltip("지금 그림 (자르는 칸 안, 폭 = 전체 폭)")] public RawImage before;
        [Tooltip("가운데 막대 · 손잡이 (가로 위치만 바뀜)")] public RectTransform line, knob;
        [Tooltip("커서 장식: 그림 위 커서 (평소 · 누름) — 지금 / 산 뒤")] public RawImage[] cursorBefore = new RawImage[0], cursorAfter = new RawImage[0];
        [Tooltip("UI 버튼 장식: 그림 위 견본 버튼 묶음 (그림 = UI 키트 원래 그림) — 지금 / 산 뒤")] public RectTransform uiBefore, uiAfter;
        public Vector2Int texSize = new(1280, 720);
        [Tooltip("물건을 바꾸는 장식: 보여 줄 폭 = 물건 폭 × 이 값 · 물건 높이 × 이 값 중 큰 쪽 (최소 = 배경 폭 × focusMin)")] public float focusWidth = 1.3f, focusHeight = 1.6f, focusMin = 0.4f;
        [Tooltip("커서 장식: 화면 폭의 몇 배를 보여 줄지")] public float cursorView = 0.4f;
        [Tooltip("처음 흔들기: 시간 (초) · 폭 (0~0.5)")] public float introTime = 1.1f, introAmp = 0.22f;

        RenderTexture rtBefore, rtAfter;
        Camera cam;
        float f = 0.5f, introAt = -1;
        readonly Dictionary<Image, Sprite> uiBase = new();

        RectTransform RT => (RectTransform)transform;
        float Aspect => texSize.x / (float)texSize.y;

        void Awake()
        {
            if (!home) home = FindFirstObjectByType<LobbyHome>();
            if (!pan) pan = FindFirstObjectByType<LobbyPan>(FindObjectsInactive.Include);
            foreach (var g in new[] { uiBefore, uiAfter })
                if (g) foreach (var im in g.GetComponentsInChildren<Image>(true)) uiBase[im] = im.sprite;
        }

        void OnDisable() { introAt = -1; }
        void OnDestroy()
        {
            if (rtBefore) rtBefore.Release();
            if (rtAfter) rtAfter.Release();
            if (cam) Destroy(cam.gameObject);
        }

        // ── 찍기 ──
        public void Show(DecoItemRow d)
        {
            var p = Progress.I;
            if (!p || d == null || !home || !home.cam) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            Targets();
            Rect r;
            try
            {
                p.SetDecoPreview(d); Sync();
                r = Focus(d);
                Shoot(rtAfter, r);
            }
            finally { p.SetDecoPreview(null); Sync(); }
            Shoot(rtBefore, r);
            after.texture = rtAfter; before.texture = rtBefore;

            bool cur = d.category == "Cursor";
            var (bn, bp) = CursorSkin.TexturesFor(p.EquippedDeco("Cursor"));
            var (an, ap) = CursorSkin.TexturesFor(d.deco_id);
            SetCursor(cursorBefore, cur, bn, bp); SetCursor(cursorAfter, cur, an, ap);

            bool ui = d.category == "UiButton";
            SetUi(uiBefore, ui, p.EquippedDeco("UiButton")); SetUi(uiAfter, ui, d.deco_id);

            Canvas.ForceUpdateCanvases();
            Set(0.5f); introAt = Time.unscaledTime;
        }

        static void SetCursor(RawImage[] ims, bool on, Texture2D n, Texture2D pr)
        {
            for (int i = 0; i < ims.Length; i++) if (ims[i])
            {
                ims[i].gameObject.SetActive(on && (i == 0 ? n : pr));
                if (on) ims[i].texture = i == 0 ? n : pr;
            }
        }

        void SetUi(RectTransform g, bool on, int deco)
        {
            if (!g) return;
            g.gameObject.SetActive(on);
            if (on) foreach (var im in g.GetComponentsInChildren<Image>(true)) if (uiBase.TryGetValue(im, out var s) && s) im.sprite = UiDecoSkin.SpriteFor(s, deco);
        }

        void Targets()
        {
            if (!rtBefore) { rtBefore = new RenderTexture(texSize.x, texSize.y, 24) { name = "ShopPreviewBefore" }; rtBefore.Create(); }
            if (!rtAfter) { rtAfter = new RenderTexture(texSize.x, texSize.y, 24) { name = "ShopPreviewAfter" }; rtAfter.Create(); }
            if (!cam)
            {
                var go = new GameObject("ShopPreviewCam") { hideFlags = HideFlags.HideAndDontSave };
                cam = go.AddComponent<Camera>();
            }
            cam.CopyFrom(home.cam);
            cam.enabled = false;
            cam.orthographic = true;
        }

        void Shoot(RenderTexture rt, Rect r)
        {
            cam.transform.SetPositionAndRotation(new Vector3(r.center.x, r.center.y, home.cam.transform.position.z), home.cam.transform.rotation);
            cam.orthographicSize = r.height / 2;
            cam.targetTexture = rt;
            cam.aspect = rt.width / (float)rt.height;
            cam.Render();
            cam.targetTexture = null;
        }

        // 장식 적용(미리보기 포함)을 지금 바로 화면 물건에 반영 — 원래는 Update · 페이지를 닫을 때 반영되는 것들
        void Sync()
        {
            if (home) home.Refresh();
            if (pan) pan.ApplyManhole();
            foreach (var w in FindObjectsByType<LobbyWitch>(FindObjectsSortMode.None)) w.PreviewSync();
            foreach (var h in FindObjectsByType<LobbyHamster>(FindObjectsSortMode.None)) h.PreviewSync();
            foreach (var b in FindObjectsByType<AlleyBat>(FindObjectsSortMode.None)) b.PreviewSync();
            UiDecoSkin.ApplyNow();
        }

        // ── 어디를 찍을지 (월드 사각형, 비율 = 그림 비율) ──
        Rect Focus(DecoItemRow d)
        {
            switch (d.category)
            {
                case "LobbyBg": return Whole(home.background.bounds);
                case "AlleyBg": return pan && pan.roomBackground ? Whole(pan.roomBackground.bounds) : View(1);
                case "UiButton": return View(1);
                case "Cursor": return View(cursorView);
                case "NapBed": return Around(Union(home.propBed));
                case "Table": return Around(Union(home.propTable));
                case "Bookshelf": return Around(Union(home.propBooks));
                case "CheeseStore": return Around(Union(home.propShelf));
                case "Badge": return Around(Union(home.badges));
                case "Wheel": return Around(Union(home.wheel, home.wheelRing));
                case "Manhole": return Around(Union(pan ? pan.manhole : null));
                case "RatFriend": return Around(Friend(d.deco_id));
            }
            var rs = new List<Renderer>();
            foreach (var s in FindObjectsByType<DecoSkin>(FindObjectsSortMode.None)) if (s.category == d.category) rs.AddRange(s.GetComponentsInChildren<Renderer>());
            return Around(Union(rs.ToArray()));
        }

        Bounds? Friend(int id)
        {
            var rs = new List<Renderer>();
            foreach (var w in FindObjectsByType<LobbyWitch>(FindObjectsSortMode.None)) if (w.decoId == id) rs.AddRange(w.GetComponentsInChildren<SpriteRenderer>());
            foreach (var h in FindObjectsByType<LobbyHamster>(FindObjectsSortMode.None)) if (h.decoId == id) rs.AddRange(h.GetComponentsInChildren<SpriteRenderer>());
            foreach (var b in FindObjectsByType<AlleyBat>(FindObjectsSortMode.None)) if (b.decoId == id) rs.AddRange(b.GetComponentsInChildren<SpriteRenderer>());
            return Union(rs.ToArray());
        }

        static Bounds? Union(params Renderer[] rs)
        {
            Bounds? b = null;
            if (rs != null) foreach (var r in rs)
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy || r is LineRenderer) continue;
                if (b == null) b = r.bounds; else { var x = b.Value; x.Encapsulate(r.bounds); b = x; }
            }
            return b;
        }

        // 지금 화면 (아지트 · 뒷골목 어디에 있든) 가운데 · 폭 k 배
        Rect View(float k)
        {
            var c = home.cam.transform.position;
            float h = home.cam.orthographicSize * 2 * k, w = h * Aspect;
            return new Rect(c.x - w / 2, c.y - h / 2, w, h);
        }

        // 배경판 전체 (비율에 맞춰 위아래 또는 양옆을 조금 자름)
        Rect Whole(Bounds b)
        {
            float w = b.size.x, h = w / Aspect;
            if (h > b.size.y) { h = b.size.y; w = h * Aspect; }
            return new Rect(b.center.x - w / 2, b.center.y - h / 2, w, h);
        }

        // 물건 둘레 (물건이 있는 판 = 아지트 배경 · 뒷골목 배경 밖으로는 안 나감)
        Rect Around(Bounds? ob)
        {
            if (ob == null) return View(1);
            var b = ob.Value;
            var box = Box(b.center);
            float w = Mathf.Max(b.size.x * focusWidth, b.size.y * focusHeight * Aspect, box != null ? box.Value.size.x * focusMin : 0), h = w / Aspect;
            if (box != null)
            {
                var c = box.Value;
                if (h > c.size.y) { h = c.size.y; w = h * Aspect; }
                if (w > c.size.x) { w = c.size.x; h = w / Aspect; }
                float x = Mathf.Clamp(b.center.x, c.min.x + w / 2, c.max.x - w / 2), y = Mathf.Clamp(b.center.y, c.min.y + h / 2, c.max.y - h / 2);
                return new Rect(x - w / 2, y - h / 2, w, h);
            }
            return new Rect(b.center.x - w / 2, b.center.y - h / 2, w, h);
        }

        Bounds? Box(Vector3 p)
        {
            if (pan && pan.roomBackground)
            {
                var r = pan.roomBackground.bounds; p.z = r.center.z;
                if (r.Contains(p)) return r;
            }
            return home.background ? home.background.bounds : null;
        }

        // ── 밀어서 비교 ──
        public void OnPointerDown(PointerEventData e) { introAt = -1; Drag(e); }
        public void OnDrag(PointerEventData e) { introAt = -1; Drag(e); }
        void Drag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(RT, e.position, e.pressEventCamera, out var lp)) return;
            var r = RT.rect; Set((lp.x - r.xMin) / r.width);
        }

        void Update()
        {
            if (introAt < 0) return;
            float k = (Time.unscaledTime - introAt) / Mathf.Max(0.05f, introTime);
            if (k >= 1) { introAt = -1; Set(0.5f); return; }
            Set(0.5f + introAmp * Mathf.Sin(k * Mathf.PI * 2) * (1 - k));
        }

        void Set(float v)
        {
            f = Mathf.Clamp01(v);
            float w = RT.rect.width, x = f * w;
            if (beforeClip) beforeClip.sizeDelta = new Vector2(x, beforeClip.sizeDelta.y);
            if (before) before.rectTransform.sizeDelta = new Vector2(w, before.rectTransform.sizeDelta.y);
            if (line) line.anchoredPosition = new Vector2(x, line.anchoredPosition.y);
            if (knob) knob.anchoredPosition = new Vector2(x, knob.anchoredPosition.y);
        }
    }
}
