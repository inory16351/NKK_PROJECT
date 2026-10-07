using System.Collections.Generic;
using NKK.Items;
using NKK.Rats;
using UnityEngine;

namespace NKK.Ults
{
    // 람쥐 · 람쥐썬더 (웹게임 ramthunder): 하늘이 어두워지고 웅크린 채 앞발 번쩍 찌리찌리 → 투구·망토 펑, 화면 밖으로 점프 → 망치 들고 낙하
    //   → 앞모습 히어로 랜딩(쾅) + 양옆 번개 기둥 · 금 간 바닥 · 땅을 타는 번개 물결 · 번개 비 → 마지막 초거대 번개 → 연기 펑, 평범한 다람쥐로
    // 옆모습은 게임 리그를 복제한 꼭두각시 + 웹 추가 파츠(rss_*·투구·망토·망치, 웹 drawRatRig), 히어로 랜딩은 앞모습 파츠(rs_*)를 웹 drawLanding 그대로 소품으로 조립
    // 자막 c1~c5 · 말풍선 c6 도토리 냠 · c7 …!! · c8 Bring me Acorn!! · c9~c11 람쥐썬더/찌리찌리 외침
    public class UltRamThunder : UltBase
    {
        const float T_JUMP = 0.9f, T_LAND = 1.45f, T_BACK = 4.4f, RS_H = 34;
        const int TOP = 30000;                             // 어둠 덮개 정렬 순서 (빛·번개 31500 보다 아래, 주인공은 그 위)
        public override float Dur => 5.4f;
        static readonly Color SKY = new(0.75f, 0.91f, 1f), PALE = new(0.87f, 0.94f, 1f), YEL = new(1f, 0.95f, 0.75f), DARK = new(0.078f, 0.1f, 0.18f);
        static readonly Color GLOW = new(0.59f, 0.78f, 1f, 0.35f), CORE = new(0.9f, 0.96f, 1f, 0.95f);

        class Wave { public int dir; public float d; public readonly HashSet<Item> hit = new(); }
        readonly List<Wave> waves = new();
        readonly UltProp[] pillars = new UltProp[2];
        float dark, cr = 0.2f, cr2, rainT, sayT, pillarT, finT, jx, jz, crackT, chainT, staticT, flickT;
        readonly Dictionary<Rat, float> shocked = new();        // 감전된 주변 쥐 (남은 시간)
        SpriteRenderer[] pupR; Color[] pupC;                   // 꼭두각시 그림 (전기 색 깜빡임)
        Vector3? tipL;                                         // 이번 프레임 앞발 번개 시작점 (게임 x, z)
        bool jumped, landed, fin, back;
        float Sc => R.Manager.ratScale * R.GradeData.size;

        // 옆모습 꼭두각시 (게임 리그 복제, 어둠 위에 그림) + 웹 추가 파츠
        RatRig pup; Vector3 rigOff;
        SpriteRenderer armUp, armUp2, crouchN, crouchF, sCape, sHelm, sHammer;
        // 앞모습 히어로 랜딩 파츠
        UltProp cover, crack, stamp, fCape, fTail, fBody, fBody2, fArm, fHammer, fHead, fHead2, fHelm;

        // 웹 FR_LAND: 몸통 그림 비율 (neck 목, shoulder 빠진 팔 어깨, tail 꼬리 뿌리, 발 = 가운데) / 팔 cut 어깨 잘린 면, tip 손끝
        static readonly Vector2 NECK1 = new(0.53f, 0.03f), SH1 = new(0.76f, 0.19f), TAIL1 = new(0.72f, 0.45f);
        static readonly Vector2 NECK2 = new(0.5f, 0.03f), SH2 = new(0.74f, 0.19f), TAIL2 = new(0.7f, 0.5f);
        static readonly Vector2 ARM_CUT = new(0.91f, 0.865f), ARM_TIP = new(0.07f, 0.08f);
        const float LAND_HEAD = 0.78f, LAND_ARM = 0.72f;   // 웹 FR_LAND_HEAD · FR_LAND_ARM

        public override void Begin()
        {
            Beat(0.05f, () => Cap("c1"));
            Beat(0.3f, () => Cap("c2"));
            Beat(2.3f, () => Cap("c3"));
            Beat(3.3f, () => Cap("c4"));
            Beat(4.5f, () => Cap("c5"));
            R.face = 1;
            cover = Prop("dot", R.x, R.y, 0, 9000);
            if (cover != null) { cover.tint = DARK; cover.alpha = 0; }
            MakePuppet();
            R.HideBody = pup != null;
        }

