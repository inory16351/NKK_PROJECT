using NKK.Data;
using NKK.Rats;
using NKK.Stage;
using UnityEngine;

namespace NKK.Hazards
{
    // 깜짝 보스 (94 그렘린 · 97 우주복 쥐, 보스 개편 README · 2026-10-22). 미리 보스 층이라 안 알림 (SurpriseFloors · Surprise).
    //  94 (고양이 리그): 시작 방에 알 → 흔들흔들 금 → 펑, 보통 고양이(disguiseCat)로 나와 쥐에게 애교 (공격 안 받음)
    //  97 (그 밖):        층에 들어와 joinDelay 초 뒤 "신화 우주복 쥐 획득!" → 우주복 쥐(disguiseRat)가 쥐 무리를 따라다님
    //  → 계단 벽을 부수고 쥐가 계단에 닿으면: 카메라가 보스로 + 찌지직 + 화면 흔들림 (revealTime) → 진짜 보스 그림으로 작게 → 커지며 보스전
    //    (97 은 보스전 시작 때 발악 1 = 촉수 삼키기, opt intro)
    // 글 = 보스 테이블 Boss_Line: Join (획득 배너) · Disguise (변장 중 혼잣말) · Reveal (변신 외침)
    public partial class Boss
    {
        [Header("깜짝 보스 (Boss.Surprise.cs, 94·97층)")]
        [Tooltip("알 · 금 간 알 · 깨진 껍질 그림")] public Sprite eggSprite, eggCrackSprite, eggShellsSprite;
        [Tooltip("알 크기 (게임 단위) · 시작 방 가운데에서 떨어진 자리")] public float eggSize = 90;
        public Vector2 eggOffset = new(220, -40);
        [Tooltip("알이 깨질 때까지 (초) · 그 전 흔들흔들 금 가는 시간 (초)")] public float eggTime = 10, eggCrackTime = 2;
        [Tooltip("94층: 알에서 나오는 보통 고양이 (고양이 테이블 code_id)")] public string disguiseCat = "cheese";
        [Tooltip("97층: 동료로 들어오는 쥐 그림 (쥐 그림 모음 code_id) · 몸길이 · 들어오는 시각 (층 들어와서 초)")] public string disguiseRat = "crewmate";
        public float disguiseRatLength = 46, joinDelay = 8;
        [Tooltip("변장 중 걷는 빠르기 · 혼잣말 간격 (초)")] public float disguiseSpeed = 80, disguiseSayEvery = 4;
        [Tooltip("변신: 쥐가 계단에서 이 거리 안이면 시작 · 찌지직 시간 (초) · 변신 직후 크기 (점점 원래 크기로)")] public float revealRange = 110, revealTime = 1.4f, revealStartScale = 0.3f;
        [Tooltip("찌지직 번개 색 · 애교 하트 색 · 획득 번쩍 색")] public Color revealColor = new(0.6f, 0.95f, 0.5f), loveColor = new(1f, 0.55f, 0.7f), joinColor = new(1f, 0.82f, 0.35f);

        enum Sneak { None, Egg, Pending, Disguised, Reveal }
        Sneak sneak;
        float sneakT, sneakSay, sneakGoalT, sneakRest;
        Vector2 sneakGoal;
        bool sneakArmed, disguiseRig;
        float disguiseLen;
        SpriteRenderer egg;

        bool SneakShown => sneak == Sneak.Disguised || sneak == Sneak.Reveal;
        float ShadowR => disguiseRig ? disguiseLen * 0.35f : R;

        // OnFloorEnter: 깜짝 보스면 대기 대신 변장 시작
        void BeginSneak()
        {
            sneakT = 0; sneakArmed = false; sneakRest = 0; sneakGoalT = 0;
            if (Data.IsCat)
            {
                x = World.RW / 2 + eggOffset.x; y = World.RH / 2 + eggOffset.y;
                sneak = Sneak.Egg;
                if (shotTemplate && eggSprite) { egg = Instantiate(shotTemplate, shotTemplate.transform.parent); egg.gameObject.SetActive(true); egg.enabled = true; egg.sprite = eggSprite; }
            }
            else sneak = Sneak.Pending;
        }

        void ClearSneak()
        {
            sneak = Sneak.None; sneakArmed = false;
            if (disguiseRig) { disguiseRig = false; RebuildRealRig(); }
            if (egg) { Destroy(egg.gameObject); egg = null; }
        }

