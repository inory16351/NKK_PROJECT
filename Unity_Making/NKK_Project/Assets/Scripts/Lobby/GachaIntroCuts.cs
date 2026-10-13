using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    /// <summary>
    /// 컷 그림 몇 장으로 만드는 쥐 고유 컷씬 (뽑기 공개 직전, 사용자 2026-10-12: 전설 이상 · 컷 2~3장 · 약 3초).
    /// 뽑기 연출과 같은 전환(CutFx): 하얀 화면에서 크게 시작해 자리 잡음(펀치 인) → 천천히 확대·이동 → 컷 끝에 앞으로 빨려 들며 하얗게(줌 인 + 플래시)
    /// → 다음 컷. 마지막 하얀 화면은 GachaShow 공개 번쩍으로 이어짐. 그 위로 벡터풍 입자(Blizzard: 눈보라·불티·빗줄기 등)가 컷마다 돌풍과 함께 흩날림.
    /// 쥐마다 이 컴포넌트를 붙인 오브젝트 하나 (UI/GachaShow/Root/Intros 아래, 꺼 둠: Cut · Fx(Dot · Flake 템플릿) · Flash) + GachaShow.intros 에 등록.
    /// </summary>
    public class GachaIntroCuts : GachaIntro
    {
        [System.Serializable]
        public class Step
        {
            public Sprite sprite;
            [Tooltip("이 컷 시간 (초, 펀치 인 · 줌 인 포함)")] public float time = 1f;
            [Tooltip("컷 그림 확대: 시작 → 끝 (천천히)")] public float zoomFrom = 1.1f, zoomTo = 1f;
            [Tooltip("컷 그림 위치 (px): 시작 → 끝")] public Vector2 panFrom, panTo;
            [Tooltip("시작 순간 흔들림 (px) · 흔들림 시간")] public float shake, shakeTime = 0.3f;
            [Tooltip("시작 효과음 (SfxManager 이름, 비우면 없음)")] public string sfx;
            [Tooltip("이 컷에서 더하는 입자 수 · 시작 돌풍 세기 (속도 배율에 더함) · 컷 끝 돌풍 (시작 돌풍에 곱함)")] public int fx;
            public float gust = 1.5f, endGust = 0.6f;
        }

        public Step[] steps;
        [Tooltip("컷 그림 (화면을 덮는 크기, 컷마다 그림만 바꿈)")] public Image cut;
        [Tooltip("하얀 화면 (펀치 인 · 줌 인 플래시)")] public Image flash;
        [Tooltip("컷 전환: 끝 줌 인 · 다음 컷 펀치 인")] public CutTransition trans = new() { pushTime = 0.16f, pushZoom = 1.22f, punchTime = 0.22f, punchFrom = 1.18f };
        [Tooltip("벡터풍 입자 (눈보라 등)")] public Blizzard snow = new();

        public override IEnumerator Play(GachaShow show, GeneGacha.Result r)
        {
            var frame = (RectTransform)transform;           // 펀치·줌·흔들림은 컷씬 전체에
            bool Stop() => show.Skipping;
            CutFx.Alpha(flash, 1);                          // GachaShow 가 하얗게 넘겨 줌
            if (cut) { cut.color = Color.white; cut.preserveAspect = false; }

            for (int i = 0; steps != null && i < steps.Length && !Stop(); i++)
            {
                var s = steps[i];
                if (cut) cut.sprite = s.sprite;
                if (!string.IsNullOrEmpty(s.sfx)) SfxManager.Play(s.sfx);
                snow.Add(s.fx); snow.Gust(s.gust);
                float t = Mathf.Max(trans.punchTime + trans.pushTime + 0.1f, s.time), hold = t - trans.pushTime;
                for (float x = 0; x < hold && !Stop(); x += Time.unscaledDeltaTime)
                {
                    float k = x / t, e = CutFx.Ease(k), p = CutFx.Ease(Mathf.Clamp01(x / trans.punchTime));
                    if (cut) { cut.rectTransform.localScale = Vector3.one * Mathf.Lerp(s.zoomFrom, s.zoomTo, e); cut.rectTransform.anchoredPosition = Vector2.Lerp(s.panFrom, s.panTo, e); }
                    frame.localScale = Vector3.one * Mathf.Lerp(trans.punchFrom, 1, p);
                    CutFx.Alpha(flash, 1 - p);
                    float sh = s.shake * Mathf.Max(0, 1 - x / Mathf.Max(0.01f, s.shakeTime));
                    frame.anchoredPosition = sh > 0 ? Random.insideUnitCircle * sh : Vector2.zero;
                    snow.Tick(Time.unscaledDeltaTime);
                    yield return null;
                }
                frame.anchoredPosition = Vector2.zero;
                // 컷 끝: 앞으로 빨려 들며 하얗게 (돌풍도 같이)
                snow.Gust(s.gust * s.endGust);
                yield return CutFx.PushOut(frame, flash, trans.pushTime, trans.pushZoom, Stop, snow.Tick);
            }
            frame.localScale = Vector3.one; frame.anchoredPosition = Vector2.zero;
            snow.Clear();
        }
    }
}
