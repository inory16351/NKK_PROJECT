using System.Collections.Generic;
using NKK.Data;
using NKK.Items;
using NKK.Rats;
using UnityEngine;

namespace NKK.Ults
{
    // 필살기 한 종의 상황극 (웹게임 ULT_ENG[type]). 종마다 이 클래스를 상속한 UltXxx 를 만들고 쥐 테이블 Ultimate.script 칸에 클래스 이름을 적음.
    //   Pre()  : 컷인 전 (제목 바꾸기 등)
    //   Begin(): 상황극 시작 (Beat 로 자막 시각 등록)
    //   Step(dt, k): 매 프레임 (k = 0~1 진행도). R.x·R.y·R.z·R.face·R.UltPose 등을 직접 움직임
    //   Finish(): 끝까지 했을 때 (마무리 연출. 붙잡은 쥐·물건·소품은 매니저가 풀어 줌)
    //   Cleanup(): 끝나거나 취소될 때 항상 (상태 되돌리기)
    // 화면 글자는 전부 Ult_Caption 시트 (Cap("c1") · CapText("c1")) — 코드에 글자를 쓰지 않음.
    public abstract class UltBase
    {
        public UltimateManager M { get; private set; }
        public Rat R { get; private set; }                 // 필살기 쓰는 쥐
        public RatUltimateRow U { get; private set; }
        public float T;                                    // 상황극 경과 (초)
        public float X0, Y0;                               // 시작 위치
        public readonly List<Rat> Rats = new();            // 붙잡은 쥐 (끝나면 풀림)
        public readonly List<Item> Items = new();          // 붙잡은 물건 (끝나면 놓음)
        public readonly List<UltProp> Props = new();       // 소품 그림 (끝나면 지움)
        public string Title;                               // 컷인 제목 (기본 = 필살기 이름)
        protected float hitT;                              // 웹게임 s.hitT (주기 타이머로 자유롭게)

        public virtual float Dur => 6;
        public virtual void Pre() { }
        public virtual void Begin() { }
        public abstract void Step(float dt, float k);
        public virtual void Finish() { }
        // 끝나거나 중간에 취소될 때 항상 (꺼 둔 그림자·투명 등 되돌리기)
        public virtual void Cleanup() { }

        public void Setup(UltimateManager m, Rat r, RatUltimateRow u)
        {
            M = m; R = r; U = u; X0 = r.x; Y0 = r.y; Title = u.ultimate_name;
        }

        // ── 자막 시각 (웹게임 beats) ──
        readonly List<(float t, System.Action a)> beats = new();
        protected void Beat(float t, System.Action a) => beats.Add((t, a));
        public void RunBeats()
        {
            for (int i = beats.Count - 1; i >= 0; i--) if (T >= beats[i].t) { var a = beats[i].a; beats.RemoveAt(i); a(); }
        }

        // ── 글자 (Ult_Caption 시트) ──
        // 화면 위 자막. n = 글 안 {n} 자리
        protected void Cap(string key, object n = null) => M.ShowCaption(U.ultimate_id, key, n);
        // 자막 글만 (말풍선·제목 등에 씀). 없으면 빈 글
        protected string CapText(string key, object n = null) => M.CaptionText(U.ultimate_id, key, n);
        // 월드 팝업 (글은 자막 시트)
        protected void PopupCap(string key, float x, float y, Color col, float size = 20, float life = 1, float z = 40, object n = null)
        { var s = CapText(key, n); if (!string.IsNullOrEmpty(s)) Fx?.Popup(x, y, s, col, size, life, z); }

        // ── 수치 ──
        protected float UltD => M.UltDamage(R);            // 웹게임 ultD (공격력 × 25)
        protected float ItemD => M.UltItemDamage(R);       // 웹게임 ultItemD (물건 하나 × 8)
        protected float ULT_R => M.ultRadius;
        protected Color Col => U.Color;
        protected FxManager Fx => FxManager.I;
        protected ItemManager ItemMgr => M.Items;
        protected RatManager RatMgr => M.Rats;
        protected static float Rand(float a, float b) => Random.Range(a, b);
        protected static T Pick<T>(IList<T> l) => l.Count > 0 ? l[Random.Range(0, l.Count)] : default;
        protected static float Dist(float x1, float y1, float x2, float y2) => Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        protected static float Ease(float k) => 1 - (1 - k) * (1 - k) * (1 - k);
        protected bool OnScreen(float x, float y, float margin = 120) => M.OnScreen(x, y, margin);
        protected Rect ViewRect(float pad = 0) => M.ViewRect(pad);

