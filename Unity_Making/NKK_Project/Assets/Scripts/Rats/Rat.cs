using System.Collections.Generic;
using NKK.Data;
using NKK.Hazards;
using NKK.Humans;
using NKK.Items;
using NKK.Stage;
using UnityEngine;

namespace NKK.Rats
{
    // 쥐 한 마리 (웹게임 makeRat / newDash / moveRat / stomp / hitWall 이식). 위치·속도는 게임 단위.
    public partial class Rat : MonoBehaviour
    {
        public RatRig rig;
        [Tooltip("바닥 그림자 (자식)")] public SpriteRenderer shadow;

        [Header("상태 (실행 중 확인용)")]
        public string codeId;
        public Grade grade;
        public float x, y, z, vx, vy, vz;
        public int face = 1;
        [Tooltip("총공격(클릭 돌진) 남은 시간")] public float rushT;

        public RatCharacterRow Data { get; private set; }
        public RatGradeRow GradeData { get; private set; }
        public RatManager Manager { get; private set; }
        public float Speed { get; private set; }
        public float Radius => Manager.ratRadius * GradeData.size;
        public bool Rushing => Manager.RushActive && rushT > 0;
        [Tooltip("물건 날리는 힘 배율 (박치기 특수 능력 자리)")] public float KnockPower = 1;

        [Tooltip("기절 남은 시간 (고양이·쥐덫)")] public float stun;
        [HideInInspector] public float flee; float fleeX, fleeY, tumbleT;

        // 번식
        [HideInInspector] public float breedCD, noBreed;
        [HideInInspector] public int rushLock;

        enum Mode { Pause, Run }
        Mode mode = Mode.Pause;
        float t, walk, biteCD, wallCD, bite, sq = 1, born = 1, stopT = -9, seed;

        public void Init(RatManager m, RatCharacterRow data, RatGradeRow grade, RatArtLibrary.Entry art, float px, float py)
        {
            Manager = m; Data = data; GradeData = grade; codeId = data.code_id; this.grade = data.Grade;
            name = $"Rat_{data.code_id}";
            x = px; y = py; t = Random.Range(0f, 0.5f); walk = Random.Range(0f, 6f); seed = Random.Range(0f, 10f);
            breedCD = Random.Range(1f, 4f);
            rig.Build(art, m.RigLength(data));
            born = 0;
            InitPassive();
            InitAction();
            // 그림자는 쥐 정렬 그룹 밖(바닥 바로 위)에 그려야 다른 쥐 몸 위에 겹치지 않음 → 실행 중엔 그림자 묶음으로 옮김
            if (shadow && m.shadowRoot) { shadow.transform.SetParent(m.shadowRoot, true); shadow.sortingOrder = m.shadowSortOrder; }
        }

        void OnDestroy() { if (shadow && shadow.transform.parent != transform) Destroy(shadow.gameObject); }

        public void Place(float px, float py) { x = px; y = py; vx = vy = z = vz = 0; rushT = 0; mode = Mode.Pause; t = Random.Range(0f, 0.5f); }

        // 공격력 = 테이블 × 특수 능력 × 종별 성장 × 공용 스킬 (반란의 시작·이빨 강화·쥐 헬스장) × 광란
        public float Damage => Data.atk * PassiveDamageMult * GrowthAtkMult * CommonSkill.AtkMul((int)Data.Grade) * (frenzy > 0 ? 1.5f : 1) * (zombie > 0 ? 2 : 1);
        float RunSpeed => Manager.baseSpeed * GradeData.move_speed * PassiveSpeedMult * CommonSkill.MoveSpeedMul * (frenzy > 0 ? 1.5f : 1);
        float RushMult => Rushing ? Manager.RushDamage : 1;

