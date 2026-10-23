using System.Collections.Generic;
using UnityEngine;

namespace NKK.Rats
{
    // 증강이 쥐에 남기는 것 (2026-10-23 기획서 Ver.04, 기획서/증강 기획서 Ver.04 반영 결정.md)
    // · 쥐 탑: 5마리를 합친 1마리 (능력치 합산, 특성 = 1층 쥐). 위 4층 = 합쳐진 쥐 리그를 쌓아 그림 (요리사 쥐도 사람 없이 쥐만). 판 끝까지
    // · 공 굴리기 (등급 통일 골든 햄스터 · 판다): 늘 몸을 만 공. 움직임 고르기는 그대로, 특수 액션·묘기 없음
    // · 다다다다 잔상: 쥐 뒤에 반투명 잔상 하나
    public partial class Rat
    {
        // ── 쥐 탑 ──
        readonly List<RatRig> towerRigs = new();
        readonly List<float> towerScales = new();
        float towerMul = 1, towerT = -1;
        bool towerThud;
        public bool IsTower => towerRigs.Count > 0;
        public float TowerMul => towerMul;

        // arts·lengths·scales = 위층 쥐 그림 · 리그 길이 · 크기 (아래 → 위). addDamage = 합친 쥐들 공격력 합
        public void MakeTower(List<RatArtLibrary.Entry> arts, List<float> lengths, List<float> scales, float addDamage)
        {
            if (!rig || arts.Count == 0) return;
            float baseD = Mathf.Max(1, Damage);
            for (int i = 0; i < arts.Count; i++)
            {
                if (arts[i] == null) continue;
                // rig 은 쥐 본체(Rat)와 같은 오브젝트 → 복제하면 쥐가 통째로 생겨 매니저 밖에서 멈춰 있었음 (벽에 낀 탑, 2026-10-23).
                // 이 쥐 자식으로 붙이고 그림(Visual)만 남김: Rat 컴포넌트 · 그림자(원래 쥐 것을 가리킴) · 탈것 · 다른 층은 지움
                var r = Instantiate(rig, transform);
                r.name = "Tower" + (towerRigs.Count + 1);
                var body = r.GetComponent<Rat>(); if (body) { body.shadow = null; DestroyImmediate(body); }
                for (int c = r.transform.childCount - 1; c >= 0; c--) { var ch = r.transform.GetChild(c); if (ch.name != "Visual") DestroyImmediate(ch.gameObject); }
                r.transform.localRotation = Quaternion.identity; r.transform.localScale = Vector3.one;
                r.Build(arts[i], lengths[i]);
                towerRigs.Add(r); towerScales.Add(scales[i]);
            }
            towerMul = 1 + addDamage / baseD;
            towerT = 0; towerThud = false;
            if (mRoot) mRoot.gameObject.SetActive(false);          // 슈퍼 요리사 쥐: 사람 없이 쥐만
        }

        // 탑 그리기 (LateUpdate, 아래 쥐 rig.Apply 뒤): 0.5초 동안 차례로 떨어져 쌓임 → 1초 흔들흔들 → 쿵
        void TowerApply(in RatRig.Pose p, int sortOrder, float lift, float rot)
        {
            if (towerRigs.Count == 0) return;
            if (towerT >= 0) towerT += Time.deltaTime;
            float step = Manager.RigLength(Data) * GradeData.size * Manager.ratScale * 0.55f;       // 한 층 높이 (게임 단위)
            float wob = towerT >= 0 && towerT < 1.5f ? Mathf.Sin(towerT * 14) * Mathf.Clamp01(1.5f - towerT) : Mathf.Sin(Time.time * 2 + seed) * 0.15f;
            for (int k = 0; k < towerRigs.Count; k++)
            {
                var r = towerRigs[k]; if (!r) continue;
                int lv = k + 1;
                float appear = towerT < 0 ? 1 : Mathf.Clamp01((towerT - lv * 0.1f) / 0.12f);
                r.gameObject.SetActive(appear > 0);
                if (appear <= 0) continue;
                float drop = (1 - appear) * 260;                        // 위에서 떨어져 쌓임
                r.transform.localPosition = new Vector3(wob * lv * 3 * World.U, 0, 0);
                r.Apply(p, Manager.ratScale * towerScales[k], face, sq, sortOrder + lv, lift + step * lv + drop, rot + wob * 0.02f * lv);
            }
            if (towerT > 1.5f && !towerThud)
            {
                towerThud = true; towerT = -1;
                FxManager.I?.Shake(0.4f); FxManager.I?.Dust(x, y, 10, 1.6f); FxManager.I?.Ring(x, y, 90, Color.white, 0.4f);
                SfxManager.PlayAt("boss_slam", x, y, 0.6f);
            }
        }

