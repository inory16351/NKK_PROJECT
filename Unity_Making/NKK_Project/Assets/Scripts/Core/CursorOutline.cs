using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 마우스 커서 그림의 외곽선을 따라 차는 선 (총공격 쿨타임 게이지, RushUI 가 씀)
    //  길 = 커서 그림 알파 → 원본 64px 기준 gap(2px) 만큼 부풀린 윤곽 → 끝점(코끝)에서 시계 방향 한 바퀴.
    //       그림(스킨 · 누름)마다 처음 한 번만 계산해 둠 → 커서 장식을 새로 넣어도 따로 할 일 없음 (그림 Read/Write 켬)
    //  그리기 = 화면 맨 위 캔버스(1단위 = 1픽셀)에 화면 좌표로 바로. 가운데 진한 선 + 양옆 흐려지는 번짐 + 끝머리 흰 점
    [RequireComponent(typeof(CanvasRenderer))]
    public class CursorOutline : MaskableGraphic
    {
        [Tooltip("얼마나 찼나 (0~1)")] public float fill;
        [Tooltip("선 굵기 (화면 픽셀)")] public float width = 2;
        [Tooltip("양옆 번짐 폭 (화면 픽셀)")] public float glow = 2.6f;
        [Tooltip("선 가운데를 커서 바깥으로 미는 거리 (화면 픽셀, 굵은 선이 커서 밑에 가려지지 않게)")] public float outset;
        [Tooltip("진행 끝머리 흰 점")] public bool knob = true;
        public Color lineColor = new Color32(112, 226, 64, 255);
        [Range(0, 1)] public float alpha = 1;

        Path path; Vector2 topLeft; float size = 32;

        // 커서 그림 · 끝점(원본 픽셀) · 마우스 위치(화면) · 화면 커서 크기 → 이번 프레임 선 위치
        public bool Place(Texture2D tex, Vector2 hotspot, Vector2 mouse, float pixelSize)
        {
            path = Get(tex, hotspot);
            if (path == null) return false;
            size = pixelSize;
            float k = pixelSize / tex.width;
            topLeft = mouse + new Vector2(-hotspot.x * k, hotspot.y * k);      // 커서 그림 왼쪽 위 (화면, y 위로)
            SetVerticesDirty();
            return true;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (path == null || fill <= 0.001f || alpha <= 0.001f) return;
            float h = width * 0.5f;
            Color core = lineColor; core.a *= alpha;
            Color g0 = core; g0.a *= 0.45f;
            Color g1 = core; g1.a = 0;

            // 찬 만큼의 점 (끝은 선분 사이를 잘라서)
            bool closed = fill >= 0.999f;
            var pts = new List<Vector2>(path.p.Length);
            float t = Mathf.Clamp01(fill) * path.L[path.L.Length - 1];
            for (int i = 0; i < path.p.Length; i++)
            {
                if (path.L[i] <= t) { pts.Add(ToScreen(path.p[i])); continue; }
                if (i > 0)
                {
                    float u = (t - path.L[i - 1]) / Mathf.Max(path.L[i] - path.L[i - 1], 1e-6f);
                    pts.Add(ToScreen(Vector2.Lerp(path.p[i - 1], path.p[i], u)));
                }
                break;
            }
            if (closed && pts.Count > 1) pts.RemoveAt(pts.Count - 1);    // 마지막 점 = 첫 점 → 고리로 이음
            int n = pts.Count;
            if (n < 2) return;

            // 단면 6개: 번짐 끝 · 번짐 시작 · 선 왼쪽 · 선 오른쪽 · 번짐 시작 · 번짐 끝
            float[] off = { -(h + glow), -h, -h, h, h, h + glow };
            Color[] col = { g1, g0, core, core, g0, g1 };
            Vector2 c0 = pts[0], c1 = pts[n - 1];
            for (int i = 0; i < n; i++)
            {
                Vector2 a = pts[closed ? (i - 1 + n) % n : Mathf.Max(i - 1, 0)], b = pts[closed ? (i + 1) % n : Mathf.Min(i + 1, n - 1)];
                Vector2 d = (b - a).normalized, nn = new Vector2(-d.y, d.x);
                // 꺾이는 곳은 굵기 유지 (miter, 너무 뾰족하면 제한)
                Vector2 s = (pts[closed ? (i + 1) % n : Mathf.Min(i + 1, n - 1)] - pts[i]);
                if (s.sqrMagnitude < 1e-8f) s = pts[i] - a;
                Vector2 sn = new Vector2(-s.normalized.y, s.normalized.x);
                float miter = 1 / Mathf.Max(0.8f, Vector2.Dot(nn, sn));
                // nn = 바깥쪽 (화면 기준 시계 방향 길의 왼쪽). outset 만큼 커서 바깥으로 밀어 그림
                Vector2 c = pts[i] + nn * outset * miter;
                if (i == 0) c0 = c;
                if (i == n - 1) c1 = c;
                for (int j = 0; j < 6; j++) vh.AddVert(c + nn * off[j] * miter, col[j], Vector4.zero);
            }
            int segs = closed ? n : n - 1;
            for (int i = 0; i < segs; i++)
            {
                int a = i * 6, b = ((i + 1) % n) * 6;
                for (int j = 0; j < 5; j += 2)
                {
                    vh.AddTriangle(a + j, a + j + 1, b + j + 1);
                    vh.AddTriangle(a + j, b + j + 1, b + j);
                }
            }
            if (!closed)
            {
                Disk(vh, c0, h, glow, core, g0);                     // 둥근 시작
                if (knob) { Disk(vh, c1, width * 0.95f, glow, core, g0); Disk(vh, c1, width * 0.45f, 0, new Color(1, 1, 1, alpha), default); }
                else Disk(vh, c1, h, glow, core, g0);
            }
        }

        Vector2 ToScreen(Vector2 p) => topLeft + new Vector2(p.x * size, -p.y * size);

        // 동그라미 (가운데 진하게 + 바깥 번짐)
        static void Disk(VertexHelper vh, Vector2 c, float r, float gl, Color core, Color g0)
        {
            const int N = 16;
            int o = vh.currentVertCount;
            vh.AddVert(c, core, Vector4.zero);
            Color g1 = g0; g1.a = 0;
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2 / N; var v = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vh.AddVert(c + v * r, core, Vector4.zero);
                if (gl > 0) { vh.AddVert(c + v * r, g0, Vector4.zero); vh.AddVert(c + v * (r + gl), g1, Vector4.zero); }
            }
            int step = gl > 0 ? 3 : 1;
            for (int i = 0; i < N; i++)
            {
                int a = o + 1 + i * step, b = o + 1 + (i + 1) % N * step;
                vh.AddTriangle(o, a, b);
                if (gl > 0) { vh.AddTriangle(a + 1, a + 2, b + 2); vh.AddTriangle(a + 1, b + 2, b + 1); }
            }
        }

        // ── 외곽선 길 (그림 크기 1 기준, x 오른쪽 · y 아래, 왼쪽 위 = 0)
        class Path { public Vector2[] p; public float[] L; }
        static readonly Dictionary<Texture2D, Path> cache = new();

        static Path Get(Texture2D tex, Vector2 hot)
        {
            if (!tex || !tex.isReadable) return null;
            if (cache.TryGetValue(tex, out var c)) return c;
            cache[tex] = c = Build(tex, hot);
            return c;
        }

        static Path Build(Texture2D tex, Vector2 hot)
        {
            int w = tex.width, hgt = tex.height;
            var px = tex.GetPixels32();
            float gap = 2f * w / 64f;                                 // 원본 64px 에서 2px 바깥
            int r = Mathf.CeilToInt(gap), pad = r + 2, W = w + pad * 2, H = hgt + pad * 2;
            var d = new bool[W * H];
            var offs = new List<Vector2Int>();
            for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++) if (dx * dx + dy * dy <= gap * gap + 0.5f) offs.Add(new Vector2Int(dx, dy));
            for (int y = 0; y < hgt; y++) for (int x = 0; x < w; x++)
            {
                if (px[(hgt - 1 - y) * w + x].a <= 96) continue;       // GetPixels32 = 아래 줄부터 → 위 줄부터로
                foreach (var o in offs) d[(x + pad + o.x) + (y + pad + o.y) * W] = true;
            }
            int s = System.Array.IndexOf(d, true);
            if (s < 0) return null;

            // 테두리 따라가기 (Moore, 화면 기준 시계 방향)
            var nb = new[] { new Vector2Int(-1, 0), new(-1, -1), new(0, -1), new(1, -1), new(1, 0), new(1, 1), new(0, 1), new(-1, 1) };
            var start = new Vector2Int(s % W, s / W);
            var pts = new List<Vector2> { start };
            var cur = start; int back = 0;
            for (int it = 0; it < 20000; it++)
            {
                bool found = false;
                for (int k = 0; k < 8; k++)
                {
                    int j = (back + 1 + k) % 8; var q = cur + nb[j];
                    if (q.x >= 0 && q.x < W && q.y >= 0 && q.y < H && d[q.x + q.y * W]) { back = (j + 4) % 8; cur = q; found = true; break; }
                }
                if (!found || cur == start) break;
                pts.Add(cur);
            }
            int n = pts.Count;
            if (n < 3) return null;
            var p = new Vector2[n];
            for (int i = 0; i < n; i++) p[i] = pts[i] - new Vector2(pad - 0.5f, pad - 0.5f);
            // 부드럽게 (계단 없애기) → 고른 간격으로 다시 찍고 → 한 번 더 부드럽게 (꺾임에서 선이 삐죽 튀지 않게)
            p = Smooth(p, 3);
            p = Resample(p, w / 64f);
            n = p.Length;
            if (n < 3) return null;
            p = Smooth(p, 4);
            // 시계 방향 보장 (y 아래 좌표에서 넓이 +)
            float area = 0;
            for (int i = 0; i < n; i++) { var a = p[i]; var b = p[(i + 1) % n]; area += a.x * b.y - b.x * a.y; }
            if (area < 0) System.Array.Reverse(p);
            // 끝점(코끝)에서 출발
            int st = 0; float best = float.MaxValue;
            for (int i = 0; i < n; i++) { float e = (p[i] - hot).sqrMagnitude; if (e < best) { best = e; st = i; } }
            var path = new Path { p = new Vector2[n + 1], L = new float[n + 1] };
            for (int i = 0; i <= n; i++) path.p[i] = new Vector2(p[(st + i) % n].x / w, p[(st + i) % n].y / hgt);
            for (int i = 1; i <= n; i++) path.L[i] = path.L[i - 1] + Vector2.Distance(path.p[i], path.p[i - 1]);
            return path;
        }

        static Vector2[] Smooth(Vector2[] p, int passes)
        {
            int n = p.Length;
            for (int pass = 0; pass < passes; pass++)
            {
                var q = new Vector2[n];
                for (int i = 0; i < n; i++) q[i] = (p[(i - 1 + n) % n] + p[i] * 2 + p[(i + 1) % n]) / 4;
                p = q;
            }
            return p;
        }

        // 닫힌 길을 같은 간격(step)으로 다시 찍음
        static Vector2[] Resample(Vector2[] p, float step)
        {
            int n = p.Length;
            float total = 0;
            for (int i = 0; i < n; i++) total += Vector2.Distance(p[i], p[(i + 1) % n]);
            int m = Mathf.Max(3, Mathf.RoundToInt(total / step));
            float ds = total / m;
            var o = new Vector2[m];
            int seg = 0; float segStart = 0, segLen = Vector2.Distance(p[0], p[1 % n]);
            for (int k = 0; k < m; k++)
            {
                float t = k * ds;
                while (segStart + segLen < t && seg < n - 1) { segStart += segLen; seg++; segLen = Vector2.Distance(p[seg], p[(seg + 1) % n]); }
                float u = segLen > 1e-6f ? (t - segStart) / segLen : 0;
                o[k] = Vector2.Lerp(p[seg], p[(seg + 1) % n], Mathf.Clamp01(u));
            }
            return o;
        }
    }
}
