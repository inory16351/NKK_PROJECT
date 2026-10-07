using System.Collections.Generic;
using NKK.Items;
using NKK.Rats;
using NKK.Stage;
using UnityEngine;

namespace NKK.Ults
{
    // 방송쥐 · 쥐커드 제노사이드 (웹게임 livestream, 괴물쥐 패러디): 도네 "쥐커드 해주세요!!" → 머뭇 → 안경 번쩍, 쌍권총 트리플 악셀 난사(나선 탄막)
    //   → 도네가 계속 들어오면 앵콜 탄막 + 하늘에서 도네 상자 쾅 → 큰손 도네 "리마스터" = 360° 탄막 3연발 → 꾸벅 "후원 감사합니다~"
    // 자막: c1 LIVE · c2 (아 진짜요…?) · c3 제노사이드 · c4 리마스터
    //   팝업: c5 후원 알림({n}=치즈) · c6 큰손 후원({n}) · c7 큰손 메시지 · c8~c15 도네 메시지 · c16~c22 쥐 대사 · c23·c24 총소리 · c25~c34 채팅 · c35 시청자 수({n})
    public class UltLiveStream : UltBase
    {
        public override float Dur => 6.4f;
        static readonly Color GOLD = new(0.95f, 0.76f, 0.31f), CREAM = new(1f, 0.95f, 0.75f), LILAC = new(0.80f, 0.71f, 0.86f), CASE = new(0.90f, 0.70f, 0.35f);
        static readonly int[] AMOUNTS = { 1000, 1000, 5000, 10000, 50000 };

        class Bullet { public float x, y, z, vx, vy, life, dmg; public UltProp tr; }
        class Gift { public UltProp p; public float x, y, z, vz, rot; }
        class Shell { public UltProp p; public float x, y, z, vx, vy, vz, life; public int bounce; }
        // 앞발에 쥔 권총 (리그 앞다리 자식 → 다리와 같이 움직임): 손잡이 = 발끝, 총구 끝에 섬광
        class Gun { public Transform hold; public SpriteRenderer gun, flash; public float flashT; }
        readonly List<Bullet> bullets = new();
        readonly List<Gift> gifts = new();
        readonly List<Shell> shells = new();

        UltProp chat, alertIcon;
        Gun gunN, gunF;                                   // 가까운 앞발 · 먼 앞발
        Sprite gunSp, goldSp, flashSp;
        float gunW, propRot, propRot2;
        float alertT;
        float glint, recoil, hop, spin, dir, fireT, donT = 1.6f, chatT, sayT, viewT;
        int hopN = -1, shot, enc, rings;
        bool shout;

        public override void Begin()
        {
            Beat(0.05f, () => Cap("c1"));
            Beat(0.4f, () => { Donate(1000, "c" + Random.Range(8, 11), false); Cap("c2"); });
            Beat(1.1f, () => { glint = 1.6f; Cap("c3"); Fx?.Stars(R.x + R.face * 10, R.y, 34, 10, Color.white, CREAM, 120, 320); Fx?.Shake(0.12f); });
            Beat(4.6f, () => { Donate(2750000, "c7", true); Cap("c4"); });
            // 권총: 새 그림(ult_pistol · 리마스터 금색 ult_pistol_gold) 없으면 기존 gun
            gunSp = Spr("ult_pistol") ?? Spr("gun"); goldSp = Spr("ult_pistol_gold") ?? gunSp; flashSp = Spr("muzzle");
            gunW = R.Manager.RigLength(R.Data) * R.Manager.ratScale * R.GradeData.size * 0.62f;    // 몸길이의 0.62 (멀리서도 총으로 읽히게)
            if (R.rig && !R.rig.IsSingle && gunSp) { gunN = MakeGun(R.rig.front, 9); gunF = MakeGun(R.rig.farFront, 2); }
            chat = Prop("ult_chat", R.x, R.y, 0, 260);
            alertIcon = Prop("ult_donation", R.x, R.y, 0, 64); if (alertIcon != null) alertIcon.visible = false;
            dir = Rand(0, Mathf.PI * 2);
        }