        // ── 쥐 ──
        // 다른 쥐를 붙잡음 (필살기가 움직임)
        protected bool GrabRat(Rat o)
        {
            if (!o || (o.UltOn && o != R)) return false;
            o.UltGrab();
            if (!Rats.Contains(o)) Rats.Add(o);
            return true;
        }
        protected void ReleaseRat(Rat o) { if (!o || o == R) return; o.UltRelease(); Rats.Remove(o); }
        // 날아온 쥐는 데굴데굴 (필살기가 붙잡은 쥐는 제외)
        protected void Ragdoll(Rat o, float ang, float spd = 380, float vz = 320) { if (!o || o.UltOn) return; o.Ragdoll(ang, spd, vz, 0.9f); }
        protected List<Rat> RatsNear(float x, float y, float R0)
        {
            var l = new List<Rat>();
            foreach (var o in RatMgr.Rats) if (o != R && !o.UltOn && Dist(o.x, o.y, x, y) < R0) l.Add(o);
            return l;
        }
        // 필살기 쥐 걷기 (웹게임 ultWalk): 가까운 물건 쪽으로
        Vector2? walkT; float walkTT;
        protected void Walk(float dt, float spd = 150)
        {
            if (walkT == null || (walkTT -= dt) <= 0 || Dist(walkT.Value.x, walkT.Value.y, R.x, R.y) < 50)
            {
                var it = ItemMgr.Nearest(R.x, R.y, 520);
                walkT = it ? new Vector2(it.x, it.y) : new Vector2(X0 + Rand(-220, 220), Y0 + Rand(-160, 160)); walkTT = 1.4f;
            }
            float dx = walkT.Value.x - R.x, dy = walkT.Value.y - R.y, d = Mathf.Max(1, Mathf.Sqrt(dx * dx + dy * dy));
            MoveRat(R, dx / d * spd * dt, dy / d * spd * dt);
            R.face = dx >= 0 ? 1 : -1;
        }
        // 쥐를 옮김 (열린 방 밖으로 안 나감)
        protected void MoveRat(Rat o, float dx, float dy)
        {
            float px = o.x, py = o.y, vx = 0, vy = 0; o.x += dx; o.y += dy;
            M.Stage.Confine(ref o.x, ref o.y, ref vx, ref vy, o.Radius, px, py, 0);
        }
        // 벽에 튕기며 움직이기 (웹게임 bounceMove)
        protected void BounceMove(ref float x, ref float y, ref float vx, ref float vy, float rad, float dt)
        {
            float px = x, py = y; x += vx * dt; y += vy * dt;
            M.Stage.Confine(ref x, ref y, ref vx, ref vy, rad, px, py, 1);
        }

        // ── 물건 ──
        // 반경 안 (바닥에 있는) 물건
        protected List<Item> ItemsIn(float x, float y, float R0)
        {
            var l = new List<Item>();
            foreach (var it in ItemMgr.InRange(x, y, R0 + 80)) if (it.State == Item.ItemState.Rest && Dist(it.x, it.y, x, y) < R0 + it.R) l.Add(it);
            return l;
        }
        // 붙잡음: 필살기가 x·y·z 를 직접 움직임
        protected bool GrabItem(Item it)
        {
            if (!it || it.State != Item.ItemState.Rest || !it.Appeared) return false;
            it.Hold(); if (!Items.Contains(it)) Items.Add(it);
            return true;
        }
        // 붙잡은(또는 바닥) 물건을 날려 보냄. 피해(기본 ItemD)로 체력 0 이면 떨어질 때 박살, 남으면 멀쩡히 착지
        protected void DropItem(Item it, float vx, float vy, float vz, float dmg = -1)
        {
            if (!it || (it.State != Item.ItemState.Held && it.State != Item.ItemState.Rest)) return;
            it.SkillHit(dmg < 0 ? ItemD : dmg, R);
            it.Fling(vx, vy, vz);
            Items.Remove(it);
        }
        protected void FlingItem(Item it, float ang, float spd, float vz)
        {
            DropItem(it, Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd, vz);
            if (it && OnScreen(it.x, it.y)) Fx?.Stars(it.x, it.y, 16, 4, Color.white, Col, 100, 260);
        }
        // 굴러다니는 큰 물체가 깔아뭉개기 (웹게임 crush)
        protected void Crush(float x, float y, float R0, float vx, float vy)
        {
            foreach (var it in ItemsIn(x, y, R0)) FlingItem(it, Mathf.Atan2(it.y - y, it.x - x) + Rand(-0.3f, 0.3f), 520 + Mathf.Sqrt(vx * vx + vy * vy) * 0.4f, Rand(350, 520));
            foreach (var o in RatsNear(x, y, R0 + 10)) Ragdoll(o, Mathf.Atan2(o.y - y, o.x - x), 420, 380);
            BlastActors(x, y, R0 + 20, 560, UltD * 0.5f);
        }
        // 충격파 (웹게임 shock): 반경 안 물건 피해 + 고리·먼지
        protected void Shock(float x, float y, float rad, float dmg, Color? col = null, float power = 1)
        {
            ItemMgr.Aoe(x, y, rad, dmg, R, false);
            if (!OnScreen(x, y)) return;
            Fx?.Ring(x, y, rad, col ?? Color.white, 0.35f); Fx?.Ring(x, y, rad * 1.5f, new Color(0.95f, 0.86f, 0.75f, 0.7f), 0.45f);
            Fx?.Dust(x, y, Mathf.RoundToInt(4 + power * 4), 0.8f + power * 0.5f);
            Fx?.Stars(x, y, 8, Mathf.RoundToInt(4 + power * 4), Color.white, new Color(0.95f, 0.86f, 0.75f), 150, 380);
            Fx?.Shake(0.03f * power); Fx?.Hitstop(0.015f * power);
        }
        // 물건이 가장 많은 방향 (웹게임 aimMost)
        protected float AimMost(float R0 = 700)
        {
            float bx = 0, by = 0; foreach (var it in ItemsIn(R.x, R.y, R0)) { bx += it.x - R.x; by += it.y - R.y; }
            return bx != 0 || by != 0 ? Mathf.Atan2(by, bx) : (R.face > 0 ? 0 : Mathf.PI);
        }
        // 사람·고양이 휘말림 (웹게임 blastActorsIn)
        protected int BlastActors(float x, float y, float R0, float spd, float dmg) => ItemMgr.BlastActors(x, y, R0, spd, dmg, R);
        protected void Smoke(float x, float y) => Fx?.Dust(x, y, 8, 1);
        protected void Flash(Color c, float a) => M.Flash(c, a);

