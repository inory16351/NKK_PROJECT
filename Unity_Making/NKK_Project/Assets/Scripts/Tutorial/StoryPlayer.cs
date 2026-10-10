using System;
using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NKK.Tutorial
{
    // 오프닝 컷씬 (씬 오브젝트 Story). 튜토리얼 테이블 Story 시트를 한 줄씩: 컷 그림(cut · image) + 아래 자막.
    // 클릭·스페이스 = 글 다 보이기 → 다음 줄, 컷이 바뀌면 그림이 겹쳐 바뀜 (천천히 확대). 건너뛰기 버튼 = 바로 끝.
    // 시간은 unscaled. 글 자체는 테이블, 버튼 글은 씬 TMP
    public class StoryPlayer : MonoBehaviour
    {
        [Tooltip("컷씬 전체 (꺼 둠)")] public CanvasGroup group;
        [Tooltip("컷 그림 두 장 (번갈아 쓰며 겹쳐 바꿈)")] public Image cutA, cutB;
        [Tooltip("자막 판 (들어올 때 살짝 올라옴)")] public RectTransform captionPanel;
        public TMP_Text captionText;
        [Tooltip("글이 다 나오면 깜빡이는 '계속' 표시")] public GameObject nextMark;
        public Button skipButton;
        [Tooltip("컷 그림 (이름 = 테이블 image)")] public List<FxManager.NamedSprite> cuts = new();

        [Header("연출")]
        [Tooltip("글자 나오는 속도 (초당 글자 수)")] public float charsPerSec = 26;
        [Tooltip("컷이 바뀔 때 겹치는 시간 (초)")] public float crossTime = 0.6f;
        [Tooltip("컷 하나가 보이는 동안 천천히 확대 (배율/초)")] public float zoomPerSec = 0.012f;
        [Tooltip("처음 · 끝 검은 화면에서 밝아지는 / 어두워지는 시간 (초)")] public float fadeTime = 0.6f;

        public bool Playing { get; private set; }

        readonly List<TutoStoryRow> rows = new();
        Action done;
        int idx = -1, curCut = -1;
        float shown, crossT, zoomT, fadeT;
        bool ending, aFront;

        void Awake()
        {
            if (skipButton) skipButton.onClick.AddListener(Finish);
            // 씬에선 꺼 둔 채로 둠 (Play 가 켬 → 이 Awake 가 그때 돌아서 여기서 다시 끄면 안 됨)
            if (group && !Playing) group.alpha = 0;
        }

        public void Play(Action onDone)
        {
            var db = GameDatabase.Instance;
            rows.Clear(); if (db) rows.AddRange(db.Story);
            done = onDone;
            if (rows.Count == 0) { onDone?.Invoke(); return; }
            Playing = true; ending = false; idx = -1; curCut = -1; fadeT = 0; aFront = false;
            if (group) { group.gameObject.SetActive(true); group.alpha = 0; group.blocksRaycasts = true; }
            if (cutA) cutA.color = new Color(1, 1, 1, 0); if (cutB) cutB.color = new Color(1, 1, 1, 0);
            Next();
        }

        Sprite Find(string n) { foreach (var c in cuts) if (c.name == n) return c.sprite; return null; }
        Image Front => aFront ? cutA : cutB;
        Image Back => aFront ? cutB : cutA;

        void Next()
        {
            idx++;
            if (idx >= rows.Count) { Finish(); return; }
            var r = rows[idx];
            if (r.cut != curCut)
            {
                curCut = r.cut;
                if (idx > 0) SfxManager.Play("story_page", 0.6f);
                aFront = !aFront;
                var f = Front; if (f) { f.sprite = Find(r.image); f.transform.SetAsLastSibling(); f.color = new Color(1, 1, 1, 0); f.rectTransform.localScale = Vector3.one; }
                if (captionPanel) captionPanel.SetAsLastSibling();
                if (skipButton) skipButton.transform.SetAsLastSibling();      // 자막 · 건너뛰기는 늘 컷 그림 위
                crossT = 0; zoomT = 0;
            }
            if (captionText) { captionText.text = r.text; captionText.maxVisibleCharacters = 0; captionText.ForceMeshUpdate(); }
            shown = 0;
            if (nextMark) nextMark.SetActive(false);
        }

        // 건너뛰기 · 마지막 줄 다음: 어두워진 뒤 done
        public void Finish()
        {
            if (!Playing || ending) return;
            ending = true; fadeT = 0;
        }

        void Update()
        {
            if (!Playing) return;
            float dt = Time.unscaledDeltaTime;
            if (ending)
            {
                fadeT += dt;
                if (group) group.alpha = 1 - Mathf.Clamp01(fadeT / fadeTime);
                if (fadeT >= fadeTime)
                {
                    Playing = false;
                    if (group) { group.blocksRaycasts = false; group.gameObject.SetActive(false); }
                    var d = done; done = null; d?.Invoke();
                }
                return;
            }
            fadeT += dt;
            if (group) group.alpha = Mathf.Clamp01(fadeT / fadeTime);
            // 컷 겹쳐 바꾸기 + 천천히 확대
            crossT += dt; zoomT += dt;
            float k = crossTime > 0 ? Mathf.Clamp01(crossT / crossTime) : 1;
            if (Front) { Front.color = new Color(1, 1, 1, k); Front.rectTransform.localScale = Vector3.one * (1 + zoomPerSec * zoomT); }
            if (Back && k >= 1) Back.color = new Color(1, 1, 1, 0);
            // 글자
            int total = captionText ? captionText.textInfo.characterCount : 0;
            if (captionText) { shown += dt * charsPerSec; captionText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown)); }
            bool full = !captionText || captionText.maxVisibleCharacters >= total;
            if (nextMark) { nextMark.SetActive(full); if (full) nextMark.transform.localScale = Vector3.one * (1 + 0.08f * Mathf.Sin(Time.unscaledTime * 6)); }
            if (fadeT > fadeTime * 0.5f && Pressed())
            {
                if (!full) { shown = total; if (captionText) captionText.maxVisibleCharacters = total; }
                else Next();
            }
        }

        static bool Pressed()
        {
            var m = Mouse.current; var kb = Keyboard.current;
            // 건너뛰기 버튼 위 클릭은 버튼이 처리
            if (m != null && m.leftButton.wasPressedThisFrame) return !IsOverButton();
            return kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
        }

        static bool IsOverButton()
        {
            var es = UnityEngine.EventSystems.EventSystem.current; var m = Mouse.current;
            if (!es || m == null) return false;
            var data = new UnityEngine.EventSystems.PointerEventData(es) { position = m.position.ReadValue() };
            var hits = new List<UnityEngine.EventSystems.RaycastResult>(); es.RaycastAll(data, hits);
            foreach (var h in hits) if (h.gameObject.GetComponentInParent<Button>()) return true;
            return false;
        }
    }
}
