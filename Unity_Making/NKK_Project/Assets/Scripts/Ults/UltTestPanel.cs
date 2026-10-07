using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Ults
{
    // 테스트용 (게임 화면 왼쪽 위): ◀ 필살기 고르기 ▶ · 필살기 발동 · 슈퍼 점프 발동. 출시 때는 이 오브젝트를 끄면 됨
    public class UltTestPanel : MonoBehaviour
    {
        public UltimateManager Ults;
        public SuperJumpManager SuperJump;
        public Button prev, next, ultButton, superJumpButton;
        [Tooltip("고른 필살기 (자리: {name} 쥐 이름, {ult} 필살기 이름)")] public TMP_Text pickText;
        [Tooltip("아이콘 (선택)")] public Image pickIcon;

        readonly List<RatCharacterRow> list = new();
        int idx;
        string fmt;

        void Start()
        {
            var db = GameDatabase.Instance;
            foreach (var r in db.Rats.Values) if (db.UltOf(r) != null) list.Add(r);
            list.Sort((a, b) => a.ultimate.CompareTo(b.ultimate));
            if (prev) prev.onClick.AddListener(() => Move(-1));
            if (next) next.onClick.AddListener(() => Move(1));
            if (ultButton) ultButton.onClick.AddListener(() => { if (list.Count > 0) Ults.TestUlt(list[idx].code_id); });
            if (superJumpButton) superJumpButton.onClick.AddListener(() => SuperJump.Trigger(true));
            Show();
        }

        void Move(int d) { if (list.Count == 0) return; idx = (idx + d + list.Count) % list.Count; Show(); }

        void Show()
        {
            if (list.Count == 0 || !pickText) return;
            fmt ??= pickText.text;
            var r = list[idx]; var u = GameDatabase.Instance.UltOf(r);
            pickText.text = fmt.Replace("{name}", r.character_name).Replace("{ult}", u.ultimate_name);
            if (pickIcon && Ults)
            {
                string n = u.ult_icon.Substring(u.ult_icon.LastIndexOf('/') + 1);
                foreach (var i in Ults.icons) if (i.name == n) { pickIcon.sprite = i.sprite; break; }
            }
        }
    }
}
