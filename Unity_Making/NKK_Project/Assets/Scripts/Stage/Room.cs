using UnityEngine;

namespace NKK.Stage
{
    // 방 하나의 그림 (Room 프리팹). 바닥·벽 4개는 자식 스프라이트. 로직(열림·벽 체력)은 StageManager.
    // 좌표는 게임 단위: 방 (i, j) 의 왼쪽 위 = (i × RW, j × RH).
    public class Room : MonoBehaviour
    {
        public int i, j;
        [Header("그림 (자식)")]
        public SpriteRenderer floor;
        public SpriteRenderer wallTop, wallBottom, wallLeft, wallRight;
        [Tooltip("뒷벽 높이 (게임 단위)")] public float wallHeight = 46;
        [Tooltip("아직 안 열린 방 바닥 색")] public Color closedTint = new(0.35f, 0.32f, 0.3f);
        [Tooltip("벽이 거의 부서졌을 때 색")] public Color crackedTint = new(0.85f, 0.55f, 0.45f);
        [Tooltip("맞을 때 흔들림 크기 (게임 단위)")] public float shakeAmount = 4;

        public float Left => i * World.RW;
        public float Top => j * World.RH;
        public Vector2 Center => new(Left + World.RW / 2, Top + World.RH / 2);

        // 층 구간 그림: 바닥 타일 · 뒷벽(정면) 타일 · 벽 윗면 색 (Layout 전에 호출)
        public void ApplyLook(Sprite floorTile, Sprite wallFace, Color wallCap)
        {
            if (floor && floorTile) floor.sprite = floorTile;
            if (wallTop && wallFace) { wallTop.sprite = wallFace; wallTop.color = Color.white; }
            foreach (var w in new[] { wallBottom, wallLeft, wallRight }) if (w) w.color = wallCap;
        }

        Color[] baseCol;
        Vector3[] basePos;
        float[] shake = new float[4];

        SpriteRenderer[] Walls => new[] { wallTop, wallBottom, wallLeft, wallRight };

        [ContextMenu("Layout")]
        public void Layout()
        {
            var c = World.ToUnity(Left + World.RW / 2, Top + World.RH / 2);
            float w = World.RW * World.U, h = World.RH * World.TILT * World.U, wm = World.WM * 2 * World.U, wh = wallHeight * World.U;
            if (floor) { floor.transform.position = c; floor.size = new Vector2(w, h); floor.sortingOrder = -30000; }
            if (wallTop) { wallTop.transform.position = c + new Vector3(0, h / 2 + wh / 2 - wm / 2, 0); wallTop.size = new Vector2(w, wh + wm); wallTop.sortingOrder = -29000; }
            if (wallBottom) { wallBottom.transform.position = c + new Vector3(0, -h / 2 + wm / 2, 0); wallBottom.size = new Vector2(w, wm); wallBottom.sortingOrder = 32000; }
            if (wallLeft) { wallLeft.transform.position = c + new Vector3(-w / 2 + wm / 2, wh / 2, 0); wallLeft.size = new Vector2(wm, h + wh); wallLeft.sortingOrder = 32000; }
            if (wallRight) { wallRight.transform.position = c + new Vector3(w / 2 - wm / 2, wh / 2, 0); wallRight.size = new Vector2(wm, h + wh); wallRight.sortingOrder = 32000; }
            var ws = Walls;
            baseCol = new Color[4]; basePos = new Vector3[4];
            for (int k = 0; k < 4; k++) if (ws[k]) { baseCol[k] = ws[k].color; basePos[k] = ws[k].transform.position; }
        }

        // 열림 여부 + 벽 4개 (위·아래·왼쪽·오른쪽) 표시
        public void SetState(bool open, bool top, bool bottom, bool left, bool right)
        {
            if (floor) floor.color = open ? Color.white : closedTint;
            var ws = Walls; var on = new[] { top, bottom, left, right };
            for (int k = 0; k < 4; k++) if (ws[k]) { ws[k].enabled = on[k]; if (baseCol != null) ws[k].color = baseCol[k]; }
        }

        // 벽 피격: 흔들림 + 남은 체력만큼 붉어짐 (di,dj = 맞은 방향)
        public void ShakeWall(int di, int dj, float hpRatio)
        {
            int k = dj < 0 ? 0 : dj > 0 ? 1 : di < 0 ? 2 : 3;
            shake[k] = Mathf.Min(1, shake[k] + 0.15f);
            var w = Walls[k];
            if (w && baseCol != null) w.color = Color.Lerp(crackedTint, baseCol[k], Mathf.Clamp01(hpRatio));
        }

        void Update()
        {
            if (basePos == null) return;
            var ws = Walls;
            for (int k = 0; k < 4; k++)
            {
                if (!ws[k] || shake[k] <= 0) continue;
                shake[k] = Mathf.Max(0, shake[k] - Time.deltaTime * 3);
                ws[k].transform.position = basePos[k] + new Vector3(Mathf.Sin(Time.time * 60) * shake[k] * shakeAmount * World.U, 0, 0);
            }
        }
    }
}
