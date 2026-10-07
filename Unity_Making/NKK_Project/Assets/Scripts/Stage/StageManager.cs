using System;
using System.Collections.Generic;
using NKK.Data;
using NKK.Hazards;
using NKK.Items;
using NKK.Rats;
using UnityEngine;

namespace NKK.Stage
{
    // 층(스테이지): 방 배치(층 번호 시드) · 열린 방 · 벽 체력 · 계단 · 적정 전투력 (웹게임 stage.js / 방 격자 / 벽 이식).
    // 방 (i, j) 의 왼쪽 위 = (i × RW, j × RH) 게임 단위. 시작 방 (0, 0), 계단 방 = 시작 방에서 가장 먼 방.
    public class StageManager : MonoBehaviour
    {
        [System.Serializable]
        public class ZoneLook
        {
            [Tooltip("ItemTable Zone 의 zone_id")] public int zoneId;
            public string note;
            public Sprite floor, wallFace;
            [Tooltip("옆·아래 벽 윗면 색")] public Color wallCap = Color.gray;
        }

        [Header("연결")]
        public Room roomPrefab;
        public Transform roomRoot;
        public SpriteRenderer stairs;
        [Tooltip("층 구간별 바닥·벽 그림 (zone_id 로 찾음, 없으면 프리팹 그대로)")] public ZoneLook[] zoneLooks;
        public GameManager Game;
        public ItemManager Items;
        public RatManager Rats;
        public CatManager Cats;
        [Tooltip("층 클리어 연출 (연구 자료를 훔쳤다!!!). 비우면 바로 다음 층")] public Heist Heist;
        [Tooltip("층 보스 (스테이지 테이블 Boss). 보스 층이면 계단 방에서 대기, 살아 있는 동안 계단 못 씀")] public Boss Boss;

        [Header("방 배치")]
        [Tooltip("방 수 = min(최대, 기본 + 층 × 증가) (+ 보스 층 1)")] public int roomBase = 3;
        public float roomPerFloor = 0.6f;
        public int roomMax = 9;
        [Tooltip("세로로 뻗을 수 있는 최대 칸")] public int maxRow = 3;
        public int bossEvery = 5;

        [Header("적정 전투력 (찍찍!!) = 기본 × 증가^(층-1) × 초반 보정")]
        public float powNeed0 = 2000;
        public float powNeedGrow = 3.3f;
        public float[] powEarly = { 0.2f, 0.45f, 0.75f };

        [Header("벽 체력 = 적정 전투력 × 배율")]
        [Tooltip("계단 방 벽: min(최대, 2 + 6 × (층-1))")] public float wallPowStairs = 20;
        [Tooltip("일반 벽 × (1 + 0.25 × 시작 방과의 거리)")] public float wallPow = 1.5f;
        [Tooltip("전투력이 적정보다 낮으면 벽 피해 = (전투력÷적정)^지수")] public float wallGateStairs = 1.5f;
        public float wallGate = 0.5f;

        [Header("벽 체력바 (웹 drawWallBar)")]
        [Tooltip("월드 캔버스 안 템플릿 (꺼 둬도 됨). 부술 수 있는 막힌 벽마다 하나씩 복제")] public WallBar wallBarTemplate;
        [Tooltip("벽 윗면 위로 띄우는 높이 (게임 단위)")] public float wallBarLift = 14;
        [Tooltip("켜면 화면 확대·축소해도 화면에서 같은 크기 (웹과 같음)")] public bool wallBarFixedSize = true;

        [Header("쥐덫 등장")]
        public Trap trapPrefab;
        public Transform trapRoot;
        [Tooltip("이 층부터 방이 열릴 때마다 쥐덫")] public int trapFromFloor = 2;
        [Tooltip("방마다 1개, 이 확률로 2개")] public float trapTwoChance = 0.35f;
        [Tooltip("발동 반경 · 그림 폭 (게임 단위)")] public float trapRadius = 28, trapWidth = 84;
        [Tooltip("걸린 쥐 기절 (초) · 다시 장전 (초)")] public float trapStun = 3, trapReload = 8;

