using UnityEngine;

namespace NKK
{
    // 이 씬의 배경음악 (씬 오브젝트 SceneMusic → MusicManager 가 틈). Title: 오프닝 컷씬 중엔 storyClip.
    // Game 씬: 층 구역별 곡(구역 시작 층 이상 중 가장 높은 것) · 보스전 중엔 보스 곡(최종 보스 따로) · 남은 시간 촉박하면 hurry 곡
    public class SceneMusic : MonoBehaviour
    {
        [Tooltip("이 씬 기본 곡")] public AudioClip clip;
        [Tooltip("이 컷씬이 도는 동안은 storyClip (Title 씬 오프닝)")] public Tutorial.StoryPlayer story;
        public AudioClip storyClip;

        [System.Serializable] public class ZoneClip { [Tooltip("이 층부터")] public int fromFloor = 1; public AudioClip clip; }
        [Header("Game 씬")]
        public GameManager Game;
        [Tooltip("층 구역별 곡 (비어 있으면 기본 곡)")] public ZoneClip[] zones = new ZoneClip[0];
        [Tooltip("보스전 곡")] public AudioClip bossClip;
        [Tooltip("이 층 이상 보스(최종 보스) 곡 · 보스 층")] public AudioClip finalBossClip;
        public int finalBossFloor = 30;
        [Tooltip("남은 시간이 hurryAt 초 이하면 이 곡 (보스전 중엔 보스 곡 그대로)")] public RunTimer Timer;
        public AudioClip hurryClip;
        public float hurryAt = 30;

        void Update()
        {
            var m = MusicManager.I; if (!m) return;
            m.Play(Pick());
        }

        AudioClip Pick()
        {
            if (story && storyClip && story.Playing) return storyClip;
            var boss = Hazards.Boss.Current;
            if (boss && boss.State == Hazards.Boss.BState.Fight)
            {
                if (finalBossClip && boss.Data != null && boss.Data.floor >= finalBossFloor) return finalBossClip;
                if (bossClip) return bossClip;
            }
            if (hurryClip && Timer && Timer.Left > 0 && Timer.Left <= hurryAt && !GameOver.Active) return hurryClip;
            if (Game && zones != null && zones.Length > 0)
            {
                AudioClip best = null; int bestFrom = int.MinValue;
                foreach (var z in zones) if (z.clip && Game.Floor >= z.fromFloor && z.fromFloor > bestFrom) { best = z.clip; bestFrom = z.fromFloor; }
                if (best) return best;
            }
            return clip;
        }
    }
}