        public override void Step(float dt, float k)
        {
            float t = T;
            R.face = 1;
            dark = t < T_BACK ? Mathf.Min(0.5f, t * 0.9f) : Mathf.Max(0, dark - dt * 1.2f);
            Cover();
            Electric(dt, t);
            float shake = Mathf.Sin(t * 60) * 0.05f, gear = Mathf.Clamp01((t - T_JUMP) / 0.1f);

            if (t < T_JUMP)
            {
                // 밈 원본 그 자세 (옆모습): 웅크린 채 몸을 비틀고, 팔꿈치 굽힌 앞발을 머리 위로 번쩍 → 앞발에 번개. 두 팔 그림을 번갈아 부들부들
                float up = Mathf.Clamp01(t / 0.1f); bool fr2 = Mathf.FloorToInt(t / 0.07f) % 2 == 1;
                if (up < 1) Side(P(head: 0.3f, tail: 1, front: Mathf.Lerp(0.3f, 2.6f, up), farFront: 0.4f, tilt: Mathf.Lerp(0, 0.22f, up)), null, 0, true, 0, false, false, 1 + t);
                else Side(P(head: 0.42f, tail: 0.9f + shake, back: 0.3f, farBack: 0.2f, farFront: 0.3f, tilt: 0.22f + shake * 0.3f), fr2 ? armUp2 : armUp, -0.1f + shake, true, 0, false, true, 1 + t);
                if ((cr -= dt) <= 0) { cr = Rand(0.1f, 0.18f); Fx?.Bolt(R.x + Rand(-6, 6), R.y, PALE, 0.9f); if (Random.value < 0.5f) Flash(PALE, 0.08f); }
                if ((cr2 -= dt) <= 0) { cr2 = Rand(0.08f, 0.2f); Fx?.Bolt(R.x + Rand(-160, 160), R.y + Rand(-90, 90), SKY, 0.5f); }
            }
            else if (t < T_LAND)
            {
                // 점프: 투구·망토가 펑 붙으며 화면 밖으로 튀어 오름 → 망치 들고 낙하 (옆모습)
                if (!jumped)
                {
                    jumped = true; Smoke(R.x, R.y); Fx?.Anim("poof", R.x, R.y, 10, 1.2f);
                    Fx?.Bolt(R.x, R.y, Color.white, 2.2f); Fx?.Ring(R.x, R.y, 160, SKY, 0.4f);
                    Flash(Color.white, 0.3f); Fx?.Shake(0.3f); flickT = 0.2f;
                }
                float e = (t - T_JUMP) / (T_LAND - T_JUMP); bool rise = e < 0.55f;
                R.z = 1000 * (rise ? 1 - Mathf.Pow(1 - e / 0.55f, 2) : 1 - Mathf.Pow((e - 0.55f) / 0.45f, 2));
                if (rise) Side(P(head: -0.2f, tail: 1.3f, back: -1.3f, farBack: -1.1f, farFront: 0.8f, tilt: -0.35f, sx: 0.92f, sy: 1.1f), armUp, -0.3f, false, gear, false, false, 0);
                else Side(P(head: 0.35f, tail: 1.1f, front: 2.2f, farFront: 1.8f, back: 0.6f, farBack: 0.4f, tilt: 0.3f), null, 0, false, 1, true, false, 0);
            }
            else
            {
                if (!landed) Land();
                if (t < T_BACK)
                {
                    // 히어로 랜딩 (3점 착지): 착지 순간은 더 납작한 몸통 + 땅 보는 얼굴 → 고개 들며 치켜뜬 눈. 망치 든 팔은 뒤로 뻗어 부들부들, 숨 쉬듯 들썩
                    float tl = t - T_LAND, sq = Mathf.Max(0, 1 - tl / 0.18f), look = Mathf.Clamp01((tl - 0.35f) / 0.25f), j = tl < 0.6f ? 2 : 0.4f;
                    jx = Rand(-j, j); jz = Rand(-j, j);
                    Landing(tl < 0.14f, look < 0.5f, Mathf.Sin(t * 2.2f) * 0.03f, (1 - look) * 1.5f + Mathf.Sin(t * 2.5f) * 0.4f,
                        -0.3f + Mathf.Sin(t * 40) * 0.02f * (tl < 0.6f ? 1 : 0.3f), 1 + 0.2f * sq, 1 - 0.2f * sq + Mathf.Sin(t * 2.5f) * 0.01f);
                }
                else
                {
                    // 원래 모습으로: 연기 펑 → 평범한 다람쥐 (어둠이 걷힐 때까지는 꼭두각시로 밝게, 웹도 어둠 위에 주인공을 다시 그림)
                    if (!back)
                    {
                        back = true; Smoke(R.x, R.y); Fx?.Anim("poof", R.x, R.y, 10, 1.4f);
                        foreach (var p in new[] { fCape, fTail, fBody, fBody2, fArm, fHammer, fHead, fHead2, fHelm }) if (p != null) p.visible = false;
                    }
                    if (dark > 0.05f && pup) Side(P(), null, 0, false, 0, false, false, 0);
                    else { if (pup) pup.gameObject.SetActive(false); R.HideBody = false; R.UltPose = null; }
                }
                if (crack != null && t > T_BACK) { crack.alpha = Mathf.Max(0, 0.9f - (t - T_BACK) * 0.9f); if (stamp != null) stamp.alpha = crack.alpha * 0.4f; }

                // 양옆 번개 기둥 그림 + 기둥에 계속 내리치는 번개 (착지 뒤 잠깐, 깜빡깜빡)
                for (int i = 0; i < 2; i++)
                {
                    var p = pillars[i]; if (p == null) continue;
                    if (t > 3.3f) { KillProp(p); pillars[i] = null; continue; }
                    p.alpha = Mathf.Clamp01(3.3f - t) * Rand(0.75f, 1f); p.flip = Random.value < 0.5f;
                    p.sortBias = TOP + 1 - World.SortOrder(p.y);
                }
                if (t < T_LAND + 1.3f && (pillarT -= dt) <= 0)
                {
                    pillarT = 0.1f;
                    foreach (float dx in new[] { -120f, 120f }) Fx?.Bolt(R.x + dx + Rand(-20, 20), R.y + Rand(-10, 10), SKY, Rand(1.6f, 2.2f));
                }

                // 땅을 타고 좌우로 퍼지는 번개 물결
                foreach (var w in waves)
                {
                    float d0 = w.d; w.d = Mathf.Min(ULT_R * 1.6f, w.d + 1500 * dt);
                    float wx = R.x + w.dir * w.d;
                    if (w.d > d0 && Random.value < 0.7f)
                    {
                        Fx?.BoltLine(R.x + w.dir * Mathf.Max(0, w.d - 170), R.y + Rand(-25, 25), 6, wx, R.y + Rand(-25, 25), 6, SKY, 0.25f, 7, 7, 20, GLOW);
                        if (OnScreen(wx, R.y) && Random.value < 0.5f) { Fx?.Spark(wx, R.y, 8, SKY, Rand(40, 70), 0.15f); Fx?.Burst(wx, R.y, 6, 3, Color.white, SKY, 120, 300, 2, 4); }
                    }
                    foreach (var it in ItemsIn(wx, R.y, 110))
                        if (!w.hit.Contains(it) && Mathf.Abs(it.y - R.y) < 140) { w.hit.Add(it); BlastItem(it, ItemD * 1.2f, w.dir > 0 ? -0.5f : Mathf.PI + 0.5f, 300, 360); ZapHit(it.x, it.y); }
                    if (w.d > d0) { BlastActors(wx, R.y, 90, 600, UltD * 0.4f); foreach (var o in RatsNear(wx, R.y, 90)) Shocked(o); }
                }

                // 번개 비: 화면 속 물건에 연달아
                if (t > T_LAND + 0.4f && t < T_BACK - 0.1f && (rainT -= dt) <= 0)
                {
                    rainT = 0.07f;
                    var it = Pick(ItemsIn(R.x, R.y, ULT_R));
                    float x = it ? it.x : R.x + Rand(-ULT_R, ULT_R) * 0.8f, y = it ? it.y : R.y + Rand(-ULT_R, ULT_R) * 0.5f;
                    Fx?.Bolt(x, y, SKY, 1.1f);
                    if (it) { it.Damage(ItemD * 0.7f, R, true, Rand(0, Mathf.PI * 2)); ZapHit(x, y); }
                    BlastActors(x, y, 60, 480, UltD * 0.2f);
                    if (Random.value < 0.25f) Flash(PALE, 0.05f);
                }

                // 마지막: 다람쥐 자신에게 초거대 번개 → 큰 폭발 (웹 수명 0.7초 동안 계속 번쩍)
                if (t > T_BACK - 0.1f && !fin)
                {
                    fin = true; finT = 0.7f;
                    Fx?.Bolt(R.x, R.y, Color.white, 3.5f); Fx?.Bolt(R.x - 30, R.y, SKY, 2.5f); Fx?.Bolt(R.x + 30, R.y, SKY, 2.5f);
                    Shock(R.x, R.y, ULT_R * 0.8f, UltD * 0.6f, SKY, 3);
                    foreach (var it in ItemsIn(R.x, R.y, ULT_R * 0.8f)) BlastItem(it, ItemD, Mathf.Atan2(it.y - R.y, it.x - R.x), 360, 420);
                    BlastActors(R.x, R.y, ULT_R * 0.8f, 700, UltD);
                    Fx?.Anim("explosion", R.x, R.y, 0, 1.8f); Fx?.Ring(R.x, R.y, ULT_R * 0.9f, YEL, 0.5f);
                    Fx?.Stars(R.x, R.y, 40, 30, Color.white, SKY, 250, 700);
                    Flash(Color.white, 0.7f); Fx?.Shake(0.8f); Fx?.Hitstop(0.08f); flickT = 0.5f;
                    for (int i = 0; i < 10; i++) { float a = i / 10f * Mathf.PI * 2, d = Rand(120, 260); Fx?.BoltLine(R.x, R.y, 20, R.x + Mathf.Cos(a) * d, R.y + Mathf.Sin(a) * d * 0.6f, 4, SKY, 0.3f, 7, 8, 24, GLOW); }
                    foreach (var o in RatsNear(R.x, R.y, ULT_R * 0.8f)) Shocked(o);
                }
                else if (fin && finT > 0 && (finT -= dt) > 0 && Random.value < 0.4f) Fx?.Bolt(R.x + Rand(-10, 10), R.y, Color.white, 3.5f * finT / 0.7f + 1);
            }

            // 말풍선 (웹게임 r.say)
            if ((sayT -= dt) <= 0)
            {
                sayT = 0.8f;
                string key = t < 0.45f ? "c6" : t < 1.1f ? "c7" : t < 2 ? "c8" : Pick(new[] { "c9", "c10", "c11" });
                PopupCap(key, R.x, R.y, Color.white, 20, 0.8f, R.z + 50 * Sc);
            }
        }

