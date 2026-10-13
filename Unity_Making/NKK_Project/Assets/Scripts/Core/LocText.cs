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
        void Apply() { if (template) return; Init(); if (t) t.text = Template; }

        // 글에 LocText 가 있으면 지금 언어 틀, 없으면 cached (예전처럼 씬 글)
        public static string Tpl(TMP_Text text, string cached)
        {
            var l = text ? text.GetComponent<LocText>() : null;
            return l ? l.Template : cached;
        }
    }
}
