using NKK.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 낮잠 침대: 익명 통계 보내기 켜기/끄기 (Telemetry.Enabled) · 닉네임 보기/바꾸기 (NickPopup)
    // 닉네임 글은 씬 TMP 의 자리표시 {name}, 닉네임이 없으면 noNick 오브젝트를 켬
    public class ServerSettings : MonoBehaviour
    {
        public Toggle statsToggle;
        [Tooltip("닉네임 글 (자리표시 {name})")] public TMP_Text nickText;
        [Tooltip("닉네임 없음 글")] public GameObject noNick;
        public Button nickButton;

        string nickTpl;

        void Awake()
        {
            if (statsToggle) statsToggle.onValueChanged.AddListener(v => Telemetry.Enabled = v);
            if (nickButton) nickButton.onClick.AddListener(() => { if (NickPopup.I) NickPopup.I.Open(_ => Show()); });
        }

        void OnEnable() => Show();

        void Show()
        {
            if (statsToggle) statsToggle.SetIsOnWithoutNotify(Telemetry.Enabled);
            if (nickText)
            {
                nickTpl ??= LocText.Tpl(nickText, nickText.text);
                nickText.gameObject.SetActive(Leaderboard.HasNickname);
                nickText.text = nickTpl.Replace("{name}", LeaderboardPage.Safe(Leaderboard.Nickname));
            }
            if (noNick) noNick.SetActive(!Leaderboard.HasNickname);
        }
    }
}
