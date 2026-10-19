using UnityEngine;
using UnityEngine.UI;

namespace NKK.Tutorial
{
    // 기능 잠금 표시 (튜토리얼 테이블 Unlock_Id). 잠겨 있으면 숨기거나, 자물쇠를 띄우고 버튼을 막음.
    // 해금은 TutorialManager 가 단계의 unlock 칸으로 함 (Progress.Unlock)
    public class FeatureGate : MonoBehaviour
    {
        [Tooltip("기능 id: promote · power · pop · run · rec · skill · rats · rank · dex")] public string feature;
        [Tooltip("잠겨 있으면 꺼 둘 오브젝트 (예: 게임 HUD 승급 버튼)")] public GameObject[] hideWhenLocked;
        [Tooltip("잠겨 있을 때만 켜 둘 오브젝트 (열리면 꺼짐 — 예: 증강 연구소 자리에 있던 장식)")] public GameObject[] showWhenLocked;
        [Tooltip("잠겨 있으면 켤 자물쇠 표시")] public GameObject lockMark;
        [Tooltip("잠겨 있으면 못 누르게 할 버튼")] public Button[] buttons;
        [Tooltip("잠겨 있으면 이 투명도 (CanvasGroup 이 있을 때)")] public CanvasGroup dim;
        [Range(0, 1)] public float lockedAlpha = 0.6f;
        [Tooltip("잠겨 있으면 끌 컴포넌트 (예: 로비 물건 버튼의 BoxCollider2D → 안 보일 때 눌리지도 않게)")] public Behaviour[] disableWhenLocked;
        [Header("열릴 때 등장 (hideWhenLocked)")]
        [Tooltip("보고 있는 중에 열리면 hideWhenLocked 를 작게 시작해 통 튀어나오게 (초, 0 = 그냥 켬)")] public float revealTime = 0.45f;
        [Tooltip("튀어나올 때 넘치는 정도")] public float revealOvershoot = 1.6f;
        [Tooltip("튀어나올 때 효과음 (비우면 없음)")] public string revealSfx = "";

        int ver = -1;
        bool wasLocked, applied;
        Transform[] revTr; Vector3[] revScale;      // 튀어나오는 중인 대상 · 원래 크기

        // 튀어나오는 중 (크기가 0 부근에서 커지는 중) — TutorialTarget 이 이때 크기를 '원래 크기'로 잡으면 안 됨
        static readonly System.Collections.Generic.HashSet<Transform> revealing = new();
        public static bool IsRevealing(Transform t) => t && revealing.Contains(t);

        public bool Locked => Progress.I && !Progress.I.IsUnlocked(feature);

        void OnEnable() { ver = -1; applied = false; Apply(); }
        // 튀어나오는 중에 꺼지면 (씬 이동 등) 크기를 원래대로 — 작은 크기로 굳지 않게
        void OnDisable() => EndReveal();
        void Update() { if (Progress.I && Progress.I.UnlockVersion != ver) Apply(); }

        void Apply()
        {
            ver = Progress.I ? Progress.I.UnlockVersion : 0;
            bool locked = Locked;
            if (hideWhenLocked != null) foreach (var g in hideWhenLocked) if (g) g.SetActive(!locked);
            if (showWhenLocked != null) foreach (var g in showWhenLocked) if (g) g.SetActive(locked);
            if (lockMark) lockMark.SetActive(locked);
            if (buttons != null) foreach (var b in buttons) if (b) b.interactable = !locked;
            if (disableWhenLocked != null) foreach (var b in disableWhenLocked) if (b) b.enabled = !locked;
            if (dim) dim.alpha = locked ? lockedAlpha : 1;
            // 화면을 보고 있는 중에 열림 → 통 튀어나옴 (씬 시작 때 이미 열려 있으면 그냥 켬)
            if (applied && wasLocked && !locked && revealTime > 0 && hideWhenLocked != null && hideWhenLocked.Length > 0)
            {
                StopAllCoroutines(); EndReveal(); StartCoroutine(Reveal());
                if (!string.IsNullOrEmpty(revealSfx)) SfxManager.Play(revealSfx);
            }
            wasLocked = locked; applied = true;
        }

        System.Collections.IEnumerator Reveal()
        {
            revTr = new Transform[hideWhenLocked.Length]; revScale = new Vector3[revTr.Length];
            for (int i = 0; i < revTr.Length; i++) if (hideWhenLocked[i]) { revTr[i] = hideWhenLocked[i].transform; revScale[i] = revTr[i].localScale; revealing.Add(revTr[i]); }
            for (float t = 0; t < revealTime; t += Time.unscaledDeltaTime)
            {
                // 뒤로 넘치는 이징 (0 → 1 을 살짝 넘었다 돌아옴)
                float x = t / revealTime - 1, k = 1 + (revealOvershoot + 1) * x * x * x + revealOvershoot * x * x;
                for (int i = 0; i < revTr.Length; i++) if (revTr[i]) revTr[i].localScale = revScale[i] * k;
                yield return null;
            }
            EndReveal();
        }

        void EndReveal()
        {
            if (revTr == null) return;
            for (int i = 0; i < revTr.Length; i++) if (revTr[i]) { revTr[i].localScale = revScale[i]; revealing.Remove(revTr[i]); }
            revTr = null; revScale = null;
        }
    }
}
