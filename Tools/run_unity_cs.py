"""긴 C# (MCP execute_code 5만 자 한도 넘음)을 Unity 에서 실행: 파일 경로만 넘기면 에디터 안에서 읽어 CSharpCodeProvider 로 컴파일해 실행 (2026-10-21 외계인 생쥐 v3).
사용: python Tools/run_unity_cs.py <코드.cs>   — 코드는 execute_code 와 같은 '메서드 본문' (UnityEngine · UnityEditor 바로 씀, return 값 출력).
Unity MCP (http://127.0.0.1:8080/mcp) 에 직접 JSON-RPC 로 붙음 (세션 MCP 가 끊겨 있어도 됨). Play 중이면 실행하지 않음."""
import json, os, sys, urllib.request

URL = 'http://127.0.0.1:8080/mcp'
LOADER = r'''
if (EditorApplication.isPlaying) return "PLAYING - stop";
var body = System.IO.File.ReadAllText(@"%PATH%");
var src = "using UnityEngine; using UnityEditor; public static class LongRun { public static object Run() { " + body + "\n } }";
var want = new[] { "UnityEngine.CoreModule", "UnityEngine.UIModule", "UnityEngine.UI", "UnityEngine.TextRenderingModule", "UnityEngine.ImageConversionModule", "UnityEngine.IMGUIModule",
                   "Unity.TextMeshPro", "UnityEditor.CoreModule", "UnityEditor", "UnityEngine", "Assembly-CSharp", "netstandard", "System", "System.Core" };
var prm = new System.CodeDom.Compiler.CompilerParameters { GenerateInMemory = true, GenerateExecutable = false };
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
{
  if (a.IsDynamic || string.IsNullOrEmpty(a.Location)) continue;
  if (System.Array.IndexOf(want, a.GetName().Name) >= 0 && !prm.ReferencedAssemblies.Contains(a.Location)) prm.ReferencedAssemblies.Add(a.Location);
}
var res = new Microsoft.CSharp.CSharpCodeProvider().CompileAssemblyFromSource(prm, src);
if (res.Errors.HasErrors) { var sb = new System.Text.StringBuilder("COMPILE ERRORS:\n"); int n = 0; foreach (System.CodeDom.Compiler.CompilerError e in res.Errors) if (!e.IsWarning && n++ < 12) sb.AppendLine(e.Line + ": " + e.ErrorText); return sb.ToString(); }
return res.CompiledAssembly.GetType("LongRun").GetMethod("Run").Invoke(null, null);
'''


def call(code):
    sid = None
    def rpc(method, params=None, notify=False):
        nonlocal sid
        body = {'jsonrpc': '2.0', 'method': method}
        if params is not None: body['params'] = params
        if not notify: body['id'] = 1
        h = {'Content-Type': 'application/json', 'Accept': 'application/json, text/event-stream'}
        if sid: h['Mcp-Session-Id'] = sid
        with urllib.request.urlopen(urllib.request.Request(URL, json.dumps(body).encode(), h), timeout=600) as r:
            sid = r.headers.get('Mcp-Session-Id') or sid
            txt = r.read().decode('utf-8', 'replace'); ct = r.headers.get('Content-Type', '')
        if notify: return None
        if ct.startswith('text/event-stream'): txt = [l[5:].strip() for l in txt.splitlines() if l.startswith('data:')][-1]
        return json.loads(txt)
    rpc('initialize', {'protocolVersion': '2025-03-26', 'capabilities': {}, 'clientInfo': {'name': 'run_unity_cs', 'version': '1'}})
    rpc('notifications/initialized', notify=True)
    return rpc('tools/call', {'name': 'execute_code', 'arguments': {'action': 'execute', 'code': code, 'compiler': 'codedom'}})


if __name__ == '__main__':
    path = os.path.abspath(sys.argv[1]).replace('\\', '/')
    res = call(LOADER.replace('%PATH%', path))
    sc = res.get('result', {}).get('structuredContent') or res
    sys.stdout.reconfigure(encoding='utf-8')
    print(json.dumps(sc, ensure_ascii=False)[:4000])
