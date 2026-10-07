using UnityEngine;

namespace NKK.Ults
{
    // 슈퍼 생쥐 · (웹게임 recoil): 빔을 쐈더니 반동으로 본인이 날아감 → 벽에 튕기며 빙글빙글 난사, 빔에 닿은 물건은 날아감
    // 자막 c1~c4, c5 = 끝 '(철푸덕)' 팝업
    // 빔 = 웹 drawBigBeam (번진 빛 1.8배 · 색 몸통 · 흰 심지 0.4배, 굵기 38±5 꿈틀) → FX 폴더 소품 fx_beam_body/flare/hit 를 늘려서 매 프레임 따라감
    public class UltRecoil : UltBase
    {
        const float LEN = 850, BEAM_W = 34, BIG_W = 38, PAW = 22, ORDER = 31500;
        public override float Dur => 6.2f;
        float aim, spin, ox, oy, ovx, ovy;
        UltProp glow, body, core, flare, flareCore, hit, hitCore;

        public override void Begin()
        {
            Beat(0.35f, () => Cap("c1"));
            Beat(1.3f, () => Cap("c2"));
            Beat(3.4f, () => Cap("c3"));
            Beat(5.3f, () => Cap("c4"));
            aim = AimMost();
            ox = R.x; oy = R.y; ovx = -Mathf.Cos(aim) * 620; ovy = -Mathf.Sin(aim) * 620;
            // 빔 소품 (그림이 없으면 null → FxManager.BigBeam 으로 대신)
            glow = BeamProp("fx_beam_body"); body = BeamProp("fx_beam_body"); core = BeamProp("fx_beam_body");
            flare = BeamProp("fx_beam_flare"); flareCore = BeamProp("fx_beam_flare");
            hit = BeamProp("fx_beam_hit"); hitCore = BeamProp("fx_beam_hit");
            // 첫 발사: 총구 번쩍
            float px = R.x + Mathf.Cos(aim) * PAW, py = R.y + Mathf.Sin(aim) * PAW;
            Fx?.Burst(px, py, 40, 14, Color.white, Col, 200, 480);
            Fx?.Spark(px, py, 40, Col, 150, 0.3f); Fx?.Spark(px, py, 40, Color.white, 90, 0.2f);
            Fx?.Ring(R.x, R.y, 90, Col, 0.3f); Fx?.Ring(R.x, R.y, 140, Color.white, 0.35f); Fx?.Shake(0.25f); Fx?.Hitstop(0.06f); Flash(Col, 0.25f);
        }

        UltProp BeamProp(string name)
        {
            var p = Prop(name, R.x, R.y, 0, 100); if (p == null) return null;
            p.visible = false;
            return p;
        }

        public override void Step(float dt, float k)
        {
            spin += dt * (3 + 7 * Mathf.Min(1, T / 3));                  // 3초 동안 점점 빨라진 뒤 최고 속도로 계속
            // 빔 반동: 벽에 튕김, 일정 속도 밑으로는 안 느려짐
            float pvx = ovx, pvy = ovy;
            BounceMove(ref ox, ref oy, ref ovx, ref ovy, R.Radius, dt);
            if (Mathf.Sign(pvx) != Mathf.Sign(ovx) || Mathf.Sign(pvy) != Mathf.Sign(ovy)) { Fx?.Ring(ox, oy, 70, Color.white, 0.25f); Fx?.Dust(ox, oy, 4, 1); Fx?.Shake(0.08f); }
            if (Mathf.Sqrt(ovx * ovx + ovy * ovy) > 340) { ovx *= 1 - dt * 0.2f; ovy *= 1 - dt * 0.2f; }
            R.x = ox; R.y = oy; R.z = 28 + Mathf.Sin(Time.time * 9) * 8; R.UltRot = -spin * 0.6f;
            R.UltPose = P(tilt: -0.3f, front: 1.5f, farFront: 1.3f, back: -1.4f, farBack: -1.2f, head: -0.3f, tail: -1);

            // 빔: 앞발에서 뻗어 나감
            float a = aim + spin, ux = Mathf.Cos(a), uy = Mathf.Sin(a), bz = R.z + 14;
            R.face = ux >= 0 ? 1 : -1;
            float sx = R.x + ux * PAW, sy = R.y + uy * PAW, ex = R.x + ux * LEN, ey = R.y + uy * LEN;
            DrawBeam(sx, sy, ex, ey, bz, dt);
            if (Random.value < 0.5f) Fx?.Burst(sx, sy, bz, 2, Color.white, Col, 100, 260, 3, 6);    // 총구 불꽃
            if ((hitT -= dt) > 0) return;
            hitT = 0.08f;
            // 빔 줄 위 물건 → 날아감
            foreach (var it in ItemMgr.OnLine(R.x, R.y, ux, uy, LEN, BEAM_W, 99))
            {
                FlingItem(it, a + Rand(-0.4f, 0.4f), 480, 420);
                if (OnScreen(it.x, it.y)) { Fx?.Anim("zap", it.x, it.y, 10, 0.7f); Fx?.Spark(it.x, it.y, 20, Col, 80, 0.2f); }
            }
            foreach (var q in RatsNear(R.x, R.y, 50)) Ragdoll(q, Mathf.Atan2(q.y - R.y, q.x - R.x));
            if (OnScreen(ex, ey)) { Fx?.Burst(ex, ey, bz, 4, Color.white, Col, 120, 320); Fx?.Ring(ex, ey, 40, Col, 0.2f); }
            Fx?.Shake(0.05f);
        }

