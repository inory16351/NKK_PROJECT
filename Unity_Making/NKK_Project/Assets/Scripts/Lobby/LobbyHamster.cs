using UnityEngine;
using UnityEngine.InputSystem;

namespace NKK.Lobby
{
    // 아지트 햄스터 = 쥐 친구 '앙기뮤띠쥐' (95162, 도토리 상점에서 사면 나타남 · 2026-11-05 사용자).
    // 평소 = 무표정으로 앉아 숨 쉬며 가끔 눈 깜빡 · 누르면 움찔 → 폴짝 뛰며 활짝 웃고 따봉 (actTime 초) → 다시 무표정.
    // 그림 = Codex 로 그린 V2 파츠 (Art/Rats/RatFriends/Hamster). 조립 · 움직임 = 웹 미리보기 (UnityResources/.../Hamster/Sheets/web_template.html) 와 같음.
    // 리그는 Awake 에서 Visual 아래에 만들고, 관절 위치는 몸 그림 좌상단 기준 0~1 (아래 값). 이 오브젝트 위치 = 발끝 (바닥).
    public class LobbyHamster : MonoBehaviour
    {
        [Tooltip("살 때 나타나는 장식 id (도토리 테이블 Deco_Item)")] public int decoId = 95162;
        public LobbyHome home;
        public LobbyPan pan;

        [Header("그림 (피벗 = 관절)")]
        public Sprite body, headNeutral, headBlink, headHappy, arm, armThumb, foot, tail;

        [Header("크기 · 그리는 순서")]
        [Tooltip("발끝 → 머리 꼭대기 키 (게임 단위)")] public float height = 1.05f;
        [Tooltip("그리는 순서 시작값 (0 = 쥐처럼 높이로 정함: 1000 + 화면 위치 × 2000)")] public int baseOrder = 0;

        [Header("조립 (몸 그림 좌상단 기준 0~1)")]
        public Vector2 neck = new(0.5f, 0.33f);
        public Vector2 shoulderL = new(0.22f, 0.24f), shoulderR = new(0.78f, 0.24f);
        public Vector2 footL = new(0.26f, 0.86f), footR = new(0.74f, 0.86f), tailAt = new(0.80f, 0.86f);
        [Tooltip("몸 대비 크기: 머리 · 팔 · 발 · 따봉 팔")] public float headK = 0.92f, armK = 0.55f, footK = 0.5f, thumbK = 0.6f;
        [Tooltip("따봉 팔 각도: 시작 (내린 상태) → 끝")] public float thumbAng0 = -80f, thumbAng1 = 0f;

        [Header("움직임")]
        [Tooltip("따봉 전체 시간 (초)")] public float actTime = 2.2f;
        [Tooltip("폴짝 높이 (몸 그림 픽셀)")] public float hopPx = 34f;
        [Tooltip("눈 깜빡 주기 (초)")] public float blinkEvery = 3.7f;
        [Tooltip("누르는 범위 여유 (배)")] public float hitPad = 1.0f;

        Transform visual, tTail, tFootL, tFootR, tBody, tArmL, tArmR, tThumb, tHead;
        SpriteRenderer rArmR, rThumb, rHead;
        float life, clickAt = -99, rigH, footToCenter, bw, bh, scale;

        void Awake()
        {
            if (!body) { enabled = false; return; }
            bw = body.rect.width; bh = body.rect.height;
            float fh = foot ? foot.rect.height : 0, hh = headNeutral ? headNeutral.rect.height : 0;
            footToCenter = (footL.y - 0.5f) * bh + fh * footK * 0.96f;          // 몸 중심 → 발끝 (픽셀, 아래로)
            float headTop = (neck.y - 0.5f) * bh - hh * headK * 0.96f;            // 몸 중심 → 머리 꼭대기 (픽셀, 위로 = 음수)
            rigH = footToCenter - headTop;
            scale = height / (rigH / 100f);

            visual = new GameObject("Visual").transform; visual.SetParent(transform, false);
            int order = baseOrder != 0 ? baseOrder : home ? 1000 + Mathf.RoundToInt(home.ToUV(transform.position).y * 2000) : 2500;
            tTail = Part("tail", tail, order, out _);
            tFootL = Part("footL", foot, order + 1, out _);
            tFootR = Part("footR", foot, order + 2, out _);
            tBody = Part("body", body, order + 3, out _);
            tArmL = Part("armL", arm, order + 4, out _);
            tArmR = Part("armR", arm, order + 5, out rArmR);
            tThumb = Part("thumb", armThumb, order + 5, out rThumb);
            tHead = Part("head", headNeutral, order + 6, out rHead);
            Update();
        }

        Transform Part(string n, Sprite s, int order, out SpriteRenderer r)
        {
            var go = new GameObject(n); go.transform.SetParent(visual, false);
            r = go.AddComponent<SpriteRenderer>(); r.sprite = s; r.sortingOrder = order;
            return go.transform;
        }

        bool Owned => Progress.I && Progress.I.OwnsDeco(decoId);

