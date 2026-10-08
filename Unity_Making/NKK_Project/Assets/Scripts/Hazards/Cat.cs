using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using UnityEngine;

namespace NKK.Hazards
{
    // 연구소 고양이 (웹게임 hazards.js 이식). 쥐를 쫓다 덮치고, 4~6초마다 품종 스킬.
    // + 무리 스킬 (고양이 테이블 crowd_skill, 고양이마다 다름): 쥐가 가장 많이 모인 곳을 찾아 바닥 경고 원 → 웅크림(Aim) → 발동 → 범위 안 쥐 기절.
    //   내려찍기형(Slam·Combo·Pinpoint·Belly·Quake) = 달려가서 도약(Leap) · Roll = 굴러서 지나감 · Blink = 사라졌다 무리 한가운데 · Laser·Meteor·Vortex = 제자리 원거리.
    // 체력이 0 이 될 때까지 안 사라짐 (예전 life_time 퇴장 없음). 체력 0 → 날아가서 통통 → 삐져서 도망. 위치는 게임 단위.
    public class Cat : MonoBehaviour
    {
        public enum CState { Prowl, Pounce, Roll, Flung, Leave, Aim, Leap }

        public RatRig rig;
        [Tooltip("접지 그림자 (자식)")] public SpriteRenderer shadow;

        [Header("상태 (실행 중 확인용)")]
        public string codeId;
        public CState State = CState.Prowl;
        public float x, y, z, vx, vy, vz, hp, hpMax, life;

        public CatCharacterRow Data { get; private set; }
        public CatSkillRow Skill { get; private set; }
        public bool Gone => State == CState.Leave && life <= 0;
        public bool Alive => State != CState.Flung && State != CState.Leave && alpha >= 0.8f;
        public float R => mgr.catRadius;

        CatManager mgr;
        int face = 1, bounces;
        float t, cd, skillT, castT, pounceT, rollT, walk, rot, alpha, jit, hissUntil, value;
        bool critNext, doubleNext, slamming;
        float slamT, aimT, leapT, scanT, tx, ty, sx0, sy0; int crowdN, comboLeft;
        bool crowdRoll;
        public CatSkillRow Crowd { get; private set; }
        CatEffectType crowdFx;
        readonly List<Vector2> targets = new();
        readonly List<(float t, float rad)> quake = new();
        float qx, qy;
        float CV(int i) => Crowd == null ? 0 : i switch { 1 => Crowd.value_01, 2 => Crowd.value_02, 3 => Crowd.value_03, 4 => Crowd.value_04, 5 => Crowd.value_05, 6 => Crowd.value_06, _ => 0 };
        float CRad => CV(1) * (0.85f + 0.15f * Data.size_mul);
        float CWind => Mathf.Max(0.15f, CV(3));
        bool Ranged => crowdFx == CatEffectType.Crowd_Laser || crowdFx == CatEffectType.Crowd_Meteor || crowdFx == CatEffectType.Crowd_Vortex;
        bool LeapType => crowdFx == CatEffectType.Crowd_Slam || crowdFx == CatEffectType.Crowd_Combo || crowdFx == CatEffectType.Crowd_Pinpoint || crowdFx == CatEffectType.Crowd_Belly || crowdFx == CatEffectType.Crowd_Quake;
        float Air => (crowdFx == CatEffectType.Crowd_Slam || crowdFx == CatEffectType.Crowd_Pinpoint || crowdFx == CatEffectType.Crowd_Belly) && CV(4) > 0 ? CV(4) : mgr.slamAir;
        // 경고 원 반경 (지진은 마지막 고리까지)
        float WarnRad => crowdFx == CatEffectType.Crowd_Quake ? CRad + CV(5) * Mathf.Max(0, CV(4) - 1) : CRad;
        FxManager.HpBar bar;

