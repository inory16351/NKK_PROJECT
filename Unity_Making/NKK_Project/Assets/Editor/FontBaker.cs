using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Assets/Fonts 의 TTF → TMP SDF 폰트 에셋 (정적 아틀라스, 글자 = charset_ko.txt). 메뉴 NKK/Bake Fonts
// 주의: Bake Fonts · Bake Font (Jua) 는 에셋을 지우고 새로 만듦 → 씬·프리팹의 폰트 연결이 끊김.
// charset_ko.txt 에 글자를 추가했을 땐 "Add Missing Chars (연결 유지)" 메뉴로 기존 에셋에 덧붙일 것
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

    // charset_ko.txt 에 있는데 구운 폰트에 없는 글자만 기존 에셋에 덧붙임 (에셋을 지우지 않아서 씬·머티리얼 연결이 그대로)
    // 폰트 파일에 아예 없는 글자는 건너뜀 (로그에 표시)
    [MenuItem("NKK/Add Missing Chars (연결 유지)")]
    public static void AddMissingAll()
    {
        string chars = File.ReadAllText(Dir + "charset_ko.txt");
        foreach (var name in Fonts) AddMissing(name, chars);
        AssetDatabase.SaveAssets();
    }

    static void AddMissing(string name, string chars)
    {
        string path = Dir + name + " SDF.asset";
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (!fa) { Debug.LogWarning("[FontBaker] 에셋 없음: " + path); return; }
        var need = new System.Text.StringBuilder();
        foreach (var c in chars) if (!char.IsWhiteSpace(c) && !fa.HasCharacter(c, false, false)) need.Append(c);
        if (need.Length == 0) { Debug.Log($"[FontBaker] {name}: 빠진 글자 없음"); return; }
        int before = fa.characterTable.Count, atlases = fa.atlasTextures.Length;
        fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fa.TryAddCharacters(need.ToString(), out string missing);
        fa.atlasPopulationMode = AtlasPopulationMode.Static;
        // 아틀라스가 꽉 차서 새 장이 생겼으면 에셋 안에 넣음
        for (int i = atlases; i < fa.atlasTextures.Length; i++) { var tex = fa.atlasTextures[i]; if (!tex) continue; tex.name = name + " Atlas " + i; AssetDatabase.AddObjectToAsset(tex, fa); }
        EditorUtility.SetDirty(fa);
        Debug.Log($"[FontBaker] {name}: {fa.characterTable.Count - before}자 추가 (아틀라스 {fa.atlasTextures.Length}장), 폰트에 없는 글자 {(string.IsNullOrEmpty(missing) ? "없음" : missing)}");
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
