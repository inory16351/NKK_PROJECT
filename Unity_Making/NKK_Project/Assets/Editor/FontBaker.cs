using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Assets/Fonts 의 TTF → TMP SDF 폰트 에셋 (정적 아틀라스, 글자 = charset_ko.txt). 메뉴 NKK/Bake Fonts
// 주의: Bake Fonts 는 에셋을 지우고 새로 만듦 → 씬·프리팹의 폰트 연결이 끊김. 폰트 하나만 추가할 땐 Bake Font (하나) 메뉴 사용
public static class FontBaker
{
    const string Dir = "Assets/Fonts/";
    static readonly string[] Fonts = { "GowunDodum-Regular", "IBMPlexSansKR-Medium", "IBMPlexSansKR-Bold", "Jua-Regular" };

    // 게임 UI 제목·팻말·팝업용 둥근 폰트 (Jua, OFL)
    [MenuItem("NKK/Bake Font (Jua)")]
    public static void BakeJua() { Bake("Jua-Regular", File.ReadAllText(Dir + "charset_ko.txt")); AssetDatabase.SaveAssets(); }

    [MenuItem("NKK/Bake Fonts")]
    public static void BakeAll()
    {
        string chars = File.ReadAllText(Dir + "charset_ko.txt");
        foreach (var name in Fonts) Bake(name, chars);
        AssetDatabase.SaveAssets();
    }

    static void Bake(string name, string chars)
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(Dir + name + ".ttf");
        if (!font) { Debug.LogWarning("[FontBaker] 폰트 없음: " + name); return; }
        string path = Dir + name + " SDF.asset";
        AssetDatabase.DeleteAsset(path);
        // 샘플 크기 48 · 여백 6 · 4096 아틀라스 1장 (한글 2,500자 정도 들어감)
        var fa = TMP_FontAsset.CreateFontAsset(font, 48, 6, GlyphRenderMode.SDFAA, 4096, 4096, AtlasPopulationMode.Dynamic, true);
        fa.name = name + " SDF";
        fa.TryAddCharacters(chars, out string missing);
        fa.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(fa, path);
        foreach (var tex in fa.atlasTextures) { if (!tex) continue; tex.name = name + " Atlas"; AssetDatabase.AddObjectToAsset(tex, fa); }
        if (fa.material) { fa.material.name = name + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
        EditorUtility.SetDirty(fa);
        Debug.Log($"[FontBaker] {name}: {fa.characterTable.Count}자, 아틀라스 {fa.atlasTextures.Length}장, 빠진 글자 {(string.IsNullOrEmpty(missing) ? 0 : missing.Length)}");
    }
}
