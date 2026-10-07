using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Assets/Art 아래 PNG 를 전부 Sprite 로 임포트. (Art/Stage 바닥·벽 타일은 PPU 200 = 선명하게)
// 파츠(쥐·고양이·사람·필살기 앞모습)는 sprite_pivots.json 의 관절 피벗을 적용 (Tools/gen_sprite_pivots.py 가 생성).
public class RatSpriteImporter : AssetPostprocessor
{
    const string ArtRoot = "Assets/Art/";
    const string PivotFile = "Assets/Art/Rats/sprite_pivots.json";
    public const float PixelsPerUnit = 100f;
    const string StageRoot = "Assets/Art/Stage/";
    const float StagePixelsPerUnit = 200f;

    [System.Serializable] class PivotItem { public string path; public float x, y; }
    [System.Serializable] class PivotList { public List<PivotItem> items; }

    static Dictionary<string, Vector2> pivots;

    static Dictionary<string, Vector2> Pivots
    {
        get
        {
            if (pivots != null) return pivots;
            pivots = new Dictionary<string, Vector2>();
            if (File.Exists(PivotFile))
                foreach (var it in JsonUtility.FromJson<PivotList>(File.ReadAllText(PivotFile)).items)
                    pivots[it.path] = new Vector2(it.x, it.y);
            return pivots;
        }
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtRoot) || !assetPath.EndsWith(".png")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = assetPath.StartsWith(StageRoot) ? StagePixelsPerUnit : PixelsPerUnit;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        if (Pivots.TryGetValue(assetPath, out var p))
        {
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = p;
        }
        else
        {
            s.spriteAlignment = (int)SpriteAlignment.Center;
        }
        ti.SetTextureSettings(s);
    }

    [MenuItem("NKK/Reimport Art Sprites")]
    static void ReimportAll()
    {
        pivots = null;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
    }
}