        // 웹 drawBigBeam 3겹 + 총구·끝 번쩍 (소품을 매 프레임 맞춤)
        void DrawBeam(float sx, float sy, float ex, float ey, float bz, float dt)
        {
            float w = BIG_W + Mathf.Sin(T * 40) * 5;
            if (body == null) { Fx?.BigBeam(sx, sy, bz, ex, ey, bz, Col, dt * 1.5f, w); return; }
            Vector3 a = World.ToUnity(sx, sy, bz), b = World.ToUnity(ex, ey, bz), d = b - a;
            float len = d.magnitude / World.U, rot = Mathf.Atan2(d.y, d.x), mx = (sx + ex) * 0.5f, my = (sy + ey) * 0.5f;
            Stretch(glow, 0, mx, my, bz, len, w * 1.8f, rot, Col, 0.45f * Rand(0.85f, 1));
            Stretch(body, 1, mx, my, bz, len, w, rot, Col, 0.9f);
            Stretch(core, 2, mx, my, bz, len, w * 0.4f, rot, Color.white, 1);
            float f = w * Rand(2.5f, 2.9f), h = w * Rand(2.1f, 2.6f);
            Blob(flare, 3, sx, sy, bz, f, rot + Rand(-0.15f, 0.15f), Col, 0.9f);
            Blob(flareCore, 4, sx, sy, bz, f * 0.55f, rot + Rand(-0.3f, 0.3f), Color.white, 1);
            Blob(hit, 3, ex, ey, bz, h, Rand(0, Mathf.PI * 2), Col, 0.85f);
            Blob(hitCore, 4, ex, ey, bz, h * 0.5f, Rand(0, Mathf.PI * 2), Color.white, 1);
        }
        void Stretch(UltProp p, int layer, float x, float y, float z, float len, float thick, float rot, Color c, float alpha)
        {
            if (p == null || !p.r || !p.r.sprite) return;
            var s = p.r.sprite.bounds.size;
            p.x = x; p.y = y; p.z = z; p.w = len; p.flat = thick / (len * s.y / s.x); p.rot = rot;
            Look(p, layer, c, alpha);
        }
        void Blob(UltProp p, int layer, float x, float y, float z, float size, float rot, Color c, float alpha)
        {
            if (p == null) return;
            p.x = x; p.y = y; p.z = z; p.w = size; p.flat = 1; p.rot = rot;
            Look(p, layer, c, alpha);
        }
        static void Look(UltProp p, int layer, Color c, float alpha)
        {
            p.tint = c; p.alpha = alpha; p.visible = true;
            p.sortBias = (int)ORDER + layer - World.SortOrder(p.y);     // 화면 맨 앞 (레이저 템플릿과 같은 순서), layer = 겹 순서
        }

        public override void Finish()
        {
            R.z = 0; R.UltRot = 0;
            foreach (var p in new[] { glow, body, core, flare, flareCore, hit, hitCore }) if (p != null) p.visible = false;
            PopupCap("c5", R.x, R.y, Color.white, 20, 1.2f, 40);
            Fx?.Dust(R.x, R.y, 8, 1.2f); Fx?.Ring(R.x, R.y, 80, Color.white, 0.3f); Fx?.Shake(0.15f);
        }
    }
}
