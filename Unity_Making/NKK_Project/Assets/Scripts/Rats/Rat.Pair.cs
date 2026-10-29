using NKK.Data;
using UnityEngine;

namespace NKK.Rats
{
    // 새 쥐 둘 (2026-10-29)
    // · 콩쥐팥쥐 `kongpat`: 두 마리가 등을 맞대고 붙은 쌍둥이. 아래 쥐는 정상, 위 쥐는 거꾸로. 거꾸로 붙은 쪽은 팔다리를 바둥거림.
    //   특수 액션 (Jump_Slam) = 점프 한 번마다 공중제비로 위아래가 뒤바뀜 (3번 뛰면 홀수 → 처음과 뒤바뀐 채로 끝)
    // · 나비쥐 `butterfly`: 큰 귀 날개를 펄럭이며 땅에서 살짝 떠서 다님 (보이는 것만, 이동·충돌은 그대로)
    public partial class Rat
    {
        // ── 콩쥐팥쥐 ──
        RatRig twinRig;                       // 위에 붙은 쥐 (code_id twinCodeId 그림)
        float twinP, twinBase;                // 공중제비 진행 (반바퀴 = 1). 짝수 = 처음 모습
        bool twinWasAct;

        void InitTwin(RatArtLibrary.Entry art)
        {
            if (string.IsNullOrEmpty(art.twinCodeId)) return;
            var top = Manager.artLibrary.Get(art.twinCodeId);
            if (top == null || !rig) return;
            twinRig = Instantiate(rig, transform);
            twinRig.name = "TwinTop";
            var body = twinRig.GetComponent<Rat>(); if (body) { body.shadow = null; DestroyImmediate(body); }
            for (int c = twinRig.transform.childCount - 1; c >= 0; c--) { var ch = twinRig.transform.GetChild(c); if (ch.name != "Visual") DestroyImmediate(ch.gameObject); }
            twinRig.transform.localRotation = Quaternion.identity; twinRig.transform.localScale = Vector3.one; twinRig.transform.localPosition = Vector3.zero;
            twinRig.Build(top, Manager.RigLength(Data));
        }

        // 위·아래를 한 쌍으로: 아래 쥐 A = rig, 위 쥐 B = A 를 쌍의 가운데(바닥 위 H) 축으로 180° 돌린 것 (머리·꼬리가 서로 반대쪽 끝).
        // 공중제비 = 이 쌍 전체가 가운데 축으로 반 바퀴씩 돎 (θ = π·진행) → 아래·위 쥐가 자리를 바꿈. 서 있는 쥐가 pose 를 받고, 거꾸로 선 쥐는 바둥거림
        void TwinSplit(float scale, ref RatRig.Pose pose, ref float rot, ref float pivotH, out RatRig.Pose poseB, out float rotB)
        {
            var m = Manager;
            if (act.on && ActionType == EffectType.Jump_Slam)
            {
                int hops = Mathf.Max(1, Mathf.RoundToInt(act.x ? AW(1) : AV(1)));
                float u = Mathf.Min(hops, act.t / Mathf.Max(0.01f, act.dur) * hops * m.twinFlipRate);
                int f = Mathf.FloorToInt(u);
                twinP = twinBase + f + Mathf.SmoothStep(0, 1, u - f);
                twinWasAct = true;
            }
            else if (twinWasAct) { twinBase = Mathf.Round(twinP); twinP = twinBase; twinWasAct = false; }
            float c = Mathf.Cos(Mathf.PI * twinP);
            var flail = TwinFlail(pose, c);
            bool aUp = c >= 0;
            poseB = aUp ? flail : pose; if (!aUp) pose = flail;
            pivotH = rig.BackHeight * scale * m.twinGap;         // 쌍의 가운데 높이 = 등 높이 (twinGap 1 = 몸통 등이 딱 닿음)
            rot += Mathf.PI * twinP;
            rotB = rot + Mathf.PI;
            twinRig.visual.gameObject.SetActive(rig.visual.gameObject.activeSelf);
        }

        // 거꾸로 선 쥐: 팔다리를 허우적, 머리 도리도리, 꼬리 휘휘 (공중에선 더 세게)
        RatRig.Pose TwinFlail(in RatRig.Pose basePose, float c)
        {
            var m = Manager; float tt = Time.time * m.twinFlailSpeed + seed, k = z > 8 ? 1.5f : 1;
            var p = new RatRig.Pose { sx = 1, sy = 1 };
            float L = m.twinFlailLeg * k;
            p.front = Mathf.Sin(tt) * L; p.farFront = Mathf.Sin(tt + 1.9f) * L;
            p.back = Mathf.Sin(tt * 0.9f + 3.1f) * L; p.farBack = Mathf.Sin(tt * 0.9f + 4.6f) * L;
            p.head = Mathf.Sin(tt * 0.7f + 1) * 0.22f; p.tail = Mathf.Sin(tt * 0.6f) * m.twinFlailTail; p.tilt = Mathf.Sin(tt * 0.5f + 2) * 0.07f;
            return p;
        }

        // ── 나비쥐 ──
        bool IsButterfly => codeId == "butterfly";
        float flapPh;

        // 머리만 있는 쥐: 날개(귀)를 펄럭이고 머리가 둥실 · 꼬리는 하늘하늘. 액션·묘기 중엔 몸 모양이 없어 날갯짓만 (기절·잠 = 날개 축 처짐)
        void ButterflyPose(ref RatRig.Pose p)
        {
            var m = Manager; float tt = Time.time;
            bool moving = Speed > 30;
            p.front = p.farFront = p.back = p.farBack = 0; p.bob = 0; p.sx = p.sy = 1;
            p.tilt = (moving ? m.flyLean : 0) + Mathf.Sin(tt * 2 + seed) * 0.06f;
            p.head = Mathf.Sin(tt * 3 + seed) * 0.05f;
            p.tail = 0.1f + Mathf.Sin(tt * 3.4f + seed) * 0.35f;
            float ph = tt * (moving || act.on ? m.flapMove : m.flapIdle) + seed;
            flapPh = ph;
            p.wing = (stun > 0 || sleep > 0) ? -1 : Mathf.Sin(ph);        // −1 접힘 ~ +1 활짝
        }

        void ButterflyTransform(ref float lift)
        {
            if (!IsButterfly || HideBody) return;
            var m = Manager; float up = Mathf.Clamp01(1 - Mathf.Max(0, stun > 0 || sleep > 0 ? 1 : 0));
            lift += (m.flyHeight + Mathf.Sin(Time.time * m.flyBobSpeed + seed) * m.flyBob - Mathf.Cos(flapPh) * m.flapBob) * up * Mathf.Min(1, born * 2);      // 날개가 열릴 때 몸이 내려가고 닫힐 때 올라감
        }
    }
}
