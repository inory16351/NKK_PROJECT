using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 이펙트 (웹게임 burst / dust / ring / popup / stampAt / coins / shake / hitstop / HP 바 이식).
    // 템플릿(하이라키 자식, 꺼져 있음)을 복제해서 풀로 씀 → 모양·색·크기는 템플릿 인스펙터에서 수정.
    // 좌표는 게임 단위 (x, y = 바닥, z = 높이).
    public class FxManager : MonoBehaviour
    {
        public static FxManager I { get; private set; }
        public static bool Paused;
        public static bool UltFreeze;              // 필살기 컷인 중 화면 정지
        public static bool WorldFreeze;            // 슈퍼 점프 중 게임 세계 정지 (이펙트는 계속)
        public static float BaseTimeScale = 1;     // 밸런스 측정용 배속 (BalanceProbe)

        [Header("파티클 (하이라키 자식)")]
        public ParticleSystem shards;     // 깨진 조각
        public ParticleSystem dust;       // 먼지 뭉게
        public ParticleSystem stars;      // 반짝이·별
        public ParticleSystem spray;      // 분사 (브레스·물줄기). 비어 있으면 깨진 조각으로
        [Tooltip("파티클 크기 배율 (웹 원본 크기 = 반지름 → 그림 여백 포함 지름으로)")] public float shardSizeK = 2.6f, dustSizeK = 2.4f, starSizeK = 2.8f, spraySizeK = 2.4f;

        [Header("월드 캔버스 (1 픽셀 = 1 게임 단위)")]
        public RectTransform worldCanvas;
        public TMP_Text popupTemplate;
        [Tooltip("팝업이 튀어나오는 시간 (초)")] public float popInTime = 0.2f;
        [Tooltip("튀어나올 때 넘치는 정도 (클수록 통통)")] public float popOvershoot = 3f;
        [Tooltip("팝업 기울기 (도, ± 무작위)")] public float popTilt = 8;
        public RectTransform hpBarTemplate;      // 자식: Empty(Image) · Fill(Image, Filled)
        public Sprite hpGreen, hpYellow, hpRed;

        [Header("스프라이트 템플릿")]
        public SpriteRenderer ringTemplate;
        public SpriteRenderer spillTemplate;
        [Tooltip("얼룩 그림 (여러 장이면 무작위)")] public Sprite[] spillSprites;
        [Tooltip("바닥 얼룩이 사라지는 시간 (초)")] public float spillLife = 40;
        public int spillMax = 160;

        [Header("치즈 코인 (HUD 로 날아감)")]
        public RectTransform hudCanvas;
        public Image coinTemplate;
        public RectTransform coinTarget;      // 치즈 글자
        public int coinMax = 70;

        [Header("프레임 애니메이션 (폭발·먼지·타격·번개 착탄)")]
        public SpriteRenderer animTemplate;
        public FlipAnim[] anims;

        [Header("붙는 그림 (소용돌이·회오리 등, 일정 시간 머무름)")]
        [Tooltip("부모(바닥 눌림용 크기) + 자식 SpriteRenderer(회전용)")] public Transform stickerTemplate;
        public NamedSprite[] stickerSprites;

        [Header("레이저 (LineRenderer 템플릿)")]
        public LineRenderer beamTemplate;
        [Tooltip("레이저 굵기 (유니티 유닛, 빛 번짐 포함)")] public float beamWidth = 0.3f;
        [Tooltip("번개 굵기 (유니티 유닛, 빛 번짐 포함)")] public float boltWidth = 0.18f;

        [Header("빔·번개·베기 그림 (Assets/Art/Rats/FX_Beam, 비어 있으면 예전 줄 모양)")]
        [Tooltip("레이저 몸통 (가로 띠, 늘려 씀)")] public Sprite beamBodySprite;
        [Tooltip("레이저 총구 번쩍")] public Sprite beamFlareSprite;
        [Tooltip("레이저 맞은 자리 튐")] public Sprite beamHitSprite;
        [Tooltip("하늘 번개 (세로)")] public Sprite boltSprite;
        [Tooltip("짧은 번개 조각 (가로, 꺾인 줄에 이어 붙임)")] public Sprite boltSegSprite;
        [Tooltip("작은 전기 불꽃")] public Sprite sparkSprite;
        [Tooltip("칼 베기 초승달")] public Sprite slashArcSprite;
        [Tooltip("일자 베기")] public Sprite slashStreakSprite;
        [Tooltip("X 베기")] public Sprite slashCrossSprite;
        [Tooltip("빛 그림 템플릿 (비어 있으면 고리 템플릿 재질로 만듦)")] public SpriteRenderer glowTemplate;
        [Tooltip("빛 그림 정렬 순서 (레이저 템플릿과 같게)")] public int glowOrder = 31500;

        [Header("화면")]
        public CameraController cam;
        [Tooltip("흔들림 최대")] public float shakeMax = 0.5f;
        [Tooltip("역경직 중 시간 배율")] public float hitstopScale = 0.05f;

        [Header("체력바")]
        [Tooltip("맞은 뒤 체력바가 보이는 시간 (초)")] public float hpBarShowTime = 2.5f;
        [Tooltip("체력바 양 끝 테두리 비율 (채움이 테두리 안쪽부터)")] public float hpBarRim = 0.07f;

        [System.Serializable]
        public class FlipAnim
        {
            public string name;
            public Sprite[] frames;
            [Tooltip("초당 프레임")] public float fps = 18;
            [Tooltip("기본 크기 (게임 단위, 가로 폭)")] public float size = 140;
            [Tooltip("그림 중심을 바닥에서 띄우는 높이 (게임 단위)")] public float lift = 40;
        }

        [System.Serializable] public class NamedSprite { public string name; public Sprite sprite; }

        // 붙는 그림 핸들: Move 로 따라다니게, End 로 일찍 끝냄
        public class Sticker
        {
            public Transform root; public SpriteRenderer r;
            public float x, y, z, size, life, max, spin, flipRate, flipT; public bool ground; public Color col;
            public void Move(float nx, float ny, float nz) { x = nx; y = ny; z = nz; }
            public void End(float fade = 0.25f) { life = Mathf.Min(life, fade); }
        }

        class PopupFx { public TMP_Text t; public float x, y, z, vz, life, max, rot; }
        class AnimFx { public SpriteRenderer r; public FlipAnim a; public float t; }
        class RingFx { public SpriteRenderer r; public float x, y, rad, life, max; public Color col; }
        class SpillFx { public SpriteRenderer r; public float life; public float a0; }
        class CoinFx { public Image img; public Vector2 from; public float t, dur; }
        public class HpBar { public RectTransform rt; public Image fill; public bool used; }

        readonly List<PopupFx> popups = new(), popupPool = new();
        readonly List<RingFx> rings = new(), ringPool = new();
        readonly List<SpillFx> spills = new();
        readonly List<CoinFx> coins = new(), coinPool = new();
        readonly List<HpBar> hpPool = new();
        readonly List<AnimFx> animList = new(), animPool = new();
        readonly List<Sticker> stickers = new(), stickerPool = new();
        float shake, hitstopT;

        void Awake()
        {
            I = this;
            foreach (var t in new Component[] { popupTemplate, hpBarTemplate, ringTemplate, spillTemplate, coinTemplate, animTemplate, stickerTemplate, glowTemplate }) if (t) t.gameObject.SetActive(false);
        }

        static Vector3 W(float x, float y, float z) => World.ToUnity(x, y, z);

        // ── 파티클 ──
        void Emit(ParticleSystem ps, float x, float y, float z, int n, float min, float max, float up0, float up1, Color c0, Color c1, float s0, float s1, float life0, float life1)
        {
            if (!ps) return;
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                float a = Random.Range(0, Mathf.PI * 2), sp = Random.Range(min, max), vz = Random.Range(up0, up1);
                ep.position = W(x, y, z);
                ep.velocity = new Vector3(Mathf.Cos(a) * sp, -Mathf.Sin(a) * sp * World.TILT + vz, 0) * World.U;
                ep.startColor = Color.Lerp(c0, c1, Random.value);
                ep.startSize = Random.Range(s0, s1) * World.U;
                ep.startLifetime = Random.Range(life0, life1);
                ep.rotation = Random.Range(0, 360f);
                ps.Emit(ep, 1);
            }
        }

        // 깨진 조각 (색 두 개 사이)
        public void Burst(float x, float y, float z, int n, Color c0, Color c1, float min = 80, float max = 300, float s0 = 4, float s1 = 9)
            => Emit(shards, x, y, z, n, min, max, -40, 260, c0, c1, s0 * shardSizeK, s1 * shardSizeK, 0.35f, 0.8f);

        public void Dust(float x, float y, int n = 6, float s = 1)
            => Emit(dust, x, y, 4, n, 40 * s, 150 * s, 20, 60, new Color(0.98f, 0.97f, 0.94f, 0.75f), new Color(0.9f, 0.88f, 0.84f, 0.6f), 7 * Mathf.Sqrt(s) * dustSizeK, 13 * Mathf.Sqrt(s) * dustSizeK, 0.35f, 0.7f);

        public void Stars(float x, float y, float z, int n, Color c0, Color c1, float min = 100, float max = 260)
            => Emit(stars, x, y, z, n, min, max, 40, 220, c0, c1, 5 * starSizeK, 10 * starSizeK, 0.4f, 0.8f);

        // ── 글자 팝업 (위로 떠오르며 사라짐) ──
        public void Popup(float x, float y, string text, Color color, float size = 22, float life = 0.9f, float z = 30)
        {
            if (!popupTemplate) return;
            if (popups.Count > 36) { Recycle(popups[0]); popups.RemoveAt(0); }
            PopupFx p;
            if (popupPool.Count > 0) { p = popupPool[^1]; popupPool.RemoveAt(popupPool.Count - 1); }
            else { var t = Instantiate(popupTemplate, popupTemplate.transform.parent); p = new PopupFx { t = t }; }
            p.t.gameObject.SetActive(true);
            p.t.text = text; p.t.color = color; p.t.fontSize = size;
            p.x = x; p.y = y; p.z = z; p.vz = 80; p.life = p.max = life; p.rot = Random.Range(-popTilt, popTilt);
            popups.Add(p);
        }
        void Recycle(PopupFx p) { p.t.gameObject.SetActive(false); popupPool.Add(p); }

        // ── 바닥 고리 (퍼지며 사라짐) ──
        public void Ring(float x, float y, float rad, Color col, float life = 0.35f)
        {
            if (!ringTemplate || rings.Count >= 120) return;
            RingFx r;
            if (ringPool.Count > 0) { r = ringPool[^1]; ringPool.RemoveAt(ringPool.Count - 1); }
            else r = new RingFx { r = Instantiate(ringTemplate, ringTemplate.transform.parent) };
            r.r.gameObject.SetActive(true);
            r.x = x; r.y = y; r.rad = rad; r.col = col; r.life = r.max = life;
            rings.Add(r);
        }

        // ── 바닥 얼룩 (깨진 물건에서 쏟아짐, 천천히 옅어짐) ──
        public void Spill(float x, float y, float rad, Color col)
        {
            if (!spillTemplate) return;
            for (int i = 0; i < 4; i++)
            {
                SpillFx s;
                if (spills.Count >= spillMax) { s = spills[0]; spills.RemoveAt(0); }
                else s = new SpillFx { r = Instantiate(spillTemplate, spillTemplate.transform.parent) };
                s.r.gameObject.SetActive(true);
                if (spillSprites != null && spillSprites.Length > 0) s.r.sprite = spillSprites[Random.Range(0, spillSprites.Length)];
                float w =Random.Range(rad * 0.5f, rad), h = Random.Range(rad * 0.4f, rad * 0.8f), sz = s.r.sprite ? s.r.sprite.bounds.size.x : 1;
                s.r.transform.position = W(x + Random.Range(-rad, rad), y + Random.Range(-rad, rad) * 0.7f, 0);
                s.r.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 180f));
                s.r.transform.localScale = new Vector3(w * 2 * World.U / sz, h * 2 * World.TILT * World.U / sz, 1);
                s.a0 = 0.3f; col.a = s.a0; s.r.color = col; s.life = spillLife;
                spills.Add(s);
            }
        }
        public void ClearSpills() { foreach (var s in spills) Destroy(s.r.gameObject); spills.Clear(); }

        // ── 프레임 애니메이션: name = anims 목록의 이름 (explosion · poof · hit · zap), scale = 크기 배율 ──
        public void Anim(string name, float x, float y, float z = 0, float scale = 1)
        {
            if (!animTemplate || anims == null || animList.Count >= 60) return;
            FlipAnim a = null;
            foreach (var q in anims) if (q.name == name) { a = q; break; }
            if (a == null || a.frames == null || a.frames.Length == 0 || !a.frames[0]) return;
            AnimFx f;
            if (animPool.Count > 0) { f = animPool[^1]; animPool.RemoveAt(animPool.Count - 1); }
            else f = new AnimFx { r = Instantiate(animTemplate, animTemplate.transform.parent) };
            f.r.gameObject.SetActive(true);
            f.a = a; f.t = 0; f.r.sprite = a.frames[0]; f.r.flipX = Random.value < 0.5f;
            f.r.transform.position = W(x, y, z + a.lift * scale);
            f.r.transform.localScale = Vector3.one * (a.size * scale * World.U / a.frames[0].bounds.size.x);
            f.r.sortingOrder = World.SortOrder(y) + 20;
            animList.Add(f);
        }

        // ── 붙는 그림: size = 가로 폭 (게임 단위), spin = 초당 회전(도), ground = 바닥에 눕힘(위에서 본 그림), flipRate = 좌우 뒤집기 간격(초, 0 = 안 함) ──
        public Sticker AddSticker(string name, float x, float y, float z, float size, float life, float spin = 0, bool ground = false, Color? col = null, float flipRate = 0)
        {
            if (!stickerTemplate || stickerSprites == null || stickers.Count >= 40) return null;
            Sprite sp = null;
            foreach (var q in stickerSprites) if (q.name == name) { sp = q.sprite; break; }
            if (!sp) return null;
            Sticker s;
            if (stickerPool.Count > 0) { s = stickerPool[^1]; stickerPool.RemoveAt(stickerPool.Count - 1); }
            else { var t = Instantiate(stickerTemplate, stickerTemplate.parent); s = new Sticker { root = t, r = t.GetComponentInChildren<SpriteRenderer>(true) }; }
            s.root.gameObject.SetActive(true);
            s.r.sprite = sp; s.r.transform.localRotation = Quaternion.identity; s.r.flipX = false;
            s.x = x; s.y = y; s.z = z; s.size = size; s.life = s.max = life; s.spin = spin; s.ground = ground; s.flipRate = flipRate; s.flipT = flipRate;
            s.col = col ?? Color.white;
            float k = size * World.U / sp.bounds.size.x;
            s.root.localScale = new Vector3(k, ground ? k * World.TILT : k, 1);
            stickers.Add(s);
            return s;
        }

        // ── 치즈 코인: 박살 난 자리에서 HUD 치즈 글자로 날아감 ──
        public void Coin(float x, float y, int n = 1)
        {
            if (!coinTemplate || !hudCanvas) return;
            var c = Camera.main; if (!c) return;          // 밸런스 측정 중엔 카메라를 꺼서 없음
            for (int i = 0; i < n && coins.Count < coinMax; i++)
            {
                CoinFx k;
                if (coinPool.Count > 0) { k = coinPool[^1]; coinPool.RemoveAt(coinPool.Count - 1); }
                else k = new CoinFx { img = Instantiate(coinTemplate, coinTemplate.transform.parent) };
                k.img.gameObject.SetActive(true);
                Vector2 sp = c.WorldToScreenPoint(W(x + Random.Range(-15f, 15f), y, 30));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(hudCanvas, sp, null, out k.from);
                k.t = 0; k.dur = Random.Range(0.55f, 0.85f);
                coins.Add(k);
            }
        }

        // ── 빛 그림 (빔·번개·베기 스프라이트, 늘리고 돌려서 씀) ──
        class GlowFx { public SpriteRenderer r; public float life, max, grow, flick; public Color col; public Vector3 scale; }
        readonly List<GlowFx> glows = new(), glowPool = new();
        const int GLOW_MAX = 400;

        SpriteRenderer NewSR()
        {
            if (glowTemplate) return Instantiate(glowTemplate, glowTemplate.transform.parent);
            var go = new GameObject("Glow"); go.transform.SetParent(transform, false); var r = go.AddComponent<SpriteRenderer>();
            var src = ringTemplate ? ringTemplate : spillTemplate;
            if (src) { r.sortingLayerID = src.sortingLayerID; r.sharedMaterial = src.sharedMaterial; }
            return r;
        }
        // 그림 한 장: pos = 유니티 좌표, ang = 도, w·h = 유니티 크기, grow = 사는 동안 커지는 비율, flick = 깜빡임 세기
        void Glow(Sprite sp, Vector3 pos, float ang, float w, float h, Color col, float life, int order = 0, float grow = 0, float flick = 0, bool flipX = false)
        {
            if (!sp || glows.Count >= GLOW_MAX || life <= 0) return;
            GlowFx g;
            if (glowPool.Count > 0) { g = glowPool[^1]; glowPool.RemoveAt(glowPool.Count - 1); }
            else g = new GlowFx { r = NewSR() };
            g.r.gameObject.SetActive(true);
            g.r.sprite = sp; g.r.flipX = flipX; g.r.sortingOrder = glowOrder + order;
            var b = sp.bounds.size;
            g.scale = new Vector3(w / b.x, h / b.y, 1);
            g.r.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, ang));
            g.r.transform.localScale = g.scale;
            g.life = g.max = life; g.grow = grow; g.flick = flick; g.col = col; g.r.color = col;
            glows.Add(g);
        }
        // 그림을 a → b 로 늘림 (그림의 가로가 길이 방향). thick = 굵기 (유니티), over = 양 끝 겹침 비율
        void Strip(Sprite sp, Vector3 a, Vector3 b, float thick, Color col, float life, int order = 0, float over = 0, float flick = 0)
        {
            var d = b - a; float len = d.magnitude;
            if (len < 0.0001f) return;
            Glow(sp, (a + b) * 0.5f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, len * (1 + over), thick, col, life, order, 0, flick);
        }
        // 게임 각도(바닥 기준) → 화면 각도(도)
        static float ScreenAng(float a) => Mathf.Atan2(-Mathf.Sin(a) * World.TILT, Mathf.Cos(a)) * Mathf.Rad2Deg;
        static Color A(Color c, float a) { c.a *= a; return c; }
        static Color WhiteA(Color c, float k = 1) => new(1, 1, 1, c.a * k);

        // ── 레이저 줄 ──
        readonly List<(LineRenderer l, float life, float max)> beams = new();
        readonly List<LineRenderer> beamPool = new();
        LineRenderer GetLine()
        {
            if (!beamTemplate) return null;
            LineRenderer l; if (beamPool.Count > 0) { l = beamPool[^1]; beamPool.RemoveAt(beamPool.Count - 1); } else l = Instantiate(beamTemplate, beamTemplate.transform.parent);
            l.gameObject.SetActive(true); return l;
        }
        // width = 굵기 배율 (기본 beamWidth)
        public void Beam(float x1, float y1, float z1, float x2, float y2, float z2, Color col, float life, float width = 1)
        {
            if (beamBodySprite)
            {
                // 색 띠 + 가운데 흰 심지
                Vector3 a = W(x1, y1, z1), b = W(x2, y2, z2); float w = beamWidth * width * 0.6f;
                Strip(beamBodySprite, a, b, w, col, life);
                Strip(beamBodySprite, a, b, w * 0.4f, WhiteA(col, 0.9f), life, 1);
                return;
            }
            var l = GetLine(); if (!l) return;
            l.positionCount = 2; l.SetPosition(0, W(x1, y1, z1)); l.SetPosition(1, W(x2, y2, z2));
            l.widthMultiplier = beamWidth * width; l.startColor = l.endColor = col; beams.Add((l, life, life));
        }

        // 굵은 레이저 (웹게임 drawBigBeam): 번진 빛 · 색 몸통 · 흰 심지 3겹 + 총구 번쩍 + 끝 튐. w = 굵기 (게임 단위, 웹 38)
        public void BigBeam(float x1, float y1, float z1, float x2, float y2, float z2, Color col, float life, float w = 38, bool flare = true, bool hit = true)
        {
            if (!beamBodySprite) { Beam(x1, y1, z1, x2, y2, z2, col, life, w / 22f); Beam(x1, y1, z1, x2, y2, z2, Color.white, life, w / 60f); return; }
            Vector3 a = W(x1, y1, z1), b = W(x2, y2, z2), d = (b - a).normalized;
            float t = w * World.U, ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Strip(beamBodySprite, a, b, t * 1.8f, A(col, 0.45f), life, 0, 0, 0.15f);
            Strip(beamBodySprite, a, b, t, A(col, 0.9f), life, 1);
            Strip(beamBodySprite, a, b, t * 0.4f, Color.white, life, 2, 0, 0.1f);
            if (flare && beamFlareSprite)
            {
                float f = t * Random.Range(2.4f, 3f);
                Glow(beamFlareSprite, a, ang + Random.Range(-12f, 12f), f, f, A(col, 0.9f), life, 3);
                Glow(beamFlareSprite, a, ang + Random.Range(-20f, 20f), f * 0.55f, f * 0.55f, Color.white, life, 4);
            }
            if (hit && beamHitSprite)
            {
                float f = t * Random.Range(2f, 2.8f);
                Glow(beamHitSprite, b, Random.Range(0, 360f), f, f, A(col, 0.85f), life, 3);
                Glow(beamHitSprite, b, Random.Range(0, 360f), f * 0.5f, f * 0.5f, Color.white, life, 4);
            }
        }

        // ── 지지직 번개 (웹게임 bolt·drawZap·drawSkyBolt): 살아 있는 동안 rejag 초마다 새로 꺾이고 깜빡임 ──
        // 번진 빛(번개 조각 그림) · 색 몸통 · 흰 심지 3겹 + 곁가지. Move 로 끝점 이동, End 로 일찍 끝냄
        public class Zap
        {
            internal Vector3 a, b; internal float life, max, w, amp, rejag, rejagT, flick; internal int n, branches; internal Color col, halo;
            internal readonly List<SpriteRenderer> parts = new(); internal readonly List<Color> cols = new(); internal int used;
            public bool Alive => life > 0;
            public void Move(float x1, float y1, float z1, float x2, float y2, float z2) { a = W(x1, y1, z1); b = W(x2, y2, z2); rejagT = 0; }
            public void End(float fade = 0.06f) { life = Mathf.Min(life, fade); }
        }
        readonly List<Zap> zaps = new();
        readonly List<SpriteRenderer> srPool = new();
        int zapParts;
        const int ZAP_PARTS_MAX = 900;

        // 꺾인 번개 줄: 두 점 사이를 n 토막, amp = 꺾임 (게임 단위), w = 몸통 굵기 (게임 단위, 웹 7), glowCol = 번진 빛 색 (기본 col 반투명)
        // branches = 곁가지 수, rejag = 다시 꺾는 간격 (초, 0 = 안 함), flick = 깜빡임 세기. 그림이 없으면 예전 줄 (null 반환)
        public Zap BoltLine(float x1, float y1, float z1, float x2, float y2, float z2, Color col, float life, float w = 10, int n = 9, float amp = 22, Color? glowCol = null, int branches = 0, float rejag = 0.035f, float flick = 0.35f)
        {
            if (life <= 0) return null;
            if (boltSegSprite && beamBodySprite)
            {
                if (zapParts >= ZAP_PARTS_MAX) return null;
                var z = new Zap { a = W(x1, y1, z1), b = W(x2, y2, z2), life = life, max = life, w = w, amp = amp, n = Mathf.Max(1, n), branches = branches, rejag = rejag, flick = flick, col = col, halo = glowCol ?? A(col, 0.45f) };
                z.rejagT = rejag;
                DrawZap(z); ShadeZap(z);
                zaps.Add(z);
                return z;
            }
            var l = GetLine(); if (!l) return null;
            var pts = Jag(W(x1, y1, z1), W(x2, y2, z2), n, amp);
            l.positionCount = n + 1; for (int i = 0; i <= n; i++) l.SetPosition(i, pts[i]);
            l.widthMultiplier = boltWidth * w / 10f; l.startColor = l.endColor = col; beams.Add((l, life, life));
            return null;
        }

        // 지그재그 점 (웹 bolt)
        static Vector3[] Jag(Vector3 a, Vector3 b, int n, float amp)
        {
            Vector3 d = b - a, nrm = new Vector3(-d.y, d.x, 0).normalized; var pts = new Vector3[n + 1];
            for (int i = 0; i <= n; i++) pts[i] = Vector3.Lerp(a, b, i / (float)n) + (i == 0 || i == n ? Vector3.zero : nrm * Random.Range(-amp, amp) * World.U);
            return pts;
        }
        // 조각 하나 배치 (그림을 a → b 로 늘림)
        void ZapPart(Zap z, Sprite sp, Vector3 a, Vector3 b, float thick, Color c, int order, float over)
        {
            var d = b - a; float len = d.magnitude; if (len < 0.0001f) return;
            SpriteRenderer r;
            if (z.used < z.parts.Count) r = z.parts[z.used];
            else
            {
                if (srPool.Count > 0) { r = srPool[^1]; srPool.RemoveAt(srPool.Count - 1); } else r = NewSR();
                z.parts.Add(r); z.cols.Add(c); zapParts++;
            }
            z.cols[z.used] = c; z.used++;
            r.gameObject.SetActive(true); r.sprite = sp; r.flipX = Random.value < 0.5f; r.flipY = Random.value < 0.5f; r.sortingOrder = glowOrder + order;
            var s = sp.bounds.size;
            r.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg));
            r.transform.localScale = new Vector3(len * (1 + over) / s.x, thick / s.y, 1);
        }
        // 모양 새로 꺾기
        void DrawZap(Zap z)
        {
            z.used = 0;
            float t = z.w * World.U, k = z.w / 7f;
            var pts = Jag(z.a, z.b, z.n, z.amp);
            for (int i = 0; i < z.n; i++)
            {
                ZapPart(z, boltSegSprite, pts[i], pts[i + 1], t * 3.2f, z.halo, 0, 0.3f);         // 번진 빛 (지그재그 조각)
                ZapPart(z, beamBodySprite, pts[i], pts[i + 1], t, z.col, 1, 0.1f);                 // 색 몸통
                ZapPart(z, beamBodySprite, pts[i], pts[i + 1], t * 0.38f, WhiteA(z.col), 2, 0.1f); // 흰 심지
            }
            // 곁가지 (웹 drawSkyBolt: 아래쪽으로 짧게 4토막)
            for (int bI = 0; bI < z.branches && z.n > 1; bI++)
            {
                var p = pts[1 + Random.Range(0, z.n - 1)];
                var q = p + new Vector3(Random.Range(-70f, 70f), -Random.Range(20f, 80f), 0) * k * World.U;
                var bp = Jag(p, q, 4, 10 * k);
                for (int i = 0; i < 4; i++)
                {
                    ZapPart(z, beamBodySprite, bp[i], bp[i + 1], t * 0.4f, A(z.col, 0.85f), 1, 0.1f);
                    ZapPart(z, beamBodySprite, bp[i], bp[i + 1], t * 0.15f, WhiteA(z.col, 0.9f), 2, 0.1f);
                }
            }
            for (int i = z.used; i < z.parts.Count; i++) z.parts[i].gameObject.SetActive(false);
        }
        // 투명도 (끝 무렵 옅어짐 + 깜빡임)
        void ShadeZap(Zap z)
        {
            float a = Mathf.Clamp01(z.life / z.max * 2.2f) * (1 - z.flick * Random.value);
            for (int i = 0; i < z.used; i++) { var c = z.cols[i]; c.a *= a; z.parts[i].color = c; }
        }
        void UpdateZaps(float dt)
        {
            for (int i = zaps.Count - 1; i >= 0; i--)
            {
                var z = zaps[i]; z.life -= dt;
                if (z.life <= 0)
                {
                    foreach (var r in z.parts) { r.gameObject.SetActive(false); srPool.Add(r); }
                    zapParts -= z.parts.Count; z.parts.Clear(); z.cols.Clear(); zaps.RemoveAt(i); continue;
                }
                if (z.rejag > 0 && (z.rejagT -= dt) <= 0) { z.rejagT = z.rejag; DrawZap(z); }
                ShadeZap(z);
            }
        }

        // 번개: 하늘(z0)에서 (x, y) 바닥까지 (웹게임 strikeBolt + drawSkyBolt). life 동안 지지직, width = 굵기 배율
        public Zap Bolt(float x, float y, Color col, float width = 1, float life = 0.28f, float z0 = 760)
        {
            float sx = x + Random.Range(-30f, 30f);
            Zap z = null;
            if (boltSegSprite && beamBodySprite)
            {
                int n = Mathf.Max(6, Mathf.RoundToInt(z0 / 45));
                z = BoltLine(sx, y, z0, x, y, 0, col, life, 7 * width, n, 22 * width, null, 2);
                // 첫 순간 번쩍: 세로 번개 그림 (번진 빛)
                if (boltSprite)
                {
                    Vector3 top = W(sx, y, z0), bot = W(x, y, 0), d = bot - top; float len = d.magnitude, ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 90;
                    var bs = boltSprite.bounds.size; float wid = Mathf.Min(len * bs.x / bs.y, 160 * width * World.U);
                    Glow(boltSprite, (top + bot) * 0.5f, ang, wid, len, A(col, 0.45f), Mathf.Min(0.12f, life), -1, 0, 0.5f, Random.value < 0.5f);
                }
                Spark(x, y, 6, col, 90 * width, 0.22f);
                Spark(x, y, 10, Color.white, 50 * width, 0.15f);
            }
            else
            {
                var l = GetLine();
                if (l)
                {
                    const int n = 9; l.positionCount = n;
                    for (int i = 0; i < n; i++) { float k = i / (float)(n - 1), j = (i == 0 || i == n - 1) ? 0 : Random.Range(-22f, 22f) * width; l.SetPosition(i, W(Mathf.Lerp(sx, x, k) + j, y, z0 * (1 - k))); }
                    l.widthMultiplier = boltWidth * width; l.startColor = l.endColor = col; beams.Add((l, life, life));
                }
            }
            Emit(shards, x, y, 6, 6, 120, 360, -40, 260, Color.white, col, 2 * shardSizeK, 4 * shardSizeK, 0.2f, 0.4f);   // 웹 spark 튐
            Ring(x, y, 50 * width, new Color(col.r, col.g, col.b, 0.8f), 0.3f); Anim("zap", x, y, 0, width); Shake(0.04f);
            return z;
        }

        // 짧은 전기 튐: (x, y, z) 둘레 rad 안에 짧은 번개 n 개 + 가끔 불꽃 (충전·감전 몸 둘레)
        public void Crackle(float x, float y, float z, float rad, Color col, int n = 2, float w = 3, float life = 0.08f)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Random.Range(0, Mathf.PI * 2), d = Random.Range(0.5f, 1f) * rad, a2 = a + Random.Range(0.6f, 1.6f) * (Random.value < 0.5f ? -1 : 1);
                BoltLine(x + Mathf.Cos(a) * rad * 0.3f, y, z + Mathf.Sin(a) * rad * 0.3f, x + Mathf.Cos(a2) * d, y, z + Mathf.Sin(a2) * d, col, life, w, 4, rad * 0.18f, null, 0, 0.03f);
            }
            if (Random.value < 0.4f) Spark(x + Random.Range(-rad, rad) * 0.6f, y, z + Random.Range(-rad, rad) * 0.6f, col, rad * 0.8f, 0.1f);
        }

        // 작은 전기 불꽃 (size = 폭, 게임 단위)
        public void Spark(float x, float y, float z, Color col, float size = 50, float life = 0.18f)
        {
            if (!sparkSprite) { Stars(x, y, z, 3, Color.white, col, 60, 160); return; }
            Vector3 p = W(x, y, z); float s = size * World.U;
            Glow(sparkSprite, p, Random.Range(0, 360f), s, s, col, life, 3, 0.3f, 0.4f);
            Glow(sparkSprite, p, Random.Range(0, 360f), s * 0.5f, s * 0.5f, WhiteA(col), life, 4, 0.3f);
        }

        // 칼 베기: kind 0 = 초승달, 1 = 일자, 2 = X. ang = 게임 각도 (라디안, 바닥 기준), len = 길이 (게임 단위)
        public void Slash(float x, float y, float z, float ang, float len, Color col, float life = 0.3f, int kind = 0, bool flip = false)
        {
            var sp = kind == 0 ? slashArcSprite : kind == 1 ? slashStreakSprite : slashCrossSprite;
            if (!sp)
            {
                float hx = Mathf.Cos(ang) * len * 0.5f, hy = Mathf.Sin(ang) * len * 0.5f;
                Beam(x - hx, y - hy, z, x + hx, y + hy, z, col, life);
                if (kind == 2) Beam(x + hy, y - hx, z, x - hy, y + hx, z, col, life);
                return;
            }
            Vector3 p = W(x, y, z); float a = ScreenAng(ang), w = len * World.U, b = sp.bounds.size.y / sp.bounds.size.x, h = w * b;
            if (kind == 1) h = Mathf.Max(h, w * 0.07f);
            Glow(sp, p, a, w * 1.08f, h * 1.25f, A(col, 0.6f), life, 0, 0.25f, 0, flip);
            Glow(sp, p, a, w, h, WhiteA(col), life * 0.8f, 1, 0.2f, 0, flip);
        }

        // 부채꼴 분사 (치즈 브레스 등): 각도 a 방향으로 반각 half 안에서
        public void Spray(float x, float y, float z, float a, float range, int n, float half, Color c0, Color c1)
        {
            var ps = spray ? spray : shards;
            if (!ps) return;
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                float an = a + Random.Range(-half, half), sp = Random.Range(0.3f, 1f) * range * 3;
                ep.position = W(x, y, z);
                ep.velocity = new Vector3(Mathf.Cos(an) * sp, -Mathf.Sin(an) * sp * World.TILT, 0) * World.U;
                ep.startColor = Color.Lerp(c0, c1, Random.value); ep.startSize = Random.Range(5f, 9f) * spraySizeK * World.U; ep.startLifetime = Random.Range(0.3f, 0.5f);
                ps.Emit(ep, 1);
            }
        }

        // ── 화면 ──
        public void Shake(float v) => shake = Mathf.Min(shakeMax, shake + v);
        public void Hitstop(float t) => hitstopT = Mathf.Max(hitstopT, t);

        // ── 체력바 (물건·사람이 매 프레임 요청) ──
        public HpBar GetHpBar()
        {
            foreach (var b in hpPool) if (!b.used) { b.used = true; return b; }
            if (!hpBarTemplate) return null;
            var rt = Instantiate(hpBarTemplate, hpBarTemplate.parent);
            var nb = new HpBar { rt = rt, fill = rt.Find("Fill").GetComponent<Image>(), used = true };
            hpPool.Add(nb);
            return nb;
        }
        public void ReleaseHpBar(HpBar b) { if (b == null) return; b.used = false; b.rt.gameObject.SetActive(false); }
        // x,y,z = 막대 아래 가운데, w = 폭 (게임 단위), k = 남은 비율, alpha = 투명도
        public void ShowHpBar(HpBar b, float x, float y, float z, float w, float k, float alpha)
        {
            if (b == null) return;
            b.rt.gameObject.SetActive(alpha > 0.01f);
            b.rt.position = W(x, y, z);
            b.rt.sizeDelta = new Vector2(w, w / 5.1f);
            b.fill.sprite = k > 0.5f ? hpGreen : k > 0.25f ? hpYellow : hpRed;
            b.fill.fillAmount = k > 0 ? hpBarRim + (1 - 2 * hpBarRim) * k : 0;
            var cg = b.rt.GetComponent<CanvasGroup>(); if (cg) cg.alpha = alpha;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // 역경직
            if (Paused || UltFreeze) Time.timeScale = 0;          // 포기 창 등으로 멈춤
            else if (hitstopT > 0) { hitstopT -= dt; Time.timeScale = hitstopScale * BaseTimeScale; } else Time.timeScale = BaseTimeScale;
            // 팝업
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.life -= dt; p.z += p.vz * dt; p.vz *= 1 - dt * 2.5f;
                if (p.life <= 0) { Recycle(p); popups.RemoveAt(i); continue; }
                // 통통: 작게 시작 → 크게 넘쳤다가 제자리 (easeOutBack), 사라질 때 살짝 작아짐
                float age = p.max - p.life, e = Mathf.Clamp01(age / popInTime) - 1;
                float s = (1 + (popOvershoot + 1) * e * e * e + popOvershoot * e * e) * (0.8f + 0.2f * Mathf.Clamp01(p.life / (p.max * 0.3f)));
                p.t.rectTransform.position = W(p.x, p.y, p.z);
                p.t.rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, s);
                p.t.rectTransform.localRotation = Quaternion.Euler(0, 0, p.rot * Mathf.Clamp01(age / popInTime));
                var c = p.t.color; c.a = Mathf.Clamp01(p.life / p.max * 2.5f); p.t.color = c;
            }
            // 고리
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                var r = rings[i];
                r.life -= dt;
                if (r.life <= 0) { r.r.gameObject.SetActive(false); ringPool.Add(r); rings.RemoveAt(i); continue; }
                float k = 1 - r.life / r.max, rad = r.rad * (0.3f + k * 0.7f), sz = r.r.sprite ? r.r.sprite.bounds.size.x : 1;
                r.r.transform.position = W(r.x, r.y, 0);
                r.r.transform.localScale = new Vector3(rad * 2 * World.U / sz, rad * 2 * World.TILT * World.U / sz, 1);
                var c = r.col; c.a *= 1 - k; r.r.color = c;
            }
            // 프레임 애니메이션 (역경직 중엔 같이 느려짐)
            for (int i = animList.Count - 1; i >= 0; i--)
            {
                var f = animList[i];
                f.t += Time.deltaTime;
                int k = (int)(f.t * f.a.fps);
                if (k >= f.a.frames.Length) { f.r.gameObject.SetActive(false); animPool.Add(f); animList.RemoveAt(i); continue; }
                f.r.sprite = f.a.frames[k];
            }
            // 붙는 그림 (나타날 때·사라질 때 서서히)
            for (int i = stickers.Count - 1; i >= 0; i--)
            {
                var s = stickers[i];
                s.life -= Time.deltaTime;
                if (s.life <= 0) { s.root.gameObject.SetActive(false); stickerPool.Add(s); stickers.RemoveAt(i); continue; }
                s.root.position = W(s.x, s.y, s.z);
                if (s.spin != 0) s.r.transform.Rotate(0, 0, s.spin * Time.deltaTime);
                if (s.flipRate > 0 && (s.flipT -= Time.deltaTime) <= 0) { s.flipT = s.flipRate; s.r.flipX = !s.r.flipX; }
                s.r.sortingOrder = World.SortOrder(s.y) + (s.ground ? -1 : 10);
                var c = s.col; c.a *= Mathf.Clamp01((s.max - s.life) / 0.15f) * Mathf.Clamp01(s.life / 0.25f); s.r.color = c;
            }
            // 얼룩
            for (int i = spills.Count - 1; i >= 0; i--)
            {
                var s = spills[i];
                s.life -= dt;
                if (s.life <= 0) { Destroy(s.r.gameObject); spills.RemoveAt(i); continue; }
                var c = s.r.color; c.a = s.a0 * Mathf.Clamp01(s.life / 8); s.r.color = c;
            }
            // 코인
            if (coinTarget && hudCanvas)
            {
                Vector2 to = (Vector2)hudCanvas.InverseTransformPoint(coinTarget.position);
                for (int i = coins.Count - 1; i >= 0; i--)
                {
                    var k = coins[i];
                    k.t += dt;
                    float e = Mathf.Clamp01(k.t / k.dur), ee = e * e;
                    if (e >= 1) { k.img.gameObject.SetActive(false); coinPool.Add(k); coins.RemoveAt(i); continue; }
                    var p = Vector2.Lerp(k.from, to, ee) + new Vector2(0, Mathf.Sin(e * Mathf.PI) * 120);
                    k.img.rectTransform.anchoredPosition = p;
                    k.img.rectTransform.localScale = Vector3.one * (1.2f - 0.5f * e);
                }
            }
            // 레이저
            for (int i = beams.Count - 1; i >= 0; i--)
            {
                var b = beams[i]; b.life -= dt;
                if (b.life <= 0) { b.l.gameObject.SetActive(false); beamPool.Add(b.l); beams.RemoveAt(i); continue; }
                var c = b.l.startColor; c.a = b.life / b.max; b.l.startColor = b.l.endColor = c; beams[i] = b;
            }
            UpdateZaps(dt);
            // 빛 그림 (끝 무렵 빠르게 옅어짐, 커짐·깜빡임)
            for (int i = glows.Count - 1; i >= 0; i--)
            {
                var g = glows[i]; g.life -= dt;
                if (g.life <= 0) { g.r.gameObject.SetActive(false); glowPool.Add(g); glows.RemoveAt(i); continue; }
                float k = 1 - g.life / g.max, a = Mathf.Clamp01(g.life / g.max * 2.2f);
                if (g.flick > 0) a *= 1 - g.flick * Random.value;
                if (g.grow != 0) g.r.transform.localScale = g.scale * (1 + g.grow * k);
                var c = g.col; c.a *= a; g.r.color = c;
            }
            // 흔들림
            shake = Mathf.Max(0, shake - dt * 1.6f);
            if (cam) cam.shakeOffset = shake > 0 ? new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * shake * shake * 40 * World.U : Vector3.zero;
        }
    }
}
