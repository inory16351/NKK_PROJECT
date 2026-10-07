using System.Collections.Generic;
using NKK.Data;
using NKK.Items;
using NKK.Stage;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NKK.Rats
{
    // 쥐 무리: 시작 쥐 · 갱신 · 번식(쥐끼리 부딪히면 확률로 탄생) · 클릭 총공격. 수치는 인스펙터에서 조정.
    public class RatManager : MonoBehaviour
    {
        [Header("연결")]
        public Rat ratPrefab;
        public RatArtLibrary artLibrary;
        public StageManager Stage;
        public ItemManager Items;
        public NKK.Hazards.CatManager Cats;
        public GameManager Game;
        [Tooltip("필살기 게이지 매니저")] public NKK.Ults.UltimateManager Ults;
        public Transform ratRoot;
        [Tooltip("쥐 그림자를 모아 두는 곳 (바닥 바로 위에 그림)")] public Transform shadowRoot;
        public int shadowSortOrder = -20000;
        [Tooltip("그림자 길이 = 몸길이 × 이 값")] public float shadowLength = 0.95f;
        [Tooltip("그림자 세로/가로 비율")] public float shadowFlat = 0.36f;
        [Tooltip("그림자 진하기 (알파)")] public float shadowAlpha = 0.36f;
        [Tooltip("총공격 지점 표시 (클릭한 곳)")] public SpriteRenderer rushMarker;
        [Tooltip("총공격 표시 지름 (게임 단위, 시작 → 끝)")] public float rushMarkerSize0 = 60, rushMarkerSize1 = 100;

        [Header("기절 별 (기절·데굴데굴 중 머리 위)")]
        public Sprite stunStarSprite;
        public Color stunStarColor = new(0.94f, 0.78f, 0.47f);
        [Tooltip("별 크기 · 머리 위 높이 (게임 단위, 쥐 크기 배율이 곱해짐)")] public float stunStarSize = 11, stunStarHeight = 30;

        [Tooltip("좀비 상태 쥐 색 (좀비 아포칼립스 필살기)")] public Color zombieTint = new(0.72f, 0.92f, 0.62f);

        [Header("시작 쥐")]
        [Tooltip("테스트용: 시작할 때 만들 쥐 (코드 id). 비어 있으면 티어 테이블 시작 마릿수만큼 탄생 확률로 뽑음")] public List<string> startRats = new();
        [Tooltip("0 이면 티어 테이블 start_rat_count")] public int startCount = 0;

        [Header("쥐 공통 수치 (웹게임 기준)")]
        [Tooltip("쥐 그림 기본 배율 (RAT_SCALE)")] public float ratScale = 1.25f;
        [Tooltip("충돌 반지름 = 이 값 × 등급 크기")] public float ratRadius = 11;
        [Tooltip("기본 이동 속도 = 이 값 × 등급 이동 속도")] public float baseSpeed = 200;
        public float baseCritChance = 0.05f;
        public float critMultiplier = 3;
        [Tooltip("물건에 부딪힐 때 백덤블링 기본 확률 (백덤블링 스킬로 늘어남)")] public float baseFlipChance = 0.04f;
        [Tooltip("벽 피해 배율 (굴착 본능 스킬 자리)")] public float digMult = 1;
        [Tooltip("체형별 몸길이 (게임 단위): Rat, Mouse, Hamster, Gerbil, Squirrel")]
        public float[] rigLengthByBody = { 46, 36, 34, 38, 25 };
        [Tooltip("종별 몸길이 덮어쓰기 (줴리 27)")] public List<RigLengthOverride> rigLengthOverrides = new() { new RigLengthOverride { codeId = "jwerry", length = 27 } };
        [System.Serializable] public class RigLengthOverride { public string codeId; public float length; }

        [Tooltip("멈칫 끝에 잠들 확률 (코골이 능력은 테이블 값)")] public float sleepChance = 0.01f;
        [Tooltip("떼거리 판정 반경 (Pack_Power value_03 이 우선)")] public float packRadius = 120;

        [Header("번식 (웹게임 기준)")]
        [Tooltip("최대 인구 (둥지 확장 스킬로 늘어남)")] public int popCap = 30;
        public int PopCap => popCap + CommonSkill.MaxPopAdd;
        [Tooltip("번식 쿨타임 (초)")] public float breedCool = 4;
        [Tooltip("이 마리 수까지는 부딪히면 100% 탄생")] public int breedFree = 5;
        [Tooltip("확률 = 1 / (1 + ((인구 ÷ 최대 마리 수) / 이 비율)^지수). 최대 마리 수의 이 비율일 때 50%")] [Range(0.05f, 1)] public float breedHalfRatio = 0.5f;
        [Tooltip("클수록 최대 마리 수 가까이에서 확 떨어짐")] public float breedRatioExp = 3;
        public float breedMinChance = 0.002f;
        [Tooltip("탄생 실패 시 둘 다 쉬는 시간")] public float breedFailCD = 0.8f;

        [Header("총공격 (클릭)")]
        [Tooltip("돌진 시간 (초)")] public float rushTime = 1.5f;
        [Tooltip("돌진 중 피해 배율")] public float rushDamageMult = 1.5f;
        public float RushTime => rushTime + CommonSkill.RushTimeAdd;           // 총공격 스킬
        public float RushDamage => rushDamageMult + CommonSkill.RushMulAdd;
        [Tooltip("돌진 속도 = 이동 속도 × 이 값")] public float rushSpeedMult = 2.6f;
        [Tooltip("돌진 끝나고도 번식 금지 (초)")] public float rushNoBreed = 1;
        [Tooltip("이 픽셀보다 많이 끌면 클릭이 아니라 화면 이동")] public float clickDragPixels = 16;
        [Tooltip("클릭 총공격 쿨타임: 돌진이 끝난 뒤 이 초 동안 다시 못 씀 (공용 스킬로 줄어듦)")] public float rushCooldown = 3;
        [Tooltip("공용 스킬로 줄어도 최소 (초)")] public float rushCooldownMin = 1;
        [Tooltip("같은 대상이 이 시간(초) 안에 총공격으로 맞은 횟수를 셈")] public float rushStackWindow = 0.5f;
        [Tooltip("그 시간 안에 이 횟수까지는 피해 그대로")] public int rushStackMax = 6;
        [Tooltip("넘은 타격의 피해 배율")] [Range(0, 1)] public float rushStackOverMult = 0.1f;
        public float RushCooldown => Mathf.Max(rushCooldownMin, rushCooldown - CommonSkill.RushCdLess);
        public bool RushReady => rushCdLeft <= 0;
        float rushCdLeft;
        readonly Dictionary<object, (float t0, int n)> rushStacks = new();
        // 총공격 중첩 제한: 같은 대상(물건·사람·보스·벽 키)이 짧은 시간에 너무 많이 맞으면 넘은 타격은 약하게
        public float RushStack(object target)
        {
            if (target == null) return 1;
            float now = Time.time;
            if (rushStacks.Count > 400) { var old = new List<object>(); foreach (var kv in rushStacks) if (now - kv.Value.t0 > rushStackWindow) old.Add(kv.Key); foreach (var o in old) rushStacks.Remove(o); }
            var s = rushStacks.TryGetValue(target, out var v) && now - v.t0 <= rushStackWindow ? (v.t0, v.n + 1) : (now, 1);
            rushStacks[target] = s;
            return s.Item2 <= rushStackMax ? 1 : rushStackOverMult;
        }

        public readonly List<Rat> Rats = new();
        readonly List<(float t, System.Action a)> timers = new();

        // 잠깐 뒤에 실행 (웹게임 later)
        public void Later(float t, System.Action a) => timers.Add((t, a));

        // 리더 버프 · 떼거리 인원 (매 프레임, 웹게임 updateAuras)
        void UpdateAuras()
        {
            foreach (var r in Rats) { r.buff = 0; r.packN = 0; }
            foreach (var r in Rats)
            {
                if (!r.IsLeader && !r.IsPack) continue;
                float R0 = r.IsLeader ? r.Passive.value_03 : (r.Passive.value_03 > 0 ? r.Passive.value_03 : packRadius);
                int n = 0;
                foreach (var o in Rats)
                {
                    if (o == r || Mathf.Abs(o.x - r.x) > R0 || Mathf.Abs(o.y - r.y) > R0 || Vector2.Distance(new Vector2(o.x, o.y), new Vector2(r.x, r.y)) > R0) continue;
                    n++; if (r.IsLeader) o.buff = Mathf.Max(o.buff, r.Passive.value_01);
                }
                if (r.IsPack) r.packN = Mathf.Min(Mathf.RoundToInt(r.Passive.value_02), n);
            }
        }
        public bool RushActive => rushLeft > 0;
        public Vector2 RushPoint { get; private set; }
        float rushLeft;
        Vector2 pressPos; bool pressed, dragged;

        public float RigLength(RatCharacterRow r)
        {
            foreach (var o in rigLengthOverrides) if (o.codeId == r.code_id) return o.length;
            int b = (int)r.Body; return b < rigLengthByBody.Length ? rigLengthByBody[b] : 44;
        }

        // 화면 안 진짜 쥐 하나 (없으면 null) — 보스 투척 목표
        public Rat RandomOnScreen()
        {
            var l = new List<Rat>();
            foreach (var o in Rats) if (o.temp <= 0 && !o.UltOn && o.OnScreen()) l.Add(o);
            return l.Count > 0 ? l[Random.Range(0, l.Count)] : null;
        }
        public Rat NearestRat(float x, float y, float maxD)
        {
            Rat best = null; float bd = maxD;
            foreach (var r in Rats) { float d = Vector2.Distance(new Vector2(r.x, r.y), new Vector2(x, y)); if (d < bd) { bd = d; best = r; } }
            return best;
        }

        public float TotalPower() { float p = 0; foreach (var r in Rats) if (r.temp <= 0) p += r.Damage; return p; }

        // ── 특수 액션 도우미 ──
        [Header("특수 액션")]
        [Tooltip("화면 안에서 동시에 진행되는 액션 최대 수")] public int maxVisibleActs = 3;
        [Tooltip("총알 그림 (건카타)")] public SpriteRenderer bulletTemplate;
        [Tooltip("드랍 소품 그림 (스킬 에셋이 없을 때)")] public SpriteRenderer pickupTemplate;
        [Tooltip("투척물 그림: 종 코드 → 스프라이트 (없으면 폭탄)")] public List<CodeSprite> throwSprites = new();
        [Tooltip("드랍 소품 그림: 종 코드 → 스프라이트 (쥐 테이블 스킬 에셋)")] public List<CodeSprite> propSprites = new();
        [Tooltip("낙하물 그림: 종 코드 → 스프라이트 (없으면 운석)")] public List<CodeSprite> meteorSprites = new();
        public Sprite defaultMeteor;
        [System.Serializable] public class CodeSprite { public string codeId; public Sprite sprite; }

        public int VisibleActs { get { int n = 0; foreach (var r in Rats) if (r.Acting && r.OnScreen()) n++; return n; } }
        public Sprite ThrowSprite(Rat r) { foreach (var c in throwSprites) if (c.codeId == r.codeId) return c.sprite; return null; }
        public Sprite MeteorSprite(Rat r) { foreach (var c in meteorSprites) if (c.codeId == r.codeId) return c.sprite; return defaultMeteor; }

        // 총알 (건카타)
        class Bullet { public float x, y, vx, vy, life, dmg; public bool skill; public Rat by; public SpriteRenderer r; }
        [Tooltip("치즈 분수 탄환 그림")] public Sprite cheeseBulletSprite;
        public Color cheeseBulletColor = new(0.98f, 0.82f, 0.36f);
        readonly List<Bullet> bullets = new();
        // skill = 특수 액션 탄환 (치즈 추가 패시브 판정), spr·tint = 그림 바꾸기
        public void FireBullet(Rat by, float x, float y, float ang, float speed, float dmg, float life = 0.4f, bool skill = false, Sprite spr = null, Color? tint = null)
        {
            SpriteRenderer r = null;
            if (bulletTemplate)
            {
                r = Instantiate(bulletTemplate, bulletTemplate.transform.parent); r.gameObject.SetActive(true); r.transform.rotation = Quaternion.Euler(0, 0, -ang * Mathf.Rad2Deg);
                if (spr) { r.sprite = spr; r.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f)); }
                if (tint.HasValue) r.color = tint.Value;
            }
            bullets.Add(new Bullet { x = x, y = y, vx = Mathf.Cos(ang) * speed, vy = Mathf.Sin(ang) * speed, life = life, dmg = dmg, skill = skill, by = by, r = r });
        }
        void UpdateBullets(float dt)
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var b = bullets[i];
                b.life -= dt; b.x += b.vx * dt; b.y += b.vy * dt;
                if (!Stage.Open.Contains(StageManager.RoomOf(b.x, b.y))) b.life = 0;
                if (b.life > 0)
                    foreach (var it in Items.InRange(b.x, b.y, 60))
                        if (it.State == NKK.Items.Item.ItemState.Rest && Vector2.Distance(new Vector2(it.x, it.y), new Vector2(b.x, b.y)) < it.R + 4)
                        { b.life = 0; it.Damage(b.dmg, b.by, false, Mathf.Atan2(b.vy, b.vx)); if (b.skill) it.ByAction = true; FxManager.I?.Stars(b.x, b.y, 16, 3, Color.white, new Color(1, 0.95f, 0.75f)); break; }
                if (b.r) { b.r.transform.position = World.ToUnity(b.x, b.y, 16); b.r.sortingOrder = World.SortOrder(b.y) + 4; }
                if (b.life <= 0) { if (b.r) Destroy(b.r.gameObject); bullets.RemoveAt(i); }
            }
        }

        // 소환된 동료: 같은 종 임시 쥐 (번식·전투력·인구에서 빠짐)
        public void SpawnTemp(Rat src, float x, float y, float life)
        {
            if (TempCount >= 40) return;
            var r = Spawn(src.Data, x, y, false);
            if (!r) return;
            r.temp = life; r.ghost = src.codeId == "ninja" || src.codeId == "ghost"; r.noBreed = 99; r.breedCD = 99; r.frenzy = life;
            FxManager.I?.Dust(x, y, 6, 1);
        }
        public int TempCount { get { int n = 0; foreach (var r in Rats) if (r.temp > 0) n++; return n; } }
        public int RealCount => Rats.Count - TempCount;
        public void RemoveTemp(Rat r) { FxManager.I?.Dust(r.x, r.y, 6, 1); Rats.Remove(r); Destroy(r.gameObject); }

        // ── 승급: 같은 등급 N마리 희생 → 윗등급 무작위 1마리 (공용 스킬 확률로 2마리) ──
        // N = 올림(등급 테이블 promote_base × promote_grow^k), k = 이번 판에 그 등급을 승급한 횟수 (판마다 0부터)
        //   N 이 promote_soft 에 닿은 뒤로는 promote_soft × promote_grow2^(넘은 횟수) 로 완만하게 (이어지게)
        [Header("승급")]
        [Tooltip("일괄 승급 때 남겨 둘 마리 수 (번식용, 최소)")] public int promoteKeep = 6;
        [Tooltip("일괄 승급 때 최대 마리 수의 이 비율만큼은 남김")] [Range(0, 1)] public float promoteKeepRatio = 0.5f;
        public int PromoteKeepCount => Mathf.Max(promoteKeep, Mathf.CeilToInt(PopCap * promoteKeepRatio));
        [Tooltip("승급한 쥐 위 팝업 글")] public string promotePopup;
        [Tooltip("공용 스킬로 2마리가 나왔을 때 팝업 글")] public string promoteDoublePopup;
        readonly int[] promoteTimes = new int[6];
        public int PromoteTimes(int g) => g >= 0 && g < promoteTimes.Length ? promoteTimes[g] : 0;
        public int PromoteNeed(int g)
        {
            if (!GameDatabase.Instance.Grades.TryGetValue((Grade)g, out var gr) || gr.promote_base <= 0) return 999;
            float k = PromoteTimes(g), grow = Mathf.Max(1.0001f, gr.promote_grow), n;
            float kSoft = gr.promote_soft > gr.promote_base ? Mathf.Log(gr.promote_soft / gr.promote_base) / Mathf.Log(grow) : float.MaxValue;
            if (k <= kSoft) n = gr.promote_base * Mathf.Pow(grow, k);
            else n = gr.promote_soft * Mathf.Pow(Mathf.Max(1, gr.promote_grow2), k - kSoft);
            return Mathf.Max(1, Mathf.CeilToInt(n - 0.0001f));
        }
        public int CountGrade(int g) { int n = 0; foreach (var r in Rats) if (r.temp <= 0 && !r.UltOn && (int)r.Data.Grade == g) n++; return n; }
        public bool CanPromote(int g) => g >= 0 && g < 5 && GradeOpen(g + 1, Game.Tier) && CountGrade(g) >= PromoteNeed(g) && RealCount - PromoteNeed(g) + 1 >= 2 && !GameOver.Active;
        public Rat Promote(int g)
        {
            if (!CanPromote(g)) return null;
            int need = PromoteNeed(g);
            var pool = new List<Rat>();
            foreach (var r in Rats) if (r.temp <= 0 && !r.UltOn && (int)r.Data.Grade == g) pool.Add(r);
            pool.Sort((a, b) => b.OnScreen().CompareTo(a.OnScreen()));       // 화면 안 쥐부터
            float cx = 0, cy = 0; var fx = FxManager.I;
            var gc = GameDatabase.Instance.Grades.TryGetValue((Grade)g, out var gr) && ColorUtility.TryParseHtmlString(gr.color, out var c0) ? c0 : Color.white;
            for (int i = 0; i < need; i++)
            {
                var r = pool[i]; cx += r.x / need; cy += r.y / need;
                if (fx && r.OnScreen()) fx.Stars(r.x, r.y, 10, 8, gc, Color.white, 60, 200);
                Rats.Remove(r); Destroy(r.gameObject);
            }
            promoteTimes[g]++;
            var row = SpeciesOfGrade(g + 1);
            var nr = row != null ? Spawn(row, cx, cy) : null;
            bool twice = Random.value < CommonSkill.PromoteDouble(g);
            if (twice) { var row2 = SpeciesOfGrade(g + 1); if (row2 != null) Spawn(row2, cx + Random.Range(-40f, 40f), cy + Random.Range(-30f, 30f)); }
            var nc = GameDatabase.Instance.Grades.TryGetValue((Grade)(g + 1), out var ng) && ColorUtility.TryParseHtmlString(ng.color, out var c1) ? c1 : Color.white;
            if (fx)
            {
                for (int i = 0; i < 3; i++) fx.Ring(cx, cy, 40 + i * 30, i % 2 == 1 ? Color.white : nc, 0.5f + i * 0.15f);
                var pop = twice && !string.IsNullOrEmpty(promoteDoublePopup) ? promoteDoublePopup : promotePopup;
                if (!string.IsNullOrEmpty(pop)) fx.Popup(cx, cy, pop, nc, twice ? 30 : 26, 1.5f, 60);
                fx.Shake(0.1f);
            }
            Ults?.Flash(nc, 0.15f);
            return nr;
        }
        // 일괄 승급: 낮은 등급부터 되는 만큼 (번식용 PromoteKeepCount 마리는 남김). 승급 횟수
        public int PromoteAll()
        {
            int n = 0;
            for (int guard = 0; guard < 500; guard++)
            {
                int g = -1; for (int k = 0; k < 5; k++) if (CanPromote(k)) { g = k; break; }
                if (g < 0 || RealCount - PromoteNeed(g) + 1 < PromoteKeepCount) break;
                if (!Promote(g)) break;
                n++;
            }
            return n;
        }
        public bool CanPromoteAny { get { for (int k = 0; k < 5; k++) if (CanPromote(k)) return true; return false; } }

        // 드랍 소품 (조건 Drop_Prop): 같은 종이 닿으면 줍고 바로 액션
        class Pickup { public float x, y, t, life; public string code; public SpriteRenderer r; }
        readonly List<Pickup> pickups = new();
        public void OnItemSmashedBy(Rat by, float x, float y)
        {
            if (by) by.OnSmashedItem();
            if (!by || by.temp > 0 || !by.DropsProp || pickups.Count >= 6 || !by.OnScreen()) return;
            if (Random.value >= by.DropChance) return;
            SpriteRenderer r = null;
            var spr = PropSprite(by) ?? ThrowSprite(by);
            if (pickupTemplate) { r = Instantiate(pickupTemplate, pickupTemplate.transform.parent); r.gameObject.SetActive(true); if (spr) r.sprite = spr; }
            pickups.Add(new Pickup { x = x, y = y, life = by.PropLife, code = by.codeId, r = r });
            FxManager.I?.Popup(x, y, "소품이 떨어졌다!", new Color(1, 0.95f, 0.75f), 16, 0.9f, 40);
        }
        Sprite PropSprite(Rat r) { foreach (var c in propSprites) if (c.codeId == r.codeId) return c.sprite; return null; }
        public float? PickupAim(Rat r)
        {
            foreach (var p in pickups) if (p.code == r.codeId && Vector2.Distance(new Vector2(p.x, p.y), new Vector2(r.x, r.y)) < 500) return Mathf.Atan2(p.y - r.y, p.x - r.x);
            return null;
        }
        void UpdatePickups(float dt)
        {
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                var p = pickups[i]; p.t += dt;
                bool taken = false;
                foreach (var r in Rats) if (r.codeId == p.code && r.temp <= 0 && Vector2.Distance(new Vector2(r.x, r.y), new Vector2(p.x, p.y)) < r.Radius + 18) { r.OnPickup(); taken = true; break; }
                if (p.r)
                {
                    p.r.transform.position = World.ToUnity(p.x, p.y, 26 + Mathf.Sin(p.t * 4) * 4);
                    p.r.sortingOrder = World.SortOrder(p.y) + 2;
                    p.r.enabled = !(p.t > p.life - 3 && Mathf.Sin(p.t * 20) > 0);      // 사라지기 전 깜빡임
                }
                if (taken || p.t > p.life) { if (p.r) Destroy(p.r.gameObject); pickups.RemoveAt(i); }
            }
        }


        void Start()
        {
            var db = GameDatabase.Instance;
            int n = startCount > 0 ? startCount : (db.Tiers.TryGetValue(Game.Tier, out var t) ? t.start_rat_count : 6);
            for (int i = 0; i < n; i++)
            {
                RatCharacterRow row = null;
                if (startRats.Count > 0) db.RatsByCode.TryGetValue(startRats[i % startRats.Count], out row);
                row ??= RollSpecies(0);
                if (row != null) Spawn(row, World.RW / 2 + Random.Range(-200f, 200f), World.RH / 2 + Random.Range(-120f, 120f));
            }
            // 공용 스킬 시작 쥐: 그 등급에서 해금된 종 하나씩 (없으면 아래 등급)
            if (startRats.Count == 0)
                foreach (var (g, cnt) in CommonSkill.StartRats())
                    for (int i = 0; i < cnt; i++) { var row = SpeciesOfGrade(g); if (row != null) Spawn(row, World.RW / 2 + Random.Range(-200f, 200f), World.RH / 2 + Random.Range(-120f, 120f)); }
        }

        public void PlaceAll(float cx, float cy)
        {
            foreach (var r in Rats) r.Place(cx + Random.Range(-World.RW * 0.3f, World.RW * 0.3f), cy + Random.Range(-World.RH * 0.25f, World.RH * 0.25f));
            rushLeft = 0;
        }

        // obtain = 진짜로 얻은 쥐 (조각·도감 기록). 소환된 임시 쥐는 false
        public Rat Spawn(RatCharacterRow row, float x, float y, bool obtain = true)
        {
            var art = artLibrary.Get(row.code_id);
            if (art == null) { Debug.LogWarning($"[RatManager] 그림 없음: {row.code_id}"); return null; }
            var rat = Instantiate(ratPrefab, ratRoot ? ratRoot : transform);
            rat.Init(this, row, GameDatabase.Instance.GradeOf(row), art, x, y);
            rat.breedCD = breedCool;
            Rats.Add(rat);
            if (obtain && Progress.I)
            {
                int lv0 = Progress.I.Level(row.code_id);
                bool isNew = Progress.I.OnRatObtained(row);
                var fx = FxManager.I;
                if (fx && rat.OnScreen())
                {
                    if (isNew) fx.Popup(x, y, "NEW! " + row.character_name, Color.white, 20, 1.3f, 50);
                    else fx.Popup(x, y, "조각 +1", new Color(0.95f, 0.76f, 0.31f), 15, 0.9f, 60);
                    if (Progress.I.Level(row.code_id) > lv0) fx.Popup(x, y, $"{row.character_name} 강화! Lv {Progress.I.Level(row.code_id)}", new Color(0.95f, 0.76f, 0.31f), 20, 1.4f, 80);
                }
                if (Progress.I.Level(row.code_id) > lv0) foreach (var o in Rats) if (o.codeId == row.code_id) o.RefreshGrowth();
            }
            return rat;
        }

        // ── 탄생: 등급 뽑기 (등급 탄생 가중치 × 티어 배율^등급) → 그 등급에서 해금된 종 하나 ──
        public static bool GradeOpen(int g, int tier)
        {
            foreach (var r in GameDatabase.Instance.Rats.Values) if ((int)r.Grade == g && r.unlock_rank <= tier) return true;
            return false;
        }

        // 등급별 탄생 가중치 (등급 탄생 가중치 × (티어 배율 + 돌연변이 + 0.05×보정)^등급). 해금 안 된 등급은 0. 로비 확률 표시도 이걸 씀
        public static float[] GradeWeights(int tier, float bonus = 0)
        {
            var db = GameDatabase.Instance;
            float k = (db.Tiers.TryGetValue(tier, out var t) ? t.birth_grade_k : 1) + CommonSkill.MutationAdd + 0.05f * bonus;   // 돌연변이 유전자
            var w = new float[6];
            for (int g = 0; g < 6; g++) w[g] = db.Grades.TryGetValue((Grade)g, out var gr) && GradeOpen(g, tier) ? gr.birth_weight * Mathf.Pow(k, g) : 0;
            return w;
        }

        // 이 등급(없으면 아래 등급)에서 해금된 종 하나
        public RatCharacterRow SpeciesOfGrade(int grade)
        {
            var db = GameDatabase.Instance;
            for (int g = Mathf.Clamp(grade, 0, 5); g >= 0; g--)
            {
                var list = new List<RatCharacterRow>();
                foreach (var r in db.Rats.Values) if ((int)r.Grade == g && r.unlock_rank <= Game.Tier) list.Add(r);
                if (list.Count > 0) return list[Random.Range(0, list.Count)];
            }
            return null;
        }

        public RatCharacterRow RollSpecies(float bonus)
        {
            var db = GameDatabase.Instance;
            var w = GradeWeights(Game.Tier, bonus); float sum = 0; foreach (var v in w) sum += v;
            float x = Random.value * sum; int pickG = 0;
            for (int g = 0; g < 6; g++) { x -= w[g]; if (x <= 0) { pickG = g; break; } }
            for (int g = pickG; g >= 0; g--)
            {
                var list = new List<RatCharacterRow>();
                foreach (var r in db.Rats.Values) if ((int)r.Grade == g && r.unlock_rank <= Game.Tier) list.Add(r);
                if (list.Count > 0) return list[Random.Range(0, list.Count)];
            }
            return null;
        }

        // 번식 확률: 무료 마리 수 이하는 100%, 그 위로는 최대 마리 수 대비 채운 비율로 떨어짐 (최대 마리 수가 늘면 번식도 같이 잘 됨) + 공용 스킬 번식 확률
        float BreedChance(int pop) => Mathf.Clamp((pop <= breedFree ? 1 : 1f / (1 + Mathf.Pow((float)pop / Mathf.Max(1, PopCap) / breedHalfRatio, breedRatioExp))) + CommonSkill.BreedChanceAdd(pop), breedMinChance, 1);

        void Breed()
        {
            int pop = RealCount;
            var born = new List<Vector3>();      // x, y, 윗등급 보정
            var parents = new List<(Rat a, Rat b)>();
            for (int i = 0; i < Rats.Count; i++)
            {
                var a = Rats[i];
                for (int j = i + 1; j < Rats.Count; j++)
                {
                    var b = Rats[j];
                    if (a.temp > 0 || b.temp > 0 || a.UltOn || b.UltOn) { continue; }
                    float dx = b.x - a.x, dy = b.y - a.y; float rr = a.Radius + b.Radius;
                    if (Mathf.Abs(dx) > rr || Mathf.Abs(dy) > rr) continue;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > rr || d < 0.01f) continue;
                    float ux = dx / d, uy = dy / d, push = (rr - d) / 2;
                    a.x -= ux * push; a.y -= uy * push; b.x += ux * push; b.y += uy * push;
                    bool rushing = RushActive && (a.rushT > 0 || b.rushT > 0);
                    bool canBreed = a.noBreed <= 0 && b.noBreed <= 0 && a.rushLock <= 0 && b.rushLock <= 0 && a.breedCD <= 0 && b.breedCD <= 0 && pop + born.Count < PopCap;
                    bool lucky = canBreed && Random.value < BreedChance(pop + born.Count) * (a.BreedChanceMul + b.BreedChanceMul) / 2;
                    if (canBreed && !lucky) a.breedCD = b.breedCD = breedFailCD;
                    if (lucky)
                    {
                        float cool = breedCool / CommonSkill.BreedCoolDiv;      // 번식력
                        a.breedCD = cool / a.BreedCoolDiv; b.breedCD = cool / b.BreedCoolDiv;
                        born.Add(new Vector3((a.x + b.x) / 2, (a.y + b.y) / 2, a.BirthBonus + b.BirthBonus));
                        parents.Add((a, b));
                        a.StopDash(0.3f, 0.6f); b.StopDash(0.3f, 0.6f);       // 부딪힌 자리에서 딱 멈추고 새끼 탄생
                    }
                    else if (!rushing)
                    {
                        float an = Mathf.Atan2(uy, ux);
                        a.Bounce(an + Mathf.PI + Random.Range(-0.6f, 0.6f)); b.Bounce(an + Random.Range(-0.6f, 0.6f));
                    }
                }
            }
            for (int bi = 0; bi < parents.Count; bi++) { var pr = parents[bi]; if (!pr.a.ActTrigger(CondType.Birth)) pr.b.ActTrigger(CondType.Birth); Ults?.Charge(pr.a, CondType.Birth); Ults?.Charge(pr.b, CondType.Birth); }
            foreach (var p in born)
            {
                var row = RollSpecies(p.z);
                if (row == null) continue;
                var r = Spawn(row, p.x, p.y);
                if (r && (int)row.Grade >= 2) Game.ShowBanner($"{GameDatabase.Instance.GradeOf(row).grade_name} 탄생!", row.character_name);
                // 쌍둥이: 한 마리 더 (최대 인구 안에서)
                if (r && Random.value < CommonSkill.TwinChance && RealCount < PopCap)
                {
                    var row2 = RollSpecies(p.z);
                    if (row2 != null && Spawn(row2, p.x + Random.Range(-14f, 14f), p.y + Random.Range(-10f, 10f)) && r.OnScreen()) FxManager.I?.Popup(p.x, p.y, "쌍둥이!", new Color(1, 0.75f, 0.85f), 18, 1, 60);
                }
                // 탄생 축제: 주변 쥐 광란
                float ft = CommonSkill.FrenzyTime;
                if (r && ft > 0) foreach (var o in Rats) if (Vector2.Distance(new Vector2(o.x, o.y), new Vector2(p.x, p.y)) < CommonSkill.FrenzyRadius) o.frenzy = Mathf.Max(o.frenzy, ft);
            }
        }

        // ── 클릭: 짧게 누르면 총공격, 끌면 화면 이동 (CameraController) ──
        void HandleInput()
        {
            var m = Mouse.current; if (m == null || GameOver.Active) return;      // 게임 오버 습격 중엔 총공격 없음
            Vector2 sp = m.position.ReadValue();
            if (m.leftButton.wasPressedThisFrame) { pressed = !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()); dragged = false; pressPos = sp; }
            if (pressed && (sp - pressPos).magnitude > clickDragPixels) dragged = true;
            if (pressed && m.leftButton.wasReleasedThisFrame)
            {
                pressed = false;
                if (!dragged) ClickRush(World.FromUnity(Camera.main.ScreenToWorldPoint(sp)));
            }
        }

        // 클릭 총공격 (쿨타임 적용). 밸런스 측정도 이걸 씀
        public bool ClickRush(Vector2 p)
        {
            if (!RushReady || RushActive) return false;
            StartRush(p); rushCdLeft = RushTime + RushCooldown;
            return true;
        }

        public void StartRush(Vector2 p)
        {
            float rt = RushTime;
            RushPoint = p; rushLeft = rt;
            // 화면에 보이는 쥐만 모임 (돌진 중엔 번식 금지)
            var cam = Camera.main;
            foreach (var r in Rats)
            {
                var vp = cam.WorldToViewportPoint(r.transform.position);
                if (vp.x < -0.02f || vp.x > 1.02f || vp.y < -0.02f || vp.y > 1.02f) continue;
                r.rushT = rt; r.noBreed = rt + rushNoBreed; r.rushLock = 1;
            }
        }

        // 치즈 운석 (공용 스킬): 주기마다 화면 속 물건 하나에 쿵! 위력 = 무리 평균 힘 × 배율
        [Header("치즈 운석 (공용 스킬)")]
        [Tooltip("운석 폭발 반경 (게임 단위)")] public float meteorRadius = 90;
        [Tooltip("운석 그림 (비어 있으면 defaultMeteor)")] public Sprite cheeseMeteorSprite;
        float meteorT = 5;
        void UpdateMeteor(float dt)
        {
            if (!CommonSkill.MeteorOn || (meteorT -= dt) > 0) return;
            meteorT = CommonSkill.MeteorCool;
            int n = RealCount; if (n == 0) return;
            var t = Items.RandomRestInView();
            if (!t) return;
            Rat by = null; foreach (var r in Rats) if (r.temp <= 0) { by = r; if (Random.value < 0.3f) break; }
            Items.DropMeteor(by, t.x, t.y, meteorRadius, TotalPower() / n * CommonSkill.MeteorPowerK, cheeseMeteorSprite ? cheeseMeteorSprite : defaultMeteor);
            FxManager.I?.Popup(t.x, t.y, "치즈 운석!", new Color(0.95f, 0.76f, 0.31f), 22, 1, 120);
        }

        void Update()
        {
            if (FxManager.WorldFreeze) return;
            HandleInput();
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            rushLeft -= dt; rushCdLeft -= dt;
            for (int i = timers.Count - 1; i >= 0; i--) { var tm = timers[i]; tm.t -= dt; if (tm.t <= 0) { timers.RemoveAt(i); tm.a(); } else timers[i] = tm; }
            UpdateAuras();
            UpdateBullets(dt);
            UpdatePickups(dt);
            for (int i = 0; i < Rats.Count; i++) Rats[i].Tick(dt);
            Breed();
            UpdateMeteor(dt);
            if (rushMarker)
            {
                rushMarker.enabled = RushActive;
                if (RushActive)
                {
                    float k = rushLeft / RushTime;
                    rushMarker.transform.position = World.ToUnity(RushPoint.x, RushPoint.y);
                    float d = Mathf.Lerp(rushMarkerSize0, rushMarkerSize1, 1 - k) * World.U / (rushMarker.sprite ? rushMarker.sprite.bounds.size.x : 1);
                    rushMarker.transform.localScale = new Vector3(d, d * World.TILT, 1);
                    rushMarker.color = new Color(1, 1, 1, 0.5f + 0.3f * Mathf.Sin(Time.time * 12));
                }
            }
        }
    }
}
