using System.IO;
using NKK.Data;
using NKK.Rats;
using UnityEditor;
using UnityEngine;

// Assets/Art/Rats/Parts/<종>/*.png + Assets/Data/RatRigMeta.json → Assets/Data/RatArtLibrary.asset
public static class RatArtLibraryBuilder
{
    const string AssetPath = "Assets/Data/RatArtLibrary.asset";
    // 5G 휴대폰 그림(rat_fiveg.png)의 화면 칸 (Tools 에서 잘라 낼 때 잰 값, UnityResources/Rats/NewRats3/README.md)
    static readonly Rect ScreenRectFiveG = new(0.0721f, 0.1123f, 0.8559f, 0.7536f);

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
        // 파츠 없는 한 장짜리 (Parody/rat_<종>.png): 컴퓨터 마우스 · 쥐한폭탄 · 노릇노릇 쥐포 · 맘스땃쥐(햄버거 층 쌓기)
        string[] singles = { "pcmouse", "bombrat", "jwipo", "burger" };
        foreach (var m in meta.items)
        {
            var d = $"{partsDir}{m.code_id}/";
            if (!S(d + "torso.png")) continue;       // 안 쓰는 종(이전 시도)은 건너뜀
            if (rats && System.Array.IndexOf(singles, m.code_id) >= 0 && S($"Assets/Art/Rats/Parody/rat_{m.code_id}.png")) continue;     // 한 장짜리로 바꾼 종 (옛 파츠는 안 씀)
            lib.entries.Add(new RatArtLibrary.Entry
            {
                codeId = m.code_id, head = S(d + "head.png"), torso = S(d + "torso.png"), tail = S(d + "tail.png"),
                front = S(d + "front.png"), back = S(d + "back.png"),
                neck = V(m.neck), tailAnchor = V(m.tail), shoulder = V(m.shoulder), hip = V(m.hip),
                legFront = m.leg_front, legBack = m.leg_back,
            });
        }
        foreach (var id in singles)
        {
            var sp = rats ? S($"Assets/Art/Rats/Parody/rat_{id}.png") : null;
            if (sp) lib.entries.Add(new RatArtLibrary.Entry { codeId = id, single = sp });
        }
        // 화면 속 쥐 (2026-10-12 사용자: 5G = 휴대폰 화면 속 쥐): 겉은 휴대폰 한 장짜리 Parody/rat_<종>.png 가 컴퓨터 마우스처럼 움직이고,
        // 파츠 쥐(Parts/<종>)는 화면 칸 안에서 걷고 행동함. 화면 칸 = 휴대폰 그림에서 비어 있던(자홍) 자리, 좌하단 기준 0~1
        if (rats)
            foreach (var (id, rect) in new[] { ("fiveg", ScreenRectFiveG) })
            {
                var en = lib.entries.Find(x => x.codeId == id);
                var phone = S($"Assets/Art/Rats/Parody/rat_{id}.png");
                if (en == null || !phone) continue;
                en.single = phone; en.screen = true; en.screenRect = rect;
                en.screenBg = S($"Assets/Art/Rats/Parody/rat_{id}_screen.png");
            }
        // 그림이 아직 없는 새 쥐: 다른 쥐 그림을 잠깐 빌려 씀 (로직 시험용, 진짜 파츠가 생기면 위에서 먼저 잡힘) — 2026-10-12 아이스 퀸 쥐 · 5G
        if (rats)
            foreach (var (id, from) in new[] { ("icequeen", "angel"), ("fiveg", "student") })
            {
                if (lib.entries.Exists(e => e.codeId == id)) continue;
                var src = lib.entries.Find(e => e.codeId == from);
                if (src == null) continue;
                lib.entries.Add(new RatArtLibrary.Entry
                {
                    codeId = id, head = src.head, torso = src.torso, tail = src.tail, front = src.front, back = src.back, single = src.single,
                    neck = src.neck, tailAnchor = src.tailAnchor, shoulder = src.shoulder, hip = src.hip, legFront = src.legFront, legBack = src.legBack,
                });
                Debug.Log($"[RatArtLibrary] {id}: 그림 없음 → {from} 그림 임시로 씀");
            }
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
