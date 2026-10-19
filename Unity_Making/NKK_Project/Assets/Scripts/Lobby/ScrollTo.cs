using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 가로 스크롤 줄에서 고른 칸이 가운데 오게 (2026-10-19, 훈장 16칸: 훈장 사다리 LadderView · 치즈 창고 탭 TabsView)
    public static class ScrollTo
    {
        public static void Center(Transform item)
        {
            if (!item) return;
            var sr = item.GetComponentInParent<ScrollRect>();
            if (!sr || !sr.content || !sr.viewport) return;
            Canvas.ForceUpdateCanvases();
            float w = sr.content.rect.width, v = sr.viewport.rect.width;
            if (w <= v + 1) return;
            var r = (RectTransform)item;
            float x = sr.content.InverseTransformPoint(r.TransformPoint(r.rect.center)).x - sr.content.rect.xMin;
            sr.horizontalNormalizedPosition = Mathf.Clamp01((x - v * 0.5f) / (w - v));
        }
    }
}
