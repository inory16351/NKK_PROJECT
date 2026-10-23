using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NKK.Hazards
{
    // 전용 파츠 보스 리그 (BossPartArt 한 줄을 관절 트리로 조립). 리그 좌표 = 파츠 픽셀 / 100, 발끝 = 원점, 그림은 왼쪽을 봄.
    // 관절마다 Transform 하나 + 그림 자식 (그림의 pivot 이 관절에 오게 옮김) → 각도·크기·뒤집기는 관절에, 자식 파츠도 같이 따라감.
    [RequireComponent(typeof(SortingGroup))]
    public class BossPartRig : MonoBehaviour
    {
        public Transform visual;      // 좌우 반전·크기·날아갈 때 회전 (비면 만들 때 자식으로 만듦)
        [Tooltip("먼 쪽 파츠 색")] public Color farColor = new(0.78f, 0.76f, 0.77f);
        [Tooltip("멈춰 있을 때 Walk 파츠 흔들림 (걸을 때 대비)")] public float idleWalk = 0.15f;
        [Tooltip("Recoil 파츠 사격: 초당 발사 수 (그림 깜빡) · 반동 각도 흔들림 (°)")] public float recoilRate = 12, recoilTilt = 4;
        [Tooltip("머리 열림이 이 값 이상이면 닫힘 파츠 대신 열림 파츠를 보임")] public float openSwap = 0.1f;
        [Tooltip("Neck 물기: 한 번 무는 시간 (초) · 0~앞 = 뒤로 움찔, 앞~뒤 = 앞·아래로 찌름 (두 번째 그림), 뒤~1 = 돌아옴 (히드라, 웹 미리보기 BossRework/rig_web/hydra.html)")]
        public float biteTime = 0.9f, biteWind = 0.3f, biteStrike = 0.55f;
        [Tooltip("Neck 물기 순서 (목 번호, 공격 중 한 목씩 차례로)")] public int[] biteOrder = { 3, 0, 5, 1, 6, 2, 4 };
        [Tooltip("Neck IK: 마디가 쉬는 각도에서 벗어날 수 있는 만큼 (°) · 풀이 반복 수")] public float neckLimit = 40; public int neckIters = 8;
        [HideInInspector] public bool still;      // 미리보기용: 시간에 따른 움직임 끔 (BossRigPreview)
        [HideInInspector] public float open;      // 머리 열림 0~1 (Boss 가 매 프레임 정함, 우주복 쥐 — 웹 미리보기 BossRework/rig_web/crewmate.html)

        class J { public BossPartArt.Part p; public Transform j; public SpriteRenderer r; public Vector3 basePos; public float ik; public bool ikAlt; }
        // 히드라 목 사슬 (Neck 파츠, 같은 neck 번호 = 마디 3 + 머리): 부모(몸통) 좌표의 뿌리 · 마디 길이 · 마디 방향 (그림이 왼쪽으로 뻗음 → 약 180°)
        class NeckChain { public J[] js = new J[4]; public Vector2 b; public float[] L = new float[3], O = new float[3]; public float F = 1; }
        readonly List<NeckChain> necks = new();
        readonly List<J> joints = new();
        readonly Dictionary<string, J> byName = new();
        BossPartArt.Entry art;
        SortingGroup group;
        float unit = 1;
        bool wasMoving; float stopTime = -99;      // Wiggle: 걷다 멈춘 순간

        public float Height => art != null ? art.height : 200;
        public BossPartArt.Entry Art => art;
        public bool HasOpen { get; private set; }      // 열리는 머리가 있음 (닫힘·열림 파츠)

        static Vector3 SpritePoint(Sprite s, Vector2 tl) => s ? new Vector3((tl.x * s.rect.width - s.pivot.x) / 100f, ((1 - tl.y) * s.rect.height - s.pivot.y) / 100f, 0) : Vector3.zero;

        public void Build(BossPartArt.Entry e, float scale)
        {
            art = e; group = GetComponent<SortingGroup>();
            if (!visual) { visual = new GameObject("Visual").transform; visual.SetParent(transform, false); }
            for (int i = visual.childCount - 1; i >= 0; i--) Destroy(visual.GetChild(i).gameObject);
            joints.Clear(); byName.Clear(); necks.Clear(); open = 0;
            HasOpen = e.parts.Exists(p => p.show != BossPartArt.Show.Always);
            if (e.parts.Count == 0 || !e.parts[0].sprite) return;
            var root = e.parts[0].sprite;
            unit = e.length * scale / root.rect.width;
            foreach (var p in e.parts)
            {
                byName.TryGetValue(p.parent ?? "", out var par);
                var j = new GameObject(p.name).transform;
                j.SetParent(par != null ? par.j : visual, false);
                Vector3 pos;
                if (par != null) pos = SpritePoint(par.p.sprite, p.at) - SpritePoint(par.p.sprite, par.p.pivot);
                else pos = new Vector3(p.at.x * (p.sprite ? p.sprite.rect.width : 0) / 100f, p.at.y * (p.sprite ? p.sprite.rect.height : 0) / 100f, 0);
                j.localPosition = pos;
                j.localScale = new Vector3(p.flipX ? -p.scale : p.scale, p.scale, 1);
                var go = new GameObject("img"); go.transform.SetParent(j, false);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = p.sprite; r.sortingOrder = p.order;
                if (p.far) r.color = farColor;
                go.transform.localPosition = -SpritePoint(p.sprite, p.pivot);
                var jj = new J { p = p, j = j, r = r, basePos = pos };
                joints.Add(jj); byName[p.name] = jj;
            }
            var chains = new SortedDictionary<int, List<J>>();
            foreach (var j in joints)
                if (j.p.move == BossPartArt.Move.Neck) { if (!chains.TryGetValue(j.p.neck, out var l)) chains[j.p.neck] = l = new List<J>(); l.Add(j); }
            foreach (var l in chains.Values)
            {
                if (l.Count < 4) continue;
                // 첫 마디가 뒤집힌 사슬 (뒤를 보는 목, flipX) = 몸통 좌표의 실제 각도로 풂: 마디 방향 x 반대, 둘째부터 상대 각도 부호 반대
                var c = new NeckChain { b = l[0].basePos, F = l[0].p.flipX ? -1 : 1 };
                float s = 1;
                for (int k = 0; k < 4; k++) c.js[k] = l[k];
                for (int k = 0; k < 3; k++)
                {
                    s *= l[k].p.scale;
                    Vector2 v = l[k + 1].basePos * s;
                    c.L[k] = v.magnitude; c.O[k] = Mathf.Atan2(v.y, v.x * c.F) * Mathf.Rad2Deg;
                }
                necks.Add(c);
            }
        }

        // 목 사슬 끝 (머리 관절) 자리 · 마디 관절 자리 (부모 좌표, 단위)
        static Vector2 NeckFK(NeckChain c, float[] rel, int upTo)
        {
            Vector2 p = c.b; float A = 0;
            for (int k = 0; k < upTo; k++) { A += rel[k]; float d = (A + c.O[k]) * Mathf.Deg2Rad; p += c.L[k] * new Vector2(Mathf.Cos(d), Mathf.Sin(d)); }
            return p;
        }

        // 히드라 목: 머리 관절 목표를 먼저 정하고 (쉬는 자리 + 떠다님 8자 + 물기 = 뒤·위로 wind → 앞·아래로 strike, 픽셀) 마디 3개를 CCD 로 돌림.
        // 매 프레임 쉬는 자세에서 시작, 마디마다 쉬는 각도 ±neckLimit. 머리 = 쉬는 절대 각도 + 끄덕임 → 거의 수평 유지.
        // 값: 첫 마디 amp(떠다님 px) · speed · phase · wind · strike, 머리 amp(끄덕임 °). 웹 hydra.html neckIK · Tools/hydra_rig.py neck_ik 와 같은 계산
        void NeckIK(float t, bool moving, bool alt)
        {
            var rel = new float[3];
            foreach (var c in necks)
            {
                var n1 = c.js[0].p; var head = c.js[3].p;
                float F = c.F, r0 = n1.angle, r1 = c.js[1].p.angle * F, r2 = c.js[2].p.angle * F;      // 실제 각도
                rel[0] = r0; rel[1] = r1; rel[2] = r2;
                var end = NeckFK(c, rel, 3);
                float headAbs = r0 + r1 + r2 + head.angle * F;
                float fa = (headAbs + (F > 0 ? 180 : 0)) * Mathf.Deg2Rad;      // 뒤집은 사슬 = 머리가 오른쪽을 봄
                var face = new Vector2(Mathf.Cos(fa), Mathf.Sin(fa));      // 머리가 보는 쪽 (그림은 왼쪽을 봄)
                float spd = n1.speed * (moving ? 1.5f : 1), ph = n1.phase * Mathf.PI * 2, amp = n1.amp / 100f * (moving ? 1.3f : 1);
                float kw = 0, ks = 0; bool bite = false;
                if (alt && !still && biteOrder.Length > 0)
                {
                    float step = Mathf.Floor(t / biteTime), f = t / biteTime - step;
                    if (biteOrder[(int)Mathf.Repeat(step, biteOrder.Length)] == n1.neck)
                    {
                        if (f < biteWind) kw = Mathf.SmoothStep(0, 1, f / biteWind);
                        else if (f < biteStrike) { ks = Mathf.SmoothStep(0, 1, (f - biteWind) / (biteStrike - biteWind)); kw = 1 - ks; bite = true; }
                        else { float k = Mathf.SmoothStep(0, 1, (f - biteStrike) / (1 - biteStrike)); ks = 1 - k; bite = k < 0.4f; }
                    }
                }
                var wd = new Vector2(-face.x * 0.8f, -face.y * 0.8f + 0.6f).normalized;      // 뒤·위
                var ld = new Vector2(face.x * 0.85f, face.y * 0.85f - 0.5f).normalized;      // 앞·아래
                var tg = end + amp * new Vector2(Mathf.Sin(t * 1.7f * spd + ph), 0.7f * Mathf.Sin(t * 2.6f * spd + ph * 1.7f))
                         + n1.wind / 100f * kw * wd + n1.strike / 100f * ks * ld;
                for (int it = 0; it < neckIters; it++)
                    for (int k = 2; k >= 0; k--)
                    {
                        var pk = NeckFK(c, rel, k); var e = NeckFK(c, rel, 3);
                        float d = Mathf.DeltaAngle(Mathf.Atan2(e.y - pk.y, e.x - pk.x) * Mathf.Rad2Deg, Mathf.Atan2(tg.y - pk.y, tg.x - pk.x) * Mathf.Rad2Deg);
                        float r = k == 0 ? r0 : k == 1 ? r1 : r2;
                        rel[k] = Mathf.Clamp(rel[k] + d, r - neckLimit, r + neckLimit);
                    }
                float nod = (still ? 0 : head.amp * Mathf.Sin(t * 2.1f * spd + ph + 1)) + F * (10 * ks - 8 * kw);
                for (int k = 0; k < 3; k++) { c.js[k].ik = rel[k] * (k == 0 ? 1 : F); c.js[k].ikAlt = false; }
                c.js[3].ik = (headAbs + nod - rel[0] - rel[1] - rel[2]) * F; c.js[3].ikAlt = bite;
            }
        }

        // 파츠의 한 점 (그림 좌상단 기준 0~1) 월드 위치. Apply 뒤에 부름 (발사 위치·손 등)
        public Vector3 Point(string part, Vector2 tl)
        {
            if (!byName.TryGetValue(part, out var j) || !j.r.sprite) return transform.position;
            return j.r.transform.TransformPoint(SpritePoint(j.r.sprite, tl));
        }
        public SpriteRenderer Renderer(string part) => byName.TryGetValue(part, out var j) ? j.r : null;

        // walk = 걸음 위상 (라디안), moving = 걷는 중, alt = 두 번째 그림, rot = 날아갈 때 회전 (라디안), sq = 통통 납작
        public void Apply(float walk, bool moving, bool alt, float scale, int face, float rot, float sq, int sortOrder, float shake = 0)
        {
            if (group) group.sortingOrder = sortOrder;
            float t = still ? 0 : Time.time;
            if (wasMoving && !moving) stopTime = t;
            wasMoving = moving;
            float h =Height * scale * 0.45f * World.U;
            var q = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            visual.localRotation = q;
            visual.localPosition = (Vector3)(Vector2)(q * new Vector3(0, -h, 0)) + new Vector3(0, h, 0);
            float s = unit * scale;
            visual.localScale = new Vector3(-face * s / Mathf.Sqrt(sq), s * sq, 1);
            if (necks.Count > 0) NeckIK(t, moving, alt);
            foreach (var j in joints)
            {
                var p = j.p;
                float a = p.angle, dx = 0, dy = 0;
                var sp = alt && p.alt ? p.alt : p.sprite;
                switch (p.move)
                {
                    case BossPartArt.Move.Walk: a += p.amp * Mathf.Sin(walk * p.speed + p.phase * Mathf.PI * 2) * (moving ? 1 : idleWalk); break;
                    case BossPartArt.Move.Bob: dy = p.amp / 100f * Mathf.Sin(t * 3 * p.speed + p.phase * Mathf.PI * 2); break;
                    case BossPartArt.Move.Sway: a += p.amp * Mathf.Sin(t * 3 * p.speed + p.phase * Mathf.PI * 2); break;
                    case BossPartArt.Move.Spin: a += t * 360 * p.speed; break;
                    case BossPartArt.Move.Recoil:
                        if (alt && !still)
                        {
                            // 사격: 발사마다 두 번째 그림(총구 불꽃) ↔ 보통 그림, 쏠 때 뒤로 밀렸다 돌아옴 + 총이 들썩
                            float f = Mathf.Repeat(t * recoilRate * p.speed + p.phase, 1);
                            sp = f < 0.5f && p.alt ? p.alt : p.sprite;
                            dx = p.amp / 100f * (1 - f);
                            a += Random.Range(-recoilTilt, recoilTilt) * (1 - f);
                        }
                        else { sp = p.sprite; dy = p.amp / 300f * Mathf.Sin(t * 3 * p.speed + p.phase * Mathf.PI * 2); }
                        break;
                    case BossPartArt.Move.Wiggle:
                        // 공격 중엔 사격(Recoil)과 같은 박자로 발사마다 툭 튀었다 돌아옴 · 걷다 멈춘 순간엔 한 번 출렁 (1/speed 초, 점점 작아짐)
                        if (alt && !still) a += p.amp * (1 - Mathf.Repeat(t * recoilRate + p.phase, 1));
                        else if (!moving)
                        {
                            float e = t - stopTime, d = 1 / Mathf.Max(0.01f, p.speed);
                            if (e >= 0 && e < d) a += p.amp * Mathf.Sin(Mathf.PI * 2 * e / d) * (1 - e / d);
                        }
                        break;
                    case BossPartArt.Move.Neck:      // 히드라 목: 각도는 NeckIK 가 미리 계산 (사슬 4개가 다 있을 때만)
                        if (necks.Count > 0) a = j.ik;
                        sp = j.ikAlt && p.alt ? p.alt : p.sprite;
                        break;
                    case BossPartArt.Move.Twitch:
                    {
                        // 몸에 박힌 머리: 가끔 몸부림 (펄스 동안 빠르게 떨고 살짝 튀어나옴, 그동안 두 번째 그림) · 공격 중엔 더 자주
                        float ph = p.phase * Mathf.PI * 2;
                        float pulse = still ? 0 : Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * 1.3f * p.speed + ph * 3)), 8) + (alt ? 0.5f : 0);
                        a += p.amp * Mathf.Sin(t * 22 + ph) * pulse;
                        dy = p.amp * 0.8f / 100f * pulse;
                        sp = pulse > 0.35f && p.alt ? p.alt : p.sprite;
                        break;
                    }
                }
                a += p.open * open;
                if (p.show != BossPartArt.Show.Always) { bool vis = (p.show == BossPartArt.Show.Open) == (open >= openSwap); if (j.r.enabled != vis) j.r.enabled = vis; }
                if (shake > 0) a += Random.Range(-shake, shake);
                j.j.localRotation = Quaternion.Euler(0, 0, a);
                j.j.localPosition = j.basePos + new Vector3(dx, dy, 0);      // 그림은 왼쪽을 봄 → dx + = 뒤로
                if (j.r.sprite != sp) { j.r.sprite = sp; j.r.transform.localPosition = -SpritePoint(sp, p.pivot); }
            }
        }
    }
}