        [Header("고양이 등장")]
        [Tooltip("이 층부터 고양이가 나옴")] public int catFromFloor = 2;
        [Tooltip("층에 들어온 뒤 첫 등장 (초, 최소~최대)")] public Vector2 catFirstDelay = new(35, 60);
        [Tooltip("다음 등장까지 (초, 최소~최대)")] public Vector2 catInterval = new(55, 85);
        [Tooltip("이 층부터 특별 복장 고양이(마녀·우주복)가 가끔")] public int specialCatFromFloor = 25;
        [Tooltip("특별 고양이가 나올 확률")] public float specialCatChance = 0.25f;

        [Header("계단")]
        [Tooltip("계단 위치 = 방 위쪽에서 이만큼 아래")] public float stairsY = 190;
        public Vector2 stairsTouch = new(110, 60);
        public float stairsWidth = 230;

        public static readonly Vector2Int[] Dirs = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        public readonly HashSet<Vector2Int> Layout = new();
        public readonly HashSet<Vector2Int> Open = new();
        public Vector2Int StairsRoom { get; private set; }
        public float Power { get; private set; }
        public event Action<Vector2Int> RoomOpened;
        public event Action FloorEntered;          // 층 시작 (제한시간 채우기 등)
        public bool Climbing => climbing;          // 계단 → 다음 층 페이드 중

        readonly Dictionary<string, float> walls = new();
        readonly List<Trap> traps = new();
        float catT;
        readonly Dictionary<Vector2Int, Room> rooms = new();
        float powT;
        bool climbing;

        public bool IsOpen(int i, int j) => Open.Contains(new Vector2Int(i, j));
        public bool InLayout(int i, int j) => Layout.Contains(new Vector2Int(i, j));
        public static Vector2Int RoomOf(float x, float y) => new(Mathf.FloorToInt(x / World.RW), Mathf.FloorToInt(y / World.RH));
        public static int RoomDist(Vector2Int r) => Mathf.Abs(r.x) + Mathf.Abs(r.y);
        public bool IsStairsRoom(int i, int j) => i == StairsRoom.x && j == StairsRoom.y;
        public bool IsBossFloor(int f) => f % bossEvery == 0;
        public Vector2 StairsPos => new((StairsRoom.x + 0.5f) * World.RW, StairsRoom.y * World.RH + stairsY);

        // 층 밸런스 = 스테이지 테이블 Stage (없으면 인스펙터 옛 수식)
        StageRow Row(int f) => GameDatabase.Instance ? GameDatabase.Instance.StageOf(f) : null;
        public float PowNeed(int f) { var r = Row(f); return r != null ? r.pow_need : powNeed0 * Mathf.Pow(powNeedGrow, f - 1) * (f - 1 < powEarly.Length ? powEarly[f - 1] : 1); }
        public float ItemHpK(int f) { var r = Row(f); return r != null ? r.item_hp : Mathf.Pow(3.6f, f - 1); }
        public float CheeseK(int f) { var r = Row(f); return r != null ? r.cheese : Mathf.Pow(1.8f, f - 1); }
        public float TimeAdd(int f) { var r = Row(f); return r != null ? r.time_add : 0; }

