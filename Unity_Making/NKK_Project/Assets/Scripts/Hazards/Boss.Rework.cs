using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using NKK.Stage;
using UnityEngine;

namespace NKK.Hazards
{
    // 보스 개편 (2026-10-22, 명세 = UnityResources/Rats/BossRework/boss_spec_20261022.md): 밈 패턴(Boss.Meme.cs)에 더한 새 패턴 + opt 키워드.
    // 테이블 Atk_Type: pattern · sprite(쉼표로 여러 장 = 차례로) · p1 · p2 · p3 · opt. 그림이 아직 없으면 placeholderSprite 로 보임 (동작 먼저, 그림은 나중 — 사용자)
    //  Fan      count 개를 쏨 (dur > 0 이면 dur 동안 나눠서). p1 = 퍼지는 각도(°, 0 = 일직선 · 360 이상 = 사방 아무 데나) · p2 = 속도 · p3 = 밀어내는 세기
    //  Parade   sprite 를 count 마리 화면 왼쪽 밖 → 오른쪽 밖으로 dur 초에 지나가게, 닿은 쥐 기절. p1 = 1 이면 다 지나간 뒤 화면 안 쥐 전부 기절
    //  Orbit    sprite count 마리가 windup 동안 나타나 dur 초 보스 둘레를 돎 (반경 p1), 닿은 쥐 기절. 보스는 p2 속도로 천천히 걸음
    //  Path     dur 초 동안 경로를 따라 돌진, 닿은 쥐 기절. p1 = 0 별(오각형 꼭짓점 한 칸 건너뛰기) + 오각형 · 1 바깥 → 안쪽 나선. p2 = 경로 반경
    //  Rise     Drop 과 같지만 바닥 포탈에서 솟아오름 (p1 = 춤 종류 1~4 · p2 = 춤 시간)
    //  Slam     쥐 무리로 뛰어가 착지 → p1 번(기본 4) 연타 (반경 기절 + 화면 흔들림)
    //  Smash    팔을 들었다가 바로 앞을 내려침 (반경 기절)
    //  Swallow  머리가 열리고 입에서 촉수(sprite) count 갈래 → 반경 안 쥐를 잡아 dur 초에 입으로 → p1 초 삼켰다가 뱉음 (쥐 목록에 남아 인구 그대로, Boss.Crew.cs)
    //  Meeting  빨간 버튼(sprite) → 보스가 폴짝 눌러 긴급 회의 화면 → 쥐 전부 stun 초 기절 (Boss.Crew.cs)
    //  Grow     dur 초 동안 거대화해서 걸어다님, 0.6초마다 둘레 기절 + 화면 진동
    // opt (쉼표로 여러 개, '키:값'):
    //  head:그림 (고양이 머리, 이 공격 동안) · head_keep:그림 (그 뒤로 계속) · body:그림@크기 (보스를 그림 한 장으로, 공격 + 1.5초)
    //  hide:그림 (보스는 숨고 그림만 보스를 따라감, 피해 안 받음) · ride:code (전용 파츠 리그로 갈아탐, 계속)
    //  explode (Fan: 목표 거리에서 터짐) · near (Fan: 쏠 때 보스 둘레도 기절) · all (Global: 맵 전체) · red (준비 동안 화면 붉게 깜빡) · shake (준비 동안 점점 크게 흔들림)
    //  around (Throw: 쥐 대신 보스 둘레 사방으로) · sticky:초 (Drop: 떨어진 자리에 닿으면 기절하는 반죽) · morph:그림@초 (Drop: 떨어진 자리 크레이터, 닿으면 그 머리로 멋대로 걷기)
    //  mask:그림 (Zone: 기절한 쥐에 방독면, p3 초 면역) · intro (발악 1 을 전투 시작 때도 씀)
    //  alarm (Drop: 떨어진 것이 남아 있는 동안 화면 붉게 깜빡, 사이렌) · face:그림 (Meeting: 회의 화면 보스 얼굴) · all (Meeting: 맵 전체)
    //  rig:코드|코드@길이배율 (고양이 그림 리그로 소환, 여러 개면 돌아가며 · 2026-10-23) — Parade: 고양이가 sprite(팻말)를 앞발로 들고 행진 · Throw: sprite 대신 고양이가 포물선으로 날아가 착지 → 잠깐 서서 외침 → p2 초 쥐 쪽으로 돌아다니며 닿은 쥐 기절 → 펑
    //  dust (준비 동안 보스 발밑 먼지 구름이 점점 크게, 던질 때마다 먼지 펑)
    public partial class Boss
    {
        [Header("보스 개편 패턴 (Boss.Rework.cs)")]
        [Tooltip("그림이 아직 없는 기술의 임시 그림")] public Sprite placeholderSprite;
        [Tooltip("Fan 기본 속도 · 날아가는 최대 거리 · 기본 밀어내기")] public float projSpeed = 620, projRange = 1200, projKnock = 260;
        [Tooltip("Orbit 기본 반경 · 도는 빠르기(초당 라디안) · 보스 걷는 속도")] public float orbitRadius = 240, orbitSpin = 2.4f, orbitWalk = 70;
        [Tooltip("Path 기본 반경 · Parade 크기 배율")] public float pathRadius = 300;

