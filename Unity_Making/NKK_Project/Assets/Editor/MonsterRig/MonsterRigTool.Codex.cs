using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NKK.Hazards;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// 몬스터 리깅 툴 — Codex 로 파츠 그림 고치기 (2026-11-06 사용자).
// 고른 파츠 그림을 1024 칸 가운데에 자홍(#FF00FF) 바탕으로 놓아 Codex(codex exec + 그림 생성)에 보내고, 받은 그림의 자홍을 빼서 원래 크기로 되돌림.
// 원래 그림과 같은 칸 · 같은 크기라 관절(피벗) 비율 그대로 끼워짐 → '미리보기에 끼워 보기' 로 확인 → '적용' = 그림 파일 바꾸기 (예전 그림 백업, MonsterRigSave.ReplaceImage).
// 작업 폴더 = UnityResources/Rats/_codex_edit/<code>_<파츠>_<시각>/ (ref.png · prompt.txt · log.txt · out.png · result.png)
public partial class MonsterRigTool
{
    class CodexJob
    {
        public Process proc; public StringBuilder log = new();
        public string dir, label; public Sprite target; public double start;
        public int S = 1024; public RectInt inner;     // ref.png 안에서 원래 그림이 놓인 칸 (좌하단 기준 픽셀)
        public bool running, failed; public string msg;
        public Texture2D result; public string resultPath;
    }
    CodexJob cjob;
    string codexAsk = "";
    bool codexOpen = true, codexTry = true, codexAlt;
    Vector2 codexScroll;
    static string ResRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../UnityResources/Rats"));

    // 고른 파츠의 지금 그림 (원래 스프라이트)
    Sprite CodexTarget(out string label)
    {
        label = sel;
        if (cur == null || string.IsNullOrEmpty(sel)) return null;
        if (cur.kind == Kind.Part)
        {
            var p = work.part.parts.Find(x => x.name == sel); if (p == null) return null;
            return codexAlt && p.alt ? p.alt : p.sprite;
        }
        var r = RigRenderer(sel); if (!r || !r.sprite) return null;
        return tempSprites.TryGetValue(r.sprite, out var src) ? src.src : r.sprite;
    }

