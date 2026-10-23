using System.Collections.Generic;
using NKK.Data;
using NKK.Items;
using NKK.Rats;
using NKK.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NKK.Ults
{
    // 필살기 게이지 매니저.
    // · 게이지는 쥐 한 마리마다 따로 (Rat.ultGauge): 그 쥐가 Ult_Charge 시트 조건(물건·벽 파괴, 액션, 번식, 사람·고양이 퇴치, 층 통과…)을 하면 참.
    // · 요구량 = 쥐 테이블 Ultimate.ult_gauge, 충전량 × 공용 스킬 '필살기 연습' 배율. 다 차면 하단에 그 쥐 버튼이 뜨고 머리 위에 반짝이.
    // · 버튼을 누르거나(또는 '필살기 자동 사용' 공용 스킬) → 대기열. 필살기는 모은 그 쥐가 씀. concurrentTier 전엔 한 번에 하나 (앞 필살기가 끝나야 다음 것),
    //   그 훈장부터는 대기열을 기다림 없이 바로 전부 동시에.
    // · 진행: ① 컷인 (cutTime 초, 확대 · 제목 — 카메라 고정 끄면 없음) → ② 상황극 (UltXxx.Dur 초, 게임 진행) → ③ 업적 알림
    // · 컷인 없이 시작한 필살기 (동시 필살기 · 카메라 고정 끔) 는 딜 미터기 아래 UltLog 에 한 줄씩
    public class UltimateManager : MonoBehaviour
    {
        [Header("연결")]
        public RatManager Rats;
        public ItemManager Items;
        public StageManager Stage;
        public GameManager Game;
        public CameraController Cam;
        [Tooltip("슈퍼 점프 중엔 필살기를 미룸")] public SuperJumpManager SuperJump;

        [Header("하단 버튼")]
        [Tooltip("버튼이 늘어서는 곳 (가로 정렬)")] public RectTransform bar;
        [Tooltip("버튼 템플릿 (꺼 둔 채로 두면 복제해서 씀)")] public UltButton buttonTemplate;
        [Tooltip("필살기 아이콘 (테이블 ult_icon = UltIcons/ult_<id>) — 컴포넌트 메뉴 Fill Icons")] public List<FxManager.NamedSprite> icons = new();
        [Tooltip("버튼 최대 개수 (더 많이 차면 먼저 찬 쥐부터 보이고 나머지는 자리가 나면)")] public int maxButtons = 8;

        [Header("다 찬 쥐 머리 위 표시")]
        [Tooltip("반짝이 그림 (필살기 색으로 물듦). 비우면 표시 안 함")] public Sprite readyMark;
        [Tooltip("크기 · 기절 별 높이에서 더 올림 (게임 단위, 쥐 크기 배율이 곱해짐)")] public float readyMarkSize = 26, readyMarkLift = 14;
        [Tooltip("하단 버튼에 마우스를 올리면 이 배율로 커짐")] public float readyMarkHover = 1.9f;

        [Header("컷인 (Canvas 자식)")]
        public CanvasGroup cutIn;
        [Tooltip("필살기 이름")] public TMP_Text cutTitle;
        [Tooltip("'{grade} · {name}의 필살기' 같은 글 (자리: {grade} {name})")] public TMP_Text cutSub;
        [Tooltip("제목 뒤 띠 (왼쪽에서 휙 들어옴)")] public RectTransform cutBanner;
        [Tooltip("집중선 (빙글빙글)")] public RectTransform cutLines;
        [Tooltip("컷인 띠 색 = 필살기 테마 색으로 칠할 이미지")] public Image cutTint;
        [Tooltip("화면 번쩍 (전체 덮는 이미지)")] public Image flash;

        [Header("자막 · 업적")]
        [Tooltip("큰 자막 (자막 크기 ≥ bigCaptionSize)")] public TMP_Text captionBig;
        public TMP_Text captionSmall;
        public float bigCaptionSize = 40;
        [Tooltip("자막이 보이는 시간 (초)")] public float captionTime = 1.3f;
        [Tooltip("끝날 때 휘말린 사람 수 팝업 (자리: {n}). 비우면 안 띄움")] public string blastPopup = "";

        [Header("소품 그림 (UltXxx 의 Prop 이름) — 컴포넌트 메뉴 Fill Props")]
        public List<FxManager.NamedSprite> props = new();
        public Transform propRoot;
        [Tooltip("소품 정렬 레이어·재질 기준 (쥐덫 등 월드 스프라이트)")] public SpriteRenderer propTemplate;

        [Header("수치 (웹게임 기준)")]
        [Tooltip("컷인 시간 (초)")] public float cutTime = 1.4f;
        [Tooltip("컷인 확대")] public float cutZoom = 1.6f;
        [Tooltip("필살기 반경 (끝날 때 휘말림)")] public float ultRadius = 560;
        [Tooltip("필살기 피해 = 공격력 × 이 값 (ultD)")] public float ultDamageK = 13;
        [Tooltip("물건 하나 피해 = 공격력 × 이 값 (ultItemD)")] public float ultItemK = 4.5f;
        [Tooltip("필살기 하나가 끝나고 다음 필살기까지 기다리는 시간 (초). 2026-10-19 사용자: 필살기 사이 쿨타임 없앰 → 0")] public float ultCooldown = 0;
        [Tooltip("공용 스킬로 줄어도 최소 (초)")] public float ultCooldownMin = 0;
        [Tooltip("상황극 중 주변 사람·고양이 휘말림 반경 · 속도 (0.3초마다)")] public float actorRadius = 220, actorSpeed = 620;
        [Tooltip("테스트: 시작부터 모든 게이지를 채움")] public bool testFullGauge;
        [Header("동시 필살기 (2026-10-21 사용자: 필살기가 너무 쌓임)")]
        [Tooltip("이 훈장부터 필살기가 차는 대로 바로 동시에 (대표 컷인을 기다리지 않음 · 두 번째부터는 컷인 없이 바로 상황극). 0 = 끔")] public int concurrentTier = 8;
        [Tooltip("동시에 돌아가는 필살기 최대 수 (컷인 있는 대표 1 + 나머지). 0 = 제한 없음")] public int maxConcurrent = 0;
        [Tooltip("컷인 없이 시작한 필살기를 적는 로그 (딜 미터기 아래)")] public UltLog log;
        [Header("카메라 고정 (Y 키로 켜고 끔, 저장)")]
        public UnityEngine.InputSystem.Key camLockKey = UnityEngine.InputSystem.Key.Y;
        [Tooltip("켜고 끌 때 배너 (빈칸 = 안 띄움)")] public string camLockOn = "필살기 카메라 고정 켬", camLockOff = "필살기 카메라 고정 끔";
        public int camLockOnKey, camLockOffKey;
        const string CamLockPref = "nkk_ult_cam";
        public static bool CamLock => PlayerPrefs.GetInt(CamLockPref, 1) == 1;

        // ── 상태 ──
        readonly List<Rat> queue = new();
        readonly Dictionary<Rat, UltButton> buttons = new();
        readonly List<Rat> buttonOrder = new();
        readonly HashSet<Rat> testFilled = new();
        UltBase cur; bool cutPhase; float cutT, flashA; Color flashCol;
        readonly List<UltBase> extras = new();      // 컷인 없이 같이 도는 필살기 (동시 필살기)
        float capBigT = -9, capSmallT = -9;

        public bool Busy => cur != null || extras.Count > 0;
        int Running => (cur != null ? 1 : 0) + extras.Count;
        bool Concurrent => concurrentTier > 0 && Progress.I && Progress.I.tier >= concurrentTier;
        bool RoomForMore => maxConcurrent <= 0 || Running < maxConcurrent;
        bool IsRunning(Rat r) { if (cur != null && cur.R == r) return true; foreach (var e in extras) if (e.R == r) return true; return false; }
        public bool CutPhase => cur != null && cutPhase;      // 컷인 중 (월드 정지)
        public float Cooldown => Mathf.Max(ultCooldownMin, ultCooldown - CommonSkill.UltCdLess);
        public float CooldownLeft => cdLeft;
        float cdLeft;
        public UltBase Current => cur;
        static GameDatabase DB => GameDatabase.Instance;

        void Awake()
        {
            if (buttonTemplate) buttonTemplate.gameObject.SetActive(false);
            if (cutIn) { cutIn.alpha = 0; cutIn.gameObject.SetActive(false); }
            if (captionBig) captionBig.text = ""; if (captionSmall) captionSmall.text = "";
            if (flash) flash.enabled = false;
        }

        // ── 게이지 ──
        public float Need(string code)
        {
            var u = DB.RatsByCode.TryGetValue(code, out var row) ? DB.UltOf(row) : null;
            return u == null ? 0 : Mathf.Max(1, u.ult_gauge);
        }
        public float Need(Rat r) => r && r.UltUnlocked ? Need(r.codeId) : 0;      // 필살기 해금(쳇바퀴 훈련) 전엔 게이지 없음
        public float Gauge01(Rat r) { float n = Need(r); return n > 0 ? Mathf.Clamp01(r.ultGauge / n) : 0; }
        public bool Full(Rat r) { float n = Need(r); return n > 0 && r.ultGauge >= n; }

        // 조건을 한 그 쥐만 참
        public void Charge(Rat r, CondType c, float times = 1)
        {
            if (!r || r.temp > 0 || DB == null) return;
            if (!DB.UltCharges.TryGetValue(c, out var g) || g <= 0) return;
            if (IsRunning(r)) return;      // 쓰는 중엔 안 참
            Add(r, g * times * CommonSkill.UltGaugeMul);
        }
        // 무리의 쥐 전부 (층 통과 등)
        public void ChargeAll(CondType c)
        {
            if (!DB.UltCharges.TryGetValue(c, out var g) || g <= 0) return;
            foreach (var r in Rats.Rats) if (r.temp <= 0) Add(r, g * CommonSkill.UltGaugeMul);
        }
        void Add(Rat r, float v) { float n = Need(r); if (n <= 0) return; bool was = r.ultGauge >= n; r.ultGauge = Mathf.Min(n, r.ultGauge + v); if (!was && r.ultGauge >= n) SfxManager.PlayAt("ult_ready", r.x, r.y); }

        // ── 사용 ──
        public void Request(Rat r)
        {
            if (GameOver.Active || !r || r.temp > 0 || !Full(r) || queue.Contains(r) || IsRunning(r)) return;
            queue.Add(r);
        }

        // 테스트 버튼: 게이지 채우고 바로 사용. 그 종 쥐가 화면에 없으면 화면 가운데 근처에 15초짜리로 불러옴
        public bool TestUlt(string code)
        {
            if ((cur != null && (!Concurrent || !RoomForMore)) || (SuperJump && SuperJump.Busy) || !DB.RatsByCode.TryGetValue(code, out var row) || DB.UltOf(row) == null) return false;
            Rat r = null;
            foreach (var o in Rats.Rats) if (o.codeId == code && !o.UltOn && o.OnScreen(-0.05f)) { r = o; break; }
            if (!r)
            {
                var c = ViewRect(0).center; float a = Random.Range(0, Mathf.PI * 2);
                float x = c.x + Mathf.Cos(a) * 220, y = c.y + Mathf.Sin(a) * 140;
                if (!Stage.Open.Contains(StageManager.RoomOf(x, y))) { x = c.x; y = c.y; }
                r = Rats.Spawn(row, x, y, false);
                if (!r) return false;
                r.temp = 15; r.noBreed = 99; r.breedCD = 99;
                FxManager.I?.Dust(x, y, 8, 1.2f);
            }
            r.ultGauge = Need(r);
            queue.Remove(r);
            return TryStart(r);
        }

        public void CancelAll()
        {
            queue.Clear();
            foreach (var e in extras.ToArray()) Stop(e, false);
            if (cur != null) Stop(cur, false);
        }

        // 대표(컷인 + 카메라) 로 시작. 대표가 이미 돌고 있으면 (동시 필살기) 컷인 없이 바로 상황극.
        // 카메라 고정을 끄면 (Y) 대표도 컷인 · 확대 · 번쩍 없이 바로 상황극 — 컷인 없이 시작한 필살기는 딜 미터기 아래 로그에 적음
        bool TryStart(Rat r)
        {
            if (!r || r.UltOn) return false;
            var u = DB.UltOf(r.Data);
            var type = u != null && !string.IsNullOrEmpty(u.script) ? System.Type.GetType("NKK.Ults." + u.script) : null;
            if (type == null) { Debug.LogWarning($"[UltimateManager] 필살기 코드 없음: {u?.script} ({r.codeId})"); r.ultGauge = 0; return false; }
            var s = (UltBase)System.Activator.CreateInstance(type);
            s.Setup(this, r, u);
            r.ultGauge = 0;
            r.UltGrab(); r.z = 0; r.vz = 0;
            s.Pre();
            if (cur != null)
            {
                extras.Add(s); BeginAct(s);       // 동시 필살기: 컷인 · 카메라 없이
                if (log) log.Add(r, u, UltIcon(u));
                return true;
            }
            cur = s;
            if (!CamLock)
            {
                cutPhase = false; BeginAct(s);    // 카메라 고정 끔: 컷인 없이
                if (log) log.Add(r, u, UltIcon(u));
                return true;
            }
            SfxManager.Play("jgl_ult_cutin");
            cutPhase = true; cutT = 0;
            // 컷인 중 게임 정지 없앰 (2026-10-21 사용자) — 예전: FxManager.UltFreeze = true
            if (Cam && CamLock) { Cam.ultFollow = true; Cam.ultFocus = new Vector2(r.x, r.y); }
            Flash(u.Color, 0.35f);
            if (cutIn)
            {
                cutIn.gameObject.SetActive(true); cutIn.alpha = 0;
                if (cutTitle) cutTitle.text = cur.Title;
                if (cutTint) { var c = u.Color; c.a = cutTint.color.a; cutTint.color = c; }
                if (cutSub) { subFormat ??= cutSub.text; cutSub.text = subFormat.Replace("{grade}", r.GradeData.grade_name).Replace("{name}", r.Data.character_name); }
            }
            return true;
        }
        string subFormat;

        // 컷인 끝 → 상황극 시작 (동시 필살기는 바로)
        void BeginAct(UltBase s)
        {
            var r = s.R;
            if (s == cur) { cutPhase = false; FxManager.UltFreeze = false; if (cutIn) cutIn.gameObject.SetActive(false); }
            r.UltPose = null; r.UltJit = 0;
            var fx = FxManager.I;
            if (fx)
            {
                if (!string.IsNullOrEmpty(s.U.ult_line)) fx.Popup(r.x, r.y, s.U.ult_line, Color.white, 24, 1.6f, 80);
                fx.Ring(r.x, r.y, 140, Color.white, 0.5f); fx.Ring(r.x, r.y, 220, s.U.Color, 0.6f); fx.Dust(r.x, r.y, 14, 1.8f); fx.Shake(0.3f);
            }
            if (s == cur && CamLock) Flash(Color.white, 0.4f);
            s.Begin();
        }

        // 끝 (done = 끝까지 함 → 마무리 휘말림 + 업적)
        void Stop(UltBase s, bool done)
        {
            if (s == null) return;
            if (s == cur)
            {
                cur = null; cutPhase = false; FxManager.UltFreeze = false;
                if (cutIn) cutIn.gameObject.SetActive(false);
                if (Cam) { Cam.ultFollow = false; Cam.ultZoom = 1; }
            }
            else extras.Remove(s);
            cdLeft = Cooldown;                          // 다음 필살기까지 쿨타임
            var r = s.R;
            if (done)
            {
                s.Finish();
                if (r)
                {
                    int n = Items.BlastActors(r.x, r.y, ultRadius, 700, UltDamage(r) * 3, r);
                    if (n > 0 && !string.IsNullOrEmpty(blastPopup) && OnScreen(r.x, r.y)) FxManager.I?.Popup(r.x, r.y, blastPopup.Replace("{n}", n.ToString()), new Color(1, 0.95f, 0.75f), 22, 1.1f, 120);
                }
                if (Progress.I) Progress.I.OnUltUsed(s.U.ultimate_id);       // 업적 알림은 업적 테이블 기준 (AchievementToast)
                if (r && r.temp <= 0) SuperUltManager.I?.OnUltDone(r);        // 초필살기 스택 (신화 · 끝까지 쓴 것만)
            }
            s.Cleanup();
            s.ReleaseAll();
            if (r)
            {
                r.clues = 0;
                r.z = Mathf.Max(0, r.z);
                r.UltRelease();
                if (!Stage.Open.Contains(StageManager.RoomOf(r.x, r.y))) { r.x = s.X0; r.y = s.Y0; }
            }
        }

        public float UltDamage(Rat r) => r.SkillDamage * ultDamageK * CommonSkill.UltPowerMul * r.UltPowerMul;
        public float UltItemDamage(Rat r) => r.SkillDamage * ultItemK * CommonSkill.UltPowerMul * r.UltPowerMul;

        // ── 글자 ──
        public string CaptionText(int ultId, string key, object n = null)
        {
            var c = DB.UltCaption(ultId, key);
            if (c == null) return "";
            return n != null ? c.text.Replace("{n}", n.ToString()) : c.text;
        }
        public void ShowCaption(int ultId, string key, object n = null)
        {
            var c = DB.UltCaption(ultId, key); if (c == null) return;
            bool big = c.size >= bigCaptionSize;
            var t = big ? captionBig : captionSmall; if (!t) return;
            t.text = n != null ? c.text.Replace("{n}", n.ToString()) : c.text;
            t.color = !string.IsNullOrEmpty(c.color) && ColorUtility.TryParseHtmlString(c.color, out var col) ? col : Color.white;
            if (big) capBigT = Time.unscaledTime; else capSmallT = Time.unscaledTime;
        }
        public void Flash(Color c, float a) { flashCol = c; flashA = Mathf.Max(flashA, a); }

        // ── 소품 ──
        public UltProp MakeProp(string name)
        {
            Sprite sp = null; foreach (var p in props) if (p.name == name) { sp = p.sprite; break; }
            if (!sp) { Debug.LogWarning($"[UltimateManager] 소품 그림 없음: {name}"); return null; }
            var go = new GameObject("UltProp_" + name); go.transform.SetParent(propRoot ? propRoot : transform, false);
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = sp;
            if (propTemplate) { r.sortingLayerID = propTemplate.sortingLayerID; r.sharedMaterial = propTemplate.sharedMaterial; }
            return new UltProp { r = r };
        }

        // ── 화면 ──
        public Rect ViewRect(float pad = 0)
        {
            var cam = Camera.main;
            Vector2 a = World.FromUnity(cam.ViewportToWorldPoint(new Vector3(0, 1, 0))), b = World.FromUnity(cam.ViewportToWorldPoint(new Vector3(1, 0, 0)));
            return Rect.MinMaxRect(a.x + pad, a.y + pad, b.x - pad, b.y - pad);
        }
        public bool OnScreen(float x, float y, float margin = 120) => ViewRect(-margin).Contains(new Vector2(x, y));

        public static bool ForceAuto;     // 밸런스 측정: 플레이어처럼 필살기가 차면 바로 씀

        void Update()
        {
            if (DB == null) return;
            float udt = Time.unscaledDeltaTime, dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (testFullGauge) foreach (var r in Rats.Rats) if (r.temp <= 0 && testFilled.Add(r)) r.ultGauge = Need(r);
            // 자동 사용 (공용 스킬)
            bool auto = CommonSkill.UltAuto || ForceAuto;
            if (auto) foreach (var r in Rats.Rats) if (Full(r)) Request(r);
            // 대기열 → 하나씩 (동시 필살기 훈장이면 쿨 · 대표 컷인 기다림 없이 대기열 전부 바로)
            if (cur == null) cdLeft -= dt;
            bool conc = Concurrent;
            if (!FxManager.Halt && !(SuperJump && SuperJump.Busy) && !Heist.Active && !(Stage && Stage.Climbing) && !(SuperUltManager.I && SuperUltManager.I.Busy))      // 초필살기 컷신 중엔 기다림
                while (queue.Count > 0 && (conc ? RoomForMore : cur == null && cdLeft <= 0))
                {
                    var r = queue[0]; queue.RemoveAt(0);
                    if (r && r.temp <= 0 && Full(r) && TryStart(r) && !conc) break;
                }

            if (cur != null && cutPhase) StepCut(udt);
            else if (cur != null) StepAct(cur, dt);
            for (int i = extras.Count - 1; i >= 0; i--) if (i < extras.Count) StepAct(extras[i], dt);
            // Y 키: 필살기 카메라 고정 켜고 끄기 (저장)
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && camLockKey != UnityEngine.InputSystem.Key.None && kb[camLockKey].wasPressedThisFrame && !FxManager.Halt) ToggleCamLock();
            UpdateUi(udt, auto);
        }

        void ToggleCamLock()
        {
            bool on = !CamLock;
            PlayerPrefs.SetInt(CamLockPref, on ? 1 : 0); PlayerPrefs.Save();
            if (Cam && cur != null) { Cam.ultFollow = on; if (!on) Cam.ultZoom = 1; }
            string msg = on ? Loc.Get(camLockOnKey, camLockOn) : Loc.Get(camLockOffKey, camLockOff);
            if (!string.IsNullOrEmpty(msg)) Game?.ShowBanner(msg, "");
            SfxManager.Play("ui_click");
        }

        void StepCut(float udt)
        {
            var r = cur.R; if (!r) { Stop(cur, false); return; }
            cutT += udt;
            float k = cutT / cutTime, e = Ease(Mathf.Clamp01((k - 0.1f) / 0.6f));
            if (Cam && CamLock) { Cam.ultZoom = 1 + (cutZoom - 1) * Ease(Mathf.Min(1, k * 2.2f)); Cam.ultFocus = new Vector2(r.x, r.y); }
            r.UltJit = k > 0.15f ? 1 + 3 * k : 0;
            r.UltPose = new RatRig.Pose { tilt = -0.5f * e, front = 2.6f * e, farFront = 2.3f * e, back = -0.3f * e, farBack = 0.3f * e, head = -0.4f * e, tail = 1.3f * e, bob = 3 * e, sx = 1, sy = 1 - 0.1f * e };
            var fx = FxManager.I;
            if (fx && Mathf.Repeat(cutT, 0.3f - 0.15f * k) < udt && k > 0.15f) { fx.Ring(r.x, r.y, 50 + 50 * k, cur.U.Color, 0.3f); fx.Shake(0.02f + 0.04f * k); }
            if (cutBanner) { float bk = Ease(Mathf.Clamp01((cutT - 0.12f) / 0.22f)); cutBanner.anchoredPosition = new Vector2((1 - bk) * -2400, cutBanner.anchoredPosition.y); }
            if (cutLines) { cutLines.localRotation = Quaternion.Euler(0, 0, cutT * 25); cutLines.localScale = Vector3.one * (1.05f + 0.05f * Mathf.Sin(cutT * 40)); }
            if (cutIn)
            {
                cutIn.alpha = Mathf.Min(1, cutT / 0.15f);
                if (cutTitle)
                {
                    float tk = Mathf.Clamp01((cutT - 0.25f) / 0.14f), sc = cutT < 0.25f ? 0 : 1 + (1 - Ease(tk)) * 2;
                    cutTitle.transform.localScale = Vector3.one * sc;
                    cutTitle.transform.localRotation = Quaternion.Euler(0, 0, -3 + (cutT < 0.5f || k > 0.85f ? Random.Range(-1f, 1f) : 0));
                }
            }
            if (k > 0.85f) Flash(cur.U.Color, (k - 0.85f) / 0.15f * 0.35f);
            if (cutT >= cutTime) BeginAct(cur);
        }

        void StepAct(UltBase s, float dt)
        {
            var r = s.R;
            if (!r) { Stop(s, false); return; }
            s.T += dt;
            if (s == cur && Cam) { Cam.ultZoom = 1 + (Cam.ultZoom - 1) * Mathf.Max(0, 1 - dt * 6); if (CamLock) Cam.ultFocus = new Vector2(r.x, r.y); }
            s.RunBeats();
            r.vx = r.vy = 0;
            s.Step(dt, Mathf.Min(1, s.T / s.Dur));
            if (s != cur && !extras.Contains(s)) return;        // Step 안에서 끝남
            if ((s.ActorT -= dt) <= 0) { s.ActorT = 0.3f; Items.BlastActors(r.x, r.y, actorRadius, actorSpeed, UltDamage(r) * 0.4f, r); }
            if (s.T >= s.Dur) Stop(s, true);
        }

        void LateUpdate()
        {
            if (cur != null) foreach (var p in cur.Props) p.Apply();
            foreach (var e in extras) foreach (var p in e.Props) p.Apply();
        }

        void UpdateUi(float udt, bool auto)
        {
            float t = Time.unscaledTime;
            // 버튼: 게이지가 다 찬 쥐 한 마리마다 하나 (먼저 찬 쥐부터, 최대 maxButtons)
            if (bar && buttonTemplate)
            {
                for (int i = buttonOrder.Count - 1; i >= 0; i--)
                {
                    var r = buttonOrder[i];
                    if (r && r.temp <= 0 && Full(r)) continue;
                    if (buttons.TryGetValue(r, out var old) && old) Destroy(old.gameObject);
                    if (r) r.ultHover = false;
                    buttons.Remove(r); buttonOrder.RemoveAt(i);
                }
                foreach (var r in Rats.Rats)
                {
                    if (buttonOrder.Count >= maxButtons) break;
                    if (r.temp > 0 || buttons.ContainsKey(r) || !Full(r)) continue;
                    var b = Instantiate(buttonTemplate, bar); b.gameObject.SetActive(true); b.name = "Ult_" + r.codeId;
                    var rr = r;
                    b.Setup(UltIcon(DB.UltOf(r.Data)), r.Data.character_name, () => Request(rr));
                    buttons[r] = b; buttonOrder.Add(r);
                }
                foreach (var r in buttonOrder)
                {
                    var b = buttons[r];
                    b.Refresh(queue.Contains(r), auto);
                    r.ultHover = b.Hovered;
                }
            }
            // 자막
            Fade(captionBig, t - capBigT);
            Fade(captionSmall, t - capSmallT);
            // 번쩍
            if (flash)
            {
                flashA = Mathf.Max(0, flashA - udt * 1.6f);
                flash.enabled = flashA > 0.005f;
                var c = flashCol; c.a = flashA; flash.color = c;
            }
        }

        void Fade(TMP_Text txt, float age)
        {
            if (!txt) return;
            if (age > captionTime || age < 0) { if (txt.alpha > 0) txt.alpha = 0; return; }
            float sc = age < 0.12f ? 1.5f - age / 0.12f * 0.5f : 1, a = age > captionTime - 0.25f ? (captionTime - age) / 0.25f : 1;
            txt.alpha = a; txt.transform.localScale = Vector3.one * sc;
        }

        public Sprite UltIcon(RatUltimateRow u)
        {
            if (u == null) return null;
            string n = string.IsNullOrEmpty(u.ult_icon) ? "" : u.ult_icon.Substring(u.ult_icon.LastIndexOf('/') + 1);
            foreach (var i in icons) if (i.name == n) return i.sprite;
            return null;
        }

        static float Ease(float k) => 1 - (1 - k) * (1 - k) * (1 - k);

#if UNITY_EDITOR
        [ContextMenu("Fill Icons")]
        void FillIcons()
        {
            icons.Clear();
            foreach (var g in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art/Rats/UltIcons" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g); var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s) icons.Add(new FxManager.NamedSprite { name = System.IO.Path.GetFileNameWithoutExtension(p), sprite = s });
            }
            EditorUtility.SetDirty(this);
        }
        [ContextMenu("Fill Props")]
        void FillProps()
        {
            props.Clear();
            foreach (var dir in new[] { "Assets/Art/Rats/FX", "Assets/Art/Rats/FX_Meteor", "Assets/Art/Rats/UltProps", "Assets/Art/Rats/Parody", "Assets/Art/Rats/NewRats", "Assets/Art/Rats/Props", "Assets/Art/Rats/FrontRig", "Assets/Art/FX/Tint" })
            {
                if (!AssetDatabase.IsValidFolder(dir)) continue;
                foreach (var g in AssetDatabase.FindAssets("t:Sprite", new[] { dir }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g); var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    string n = System.IO.Path.GetFileNameWithoutExtension(p);
                    if (s && !props.Exists(o => o.name == n)) props.Add(new FxManager.NamedSprite { name = n, sprite = s });
                }
            }
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
