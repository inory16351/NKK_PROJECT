using System;
using System.Collections.Generic;
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

        public static string Lang
        {
            get { if (lang == null) lang = PlayerPrefs.GetString(Pref, Korean); return lang; }
        }
        public static bool IsKorean => Lang == Korean;

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

        // 키의 지금 언어 글. 한국어이거나 번역이 없으면 fallback (씬·테이블에 적힌 원래 글)
        public static string Get(int key, string fallback)
        {
            if (IsKorean || key <= 0 || !map.TryGetValue(key, out var r)) return fallback;
            string s = Lang == English ? r.en : null;
            return string.IsNullOrEmpty(s) ? fallback : s;
        }
    }
}