        public void Tick(float dt)
        {
            born = Mathf.Min(1, born + dt * 3);
            if (UltOn) { UltTick(dt); return; }          // 필살기가 붙잡은 쥐: 위치·자세는 필살기 코드가 정함
            biteCD -= dt; wallCD -= dt; breedCD -= dt; noBreed -= dt; rushT -= dt; flee -= dt; tumbleT -= dt; frenzy -= dt; zombie -= dt;
            if (temp > 0 && (temp -= dt) <= 0) { Manager.RemoveTemp(this); return; }
            bite = Mathf.Max(0, bite - dt * 5); sq += (1 - sq) * Mathf.Min(1, dt * 10);
            if (z > 0 || vz > 0) { vz -= 1600 * dt; z = Mathf.Max(0, z + vz * dt); if (z <= 0) vz = 0; }

            if (stun > 0)
            {
                // 기절: 죽지 않고 잠깐 전투 불능 (미끄러지며 멈춤)
                stun -= dt; vx *= Mathf.Max(0, 1 - dt * 8); vy *= Mathf.Max(0, 1 - dt * 8); noBreed = Mathf.Max(noBreed, 0.3f); rushT = 0;
                if (Held) { vx = vy = 0; } else Move(dt, false, false);
                return;
            }
            if (act.on) { ActionStep(dt); if (act.on && ActionType != EffectType.Feast && ActionType != EffectType.Dash_Slash) Move(dt, false, false); return; }
            ActTick(dt);
            if (sleep > 0) { sleep -= dt; vx = vy = 0; SnoreTick(dt); return; }
            if (Trick != TrickType.None) { TrickStep(dt); Move(dt, false, false); return; }
            bool rushing = Rushing;
            if (!rushing) PassiveTick(dt);
            if (rushing)
            {
                // 총공격: 클릭 지점으로 빠르게 돌진
                var p = Manager.RushPoint;
                float dx = p.x - x, dy = p.y - y, d = Mathf.Max(0.001f, Mathf.Sqrt(dx * dx + dy * dy)), sp = RunSpeed * Manager.rushSpeedMult;
                float tx = d > 30 ? dx / d * sp : Random.Range(-sp, sp), ty = d > 30 ? dy / d * sp : Random.Range(-sp, sp);
                vx += (tx - vx) * Mathf.Min(1, dt * 8); vy += (ty - vy) * Mathf.Min(1, dt * 8);
                mode = Mode.Run;
            }
            else
            {
                t -= dt;
                if (mode == Mode.Run) { if (t <= 0) { StopDash(); Stomp(); } }
                else { vx = vy = 0; if (t <= 0 && !TrySleep()) NewDash(); }
            }
            Move(dt, rushing, true);
        }

        // 바퀴벌레처럼: 아무 방향으로 휙 달렸다가 멈칫
        void NewDash()
        {
            if (rushLock > 0 && !(rushT > 0)) rushLock--;      // 총공격 뒤 한 번 흩어지면 다시 번식 가능
            float a = Random.Range(0, Mathf.PI * 2);
            var st = Manager.Stage; var room = StageManager.RoomOf(x, y);
            if (Random.value < 0.35f)
            {
                var best = Manager.Items.Nearest(x, y, 280);
                if (best) a = Mathf.Atan2(best.y - y, best.x - x) + Random.Range(-0.3f, 0.3f);
            }
            else if (Random.value < 0.12f)
            {
                // 가끔 열린 옆방으로 이사 (맵 전체로 퍼짐)
                var opens = new List<Vector2Int>();
                foreach (var d in StageManager.Dirs) if (st.IsOpen(room.x + d.x, room.y + d.y)) opens.Add(d);
                if (opens.Count > 0) { var d = opens[Random.Range(0, opens.Count)]; a = Mathf.Atan2((room.y + d.y + 0.5f) * World.RH - y, (room.x + d.x + 0.5f) * World.RW - x) + Random.Range(-0.4f, 0.4f); }
            }
            else if (Random.value < 0.15f)
            {
                // 가끔 막힌 벽 쪽으로 (벽 부수기)
                var sides = st.ClosedSides(room.x, room.y);
                if (sides.Count > 0) { var d = sides[Random.Range(0, sides.Count)]; a = Mathf.Atan2(d.y, d.x) + Random.Range(-0.7f, 0.7f); }
            }
            var boss = Boss.Current;
            if (boss && boss.CanHit && Random.value < 0.45f && Manager.Ults && Manager.Ults.OnScreen(boss.x, boss.y, 0)) a = Mathf.Atan2(boss.y - y, boss.x - x) + Random.Range(-0.3f, 0.3f);   // 보스한테 우르르
            else if (st.Open.Contains(st.StairsRoom) && !(boss && boss.Blocking) && Random.value < 0.12f) { var s = st.StairsPos; a = Mathf.Atan2(s.y - y, s.x - x) + Random.Range(-0.3f, 0.3f); }
            // 쥐덫의 치즈 미끼에 홀려서 (가끔)
            var bait = Random.value < 0.1f ? st.NearestArmedTrap(x, y, 320) : null;
            if (bait) a = Mathf.Atan2(bait.y - y, bait.x - x) + Random.Range(-0.08f, 0.08f);
            if (flee > 0) a = Mathf.Atan2(y - fleeY, x - fleeX) + Random.Range(-0.7f, 0.7f);        // 고양이한테서 도망
            var pa = Manager.PickupAim(this);
            if (pa.HasValue && Random.value < 0.8f) a = pa.Value + Random.Range(-0.15f, 0.15f);     // 같은 종이 떨어뜨린 소품 주우러
            float sp = RunSpeed * Random.Range(2.2f, 3f) * DashSpeedMult * GrowthDashMult * (flee > 0 ? 1.25f : 1);
            vx = Mathf.Cos(a) * sp; vy = Mathf.Sin(a) * sp;
            mode = Mode.Run; t = Random.Range(0.18f, 0.45f); sq = 0.75f; dashHits.Clear();
        }

