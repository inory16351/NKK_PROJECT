using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Ults
{
    /// <summary>
    /// 초필살기 컷신 레이어 하나 (메이플 오리진 스킬식: 배경 · 캐릭터 · 이펙트 · 소품을 따로 움직임, 2026-10-17).
    /// 컷신 시간(초)마다 키 (위치 px · 크기 · 회전 · 투명도) → 사이를 이징으로 이음. 그 위에 계속 도는 효과(회전 · 맥동 · 떨림 · 깜빡임).
    /// 그림 바꾸기(swaps) · 숫자 세기(counter, 글의 {n}) · 채우기(fill) 도 시간으로. 쥐별 컷신 = Cut/Frame/Scenes/&lt;쥐&gt; 아래 이 컴포넌트를 붙인 오브젝트들.
    /// 투명도는 CanvasGroup 이 있으면 그것, 없으면 Graphic 색 알파. SuperUltManager 가 매 프레임 Apply(t).
    /// </summary>
    public class SuperUltLayer : MonoBehaviour
    {
        public enum Ease { Out, Linear, In, InOut, Back, Snap }

        [System.Serializable]
        public class Key
        {
            [Tooltip("컷신 시간 (초)")] public float t;
            public Vector2 pos;
            public float scale = 1, rot, alpha = 1;
            [Tooltip("앞 키에서 이 키로 갈 때 이징 (Snap = 이 시간에 바로 바뀜)")] public Ease ease = Ease.Out;
        }
        [System.Serializable] public class Swap { public float t; public Sprite sprite; }

        [Tooltip("키 (시간 순서)")] public Key[] keys;
        [Tooltip("계속 회전 (도/초)")] public float spin;
        [Tooltip("맥동: 크기 ± 폭 · 초당 횟수")] public float pulse, pulseHz = 6;
        [Tooltip("떨림 (px)")] public float jitter;
        [Tooltip("깜빡임 (0~1, 무작위로 투명도가 줄어듦 — 번개 · 전기)")] [Range(0, 1)] public float flicker;
        [Tooltip("충격 흔들림 시각들 (카메라 묶음에 주면 화면이 쾅): 각 시각부터 kickTime 동안 kickPower px 에서 줄어듦 · 회전도 kickRot 도")] public float[] kicks;
        public float kickPower = 40, kickRot = 2, kickTime = 0.3f;
        [Tooltip("그림 바꾸기 (시간 순서)")] public Swap[] swaps;
        [Tooltip("그림을 바꿀 때 크기를 그림 픽셀 비율대로 다시 맞춤 (셀 애니 프레임마다 그림 크기가 다를 때, 피벗 = 발끝)")] public bool swapResize;
        [Tooltip("숫자 세기: 이 글의 {n} 을 count.x → count.y 로 (countTime 구간)")] public TMP_Text counter;
        public Vector2 count, countTime;
        [Tooltip("채우기 이미지 fillAmount: fillRange.x → y (countTime 구간)")] public Image fill;
        public Vector2 fillRange = new(0, 1);

        RectTransform rt; CanvasGroup cg; Graphic g; Image img; string fmt; float pxK; Sprite first;

        void Init()
        {
            if (rt) return;
            rt = (RectTransform)transform; cg = GetComponent<CanvasGroup>(); g = GetComponent<Graphic>(); img = g as Image;
            if (counter) fmt = counter.text;
            first = img ? img.sprite : null;
            if (img && img.sprite) pxK = rt.sizeDelta.y / Mathf.Max(1, img.sprite.rect.height);
        }

        public void Apply(float t)
        {
            Init();
            if (keys == null || keys.Length == 0) return;
            Key a = keys[0], b = keys[0]; float k = 0;
            if (t >= keys[keys.Length - 1].t) a = b = keys[keys.Length - 1];
            else if (t > keys[0].t)
                for (int i = 0; i < keys.Length - 1; i++)
                    if (t < keys[i + 1].t) { a = keys[i]; b = keys[i + 1]; k = E(b.ease, (t - a.t) / Mathf.Max(0.0001f, b.t - a.t)); break; }
            var pos = Vector2.LerpUnclamped(a.pos, b.pos, k);
            float sc = Mathf.LerpUnclamped(a.scale, b.scale, k), rot = Mathf.LerpUnclamped(a.rot, b.rot, k), al = Mathf.Clamp01(Mathf.Lerp(a.alpha, b.alpha, Mathf.Clamp01(k)));
            if (pulse != 0) sc *= 1 + pulse * Mathf.Sin(t * pulseHz * Mathf.PI * 2);
            if (jitter > 0 && al > 0) pos += Random.insideUnitCircle * jitter;
            if (flicker > 0 && al > 0) al *= 1 - flicker * Random.value;
            float kick = 0;
            if (kicks != null) foreach (var kt in kicks) if (t >= kt && t < kt + kickTime) kick = Mathf.Max(kick, 1 - (t - kt) / kickTime);
            if (kick > 0) { kick *= kick; pos += Random.insideUnitCircle * kickPower * kick; rot += Random.Range(-1f, 1f) * kickRot * kick; }
            rt.anchoredPosition = pos;
            rt.localScale = Vector3.one * sc;
            rt.localRotation = Quaternion.Euler(0, 0, rot + spin * t);
            if (cg) cg.alpha = al;
            else if (g) { var c = g.color; c.a = al; g.color = c; }
            if (img && swaps != null) { Sprite s = first; foreach (var w in swaps) if (t >= w.t) s = w.sprite; if (s && img.sprite != s) { img.sprite = s; if (swapResize && pxK > 0) rt.sizeDelta = s.rect.size * pxK; } }
            float ck = countTime.y > countTime.x ? Mathf.Clamp01((t - countTime.x) / (countTime.y - countTime.x)) : 1;
            if (counter && fmt != null) counter.text = fmt.Replace("{n}", Mathf.RoundToInt(Mathf.Lerp(count.x, count.y, ck)).ToString());
            if (fill) fill.fillAmount = Mathf.Lerp(fillRange.x, fillRange.y, ck);
        }

        static float E(Ease e, float k)
        {
            k = Mathf.Clamp01(k);
            switch (e)
            {
                case Ease.Linear: return k;
                case Ease.In: return k * k * k;
                case Ease.InOut: return k < 0.5f ? 4 * k * k * k : 1 - Mathf.Pow(-2 * k + 2, 3) / 2;
                case Ease.Back: { const float c1 = 1.9f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(k - 1, 3) + c1 * Mathf.Pow(k - 1, 2); }
                case Ease.Snap: return k >= 1 ? 1 : 0;
                default: return 1 - (1 - k) * (1 - k) * (1 - k);
            }
        }
    }
}