        // ── 층 생성 ──
        void GenLayout(int f)
        {
            var rnd = new SeededRandom((uint)(f * 7919 + 17));
            var sr = Row(f);
            int n = sr != null && sr.rooms > 0 ? sr.rooms : Mathf.Min(roomMax, roomBase + Mathf.FloorToInt(f * roomPerFloor)) + (IsBossFloor(f) ? 1 : 0);
            var list = new List<Vector2Int> { Vector2Int.zero };
            Layout.Clear(); Layout.Add(Vector2Int.zero);
            int guard = 0;
            while (list.Count < n && guard++ < 500)
            {
                var b = rnd.Next() < 0.6 ? list[^1] : list[Mathf.FloorToInt(rnd.Next() * list.Count)];
                var k = b + Dirs[Mathf.FloorToInt(rnd.Next() * 4)];
                if (Layout.Contains(k) || Mathf.Abs(k.y) > maxRow) continue;
                Layout.Add(k); list.Add(k);
            }
            // 계단 방 = 방 이동 횟수가 가장 먼 방
            var dist = new Dictionary<Vector2Int, int> { [Vector2Int.zero] = 0 };
            var q = new Queue<Vector2Int>(); q.Enqueue(Vector2Int.zero);
            while (q.Count > 0) { var c = q.Dequeue(); foreach (var d in Dirs) { var k = c + d; if (Layout.Contains(k) && !dist.ContainsKey(k)) { dist[k] = dist[c] + 1; q.Enqueue(k); } } }
            var best = Vector2Int.zero; foreach (var kv in dist) if (kv.Value > dist[best]) best = kv.Key;
            StairsRoom = best;
        }

        Rat wallBy;
        bool entered;
        public void EnterFloor(int f)
        {
            Rats.Ults?.CancelAll();
            if (entered) Rats.Ults?.ChargeAll(CondType.Stage_Clear);           // 층 통과 → 있는 종 전부
            entered = true;
            Game.Floor = Mathf.Max(1, f);
            GenLayout(Game.Floor);
            Open.Clear(); Open.Add(Vector2Int.zero); walls.Clear();
            foreach (var r in rooms.Values) if (r) Destroy(r.gameObject);
            rooms.Clear();
            foreach (var k in Layout) MakeRoom(k);
            RefreshWalls();
            if (stairs) { var sp = StairsPos; stairs.transform.position = World.ToUnity(sp.x, sp.y + 30); stairs.sortingOrder = World.SortOrder(sp.y - 40); float w = stairsWidth * World.U; stairs.transform.localScale = Vector3.one * (stairs.sprite ? w / stairs.sprite.bounds.size.x : 1); }
            Items.ClearAll();
            foreach (var tp in traps) if (tp) Destroy(tp.gameObject);
            traps.Clear();
            if (Cats) Cats.Clear();
            catT = UnityEngine.Random.Range(catFirstDelay.x, catFirstDelay.y);
            Items.FurnishRoom(Vector2Int.zero);
            Items.FillRoom(Vector2Int.zero, Items.RoomCap);
            Items.OnFloorStart();
            Rats.PlaceAll(World.RW / 2, World.RH / 2);
            if (Game.cam) Game.cam.CenterOn(World.RW / 2, World.RH / 2);
            Game.ShowBanner($"{Game.Floor}층 · {GameDatabase.Instance.ZoneOf(Game.Floor)?.zone_name}", "계단 방 벽을 부숴라!");
            if (Boss) Boss.OnFloorEnter();                                     // 보스 층: 계단 방에 보스 대기 + 배너 부제
            climbing = false;
            FloorEntered?.Invoke();
        }

        void MakeRoom(Vector2Int k)
        {
            var r = Instantiate(roomPrefab, roomRoot ? roomRoot : transform);
            r.name = $"Room_{k.x}_{k.y}"; r.i = k.x; r.j = k.y;
            var zone = GameDatabase.Instance.ZoneOf(Game.Floor);
            if (zone != null && zoneLooks != null) foreach (var z in zoneLooks) if (z.zoneId == zone.zone_id) { r.ApplyLook(z.floor, z.wallFace, z.wallCap); break; }
            r.Layout();
            rooms[k] = r;
        }

