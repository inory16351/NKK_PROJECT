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
        [Tooltip("켜면 컷신에 글을 안 띄움: 제목 띠 · 자막(본 연출 · 마무리) · 히트 수 (2026-10-19 기사, 사용자)")] public bool hideText;
        [Tooltip("제목 띠가 빠지는 시각 (초, 0 = ② 끝). 클로즈업 컷을 가리지 않게 앞당김")] public float titleOut;
        [Tooltip("월드 위 어둠 (연출 내내, 알파 = 진하기). 햄찌 대정전 = 짙은 남색")] public Color dim = new(0, 0, 0, 0.35f);
        [Tooltip("① 진입 때 어둠이 깜빡이다 꺼지는 횟수 (0 = 깜빡임 없이 테마 색 덮개, 대정전 = 3)")] public int enterFlicker;
        [Tooltip("다단 한 타마다 어둠이 잠깐 걷히는 정도 (0~1, 번개 불빛)")] [Range(0, 1)] public float hitLighten;
        [Tooltip("번개 불빛 색 (어둠이 걷힐 때 살짝 물듦)")] public Color lightenColor = new(1, 0.95f, 0.7f, 1);
        [Tooltip("월드 타격 효과 색 (번개 · 반짝이)")] public Color hitColor = new(1, 0.9f, 0.4f, 1);
        [Tooltip("월드 타격 효과 세기 (번개 굵기 등)")] public float hitSize = 1.4f;
        [Tooltip("다단 한 타에 효과를 그릴 대상 수 (피해는 화면 안 전부)")] public int hitFxTargets = 4;

        [Header("피니시 컷 없음 (2026-10-19 사용자: 아이스 퀸부터 마지막 컷신 제거)")]
        [Tooltip("켜면 ④ 피니시 컷(펀치 인 · 피니시 그림) 없이 ③ 본 연출이 actEnd 까지 이어지고, 마지막 일격 피해만 주고 t_end 까지 걷힘")] public bool noFinishCut;
        [Tooltip("noFinishCut 일 때 ③ 본 연출이 끝나는 시각 (초) = 마지막 일격 · 걷힘 시작. 전체 길이는 Super_Config t_end")] public float actEnd = 4.5f;
        [Tooltip("마지막 일격 하얀 번쩍 세기 (0~1)")] [Range(0, 1)] public float finishFlash = 1;

        [Header("피해 표시 (2026-10-19 사용자: 화면 전체 컷신 중에도 게임에 피해가 들어가는 느낌)")]
        [Tooltip("다단 한 타 · 마지막 일격마다 실제로 준 피해 숫자를 컷신 위에 띄우고 누적 피해를 셈 (매니저 dmgTemplate · total)")] public bool hitNumbers;
        [Tooltip("다단 한 타마다 숫자가 뜨는 자리 (화면 px, 가운데 0 · 위 +). 레이어 컷신에서 고드름 등이 꽂히는 자리와 맞춤. 마지막 = 일격 자리")] public Vector2[] hitPoints;
        [Tooltip("다단 한 타 화면 흔들림 px (0 = 매니저 값)")] public float hitShake;

        [Tooltip("컷신 재생 속도 (1 = 그대로, 0.75 = 느리게). 컷신이 걷힌 뒤 게임 화면 단계는 제 속도 (2026-10-19 아이스 퀸 0.75)")] public float playRate = 1;

        [Header("컷신 뒤 게임 화면 단계 (2026-10-19 아이스 퀸: 얼음성 컷신 → 하늘에서 눈보라 광역)")]
        [Tooltip("켜면 다단 히트 · 마지막 일격을 컷신이 걷힌 뒤 게임 화면에서 (noFinishCut 과 같이: 컷신 = actEnd 까지, cutFade 동안 걷힘)")] public bool worldPhase;
        [Tooltip("컷신 걷히는 시간 · 게임 화면 단계 길이 (초)")] public float cutFade = 0.4f, worldTime = 3;
        [Tooltip("눈보라: 몇 초마다 · 한 번에 몇 개 · 색")] public float blizzardGap = 0.03f; public int blizzardCount = 4; public Color blizzardColor = new(0.94f, 0.98f, 1f, 1);
        [Tooltip("우박: 한 번에 바닥에 떨어지는 알갱이 수 (얼음 조각이 튐, 0 = 안 함)")] public int hailCount;
        [Header("그림 이펙트 (2026-10-19 사용자: 벡터 점 대신 Codex 그림)")]
        [Tooltip("켜면 게임 화면 단계의 눈보라 · 우박 · 타격 효과를 코드 점 파티클 대신 아래 그림으로 (컷신 밖 오버레이 HUD/SuperUlt/WorldSnow)")] public bool imageFx;
        [Tooltip("떨어지는 우박 그림 (꼬리 달린 알갱이 · 둥근 알갱이) · 큰 얼음 덩어리 그림")] public Sprite[] hailFall, hailChunk;
        [Tooltip("바닥 충돌 프레임 (순서대로)")] public Sprite[] hailSplash;
        [Tooltip("바닥 충돌 크기 (px, 가로): 우박 하나 · 대상 타격 · 마지막 일격 · 프레임 시간 (초)")] public float splashSize = 90, splashHitSize = 220, splashFinishSize = 420, splashFrame = 0.07f;
        [Tooltip("게임 화면 입자 바람 (px/초, 0 = 매니저 값) · 입자 크기 (0 = 매니저 값) — 기사 = 위로 떠오르는 빛 입자")] public Vector2 worldWind, worldDotSize;
        [Tooltip("다단 한 타마다 화면을 가르는 베기 그림 (2026-10-19 기사: 거대 유령 검 연속 베기) · 길이 (px) · 보이는 시간")] public Sprite[] slashSprites;
        public float slashSize = 1500, slashLife = 0.28f;
        [Tooltip("마지막 일격: 화면을 왼→오로 쓸고 가는 그림 (성검 빛 물결) · 높이 (px) · 지나가는 시간")] public Sprite waveSprite;
        public float waveSize = 900, waveTime = 0.55f;
        [Tooltip("일직선 베기 (2026-10-19 기사 v3: 화면을 가르는 한 줄 → 그 선을 따라 폭발): 선 그림 · 각도(도, 0 = 가로) · 길이 · 굵기 (px)")] public Sprite lineSprite;
        public float lineAngle = -55, lineLength = 2600, lineThickness = 90;
        [Tooltip("선을 따라 터지는 폭발 프레임 · 크기 (px) · 마지막 일격 때 선 전체에 한꺼번에 터지는 수")] public Sprite[] boomFrames;
        public float boomSize = 360; public int boomFinishCount = 9;
        [Tooltip("선을 따라 뿜는 파동 (2026-10-19 기사: 화면 전체를 가르며 파동): 굵은 에너지 띠 그림 (선 길이 그대로 두꺼워짐) · 양옆으로 퍼지는 충격파 줄 그림")] public Sprite lineWave, lineRipple;
        [Tooltip("켜면 파동을 한 번만 (2026-10-19 기사: 한 번 딱 내려찍을 때 퍼짐): 게임 화면 단계 시작에 선 · 파동 · 폭발이 가운데서 양끝으로 한 번 퍼지고, 다단 · 마무리 때는 선 위 폭발 · 파동을 더 안 냄")] public bool lineOnce;
        [Tooltip("파동 띠 굵기 (px, 처음 → 끝) · 충격파가 퍼지는 거리 (px) · 시간")] public Vector2 waveThick = new(80, 520); public float rippleDist = 700, rippleTime = 0.6f;
        [Tooltip("게임 화면 단계 동안 화면 안 생명체(고양이 · 보스 · 사람) 꽁꽁, 끝나고도 이 초만큼 더 (0 = 안 함) — 렛잇고 필살기에서 옮겨 옴")] public float freezeAll;
        [Header("게임 화면 돌격 (2026-10-20 파라오: 모래바람 대신 미라 쥐들이 화면을 달려 지나감)")]
        [Tooltip("달리기 프레임 (오른쪽을 보는 옆모습, 순서대로 반복). 있으면 게임 화면 단계 동안 왼→오로 달려 지나감")] public Sprite[] runFrames;
        [Tooltip("몇 초마다 · 한 번에 몇 마리 · 키 (px, 최소~최대) · 속도 (px/초, 최소~최대) · 프레임 시간 (초)")] public float runGap = 0.12f; public int runCount = 3;
        public Vector2 runHeight = new(110, 190), runSpeed = new(1500, 2100); public float runFrame = 0.07f;
        [Tooltip("켜면 게임 화면 단계의 눈보라 · 입자(worldSnow)를 안 띄움")] public bool worldSnowOff;
        [Header("게임 화면 큰 폭발 하나 (2026-10-20 폭탄마: 레이저가 맵에 내리꽂혀 맵 전체를 덮는 폭발 → 버섯구름)")]
        [Tooltip("게임 화면 단계 시작에 위에서 화면 가운데로 내리꽂히는 레이저 그림 · 굵기 (px) · 시간")] public Sprite blastBeam;
        public float blastBeamWidth = 260, blastBeamTime = 0.22f;
        [Tooltip("레이저가 닿는 순간 화면 가운데 큰 폭발 프레임 · 폭 (px) · 프레임 시간")] public Sprite[] blastFrames;
        public float blastSize = 2600, blastFrame = 0.12f;
        [Tooltip("이어서 피어오르는 버섯구름 단계 그림 (바닥 = 화면 아래쪽) · 폭 (px) · 단계 시간 (마지막은 단계 끝까지 남았다 사라짐)")] public Sprite[] mushFrames;
        public float mushSize = 1300, mushFrame = 0.3f;
        [Tooltip("초토화 (2026-10-20 폭탄마): 폭발 순간 화면을 덮는 섬광 · 맵 끝까지 퍼지는 충격파 고리 · 사방으로 날아가는 파편 · 맵을 덮었다 걷히는 연기")] public Sprite blastFlash, blastRing, blastDebris, blastSmoke;
        public int blastDebrisCount = 14; public float blastSmokeSize = 3400;

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
