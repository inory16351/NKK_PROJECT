using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Ults
{
    // 테스트용 (게임 화면 왼쪽 위): ◀ 필살기 고르기 ▶ · 필살기 발동 · 슈퍼 점프 발동 · 층 클리어 · 게임 오버 · 저장값 초기화. 출시 때는 이 오브젝트를 끄면 됨
    public class UltTestPanel : MonoBehaviour
    {
        public UltimateManager Ults;
        public SuperJumpManager SuperJump;
        public Button prev, next, ultButton, superJumpButton;
        [Tooltip("층 클리어 연출 바로 보기 (계단 닿은 것처럼)")] public Button clearButton;
        [Tooltip("게임 오버 연출 바로 보기 (시간 초과)")] public Button gameOverButton;
        public NKK.Stage.StageManager Stage;
        public GameOver Over;
        [Tooltip("5층 보스를 화면 가운데에 불러 바로 싸움")] public Button bossButton;
        public NKK.Hazards.Boss Boss;
        [Tooltip("고른 필살기 (자리: {name} 쥐 이름, {ult} 필살기 이름)")] public TMP_Text pickText;
        [Tooltip("아이콘 (선택)")] public Image pickIcon;
        [Tooltip("저장값 초기화: 진행도(조각·노드·치즈·연구자료·훈장·기록·업적) 전부 지우고 저장 없이 로비로")] public Button resetButton;
        [Tooltip("초기화 뒤 갈 씬")] public string lobbyScene = "Lobby";

        readonly List<RatCharacterRow> list = new();
        int idx;
        string fmt;

        void Start()
        {
            var db = GameDatabase.Instance;
            foreach (var r in db.Rats.Values) if (db.UltOf(r) != null) list.Add(r);
            list.Sort((a, b) => a.ultimate.CompareTo(b.ultimate));
            if (prev) prev.onClick.AddListener(() => Move(-1));
            if (next) next.onClick.AddListener(() => Move(1));
            if (ultButton) ultButton.onClick.AddListener(() => { if (list.Count > 0) Ults.TestUlt(list[idx].code_id); });
            if (superJumpButton) superJumpButton.onClick.AddListener(() => SuperJump.Trigger(true));
            if (clearButton) clearButton.onClick.AddListener(() => { if (Stage && !Ults.Busy && !SuperJump.Busy) Stage.Climb(); });
            if (bossButton) bossButton.onClick.AddListener(() => { if (Boss && !Ults.Busy && !SuperJump.Busy && !GameOver.Active) Boss.TestSpawn(); });
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
