using NKK.Data;
using NKK.Items;
using NKK.Stage;
using UnityEngine;

namespace NKK.Rats
{
    // 새 쥐 6종 (2026-10-12, 캐릭터 상세 기획서): 패시브·액션. 수치 = 쥐 테이블 Skill 시트 value_01~06 (뜻은 Effect_Type 시트)
    //   폭탄마 쥐     Ult_Power(필살기 위력 ×) · 액션 없음 (필살기 익스플로전 = UltExplosion)
    //   쥐4 (쥐포)    Grill_Stack(갉을수록 공격력 +) · Savory_Smell(주변 물건 받는 피해 증가)
    //   고쳐쓰쥐      Part_Collect(부숴서 부품) · Tune_Up(부품이 모이면 가장 센 동료 강화)
    //   제국군 쥐     Aim_Assist(명중률 70%, 빗나갈수록 +) · Blaster_Shot(블래스터 3발)
    //   폭탄이쥐      Burning_Fuse(심지 = 꼬리가 타들어감) · Self_Destruct(가장 가까운 벽으로 돌진해 자폭, 쳇바퀴 훈련 없이 바로)
    //   아이스 퀸 쥐  Frost_Stack(생명체를 때릴 때마다 빙결 스택 → 느려짐, 최대면 꽁꽁) · Ice_Missile(고드름 미사일 유도) · 필살기 렛잇고 = UltRatItGo
    //   5G           Rush_Range_Up(총공격 집결 범위 +, 여러 마리여도 한 번) · Data_Boost(동료가 모이면 데이터 전송 = 주변 쥐 공격력 +)
    //   맘스땃쥐      Patty_Stack(부술 때마다 패티, 몸이 두꺼워짐) · Patty_Slam(패티가 꽉 차면 빅버거 박치기)
    // 팝업 글·그림은 RatManager 인스펙터 '새 쥐 6종'
    public partial class Rat
    {
        float grill, grillT;              // 쥐포: 노릇노릇 단계 · 식기 전까지 남은 시간
        float parts;                      // 고쳐쓰쥐: 모은 부품
        [HideInInspector] public float tuneT, tuneAdd;     // 튜닝 받은 쥐: 남은 시간 · 공격력 +
        float aimBonus;                   // 스톰쥐퍼: 빗나간 만큼 쌓인 명중률
        float fuse = -1;                  // 쥐한폭탄: 남은 심지 (초, -1 = 아직 안 붙임)
        int patty;                        // 맘스땃쥐: 쌓인 패티
        Rat tuneTgt; int blastHits; Item nrTgt;
        [HideInInspector] public float dataT, dataAdd;     // 5G 데이터 전송 받은 쥐: 남은 시간 · 공격력 +
        object lastFoe, iceTgt; float lastFoeT;           // 마지막으로 때린 생명체 (얼음 미사일 목표)
        float nrX0, nrY0, nrX1, nrY1; Vector2Int sdRoom, sdWall; bool sdHasWall;

        float NewRatDamageAdd => (Is(EffectType.Grill_Stack) ? grill * PV(1) : 0) + (Is(EffectType.Patty_Stack) ? patty * PV(3) : 0) + (tuneT > 0 ? tuneAdd : 0) + (dataT > 0 ? dataAdd : 0);
        public float RushRangeAdd => Is(EffectType.Rush_Range_Up) && Passive != null ? PV(1) : 0;
        int PattyMax => AugmentSpecials.PattyMax(this, Mathf.RoundToInt(PV(1)));         // 증강 '맘스땃쥐 생쥐': 최대 8장

        // 생명체(고양이 · 보스 · 사람)를 때림 (그쪽 Damage 에서 부름): 액션 목표로 기억 + 아이스 퀸 빙결 스택
        public void OnHitCreature(object who, Frost f, float fx, float fy, float h)
        {
            lastFoe = who; lastFoeT = Time.time;
            if (Is(EffectType.Frost_Stack) && f != null && temp <= 0) f.Add(1, PV(1), Mathf.RoundToInt(PV(2)), PV(3), PV(4), fx, fy, h, Manager.freezePopup);
        }