        // 슈퍼히어로 착지: 땅 쾅 + 양옆 번개 기둥 + 사방 충격파 → 앞모습 파츠로 바꿈
        void Land()
        {
            landed = true; R.z = 0;
            if (pup) pup.gameObject.SetActive(false);
            R.HideBody = true;
            fCape = Prop("rs_cape_flare", R.x, R.y, 0, 10); fTail = Prop("rs_tail_sweep", R.x, R.y, 0, 10);
            fBody = Prop("rs_land_body", R.x, R.y, 0, 10); fBody2 = Prop("rs_land_body2", R.x, R.y, 0, 10);
            fArm = Prop("rs_arm_up_back", R.x, R.y, 0, 10); fHammer = Prop("rs_hammer", R.x, R.y, 0, 10);
            fHead = Prop("rs_head_bow", R.x, R.y, 0, 10); fHead2 = Prop("rs_head_bow2", R.x, R.y, 0, 10); fHelm = Prop("rs_helm", R.x, R.y, 0, 10);
            // 바닥: 거무스름한 자국(웹 stampAt) + 금 간 바닥 (바닥 층)
            stamp = Prop("dot", R.x, R.y, 0, 300);
            if (stamp != null) { stamp.ground = true; stamp.flat = 0.69f / World.TILT; stamp.tint = new Color(0.16f, 0.2f, 0.27f); stamp.alpha = 0.35f; }
            crack = Prop("ground_crack", R.x, R.y, 4, 330);
            if (crack != null) { crack.ground = true; crack.flat = 1 / World.TILT; crack.alpha = 0.9f; crack.sortBias = 1; }
            for (int i = 0; i < 2; i++)
            {
                float dx = i == 0 ? -120 : 120;
                var p = Prop("bolt_pillar", R.x + dx, R.y, 0, 150);
                if (p != null) p.z = 150 * p.r.sprite.rect.height / p.r.sprite.rect.width * 0.5f - 10;
                pillars[i] = p;
                for (int n = 0; n < 3; n++) Fx?.Bolt(R.x + dx + Rand(-20, 20), R.y + Rand(-10, 10), SKY, 2.2f);
            }
            Shock(R.x, R.y, 280, UltD * 0.8f, SKY, 3);
            foreach (var it in ItemsIn(R.x, R.y, 320)) BlastItem(it, ItemD * 1.5f, Mathf.Atan2(it.y - R.y, it.x - R.x), 420, 380);
            BlastActors(R.x, R.y, 320, 700, UltD);
            foreach (var o in RatsNear(R.x, R.y, 320)) Ragdoll(o, Mathf.Atan2(o.y - R.y, o.x - R.x), 420, 360);
            waves.Add(new Wave { dir = -1 }); waves.Add(new Wave { dir = 1 });
            Fx?.Anim("explosion", R.x, R.y, 0, 1.3f); Fx?.Dust(R.x, R.y, 16, 2);
            Fx?.Ring(R.x, R.y, 200, Color.white, 0.4f); Fx?.Ring(R.x, R.y, 360, SKY, 0.6f);
            Flash(Color.white, 0.55f); Fx?.Shake(0.9f); Fx?.Hitstop(0.12f); flickT = 0.35f;
            for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2 + Rand(-0.3f, 0.3f), d = Rand(140, 240); Fx?.BoltLine(R.x, R.y, 4, R.x + Mathf.Cos(a) * d, R.y + Mathf.Sin(a) * d * 0.6f, 4, SKY, 0.35f, 8, 8, 26, GLOW); }
            foreach (var o in RatsNear(R.x, R.y, 320)) Shocked(o);
        }

