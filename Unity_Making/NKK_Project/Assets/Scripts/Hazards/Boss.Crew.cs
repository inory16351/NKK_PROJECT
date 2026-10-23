using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using UnityEngine;

namespace NKK.Hazards
{
    // 97층 수상한 우주복 쥐 (2026-10-23, 웹 미리보기 UnityResources/Rats/BossRework/rig_web/crewmate.html 에서 동작 확인 후 옮김)
    //  · 괴물 머리: 열리는 머리가 있는 파츠 리그(BossPartRig.HasOpen)만. 평소엔 닫힘, 등장(커지며)·촉수 삼키기·덮치기(공중)·뱉기 때만 열림
    //    (예전: 공격·맞을 때 그림(alt)으로 바꿔 맞을 때마다 괴물 입이 깜빡였음)
    //  · Swallow (촉수 삼키기): 머리가 열림 → 입에서 혀 촉수(sprite)가 범위 안 쥐 쪽으로 count 갈래 뻗음 → windup 에 범위 안 쥐를 가장 가까운 촉수가 잡아
    //    dur 초에 입으로 끌어감 → 꿀꺽, p1 초 뒤 한 마리씩 앞으로 뱉음 (그동안 우물우물). 예전엔 블랙홀 소용돌이 + 쥐가 그 자리에서 사라짐
    //  · Meeting (긴급 회의): 빨간 버튼(sprite)이 보스 앞에 떨어짐 → 보스가 폴짝 올라가 꾹 → 긴급 회의 화면 (BossCutscene.PlayMeeting, 얼굴 = opt face)
    //    → 끝나면 쥐 전부 stun 초 기절 (opt all = 맵 전체, 없으면 화면 안)
    //  · opt alarm (Drop): 떨어진 사이렌이 남아 있는 동안 화면이 붉게 깜빡
    public partial class Boss
    {
        [Header("우주복 쥐 (Boss.Crew.cs, 97층)")]
        [Tooltip("머리가 열리는 빠르기 · 닫히는 빠르기 (초당) · 덮치기(공중)에서 열리는 정도")] public float headOpenSpeed = 7, headCloseSpeed = 14, pounceOpen = 0.85f;
        [Tooltip("입 자리 = 이 파츠 그림의 한 점 (좌상단 기준 0~1): 촉수가 나오고 삼킨 쥐가 있는 곳")] public string mouthPart = "jawLow";
        public Vector2 mouthPoint = new(0.25f, 0.35f);
        [Tooltip("촉수 굵기 (게임 단위) · 뻗는 시간 (초) · 촉수끼리 뻗는 간격 (초) · 뻗기 시작 (windup 비율)")] public float tentacleThick = 70, tentacleOut = 0.18f, tentacleGap = 0.04f, tentacleShoot = 0.5f;
        [Tooltip("뱉을 때 한 마리씩 간격 (초) · 뱉기 직전 입 벌리는 시간 (초) · 삼킨 동안 몸 출렁 크기")] public float spitGap = 0.04f, spitOpen = 0.45f, chewSquash = 0.06f;
        [Tooltip("긴급 회의 버튼: 보스 앞 거리 · 앞발 거리 (버튼 위에 앞발) · 폴짝 뛰는 시각 (windup 비율) · 폴짝 속도")] public float buttonAhead = 130, buttonPaw = 70, buttonHop = 0.38f, buttonHopV = 520;
        [Tooltip("사이렌 경보: 화면 깜빡 간격 (초) · 세기")] public float alarmEvery = 0.5f, alarmFlash = 0.12f;

        class Tentacle { public float ang, tx, ty, x0, y0, t, wig, hold, back; public bool pull; public SpriteRenderer r; public readonly List<(Rat o, float x0, float y0, float z0)> rats = new(); }
        readonly List<Tentacle> tentacles = new();
        float headOpen, headOpenHold, alarmT, alarmTick;
        Vector3 mouth;          // 입 자리 (게임 좌표 x · y · 높이)
        int swallowN;
        SpriteRenderer button; float buttonX, buttonY, buttonZ, buttonVz, buttonSq = 1, buttonSize, hopX0, meetDone = -1;
        bool hopped, pressed;

        bool OpenHead => Part && partRig && partRig.HasOpen;

