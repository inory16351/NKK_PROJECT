using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NKK.Lobby
{
    // 아지트 마녀 생쥐 (쥐 친구 95163, 도토리 상점에서 사면 나타남 · 2026-11-04 사용자).
    // 평소 = 공중 제자리에서 둥둥 · 누르면 타원을 따라 한 바퀴 (제자리 = 타원 오른쪽 끝 → 위쪽 길로 왼쪽 → 아래쪽 길로 돌아옴),
    // 날 때 빗자루 끝에서 마법가루(반짝이 · 별)가 떨어짐. 방향은 움직이는 쪽을 보고, 바뀔 때 좌우로 납작해지며 돌아섬.
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

        [Header("클릭 → 타원 한 바퀴")]
        [Tooltip("타원 가로 · 세로 반지름 (게임 단위). 제자리 = 타원 오른쪽 끝")] public Vector2 loopRadius = new(4.0f, 0.85f);
        [Tooltip("타원 가운데 높이 보정 (제자리 기준)")] public float loopCenterY = 0f;
        [Tooltip("한 바퀴 시간 (초)")] public float loopTime = 3.6f;
        [Tooltip("돌아서는 빠르기 (초, 좌우 납작해졌다 펴짐)")] public float turnTime = 0.3f;
        [Tooltip("위아래로 움직일 때 기울기 (속도당 각도) · 최대")] public float tiltPerSpeed = 6f, tiltMax = 14f;
        [Tooltip("누르는 범위 (이 오브젝트 위치 기준 반지름, 게임 단위)")] public float hitRadius = 1.1f;
        [Tooltip("누르는 범위 중심 보정")] public Vector2 hitOffset;

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

        enum St { Idle, Loop }
        St st = St.Idle;
        Vector3 homeLocal;
        float t, life, flap, scale = 1, faceCur = 1, faceWant = 1, dustAcc;
        Quaternion capeR, tailR, hatR, headR;

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
        }

        bool Owned => Progress.I && Progress.I.OwnsDeco(decoId);

        void Update()
        {
            bool show = Owned;
            if (visual && visual.gameObject.activeSelf != show) visual.gameObject.SetActive(show);
            float dt = Time.deltaTime;
            UpdateDust(dt);
            if (!show) { st = St.Idle; transform.localPosition = homeLocal; return; }
            t += dt; life += dt;
            flap += dt * (st == St.Loop ? flapFast : flapSlow);

            float bob = Mathf.Sin(life * bobSpeed * Mathf.PI * 2) * bobHeight;
            Vector3 off = Vector3.zero, vel = Vector3.zero;
            float tilt = Mathf.Sin(life * bobSpeed * Mathf.PI * 2) * bobTilt;
            if (st == St.Idle)
            {
                faceWant = 1;
                if (Clicked()) { st = St.Loop; t = 0; SfxManager.Play("ui_open"); }
            }
            if (st == St.Loop)
            {
                float k = Mathf.Clamp01(t / loopTime);
                float th = Theta(k), th2 = Theta(Mathf.Min(1, k + 0.01f));
                off = LoopPos(th); vel = (LoopPos(th2) - off) / (loopTime * 0.01f);
                if (Mathf.Abs(vel.x) > 0.2f) faceWant = vel.x < 0 ? 1 : -1;
                tilt = Mathf.Clamp(vel.y * tiltPerSpeed, -tiltMax, tiltMax) * faceWant + 4f;
                bob *= 0.4f;
                if (k >= 1) { st = St.Idle; t = 0; off = Vector3.zero; }
            }
            transform.localPosition = homeLocal + off + new Vector3(0, bob, 0);

            faceCur = Mathf.MoveTowards(faceCur, faceWant, dt * 2f / Mathf.Max(0.05f, turnTime));
            if (visual)
            {
                float fx = Mathf.Abs(faceCur) < 0.06f ? 0.06f * Mathf.Sign(faceWant) : faceCur;
                visual.localScale = new Vector3(scale * fx, scale, 1);
                visual.localRotation = Quaternion.Euler(0, 0, tilt * Mathf.Sign(fx));
            }
            float w = flap * Mathf.PI * 2;
            if (cape) cape.localRotation = capeR * Quaternion.Euler(0, 0, Mathf.Sin(w * 2) * capeSwing);
            if (tail) tail.localRotation = tailR * Quaternion.Euler(0, 0, Mathf.Sin(w) * tailSwing);
            if (hat) hat.localRotation = hatR * Quaternion.Euler(0, 0, Mathf.Sin(w + 1.2f) * hatSwing);
            if (head) head.localRotation = headR * Quaternion.Euler(0, 0, Mathf.Sin(w + 0.6f) * headSwing);

            // 마법가루
            dustAcc += dt * (st == St.Loop ? dustRate : dustIdleRate);
            while (dustAcc >= 1) { dustAcc -= 1; Spawn(vel); }
        }

        // 한 바퀴 각도: 0 = 제자리(오른쪽 끝), 위쪽 길 먼저 (출발 · 도착은 부드럽게)
        static float Theta(float k) => Mathf.SmoothStep(0, 1, k) * Mathf.PI * 2;
        // 제자리 기준 타원 위치 (부모 = Home 좌표, 회전 없음 가정)
        Vector3 LoopPos(float th) => new Vector3(-loopRadius.x + loopRadius.x * Mathf.Cos(th), loopCenterY + loopRadius.y * Mathf.Sin(th), 0);

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
