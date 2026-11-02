using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NKK
{
    // 언어 (스트링 테이블 Data_Table/스트링 테이블.xlsx → StringTable.json).
    // 키 = 다른 테이블의 <칸>_sk · 씬 글 80xxxxx · 코드 글 90xxxxx. 한국어는 원래 글(씬·테이블)을 그대로 쓰고, 다른 언어만 테이블에서 찾음.
    // 고른 언어는 PlayerPrefs (저장 슬롯과 상관없이 이 PC 에 하나).
    // 언어: 한국어 · 영어 · 일본어 · 중국어(간체) · 포르투갈어(브라질, 포르투갈도 읽음) · 스페인어(중남미 공통, 스페인도 읽음) · 러시아어
    //       · 프랑스어 · 독일어 · 아랍어(표준 아랍어, 오른쪽→왼쪽 — LocArabic).
    // 번역이 빈 칸이면 영어 → 원래 글 순서로 대신 씀. 한·영이 아닌 언어는 글꼴도 바뀜 (LocFont).
    public static class Loc
    {
        public const string Pref = "nkk_lang";
        public const string Korean = "ko", English = "en", Japanese = "ja", Chinese = "zh", Portuguese = "pt", Spanish = "es", Russian = "ru",
            French = "fr", German = "de", Arabic = "ar";
        // 설정 창에 나오는 순서
        public static readonly string[] All = { Korean, English, Japanese, Chinese, Portuguese, Spanish, Russian, French, German, Arabic };

        [Serializable] class Row { public int string_key; public string ko, en, ja, zh, pt, es, ru, fr, de, ar; }

        static string Text(Row r, string l) => l switch
        {
            Korean => r.ko, English => r.en, Japanese => r.ja, Chinese => r.zh,
            Portuguese => r.pt, Spanish => r.es, Russian => r.ru,
            French => r.fr, German => r.de, Arabic => r.ar, _ => null,
        };
        // 지금 언어 글, 비었으면 영어 (번역이 덜 된 언어도 한국어보다 읽기 쉬움)
        static string Best(Row r)
        {
            var s = Text(r, Lang);
            return string.IsNullOrEmpty(s) && !IsKorean ? r.en : s;
        }
        [Serializable] class File { public List<Row> String; }

        static readonly Dictionary<int, Row> map = new();
        static string lang;

        // 언어가 바뀌면 (LocText · 글을 채우는 스크립트가 다시 그림)
        public static event Action Changed;

        public static string Lang
        {
            get
            {
                if (lang == null) { lang = PlayerPrefs.GetString(Pref, Korean); if (Array.IndexOf(All, lang) < 0) lang = Korean; }
                return lang;
            }
        }
        public static bool IsKorean => Lang == Korean;
        // 한국어·영어는 씬에 정한 글꼴 그대로, 나머지는 LocFont 가 그 언어 글꼴로 바꿈
        public static bool OwnFont => Lang != Korean && Lang != English;
        public static bool Loaded => map.Count > 0;

        public static void Load(TextAsset t)
        {
            if (!t) return;
            map.Clear();
            var f = JsonUtility.FromJson<File>(t.text);
            if (f?.String != null) foreach (var r in f.String) map[r.string_key] = r;
        }

        public static void SetLang(string l)
        {
            if (string.IsNullOrEmpty(l) || l == Lang || Array.IndexOf(All, l) < 0) return;
            lang = l; PlayerPrefs.SetString(Pref, l); PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // 테이블 행의 글 칸을 지금 언어로 바꿈: string 칸 X 옆에 int 칸 X_sk 가 있으면 X = Get(X_sk, X)
        static readonly Dictionary<Type, (FieldInfo text, FieldInfo key)[]> pairs = new();
        public static void ApplyAll<T>(IEnumerable<T> rows)
        {
            if (rows == null || IsKorean) return;
            foreach (var r in rows) Apply(r);
        }
        public static void Apply(object row)
        {
            if (row == null || IsKorean) return;
            var t = row.GetType();
            if (!pairs.TryGetValue(t, out var ps))
            {
                var l = new List<(FieldInfo, FieldInfo)>();
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.FieldType != typeof(string)) continue;
                    var k = t.GetField(f.Name + "_sk", BindingFlags.Public | BindingFlags.Instance);
                    if (k != null && k.FieldType == typeof(int)) l.Add((f, k));
                }
                pairs[t] = ps = l.ToArray();
            }
            foreach (var (text, key) in ps) text.SetValue(row, Get((int)key.GetValue(row), (string)text.GetValue(row)));
        }

        // 키의 지금 언어 글 (테이블·씬 글: 한국어면 원래 글 그대로). 다른 언어인데 번역이 없으면 경고 (스트링 테이블 상태 '번역 필요' 를 채울 것)
        public static string Get(int key, string fallback)
        {
            if (IsKorean || key <= 0) return fallback;
            if (map.TryGetValue(key, out var r))
            {
                if (string.IsNullOrEmpty(Text(r, Lang))) Missing(key);
                string s = Best(r);
                if (!string.IsNullOrEmpty(s)) return s;
            }
            else Missing(key);
            return fallback;
        }

        // 코드 글 (90xxxxx 등): 키만으로 지금 언어 글. 한국어도 테이블 ko 칸에서 (코드에 원문을 두지 않음)
        public static string T(int key)
        {
            if (map.TryGetValue(key, out var r))
            {
                if (string.IsNullOrEmpty(Text(r, Lang))) Missing(key);
                string s = Best(r);
                if (!string.IsNullOrEmpty(s)) return s;
            }
            else Missing(key);
            return "#" + key;
        }
        // 무작위 하나 (팝업 글 묶음)
        public static string Pick(params int[] keys) => keys == null || keys.Length == 0 ? "" : T(keys[UnityEngine.Random.Range(0, keys.Length)]);

        static readonly HashSet<int> warned = new();
        static void Missing(int key) { if (warned.Add(key)) Debug.LogWarning($"[Loc] {Lang} 글 없음: 키 {key} (스트링 테이블 확인)"); }
    }
}