        // 매 프레임 (싸우는 중, UpdateReworkFx): 머리 열림 · 촉수 · 우물우물 · 버튼 · 경보
        void UpdateCrew(float dt)
        {
            UpdateTentacles(dt);
            UpdateButton(dt);
            if (alarmT > 0)
            {
                alarmT -= dt;
                if ((alarmTick -= dt) <= 0) { alarmTick = alarmEvery; if (Ults) Ults.Flash(new Color(1f, 0.15f, 0.15f), alarmFlash); }
            }
            if (!OpenHead) { headOpen = 0; UpdateMouth(); return; }
            // 곧 뱉을 쥐가 있으면 입을 벌림 · 삼킨 쥐가 있으면 우물우물 (몸 출렁)
            bool spitSoon = false;
            foreach (var o in swallowed) if (o && o.swallowT < 0.3f) spitSoon = true;
            if (spitSoon) headOpenHold = spitOpen;
            headOpenHold = Mathf.Max(0, headOpenHold - dt);
            float want = 0;
            if (attacking && atk != null && atk.pattern == "Swallow" && tentacles.Count + (atkHit ? 0 : 1) > 0) want = 1;
            if (attacking && atkType == "Pounce" && air) want = pounceOpen;
            if (pendingSpecial != null && Opt("intro", DB.BossAtk(pendingSpecial)) != null) want = 1;      // 커지면서 머리가 열림 → 그대로 등장 촉수
            if (headOpenHold > 0) want = 1;
            headOpen += (want - headOpen) * Mathf.Min(1, dt * (want > headOpen ? headOpenSpeed : headCloseSpeed));
            if (Mathf.Abs(want - headOpen) < 0.003f) headOpen = want;
            if (swallowed.Count > 0 && headOpen < 0.1f) sq = 1 + chewSquash * Mathf.Sin(t * 14);
        }

        // LateUpdate: 리그에 열림을 넘기고 (Apply 전), 입 자리를 잼 (Apply 뒤)
        void ApplyHeadOpen() { if (partRig) partRig.open = OpenHead ? headOpen : 0; }
        void UpdateMouth()
        {
            if (OpenHead)
            {
                var p = partRig.Point(mouthPart, mouthPoint); var q = transform.position;
                mouth = new Vector3(x + (p.x - q.x) / World.U, y, Mathf.Max(0, z + (p.y - q.y) / World.U));
            }
            else mouth = new Vector3(x + face * R * 0.3f, y, Height * 0.5f);
        }

        // ── 촉수 삼키기 (UpdateRework 의 Swallow) ──
        void UpdateSwallow(float dt, float wind, float k)
        {
            if (atkT < wind) { jit = 2; Cats?.ShowWarn(0, x, y, atk.radius, k); }
            if (atkT >= wind * tentacleShoot && released == 0) { released = 1; ShootTentacles(); }
            if (atkT >= wind && !atkHit) { atkHit = true; Cats?.HideWarns(); GrabTentacles(); }
            var fx = FxManager.I;
            if (atkHit && tentacles.Count == 0 && seqI == 0)
            {
                // 다 삼킴: 콱 다묾
                seqI = 1; phaseT = atkT; sq = 0.82f;
                fx?.Shake(0.35f);
                if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 34, 1.2f, Height);
            }
            if (seqI == 1 && atkT > phaseT + 0.4f) attacking = false;
        }

        void ShootTentacles()
        {
            var inR = new List<Rat>();
            float r2 = atk.radius * atk.radius;
            foreach (var o in Rats.Rats) if (!o.UltOn && !o.Swallowed && !o.Grabbed && (o.x - x) * (o.x - x) + (o.y - y) * (o.y - y) < r2) inR.Add(o);
            inR.Sort((p, q) => Mathf.Atan2(p.y - y, p.x - x).CompareTo(Mathf.Atan2(q.y - y, q.x - x)));
            int n = Mathf.Max(3, Mathf.Min(Mathf.Max(1, atk.count), inR.Count));
            var sp = MemeSpriteAt(atk, 0);
            for (int i = 0; i < n; i++)
            {
                var o = inR.Count > 0 ? inR[Mathf.Min(inR.Count - 1, (int)((i + 0.5f) * inR.Count / n))] : null;
                float a = o ? Mathf.Atan2(o.y - y, o.x - x) : i * Mathf.PI * 2 / n + 0.4f;
                float d = o ? Mathf.Sqrt((o.x - x) * (o.x - x) + (o.y - y) * (o.y - y)) : atk.radius * 0.8f;
                SpriteRenderer r = null;
                if (shotTemplate && sp) { r = Instantiate(shotTemplate, shotTemplate.transform.parent); r.sprite = sp; r.gameObject.SetActive(true); r.enabled = true; }
                tentacles.Add(new Tentacle { ang = a, tx = x + Mathf.Cos(a) * d, ty = y + Mathf.Sin(a) * d, t = -i * tentacleGap, wig = Random.Range(0f, 6f), r = r });
            }
            FxManager.I?.Shake(0.2f);
            SfxManager.PlayAt("zap", x, y);
        }

