using UnityEngine;
using UnityEngine.UI;

namespace NKK.Ults
{
    /// <summary>
    /// 초필살기 컷신 속 쥐 리그 (2026-10-19 사용자: 쥐가 리깅 데이터로 역동적으로 움직이게). 게임 RatRig 과 같은 구조를 UI 로:
    /// 몸통에 관절(목 · 어깨 · 엉덩이 · 꼬리 뿌리)을 두고 머리 · 앞다리 · 뒷다리(+ 먼 쪽 둘) · 꼬리 · 휘날리는 망토를 붙여 각도로 움직임.
    /// 그림 = 컷신용 고해상도 파츠 (Art/Rats/SuperUlt/&lt;code&gt;/rig_*), 쥐는 왼쪽을 봄 (face 1 = 오른쪽을 보게 뒤집음).
    /// 동작 = moves (시간 순서): 그 시각부터 그 동작, 앞 동작에서 blend 초 동안 섞임. 자리 · 크기 · 투명도는 같은 오브젝트의 SuperUltLayer 키.
    /// SuperUltManager 가 매 프레임 Apply(t). 각도는 도 (+ = 반시계, 쥐가 왼쪽을 보니 앞발을 앞으로 드는 건 -).
    /// </summary>
    public class SuperUltRig : MonoBehaviour
    {
        public enum Move { Idle, Skate, Cast, Leap, Spin, Land, Point, Twirl, Dash }

        [System.Serializable]
        public class MoveKey
        {
            [Tooltip("컷신 시간 (초)")] public float t;
            public Move move;
            [Tooltip("동작 빠르기 배율")] public float speed = 1;
            [Tooltip("동작 크기 배율")] public float amount = 1;
        }

        [Header("파츠 그림 (왼쪽을 봄)")]
        public Sprite torso, head, front, back, tail, cape;
        [Header("관절: 몸통 그림 안 위치 (0~1, 왼쪽 위 기준)")]
        public Vector2 neck = new(0.12f, 0.35f), shoulder = new(0.27f, 0.62f), hip = new(0.72f, 0.6f), tailRoot = new(0.95f, 0.55f), capeRoot = new(0.2f, 0.25f);
        [Header("파츠 축: 각 그림 안 회전 중심 (0~1, 왼쪽 위 기준)")]
        public Vector2 headPivot = new(0.75f, 0.85f), frontPivot = new(0.5f, 0.05f), backPivot = new(0.5f, 0.05f), tailPivot = new(0.03f, 0.5f), capePivot = new(0.04f, 0.4f);
        [Header("크기")]
        [Tooltip("몸통 폭 (px) — 나머지 파츠는 그림 픽셀 비율 그대로")] public float torsoWidth = 420;
        [Tooltip("파츠 그림 픽셀 → 몸통 폭 기준 배율 보정 (머리 · 앞다리 · 뒷다리 · 꼬리 · 망토, slice_superult 의 rig.json)")] public float headScale = 1, frontScale = 1, backScale = 1, tailScale = 1, capeScale = 1;
        [Tooltip("1 = 오른쪽을 봄 (그림 뒤집기)")] public int face = -1;
        [Tooltip("먼 쪽 다리 색 · 간격 (몸통 폭 배율)")] public Color farColor = new(0.8f, 0.82f, 0.9f, 1);
        public float farGap = 0.07f;

        [Header("동작")]
        public MoveKey[] moves;
        [Tooltip("동작 바뀔 때 섞는 시간 (초)")] public float blend = 0.12f;
        [Tooltip("동작이 바뀌는 순간 찌그러짐 (탄력)")] public float changeSquash = 0.12f;

        RectTransform root, body, rT, rH, rF, rB, rFF, rFB, rTail, rCape;
        Image iT, iH, iF, iB, iFF, iFB, iTail, iCape;

        struct P { public float head, front, back, farFront, farBack, tail, cape, tilt, bob, sx, sy, spin; }

