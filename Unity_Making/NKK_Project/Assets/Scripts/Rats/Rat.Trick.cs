using System.Collections.Generic;
using NKK.Data;
using NKK.Items;
using UnityEngine;

namespace NKK.Rats
{
    // 쥐 묘기 (웹게임 doTrick / trickStep): 킥플립·트리플 악셀·윈드밀·대포알 + 잠자기. 쳇바퀴 돌기는 유니티 전용 (5훈장 해금)
    public partial class Rat
    {
        public enum TrickType { None, Flip, Axel, Windmill, Cannon, Wheel }

        static readonly Dictionary<TrickType, (float dur, int pts, string[] text)> Tricks = new()
        {
            [TrickType.Flip] = (0.62f, 1, new[] { "백덤블링!", "공중제비!", "찍-공중회전!" }),
            [TrickType.Axel] = (1.05f, 2, new[] { "트리플 악셀!!", "3회전 성공!", "심사위원 전원 10점!" }),
            [TrickType.Windmill] = (1.15f, 2, new[] { "윈드밀!!", "브레이크 댄스!", "빙글빙글 파괴!" }),
            [TrickType.Cannon] = (1.3f, 2, new[] { "쥐 대포알!", "데굴데굴!", "핀볼 모드!" }),
            [TrickType.Wheel] = (1.6f, 2, new[] { "쳇바퀴 돌기!!", "빙글빙글~", "어지러워!!" }),
        };

        public TrickType Trick { get; private set; }
        float trickT, trickDur, trickHitT, trickAng; bool trickLanded;
        readonly Dictionary<Item, float> trickHits = new();
        [HideInInspector] public float sleep;

        // 묘기 중에 쳐서 날린 물건은 묘기 점수만큼 더 아픔
        public int TrickPoints => Trick != TrickType.None ? Tricks[Trick].pts : 0;

        // 물건에 부딪힌 순간 공용 묘기 굴리기 (웹 rollTrick): 윈드밀·트리플 악셀·쥐 대포알은 공용 스킬로 해금, 백덤블링은 기본 확률 + 스킬
        // 날아차기·배치기(웹 2.5%·2%)는 아직 없음. 확률 × 재롱 본능(종별 성장)
        void RollCommonTrick(Item it, float ang)
        {
            float k = TrickChanceMult;
            bool big = it.Data.is_big == 1 || it.R > 30;
            // 공용 묘기는 훈장 트리에서 해금해야 나옴 (1 백덤블링 · 2 윈드밀 · 3 트리플 악셀 · 4 쥐 대포알 · 5 쳇바퀴 돌기). 윈드밀은 큰 물건에서 1.5배
            if (Random.value < CommonSkill.TrickChance(2) * (big ? 1.5f : 1) * k) { StartTrick(TrickType.Windmill, ang); return; }
            if (Random.value < CommonSkill.TrickChance(3) * k) { StartTrick(TrickType.Axel, ang); return; }
            if (Random.value < CommonSkill.TrickChance(5) * k) { StartTrick(TrickType.Wheel, ang + Random.Range(-0.6f, 0.6f)); return; }
            if (Random.value < CommonSkill.TrickChance(4) * k) { StartTrick(TrickType.Cannon, ang + Mathf.PI + Random.Range(-0.8f, 0.8f)); return; }
            if (Random.value < CommonSkill.TrickChance(1) * k) StartTrick(TrickType.Flip, ang);
        }

        public bool StartTrick(TrickType type, float ang)
        {
            if (Trick != TrickType.None || sleep > 0) return false;
            Trick = type; trickT = 0; trickDur = Tricks[type].dur; trickHitT = 0; trickAng = ang; trickLanded = false; trickHits.Clear();
            if (type == TrickType.Flip) { vx = -Mathf.Cos(ang) * 170; vy = -Mathf.Sin(ang) * 170; }
            else if (type == TrickType.Cannon) { float s = 720 * DashSpeedMult; vx = Mathf.Cos(ang) * s; vy = Mathf.Sin(ang) * s; }
            else if (type == TrickType.Wheel) { float s = Manager.wheelTrickSpeed * DashSpeedMult; vx = Mathf.Cos(ang) * s; vy = Mathf.Sin(ang) * s; face = vx >= 0 ? 1 : -1; }
            else { vx = vy = 0; }
            var fx = FxManager.I;
            if (fx) { fx.Dust(x, y, 4, 0.8f); var tx = Tricks[type].text; fx.Popup(x, y, tx[Random.Range(0, tx.Length)], new Color(1, 0.95f, 0.75f), 19, 0.9f, 50); }
            return true;
        }

