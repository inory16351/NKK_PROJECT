using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NKK
{
    // 효과음 (씬 오브젝트 SfxManager, 씬이 바뀌어도 하나만 남음). 이름으로 틀기: SfxManager.Play("smash_glass").
    // 이름 = Assets/Audio/SFX 파일 이름 (끝의 _1 _2 … 는 같은 소리 변주 → 랜덤). 목록은 컴포넌트 메뉴 'Fill From Folder' 로 채움.
    // 같은 소리가 너무 겹치지 않게 이름마다 최소 간격 · 동시 재생 수 제한 (웹 프로토 gate 와 같음).
    // 화면 밖에서 난 소리는 PlayAt 이 무시. 버튼 클릭 소리는 여기서 자동 (누를 수 있는 UI 위에서 마우스를 누르면)
    [DefaultExecutionOrder(-800)]
    public class SfxManager : MonoBehaviour
    {
        public static SfxManager I { get; private set; }

        [System.Serializable]
        public class Entry
        {
            public string name;
            public AudioClip[] clips = new AudioClip[0];
            [Range(0, 1)] public float volume = 0.7f;
            [Tooltip("같은 소리 최소 간격 (초)")] public float minGap = 0.05f;
            [Tooltip("동시에 울릴 수 있는 수")] public int maxVoices = 3;
            [Tooltip("음 높이 흔들림 (±)")] public float pitchJitter = 0.05f;
        }

        [Tooltip("효과음 볼륨 (0~1, 저장됨)")] [Range(0, 1)] public float volume = 0.8f;
        [Tooltip("동시에 쓰는 AudioSource 수")] public int voices = 16;
        [Tooltip("UI 버튼 클릭 소리 이름 (빈칸 = 끔)")] public string uiClick = "ui_click";
        public List<Entry> entries = new();
        const string VolPref = "nkk_sfx_vol";

        readonly Dictionary<string, Entry> map = new();
        readonly Dictionary<string, float> lastTime = new();
        readonly Dictionary<string, List<AudioSource>> playing = new();
        AudioSource[] pool; int next;

        void Awake()
        {
            if (I && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            volume = PlayerPrefs.GetFloat(VolPref, volume);
            foreach (var e in entries) if (!string.IsNullOrEmpty(e.name)) map[e.name] = e;
            pool = new AudioSource[Mathf.Max(4, voices)];
            for (int i = 0; i < pool.Length; i++) { var s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 0; pool[i] = s; }
        }

        public void SetVolume(float v) { volume = Mathf.Clamp01(v); PlayerPrefs.SetFloat(VolPref, volume); }

        // 어디서나: 이름으로 재생 (없는 이름은 조용히 무시)
        public static void Play(string name, float vol = 1, float pitch = 1) { if (I) I.PlayInternal(name, vol, pitch); }
        // 게임 좌표 (x, y) 에서 난 소리: 화면 밖이면 안 틂
        public static void PlayAt(string name, float x, float y, float vol = 1, float pitch = 1)
        {
            if (!I) return;
            var cam = Camera.main;
            if (cam)
            {
                var v = cam.WorldToViewportPoint(World.ToUnity(x, y));
                if (v.x < -0.1f || v.x > 1.1f || v.y < -0.1f || v.y > 1.1f) return;
            }
            I.PlayInternal(name, vol, pitch);
        }

        void PlayInternal(string name, float vol, float pitch)
        {
            if (string.IsNullOrEmpty(name) || !map.TryGetValue(name, out var e) || e.clips.Length == 0) return;
            float now = Time.unscaledTime;
            if (lastTime.TryGetValue(name, out var t0) && now - t0 < e.minGap) return;
            if (!playing.TryGetValue(name, out var list)) playing[name] = list = new List<AudioSource>();
            list.RemoveAll(s => !s || !s.isPlaying);
            if (list.Count >= e.maxVoices) return;
            var clip = e.clips[Random.Range(0, e.clips.Length)]; if (!clip) return;
            // 쉬는 AudioSource (없으면 가장 오래된 것)
            AudioSource src = null;
            for (int i = 0; i < pool.Length; i++) { var s = pool[(next + i) % pool.Length]; if (!s.isPlaying) { src = s; next = (next + i + 1) % pool.Length; break; } }
            if (!src) { src = pool[next]; next = (next + 1) % pool.Length; }
            src.clip = clip;
            src.volume = Mathf.Clamp01(e.volume * vol * volume);
            src.pitch = pitch * (1 + Random.Range(-e.pitchJitter, e.pitchJitter));
            src.ignoreListenerPause = true;
            src.Play();
            lastTime[name] = now; list.Add(src);
        }

        // 버튼 클릭 소리: 누를 수 있는 UI(Button · Toggle …) 위에서 마우스 왼쪽을 누르면
        void Update()
        {
            if (string.IsNullOrEmpty(uiClick)) return;
            var m = Mouse.current; var es = EventSystem.current;
            if (m == null || es == null || !m.leftButton.wasPressedThisFrame) return;
            var data = new PointerEventData(es) { position = m.position.ReadValue() };
            var hits = new List<RaycastResult>(); es.RaycastAll(data, hits);
            if (hits.Count == 0) return;
            var sel = hits[0].gameObject.GetComponentInParent<Selectable>();
            if (sel && sel.IsInteractable()) PlayInternal(uiClick, 1, 1);
        }

#if UNITY_EDITOR
        // Assets/Audio/SFX 의 wav 를 이름별로 묶어 목록 채우기 (끝의 _숫자 = 변주). 이미 있는 항목의 볼륨 · 간격은 유지
        [ContextMenu("Fill From Folder")]
        void FillFromFolder()
        {
            var groups = new SortedDictionary<string, List<AudioClip>>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" }))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                string n = System.IO.Path.GetFileNameWithoutExtension(path);
                var mt = System.Text.RegularExpressions.Regex.Match(n, @"^(.*)_(\d)$");     // rat_squeak_1 → rat_squeak (combo_02 처럼 두 자리는 따로)
                if (mt.Success) n = mt.Groups[1].Value;
                if (!groups.TryGetValue(n, out var l)) groups[n] = l = new List<AudioClip>();
                l.Add(clip);
            }
            var old = new Dictionary<string, Entry>(); foreach (var e in entries) if (e != null && e.name != null) old[e.name] = e;
            entries.Clear();
            foreach (var kv in groups)
            {
                var e = old.TryGetValue(kv.Key, out var o) ? o : new Entry { name = kv.Key };
                e.clips = kv.Value.ToArray();
                entries.Add(e);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SfxManager] {entries.Count} 개 소리");
        }
#endif
    }
}
