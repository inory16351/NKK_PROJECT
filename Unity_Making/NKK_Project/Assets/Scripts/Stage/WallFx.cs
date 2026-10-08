using System.Collections.Generic;
using UnityEngine;

namespace NKK.Stage
{
    // 벽 무너짐 연출 (Game 씬 WallFx): 영화 속 벽 폭파처럼 — 벽 선을 따라 폭발이 연달아 터지며 벽 잔해·벽돌이 새로 열리는 방 쪽으로 부채꼴로 쏟아져 날아감
    // (큰 콘크리트 덩어리는 굴러감) → 바닥에서 통통 → 사라짐. 빠른 잔해 뒤엔 흙먼지 꼬리(방사형 줄기), 연기 기둥이 열리는 방 쪽으로 뿜어져 부풀어 오름.
    // 충격파·역경직·흔들림. (참고: 사용자가 준 폭발 사진 — 가운데 불꽃 + 방사형 잔해 줄기 + 바깥으로 뿜는 연기)
    // 파편 = 자식 템플릿(debrisTemplate, 꺼져 있음)을 복제해 씀. 그림 = UnityResources/Rats/FX_Wall (흰색 → 벽 색으로 칠함)
    public class WallFx : MonoBehaviour
    {
        public static WallFx I { get; private set; }

        [Tooltip("파편 템플릿 (SpriteRenderer, 꺼 둠)")] public SpriteRenderer debrisTemplate;
        [Tooltip("파편 그림 (벽돌·콘크리트·잔돌)")] public Sprite[] debrisSprites;
        [Tooltip("먼지구름 그림")] public Sprite dustSprite;
        [Tooltip("벽 하나 무너질 때 파편 수 (계단 방 벽은 × stairsMul)")] public int debrisCount = 30;
        public float stairsMul = 1.6f;
        [Tooltip("파편 크기 (게임 단위, 최소·최대)")] public Vector2 debrisSize = new(26, 56);
        [Tooltip("발사 속도 (앞 · 위)")] public Vector2 debrisSpeed = new(1300, 420);
        [Tooltip("중력 (게임 단위/초²)")] public float gravity = 1100;
        [Tooltip("파편 수명 (초)")] public float debrisLife = 2.2f;
        [Tooltip("먼지구름 수 · 크기")] public int dustCount = 7; public float dustSize = 120;
        [Tooltip("파편 색 = 벽 윗면 색 × 이 값 (어두운 면 섞음)")] public float shadeMin = 0.75f;
        [Tooltip("파편 기본 색 (벽돌) — 벽 윗면 색과 brickMix 만큼 섞음 (밝은 바닥에 묻히지 않게)")] public Color brickColor = new(0.66f, 0.46f, 0.38f); [Range(0, 1)] public float brickMix = 0.65f;
        [Tooltip("무너질 때 화면 흔들림 (계단 방 벽은 × 2)")] public float shake = 0.25f;
        [Tooltip("날아가는 방향 퍼짐 (0 = 일직선, 1 = 옆으로 같은 만큼)")] public float spread = 0.75f;
        [Tooltip("큰 덩어리 수 · 크기 (게임 단위)")] public int slabCount = 5; public Vector2 slabSize = new(60, 95);
        [Tooltip("먼지구름이 밀려 나가는 속도")] public float dustPush = 260;
        [Header("연기 기둥 (열리는 방 쪽으로 뿜어짐)")]
        public int smokeCount = 6;
        [Tooltip("크기 (처음 → 끝)")] public Vector2 smokeSize = new(150, 320);
        [Tooltip("뿜는 속도 (최소·최대) — 공기 저항으로 점점 느려짐")] public Vector2 smokePush = new(450, 900);
        public float smokeLife = 1.8f, smokeDrag = 2.4f;
        public Color smokeColor = new(0.5f, 0.43f, 0.38f, 0.9f);
        [Header("잔해 꼬리 (흙먼지 줄기)")]
        [Tooltip("이 속도보다 빠른 잔해만 꼬리를 남김")] public float trailMinSpeed = 450;
        [Tooltip("꼬리 간격 (초) · 크기 · 수명")] public float trailGap = 0.03f; public Vector2 trailSize = new(14, 30); public float trailLife = 0.5f;
        public Color trailColor = new(0.55f, 0.45f, 0.38f, 0.7f);
        [Tooltip("벽 선을 따라 터지는 폭발 수 · 간격(초) · 크기")] public int blastCount = 5; public float blastGap = 0.05f, blastScale = 2.2f;
        [Tooltip("무너질 때 역경직 (초)")] public float hitstop = 0.07f;
        [Tooltip("무너질 때 글자 (비우면 안 띄움)")] public string[] breakWords = { "와르르!!", "콰광!!", "쿠르릉!!" };

        class Piece { public SpriteRenderer r; public float x, y, z, vx, vy, vz, rot, vr, life, max, size, size1, delay, trailT, drag; public bool dust; }
        readonly List<Piece> live = new(), born = new();
        readonly Stack<SpriteRenderer> pool = new();

