using NKK.Data;
using NKK.Rats;
using NKK.Stage;
using UnityEngine;

namespace NKK.Hazards
{
    // 밈 기술 (2026-10-19, 100층 확장 보스 17마리): 보스 테이블 Atk_Type 에 pattern 칸이 있는 기술은 코드 없이 테이블 값만으로 동작.
    // 기술 하나 = 패턴(아래 17종) + 수치(radius · stun · count · windup · dur · p1 · p2) + 그림(sprite = memeSprites 이름) + 색 + 팝업 글.
    // 어느 칸(atk_type · atk2_type · skill_type · special1/2_type)에 넣어도 됨. 목록·원본 밈 = 기획서/100층 확장 구현 기록.md
    //  Drop     쥐 무리 count 곳에 경고 원 → 그림이 하늘에서 떨어짐 (착지 범위 기절). p1 = 착지 뒤: 0 사라짐 · 1~4 춤(맥스웰 춤 0~3) · 5 그 자리에 p2 초 머묾 · 6 쥐에게 걸어가다 펑
    //  Throw    화면 속 쥐에게 count 개 던짐 (dur 동안 나눠서). p1 = 겨냥 흔들림(게임 단위, 클수록 엉뚱한 데)
    //  Pulse    보스 둘레 충격파 count 번 (dur 동안, 반경이 점점 커짐)
    //  Shout    웅크림 → 큰 외침: 반경 안 쥐 날림·기절 + 큰 팝업. sprite = 머리 위 소품 (확성기 등)
    //  Zone     쥐 무리 count 곳(p1 = 1 이면 보스 자리)에 바닥 장판 dur 초 (안에 p2 초(기본 0.6) 머문 쥐 기절)
    //  Dash     쥐 무리 쪽으로 count 번 돌진 (dur 동안), 지나간 반경 안 쥐 날림. p1 = 속도 (기본 Boss.zoomSpeed)
    //  Spin     dur 초 동안 빙글빙글 돌며 쥐 쪽으로 (p1 = 속도), 반경 안 쥐 날림. 팝업 0.6초마다
    //  Hop      쥐 무리 count 곳으로 차례로 높이 점프 → 착지 범위 기절 (무야호와 같은 움직임)
    //  Sweep    쥐 무리를 지나는 가로줄에 count 곳 차례로 낙하 폭격 (dur 동안, 왼쪽 → 오른쪽)
    //  Beam     보스 눈/몸에서 쥐 무리 count 곳으로 빔 (dur 동안 차례로). p1 = 1 이면 사방으로 고르게 (360°)
    //  Pull     쥐 무리 한 곳에 소용돌이: windup 동안 반경 안 쥐를 빨아들임 → 펑 (사방으로 튕김). sprite = 가운데에서 빙글 도는 그림
    //  Teleport count 번: 보스가 사라졌다가(sprite = 들어가는 구멍) 쥐 무리 자리에 나타나며 범위 기절
    //  Global   windup 동안 화면이 color 로 깜빡 → 화면 안 모든 쥐 stun 초 기절 + 큰 팝업 (모두 멈춤)
    //  Buff     오라 폭발 (반경 밀어냄) + dur 초 동안 받는 피해 × p1 (기본 0.5)
    //  Summon   보스 둘레에 sprite 를 count 마리 소환 → 쥐에게 걸어가다 닿거나 dur 초가 지나면 펑 (반경 기절). p1 = 걷는 속도
    //  Mark     쥐 count 마리에 표식(sprite) → windup 뒤 그 쥐들을 멀리 날려 버림 (기절)
    //  Rush     sprite 를 count 개 쥐 무리 쪽으로 가로질러 날림 (dur 초, 방 안에서 튕김), 닿은 반경 안 쥐 기절
    //  (Throw · Parade 에 opt rig:고양이코드 = 그림 대신 고양이 리그 소환, Boss.Rework.cs)
    public partial class Boss
    {
        [Header("밈 기술 (Atk_Type pattern · sprite 이름 = 이 목록)")]
        [Tooltip("밈 기술 그림 (Fill Meme Sprites = Assets/Art/Rats/BossMeme)")] public System.Collections.Generic.List<NKK.FxManager.NamedSprite> memeSprites = new();
        [Tooltip("밈 그림 기본 크기 (게임 단위, size 칸이 0 일 때)")] public float memeSize = 120;
        [Tooltip("소환물 기본 걷기 속도 · 회전 기본 속도")] public float memeWalk = 140, memeSpinMove = 260;
        System.Collections.Generic.Dictionary<string, Sprite> memeMap;
        float memeBuffT, memeBuffMul = 1;
        readonly System.Collections.Generic.List<(Rat r, float t)> marked = new();
        class Rusher { public float x, y, vx, vy, t, life, rad, stun, size; public SpriteRenderer r; }
        readonly System.Collections.Generic.List<Rusher> rushers = new();
        SpriteRenderer pullFx; float pullRot;

