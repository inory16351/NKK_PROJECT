using System.Collections.Generic;
using UnityEngine;

namespace NKK.Tutorial
{
    // 튜토리얼 대사가 가리킬 수 있는 곳 (튜토리얼 테이블 Line.highlight = 이 id).
    // UI(RectTransform) 면 그 사각형, 월드 오브젝트면 Renderer/Collider 범위를 강조 테두리가 감쌈
    public class TutorialTarget : MonoBehaviour
    {
        [Tooltip("강조 id (예: promote · power · pop · timer · minimap · ult · tab_skill · research)")] public string id;
        [Tooltip("강조 테두리 여백 (화면 픽셀)")] public float pad = 10;

        static readonly List<TutorialTarget> all = new();
        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        // 켜져 있는 것 중 첫 번째
        public static TutorialTarget Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var t in all) if (t && t.id == id && t.isActiveAndEnabled) return t;
            return null;
        }

        // 화면 좌표 사각형 (픽셀, 왼쪽 아래 원점)
        public bool ScreenRect(out Rect r)
        {
            r = default;
            var rt = transform as RectTransform;
            if (rt && rt.GetComponentInParent<Canvas>() is Canvas cv)
            {
                var c = new Vector3[4]; rt.GetWorldCorners(c);
                var cam = cv.renderMode == RenderMode.ScreenSpaceOverlay ? null : cv.worldCamera;
                Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, c[0]), b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
                r = Rect.MinMaxRect(Mathf.Min(a.x, b.x) - pad, Mathf.Min(a.y, b.y) - pad, Mathf.Max(a.x, b.x) + pad, Mathf.Max(a.y, b.y) + pad);
                return true;
            }
            var main = Camera.main; if (!main) return false;
            Bounds bd; bool has = false; bd = default;
            foreach (var rd in GetComponentsInChildren<Renderer>()) { if (!rd.enabled) continue; if (!has) { bd = rd.bounds; has = true; } else bd.Encapsulate(rd.bounds); }
            if (!has && GetComponent<Collider2D>() is Collider2D col) { bd = col.bounds; has = true; }
            if (!has) return false;
            Vector2 p0 = main.WorldToScreenPoint(bd.min), p1 = main.WorldToScreenPoint(bd.max);
            r = Rect.MinMaxRect(Mathf.Min(p0.x, p1.x) - pad, Mathf.Min(p0.y, p1.y) - pad, Mathf.Max(p0.x, p1.x) + pad, Mathf.Max(p0.y, p1.y) + pad);
            return true;
        }
    }
}
