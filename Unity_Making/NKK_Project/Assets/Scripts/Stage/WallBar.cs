using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Stage
{
    // 벽 체력바 (웹 drawWallBar): 크림색 판 + 막대 + "남은 체력 → 잠긴 방" 글자.
    // 월드 캔버스(1 픽셀 = 1 게임 단위) 안 템플릿 하나를 StageManager 가 벽마다 복제해 씀. 글자 틀은 인스펙터 format.
    public class WallBar : MonoBehaviour
    {
        [Tooltip("막대 (Image Type = Filled, Horizontal)")] public Image fill;
        public TMP_Text label;
        [Tooltip("글자 틀. {0} = 남은 체력 (예: 인스펙터에서 '{0} → 잠긴 방')")] public string format = "{0}";
        [Tooltip("막대 그림 양끝 둥근 테두리 비율 (ui_hp 그림이면 FxManager 의 hpBarRim 과 같게)")] public float rim;

        float lastHp = -1, lastK = -1;

        public void Set(float hp, float k)
        {
            hp = Mathf.Max(0, hp);
            if (fill && !Mathf.Approximately(k, lastK)) { fill.fillAmount = k > 0 ? rim + (1 - 2 * rim) * k : 0; lastK = k; }
            if (label && !Mathf.Approximately(hp, lastHp)) { label.text = string.Format(format, GameManager.Format(hp)); lastHp = hp; }
        }
    }
}
