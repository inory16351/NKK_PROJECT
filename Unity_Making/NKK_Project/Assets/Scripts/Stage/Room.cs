using UnityEngine;

namespace NKK.Stage
{
    // 방 하나의 그림 (Room 프리팹). 바닥·벽 4개는 자식 스프라이트. 로직(열림·벽 체력)은 StageManager.
    // 좌표는 게임 단위: 방 (i, j) 의 왼쪽 위 = (i × RW, j × RH).
    // 벽 모양·앞뒤 순서는 웹게임 drawWallSeg 그대로:
    //  위·아래 벽 = 경계선에 서서 WALL_H 만큼 솟은 벽 (윗면 + 정면). 위 벽은 방 안 모든 것 뒤, 아래 벽은 방 안 모든 것 앞.
    //  왼·오른 벽 = 두께 WM×2 의 윗면 띠 (WALL_H 만큼 들려 보임) + 아래 끝 단면. 방 안 모든 것 뒤.
    //  벽은 열린 방에서 안 열린 쪽에만. 안 열린 방은 열린 방과 붙어 있으면 어둡게, 아니면 안 그림.
    public class Room : MonoBehaviour
    {
        public int i, j;
        [Header("그림 (자식)")]
        public SpriteRenderer floor;
        public SpriteRenderer wallTop, wallBottom, wallLeft, wallRight;
        [Tooltip("(선택) 왼·오른 벽 아래 끝 단면 (어두운 색). 비우면 안 그림")] public SpriteRenderer wallLeftEnd, wallRightEnd;
        [Tooltip("(선택) 벽 너머 잠긴 방 가운데 표시 (글자는 프리팹에서)")] public GameObject lockedMark;
        [Tooltip("뒷벽 높이 (게임 단위)")] public float wallHeight = 46;
        [Tooltip("벽 너머 잠긴 방 바닥 색 (웹: 어두운 남색 78% 덮기)")] public Color closedTint = new(0.3f, 0.28f, 0.36f);
        [Tooltip("벽 끝 단면 = 윗면 색 × 이 값")] public float endShade = 0.76f;
        [Tooltip("벽이 거의 부서졌을 때 색")] public Color crackedTint = new(0.85f, 0.55f, 0.45f);
        [Tooltip("맞을 때 흔들림 크기 (게임 단위)")] public float shakeAmount = 4;
        [Header("금 자국 (벽 체력 75%·45%·20% 아래)")]
        [Tooltip("금 자국 템플릿 (자식 SpriteRenderer, 꺼 둠). 벽마다 3개씩 복제")] public SpriteRenderer crackTemplate;
        [Tooltip("금 자국 그림 1·2·3단계 (흰색, crackColor 로 칠함)")] public Sprite[] crackSprites;
        [Tooltip("금 자국 색 (벽 위에 곱해 보이게 어둡게)")] public Color crackColor = new(0.25f, 0.22f, 0.24f, 0.75f);
        [Tooltip("단계가 되는 남은 체력 비율")] public float[] crackAt = { 0.75f, 0.45f, 0.2f };
        SpriteRenderer[,] cracks;
        readonly int[] crackStage = new int[4];

        const int FloorOrder = -30000, SideOrder = -29500, EndOrder = -29400, TopOrder = -29000;

        public float Left => i * World.RW;
        public float Top => j * World.RH;
        public Vector2 Center => new(Left + World.RW / 2, Top + World.RH / 2);

        // 층 구간 그림: 바닥 타일 · 벽 정면 타일 (위·아래 벽) · 벽 윗면 색 (옆 벽) (Layout 전에 호출)
        public void ApplyLook(Sprite floorTile, Sprite wallFace, Color wallCap)
        {
            if (floor && floorTile) floor.sprite = floorTile;
            foreach (var w in new[] { wallTop, wallBottom }) if (w && wallFace) { w.sprite = wallFace; w.color = Color.white; }
            foreach (var w in new[] { wallLeft, wallRight }) if (w) w.color = wallCap;
            var end = new Color(wallCap.r * endShade, wallCap.g * endShade, wallCap.b * endShade, wallCap.a);
            foreach (var w in new[] { wallLeftEnd, wallRightEnd }) if (w) w.color = end;
        }

        Color[] baseCol;
        Vector3[] basePos;
        readonly float[] shake = new float[4];

