using System.Collections.Generic;
using NKK.Data;
using NKK.Hazards;
using NKK.Lobby;
using NKK.Rats;
using NKK.Stage;
using NKK.Ults;
using UnityEngine;

namespace NKK.Tutorial
{
    // 튜토리얼 진행 (씬 오브젝트 TutorialManager, Game 씬과 Lobby 씬에 하나씩).
    // 튜토리얼 테이블 Step 을 순서(sort)대로 보면서 이 씬 단계 중 아직 안 본 것의 조건·발동을 확인 →
    // 해금(unlock) → 대사(Line) → 끝나면 행동(action) → 'After' 단계가 이어짐. 본 단계는 Progress 에 저장 (슬롯마다)
    public class TutorialManager : MonoBehaviour
    {
        [Tooltip("이 씬 이름 (테이블 Step.scene 과 같게: Game · Lobby)")] public string scene = "Game";
        public TutorialBox box;

        [Header("Game 씬")]
        public GameManager Game;
        public StageManager Stage;
        public RatManager Rats;
        public CatManager Cats;
        public UltimateManager Ults;
        public SuperJumpManager SuperJump;

        [Tooltip("비워 두면 메인 카메라에서 찾음")] public CameraController Cam;

        [Header("Lobby 씬")]
        public LobbyManager Lobby;
        [Tooltip("비워 두면 씬에서 찾음")] public SkillPage Skills;
        [Tooltip("이 접두어로 시작하는 강조는 다음 단계가 올 때까지 계속 (치즈 창고 따라 하기)")] public string holdPrefix = "skill_";
        [Tooltip("holdPrefix 강조를 남겨 두는 최대 초")] public float holdTime = 600;

        [Header("연출")]
        [Tooltip("씬·층이 시작되고 이 초 뒤부터 튜토리얼 확인 (화면이 밝아진 뒤)")] public float startDelay = 1.2f;
        [Tooltip("FloorStart 단계는 층 시작 뒤 이 초 안에만 발동")] public float floorStartWindow = 6;
        [Tooltip("대사가 끝난 뒤 마지막으로 가리킨 곳을 이 초 더 강조")] public float lingerTime = 4;
        [Tooltip("로비: 탭을 가리킨 채 끝나면 그 페이지를 열 때까지 강조 (최대 초)")] public float lobbyLingerTime = 20;
        [Tooltip("대사 중 게임을 멈춤")] public bool pauseGame = true;
        [Tooltip("대사가 월드 대상(새끼 쥐 등)을 가리키면 카메라가 비추며 이만큼 확대")] public float worldFocusZoom = 1.7f;
        [Tooltip("첫 탄생(Birth) 단계: 갓 태어난 쥐에 붙일 강조 id (대사 highlight 에 쓰기)")] public string birthTargetId = "baby";
        [Tooltip("첫 탄생 대사 중 새끼 쥐 위로 하트를 이 초마다")] public float birthHeartEvery = 1.1f;
        [Tooltip("발동 기록을 콘솔에 남김")] public bool log = true;

        // 대사 중 게임 정지 (FxManager 가 timeScale 0)
        public static bool Pausing { get; private set; }

        float sceneT, floorT; int lastFloor = -1;
        TutorialTarget tempTarget; float heartT;      // 첫 탄생: 새끼 쥐에 잠깐 붙이는 강조
        string lastDone;             // 방금 끝난 단계 (After 발동)
        TutoStepRow running;
        string actionPending, actionValue; float actionTry;

        static GameDatabase DB => GameDatabase.Instance;
        static Progress P => Progress.I;

        void OnDestroy() { if (Pausing) Pausing = false; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            sceneT += dt;
            if (scene == "Game" && Game)
            {
                if (Game.Floor != lastFloor) { lastFloor = Game.Floor; floorT = 0; }
                if (!Pausing && !(Stage && Stage.Climbing)) floorT += Time.deltaTime;
            }
            UpdateAction(dt);
            UpdateFocus(dt);
            if (running != null || !DB || !P || TutorialBox.Showing || sceneT < startDelay) return;
            if (P.TutoSkipped || Busy()) return;
            foreach (var s in DB.TutoSteps)
            {
                if (s.scene != scene || P.TutoDone(s.step_id)) continue;
                if (!Cond(s.cond1_type, s.cond1_value) || !Cond(s.cond2_type, s.cond2_value)) continue;
                if (!Trigger(s)) continue;
                Run(s);
                break;
            }
        }

