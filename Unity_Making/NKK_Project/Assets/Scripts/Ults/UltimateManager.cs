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
    // · 게이지는 종마다 따로: 그 종의 쥐가 Ult_Charge 시트 조건(물건·벽 파괴, 액션, 번식, 사람·고양이 퇴치, 층 통과…)을 하면 참.
    // · 요구량 = 쥐 테이블 Ultimate.ult_gauge ÷ 공용 스킬 '필살기 연습' 배율. 다 차면 하단에 그 종 버튼이 뜸.
    // · 버튼을 누르거나(또는 '필살기 자동 사용' 공용 스킬) → 대기열. 필살기는 한 번에 하나: 앞 필살기가 끝나야 다음 것.
    // · 진행: ① 컷인 (cutTime 초, 화면 정지 · 확대 · 제목) → ② 상황극 (UltXxx.Dur 초, 게임 진행) → ③ 업적 알림
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
        [Tooltip("업적 알림 패널 (오른쪽에서 밀려 들어옴)")] public RectTransform achvPanel;
        [Tooltip("업적 이름 (자리: {achv})")] public TMP_Text achvTitle;
        [Tooltip("처음 달성일 때만 보이는 표시 (NEW)")] public GameObject achvNew;
        public float achvTime = 3.2f;
        [Tooltip("끝날 때 휘말린 사람 수 팝업 (자리: {n}). 비우면 안 띄움")] public string blastPopup = "";

        [Header("소품 그림 (UltXxx 의 Prop 이름) — 컴포넌트 메뉴 Fill Props")]
        public List<FxManager.NamedSprite> props = new();
        public Transform propRoot;
        [Tooltip("소품 정렬 레이어·재질 기준 (쥐덫 등 월드 스프라이트)")] public SpriteRenderer propTemplate;

        [Header("수치 (웹게임 기준)")]
        [Tooltip("컷인 시간 (초)")] public float cutTime = 1.4f;
        [Tooltip("컷인 확대")] public float cutZoom = 1.6f;
        [Tooltip("필살기 반경 (끝날 때 휘말림)")] public float ultRadius = 560;
        [Tooltip("필살기 피해 = 공격력 × 이 값 (ultD)")] public float ultDamageK = 25;
        [Tooltip("물건 하나 피해 = 공격력 × 이 값 (ultItemD)")] public float ultItemK = 8;
        [Tooltip("상황극 중 주변 사람·고양이 휘말림 반경 · 속도 (0.3초마다)")] public float actorRadius = 220, actorSpeed = 620;
        [Tooltip("테스트: 시작부터 모든 게이지를 채움")] public bool testFullGauge;

        // ── 상태 ──
        readonly Dictionary<string, float> gauge = new(), clues = new();
        readonly List<string> queue = new();
        readonly Dictionary<string, UltButton> buttons = new();
        UltBase cur; bool cutPhase; float cutT, actorT, flashA; Color flashCol;
        float capBigT = -9, capSmallT = -9, achvT = -9;

        public bool Busy => cur != null;
        public UltBase Current => cur;
        static GameDatabase DB => GameDatabase.Instance;

        void Awake()
        {
            if (buttonTemplate) buttonTemplate.gameObject.SetActive(false);
            if (cutIn) { cutIn.alpha = 0; cutIn.gameObject.SetActive(false); }
            if (captionBig) captionBig.text = ""; if (captionSmall) captionSmall.text = "";
            if (achvPanel) achvPanel.gameObject.SetActive(false);
            if (flash) flash.enabled = false;
        }

        // ── 게이지 ──
        public float Need(string code)
        {
            var u = DB.RatsByCode.TryGetValue(code, out var row) ? DB.UltOf(row) : null;
            return u == null ? 0 : Mathf.Max(1, u.ult_gauge);
        }
        public float Gauge(string code) => gauge.TryGetValue(code, out var g) ? g : 0;
        public float Gauge01(string code) { float n = Need(code); return n > 0 ? Mathf.Clamp01(Gauge(code) / n) : 0; }
        public bool Full(string code) { float n = Need(code); return n > 0 && Gauge(code) >= n; }

        public void Charge(Rat r, CondType c)
        {
            if (!r || r.temp > 0 || DB == null || DB.UltOf(r.Data) == null) return;
            if (!DB.UltCharges.TryGetValue(c, out var g) || g <= 0) return;
            if (cur != null && cur.R && cur.R.codeId == r.codeId) return;      // 쓰는 중엔 안 참
            Add(r.codeId, g * CommonSkill.UltGaugeMul);
        }
        // 무리에 있는 종 전부 (층 통과 등)
        public void ChargeAll(CondType c)
        {
            if (!DB.UltCharges.TryGetValue(c, out var g) || g <= 0) return;
            var done = new HashSet<string>();
            foreach (var r in Rats.Rats) if (r.temp <= 0 && DB.UltOf(r.Data) != null && done.Add(r.codeId)) Add(r.codeId, g * CommonSkill.UltGaugeMul);
        }
        void Add(string code, float v) { float n = Need(code); if (n <= 0) return; gauge[code] = Mathf.Min(n, Gauge(code) + v); }

        // 단서 (찍찍 탐정 패시브) — 종 전체가 같이 모음, 필살기 쓰면 0
        public void AddClues(string code, float v) => clues[code] = Clues(code) + v;
        public float Clues(string code) => clues.TryGetValue(code, out var c) ? c : 0;

        // ── 사용 ──
        public void Request(string code)
        {
            if (GameOver.Active || !Full(code) || queue.Contains(code) || (cur != null && cur.R && cur.R.codeId == code)) return;
            queue.Add(code);
        }

        // 테스트 버튼: 게이지 채우고 바로 사용. 그 종 쥐가 화면에 없으면 화면 가운데 근처에 15초짜리로 불러옴
        public bool TestUlt(string code)
        {
            if (cur != null || (SuperJump && SuperJump.Busy) || !DB.RatsByCode.TryGetValue(code, out var row) || DB.UltOf(row) == null) return false;
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
            gauge[code] = Need(code);
            queue.Remove(code);
            return TryStart(code, r);
        }

        public void CancelAll()
        {
            queue.Clear();
            if (cur != null) Stop(false);
        }

        Rat PickRat(string code)
        {
            Rat any = null;
            foreach (var r in Rats.Rats)
            {
                if (r.codeId != code || r.temp > 0 || r.UltOn) continue;
                if (r.OnScreen(-0.05f)) return r;
                any ??= r;
            }
            return any;
        }

        bool TryStart(string code, Rat forced = null)
        {
            var r = forced ? forced : PickRat(code);
            if (!r) return false;
            var u = DB.UltOf(r.Data);
            var type = u != null && !string.IsNullOrEmpty(u.script) ? System.Type.GetType("NKK.Ults." + u.script) : null;
            if (type == null) { Debug.LogWarning($"[UltimateManager] 필살기 코드 없음: {u?.script} ({code})"); gauge[code] = 0; return false; }
            cur = (UltBase)System.Activator.CreateInstance(type);
            cur.Setup(this, r, u);
            gauge[code] = 0;
            r.UltGrab(); r.z = 0; r.vz = 0;
            cur.Pre();
            cutPhase = true; cutT = 0; actorT = 0.3f;
            FxManager.UltFreeze = true;
            if (Cam) { Cam.ultFollow = true; Cam.ultFocus = new Vector2(r.x, r.y); }
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
        string subFormat, achvFormat;

        // 컷인 끝 → 상황극 시작
        void BeginAct()
        {
            cutPhase = false; FxManager.UltFreeze = false;
            var r = cur.R;
            r.UltPose = null; r.UltJit = 0;
            if (cutIn) cutIn.gameObject.SetActive(false);
            var fx = FxManager.I;
            if (fx)
            {
                if (!string.IsNullOrEmpty(cur.U.ult_line)) fx.Popup(r.x, r.y, cur.U.ult_line, Color.white, 24, 1.6f, 80);
                fx.Ring(r.x, r.y, 140, Color.white, 0.5f); fx.Ring(r.x, r.y, 220, cur.U.Color, 0.6f); fx.Dust(r.x, r.y, 14, 1.8f); fx.Shake(0.3f);
            }
            Flash(Color.white, 0.4f);
            cur.Begin();
        }

        // 끝 (done = 끝까지 함 → 마무리 휘말림 + 업적)
        void Stop(bool done)
        {
            var s = cur; cur = null; cutPhase = false; FxManager.UltFreeze = false;
            if (cutIn) cutIn.gameObject.SetActive(false);
            if (Cam) { Cam.ultFollow = false; Cam.ultZoom = 1; }
            if (s == null) return;
            var r = s.R;
            if (done)
            {
                s.Finish();
                if (r)
                {
                    int n = Items.BlastActors(r.x, r.y, ultRadius, 700, UltDamage(r) * 3, r);
                    if (n > 0 && !string.IsNullOrEmpty(blastPopup) && OnScreen(r.x, r.y)) FxManager.I?.Popup(r.x, r.y, blastPopup.Replace("{n}", n.ToString()), new Color(1, 0.95f, 0.75f), 22, 1.1f, 120);
                }
                bool first = Progress.I && Progress.I.OnAchievement(s.U.ultimate_id);
                ShowAchievement(s.U.ult_achv, first);
            }
            s.Cleanup();
            s.ReleaseAll();
            if (r)
            {
                if (r.CollectsClues) clues[r.codeId] = 0;
                r.z = Mathf.Max(0, r.z);
                r.UltRelease();
                if (!Stage.Open.Contains(StageManager.RoomOf(r.x, r.y))) { r.x = s.X0; r.y = s.Y0; }
            }
        }

        public float UltDamage(Rat r) => r.Damage * ultDamageK * CommonSkill.UltPowerMul;
        public float UltItemDamage(Rat r) => r.Damage * ultItemK * CommonSkill.UltPowerMul;

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
        void ShowAchievement(string achv, bool first)
        {
            if (!achvPanel || string.IsNullOrEmpty(achv)) return;
            achvPanel.gameObject.SetActive(true);
            if (achvNew) achvNew.SetActive(first);
            if (achvTitle) { achvFormat ??= achvTitle.text; achvTitle.text = achvFormat.Contains("{achv}") ? achvFormat.Replace("{achv}", achv) : achv; }
            achvT = Time.unscaledTime;
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

        void Update()
        {
            if (DB == null) return;
            float udt = Time.unscaledDeltaTime, dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (testFullGauge) foreach (var r in Rats.Rats) if (DB.UltOf(r.Data) != null && !gauge.ContainsKey(r.codeId)) gauge[r.codeId] = Need(r.codeId);
            // 자동 사용 (공용 스킬)
            bool auto = CommonSkill.UltAuto;
            if (auto) foreach (var kv in new List<string>(gauge.Keys)) if (Full(kv) && HasRat(kv)) Request(kv);
            // 대기열 → 하나씩
            if (cur == null && !FxManager.Paused && !(SuperJump && SuperJump.Busy))
                while (queue.Count > 0) { var code = queue[0]; queue.RemoveAt(0); if (Full(code) && TryStart(code)) break; }

            if (cur != null && cutPhase) StepCut(udt);
            else if (cur != null) StepAct(dt);
            UpdateUi(udt, auto);
        }
        bool HasRat(string code) { foreach (var r in Rats.Rats) if (r.codeId == code && r.temp <= 0) return true; return false; }

        void StepCut(float udt)
        {
            var r = cur.R; if (!r) { Stop(false); return; }
            cutT += udt;
            float k = cutT / cutTime, e = Ease(Mathf.Clamp01((k - 0.1f) / 0.6f));
            if (Cam) { Cam.ultZoom = 1 + (cutZoom - 1) * Ease(Mathf.Min(1, k * 2.2f)); Cam.ultFocus = new Vector2(r.x, r.y); }
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
            if (cutT >= cutTime) BeginAct();
        }

        void StepAct(float dt)
        {
            var s = cur; var r = s.R;
            if (!r) { Stop(false); return; }
            s.T += dt;
            if (Cam) { Cam.ultZoom = 1 + (Cam.ultZoom - 1) * Mathf.Max(0, 1 - dt * 6); Cam.ultFocus = new Vector2(r.x, r.y); }
            s.RunBeats();
            r.vx = r.vy = 0;
            s.Step(dt, Mathf.Min(1, s.T / s.Dur));
            if (cur != s) return;
            if ((actorT -= dt) <= 0) { actorT = 0.3f; Items.BlastActors(r.x, r.y, actorRadius, actorSpeed, UltDamage(r) * 0.4f, r); }
            if (s.T >= s.Dur) Stop(true);
        }

        void LateUpdate() { if (cur != null) foreach (var p in cur.Props) p.Apply(); }

        void UpdateUi(float udt, bool auto)
        {
            float t = Time.unscaledTime;
            // 버튼: 다 찼고 그 종 쥐가 있으면 보임
            if (bar && buttonTemplate)
            {
                foreach (var kv in gauge)
                {
                    string code = kv.Key;
                    bool show = Full(code) && HasRat(code);
                    buttons.TryGetValue(code, out var b);
                    if (show && !b)
                    {
                        b = Instantiate(buttonTemplate, bar); b.gameObject.SetActive(true); b.name = "Ult_" + code;
                        var row = DB.RatsByCode[code]; var u = DB.UltOf(row);
                        b.Setup(code, Icon(u), row.character_name, Request);
                        buttons[code] = b;
                    }
                    else if (!show && b) { Destroy(b.gameObject); buttons.Remove(code); }
                    if (show && b) b.Refresh(queue.Contains(code) || (cur != null && cur.R && cur.R.codeId == code), auto);
                }
            }
            // 자막
            Fade(captionBig, t - capBigT);
            Fade(captionSmall, t - capSmallT);
            // 업적
            if (achvPanel && achvPanel.gameObject.activeSelf)
            {
                float age = t - achvT;
                if (age > achvTime) achvPanel.gameObject.SetActive(false);
                else
                {
                    float slide = age < 0.3f ? Ease(age / 0.3f) : age > achvTime - 0.3f ? 1 - (age - achvTime + 0.3f) / 0.3f : 1;
                    var p = achvPanel.anchoredPosition; achvPanel.anchoredPosition = new Vector2(achvY0 + (1 - slide) * (achvPanel.rect.width + 80), p.y);   // 오른쪽에서 밀려 들어옴
                }
            }
            // 번쩍
            if (flash)
            {
                flashA = Mathf.Max(0, flashA - udt * 1.6f);
                flash.enabled = flashA > 0.005f;
                var c = flashCol; c.a = flashA; flash.color = c;
            }
        }
        float achvY0 { get { if (!achvYSet && achvPanel) { achvYCache = achvPanel.anchoredPosition.x; achvYSet = true; } return achvYCache; } }
        float achvYCache; bool achvYSet;

        void Fade(TMP_Text txt, float age)
        {
            if (!txt) return;
            if (age > captionTime || age < 0) { if (txt.alpha > 0) txt.alpha = 0; return; }
            float sc = age < 0.12f ? 1.5f - age / 0.12f * 0.5f : 1, a = age > captionTime - 0.25f ? (captionTime - age) / 0.25f : 1;
            txt.alpha = a; txt.transform.localScale = Vector3.one * sc;
        }

        Sprite Icon(RatUltimateRow u)
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
