using System;
using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NKK.Tutorial
{
    // 튜토리얼 대사창 (씬 오브젝트 TutorialBox). 아래쪽 대사 판 + 왼쪽 해설 쥐 초상화 + 이름표 + 글.
    // 대사가 떠 있는 동안 화면 전체(blocker)가 클릭을 받음 → 글자가 다 나오면 다음 대사, 나오는 중이면 한 번에 다 보여 줌.
    // 시간은 unscaled (게임이 멈춰 있어도 돌아감). 글 자체는 튜토리얼 테이블 Line 시트
    public class TutorialBox : MonoBehaviour
    {
        public static TutorialBox I { get; private set; }
        // 대사창이 떠 있음 (게임 입력·카메라 끌기 막음)
        public static bool Showing => I && I.busy;

        [Header("씬 연결")]
        [Tooltip("대사창 전체 (꺼 둠)")] public CanvasGroup group;
        [Tooltip("화면 전체를 덮는 투명 판 (클릭 받기)")] public Image blocker;
        [Tooltip("아래 대사 판 (들어올 때 아래에서 올라옴)")] public RectTransform panel;
        public Image portrait;
        public TMP_Text nameText, bodyText;
        [Tooltip("글이 다 나오면 깜빡이는 '계속' 표시")] public GameObject nextMark;
        [Tooltip("강조 테두리 (가리킬 곳을 감쌈, 꺼 둠)")] public RectTransform highlight;
        [Tooltip("강조 테두리 옆 화살표 (선택)")] public RectTransform arrow;

        [Header("해설 쥐 초상화 (이름 = tuto_<쥐>_<표정>)")]
        public List<FxManager.NamedSprite> portraits = new();

        [Header("연출")]
        [Tooltip("글자 나오는 속도 (초당 글자 수)")] public float charsPerSec = 40;
        [Tooltip("대사창 들어오는 시간 (초)")] public float inTime = 0.22f;
        [Tooltip("초상화가 바뀔 때 통 튀는 크기")] public float portraitBump = 0.08f;
        [Tooltip("강조 테두리 숨쉬기 크기 (픽셀)")] public float highlightPulse = 8;
        [Tooltip("화살표 위아래 흔들림 (픽셀)")] public float arrowBob = 12;
        [Tooltip("화자가 없을 때 이름표에 쓸 글")] public string noSpeakerName = "";

        [Header("가리키는 곳 보이게")]
        [Tooltip("가리키는 곳이 대사 판·초상화에 가리면 대사창을 화면 위쪽으로 옮김")] public bool dodge = true;
        [Tooltip("위로 옮긴 대사 판의 화면 위 여백 (픽셀)")] public float dodgeTopMargin = 28;
        [Tooltip("위로 옮긴 초상화의 화면 위 여백 (픽셀)")] public float dodgePortraitMargin = 8;
        [Tooltip("옮겨 가는 빠르기")] public float dodgeSpeed = 12;
        [Tooltip("가리키는 UI 를 이만큼 키워서 보여 줌 (1 = 그대로, 대상마다 TutorialTarget.zoom 으로 따로 정할 수 있음)")] public float targetZoom = 1.25f;
        [Tooltip("키우는 빠르기")] public float targetZoomSpeed = 10;

        readonly List<TutoLineRow> lines = new();
        Action done;
        bool busy;
        int idx; float shown, inT, bumpT;
        string pointer; float pointerLeft;       // 대사가 끝난 뒤에도 잠깐 남는 강조
        Vector2 panelPos, portraitPos;
        float dodgeK, dodgeGoal;          // 0 = 아래(기본) · 1 = 위로 옮김
        TutorialTarget zoomed;             // 지금 키워 둔 대상
        // 지금 강조 중인 대상 (TutorialManager 가 월드 대상이면 카메라로 비춤)
        public TutorialTarget CurrentTarget { get; private set; }
        string blip; int lastVis;           // 글자 나올 때 '찍' 소리 (화자별)
        Canvas canvas;

        void Awake()
        {
            I = this;
            canvas = GetComponentInParent<Canvas>();
            if (panel) panelPos = panel.anchoredPosition;
            if (portrait) portraitPos = portrait.rectTransform.anchoredPosition;
            Hide();
        }
        void OnDestroy() { if (I == this) I = null; }

        // 대사 여러 줄을 차례로 보여 주고 끝나면 done
        public void Play(List<TutoLineRow> rows, Action onDone)
        {
            lines.Clear(); if (rows != null) lines.AddRange(rows);
            done = onDone;
            if (lines.Count == 0) { onDone?.Invoke(); return; }
            busy = true; idx = 0; inT = 0; pointer = null; pointerLeft = 0;
            if (group) { group.gameObject.SetActive(true); group.alpha = 0; group.blocksRaycasts = true; }
            if (blocker) blocker.raycastTarget = true;
            ShowLine();
            dodgeGoal = DodgeGoal(lines[0].highlight); dodgeK = dodgeGoal;      // 첫 줄은 처음부터 그 자리에서
        }

        // 대사가 끝난 뒤에도 강조를 몇 초 더 (0 이하 = 끔)
        public void Linger(string id, float seconds) { pointer = id; pointerLeft = seconds; }
        public string LingerId => pointerLeft > 0 ? pointer : null;
        public void ClearLinger() { pointer = null; pointerLeft = 0; if (!busy) SetHighlight(null); }

        void ShowLine()
        {
            var l = lines[idx];
            var db = GameDatabase.Instance;
            TutoSpeakerRow sp = null;
            if (db && !string.IsNullOrEmpty(l.speaker_id)) db.TutoSpeakers.TryGetValue(l.speaker_id, out sp);
            if (nameText) nameText.text = sp != null ? sp.speaker_name : noSpeakerName;
            blip = sp != null ? "tuto_blip_" + sp.speaker_id : null; lastVis = 0;
            if (portrait)
            {
                var spr = sp != null ? Find($"{sp.portrait}_{(string.IsNullOrEmpty(l.face) ? "normal" : l.face)}") ?? Find(sp.portrait + "_normal") : null;
                bool changed = portrait.sprite != spr;
                portrait.sprite = spr; portrait.enabled = spr;
                if (changed) bumpT = 0.25f;
            }
            if (bodyText) { bodyText.text = l.text; bodyText.maxVisibleCharacters = 0; bodyText.ForceMeshUpdate(); }
            shown = 0;
            if (nextMark) nextMark.SetActive(false);
        }

        Sprite Find(string n) { foreach (var p in portraits) if (p.name == n) return p.sprite; return null; }

        int TotalChars => bodyText ? bodyText.textInfo.characterCount : 0;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!busy)
            {
                if (pointerLeft > 0) { pointerLeft -= dt; SetHighlight(pointerLeft > 0 ? pointer : null); }
                return;
            }
            // 들어오기
            inT = Mathf.Min(inTime, inT + dt);
            float k = inTime > 0 ? inT / inTime : 1, e = 1 - (1 - k) * (1 - k);
            if (group) group.alpha = e;
            dodgeGoal = DodgeGoal(lines[idx].highlight);
            dodgeK = Mathf.Lerp(dodgeK, dodgeGoal, Mathf.Min(1, dt * dodgeSpeed));
            Offsets(out float panelUp, out float portraitUp);
            if (panel) panel.anchoredPosition = panelPos + new Vector2(0, panelUp * dodgeK - 60 * (1 - e) * (dodgeK > 0.5f ? -1 : 1));
            if (portrait) portrait.rectTransform.anchoredPosition = portraitPos + new Vector2(0, portraitUp * dodgeK);
            if (portrait)
            {
                bumpT = Mathf.Max(0, bumpT - dt);
                portrait.rectTransform.localScale = Vector3.one * (1 + portraitBump * Mathf.Sin(bumpT / 0.25f * Mathf.PI));
            }
            // 글자
            if (bodyText)
            {
                shown += dt * charsPerSec;
                bodyText.maxVisibleCharacters = Mathf.Min(TotalChars, Mathf.FloorToInt(shown));
                int vis = bodyText.maxVisibleCharacters;
                if (vis > lastVis) { if (vis / 3 != lastVis / 3) SfxManager.Play(blip, 0.5f); lastVis = vis; }
            }
            bool full = !bodyText || bodyText.maxVisibleCharacters >= TotalChars;
            if (nextMark) { nextMark.SetActive(full); if (full) nextMark.transform.localScale = Vector3.one * (1 + 0.08f * Mathf.Sin(Time.unscaledTime * 6)); }
            SetHighlight(lines[idx].highlight);

            if (inT >= inTime && Pressed())
            {
                if (!full) { shown = TotalChars; if (bodyText) bodyText.maxVisibleCharacters = TotalChars; }
                else Next();
            }
        }

        static bool Pressed()
        {
            var m = Mouse.current; var kb = Keyboard.current;
            return (m != null && m.leftButton.wasPressedThisFrame)
                || (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame));
        }

        void Next()
        {
            SfxManager.Play("tuto_next");
            idx++;
            if (idx < lines.Count) { ShowLine(); return; }
            Hide();
            var d = done; done = null; d?.Invoke();
        }

        // ── 대사창 피하기 ──
        // 위로 옮길 때 대사 판·초상화가 올라가는 거리 (캔버스 단위)
        void Offsets(out float panelUp, out float portraitUp)
        {
            panelUp = portraitUp = 0;
            if (!canvas) return;
            var top = (canvas.transform as RectTransform).rect.yMax;
            if (panel) panelUp = top - dodgeTopMargin - BaseRect(panel, panelPos).yMax;
            if (portrait) portraitUp = Mathf.Min(panelUp, top - dodgePortraitMargin - BaseRect(portrait.rectTransform, portraitPos).yMax);
        }

        // 기본(아래) 자리에 있을 때의 캔버스 안 사각형
        Rect BaseRect(RectTransform rt, Vector2 basePos)
        {
            var cr = canvas.transform as RectTransform;
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            Vector2 a = cr.InverseTransformPoint(c[0]), b = cr.InverseTransformPoint(c[2]);
            var d = basePos - rt.anchoredPosition;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x) + d.x, Mathf.Min(a.y, b.y) + d.y, Mathf.Max(a.x, b.x) + d.x, Mathf.Max(a.y, b.y) + d.y);
        }

        // 가리킬 곳이 아래 대사창에 가리면 1 (위쪽에서도 가리면 덜 가리는 쪽)
        float DodgeGoal(string id)
        {
            if (!dodge || !canvas) return 0;
            var t = TutorialTarget.Find(id);
            if (!t || !t.GoalScreenRect(out var sr)) return 0;
            if (t.IsWorld) return 0;          // 월드 대상은 카메라가 화면 가운데로 비춤
            var cr = canvas.transform as RectTransform;
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cr, sr.min, cam, out var a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cr, sr.max, cam, out var b);
            var tr = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            Offsets(out float panelUp, out float portraitUp);
            float down = Cover(tr, 0, 0), up = Cover(tr, panelUp, portraitUp);
            if (down <= 0) return 0;
            return up < down ? 1 : 0;
        }

        // 대사 판·초상화가 사각형 r 을 가리는 넓이
        float Cover(Rect r, float panelUp, float portraitUp)
        {
            float s = 0;
            if (panel) s += Overlap(r, BaseRect(panel, panelPos), panelUp);
            if (portrait && portrait.enabled) s += Overlap(r, BaseRect(portrait.rectTransform, portraitPos), portraitUp);
            return s;
        }
        static float Overlap(Rect a, Rect b, float up)
        {
            b.y += up;
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return w > 0 && h > 0 ? w * h : 0;
        }

        void Hide()
        {
            busy = false;
            if (group) { group.alpha = 0; group.blocksRaycasts = false; group.gameObject.SetActive(false); }
            if (blocker) blocker.raycastTarget = false;
            SetHighlight(pointerLeft > 0 ? pointer : null);
        }

        // 강조 테두리를 그 대상 위에 (없으면 숨김). 테두리·화살표는 대사창 group 밖에 둬야 대사가 끝난 뒤에도 보임
        void SetHighlight(string id)
        {
            if (!highlight) return;
            var t = TutorialTarget.Find(id);
            if (zoomed != t)
            {
                if (zoomed) zoomed.SetZoom(1, targetZoomSpeed);
                zoomed = t;
                if (t) t.SetZoom(t.zoom > 0 ? t.zoom : targetZoom, targetZoomSpeed);
            }
            CurrentTarget = t;
            if (!t || !t.ScreenRect(out var r) || !canvas)
            {
                highlight.gameObject.SetActive(false); if (arrow) arrow.gameObject.SetActive(false);
                return;
            }
            if (!highlight.gameObject.activeSelf) SfxManager.Play("tuto_highlight", 0.6f);
            highlight.gameObject.SetActive(true);
            var cr = canvas.transform as RectTransform;
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cr, r.min, cam, out var a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cr, r.max, cam, out var b);
            float p = highlightPulse * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5));
            highlight.anchorMin = highlight.anchorMax = new Vector2(0.5f, 0.5f);
            highlight.pivot = new Vector2(0.5f, 0.5f);
            highlight.anchoredPosition = (a + b) / 2 - cr.rect.center;
            highlight.sizeDelta = new Vector2(Mathf.Abs(b.x - a.x) + p * 2, Mathf.Abs(b.y - a.y) + p * 2);
            if (arrow)
            {
                arrow.gameObject.SetActive(true);
                // 대상이 화면 위쪽이면 화살표를 아래에, 아래쪽이면 위에
                bool top = (r.center.y / Screen.height) > 0.5f;
                float h = Mathf.Abs(b.y - a.y) / 2 + p + arrow.rect.height * 0.6f + arrowBob * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6));
                arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
                arrow.anchoredPosition = highlight.anchoredPosition + new Vector2(0, top ? -h : h);
                arrow.localRotation = Quaternion.Euler(0, 0, top ? 0 : 180);      // 그림은 위를 가리킴
            }
        }
    }
}
