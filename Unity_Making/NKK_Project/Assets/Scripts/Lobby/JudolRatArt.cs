using System.Collections.Generic;
using NKK.Rats;
using UnityEngine;

namespace NKK.Lobby
{
    // 쥐돌깨기 쥐 그림: 원작 파츠를 웹 프로토타입 drawQuad (= RatRig.Build) 와 같은 규칙으로 조립해 큰 텍스처 한 장에 칸마다 미리 그려 둠.
    // 수백 마리가 날아도 RawImage 하나씩 (같은 텍스처라 한 번에 그려짐). 종 · 등급 · 자세(서 있음/날아감)마다 한 칸.
    // 웹 칸 96 → 텍스처 칸 128 픽셀. 칸 안 쥐 길이 56 · 발끝 = 가운데 아래 17.9 (웹 ratSprite). 등급 오라도 같이 그림.
    public static class JudolRatArt
    {
        const int Cell = 128, PageSize = 2048, PerRow = PageSize / Cell;
        const float S = 96, L = 56;
        static readonly List<RenderTexture> pages = new();
        static readonly Dictionary<string, (Texture tex, Rect uv)> map = new();
        static int used;
        static Texture2D aura;
        static Material mat;

        public static void Clear()
        {
            foreach (var p in pages) if (p) { p.Release(); Object.Destroy(p); }
            pages.Clear(); map.Clear(); used = 0;
        }

        // 쥐 그림 칸. auraCol.a = 오라 진하기 (0 = 오라 없음)
        public static bool Get(RatArtLibrary.Entry e, string code, int grade, bool air, Color auraCol, float auraR, Material bake, out Texture tex, out Rect uv)
        {
            tex = null; uv = default;
            if (pages.Count > 0 && !pages[0].IsCreated()) Clear();          // 그래픽 장치가 바뀌어 텍스처가 날아감 → 다시 그림
            string key = code + "|" + grade + "|" + (air ? 1 : 0);
            if (map.TryGetValue(key, out var v)) { tex = v.tex; uv = v.uv; return true; }
            if (!bake || !Usable(e)) return false;
            mat = bake;
            int page = used / (PerRow * PerRow), idx = used % (PerRow * PerRow); used++;
            while (pages.Count <= page)
            {
                var rt = new RenderTexture(PageSize, PageSize, 0, RenderTextureFormat.ARGB32) { name = "JudolRats" + pages.Count, useMipMap = false, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                rt.Create(); pages.Add(rt);
                var prev0 = RenderTexture.active; RenderTexture.active = rt; GL.Clear(false, true, new Color(0, 0, 0, 0)); RenderTexture.active = prev0;
            }
            int cx = idx % PerRow * Cell, cy = idx / PerRow * Cell;
            var prev = RenderTexture.active;
            RenderTexture.active = pages[page];
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, PageSize, PageSize, 0);           // y 아래 + (웹 캔버스와 같음)
            var m = Aff.Id.T(cx + Cell * 0.5f, cy + Cell * 0.5f).S(Cell / S, Cell / S);
            // 바탕: 오라 (없으면 투명하지만 색은 쥐 털색 비슷하게 — 가장자리가 검게 번지지 않게) 를 덮어씀
            DrawAura(m, auraCol, auraR, cx, cy);
            var pose = air ? Pose.Air : Pose.Idle;
            DrawRat(e, m.T(0, L * 0.32f), L, pose);
            GL.PopMatrix();
            RenderTexture.active = prev;
            tex = pages[page];
            uv = new Rect((float)cx / PageSize, 1 - (float)(cy + Cell) / PageSize, (float)Cell / PageSize, (float)Cell / PageSize);
            map[key] = (tex, uv);
            return true;
        }

        // 파츠가 다 있는 쥐만 (웹 POOL: Parts/ 폴더 쥐). 한 장짜리 · 머리만 · 쌍둥이는 뺌
        public static bool Usable(RatArtLibrary.Entry e) => e != null && e.head && e.torso && e.tail && e.front && e.back && !e.IsSingle && !e.headOnly && string.IsNullOrEmpty(e.twinCodeId);

        // ── 자세 (웹 Pose.ratIdle(0) · ratAir) ──
        struct Pose
        {
            public float head, tail, front, back, farFront, farBack;
            public static readonly Pose Idle = new() { head = 0, tail = 0.15f, front = 0.05f, back = -0.05f, farFront = -0.1f, farBack = 0.1f };
            public static readonly Pose Air = new() { head = -0.3f, tail = 1.2f, front = 1.3f, back = -1.3f, farFront = 1.0f, farBack = -1.0f };
        }