        public override void Finish() { R.z = 0; R.HideBody = false; R.UltPose = null; }
        public override void Cleanup()
        {
            if (pup) Object.Destroy(pup.gameObject);
            pup = null;
            if (R) { R.HideBody = false; R.UltJit = 0; }
            foreach (var o in shocked.Keys) if (o && !o.UltOn) o.UltJit = 0;
            shocked.Clear();
        }

        // 어둠 덮개: 화면 전체 (주인공·번개만 그 위)
        void Cover()
        {
            if (cover == null) return;
            var v = ViewRect();
            cover.x = v.center.x; cover.y = R.y; cover.z = (R.y - v.center.y) * World.TILT;
            cover.w = Mathf.Max(v.width, v.height) * 4 + 2000; cover.alpha = dark;
            cover.sortBias = TOP - World.SortOrder(R.y);
        }

        // ── 옆모습 (웹 drawRatRig + 추가 파츠) ──
        void MakePuppet()
        {
            var e = R.Manager.artLibrary ? R.Manager.artLibrary.Get(R.codeId) : null;
            var src = R.rig;
            if (e == null || e.torso == null || !src || !src.visual) return;
            var go = new GameObject("UltRamThunder_Puppet");
            go.transform.SetParent(M.propRoot ? M.propRoot : M.transform, false);
            var vis = Object.Instantiate(src.visual.gameObject, go.transform, false);
            vis.SetActive(true);
            pup = go.AddComponent<RatRig>();
            pup.visual = vis.transform;
            pup.body = Find(vis.transform, src.body);
            SpriteRenderer F(SpriteRenderer s) => s ? Find(vis.transform, s.transform)?.GetComponent<SpriteRenderer>() : null;
            pup.farBack = F(src.farBack); pup.farFront = F(src.farFront); pup.tail = F(src.tail); pup.torso = F(src.torso);
            pup.back = F(src.back); pup.front = F(src.front); pup.head = F(src.head); pup.single = F(src.single);
            pup.farLegColor = src.farLegColor; pup.farGap = src.farGap; pup.headWidthRatio = src.headWidthRatio;
            if (!pup.body || !pup.torso || !pup.head || !pup.front || !pup.back || !pup.farBack) { Object.Destroy(go); pup = null; return; }
            foreach (var r in vis.GetComponentsInChildren<SpriteRenderer>(true)) r.color = Color.white;   // 좀비·투명 색 지움
            pup.Build(e, R.Manager.RigLength(R.Data));
            rigOff = src.transform.position - R.transform.position;
            // 정렬 (웹 그리는 순서): 먼 뒷다리 -2 · 망토 -1 · 꼬리·몸통 · 웅크린 다리 4 · 머리 5 · 투구 6 · 치켜든 앞발 7 · 번쩍 든 팔 8 · 망치 9
            pup.farBack.sortingOrder = -2; pup.head.sortingOrder = 5;
            armUp = Attach("rss_arm_up", pup.body, 8); armUp2 = Attach("rss_arm_up2", pup.body, 8);
            crouchN = Attach("rss_leg_crouch", pup.body, 4); crouchF = Attach("rss_leg_crouch", pup.body, -2);
            if (crouchF) crouchF.color = new Color(0.8f, 0.8f, 0.8f);                                    // 웹 brightness(0.8)
            sCape = Attach("thor_cape", pup.body, -1); sHelm = Attach("thor_helm", pup.head.transform, 6); sHammer = Attach("rs_hammer", pup.body, 9);
            pupR = vis.GetComponentsInChildren<SpriteRenderer>(true); pupC = new Color[pupR.Length];
            for (int i = 0; i < pupR.Length; i++) pupC[i] = pupR[i].color;
        }
        // 원본 리그 안의 같은 위치(이름 경로) 자식
        Transform Find(Transform root, Transform orig)
        {
            if (!orig) return null;
            var path = new List<string>();
            for (var t = orig; t && t != R.rig.visual; t = t.parent) path.Insert(0, t.name);
            return path.Count == 0 ? root : root.Find(string.Join("/", path));
        }
        SpriteRenderer Attach(string name, Transform parent, int order)
        {
            var p = M.MakeProp(name); if (p == null) return null;
            var r = p.r; r.transform.SetParent(parent, false); r.sortingOrder = order; r.enabled = false;
            return r;
        }