        // 벽 그림 (웹 visibleWalls): 열린 방에서 안 열린 쪽에만 벽. 열린 방끼리는 벽 없이 한 공간으로 이어짐.
        // 안 열린 방은 열린 방과 붙어 있으면 어둡게, 아니면 안 그림
        void RefreshWalls()
        {
            var up = new Vector2Int(0, -1); var down = new Vector2Int(0, 1); var left = new Vector2Int(-1, 0); var right = new Vector2Int(1, 0);
            foreach (var kv in rooms)
            {
                var k = kv.Key; bool open = Open.Contains(k), peek = false;
                foreach (var d in Dirs) if (Open.Contains(k + d)) peek = true;
                bool Wall(Vector2Int at, Vector2Int d) => Open.Contains(at) && !Open.Contains(at + d);
                // 아래 방에서 같은 쪽 옆 벽이 이어지면 끝 단면 생략
                kv.Value.SetState(open, peek, Wall(k, up), Wall(k, down), Wall(k, left), Wall(k, right), !Wall(k + down, left), !Wall(k + down, right));
            }
        }

        // ── 가두기 (웹게임 confine): 열린 방 안. 막힌 벽에 닿으면 onWall(i, j, di, dj, 속도) ──
        public bool Confine(ref float x, ref float y, ref float vx, ref float vy, float rad, float px, float py, float bounce, Action<int, int, int, int, float> onWall = null)
        {
            var c = RoomOf(px, py);
            if (!Open.Contains(c)) { c = RoomOf(x, y); if (!Open.Contains(c)) return false; }
            float L = c.x * World.RW, T = c.y * World.RH, WM = World.WM;
            bool hit = false;
            if (!IsOpen(c.x - 1, c.y) && x < L + WM + rad) { x = L + WM + rad; if (vx < 0) { onWall?.Invoke(c.x, c.y, -1, 0, -vx); vx = -vx * bounce; } hit = true; }
            if (!IsOpen(c.x + 1, c.y) && x > L + World.RW - WM - rad) { x = L + World.RW - WM - rad; if (vx > 0) { onWall?.Invoke(c.x, c.y, 1, 0, vx); vx = -vx * bounce; } hit = true; }
            if (!IsOpen(c.x, c.y - 1) && y < T + WM + rad) { y = T + WM + rad; if (vy < 0) { onWall?.Invoke(c.x, c.y, 0, -1, -vy); vy = -vy * bounce; } hit = true; }
            if (!IsOpen(c.x, c.y + 1) && y > T + World.RH - WM - rad) { y = T + World.RH - WM - rad; if (vy > 0) { onWall?.Invoke(c.x, c.y, 0, 1, vy); vy = -vy * bounce; } hit = true; }
            if (!Open.Contains(RoomOf(x, y))) { x = px; y = py; vx = -vx * bounce; vy = -vy * bounce; hit = true; }   // 대각선 모서리
            return hit;
        }

        // ── 벽 ──
        static string WallKey(int i, int j, int di, int dj) => di != 0 ? $"v,{i + (di > 0 ? 1 : 0)},{j}" : $"h,{i},{j + (dj > 0 ? 1 : 0)}";

        float WallMax(int ti, int tj) => WallMaxBase(ti, tj) * CommonSkill.WallHpMul;     // 공용 스킬 벽 체력 감소
        float WallMaxBase(int ti, int tj)
        {
            int f = Game.Floor;
            var sr = Row(f);
            if (IsStairsRoom(ti, tj)) return PowNeed(f) * (sr != null ? sr.wall_stairs : Mathf.Min(wallPowStairs, 2 + 6 * (f - 1)));
            return PowNeed(f) * (sr != null ? sr.wall_normal : wallPow) * (1 + 0.25f * RoomDist(new Vector2Int(ti, tj)));
        }

        public float WallHP(int i, int j, int di, int dj) => walls.TryGetValue(WallKey(i, j, di, dj), out var v) ? v : WallMax(i + di, j + dj);
        public float WallHPMax(int i, int j, int di, int dj) => WallMax(i + di, j + dj);

