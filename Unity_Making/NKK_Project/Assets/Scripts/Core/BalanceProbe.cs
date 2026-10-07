using System.Collections.Generic;
using System.Text;
using NKK.Data;
using NKK.Rats;
using NKK.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NKK
{
    // 밸런스 측정 (개발용, 평소엔 꺼 둠).
    // 프레임마다 정확히 1/simHz 초씩 진행하며 화면을 그리지 않고 최대한 빨리 돌림 (게임 코드의 dt 0.05 제한에 안 걸리게).
    // Run(티어, 시작 층, 끝 층) → 진행도를 측정 전용 저장 키로 바꾸고 그 티어의 전형적인 성장 상태를 만든 뒤 게임 씬을 다시 엶.
    //   성장 상태 = 그 티어에 오르는 데 필요했던 공용 스킬 레벨 합(가장 싼 것부터 삼) · 조각 강화 레벨 합(해금된 종에 고르게)
    // 클릭 총공격 없이 쥐들끼리만 진행 (최소 성능 기준). 결과: 정적 Results (씬을 넘어가도 남음) · 콘솔
    public class BalanceProbe : MonoBehaviour
    {
        public GameManager Game;
        public StageManager Stage;
        public RatManager Rats;

        public static bool Active;
        public static int SimHz = 20, EndFloor = 3;
        public static float GiveUp = 600;
        public static float LogEvery = 0;          // >0 이면 이 초마다 진행 상황 기록
        public static int PromoteMode = 1;          // 0 승급 안 함 · 1 꽉 찼을 때 한 묶음만 (가장 낮은 등급) · 2 될 때마다 일괄 승급 (최대 마리 수 절반은 남음)
        public static bool UseUlt = true;
        public static bool UseRush = true;
        public static bool Calib;                   // 적정 고정: 적정 전투력 = 지금 무리 전투력 (전투력 = 적정일 때 몇 초 걸리나 재기)          // 플레이어처럼 클릭 총공격 (쿨타임마다: 보스 → 계단 방 벽 → 가장 약한 벽)           // 필살기가 차면 바로 씀 (플레이어처럼, Ults.UltimateManager.ForceAuto)
        public static readonly List<string> Results = new();
        public static float BaseTime = 180, BossBaseTime = 30;      // 목표 비교 기준 (노드 추가 시간 빼고): 일반 180초 · 보스 층 +30
        // 층마다 한 줄 (적정 고정 분석용): 층, 제한시간 기준 사용 초, 기준 초, 계단 거리, 경로 벽 배율 합, 방 수, 성공 여부
        public static readonly List<float[]> Rows = new();
        // 이어서 돌릴 측정 (Run 인자: 티어, 시작 층, 끝 층, 포기 초, simHz, 노드 수, 조각 합[, 승급 방식]). 하나 끝나면 다음 것 자동 시작
        public static readonly List<float[]> Queue = new();
        public static void RunQueue()
        {
            if (Queue.Count == 0) return;
            var a = Queue[0]; Queue.RemoveAt(0);
            if (a.Length > 7) PromoteMode = (int)a[7];
            Run((int)a[0], (int)a[1], (int)a[2], a[3], (int)a[4], (int)a[5], (int)a[6]);
        }

        int floor, mask = ~0; float t0, maxPow, limit, promoT, logT, bossT0 = -1, bossDur; double cheese0;
        readonly List<Camera> offCams = new();
        RunTimer timer;
        bool done;

        // 측정 시작 (Play 중에 호출). 저장은 측정 전용 키 nkk_probe 에만 함
        public static void Run(int tier, int startFloor, int endFloor, float giveUp = 600, int simHz = 20, int nodes = -1, int shards = -1)
        {
            var p = Progress.I;
            p.saveKey = "nkk_probe"; p.ResetAll(); p.autoUpgradeInRun = false; p.testSkills.Clear(); p.testSkillTier = 0;
            p.tier = tier; p.maxFloor = Mathf.Max(startFloor, 1);
            MakeMeta(p, tier, nodes, shards);
            Progress.PendingStartFloor = startFloor;
            EndFloor = endFloor; GiveUp = giveUp; SimHz = simHz; Active = true;
            Results.Add($"── 티어 {tier}{(Calib ? " · 적정 고정" : "")} · 승급 {PromoteMode} · 필살기 {(UseUlt ? "씀" : "안 씀")} · 총공격 {(UseRush ? "씀" : "안 씀")} · {startFloor}층부터 · 스킬 노드 {p.testSkills.Count} (지금 트리 {p.SkillCountIn(tier)}) · 조각 합 {ShardSum(p)}");
            SceneManager.LoadScene("Game");
        }

        // 측정할 성장 상태: 이전 훈장 트리는 전부 + 지금 훈장 트리에서 nodes 개 (싼 것부터, -1 = 다음 훈장 승급 조건 수, 99 = 전부)
        //   조각 강화 합 shards (-1 = 다음 훈장 승급 조건)
        static void MakeMeta(Progress p, int tier, int nodes = -1, int shards = -1)
        {
            var db = GameDatabase.Instance;
            db.Tiers.TryGetValue(tier + 1, out var next);
            float Need(string type) => next == null ? 0 : next.cond1_type == type ? next.cond1_value : next.cond2_type == type ? next.cond2_value : next.cond3_type == type ? next.cond3_value : 0;
            int cur = nodes >= 0 ? nodes : Mathf.RoundToInt(Need("Skill_Node_Count")), shardSum = shards >= 0 ? shards : Mathf.RoundToInt(Need("Shard_Level_Sum"));
            foreach (var s in db.CommonSkills) if (s.tier < tier && !s.IsRoot) p.testSkills.Add(s.skill_id);
            for (int n = 0; n < cur; n++)
            {
                CommonSkillRow best = null; float bc = float.MaxValue;
                foreach (var s in db.CommonSkills)
                    if (s.tier == tier && !s.IsRoot && p.StateOf(s) == Progress.SkillState.Open && s.cost_cheese < bc) { bc = s.cost_cheese; best = s; }
                if (best == null) break;
                p.testSkills.Add(best.skill_id);
            }
            // 조각 강화: 해금된 일반·레어 종에 고르게
            var pool = new List<RatCharacterRow>();
            foreach (var r in db.Rats.Values) if (r.unlock_rank <= tier && (int)r.Grade <= 1) pool.Add(r);
            for (int n = 0; n < shardSum && pool.Count > 0; n++) p.SetLevelForTest(pool[n % pool.Count].code_id, p.Level(pool[n % pool.Count].code_id) + 1);
        }
        static int ShardSum(Progress p) { int s = 0; foreach (var r in GameDatabase.Instance.Rats.Values) s += p.Level(r.code_id); return s; }

        void Start()
        {
            if (!Active) { enabled = false; return; }
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            Ults.UltimateManager.ForceAuto = UseUlt;
            Time.captureDeltaTime = 1f / SimHz;
            // 화면 안 그림 (빠르게). 메인 카메라는 켜 둔 채 아무것도 안 그림 → Camera.main 을 쓰는 코드(고양이 등장·화면 안 판정)가 그대로 돎
            foreach (var c in Camera.allCameras) { if (c == Camera.main) { mask = c.cullingMask; c.cullingMask = 0; } else { c.enabled = false; offCams.Add(c); } }
            timer = FindFirstObjectByType<RunTimer>(); if (timer) timer.testFreeze = true;     // 제한시간은 재기만 함
            floor = Game.Floor; t0 = Time.time; maxPow = 0; cheese0 = Game.Cheese; limit = 0; pathD = -1;
        }

        void Update()
        {
            if (!Active || done || !Game) return;
            // 플레이어처럼: 5초마다 승급 (PromoteMode)
            if ((promoT -= Time.deltaTime) <= 0) { promoT = 5; if (PromoteMode == 2) Rats.PromoteAll(); else if (PromoteMode == 1 && Rats.RealCount >= Rats.PopCap - 1) for (int g = 0; g < 5; g++) if (Rats.Promote(g)) break; }
            if (UseRush) ProbeRush();
            StageManager.PowOverride = Calib ? Mathf.Max(1, Rats.TotalPower()) : 0;
            // 보스전 시간 (싸움 시작 → 쓰러짐, 제한시간 기준)
            var bs = Hazards.Boss.Current;
            if (bs && bs.CanHit && bossT0 < 0) bossT0 = timer ? timer.Used : Time.time - t0;
            if (bossT0 >= 0 && (!bs || !bs.CanHit) && bossDur <= 0) bossDur = Mathf.Max(0.01f, (timer ? timer.Used : Time.time - t0) - bossT0);
            float pow = Rats.TotalPower(); maxPow = Mathf.Max(maxPow, pow);
            float dt = Time.time - t0, used = timer ? timer.Used : dt;      // used = 제한시간 기준 (필살기·연출 중엔 안 셈)
            if (limit <= 0 && timer) limit = timer.Max;
            if (pathD < 0 && Stage.Layout.Count > 0) { StageManager.PowOverride = Calib ? Mathf.Max(1, Rats.TotalPower()) : 0; (pathSum, pathD) = PathWalls(); pathRooms = Stage.Layout.Count; }
            if (LogEvery > 0 && (logT += Time.deltaTime) >= LogEvery) { logT = 0; Add($"   {floor}층 {dt:0}초 · 쥐 {Rats.RealCount} · 전투력 {GameManager.Format(pow)} ({pow / Stage.PowNeed(floor):0.00}배) · 방 {Stage.Open.Count}/{Stage.Layout.Count}"); }
            if (Game.Floor != floor)
            {
                if (timer) used = timer.LastUsed;      // 새 층으로 넘어오며 타이머가 이미 다시 채워짐
                Add($"{floor}층 {used:0}초 / 제한 {limit:0}초 (전체 {dt:0}초) · 쥐 {Rats.RealCount} (승급 {Rats.PromoteTimes(0)}/{Rats.PromoteTimes(1)}/{Rats.PromoteTimes(2)}/{Rats.PromoteTimes(3)}/{Rats.PromoteTimes(4)}) · 전투력 {GameManager.Format(maxPow)} / 적정 {GameManager.Format(Stage.PowNeed(floor))} ({maxPow / Stage.PowNeed(floor):0.00}배) · 치즈 +{GameManager.Format(Game.Cheese - cheese0)}");
                if (bossDur > 0) Add($"   └ 보스전 {bossDur:0}초");
                Add($"   └ 기준 {BaseLimit(floor):0}초의 {used / BaseLimit(floor) * 100:0}% · 계단 거리 {pathD} · 경로 벽 배율 합 {pathSum:0.0} · 방 {pathRooms}");
                Rows.Add(new[] { floor, used, BaseLimit(floor), pathD, pathSum, pathRooms, 1, bossDur });
                floor = Game.Floor; t0 = Time.time; maxPow = 0; cheese0 = Game.Cheese; limit = 0; bossT0 = -1; bossDur = 0; pathD = -1;
                if (floor > EndFloor) Finish();
            }
            else if ((!Calib && limit > 0 && used > limit) || dt > Mathf.Max(GiveUp, limit * 2))      // 적정 고정 모드는 제한시간을 넘어도 끝까지 잼
            {
                if (bossT0 >= 0) Add($"   └ 보스전 {(bossDur > 0 ? bossDur : (timer ? timer.Used : dt) - bossT0):0}초{(bossDur > 0 ? "" : " (못 잡음)")}");
                Rows.Add(new[] { floor, used, BaseLimit(floor), pathD, pathSum, pathRooms, 0, bossDur });
                Add($"{floor}층 실패 (제한 {limit:0}초 넘음, 전체 {dt:0}초) · 쥐 {Rats.RealCount} · 전투력 {GameManager.Format(pow)} / 적정 {GameManager.Format(Stage.PowNeed(floor))} ({pow / Stage.PowNeed(floor):0.00}배) · 방 {Stage.Open.Count}/{Stage.Layout.Count}");
                Finish();
            }
        }

        // 플레이어처럼 클릭 총공격: 화면을 그 지점으로 옮기고 돌진 (화면 안 쥐만 모임)
        void ProbeRush()
        {
            if (!Rats.RushReady || Rats.RushActive || Stage.Climbing) return;
            Vector2 p = default; bool found = false;
            var b = Hazards.Boss.Current;
            if (b && b.CanHit) { p = new Vector2(b.x, b.y); found = true; }
            else if (Stage.Open.Contains(Stage.StairsRoom) && (!b || !b.Blocking)) { p = Stage.StairsPos; found = true; }     // 계단 방이 열렸으면 계단으로
            else
            {
                // 플레이어처럼 계단 쪽 길을 뚫음: 열린 방에 붙은 안 열린 방 중 계단 방까지 (레이아웃 안) 거리가 가장 가까운 벽 → 같으면 약한 벽
                var toStairs = new Dictionary<Vector2Int, int> { [Stage.StairsRoom] = 0 }; var q = new Queue<Vector2Int>(); q.Enqueue(Stage.StairsRoom);
                while (q.Count > 0) { var c = q.Dequeue(); foreach (var dd in StageManager.Dirs) { var k = c + dd; if (Stage.Layout.Contains(k) && !toStairs.ContainsKey(k)) { toStairs[k] = toStairs[c] + 1; q.Enqueue(k); } } }
                float bestHp = float.MaxValue; int bestD = int.MaxValue;
                foreach (var k in Stage.Open)
                    foreach (var d in StageManager.Dirs)
                    {
                        var t = k + d;
                        if (Stage.Open.Contains(t) || !Stage.Layout.Contains(t)) continue;
                        int dist = toStairs.TryGetValue(t, out var v) ? v : 99; float hp = Stage.WallHP(k.x, k.y, d.x, d.y);
                        if (found && (dist > bestD || dist == bestD && hp >= bestHp)) continue;
                        found = true; bestD = dist; bestHp = hp;
                        p = new Vector2((k.x + 0.5f + d.x * 0.5f) * World.RW - d.x * 40, (k.y + 0.5f + d.y * 0.5f) * World.RH - d.y * 40);
                    }
            }
            if (!found) return;
            if (Game.cam) Game.cam.CenterOn(p.x, p.y);
            Rats.ClickRush(p);
        }

        // 시작 방 → 계단 방 최단 경로의 벽 체력 배율 합 (적정 대비) · 계단 거리
        (float sum, int d) PathWalls()
        {
            var dist = new Dictionary<Vector2Int, int> { [Vector2Int.zero] = 0 }; var par = new Dictionary<Vector2Int, Vector2Int>();
            var q = new Queue<Vector2Int>(); q.Enqueue(Vector2Int.zero);
            while (q.Count > 0) { var c = q.Dequeue(); foreach (var dd in StageManager.Dirs) { var k = c + dd; if (Stage.Layout.Contains(k) && !dist.ContainsKey(k)) { dist[k] = dist[c] + 1; par[k] = c; q.Enqueue(k); } } }
            var st = Stage.StairsRoom; if (!dist.ContainsKey(st)) return (0, 0);
            float need = Stage.PowNeed(floor), sum = 0; var cur = st;
            while (cur != Vector2Int.zero) { var pv = par[cur]; sum += Stage.WallHPMax(pv.x, pv.y, cur.x - pv.x, cur.y - pv.y) / Mathf.Max(1, need); cur = pv; }
            return (sum, dist[st]);
        }
        float pathSum; int pathD, pathRooms;
        float BaseLimit(int f) => BaseTime + (Stage.IsBossFloor(f) ? BossBaseTime : 0);

        void Add(string s) { Results.Add(s); Debug.Log("[BalanceProbe] " + s); }
        void Finish()
        {
            done = true; Active = false; Time.captureDeltaTime = 0; StageManager.PowOverride = 0; Ults.UltimateManager.ForceAuto = false;
            var m = Camera.main; if (m) m.cullingMask = mask;
            foreach (var c in offCams) if (c) c.enabled = true;
            Add("끝");
            if (Queue.Count > 0) RunQueue();
        }

        public static string RowsCsv() { var sb = new StringBuilder("floor,used,base,d,path,rooms,ok,boss").AppendLine(); foreach (var r in Rows) sb.AppendLine(string.Join(",", r)); return sb.ToString(); }
        public static string Report() { var sb = new StringBuilder(); foreach (var l in Results) sb.AppendLine(l); return sb.ToString(); }
    }
}
