using TMPro;
using UnityEngine;

namespace NKK.Lobby
{
    // 뒷골목 미니 게임기 (2026-10-30): 10훈장 승급 뒤 튜토리얼 l_arcade 가 'arcade' 를 해금하면 암시장 왼쪽에 나타남 (FeatureGate).
    // 누르면 (물건 버튼 Hot_arcade → LobbyManager.OnHot) 통 튀고 말풍선. 미니게임 화면이 생기면 Play 에서 열면 됨.
    // 말풍선 대사 = 인스펙터 (씬에서 고침)
    public class ArcadeMachine : MonoBehaviour
    {
        public static ArcadeMachine I { get; private set; }

        public SpriteRenderer body;
        [Tooltip("말풍선 (꺼 둠) · 글")] public GameObject bubble;
        public TMP_Text bubbleText;
        [Tooltip("눌렀을 때 대사 (하나 골라 씀)")] public string[] pokeLines;
        [Tooltip("말풍선 시간 (초)")] public float bubbleTime = 2.4f;
        [Tooltip("말풍선 여백 (월드 유닛)")] public Vector2 bubblePad = new(0.22f, 0.14f);
        [Tooltip("말풍선 높이: 게임기 꼭대기에서 말풍선 아래 끝까지 (월드 유닛, 음수 = 게임기에 겹침 — 위 팻말을 피함)")] public float bubbleLift = -0.35f;
        [Tooltip("눌렀을 때 통 튀기: 시간 (초) · 세기")] public float pokeTime = 0.35f, pokeSquash = 0.12f;
        [Tooltip("눌렀을 때 효과음")] public string pokeSfx = "ui_open";

        Vector3 baseScale;
        float bubbleT, pokeT = -1;

        void Awake()
        {
            I = this;
            baseScale = transform.localScale;
            if (bubble) bubble.SetActive(false);
        }
        void OnDestroy() { if (I == this) I = null; }
        // 꺼질 때 (잠김 · 씬 이동) 크기·말풍선을 원래대로
        void OnDisable() { pokeT = -1; bubbleT = 0; if (bubble) bubble.SetActive(false); if (!FeatureGateRevealing()) transform.localScale = baseScale; }

        bool FeatureGateRevealing() => NKK.Tutorial.FeatureGate.IsRevealing(transform);

        public void Poke()
        {
            if (!string.IsNullOrEmpty(pokeSfx)) SfxManager.Play(pokeSfx);
            pokeT = 0;
            if (pokeLines != null && pokeLines.Length > 0) Say(pokeLines[Random.Range(0, pokeLines.Length)]);
        }

        public void Say(string line)
        {
            if (string.IsNullOrEmpty(line) || !bubble) return;
            bubble.SetActive(true);
            if (bubbleText) bubbleText.text = line;
            FitBubble();
            bubbleT = bubbleTime;
        }

        // 브로커 말풍선과 같은 모양: 글 크기에 맞춰 판을 늘리고, 꼬리는 아래 가운데, 게임기 위에
        void FitBubble()
        {
            if (!bubbleText) return;
            var bg = bubble.GetComponent<SpriteRenderer>();
            bubbleText.ForceMeshUpdate(); bubbleText.alpha = 1;
            float w = bubbleText.preferredWidth * bubbleText.transform.lossyScale.x, h = bubbleText.preferredHeight * bubbleText.transform.lossyScale.y;
            if (bg) { bg.size = new Vector2(w + bubblePad.x * 2, h + bubblePad.y * 2); bg.color = Color.white; bg.sortingOrder = 9000; }
            var mr = bubbleText.GetComponent<MeshRenderer>(); if (mr) mr.sortingOrder = 9001;
            var tail = bubble.transform.Find("Tail");
            if (tail && bg) { var tr = tail.GetComponent<SpriteRenderer>(); float th = tr && tr.sprite ? tr.bounds.size.y : 0; tail.localPosition = new Vector3(0, -bg.size.y / 2 - th / 2 + 0.02f, 0); if (tr) { tr.color = Color.white; tr.sortingOrder = 9000; } }
            if (body && bg) bubble.transform.position = new Vector3(body.bounds.center.x, body.bounds.max.y + bg.size.y / 2 + bubbleLift, 0);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (bubbleT > 0 && (bubbleT -= dt) <= 0 && bubble) bubble.SetActive(false);
            if (pokeT >= 0 && !FeatureGateRevealing())
            {
                pokeT += dt;
                float k = Mathf.Clamp01(pokeT / pokeTime), s = Mathf.Sin(k * Mathf.PI) * pokeSquash * (1 - k);
                transform.localScale = new Vector3(baseScale.x * (1 + s), baseScale.y * (1 - s), baseScale.z);
                if (k >= 1) { pokeT = -1; transform.localScale = baseScale; }
            }
        }
    }
}
