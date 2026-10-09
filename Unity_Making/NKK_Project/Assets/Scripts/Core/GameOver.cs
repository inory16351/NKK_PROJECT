using System.Collections.Generic;
using NKK.Data;
using NKK.Hazards;
using NKK.Humans;
using NKK.Items;
using NKK.Rats;
using NKK.Ults;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NKK
{
    // 게임 오버 (웹게임 meta.js startGameOver · updateGameOver · drawGameOverFx · lobby.js showGameOver).
    // 시간 초과(또는 보스 패배) → 화면 사방에서 경비원·고양이가 몰려와 가장 가까운 쥐를 잡음 (잡힌 쥐 = 기절 + 철창),
    // 남은 쥐는 도망 → 4.5초 뒤(또는 다 잡히고 2.2초) 결과 창 "잡혀버리고 말았다…" → 아지트로.
    // 판이 끝나도 치즈·연구자료·공용 스킬·훈장·훈련은 남고, 쥐·층은 처음부터 (포기 창과 같은 규칙).
    public class GameOver : MonoBehaviour
    {
        public enum Why { Time, Boss }
        public static bool Active { get; private set; }

        [Header("연결")]
        public GameManager Game;
        public RatManager Rats;
        public ItemManager Items;
        public CatManager Cats;
        public UltimateManager Ults;

        [Header("습격 (웹 기준): 수 = min(최대, 기본 + 쥐 수 ÷ 나눔)")]
        [Tooltip("경비원 사람 code_id (사람 테이블)")] public string guardCode = "guard";
        public int guardBase = 12, guardPerRats = 4, guardMax = 24;
        public int catBase = 5, catPerRats = 10, catMax = 10;
        [Tooltip("경비원 속도 (최소~최대)")] public Vector2 guardSpeed = new(230, 300);
        [Tooltip("고양이 속도 (최소~최대)")] public Vector2 catSpeed = new(330, 400);
        [Tooltip("잡는 거리: 경비원 · 고양이")] public float guardReach = 34, catReach = 40;
        [Tooltip("고양이가 이 거리 안이면 덮치기 자세로 깡충")] public float catPounceRange = 140;
        [Tooltip("이 거리 안의 습격자를 보면 쥐가 도망")] public float fleeRadius = 360;
        [Tooltip("처음에 외치는 경비원 수")] public int shoutCount = 3;
        [Tooltip("쥐를 잡았을 때 경비원이 외칠 확률")] public float catchShoutChance = 0.3f;

        [Header("결과 창까지 (초)")]
        public float resultTime = 4.5f;
        [Tooltip("다 잡혔으면 이 시간 뒤 바로")] public float allCaughtTime = 2.2f;
        [Tooltip("결과 판정 뒤 창이 뜨기까지")] public float panelDelay = 0.6f;

        [Header("글 (인스펙터)")]
        [Tooltip("배너 제목: 시간 초과 / 보스 패배")] public string bannerTime;
        public string bannerBoss;
        public string bannerSub;
        [Tooltip("쥐가 잡힐 때 뜨는 글 (무작위)")] public string[] caughtPops;
        public Color flashColor = new(0.91f, 0.47f, 0.42f);

        [Header("잡힌 쥐 철창 (월드)")]
        [Tooltip("꺼 둔 템플릿 (철창 그림)")] public SpriteRenderer cageTemplate;
        [Tooltip("철창 폭 (게임 단위) × 쥐 등급 크기")] public float cageWidth = 70;

        [Header("화면 연출 (HUD)")]
        [Tooltip("연출 묶음 (꺼 둠). 알파 = 시작 1.2초 동안 차오름")] public CanvasGroup fx;
        [Tooltip("붉은 테두리 (맥박)")] public Image vignette;
        [Tooltip("2.5초부터 서서히 어두워짐")] public Image dark;
        [Tooltip("경보등 빛줄기 (빙글빙글, 왼쪽 → 오른쪽 순)")] public RectTransform[] sirenBeams;
        [Tooltip("일망타진!!! 글 (3.2초까지)")] public TMP_Text title;
        public float beamSpin = 7;

        [Header("결과 창 (HUD)")]
        public GameObject panel;
        [Tooltip("이유 글: 시간 초과 / 보스 패배 (하나만 켬)")] public GameObject whyTime, whyBoss;
        [Tooltip("성과 값 글 (자리표시 {floor} {start} {research} {best} {n})")] public TMP_Text summary;
        public Button okButton;
        [Tooltip("돌아갈 씬 이름")] public string lobbyScene = "Lobby";

        class Raider { public Human h; public Cat c; public float spd; }
        readonly List<Raider> raiders = new();
        readonly Dictionary<Rat, SpriteRenderer> caught = new();
        readonly List<Rat> free = new();
        Why why;
        float t, panelT = -1;
        bool shown;
        string summaryFormat;

        void Awake()
        {
            Active = false;
            if (fx) fx.gameObject.SetActive(false);
            if (panel) panel.SetActive(false);
            if (cageTemplate) cageTemplate.gameObject.SetActive(false);
            if (summary) summaryFormat = summary.text;
            if (okButton) okButton.onClick.AddListener(BackToLobby);
        }
        void OnDestroy() { Active = false; }

        static T Pick<T>(IList<T> l) => l[Random.Range(0, l.Count)];

        public void Begin(Why reason)
        {
            SfxManager.Play("guard_siren");
            if (Active) return;
            Active = true; why = reason; t = 0; shown = false; panelT = -1;
            if (Ults) Ults.CancelAll();
            int n = 0; foreach (var r in Rats.Rats) if (r.temp <= 0) n++;
            var vr = Ults ? Ults.ViewRect(0) : new Rect(0, 0, World.RW, World.RH);
            Vector2 Edge()
            {
                int s = Random.Range(0, 4);
                return s == 0 ? new Vector2(vr.xMin - 60, vr.yMin + Random.value * vr.height) : s == 1 ? new Vector2(vr.xMax + 60, vr.yMin + Random.value * vr.height)
                     : s == 2 ? new Vector2(vr.xMin + Random.value * vr.width, vr.yMin - 40) : new Vector2(vr.xMin + Random.value * vr.width, vr.yMax + 60);
            }
            var db = GameDatabase.Instance;
            // 경비원
            HumanRow guard = null; foreach (var h in db.Humans) if (h.code_id == guardCode) guard = h;
            var gArt = guard != null && Items.humanArt ? Items.humanArt.Get(guard.code_id) : null;
            if (gArt != null && Items.humanPrefab)
            {
                int nG = Mathf.Min(guardMax, guardBase + n / Mathf.Max(1, guardPerRats));
                for (int i = 0; i < nG; i++)
                {
                    var p = Edge();
                    var h = Instantiate(Items.humanPrefab, Items.humanRoot ? Items.humanRoot : Items.transform);
                    h.Init(Items, guard, gArt, p.x, p.y); h.BeginRaid();
                    if (i < shoutCount) h.RaidSay("Raid");
                    raiders.Add(new Raider { h = h, spd = Random.Range(guardSpeed.x, guardSpeed.y) });
                }
            }
            // 고양이 (실제 품종만)
            var pool = new List<CatCharacterRow>();
            foreach (var c in db.Cats.Values) if (c.Category != CatCategory.Special && Cats.catArt && Cats.catArt.Get(c.code_id) != null) pool.Add(c);
            if (pool.Count > 0 && Cats.catPrefab)
            {
                int nC = Mathf.Min(catMax, catBase + n / Mathf.Max(1, catPerRats));
                for (int i = 0; i < nC; i++)
                {
                    var p = Edge(); var row = Pick(pool);
                    db.CatSkills.TryGetValue(row.skill, out var skill);
                    var c = Instantiate(Cats.catPrefab, Cats.catRoot ? Cats.catRoot : Cats.transform);
                    c.Init(Cats, row, skill, Cats.catArt.Get(row.code_id), p.x, p.y, 1, 0); c.BeginRaid(Random.value);
                    raiders.Add(new Raider { c = c, spd = Random.Range(catSpeed.x, catSpeed.y) });
                }
            }
            foreach (var r in Rats.Rats) { r.rushT = 0; r.noBreed = 99; }
            if (Ults) Ults.Flash(flashColor, 0.5f);
            var fxm = FxManager.I; if (fxm) fxm.Shake(0.4f);
            Game.ShowBanner(why == Why.Boss ? bannerBoss : bannerTime, bannerSub);
            if (fx) { fx.gameObject.SetActive(true); fx.alpha = 0; }
        }

        void Catch(Rat r, bool pop)
        {
            if (caught.ContainsKey(r)) return;
            r.Stun(999); r.Held = true; r.vx = r.vy = 0; r.flee = 0;
            SpriteRenderer cage = null;
            if (cageTemplate)
            {
                cage = Instantiate(cageTemplate, cageTemplate.transform.parent); cage.gameObject.SetActive(true);
                float w = cageWidth * r.GradeData.size * World.U / (cage.sprite ? cage.sprite.bounds.size.x : 1);
                cage.transform.localScale = Vector3.one * w;
            }
            caught[r] = cage;
            var fxm = FxManager.I;
            if (pop && fxm && Ults && Ults.OnScreen(r.x, r.y, 0))
            {
                if (caughtPops != null && caughtPops.Length > 0 && Random.value < 0.5f) fxm.Popup(r.x, r.y, Pick(caughtPops), Color.white, 16, 0.8f, 40);
                fxm.Dust(r.x, r.y, 4, 0.8f);
            }
        }

        Rat Nearest(float x, float y)
        {
            Rat b = null; float bd = float.MaxValue;
            foreach (var r in free) { float d = (r.x - x) * (r.x - x) + (r.y - y) * (r.y - y); if (d < bd) { bd = d; b = r; } }
            return b;
        }

        void Update()
        {
            if (!Active) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            t += dt;
            free.Clear();
            foreach (var r in Rats.Rats) if (r.temp <= 0 && !r.UltOn && !caught.ContainsKey(r)) free.Add(r);
            // 습격자: 가장 가까운 쥐로 곧장 → 닿으면 잡음
            foreach (var a in raiders)
            {
                if (a.h)
                {
                    var r = Nearest(a.h.x, a.h.y);
                    if (a.h.RaidStep(dt, r, a.spd, guardReach)) { Catch(r, true); free.Remove(r); if (Random.value < catchShoutChance) a.h.RaidSay("Raid_Catch"); }
                }
                else if (a.c)
                {
                    var r = Nearest(a.c.x, a.c.y);
                    if (a.c.RaidStep(dt, r, a.spd, catReach, catPounceRange)) { Catch(r, true); free.Remove(r); }
                }
            }
            // 남은 쥐는 가장 가까운 습격자 반대쪽으로 도망
            foreach (var r in free)
            {
                float bd = fleeRadius * fleeRadius; Vector2? b = null;
                foreach (var a in raiders)
                {
                    float ax = a.h ? a.h.x : a.c ? a.c.x : 0, ay = a.h ? a.h.y : a.c ? a.c.y : 0;
                    if (!a.h && !a.c) continue;
                    float d = (ax - r.x) * (ax - r.x) + (ay - r.y) * (ay - r.y);
                    if (d < bd) { bd = d; b = new Vector2(ax, ay); }
                }
                if (b.HasValue) r.Scare(b.Value.x, b.Value.y, 1);
            }
            // 화면 밖 쥐까지 기다리지 않고 결과 창 (그때까지 안 잡힌 쥐도 결국 잡힘)
            if (!shown && (t > resultTime || (free.Count == 0 && t > allCaughtTime)))
            {
                shown = true; panelT = panelDelay;
                foreach (var r in free) Catch(r, false);
                free.Clear();
            }
            if (panelT >= 0 && (panelT -= Time.unscaledDeltaTime) < 0) ShowPanel();
        }

        void LateUpdate()
        {
            if (!Active) return;
            // 철창: 잡힌 쥐 위 (쥐보다 살짝 앞)
            foreach (var kv in caught)
            {
                var cage = kv.Value; if (!cage) continue;
                if (!kv.Key) { cage.enabled = false; continue; }
                cage.transform.position = World.ToUnity(kv.Key.x, kv.Key.y);
                cage.sortingOrder = World.SortOrder(kv.Key.y + 0.5f) + 1;
            }
            // 화면 연출 (웹 drawGameOverFx)
            float k = Mathf.Clamp01(t / 1.2f), pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10);
            if (fx) fx.alpha = k;
            if (vignette) { var c = vignette.color; c.a = 0.35f + 0.15f * pulse; vignette.color = c; }
            if (dark) { var c = dark.color; c.a = 0.25f * Mathf.Clamp01((t - 2.5f) / 2); dark.color = c; }
            if (sirenBeams != null)
                for (int i = 0; i < sirenBeams.Length; i++)
                    if (sirenBeams[i]) sirenBeams[i].localRotation = Quaternion.Euler(0, 0, -(Time.time * beamSpin + (i % 2 == 1 ? Mathf.PI : 0)) * Mathf.Rad2Deg);
            if (title)
            {
                title.alpha = Mathf.Clamp01(t * 3) * Mathf.Clamp01((3.2f - t) * 2);
                float s = t < 0.25f ? 1.6f - t * 2.4f : 1;
                title.transform.localScale = Vector3.one * s;
                title.transform.localRotation = Quaternion.Euler(0, 0, -2 + Mathf.Sin(Time.time * 30) * 0.6f);
            }
        }

        void ShowPanel()
        {
            SfxManager.Play("jgl_game_over");
            panelT = -1;
            if (whyTime) whyTime.SetActive(why == Why.Time);
            if (whyBoss) whyBoss.SetActive(why == Why.Boss);
            if (summary)
            {
                var p = Progress.I;
                int best = Mathf.Max(Game.Floor, p ? p.maxFloor : 1);
                summary.text = (summaryFormat ?? "").Replace("{floor}", Game.Floor.ToString()).Replace("{start}", Game.StartFloor.ToString())
                    .Replace("{research}", GameManager.Format(Game.RunResearch)).Replace("{best}", best.ToString()).Replace("{n}", caught.Count.ToString());
            }
            if (panel) panel.SetActive(true);
        }

        public void BackToLobby()
        {
            Game.SaveProgress();
            Active = false;
            SceneManager.LoadScene(lobbyScene);
        }

        [ContextMenu("테스트: 게임 오버 (시간 초과)")]
        void TestBegin() { if (Application.isPlaying) Begin(Why.Time); }
    }
}
