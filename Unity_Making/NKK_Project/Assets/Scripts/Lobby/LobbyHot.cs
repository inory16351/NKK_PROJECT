using TMPro;
using UnityEngine;

namespace NKK.Lobby
{
    // 아지트 물건 버튼 + 그 위에 끈으로 매달린 판자 팻말 (웹 .home-hot / .home-tag).
    // 물건 영역 = BoxCollider2D (씬에서 크기 조절). 팻말·물건 둘 중 하나에 마우스를 올리면 둘 다 빛남.
    [RequireComponent(typeof(BoxCollider2D))]
    public class LobbyHot : MonoBehaviour
    {
        [Tooltip("페이지 id: run · rank · rats · skill · dex · rec")] public string id = "run";
        [Tooltip("주 버튼 (작전 탁자 = 출발): 반짝임")] public bool main;
        [Header("자식")]
        public SpriteRenderer highlight;
        public Transform tag;
        public SpriteRenderer plank;
        public TMP_Text title, sub;
        public GameObject alertDot;
        [Header("움직임")]
        [Tooltip("팻말 흔들림 (도)")] public float sway = 1.5f;
        public float swaySpeed = 2f;
        [Tooltip("팻말 좌우 여백 (월드 유닛)")] public float padX = 0.35f;

        [HideInInspector] public bool hover;
        BoxCollider2D box;
        float hl, phase;
        string titleTpl, subTpl;          // 씬에 적힌 글 (자리표시 {tier} 등 포함)

        void Awake()
        {
            box = GetComponent<BoxCollider2D>(); phase = Random.Range(0, 6f);
            titleTpl = title ? title.text : ""; subTpl = sub ? sub.text : "";
        }

        public bool Contains(Vector2 p)
        {
            if (box && box.OverlapPoint(p)) return true;
            return plank && plank.bounds.Contains(new Vector3(p.x, p.y, plank.bounds.center.z));
        }

        // 씬에 적힌 글의 자리표시를 값으로 바꿈 (글 자체는 하이라키에서 고침)
        public void Fill(System.Func<string, string> fill, bool alert)
        {
            if (title) title.text = fill(titleTpl);
            if (sub) sub.text = fill(subTpl);
            if (alertDot) alertDot.SetActive(alert);
            // 글자 폭에 맞춰 판자 크기
            if (plank && title)
            {
                title.ForceMeshUpdate(); if (sub) sub.ForceMeshUpdate();
                float w = Mathf.Max(title.preferredWidth, sub ? sub.preferredWidth : 0) * title.transform.lossyScale.x;
                if (plank.drawMode != SpriteDrawMode.Simple) plank.size = new Vector2(w + padX * 2, plank.size.y);
            }
        }

        void Update()
        {
            float t = Time.time;
            hl = Mathf.MoveTowards(hl, hover ? 1 : 0, Time.deltaTime * 6);
            if (highlight)
            {
                float pulse = main ? 0.25f + 0.2f * Mathf.Sin(t * 4) : 0;
                var c = highlight.color; c.a = Mathf.Max(hl * 0.55f, pulse * 0.6f); highlight.color = c;
            }
            if (tag)
            {
                tag.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * swaySpeed + phase) * sway);
                tag.localScale = Vector3.one * (1 + hl * 0.1f + (main ? 0.03f * Mathf.Sin(t * 4) : 0));
            }
        }
    }
}
