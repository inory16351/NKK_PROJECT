using NKK.Stage;
using NKK.Ults;
using TMPro;
using UnityEngine;

namespace NKK
{
    // 층 제한시간 (웹게임 meta.js floorTime · updateRunTimer).
    // · 층에 들어갈 때마다 다시 채움: 기본 + 방 수 × 방당 (+ 보스 층 추가)
    // · 필살기·슈퍼 점프·층 이동(페이드)·게임 오버 중에는 안 줄어듦
    // · 60·30·10초가 되는 순간 경고 배너 + 빨간 번쩍, 30초 아래면 시간 글이 빨갛게 깜빡, 0 → 게임 오버(시간 초과)
    public class RunTimer : MonoBehaviour
    {
        [Header("연결")]
        public GameManager Game;
        public StageManager Stage;
        public UltimateManager Ults;
        public SuperJumpManager SuperJump;
        public GameOver Over;

        [Header("제한시간 = 기본 + 방 수 × 방당 (+ 보스 층이면 추가) 초")]
        public float baseTime = 190;
        public float perRoom = 35;
        [Tooltip("웹: BOSS_TIME(60) + 30")] public float bossExtra = 90;

        [Header("경고")]
        [Tooltip("남은 시간이 이 초를 지나는 순간 경고 배너")] public int[] warnAt = { 60, 30, 10 };
        [Tooltip("이 초 아래면 시간 글이 빨갛게 깜빡")] public float lowTime = 30;
        [Tooltip("이 초 아래면 1초마다 시간 글이 통 튐")] public float tickTime = 10;
        [Tooltip("경고 배너 제목 ({n} = 남은 초)")] public string warnTitle;
        [Tooltip("경고 배너 부제")] public string warnSub;
        public Color warnColor = new(0.91f, 0.47f, 0.42f);

        [Header("HUD")]
        [Tooltip("남은 시간 글 (씬 TMP, 자리표시 {m} 분 · {s} 초 두 자리)")] public TMP_Text timeText;
        [Tooltip("남은 시간 막대 (채움 부분, 가로 앵커로 줄어듦)")] public RectTransform barFill;
        public Color normalColor = new(1, 0.95f, 0.75f);
        public Color blinkColor = Color.white;

        [Header("테스트")]
        [Tooltip("켜면 시간이 안 줄어듦")] public bool testFreeze;

        [Header("상태 (보기용)")]
        public float Left;
        public float Max = 1;

        string timeFormat;
        float pulse;

        public float FloorTime(int f) => Mathf.Round(baseTime + perRoom * Stage.Layout.Count + (Stage.IsBossFloor(f) ? bossExtra : 0) + Stage.TimeAdd(f) + CommonSkill.TimeAdd);   // + 스테이지 테이블 · 공용 스킬 제한시간

        void Awake()
        {
            if (timeText) timeFormat = timeText.text;
            if (Stage) Stage.FloorEntered += Refill;
        }
        void OnDestroy() { if (Stage) Stage.FloorEntered -= Refill; }

        public void Refill() { Max = Left = FloorTime(Game.Floor); }

        bool Stopped => GameOver.Active || testFreeze || FxManager.WorldFreeze || FxManager.Paused || Stage.Climbing
                        || (Ults && Ults.Busy) || (SuperJump && SuperJump.Busy);

        void Update()
        {
            if (!Stopped && Left > 0)
            {
                float before = Left;
                Left -= Time.deltaTime;
                foreach (int w in warnAt)
                    if (before > w && Left <= w)
                    {
                        Game.ShowBanner((warnTitle ?? "").Replace("{n}", w.ToString()), warnSub);
                        if (Ults) Ults.Flash(warnColor, 0.15f);
                        FxManager.I?.Shake(0.08f);
                    }
                if (Left < tickTime && Mathf.Floor(before) != Mathf.Floor(Left)) pulse = 1;
                if (Left <= 0) { Left = 0; if (Over) Over.Begin(GameOver.Why.Time); }
            }
            UpdateHud();
        }

        void UpdateHud()
        {
            int tl = Mathf.Max(0, Mathf.CeilToInt(Left));
            if (timeText)
            {
                timeText.text = (timeFormat ?? "{m}:{s}").Replace("{m}", (tl / 60).ToString()).Replace("{s}", (tl % 60).ToString("00"));
                bool low = tl < lowTime;
                timeText.color = low ? (Mathf.Sin(Time.time * 12) > 0 ? warnColor : blinkColor) : normalColor;
                pulse = Mathf.Max(0, pulse - Time.unscaledDeltaTime * 4);
                timeText.transform.localScale = Vector3.one * (1 + 0.25f * pulse * pulse);
            }
            if (barFill)
            {
                barFill.anchorMax = new Vector2(Mathf.Clamp01(Left / Mathf.Max(1, Max)), barFill.anchorMax.y);
                var img = barFill.GetComponent<UnityEngine.UI.Image>();
                if (img) img.color = tl < lowTime ? warnColor : normalColor;
            }
        }

        [ContextMenu("테스트: 12초 남기기")]
        void TestNearEnd() { Left = 12; }
    }
}