        // 다른 연출 중이면 기다림
        bool Busy()
        {
            if (scene != "Game") return Lobby && RatTreePopup.Current && RatTreePopup.Current.IsOpen;
            return GameOver.Active || Heist.Active || FxManager.WorldFreeze || FxManager.Paused || (Stage && Stage.Climbing)
                || (Ults && Ults.Busy) || (SuperJump && SuperJump.Busy) || actionPending != null;
        }

        bool Cond(string type, string v)
        {
            if (string.IsNullOrEmpty(type) || type == "None") return true;
            float.TryParse(v, out float n);
            switch (type)
            {
                case "RunMin": return P.runs >= n;
                case "RunMax": return P.runs <= n;
                case "Unlocked": return P.IsUnlocked(v);
                case "Locked": return !P.IsUnlocked(v);
                case "Done": foreach (var id in v.Split(',')) if (P.TutoDone(id.Trim())) return true; return false;       // 쉼표 = 그중 하나라도
                case "NotDone": foreach (var id in v.Split(',')) if (P.TutoDone(id.Trim())) return false; return true;
                case "FloorMin": return Game && Game.Floor >= n;
                case "SkillAffordable": { var r = SkillPage.Recommend(); return r != null && P.CanBuySkill(r); }
                case "SkillPoor": { var r = SkillPage.Recommend(); return r == null || !P.CanBuySkill(r); }
                case "WishOpen": return GeneGacha.WishUnlocked;        // 관심 종 (18층 클리어)
            }
            Debug.LogWarning($"[Tutorial] 모르는 조건: {type}");
            return false;
        }

        bool Trigger(TutoStepRow s)
        {
            float v = s.trigger_value;
            switch (s.trigger)
            {
                // ── Game ──
                case "FloorStart": return Game && (v <= 0 || Game.Floor == (int)v) && floorT >= startDelay && floorT < startDelay + floorStartWindow;
                case "FloorTime": return Game && floorT >= v;
                case "Birth": return Rats && Rats.Births >= Mathf.Max(1, v);
                case "RatCount": return Rats && Rats.RealCount >= v;
                case "SuperJump": return SuperJump && SuperJump.Jumps >= Mathf.Max(1, v);      // 끝난 뒤 (Busy 가 기다림)
                case "PopFull": return Rats && Rats.RealCount >= Rats.PopCap;
                case "RoomOpen": return Stage && Stage.Open.Count >= 1 + Mathf.Max(1, v);
                case "CatAppear": return Cats && Cats.Current;
                case "BossFight": return Boss.Current && Boss.Current.State == Boss.BState.Fight;
                case "UltReady":
                    if (!Rats || !Ults) return false;
                    foreach (var r in Rats.Rats) if (Ults.Full(r)) return true;
                    return false;
                // ── 둘 다 ──
                case "After": return lastDone != null && System.Array.IndexOf((s.trigger_text ?? "").Split(','), lastDone) >= 0;      // 쉼표로 여러 단계
                // ── Lobby ──
                case "LobbyEnter": return Lobby && !Lobby.PrepOpen;
                case "AlleyEnter": return Lobby && !Lobby.PrepOpen && NKK.Lobby.LobbyPan.I && NKK.Lobby.LobbyPan.I.AtRoom;      // 뒷골목에 도착
                case "PageOpen": return Lobby && Lobby.PrepOpen && Lobby.CurrentPage == s.trigger_text;
                case "CanRankUp": return Lobby && !Lobby.PrepOpen && P.CanRankUp();
                case "TierAtLeast": return Lobby && !Lobby.PrepOpen && P.tier >= v;
                case "SkillSelect":
                    {
                        if (!Lobby || !Lobby.PrepOpen || Lobby.CurrentPage != "skill") return false;
                        if (!Skills) Skills = FindFirstObjectByType<SkillPage>(FindObjectsInactive.Include);
                        var sk = Skills ? Skills.Selected : null;
                        return sk != null && !sk.IsRoot && P.CanBuySkill(sk);
                    }
                case "SkillBuy": return SkillPage.Bought >= Mathf.Max(1, v);
            }
            Debug.LogWarning($"[Tutorial] 모르는 발동: {s.trigger}");
            return false;
        }

