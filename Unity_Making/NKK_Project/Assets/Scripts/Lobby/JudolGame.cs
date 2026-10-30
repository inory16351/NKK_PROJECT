using System;
using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace NKK.Lobby
{
    // 미니게임 테이블 (MinigameTable.json) 쥐돌깨기 시트들
    [Serializable] public class JudolGradeRow { public string grade; public float dmg, skill_p, aura_alpha, aura_r; public string color; public Color Color => ColorUtility.TryParseHtmlString(color, out var c) ? c : Color.white; }
    [Serializable] public class JudolAugRow { public string key; public int augment_id, tier; public float v1, cap, v2, v3; public float V(int n) => cap > 0 ? Mathf.Min(cap, v1 * n) : v1 * n; }
    [Serializable] public class JudolTierRow { public int tier; public float weight; }
    [Serializable] public class JudolBandRow { public float hp_below; public string color; }
    [Serializable] public class JudolShapeRow { public string effect_type, shape; }
    [Serializable] public class JudolTableFile { public List<KeyValueRow> Judol_Config; public List<JudolGradeRow> Judol_Grade; public List<JudolAugRow> Judol_Aug; public List<JudolTierRow> Judol_Tier; public List<JudolBandRow> Judol_Band; public List<JudolShapeRow> Judol_Shape; }

    // 쥐돌깨기 (뒷골목 미니 게임기, 2026-10-30): 웹 프로토타입 쥐돌깨기.html (Shooting Ballz 식 벽돌깨기) 을 로비 창으로 옮김.
    // 끌었다 놓으면 가진 쥐 전부가 줄지어 날아가 벽돌을 튕기며 깎음. 처음 바닥에 닿은 쥐 자리 = 다음 발사 지점. 턴마다 벽돌 한 줄씩 내려오고 바닥 줄에 닿으면 끝.
    // 벽돌을 부수면 치즈 → 쥐 뽑기, 쥐를 재료로 증강 뽑기. 판 좌표 = 웹과 같은 1280×720 픽셀 (왼쪽 위 0, y 아래 +).
    // 그림은 매 프레임 풀에서 다시 놓음 (웹 draw 와 같음). 수치 = 미니게임 테이블 · 글 = 씬 TMP / 인스펙터 문자열 · 쥐 그림 = JudolRatArt
    public class JudolGame : MonoBehaviour
    {
        public static JudolGame I { get; private set; }
        public static bool IsOpen => I && I.isActiveAndEnabled;
        // 배경음악 켬 (M) — SceneMusic 이 창이 열려 있을 때 게임기 곡을 틈
        public static bool MusicOn = true;

        [Header("데이터")]
        [Tooltip("미니게임 테이블 (MinigameTable.json)")] public TextAsset table;
        public RatArtLibrary artLibrary;
        [Tooltip("쥐 그림 미리 그리기 재질 (NKK/JudolBake)")] public Material bakeMaterial;

        [Header("판 (웹 1280×720 픽셀 그대로)")]
        public RectTransform board;
        [Tooltip("판을 창에 맞출 때 비율 (1 = 꽉)")] public float fitMargin = 0.94f;
        [Tooltip("경기장 (RectMask2D, 자식은 경기장 기준)")] public RectTransform field;
        [Tooltip("경기장 꾸밈: 바닥 무늬 · 어둡게 · 바닥 줄 (자리만 맞춤)")] public RectTransform floorArt, shade, dangerRow;
        [Tooltip("경기장 아래 벽 · 테두리 (자리만 맞춤)")] public RectTransform wall, frame;

        [Header("템플릿 (꺼 둠, 풀에서 복제)")]
        public Image cellFxTpl;
        public Image itemTpl;
        [Tooltip("벽돌 (자식: Tint · Crack · Flash · Hp)")] public Image brickTpl;
        public Image aimTpl;
        public RawImage flyerTpl, walkerTpl;
        public Image bladeTpl;
        [Tooltip("이펙트 (경기장 위) · 화면 이펙트 (패널 위)")] public Image partTpl, screenPartTpl;
        public TMP_Text floatTpl;
        [Tooltip("발사 지점 쥐 · 마릿수 ({n})")] public RawImage launcher;
        public TMP_Text launchCount;

        [Header("그림")]
        public Sprite tile;
        public Sprite tileHard, crackMid, crackLow, cheeseChunk, cheeseSplash, dust, debrisBrick, debrisSlab, blade, meteor, bolt, burstGold, burstRainbow;
        [Tooltip("고유 스킬 모양 이펙트: ■ 3×3 · ━ 가로줄 · ┃ 세로줄 · ✕ 대각선 · ✚ 십자")] public Sprite fxSq, fxRow, fxCol, fxX, fxCross;

        [Header("왼쪽: 치즈 · 쥐 뽑기 · 보유 쥐")]
        public TMP_Text cheeseText;
        [Tooltip("쥐 뽑기 (JudolPress id buy) · 아래 글 ({price} {pity})")] public Button buyButton;
        public TMP_Text buySub, ownedText;
        [Tooltip("등급 줄 6개 (자식: Pic(RawImage) · Name · Dmg({n}) · Skill · Count({n}))")] public Image[] gradeRows = new Image[0];
        [Tooltip("등급 줄 바탕 투명도: 있음 · 없음")] public float rowOn = 0.9f, rowOff = 0.4f;
        [Tooltip("일반 등급 이름 색 (다른 등급은 등급 색)")] public Color commonNameColor = new(0.54f, 0.51f, 0.47f);
        public RawImage[] recentPics = new RawImage[0];
        [Tooltip("뽑기 확률 ({rates}) · 스킬 확률 ({rates})")] public TMP_Text rateText, skillNoteText;

        [Header("오른쪽: 턴 · 증강")]
        public TMP_Text turnText;
        public TMP_Text bestText;
        [Tooltip("증강 뽑기 · 버튼 글 ({n}) · 재료 ({list}) · 모자람 ({n})")] public Button augButton;
        public TMP_Text augLabel, augSubUse, augSubNeed, augCountText;
        [Tooltip("가진 증강 줄 템플릿 (자식: Bar · Icon · Name({name}{lv}) · Desc)")] public Image augRowTpl;
        [Tooltip("가진 증강 줄 간격 · 줄 수 상한")] public float augRowStep = 46; public int augRowMax = 9;
        [Tooltip("빠르게 ({n}) · 회수 · 조준 안내")] public Button ffButton;
        public TMP_Text ffLabel;
        public Button recallButton;
        public GameObject aimHint;
        [Tooltip("빠르게 버튼 색: 보통 · 깜빡임 (4초 넘게 날 때)")] public Color ffColor = new(0.36f, 0.56f, 0.79f), ffBlink = new(0.94f, 0.71f, 0.29f);

        [Header("창")]
        public GameObject ovTitle;
        public GameObject ovAug, ovPause, ovOver;
        public Button startButton, resumeButton, retryButton, againButton, closeButton;
        [Tooltip("재료로 쓴 쥐 ({list}) · 끝 ({turn} {best} {broken} {cheese} {rats} {grade} {augs})")] public TMP_Text augInfo, overBody;
        [Tooltip("증강 고르기 카드 3장 (Image = 등급 색 테두리, 자식: Icon · Name · Tier({tier}{lv}) · Desc)")] public Button[] augCards = new Button[0];

        [Header("글 (자리표시만 채움)")]
        [Tooltip("증강 설명: key = 테이블 key · desc ({v} {v2} {v3})")] public AugText[] augTexts = new AugText[0];
        [Serializable] public class AugText { public string key; public string desc; }
        public string critText = "치명!", jackpotText = "잭팟!", gainText = "+{n}", rainText = "치즈 비 +{n}", trainText = "장난감 기차!";
        [Tooltip("고유 스킬 ({name}) · 이름 없을 때")] public string skillText = "{name}!", skillNoName = "필살!";
        [Tooltip("높은 등급 뽑음 ({grade} {name}) · 증강 고름 ({name})")] public string pullText = "{grade}! {name}", augGotText = "{name}!";
        [Tooltip("목록 사이 · 스킬 확률 사이 · 증강 단계 ({n}) · 카드 단계 ({n})")] public string listSep = " · ", rateSep = "/", augLvText = " ×{n}", augStageText = " · {n}단계";
        [Tooltip("글자 색: 치즈 · 치명 · 잭팟 · 기차")] public Color cheeseCol = new(1, 0.886f, 0.54f), critCol = new(1, 0.88f, 0.4f), jackpotCol = new(1, 0.835f, 0.29f), trainCol = Color.white;
        [Tooltip("감전 칸 색")] public Color zapCol = new(0.56f, 0.83f, 1);
        [Tooltip("떠오르는 글 크기: 작은 · 보통")] public float floatSmall = 15, floatBig = 21;

        [Header("효과음 (SfxManager 이름)")]
        public string sfxFire = "rat_squeak";
        public string sfxHit = "hit_wood", sfxBreak = "smash_wood", sfxCoin = "cheese", sfxWin = "rat_grade_birth", sfxPull = "rat_birth", sfxLose = "jgl_game_over", sfxTurn = "rat_land", sfxSkill = "wall_blast", sfxDeny = "ui_deny", sfxStart = "ui_open", sfxClose = "ui_close";

        // ── 표 ──
        const float W = 1280, H = 720;
        int COLS = 7, ROWS = 9; float CELL = 70, FX0, FY0 = 22, FW, FH, FLOOR;
        readonly Dictionary<string, float> cfg = new();
        float Cf(string k, float d) => cfg.TryGetValue(k, out var v) ? v : d;
        readonly JudolGradeRow[] grades = new JudolGradeRow[6];
        readonly List<JudolAugRow> augRows = new();
        readonly List<JudolTierRow> tiers = new();
        readonly List<JudolBandRow> bands = new();
        readonly Dictionary<string, string> shapeOf = new();
        readonly List<string>[] pool = new List<string>[6];
        readonly float[] gachaRate = new float[6];
        bool inited;

        // ── 상태 ──
        enum St { Title, Aim, Fly, AugPick, Over }
        class Rat { public int id, grade; public string code; }
        class Brick { public int c, r, born; public float hp, max, flash, vy; public bool hard, dead; }
        class Item { public int c, r; public bool got; }
        class Shot { public int grade; public string code; public bool giant, blades, clone; }
        class Flyer : Shot { public float x, y, vx, vy, r; public bool usedFirst, done; public readonly Dictionary<Brick, float> bladeCd = new(); }
        class Walker { public string code; public int grade, face; public float x; public bool done; }
        class Part { public Sprite s; public float x, y, size, h, life, max, grow, rot, vx, vy, grav, vr; public bool screen; }
        class Floater { public float x, y, t; public string text; public Color col; public bool small; }
        class CellFx { public int c, r; public Color col; public float t; }
        class Delay { public float t; public Action f; public bool done; }

        St state = St.Title; bool paused;
        int turn, nextId, augCount, pity, ffMul = 1, broken, bestGrade;
        double cheese, cheeseTotal;
        List<Rat> rats = new(); List<Brick> bricks = new(); List<Item> items = new(); List<Flyer> flyers = new(); List<Walker> walkers = new(); List<Shot> launchQ = new();
        List<Part> parts = new(); List<Floater> floats = new(); List<CellFx> cellFx = new(); List<Rat> recent = new(); List<Delay> later = new();
        readonly Dictionary<string, int> augs = new();
        float launchX, launchT, flyT, time, popT, sndHit, sndBreak, holdBuy = -1;
        float? firstLandX;
        Vector2 launchDir = new(0, -1), pressAt; Vector2? aim;
        int A(string k) => augs.TryGetValue(k, out var n) ? n : 0;
        JudolAugRow AugRow(string k) { foreach (var a in augRows) if (a.key == k) return a; return null; }
        float AV(string k) { var r = AugRow(k); int n = A(k); return r != null && n > 0 ? r.V(n) : 0; }
        float AV2(string k) { var r = AugRow(k); return r != null ? r.v2 : 0; }
        static int Best => Progress.I ? Progress.I.judolBest : 0;

        // ── 열기 · 닫기 ──
        void Awake()
        {
            I = this;
            Init();
        }
        void OnDestroy() { if (I == this) I = null; JudolRatArt.Clear(); }

        public void Open()
        {
            gameObject.SetActive(true);
            Init();
            if (state == St.Aim || state == St.Fly) { paused = true; Show(ovPause); }
            else if (state == St.Title) Show(ovTitle);
            Sfx(sfxStart);
        }

        public void Close()
        {
            holdBuy = -1; aim = null;
            Sfx(sfxClose);
            gameObject.SetActive(false);
        }

        void Init()
        {
            if (inited) return; inited = true;
            LoadTable();
            if (startButton) startButton.onClick.AddListener(NewGame);
            if (resumeButton) resumeButton.onClick.AddListener(() => { paused = false; Show(null); });
            if (retryButton) retryButton.onClick.AddListener(NewGame);
            if (againButton) againButton.onClick.AddListener(NewGame);
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (augButton) augButton.onClick.AddListener(OpenAug);
            if (ffButton) ffButton.onClick.AddListener(ToggleFast);
            if (recallButton) recallButton.onClick.AddListener(Recall);
            for (int i = 0; i < augCards.Length; i++) { int k = i; if (augCards[i]) augCards[i].onClick.AddListener(() => PickAug(k)); }
            if (aimTpl && !aimTpl.sprite) aimTpl.sprite = Circle();         // 조준 점 = 흰 동그라미 (코드로 만듦)
            foreach (var t in new Component[] { cellFxTpl, itemTpl, brickTpl, aimTpl, flyerTpl, walkerTpl, bladeTpl, partTpl, screenPartTpl, floatTpl, augRowTpl }) if (t) t.gameObject.SetActive(false);
            Layout();
            Reset();
            state = St.Title; Show(ovTitle);
            StaticTexts();
        }

        void LoadTable()
        {
            cfg.Clear(); augRows.Clear(); tiers.Clear(); bands.Clear(); shapeOf.Clear();
            var t = table ? JsonUtility.FromJson<JudolTableFile>(table.text) : null;
            if (t != null)
            {
                if (t.Judol_Config != null) foreach (var r in t.Judol_Config) cfg[r.key] = r.value;
                if (t.Judol_Grade != null) foreach (var g in t.Judol_Grade) { var e = E.P<Grade>(g.grade); grades[(int)e] = g; }
                if (t.Judol_Aug != null) augRows.AddRange(t.Judol_Aug);
                if (t.Judol_Tier != null) tiers.AddRange(t.Judol_Tier);
                if (t.Judol_Band != null) bands.AddRange(t.Judol_Band);
                if (t.Judol_Shape != null) foreach (var s in t.Judol_Shape) shapeOf[s.effect_type] = s.shape;
            }
            for (int i = 0; i < 6; i++) grades[i] ??= new JudolGradeRow { grade = ((Grade)i).ToString(), dmg = i + 1, color = "#ffffff" };
            COLS = (int)Cf("cols", 7); ROWS = (int)Cf("rows", 9); CELL = Cf("cell", 70); FY0 = Cf("field_y", 22);
            FW = COLS * CELL; FH = ROWS * CELL; FX0 = (W - FW) / 2; FLOOR = FY0 + FH;
            // 쥐 뽑기 = 원작 뽑기 테이블 등급 확률 · 등급별 쥐 = 파츠 그림이 있는 쥐
            var db = GameDatabase.Instance;
            for (int i = 0; i < 6; i++) { pool[i] = new List<string>(); gachaRate[i] = 0; }
            if (db)
            {
                if (db.Gacha.Gacha_Grade != null) foreach (var g in db.Gacha.Gacha_Grade) gachaRate[(int)g.Grade] = g.rate;
                foreach (var r in db.Rats.Values) if (artLibrary && JudolRatArt.Usable(artLibrary.Get(r.code_id))) pool[(int)r.Grade].Add(r.code_id);
            }
            for (int i = 0; i < 6; i++) if (pool[i].Count == 0) pool[i].Add("brownrat");
        }

        // 경기장 크기가 표에서 바뀌어도 꾸밈 자리를 맞춤
        void Layout()
        {
            if (field) { field.anchoredPosition = B(FX0 + FW / 2, FY0 + FH / 2); field.sizeDelta = new Vector2(FW, FH); }
            foreach (var r in new[] { floorArt, shade }) if (r) { r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(FW, FH); }
            if (dangerRow) { dangerRow.anchoredPosition = F(FX0 + FW / 2, FY0 + (ROWS - 0.5f) * CELL); dangerRow.sizeDelta = new Vector2(FW, CELL); }
            if (wall) { wall.anchoredPosition = B(FX0 + FW / 2, FLOOR - 4 + 23); wall.sizeDelta = new Vector2(FW + 20, 46); }
            if (frame) { frame.anchoredPosition = B(FX0 + FW / 2, FY0 + FH / 2); frame.sizeDelta = new Vector2(FW + 4, FH + 4); }
        }

        void StaticTexts()
        {
            var rs = new List<string>(); foreach (var r in gachaRate) rs.Add((r * 100).ToString("0.##"));
            Fill(rateText, ("rates", string.Join(listSep, rs)));
            var sk = new List<string>(); int sg = (int)Cf("skill_grade", 3);
            for (int g = sg; g < 6; g++) sk.Add((grades[g].skill_p * 100).ToString("0.##") + "%");
            Fill(skillNoteText, ("rates", string.Join(rateSep, sk)));
        }

        // ── 새 판 ──
        void Reset()
        {
            turn = 1; cheese = 0; nextId = 0; rats.Clear(); bricks.Clear(); items.Clear(); flyers.Clear(); walkers.Clear(); parts.Clear(); floats.Clear(); cellFx.Clear(); recent.Clear(); later.Clear();
            augs.Clear(); augCount = 0; pity = 0; ffMul = 1; launchQ.Clear(); aim = null; broken = 0; cheeseTotal = 0; bestGrade = 0;
            AddRat(0, "brownrat"); for (int i = 1; i < (int)Cf("start_rats", 3); i++) AddRat(0);
            launchX = FX0 + FW / 2; firstLandX = null;
            SpawnRow(); TurnStart();
        }

        public void NewGame()
        {
            Reset();
            state = St.Aim; paused = false; Show(null);
            Sfx(sfxStart);
        }

        Rat AddRat(int grade, string code = null)
        {
            var p = pool[grade];
            var r = new Rat { id = nextId++, grade = grade, code = code ?? p[Random.Range(0, p.Count)] };
            rats.Add(r); bestGrade = Mathf.Max(bestGrade, grade); return r;
        }

        // ── 벽돌 ──
        Brick BrickAt(int c, int r) { foreach (var b in bricks) if (b.c == c && b.r == r && !b.dead) return b; return null; }
        float CellX(int c) => FX0 + c * CELL;
        float CellY(int r) => FY0 + r * CELL;
        float BrickHp(int t) => Mathf.Max(1, Mathf.Round(t * Mathf.Pow(Cf("hp_grow", 1.075f), t)));      // 체력 = 턴 × hp_grow^턴 (쥐 수는 턴²으로 늘어나므로 후반에 따라잡음)

        void SpawnRow()
        {
            float p4 = Mathf.Min(Cf("p4_max", 0.8f), Cf("p4_base", 0.3f) + turn * Cf("p4_per", 0.015f)), p5 = Mathf.Min(Cf("p5_max", 0.6f), turn * Cf("p5_per", 0.012f));
            int n = Mathf.Min(COLS, (int)Cf("row_min", 3) + (Random.value < p4 ? 1 : 0) + (Random.value < p5 ? 1 : 0));
            var cols = new List<int>(); for (int c = 0; c < COLS; c++) cols.Insert(Random.Range(0, cols.Count + 1), c);
            for (int i = 0; i < n; i++)
            {
                bool hard = Random.value < Cf("hard_rate", 0.15f); float hp = BrickHp(turn) * (hard ? Cf("hard_mul", 2) : 1);
                bricks.Add(new Brick { c = cols[i], r = 0, hp = hp, max = hp, hard = hard, born = turn, vy = CellY(0) - CELL });
            }
            if (n < COLS && Random.value < Cf("item_rate", 0.25f)) items.Add(new Item { c = cols[n], r = 0 });
        }

        bool ShiftDown()
        {
            foreach (var b in bricks) b.r++;
            foreach (var it in items) it.r++;
            items.RemoveAll(it => it.r >= ROWS - 1);
            foreach (var b in bricks) if (!b.dead && b.r >= ROWS - 1) return true;
            return false;
        }

        // 피해: 치명 같은 증강은 여기서 (plain = 증강 효과 없이)
        void HitBrick(Brick b, float dmg, bool plain = false)
        {
            if (b == null || b.dead) return;
            float d = dmg;
            if (!plain && Random.value < AV("crit") / 100f) { d *= Mathf.Max(1, AV2("crit")); Float(CellX(b.c) + CELL / 2, CellY(b.r) + 10, critText, critCol, true); }
            b.hp -= d; b.flash = 0.08f;
            if (b.hp <= 0) BreakBrick(b);
            else if (time - sndHit > 0.035f) { sndHit = time; Sfx(sfxHit); }
        }

        void BreakBrick(Brick b)
        {
            b.dead = true; broken++;
            double gain = b.born * (b.hard ? Cf("hard_mul", 2) : 1) * (1 + AV("gold") / 100f);       // 치즈 = 생긴 턴 수 (단단한 벽돌 ×2)
            if (Random.value < AV("jackpot") / 100f) { gain *= Mathf.Max(1, AV2("jackpot")); Float(CellX(b.c) + CELL / 2, CellY(b.r), jackpotText, jackpotCol); }
            gain = Math.Round(gain); cheese += gain; cheeseTotal += gain;
            float x = CellX(b.c) + CELL / 2, y = CellY(b.r) + CELL / 2;
            Float(x, y - 6, gainText.Replace("{n}", gain.ToString("0")), cheeseCol, true);
            parts.Add(new Part { s = dust, x = x, y = y, size = 70, life = 0.45f, max = 0.45f, grow = 0.5f, rot = Random.value * 6 });
            parts.Add(new Part { s = cheeseSplash, x = x, y = y, size = 46, life = 0.5f, max = 0.5f, vy = -0.6f });
            for (int i = 0; i < 3; i++) parts.Add(new Part { s = Random.value < 0.5f ? debrisBrick : debrisSlab, x = x, y = y, vx = (Random.value - 0.5f) * 6, vy = -2 - Random.value * 3, grav = 0.3f, size = 16 + Random.value * 10, life = 0.7f, max = 0.7f, rot = Random.value * 6, vr = (Random.value - 0.5f) * 0.3f });
            if (time - sndBreak > 0.05f) { sndBreak = time; Sfx(sfxBreak); }
            float dom = AV("domino");
            if (dom > 0) Later(0.06f, () => { foreach (var (dc, dr) in Dirs) HitBrick(BrickAt(b.c + dc, b.r + dr), dom, true); });
        }
        static readonly (int, int)[] Dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        void Later(float t, Action f) => later.Add(new Delay { t = t, f = f });

        // ── 쥐 피해 · 고유 스킬 ──
        float RatDmg(Shot f) => (grades[f.grade].dmg + AV("dmg") + (f.giant ? AV("giant") : 0)) * (1 + AV("legend") / 100f);

        void OnBrickHit(Flyer f, Brick b)
        {
            float d = RatDmg(f);
            HitBrick(b, d);
            if (Random.value < AV("multi") / 100f) HitBrick(b, d);
            if (Random.value < AV("zap") / 100f)
            {
                var nb = new List<Brick>(); foreach (var (dc, dr) in Dirs) { var x = BrickAt(b.c + dc, b.r + dr); if (x != null) nb.Add(x); }
                if (nb.Count > 0) { var t = nb[Random.Range(0, nb.Count)]; HitBrick(t, Mathf.Max(1, AV2("zap")), true); cellFx.Add(new CellFx { c = t.c, r = t.r, col = zapCol, t = 0.2f }); }
            }
            if (f.grade >= (int)Cf("skill_grade", 3) && (!f.usedFirst || Random.value < grades[f.grade].skill_p)) { f.usedFirst = true; Skill(f, b); }
        }

        RatSkillRow SkillOf(string code)
        {
            var db = GameDatabase.Instance;
            return db && db.RatsByCode.TryGetValue(code, out var r) && db.RatSkills.TryGetValue(r.action_skill, out var s) ? s : null;
        }

        // 고유 스킬 모양 = 그 쥐의 원작 액션 스킬 effect_type (신화는 3×3 도 같이)
        List<string> ShapesOf(Shot f)
        {
            var l = new List<string>();
            if (f.grade < (int)Cf("skill_grade", 3)) return l;
            var sk = SkillOf(f.code);
            string s = sk != null && shapeOf.TryGetValue(sk.effect_type, out var v) ? v : "cross";
            l.Add(s); if (f.grade == 5 && s != "sq") l.Add("sq");
            return l;
        }

        void Skill(Flyer f, Brick b)
        {
            var cells = new List<(int, int)>();
            void Add(int c, int r) { if (c >= 0 && c < COLS && r >= 0 && r < ROWS && !(c == b.c && r == b.r) && !cells.Contains((c, r))) cells.Add((c, r)); }
            var shapes = ShapesOf(f);
            foreach (var s in shapes)
            {
                if (s == "sq") for (int dc = -1; dc <= 1; dc++) for (int dr = -1; dr <= 1; dr++) Add(b.c + dc, b.r + dr);
                if (s == "row" || s == "cross") for (int c = 0; c < COLS; c++) Add(c, b.r);
                if (s == "col" || s == "cross") for (int r = 0; r < ROWS; r++) Add(b.c, r);
                if (s == "x") for (int k = -ROWS; k <= ROWS; k++) { Add(b.c + k, b.r + k); Add(b.c + k, b.r - k); }
            }
            float d = RatDmg(f); var col = grades[f.grade].Color;
            foreach (var (c, r) in cells) { cellFx.Add(new CellFx { c = c, r = r, col = col, t = 0.25f }); HitBrick(BrickAt(c, r), d, true); }
            float cx = CellX(b.c) + CELL / 2, cy = CellY(b.r) + CELL / 2;
            foreach (var s in shapes)
            {
                if (s == "row" || s == "cross") parts.Add(new Part { s = fxRow, x = FX0 + FW / 2, y = cy, size = FW, life = 0.25f, max = 0.25f });
                if (s == "col" || s == "cross") parts.Add(new Part { s = fxCol, x = cx, y = FY0 + FH / 2, size = 120, h = FH, life = 0.25f, max = 0.25f });
                if (s == "sq") parts.Add(new Part { s = fxSq, x = cx, y = cy, size = CELL * 3, life = 0.25f, max = 0.25f, grow = 0.3f });
                if (s == "x") parts.Add(new Part { s = fxX, x = cx, y = cy, size = CELL * 4, life = 0.3f, max = 0.3f });
            }
            if (time - popT > 0.35f)
            {
                popT = time; var sk = SkillOf(f.code);
                Float(cx, cy - 20, sk != null && !string.IsNullOrEmpty(sk.skill_name) ? skillText.Replace("{name}", sk.skill_name) : skillNoName, col);
                Sfx(sfxSkill);
            }
        }

        // ── 턴 ──
        void TurnStart()
        {
            cheese += Cf("turn_cheese", 10);                                           // 턴마다 기본 치즈 (쥐 1마리 값)
            if (A("rain") > 0) { double g2 = Math.Round(turn * AV("rain")); cheese += g2; cheeseTotal += g2; Float(FX0 + FW / 2, FY0 + 30, rainText.Replace("{n}", g2.ToString("0")), cheeseCol); }
            var alive = bricks.FindAll(b => !b.dead);
            int top = 0; foreach (var r in rats) top = Mathf.Max(top, r.grade);
            float topDmg = (grades[top].dmg + AV("dmg")) * (1 + AV("legend") / 100f);
            if (A("storm") > 0)
                for (int i = 0; i < (int)AV("storm"); i++)
                {
                    if (alive.Count == 0) break; var b = alive[Random.Range(0, alive.Count)];
                    Later(0.1f + i * 0.06f, () => { if (b.dead) return; parts.Add(new Part { s = bolt, x = CellX(b.c) + CELL / 2, y = CellY(b.r) - 40, size = 70, h = 160, life = 0.25f, max = 0.25f }); HitBrick(b, topDmg, true); });
                }
            if (A("train") > 0 && alive.Count > 0)
            {
                int low = 0; foreach (var b in alive) low = Mathf.Max(low, b.r);
                float dmg = AV("train");
                Later(0.3f, () =>
                {
                    parts.Add(new Part { s = fxRow, x = FX0 + FW / 2, y = CellY(low) + CELL / 2, size = FW, life = 0.3f, max = 0.3f });
                    foreach (var b in bricks.FindAll(x => !x.dead && x.r == low)) HitBrick(b, dmg, true);
                    Float(FX0 + FW / 2, CellY(low), trainText, trainCol);
                });
            }
            int every = Mathf.Max(1, (int)AV2("meteor"));
            if (A("meteor") > 0 && turn % every == 0)
            {
                var big = alive.FindAll(_ => true); big.Sort((a, b) => b.hp.CompareTo(a.hp));
                float ratio = AugRow("meteor") != null ? AugRow("meteor").v3 / 100f : 0.5f;
                for (int i = 0; i < Mathf.Min(big.Count, (int)AV("meteor")); i++)
                {
                    var b = big[i];
                    Later(0.2f + i * 0.15f, () => { if (b.dead) return; parts.Add(new Part { s = meteor, x = CellX(b.c) + CELL / 2, y = CellY(b.r) + CELL / 2, size = 110, life = 0.35f, max = 0.35f }); HitBrick(b, Mathf.Ceil(b.hp * ratio), true); });
                }
            }
        }

        void EndTurn()
        {
            launchX = firstLandX ?? launchX; firstLandX = null;
            turn++;
            if (ShiftDown()) { GameOver(); return; }
            SpawnRow(); TurnStart();
            state = St.Aim; ffMul = 1;
            Sfx(sfxTurn);
        }

        void GameOver()
        {
            state = St.Over;
            int best = Best;
            if (turn > best) { best = turn; if (Progress.I) Progress.I.SetJudolBest(turn); }
            Fill(overBody, ("turn", turn), ("best", best), ("broken", broken), ("cheese", LobbyManager.Fmt(cheeseTotal)), ("rats", rats.Count), ("grade", GradeName(bestGrade)), ("augs", augCount));
            Sfx(sfxLose);
            Later(0.4f, () => Show(ovOver));
        }

        // ── 발사 ──
        Vector2? AimDir()
        {
            if (aim == null) return null;
            var a = aim.Value; float l = a.magnitude; if (l < 20) return null;
            float dx = a.x / l, dy = a.y / l, minA = Cf("min_angle", 8) * Mathf.Deg2Rad, minS = Mathf.Sin(minA);
            if (dy > -minS) { dy = -minS; dx = (dx >= 0 ? 1 : -1) * Mathf.Cos(minA); }
            return new Vector2(dx, dy);
        }

        void Fire(Vector2 dir)
        {
            var order = new List<Rat>(rats); order.Sort((a, b) => a.id.CompareTo(b.id));
            launchQ.Clear();
            for (int i = 0; i < order.Count; i++) launchQ.Add(new Shot { grade = order[i].grade, code = order[i].code, giant = i == 0 && A("giant") > 0, blades = i == 0 && A("blades") > 0 });
            if (A("clone") > 0 && order.Count > 0)
            {
                var top = order[0]; foreach (var r in order) if (r.grade > top.grade) top = r;
                for (int i = 0; i < (int)AV("clone"); i++) launchQ.Add(new Shot { grade = top.grade, code = top.code, clone = true });
            }
            launchT = 0; flyT = 0; state = St.Fly; aim = null; firstLandX = null; walkers.Clear();
            launchDir = dir; Sfx(sfxFire);
        }

        float LaunchInterval() => Mathf.Max(Cf("launch_min", 0.02f), Mathf.Min(Cf("launch_max", 0.07f), Cf("launch_total", 2.8f) / Mathf.Max(1, launchQ.Count)));
        float RatR => Cf("rat_r", 9);

        void SpawnFlyer(Shot q)
        {
            float r = RatR * (q.giant ? Mathf.Max(1, AV2("giant")) : 1), sp = Cf("speed", 15);
            flyers.Add(new Flyer { grade = q.grade, code = q.code, giant = q.giant, blades = q.blades, clone = q.clone, x = launchX, y = FLOOR - r, vx = launchDir.x * sp, vy = launchDir.y * sp, r = r });
        }

        void Recall()
        {
            if (state != St.Fly) return;
            foreach (var q in launchQ) Land(q, launchX);
            launchQ.Clear();
            foreach (var f in flyers) Land(f, f.x);
            flyers.Clear();
        }

        void Land(Shot f, float x)
        {
            if (firstLandX == null) firstLandX = Mathf.Clamp(x, FX0 + RatR, FX0 + FW - RatR);
            if (!f.clone) walkers.Add(new Walker { code = f.code, grade = f.grade, x = x, face = x < firstLandX ? 1 : -1 });
        }

        // 원 vs 사각형: 가장 가까운 점 → 법선 방향으로 반사 (입사각 = 반사각, 모서리는 모서리 법선)
        static bool CollideRect(ref float fx, ref float fy, ref float vx, ref float vy, float r, float x0, float y0, float x1, float y1)
        {
            float nx = Mathf.Max(x0, Mathf.Min(fx, x1)), ny = Mathf.Max(y0, Mathf.Min(fy, y1));
            float dx = fx - nx, dy = fy - ny, d2 = dx * dx + dy * dy;
            if (d2 > r * r) return false;
            float d = Mathf.Sqrt(d2);
            if (d < 1e-6f)          // 중심이 사각형 안: 가장 얕은 면으로 밀어냄
            {
                float l = fx - x0, rr = x1 - fx, t = fy - y0, bb = y1 - fy, m = Mathf.Min(Mathf.Min(l, rr), Mathf.Min(t, bb));
                if (m == l) { dx = -1; dy = 0; } else if (m == rr) { dx = 1; dy = 0; } else if (m == t) { dx = 0; dy = -1; } else { dx = 0; dy = 1; }
            }
            else { dx /= d; dy /= d; }
            float dot = vx * dx + vy * dy;
            if (dot < 0) { vx -= 2 * dot * dx; vy -= 2 * dot * dy; }
            fx = nx + dx * (r + 0.01f); fy = ny + dy * (r + 0.01f);
            return true;
        }

        // 거의 수평으로 끝없이 왕복하지 않게 최소 각도 유지 + 속력 고정
        void KeepAngle(Flyer f)
        {
            float sp = Mathf.Max(1e-4f, Mathf.Sqrt(f.vx * f.vx + f.vy * f.vy)), minVy = sp * Mathf.Sin(Cf("min_angle", 8) * Mathf.Deg2Rad);
            if (Mathf.Abs(f.vy) < minVy) { f.vy = (f.vy >= 0 ? 1 : -1) * minVy; f.vx = (f.vx >= 0 ? 1 : -1) * Mathf.Sqrt(Mathf.Max(0, sp * sp - minVy * minVy)); }
            float k = Cf("speed", 15) / Mathf.Sqrt(f.vx * f.vx + f.vy * f.vy); f.vx *= k; f.vy *= k;
        }

        void FlyerStep(Flyer f, float dt)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(Cf("substeps", 4) * dt * 60)); float k = dt * 60 / n;      // 속도 = 1/60초당 speed px, 반지름보다 적게 움직이도록 나눔
            for (int s = 0; s < n; s++)
            {
                f.x += f.vx * k; f.y += f.vy * k;
                bool hit = false;
                if (f.x - f.r < FX0) { f.x = FX0 + f.r; f.vx = Mathf.Abs(f.vx); hit = true; }
                if (f.x + f.r > FX0 + FW) { f.x = FX0 + FW - f.r; f.vx = -Mathf.Abs(f.vx); hit = true; }
                if (f.y - f.r < FY0) { f.y = FY0 + f.r; f.vy = Mathf.Abs(f.vy); hit = true; }
                if (f.y + f.r >= FLOOR && f.vy > 0) { f.y = FLOOR - f.r; f.done = true; Land(f, f.x); return; }
                int c0 = Mathf.FloorToInt((f.x - FX0) / CELL), r0 = Mathf.FloorToInt((f.y - FY0) / CELL);
                for (int i = 0; i < bricks.Count; i++)
                {
                    var b = bricks[i];
                    if (b.dead || Mathf.Abs(b.c - c0) > 1 || Mathf.Abs(b.r - r0) > 1) continue;
                    float x0 = CellX(b.c) + 2, y0 = CellY(b.r) + 2;
                    if (CollideRect(ref f.x, ref f.y, ref f.vx, ref f.vy, f.r, x0, y0, x0 + CELL - 4, y0 + CELL - 4)) { OnBrickHit(f, b); hit = true; }
                }
                if (hit) KeepAngle(f);
                foreach (var it in items)
                    if (!it.got && Vector2.Distance(new Vector2(f.x, f.y), new Vector2(CellX(it.c) + CELL / 2, CellY(it.r) + CELL / 2)) < Cf("item_r", 18) + f.r)
                    { it.got = true; cheese += turn; cheeseTotal += turn; Sfx(sfxCoin); Float(CellX(it.c) + CELL / 2, CellY(it.r) + 20, gainText.Replace("{n}", turn.ToString()), cheeseCol); }
                if (f.blades)            // 회전 치즈 칼날
                {
                    int nb = (int)AV("blades"); float rad = Cf("blade_radius", 28), spin = Cf("blade_spin", 8), dmg = Mathf.Max(1, AV2("blades"));
                    for (int i = 0; i < nb; i++)
                    {
                        float a = time * spin + i * Mathf.PI * 2 / nb, bx = f.x + Mathf.Cos(a) * rad, by = f.y + Mathf.Sin(a) * rad;
                        var b = BrickAt(Mathf.FloorToInt((bx - FX0) / CELL), Mathf.FloorToInt((by - FY0) / CELL));
                        if (b != null && !(f.bladeCd.TryGetValue(b, out var cd) && cd > time)) { f.bladeCd[b] = time + Cf("blade_cd", 0.15f); HitBrick(b, dmg, true); }
                    }
                }
            }
            items.RemoveAll(it => it.got);
        }

        // ── 뽑기 ──
        bool PullRat()
        {
            float price = Cf("rat_price", 10);
            if (state != St.Aim || cheese < price) return false;
            cheese -= price; pity++;
            float roll = Random.value, acc = 0; int gr = 0;
            for (int i = 0; i < 6; i++) { acc += gachaRate[i]; gr = i; if (roll < acc) break; }
            int pg = (int)Cf("pity_grade", 4);
            if (pity >= Cf("pity", 30) && gr < pg) gr = Random.value < Cf("pity_mythic", 0.25f) ? 5 : pg;          // 전설 이상 천장
            if (gr >= pg) pity = 0;
            var r = AddRat(gr); Recent(r);
            if (Random.value < AV("birth") / 100f) Recent(AddRat(0));
            if (gr >= (int)Cf("skill_grade", 3))
            {
                parts.Add(new Part { s = gr == 5 ? burstRainbow : burstGold, x = 198, y = 300, size = 220, life = 0.7f, max = 0.7f, grow = 0.4f, screen = true });
                Float(198, 250, pullText.Replace("{grade}", GradeName(gr)).Replace("{name}", RatName(r.code)), grades[gr].Color);
                Sfx(sfxWin);
            }
            else Sfx(sfxPull);
            return true;
        }
        void Recent(Rat r) { recent.Insert(0, r); if (recent.Count > 10) recent.RemoveRange(10, recent.Count - 10); }
        void PullMany() { for (int i = 0; i < (int)Cf("ten_pull", 10); i++) if (!PullRat()) break; }

        int AugCost() => Mathf.RoundToInt(Cf("aug_base", 10) * Mathf.Pow(Cf("aug_mul", 1.5f), augCount));
        // 재료 = 낮은 등급 먼저, 같은 등급이면 오래된(먼저 얻은) 쥐 먼저
        List<Rat> AugMaterials()
        {
            var l = new List<Rat>(rats); l.Sort((a, b) => a.grade != b.grade ? a.grade.CompareTo(b.grade) : a.id.CompareTo(b.id));
            int n = Mathf.Min(l.Count, AugCost()); return l.GetRange(0, n);
        }
        string MatText(List<Rat> mats)
        {
            var byG = new int[6]; foreach (var r in mats) byG[r.grade]++;
            var l = new List<string>(); for (int i = 0; i < 6; i++) if (byG[i] > 0) l.Add(GradeName(i) + " " + byG[i]);
            return string.Join(listSep, l);
        }

        readonly List<JudolAugRow> offers = new();
        void OpenAug()
        {
            if (state != St.Aim || rats.Count <= AugCost()) { Sfx(sfxDeny); return; }
            var mats = AugMaterials(); var ids = new HashSet<int>(); foreach (var r in mats) ids.Add(r.id);
            rats.RemoveAll(r => ids.Contains(r.id)); augCount++;
            offers.Clear();
            float tw = 0; foreach (var t in tiers) tw += t.weight;
            for (int guard = 0; offers.Count < Mathf.Min(3, augRows.Count) && guard < 200; guard++)
            {
                float roll = Random.value * tw, acc = 0; int tier = tiers.Count > 0 ? tiers[0].tier : 1;
                foreach (var t in tiers) { acc += t.weight; if (roll < acc) { tier = t.tier; break; } }
                var cand = augRows.FindAll(a => a.tier == tier && !offers.Contains(a)); if (cand.Count == 0) continue;
                offers.Add(cand[Random.Range(0, cand.Count)]);
            }
            Fill(augInfo, ("list", MatText(mats)));
            var db = GameDatabase.Instance;
            for (int i = 0; i < augCards.Length; i++)
            {
                var card = augCards[i]; if (!card) continue;
                bool on = i < offers.Count; card.gameObject.SetActive(on); if (!on) continue;
                var a = offers[i]; int n = A(a.key) + 1; var row = AugMeta(a);
                var tc = TierColor(a.tier);
                if (card.image) card.image.color = tc;
                var icon = Child<Image>(card, "Icon"); if (icon) { icon.sprite = AugIcon(row); icon.enabled = icon.sprite; icon.preserveAspect = true; }
                SetText(Child<TMP_Text>(card, "Name"), row != null ? row.aug_name : a.key);
                var tier = Child<TMP_Text>(card, "Tier"); if (tier) { Fill(tier, ("tier", TierName(a.tier)), ("lv", n > 1 ? augStageText.Replace("{n}", n.ToString()) : "")); tier.color = tc; }
                SetText(Child<TMP_Text>(card, "Desc"), AugDesc(a, n));
            }
            state = St.AugPick; Show(ovAug); Sfx(sfxWin);
        }

        void PickAug(int i)
        {
            if (state != St.AugPick || i >= offers.Count) return;
            var a = offers[i]; int n = A(a.key) + 1; augs[a.key] = n;
            Show(null); state = St.Aim; Sfx(sfxCoin);
            var row = AugMeta(a);
            Float(W - 200, 300, augGotText.Replace("{name}", row != null ? row.aug_name : a.key), TierColor(a.tier));
        }

        AugmentRow AugMeta(JudolAugRow a) { var db = GameDatabase.Instance; return db && db.AugmentsById.TryGetValue(a.augment_id, out var r) ? r : null; }
        static Sprite AugIcon(AugmentRow r) { var art = AugmentArt.I; return art ? (r != null ? art.Icon(r.icon) : null) ?? art.emptySlot : null; }
        string AugDesc(JudolAugRow a, int n)
        {
            string d = a.key; foreach (var t in augTexts) if (t.key == a.key) { d = t.desc; break; }
            return d.Replace("{v}", F0(a.V(n))).Replace("{v2}", F0(a.v2)).Replace("{v3}", F0(a.v3));
        }
        static string F0(float v) => v.ToString("0.##");
        static string TierName(int t) { var db = GameDatabase.Instance; return db && db.AugGrades.TryGetValue(t, out var g) ? g.grade_name : ""; }
        static Color TierColor(int t) { var db = GameDatabase.Instance; return db && db.AugGrades.TryGetValue(t, out var g) ? g.Color : Color.white; }
        static string GradeName(int g) { var db = GameDatabase.Instance; return db && db.Grades.TryGetValue((Grade)g, out var r) ? r.grade_name : ""; }
        static string RatName(string code) { var db = GameDatabase.Instance; return db && db.RatsByCode.TryGetValue(code, out var r) ? r.character_name : code; }

        void ToggleFast() { if (state != St.Fly) return; int mx = (int)Cf("ff_max", 4); ffMul = ffMul >= mx ? 1 : ffMul * 2; }

        // ── 입력 ──
        public void OnPress(string id, PointerEventData e, int phase)
        {
            if (paused || !board) return;
            if (id == "buy")
            {
                if (phase == 0) { var k = Keyboard.current; if (k != null && k.shiftKey.isPressed) PullMany(); else { if (!PullRat()) Sfx(sfxDeny); holdBuy = Cf("hold_delay", 0.35f); } }
                if (phase == 2) holdBuy = -1;
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(board, e.position, e.pressEventCamera, out var l);
            var p = new Vector2(l.x + W / 2, H / 2 - l.y);
            if (phase == 0) { if (state == St.Aim && p.x > FX0 - 40 && p.x < FX0 + FW + 40) { pressAt = p; aim = Vector2.zero; } }
            else if (phase == 1) { if (aim != null) aim = pressAt - p; }
            else { if (aim != null && state == St.Aim) { var d = AimDir(); if (d != null) Fire(d.Value); } aim = null; }
        }

        void Keys()
        {
            var k = Keyboard.current; if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame)
            {
                if (state == St.Aim || state == St.Fly) { paused = !paused; Show(paused ? ovPause : null); }
                else if (state == St.Title || state == St.Over) Close();
                return;
            }
            if (k.mKey.wasPressedThisFrame) MusicOn = !MusicOn;
            if (paused) return;
            if (k.qKey.wasPressedThisFrame) { if (k.shiftKey.isPressed) PullMany(); else PullRat(); }
            if (k.eKey.wasPressedThisFrame) OpenAug();
            if (k.fKey.wasPressedThisFrame) ToggleFast();
            if (k.spaceKey.wasPressedThisFrame) Recall();
            if (k.rKey.wasPressedThisFrame && state != St.Title) NewGame();
        }

        // ── 진행 ──
        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            Fit();
            Keys();
            if (state != St.Title && !paused && state != St.Over) Step(dt);
            else if (state == St.Over) StepLater(dt);
            Draw();
        }

        void Fit()
        {
            var pr = board ? board.parent as RectTransform : null; if (!pr) return;
            float s = Mathf.Min(pr.rect.width / W, pr.rect.height / H) * fitMargin;
            board.localScale = new Vector3(s, s, 1);
        }

        void StepLater(float dt)
        {
            for (int i = 0; i < later.Count; i++) { var l = later[i]; l.t -= dt; if (l.t <= 0 && !l.done) { l.done = true; l.f(); } }
            later.RemoveAll(l => l.done);
        }

        void Step(float dt)
        {
            time += dt;
            StepLater(dt);
            if (state == St.Fly)
            {
                flyT += dt;
                for (int m = 0; m < ffMul; m++)
                {
                    launchT -= dt;
                    while (launchQ.Count > 0 && launchT <= 0) { SpawnFlyer(launchQ[0]); launchQ.RemoveAt(0); launchT += LaunchInterval(); }
                    for (int i = 0; i < flyers.Count; i++) if (!flyers[i].done) FlyerStep(flyers[i], dt);
                    flyers.RemoveAll(f => f.done);
                }
                if (launchQ.Count == 0 && flyers.Count == 0 && later.Count == 0) EndTurn();
            }
            if (firstLandX != null)
                foreach (var w in walkers) { float dx = firstLandX.Value - w.x; if (Mathf.Abs(dx) < 4) w.done = true; else { w.x += Mathf.Sign(dx) * Mathf.Min(Mathf.Abs(dx), Cf("walk", 900) * dt); w.face = dx > 0 ? 1 : -1; } }
            walkers.RemoveAll(w => w.done);
            foreach (var b in bricks) { b.flash = Mathf.Max(0, b.flash - dt); b.vy += (CellY(b.r) - b.vy) * Mathf.Min(1, dt * 14); }
            bricks.RemoveAll(b => b.dead);
            float f60 = dt * 60;
            foreach (var p in parts) { p.life -= dt; p.x += p.vx * f60; p.y += p.vy * f60; p.vy += p.grav * f60; p.rot += p.vr * f60; }
            parts.RemoveAll(p => p.life <= 0);
            foreach (var f in floats) { f.t += dt; f.y -= 30 * dt; }
            floats.RemoveAll(f => f.t >= 1.1f);
            foreach (var c in cellFx) c.t -= dt; cellFx.RemoveAll(c => c.t <= 0);
            if (holdBuy >= 0) { holdBuy -= dt; if (holdBuy <= 0) { holdBuy = Cf("hold_repeat", 0.07f); if (!PullRat()) holdBuy = -1; } }
        }

        void Float(float x, float y, string text, Color col, bool small = false) => floats.Add(new Floater { x = x, y = y, text = text, col = col, small = small });
        static void Sfx(string n) { if (!string.IsNullOrEmpty(n)) SfxManager.Play(n); }

        void Show(GameObject ov)
        {
            foreach (var o in new[] { ovTitle, ovAug, ovPause, ovOver }) if (o) o.SetActive(o == ov);
        }

        // ── 그리기 (웹 draw: 매 프레임 풀에서 다시 놓음) ──
        Vector2 B(float x, float y) => new(x - W / 2, H / 2 - y);                                   // 판 픽셀 → 판 자식 UI
        Vector2 F(float x, float y) => new(x - (FX0 + FW / 2), (FY0 + FH / 2) - y);                  // 판 픽셀 → 경기장 자식 UI

        class Pool<T> where T : Component
        {
            readonly T tpl; readonly List<T> list = new(); int n;
            public Pool(T t) { tpl = t; }
            public void Begin() => n = 0;
            public T Get()
            {
                if (n == list.Count) list.Add(Instantiate(tpl, tpl.transform.parent));
                var x = list[n++]; if (!x.gameObject.activeSelf) x.gameObject.SetActive(true); return x;
            }
            public void End() { for (int i = n; i < list.Count; i++) if (list[i].gameObject.activeSelf) list[i].gameObject.SetActive(false); }
        }
        Pool<Image> pCell, pItem, pBrick, pAim, pBlade, pPart, pScreen, pAugRow; Pool<RawImage> pFly, pWalk; Pool<TMP_Text> pFloat;
        class BrickView { public Image tint, crack, flash; public TMP_Text hp; }
        readonly Dictionary<Image, BrickView> brickViews = new();

        void Draw()
        {
            if (pCell == null)
            {
                pCell = new(cellFxTpl); pItem = new(itemTpl); pBrick = new(brickTpl); pAim = new(aimTpl); pBlade = new(bladeTpl);
                pPart = new(partTpl); pScreen = new(screenPartTpl); pAugRow = new(augRowTpl); pFly = new(flyerTpl); pWalk = new(walkerTpl); pFloat = new(floatTpl);
            }
            // 칸 이펙트
            pCell.Begin();
            foreach (var c in cellFx) { var im = pCell.Get(); var col = c.col; col.a = Mathf.Min(1, c.t * 5) * 0.7f; im.color = col; Box(im, F(CellX(c.c) + CELL / 2, CellY(c.r) + CELL / 2), CELL - 4, CELL - 4); }
            pCell.End();
            // 치즈 조각
            pItem.Begin();
            foreach (var it in items) { var im = pItem.Get(); Spr(im, cheeseChunk, F(CellX(it.c) + CELL / 2, CellY(it.r) + CELL / 2 + Mathf.Sin(time * 4 + it.c) * 3), 34); }
            pItem.End();
            // 벽돌
            pBrick.Begin();
            foreach (var b in bricks)
            {
                var im = pBrick.Get(); var v = View(im);
                float s = CELL - 4, sq = b.flash > 0 ? 3 : 0;
                im.sprite = b.hard ? tileHard : tile;
                Box(im, F(CellX(b.c) + CELL / 2, b.vy + CELL / 2), s - sq * 2, s - sq * 2);
                if (v.tint) { v.tint.sprite = im.sprite; v.tint.color = BandColor(b.max); }
                float k = b.hp / b.max; var cr = k < 0.33f ? crackLow : k < 0.66f ? crackMid : null;
                if (v.crack) { v.crack.enabled = cr; if (cr) v.crack.sprite = cr; }
                if (v.flash) v.flash.enabled = b.flash > 0;
                if (v.hp) { SetText(v.hp, Mathf.Ceil(b.hp).ToString("0")); float fs = b.hp >= 1000 ? 20 : 25; if (v.hp.fontSize != fs) v.hp.fontSize = fs; }
            }
            pBrick.End();
            // 조준선
            pAim.Begin();
            if (state == St.Aim && !paused) { var d = AimDir(); if (d != null) DrawAim(d.Value); }
            pAim.End();
            // 나는 쥐 · 칼날 · 걷는 쥐
            pFly.Begin(); pBlade.Begin();
            foreach (var f in flyers)
            {
                var ri = pFly.Get(); RatPic(ri, f.code, f.grade, true, F(f.x, f.y), f.r * 4.6f, Mathf.Atan2(f.vy, f.vx), 1);
                if (!f.blades) continue;
                int n = (int)AV("blades"); float rad = Cf("blade_radius", 28), spin = Cf("blade_spin", 8);
                for (int i = 0; i < n; i++) { float a = time * spin + i * Mathf.PI * 2 / n; var bi = pBlade.Get(); Spr(bi, blade, F(f.x + Mathf.Cos(a) * rad, f.y + Mathf.Sin(a) * rad), 22, rot: a); }
            }
            pFly.End(); pBlade.End();
            pWalk.Begin();
            foreach (var w in walkers) RatPic(pWalk.Get(), w.code, w.grade, false, F(w.x, FLOOR - 13), 42, 0, w.face);
            pWalk.End();
            // 발사 지점의 쥐
            bool lead = state == St.Aim || state == St.AugPick || (state == St.Fly && launchQ.Count > 0);
            Rat first = null; foreach (var r in rats) if (first == null || r.id < first.id) first = r;
            if (launcher)
            {
                launcher.gameObject.SetActive(lead && first != null);
                if (lead && first != null) { var ad = AimDir(); RatPic(launcher, first.code, first.grade, false, B(launchX, FLOOR - 13), 46, 0, ad != null && ad.Value.x < 0 ? -1 : 1); }
            }
            if (launchCount)
            {
                int n = state == St.Fly ? launchQ.Count : rats.Count;
                launchCount.gameObject.SetActive(lead && n > 1);
                if (lead && n > 1) { launchCount.rectTransform.anchoredPosition = B(launchX, FLOOR + 22 - 6); Fill(launchCount, ("n", n)); }
            }
            // 이펙트 · 패널 · 화면 이펙트 · 떠오르는 글
            pPart.Begin(); pScreen.Begin();
            foreach (var p in parts)
            {
                if (!p.s) continue;
                float k = p.life / p.max; var im = p.screen ? pScreen.Get() : pPart.Get();
                Spr(im, p.s, B(p.x, p.y), p.size * (1 + p.grow * (1 - k)), p.h > 0 ? p.h : -1, k, p.rot);
            }
            pPart.End(); pScreen.End();
            Panels();
            pFloat.Begin();
            foreach (var f in floats)
            {
                var t = pFloat.Get(); SetText(t, f.text);
                var col = f.col; col.a = Mathf.Min(1, (1.1f - f.t) * 3); t.color = col;
                float fs = f.small ? floatSmall : floatBig; if (t.fontSize != fs) t.fontSize = fs;
                t.rectTransform.anchoredPosition = B(f.x, f.y - fs * 0.35f);
            }
            pFloat.End();
        }

        BrickView View(Image im)
        {
            if (brickViews.TryGetValue(im, out var v)) return v;
            v = new BrickView { tint = Child<Image>(im, "Tint"), crack = Child<Image>(im, "Crack"), flash = Child<Image>(im, "Flash"), hp = Child<TMP_Text>(im, "Hp") };
            brickViews[im] = v; return v;
        }

        // 벽돌 색 = 체력 구간 (웹 bandOf · 반투명 덧칠)
        Color BandColor(float max)
        {
            foreach (var b in bands) if (b.hp_below <= 0 || max < b.hp_below) return ColorUtility.TryParseHtmlString(b.color, out var c) ? c : Color.clear;
            return Color.clear;
        }

        // 점선: 쥐 크기 원을 조금씩 밀며 첫 충돌 찾기 (+ 짧은 반사)
        void DrawAim(Vector2 d)
        {
            float x = launchX, y = FLOOR - RatR, vx = d.x, vy = d.y, r = RatR, len = 0; int bounced = 0, cnt = 0;
            for (int i = 0; i < 1400 && bounced < 2; i++)
            {
                x += vx * 3; y += vy * 3; len += 3;
                bool hit = false;
                if (x - r < FX0) { x = FX0 + r; vx = Mathf.Abs(vx); hit = true; }
                if (x + r > FX0 + FW) { x = FX0 + FW - r; vx = -Mathf.Abs(vx); hit = true; }
                if (y - r < FY0) { y = FY0 + r; vy = Mathf.Abs(vy); hit = true; }
                if (y > FLOOR) break;
                foreach (var b in bricks) { float x0 = CellX(b.c) + 2, y0 = CellY(b.r) + 2; if (CollideRect(ref x, ref y, ref vx, ref vy, r, x0, y0, x0 + CELL - 4, y0 + CELL - 4)) hit = true; }
                if (i % 5 == 0) Dot(x, y, bounced > 0, false);
                if (hit) { bounced++; Dot(x, y, true, true); }
                if (bounced == 1 && len > 2000) break;
                if (bounced >= 1 && ++cnt > 40) break;
            }
        }
        void Dot(float x, float y, bool faint, bool big)
        {
            var im = pAim.Get(); var c = im.color; c.a = faint ? 0.35f : 0.85f; im.color = c;
            Box(im, F(x, y), big ? 12 : 7, big ? 12 : 7);
        }

        void Panels()
        {
            float price = Cf("rat_price", 10);
            SetText(cheeseText, LobbyManager.Fmt(cheese));
            if (buyButton) buyButton.interactable = state == St.Aim && cheese >= price;
            Fill(buySub, ("price", F0(price)), ("pity", Mathf.Max(0, (int)Cf("pity", 30) - pity)));
            Fill(ownedText, ("n", rats.Count));
            var cnt = new int[6]; var sample = new string[6];
            foreach (var r in rats) { cnt[r.grade]++; sample[r.grade] ??= r.code; }
            int sg = (int)Cf("skill_grade", 3);
            for (int g = 0; g < 6 && g < gradeRows.Length; g++)
            {
                var row = gradeRows[g]; if (!row) continue;
                var c = row.color; c.a = cnt[g] > 0 ? rowOn : rowOff; row.color = c;
                var pic = Child<RawImage>(row, "Pic"); if (pic) RatPic(pic, sample[g] ?? pool[g][0], g, false, pic.rectTransform.anchoredPosition, pic.rectTransform.sizeDelta.x, 0, 1, true);
                var nm = Child<TMP_Text>(row, "Name"); if (nm) { SetText(nm, GradeName(g)); nm.color = g == 0 ? commonNameColor : grades[g].Color; }
                Fill(Child<TMP_Text>(row, "Dmg"), ("n", F0(grades[g].dmg)));
                var sk = Child<TMP_Text>(row, "Skill"); if (sk) sk.gameObject.SetActive(g >= sg);
                Fill(Child<TMP_Text>(row, "Count"), ("n", cnt[g]));
            }
            for (int i = 0; i < recentPics.Length; i++)
            {
                var pic = recentPics[i]; if (!pic) continue;
                bool on = i < recent.Count && i < 8; pic.enabled = on;
                if (on) RatPic(pic, recent[i].code, recent[i].grade, false, pic.rectTransform.anchoredPosition, pic.rectTransform.sizeDelta.x, 0, 1, true);
            }
            // 오른쪽
            Fill(turnText, ("n", turn)); Fill(bestText, ("n", Best));
            int cost = AugCost(); bool can = rats.Count > cost;
            if (augButton) augButton.interactable = state == St.Aim && can;
            Fill(augLabel, ("n", cost));
            if (augSubUse) { augSubUse.gameObject.SetActive(can); if (can) Fill(augSubUse, ("list", MatText(AugMaterials()))); }
            if (augSubNeed) { augSubNeed.gameObject.SetActive(!can); Fill(augSubNeed, ("n", cost + 1)); }
            Fill(augCountText, ("n", augCount));
            pAugRow.Begin();
            int shown = 0;
            foreach (var a in augRows)
            {
                int n = A(a.key); if (n <= 0) continue; if (shown >= augRowMax) break;
                var row = pAugRow.Get(); var meta = AugMeta(a);
                var rt = row.rectTransform; var p0 = augRowTpl.rectTransform.anchoredPosition; rt.anchoredPosition = p0 - new Vector2(0, augRowStep * shown);
                var bar = Child<Image>(row, "Bar"); if (bar) bar.color = TierColor(a.tier);
                var ic = Child<Image>(row, "Icon"); if (ic) { ic.sprite = AugIcon(meta); ic.enabled = ic.sprite; ic.preserveAspect = true; }
                Fill(Child<TMP_Text>(row, "Name"), ("name", meta != null ? meta.aug_name : a.key), ("lv", n > 1 ? augLvText.Replace("{n}", n.ToString()) : ""));
                SetText(Child<TMP_Text>(row, "Desc"), AugDesc(a, n));
                shown++;
            }
            pAugRow.End();
            bool fly = state == St.Fly;
            if (ffButton)
            {
                ffButton.gameObject.SetActive(fly);
                if (fly)
                {
                    int mx = (int)Cf("ff_max", 4);
                    Fill(ffLabel, ("n", ffMul >= mx ? 1 : ffMul * 2));
                    if (ffButton.image) ffButton.image.color = flyT > 4 && Mathf.Sin(time * 8) > 0 ? ffBlink : ffColor;
                }
            }
            if (recallButton) recallButton.gameObject.SetActive(fly);
            if (aimHint) aimHint.SetActive(state == St.Aim);
        }

        // ── 놓기 도우미 ──
        static void Box(Graphic g, Vector2 pos, float w, float h)
        {
            var rt = g.rectTransform; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(w, h);
        }
        // 스프라이트 (가운데 = pos, 폭 w, 높이 h 없으면 그림 비율)
        static void Spr(Image im, Sprite s, Vector2 pos, float w, float h = -1, float alpha = 1, float rot = 0)
        {
            if (im.sprite != s) im.sprite = s;
            if (h < 0) h = s ? w * s.rect.height / s.rect.width : w;
            var c = im.color; c.a = alpha; im.color = c;
            var rt = im.rectTransform; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(w, h); rt.localRotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
        }
        // 쥐 그림 칸 (웹 drawRatAt: 가운데 = pos, 정사각 size, 회전 rot, face < 0 = 왼쪽을 봄). keepPos = 자리·크기는 씬 그대로
        void RatPic(RawImage ri, string code, int grade, bool air, Vector2 pos, float size, float rot, int face, bool keepPos = false)
        {
            var g = grades[grade]; var ac = g.Color; ac.a = grade > 0 ? g.aura_alpha : 0;
            if (JudolRatArt.Get(artLibrary ? artLibrary.Get(code) : null, code, grade, air, ac, g.aura_r, bakeMaterial, out var tex, out var uv))
            { if (ri.texture != tex) ri.texture = tex; ri.uvRect = uv; ri.color = Color.white; }
            else ri.color = Color.clear;
            var rt = ri.rectTransform;
            if (!keepPos) { rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(size, size); }
            rt.localRotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            rt.localScale = new Vector3(face < 0 ? -1 : 1, 1, 1);
        }

        static Sprite Circle()
        {
            const int N = 32; var tx = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "JudolDot", wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) px[y * N + x] = new Color(1, 1, 1, Mathf.Clamp01(N / 2f - new Vector2(x + 0.5f - N / 2f, y + 0.5f - N / 2f).magnitude));
            tx.SetPixels(px); tx.Apply();
            return Sprite.Create(tx, new UnityEngine.Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100);
        }

        static T Child<T>(Component c, string name) where T : Component { var t = c ? c.transform.Find(name) : null; return t ? t.GetComponent<T>() : null; }
        static void SetText(TMP_Text t, string s) { if (t && t.text != s) t.text = s; }
        readonly Dictionary<TMP_Text, string> tpls = new();
        // 씬 글 틀 (처음 한 번 기억, LocText 가 있으면 지금 언어 틀) 에 자리표시 채우기
        void Fill(TMP_Text t, params (string k, object v)[] kv)
        {
            if (!t) return;
            if (!tpls.TryGetValue(t, out var tpl)) tpls[t] = tpl = t.text;
            string s = LocText.Tpl(t, tpl);
            foreach (var (k, v) in kv) s = s.Replace("{" + k + "}", v?.ToString() ?? "");
            SetText(t, s);
        }
    }
}
