using System.Collections.Generic;
using NKK.Data;
using NKK.Humans;
using NKK.Rats;
using NKK.Stage;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NKK.Items
{
    // 물건·사람 생성·갱신·박살 (웹게임 spawnInRoom / furnishRoom / updateSpawns / smashItem / humans.js). 수치는 인스펙터에서 조정.
    public partial class ItemManager : MonoBehaviour
    {
        [Header("연결")]
        public Item itemPrefab;
        public StageManager Stage;
        public GameManager Game;
        public Transform itemRoot;
        public RatManager Rats;
        public NKK.Hazards.CatManager Cats;
        [Tooltip("물건 테이블 asset 칼럼 → 스프라이트. 컴포넌트 메뉴 Fill Item Sprites 로 채움")]
        public List<ItemSprite> sprites = new();
        [System.Serializable] public class ItemSprite { public string codeId; public Sprite sprite; }

        [Header("생성 (웹게임 기준)")]
        [Tooltip("열린 방 하나당 물건 상한 (zoneCap). 물건 사재기 스킬로 늘어남")] public int roomCap = 24;
        public int RoomCap => Mathf.RoundToInt(roomCap * CommonSkill.ItemCapMul);
        [Tooltip("생성 주기 (초)")] public float spawnInterval = 1.2f;
        [Tooltip("물건 체력 = 12 × 체력 배율 × 이 값^(층-1 + 방 거리×0.1)")] public float itemHpGrow = 3.6f;
        [Tooltip("치즈 = 3 × 치즈 배율 × 이 값^(층-1 + 방 거리×0.1)")] public float valueGrow = 1.8f;
        [Tooltip("충돌 반지름 = 테이블 radius × 이 값")] public float radiusScale = 1.45f;
        [Tooltip("방 가장자리 여백")] public float spawnMargin = 50;
        [Tooltip("가구가 하나씩 빠질 확률 (방마다 조금씩 다르게)")] public float furnitureSkip = 0.15f;

        [Header("날아간 물건 타격 (FLY_W · FLY_MULT · FLY_STYLE · AIR_MAX)")]
        public float flyWeight = 0.35f;
        public float flyForceMul = 3;
        public float flyStyle = 0.35f;
        public int airMax = 8;
        [Tooltip("때린 쥐를 모를 때 쓰는 평균 공격력")] public float avgRatDamage = 10;
        [Tooltip("박살 연쇄: 주변 물건에 최대 체력의 이 비율만큼 피해")] public float chainDamage = 0.25f;

        [Header("로켓배송 (택배 웨이브)")]
        [Tooltip("택배가 오는 주기 (초)")] public float waveCool = 60;
        [Tooltip("한 번에 오는 최대 상자 수")] public int waveSize = 15;
        public SpriteRenderer parcelTemplate;

        [Header("특수 능력 투척물")]
        public SpriteRenderer bombTemplate;
        public Sprite flaskSprite;

        [Header("파괴 이펙트")]
        [Tooltip("물건 체력바 폭 (게임 단위)")] public float hpBarWidth = 66;
        [Tooltip("박살 때 뜨는 글자")] public string[] smashWords = { "찍!", "와장창!", "갉갉!", "챙그랑!", "와르르!" };
        [Tooltip("박살 글자가 뜰 확률")] public float smashWordChance = 0.35f;
        [Tooltip("가구 박살 글자")] public string[] furnitureWords = { "와장창!!", "쿠당탕!!", "콰직!!" };

        [Header("사람 (웹게임 humans.js)")]
        public Human humanPrefab;
        public HumanArtLibrary humanArt;
        public Transform humanRoot;
        public float humanRadius = 26;
        [Tooltip("사람 키 (게임 단위)")] public float humanHeight = 150;
        [Tooltip("사람 체력 = 12 × 체력 배율 × 물건 성장^(층-1) × 이 값")] public float humanHpMul = 3;
        [Tooltip("사람 치즈 = 3 × 치즈 배율 × 치즈 성장^(층-1) × 이 값")] public float humanValueMul = 2;
        [Tooltip("사람 상한 = min(최대, 기본 + 층 × 증가)")] public int humanCapMax = 12;
        public int humanCapBase = 4;
        public float humanCapPerFloor = 0.4f;
        [Tooltip("사람 보충 주기 (초, 최소~최대)")] public Vector2 humanRefill = new(18, 30);
        [Tooltip("층 시작 때 사람 수 (1층 / 그 위)")] public int humanStartFirst = 1, humanStartOther = 2;

        [Header("그림자")]
        [Tooltip("접지 그림자 반폭 = 충돌 반지름 × 이 값")] public float contactShadowWidth = 1.05f;
        [Tooltip("접지 그림자 세로/가로 비율")] public float contactShadowFlat = 0.42f;
        [Tooltip("접지 그림자 진하기 (알파)")] public float contactShadowAlpha = 0.34f;

        readonly List<Item> items = new();
        public readonly List<Human> Humans = new();
        readonly List<Item> flying = new();
        float spawnT, humanT = 20;
        Dictionary<string, Sprite> spriteMap;

        public IReadOnlyList<Item> All => items;

        void Awake()
        {
            spriteMap = new();
            foreach (var s in sprites) if (s.sprite) spriteMap[s.codeId] = s.sprite;
        }

        public IEnumerable<Item> InRange(float x, float y, float r)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (Mathf.Abs(it.x - x) < r && Mathf.Abs(it.y - y) < r) yield return it;
            }
        }

        public Item Nearest(float x, float y, float maxD)
        {
            Item best = null; float bd = maxD;
            foreach (var it in items) { if (it.State == Item.ItemState.Dead) continue; float d = Vector2.Distance(new Vector2(it.x, it.y), new Vector2(x, y)); if (d < bd) { bd = d; best = it; } }
            return best;
        }

        public void ClearAll()
        {
            foreach (var it in items) if (it) Destroy(it.gameObject);
            items.Clear();
            foreach (var h in Humans) if (h) Destroy(h.gameObject);
            Humans.Clear(); humanT = 20;
            foreach (var b in bombs) { if (b.r) Destroy(b.r.gameObject); if (b.mv != null) b.mv.Destroy(); }
            bombs.Clear(); ClearDecos();
            foreach (var pc in parcels) if (pc.r) Destroy(pc.r.gameObject);
            parcels.Clear(); waveT = 20;
            FxManager.I?.ClearSpills();
        }

        ItemRow PickWeighted(ZoneRow z)
        {
            var db = GameDatabase.Instance; float s = 0;
            var list = new List<ItemRow>();
            foreach (var id in z.Items()) if (db.Items.TryGetValue(id, out var r)) { list.Add(r); s += r.spawn_weight; }
            float x = Random.value * s;
            foreach (var r in list) { x -= r.spawn_weight; if (x <= 0) return r; }
            return list.Count > 0 ? list[0] : null;
        }

        public void FillRoom(Vector2Int room, int n) { for (int k = 0; k < n; k++) SpawnInRoom(room, true); }

        public bool SpawnInRoom(Vector2Int room, bool instant)
        {
            var row = PickWeighted(GameDatabase.Instance.ZoneOf(Game.Floor));
            if (row == null) return false;
            float rad = row.radius * radiusScale, L = room.x * World.RW, T = room.y * World.RH;
            for (int n = 0; n < 12; n++)       // 빈자리 찾기
            {
                float x = Random.Range(L + World.WM + spawnMargin + rad, L + World.RW - World.WM - spawnMargin - rad);
                float y = Random.Range(T + World.WM + spawnMargin + rad, T + World.RH - World.WM - 40 - rad);
                bool ok = true;
                foreach (var o in InRange(x, y, rad + 80)) if (Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) < o.R + rad + 6) { ok = false; break; }
                if (!ok) continue;
                Spawn(row, x, y, instant);
                return true;
            }
            return false;
        }

        // 방 테마별 가구 배치 (층·방 번호 시드라 같은 층은 늘 같은 배치). 시작 방은 실험실 테마
        public void FurnishRoom(Vector2Int room)
        {
            var db = GameDatabase.Instance;
            var themes = new List<string>();
            foreach (var l in db.FurnitureLayouts) if (!themes.Contains(l.room_theme)) themes.Add(l.room_theme);
            if (themes.Count == 0) return;
            var rnd = new SeededRandom((uint)(Game.Floor * 131 + room.x * 17 + room.y * 71 + 5));
            string theme = room == Vector2Int.zero ? "Lab" : themes[Mathf.FloorToInt(rnd.Next() * themes.Count)];
            float x0 = room.x * World.RW + World.WM + 60, y0 = room.y * World.RH + World.WM + 60, w = World.RW - 2 * (World.WM + 60), h = World.RH - 2 * (World.WM + 60) - 20;
            foreach (var l in db.FurnitureLayouts)
            {
                if (l.room_theme != theme) continue;
                if (rnd.Next() < furnitureSkip) continue;
                if (!db.Items.TryGetValue(l.item_id, out var row)) continue;
                Spawn(row, x0 + (l.pos_u + (rnd.Next() - 0.5f) * 0.04f) * w, y0 + l.pos_v * h, true);
            }
        }

        public Item Spawn(ItemRow row, float x, float y, bool instant)
        {
            spriteMap.TryGetValue(row.code_id, out var spr);
            var r = StageManager.RoomOf(x, y);
            float zi = Game.Floor - 1 + StageManager.RoomDist(r) * 0.1f;
            var it = Instantiate(itemPrefab, itemRoot ? itemRoot : transform);
            float cheese = 3 * row.value_mul * Mathf.Pow(valueGrow, zi) * CommonSkill.CheeseMul * (row.IsFurniture ? CommonSkill.FurnitureCheeseMul : 1);
            it.Init(this, row, spr, x, y, 12 * row.hp_mul * Mathf.Pow(itemHpGrow, zi), cheese, instant);
            items.Add(it);
            if (!row.IsFurniture && Random.value < CommonSkill.GoldChance) it.MakeGold(CommonSkill.GoldCheeseMul, 1);     // 황금 물건 스킬
            return it;
        }

        public void OnSmashed(Item it)
        {
            float gain = it.value * (1 + 0.5f * Mathf.Min(it.Air, airMax)) * (it.Crit ? 2 : 1) * (it.By ? it.By.CheeseMult : 1) * (it.By && it.ByAction ? it.By.SkillKillCheeseMul : 1);
            Game.OnSmash(gain);
            if (it.By) { Rats.OnItemSmashedBy(it.By, it.x, it.y); Rats.Ults?.Charge(it.By, CondType.Destroy_Item); }
            var fx = FxManager.I;
            bool big = it.Data.is_big == 1 || it.Data.IsFurniture;
            float bigK = Mathf.Clamp(it.R / 16, 0.6f, 2.5f);
            if (fx)
            {
                Color spill = Color.clear;
                bool hasSpill = !string.IsNullOrEmpty(it.Data.spill_color) && ColorUtility.TryParseHtmlString(it.Data.spill_color, out spill);
                Color c0 = hasSpill ? spill : new Color(0.85f, 0.85f, 0.82f), c1 = it.Data.is_paper == 1 ? new Color(0.98f, 0.97f, 0.94f) : Color.white;
                fx.Burst(it.x, it.y, 20, Mathf.RoundToInt(5 + bigK * 5), c0, c1, 120 * (0.7f + bigK * 0.3f), 380 * (0.7f + bigK * 0.3f));
                fx.Stars(it.x, it.y, 20, 4, Color.white, new Color(1, 0.95f, 0.75f));
                fx.Dust(it.x, it.y, big ? 6 : 3, big ? 1.4f : 0.8f);
                fx.Ring(it.x, it.y, it.R + 30, new Color(1, 1, 1, 0.55f), 0.3f);
                fx.Anim("poof", it.x, it.y, 0, 0.5f + bigK * 0.35f);
                if (hasSpill) fx.Spill(it.x, it.y, it.R, spill);
                fx.Coin(it.x, it.y);
                fx.Shake(0.02f + bigK * 0.02f);
                if (it.Data.IsFurniture) { fx.Shake(0.25f); fx.Dust(it.x, it.y, 10, 1.6f); fx.Popup(it.x, it.y, furnitureWords[Random.Range(0, furnitureWords.Length)], Color.white, 26, 0.9f, 80); }
                else if (Random.value < smashWordChance) fx.Popup(it.x, it.y, smashWords[Random.Range(0, smashWords.Length)], Color.white, 20, 0.7f, 40);
                if (it.Air >= 2) fx.Popup(it.x, it.y, $"AIR x{it.Air} 보너스!", new Color(0.61f, 0.96f, 1f), 20, 0.9f, 60);
            }
            // 가구: 안에 든 작은 물건들이 우르르 쏟아져서 날아감
            if (it.Data.IsFurniture)
                foreach (var d in it.Data.Drops())
                    if (GameDatabase.Instance.Items.TryGetValue(d, out var r))
                    {
                        var o = Spawn(r, it.x + Random.Range(-30f, 30f), it.y + Random.Range(-20f, 20f), true);
                        o.z = 40; o.By = it.By; o.Launch(Random.Range(0, Mathf.PI * 2), Random.Range(160f, 300f), false); o.vz *= 0.8f;
                    }
            // 작은 연쇄: 주변 물건을 흔들고 최대 체력의 일부 피해 (도미노). 연쇄 폭발 능력이면 더 크게
            float chainAdd = (it.By ? it.By.ChainDamageAdd : 0) + CommonSkill.ChainDamageAdd, rr = it.R + 30 + (it.By ? it.By.ChainRadiusAdd : 0) + CommonSkill.ChainRadiusAdd;
            if (chainAdd > 0) { FxManager.I?.Ring(it.x, it.y, rr, new Color(0.91f, 0.64f, 0.63f, 0.8f), 0.3f); FxManager.I?.Shake(0.05f); }
            foreach (var o in new List<Item>(InRange(it.x, it.y, rr + 60)))
                if (o != it && o.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(o.x, o.y), new Vector2(it.x, it.y)) < rr + o.R)
                    o.Damage(o.hpMax * (chainDamage + chainAdd), it.By, false, Mathf.Atan2(o.y - it.y, o.x - it.x));
        }

        // 충격파: 반경 안 물건에 피해 (사람 착지·펑 등)
        public void Shock(float x, float y, float rad, float dmg, Rat by)
        {
            foreach (var o in new List<Item>(InRange(x, y, rad + 60)))
                if (o.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) < rad + o.R)
                    o.Damage(dmg, by, false, Mathf.Atan2(o.y - y, o.x - x));
            FxManager.I?.Ring(x, y, rad, new Color(1, 1, 1, 0.8f), 0.35f);
        }


        // ── 특수 능력 도우미 ──
        // 범위 피해: 반경 안 물건 + 사람
        // 필살기에 휘말린 사람·고양이 (웹게임 blastActorsIn): 반경 안 사람은 날아가고 고양이는 피해. 휘말린 수
        public int BlastActors(float x, float y, float R, float spd, float dmg, Rat by)
        {
            int n = 0;
            foreach (var h in Humans) if (Vector2.Distance(new Vector2(h.x, h.y), new Vector2(x, y)) < R + h.R && h.Blast(Mathf.Atan2(h.y - y, h.x - x) + Random.Range(-0.3f, 0.3f), spd, dmg, by)) n++;
            var c = Cats ? Cats.Current : null;
            if (c && c.Alive && Vector2.Distance(new Vector2(c.x, c.y), new Vector2(x, y)) < R + c.R && c.Damage(dmg, Mathf.Atan2(c.y - y, c.x - x), by)) n++;
            return n;
        }

        public void Aoe(float x, float y, float rad, float dmg, Rat by, bool ring = true)
        {
            foreach (var o in new List<Item>(InRange(x, y, rad + 60)))
                if (o.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) < rad + o.R)
                    o.Damage(dmg, by, false, Mathf.Atan2(o.y - y, o.x - x));
            foreach (var h in Humans)
                if (h.State != Human.HState.Fly && h.State != Human.HState.Dead && Vector2.Distance(new Vector2(h.x, h.y), new Vector2(x, y)) < rad + h.R)
                    h.Damage(dmg, by, Mathf.Atan2(h.y - y, h.x - x));
            if (ring) FxManager.I?.Ring(x, y, rad, new Color(1, 1, 1, 0.7f), 0.3f);
        }

        public Item RandomRestInView()
        {
            var vr = ViewRect(40); var l = new List<Item>();
            foreach (var it in items) if (it.State == Item.ItemState.Rest && vr.Contains(new Vector2(it.x, it.y))) l.Add(it);
            return l.Count > 0 ? l[Random.Range(0, l.Count)] : null;
        }

        // 전기 이빨 (공용 스킬): 깨문 물건 근처 물건 n 개에 찌릿 번개
        [Tooltip("전기 이빨 번개가 닿는 거리")] public float zapRange = 220;
        public void ZapChain(Rat by, Item from, float dmg, int n)
        {
            var near = new List<Item>();
            foreach (var o in InRange(from.x, from.y, zapRange)) if (o != from && o.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(o.x, o.y), new Vector2(from.x, from.y)) < zapRange) near.Add(o);
            near.Sort((a, b) => Vector2.Distance(new Vector2(a.x, a.y), new Vector2(from.x, from.y)).CompareTo(Vector2.Distance(new Vector2(b.x, b.y), new Vector2(from.x, from.y))));
            var fx = FxManager.I; var col = new Color(0.75f, 0.91f, 1f);
            for (int i = 0; i < Mathf.Min(n, near.Count); i++)
            {
                var o = near[i];
                // 웹 zapChain: 지그재그 번개 (6토막·꺾임 14, 파랑 빛 + 흰 심지) + 양 끝 불꽃
                fx?.BoltLine(from.x, from.y, 20, o.x, o.y, 20, col, 0.2f, 4, 6, 14, new Color(0.61f, 0.96f, 1f, 0.45f), 1);
                fx?.Spark(from.x, from.y, 20, col, 40, 0.12f); fx?.Spark(o.x, o.y, 16, col, 55, 0.16f); fx?.Anim("zap", o.x, o.y, 0, 0.6f);
                o.Damage(dmg, by, false, Mathf.Atan2(o.y - from.y, o.x - from.x));
            }
            if (near.Count > 0) fx?.Popup(from.x, from.y, "찌릿!", col, 18, 0.6f, 50);
        }

        public Item RandomRestInRange(float x, float y, float r)
        {
            var l = new List<Item>();
            foreach (var it in InRange(x, y, r)) if (it.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(it.x, it.y), new Vector2(x, y)) < r) l.Add(it);
            return l.Count > 0 ? l[Random.Range(0, l.Count)] : null;
        }

        // 직선 위 물건 (가까운 순, 최대 n개)
        public List<Item> OnLine(float x, float y, float ux, float uy, float len, float width, int n)
        {
            var hit = new List<Item>();
            foreach (var it in items)
            {
                if (it.State != Item.ItemState.Rest) continue;
                float px = it.x - x, py = it.y - y, along = px * ux + py * uy;
                if (along > 0 && along < len && Mathf.Abs(px * uy - py * ux) < it.R + width) hit.Add(it);
            }
            hit.Sort((a, b) => Vector2.Distance(new Vector2(a.x, a.y), new Vector2(x, y)).CompareTo(Vector2.Distance(new Vector2(b.x, b.y), new Vector2(x, y))));
            if (hit.Count > n) hit.RemoveRange(n, hit.Count - n);
            return hit;
        }


        // 반경 안 가까운 물건 n개 (연쇄 번개 등)
        public List<Item> Nearby(Item from, float r, int n)
        {
            var l = new List<Item>();
            foreach (var o in InRange(from.x, from.y, r)) if (o != from && o.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(o.x, o.y), new Vector2(from.x, from.y)) < r) l.Add(o);
            l.Sort((a, b) => Vector2.Distance(new Vector2(a.x, a.y), new Vector2(from.x, from.y)).CompareTo(Vector2.Distance(new Vector2(b.x, b.y), new Vector2(from.x, from.y))));
            if (l.Count > n) l.RemoveRange(n, l.Count - n);
            return l;
        }

        // 주변 물건을 가운데로 빨아들임 (소용돌이)
        public void Pull(float x, float y, float R, float amount)
        {
            foreach (var it in InRange(x, y, R + 40))
            {
                if (it.State != Item.ItemState.Rest) continue;
                float dx = x - it.x, dy = y - it.y, d = Mathf.Max(1, Mathf.Sqrt(dx * dx + dy * dy));
                if (d > R || d < 30) continue;
                it.AddPush(dx / d * amount, dy / d * amount);
            }
        }

        // 투척 폭격 (특수 액션 Barrage): 시작 높이·위로 던지는 힘·비행 시간·그림 지정
        public void ThrowBomb(Rat by, float tx, float ty, float rad, float dmg, float startZ, float upV, float T, Sprite spr)
        {
            SpriteRenderer r = null;
            if (bombTemplate) { r = Instantiate(bombTemplate, bombTemplate.transform.parent); r.gameObject.SetActive(true); if (spr) r.sprite = spr; }
            bombs.Add(new Bomb { x = by.x, y = by.y, z = startZ, vx = (tx - by.x) / T, vy = (ty - by.y) / T, vz = upV, rad = rad, dmg = dmg, by = by, r = r });
        }

        // 하늘에서 낙하 (특수 액션 Meteor · 치즈 운석 · 필살기 운석 비). 연출은 ItemManager.Meteor.cs
        public void DropMeteor(Rat by, float tx, float ty, float rad, float dmg, Sprite spr, float size = 1, float fallTime = -1)
        {
            var b = new Bomb { x = tx, y = ty, z = 900, rad = rad, dmg = dmg, by = by, meteor = true };
            SetupMeteor(b, spr, size, fallTime > 0 ? fallTime : meteorFallTime);
            bombs.Add(b);
        }

        // ── 폭탄 투척 (특수 능력 Throw_Bomb) ──
        class Bomb { public float x, y, z, vx, vy, vz, rad, dmg; public Rat by; public SpriteRenderer r; public bool meteor; public MeteorView mv; }
        readonly List<Bomb> bombs = new();
        public void ThrowBomb(Rat by, float tx, float ty, float rad, float dmg)
        {
            const float T = 0.5f;
            SpriteRenderer r = null;
            if (bombTemplate) { r = Instantiate(bombTemplate, bombTemplate.transform.parent); r.gameObject.SetActive(true); if (by.codeId == "scientist" && flaskSprite) r.sprite = flaskSprite; }
            bombs.Add(new Bomb { x = by.x, y = by.y, z = 20, vx = (tx - by.x) / T, vy = (ty - by.y) / T, vz = 380, rad = rad, dmg = dmg, by = by, r = r });
        }
        void UpdateBombs(float dt)
        {
            for (int i = bombs.Count - 1; i >= 0; i--)
            {
                var b = bombs[i];
                b.x += b.vx * dt; b.y += b.vy * dt; if (!b.meteor) b.vz -= 1500 * dt; b.z += b.vz * dt;
                if (b.meteor) TickMeteor(b, dt);
                else if (b.r) { b.r.transform.position = World.ToUnity(b.x, b.y, b.z); b.r.transform.Rotate(0, 0, 400 * dt); b.r.sortingOrder = World.SortOrder(b.y) + 5; }
                if (b.z > 0) continue;
                Aoe(b.x, b.y, b.rad, b.dmg, b.by, false);
                var fx = FxManager.I;
                if (b.meteor) MeteorImpact(b);
                else if (fx) { fx.Ring(b.x, b.y, b.rad, new Color(0.94f, 0.78f, 0.47f), 0.4f); fx.Ring(b.x, b.y, b.rad * 0.6f, Color.white, 0.3f); fx.Burst(b.x, b.y, 12, 16, new Color(0.94f, 0.78f, 0.47f), new Color(0.89f, 0.6f, 0.35f), 200, 520); fx.Dust(b.x, b.y, 8, 1.4f); fx.Anim("explosion", b.x, b.y, 0, b.rad / 70); fx.Shake(0.08f); fx.Hitstop(0.03f); }
                if (b.r) Destroy(b.r.gameObject); if (b.mv != null) b.mv.Destroy();
                bombs.RemoveAt(i);
            }
        }

        // ── 로켓배송 (택배 웨이브): 하늘에서 상자가 역추진 로켓으로 쿵쿵 → 상자가 터지며 물건 ──
        class Parcel { public float x, y, z, vz; public ItemRow row; public SpriteRenderer r; }
        readonly List<Parcel> parcels = new();
        float waveT = 20;
        bool parcelSay;

        Rect ViewRect(float pad)
        {
            var cam = Camera.main;
            Vector2 a = World.FromUnity(cam.ViewportToWorldPoint(new Vector3(0, 1, 0))), b = World.FromUnity(cam.ViewportToWorldPoint(new Vector3(1, 0, 0)));
            return Rect.MinMaxRect(a.x + pad, a.y + pad, b.x - pad, b.y - pad);
        }

        bool DropParcel(float x, float y, float height)
        {
            var k = StageManager.RoomOf(x, y);
            if (!Stage.Open.Contains(k)) return false;
            var row = PickWeighted(GameDatabase.Instance.ZoneOf(Game.Floor));
            if (row == null) return false;
            SpriteRenderer r = null;
            if (parcelTemplate) { r = Instantiate(parcelTemplate, parcelTemplate.transform.parent); r.gameObject.SetActive(true); }
            parcels.Add(new Parcel { x = x, y = y, z = height, vz = -1100, row = row, r = r });
            return true;
        }

        public void DropParcelNear(float x, float y, float rad, float height)
        {
            if (items.Count + parcels.Count >= 2500) return;
            for (int m = 0; m < 4; m++) if (DropParcel(x + Random.Range(-rad, rad), y + Random.Range(-rad, rad) * 0.7f, height)) return;
        }

        void UpdateWaves(float dt)
        {
            waveT -= dt;
            if (waveT <= 0)
            {
                waveT = CommonSkill.DeliveryInterval(waveCool);
                var vr = ViewRect(30);
                int inView = parcels.Count; foreach (var it in items) if (it.State == Item.ItemState.Rest && vr.Contains(new Vector2(it.x, it.y))) inView++;
                int n = Mathf.Min(CommonSkill.DeliveryCount(waveSize), Mathf.Max(0, Mathf.RoundToInt(RoomCap * 1.8f - inView)));
                int got = 0;
                for (int m = 0; m < n; m++)
                    for (int tries = 0; tries < 4; tries++)
                        if (DropParcel(Random.Range(vr.xMin, vr.xMax), Random.Range(vr.yMin, vr.yMax), 900 + m * 40 + Random.Range(0f, 200f))) { got++; break; }
                parcelSay = got > 0;
                foreach (var r in Rats.Rats) r.OnParcelWave();
                if (got > 0) Game.ShowBanner("로켓배송!", $"택배 왔습니다!! ×{got}");
            }
            for (int i = parcels.Count - 1; i >= 0; i--)
            {
                var p = parcels[i];
                if (p.z > 240) p.vz = Mathf.Max(p.vz - 900 * dt, -1300);
                else p.vz += (-240 - p.vz) * Mathf.Min(1, dt * 7);          // 땅 가까이서 역추진 브레이크
                p.z += p.vz * dt;
                if (p.r) { p.r.transform.position = World.ToUnity(p.x, p.y, p.z); p.r.sortingOrder = World.SortOrder(p.y) + 3; }
                if (p.z > 0) continue;
                var it = Spawn(p.row, p.x, p.y, false);
                var fx = FxManager.I;
                if (fx)
                {
                    fx.Burst(p.x, p.y, 20, 8, new Color(0.83f, 0.64f, 0.45f), new Color(0.95f, 0.86f, 0.65f), 120, 320);
                    fx.Dust(p.x, p.y, 4, 0.8f); fx.Ring(p.x, p.y, 40, Color.white, 0.2f); fx.Shake(0.03f);
                    if (parcelSay) { parcelSay = false; fx.Popup(p.x, p.y, "택배 왔습니다!!", new Color(1, 0.95f, 0.75f), 22, 1.2f, 70); }
                    else if (Random.value < 0.12f) fx.Popup(p.x, p.y, Random.value < 0.5f ? "쿵!" : "문 앞 배송 완료", Color.white, 16, 0.7f, 50);
                }
                if (p.r) Destroy(p.r.gameObject);
                parcels.RemoveAt(i);
            }
        }

        // ── 사람 ──
        int HumanCap => Mathf.Min(humanCapMax, humanCapBase + Mathf.FloorToInt(Game.Floor * humanCapPerFloor));

        public void SpawnHumans(Vector2Int room, int n)
        {
            var db = GameDatabase.Instance;
            var pool = new List<HumanRow>();
            foreach (var h in db.Humans) if (Game.Floor >= h.from_floor) pool.Add(h);
            if (pool.Count == 0 || !humanPrefab || !humanArt) return;
            for (int m = 0; m < n && Humans.Count < HumanCap; m++)
            {
                var row = pool[Random.Range(0, pool.Count)];
                var art = humanArt.Get(row.code_id); if (art == null) continue;
                var h = Instantiate(humanPrefab, humanRoot ? humanRoot : transform);
                h.Init(this, row, art, (room.x + 0.5f) * World.RW + Random.Range(-World.RW * 0.35f, World.RW * 0.35f), (room.y + 0.5f) * World.RH + Random.Range(-World.RH * 0.25f, World.RH * 0.3f));
                Humans.Add(h);
            }
        }

        // 층 시작 · 방이 열렸을 때 (StageManager 가 부름)
        public void OnFloorStart() => SpawnHumans(Vector2Int.zero, Game.Floor == 1 ? humanStartFirst : humanStartOther);
        public void OnRoomOpened(Vector2Int room) { if (!Stage.IsStairsRoom(room.x, room.y)) SpawnHumans(room, 1 + (Random.value < 0.5f ? 1 : 0)); }

        void UpdateHumans(float dt)
        {
            humanT -= dt;
            if (humanT <= 0)
            {
                humanT = Random.Range(humanRefill.x, humanRefill.y);
                // 사람 보충: 열린 방 중 하나로 걸어 들어옴 (계단 방 제외)
                var rooms = new List<Vector2Int>();
                foreach (var k in Stage.Open) if (!Stage.IsStairsRoom(k.x, k.y)) rooms.Add(k);
                if (rooms.Count > 0) SpawnHumans(rooms[Random.Range(0, rooms.Count)], 1);
            }
            for (int i = 0; i < Humans.Count; i++) Humans[i].Tick(dt);
            for (int i = Humans.Count - 1; i >= 0; i--) if (Humans[i].State == Human.HState.Dead) { Destroy(Humans[i].gameObject); Humans.RemoveAt(i); }
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            UpdateDecos(dt);
            if (FxManager.WorldFreeze) return;
            for (int i = 0; i < items.Count; i++) items[i].Tick(dt);
            // 공중 물건끼리 부딪힘
            flying.Clear();
            foreach (var it in items) if (it.State == Item.ItemState.Fly) flying.Add(it);
            for (int i = 0; i < flying.Count; i++) for (int j = i + 1; j < flying.Count; j++) flying[i].TryCollide(flying[j]);
            UpdateHumans(dt);
            UpdateBombs(dt);
            UpdateWaves(dt);
            for (int i = items.Count - 1; i >= 0; i--) if (items[i].State == Item.ItemState.Dead) { Destroy(items[i].gameObject); items.RemoveAt(i); }
            spawnT -= dt;
            if (spawnT <= 0 && Stage.Open.Count > 0)
            {
                spawnT = spawnInterval / CommonSkill.SpawnRateMul;          // 실험 재료 반입: 더 자주, 한 번에 여러 개
                var open = new List<Vector2Int>(Stage.Open);
                for (int b = CommonSkill.SpawnBatch; b > 0 && items.Count < RoomCap * Stage.Open.Count; b--)
                    SpawnInRoom(open[Random.Range(0, open.Count)], false);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Fill Item Sprites")]
        public void FillSprites()
        {
            sprites.Clear();
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/ItemTable.json");
            var f = JsonUtility.FromJson<ItemTableFile>(json.text);
            foreach (var r in f.Item)
                sprites.Add(new ItemSprite { codeId = r.code_id, sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Rats/{r.asset}.png") });
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