        // ── 조립 (씬 만들기 코드가 그림 · 관절을 넣고 부름, 다시 불러도 됨) ──
        public void Build()
        {
            root = (RectTransform)transform;
            body = Child(root, "Body");
            rFB = Part(body, "FarBack", back, out iFB); rFF = Part(body, "FarFront", front, out iFF);
            rCape = Part(body, "Cape", cape, out iCape); rTail = Part(body, "Tail", tail, out iTail);
            rT = Part(body, "Torso", torso, out iT); rB = Part(body, "Back", back, out iB); rF = Part(body, "Front", front, out iF);
            rH = Part(body, "Head", head, out iH);
            if (iFF) iFF.color = farColor; if (iFB) iFB.color = farColor;
            if (!torso) return;
            float k = torsoWidth / torso.rect.width;           // 그림 픽셀 → px
            Vector2 tsz = torso.rect.size * k;
            // body = 엉덩이 (기울기 축). 몸통 그림 왼쪽 위 = (0,0) → body 기준 위치
            Vector2 At(Vector2 a) => new Vector2((a.x - hip.x) * tsz.x, -(a.y - hip.y) * tsz.y);
            Size(rT, torso, k, new Vector2(0.5f, 0.5f)); rT.anchoredPosition = At(new Vector2(0.5f, 0.5f));
            Size(rH, head, k * headScale, headPivot); rH.anchoredPosition = At(neck);
            Size(rF, front, k * frontScale, frontPivot); rF.anchoredPosition = At(shoulder);
            Size(rFF, front, k * frontScale, frontPivot); rFF.anchoredPosition = At(shoulder) + new Vector2(tsz.x * farGap, 0);
            Size(rB, back, k * backScale, backPivot); rB.anchoredPosition = At(hip);
            Size(rFB, back, k * backScale, backPivot); rFB.anchoredPosition = At(hip) + new Vector2(tsz.x * farGap, 0);
            Size(rTail, tail, k * tailScale, tailPivot); rTail.anchoredPosition = At(tailRoot);
            Size(rCape, cape, k * capeScale, capePivot); rCape.anchoredPosition = At(capeRoot);
            body.anchoredPosition = new Vector2((hip.x - 0.5f) * tsz.x, -(hip.y - 0.5f) * tsz.y);      // 몸통 가운데 = 오브젝트 원점
            Apply(0);
        }

        static RectTransform Child(RectTransform p, string n)
        {
            var t = p.Find(n) as RectTransform;
            if (!t) { var g = new GameObject(n, typeof(RectTransform)); g.layer = p.gameObject.layer; t = (RectTransform)g.transform; t.SetParent(p, false); }
            return t;
        }

        static RectTransform Part(RectTransform p, string n, Sprite s, out Image im)
        {
            var t = Child(p, n);
            im = t.GetComponent<Image>(); if (!im) im = t.gameObject.AddComponent<Image>();
            im.sprite = s; im.raycastTarget = false; im.enabled = s;
            t.SetAsLastSibling();
            return t;
        }

        static void Size(RectTransform r, Sprite s, float k, Vector2 pivotTL)
        {
            if (!r || !s) return;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = s.rect.size * k;
            r.pivot = new Vector2(pivotTL.x, 1 - pivotTL.y);
        }

