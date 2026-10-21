using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Ults
{
    // 필살기 로그 (딜 미터기 아래): 컷인 없이 시작한 필살기 (동시 필살기 · 카메라 고정 끔) 를 한 줄씩.
    // 새 줄은 맨 위에 옆에서 밀려 들어오고, 예전 줄은 아래로 밀림. life 초 뒤 흐려지며 사라짐. 줄이 모자라면 가장 오래된 줄을 다시 씀.
    // 줄은 하이라키에 미리 만들어 둔 것을 돌려 씀. 줄 자식: Icon(Image, 필살기 아이콘) · Text(자리: {grade} {name} {ult})
    public class UltLog : MonoBehaviour
    {
        [Tooltip("돌려 쓰는 줄들 (위 → 아래 보이는 최대 수)")] public RectTransform[] rows;
        [Tooltip("줄 간격 (px, 위 → 아래)")] public float rowGap = 34;
        [Tooltip("한 줄이 보이는 시간 (초) · 사라지는 시간")] public float life = 4, fadeTime = 0.6f;
        [Tooltip("들어올 때 옆에서 밀려오는 거리 (px) · 따라가기 빠르기")] public float enterShift = 60, slideSharp = 14;
        [Tooltip("필살기 이름을 필살기 색으로")] public bool tintUltName = true;
        [Tooltip("옅은 필살기 색은 밝은 바탕에서 안 보여서 진하게: 채도 최소 · 밝기 최대 (HSV)")] public float minSat = 0.55f, maxVal = 0.62f;

        class Line
        {
            public RectTransform rt; public CanvasGroup cg; public Image icon; public TMP_Text text; public string tpl;
            public float age = -1, x, y;
        }
        readonly List<Line> lines = new();       // 앞 = 가장 새 줄

        void Awake()
        {
            foreach (var r in rows)
            {
                if (!r) continue;
                var l = new Line { rt = r, cg = r.GetComponent<CanvasGroup>() ?? r.gameObject.AddComponent<CanvasGroup>() };
                l.icon = r.Find("Icon")?.GetComponent<Image>();
                l.text = r.Find("Text")?.GetComponent<TMP_Text>();
                l.tpl = l.text ? l.text.text : "{name} · {ult}";
                l.cg.alpha = 0; l.cg.blocksRaycasts = false;
                lines.Add(l);
            }
        }

        public void Add(Rat r, RatUltimateRow u, Sprite icon)
        {
            if (lines.Count == 0 || !r || u == null) return;
            var l = lines[^1];                       // 가장 오래된 (또는 빈) 줄을 맨 위로
            lines.RemoveAt(lines.Count - 1); lines.Insert(0, l);
            l.age = 0; l.x = enterShift; l.y = 0; l.rt.SetAsLastSibling();
            if (l.icon) { l.icon.sprite = icon; l.icon.enabled = icon; }
            if (l.text)
            {
                string ult = u.ultimate_name;
                if (tintUltName)
                {
                    Color.RGBToHSV(u.Color, out float h, out float s, out float v);
                    var c = Color.HSVToRGB(h, s < 0.08f ? s : Mathf.Max(s, minSat), Mathf.Min(v, maxVal));      // 흰색·회색은 색을 안 입히고 어둡게만
                    ult = $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{ult}</color>";
                }
                l.text.text = LocText.Tpl(l.text, l.tpl).Replace("{grade}", r.GradeData != null ? r.GradeData.grade_name : "").Replace("{name}", r.Data.character_name).Replace("{ult}", ult);
            }
            // 예전 줄은 지금 자리에서 아래로 밀려남 (y 는 Update 가 따라감)
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime, k = 1 - Mathf.Exp(-slideSharp * dt);
            bool age = !FxManager.Halt;              // 멈춤 창 · 일시 정지 중엔 안 사라짐
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                if (l.age < 0) { l.cg.alpha = 0; continue; }
                if (age) l.age += dt;
                if (l.age >= life) { l.age = -1; l.cg.alpha = 0; continue; }
                l.x += (0 - l.x) * k; l.y += (-i * rowGap - l.y) * k;
                l.rt.anchoredPosition = new Vector2(l.x, l.y);
                float fade = Mathf.Clamp01((life - l.age) / Mathf.Max(0.01f, fadeTime)), enter = Mathf.Clamp01(l.age / 0.15f);
                l.cg.alpha = Mathf.Min(fade, enter);
            }
        }
    }
}
