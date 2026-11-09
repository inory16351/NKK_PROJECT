using NKK.Rats;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // UI 용 쥐 그림 (웹 UI.ratPic): 5파츠를 RatRig 와 같은 관절 위치로 UI Image 에 조립하고, 이 칸 크기에 맞춰 가운데 놓음.
    // 자식 Image 들은 하이라키에 미리 있음 (visual 아래: FarBack · FarFront · Tail · Torso · Back · Front · Head · Single — 그리는 순서대로)
    public class RatPortrait : MonoBehaviour
    {
        public RectTransform visual;
        public Image farBack, farFront, tail, torso, back, front, head, single;
        [Tooltip("먼 쪽 다리 어둡게")] public Color farLegColor = new(0.8f, 0.78f, 0.79f);
        [Tooltip("몸통 폭 대비 먼 쪽 다리 간격")] public float farGap = 0.07f;
        [Tooltip("머리 폭 상한 = 몸통 폭 × 이 값")] public float headWidthRatio = 0.62f;
        [Tooltip("칸 안 여백 비율")] public float padding = 0.08f;
        [Tooltip("오른쪽을 보게 (원본 그림은 왼쪽을 봄)")] public bool faceRight;
        [Tooltip("화면 속 쥐 (5G 휴대폰): 화면 칸 안쪽 여백 비율")] public float screenRatPadding = 0.12f;
        [HideInInspector] public bool asScreen;            // 이 그림이 휴대폰 화면 속 쥐 (파츠로 조립)
        RatPortrait screenRat;                              // 휴대폰 화면 안 쥐 (처음 필요할 때 이 칸을 복제해 single 아래에 둠)
        Image screenBgImg;
        [Tooltip("날개 (나비쥐): 그림에서 펼친 정도 (0 접힘 ~ 1 활짝)")] [Range(0, 1)] public float wingPose = 0.8f;
        // 날개 각도 = RatRig 기본값과 같음 (라디안, 위 = 0)
        const float WingClosed = 0.12f, WingOpen = 1.25f, SmallClosed = 3.0f, SmallOpen = 2.2f, WingNarrow = 0.5f;
        RectTransform wingBack, wingFront;                  // 날개 층: 먼 쪽 (꼬리 뒤) · 가까운 쪽 (머리 바로 뒤). 처음 필요할 때 visual 아래에 만듦
        readonly Image[] wings = new Image[4];              // 0 = 큰 날개 가까운 쪽 · 1 = 큰 날개 먼 쪽 · 2 = 작은 날개 가까운 쪽 · 3 = 작은 날개 먼 쪽
        bool wingsOn;
        [HideInInspector] public bool asTwin;               // 이 그림이 두 마리 쥐의 위쪽 쥐 (칸 맞춤은 아래 쥐가 함)
        RatPortrait twinTop;                                // 콩쥐팥쥐: 위에 거꾸로 붙는 쥐 (처음 필요할 때 이 칸을 복제해 visual 아래에 둠)
        public RatPortrait TwinTop => twinTop && twinTop.gameObject.activeSelf ? twinTop : null;

        static Vector2 PivotTL(Sprite s) => new(s.pivot.x / s.rect.width, 1 - s.pivot.y / s.rect.height);
        static Vector2 L(Vector2 px) => new(px.x, -px.y);       // 리그 픽셀(y 아래 +) → UI (y 위 +)

        bool shown;
        // 칸 크기가 바뀌면 (레이아웃 그룹이 나중에 크기를 정함) 다시 맞춤
        void OnRectTransformDimensionsChange() { if (shown) Fit(); }

        public void Show(RatArtLibrary.Entry e, Color tint = default)
        {
            shown = e != null;
            if (tint == default) tint = Color.white;
            bool isSingle = e != null && !asScreen && e.IsSingle;
            bool scr = isSingle && e.screen && e.torso && single;
            if (screenRat) screenRat.gameObject.SetActive(scr);
            if (screenBgImg) screenBgImg.enabled = scr && e.screenBg;
            foreach (var r in new[] { farBack, farFront, tail, torso, back, front, head }) if (r) r.enabled = e != null && !isSingle;
            if (single) single.enabled = isSingle;
            if (e == null || isSingle || !(e.headOnly && e.head)) HideWings();
            bool twin = !asTwin && e != null && !isSingle && e.Twin != null;
            if (!twin) HideTwin();
            if (e == null) return;
            if (isSingle) { Put(single, e.single, Vector2.zero, 1, tint); Fit(); if (scr) ShowScreen(e, tint); return; }

            if (e.headOnly && e.head)          // 머리만 있는 쥐 (나비쥐): 머리 가운데 + 꼬리 + 날개 (귀)
            {
                foreach (var r in new[] { farBack, farFront, torso, back, front }) if (r) r.enabled = false;      // 이 칸이 앞서 보여 준 쥐의 몸통 · 다리가 남지 않게
                float hw = e.head.rect.width, hh = e.head.rect.height;
                Put(head, e.head, Vector2.zero, 1, tint, true);
                Put(tail, e.tail, new Vector2(hw * 0.42f, -hh * 0.12f), 1, tint);      // 꼬리는 얼굴 반대쪽 (RatRig 와 같음)
                tail.rectTransform.localRotation = Quaternion.identity;
                ShowWings(e, tint);
                Fit(); return;
            }
            float tw = e.TorsoW, th = e.torso.rect.height;      // 늘린 몸통 그림은 원래 폭 기준 (torsoW)
            float legLenF = e.front ? e.front.rect.height * (1 - PivotTL(e.front).y) * e.legFront : 0;
            float torsoX = -tw * 0.5f, torsoY = -(e.shoulder.y * th + legLenF);
            Vector2 Place(Vector2 a) => new(torsoX + a.x * tw, torsoY + a.y * th);
            Vector2 hip = Place(e.hip), neck = Place(e.neck), tailA = Place(e.tailAnchor), shoulder = Place(e.shoulder);
            float headScale = e.head ? Mathf.Min(1, headWidthRatio * tw / e.head.rect.width) : 1, gap = tw * farGap;
            var far = tint * farLegColor; far.a = tint.a;
            Put(farBack, e.back, L(hip + new Vector2(gap, 0)), e.legBack, far);
            Put(farFront, e.front, L(shoulder + new Vector2(gap, 0)), e.legFront, far);
            Put(tail, e.tail, L(tailA), 1, tint);
            Put(torso, e.torso, L(new Vector2(0, torsoY + th * 0.5f)), 1, tint, e.torsoW <= 0);      // 늘린 몸통은 피벗(원래 그림 가운데) 기준
            Put(back, e.back, L(hip), e.legBack, tint);
            Put(front, e.front, L(shoulder), e.legFront, tint);
            Put(head, e.head, L(neck), headScale, tint);
            if (twin) ShowTwin(e, torsoY, tint);
            Fit();
        }

        // ── 두 마리 쥐 (콩쥐팥쥐): 위 쥐 = 쌍의 가운데 (아래 쥐 등 높이) 축으로 180° 돌린 것 (Rat.TwinSplit, twinGap 1 과 같음) ──
        void ShowTwin(RatArtLibrary.Entry e, float torsoY, Color tint)
        {
            if (!visual) return;
            HideTwin();                                          // 복제된 칸은 위 쥐가 이미 있음
            if (!twinTop)
            {
                twinTop = Instantiate(this, visual);
                twinTop.name = "TwinTop"; twinTop.asTwin = true;
                if (twinTop.single) for (int c = twinTop.single.transform.childCount - 1; c >= 0; c--) DestroyImmediate(twinTop.single.transform.GetChild(c).gameObject);      // 이 칸이 휴대폰 쥐를 보여 준 적이 있으면 그 화면은 빼고
                foreach (var g in twinTop.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            }
            twinTop.gameObject.SetActive(true);
            twinTop.Show(e.Twin, tint);
            float H = -torsoY - 4;                               // 등 높이 (그림 둘레 여백 4px 만큼 빼서 등선이 딱 닿게, RatRig.BackHeight)
            var rt = (RectTransform)twinTop.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero; rt.localScale = Vector3.one;
            rt.anchoredPosition = new Vector2(0, H * 2); rt.localRotation = Quaternion.Euler(0, 0, 180);
            float s = RigLength(e) / Mathf.Max(1, RigLength(e.Twin));      // 두 쥐 몸 길이를 게임처럼 맞춤
            twinTop.visual.localScale = new Vector3(s, s, 1); twinTop.visual.anchoredPosition = Vector2.zero;
        }

        float RigLength(RatArtLibrary.Entry e)
        {
            float tw = e.TorsoW, hs = e.headScale > 0 ? e.headScale : e.head ? Mathf.Min(1, headWidthRatio * tw / e.head.rect.width) : 1;
            return tw + (e.head ? e.head.rect.width * hs * 0.55f : 0);
        }

        void HideTwin()
        {
            if (!twinTop && visual) { var t = visual.Find("TwinTop"); if (t) twinTop = t.GetComponent<RatPortrait>(); }
            if (twinTop) twinTop.gameObject.SetActive(false);
        }

        // ── 날개 (나비쥐): RatRig.BuildWings 와 같은 자리 · 크기로, wingPose 만큼 펼친 자세로 그림 ──
        void ShowWings(RatArtLibrary.Entry e, Color tint)
        {
            if (!e.wing || !visual) { HideWings(); return; }
            wingBack = WingLayer(wingBack, "WingsBack", tail);
            wingFront = WingLayer(wingFront, "WingsFront", head);
            float o = Mathf.Clamp01(wingPose), o2 = Mathf.Clamp01(o * 0.8f + 0.1f);
            float ang = Mathf.Lerp(WingClosed, WingOpen, o) * Mathf.Rad2Deg, ang2 = Mathf.Lerp(SmallClosed, SmallOpen, o2) * Mathf.Rad2Deg;
            float nx = Mathf.Lerp(WingNarrow, 1, o), nx2 = Mathf.Lerp(WingNarrow + 0.15f, 1, o2);
            static Sprite FarOf(Sprite near, Sprite far) => near && far ? far : near;
            // 만드는 순서 = 그리는 순서 (RatRig: 먼 큰 날개 → 먼 작은 날개 → 꼬리 → 가까운 작은 날개 → 가까운 큰 날개 → 머리)
            Wing(1, wingBack, FarOf(e.wing, e.wingFar), e.wingAnchor, e.wingSize, true, ang * 0.96f, nx * 0.96f, e, tint);
            Wing(3, wingBack, FarOf(e.wing2, e.wing2Far), e.wing2Anchor, e.wing2Size, true, ang2 * 0.96f, nx2 * 0.96f, e, tint);
            Wing(2, wingFront, e.wing2, e.wing2Anchor, e.wing2Size, false, -ang2, nx2, e, tint);
            Wing(0, wingFront, e.wing, e.wingAnchor, e.wingSize, false, -ang, nx, e, tint);
            wingsOn = true;
        }

        RectTransform WingLayer(RectTransform layer, string layerName, Image before)
        {
            if (!layer) layer = visual.Find(layerName) as RectTransform;      // 복제된 칸은 층이 이미 있음
            if (!layer)
            {
                layer = new GameObject(layerName, typeof(RectTransform)).GetComponent<RectTransform>();
                layer.SetParent(visual, false);
                layer.anchorMin = layer.anchorMax = layer.pivot = new Vector2(0.5f, 0.5f);
                layer.sizeDelta = Vector2.zero;
                layer.SetSiblingIndex(before.transform.GetSiblingIndex());
            }
            layer.gameObject.SetActive(true);
            layer.localRotation = Quaternion.identity; layer.localScale = Vector3.one; layer.anchoredPosition = Vector2.zero;
            return layer;
        }

        void Wing(int i, RectTransform layer, Sprite s, Vector2 anchor, float size, bool far, float deg, float nx, RatArtLibrary.Entry e, Color tint)
        {
            var found = layer.Find("Wing" + i);
            var img = found ? found.GetComponent<Image>() : null;
            if (!img)
            {
                var go = new GameObject("Wing" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(layer, false);
                img = go.GetComponent<Image>(); img.raycastTarget = false;
            }
            wings[i] = img;
            img.enabled = s; if (!s) return;
            float hw = e.head.rect.width, hh = e.head.rect.height, k = size * hw / s.rect.height;      // 날개 키 = 머리 폭 × size
            var c = far ? Color.Lerp(Color.white, farLegColor, 0.45f) * tint : tint; c.a = tint.a;
            img.sprite = s; img.color = c; img.preserveAspect = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = s.rect.size;
            rt.pivot = new Vector2(0.5f, 0.06f);                 // 날개 밑동 = 회전축 (그림 아래 끝, 조금 안쪽)
            rt.localScale = new Vector3(k * nx, k, 1); rt.localRotation = Quaternion.Euler(0, 0, deg);
            float ax = far ? 1 - anchor.x : anchor.x;            // 먼 쪽 날개는 머리 반대쪽 (좌우 대칭)
            rt.anchoredPosition = new Vector2(ax * hw - hw * 0.5f, (1 - anchor.y) * hh - hh * 0.5f);
        }

        void HideWings()
        {
            wingsOn = false;
            if (!visual) return;
            if (!wingBack) wingBack = visual.Find("WingsBack") as RectTransform;
            if (!wingFront) wingFront = visual.Find("WingsFront") as RectTransform;
            if (wingBack) wingBack.gameObject.SetActive(false);
            if (wingFront) wingFront.gameObject.SetActive(false);
        }

        // 연출용 (BirthCutscene): 날개도 머리를 따라 기울고 같이 흐려짐
        public void RotateWings(float deg)
        {
            if (!wingsOn) return;
            var q = Quaternion.Euler(0, 0, deg);
            wingBack.localRotation = q; wingFront.localRotation = q;
        }
        public void SetWingsAlpha(float a)
        {
            foreach (var i in wings) if (i) { var c = i.color; c.a = a; i.color = c; }
        }

        // 휴대폰 화면 칸에 배경 + 파츠 쥐 (화면 칸에 꽉 맞춰 가운데)
        void ShowScreen(RatArtLibrary.Entry e, Color tint)
        {
            var sr = single.rectTransform;
            Vector2 a = e.screenRect.min, b = e.screenRect.max;
            // 화면 쥐를 먼저 복제 (배경 자식이 생기기 전에, 복제본에 배경이 딸려 가지 않게)
            if (!screenRat)
            {
                screenRat = Instantiate(this, sr);
                screenRat.name = "ScreenRat"; screenRat.asScreen = true; screenRat.faceRight = false;
                foreach (var g in screenRat.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            }
            if (!screenBgImg)
            {
                var go = new GameObject("ScreenBg", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(sr, false);
                screenBgImg = go.GetComponent<Image>(); screenBgImg.raycastTarget = false;
            }
            Stretch(screenBgImg.rectTransform, a, b);
            screenBgImg.sprite = e.screenBg; screenBgImg.enabled = e.screenBg; screenBgImg.color = tint;
            screenBgImg.transform.SetAsFirstSibling();
            screenRat.transform.SetAsLastSibling();
            float pad = screenRatPadding;
            Stretch((RectTransform)screenRat.transform, a + new Vector2(e.screenRect.width, e.screenRect.height) * pad, b - new Vector2(e.screenRect.width, e.screenRect.height) * pad);
            screenRat.gameObject.SetActive(true);
            screenRat.Show(e, tint);
        }

        static void Stretch(RectTransform rt, Vector2 a, Vector2 b)
        {
            rt.anchorMin = a; rt.anchorMax = b; rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f); rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
        }

        // center = true 면 스프라이트 가운데를 pos 에 (몸통), 아니면 스프라이트 피벗(관절)을 pos 에
        static void Put(Image img, Sprite s, Vector2 pos, float scale, Color c, bool center = false)
        {
            if (!img) return;
            img.enabled = s; if (!s) return;
            img.sprite = s; img.color = c; img.preserveAspect = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = s.rect.size;
            rt.pivot = center ? new Vector2(0.5f, 0.5f) : new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height);
            rt.localScale = Vector3.one * scale; rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = pos;
        }

        // 칸 맞춤에 넣을 그림 전부 (날개 · 위쪽 쥐 포함)
        System.Collections.Generic.List<Image> FitParts()
        {
            var l = new System.Collections.Generic.List<Image> { farBack, farFront, tail, torso, back, front, head, single };
            if (wingsOn) l.AddRange(wings);
            var top = TwinTop;
            if (top) l.AddRange(new[] { top.farBack, top.farFront, top.tail, top.torso, top.back, top.front, top.head, top.single });
            return l;
        }

        // 보이는 파츠 전체 테두리를 이 칸 안에 맞춤
        void Fit()
        {
            if (!visual) return;
            if (asTwin) return;                                  // 위쪽 쥐는 아래 쥐가 함께 맞춤
            visual.localScale = Vector3.one; visual.anchoredPosition = Vector2.zero;
            Vector2 mn = new(float.MaxValue, float.MaxValue), mx = new(float.MinValue, float.MinValue);
            var corners = new Vector3[4];
            foreach (var img in FitParts())
            {
                if (!img || !img.enabled || !img.sprite) continue;
                img.rectTransform.GetLocalCorners(corners);
                foreach (var c in corners)
                {
                    var p = visual.InverseTransformPoint(img.rectTransform.TransformPoint(c));
                    mn = Vector2.Min(mn, p); mx = Vector2.Max(mx, p);
                }
            }
            if (mn.x > mx.x) return;
            var box = ((RectTransform)transform).rect.size * (1 - padding * 2);
            var size = mx - mn;
            float k = Mathf.Min(box.x / Mathf.Max(1, size.x), box.y / Mathf.Max(1, size.y));
            visual.localScale = new Vector3(faceRight ? -k : k, k, 1);
            var mid = (mn + mx) * 0.5f;
            visual.anchoredPosition = new Vector2(-mid.x * (faceRight ? -k : k), -mid.y * k);
        }
    }
}
