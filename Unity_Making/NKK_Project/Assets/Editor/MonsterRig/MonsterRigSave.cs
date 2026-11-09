using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NKK.Hazards;
using NKK.Humans;
using NKK.Rats;
using UnityEditor;
using UnityEngine;

// 몬스터 리깅 툴 (MonsterRigTool) 의 '게임에 반영' — 고친 값을 게임이 읽는 곳과, 다시 만들 때 읽는 원본에 모두 씀 (2026-11-06).
// · 파츠 보스: Data/BossPartArt.asset (게임이 읽음) + Data/BossPartArtEdits.asset (NKK/Build Boss Part Art 로 다시 만들어도 이 값이 다시 덮임)
// · 고양이 · 사람: 그림 관절(피벗) = Art/Rats/sprite_pivots.json → 그림 다시 임포트 (.meta) · 몸통 부착점 등 = Data/CatRigMeta·HumanRigMeta.json
//   + Data/CatArtLibrary·HumanArtLibrary.asset + 원본 UnityResources/Rats/{Cats,Humans}/Parts/pivots.json (Tools/gen_sprite_pivots.py 가 읽는 곳)
public static class MonsterRigSave
{
    public const string PartAsset = "Assets/Data/BossPartArt.asset";
    public const string PartEdits = "Assets/Data/BossPartArtEdits.asset";
    public const string CatAsset = "Assets/Data/CatArtLibrary.asset";
    public const string RatAsset = "Assets/Data/RatArtLibrary.asset";
    public const string HumanAsset = "Assets/Data/HumanArtLibrary.asset";
    const string SpritePivots = "Assets/Art/Rats/sprite_pivots.json";
    static string Res => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../UnityResources/Rats"));