        // 얼음 미사일 목표: 방금 때리던 생명체 → 가까운 생명체 → 가까운 물건
        object IceTarget(float range)
        {
            if (lastFoe != null && Time.time - lastFoeT < 6 && RatManager.FoePos(lastFoe, out _, out _)) return lastFoe;
            object best = null; float bd = range;
            void Try(object o) { if (RatManager.FoePos(o, out var p, out _)) { float d = Vector2.Distance(p, new Vector2(x, y)); if (d < bd) { bd = d; best = o; } } }
            if (Manager.Cats) Try(Manager.Cats.Current);
            Try(NKK.Hazards.Boss.Current);
            foreach (var h in Manager.Items.Humans) Try(h);
            return best ?? NearestItem(range);
        }
        public float UltPowerMul => Is(EffectType.Ult_Power) && PV(1) > 0 ? PV(1) : 1;
        public float UltSelfStun => Is(EffectType.Ult_Power) ? PV(2) : 0;      // 폭렬쥐: 필살기 뒤 반동 기절 (초)
        // 심지 길이 (꼬리 가로 배율): 다 타면 짧은 꼬투리만
        float FuseLeft => Is(EffectType.Burning_Fuse) && fuse >= 0 && PV(1) > 0 ? Mathf.Lerp(0.15f, 1, fuse / PV(1)) : 1;
        bool ActAlwaysOpen => ActionType == EffectType.Self_Destruct;          // 자폭은 바로 해금 (기획서)
        bool NewActReady => ActionType switch
        {
            EffectType.Tune_Up => parts >= Mathf.Max(1, PV(2)),
            EffectType.Patty_Slam => patty >= Mathf.Max(1, PattyMax),
            EffectType.Self_Destruct => fuse == 0,
            _ => true,
        };

        void NewPopup(string s, float px, float py, Color c, float size = 18, float life = 0.9f, float pz = 50, object n = null)
        {
            if (string.IsNullOrEmpty(s) || !OnScreen()) return;
            FxManager.I?.Popup(px, py, n != null ? s.Replace("{n}", n.ToString()) : s, c, size, life, pz);
        }

        // 물건을 부쉈을 때 (Rat.Passive OnSmashedItem 에서)
        void NewRatOnSmash()
        {
            if (temp > 0) return;
            if (Is(EffectType.Part_Collect) && parts < PV(2)) { parts = Mathf.Min(PV(2), parts + PV(1)); if (parts >= PV(2)) FxManager.I?.Stars(x, y, 30, 5, new Color(0.8f, 0.82f, 0.86f), Color.white, 60, 160); }
            if (Is(EffectType.Patty_Stack) && patty < PattyMax)
            {
                patty++; sq = 0.7f;
                NewPopup(Manager.pattyPopup, x, y, new Color(0.62f, 0.38f, 0.2f), 16, 0.8f, 46, patty);
            }
        }

        // 쥐포: 갉을 때마다 노릇노릇 +1 (최대 밸류_02), 밸류_03 초 동안 안 갉으면 식음
        void GrillOnHit() { grill = Mathf.Min(PV(2), grill + 1); grillT = PV(3); }

        void NewRatTick(float dt)
        {
            if (dataT > 0) { dataT -= dt; if (Random.value < dt * 3 && OnScreen()) FxManager.I?.Stars(x, y, 34, 1, new Color(0.45f, 0.9f, 1f), Color.white, 30, 90); }
            if (tuneT > 0) { tuneT -= dt; if (Random.value < dt * 3 && OnScreen()) FxManager.I?.Stars(x, y, 30, 1, new Color(1, 0.86f, 0.4f), Color.white, 30, 90); }
            if (Is(EffectType.Grill_Stack) && grill > 0)
            {
                if ((grillT -= dt) <= 0) grill = Mathf.Max(0, grill - dt * PV(2) / 1.5f);
                if (grill > PV(2) * 0.5f && Random.value < dt * 4 && OnScreen()) FxManager.I?.Burst(x, y, 30, 1, new Color(0.93f, 0.88f, 0.78f, 0.8f), Color.white, 20, 60, 4, 8);   // 지글지글 김
            }
            if (Is(EffectType.Burning_Fuse) && temp <= 0)
            {
                if (fuse < 0) fuse = PV(1);
                if (fuse > 0)
                {
                    fuse = Mathf.Max(0, fuse - dt);
                    // 꼬리 끝 불꽃 (얼마 안 남으면 더 자주)
                    float rate = fuse < 5 ? 14 : 4;
                    if (Random.value < dt * rate && OnScreen())
                    {
                        float len = Manager.RigLength(Data) * GradeData.size * Manager.ratScale * (rig.IsSingle ? 0.5f : 0.9f * FuseLeft);
                        FxManager.I?.Spark(x - face * len, y, 14, new Color(1, 0.75f, 0.3f), 30, 0.15f);
                    }
                    if (fuse <= 5 && fuse + dt > 5) NewPopup(Manager.fusePopup, x, y, new Color(1, 0.6f, 0.3f), 16, 1, 44);
                }
                if (fuse == 0 && !act.on && stun <= 0 && Trick == TrickType.None && Action != null && ActionType == EffectType.Self_Destruct) StartAction();
            }
        }

