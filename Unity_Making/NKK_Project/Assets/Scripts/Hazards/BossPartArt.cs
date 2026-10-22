using System;
using System.Collections.Generic;
using UnityEngine;

namespace NKK.Hazards
{
    // 전용 파츠 보스 그림 모음 (사람·고양이 리그에 안 맞는 보스: 거미·상어·물고기 고양이 · 항공모함 · 차력거인 · 굴착기).
    // 메뉴 NKK/Build Boss Part Art 로 채움 (BossRework 시트를 자른 Art/Rats/BossParts/<code>/), 붙는 자리·움직임 값은 이 에셋에서 고침.
    // 그림은 왼쪽을 봄. 첫 파츠 = 뿌리 (부모 없음).
    [CreateAssetMenu(menuName = "NKK/Boss Part Art")]
    public class BossPartArt : ScriptableObject
    {
        public enum Move { None, Walk, Bob, Sway, Spin, Recoil, Wiggle }

        [Serializable]
        public class Part
        {
            public string name;
            public Sprite sprite;
            [Tooltip("두 번째 그림 (공격·발악 때, 없으면 sprite)")] public Sprite alt;
            [Tooltip("붙을 부모 파츠 이름 (뿌리는 비움)")] public string parent;
            [Tooltip("부모 그림 좌상단 기준 0~1 붙는 자리 · 뿌리는 발끝 원점에서 (뿌리 폭, 뿌리 높이) 배율 (y 위 +)")] public Vector2 at;
            [Tooltip("이 그림의 관절 = 그림 좌상단 기준 0~1")] public Vector2 pivot = new(0.5f, 0.5f);
            [Tooltip("기본 각도 (도, 반시계 +)")] public float angle;
            [Tooltip("크기 배율 (자식도 같이)")] public float scale = 1;
            [Tooltip("좌우 뒤집기 (자식도 같이)")] public bool flipX;
            [Tooltip("그리는 순서 (클수록 앞)")] public int order;
            [Tooltip("먼 쪽 파츠 (어둡게)")] public bool far;
            [Tooltip("움직임: Walk = 걸을 때 흔듦 · Bob = 위아래 · Sway = 늘 흔듦 · Spin = 빙글 · Recoil = 공격 중 사격 (두 번째 그림과 번갈아 깜빡 + 반동으로 흔들림, 평소엔 살짝 들썩) · Wiggle = 걷다 멈춘 순간 한 번 출렁 (1/speed 초) + 공격 중엔 사격 박자로 툭툭 (차력거인 엉덩이)")] public Move move;
            [Tooltip("움직임 크기 (각도 ° · Bob·Recoil 은 픽셀)")] public float amp;
            [Tooltip("위상 (0~1, 다리마다 엇갈리게)")] public float phase;
            [Tooltip("빠르기 배율")] public float speed = 1;
        }

        [Serializable]
        public class Entry
        {
            public string codeId;
            [Tooltip("뿌리 그림 가로 = 이 게임 단위 (× 보스 scale)")] public float length = 300;
            [Tooltip("말풍선·소품 높이 (게임 단위, × 보스 scale)")] public float height = 220;
            public List<Part> parts = new();
        }

        public List<Entry> entries = new();
        public Entry Get(string id) { foreach (var e in entries) if (e.codeId == id) return e; return null; }
    }
}
