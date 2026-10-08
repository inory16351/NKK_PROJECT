using UnityEditor;
using UnityEditor.SceneManagement;

namespace NKK.EditorTools
{
    // 다른 PC 에서 클론해 처음 열면 (마지막 씬 기록은 커밋 안 하는 Library 에 있음) 빈 씬·SampleScene 이 열림
    // → 에디터를 켤 때 빈 씬이거나 SampleScene 이면 로비 씬을 연다 (에디터 세션마다 한 번)
    [InitializeOnLoad]
    static class OpenLobbyOnStart
    {
        const string Done = "NKK.OpenLobbyOnStart.done";
        const string Lobby = "Assets/Scenes/Lobby.unity";

        static OpenLobbyOnStart() { EditorApplication.delayCall += Check; }

        static void Check()
        {
            if (SessionState.GetBool(Done, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(Done, true);
            var s = EditorSceneManager.GetActiveScene();
            bool empty = string.IsNullOrEmpty(s.path) && !s.isDirty;
            if ((empty || s.path.EndsWith("SampleScene.unity")) && System.IO.File.Exists(Lobby)) EditorSceneManager.OpenScene(Lobby);
        }
    }
}
