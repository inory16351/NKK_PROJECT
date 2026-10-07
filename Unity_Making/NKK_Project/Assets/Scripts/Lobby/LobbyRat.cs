using System;
using NKK.Data;
using NKK.Rats;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NKK.Lobby
{
    // 아지트에서 생활하는 쥐 한 마리 (웹 lobbyscene.js HOME 의 actor). 좌표는 배경판 기준 u(가로 0~1)·v(세로 0~1)·z(바닥에서 높이, 세로 길이 비율)
    // 상태: 걷기 · 웅크림 → 공중 → 착지 · 어질어질 · 잡힘 · 낮잠 · 차 · 쳇바퀴 · 먹기 · 아령
    public class LobbyRat : MonoBehaviour
    {
        public enum State { Idle, Walk, Crouch, Air, Land, Dizzy, Held, Sleep, Tea, Wheel, Eat, Lift }

        [Header("자식")]
        public RatRig rig;
        public SpriteRenderer shadow;
        [Tooltip("손에 든 것 (치즈·아령·찻잔)")] public SpriteRenderer held;

        [HideInInspector] public LobbyHome home;
        [HideInInspector] public RatCharacterRow row;
        public State state;
        [HideInInspector] public float u, v, z, vz, vu, rot, spin, tState, walk, seed, size = 1, squash, tu, tv, dizzy, liftK, ground;
        [HideInInspector] public int face = 1;
        [HideInInspector] public bool thrown, sip;
        [HideInInspector] public LobbySlot slot;
        Action then;
        Vector2? jto; float airT, airTotal; Vector2 air0, air1; bool arc;
        float rigLength;

        public void Init(LobbyHome h, RatCharacterRow r, RatArtLibrary.Entry art, float rigLen)
        {
            home = h; row = r; rigLength = rigLen;
            rig.Build(art, rigLen);
            name = "LobbyRat_" + r.code_id;
            u = Random.Range(h.floorU.x, h.floorU.y); v = Random.Range(h.floorV.x, h.floorV.y);
            face = Random.value < 0.5f ? -1 : 1; state = State.Idle; tState = Random.Range(0.5f, 3f);
            walk = Random.Range(0, 6f); seed = Random.Range(0, 10f);
            size = 0.92f + Mathf.Min(0.25f, (int)r.Grade * 0.05f);
        }

        // ── 행동 ──
        public void WalkTo(float tu0, float tv0, Action next)
        {
            state = State.Walk; tu = Mathf.Clamp(tu0, home.floorU.x - 0.1f, home.floorU.y + 0.1f); tv = tv0; then = next; face = tu > u ? 1 : -1;
        }

        // 점프: 웅크림(0.12초) → 도약 → 공중 → 착지 찌그러짐. to 가 있으면 그 자리(높은 곳)로 아치를 그리며 날아감
        public void Jump(Vector2? to = null, Action next = null)
        {
            state = State.Crouch; tState = 0.12f; jto = to; then = next; thrown = false;
            if (to.HasValue) face = to.Value.x > u ? 1 : to.Value.x < u ? -1 : face;
        }

        void Launch()
        {
            state = State.Air;
            if (jto.HasValue) { arc = true; airT = 0; airTotal = 0.42f; air0 = new Vector2(u, v); air1 = jto.Value; vz = 0; }
            else { arc = false; vz = home.jumpV * Random.Range(0.9f, 1.15f); vu = Random.Range(-0.05f, 0.05f); }
        }

        void Land(bool hard)
        {
            z = 0; vz = 0; vu = 0; spin = 0; squash = 1; state = State.Land; tState = hard ? 0.3f : 0.18f;
            for (int i = 0; i < (hard ? 7 : 4); i++) home.Fx(LobbyHome.FxKind.Dust, u + Random.Range(-0.015f, 0.015f), v, Random.Range(-0.03f, 0.03f), Random.Range(0.35f, 0.6f));
        }

        public void Choose()
        {
            var free = home.FreeSlots();
            if (free.Count > 0 && Random.value < 0.55f)
            {
                var sl = free[Random.Range(0, free.Count)];
                slot = sl; sl.user = this;
                var p = home.ToUV(sl.transform.position);
                void Sit()
                {
                    state = sl.act switch { LobbySlot.Act.Sleep => State.Sleep, LobbySlot.Act.Tea => State.Tea, LobbySlot.Act.Wheel => State.Wheel, LobbySlot.Act.Eat => State.Eat, _ => State.Lift };
                    face = sl.face; tState = state == State.Sleep ? Random.Range(10f, 18f) : Random.Range(6f, 12f); u = p.x; v = p.y;
                }
                WalkTo(p.x + (sl.up ? -0.03f * sl.face : 0), sl.up ? home.floorLine : p.y, () => { if (sl.up) Jump(p, Sit); else Sit(); });
                return;
            }
            FreeSlot();
            WalkTo(Random.Range(home.floorU.x, home.floorU.y), Random.Range(home.floorV.x, home.floorV.y), () =>
            {
                state = State.Idle; tState = Random.Range(1f, 3.5f);
                if (Random.value < 0.18f) home.Say(this, home.RandomLine(home.soloLines), 1.8f);
            });
        }

        public void FreeSlot() { if (slot) { if (slot.user == this) slot.user = null; slot = null; } }

        public void LeaveSlot(Action next = null)
        {
            var sl = slot; FreeSlot();
            next ??= Choose;
            if (sl && sl.up) Jump(new Vector2(Mathf.Clamp(u + (Random.value < 0.5f ? -0.035f : 0.035f), home.floorU.x, home.floorU.y), home.floorLine + Random.Range(-0.01f, 0.01f)), next);
            else next();
        }

        // ── 마우스: 누르면 점프 / 끌면 잡힘 / 놓으면 던져짐 ──
        public bool Busy => state == State.Air || state == State.Crouch || state == State.Held;

        public void Poke()
        {
            if (Busy) return;
            if (slot)
            {
                var sl = slot; FreeSlot();
                if (sl.up) { Jump(new Vector2(Mathf.Clamp(u + (Random.value < 0.5f ? -0.04f : 0.04f), home.floorU.x, home.floorU.y), home.floorLine)); return; }
            }
            Jump();
            if (Random.value < 0.5f) home.Say(this, home.RandomLine(home.pokeLines), 1.2f);
        }

        public void Grab()
        {
            FreeSlot();
            ground = Mathf.Clamp(v, home.floorV.x, home.floorV.y);
            state = State.Held; then = null; arc = false; rot = 0;
            home.Say(this, home.RandomLine(home.grabLines), 1.4f);
        }

        public void HoldAt(float pu, float pv, float vel)
        {
            u = Mathf.Clamp(pu, 0.03f, 0.97f); v = ground;
            z = Mathf.Max(0, ground - pv - home.ratLen * 0.4f * home.Aspect * 0.3f);
            rot = Mathf.Clamp(vel * 0.6f, -0.8f, 0.8f); face = vel >= 0 ? 1 : -1;
        }

        public void Throw(float velU, float velZ)
        {
            vu = Mathf.Clamp(velU, -2.2f, 2.2f); vz = Mathf.Clamp(velZ, -1.5f, 2.6f);
            spin = vu * 9; state = State.Air; arc = false; thrown = true; then = null;
            if (z <= 0.001f) z = 0.002f;
            if (Mathf.Sqrt(vu * vu + vz * vz) > 1.2f) home.Say(this, home.throwLine, 1.2f);
        }

        // ── 매 프레임 ──
        public void Tick(float dt, float t)
        {
            tState -= dt; squash = Mathf.Max(0, squash - dt * 5);
            if (state == State.Walk || state == State.Wheel) walk += dt * (state == State.Wheel ? 30 : 18);
            switch (state)
            {
                case State.Walk:
                {
                    float du = tu - u, dv = tv - v, d = Mathf.Sqrt(du * du + dv * dv), sp = home.walkSpeed * dt;
                    if (d <= sp) { u = tu; v = tv; var f = then; then = null; if (f != null) f(); else { state = State.Idle; tState = 2; } }
                    else { u += du / d * sp; v += dv / d * sp; }
                    break;
                }
                case State.Crouch: if (tState <= 0) Launch(); break;
                case State.Air:
                    if (arc)
                    {
                        airT += dt; float k = Mathf.Min(1, airT / airTotal);
                        u = Mathf.Lerp(air0.x, air1.x, k); v = Mathf.Lerp(air0.y, air1.y, k); z = Mathf.Sin(k * Mathf.PI) * (0.045f + Mathf.Abs(air1.y - air0.y) * 0.6f);
                        vz = Mathf.Cos(k * Mathf.PI);
                        if (k >= 1) { arc = false; Land(false); }
                        break;
                    }
                    vz -= home.gravity * dt; z += vz * dt; u += vu * dt; rot += spin * dt;
                    if (u < 0.03f) { u = 0.03f; vu = Mathf.Abs(vu) * 0.5f; } else if (u > 0.97f) { u = 0.97f; vu = -Mathf.Abs(vu) * 0.5f; }
                    if (v - z < 0.1f) { z = v - 0.1f; vz = -Mathf.Abs(vz) * 0.3f; }            // 천장
                    if (z <= 0 && vz < 0)
                    {
                        if (thrown && vz < -0.55f)                                              // 세게 떨어지면 통통 튐
                        {
                            z = 0; vz = -vz * 0.38f; vu *= 0.55f; spin *= 0.5f;
                            for (int i = 0; i < 5; i++) home.Fx(LobbyHome.FxKind.Dust, u + Random.Range(-0.015f, 0.015f), v, Random.Range(-0.03f, 0.03f), 0.5f);
                        }
                        else
                        {
                            bool hard = thrown; rot = 0; Land(hard);
                            if (hard) { dizzy = 1.8f; home.Say(this, home.RandomLine(home.dizzyLines), 1.8f); }
                        }
                    }
                    break;
                case State.Land:
                    if (tState <= 0)
                    {
                        if (dizzy > 0) { state = State.Dizzy; tState = dizzy; dizzy = 0; }
                        else { var f = then; then = null; if (f != null) f(); else { state = State.Idle; tState = Random.Range(0.6f, 2f); } }
                    }
                    break;
                case State.Dizzy: if (tState <= 0) { state = State.Idle; tState = Random.Range(0.5f, 1.5f); } break;
                case State.Held: break;
                case State.Idle: if (tState <= 0) Choose(); break;
                case State.Sleep:
                    if (Random.value < dt * 0.8f) home.Fx(LobbyHome.FxKind.Z, u - 0.01f, v - 0.05f, 0, 2.2f);
                    if (tState <= 0) { home.Say(this, home.wakeLine, 1.6f); LeaveSlot(); }
                    break;
                case State.Tea:
                    sip = Mathf.Sin(t * 1.3f + seed) > 0.85f;
                    home.TeaChat(this, dt);
                    if (tState <= 0) LeaveSlot();
                    break;
                case State.Wheel:
                    if (Random.value < dt * 0.15f) home.Say(this, home.RandomLine(home.wheelLines), 1.4f);
                    if (tState <= 0) { home.Say(this, home.wheelDoneLine, 1); LeaveSlot(); }
                    break;
                case State.Eat:
                    if (Random.value < dt * 3) home.Fx(LobbyHome.FxKind.Crumb, u + 0.03f * face, v - 0.02f, Random.Range(-0.02f, 0.02f), 0.7f);
                    if (tState <= 0) { home.Say(this, home.fullLine, 1.4f); LeaveSlot(); }
                    break;
                case State.Lift:
                    liftK = (Mathf.Sin(t * 3 + seed) + 1) / 2;
                    if (Random.value < dt * 0.6f) home.Fx(LobbyHome.FxKind.Sweat, u + Random.Range(-0.01f, 0.01f), v - 0.06f, 0, 0.8f);
                    if (tState <= 0) LeaveSlot();
                    break;
            }
        }

        // ── 자세 (웹 poseOf). 값은 게임 ratPose 와 같은 의미 ──
        RatRig.Pose PoseOf(float t)
        {
            float w = Mathf.Sin(t * 2.2f + seed);
            var p = new RatRig.Pose { head = w * 0.04f, tail = 0.2f + Mathf.Sin(t * 1.6f + seed) * 0.12f, front = 0.05f, back = -0.05f, farFront = -0.1f, farBack = 0.1f, sx = 1, sy = 1 + Mathf.Sin(t * 2.6f + seed) * 0.015f };
            void Flail(float ph) { p.front = Mathf.Sin(t * 14 + ph) * 1.4f; p.farFront = Mathf.Cos(t * 12 + ph) * 1.4f; p.back = Mathf.Sin(t * 13 + ph + 2) * 1.3f; p.farBack = Mathf.Cos(t * 11 + ph) * 1.3f; p.head = -0.3f; p.tail = 1.2f + Mathf.Sin(t * 9) * 0.3f; }
            switch (state)
            {
                case State.Walk: case State.Wheel:
                {
                    float wk = walk, amp = state == State.Wheel ? 1.15f : 0.8f;
                    p.front = Mathf.Sin(wk) * amp; p.farFront = Mathf.Sin(wk + 0.6f) * amp; p.back = Mathf.Sin(wk + Mathf.PI) * amp; p.farBack = Mathf.Sin(wk + Mathf.PI + 0.6f) * amp;
                    p.bob = -Mathf.Abs(Mathf.Sin(wk)) * 3; p.head = Mathf.Cos(wk) * 0.05f - 0.05f; p.tail = 0.05f + Mathf.Sin(wk * 0.5f) * 0.25f;
                    break;
                }
                case State.Crouch: p.front = 0.55f; p.farFront = 0.5f; p.back = -0.6f; p.farBack = -0.55f; p.bob = 6; p.sy = 0.75f; p.sx = 1.12f; p.head = 0.3f; p.tail = -0.4f; break;
                case State.Air:
                    if (thrown) Flail(seed);
                    else if (vz > 0) { p.front = 2.5f; p.farFront = 2.3f; p.back = -1.7f; p.farBack = -1.5f; p.head = -0.5f; p.tail = -1.4f; p.sx = 0.85f; p.sy = 1.25f; }
                    else { p.front = 1.2f; p.farFront = 1; p.back = -1.1f; p.farBack = -0.9f; p.head = -0.2f; p.tail = 1.1f; }
                    break;
                case State.Held: Flail(seed); break;
                case State.Land: p.front = 0.9f; p.farFront = -0.4f; p.back = -1.2f; p.farBack = 1; p.head = 0.4f; p.tail = 1.2f; p.bob = 4; p.sy = 1 - 0.22f * squash; p.sx = 1 + 0.15f * squash; break;
                case State.Dizzy: p.head = Mathf.Sin(t * 6) * 0.25f; p.tilt = Mathf.Sin(t * 5) * 0.12f; p.tail = -0.3f; break;
                case State.Sleep: p.front = 1.5f; p.farFront = 1.5f; p.back = -1.5f; p.farBack = -1.5f; p.head = 0.4f; p.tail = -0.6f + Mathf.Sin(t * 0.8f) * 0.08f; p.sy = 0.92f + Mathf.Sin(t * 2) * 0.02f; break;
                case State.Tea: p.front = sip ? 1.4f : 0.5f; p.farFront = sip ? 0.6f : 0.3f; if (sip) p.head = -0.25f; break;
                case State.Eat: { float b = Mathf.Max(0, Mathf.Sin(t * 14 + seed)); p.headX = -4 * b; p.head = 0.2f * b; p.front = 0.4f + 0.3f * b; p.farFront = 0.3f; break; }
                case State.Lift: p.front = 2.2f + liftK * 0.6f; p.farFront = 2.0f + liftK * 0.6f; p.head = -0.15f; p.tilt = -0.08f; p.back = -0.2f; p.farBack = 0.2f; p.sy = 1 - 0.04f * liftK; break;
            }
            return p;
        }

        // ── 그리기 ── 반환: 몸길이 (월드 유닛)
        public float Len => home.ratLen * home.Width * size;
        public Vector3 GroundPos => home.P(u, v);
        public Vector3 BodyPos => home.P(u, v) + Vector3.up * home.Height * z;

        public void Draw(float t, int order)
        {
            float len = Len, scale = len / (rigLength * World.U);
            transform.position = BodyPos;
            bool spinning = state == State.Held || (state == State.Air && thrown);
            float pivotH = len * 0.3f / World.U / scale;
            rig.Apply(PoseOf(t), scale, face, 1, order, 0, spinning ? rot : 0, 1, 1, spinning ? pivotH * scale : 0);
            // 그림자: 바닥에 (높이 올라가면 작고 옅게)
            if (shadow)
            {
                shadow.transform.position = GroundPos;
                float k = Mathf.Clamp(1 - z * 2, 0.5f, 1), sz = shadow.sprite ? shadow.sprite.bounds.size.x : 1;
                shadow.transform.localScale = new Vector3(len * 0.8f * k / sz, len * 0.18f / sz, 1);
                shadow.color = new Color(0.17f, 0.13f, 0.09f, 0.2f * Mathf.Clamp(1 - z * 4, 0.3f, 1));
                shadow.sortingOrder = order - 1;
            }
            DrawHeld(len, order);
            DrawDizzy(t, len, order);
        }

        // 던져져서 헤롱헤롱: 머리 위에 별 3개가 빙글빙글 (게임 쥐 기절 별과 같은 모양)
        SpriteRenderer[] stars;
        void DrawDizzy(float t, float len, int order)
        {
            bool on = (state == State.Dizzy || (state == State.Land && dizzy > 0)) && home.dizzyStar;
            if (!on) { if (stars != null && stars[0].enabled) foreach (var s in stars) s.enabled = false; return; }
            if (stars == null)
            {
                stars = new SpriteRenderer[3];
                for (int i = 0; i < 3; i++) { var go = new GameObject("DizzyStar"); go.transform.SetParent(transform, false); stars[i] = go.AddComponent<SpriteRenderer>(); stars[i].sprite = home.dizzyStar; stars[i].color = home.dizzyStarColor; }
            }
            float w = len * home.dizzyStarSize / Mathf.Max(0.001f, home.dizzyStar.bounds.size.x);
            for (int i = 0; i < 3; i++)
            {
                float a = t * 8 + i * 2.09f, s = Mathf.Sin(a);
                var sr = stars[i]; sr.enabled = true;
                sr.transform.localPosition = new Vector3(Mathf.Cos(a) * len * 0.26f + face * len * 0.18f, len * 0.62f + s * len * 0.08f, 0);
                sr.transform.localScale = Vector3.one * w * (0.85f + 0.15f * s);
                sr.transform.localRotation = Quaternion.Euler(0, 0, t * 200 + i * 40);
                sr.sortingOrder = order + (s > 0 ? -1 : 30);
            }
        }

        // 손에 든 것: 치즈(먹기) · 아령(들기) · 찻잔(차)
        void DrawHeld(float len, int order)
        {
            if (!held) return;
            Sprite s = state switch { State.Eat => home.heldCheese, State.Lift => home.heldDumbbell, State.Tea => home.heldCup, _ => null };
            held.enabled = s; if (!s) return;
            held.sprite = s; held.sortingOrder = order + 20;
            Vector3 b = BodyPos; float w, r = 0; Vector3 off;
            if (state == State.Eat) { w = len * 0.42f; off = new Vector3(face * len * 0.58f, len * 0.16f); }
            else if (state == State.Lift) { w = len * 0.8f; off = new Vector3(face * len * 0.18f, len * (0.78f + 0.22f * liftK)); }
            else { w = len * 0.2f; off = new Vector3(face * len * 0.5f, len * (sip ? 0.38f : 0.12f)); r = sip ? face * 0.5f : 0; }
            held.transform.position = b + off;
            held.transform.rotation = Quaternion.Euler(0, 0, r * Mathf.Rad2Deg);
            held.transform.localScale = Vector3.one * (w / s.bounds.size.x);
        }
    }
}