        bool IsMeme(string t) { var a = DB.BossAtk(t); return a != null && !string.IsNullOrEmpty(a.pattern) && a.pattern != "-"; }
        public float MemeDmgMul => memeBuffT > 0 ? memeBuffMul : 1;

        public Sprite MemeSprite(string n)
        {
            if (string.IsNullOrEmpty(n) || n == "-") return null;
            if (n[0] == '@') return n switch      // '@이름' = 보스 인스펙터의 기존 그림
            {
                "@grill" => grillSprite, "@cone" => coneSprite, "@gas" => gasSprite, "@ice" => iceSprite, "@coffin" => coffinSprite, "@maxwell" => maxwellSprite, "@shark" => sharkSprite,
                "@money" => moneySprite, "@banana" => bananaCatSprite, "@butter" => butterCatSprite, "@flask" => flaskSprite, "@hairball" => hairballSprite, "@fireball" => fireballSprite, _ => null,
            };
            if (memeMap == null) { memeMap = new(); foreach (var s in memeSprites) if (s.sprite && !memeMap.ContainsKey(s.name)) memeMap[s.name] = s.sprite; }
            return memeMap.TryGetValue(n, out var sp) ? sp : null;
        }
        Color MemeColor => atk != null && ColorUtility.TryParseHtmlString(atk.color, out var c) ? c : Data.Color;
        float MemeSize => atk != null && atk.size > 0 ? atk.size : memeSize;
        string MemePop => atk != null && !string.IsNullOrEmpty(atk.popup) && atk.popup != "-" ? atk.popup : null;

        // 시작: 필살(배너) · 스킬(이름 팝업) · 일반(대사)은 원래 Begin* 이 처리한 뒤 부름
        void BeginMeme()
        {
            seqI = 0; released = 0; phaseT = 0; shots = 0; seqTargets.Clear(); marked.Clear();
            int n = Mathf.Max(1, atk.count);
            var sp = MemeSprite(atk.sprite);
            switch (atk.pattern)
            {
                case "Drop": case "Hop": case "Zone": case "Beam": case "Rush": PickTargets(n); break;
                case "Pull": PickTargets(1); break;
                case "Sweep": PickSweep(n); break;
                case "Shout": case "Buff": case "Spin": case "Dash": if (sp) ShowHeld(sp, MemeSize); break;      // 손에 든 소품 (고양이는 머리 위)
                case "Mark":
                {
                    var pool = new System.Collections.Generic.List<Rat>();
                    foreach (var o in Rats.Rats) if (!o.UltOn && Ults && Ults.OnScreen(o.x, o.y, 0)) pool.Add(o);
                    for (int i = 0; i < n && pool.Count > 0; i++) { int k = Random.Range(0, pool.Count); marked.Add((pool[k], 0)); AddMark(pool[k], atk.windup + 0.4f, sp, false); pool.RemoveAt(k); }
                    break;
                }
            }
            if (atk.pattern == "Zone" && atk.p1 >= 1) { seqTargets.Clear(); seqTargets.Add(new Vector2(x, y)); }
            BeginRework();          // 보스 개편 패턴 · opt (Boss.Rework.cs)
        }

        // 쥐 무리를 지나는 가로줄 (방 안으로 자름)
        void PickSweep(int n)
        {
            PickTargets(1);
            var c = seqTargets.Count > 0 ? seqTargets[0] : new Vector2(x, y);
            seqTargets.Clear();
            float span = 900;
            for (int i = 0; i < n; i++)
            {
                float px = c.x - span / 2 + span * (n > 1 ? i / (float)(n - 1) : 0.5f), py = c.y + Mathf.Sin(i * 1.7f) * 40;
                if (!Stage.Open.Contains(StageManager.RoomOf(px, py))) continue;
                seqTargets.Add(new Vector2(px, py));
            }
            if (seqTargets.Count == 0) seqTargets.Add(c);
        }

