using UnityEngine;

namespace NKK
{
    // 배경음악 (씬 오브젝트 MusicManager, 씬이 바뀌어도 하나만 남음). 곡이 바뀌면 두 AudioSource 로 겹쳐 바꿈.
    // 어떤 곡을 틀지는 씬마다 SceneMusic 이 정함. 튜토리얼 대사 · 포기 창 중엔 소리를 줄임
    [DefaultExecutionOrder(-800)]
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager I { get; private set; }

        [Tooltip("배경음악 볼륨 (0~1, 저장됨)")] [Range(0, 1)] public float volume = 0.6f;
        [Tooltip("곡이 바뀔 때 겹치는 시간 (초)")] public float crossTime = 1.2f;
        [Tooltip("튜토리얼 대사 · 포기 창 중 볼륨 배율")] [Range(0, 1)] public float duck = 0.4f;
        const string VolPref = "nkk_bgm_vol";

        AudioSource a, b;          // a = 지금 곡
        float k = 1, duckK = 1;    // k: 겹쳐 바꾸기 진행 (1 = 끝)
        public AudioClip Current => a ? a.clip : null;

        void Awake()
        {
            if (I && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            volume = PlayerPrefs.GetFloat(VolPref, volume);
            a = Make(); b = Make();
        }

        AudioSource Make()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true; s.playOnAwake = false; s.spatialBlend = 0; s.ignoreListenerPause = true;
            return s;
        }

        public void SetVolume(float v) { volume = Mathf.Clamp01(v); PlayerPrefs.SetFloat(VolPref, volume); }

        // 같은 곡이면 그대로 (씬이 바뀌어도 이어서). null = 조용히 끔
        public void Play(AudioClip clip)
        {
            if (a.clip == clip && (clip == null || a.isPlaying)) return;
            (a, b) = (b, a);           // 지금 곡은 b 로 → 줄어듦
            a.clip = clip; a.volume = 0;
            if (clip) a.Play(); else a.Stop();
            k = 0;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool ducked = Tutorial.TutorialBox.Showing || FxManager.Paused;
            duckK = Mathf.MoveTowards(duckK, ducked ? duck : 1, dt * 2);
            k = Mathf.Min(1, k + (crossTime > 0 ? dt / crossTime : 1));
            float v = volume * duckK;
            a.volume = v * k;
            b.volume = v * (1 - k);
            if (k >= 1 && b.isPlaying) b.Stop();
        }
    }
}
