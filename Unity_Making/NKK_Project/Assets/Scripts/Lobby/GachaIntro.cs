using System.Collections;
using UnityEngine;

namespace NKK.Lobby
{
    /// <summary>
    /// 뽑기 공개 직전에 끼어드는 쥐 고유 컷씬 (전설 이상, 2026-10-12 고려 중 — 아직 만든 컷씬 없음).
    /// 쓰는 법: 이 클래스를 상속한 컴포넌트를 UI/GachaShow/Root 아래 꺼 둔 오브젝트에 붙이고 code 를 쥐 code_id 로 →
    /// GachaShow.intros 에 넣으면, 그 쥐가 공개될 때 실루엣 단계 대신 Play 가 돌고 끝나면 번쩍 → 공개로 이어짐.
    /// 건너뛰기는 show.Skipping 을 보고 바로 끝낼 것. 시간은 Time.unscaledDeltaTime.
    /// </summary>
    public abstract class GachaIntro : MonoBehaviour
    {
        [Tooltip("쥐 code_id (RatTable)")] public string code;
        [Tooltip("처음 뽑았을 때만 (중복으로 또 뽑으면 보통 공개)")] public bool firstOnly = true;

        public abstract IEnumerator Play(GachaShow show, GeneGacha.Result r);
    }
}
