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

        public Cat Current { get; private set; }

        class Shot { public float x, y, z, vx, vy, vz; public SpriteRenderer r; }
        class Pending { public Cat c; public float t, range; }
        readonly List<Shot> shots = new();
        readonly List<Pending> pending = new();
        float beamT;

        public float CatHP(CatCharacterRow row)
        {
            int f = Game.Floor;
            return Mathf.Max(12 * Mathf.Pow(Items.itemHpGrow, f - 1) * 3 * 4, Stage.PowNeed(f) * hpPowMul) * row.hp_mul;
        }

        public void Clear()
        {
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
            c.Init(this, row, skill, art, x, y, CatHP(row) * CommonSkill.CatHpMul, 3 * Mathf.Pow(Items.valueGrow, Game.Floor - 1) * valueMul * CommonSkill.CheeseMul);
            Current = c;
            var fx = FxManager.I; if (fx) fx.Dust(x, y, 10, 1.4f);
            bool special = row.Category == CatCategory.Special;
            Game.ShowBanner(special ? $"특별 고양이: {row.character_name}!" : $"{row.character_name} 출현!", $"{skill?.skill_name} · 들이받아서 날려버려요!");
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
            if (c) { c.Tick(dt); if (c.Gone) { Destroy(c.gameObject); Current = null; } }
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
