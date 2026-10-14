using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// 가로·세로가 4의 배수가 아닌 Art 스프라이트는 DXT 압축이 안 돼 무압축(RGBA32)으로 빌드에 들어감 (WebGL 용량 폭증).
// 그런 스프라이트만 폴더별 스프라이트 아틀라스(Assets/Art/_WebAtlas)로 묶어 아틀라스 통째로 압축 — 원본 PNG·크기·피벗은 그대로.
// 그림을 새로 넣거나 지우면 자동으로 다시 맞춤 (메뉴 NKK/Rebuild Web Atlases 로 직접 돌려도 됨).
public class WebAtlasBuilder : AssetPostprocessor
{
    const string ArtRoot = "Assets/Art/";
    const string AtlasDir = "Assets/Art/_WebAtlas";
    const string Ext = ".spriteatlasv2";
    const int Padding = 4;

    static bool queued;

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (queued) return;
        if (!imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(IsArtPng)) return;
        queued = true;
        EditorApplication.delayCall += () => { queued = false; Sync(false); };
    }

    static bool IsArtPng(string p) => p.StartsWith(ArtRoot) && !p.StartsWith(AtlasDir) && p.EndsWith(".png");

    [MenuItem("NKK/Rebuild Web Atlases")]
    static void RebuildMenu() => Sync(true);

    // 이미 만든 아틀라스의 설정을 기본값으로 다시 맞춤 (인스펙터에서 바꾼 값도 덮어씀)
    [MenuItem("NKK/Reset Web Atlas Settings")]
    static void ResetSettingsMenu()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:SpriteAtlas", new[] { AtlasDir }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(p);
            Configure(p, atlas.GetPackables().Select(AssetDatabase.GetAssetPath).ToList());
        }
    }

    static void Sync(bool log)
    {
        if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

        // 폴더 → 4의 배수가 아닌 스프라이트 경로들
        var groups = new SortedDictionary<string, List<string>>();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!IsArtPng(path)) continue;
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti) || ti.textureType != TextureImporterType.Sprite) continue;
            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            if (w % 4 == 0 && h % 4 == 0) continue;
            var key = Path.GetDirectoryName(path).Replace('\\', '/').Substring(ArtRoot.Length).Replace('/', '_');
            if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<string>();
            list.Add(path);
        }

        if (!AssetDatabase.IsValidFolder(AtlasDir)) AssetDatabase.CreateFolder("Assets/Art", "_WebAtlas");

        int created = 0, updated = 0, removed = 0;
        var keep = new HashSet<string>();
        foreach (var kv in groups)
        {
            var atlasPath = AtlasDir + "/" + kv.Key + Ext;
            keep.Add(atlasPath);
            var paths = kv.Value.OrderBy(p => p).ToList();
            var sprites = paths.Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p)).Where(s => s).Cast<Object>().ToArray();

            var existing = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (existing)
            {
                var now = existing.GetPackables().Select(AssetDatabase.GetAssetPath).OrderBy(p => p);
                if (now.SequenceEqual(paths)) continue;
                var asset = SpriteAtlasAsset.Load(atlasPath);
                asset.Remove(existing.GetPackables());
                asset.Add(sprites);
                SpriteAtlasAsset.Save(asset, atlasPath);
                AssetDatabase.ImportAsset(atlasPath);
                updated++;
            }
            else
            {
                var asset = new SpriteAtlasAsset();
                asset.Add(sprites);
                SpriteAtlasAsset.Save(asset, atlasPath);
                AssetDatabase.ImportAsset(atlasPath);
                Configure(atlasPath, paths);
                created++;
            }
        }

        foreach (var guid in AssetDatabase.FindAssets("t:SpriteAtlas", new[] { AtlasDir }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (!keep.Contains(p)) { AssetDatabase.DeleteAsset(p); removed++; }
        }

        if (log || created + updated + removed > 0)
            Debug.Log($"[WebAtlas] 아틀라스 {groups.Count}개 (새로 {created} · 고침 {updated} · 지움 {removed})");
    }

    // 새 아틀라스 기본 설정 (만든 뒤 인스펙터에서 바꾼 값은 유지)
    static void Configure(string atlasPath, List<string> paths)
    {
        int biggest = 0;
        foreach (var p in paths)
        {
            ((TextureImporter)AssetImporter.GetAtPath(p)).GetSourceTextureWidthAndHeight(out int w, out int h);
            biggest = Mathf.Max(biggest, w, h);
        }

        var imp = (SpriteAtlasImporter)AssetImporter.GetAtPath(atlasPath);
        imp.includeInBuild = true;
        imp.packingSettings = new SpriteAtlasPackingSettings
        {
            enableRotation = false, enableTightPacking = false, enableAlphaDilation = true,
            padding = Padding, blockOffset = 1,
        };
        imp.textureSettings = new SpriteAtlasTextureSettings
        {
            readable = false, generateMipMaps = false, sRGB = true, filterMode = FilterMode.Bilinear,
        };
        int max = biggest + Padding * 2 > 2048 ? 4096 : 2048;
        // 기본값만 바꾸면 WebGL·윈도우는 옛 최대 크기(2048)로 남아 큰 그림이 안 들어감 → 플랫폼별로도 같은 값
        foreach (var platform in new[] { "DefaultTexturePlatform", "WebGL", "Standalone" })
        {
            var ps = imp.GetPlatformSettings(platform);
            ps.overridden = platform != "DefaultTexturePlatform";
            ps.maxTextureSize = max;
            ps.textureCompression = TextureImporterCompression.CompressedHQ;
            ps.format = TextureImporterFormat.Automatic;
            imp.SetPlatformSettings(ps);
        }
        imp.SaveAndReimport();
    }
}