        public void StopDash(float t0 = 0.25f, float t1 = 0.9f)
        {
            if (Speed > 150) stopT = Time.time;
            vx = vy = 0; mode = Mode.Pause; t = Random.Range(t0, t1) * CommonSkill.StopTimeMul; sq = 1.2f;      // 카페인 중독: 덜 멈칫
        }

        public bool Held { get; set; }          // 쥐덫에 걸림
        readonly System.Collections.Generic.HashSet<Item> dashHits = new();
        [HideInInspector] public float catCD;      // 고양이 연속 들이받기 간격

        // 기절 (보스 공격·쥐덫·고양이)
        public void Stun(float t) { stun = Mathf.Max(stun, t); rushT = 0; }

        // 고양이 공격에 맞아 날아감: 데굴데굴
        public void Ragdoll(float ang, float spd, float upV, float stunT)
        {
            vx = Mathf.Cos(ang) * spd; vy = Mathf.Sin(ang) * spd; vz = upV; tumbleT = 0.9f; Stun(stunT);
        }

        // 겁먹음: 반대로 도망 + 그동안 번식 금지
        public void Scare(float fx, float fy, float t)
        {
            flee = Mathf.Max(flee, t); fleeX = fx; fleeY = fy; noBreed = Mathf.Max(noBreed, t + Manager.rushNoBreed);
        }

        // 날아온 물건·사람을 머리로 받아침
        public void Header() { vz = Mathf.Max(vz, 170); bite = 1; sq = 1.3f; }

        // 서로 부딪혀 번식 못 하면 반대로 휙 튀어나감
        public void Bounce(float ang)
        {
            float sp = RunSpeed * Random.Range(2.2f, 3f);
            vx = Mathf.Cos(ang) * sp; vy = Mathf.Sin(ang) * sp; mode = Mode.Run; t = Random.Range(0.12f, 0.3f);
        }

        // 멈칫하며 발 구르기: 가까운 물건을 차냄
        void Stomp()
        {
            float r0 = Radius + 22;
            foreach (var it in Manager.Items.InRange(x, y, r0 + 40))
                if (it.State == Item.ItemState.Rest && Dist(it.x, it.y) < r0 + it.R) it.Damage(Damage, this, false, Mathf.Atan2(it.y - y, it.x - x));
        }

        float Dist(float ox, float oy) => Mathf.Sqrt((ox - x) * (ox - x) + (oy - y) * (oy - y));

        void HitWall(int i, int j, int di, int dj, float v)
        {
            if (Trick == TrickType.Cannon) { Manager.Stage.DamageWall(i, j, di, dj, Damage * 2 * Manager.digMult * CommonSkill.WallDmgMul * WallMult, this); FxManager.I?.Shake(0.04f); return; }
            if (v <= 60 || wallCD > 0) return;
            wallCD = 0.3f; bite = 1; sq = 0.8f;
            Manager.Stage.DamageWall(i, j, di, dj, Damage * Manager.digMult * CommonSkill.WallDmgMul * RushMult * WallMult, this);
            ActTrigger(CondType.Hit_Wall);
            FxManager.I?.Dust(x, y, 2, 0.6f);
        }

