using System.Collections.Generic;
using NKK.Data;
using NKK.Hazards;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Stage
{
    // 미니맵 (웹 drawMinimap): 화면 오른쪽 아래. 열린 방 + 지금 부술 수 있는 옆방(어둡게), 계단 · 보스 표시, 쥐 점, 카메라가 보는 곳 테두리.
    // 보스 층이면 계단 방이 안 열려도 보스 표시 (보스가 어디서 기다리는지 보이게).
    // 칸·점·아이콘은 씬 자식(꺼 둔 틀)을 복제해서 씀 → 크기·색은 인스펙터에서
    public class Minimap : MonoBehaviour
    {
        [Header("연결")]
        public StageManager Stage;
        public RatManager Rats;
        public GameManager Game;
        public Camera cam;
        [Tooltip("방 칸을 그리는 영역 (이 안에 가운데 정렬)")] public RectTransform area;
        [Tooltip("방 칸 틀 (꺼 둠)")] public Image cellTemplate;
        [Tooltip("쥐 점 틀 (꺼 둠)")] public Image dotTemplate;
        [Tooltip("계단 아이콘 (계단 방이 열리면)")] public RectTransform stairsIcon;
        [Tooltip("보스 아이콘 (보스가 살아 있으면 계단 방 / 전투 중엔 보스 위치)")] public RectTransform bossIcon;
        [Tooltip("카메라가 보는 곳 테두리 (9-슬라이스)")] public RectTransform viewFrame;
        [Tooltip("층 · 구간 이름 (자리표시 {floor} {zone})")] public TMP_Text title;

        [Header("모양")]
        [Tooltip("열린 방 · 안 열린 방 칸 그림 (비우면 틀 그림 그대로)")] public Sprite openSprite, lockedSprite;
        [Tooltip("방 칸 최대 크기 (px) · 세로 비율 (웹 0.7)")] public float maxCell = 46, cellAspect = 0.7f;
        [Tooltip("칸 사이 틈 (px)")] public float cellGap = 3;
        [Tooltip("열린 방 색 (구간 순서대로, 웹 ZONE_COL)")] public Color[] zoneColors = {
            new(0.902f, 0.910f, 0.890f), new(0.812f, 0.863f, 0.788f), new(0.557f, 0.541f, 0.518f), new(0.663f, 0.639f, 0.604f),
            new(0.788f, 0.765f, 0.722f), new(0.604f, 0.584f, 0.557f), new(0.890f, 0.847f, 0.776f) };
        [Tooltip("아직 안 열린 옆방 색")] public Color lockedColor = new(0.227f, 0.204f, 0.192f, 0.4f);
        [Tooltip("보스가 기다리는 계단 방 색 (안 열렸을 때)")] public Color bossRoomColor = new(0.55f, 0.22f, 0.24f, 0.75f);
        [Tooltip("쥐 점 최대 개수 (넘으면 일부만)")] public int maxDots = 200;
        [Tooltip("보스 아이콘 두근거림 (배율 · 빠르기)")] public float bossPulse = 0.12f, bossPulseSpeed = 5;
        [Tooltip("갱신 간격 (초)")] public float interval = 0.05f;

        readonly List<Image> cells = new(), dots = new();
        string titleFormat;
        float t;
        int i0, j0, cols, rows;
        float cs, ch, ox, oy;
        Vector3 bossBase = Vector3.one;

        void Awake()
        {
            if (title) titleFormat = title.text;
            if (cellTemplate) cellTemplate.gameObject.SetActive(false);
            if (dotTemplate) dotTemplate.gameObject.SetActive(false);
            if (bossIcon) bossBase = bossIcon.localScale;
            if (!cam) cam = Camera.main;
        }

        // 게임 좌표 → 영역 안 좌표 (왼쪽 위 기준, UI 는 위가 +)
        Vector2 P(float x, float y) => new(ox + (x / World.RW - i0 + 0.5f) * cs, oy - (y / World.RH - j0 + 0.5f) * ch);

        void Layout()
        {
            int a = int.MaxValue, b = int.MaxValue, c = int.MinValue, d = int.MinValue;
            foreach (var k in Stage.Layout) { a = Mathf.Min(a, k.x); b = Mathf.Min(b, k.y); c = Mathf.Max(c, k.x); d = Mathf.Max(d, k.y); }
            if (a == int.MaxValue) { a = b = c = d = 0; }
            i0 = a; j0 = b; cols = c - a + 2; rows = d - b + 2;      // 바깥 여백 반 칸씩
            var r = area.rect;
            cs = Mathf.Min(r.width / cols, r.height / (rows * cellAspect), maxCell); ch = cs * cellAspect;
            ox = r.xMin + (r.width - cols * cs) / 2; oy = r.yMax - (r.height - rows * ch) / 2;
        }

        Image Get(List<Image> pool, Image tpl, int n)
        {
            while (pool.Count <= n) pool.Add(Instantiate(tpl, area));
            var im = pool[n]; if (!im.gameObject.activeSelf) im.gameObject.SetActive(true);
            return im;
        }

        static void Place(RectTransform rt, Vector2 p) { if (rt) rt.anchoredPosition = p; }

        void LateUpdate()
        {
            if (!Stage || !area || !cellTemplate) return;
            if ((t -= Time.unscaledDeltaTime) > 0) return;
            t = interval;
            Layout();
            int f = Game ? Game.Floor : 1;
            var zone = GameDatabase.Instance ? GameDatabase.Instance.ZoneOf(f) : null;
            if (title) title.text = (titleFormat ?? "{floor}층 · {zone}").Replace("{floor}", f.ToString()).Replace("{zone}", zone?.zone_name ?? "");
            int zi = 0;
            if (zone != null && GameDatabase.Instance) zi = Mathf.Max(0, GameDatabase.Instance.Zones.IndexOf(zone));
            Color openCol = zoneColors.Length > 0 ? zoneColors[zi % zoneColors.Length] : Color.white;

            var boss = Stage.Boss;
            bool bossAlive = boss && (boss.State == Boss.BState.Wait || boss.State == Boss.BState.Fight) && !boss.Test;
            bool stairsOpen = Stage.Open.Contains(Stage.StairsRoom);

            // 방 칸: 열린 방 + 열린 방 옆 안 열린 방 (+ 보스가 기다리는 계단 방)
            int n = 0;
            foreach (var k in Stage.Layout)
            {
                bool open = Stage.Open.Contains(k), side = false;
                if (!open) foreach (var dd in StageManager.Dirs) if (Stage.Open.Contains(k + dd)) side = true;
                bool bossRoom = !open && bossAlive && k == Stage.StairsRoom;
                if (!open && !side && !bossRoom) continue;
                var im = Get(cells, cellTemplate, n++);
                im.color = open ? openCol : bossRoom ? bossRoomColor : lockedColor;
                var spr = open ? openSprite : lockedSprite; if (spr && im.sprite != spr) im.sprite = spr;
                var rt = im.rectTransform;
                rt.sizeDelta = new Vector2(cs - cellGap, ch - cellGap);
                rt.anchoredPosition = P((k.x + 0.5f) * World.RW, (k.y + 0.5f) * World.RH);
            }
            for (int m = n; m < cells.Count; m++) if (cells[m].gameObject.activeSelf) cells[m].gameObject.SetActive(false);

            // 계단 · 보스
            var sc = P((Stage.StairsRoom.x + 0.5f) * World.RW, (Stage.StairsRoom.y + 0.5f) * World.RH);
            if (stairsIcon) { stairsIcon.gameObject.SetActive(stairsOpen && !bossAlive); Place(stairsIcon, sc); }
            if (bossIcon)
            {
                bossIcon.gameObject.SetActive(bossAlive);
                if (bossAlive)
                {
                    Place(bossIcon, boss.State == Boss.BState.Fight ? P(boss.x, boss.y) : sc);
                    bossIcon.localScale = bossBase * (1 + bossPulse * Mathf.Sin(Time.unscaledTime * bossPulseSpeed));
                }
            }

            // 쥐 점 (많으면 골고루 일부만)
            n = 0;
            if (Rats && dotTemplate)
            {
                int total = Rats.Rats.Count, step = Mathf.Max(1, Mathf.CeilToInt(total / (float)Mathf.Max(1, maxDots)));
                for (int q = 0; q < total; q += step)
                {
                    var r = Rats.Rats[q]; if (!r) continue;
                    Get(dots, dotTemplate, n++).rectTransform.anchoredPosition = P(r.x, r.y);
                }
            }
            for (int m = n; m < dots.Count; m++) if (dots[m].gameObject.activeSelf) dots[m].gameObject.SetActive(false);

            // 카메라가 보는 곳
            if (viewFrame && cam)
            {
                var c = World.FromUnity(cam.transform.position);
                float hw = cam.orthographicSize * cam.aspect / World.U, hh = cam.orthographicSize / World.U / World.TILT;
                Vector2 a = P(c.x - hw, c.y - hh), b = P(c.x + hw, c.y + hh);
                viewFrame.anchoredPosition = (a + b) / 2;
                viewFrame.sizeDelta = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
                viewFrame.SetAsLastSibling();
            }
            if (stairsIcon) stairsIcon.SetAsLastSibling();
            if (bossIcon) bossIcon.SetAsLastSibling();
        }
    }
}