        // 계단 방이 열림: 깜짝 보스는 바로 싸우지 않고 쥐가 계단에 닿기를 기다림 (아직 알·합류 전이면 바로 나옴)
        bool SneakStairsOpened()
        {
            if (sneak == Sneak.None) return false;
            if (sneak == Sneak.Egg) Hatch();
            else if (sneak == Sneak.Pending) Join();
            sneakArmed = true;
            return true;
        }

        // State == Wait 동안 매 프레임 (Update). 처리했으면 true
        bool UpdateSneak(float dt)
        {
            if (sneak == Sneak.None) return false;
            sneakT += dt; sneakSay -= dt;
            var fx = FxManager.I;
            switch (sneak)
            {
                case Sneak.Egg:
                {
                    vx = vy = 0;
                    float left = eggTime - sneakT;
                    if (egg)
                    {
                        bool crack = left < eggCrackTime;
                        if (crack && eggCrackSprite && egg.sprite != eggCrackSprite) { egg.sprite = eggCrackSprite; SfxManager.PlayAt("hit_glass", x, y); }
                        float wob = crack ? Mathf.Sin(sneakT * 30) * 12 * (1 - left / eggCrackTime) : Mathf.Sin(sneakT * 2) * 2;
                        float w = eggSize * World.U / Mathf.Max(0.001f, egg.sprite.bounds.size.x);
                        egg.transform.position = World.ToUnity(x, y, eggSize * 0.5f);
                        egg.transform.rotation = Quaternion.Euler(0, 0, wob);
                        egg.transform.localScale = new Vector3(w, w, 1);
                        egg.sortingOrder = World.SortOrder(y);
                    }
                    if (left <= 0) Hatch();
                    return true;
                }
                case Sneak.Pending:
                    vx = vy = 0;
                    if (sneakT >= joinDelay) Join();
                    return true;
                case Sneak.Disguised:
                {
                    DisguiseWalk(dt);
                    if (sneakSay <= 0) { sneakSay = disguiseSayEvery * Random.Range(0.7f, 1.3f); sayCD = 0; Say(DB.BossLine("Disguise", Data.boss_id), 1.4f); }
                    if (Data.IsCat && fx && Random.value < dt * 1.5f) fx.Stars(x, y, disguiseLen * 0.8f, 2, loveColor, Color.white, 30, 80);     // 애교 하트
                    if (sneakArmed)
                    {
                        var s = Stage.StairsPos; float r2 = revealRange * revealRange;
                        foreach (var o in Rats.Rats) if (!o.UltOn && (o.x - s.x) * (o.x - s.x) + (o.y - s.y) * (o.y - s.y) < r2) { BeginReveal(); break; }
                    }
                    return true;
                }
                case Sneak.Reveal:
                {
                    vx = vy = 0;
                    float k = Mathf.Clamp01(sneakT / revealTime);
                    jit = 2 + 6 * k;
                    if (fx && Random.value < dt * (10 + 30 * k))
                    {
                        fx.Crackle(x, y, disguiseLen * 0.5f, disguiseLen * (0.8f + k), revealColor, 2, 3, 0.1f);
                        fx.Spark(x + Random.Range(-1f, 1f) * disguiseLen, y, Random.Range(10f, disguiseLen), revealColor, 40 + 40 * k, 0.15f);
                    }
                    if (fx && Random.value < dt * 8) fx.Shake(0.05f + 0.3f * k);
                    if (Mathf.Repeat(sneakT, 0.35f) < dt) SfxManager.PlayAt("zap", x, y);
                    if (sneakT >= revealTime) Unmask();
                    return true;
                }
            }
            return true;
        }

        void Hatch()
        {
            var fx = FxManager.I;
            if (egg)
            {
                if (eggShellsSprite) { egg.sprite = eggShellsSprite; egg.transform.rotation = Quaternion.identity; }
                Destroy(egg.gameObject, 1.2f); egg = null;
            }
            fx?.Anim("poof", x, y, 0, 1.2f); fx?.Burst(x, y, eggSize * 0.5f, 14, Color.white, new Color(1f, 0.95f, 0.8f), 80, 220, 3, 7);
            SfxManager.PlayAt("cat_meow", x, y);
            Disguise(Cats && Cats.catArt ? Cats.catArt.Get(disguiseCat) : null, Cats ? Cats.catLength : 115);
        }