        // 옆모습 한 프레임: 자세 + 번쩍 든 팔(arm) · 웅크린 다리 · 투구/망토(gear) · 앞발 망치 · 앞발 번개 · 부들부들(jit)
        void Side(RatRig.Pose pose, SpriteRenderer arm, float armRot, bool crouch, float gear, bool hammer, bool bolt, float jit)
        {
            if (!pup) { R.UltPose = pose; R.UltJit = jit; return; }
            if (!pup.gameObject.activeSelf) pup.gameObject.SetActive(true);
            var j = jit > 0 ? new Vector3(Rand(-jit, jit), Rand(-jit, jit), 0) * World.U : Vector3.zero;
            pup.transform.position = World.ToUnity(R.x, R.y, R.z) + rigOff + j;
            pup.Apply(pose, Sc, 1, 1, TOP + 5);
            if (pup.IsSingle) return;

            Vector3 sh = pup.front.transform.localPosition, neck = pup.head.transform.localPosition;
            float legF = pup.front.transform.localScale.y, legB = pup.back.transform.localScale.y;
            Rect fr = pup.front.sprite.rect, br = pup.back.sprite.rect, tr = pup.torso.sprite.rect;

            // 팔꿈치 굽혀 머리 위로 번쩍 든 앞발 (피벗 = 어깨, 길이 = 앞다리 × 1.35) + 손끝 번개
            pup.front.enabled = arm == null;
            foreach (var a in new[] { armUp, armUp2 }) if (a) a.enabled = false;
            if (arm)
            {
                Pivot(arm, sh, armRot, fr.height * legF * 1.35f / arm.sprite.rect.height);
                if (bolt) PawBolt(arm, Mathf.Abs(pup.torso.transform.lossyScale.y) * tr.height / 100f / World.U);
            }
            // 웅크린 뒷다리 (길이 = 뒷다리 × 0.85, 가까운 쪽 + 먼 쪽 어둡게)
            pup.back.enabled = pup.farBack.enabled = !crouch;
            if (crouchN) crouchN.enabled = false;
            if (crouchF) crouchF.enabled = false;
            if (crouch && crouchN && crouchF)
            {
                float kC = br.height * legB * 0.85f / crouchN.sprite.rect.height;
                Pivot(crouchN, pup.back.transform.localPosition, pose.back, kC);
                Pivot(crouchF, pup.farBack.transform.localPosition, pose.farBack, kC);
            }
            // 망토: 목 뒤에서 뒤로 펄럭 (걸쇠 = 그림 왼쪽 위)
            if (sCape) sCape.enabled = false;
            if (sCape && gear > 0)
            {
                Rect c = sCape.sprite.rect; float cw = tr.width * 1.25f * gear, ch = cw * c.height / c.width, fl = 1 + Mathf.Sin(Time.time * 14) * 0.06f;
                Place(sCape, neck + new Vector3(tr.width * 0.05f, -tr.height * 0.1f) / 100f, -0.15f + Mathf.Sin(Time.time * 9) * 0.05f, fl, 1 / fl, -cw * 0.1f, -ch * 0.12f, cw, ch);
            }
            // 투구: 머리 위 (머리 그림 기준)
            if (sHelm) sHelm.enabled = false;
            if (sHelm && gear > 0)
            {
                var hs = pup.head.sprite; Rect H = hs.rect, hr = sHelm.sprite.rect;
                float px = hs.pivot.x / H.width, py = 1 - hs.pivot.y / H.height, hw = H.width * 0.8f * gear, hh = hw * hr.height / hr.width;
                Place(sHelm, Vector3.zero, 0, 1, 1, H.width * 0.52f - hw / 2 - px * H.width, H.height * 0.22f - hh * 0.75f - py * H.height, hw, hh);
            }
            // 앞발 끝 도토리 망치 (웹 prop 'fr:rs_hammer', 크기 26, propRot -0.4)
            if (sHammer) sHammer.enabled = false;
            if (sHammer && hammer)
            {
                float a = pose.front, len = pup.front.sprite.pivot.y * legF * 0.9f, size = 26 / Mathf.Max(pup.Unit, 1e-4f);
                Rect m = sHammer.sprite.rect; float kk = size * 1.35f / Mathf.Max(m.width, m.height), w = m.width * kk, h = m.height * kk;
                Place(sHammer, sh + new Vector3(-Mathf.Sin(a) * len, -Mathf.Cos(a) * len) / 100f, a * 0.5f - 0.4f, 1, 1, -w / 2, -h / 2, w, h);
            }
        }
        // 피벗(관절)을 기준점에 두고 돌림 (웹 extraPart)
        static void Pivot(SpriteRenderer r, Vector3 at, float ang, float k)
        {
            Rect s = r.sprite.rect; float pu = r.sprite.pivot.x / s.width, pv = 1 - r.sprite.pivot.y / s.height;
            Place(r, at, ang, 1, 1, -pu * s.width * k, -pv * s.height * k, s.width * k, s.height * k);
        }
        // 웹 캔버스 그리기 그대로 (리그 픽셀, y 아래 +): 기준점 at(로컬) → 회전 ang → 배율 (kx, ky) → 그림 사각형 (dx, dy, w, h)
        static void Place(SpriteRenderer r, Vector3 at, float ang, float kx, float ky, float dx, float dy, float w, float h)
        {
            var s = r.sprite; Rect rc = s.rect;
            float px = (dx + s.pivot.x / rc.width * w) * kx, py = (dy + (1 - s.pivot.y / rc.height) * h) * ky, c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
            r.transform.localPosition = at + new Vector3(c * px - sn * py, -(sn * px + c * py)) / 100f;
            r.transform.localRotation = Quaternion.Euler(0, 0, -ang * Mathf.Rad2Deg);
            r.transform.localScale = new Vector3(kx * w / rc.width, ky * h / rc.height, 1);
            r.enabled = true;
        }
        // 앞발 번개 (웹 pawBolt): 손끝에서 팔 방향으로 지지직, 매 프레임 모양이 바뀜 (번진 빛 + 흰 심지)
        void PawBolt(SpriteRenderer arm, float len)
        {
            var s = arm.sprite; float py = 1 - s.pivot.y / s.rect.height;
            Vector3 tip = arm.transform.TransformPoint(new Vector3(0, s.rect.height * py * 0.95f / 100f, 0));
            Vector3 dir = Quaternion.Euler(0, 0, Rand(-14, 14)) * arm.transform.TransformVector(Vector3.up).normalized, nrm = new(-dir.y, dir.x, 0);
            len *= Rand(0.8f, 1.3f);
            Vector3 end = tip + dir * len * World.U + nrm * Rand(-0.15f, 0.15f) * len * World.U;
            float x1 = tip.x / World.U, z1 = tip.y / World.U + R.y * World.TILT, x2 = end.x / World.U, z2 = end.y / World.U + R.y * World.TILT;
            Fx?.BoltLine(x1, R.y, z1, x2, R.y, z2, SKY, 0.05f, 4, 6, len * 0.18f, GLOW);
            tipL = new Vector3(x1, z1, 0);
            if (Random.value < 0.5f) Fx?.Spark(x2, R.y, z2, SKY, Rand(20, 40), 0.1f);
        }

