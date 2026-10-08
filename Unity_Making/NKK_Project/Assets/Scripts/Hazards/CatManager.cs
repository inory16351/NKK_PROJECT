using System.Collections.Generic;
using NKK.Data;
using NKK.Items;
using NKK.Rats;
using NKK.Stage;
using UnityEngine;

namespace NKK.Hazards
{
    // 고양이 관리: 등장(StageManager 가 시점을 정함) · 갱신 · 쥐/물건과의 충돌 · 불덩이·레이저 연출.
    // 언제·어떤 고양이가 나오는지(층·주기·특별 고양이 확률)는 StageManager, 고양이 자체 수치는 여기와 고양이 테이블.
    public class CatManager : MonoBehaviour
    {
        [Header("연결")]
        public Cat catPrefab;
        public RatArtLibrary catArt;
        public RatManager Rats;
        public ItemManager Items;
        public StageManager Stage;
        public GameManager Game;
        public Transform catRoot;
        [Tooltip("레이저 눈빛 연출")] public LineRenderer beam;
        [Tooltip("불덩이 그림")] public SpriteRenderer fireballTemplate;

        [Header("고양이 공통 (웹게임 기준)")]
        public float catRadius = 30;
        [Tooltip("몸길이 (게임 단위) × 고양이 테이블 크기 배율")] public float catLength = 115;
        [Tooltip("쥐가 겁먹는 반경")] public float fearRadius = 330;
        [Tooltip("겁먹고 도망치는 시간 (초)")] public float fearTime = 1.3f;
        [Tooltip("기본 덮치기 범위 · 기절 시간")] public float pounceRadius = 70;
        public float pounceStun = 1.4f;
        [Tooltip("체력 = max(12 × 3.6^(층-1) × 3 × 4, 적정 전투력 × 이 값) × 고양이 테이블 체력 배율")] public float hpPowMul = 1.2f;
        [Tooltip("퇴치 치즈 = 3 × 1.8^(층-1) × 이 값")] public float valueMul = 30;
        [Tooltip("쥐가 들이받을 때 피해 배율 (평소 / 총공격 때는 총공격 배율 × 이 값)")] public float bumpNormal = 0.6f, bumpRush = 2;
        [Tooltip("불덩이 착지 반경 · 기절")] public float fireballRadius = 60, fireballStun = 1.2f;

        [Header("무리 스킬 공통 (고양이마다 다른 스킬 = 고양이 테이블 crowd_skill · Crowd_* 효과, 쿨타임 = cond1 Interval)")]
        [Tooltip("무리 판단 반경 (이 안에 쥐가 몇 마리 있나) · 이보다 적으면 그냥 가까운 쥐를 노림")] public float crowdScanRadius = 170;
        public int crowdMin = 3;
        [Tooltip("무리 스킬 발동을 콘솔에 기록 (에디터)")] public bool logCrowd;
        [Tooltip("등장 후 첫 무리 스킬까지 (초)")] public float slamFirst = 1.2f;
        [Tooltip("무리 쪽으로 달려가는 속도 · 이 거리 안이면 웅크림 시작 · 원거리 스킬(레이저·운석·블랙홀) 사거리")] public float slamChaseSpeed = 400, slamLeapRange = 650, castRange = 1100;
        [Tooltip("범위 안 쥐 날리는 속도 · 위로 · 기본 물건 날리는 속도 · 기본 체공 (초)")] public float slamFling = 380, slamUp = 420, slamItemLaunch = 300, slamAir = 0.35f;
        [Tooltip("착지 팝업 (자리표시 {skill} 스킬 이름 · {n} 기절한 쥐 수, 빈칸 = 안 띄움. 기절은 쥐마다 RatManager.stunPopup)")] public string slamPopup = "";
        [Tooltip("웅크릴 때 고양이 말 (무작위)")] public string[] slamCalls = { "거기 모였냥?!", "딱 걸렸냥!", "냥냥냥!!", "다 잡았다옹!" };
        [Tooltip("등장 배너 부제 (자리표시 {skill} {crowd})")] public string spawnSub = "{crowd} · 쥐가 몰린 곳에 빨간 원이 뜨면 피하기 · 들이받아서 날려버려요!";