        public void Init(CatManager m, CatCharacterRow row, CatSkillRow skill, RatArtLibrary.Entry art, float px, float py, float hpValue, float cheese)
        {
            mgr = m; Data = row; Skill = skill; codeId = row.code_id; name = $"Cat_{row.code_id}";
            x = px; y = py; hpMax = hp = hpValue; value = cheese;
            life = row.life_time; cd = 1.5f; skillT = Random.Range(2.5f, 4f); slamT = m.slamFirst;
            GameDatabase.Instance.CatSkills.TryGetValue(row.crowd_skill, out var cs); Crowd = cs; crowdFx = cs != null ? cs.Effect : CatEffectType.None;
            rig.Build(art, m.catLength * row.size_mul, row.code_id == "chonk" ? 0.72f : 1, false);      // 가까운 다리는 몸통 앞 (입체)
            if (shadow && m.Rats.shadowRoot) { shadow.transform.SetParent(m.Rats.shadowRoot, true); shadow.sortingOrder = m.Rats.shadowSortOrder; }
        }

        void OnDestroy()
        {
            if (FxManager.I) FxManager.I.ReleaseHpBar(bar);
            if (shadow && shadow.transform.parent != transform) Destroy(shadow.gameObject);
        }

        float V(int i) => Skill == null ? 0 : i switch { 1 => Skill.value_01, 2 => Skill.value_02, 3 => Skill.value_03, 4 => Skill.value_04, _ => 0 };

        // ── 피해 ──
        public bool Damage(float dmg, float ang, Rat by = null)
        {
            if (!Alive) return false;
            hp -= dmg * CommonSkill.BossDmgMul; jit = 3;          // 보스 사냥꾼
            if (hp <= 0) { if (by) mgr.Rats.Ults?.Charge(by, CondType.Defeat_Cat); Fling(ang); return true; }
            if (Random.value < 0.25f) FxManager.I?.Popup(x, y, RandomOf("냥!", "캬악!", "냐?!", "하악!"), Color.white, 18, 0.6f, 60);
            return true;
        }

        void Fling(float a)
        {
            mgr.HideWarns(); mgr.ShowAlert(this, false); quake.Clear();
            hp = 0; State = CState.Flung; vx = Mathf.Cos(a) * 560; vy = Mathf.Sin(a) * 560; vz = 760; bounces = 0; life = 4;
            foreach (var r in mgr.Rats.Rats) r.flee = 0;
            mgr.Game.OnSmash(value, 3);
            var fx = FxManager.I;
            if (fx) { fx.Stars(x, y, 40, 12, Color.white, new Color(0.94f, 0.78f, 0.47f)); fx.Coin(x, y, 5); fx.Shake(0.2f); }
            mgr.Game.ShowBanner("고양이 날려버림!", $"{Data.character_name} 퇴치 · 쥐의 힘을 보여줬다");
        }