        void Move(float dt, bool rushing, bool ai)
        {
            float px = x, py = y;
            x += vx * dt; y += vy * dt;
            float rad = Radius;
            bool wasRun = mode == Mode.Run && !rushing;
            bool edge = Manager.Stage.Confine(ref x, ref y, ref vx, ref vy, rad, px, py, 0.8f, HitWall);
            if (edge && wasRun) StopDash(0.1f, 0.4f);
            Speed = Mathf.Sqrt(vx * vx + vy * vy);
            if (Mathf.Abs(vx) > 20) face = vx > 0 ? 1 : -1;
            if (Speed > 20) walk += dt * Speed / 9;

            if (!ai) return;
            Manager.Cats?.RatBump(this, rushing);
            // 물건 갉기: 부딪히면 피해 + 튕겨나감 (핀볼처럼)
            bool pierce = Pierces && mode == Mode.Run;
            if (biteCD > 0 && !pierce) return;
            foreach (var it in Manager.Items.InRange(x, y, rad + 60))
            {
                if (it.State != Item.ItemState.Rest) continue;
                float dx = x - it.x, dy = y - it.y, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > rad + it.R) continue;
                float nx = dx / (d > 0 ? d : 1), ny = dy / (d > 0 ? d : 1);
                if (pierce)
                {
                    // 뚫고 지나가기: 같은 돌진에서 같은 물건은 한 번만
                    if (dashHits.Contains(it)) continue;
                    dashHits.Add(it); bite = 1;
                    Bump(it, nx, ny, Speed);
                    FxManager.I?.Stars(it.x, it.y, 16, 2, Color.white, Color.white, 200, 300);
                    continue;
                }
                x = it.x + nx * (rad + it.R + 1); y = it.y + ny * (rad + it.R + 1);
                float dot = vx * nx + vy * ny; if (dot < 0) { vx -= 2 * dot * nx; vy -= 2 * dot * ny; }
                biteCD = 0.22f; bite = 1; sq = 1.25f;
                float spd = Speed;
                if (!rushing) StopDash(0.15f, 0.5f);         // 들이받고 멈칫 (총공격 중엔 계속 돌진)
                Bump(it, nx, ny, spd);
                return;
            }
            if (BumpBoss(rushing)) return;
            BumpHumans(rushing);
        }

        // 보스 들이받기 (웹: 보스도 사람 목록에 있어서 ratBumpHumans 로 맞음)
        bool BumpBoss(bool rushing)
        {
            var b = Boss.Current;
            if (!b || !b.CanHit || b.z > 40) return false;
            float dx = x - b.x, dy = y - b.y, d = Mathf.Sqrt(dx * dx + dy * dy), R = Radius + b.R;
            if (d > R) return false;
            float nx = dx / (d > 0 ? d : 1), ny = dy / (d > 0 ? d : 1);
            x = b.x + nx * (R + 1); y = b.y + ny * (R + 1);
            float dot = vx * nx + vy * ny; if (dot < 0) { vx -= 2 * dot * nx; vy -= 2 * dot * ny; }
            biteCD = 0.22f; bite = 1; sq = 1.25f;
            if (!rushing) StopDash(0.1f, 0.35f);
            bool crit = Random.value < CritChance + GrowthCritAdd + CommonSkill.CritAdd;
            b.Damage(Damage * RushMult * (crit ? CritMult : 1), this, Mathf.Atan2(-ny, -nx), crit);
            FxManager.I?.Stars(b.x - nx * b.R * 0.5f, b.y - ny * b.R * 0.5f, 40, crit ? 6 : 2, Color.white, new Color(1, 0.95f, 0.75f));
            return true;
        }

        // 사람 들이받기 (웹게임 ratBumpHumans)
        void BumpHumans(bool rushing)
        {
            float rad = Radius;
            foreach (var h in Manager.Items.Humans)
            {
                if (h.State == Human.HState.Fly || h.State == Human.HState.Splat || h.State == Human.HState.Dead || h.z > 40) continue;
                float dx = x - h.x, dy = y - h.y, d = Mathf.Sqrt(dx * dx + dy * dy), R = rad + h.R;
                if (d > R) continue;
                float nx = dx / (d > 0 ? d : 1), ny = dy / (d > 0 ? d : 1);
                x = h.x + nx * (R + 1); y = h.y + ny * (R + 1);
                float dot = vx * nx + vy * ny; if (dot < 0) { vx -= 2 * dot * nx; vy -= 2 * dot * ny; }
                biteCD = 0.22f; bite = 1; sq = 1.25f;
                if (!rushing) StopDash(0.1f, 0.35f);
                bool crit = Random.value < Manager.baseCritChance + CommonSkill.CritAdd;
                h.Damage(Damage * RushMult * (crit ? Manager.critMultiplier : 1), this, Mathf.Atan2(-ny, -nx), crit);
                FxManager.I?.Stars(h.x - nx * h.R * 0.5f, h.y - ny * h.R * 0.5f, 40, crit ? 6 : 2, Color.white, new Color(1, 0.95f, 0.75f));
                return;
            }
        }

