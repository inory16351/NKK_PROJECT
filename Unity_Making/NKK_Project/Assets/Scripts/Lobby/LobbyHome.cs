using System;
using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace NKK.Lobby
{
    // 아지트 (웹 lobbyscene.js HOME): 벽 속 아지트 배경판 위에서 게임 리그 쥐들이 생활함.
    // 낮잠·차·쳇바퀴·치즈 갉기·아령·수다·쥐구멍 빼꼼. 쥐를 누르면 점프, 끌면 잡혀서 버둥, 놓으면 던져짐.
    // 물건(LobbyHot)을 누르면 탈출 준비실 페이지로. 좌표는 배경판 기준 u(0~1 가로)·v(0~1 세로, 아래로 +).
    public class LobbyHome : MonoBehaviour
    {
        [Header("연결")]
        public Camera cam;
        [Tooltip("배경판 (화면을 꽉 채움, 위아래가 조금 잘림)")] public SpriteRenderer background;
        public LobbyRat ratPrefab;
        public RatArtLibrary artLibrary;
        public Transform actorRoot;
        [Tooltip("활동 자리 (자식 LobbySlot)")] public Transform slotRoot;
        [Tooltip("물건 버튼 (자식 LobbyHot)")] public Transform hotRoot;
        public LobbyManager manager;

        [Header("바닥·쥐 (배경판 비율)")]
        [Tooltip("쥐가 걸어 다니는 판자 가로 범위")] public Vector2 floorU = new(0.17f, 0.86f);
        [Tooltip("판자 세로 범위 (앞쪽 가장자리)")] public Vector2 floorV = new(0.683f, 0.718f);
        public float floorLine = 0.70f;
        [Tooltip("쥐 몸길이 (배경 가로 대비)")] public float ratLen = 0.058f;
        public float gravity = 3.4f, jumpV = 0.8f;
        [Tooltip("걷는 속도 (배경 가로/초)")] public float walkSpeed = 0.09f;
        [Tooltip("쥐 수 = 5 + 티어 (최소·최대)")] public Vector2Int castRange = new(6, 13);
        [Tooltip("몸길이 기준 (리그 빌드용 게임 단위)")] public float rigLength = 46;

        [Header("손에 든 것")]
        [Header("던져진 쥐 헤롱헤롱 별")]
        public Sprite dizzyStar;
        public Color dizzyStarColor = new(0.94f, 0.78f, 0.47f);
        [Tooltip("별 크기 = 쥐 몸길이 × 이 값")] public float dizzyStarSize = 0.22f;
        public Sprite heldCheese;
        public Sprite heldDumbbell;
        public Sprite heldCup;

        [Header("쳇바퀴 · 쥐구멍 · 훈장 · 소품")]
        [Tooltip("쳇바퀴 받침대 (안 돎)")] public SpriteRenderer wheel;
        [Tooltip("쳇바퀴 바퀴 (돎, 가운데 피벗)")] public SpriteRenderer wheelRing;
        [Tooltip("쥐가 달릴 때 바퀴 회전 속도 (도/초)")] public float wheelSpinSpeed = 420;
        [Tooltip("바퀴가 빨라지고 멈추는 정도")] public float wheelSpinAccel = 3;
        [Tooltip("쥐구멍 빼꼼 쥐 (SpriteMask 안에서만 보임)")] public LobbyRat peekRat;
        public Transform hole;
        [Tooltip("전구 줄 훈장 1~8 (티어 이상만 진하게)")] public SpriteRenderer[] badges;
        [Tooltip("티어 2 부터 탁자 위 치즈")] public SpriteRenderer propCheese;
        [Tooltip("티어 3 부터 탁자 위 찻잔")] public SpriteRenderer propCup;

        [Header("말풍선 · 효과 (템플릿, 꺼져 있음)")]
        public SpriteRenderer bubbleTemplate;
        public SpriteRenderer fxTemplate;
        public Sprite fxDust, fxZ, fxCrumb, fxSweat;
        [Tooltip("말풍선 좌우·위아래 여백 (월드 유닛)")] public Vector2 bubblePad = new(0.22f, 0.14f);

        [Header("대사")]
        [Tooltip("수다: '말|대답'")] public string[] chatLines =
        {
            "다음엔 더 높이 간다!|계단만 찾으면 돼!", "치즈 몰래 먹었지?|…아니?", "연구원 표정 봤어?|ㅋㅋㅋ 완전 놀람", "고양이 진짜 무서웠어…|난 꼬리 밟혔어",
            "훈장 달면 새 친구 온대!|오 진짜?", "쳇바퀴 내 차례야|5분만…", "경비원 손전등 봤어?|눈부셔 죽는 줄", "연구 자료 또 훔치자!|찍찍!", "오늘 저녁은 체다!|어제도 체다였잖아",
        };
        public string[] soloLines = { "찍?", "찍찍!", "킁킁…", "배고파…", "탈출하고 싶다", "치즈 냄새!", "여긴 안전해", "…!" };
        public string[] grabLines = { "찍?!", "놔줘~!", "으아아", "높아 높아!", "찍찍찍!!" };
        public string[] dizzyLines = { "어질어질…", "다시 해줘!", "별이 보여…", "찍… 찍…" };
        public string[] pokeLines = { "찍!", "폴짝!", "야호!" };
        public string[] wheelLines = { "헉헉", "더 빨리!", "근육 붙는 중", "찍찍찍찍" };
        public string throwLine = "으아아아~!", wakeLine = "하암~ 잘 잤다", fullLine = "냠, 배부르다", wheelDoneLine = "휴…";

        public enum FxKind { Dust, Z, Crumb, Sweat }

        public readonly List<LobbyRat> actors = new();
        readonly List<LobbySlot> slots = new();
        readonly List<LobbyHot> hots = new();
        class Bubble { public LobbyRat a; public SpriteRenderer bg; public TMP_Text text; public float t, dur; }
        class FxP { public SpriteRenderer r; public FxKind kind; public float u, v, du, t, dur; }
        readonly List<Bubble> bubbles = new(), bubblePool = new();
        readonly List<FxP> fx = new(), fxPool = new();
        readonly List<(float at, LobbyRat a, string text)> delayed = new();
        float t, nextEvent = 3, peekT = -1, wheelSpin;
        LobbyRat drag; bool dragMoved; Vector2 dragStart; readonly List<(Vector2 p, float t)> dragHist = new();
        LobbyHot pressedHot;
        public Func<bool> inputBlocked;            // 탈출 준비실이 열려 있으면 아지트 입력 막음

        // ── 좌표 ──
        public float Width => background.bounds.size.x;
        public float Height => background.bounds.size.y;
        public float Aspect => Width / Height;
        public Vector3 P(float u, float v) { var b = background.bounds; return new Vector3(b.min.x + u * b.size.x, b.max.y - v * b.size.y, 0); }
        public Vector2 ToUV(Vector3 w) { var b = background.bounds; return new Vector2((w.x - b.min.x) / b.size.x, (b.max.y - w.y) / b.size.y); }

        void Awake()
        {
            if (slotRoot) slots.AddRange(slotRoot.GetComponentsInChildren<LobbySlot>());
            if (hotRoot) hots.AddRange(hotRoot.GetComponentsInChildren<LobbyHot>());
            if (bubbleTemplate) bubbleTemplate.gameObject.SetActive(false);
            if (fxTemplate) fxTemplate.gameObject.SetActive(false);
        }

        void Start() { FitCamera(); MakeActors(); Refresh(); }

        // 배경판으로 화면을 꽉 채움 (cover)
        void FitCamera()
        {
            if (!cam || !background) return;
            float a = (float)Screen.width / Mathf.Max(1, Screen.height);
            cam.orthographicSize = Mathf.Min(Height / 2, Width / 2 / a);
            var c = background.bounds.center; cam.transform.position = new Vector3(c.x, c.y, cam.transform.position.z);
        }

        // ── 쥐 고르기: 만난 쥐 중 다양하게, 제일 높은 등급 한 마리는 꼭 ──
        List<RatCharacterRow> PickCast(int n)
        {
            var db = GameDatabase.Instance; var p = Progress.I;
            var pool = new List<RatCharacterRow>();
            foreach (var r in db.Rats.Values) if (p && p.Seen(r.code_id) && artLibrary.Get(r.code_id) != null) pool.Add(r);
            if (pool.Count == 0) foreach (var r in db.Rats.Values) if (r.Grade == Grade.Common && artLibrary.Get(r.code_id) != null) pool.Add(r);
            var cast = new List<RatCharacterRow>();
            if (pool.Count == 0) return cast;
            RatCharacterRow top = pool[0]; foreach (var r in pool) if (r.Grade > top.Grade) top = r;
            cast.Add(top);
            var shuffled = new List<RatCharacterRow>(pool); for (int i = 0; i < shuffled.Count; i++) { int j = Random.Range(i, shuffled.Count); (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]); }
            foreach (var r in shuffled) { if (cast.Count >= n) break; if (!cast.Contains(r)) cast.Add(r); }
            while (cast.Count < n) cast.Add(pool[Random.Range(0, pool.Count)]);
            return cast;
        }

        public void MakeActors()
        {
            foreach (var a in actors) if (a) Destroy(a.gameObject);
            actors.Clear();
            foreach (var s in slots) s.user = null;
            int tier = Progress.I ? Progress.I.tier : 1;
            foreach (var row in PickCast(Mathf.Clamp(5 + tier, castRange.x, castRange.y)))
            {
                var a = Instantiate(ratPrefab, actorRoot ? actorRoot : transform);
                a.Init(this, row, artLibrary.Get(row.code_id), rigLength);
                actors.Add(a);
            }
        }

        public List<LobbySlot> FreeSlots() { var l = new List<LobbySlot>(); foreach (var s in slots) if (!s.user) l.Add(s); return l; }
        public string RandomLine(string[] lines) => lines != null && lines.Length > 0 ? lines[Random.Range(0, lines.Length)] : "찍";

        // 티어·훈장·소품·팻말 다시 표시 (페이지에서 돌아올 때)
        public void Refresh()
        {
            int tier = Progress.I ? Progress.I.tier : 1;
            for (int i = 0; i < badges.Length; i++) if (badges[i]) badges[i].color = i < tier ? Color.white : new Color(0.55f, 0.47f, 0.4f, 0.25f);
            if (propCheese) propCheese.enabled = tier >= 2;
            if (propCup) propCup.enabled = tier >= 3;
            if (manager) foreach (var h in hots) h.Fill(manager.FillTokens, manager.Alert(h.id));
        }

        // ── 말풍선 · 효과 ──
        public void Say(LobbyRat a, string text, float dur = 2.4f)
        {
            if (!bubbleTemplate) return;
            for (int i = bubbles.Count - 1; i >= 0; i--) if (bubbles[i].a == a) { Recycle(bubbles[i]); bubbles.RemoveAt(i); }
            Bubble b;
            if (bubblePool.Count > 0) { b = bubblePool[^1]; bubblePool.RemoveAt(bubblePool.Count - 1); }
            else { var r = Instantiate(bubbleTemplate, bubbleTemplate.transform.parent); b = new Bubble { bg = r, text = r.GetComponentInChildren<TMP_Text>(true) }; }
            b.bg.gameObject.SetActive(true);
            b.a = a; b.t = 0; b.dur = dur; b.text.text = text; b.text.ForceMeshUpdate();
            float w = b.text.preferredWidth * b.text.transform.lossyScale.x, h = b.text.preferredHeight * b.text.transform.lossyScale.y;
            b.bg.size = new Vector2(w + bubblePad.x * 2, h + bubblePad.y * 2);
            var tail = b.bg.transform.Find("Tail");          // 꼬리는 늘리지 않고 아래 가운데에 붙임
            if (tail) { var tr = tail.GetComponent<SpriteRenderer>(); float th = tr && tr.sprite ? tr.bounds.size.y : 0; tail.localPosition = new Vector3(0, -b.bg.size.y / 2 - th / 2 + 0.02f, 0); }
            bubbles.Add(b);
        }
        void Recycle(Bubble b) { b.bg.gameObject.SetActive(false); bubblePool.Add(b); }
        void SayLater(float delay, LobbyRat a, string text) => delayed.Add((t + delay, a, text));

        public void Fx(FxKind kind, float u, float v, float du, float dur)
        {
            if (!fxTemplate || fx.Count > 80) return;
            FxP f;
            if (fxPool.Count > 0) { f = fxPool[^1]; fxPool.RemoveAt(fxPool.Count - 1); }
            else f = new FxP { r = Instantiate(fxTemplate, fxTemplate.transform.parent) };
            f.r.gameObject.SetActive(true);
            f.kind = kind; f.u = u; f.v = v; f.du = du; f.t = 0; f.dur = dur;
            f.r.sprite = kind switch { FxKind.Dust => fxDust, FxKind.Z => fxZ, FxKind.Crumb => fxCrumb, _ => fxSweat };
            f.r.color = kind switch { FxKind.Dust => new Color(0.92f, 0.86f, 0.76f), FxKind.Crumb => new Color(0.95f, 0.76f, 0.31f), _ => Color.white };
            fx.Add(f);
        }

        // 차 마시는 두 쥐가 가끔 수다
        public void TeaChat(LobbyRat a, float dt)
        {
            if (!a.slot || a.slot.transform.GetSiblingIndex() != FirstTeaIndex()) return;
            LobbyRat other = null; foreach (var b in actors) if (b != a && b.state == LobbyRat.State.Tea) other = b;
            if (!other || Talking(a) || Talking(other) || Random.value >= dt * 0.35f) return;
            var c = RandomLine(chatLines).Split('|'); Say(a, c[0], 2.4f); if (c.Length > 1) SayLater(1.7f, other, c[1]);
        }
        int FirstTeaIndex() { foreach (var s in slots) if (s.act == LobbySlot.Act.Tea) return s.transform.GetSiblingIndex(); return -1; }
        bool Talking(LobbyRat a) { foreach (var b in bubbles) if (b.a == a) return true; return false; }

        // ── 매 프레임 ──
        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f); t += dt;
            FitCamera();
            HandleInput();
            foreach (var a in actors) a.Tick(dt, t);
            // 가끔: 바닥의 두 쥐가 가까우면 마주보고 수다 · 쥐구멍 빼꼼
            if ((nextEvent -= dt) <= 0)
            {
                nextEvent = Random.Range(3f, 6f);
                var idle = actors.FindAll(a => a.state == LobbyRat.State.Idle);
                foreach (var a in idle)
                {
                    var b = idle.Find(c => c != a && Mathf.Abs(c.u - a.u) < 0.09f);
                    if (!b) continue;
                    a.face = b.u > a.u ? 1 : -1; b.face = -a.face; a.tState = b.tState = 4;
                    var c2 = RandomLine(chatLines).Split('|'); Say(a, c2[0]); if (c2.Length > 1) SayLater(1.6f, b, c2[1]);
                    break;
                }
                if (peekT < 0 && t > 8 && Random.value < 0.12f) StartPeek();
            }
            for (int i = delayed.Count - 1; i >= 0; i--) if (t >= delayed[i].at) { var d = delayed[i]; delayed.RemoveAt(i); if (d.a && d.a.state != LobbyRat.State.Held && d.a.state != LobbyRat.State.Air) Say(d.a, d.text, 2.2f); }
            Draw(dt);
        }

        void StartPeek()
        {
            if (!peekRat || !hole) return;
            var cast = PickCast(1); if (cast.Count == 0) return;
            peekRat.home = this; peekRat.gameObject.SetActive(true);
            peekRat.Init(this, cast[0], artLibrary.Get(cast[0].code_id), rigLength);
            peekT = 0;
        }

        void Draw(float dt)
        {
            // 쥐: 뒤(위쪽 v)부터, 잡힌 쥐는 맨 앞
            for (int i = 0; i < actors.Count; i++)
            {
                var a = actors[i];
                int order = 1000 + Mathf.RoundToInt(a.v * 2000) + (a.state == LobbyRat.State.Held ? 5000 : 0);
                a.Draw(t, order);
            }
            // 쥐구멍 빼꼼: 창틀 아래에 숨어 있다가 스르륵 올라와 두리번 → 다시 쏙
            if (peekRat)
            {
                if (peekT >= 0)
                {
                    peekT += dt; const float dur = 4;
                    static float Ease(float x) => x * x * (3 - 2 * x);
                    float k = peekT / dur, out_ = k < 0.25f ? Ease(k / 0.25f) : k > 0.75f ? Ease((1 - k) / 0.25f) : 1;
                    float R = hole.lossyScale.x * 0.5f, len = ratLen * Width * 0.8f, ratH = len * 0.75f;
                    var hp = hole.position;
                    peekRat.face = Mathf.Sin(peekT * 1.4f + peekRat.seed) < 0 ? 1 : -1;
                    peekRat.size = 0.8f; peekRat.state = LobbyRat.State.Idle;
                    var uv = ToUV(new Vector3(hp.x + R * 0.15f, hp.y - R - ratH + out_ * (ratH + R * 0.35f)));
                    peekRat.u = uv.x; peekRat.v = uv.y; peekRat.z = 0;
                    peekRat.Draw(t, 900);
                    if (peekRat.shadow) peekRat.shadow.enabled = false;
                    if (peekT > dur) { peekT = -1; peekRat.gameObject.SetActive(false); }
                }
            }
            // 쳇바퀴: 안에서 쥐가 달리면 바퀴가 돎 (쥐가 보는 방향의 반대로 바닥이 밀림), 내리면 서서히 멈춤
            var runner = actors.Find(a => a.state == LobbyRat.State.Wheel);
            float target = runner ? -runner.face * wheelSpinSpeed : 0;
            wheelSpin = Mathf.Lerp(wheelSpin, target, 1 - Mathf.Exp(-wheelSpinAccel * dt));
            if (wheelRing) wheelRing.transform.Rotate(0, 0, wheelSpin * dt);
            else if (wheel) wheel.transform.localRotation = Quaternion.Euler(0, 0, runner ? Mathf.Sin(t * 40) * 0.6f : 0);
            // 훈장 흔들림
            for (int i = 0; i < badges.Length; i++) if (badges[i]) badges[i].transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.5f + i) * 2.3f);
            // 말풍선
            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                var b = bubbles[i]; b.t += dt;
                if (b.t >= b.dur || !b.a) { Recycle(b); bubbles.RemoveAt(i); continue; }
                float k = Mathf.Min(1, b.t / 0.15f) * Mathf.Min(1, (b.dur - b.t) / 0.25f);
                var p = b.a.BodyPos + Vector3.up * (b.a.Len * 0.95f + b.bg.size.y * 0.5f);
                b.bg.transform.position = p + Vector3.up * 0.12f; b.bg.color = new Color(1, 1, 1, k); b.text.alpha = k;
                b.bg.sortingOrder = 9000; var tr = b.text.GetComponent<MeshRenderer>(); if (tr) tr.sortingOrder = 9001;
                var tl = b.bg.transform.Find("Tail"); if (tl) { var ts = tl.GetComponent<SpriteRenderer>(); ts.color = b.bg.color; ts.sortingOrder = 9000; }
            }
            // 효과
            float fs = Width * 0.012f;
            for (int i = fx.Count - 1; i >= 0; i--)
            {
                var f = fx[i]; f.t += dt;
                if (f.t >= f.dur) { f.r.gameObject.SetActive(false); fxPool.Add(f); fx.RemoveAt(i); continue; }
                float k = f.t / f.dur;
                float dv = f.kind == FxKind.Z ? 0.06f * k : f.kind == FxKind.Crumb ? -0.02f * k : f.kind == FxKind.Dust ? 0.012f * k : 0.01f * k;
                var p = P(f.u + f.du * k, f.v - dv);
                if (f.kind == FxKind.Z) p.x += Mathf.Sin(k * 6) * fs * 0.5f;
                f.r.transform.position = p;
                float size = f.kind switch { FxKind.Z => fs * (1 + k) * 1.4f, FxKind.Dust => fs * (0.5f + k * 0.8f) * 1.6f, FxKind.Crumb => fs * 0.6f, _ => fs * 0.6f };
                f.r.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, f.r.sprite ? f.r.sprite.bounds.size.x : 1));
                var c = f.r.color; c.a = 1 - k; f.r.color = c; f.r.sortingOrder = 8000;
            }
        }

        // ── 마우스: 쥐 누르기(점프) · 끌어서 던지기 · 물건 누르기(페이지) ──
        LobbyRat HitRat(Vector2 w)
        {
            LobbyRat best = null; float bd = 1e9f;
            foreach (var a in actors)
            {
                var c = a.BodyPos + Vector3.up * a.Len * 0.3f;
                float d = Vector2.Distance(w, c);
                if (d < a.Len * 0.65f && d < bd) { bd = d; best = a; }
            }
            return best;
        }

        void HandleInput()
        {
            var m = Mouse.current; if (m == null || !cam) return;
            bool blocked = inputBlocked != null && inputBlocked();
            Vector2 w = cam.ScreenToWorldPoint(m.position.ReadValue());
            if (!blocked && !drag) { var over = HitRat(w) ? null : hots.Find(h => h.Contains(w)); foreach (var h in hots) h.hover = h == over; }
            else foreach (var h in hots) h.hover = false;
            if (blocked) { drag = null; pressedHot = null; return; }
            if (m.leftButton.wasPressedThisFrame)
            {
                var a = HitRat(w);
                if (a) { drag = a; dragMoved = false; dragStart = w; dragHist.Clear(); dragHist.Add((w, Time.time)); }
                else pressedHot = hots.Find(h => h.Contains(w));
            }
            if (drag && m.leftButton.isPressed)
            {
                dragHist.Add((w, Time.time)); if (dragHist.Count > 6) dragHist.RemoveAt(0);
                var uv = ToUV(w); var uv0 = ToUV(dragStart);
                if (!dragMoved && Vector2.Distance(new Vector2(uv.x, uv.y / Aspect), new Vector2(uv0.x, uv0.y / Aspect)) > 0.012f) { dragMoved = true; drag.Grab(); }
                if (dragMoved)
                {
                    var p0 = ToUV(dragHist[0].p); float dtt = Mathf.Max(0.016f, Time.time - dragHist[0].t);
                    drag.HoldAt(uv.x, uv.y, (uv.x - p0.x) / dtt);
                }
            }
            if (m.leftButton.wasReleasedThisFrame)
            {
                if (drag)
                {
                    if (!dragMoved) drag.Poke();
                    else
                    {
                        var p0 = ToUV(dragHist[0].p); var p1 = ToUV(dragHist[^1].p); float dtt = Mathf.Max(0.016f, dragHist[^1].t - dragHist[0].t);
                        drag.Throw((p1.x - p0.x) / dtt, -(p1.y - p0.y) / dtt);
                    }
                    drag = null;
                }
                else if (pressedHot && pressedHot.Contains(w) && manager) manager.OnHot(pressedHot.id);
                pressedHot = null;
            }
        }
    }
}