        // ── 공 굴리기 (등급 통일) ──
        public bool BallForm => AugmentSpecials.BallForm(this);
        float ballRot;
        void BallPose(ref RatRig.Pose p)
        {
            p.front = 1.9f; p.farFront = 1.9f; p.back = -1.9f; p.farBack = -1.9f; p.head = 0.8f; p.headX = 4; p.tail = -1.6f; p.sx = p.sy = 0.9f;
        }
        void BallTransform(ref float rot, ref float sx, ref float sy, ref float lift)
        {
            ballRot += Time.deltaTime * Speed / Mathf.Max(8, Radius) * face;
            rot = ballRot; sx = sy = 0.8f; lift = Speed > 40 ? Mathf.Abs(Mathf.Sin(ballRot * 2)) * 5 : 0;
        }

        // ── 롤러스케이트 (등급 통일 스케이터 생쥐 = 양복 대머리, 겟 투 워크 패러디: 팔·다리 네 곳에 롤러스케이트) ──
        //  걷지 않고 미끄러짐: 달릴 땐 뒷다리가 번갈아 뒤로 차고(밀기) 앞발은 바퀴를 바닥에 댄 채 활주 · 몸은 낮게 앞으로 숙임 · 걸음 들썩임 없음
        //  아주 빠르면 앞발을 앞으로 뻗고 쭉 미끄러짐 · 멈출 땐 바로 서지 않고 관성으로 미끄러지며 브레이크 (StopDash → SkateCoast)
        public bool SkateForm => AugmentSpecials.SkateForm(this);
        // 값 = AugmentSpecials 인스펙터 '스케이트' (웹 미리보기 UnityResources/Rats/Augment/rig_web/skater.html 의 pose() 와 같은 계산)
        float skatePh, skatePhT = -1;           // 밀기 박자 (한 주기 = 가까운 다리 · 먼 다리 한 번씩, 원래 빠른 쥐라 거리 기준이면 너무 바빠 보여 시간 기준)
        bool skateWasRun;
        static float SkatePush(AugmentSpecials.SkateTune k, float u)         // 밀기 한 번의 다리 각도: 뒤로 참 → 돌아옴 → 활주
        {
            if (u < k.kickEnd) return k.kick * Mathf.SmoothStep(0, 1, u / k.kickEnd);
            if (u < k.recoverEnd) return Mathf.Lerp(k.kick, k.recover, Mathf.SmoothStep(0, 1, (u - k.kickEnd) / (k.recoverEnd - k.kickEnd)));
            return Mathf.Lerp(k.recover, 0, (u - k.recoverEnd) / (1 - k.recoverEnd));
        }
        void SkatePose(ref RatRig.Pose p)
        {
            var k = AugmentSpecials.Skate;
            float tt = Time.time;
            bool run = mode == Mode.Run;
            if (run && !skateWasRun) skatePh = Mathf.Floor(skatePh * 2 + 1) / 2;      // 새로 달릴 때마다 다른 다리로 첫 밀기
            skateWasRun = run;
            if (skatePhT != tt) { if (skatePhT >= 0 && run && !Rushing) skatePh += (tt - skatePhT) * Mathf.Clamp(Speed / k.cadenceDiv, k.cadenceMin, k.cadenceMax); skatePhT = tt; }
            p.bob = 0; p.head = k.head + Mathf.Sin(tt * 1.3f + seed) * 0.02f; p.headX = k.headX; p.sx = k.sx; p.sy = k.sy;
            p.tail = k.tailBase + Mathf.Sin(tt * 5 + seed) * k.tailWave;
            bool fastCoast = !run && Speed > RunSpeed * k.glideAt;
            if (fastCoast || (Rushing && Speed > 30))
            {
                // 쭉 미끄러짐 (관성으로 빠르게 · 총공격 돌진): 앞발 앞으로 · 뒷발 뒤로 뻗음 (슈퍼맨 자세), 꼬리 펄럭
                p.front = k.glideFront; p.farFront = k.glideFarFront; p.back = k.glideBack; p.farBack = k.glideFarBack; p.head = k.glideHead; p.tilt = k.glideTilt; p.sx = k.glideSx; p.sy = k.glideSy;
                p.tail = -0.2f + Mathf.Sin(tt * 30) * 0.07f;
            }
            else if (Speed > 30 && run)
            {
                // 밀기: 가까운 뒷다리 · 먼 뒷다리 번갈아, 미는 순간 몸이 살짝 앞으로 쏠림
                float ph = skatePh, a = SkatePush(k, Mathf.Repeat(ph, 1)), b = SkatePush(k, Mathf.Repeat(ph + 0.5f, 1));
                float pw = Mathf.Max(-a, -b) / Mathf.Max(0.01f, -k.kick);
                p.back = a; p.farBack = b;
                p.front = k.frontBase + k.frontSwing * Mathf.Sin(ph * Mathf.PI * 2); p.farFront = k.frontBase - k.frontSwing * Mathf.Sin(ph * Mathf.PI * 2);
                p.tilt = k.tilt + pw * k.tiltPush; p.sx = k.sx + pw * k.sxPush;
            }
            else if (Speed > 30)
            {
                // 관성 활주: 네 발 다 바닥에 대고 균형만
                p.front = k.coastFront; p.farFront = k.coastFarFront; p.back = k.coastBack; p.farBack = k.coastFarBack; p.tilt = k.coastTilt + Mathf.Sin(tt * 3 + seed) * k.coastWobble;
            }
            else
            {
                // 제자리: 바퀴 위에서 앞뒤로 살짝 굴러다님
                float s = Mathf.Sin(tt * k.idleSpeed + seed);
                p.front = k.frontBase + s * k.idleFront; p.farFront = k.frontBase - 0.05f + s * k.idleFront; p.back = -0.05f - s * k.idleBack; p.farBack = -s * k.idleBack; p.tilt = s * k.idleTilt;
            }
            // 브레이크: 몸을 뒤로 젖히고 앞발을 듦 (뒷바퀴로 끼익)
            float bk = 1 - Mathf.Clamp01((tt - stopT) / k.brakeTime);
            if (bk > 0 && Speed > 30)
            {
                p.front = Mathf.Lerp(p.front, k.brakeFront, bk); p.farFront = Mathf.Lerp(p.farFront, k.brakeFarFront, bk); p.back = Mathf.Lerp(p.back, k.brakeBack, bk); p.farBack = Mathf.Lerp(p.farBack, k.brakeFarBack, bk);
                p.tilt = Mathf.Lerp(p.tilt, k.brakeTilt, bk); p.head = Mathf.Lerp(p.head, k.brakeHead, bk); p.tail = Mathf.Lerp(p.tail, k.brakeTail, bk);
            }
            if (k.ground > 0.5f && rig) p.bob = rig.GroundBob(p);      // 가장 낮은 바퀴가 바닥에 닿게
        }
        // 멈춤 → 속도를 두고 미끄러지다 섬 (Move 첫머리, Pause 동안). 빠르면 바퀴 자국 먼지 · 브레이크 불꽃
        bool SkateStop(float t0, float t1)
        {
            if (!SkateForm || Held || UltOn) return false;
            if (Speed > 150) stopT = Time.time;
            mode = Mode.Pause; t = Random.Range(t0, t1) * CommonSkill.StopTimeMul; sq = 1.05f;
            return true;
        }
        void SkateCoast(float dt)
        {
            float f = Mathf.Exp(-AugmentSpecials.Skate.friction * dt);
            vx *= f; vy *= f;
            if (Speed > 150 && OnScreen() && Random.value < dt * 10)
            {
                var fx = FxManager.I; if (!fx) return;
                fx.Dust(x - face * Radius * 0.6f, y, 1, 0.6f);
                if (Time.time - stopT < AugmentSpecials.Skate.brakeTime) fx.Spark(x + face * Radius * 0.7f, y, 4, new Color(1f, 0.85f, 0.4f), 30, 0.12f);
            }
        }

