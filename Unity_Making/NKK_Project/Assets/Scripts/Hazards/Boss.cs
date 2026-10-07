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
    // · 전투: 가장 가까운 쥐 쪽으로 쿵쿵 걸어옴, 공격 간격마다 공격 (Stomp = 웅크렸다 점프 내려찍기 → 반경 안 쥐 기절·데굴, 물건 날아감)
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
        [Tooltip("접지 그림자")] public SpriteRenderer shadow;

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
        [Tooltip("치즈 팝업 (자리표시 {n})")] public string cheesePopup;

        [Header("수치")]
        [Tooltip("계단 기준 대기 위치 (게임 단위, 아래로 +)")] public float waitOffsetY = 220;
        [Tooltip("웅크림 시간 · 점프 세기 · 중력")] public float windup = 0.5f, jumpV = 900, gravity = 1800;
        [Tooltip("공격 하나 길이 (초)")] public float atkTime = 2;
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

        int face = 1;
        float t, walk, atkCd, atkT, rot, vr, jit, sq = 1, hitT, sayCD, value;
        bool attacking, air;
        string nameFormat;
        BossRow stash;          // 테스트 보스를 부를 때 이 층 진짜 보스 (끝나면 되돌림)
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
            if (rig) rig.gameObject.SetActive(on);
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
            hpMax = hp = (test ? Mathf.Max(1, Stage.PowNeed(Game.Floor) * row.hp_pow_sec) : HpFor(row, Game.Floor)) * CommonSkill.BossHpMul(row.floor);   // 테스트 = 지금 층 기준 (잡을 수 있게) · 보스 체력 감소 노드
            value = 3 * Mathf.Pow(Items.valueGrow, (test ? Game.Floor : Mathf.Max(Game.Floor, row.floor)) - 1) * row.cheese_mul * CommonSkill.CreatureCheeseMul;
            var art = Items.humanArt ? Items.humanArt.Get(row.code_id) : null;
            if (art == null) { Debug.LogWarning("[Boss] 그림 없음: " + row.code_id); State = BState.Off; return; }
            rig.height = Items.humanHeight * row.scale;
            rig.Build(art);
            SetVisible(true);
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
            SetVisible(false);
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
            var row = DB.BossOf(5) ?? DB.Bosses[0];
            var c = Ults ? Ults.ViewRect(0).center : new Vector2(World.RW / 2, World.RH / 2);
            Spawn(row, c.x, c.y + 60, true);
            stash = keep;
            FxManager.I?.Dust(x, y, 16, 2.4f);
            StartFight();
        }

        void Say(string line, float life = 1.2f)
        {
            if (string.IsNullOrEmpty(line) || sayCD > 0) return;
            FxManager.I?.Popup(x, y, line, Color.white, 22, life, rig.height * 1.05f);
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
            vx = Random.Range(-120f, 120f); vy = -40; vz = downV; vr = downSpin;
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
            if (z > 0 || vz > 0) { vz -= gravity * dt; z = Mathf.Max(0, z + vz * dt); if (z <= 0) { vz = 0; if (air) Land(); } }
            if (!attacking)
            {
                var r = Rats.NearestRat(x, y, 2000);
                if (r) { float a = Mathf.Atan2(r.y - y, r.x - x), s = Data.move_speed; vx += (Mathf.Cos(a) * s - vx) * Mathf.Min(1, dt * 3); vy += (Mathf.Sin(a) * s - vy) * Mathf.Min(1, dt * 3); }
                atkCd -= dt;
                if (atkCd <= 0 && !(Ults && Ults.Busy))
                {
                    attacking = true; air = false; atkT = 0; atkCd = Random.Range(Data.atk_cd_min, Data.atk_cd_max); vx = vy = 0;
                    Say(DB.BossLine("Attack", Data.boss_id), 0.9f);
                }
            }
            else
            {
                atkT += dt; vx *= 0.85f; vy *= 0.85f;
                // Stomp: 웅크림 (덜덜) → 가장 가까운 쥐 쪽으로 점프
                if (atkT < windup) jit = 3;
                else if (!air && z <= 0 && atkT < windup + 0.1f)
                {
                    air = true; vz = jumpV;
                    var r = Rats.NearestRat(x, y, 900);
                    if (r) { vx = (r.x - x) * 1.2f; vy = (r.y - y) * 1.2f; }
                }
                if (atkT > atkTime && z <= 0) attacking = false;
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
            float R0 = Data.atk_radius;
            foreach (var r in Rats.Rats)
            {
                if (r.UltOn) continue;
                float dx = r.x - x, dy = r.y - y;
                if (dx * dx + dy * dy < R0 * R0) r.Ragdoll(Mathf.Atan2(dy, dx), ragdollSpeed, ragdollUp, Data.atk_stun);
            }
            foreach (var it in Items.InRange(x, y, R0))
                if (it.State == Item.ItemState.Rest) it.Launch(Mathf.Atan2(it.y - y, it.x - x), itemLaunch, false);
            var fx = FxManager.I; if (!fx) return;
            fx.Ring(x, y, R0, Data.Color, 0.45f); fx.Ring(x, y, R0 * 0.6f, Color.white, 0.35f);
            fx.Dust(x, y, 20, 2.4f); fx.Anim("poof", x, y, 0, 2.2f); fx.Shake(0.5f);
            fx.Spill(x, y, 110, new Color(0.24f, 0.2f, 0.18f, 0.3f));
            if (!string.IsNullOrEmpty(stompPopup)) fx.Popup(x, y, stompPopup, Color.white, 40, 0.8f, 60);
            if (Ults) Ults.Flash(Color.white, 0.2f);
        }

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
                p.armN = up ? 2.6f : 1.2f; p.armF = up ? -2.6f : -1.2f; p.legN = up ? 0.8f : 0; p.legF = up ? -0.6f : 0;
            }
            else if (hitT > 0) p.angry = true;
            return p;
        }

        void LateUpdate()
        {
            if (State == BState.Off || State == BState.Dead || !rig) return;
            float jx = jit > 0 ? Random.Range(-jit, jit) : 0;
            transform.position = World.ToUnity(x + jx, y, z);
            rig.Apply(MakePose(), 1, face, State == BState.Dying ? rot : 0, sq, World.SortOrder(y), 1);
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
