using System;
using System.Collections.Generic;
using UnityEngine;

namespace NKK.Rats
{
    // 종별 리그 그림(파츠 5장) + 몸통 부착점. 메뉴 NKK/Build Rat Art Library 로 Assets/Art/Rats/Parts 와 RatRigMeta.json 에서 채움.
    // 값은 인스펙터에서 바로 고칠 수 있음.
    [CreateAssetMenu(menuName = "NKK/Rat Art Library")]
    public class RatArtLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string codeId;
            public Sprite head, torso, tail, front, back;
            [Tooltip("파츠가 없는 한 장짜리 그림 (컴퓨터 마우스 쥐)")] public Sprite single;
            [Tooltip("몸통 이미지 좌상단 기준 0~1")] public Vector2 neck, tailAnchor, shoulder, hip;
            public float legFront = 1, legBack = 1;
        }

        public List<Entry> entries = new();

        Dictionary<string, Entry> map;
        public Entry Get(string codeId)
        {
            if (map == null || map.Count != entries.Count) { map = new(); foreach (var e in entries) map[e.codeId] = e; }
            return map.TryGetValue(codeId, out var v) ? v : null;
        }
    }
}
