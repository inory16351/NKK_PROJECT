using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Hazards
{
    // 보스 컷씬 두 가지 (씬 HUD/StayCutscene). 도는 동안 게임 세계 멈춤 (FxManager.WorldFreeze), 시간은 unscaled
    // · STAY (30층 우주 사령관 초거대 블랙홀): 인터스텔라 책장 패러디. 블랙홀이 쥐를 다 빨아들이면 Boss 가 Play
    //   → 하얗게 번쩍 (플래시 인) → 장면 하나 랜덤 (책장 사이로 과거의 자신을 보며 찍찍!! 말림) → 다시 번쩍 → 콜백 (쥐가 튕겨 나옴)
    // · 긴급 회의 (15층 연구소장 긴급 비상 회의): 어몽어스 긴급 회의 화면 + 보스 얼굴 (PlayMeeting)
    // 글은 장면마다 · 회의 제목 모두 인스펙터/씬
    public class BossCutscene : MonoBehaviour
    {
        [Serializable]
        public class Scene
        {
            public Sprite art;
            [Tooltip("외침 (흔들림)")] public string shout;
            [Tooltip("아래 자막")] public string caption;
        }

        [Header("STAY (씬 StayCutscene/Panel)")]
        [Tooltip("STAY 화면 (꺼 둠)")] public CanvasGroup group;
        public Image art;
        public TMP_Text shout, caption;
        public Scene[] scenes;

        [Header("긴급 회의 (씬 StayCutscene/Meeting)")]
        [Tooltip("긴급 회의 화면 (꺼 둠)")] public CanvasGroup meetingGroup;
        [Tooltip("가운데 보스 얼굴 (쾅 하고 커졌다 작아짐)")] public Image meetingFace;
        [Tooltip("제목 (흔들림)")] public TMP_Text meetingTitle;
        [Tooltip("보여 주는 시간 (초) · 얼굴이 처음 커지는 정도")] public float meetingHold = 2f, meetingPunch = 0.35f;

        [Header("전환 · 연출")]
        [Tooltip("전환 번쩍 (흰 덮개, 맨 위)")] public Image flash;
        [Tooltip("플래시 인 (흰색이 빠지는 시간) · STAY 보여 주는 시간 · 끝 번쩍 (흰색이 차는 시간) (초)")] public float fadeIn = 0.35f, hold = 3.8f, fadeOut = 0.15f;
        [Tooltip("외침 흔들림 (픽셀) · 그림이 천천히 다가오는 정도")] public float shoutShake = 6, zoom = 0.06f;

        float t, curHold; bool on; Action done; CanvasGroup cur; TMP_Text curShake;
        Vector2 shoutPos, titlePos;
        public bool Playing => on;

        void Awake()
        {
            if (shout) shoutPos = shout.rectTransform.anchoredPosition;
            if (meetingTitle) titlePos = meetingTitle.rectTransform.anchoredPosition;
            if (group) group.gameObject.SetActive(false);
            if (meetingGroup) meetingGroup.gameObject.SetActive(false);
            if (flash) { var c = flash.color; c.a = 0; flash.color = c; }
        }

        public void Play(Action onDone)
        {
            if (on || scenes == null || scenes.Length == 0 || !group) { onDone?.Invoke(); return; }
            var s = scenes[UnityEngine.Random.Range(0, scenes.Length)];
            if (art) art.sprite = s.art;
            if (shout) shout.text = s.shout;
            if (caption) caption.text = s.caption;
            Begin(group, hold, shout, onDone);
        }

        public void PlayMeeting(Sprite face, Action onDone)
        {
            if (on || !meetingGroup) { onDone?.Invoke(); return; }
            if (meetingFace) { meetingFace.sprite = face; meetingFace.enabled = face; }
            Begin(meetingGroup, meetingHold, meetingTitle, onDone);
        }

        void Begin(CanvasGroup g, float h, TMP_Text shake, Action onDone)
        {
            done = onDone; t = 0; on = true; cur = g; curHold = h; curShake = shake;
            g.gameObject.SetActive(true); g.alpha = 1;
            FxManager.WorldFreeze = true;
        }

        void Update()
        {
            if (!on) return;
            t += Time.unscaledDeltaTime;
            float T = fadeIn + curHold + fadeOut;
            if (flash) { var c = flash.color; c.a = t < fadeIn ? 1 - t / fadeIn : t > fadeIn + curHold ? (t - fadeIn - curHold) / fadeOut : 0; flash.color = c; }
            if (cur == group && art) art.rectTransform.localScale = Vector3.one * (1 + zoom * Mathf.Clamp01(t / T));
            if (cur == meetingGroup && meetingFace) meetingFace.rectTransform.localScale = Vector3.one * (1 + meetingPunch * Mathf.Exp(-t * 6) + 0.03f * Mathf.Sin(t * 20));
            if (curShake) curShake.rectTransform.anchoredPosition = (curShake == shout ? shoutPos : titlePos) + UnityEngine.Random.insideUnitCircle * shoutShake;
            if (t < T) return;
            Stop();
            var d = done; done = null; d?.Invoke();
        }

        void Stop()
        {
            on = false; FxManager.WorldFreeze = false;
            if (cur) cur.gameObject.SetActive(false);
            if (flash) { var c = flash.color; c.a = 0; flash.color = c; }
        }

        void OnDisable() { if (on) Stop(); }
    }
}