        [Header("경고 표시 (씬 자식, 꺼 둠 · 경고 원은 필요한 만큼 복제)")]
        [Tooltip("바닥 경고 원 채움 (웅크리는 동안 커짐)")] public SpriteRenderer warnFill;
        [Tooltip("바닥 경고 원 테두리 (최종 범위, 깜빡)")] public SpriteRenderer warnRim;
        [Tooltip("고양이 머리 위 느낌표")] public SpriteRenderer alertIcon;
        [Tooltip("착지 자리 자국 (발톱 · 균열)")] public SpriteRenderer clawMark;
        public Sprite clawSprite, crackSprite;
        [Tooltip("블랙홀 소용돌이 (바닥, 돌아감)")] public SpriteRenderer vortex;
        [Tooltip("하늘 레이저 기둥 · 운석 틀 (꺼 둠, 복제)")] public SpriteRenderer laserTemplate, meteorTemplate;
        [Tooltip("레이저 기둥 높이 · 운석 떨어지는 시간 (초)")] public float laserHeight = 900, meteorFall = 0.35f;
        [Tooltip("경고 원 진하기 (채움 · 테두리) · 테두리 깜빡 빠르기")] public float warnFillAlpha = 0.38f, warnRimAlpha = 0.95f, warnBlink = 14;
        [Tooltip("느낌표 크기 (게임 단위) · 높이 · 자국 남는 시간")] public float alertSize = 70, alertLift = 130, clawLife = 0.8f;
        float clawT; Vector2 clawPos; float clawSize;
        readonly List<(SpriteRenderer fill, SpriteRenderer rim)> warns = new();
        class Strike { public SpriteRenderer r; public bool meteor, hit, boom = true, custom; public float x, y, rad, stun, t; public string name; }
        readonly List<Strike> strikes = new();
        float vortexK; Vector2 vortexPos; float vortexRad;

        public Cat Current { get; private set; }

        class Shot { public float x, y, z, vx, vy, vz; public SpriteRenderer r; }
        class Pending { public Cat c; public float t, range; }
        readonly List<Shot> shots = new();
        readonly List<Pending> pending = new();
        float beamT;

        public float CatHP(CatCharacterRow row)
        {
            int f = Game.Floor;
            return Mathf.Max(12 * Stage.ItemHpK(f) * 3 * 4, Stage.PowNeed(f) * hpPowMul) * row.hp_mul;
        }

        public void Clear()
        {
            HideWarns(); clawT = 0; vortexK = 0;
            if (alertIcon) alertIcon.gameObject.SetActive(false);
            foreach (var st in strikes) if (st.r) Destroy(st.r.gameObject);
            strikes.Clear();
            if (Current) Destroy(Current.gameObject);
            Current = null;
            foreach (var s in shots) Destroy(s.r.gameObject);
            shots.Clear(); pending.Clear();
        }

        // 화면에 보이는 열린 방 안 무작위 위치에 등장
        public bool Spawn(CatCharacterRow row)
        {
            if (Current || row == null) return false;
            var db = GameDatabase.Instance;
            db.CatSkills.TryGetValue(row.skill, out var skill);
            var art = catArt ? catArt.Get(row.code_id) : null;
            if (art == null) { Debug.LogWarning("[CatManager] 그림 없음: " + row.code_id); return false; }
            var cam = Camera.main;
            Vector2 a = World.FromUnity(cam.ViewportToWorldPoint(new Vector3(0.1f, 0.85f, 0))), b = World.FromUnity(cam.ViewportToWorldPoint(new Vector3(0.9f, 0.15f, 0)));
            float x = 0, y = 0; bool ok = false;
            for (int n = 0; n < 20 && !ok; n++)
            {
                x = Random.Range(a.x, b.x); y = Random.Range(a.y, b.y);
                var k = StageManager.RoomOf(x, y);
                ok = Stage.Open.Contains(k) && !Stage.IsStairsRoom(k.x, k.y);
                if (ok) { x = Mathf.Clamp(x, k.x * World.RW + 80, (k.x + 1) * World.RW - 80); y = Mathf.Clamp(y, k.y * World.RH + 80, (k.y + 1) * World.RH - 80); }
            }
            if (!ok) return false;
            var c = Instantiate(catPrefab, catRoot ? catRoot : transform);
            c.Init(this, row, skill, art, x, y, CatHP(row) * CommonSkill.CatHpMul, 3 * Stage.CheeseK(Game.Floor) * valueMul * CommonSkill.CreatureCheeseMul);
            Current = c;
            var fx = FxManager.I; if (fx) fx.Dust(x, y, 10, 1.4f);
            bool special = row.Category == CatCategory.Special;
            Game.ShowBanner(special ? $"특별 고양이: {row.character_name}!" : $"{row.character_name} 출현!", (spawnSub ?? "").Replace("{skill}", skill?.skill_name ?? "").Replace("{crowd}", db.CatSkills.TryGetValue(row.crowd_skill, out var cs) ? cs.skill_name : ""));
            return true;
        }

