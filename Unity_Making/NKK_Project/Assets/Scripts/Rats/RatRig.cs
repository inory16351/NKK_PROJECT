using UnityEngine;
using UnityEngine.Rendering;

namespace NKK.Rats
{
    // 쥐 컷아웃 리그: 머리·몸통·꼬리·앞다리·뒷다리(+ 먼 쪽 다리 2개)를 관절에 붙여 조립 (웹게임 buildRatRig/drawRatRig 이식).
    // 리그 좌표 = 파츠 원본 픽셀 / 100 (스프라이트 PPU 100), 발끝 = 원점, 쥐는 왼쪽을 봄.
    [RequireComponent(typeof(SortingGroup))]
    public class RatRig : MonoBehaviour
    {
        [Header("파츠 (프리팹 자식)")]
        public Transform visual;      // 좌우 반전·크기
        public Transform body;        // 엉덩이(hip) 기준 기울기
        public SpriteRenderer farBack, farFront, tail, torso, back, front, head, single;

        [Header("모양")]
        [Tooltip("먼 쪽 다리 어둡게")] public Color farLegColor = new(0.8f, 0.78f, 0.79f);
        [Tooltip("몸통 폭 대비 먼 쪽 다리 간격")] public float farGap = 0.07f;
        [Tooltip("머리 폭 상한 = 몸통 폭 × 이 값")] public float headWidthRatio = 0.62f;

        public float Unit { get; private set; }        // 리그 1 픽셀 = 게임 단위
        public bool IsSingle => single && single.enabled;

        Vector2 hipPx, neckPx, tailPx, shoulderPx;
        float legF = 1, legB = 1, headScale = 1;
        Sprite artHead;
        [Tooltip("머리 바꾸기 (보스 효과) 그림 크기 배율 · 목에서 앞으로 (원래 머리 폭 배율)")] public float headSwapScale = 1.5f, headSwapFwd = 0.35f;
        [HideInInspector] public Sprite headReplace;       // 같은 크기·방향의 다른 머리 그림 (보스 눈 레이저 때 빨간 눈 등, Boss 가 매 프레임 정함)
        [HideInInspector] public Sprite headOverride;      // 보스 효과로 바꾼 머리 (오른쪽을 보는 그림, Rat.Boss 가 매 프레임 정함)
        SortingGroup group;

        static Vector3 L(Vector2 px) => new(px.x / 100f, -px.y / 100f, 0);    // 리그 픽셀(y 아래 +) → 로컬

        // 파츠 피벗(스프라이트 좌하단 기준) → 좌상단 기준 0~1
        static Vector2 PivotTL(Sprite s) => new(s.pivot.x / s.rect.width, 1 - s.pivot.y / s.rect.height);

        bool legsBehind;

        // headScaleOverride > 0 이면 머리 크기 고정 (고양이), legsBehind = 가까운 다리도 몸통 뒤 (고양이)
        public void Build(RatArtLibrary.Entry e, float rigLength, float headScaleOverride = -1, bool legsBehind = false)
        {
            group = GetComponent<SortingGroup>();
            bool isSingle = e.torso == null && e.single != null;
            foreach (var r in new[] { farBack, farFront, tail, torso, back, front, head }) if (r) r.enabled = !isSingle;
            if (single) single.enabled = isSingle;
            if (isSingle)
            {
                single.sprite = e.single;
                float w = e.single.rect.width;
                Unit = rigLength / w;
                single.transform.localPosition = new Vector3(0, (e.single.rect.height * 0.5f) / 100f, 0);
                body.localPosition = Vector3.zero;
                return;
            }

            torso.sprite = e.torso; head.sprite = artHead = e.head; tail.sprite = e.tail;
            front.sprite = farFront.sprite = e.front; back.sprite = farBack.sprite = e.back;
            farFront.color = farBack.color = farLegColor;
            legF = e.legFront; legB = e.legBack;

            float tw = e.torso.rect.width, th = e.torso.rect.height;
            float legLenF = e.front ? e.front.rect.height * (1 - PivotTL(e.front).y) * legF : 0;
            float torsoX = -tw * 0.5f, torsoY = -(e.shoulder.y * th + legLenF);
            Vector2 Place(Vector2 a) => new(torsoX + a.x * tw, torsoY + a.y * th);
            hipPx = Place(e.hip); neckPx = Place(e.neck); tailPx = Place(e.tailAnchor); shoulderPx = Place(e.shoulder);
            headScale = headScaleOverride > 0 ? headScaleOverride : e.head ? Mathf.Min(1, headWidthRatio * tw / e.head.rect.width) : 1;
            this.legsBehind = legsBehind;
            float len = tw + (e.head ? e.head.rect.width * headScale * 0.55f : 0);
            Unit = rigLength / len;

            // body = 엉덩이 위치 (기울기 축). 파츠는 body 기준 상대 위치
            body.localPosition = L(hipPx);
            Vector3 Rel(Vector2 px) => L(px) - L(hipPx);
            torso.transform.localPosition = Rel(new Vector2(0, torsoY + th * 0.5f));
            float gap = tw * farGap;
            farBack.transform.localPosition = Rel(hipPx + new Vector2(gap, 0));
            farFront.transform.localPosition = Rel(shoulderPx + new Vector2(gap, 0));
            back.transform.localPosition = Rel(hipPx);
            front.transform.localPosition = Rel(shoulderPx);
            tail.transform.localPosition = Rel(tailPx);
            head.transform.localPosition = Rel(neckPx);
            farBack.transform.localScale = back.transform.localScale = Vector3.one * legB;
            farFront.transform.localScale = front.transform.localScale = Vector3.one * legF;
            head.transform.localScale = Vector3.one * headScale;
            // 그리는 순서: 먼 다리 → 꼬리 → 몸통 → 가까운 다리 → 머리
            farBack.sortingOrder = 0; farFront.sortingOrder = 1; tail.sortingOrder = 2; torso.sortingOrder = 3;
            back.sortingOrder = 4; front.sortingOrder = 5; head.sortingOrder = 6;
            if (legsBehind) { back.sortingOrder = 2; front.sortingOrder = 2; tail.sortingOrder = 1; }
        }