        // 범위 안 쥐를 가장 가까운 (방향이 비슷한) 촉수가 잡음 → 입으로 끌어감
        void GrabTentacles()
        {
            if (tentacles.Count == 0) return;
            float r2 = atk.radius * atk.radius, hold = atk.p1 > 0 ? atk.p1 : 3, back = Mathf.Max(0.1f, atk.dur);
            int c = 0;
            foreach (var o in Rats.Rats)
            {
                if (o.UltOn || o.Swallowed || o.Grabbed || (o.x - x) * (o.x - x) + (o.y - y) * (o.y - y) > r2) continue;
                float ao = Mathf.Atan2(o.y - y, o.x - x), bd = 1e9f; Tentacle best = null;
                foreach (var te in tentacles) { float d = Mathf.Abs(Mathf.DeltaAngle(ao * Mathf.Rad2Deg, te.ang * Mathf.Rad2Deg)); if (d < bd) { bd = d; best = te; } }
                best.rats.Add((o, o.x, o.y, o.z)); o.Grab(back + 0.5f); c++;
            }
            foreach (var te in tentacles) { te.pull = true; te.t = 0; te.x0 = te.tx; te.y0 = te.ty; te.hold = hold; te.back = back; }
            Cats?.HitFx(x, y, atk.radius, SpName, c, 0.3f);
        }

        void UpdateTentacles(float dt)
        {
            for (int i = tentacles.Count - 1; i >= 0; i--)
            {
                var te = tentacles[i]; te.t += dt;
                float tx, ty, tz;
                if (!te.pull)
                {
                    float k = Mathf.Clamp01(te.t / tentacleOut);
                    tx = Mathf.Lerp(mouth.x, te.tx, k); ty = Mathf.Lerp(mouth.y, te.ty, k); tz = Mathf.Lerp(mouth.z, 20, k);
                }
                else
                {
                    // 끝이 입으로 (점점 빨라짐), 잡힌 쥐가 끝에 매달려 따라감
                    float k = Mathf.Clamp01(te.t / te.back), e = k * k;
                    tx = Mathf.Lerp(te.x0, mouth.x, e); ty = Mathf.Lerp(te.y0, mouth.y, e); tz = Mathf.Lerp(20, mouth.z, e);
                    foreach (var g in te.rats)
                    {
                        if (!g.o) continue;
                        g.o.x = Mathf.Lerp(g.x0, mouth.x, e) + Mathf.Sin(t * 30 + g.x0) * 3 * (1 - k); g.o.y = Mathf.Lerp(g.y0, mouth.y + 1, e); g.o.z = Mathf.Lerp(g.z0, mouth.z - 10, e);
                    }
                    if (k >= 1)
                    {
                        foreach (var g in te.rats) if (g.o && g.o.Grabbed) { g.o.Swallow(te.hold + (swallowN++ % 20) * spitGap, mouth.x, mouth.y); swallowed.Add(g.o); }      // 한 마리씩 퉤퉤 (뭉쳐 나오지 않게)
                        if (te.r) Destroy(te.r.gameObject);
                        tentacles.RemoveAt(i);
                        continue;
                    }
                }
                PlaceTentacle(te, tx, ty, tz);
            }
        }

