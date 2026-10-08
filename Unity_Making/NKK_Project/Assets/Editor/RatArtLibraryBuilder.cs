using System.IO;
using NKK.Data;
using NKK.Rats;
using UnityEditor;
using UnityEngine;

// Assets/Art/Rats/Parts/<종>/*.png + Assets/Data/RatRigMeta.json → Assets/Data/RatArtLibrary.asset
public static class RatArtLibraryBuilder
{
    const string AssetPath = "Assets/Data/RatArtLibrary.asset";

    [MenuItem("NKK/Build Cat Art Library")]
    public static void BuildCats() => BuildFrom("Assets/Data/CatRigMeta.json", "Assets/Art/Rats/Cats/Parts/", "Assets/Data/CatArtLibrary.asset", false);

    [MenuItem("NKK/Build Rat Art Library")]
    public static void Build() => BuildFrom("Assets/Data/RatRigMeta.json", "Assets/Art/Rats/Parts/", AssetPath, true);

    static void BuildFrom(string metaPath, string partsDir, string assetPath, bool rats)
    {
        var lib = AssetDatabase.LoadAssetAtPath<RatArtLibrary>(assetPath);
        if (!lib) { lib = ScriptableObject.CreateInstance<RatArtLibrary>(); AssetDatabase.CreateAsset(lib, assetPath); }
        lib.entries.Clear();
        var meta = JsonUtility.FromJson<RatRigMetaFile>(File.ReadAllText(metaPath));
        Sprite S(string p) => AssetDatabase.LoadAssetAtPath<Sprite>(p);
        Vector2 V(float[] a) => a != null && a.Length >= 2 ? new Vector2(a[0], a[1]) : Vector2.zero;
        foreach (var m in meta.items)
        {
            var d = $"{partsDir}{m.code_id}/";
            if (!S(d + "torso.png")) continue;       // 안 쓰는 종(이전 시도)은 건너뜀
            lib.entries.Add(new RatArtLibrary.Entry
            {
                codeId = m.code_id, head = S(d + "head.png"), torso = S(d + "torso.png"), tail = S(d + "tail.png"),
                front = S(d + "front.png"), back = S(d + "back.png"),
                neck = V(m.neck), tailAnchor = V(m.tail), shoulder = V(m.shoulder), hip = V(m.hip),
                legFront = m.leg_front, legBack = m.leg_back,
            });
        }
        // 파츠 없는 한 장짜리
        var pc = rats ? S("Assets/Art/Rats/Parody/rat_pcmouse.png") : null;
        if (pc) lib.entries.Add(new RatArtLibrary.Entry { codeId = "pcmouse", single = pc });
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[{assetPath}] {lib.entries.Count}종");
    }
}

// Assets/Art/Rats/Humans/Parts/<사람>/*.png + Assets/Data/HumanRigMeta.json → Assets/Data/HumanArtLibrary.asset
public static class HumanArtLibraryBuilder
{
    const string AssetPath = "Assets/Data/HumanArtLibrary.asset";

    [MenuItem("NKK/Build Human Art Library")]
    public static void Build()
    {
        var lib = AssetDatabase.LoadAssetAtPath<NKK.Humans.HumanArtLibrary>(AssetPath);
        if (!lib) { lib = ScriptableObject.CreateInstance<NKK.Humans.HumanArtLibrary>(); AssetDatabase.CreateAsset(lib, AssetPath); }
        lib.entries.Clear();
        var meta = JsonUtility.FromJson<HumanRigMetaFile>(File.ReadAllText("Assets/Data/HumanRigMeta.json"));
        Sprite S(string p) => AssetDatabase.LoadAssetAtPath<Sprite>(p);
        Vector2 V(float[] a) => a != null && a.Length >= 2 ? new Vector2(a[0], a[1]) : Vector2.zero;
        foreach (var m in meta.items)
        {
            var d = $"Assets/Art/Rats/Humans/Parts/{m.code_id}/";
            if (!S(d + "torso.png")) continue;
            lib.entries.Add(new NKK.Humans.HumanArtLibrary.Entry
            {
                codeId = m.code_id, head = S(d + "head.png"), scared = S(d + "scared.png"), angry = S(d + "angry.png"), torso = S(d + "torso.png"),
                arm = S(d + "arm.png"), leg = S(d + "leg.png"), neck = V(m.neck), shoulder = V(m.shoulder), hip = V(m.hip), front = m.front == 1, headScale = m.head_scale > 0 ? m.head_scale : 1,
            });
        }
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[HumanArtLibrary] {lib.entries.Count}명");
    }
}
