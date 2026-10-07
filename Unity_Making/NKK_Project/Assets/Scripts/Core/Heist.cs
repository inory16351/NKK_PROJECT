using System;
using NKK.Rats;
using NKK.Ults;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 층 클리어 연출 (웹게임 meta.js startHeist · updateHeist · drawHeist): 쥐가 계단에 닿으면
    // "연구 자료를 훔쳤다!!!" → "빨리 도망가!!!" → 연구자료 +n, 경보등·빨간 테두리·통통 튀는 자료 뭉치·날아다니는 종이.
    // 화면 속 쥐는 계단으로 우르르 돌진. moveAt 초에 층 이동(페이드) 시작, endAt 초에 연출 끝.
    // 연구자료 = round(기본 × 증가^(층-1)) (보스 층 × 배율) → Progress.research (판이 끝나도 남음) · GameManager.RunResearch (이번 판)
    public class Heist : MonoBehaviour
    {
        public static bool Active { get; private set; }

        [Header("연결")]
        public GameManager Game;
        public RatManager Rats;
        public UltimateManager Ults;

        [Header("연구자료 = round(기본 × 증가^(층-1)) × (보스 층 배율)")]
        public float researchBase = 6;
        public float researchGrow = 1.45f;
        public float bossMul = 3;
        public int bossEvery = 5;

        [Header("시간 (초)")]
        [Tooltip("이때 층 이동(페이드) 시작")] public float moveAt = 2.2f;
        [Tooltip("연출 끝")] public float endAt = 2.7f;
        public Color flashColor = new(0.91f, 0.47f, 0.42f);

        [Header("화면 (HUD/HeistFx)")]
        [Tooltip("연출 묶음 (꺼 둠)")] public CanvasGroup fx;
        [Tooltip("빨간 테두리 (깜빡)")] public Image vignette;
        [Tooltip("경보등 빛줄기 (빙글빙글, 왼쪽 → 오른쪽 순)")] public RectTransform[] sirenBeams;
        public float beamSpin = 7;
        [Tooltip("'연구 자료를 훔쳤다!!!'")] public TMP_Text line1;
        [Tooltip("'빨리 도망가!!!' (0.45초부터)")] public TMP_Text line2;
        [Tooltip("얻은 연구자료 (0.8초부터, 자리표시 {n})")] public TMP_Text amount;
        [Tooltip("통통 튀는 자료 뭉치")] public RectTransform docs;
        [Tooltip("화면을 가로지르는 종이 (꺼 둔 템플릿, 12장 복제)")] public RectTransform paperTemplate;
        public int paperCount = 12;
        [Tooltip("종이 크기 (기본 + (i % 3) × 증가)")] public float paperSize = 60, paperSizeStep = 16;

        float t;
        int gained;
        bool moved;
        Action onMove;
        string amountFormat;
        RectTransform[] papers;
        Vector2 docsPos;

        public float ResearchFor(int f) => Mathf.Round(researchBase * Mathf.Pow(researchGrow, f - 1)) * (f % bossEvery == 0 ? bossMul : 1);

        void Awake()
        {
            Active = false;
            if (amount) amountFormat = amount.text;
            if (docs) docsPos = docs.anchoredPosition;
            if (paperTemplate)
            {
                paperTemplate.gameObject.SetActive(false);
                papers = new RectTransform[paperCount];
                for (int i = 0; i < paperCount; i++) { papers[i] = Instantiate(paperTemplate, paperTemplate.parent); papers[i].name = "Paper_" + i; papers[i].gameObject.SetActive(true); }
            }
            if (fx) fx.gameObject.SetActive(false);
        }
        void OnDestroy() { Active = false; }

        // 계단에 닿음 (StageManager.Climb) — move = 층 이동 시작
        public void Begin(Vector2 stairs, Action move)
        {
            if (Active) { move?.Invoke(); return; }
            Active = true; t = 0; moved = false; onMove = move;
            gained = Mathf.RoundToInt(ResearchFor(Game.Floor));
            var p = Progress.I;
            if (p) p.OnFloorReached(Game.Floor + 1);
            if (Research.I) Research.I.Earn(gained, stairs.x, stairs.y, false);
            else { if (p) { p.research += gained; p.Save(); } Game.RunResearch += gained; }
            if (amount) amount.text = (amountFormat ?? "+{n}").Replace("{n}", GameManager.Format(gained));
            // 화면 속 쥐는 계단으로 우르르
            if (Rats) Rats.StartRush(stairs);
            var fxm = FxManager.I;
            if (fxm)
            {
                fxm.Shake(0.25f);
                fxm.Burst(stairs.x, stairs.y, 60, 22, Color.white, new Color(0.75f, 0.91f, 1f), 160, 420);
                fxm.Stars(stairs.x, stairs.y, 60, 10, Color.white, new Color(0.75f, 0.91f, 1f));
            }
            if (Ults) Ults.Flash(flashColor, 0.35f);
            if (fx) { fx.gameObject.SetActive(true); fx.alpha = 0; }
            Step();
        }

        void Update()
        {
            if (!Active) return;
            t += Time.deltaTime;
            if (!moved && t >= moveAt) { moved = true; onMove?.Invoke(); onMove = null; }
            if (t >= endAt) { Active = false; if (fx) fx.gameObject.SetActive(false); return; }
            Step();
        }

        void Step()
        {
            float k = t, a = k < 0.15f ? k / 0.15f : k > 1.9f ? Mathf.Max(0, 1 - (k - 1.9f) / 0.3f) : 1, now = Time.time;
            if (fx) fx.alpha = a;
            if (vignette) { var c = vignette.color; c.a = 0.45f * (0.5f + 0.5f * Mathf.Sin(now * 16)); vignette.color = c; }
            if (sirenBeams != null)
                for (int i = 0; i < sirenBeams.Length; i++)
                    if (sirenBeams[i]) sirenBeams[i].localRotation = Quaternion.Euler(0, 0, -(now * beamSpin + (i % 2 == 1 ? Mathf.PI : 0)) * Mathf.Rad2Deg);
            if (line1)
            {
                float s1 = k < 0.2f ? 1.6f - k * 3 : 1;
                line1.transform.localScale = Vector3.one * s1;
                line1.transform.localRotation = Quaternion.Euler(0, 0, 1.7f);
            }
            if (line2)
            {
                bool on = k > 0.45f;
                if (line2.gameObject.activeSelf != on) line2.gameObject.SetActive(on);
                float s2 = k < 0.6f ? 1.5f - (k - 0.45f) * 3.3f : 1;
                line2.transform.localScale = Vector3.one * s2;
                line2.transform.localRotation = Quaternion.Euler(0, 0, -(0.03f + Mathf.Sin(now * 30) * 0.01f) * Mathf.Rad2Deg);
            }
            if (amount && amount.gameObject.activeSelf != k > 0.8f) amount.gameObject.SetActive(k > 0.8f);
            if (docs)
            {
                float sc = k < 0.25f ? k / 0.25f : 1;
                docs.anchoredPosition = docsPos + new Vector2(0, Mathf.Abs(Mathf.Sin(k * 7)) * 26 * Mathf.Max(0, 1 - k / 1.6f));
                docs.localScale = Vector3.one * sc;
                docs.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(k * 5) * 0.08f * Mathf.Rad2Deg);
            }
            if (papers != null && paperTemplate)
            {
                var area = ((RectTransform)paperTemplate.parent).rect;
                float W = area.width, H = area.height;
                for (int i = 0; i < papers.Length; i++)
                {
                    float u = Mathf.Repeat(k * (0.35f + (i % 4) * 0.08f) + i * 0.137f, 1);
                    float x = (i % 2 == 1 ? u : 1 - u) * (W + 160) - 80, y = H * (0.18f + Mathf.Repeat(i * 0.29f, 0.7f)) + Mathf.Sin(k * 6 + i) * 30;
                    papers[i].anchoredPosition = new Vector2(x, -y);
                    papers[i].sizeDelta = Vector2.one * (paperSize + (i % 3) * paperSizeStep);
                    papers[i].localRotation = Quaternion.Euler(0, 0, -(k * (i % 2 == 1 ? 3 : -3) + i) * Mathf.Rad2Deg);
                }
            }
        }
    }
}