        // 쥐가 고양이를 들이받음: 평소엔 약하게, 총공격이면 세게
        public void RatBump(Rat r, bool rushing)
        {
            var c = Current; if (!c || !c.Alive) return;
            float dx = r.x - c.x, dy = r.y - c.y, d = Mathf.Sqrt(dx * dx + dy * dy), R = r.Radius + c.R;
            if (d > R) return;
            float nx = dx / (d > 0 ? d : 1), ny = dy / (d > 0 ? d : 1);
            r.x = c.x + nx * (R + 1); r.y = c.y + ny * (R + 1); r.vx = nx * 300; r.vy = ny * 300;
            if (r.catCD > Time.time) return;
            r.catCD = Time.time + 0.25f;
            c.Damage(r.Damage * (rushing ? Rats.RushDamage * bumpRush : bumpNormal), Mathf.Atan2(-ny, -nx), r);
            FxManager.I?.Ring(c.x, c.y, 40, Color.white, 0.2f);
        }

        // 날아가는 물건에 맞음 (Item 이 부름)
        public bool ItemHit(Item it, float dmg)
        {
            var c = Current; if (!c || !c.Alive) return false;
            if (Vector2.Distance(new Vector2(it.x, it.y), new Vector2(c.x, c.y)) > it.R + c.R || it.z > 80) return false;
            c.Damage(dmg * 2, Mathf.Atan2(it.vy, it.vx), it.By);
            FxManager.I?.Popup(c.x, c.y, "퍽!", Color.white, 20, 0.5f, 60);
            return true;
        }

        public void Beam(float x1, float y1, float x2, float y2)
        {
            if (!beam) return;
            beam.positionCount = 2;
            beam.SetPosition(0, World.ToUnity(x1, y1, 30)); beam.SetPosition(1, World.ToUnity(x2, y2, 30));
            beam.enabled = true; beamT = 0.3f;
        }

