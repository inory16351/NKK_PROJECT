using NKK.Lobby;
using UnityEngine;

namespace NKK.Ults
{
    /// <summary>
    /// 초필살기 컷신의 쥐별 모습 (신화 15종, 2026-10-17). HUD/SuperUlt/Styles 아래에 쥐마다 하나 (꺼 둔 채로, 값만 씀).
    /// 이름·자막·연출 방식(motif)·수치는 쥐 캐릭터 테이블 Super_Ult / Super_Config, 여기는 그림·색·입자·소리.
    /// 컷 그림 = 뽑기 신화 고유 컷씬 그림 재사용 (Art/Rats/GachaIntro/&lt;code&gt;/*_c1~3): ① 배경·상황 → ② 대표 기술 → ③ 피니시 포즈.
    /// </summary>
    public class SuperUltStyle : MonoBehaviour
    {
        [Tooltip("쥐 code_id (쥐 캐릭터 테이블)")] public string code;
        [Tooltip("레이어 컷신 (Cut/Frame/Scenes/<쥐>, 꺼 둠): 배경·캐릭터·이펙트를 SuperUltLayer 키로 따로 움직임. 있으면 아래 컷 그림 대신 이것")] public RectTransform scene;
        [Tooltip("컷 그림: [0] 컷인(②) · [1] 본 연출(③) · [2] 피니시(④)")] public Sprite[] cuts = new Sprite[3];
        [Tooltip("테마 색 (진입 덮개 · 제목 띠). 알파 0 이면 필살기 테이블 ult_color")] public Color color = new(0, 0, 0, 0);
        [Tooltip("제목 띠가 빠지는 시각 (초, 0 = ② 끝). 클로즈업 컷을 가리지 않게 앞당김")] public float titleOut;
        [Tooltip("월드 위 어둠 (연출 내내, 알파 = 진하기). 햄찌 대정전 = 짙은 남색")] public Color dim = new(0, 0, 0, 0.35f);
        [Tooltip("① 진입 때 어둠이 깜빡이다 꺼지는 횟수 (0 = 깜빡임 없이 테마 색 덮개, 대정전 = 3)")] public int enterFlicker;
        [Tooltip("다단 한 타마다 어둠이 잠깐 걷히는 정도 (0~1, 번개 불빛)")] [Range(0, 1)] public float hitLighten;
        [Tooltip("번개 불빛 색 (어둠이 걷힐 때 살짝 물듦)")] public Color lightenColor = new(1, 0.95f, 0.7f, 1);
        [Tooltip("월드 타격 효과 색 (번개 · 반짝이)")] public Color hitColor = new(1, 0.9f, 0.4f, 1);
        [Tooltip("월드 타격 효과 세기 (번개 굵기 등)")] public float hitSize = 1.4f;
        [Tooltip("다단 한 타에 효과를 그릴 대상 수 (피해는 화면 안 전부)")] public int hitFxTargets = 4;

        [Header("화면 위 피니시 그림 (예: 화면을 가르는 번개)")]
        public Sprite finishArt;
        [Tooltip("크기 (px, 세로) · 기울기 (도)")] public float finishArtSize = 1500, finishArtAngle = -28;
        public Color finishArtColor = Color.white;

        [Header("입자 (뽑기 컷씬과 같은 벡터풍)")]
        [Tooltip("layer · dot 은 비우면 매니저의 것을 씀")] public Blizzard snow = new();
        [Tooltip("컷인 · 본 연출 · 피니시 때 더하는 입자 수")] public int fxCut = 40, fxAct = 40, fxFinish = 0;
        [Tooltip("돌풍 세기: 컷인 · 다단 한 타 · 피니시")] public float gustCut = 2, gustHit = 0.6f, gustFinish = 3;

        [Header("소리 (SfxManager 이름, 비우면 없음)")]
        public string sfxEnter = "jgl_ult_cutin";
        public string sfxCut = "superjump_charge", sfxHit = "laser", sfxFinish = "wall_blast";
    }
}