        // 0 위 · 1 아래 · 2 왼 · 3 오른 · 4 왼 끝 · 5 오른 끝 (끝은 옆 벽과 함께 흔들림)
        SpriteRenderer[] Parts => new[] { wallTop, wallBottom, wallLeft, wallRight, wallLeftEnd, wallRightEnd };

        // 서 있는 벽 (위·아래): 경계선 by 에 받침, 윗면까지 wallHeight + WM×TILT 위로. 정면 그림 세로를 높이에 맞춰 늘림
        void PlaceFace(SpriteRenderer r, float cx, float by, float w, int order)
        {
            if (!r) return;
            float capT = World.WM * World.TILT, H = wallHeight + capT * 2;
            float sh = r.sprite ? r.sprite.bounds.size.y : H * World.U;
            r.transform.localScale = new Vector3(1, H * World.U / sh, 1);
            r.size = new Vector2(w, sh);
            var bot = World.ToUnity(cx, by); bot.y -= capT * World.U;
            r.transform.position = bot + new Vector3(0, H * World.U / 2, 0);
            r.sortingOrder = order;
        }

        // 옆 벽 띠: 위는 방 위 경계 - wallHeight (위 벽이 있으면 그 윗면 끝까지), 아래는 방 아래 경계 - wallHeight (그 아래는 끝 단면)
        void PlaceSide(SpriteRenderer r, SpriteRenderer end, float x, bool topWall)
        {
            float capT = World.WM * World.TILT, wm = World.WM * 2 * World.U;
            float top = Top * World.TILT - wallHeight - (topWall ? capT : 0), bot = (Top + World.RH) * World.TILT, mid = bot - wallHeight;
            if (r)
            {
                r.transform.localScale = Vector3.one;
                r.size = new Vector2(wm, (mid - top) * World.U);
                r.transform.position = new Vector3(x * World.U, -(top + mid) / 2 * World.U, 0);
                r.sortingOrder = SideOrder;
            }
            if (end)
            {
                end.transform.localScale = Vector3.one;
                end.size = new Vector2(wm, wallHeight * World.U);
                end.transform.position = new Vector3(x * World.U, -(mid + bot) / 2 * World.U, 0);
                end.sortingOrder = EndOrder;
            }
        }

        [ContextMenu("Layout")]
        public void Layout()
        {
            var c = World.ToUnity(Left + World.RW / 2, Top + World.RH / 2);
            float w = World.RW * World.U, h = World.RH * World.TILT * World.U;
            if (floor) { floor.transform.position = c; floor.size = new Vector2(w, h); floor.sortingOrder = FloorOrder; }
            if (lockedMark) lockedMark.transform.position = c;
            PlaceFace(wallTop, Center.x, Top, w, TopOrder);
            PlaceFace(wallBottom, Center.x, Top + World.RH, w, World.SortOrder(Top + World.RH + 1));
            PlaceSide(wallLeft, wallLeftEnd, Left, true);
            PlaceSide(wallRight, wallRightEnd, Left + World.RW, true);
            var ps = Parts;
            baseCol = new Color[ps.Length]; basePos = new Vector3[ps.Length];
            for (int k = 0; k < ps.Length; k++) if (ps[k]) { baseCol[k] = ps[k].color; basePos[k] = ps[k].transform.position; }
        }

        // open = 열린 방, peek = 안 열렸지만 열린 방과 붙음 (어둡게 보임). 벽 4개 (위·아래·왼쪽·오른쪽) 표시.
        // leftEnd/rightEnd = 옆 벽 아래 끝 단면 (아래 방에서 같은 쪽 벽이 이어지면 false → 이음매 없이)
        public void SetState(bool open, bool peek, bool top, bool bottom, bool left, bool right, bool leftEnd, bool rightEnd)
        {
            if (basePos == null) Layout();
            if (floor) { floor.enabled = open || peek; floor.color = open ? Color.white : closedTint; }
            if (lockedMark) lockedMark.SetActive(!open && peek);
            PlaceSide(wallLeft, wallLeftEnd, Left, top);
            PlaceSide(wallRight, wallRightEnd, Left + World.RW, top);
            var ps = Parts; var on = new[] { top, bottom, left, right, left && leftEnd, right && rightEnd };
            for (int k = 0; k < ps.Length; k++) if (ps[k])
                {
                    ps[k].enabled = on[k];                                   // 색(금 간 정도)은 유지
                    if (k < 2) ps[k].transform.position = basePos[k]; else basePos[k] = ps[k].transform.position;
                }
            for (int k = 0; k < 4; k++) shake[k] = 0;
            // 벽이 사라졌으면(방이 열림) 금 자국도
            if (cracks != null) for (int k = 0; k < 4; k++) if (!on[k]) { crackStage[k] = 0; for (int n = 0; n < 3; n++) if (cracks[k, n]) cracks[k, n].gameObject.SetActive(false); }
        }

