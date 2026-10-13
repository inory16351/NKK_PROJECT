using System;
using NKK.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 닉네임 창 (랭킹에 처음 들어갈 때 · 낮잠 침대 · 랭킹 페이지의 '닉네임 바꾸기').
    // 글자 수 · 금지어 검사는 Leaderboard. 안내·오류 글은 씬 TMP (errLength · errBanned 를 켜고 끔).
    public class NickPopup : MonoBehaviour
    {
        public static NickPopup I { get; private set; }

        [Tooltip("창 전체 (꺼져 있음)")] public GameObject root;
        public TMP_InputField input;
        public Button okButton, cancelButton;
        [Tooltip("오류: 글자 수")] public GameObject errLength;
        [Tooltip("오류: 쓸 수 없는 말")] public GameObject errBanned;
        [Tooltip("오류: 서버 없음 (설정 꺼짐 등)")] public GameObject errOffline;

        Action<bool> onDone;
        public bool IsOpen => root && root.activeSelf;

        void Awake()
        {
            I = this;
            if (root) root.SetActive(false);
            if (okButton) okButton.onClick.AddListener(Ok);
            if (cancelButton) cancelButton.onClick.AddListener(() => Close(false));
            if (input) { input.onSubmit.AddListener(_ => Ok()); input.onValueChanged.AddListener(_ => ShowError(Leaderboard.NickError.None)); }
        }

        // 창 열기 — done(true) = 닉네임 정함
        public void Open(Action<bool> done = null)
        {
            onDone = done;
            if (!root) { done?.Invoke(false); return; }
            root.SetActive(true);
            var lb = Leaderboard.I;
            if (input) { if (lb) input.characterLimit = lb.nickMax; input.text = Leaderboard.Nickname; input.ActivateInputField(); }
            ShowError(Leaderboard.NickError.None);
            if (errOffline) errOffline.SetActive(!lb);
        }

        void ShowError(Leaderboard.NickError e)
        {
            if (errLength) errLength.SetActive(e == Leaderboard.NickError.Length);
            if (errBanned) errBanned.SetActive(e == Leaderboard.NickError.Banned);
        }

        void Ok()
        {
            var lb = Leaderboard.I; if (!lb || !input) return;
            var e = lb.Check(input.text);
            if (e != Leaderboard.NickError.None) { ShowError(e); return; }
            lb.SetNickname(input.text);
            Close(true);
        }

        public void Close(bool ok)
        {
            if (root) root.SetActive(false);
            var d = onDone; onDone = null; d?.Invoke(ok);
        }
    }
}
