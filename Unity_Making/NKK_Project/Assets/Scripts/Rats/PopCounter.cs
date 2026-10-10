using TMPro;
using UnityEngine;

namespace NKK.Rats
{
    // HUD 쥐 마릿수: 지금 있는 쥐 / 최대 인구 (화면 오른쪽 위, 제한시간 아래). 글은 씬 TMP (자리표시 {n} {max})
    // 최대에 닿으면 색이 바뀜 (더 번식 안 함 → 승급하라는 신호), 마릿수가 바뀌면 살짝 통통
    public class PopCounter : MonoBehaviour
    {
        public RatManager Rats;
        public TMP_Text text;
        public Color normalColor = Color.white;
        [Tooltip("최대 인구에 닿았을 때 색")] public Color fullColor = new(1f, 0.72f, 0.45f);
        [Tooltip("마릿수가 바뀔 때 통통 (배율)")] public float bump = 0.15f;

        string format;
        int last = -1;
        float pulse;

        void Awake() { if (!text) text = GetComponent<TMP_Text>(); if (text) format = text.text; }

        void LateUpdate()
        {
            if (!Rats || !text) return;
            int n = Rats.RealCount, max = Rats.PopCap;
            int key = n * 100000 + max;
            if (key != last) { if (last >= 0 && last / 100000 != n) pulse = 1; last = key; text.text = (format ?? "{n} / {max}").Replace("{n}", n.ToString()).Replace("{max}", max.ToString()); }
            text.color = n >= max ? fullColor : normalColor;
            pulse = Mathf.Max(0, pulse - Time.unscaledDeltaTime * 5);
            text.transform.localScale = Vector3.one * (1 + bump * pulse * pulse);
        }
    }
}
