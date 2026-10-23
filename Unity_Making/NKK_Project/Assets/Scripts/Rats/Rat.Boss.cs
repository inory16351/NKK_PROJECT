using UnityEngine;

namespace NKK.Rats
{
    // 보스 효과가 쥐에게 남기는 것: 머리 바꾸기 (트랄랄레로 상어 · 발레리나 커피잔) · 빨갛게 칠하기 (불판)
    // · 삼켜짐 (우주복 쥐 촉수): 몸을 숨기고 보스 입 자리에 붙어 있다가 뱉어짐. 쥐 목록에 그대로 있어 최대 마릿수(인구)에 계속 포함 (사용자 2026-10-22)
    // · 멋대로 걷기 (미니 버티컬 마우스): 조종 불가로 아무 데나 돌아다님 (공격·번식 안 함, 인구 그대로)
    // · 방독면 (방구가스 면역)
    // 남은 시간은 쥐가 직접 셈 → 보스가 사라지거나 층이 바뀌어도 시간이 지나면 반드시 원래대로 돌아감
    public partial class Rat
    {
        Sprite headSwap; float headSwapT, burnT; bool burnTinted;
        [HideInInspector] public float swallowT, wanderT, gasImmuneT, grabT;
        float wanderTurn, spitFace;
        public bool Swallowed => swallowT > 0;
        public bool Grabbed => grabT > 0;

        // t 초 동안 촉수에 잡힘: 보스가 매 프레임 x·y·z 를 정함 (입으로 끌려감, 보임) → 보통 Swallow 로 이어짐
        public void Grab(float t)
        {
            if (UltOn || swallowT > 0) return;
            Stun(t); grabT = Mathf.Max(grabT, t); vx = vy = vz = 0; rushT = 0;
        }

        // t 초 동안 삼켜짐 (보스가 매 프레임 HoldAt 으로 입 자리를 알려 줌). 끝나면 Spit
        public void Swallow(float t, float px, float py)
        {
            if (UltOn) return;
            grabT = 0; swallowT = Mathf.Max(swallowT, t); x = px; y = py; vx = vy = vz = 0; z = 0; rushT = 0; HideBody = true;
        }
        // 입 자리 (pz = 높이, face = 보스가 보는 쪽 → 그쪽으로 뱉음, 0 = 아무 쪽)
        public void HoldAt(float px, float py, float pz = 0, float face = 0) { if (swallowT > 0) { x = px; y = py; z = pz; spitFace = face; } }

        void Spit()
        {
            swallowT = 0; HideBody = false;
            float a = spitFace == 0 ? Random.Range(0, Mathf.PI * 2) : (spitFace > 0 ? 0 : Mathf.PI) + Random.Range(-0.9f, 0.9f);
            Ragdoll(a, Random.Range(260f, 420f), Random.Range(380f, 560f), 0.8f);
        }

        // t 초 동안 조종 불가로 멋대로 돌아다님 (머리 = s, 없으면 그대로)
        public void Wander(float t, Sprite s = null)
        {
            wanderT = Mathf.Max(wanderT, t); wanderTurn = 0; rushT = 0;
            if (s) SwapHead(s, t);
        }

        // Tick 맨 앞: 삼켜짐·멋대로 걷기면 여기서 처리하고 true (평소 움직임 건너뜀)
        bool BossStateTick(float dt)
        {
            gasImmuneT = Mathf.Max(0, gasImmuneT - dt);
            if (grabT > 0) { grabT -= dt; vx = vy = vz = 0; noBreed = Mathf.Max(noBreed, 0.3f); return true; }      // 촉수가 끌고 감 (보스가 자리를 정함)
            if (swallowT > 0)
            {
                if ((swallowT -= dt) <= 0) Spit();
                else { vx = vy = 0; noBreed = Mathf.Max(noBreed, 0.3f); return true; }
            }
            if (wanderT > 0 && stun <= 0)
            {
                wanderT -= dt; noBreed = Mathf.Max(noBreed, 0.3f); rushT = 0;
                if ((wanderTurn -= dt) <= 0)
                {
                    wanderTurn = Random.Range(0.25f, 0.6f);
                    float a = Random.Range(0, Mathf.PI * 2), sp = RunSpeed * Random.Range(1.4f, 2.2f);
                    vx = Mathf.Cos(a) * sp; vy = Mathf.Sin(a) * sp; mode = Mode.Run;
                }
                Move(dt, false, false);
                return true;
            }
            return false;
        }

        // 머리 그림 자체를 t 초 동안 바꿈 (그림 한 장짜리 쥐는 머리가 따로 없어서 false → 보스가 위에 표시를 씌움)
        public bool SwapHead(Sprite s, float t)
        {
            if (!s || !rig || rig.IsSingle) return false;
            headSwap = s; headSwapT = Mathf.Max(headSwapT, t);
            return true;
        }

        // t 초 동안 빨갛게 (불판 위)
        public void Burn(float t) { burnT = Mathf.Max(burnT, t); }

        void BossEffects(float dt)
        {
            headSwapT = Mathf.Max(0, headSwapT - dt); burnT = Mathf.Max(0, burnT - dt);
            rig.headOverride = headSwapT > 0 ? headSwap : null;
            bool on = burnT > 0;
            if (on)
            {
                var c = Color.Lerp(Color.white, Manager.burnTint, 0.75f + 0.25f * Mathf.Sin(Time.time * 18));
                foreach (var sr in rig.GetComponentsInChildren<SpriteRenderer>()) { var k = sr.color; sr.color = new Color(c.r, c.g, c.b, k.a); }
            }
            else if (burnTinted)
            {
                // 원래 색 (좀비면 좀비 색)
                rig.ResetColors();
                zombieTinted = false; ZombieTint();
            }
            burnTinted = on;
        }
    }
}