        // 입 → 끝: 혀 촉수 그림(왼쪽 = 뿌리, 오른쪽 = 이빨 달린 끝)을 길이만큼 늘이고 살짝 꿈틀
        void PlaceTentacle(Tentacle te, float tx, float ty, float tz)
        {
            if (!te.r) return;
            Vector3 a = World.ToUnity(mouth.x, mouth.y, mouth.z), b = World.ToUnity(tx, ty, tz);
            var d = b - a; float len = d.magnitude;
            te.r.enabled = len > 0.01f;
            if (!te.r.enabled) return;
            var bs = te.r.sprite.bounds.size;
            float thick = tentacleThick * World.U * (0.8f + 0.2f * Mathf.Sin(t * 12 + te.wig));
            te.r.transform.position = (a + b) * 0.5f;
            te.r.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + Mathf.Sin(t * 9 + te.wig) * 3);
            te.r.transform.localScale = new Vector3(len / Mathf.Max(0.001f, bs.x), thick / Mathf.Max(0.001f, bs.y), 1);
            te.r.sortingOrder = World.SortOrder(y) + 50;
        }

        // ── 긴급 회의 (UpdateRework 의 Meeting) ──
        void BeginMeeting()
        {
            hopped = pressed = false; meetDone = -1;
            var sp = MemeSpriteAt(atk, 0);
            if (button) Destroy(button.gameObject);
            button = null;
            buttonX = x + face * buttonAhead; buttonY = y + 10; buttonZ = 600; buttonVz = -1500; buttonSq = 1; buttonSize = MemeSize;
            if (shotTemplate && sp) { button = Instantiate(shotTemplate, shotTemplate.transform.parent); button.sprite = sp; button.gameObject.SetActive(true); button.enabled = true; button.transform.rotation = Quaternion.identity; }
        }

        void UpdateMeeting(float dt, float wind)
        {
            var fx = FxManager.I;
            if (atkT < wind)
            {
                if (Ults && Mathf.Repeat(atkT, 0.35f) < dt) Ults.Flash(MemeColor, 0.18f);
                // 폴짝 → 앞발이 버튼 위로 → 꾹
                if (atkT >= wind * buttonHop && !hopped) { hopped = true; vz = buttonHopV; hopX0 = x; face = buttonX >= x ? 1 : -1; }
                if (hopped && !pressed)
                {
                    float kk = Mathf.Clamp01((atkT - wind * buttonHop) / Mathf.Max(0.1f, wind * (1 - buttonHop) - 0.1f));
                    x = Mathf.Lerp(hopX0, buttonX - face * buttonPaw, kk); vx = vy = 0;
                    if (z <= 0 && atkT > wind * buttonHop + 0.1f) { pressed = true; sq = 0.7f; buttonSq = 0.45f; fx?.Shake(0.3f); SfxManager.PlayAt("boss_slam", x, y); }
                }
                return;
            }
            if (!atkHit)
            {
                atkHit = true; buttonSq = 0.45f;
                var face0 = Opt("face");
                if (cutscene && !BalanceProbe.Active) cutscene.PlayMeeting(face0 != null ? SpriteOrTemp(face0) : null, MeetingDone);      // 세상 멈춤 (WorldFreeze) → 끝나면 기절
                else MeetingDone();
                return;
            }
            if (meetDone >= 0 && atkT > meetDone + Mathf.Max(0.3f, atk.dur)) { ClearButton(true); attacking = false; }
        }

        void MeetingDone()
        {
            if (!attacking || atk == null) return;
            meetDone = atkT;
            bool all = Opt("all") != null;
            int c = 0;
            foreach (var o in Rats.Rats) if (!o.UltOn && !o.Swallowed && (all || !Ults || Ults.OnScreen(o.x, o.y, 0))) { o.Stun(atk.stun); o.vz = Mathf.Max(o.vz, 220); c++; }
            var fx = FxManager.I;
            if (MemePop != null) fx?.Popup(x, y, MemePop, MemeColor, 56, 2, Height * 1.2f);
            fx?.Shake(0.6f); fx?.Stars(x, y, Height * 0.6f, 40, MemeColor, Color.white, 200, 700);
            if (Ults) Ults.Flash(MemeColor, 0.45f);
        }

        void UpdateButton(float dt)
        {
            if (!button) return;
            if (buttonVz < 0) { buttonZ = Mathf.Max(0, buttonZ + buttonVz * dt); if (buttonZ <= 0) { buttonVz = 0; FxManager.I?.Shake(0.15f); FxManager.I?.Anim("poof", buttonX, buttonY, 0, 0.8f); } }
            buttonSq += (1 - buttonSq) * Mathf.Min(1, dt * 2);
            float w = buttonSize * World.U / Mathf.Max(0.001f, button.sprite.bounds.size.x);
            float h = button.sprite.bounds.size.y * w * buttonSq / World.U;
            button.transform.position = World.ToUnity(buttonX, buttonY, buttonZ + h * 0.5f);
            button.transform.localScale = new Vector3(w / Mathf.Sqrt(buttonSq), w * buttonSq, 1);
            button.sortingOrder = World.SortOrder(buttonY);
        }

        void ClearButton(bool poof)
        {
            if (!button) return;
            if (poof) FxManager.I?.Anim("poof", buttonX, buttonY, 20, 1);
            Destroy(button.gameObject); button = null;
        }

        void ClearCrew()
        {
            foreach (var te in tentacles)
            {
                if (te.r) Destroy(te.r.gameObject);
                foreach (var g in te.rats) if (g.o && g.o.Grabbed) g.o.grabT = 0.01f;      // 다음 프레임에 놓아줌
            }
            tentacles.Clear();
            ClearButton(false);
            headOpen = headOpenHold = 0; alarmT = 0;
            if (partRig) partRig.open = 0;
        }
    }
}
