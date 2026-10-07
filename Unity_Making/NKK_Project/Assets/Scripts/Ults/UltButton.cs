using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Ults
{
    // 하단 필살기 버튼 하나 (템플릿을 복제해서 씀). 글자는 씬의 TMP 에 쓰여 있고 코드는 {name} 자리만 채움
    public class UltButton : MonoBehaviour
    {
        public Button button;
        public Image icon;
        [Tooltip("다 찼을 때 반짝이는 테두리")] public Image glow;
        [Tooltip("버튼 뒤에서 빙글 도는 햇살")] public RectTransform burst;
        public float burstSpin = 40;
        [Tooltip("종 이름 (글 안 {name} 자리)")] public TMP_Text nameText;
        [Tooltip("다른 필살기가 끝나기를 기다리는 중 표시 (씬에서 글 넣기)")] public GameObject waitMark;
        [Tooltip("자동 사용 표시")] public GameObject autoMark;

        [HideInInspector] public string code;
        string nameFormat;
        float born;

        public void Setup(string code, Sprite spr, string ratName, System.Action<string> onClick)
        {
            this.code = code;
            if (icon) { icon.sprite = spr; icon.enabled = spr; }
            if (nameText) { nameFormat ??= nameText.text; nameText.text = nameFormat.Replace("{name}", ratName); }
            if (button) { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => onClick(code)); }
            born = Time.unscaledTime;
        }

        public void Refresh(bool waiting, bool auto)
        {
            if (waitMark && waitMark.activeSelf != waiting) waitMark.SetActive(waiting);
            if (autoMark && autoMark.activeSelf != auto) autoMark.SetActive(auto);
            float t = Time.unscaledTime;
            if (glow) { var c = glow.color; c.a = 0.55f + 0.45f * Mathf.Sin(t * 6); glow.color = c; }
            if (burst) { burst.localRotation = Quaternion.Euler(0, 0, -t * burstSpin); burst.localScale = Vector3.one * (1 + 0.06f * Mathf.Sin(t * 4)); }
            // 나타날 때 통! · 기다릴 땐 작게
            float age = t - born, pop = age < 0.25f ? 1 + 0.35f * Mathf.Sin(age / 0.25f * Mathf.PI) : 1;
            transform.localScale = Vector3.one * pop * (waiting ? 0.9f : 1 + 0.04f * Mathf.Sin(t * 5));
        }
    }
}
