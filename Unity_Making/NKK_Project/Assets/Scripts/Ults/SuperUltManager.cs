using System.Collections.Generic;
using NKK.Data;
using NKK.Hazards;
using NKK.Items;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Ults
{
    /// <summary>
    /// 초필살기 (기획서 20261016, 2026-10-17 1단계): 신화 15종만, 판 한 번의 클라이맥스.
    /// · 자격 = 신화 + 그 종의 쳇바퀴 훈련 Lv(Super_Config unlock_level) 이상. 임시 쥐(소환·필살기용)는 제외.
    /// · 스택 = 그 종이 자기 필살기를 끝까지 쓸 때마다 +1 (UltimateManager.Stop → OnUltDone, 종마다 · 화면엔 안 보임). stack_need 개면 버튼.
    /// · 수동만 (필살기 자동 사용 공용 스킬 무시), 종마다 판당 1회. 층을 넘어도 유지 (Game 씬이 남아 있는 동안), 판이 끝나면 0.
    /// · 컷신 = HUD 위 UI 만 (Time.timeScale 안 건드림 → 게임·제한시간은 그대로 흐름). ① 진입 → ② 컷인 → ③ 본 연출(다단) → ④ 피니시 → ⑤ 걷힘.
    /// · 피해 = 발동 순간 화면 범위 안 물건·사람·고양이·보스 (벽 제외). 다단 hit_count × hit_mul, 피니시 finish_mul (공격력 = 쥐 스킬 피해 기준).
    ///   보스는 이 초필살기로 깎인 합이 최대 체력 × boss_cap 을 못 넘음.
    /// · 쥐별 모습 = SuperUltStyle (그림·색·입자·소리), 월드 쪽 연출은 테이블 motif 로 갈림 (Blackout_Bolt · Default …).
    /// </summary>
    public class SuperUltManager : MonoBehaviour
    {
        public static SuperUltManager I { get; private set; }

        [Header("연결")]
        public RatManager Rats;
        public UltimateManager Ults;
        public ItemManager Items;
        public CatManager Cats;

        [Header("버튼 (HUD 아래, 필살기 버튼 줄 옆)")]
        [Tooltip("버튼이 늘어서는 곳")] public RectTransform bar;
        [Tooltip("버튼 템플릿 (꺼 둔 채로, 복제해서 씀). 글 {name} = 쥐 이름")] public UltButton buttonTemplate;

        [Header("컷신 (HUD 자식, 꺼 둠)")]
        [Tooltip("컷신 전체 (걷힘 때 알파)")] public CanvasGroup cut;
        [Tooltip("월드 위 어둠 (쥐별 dim 색)")] public Image dim;
        [Tooltip("②~④ 동안 클릭 막기 (투명 이미지)")] public Image blocker;
        [Tooltip("① 진입 때 화면을 덮는 테마 색")] public Image tint;
        [Tooltip("펀치 인 · 흔들림을 주는 틀")] public RectTransform frame;
        [Tooltip("컷 그림 틀 (Mask): ②④ 화면 전체 → ③ 위쪽 비스듬한 띠")] public RectTransform panel;
        [Tooltip("컷 그림 (panel 자식, 화면을 덮는 크기 고정)")] public Image art;
        [Tooltip("제목 띠 (왼쪽에서 휙)")] public RectTransform band;
        [Tooltip("초필살기 이름")] public TMP_Text title;
        [Tooltip("'{name}의 초필살기' (자리: {name})")] public TMP_Text sub;
        [Tooltip("자막 (본 연출 · 피니시)")] public TMP_Text line;
        [Tooltip("다단 히트 수 (자리: {n})")] public TMP_Text hits;
        [Tooltip("하얀 번쩍")] public Image flash;
        [Tooltip("피니시 그림 (쥐별 finishArt, 없으면 숨김)")] public Image finishArt;
        [Tooltip("입자 자리 · 점 템플릿 · 큰 입자 템플릿 (꺼 둠) — 쥐별 Blizzard 가 비어 있으면 이것")] public RectTransform fxLayer;
        public Image fxDot, fxFlake;
        [Tooltip("쥐별 모습")] public SuperUltStyle[] styles;

        [Header("모양")]
        [Tooltip("③ 본 연출 때 컷 그림 띠: 크기 · 위치 (px) · 기울기 (도)")] public Vector2 actPanelSize = new(2400, 440);
        public Vector2 actPanelPos = new(0, 250);
        public float actPanelAngle = -5;
        [Tooltip("띠 바뀌는 시간 (초)")] public float actMorph = 0.25f;
        [Tooltip("② · ④ 펀치 인: 시작 배율 · 시간")] public float punchFrom = 1.2f, punchTime = 0.22f;
        [Tooltip("컷 그림 천천히 확대 (시작 → 끝)")] public Vector2 artZoom = new(1.04f, 1.12f);
        [Tooltip("① 테마 색 덮개 최대 알파")] public float tintAlpha = 0.9f;
        [Tooltip("제목 띠가 들어오는 거리 (px) · 시간")] public float bandSlide = 2400, bandTime = 0.22f;
        [Tooltip("히트 숫자 · 피니시 자막 튀는 배율")] public float hitPop = 1.6f, finishPop = 2.2f;
        [Tooltip("다단 한 타 화면(UI) 흔들림 px · 피니시")] public float uiShakeHit = 8, uiShakeFinish = 26;
        [Tooltip("월드 흔들림: 다단 한 타 · 피니시")] public float worldShakeHit = 0.08f, worldShakeFinish = 0.6f;

        [Header("테스트")]
        [Tooltip("시작부터 자격 있는 신화 종 스택을 다 채움")] public bool testFullStack;
        public bool log;

        // ── 상태 ──
        readonly Dictionary<string, int> stacks = new();
        readonly HashSet<string> used = new();
        readonly Dictionary<string, UltButton> buttons = new();
        readonly List<string> buttonOrder = new();

        class Run
        {
            public SuperUltRow row; public SuperUltStyle st; public Rat r; public string code;
            public Rect view; public float t, baseDmg, uiShake, light; public int hitsDone; public bool finished;
            public Boss boss; public float bossDealt, bossCap; public Color col;
            public bool test;
            public SuperUltLayer[] layers;       // 레이어 컷신 (쥐별 scene)
        }
        Run cur;
        string subFormat, hitsFormat;
        Vector2 bandHome;

        public bool Busy => cur != null;
        static GameDatabase DB => GameDatabase.Instance;
        static float Cfg(string k, float d) => DB != null ? DB.SuperCfg(k, d) : d;

        void Awake()
        {
            I = this;
            if (buttonTemplate) buttonTemplate.gameObject.SetActive(false);
            if (cut) cut.gameObject.SetActive(false);
            if (sub) subFormat = sub.text;
            if (hits) hitsFormat = hits.text;
            if (band) bandHome = band.anchoredPosition;
        }
        void OnDestroy() { if (I == this) I = null; }

        // ── 자격 · 스택 ──
        public static bool Eligible(Rat r)
        {
            if (!r || r.temp > 0 || r.Data == null || r.Data.Grade != Grade.Mythic || DB == null || !DB.SuperUlts.ContainsKey(r.codeId)) return false;
            return Progress.I && Progress.I.Level(r.codeId) >= Cfg("unlock_level", 10);
        }
        public int Stack(string code) => stacks.TryGetValue(code, out var n) ? n : 0;

        // UltimateManager.Stop(done) — 자기 필살기를 끝까지 씀
        public void OnUltDone(Rat r)
        {
            if (!Eligible(r) || used.Contains(r.codeId)) return;
            stacks[r.codeId] = Stack(r.codeId) + 1;
            if (log) Debug.Log($"[SuperUlt] {r.codeId} 스택 {stacks[r.codeId]}/{Cfg("stack_need", 5)}");
        }

        Rat RatOf(string code)
        {
            Rat best = null;
            foreach (var r in Rats.Rats)
                if (r.codeId == code && Eligible(r) && (!best || (r.OnScreen() && !best.OnScreen()))) best = r;
            return best;
        }
        public bool Ready(string code) => !used.Contains(code) && Stack(code) >= Cfg("stack_need", 5) && RatOf(code);
        public bool AnyReady { get { foreach (var c in DB.SuperUlts.Keys) if (Ready(c)) return true; return false; } }

        // 지금 발동할 수 있나 (다른 연출 중이면 기다림)
        public bool CanStart => cur == null && !GameOver.Active && !Heist.Active && !FxManager.Halt && !FxManager.WorldFreeze
            && !(Ults && Ults.CutPhase) && !(Ults && Ults.SuperJump && Ults.SuperJump.Busy) && !(Ults && Ults.Stage && Ults.Stage.Climbing);

        public void Request(string code)
        {
            if (!CanStart || !Ready(code)) { SfxManager.Play("ui_deny"); return; }
            Begin(code, RatOf(code), false);
        }

        // 테스트 패널: 조건 무시하고 바로 (그 종이 없으면 화면 가운데에 임시로 불러옴)
        public bool TestSuper(string code)
        {
            if (cur != null || DB == null || !DB.SuperUlts.ContainsKey(code) || !DB.RatsByCode.TryGetValue(code, out var row)) return false;
            Rat r = null;
            foreach (var o in Rats.Rats) if (o.codeId == code && !o.UltOn) { r = o; if (o.OnScreen()) break; }
            if (!r)
            {
                var c = Ults.ViewRect(0).center;
                r = Rats.Spawn(row, c.x, c.y, false);
                if (!r) return false;
                r.temp = 15; r.noBreed = 99; r.breedCD = 99;
            }
            Begin(code, r, true);
            return true;
        }

        void Begin(string code, Rat r, bool test)
        {
            var row = DB.SuperUlts[code];
            SuperUltStyle st = null;
            if (styles != null) foreach (var s in styles) if (s && s.code == code) { st = s; break; }
            if (!st) Debug.LogWarning($"[SuperUlt] 쥐별 모습 없음: {code} (HUD/SuperUlt/Styles)");
            var u = DB.UltOf(r.Data);
            cur = new Run
            {
                row = row, st = st, r = r, code = code, test = test,
                view = Ults.ViewRect(0), baseDmg = r.SkillDamage * CommonSkill.UltPowerMul,
                boss = Boss.Current, col = st && st.color.a > 0 ? st.color : u != null ? u.Color : Color.white,
            };
            if (cur.boss) cur.bossCap = cur.boss.hpMax * Cfg("boss_cap", 0.35f);
            if (!test) { used.Add(code); stacks[code] = 0; }
            if (log) Debug.Log($"[SuperUlt] 발동 {code} ({row.super_name}) 공격력 {cur.baseDmg:0}");

            // UI 처음 상태
            cut.gameObject.SetActive(true); cut.alpha = 1;
            if (title) title.text = row.super_name;
            if (sub) sub.text = (subFormat ?? "").Replace("{name}", r.Data.character_name);
            if (line) line.text = "";
            if (hits) hits.text = "";
            if (tint) tint.color = WithA(cur.col, 0);
            if (dim) dim.color = st ? WithA(st.dim, 0) : new Color(0, 0, 0, 0);
            if (flash) flash.color = WithA(Color.white, 0);
            if (art) { art.color = WithA(Color.white, 0); art.sprite = Cut(0); }
            if (finishArt) finishArt.gameObject.SetActive(false);
            if (band) band.anchoredPosition = bandHome + new Vector2(-bandSlide, 0);
            if (blocker) blocker.raycastTarget = false;
            if (st)
            {
                if (!st.snow.layer) st.snow.layer = fxLayer;
                if (!st.snow.dot) st.snow.dot = fxDot;
                if (!st.snow.flake) st.snow.flake = fxFlake;
                Sfx(st.sfxEnter);
                if (st.scene)
                {
                    st.scene.gameObject.SetActive(true);
                    cur.layers = st.scene.GetComponentsInChildren<SuperUltLayer>(true);
                    foreach (var l in cur.layers) l.Apply(0);
                }
            }
            phase = 0;
        }
        int phase;      // 지금 단계 (0 ① · 1 ② · 2 ③ · 3 ④ · 4 ⑤)

        Sprite Cut(int i) => cur.st && cur.st.cuts != null && i < cur.st.cuts.Length ? cur.st.cuts[i] : null;
        static Color WithA(Color c, float a) { c.a = a; return c; }
        static void Sfx(string n) { if (!string.IsNullOrEmpty(n)) SfxManager.Play(n); }
        static float Ease(float k) => 1 - (1 - k) * (1 - k) * (1 - k);

        void Update()
        {
            if (DB == null || !Rats) return;
            if (testFullStack)
                foreach (var r in Rats.Rats) if (Eligible(r) && !used.Contains(r.codeId)) stacks[r.codeId] = Mathf.Max(Stack(r.codeId), (int)Cfg("stack_need", 5));
            if (cur != null) Step();
            UpdateButtons();
        }

        // ── 컷신 진행 ──
        void Step()
        {
            if (GameOver.Active) { End(); return; }
            if (FxManager.Halt) return;                  // 포기 창 · 튜토리얼 대사 중엔 멈춤
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f), Te = Cfg("t_enter", 0.4f), Tc = Cfg("t_cutin", 1.4f), Ta = Cfg("t_act", 3.6f), Tf = Cfg("t_finish", 4.2f), Tend = Cfg("t_end", 4.5f);
            var c = cur; var st = c.st;
            c.t += dt; float t = c.t;          // dt 는 한 프레임 최대 0.05초 (에디터·로딩 끊김에 컷신이 통째로 넘어가지 않게)

            // 단계 바뀜
            int ph = t < Te ? 0 : t < Tc ? 1 : t < Ta ? 2 : t < Tf ? 3 : 4;
            while (phase < ph)
            {
                phase++;
                if (phase == 1) { if (st) { Sfx(st.sfxCut); st.snow.Add(st.fxCut); st.snow.Gust(st.gustCut); } }
                if (phase == 2) { if (line) line.text = c.row.super_line; Pulse(0.8f); if (st) st.snow.Add(st.fxAct); }
                if (phase == 3) Finish();
            }

            // 다단 히트 (본 연출 구간에 고르게)
            int hc = Mathf.Max(0, (int)Cfg("hit_count", 10));
            while (c.hitsDone < hc && t >= Tc + (c.hitsDone + 0.5f) * (Ta - Tc) / hc) Hit();

            Animate(t, Te, Tc, Ta, Tf, Tend);
            st?.snow.Tick(dt);
            if (t >= Tend) End();
        }

        void Animate(float t, float Te, float Tc, float Ta, float Tf, float Tend)
        {
            var c = cur; var st = c.st;
            var full = ((RectTransform)cut.transform).rect.size;
            // 어둠 (월드): ① 에서 짙어지고 (깜빡임이 있으면 불이 깜빡이다 꺼짐) ⑤ 에서 걷힘. 다단 한 타마다 번개 불빛으로 잠깐 걷힘
            int fl = st ? st.enterFlicker : 0;
            if (dim && st)
            {
                float k = Mathf.Clamp01(t / Te);
                if (fl > 0 && t < Te) k = Mathf.Repeat(k * fl * 2, 2) < 1 ? 1 : 0.15f;        // 꺼짐 · 켜짐 · 꺼짐 …
                float a = st.dim.a * k * (t > Tf ? 1 - (t - Tf) / (Tend - Tf) : 1) * (1 - st.hitLighten * c.light);
                dim.color = WithA(Color.Lerp(st.dim, st.lightenColor, st.hitLighten * c.light * 0.5f), a);
            }
            c.light = Mathf.Max(0, c.light - Time.unscaledDeltaTime * 7);
            // 테마 색 덮개: ① 차오름 (깜빡임이 있으면 ① 끝 무렵에만) → ② 시작에 컷 그림이 덮으며 빠짐
            float tk = fl > 0 ? Mathf.Clamp01((t / Te - 0.7f) / 0.3f) : Ease(Mathf.Clamp01(t / Te));
            if (tint) tint.color = WithA(c.col, tintAlpha * (t < Te ? tk : Mathf.Max(0, 1 - (t - Te) / 0.15f)));
            // 클릭 막기
            if (blocker) blocker.raycastTarget = t >= Te && t < Tf;
            // 레이어 컷신 (있으면 컷 그림 대신)
            if (c.layers != null)
            {
                foreach (var l in c.layers) if (l) l.Apply(t);
                if (art) art.color = WithA(Color.white, 0);
            }
            // 컷 그림
            else if (art && panel)
            {
                int ci = t < Tc ? 0 : t < Ta ? 1 : 2;
                var sp = Cut(ci); if (art.sprite != sp) art.sprite = sp;
                art.color = WithA(Color.white, t < Te || !sp ? 0 : 1);
                float a0 = ci == 0 ? Te : ci == 1 ? Tc : Ta, a1 = ci == 0 ? Tc : ci == 1 ? Ta : Tend;
                art.rectTransform.localScale = Vector3.one * Mathf.Lerp(artZoom.x, artZoom.y, Mathf.Clamp01((t - a0) / (a1 - a0)));
                // 띠 모양: ③ 에서만 위쪽 비스듬한 띠
                float m = t >= Tc && t < Ta ? Ease(Mathf.Clamp01((t - Tc) / actMorph)) : 0;
                panel.sizeDelta = Vector2.Lerp(full + Vector2.one * 40, actPanelSize, m);
                panel.anchoredPosition = Vector2.Lerp(Vector2.zero, actPanelPos, m);
                panel.localRotation = Quaternion.Euler(0, 0, actPanelAngle * m);
            }
            // 펀치 인 (② · ④ 시작) + 흔들림
            if (frame)
            {
                float p = t >= Ta ? (t - Ta) / punchTime : t >= Te && t < Tc ? (t - Te) / punchTime : 1;
                frame.localScale = Vector3.one * Mathf.Lerp(punchFrom, 1, Ease(Mathf.Clamp01(p)));
                c.uiShake = Mathf.Max(0, c.uiShake - Time.unscaledDeltaTime * 90);
                frame.anchoredPosition = c.uiShake > 0 ? Random.insideUnitCircle * c.uiShake : Vector2.zero;
            }
            // 제목 띠: ② 에 들어오고 ③ 시작에 나감
            if (band)
            {
                float To = st && st.titleOut > 0 ? Mathf.Min(st.titleOut, Tc) : Tc;
                float x = t < To ? -bandSlide * (1 - Ease(Mathf.Clamp01((t - Te) / bandTime))) : bandSlide * Ease(Mathf.Clamp01((t - To) / bandTime));
                band.anchoredPosition = bandHome + new Vector2(x, 0);
            }
            // 히트 숫자 · 자막 튐
            if (hits) hits.transform.localScale = Vector3.Lerp(hits.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 14);
            if (line) line.transform.localScale = Vector3.Lerp(line.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 10);
            // 하얀 번쩍
            if (flash) flash.color = WithA(Color.white, Mathf.Max(0, flash.color.a - Time.unscaledDeltaTime * 3.2f));
            // 피니시 그림: 번쩍 나타나 커지며 사라짐
            if (finishArt && finishArt.gameObject.activeSelf)
            {
                float k = Mathf.Clamp01((t - Ta) / Mathf.Max(0.05f, Tf - Ta));
                finishArt.color = WithA(st ? st.finishArtColor : Color.white, 1 - k * k);
                finishArt.rectTransform.localScale = Vector3.one * (1 + 0.15f * k);
            }
            // ⑤ 걷힘
            cut.alpha = t > Tf ? 1 - Mathf.Clamp01((t - Tf) / (Tend - Tf)) : 1;
        }

        void Pulse(float a) { if (flash) flash.color = WithA(Color.white, Mathf.Max(flash.color.a, a)); }

        void Hit()
        {
            var c = cur; var st = c.st;
            c.hitsDone++;
            var pts = Damage(c.baseDmg * Cfg("hit_mul", 4));
            WorldFx(pts, false);
            if (hits) { hits.text = (hitsFormat ?? "{n}").Replace("{n}", c.hitsDone.ToString()); hits.transform.localScale = Vector3.one * hitPop; }
            c.uiShake = Mathf.Max(c.uiShake, uiShakeHit);
            c.light = 1;
            FxManager.I?.Shake(worldShakeHit);
            if (st) { Sfx(st.sfxHit); st.snow.Gust(st.gustHit); }
        }

        void Finish()
        {
            var c = cur; var st = c.st;
            if (c.finished) return;
            c.finished = true;
            var pts = Damage(c.baseDmg * Cfg("finish_mul", 20));
            WorldFx(pts, true);
            Pulse(1);
            if (line) { line.text = c.row.finish_line; line.transform.localScale = Vector3.one * finishPop; }
            if (hits) { hits.text = (hitsFormat ?? "{n}").Replace("{n}", (c.hitsDone + 1).ToString()); hits.transform.localScale = Vector3.one * finishPop; }
            if (finishArt && st && st.finishArt)
            {
                finishArt.sprite = st.finishArt; finishArt.preserveAspect = true;
                var sz = st.finishArt.rect.size;
                finishArt.rectTransform.sizeDelta = new Vector2(st.finishArtSize * sz.x / Mathf.Max(1, sz.y), st.finishArtSize);
                finishArt.rectTransform.localRotation = Quaternion.Euler(0, 0, st.finishArtAngle);
                finishArt.gameObject.SetActive(true);
            }
            c.uiShake = uiShakeFinish;
            FxManager.I?.Shake(worldShakeFinish);
            if (st) { Sfx(st.sfxFinish); st.snow.Add(st.fxFinish); st.snow.Gust(st.gustFinish); }
        }

        // ── 피해: 발동 순간 화면 범위 안 (벽 제외). 효과를 그릴 자리를 돌려줌 ──
        readonly List<Vector3> pts = new();
        readonly List<Item> itemBuf = new();
        readonly List<NKK.Humans.Human> humanBuf = new();
        List<Vector3> Damage(float dmg)
        {
            var c = cur; var v = c.view; var r = c.r ? c.r : null;
            pts.Clear();
            var mid = v.center;
            float Ang(float x, float y) => Mathf.Atan2(y - mid.y, x - mid.x);
            itemBuf.Clear(); itemBuf.AddRange(Items.All);
            foreach (var it in itemBuf)
                if (it && it.State == Item.ItemState.Rest && v.Contains(new Vector2(it.x, it.y))) { it.Damage(dmg, r, false, Ang(it.x, it.y)); pts.Add(new Vector3(it.x, it.y, 0)); }
            humanBuf.Clear(); humanBuf.AddRange(Items.Humans);
            foreach (var h in humanBuf)
                if (h && h.Alive && v.Contains(new Vector2(h.x, h.y))) { h.Damage(dmg, r, Ang(h.x, h.y)); pts.Add(new Vector3(h.x, h.y, 1)); }
            var cat = Cats ? Cats.Current : null;
            if (cat && cat.Alive && v.Contains(new Vector2(cat.x, cat.y))) { cat.Damage(dmg, Ang(cat.x, cat.y), r); pts.Add(new Vector3(cat.x, cat.y, 2)); }
            var b = Boss.Current;
            if (b && b == c.boss && v.Contains(new Vector2(b.x, b.y)))
            {
                float left = c.bossCap - c.bossDealt;
                if (left > 0)
                {
                    float before = b.hp;
                    b.Damage(Mathf.Min(dmg, left), r, Ang(b.x, b.y), false);
                    c.bossDealt += Mathf.Max(0, before - b.hp);
                    if (c.bossDealt > c.bossCap && b.hp > 0) { b.hp += c.bossDealt - c.bossCap; c.bossDealt = c.bossCap; }      // 배율로 넘친 만큼 되돌림
                }
                pts.Add(new Vector3(b.x, b.y, 3));
            }
            return pts;
        }

        // ── 월드 쪽 연출 (테이블 motif) ──
        void WorldFx(List<Vector3> at, bool finish)
        {
            var fx = FxManager.I; var c = cur; var st = c.st;
            if (!fx) return;
            var col = st ? st.hitColor : c.col; float size = st ? st.hitSize : 1;
            // 그릴 자리: 다단 = 몇 군데만, 피니시 = 전부 (대상이 없으면 화면 안 아무 데나 — 허공에라도 터지게)
            var list = new List<Vector3>(at);
            int want = finish ? Mathf.Min(24, Mathf.Max(5, list.Count)) : Mathf.Max(1, st ? st.hitFxTargets : 3);
            while (list.Count < (finish ? 5 : 1)) list.Add(new Vector3(Random.Range(c.view.xMin + 80, c.view.xMax - 80), Random.Range(c.view.yMin + 80, c.view.yMax - 80), -1));
            for (int i = list.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
            int n = Mathf.Min(want, list.Count);
            switch (c.row.motif)
            {
                case "Blackout_Bolt":       // 찌릿 햄찌 '도시 대정전': 깜깜한 도시에 번개가 내리꽂힘 → 피니시 = 전부 한꺼번에
                    for (int i = 0; i < n; i++)
                    {
                        var p = list[i];
                        fx.Bolt(p.x, p.y, col, size * (finish ? 2.2f : 1), finish ? 0.45f : 0.22f);
                        fx.Crackle(p.x, p.y, 30, finish ? 120 : 60, col, finish ? 4 : 2, finish ? 5 : 3, 0.12f);
                        if (finish) fx.Ring(p.x, p.y, 180, col, 0.4f);
                    }
                    break;
                default:                    // 공통: 테마 색 반짝이 · 고리 (2단계에서 쥐별로 바꿈)
                    for (int i = 0; i < n; i++)
                    {
                        var p = list[i];
                        fx.Stars(p.x, p.y, 40, finish ? 14 : 6, col, Color.white);
                        fx.Ring(p.x, p.y, (finish ? 170 : 70) * size, col, finish ? 0.45f : 0.3f);
                    }
                    break;
            }
        }

        void End()
        {
            var c = cur; cur = null; phase = 0;
            if (c?.st) { c.st.snow.Clear(); if (c.st.scene) c.st.scene.gameObject.SetActive(false); }
            if (cut) cut.gameObject.SetActive(false);
            if (frame) { frame.localScale = Vector3.one; frame.anchoredPosition = Vector2.zero; }
            if (log && c != null) Debug.Log($"[SuperUlt] 끝 {c.code} · 다단 {c.hitsDone} · 보스에 {c.bossDealt:0}");
        }

        // ── 버튼: 준비된 종마다 하나 ──
        void UpdateButtons()
        {
            if (!bar || !buttonTemplate) return;
            for (int i = buttonOrder.Count - 1; i >= 0; i--)
            {
                var code = buttonOrder[i];
                if (Ready(code)) continue;
                if (buttons.TryGetValue(code, out var old) && old) Destroy(old.gameObject);
                buttons.Remove(code); buttonOrder.RemoveAt(i);
            }
            foreach (var code in DB.SuperUlts.Keys)
            {
                if (buttons.ContainsKey(code) || !Ready(code)) continue;
                var r = RatOf(code);
                var b = Instantiate(buttonTemplate, bar); b.gameObject.SetActive(true); b.name = "Super_" + code;
                var cc = code;
                b.Setup(Ults ? Ults.UltIcon(DB.UltOf(r.Data)) : null, r.Data.character_name, () => Request(cc));
                buttons[code] = b; buttonOrder.Add(code);
            }
            bool can = CanStart;
            foreach (var code in buttonOrder) buttons[code].Refresh(!can, false);
        }
    }
}