        float Gate(int ti, int tj)
        {
            float k = Mathf.Clamp(Power / PowNeed(Game.Floor), 0.001f, 1);
            return Mathf.Pow(k, IsStairsRoom(ti, tj) ? wallGateStairs : wallGate);
        }

        // 슈퍼 점프: 이 사각형(게임 좌표)에 걸친 막힌 벽 (열린 방 → 레이아웃 안 안 열린 방 쪽)
        public List<(int i, int j, int di, int dj)> WallsIn(Rect r)
        {
            var l = new List<(int, int, int, int)>();
            foreach (var k in Open)
                foreach (var d in Dirs)
                {
                    var t = k + d;
                    if (Open.Contains(t) || !Layout.Contains(t)) continue;
                    float cx = (k.x + 0.5f + d.x * 0.5f) * World.RW, cy = (k.y + 0.5f + d.y * 0.5f) * World.RH;
                    bool hit = d.x != 0 ? cx > r.xMin && cx < r.xMax && (k.y + 1) * World.RH > r.yMin && k.y * World.RH < r.yMax
                                        : cy > r.yMin && cy < r.yMax && (k.x + 1) * World.RW > r.xMin && k.x * World.RW < r.xMax;
                    if (hit) l.Add((k.x, k.y, d.x, d.y));
                }
            return l;
        }
        public void ShakeWall(int i, int j, int di, int dj) { if (rooms.TryGetValue(new Vector2Int(i, j), out var room)) room.ShakeWall(di, dj, WallHP(i, j, di, dj) / WallMax(i + di, j + dj)); }
        // 슈퍼 점프: 계단 방 벽은 최대 체력의 25%만 (층 넘어가기는 쥐들이 직접), 나머지는 한 방에
        public void SuperBreakWall(int i, int j, int di, int dj, Rat by)
        {
            if (by) wallBy = by;
            if (IsStairsRoom(i + di, j + dj)) { var k = WallKey(i, j, di, dj); float hp = WallHP(i, j, di, dj) - WallMax(i + di, j + dj) * 0.25f; walls[k] = hp; ShakeWall(i, j, di, dj); if (hp <= 0) BreakWall(i, j, di, dj); }
            else BreakWall(i, j, di, dj);
        }

        public void DamageWall(int i, int j, int di, int dj, float dmg, Rat by = null)
        {
            if (by) wallBy = by;
            if (!InLayout(i + di, j + dj)) return;               // 연구소 바깥벽은 못 부숨
            dmg *= Gate(i + di, j + dj);
            var k = WallKey(i, j, di, dj);
            float hp = WallHP(i, j, di, dj) - dmg;
            walls[k] = hp;
            if (rooms.TryGetValue(new Vector2Int(i, j), out var room)) room.ShakeWall(di, dj, hp / WallMax(i + di, j + dj));
            if (hp <= 0) BreakWall(i, j, di, dj);
        }

        void BreakWall(int i, int j, int di, int dj)
        {
            walls.Remove(WallKey(i, j, di, dj));
            var t = new Vector2Int(i + di, j + dj);
            if (Open.Contains(t)) return;
            Open.Add(t);
            if (wallBy) Rats.Ults?.Charge(wallBy, CondType.Destroy_Wall);      // 벽 붕괴 → 마지막으로 친 쥐 종 필살기 게이지
            foreach (var d in Dirs) if (Open.Contains(t + d)) walls.Remove(WallKey(t.x, t.y, d.x, d.y));
            RefreshWalls();
            Game.ShowBanner("벽 붕괴! 방 확장", $"{Game.Floor}층 · 방 {Open.Count}/{Layout.Count}");
            if (!IsStairsRoom(t.x, t.y)) Items.FurnishRoom(t);
            Items.FillRoom(t, Mathf.CeilToInt(Items.RoomCap / 2f));
            if (IsStairsRoom(t.x, t.y))
            {
                if (Boss && Boss.State == Boss.BState.Wait) Boss.StartFight();   // 보스 전투 시작
                else Game.ShowBanner("계단 발견!", $"계단에 닿으면 {Game.Floor + 1}층으로");
            }
            Items.OnRoomOpened(t);
            // 쥐덫: 공용 스킬로 등장 확률(기본 100%)·2개 확률을 줄임
            if (!IsStairsRoom(t.x, t.y) && Game.Floor >= trapFromFloor && UnityEngine.Random.value < CommonSkill.TrapSingleChance)
                SpawnTraps(t, UnityEngine.Random.value < trapTwoChance - CommonSkill.TrapMultiDown ? 2 : 1);
            RoomOpened?.Invoke(t);
        }

