using UnityEngine;

namespace NKK
{
    // 좌표계: 게임 로직은 웹게임과 같은 "게임 단위"(x 오른쪽 +, y 아래 +, z 위 +)로 계산하고,
    // 화면에 놓을 때만 유니티 월드로 바꾼다. 1 유니티 유닛 = 100 게임 단위.
    // 3/4 탑다운: 바닥(y)은 TILT 만큼 눌러 그리고, 높이(z)는 그대로 위로.
    public static class World
    {
        public const float RW = 1280, RH = 800;   // 방 하나 크기
        public const float WM = 16;               // 벽 두께의 절반
        public const float TILT = 0.85f;
        public const float U = 0.01f;             // 게임 단위 → 유니티 유닛
        public const float GZ = 1700;             // 중력 (물건 날아갈 때)

        public static Vector3 ToUnity(float x, float y, float z = 0) => new Vector3(x * U, -(y * TILT - z) * U, 0);
        public static Vector2 FromUnity(Vector3 p) => new Vector2(p.x / U, -p.y / U / TILT);
        // 아래(y 가 큰) 것이 앞에 그려지게
        public static int SortOrder(float y) => Mathf.RoundToInt(y * 2);

        public static float Rand(float a, float b) => Random.Range(a, b);
    }
}