        // 색 되돌리기 (보스 효과 칠하기 끝): 전부 흰색, 먼 다리만 어둡게. 투명도는 그대로
        public void ResetColors()
        {
            foreach (var r in GetComponentsInChildren<SpriteRenderer>()) { var a = r.color.a; var c = (r == farFront || r == farBack) ? farLegColor : Color.white; c.a = a; r.color = c; }
        }

        // 웹게임 ratPose 의 자세 값 (각도는 라디안, 캔버스 기준 → 유니티는 부호 반대)
        public struct Pose { public float head, headX, tail, front, back, farFront, farBack, bob, tilt, sx, sy; }

        // lift·rot·tsx·tsy·pivotH = 묘기 중 몸 전체 변환 (게임 단위·라디안, pivotH = 회전 중심 높이)
        // bodyPivot = 몸통 중심을 축으로 돎 (윈드밀: 뒤집혀도 제자리). 몸통 중심이 바닥 위 (몸통 반 높이 + lift) 에 옴
        public void Apply(in Pose p, float scale, int face, float sq, int sortOrder, float lift = 0, float rot = 0, float tsx = 1, float tsy = 1, float pivotH = 0, bool bodyPivot = false)
        {
            if (group) group.sortingOrder = sortOrder;
            // 쥐 그림은 왼쪽을 봄: 오른쪽(face 1)으로 갈 땐 뒤집음
            visual.localScale = new Vector3(scale * Unit * (face > 0 ? -1 : 1) * p.sx * tsx, scale * Unit * p.sy * sq * tsy, 1);
            var q = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            visual.localRotation = q;
            if (bodyPivot && !IsSingle && torso)
            {
                var c = Vector3.Scale(body.localPosition + torso.transform.localPosition, visual.localScale);     // 몸통 중심 (크기·뒤집기 적용)
                float half = torso.sprite ? torso.sprite.rect.height / 200f * Mathf.Abs(visual.localScale.y) : Mathf.Abs(c.y);
                visual.localPosition = new Vector3(0, lift * World.U + half, 0) - q * c;
            }
            else visual.localPosition = new Vector3(0, (lift + pivotH) * World.U, 0) + q * new Vector3(0, -pivotH * World.U, 0);
            if (IsSingle) return;
            float u = 1f / Mathf.Max(Unit, 1e-5f);
            body.localPosition = L(hipPx) + new Vector3(0, -p.bob * u / 100f, 0);
            body.localRotation = Quaternion.Euler(0, 0, p.tilt * Mathf.Rad2Deg);
            const float D = Mathf.Rad2Deg;
            farBack.transform.localRotation = Quaternion.Euler(0, 0, -p.farBack * D);
            farFront.transform.localRotation = Quaternion.Euler(0, 0, -p.farFront * D);
            back.transform.localRotation = Quaternion.Euler(0, 0, -p.back * D);
            front.transform.localRotation = Quaternion.Euler(0, 0, -p.front * D);
            tail.transform.localRotation = Quaternion.Euler(0, 0, p.tail * D);
            head.transform.localRotation = Quaternion.Euler(0, 0, p.head * D);
            Vector3 hp = L(neckPx) - L(hipPx);
            head.transform.localPosition = hp + new Vector3(p.headX * u / 100f, 0, 0);
            if (headOverride)
            {
                // 바꾼 머리: 원래 머리 폭에 맞추고, 쥐 그림(왼쪽을 봄)에 맞게 뒤집고, 목에서 앞(왼쪽)으로
                float aw = artHead ? artHead.rect.width : headOverride.rect.width, k = aw / headOverride.rect.width * headSwapScale;
                head.sprite = headOverride;
                head.transform.localScale = new Vector3(-headScale * k, headScale * k, 1);
                head.transform.localPosition += new Vector3(-aw * headScale * headSwapFwd / 100f, 0, 0);
            }
            else { var want = headReplace ? headReplace : artHead; if (head.sprite != want) { head.sprite = want; head.transform.localScale = Vector3.one * headScale; } }
            front.sortingOrder = p.front > 0.9f ? 7 : legsBehind ? 2 : 5;      // 치켜든 앞발은 얼굴 앞으로
        }
    }
}
