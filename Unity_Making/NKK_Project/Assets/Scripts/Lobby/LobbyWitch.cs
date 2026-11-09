using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NKK.Lobby
{
    // 아지트 마녀 생쥐 (쥐 친구 95163, 도토리 상점에서 사면 나타남 · 2026-11-04 사용자).
    // 평소 = 공중 제자리에서 둥둥 · 날 때 빗자루 끝에서 마법가루(반짝이 · 별)가 떨어짐.
    // 누르면 (2026-11-09 사용자 그림): 앞(왼쪽)으로 날아가 화면 밖으로 나감 →
    //   보통 = 오른쪽(뒤쪽)에서 작고 어둡게 나타나 점점 커지며 제자리로 /
    //   winkChance(5%) = 나타나는 순간부터 얼굴이 화면을 꽉 채운 크기로 오른쪽에서 들어와 윙크 → 작아지며 제자리로.
    // 화면 위치는 카메라 화면 비율(0~1, 왼쪽 아래 = 0)로 잡고, 마녀의 '눈' 을 그 자리에 맞춤 (웹 미리보기 preview/witch_click 과 같은 시간표).
    // 리그 = Visual 아래 SpriteRenderer (꼬리 · 망토 · 빗자루 · 몸 · 머리 · 모자 · 팔, 조립 위치는 씬에서 정함). 그림은 왼쪽을 봄.
    public class LobbyWitch : MonoBehaviour
    {
        [Tooltip("살 때 나타나는 장식 id (도토리 테이블 Deco_Item)")] public int decoId = 95163;
        public LobbyHome home;
        public LobbyPan pan;

        [Header("리그 (Visual 자식)")]
        public Transform visual;
        public Transform cape, tail, hat, head;

        [Header("둥둥 (평소)")]
        [Tooltip("위아래 높이 · 빠르기(초당) · 기울기 각도")] public float bobHeight = 0.12f, bobSpeed = 0.8f, bobTilt = 3f;
        [Tooltip("망토 · 꼬리 · 모자 · 머리 흔들림 각도")] public float capeSwing = 7f, tailSwing = 10f, hatSwing = 3f, headSwing = 2f;
        [Tooltip("평소 흔들림 빠르기 (초당) · 날 때 빠르기")] public float flapSlow = 1.2f, flapFast = 3f;
        [Tooltip("누르는 범위 (이 오브젝트 위치 기준 반지름, 게임 단위)")] public float hitRadius = 1.1f;
        [Tooltip("누르는 범위 중심 보정")] public Vector2 hitOffset;

        [Header("클릭 → 화면 밖으로 나갔다 돌아오기")]
        [Tooltip("윙크 확률 (0~1)")] [Range(0, 1)] public float winkChance = 0.05f;
        [Tooltip("나가는 시간 · 화면 밖에 있는 시간 (보통 · 윙크)")] public float exitTime = 1.3f, offTime = 0.3f, offTimeWink = 0.6f;
        [Tooltip("나가는 곳 (눈 자리, 카메라 화면 비율) · 나갈 때 위로 뜨는 높이 (화면 높이 비율)")] public Vector2 exitAt = new(-0.1625f, 0.6667f);
        public float exitArc = 0.078f;
        [Tooltip("보통: 돌아오는 시간 · 나타나는 곳 · 처음 크기 · 처음 어둡기 · 아래로 처지는 높이")] public float returnTime = 1.8f;
        public Vector2 returnFrom = new(1.1f, 0.5333f);
        public float returnScale = 0.55f, returnDim = 0.35f, returnArc = 0.044f;

        [Header("윙크 (클로즈업)")]
        [Tooltip("클로즈업 크기 (평소 대비) · 눈 자리 (카메라 화면 비율)")] public float closeScale = 14f;
        public Vector2 closeAt = new(0.375f, 0.5111f);
        [Tooltip("들어오기 시작하는 가로 자리 (화면 비율, 얼굴이 다 숨는 곳)")] public float winkFromX = 1.45f;
        [Tooltip("들어오는 시간 · 윙크 시간 · 제자리로 돌아가는 시간")] public float winkEnterTime = 1.25f, winkHoldTime = 1.3f, winkBackTime = 1.15f;
        [Tooltip("감은 눈 그림 (머리 자식, 가운데 = 눈) · 반짝이 (머리 자식, 돎)")] public SpriteRenderer winkEye, winkSpark;
        [Tooltip("반짝이 도는 빠르기 (도/초)")] public float sparkSpin = 90f;
        [Tooltip("클로즈업 동안 그리는 순서를 올리는 값 (아지트 말풍선 9000 · 글자 9001 보다 앞으로)")] public int closeOrderBoost = 2200;

        [Header("마법가루 (날 때 빗자루 끝에서)")]
        [Tooltip("가루 템플릿 (꺼져 있는 SpriteRenderer)")] public SpriteRenderer dustTemplate;
        [Tooltip("나오는 곳 (Visual 자식 = 빗자루 끝)")] public Transform dustFrom;
        [Tooltip("가루 그림 (흰색 → 색을 입힘)")] public Sprite[] dustSprites;
        [Tooltip("가루 색 (아무거나)")] public Color[] dustColors = { new(1f, 0.92f, 0.55f), new(0.85f, 0.75f, 1f), new(1f, 0.75f, 0.9f), new(1f, 1f, 1f) };
        [Tooltip("날 때 초당 개수 · 평소 초당 개수")] public float dustRate = 34f, dustIdleRate = 2f;
        [Tooltip("살아 있는 시간 (초) 범위")] public Vector2 dustLife = new(0.9f, 1.5f);
        [Tooltip("크기 범위 (게임 단위 폭)")] public Vector2 dustSize = new(0.16f, 0.32f);
        [Tooltip("떨어지는 가속 (단위/초²) · 처음 옆 흩어짐 · 처음 아래 속도")] public float dustGravity = 1.6f, dustSpread = 0.5f, dustFall = 0.3f;
        [Tooltip("그리는 순서 (마녀 리그보다 뒤)")] public int dustOrder = 7090;

        enum St { Idle, Fly }
        St st = St.Idle;
        bool wink;
        Vector3 homeLocal, eyeOff, lastPos;
        float t, life, flap, scale = 1, faceCur = 1, faceWant = 1, dustAcc;
        Quaternion capeR, tailR, hatR, headR;
        readonly List<(SpriteRenderer r, Color c)> tints = new();
        readonly List<(SpriteRenderer r, int o)> orders = new();
        bool boosted;

        class Dust { public SpriteRenderer r; public Vector3 v; public float age, life, size, spin; }
        readonly List<Dust> dust = new(), pool = new();

        void Awake()
        {
            homeLocal = transform.localPosition;
            if (visual) scale = Mathf.Abs(visual.localScale.x);
            if (cape) capeR = cape.localRotation;
            if (tail) tailR = tail.localRotation;
            if (hat) hatR = hat.localRotation;
            if (head) headR = head.localRotation;
            if (dustTemplate) dustTemplate.gameObject.SetActive(false);
            // 눈 자리 (이 오브젝트 기준, 평소 크기) — 화면 위치를 눈에 맞출 때 씀
            eyeOff = winkEye ? winkEye.transform.position - transform.position : Vector3.zero;
            if (visual) foreach (var r in visual.GetComponentsInChildren<SpriteRenderer>(true)) { if (r != winkEye && r != winkSpark) tints.Add((r, r.color)); orders.Add((r, r.sortingOrder)); }
            SetWink(0);
        }

        bool Owned => Progress.I && Progress.I.OwnsDeco(decoId);
        // 상점 '사기 전 미리보기' 가 찍기 직전 · 직후 (같은 프레임): 보임 여부만 바로 (ShopPreview)
        public void PreviewSync() { if (visual) visual.gameObject.SetActive(Owned); }

        void Update()
        {
            bool show = Owned;
            if (visual && visual.gameObject.activeSelf != show) visual.gameObject.SetActive(show);
            float dt = Time.deltaTime;
            UpdateDust(dt);
            if (!show) { if (st != St.Idle) EndFly(); transform.localPosition = homeLocal; return; }
            t += dt; life += dt;
            flap += dt * (st == St.Fly ? flapFast : flapSlow);

            float bob = Mathf.Sin(life * bobSpeed * Mathf.PI * 2) * bobHeight;
            float tilt = Mathf.Sin(life * bobSpeed * Mathf.PI * 2) * bobTilt;
            float m = 1, dim = 0, lid = 0;
            bool hidden = false;
            faceWant = 1;          // 이 연출은 늘 왼쪽을 봄 (웹 미리보기와 같게)
            if (st == St.Idle)
            {
                transform.localPosition = homeLocal + new Vector3(0, bob, 0);
                if (Clicked()) { st = St.Fly; t = 0; wink = Random.value < winkChance; SfxManager.Play("ui_open"); }
            }
            if (st == St.Fly && !Fly(ref m, ref dim, ref lid, ref tilt, ref hidden)) EndFly();

            faceCur = Mathf.MoveTowards(faceCur, faceWant, dt * 2f);
            if (visual)
            {
                visual.localScale = new Vector3(scale * faceCur * m, scale * m, 1);
                visual.localRotation = Quaternion.Euler(0, 0, tilt);
            }
            for (int i = 0; i < tints.Count; i++) { var (r, c) = tints[i]; if (!r) continue; float k = 1 - dim; r.color = new Color(c.r * k, c.g * k, c.b * k, c.a); }
            SetWink(lid);
            Boost(m > 1.5f);          // 클로즈업 = 아지트 말풍선보다 앞
            if (winkSpark) winkSpark.transform.localRotation = Quaternion.Euler(0, 0, -life * sparkSpin);

            float w = flap * Mathf.PI * 2;
            if (cape) cape.localRotation = capeR * Quaternion.Euler(0, 0, Mathf.Sin(w * 2) * capeSwing);
            if (tail) tail.localRotation = tailR * Quaternion.Euler(0, 0, Mathf.Sin(w) * tailSwing);
            if (hat) hat.localRotation = hatR * Quaternion.Euler(0, 0, Mathf.Sin(w + 1.2f) * hatSwing);
            if (head) head.localRotation = headR * Quaternion.Euler(0, 0, Mathf.Sin(w + 0.6f) * headSwing);

            // 마법가루 (화면 밖 · 클로즈업 땐 안 뿌림)
            var vel = dt > 0 ? (transform.position - lastPos) / dt : Vector3.zero; lastPos = transform.position;
            if (!hidden && m < 2)
            {
                dustAcc += dt * (st == St.Fly ? dustRate : dustIdleRate);
                while (dustAcc >= 1) { dustAcc -= 1; Spawn(vel); }
            }
        }

        void EndFly() { st = St.Idle; t = 0; wink = false; transform.localPosition = homeLocal; SetWink(0); Boost(false); }

        void Boost(bool on)
        {
            if (on == boosted) return;
            boosted = on;
            foreach (var (r, o) in orders) if (r) r.sortingOrder = o + (on ? closeOrderBoost : 0);
        }

        // ── 시간표 (웹 미리보기 normal() · wink() 와 같음). 끝나면 false ──
        bool Fly(ref float m, ref float dim, ref float lid, ref float tilt, ref bool hidden)
        {
            var cam = home ? home.cam : null;
            if (!cam) return false;
            Vector3 homeEye = (transform.parent ? transform.parent.TransformPoint(homeLocal) : homeLocal) + eyeOff;
            float H = cam.orthographicSize * 2;          // 화면 높이 (게임 단위)
            Vector3 VP(Vector2 v) { var p = cam.ViewportToWorldPoint(new Vector3(v.x, v.y, 0)); p.z = homeEye.z; return p; }
            Vector3 eye;
            float te = exitTime, to = wink ? offTimeWink : offTime;
            if (t < te)
            {
                float k = Ease(t / te);
                eye = Vector3.Lerp(homeEye, VP(exitAt), k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * exitArc * H);
                tilt = 6 * Mathf.Sin(k * Mathf.PI);
            }
            else if (t < te + to) { hidden = true; eye = VP(exitAt) + Vector3.left * 20; }
            else if (!wink)
            {
                float k = Ease((t - te - to) / returnTime);
                if (k >= 1) return false;
                eye = Vector3.Lerp(VP(returnFrom), homeEye, k) - Vector3.up * (Mathf.Sin(k * Mathf.PI) * returnArc * H);
                m = Mathf.Lerp(returnScale, 1, k); dim = returnDim * (1 - k);
                tilt = -4 * Mathf.Sin(k * Mathf.PI);
            }
            else
            {
                float tw = t - te - to;
                Vector3 close = VP(closeAt);
                if (tw < winkEnterTime)
                {
                    // 나타나는 순간부터 클로즈업 크기: 오른쪽 화면 밖에서 쑥 미끄러져 들어옴
                    float k = tw / winkEnterTime, e = 1 - Mathf.Pow(1 - k, 3);
                    eye = Vector3.Lerp(VP(new Vector2(winkFromX, closeAt.y)), close, e) - Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.02f * H);
                    m = closeScale; tilt = Mathf.Lerp(4, 0, e);
                }
                else if (tw < winkEnterTime + winkHoldTime)
                {
                    float k = (tw - winkEnterTime) / winkHoldTime;
                    lid = k < 0.15f ? 0 : k < 0.25f ? (k - 0.15f) / 0.1f : k < 0.8f ? 1 : k < 0.9f ? 1 - (k - 0.8f) / 0.1f : 0;
                    eye = close - Vector3.up * (Mathf.Sin(k * Mathf.PI * 2) * 0.0067f * H);
                    m = closeScale * (1 + 0.02f * Mathf.Sin(k * Mathf.PI)); tilt = 0;
                }
                else
                {
                    float k = Ease((tw - winkEnterTime - winkHoldTime) / winkBackTime);
                    if (k >= 1) return false;
                    eye = Vector3.Lerp(close, homeEye, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.067f * H);
                    m = Mathf.Exp(Mathf.Lerp(Mathf.Log(closeScale), 0, k)); tilt = -6 * Mathf.Sin(k * Mathf.PI);
                }
            }
            // 눈이 그 자리에 오도록 (눈 자리는 크기만큼 멀어짐)
            transform.position = eye - eyeOff * m;
            return true;
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return x < 0.5f ? 2 * x * x : 1 - Mathf.Pow(-2 * x + 2, 2) / 2; }

        void SetWink(float a)
        {
            if (winkEye) { winkEye.enabled = a > 0.001f; var c = winkEye.color; c.a = a; winkEye.color = c; }
            if (winkSpark) { winkSpark.enabled = a > 0.001f; var c = winkSpark.color; c.a = a; winkSpark.color = c; }
        }

        void Spawn(Vector3 vel)
        {
            if (!dustTemplate || dustSprites == null || dustSprites.Length == 0) return;
            Dust d;
            if (pool.Count > 0) { d = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1); }
            else { var r = Instantiate(dustTemplate, dustTemplate.transform.parent); d = new Dust { r = r }; }
            var from = dustFrom ? dustFrom.position : transform.position;
            d.r.gameObject.SetActive(true);
            d.r.transform.position = from + (Vector3)(Random.insideUnitCircle * 0.12f);
            d.r.sprite = dustSprites[Random.Range(0, dustSprites.Length)];
            var c = dustColors.Length > 0 ? dustColors[Random.Range(0, dustColors.Length)] : Color.white;
            d.r.color = c; d.r.sortingOrder = dustOrder;
            d.v = new Vector3(Random.Range(-dustSpread, dustSpread) - vel.x * 0.15f, -dustFall * Random.Range(0.5f, 1.5f), 0);
            d.age = 0; d.life = Random.Range(dustLife.x, dustLife.y); d.size = Random.Range(dustSize.x, dustSize.y); d.spin = Random.Range(-180f, 180f);
            dust.Add(d);
        }

        void UpdateDust(float dt)
        {
            for (int i = dust.Count - 1; i >= 0; i--)
            {
                var d = dust[i]; d.age += dt;
                float k = d.age / d.life;
                if (k >= 1) { d.r.gameObject.SetActive(false); dust.RemoveAt(i); pool.Add(d); continue; }
                d.v.y -= dustGravity * dt; d.v.x *= 1 - dt * 1.5f;
                var tr = d.r.transform; tr.position += d.v * dt; tr.Rotate(0, 0, d.spin * dt);
                float sw = d.r.sprite ? d.r.sprite.bounds.size.x : 1;
                float s = d.size / Mathf.Max(0.01f, sw) * (k < 0.15f ? k / 0.15f : 1 - (k - 0.15f) / 0.85f * 0.6f);
                tr.localScale = new Vector3(s, s, 1);
                var c = d.r.color; c.a = k < 0.6f ? 1 : 1 - (k - 0.6f) / 0.4f; d.r.color = c;
            }
        }

        bool Clicked()
        {
            var m = Mouse.current; if (m == null || !m.leftButton.wasPressedThisFrame) return false;
            if (!home || !home.cam || (pan && pan.AtRoom) || GachaShow.Busy) return false;
            var blockedBy = home.main ? home.main.inputBlocked : home.inputBlocked;
            if (blockedBy != null && blockedBy()) return false;
            Vector2 wpos = home.cam.ScreenToWorldPoint(m.position.ReadValue());
            return Vector2.Distance(wpos, (Vector2)transform.position + hitOffset) < hitRadius;
        }
    }
}
