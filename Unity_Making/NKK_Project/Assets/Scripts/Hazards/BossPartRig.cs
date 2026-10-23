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
        [HideInInspector] public bool still;      // 미리보기용: 시간에 따른 움직임 끔 (BossRigPreview)
        [HideInInspector] public float open;      // 머리 열림 0~1 (Boss 가 매 프레임 정함, 우주복 쥐 — 웹 미리보기 BossRework/rig_web/crewmate.html)

        class J { public BossPartArt.Part p; public Transform j; public SpriteRenderer r; public Vector3 basePos; }
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
            joints.Clear(); byName.Clear(); open = 0;
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
        }

        // 파츠의 한 점 (그림 좌상단 기준 0~1) 월드 위치. Apply 뒤에 부름 (발사 위치·손 등)
        public Vector3 Point(string part, Vector2 tl)
        {
            if (!byName.TryGetValue(part, out var j) || !j.r.sprite) return transform.position;
            return j.r.transform.TransformPoint(SpritePoint(j.r.sprite, tl));
        }
        public SpriteRenderer Renderer(string part) => byName.TryGetValue(part, out var j) ? j.r : null;

        // 파츠 원래 색 (먼 쪽 = farColor): Boss 가 실루엣·금빛·투명을 칠한 뒤 되돌리는 기준. 렌더러의 지금 색을 읽으면 칠해 둔 어두운 색이 기준이 될 수 있음
        public void BaseColors(List<(SpriteRenderer r, Color c)> into)
        {
            foreach (var j in joints) if (j.r) into.Add((j.r, j.p.far ? farColor : Color.white));
        }

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