        class Proj { public float x, y, vx, vy, life, rad, stun, size, knock, dist, maxDist, rot; public bool explode, roll; public SpriteRenderer r; public readonly HashSet<Rat> hit = new(); }
        class Walker { public float x, y, vx, rad, stun, size, t, len; public SpriteRenderer r; public Cat c; public readonly HashSet<Rat> hit = new(); }
        class Leaper { public float x, y, z, vx, vy, vz, t, stay, rad, stun, len, roam, ang, turnT, walk; public bool landed; public Cat c; public string name, pop; public Color color; public readonly HashSet<Rat> hit = new(); }
        readonly List<Leaper> leapers = new();
        class Orbiter { public float ang, size, rad, stun; public SpriteRenderer r; }
        readonly List<Proj> projs = new();
        readonly List<Walker> walkers = new();
        readonly List<Orbiter> orbiters = new();
        readonly List<Rat> swallowed = new();
        readonly List<Vector2> pathPts = new();
        float orbitT, orbitR, bodyT, bodySize;
        Sprite formHead, atkHead, bodySprite, hideSprite;
        SpriteRenderer formFx;
        bool riding, walkersAll; float walkersStun;
        BossAtkRow growAtk;

        // ── opt ──
        string Opt(string key, BossAtkRow a = null)
        {
            a ??= atk; if (a == null || string.IsNullOrEmpty(a.opt) || a.opt == "-") return null;
            foreach (var part in a.opt.Split(','))
            {
                var p = part.Trim(); int c = p.IndexOf(':');
                string k = c >= 0 ? p.Substring(0, c) : p;
                if (k == key) return c >= 0 ? p.Substring(c + 1) : "";
            }
            return null;
        }
        // '그림@값' → (그림, 값)
        static (string, float) SplitAt(string v, float def)
        {
            if (string.IsNullOrEmpty(v)) return (v, def);
            int i = v.IndexOf('@');
            return i < 0 ? (v, def) : (v.Substring(0, i), float.TryParse(v.Substring(i + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : def);
        }
        // sprite 칸에 쉼표로 여러 장 → i 번째 (돌아가며). 없으면 임시 그림
        Sprite MemeSpriteAt(BossAtkRow a, int i)
        {
            if (a == null || string.IsNullOrEmpty(a.sprite) || a.sprite == "-") return null;
            var l = a.sprite.Split(',');
            return SpriteOrTemp(l[Mathf.Abs(i) % l.Length].Trim());
        }
        Sprite SpriteOrTemp(string n) { if (string.IsNullOrEmpty(n) || n == "-") return null; var s = MemeSprite(n); return s ? s : placeholderSprite; }

        bool Riding => riding;

        void ResetRework()
        {
            ClearReworkFx();
            riding = false; formHead = null; atkHead = null; bodySprite = null; hideSprite = null; bodyT = 0; growAtk = null;
        }

        // 공격 시작 (BeginMeme 끝에서): opt 처리 + 새 패턴 준비
        void BeginRework()
        {
            atkHead = null; hideSprite = null;
            var h = Opt("head"); if (h != null) atkHead = SpriteOrTemp(h);
            var hk = Opt("head_keep"); if (hk != null) formHead = SpriteOrTemp(hk);
            var b = Opt("body"); if (b != null) { var (bn, sz) = SplitAt(b, Height * 1.1f); bodySprite = SpriteOrTemp(bn); bodySize = sz; bodyT = atk.windup + atk.dur + 1.5f; }
            var hd = Opt("hide"); if (hd != null) { hideSprite = SpriteOrTemp(hd); Hidden = true; }
            var rd = Opt("ride"); if (rd != null && !riding) Ride(rd);
            if (Opt("alarm") != null) { alarmT = atk.windup + atk.dur + Mathf.Max(0, atk.p2) + 0.3f; alarmTick = atk.windup; }
            int n = Mathf.Max(1, atk.count);
            switch (atk.pattern)
            {
                case "Meeting": BeginMeeting(); break;
                case "Rise": PickTargets(n); break;
                case "Slam": PickTargets(1); break;
                case "Path": BuildPath(); break;
                case "Orbit": orbitT = 0; orbitR = atk.p1 > 0 ? atk.p1 : orbitRadius; break;
            }
        }

        // 전용 파츠 리그로 갈아탐 (굴착기 기사 → 굴착기, 발악 1 부터 끝까지)
        void Ride(string code)
        {
            var art = partArt ? partArt.Get(code) : null;
            if (art == null) { Debug.LogWarning("[Boss] 탈것 그림 없음: " + code); return; }
            EnsurePartRig(); riding = true;
            partRig.Build(art, 1);
            SetVisible(true);
            baseColors.Clear(); foreach (var r in partRig.GetComponentsInChildren<SpriteRenderer>(true)) baseColors.Add((r, r.color));
            FxManager.I?.Anim("poof", x, y, 0, 3); FxManager.I?.Shake(0.4f);
        }

        // 고양이 머리 바꾸기 (LateUpdate): 이 공격 머리 > 계속 바뀐 머리 > 원래
        Sprite ReworkHead() => attacking && atkHead ? atkHead : formHead;

        // ── 매 프레임 (공격 중) ── 처리했으면 true
        bool UpdateRework(float dt)
        {
            var fx = FxManager.I;
            float wind = Mathf.Max(0.1f, atk.windup), k = Mathf.Clamp01(atkT / wind);
            int n = Mathf.Max(1, atk.count);
            switch (atk.pattern)
            {
                case "Fan":
                {
                    if (atkT < wind) { jit = 2.5f; if (Opt("explode") != null || atk.count == 1) ShowAim(k); break; }
                    float gap = atk.dur > 0 ? atk.dur / n : 0;
                    while (shots < n && atkT >= wind + shots * gap)
                    {
                        if (shots == 0 && Opt("near") != null && Cats) { int c = Cats.StunArea(x, y, R * 1.5f, atk.stun, 260, 260); Cats.HitFx(x, y, R * 1.5f, SpName, c, 0.25f); }
                        FireProj(shots, n); shots++;
                    }
                    if (shots >= n && atkT > wind + atk.dur + 0.2f) { Cats?.HideWarns(); HideProp(); attacking = false; }
                    break;
                }
                case "Parade":
                    jit = atkT < wind ? 2 : 0;
                    if (atkT >= wind && !atkHit) { atkHit = true; SpawnParade(n); }
                    if (atkHit && atkT > wind + atk.dur + 0.3f) { HideProp(); attacking = false; }
                    break;
                case "Orbit":
                {
                    if (orbiters.Count == 0) for (int i = 0; i < n; i++) AddOrbiter(i, n);
                    orbitT += dt;
                    var r = Rats.NearestRat(x, y, 2000);
                    float spd = atk.p2 > 0 ? atk.p2 : orbitWalk;
                    if (r && atkT >= wind) { float a = Mathf.Atan2(r.y - y, r.x - x); vx = Mathf.Cos(a) * spd; vy = Mathf.Sin(a) * spd; }
                    if (atkT > wind + atk.dur) { ClearOrbiters(true); attacking = false; }
                    break;
                }
                case "Path":
                {
                    if (atkT < wind) { jit = 3; ShowPathWarn(k); break; }
                    int legs = pathPts.Count - 1;
                    if (legs <= 0) { attacking = false; break; }
                    float per = Mathf.Max(0.05f, atk.dur / legs), tt = atkT - wind;
                    int i = Mathf.Min(legs - 1, Mathf.FloorToInt(tt / per));
                    float kk = Mathf.Clamp01((tt - i * per) / per);
                    var a0 = pathPts[i]; var a1 = pathPts[i + 1];
                    float nx = Mathf.Lerp(a0.x, a1.x, kk), ny = Mathf.Lerp(a0.y, a1.y, kk);
                    face = a1.x >= a0.x ? 1 : -1; vx = vy = 0;
                    if (fx && Random.value < dt * 20) { fx.Dust(nx, ny, 2, 1); fx.Beam(x, y, 30, nx, ny, 30, MemeColor, 0.5f, 0.8f); }
                    x = nx; y = ny;
                    foreach (var o in Rats.Rats) if (!o.UltOn && o.stun <= 0 && (o.x - x) * (o.x - x) + (o.y - y) * (o.y - y) < atk.radius * atk.radius) o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x), 420, 340, atk.stun);
                    if (tt >= atk.dur) { Cats?.HideWarns(); attacking = false; }
                    break;
                }
                case "Rise":
                {
                    jit = atkT < wind ? 2 : 0;
                    ShowWarns(released, k);
                    if (atkT < wind) break;
                    float gap = Mathf.Max(0.05f, atk.dur / Mathf.Max(1, seqTargets.Count));
                    while (released < seqTargets.Count && atkT >= wind + released * gap)
                    {
                        var v = seqTargets[released];
                        var sp = MemeSpriteAt(atk, released++);
                        int after = Mathf.Clamp(Mathf.RoundToInt(atk.p1), 1, 4);
                        if (sp) { AddDrop(v, sp, MemeSize, after - 1, Mathf.Max(1, atk.p2 > 0 ? atk.p2 : maxwellDance)); maxwells[maxwells.Count - 1].rise = true; }
                        else Cats?.AddStrike(true, v.x, v.y, atk.radius, atk.stun, SpName);
                        fx?.Anim("poof", v.x, v.y, 0, atk.radius / 80);
                    }
                    if (released >= seqTargets.Count) { Cats?.HideWarns(); attacking = false; }
                    break;
                }
                case "Slam":
                {
                    // 0: 웅크림 → 1: 점프 → 2: 연타
                    var tg = seqTargets.Count > 0 ? seqTargets[0] : new Vector2(x, y);
                    if (released == 0)
                    {
                        jit = 2; ShowWarns(0, k);
                        if (atkT >= wind) { released = 1; phaseT = 0; lx0 = x; ly0 = y; ltx = tg.x; lty = tg.y; vz = pounceV; air = true; }
                        break;
                    }
                    phaseT += dt;
                    if (released == 1)
                    {
                        float T = 2 * pounceV / gravity, kk = Mathf.Clamp01(phaseT / T);
                        x = Mathf.Lerp(lx0, ltx, kk); y = Mathf.Lerp(ly0, lty, kk); vx = vy = 0;
                        if (kk >= 1 || (z <= 0 && phaseT > 0.1f)) { z = 0; vz = 0; air = false; released = 2; phaseT = 0; seqI = 0; Cats?.HideWarns(); }
                        break;
                    }
                    int hits = atk.p1 > 0 ? Mathf.RoundToInt(atk.p1) : 4;
                    if (seqI < hits && phaseT >= seqI * 0.28f)
                    {
                        float hx = x + face * R * 0.6f;
                        MemeHit(hx, y, atk.radius, atk.stun); fx?.Shake(0.25f); sq = 0.7f; face = -face; seqI++;
                    }
                    if (seqI >= hits && phaseT > hits * 0.28f + 0.3f) attacking = false;
                    break;
                }
                case "Smash":
                    if (atkT < wind) { jit = 1.5f; sq = 1.15f; Cats?.ShowWarn(0, x + face * R * 1.2f, y, atk.radius, k); break; }
                    if (!atkHit) { atkHit = true; Cats?.HideWarns(); MemeHit(x + face * R * 1.2f, y, atk.radius, atk.stun); fx?.Shake(0.5f); sq = 0.6f; if (Ults) Ults.Flash(Color.white, 0.15f); }
                    if (atkT > wind + Mathf.Max(0.3f, atk.dur)) attacking = false;
                    break;
                case "Swallow": UpdateSwallow(dt, wind, k); break;          // 촉수 삼키기 (Boss.Crew.cs)
                case "Meeting": UpdateMeeting(dt, wind); break;             // 긴급 회의 (Boss.Crew.cs)
                case "Grow":
                    if (atkT < wind) { jit = 3; break; }
                    growT = atk.dur; growAtk = atk; pulseEvery = 0.6f; pulseT = 0;
                    if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 40, 1.2f, Height * 1.1f);
                    fx?.Shake(0.4f); attacking = false;
                    break;
                default: return false;
            }
            return true;
        }

