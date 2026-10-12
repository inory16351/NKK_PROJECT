using System;
using System.Collections.Generic;
using UnityEngine;

namespace NKK.Humans
{
    // 사람 그림 모음 (머리·겁먹은 머리·화난 머리(보스, 없어도 됨)·몸통·팔·다리 + 몸통 부착점). 메뉴 NKK/Build Human Art Library 로 채움
    [CreateAssetMenu(menuName = "NKK/Human Art Library")]
    public class HumanArtLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string codeId;
            public Sprite head, scared, torso, arm, leg;
            [Tooltip("화난 머리 (보스 공격 때, 없으면 head)")] public Sprite angry;
            [Tooltip("몸통 이미지 좌상단 기준 0~1")] public Vector2 neck, shoulder, hip;
            [Tooltip("몸통이 정면 그림 (보스): 팔은 몸통 양옆, 다리는 벌려서, 목은 가운데, 먼 쪽 어둡게 안 함")] public bool front;
            [Tooltip("머리 크기 배율 (목 기준). 보스 머리가 몸통만큼 커서 0.85")] public float headScale = 1;
        }
        public List<Entry> entries = new();
        public Entry Get(string id) { foreach (var e in entries) if (e.codeId == id) return e; return null; }
    }
}
