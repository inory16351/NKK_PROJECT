using UnityEngine;

namespace NKK.Rats
{
    // 필살기가 붙잡은 쥐 (웹게임 r.ultOn · r.pose · r.sjRot · r.hideBody · r.jit) + 기절 별
    public partial class Rat
    {
        // ── 필살기 ──
        [HideInInspector] public bool UltOn;               // 필살기가 위치·자세를 정함 (AI 멈춤)
        public RatRig.Pose? UltPose;                       // null 이면 평소 자세
        [HideInInspector] public float UltRot, UltLift, UltSx = 1, UltSy = 1, UltJit;
        [HideInInspector] public bool HideBody;            // 몸 숨김 (변신·탈것 안 등)

        void UltTick(float dt)
        {
            stun = 0; tumbleT = 0; rushT = 0; noBreed = Mathf.Max(noBreed, 0.5f);
            Speed = Mathf.Sqrt(vx * vx + vy * vy);
            if (Mathf.Abs(vx) > 20) face = vx > 0 ? 1 : -1;
        }

        // 필살기 시작: 하던 것 멈춤
        public void UltGrab()
        {
            if (act.on) EndAction();
            Trick = TrickType.None; sleep = 0; vx = vy = 0; rushT = 0; stun = 0; tumbleT = 0;
            UltOn = true;
        }

        // 필살기 끝: 평소대로
        public void UltRelease()
        {
            UltOn = false; UltPose = null; UltRot = UltLift = UltJit = 0; UltSx = UltSy = 1; HideBody = false;
            if (ghost && temp <= 0) { ghost = false; foreach (var sr in rig.GetComponentsInChildren<SpriteRenderer>()) { var c = sr.color; c.a = 1; sr.color = c; } }   // 필살기 투명 되돌리기
            if (z > 0) vz = 0;
            if (rig && rig.visual) rig.visual.gameObject.SetActive(true);
            StopDash(0.3f, 0.6f);
        }

        void UltTransform(ref float rot, ref float sx, ref float sy, ref float lift)
        {
            if (rig && rig.visual && rig.visual.gameObject.activeSelf == HideBody) rig.visual.gameObject.SetActive(!HideBody);
            if (!UltOn) return;
            rot += UltRot; sx *= UltSx; sy *= UltSy; lift += UltLift;
        }

        Vector3 UltJitter() => UltJit > 0 ? new Vector3(Random.Range(-UltJit, UltJit), Random.Range(-UltJit, UltJit), 0) * World.U : Vector3.zero;

        // ── 좀비 (좀비 아포칼립스 필살기): 남은 시간 동안 피해 ×2 · 초록 · 팔 앞으로 ──
        [HideInInspector] public float zombie;
        bool zombieTinted;
        void ZombieTint()
        {
            bool on = zombie > 0;
            if (on == zombieTinted) return;
            zombieTinted = on;
            var c = on ? Manager.zombieTint : Color.white;
            foreach (var sr in rig.GetComponentsInChildren<SpriteRenderer>()) { var k = sr.color; sr.color = new Color(c.r, c.g, c.b, k.a); }
        }

        // ── 기절 별 (웹게임: 기절·데굴데굴 중 머리 위에 ★ 3개가 빙글빙글) ──
        SpriteRenderer[] stunStars;
        void StunStars()
        {
            bool on = (stun > 0 || tumbleT > 0) && !UltOn && !HideBody && Manager.stunStarSprite;
            if (!on) { if (stunStars != null && stunStars[0].enabled) foreach (var s in stunStars) s.enabled = false; return; }
            if (stunStars == null)
            {
                stunStars = new SpriteRenderer[3];
                for (int i = 0; i < 3; i++)
                {
                    var go = new GameObject("StunStar"); go.transform.SetParent(transform, false);
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = Manager.stunStarSprite; sr.color = Manager.stunStarColor;
                    stunStars[i] = sr;
                }
            }
            float sc = Manager.ratScale * GradeData.size, t = Time.time, head = Manager.stunStarHeight * sc;
            float w = Manager.stunStarSize * sc * World.U / Mathf.Max(0.001f, Manager.stunStarSprite.bounds.size.x);
            for (int i = 0; i < 3; i++)
            {
                float a = t * 8 + i * 2.09f, s = Mathf.Sin(a);
                var sr = stunStars[i];
                sr.enabled = true;
                sr.transform.localPosition = new Vector3(Mathf.Cos(a) * 12 * sc, head + s * 4 * sc, 0) * World.U;
                sr.transform.localScale = Vector3.one * w * (0.85f + 0.15f * s);
                sr.transform.localRotation = Quaternion.Euler(0, 0, t * 200 + i * 40);
                sr.sortingOrder = World.SortOrder(y) + (s > 0 ? -1 : 30);      // 뒤로 돌 땐 머리 뒤
            }
        }
    }
}