        // 준비 동안 opt 연출 (모든 밈·개편 패턴 공통, UpdateMeme 처음에 부름): red · shake · dust
        void ReworkWindup(float dt)
        {
            float wind = Mathf.Max(0.1f, atk.windup);
            if (atkT >= wind) return;
            float k = Mathf.Clamp01(atkT / wind);
            var fx = FxManager.I;
            if (Opt("red") != null && Ults && Mathf.Repeat(atkT, 0.3f) < dt) Ults.Flash(new Color(0.95f, 0.2f, 0.2f), 0.15f);
            if (Opt("shake") != null && Random.value < dt * 12) fx?.Shake(0.05f + 0.25f * k);
            if (Opt("dust") != null && fx && Random.value < dt * (8 + 30 * k))
                fx.Dust(x + Random.Range(-1f, 1f) * R * (0.6f + 0.6f * k), y + Random.Range(-0.4f, 0.4f) * R, 2, 0.8f + 1.4f * k);
        }

        // ── 소환 고양이 꼭두각시 (opt rig): 고양이 프리팹의 리그만 빌려 씀 (게임 고양이 아님, Cat 은 꺼 둠) ──
        Cat MakePuppet(string codes, int i, float len)
        {
            if (!Cats || !Cats.catPrefab || !Cats.catArt || string.IsNullOrEmpty(codes)) return null;
            var l = codes.Split('|');
            var art = Cats.catArt.Get(l[Mathf.Abs(i) % l.Length].Trim());
            if (art == null) { Debug.LogWarning("[Boss] 소환 고양이 그림 없음: " + codes); return null; }
            var c = Instantiate(Cats.catPrefab, Cats.catRoot ? Cats.catRoot : Cats.transform);
            c.enabled = false; c.name = "BossPuppet";
            c.rig.Build(art, len, 1, false);
            if (c.shadow && Rats.shadowRoot) { c.shadow.transform.SetParent(Rats.shadowRoot, true); c.shadow.sortingOrder = Rats.shadowSortOrder; }
            return c;
        }
        void PlacePuppet(Cat c, float px, float py, float pz, in RatRig.Pose p, int f, int order, float len, float rot = 0)
        {
            c.transform.position = World.ToUnity(px, py, pz);
            c.transform.rotation = Quaternion.identity;
            c.rig.Apply(p, 1, f, 1, order, 0, rot);
            if (!c.shadow) return;
            float w = len * 0.5f * (1 - Mathf.Min(0.7f, pz / 500)), sw = c.shadow.sprite ? c.shadow.sprite.bounds.size.x : 1;
            c.shadow.transform.position = World.ToUnity(px, py);
            c.shadow.transform.localScale = new Vector3(w * 2 * World.U / sw, w * 0.6f * 2 * World.TILT * World.U / sw, 1);
        }
        static void KillPuppet(Cat c) { if (c) Destroy(c.gameObject); }      // 그림자는 Cat.OnDestroy 가 지움