        public override void Step(float dt, float k)
        {
            float t = T, sc = R.GradeData.size;
            bool firing = t > 1.1f && t < 4.5f, remaster = t >= 4.7f;
            glint = Mathf.Max(0, glint - dt * 1.5f);
            recoil = Mathf.Max(0, recoil - dt * 14);
            R.UltSx = 1; R.UltJit = 0;
            propRot = propRot2 = 0;

            // ── 자세 ──
            if (t < 1.1f)
            {
                // 도네 읽는 중: 머뭇머뭇 (총은 아직 안 꺼냄)
                R.UltPose = P(head: Mathf.Sin(t * 3) * 0.1f, front: 0.3f, farFront: 0.2f, tail: 0.4f, tilt: 0.05f);
            }
            else if (t < 1.6f)
            {
                // 쥐커드 제노사이드!!! 폼: 두 권총을 얼굴 옆으로 번쩍, 몸 뒤로 젖힘
                float e = Mathf.Clamp01((t - 1.1f) / 0.15f);
                R.UltPose = P(front: Mathf.Lerp(0.3f, 2.5f, e), farFront: Mathf.Lerp(0.2f, 2.2f, e), head: -0.3f * e, tail: 1.2f, tilt: -0.12f * e);
                R.face = 1;
                propRot = 1.3f * e; propRot2 = 1.1f * e;
            }
            else if (t < 4.5f || (t >= 4.7f && t < 6.05f))
            {
                // 트리플 악셀 난사: 직선으로 쭉 미끄러지다 점프 → 공중에서 세로축 3바퀴 → 착지하며 방향 전환
                float hopT = remaster ? 0.42f : 0.55f;
                hop += dt / hopT; spin += dt * 6 * Mathf.PI / hopT;
                float ph = hop % 1; int n = Mathf.FloorToInt(hop);
                if (n != hopN)
                {
                    hopN = n;
                    var it = Pick(ItemsIn(R.x, R.y, 450));
                    dir = it && Random.value < 0.7f ? Mathf.Atan2(it.y - R.y, it.x - R.x) + Rand(-0.3f, 0.3f) : Rand(0, Mathf.PI * 2);
                    if (OnScreen(R.x, R.y)) { Fx?.Dust(R.x, R.y, 6, 0.9f); Fx?.Burst(R.x, R.y, 2, 4, Color.white, new Color(0.87f, 0.94f, 1f), 60, 160, 3, 6); }
                }
                float spd = remaster ? 640 : 480;
                float x = R.x, y = R.y, vx = Mathf.Cos(dir) * spd, vy = Mathf.Sin(dir) * spd;
                BounceMove(ref x, ref y, ref vx, ref vy, R.Radius, dt);
                R.x = x; R.y = y;
                if (Mathf.Sign(vx) != Mathf.Sign(Mathf.Cos(dir)) || Mathf.Sign(vy) != Mathf.Sign(Mathf.Sin(dir))) dir = Mathf.Atan2(vy, vx);   // 벽에 닿으면 튕겨서 계속
                R.z = Mathf.Sin(ph * Mathf.PI) * (remaster ? 75 : 45);
                if (OnScreen(R.x, R.y) && Random.value < 0.25f) Fx?.Dust(R.x, R.y, 1, 0.5f);     // 스케이트 자국
                float rc = recoil * 0.2f, air = Mathf.Sin(ph * Mathf.PI);
                propRot = -0.8f; propRot2 = -2.35f;                                     // 가까운 총 = 앞, 먼 총 = 뒤
                R.UltPose = P(front: 1.55f + rc, farFront: -1.55f - rc, back: -0.3f - 0.5f * air, farBack: 0.2f + 0.5f * air, tail: 1.2f + 0.3f * air, head: -0.12f);
                R.UltSx = Mathf.Cos(spin);
                R.face = 1; R.UltJit = 0.2f;
            }
            else if (t < 4.7f)
            {
                // 리마스터 직전: 착지해서 권총 빙글빙글
                R.z = Mathf.Max(0, R.z - dt * 500);
                R.UltPose = P(front: 2.5f, farFront: 2.2f, head: -0.25f, tail: 1.1f);
                R.face = 1;
                propRot = t * 40; propRot2 = -t * 40;
            }
            else
            {
                // 후원 감사합니다~ (꾸벅)
                float b = Mathf.Min(1, (t - 6.05f) / 0.15f);
                R.UltPose = P(head: 0.45f * b, front: Mathf.Lerp(1.5f, 0.6f, b), farFront: Mathf.Lerp(1.5f, 1.9f, b), tilt: 0.25f * b, tail: 0.9f);
                R.face = 1; R.z = 0;
            }
            // 안경 번쩍
            if (glint > 0.3f && Random.value < glint * 0.4f) Fx?.Stars(R.x + R.face * 12, R.y, 30 * sc + R.z, 1, Color.white, CREAM, 20, 80);

            // ── 권총 (앞발에 쥠) ──
            var ps = R.UltPose ?? default;
            HoldGun(gunN, t >= 1.1f, ps.front, propRot, t >= 4.6f, dt);
            HoldGun(gunF, t >= 1.1f, ps.farFront, propRot2, t >= 4.6f, dt);
            // 회전 궤적 (웹게임: 흰 호 두 줄)
            if (spin != 0 && t >= 1.6f && t < 6.05f && (t < 4.5f || t >= 4.7f) && OnScreen(R.x, R.y))
                for (int g = 0; g < 2; g++)
                    for (int q = 0; q < 6; q++)
                    {
                        float a0 = spin + g * Mathf.PI - 1.2f + q * 0.2f, a1 = a0 + 0.2f, hz = 22 * sc + R.z;
                        Fx?.Beam(R.x + Mathf.Cos(a0) * 46 * sc, R.y + Mathf.Sin(a0) * 24 * sc, hz, R.x + Mathf.Cos(a1) * 46 * sc, R.y + Mathf.Sin(a1) * 24 * sc, hz, new Color(1, 0.95f, 0.75f, 0.2f + 0.06f * q), dt * 1.1f);
                    }

            // ── 쥐 대사 ──
            if ((sayT -= dt) <= 0)
            {
                sayT = 0.85f;
                string key = t < 1.1f ? Pick(new[] { "c16", "c17" }) : remaster ? "c21" : Pick(new[] { "c18", "c19", "c20" });
                if (t < 6.05f) PopupCap(key, R.x, R.y, Color.white, 17, 0.8f, 70 * sc + R.z);
            }
            if (t > 6.1f && !shout) { shout = true; PopupCap("c22", R.x, R.y, CREAM, 20, 1.5f, 70 * sc); R.UltJit = 0; }

            // ── 방송 화면: 채팅창 · 시청자 수 · 후원 알림 (화면 기준 자리) ──
            var vr = ViewRect();
            float cx = vr.xMax - vr.width * 0.12f, cy = vr.yMin + vr.height * 0.42f;
            if (chat != null) { chat.x = cx; chat.y = cy; chat.z = 0; chat.w = vr.width * 0.14f; chat.alpha = Mathf.Min(0.9f, t * 4) * (t > 6.2f ? Mathf.Max(0, 1 - (t - 6.2f) / 0.2f) : 1); OnTop(chat); }
            if ((chatT -= dt) <= 0)
            {
                chatT = Rand(0.25f, 0.4f);
                string key = firing || remaster ? "c" + Random.Range(28, 35) : "c" + Random.Range(25, 28);
                PopupCap(key, cx + Rand(-30, 30), cy + vr.height * 0.1f, Pick(new[] { GOLD, new Color(0.62f, 0.84f, 0.66f), new Color(0.66f, 0.83f, 0.86f), new Color(0.95f, 0.72f, 0.69f), LILAC }), 15, 1.1f, 0);
            }
            if ((viewT -= dt) <= 0) { viewT = 1; PopupCap("c35", cx, cy - vr.height * 0.17f, new Color(0.91f, 0.47f, 0.42f), 16, 0.9f, 0, Mathf.FloorToInt(1200 + t * 900 + enc * 350).ToString("N0")); }
            if (alertIcon != null)
            {
                alertT -= dt;
                alertIcon.visible = alertT > 0;
                float pk = Mathf.Min(1, (1.4f - alertT) * 6);
                alertIcon.x = vr.xMin + vr.width * 0.06f; alertIcon.y = vr.yMin + vr.height * 0.36f; alertIcon.z = 0;
                alertIcon.w = 64 * (0.6f + 0.4f * pk) * (1 + 0.08f * Mathf.Sin(t * 20)); alertIcon.rot = Mathf.Sin(t * 12) * 0.15f; alertIcon.alpha = Mathf.Min(1, alertT * 3);
                OnTop(alertIcon);
            }

            // ── 쌍권총 난사: 앞뒤 두 줄기가 빙글빙글 (나선 탄막) ──
            float dmg = UltD * 0.08f;                 // 웹게임: 쥐 공격력 × 2 / 발
            if (((firing && t >= 1.6f) || (remaster && t < 6.05f)) && (fireT -= dt) <= 0)
            {
                fireT = remaster ? 0.035f : 0.05f; recoil = 1; shot++;
                bool vis = OnScreen(R.x, R.y);
                for (int g = 0; g < 2; g++)
                {
                    float a = spin + g * Mathf.PI + Rand(-0.05f, 0.05f), ux = Mathf.Cos(a), uy = Mathf.Sin(a);
                    var gn = g == 0 ? gunN : gunF;
                    float mx = R.x + ux * 24 * sc, my = R.y + uy * 10, mz = 22 * sc + R.z;
                    if (gn != null && gn.gun.enabled)
                    {
                        var mp = GunPoint(gn, 0.02f, 0.77f); mx = mp.x; mz = mp.y;                              // 총구 끝에서 발사
                        gn.flashT = 0.06f; if (gn.flash) gn.flash.transform.localScale = Vector3.one * FlashK() * Rand(0.85f, 1.25f);
                    }
                    var bl = new Bullet { x = mx, y = my, z = mz, vx = ux * 1300, vy = uy * 1300, life = 0.55f, dmg = dmg };
                    bl.tr = Tracer(mx, my, mz, ux, uy);
                    bullets.Add(bl);
                    if (vis)
                    {
                        Fx?.Stars(mx + ux * 8, my, mz, 2, CREAM, Color.white, 40, 120);                          // 총구 섬광
                        Fx?.Burst(mx + ux * 6, my, mz, 1, Color.white, CREAM, 60, 140, 3, 5);
                        if (Random.value < 0.3f) Fx?.Dust(mx, my, 1, 0.5f);                                        // 총구 연기
                        EjectShell(gn, sc, g);                                                                      // 탄피
                    }
                }
                if (vis)
                {
                    if (shot % 9 == 0) PopupCap(Random.value < 0.5f ? "c23" : "c24", R.x + R.face * 50, R.y, CREAM, 18, 0.45f, 60);
                    Fx?.Shake(0.012f);
                }
            }
            // 도네가 계속 들어옴 → 앵콜: 물건 위로 도네 상자 + 탄막 한 바퀴
            if (firing && (donT -= dt) <= 0)
            {
                donT = Rand(0.55f, 0.85f); enc++;
                Donate(Pick(AMOUNTS), "c" + Random.Range(8, 16), false);
                var tt = Pick(ItemsIn(R.x, R.y, 700)); if (tt) AddGift(tt.x, tt.y, Rand(700, 900), -1000);
                for (int q = 0; q < 12; q++) { float a = q / 12f * Mathf.PI * 2 + enc; bullets.Add(new Bullet { x = R.x, y = R.y, z = 22 * sc + R.z, vx = Mathf.Cos(a) * 1100, vy = Mathf.Sin(a) * 1100, life = 0.5f, dmg = dmg }); }
                if (OnScreen(R.x, R.y)) Fx?.Ring(R.x, R.y, 120, LILAC, 0.3f);
            }
            // 리마스터: 360° 탄막 3연발
            if (remaster && rings < 3 && t > 4.7f + rings * 0.35f)
            {
                rings++;
                for (int q = 0; q < 36; q++) { float a = q / 36f * Mathf.PI * 2 + rings * 0.09f; bullets.Add(new Bullet { x = R.x, y = R.y, z = 22 * sc + R.z, vx = Mathf.Cos(a) * 1300, vy = Mathf.Sin(a) * 1300, life = 0.65f, dmg = dmg * 1.5f }); }
                if (OnScreen(R.x, R.y))
                {
                    Fx?.Ring(R.x, R.y, 200 + rings * 80, GOLD, 0.4f); Fx?.Ring(R.x, R.y, 120 + rings * 40, Color.white, 0.3f);
                    Fx?.Stars(R.x, R.y, 30, 14, GOLD, CREAM, 200, 520);
                    Fx?.Shake(0.25f); Flash(CREAM, 0.12f); Fx?.Hitstop(0.03f);
                }
                for (int q = 0; q < 4; q++) { var tt = Pick(ItemsIn(R.x, R.y, 800)); if (tt) AddGift(tt.x, tt.y, Rand(700, 1000), -1100); }
            }

            StepBullets(dt);
            StepGifts(dt);
            StepShells(dt);

            // 시청자(쥐)들 들썩들썩
            foreach (var o in RatsNear(R.x, R.y, 600)) { o.frenzy = Mathf.Max(o.frenzy, 1); if (o.z <= 0 && Random.value < dt * 2) o.vz = 240; }
        }