        // 한 장짜리 그림 (컴퓨터 마우스 쥐처럼): 쥐한폭탄 = 남은 심지에 따라 그림 · 쥐포 = 노릇노릇 절반 넘으면 구운 그림
        Sprite baseSingle;
        void NewRatSprite()
        {
            if (!rig || !rig.IsSingle) return;
            if (!baseSingle) baseSingle = rig.single.sprite;
            Sprite want = baseSingle;
            var fs = Manager.fuseSprites;
            if (Is(EffectType.Burning_Fuse) && fs != null && fs.Length > 0 && fuse >= 0 && PV(1) > 0)
                want = fs[Mathf.Clamp(Mathf.FloorToInt((1 - fuse / PV(1)) * fs.Length), 0, fs.Length - 1)];
            if (Is(EffectType.Grill_Stack) && Manager.grilledSprite && grill > PV(2) * 0.5f) want = Manager.grilledSprite;
            var bs = Manager.burgerStack;
            if (Is(EffectType.Patty_Stack) && bs != null && bs.Length > 0) want = bs[Mathf.Clamp(patty, 0, bs.Length - 1)];
            if (want && rig.single.sprite != want)
            {
                rig.single.sprite = want;
                rig.single.transform.localPosition = new Vector3(0, want.rect.height * 0.5f / 100f, 0);     // 키가 달라져도 발끝은 바닥에
            }
        }

        // 맘스땃쥐: 패티만큼 두꺼워짐 (층 쌓기 그림이 있으면 그림이 높아지므로 늘리지 않음)
        void NewRatTransform(ref float scale, ref float sy)
        {
            if (!Is(EffectType.Patty_Stack) || patty <= 0) return;
            var bs = Manager.burgerStack;
            if (rig && rig.IsSingle && bs != null && bs.Length > 0) { if (patty > bs.Length - 1) sy *= 1 + (patty - (bs.Length - 1)) * 0.14f; return; }      // 그림보다 많이 쌓이면 (맘스땃쥐 생쥐 8장) 위로 늘림
            scale *= 1 + patty * PV(2) * 0.4f; sy *= 1 + patty * PV(2);
        }