        // ── 앞모습 히어로 랜딩 (웹 drawLanding): 한 덩어리 자세 몸통 + 숙인 얼굴·뒤로 뻗은 팔·꼬리·망토 ──
        void Landing(bool body2, bool bow2, float headRot, float headY, float armRot, float sx, float sy)
        {
            var B = body2 ? fBody2 : fBody; var Hd = bow2 ? fHead2 : fHead;
            if (B == null || Hd == null) return;
            if (fBody != null) fBody.visible = !body2;
            if (fBody2 != null) fBody2.visible = body2;
            if (fHead != null) fHead.visible = !bow2;
            if (fHead2 != null) fHead2.visible = bow2;
            Vector2 neckA = body2 ? NECK2 : NECK1, shA = body2 ? SH2 : SH1, tailA = body2 ? TAIL2 : TAIL1;
            Rect br = B.r.sprite.rect, hr = Hd.r.sprite.rect; float bw = br.width, bh = br.height, hw = hr.width, hh = hr.height;
            float unit = RS_H * 0.95f / (bh + hh * LAND_HEAD * 0.45f);
            var m0 = Aff.I.Scale(sx, sy).Scale(unit, unit);
            Vector2 Pt(Vector2 v) => new((v.x - 0.5f) * bw, -bh + v.y * bh);
            Vector2 nk = Pt(neckA);
            // 망토 (펄럭, 걸쇠 = 목)
            if (fCape != null)
            {
                Rect c = fCape.r.sprite.rect; float cw = bw * 1.25f, ch = cw * c.height / c.width, fl = 1 + Mathf.Sin(Time.time * 14) * 0.05f;
                Blit(fCape, m0.Translate(nk.x, nk.y + bh * 0.08f).Scale(fl, 1 / fl), -cw / 2, -ch * 0.8f, cw, ch, 0);
            }
            // 꼬리 (몸 뒤)
            if (fTail != null)
            {
                var ts = fTail.r.sprite; Rect tr = ts.rect; Vector2 tp = Pt(tailA); float k = bh * 0.95f / tr.height;
                Blit(fTail, m0.Translate(tp.x, tp.y).Scale(k, k), -ts.pivot.x, -(tr.height - ts.pivot.y), tr.width, tr.height, 1);
            }
            Blit(B, m0, -0.5f * bw, -bh, bw, bh, 2);
            // 뒤로 뻗은 팔 (좌우 뒤집어 오른쪽 위로) + 손끝 망치
            if (fArm != null)
            {
                Rect ar = fArm.r.sprite.rect; float aw = ar.width, ah = ar.height; Vector2 s0 = Pt(shA);
                Blit(fArm, m0.Translate(s0.x, s0.y).Rotate(armRot).Scale(-LAND_ARM, LAND_ARM), -ARM_CUT.x * aw, -ARM_CUT.y * ah, aw, ah, 3);
                float vx = -(ARM_TIP.x - ARM_CUT.x) * aw * LAND_ARM, vy = (ARM_TIP.y - ARM_CUT.y) * ah * LAND_ARM, c = Mathf.Cos(armRot), sn = Mathf.Sin(armRot);
                float tx = s0.x + vx * c - vy * sn, ty = s0.y + vx * sn + vy * c, dir = Mathf.Atan2(tx - s0.x, -(ty - s0.y));
                if (fHammer != null)
                {
                    Rect m = fHammer.r.sprite.rect; float h2 = bh * 0.75f, w2 = h2 * m.width / m.height;
                    Blit(fHammer, m0.Translate(tx, ty).Rotate(dir), -w2 / 2, -h2 * 0.75f, w2, h2, 4);
                }
            }
            // 숙인 얼굴 (+ 투구)
            var hs = Hd.r.sprite; var mh = m0.Translate(nk.x, nk.y + bh * 0.1f + headY / unit).Rotate(headRot).Scale(LAND_HEAD, LAND_HEAD);
            Blit(Hd, mh, -hs.pivot.x, -(hh - hs.pivot.y), hw, hh, 5);
            if (fHelm != null)
            {
                Rect m = fHelm.r.sprite.rect; float w = hw * 0.78f, h = w * m.height / m.width;
                Blit(fHelm, mh, -w / 2, -hh * (bow2 ? 0.95f : 0.85f) - h * 0.45f, w, h, 6);
            }
        }
        // 캔버스 변환 m 으로 그린 그림 사각형 (dx, dy, w, h) → 소품 위치·폭·회전·뒤집기 (발끝 = 쥐 위치)
        void Blit(UltProp p, Aff m, float dx, float dy, float w, float h, int order)
        {
            if (p == null || !p.r || !p.r.sprite) return;
            var s = p.r.sprite; Rect rc = s.rect; float S = Sc;
            Vector2 q = m.Map(dx + s.pivot.x / rc.width * w, dy + (1 - s.pivot.y / rc.height) * h);
            float det = m.a * m.d - m.b * m.c;
            p.x = R.x + q.x * S + jx; p.y = R.y; p.z = R.z - q.y * S + jz;
            p.w = Mathf.Sqrt(m.a * m.a + m.b * m.b) * w * S;
            p.flat = Mathf.Sqrt(m.c * m.c + m.d * m.d) * h * S / Mathf.Max(1e-4f, p.w * rc.height / rc.width);
            p.flip = det < 0; p.rot = det < 0 ? Mathf.Atan2(m.b, -m.a) : Mathf.Atan2(-m.b, m.a);
            p.sortBias = TOP + 1 + order - World.SortOrder(R.y);
        }
        // 2D 캔버스 변환 (x' = a x + c y + e, y' = b x + d y + f; y 아래 +, 회전 + = 시계 방향)
        struct Aff
        {
            public float a, b, c, d, e, f;
            public static Aff I => new() { a = 1, d = 1 };
            public Aff Translate(float x, float y) { var r = this; r.e += a * x + c * y; r.f += b * x + d * y; return r; }
            public Aff Rotate(float t) { float cs = Mathf.Cos(t), sn = Mathf.Sin(t); var r = this; r.a = a * cs + c * sn; r.b = b * cs + d * sn; r.c = -a * sn + c * cs; r.d = -b * sn + d * cs; return r; }
            public Aff Scale(float x, float y) { var r = this; r.a *= x; r.b *= x; r.c *= y; r.d *= y; return r; }
            public Vector2 Map(float x, float y) => new(a * x + c * y + e, b * x + d * y + f);
        }

