using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 증강 카드 한 장 (씬 템플릿을 복제해서 씀: 고르기 3장 · 슬롯 · 보관함 · 도감). 글 틀은 씬 TMP 글 그대로, 자리표시만 채움
    public class AugmentCard : MonoBehaviour
    {
        public Button button;
        public Image icon;
        [Tooltip("등급 색으로 칠하는 테두리/배경 (AugmentArt 등급 그림이 있으면 그 그림)")] public Image frame;
        public TMP_Text nameText, descText;
        [Tooltip("등급 이름")] public TMP_Text gradeText;
        [Tooltip("강화 레벨 (자리: {lv}, 0 이면 숨김)")] public TMP_Text levelText;
        [Tooltip("강화 포인트 (자리: {pts})")] public TMP_Text pointsText;
        [Tooltip("골랐을 때 켜짐")] public GameObject selectMark;
        [Tooltip("새로 얻음 표시")] public GameObject newMark;
        [Tooltip("장착 중 / 들고 가기 표시")] public GameObject equipMark;
        [Tooltip("못 얻은 증강일 때 이름")] public string unknownName = "???";
        [Tooltip("unknownName 스트링 키")] public int unknownNameKey;

        string lvTpl, ptsTpl; bool init;
        public int Uid { get; private set; }
        public AugmentRow Row { get; private set; }

        void Init()
        {
            if (init) return; init = true;
            lvTpl = levelText ? LocText.Tpl(levelText, levelText.text) : "{lv}";
            ptsTpl = pointsText ? LocText.Tpl(pointsText, pointsText.text) : "{pts}";
        }

        // revealed = false 면 ??? (도감)
        public void Bind(AugmentRow r, int lv, int uid = 0, int pts = -1, bool revealed = true)
        {
            Init();
            Row = r; Uid = uid;
            var g = r != null ? Augments.Grade(r.grade) : null;
            var art = AugmentArt.I;
            if (icon) { var s = !revealed ? (art ? art.unknown : null) : art && r != null ? art.Icon(r.icon) : null; icon.sprite = s; icon.enabled = s; icon.preserveAspect = true; }
            if (frame)
            {
                var fs = art && r != null ? art.Frame(r.grade) : null;
                if (fs) { frame.sprite = fs; frame.color = Color.white; }
                else frame.color = g != null ? g.Color : Color.white;
            }
            if (nameText) nameText.text = revealed && r != null ? r.aug_name : Loc.Get(unknownNameKey, unknownName);
            if (descText) descText.text = revealed && r != null ? Augments.Desc(r, lv) : "";
            if (gradeText) { gradeText.text = g != null ? g.grade_name : ""; if (g != null) gradeText.color = g.Color; }
            if (levelText) { levelText.gameObject.SetActive(lv > 0); levelText.text = (lvTpl ?? "").Replace("{lv}", lv.ToString()); }
            if (pointsText) { pointsText.gameObject.SetActive(pts >= 0); pointsText.text = (ptsTpl ?? "").Replace("{pts}", Mathf.Max(0, pts).ToString()); }
            SetSelected(false); SetNew(false); SetEquipped(false);
        }
        public void BindItem(Progress.AugItem it) => Bind(Augments.RowOf(it), it != null ? it.lv : 0, it != null ? it.uid : 0, it != null ? it.pts : -1);

        public void SetSelected(bool on) { if (selectMark) selectMark.SetActive(on); }
        public void SetNew(bool on) { if (newMark) newMark.SetActive(on); }
        public void SetEquipped(bool on) { if (equipMark) equipMark.SetActive(on); }
    }
}