        void NewActionBegin()
        {
            var fx = FxManager.I;
            switch (ActionType)
            {
                case EffectType.Tune_Up:
                {
                    // 반경 안에서 공격력이 가장 센 동료 (없으면 자기)
                    tuneTgt = this; float best = -1, R0 = AV(2) > 0 ? AV(2) : 600;
                    foreach (var o in Manager.Rats)
                    {
                        if (o == this || o.temp > 0 || o.UltOn || Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) > R0) continue;
                        float d = o.Damage; if (d > best) { best = d; tuneTgt = o; }
                    }
                    parts = 0;
                    break;
                }
                case EffectType.Blaster_Shot:
                    nrTgt = NearestItem(AV(4) > 0 ? AV(4) : 600); blastHits = 0;
                    break;
                case EffectType.Self_Destruct:
                {
                    // 지금 방의 막힌 벽 중 가장 가까운 쪽 (없으면 가장 가까운 물건)
                    sdRoom = StageManager.RoomOf(x, y); sdHasWall = false;
                    float cx = (sdRoom.x + 0.5f) * World.RW, cy = (sdRoom.y + 0.5f) * World.RH, bd = float.MaxValue;
                    nrX1 = x; nrY1 = y;
                    foreach (var d in Manager.Stage.ClosedSides(sdRoom.x, sdRoom.y))
                    {
                        float wx = cx + d.x * (World.RW * 0.5f - Radius - 6), wy = cy + d.y * (World.RH * 0.5f - Radius - 6);
                        wx = d.x != 0 ? wx : Mathf.Clamp(x, cx - World.RW * 0.3f, cx + World.RW * 0.3f);
                        wy = d.y != 0 ? wy : Mathf.Clamp(y, cy - World.RH * 0.3f, cy + World.RH * 0.3f);
                        float dd = Vector2.Distance(new Vector2(wx, wy), new Vector2(x, y));
                        if (dd < bd) { bd = dd; nrX1 = wx; nrY1 = wy; sdWall = d; sdHasWall = true; }
                    }
                    if (!sdHasWall) { var it = NearestItem(700); if (it) { nrX1 = it.x; nrY1 = it.y; } }
                    break;
                }
                case EffectType.Ice_Missile:
                    iceTgt = IceTarget(AV(4) > 0 ? AV(4) : 700);
                    if (RatManager.FoePos(iceTgt, out var ip, out _)) face = ip.x > x ? 1 : -1;
                    break;
                case EffectType.Patty_Slam:
                {
                    var it = NearestItem(AV(4) > 0 ? AV(4) : 420);
                    nrX0 = x; nrY0 = y; nrX1 = it ? it.x : x + face * 120; nrY1 = it ? it.y : y; nrTgt = it;
                    if (it) face = it.x > x ? 1 : -1;
                    break;
                }
            }
        }