        void Bump(Item it, float nx, float ny, float spd)
        {
            float ang = Mathf.Atan2(-ny, -nx);
            float dmg = Damage * RushMult * DashHitMult(spd);
            bool crit = Random.value < CritChance + GrowthCritAdd + CommonSkill.CritAdd;
            PassiveBeforeHit(it);
            it.Damage(dmg * (crit ? CritMult : 1), this, crit, ang);
            PassiveOnHit(it, dmg, ang);
            if (Random.value < CommonSkill.ZapChance) Manager.Items.ZapChain(this, it, dmg * CommonSkill.ZapDamageK, CommonSkill.ZapTargets);   // 전기 이빨
            // 특수 액티브가 먼저: 안 터졌을 때만 공용 스킬 묘기 확률
            if (Trick == TrickType.None && !act.on) { if (!ActTrigger(it.Gold ? CondType.Hit_Gold_Item : CondType.Hit_Item, it) && it.Gold) ActTrigger(CondType.Hit_Item, it); }
            if (Trick == TrickType.None && !act.on && sleep <= 0) RollCommonTrick(it, ang);
            if (spd > 350) vz = 150;           // 세게 들이받으면 작게 튀어오름
        }

        // 웹게임 ratPose (달리기·급정거·킁킁·갉기 박치기·공중·총공격)
        RatRig.Pose MakePose()
        {
            float tt = Time.time;
            var p = new RatRig.Pose { head = Mathf.Sin(tt * 1.7f + seed) * 0.05f, tail = 0.15f + Mathf.Sin(tt * 2.6f + seed) * 0.15f, sx = 1, sy = 1 };
            if (Speed > 30)
            {
                float w = walk, amp = Mathf.Min(1.25f, 0.45f + Speed / 900);
                p.front = Mathf.Sin(w) * amp; p.farFront = Mathf.Sin(w + 0.6f) * amp;
                p.back = Mathf.Sin(w + Mathf.PI) * amp; p.farBack = Mathf.Sin(w + Mathf.PI + 0.6f) * amp;
                p.bob = -Mathf.Abs(Mathf.Sin(w)) * 3.5f; p.tilt = Mathf.Sin(w) * 0.06f;
                p.tail = 0.05f + Mathf.Sin(w * 0.5f) * 0.25f; p.head = Mathf.Cos(w) * 0.05f - 0.05f; p.sx = 1 + Mathf.Sin(w) * 0.05f;
                if (Speed > 450) { p.head = -0.12f; p.tail = -0.15f + Mathf.Sin(tt * 30) * 0.08f; p.sx = 1.1f; p.sy = 0.93f; }
            }
            else if (tt - stopT < 0.2f)
            {
                float k = 1 - (tt - stopT) / 0.2f;
                p.front = 0.9f * k; p.farFront = 0.8f * k; p.back = -0.4f * k; p.farBack = -0.3f * k; p.tilt = -0.18f * k; p.tail = 0.9f * k; p.head = -0.2f * k;
            }
            else if (Mathf.Max(0, Mathf.Sin(tt * 0.9f + seed * 3)) > 0.7f) { p.head = -0.08f + Mathf.Sin(tt * 40) * 0.05f; p.headX = -1.5f; }
            if (UltOn && UltPose.HasValue) { var u = UltPose.Value; if (u.sx == 0) u.sx = 1; if (u.sy == 0) u.sy = 1; return u; }
            if (sleep > 0) { p.front = 1.5f; p.farFront = 1.5f; p.back = -1.5f; p.farBack = -1.5f; p.head = 0.4f; p.tail = -0.6f + Mathf.Sin(tt * 0.8f) * 0.08f; p.bob = 0; p.sy = 0.92f + Mathf.Sin(tt * 2) * 0.02f; return p; }
            if (act.on) { ActionPose(ref p); return p; }
            if (Trick != TrickType.None) { TrickPose(ref p); return p; }
            if (tumbleT > 0 || (stun > 0 && z > 2))
            {
                p.front = Mathf.Sin(tt * 34) * 1.6f; p.farFront = Mathf.Cos(tt * 30) * 1.6f; p.back = Mathf.Sin(tt * 31 + 2) * 1.5f; p.farBack = Mathf.Cos(tt * 27) * 1.5f;
                p.head = Mathf.Sin(tt * 20) * 0.5f; p.tail = Mathf.Sin(tt * 25) * 1.3f; return p;
            }
            if (stun > 0) { p.front = 0.6f; p.farFront = 0.5f; p.back = -0.6f; p.farBack = -0.5f; p.head = 0.4f + Mathf.Sin(tt * 6) * 0.1f; p.tail = -0.3f; p.sy = 0.9f; return p; }
            if (zombie > 0) { p.front = 1.5f + Mathf.Sin(tt * 3 + seed) * 0.15f; p.farFront = 1.4f + Mathf.Cos(tt * 3 + seed) * 0.15f; p.head = 0.2f + Mathf.Sin(tt * 2) * 0.1f; p.tilt = Mathf.Sin(tt * 2.5f + seed) * 0.08f; }   // 좀비: 팔을 앞으로
            if (Rushing) { p.head = 0.18f; p.headX = -3; p.tail = 1.1f + Mathf.Sin(tt * 25) * 0.1f; p.tilt = 0.1f; }
            if (bite > 0)
            {
                float k = Mathf.Sin(bite * Mathf.PI);
                p.headX -= 9 * k; p.head += 0.35f * k; p.front += 0.7f * k; p.farFront += 0.4f * k; p.tilt += 0.08f * k; p.sx *= 1 + 0.08f * k;
            }
            if (z > 2)
            {
                float k = Mathf.Min(1, z / 30);
                p.front = Mathf.Lerp(p.front, 1.3f, k); p.farFront = Mathf.Lerp(p.farFront, 1f, k); p.back = Mathf.Lerp(p.back, -1.3f, k);
                p.farBack = Mathf.Lerp(p.farBack, -1f, k); p.tail = Mathf.Lerp(p.tail, 1.2f, k); p.head = Mathf.Lerp(p.head, -0.3f, k);
            }
            return p;
        }