        void TrickStep(float dt)
        {
            float dmg = Damage, rad = Radius;
            trickT += dt; trickHitT -= dt;
            float k = trickT / trickDur;
            void Drag(float d) { float f = Mathf.Max(0, 1 - d * dt); vx *= f; vy *= f; }
            var fx = FxManager.I;
            switch (Trick)
            {
                case TrickType.Flip:
                    Drag(3);
                    if (trickT >= trickDur) Manager.Items.Aoe(x, y, 50, dmg * 1.5f, this);
                    break;
                case TrickType.Axel:
                    if (!trickLanded && k >= 0.85f)
                    {
                        trickLanded = true;
                        for (int i = 0; i < 3; i++) { int n = i; Manager.Later(n * 0.1f, () => Manager.Items.Aoe(x, y, 80 * (0.7f + n * 0.2f), dmg * 3, this)); }
                        Manager.Game.AddCombo(5);
                        fx?.Popup(x, y, "짠! 10.0 · 10.0 · 10.0", new Color(0.94f, 0.78f, 0.47f), 20, 1.1f, 70); fx?.Shake(0.1f);
                    }
                    break;
                case TrickType.Windmill:
                    walk += dt * 30;
                    if (trickHitT <= 0) { trickHitT = 0.15f; Manager.Items.Aoe(x, y, 55, dmg * 0.5f, this); fx?.Ring(x, y, 55, new Color(1, 1, 1, 0.7f), 0.25f); fx?.Dust(x, y, 2, 1); }     // 웹: 칠 때마다 흰 고리
                    break;
                case TrickType.Wheel:
                    // 쳇바퀴 돌기: 바퀴처럼 굴러가며 지나가는 길의 물건을 계속 침 (벽에 부딪히면 튕겨 나감 — Move 기본)
                    walk += dt * 30;
                    if (trickHitT <= 0) { trickHitT = 0.12f; Manager.Items.Aoe(x, y, 50, dmg * 0.6f, this); fx?.Dust(x, y, 2, 1); }
                    break;
                case TrickType.Cannon:
                    walk += dt * 40;
                    // 닿는 물건마다 핀볼처럼 튕기며 박살
                    foreach (var it in Manager.Items.InRange(x, y, rad + 60))
                    {
                        if (it.State != Item.ItemState.Rest) continue;
                        float dx = x - it.x, dy = y - it.y, d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > rad + it.R || (trickHits.TryGetValue(it, out var tHit) && tHit > Time.time)) continue;
                        trickHits[it] = Time.time + 0.25f;
                        float nx = dx / (d > 0 ? d : 1), ny = dy / (d > 0 ? d : 1), dot = vx * nx + vy * ny;
                        if (dot < 0) { vx -= 2 * dot * nx; vy -= 2 * dot * ny; }
                        x = it.x + nx * (rad + it.R + 1); y = it.y + ny * (rad + it.R + 1);
                        it.Damage(dmg * 2, this, false, Mathf.Atan2(-ny, -nx));
                    }
                    break;
            }
            if (trickT >= trickDur) { Manager.Ults?.Charge(this, CondType.Action_Use, CommonSkill.TrickGaugeMul); Trick = TrickType.None; StopDash(0.15f, 0.4f); sq = 0.7f; }   // 묘기 성공 = 특수 액션과 같은 필살기 게이지
        }