        // 벽 피격: 흔들림 + 남은 체력만큼 붉어짐 (di,dj = 맞은 방향)
        public void ShakeWall(int di, int dj, float hpRatio)
        {
            int k = dj < 0 ? 0 : dj > 0 ? 1 : di < 0 ? 2 : 3;
            SetCracks(k, hpRatio);
            shake[k] = Mathf.Min(1, shake[k] + 0.15f);
            var ps = Parts;
            if (baseCol == null) return;
            float t = Mathf.Clamp01(hpRatio);
            if (ps[k]) ps[k].color = Color.Lerp(crackedTint, baseCol[k], t);
            if (k >= 2 && ps[k + 2]) ps[k + 2].color = Color.Lerp(crackedTint * baseCol[k + 2], baseCol[k + 2], t);
        }

        // 벽 k (0 위 · 1 아래 · 2 왼 · 3 오른) 금 자국: 단계 1 = 가운데 작은 금, 2 = 중간 금 + 작은 금, 3 = 큰 금 + 중간 금 2개
        void SetCracks(int k, float hpRatio)
        {
            if (!crackTemplate || crackSprites == null || crackSprites.Length < 3) return;
            int st = 0; for (int s = 0; s < crackAt.Length; s++) if (hpRatio < crackAt[s]) st = s + 1;
            if (st == crackStage[k]) return;
            crackStage[k] = st;
            if (cracks == null) { cracks = new SpriteRenderer[4, 3]; crackTemplate.gameObject.SetActive(false); }
            var w = Parts[k];
            int[][] pick = { new int[0], new[] { 0 }, new[] { 1, 0 }, new[] { 2, 1, 1 } };
            float[] along = { 0, -0.27f, 0.27f };
            for (int n = 0; n < 3; n++)
            {
                var c = cracks[k, n];
                bool on = w && w.enabled && n < pick[st].Length;
                if (!on) { if (c) c.gameObject.SetActive(false); continue; }
                if (!c) { c = cracks[k, n] = Instantiate(crackTemplate, transform); c.name = $"Crack_{k}_{n}"; }
                c.gameObject.SetActive(true);
                c.sprite = crackSprites[pick[st][n]]; c.color = crackColor;
                var b = w.bounds; bool side = k >= 2;
                float len = side ? b.size.y : b.size.x, thick = side ? b.size.x : b.size.y;
                var pos = b.center + (side ? new Vector3(0, along[n] * len, 0) : new Vector3(along[n] * len, 0, 0));
                float size = Mathf.Min(thick * (side ? 3.2f : 1.5f), len * 0.4f) * (n == 0 ? 1 : 0.8f);
                c.transform.position = pos;
                c.transform.rotation = Quaternion.Euler(0, 0, side ? 90 : 0);
                float sw = c.sprite.bounds.size.x; c.transform.localScale = Vector3.one * size / Mathf.Max(0.01f, sw);
                c.sortingOrder = w.sortingOrder + 1;
            }
        }

        void Update()
        {
            if (basePos == null) return;
            var ps = Parts;
            for (int k = 0; k < 4; k++)
            {
                if (shake[k] <= 0) continue;
                shake[k] = Mathf.Max(0, shake[k] - Time.deltaTime * 2);
                // 웹: 옆 벽은 좌우로, 위·아래 벽은 위아래로 반만큼
                float jit = Mathf.Sin(Time.time * 60) * shake[k] * shakeAmount * World.U;
                var off = k >= 2 ? new Vector3(jit, 0, 0) : new Vector3(0, jit * 0.5f, 0);
                if (ps[k]) ps[k].transform.position = basePos[k] + off;
                if (k >= 2 && ps[k + 2]) ps[k + 2].transform.position = basePos[k + 2] + off;
            }
        }
    }
}
