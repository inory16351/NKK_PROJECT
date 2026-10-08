using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 게임 화면 업적 알림: Progress.AchvGot (업적 테이블 조건 달성) → 오른쪽에서 밀려 들어오는 패널 (메달 아이콘 · 업적 이름 · 처음이면 NEW).
    // 여러 개가 한꺼번에 오면 차례로. 글은 씬 TMP ({achv} {desc}), 아이콘 = 업적 테이블 achv_icon (컴포넌트 메뉴 Fill Icons).
    // 이 컴포넌트는 늘 켜져 있는 오브젝트에 두고 panel 만 켰다 껐다 함.
    public class AchievementToast : MonoBehaviour
    {
        public RectTransform panel;
        [Tooltip("업적 이름 (자리 {achv})")] public TMP_Text title;
        [Tooltip("조건 설명 (자리 {desc}, 없어도 됨)")] public TMP_Text desc;
        public Image icon;
        [Tooltip("처음 달성일 때만 보이는 표시 (NEW)")] public GameObject newMark;
        [Tooltip("보이는 시간 (초)")] public float showTime = 3.2f;
        [Tooltip("업적 아이콘 (ach_<id>)")] public Sprite[] icons;

        readonly Queue<(AchievementRow a, bool first)> queue = new();
        string titleFmt, descFmt;
        float t0 = -99, x0; bool xSet, hooked;

        void Start()
        {
            if (panel) panel.gameObject.SetActive(false);
            if (title) titleFmt = title.text;
            if (desc) descFmt = desc.text;
        }

        void OnDestroy() { if (hooked && Progress.I) Progress.I.AchvGot -= OnGot; }

        void OnGot(AchievementRow a, bool first) => queue.Enqueue((a, first));

        void Show(AchievementRow a, bool first)
        {
            if (!panel) return;
            if (!xSet) { x0 = panel.anchoredPosition.x; xSet = true; }
            panel.gameObject.SetActive(true);
            if (newMark) newMark.SetActive(first);
            if (title) title.text = (titleFmt ?? "{achv}").Replace("{achv}", a.achv_name);
            if (desc)
            {
                string d = a.achv_desc ?? "";
                if (d.Contains("{ult}")) d = d.Replace("{ult}", GameDatabase.Instance.Ultimates.TryGetValue(a.target_id, out var u) ? u.ultimate_name : "");
                desc.text = (descFmt ?? "{desc}").Replace("{desc}", d);
            }
            if (icon) { icon.sprite = Icon(a); icon.enabled = icon.sprite; }
            t0 = Time.unscaledTime;
        }

        Sprite Icon(AchievementRow a)
        {
            if (icons == null || string.IsNullOrEmpty(a.achv_icon)) return null;
            string n = a.achv_icon.Substring(a.achv_icon.LastIndexOf('/') + 1);
            foreach (var s in icons) if (s && s.name == n) return s;
            return null;
        }

        void Update()
        {
            if (!hooked && Progress.I) { Progress.I.AchvGot += OnGot; hooked = true; }
            if (!panel) return;
            float age = Time.unscaledTime - t0;
            if (age > showTime)
            {
                if (panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
                if (queue.Count > 0) { var (a, f) = queue.Dequeue(); Show(a, f); }
                return;
            }
            float slide = age < 0.3f ? Ease(age / 0.3f) : age > showTime - 0.3f ? 1 - (age - showTime + 0.3f) / 0.3f : 1;
            var p = panel.anchoredPosition; panel.anchoredPosition = new Vector2(x0 + (1 - slide) * (panel.rect.width + 80), p.y);     // 오른쪽에서 밀려 들어옴
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return 1 - Mathf.Pow(1 - x, 3); }

#if UNITY_EDITOR
        [ContextMenu("Fill Icons")]
        void FillIcons()
        {
            var l = new List<Sprite>();
            foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art/Rats/AchvIcons" }))
            { var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)); if (s) l.Add(s); }
            icons = l.ToArray(); UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
