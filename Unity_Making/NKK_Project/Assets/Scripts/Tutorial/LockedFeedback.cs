using System.Collections.Generic;
using NKK.Lobby;
using TMPro;
using UnityEngine;

namespace NKK.Tutorial
{
    // 잠긴 로비 기능을 눌렀을 때 반응 (씬 오브젝트 LockedFeedback): 그 기능의 자물쇠(FeatureGate.lockMark)가 흔들리고
    // 안내 말풍선이 잠깐 뜸. 말풍선 글 = 씬 TMP (자리표시 {hint}), 기능별 힌트 = 인스펙터 목록
    public class LockedFeedback : MonoBehaviour
    {
        [System.Serializable] public class Hint { [Tooltip("기능 id (run · rank · rats · skill · dex · rec)")] public string feature; [Tooltip("언제 열리는지")] public string text; }

        public LobbyManager lobby;
        [Tooltip("안내 말풍선 (꺼 둠)")] public CanvasGroup toast;
        [Tooltip("말풍선 글 (자리표시 {hint})")] public TMP_Text toastText;
        public List<Hint> hints = new();
        [Tooltip("말풍선 보이는 시간 · 사라지는 시간 (초)")] public float showTime = 1.8f, fadeTime = 0.3f;
        [Tooltip("자물쇠 흔들림: 시간 (초) · 각도 · 횟수")] public float shakeTime = 0.45f, shakeAngle = 22, shakeCount = 4;
        [Tooltip("자물쇠 통 튀는 크기")] public float shakePop = 0.25f;

        class Shake { public Transform t; public Quaternion rot; public Vector3 scale; public float time; }
        readonly List<Shake> shakes = new();
        string tpl; float toastT = -1;

        void Awake()
        {
            if (toastText) tpl = toastText.text;
            if (toast) { toast.alpha = 0; toast.gameObject.SetActive(false); }
        }
        void OnEnable() { if (lobby) lobby.OnLocked += Show; }
        void OnDisable() { if (lobby) lobby.OnLocked -= Show; }

        public void Show(string feature)
        {
            // 그 기능의 자물쇠 (아지트 팻말 · 위쪽 탭 둘 다)
            foreach (var g in FindObjectsByType<FeatureGate>(FindObjectsSortMode.None))
            {
                if (g.feature != feature || !g.lockMark || !g.lockMark.activeInHierarchy) continue;
                var t = g.lockMark.transform;
                var s = shakes.Find(x => x.t == t);
                if (s == null) shakes.Add(s = new Shake { t = t, rot = t.localRotation, scale = t.localScale });
                s.time = 0;
            }
            // 말풍선
            string hint = ""; foreach (var h in hints) if (h.feature == feature) { hint = h.text; break; }
            if (toastText) toastText.text = (tpl ?? "").Replace("{hint}", hint);
            if (toast) { toast.gameObject.SetActive(true); toastT = 0; }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = shakes.Count - 1; i >= 0; i--)
            {
                var s = shakes[i];
                if (!s.t) { shakes.RemoveAt(i); continue; }
                s.time += dt;
                float k = Mathf.Clamp01(s.time / shakeTime);
                if (k >= 1) { s.t.localRotation = s.rot; s.t.localScale = s.scale; shakes.RemoveAt(i); continue; }
                float a = Mathf.Sin(k * Mathf.PI * 2 * shakeCount) * shakeAngle * (1 - k);
                s.t.localRotation = s.rot * Quaternion.Euler(0, 0, a);
                s.t.localScale = s.scale * (1 + shakePop * Mathf.Sin(k * Mathf.PI));
            }
            if (toast && toastT >= 0)
            {
                toastT += dt;
                float inK = Mathf.Clamp01(toastT / 0.15f), outK = Mathf.Clamp01((toastT - showTime) / fadeTime);
                toast.alpha = inK * (1 - outK);
                toast.transform.localScale = Vector3.one * (0.9f + 0.1f * inK);
                if (outK >= 1) { toastT = -1; toast.gameObject.SetActive(false); }
            }
        }
    }
}
