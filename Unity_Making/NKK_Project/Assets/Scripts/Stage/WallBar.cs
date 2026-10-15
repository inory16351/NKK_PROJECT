using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Stage
{
    // 벽 체력바 (웹 drawWallBar): 크림색 판 + 막대 (+ "남은 체력 → 잠긴 방" 글자, 2026-10-15 사용자: 숫자는 안 보이게 = 물건·고양이처럼 막대만).
    // 월드 캔버스(1 픽셀 = 1 게임 단위) 안 템플릿 하나를 StageManager 가 벽마다 복제해 씀. 글자 틀은 인스펙터 format.
    public class WallBar : MonoBehaviour
    {
        [Tooltip("막대 (Image Type = Filled, Horizontal)")] public Image fill;
        public TMP_Text label;
        [Tooltip("글자 틀. {0} = 남은 체력 (예: 인스펙터에서 '{0} → 잠긴 방')")] public string format = "{0}";
        [Tooltip("막대 그림 양끝 둥근 테두리 비율 (ui_hp 그림이면 FxManager 의 hpBarRim 과 같게)")] public float rim;
        [Tooltip("남은 체력 숫자 글 보이기 (끄면 막대만)")] public bool showLabel;
        [Tooltip("물건·고양이 체력바처럼 남은 비율에 따라 초록·노랑·빨강 (FxManager hpGreen·hpYellow·hpRed). 끄면 빨간 막대 그대로 (사용자 2026-10-15: 벽은 빨강으로 구별)")] public bool hpColors;

        float lastHp = -1, lastK = -1;

        public void Set(float hp, float k)
        {
            hp = Mathf.Max(0, hp);
            if (fill && !Mathf.Approximately(k, lastK))
            {
                fill.fillAmount = k > 0 ? rim + (1 - 2 * rim) * k : 0; lastK = k;
                var fx = FxManager.I;
                if (hpColors && fx && fx.hpGreen) fill.sprite = k > 0.5f ? fx.hpGreen : k > 0.25f ? fx.hpYellow : fx.hpRed;
            }
            if (label && label.gameObject.activeSelf != showLabel) label.gameObject.SetActive(showLabel);
            if (showLabel && label && !Mathf.Approximately(hp, lastHp)) { label.text = string.Format(format, GameManager.Format(hp)); lastHp = hp; }
        }
    }
}