        // 피해 계수 = 테이블 값 × atk (= 공격력 × 스킬 위력 × 성장 위력)
        void NewActionStep(float dt, float k, float atk)
        {
            var fx = FxManager.I; var items = Manager.Items;
            switch (ActionType)
            {
                case EffectType.Savory_Smell:
                {
                    // 킁킁 → 고소한 냄새가 퍼짐: 반경(밸류_02) 안 물건이 밸류_03 초 동안 받는 피해 ×(1 + 밸류_01)
                    vx = vy = 0;
                    var smell = new Color(0.93f, 0.85f, 0.68f, 0.85f);
                    if (fx && OnScreen() && Random.value < dt * 10) fx.Burst(x + Random.Range(-20f, 20f), y, 26, 1, smell, Color.white, 20, 70, 6, 12);
                    if (!act.done && act.t >= act.dur * 0.35f)
                    {
                        act.done = true;
                        float R0 = AV(2);
                        foreach (var it in items.InRange(x, y, R0 + 40))
                            if (it.State == Item.ItemState.Rest && Vector2.Distance(new Vector2(it.x, it.y), new Vector2(x, y)) < R0) { it.tenderT = Time.time + AV(3); it.tenderMul = 1 + AV(1); }
                        if (fx)
                        {
                            fx.Ring(x, y, R0, smell, 0.6f); fx.Ring(x, y, R0 * 0.6f, smell, 0.45f);
                            if (fx.AddSticker("smell", x, y, 40, 120, 1.4f) == null) fx.Burst(x, y, 30, 10, smell, Color.white, 60, 200, 6, 12);
                        }
                        NewPopup(Manager.smellPopup, x, y, new Color(0.95f, 0.78f, 0.45f), 18, 1.1f);
                    }
                    break;
                }
                case EffectType.Tune_Up:
                {
                    // 뚝딱뚝딱 → 가장 센 동료에게 튜닝: 밸류_03 초 동안 공격력 +밸류_01
                    vx = vy = 0;
                    if (act.hitT <= 0) { act.hitT = 0.18f; bite = 1; if (fx && OnScreen()) fx.Spark(x + face * 18, y, 16, new Color(1, 0.85f, 0.4f), 26, 0.12f); }
                    if (!act.done && act.t >= act.dur * 0.6f)
                    {
                        act.done = true;
                        var o = tuneTgt ? tuneTgt : this;
                        o.tuneT = AV(3); o.tuneAdd = AV(1);
                        if (fx)
                        {
                            if (o != this) fx.Beam(x, y, 20, o.x, o.y, 20, new Color(1, 0.86f, 0.4f), 0.3f);
                            fx.Ring(o.x, o.y, 60, new Color(1, 0.86f, 0.4f), 0.5f); fx.Stars(o.x, o.y, 30, 10, new Color(1, 0.86f, 0.4f), Color.white, 80, 240);
                        }
                        NewPopup(Manager.tunePopup, o.x, o.y, new Color(1, 0.86f, 0.4f), 18, 1.1f, 56);
                    }
                    break;
                }
                case EffectType.Ice_Missile:
                {
                    // 쥐 주위에 고드름 밸류_01 개가 하나씩 생김 → 잠깐 떠 있다가 목표로 유도 (발당 ×밸류_02, 맞으면 빙결 스택)
                    vx = vy = 0;
                    int n = Mathf.Max(1, Mathf.RoundToInt(AV(1)));
                    if (act.n < n && act.t >= 0.15f + act.n * 0.1f)
                    {
                        if (!RatManager.FoePos(iceTgt, out _, out _)) iceTgt = IceTarget(AV(4) > 0 ? AV(4) : 700);
                        float a = Mathf.PI * (0.15f + 0.7f * act.n / Mathf.Max(1, n - 1)), r0 = 34;       // 머리 위 반원
                        float sx = x + Mathf.Cos(a) * r0 * face, sy = y + Mathf.Sin(a) * r0 * 0.5f;
                        Manager.FireIce(this, sx, sy, iceTgt, atk * AV(2), AV(5) > 0 ? AV(5) : 650, 0.55f - act.n * 0.06f);
                        if (fx && OnScreen()) fx.Burst(sx, sy, 30, 4, Frost.Ice, Frost.IceLight, 20, 80, 3, 6);
                        if (act.n == 0) { NewPopup(Manager.iceMissilePopup, x, y, Frost.Ice, 18, 1, 50); SfxManager.PlayAt("laser", x, y, 0.35f); }
                        act.n++;
                    }
                    break;
                }
                case EffectType.Data_Boost:
                {
                    // 휴대폰으로 데이터 전송: 신호가 퍼지다가 반경(밸류_02) 안 쥐 전부 밸류_03 초 동안 공격력 +밸류_01
                    vx = vy = 0;
                    var sig = new Color(0.45f, 0.9f, 1f);
                    if (act.hitT <= 0) { act.hitT = 0.25f; if (fx && OnScreen()) fx.Ring(x, y, 30 + act.t * 80, sig, 0.35f); }
                    if (!act.done && act.t >= act.dur * 0.5f)
                    {
                        act.done = true;
                        float R0 = AugmentSpecials.DataToAll(this) ? 1e6f : AV(2) > 0 ? AV(2) : 250; int shown = 0;      // 증강 '5G 생쥐': 모든 쥐에게
                        foreach (var o in Manager.Rats)
                        {
                            if (o.temp > 0 || Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) > R0) continue;
                            o.dataT = AV(3); o.dataAdd = Mathf.Max(o.dataT > 0 ? o.dataAdd : 0, AV(1));
                            if (fx && o != this && shown++ < 12 && OnScreen()) fx.Beam(x, y, 30, o.x, o.y, 20, sig, 0.3f);
                        }
                        if (fx && OnScreen()) { fx.Ring(x, y, R0, sig, 0.6f); fx.Stars(x, y, 34, 8, sig, Color.white, 60, 200); }
                        NewPopup(Manager.dataPopup, x, y, sig, 18, 1.1f, 56);
                    }
                    break;
                }
                case EffectType.Blaster_Shot:
                {
                    // 블래스터 밸류_01 발 (명중률 = 패시브 조준 보정). 빗나가면 엉뚱한 쪽으로. 다 맞으면 추가 피해 ×밸류_03
                    vx = vy = 0;
                    int n = Mathf.Max(1, Mathf.RoundToInt(AV(1)));
                    if (act.n < n && act.t >= 0.25f + act.n * 0.22f)
                    {
                        if (!nrTgt || nrTgt.State != Item.ItemState.Rest) nrTgt = NearestItem(AV(4) > 0 ? AV(4) : 600);
                        float a = nrTgt ? Mathf.Atan2(nrTgt.y - y, nrTgt.x - x) : (face > 0 ? 0 : Mathf.PI);
                        float hitP = Is(EffectType.Aim_Assist) ? Mathf.Min(1, PV(1) + aimBonus) : 0.7f;
                        bool hit = nrTgt && Random.value < hitP;
                        if (hit) { blastHits++; aimBonus = 0; }
                        else
                        {
                            a += (Random.value < 0.5f ? -1 : 1) * Random.Range(0.35f, 0.7f);
                            if (Is(EffectType.Aim_Assist)) aimBonus += PV(2);
                            NewPopup(Manager.missPopup, x, y, new Color(0.85f, 0.88f, 0.92f), 15, 0.7f, 50);
                        }
                        float sp = AV(5) > 0 ? AV(5) : 900;
                        Manager.FireBullet(this, x + Mathf.Cos(a) * 20, y + Mathf.Sin(a) * 20, a, sp, atk * AV(2), 0.7f, true, Manager.blasterBoltSprite, Manager.blasterBoltSprite ? Color.white : Manager.blasterBoltColor, true);
                        face = Mathf.Cos(a) >= 0 ? 1 : -1; bite = 1;
                        SfxManager.PlayAt("laser", x, y, 0.5f);
                        act.n++;
                        if (act.n == n && blastHits == n && nrTgt)
                        {
                            // 세 발 다 맞음: 추가 피해
                            var t = nrTgt; float bonus = atk * AV(2) * n * AV(3);
                            Manager.Later(0.25f, () => { if (t && t.State == Item.ItemState.Rest) { t.Damage(bonus, this, true, a); FxManager.I?.Anim("hit", t.x, t.y, 20, 1.3f); } });
                            NewPopup(Manager.blasterAllHitPopup, nrTgt.x, nrTgt.y, new Color(1, 0.45f, 0.4f), 20, 1, 50);
                        }
                    }
                    break;
                }
                case EffectType.Self_Destruct:
                {
                    // 심지가 다 탐 → 막힌 벽으로 전력 질주 → 쾅! (벽 피해 = 공격력 × 밸류_01, 반경 밸류_02 안 물건 × 밸류_03) → 소멸
                    float dx = nrX1 - x, dy = nrY1 - y, d = Mathf.Sqrt(dx * dx + dy * dy), sp = RunSpeed * 3.2f;
                    if (d > 1) { vx = dx / d * sp; vy = dy / d * sp; face = dx >= 0 ? 1 : -1; }
                    walk += dt * 40; sq = 1 + Mathf.Sin(act.t * 30) * 0.12f;
                    if (fx && OnScreen() && Random.value < dt * 20) fx.Spark(x - face * 30, y, 14, new Color(1, 0.75f, 0.3f), 36, 0.12f);
                    if (d < 36 || act.t >= act.dur - dt) { SelfDestruct(atk); return; }
                    break;
                }
                case EffectType.Patty_Slam:
                {
                    // 빅버거 박치기: 패티를 다 쌓으면 물건으로 뛰어올라 쾅 (직격 × 밸류_01 + 반경 밸류_02 × 밸류_03), 패티 초기화
                    float e = Mathf.Clamp01(k / 0.85f);
                    vx = vy = 0;
                    if (!act.done)
                    {
                        x = Mathf.Lerp(nrX0, nrX1, e); y = Mathf.Lerp(nrY0, nrY1, e); z = Mathf.Sin(e * Mathf.PI) * 120;
                        if (e >= 1)
                        {
                            act.done = true; z = 0; sq = 0.6f;
                            if (nrTgt && nrTgt.State == Item.ItemState.Rest) nrTgt.Damage(atk * AV(1), this, true, face > 0 ? 0 : Mathf.PI);
                            items.Aoe(x, y, AV(2), atk * AV(3), this, false);
                            if (fx) { fx.Ring(x, y, AV(2), new Color(0.95f, 0.75f, 0.35f), 0.45f); fx.Dust(x, y, 10, 1.4f); fx.Shake(0.15f); fx.Hitstop(0.05f); if (Manager.pattySprite) fx.Burst(x, y, 20, 8, new Color(0.55f, 0.32f, 0.18f), new Color(0.95f, 0.8f, 0.4f), 120, 340, 6, 12); }
                            SfxManager.PlayAt("superjump_slam", x, y, 0.6f);
                            NewPopup(Manager.pattySlamPopup, x, y, new Color(0.95f, 0.6f, 0.25f), 22, 1.1f);
                            patty = 0;
                        }
                    }
                    break;
                }
            }
        }

        void SelfDestruct(float atk)
        {
            var fx = FxManager.I;
            if (sdHasWall) Manager.Stage.DamageWall(sdRoom.x, sdRoom.y, sdWall.x, sdWall.y, Damage * AV(1) * Manager.digMult * CommonSkill.WallDmgMul * WallMult, this);
            float R0 = AV(2);
            Manager.Items.Aoe(x, y, R0, atk * AV(3), this, false);
            Manager.Items.BlastActors(x, y, R0, 600, atk * AV(3) * 0.5f, this);
            foreach (var o in Manager.Rats)
                if (o != this && !o.UltOn && Vector2.Distance(new Vector2(o.x, o.y), new Vector2(x, y)) < R0 * 0.8f) o.Ragdoll(Mathf.Atan2(o.y - y, o.x - x), 380, 320, 0.4f);
            if (fx)
            {
                if (fx.AddSticker("explosion_big", x, y, 30, R0 * 1.4f, 0.6f) == null) fx.Anim("hit", x, y, 20, R0 / 70);
                fx.Ring(x, y, R0, new Color(1, 0.6f, 0.3f), 0.5f); fx.Ring(x, y, R0 * 1.4f, new Color(1, 0.85f, 0.5f, 0.7f), 0.6f);
                fx.Burst(x, y, 20, 24, new Color(1, 0.55f, 0.25f), new Color(1, 0.9f, 0.5f), 200, 600, 6, 12); fx.Dust(x, y, 14, 1.6f);
                fx.Shake(0.3f); fx.Hitstop(0.06f);
            }
            SfxManager.PlayAt("wall_blast", x, y);
            NewPopup(Manager.selfDestructPopup, x, y, new Color(1, 0.55f, 0.3f), 26, 1.2f, 40);
            act.on = false;
            Manager.RemoveRat(this);
        }

        void NewActionPose(ref RatRig.Pose p, float t, float k)
        {
            switch (ActionType)
            {
                case EffectType.Savory_Smell: p.head = -0.45f + Mathf.Sin(t * 14) * 0.08f; p.tail = 0.6f + Mathf.Sin(t * 10) * 0.4f; p.front = 0.3f; p.bob = -Mathf.Abs(Mathf.Sin(t * 7)) * 3; break;
                case EffectType.Tune_Up: p.tilt = -0.35f; p.back = -0.2f; p.farBack = 0.2f; p.front = 1.4f + Mathf.Sin(t * 28) * 0.7f; p.farFront = 0.9f; p.head = -0.1f + Mathf.Sin(t * 28) * 0.05f; p.tail = 0.8f; break;
                case EffectType.Blaster_Shot: p.tilt = -0.45f; p.back = -0.25f; p.farBack = 0.25f; p.front = 1.6f - bite * 0.3f; p.farFront = 1.4f; p.head = -0.15f + bite * 0.08f; p.tail = 0.9f; break;
                case EffectType.Self_Destruct: { float w = walk; p.front = Mathf.Sin(w) * 1.25f; p.farFront = Mathf.Sin(w + 0.6f) * 1.25f; p.back = Mathf.Sin(w + Mathf.PI) * 1.25f; p.farBack = Mathf.Sin(w + Mathf.PI + 0.6f) * 1.25f; p.head = -0.12f; p.tail = -0.2f; p.sx = 1.1f; p.sy = 0.93f; break; }
                case EffectType.Ice_Missile: p.tilt = -0.5f; p.front = 2.4f + Mathf.Sin(t * 12) * 0.15f; p.farFront = 2.2f; p.back = -0.3f; p.farBack = 0.3f; p.head = -0.35f; p.tail = 1.2f; break;
                case EffectType.Data_Boost: p.tilt = -0.25f; p.front = 1.5f; p.farFront = 0.6f; p.head = -0.2f + Mathf.Sin(t * 6) * 0.06f; p.tail = 0.6f + Mathf.Sin(t * 8) * 0.3f; break;
                case EffectType.Patty_Slam: if (!act.done) { p.front = 2.2f; p.farFront = 2; p.back = -1.2f; p.farBack = -1; p.head = -0.3f; p.tail = 1.1f; } else { p.sy = 0.8f; p.sx = 1.15f; p.head = 0.3f; } break;
            }
        }
    }
}
