using System.Collections.Generic;
using NKK.Data;
using UnityEngine;

namespace NKK.Lobby
{
    // 로비 UI 아이콘 모음: 쥐 스킬(RatSkillIcons/rs_<id>) · 공용 스킬(SkillIcons/cs_*) · 필살기(UltIcons/ult_<id>) · 훈장 배지.
    // 테이블에는 "폴더/파일" 경로가 적혀 있고 파일 이름으로 찾음. 새 그림을 넣으면 컴포넌트 메뉴 Fill Icons 다시 실행.
    public class IconBook : MonoBehaviour
    {
        public static IconBook I { get; private set; }
        [Tooltip("아이콘 스프라이트 전부 (Fill Icons)")] public Sprite[] sprites;
        [Tooltip("Fill Icons 가 읽는 폴더")] public string[] folders = { "Assets/Art/Rats/RatSkillIcons", "Assets/Art/Rats/SkillIcons", "Assets/Art/Rats/UltIcons", "Assets/Art/Rats/AchvIcons" };

        readonly Dictionary<string, Sprite> map = new();

        void Awake() { I = this; Build(); }
        void Build() { map.Clear(); if (sprites != null) foreach (var s in sprites) if (s) map[s.name] = s; }

        // "폴더/파일" 또는 "파일" → 스프라이트
        public Sprite Get(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (map.Count == 0) Build();
            string n = path.Substring(path.LastIndexOf('/') + 1);
            return map.TryGetValue(n, out var s) ? s : null;
        }

        public Sprite Skill(RatSkillRow s) => s != null ? Get(s.skill_asset) : null;
        public Sprite Ult(RatUltimateRow u) => u != null ? Get(u.ult_icon) : null;

        // 성장 노드 아이콘: @action · @passive · @ult = 그 쥐의 아이콘, 아니면 경로
        public Sprite Node(GrowthNodeRow n, RatCharacterRow rat)
        {
            if (n == null) return null;
            var db = GameDatabase.Instance;
            switch (n.node_icon)
            {
                case "@action": return rat != null && db.RatSkills.TryGetValue(rat.action_skill, out var a) ? Skill(a) : null;
                case "@passive": return rat != null && db.RatSkills.TryGetValue(rat.passive_skill, out var p) ? Skill(p) : null;
                case "@ult": return Ult(db.UltOf(rat));
                default: return Get(n.node_icon);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Fill Icons")]
        void FillIcons()
        {
            var l = new List<Sprite>();
            foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Sprite", folders))
            { var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)); if (s) l.Add(s); }
            sprites = l.ToArray(); Build(); UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
