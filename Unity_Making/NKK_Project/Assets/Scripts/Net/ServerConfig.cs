using UnityEngine;

namespace NKK.Net
{
    // 서버(Firebase) 설정 — Assets/Data/ServerConfig.asset 하나를 씬의 Server 오브젝트(FirebaseClient)에 연결.
    // 웹 API 키 · 프로젝트 ID 는 빌드에 들어가 공개되는 값 (Google Cloud 콘솔에서 키를 itch.io 도메인으로 제한 + Firestore 보안 규칙으로 막음).
    // 키가 비어 있거나 enabled 를 끄면 서버 기능은 조용히 꺼지고 게임은 그대로.
    [CreateAssetMenu(menuName = "NKK/Server Config", fileName = "ServerConfig")]
    public class ServerConfig : ScriptableObject
    {
        [Tooltip("서버 기능 켜기 (끄면 로그인·기록 전송 안 함)")] public bool enabled = true;
        [Tooltip("Firebase 웹 API 키 (프로젝트 설정 → 일반 → 웹 앱의 apiKey)")] public string apiKey = "";
        [Tooltip("Firebase 프로젝트 ID (projectId)")] public string projectId = "";
        [Tooltip("에디터에서도 보내기 (끄면 에디터 Play 는 전송 안 함 — 개발 중 기록 오염 방지)")] public bool sendInEditor;
        [Tooltip("요청 제한 시간 (초)")] public int timeout = 10;

        public bool Ready => enabled && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(projectId) && (sendInEditor || !Application.isEditor);
    }
}