        void MemeHit(float hx, float hy, float rad, float stun, bool items = true)
        {
            int n = Cats ? Cats.StunArea(hx, hy, rad, stun, ragdollSpeed, ragdollUp) : 0;
            if (items && Cats) Cats.LaunchItems(hx, hy, rad, itemLaunch);
            if (Cats) Cats.HitFx(hx, hy, rad, SpName, n, 0.3f);
            var fx = FxManager.I; if (fx) { fx.Ring(hx, hy, rad, MemeColor, 0.45f); fx.Burst(hx, hy, 20, 16, MemeColor, Color.white, 150, 420); }
            if (MemePop != null) fx?.Popup(hx, hy, MemePop, MemeColor, 26, 0.8f, 70);
        }

        void UpdateMeme(float dt)
        {
            var fx = FxManager.I;
            float wind = Mathf.Max(0.1f, atk.windup), k = Mathf.Clamp01(atkT / wind);
            int n = Mathf.Max(1, atk.count);
            var sp = MemeSprite(atk.sprite);
            ReworkWindup(dt);          // opt red · shake · dust (Boss.Rework.cs)
            switch (atk.pattern)
            {
                case "Drop":
                case "Sweep":
                {
                    jit = atkT < wind ? 2 : 0;
                    ShowWarns(released, k);
                    if (atkT < wind) break;
                    float gap = Mathf.Max(0.05f, atk.dur / Mathf.Max(1, seqTargets.Count));
                    while (released < seqTargets.Count && atkT >= wind + released * gap)
                    {
                        var v = seqTargets[released];
                        var spi = atk.sprite != null && atk.sprite.Contains(",") ? MemeSpriteAt(atk, released) : sp ? sp : atk.sprite != null && atk.sprite != "-" && !atk.sprite.StartsWith("@") ? placeholderSprite : null;
                        released++;
                        int after = Mathf.RoundToInt(atk.p1);
                        string stk = Opt("sticky"), mph = Opt("morph");
                        if (atk.pattern == "Drop" && spi && (after > 0 || stk != null || mph != null))
                        {
                            int kind = after <= 0 ? 5 : after <= 4 ? after - 1 : after == 5 ? 5 : 6;
                            AddDrop(v, spi, MemeSize, kind, after == 5 || after <= 0 ? Mathf.Max(0.5f, atk.p2) : after == 6 ? Mathf.Max(1, atk.p2) : Mathf.Max(1, atk.p2 > 0 ? atk.p2 : maxwellDance));
                            var md = maxwells[maxwells.Count - 1];
                            if (stk != null) md.sticky = float.TryParse(stk, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var st) ? st : 3;
                            if (mph != null) { var (mn, mt) = SplitAt(mph, 3); md.morph = SpriteOrTemp(mn); md.morphT = mt; }
                        }
                        else Cats?.AddStrike(true, v.x, v.y, atk.radius, atk.stun, SpName, sp);
                        if (MemePop != null) fx?.Popup(v.x, v.y, MemePop, MemeColor, 24, 0.8f, 120);
                    }
                    if (released >= seqTargets.Count) { Cats?.HideWarns(); attacking = false; }
                    break;
                }
                case "Throw":
                {
                    bool rigged = Opt("rig") != null;
                    jit = atkT < wind ? (Opt("dust") != null ? 2 + 5 * k : 2) : 0;          // 먼지 구름: 진동이 점점 심해짐
                    float gap = atk.dur / n;
                    while (shots < n && atkT >= wind + shots * gap)
                    {
                        shots++;
                        float tx, ty;
                        if (Opt("around") != null)
                        {
                            // 보스 둘레 사방으로 포물선 (좋았쓰 고양이 · HUH 글자 팝콘)
                            float a = shots * Mathf.PI * 2 / n + Random.Range(-0.4f, 0.4f), d = Random.Range(220f, 480f) + atk.p1;
                            tx = x + Mathf.Cos(a) * d; ty = y + Mathf.Sin(a) * d * 0.75f;
                        }
                        else
                        {
                            var r = Rats.RandomNear(x, y, 1200) ?? Rats.NearestRat(x, y, 2000);
                            if (!r) continue;
                            tx = r.x + Random.Range(-1f, 1f) * (40 + atk.p1); ty = r.y + Random.Range(-1f, 1f) * (30 + atk.p1 * 0.7f);
                        }
                        if (!Stage.Open.Contains(StageManager.RoomOf(tx, ty))) { var nr = Rats.NearestRat(tx, ty, 3000); if (nr) { tx = nr.x; ty = nr.y; } else { tx = x; ty = y; } }      // 방 바깥이면 가까운 쥐 자리로
                        face = tx >= x ? 1 : -1;
                        if (rigged) Leap(tx, ty); else Throw(tx, ty, atkType);          // rig = 그림 대신 고양이가 튀어나옴
                    }
                    if (shots >= n && atkT > wind + atk.dur + 0.2f) attacking = false;
                    break;
                }
                case "Pulse":
                {
                    if (atkT < wind) { jit = 2; break; }
                    float per = atk.dur / n;
                    if (seqI < n && atkT >= wind + seqI * per)
                    {
                        float rad = atk.radius * Mathf.Lerp(0.5f, 1, n > 1 ? seqI / (float)(n - 1) : 1);
                        if (Cats) Cats.StunArea(x, y, rad, atk.stun, 220, 260);
                        fx?.Ring(x, y, rad, MemeColor, 0.4f); fx?.Ring(x, y, rad * 0.7f, Color.white, 0.3f); fx?.Shake(0.12f);
                        if (sp) fx?.Burst(x, y, Height, 3, MemeColor, Color.white, 60, 160);
                        if (MemePop != null) fx?.Popup(x + Random.Range(-60f, 60f), y, MemePop, MemeColor, 26 + seqI * 2, 0.6f, Height * 0.9f);
                        sq = 0.8f; face = -face; seqI++;
                    }
                    if (seqI >= n && atkT > wind + atk.dur) attacking = false;
                    break;
                }
                case "Shout":
                    jit = atkT < wind ? 3 : 0;
                    if (atkT >= wind && !atkHit)
                    {
                        atkHit = true;
                        foreach (var o in Rats.Rats)
                        {
                            if (o.UltOn) continue;
                            float dx = o.x - x, dy = o.y - y;
                            if (dx * dx + dy * dy < atk.radius * atk.radius) o.Ragdoll(Mathf.Atan2(dy, dx), swingSpeed, swingUp, atk.stun);
                        }
                        fx?.Ring(x, y, atk.radius, MemeColor, 0.6f); fx?.Ring(x, y, atk.radius * 0.6f, Color.white, 0.45f);
                        fx?.Stars(x, y, Height * 0.6f, 30, MemeColor, Color.white, 200, 600); fx?.Shake(0.45f);
                        if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 52, 1.4f, Height * 1.15f);
                        if (Ults) Ults.Flash(MemeColor, 0.2f);
                    }
                    if (atkT > wind + Mathf.Max(0.4f, atk.dur)) { HideProp(); attacking = false; }
                    break;
                case "Zone":
                    jit = atkT < wind ? 1.5f : 0;
                    ShowWarns(0, k);
                    if (atkT >= wind)
                    {
                        Cats?.HideWarns();
                        foreach (var v in seqTargets) { AddMemeZone(v.x, v.y, sp); fx?.Burst(v.x, v.y, 20, 20, MemeColor, Color.white, 120, 380); if (MemePop != null) fx?.Popup(v.x, v.y, MemePop, MemeColor, 24, 0.9f, 80); }
                        fx?.Shake(0.2f);
                        attacking = false;
                    }
                    break;
                case "Dash":
                {
                    if (atkT < wind) { jit = 2; break; }
                    float per = atk.dur / n, spd = atk.p1 > 0 ? atk.p1 : zoomSpeed;
                    int i = Mathf.FloorToInt((atkT - wind) / per);
                    if (i >= n) { vx *= 0.2f; vy *= 0.2f; HideProp(); attacking = false; break; }
                    if (i != seqI || phaseT == 0)
                    {
                        seqI = i; phaseT = 1;
                        float a = Random.Range(0, Mathf.PI * 2);
                        if (Cats && Cats.FindCrowd(x, y, out var at, out _)) a = Mathf.Atan2(at.y - y, at.x - x);
                        vx = Mathf.Cos(a) * spd; vy = Mathf.Sin(a) * spd;
                        if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 26, 0.6f, Height * 0.8f);
                    }
                    else { float s = Mathf.Sqrt(vx * vx + vy * vy); if (s < spd * 0.7f && s > 1) { vx *= spd / s; vy *= spd / s; } }
                    foreach (var o in Rats.Rats) if (!o.UltOn && o.stun <= 0 && (o.x - x) * (o.x - x) + (o.y - y) * (o.y - y) < atk.radius * atk.radius) o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x), 520, 380, atk.stun);
                    if (Random.value < dt * 30) { fx?.Dust(x, y, 2, 1); fx?.Burst(x, y, 10, 3, MemeColor, Color.white, 80, 220); }
                    break;
                }
                case "Spin":
                {
                    if (atkT < wind) { jit = 2; break; }
                    face = Mathf.Sin(atkT * 26) > 0 ? 1 : -1;
                    var r = Rats.NearestRat(x, y, 2000);
                    float spd = atk.p1 > 0 ? atk.p1 : memeSpinMove;
                    if (r) { float a = Mathf.Atan2(r.y - y, r.x - x); vx = Mathf.Cos(a) * spd; vy = Mathf.Sin(a) * spd; }
                    foreach (var o in Rats.Rats) if (!o.UltOn && o.stun <= 0 && (o.x - x) * (o.x - x) + (o.y - y) * (o.y - y) < atk.radius * atk.radius) o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x) + 1.2f, 480, 360, atk.stun);
                    if (Random.value < dt * 10) fx?.Ring(x, y, atk.radius, MemeColor, 0.25f);
                    if (MemePop != null && Mathf.Repeat(atkT, 0.6f) < dt) fx?.Popup(x + Random.Range(-50f, 50f), y, MemePop, MemeColor, 26, 0.5f, Height);
                    if (atkT > wind + atk.dur) { vx *= 0.2f; vy *= 0.2f; HideProp(); attacking = false; }
                    break;
                }
                case "Hop":
                {
                    if (seqI >= seqTargets.Count) { Cats?.HideWarns(); attacking = false; break; }
                    phaseT += dt;
                    if (!air)
                    {
                        jit = 3; ShowWarns(seqI, Mathf.Clamp01(phaseT / wind));
                        if (phaseT >= wind)
                        {
                            air = true; phaseT = 0; lx0 = x; ly0 = y; ltx = seqTargets[seqI].x; lty = seqTargets[seqI].y; vz = jumpV;
                            if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 30 + seqI * 5, 0.9f, Height * 0.9f);
                        }
                    }
                    else
                    {
                        float T = 2 * jumpV / gravity, kk = Mathf.Clamp01(phaseT / T);
                        x = Mathf.Lerp(lx0, ltx, kk); y = Mathf.Lerp(ly0, lty, kk); vx = vy = 0;
                        ShowWarns(seqI, 1);
                        if (kk >= 1 || (z <= 0 && phaseT > 0.1f)) { z = 0; vz = 0; air = false; phaseT = 0; MemeHit(x, y, atk.radius, atk.stun); fx?.Shake(0.4f); seqI++; }
                    }
                    break;
                }
                case "Beam":
                {
                    jit = atkT < wind ? 2 : 0;
                    if (atk.p1 >= 1 && seqTargets.Count < n) { seqTargets.Clear(); for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; seqTargets.Add(new Vector2(x + Mathf.Cos(a) * atk.radius * 2.2f, y + Mathf.Sin(a) * atk.radius * 1.6f)); } }
                    if (atkT < wind) { if (atk.p1 < 1) ShowWarns(0, k); break; }
                    float gap = Mathf.Max(0.05f, atk.dur / Mathf.Max(1, seqTargets.Count));
                    while (released < seqTargets.Count && atkT >= wind + released * gap)
                    {
                        var v = seqTargets[released++];
                        var e = EyePos();
                        face = v.x >= x ? 1 : -1;
                        fx?.Beam(e.x, e.y, e.z, v.x, v.y, 0, MemeColor, 0.6f, 2f);
                        int c = Cats ? Cats.StunArea(v.x, v.y, atk.radius, atk.stun, 120, 300) : 0;
                        Cats?.HitFx(v.x, v.y, atk.radius, SpName, c, 0.2f);
                        fx?.Anim("explosion", v.x, v.y, 0, atk.radius / 100);
                        if (MemePop != null) fx?.Popup(e.x, e.y, MemePop, MemeColor, 24, 0.6f, e.z + 30);
                    }
                    if (released >= seqTargets.Count) { Cats?.HideWarns(); attacking = false; }
                    break;
                }
                case "Pull":
                {
                    var c = seqTargets.Count > 0 ? seqTargets[0] : new Vector2(x, y);
                    if (atkT < wind)
                    {
                        Cats?.ShowVortex(c.x, c.y, atk.radius, k); Cats?.ShowWarn(0, c.x, c.y, atk.radius, k);
                        ShowPull(c, sp, k);
                        float spd = holePull * (0.3f + 0.7f * k);
                        foreach (var o in Rats.Rats)
                        {
                            if (o.UltOn) continue;
                            float dx = c.x - o.x, dy = c.y - o.y, d = Mathf.Sqrt(dx * dx + dy * dy);
                            if (d > atk.radius * 1.6f || d < 14) continue;
                            o.Stun(0.25f); float m = Mathf.Min(spd * dt, d - 12) / d; o.x += dx * m; o.y += dy * m;
                        }
                        if (MemePop != null && Mathf.Repeat(atkT, 0.7f) < dt) fx?.Popup(c.x, c.y, MemePop, MemeColor, 26, 0.6f, 120);
                        break;
                    }
                    Cats?.HideWarns(); HidePull();
                    int hit = 0;
                    foreach (var o in Rats.Rats)
                    {
                        if (o.UltOn || (o.x - c.x) * (o.x - c.x) + (o.y - c.y) * (o.y - c.y) > atk.radius * atk.radius * 2.6f) continue;
                        o.Ragdoll(Random.Range(0, Mathf.PI * 2), Random.Range(350f, 700f), Random.Range(500f, 800f), atk.stun); hit++;
                    }
                    Cats?.HitFx(c.x, c.y, atk.radius, SpName, hit, 0.45f);
                    fx?.Stars(c.x, c.y, 100, 40, MemeColor, Color.white, 250, 800); fx?.Anim("explosion", c.x, c.y, 0, atk.radius / 90);
                    if (Ults) Ults.Flash(Color.white, 0.3f);
                    attacking = false;
                    break;
                }
                case "Teleport":
                {
                    // released: 0 숨기 · 1 숨어서 기다림 (경고 원) · 2 나타난 뒤 쉼
                    phaseT += dt;
                    float hideT = Mathf.Max(0.6f, atk.dur / n);
                    if (released == 0)
                    {
                        jit = 2;
                        if (phaseT < wind) break;
                        if (sp) AddDrop(new Vector2(x, y), sp, MemeSize, 5, 1.2f);
                        released = 1; phaseT = 0; Hidden = true; seqTargets.Clear(); PickTargets(1);
                        fx?.Dust(x, y, 14, 2); fx?.Anim("poof", x, y, 0, 2);
                        if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 30, 0.9f, Height);
                        break;
                    }
                    if (released == 1)
                    {
                        var tg = seqTargets.Count > 0 ? seqTargets[0] : new Vector2(x, y);
                        Cats?.ShowWarn(0, tg.x, tg.y, atk.radius, Mathf.Clamp01(phaseT / hideT));
                        if (phaseT < hideT) break;
                        Cats?.HideWarns();
                        if (sp) AddDrop(tg, sp, MemeSize, 5, 1.2f);
                        x = tg.x; y = tg.y; vx = vy = 0; Hidden = false; cloakA = 1; sq = 0.6f; z = 0;
                        MemeHit(x, y, atk.radius, atk.stun); fx?.Shake(0.4f);
                        seqI++; released = 2; phaseT = 0;
                        break;
                    }
                    if (phaseT < 0.5f) break;
                    if (seqI >= n) { attacking = false; break; }
                    released = 0; phaseT = 0;
                    break;
                }
                case "Global":
                    jit = atkT < wind ? 3 : 0;
                    if (atkT < wind && Ults && Mathf.Repeat(atkT, 0.35f) < dt) Ults.Flash(MemeColor, 0.18f);
                    if (atkT >= wind && !atkHit)
                    {
                        atkHit = true; int c = 0;
                        bool all = Opt("all") != null;          // 맵 전체 (긴급 회의)
                        foreach (var o in Rats.Rats) if (!o.UltOn && (all || !Ults || Ults.OnScreen(o.x, o.y, 0))) { o.Stun(atk.stun); o.vz = Mathf.Max(o.vz, 220); c++; }
                        if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 56, 2, Height * 1.2f);
                        fx?.Shake(0.6f); fx?.Stars(x, y, Height * 0.6f, 40, MemeColor, Color.white, 200, 700);
                        if (Ults) Ults.Flash(MemeColor, 0.45f);
                        if (sp) ShowProp(sp, MemeSize, true);
                    }
                    if (atkT > wind + Mathf.Max(0.5f, atk.dur)) { HideProp(); attacking = false; }
                    break;
                case "Buff":
                    jit = atkT < wind ? 2.5f : 0;
                    if (atkT >= wind)
                    {
                        if (Cats) Cats.StunArea(x, y, atk.radius, atk.stun, 520, 300);
                        memeBuffT = atk.dur; memeBuffMul = atk.p1 > 0 ? atk.p1 : 0.5f;
                        fx?.Ring(x, y, atk.radius, MemeColor, 0.6f); fx?.Stars(x, y, Height * 0.5f, 40, MemeColor, Color.white, 200, 600); fx?.Shake(0.3f);
                        if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 42, 1.3f, Height * 0.95f);
                        if (Ults) Ults.Flash(MemeColor, 0.25f);
                        HideProp(); attacking = false;
                    }
                    break;
                case "Summon":
                    jit = atkT < wind ? 2 : 0;
                    if (atkT >= wind)
                    {
                        for (int i = 0; i < n; i++)
                        {
                            float a = i * Mathf.PI * 2 / n + Random.Range(-0.3f, 0.3f);
                            var v = new Vector2(x + Mathf.Cos(a) * 220, y + Mathf.Sin(a) * 150);
                            if (!Stage.Open.Contains(StageManager.RoomOf(v.x, v.y))) v = new Vector2(x, y);
                            if (sp) AddDrop(v, sp, MemeSize, 6, Mathf.Max(1, atk.dur)); else Cats?.AddStrike(true, v.x, v.y, atk.radius, atk.stun, SpName);
                        }
                        if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 36, 1, Height);
                        attacking = false;
                    }
                    break;
                case "Mark":
                    jit = atkT < wind ? 2 : 0;
                    if (atkT >= wind)
                    {
                        foreach (var (r, _) in marked)
                        {
                            if (!r || r.UltOn) continue;
                            r.Ragdoll(Random.Range(0, Mathf.PI * 2), Random.Range(700f, 950f), Random.Range(800f, 1000f), atk.stun);
                            if (MemePop != null) fx?.Popup(r.x, r.y, MemePop, MemeColor, 22, 0.9f, 60);
                            fx?.Burst(r.x, r.y, 20, 10, MemeColor, Color.white, 120, 320);
                        }
                        marked.Clear(); fx?.Shake(0.3f);
                        attacking = false;
                    }
                    break;
                case "Rush":
                    jit = atkT < wind ? 2 : 0;
                    ShowWarns(0, k);
                    if (atkT >= wind)
                    {
                        Cats?.HideWarns();
                        for (int i = 0; i < seqTargets.Count; i++) AddRusher(seqTargets[i], i, sp);
                        if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 34, 1, Height);
                        attacking = false;
                    }
                    break;
                default: if (!UpdateRework(dt)) attacking = false; break;
            }
        }

        // ── 장판 (밈): 그림 = 바닥 자국, 안에 p2 초 머문 쥐 기절 ──
        void AddMemeZone(float zx, float zy, Sprite sp)
        {
            var z0 = new Zone { x = zx, y = zy, rad = atk.radius, life = atk.dur, life0 = atk.dur, stun = atk.stun, expose = atk.p2 > 0 ? atk.p2 : 0.6f, meme = true, color = MemeColor, popup = MemePop };
            var mk = Opt("mask"); if (mk != null) { z0.immune = atk.p3 > 0 ? atk.p3 : 5; z0.mask = SpriteOrTemp(mk); }      // 방구가스: 기절한 쥐에 방독면 (p3 초 면역)
            if (shotTemplate && sp) { z0.r = Instantiate(shotTemplate, shotTemplate.transform.parent); z0.r.sprite = sp; z0.r.gameObject.SetActive(true); z0.r.enabled = true; z0.r.transform.rotation = Quaternion.identity; }
            zones.Add(z0);
        }

        // ── 가로질러 날아가는 것 (밈 Rush) ──
        void AddRusher(Vector2 to, int i, Sprite sp)
        {
            if (!shotTemplate || !sp) { Cats?.AddStrike(true, to.x, to.y, atk.radius, atk.stun, SpName); return; }
            float a = Mathf.Atan2(to.y - y, to.x - x) + (i - (seqTargets.Count - 1) * 0.5f) * 0.15f, spd = atk.p1 > 0 ? atk.p1 : butterSpeed;
            var r = Instantiate(shotTemplate, shotTemplate.transform.parent); r.sprite = sp; r.gameObject.SetActive(true); r.enabled = true; r.transform.rotation = Quaternion.identity;
            rushers.Add(new Rusher { x = x, y = y, vx = Mathf.Cos(a) * spd, vy = Mathf.Sin(a) * spd, t = i * 0.3f, life = Mathf.Max(1, atk.dur), rad = atk.radius, stun = atk.stun, size = MemeSize, r = r });
        }

        void UpdateMemeFx(float dt)
        {
            if (memeBuffT > 0) { memeBuffT -= dt; if (Random.value < dt * 6) FxManager.I?.Stars(x, y, Height * 0.5f, 2, Data.Color, Color.white, 60, 160); }
            var fx = FxManager.I;
            for (int i = rushers.Count - 1; i >= 0; i--)
            {
                var b = rushers[i]; b.t += dt; b.life -= dt;
                if (b.life <= 0 || !b.r) { fx?.Anim("poof", b.x, b.y, 50, 1.2f); if (b.r) Destroy(b.r.gameObject); rushers.RemoveAt(i); continue; }
                float nx = b.x + b.vx * dt, ny = b.y + b.vy * dt;
                if (!Stage.Open.Contains(StageManager.RoomOf(nx, b.y))) { b.vx = -b.vx; nx = b.x; }
                if (!Stage.Open.Contains(StageManager.RoomOf(b.x, ny))) { b.vy = -b.vy; ny = b.y; }
                b.x = nx; b.y = ny;
                float w = b.size * World.U / Mathf.Max(0.001f, b.r.sprite.bounds.size.x);
                b.r.transform.position = World.ToUnity(b.x, b.y, 50 + Mathf.Sin(b.t * 6) * 12);
                b.r.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(b.t * 8) * 12);
                b.r.transform.localScale = new Vector3(w * (b.vx < 0 ? 1 : -1), w, 1);      // 그림은 왼쪽을 봄
                b.r.sortingOrder = World.SortOrder(b.y) + 40;
                foreach (var o in Rats.Rats)
                {
                    if (o.UltOn || o.stun > 0) continue;
                    float dx = o.x - b.x, dy = o.y - b.y;
                    if (dx * dx + dy * dy < b.rad * b.rad) o.Ragdoll(Mathf.Atan2(dy, dx), 300, 420, b.stun);
                }
                if (fx && Random.value < dt * 8) fx.Dust(b.x, b.y, 1, 0.8f);
            }
            if (pullFx && pullFx.gameObject.activeSelf && !(attacking && atk != null && atk.pattern == "Pull")) HidePull();
        }

        void ShowPull(Vector2 c, Sprite sp, float k)
        {
            if (!sp || !shotTemplate) return;
            if (!pullFx) { pullFx = Instantiate(shotTemplate, shotTemplate.transform.parent); pullFx.name = "MemePull"; }
            pullFx.sprite = sp; pullFx.gameObject.SetActive(true); pullFx.enabled = true;
            pullRot += Time.deltaTime * (200 + 600 * k);
            float w = MemeSize * (0.6f + 0.6f * k) * World.U / Mathf.Max(0.001f, sp.bounds.size.x);
            pullFx.transform.position = World.ToUnity(c.x, c.y, 40);
            pullFx.transform.localScale = new Vector3(w * (Mathf.Sin(pullRot * 0.05f) > 0 ? 1 : -1), w, 1);
            pullFx.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(pullRot * 0.02f) * 8);
            pullFx.sortingOrder = World.SortOrder(c.y) + 45;
        }
        void HidePull() { if (pullFx) pullFx.gameObject.SetActive(false); }

        void ClearMemeFx()
        {
            foreach (var b in rushers) if (b.r) Destroy(b.r.gameObject);
            rushers.Clear(); marked.Clear(); memeBuffT = 0; HidePull();
        }

#if UNITY_EDITOR
        [ContextMenu("Fill Meme Sprites")]
        void FillMemeSprites()
        {
            memeSprites.Clear(); memeMap = null;
            const string dir = "Assets/Art/Rats/BossMeme";
            if (!UnityEditor.AssetDatabase.IsValidFolder(dir)) return;
            foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { dir }))
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g); var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s) memeSprites.Add(new NKK.FxManager.NamedSprite { name = System.IO.Path.GetFileNameWithoutExtension(p), sprite = s });
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
