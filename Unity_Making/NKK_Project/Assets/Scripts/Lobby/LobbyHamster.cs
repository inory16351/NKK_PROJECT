using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NKK.Lobby
{
    // 아지트 햄스터 = 쥐 친구 '앙기뮤띠쥐' (95162, 도토리 상점에서 사면 나타남 · 2026-11-05 사용자).
    // 평소 = 무표정으로 앉아 숨 쉬며 가끔 눈 깜빡 · 누르면 움찔 → 폴짝 뛰며 활짝 웃고 따봉 (actTime 초) → 다시 무표정.
    // 그림 = Codex 로 그린 V2 파츠 (Art/Rats/RatFriends/Hamster). 조립 · 움직임 = 웹 미리보기 (UnityResources/.../Hamster/Sheets/web_template.html) 와 같음.
    // 리그는 Awake 에서 Visual 아래에 만들고, 관절 위치는 몸 그림 좌상단 기준 0~1 (아래 값). 이 오브젝트 위치 = 발끝 (바닥).
    //
    // 옮기기 (2026-11-06 사용자: "드래그해서 원하는 위치에 · 자석처럼 · 공중에도 · 치즈 창고에 걸터앉게"):
    //  꾹 눌러 끌면 집어 듦 (손에 매달려 대롱대롱 · 발 바둥바둥) → 놓으면
    //   ① 자석 자리(spots) 가까이면 쏙 끌려가 딱 붙고 따봉 (치즈 창고 칸 = 선반 앞 끝에 걸터앉아 다리 대롱대롱)
    //   ② 바닥 띠 위면 바로 아래 바닥에 톡 앉음  ③ 그 밖(벽 · 공중)이면 그 자리 벽에 찰싹 붙어 발을 살랑살랑
    //  톡 누르기(끌지 않음) = 예전처럼 따봉. 자리는 슬롯별로 저장 (Progress.FriendPlace) → 다시 들어와도 그 자리.
    //  웹 미리보기 = UnityResources/.../Hamster/Sheets/web_drag_template.html (build_drag_web.py → preview/hamster_drag.html). 자리 좌표 = 그 화면 캡처를 월드로 옮긴 값.
    public class LobbyHamster : MonoBehaviour
    {
        [Tooltip("살 때 나타나는 장식 id (도토리 테이블 Deco_Item)")] public int decoId = 95162;
        public LobbyHome home;
        public LobbyPan pan;

        [Header("그림 (피벗 = 관절)")]
        public Sprite body, headNeutral, headBlink, headHappy, arm, armThumb, foot, tail;

        [Header("크기 · 그리는 순서")]
        [Tooltip("발끝 → 머리 꼭대기 키 (게임 단위)")] public float height = 1.05f;
        [Tooltip("바닥에 있을 때 그리는 순서 시작값 (0 = 쥐처럼 높이로 정함: 1000 + 화면 위치 × 2000)")] public int baseOrder = 0;
        [Tooltip("자석 자리(가구 위 · 선반)에 있을 때 그리는 순서 (가구 100 ~ 쳇바퀴 520 보다 앞)")] public int raisedOrder = 650;
        [Tooltip("벽에 붙었거나 들고 있을 때 그리는 순서 (간판 7000 보다 앞, 마녀 쥐 7100 보다 뒤)")] public int wallOrder = 7050;

        [Header("조립 (몸 그림 좌상단 기준 0~1)")]
        public Vector2 neck = new(0.5f, 0.33f);
        public Vector2 shoulderL = new(0.22f, 0.24f), shoulderR = new(0.78f, 0.24f);
        public Vector2 footL = new(0.26f, 0.86f), footR = new(0.74f, 0.86f), tailAt = new(0.80f, 0.86f);
        [Tooltip("몸 대비 크기: 머리 · 팔 · 발 · 따봉 팔")] public float headK = 0.92f, armK = 0.55f, footK = 0.5f, thumbK = 0.6f;
        [Tooltip("따봉 팔 각도: 시작 (내린 상태) → 끝")] public float thumbAng0 = -80f, thumbAng1 = 0f;

        [Header("움직임")]
        [Tooltip("따봉 전체 시간 (초)")] public float actTime = 2.2f;
        [Tooltip("폴짝 높이 (몸 그림 픽셀)")] public float hopPx = 34f;
        [Tooltip("눈 깜빡 주기 (초)")] public float blinkEvery = 3.7f;
        [Tooltip("누르는 범위 여유 (배)")] public float hitPad = 1.0f;

        [Serializable] public class Spot
        {
            [Tooltip("저장용 이름 (바꾸면 예전 저장 자리를 못 찾음)")] public string id;
            [Tooltip("발끝 (월드)")] public Vector2 pos;
            [Tooltip("선반 앞 끝에 걸터앉기 (다리 대롱대롱) · ringY = 선반 윗면")] public bool perch;
            public float ringY;
        }
        [Header("옮기기 (2026-11-06)")]
        [Tooltip("자석 자리 (월드 좌표 = 아지트 화면 캡처에서 옮김)")]
        public Spot[] spots =
        {
            new() { id = "bed", pos = new(-5.918f, -1.156f) },
            new() { id = "cushion_blue", pos = new(-4.024f, -1.238f) },
            new() { id = "table", pos = new(-2.311f, -1.188f) },
            new() { id = "cushion_yellow", pos = new(-0.648f, -1.270f) },
            new() { id = "books", pos = new(0.623f, -0.623f) },
            new() { id = "wheel", pos = new(4.139f, -1.598f) },
            new() { id = "cheese_top", pos = new(6.352f, 1.270f), perch = true, ringY = 1.533f },
            new() { id = "cheese_mid", pos = new(6.352f, 0.049f), perch = true, ringY = 0.311f },
            new() { id = "cheese_low", pos = new(6.352f, -0.934f), perch = true, ringY = -0.672f },
        };
        [Tooltip("바닥 띠 (발끝): 가로 범위 · 세로 범위 (앞 끝 ~ 뒤 끝)")] public Vector2 floorX = new(-7.27f, 7.24f), floorY = new(-2.451f, -1.992f);
        [Tooltip("벽에 붙일 수 있는 가로 범위 · 가장 높은 발끝 (천장 간판 줄 아래)")] public Vector2 wallX = new(-7.1f, 7.07f);
        public float wallTop = 1.615f;
        [Tooltip("자석이 끌어당기기 시작하는 거리 (월드)")] public float magnetRadius = 1.02f;
        [Tooltip("집으면 들리는 높이 · 끌기로 치는 거리 (월드)")] public float lift = 0.13f, dragStart = 0.1f;

        Transform visual, tTail, tFootL, tFootR, tBody, tArmL, tArmR, tThumb, tHead;
        SpriteRenderer rArmR, rThumb, rHead;
        readonly List<(SpriteRenderer r, int off)> parts = new();
        float life, clickAt = -99, rigH, footToCenter, bw, bh, scale;

        enum Where { Floor, Wall, Spot }
        Where where = Where.Floor; int spotIdx = -1;
        bool placeLoaded;
        // 끌기
        bool pressed, holding; Vector2 pressW, pointer, lastPointer, grabOff, magShift, dropFeet; float liftT, pull, vx; int magIdx = -1;
        float swing, swingVel;
        // 날아가기 · 떨어지기
        bool flying, falling; Vector2 flyFrom, flyTo; float flyT, flyDur, flyArc, fallVy; Vector2 fallTo; Action flyDone;
        float squashX, happyUntil = -1;
        // 표시 (자석 자리 링 · 효과 · 그림자)
        LineRenderer[] rings; LineRenderer burst; readonly LineRenderer[] slapLines = new LineRenderer[6];
        float ringFade, burstT = 9, slapT = 9; Vector2 burstAt; bool burstWall;
        SpriteRenderer shadow;
        static Material lineMat;

        static readonly List<LobbyHamster> all = new();
        // 아지트 입력 (LobbyHome): 햄스터를 누르는 중 · 들고 있으면 쥐 · 물건 버튼은 무시
        public static bool Busy { get { foreach (var h in all) if (h.pressed || h.holding) return true; return false; } }
        public static bool HitAny(Vector2 w) { foreach (var h in all) if (h.Shown && h.Hit(w)) return true; return false; }

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() { all.Remove(this); pressed = holding = false; }

        void Awake()
        {
            if (!body) { enabled = false; return; }
            bw = body.rect.width; bh = body.rect.height;
            float fh = foot ? foot.rect.height : 0, hh = headNeutral ? headNeutral.rect.height : 0;
            footToCenter = (footL.y - 0.5f) * bh + fh * footK * 0.96f;          // 몸 중심 → 발끝 (픽셀, 아래로)
            float headTop = (neck.y - 0.5f) * bh - hh * headK * 0.96f;            // 몸 중심 → 머리 꼭대기 (픽셀, 위로 = 음수)
            rigH = footToCenter - headTop;
            scale = height / (rigH / 100f);

            visual = new GameObject("Visual").transform; visual.SetParent(transform, false);
            tTail = Part("tail", tail, 0, out _);
            tFootL = Part("footL", foot, 1, out _);
            tFootR = Part("footR", foot, 2, out _);
            tBody = Part("body", body, 3, out _);
            tArmL = Part("armL", arm, 4, out _);
            tArmR = Part("armR", arm, 5, out rArmR);
            tThumb = Part("thumb", armThumb, 5, out rThumb);
            tHead = Part("head", headNeutral, 6, out rHead);
            BuildMarks();
            ApplyOrder();
            Update();
        }

        Transform Part(string n, Sprite s, int off, out SpriteRenderer r)
        {
            var go = new GameObject(n); go.transform.SetParent(visual, false);
            r = go.AddComponent<SpriteRenderer>(); r.sprite = s;
            parts.Add((r, off));
            return go.transform;
        }

        bool Owned => Progress.I && Progress.I.OwnsDeco(decoId);
        bool Shown => visual && visual.gameObject.activeSelf;
        bool Moving => holding || flying || falling;

        // 그리는 순서: 바닥 = 쥐처럼 높이로 · 가구 위 = raisedOrder · 벽 / 들고 있음 = wallOrder
        int OrderNow()
        {
            if (Moving || where == Where.Wall) return wallOrder;
            if (where == Where.Spot) return raisedOrder;
            return baseOrder != 0 ? baseOrder : home ? 1000 + Mathf.RoundToInt(home.ToUV(transform.position).y * 2000) : 2500;
        }
        int lastOrder = int.MinValue;
        void ApplyOrder()
        {
            int o = OrderNow(); if (o == lastOrder) return; lastOrder = o;
            foreach (var (r, off) in parts) r.sortingOrder = o + off;
            if (shadow) shadow.sortingOrder = o - 1;
        }

        // 몸 그림 위 한 점 (좌상단 0~1) → Visual 안 좌표 (몸 중심 기준, 몸 기울기 ang)
        Vector3 At(Vector2 a, float ang) => Quaternion.Euler(0, 0, ang) * new Vector3((a.x - 0.5f) * bw, -(a.y - 0.5f) * bh, 0) / 100f;

        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
        static float Back(float t) { t = Mathf.Clamp01(t); const float c = 2.2f; return 1 + (c + 1) * Mathf.Pow(t - 1, 3) + c * Mathf.Pow(t - 1, 2); }

        static void Set(Transform t, Vector3 p, float ang, float k, bool flip = false)
        {
            t.localPosition = p; t.localRotation = Quaternion.Euler(0, 0, ang); t.localScale = new Vector3(flip ? -k : k, k, 1);
        }

        void Update()
        {
            if (!visual) return;
            bool show = Owned;
            if (visual.gameObject.activeSelf != show) visual.gameObject.SetActive(show);
            if (shadow && shadow.gameObject.activeSelf != show) shadow.gameObject.SetActive(show);
            if (!show) { clickAt = -99; pressed = holding = false; ringFade = 0; ShowMarks(0); return; }
            if (!placeLoaded) LoadPlace();
            float dt = Time.deltaTime; life += dt;
            HandleInput();
            StepMotion(dt);
            Pose();
            ApplyOrder();
            StepMarks(dt);
        }

        // ── 입력: 톡 = 따봉 · 끌기 = 집어서 옮기기 ──
        bool InputOk()
        {
            if (!home || !home.cam || (pan && pan.AtRoom) || GachaShow.Busy) return false;
            var blockedBy = home.main ? home.main.inputBlocked : home.inputBlocked;
            return blockedBy == null || !blockedBy();
        }

        bool Hit(Vector2 w)
        {
            if (flying || falling) return false;
            Vector2 p = transform.position;
            float hw = bw * 1.1f / 100f * scale * 0.5f * hitPad, hgt = height * hitPad;
            return w.x > p.x - hw && w.x < p.x + hw && w.y > p.y - 0.05f && w.y < p.y + hgt;
        }

        void HandleInput()
        {
            var m = Mouse.current; if (m == null) return;
            if (!InputOk()) { if (holding) Release(); pressed = false; return; }
            Vector2 w = home.cam.ScreenToWorldPoint(m.position.ReadValue());
            pointer = w;
            if (m.leftButton.wasPressedThisFrame && Hit(w)) { pressed = true; pressW = w; }
            if (pressed && !holding && m.leftButton.isPressed && Vector2.Distance(w, pressW) > dragStart) Grab();
            if (m.leftButton.wasReleasedThisFrame)
            {
                if (holding) Release();
                else if (pressed && life - clickAt >= actTime) { clickAt = life; SfxManager.Play("ui_click"); }
                pressed = false;
            }
        }

        void Grab()
        {
            holding = true; pressed = false;
            grabOff = (Vector2)transform.position - pressW; lastPointer = pointer; vx = 0; dropFeet = transform.position;
            liftT = 0; pull = 0; magIdx = -1; magShift = Vector2.zero; clickAt = -99; squashX = 0;
            where = Where.Floor; spotIdx = -1;
            SfxManager.Play("ui_click");
        }

        int NearestSpot(Vector2 p, out float dist)
        {
            int best = -1; dist = magnetRadius;
            for (int i = 0; i < spots.Length; i++)
            {
                var d = spots[i].pos - p; float dd = Mathf.Sqrt(d.x * d.x + d.y * 1.2f * d.y * 1.2f);
                if (dd < dist) { dist = dd; best = i; }
            }
            return best;
        }

        Vector2 Feet => transform.position;

        void Release()
        {
            holding = false;
            var f = dropFeet;           // 들린 높이 · 흔들림 뺀 발끝 (바닥 뒤쪽에 놓았는데 벽으로 치지 않게)
            if (magIdx >= 0) { int i = magIdx; FlyTo(spots[i].pos, 0.2f, 0.15f, () => LandSpot(i)); return; }
            if (f.y > floorY.y + 0.05f) { StickWall(new Vector2(Mathf.Clamp(f.x, wallX.x, wallX.y), Mathf.Clamp(f.y, floorY.y + 0.05f, wallTop))); return; }
            falling = true; fallVy = 1f;
            fallTo = new Vector2(Mathf.Clamp(f.x, floorX.x, floorX.y), Mathf.Clamp(f.y, floorY.x, floorY.y));
        }

        void FlyTo(Vector2 to, float dur, float arc, Action done)
        {
            flying = true; flyFrom = Feet; flyTo = to; flyT = 0; flyDur = dur; flyArc = arc; flyDone = done;
        }

        void LandSpot(int i)
        {
            where = Where.Spot; spotIdx = i; squashX = 0.8f; clickAt = life - 0.1f;       // 딱 붙으면 따봉
            var s = spots[i];
            Burst(new Vector2(s.pos.x, s.perch ? s.ringY : s.pos.y), false);
            if (home) for (int k = 0; k < 6; k++) { var uv = home.ToUV(new Vector3(s.pos.x, s.pos.y + height * 0.5f, 0)); home.Fx(LobbyHome.FxKind.Crumb, uv.x + (k - 2.5f) * 0.006f, uv.y, (k - 2.5f) * 0.012f, 0.6f); }
            SfxManager.Play("ui_click");
            SavePlace();
        }

        void StickWall(Vector2 p)
        {
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            where = Where.Wall; spotIdx = -1; squashX = 0.7f; happyUntil = life + 0.6f;
            Burst(new Vector2(p.x, p.y + height * 0.45f), true);
            SfxManager.Play("ui_click");
            SavePlace();
        }

        void LandFloor()
        {
            where = Where.Floor; spotIdx = -1; squashX = 1; happyUntil = life + 0.5f;
            if (home) { var uv = home.ToUV(transform.position); for (int k = 0; k < 5; k++) home.Fx(LobbyHome.FxKind.Dust, uv.x + (k - 2) * 0.008f, uv.y, (k - 2) * 0.01f, 0.5f); }
            SavePlace();
        }

        void StepMotion(float dt)
        {
            if (holding)
            {
                if (dt > 0) { float v = (pointer.x - lastPointer.x) / dt; vx += (v - vx) * Mathf.Min(1, dt * 14); }
                lastPointer = pointer;
                liftT = Mathf.Min(1, liftT + dt * 8);
                var free = pointer + grabOff + Vector2.up * lift * Ease(liftT);
                magIdx = NearestSpot(free, out float d);
                float pullT = magIdx >= 0 ? Ease(1 - d / magnetRadius) : 0;
                pull += (pullT - pull) * Mathf.Min(1, dt * 16);
                magShift = magIdx >= 0 ? (spots[magIdx].pos - free) * 0.55f * pull : magShift * (1 - Mathf.Min(1, dt * 12));
                var feet = free + magShift;
                dropFeet = pointer + grabOff + magShift;
                // 손에 매달려 대롱대롱: 손이 가는 쪽 반대로 기울었다 되돌아옴 (축 = 손)
                float target = Mathf.Clamp(-vx * 1.46f, -28, 28) * (1 - 0.6f * pull);
                swingVel += ((target - swing) * 70 - swingVel * 7) * dt; swing += swingVel * dt;
                var pivot = pointer + magShift;
                var rot = Quaternion.Euler(0, 0, swing);
                transform.SetPositionAndRotation((Vector3)(pivot + (Vector2)(rot * (feet - pivot))) + Vector3.forward * transform.position.z, rot);
                return;
            }
            swingVel += ((0 - swing) * 90 - swingVel * 10) * dt; swing += swingVel * dt;
            transform.rotation = Quaternion.Euler(0, 0, swing);
            if (flying)
            {
                flyT += dt; float k = Mathf.Min(1, flyT / flyDur), e = 1 - Mathf.Pow(1 - k, 3);
                var p = Vector2.Lerp(flyFrom, flyTo, e) + Vector2.up * Mathf.Sin(k * Mathf.PI) * flyArc;
                transform.position = new Vector3(p.x, p.y, transform.position.z);
                if (k >= 1) { flying = false; var d = flyDone; flyDone = null; d?.Invoke(); }
            }
            if (falling)
            {
                fallVy -= 26f * dt;
                var p = Feet; p.y += fallVy * dt; p.x += (fallTo.x - p.x) * Mathf.Min(1, dt * 10);
                if (p.y <= fallTo.y) { p = fallTo; falling = false; transform.position = new Vector3(p.x, p.y, transform.position.z); LandFloor(); }
                else transform.position = new Vector3(p.x, p.y, transform.position.z);
            }
            squashX = Mathf.Max(0, squashX - dt * 4);
        }

        // ── 자세 (예전 움직임 + 들림 · 벽 · 걸터앉기) ──
        void Pose()
        {
            float ta = life - clickAt; bool act = ta >= 0 && ta < actTime;
            bool perch = !Moving && where == Where.Spot && spotIdx >= 0 && spots[spotIdx].perch;
            bool wall = !Moving && where == Where.Wall;
            float t = life, breathe = Mathf.Sin(t * Mathf.PI * 2 * 0.6f);
            float squash = 0, hop = 0, thumb = 0; bool happy = (holding && magIdx >= 0 && pull > 0.25f) || life < happyUntil;
            if (act)
            {
                squash = ta < 0.12f ? Ease(ta / 0.12f) : ta < 0.3f ? 1 - Ease((ta - 0.12f) / 0.18f) : 0;                 // 움찔
                hop = ta > 0.12f && ta < 0.55f ? Mathf.Sin((ta - 0.12f) / 0.43f * Mathf.PI) * hopPx : 0;                // 폴짝
                thumb = ta < 0.15f ? 0 : ta < actTime - 0.35f ? Back((ta - 0.15f) / 0.3f) : 1 - Ease((ta - (actTime - 0.35f)) / 0.3f);
                happy |= ta > 0.1f && ta < actTime - 0.2f;
            }
            squash = Mathf.Max(squash, squashX);
            if (holding || wall || perch) hop = 0;              // 벽 · 선반 끝에서는 따봉 때도 제자리 (떨어지지 않게)
            float kick = holding ? 1 : 0, sway = wall || perch ? 1 : 0;      // 들림 = 바둥바둥 · 벽 / 걸터앉기 = 살랑살랑
            // Visual = 몸 중심 (발끝에서 위로), 움찔하면 납작 · 숨쉬기 · 들리면 살짝 늘어남
            float sy = 1 - 0.1f * squash + 0.012f * breathe + (holding ? 0.05f : 0), sx = 1 + 0.08f * squash - (holding ? 0.03f : 0);
            visual.localPosition = new Vector3(0, footToCenter / 100f * scale, 0);
            visual.localScale = new Vector3(scale * sx, scale * sy, 1);
            var c = new Vector3(0, hop / 100f, 0);
            float aBody = act || holding ? 0 : 2.5f * Mathf.Sin(t * Mathf.PI * 2 * 0.35f);

            Set(tTail, c + At(tailAt, aBody), aBody + (8 + 14 * kick) * Mathf.Sin(t * Mathf.PI * 2 * (1.5f + 2.5f * kick)), 1);
            Set(tFootL, c + At(footL, aBody), -6 + kick * 24 * Mathf.Sin(t * Mathf.PI * 2 * 3.4f) + sway * 12 * Mathf.Sin(t * Mathf.PI * 2 * 0.9f), footK);
            Set(tFootR, c + At(footR, aBody), 6 + kick * 24 * Mathf.Sin(t * Mathf.PI * 2 * 3.4f + 1.6f) + sway * 12 * Mathf.Sin(t * Mathf.PI * 2 * 0.9f + 1.3f), footK, true);
            Set(tBody, c, aBody, 1);
            float wig = (4 + 12 * kick) * Mathf.Sin(t * Mathf.PI * 2 * (1.2f + 1.6f * kick));
            // 그림의 팔 = 화면 오른쪽 팔 → 왼쪽은 좌우 반전 (피벗이 거울 위치로 감)
            Set(tArmL, c + At(shoulderL, aBody), aBody - wig, armK, true);
            var shR = c + At(shoulderR, aBody);
            bool up = thumb > 0.05f;
            rArmR.enabled = !up; rThumb.enabled = up;
            if (up)
            {
                float a = thumbAng0 + (thumbAng1 - thumbAng0) * thumb + (thumb > 0.9f ? 4 * Mathf.Sin(ta * Mathf.PI * 2 * 3) : 0);
                Set(tThumb, shR, aBody + a, thumbK);
            }
            else Set(tArmR, shR, aBody + wig, armK);
            bool blink = !act && !happy && (t % blinkEvery) > blinkEvery - 0.15f;
            rHead.sprite = happy ? headHappy : blink ? headBlink : headNeutral;
            float tilt = act ? 6 * thumb : holding ? 5 * Mathf.Sin(t * Mathf.PI * 2 * 1.3f) : 3 * Mathf.Sin(t * Mathf.PI * 2 * 0.5f);
            Set(tHead, c + At(neck, aBody) + new Vector3(0, 1.5f * breathe / 100f, 0), aBody * 1.4f + tilt, headK);
        }

        // ── 저장 (슬롯별) ──
        void LoadPlace()
        {
            if (!Progress.I) return;
            placeLoaded = true;
            var p = Progress.I.FriendPlaceOf(decoId); if (p == null) return;       // 저장 없음 = 씬에 놓인 처음 자리
            if (p.where == 2)
            {
                int i = Array.FindIndex(spots, s => s.id == p.spot); if (i < 0) return;
                where = Where.Spot; spotIdx = i; transform.position = new Vector3(spots[i].pos.x, spots[i].pos.y, transform.position.z);
            }
            else if (p.where == 1)
            {
                where = Where.Wall; transform.position = new Vector3(Mathf.Clamp(p.x, wallX.x, wallX.y), Mathf.Clamp(p.y, floorY.y + 0.05f, wallTop), transform.position.z);
            }
            else
            {
                where = Where.Floor; transform.position = new Vector3(Mathf.Clamp(p.x, floorX.x, floorX.y), Mathf.Clamp(p.y, floorY.x, floorY.y), transform.position.z);
            }
        }

        void SavePlace()
        {
            if (!Progress.I) return;
            Vector2 f = Feet;
            Progress.I.SetFriendPlace(new Progress.FriendPlace { id = decoId, where = (int)where, spot = where == Where.Spot && spotIdx >= 0 ? spots[spotIdx].id : null, x = f.x, y = f.y });
        }

        // ── 표시: 자석 자리 링 (들고 있을 때) · 딱 / 찰싹 효과 · 그림자 ──
        static LineRenderer Line(Transform parent, string n, int pts, bool loop)
        {
            if (!lineMat) lineMat = new Material(Shader.Find("Sprites/Default"));
            var lr = new GameObject(n).AddComponent<LineRenderer>(); lr.transform.SetParent(parent, false);
            lr.sharedMaterial = lineMat; lr.useWorldSpace = true; lr.loop = loop; lr.positionCount = pts;
            lr.numCapVertices = 4; lr.numCornerVertices = 2; lr.textureMode = LineTextureMode.Stretch;
            lr.enabled = false;
            return lr;
        }

        void BuildMarks()
        {
            var root = new GameObject("HamsterMarks").transform; root.SetParent(transform.parent, false);
            rings = new LineRenderer[spots.Length];
            for (int i = 0; i < spots.Length; i++) rings[i] = Line(root, "Ring_" + spots[i].id, 40, true);
            burst = Line(root, "Burst", 40, true);
            for (int i = 0; i < slapLines.Length; i++) slapLines[i] = Line(root, "Slap" + i, 2, false);
            // 그림자: 아지트 쥐 그림자 그림을 빌려 씀 (없으면 그림자 없음)
            var go = new GameObject("HamsterShadow"); go.transform.SetParent(root, false);
            shadow = go.AddComponent<SpriteRenderer>(); shadow.color = new Color(0.17f, 0.13f, 0.09f, 0.2f);
        }

        static void Ellipse(LineRenderer lr, Vector2 c, float rx, float ry)
        {
            int n = lr.positionCount;
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; lr.SetPosition(i, new Vector3(c.x + Mathf.Cos(a) * rx, c.y + Mathf.Sin(a) * ry, 0)); }
        }

        void ShowMarks(float k)
        {
            if (rings != null) foreach (var r in rings) if (r) r.enabled = k > 0.01f;
        }

        void StepMarks(float dt)
        {
            ringFade = Mathf.MoveTowards(ringFade, holding || flying ? 1 : 0, dt * 6);
            ShowMarks(ringFade);
            if (ringFade > 0.01f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(life * Mathf.PI * 2 * 2.2f);
                for (int i = 0; i < spots.Length; i++)
                {
                    var s = spots[i]; var r = rings[i]; bool on = holding && magIdx == i;
                    var cc = new Vector2(s.pos.x, s.perch ? s.ringY : s.pos.y);
                    Ellipse(r, cc, on ? 0.51f : 0.43f, on ? 0.16f : 0.13f);
                    float w = on ? 0.05f : 0.035f; r.startWidth = r.endWidth = w;
                    var col = on ? new Color(1f, 0.95f, 0.42f, (0.75f + 0.25f * pulse) * ringFade) : new Color(1, 1, 1, 0.75f * ringFade);
                    r.startColor = r.endColor = col; r.sortingOrder = raisedOrder - 1;
                }
            }
            // 딱 (노란 납작 링이 퍼짐) · 찰싹 (흰 동그라미 + 몸 둘레 짧은 효과선)
            burstT += dt;
            float bk = burstT / 0.6f;
            burst.enabled = bk < 1;
            if (bk < 1)
            {
                float a = 1 - bk;
                if (burstWall) Ellipse(burst, burstAt, 0.41f + 0.49f * bk, 0.46f + 0.54f * bk);
                else Ellipse(burst, burstAt, 0.33f + 0.57f * bk, 0.1f + 0.18f * bk);
                burst.startWidth = burst.endWidth = 0.05f * a + 0.01f;
                var bc = burstWall ? new Color(1, 1, 1, a) : new Color(1f, 0.95f, 0.42f, a);
                burst.startColor = burst.endColor = bc; burst.sortingOrder = wallOrder + 10;
            }
            slapT += dt;
            float sk = slapT / 0.5f;
            for (int i = 0; i < slapLines.Length; i++)
            {
                var l = slapLines[i]; l.enabled = burstWall && sk < 1; if (!l.enabled) continue;
                float ang = Mathf.PI / 2 + (i - 2.5f) * 0.55f, r0 = bw * 1.1f / 100f * scale * 0.62f + 0.13f * sk, r1 = r0 + 0.18f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                l.SetPosition(0, burstAt + dir * r0); l.SetPosition(1, burstAt + dir * r1);
                l.startWidth = l.endWidth = 0.05f; l.startColor = l.endColor = new Color(1, 1, 1, 1 - sk); l.sortingOrder = wallOrder + 10;
            }
            UpdateShadow();
        }

        void Burst(Vector2 at, bool wall) { burstAt = at; burstWall = wall; burstT = 0; if (wall) slapT = 0; }

        void UpdateShadow()
        {
            if (!shadow) return;
            if (!shadow.sprite && home) foreach (var a in home.actors) if (a && a.shadow && a.shadow.sprite) { shadow.sprite = a.shadow.sprite; break; }
            if (!shadow.sprite) { shadow.enabled = false; return; }
            float sz = shadow.sprite.bounds.size.x, w = bw * 1.1f / 100f * scale;
            Vector2 at; float sx, sy, alpha; int order;
            if (holding)
            {
                // 놓으면 갈 곳에 흐리게
                var f = dropFeet;
                if (magIdx >= 0) { var s = spots[magIdx]; at = new Vector2(s.pos.x, s.perch ? s.ringY : s.pos.y); sx = w * 0.75f; sy = w * 0.18f; alpha = 0.16f; order = raisedOrder - 2; }
                else if (f.y > floorY.y + 0.05f) { at = f + new Vector2(0.08f, height * 0.45f - 0.1f); sx = w * 0.85f; sy = height * 0.85f; alpha = 0.1f; order = wallOrder - 1; }
                else { at = new Vector2(Mathf.Clamp(f.x, floorX.x, floorX.y), Mathf.Clamp(f.y, floorY.x, floorY.y)); sx = w * 0.6f; sy = w * 0.15f; alpha = 0.14f; order = lastOrder - 1; }
            }
            else if (falling) { at = fallTo; sx = w * 0.75f; sy = w * 0.18f; alpha = 0.2f; order = lastOrder - 1; }
            else if (where == Where.Wall) { at = Feet + new Vector2(0.08f, height * 0.45f - 0.1f); sx = w * 0.85f; sy = height * 0.85f; alpha = 0.18f; order = lastOrder - 1; }      // 벽: 몸 뒤 벽에 납작 그림자
            else if (where == Where.Spot && spotIdx >= 0 && spots[spotIdx].perch) { var s = spots[spotIdx]; at = new Vector2(s.pos.x, s.ringY); sx = w * 0.7f; sy = w * 0.16f; alpha = 0.2f; order = lastOrder - 1; }
            else { at = Feet; sx = w * 0.75f; sy = w * 0.18f; alpha = 0.2f; order = lastOrder - 1; }
            shadow.enabled = !flying;
            shadow.transform.position = new Vector3(at.x, at.y, 0);
            shadow.transform.localScale = new Vector3(sx / sz, sy / sz, 1);
            shadow.color = new Color(0.17f, 0.13f, 0.09f, alpha);
            shadow.sortingOrder = order;
        }
    }
}