        // ── 다다다다 잔상: 몸통 그림 하나를 조금 늦게 따라오게 ──
        SpriteRenderer ghostImg;
        Vector3 ghostPos;
        void AfterimageApply()
        {
            bool on = AugmentSpecials.Afterimage && !HideBody && temp <= 0 && rig && (rig.IsSingle ? rig.single : rig.torso) && OnScreen(0.1f);
            if (!on) { if (ghostImg) ghostImg.enabled = false; return; }
            var src = rig.IsSingle ? rig.single : rig.torso;
            if (!ghostImg)
            {
                ghostImg = new GameObject("Afterimage").AddComponent<SpriteRenderer>();
                ghostImg.transform.SetParent(transform.parent, false);
                ghostPos = src.transform.position;
            }
            ghostImg.enabled = true;
            ghostImg.sprite = src.sprite; ghostImg.sortingLayerID = src.sortingLayerID; ghostImg.sortingOrder = World.SortOrder(y) - 1;
            ghostImg.flipX = src.flipX;
            ghostPos = Vector3.Lerp(ghostPos, src.transform.position, Mathf.Min(1, Time.deltaTime * 9));
            ghostImg.transform.position = ghostPos;
            ghostImg.transform.rotation = src.transform.rotation;
            ghostImg.transform.localScale = src.transform.lossyScale;
            ghostImg.color = new Color(1, 1, 1, 0.3f);
        }
        void OnDisable() { if (ghostImg) Destroy(ghostImg.gameObject); }
    }
}