        // 웹 drawQuad: 오른쪽을 보게 뒤집음 (face = 1)
        static void DrawRat(RatArtLibrary.Entry e, Aff m0, float length, Pose p)
        {
            float tw = e.torso.rect.width, th = e.torso.rect.height;
            var pvF = PivotTL(e.front);
            float legLenF = e.front.rect.height * (1 - pvF.y) * e.legFront;
            float tx = -tw * 0.5f, ty = -(e.shoulder.y * th + legLenF);
            Vector2 Pl(Vector2 a) => new(tx + a.x * tw, ty + a.y * th);
            Vector2 hip = Pl(e.hip), neck = Pl(e.neck), tail = Pl(e.tailAnchor), shoulder = Pl(e.shoulder);
            float headScale = e.headScale > 0 ? e.headScale : Mathf.Min(1, 0.62f * tw / e.head.rect.width);
            float len = tw + e.head.rect.width * headScale * 0.55f, u = length / len, gap = tw * 0.07f;
            var m = m0.S(-u, u);
            var far = new Color(0.8f, 0.75f, 0.76f, 1);          // 먼 쪽 다리 어둡게 (웹 darker 0.22)
            Part(m, e.back, hip, p.farBack, gap, 0, e.legBack, far);
            Part(m, e.front, shoulder, p.farFront, gap, 0, e.legFront, far);
            Part(m, e.tail, tail, -p.tail, 0, 0, 1, Color.white);
            Quad(m, e.torso, tx, ty, Color.white);
            Part(m, e.back, hip, p.back, 0, 0, e.legBack, Color.white);
            bool raised = p.front > 0.9f;
            if (!raised) Part(m, e.front, shoulder, p.front, 0, 0, e.legFront, Color.white);
            Part(m, e.head, neck, -p.head, 0, 0, headScale, Color.white);
            if (raised) Part(m, e.front, shoulder, p.front, 0, 0, e.legFront, Color.white);
        }

        static Vector2 PivotTL(Sprite s) => new(s.pivot.x / s.rect.width, 1 - s.pivot.y / s.rect.height);

        static void Part(Aff m, Sprite s, Vector2 at, float ang, float dx, float dy, float sc, Color c)
        {
            if (!s) return;
            var pv = PivotTL(s);
            Quad(m.T(at.x + dx, at.y + dy).R(ang).S(sc, sc), s, -pv.x * s.rect.width, -pv.y * s.rect.height, c);
        }

        static Color Lin(Color c) => QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;

        // 스프라이트 한 장 (왼쪽 위 = (x0, y0), 스프라이트 픽셀 크기)
        static void Quad(Aff m, Sprite s, float x0, float y0, Color c)
        {
            var t = s.texture; var r = s.textureRect;
            float u0 = r.xMin / t.width, u1 = r.xMax / t.width, vTop = r.yMax / t.height, vBot = r.yMin / t.height;
            float w = s.rect.width, h = s.rect.height;
            mat.mainTexture = t; mat.SetPass(1);
            GL.Begin(GL.QUADS); GL.Color(Lin(c));
            V(m, x0, y0, u0, vTop); V(m, x0 + w, y0, u1, vTop); V(m, x0 + w, y0 + h, u1, vBot); V(m, x0, y0 + h, u0, vBot);
            GL.End();
        }

        static void V(Aff m, float x, float y, float u, float v) { GL.TexCoord2(u, v); var q = m.P(x, y); GL.Vertex3(q.x, q.y, 0); }

        // 등급 오라 (웹: 가운데 a → 0.6 에서 a/2 → 끝 0). 칸 전체를 덮어씀 (패스 0)
        static void DrawAura(Aff m, Color col, float r, int cx, int cy)
        {
            if (!aura) MakeAura();
            mat.mainTexture = aura; mat.SetPass(0);
            // 칸 전체: 투명 (색은 오라 색 또는 털색 비슷한 갈색)
            var bg = col.a > 0 ? new Color(col.r, col.g, col.b, 0) : new Color(0.55f, 0.45f, 0.4f, 0);
            GL.Begin(GL.QUADS); GL.Color(Lin(bg));
            GL.TexCoord2(0.01f, 0.01f); GL.Vertex3(cx, cy, 0); GL.Vertex3(cx + Cell, cy, 0); GL.Vertex3(cx + Cell, cy + Cell, 0); GL.Vertex3(cx, cy + Cell, 0);
            GL.End();
            if (col.a <= 0) return;
            mat.SetPass(0);
            GL.Begin(GL.QUADS); GL.Color(Lin(col));
            V(m, -r, -r, 0, 1); V(m, r, -r, 1, 1); V(m, r, r, 1, 0); V(m, -r, r, 0, 0);
            GL.End();
        }

        static void MakeAura()
        {
            const int N = 64;
            aura = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "JudolAura", wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float d = new Vector2(x + 0.5f - N / 2f, y + 0.5f - N / 2f).magnitude / (N / 2f);
                float a = d < 0.6f ? Mathf.Lerp(1, 0.5f, d / 0.6f) : Mathf.Lerp(0.5f, 0, (d - 0.6f) / 0.4f);
                px[y * N + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
            }
            aura.SetPixels(px); aura.Apply();
        }

        // 2D 변환 (웹 캔버스 translate · rotate · scale 과 같은 순서)
        struct Aff
        {
            public float a, b, c, d, e, f;
            public static Aff Id => new() { a = 1, d = 1 };
            public Aff T(float x, float y) { var m = this; m.e += a * x + c * y; m.f += b * x + d * y; return m; }
            public Aff S(float sx, float sy) { var m = this; m.a *= sx; m.b *= sx; m.c *= sy; m.d *= sy; return m; }
            public Aff R(float th)
            {
                float cs = Mathf.Cos(th), sn = Mathf.Sin(th); var m = this;
                m.a = a * cs + c * sn; m.b = b * cs + d * sn; m.c = -a * sn + c * cs; m.d = -b * sn + d * cs; return m;
            }
            public Vector2 P(float x, float y) => new(a * x + c * y + e, b * x + d * y + f);
        }
    }
}