        // 소품 그림 목록에서 스프라이트만 꺼냄 (없으면 null)
        Sprite Spr(string name)
        {
            var p = Prop(name, 0, 0, 0, 1); if (p == null) return null;
            var sp = p.r ? p.r.sprite : null; KillProp(p); return sp;
        }

        // 앞다리(리그 파츠) 밑에 권총을 붙임. order = 쥐 리그 안 그리는 순서 (가까운 총 9 = 머리 위, 먼 총 2 = 몸통 뒤)
        Gun MakeGun(SpriteRenderer leg, int order)
        {
            if (!leg || !leg.sprite) return null;
            var hold = new GameObject("UltGunHold").transform; hold.SetParent(leg.transform, false);
            var gn = new Gun { hold = hold };
            gn.gun = NewSr("UltGun", hold, gunSp, leg, order);
            // 손잡이(그림 오른쪽 아래)가 발끝에 오게
            var b = gunSp.bounds;
            gn.gun.transform.localPosition = -new Vector3(b.min.x + b.size.x * 0.8f, b.min.y + b.size.y * 0.32f, 0);
            // 섬광 그림은 왼쪽을 향함 = 총구 방향 그대로 (총 자식이라 늘 총열을 따라감)
            if (flashSp) { gn.flash = NewSr("UltGunFlash", gn.gun.transform, flashSp, leg, order + 1); gn.flash.enabled = false; }
            // 발끝 = 다리 그림 아래 끝 90% (웹게임 drawRatRig prop)
            var lb = leg.sprite.bounds;
            hold.localPosition = new Vector3(lb.center.x, lb.min.y * 0.9f, 0);
            return gn;
        }

