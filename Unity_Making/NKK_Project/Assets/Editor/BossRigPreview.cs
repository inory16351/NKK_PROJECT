using System.Collections.Generic;
using System.IO;
using NKK;
using NKK.Data;
using NKK.Hazards;
using NKK.Humans;
using NKK.Rats;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 보스 리그 조립 미리보기 (보스 개편 2026-10-22): Game 씬의 보스 리그를 복제해 자세 4개(대기 · 걸음 두 위상 · 공격)를 PNG 로 찍음.
// 쓰는 법: BossRigPreview.Shoot("C:/.../out", "boss_popcat,boss_spider") — 코드 비우면 개편 보스 전부. 씬은 바꾸지 않음 (복제는 지움).
public static class BossRigPreview
{
    const string GamePath = "Assets/Scenes/Game.unity";
    static readonly Vector3 Origin = new(9000, 9000, 0);

    public static string Shoot(string outDir, string codes = "", int size = 512)
    {
        Directory.CreateDirectory(outDir);
        var s = EditorSceneManager.GetSceneByPath(GamePath);
        bool opened = false;
        if (!s.isLoaded) { s = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Additive); opened = true; }
        Boss boss = null; CatManager cats = null; NKK.Items.ItemManager items = null;
        foreach (var root in s.GetRootGameObjects())
        {
            if (!boss) boss = root.GetComponentInChildren<Boss>(true);
            if (!cats) cats = root.GetComponentInChildren<CatManager>(true);
            if (!items) items = root.GetComponentInChildren<NKK.Items.ItemManager>(true);
        }
        var partArt = AssetDatabase.LoadAssetAtPath<BossPartArt>("Assets/Data/BossPartArt.asset");
        var rows = new List<BossRow>();
        var json = File.ReadAllText("Assets/Data/BossTable.json");
        foreach (var r in JsonUtility.FromJson<Wrap>(json).Boss)
            if (r.boss_id >= 95024 && (string.IsNullOrEmpty(codes) || ("," + codes + ",").Contains("," + r.code_id + ","))) rows.Add(r);

        var log = new List<string>();
        var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        var camGo = new GameObject("__PreviewCam"); var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.27f, 0.47f, 0.27f, 1);
        cam.targetTexture = rt; cam.enabled = false;
        foreach (var row in rows)
        {
            GameObject go = null;
            try
            {
                Bounds b; var tex = new List<Texture2D>();
                string[] poses = { "idle", "walkA", "walkB", "alt" };
                for (int i = 0; i < poses.Length; i++)
                {
                    if (go) Object.DestroyImmediate(go);
                    go = Make(row, boss, cats, items, partArt, poses[i]);
                    if (!go) { log.Add(row.code_id + ": 그림 없음"); break; }
                    go.transform.position = Origin;
                    b = Bounds(go);
                    float half = Mathf.Max(b.extents.x, b.extents.y) * 1.12f;
                    cam.orthographicSize = half; cam.transform.position = new Vector3(b.center.x, b.center.y, -50);
                    cam.Render();
                    RenderTexture.active = rt;
                    var t = new Texture2D(size, size, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, size, size), 0, 0); t.Apply();
                    RenderTexture.active = null;
                    tex.Add(t);
                }
                if (tex.Count == poses.Length)
                {
                    var sheet = new Texture2D(size * tex.Count, size, TextureFormat.RGBA32, false);
                    for (int i = 0; i < tex.Count; i++) sheet.SetPixels(i * size, 0, size, size, tex[i].GetPixels());
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(outDir, row.code_id + ".png"), sheet.EncodeToPNG());
                    Object.DestroyImmediate(sheet);
                    log.Add(row.code_id + ": ok");
                }
                foreach (var t in tex) Object.DestroyImmediate(t);
            }
            catch (System.Exception ex) { log.Add(row.code_id + ": " + ex.Message); }
            if (go) Object.DestroyImmediate(go);
        }
        Object.DestroyImmediate(camGo); rt.Release(); Object.DestroyImmediate(rt);
        if (opened) EditorSceneManager.CloseScene(s, true);
        return string.Join("\n", log);
    }

    [System.Serializable] class Wrap { public List<BossRow> Boss; }

    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<SpriteRenderer>();
        var b = new Bounds(go.transform.position, Vector3.zero); bool first = true;
        foreach (var r in rs) { if (!r.enabled || !r.sprite) continue; if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
        return b;
    }

    // 보스 한 마리 조립 (Boss.Spawn 과 같은 방식) + 자세
    static GameObject Make(BossRow row, Boss boss, CatManager cats, NKK.Items.ItemManager items, BossPartArt partArt, string pose)
    {
        bool alt = pose == "alt"; float walk = pose == "walkA" ? 0 : pose == "walkB" ? Mathf.PI * 0.5f : 0;
        bool moving = pose.StartsWith("walk");
        if (row.IsCat)
        {
            var art = cats.catArt.Get(row.code_id); if (art == null) return null;
            var go = Object.Instantiate(boss.catRig.gameObject); go.SetActive(true);
            var rr = go.GetComponent<RatRig>();
            rr.Build(art, cats.catLength * row.scale, -1, false);
            var p = new RatRig.Pose { tail = 0.2f, sx = 1, sy = 1 };
            if (moving) { float sn = pose == "walkA" ? 1 : -1; p.front = sn * 0.5f; p.farBack = sn * 0.45f; p.farFront = -sn * 0.5f; p.back = -sn * 0.45f; }
            if (alt) { p.front = 2.1f; p.farFront = 0.5f; p.tilt = -0.3f; p.head = -0.2f; p.tail = 1.1f; rr.headReplace = art.head2; }
            rr.Apply(p, 1, -1, 1, 0);
            return go;
        }
        if (row.IsPart)
        {
            var art = partArt.Get(row.code_id); if (art == null) return null;
            var go = new GameObject("__Part", typeof(UnityEngine.Rendering.SortingGroup), typeof(BossPartRig));
            var pr = go.GetComponent<BossPartRig>();
            pr.Build(art, row.scale); pr.still = !alt;      // 대기·걸음 = 시간 움직임 끔 (같은 그림), 공격 = 사격 깜빡 포함
            pr.Apply(moving ? (pose == "walkA" ? Mathf.PI * 0.5f : -Mathf.PI * 0.5f) : 0, moving, alt, 1, -1, 0, 1, 0);
            return go;
        }
        {
            var art = items.humanArt.Get(row.code_id); if (art == null) return null;
            var go = Object.Instantiate(boss.rig.gameObject); go.SetActive(true);
            var hr = go.GetComponent<HumanRig>();
            hr.height = items.humanHeight * row.scale;
            hr.Build(art);
            var p = new HumanRig.Pose { sx = 1, sy = 1, armN = 0.25f, armF = -0.2f };
            if (moving) { float sn = pose == "walkA" ? 1 : -1; p.legN = sn * 0.45f; p.legF = -p.legN; p.armN = -sn * 0.35f; p.armF = -p.armN; }
            if (alt) { p.angry = true; p.armN = 1.2f; p.armF = -1.2f; }
            hr.Apply(p, 1, -1, 0, 1, 0, 1);
            return go;
        }
    }
}
