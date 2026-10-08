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
    // 층 보스 (웹게임 makeBoss · startBossFight · updateBoss · bossStompLand · bossDown 이식). 스테이지 테이블 Boss · Boss_Line.
    // · 보스 층에 들어가면 계단 방에서 대기 (Wait) → 계단 방 벽이 무너지는 순간 전투 (Fight): 배너 + 대사 + 번쩍
    // · 전투: 가장 가까운 쥐 쪽으로 쿵쿵 걸어옴, 공격 간격마다 공격 (atk_type, atk2_chance 확률로 atk2_type). 공격 수치 = Atk_Type 시트
    //   Stomp/Pounce 점프 내려찍기 · Flask/Hairball/Fireball 투척 · Swing 휘두르기(몇 번에 한 번 경비원 호출) · Gravity 무중력 파동
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

        [Header("상태 (보기용)")]
        public BState State = BState.Off;
        public float x, y, z, vx, vy, vz, hp, hpMax;
        public BossRow Data { get; private set; }
        public bool Test { get; private set; }

        public bool Blocking => State == BState.Wait || State == BState.Fight || State == BState.Dying;   // 계단 막음
        public bool CanHit => State == BState.Fight;
        public float R => Items.humanRadius * (Data != null ? Data.radius_mul : 1);

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
            var row = DB.BossOf(Game.Floor);
            if (row == null) return;
            var sp = Stage.StairsPos;
            Spawn(row, sp.x, sp.y + waitOffsetY, false);
            if (State == BState.Wait) Game.ShowBanner(Game.BannerTitle, Fill(floorSub));
        }

        public void Clear()
        {
            State = BState.Off; Data = null; stash = null; Test = false;
            SetVisible(false); ClearShots();
            if (bar) bar.SetActive(false);
            if (Current == this) Current = null;
        }

        public void StartFight()
        {
            if (State != BState.Wait) return;
            State = BState.Fight; t = 0;
            string intro = DB.BossLine("Intro", Data.boss_id);
            Game.ShowBanner(Data.boss_name, Fill(fightSub));
            Say(intro, 2.4f);
            if (Ults) Ults.Flash(new Color(0.91f, 0.47f, 0.42f), 0.3f);
            FxManager.I?.Shake(0.4f);
            if (Game.cam) Game.cam.CenterOn(x, y);
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
            if (!CanHit) return false;
            dmg *= CommonSkill.BossDmgMul;
            hp -= dmg; hitT = 0.25f; jit = 3;
            vx += Mathf.Cos(ang) * 60; vy += Mathf.Sin(ang) * 60;
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
            State = BState.Dying; t = 0; hp = 0; attacking = false;
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
            UpdateShots(dt);
            if (z > 0 || vz > 0) { vz -= gravity * dt; z = Mathf.Max(0, z + vz * dt); if (z <= 0) { vz = 0; if (air) Land(); } }
            if (!attacking)
            {
                var r = Rats.NearestRat(x, y, 2000);
                if (r) { float a = Mathf.Atan2(r.y - y, r.x - x), s = Data.move_speed; vx += (Mathf.Cos(a) * s - vx) * Mathf.Min(1, dt * 3); vy += (Mathf.Sin(a) * s - vy) * Mathf.Min(1, dt * 3); }
                atkCd -= dt;
                if (atkCd <= 0 && !(Ults && Ults.Busy))
                {
                    bool two = !string.IsNullOrEmpty(Data.atk2_type) && Data.atk2_type != "None" && Random.value < Data.atk2_chance;
                    atkType = two ? Data.atk2_type : Data.atk_type;
                    atk = DB.BossAtk(atkType);
                    attacking = true; air = false; atkHit = false; shots = 0; atkT = 0; atkCd = Random.Range(Data.atk_cd_min, Data.atk_cd_max); vx = vy = 0;
                    Say(DB.BossLine("Attack", Data.boss_id), 0.9f);
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
                    default: attacking = false; break;
                }
            }
            x += vx * dt; y += vy * dt;
            Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 0.3f);
            if (Mathf.Abs(vx) > 8) face = vx > 0 ? 1 : -1;
            walk += dt * Mathf.Sqrt(vx * vx + vy * vy) / 16;
        }

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
            var sp = type == "Hairball" ? hairballSprite : type == "Fireball" ? fireballSprite : flaskSprite;
            var r = Instantiate(shotTemplate, shotTemplate.transform.parent);
            r.sprite = sp; r.gameObject.SetActive(true); r.enabled = true;
            float k = sp ? shotSize * World.U / Mathf.Max(0.001f, sp.bounds.size.x) : 1;
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
                if (atkType == "Swing" && atk != null)
                {
                    // 주먹 휘두르기: 웅크림 동안 뒤로 젖혔다가 앞으로 크게
                    float k = Mathf.Clamp01((atkT - atk.windup) / 0.18f);
                    p.armN = atkT < atk.windup ? -2.2f : Mathf.Lerp(-2.2f, 1.9f, k); p.armF = -0.4f;
                }
                else if (atkType == "Flask" && atk != null)
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
                bool jump = atkType == "Pounce" || atkType == "Stomp";
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
            // 층 보스 대기: 계단 방이 화면에 그려질 때만 보임 (안 열린 방은 바닥이 안 그려져 보스만 허공에 떠 보였음)
            //   계단 방 열림 = 원래 색 · 옆 방이 열려 어둡게 보임 = 실루엣 · 그 외 = 숨김
            int look = 2;
            if (State == BState.Wait && !Test)
            {
                var sr = Stage.StairsRoom; bool peek = false;
                foreach (var d in StageManager.Dirs) if (Stage.IsOpen(sr.x + d.x, sr.y + d.y)) peek = true;
                look = Stage.IsOpen(sr.x, sr.y) ? 2 : peek ? 1 : 0;
            }
            SetVisible(look > 0);
            if (look == 0) return;
            float jx = jit > 0 ? Random.Range(-jit, jit) : 0;
            transform.position = World.ToUnity(x + jx, y, z);
            if (Cat) catRig.Apply(MakeCatPose(), 1, face, sq, World.SortOrder(y), 0, State == BState.Dying ? rot : 0);
            else rig.Apply(MakePose(), 1, face, State == BState.Dying ? rot : 0, sq, World.SortOrder(y), 1);
            // 실루엣 색 (사람 리그는 Apply 가 매번 색을 되돌림, 고양이 리그는 원래 색으로 되돌림)
            var k = look == 1 ? waitDarkTint : Color.white;
            if (Cat || look == 1) foreach (var (r, c) in baseColors) if (r) r.color = new Color(c.r * k.r, c.g * k.g, c.b * k.b, c.a);
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
