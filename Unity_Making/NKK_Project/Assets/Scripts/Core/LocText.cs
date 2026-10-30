using TMPro;
using UnityEngine;

namespace NKK
{
    // 씬 TMP 글을 스트링 테이블 키로 바꿔 끼움 (한국어 = 씬에 적힌 글 그대로).
    // 자리표시({slot} 등)를 코드가 채우는 글은 template 을 켜면 직접 안 바꾸고, 코드가 LocText.Tpl 로 지금 언어 틀을 가져감.
    [RequireComponent(typeof(TMP_Text))]
    public class LocText : MonoBehaviour
    {
        [Tooltip("스트링 테이블 키 (씬 글 = 80xxxxx)")] public int key;
        [Tooltip("코드가 자리표시를 채우는 글: 직접 안 바꾸고 틀만 줌")] public bool template;

        TMP_Text t; string ko; bool ready;

        void Init() { if (ready) return; t = GetComponent<TMP_Text>(); ko = t ? t.text : ""; ready = true; }

        // 지금 언어의 글 (자리표시 채우기 전 틀)
        public string Template { get { Init(); return Loc.Get(key, ko); } }

        void Awake() => Init();
        void OnEnable() { Loc.Changed += Apply; Apply(); }
        void OnDisable() => Loc.Changed -= Apply;
        void Apply() { if (template) return; Init(); if (t) { t.text = Template; Fit(); } }
        // 씬을 열 때 한 번 (LocScene): template 이어도 씬 글 자체를 지금 언어로 (코드가 나중에 틀로 읽어 감)
        public void ApplyNow() { Init(); if (t) { t.text = Template; Fit(); } }

        // 한국어가 아니면 글이 길어지므로 칸을 넘칠 때만 글자를 줄임 (자동 크기, 최소 원래의 minScale 배) — 버튼 음영·칸 밖으로 나가는 것 막음
        public const float MinScale = 0.55f;
        void Fit()
        {
            if (Loc.IsKorean || !t || t.enableAutoSizing) return;
            // 최소 크기 바닥 8 은 화면 UI 글자만 (월드 글자 = 로비 팻말 · 말풍선은 크기가 1.5~2.4 라 8 이면 오히려 몇 배로 커짐, 2026-10-30)
            float floor = t is TextMeshPro ? 0 : 8;
            t.fontSizeMax = t.fontSize; t.fontSizeMin = Mathf.Min(t.fontSize, Mathf.Max(floor, t.fontSize * MinScale)); t.enableAutoSizing = true;
        }

        // 글에 LocText 가 있으면 지금 언어 틀, 없으면 cached (예전처럼 씬 글)
        public static string Tpl(TMP_Text text, string cached)
        {
            var l = text ? text.GetComponent<LocText>() : null;
            return l ? l.Template : cached;
        }
    }
}
