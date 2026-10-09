using UnityEngine;

namespace NKK
{
    // 이 씬의 배경음악 (씬 오브젝트 SceneMusic → MusicManager 가 틈).
    // Game 씬: 층 구역별 곡(구역 시작 층 이상 중 가장 높은 것) · 보스전 중엔 보스 곡
    public class SceneMusic : MonoBehaviour
    {
        [Tooltip("이 씬 기본 곡")] public AudioClip clip;

        [System.Serializable] public class ZoneClip { [Tooltip("이 층부터")] public int fromFloor = 1; public AudioClip clip; }
        [Header("Game 씬")]
        public GameManager Game;
        [Tooltip("층 구역별 곡 (비어 있으면 기본 곡)")] public ZoneClip[] zones = new ZoneClip[0];
        [Tooltip("보스전 곡")] public AudioClip bossClip;

        void Update()
        {
            var m = MusicManager.I; if (!m) return;
            m.Play(Pick());
        }

        AudioClip Pick()
        {
            var boss = Hazards.Boss.Current;
            if (bossClip && boss && boss.State == Hazards.Boss.BState.Fight) return bossClip;
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