        static SpriteRenderer NewSr(string name, Transform parent, Sprite sp, SpriteRenderer like, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = sp;
            r.sortingLayerID = like.sortingLayerID; r.sharedMaterial = like.sharedMaterial; r.sortingOrder = order;
            return r;
        }

        // 섬광 크기 (총 그림 기준 배율): 총 길이의 1.1배
        float FlashK() => flashSp && gunSp ? gunSp.bounds.size.x * 1.1f / flashSp.bounds.size.x : 1;

        // 매 프레임: 각도 = 웹게임 (다리 각 × 0.5 + propRot), 크기 = gunW 게임 단위
        void HoldGun(Gun gn, bool show, float legA, float rot, bool gold, float dt)
        {
            if (gn == null || !gn.hold) return;
            gn.gun.enabled = show;
            gn.gun.sprite = gold ? goldSp : gunSp;
            float legK = Mathf.Max(0.01f, gn.hold.parent.localScale.x), unit = R.rig.Unit * R.Manager.ratScale * R.GradeData.size;
            gn.hold.localScale = Vector3.one * (gunW * World.U / (gunSp.bounds.size.x * unit * legK));
            // 다리 자체 회전(-legA)을 빼서 리그 기준 -(legA·0.5 + rot) 이 되게. 쏠 때마다 반동으로 총구가 살짝 들림
            gn.hold.localRotation = Quaternion.Euler(0, 0, (legA * 0.5f - rot - recoil * 0.25f) * Mathf.Rad2Deg);
            if (!gn.flash) return;
            gn.flashT -= dt;
            gn.flash.enabled = show && gn.flashT > 0;
            if (!gn.flash.enabled) return;
            var b = gn.gun.sprite.bounds; float fk = gn.flash.transform.localScale.x;
            gn.flash.transform.localPosition = new Vector3(b.min.x - flashSp.bounds.size.x * fk * 0.42f, b.min.y + b.size.y * 0.77f, 0);
        }