        // ── 전기 느낌 ──
        // 매 프레임: 몸 위를 튀는 짧은 전기 줄 · 불꽃 · 몸 색 깜빡임 · 주변으로 뻗는 전기 · 바닥 정전기 · 화면 깜빡임 · 감전된 쥐 떨림
        void Electric(float dt, float t)
        {
            bool front = landed && !back, on = t < T_BACK && t > 0.1f, air = t >= T_JUMP && t < T_LAND;
            float bh = front ? RS_H * 0.95f * Sc : 24 * Sc, cz = R.z + bh * 0.5f, cw = front ? RS_H * 0.8f * Sc : 30 * Sc;

            // 몸 위를 튀는 전기 줄 (0.04초마다 새 모양)
            if (on && (crackT -= dt) <= 0)
            {
                crackT = 0.04f;
                for (int i = 0, n = air ? 1 : 2; i < n; i++)
                {
                    float x1 = R.x + Rand(-cw, cw) * 0.5f, z1 = cz + Rand(-bh, bh) * 0.5f, a = Rand(0, Mathf.PI * 2), l = Rand(0.4f, 0.8f) * bh;
                    Fx?.BoltLine(x1, R.y, z1, x1 + Mathf.Cos(a) * l, R.y, z1 + Mathf.Sin(a) * l, CORE, 0.05f, 2.5f, 4, l * 0.25f, GLOW);
                }
                // 번쩍 든 앞발 → 몸·머리 위로 튀는 전기
                if (tipL.HasValue && t < T_JUMP && Random.value < 0.6f)
                {
                    var tp = tipL.Value;
                    Fx?.BoltLine(tp.x, R.y, tp.y, R.x + Rand(-cw, cw), R.y, cz + Rand(0, bh), SKY, 0.06f, 3, 5, bh * 0.2f, GLOW);
                }
                if (Random.value < 0.35f) Fx?.Spark(R.x + Rand(-cw, cw) * 0.6f, R.y, cz + Rand(-bh, bh) * 0.6f, SKY, Rand(18, 34) * Sc, 0.1f);
            }

            // 몸 색: 하양 ↔ 하늘색 빠르게 깜빡 (가끔 번쩍)
            float k = on ? 0.35f + 0.35f * (Mathf.Sin(t * 47) * 0.5f + 0.5f) + (Random.value < 0.08f ? 0.3f : 0) : 0;
            Color tint = Color.Lerp(Color.white, SKY, Mathf.Clamp01(k));
            if (pupR != null) for (int i = 0; i < pupR.Length; i++) if (pupR[i]) pupR[i].color = pupC[i] * tint;
            foreach (var p in new[] { fTail, fBody, fBody2, fArm, fHead, fHead2 }) if (p != null) p.tint = tint;

            // 주변 물건·쥐로 뻗는 짧은 전기 (땅에 있을 때)
            if (on && !air && (chainT -= dt) <= 0)
            {
                chainT = landed ? 0.12f : 0.18f;
                var it = Pick(ItemsIn(R.x, R.y, 240)); var o = Pick(RatsNear(R.x, R.y, 220));
                if (o && (!it || Random.value < 0.4f)) { Fx?.BoltLine(R.x, R.y, cz, o.x, o.y, 14, SKY, 0.1f, 4, 6, 16, GLOW); Shocked(o); }
                else if (it) { Fx?.BoltLine(R.x, R.y, cz, it.x, it.y, 10, SKY, 0.1f, 4, 6, 16, GLOW); Fx?.Spark(it.x, it.y, 10, SKY, 40, 0.12f); }
            }
            // 바닥 정전기: 발밑 작은 고리 + 불꽃
            if (on && !air && (staticT -= dt) <= 0)
            {
                staticT = Rand(0.15f, 0.3f);
                float sx = R.x + Rand(-60, 60), sy = R.y + Rand(-30, 30);
                Fx?.Ring(sx, sy, Rand(30, 55), new Color(0.75f, 0.91f, 1f, 0.7f), 0.2f); Fx?.Spark(sx, sy, 3, SKY, Rand(25, 45), 0.12f);
            }
            // 큰 번개 직후 화면 깜빡 (하양 ↔ 하늘색)
            if (flickT > 0) { flickT -= dt; if (Random.value < 0.5f) Flash(Random.value < 0.5f ? Color.white : SKY, Rand(0.12f, 0.3f)); }
            // 감전된 쥐: 잠깐 바들바들 + 불꽃
            if (shocked.Count > 0)
                foreach (var o in new List<Rat>(shocked.Keys))
                {
                    float left = shocked[o] - dt;
                    if (!o || left <= 0 || o.UltOn) { if (o && !o.UltOn) o.UltJit = 0; shocked.Remove(o); continue; }
                    shocked[o] = left; o.UltJit = 2.5f;
                    if (Random.value < 0.25f) Fx?.Spark(o.x + Rand(-10, 10), o.y, Rand(5, 25), SKY, 28, 0.1f);
                }
        }
        void Shocked(Rat o)
        {
            if (!o || o.UltOn) return;
            bool first = !shocked.ContainsKey(o);
            shocked[o] = 0.45f;
            if (first && OnScreen(o.x, o.y)) { Fx?.Spark(o.x, o.y, 15, SKY, 50, 0.15f); Fx?.Anim("zap", o.x, o.y, 0, 0.7f); }
        }
        // 물건 감전: 지지직 + 불꽃 튐
        void ZapHit(float x, float y)
        {
            if (!OnScreen(x, y)) return;
            Fx?.Anim("zap", x, y, 0, 0.8f); Fx?.Spark(x, y, 12, SKY, 60, 0.15f);
            Fx?.Burst(x, y, 10, 5, Color.white, SKY, 120, 320, 2, 4);
        }

        // ── 도우미 ──
        // 웹게임 skillBlastItem: 피해 주고 날려 보냄 (0 이면 떨어질 때 박살)
        void BlastItem(Item it, float dmg, float ang, float spd, float vz) => DropItem(it, Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd, vz, dmg);
    }
}