    public static T Clone<T>(T src) where T : new()
    {
        var b = new Box<T> { v = new T() };
        EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(new Box<T> { v = src }), b);
        return b.v;
    }
    [Serializable] class Box<T> { public T v; }

    // ── 파츠 보스 ──
    public static void SavePart(BossPartArt.Entry e)
    {
        AssetDatabase.ImportAsset(PartAsset);      // 파일이 밖에서 바뀌었으면 먼저 다시 읽음
        var lib = AssetDatabase.LoadAssetAtPath<BossPartArt>(PartAsset);
        Undo.RecordObject(lib, "리깅 툴 반영");
        Put(lib.entries, Clone(e));
        EditorUtility.SetDirty(lib);
        var ed = AssetDatabase.LoadAssetAtPath<BossPartArt>(PartEdits);
        if (!ed) { ed = ScriptableObject.CreateInstance<BossPartArt>(); AssetDatabase.CreateAsset(ed, PartEdits); }
        Put(ed.entries, Clone(e));
        EditorUtility.SetDirty(ed);
        AssetDatabase.SaveAssetIfDirty(lib); AssetDatabase.SaveAssetIfDirty(ed);      // 이 두 에셋만 (다른 에셋의 옛 메모리 값을 덮어쓰지 않게)
    }
    static void Put(List<BossPartArt.Entry> l, BossPartArt.Entry e)
    {
        int i = l.FindIndex(x => x.codeId == e.codeId);
        if (i >= 0) l[i] = e; else l.Add(e);
    }

    // BossPartArtBuilder 가 에셋을 다시 만든 뒤 부름: 리깅 툴에서 고친 보스는 고친 값으로
    public static int ReapplyPartEdits(BossPartArt lib)
    {
        var ed = AssetDatabase.LoadAssetAtPath<BossPartArt>(PartEdits);
        if (!ed || !lib) return 0;
        int n = 0;
        foreach (var e in ed.entries) { int i = lib.entries.FindIndex(x => x.codeId == e.codeId); if (i < 0) continue; lib.entries[i] = Clone(e); n++; }
        if (n > 0) Debug.Log($"[리깅 툴] 고친 보스 {n}종을 다시 덮음 ({PartEdits})");
        return n;
    }

    // ── 고양이 · 사람 ──
    // pivots: 파츠 이름(head · tail …) → 그림 좌상단 기준 0~1 관절
    public static string SaveCat(RatArtLibrary.Entry e, Dictionary<string, Vector2> pivots) =>
        SaveRatRig(CatAsset, "Cats/Parts/pivots.json", "Assets/Data/CatRigMeta.json", "Assets/Art/Rats/Cats/Parts/", e, pivots);

    // 쥐 원본 pivots.json 은 모양이 다름: {"species": {코드: {"torsoAnchor": …}}}, 들여쓰기 1
    public static string SaveRat(RatArtLibrary.Entry e, Dictionary<string, Vector2> pivots) =>
        SaveRatRig(RatAsset, "Parts/pivots.json", "Assets/Data/RatRigMeta.json", "Assets/Art/Rats/Parts/", e, pivots, "species", "torsoAnchor", 1);

    static string SaveRatRig(string asset, string source, string meta, string artDir, RatArtLibrary.Entry e, Dictionary<string, Vector2> pivots, string table = null, string anchorKey = "anchor", int indent = 2)
    {
        AssetDatabase.ImportAsset(asset);      // 파일이 밖에서 바뀌었으면 먼저 다시 읽음
        var lib = AssetDatabase.LoadAssetAtPath<RatArtLibrary>(asset);
        var dst = lib.entries.Find(x => x.codeId == e.codeId);
        if (dst == null) return Path.GetFileNameWithoutExtension(asset) + " 에 " + e.codeId + " 없음";
        Undo.RecordObject(lib, "리깅 툴 반영");
        dst.neck = e.neck; dst.tailAnchor = e.tailAnchor; dst.shoulder = e.shoulder; dst.hip = e.hip;
        dst.legFront = e.legFront; dst.legBack = e.legBack; dst.headScale = e.headScale;
        dst.wingAnchor = e.wingAnchor; dst.wingSize = e.wingSize; dst.wing2Anchor = e.wing2Anchor; dst.wing2Size = e.wing2Size;
        dst.orders = Clone(new OrderBox { l = e.orders }).l;
        dst.tweaks = Clone(new TweakBox { l = e.tweaks }).l;
        EditorUtility.SetDirty(lib);

        var anchors = new Dictionary<string, Vector2> { ["neck"] = e.neck, ["tail"] = e.tailAnchor, ["shoulder"] = e.shoulder, ["hip"] = e.hip };
        WriteSource(Path.Combine(Res, source), e.codeId, anchors, pivots, m =>
        {
            m["legScale"] = new JObject { ["front"] = R(e.legFront), ["back"] = R(e.legBack) };
            if (e.headScale > 0) m["headScale"] = R(e.headScale); else m.Remove("headScale");
            if (e.wing) { m["wingAnchor"] = A(e.wingAnchor); m["wingSize"] = R(e.wingSize); }
            if (e.wing2) { m["wing2Anchor"] = A(e.wing2Anchor); m["wing2Size"] = R(e.wing2Size); }
            SourceOrder(m, e.orders);
            SourceTweak(m, e.tweaks);
        }, table, anchorKey, indent);
        WriteMeta(meta, e.codeId, o =>
        {
            o["neck"] = A(e.neck); o["tail"] = A(e.tailAnchor); o["shoulder"] = A(e.shoulder); o["hip"] = A(e.hip);
            o["leg_front"] = R(e.legFront); o["leg_back"] = R(e.legBack);
            if (e.headScale > 0) o["head_scale"] = R(e.headScale); else o.Remove("head_scale");
            if (e.headOnly) o["head_only"] = 1;
            if (e.wing) { o["wing_anchor"] = A(e.wingAnchor); o["wing_size"] = R(e.wingSize); }
            if (e.wing2) { o["wing2_anchor"] = A(e.wing2Anchor); o["wing2_size"] = R(e.wing2Size); }
            MetaOrder(o, e.orders);
            MetaTweak(o, e.tweaks);
        });
        var n = WritePivots(artDir + e.codeId + "/", pivots);
        AssetDatabase.SaveAssetIfDirty(lib);      // 이 에셋만
        return $"관절 {n}개";
    }

    public static string SaveHuman(HumanArtLibrary.Entry e, Dictionary<string, Vector2> pivots)
    {
        AssetDatabase.ImportAsset(HumanAsset);      // 파일이 밖에서 바뀌었으면 먼저 다시 읽음
        var lib = AssetDatabase.LoadAssetAtPath<HumanArtLibrary>(HumanAsset);
        var dst = lib.Get(e.codeId);
        if (dst == null) return "HumanArtLibrary 에 " + e.codeId + " 없음";
        Undo.RecordObject(lib, "리깅 툴 반영");
        dst.neck = e.neck; dst.shoulder = e.shoulder; dst.hip = e.hip; dst.front = e.front; dst.headScale = e.headScale;
        dst.orders = Clone(new OrderBox { l = e.orders }).l;
        dst.tweaks = Clone(new TweakBox { l = e.tweaks }).l;
        EditorUtility.SetDirty(lib);

        var anchors = new Dictionary<string, Vector2> { ["neck"] = e.neck, ["shoulder"] = e.shoulder, ["hip"] = e.hip };
        WriteSource(Path.Combine(Res, "Humans/Parts/pivots.json"), e.codeId, anchors, pivots, m =>
        {
            m["headScale"] = R(e.headScale > 0 ? e.headScale : 1);
            if (e.front) m["front"] = 1; else m.Remove("front");
            SourceOrder(m, e.orders);
            SourceTweak(m, e.tweaks);
        });
        WriteMeta("Assets/Data/HumanRigMeta.json", e.codeId, o =>
        {
            o["neck"] = A(e.neck); o["shoulder"] = A(e.shoulder); o["hip"] = A(e.hip);
            o["front"] = e.front ? 1 : 0; o["head_scale"] = R(e.headScale > 0 ? e.headScale : 1);
            MetaOrder(o, e.orders);
            MetaTweak(o, e.tweaks);
        });
        var n = WritePivots("Assets/Art/Rats/Humans/Parts/" + e.codeId + "/", pivots);
        AssetDatabase.SaveAssetIfDirty(lib);      // 이 에셋만
        return $"관절 {n}개";
    }

    [Serializable] class OrderBox { public List<PartOrder> l = new(); }
    // 그리는 순서 덮어쓰기: 원본 pivots.json = {"order": {"head": 8, …}} · *RigMeta.json = "order": [{"part", "order"}] (비면 지움)
    static void SourceOrder(JObject m, List<PartOrder> l)
    {
        if (l == null || l.Count == 0) { m.Remove("order"); return; }
        var o = new JObject(); foreach (var x in l) o[x.part] = x.order; m["order"] = o;
    }
    static void MetaOrder(JObject m, List<PartOrder> l)
    {
        if (l == null || l.Count == 0) { m.Remove("order"); return; }
        var a = new JArray(); foreach (var x in l) a.Add(new JObject { ["part"] = x.part, ["order"] = x.order }); m["order"] = a;
    }

    [Serializable] class TweakBox { public List<PartTweak> l = new(); }
    static bool Plain(PartTweak t) => t.offset.sqrMagnitude < 1e-6f && Mathf.Abs(t.angle) < 1e-4f && Mathf.Abs(t.scale - 1) < 1e-4f;
    // 파츠별 위치 · 회전 · 크기: 원본 pivots.json = {"tweak": {"head": {"offset": [x, y], "angle": a, "scale": s}}} · *RigMeta.json = "tweak": [{part, offset, angle, scale}]
    static void SourceTweak(JObject m, List<PartTweak> l)
    {
        var o = new JObject();
        if (l != null) foreach (var t in l) if (!Plain(t)) o[t.part] = new JObject { ["offset"] = A(t.offset), ["angle"] = R(t.angle), ["scale"] = R(t.scale) };
        if (o.Count == 0) m.Remove("tweak"); else m["tweak"] = o;
    }
    static void MetaTweak(JObject m, List<PartTweak> l)
    {
        var a = new JArray();
        if (l != null) foreach (var t in l) if (!Plain(t)) a.Add(new JObject { ["part"] = t.part, ["offset"] = A(t.offset), ["angle"] = R(t.angle), ["scale"] = R(t.scale) });
        if (a.Count == 0) m.Remove("tweak"); else m["tweak"] = a;
    }

    static JToken R(float v) => Math.Round((double)v, 4);
    static JArray A(Vector2 v) => new JArray(Math.Round((double)v.x, 4), Math.Round((double)v.y, 4));

    // 원본 pivots.json (들여쓰기 2, gen_sprite_pivots.py 가 읽음)
    // table = 종 목록이 들어 있는 키 (쥐 = "species", 고양이 · 사람 = 맨 위). 쥐는 원본에 없는 종을 새로 만들지 않음 (gen_sprite_pivots.py 가 pivotUnity 를 꼭 찾음)
    static void WriteSource(string path, string code, Dictionary<string, Vector2> anchors, Dictionary<string, Vector2> pivots, Action<JObject> more, string table = null, string anchorKey = "anchor", int indent = 2)
    {
        if (!File.Exists(path)) { Debug.LogWarning("[리깅 툴] 원본 없음: " + path); return; }
        var root = Load((path));
        var list = table == null ? root : root[table] as JObject;
        if (list == null) { Debug.LogWarning($"[리깅 툴] 원본에 {table} 없음: {path}"); return; }
        if (list[code] is not JObject m)
        {
            if (table != null) { Debug.LogWarning($"[리깅 툴] 원본에 {code} 없음 (게임 데이터에만 반영): {path}"); return; }
            m = new JObject(); list[code] = m;
        }
        JObject Sub(string k) { if (m[k] is not JObject o) { o = new JObject(); m[k] = o; } return o; }
        foreach (var kv in anchors) { Sub(anchorKey)[kv.Key] = A(kv.Value); Sub(anchorKey + "Unity")[kv.Key] = A(new Vector2(kv.Value.x, 1 - kv.Value.y)); }
        foreach (var kv in pivots) { Sub("pivot")[kv.Key] = A(kv.Value); Sub("pivotUnity")[kv.Key] = A(new Vector2(kv.Value.x, 1 - kv.Value.y)); }
        more(m);
        Write(path, root, indent);
    }

    // Data/*RigMeta.json (들여쓰기 0)
    static void WriteMeta(string path, string code, Action<JObject> set)
    {
        var root = Load((path));
        var items = (JArray)root["items"];
        JObject o = null;
        foreach (var t in items) if ((string)t["code_id"] == code) { o = (JObject)t; break; }
        if (o == null) { o = new JObject { ["code_id"] = code }; items.Add(o); }
        set(o);
        Write(path, root, 0);
        AssetDatabase.ImportAsset(path);
    }

    // sprite_pivots.json (좌하단 기준) → 그 그림만 다시 임포트 (RatSpriteImporter 가 이 피벗을 .meta 에 넣음)
    static int WritePivots(string dir, Dictionary<string, Vector2> pivots)
    {
        if (pivots.Count == 0) return 0;
        var root = Load((SpritePivots));
        var items = (JArray)root["items"];
        var changed = new List<string>();
        foreach (var kv in pivots)
        {
            var p = dir + kv.Key + ".png";
            if (!File.Exists(p)) continue;
            double x = Math.Round((double)kv.Value.x, 4), y = Math.Round(1 - (double)kv.Value.y, 4);
            JObject o = null;
            foreach (var t in items) if ((string)t["path"] == p) { o = (JObject)t; break; }
            if (o == null) { o = new JObject { ["path"] = p }; items.Add(o); }
            if (o["x"] != null && Math.Abs((double)o["x"] - x) < 1e-5 && Math.Abs((double)o["y"] - y) < 1e-5) continue;
            o["x"] = x; o["y"] = y; changed.Add(p);
        }
        if (changed.Count == 0) return 0;
        Write(SpritePivots, root, 0);
        RatSpriteImporter.ResetPivotCache();
        foreach (var p in changed) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        return changed.Count;
    }

    // 원래 숫자 글자 그대로 (double 로 읽으면 끝자리가 바뀌는 수가 있음)
    static JObject Load(string path)
    {
        using var r = new JsonTextReader(new StringReader(File.ReadAllText(path))) { FloatParseHandling = FloatParseHandling.Decimal };
        return JObject.Load(r);
    }

    // 파이썬 json.dump 와 같은 모양으로 (숫자 = 가장 짧게 되돌아오는 자릿수, 줄바꿈 = 원래 파일 그대로) → 고친 줄만 바뀜
    static void Write(string path, JObject root, int indent)
    {
        bool crlf = File.Exists(path) && File.ReadAllText(path).Contains("\r\n");
        var sw = new StringWriter { NewLine = crlf ? "\r\n" : "\n" };
        using (var w = new PyWriter(sw) { Formatting = Formatting.Indented, Indentation = indent, IndentChar = ' ' })
            root.WriteTo(w);
        File.WriteAllText(path, sw.ToString());
    }

    class PyWriter : JsonTextWriter
    {
        public PyWriter(TextWriter t) : base(t) { }
        public override void WriteValue(double v) => WriteRawValue(Py(v));
        public override void WriteValue(float v) => WriteRawValue(Py(v));
        public override void WriteValue(decimal v) => WriteRawValue(v.ToString(System.Globalization.CultureInfo.InvariantCulture));
        static string Py(double v)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            string s = v.ToString("R", ci);
            for (int p = 15; p <= 17; p++) { var t = v.ToString("G" + p, ci); if (double.Parse(t, ci) == v) { s = t; break; } }
            if (s.Contains("E"))
            {
                // 1E-05 → 1e-05 (파이썬)
                var parts = s.Split('E'); int ex = int.Parse(parts[1], ci);
                s = parts[0] + "e" + (ex < 0 ? "-" : "+") + Math.Abs(ex).ToString("00", ci);
            }
            else if (!s.Contains(".")) s += ".0";
            return s;
        }
    }

    // ── 그림 바꾸기 (파츠 PNG 를 다른 파일로 덮음 · 같은 GUID 라 게임 연결 그대로, 관절은 0~1 비율 그대로) ──
    // 원본 폴더(UnityResources)에 같은 그림이 있으면 같이 바꾸고, 바꾸기 전 그림은 UnityResources/Rats/_rigtool_backup/<시각>/ 에 둠
    public static string ReplaceImage(Sprite s, string file)
    {
        var dst = AssetDatabase.GetAssetPath(s);
        if (string.IsNullOrEmpty(dst) || !File.Exists(file)) return "그림 경로 없음";
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var rel = dst.Substring("Assets/Art/Rats/".Length);
        var backup = Path.Combine(Res, "_rigtool_backup", stamp);
        void Back(string p, string name) { var b = Path.Combine(backup, name); Directory.CreateDirectory(Path.GetDirectoryName(b)); File.Copy(p, b, true); }
        Back(dst, "Assets/" + rel);
        File.Copy(file, dst, true);
        string src = SourceOf(rel);
        if (src != null && File.Exists(src)) { Back(src, "UnityResources/" + Path.GetRelativePath(Res, src)); File.Copy(file, src, true); }
        AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
        // 원본 pivots.json 의 그림 크기
        var tex = new Texture2D(2, 2); tex.LoadImage(File.ReadAllBytes(file));
        int w = tex.width, h = tex.height; UnityEngine.Object.DestroyImmediate(tex);
        foreach (var (folder, json) in new[] { ("Cats/Parts/", "Cats/Parts/pivots.json"), ("Humans/Parts/", "Humans/Parts/pivots.json") })
        {
            if (!rel.StartsWith(folder)) continue;
            var parts = rel.Substring(folder.Length).Split('/');
            var jp = Path.Combine(Res, json);
            var root = Load((jp));
            if (root[parts[0]] is JObject m && m["size"] is JObject size) { size[Path.GetFileNameWithoutExtension(parts[1])] = new JArray(w, h); Write(jp, root, 2); }
        }
        return $"{dst} ← {Path.GetFileName(file)} ({w}×{h}) · 예전 그림 = {backup}";
    }

    // Assets/Art/Rats/<rel> 의 원본 (UnityResources) 자리
    static string SourceOf(string rel)
    {
        if (rel.StartsWith("Cats/Parts/") || rel.StartsWith("Humans/Parts/") || rel.StartsWith("Parts/")) return Path.Combine(Res, rel);
        if (rel.StartsWith("BossParts/")) return Path.Combine(Res, "BossRework/Parts", rel.Substring("BossParts/".Length));
        return null;
    }
}