        // 총 그림 위 한 점(u·v 0~1, 왼쪽 = 총구, 아래 = 0)의 게임 좌표 (x, 높이 z) — 쥐 바닥 y 기준
        Vector2 GunPoint(Gun gn, float u, float v)
        {
            var b = gn.gun.sprite.bounds;
            var w = gn.gun.transform.TransformPoint(new Vector3(b.min.x + b.size.x * u, b.min.y + b.size.y * v, 0));
            return new Vector2(w.x / World.U, w.y / World.U + R.y * World.TILT);
        }

        // 예광탄 (그림 머리가 왼쪽 → 뒤집어서 날아가는 쪽으로). 그림이 없으면 선(Beam)만
        UltProp Tracer(float x, float y, float z, float ux, float uy)
        {
            var p = Prop("ult_tracer", x, y, z, 70); if (p == null) return null;
            p.flip = true; p.rot = Mathf.Atan2(-uy * World.TILT, ux); p.sortBias = 3;
            return p;
        }

        // 탄피: 총 위(배출구)에서 튀어 나와 빙글빙글 떨어져 바닥에서 한 번 튐
        void EjectShell(Gun gn, float sc, int g)
        {
            float x = R.x, z = 22 * sc + R.z;
            if (gn != null && gn.gun.enabled) { var p = GunPoint(gn, 0.55f, 0.95f); x = p.x; z = p.y; }
            var pr = shells.Count < 60 ? Prop("ult_shell", x, R.y, z, 9 * sc) : null;
            if (pr == null) { Fx?.Burst(x, R.y, z, 1, CASE, GOLD, 80, 180, 2, 3); return; }
            float side = g == 0 ? 1 : -1;
            shells.Add(new Shell { p = pr, x = x, y = R.y, z = z, vx = Rand(-160, 160) * side, vy = Rand(-50, 50), vz = Rand(260, 380), life = 0.9f });
        }

