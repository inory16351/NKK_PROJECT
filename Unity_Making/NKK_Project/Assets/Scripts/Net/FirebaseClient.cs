using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NKK.Net
{
    // Firebase REST (Unity SDK 없이 UnityWebRequest — WebGL · 윈도우 똑같이). 씬마다 Server 오브젝트에 하나, 처음 것만 남음 (DontDestroyOnLoad).
    // ① 익명 로그인: 처음 한 번 계정을 받아 PlayerPrefs(웹 = 브라우저 저장소)에 refreshToken · uid 보관, idToken 은 만료 전에 갱신
    // ② Firestore 쓰기: :commit 으로 문서 만들기(Create) · 덮어쓰기(Set), 시각 칸은 서버 시각 (REQUEST_TIME)
    // 실패해도 게임은 그대로 (콜백 false). 다시 보내기는 부르는 쪽(Telemetry 큐)이 함.
    public class FirebaseClient : MonoBehaviour
    {
        public static FirebaseClient I { get; private set; }

        [Tooltip("서버 설정 (Assets/Data/ServerConfig)")] public ServerConfig config;
        [Tooltip("콘솔에 요청 결과 남기기")] public bool log = true;

        const string PrefUid = "nkk_fb_uid", PrefRefresh = "nkk_fb_refresh";
        string idToken; float idTokenUntil;          // idToken 만료 시각 (realtimeSinceStartup)
        bool signingIn;

        public bool Ready => config && config.Ready;
        public string Uid => PlayerPrefs.GetString(PrefUid, "");
        public bool SignedIn => !string.IsNullOrEmpty(idToken) && Time.realtimeSinceStartup < idTokenUntil;

        void Awake()
        {
            if (I && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        // ── 로그인 ──
        [Serializable] class SignUpRes { public string idToken, refreshToken, expiresIn, localId; }
        [Serializable] class RefreshRes { public string id_token, refresh_token, expires_in, user_id; }

        // idToken 이 있게 만듦 (없으면 새 익명 계정 · 있으면 갱신). 끝나면 done(성공)
        public IEnumerator EnsureAuth(Action<bool> done = null)
        {
            if (!Ready) { done?.Invoke(false); yield break; }
            if (SignedIn) { done?.Invoke(true); yield break; }
            while (signingIn) yield return null;              // 다른 요청이 로그인 중이면 기다림
            if (SignedIn) { done?.Invoke(true); yield break; }
            signingIn = true;
            string refresh = PlayerPrefs.GetString(PrefRefresh, "");
            bool ok = false;
            if (!string.IsNullOrEmpty(refresh))
            {
                var form = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(refresh);
                using var rq = Post("https://securetoken.googleapis.com/v1/token?key=" + config.Key, form, "application/x-www-form-urlencoded");
                yield return rq.SendWebRequest();
                if (rq.result == UnityWebRequest.Result.Success)
                {
                    var r = JsonUtility.FromJson<RefreshRes>(rq.downloadHandler.text);
                    SetToken(r.id_token, r.refresh_token, r.user_id, r.expires_in); ok = true;
                }
                else Log("토큰 갱신 실패 → 새 익명 계정", rq);
            }
            if (!ok)
            {
                using var rq = Post("https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + config.Key, "{\"returnSecureToken\":true}", "application/json");
                yield return rq.SendWebRequest();
                if (rq.result == UnityWebRequest.Result.Success)
                {
                    var r = JsonUtility.FromJson<SignUpRes>(rq.downloadHandler.text);
                    SetToken(r.idToken, r.refreshToken, r.localId, r.expiresIn); ok = true;
                    if (log) Debug.Log($"[Server] 익명 계정 만듦: {r.localId}");
                }
                else Log("익명 로그인 실패", rq);
            }
            signingIn = false;
            done?.Invoke(ok);
        }

        void SetToken(string id, string refresh, string uid, string expiresIn)
        {
            idToken = id;
            int.TryParse(expiresIn, out int sec); if (sec <= 0) sec = 3600;
            idTokenUntil = Time.realtimeSinceStartup + sec - 120;           // 2분 여유
            PlayerPrefs.SetString(PrefRefresh, refresh); PlayerPrefs.SetString(PrefUid, uid); PlayerPrefs.Save();
        }

        // ── Firestore 쓰기 ──
        string DocBase => $"projects/{config.projectId}/databases/(default)/documents";

        // 새 문서 (collection 아래 id, 비우면 무작위). timeField 는 서버 시각으로 채움 (비우면 안 씀)
        // 같은 id 가 이미 있으면 (다시 보낸 것) 성공으로 침 → 기록이 두 번 쌓이지 않음
        public void Create(string collection, Dictionary<string, object> fields, string timeField, Action<bool> done = null, string id = null)
            => StartCoroutine(Commit($"{DocBase}/{collection}/{(string.IsNullOrEmpty(id) ? NewId() : id)}", fields, timeField, true, done));
        public static string NewDocId() => NewId();

        // 문서 덮어쓰기 (path 예: players/{uid})
        public void Set(string path, Dictionary<string, object> fields, string timeField, Action<bool> done = null)
            => StartCoroutine(Commit($"{DocBase}/{path}", fields, timeField, false, done));

        IEnumerator Commit(string name, Dictionary<string, object> fields, string timeField, bool mustBeNew, Action<bool> done)
        {
            bool authed = false;
            yield return EnsureAuth(ok => authed = ok);
            if (!authed) { done?.Invoke(false); yield break; }
            var sb = new StringBuilder("{\"writes\":[{\"update\":{\"name\":").Append(Str(name)).Append(",\"fields\":").Append(Fields(fields)).Append('}');
            if (!string.IsNullOrEmpty(timeField)) sb.Append(",\"updateTransforms\":[{\"fieldPath\":").Append(Str(timeField)).Append(",\"setToServerValue\":\"REQUEST_TIME\"}]");
            if (mustBeNew) sb.Append(",\"currentDocument\":{\"exists\":false}");
            sb.Append("}]}");
            using var rq = Post($"https://firestore.googleapis.com/v1/projects/{config.projectId}/databases/(default)/documents:commit", sb.ToString(), "application/json");
            rq.SetRequestHeader("Authorization", "Bearer " + idToken);
            yield return rq.SendWebRequest();
            bool success = rq.result == UnityWebRequest.Result.Success;
            if (!success && mustBeNew && rq.responseCode == 409) { success = true; if (log) Debug.Log($"[Server] 이미 있음 (다시 보냄): {name}"); }
            else if (!success) Log("쓰기 실패 " + name, rq);
            else if (log) Debug.Log($"[Server] 씀: {name}");
            done?.Invoke(success);
        }

        // ── Firestore 읽기 ──
        // runQuery: structuredQuery JSON (예 {"from":[{"collectionId":"players"}],"orderBy":[…],"limit":100}) → 응답 글 그대로 (실패 = null)
        public void Query(string structuredQuery, Action<string> done)
            => StartCoroutine(RunQuery(structuredQuery, done));

        IEnumerator RunQuery(string q, Action<string> done)
        {
            bool authed = false;
            yield return EnsureAuth(ok => authed = ok);
            if (!authed) { done?.Invoke(null); yield break; }
            using var rq = Post($"https://firestore.googleapis.com/v1/{DocBase}:runQuery", "{\"structuredQuery\":" + q + "}", "application/json");
            rq.SetRequestHeader("Authorization", "Bearer " + idToken);
            yield return rq.SendWebRequest();
            if (rq.result != UnityWebRequest.Result.Success) { Log("조회 실패", rq); done?.Invoke(null); yield break; }
            done?.Invoke(rq.downloadHandler.text);
        }

        // ── 도우미 ──
        public static string Json(string s) => Str(s);
        UnityWebRequest Post(string url, string body, string type)
        {
            var rq = new UnityWebRequest(url, "POST") { uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)), downloadHandler = new DownloadHandlerBuffer(), timeout = config ? config.timeout : 10 };
            rq.SetRequestHeader("Content-Type", type);
            return rq;
        }

        void Log(string what, UnityWebRequest rq)
        {
            if (log) Debug.LogWarning($"[Server] {what}: {rq.responseCode} {rq.error} {rq.downloadHandler?.text}");
        }

        const string IdChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        static string NewId()
        {
            var c = new char[20]; for (int i = 0; i < c.Length; i++) c[i] = IdChars[UnityEngine.Random.Range(0, IdChars.Length)];
            return new string(c);
        }

        // Firestore 값 표기: int·long → integerValue, float·double → doubleValue, bool → booleanValue, 그 밖 → stringValue
        static string Fields(Dictionary<string, object> f)
        {
            var sb = new StringBuilder("{"); bool first = true;
            foreach (var kv in f)
            {
                if (!first) sb.Append(','); first = false;
                sb.Append(Str(kv.Key)).Append(':').Append(Value(kv.Value));
            }
            return sb.Append('}').ToString();
        }
        static string Value(object v) => v switch
        {
            null => "{\"nullValue\":null}",
            bool b => "{\"booleanValue\":" + (b ? "true" : "false") + "}",
            int or long => "{\"integerValue\":\"" + Convert.ToInt64(v).ToString(CultureInfo.InvariantCulture) + "\"}",
            float or double => "{\"doubleValue\":" + Convert.ToDouble(v).ToString("R", CultureInfo.InvariantCulture) + "}",
            _ => "{\"stringValue\":" + Str(v.ToString()) + "}",
        };
        static string Str(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var ch in s)
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4")); else sb.Append(ch); break;
                }
            return sb.Append('"').ToString();
        }
    }
}
