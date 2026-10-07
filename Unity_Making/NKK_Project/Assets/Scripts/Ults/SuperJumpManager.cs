using System.Collections.Generic;
using NKK.Humans;
using NKK.Items;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Ults
{
    // 슈퍼 점프 (웹게임 superjump · updateSuperJump/drawSuperJump): 아주 낮은 확률로 화면 속 쥐 한 마리가 발동.
    // ① 컷인 → ② 기 모으기 → ③ 하늘 끝까지 슈웅 → ④ 머리부터 내리꽂혀 쾅! → ⑤ 화면 속 쥐·물건·사람이 전부 둥실 → ⑥ 한꺼번에 박살 (벽도) → ⑦ 착지
    // 진행 중엔 게임 세계가 멈춤 (FxManager.WorldFreeze, 박살 뒤 풀림). 필살기와 동시에 안 함. 글자는 전부 씬 TMP / 인스펙터.
    // 화면 좌표는 웹 캔버스(1280×720) × 1.5 = HUD 1920×1080 기준.
    public class SuperJumpManager : MonoBehaviour
    {
        [Header("연결")]
        public RatManager Rats;
        public ItemManager Items;
        public NKK.Stage.StageManager Stage;
        public GameManager Game;
        public CameraController Cam;
        public UltimateManager Ults;

        [Header("발동 (웹게임 기준)")]
        [Tooltip("화면에 쥐가 있을 때 초당 확률 (평균 8분에 한 번)")] public float chancePerSec = 1f / 480;
        [Tooltip("한 번 터지면 쉬는 시간 · 첫 발동까지 (초)")] public float cool = 120, firstCool = 60;
        [Tooltip("박살 피해 = 쥐 공격력 × 이 값 (화면 속 물건은 체력과 상관없이 전부 박살)")] public float damageK = 15;
        [Tooltip("단계 시간 (초): 컷인 · 기 모으기 · 발사 · 하늘 · 낙하 · 둥실 · 착지")] public float tCut = 1.9f, tCharge = 1.5f, tLaunch = 0.35f, tSky = 1.3f, tFall = 0.38f, tFloat = 1.5f, tDrop = 1;

        [Header("화면 (HUD 자식) — 글은 씬 TMP")]
        public CanvasGroup cutIn;
        [Tooltip("컷인 어두운 막 (알파 0.6 까지)")] public Image cutDark;
        [Tooltip("비스듬한 만화 칸 (왼쪽에서 휙 → 오른쪽으로 휙)")] public RectTransform cutBand;
        [Tooltip("집중선 (Image). 0.05초마다 그림·각도·뒤집기를 새로 뽑아 깜빡임")] public RectTransform cutLines;
        [Tooltip("집중선 그림 여러 장 (비우면 지금 그림을 돌리기만)")] public Sprite[] cutLineFrames;
        [Tooltip("큰 글씨 '슈퍼 점프!!!' (Sub 는 이 자식으로 두면 같이 커짐)")] public RectTransform cutTitle;
        [Tooltip("'{grade} · {name}'")] public TMP_Text cutSub;
        [Tooltip("'두둥!'")] public RectTransform cutDudung;
        [Tooltip("기 모으는 중 글 (자리: {dots})")] public TMP_Text chargeText;
        [Tooltip("기 모을 때 화면 가장자리 노란 빛 (가운데 투명 원형 그림)")] public Image chargeGlow;
        [Tooltip("하늘 단계 글")] public CanvasGroup skyText;
        [Tooltip("착지 '쿠과과광!!!' (뒤에 터짐 그림 포함)")] public RectTransform impactText;
        [Tooltip("박살 '전부 박살!!!'")] public RectTransform boomText;
        public Image flash;

        [Header("컷인 속 쥐 (전용 카메라 → RenderTexture → HUD RawImage)")]
        [Tooltip("CutIn 안, Band 바로 아래 형제 RawImage. 피벗 = 쥐 발끝, 위치 = 웅크린 자리")] public RawImage cutRat;
        [Tooltip("비워 두면 실행 중 만듦")] public Camera cutRatCam;
        [Tooltip("쥐 리그를 놓을 곳 (유니티 좌표, 게임 화면과 먼 곳)")] public Vector2 cutRigOrigin = new(10000, 10000);
        [Tooltip("RawImage 1 픽셀당 해상도: 유니티 1 유닛 = 이 HUD 픽셀")] public float cutPxPerUnit = 500;
        [Tooltip("쥐 크기 (웹 ×5.2 = 리그 1배 크기의 5.2배, 1280 캔버스 기준)")] public float cutRatZoom = 5.2f;
        [Tooltip("RenderTexture 가로 픽셀")] public int cutTexWidth = 640;
        [Tooltip("뛰어오르는 높이 (HUD 픽셀, 웹 H×0.32) · 부들부들 폭")] public float cutRatJump = 345, cutRatJit = 4.5f;
        [Tooltip("뛰어오를 때 발밑 선 색")] public Color cutLineColor = new(0.24f, 0.2f, 0.18f, 0.6f);

        [Header("이펙트 그림 (Assets/Art/Rats/FX_SuperJump)")]
        [Tooltip("흰 동그라미 (기 모으기 불꽃 · 발사/낙하 꼬리)")] public Sprite dotSprite;
        [Tooltip("흰 별 (착지 때 튀는 별, 색은 코드)")] public Sprite starSprite;
        [Tooltip("흰 4각 반짝 (하늘 끝 반짝)")] public Sprite sparkleSprite;
        [Tooltip("흰 직선 (낙하 속도선 · 컷인 발밑 선)")] public Sprite lineSprite;
        [Tooltip("바닥 자국: 움푹 + 갈라진 금 (착지)")] public Sprite stampSprite;
        [Tooltip("비우면 FxManager 고리 템플릿 재질")] public Material fxMaterial;
        [Tooltip("바닥 고리·자국 정렬 · 하늘 효과 정렬")] public int groundOrder = -28900, airOrder = 32100;
        [Tooltip("바닥 자국 남는 시간 (초)")] public float stampLife = 25;

        [Header("글 (인스펙터)")]
        [Tooltip("기 모으기 시작 때 쥐가 외침")] public string shout = "";
        [Tooltip("결과 배너 · 부제 (자리: {n} 물건, {w} 벽)")] public string resultTitle = "", resultSub = "", resultSubWalls = "";

        enum Phase { None, Cut, Charge, Launch, Sky, Fall, Float, Drop }
        Phase ph; float t, coolT, flashA, impactT = -9, boomT = -9; Color flashCol;
        Rat r; float x0, y0;
        class Floater { public Rat r; public Item it; public Human h; public float z0, hgt, d, ph, vr, vz; public bool land; }
        readonly List<Floater> fl = new();
        List<(int i, int j, int di, int dj)> walls = new();
        public bool Busy => ph != Phase.None;

        static readonly Color Cream = new(1, 0.953f, 0.749f), Gold = new(0.941f, 0.784f, 0.471f), Gold9 = new(0.941f, 0.784f, 0.471f, 0.9f), Gold95 = new(0.941f, 0.784f, 0.471f, 0.95f);
        static readonly Color[] Sparks = { Cream, Gold, Color.white };
        Vector2 titleBase, impactBase; float lineT = -9;

        void Awake()
        {
            coolT = firstCool; HideAll();
            if (cutRat) cutRatBase = cutRat.rectTransform.anchoredPosition;
            if (cutTitle) titleBase = cutTitle.anchoredPosition;
            if (impactText) impactBase = impactText.anchoredPosition;
        }
        void OnDestroy() { if (cutTex) cutTex.Release(); }
        void HideAll()
        {
            if (cutIn) cutIn.gameObject.SetActive(false);
            foreach (var g in new Component[] { chargeText, chargeGlow, skyText, impactText, boomText }) if (g) g.gameObject.SetActive(false);
            if (flash) flash.enabled = false;
            if (skyStar) skyStar.enabled = false;
            foreach (var l in fallLines) l.enabled = false;
        }

        // 테스트 버튼: 가장자리 쪽 쥐로 (카메라가 데려오는 걸 보이게)
        public bool Trigger(bool forced)
        {
            if (Busy || (Ults && Ults.Busy) || FxManager.Paused) return false;
            var vr = Ults ? Ults.ViewRect(0) : new Rect();
            var pool = new List<Rat>();
            foreach (var o in Rats.Rats) if (o.temp <= 0 && !o.UltOn && o.OnScreen(-0.05f)) pool.Add(o);
            if (pool.Count == 0) return false;
            Vector2 c = vr.center;
            pool.Sort((a, b) => Vector2.Distance(new Vector2(a.x, a.y), c).CompareTo(Vector2.Distance(new Vector2(b.x, b.y), c)));
            r = forced ? pool[Random.Range(pool.Count / 2, pool.Count)] : pool[Random.Range(0, Mathf.Max(1, Mathf.CeilToInt(pool.Count / 3f)))];
            foreach (var o in Rats.Rats) if (o.Acting) o.UltGrab();      // 하던 액션 끝내기
            foreach (var o in Rats.Rats) if (o.UltOn) o.UltRelease();
            r.UltGrab(); r.z = 0; x0 = r.x; y0 = r.y;
            fl.Clear(); walls.Clear();
            coolT = cool; Go(Phase.Cut); lineT = -9;
            FxManager.WorldFreeze = true;
            if (Cam) { Cam.ultFollow = true; Cam.ultFocus = new Vector2(r.x, r.y); }
            if (cutIn) { cutIn.gameObject.SetActive(true); cutIn.alpha = 1; }
            MakeCutRat();
            if (cutSub) { subFmt ??= cutSub.text; cutSub.text = subFmt.Replace("{grade}", r.GradeData.grade_name).Replace("{name}", r.Data.character_name); }
            UpdateUi(0);
            Flash(Color.white, 0.5f);
            return true;
        }
        string subFmt, chargeFmt;
        void Go(Phase p) { ph = p; t = 0; }
        void Flash(Color c, float a) { flashCol = c; flashA = Mathf.Max(flashA, a); }
        static float Ease(float k) { k = Mathf.Clamp01(k); return 1 - (1 - k) * (1 - k) * (1 - k); }
        static RatRig.Pose P(float head = 0, float tail = 0, float front = 0, float back = 0, float farFront = 0, float farBack = 0, float tilt = 0, float bob = 0, float sx = 1, float sy = 1)
            => new RatRig.Pose { head = head, tail = tail, front = front, back = back, farFront = farFront, farBack = farBack, tilt = tilt, bob = bob, sx = sx, sy = sy };

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            UpdateFx(dt);
            if (!Busy)
            {
                UpdateUi(dt);
                if (FxManager.Paused || (Ults && Ults.Busy)) return;
                if ((coolT -= Time.deltaTime) <= 0 && Random.value < chancePerSec * CommonSkill.SuperJumpMul * Time.deltaTime) Trigger(false);
                return;
            }
            if (!r) { End(); return; }
            t += dt;
            var fx = FxManager.I;
            if (Cam) { Cam.ultFocus = new Vector2(r.x, r.y - Mathf.Min(r.z, 120) / World.TILT * 0.2f); }
            switch (ph)
            {
                case Phase.Cut:
                    r.UltPose = P();
                    if (t > 0.3f && !slam) { slam = true; fx?.Shake(0.25f); }
                    if (t >= tCut)
                    {
                        Go(Phase.Charge); slam = false;
                        if (cutIn) cutIn.gameObject.SetActive(false);
                        DropCutRat();
                        if (!string.IsNullOrEmpty(shout)) fx?.Popup(r.x, r.y, shout, Cream, 26, 1.4f, 70);
                        if (chargeText) { chargeText.gameObject.SetActive(true); chargeFmt ??= chargeText.text; }
                        if (chargeGlow) chargeGlow.gameObject.SetActive(true);
                    }
                    break;
                case Phase.Charge:
                {
                    // 웅크리고 부들부들 + 기가 빨려 들어옴
                    float k = t / tCharge, e = Mathf.Min(1, k * 2);
                    if (Cam) Cam.ultZoom = 1 + 0.55f * Ease(k * 1.5f);
                    r.UltPose = P(front: 0.55f * e, farFront: 0.5f * e, back: -0.6f * e, farBack: -0.55f * e, bob: 6 * e, sy: 1 - 0.25f * e, sx: 1 + 0.12f * e, head: 0.3f * e, tail: -0.4f * e + Mathf.Sin(Time.unscaledTime * 40) * 0.1f);
                    r.UltJit = 1.5f + 3 * k;
                    for (sparkAcc += dt * 300; sparkAcc >= 1; sparkAcc--)      // 웹: 프레임마다 5개 (60fps)
                    {
                        float a = Random.Range(0, Mathf.PI * 2), d = Random.Range(120f, 230f), life = Random.Range(0.28f, 0.4f);
                        var f = Spawn(Kind.Spark, dotSprite, r.x + Mathf.Cos(a) * d, r.y + Mathf.Sin(a) * d, Random.Range(5f, 60f), Random.Range(6f, 12f), life, Sparks[Random.Range(0, 3)]);
                        if (f != null) { f.vx = -Mathf.Cos(a) * d / life; f.vy = -Mathf.Sin(a) * d / life; }
                    }
                    if ((beat -= dt) <= 0) { beat = 0.34f - 0.2f * k; Ring(r.x, r.y, 60 + 60 * k, Gold9, 0.3f, 5); fx?.Shake(0.02f + 0.05f * k); }
                    if (chargeText) chargeText.text = chargeFmt.Replace("{dots}", new string('.', 1 + Mathf.FloorToInt(k * 6) % 4));
                    if (chargeGlow) { var c = chargeGlow.color; c.a = 0.25f + 0.3f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (10 + 20 * k))) * k; chargeGlow.color = c; }
                    if (k >= 1)
                    {
                        Go(Phase.Launch); r.UltJit = 0;
                        if (chargeText) chargeText.gameObject.SetActive(false);
                        if (chargeGlow) chargeGlow.gameObject.SetActive(false);
                        Ring(r.x, r.y, 140, Color.white, 0.5f, 12); Ring(r.x, r.y, 220, Gold9, 0.6f, 8);
                        fx?.Dust(r.x, r.y, 18, 2); fx?.Shake(0.4f); Flash(Cream, 0.35f);
                        Stamp(dotSprite, r.x, r.y, 92, 68, new Color(0.235f, 0.196f, 0.176f, 0.35f));
                    }
                    break;
                }
                case Phase.Launch:
                {
                    float k = t / tLaunch;
                    if (Cam) Cam.ultZoom = 1 + 0.55f * (1 - Ease(k));
                    r.z = k * k * 2400;
                    r.UltPose = P(front: 2.5f, farFront: 2.3f, back: -1.7f, farBack: -1.5f, head: -0.5f, tail: -1.4f, sx: 0.78f, sy: 1.4f);
                    Spawn(Kind.Trail, dotSprite, r.x, r.y, r.z * 0.6f, 24, 0.35f, new Color(1, 0.953f, 0.749f, 0.9f));
                    if (k >= 1) { Go(Phase.Sky); r.z = 2600; if (skyText) { skyText.gameObject.SetActive(true); skyText.alpha = 0; } }
                    break;
                }
                case Phase.Sky:
                    if (Cam) Cam.ultZoom = 1;
                    if (skyText) skyText.alpha = Mathf.Clamp01(t / 0.3f);
                    SkyStar();
                    if (t >= tSky) { Go(Phase.Fall); if (skyText) skyText.gameObject.SetActive(false); if (skyStar) skyStar.enabled = false; }
                    break;
                case Phase.Fall:
                {
                    // 머리부터 거꾸로 내리꽂힘
                    float k = t / tFall;
                    r.z = 2600 * (1 - k * k);
                    r.UltRot = -r.face * 2.6f;
                    r.UltPose = P(front: 2.6f, farFront: 2.4f, back: -2.2f, farBack: -2, head: -0.6f, tail: 1.4f, sx: 0.85f, sy: 1.3f);
                    Spawn(Kind.Trail, dotSprite, r.x, r.y, r.z + 20, 28, 0.3f, new Color(1, 1, 1, 0.9f));
                    FallLines(true);
                    if (k >= 1) { FallLines(false); Impact(); }
                    break;
                }
                case Phase.Float:
                {
                    // 화면 속 모든 것이 둥실 (착지 지점부터 바깥으로 물결처럼)
                    float k = t / tFloat, tt = Time.unscaledTime;
                    foreach (var f in fl)
                    {
                        float e = Ease((t - f.d) / 0.6f);
                        if (f.it) { f.it.z = f.z0 + f.hgt * e + Mathf.Sin(tt * 3 + f.ph) * 6 * e; f.it.Rot += f.vr * dt * e; }
                        else if (f.r) { f.r.z = f.hgt * e + Mathf.Sin(tt * 3 + f.ph) * 5 * e; f.r.UltPose = P(front: Mathf.Sin(tt * 14 + f.ph) * 1.4f, farFront: Mathf.Cos(tt * 12 + f.ph) * 1.4f, back: Mathf.Sin(tt * 13 + f.ph + 2) * 1.3f, farBack: Mathf.Cos(tt * 11 + f.ph) * 1.3f, head: -0.3f, tail: 1.2f + Mathf.Sin(tt * 9) * 0.3f); f.r.UltRot = Mathf.Sin(tt * 2 + f.ph) * 0.5f * e; }
                        else if (f.h) { f.h.z = f.z0 + f.hgt * e + Mathf.Sin(tt * 3 + f.ph) * 6 * e; }
                    }
                    foreach (var w in walls) Stage.ShakeWall(w.i, w.j, w.di, w.dj);
                    fx?.Shake(0.02f);
                    if (k >= 1) Boom();
                    break;
                }
                case Phase.Drop:
                {
                    foreach (var f in fl)
                    {
                        if (!f.r || f.land) continue;
                        var o = f.r;
                        f.vz -= 1400 * dt; o.z = Mathf.Max(0, o.z + f.vz * dt);
                        if (o.z <= 0) { if (f.vz < -300) { f.vz *= -0.3f; fx?.Dust(o.x, o.y, 2, 0.5f); } else { f.land = true; o.UltRelease(); } }
                        else o.UltRot *= 0.9f;
                    }
                    // 발동한 쥐: 짠! 승리 포즈
                    r.UltRot = 0;
                    r.UltPose = t < 0.6f ? (RatRig.Pose?)P(front: 2.6f, farFront: 2.3f, back: -0.2f, farBack: 0.2f, head: -0.35f, tail: 1.3f, tilt: -0.35f) : (RatRig.Pose?)null;
                    if (t >= tDrop) End();
                    break;
                }
            }
            UpdateUi(dt);
        }
        bool slam; float beat, sparkAcc;

        void Impact()
        {
            r.z = 0; r.UltRot = 0;
            r.UltPose = P(front: 1.2f, farFront: -0.6f, back: -1.8f, farBack: 1.4f, head: 0.5f, tail: 1.5f, bob: 5, sy: 0.8f, sx: 1.15f, tilt: 0.2f);   // 슈퍼히어로 착지
            Go(Phase.Float); impactT = Time.unscaledTime;
            var vr = Ults.ViewRect(0);
            foreach (var it in new List<Item>(Items.All))
            {
                if ((it.State != Item.ItemState.Rest && it.State != Item.ItemState.Fly) || !vr.Contains(new Vector2(it.x, it.y))) continue;
                it.Hold();
                fl.Add(new Floater { it = it, z0 = Mathf.Max(0, it.z), hgt = Random.Range(150f, 320f), d = Dist(it.x, it.y) / 1600, vr = Random.Range(-3f, 3f), ph = Random.Range(0f, 6f) });
            }
            foreach (var o in Rats.Rats)
            {
                if (o == r || !vr.Contains(new Vector2(o.x, o.y))) continue;
                o.UltGrab();
                fl.Add(new Floater { r = o, hgt = Random.Range(90f, 200f), d = Dist(o.x, o.y) / 1600, ph = Random.Range(0f, 6f) });
            }
            foreach (var h in Items.Humans)
                if (vr.Contains(new Vector2(h.x, h.y)) && h.State != Human.HState.Dead && h.State != Human.HState.Splat && h.State != Human.HState.Fly)
                    fl.Add(new Floater { h = h, z0 = Mathf.Max(0, h.z), hgt = Random.Range(110f, 210f), d = Dist(h.x, h.y) / 1600, ph = Random.Range(0f, 6f) });
            walls = Stage.WallsIn(vr);
            // 웹 sjImpact: 고리 5겹 · 별 40개 · 먼지 · 바닥 자국(움푹 + 금)
            for (int i = 0; i < 5; i++) Ring(r.x, r.y, 120 + i * 140, i % 2 == 1 ? Color.white : Gold95, 0.5f + i * 0.12f, 14 - i * 2);
            for (int i = 0; i < 40; i++)
            {
                float a = Random.Range(0, Mathf.PI * 2), sp = Random.Range(300f, 900f);
                var f = Spawn(Kind.Star, starSprite, r.x, r.y, 20, Random.Range(8f, 18f), Random.Range(0.35f, 0.8f), Sparks[Random.Range(0, 3)]);
                if (f == null) break;
                f.vx = Mathf.Cos(a) * sp; f.vy = Mathf.Sin(a) * sp; f.vz = Random.Range(-40f, 160f); f.vr = Random.Range(-10f, 10f) * Mathf.Rad2Deg;
            }
            var fx = FxManager.I;
            if (fx) { fx.Dust(r.x, r.y, 30, 3); fx.Shake(0.5f); }
            Stamp(stampSprite, r.x, r.y, 520, 400, new Color(1, 1, 1, 0.42f));      // 그림이 이미 어두운 색
            Flash(Color.white, 0.9f);
            if (impactText) impactText.gameObject.SetActive(true);
        }

        void Boom()
        {
            Go(Phase.Drop); boomT = Time.unscaledTime;
            FxManager.WorldFreeze = false;              // 날아간 물건·사람이 다시 움직이게
            float sjD = r.Damage * damageK;
            int n = 0;
            foreach (var f in fl)
            {
                if (f.it)
                {
                    var it = f.it; if (it.State != Item.ItemState.Held) continue;
                    float a = Random.Range(0, Mathf.PI * 2);
                    it.SkillHit(Mathf.Max(sjD, it.hp + it.hpMax), r);
                    it.Fling(Mathf.Cos(a) * 260, Mathf.Sin(a) * 260, 520); n++;
                }
                else if (f.h) { f.h.Blast(Random.Range(0, Mathf.PI * 2), Random.Range(300f, 520f), Mathf.Max(sjD * 2, f.h.hp + 1), r); f.h.vz = Random.Range(900f, 1200f); }
                else if (f.r) f.vz = 120;
            }
            var cat = Items.Cats ? Items.Cats.Current : null;
            if (cat && cat.Alive && Ults.ViewRect(0).Contains(new Vector2(cat.x, cat.y))) cat.Damage(Mathf.Max(sjD * 2, cat.hp + 1), Random.Range(0, Mathf.PI * 2), r);
            int nw = walls.Count;
            foreach (var w in walls) Stage.SuperBreakWall(w.i, w.j, w.di, w.dj, r);
            walls.Clear();
            if (impactText) impactText.gameObject.SetActive(false);
            if (boomText) boomText.gameObject.SetActive(true);
            FxManager.I?.Shake(0.5f); Flash(Color.white, 0.8f);
            int items = n;
            Rats.Later(0.9f, () => { if (!string.IsNullOrEmpty(resultTitle)) Game.ShowBanner(resultTitle, (nw > 0 ? resultSubWalls : resultSub).Replace("{n}", items.ToString()).Replace("{w}", nw.ToString())); });
            Progress.I?.Save();
        }

        void End()
        {
            foreach (var f in fl) if (f.r && f.r.UltOn) { f.r.z = 0; f.r.UltRelease(); }
            fl.Clear();
            if (r) { r.UltRelease(); }
            r = null; ph = Phase.None; FxManager.WorldFreeze = false;
            DropCutRat();
            if (Cam) { Cam.ultFollow = false; Cam.ultZoom = 1; }
            HideAll();
        }

        float Dist(float x, float y) => Mathf.Sqrt((x - r.x) * (x - r.x) + (y - r.y) * (y - r.y));

        // ── HUD 연출 (웹 drawSuperJump) ──
        void UpdateUi(float dt)
        {
            float now = Time.unscaledTime;
            if (cutIn && cutIn.gameObject.activeSelf && ph == Phase.Cut)
            {
                float inA = Mathf.Min(1, t / 0.12f), outK = Mathf.Clamp01((t - (tCut - 0.25f)) / 0.25f);
                if (cutDark) SetA(cutDark, 0.6f * inA * (1 - outK));
                // 집중선: 0.05초마다 새로 (그림·각도·뒤집기)
                if (cutLines)
                {
                    var img = cutLines.GetComponent<Image>();
                    if (now - lineT > 0.05f)
                    {
                        lineT = now;
                        if (img && cutLineFrames != null && cutLineFrames.Length > 0) img.sprite = cutLineFrames[Random.Range(0, cutLineFrames.Length)];
                        cutLines.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
                        cutLines.localScale = new Vector3(Random.value < 0.5f ? -1 : 1, 1, 1);
                    }
                    if (img) SetA(img, 0.7f * (1 - outK));
                }
                if (cutBand) cutBand.anchoredPosition = new Vector2((1 - Ease(t / 0.2f)) * -2304 + outK * outK * 2496, cutBand.anchoredPosition.y);
                CutRatStep();
                // 큰 글씨: 쾅 박히듯 (3.2 → 1), 처음 0.6초 흔들림
                float tk = Mathf.Clamp01((t - 0.25f) / 0.14f), sc = t < 0.25f ? 0 : 1 + (1 - Ease(tk)) * 2.2f;
                if (cutTitle)
                {
                    cutTitle.localScale = Vector3.one * sc;
                    float sh = t < 0.6f ? Random.Range(-6f, 6f) : 0;
                    cutTitle.anchoredPosition = titleBase + new Vector2(sh, -sh);
                    var tt = cutTitle.GetComponent<TMP_Text>(); if (tt) tt.alpha = 1 - outK;
                }
                if (cutSub) { cutSub.alpha = 1 - outK; if (cutTitle && cutSub.transform.parent != cutTitle) cutSub.transform.localScale = Vector3.one * sc; }
                if (cutDudung)
                {
                    cutDudung.localScale = Vector3.one * (t > 0.35f && sc > 0 ? 1 : 0);
                    var dd = cutDudung.GetComponent<TMP_Text>(); if (dd) dd.alpha = 1 - outK;
                }
            }
            // 쿠과과광: 2.6 → 1 (0.15초), 들어올 때 좌우 흔들림 · 전부 박살: 2 → 1 (0.12초)
            Pop(impactText, now - impactT, 1.3f, 2.6f, 0.15f);
            if (impactText && impactText.gameObject.activeSelf) impactText.anchoredPosition = impactBase + new Vector2(Random.Range(-7.5f, 7.5f) * (1 - Ease((now - impactT) / 0.15f)), 0);
            Pop(boomText, now - boomT, 1f, 2f, 0.12f);
            if (flash)
            {
                flashA = Mathf.Max(0, flashA - dt * 2.5f);
                flash.enabled = flashA > 0.005f; var c = flashCol; c.a = flashA; flash.color = c;
            }
        }
        static void SetA(Graphic g, float a) { var c = g.color; c.a = a; g.color = c; }
        // 쾅 박히는 글씨: 크게 → 제자리, 끝 0.3초 동안 사라짐
        static void Pop(RectTransform rt, float age, float life, float big, float inT)
        {
            if (!rt) return;
            if (age < 0 || age > life) { if (rt.gameObject.activeSelf && age > life) rt.gameObject.SetActive(false); return; }
            float e = Ease(age / inT);
            rt.localScale = Vector3.one * (big - (big - 1) * e);
            var cg = rt.GetComponent<CanvasGroup>(); if (cg) cg.alpha = age > life - 0.3f ? (life - age) / 0.3f : 1;
        }

        // ── 화면 픽셀(1920×1080 기준, 위에서부터) ↔ 월드 ──
        static float HudK => Screen.height / 1080f;
        static Vector3 Px2World(Camera cam, float x, float yTop) { var p = cam.ScreenToWorldPoint(new Vector3(x * HudK, Screen.height - yTop * HudK, 10)); p.z = 0; return p; }
        static Vector2 World2Px(Camera cam, Vector3 w) { var s = cam.WorldToScreenPoint(w); return new Vector2(s.x / HudK, (Screen.height - s.y) / HudK); }
        static float PxSize(Camera cam, float px) => 2 * cam.orthographicSize * px / 1080f;

        // 하늘 끝 반짝 (웹: 쥐 x, 화면 위 118 → 177px, 반지름 22 → 지름 66px, 초당 5 라디안 회전)
        SpriteRenderer skyStar;
        void SkyStar()
        {
            var cam = Camera.main; var sp = sparkleSprite ? sparkleSprite : starSprite;
            if (!cam || !sp) return;
            if (!skyStar) skyStar = NewSr("SJ_SkyStar");
            float tw = Mathf.Clamp01((t - 0.35f) / 0.5f), d = Mathf.Sin(tw * Mathf.PI) * 66;
            skyStar.enabled = d > 1.5f; if (!skyStar.enabled) return;
            skyStar.sprite = sp; skyStar.color = Color.white; skyStar.sortingOrder = airOrder + 2;
            float fx = World2Px(cam, World.ToUnity(r.x, r.y, 0)).x;
            skyStar.transform.position = Px2World(cam, fx, 177);
            skyStar.transform.rotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 5 * Mathf.Rad2Deg);
            float s = PxSize(cam, d) / sp.bounds.size.x; skyStar.transform.localScale = new Vector3(s, s, 1);
        }

        // 낙하: 쥐 둘레 ±390px 에 흰 세로줄 26개를 매 프레임 새로 (위 → 쥐 높이)
        readonly List<SpriteRenderer> fallLines = new();
        void FallLines(bool on)
        {
            var cam = Camera.main; var sp = lineSprite;
            if (!on || !cam || !sp) { foreach (var l in fallLines) l.enabled = false; return; }
            while (fallLines.Count < 26) fallLines.Add(NewSr("SJ_FallLine"));
            var rp = World2Px(cam, World.ToUnity(r.x, r.y, 0));
            float w = PxSize(cam, 6) / sp.bounds.size.x;
            foreach (var l in fallLines)
            {
                float x = rp.x + Random.Range(-390f, 390f), y0 = Random.Range(-60f, Mathf.Max(-60f, rp.y)), len = Random.Range(90f, 270f);
                l.enabled = true; l.sprite = sp; l.color = new Color(1, 1, 1, 0.55f); l.sortingOrder = airOrder + 1;
                l.transform.position = Px2World(cam, x, y0 + len / 2); l.transform.rotation = Quaternion.identity;
                l.transform.localScale = new Vector3(w, PxSize(cam, len) / sp.bounds.size.y, 1);
            }
        }

        SpriteRenderer NewSr(string n)
        {
            var sr = new GameObject(n).AddComponent<SpriteRenderer>();
            if (!fxRoot) fxRoot = new GameObject("SJ_Fx").transform;
            sr.transform.SetParent(fxRoot, false);
            if (FxMat) sr.sharedMaterial = FxMat;
            return sr;
        }

        // ── 컷인 속 쥐: 발동한 쥐를 먼 곳에 리그로 세우고 전용 카메라로 찍어 HUD RawImage(cutRat)에 띄움 ──
        // HUD 가 Screen Space-Overlay 라 월드 그림은 그 위에 못 올라감 → RenderTexture 로 HUD 안에 넣음 (Band 위 · 글씨 아래)
        Vector2 cutRatBase; RatRig cutRig; RenderTexture cutTex;
        readonly List<SpriteRenderer> cutLegLines = new();

        void MakeCutRat()
        {
            DropCutRat();
            if (!cutRat || !Rats || !Rats.ratPrefab || !Rats.artLibrary) return;
            var art = Rats.artLibrary.Get(r.codeId); if (art == null) return;
            var src = Rats.ratPrefab.rig;
            var go = new GameObject("SJ_CutRat"); go.SetActive(false);
            go.transform.position = cutRigOrigin;
            var rig = go.AddComponent<RatRig>();
            var vis = Instantiate(src.visual, go.transform, false);
            SpriteRenderer M(SpriteRenderer s) { var tr = Map(src.visual, vis, s ? s.transform : null); return tr ? tr.GetComponent<SpriteRenderer>() : null; }
            rig.visual = vis; rig.body = Map(src.visual, vis, src.body);
            rig.farBack = M(src.farBack); rig.farFront = M(src.farFront); rig.tail = M(src.tail); rig.torso = M(src.torso);
            rig.back = M(src.back); rig.front = M(src.front); rig.head = M(src.head); rig.single = M(src.single);
            rig.farLegColor = src.farLegColor; rig.farGap = src.farGap; rig.headWidthRatio = src.headWidthRatio;
            go.SetActive(true);
            rig.Build(art, Rats.RigLength(r.Data));
            cutRig = rig;
            // 뛰어오를 때 발밑 선 5개 (같은 카메라에 찍힘)
            cutLegLines.Clear();
            if (lineSprite) for (int i = 0; i < 5; i++)
            {
                var l = new GameObject("Line").AddComponent<SpriteRenderer>();
                l.transform.SetParent(go.transform, false); l.sprite = lineSprite; l.color = cutLineColor; l.sortingOrder = -1; l.enabled = false;
                if (FxMat) l.sharedMaterial = FxMat;
                cutLegLines.Add(l);
            }
            if (!cutRatCam)
            {
                cutRatCam = new GameObject("SJ_CutRatCam").AddComponent<Camera>();
                cutRatCam.orthographic = true; cutRatCam.clearFlags = CameraClearFlags.SolidColor; cutRatCam.backgroundColor = new Color(0, 0, 0, 0);
                cutRatCam.nearClipPlane = 0.01f; cutRatCam.farClipPlane = 100;
            }
            // 카메라 화면 = RawImage 크기 그대로 (피벗 = 발끝 = 리그 원점)
            var rt = cutRat.rectTransform; var rc = rt.rect;
            int w = Mathf.Max(16, cutTexWidth), h = Mathf.Max(16, Mathf.RoundToInt(w * rc.height / Mathf.Max(1, rc.width)));
            if (!cutTex || cutTex.width != w || cutTex.height != h) { if (cutTex) cutTex.Release(); cutTex = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { name = "SJ_CutRat" }; }
            cutRatCam.targetTexture = cutTex; cutRatCam.orthographicSize = rc.height / 2 / cutPxPerUnit;
            cutRatCam.transform.position = new Vector3(cutRigOrigin.x + (0.5f - rt.pivot.x) * rc.width / cutPxPerUnit, cutRigOrigin.y + (0.5f - rt.pivot.y) * rc.height / cutPxPerUnit, -10);
            cutRatCam.enabled = true;
            cutRat.texture = cutTex; rt.anchoredPosition = cutRatBase;
            CutRatStep();
        }

        // 웹 컷인 쥐: 웅크려 부들부들 → 슈웅 뛰어올라 칸을 뚫고 나감 (오른쪽 = 글씨 쪽을 봄), 크기 ×5.2
        void CutRatStep()
        {
            if (!cutRig || !cutRat) return;
            float jt = t - 0.25f, up = jt < 0.45f ? 0 : Ease((jt - 0.45f) / 0.35f);
            var pose = jt < 0.45f ? P(front: 0.55f, farFront: 0.5f, back: -0.6f, farBack: -0.55f, bob: 6, sy: 0.75f, sx: 1.15f, head: 0.3f, tail: -0.4f)
                : P(front: 2.5f, farFront: 2.2f, back: -1.6f, farBack: -1.3f, head: -0.45f, tail: -1.2f, sx: 0.85f, sy: 1.25f, tilt: -0.15f);
            // 리그 1배 = 몸길이(게임 단위) → 웹 ×5.2 (1280 캔버스) = HUD ×7.8 픽셀
            cutRig.Apply(pose, cutRatZoom * 1.5f * 100 / cutPxPerUnit, 1, 1, 0);
            float jit = jt > 0 && jt < 0.45f ? Random.Range(-cutRatJit, cutRatJit) : 0;
            cutRat.rectTransform.anchoredPosition = cutRatBase + new Vector2((cutBand ? cutBand.anchoredPosition.x : 0) + jit, up * cutRatJump);
            // 발밑 선: 웹 (i×30, 30+|i|×10) → (i×34, +140×up), 굵기 5 (× 1.5 HUD 픽셀)
            if (!lineSprite) return;
            float u = 1 / cutPxPerUnit; var b = lineSprite.bounds.size;
            for (int i = 0; i < cutLegLines.Count; i++)
            {
                var l = cutLegLines[i]; int d = i - 2;
                l.enabled = up > 0.02f; if (!l.enabled) continue;
                Vector2 a = new(d * 45, -(45 + Mathf.Abs(d) * 15)), e = new(d * 51, a.y - 210 * up), m = (a + e) / 2, v = e - a;
                l.transform.localPosition = new Vector3(m.x * u, m.y * u, 0);
                l.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + 90);
                l.transform.localScale = new Vector3(7.5f * u / b.x, (v.magnitude + 7.5f) * u / b.y, 1);
            }
        }

        void DropCutRat()
        {
            if (cutRig) Destroy(cutRig.gameObject);
            cutRig = null; cutLegLines.Clear();
            if (cutRatCam) cutRatCam.enabled = false;
        }

        static Transform Map(Transform from, Transform to, Transform tr)
        {
            if (!tr) return null;
            var path = new List<string>();
            for (var c = tr; c && c != from; c = c.parent) path.Insert(0, c.name);
            return path.Count == 0 ? to : to.Find(string.Join("/", path));
        }

        // ── 월드 이펙트 (웹 particle · ring · stampAt) ──
        enum Kind { Spark, Trail, Star, Stamp }
        class Fx { public SpriteRenderer sr; public Kind k; public Color col; public float x, y, z, vx, vy, vz, w, h, life, max, rot, vr; }
        class RingFx { public LineRenderer lr; public float x, y, rad, width, life, max; public Color col; }
        readonly List<Fx> fxs = new(), fxPool = new();
        readonly List<RingFx> rings = new(), ringPool = new();
        Transform fxRoot;
        Material FxMat => fxMaterial ? fxMaterial : FxManager.I && FxManager.I.ringTemplate ? FxManager.I.ringTemplate.sharedMaterial : null;
        const int RingSeg = 64;

        Fx Spawn(Kind k, Sprite sp, float x, float y, float z, float w, float life, Color col)
        {
            if (!sp) return null;
            if (fxs.Count >= 600) { int j = fxs.FindIndex(q => q.k != Kind.Stamp); if (j < 0) j = 0; Recycle(fxs[j]); fxs.RemoveAt(j); }
            Fx f;
            if (fxPool.Count > 0) { f = fxPool[^1]; fxPool.RemoveAt(fxPool.Count - 1); }
            else f = new Fx { sr = NewSr("Fx") };
            f.sr.gameObject.SetActive(true); f.sr.enabled = false; f.sr.sprite = sp;
            f.k = k; f.x = x; f.y = y; f.z = z; f.w = f.h = w; f.life = f.max = life; f.col = col;
            f.vx = f.vy = f.vz = f.vr = 0; f.rot = k == Kind.Star ? Random.Range(0, 360f) : 0;
            fxs.Add(f);
            return f;
        }
        void Recycle(Fx f) { f.sr.gameObject.SetActive(false); fxPool.Add(f); }

        // 바닥 자국 (웹 stampAt): 바닥 기준 폭·높이 → TILT 로 눌러 그림, 오래 남았다가 옅어짐
        void Stamp(Sprite sp, float x, float y, float w, float h, Color col)
        {
            var f = Spawn(Kind.Stamp, sp, x, y, 0, w, stampLife, col);
            if (f != null) { f.h = h; f.rot = Random.Range(0, 360f) * (sp == dotSprite ? 0 : 1); }
        }

        // 웹 ring: 반지름 0.3 → 1 로 퍼지며 옅어지는 선 (굵기 width → 1), 바닥에 눕힘
        void Ring(float x, float y, float rad, Color col, float life, float width)
        {
            if (rings.Count >= 60) return;
            RingFx g;
            if (ringPool.Count > 0) { g = ringPool[^1]; ringPool.RemoveAt(ringPool.Count - 1); }
            else
            {
                if (!fxRoot) fxRoot = new GameObject("SJ_Fx").transform;
                var lr = new GameObject("Ring").AddComponent<LineRenderer>();
                lr.transform.SetParent(fxRoot, false);
                lr.useWorldSpace = true; lr.loop = true; lr.positionCount = RingSeg; lr.numCornerVertices = 2;
                if (FxMat) lr.sharedMaterial = FxMat;
                g = new RingFx { lr = lr };
            }
            g.lr.gameObject.SetActive(true); g.lr.sortingOrder = groundOrder;
            g.x = x; g.y = y; g.rad = rad; g.width = width; g.life = g.max = life; g.col = col;
            rings.Add(g);
            DrawRing(g);
        }
        void DrawRing(RingFx g)
        {
            float k = 1 - g.life / g.max, rad = g.rad * (0.3f + k * 0.7f);
            var c = g.col; c.a *= 1 - k;
            g.lr.startColor = g.lr.endColor = c;
            g.lr.widthMultiplier = (g.width * (1 - k) + 1) * World.U;
            for (int i = 0; i < RingSeg; i++)
            {
                float a = i * Mathf.PI * 2 / RingSeg;
                g.lr.SetPosition(i, World.ToUnity(g.x + Mathf.Cos(a) * rad, g.y + Mathf.Sin(a) * rad, 0));
            }
        }

        void UpdateFx(float dt)
        {
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                var g = rings[i];
                if ((g.life -= dt) <= 0) { g.lr.gameObject.SetActive(false); ringPool.Add(g); rings.RemoveAt(i); continue; }
                DrawRing(g);
            }
            for (int i = fxs.Count - 1; i >= 0; i--)
            {
                var f = fxs[i];
                if ((f.life -= dt) <= 0) { Recycle(f); fxs.RemoveAt(i); continue; }
                float sx = f.w, sy = f.h; var c = f.col; int order = airOrder;
                switch (f.k)
                {
                    case Kind.Spark:        // 웹 spark: 크기·알파 = 남은 수명 절반부터 줄어듦
                    {
                        f.x += f.vx * dt; f.y += f.vy * dt;
                        float a = Mathf.Clamp01(f.life / f.max * 2);
                        sx = sy = f.w * a; c.a *= a; order = World.SortOrder(f.y) + 30;
                        break;
                    }
                    case Kind.Trail:        // 웹 trail: 작아지며 옅어짐 (알파 0.45)
                    {
                        float a = f.life / f.max;
                        sx = sy = f.w * a; c.a *= a * 0.45f; order = World.SortOrder(f.y) + 30;
                        break;
                    }
                    case Kind.Star:         // 웹 burst(star): drag 2.5, 땅 위로만, 수명 0.5초 남으면 옅어짐
                    {
                        float d = Mathf.Max(0, 1 - 2.5f * dt);
                        f.vx *= d; f.vy *= d; f.x += f.vx * dt; f.y += f.vy * dt;
                        f.z = Mathf.Max(0, f.z + f.vz * dt); f.vz *= d; f.rot += f.vr * dt;
                        c.a *= Mathf.Clamp01(f.life * 2); order = World.SortOrder(f.y) + 30;
                        break;
                    }
                    case Kind.Stamp:
                        sy = f.h * World.TILT; c.a *= Mathf.Clamp01(f.life / 3); order = groundOrder - 2;
                        break;
                }
                var sr = f.sr; var b = sr.sprite.bounds.size;
                sr.enabled = true; sr.color = c; sr.sortingOrder = order;
                sr.transform.position = World.ToUnity(f.x, f.y, f.z);
                sr.transform.rotation = Quaternion.Euler(0, 0, f.k == Kind.Stamp ? 0 : -f.rot);
                sr.transform.localScale = new Vector3(Mathf.Max(0.0001f, sx * World.U / b.x), Mathf.Max(0.0001f, sy * World.U / b.y), 1);
            }
        }
    }
}