    void DrawCodex()
    {
        if (cur == null) return;
        EditorGUILayout.Space(8);
        codexOpen = EditorGUILayout.Foldout(codexOpen, "Codex 로 그림 고치기", true, EditorStyles.foldoutHeader);
        if (!codexOpen) return;
        var s = CodexTarget(out var label);
        if (cur.kind == Kind.Part) { var p = work.part.parts.Find(x => x.name == sel); if (p != null && p.alt) codexAlt = EditorGUILayout.ToggleLeft("두 번째 그림 (alt) 고치기", codexAlt); }
        if (!s) { EditorGUILayout.HelpBox("미리보기나 목록에서 파츠를 하나 고르세요.", MessageType.None); return; }
        EditorGUILayout.LabelField($"고칠 그림: {label} · {s.name} ({(int)s.rect.width}×{(int)s.rect.height})", EditorStyles.miniLabel);

        if (cjob != null && cjob.running)
        {
            EditorGUILayout.HelpBox($"Codex 가 그리는 중… {EditorApplication.timeSinceStartup - cjob.start:0}초 ({cjob.label})", MessageType.Info);
            if (GUILayout.Button("멈추기")) { try { cjob.proc?.Kill(); } catch { } cjob.running = false; cjob.failed = true; cjob.msg = "멈춤"; }
            return;
        }
        EditorGUILayout.LabelField("어떻게 고칠지 (한국어로 써도 돼요)", EditorStyles.miniLabel);
        codexScroll = EditorGUILayout.BeginScrollView(codexScroll, GUILayout.Height(54));
        codexAsk = EditorGUILayout.TextArea(codexAsk, EditorStyles.textArea, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        GUI.enabled = codexAsk.Trim().Length > 0;
        if (GUILayout.Button(cjob != null && cjob.result ? "Codex 에 다시 보내기" : "Codex 에 보내기", GUILayout.Height(24))) StartCodex(s, label, codexAsk.Trim());
        GUI.enabled = true;

        if (cjob == null) return;
        if (cjob.failed) { EditorGUILayout.HelpBox("Codex 실패: " + cjob.msg + "\n작업 폴더: " + cjob.dir, MessageType.Warning); if (GUILayout.Button("작업 폴더 열기")) EditorUtility.RevealInFinder(cjob.dir); return; }
        if (!cjob.result) return;
        // 전 · 후
        EditorGUILayout.BeginHorizontal();
        var r = GUILayoutUtility.GetRect(140, 140, GUILayout.ExpandWidth(true));
        var half = new Rect(r.x, r.y, r.width / 2 - 2, r.height);
        EditorGUI.DrawRect(half, new Color(0.3f, 0.34f, 0.3f)); EditorGUI.DrawRect(new Rect(half.xMax + 4, r.y, half.width, r.height), new Color(0.3f, 0.34f, 0.3f));
        var orig = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(cjob.target));
        if (orig) GUI.DrawTexture(half, orig, ScaleMode.ScaleToFit);
        GUI.DrawTexture(new Rect(half.xMax + 4, r.y, half.width, r.height), cjob.result, ScaleMode.ScaleToFit);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("왼쪽 = 지금 그림 · 오른쪽 = Codex 그림", EditorStyles.centeredGreyMiniLabel);
        EditorGUI.BeginChangeCheck();
        codexTry = EditorGUILayout.ToggleLeft("미리보기에 끼워 보기 (반영 전)", codexTry);
        if (EditorGUI.EndChangeCheck()) Rebuild();
        EditorGUILayout.BeginHorizontal();
        var bg = GUI.backgroundColor; GUI.backgroundColor = new Color(0.5f, 1f, 0.55f);
        if (GUILayout.Button("이 그림으로 바꾸기 (게임에 반영)")) ApplyCodex();
        GUI.backgroundColor = bg;
        if (GUILayout.Button("버리기", GUILayout.Width(60))) { cjob = null; Rebuild(); }
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("작업 폴더 열기", EditorStyles.miniButton)) EditorUtility.RevealInFinder(cjob.dir);
    }

    // ── 보내기 ──
    void StartCodex(Sprite s, string label, string ask)
    {
        var exe = CodexExe();
        if (exe == null) { cjob = new CodexJob { failed = true, msg = "codex.exe 를 찾지 못함 (Codex 데스크톱 앱 설치 필요: %LOCALAPPDATA%/OpenAI/Codex/bin)", dir = "" }; return; }
        var src = AssetDatabase.GetAssetPath(s);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(src));
        var job = new CodexJob { target = s, label = $"{cur.code} · {label}", start = EditorApplication.timeSinceStartup, running = true };
        job.dir = Path.Combine(ResRoot, "_codex_edit", $"{cur.code}_{label}_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(job.dir);
        // ref.png: 1024 칸 가운데에 80% 크기로, 바탕 자홍
        int S = job.S; float k = Mathf.Min(0.8f * S / tex.width, 0.8f * S / tex.height);
        int w = Mathf.RoundToInt(tex.width * k), h = Mathf.RoundToInt(tex.height * k);
        job.inner = new RectInt((S - w) / 2, (S - h) / 2, w, h);
        var refTex = new Texture2D(S, S, TextureFormat.RGB24, false);
        var px = new Color32[S * S]; var mag = new Color32(255, 0, 255, 255);
        for (int i = 0; i < px.Length; i++) px[i] = mag;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = tex.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h);
                var o = Color.Lerp(new Color(1, 0, 1), new Color(c.r, c.g, c.b), c.a);
                px[(job.inner.y + y) * S + job.inner.x + x] = o;
            }
        refTex.SetPixels32(px); refTex.Apply();
        File.WriteAllBytes(Path.Combine(job.dir, "ref.png"), refTex.EncodeToPNG());
        DestroyImmediate(refTex); DestroyImmediate(tex);

        string who = cur.boss ? $"the boss \"{cur.name}\" ({cur.code})" : $"\"{cur.code}\"";
        string kind = cur.kind == Kind.Part ? "custom part-rig boss" : cur.kind == Kind.Cat ? "cat" : "human";
        var prompt = new StringBuilder();
        prompt.AppendLine("Use your built-in image generation tool to EDIT the attached image ref.png and save the result as PNG to: out.png (in the current working directory). Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.");
        prompt.AppendLine();
        prompt.AppendLine($"ref.png ({S}x{S}) shows ONE body-part sprite (\"{label}\") of {who}, a {kind} character in a 2D cutout-rigged game. The flat magenta (#FF00FF) area is empty background, not part of the drawing.");
        prompt.AppendLine();
        prompt.AppendLine("CHANGE REQUESTED BY THE ARTIST (may be Korean):");
        prompt.AppendLine(ask);
        prompt.AppendLine();
        prompt.AppendLine("RULES:");
        prompt.AppendLine($"- Output exactly {S}x{S}. Keep the part in EXACTLY the same position, size, angle and footprint as in ref.png (unless the change asks otherwise), so it still fits the rig joints 1:1 - the joint/attachment end must not move.");
        prompt.AppendLine("- Same art style as ref.png: Untitled Goose Game flat look, completely flat color shapes, NO black outlines, no linework, no gradients, no gloss, no texture noise; keep the same palette unless the change asks for new colors.");
        prompt.AppendLine("- Background: one single flat pure magenta #FF00FF everywhere outside the part. No shadow, no ground, no frame, no other objects, no text.");
        File.WriteAllText(Path.Combine(job.dir, "prompt.txt"), prompt.ToString(), new UTF8Encoding(false));

        try
        {
            var psi = new ProcessStartInfo(exe, $"exec --skip-git-repo-check --ephemeral -s workspace-write -C \"{job.dir}\" -i \"{Path.Combine(job.dir, "ref.png")}\"")
            { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = job.dir, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            p.OutputDataReceived += (_, e) => { if (e.Data != null) lock (job.log) job.log.AppendLine(e.Data); };
            p.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (job.log) job.log.AppendLine(e.Data); };
            p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine();
            var bytes = new UTF8Encoding(false).GetBytes(prompt.ToString());
            p.StandardInput.BaseStream.Write(bytes, 0, bytes.Length); p.StandardInput.BaseStream.Flush(); p.StandardInput.Close();
            job.proc = p;
        }
        catch (Exception ex) { job.running = false; job.failed = true; job.msg = ex.Message; }
        cjob = job;
        Debug.Log($"[리깅 툴] Codex 에 보냄: {job.label} · {job.dir}");
    }

    static string CodexExe()
    {
        var bin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenAI", "Codex", "bin");
        if (!Directory.Exists(bin)) return null;
        return Directory.GetFiles(bin, "codex.exe", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTime).FirstOrDefault();
    }

    // 끝났는지 (EditorApplication.update)
    void CodexTick()
    {
        var j = cjob; if (j == null || !j.running || j.proc == null) return;
        if (EditorApplication.timeSinceStartup - j.start > 900) { try { j.proc.Kill(); } catch { } j.running = false; j.failed = true; j.msg = "15분이 넘어서 멈춤"; Repaint(); return; }
        if (!j.proc.HasExited) { if ((int)(EditorApplication.timeSinceStartup * 2) % 2 == 0) Repaint(); return; }
        j.running = false;
        lock (j.log) File.WriteAllText(Path.Combine(j.dir, "log.txt"), j.log.ToString(), new UTF8Encoding(false));
        var outPng = Path.Combine(j.dir, "out.png");
        if (!File.Exists(outPng)) { j.failed = true; j.msg = $"out.png 가 없음 (끝 코드 {j.proc.ExitCode}, log.txt 확인)"; Repaint(); return; }
        try { FinishCodex(j, outPng); }
        catch (Exception ex) { j.failed = true; j.msg = ex.Message; }
        Debug.Log($"[리깅 툴] Codex 끝: {j.label} {(j.failed ? "실패 " + j.msg : "→ " + j.resultPath)}");
        Rebuild(); Repaint();
    }

    // 받은 그림: ref 칸 자리만 잘라 원래 크기로 + 자홍 빼기 (가장자리 섞인 자홍도 덜어 냄)
    void FinishCodex(CodexJob j, string outPng)
    {
        var o = new Texture2D(2, 2, TextureFormat.RGBA32, false); o.LoadImage(File.ReadAllBytes(outPng));
        var src = AssetDatabase.GetAssetPath(j.target);
        var orig = new Texture2D(2, 2, TextureFormat.RGBA32, false); orig.LoadImage(File.ReadAllBytes(src));
        int W = orig.width, H = orig.height; DestroyImmediate(orig);
        var res = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var px = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (j.inner.x + (x + 0.5f) / W * j.inner.width) / j.S, v = (j.inner.y + (y + 0.5f) / H * j.inner.height) / j.S;
                var c = o.GetPixelBilinear(u, v);
                float m = Mathf.Min(c.r, c.b) - c.g;                        // 자홍다움 (바탕 ≈ 1)
                float e = Mathf.Clamp01((m - 0.35f) / 0.4f), a = 1 - e * e * (3 - 2 * e);      // smoothstep (Mathf.SmoothStep 은 보간이라 안 씀)
                if (a < 0.02f) { px[y * W + x] = new Color(0, 0, 0, 0); continue; }
                var col = new Color((c.r - (1 - a)) / a, c.g / a, (c.b - (1 - a)) / a);      // 자홍 섞임 덜기
                px[y * W + x] = new Color(Mathf.Clamp01(col.r), Mathf.Clamp01(col.g), Mathf.Clamp01(col.b), a);
            }
        res.SetPixels(px); res.Apply();
        DestroyImmediate(o);
        j.resultPath = Path.Combine(j.dir, "result.png");
        File.WriteAllBytes(j.resultPath, res.EncodeToPNG());
        res.hideFlags = HideFlags.HideAndDontSave;
        if (j.result) DestroyImmediate(j.result);
        j.result = res;
    }

    // 미리보기에 끼워 보기: 원래 그림 대신 Codex 그림 (같은 크기 · 같은 관절 비율)
    Sprite CodexSwap(Sprite s, Vector2 pivotNorm)
    {
        if (!codexTry || cjob == null || !cjob.result || s != cjob.target) return null;
        var t = Sprite.Create(cjob.result, new Rect(0, 0, cjob.result.width, cjob.result.height), pivotNorm, s.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        t.name = s.name + "_codex"; t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    void ApplyCodex()
    {
        var j = cjob; if (j == null || !j.result || !File.Exists(j.resultPath)) return;
        if (!EditorUtility.DisplayDialog("몬스터 리깅", $"{AssetDatabase.GetAssetPath(j.target)}\n그림을 Codex 그림으로 바꿀까요? (게임 그림이 바로 바뀌고, 예전 그림은 UnityResources/Rats/_rigtool_backup 에 남아요)", "바꾸기", "취소")) return;
        status = MonsterRigSave.ReplaceImage(j.target, j.resultPath);
        Debug.Log("[리깅 툴] Codex 그림 적용: " + status);
        cjob = null;
        Rebuild(); Repaint();
    }
}