        // ── Throw + rig: 고양이가 보스 발밑 먼지 속에서 튀어나와 포물선 → 착지 기절 → 잠깐 서서 외치고 펑 ──
        void Leap(float tx, float ty)
        {
            var (codes, lk) = SplitAt(Opt("rig"), 0.8f);
            float len = (Cats ? Cats.catLength : 115) * lk;
            var c = MakePuppet(codes, shots, len);
            if (!c) { Throw(tx, ty, atkType); return; }
            float T = Mathf.Max(0.35f, shotTime * 1.25f);
            leapers.Add(new Leaper { x = x, y = y, z = 10, vx = (tx - x) / T, vy = (ty - y) / T, vz = 0.5f * shotGravity * T, rad = atk.radius, stun = atk.stun, len = len, c = c, name = SpName, pop = MemePop, color = MemeColor,
                roam = Mathf.Max(0, atk.p2) });
            var fx = FxManager.I;
            if (fx) { fx.Dust(x, y, 6, 1.8f); fx.Anim("poof", x + Random.Range(-30f, 30f), y, 0, 1.3f); }
        }

        void UpdateLeapers(float dt)
        {
            var fx = FxManager.I;
            for (int i = leapers.Count - 1; i >= 0; i--)
            {
                var l = leapers[i]; l.t += dt;
                if (!l.c) { leapers.RemoveAt(i); continue; }
                int f = l.vx >= 0 ? 1 : -1;
                var p = new RatRig.Pose { sx = 1, sy = 1 };
                if (!l.landed)
                {
                    l.x += l.vx * dt; l.y += l.vy * dt; l.vz -= shotGravity * dt; l.z += l.vz * dt;
                    // 날아가는 중: 네 다리 쫙 (만세) · 닿은 쥐 기절
                    p.front = 1.4f; p.farFront = 1.2f; p.back = -1.2f; p.farBack = -1; p.tail = 1.2f + Mathf.Sin(l.t * 30) * 0.3f; p.head = -0.2f; p.tilt = Mathf.Clamp(-l.vz / 900, -0.5f, 0.5f);
                    if (l.z < 90)
                        foreach (var o in Rats.Rats)
                        {
                            if (o.UltOn || l.hit.Contains(o)) continue;
                            float dx = o.x - l.x, dy = o.y - l.y;
                            if (dx * dx + dy * dy < l.rad * l.rad * 0.36f) { l.hit.Add(o); o.Ragdoll(Mathf.Atan2(dy, dx), 260, 300, l.stun); }
                        }
                    if (l.z <= 0)
                    {
                        l.z = 0; l.landed = true; l.stay = 0.9f;
                        int c = Cats ? Cats.StunArea(l.x, l.y, l.rad, l.stun, 60, 260) : 0;
                        Cats?.HitFx(l.x, l.y, l.rad, l.name, c, 0.3f);
                        if (fx && (!Ults || Ults.OnScreen(l.x, l.y, 0)))
                        {
                            fx.Ring(l.x, l.y, l.rad, l.color, 0.45f); fx.Dust(l.x, l.y, 8, 1.4f); fx.Shake(0.12f);
                            if (l.pop != null) fx.Popup(l.x, l.y, l.pop, l.color, 22, 0.8f, l.len * 0.9f);
                        }
                    }
                }
                else if (l.stay > 0)
                {
                    // 착지: 앞발 들고 서서 가리키며 "좋았쓰!!" (p2 초 돌아다닐 거면 짧게)
                    float k = Mathf.Clamp01((0.9f - l.stay) / 0.15f);
                    p.tilt = -0.7f * k; p.back = 1.1f * k; p.farBack = 1.1f * k; p.front = 2.5f * k + Mathf.Sin(l.t * 24) * 0.15f; p.farFront = 0.6f * k; p.head = -0.25f * k; p.tail = 0.6f + Mathf.Sin(l.t * 8) * 0.3f;
                    if ((l.stay -= dt) <= 0 && l.roam <= 0) { fx?.Anim("poof", l.x, l.y, 30, 1.1f); fx?.Dust(l.x, l.y, 4, 1); KillPuppet(l.c); leapers.RemoveAt(i); continue; }
                }
                else
                {
                    // 돌아다니기 (p2 초): 가까운 쥐 쪽으로 어슬렁 (가끔 방향 바꿈), 닿은 쥐 기절 → 펑
                    l.roam -= dt; l.turnT -= dt; l.walk += dt * 14;
                    if (l.turnT <= 0)
                    {
                        l.turnT = Random.Range(0.4f, 0.8f);
                        var r = Rats.NearestRat(l.x, l.y, 600);
                        l.ang = r && Random.value < 0.75f ? Mathf.Atan2(r.y - l.y, r.x - l.x) : Random.Range(0, Mathf.PI * 2);
                    }
                    float spd = memeWalk * 1.4f, nx = l.x + Mathf.Cos(l.ang) * spd * dt, ny = l.y + Mathf.Sin(l.ang) * spd * dt;
                    if (Stage.Open.Contains(StageManager.RoomOf(nx, ny))) { l.x = nx; l.y = ny; } else l.turnT = 0;      // 벽이면 다음 프레임에 방향 바꿈
                    l.vx = Mathf.Cos(l.ang); f = l.vx >= 0 ? 1 : -1;
                    float s1 = Mathf.Sin(l.walk);
                    p.front = s1 * 0.5f; p.farBack = s1 * 0.45f; p.farFront = -s1 * 0.5f; p.back = -s1 * 0.45f; p.bob = -Mathf.Abs(Mathf.Cos(l.walk)) * 2.5f; p.tail = 0.4f + Mathf.Sin(l.walk * 0.5f) * 0.3f;
                    float tr = l.rad * 0.55f;
                    foreach (var o in Rats.Rats)
                    {
                        if (o.UltOn || o.stun > 0) continue;
                        float dx = o.x - l.x, dy = o.y - l.y;
                        if (dx * dx + dy * dy < tr * tr) { o.Ragdoll(Mathf.Atan2(dy, dx), 300, 300, l.stun); if (fx && Random.value < 0.5f && l.pop != null) fx.Popup(l.x, l.y, l.pop, l.color, 18, 0.6f, l.len * 0.8f); }
                    }
                    if (fx && Random.value < dt * 6) fx.Dust(l.x, l.y, 1, 0.7f);
                    if (l.roam <= 0) { fx?.Anim("poof", l.x, l.y, 30, 1.1f); fx?.Dust(l.x, l.y, 4, 1); KillPuppet(l.c); leapers.RemoveAt(i); continue; }
                }
                PlacePuppet(l.c, l.x, l.y, Mathf.Max(0, l.z), p, f, World.SortOrder(l.y) + (l.landed ? 0 : 40), l.len);
            }
        }

