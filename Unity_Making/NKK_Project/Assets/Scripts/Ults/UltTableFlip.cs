using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK.Ults
{
    // 쥐 여왕 · (웹게임 tableflip): 주변 물건을 식탁 위로 모아 티타임 → 홀짝 → 밥상 뒤집기! 3초에 새 탁자를 차리고 한 판 더
    // 자막 c1~c5, c6 = 첫 판 '홀짝~' 말풍선 · c7 = 둘째 판 '홀짝… (부들부들)' 말풍선
    public class UltTableFlip : UltBase
    {
        const float TABLE_W = 240, ROUND = 3, GATHER = 0.7f, FLIP = 1.55f;
        public override float Dur => 6.1f;

        class Seat { public Item it; public float hx, hy, tx, ty, tz; }
        readonly List<Seat> seats = new();
        float c0, tbx, tby, flipT = -1, tableH = 127, top = 107;
        bool again, said;
        UltProp table, shadow;
        static readonly Color Pale = new(1f, 0.95f, 0.75f), Rose = new(0.95f, 0.72f, 0.69f);

        public override void Begin()
        {
            Beat(0.2f, () => Cap("c1"));
            Beat(1.0f, () => Cap("c2"));
            Beat(1.55f, () => Cap("c3"));
            Beat(3.2f, () => Cap("c4"));
            Beat(4.55f, () => Cap("c5"));
            Setup(26);
        }

        // 탁자 차리기 + 물건 n개를 탁자 위로
        void Setup(int n)
        {
            c0 = T; flipT = -1; said = false;
            tbx = R.x - R.face * 70; tby = R.y + 6;
            KillProp(table); KillProp(shadow);
            shadow = Prop("dot", tbx, tby, 0, 200);
            if (shadow != null) { shadow.ground = true; shadow.flat = 0.21f; shadow.tint = new Color(0.12f, 0.06f, 0.02f, 0.22f); }
            table = Prop("tea_table", tbx, tby, 0, TABLE_W);
            // 탁자 윗면 높이: 다리 끝에서 이미지 높이의 약 84% 위
            if (table != null && table.r && table.r.sprite) { var b = table.r.sprite.bounds.size; tableH = TABLE_W * b.y / b.x; top = tableH * 0.84f; }
            seats.Clear();
            var l = ItemsIn(R.x, R.y, ULT_R);
            for (int i = 0; i < l.Count && i < n; i++)
            {
                var it = l[i];
                if (!GrabItem(it)) continue;
                seats.Add(new Seat { it = it, hx = it.x, hy = it.y, tx = tbx + Rand(-75, 75), ty = tby + Rand(2, 8), tz = top + Rand(-6, 10) });
                if (it.shadow) it.shadow.enabled = false;      // 탁자 위에 있는 동안은 바닥 그림자 없음
            }
            if (c0 > 0) { Smoke(tbx, tby); Fx?.Anim("poof", tbx, tby, 0, 1.3f); }
            Fx?.Stars(tbx, tby, top, 8, Color.white, Rose, 80, 220);
            Sync();
        }

        public override void Step(float dt, float k)
        {
            if (T >= ROUND && !again) { again = true; Setup(14); }      // 두 번째 판은 남은 물건 14개까지
            float lt = T - c0;
            if (lt < FLIP)
            {
                float e = Ease(Mathf.Min(1, lt / GATHER));
                foreach (var s in seats) if (s.it) { s.it.x = Mathf.Lerp(s.hx, s.tx, e); s.it.y = Mathf.Lerp(s.hy, s.ty, e); s.it.z = s.tz * e + Mathf.Sin(e * Mathf.PI) * 120; }
                R.UltPose = lt > 0.8f ? P(head: 0.2f, front: 1.8f, farFront: 0.4f, tilt: -0.1f) : P(head: -0.1f, front: 0.6f);
                if (lt > 0.8f && !said) { said = true; PopupCap(again ? "c7" : "c6", R.x, R.y, Color.white, 18, 0.75f, 60); }
                if (again && lt > 0.8f) R.UltJit = 1.5f;                // 부들부들
            }
            else if (flipT < 0)
            {
                // 밥상 뒤집기!
                flipT = T; R.UltJit = 0;
                float d = -R.face;
                foreach (var s in seats) if (s.it) { if (s.it.shadow) s.it.shadow.enabled = true; DropItem(s.it, d * Rand(450, 800), Rand(-220, 220), Rand(420, 680)); }
                seats.Clear();
                foreach (var o in RatsNear(tbx, tby, 160)) Ragdoll(o, d > 0 ? Rand(-0.5f, 0.5f) : Mathf.PI + Rand(-0.5f, 0.5f));
                R.UltPose = P(tilt: -0.6f, front: 2.8f, farFront: 2.6f, head: -0.5f, tail: 1.3f);
                Fx?.Ring(tbx, tby, 160, Rose, 0.35f); Fx?.Ring(tbx, tby, 240, Color.white, 0.45f);
                Fx?.Burst(tbx, tby, 60, 18, new Color(0.69f, 0.54f, 0.41f), Color.white, 220, 560); Fx?.Dust(tbx, tby, 10, 1.6f);
                Fx?.Stars(tbx, tby, 80, 12, Color.white, Pale, 200, 480);
                Fx?.Shake(again ? 0.5f : 0.35f); Fx?.Hitstop(again ? 0.08f : 0.05f); Flash(Color.white, 0.2f);
            }
            Sync();
        }

        // 탁자 그림: 뒤집히면 날아가며 빙글 + 흐려짐
        void Sync()
        {
            if (table == null) return;
            if (flipT >= 0)
            {
                float ft = T - flipT;
                table.x = tbx - R.face * ft * 500; table.y = tby; table.z = tableH / 2 + Mathf.Sin(Mathf.Min(1, ft) * Mathf.PI) * 140;
                table.rot = R.face * ft * 9; table.alpha = Mathf.Max(0, 1 - ft); table.sortBias = 80;
                if (shadow != null) shadow.visible = false;
            }
            else { table.x = tbx; table.y = tby; table.z = tableH / 2; table.rot = 0; table.alpha = 1; table.sortBias = 0; }
        }

        public override void Cleanup() { foreach (var s in seats) if (s.it && s.it.shadow) s.it.shadow.enabled = true; }

        public override void Finish()
        {
            foreach (var s in seats) if (s.it && s.it.shadow) s.it.shadow.enabled = true;
            seats.Clear(); R.UltJit = 0;
            KillProp(table); KillProp(shadow); table = shadow = null;
        }
    }
}
