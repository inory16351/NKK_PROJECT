using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 볼륨 설정 (씬 오브젝트, 슬라이더 두 개: 배경음악 · 효과음). 값은 MusicManager · SfxManager 가 저장 (PlayerPrefs).
    // 숫자 글은 씬 TMP 의 자리표시 {n} (0~100) 를 채움. 효과음 슬라이더를 움직이면 미리 듣기 소리
    public class VolumeSettings : MonoBehaviour
    {
        public Slider music, sfx;
        [Tooltip("숫자 글 (자리표시 {n})")] public TMP_Text musicValue, sfxValue;
        [Tooltip("효과음 미리 듣기 소리 이름")] public string previewSfx = "cheese";

        string musicTpl, sfxTpl;
        bool init;
        float lastPreview;

        void Awake()
        {
            musicTpl = musicValue ? musicValue.text : "{n}"; sfxTpl = sfxValue ? sfxValue.text : "{n}";
            if (music) music.onValueChanged.AddListener(v => { if (!init) return; if (MusicManager.I) MusicManager.I.SetVolume(v); Show(); });
            if (sfx) sfx.onValueChanged.AddListener(v =>
            {
                if (!init) return;
                if (SfxManager.I) SfxManager.I.SetVolume(v);
                if (Time.unscaledTime - lastPreview > 0.12f) { lastPreview = Time.unscaledTime; SfxManager.Play(previewSfx); }
                Show();
            });
        }

        void OnEnable()
        {
            init = false;
            if (music) music.SetValueWithoutNotify(MusicManager.I ? MusicManager.I.volume : PlayerPrefs.GetFloat("nkk_bgm_vol", 0.6f));
            if (sfx) sfx.SetValueWithoutNotify(SfxManager.I ? SfxManager.I.volume : PlayerPrefs.GetFloat("nkk_sfx_vol", 0.8f));
            Show();
            init = true;
        }

        void Show()
        {
            if (musicValue && music) musicValue.text = musicTpl.Replace("{n}", Mathf.RoundToInt(music.value * 100).ToString());
            if (sfxValue && sfx) sfxValue.text = sfxTpl.Replace("{n}", Mathf.RoundToInt(sfx.value * 100).ToString());
        }
    }
}
