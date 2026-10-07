using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 판 진행 상태: 층·티어·치즈·콤보 + HUD (치즈·콤보·찍찍!!·층 정보·배너·화면 전환).
    public class GameManager : MonoBehaviour
    {
        [Header("진행")]
        [Tooltip("현재 층 (물건 체력·치즈·구간이 이 값으로 정해짐)")] public int Floor = 1;
        [Tooltip("현재 티어 (훈장). 탄생 등급 배율·해금 쥐가 이 값으로 정해짐")] public int Tier = 1;
        public double Cheese;

        [Header("콤보 (웹게임 기준)")]
        [Tooltip("콤보 유지 시간 (초)")] public float comboTime = 1.6f;
        [Tooltip("콤보 1당 치즈 배율 증가")] public float comboStep = 0.005f;
        public int comboCap = 200;

        [Header("HUD (Canvas 자식)")]
        public TMP_Text cheeseText;
        public TMP_Text comboText;
        [Tooltip("찍찍!! (무리 전투력) / 적정")] public TMP_Text powerText;
        [Tooltip("층 · 방 수")] public TMP_Text floorText;
        public TMP_Text bannerText;
        public TMP_Text bannerSubText;
        [Tooltip("층 이동 때 화면을 덮는 검은 이미지")] public Image fade;
        public CameraController cam;
        [Tooltip("배너 표시 시간 (초)")] public float bannerTime = 2.4f;
        [Tooltip("층 이동 페이드 전체 시간 (초)")] public float fadeTime = 1.2f;

        public int Combo { get; private set; }
        float comboT, bannerT;

        public float ComboMult => 1 + Mathf.Min(Combo, comboCap) * comboStep;

        public void Earn(double v) => Cheese += v;

        [Header("저장 (Progress)")]
        [Tooltip("치즈를 진행도에 저장하는 간격 (초)")] public float saveInterval = 5;
        float saveT;

        void Awake()
        {
            // 로비에서 들어오면 고른 시작 층, 티어·치즈는 저장된 값으로 (게임 씬을 바로 켜면 인스펙터 층)
            var p = Progress.I;
            if (p)
            {
                if (Progress.PendingStartFloor > 0) { Floor = Progress.PendingStartFloor; Progress.PendingStartFloor = 0; }
                Tier = p.tier; Cheese = p.cheese;
            }
        }

        // 판에서 번 치즈를 진행도에 반영 (판이 끝나도 남음)
        public void SaveProgress()
        {
            var p = Progress.I; if (!p) return;
            p.cheese = Cheese; p.OnFloorReached(Floor); p.Save();
        }
        void OnApplicationQuit() => SaveProgress();

        public void OnSmash(float baseGain, int comboAdd = 1)
        {
            Combo += comboAdd; comboT = comboTime * CommonSkill.ComboTimeMul;
            Earn(baseGain * ComboMult);
        }

        public void AddCombo(int n) { Combo += n; comboT = comboTime * CommonSkill.ComboTimeMul; }

        public void ShowBanner(string text, string sub = "")
        {
            if (bannerText) bannerText.text = text;
            if (bannerSubText) bannerSubText.text = sub;
            bannerT = bannerTime;
        }

        public void SetStageInfo(float power, float need, int open, int total)
        {
            if (powerText) powerText.text = $"찍찍!! {Format(power)} / 적정 {Format(need)}";
            if (floorText) floorText.text = $"{Floor}층 · 방 {open}/{total}";
        }

        // 까맣게 → action → 밝아짐
        public void FadeThen(Action action) => StartCoroutine(FadeRoutine(action));
        IEnumerator FadeRoutine(Action action)
        {
            float h = fadeTime / 2;
            for (float t = 0; t < h; t += Time.deltaTime) { SetFade(t / h); yield return null; }
            SetFade(1); action?.Invoke();
            for (float t = 0; t < h; t += Time.deltaTime) { SetFade(1 - t / h); yield return null; }
            SetFade(0);
        }
        void SetFade(float a) { if (fade) { fade.enabled = a > 0.001f; fade.color = new Color(0, 0, 0, a); } }

        void Update()
        {
            if ((saveT -= Time.unscaledDeltaTime) <= 0) { saveT = saveInterval; SaveProgress(); }
            if (Combo > 0 && (comboT -= Time.deltaTime) <= 0) Combo = 0;
            if (cheeseText) cheeseText.text = $"치즈 {Format(Cheese)}";
            if (comboText) comboText.text = Combo >= 2 ? $"{Combo} 콤보!" : "";
            if (bannerT > 0)
            {
                bannerT -= Time.deltaTime;
                float a = Mathf.Clamp01(Mathf.Min(bannerT, bannerTime - bannerT) * 4);
                if (bannerText) bannerText.color = new Color(1, 1, 1, a);
                if (bannerSubText) bannerSubText.color = new Color(1, 1, 1, a);
            }
            else
            {
                if (bannerText) bannerText.color = Color.clear;
                if (bannerSubText) bannerSubText.color = Color.clear;
            }
        }

        // 웹게임 fmt: 1,234 → 1.23K → 1.23M ...
        public static string Format(double n)
        {
            if (n < 1000) return Mathf.FloorToInt((float)n).ToString();
            string[] u = { "K", "M", "B", "T", "aa", "ab", "ac", "ad", "ae", "af" };
            int i = -1; while (n >= 1000 && i < u.Length - 1) { n /= 1000; i++; }
            return (n < 10 ? n.ToString("0.00") : n < 100 ? n.ToString("0.0") : n.ToString("0")) + u[i];
        }
    }
}