        // ── Fan 발사체 ──
        void ShowAim(float k)
        {
            if (!Cats) return;
            var to = AimPoint();
            Cats.ShowWarn(0, to.x, to.y, atk.radius * (Opt("explode") != null ? 2.2f : 1), k);
        }
        Vector2 AimPoint()
        {
            if (seqTargets.Count == 0) PickTargets(1);
            return seqTargets.Count > 0 ? seqTargets[0] : new Vector2(x + face * 400, y);
        }
        void FireProj(int i, int n)
        {
            var to = AimPoint();
            float spread = atk.p1 * Mathf.Deg2Rad, a;
            if (atk.p1 >= 360) a = Random.Range(0, Mathf.PI * 2);
            else
            {
                float baseA = Mathf.Atan2(to.y - y, to.x - x);
                // 한꺼번에(dur 0) = 고르게 펼침 · 나눠서 = 범위 안 아무 데나
                a = atk.dur > 0 ? baseA + Random.Range(-0.5f, 0.5f) * spread : baseA + (n > 1 ? (i / (float)(n - 1) - 0.5f) * spread : 0);
            }
            face = Mathf.Cos(a) >= 0 ? 1 : -1;
            float spd = atk.p2 > 0 ? atk.p2 : projSpeed;
            var sp = MemeSpriteAt(atk, i);
            SpriteRenderer r = null;
            if (shotTemplate && sp) { r = Instantiate(shotTemplate, shotTemplate.transform.parent); r.sprite = sp; r.gameObject.SetActive(true); r.enabled = true; }
            bool ex = Opt("explode") != null;
            float maxD = ex ? Vector2.Distance(new Vector2(x, y), to) : projRange;
            projs.Add(new Proj { x = x + Mathf.Cos(a) * R * 0.6f, y = y + Mathf.Sin(a) * R * 0.4f, vx = Mathf.Cos(a) * spd, vy = Mathf.Sin(a) * spd, rad = atk.radius, stun = atk.stun,
                size = MemeSize, knock = atk.p3 > 0 ? atk.p3 : projKnock, maxDist = maxD, explode = ex, roll = atk.sprite != null && atk.sprite.Contains("hairball"), r = r });
            if (MemePop != null && (i == 0 || Random.value < 0.15f)) FxManager.I?.Popup(x, y, MemePop, MemeColor, 22, 0.5f, Height * 0.8f);
        }