        // ── 동작 ──
        static P Pose(Move m, float t, float amt)
        {
            var p = new P { sx = 1, sy = 1 };
            float s = Mathf.Sin(t), c = Mathf.Cos(t);
            switch (m)
            {
                case Move.Skate:        // 얼음 위 활강: 다리를 길게 번갈아 차고, 몸은 앞으로 숙이고, 망토 · 꼬리가 크게 물결
                    {
                        float ph = t * 8;
                        p.front = Mathf.Sin(ph) * 38; p.farFront = -Mathf.Sin(ph) * 30;
                        p.back = -Mathf.Sin(ph) * 45; p.farBack = Mathf.Sin(ph) * 36;
                        p.tilt = 7 + Mathf.Sin(ph) * 3; p.bob = Mathf.Abs(Mathf.Sin(ph)) * 14;
                        p.head = 4 + Mathf.Sin(ph + 0.5f) * 5; p.tail = 12 + Mathf.Sin(ph * 0.5f) * 22; p.cape = 6 + Mathf.Sin(ph * 0.75f) * 14;
                        p.sx = 1 + Mathf.Abs(Mathf.Sin(ph)) * 0.04f;
                        break;
                    }
                case Move.Cast:         // 앞발을 하늘로 쭉 · 고개를 들고 · 몸을 젖힘 (얼음 마법)
                    p.front = -135 + s * 6; p.farFront = -110 + c * 6; p.back = 12; p.farBack = -6;
                    p.tilt = -12 + s * 2; p.head = -16; p.tail = 30 + Mathf.Sin(t * 3) * 14; p.cape = 22 + Mathf.Sin(t * 2.4f) * 12;
                    p.sy = 1.06f; p.sx = 0.96f; p.bob = 10;
                    break;
                case Move.Leap:         // 높이 도약: 다리를 접고 몸을 쭉 늘림
                    p.front = -70; p.farFront = -55; p.back = 70; p.farBack = 55;
                    p.tilt = -22; p.head = -10; p.tail = 45 + s * 10; p.cape = 40 + Mathf.Sin(t * 3) * 10;
                    p.sx = 1.12f; p.sy = 0.92f; p.bob = 30;
                    break;
                case Move.Spin:         // 공중 회전 (망토가 펼쳐짐)
                    p.front = -60; p.farFront = -40; p.back = 50; p.farBack = 40;
                    p.spin = -t * 57.3f * 12; p.tail = 50; p.cape = 55 + Mathf.Sin(t * 4) * 8; p.sx = 0.95f; p.sy = 1.05f;
                    break;
                case Move.Land:         // 착지: 쿵 찌그러졌다 펴짐
                    {
                        float k = Mathf.Exp(-t * 1.6f);
                        p.front = 25 * k; p.farFront = 18 * k; p.back = -28 * k; p.farBack = -20 * k;
                        p.sx = 1 + 0.2f * k * Mathf.Cos(t * 1.2f); p.sy = 1 - 0.18f * k * Mathf.Cos(t * 1.2f); p.bob = -18 * k;
                        p.tail = -10 + 20 * k; p.cape = -5 + 25 * k; p.head = 8 * k;
                        break;
                    }
                case Move.Point:        // 앞발로 아래를 가리킴 (명령)
                    p.front = -80 + s * 3; p.farFront = -20; p.back = -8; p.farBack = 6;
                    p.tilt = 6; p.head = 10; p.tail = 18 + Mathf.Sin(t * 2.5f) * 10; p.cape = 12 + Mathf.Sin(t * 2) * 10; p.bob = 4;
                    break;
                case Move.Twirl:        // 제자리에서 우아하게 빙그르 (몸 좌우 뒤집힘처럼 sx 가 흔들림)
                    p.sx = Mathf.Cos(t * 1.5f); p.front = -40; p.farFront = -60; p.back = 10; p.tail = 30 + s * 20; p.cape = 35 + Mathf.Sin(t * 3) * 15; p.head = -8;
                    break;
                case Move.Dash:         // 앞으로 돌진: 앞으로 크게 숙이고 다리는 뒤로
                    p.front = 55; p.farFront = 45; p.back = 60; p.farBack = 50;
                    p.tilt = 18; p.head = 10; p.tail = 5 + Mathf.Sin(t * 6) * 8; p.cape = 0 + Mathf.Sin(t * 5) * 10; p.sx = 1.12f; p.sy = 0.94f;
                    break;
                default:                // 숨쉬기
                    p.bob = Mathf.Sin(t * 1.3f) * 4; p.head = Mathf.Sin(t * 0.9f) * 4; p.tail = 10 + Mathf.Sin(t * 1.1f) * 12; p.cape = 8 + Mathf.Sin(t * 1.4f) * 6;
                    p.front = Mathf.Sin(t) * 4; p.back = -Mathf.Sin(t) * 3;
                    break;
            }
            p.head *= amt; p.front *= amt; p.back *= amt; p.farFront *= amt; p.farBack *= amt; p.tail *= amt; p.cape *= amt; p.tilt *= amt; p.bob *= amt; p.spin *= amt;
            p.sx = 1 + (p.sx - 1) * amt; p.sy = 1 + (p.sy - 1) * amt;
            return p;
        }