        // ── 소품 그림 (UltimateManager.props 이름) ──
        protected UltProp Prop(string name, float x, float y, float z, float width)
        {
            var p = M.MakeProp(name); if (p == null) return null;
            p.x = x; p.y = y; p.z = z; p.w = width;
            Props.Add(p);
            return p;
        }
        protected void KillProp(UltProp p) { if (p == null) return; p.Destroy(); Props.Remove(p); }

        // ── 자세 (웹게임 POSE_UP · POSE_FLAIL) ──
        protected static RatRig.Pose PoseUp(float e = 1) => new RatRig.Pose { tilt = -0.5f * e, front = 2.6f * e, farFront = 2.3f * e, back = -0.3f * e, farBack = 0.3f * e, head = -0.4f * e, tail = 1.3f * e, sx = 1, sy = 1 };
        protected static RatRig.Pose PoseFlail()
        {
            float t = Time.time;
            return new RatRig.Pose { front = Mathf.Sin(t * 30) * 1.6f, farFront = Mathf.Cos(t * 27) * 1.6f, back = Mathf.Sin(t * 29 + 2) * 1.5f, farBack = Mathf.Cos(t * 25) * 1.5f, head = Mathf.Sin(t * 18) * 0.5f, tail = Mathf.Sin(t * 22) * 1.3f, sx = 1, sy = 1 };
        }
        protected static RatRig.Pose P(float head = 0, float tail = 0, float front = 0, float back = 0, float farFront = 0, float farBack = 0, float tilt = 0, float bob = 0, float sx = 1, float sy = 1, float headX = 0)
            => new RatRig.Pose { head = head, tail = tail, front = front, back = back, farFront = farFront, farBack = farBack, tilt = tilt, bob = bob, sx = sx, sy = sy, headX = headX };

        // 끝: 붙잡은 것 전부 풀기 (웹게임 ultRelease, 놓아줄 땐 피해 없음)
        public void ReleaseAll()
        {
            foreach (var it in new List<Item>(Items)) DropItem(it, Rand(-120, 120), Rand(-120, 120), 60, 0);
            foreach (var o in new List<Rat>(Rats)) if (o && o != R) o.UltRelease();
            Rats.Clear();
            foreach (var p in Props) p.Destroy();
            Props.Clear();
        }
    }

    // 필살기 소품 그림 하나 (게임 좌표). 매니저가 매 프레임 위치를 맞춤
    public class UltProp
    {
        public SpriteRenderer r;
        public float x, y, z, w = 100, rot, alpha = 1, flat = 1;     // rot = 라디안 (반시계), flat = 세로 배율
        public bool flip, ground, visible = true;                    // ground = 바닥에 눕힘 (그림자·자국)
        public int sortBias;                                         // 정렬 보정 (+ = 앞)
        public Color tint = Color.white;
        public Vector2 pivotY;                                       // 미사용 예비
        public void Destroy() { if (r) Object.Destroy(r.gameObject); r = null; }
        public void Apply()
        {
            if (!r) return;
            r.enabled = visible && alpha > 0.001f;
            r.transform.position = World.ToUnity(x, y, z);
            float k = r.sprite ? w * World.U / r.sprite.bounds.size.x : 1;
            r.transform.localScale = new Vector3(k * (flip ? -1 : 1), k * flat * (ground ? World.TILT : 1), 1);
            r.transform.localRotation = Quaternion.Euler(0, 0, rot * Mathf.Rad2Deg);
            r.sortingOrder = ground ? -29000 + sortBias : World.SortOrder(y) + sortBias;
            var c = tint; c.a *= alpha; r.color = c;
        }
    }
}