        void UpdateProjs(float dt)
        {
            var fx = FxManager.I;
            for (int i = projs.Count - 1; i >= 0; i--)
            {
                var p = projs[i];
                float sx = p.vx * dt, sy = p.vy * dt;
                p.x += sx; p.y += sy; p.dist += Mathf.Sqrt(sx * sx + sy * sy); p.rot += dt * (p.roll ? 10 : 0);
                bool wall = !Stage.Open.Contains(StageManager.RoomOf(p.x, p.y));
                bool end = wall || p.dist >= p.maxDist;
                if (!end)
                    foreach (var o in Rats.Rats)
                    {
                        if (o.UltOn || p.hit.Contains(o)) continue;
                        float dx = o.x - p.x, dy = o.y - p.y;
                        if (dx * dx + dy * dy > p.rad * p.rad) continue;
                        p.hit.Add(o); o.Ragdoll(Mathf.Atan2(p.vy, p.vx), p.knock, 200, p.stun);
                        if (p.explode) { end = true; break; }
                    }
                if (p.r)
                {
                    float w = p.size * World.U / Mathf.Max(0.001f, p.r.sprite.bounds.size.x);
                    p.r.transform.position = World.ToUnity(p.x, p.y, 30);
                    p.r.transform.rotation = Quaternion.Euler(0, 0, p.roll ? -p.rot * Mathf.Rad2Deg : Mathf.Atan2(p.vy, Mathf.Abs(p.vx)) * Mathf.Rad2Deg * (p.vx < 0 ? -1 : 1));
                    p.r.transform.localScale = new Vector3(w * (p.vx < 0 ? 1 : -1), w, 1);      // 그림은 왼쪽을 봄
                    p.r.sortingOrder = World.SortOrder(p.y) + 40;
                }
                if (!end) continue;
                if (p.explode && !wall) { if (Cats) { int c = Cats.StunArea(p.x, p.y, p.rad * 2.2f, p.stun, ragdollSpeed, ragdollUp); Cats.HitFx(p.x, p.y, p.rad * 2.2f, SpName, c, 0.3f); } fx?.Anim("explosion", p.x, p.y, 0, p.rad / 50); fx?.Shake(0.3f); }
                else fx?.Anim("poof", p.x, p.y, 20, 0.8f);
                if (p.r) Destroy(p.r.gameObject);
                projs.RemoveAt(i);
            }
        }

        // ── Parade: 화면 왼쪽 밖 → 오른쪽 밖 ──
        void SpawnParade(int n)
        {
            var view = Ults ? Ults.ViewRect(0) : new Rect(x - World.RW / 2, y - World.RH / 2, World.RW, World.RH);
            float speed = (view.width + 600) / Mathf.Max(0.3f, atk.dur);
            walkersAll = atk.p1 >= 1; walkersStun = atk.stun;
            var (codes, lk) = SplitAt(Opt("rig"), 1);
            float len = (Cats ? Cats.catLength : 115) * lk;
            for (int i = 0; i < n; i++)
            {
                var sp = MemeSpriteAt(atk, i);
                SpriteRenderer r = null;
                if (shotTemplate && sp) { r = Instantiate(shotTemplate, shotTemplate.transform.parent); r.sprite = sp; r.gameObject.SetActive(true); r.enabled = true; r.transform.rotation = Quaternion.identity; }
                float row = n > 1 ? i / (float)(n - 1) : 0.5f;
                walkers.Add(new Walker { x = view.xMin - 200 - Random.Range(0f, 250f), y = Mathf.Lerp(view.yMin + 80, view.yMax - 80, row) + Random.Range(-30f, 30f), vx = speed * Random.Range(0.9f, 1.1f),
                    rad = atk.radius, stun = atk.stun, size = MemeSize, r = r, t = Random.Range(0f, 1f), c = MakePuppet(codes, i, len), len = len });
            }
            if (MemePop != null) FxManager.I?.Popup(x, y, MemePop, MemeColor, 40, 1.4f, Height * 1.1f);
            FxManager.I?.Shake(0.3f);
        }