        void Awake() { I = this; if (debrisTemplate) debrisTemplate.gameObject.SetActive(false); }
        void OnDestroy() { if (I == this) I = null; }

        SpriteRenderer Get()
        {
            var r = pool.Count > 0 ? pool.Pop() : Instantiate(debrisTemplate, debrisTemplate.transform.parent);
            r.gameObject.SetActive(true); return r;
        }

        // 벽 (x0,y0)~(x1,y1) 선이 무너짐. col = 벽 윗면 색, h = 벽 높이(게임 단위, 파편이 이 높이에서 떨어짐)
        public void Break(float x0, float y0, float x1, float y1, Color col, float h, bool stairs) => Break(x0, y0, x1, y1, col, h, stairs, 0, 0);
        // dirX, dirY = 새로 열리는 방 쪽 (0,0 이면 양쪽으로)
        public void Break(float x0, float y0, float x1, float y1, Color col, float h, bool stairs, float dirX, float dirY)
        {
            if (!debrisTemplate || debrisSprites == null || debrisSprites.Length == 0) return;
            int n = Mathf.RoundToInt(debrisCount * (stairs ? stairsMul : 1));
            col = Color.Lerp(col, brickColor, brickMix);
            float nx = -(y1 - y0), ny = x1 - x0, L = Mathf.Max(1, Mathf.Sqrt(nx * nx + ny * ny)); nx /= L; ny /= L;   // 벽에 수직
            bool aimed = dirX != 0 || dirY != 0;
            if (aimed) { nx = dirX; ny = dirY; }
            float tx = -ny, ty = nx;                                                                                    // 벽 따라
            for (int i = 0; i < n; i++)
            {
                float t = Random.value, side = aimed ? (Random.value < 0.88f ? 1 : -0.35f) : (Random.value < 0.5f ? 1 : -1), delay = Mathf.Floor(t * blastCount) * blastGap;
                bool slab = i < slabCount;
                var r = Get(); r.sprite = slab && debrisSprites.Length > 2 ? debrisSprites[2] : debrisSprites[Random.Range(0, debrisSprites.Length)];
                float k = Random.Range(shadeMin, 1f); r.color = new Color(col.r * k, col.g * k, col.b * k, 1);
                float sp = Random.Range(0.45f, 1f) * debrisSpeed.x * (slab ? 0.55f : 1), sd = Random.Range(-spread, spread) * sp;
                live.Add(new Piece
                {
                    r = r, x = Mathf.Lerp(x0, x1, t), y = Mathf.Lerp(y0, y1, t), z = Random.Range(0.3f, 1f) * h,
                    vx = nx * side * sp + tx * sd, vy = ny * side * sp + ty * sd, vz = Random.Range(0.35f, 1f) * debrisSpeed.y * (slab ? 0.6f : 1),
                    rot = Random.Range(0, 360f), vr = Random.Range(-900f, 900f) * (slab ? 0.4f : 1), life = debrisLife * Random.Range(0.8f, 1.3f) * (slab ? 1.4f : 1), size = slab ? Random.Range(slabSize.x, slabSize.y) : Random.Range(debrisSize.x, debrisSize.y), delay = delay
                });
                live[^1].max = live[^1].life;
            }
            if (dustSprite)
                for (int i = 0; i < dustCount; i++)
                {
                    float t = (i + Random.value) / dustCount;
                    var r = Get(); r.sprite = dustSprite; r.color = new Color(Mathf.Lerp(col.r, 1, 0.6f), Mathf.Lerp(col.g, 1, 0.6f), Mathf.Lerp(col.b, 1, 0.6f), 0.85f);
                    float push = aimed ? Random.Range(0.4f, 1f) * dustPush : 0;
                    live.Add(new Piece { r = r, x = Mathf.Lerp(x0, x1, t), y = Mathf.Lerp(y0, y1, t), z = Random.Range(10f, h), vx = nx * push + Random.Range(-40f, 40f), vy = ny * push + Random.Range(-30f, 30f), vz = Random.Range(20f, 70f),
                        rot = Random.Range(0, 360f), vr = Random.Range(-40f, 40f), life = 1.1f, max = 1.1f, size = dustSize * Random.Range(0.7f, 1.2f), dust = true });
                }
            // 연기 기둥: 벽 가운데쯤에서 열리는 방 쪽 부채꼴로 크게 뿜어져 부풀어 오름
            if (dustSprite)
                for (int i = 0; i < smokeCount * (stairs ? 2 : 1); i++)
                {
                    float t = Random.Range(0.2f, 0.8f), a = Random.Range(-spread, spread), sp = Random.Range(smokePush.x, smokePush.y), sgn = aimed || Random.value < 0.5f ? 1 : -1;
                    float dx = sgn * nx + tx * a, dy = sgn * ny + ty * a, dl = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dy * dy));
                    var r = Get(); r.sprite = dustSprite; float k = Random.Range(0.85f, 1.1f);
                    r.color = new Color(smokeColor.r * k, smokeColor.g * k, smokeColor.b * k, smokeColor.a);
                    live.Add(new Piece { r = r, x = Mathf.Lerp(x0, x1, t), y = Mathf.Lerp(y0, y1, t), z = h * 0.5f, vx = dx / dl * sp, vy = dy / dl * sp, vz = Random.Range(30f, 120f),
                        rot = Random.Range(0, 360f), vr = Random.Range(-60f, 60f), life = smokeLife * Random.Range(0.8f, 1.2f), size = smokeSize.x * Random.Range(0.8f, 1.2f), size1 = smokeSize.y * Random.Range(0.8f, 1.2f),
                        drag = smokeDrag, dust = true, delay = Random.Range(0, blastGap * blastCount) });
                    live[^1].max = live[^1].life;
                }
            var fx = FxManager.I;
            if (fx)
            {
                // 벽 선을 따라 폭발이 연달아
                for (int b = 0; b < blastCount; b++)
                {
                    float t = (b + 0.5f) / blastCount, bx = Mathf.Lerp(x0, x1, t), by = Mathf.Lerp(y0, y1, t), bz = h * 0.5f;
                    if (b == 0) Blast(bx, by, bz); else StartCoroutine(Later(b * blastGap, bx, by, bz));
                }
                fx.Hitstop(hitstop * (stairs ? 1.5f : 1));
                fx.Ring((x0 + x1) / 2, (y0 + y1) / 2, Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 0.6f, new Color(1, 0.95f, 0.85f, 0.8f), 0.4f);
                fx.Shake(shake * (stairs ? 2 : 1));
                fx.Dust((x0 + x1) / 2, (y0 + y1) / 2, 10, 1.8f);
                if (breakWords != null && breakWords.Length > 0) fx.Popup((x0 + x1) / 2, (y0 + y1) / 2, breakWords[Random.Range(0, breakWords.Length)], Color.white, stairs ? 34 : 28, 1, h + 30);
            }
        }

        void Blast(float x, float y, float z)
        {
            var fx = FxManager.I; if (!fx) return;
            fx.Anim("explosion", x, y, z, blastScale * Random.Range(0.85f, 1.15f));
            fx.Burst(x, y, z, 6, new Color(1, 0.85f, 0.5f), Color.white, 200, 520);
        }
        System.Collections.IEnumerator Later(float t, float x, float y, float z) { yield return new WaitForSeconds(t); Blast(x, y, z); }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                if (p.delay > 0) { p.delay -= dt; p.r.enabled = false; continue; }      // 그 자리 폭발이 터질 때 출발
                p.r.enabled = true;
                p.life -= dt;
                if (p.life <= 0) { p.r.gameObject.SetActive(false); pool.Push(p.r); live.RemoveAt(i); continue; }
                if (p.dust)
                {
                    float d = Mathf.Max(0, 1 - dt * (p.drag > 0 ? p.drag : 2.5f)); p.vx *= d; p.vy *= d; p.x += p.vx * dt; p.y += p.vy * dt; p.z += p.vz * dt;
                    if (p.size1 > 0) p.size += (p.size1 - p.size) * Mathf.Min(1, dt * 2.2f); else p.size += dt * 90;
                }
                else
                {
                    p.vz -= gravity * dt; p.x += p.vx * dt; p.y += p.vy * dt; p.z += p.vz * dt;
                    if (p.z < 0) { p.z = 0; p.vz = -p.vz * 0.35f; p.vx *= 0.6f; p.vy *= 0.6f; p.vr *= 0.5f; }     // 바닥에서 통통
                    // 빠르게 날아가는 잔해 뒤에 흙먼지 꼬리 (방사형 줄기)
                    if (dustSprite && p.vx * p.vx + p.vy * p.vy > trailMinSpeed * trailMinSpeed && (p.trailT -= dt) <= 0)
                    {
                        p.trailT = trailGap;
                        var tr = Get(); tr.sprite = dustSprite; tr.color = trailColor;
                        float ts = Random.Range(trailSize.x, trailSize.y);
                        born.Add(new Piece { r = tr, x = p.x, y = p.y, z = p.z, rot = Random.Range(0, 360f), life = trailLife, max = trailLife, size = ts, size1 = ts * 2.2f, drag = 6, dust = true });
                    }
                }
                p.rot += p.vr * dt;
                var r = p.r;
                r.transform.position = World.ToUnity(p.x, p.y, p.z);
                r.transform.rotation = Quaternion.Euler(0, 0, p.rot);
                float s = p.size * World.U / Mathf.Max(0.01f, r.sprite.bounds.size.x);
                r.transform.localScale = new Vector3(s, s, 1);
                r.sortingOrder = World.SortOrder(p.y) + 2;
                var c = r.color; c.a = Mathf.Clamp01(p.life / Mathf.Min(0.4f, p.max)) * (p.dust ? 0.85f : 1); r.color = c;
            }
            if (born.Count > 0) { live.AddRange(born); born.Clear(); }
        }
    }
}
