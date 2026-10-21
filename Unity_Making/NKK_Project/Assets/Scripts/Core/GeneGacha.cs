using System;
using System.Collections.Generic;
using NKK.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NKK
{
    // 유전자 뽑기 (기획서/20261012_프로젝트 NKK_뽑기 시스템 기획서_Ver02): 연구자료로 쥐 조각 캡슐을 뽑는 아지트 기계 (화면 = 유전자 추출기 믹서기).
    //   해금 = 16층 첫 클리어 (첫 10회 무료) · 1회 0.2 BR · 10회 1.8 BR (유니크 이상 1개 보장)
    //   천장: 전설 이상 30회 · 신화 100회 (영구 누적) · 관심 종 (18층~ 50%, 20층~ 75%)
    //   판정 순서 = 신화 천장 → 전설 천장 → 10회 보장(마지막 캡슐) → 확률표. 종 = 그 등급 해금(훈장) 종 균등 (못 만난 종도 나옴 → 도감 등록)
    // BR = 스테이지 테이블 research (최고 도달 층). 결과는 연출 전에 저장 (Progress.gacha.pending)
    public static class GeneGacha
    {
        public class Result { public string code; public Grade grade; public int shards; public bool isNew, wish; public string source; }

        static GameDatabase Db => GameDatabase.Instance;
        static Progress P => Progress.I;
        static float Cfg(string k, float d) => Db ? Db.GachaCfg(k, d) : d;
        public static bool TestOpen;          // 테스트: 층 조건 무시

        public static bool Unlocked => P && (TestOpen || P.ClearedFloor >= (int)Cfg("unlock_floor", 16));
        public static bool WishUnlocked => P && (TestOpen || P.ClearedFloor >= (int)Cfg("wish_unlock_floor", 18));
        public static float WishRate => P && P.ClearedFloor >= (int)Cfg("wish_rate_up_floor", 20) ? Cfg("wish_rate_up", 0.75f) : Cfg("wish_rate", 0.5f);

        public static double BR { get { var st = Db ? Db.StageOf(P ? P.maxFloor : 1) : null; return st != null && st.research > 0 ? st.research : 8; } }
        public static double CostOne => Math.Max(1, Math.Ceiling(Cfg("cost_single_br", 0.2f) * BR));
        public static double CostTen => CostOne * Cfg("ten_pull_cost_mul", 9);
        public static int LegendPityMax => (int)Cfg("legend_pity_count", 30);
        public static int MythicPityMax => (int)Cfg("mythic_pity_count", 100);

        // 처음 해금되면 무료 10회 (first_free_ten)
        public static int FreeTen
        {
            get
            {
                if (!Unlocked) return 0;
                if (P.gacha.freeTen < 0) { P.gacha.freeTen = (int)Cfg("first_free_ten", 1); P.Save(); }
                return P.gacha.freeTen;
            }
        }
        public static bool CanPull(int n) => Unlocked && (n == 10 && FreeTen > 0 || P.research >= (n == 10 ? CostTen : CostOne * n));

        // 뽑기: 연구자료를 내고 결과를 확정해 저장 (연출은 화면이 pending 으로)
        public static List<Result> Pull(int n)
        {
            var res = new List<Result>();
            if (!CanPull(n)) return res;
            if (n == 10 && FreeTen > 0) P.gacha.freeTen--;
            else P.research -= n == 10 ? CostTen : CostOne * n;
            var g = P.gacha; var rows = Db.Gacha.Gacha_Grade;
            for (int i = 0; i < n; i++)
            {
                g.legendPity++; g.mythicPity++;
                Grade gr; string src;
                if (g.mythicPity >= MythicPityMax) { gr = Grade.Mythic; src = "Pity_Mythic"; }
                else if (g.legendPity >= LegendPityMax) { gr = Grade.Legendary; src = "Pity_Legend"; }
                else if (n == 10 && i == n - 1 && !res.Exists(r => (int)r.grade >= (int)Cfg("ten_guarantee_grade", 3))) { gr = Roll(rows, true); src = "Guarantee"; }
                else { gr = Roll(rows, false); src = "Normal"; }
                if (gr >= Grade.Legendary) g.legendPity = 0;
                if (gr == Grade.Mythic) g.mythicPity = 0;
                var r = new Result { grade = gr, source = src };
                r.code = PickSpecies(ref r.grade, out r.wish);
                if (r.wish) r.source = "Wish";
                r.shards = ShardsOf(r.grade);
                if (r.code != null) { r.isNew = P.AddShards(r.code, r.shards); res.Add(r); }
            }
            // 기록 (최근 log_keep 개) · 연출 전 결과
            g.total += res.Count; g.pending.Clear();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var r in res)
            {
                var l = new Progress.GachaLog { at = now, maxFloor = P.maxFloor, code = r.code, grade = r.grade.ToString(), source = r.source, shards = r.shards, isNew = r.isNew };
                g.log.Add(l); g.pending.Add(l);
            }
            int keep = (int)Cfg("log_keep", 100); if (g.log.Count > keep) g.log.RemoveRange(0, g.log.Count - keep);
            P.Save();
            return res;
        }
        // 연출을 다 봄 → 복구용 결과 지움
        public static void ClearPending() { if (P && P.gacha.pending.Count > 0) { P.gacha.pending.Clear(); P.Save(); } }

        // 조각 개수 = 테이블 shard_count × 훈장 ÷ shard_full_tier (16훈장 = 테이블 값, 최소 1, 2026-10-21)
        public static int ShardsOf(Grade g)
        {
            int n = 1; if (Db?.Gacha.Gacha_Grade != null) foreach (var r in Db.Gacha.Gacha_Grade) if (r.Grade == g) { n = r.shard_count; break; }
            return Mathf.Max(1, Mathf.RoundToInt(n * BlackMarket.TierK(Cfg("shard_full_tier", 16))));
        }
        public static float RateOf(Grade g) { if (Db?.Gacha.Gacha_Grade != null) foreach (var r in Db.Gacha.Gacha_Grade) if (r.Grade == g) return r.rate; return 0; }

        static Grade Roll(List<GachaGradeRow> rows, bool guarantee)
        {
            float sum = 0; foreach (var r in rows) sum += guarantee ? r.guarantee_rate : r.rate;
            float x = Random.value * sum;
            foreach (var r in rows) { float w = guarantee ? r.guarantee_rate : r.rate; if (w > 0 && (x -= w) <= 0) return r.Grade; }
            return guarantee ? Grade.Unique : Grade.Common;
        }

        // 종 = 그 등급 해금(훈장) 종 균등. 관심 종이 그 등급이면 WishRate 로. 그 등급에 해금 종이 없으면 아래 등급으로
        static string PickSpecies(ref Grade g, out bool wish)
        {
            wish = false;
            for (int gi = (int)g; gi >= 0; gi--)
            {
                var pool = new List<RatCharacterRow>();
                foreach (var r in Db.Rats.Values) if ((int)r.Grade == gi && r.unlock_rank <= P.tier) pool.Add(r);
                if (pool.Count == 0) continue;
                g = (Grade)gi;
                string w = g == Grade.Legendary ? P.gacha.wishLegend : g == Grade.Mythic ? P.gacha.wishMythic : null;
                if (WishUnlocked && !string.IsNullOrEmpty(w) && pool.Count > 1 && pool.Exists(r => r.code_id == w) && Random.value < WishRate) { wish = true; return w; }
                return pool[Random.Range(0, pool.Count)].code_id;
            }
            return null;
        }

        // 관심 종 지정 (해당 등급 해금 종만)
        public static void SetWish(Grade g, string code)
        {
            if (!P) return;
            if (g == Grade.Legendary) P.gacha.wishLegend = code; else if (g == Grade.Mythic) P.gacha.wishMythic = code;
            P.Save();
        }
        public static List<RatCharacterRow> WishPool(Grade g)
        {
            var l = new List<RatCharacterRow>();
            if (Db && P) foreach (var r in Db.Rats.Values) if (r.Grade == g && r.unlock_rank <= P.tier) l.Add(r);
            l.Sort((a, b) => a.character_id.CompareTo(b.character_id));
            return l;
        }
    }
}