        void UpdateWalkers(float dt)
        {
            if (walkers.Count == 0) return;
            var view = Ults ? Ults.ViewRect(0) : new Rect(-1e5f, -1e5f, 2e5f, 2e5f);
            var fx = FxManager.I;
            for (int i = walkers.Count - 1; i >= 0; i--)
            {
                var w = walkers[i]; w.t += dt; w.x += w.vx * dt;
                if (w.c)
                {
                    // 시위 행진: 뒷다리로 걷고 앞발로 팻말을 치켜듦 (팻말은 고양이 뒤, 머리 위로 흔들)
                    float s1 = Mathf.Sin(w.t * 16), sway = Mathf.Sin(w.t * 6);
                    var p = new RatRig.Pose { sx = 1, sy = 1, tilt = -0.35f, front = 2.4f + sway * 0.15f, farFront = 1.6f - sway * 0.2f, back = -s1 * 0.55f, farBack = s1 * 0.55f,
                        bob = -Mathf.Abs(Mathf.Cos(w.t * 16)) * 4, head = -0.1f + sway * 0.08f, tail = 0.5f + Mathf.Sin(w.t * 5) * 0.3f };
                    int order = World.SortOrder(w.y) + 30;
                    PlacePuppet(w.c, w.x, w.y, 0, p, 1, order, w.len);
                    if (w.r)
                    {
                        float s = w.size * World.U / Mathf.Max(0.001f, w.r.sprite.bounds.size.x), h = w.r.sprite.bounds.size.y * s / World.U;
                        // 막대 끝 = 치켜든 앞발 높이 (몸 뒤로 숨음), 판 = 머리 위
                        w.r.transform.position = World.ToUnity(w.x + w.len * 0.28f, w.y, w.len * 0.4f + h * 0.5f + Mathf.Abs(Mathf.Sin(w.t * 8)) * 10);
                        w.r.transform.rotation = Quaternion.Euler(0, 0, sway * 7);
                        w.r.transform.localScale = new Vector3(-s, s, 1);
                        var sg = w.c.rig.GetComponent<UnityEngine.Rendering.SortingGroup>();
                        if (sg) w.r.sortingLayerID = sg.sortingLayerID;
                        w.r.sortingOrder = order - 1;
                    }
                }
                else if (w.r)
                {
                    float s = w.size * World.U / Mathf.Max(0.001f, w.r.sprite.bounds.size.x);
                    w.r.transform.position = World.ToUnity(w.x, w.y, Mathf.Abs(Mathf.Sin(w.t * 12)) * 14);
                    w.r.transform.localScale = new Vector3(-s, s, 1);          // 오른쪽으로 감 (그림은 왼쪽을 봄)
                    w.r.sortingOrder = World.SortOrder(w.y) + 30;
                }
                if (fx && Random.value < dt * 10) fx.Dust(w.x, w.y, 1, 0.8f);
                foreach (var o in Rats.Rats)
                {
                    if (o.UltOn || w.hit.Contains(o)) continue;
                    float dx = o.x - w.x, dy = o.y - w.y;
                    if (dx * dx + dy * dy < w.rad * w.rad) { w.hit.Add(o); o.Ragdoll(0, 380, 300, w.stun); }
                }
                if (w.x > view.xMax + 300) { if (w.r) Destroy(w.r.gameObject); KillPuppet(w.c); walkers.RemoveAt(i); }
            }
            if (walkers.Count == 0 && walkersAll)
            {
                // 다 지나간 뒤: 화면 안 쥐 전부 기절 (랩터 떼)
                walkersAll = false;
                foreach (var o in Rats.Rats) if (!o.UltOn && (!Ults || Ults.OnScreen(o.x, o.y, 0))) { o.Stun(walkersStun); o.vz = Mathf.Max(o.vz, 200); }
                fx?.Shake(0.4f);
            }
        }

        // ── Orbit ──
        void AddOrbiter(int i, int n)
        {
            var sp = MemeSpriteAt(atk, i);
            SpriteRenderer r = null;
            if (shotTemplate && sp) { r = Instantiate(shotTemplate, shotTemplate.transform.parent); r.sprite = sp; r.gameObject.SetActive(true); r.enabled = true; r.transform.rotation = Quaternion.identity; }
            orbiters.Add(new Orbiter { ang = i * Mathf.PI * 2 / n, size = MemeSize, rad = atk.radius, stun = atk.stun, r = r });
        }
        void UpdateOrbiters(float dt)
        {
            if (orbiters.Count == 0) return;
            bool live = attacking && atk != null && atk.pattern == "Orbit";
            if (!live) { ClearOrbiters(true); return; }
            float appear = Mathf.Clamp01(atkT / Mathf.Max(0.1f, atk.windup));
            foreach (var o in orbiters)
            {
                o.ang += dt * orbitSpin;
                float ox = x + Mathf.Cos(o.ang) * orbitR, oy = y + Mathf.Sin(o.ang) * orbitR * 0.7f;
                if (o.r)
                {
                    float s = o.size * World.U / Mathf.Max(0.001f, o.r.sprite.bounds.size.x);
                    o.r.transform.position = World.ToUnity(ox, oy, Mathf.Abs(Mathf.Sin(o.ang * 4)) * 10);
                    o.r.transform.localScale = new Vector3(s * (-Mathf.Sin(o.ang) >= 0 ? -1 : 1), s, 1);
                    var c = o.r.color; c.a = appear; o.r.color = c;            // 투명 → 불투명하게 나타남
                    o.r.sortingOrder = World.SortOrder(oy) + 30;
                }
                if (appear < 1) continue;
                foreach (var r in Rats.Rats)
                {
                    if (r.UltOn || r.stun > 0) continue;
                    float dx = r.x - ox, dy = r.y - oy;
                    if (dx * dx + dy * dy < o.rad * o.rad) r.Ragdoll(Mathf.Atan2(dy, dx), 320, 280, o.stun);
                }
            }
        }
        void ClearOrbiters(bool poof)
        {
            foreach (var o in orbiters)
            {
                if (poof) FxManager.I?.Anim("poof", x + Mathf.Cos(o.ang) * orbitR, y + Mathf.Sin(o.ang) * orbitR * 0.7f, 20, 1);
                if (o.r) Destroy(o.r.gameObject);
            }
            orbiters.Clear();
        }