        // ── 쥐덫 ──
        void SpawnTraps(Vector2Int room, int n)
        {
            if (!trapPrefab) return;
            for (int m = 0; m < n; m++)
            {
                var tp = Instantiate(trapPrefab, trapRoot ? trapRoot : transform);
                tp.Init((room.x + 0.5f) * World.RW + UnityEngine.Random.Range(-World.RW * 0.35f, World.RW * 0.35f), (room.y + 0.5f) * World.RH + UnityEngine.Random.Range(-World.RH * 0.3f, World.RH * 0.3f), trapWidth);
                traps.Add(tp);
            }
        }

        public Trap NearestArmedTrap(float x, float y, float maxD)
        {
            Trap best = null; float bd = maxD;
            foreach (var tp in traps) { if (!tp.Armed) continue; float d = Vector2.Distance(new Vector2(tp.x, tp.y), new Vector2(x, y)); if (d < bd) { bd = d; best = tp; } }
            return best;
        }

        // ── 고양이: 평소엔 실제 품종만, 특별 복장 고양이는 아주 후반에 가끔 ──
        void UpdateCats(float dt)
        {
            if (!Cats || Game.Floor < catFromFloor || Cats.Current || (Boss && Boss.CanHit)) return;     // 보스전 중엔 고양이 없음
            if ((catT -= dt) > 0) return;
            catT = UnityEngine.Random.Range(catInterval.x, catInterval.y);
            var normal = new List<CatCharacterRow>(); var special = new List<CatCharacterRow>();
            foreach (var c in GameDatabase.Instance.Cats.Values) (c.Category == CatCategory.Special ? special : normal).Add(c);
            bool sp = Game.Floor >= specialCatFromFloor && special.Count > 0 && UnityEngine.Random.value < specialCatChance;
            var pool = sp ? special : normal;
            pool.RemoveAll(c => Game.Floor < c.spawn_floor && !sp);
            if (pool.Count > 0) Cats.Spawn(pool[UnityEngine.Random.Range(0, pool.Count)]);
        }

        // 아직 안 열린 이웃 중 부술 수 있는 쪽
        public List<Vector2Int> ClosedSides(int i, int j)
        {
            var l = new List<Vector2Int>();
            foreach (var d in Dirs) if (!IsOpen(i + d.x, j + d.y) && InLayout(i + d.x, j + d.y)) l.Add(d);
            return l;
        }

        public Rect OpenBounds()
        {
            int i0 = int.MaxValue, j0 = int.MaxValue, i1 = int.MinValue, j1 = int.MinValue;
            foreach (var k in Open) { i0 = Mathf.Min(i0, k.x); j0 = Mathf.Min(j0, k.y); i1 = Mathf.Max(i1, k.x); j1 = Mathf.Max(j1, k.y); }
            return Rect.MinMaxRect(i0 * World.RW, j0 * World.RH, (i1 + 1) * World.RW, (j1 + 1) * World.RH);
        }

        void Start()
        {
            if (wallBarTemplate) { wallBarScale = wallBarTemplate.transform.localScale; wallBarTemplate.gameObject.SetActive(false); }
            EnterFloor(Game.Floor);
        }

