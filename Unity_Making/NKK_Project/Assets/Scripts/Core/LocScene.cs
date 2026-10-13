using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NKK
{
    // 씬이 열릴 때 제일 먼저 (다른 스크립트가 글을 읽어 두기 전에) 씬 글을 지금 언어로 바꿔 끼움 (로비 · 게임 씬의 Localization 오브젝트).
    // ① 이 씬의 LocText 전부 (꺼진 것 포함) ② 인스펙터 문자열 (fields: 컴포넌트 + 경로 예 chatLines[0] · pages[1].label + 키).
    // 언어는 타이틀 설정에서만 바뀌므로 여기선 씬을 열 때 한 번만. 한국어면 아무것도 안 함 (씬 글 그대로).
    [DefaultExecutionOrder(-10000)]
    public class LocScene : MonoBehaviour
    {
        [Tooltip("씬을 바로 열었을 때 (타이틀을 거치지 않음) 쓸 스트링 테이블")] public TextAsset stringTable;

        [Serializable]
        public class Field
        {
            public Component target;
            [Tooltip("필드 경로: 이름 · 이름[번호] · 이름[번호].하위 이름")] public string path;
            public int key;
        }
        public List<Field> fields = new();

        void Awake()
        {
            if (!Loc.Loaded && stringTable) Loc.Load(stringTable);
            if (Loc.IsKorean) return;
            foreach (var t in FindObjectsByType<LocText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.gameObject.scene == gameObject.scene) t.ApplyNow();
            foreach (var f in fields)
            {
                if (!f.target || string.IsNullOrEmpty(f.path)) continue;
                try { SetPath(f.target, f.path, f.key); }
                catch (Exception e) { Debug.LogWarning($"[LocScene] {f.target.name}.{f.path}: {e.Message}"); }
            }
        }

        static readonly Regex Part = new(@"^(\w+)(?:\[(\d+)\])?$");

        // path 를 따라 내려가 마지막 문자열을 Loc.Get(key, 원래 글) 로 바꿈 (중간이 struct 여도 되돌려 씀)
        static void SetPath(object root, string path, int key)
        {
            var parts = path.Split('.');
            Set(root, parts, 0, key);
        }

        static object Set(object obj, string[] parts, int i, int key)
        {
            var m = Part.Match(parts[i]);
            if (!m.Success) throw new Exception("경로 " + parts[i]);
            var f = obj.GetType().GetField(m.Groups[1].Value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) throw new Exception("필드 없음 " + m.Groups[1].Value);
            object v = f.GetValue(obj);
            bool last = i == parts.Length - 1;
            if (m.Groups[2].Success)
            {
                int idx = int.Parse(m.Groups[2].Value);
                var list = (IList)v;
                object e = list[idx];
                list[idx] = last ? Loc.Get(key, (string)e) : Set(e, parts, i + 1, key);
            }
            else v = last ? Loc.Get(key, (string)v) : Set(v, parts, i + 1, key);
            f.SetValue(obj, v);
            return obj;
        }
    }
}
