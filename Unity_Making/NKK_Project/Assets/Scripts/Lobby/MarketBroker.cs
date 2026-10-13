using TMPro;
using UnityEngine;

namespace NKK.Lobby
{
    // 쥐구멍 암시장 상인 '브로커 쥐' (2026-10-12): 노점 뒤에 서서 숨 쉬듯 들썩, 가끔 손짓·말풍선.
    // 뒷골목에 도착하면 인사, 누르면 수다 (누르기는 노점 물건 버튼이 받음 → 암시장 페이지). 대사 = 인스펙터 (씬에서 고침)
    public class MarketBroker : MonoBehaviour
    {
        public SpriteRenderer body;
        [Tooltip("자세: 기본 · 말하기 · 코트 열기 · 치즈 세기")] public Sprite idle, talk, offer, cheese;
        [Tooltip("말풍선 (꺼 둠) · 글")] public GameObject bubble;
        public TMP_Text bubbleText;
        [Tooltip("뒷골목 도착 인사 (해금 전 · 후)")] public string[] greetLocked, greetOpen;
        [Tooltip("가끔 혼잣말")] public string[] idleLines;
        [Tooltip("혼잣말 간격 (초)")] public Vector2 idleEvery = new(7, 13);
        [Tooltip("말풍선 시간 (초)")] public float bubbleTime = 2.6f;
        [Tooltip("숨쉬기 들썩임 (배율) · 빠르기")] public float breathe = 0.025f, breatheSpeed = 2.2f;

        float t, lineT, poseT;
        Vector3 baseScale;

        void Awake()
        {
            baseScale = transform.localScale;
            if (bubble) bubble.SetActive(false);
            lineT = Random.Range(idleEvery.x, idleEvery.y);
        }
        void OnEnable() { if (LobbyPan.I) LobbyPan.I.Moved += OnMoved; }
        void Start() { if (LobbyPan.I) { LobbyPan.I.Moved -= OnMoved; LobbyPan.I.Moved += OnMoved; } }
        void OnDisable() { if (LobbyPan.I) LobbyPan.I.Moved -= OnMoved; }

        void OnMoved(bool atRoom)
        {
            if (!atRoom) return;
            bool open = Progress.I && Progress.I.IsUnlocked("market");
            Say(Pick(open ? greetOpen : greetLocked), open ? offer : talk);
        }

        static string Pick(string[] l) => l != null && l.Length > 0 ? l[Random.Range(0, l.Length)] : null;

        public void Say(string line, Sprite pose = null)
        {
            if (string.IsNullOrEmpty(line)) return;
            if (bubble) bubble.SetActive(true);
            if (bubbleText) bubbleText.text = line;
            FitBubble();
            t = bubbleTime; poseT = bubbleTime;
            if (body && (pose || talk)) body.sprite = pose ? pose : talk;
        }

        [Tooltip("말풍선 여백 (월드 유닛)")] public Vector2 bubblePad = new(0.22f, 0.14f);
        // 아지트 말풍선과 같은 모양: 글 크기에 맞춰 판을 늘리고, 꼬리는 아래 가운데, 머리 위에
        void FitBubble()
        {
            if (!bubble || !bubbleText) return;
            var bg = bubble.GetComponent<SpriteRenderer>();
            bubbleText.ForceMeshUpdate(); bubbleText.alpha = 1;
            float w = bubbleText.preferredWidth * bubbleText.transform.lossyScale.x, h = bubbleText.preferredHeight * bubbleText.transform.lossyScale.y;
            if (bg) { bg.size = new Vector2(w + bubblePad.x * 2, h + bubblePad.y * 2); bg.color = Color.white; bg.sortingOrder = 9000; }
            var mr = bubbleText.GetComponent<MeshRenderer>(); if (mr) mr.sortingOrder = 9001;
            var tail = bubble.transform.Find("Tail");
            if (tail && bg) { var tr = tail.GetComponent<SpriteRenderer>(); float th = tr && tr.sprite ? tr.bounds.size.y : 0; tail.localPosition = new Vector3(0, -bg.size.y / 2 - th / 2 + 0.02f, 0); if (tr) { tr.color = Color.white; tr.sortingOrder = 9000; } }
            if (body && bg) bubble.transform.position = new Vector3(body.bounds.center.x, body.bounds.max.y + bg.size.y / 2 + 0.2f, 0);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            transform.localScale = new Vector3(baseScale.x, baseScale.y * (1 + breathe * Mathf.Sin(Time.time * breatheSpeed)), baseScale.z);
            if (t > 0 && (t -= dt) <= 0 && bubble) bubble.SetActive(false);
            if (poseT > 0 && (poseT -= dt) <= 0 && body && idle) body.sprite = idle;
            bool here = LobbyPan.I && LobbyPan.I.AtRoom;
            if (here && (lineT -= dt) <= 0)
            {
                lineT = Random.Range(idleEvery.x, idleEvery.y);
                Say(Pick(idleLines), Random.value < 0.5f ? cheese : talk);
            }
        }
    }
}
