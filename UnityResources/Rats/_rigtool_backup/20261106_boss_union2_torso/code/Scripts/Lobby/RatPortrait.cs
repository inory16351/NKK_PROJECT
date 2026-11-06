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
            if (e == null) return;
            if (isSingle) { Put(single, e.single, Vector2.zero, 1, tint); Fit(); if (scr) ShowScreen(e, tint); return; }

            if (e.headOnly && e.head)          // 머리만 있는 쥐 (나비쥐): 머리 가운데 + 꼬리 + 날개 (귀)
            {
                float hw = e.head.rect.width, hh = e.head.rect.height;
                Put(head, e.head, Vector2.zero, 1, tint, true);
                Put(tail, e.tail, new Vector2(-hw * 0.42f, -hh * 0.12f), 1, tint);
                tail.rectTransform.localRotation = Quaternion.identity;
                Fit(); return;
            }
            float tw = e.torso.rect.width, th = e.torso.rect.height;
            float legLenF = e.front ? e.front.rect.height * (1 - PivotTL(e.front).y) * e.legFront : 0;
            float torsoX = -tw * 0.5f, torsoY = -(e.shoulder.y * th + legLenF);
            Vector2 Place(Vector2 a) => new(torsoX + a.x * tw, torsoY + a.y * th);
            Vector2 hip = Place(e.hip), neck = Place(e.neck), tailA = Place(e.tailAnchor), shoulder = Place(e.shoulder);
            float headScale = e.head ? Mathf.Min(1, headWidthRatio * tw / e.head.rect.width) : 1, gap = tw * farGap;
            var far = tint * farLegColor; far.a = tint.a;
            Put(farBack, e.back, L(hip + new Vector2(gap, 0)), e.legBack, far);
            Put(farFront, e.front, L(shoulder + new Vector2(gap, 0)), e.legFront, far);
            Put(tail, e.tail, L(tailA), 1, tint);
            Put(torso, e.torso, L(new Vector2(0, torsoY + th * 0.5f)), 1, tint, true);
            Put(back, e.back, L(hip), e.legBack, tint);
            Put(front, e.front, L(shoulder), e.legFront, tint);
            Put(head, e.head, L(neck), headScale, tint);
            Fit();
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

        // 보이는 파츠 전체 테두리를 이 칸 안에 맞춤
        void Fit()
        {
            if (!visual) return;
            visual.localScale = Vector3.one; visual.anchoredPosition = Vector2.zero;
            Vector2 mn = new(float.MaxValue, float.MaxValue), mx = new(float.MinValue, float.MinValue);
            var corners = new Vector3[4];
            foreach (var img in new[] { farBack, farFront, tail, torso, back, front, head, single })
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