        void Join()
        {
            // 쥐 무리 한가운데로 (없으면 시작 방)
            Vector2 at = Cats && Cats.FindCrowd(World.RW / 2, World.RH / 2, out var c, out _) ? c : new Vector2(World.RW / 2, World.RH / 2);
            x = at.x + Random.Range(-60f, 60f); y = at.y + Random.Range(-40f, 40f);
            Disguise(Rats.artLibrary ? Rats.artLibrary.Get(disguiseRat) : null, disguiseRatLength);
            var line = DB.BossLine("Join", Data.boss_id);
            if (!string.IsNullOrEmpty(line)) { Game.ShowBanner(line, ""); sayCD = 0; Say(line, 2.5f); sneakSay = 3; }      // 벽 붕괴 배너에 덮여도 보이게 머리 위에도
            if (Ults) Ults.Flash(joinColor, 0.35f);
            var fx = FxManager.I;
            fx?.Anim("poof", x, y, 0, 1); fx?.Stars(x, y, 30, 24, joinColor, Color.white, 120, 360);
            SfxManager.Play("jgl_unlock");
        }

        // 변장 그림 (고양이 리그 = RatRig 에 보통 고양이 · 쥐 그림). 그림이 없으면 바로 변신 대기 없이 숨은 채로 (계단이 열리면 보스전)
        void Disguise(RatArtLibrary.Entry art, float len)
        {
            if (art == null || !catRig) { Debug.LogWarning("[Boss] 깜짝 보스 변장 그림 없음 → 숨은 채 대기"); sneak = Sneak.None; return; }
            catRig.Build(art, len, -1, false);
            disguiseRig = true; disguiseLen = len; sneak = Sneak.Disguised; sneakSay = 1.5f; sneakGoalT = 0;
            RefreshBaseColors();
        }

        // 변장 중 걷기: 쥐 하나 골라 그 옆으로 → 잠깐 섬 (고양이는 애교, 쥐는 무리 따라감)
        void DisguiseWalk(float dt)
        {
            float px = x, py = y;
            sneakGoalT -= dt;
            float gx = sneakGoal.x - x, gy = sneakGoal.y - y, gd = Mathf.Sqrt(gx * gx + gy * gy);
            if (sneakRest > 0) { sneakRest -= dt; gd = 0; }
            else if (gd < 20 || sneakGoalT <= 0)
            {
                if (gd < 20) sneakRest = Random.Range(0.8f, 2f);
                sneakGoalT = 3;
                var r = Rats.RandomNear(x, y, 900) ?? Rats.NearestRat(x, y, 3000);
                sneakGoal = r ? new Vector2(r.x + Random.Range(-50f, 50f), r.y + Random.Range(-35f, 35f)) : new Vector2(x, y);
            }
            float s = gd > 1 ? disguiseSpeed * Mathf.Clamp01(gd / 40) : 0, k = Mathf.Min(1, dt * 4);
            vx += ((gd > 1 ? gx / gd * s : 0) - vx) * k; vy += ((gd > 1 ? gy / gd * s : 0) - vy) * k;
            x += vx * dt; y += vy * dt;
            Stage.Confine(ref x, ref y, ref vx, ref vy, disguiseLen * 0.35f, px, py, 0.3f);
            if (Mathf.Abs(vx) > 8) face = vx > 0 ? 1 : -1;
            walk += dt * Mathf.Sqrt(vx * vx + vy * vy) / 10;
        }

        void BeginReveal()
        {
            sneak = Sneak.Reveal; sneakT = 0; vx = vy = 0;
            if (Game.cam) Game.cam.CenterOn(x, y);
            sayCD = 0; Say(DB.BossLine("Reveal", Data.boss_id), revealTime + 0.4f);
        }

        // 진짜 보스 그림으로 (작게 → UpdateGrow 가 원래 크기로 키움) → 보스전
        void Unmask()
        {
            sneak = Sneak.None; disguiseRig = false; jit = 0;
            RebuildRealRig();
            growK = revealStartScale;
            var fx = FxManager.I;
            fx?.Anim("poof", x, y, 0, 2.5f); fx?.Shake(0.6f); fx?.Hitstop(0.08f);
            if (Ults) Ults.Flash(revealColor, 0.4f);
            SfxManager.PlayAt("boss_slam", x, y);
            StartFight();
        }

        void RebuildRealRig()
        {
            if (Data == null) return;
            if (Data.IsCat && catRig && Cats && Cats.catArt)
            {
                var art = Cats.catArt.Get(Data.code_id);
                if (art != null) { catRig.Build(art, Cats.catLength * Data.scale, -1, false); catArtEntry = art; }
            }
            RefreshBaseColors();
        }

        void RefreshBaseColors()
        {
            baseColors.Clear();
            if (Part && partRig) partRig.BaseColors(baseColors);
            else foreach (var r in (Cat ? (Component)catRig : rig).GetComponentsInChildren<SpriteRenderer>(true)) baseColors.Add((r, r.color));
        }
    }
}