        // ── Path ──
        void BuildPath()
        {
            pathPts.Clear();
            Vector2 c = Cats && Cats.FindCrowd(x, y, out var at, out _) ? at : new Vector2(x, y);
            float R0 = atk.p2 > 0 ? atk.p2 : pathRadius;
            if (atk.p1 >= 1)
            {
                // 나선: 바깥 → 안쪽 (3바퀴)
                const int steps = 36;
                for (int i = 0; i <= steps; i++)
                {
                    float kk = i / (float)steps, a = kk * Mathf.PI * 6, rr = R0 * (1 - kk * 0.85f);
                    pathPts.Add(new Vector2(c.x + Mathf.Cos(a) * rr, c.y + Mathf.Sin(a) * rr * 0.75f));
                }
            }
            else
            {
                // 별 (꼭짓점 한 칸 건너뛰기) + 오각형 테두리 → 거미줄 모양
                var v = new Vector2[5];
                for (int i = 0; i < 5; i++) { float a = -Mathf.PI / 2 + i * Mathf.PI * 2 / 5; v[i] = new Vector2(c.x + Mathf.Cos(a) * R0, c.y + Mathf.Sin(a) * R0 * 0.75f); }
                pathPts.Add(new Vector2(x, y));
                foreach (int i in new[] { 0, 2, 4, 1, 3, 0, 1, 2, 3, 4, 0 }) pathPts.Add(v[i]);
            }
            for (int i = 0; i < pathPts.Count; i++) if (!Stage.Open.Contains(StageManager.RoomOf(pathPts[i].x, pathPts[i].y))) pathPts[i] = i > 0 ? pathPts[i - 1] : new Vector2(x, y);
        }
        void ShowPathWarn(float k)
        {
            if (!Cats) return;
            int n = 0;
            for (int i = 1; i < pathPts.Count && n < 12; i += Mathf.Max(1, pathPts.Count / 10)) Cats.ShowWarn(n++, pathPts[i].x, pathPts[i].y, atk.radius, k);
            Cats.HideWarns(n);
        }

        // ── 매 프레임 (공격 밖에서도): 발사체 · 행렬 · 회전 · 삼킨 쥐 · 모습 ──
        void UpdateReworkFx(float dt)
        {
            UpdateProjs(dt); UpdateWalkers(dt); UpdateLeapers(dt); UpdateOrbiters(dt); UpdateCrew(dt);
            for (int i = swallowed.Count - 1; i >= 0; i--)
            {
                var o = swallowed[i];
                if (!o || !o.Swallowed) { swallowed.RemoveAt(i); continue; }
                o.HoldAt(mouth.x, mouth.y, mouth.z, face);          // 입 자리 · 뱉을 땐 보는 쪽으로
            }
            if (bodyT > 0 && (bodyT -= dt) <= 0) bodySprite = null;
            if (hideSprite && !(attacking && Opt("hide") != null)) { hideSprite = null; Hidden = false; }
        }

        // LateUpdate 끝: 보스를 그림 한 장으로 바꾸거나 숨김 (식빵 · 서서 확성기 · 지느러미)
        void ApplyForm()
        {
            var sp = hideSprite ? hideSprite : bodySprite;
            bool on = sp && State == BState.Fight;
            if (on)
            {
                if (rig) rig.gameObject.SetActive(false);
                if (catRig) catRig.gameObject.SetActive(false);
                if (partRig) partRig.gameObject.SetActive(false);
                if (!formFx && shotTemplate) { formFx = Instantiate(shotTemplate, shotTemplate.transform.parent); formFx.name = "BossForm"; }
                if (formFx)
                {
                    formFx.sprite = sp; formFx.gameObject.SetActive(true); formFx.enabled = true;
                    float size = hideSprite ? MemeSize : bodySize;
                    float s = size * World.U / Mathf.Max(0.001f, sp.bounds.size.x);
                    formFx.transform.position = World.ToUnity(x, y, sp.bounds.size.y * s / World.U * 0.5f + Mathf.Abs(Mathf.Sin(t * 8)) * 4);
                    formFx.transform.localScale = new Vector3(s * (face > 0 ? -1 : 1), s, 1);
                    formFx.transform.rotation = Quaternion.identity;
                    formFx.color = hideSprite ? new Color(1f, 0.55f, 0.5f) : Color.white;       // 지느러미 = 붉게 물듦
                    formFx.sortingOrder = World.SortOrder(y) + 5;
                }
            }
            else if (formFx && formFx.gameObject.activeSelf)
            {
                formFx.gameObject.SetActive(false);
                if (State == BState.Fight) SetVisible(true);
            }
        }

        void ClearReworkFx()
        {
            foreach (var p in projs) if (p.r) Destroy(p.r.gameObject);
            projs.Clear();
            foreach (var w in walkers) { if (w.r) Destroy(w.r.gameObject); KillPuppet(w.c); }
            walkers.Clear(); walkersAll = false;
            foreach (var l in leapers) KillPuppet(l.c);
            leapers.Clear();
            ClearOrbiters(false);
            foreach (var o in swallowed) if (o && o.Swallowed) o.swallowT = 0.01f;      // 다음 프레임에 뱉음
            swallowed.Clear();
            ClearCrew();
            if (formFx) formFx.gameObject.SetActive(false);
            bodySprite = null; hideSprite = null; bodyT = 0;
        }
    }
}