        // ── 무리 찾기: 반경 안 쥐가 가장 많은 자리 (그 무리의 평균 위치). avoid 근처(이미 고른 곳)는 뺌, maxD > 0 이면 (cx, cy) 에서 그 거리 안만 ──
        public bool FindCrowd(float cx, float cy, out Vector2 at, out int count, List<Vector2> avoid = null, float maxD = 0)
        {
            at = default; count = 0;
            var list = Rats.Rats; float r2 = crowdScanRadius * crowdScanRadius;
            int best = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i]; if (!o || o.z > 40) continue;
                if (maxD > 0 && Dist2(o, cx, cy) > maxD * maxD) continue;
                if (!Stage.Open.Contains(StageManager.RoomOf(o.x, o.y))) continue;
                if (avoid != null) { bool near = false; foreach (var v in avoid) if ((o.x - v.x) * (o.x - v.x) + (o.y - v.y) * (o.y - v.y) < r2 * 4) near = true; if (near) continue; }
                int n = 0; for (int j = 0; j < list.Count; j++) { var q = list[j]; float dx = q.x - o.x, dy = q.y - o.y; if (dx * dx + dy * dy < r2) n++; }
                if (n > count || (n == count && best >= 0 && Dist2(o, cx, cy) < Dist2(list[best], cx, cy))) { count = n; best = i; }
            }
            if (best < 0) return false;
            float sx = 0, sy = 0; int m = 0; var b = list[best];
            foreach (var q in list) { float dx = q.x - b.x, dy = q.y - b.y; if (dx * dx + dy * dy < r2) { sx += q.x; sy += q.y; m++; } }
            at = new Vector2(sx / m, sy / m);
            return true;
        }
        static float Dist2(Rat o, float x, float y) => (o.x - x) * (o.x - x) + (o.y - y) * (o.y - y);

        // 범위 안 쥐 기절·날림 (필살기 중인 쥐 제외). 기절한 수
        public int StunArea(float x, float y, float rad, float stun, float fling = -1, float up = -1)
        {
            int n = 0;
            foreach (var o in Rats.Rats)
            {
                if (o.UltOn) continue;
                float dx = o.x - x, dy = o.y - y;
                if (dx * dx + dy * dy < rad * rad) { o.Ragdoll(Mathf.Atan2(dy, dx), fling < 0 ? slamFling : fling, up < 0 ? slamUp : up, stun); n++; }
            }
            return n;
        }
        public void LaunchItems(float x, float y, float rad, float speed)
        {
            foreach (var it in Items.InRange(x, y, rad + 40))
                if (it.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(it.x, it.y), new Vector2(x, y)) < rad) it.Launch(Mathf.Atan2(it.y - y, it.x - x), speed, false);
        }
        public void HitFx(float x, float y, float rad, string skillName, int n, float shake = 0.35f)
        {
            var fx = FxManager.I; if (!fx) return;
            fx.Ring(x, y, rad, new Color(0.89f, 0.38f, 0.25f), 0.5f); fx.Ring(x, y, rad * 0.6f, Color.white, 0.4f);
            fx.Dust(x, y, 14, 2); fx.Anim("poof", x, y, 0, 1.6f); fx.Shake(shake); fx.Hitstop(0.06f);
            if (!string.IsNullOrEmpty(slamPopup)) fx.Popup(x, y, slamPopup.Replace("{skill}", skillName).Replace("{n}", n.ToString()), new Color(1f, 0.6f, 0.45f), 28, 1.1f, 120);
        }
        public string SlamCall() => slamCalls != null && slamCalls.Length > 0 ? slamCalls[Random.Range(0, slamCalls.Length)] : null;

        // 바닥 경고 원 i 번 (k = 0~1 차오름, rad = 최종 반경)
        public void ShowWarn(int i, float x, float y, float rad, float k)
        {
            if (!warnFill || !warnRim) return;
            while (warns.Count <= i)
            {
                if (warns.Count == 0) warns.Add((warnFill, warnRim));
                else warns.Add((Instantiate(warnFill, warnFill.transform.parent), Instantiate(warnRim, warnRim.transform.parent)));
            }
            var (f, r) = warns[i];
            if (!f.gameObject.activeSelf) { f.gameObject.SetActive(true); r.gameObject.SetActive(true); }
            PlaceDecal(f, x, y, rad * Mathf.Lerp(0.15f, 1, k), warnFillAlpha * (0.6f + 0.4f * k));
            PlaceDecal(r, x, y, rad, warnRimAlpha * (k > 0.6f ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * warnBlink)) : 1));
        }
        public void HideWarns(int from = 0) { for (int i = from; i < warns.Count; i++) { warns[i].fill.gameObject.SetActive(false); warns[i].rim.gameObject.SetActive(false); } }

        public void ShowAlert(Cat c, bool on)
        {
            if (!alertIcon) return;
            if (alertIcon.gameObject.activeSelf != on) alertIcon.gameObject.SetActive(on);
            if (!on || !c) return;
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 10)) * 10;
            alertIcon.transform.position = World.ToUnity(c.x, c.y, c.z + alertLift * c.Data.size_mul + bob);
            float w = alertSize * World.U, sw = alertIcon.sprite ? alertIcon.sprite.bounds.size.x : 1;
            alertIcon.transform.localScale = Vector3.one * (w / sw) * (1 + 0.15f * Mathf.Sin(Time.time * 18));
            alertIcon.sortingOrder = World.SortOrder(c.y) + 60;
        }
        public void ShowClaw(float x, float y, float rad, bool crack = false)
        {
            clawT = clawLife; clawPos = new Vector2(x, y); clawSize = rad * (crack ? 1.6f : 0.9f);
            if (clawMark) { var sp = crack ? crackSprite : clawSprite; if (sp) clawMark.sprite = sp; }
        }
        // 블랙홀 (고양이가 매 프레임 켬)
        public void ShowVortex(float x, float y, float rad, float k) { vortexPos = new Vector2(x, y); vortexRad = rad; vortexK = Mathf.Max(0.01f, k); }

        // 하늘 레이저 · 운석: 떨어지는 순간 범위 기절
        // sprite = 떨어지는 그림 바꾸기 (보스 고깔·서류 등, 운석처럼 떨어짐), boom = 착지 폭발 (끄면 펑 연기)
        public void AddStrike(bool meteor, float x, float y, float rad, float stun, string skillName, Sprite sprite = null, bool boom = true)
        {
            var tpl = meteor ? meteorTemplate : laserTemplate;
            if (!tpl) { int n = StunArea(x, y, rad, stun); HitFx(x, y, rad, skillName, n); return; }
            var r = Instantiate(tpl, tpl.transform.parent); r.gameObject.SetActive(true);
            if (sprite) r.sprite = sprite;
            strikes.Add(new Strike { r = r, meteor = meteor, x = x, y = y, rad = rad, stun = stun, name = skillName, boom = boom, custom = sprite != null });
        }

        void UpdateStrikes(float dt)
        {
            for (int i = strikes.Count - 1; i >= 0; i--)
            {
                var s = strikes[i]; s.t += dt;
                var r = s.r; float sw = r.sprite ? r.sprite.bounds.size.x : 1, sh = r.sprite ? r.sprite.bounds.size.y : 1;
                if (s.meteor)
                {
                    float k = Mathf.Clamp01(s.t / meteorFall), w = s.rad * (s.custom ? 1.1f : 0.9f) * World.U / sw;
                    r.transform.position = World.ToUnity(s.x + (1 - k) * (s.custom ? 60 : 420), s.y, (1 - k) * 1100);
                    if (s.custom) r.transform.rotation = Quaternion.Euler(0, 0, (1 - k) * 540);
                    r.transform.localScale = Vector3.one * w; r.sortingOrder = World.SortOrder(s.y) + 80;
                    if (k >= 1 && !s.hit)
                    {
                        s.hit = true; int n = StunArea(s.x, s.y, s.rad, s.stun); LaunchItems(s.x, s.y, s.rad, slamItemLaunch);
                        HitFx(s.x, s.y, s.rad, s.name, n, 0.3f); FxManager.I?.Anim(s.boom ? "explosion" : "poof", s.x, s.y, 0, s.rad / 70); if (s.boom) ShowClaw(s.x, s.y, s.rad, true);
                    }
                    if (s.hit) { Destroy(r.gameObject); strikes.RemoveAt(i); }
                }
                else
                {
                    // 레이저: 위에서 기둥이 내려꽂힘 → 잠깐 머묾 → 가늘어지며 사라짐 (그림 아래 끝 = 바닥)
                    const float drop = 0.12f, hold = 0.25f, fade = 0.2f;
                    float w = s.rad * 0.7f * World.U / sw, hFull = laserHeight * World.U / sh;
                    float kd = Mathf.Clamp01(s.t / drop), kf = Mathf.Clamp01((s.t - drop - hold) / fade);
                    r.transform.position = World.ToUnity(s.x, s.y, laserHeight * 0.5f * kd);
                    r.transform.localScale = new Vector3(w * (1 - kf * 0.8f), hFull * kd, 1);
                    var c = r.color; c.a = 1 - kf; r.color = c; r.sortingOrder = World.SortOrder(s.y) + 80;
                    if (kd >= 1 && !s.hit) { s.hit = true; int n = StunArea(s.x, s.y, s.rad, s.stun, 120, 300); HitFx(s.x, s.y, s.rad, s.name, n, 0.25f); FxManager.I?.Spark(s.x, s.y, 0, new Color(1f, 0.45f, 0.55f), s.rad); }
                    if (kf >= 1) { Destroy(r.gameObject); strikes.RemoveAt(i); }
                }
            }
        }

        void PlaceDecal(SpriteRenderer r, float x, float y, float rad, float alpha)
        {
            float sw = r.sprite ? r.sprite.bounds.size.x : 1, w = rad * 2 * World.U / sw;
            r.transform.position = World.ToUnity(x, y);
            r.transform.localScale = new Vector3(w, w * World.TILT, 1);
            var c = r.color; c.a = alpha; r.color = c;
        }

        void UpdateDecals(float dt)
        {
            if (clawMark)
            {
                bool on = clawT > 0; if (clawMark.gameObject.activeSelf != on) clawMark.gameObject.SetActive(on);
                if (on)
                {
                    clawT -= dt;
                    float sw = clawMark.sprite ? clawMark.sprite.bounds.size.x : 1, w = clawSize * World.U / sw, k = clawT / clawLife;
                    clawMark.transform.position = World.ToUnity(clawPos.x, clawPos.y);
                    clawMark.transform.localScale = new Vector3(w, w * World.TILT, 1) * (1 + (1 - k) * 0.15f);
                    var c = clawMark.color; c.a = Mathf.Clamp01(k * 2); clawMark.color = c;
                }
            }
            if (vortex)
            {
                bool on = vortexK > 0; if (vortex.gameObject.activeSelf != on) vortex.gameObject.SetActive(on);
                if (on)
                {
                    PlaceDecal(vortex, vortexPos.x, vortexPos.y, vortexRad * Mathf.Lerp(0.3f, 1, Mathf.Clamp01(vortexK)), 0.85f);
                    vortex.transform.rotation = Quaternion.Euler(0, 0, Time.time * -260);
                    vortexK = Mathf.Max(0, vortexK - dt * 4);      // 고양이가 매 프레임 다시 켬, 안 켜면 금방 사라짐
                }
            }
        }

        void Awake()
        {
            foreach (var r in new[] { warnFill, warnRim, alertIcon, clawMark, vortex, laserTemplate, meteorTemplate }) if (r) r.gameObject.SetActive(false);
            if (clawMark && !clawSprite) clawSprite = clawMark.sprite;
        }

        public void QueueFireball(Cat c, float delay, float range) => pending.Add(new Pending { c = c, t = delay, range = range });

        void Fire(Cat c, float range)
        {
            if (!fireballTemplate) return;
            var cand = new List<Rat>();
            foreach (var o in Rats.Rats) if (Vector2.Distance(new Vector2(o.x, o.y), new Vector2(c.x, c.y)) < range) cand.Add(o);
            if (cand.Count == 0) return;
            var t = cand[Random.Range(0, cand.Count)];
            var r = Instantiate(fireballTemplate, fireballTemplate.transform.parent); r.gameObject.SetActive(true);
            shots.Add(new Shot { x = c.x, y = c.y, z = 60, vx = (t.x - c.x) / 0.6f, vy = (t.y - c.y) / 0.6f, vz = 380, r = r });
        }

        void Update()
        {
            if (FxManager.WorldFreeze) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            var c = Current;
            if (c) { c.Tick(dt); if (c.Gone) { Destroy(c.gameObject); Current = null; HideWarns(); ShowAlert(null, false); } }
            UpdateStrikes(dt); UpdateDecals(dt);
            for (int i = pending.Count - 1; i >= 0; i--) { var p = pending[i]; p.t -= dt; if (p.t <= 0) { if (p.c && p.c == Current) Fire(p.c, p.range); pending.RemoveAt(i); } }
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var s = shots[i];
                s.vz -= 1500 * dt; s.x += s.vx * dt; s.y += s.vy * dt; s.z += s.vz * dt;
                s.r.transform.position = World.ToUnity(s.x, s.y, s.z);
                s.r.transform.Rotate(0, 0, 360 * dt);
                s.r.sortingOrder = World.SortOrder(s.y) + 5;
                if (s.z > 0) continue;
                foreach (var o in Rats.Rats) if (Vector2.Distance(new Vector2(o.x, o.y), new Vector2(s.x, s.y)) < fireballRadius) o.Ragdoll(Mathf.Atan2(o.y - s.y, o.x - s.x), 300, 300, fireballStun);
                var fx = FxManager.I; if (fx) { fx.Ring(s.x, s.y, fireballRadius, new Color(0.89f, 0.6f, 0.35f), 0.4f); fx.Burst(s.x, s.y, 10, 10, new Color(0.94f, 0.78f, 0.47f), new Color(0.89f, 0.6f, 0.35f)); fx.Anim("explosion", s.x, s.y, 0, fireballRadius / 70); fx.Shake(0.08f); }
                Destroy(s.r.gameObject); shots.RemoveAt(i);
            }
            if (beam && beamT > 0) { beamT -= dt; if (beamT <= 0) beam.enabled = false; }
        }
    }
}
