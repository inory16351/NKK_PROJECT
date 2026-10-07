using UnityEditor;
using UnityEngine;

// UI 키트(Assets/Art/UI_Kit) 9-슬라이스 테두리 넣기. 메뉴 NKK/Apply UI Kit Borders
// 테두리 = 그림 크기 비율 (모서리·못·끈 구멍이 늘어나지 않게). 노드·압정·점·둥근 버튼은 안 늘림
public static class UiKitBorders
{
    const string Dir = "Assets/Art/UI_Kit/";

    [MenuItem("NKK/Apply UI Kit Borders")]
    public static void Apply()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid), name = System.IO.Path.GetFileNameWithoutExtension(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!ti || !tex) continue;
            float w = tex.width, h = tex.height, m = Mathf.Min(w, h);
            // (왼쪽, 아래, 오른쪽, 위) 픽셀
            Vector4 b = name switch
            {
                var n when n.StartsWith("ui_plank") => new Vector4(h * 0.6f, h * 0.4f, h * 0.6f, h * 0.4f),
                var n when n.StartsWith("ui_bar") || n == "ui_pill" => new Vector4(h * 0.5f, h * 0.45f, h * 0.5f, h * 0.45f),
                var n when n.StartsWith("ui_tile") => new Vector4(w * 0.3f, h * 0.35f, w * 0.3f, h * 0.3f),
                "ui_card" => new Vector4(m * 0.18f, m * 0.2f, m * 0.18f, m * 0.18f),
                "ui_cork" => new Vector4(m * 0.15f, m * 0.15f, m * 0.15f, m * 0.15f),
                "ui_sign" => new Vector4(m * 0.18f, m * 0.18f, m * 0.18f, h * 0.4f),
                "ui_tape" => new Vector4(h * 0.5f, h * 0.3f, h * 0.5f, h * 0.3f),
                "ui_topbar" => new Vector4(h, h * 0.4f, h, h * 0.4f),
                "ui_bubble" => new Vector4(m * 0.32f, m * 0.32f, m * 0.32f, m * 0.32f),
                "ui_ribbon" => new Vector4(h * 0.3f, h * 0.3f, h * 0.6f, h * 0.3f),
                _ => Vector4.zero,
            };
            b = new Vector4(Mathf.Round(b.x), Mathf.Round(b.y), Mathf.Round(b.z), Mathf.Round(b.w));
            if (ti.spriteBorder == b) continue;
            ti.spriteBorder = b;
            ti.SaveAndReimport();
            Debug.Log($"[UiKitBorders] {name}: {b}");
        }
    }
}