        void StepShells(float dt)
        {
            for (int i = shells.Count - 1; i >= 0; i--)
            {
                var s = shells[i];
                s.life -= dt; s.vz -= 1500 * dt; s.x += s.vx * dt; s.y += s.vy * dt; s.z += s.vz * dt;
                if (s.z <= 0) { s.z = 0; if (s.bounce++ < 1) { s.vz = -s.vz * 0.35f; s.vx *= 0.5f; s.vy *= 0.5f; } else { s.vz = 0; s.vx *= 0.8f; s.vy *= 0.8f; } }
                if (s.p != null) { s.p.x = s.x; s.p.y = s.y; s.p.z = s.z + 2; if (s.z > 0) s.p.rot += dt * 22; s.p.alpha = Mathf.Min(1, s.life * 4); }
                if (s.life <= 0) { KillProp(s.p); shells.RemoveAt(i); }
            }
        }

        // 후원 알림: 왼쪽 위 카드 자리에 아이콘 + 글 (큰 도네는 금색 + 번쩍 + 폭죽)
        void Donate(int amt, string msgKey, bool big)
        {
            var vr = ViewRect();
            float ax = vr.xMin + vr.width * 0.17f, ay = vr.yMin + vr.height * 0.36f;
            PopupCap(big ? "c6" : "c5", ax, ay, big ? GOLD : CREAM, big ? 26 : 19, big ? 2.2f : 1.4f, 30, amt.ToString("N0"));
            PopupCap(msgKey, ax, ay, Color.white, big ? 20 : 16, big ? 2.2f : 1.4f, 0);
            alertT = big ? 2.2f : 1.4f;
            if (big)
            {
                Flash(GOLD, 0.2f);
                Fx?.Stars(R.x, R.y, 60, 30, GOLD, LILAC, 200, 600);
                Fx?.Burst(R.x, R.y, 60, 16, GOLD, CREAM, 200, 500, 3, 8);
                Fx?.Shake(0.15f);
            }
        }

        void AddGift(float x, float y, float z, float vz)
        {
            var p = Prop("ult_donation", x, y, z, 46);
            gifts.Add(new Gift { p = p, x = x, y = y, z = z, vz = vz, rot = Rand(-0.5f, 0.5f) });
        }

