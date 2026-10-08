using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Hazards
{
    // STAY 컷씬 (30층 우주 사령관 초거대 블랙홀): 인터스텔라 책장 패러디.
    // 블랙홀이 쥐를 다 빨아들이면 Boss 가 Play → 하얗게 번쩍 (플래시 인) → 장면 하나 랜덤 (책장 사이로 과거의 자신을 보며 찍찍!! 말림) → 다시 번쩍 → 콜백 (쥐가 튕겨 나옴)
    // 도는 동안 게임 세계 멈춤 (FxManager.WorldFreeze), 시간은 unscaled. 글은 장면마다 인스펙터
    public class BossCutscene : MonoBehaviour
    {
        [Serializable]
        public class Scene
        {
            public Sprite art;
            [Tooltip("외침 (흔들림)")] public string shout;
            [Tooltip("아래 자막")] public string caption;
        }

        [Header("연결 (씬 HUD/StayCutscene)")]
        [Tooltip("컷씬 전체 (꺼 둠)")] public CanvasGroup group;
        [Tooltip("전환 번쩍 (흰 덮개, 컷씬 맨 위)")] public Image flash;
        public Image art;
        public TMP_Text shout, caption;

        [Header("장면 (랜덤 하나)")]
        public Scene[] scenes;

        [Header("시간 · 연출")]
        [Tooltip("플래시 인 (흰색이 빠지는 시간) · 보여 주는 시간 · 끝 번쩍 (흰색이 차는 시간) (초)")] public float fadeIn = 0.35f, hold = 3.8f, fadeOut = 0.15f;
        [Tooltip("외침 흔들림 (픽셀) · 그림이 천천히 다가오는 정도")] public float shoutShake = 6, zoom = 0.06f;

        float t; bool on; Action done; Vector2 shoutPos;
        public bool Playing => on;

        void Awake()
        {
            if (shout) shoutPos = shout.rectTransform.anchoredPosition;
            if (group) group.gameObject.SetActive(false);
        }

        public void Play(Action onDone)
        {
            if (scenes == null || scenes.Length == 0 || !group) { onDone?.Invoke(); return; }
            var s = scenes[UnityEngine.Random.Range(0, scenes.Length)];
            if (art) art.sprite = s.art;
            if (shout) shout.text = s.shout;
            if (caption) caption.text = s.caption;
            done = onDone; t = 0; on = true;
            group.gameObject.SetActive(true); group.alpha = 1;
            FxManager.WorldFreeze = true;
        }

        void Update()
        {
            if (!on) return;
            t += Time.unscaledDeltaTime;
            float T = fadeIn + hold + fadeOut;
            group.alpha = 1;
            if (flash) { var c = flash.color; c.a = t < fadeIn ? 1 - t / fadeIn : t > fadeIn + hold ? (t - fadeIn - hold) / fadeOut : 0; flash.color = c; }
            if (art) art.rectTransform.localScale = Vector3.one * (1 + zoom * Mathf.Clamp01(t / T));
            if (shout) shout.rectTransform.anchoredPosition = shoutPos + UnityEngine.Random.insideUnitCircle * shoutShake;
            if (t < T) return;
            on = false; group.gameObject.SetActive(false); FxManager.WorldFreeze = false;
            var d = done; done = null; d?.Invoke();
        }

        void OnDisable() { if (on) { on = false; FxManager.WorldFreeze = false; } }
    }
}