        // 몸 그림 위 한 점 (좌상단 0~1) → Visual 안 좌표 (몸 중심 기준, 몸 기울기 ang)
        Vector3 At(Vector2 a, float ang) => Quaternion.Euler(0, 0, ang) * new Vector3((a.x - 0.5f) * bw, -(a.y - 0.5f) * bh, 0) / 100f;

        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
        static float Back(float t) { t = Mathf.Clamp01(t); const float c = 2.2f; return 1 + (c + 1) * Mathf.Pow(t - 1, 3) + c * Mathf.Pow(t - 1, 2); }

        static void Set(Transform t, Vector3 p, float ang, float k, bool flip = false)
        {
            t.localPosition = p; t.localRotation = Quaternion.Euler(0, 0, ang); t.localScale = new Vector3(flip ? -k : k, k, 1);
        }

        void Update()
        {
            if (!visual) return;
            bool show = Owned;
            if (visual.gameObject.activeSelf != show) visual.gameObject.SetActive(show);
            if (!show) { clickAt = -99; return; }
            float dt = Time.deltaTime; life += dt;
            if (Clicked()) { clickAt = life; SfxManager.Play("ui_click"); }

            float ta = life - clickAt; bool act = ta >= 0 && ta < actTime;
            float t = life, breathe = Mathf.Sin(t * Mathf.PI * 2 * 0.6f);
            float squash = 0, hop = 0, thumb = 0; bool happy = false;
            if (act)
            {
                squash = ta < 0.12f ? Ease(ta / 0.12f) : ta < 0.3f ? 1 - Ease((ta - 0.12f) / 0.18f) : 0;                 // 움찔
                hop = ta > 0.12f && ta < 0.55f ? Mathf.Sin((ta - 0.12f) / 0.43f * Mathf.PI) * hopPx : 0;                // 폴짝
                thumb = ta < 0.15f ? 0 : ta < actTime - 0.35f ? Back((ta - 0.15f) / 0.3f) : 1 - Ease((ta - (actTime - 0.35f)) / 0.3f);
                happy = ta > 0.1f && ta < actTime - 0.2f;
            }
            // Visual = 몸 중심 (발끝에서 위로), 움찔하면 납작 · 숨쉬기
            float sy = 1 - 0.1f * squash + 0.012f * breathe, sx = 1 + 0.08f * squash;
            visual.localPosition = new Vector3(0, footToCenter / 100f * scale, 0);
            visual.localScale = new Vector3(scale * sx, scale * sy, 1);
            var c = new Vector3(0, hop / 100f, 0);
            float aBody = act ? 0 : 2.5f * Mathf.Sin(t * Mathf.PI * 2 * 0.35f);

            Set(tTail, c + At(tailAt, aBody), aBody + 8 * Mathf.Sin(t * Mathf.PI * 2 * 1.5f), 1);
            Set(tFootL, c + At(footL, aBody), -6, footK);
            Set(tFootR, c + At(footR, aBody), 6, footK, true);
            Set(tBody, c, aBody, 1);
            float wig = 4 * Mathf.Sin(t * Mathf.PI * 2 * 1.2f);
            // 그림의 팔 = 화면 오른쪽 팔 → 왼쪽은 좌우 반전 (피벗이 거울 위치로 감)
            Set(tArmL, c + At(shoulderL, aBody), aBody - wig, armK, true);
            var shR = c + At(shoulderR, aBody);
            bool up = thumb > 0.05f;
            rArmR.enabled = !up; rThumb.enabled = up;
            if (up)
            {
                float a = thumbAng0 + (thumbAng1 - thumbAng0) * thumb + (thumb > 0.9f ? 4 * Mathf.Sin(ta * Mathf.PI * 2 * 3) : 0);
                Set(tThumb, shR, aBody + a, thumbK);
            }
            else Set(tArmR, shR, aBody + wig, armK);
            bool blink = !act && (t % blinkEvery) > blinkEvery - 0.15f;
            rHead.sprite = happy ? headHappy : blink ? headBlink : headNeutral;
            float tilt = act ? 6 * thumb : 3 * Mathf.Sin(t * Mathf.PI * 2 * 0.5f);
            Set(tHead, c + At(neck, aBody) + new Vector3(0, 1.5f * breathe / 100f, 0), aBody * 1.4f + tilt, headK);
        }

        bool Clicked()
        {
            var m = Mouse.current; if (m == null || !m.leftButton.wasPressedThisFrame) return false;
            if (life - clickAt < actTime) return false;
            if (!home || !home.cam || (pan && pan.AtRoom) || GachaShow.Busy) return false;
            var blockedBy = home.main ? home.main.inputBlocked : home.inputBlocked;
            if (blockedBy != null && blockedBy()) return false;
            Vector2 w = home.cam.ScreenToWorldPoint(m.position.ReadValue());
            Vector2 p = transform.position;
            float hw = bw * 1.1f / 100f * scale * 0.5f * hitPad, hgt = height * hitPad;
            return w.x > p.x - hw && w.x < p.x + hw && w.y > p.y && w.y < p.y + hgt;
        }
    }
}
