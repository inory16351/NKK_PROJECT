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
            [Tooltip("두 번째 머리 (고양이 보스: 공격·스킬 때 / 팝 캣은 계속 번갈아 뻐끔, 없으면 head)")] public Sprite head2;
            [Tooltip("파츠가 없는 한 장짜리 그림 (컴퓨터 마우스 쥐)")] public Sprite single;
            [Tooltip("몸통 이미지 좌상단 기준 0~1")] public Vector2 neck, tailAnchor, shoulder, hip;
            public float legFront = 1, legBack = 1;
            [Tooltip("화면 속 쥐 (5G 휴대폰): 겉은 single 한 장짜리로 움직이고, 파츠 쥐는 single 그림의 화면 칸 안에서 걷고 행동함")] public bool screen;
            [Tooltip("single 그림 안 화면 칸 (좌하단 기준 0~1)")] public Rect screenRect = new(0.15f, 0.2f, 0.7f, 0.6f);
            [Tooltip("화면 배경 그림 (화면 칸에 맞춰 늘림)")] public Sprite screenBg;
            [Tooltip("날개 (나비쥐: 큰 귀 한 장, 가까운·먼 쪽 둘로 그림)")] public Sprite wing;
            [Tooltip("날개 붙는 자리 (머리 이미지 좌상단 기준 0~1) · 날개 키 (머리 폭 배율)")] public Vector2 wingAnchor = new(0.3f, 0.1f);
            public float wingSize = 1.4f;
            [Tooltip("먼 쪽 날개 그림 (얼굴 반대쪽으로 돌아간 납작한 모양, 없으면 가까운 쪽 그림) · 작은 아래 날개 (없으면 안 씀) · 붙는 자리 · 키 (머리 폭 배율)")] public Sprite wingFar, wing2Far, wing2; public Vector2 wing2Anchor = new(0.3f, 0.7f); public float wing2Size = 0.7f;
            [Tooltip("머리 크기 고정 (0 = 몸통 폭 × 0.62 이하로 자동). 나비쥐처럼 머리가 몸통보다 클 때")] public float headScale;
            [Tooltip("두 마리가 붙은 쥐 (콩쥐팥쥐): 위에 거꾸로 붙는 쥐 그림 code_id")] public string twinCodeId;
            [Tooltip("머리만 있는 쥐 (나비쥐): 몸통·다리 없이 머리 + 날개(귀) + 꼬리")] public bool headOnly;
            // 겉모습이 한 장짜리인지 (파츠가 없거나, 화면 속 쥐)
            public bool IsSingle => single && (torso == null || screen);
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
