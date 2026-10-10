using UnityEngine;

namespace NKK.Rats
{
    // 보스 효과가 쥐에게 남기는 것: 머리 바꾸기 (트랄랄레로 상어 · 발레리나 커피잔) · 빨갛게 칠하기 (불판)
    // 남은 시간은 쥐가 직접 셈 → 보스가 사라지거나 층이 바뀌어도 시간이 지나면 반드시 원래대로 돌아감
    public partial class Rat
    {
        Sprite headSwap; float headSwapT, burnT; bool burnTinted;

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
