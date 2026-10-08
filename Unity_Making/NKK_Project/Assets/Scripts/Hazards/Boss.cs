using NKK.Data;
using NKK.Humans;
using NKK.Items;
using NKK.Rats;
using NKK.Stage;
using NKK.Ults;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Hazards
{
    // 층 보스 (웹게임 makeBoss · startBossFight · updateBoss · bossStompLand · bossDown 이식). 보스 테이블 Boss · Boss_Line · Atk_Type.
    // · 보스 층에 들어가면 계단 방에서 대기 (Wait) → 계단 방 벽이 무너지는 순간 전투 (Fight): 배너 + 대사 + 번쩍
    // · 전투: 가장 가까운 쥐 쪽으로 쿵쿵 걸어옴, 공격 간격마다 공격 (atk_type, atk2_chance 확률로 atk2_type). 공격 수치 = Atk_Type 시트
    //   Stomp/Pounce 점프 내려찍기 · Flask/Hairball/Fireball 투척 · Swing 휘두르기(몇 번에 한 번 경비원 호출) · Gravity 무중력 파동
    //   Baton 진압봉(앞쪽 반원) · Gas_Cloud 독가스 구름(바닥에 남음) · Briefcase 서류 가방 던지기
    // · 쿨타임 스킬 (skill_type, 밈): 전투 skill_first 초 뒤 처음, 그 뒤 skill_cd 초마다. 필살 패턴과 따로 돎
    //   Tung_Sahur 퉁퉁퉁 사후르 · Even_Cook 이븐하게 익혀드릴게요(불판) · Unbroken 중꺾마(피해 감소) · Happy_Jump 해피해피해피
    //   Buttered_Cat 무한동력 버터 고양이(가로축 회전) · Maxwell_Drop 맥스웰 고양이 낙하
    // · 리그: Human = 사람 리그(사람 그림 모음) · Cat = 고양이 리그(고양이 그림 모음, 웹 drawBossCat)
    // · 쥐가 들이받으면 피해 (Rat.BumpBoss), 필살기·슈퍼 점프 폭발도 (ItemManager.BlastActors). 체력 0 → 빙글빙글 하늘로 (Dying) → 격파 배너·치즈
    // · 살아 있는 동안 계단을 못 씀 (Blocking). 테스트 버튼은 화면 가운데에 불러 바로 싸움 (층 기록 없음)
    // 씬 오브젝트 하나 (사람 리그를 그대로 씀). 그림 = 사람 그림 모음(HumanArtLibrary)의 code_id
    public class Boss : MonoBehaviour
    {
        public static Boss Current { get; private set; }
        public enum BState { Off, Wait, Fight, Dying, Dead }

        [Header("연결")]
        public GameManager Game;
        public StageManager Stage;
        public RatManager Rats;
        public ItemManager Items;
        public UltimateManager Ults;
        public HumanRig rig;
        [Tooltip("고양이 보스 리그 (Cat)")] public RatRig catRig;
        [Tooltip("고양이 그림 모음 · 몸길이 기준")] public CatManager Cats;
        [Tooltip("접지 그림자")] public SpriteRenderer shadow;

        [Header("투사체 (Flask · Hairball · Fireball)")]
        [Tooltip("투사체 그림 틀 (꺼 둠, 복제해서 씀)")] public SpriteRenderer shotTemplate;
        public Sprite flaskSprite, hairballSprite, fireballSprite;
        [Tooltip("투사체 크기 (게임 단위) · 날아가는 시간 · 위로 · 중력")] public float shotSize = 70, shotTime = 0.7f, shotUp = 520, shotGravity = 1500;
        [Tooltip("떨어진 자리 얼룩 색 (플라스크 · 헤어볼 · 불덩이)")] public Color flaskSpill = new(0.62f, 0.84f, 0.66f, 0.45f), hairSpill = new(0.78f, 0.64f, 0.48f, 0.4f), fireSpill = new(0.89f, 0.6f, 0.35f, 0.35f);

        [Header("HUD 체력바 (꺼 둠)")]
        public GameObject bar;
        [Tooltip("이름 (자리표시 {name})")] public TMP_Text barName;
        [Tooltip("채움 (가로 앵커로 줄어듦, 색 = 보스 테마 색)")] public RectTransform barFill;
        [Tooltip("맞을 때 번쩍이는 흰 덮개 (채움과 같은 크기)")] public Image barHit;
        [Tooltip("깎인 체력 자국 (채움 뒤, 잠깐 있다가 천천히 따라 줄어듦)")] public RectTransform barTrail;
        [Tooltip("자국이 따라오기 전 기다림 (초) · 따라오는 빠르기")] public float trailDelay = 0.45f, trailSpeed = 2.5f;
        [Tooltip("보스 엠블럼 (맞을 때 흔들림)")] public RectTransform barEmblem;
        float trailK = 1, trailWait, fightStart;

        [Header("글 (인스펙터, 자리표시 {name} {title} {floor})")]
        [Tooltip("보스 층에 들어갈 때 배너 부제")] public string floorSub;
        [Tooltip("전투 시작 배너 부제")] public string fightSub;
        public string downTitle, downSub;
        [Tooltip("테스트 보스 격파 부제")] public string testDownSub;
        [Tooltip("내려찍기 착지 팝업")] public string stompPopup;
        [Tooltip("고양이 덮치기 착지 팝업")] public string pouncePopup;
        [Tooltip("휘두르기 팝업")] public string swingPopup;
        [Tooltip("무중력 파동 팝업")] public string gravityPopup;
        [Tooltip("헤어볼 떨어질 때 팝업")] public string hairPopup;
        [Tooltip("경비원 부를 때 말")] public string guardCall;
        [Tooltip("치즈 팝업 (자리표시 {n})")] public string cheesePopup;

        [Header("수치")]
        [Tooltip("계단 기준 대기 위치 (게임 단위, 아래로 +)")] public float waitOffsetY = 220;
        [Tooltip("보스 층에 들어가면 카메라가 계단 방의 보스를 잠깐 비춤 (켜기 · 몇 초 뒤 = 화면이 밝아진 뒤)")] public bool peekOnEnter = true;
        public float peekDelay = 0.7f;
        [Tooltip("대기 중 계단 방이 아직 안 열렸지만 옆 방이 열려 어둡게 보일 때 보스 색 (실루엣). 계단 방이 안 보이면 보스도 숨김")] public Color waitDarkTint = new(0.22f, 0.22f, 0.26f, 1f);
        readonly System.Collections.Generic.List<(SpriteRenderer r, Color c)> baseColors = new();
        [Tooltip("점프 세기 (내려찍기 · 덮치기) · 중력")] public float jumpV = 900, pounceV = 700, gravity = 1800;
        [Tooltip("휘두르기에 맞은 쥐 날리는 속도 · 위로")] public float swingSpeed = 520, swingUp = 420;
        [Tooltip("무중력: 쥐 떠오르는 속도 최소·최대 · 물건 날리는 속도")] public float gravUpMin = 600, gravUpMax = 850, gravItem = 200;
        [Tooltip("휘두르기 몇 번마다 경비원 호출 (0 = 안 부름, 호출 수 = Atk_Type count)")] public int guardEvery = 2;
        [Tooltip("고양이 보스 말 높이 (몸길이 배율)")] public float catSayLift = 0.9f;
        [Tooltip("쥐를 날리는 속도 · 위로")] public float ragdollSpeed = 480, ragdollUp = 460;
        [Tooltip("물건 날리는 속도")] public float itemLaunch = 400;
        [Tooltip("쓰러질 때 위로 날아가는 속도 · 회전 · 사라지는 높이")] public float downV = 1500, downSpin = 9, downHeight = 2400;
        [Tooltip("피해 숫자 팝업 확률 · 맞을 때 대사 확률")] public float dmgPopupChance = 0.25f, hitLineChance = 0.08f;

        [Header("필살 패턴 (스테이지 테이블 Boss special1/2 · Atk_Type, 체력 70% · 35% 에서 발동)")]
        [Tooltip("필살 배너 부제 (자리표시 {name})")] public string specialSub = "{name}의 필살 패턴!!";
        [Tooltip("개구리 저주 팝업")] public string frogPopup = "개굴!";
        [Tooltip("상자 폭발 팝업")] public string boxPopup = "짜잔!!";
        [Tooltip("거대화: 크기 배율 · 걷기 빠르기 배율")] public float growScale = 1.6f, growSpeedMul = 1.8f;
        [Tooltip("우다다 질주 속도 · 블랙홀 빨아들이는 속도")] public float zoomSpeed = 1150, holePull = 700;
        [Tooltip("떨어지는 그림: 고깔 · 서류 더미 / 상자 · 약병 · 개구리")] public Sprite coneSprite, paperSprite, boxSprite, potionSprite, frogSprite;
        [Tooltip("상자·약병을 보여 줄 그림 (꺼 둠) · 개구리 표시 틀 (꺼 둠, 복제)")] public SpriteRenderer specialProp, markTemplate;
        [Tooltip("상자 크기 · 약병 크기 · 개구리 크기 (게임 단위)")] public float boxSize = 360, potionSize = 70, frogSize = 46;
        bool sp1Done, sp2Done, boxed; string pendingSpecial;
        float growT, growK = 1, pulseEvery, pulseT;
        int seqI, released; float phaseT, lx0, ly0, ltx, lty;
        readonly System.Collections.Generic.List<Vector2> seqTargets = new();
        class Mark { public SpriteRenderer r; public Rat rat; public float life; }
        readonly System.Collections.Generic.List<Mark> marks = new();

        [Header("두 번째 공격 · 쿨타임 스킬 (보스 테이블 atk2_type · skill_type, 수치 = Atk_Type)")]
        [Tooltip("진압봉 · 서류 가방 · 토치 · 버터 고양이 · 맥스웰 · 불판 자국 · 독가스 구름")] public Sprite batonSprite, briefcaseSprite, torchSprite, butterCatSprite, maxwellSprite, grillSprite, gasSprite;
        [Tooltip("머리 위 소품 크기: 진압봉 · 토치 · 서류 가방 (게임 단위)")] public float batonSize = 120, torchSize = 90, briefcaseSize = 95;
        [Tooltip("스킬 쓸 때 머리 위 이름 팝업 (자리표시 {skill})")] public string skillPopup;
        [Tooltip("퉁퉁퉁 사후르: 퉁 팝업 · 마지막 팝업")] public string tungPopup, sahurPopup;
        [Tooltip("퉁 기절 배율 (마지막 대비) · 첫 퉁 반경 배율")] public float tungStunMul = 0.35f, tungRadStart = 0.45f;
        [Tooltip("바닥에 남는 구역: 독가스 · 불판 안에 이 초 머문 쥐 기절")] public float gasExpose = 0.5f, grillExpose = 1f;
        [Tooltip("독가스 · 불판 기절 팝업")] public string gasPopup, grillPopup;
        [Tooltip("독가스 구름 색 · 불판 불꽃 색")] public Color gasColor = new(0.62f, 0.84f, 0.45f, 0.75f), grillColor = new(0.98f, 0.62f, 0.25f, 1f);
        [Tooltip("중꺾마: 받는 피해 배율")] public float unbrokenDmgMul = 0.4f;
        [Tooltip("중꺾마: 오라 색")] public Color unbrokenColor = new(1f, 0.82f, 0.3f, 1f);
        [Tooltip("중꺾마: 시작 팝업")] public string unbrokenPopup;
        [Tooltip("해피해피해피: 착지 팝업")] public string happyPopup;
        [Tooltip("해피해피해피: 쥐 쪽으로 가는 속도")] public float hopMove = 260;
        [Tooltip("버터 고양이: 크기 · 나는 속도 · 회전(초당 바퀴) · 떠 있는 높이")] public float butterSize = 110, butterSpeed = 380, butterSpin = 2.2f, butterHover = 60;
        [Tooltip("버터 고양이: 날릴 때 팝업")] public string butterPopup;
        float skillCd, unbrokenT;
        class Zone { public float x, y, rad, life, life0, stun, expose; public bool gas; public SpriteRenderer r; public readonly System.Collections.Generic.Dictionary<Rat, float> stay = new(); }
        readonly System.Collections.Generic.List<Zone> zones = new();
        class Butter { public float x, y, vx, vy, t, life; public SpriteRenderer r; }
        readonly System.Collections.Generic.List<Butter> butters = new();

        [Header("상태 (보기용)")]
        public BState State = BState.Off;
        public float x, y, z, vx, vy, vz, hp, hpMax;
        public BossRow Data { get; private set; }
        public bool Test { get; private set; }

        // 이 층 보스를 잡았는지 (층에 들어갈 때 초기화). 보스 층에서 안 잡았으면 계단 못 씀 (어떤 이유로 보스가 사라져도)
        public bool DefeatedThisFloor { get; private set; }
        int bossFloor = -1;
        public bool FloorCleared(int f) => bossFloor != f || DefeatedThisFloor;
        public bool Blocking => State == BState.Wait || State == BState.Fight || State == BState.Dying;   // 계단 막음
        public bool CanHit => State == BState.Fight;
        public float R => Items.humanRadius * (Data != null ? Data.radius_mul : 1) * growK;
        bool IsSpecial(string t) => Data != null && !string.IsNullOrEmpty(t) && (t == Data.special1_type || t == Data.special2_type);
        bool IsSkill(string t) => Data != null && !string.IsNullOrEmpty(t) && t == Data.skill_type;

        int face = 1, shots, swings;
        float t, walk, atkCd, atkT, rot, vr, jit, sq = 1, hitT, sayCD, value;
        bool attacking, air, atkHit;
        string atkType;                 // 지금 하는 공격 (Atk_Type)
        BossAtkRow atk;
        class Shot { public float x, y, z, vx, vy, vz, rot; public string type; public SpriteRenderer r; }
        readonly System.Collections.Generic.List<Shot> flying = new();
        bool Cat => Data != null && Data.IsCat;
        float Height => Cat ? (Cats ? Cats.catLength : 115) * Data.scale * catSayLift : rig.height;
        string nameFormat;
        BossRow stash;          // 테스트 보스를 부를 때 이 층 진짜 보스 (끝나면 되돌림)
        int testN = -1;
        float stashHp;

        static GameDatabase DB => GameDatabase.Instance;

        void Awake()
        {
            if (barName) nameFormat = barName.text;
            if (bar) bar.SetActive(false);
            SetVisible(false);
        }
        void OnDestroy() { if (Current == this) Current = null; }

        void SetVisible(bool on)
        {
            if (rig) rig.gameObject.SetActive(on && !Cat);
            if (catRig) catRig.gameObject.SetActive(on && Cat);
            if (shadow) shadow.enabled = on;
        }

        string Fill(string s) => (s ?? "").Replace("{name}", Data?.boss_name).Replace("{floor}", (Game.Floor + 1).ToString())
            .Replace("{title}", Data != null ? DB.BossLine("Intro", Data.boss_id) : "");

        public float HpFor(BossRow row, int f) => Mathf.Max(1, Stage.PowNeed(Mathf.Max(f, row.floor)) * row.hp_pow_sec);

        // ── 등장 ──
        public void Spawn(BossRow row, float px, float py, bool test)
        {
            Data = row; Test = test; State = BState.Wait;
            x = px; y = py; z = vz = vx = vy = 0; rot = vr = 0; t = 0; jit = 0; sq = 1; attacking = air = false; atkCd = Random.Range(row.atk_cd_min, row.atk_cd_max);
            hpMax = hp = (test ? Mathf.Max(1, Stage.PowNeed(Game.Floor) * row.hp_pow_sec) : HpFor(row, Game.Floor));   // 테스트 = 지금 층 기준 (잡을 수 있게)
            value = 3 * Stage.CheeseK(test ? Game.Floor : Mathf.Max(Game.Floor, row.floor)) * row.cheese_mul * CommonSkill.CreatureCheeseMul;
            shots = swings = 0; atkType = null; atk = null;
            sp1Done = sp2Done = boxed = false; pendingSpecial = null; growT = 0; growK = 1; ClearMarks();
            skillCd = row.skill_first; unbrokenT = 0;
            if (row.IsCat)
            {
                var art = Cats && Cats.catArt ? Cats.catArt.Get(row.code_id) : null;
                if (art == null || !catRig) { Debug.LogWarning("[Boss] 고양이 그림 없음: " + row.code_id); State = BState.Off; return; }
                catRig.Build(art, Cats.catLength * row.scale, -1, false);      // 가까운 다리는 몸통 앞, 먼 다리만 뒤 (입체)
            }
            else
            {
                var art = Items.humanArt ? Items.humanArt.Get(row.code_id) : null;
                if (art == null) { Debug.LogWarning("[Boss] 그림 없음: " + row.code_id); State = BState.Off; return; }
                rig.height = Items.humanHeight * row.scale;
                rig.Build(art);
            }
            SetVisible(true);
            baseColors.Clear();
            foreach (var r in (row.IsCat ? (Component)catRig : rig).GetComponentsInChildren<SpriteRenderer>(true)) baseColors.Add((r, r.color));
            Current = this;
        }

        // 층에 들어갈 때 (StageManager.EnterFloor): 보스 층이면 계단 방에 대기
        public void OnFloorEnter()
        {
            Clear();
            DefeatedThisFloor = false; bossFloor = -1;
            var row = DB.BossOf(Game.Floor);
            if (row == null) return;
            bossFloor = Game.Floor;
            var sp = Stage.StairsPos;
            Spawn(row, sp.x, sp.y + waitOffsetY, false);
            Debug.Log($"[Boss] {Game.Floor}층 보스 {row.boss_name} → {State}");
            if (State != BState.Wait) return;
            Game.ShowBanner(Game.BannerTitle, Fill(floorSub));
            // 계단 방은 멀리 있어 카메라(열린 방 안만 움직임)로는 안 보임 → 잠깐 비춰서 보스가 있다는 걸 보여 줌
            if (peekOnEnter && Game.cam && !BalanceProbe.Active) Game.cam.Peek(x, y - Height * 0.4f, peekDelay);
        }

        public void Clear()
        {
            State = BState.Off; Data = null; stash = null; Test = false;
            SetVisible(false); ClearShots(); EndSpecialFx();
            if (bar) bar.SetActive(false);
            if (Current == this) Current = null;
        }

        // 계단 방이 열림 (StageManager.BreakWall): 대기 중이면 전투. 이 층 보스를 아직 안 잡았는데 보스가 없으면 다시 불러서 전투 (안전장치)
        public void OnStairsOpened()
        {
            if (Test || bossFloor != Game.Floor || DefeatedThisFloor) return;
            if (State == BState.Off || State == BState.Dead)
            {
                Debug.LogWarning($"[Boss] {Game.Floor}층 계단 방이 열렸는데 보스가 {State} → 다시 불러옴");
                var row = DB.BossOf(Game.Floor); if (row == null) return;
                var sp = Stage.StairsPos; Spawn(row, sp.x, sp.y + waitOffsetY, false);
                if (State != BState.Wait) { Debug.LogError($"[Boss] {Game.Floor}층 보스를 못 불러옴 (그림 없음?) → 이 층은 통과 허용"); DefeatedThisFloor = true; return; }
            }
            StartFight();
        }

        public void StartFight()
        {
            if (State != BState.Wait) return;
            if (!Test) Debug.Log($"[Boss] {Game.Floor}층 보스전 시작 (체력 {GameManager.Format(hpMax)})");
            fightStart = Time.time;
            State = BState.Fight; t = 0; skillCd = Data.skill_first;
            string intro = DB.BossLine("Intro", Data.boss_id);
            Game.ShowBanner(Data.boss_name, Fill(fightSub));
            Say(intro, 2.4f);
            if (Ults) Ults.Flash(new Color(0.91f, 0.47f, 0.42f), 0.3f);
            FxManager.I?.Shake(0.4f);
            if (Game.cam) Game.cam.CenterOn(x, y);
            trailK = 1; trailWait = 0;
            if (bar) { bar.SetActive(true); if (barName) barName.text = (nameFormat ?? "{name}").Replace("{name}", Data.boss_name); }
        }

        // 테스트 버튼: 화면 가운데에 불러서 바로 싸움 (이 층 보스가 대기 중이었으면 끝난 뒤 되돌림)
        public void TestSpawn()
        {
            if (State == BState.Fight || State == BState.Dying || DB.Bosses.Count == 0) return;
            BossRow keep = State == BState.Wait && !Test ? Data : null;
            testN = (testN + 1) % DB.Bosses.Count;          // 누를 때마다 다음 보스 (웹 bossTestN)
            var row = DB.Bosses[testN];
            var c = Ults ? Ults.ViewRect(0).center : new Vector2(World.RW / 2, World.RH / 2);
            Spawn(row, c.x, c.y + 60, true);
            stash = keep;
            FxManager.I?.Dust(x, y, 16, 2.4f);
            StartFight();
        }

        void Say(string line, float life = 1.2f)
        {
            if (string.IsNullOrEmpty(line) || sayCD > 0) return;
            FxManager.I?.Popup(x, y, line, Color.white, 22, life, Height * 1.05f);
            sayCD = 1.2f;
        }

        // ── 피해 (쥐 들이받기·폭발) ──
        public bool Damage(float dmg, Rat by, float ang, bool crit)
        {
            if (!CanHit || boxed) return false;
            dmg *= CommonSkill.BossDmgMul;
            if (unbrokenT > 0) dmg *= unbrokenDmgMul;          // 중꺾마
            hp -= dmg; hitT = 0.25f; jit = 3;
            // 필살 패턴: 체력 70% · 35% 아래로 처음 내려가면 다음 공격으로 바로
            if (!sp1Done && hp < hpMax * 0.7f && Valid(Data.special1_type)) { sp1Done = true; pendingSpecial = Data.special1_type; atkCd = Mathf.Min(atkCd, 0.3f); }
            else if (!sp2Done && hp < hpMax * 0.35f && Valid(Data.special2_type)) { sp2Done = true; pendingSpecial = Data.special2_type; atkCd = Mathf.Min(atkCd, 0.3f); }
            if (unbrokenT <= 0) { vx += Mathf.Cos(ang) * 60; vy += Mathf.Sin(ang) * 60; }
            var fx = FxManager.I;
            bool vis = Ults && Ults.OnScreen(x, y, 0);
            if (fx && vis && Random.value < dmgPopupChance) fx.Popup(x + Random.Range(-30f, 30f), y, "-" + GameManager.Format(dmg), crit ? new Color(0.95f, 0.76f, 0.31f) : Color.white, crit ? 22 : 16, 0.6f, 150 + Random.Range(0f, 60f));
            if (vis && Random.value < hitLineChance) Say(DB.BossLine("Hit", Data.boss_id), 0.8f);
            if (by) Ults?.Charge(by, CondType.Hit_Boss);
            if (hp <= 0) Down();
            return true;
        }

        void Down()
        {
            State = BState.Dying; t = 0; hp = 0; attacking = false; EndSpecialFx(); unbrokenT = 0;
            if (!Test && bossFloor == Game.Floor) DefeatedThisFloor = true;
            Debug.Log($"[Boss] {Game.Floor}층 보스 격파{(Test ? " (테스트)" : "")} · 전투 {Time.time - fightStart:0}초");
            vx = Random.Range(-120f, 120f); vy = -40; vz = downV; vr = downSpin; ClearShots();
            if (!Test) Game.ShowBanner(Fill(downTitle), Fill(downSub));
            else Game.ShowBanner(Fill(downTitle), Fill(testDownSub));
            Game.OnSmash(value, 5);
            sayCD = 0; Say(DB.BossLine("Down", Data.boss_id), 2);
            var fx = FxManager.I;
            if (fx)
            {
                if (!string.IsNullOrEmpty(cheesePopup)) fx.Popup(x, y, cheesePopup.Replace("{n}", GameManager.Format(value * Game.ComboMult)), new Color(0.95f, 0.76f, 0.31f), 40, 2, 200);
                fx.Coin(x, y, 20); fx.Hitstop(0.35f); fx.Shake(0.5f);
                fx.Stars(x, y, 100, 30, Color.white, new Color(0.95f, 0.76f, 0.31f), 300, 900);
            }
            if (Ults) Ults.Flash(Color.white, 0.7f);
        }

        // ── 매 프레임 ──
        void Update()
        {
            if (State == BState.Off || State == BState.Dead) return;
            float udt = Time.unscaledDeltaTime;
            UpdateBar(udt);
            if (FxManager.WorldFreeze || GameOver.Active) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            t += dt; sayCD -= dt; hitT = Mathf.Max(0, hitT - dt); jit = Mathf.Max(0, jit - dt * 12);
            sq += (1 - sq) * Mathf.Min(1, dt * 8);
            if (State == BState.Wait) { vx = vy = 0; face = 1; walk = 0; return; }
            if (State == BState.Dying)
            {
                vz -= 300 * dt; x += vx * dt; y += vy * dt; z += vz * dt; rot += vr * dt;
                if (z > downHeight) Finish();
                return;
            }
            float px = x, py = y;
            UpdateShots(dt); UpdateGrow(dt); UpdateZones(dt); UpdateButters(dt);
            if (unbrokenT > 0 && (unbrokenT -= dt) > 0 && Random.value < dt * 6) FxManager.I?.Stars(x, y, Height * 0.5f, 2, unbrokenColor, Color.white, 60, 160);
            if (z > 0 || vz > 0) { vz -= gravity * dt; z = Mathf.Max(0, z + vz * dt); if (z <= 0) { vz = 0; if (air) Land(); } }
            if (!attacking)
            {
                var r = Rats.NearestRat(x, y, 2000);
                if (r) { float a = Mathf.Atan2(r.y - y, r.x - x), s = Data.move_speed * (growT > 0 ? growSpeedMul : 1); vx += (Mathf.Cos(a) * s - vx) * Mathf.Min(1, dt * 3); vy += (Mathf.Sin(a) * s - vy) * Mathf.Min(1, dt * 3); }
                atkCd -= dt; skillCd -= dt;
                bool skill = skillCd <= 0 && pendingSpecial == null && Valid(Data.skill_type);
                if ((atkCd <= 0 || skill) && !(Ults && Ults.Busy))
                {
                    bool two = !string.IsNullOrEmpty(Data.atk2_type) && Data.atk2_type != "None" && Random.value < Data.atk2_chance;
                    atkType = two ? Data.atk2_type : Data.atk_type;
                    // 필살: 체력 문턱이면 무조건, 둘 다 쓴 뒤엔 special_chance 확률로 둘 중 하나
                    if (pendingSpecial != null) { atkType = pendingSpecial; pendingSpecial = null; }
                    else if (skill) { atkType = Data.skill_type; skillCd = Mathf.Max(1, Data.skill_cd); }
                    else if (sp1Done && sp2Done && Random.value < Data.special_chance) atkType = Random.value < 0.5f ? Data.special1_type : Data.special2_type;
                    atk = DB.BossAtk(atkType);
                    attacking = true; air = false; atkHit = false; shots = 0; atkT = 0; atkCd = Random.Range(Data.atk_cd_min, Data.atk_cd_max); vx = vy = 0;
                    if (IsSpecial(atkType)) BeginSpecial();
                    else if (IsSkill(atkType)) BeginSkill();
                    else BeginAttack();
                }
            }
            else if (atk == null) attacking = false;
            else
            {
                atkT += dt; vx *= 0.85f; vy *= 0.85f;
                switch (atkType)
                {
                    case "Stomp":
                    case "Pounce":
                        // 웅크림 (덜덜) → 가장 가까운 쥐 쪽으로 점프 (고양이는 낮고 빠르게)
                        if (atkT < atk.windup) jit = atkType == "Pounce" ? 1 : 3;
                        else if (!air && z <= 0 && atkT < atk.windup + 0.1f)
                        {
                            air = true; vz = atkType == "Pounce" ? pounceV : jumpV;
                            var r = Rats.NearestRat(x, y, 900);
                            if (r) { vx = (r.x - x) * 1.2f; vy = (r.y - y) * 1.2f; }
                        }
                        if (atkT > atk.dur && z <= 0) attacking = false;
                        break;
                    case "Flask":
                    case "Hairball":
                    case "Fireball":
                        // 덜덜 → 화면 속 쥐에게 0.22초 간격으로 던짐
                        jit = atkT < atk.windup ? 2 : 0;
                        if (atkT > atk.windup + shots * 0.22f && shots < atk.count)
                        {
                            shots++;
                            var r = Rats.RandomOnScreen() ?? Rats.NearestRat(x, y, 2000);
                            if (r)
                            {
                                float tx = r.x + Random.Range(-40f, 40f), ty = r.y + Random.Range(-30f, 30f);
                                if (!Stage.Open.Contains(StageManager.RoomOf(tx, ty))) { tx = r.x; ty = r.y; }      // 방 바깥(벽 너머)엔 안 떨어지게
                                Throw(tx, ty, atkType);
                            }
                        }
                        if (atkT > atk.dur) attacking = false;
                        break;
                    case "Swing":
                        if (atkT < atk.windup) jit = 2.5f;
                        else if (!atkHit) { atkHit = true; Swing(); }
                        if (atkT > atk.dur) attacking = false;
                        break;
                    case "Gravity":
                        if (atkT < atk.windup) jit = 2;
                        else if (!atkHit) { atkHit = true; GravityWave(); }
                        if (atkT > atk.dur) attacking = false;
                        break;
                    case "Baton":
                        // 진압봉: 머리 위로 들었다가 바라보는 쪽으로 크게
                        if (atkT < atk.windup) jit = 2.5f;
                        else if (!atkHit) { atkHit = true; HideProp(); BatonHit(); }
                        if (atkT > atk.dur) attacking = false;
                        break;
                    case "Gas_Cloud":
                        // 독가스 플라스크 개수만큼 → 떨어진 곳에 구름 (지속 = dur)
                        jit = atkT < atk.windup ? 2 : 0;
                        if (atkT > atk.windup + shots * 0.3f && shots < atk.count)
                        {
                            shots++;
                            var r = Rats.RandomOnScreen() ?? Rats.NearestRat(x, y, 2000);
                            if (r) Throw(r.x, r.y, atkType);
                        }
                        if (shots >= atk.count && atkT > atk.windup + atk.count * 0.3f + 0.4f) attacking = false;
                        break;
                    case "Briefcase":
                        // 서류 가방: 머리 위로 들었다가 쥐 무리에 던짐
                        jit = atkT < atk.windup ? 2 : 0;
                        if (atkT >= atk.windup && shots == 0)
                        {
                            shots = 1; HideProp();
                            Vector2 at;
                            if (!(Cats && Cats.FindCrowd(x, y, out at, out _))) { var r = Rats.NearestRat(x, y, 2000); at = r ? new Vector2(r.x, r.y) : new Vector2(x + face * 200, y); }
                            if (!Stage.Open.Contains(StageManager.RoomOf(at.x, at.y))) { var r = Rats.NearestRat(x, y, 2000); if (r) at = new Vector2(r.x, r.y); }
                            Throw(at.x, at.y, atkType);
                        }
                        if (atkT > atk.dur) attacking = false;
                        break;
                    default:
                        if (IsSpecial(atkType) || IsSkill(atkType)) UpdateSpecial(dt); else attacking = false;
                        break;
                }
            }
            x += vx * dt; y += vy * dt;
            Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 0.3f);
            if (Mathf.Abs(vx) > 8) face = vx > 0 ? 1 : -1;
            walk += dt * Mathf.Sqrt(vx * vx + vy * vy) / 16;
        }

        // ── 필살 패턴 ──
        bool Valid(string t) => !string.IsNullOrEmpty(t) && t != "None" && DB.BossAtk(t) != null;
        string SpName => atk != null && !string.IsNullOrEmpty(atk.atk_name) ? atk.atk_name : atkType;

        void BeginSpecial()
        {
            seqI = 0; released = 0; phaseT = 0; seqTargets.Clear();
            Game.ShowBanner(SpName, Fill(specialSub));
            sayCD = 0; Say(DB.BossLine("Special", Data.boss_id), 1.6f);
            if (Ults) Ults.Flash(Data.Color, 0.25f);
            FxManager.I?.Shake(0.3f);
            int n = Mathf.Max(1, atk.count);
            switch (atkType)
            {
                case "Mega_Stomp": case "Cone_Rain": case "Paper_Storm": case "Meteor_Shower": case "Orbital_Laser":
                    PickTargets(n); break;
                case "Black_Hole": PickTargets(1); break;
                case "Potion_Party": case "Self_Experiment": ShowProp(potionSprite, potionSize, true); break;
                case "Box_Fit":
                    boxed = true; SetVisible(false); if (shadow) shadow.enabled = true; ShowProp(boxSprite, boxSize, false);
                    FxManager.I?.Dust(x, y, 14, 2); break;
            }
        }

        // 쥐가 많이 모인 곳 n 군데 (부족하면 그 근처 흩뿌림)
        void PickTargets(int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (Cats && Cats.FindCrowd(x, y, out var at, out _, seqTargets)) { seqTargets.Add(at); continue; }
                var r = Rats.RandomOnScreen() ?? Rats.NearestRat(x, y, 3000);
                if (r) seqTargets.Add(new Vector2(r.x + Random.Range(-90f, 90f), r.y + Random.Range(-60f, 60f)));
                else if (seqTargets.Count > 0) seqTargets.Add(seqTargets[0] + Random.insideUnitCircle * 200);
                else seqTargets.Add(new Vector2(x, y));
            }
            for (int i = 0; i < seqTargets.Count; i++)
            {
                var v = seqTargets[i];
                if (!Stage.Open.Contains(StageManager.RoomOf(v.x, v.y))) seqTargets[i] = new Vector2(x, y);
            }
        }

        void ShowWarns(int from, float k)
        {
            if (!Cats) return;
            int n = 0;
            for (int i = from; i < seqTargets.Count; i++) Cats.ShowWarn(n++, seqTargets[i].x, seqTargets[i].y, atk.radius, k);
            Cats.HideWarns(n);
        }

        void UpdateSpecial(float dt)
        {
            var fx = FxManager.I;
            float wind = Mathf.Max(0.1f, atk.windup), k = Mathf.Clamp01(atkT / wind);
            switch (atkType)
            {
                case "Mega_Stomp":
                {
                    // 경고 원 → 점프 → 착지, 목표마다 반복
                    if (seqI >= seqTargets.Count) { Cats?.HideWarns(); attacking = false; break; }
                    phaseT += dt;
                    if (!air)
                    {
                        jit = 3; ShowWarns(seqI, Mathf.Clamp01(phaseT / wind));
                        if (phaseT >= wind) { air = true; phaseT = 0; lx0 = x; ly0 = y; ltx = seqTargets[seqI].x; lty = seqTargets[seqI].y; vz = jumpV * 1.1f; }
                    }
                    else
                    {
                        float T = 2 * jumpV * 1.1f / gravity, kk = Mathf.Clamp01(phaseT / T);
                        x = Mathf.Lerp(lx0, ltx, kk); y = Mathf.Lerp(ly0, lty, kk); vx = vy = 0;
                        ShowWarns(seqI, 1);
                        if (kk >= 1 || (z <= 0 && phaseT > 0.1f)) { z = 0; vz = 0; air = false; phaseT = 0; SpecialHit(x, y, atk.radius, atk.stun, true); seqI++; }
                    }
                    break;
                }
                case "Cone_Rain": case "Paper_Storm": case "Meteor_Shower": case "Orbital_Laser": case "Maxwell_Drop":
                {
                    jit = atkT < wind ? 2 : 0;
                    ShowWarns(released, k);
                    if (atkT < wind) break;
                    float gap = Mathf.Max(0.05f, atk.dur / Mathf.Max(1, seqTargets.Count));
                    while (released < seqTargets.Count && atkT >= wind + released * gap)
                    {
                        var v = seqTargets[released++];
                        bool laser = atkType == "Orbital_Laser";
                        var sp = atkType == "Cone_Rain" ? coneSprite : atkType == "Paper_Storm" ? paperSprite : atkType == "Maxwell_Drop" ? maxwellSprite : null;
                        Cats?.AddStrike(!laser, v.x, v.y, atk.radius, atk.stun, SpName, sp, atkType == "Meteor_Shower");
                    }
                    if (released >= seqTargets.Count) { Cats?.HideWarns(); attacking = false; }
                    break;
                }
                case "Potion_Party":
                {
                    // 제자리에서 빙글 돌며 나선으로 사방에 던짐
                    jit = atkT < wind ? 2 : 0; face = Mathf.Sin(atkT * 14) > 0 ? 1 : -1;
                    if (atkT < wind) break;
                    float gap = atk.dur / Mathf.Max(1, atk.count);
                    while (shots < atk.count && atkT >= wind + shots * gap)
                    {
                        float a = shots * 2.4f, d = 180 + (shots % 4) * 90;
                        float tx = x + Mathf.Cos(a) * d, ty = y + Mathf.Sin(a) * d * 0.8f;
                        if (!Stage.Open.Contains(StageManager.RoomOf(tx, ty))) { var r = Rats.NearestRat(x, y, 900); if (!r) { shots++; continue; } tx = r.x; ty = r.y; }
                        Throw(tx, ty, "Flask"); shots++;
                    }
                    if (shots >= atk.count) { HideProp(); attacking = false; }
                    break;
                }
                case "Self_Experiment":
                    // 약 마시기 → 거대화 (배경에서 dur 초 동안, 주기마다 충격파)
                    jit = 2.5f;
                    if (atkT >= wind) { HideProp(); growT = atk.dur; pulseEvery = atk.dur / Mathf.Max(1, atk.count); pulseT = pulseEvery * 0.5f; fx?.Stars(x, y, Height, 30, Data.Color, Color.white, 200, 500); fx?.Shake(0.4f); fx?.Popup(x, y, SpName, Data.Color, 34, 1, Height); attacking = false; }
                    break;
                case "Board_Meeting":
                    jit = atkT < wind ? 3 : 0;
                    if (!atkHit && atkT >= wind)
                    {
                        atkHit = true; SpecialHit(x, y, atk.radius, atk.stun, true);
                        for (int i = 0; i < Mathf.Max(1, atk.count); i++) { float a = i * Mathf.PI * 2 / atk.count; Items.SpawnHumanAt("guard", x + Mathf.Cos(a) * 220, y + Mathf.Sin(a) * 160); }
                        sayCD = 0; Say(guardCall, 1.2f);
                    }
                    if (atkT > wind + atk.dur) attacking = false;
                    break;
                case "Zoomies":
                {
                    // 미친 질주: 쥐 무리 쪽(또는 아무 데나)으로 돌진, 지나간 쥐 기절
                    if (atkT < wind) { jit = 2; break; }
                    float per = atk.dur / Mathf.Max(1, atk.count);
                    int i = Mathf.FloorToInt((atkT - wind) / per);
                    if (i >= atk.count) { vx *= 0.2f; vy *= 0.2f; attacking = false; break; }
                    if (i != seqI || (phaseT == 0 && i == 0))
                    {
                        seqI = i; phaseT = 1;
                        float a = Random.Range(0, Mathf.PI * 2);
                        if (Cats && Random.value < 0.7f && Cats.FindCrowd(x, y, out var at, out _)) a = Mathf.Atan2(at.y - y, at.x - x);
                        vx = Mathf.Cos(a) * zoomSpeed; vy = Mathf.Sin(a) * zoomSpeed;
                        fx?.Popup(x, y, "우다다다!!", Data.Color, 24, 0.6f, Height * 0.7f);
                    }
                    else { float sp = Mathf.Sqrt(vx * vx + vy * vy); if (sp < zoomSpeed * 0.7f && sp > 1) { vx *= zoomSpeed / sp; vy *= zoomSpeed / sp; } }
                    foreach (var o in Rats.Rats) if (!o.UltOn && o.stun <= 0 && Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) < atk.radius) o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x), 520, 380, atk.stun);
                    if (Random.value < dt * 30) fx?.Dust(x, y, 2, 1);
                    break;
                }
                case "Box_Fit":
                    // 상자 안 (무적, 들썩들썩) → 폭발
                    if (specialProp) { specialProp.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(atkT * 20) * 6 * k); }
                    Cats?.ShowWarn(0, x, y, atk.radius, k);
                    if (atkT >= wind)
                    {
                        boxed = false; HideProp(); SetVisible(true); Cats?.HideWarns();
                        SpecialHit(x, y, atk.radius, atk.stun, true);
                        fx?.Anim("explosion", x, y, 0, 3); if (!string.IsNullOrEmpty(boxPopup)) fx?.Popup(x, y, boxPopup, Data.Color, 40, 1, Height);
                        attacking = false;
                    }
                    break;
                case "Frog_Curse":
                    jit = atkT < wind ? 2 : 0;
                    Cats?.ShowWarn(0, x, y, atk.radius, k);
                    if (atkT >= wind)
                    {
                        Cats?.HideWarns();
                        int n = 0;
                        foreach (var o in Rats.Rats)
                        {
                            if (o.UltOn || Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) > atk.radius) continue;
                            o.Stun(atk.stun); o.vz = 260;
                            if (n++ < 14) { AddMark(o, atk.stun); if (fx && n < 8) fx.Popup(o.x, o.y, frogPopup, new Color(0.55f, 0.85f, 0.45f), 18, 0.9f, 50); }
                        }
                        fx?.Ring(x, y, atk.radius, new Color(0.55f, 0.85f, 0.45f), 0.6f); fx?.Ring(x, y, atk.radius * 0.6f, Color.white, 0.45f);
                        fx?.Burst(x, y, 60, 30, new Color(0.55f, 0.85f, 0.45f), Color.white, 150, 500); fx?.Shake(0.3f);
                        attacking = false;
                    }
                    break;
                case "Black_Hole":
                {
                    var c = seqTargets.Count > 0 ? seqTargets[0] : new Vector2(x, y);
                    Cats?.ShowVortex(c.x, c.y, atk.radius, k); Cats?.ShowWarn(0, c.x, c.y, atk.radius, k);
                    float sp = holePull * k;
                    foreach (var o in Rats.Rats)
                    {
                        if (o.UltOn) continue;
                        float dx = c.x - o.x, dy = c.y - o.y, d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > atk.radius * 1.3f || d < 14) continue;
                        float m = Mathf.Min(sp * dt, d - 12) / d; o.x += dx * m; o.y += dy * m;
                    }
                    if (atkT >= wind)
                    {
                        Cats?.HideWarns();
                        int n = 0;
                        foreach (var o in Rats.Rats) if (!o.UltOn && Vector2.Distance(new Vector2(o.x, o.y), c) < atk.radius) { o.vz = Random.Range(650f, 900f); o.Stun(atk.stun); n++; }
                        Cats?.HitFx(c.x, c.y, atk.radius, SpName, n, 0.45f);
                        fx?.Stars(c.x, c.y, 100, 40, Data.Color, Color.white, 200, 600);
                        attacking = false;
                    }
                    break;
                }
                case "Tung_Sahur":
                {
                    // 진압봉으로 바닥 퉁 × 개수 (점점 커짐, 짧은 기절) → 사후르!! (큰 기절)
                    if (atkT < wind) { jit = 2; break; }
                    int n = Mathf.Max(1, atk.count);
                    float per = atk.dur / n;
                    if (atkT < wind + seqI * per) break;
                    if (seqI < n)
                    {
                        float rad = atk.radius * Mathf.Lerp(tungRadStart, 0.8f, n > 1 ? seqI / (float)(n - 1) : 1);
                        if (Cats) Cats.StunArea(x, y, rad, atk.stun * tungStunMul, 160, 220);
                        fx?.Ring(x, y, rad, Data.Color, 0.35f); fx?.Dust(x, y, 8, 1.4f); fx?.Shake(0.15f);
                        if (!string.IsNullOrEmpty(tungPopup)) fx?.Popup(x + face * 40, y, tungPopup, Color.white, 30 + seqI * 4, 0.6f, 60);
                        sq = 0.8f;
                    }
                    else
                    {
                        HideProp(); SpecialHit(x, y, atk.radius, atk.stun, true);
                        if (!string.IsNullOrEmpty(sahurPopup)) fx?.Popup(x, y, sahurPopup, Data.Color, 46, 1.1f, Height * 0.8f);
                        fx?.Shake(0.45f);
                        attacking = false;
                    }
                    seqI++;
                    break;
                }
                case "Even_Cook":
                    // 토치 들고 덜덜 + 경고 원 → 쥐 무리에 지글지글 불판 (지속 = dur)
                    jit = atkT < wind ? 1.5f : 0;
                    ShowWarns(0, k);
                    if (atkT >= wind)
                    {
                        Cats?.HideWarns(); HideProp();
                        foreach (var v in seqTargets) { AddZone(v.x, v.y, false); fx?.Burst(v.x, v.y, 20, 24, grillColor, Color.white, 120, 380); }
                        fx?.Shake(0.2f);
                        attacking = false;
                    }
                    break;
                case "Unbroken":
                    // 금빛 웅크림 → 오라 폭발 (주변 쥐 밀어냄) + dur 초 동안 받는 피해 감소
                    jit = atkT < wind ? 2.5f : 0;
                    if (atkT >= wind)
                    {
                        if (Cats) Cats.StunArea(x, y, atk.radius, atk.stun, 520, 300);
                        unbrokenT = atk.dur;
                        fx?.Ring(x, y, atk.radius, unbrokenColor, 0.6f); fx?.Ring(x, y, atk.radius * 0.6f, Color.white, 0.45f);
                        fx?.Stars(x, y, Height * 0.5f, 40, unbrokenColor, Color.white, 200, 600); fx?.Shake(0.3f);
                        if (!string.IsNullOrEmpty(unbrokenPopup)) fx?.Popup(x, y, unbrokenPopup, unbrokenColor, 40, 1.2f, Height * 0.9f);
                        if (Ults) Ults.Flash(unbrokenColor, 0.25f);
                        attacking = false;
                    }
                    break;
                case "Happy_Jump":
                {
                    // 통통 점프 × 개수 (쥐 쪽으로 조금씩), 착지마다 작은 충격파
                    if (atkT < wind) { jit = 1; break; }
                    int n = Mathf.Max(1, atk.count);
                    float per = atk.dur / n;
                    if (phaseT > 0)
                    {
                        vx = ltx; vy = lty;      // 공중에서 이동 (공격 중 감속 무시)
                        if (z <= 0 && vz <= 0)
                        {
                            phaseT = 0; vx = vy = 0; sq = 0.7f;
                            if (Cats) Cats.StunArea(x, y, atk.radius, atk.stun, 260, 300);
                            fx?.Ring(x, y, atk.radius, Data.Color, 0.35f); fx?.Dust(x, y, 8, 1.4f); fx?.Shake(0.12f);
                            if (!string.IsNullOrEmpty(happyPopup)) fx?.Popup(x, y, happyPopup, Data.Color, 26, 0.6f, Height * 0.8f);
                            if (seqI >= n) attacking = false;
                        }
                    }
                    else if (seqI < n && atkT >= wind + seqI * per)
                    {
                        seqI++; phaseT = 1;
                        vz = gravity * per * 0.45f; z = 1;
                        var r = Rats.NearestRat(x, y, 1500);
                        float a = r ? Mathf.Atan2(r.y - y, r.x - x) : Random.Range(0, Mathf.PI * 2);
                        ltx = Mathf.Cos(a) * hopMove; lty = Mathf.Sin(a) * hopMove;
                    }
                    break;
                }
                case "Buttered_Cat":
                    // 덜덜 + 경고 원 → 잼 식빵 고양이 개수 마리를 쥐 무리 쪽으로 날림 (가로축 빙글빙글, 지속 = dur)
                    jit = atkT < wind ? 2 : 0;
                    ShowWarns(0, k);
                    if (atkT >= wind)
                    {
                        Cats?.HideWarns();
                        for (int i = 0; i < seqTargets.Count; i++) AddButter(seqTargets[i], i);
                        if (!string.IsNullOrEmpty(butterPopup)) fx?.Popup(x, y, butterPopup, Data.Color, 34, 1, Height);
                        attacking = false;
                    }
                    break;
                default: attacking = false; break;
            }
        }

        // ── 두 번째 일반 공격 · 쿨타임 스킬 시작 ──
        void BeginAttack()
        {
            Say(DB.BossLine("Attack", Data.boss_id), 0.9f);
            if (atkType == "Baton") ShowProp(batonSprite, batonSize, true);
            else if (atkType == "Briefcase") ShowProp(briefcaseSprite, briefcaseSize, true);
        }

        void BeginSkill()
        {
            seqI = 0; released = 0; phaseT = 0; seqTargets.Clear();
            sayCD = 0; Say(DB.BossLine("Skill", Data.boss_id), 1.4f);
            if (!string.IsNullOrEmpty(skillPopup)) FxManager.I?.Popup(x, y, skillPopup.Replace("{skill}", SpName), Data.Color, 34, 1.2f, Height * 1.35f);
            if (Ults) Ults.Flash(Data.Color, 0.15f);
            int n = Mathf.Max(1, atk.count);
            switch (atkType)
            {
                case "Tung_Sahur": ShowProp(batonSprite, batonSize, true); break;
                case "Even_Cook": ShowProp(torchSprite, torchSize, true); PickTargets(n); break;
                case "Maxwell_Drop": case "Buttered_Cat": PickTargets(n); break;
            }
        }

        // 진압봉: 바라보는 쪽 반원 안 쥐를 날림
        void BatonHit()
        {
            float R0 = atk.radius;
            foreach (var r in Rats.Rats)
            {
                if (r.UltOn) continue;
                float dx = r.x - x, dy = r.y - y;
                if (dx * dx + dy * dy < R0 * R0 && dx * face > -40) r.Ragdoll(Mathf.Atan2(dy, dx), swingSpeed * 1.15f, swingUp, atk.stun);
            }
            var fx = FxManager.I;
            if (fx && Ults && Ults.OnScreen(x, y, 0))
            {
                fx.Slash(x + face * 70, y, 90, face > 0 ? 0 : Mathf.PI, R0, Data.Color);
                fx.Ring(x + face * R0 * 0.3f, y, R0 * 0.7f, Color.white, 0.3f);
                fx.Popup(x + face * 80, y, SpName, Color.white, 30, 0.7f, 160); fx.Shake(0.35f);
            }
        }

        // ── 바닥에 남는 구역 (독가스 구름 · 불판): 안에 expose 초 머문 쥐 기절 ──
        void AddZone(float zx, float zy, bool gas, BossAtkRow a)
        {
            var z0 = new Zone { x = zx, y = zy, rad = a.radius, life = a.dur, life0 = a.dur, stun = a.stun, expose = gas ? gasExpose : grillExpose, gas = gas };
            var sp = gas ? gasSprite : grillSprite;
            if (shotTemplate && sp) { z0.r = Instantiate(shotTemplate, shotTemplate.transform.parent); z0.r.sprite = sp; z0.r.gameObject.SetActive(true); z0.r.enabled = true; z0.r.transform.rotation = Quaternion.identity; }
            zones.Add(z0);
        }
        void AddZone(float zx, float zy, bool gas) => AddZone(zx, zy, gas, atk);

        void UpdateZones(float dt)
        {
            var fx = FxManager.I;
            int order = Cats && Cats.warnFill ? Cats.warnFill.sortingOrder : -100;
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                var z0 = zones[i]; z0.life -= dt;
                if (z0.life <= 0) { if (z0.r) Destroy(z0.r.gameObject); zones.RemoveAt(i); continue; }
                float fade = Mathf.Clamp01(z0.life / 0.5f) * Mathf.Clamp01((z0.life0 - z0.life) / 0.25f);
                if (z0.r)
                {
                    float sw = Mathf.Max(0.001f, z0.r.sprite.bounds.size.x), w = z0.rad * 2 * World.U / sw;
                    if (z0.gas)
                    {
                        // 구름: 바닥 위로 둥실, 숨 쉬듯
                        float b = 1 + 0.06f * Mathf.Sin(Time.time * 3 + i);
                        z0.r.transform.position = World.ToUnity(z0.x, z0.y, 25 + Mathf.Sin(Time.time * 2 + i) * 6);
                        z0.r.transform.localScale = new Vector3(w * b, w * b * 0.75f, 1);
                        z0.r.sortingOrder = World.SortOrder(z0.y) + 45;
                        var c = gasColor; c.a *= fade; z0.r.color = c;
                    }
                    else
                    {
                        z0.r.transform.position = World.ToUnity(z0.x, z0.y);
                        z0.r.transform.localScale = new Vector3(w, w * World.TILT, 1);
                        z0.r.sortingOrder = order + 1;
                        var c = Color.white; c.a = fade; z0.r.color = c;
                        if (fx && Random.value < dt * 10) fx.Burst(z0.x + Random.Range(-z0.rad, z0.rad) * 0.7f, z0.y + Random.Range(-z0.rad, z0.rad) * 0.5f, 10, 2, grillColor, Color.white, 40, 120);
                    }
                }
                foreach (var o in Rats.Rats)
                {
                    if (o.UltOn) continue;
                    float dx = o.x - z0.x, dy = o.y - z0.y;
                    bool inside = dx * dx + dy * dy < z0.rad * z0.rad && o.z < 60;
                    if (!inside || o.stun > 0) { z0.stay.Remove(o); continue; }
                    z0.stay.TryGetValue(o, out float st); st += dt;
                    if (st < z0.expose) { z0.stay[o] = st; continue; }
                    z0.stay.Remove(o);
                    o.Stun(z0.stun); o.vz = Mathf.Max(o.vz, z0.gas ? 120 : 280);
                    string pop = z0.gas ? gasPopup : grillPopup;
                    if (fx && !string.IsNullOrEmpty(pop) && Random.value < 0.35f) fx.Popup(o.x, o.y, pop, z0.gas ? gasColor : grillColor, 16, 0.8f, 40);
                }
            }
        }

        // ── 무한동력 버터 고양이: 가로축으로 빙글빙글 돌며 떠서 날아감 (열린 방 안에서 튕김), 닿은 쥐 기절 ──
        void AddButter(Vector2 to, int i)
        {
            if (!shotTemplate || !butterCatSprite) return;
            float a = Mathf.Atan2(to.y - y, to.x - x) + (i - (seqTargets.Count - 1) * 0.5f) * 0.12f;
            var r = Instantiate(shotTemplate, shotTemplate.transform.parent); r.sprite = butterCatSprite; r.gameObject.SetActive(true); r.enabled = true;
            r.transform.rotation = Quaternion.identity;
            butters.Add(new Butter { x = x, y = y, vx = Mathf.Cos(a) * butterSpeed, vy = Mathf.Sin(a) * butterSpeed, t = i * 0.37f, life = atk.dur, r = r });
        }

        void UpdateButters(float dt)
        {
            if (butters.Count == 0) return;
            var a = DB.BossAtk("Buttered_Cat");
            float rad = a != null ? a.radius : 70, stunT = a != null ? a.stun : 1.6f;
            var fx = FxManager.I;
            var jam = new Color(0.85f, 0.2f, 0.25f);
            for (int i = butters.Count - 1; i >= 0; i--)
            {
                var b = butters[i]; b.t += dt; b.life -= dt;
                if (b.life <= 0 || !b.r) { fx?.Anim("poof", b.x, b.y, butterHover, 1.2f); if (b.r) Destroy(b.r.gameObject); butters.RemoveAt(i); continue; }
                float nx = b.x + b.vx * dt, ny = b.y + b.vy * dt;
                if (!Stage.Open.Contains(StageManager.RoomOf(nx, b.y))) { b.vx = -b.vx; nx = b.x; }
                if (!Stage.Open.Contains(StageManager.RoomOf(b.x, ny))) { b.vy = -b.vy; ny = b.y; }
                b.x = nx; b.y = ny;
                float spin = Mathf.Cos(b.t * butterSpin * Mathf.PI * 2);         // 가로축 회전 = 세로로 뒤집힘 (위 = 고양이 등, 아래 = 잼 식빵)
                float w = butterSize * World.U / Mathf.Max(0.001f, b.r.sprite.bounds.size.x);
                b.r.transform.position = World.ToUnity(b.x, b.y, butterHover + Mathf.Sin(b.t * 5) * 10);
                b.r.transform.localScale = new Vector3(w * (b.vx < 0 ? -1 : 1), w * spin, 1);
                b.r.sortingOrder = World.SortOrder(b.y) + 40;
                foreach (var o in Rats.Rats)
                {
                    if (o.UltOn || o.stun > 0) continue;
                    float dx = o.x - b.x, dy = o.y - b.y;
                    if (dx * dx + dy * dy < rad * rad) { o.Ragdoll(Mathf.Atan2(dy, dx), 300, 420, stunT); if (fx && Random.value < 0.3f) fx.Burst(o.x, o.y, 20, 6, jam, Color.white, 80, 220); }
                }
                if (fx && Random.value < dt * 8) fx.Burst(b.x, b.y, butterHover, 1, jam, new Color(0.95f, 0.8f, 0.5f), 30, 90);
            }
        }

        void ClearSkillFx()
        {
            foreach (var z0 in zones) if (z0.r) Destroy(z0.r.gameObject);
            zones.Clear();
            foreach (var b in butters) if (b.r) Destroy(b.r.gameObject);
            butters.Clear();
        }

        // 범위 기절 + 물건 날림 + 연출
        void SpecialHit(float hx, float hy, float rad, float stun, bool items)
        {
            int n = Cats ? Cats.StunArea(hx, hy, rad, stun, ragdollSpeed, ragdollUp) : 0;
            if (items && Cats) Cats.LaunchItems(hx, hy, rad, itemLaunch);
            if (Cats) Cats.HitFx(hx, hy, rad, SpName, n, 0.45f);
            var fx = FxManager.I; if (fx) { fx.Ring(hx, hy, rad, Data.Color, 0.5f); fx.Spill(hx, hy, rad * 0.4f, new Color(0.24f, 0.2f, 0.18f, 0.3f)); }
            if (Ults) Ults.Flash(Color.white, 0.15f);
            sq = 0.7f;
        }

        // 거대화 진행 (공격 중이 아니어도 계속)
        void UpdateGrow(float dt)
        {
            float target = growT > 0 ? growScale : 1;
            growK += (target - growK) * Mathf.Min(1, dt * 4);
            if (growT <= 0) return;
            growT -= dt;
            if ((pulseT -= dt) <= 0) { pulseT = pulseEvery; var a = DB.BossAtk("Self_Experiment"); if (a != null) { var keep = atk; atk = a; SpecialHit(x, y, a.radius * growK, a.stun, true); atk = keep; } }
        }

        void ShowProp(Sprite sp, float size, bool aboveHead)
        {
            if (!specialProp || !sp) return;
            specialProp.sprite = sp; specialProp.gameObject.SetActive(true);
            specialProp.transform.localRotation = Quaternion.identity;
            float w = size * World.U / Mathf.Max(0.001f, sp.bounds.size.x);
            specialProp.transform.localScale = Vector3.one * w;
            propAbove = aboveHead;
        }
        bool propAbove;
        void HideProp() { if (specialProp) specialProp.gameObject.SetActive(false); }

        void AddMark(Rat r, float life)
        {
            if (!markTemplate || !frogSprite) return;
            var m = Instantiate(markTemplate, markTemplate.transform.parent); m.sprite = frogSprite; m.gameObject.SetActive(true);
            marks.Add(new Mark { r = m, rat = r, life = life });
        }
        void UpdateMarks(float dt)
        {
            for (int i = marks.Count - 1; i >= 0; i--)
            {
                var m = marks[i]; m.life -= dt;
                if (m.life <= 0 || !m.rat) { if (m.r) Destroy(m.r.gameObject); marks.RemoveAt(i); continue; }
                float w = frogSize * World.U / Mathf.Max(0.001f, m.r.sprite.bounds.size.x) * (1 + 0.1f * Mathf.Sin(Time.time * 12 + i));
                m.r.transform.position = World.ToUnity(m.rat.x, m.rat.y, m.rat.z + 34);
                m.r.transform.localScale = Vector3.one * w;
                m.r.sortingOrder = World.SortOrder(m.rat.y) + 50;
                var c = m.r.color; c.a = Mathf.Clamp01(m.life * 3); m.r.color = c;
            }
        }
        void ClearMarks() { foreach (var m in marks) if (m.r) Destroy(m.r.gameObject); marks.Clear(); }
        void EndSpecialFx() { boxed = false; growT = 0; HideProp(); ClearMarks(); ClearSkillFx(); if (Cats) Cats.HideWarns(); }

        // 내려찍기 착지 (웹 bossStompLand)
        void Land()
        {
            air = false; sq = 0.7f;
            bool pounce = atkType == "Pounce";
            float R0 = atk != null ? atk.radius : 300, stunT = atk != null ? atk.stun : 2;
            foreach (var r in Rats.Rats)
            {
                if (r.UltOn) continue;
                float dx = r.x - x, dy = r.y - y;
                if (dx * dx + dy * dy < R0 * R0) r.Ragdoll(Mathf.Atan2(dy, dx), ragdollSpeed, ragdollUp, stunT);
            }
            foreach (var it in Items.InRange(x, y, R0))
                if (it.State == Item.ItemState.Rest) it.Launch(Mathf.Atan2(it.y - y, it.x - x), itemLaunch, false);
            var fx = FxManager.I; if (!fx) return;
            fx.Ring(x, y, R0, Data.Color, 0.45f); fx.Ring(x, y, R0 * 0.6f, Color.white, 0.35f);
            fx.Dust(x, y, 20, 2.4f); fx.Anim("poof", x, y, 0, 2.2f); fx.Shake(0.5f);
            fx.Spill(x, y, 110, new Color(0.24f, 0.2f, 0.18f, 0.3f));
            string pop = pounce ? pouncePopup : stompPopup;
            if (!string.IsNullOrEmpty(pop)) fx.Popup(x, y, pop, pounce ? Data.Color : Color.white, pounce ? 34 : 40, 0.8f, pounce ? 120 : 60);
            if (Ults) Ults.Flash(Color.white, 0.2f);
        }

        // ── 휘두르기 (웹 swing): 반경 안 쥐 기절·날림, 몇 번에 한 번 경비원 호출 ──
        void Swing()
        {
            float R0 = atk.radius;
            foreach (var r in Rats.Rats)
            {
                if (r.UltOn) continue;
                float dx = r.x - x, dy = r.y - y;
                if (dx * dx + dy * dy < R0 * R0) r.Ragdoll(Mathf.Atan2(dy, dx), swingSpeed, swingUp, atk.stun);
            }
            var fx = FxManager.I;
            if (fx && Ults && Ults.OnScreen(x, y, 0))
            {
                fx.Ring(x, y, R0, Data.Color, 0.4f);
                fx.Slash(x + face * 60, y, 80, face > 0 ? 0 : Mathf.PI, R0 * 0.9f, Data.Color);
                if (!string.IsNullOrEmpty(swingPopup)) fx.Popup(x, y, swingPopup, Color.white, 34, 0.7f, 160);
                fx.Shake(0.35f);
            }
            if (guardEvery > 0 && ++swings % guardEvery == 0)
            {
                for (int i = 0; i < Mathf.Max(1, atk.count); i++) Items.SpawnHumanAt("guard", x + Random.Range(-120f, 120f), y + Random.Range(-60f, 80f));
                fx?.Dust(x, y, 10, 1.6f);
                sayCD = 0; Say(guardCall, 1.2f);
            }
        }

        // ── 무중력 파동 (웹 gravity): 반경 안 쥐가 둥실 떠올랐다 떨어지며 기절, 물건도 떠오름 ──
        void GravityWave()
        {
            float R0 = atk.radius;
            foreach (var r in Rats.Rats)
            {
                if (r.UltOn) continue;
                float dx = r.x - x, dy = r.y - y;
                if (dx * dx + dy * dy < R0 * R0) r.Ragdoll(Random.Range(0, Mathf.PI * 2), 40, Random.Range(gravUpMin, gravUpMax), atk.stun);
            }
            foreach (var it in Items.InRange(x, y, R0))
                if (it.State == Item.ItemState.Rest) it.Launch(Random.Range(0, Mathf.PI * 2), gravItem, false);
            var fx = FxManager.I;
            if (fx && Ults && Ults.OnScreen(x, y, 0))
            {
                fx.Ring(x, y, R0, Data.Color, 0.6f); fx.Ring(x, y, R0 * 0.6f, Color.white, 0.45f);
                fx.Stars(x, y, 80, 24, Data.Color, Color.white, 150, 400); fx.Shake(0.3f);
                if (!string.IsNullOrEmpty(gravityPopup)) fx.Popup(x, y, gravityPopup, Data.Color, 34, 0.8f, 200);
            }
        }

        // ── 투사체 (웹 bossThrow · updateBossShots) ──
        void Throw(float tx, float ty, string type)
        {
            if (!shotTemplate) return;
            var sp = type == "Hairball" ? hairballSprite : type == "Fireball" ? fireballSprite : type == "Briefcase" ? briefcaseSprite : flaskSprite;
            var r = Instantiate(shotTemplate, shotTemplate.transform.parent);
            r.sprite = sp; r.gameObject.SetActive(true); r.enabled = true;
            float k = sp ? (type == "Briefcase" ? briefcaseSize : shotSize) * World.U / Mathf.Max(0.001f, sp.bounds.size.x) : 1;
            r.transform.localScale = Vector3.one * k;
            flying.Add(new Shot { x = x, y = y, z = type == "Hairball" ? 120 : 200, vx = (tx - x) / shotTime, vy = (ty - y) / shotTime, vz = shotUp, type = type, r = r });
        }

        void UpdateShots(float dt)
        {
            for (int i = flying.Count - 1; i >= 0; i--)
            {
                var s = flying[i];
                s.x += s.vx * dt; s.y += s.vy * dt; s.vz -= shotGravity * dt; s.z += s.vz * dt; s.rot += dt * 14;
                if (s.r)
                {
                    s.r.transform.position = World.ToUnity(s.x, s.y, Mathf.Max(0, s.z));
                    s.r.transform.rotation = Quaternion.Euler(0, 0, -s.rot * Mathf.Rad2Deg);
                    s.r.sortingOrder = World.SortOrder(s.y) + 40;
                }
                if (s.z > 0) continue;
                ShotLand(s);
                if (s.r) Destroy(s.r.gameObject);
                flying.RemoveAt(i);
            }
        }

        void ShotLand(Shot s)
        {
            var a = DB.BossAtk(s.type);
            float R0 = a != null ? a.radius : 110, stunT = a != null ? a.stun : 2.5f;
            if (s.type == "Gas_Cloud")
            {
                // 독가스: 맞은 자리엔 기절 없이 구름만 (구름 안에 머물면 기절)
                if (a != null) AddZone(s.x, s.y, true, a);
                FxManager.I?.Anim("poof", s.x, s.y, 0, 1); FxManager.I?.Spill(s.x, s.y, R0 * 0.6f, flaskSpill);
                return;
            }
            if (s.type == "Briefcase")
            {
                int n = Cats ? Cats.StunArea(s.x, s.y, R0, stunT, ragdollSpeed, ragdollUp) : 0;
                if (Cats) { Cats.LaunchItems(s.x, s.y, R0, itemLaunch); Cats.HitFx(s.x, s.y, R0, a != null ? a.atk_name : s.type, n, 0.3f); }
                FxManager.I?.Burst(s.x, s.y, 30, 24, Color.white, new Color(0.95f, 0.92f, 0.82f), 200, 520);     // 서류 흩날림
                return;
            }
            foreach (var r in Rats.Rats)
            {
                if (r.UltOn) continue;
                float dx = r.x - s.x, dy = r.y - s.y;
                if (dx * dx + dy * dy < R0 * R0) r.Ragdoll(Mathf.Atan2(dy, dx), 60, 260, stunT);
            }
            var fx = FxManager.I;
            if (!fx || !(Ults && Ults.OnScreen(s.x, s.y, 0))) return;
            Color c0 = s.type == "Fireball" ? new Color(0.94f, 0.78f, 0.47f) : s.type == "Hairball" ? new Color(0.78f, 0.64f, 0.48f) : new Color(0.62f, 0.84f, 0.66f);
            fx.Ring(s.x, s.y, R0, c0, 0.5f);
            fx.Burst(s.x, s.y, 10, 18, c0, Color.white, 150, 420);
            fx.Spill(s.x, s.y, R0 * 0.8f, s.type == "Fireball" ? fireSpill : s.type == "Hairball" ? hairSpill : flaskSpill);
            fx.Anim(s.type == "Fireball" ? "explosion" : "poof", s.x, s.y, 0, s.type == "Fireball" ? 1.2f : 1);
            fx.Shake(0.12f);
            if (s.type == "Hairball" && !string.IsNullOrEmpty(hairPopup)) fx.Popup(s.x, s.y, hairPopup, Color.white, 18, 0.7f, 30);
        }

        void ClearShots() { foreach (var s in flying) if (s.r) Destroy(s.r.gameObject); flying.Clear(); }

        // 하늘로 사라짐 → 계단 열림 (테스트면 이 층 진짜 보스를 되돌림)
        void Finish()
        {
            var keep = stash; bool test = Test;
            State = BState.Dead; SetVisible(false);
            if (bar) bar.SetActive(false);
            if (test)
            {
                Clear();
                if (keep != null) { var sp = Stage.StairsPos; Spawn(keep, sp.x, sp.y + waitOffsetY, false); }
            }
        }

        void UpdateBar(float udt)
        {
            if (!bar || !bar.activeSelf) return;
            if (State == BState.Dying && t > 1) { bar.SetActive(false); return; }
            float k = Mathf.Clamp01(hp / Mathf.Max(1, hpMax));
            if (hitT > 0.2f) trailWait = trailDelay;
            if ((trailWait -= udt) <= 0) trailK = Mathf.MoveTowards(trailK, k, udt * trailSpeed * Mathf.Max(0.05f, trailK - k));
            if (trailK < k || State == BState.Wait) trailK = k;
            if (barTrail) barTrail.anchorMax = new Vector2(trailK, barTrail.anchorMax.y);
            if (barEmblem) barEmblem.localRotation = Quaternion.Euler(0, 0, hitT > 0 ? Mathf.Sin(Time.unscaledTime * 60) * 8 * hitT * 4 : 0);
            if (barFill)
            {
                barFill.anchorMax = new Vector2(k, barFill.anchorMax.y);
                var img = barFill.GetComponent<Image>(); if (img && Data != null) img.color = Data.Color;
            }
            if (barHit)
            {
                barHit.rectTransform.anchorMax = new Vector2(k, barHit.rectTransform.anchorMax.y);
                var c = barHit.color; c.a = Mathf.Clamp01(hitT * 3); barHit.color = c;
            }
        }

        // ── 그림 (웹 humanPose 보스) ──
        HumanRig.Pose MakePose()
        {
            var p = new HumanRig.Pose { sx = 1, sy = 1 };
            float tt = Time.time, sp = Mathf.Sqrt(vx * vx + vy * vy);
            if (State == BState.Wait) { p.armN = 0.25f; p.armF = -0.2f; p.bob = Mathf.Sin(tt * 2) * 2; return p; }
            if (State == BState.Dying || State == BState.Dead) { p.scared = true; p.armN = 2.3f; p.armF = -2.3f; p.legN = 0.55f; p.legF = -0.55f; return p; }
            if (sp > 12) { p.legN = Mathf.Sin(walk) * 0.45f; p.legF = -p.legN; p.armN = -Mathf.Sin(walk) * 0.35f; p.armF = -p.armN; p.bob = Mathf.Abs(Mathf.Cos(walk)) * 3; }
            if (attacking)
            {
                p.angry = true;
                bool up = air || z > 0;
                if ((atkType == "Swing" || atkType == "Baton") && atk != null)
                {
                    // 주먹 휘두르기: 웅크림 동안 뒤로 젖혔다가 앞으로 크게
                    float k = Mathf.Clamp01((atkT - atk.windup) / 0.18f);
                    p.armN = atkT < atk.windup ? -2.2f : Mathf.Lerp(-2.2f, 1.9f, k); p.armF = -0.4f;
                }
                else if (atkType == "Tung_Sahur" && atk != null)
                {
                    // 퉁퉁퉁: 진압봉을 들었다 내려찍기 반복
                    float per = atk.dur / Mathf.Max(1, atk.count), ph = (atkT - atk.windup) / per, f = ph - Mathf.Floor(ph);
                    p.armN = atkT < atk.windup ? 2.6f : Mathf.Lerp(0.9f, 2.6f, f); p.armF = -p.armN * 0.8f;      // 퉁 순간 아래로 → 천천히 다시 듦
                }
                else if ((atkType == "Flask" || atkType == "Gas_Cloud" || atkType == "Briefcase" || atkType == "Even_Cook") && atk != null)
                {
                    // 던지기: 팔을 뒤로 → 던질 때마다 앞으로
                    float ph = (atkT - atk.windup) / 0.22f, f = ph - Mathf.Floor(ph);
                    p.armN = atkT < atk.windup ? -2.4f : Mathf.Lerp(1.6f, -2f, f); p.armF = 0.4f;
                }
                else { p.armN = up ? 2.6f : 1.2f; p.armF = up ? -2.6f : -1.2f; p.legN = up ? 0.8f : 0; p.legF = up ? -0.6f : 0; }
            }
            else if (hitT > 0) p.angry = true;
            return p;
        }

        // 고양이 보스 (웹 drawBossCat · catPose): 걷기 · 웅크림 · 덮치기 · 던지기 · 날아감
        RatRig.Pose MakeCatPose()
        {
            float tt = Time.time;
            var p = new RatRig.Pose { head = Mathf.Sin(tt * 1.6f) * 0.06f, tail = 0.1f + Mathf.Sin(tt * 3) * 0.25f, sx = 1, sy = 1 };
            if (State == BState.Wait) { p.tail = 0.3f + Mathf.Sin(tt * 2) * 0.3f; return p; }
            if (State == BState.Dying) { p.front = Mathf.Sin(tt * 30) * 1.4f; p.farFront = Mathf.Cos(tt * 27) * 1.4f; p.back = Mathf.Sin(tt * 28) * 1.2f; p.farBack = Mathf.Cos(tt * 25) * 1.2f; p.tail = Mathf.Sin(tt * 20); p.head = Mathf.Sin(tt * 15) * 0.4f; return p; }
            if (attacking && atk != null)
            {
                bool jump = atkType == "Pounce" || atkType == "Stomp" || atkType == "Happy_Jump";
                if (jump && (air || z > 0)) { p.front = 1.3f; p.farFront = 1.1f; p.back = -1.1f; p.farBack = -0.9f; p.tilt = -0.25f; p.tail = 1; p.head = -0.15f; return p; }
                if (atkT < atk.windup) { p.front = 0.3f; p.farFront = 0.3f; p.back = 0.4f; p.farBack = 0.4f; p.tilt = 0.12f; p.bob = 6; p.tail = 0.9f + Mathf.Sin(tt * 20) * 0.2f; return p; }
                if (!jump) { p.front = 2.1f; p.farFront = 0.5f; p.tilt = -0.3f; p.head = -0.2f; p.tail = 1.1f; return p; }
            }
            if (Mathf.Sqrt(vx * vx + vy * vy) > 12) { float sn = Mathf.Sin(walk); p.front = sn * 0.5f; p.farBack = sn * 0.45f; p.farFront = -sn * 0.5f; p.back = -sn * 0.45f; p.bob = -Mathf.Abs(Mathf.Cos(walk)) * 2.5f; p.tail = Mathf.Sin(walk * 0.5f) * 0.3f + 0.1f; }
            return p;
        }

        void LateUpdate()
        {
            if (State == BState.Off || State == BState.Dead || (!rig && !catRig)) return;
            // 층 보스 대기: 계단 방은 처음부터 어둡게 그려짐 (StageManager.BossWaitRoom) → 보스는 그 안에 실루엣, 계단 방이 열리면 원래 색 + 전투
            int look = 2;
            if (State == BState.Wait && !Test) look = Stage.IsOpen(Stage.StairsRoom.x, Stage.StairsRoom.y) ? 2 : 1;
            SetVisible(look > 0 && !boxed);
            if (look == 0) return;
            float jx = jit > 0 ? Random.Range(-jit, jit) : 0;
            transform.position = World.ToUnity(x + jx, y, z);
            if (Cat) catRig.Apply(MakeCatPose(), growK, face, sq, World.SortOrder(y), 0, State == BState.Dying ? rot : 0);
            else rig.Apply(MakePose(), growK, face, State == BState.Dying ? rot : 0, sq, World.SortOrder(y), 1);
            UpdateMarks(Time.deltaTime);
            if (specialProp && specialProp.gameObject.activeSelf)
            {
                specialProp.transform.position = World.ToUnity(x + (propAbove ? face * 30 : 0), y, propAbove ? Height * 0.75f + Mathf.Sin(Time.time * 10) * 6 : boxSize * 0.38f);
                specialProp.sortingOrder = World.SortOrder(y) + 30;
            }
            // 실루엣 색 (사람 리그는 Apply 가 매번 색을 되돌림, 고양이 리그는 원래 색으로 되돌림)
            var k = look == 1 ? waitDarkTint : unbrokenT > 0 ? Color.Lerp(Color.white, unbrokenColor, 0.35f + 0.2f * Mathf.Sin(Time.time * 8)) : Color.white;     // 중꺾마 = 금빛
            if (Cat || look == 1 || unbrokenT > 0) foreach (var (r, c) in baseColors) if (r) r.color = new Color(c.r * k.r, c.g * k.g, c.b * k.b, c.a);
            if (shadow)
            {
                float w = R * 1.4f * (1 - Mathf.Min(0.7f, z / 900)), sw = shadow.sprite ? shadow.sprite.bounds.size.x : 1;
                shadow.enabled = State != BState.Dying;
                shadow.transform.position = World.ToUnity(x, y);
                shadow.transform.localScale = new Vector3(w * 2 * World.U / sw, w * World.U * World.TILT / sw, 1);
            }
        }
    }
}
