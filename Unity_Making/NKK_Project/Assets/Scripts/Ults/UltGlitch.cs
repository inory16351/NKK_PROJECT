using System.Collections.Generic;
using NKK.Items;
using NKK.Rats;
using TMPro;
using UnityEngine;

namespace NKK.Ults
{
    // 우주 쥐 · 빅뱅.exe (웹게임 glitch): 5.1초 동안 우주가 버그 남 (물건 잔상·순간이동, 쥐 자세 깨짐, 오류 창) → 강제 종료 = 주변 물건 일괄 삭제
    // 자막: c1 재부팅 · c2 응답 없음 · c3 강제 종료 / 오류 창 글: c4 제목(우주.exe) · c5·c6 내용 두 줄 · c7 기다리기 · c8 강제 종료 · c9 경고 아이콘(!)
    public class UltGlitch : UltBase
    {
        const float DEL = 5.1f, TICK = 0.1f;
        public override float Dur => 6.2f;

        List<Item> vict;
        readonly Dictionary<Item, Vector2> home = new();                 // 원래 자리 (근처에서만 튐)
        readonly List<(UltProp p, float life)> ghosts = new();          // 물건 잔상
        // 가짜 오류 창 (그림 ult_error_window + 그 위 월드 글자. 창마다 정렬 순서가 달라 겹친 창 글자도 제대로 가려짐)
        class Win { public UltProp p, btn; public TextMeshPro title, icon, msg1, msg2, wait, kill; }
        readonly List<Win> windows = new();
        readonly List<GameObject> temps = new();
        Material txtMat;
        static readonly Color WinInk = C("#4b4540"), BtnRed = C("#d9786a");
        bool deleted, noWindowArt;
        static readonly Color[] bits = { C("#a58bb8"), C("#9dd5a8"), C("#e8786a"), Color.white };

        public override void Begin()
        {
            Beat(0.1f, () => Cap("c1"));
            Beat(2.6f, () => Cap("c2"));
            Beat(4.9f, () => Cap("c3"));
            vict = ItemsIn(R.x, R.y, ULT_R);
            if (vict.Count > 44) vict.RemoveRange(44, vict.Count - 44);
            foreach (var it in vict) home[it] = new Vector2(it.x, it.y);
            // 화면 속 쥐들도 버그: 자세가 깨지고 뚝뚝 끊겨 움직임 (삭제 순간 풀어 줌)
            foreach (var o in RatMgr.Rats)
            {
                if (Rats.Count >= 16) break;
                if (o != R && !o.UltOn && OnScreen(o.x, o.y, 0)) GrabRat(o);
            }
        }

        public override void Step(float dt, float k)
        {
            R.UltPose = Pose(PoseUp(), Rand(-0.6f, 0.6f));
            R.UltJit = deleted ? 0 : 2;
            if (!deleted && (hitT -= dt) <= 0)
            {
                hitT = TICK;
                Glitch();
            }
            for (int i = ghosts.Count - 1; i >= 0; i--)
            {
                var g = ghosts[i]; g.life -= dt;
                if (g.life <= 0) { KillProp(g.p); ghosts.RemoveAt(i); continue; }
                g.p.alpha = 0.4f * Mathf.Clamp01(g.life / 0.5f); ghosts[i] = g;
            }
            Windows();
            if (T >= DEL && !deleted) Delete();
        }

        // 0.1초마다: 물건 4개 순간이동(잔상 남김) + 쥐 자세 깨짐 + 화면 찢김 대신 번쩍·지직
        void Glitch()
        {
            var rest = new List<Item>(); foreach (var it in vict) if (it && it.State == Item.ItemState.Rest) rest.Add(it);
            for (int n = 0; n < 4 && rest.Count > 0; n++)
            {
                int i = Random.Range(0, rest.Count); var it = rest[i]; rest.RemoveAt(i);
                Ghost(it);
                var h = home[it];
                it.x = h.x + Rand(-50, 50); it.y = h.y + Rand(-40, 40); it.Rot = Rand(0, Mathf.PI * 2);
                if (Random.value < 0.25f && OnScreen(it.x, it.y)) Fx?.Anim("zap", it.x, it.y, 10, 0.6f);
            }
            foreach (var o in Rats)
            {
                if (!o || Random.value >= 0.5f) continue;
                o.UltPose = P(front: Rand(-3, 3), farFront: Rand(-3, 3), back: Rand(-3, 3), farBack: Rand(-3, 3), head: Rand(-1.5f, 1.5f), tail: Rand(-2, 2), sx: Rand(0.6f, 1.8f), sy: Rand(0.6f, 1.8f), headX: Rand(-10, 10));
                o.UltJit = Rand(0, 4);
                if (Random.value < 0.3f) MoveRat(o, Rand(-24, 24), Rand(-18, 18));
            }
            // 화면 찢김 (웹 ui) 대신: 보라 번쩍 + 지직 흔들림 + 픽셀 조각
            if (Random.value < 0.35f) Flash(Pick(bits), Rand(0.05f, 0.14f));
            Fx?.Shake(0.03f);
            var v = ViewRect(60);
            Fx?.Burst(Rand(v.xMin, v.xMax), Rand(v.yMin, v.yMax), Rand(0, 60), 4, Pick(bits), Pick(bits), 40, 160, 4, 8);
            if (Random.value < 0.3f) Fx?.Ring(R.x, R.y, Rand(60, 200), new Color(0.65f, 0.55f, 0.72f, 0.8f), 0.2f);
        }