        // ── 벽 체력바: 열린 방 → 레이아웃 안 안 열린 방 쪽 벽마다, 벽 가운데 윗면 위 (연구소 바깥벽은 없음) ──
        readonly List<WallBar> wallBars = new();
        Vector3 wallBarScale = Vector3.one;
        void LateUpdate()
        {
            int n = 0;
            if (wallBarTemplate)
            {
                var cam = Game ? Game.cam : null;
                float inv = wallBarFixedSize && cam ? 1 / Mathf.Max(0.01f, cam.zoom * cam.ultZoom) : 1;
                float lift = (roomPrefab ? roomPrefab.wallHeight : 46) + wallBarLift;
                foreach (var k in Open)
                    foreach (var d in Dirs)
                    {
                        var t = k + d;
                        if (Open.Contains(t) || !Layout.Contains(t)) continue;
                        if (n == wallBars.Count) wallBars.Add(Instantiate(wallBarTemplate, wallBarTemplate.transform.parent));
                        var b = wallBars[n++];
                        if (!b.gameObject.activeSelf) b.gameObject.SetActive(true);
                        float cx = (k.x + 0.5f + d.x * 0.5f) * World.RW, cy = (k.y + 0.5f + d.y * 0.5f) * World.RH;
                        b.transform.position = World.ToUnity(cx, cy, lift);
                        b.transform.localScale = wallBarScale * inv;
                        float hp = WallHP(k.x, k.y, d.x, d.y);
                        b.Set(hp, Mathf.Clamp01(hp / WallMax(t.x, t.y)));
                    }
            }
            for (; n < wallBars.Count; n++) if (wallBars[n].gameObject.activeSelf) wallBars[n].gameObject.SetActive(false);
        }

        void Update()
        {
            if ((powT -= Time.deltaTime) <= 0) { powT = 0.5f; Power = Rats.TotalPower(); }
            Game.SetStageInfo(Power, PowNeed(Game.Floor), Open.Count, Layout.Count);
            if (stairs) stairs.enabled = Open.Contains(StairsRoom);          // 계단 방이 열리기 전엔 계단 안 보임
            if (FxManager.WorldFreeze) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (GameOver.Active) return;                                       // 게임 오버 습격 중엔 고양이 등장·계단 없음
            foreach (var tp in traps) tp.Tick(dt, Rats, trapRadius, trapStun * CommonSkill.TrapStunMul, trapReload);   // 덫 해체 전문가
            UpdateCats(dt);
            // 계단: 계단 방이 열렸으면 쥐가 닿는 순간 위층으로 (보스·탈취 연출은 이후 단계)
            if (climbing || !Open.Contains(StairsRoom) || (Boss && Boss.Blocking)) return;      // 보스가 살아 있으면 계단 못 씀
            var sp = StairsPos;
            foreach (var r in Rats.Rats)
                if (r.temp <= 0 && !r.UltOn && Mathf.Abs(r.x - sp.x) < stairsTouch.x && Mathf.Abs(r.y - sp.y) < stairsTouch.y) { Climb(); break; }
        }

        // 층 클리어: 탈취 연출 → (moveAt 초 뒤) 페이드 → 다음 층. 테스트 버튼도 이걸 부름
        public void Climb()
        {
            if (climbing || GameOver.Active) return;
            climbing = true;
            if (Heist) Heist.Begin(StairsPos, () => Game.FadeThen(() => EnterFloor(Game.Floor + 1)));
            else Game.FadeThen(() => EnterFloor(Game.Floor + 1));
        }
    }

    // 웹게임 seeded() 와 같은 난수 (층 번호가 같으면 같은 배치)
    public class SeededRandom
    {
        uint a;
        public SeededRandom(uint seed) { a = seed; }
        public float Next()
        {
            unchecked
            {
                a += 0x6D2B79F5; uint t = a;
                t = (uint)((t ^ (t >> 15)) * (t | 1));
                t ^= t + (uint)((t ^ (t >> 7)) * (t | 61));
                return ((t ^ (t >> 14)) >> 0) / 4294967296f;
            }
        }
    }
}