        void Run(TutoStepRow s)
        {
            running = s;
            if (log) Debug.Log($"[Tutorial] {s.step_id} ({s.trigger})");
            P.MarkTuto(s.step_id);             // 시작할 때 기록 (중간에 꺼도 다시 안 나옴)
            if (P.Unlock(s.unlock)) SfxManager.Play("jgl_unlock");
            if (box) box.ClearLinger();
            if (tempTarget) Destroy(tempTarget);
            if (s.trigger == "Birth" && Rats && Rats.LastBorn)
            {
                // 갓 태어난 쥐를 강조할 수 있게 (대사 highlight = birthTargetId)
                tempTarget = Rats.LastBorn.gameObject.AddComponent<TutorialTarget>();
                tempTarget.id = birthTargetId; tempTarget.pad = 16;
                heartT = 0.35f;
            }
            List<TutoLineRow> lines = null;
            DB.TutoLines.TryGetValue(s.step_id, out lines);
            if (scene == "Game" && pauseGame && lines != null && lines.Count > 0) Pausing = true;
            if (box) box.Play(lines, () => Finish(s, lines));
            else Finish(s, lines);
        }

        void Finish(TutoStepRow s, List<TutoLineRow> lines)
        {
            Pausing = false;
            running = null;
            // 마지막 대사가 가리킨 곳은 잠깐 더 강조 (로비 탭은 페이지를 열 때까지)
            string last = lines != null && lines.Count > 0 ? lines[lines.Count - 1].highlight : null;
            if (box && !string.IsNullOrEmpty(last))
                box.Linger(last, !string.IsNullOrEmpty(holdPrefix) && last.StartsWith(holdPrefix) ? holdTime : scene == "Lobby" && last.StartsWith("tab_") ? lobbyLingerTime : lingerTime);
            if (!string.IsNullOrEmpty(s.action)) { actionPending = s.action; actionValue = s.action_value; actionTry = 0; }
            lastDone = s.step_id;
            if (tempTarget) Destroy(tempTarget, lingerTime + 0.5f);
        }

        // 대사가 월드 대상을 가리키는 동안 카메라가 그쪽으로 가서 확대 (+ 새끼 쥐면 하트)
        void UpdateFocus(float dt)
        {
            if (scene != "Game") return;
            if (!Cam && Camera.main) Cam = Camera.main.GetComponent<CameraController>();
            if (!Cam) return;
            var t = box && TutorialBox.Showing ? box.CurrentTarget : null;
            bool world = t && t.IsWorld;
            Cam.tutoFollow = world;
            if (!world) return;
            Cam.tutoFocus = t.transform.position;
            Cam.tutoZoomGoal = worldFocusZoom;
            if (t == tempTarget && t.TryGetComponent<Rat>(out var r) && (heartT -= dt) <= 0)
            {
                heartT = birthHeartEvery;
                FxManager.I?.Hearts(r.x, r.y, Rats ? Rats.birthHearts : 4);
            }
        }

        // 끝날 때 행동: 필살기 시연 등. 바로 안 되면 몇 초 동안 다시 시도
        void UpdateAction(float dt)
        {
            if (actionPending == null) return;
            actionTry += dt;
            bool ok = false;
            switch (actionPending)
            {
                case "Ult": ok = Ults && !Ults.Busy && !(SuperJump && SuperJump.Busy) && Ults.TestUlt(actionValue); break;
                case "OpenPage": if (Lobby) { Lobby.OpenPage(actionValue); ok = true; } break;
                default: Debug.LogWarning($"[Tutorial] 모르는 행동: {actionPending}"); ok = true; break;
            }
            if (ok || actionTry > 5) { if (log) Debug.Log($"[Tutorial] 행동 {actionPending} {actionValue} → {(ok ? "성공" : "포기")}"); actionPending = null; }
        }

        // 로비 페이지가 열리면 그 탭 강조는 끝
        void LateUpdate()
        {
            if (scene == "Lobby" && box && Lobby && Lobby.PrepOpen && !TutorialBox.Showing && box.LingerId != null && box.LingerId.StartsWith("tab_")) box.ClearLinger();
        }
    }
}