        // ── 매 프레임 ──
        public void Tick(float dt)
        {
            float px = x, py = y;
            UpdateQuake(dt);
            t += dt; life -= dt; cd -= dt; alpha = Mathf.Min(State == CState.Aim && crowdFx == CatEffectType.Crowd_Blink ? Mathf.Max(0.15f, 1 - aimT / CWind) : 1, alpha + dt * 3); jit = Mathf.Max(0, jit - dt * 12); castT = Mathf.Max(0, castT - dt);
            switch (State)
            {
                case CState.Flung:
                {
                    // 날아감: 빙글빙글 → 땅에 통통 2번 → 도망
                    vz -= 1500 * dt; z += vz * dt; rot += dt * 14; x += vx * dt; y += vy * dt;
                    mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 0.5f);
                    if (z <= 0 && vz < 0)
                    {
                        z = 0; FxManager.I?.Dust(x, y, 6, 1.2f);
                        if (++bounces <= 2) { vz = 420; vx *= 0.6f; vy *= 0.6f; }
                        else { State = CState.Leave; life = 1.6f; rot = 0; float a = Random.Range(0, Mathf.PI * 2); vx = Mathf.Cos(a) * 420; vy = Mathf.Sin(a) * 420; FxManager.I?.Popup(x, y, "(삐짐)", Color.white, 16, 1, 60); }
                    }
                    return;
                }
                case CState.Leave:
                    alpha = Mathf.Min(alpha, life / 1.6f); x += vx * dt; y += vy * dt; walk += dt * 24;
                    return;
                case CState.Roll:
                {
                    // 먼치킨 식빵 굴리기: 몸 말고 직선 돌진, 닿는 쥐 나뒹굴기
                    rollT -= dt; rot += dt * 16 * face;
                    x += vx * dt; y += vy * dt;
                    if (mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 1)) FxManager.I?.Shake(0.05f);
                    float pathR = crowdRoll ? CV(4) : R + 6, pathStun = crowdRoll ? CV(2) * 0.6f : V(3);
                    foreach (var o in mgr.Rats.Rats)
                        if (o.stun <= 0 && !o.UltOn && Dist(o.x, o.y) < pathR + o.Radius) o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x), 480, 360, pathStun);
                    if (crowdRoll) { mgr.ShowWarn(0, tx, ty, CRad, 1); if (Random.value < dt * 20) FxManager.I?.Dust(x, y, 2, 0.8f); }
                    if (rollT <= 0) { State = CState.Prowl; rot = 0; vx *= 0.2f; vy *= 0.2f; cd = 0.8f; if (crowdRoll) { crowdRoll = false; CrowdHit(x, y); } }
                    break;
                }
                case CState.Aim:
                {
                    // 웅크림: 경고 원이 차오름 → 다 차면 발동 (고양이마다 다름)
                    aimT += dt; vx *= 0.8f; vy *= 0.8f; jit = Ranged ? 0.8f : 1.5f;
                    if (targets.Count > 0) face = targets[0].x >= x ? 1 : -1;
                    float k = Mathf.Clamp01(aimT / CWind);
                    for (int n = 0; n < targets.Count; n++) mgr.ShowWarn(n, targets[n].x, targets[n].y, WarnRad, k);
                    mgr.ShowAlert(this, crowdFx != CatEffectType.Crowd_Blink || k < 0.5f);
                    if (crowdFx == CatEffectType.Crowd_Vortex) Pull(dt, k);
                    if (k >= 1) Release();
                    x += vx * dt; y += vy * dt;
                    mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 0.5f);
                    break;
                }
                case CState.Leap:
                {
                    leapT += dt;
                    float k = Mathf.Clamp01(leapT / Air);
                    x = Mathf.Lerp(sx0, tx, k); y = Mathf.Lerp(sy0, ty, k);
                    vz -= 1600 * dt; z = Mathf.Max(0, z + vz * dt);
                    mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 0);
                    mgr.ShowWarn(0, tx, ty, WarnRad, 1);
                    if (k >= 1) { z = 0; vz = 0; CrowdHit(x, y); }
                    break;
                }
                default:
                {
                    // 사냥: 가까운 쥐 쪽으로 살금살금 → 가까우면 달려들기 (+ 품종 스킬)
                    var r = mgr.Rats.NearestRat(x, y, 900);
                    // 무리 스킬: 때가 되면 쥐가 가장 많이 모인 곳을 찾아 (원거리는 그 자리에서, 근접은 달려가서) 웅크림
                    slamT -= dt;
                    if (Crowd != null && slamT <= 0 && State == CState.Prowl && z <= 0 && !slamming)
                    {
                        if ((scanT -= dt) <= 0)
                        {
                            scanT = 0.25f;
                            if (mgr.FindCrowd(x, y, out var at, out crowdN)) { tx = at.x; ty = at.y; }
                            else if (r) { tx = r.x; ty = r.y; crowdN = 1; } else crowdN = 0;
                            if (crowdN < mgr.crowdMin && r) { tx = r.x; ty = r.y; }
                        }
                        if (crowdN > 0)
                        {
                            float dx = tx - x, dy = ty - y, d = Mathf.Max(1, Mathf.Sqrt(dx * dx + dy * dy));
                            float reach = crowdFx == CatEffectType.Crowd_Blink ? float.MaxValue : Ranged ? mgr.castRange : crowdFx == CatEffectType.Crowd_Roll ? mgr.slamLeapRange * 1.4f : mgr.slamLeapRange;
                            if (d < reach) { BeginAim(); break; }
                            vx += (dx / d * mgr.slamChaseSpeed - vx) * Mathf.Min(1, dt * 5); vy += (dy / d * mgr.slamChaseSpeed - vy) * Mathf.Min(1, dt * 5);
                            x += vx * dt; y += vy * dt;
                            mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 0.5f);
                            break;
                        }
                    }
                    if ((skillT -= dt) <= 0 && State != CState.Pounce) { skillT = Random.Range(Skill.cond1_value_01, Skill.cond1_value_02); CastSkill(r); }
                    if (State == CState.Pounce)
                    {
                        pounceT -= dt; x += vx * dt; y += vy * dt;
                        if (pounceT <= 0)
                        {
                            bool crit = critNext; critNext = false;
                            int n = PounceHit(crit ? V(1) : mgr.pounceRadius, crit ? V(2) : mgr.pounceStun);
                            if (n > 0) { FxManager.I?.Popup(x, y, crit ? "신사의 일격!! 크리티컬!" : "냥냥펀치!!", crit ? new Color(0.95f, 0.76f, 0.31f) : new Color(0.89f, 0.6f, 0.35f), crit ? 28 : 24, 0.8f, 70); FxManager.I?.Shake(crit ? 0.25f : 0.12f); }
                            if (doubleNext && r) { doubleNext = false; float dx = r.x - x, dy = r.y - y, d = Mathf.Max(1, Mathf.Sqrt(dx * dx + dy * dy)); pounceT = 0.3f; vx = dx / d * V(2); vy = dy / d * V(2); vz = 240; FxManager.I?.Popup(x, y, "한 번 더!!", new Color(0.66f, 0.83f, 0.86f), 22, 0.6f, 90); }
                            else { State = CState.Prowl; cd = Random.Range(0.9f, 1.6f); vx *= 0.2f; vy *= 0.2f; }
                        }
                    }
                    else if (r)
                    {
                        float dx = r.x - x, dy = r.y - y, d = Mathf.Max(1, Mathf.Sqrt(dx * dx + dy * dy)), s = d < 320 ? 330 : 150;
                        vx += (dx / d * s - vx) * Mathf.Min(1, dt * 4); vy += (dy / d * s - vy) * Mathf.Min(1, dt * 4);
                        if (d < 130 && cd <= 0) { State = CState.Pounce; pounceT = 0.35f; vx = dx / d * 620; vy = dy / d * 620; vz = 260; }
                    }
                    if (z > 0 || vz > 0) { vz -= 1600 * dt; z = Mathf.Max(0, z + vz * dt); if (z <= 0) { vz = 0; if (slamming) SlamLand(); } }
                    x += vx * dt; y += vy * dt;
                    mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, px, py, 0.5f);
                    // (예전: life_time 이 다 되면 스스로 떠남 → 지금은 체력이 0 이 될 때까지 계속 방해)
                    // 겁먹은 쥐들 (하악질 중엔 범위 넓어짐)
                    float fear = mgr.fearRadius * (hissUntil > Time.time ? V(1) : 1);
                    foreach (var o in mgr.Rats.Rats) if (Dist(o.x, o.y) < fear) o.Scare(x, y, mgr.fearTime * CommonSkill.CatFearMul);
                    if (Random.value < dt * 0.5f) FxManager.I?.Popup(x, y, RandomOf("냐옹~", "냥?", "크르릉…"), Color.white, 16, 0.8f, 80);
                    break;
                }
            }
            if (Mathf.Abs(vx) > 10 && State != CState.Roll) face = vx > 0 ? 1 : -1;
            walk += dt * Mathf.Sqrt(vx * vx + vy * vy) / 12;
        }

        // ── 게임 오버 습격 (웹 updateGameOver 고양이): GameOver 가 Tick 대신 부름. 품종 스킬 없이 쫓아가 덮침 ──
        [HideInInspector] public bool raid;
        public void BeginRaid(float hopPhase) { raid = true; alpha = 1; State = CState.Prowl; t = hopPhase; }
        // 목표에 닿으면 true
        public bool RaidStep(float dt, Rat target, float spd, float reach, float pounceRange)
        {
            t += dt;
            if (!target) { vx = vy = 0; State = CState.Prowl; z = 0; return false; }
            float dx = target.x - x, dy = target.y - y, d = Mathf.Sqrt(dx * dx + dy * dy);
            face = dx >= 0 ? 1 : -1;
            State = d < pounceRange ? CState.Pounce : CState.Prowl;
            if (d < reach) { vx = vy = 0; z = 0; return true; }
            vx = dx / d * spd; vy = dy / d * spd; x += vx * dt; y += vy * dt;
            z = State == CState.Pounce ? Mathf.Abs(Mathf.Sin(t * 8)) * 40 : 0;
            walk += dt * 14;
            return false;
        }

        float Dist(float ox, float oy) => Mathf.Sqrt((ox - x) * (ox - x) + (oy - y) * (oy - y));

        int PounceHit(float rad, float stunT)
        {
            int n = 0;
            foreach (var o in mgr.Rats.Rats) if (Dist(o.x, o.y) < rad) { o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x), 460, 380, stunT); n++; }
            return n;
        }

        // ── 무리 스킬 ──
        void BeginAim()
        {
            State = CState.Aim; aimT = 0; vx = vy = 0;
            targets.Clear(); targets.Add(new Vector2(tx, ty));
            int extra = crowdFx == CatEffectType.Crowd_Laser || crowdFx == CatEffectType.Crowd_Meteor ? Mathf.Max(1, Mathf.RoundToInt(CV(4))) - 1 : 0;
            for (int n = 0; n < extra; n++) { if (mgr.FindCrowd(x, y, out var at, out _, targets, mgr.castRange)) targets.Add(at); else break; }
            var call = mgr.SlamCall(); if (!string.IsNullOrEmpty(call)) FxManager.I?.Popup(x, y, call, new Color(1f, 0.85f, 0.75f), 22, 1, 110);
            if (crowdFx == CatEffectType.Crowd_Blink) FxManager.I?.Dust(x, y, 10, 1.4f);
        }

        // 웅크림이 끝남 → 발동
        void Release()
        {
            mgr.ShowAlert(this, false);
#if UNITY_EDITOR
            if (mgr.logCrowd) Debug.Log($"[Cat] {Data.character_name} · {Crowd.skill_name} 발동 (무리 {crowdN}마리, 노리는 곳 {targets.Count})");
#endif
            var fx = FxManager.I;
            switch (crowdFx)
            {
                case CatEffectType.Crowd_Blink:
                    fx?.Anim("poof", x, y, 0, 1.4f);
                    x = tx; y = ty; mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, tx, ty, 0); alpha = 1;
                    fx?.Anim("poof", x, y, 0, 1.6f);
                    CrowdHit(x, y);
                    return;
                case CatEffectType.Crowd_Roll:
                {
                    float dx = tx - x, dy = ty - y, d = Mathf.Max(1, Mathf.Sqrt(dx * dx + dy * dy)), sp = Mathf.Max(200, CV(5));
                    State = CState.Roll; crowdRoll = true; rollT = d / sp; vx = dx / d * sp; vy = dy / d * sp; face = dx >= 0 ? 1 : -1;
                    return;
                }
                case CatEffectType.Crowd_Laser:
                case CatEffectType.Crowd_Meteor:
                    foreach (var tg in targets) mgr.AddStrike(crowdFx == CatEffectType.Crowd_Meteor, tg.x, tg.y, CRad, CV(2), Crowd.skill_name);
                    mgr.HideWarns();
                    EndCrowd();
                    return;
                case CatEffectType.Crowd_Vortex:
                {
                    int n = 0;
                    foreach (var o in mgr.Rats.Rats) if (!o.UltOn && Vector2.Distance(new Vector2(o.x, o.y), new Vector2(tx, ty)) < CRad) { o.vz = Random.Range(500f, 750f); o.Stun(CV(2)); n++; }
                    mgr.HitFx(tx, ty, CRad, Crowd.skill_name, n, 0.3f);
                    fx?.Stars(tx, ty, 80, 24, new Color(0.73f, 0.64f, 0.89f), Color.white, 150, 420);
                    mgr.HideWarns();
                    EndCrowd();
                    return;
                }
                default:
                    // 내려찍기형: 도약
                    State = CState.Leap; leapT = 0; sx0 = x; sy0 = y; vz = 1600 * Air / 2; z = 0;
                    mgr.HideWarns(1);
                    return;
            }
        }

        // 블랙홀: 범위 안 쥐를 가운데로 끌어당김 (가운데에 가까울수록 약하게)
        void Pull(float dt, float k)
        {
            mgr.ShowVortex(tx, ty, CRad, k);
            float sp = CV(4) * k;
            foreach (var o in mgr.Rats.Rats)
            {
                if (o.UltOn) continue;
                float dx = tx - o.x, dy = ty - o.y, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > CRad * 1.15f || d < 12) continue;
                float m = Mathf.Min(sp * dt, d - 10) / d; o.x += dx * m; o.y += dy * m;
            }
        }

        // 범위 기절 (착지·도착 자리)
        void CrowdHit(float hx, float hy)
        {
            float R0 = CRad;
            int n = mgr.StunArea(hx, hy, R0, CV(2));
            bool belly = crowdFx == CatEffectType.Crowd_Belly, quakeFx = crowdFx == CatEffectType.Crowd_Quake;
            mgr.LaunchItems(hx, hy, R0, belly ? CV(5) : mgr.slamItemLaunch);
            mgr.HideWarns(); mgr.ShowClaw(hx, hy, R0, belly || quakeFx);
            mgr.HitFx(hx, hy, R0, Crowd.skill_name, n, belly ? 0.5f : 0.35f);
            if (quakeFx) { qx = hx; qy = hy; quake.Clear(); for (int i = 1; i < Mathf.RoundToInt(CV(4)); i++) quake.Add((CV(6) * i, R0 + CV(5) * i)); }
            State = CState.Prowl; cd = 0.6f; vx = vy = 0;
            if (crowdFx == CatEffectType.Crowd_Combo)
            {
                if (comboLeft <= 0) comboLeft = Mathf.Max(1, Mathf.RoundToInt(CV(4))) - 1; else comboLeft--;
                if (comboLeft > 0) { slamT = CV(5); scanT = 0; return; }
            }
            EndCrowd();
        }

        void EndCrowd() { State = CState.Prowl; comboLeft = 0; slamT = Random.Range(Crowd.cond1_value_01, Crowd.cond1_value_02); cd = 0.6f; }

        // 지진 충격파 고리 (차례로 퍼짐, 바깥 고리일수록 짧게 기절)
        void UpdateQuake(float dt)
        {
            for (int i = quake.Count - 1; i >= 0; i--)
            {
                var q = quake[i]; q.t -= dt; quake[i] = q;
                if (q.t > 0) continue;
                int n = 0;
                foreach (var o in mgr.Rats.Rats)
                {
                    if (o.UltOn || o.stun > 0) continue;
                    float d = Vector2.Distance(new Vector2(o.x, o.y), new Vector2(qx, qy));
                    if (d < q.rad) { o.Ragdoll(Mathf.Atan2(o.y - qy, o.x - qx), 300, 260, CV(2) * 0.7f); n++; }
                }
                var fx = FxManager.I; if (fx) { fx.Ring(qx, qy, q.rad, new Color(0.62f, 0.45f, 0.33f), 0.45f); fx.Dust(qx + Random.Range(-q.rad, q.rad) * 0.6f, qy, 6, 1.4f); fx.Shake(0.15f); }
                quake.RemoveAt(i);
            }
        }

        void SlamLand()
        {
            slamming = false;
            PounceHit(V(1), V(2));
            foreach (var it in mgr.Items.InRange(x, y, V(1) + 40))
                if (it.State == Items.Item.ItemState.Rest && Dist(it.x, it.y) < V(1)) it.Launch(Mathf.Atan2(it.y - y, it.x - x), V(3), false);
            var fx = FxManager.I; if (fx) { fx.Ring(x, y, V(1), new Color(0.89f, 0.6f, 0.35f), 0.5f); fx.Dust(x, y, 10, 1.6f); fx.Anim("poof", x, y, 0, 1.2f); fx.Shake(0.25f); }
        }

        // ── 품종 스킬 (고양이 테이블 effect_type + value_01~04) ──
        void CastSkill(Rat r)
        {
            if (Skill == null) return;
            castT = 0.5f;
            var fx = FxManager.I;
            fx?.Popup(x, y, Skill.skill_name + "!", Data.Category == CatCategory.Special ? new Color(0.8f, 0.71f, 0.86f) : new Color(1, 0.95f, 0.75f), 20, 1, 100);
            switch (Skill.Effect)
            {
                case CatEffectType.Hiss_Fear: hissUntil = Time.time + V(2); fx?.Ring(x, y, mgr.fearRadius * V(1), new Color(0.91f, 0.47f, 0.42f, 0.7f), 0.6f); break;
                case CatEffectType.Double_Pounce: doubleNext = true; cd = 0; break;
                case CatEffectType.Crit_Pounce: critNext = true; cd = 0; break;
                case CatEffectType.Jump_Press: slamming = true; vz = 700; if (r) { vx = (r.x - x) * 0.9f; vy = (r.y - y) * 0.9f; } break;
                case CatEffectType.Roll_Charge:
                    if (r) { float a = Mathf.Atan2(r.y - y, r.x - x); State = CState.Roll; rollT = V(1); vx = Mathf.Cos(a) * V(2); vy = Mathf.Sin(a) * V(2); face = Mathf.Cos(a) >= 0 ? 1 : -1; }
                    break;
                case CatEffectType.Crowd_Teleport:
                {
                    // 쥐가 제일 많은 곳 옆으로 순간이동 → 바로 덮치기
                    Rat best = r; int bn = 0;
                    foreach (var o in mgr.Rats.Rats) { int n = 0; foreach (var q in mgr.Rats.Rats) if (Vector2.Distance(new Vector2(q.x, q.y), new Vector2(o.x, o.y)) < V(1)) n++; if (n > bn) { bn = n; best = o; } }
                    if (best) { fx?.Dust(x, y, 8, 1); x = best.x + Random.Range(-60f, 60f); y = best.y + Random.Range(-40f, 40f); float ox = x, oy = y; mgr.Stage.Confine(ref x, ref y, ref vx, ref vy, R, best.x, best.y, 0); fx?.Dust(x, y, 8, 1); cd = 0; }
                    break;
                }
                case CatEffectType.Laser_Stun:
                    if (r)
                    {
                        float a = Mathf.Atan2(r.y - y, r.x - x), L = V(1), ux = Mathf.Cos(a), uy = Mathf.Sin(a);
                        foreach (var o in mgr.Rats.Rats) { float px = o.x - x, py = o.y - y, al = px * ux + py * uy; if (al > 0 && al < L && Mathf.Abs(px * uy - py * ux) < V(2)) o.Stun(V(3)); }
                        mgr.Beam(x, y, x + ux * L, y + uy * L);
                    }
                    break;
                case CatEffectType.Roar_Blast:
                {
                    float R0 = V(1);
                    foreach (var o in mgr.Rats.Rats) if (Dist(o.x, o.y) < R0) o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x), V(2), 420, 1.2f);
                    foreach (var it in mgr.Items.InRange(x, y, R0 + 40)) if (it.State == Items.Item.ItemState.Rest && Dist(it.x, it.y) < R0) it.Launch(Mathf.Atan2(it.y - y, it.x - x), V(3), false);
                    if (fx) { fx.Ring(x, y, R0, new Color(0.89f, 0.6f, 0.35f), 0.5f); fx.Ring(x, y, R0 * 0.6f, Color.white, 0.35f); fx.Shake(0.3f); fx.Popup(x, y, "크아아앙!!", Color.white, 30, 0.8f, 110); }
                    break;
                }
                case CatEffectType.Fireball:
                    for (int n = 0; n < Mathf.RoundToInt(V(1)); n++) mgr.QueueFireball(this, 0.15f + n * V(2), V(3));
                    break;
                case CatEffectType.Gravity_Wave:
                    foreach (var o in mgr.Rats.Rats) if (Dist(o.x, o.y) < V(1)) { o.vz = Random.Range(V(3), V(4)); o.Stun(V(2)); }
                    fx?.Ring(x, y, V(1), new Color(0.8f, 0.71f, 0.86f), 0.6f);
                    break;
            }
        }

        // 웹게임 catPose: walk · pounce · crouch · flung · cast
        RatRig.Pose MakePose()
        {
            float tt = Time.time;
            var p = new RatRig.Pose { head = Mathf.Sin(tt * 1.6f) * 0.06f, tail = 0.1f + Mathf.Sin(tt * 3) * 0.25f, sx = 1, sy = 1 };
            if (State == CState.Pounce || State == CState.Leap) { p.front = 1.3f; p.farFront = 1.1f; p.back = -1.1f; p.farBack = -0.9f; p.tilt = -0.25f; p.tail = 1; p.head = -0.15f; }
            else if (State == CState.Roll || State == CState.Aim || (State == CState.Prowl && cd > 0 && cd < 0.4f)) { p.front = 0.3f; p.farFront = 0.3f; p.back = 0.4f; p.farBack = 0.4f; p.tilt = 0.12f; p.bob = 6; p.tail = 0.9f + Mathf.Sin(tt * 20) * 0.2f; }
            else if (State == CState.Flung) { p.front = Mathf.Sin(tt * 30) * 1.4f; p.farFront = Mathf.Cos(tt * 27) * 1.4f; p.back = Mathf.Sin(tt * 28) * 1.2f; p.farBack = Mathf.Cos(tt * 25) * 1.2f; p.tail = Mathf.Sin(tt * 20); p.head = Mathf.Sin(tt * 15) * 0.4f; }
            else if (castT > 0) { p.front = 2.1f; p.farFront = 0.5f; p.tilt = -0.3f; p.head = -0.2f; p.tail = 1.1f; }
            else if (Mathf.Sqrt(vx * vx + vy * vy) > 20) { float s = Mathf.Sin(walk); p.front = s * 0.5f; p.farBack = s * 0.45f; p.farFront = -s * 0.5f; p.back = -s * 0.45f; p.bob = -Mathf.Abs(Mathf.Cos(walk)) * 2.5f; p.tail = Mathf.Sin(walk * 0.5f) * 0.3f + 0.1f; }
            return p;
        }

        void LateUpdate()
        {
            if (!mgr) return;
            float jx = jit > 0 ? Random.Range(-jit, jit) : 0;
            transform.position = World.ToUnity(x + jx, y, z);
            transform.rotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            rig.Apply(MakePose(), 1, face, 1, World.SortOrder(y));
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) { var c = sr.color; c.a = alpha; sr.color = c; }
            if (shadow)
            {
                float w = R * 1.6f * (1 - Mathf.Min(0.7f, z / 500)), sw = shadow.sprite ? shadow.sprite.bounds.size.x : 1;
                shadow.transform.position = World.ToUnity(x, y);
                shadow.transform.localScale = new Vector3(w * 2 * World.U / sw, w * 0.6f * 2 * World.TILT * World.U / sw, 1);
                var c = shadow.color; c.a = 0.35f * alpha; shadow.color = c;
            }
            var fx = FxManager.I;
            if (fx)
            {
                bool show = Alive && !raid;
                if (show) { bar ??= fx.GetHpBar(); fx.ShowHpBar(bar, x, y, z + 92, 90, Mathf.Clamp01(hp / hpMax), 1); }
                else if (bar != null) { fx.ReleaseHpBar(bar); bar = null; }
            }
        }

        static string RandomOf(params string[] a) => a[Random.Range(0, a.Length)];
    }
}