        // 묘기 중 몸 전체 움직임 (웹게임 drawRat 의 trick 변환): 위로 뜨는 높이, 회전, 좌우·상하 배율
        void TrickTransform(out float lift, out float rot, out float sx, out float sy, out float pivotH)
        {
            lift = 0; rot = 0; sx = 1; sy = 1; pivotH = 11 * GradeData.size * Manager.ratScale;
            if (Trick == TrickType.None) return;
            float k = Mathf.Min(1, trickT / trickDur), f = face;
            switch (Trick)
            {
                case TrickType.Flip: lift = Mathf.Sin(k * Mathf.PI) * 58; rot = -f * k * Mathf.PI * 2; break;
                case TrickType.Axel:
                {
                    float air = Mathf.Min(1, k / 0.85f);
                    lift = Mathf.Sin(air * Mathf.PI) * 85;
                    if (k < 0.85f) { sx = Mathf.Cos(air * Mathf.PI * 6); rot = Mathf.Sin(air * Mathf.PI * 6) * 0.1f; }
                    else { float e = Mathf.Sin((k - 0.85f) / 0.15f * Mathf.PI); sx = 1 + e * 0.25f; sy = 1 - e * 0.2f; }
                    break;
                }
                // 윈드밀: 등을 바닥에 대고 몸 중심을 축으로 뱅글뱅글 (웹: 축 높이 0.6hh, 뒤집힌 몸의 발이 축 위 0.5hh → 몸 중심 ≈ 축)
                // 뒤집힌 몸은 발에서 아래로 뻗으므로 발 위치를 축 위쪽(pivotH 음수)에 둠. 예전엔 축 아래 hh 에 둬서 몸이 큰 원을 그림(바퀴처럼)
                case TrickType.Windmill: { float hh = pivotH; lift = hh * 1.1f; pivotH = -hh * 0.5f; rot = k * Mathf.PI * 2 * 4 * f; sy = -1; break; }
                // 대포알: 몸을 말고 데굴데굴 (웹: 축 높이 0.8hh + 통통, 발은 축 아래 0.9hh × 0.72)
                // 쳇바퀴 돌기: 뒤집힌 몸이 축에서 떨어져 큰 원을 그리며 돎 (예전 윈드밀 버그 모습을 살린 것)
                case TrickType.Wheel: lift = pivotH * 0.1f; rot = k * Mathf.PI * 2 * 5 * f; sy = -1; break;
                case TrickType.Cannon: { float hh = pivotH; pivotH = hh * 0.65f; lift = hh * 0.15f + Mathf.Abs(Mathf.Sin(trickT * 9)) * 10; rot = trickT * 22 * f; sx = 0.8f; sy = 0.72f; break; }
            }
        }

        // 묘기 중 자세 (웹게임 ratPose 의 trick)
        void TrickPose(ref RatRig.Pose p)
        {
            float tt = Time.time, k = Mathf.Min(1, trickT / trickDur), f = Mathf.Sin(tt * 28);
            switch (Trick)
            {
                case TrickType.Flip: { float tuck = Mathf.Sin(k * Mathf.PI); p.front = Mathf.Lerp(0.3f, 1.8f, tuck); p.farFront = Mathf.Lerp(0.2f, 1.6f, tuck); p.back = Mathf.Lerp(-0.2f, -1.6f, tuck); p.farBack = Mathf.Lerp(-0.1f, -1.4f, tuck); p.tail = 1.3f * tuck; p.head = 0.5f * tuck; break; }
                case TrickType.Axel:
                    if (k < 0.85f) { p.front = 0.25f; p.farFront = 0.25f; p.back = -0.2f; p.farBack = -0.2f; p.tail = 1.4f; p.head = -0.25f; }
                    else { float e = (k - 0.85f) / 0.15f; p.front = Mathf.Lerp(0.25f, 2.6f, e); p.farFront = Mathf.Lerp(0.25f, 2.3f, e); p.tilt = Mathf.Lerp(0, -0.4f, e); p.head = -0.3f; p.tail = 1.2f; }
                    break;
                case TrickType.Windmill: p.front = f * 1.5f; p.farFront = -f * 1.5f; p.back = Mathf.Sin(tt * 28 + 1.6f) * 1.5f; p.farBack = -Mathf.Sin(tt * 28 + 1.6f) * 1.5f; p.tail = Mathf.Sin(tt * 22) * 1.2f; p.head = Mathf.Sin(tt * 14) * 0.35f; break;
                case TrickType.Wheel: p.front = f * 1.5f; p.farFront = -f * 1.5f; p.back = Mathf.Sin(tt * 28 + 1.6f) * 1.5f; p.farBack = -Mathf.Sin(tt * 28 + 1.6f) * 1.5f; p.tail = Mathf.Sin(tt * 22) * 1.2f; p.head = Mathf.Sin(tt * 14) * 0.35f; break;
                case TrickType.Cannon: p.front = 1.9f; p.farFront = 1.9f; p.back = -1.9f; p.farBack = -1.9f; p.head = 0.8f; p.headX = 4; p.tail = -1.6f; p.sx = 0.9f; p.sy = 0.9f; break;
            }
        }

        // 멈칫 끝에 가끔 잠듦 (코골이 능력은 자주)
        bool TrySleep()
        {
            if (Random.value >= SleepChance) return false;
            if (ActTrigger(NKK.Data.CondType.Sleep_Start)) return true;
            sleep = Random.Range(2f, 4f); abT = 0.7f;
            return true;
        }
    }
}
