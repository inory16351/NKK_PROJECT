using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NKK.Rats
{
    // 탈것 (웹게임 drawMount · drawCookBeast · beastGait · MOUNT_POSE): 슈퍼 요리사 쥐는 늘 네 발로 기는 요리사 등에 올라타 있음.
    // 요리사 = 전용 파츠 리그 (Art/Rats/Humans/Parts/cook_mount: 몸통·엉덩이·허벅지·정강이·윗팔·아래팔·끙끙/엉엉 머리).
    // 머리는 쥐 앞에 그려 쥐 앞발이 뒤통수 머리카락을 쥔 것처럼 + 머리카락 가닥 3줄. 필살기 대폭발 뒤엔 지쳐 뻗은 그림 (Parody/mount_cook_tired).
    // 좌표: 웹 캔버스 그대로 (게임 단위, y 아래 +, 그림은 왼쪽을 봄) → 유니티 로컬은 y·각도 부호만 반대.
    public partial class Rat
    {
        [Header("탈것 (슈퍼 요리사 쥐)")]
        [Tooltip("요리사를 타는 종 code_id")] public string mountCodeId = "starchef";
        [Tooltip("요리사 파츠 (비면 에디터에서 Art/Rats/Humans/Parts/cook_mount 에서 자동으로 읽음)")]
        public Sprite mountBody, mountButt, mountThigh, mountShin, mountUparm, mountForearm, mountStrain, mountCry;
        [Tooltip("지쳐 뻗은 요리사 (비면 UltimateManager.props 의 mount_cook_tired)")] public Sprite mountTiredSprite;
        [Tooltip("탄 쥐 크기 (보통 쥐 대비, 웹 MOUNT_RAT)")] public float mountRatScale = 0.5f;
        [Tooltip("요리사 머리 높이 = 전체 크기 (웹 BEAST.HEAD_H)")] public float mountHeadH = 54;
        [Tooltip("지쳐 뻗은 그림 가로 크기 (웹 MOUNT_W)")] public float mountTiredWidth = 128;
        [Tooltip("먼 쪽 팔다리 색 (어둡게)")] public Color mountFarColor = new(0.78f, 0.78f, 0.78f);
        [Tooltip("머리카락 가닥 색")] public Color mountHairColor = new(0.478f, 0.294f, 0.18f);

        // 필살기(그랑 퀴진)가 정함: 지쳐 뻗음 · 덜덜 떨림
        [HideInInspector] public bool MountTired;
        [HideInInspector] public float MountJit;

        // 웹 MOUNT_PARTS (원본 픽셀 크기 · 관절 피벗 좌상단 기준) · BEAST
        static readonly Vector2 SZ_BODY = new(447, 344), SZ_BUTT = new(281, 336), SZ_THIGH = new(176, 300), SZ_SHIN = new(231, 371),
            SZ_UPARM = new(159, 310), SZ_FOREARM = new(138, 439), SZ_STRAIN = new(333, 512), SZ_CRY = new(344, 505);
        static readonly Vector2 PV_THIGH = new(0.591f, 0.06f), PV_SHIN = new(0.682f, 0.06f), PV_UPARM = new(0.412f, 0.06f), PV_FOREARM = new(0.5f, 0.06f),
            PV_STRAIN = new(0.598f, 0.97f), PV_CRY = new(0.477f, 0.97f);
        static readonly Vector2 B_S = new(0.28f, 0.78f), B_H = new(0.95f, 0.55f), B_N = new(0.08f, 0.82f), B_B = new(0.78f, 0.04f), B_J = new(0.45f, 0.8f),
            B_HAIR = new(0.8f, 0.56f), B_PAW = new(12, 10);
        const float B_SIT = 17, LS_THIGH = 0.62f, LS_SHIN = 0.62f, LS_UPARM = 0.55f, LS_FOREARM = 0.5f, MOUNT_BACK = 18;
        const float TIRED_BX = 0.448f, TIRED_BY = 0.189f;   // 지친 그림 등 위치 (웹 mountBack 을 그림에서 미리 잰 값)

        class MPart { public Transform j; public SpriteRenderer r; }
        Transform mRoot, mHip, mHeadHip, mButtJ;
        MPart mBody, mButt, mHead, mArmF, mForeF, mThighF, mShinF, mArmN, mForeN, mThighN, mShinN;
        SpriteRenderer mTired;
        LineRenderer[] mHair;
        SpriteRenderer[] mAll;
        bool mountChecked, mountOk, mGallop;
        float mWalk, mSq = 1, mYank; int mStride = int.MinValue, mFace;

        bool Mounted => mountOk && codeId == mountCodeId;

        // ── 만들기 ──
        void MountInit()
        {
            mountChecked = true;
            if (string.IsNullOrEmpty(mountCodeId) || codeId != mountCodeId) return;
#if UNITY_EDITOR
            FillMountParts();
#endif
            if (!mountTiredSprite && Manager && Manager.Ults) foreach (var p in Manager.Ults.props) if (p.name == "mount_cook_tired") mountTiredSprite = p.sprite;
            if (!mountBody || !mountButt || !mountThigh || !mountShin || !mountUparm || !mountForearm || !mountStrain)
            { Debug.LogWarning("[Rat] 요리사 탈것 파츠 없음 (Rat 프리팹 mountBody~mountCry 연결 필요)"); return; }

            int layer = rig && rig.torso ? rig.torso.sortingLayerID : 0;
            mRoot = new GameObject("Mount_cook").transform; mRoot.SetParent(transform, false);
            // 쥐 정렬 그룹 안: 몸·팔다리 = 쥐 뒤 (-20~), 머리카락 15 · 머리 20 = 쥐 앞
            mHip = new GameObject("Hip").transform; mHip.SetParent(mRoot, false);
            mHeadHip = new GameObject("HeadHip").transform; mHeadHip.SetParent(mRoot, false);
            MPart Part(string n, Transform par, Sprite s, int order, bool far)
            {
                var j = new GameObject(n).transform; j.SetParent(par, false);
                var go = new GameObject("img"); go.transform.SetParent(j, false);
                var r = go.AddComponent<SpriteRenderer>(); r.sprite = s; r.sortingLayerID = layer; r.sortingOrder = order;
                if (far) r.color = mountFarColor;
                return new MPart { j = j, r = r };
            }
            mArmF = Part("UparmFar", mHip, mountUparm, -20, true); mForeF = Part("ForearmFar", mHip, mountForearm, -19, true);
            mThighF = Part("ThighFar", mHip, mountThigh, -18, true); mShinF = Part("ShinFar", mHip, mountShin, -17, true);
            mBody = Part("Body", mHip, mountBody, -16, false);
            mButt = Part("Butt", mHip, mountButt, -15, false); mButtJ = mButt.j;
            mThighN = Part("Thigh", mHip, mountThigh, -14, false); mShinN = Part("Shin", mHip, mountShin, -13, false);
            mArmN = Part("Uparm", mHip, mountUparm, -12, false); mForeN = Part("Forearm", mHip, mountForearm, -11, false);
            mHead = Part("Head", mHeadHip, mountStrain, 20, false);
            // 고정 크기·피벗 (머리는 매 프레임: 끙끙/엉엉 그림이 다름)
            float U = mountHeadH / SZ_STRAIN.y;
            Fit(mBody, SZ_BODY * U, B_H); Fit(mButt, SZ_BUTT * U, new Vector2(0.28f, 0.55f));
            foreach (var (p, sz, pv, ls) in new[] { (mThighF, SZ_THIGH, PV_THIGH, LS_THIGH), (mThighN, SZ_THIGH, PV_THIGH, LS_THIGH), (mShinF, SZ_SHIN, PV_SHIN, LS_SHIN), (mShinN, SZ_SHIN, PV_SHIN, LS_SHIN),
                (mArmF, SZ_UPARM, PV_UPARM, LS_UPARM), (mArmN, SZ_UPARM, PV_UPARM, LS_UPARM), (mForeF, SZ_FOREARM, PV_FOREARM, LS_FOREARM), (mForeN, SZ_FOREARM, PV_FOREARM, LS_FOREARM) })
                Fit(p, new Vector2(sz.x * U, sz.y * U * ls), pv);
            // 지쳐 뻗은 그림
            if (mountTiredSprite)
            {
                var go = new GameObject("Tired"); go.transform.SetParent(mRoot, false);
                mTired = go.AddComponent<SpriteRenderer>(); mTired.sprite = mountTiredSprite; mTired.sortingLayerID = layer; mTired.sortingOrder = -16; mTired.enabled = false;
            }
            // 머리카락 가닥 3줄 (쥐 앞발 → 뒤통수)
            mHair = new LineRenderer[3];
            var mat = new Material(Shader.Find("Sprites/Default"));
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Hair" + i); go.transform.SetParent(mRoot, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true; lr.positionCount = 9; lr.sharedMaterial = mat; lr.numCapVertices = 2;
                lr.startColor = lr.endColor = mountHairColor; lr.startWidth = lr.endWidth = 1.5f * World.U;
                lr.sortingLayerID = layer; lr.sortingOrder = 15;
                mHair[i] = lr;
            }
            mAll = mRoot.GetComponentsInChildren<SpriteRenderer>(true);
            mFace = face; mountOk = true;
        }

        // 파츠 크기(게임 단위)·피벗(좌상단 기준 0~1) 맞추기: 관절 = 피벗 자리
        static void Fit(MPart p, Vector2 size, Vector2 pv)
        {
            var s = p.r.sprite; if (!s) return;
            var t = p.r.transform;
            t.localScale = new Vector3(size.x / (s.rect.width / s.pixelsPerUnit), size.y / (s.rect.height / s.pixelsPerUnit), 1);
            t.localPosition = new Vector3((0.5f - pv.x) * size.x, -(0.5f - pv.y) * size.y, 0);
        }
        // 웹 캔버스 좌표 (y 아래 +, 각도 시계방향 +) → 로컬
        static void Put(Transform t, float x, float y, float a) { t.localPosition = new Vector3(x, -y, 0); t.localRotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg); }

        // ── 걸음새 (웹 beastGait) ──
        struct BGait { public float ua, fa, ua_f, fa_f, ta, sa, ta_f, sa_f, lift, wig, head; }
        BGait BeastGait()
        {
            float T = Time.time;
            if (!mGallop)
            {
                float b = Mathf.Sin(T * 9), w = Mathf.Sin(T * 7);
                return new BGait { ua = 0.15f, fa = -0.25f, ua_f = 0.05f, fa_f = -0.3f, ta = 0.55f, sa = -0.45f, ta_f = 0.45f, sa_f = -0.5f, lift = 0, wig = w * 0.18f, head = -0.15f + b * 0.06f };   // 멈춤: 쪼그려 앉아 엉덩이 씰룩
            }
            float f = mWalk * 0.9f;
            void G(float ph, out float ua, out float fa, out float ta, out float sa)
            {
                float s = Mathf.Sin(f + ph);
                ua = 0.3f + 0.95f * s; fa = ua - 0.35f - 0.9f * Mathf.Max(0, -s);
                ta = 0.45f - 0.95f * s; sa = ta - 1.0f - 0.3f * Mathf.Max(0, s);
            }
            G(0, out var ua, out var fa, out var ta, out var sa); G(-0.45f, out var uaf, out var faf, out var taf, out var saf);
            float s0 = Mathf.Sin(f);
            return new BGait { ua = ua, fa = fa, ua_f = uaf, fa_f = faf, ta = ta, sa = sa, ta_f = taf, sa_f = saf,
                lift = Mathf.Max(0, s0) * 22, wig = Mathf.Sin(f * 2) * 0.22f, head = -0.25f + Mathf.Sin(f * 2 + 1) * 0.25f };
        }

        // ── 매 프레임: LateUpdate 에서 rig.Apply 전에 (쥐를 등 위로 올리고 작게) ──
        void MountTransform(ref float scale, ref float lift)
        {
            if (!mountChecked) MountInit();
            if (!Mounted) return;
            if (!UltOn) { MountTired = false; MountJit = 0; }
            bool show = !HideBody;
            if (mRoot.gameObject.activeSelf != show) mRoot.gameObject.SetActive(show);
            if (!show) return;

            float dt = Time.deltaTime, born01 = EaseOutBack(born), f60 = dt * 60;
            bool mv = Speed > 25 || UltOn;
            // 조종: 방향을 바꾸거나 출발할 때 머리카락을 홱 당김 (달리는 중에도 가끔)
            bool turn = mFace != face, go = mv && !mGallop;
            if (turn || go || (mv && Random.value < 0.006f * f60)) mYank = turn ? 1 : Mathf.Max(mYank, 0.7f);
            mYank = Mathf.Max(0, mYank - 0.05f * f60);
            mFace = face; mWalk += (mv ? 0.35f : 0.04f) * f60; mGallop = mv;
            float jit = MountJit > 0 ? Random.Range(-MountJit, MountJit) : 0;
            float alpha = ghost ? 0.6f : temp > 0 ? Mathf.Clamp01(temp / 0.6f) : 1;
            foreach (var r in mAll) { var c = r.color; c.a = alpha; r.color = c; }

            // 필살기 대폭발 뒤: 지쳐 뻗은 그림 한 장
            bool tired = MountTired && mTired;
            mHip.gameObject.SetActive(!tired); mHeadHip.gameObject.SetActive(!tired);
            foreach (var h in mHair) h.enabled = !tired;
            if (mTired) mTired.enabled = tired;
            float seat;
            if (tired)
            {
                var sp = mTired.sprite; float w = mountTiredWidth, h = w * sp.rect.height / sp.rect.width;
                mRoot.localScale = new Vector3(-face, 1, 1) * World.U * born01;
                mRoot.localPosition = new Vector3(jit * World.U, 0, 0);
                mTired.transform.localScale = new Vector3(w / (sp.rect.width / sp.pixelsPerUnit), h / (sp.rect.height / sp.pixelsPerUnit), 1);
                mTired.transform.localPosition = new Vector3(-(TIRED_BX - 0.5f) * w, h * 0.5f, 0);
                seat = h * (1 - TIRED_BY) - 10;
            }
            else seat = DrawBeast(jit);

            // 착지할 때마다 흙먼지 + 찌그러짐 (보폭 한 번 = 2π)
            int stride = Mathf.FloorToInt(mWalk * 0.9f / (Mathf.PI * 2) + 0.25f);
            if (mGallop && !tired && stride != mStride && mStride != int.MinValue)
            {
                mSq = 0.88f;
                var fx = FxManager.I; if (fx && Manager.Ults && Manager.Ults.OnScreen(x, y)) fx.Dust(x - face * MOUNT_BACK, y - 1, 2, 0.7f);
            }
            mStride = stride; mSq += (1 - mSq) * (1 - Mathf.Pow(0.8f, f60));

            scale *= mountRatScale;
            lift += seat * born01;
        }

        // 웹 drawCookBeast: 쥐 발밑(x = 0)에 등 위 B 가 오게 몸을 놓고, 쥐가 앉을 높이를 돌려줌
        float DrawBeast(float jit)
        {
            var q = BeastGait();
            float U = mountHeadH / SZ_STRAIN.y;
            float bw = SZ_BODY.x * U, bh = SZ_BODY.y * U, uw = SZ_BUTT.x * U, uh = SZ_BUTT.y * U;
            float Len(Vector2 sz, float ls) => sz.y * U * ls * 0.86f;   // 마디 길이 = 그림 세로의 86%
            float lThigh = Len(SZ_THIGH, LS_THIGH), lShin = Len(SZ_SHIN, LS_SHIN), lUparm = Len(SZ_UPARM, LS_UPARM), lFore = Len(SZ_FOREARM, LS_FOREARM);
            // 어깨·엉덩이 높이 = 팔다리가 닿는 높이
            Vector2 J0 = new((B_J.x - 0.28f) * uw, (B_J.y - 0.55f) * uh);
            float hs = lUparm * Mathf.Cos(q.ua) + lFore * Mathf.Cos(q.fa) + (1 - B_S.y) * bh * 0.3f;
            float hh = lThigh * Mathf.Cos(q.ta) + lShin * Mathf.Cos(q.sa) + J0.y;
            float D = (B_H.x - B_S.x) * bw;
            float rot = Mathf.Clamp(Mathf.Atan2(hs - hh, D) * 0.7f, -0.4f, 0.4f) - Mathf.Atan2((B_H.y - B_S.y) * bh, D);
            Vector2 Rv(float px, float py, float a) => new(px * Mathf.Cos(a) - py * Mathf.Sin(a), px * Mathf.Sin(a) + py * Mathf.Cos(a));
            var Bl = Rv((B_B.x - B_H.x) * bw, (B_B.y - B_H.y) * bh, rot);
            float Hx = -Bl.x, Hy = -hh - q.lift;
            float seat = -(Hy + Bl.y) - B_SIT;
            Vector2 S = new((B_S.x - B_H.x) * bw, (B_S.y - B_H.y) * bh), N = new((B_N.x - B_H.x) * bw, (B_N.y - B_H.y) * bh);
            var J = Rv(J0.x, J0.y, q.wig);

            // 전체: 쥐 위치 기준, 그림은 왼쪽을 봄 → 오른쪽 볼 땐 뒤집음 + 착지 찌그러짐
            float sq = mSq, b01 = EaseOutBack(born);
            mRoot.localScale = new Vector3(-face / Mathf.Sqrt(sq), sq, 1) * World.U * b01;
            mRoot.localPosition = new Vector3(jit * World.U, 0, 0);
            Put(mHip, Hx, Hy, rot); Put(mHeadHip, Hx, Hy, rot);
            void Limb(MPart a, MPart b, float l1, float px, float py, float a1, float a2)
            {
                Put(a.j, px, py, a1 - rot);
                Put(b.j, px - Mathf.Sin(a1 - rot) * l1, py + Mathf.Cos(a1 - rot) * l1, a2 - rot);
            }
            Limb(mArmF, mForeF, lUparm, S.x + bw * 0.06f, S.y, q.ua_f, q.fa_f);   // 먼 쪽 (어둡게)
            Limb(mThighF, mShinF, lThigh, J.x - uw * 0.08f, J.y, q.ta_f, q.sa_f);
            Put(mBody.j, 0, 0, 0);
            Put(mButtJ, 0, 0, q.wig);                                             // 엉덩이 씰룩
            Limb(mThighN, mShinN, lThigh, J.x, J.y, q.ta, q.sa);                  // 가까운 쪽
            Limb(mArmN, mForeN, lUparm, S.x, S.y, q.ua, q.fa);

            // 머리: 멈추면 엉엉, 달리면 끙끙 (가끔 엉엉). 쥐가 머리카락을 당기면 고개가 뒤로 홱
            bool cry = (!mGallop || Mathf.Sin(Time.time * 0.7f + x * 0.01f) > 0.75f) && mountCry;
            Vector2 hsz = (cry ? SZ_CRY : SZ_STRAIN) * U, hpv = cry ? PV_CRY : PV_STRAIN;
            mHead.r.sprite = cry ? mountCry : mountStrain;
            Fit(mHead, hsz, hpv);
            Put(mHead.j, N.x, N.y, q.head + mYank * 0.6f - rot);

            // 머리카락 가닥: 쥐 앞발 → 뒤통수 (당길 땐 팽팽, 평소엔 살짝 처짐)
            float rs = Manager.ratScale * GradeData.size * mountRatScale * b01, sag = 5 * (1 - mYank);
            Vector3 hpW = mHead.j.TransformPoint(new Vector3((B_HAIR.x - hpv.x) * hsz.x, -(B_HAIR.y - hpv.y) * hsz.y, 0));
            Vector3 pawW = transform.position + new Vector3(face * B_PAW.x * rs, seat * b01 + B_PAW.y * rs, 0) * World.U;
            for (int i = -1; i <= 1; i++)
            {
                var lr = mHair[i + 1];
                Vector3 p0 = pawW + new Vector3(0, -i * 1.2f, 0) * World.U, p2 = hpW + new Vector3(0, -i * 1.5f, 0) * World.U;
                Vector3 c = (p0 + p2) * 0.5f + new Vector3(0, -(sag + i), 0) * World.U;
                for (int k = 0; k < 9; k++) { float t = k / 8f; lr.SetPosition(k, (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * c + t * t * p2); }
            }
            return seat;
        }

        // 탄 쥐 자세 (웹 MOUNT_POSE): 허리 펴고 앉아 앞발로 요리사 머리카락을 쥠. 필살기 자세·액션·묘기·기절 중엔 그대로
        RatRig.Pose MountPose(RatRig.Pose p)
        {
            if (!Mounted || HideBody || (UltOn && UltPose.HasValue) || act.on || Trick != TrickType.None || stun > 0 || tumbleT > 0 || sleep > 0) return p;
            float tt = Time.time, yk = mYank;
            return new RatRig.Pose
            {
                front = 0.35f - yk * 0.5f + Mathf.Sin(tt * 12) * 0.05f, farFront = 0.3f - yk * 0.4f + Mathf.Cos(tt * 12) * 0.05f,
                back = 1.2f, farBack = 1.1f, head = -0.25f - yk * 0.2f, tail = 1.1f + Mathf.Sin(tt * 6) * 0.2f, tilt = 0.12f - yk * 0.4f, sx = 1, sy = 1
            };
        }

#if UNITY_EDITOR
        // 비어 있는 요리사 파츠를 에디터에서 채움 (프리팹에서 우클릭 → Fill Mount Parts 로 저장하면 빌드에서도 씀)
        [ContextMenu("Fill Mount Parts")]
        void FillMountParts()
        {
            const string dir = "Assets/Art/Rats/Humans/Parts/cook_mount/";
            Sprite L(Sprite cur, string n) => cur ? cur : AssetDatabase.LoadAssetAtPath<Sprite>(dir + n + ".png");
            mountBody = L(mountBody, "body"); mountButt = L(mountButt, "butt"); mountThigh = L(mountThigh, "thigh"); mountShin = L(mountShin, "shin");
            mountUparm = L(mountUparm, "uparm"); mountForearm = L(mountForearm, "forearm"); mountStrain = L(mountStrain, "strain"); mountCry = L(mountCry, "cry");
            if (!mountTiredSprite) mountTiredSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Rats/Parody/mount_cook_tired.png");
            if (!Application.isPlaying) EditorUtility.SetDirty(this);
        }
#endif
    }
}