        static P Lerp(P a, P b, float k)
        {
            float L(float x, float y) => Mathf.LerpUnclamped(x, y, k);
            return new P { head = L(a.head, b.head), front = L(a.front, b.front), back = L(a.back, b.back), farFront = L(a.farFront, b.farFront), farBack = L(a.farBack, b.farBack),
                tail = L(a.tail, b.tail), cape = L(a.cape, b.cape), tilt = L(a.tilt, b.tilt), bob = L(a.bob, b.bob), sx = L(a.sx, b.sx), sy = L(a.sy, b.sy), spin = L(a.spin, b.spin) };
        }

        P At(int i, float t)
        {
            var m = moves[i];
            float lt = (t - m.t) * Mathf.PI * 2 * 0.5f * Mathf.Max(0.05f, m.speed);    // 동작 안 시간 (라디안 위상 기준 0.5Hz × speed)
            return Pose(m.move, lt, m.amount);
        }

        public void Apply(float t)
        {
            if (!body) { root = (RectTransform)transform; body = root.Find("Body") as RectTransform; if (!body) return; Cache(); }
            P p;
            if (moves == null || moves.Length == 0) p = Pose(Move.Idle, t * Mathf.PI, 1);
            else
            {
                int i = 0; while (i + 1 < moves.Length && t >= moves[i + 1].t) i++;
                p = At(i, t);
                float since = t - moves[i].t;
                if (i > 0 && since < blend)
                {
                    float k = since / Mathf.Max(0.001f, blend); k = 1 - (1 - k) * (1 - k);
                    p = Lerp(At(i - 1, t), p, k);
                }
                if (since >= 0 && since < 0.2f && i > 0) { float q = Mathf.Sin(since / 0.2f * Mathf.PI) * changeSquash; p.sx *= 1 + q; p.sy *= 1 - q; }
            }
            float f = face > 0 ? -1 : 1;
            body.localScale = new Vector3(f * p.sx, p.sy, 1);
            body.localRotation = Quaternion.Euler(0, 0, f * (-p.tilt + p.spin));
            var bp = body.anchoredPosition; body.anchoredPosition = new Vector2(bp.x, BaseY + p.bob);
            R(rH, -p.head); R(rF, p.front); R(rFF, p.farFront); R(rB, p.back); R(rFB, p.farBack); R(rTail, p.tail); R(rCape, p.cape);
        }

        float baseY = float.NaN;
        float BaseY { get { if (float.IsNaN(baseY)) baseY = body.anchoredPosition.y; return baseY; } }

        void Cache()
        {
            rT = body.Find("Torso") as RectTransform; rH = body.Find("Head") as RectTransform; rF = body.Find("Front") as RectTransform; rB = body.Find("Back") as RectTransform;
            rFF = body.Find("FarFront") as RectTransform; rFB = body.Find("FarBack") as RectTransform; rTail = body.Find("Tail") as RectTransform; rCape = body.Find("Cape") as RectTransform;
        }

        static void R(RectTransform r, float deg) { if (r) r.localRotation = Quaternion.Euler(0, 0, deg); }
    }
}
