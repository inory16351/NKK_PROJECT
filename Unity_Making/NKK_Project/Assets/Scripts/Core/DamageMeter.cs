using System.Collections.Generic;
using NKK.Rats;

namespace NKK
{
    // 딜 미터기 집계 (롤체 딜 미터기): 이번 층에 쥐 종(code_id)별로 준 피해 합. 층이 바뀌면 0 부터 (StageManager 층 시작에서 Clear).
    // 대상 = 물건·가구·벽·사람·고양이·보스 전부. 실제로 깎인 체력만 셈 (남은 체력보다 큰 피해는 남은 체력까지).
    // 날아간 물건·사람이 다른 걸 맞히면 그걸 날린 쥐(By) 몫.
    public static class DamageMeter
    {
        static readonly Dictionary<string, double> dealt = new();
        public static IReadOnlyDictionary<string, double> Dealt => dealt;
        public static double Total { get; private set; }
        public static int Version { get; private set; }          // 바뀔 때마다 +1 (UI 가 다시 그릴지 판단)

        public static void Add(Rat by, float dmg)
        {
            if (!by || dmg <= 0 || string.IsNullOrEmpty(by.codeId)) return;
            dealt.TryGetValue(by.codeId, out var v);
            dealt[by.codeId] = v + dmg; Total += dmg; Version++;
        }

        // 남은 체력까지만 (넘친 피해 제외)
        public static void Add(Rat by, float dmg, float hpBefore) => Add(by, System.Math.Min(dmg, System.Math.Max(0, hpBefore)));

        public static double Of(string codeId) => codeId != null && dealt.TryGetValue(codeId, out var v) ? v : 0;

        public static void Clear() { dealt.Clear(); Total = 0; Version++; }
    }
}
