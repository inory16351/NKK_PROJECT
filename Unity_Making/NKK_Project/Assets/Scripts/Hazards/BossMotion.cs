using System;
using System.Collections.Generic;
using NKK.Humans;
using NKK.Rats;
using UnityEngine;

namespace NKK.Hazards
{
    // 보스 공격 · 필살기 모션 (몬스터 리깅 툴 '모션', 2026-11-06 사용자). 설정 에셋 = Resources/BossMotion.asset.
    // 보스(code) × 기술(Atk_Type) 마다 키 프레임 (공격 시작부터 초). 있으면 그 기술 동안 Boss 의 코드 자세(MakeCatPose · MakePose) 대신 키 사이를 이어 씀.
    // 고양이 = RatRig.Pose 값 (라디안), 사람 = HumanRig.Pose 값, 파츠 보스 = 파츠마다 더하는 각도(°) + 두 번째 그림.
    // 효과(투사체 · 소환 등)와 공격 판정은 그대로 코드 — 모션은 몸 자세만.
    [CreateAssetMenu(menuName = "NKK/Boss Motion")]
    public class BossMotion : ScriptableObject
    {
        [Serializable] public class PartAngle { public string part; public float angle; }

        [Serializable]
        public class Key
        {
            [Tooltip("공격 시작부터 (초)")] public float t;
            [Header("고양이 (라디안)")]
            public float head, headX, tail, front, back, farFront, farBack, bob, tilt;
            [Tooltip("두 번째 머리 (head2, 입 벌림 등)")] public bool head2;
            [Header("사람 (라디안)")]
            public float armN, armF, legN, legF, lean;
            public bool angry = true, scared;
            [Header("공통")]
            [Tooltip("가로 · 세로 늘임 (1 = 그대로)")] public float sx = 1, sy = 1;
            [Header("파츠 보스")]
            [Tooltip("두 번째 그림 (공격 그림)")] public bool alt = true;
            [Tooltip("파츠마다 더하는 각도 (°)")] public List<PartAngle> parts = new();
            [Header("그리는 순서 (이 키부터 · 뒤 키로 이어짐)")]
            [Tooltip("파츠 이름 → 순서 (고양이 farBack · farFront · tail · torso · back · front · head / 사람 armFar · legFar · legNear · torso · head · armNear / 파츠 보스 = 파츠 이름). 비면 원래 순서")] public List<PartOrder> orders = new();
        }

        [Serializable]
        public class Motion
        {
            public string codeId, atkType;
            [Tooltip("마지막 키 뒤로: 끔 = 마지막 자세 유지 · 켬 = loopFrom 초부터 마지막 키까지 되풀이")] public bool loop;
            public float loopFrom;
            public List<Key> keys = new();
            public float Length => keys.Count > 0 ? keys[keys.Count - 1].t : 0;
        }

        public List<Motion> motions = new();

        public Motion Get(string code, string atk)
        {
            foreach (var m in motions) if (m.codeId == code && m.atkType == atk && m.keys.Count > 0) return m;
            return null;
        }

        // ── 게임에서 읽기 ──
        static BossMotion loaded; static bool tried;
        public static BossMotion I
        {
            get
            {
                if (!loaded && !tried) { tried = true; loaded = Resources.Load<BossMotion>("BossMotion"); }
                return loaded;
            }
        }
        public static void Reload() { tried = false; loaded = null; }      // 리깅 툴이 고친 뒤
        public static Motion Find(string code, string atk) => string.IsNullOrEmpty(code) || string.IsNullOrEmpty(atk) ? null : I ? I.Get(code, atk) : null;

        // 시간 t 의 두 키와 섞는 비율 (부드럽게)
        public static void Span(Motion m, float t, out Key a, out Key b, out float k)
        {
            var ks = m.keys; a = b = ks[0]; k = 0;
            if (ks.Count == 1) return;
            float end = ks[ks.Count - 1].t;
            if (m.loop && t > end && end > m.loopFrom) t = m.loopFrom + Mathf.Repeat(t - m.loopFrom, end - m.loopFrom);
            if (t <= ks[0].t) return;
            if (t >= end) { a = b = ks[ks.Count - 1]; return; }
            for (int i = 0; i < ks.Count - 1; i++)
                if (t < ks[i + 1].t) { a = ks[i]; b = ks[i + 1]; k = Mathf.SmoothStep(0, 1, (t - a.t) / Mathf.Max(1e-4f, b.t - a.t)); return; }
        }

        public static RatRig.Pose Cat(Motion m, float t, out bool head2)
        {
            Span(m, t, out var a, out var b, out var k);
            float L(float x, float y) => Mathf.Lerp(x, y, k);
            head2 = k < 0.5f ? a.head2 : b.head2;
            return new RatRig.Pose
            {
                head = L(a.head, b.head), headX = L(a.headX, b.headX), tail = L(a.tail, b.tail), front = L(a.front, b.front), back = L(a.back, b.back),
                farFront = L(a.farFront, b.farFront), farBack = L(a.farBack, b.farBack), bob = L(a.bob, b.bob), tilt = L(a.tilt, b.tilt), sx = L(a.sx, b.sx), sy = L(a.sy, b.sy),
            };
        }

        public static HumanRig.Pose Human(Motion m, float t)
        {
            Span(m, t, out var a, out var b, out var k);
            float L(float x, float y) => Mathf.Lerp(x, y, k);
            var n = k < 0.5f ? a : b;
            return new HumanRig.Pose
            {
                armN = L(a.armN, b.armN), armF = L(a.armF, b.armF), legN = L(a.legN, b.legN), legF = L(a.legF, b.legF), lean = L(a.lean, b.lean),
                head = L(a.head, b.head), bob = L(a.bob, b.bob), sx = L(a.sx, b.sx), sy = L(a.sy, b.sy), angry = n.angry, scared = n.scared,
            };
        }

        // 지금 시간의 그리는 순서: 그 시간까지 지난 키들의 순서를 차례로 덮음 (어느 키에서 바꾼 순서는 뒤 키로 이어짐, 되돌리려면 뒤 키에서 다시 정함). 섞지 않고 단계로 바뀜
        static readonly List<PartOrder> ordersBuf = new();
        static readonly Dictionary<string, int> ordersMap = new();
        public static List<PartOrder> Orders(Motion m, float t)
        {
            var ks = m.keys; if (ks.Count == 0) return null;
            float end = ks[ks.Count - 1].t;
            if (m.loop && t > end && end > m.loopFrom) t = m.loopFrom + Mathf.Repeat(t - m.loopFrom, end - m.loopFrom);
            ordersMap.Clear();
            foreach (var k in ks) { if (k.t > t + 1e-4f) break; if (k.orders != null) foreach (var o in k.orders) ordersMap[o.part] = o.order; }
            if (ordersMap.Count == 0) return null;
            ordersBuf.Clear();
            foreach (var kv in ordersMap) ordersBuf.Add(new PartOrder { part = kv.Key, order = kv.Value });
            return ordersBuf;
        }

        // 파츠 보스: 파츠마다 더하는 각도 · 두 번째 그림
        public static bool Part(Motion m, float t, Dictionary<string, float> into)
        {
            Span(m, t, out var a, out var b, out var k);
            into.Clear();
            foreach (var p in a.parts) into[p.part] = p.angle * (1 - k);
            foreach (var p in b.parts) into[p.part] = (into.TryGetValue(p.part, out var v) ? v : 0) + p.angle * k;
            return k < 0.5f ? a.alt : b.alt;
        }
    }
}