        // 떨어진 도네 상자: 쾅 (필살기 피해 공식)
        void StepGifts(float dt)
        {
            for (int i = gifts.Count - 1; i >= 0; i--)
            {
                var g = gifts[i];
                g.z += g.vz * dt; g.rot += dt * 3;
                if (g.p != null) { g.p.x = g.x; g.p.y = g.y; g.p.z = Mathf.Max(0, g.z) + 20; g.p.rot = g.rot; }
                if (g.z > 0) continue;
                Shock(g.x, g.y, 130, UltD * 0.4f, LILAC, 1);
                foreach (var it in ItemsIn(g.x, g.y, 130)) FlingItem(it, Mathf.Atan2(it.y - g.y, it.x - g.x), 420, 420);
                BlastActors(g.x, g.y, 120, 520, UltD * 0.5f);
                if (OnScreen(g.x, g.y)) { Fx?.Stars(g.x, g.y, 20, 10, GOLD, LILAC, 150, 380); Fx?.Burst(g.x, g.y, 20, 8, LILAC, Color.white, 150, 380, 3, 7); Fx?.Anim("poof", g.x, g.y, 0, 0.8f); }
                KillProp(g.p); gifts.RemoveAt(i);
            }
        }

        // 총알: 열린 방 밖이면 사라짐. 물건·사람·고양이를 맞힘 (웹게임 G.bullets, actors: true)
        void StepBullets(float dt)
        {
            var cat = ItemMgr.Cats ? ItemMgr.Cats.Current : null;
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var b = bullets[i];
                float px = b.x, py = b.y;
                b.life -= dt; b.x += b.vx * dt; b.y += b.vy * dt;
                if (b.life <= 0 || !M.Stage.Open.Contains(StageManager.RoomOf(b.x, b.y))) { KillProp(b.tr); bullets.RemoveAt(i); continue; }
                bool vis = OnScreen(b.x, b.y, 40);
                b.z = Mathf.Max(10, b.z - dt * 20);
                if (b.tr != null) { b.tr.x = b.x; b.tr.y = b.y; b.tr.z = b.z; b.tr.alpha = Mathf.Min(1, b.life * 6); }
                if (vis) Fx?.Beam(px - b.vx * dt, py - b.vy * dt, b.z, b.x, b.y, b.z, CREAM, dt * 1.5f);
                float ang = Mathf.Atan2(b.vy, b.vx); bool hit = false;
                foreach (var h in ItemMgr.Humans)
                    if (h.z < 90 && Dist(h.x, h.y, b.x, b.y) < h.R + 6 && h.Damage(b.dmg * 2, R, ang)) { hit = true; break; }
                if (!hit && cat && cat.Alive && Dist(cat.x, cat.y, b.x, b.y) < cat.R + 6 && cat.Damage(b.dmg, ang, R)) hit = true;
                if (!hit)
                    foreach (var it in ItemMgr.InRange(b.x, b.y, 80))
                        if (it.State == Item.ItemState.Rest && Dist(it.x, it.y, b.x, b.y) < it.R + 4)
                        {
                            hit = true; it.Damage(b.dmg, R, false, ang);
                            if (vis) Fx?.Burst(b.x, b.y, 14, 3, CREAM, Color.white, 80, 200, 2, 3);
                            break;
                        }
                if (hit) { if (vis) Fx?.Stars(b.x, b.y, b.z, 3, CREAM, Color.white, 60, 180); KillProp(b.tr); bullets.RemoveAt(i); }
            }
        }

        // 화면 맨 앞 (방송 화면 소품)
        static void OnTop(UltProp p, int k = 0) => p.sortBias = 32000 + k - World.SortOrder(p.y);

        // 앞발 권총은 리그 자식이라 직접 지움
        public override void Cleanup()
        {
            foreach (var gn in new[] { gunN, gunF }) if (gn != null && gn.hold) Object.Destroy(gn.hold.gameObject);
            gunN = gunF = null;
        }

        public override void Finish()
        {
            bullets.Clear(); gifts.Clear(); shells.Clear();
            R.UltSx = 1; R.z = 0; R.UltJit = 0;
        }
    }
}
