using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NKK.Humans
{
    // 사람 그림 모음 (머리·겁먹은 머리·화난 머리(보스, 없어도 됨)·몸통·팔·다리 + 몸통 부착점). 메뉴 NKK/Build Human Art Library 로 채움
    [CreateAssetMenu(menuName = "NKK/Human Art Library")]
    public class HumanArtLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string codeId;
            public Sprite head, scared, torso, arm, leg;
            [Tooltip("화난 머리 (보스 공격 때, 없으면 head)")] public Sprite angry;
            [Tooltip("몸통 이미지 좌상단 기준 0~1")] public Vector2 neck, shoulder, hip;
            [Tooltip("몸통이 정면 그림 (보스): 팔은 몸통 양옆, 다리는 벌려서, 목은 가운데, 먼 쪽 어둡게 안 함")] public bool front;
            [Tooltip("머리 크기 배율 (목 기준). 보스 머리가 몸통만큼 커서 0.85")] public float headScale = 1;
        }
        public List<Entry> entries = new();
        public Entry Get(string id) { foreach (var e in entries) if (e.codeId == id) return e; return null; }
    }

    // 사람 컷아웃 리그 (웹게임 drawHuman 이식). 리그 좌표 = 파츠 픽셀 / 100, 발끝 = 원점, 그림은 왼쪽을 봄.
    [RequireComponent(typeof(SortingGroup))]
    public class HumanRig : MonoBehaviour
    {
        public Transform visual;      // 좌우 반전·크기·날아갈 때 회전
        public Transform body;        // 엉덩이 기준 기울기 (lean)
        public SpriteRenderer armFar, legFar, legNear, torso, head, armNear;
        public Color farColor = new(0.8f, 0.78f, 0.79f);
        [Tooltip("정면 몸통: 팔 위치 = 몸통 가운데 ± 이 값 × 몸통 폭 · 다리 = ± legSpread × 몸통 폭")] public float frontArmSide = 0.4f, frontLegSpread = 0.2f;
        [Tooltip("사람 키 (게임 단위)")] public float height = 150;

        HumanArtLibrary.Entry art;
        float unit;                     // 픽셀 → 게임 단위
        Vector2 hipPx, shoulderPx, neckPx;
        SortingGroup group;

        static Vector3 L(Vector2 px) => new(px.x / 100f, -px.y / 100f, 0);

        public void Build(HumanArtLibrary.Entry e)
        {
            art = e; group = GetComponent<SortingGroup>();
            torso.sprite = e.torso; head.sprite = e.head; armFar.sprite = armNear.sprite = e.arm; legFar.sprite = legNear.sprite = e.leg;
            armFar.color = legFar.color = e.front ? Color.white : farColor;
            float hsc = e.headScale > 0 ? e.headScale : 1;
            float tw = e.torso.rect.width, th = e.torso.rect.height, lh = e.leg.rect.height, hh = e.head.rect.height * hsc;
            float legLen = lh * 0.95f;
            unit = height / (legLen + th * e.hip.y + hh * 0.85f);
            hipPx = new Vector2(0, -legLen);
            float tx = hipPx.x - e.hip.x * tw, ty = hipPx.y - e.hip.y * th;
            shoulderPx = new Vector2(tx + e.shoulder.x * tw, ty + e.shoulder.y * th);
            neckPx = new Vector2(tx + (e.front ? 0.5f : e.neck.x) * tw, ty + e.neck.y * th + th * 0.05f);
            body.localPosition = L(hipPx);
            Vector3 Rel(Vector2 px) => L(px) - L(hipPx);
            torso.transform.localPosition = Rel(new Vector2(tx + tw / 2, ty + th / 2));
            head.transform.localPosition = Rel(neckPx);
            head.transform.localScale = Vector3.one * hsc;
            if (e.front)
            {
                // 정면 몸통: 팔은 양 어깨 끝, 다리는 좌우로 (옆모습처럼 가운데 겹치면 팔이 같은 색 몸통에 묻혀 손만 보임)
                float cx = tx + tw / 2, sy = shoulderPx.y;
                armNear.transform.localPosition = Rel(new Vector2(cx - tw * frontArmSide, sy));
                armFar.transform.localPosition = Rel(new Vector2(cx + tw * frontArmSide, sy));
                legNear.transform.localPosition = Rel(new Vector2(cx - tw * frontLegSpread, hipPx.y - lh * 0.04f));
                legFar.transform.localPosition = Rel(new Vector2(cx + tw * frontLegSpread, hipPx.y - lh * 0.04f));
                armFar.sortingOrder = 2; legFar.sortingOrder = 1; legNear.sortingOrder = 1; torso.sortingOrder = 3; head.sortingOrder = 4; armNear.sortingOrder = 2;
            }
            else
            {
                armFar.transform.localPosition = Rel(shoulderPx + new Vector2(tw * 0.08f, 0));
                legFar.transform.localPosition = Rel(new Vector2(hipPx.x + tw * 0.12f, hipPx.y - lh * 0.04f));
                legNear.transform.localPosition = Rel(new Vector2(hipPx.x - tw * 0.1f, hipPx.y - lh * 0.04f));
                armNear.transform.localPosition = Rel(shoulderPx + new Vector2(-tw * 0.05f, 0));
                armFar.sortingOrder = 0; legFar.sortingOrder = 1; legNear.sortingOrder = 2; torso.sortingOrder = 3; head.sortingOrder = 4; armNear.sortingOrder = 5;
            }
        }

        public struct Pose { public float legN, legF, armN, armF, lean, head, bob, sx, sy; public bool scared, angry; }

        // scale = 크기 배율, rot = 날아갈 때 회전(라디안, 몸 가운데 기준), sq = 통통 튈 때 납작
        public void Apply(in Pose p, float scale, int face, float rot, float sq, int sortOrder, float alpha)
        {
            if (group) group.sortingOrder = sortOrder;
            float s = scale * unit;
            float sx = -face * s * p.sx / Mathf.Sqrt(sq), sy = s * p.sy * sq;
            // 회전 중심 = 발끝에서 키의 45% 위
            float cy = height * scale * 0.45f * World.U;
            visual.localRotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            visual.localPosition = new Vector3(0, p.bob * World.U, 0) + (Vector3)(Vector2)(Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg) * new Vector3(0, -cy, 0)) + new Vector3(0, cy, 0);
            visual.localScale = new Vector3(sx, sy, 1);
            head.sprite = p.scared && art.scared ? art.scared : p.angry && art.angry ? art.angry : art.head;
            const float D = Mathf.Rad2Deg;
            body.localRotation = Quaternion.Euler(0, 0, -p.lean * D);
            armFar.transform.localRotation = Quaternion.Euler(0, 0, p.armF * D);
            legFar.transform.localRotation = Quaternion.Euler(0, 0, p.legF * D);
            legNear.transform.localRotation = Quaternion.Euler(0, 0, p.legN * D);
            head.transform.localRotation = Quaternion.Euler(0, 0, p.head * D);
            armNear.transform.localRotation = Quaternion.Euler(0, 0, p.armN * D);
            var c = new Color(1, 1, 1, alpha);
            torso.color = head.color = legNear.color = armNear.color = c;
            var fc = art.front ? Color.white : farColor;
            armFar.color = legFar.color = new Color(fc.r, fc.g, fc.b, alpha);
        }
    }
}
