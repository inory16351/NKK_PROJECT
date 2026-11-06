using UnityEngine;
using UnityEngine.InputSystem;

namespace NKK.Lobby
{
    // 뒷골목 박쥐 (쥐 친구 95161, 도토리 상점에서 사면 나타남 · 2026-11-04 사용자).
    // 평소 = 천장에 거꾸로 매달려 살랑 · 누르면 떨어지며 몸을 뒤집고 뒷골목을 날아다니다가 제자리로 돌아와 다시 매달림.
    // 리그 = 자식 SpriteRenderer (Visual 아래 머리 · 몸통 · 펼친 날개 2 · 접은 날개 · 발, 조립 위치는 씬에서 정함). 그림은 왼쪽을 봄.
    // 매달림 = Visual 위아래 뒤집기 (발끝이 이 오브젝트 위치 = 천장).
    public class AlleyBat : MonoBehaviour
    {
        [Tooltip("살 때 나타나는 장식 id (도토리 테이블 Deco_Item)")] public int decoId = 95161;
        public LobbyHome home;
        public LobbyPan pan;
        [Tooltip("날아다닐 범위 (뒷골목 배경판)")] public SpriteRenderer area;

        [Header("리그 (Visual 자식)")]
        public Transform visual;
        public SpriteRenderer head, torso, wing, wingFar, wingFold, feet;

        [Header("매달림")]
        [Tooltip("몸통 가운데 → 거꾸로 매달린 발끝 거리 (리그 단위, 크기 1 기준)")] public float hangTop = 3.3f;
        [Tooltip("살랑 각도 · 빠르기")] public float swayAngle = 3f, swaySpeed = 1.3f;
        [Tooltip("누르는 범위 (몸통 가운데 기준, 게임 단위)")] public float hitRadius = 0.75f;

        [Header("날기")]
        [Tooltip("날아다니는 시간 (초) · 속도 (단위/초) · 방향 바꾸는 빠르기")] public float flyTime = 4.5f, flySpeed = 3.6f, steer = 3.5f;
        [Tooltip("날개 퍼덕임 (초당) · 위 각도 · 아래 각도 · 먼 날개 차이")] public float flapRate = 3.5f, flapUp = 20f, flapDown = -75f, farWingOffset = -10f;
        [Tooltip("날 때 발 각도 · 머리 각도")] public float flyFeetAngle = 35f, flyHeadAngle = -5f;
        [Tooltip("날 범위 여백 (배경판 가장자리에서 안쪽, 위 · 아래 · 옆, 게임 단위)")] public float marginTop = 2.6f, marginBottom = 3.6f, marginSide = 1.2f;
        [Tooltip("떨어지며 몸 뒤집는 시간 · 떨어지는 거리 · 돌아와 붙는 거리")] public float dropTime = 0.35f, dropDist = 0.8f, landDist = 0.15f;

        enum St { Hang, Drop, Fly, Return }
        St st = St.Hang;
        Vector3 hangLocal, pos, vel, target;
        float t, flap, scale = 1, face = 1;

        void Awake()
        {
            hangLocal = transform.localPosition;
            if (visual) scale = Mathf.Abs(visual.localScale.x);
        }

        bool Owned => Progress.I && Progress.I.OwnsDeco(decoId);
        // 상점 '사기 전 미리보기' 가 찍기 직전 · 직후 (같은 프레임): 보임 여부 · 매달린 자세만 바로 (ShopPreview)
        public void PreviewSync() { bool show = Owned; if (visual) visual.gameObject.SetActive(show); if (show && st == St.Hang) Hang(); }

