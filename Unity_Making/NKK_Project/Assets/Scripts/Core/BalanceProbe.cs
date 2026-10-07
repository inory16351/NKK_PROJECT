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
        public static readonly List<string> Results = new();

        int floor; float t0, maxPow;
        bool done;

        // 측정 시작 (Play 중에 호출). 저장은 측정 전용 키 nkk_probe 에만 함
        public static void Run(int tier, int startFloor, int endFloor, float giveUp = 600, int simHz = 20)
        {
            var p = Progress.I;
            p.saveKey = "nkk_probe"; p.ResetAll(); p.autoUpgradeInRun = false; p.testSkillLevels.Clear();
            p.tier = tier; p.maxFloor = Mathf.Max(startFloor, 1);
            MakeMeta(p, tier);
            Progress.PendingStartFloor = startFloor;
            EndFloor = endFloor; GiveUp = giveUp; SimHz = simHz; Active = true;
            int sk = 0; foreach (var e in p.testSkillLevels) sk += e.level;
            Results.Add($"── 티어 {tier} · {startFloor}층부터 · 스킬 합 {sk} · 조각 합 {ShardSum(p)}");
            SceneManager.LoadScene("Game");
        }

        // 티어 t 에 오를 때 필요했던 성장 (티어 테이블 t 행의 Skill_Level_Sum · Shard_Level_Sum 조건값)
        static void MakeMeta(Progress p, int tier)
        {
            var db = GameDatabase.Instance;
            if (!db.Tiers.TryGetValue(tier, out var tr)) return;
            float Need(string type) => tr.cond1_type == type ? tr.cond1_value : tr.cond2_type == type ? tr.cond2_value : tr.cond3_type == type ? tr.cond3_value : 0;
            int skillSum = Mathf.RoundToInt(Need("Skill_Level_Sum")), shardSum = Mathf.RoundToInt(Need("Shard_Level_Sum"));
            // 공용 스킬: 찍을 수 있는 것 중 가장 싼 것부터
            var lv = new Dictionary<string, int>();
            for (int n = 0; n < skillSum; n++)
            {
                CommonSkillRow best = null; double bc = double.MaxValue;
                foreach (var s in db.CommonSkills)
                {
                    int l = lv.TryGetValue(s.code_id, out var v) ? v : 0;
                    if (tier < s.unlock_tier || (!s.Infinite && l >= s.max_level)) continue;
                    if (s.req_skill != 0 && db.CommonSkillsById.TryGetValue(s.req_skill, out var r) && (lv.TryGetValue(r.code_id, out var rl) ? rl : 0) < s.req_level) continue;
                    double c = s.Cost(l); if (c < bc) { bc = c; best = s; }
                }
                if (best == null) break;
                lv[best.code_id] = (lv.TryGetValue(best.code_id, out var bv) ? bv : 0) + 1;
            }
            foreach (var kv in lv) p.testSkillLevels.Add(new Progress.SkillEntry { code = kv.Key, level = kv.Value });
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
            Time.captureDeltaTime = 1f / SimHz;
            foreach (var c in Camera.allCameras) c.enabled = false;           // 화면 안 그림 (빠르게)
            floor = Game.Floor; t0 = Time.time; maxPow = 0;
        }

        void Update()
        {
            if (!Active || done || !Game) return;
            float pow = Rats.TotalPower(); maxPow = Mathf.Max(maxPow, pow);
            float dt = Time.time - t0;
            if (Game.Floor != floor)
            {
                Add($"{floor}층 {dt:0}초 · 쥐 {Rats.RealCount} · 전투력 {GameManager.Format(maxPow)} / 적정 {GameManager.Format(Stage.PowNeed(floor))} ({maxPow / Stage.PowNeed(floor):0.00}배)");
                floor = Game.Floor; t0 = Time.time; maxPow = 0;
                if (floor > EndFloor) Finish();
            }
            else if (dt > GiveUp)
            {
                Add($"{floor}층 실패 ({GiveUp:0}초 넘음) · 쥐 {Rats.RealCount} · 전투력 {GameManager.Format(pow)} / 적정 {GameManager.Format(Stage.PowNeed(floor))} ({pow / Stage.PowNeed(floor):0.00}배) · 방 {Stage.Open.Count}/{Stage.Layout.Count}");
                Finish();
            }
        }

        void Add(string s) { Results.Add(s); Debug.Log("[BalanceProbe] " + s); }
        void Finish()
        {
            done = true; Active = false; Time.captureDeltaTime = 0;
            foreach (var c in Camera.allCameras) c.enabled = true;
            Add("끝");
        }

        public static string Report() { var sb = new StringBuilder(); foreach (var l in Results) sb.AppendLine(l); return sb.ToString(); }
    }
}
