using UnityEngine;
using UnityEngine.Rendering;

namespace NKK.Rats
{
    // 탈것 (웹게임 drawMount): 슈퍼 요리사 쥐는 늘 네 발로 기는 요리사 등에 올라타 있음.
    // 그림 = UltimateManager.props 의 mount_cook1~4 (기어가는 프레임) · mount_cook_tired (필살기 대폭발 뒤 지쳐 뻗음). 그림은 왼쪽을 봄.
    public partial class Rat
    {
        [Header("탈것 (슈퍼 요리사 쥐)")]
        [Tooltip("요리사를 타는 종 code_id")] public string mountCodeId = "starchef";
        [Tooltip("기는 요리사 그림 가로 크기 (게임 단위)")] public float mountWidth = 130;
        [Tooltip("지쳐 뻗은 요리사 그림 가로 크기")] public float mountTiredWidth = 158;
        [Tooltip("탄 쥐 크기 (보통 쥐 대비, 웹 MOUNT_RAT)")] public float mountRatScale = 0.5f;
        [Tooltip("쥐가 앉는 등 높이 = 그림 높이 × 이 값")] public float mountSeat = 0.62f;
        [Tooltip("지친 그림의 등 높이 비율")] public float mountTiredSeat = 0.55f;
        [Tooltip("요리사 중심이 쥐보다 뒤로 떨어진 거리 (웹 MOUNT_BACK)")] public float mountBack = 18;
        [Tooltip("달릴 때 프레임 속도 (초당)")] public float mountFps = 12;

        // 필살기(그랑 퀴진)가 정함: 지쳐 뻗음 · 덜덜 떨림
        [HideInInspector] public bool MountTired;
        [HideInInspector] public float MountJit;

        SpriteRenderer mountSr;
        Sprite[] mountRun; Sprite mountTiredSp;
        float mountWalk, mountSq = 1, mountYank; int mountStride = -1, mountFace;
        bool mountChecked;

        bool Mounted => mountRun != null && !string.IsNullOrEmpty(mountCodeId) && codeId == mountCodeId;

        void MountInit()
        {
            mountChecked = true;
            if (codeId != mountCodeId || !Manager || !Manager.Ults) return;
            var run = new Sprite[4];
            foreach (var p in Manager.Ults.props)
            {
                if (p.name == "mount_cook_tired") mountTiredSp = p.sprite;
                else if (p.name.StartsWith("mount_cook") && int.TryParse(p.name.Substring(10), out int i) && i >= 1 && i <= 4) run[i - 1] = p.sprite;
            }
            foreach (var s in run) if (!s) { Debug.LogWarning("[Rat] 탈것 그림 없음 (UltimateManager Fill Props 필요): mount_cook1~4"); return; }
            mountRun = run;
            var go = new GameObject("Mount_cook"); go.transform.SetParent(transform, false);
            mountSr = go.AddComponent<SpriteRenderer>();
            var g = rig ? rig.GetComponent<SortingGroup>() : null;
            if (g) mountSr.sortingLayerID = g.sortingLayerID;
            mountFace = face;
        }

        // LateUpdate 에서 rig.Apply 전에: 요리사 그림 갱신 + 쥐를 등 위로 올리고 작게
        void MountTransform(ref float scale, ref float lift)
        {
            if (!mountChecked) MountInit();
            if (!Mounted) return;
            if (!UltOn) { MountTired = false; MountJit = 0; }
            bool show = !HideBody;
            mountSr.enabled = show;
            if (!show) return;

            float dt = Time.deltaTime, born01 = EaseOutBack(born);
            bool mv = Speed > 25 || UltOn;
            // 조종: 방향을 바꾸거나 출발할 때 머리카락을 홱 당김
            if (mountFace != face) mountYank = 1; else if (mv && Random.value < 0.006f) mountYank = Mathf.Max(mountYank, 0.7f);
            mountFace = face;
            mountYank = Mathf.Max(0, mountYank - dt * 3);

            Sprite sp; float w, seatK, bob = 0;
            if (MountTired && mountTiredSp) { sp = mountTiredSp; w = mountTiredWidth; seatK = mountTiredSeat; }
            else
            {
                mountWalk += dt * (mv ? mountFps : 1.2f);
                int f = mv ? Mathf.FloorToInt(mountWalk) % 4 : 0;
                sp = mountRun[f]; w = mountWidth; seatK = mountSeat;
                // 착지(프레임 한 바퀴 = 보폭 두 번)마다 찌그러짐 + 흙먼지
                int stride = Mathf.FloorToInt(mountWalk / 2);
                if (mv && stride != mountStride) { mountSq = 0.9f; var fx = FxManager.I; if (fx && Random.value < 0.6f) fx.Dust(x - face * mountBack, y, 2, 0.7f); }
                mountStride = stride;
                bob = mv ? Mathf.Abs(Mathf.Sin(mountWalk * Mathf.PI * 0.5f)) * 4 : Mathf.Sin(Time.time * 3 + seed) * 1.2f;
            }
            mountSq += (1 - mountSq) * Mathf.Min(1, dt * 12);

            float sw = sp.bounds.size.x, h = w * sp.bounds.size.y / sw;
            float jit = MountJit > 0 ? Random.Range(-MountJit, MountJit) : 0;
            mountSr.sprite = sp;
            mountSr.flipX = face > 0;                     // 그림은 왼쪽을 봄
            mountSr.sortingOrder = World.SortOrder(y) - 1; // 쥐 바로 뒤
            var c = mountSr.color; c.a = ghost ? 0.6f : temp > 0 ? Mathf.Clamp01(temp / 0.6f) : 1; mountSr.color = c;
            float k = w * born01 * World.U / sw;
            mountSr.transform.localScale = new Vector3(k / Mathf.Sqrt(mountSq), k * mountSq, 1);
            mountSr.transform.localPosition = new Vector3(-face * mountBack + jit, h * born01 * mountSq * 0.5f + bob * 0.3f, 0) * World.U;

            scale *= mountRatScale;
            lift += h * seatK * born01 * mountSq + bob;
        }

        // 탄 쥐 자세 (웹 MOUNT_POSE): 허리 펴고 앉아 앞발로 요리사 머리카락을 쥠. 필살기·액션·묘기·기절 중엔 그대로
        RatRig.Pose MountPose(RatRig.Pose p)
        {
            if (!Mounted || HideBody || (UltOn && UltPose.HasValue) || act.on || Trick != TrickType.None || stun > 0 || tumbleT > 0 || sleep > 0 || z > 2) return p;
            float tt = Time.time, yk = mountYank;
            return new RatRig.Pose
            {
                front = 0.35f - yk * 0.5f + Mathf.Sin(tt * 12) * 0.05f, farFront = 0.3f - yk * 0.4f + Mathf.Cos(tt * 12) * 0.05f,
                back = 1.2f, farBack = 1.1f, head = -0.25f - yk * 0.2f, tail = 1.1f + Mathf.Sin(tt * 6) * 0.2f, tilt = 0.12f - yk * 0.4f, sx = 1, sy = 1
            };
        }
    }
}
