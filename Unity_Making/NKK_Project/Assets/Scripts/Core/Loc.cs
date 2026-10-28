using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NKK
{
    // 언어 (스트링 테이블 Data_Table/스트링 테이블.xlsx → StringTable.json).
    // 키 = 다른 테이블의 <칸>_sk · 씬 글 80xxxxx · 코드 글 90xxxxx. 한국어는 원래 글(씬·테이블)을 그대로 쓰고, 다른 언어만 테이블에서 찾음.
    // 고른 언어는 PlayerPrefs (저장 슬롯과 상관없이 이 PC 에 하나).
    public static class Loc
    {
        public const string Pref = "nkk_lang";
        public const string Korean = "ko", English = "en";

        [Serializable] class Row { public int string_key; public string ko, en; }
        [Serializable] class File { public List<Row> String; }

        static readonly Dictionary<int, Row> map = new();
        static string lang;

        // 언어가 바뀌면 (LocText · 글을 채우는 스크립트가 다시 그림)
        public static event Action Changed;

        // 처음 실행(고른 언어 없음) 때 언어: 웹 빌드(itch.io) = 영어, 에디터·윈도우 = 한국어 (팀 작업용)
#if UNITY_WEBGL && !UNITY_EDITOR
        public const string Default = English;
#else
        public const string Default = Korean;
#endif

        public static string Lang
        {
            get { if (lang == null) lang = PlayerPrefs.GetString(Pref, Default); return lang; }
        }
        public static bool IsKorean => Lang == Korean;
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
            if (string.IsNullOrEmpty(l) || l == Lang) return;
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
                string s = Lang == English ? r.en : null;
                if (!string.IsNullOrEmpty(s)) return s;
            }
            Missing(key);
            return fallback;
        }

        // 코드 글 (90xxxxx 등): 키만으로 지금 언어 글. 한국어도 테이블 ko 칸에서 (코드에 원문을 두지 않음)
        public static string T(int key)
        {
            if (map.TryGetValue(key, out var r))
            {
                string s = IsKorean ? r.ko : Lang == English ? r.en : null;
                if (!string.IsNullOrEmpty(s)) return s;
            }
            Missing(key);
            return "#" + key;
        }
        // 무작위 하나 (팝업 글 묶음)
        public static string Pick(params int[] keys) => keys == null || keys.Length == 0 ? "" : T(keys[UnityEngine.Random.Range(0, keys.Length)]);

        static readonly HashSet<int> warned = new();
        static void Missing(int key) { if (warned.Add(key)) Debug.LogWarning($"[Loc] {Lang} 글 없음: 키 {key} (스트링 테이블 확인)"); }
    }
}
