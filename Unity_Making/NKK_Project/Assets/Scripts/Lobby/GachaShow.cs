using System.Collections;
using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 유전자 뽑기 연출 v2 — 비밀 실험실 컷씬 (사용자 2026-10-12, 믹서기 연출이 잔인하다는 의견으로 교체)
    // ① 로우앵글: 과학 고글 쓴 쥐들이 실험 용기 안(카메라)을 내려다봄 → ② 스포이드 끝 물방울이 맺히고 반짝 →
    // ③ 떨어지는 물방울을 따라 줌 블러 → ④ 펑! 폭발과 함께 실험실의 쥐들이 실험 용기 안으로 빨려 들어감 →
    // ⑤ 용기 아래 배관을 따라 지하로 (가속 + 모션 블러) → ⑥ 하수구 뚜껑 덜컹덜컹 → 열림
    //    (유니크 이상 = 등급 색 빛이 새어 나옴 · 에픽 이하 = 파리가 날림, 10연차는 그 판 최고 등급 기준) →
    // ⑦ 쥐 한 마리씩 강조 폭발과 함께 (클릭/스페이스 = 다음, 건너뛰기 = 바로 결과) → 결과 칸 (확인 · 한 번 더)
    // 글은 씬 TMP (자막 글 그대로, 강조 카드 Count {n}, 결과 칸 Count {n}, 다시 버튼 {n} {cost})
    // 결과 카드 템플릿 자식: Frame(Image) · Grade · Portrait(RatPortrait) · Name · Count({n}) · New · Wish (강조 카드 reveal 도 같은 자식 이름)
    public class GachaShow : MonoBehaviour
    {
        public RatArtLibrary artLibrary;
        [Tooltip("연출 전체 (꺼 둠, 맨 위)")] public GameObject root;
        public CanvasGroup dim;
        [Tooltip("컷 전체를 담는 틀 (흔들림은 여기에)")] public RectTransform stage;

        [Header("① 로우앵글")]
        public RectTransform cutLab;
        public TMP_Text captionLab;
        [Header("② 스포이드 물방울")]
        public RectTransform cutDrop;
        [Tooltip("스포이드 끝 물방울 · 반짝이 (템플릿, 꺼 둠)")] public Image drop, sparkleTemplate;
        [Header("③ 낙하 줌 블러")]
        public RectTransform cutFall;
        [Tooltip("용기 안을 내려다본 그림 (줌 대상) · 떨어지는 물방울")] public Image fallBg, fallDrop;
        [Header("④ 폭발 · 빨려 들어감")]
        public RectTransform cutBench;
        [Tooltip("실험 용기 입구 (쥐가 빨려 드는 곳)")] public RectTransform flaskMouth;
        public Image explosion;
        [Tooltip("빨려 드는 쥐 (RatPortrait 템플릿, 꺼 둠)")] public RatPortrait runnerTemplate;
        public TMP_Text captionSuck;
        [Header("④ 지하도시: 비밀 실험실 폭발 → 돌아다니는 쥐 + 고글 쥐가 전부 빨려 듦 (사용자 2026-10-12)")]
        [Tooltip("비밀 실험실 건물 (지하 Alley 의 SpriteRenderer) · 터진 그림 · 빨려 드는 곳 (문)")] public SpriteRenderer labBuilding;
        public Sprite labBlownSprite;
        public Transform labDoor;
        [Tooltip("월드 폭발 그림 (Alley 자식 SpriteRenderer, 꺼 둠)")] public SpriteRenderer worldBoom;
        [Tooltip("지하 돌아다니는 쥐들 (Alley/AlleyRats)")] public LobbyHome alleyRats;
        [Tooltip("고글 쓴 실험실 쥐 종류 (건물 옆에 서 있다가 같이 빨려 듦) · 고글 템플릿 (Alley 자식 SpriteRenderer, 꺼 둠) · 고글 위치 (머리 그림 폭·높이 비율, 목 기준) · 크기 (머리 폭 배율)")]
        public string[] labRatCodes = { "labrat", "brownrat", "mouse" };
        public SpriteRenderer gogglesTemplate;
        public Vector2 gogglesAt = new(-0.62f, 0.55f);
        public float gogglesSize = 0.55f;
        [Tooltip("고글 쥐가 서는 높이 (건물 바닥에서 위로, 월드 유닛)")] public float labRatLift = 0.3f;
        [Tooltip("월드 장면 동안 숨길 UI (뽑기 페이지 등)")] public GameObject[] hideInWorld;
        [Tooltip("폭발 전 부르르 · 폭발 · 빨려 듦 · 빨려 든 뒤 (초)")] public float worldShakeTime = 0.6f, worldBoomTime = 0.45f, worldSuckTime = 1.3f, worldAfter = 0.5f;
        [Tooltip("월드 폭발 크기 (건물 높이 배율)")] public float worldBoomScale = 1.6f;

        [Header("⑤ 배관 타고 지하로")]
        public RectTransform cutPipe;
        [Tooltip("세로로 긴 배관 그림 (위 → 아래로 내려감)")] public Image pipeImage;
        [Header("⑥ 하수구 뚜껑")]
        public RectTransform cutSewer;
        [Tooltip("뚜껑 (앞모습) · 열린 뚜껑 그림 · 입구 빛 · 빛줄기 · 파리 (템플릿, 꺼 둠)")] public Image lid;
        public Sprite lidClosedSprite, lidOpenSprite;
        public Image holeGlow, rays, flyTemplate;
        [Tooltip("열린 뒤 위로 솟는 빛기둥 (좋은 등급) · 반짝이 (템플릿, 꺼 둠)")] public Image pillar, glitterTemplate;
        [Tooltip("덜컹덜컹 · '눌러서 열기' 안내")] public TMP_Text captionRattle, captionTap;
        [Tooltip("출구로 당겨 올 때 확대 배율")] public float sewerZoom = 1.25f;
        [Header("⑦ 한 마리씩 강조")]
        [Tooltip("강조 묶음 (켜고 끔) · 그 안 카드 (Frame · Grade · Portrait · Name · Count · New · Wish) · 뒤 폭발 · 반짝이 고리")] public GameObject reveal;
        public GameObject revealCard;
        public Image revealBurst, revealRing;
        [Tooltip("공개 순간 번쩍 · 좋은 등급 빛기둥")] public Image revealFlash, revealPillar;
        [Tooltip("공개 폭발 그림: 보통 · 전설(금빛) · 신화(무지개)")] public Sprite burstNormal, burstGold, burstRainbow;
        [Tooltip("'눌러서 다음' 안내 (선택)")] public GameObject revealHint;
        [Tooltip("쥐 고유 컷씬 (전설 이상 — 있으면 실루엣 단계 대신 재생, 없으면 보통 공개). GachaIntro 참고")] public GachaIntro[] intros;
        [Tooltip("고유 컷씬을 쓰는 최소 등급")] public Grade introFrom = Grade.Legendary;
        [Tooltip("건너뛰기 해도 꼭 보여 줄 고유 컷씬: 이 등급 이상 + 처음 획득 (사용자 2026-10-12)")] public Grade mustIntroFrom = Grade.Mythic;

        [Header("결과")]
        [Tooltip("최고 등급 번쩍 (흰 화면)")] public Image flash;
        [Tooltip("결과 칸 템플릿 (꺼 둠) · 결과 줄 (Grid)")] public GameObject cardTemplate;
        public Transform grid;
        [Tooltip("확인 · 한 번 더 (글 {n} {cost}) · 건너뛰기")] public Button ok, again, skip;
        public TMP_Text againText;

        [Header("시간 (초)")]
        public float labTime = 1.3f, dropForm = 0.9f, fallTime = 0.75f, boomTime = 0.35f, suckTime = 0.9f, suckStagger = 0.06f;
        public float pipeTime = 1.1f, rattleTime = 1.2f, openTime = 0.3f, afterOpen = 0.9f;
        [Tooltip("강조: 낮은 등급 · 유니크 이상 자동 넘김")] public float revealLow = 1.0f, revealHigh = 1.8f;
        [Header("움직임")]
        [Tooltip("로우앵글 시작 확대 · 끝 확대")] public float labZoomFrom = 1.18f, labZoomTo = 1.04f;
        [Tooltip("낙하 줌 끝 배율 · 블러 잔상 수 · 잔상 간격(배율)")] public float fallZoom = 2.6f;
        public int blurCopies = 5;
        public float blurStep = 0.06f;
        [Tooltip("배관 모션 블러 잔상 간격 (속도에 곱함) · 잔상 진하기 (속도에 곱함)")] public float pipeBlur = 0.035f, pipeGhostAlpha = 0.45f;
        [Tooltip("배관 속도선 (고유 컷씬 눈보라와 같은 벡터풍, 바람은 스크롤 속도로 정함) · 수 · 속도 배율 (스크롤 속도에 곱함) · 기본 속도 (px/초)")]
        public Blizzard pipeLines = new() { wind = Vector2.zero, color = new Color(1f, 0.92f, 0.78f, 0.55f), streak = 0.0022f, sway = 15 };
        public int speedDots = 45;
        public float speedMul = 0.55f, speedBase = 60;

        [Header("컷 전환 (CutFx: 컷 끝 줌 인 + 하얗게 → 다음 컷 펀치 인)")]
        public CutTransition labCut = new() { pushTime = 0.16f, pushZoom = 1.15f, punchTime = 0.3f, punchFrom = 1.1f };
        public CutTransition dropCut = new() { pushTime = 0.12f, pushZoom = 1.1f, punchTime = 0.25f, punchFrom = 1.12f };
        public CutTransition fallCut = new() { pushTime = 0.14f, pushZoom = 1.12f, punchTime = 0.18f, punchFrom = 1.06f };
        [Tooltip("실험대 컷 (지하 건물이 화면에 없을 때) — 끝만")] public CutTransition benchCut = new() { pushTime = 0.16f, pushZoom = 1.15f };
        [Tooltip("지하 장면 (월드라 배율 없음, 플래시만)")] public CutTransition worldCut = new() { pushTime = 0.18f, pushZoom = 1, punchTime = 0.3f, punchFrom = 1 };
        [Tooltip("배관 — 시작만")] public CutTransition pipeCut = new() { punchTime = 0.3f, punchFrom = 1.12f };
        [Tooltip("고유 컷씬 직전 (하수구가 빨려 듦) — 끝만")] public CutTransition introCut = new() { pushTime = 0.2f, pushZoom = 1.2f };
        [Tooltip("고유 컷씬 직전 효과음 · 컷씬 뒤 공개 번쩍 시간 · 결과 칸 번쩍 시간 · 세기")] public string introSfx = "superjump_charge";
        public float introRevealFlash = 0.45f, resultFlashTime = 0.3f, resultFlashPeak = 0.6f;
        [Tooltip("빨려 드는 쥐 크기 (px) · 빙글 각도")] public float runnerSize = 170, suckSpin = 720;
        [Tooltip("덜컹 횟수 · 각도 · 들썩 (px)")] public int rattles = 3;
        public float rattleAngle = 7, rattleLift = 18;
        [Tooltip("파리 수 · 날갯짓 반경 (px)")] public int flies = 6;
        public float flyRadius = 260;
        [Tooltip("빛을 내는 최소 등급 (이 등급부터 빛, 아래는 파리)")] public Grade glowFrom = Grade.Unique;
        [Tooltip("흔들림 (px)")] public float shake = 18;

        public bool Playing { get; private set; }
        public static bool Busy;          // 연출 중 (로비 ESC 가 탈출 준비실을 닫지 않게)
        public static bool Cutscene;      // 결과 칸 전까지 (뽑기 곡: 결과가 뜨면 페이드 아웃)
        public bool Skipping => skipping; // 고유 컷씬(GachaIntro)이 건너뛰기를 볼 때
        bool skipping, next;
        // 건너뛰기 2단계 (사용자 2026-10-12): 뚜껑 앞까지는 1번째 = 뚜껑 열기 전으로, 그 뒤 = 결과로. 꼭 보여 줄 컷씬 중엔 안 받음
        bool beforeSewer, toSewer, lockSkip;
        readonly HashSet<GeneGacha.Result> introShown = new();
        GachaPage page; int lastN;
        string againTpl, revealCountTpl;
        readonly List<GameObject> spawned = new();

        void Awake()
        {
            if (root) root.SetActive(false);
            foreach (var t in new Component[] { runnerTemplate, sparkleTemplate, flyTemplate }) if (t) t.gameObject.SetActive(false);
            if (cardTemplate) cardTemplate.SetActive(false);
            if (againText) againTpl = againText.text;
            var rc = RevealCard ? RevealCard.transform.Find("Count")?.GetComponent<TMP_Text>() : null; if (rc) revealCountTpl = rc.text;
            if (ok) ok.onClick.AddListener(Close);
            if (again) again.onClick.AddListener(() => { int n = lastN; var pg = page; Close(); if (pg) pg.Pull(n); });
            if (skip) skip.onClick.AddListener(OnSkip);
        }

        public void Play(List<GeneGacha.Result> res, int n, GachaPage from)
        {
            page = from; lastN = n; skipping = false;
            StopAllCoroutines(); Clear();
            StartCoroutine(Run(res));
        }

        // 강제 종료 뒤 복구: 연출 없이 결과만
        public void ShowResultsOnly(List<GeneGacha.Result> res)
        {
            StopAllCoroutines(); Clear();
            if (root) root.SetActive(true);
            Playing = true; skipping = true;
            StartCoroutine(Run(res));
        }

        void OnSkip()
        {
            if (lockSkip || skipping || !Playing) return;
            if (beforeSewer) toSewer = true;
            skipping = true;
        }

        void Clear()
        {
            foreach (var g in spawned) if (g) Destroy(g);
            spawned.Clear();
        }

        void Close()
        {
            StopAllCoroutines(); Clear(); RestoreWorld();
            if (root) root.SetActive(false);
            Playing = Busy = Cutscene = false;
            GeneGacha.ClearPending();
            if (page) page.Render();
        }

        GameObject RevealCard => revealCard ? revealCard : reveal;
        RatCharacterRow Row(string code) { foreach (var r in GameDatabase.Instance.Rats.Values) if (r.code_id == code) return r; return null; }
        static float E(float k) => 1 - (1 - k) * (1 - k);          // 빨리 → 느리게
        static float In(float k) => k * k * k;                    // 느리게 → 빨리 (가속)
        static Color A(Color c, float a) { c.a = a; return c; }

        IEnumerator Wait(float t) { for (float x = 0; x < t && !skipping; x += Time.unscaledDeltaTime) yield return null; }

        void Cut(RectTransform on)
        {
            foreach (var c in new[] { cutLab, cutDrop, cutFall, cutBench, cutPipe, cutSewer }) if (c) c.gameObject.SetActive(c == on);
            if (stage) { stage.anchoredPosition = Vector2.zero; stage.localScale = Vector3.one; }
        }

        void Shake(float amount) { if (stage) stage.anchoredPosition = Random.insideUnitCircle * amount; }

        // 컷 전환 (CutFx, 고유 컷씬과 같은 방식): 끝 = 앞으로 빨려 들며 하얗게 · 시작 = 하얀 화면에서 크게 시작해 자리 잡음
        bool Stopping() => skipping;
        IEnumerator PushOut(CutTransition c) => CutFx.PushOut(stage, flash, c.pushTime, c.pushZoom, Stopping);
        void PunchIn(CutTransition c) => StartCoroutine(CutFx.PunchIn(stage, flash, c.punchTime, c.punchFrom, Stopping));

        IEnumerator Run(List<GeneGacha.Result> res)
        {
            Playing = Busy = true; Cutscene = !skipping;
            if (root) root.SetActive(true);
            foreach (var c in new[] { captionLab, captionSuck, captionRattle }) if (c) c.gameObject.SetActive(false);
            if (ok) ok.gameObject.SetActive(false); if (again) again.gameObject.SetActive(false); if (skip) skip.gameObject.SetActive(!skipping);
            if (flash) flash.gameObject.SetActive(false);
            if (reveal) reveal.SetActive(false);
            if (grid) grid.gameObject.SetActive(false);
            if (dim) dim.alpha = 1;
            res.Sort((a, b) => a.grade.CompareTo(b.grade));          // 높은 등급은 마지막에
            var best = res.Count > 0 ? res[res.Count - 1].grade : Grade.Common;

            bool recover = skipping;                                   // 강제 종료 뒤 복구 (ShowResultsOnly) = 연출 없이
            introShown.Clear(); toSewer = lockSkip = false;
            if (!skipping)
            {
                beforeSewer = true;
                yield return Lab();
                if (!skipping) yield return Drop();
                if (!skipping) yield return Fall();
                if (!skipping) yield return World(res);
                if (!skipping) yield return Pipe();
                beforeSewer = false;
                if (toSewer) { toSewer = false; skipping = false; JumpToSewer(); }   // 1번째 건너뛰기 = 뚜껑 열기 전으로
                if (!skipping) yield return Sewer(best);
                if (!skipping) yield return Reveal(res);
            }
            beforeSewer = false;
            // 건너뛰어도 처음 얻은 신화(이상)의 고유 컷씬은 보여 줌
            bool forced = false;
            if (!recover)
                foreach (var r in res)
                {
                    var intro = Intro(r);
                    if (!intro || !Must(r) || introShown.Contains(r)) continue;
                    if (!forced) { Cut(null); RestoreWorld(); if (reveal) reveal.SetActive(false); if (dim) dim.alpha = 1; }
                    forced = true;
                    yield return PlayIntro(intro, r);
                    CutFx.Alpha(flash, 1);                              // 컷씬 끝 하얀 화면 유지 (다음 컷씬 · 결과 번쩍으로)
                }
            Cut(null); RestoreWorld(); Cutscene = false;
            if (reveal) reveal.SetActive(false);
            if (flash) flash.gameObject.SetActive(false);
            if (forced && flash) StartCoroutine(Flash(introRevealFlash, 1));
            else if (!skipping && flash) StartCoroutine(Flash(resultFlashTime, resultFlashPeak));
            yield return Results(res);
        }

        // ① 로우앵글 — 고글 쥐들이 내려다봄 (천천히 당겨 오며 살짝 흔들)
        IEnumerator Lab()
        {
            Cut(cutLab); SfxManager.Play("story_page"); PunchIn(labCut);
            if (captionLab) captionLab.gameObject.SetActive(true);
            for (float x = 0; x < labTime && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = x / labTime;
                if (cutLab) cutLab.localScale = Vector3.one * Mathf.Lerp(labZoomFrom, labZoomTo, E(k));
                if (captionLab) captionLab.transform.localScale = Vector3.one * (1 + 0.06f * Mathf.Sin(Time.unscaledTime * 9));
                yield return null;
            }
            if (captionLab) captionLab.gameObject.SetActive(false);
            yield return PushOut(labCut);
        }

        // ② 스포이드 끝 물방울이 맺히며 반짝 → 똑
        IEnumerator Drop()
        {
            Cut(cutDrop); PunchIn(dropCut);
            var home = drop ? drop.rectTransform.anchoredPosition : Vector2.zero;
            if (drop) { drop.rectTransform.localScale = Vector3.one * 0.2f; drop.rectTransform.anchoredPosition = home; }
            bool sparkled = false;
            for (float x = 0; x < dropForm && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = x / dropForm;
                if (drop) { float s = Mathf.Lerp(0.2f, 1f, E(Mathf.Min(1, k * 1.4f))); drop.rectTransform.localScale = new Vector3(s, s * (1 + 0.08f * Mathf.Sin(x * 20) * (1 - k)), 1); }
                if (!sparkled && k > 0.55f) { sparkled = true; SfxManager.Play("tuto_highlight"); for (int i = 0; i < 4; i++) StartCoroutine(Sparkle(i)); }
                yield return null;
            }
            yield return Wait(0.25f);
            // 똑 — 아래로 떨어져 나감
            for (float x = 0; x < 0.25f && !skipping; x += Time.unscaledDeltaTime)
            {
                if (drop) drop.rectTransform.anchoredPosition = home + new Vector2(0, -900 * In(x / 0.25f));
                yield return null;
            }
            yield return PushOut(dropCut);
            if (drop) drop.rectTransform.anchoredPosition = home;
        }

        // 물방울 위 반짝이: 퐁 커졌다 돌며 사라짐
        IEnumerator Sparkle(int i)
        {
            if (!sparkleTemplate || !drop) yield break;
            var s = Instantiate(sparkleTemplate, drop.transform.parent); s.gameObject.SetActive(true); spawned.Add(s.gameObject);
            var rt = s.rectTransform; var c = s.color;
            Vector2 off = i == 0 ? new Vector2(-0.18f, 0.22f) : (Random.insideUnitCircle * 0.45f);
            var dr = drop.rectTransform;      // 물방울 가운데 (기준점이 꼭지(위)여도)
            Vector2 mid = dr.anchoredPosition + new Vector2((0.5f - dr.pivot.x) * dr.rect.width, (0.5f - dr.pivot.y) * dr.rect.height);
            rt.anchoredPosition = mid + off * dr.rect.width;
            yield return Wait(i * 0.09f);
            float life = 0.55f, size = i == 0 ? 1.2f : Random.Range(0.45f, 0.8f);
            for (float x = 0; x < life && rt; x += Time.unscaledDeltaTime)
            {
                float k = x / life, sc = size * (k < 0.35f ? E(k / 0.35f) : 1 - (k - 0.35f) / 0.65f);
                rt.localScale = Vector3.one * sc; rt.localRotation = Quaternion.Euler(0, 0, k * 120);
                yield return null;
            }
            if (rt) rt.gameObject.SetActive(false);
        }

        // ③ 떨어지는 물방울을 따라 용기 속으로 — 배경이 가속하며 확대 + 잔상(줌 블러), 물방울은 작아지며 가운데로
        IEnumerator Fall()
        {
            Cut(cutFall); SfxManager.Play("rat_dash"); PunchIn(fallCut);
            var ghosts = Ghosts(fallBg, blurCopies);
            for (float x = 0; x < fallTime && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = x / fallTime, z = Mathf.Lerp(1, fallZoom, In(k)), speed = 3 * k * k;   // 줌 속도에 비례한 잔상
                if (fallBg) fallBg.rectTransform.localScale = Vector3.one * z;
                for (int i = 0; i < ghosts.Count; i++)
                {
                    ghosts[i].rectTransform.localScale = Vector3.one * z * (1 + blurStep * (i + 1) * speed);
                    ghosts[i].color = A(fallBg.color, 0.35f * speed / (i + 1.5f));
                }
                if (fallDrop) { fallDrop.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 0.25f, E(k)); fallDrop.rectTransform.localRotation = Quaternion.identity; }
                yield return null;
            }
            yield return PushOut(fallCut);
            foreach (var g in ghosts) if (g) g.gameObject.SetActive(false);
            if (fallBg) fallBg.rectTransform.localScale = Vector3.one;
        }

        // 그림 뒤에 반투명 잔상 사본 (줌·모션 블러용)
        List<Image> Ghosts(Image src, int n)
        {
            var l = new List<Image>();
            if (!src) return l;
            for (int i = 0; i < n; i++)
            {
                var g = Instantiate(src, src.transform.parent); spawned.Add(g.gameObject);
                foreach (Transform ch in g.transform) Destroy(ch.gameObject);
                g.raycastTarget = false; g.color = A(src.color, 0);
                g.transform.SetSiblingIndex(src.transform.GetSiblingIndex() + 1);
                l.Add(g);
            }
            return l;
        }

        // ④ 펑! — 흰 번쩍 + 폭발 + 흔들림, 뽑힌 쥐들이 사방에서 빙글 돌며 용기 입구로 빨려 듦
        IEnumerator Boom(List<GeneGacha.Result> res)
        {
            Cut(cutBench);
            if (flash) StartCoroutine(Flash(0.35f, 1));
            SfxManager.Play("wall_blast");
            if (explosion) explosion.gameObject.SetActive(true);
            for (float x = 0; x < boomTime && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = x / boomTime;
                if (explosion) { explosion.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.3f, E(k)); explosion.color = A(explosion.color, 1); }
                Shake(shake * (1 - k));
                yield return null;
            }
            if (captionSuck) captionSuck.gameObject.SetActive(true);
            SfxManager.Play("superjump_charge");
            for (int i = 0; i < res.Count && !skipping; i++) { StartCoroutine(Suck(res[i], i, res.Count)); yield return Wait(suckStagger); }
            for (float x = 0; x < suckTime && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = x / suckTime;
                if (explosion) { explosion.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.2f, In(k)); explosion.color = A(explosion.color, 1 - k); explosion.rectTransform.localRotation = Quaternion.Euler(0, 0, -k * 200); }
                Shake(shake * 0.25f);
                yield return null;
            }
            if (explosion) explosion.gameObject.SetActive(false);
            if (captionSuck) captionSuck.gameObject.SetActive(false);
            if (stage) stage.anchoredPosition = Vector2.zero;
            SfxManager.Play("spill");
            yield return PushOut(benchCut);
        }

        // 쥐 한 마리: 방 여기저기에서 소용돌이를 그리며 용기 입구로 (작아지며 빙글)
        IEnumerator Suck(GeneGacha.Result r, int i, int n)
        {
            if (!runnerTemplate || !flaskMouth) yield break;
            var row = Row(r.code);
            var rp = Instantiate(runnerTemplate, runnerTemplate.transform.parent); rp.gameObject.SetActive(true); spawned.Add(rp.gameObject);
            var rt = (RectTransform)rp.transform; rt.sizeDelta = new Vector2(runnerSize, runnerSize * 0.7f);
            float ang = (i / (float)Mathf.Max(1, n)) * Mathf.PI * 2 + Random.Range(-0.3f, 0.3f);
            rp.faceRight = Mathf.Cos(ang) < 0;
            if (row != null && artLibrary) rp.Show(artLibrary.Get(row.code_id));
            var parent = (RectTransform)rt.parent;
            Vector2 mouth = parent.InverseTransformPoint(flaskMouth.position);
            float r0 = Random.Range(520f, 760f);
            for (float x = 0; x < suckTime; x += Time.unscaledDeltaTime)
            {
                float k = x / suckTime, kk = In(k), a = ang + kk * 4.5f, rad = r0 * (1 - kk);
                rt.anchoredPosition = mouth + new Vector2(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad * 0.55f);
                rt.localRotation = Quaternion.Euler(0, 0, kk * suckSpin * (rp.faceRight ? -1 : 1));
                rt.localScale = Vector3.one * Mathf.Lerp(1, 0.1f, kk);
                if (skipping) break;
                yield return null;
            }
            if (squeakN++ % 3 == 0) SfxManager.Play(Random.value < 0.5f ? "rat_squeak_1" : "rat_squeak_2");
            rp.gameObject.SetActive(false);
        }
        int squeakN;

        // ④ 지하도시 화면으로 돌아와: 비밀 실험실이 부르르 → 펑! → 돌아다니던 쥐들 + 고글 쥐들이 빙글빙글 건물 문으로 빨려 듦.
        // 건물이 화면에 안 보이면 (다른 곳에서 열었을 때) 실험대 컷(Boom)으로 대신함. 쥐들은 연출이 끝나면 (RestoreWorld) 다시 돌아다님
        readonly List<Transform> victims = new();
        readonly List<GameObject> worldSpawned = new();
        readonly List<GameObject> hidden = new();
        Sprite labNormal; bool worldOn; Vector3? labHome;
        float buildingScale = -1;
        float BuildingScale { get { if (buildingScale < 0 && labBuilding) buildingScale = labBuilding.transform.localScale.x; return buildingScale; } }

        bool OnScreen(Vector3 w) { var c = Camera.main; if (!c) return false; var v = c.WorldToViewportPoint(w); return v.x > 0.05f && v.x < 0.95f && v.y > 0.05f && v.y < 0.95f; }

        IEnumerator World(List<GeneGacha.Result> res)
        {
            if (!labBuilding || !alleyRats || !labBuilding.gameObject.activeInHierarchy || !OnScreen(labBuilding.bounds.center)) { yield return Boom(res); yield break; }
            worldOn = true;
            Cut(null);
            if (dim) dim.alpha = 0;
            foreach (var g in hideInWorld) if (g && g.activeSelf) { g.SetActive(false); hidden.Add(g); }
            StartCoroutine(CutFx.PunchIn(null, flash, worldCut.punchTime, worldCut.punchFrom, Stopping));
            alleyRats.suspended = true;
            victims.Clear();
            foreach (var a in alleyRats.actors) if (a && a.gameObject.activeInHierarchy) victims.Add(a.transform);
            SpawnLabRats();
            labNormal = labBuilding.sprite;
            var bt = labBuilding.transform; Vector3 home = bt.localPosition; float bs = BuildingScale; labHome = home;
            Vector3 door = labDoor ? labDoor.position : labBuilding.bounds.center;

            // 부르르 (점점 세게)
            SfxManager.Play("superjump_charge");
            for (float x = 0; x < worldShakeTime && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = x / worldShakeTime, w = Mathf.Sin(x * 60) * 0.03f * k;
                bt.localPosition = home + (Vector3)(Random.insideUnitCircle * 0.04f * (0.3f + k));
                bt.localScale = new Vector3(bs * (1 + w), bs * (1 - w), 1);
                yield return null;
            }
            bt.localPosition = home; bt.localScale = new Vector3(bs, bs, 1);

            // 펑!
            if (labBlownSprite) { float y0 = labBuilding.bounds.min.y; labBuilding.sprite = labBlownSprite; home += new Vector3(0, (y0 - labBuilding.bounds.min.y) / Mathf.Max(1e-4f, bt.lossyScale.y) * bt.localScale.y, 0); bt.localPosition = home; }   // 바닥선 그대로
            if (flash) StartCoroutine(Flash(0.35f, 1));
            SfxManager.Play("wall_blast");
            float boomH = labBuilding.bounds.size.y * worldBoomScale, sprH = worldBoom && worldBoom.sprite ? worldBoom.sprite.bounds.size.y : 1;
            if (worldBoom) { worldBoom.gameObject.SetActive(true); worldBoom.transform.position = labBuilding.bounds.center; }
            for (float x = 0; x < worldBoomTime && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = x / worldBoomTime;
                if (worldBoom) { worldBoom.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1, E(k)) * boomH / sprH; worldBoom.color = Color.white; }
                bt.localPosition = home + (Vector3)(Random.insideUnitCircle * 0.08f * (1 - k));
                yield return null;
            }
            bt.localPosition = home;

            // 빨려 듦 (하나씩 조금씩 늦게)
            if (captionSuck) captionSuck.gameObject.SetActive(true);
            SfxManager.Play("rat_rush_call");
            var starts = new List<Vector3>(); foreach (var v in victims) starts.Add(v ? v.position : door);
            float total = worldSuckTime + victims.Count * suckStagger;
            for (float x = 0; x < total && !skipping; x += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < victims.Count; i++)
                {
                    var v = victims[i]; if (!v) continue;
                    float k = Mathf.Clamp01((x - i * suckStagger) / worldSuckTime);
                    if (k <= 0) continue;
                    if (k >= 1)
                    {
                        if (v.gameObject.activeSelf) { v.gameObject.SetActive(false); if (squeakN++ % 2 == 0) SfxManager.Play(Random.value < 0.5f ? "rat_squeak_1" : "rat_squeak_2"); }
                        continue;
                    }
                    float kk = In(k), a0 = Mathf.Atan2(starts[i].y - door.y, starts[i].x - door.x), r0 = Vector2.Distance(starts[i], door);
                    float a = a0 + kk * 5f, rad = r0 * (1 - kk);
                    v.position = door + new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad * 0.7f, 0);
                    v.localScale = Vector3.one * Mathf.Lerp(1, 0.1f, kk);
                    v.localRotation = Quaternion.Euler(0, 0, kk * 900 * (i % 2 == 0 ? 1 : -1));
                }
                if (worldBoom)
                {
                    float k = Mathf.Clamp01(x / total);
                    worldBoom.transform.localScale = Vector3.one * Mathf.Lerp(1, 0.3f, In(k)) * boomH / sprH;
                    worldBoom.transform.localRotation = Quaternion.Euler(0, 0, -k * 240); worldBoom.color = A(Color.white, 1 - k);
                }
                yield return null;
            }
            foreach (var v in victims) if (v) v.gameObject.SetActive(false);
            if (worldBoom) worldBoom.gameObject.SetActive(false);
            if (captionSuck) captionSuck.gameObject.SetActive(false);
            yield return Wait(worldAfter);
            SfxManager.Play("spill");
            yield return CutFx.PushOut(null, flash, worldCut.pushTime, worldCut.pushZoom, Stopping);
            // 다음 컷(배관)부터는 다시 막 위에서
            if (dim) dim.alpha = 1;
            foreach (var g in hidden) if (g) g.SetActive(true);
            hidden.Clear();
        }

        // 고글 쓴 실험실 쥐들: 지하 쥐를 복제해 종을 바꾸고 건물 양옆에 세움 (컷씬 첫 장면의 그 쥐들)
        void SpawnLabRats()
        {
            if (alleyRats.actors.Count == 0 || !artLibrary) return;
            var src = alleyRats.actors[0];
            var c = labBuilding.bounds.center; float w = labBuilding.bounds.size.x, floorY = labBuilding.bounds.min.y;
            for (int i = 0; i < labRatCodes.Length; i++)
            {
                var row = Row(labRatCodes[i]); var art = row != null ? artLibrary.Get(row.code_id) : null;
                if (art == null) continue;
                var go = Instantiate(src.gameObject, src.transform.parent); worldSpawned.Add(go);
                go.SetActive(true);
                var lr = go.GetComponent<LobbyRat>();
                lr.Init(alleyRats, row, art, alleyRats.rigLength);
                float side = i % 2 == 0 ? -1 : 1, off = w * (0.62f + 0.25f * (i / 2));
                var uv = alleyRats.ToUV(new Vector3(c.x + side * off, floorY + labRatLift, 0));
                lr.u = uv.x; lr.v = uv.y; lr.z = 0; lr.face = side < 0 ? 1 : -1; lr.state = LobbyRat.State.Idle;
                lr.Draw(0, 1000 + Mathf.RoundToInt(lr.v * 2000));
                var goggles = gogglesTemplate ? gogglesTemplate.sprite : null;
                if (goggles && lr.rig && lr.rig.head && lr.rig.head.sprite)
                {
                    // 머리 그림(왼쪽을 봄, 피벗 = 목) 기준: 눈 근처에 고글
                    var hs = lr.rig.head.sprite; float hw = hs.rect.width / 100f, hh = hs.rect.height / 100f;
                    var pv = hs.pivot / 100f;
                    var sr = Instantiate(gogglesTemplate, lr.rig.head.transform, false); var g = sr.gameObject;
                    g.SetActive(true); sr.sortingOrder = lr.rig.head.sortingOrder + 1;
                    g.transform.localScale = Vector3.one * (hw * gogglesSize / Mathf.Max(0.01f, goggles.bounds.size.x));
                    g.transform.localPosition = new Vector3(hw * 0.5f - pv.x + gogglesAt.x * hw * 0.5f, gogglesAt.y * hh - pv.y, 0);
                }
                victims.Add(go.transform);
            }
        }

        // 연출이 끝나면 (또는 건너뛰면) 지하 원래대로: 쥐들 다시 보이고 걷기 재개, 건물 그림 되돌림, 고글 쥐 지움, 숨긴 UI 다시
        void RestoreWorld()
        {
            if (!worldOn) return;
            worldOn = false;
            foreach (var v in victims) if (v) { v.gameObject.SetActive(true); v.localScale = Vector3.one; v.localRotation = Quaternion.identity; }
            victims.Clear();
            foreach (var g in worldSpawned) if (g) Destroy(g);
            worldSpawned.Clear();
            if (labBuilding && labNormal) { labBuilding.sprite = labNormal; if (labHome.HasValue) labBuilding.transform.localPosition = labHome.Value; }
            if (labBuilding && buildingScale > 0) labBuilding.transform.localScale = new Vector3(buildingScale, buildingScale, 1);
            if (worldBoom) worldBoom.gameObject.SetActive(false);
            if (alleyRats) alleyRats.suspended = false;
            foreach (var g in hidden) if (g) g.SetActive(true);
            hidden.Clear();
            if (dim) dim.alpha = 1;
        }

        // ⑤ 배관 따라 지하로 — 실험실 바닥부터 하수구 출구까지 이어진 세로로 긴 한 장을 위 → 아래로 (가속 → 감속) + 세로 잔상.
        // 맨 아래 출구 뚜껑(lid 는 이 그림의 자식)에서 그대로 ⑥ 으로 이어짐 (사용자 2026-10-12: 컷이 따로 놀아 연결이 안 됨)
        IEnumerator Pipe()
        {
            Cut(cutPipe); SfxManager.Play("human_fly"); PunchIn(pipeCut);
            if (!pipeImage) yield break;
            var rt = pipeImage.rectTransform;
            rt.localScale = Vector3.one;
            float view = cutPipe.rect.height, travel = Mathf.Max(0, rt.rect.height - view);
            Vector2 top = new(0, -travel * 0.5f), bottom = new(0, travel * 0.5f);
            rt.anchoredPosition = top;
            var ghosts = Ghosts(pipeImage, blurCopies);
            // 속도선: 내려가는 동안 위로 스쳐 감 (빠를수록 길게)
            var lines = pipeLines;
            if (!lines.layer) lines.layer = cutPipe;
            lines.wind = Vector2.zero; lines.Add(speedDots);
            yield return Wait(0.25f);
            float prev = 0;
            for (float x = 0; x < pipeTime && !skipping; x += Time.unscaledDeltaTime)
            {
                float t = x / pipeTime, k = t * t * t * (t * (6 * t - 15) + 10);      // 천천히 → 빠르게 → 천천히 (smootherstep)
                float v = (k - prev) / Mathf.Max(1e-4f, Time.unscaledDeltaTime); prev = k;
                var p = Vector2.Lerp(top, bottom, k); rt.anchoredPosition = p;
                for (int i = 0; i < ghosts.Count; i++)
                {
                    ghosts[i].rectTransform.anchoredPosition = p - new Vector2(0, travel * v * pipeBlur * (i + 1));
                    ghosts[i].color = A(pipeImage.color, Mathf.Clamp01(v * pipeGhostAlpha) / (i + 1.5f));
                }
                lines.wind = new Vector2(0, travel * v * speedMul + speedBase);
                lines.Tick(Time.unscaledDeltaTime);
                yield return null;
            }
            lines.Clear();
            foreach (var g in ghosts) if (g) g.gameObject.SetActive(false);
            rt.anchoredPosition = bottom;
            SfxManager.Play("rat_land");
        }

        // ⑥ 하수구 뚜껑: 출구 쪽으로 살짝 당겨 옴 → 덜컹덜컹 (좋은 등급이면 틈으로 등급 색 빛이 샘) →
        // 눌러야 열림 (사용자: 덜컹덜컹 뒤 클릭해야 빛 나오게) → 좋은 등급 = 빛기둥 · 빛 폭발 · 반짝이, 아니면 파리
        IEnumerator Sewer(Grade best)
        {
            if (cutSewer && cutSewer != cutPipe) Cut(cutSewer);
            bool glow = best >= glowFrom;
            var col = GlowColor(best);
            var lidRt = lid ? lid.rectTransform : null;
            if (lidRt && !lidHomeSet) { lidHome0 = lidRt.anchoredPosition; lidHomeSet = true; }
            Vector2 lidHome = lidHome0;
            if (lidSize == Vector2.zero && lidRt) lidSize = lidRt.sizeDelta;          // 씬에서 정한 닫힌 뚜껑 크기
            if (lid) { if (lidClosedSprite) lid.sprite = lidClosedSprite; lidRt.sizeDelta = lidSize; lidRt.localScale = Vector3.one; lidRt.localRotation = Quaternion.identity; lidRt.anchoredPosition = lidHome; }
            foreach (var g in new[] { holeGlow, rays, pillar }) if (g) { g.gameObject.SetActive(glow); g.color = A(col, 0); }

            // 출구로 당겨 옴 (긴 그림 안의 뚜껑이 화면 가운데 조금 아래로)
            if (pipeImage && lidRt && lidRt.parent == pipeImage.rectTransform)
            {
                var rt = pipeImage.rectTransform; Vector2 p0 = rt.anchoredPosition;
                for (float x = 0; x < 0.5f && !skipping; x += Time.unscaledDeltaTime)
                {
                    float k = E(x / 0.5f), s = Mathf.Lerp(1, sewerZoom, k);
                    rt.localScale = Vector3.one * s;
                    rt.anchoredPosition = Vector2.Lerp(p0, new Vector2(0, -60) - lidHome * sewerZoom, k);
                    yield return null;
                }
            }

            // 덜컹 × rattles (점점 세게, 좋은 등급이면 틈 빛이 점점 밝아짐)
            if (captionRattle) captionRattle.gameObject.SetActive(true);
            float each = rattleTime / Mathf.Max(1, rattles);
            for (int b = 0; b < rattles && !skipping; b++)
            {
                SfxManager.Play(b % 2 == 0 ? "hit_metal" : "smash_metal");
                float power = 0.6f + 0.4f * b / Mathf.Max(1, rattles - 1);
                for (float x = 0; x < each && !skipping; x += Time.unscaledDeltaTime)
                {
                    float k = x / each, hop = Mathf.Sin(Mathf.Min(1, k * 2.5f) * Mathf.PI);
                    Rattle(lidRt, lidHome, hop * power, b);
                    if (captionRattle) captionRattle.transform.localScale = Vector3.one * (1 + 0.15f * hop);
                    if (glow) Leak(col, Mathf.Lerp(0.2f, 0.6f, (b + k) / rattles) * (0.6f + 0.4f * hop), x);
                    Shake(shake * 0.3f * hop * power);
                    yield return null;
                }
            }
            if (captionRattle) captionRattle.gameObject.SetActive(false);

            // 눌러서 열기: 그동안 가끔 들썩 + (좋은 등급) 틈 빛이 숨쉬듯 깜빡
            if (captionTap) captionTap.gameObject.SetActive(true);
            tapped = false; waitTap = true;
            for (float x = 0; !skipping && !tapped; x += Time.unscaledDeltaTime)
            {
                float ph = x % 1.1f, hop = ph < 0.25f ? Mathf.Sin(ph / 0.25f * Mathf.PI) * 0.45f : 0;
                if (ph < Time.unscaledDeltaTime && x > 0.1f) SfxManager.Play("hit_metal");
                Rattle(lidRt, lidHome, hop, (int)(x / 1.1f));
                if (glow) Leak(col, 0.55f + 0.2f * Mathf.Sin(x * 5), x);
                if (captionTap) captionTap.transform.localScale = Vector3.one * (1 + 0.08f * Mathf.Sin(x * 6));
                yield return null;
            }
            waitTap = false;
            if (captionTap) captionTap.gameObject.SetActive(false);
            if (lidRt) { lidRt.anchoredPosition = lidHome; lidRt.localRotation = Quaternion.identity; }

            // 열림 — 오른쪽 경첩으로 휙 (가로로 찌그러지며 → 열린 뚜껑 그림)
            SfxManager.Play(glow ? "jgl_unlock" : "smash_metal");
            if (glow && flash) StartCoroutine(Flash(0.45f, best >= Grade.Legendary ? 0.95f : 0.6f, col));
            Shake(glow ? shake : shake * 0.5f);
            float w = lidRt ? lidRt.rect.width : 0;
            for (float x = 0; x < openTime && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = E(x / openTime);
                if (lidRt) { lidRt.localScale = new Vector3(Mathf.Lerp(1, 0.2f, k), 1, 1); lidRt.anchoredPosition = lidHome + new Vector2(w * 0.5f * (1 - lidRt.localScale.x), 0); }
                if (glow) Burst(col, k, 0);
                yield return null;
            }
            if (lid && lidOpenSprite)
            {
                // 열린 뚜껑(옆으로 선 모습): 닫힌 그림과 같은 배율, 경첩(오른쪽) 바깥에 섬
                var cs = lidClosedSprite ? lidClosedSprite.rect.size : lidOpenSprite.rect.size;
                float k = lidSize.y / Mathf.Max(1, cs.y);
                lid.sprite = lidOpenSprite; lidRt.sizeDelta = lidOpenSprite.rect.size * k; lidRt.localScale = Vector3.one;
                lidRt.anchoredPosition = lidHome + new Vector2(w * 0.5f + lidRt.sizeDelta.x * 0.3f, 0);
            }
            if (glow) for (int i = 0; i < (best >= Grade.Legendary ? 24 : 14); i++) Spark(lid ? (RectTransform)lid.transform.parent : null, lidHome, col, 500);
            else { SfxManager.Play("rat_gnaw"); for (int i = 0; i < flies; i++) StartCoroutine(Fly(i)); }

            for (float x = 0; x < afterOpen && !skipping; x += Time.unscaledDeltaTime)
            {
                if (glow) Burst(col, 1, x);
                yield return null;
            }
            // 열린 뚜껑·빛은 강조 장면 뒤에 그대로 둠 (다음 연출 시작 때 닫힌 그림으로 되돌림)
        }
        Vector2 lidSize, lidHome0; bool lidHomeSet;
        bool tapped, waitTap;
        float cardBase;

        // 최고 등급 빛 색: 전설 = 금빛, 신화 = 무지개(흰빛 + 무지개 폭발 그림), 그 외 = 등급 색을 밝게
        Color GlowColor(Grade g) => g >= Grade.Mythic ? Color.white : g >= Grade.Legendary ? new Color(1f, 0.85f, 0.45f) : Color.Lerp(MarketPage.GradeColor((int)g), Color.white, 0.25f);
        Sprite BurstSprite(Grade g) => g >= Grade.Mythic && burstRainbow ? burstRainbow : g >= Grade.Legendary && burstGold ? burstGold : burstNormal;

        void Rattle(RectTransform lidRt, Vector2 home, float hop, int b)
        {
            if (!lidRt) return;
            lidRt.anchoredPosition = home + new Vector2(Random.Range(-3f, 3f) * hop, hop * rattleLift);
            lidRt.localRotation = Quaternion.Euler(0, 0, hop * rattleAngle * (b % 2 == 0 ? 1 : -1));
        }

        // 닫힌 뚜껑 틈으로 새는 빛 (뒤 입구 빛 + 위로 퍼지는 빛줄기)
        void Leak(Color col, float a, float t)
        {
            if (holeGlow) { holeGlow.color = A(col, a); holeGlow.rectTransform.localScale = Vector3.one * (0.9f + 0.1f * a); }
            if (rays) { rays.color = A(col, a * 0.8f); rays.rectTransform.localScale = Vector3.one * (0.55f + 0.25f * a); rays.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 3) * 4); }
        }

        // 열린 뒤: 입구 빛 가득 + 빛줄기 크게 + 빛기둥 (k = 열리는 정도, t = 열린 뒤 시간)
        void Burst(Color col, float k, float t)
        {
            if (holeGlow) { holeGlow.color = A(col, Mathf.Lerp(0.6f, 1, k)); holeGlow.rectTransform.localScale = Vector3.one * (1.1f + 0.06f * Mathf.Sin(t * 8)); }
            if (rays) { rays.color = A(col, k); rays.rectTransform.localScale = Vector3.one * (Mathf.Lerp(0.8f, 1.5f, k) + 0.05f * Mathf.Sin(t * 6)); rays.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.5f) * 5); }
            if (pillar) { pillar.color = A(col, k * (0.85f + 0.15f * Mathf.Sin(t * 10))); pillar.rectTransform.localScale = new Vector3(Mathf.Lerp(0.3f, 1, k), Mathf.Lerp(0.2f, 1, E(k)), 1); }
        }

        // 반짝이 하나: at 에서 튀어나와 퍼지며 사라짐 (그림 = glitterTemplate/sparkleTemplate)
        void Spark(RectTransform parent, Vector2 at, Color col, float speed)
        {
            var src = glitterTemplate ? glitterTemplate : sparkleTemplate;
            if (!src || !parent) return;
            var s = Instantiate(src, parent); s.gameObject.SetActive(true); spawned.Add(s.gameObject);
            s.color = Color.Lerp(col, Color.white, 0.5f);
            StartCoroutine(SparkFly(s.rectTransform, at, Random.insideUnitCircle.normalized * speed * Random.Range(0.4f, 1f), Random.Range(0.25f, 0.6f)));
        }
        IEnumerator SparkFly(RectTransform rt, Vector2 at, Vector2 v, float size)
        {
            float life = Random.Range(0.6f, 1.1f), spin = Random.Range(-200f, 200f);
            rt.anchoredPosition = at;
            for (float x = 0; x < life && rt; x += Time.unscaledDeltaTime)
            {
                float k = x / life;
                v *= 1 - 2.5f * Time.unscaledDeltaTime; v.y -= 120 * Time.unscaledDeltaTime;
                rt.anchoredPosition += v * Time.unscaledDeltaTime;
                rt.localScale = Vector3.one * size * (k < 0.2f ? k / 0.2f : 1 - (k - 0.2f) / 0.8f);
                rt.localRotation = Quaternion.Euler(0, 0, x * spin);
                yield return null;
            }
            if (rt) rt.gameObject.SetActive(false);
        }

        // 파리: 입구에서 나와 8자로 윙윙
        IEnumerator Fly(int i)
        {
            if (!flyTemplate || !lid) yield break;
            var f = Instantiate(flyTemplate, flyTemplate.transform.parent); f.gameObject.SetActive(true); spawned.Add(f.gameObject);
            var rt = f.rectTransform; Vector2 c = lid.rectTransform.anchoredPosition;
            float ph = Random.value * 10, sp = Random.Range(3f, 5f), r = flyRadius * Random.Range(0.5f, 1f), life = afterOpen + 4;
            for (float x = 0; x < life && rt; x += Time.unscaledDeltaTime)
            {
                float k = Mathf.Min(1, x / 0.4f), t = ph + x * sp;
                var p = c + new Vector2(Mathf.Sin(t) * r, Mathf.Sin(t * 2) * r * 0.45f + 40 * x) * k;
                var d = p - rt.anchoredPosition;
                rt.anchoredPosition = p;
                if (d.sqrMagnitude > 0.01f) rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90);
                rt.localScale = Vector3.one * k * (0.9f + 0.1f * Mathf.Sin(x * 60));
                yield return null;
            }
            if (rt) rt.gameObject.SetActive(false);
        }

        // ⑦ 쥐 한 마리씩 (가챠 공개 연출): 검은 실루엣 + 모이는 빛 (좋은 등급은 빛기둥·더 길게) → 번쩍 → 색이 돌아오며 등급 폭발(전설 금빛 · 신화 무지개)
        // · 충격파 고리 · 반짝이 튐 → 이름·등급 표시. 클릭/스페이스 = 다음, 건너뛰기 = 결과
        IEnumerator Reveal(List<GeneGacha.Result> res)
        {
            // 하수구 장면(열린 뚜껑)을 뒤에 둔 채 — 쥐가 하수구에서 한 마리씩 튀어나오는 느낌
            var sewerOn = (cutSewer && cutSewer.gameObject.activeSelf) || (cutPipe && cutPipe.gameObject.activeSelf);
            if (!sewerOn) Cut(null);
            if (!reveal) yield break;
            reveal.SetActive(true);
            if (revealHint) revealHint.SetActive(false);
            var card = RevealCard.transform; var rr = (RectTransform)reveal.transform;
            if (cardBase <= 0) cardBase = card.localScale.x;          // 씬에서 정한 카드 크기 (애니메이션은 여기에 곱함)
            var por = card.Find("Portrait")?.GetComponent<RatPortrait>();
            var texts = new List<GameObject>();
            foreach (var n in new[] { "Grade", "Name", "Count", "New", "Wish", "Frame" }) { var t = card.Find(n); if (t) texts.Add(t.gameObject); }
            foreach (var r in res)
            {
                if (skipping) break;
                int gi = (int)r.grade; var col = GlowColor(r.grade);
                bool high = r.grade >= glowFrom, top = r.grade >= Grade.Legendary;
                var row = Row(r.code); var art = row != null && artLibrary ? artLibrary.Get(row.code_id) : null;
                Fill(RevealCard, r, revealCountTpl);
                var shown = new List<GameObject>(); foreach (var g in texts) if (g.activeSelf) { shown.Add(g); g.SetActive(false); }
                if (por && art != null) por.Show(art, Color.black);
                if (revealBurst) { revealBurst.sprite = BurstSprite(r.grade); revealBurst.color = A(col, 0); }
                if (revealRing) revealRing.color = A(col, 0);
                if (revealPillar) { revealPillar.gameObject.SetActive(high); revealPillar.color = A(col, 0); }
                if (revealFlash) revealFlash.color = A(Color.white, 0);
                card.localScale = Vector3.one * cardBase * 0.85f;

                // 0) 고유 컷씬이 있으면 (전설 이상) 실루엣 단계 대신 — 끝나면 바로 번쩍 → 공개
                // (하수구가 앞으로 빨려 들며 하얗게 → 컷씬은 하얀 화면에서 시작 → 컷씬 끝 하얀 화면에서 카드가 번쩍 공개)
                var intro = Intro(r);
                if (intro)
                {
                    reveal.SetActive(false);
                    if (!string.IsNullOrEmpty(introSfx)) SfxManager.Play(introSfx);
                    yield return PushOut(introCut);
                    if (flash) flash.gameObject.SetActive(false);          // Root/Flash 는 컷씬보다 위 → 컷씬 자기 흰 화면으로 넘김
                    if (stage) stage.localScale = Vector3.one;
                    yield return PlayIntro(intro, r);
                    CutFx.Alpha(flash, 1);
                    reveal.SetActive(true);
                }

                // 1) 실루엣 + 모이는 빛
                float charge = intro ? 0 : top ? 1.1f : high ? 0.75f : 0.35f;
                if (!intro) SfxManager.Play(high ? "superjump_charge" : "tuto_highlight");
                for (float x = 0; x < charge && !skipping; x += Time.unscaledDeltaTime)
                {
                    float k = x / charge;
                    card.localScale = Vector3.one * cardBase * (0.85f + 0.05f * k + 0.02f * Mathf.Sin(x * 40) * k);
                    if (revealPillar && high) { revealPillar.color = A(col, k); revealPillar.rectTransform.localScale = new Vector3(Mathf.Lerp(0.2f, 1, k), 1, 1); }
                    if (revealBurst) { revealBurst.color = A(col, 0.35f * k); revealBurst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 0.7f, k); revealBurst.rectTransform.localRotation = Quaternion.Euler(0, 0, x * 40); }
                    if (Random.value < Time.unscaledDeltaTime * (high ? 30 : 12)) Spark(rr, Random.insideUnitCircle.normalized * 420, col, -350);
                    yield return null;
                }

                // 2) 번쩍 → 공개
                if (por && art != null) por.Show(art);
                foreach (var g in shown) g.SetActive(true);
                if (flash && intro) StartCoroutine(Flash(introRevealFlash, 1));
                else if (flash && high) StartCoroutine(Flash(0.35f, top ? 0.85f : 0.5f, col));
                SfxManager.Play(top ? "jgl_achievement" : high ? "combo_word" : r.isNew ? "jgl_unlock" : "cheese");
                for (int i = 0; i < (top ? 28 : high ? 18 : 8); i++) Spark(rr, Vector2.zero, col, top ? 900 : 650);
                next = false;
                float hold = high ? revealHigh : revealLow;
                for (float x = 0; !skipping; x += Time.unscaledDeltaTime)
                {
                    float pop = x < 0.16f ? Mathf.Lerp(0.9f, 1.18f, E(x / 0.16f)) : x < 0.3f ? 1.18f - (x - 0.16f) / 0.14f * 0.18f : 1;
                    card.localScale = Vector3.one * cardBase * pop;
                    if (revealFlash) { float k = Mathf.Clamp01(x / 0.35f); revealFlash.color = A(Color.white, 1 - k); revealFlash.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.6f, E(k)); }
                    if (revealBurst) { revealBurst.color = A(col, x < 0.2f ? E(x / 0.2f) : 0.85f + 0.15f * Mathf.Sin(x * 4)); revealBurst.rectTransform.localScale = Vector3.one * (x < 0.25f ? Mathf.Lerp(0.7f, 1.25f, E(x / 0.25f)) : 1.2f + 0.04f * Mathf.Sin(x * 5)); revealBurst.rectTransform.localRotation = Quaternion.Euler(0, 0, 40 + x * 18); }
                    if (revealRing) { float k = Mathf.Clamp01(x / 0.6f); revealRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 2.2f, E(k)); revealRing.color = A(col, 1 - k); }
                    if (revealPillar && high) revealPillar.color = A(col, Mathf.Max(0, 1 - x * 1.5f));
                    if (high && Random.value < Time.unscaledDeltaTime * 6) Spark(rr, Random.insideUnitCircle * 300, col, 200);
                    if (x <= 0.18f) Shake((high ? shake : shake * 0.4f) * (1 - x / 0.18f)); else if (stage) stage.anchoredPosition = Vector2.zero;
                    if (revealHint && x > 0.4f && !revealHint.activeSelf) revealHint.SetActive(true);
                    if ((next && x > 0.25f) || x > hold) break;
                    yield return null;
                }
                next = false;
            }
            if (revealHint) revealHint.SetActive(false);
            reveal.SetActive(false);
        }

        bool Must(GeneGacha.Result r) => r.isNew && r.grade >= mustIntroFrom;

        // 고유 컷씬 한 번 (꼭 보여 줄 컷씬이면 그동안 건너뛰기 안 받고, 건너뛰는 중이었어도 끝까지 보여 줌)
        IEnumerator PlayIntro(GachaIntro intro, GeneGacha.Result r)
        {
            bool must = Must(r), wasSkip = skipping;
            if (must) { lockSkip = true; skipping = false; if (skip) skip.gameObject.SetActive(false); }
            if (flash) flash.gameObject.SetActive(false);          // Root/Flash 는 컷씬보다 위 → 컷씬 자기 흰 화면으로
            intro.gameObject.SetActive(true);
            yield return intro.Play(this, r);
            intro.gameObject.SetActive(false);
            introShown.Add(r);
            if (must) { lockSkip = false; skipping = wasSkip; if (skip) skip.gameObject.SetActive(!skipping); }
        }

        // 1번째 건너뛰기: 지금 장면을 정리하고 배관 맨 아래(뚜껑 앞)로 — 하얀 화면에서 걷히며
        void JumpToSewer()
        {
            RestoreWorld();
            foreach (var c in new[] { captionLab, captionSuck, captionRattle }) if (c) c.gameObject.SetActive(false);
            if (explosion) explosion.gameObject.SetActive(false);
            if (dim) dim.alpha = 1;
            Cut(cutPipe);
            if (pipeImage)
            {
                var rt = pipeImage.rectTransform; rt.localScale = Vector3.one;
                float travel = Mathf.Max(0, rt.rect.height - (cutPipe ? cutPipe.rect.height : 0));
                rt.anchoredPosition = new Vector2(0, travel * 0.5f);
            }
            pipeLines.Clear();
            StartCoroutine(CutFx.PunchIn(stage, flash, pipeCut.punchTime, pipeCut.punchFrom, Stopping));
        }

        // 이 쥐의 고유 컷씬 (없으면 null) — introFrom 등급 이상 · firstOnly 면 처음 뽑았을 때만
        GachaIntro Intro(GeneGacha.Result r)
        {
            if (intros == null || r.grade < introFrom) return null;
            foreach (var i in intros) if (i && i.code == r.code && (r.isNew || !i.firstOnly)) return i;
            return null;
        }

        // 카드 칸 채우기 (강조 카드 · 결과 칸 공통)
        void Fill(GameObject go, GeneGacha.Result r, string countTpl)
        {
            var row = Row(r.code); int gi = (int)r.grade;
            var frame = go.transform.Find("Frame")?.GetComponent<Image>(); if (frame) frame.color = MarketPage.GradeColor(gi);
            var g = go.transform.Find("Grade")?.GetComponent<TMP_Text>(); if (g) g.text = MarketPage.GradeName(gi);
            var nm = go.transform.Find("Name")?.GetComponent<TMP_Text>(); if (nm) nm.text = row != null ? row.character_name : "";
            var ct = go.transform.Find("Count")?.GetComponent<TMP_Text>(); if (ct) ct.text = (countTpl ?? ct.text).Replace("{n}", r.shards.ToString());
            var nw = go.transform.Find("New"); if (nw) nw.gameObject.SetActive(r.isNew);
            var ws = go.transform.Find("Wish"); if (ws) ws.gameObject.SetActive(r.wish);
            var por = go.transform.Find("Portrait")?.GetComponent<RatPortrait>(); if (por && row != null && artLibrary) por.Show(artLibrary.Get(row.code_id));
        }

        // 결과 칸 — 통통 하나씩 (건너뛰면 한 번에)
        IEnumerator Results(List<GeneGacha.Result> res)
        {
            if (grid) grid.gameObject.SetActive(true);
            var cards = new List<GameObject>();
            foreach (var r in res)
            {
                var go = Instantiate(cardTemplate, grid); spawned.Add(go);
                go.SetActive(true); Fill(go, r, null);
                go.transform.localScale = skipping ? Vector3.one : Vector3.zero;
                cards.Add(go);
            }
            if (skip) skip.gameObject.SetActive(false);
            bool wasSkip = skipping; skipping = false;
            if (!wasSkip)
                foreach (var c in cards)
                {
                    StartCoroutine(Pop(c.transform));
                    yield return Wait(0.05f);
                }
            bool high = res.Exists(r => r.grade >= Grade.Legendary);
            SfxManager.Play(high ? "jgl_achievement" : "jgl_unlock");

            if (ok) ok.gameObject.SetActive(true);
            if (again)
            {
                bool can = GeneGacha.CanPull(lastN);
                again.gameObject.SetActive(lastN > 0);
                again.interactable = can;
                if (againText) againText.text = (againTpl ?? "").Replace("{n}", lastN.ToString()).Replace("{cost}", LobbyManager.Fmt(lastN == 10 ? GeneGacha.CostTen : GeneGacha.CostOne * lastN));
            }
        }

        IEnumerator Pop(Transform t)
        {
            for (float x = 0; x < 0.2f && t; x += Time.unscaledDeltaTime) { float k = x / 0.2f; t.localScale = Vector3.one * (k < 0.7f ? k / 0.7f * 1.15f : 1.15f - (k - 0.7f) / 0.3f * 0.15f); yield return null; }
            if (t) t.localScale = Vector3.one;
        }

        IEnumerator Flash(float time, float peak, Color tint = default)
        {
            if (!flash) yield break;
            flash.gameObject.SetActive(true);
            var c = tint == default ? Color.white : Color.Lerp(tint, Color.white, 0.5f);
            for (float x = 0; x < time; x += Time.unscaledDeltaTime) { flash.color = A(c, peak * (1 - x / time)); yield return null; }
            flash.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!Playing) return;
            var k = UnityEngine.InputSystem.Keyboard.current; var m = UnityEngine.InputSystem.Mouse.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) { if (ok && ok.gameObject.activeSelf) Close(); else OnSkip(); }
            bool press = (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) || (m != null && m.leftButton.wasPressedThisFrame);
            if (press && reveal && reveal.activeSelf) next = true;
            if (press && waitTap) tapped = true;
        }
    }
}
