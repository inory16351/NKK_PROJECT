using System.Collections.Generic;
using UnityEngine;

namespace NKK
{
    // 증강 그림 모음 (Resources/AugmentArt.asset, 로비·게임 같이 씀). 아이콘 이름 = 증강 테이블 icon 칸
    [CreateAssetMenu(menuName = "NKK/Augment Art")]
    public class AugmentArt : ScriptableObject
    {
        public List<FxManager.NamedSprite> icons = new();
        [Tooltip("아직 못 얻은 증강 (???) 그림")] public Sprite unknown;
        [Tooltip("등급별 카드 테두리 (1 브론즈 ~ 4 프리즘, 비우면 색만). 9칸 늘리기 테두리가 있으면 Sliced 로 그림")] public Sprite[] gradeFrames = new Sprite[4];
        [Tooltip("빈 슬롯 · 아직 안 열린 슬롯 그림 (2026-10-20)")] public Sprite emptySlot, lockedSlot;

        static AugmentArt inst;
        public static AugmentArt I { get { if (!inst) inst = Resources.Load<AugmentArt>("AugmentArt"); return inst; } }

        public Sprite Icon(string name) { foreach (var n in icons) if (n.name == name) return n.sprite; return null; }
        public Sprite Frame(int grade) => gradeFrames != null && grade >= 1 && grade <= gradeFrames.Length ? gradeFrames[grade - 1] : null;

#if UNITY_EDITOR
        [ContextMenu("Fill Icons (Assets/Art/Augment)")]
        void FillIcons()
        {
            icons.Clear();
            foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art/Augment" }))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (!s) continue;
                if (s.name == "aug_unknown") { unknown = s; continue; }
                if (s.name == "aug_slot_empty") { emptySlot = s; continue; }
                if (s.name == "aug_slot_lock") { lockedSlot = s; continue; }
                if (s.name.StartsWith("aug_ui_")) continue;          // 관리 창 판 · HUD 선반 (씬에 직접 연결)
                if (s.name.StartsWith("aug_frame_")) { if (int.TryParse(s.name.Substring(10), out int k) && k >= 1 && k <= 4) gradeFrames[k - 1] = s; continue; }
                icons.Add(new FxManager.NamedSprite { name = s.name, sprite = s });
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
