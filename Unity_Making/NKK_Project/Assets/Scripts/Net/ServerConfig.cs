using UnityEngine;

namespace NKK.Net
{
    // 서버(Firebase) 설정 — Assets/Data/ServerConfig.asset 하나를 씬의 Server 오브젝트(FirebaseClient)에 연결.
    // 웹 API 키 · 프로젝트 ID 는 빌드에 들어가 공개되는 값 (Google Cloud 콘솔에서 출시용 키 = itch.io 도메인, 개발용 키 = 사무실 공인 IP 로 제한 + Firestore 보안 규칙으로 막음).
    // 키가 비어 있거나 enabled 를 끄면 서버 기능은 조용히 꺼지고 게임은 그대로.
    [CreateAssetMenu(menuName = "NKK/Server Config", fileName = "ServerConfig")]
    public class ServerConfig : ScriptableObject
    {
        [Tooltip("서버 기능 켜기 (끄면 로그인·기록 전송 안 함)")] public bool enabled = true;
        [Tooltip("출시용 웹 API 키 (itch.io 에서 연 WebGL 에서 씀) — Google Cloud 콘솔에서 itch.io 주소(HTTP 리퍼러)로만 쓸 수 있게 제한")] public string apiKey = "";
        [Tooltip("개발용 API 키 (에디터 · 윈도우 빌드 · itch.io 밖에서 연 WebGL(사무실 localhost 등) 에서 씀) — 사무실 공인 IP + API 종류로 제한. 비우면 apiKey")] public string devApiKey = "";
        [Tooltip("Firebase 프로젝트 ID (projectId)")] public string projectId = "";
        [Tooltip("접속 국가 알려 주는 Cloudflare Worker 주소 (Cloudflare/nation-worker.js) — 비우면 기록에 nation 칸 없음")] public string nationUrl = "";
        [Tooltip("에디터에서도 보내기 (끄면 에디터 Play 는 전송 안 함 — 개발 중 기록 오염 방지)")] public bool sendInEditor;
        [Tooltip("요청 제한 시간 (초)")] public int timeout = 10;

        // 지금 쓸 키: itch.io 에서 연 WebGL = 출시용, 그 밖(에디터·윈도우·사무실에서 연 WebGL) = 개발용 — 빌드 하나로 둘 다 됨
        public string Key => IsItchPage || string.IsNullOrEmpty(devApiKey) ? apiKey : devApiKey;

        static bool IsItchPage
        {
            get
            {
                if (Application.platform != RuntimePlatform.WebGLPlayer) return false;
                string url = Application.absoluteURL ?? "";
                return url.Contains("itch.zone") || url.Contains("itch.io");
            }
        }

        public bool Ready => enabled && !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(projectId) && (sendInEditor || !Application.isEditor);
    }
}
