using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 업적 알림 (게임 · 로비 둘 다): Progress.AchvGot (업적 스위치가 처음 켜질 때 한 번) → 오른쪽에서 밀려 들어오는 패널 (메달 아이콘 · 업적 이름 · NEW · '+n 도토리').
    // 여러 개가 한꺼번에 오면 차례로, 대기가 groupOver 개를 넘으면 '업적 n개 달성 · +합계' 한 장으로 묶음. 업적 안내 때 지난 업적 소급은 Progress.AchvBatch → 한 장.
    // 초필살기 · 증강 고르기 · 튜토리얼 대사 중에는 끝난 뒤 표시. 게임은 멈추지 않음.
    // 글은 씬 TMP ({achv} {desc} {n}) + 인스펙터 문자열, 아이콘 = 업적 테이블 achv_icon (컴포넌트 메뉴 Fill Icons).
    // 이 컴포넌트는 늘 켜져 있는 오브젝트에 두고 panel 만 켰다 껐다 함.
    public class AchievementToast : MonoBehaviour
    {
        public RectTransform panel;
        [Tooltip("업적 이름 (자리 {achv})")] public TMP_Text title;
        [Tooltip("조건 설명 (자리 {desc}, 없어도 됨)")] public TMP_Text desc;
        public Image icon;
        [Tooltip("NEW 표시 (알림은 처음 달성 때만 오므로 늘 켬)")] public GameObject newMark;
        [Tooltip("도토리 줄 ({n}) — 도토리 0 인 업적(기존 필살기)은 숨김")] public TMP_Text acornLine;
        [Tooltip("보이는 시간 (초)")] public float showTime = 3.2f;
        [Tooltip("업적 아이콘 (ach_<id>)")] public Sprite[] icons;
        [Header("묶음 알림")]
        [Tooltip("대기가 이 개수를 넘으면 한 장으로 묶음")] public int groupOver = 5;
        [Tooltip("묶음 제목 ({n}) · 설명 ({n})")] public string groupTitle = "업적 {n}개 달성!", groupDesc = "여러 업적을 한꺼번에 달성했어요";
        [Tooltip("지난 업적 소급 (업적 안내 때) 제목 ({n}) · 설명 ({n})")] public string pastTitle = "지난 업적 {n}개!", pastDesc = "안내 전에 달성한 업적 도토리를 한꺼번에 받았어요";

        class Item { public AchievementRow row; public int count, acorn; public bool past; }
        readonly List<Item> queue = new();
        string titleFmt, descFmt, acornFmt;
        float t0 = -99, x0; bool xSet, hooked;

        void Start()
        {
            if (panel) panel.gameObject.SetActive(false);
            if (title) titleFmt = LocText.Tpl(title, title.text);
            if (desc) descFmt = LocText.Tpl(desc, desc.text);
            if (acornLine) acornFmt = LocText.Tpl(acornLine, acornLine.text);
        }

        void OnDestroy() { if (hooked && Progress.I) { Progress.I.AchvGot -= OnGot; Progress.I.AchvBatch -= OnBatch; } }

        void OnGot(AchievementRow a) => queue.Add(new Item { row = a, count = 1, acorn = Progress.I ? Progress.I.AchvReward(a) : 0 });
        void OnBatch(int n, int acorn) => queue.Add(new Item { count = n, acorn = acorn, past = true });

        // 대기가 많으면 한 장으로 묶음
        Item Next()
        {
            if (queue.Count > groupOver)
            {
                var g = new Item(); foreach (var q in queue) { g.count += q.count; g.acorn += q.acorn; }
                queue.Clear(); return g;
            }
            var it = queue[0]; queue.RemoveAt(0); return it;
        }

        void Show(Item it)
        {
            SfxManager.Play("jgl_achievement");
            if (!panel) return;
            if (!xSet) { x0 = panel.anchoredPosition.x; xSet = true; }
            panel.gameObject.SetActive(true);
            if (newMark) newMark.SetActive(true);
            var a = it.row;
            string name = a != null ? a.achv_name : (it.past ? pastTitle : groupTitle).Replace("{n}", it.count.ToString());
            string d = a != null ? a.achv_desc ?? "" : (it.past ? pastDesc : groupDesc).Replace("{n}", it.count.ToString());
            if (a != null && d.Contains("{ult}")) d = d.Replace("{ult}", GameDatabase.Instance.Ultimates.TryGetValue(a.target_id, out var u) ? u.ultimate_name : "");
            if (title) title.text = (titleFmt ?? "{achv}").Replace("{achv}", name);
            if (desc) desc.text = (descFmt ?? "{desc}").Replace("{desc}", d);
            if (acornLine) { acornLine.gameObject.SetActive(it.acorn > 0); acornLine.text = (acornFmt ?? "+{n}").Replace("{n}", it.acorn.ToString("N0")); }
            if (icon) { icon.sprite = a != null ? Icon(a) : null; icon.enabled = icon.sprite; }
            t0 = Time.unscaledTime;
        }

        Sprite Icon(AchievementRow a)
        {
            if (icons == null || string.IsNullOrEmpty(a.achv_icon)) return null;
            string n = a.achv_icon.Substring(a.achv_icon.LastIndexOf('/') + 1);
            foreach (var s in icons) if (s && s.name == n) return s;
            return null;
        }

        // 초필살기 · 증강 고르기 · 튜토리얼 대사 중에는 새 알림을 띄우지 않고 기다림
        static bool Busy => (NKK.Ults.SuperUltManager.I && NKK.Ults.SuperUltManager.I.Busy) || (AugmentManager.I && AugmentManager.I.PickShowing) || NKK.Tutorial.TutorialBox.Showing;

        void Update()
        {
            if (!hooked && Progress.I) { Progress.I.AchvGot += OnGot; Progress.I.AchvBatch += OnBatch; hooked = true; }
            if (!panel) return;
            float age = Time.unscaledTime - t0;
            if (age > showTime)
            {
                if (panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
                if (queue.Count > 0 && !Busy) Show(Next());
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
