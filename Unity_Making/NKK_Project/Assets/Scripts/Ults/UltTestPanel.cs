using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Ults
{
    // 테스트용 (게임 화면 왼쪽 위): ◀ 필살기 고르기 ▶ · 필살기 발동 · 슈퍼 점프 발동 · 층 클리어 · 게임 오버 · 저장값 초기화
    // · ◀ 보스 고르기 ▶ + 보스 소환 · ◀ 보스 기술 고르기 ▶ + 기술 발동 (소환한 보스가 다음 공격으로 바로 씀). 출시 때는 이 오브젝트를 끄면 됨
    public class UltTestPanel : MonoBehaviour
    {
        public UltimateManager Ults;
        public SuperJumpManager SuperJump;
        public Button prev, next, ultButton, superJumpButton;
        [Tooltip("층 클리어 연출 바로 보기 (계단 닿은 것처럼)")] public Button clearButton;
        [Tooltip("게임 오버 연출 바로 보기 (시간 초과)")] public Button gameOverButton;
        public NKK.Stage.StageManager Stage;
        public GameOver Over;
        [Tooltip("고른 보스를 화면 가운데에 불러 바로 싸움 (싸우는 테스트 보스가 있으면 바꿔 부름)")] public Button bossButton;
        [Header("보스 고르기 · 기술 강제 발동")]
        public Button bossPrev, bossNext, movePrev, moveNext, moveButton;
        [Tooltip("고른 보스 (자리: {floor} 층, {name} 이름)")] public TMP_Text bossPickText;
        [Tooltip("고른 기술 (자리: {kind} 종류, {name} 기술 이름)")] public TMP_Text movePickText;
        [Tooltip("기술 종류 이름: 일반 · 두 번째 · 쿨타임 스킬 · 발악 1 · 발악 2")] public string[] moveKinds = { "일반", "두 번째", "스킬", "발악 1", "발악 2" };
        public NKK.Hazards.Boss Boss;
        [Tooltip("고른 필살기 (자리: {name} 쥐 이름, {ult} 필살기 이름)")] public TMP_Text pickText;
        [Tooltip("아이콘 (선택)")] public Image pickIcon;
        [Tooltip("저장값 초기화: 진행도(조각·노드·치즈·연구자료·훈장·기록·업적) 전부 지우고 저장 없이 로비로")] public Button resetButton;
        [Tooltip("초기화 뒤 갈 씬")] public string lobbyScene = "Lobby";

        readonly List<RatCharacterRow> list = new();
        int idx;
        string fmt;
        int bossIdx, moveIdx;
        string bossFmt, moveFmt;
        readonly List<(int kind, string type)> moves = new();

        void Start()
        {
            var db = GameDatabase.Instance;
            foreach (var r in db.Rats.Values) if (db.UltOf(r) != null) list.Add(r);
            list.Sort((a, b) => a.ultimate.CompareTo(b.ultimate));
            if (prev) prev.onClick.AddListener(() => Move(-1));
            if (next) next.onClick.AddListener(() => Move(1));
            if (ultButton) ultButton.onClick.AddListener(() => { if (list.Count > 0) Ults.TestUlt(list[idx].code_id); });
            if (superJumpButton) superJumpButton.onClick.AddListener(() => SuperJump.Trigger(true));
            if (clearButton) clearButton.onClick.AddListener(() => { if (Stage && !Ults.Busy && !SuperJump.Busy) Stage.TestClear(); });
            if (bossButton) bossButton.onClick.AddListener(() => { if (Boss && !Ults.Busy && !SuperJump.Busy && !GameOver.Active) Boss.TestSpawn(bossIdx); });
            if (bossPrev) bossPrev.onClick.AddListener(() => MoveBoss(-1));
            if (bossNext) bossNext.onClick.AddListener(() => MoveBoss(1));
            if (movePrev) movePrev.onClick.AddListener(() => MoveMove(-1));
            if (moveNext) moveNext.onClick.AddListener(() => MoveMove(1));
            if (moveButton) moveButton.onClick.AddListener(ForceMove);
            ShowBoss();
            if (gameOverButton) gameOverButton.onClick.AddListener(() => { if (Over && !Heist.Active) Over.Begin(GameOver.Why.Time); });
            if (resetButton) resetButton.onClick.AddListener(ResetSave);
            Show();
        }

        // 저장값 초기화 → 판 치즈가 다시 저장되지 않게 GameManager.SaveProgress 없이 바로 로비로
        void ResetSave()
        {
            var g = FindAnyObjectByType<GameManager>(); if (g) g.enabled = false;      // 이 프레임에 5초 저장이 돌지 않게
            if (Progress.I) Progress.I.ResetAll();
            FxManager.Paused = false; Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(lobbyScene);
        }

        // ── 보스 고르기 · 기술 강제 발동 ──
        void MoveBoss(int d)
        {
            int n = GameDatabase.Instance.Bosses.Count; if (n == 0) return;
            bossIdx = (bossIdx + d + n) % n; moveIdx = 0; ShowBoss();
        }
        void MoveMove(int d) { if (moves.Count == 0) return; moveIdx = (moveIdx + d + moves.Count) % moves.Count; ShowMove(); }

        void ShowBoss()
        {
            var db = GameDatabase.Instance; if (db.Bosses.Count == 0) return;
            var b = db.Bosses[bossIdx];
            if (bossPickText) { bossFmt ??= bossPickText.text; bossPickText.text = bossFmt.Replace("{floor}", b.floor.ToString()).Replace("{name}", b.boss_name); }
            moves.Clear();
            string[] t = { b.atk_type, b.atk2_type, b.skill_type, b.special1_type, b.special2_type };
            for (int i = 0; i < t.Length; i++) if (!string.IsNullOrEmpty(t[i]) && t[i] != "None" && db.BossAtk(t[i]) != null) moves.Add((i, t[i]));
            ShowMove();
        }

        void ShowMove()
        {
            if (!movePickText || moves.Count == 0) return;
            moveFmt ??= movePickText.text;
            var (k, t) = moves[moveIdx]; var a = GameDatabase.Instance.BossAtk(t);
            movePickText.text = moveFmt.Replace("{kind}", k < moveKinds.Length ? moveKinds[k] : "").Replace("{name}", a != null && !string.IsNullOrEmpty(a.atk_name) ? a.atk_name : t);
        }

        // 고른 보스가 싸우는 중이 아니면 먼저 불러 놓고 다음 공격으로 기술 발동
        void ForceMove()
        {
            if (!Boss || moves.Count == 0 || Ults.Busy || SuperJump.Busy || GameOver.Active) return;
            var row = GameDatabase.Instance.Bosses[bossIdx];
            if (Boss.State != NKK.Hazards.Boss.BState.Fight || Boss.Data != row) Boss.TestSpawn(bossIdx);
            Boss.ForceAttack(moves[moveIdx].type);
        }

        void Move(int d) { if (list.Count == 0) return; idx = (idx + d + list.Count) % list.Count; Show(); }

        void Show()
        {
            if (list.Count == 0 || !pickText) return;
            fmt ??= pickText.text;
            var r = list[idx]; var u = GameDatabase.Instance.UltOf(r);
            pickText.text = fmt.Replace("{name}", r.character_name).Replace("{ult}", u.ultimate_name);
            if (pickIcon && Ults)
            {
                string n = u.ult_icon.Substring(u.ult_icon.LastIndexOf('/') + 1);
                foreach (var i in Ults.icons) if (i.name == n) { pickIcon.sprite = i.sprite; break; }
            }
        }
    }
}
