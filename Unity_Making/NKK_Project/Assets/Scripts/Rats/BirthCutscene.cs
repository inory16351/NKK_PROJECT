using System.Collections.Generic;
using NKK.Data;
using NKK.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Rats
{
    /// <summary>
    /// 번식 탄생 연출 (사용자 2026-11-05): 이 저장 파일에서 처음 번식으로 태어난 쥐 종마다 한 번.
    /// 부모 두 마리(리그 파츠 그림)가 걸어와 몇 번 부딪힘 → 합쳐지며 화이트 플래시 → 하트 등장 → 하트가 녹듯 사라지며(디졸브) 새 쥐가 점점 커짐
    /// → 아래에 "새로운 쥐 탄생 !!" · "{name}!!". 여러 마리가 겹치면 줄을 서서 하나씩. 게임은 멈추지 않음 (화면 위에 겹쳐 보임, 클릭은 그대로 통과).
    /// 스위치 = Progress.MarkBirthFx (저장 파일마다 종별 한 번). 오브젝트는 하이라키 HUD/BirthCutscene 아래, 글은 씬 TMP (LocText).
    /// </summary>
    public class BirthCutscene : MonoBehaviour
    {
        public static BirthCutscene I { get; private set; }

        [Header("오브젝트 (하이라키)")]
        [Tooltip("연출 전체 (평소 꺼 둠) — CanvasGroup 으로 전체 투명도")] public CanvasGroup content;
        [Tooltip("뒤 어둡게")] public Image dim;
        [Tooltip("부모 쥐 왼쪽 (오른쪽을 봄) · 오른쪽 (왼쪽을 봄) · 새 쥐")] public RatPortrait parentA, parentB, baby;
        [Tooltip("하트 (NKK/UIDissolve 머티리얼)")] public Image heart;
        [Tooltip("하트 뒤 햇살 (돌아감)")] public Image rays;
        [Tooltip("반짝이 (반짝반짝 커졌다 작아짐)")] public RectTransform[] sparkles;
        [Tooltip("화이트 플래시 (화면 전체)")] public Image flash;
        [Tooltip("'새로운 쥐 탄생 !!' · '{name}!!' (LocText 틀)")] public TMP_Text title, nameText;
        public RatArtLibrary artLibrary;

        [Header("시간 (초)")]
        [Tooltip("부모가 양쪽에서 걸어 들어옴")] public float enterTime = 0.35f;
        [Tooltip("한 번 부딪히는 데 걸리는 시간 · 부딪히는 횟수 (마지막 다음에 합쳐짐)")] public float bumpTime = 0.28f;
        public int bumpCount = 3;
        [Tooltip("합쳐지며 하얘짐")] public float mergeTime = 0.25f;
        [Tooltip("플래시가 걷히는 시간")] public float flashTime = 0.3f;
        [Tooltip("하트가 톡 튀어나옴 · 하트만 보이는 시간 (그 뒤 쥐가 자람)")] public float heartPopTime = 0.3f, heartHold = 0.35f;
        [Tooltip("새 쥐가 자라며 하트가 녹는 시간")] public float growTime = 0.9f;
        [Tooltip("자라기 시작 뒤 글이 뜨는 시점 (자라는 시간 배율) · 글이 떠 있는 시간 · 글 튀어나오는 시간")] public float nameAt = 0.6f, nameTime = 1.2f, namePopTime = 0.3f;
        [Tooltip("사라짐 · 다음 쥐 연출까지 쉬는 시간")] public float outTime = 0.3f, gapTime = 0.15f;

        [Header("부모 움직임 (UI px)")]
        [Tooltip("두 쥐 중심 사이 거리의 절반: 들어오기 시작 · 쉬는 자리 · 맞닿는 자리")] public float startGap = 620, restGap = 230, touchGap = 105;
        [Tooltip("부딪힐 때마다 쉬는 자리가 가까워지는 비율")] public float restShrink = 0.15f;
        [Tooltip("튕겨 날 때 뛰는 높이 · 부딪힐 때 찌그러짐 · 합쳐질 때 크기")] public float hop = 45, squash = 0.18f, mergeScale = 0.55f;
        [Tooltip("다리 흔들기 각도 · 빠르기 · 머리 끄덕 각도 · 꼬리 흔들기 각도")] public float legSwing = 28, legSpeed = 22, headBob = 8, tailSwing = 14;

        [Header("하트 · 새 쥐")]
        [Tooltip("하트 두근 크기 · 빠르기 · 녹으며 커지는 배율")] public float heartBeat = 0.07f, heartBeatSpeed = 9, heartGrow = 0.3f;
        [Tooltip("하트가 다 녹는 시점 (자라는 시간 배율)")] public float heartGone = 0.85f;
        [Tooltip("새 쥐 처음 크기 (배율)")] public float babyStart = 0.15f;
        [Tooltip("햇살 도는 속도 (도/초) · 반짝이 빠르기")] public float raySpin = 25, sparkleSpeed = 5;
        [Tooltip("뒤 어둡게 투명도")] [Range(0, 1)] public float dimAlpha = 0.35f;

        [Header("효과음 (SfxManager 이름, 비우면 없음)")]
        public string bumpSfx = "rat_land", flashSfx = "rat_grade_birth", heartSfx = "rat_birth", nameSfx = "jgl_unlock";

        class Item { public RatCharacterRow row; public string a, b; }
        readonly Queue<Item> queue = new();
        Item cur; float t, wait; bool playing;
        string nameTpl; Material heartMat;
        static readonly int CutId = Shader.PropertyToID("_Cut");
        int lastBump; bool flashed, hearted, named;

        public static bool Playing => I && (I.playing || I.queue.Count > 0);

        // 연출 줄 세우기 (스위치는 여기서 켬 — 같은 종이 한 번에 두 번 줄 서지 않게)
        public static void Enqueue(RatCharacterRow row, string parentA, string parentB)
        {
            if (row == null || !Progress.I || !Progress.I.MarkBirthFx(row.code_id)) return;
            if (!I) return;
            I.queue.Enqueue(new Item { row = row, a = parentA, b = parentB });
        }

        void Awake()
        {
            I = this;
            if (nameText) nameTpl = nameText.text;
            if (heart && heart.material) { heartMat = new Material(heart.material); heart.material = heartMat; }
            if (content) content.gameObject.SetActive(false);
        }
        void Start()
        {
            ults = FindFirstObjectByType<NKK.Ults.UltimateManager>();
            stage = FindFirstObjectByType<NKK.Stage.StageManager>();
            if (stage) stage.FloorEntered += Cancel;
        }
        void OnDestroy() { if (stage) stage.FloorEntered -= Cancel; if (I == this) I = null; if (heartMat) Destroy(heartMat); }

        // ── 예외처리 (사용자 2026-11-05): 아래 경우엔 보던 연출 · 줄 선 연출을 모두 지움 (다시 띄우지 않음, 스위치는 켜진 채) ──
        //   판(층)이 끝남 = 다음 층 시작 · 씬이 바뀜(아지트로) · 게임 오버 · 필살기 컷인 / 슈퍼 필살기 컷신 (컷인이 보이게)
        NKK.Ults.UltimateManager ults; NKK.Stage.StageManager stage;
        bool MustClear => GameOver.Active || (ults && ults.CutPhase) || (NKK.Ults.SuperUltManager.I && NKK.Ults.SuperUltManager.I.Busy);

        public void Cancel()
        {
            queue.Clear(); wait = 0;
            if (playing) { playing = false; cur = null; }
            if (content) content.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!content) return;
            if (MustClear) { if (playing || queue.Count > 0) Cancel(); return; }
            if (!playing)
            {
                if (wait > 0) { wait -= Time.unscaledDeltaTime; return; }
                if (queue.Count == 0) return;
                Begin(queue.Dequeue());
            }
            // 포기 창 · 튜토리얼 대사 · 증강 고르기로 게임이 멈추면 연출도 멈추고 숨김
            if (FxManager.Halt) { content.alpha = 0; return; }
            t += Time.unscaledDeltaTime;
            Apply();
        }

        void Begin(Item it)
        {
            cur = it; t = 0; playing = true; lastBump = -1; flashed = hearted = named = false;
            content.gameObject.SetActive(true);
            content.blocksRaycasts = false; content.interactable = false;
            var lib = artLibrary;
            if (parentA) { parentA.gameObject.SetActive(true); parentA.Show(lib ? lib.Get(it.a) : null); }
            if (parentB) { parentB.gameObject.SetActive(true); parentB.Show(lib ? lib.Get(it.b) : null); }
            if (baby) baby.Show(lib ? lib.Get(it.row.code_id) : null);
            if (nameText) nameText.text = LocText.Tpl(nameText, nameTpl).Replace("{name}", it.row.character_name);
            Apply();
        }

        void End()
        {
            playing = false; cur = null; wait = gapTime;
            content.gameObject.SetActive(false);
        }

        // ── 시간 t 에 맞춰 전부 놓기 ──
        void Apply()
        {
            float sBump = enterTime, sMerge = sBump + bumpTime * bumpCount, sFlash = sMerge + mergeTime;
            float sGrow = sFlash + heartHold, sName = sGrow + growTime * nameAt, sOut = sGrow + growTime + nameTime, sEnd = sOut + outTime;
            if (t >= sEnd) { End(); return; }

            content.alpha = t < sOut ? 1 : 1 - Mathf.Clamp01((t - sOut) / Mathf.Max(0.01f, outTime));
            if (dim) dim.color = new Color(0, 0, 0, dimAlpha * Mathf.Clamp01(t / 0.2f));

            // 부모: 들어오기 → 부딪히기 × n → 합쳐지기
            float d, y = 0, sx = 1, sy = 1, alpha = 1, walk = 1;
            if (t < sBump)
            {
                float u = t / Mathf.Max(0.01f, enterTime);
                d = Mathf.Lerp(startGap, restGap, EaseOut(u)); alpha = Mathf.Clamp01(u * 3);
            }
            else if (t < sMerge)
            {
                int k = Mathf.Min(bumpCount - 1, (int)((t - sBump) / bumpTime));
                float u = (t - sBump - k * bumpTime) / bumpTime;
                float rest0 = Rest(k), rest1 = Rest(k + 1);
                if (u < 0.4f) { d = Mathf.Lerp(rest0, touchGap, EaseIn(u / 0.4f)); }
                else
                {
                    float v = (u - 0.4f) / 0.6f;
                    d = Mathf.Lerp(touchGap, rest1, EaseOut(v)); y = Mathf.Sin(v * Mathf.PI) * hop;
                    float q = Mathf.Clamp01(1 - v * 3) * squash;        // 맞닿은 순간 찌그러졌다 돌아옴
                    sx = 1 - q; sy = 1 + q * 0.6f; walk = 0.3f;
                    if (k != lastBump) { lastBump = k; Sfx(bumpSfx); }
                }
            }
            else if (t < sFlash)
            {
                float u = (t - sMerge) / Mathf.Max(0.01f, mergeTime);
                d = Mathf.Lerp(Rest(bumpCount), 0, EaseIn(u));
                sx = sy = Mathf.Lerp(1, mergeScale, u); alpha = 1 - u * 0.3f;
            }
            else { d = 0; alpha = 0; }
            PlaceParent(parentA, -d, y, sx, sy, alpha, walk, 0);
            PlaceParent(parentB, d, y, sx, sy, alpha, walk, 1.3f);

            // 화이트 플래시: 합쳐질 때 차오름 → 걷힘
            float fl = t < sMerge ? 0 : t < sFlash ? EaseIn((t - sMerge) / Mathf.Max(0.01f, mergeTime)) : 1 - Mathf.Clamp01((t - sFlash) / Mathf.Max(0.01f, flashTime));
            if (flash) flash.color = new Color(1, 1, 1, fl);
            if (t >= sFlash && !flashed) { flashed = true; Sfx(flashSfx); }

            // 하트: 플래시 정점에서 톡 → 두근두근 → 쥐가 자라는 동안 녹아 사라짐
            if (heart)
            {
                bool on = t >= sFlash;
                heart.enabled = on;
                if (on)
                {
                    float pop = EaseOutBack(Mathf.Clamp01((t - sFlash) / Mathf.Max(0.01f, heartPopTime)));
                    float g = Mathf.Clamp01((t - sGrow) / Mathf.Max(0.01f, growTime * heartGone));
                    float beat = 1 + heartBeat * Mathf.Pow(Mathf.Abs(Mathf.Sin((t - sFlash) * heartBeatSpeed)), 4);
                    heart.rectTransform.localScale = Vector3.one * (pop * beat * (1 + heartGrow * EaseOut(g)));
                    if (heartMat) heartMat.SetFloat(CutId, g);
                    heart.enabled = g < 1;
                    if (!hearted) { hearted = true; Sfx(heartSfx); }
                }
            }
            if (rays)
            {
                float ra = t < sFlash ? 0 : Mathf.Clamp01((t - sFlash) / 0.3f);
                rays.enabled = ra > 0;
                rays.color = new Color(1, 1, 1, ra);
                rays.rectTransform.localRotation = Quaternion.Euler(0, 0, -(t - sFlash) * raySpin);
                rays.rectTransform.localScale = Vector3.one * (0.6f + 0.4f * EaseOut(ra));
            }

            // 새 쥐: 하트 안에서 작게 → 크게 (톡 넘쳤다 돌아옴), 다 자란 뒤엔 살랑살랑
            if (baby)
            {
                bool on = t >= sGrow;
                baby.gameObject.SetActive(on);
                if (on)
                {
                    float u = Mathf.Clamp01((t - sGrow) / Mathf.Max(0.01f, growTime));
                    float s = Mathf.LerpUnclamped(babyStart, 1, EaseOutBack(u));
                    var rt = (RectTransform)baby.transform;
                    rt.localScale = new Vector3(s, s, 1);
                    rt.anchoredPosition = new Vector2(0, u >= 1 ? Mathf.Abs(Mathf.Sin((t - sGrow) * 6)) * 10 : 0);
                    SetAlpha(baby, Mathf.Clamp01(u / 0.3f));
                    Wiggle(baby, t - sGrow, u >= 1 ? 0.5f : 0.2f, 0.5f);
                }
            }

            // 반짝이
            if (sparkles != null)
                for (int i = 0; i < sparkles.Length; i++)
                {
                    var sp = sparkles[i]; if (!sp) continue;
                    float v = t < sGrow ? 0 : Mathf.Max(0, Mathf.Sin((t - sGrow) * sparkleSpeed + i * 1.7f));
                    sp.localScale = Vector3.one * v;
                    sp.localRotation = Quaternion.Euler(0, 0, (t + i) * 90);
                }

            // 글: "새로운 쥐 탄생 !!" → 조금 뒤 "{name}!!"
            if (title) Pop(title.rectTransform, t - sName);
            if (nameText) Pop(nameText.rectTransform, t - sName - 0.15f);
            if (t >= sName && !named) { named = true; Sfx(nameSfx); }
        }

        float Rest(int k) => Mathf.Max(touchGap, restGap * (1 - restShrink * k));

        void Pop(RectTransform rt, float dt)
        {
            rt.gameObject.SetActive(dt >= 0);
            if (dt >= 0) rt.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(dt / Mathf.Max(0.01f, namePopTime)));
        }

        // 부모 한 마리 놓기 (x = 무대 가운데 기준, 쥐가 바라보는 방향은 RatPortrait.faceRight)
        void PlaceParent(RatPortrait p, float x, float y, float sx, float sy, float alpha, float walk, float phase)
        {
            if (!p) return;
            p.gameObject.SetActive(alpha > 0.001f);
            if (alpha <= 0.001f) return;
            var rt = (RectTransform)p.transform;
            rt.anchoredPosition = new Vector2(x, y);
            rt.localScale = new Vector3(sx, sy, 1);
            SetAlpha(p, alpha);
            Wiggle(p, t + phase, walk, 1);
        }

        // 리그 파츠 흔들기: 다리 교차 · 머리 끄덕 · 꼬리 (파츠 피벗 = 관절, RatPortrait.Put)
        void Wiggle(RatPortrait p, float time, float walk, float tail)
        {
            float w = Mathf.Sin(time * legSpeed) * legSwing * walk;
            Rot(p.front, w); Rot(p.farBack, w); Rot(p.back, -w); Rot(p.farFront, -w);
            Rot(p.head, Mathf.Sin(time * legSpeed * 0.5f) * headBob * Mathf.Max(walk, 0.4f));
            Rot(p.tail, Mathf.Sin(time * 7) * tailSwing * tail);
        }
        static void Rot(Image i, float deg) { if (i) i.rectTransform.localRotation = Quaternion.Euler(0, 0, deg); }

        static void SetAlpha(RatPortrait p, float a)
        {
            foreach (var i in new[] { p.farBack, p.farFront, p.tail, p.torso, p.back, p.front, p.head, p.single })
                if (i) { var c = i.color; c.a = a; i.color = c; }
        }

        static void Sfx(string n) { if (!string.IsNullOrEmpty(n)) SfxManager.Play(n); }
        static float EaseIn(float u) => u * u;
        static float EaseOut(float u) => 1 - (1 - u) * (1 - u);
        static float EaseOutBack(float u) { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(u - 1, 3) + c1 * Mathf.Pow(u - 1, 2); }

        // ── 테스트: Play 중 인스펙터 ⋮ 메뉴 → 아무 쥐 셋으로 연출 (스위치는 안 켬) ──
        [ContextMenu("테스트 재생 (Play 중)")]
        void TestPlay()
        {
            if (!Application.isPlaying || GameDatabase.Instance == null) return;
            var all = new List<RatCharacterRow>(GameDatabase.Instance.Rats.Values);
            if (all.Count == 0) return;
            RatCharacterRow R() => all[Random.Range(0, all.Count)];
            queue.Enqueue(new Item { row = R(), a = R().code_id, b = R().code_id });
        }
        [ContextMenu("테스트 재생 3마리 연속 (Play 중)")]
        void TestPlay3() { TestPlay(); TestPlay(); TestPlay(); }
    }
}
