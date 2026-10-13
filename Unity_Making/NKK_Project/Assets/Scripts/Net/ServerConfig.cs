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
        [Tooltip("출시용 웹 API 키 (WebGL 빌드에서 씀) — Google Cloud 콘솔에서 itch.io 주소(HTTP 리퍼러)로만 쓸 수 있게 제한")] public string apiKey = "";
        [Tooltip("개발용 API 키 (에디터 · 윈도우 빌드에서 씀, 주소 정보가 없어 리퍼러 제한 키를 못 씀) — API 종류만 제한. 비우면 apiKey")] public string devApiKey = "";
        [Tooltip("Firebase 프로젝트 ID (projectId)")] public string projectId = "";
        [Tooltip("에디터에서도 보내기 (끄면 에디터 Play 는 전송 안 함 — 개발 중 기록 오염 방지)")] public bool sendInEditor;
        [Tooltip("요청 제한 시간 (초)")] public int timeout = 10;

        // 지금 쓸 키: WebGL = 출시용, 그 밖(에디터·윈도우) = 개발용
        public string Key => Application.platform == RuntimePlatform.WebGLPlayer || string.IsNullOrEmpty(devApiKey) ? apiKey : devApiKey;

        public bool Ready => enabled && !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(projectId) && (sendInEditor || !Application.isEditor);
    }
}