        void LateUpdate()
        {
            if (!Manager) return;
            transform.position = World.ToUnity(x, y, z) + UltJitter();
            float scale = Manager.ratScale * GradeData.size * EaseOutBack(born);
            TrickTransform(out float lift, out float trot, out float tsx, out float tsy, out float pivotH);
            ActionTransform(ref trot, ref tsx, ref tsy, ref lift);
            UltTransform(ref trot, ref tsx, ref tsy, ref lift);
            MountTransform(ref scale, ref lift);          // 슈퍼 요리사 쥐: 요리사 등 위 (Rat.Mount.cs)
            ZombieTint();
            if (ghost || temp > 0) foreach (var sr in rig.GetComponentsInChildren<SpriteRenderer>()) { var c = sr.color; c.a = ghost ? 0.55f + 0.15f * Mathf.Sin(Time.time * 10) : Mathf.Clamp01(temp / 0.6f); sr.color = c; }
            rig.Apply(MountPose(MakePose()), scale, face, sq, World.SortOrder(y), lift, trot, tsx, tsy, pivotH);
            StunStars();
            UltReadyMark();
            if (shadow)
            {
                shadow.enabled = !HideBody;
                // 몸길이에 맞춘 길쭉한 접지 그림자, 높이 뜰수록 작고 옅게
                float k = (1 - Mathf.Min(0.7f, z / 300)) * EaseOutBack(born);
                float hw = Manager.RigLength(Data) * Manager.ratScale * GradeData.size * Manager.shadowLength * 0.5f * k;
                float sw = shadow.sprite ? shadow.sprite.bounds.size.x : 1;
                shadow.transform.position = World.ToUnity(x + face * hw * 0.08f, y);
                shadow.transform.localScale = new Vector3(hw * 2 * World.U / sw, hw * 2 * Manager.shadowFlat * World.U / sw, 1);
                var c = shadow.color; c.a = Manager.shadowAlpha * (1 - Mathf.Min(0.6f, z / 200)); shadow.color = c;
            }
        }

        static float EaseOutBack(float v) { if (v >= 1) return 1; const float c1 = 1.9f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(v - 1, 3) + c1 * Mathf.Pow(v - 1, 2); }
    }
}
