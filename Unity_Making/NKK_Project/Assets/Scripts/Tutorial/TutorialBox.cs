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

        readonly List<TutoLineRow> lines = new();
        Action done;
        bool busy;
        int idx; float shown, inT, bumpT;
        string pointer; float pointerLeft;       // 대사가 끝난 뒤에도 잠깐 남는 강조
        Vector2 panelPos;
        string blip; int lastVis;           // 글자 나올 때 '찍' 소리 (화자별)
        Canvas canvas;

        void Awake()
        {
            I = this;
            canvas = GetComponentInParent<Canvas>();
            if (panel) panelPos = panel.anchoredPosition;
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
            if (panel) panel.anchoredPosition = panelPos + new Vector2(0, -60 * (1 - e));
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