        void Update()
        {
            bool show = Owned;
            if (visual && visual.gameObject.activeSelf != show) visual.gameObject.SetActive(show);
            if (!show) { st = St.Hang; transform.localPosition = hangLocal; return; }
            float dt = Time.deltaTime; t += dt;
            if (st == St.Hang) { Hang(); if (Clicked()) Begin(); return; }
            flap += dt * flapRate;
            switch (st)
            {
                case St.Drop:
                {
                    float k = Mathf.Clamp01(t / dropTime);
                    pos = HangPos + Vector3.down * dropDist * Mathf.SmoothStep(0, 1, k);
                    transform.position = pos;
                    Pose(true, Mathf.Lerp(-1, 1, Mathf.SmoothStep(0, 1, k)));
                    if (k >= 1) { st = St.Fly; t = 0; vel = Vector3.left * flySpeed * 0.5f; target = Wander(); }
                    break;
                }
                case St.Fly:
                    if (Vector2.Distance(pos, target) < 0.6f) target = Wander();
                    Steer(target, dt);
                    if (t > flyTime) { st = St.Return; t = 0; }
                    break;
                case St.Return:
                {
                    var spot = HangPos + Vector3.down * dropDist * 0.6f;
                    Steer(spot, dt);
                    if (Vector2.Distance(pos, spot) < landDist + flySpeed * dt || t > 6) { st = St.Hang; t = 0; face = 1; transform.position = HangPos; SfxManager.Play("ui_click"); }
                    break;
                }
            }
        }

        Vector3 HangPos => transform.parent ? transform.parent.TransformPoint(hangLocal) : hangLocal;

        void Hang()
        {
            transform.localPosition = hangLocal;
            Pose(false, -1);
            if (visual) visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * swaySpeed) * swayAngle);
        }

        void Begin() { st = St.Drop; t = 0; flap = 0; face = 1; if (visual) visual.localRotation = Quaternion.identity; SfxManager.Play("ui_open"); }

        void Steer(Vector3 to, float dt)
        {
            var want = (to - pos); want.z = 0; want = want.normalized * flySpeed;
            vel = Vector3.Lerp(vel, want, 1 - Mathf.Exp(-steer * dt));
            pos += vel * dt; transform.position = pos;
            if (Mathf.Abs(vel.x) > 0.3f) face = vel.x < 0 ? 1 : -1;      // 그림이 왼쪽을 봄
            Pose(true, 1);
            if (visual) visual.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(vel.y * 4f, -15, 15) * face);
        }

        // 날 곳: 배경판 안쪽 위쪽 공간에서 아무 데나
        Vector3 Wander()
        {
            if (!area) return HangPos + (Vector3)Random.insideUnitCircle * 3f;
            var b = area.bounds;
            return new Vector3(Random.Range(b.min.x + marginSide, b.max.x - marginSide), Random.Range(b.min.y + marginBottom, b.max.y - marginTop), pos.z);
        }

        // flip = Visual 위아래 (-1 매달림 ~ 1 바로). 날 때는 펼친 날개, 매달리면 접은 날개
        void Pose(bool flying, float flip)
        {
            if (!visual) return;
            bool open = flying && flip > 0;
            visual.localScale = new Vector3(scale * face, scale * (Mathf.Abs(flip) < 0.05f ? 0.05f * Mathf.Sign(flip + 1e-4f) : flip), 1);
            visual.localPosition = new Vector3(0, flip < 0 ? -hangTop * scale * -flip : 0, 0);
            if (!flying) visual.localPosition = new Vector3(0, -hangTop * scale, 0);
            if (wing) wing.enabled = open;
            if (wingFar) wingFar.enabled = open;
            if (wingFold) wingFold.enabled = !open;
            float f = (Mathf.Sin(flap * Mathf.PI * 2) + 1) * 0.5f;            // 0 = 위, 1 = 아래
            float a = Mathf.Lerp(flapUp, flapDown, f);
            if (wing) wing.transform.localRotation = Quaternion.Euler(0, 0, a);
            if (wingFar) wingFar.transform.localRotation = Quaternion.Euler(0, 0, a + farWingOffset);
            if (feet) feet.transform.localRotation = Quaternion.Euler(0, 0, open ? flyFeetAngle : 0);
            if (head) head.transform.localRotation = Quaternion.Euler(0, 0, open ? flyHeadAngle : 0);
        }

        bool Clicked()
        {
            var m = Mouse.current; if (m == null || !m.leftButton.wasPressedThisFrame) return false;
            if (!home || !home.cam || (pan && !pan.AtRoom) || GachaShow.Busy) return false;
            var blockedBy = home.main ? home.main.inputBlocked : home.inputBlocked;
            if (blockedBy != null && blockedBy()) return false;
            Vector2 w = home.cam.ScreenToWorldPoint(m.position.ReadValue());
            Vector2 c = torso ? (Vector2)torso.bounds.center : (Vector2)transform.position;
            return Vector2.Distance(w, c) < hitRadius;
        }
    }
}