        // 잔상: 물건 그림을 그 자리에 반투명하게 복사 (0.5초)
        void Ghost(Item it)
        {
            if (!it.sprite || !it.sprite.sprite || ghosts.Count > 40 || !OnScreen(it.x, it.y)) return;
            var src = it.sprite;
            var go = new GameObject("UltGhost"); go.transform.SetParent(M.propRoot ? M.propRoot : M.transform, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = src.sprite;
            sr.sortingLayerID = src.sortingLayerID; sr.sharedMaterial = src.sharedMaterial;
            Vector3 wp = src.transform.position, ls = src.transform.lossyScale;
            var p = new UltProp { r = sr };
            p.x = wp.x / World.U; p.y = it.y; p.z = it.y * World.TILT + wp.y / World.U;
            p.w = src.sprite.bounds.size.x * Mathf.Abs(ls.x) / World.U;
            p.flat = Mathf.Abs(ls.x) > 0.0001f ? Mathf.Abs(ls.y / ls.x) : 1;
            p.flip = ls.x < 0; p.rot = src.transform.eulerAngles.z * Mathf.Deg2Rad; p.alpha = 0.4f; p.sortBias = -1;
            p.tint = new Color(0.85f, 0.75f, 1f);
            Props.Add(p); ghosts.Add((p, 0.5f));
        }

        // 가짜 오류 창 (웹 ui): 0.8초마다 한 장씩 (최대 5장), 화면 가운데에 겹쳐 뜸. 그림(ult_error_window)이 없으면 생략
        //   글 = 웹과 같은 자리: 파란 제목줄 '우주.exe' · 경고 아이콘 · 내용 두 줄 · 버튼 [기다리기] [강제 종료] (끝 직전 맨 앞 창의 강제 종료가 빨갛게 눌림)
        void Windows()
        {
            if (deleted || noWindowArt) return;
            int n = Mathf.Min(5, 1 + Mathf.FloorToInt(T / 0.8f));
            while (windows.Count < n)
            {
                var p = Prop("ult_error_window", R.x, R.y, 0, 420);
                if (p == null) { noWindowArt = true; return; }
                var w = new Win { p = p };
                w.btn = Prop("ult_gauge_bar", R.x, R.y, 0, 100); if (w.btn != null) { w.btn.tint = BtnRed; w.btn.visible = false; }
                w.title = Txt(Color.white, true, false); w.icon = Txt(Color.white, true, true);
                w.msg1 = Txt(WinInk, false, false); w.msg2 = Txt(WinInk, false, false);
                w.wait = Txt(WinInk, true, true); w.kill = Txt(WinInk, true, true);
                windows.Add(w);
                Fx?.Shake(0.05f);
            }
            var v = ViewRect();
            float cx = v.center.x, cy = v.center.y;
            for (int i = 0; i < n; i++)
            {
                var w = windows[i]; var p = w.p; bool top = i == n - 1;
                p.x = cx + (i - (n - 1) / 2f) * 34 + (top && T - i * 0.8f < 0.32f ? Rand(-4, 4) : 0);
                p.y = cy + (i - (n - 1) / 2f) * 26 / World.TILT; p.z = 0;
                int so = 31000 + i * 4; p.sortBias = so - World.SortOrder(p.y);
                // 그림 비율 (344×243): 제목줄 위 21% · 아이콘 (0.2, 0.44) · 버튼 (0.29 / 0.71, 0.78) 크기 0.37×0.19
                float W = p.w, H = W * Aspect(p);
                bool press = top && T > DEL - 0.35f;
                if (w.btn != null)
                {
                    w.btn.visible = press; w.btn.x = p.x + 0.209f * W; w.btn.y = p.y; w.btn.z = -0.274f * H;
                    w.btn.w = 0.37f * W * (press ? 0.96f : 1); w.btn.flat = 0.19f * H / Mathf.Max(0.01f, w.btn.w * Aspect(w.btn));
                    w.btn.sortBias = so + 1 - World.SortOrder(w.btn.y);
                }
                Put(w.title, CapText("c4"), p, -0.46f * W, 0.397f * H, 18, 0.5f * W, so + 2);
                Put(w.icon, CapText("c9"), p, -0.297f * W, 0.056f * H, 40, 0.2f * W, so + 2);
                Put(w.msg1, CapText("c5"), p, -0.137f * W, 0.13f * H, 15, 0.6f * W, so + 2);
                Put(w.msg2, CapText("c6"), p, -0.137f * W, -0.014f * H, 15, 0.6f * W, so + 2);
                Put(w.wait, CapText("c7"), p, -0.212f * W, -0.274f * H, 16, 0.34f * W, so + 2);
                Put(w.kill, CapText("c8"), p, 0.209f * W, -0.274f * H, 16, 0.34f * W, so + 2);
                if (w.kill) w.kill.color = press ? Color.white : WinInk;
            }
        }

        // 오류 창 글자: TextMeshPro(월드, 정렬 순서 지정). 글꼴 = 팝업 글자 틀과 같음, 재질은 테두리·그림자 없는 복사본
        TextMeshPro Txt(Color col, bool bold, bool center)
        {
            var tpl = Fx ? Fx.popupTemplate : null; if (!tpl || !tpl.font) return null;
            var go = new GameObject("UltErrText"); go.transform.SetParent(M.propRoot ? M.propRoot : M.transform, false);
            temps.Add(go);
            var t = go.AddComponent<TextMeshPro>();
            t.rectTransform.localScale = Vector3.one * 0.1f;        // TextMeshPro 글자 크기 1 = 0.1 유닛 → 0.1배 하면 글자 크기 = 게임 단위
            t.font = tpl.font;
            if (!txtMat)
            {
                txtMat = new Material(tpl.fontSharedMaterial);
                txtMat.DisableKeyword(ShaderUtilities.Keyword_Underlay); txtMat.DisableKeyword(ShaderUtilities.Keyword_Outline);
                txtMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0); txtMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.05f);
                txtMat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0));
            }
            t.fontSharedMaterial = txtMat;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            t.alignment = center ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
            t.rectTransform.pivot = new Vector2(center ? 0.5f : 0, 0.5f); t.rectTransform.sizeDelta = Vector2.zero;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.color = col; t.text = "";
            return t;
        }
        // 창 기준 (dx, 높이 dz) 에 글자. maxW 보다 길면 줄여서 맞춤
        void Put(TextMeshPro t, string s, UltProp p, float dx, float dz, float size, float maxW, int order)
        {
            if (!t) return;
            if (t.text != s) t.text = s;
            t.fontSize = size;
            float w = t.GetPreferredValues(s).x * 10;             // 로컬 → 게임 단위 (0.1배 · 0.01 유닛)
            if (w > maxW) t.fontSize = size * maxW / w;
            t.transform.position = World.ToUnity(p.x + dx, p.y, p.z + dz);
            var mr = t.renderer; if (mr) { mr.sortingLayerID = p.r ? p.r.sortingLayerID : 0; mr.sortingOrder = order; }
        }
        static float Aspect(UltProp p) => p != null && p.r && p.r.sprite ? p.r.sprite.bounds.size.y / Mathf.Max(1e-5f, p.r.sprite.bounds.size.x) : 1;

        // 강제 종료: 남은 물건 전부 삭제 (박살) + 쥐들 원래대로
        void Delete()
        {
            deleted = true;
            foreach (var it in vict)
            {
                if (!it || it.State != Item.ItemState.Rest) continue;
                if (OnScreen(it.x, it.y)) Fx?.Burst(it.x, it.y, 15, 6, Pick(bits), Pick(bits), 40, 140, 4, 8);
                if (it.SkillHit(ItemD * 1.5f, R)) it.Fling(0, 0, -1);           // 그 자리에서 바로 박살
                else { float a = Rand(0, Mathf.PI * 2); it.Fling(Mathf.Cos(a) * 220, Mathf.Sin(a) * 220, 320); }   // 버텨도 튕겨 나감
            }
            foreach (var g in ghosts) KillProp(g.p);
            ghosts.Clear();
            foreach (var w in windows) { KillProp(w.p); KillProp(w.btn); }
            windows.Clear(); KillTemps();
            foreach (var o in new List<Rat>(Rats)) ReleaseRat(o);
            Flash(Color.white, 0.4f); Fx?.Shake(0.4f); Fx?.Hitstop(0.08f);
            Fx?.Ring(R.x, R.y, ULT_R, Color.white, 0.4f); Fx?.Ring(R.x, R.y, ULT_R * 0.6f, Col, 0.5f);
        }

        void KillTemps() { foreach (var go in temps) if (go) Object.Destroy(go); temps.Clear(); }
        public override void Cleanup() { KillTemps(); if (txtMat) Object.Destroy(txtMat); txtMat = null; }

        static RatRig.Pose Pose(RatRig.Pose p, float head) { p.head = head; return p; }
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
    }
}
