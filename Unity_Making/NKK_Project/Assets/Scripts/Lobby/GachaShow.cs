using System.Collections;
using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 유전자 뽑기 연출 (사용자 2026-10-12): 화면 가운데 믹서기(유전자 추출기) 등장 → "다들 들어가!!! 빨리!!" 뽑힌 쥐들이 뛰어와 믹서기에 퐁당 →
    // 뚜껑 쾅 → "갈갈갈갈!!" 믹서기가 덜덜 돌며 유전자 스무디가 차오름 → 조각이 하나씩 튀어나와 결과 칸으로 → 결과 (확인 · 한 번 더)
    // 글은 씬 TMP (captionIn · captionGrind · captionDone 의 글 그대로, 결과 칸 Count {n}, 다시 버튼 {n} {cost})
    // 결과 카드 템플릿 자식: Frame(Image) · Grade · Portrait(RatPortrait) · Name · Count({n}) · New · Wish
    public class GachaShow : MonoBehaviour
    {
        public RatArtLibrary artLibrary;
        [Tooltip("연출 전체 (꺼 둠, 맨 위)")] public GameObject root;
        public CanvasGroup dim;
        [Tooltip("믹서기 묶음 (가운데) · 유리병 · 뚜껑 · 스무디 (Image Filled 세로) · 튀는 스무디 (템플릿, 꺼 둠)")] public RectTransform blender;
        public RectTransform lid;
        public Image smoothie;
        public Image splashTemplate;
        [Tooltip("믹서기 병 입구 (쥐가 뛰어드는 곳)")] public RectTransform jarMouth;
        [Tooltip("자막: 들어가!! · 갈갈갈 · 완성")] public TMP_Text captionIn, captionGrind, captionDone;
        [Tooltip("뛰어오는 쥐 (RatPortrait 템플릿, 꺼 둠)")] public RatPortrait runnerTemplate;
        [Tooltip("튀어나오는 조각 (템플릿, 꺼 둠)")] public Image shardTemplate;
        [Tooltip("결과 칸 템플릿 (꺼 둠) · 결과 줄 (Grid)")] public GameObject cardTemplate;
        public Transform grid;
        [Tooltip("확인 · 한 번 더 (글 {n} {cost}) · 건너뛰기")] public Button ok, again, skip;
        public TMP_Text againText;
        [Tooltip("최고 등급 번쩍 (전설 이상)")] public Image flash;

        [Header("시간 (초)")]
        public float dropIn = 0.45f, runStagger = 0.14f, runTime = 0.55f, jumpTime = 0.35f, lidTime = 0.25f, grindTime = 1.8f, shardStagger = 0.12f, shardFly = 0.45f;
        [Header("움직임")]
        [Tooltip("갈 때 덜덜 (각도 · 픽셀)")] public float shakeAngle = 6, shakeMove = 10;
        [Tooltip("쥐 크기 (px)")] public float runnerSize = 150;
        [Tooltip("결과 볼 때 믹서기가 비켜나는 자리 · 크기")] public Vector2 blenderAside = new(-640, 60);
        public float asideScale = 0.6f, asideTime = 0.35f;

        public bool Playing { get; private set; }
        public static bool Busy;          // 연출 중 (로비 ESC 가 탈출 준비실을 닫지 않게)
        bool skipping;
        GachaPage page; int lastN;
        string againTpl;
        readonly List<GameObject> spawned = new();
        Vector2 blenderHome, lidHome;

        void Awake()
        {
            if (root) root.SetActive(false);
            foreach (var t in new Component[] { runnerTemplate, shardTemplate, splashTemplate }) if (t) t.gameObject.SetActive(false);
            if (cardTemplate) cardTemplate.SetActive(false);
            if (againText) againTpl = againText.text;
            if (blender) blenderHome = blender.anchoredPosition;
            if (lid) lidHome = lid.anchoredPosition;
            if (ok) ok.onClick.AddListener(Close);
            if (again) again.onClick.AddListener(() => { int n = lastN; var pg = page; Close(); if (pg) pg.Pull(n); });
            if (skip) skip.onClick.AddListener(() => skipping = true);
        }

        public void Play(List<GeneGacha.Result> res, int n, GachaPage from)
        {
            page = from; lastN = n; skipping = false;
            StopAllCoroutines(); Clear();
            StartCoroutine(Run(res));
        }

        // 강제 종료 뒤 복구: 연출 없이 결과만
        public void ShowResultsOnly(List<GeneGacha.Result> res)
        {
            StopAllCoroutines(); Clear();
            if (root) root.SetActive(true);
            Playing = true; skipping = true;
            StartCoroutine(Run(res));
        }

        void Clear()
        {
            foreach (var g in spawned) if (g) Destroy(g);
            spawned.Clear();
        }

        void Close()
        {
            StopAllCoroutines(); Clear();
            if (root) root.SetActive(false);
            Playing = Busy = false;
            GeneGacha.ClearPending();
            if (page) page.Render();
        }

        RatCharacterRow Row(string code) { foreach (var r in GameDatabase.Instance.Rats.Values) if (r.code_id == code) return r; return null; }
        static float E(float k) => 1 - (1 - k) * (1 - k);

        IEnumerator Wait(float t) { for (float x = 0; x < t && !skipping; x += Time.unscaledDeltaTime) yield return null; }

        IEnumerator Run(List<GeneGacha.Result> res)
        {
            Playing = Busy = true;
            if (root) root.SetActive(true);
            foreach (var c in new[] { captionIn, captionGrind, captionDone }) if (c) c.gameObject.SetActive(false);
            if (ok) ok.gameObject.SetActive(false); if (again) again.gameObject.SetActive(false); if (skip) skip.gameObject.SetActive(!skipping);
            if (flash) flash.gameObject.SetActive(false);
            if (smoothie) smoothie.fillAmount = 0;
            if (lid) { lid.gameObject.SetActive(false); lid.anchoredPosition = lidHome; }
            if (blender) { blender.gameObject.SetActive(true); blender.anchoredPosition = blenderHome; blender.localRotation = Quaternion.identity; }
            res.Sort((a, b) => a.grade.CompareTo(b.grade));          // 높은 등급은 마지막에

            // 1) 믹서기가 아래에서 쑥
            SfxManager.Play("ui_open");
            for (float x = 0; x < dropIn && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = x / dropIn; if (dim) dim.alpha = k;
                if (blender) { blender.anchoredPosition = blenderHome + new Vector2(0, -700 * (1 - E(k))); blender.localScale = Vector3.one * (0.6f + 0.4f * E(k)); }
                yield return null;
            }
            if (dim) dim.alpha = 1; if (blender) { blender.anchoredPosition = blenderHome; blender.localScale = Vector3.one; }

            // 2) "다들 들어가!!! 빨리!!" 쥐들이 양옆에서 뛰어와 병 속으로 퐁당
            if (captionIn && !skipping) { captionIn.gameObject.SetActive(true); SfxManager.Play("rat_rush_call"); }
            var runs = new List<Coroutine>();
            for (int i = 0; i < res.Count && !skipping; i++)
            {
                runs.Add(StartCoroutine(Runner(res[i], i)));
                yield return Wait(runStagger);
            }
            yield return Wait(runTime + jumpTime + 0.1f);
            if (captionIn) captionIn.gameObject.SetActive(false);

            // 3) 뚜껑 쾅 → 갈갈갈갈!!
            if (lid && !skipping)
            {
                lid.gameObject.SetActive(true);
                for (float x = 0; x < lidTime && !skipping; x += Time.unscaledDeltaTime) { lid.anchoredPosition = lidHome + new Vector2(0, 300 * (1 - E(x / lidTime))); yield return null; }
                lid.anchoredPosition = lidHome; SfxManager.Play("rat_land");
            }
            if (captionGrind && !skipping) captionGrind.gameObject.SetActive(true);
            SfxManager.Play("superjump_charge");
            for (float x = 0; x < grindTime && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = x / grindTime, s = Mathf.Sin(Time.unscaledTime * 55);
                if (blender) { blender.localRotation = Quaternion.Euler(0, 0, s * shakeAngle * (0.4f + k)); blender.anchoredPosition = blenderHome + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shakeMove * (0.4f + k); }
                if (smoothie) smoothie.fillAmount = E(k) * 0.85f;
                if (captionGrind) captionGrind.transform.localScale = Vector3.one * (1 + 0.12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 18)));
                if (splashTemplate && Random.value < Time.unscaledDeltaTime * 14) Splash(jarMouth ? jarMouth.position : blender.position);
                yield return null;
            }
            if (blender) { blender.localRotation = Quaternion.identity; blender.anchoredPosition = blenderHome; }
            if (smoothie) smoothie.fillAmount = 0.85f;
            if (captionGrind) captionGrind.gameObject.SetActive(false);
            if (captionDone) captionDone.gameObject.SetActive(true);
            if (lid) lid.gameObject.SetActive(false);
            SfxManager.Play("spill");
            // 결과 칸 자리를 비켜 줌
            for (float x = 0; x < asideTime && !skipping; x += Time.unscaledDeltaTime)
            {
                float k = E(x / asideTime);
                if (blender) { blender.anchoredPosition = Vector2.Lerp(blenderHome, blenderAside, k); blender.localScale = Vector3.one * Mathf.Lerp(1, asideScale, k); }
                yield return null;
            }
            if (blender) { blender.anchoredPosition = blenderAside; blender.localScale = Vector3.one * asideScale; }

            // 4) 조각이 하나씩 튀어나와 결과 칸으로
            var cards = new List<GameObject>();
            foreach (var r in res) cards.Add(MakeCard(r));
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < res.Count; i++)
            {
                if (skipping) { cards[i].SetActive(true); continue; }
                StartCoroutine(Shard(res[i], cards[i]));
                yield return Wait(shardStagger);
            }
            yield return Wait(shardFly + 0.2f);
            foreach (var c in cards) { c.SetActive(true); c.transform.localScale = Vector3.one; }
            bool high = res.Exists(r => r.grade >= Grade.Legendary);
            if (high && flash && !skipping) StartCoroutine(Flash());
            SfxManager.Play(high ? "jgl_achievement" : "jgl_unlock");

            if (skip) skip.gameObject.SetActive(false);
            if (ok) ok.gameObject.SetActive(true);
            if (again)
            {
                bool can = GeneGacha.CanPull(lastN);
                again.gameObject.SetActive(lastN > 0);
                again.interactable = can;
                if (againText) againText.text = (againTpl ?? "").Replace("{n}", lastN.ToString()).Replace("{cost}", LobbyManager.Fmt(lastN == 10 ? GeneGacha.CostTen : GeneGacha.CostOne * lastN));
            }
            skipping = false;
        }

        // 쥐 한 마리: 화면 옆에서 달려와 병 입구로 폴짝 → 사라짐 (풍덩)
        IEnumerator Runner(GeneGacha.Result r, int i)
        {
            if (!runnerTemplate || !blender) yield break;
            var row = Row(r.code);
            var rp = Instantiate(runnerTemplate, runnerTemplate.transform.parent); rp.gameObject.SetActive(true); spawned.Add(rp.gameObject);
            var rt = (RectTransform)rp.transform; rt.sizeDelta = new Vector2(runnerSize, runnerSize * 0.7f);
            bool left = i % 2 == 0; rp.faceRight = left;
            if (row != null && artLibrary) rp.Show(artLibrary.Get(row.code_id));
            var parent = (RectTransform)rt.parent;
            Vector2 mouth = jarMouth ? (Vector2)parent.InverseTransformPoint(jarMouth.position) : (Vector2)parent.InverseTransformPoint(blender.position);
            float floorY = mouth.y - 420;
            Vector2 start = new(left ? -1150 : 1150, floorY + Random.Range(-30f, 30f)), edge = new(mouth.x + (left ? -260 : 260), floorY);
            for (float x = 0; x < runTime; x += Time.unscaledDeltaTime)
            {
                float k = x / runTime;
                rt.anchoredPosition = Vector2.Lerp(start, edge, k) + new Vector2(0, Mathf.Abs(Mathf.Sin(x * 28)) * 18);
                if (skipping) break;
                yield return null;
            }
            for (float x = 0; x < jumpTime; x += Time.unscaledDeltaTime)
            {
                float k = x / jumpTime;
                rt.anchoredPosition = Vector2.Lerp(edge, mouth, k) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * 260);
                rt.localRotation = Quaternion.Euler(0, 0, (left ? -1 : 1) * k * 200);
                rt.localScale = Vector3.one * (1 - 0.5f * k);
                if (skipping) break;
                yield return null;
            }
            SfxManager.Play(Random.value < 0.5f ? "rat_squeak_1" : "rat_squeak_2");
            if (splashTemplate) Splash(jarMouth ? jarMouth.position : blender.position);
            rp.gameObject.SetActive(false);
        }

        void Splash(Vector3 world)
        {
            var s = Instantiate(splashTemplate, splashTemplate.transform.parent); s.gameObject.SetActive(true); spawned.Add(s.gameObject);
            s.transform.position = world; StartCoroutine(SplashFly((RectTransform)s.transform));
        }
        IEnumerator SplashFly(RectTransform s)
        {
            Vector2 v = new(Random.Range(-260f, 260f), Random.Range(260f, 520f)); float life = 0.6f;
            for (float x = 0; x < life && s; x += Time.unscaledDeltaTime)
            {
                v.y -= 1400 * Time.unscaledDeltaTime; s.anchoredPosition += v * Time.unscaledDeltaTime;
                s.localScale = Vector3.one * (1 - x / life * 0.6f);
                yield return null;
            }
            if (s) s.gameObject.SetActive(false);
        }

        GameObject MakeCard(GeneGacha.Result r)
        {
            var go = Instantiate(cardTemplate, grid); spawned.Add(go);
            var row = Row(r.code); int gi = (int)r.grade;
            var frame = go.transform.Find("Frame")?.GetComponent<Image>(); if (frame) frame.color = MarketPage.GradeColor(gi);
            var g = go.transform.Find("Grade")?.GetComponent<TMP_Text>(); if (g) g.text = MarketPage.GradeName(gi);
            var nm = go.transform.Find("Name")?.GetComponent<TMP_Text>(); if (nm) nm.text = row != null ? row.character_name : "";
            var ct = go.transform.Find("Count")?.GetComponent<TMP_Text>(); if (ct) ct.text = ct.text.Replace("{n}", r.shards.ToString());
            var nw = go.transform.Find("New"); if (nw) nw.gameObject.SetActive(r.isNew);
            var ws = go.transform.Find("Wish"); if (ws) ws.gameObject.SetActive(r.wish);
            go.SetActive(true);
            var por = go.transform.Find("Portrait")?.GetComponent<RatPortrait>(); if (por && row != null && artLibrary) por.Show(artLibrary.Get(row.code_id));
            go.SetActive(false);
            return go;
        }

        // 조각: 병 입구에서 튀어올라 결과 칸으로 날아감 → 칸이 퐁 하고 나타남
        IEnumerator Shard(GeneGacha.Result r, GameObject card)
        {
            if (!shardTemplate) { card.SetActive(true); yield break; }
            var s = Instantiate(shardTemplate, shardTemplate.transform.parent); s.gameObject.SetActive(true); spawned.Add(s.gameObject);
            s.color = MarketPage.GradeColor((int)r.grade);
            var rt = (RectTransform)s.transform;
            Vector3 a = jarMouth ? jarMouth.position : blender.position;
            card.SetActive(true); card.transform.localScale = Vector3.zero;
            Vector3 b = card.transform.position;
            for (float x = 0; x < shardFly; x += Time.unscaledDeltaTime)
            {
                float k = x / shardFly;
                rt.position = Vector3.Lerp(a, b, E(k)) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.2f * rt.lossyScale.y * 100;
                rt.localRotation = Quaternion.Euler(0, 0, k * 540);
                if (skipping) break;
                yield return null;
            }
            s.gameObject.SetActive(false);
            SfxManager.Play(r.grade >= Grade.Unique ? "combo_word" : "cheese");
            for (float x = 0; x < 0.2f; x += Time.unscaledDeltaTime) { float k = x / 0.2f; card.transform.localScale = Vector3.one * (k < 0.7f ? k / 0.7f * 1.15f : 1.15f - (k - 0.7f) / 0.3f * 0.15f); yield return null; }
            card.transform.localScale = Vector3.one;
        }

        IEnumerator Flash()
        {
            flash.gameObject.SetActive(true);
            for (float x = 0; x < 0.5f; x += Time.unscaledDeltaTime) { var c = flash.color; c.a = 0.8f * (1 - x / 0.5f); flash.color = c; yield return null; }
            flash.gameObject.SetActive(false);
        }

        void Update()
        {
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (Playing && k != null && k.escapeKey.wasPressedThisFrame) { if (ok && ok.gameObject.activeSelf) Close(); else skipping = true; }
        }
    }
}
