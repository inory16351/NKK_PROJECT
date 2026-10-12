using UnityEngine;

namespace NKK
{
    // 빙결 (아이스 퀸 쥐 '서리 축적', 2026-10-12 캐릭터 상세 기획서): 생명체(고양이 · 보스 · 사람)마다 하나.
    // 맞을 때마다 스택 +1 → 스택만큼 느려짐 (그 생명체의 시간이 느리게 감). 최대 스택이면 잠깐 꽁꽁 (시간 멈춤) 뒤 스택 0.
    // 한동안(keep 초) 안 맞으면 스택이 풀림. 렛잇고 필살기는 FreezeNow 로 바로 꽁꽁.
    public class Frost
    {
        public int stacks;
        public float frozenT;
        float keepT, slowPer, fxT;
        public bool Frozen => frozenT > 0;
        public static readonly Color Ice = new(0.62f, 0.86f, 1f), IceLight = new(0.9f, 0.97f, 1f);

        // n 스택 쌓기. slow = 스택당 느려짐 비율, max = 최대 스택, freeze = 최대일 때 꽁꽁 시간, keep = 스택 유지 시간. 반환 = 이번에 꽁꽁 됨
        public bool Add(int n, float slow, int max, float freeze, float keep, float x, float y, float h, string popup)
        {
            if (Frozen || n <= 0) return false;
            slowPer = slow; keepT = keep; stacks += n;
            var fx = FxManager.I;
            if (stacks < Mathf.Max(1, max)) { fx?.Burst(x, y, h * 0.5f, 3, Ice, IceLight, 40, 120, 3, 6); return false; }
            FreezeNow(freeze, x, y, h, popup);
            return true;
        }

        public void FreezeNow(float t, float x, float y, float h, string popup = null)
        {
            bool was = Frozen;
            frozenT = Mathf.Max(frozenT, t); stacks = 0;
            if (was) return;
            var fx = FxManager.I;
            if (!fx) return;
            fx.Ring(x, y, h * 1.1f, Ice, 0.5f);
            fx.Burst(x, y, h * 0.5f, 14, Ice, IceLight, 80, 260, 4, 9);
            if (!string.IsNullOrEmpty(popup)) fx.Popup(x, y, popup, Ice, 20, 1, h + 20);
        }

        public void Clear() { stacks = 0; frozenT = 0; }

        // 매 프레임 (생명체 Tick 맨 앞): 반환 = 이 생명체의 시간 배율 (꽁꽁 = 0)
        public float Step(float dt, float x, float y, float h)
        {
            var fx = FxManager.I;
            if (frozenT > 0)
            {
                frozenT -= dt;
                if (fx && (fxT -= dt) <= 0) { fxT = 0.12f; fx.Burst(x + Random.Range(-h, h) * 0.4f, y, Random.Range(0, h), 1, IceLight, Ice, 10, 40, 3, 6); }
                return 0;
            }
            if (stacks <= 0) return 1;
            if ((keepT -= dt) <= 0) { stacks = 0; return 1; }
            if (fx && (fxT -= dt) <= 0) { fxT = Mathf.Lerp(0.6f, 0.15f, stacks / 10f); fx.Burst(x, y, Random.Range(0, h), 1, Ice, IceLight, 10, 40, 3, 5); }
            return 1 - Mathf.Min(0.85f, stacks * slowPer);
        }
    }
}
