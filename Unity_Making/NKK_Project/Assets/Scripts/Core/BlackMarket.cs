using System;
using System.Collections.Generic;
using NKK.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NKK
{
    // 쥐구멍 암시장 (기획서/20261012_프로젝트 NKK_상점 시스템 기획서_Ver02): 남는 치즈로 전설·신화 쥐 조각을 사는 아지트 상점.
    //   해금 = 17층 첫 클리어 · 슬롯 4~6 (Shop_Floor) · 탈출 시간 15분마다 입고 (사용자 2026-10-12: 실시간 대신 판에서 보낸 시간, RunTimer 가 셈) · 치즈 리롤 (회차마다 ×1.5) · 슬롯 고정 1 · 신화 천장 8
    //   가격 = price_cheese_bc × BC, BC = bc_base × 스테이지 치즈 배율(최고 도달 층). 수치는 상점 테이블
    // 상태는 Progress.shop 에 저장 (구매·리롤 직후 저장). 화면은 Lobby/MarketPage
    public static class BlackMarket
    {
        static GameDatabase Db => GameDatabase.Instance;
        static Progress P => Progress.I;
        static float Cfg(string k, float d) => Db ? Db.ShopCfg(k, d) : d;

        public static bool Unlocked => P && (TestOpen || P.ClearedFloor >= (int)Cfg("unlock_floor", 17));
        public static bool TestOpen;          // 테스트: 층 조건 무시

        // 클리어 층 기준 상점 보정 줄 (없으면 첫 줄)
        public static ShopFloorRow FloorRow
        {
            get
            {
                if (!Db || Db.Shop.Shop_Floor == null || Db.Shop.Shop_Floor.Count == 0) return new ShopFloorRow { floor = 17, slot_count = 4 };
                ShopFloorRow best = Db.Shop.Shop_Floor[0]; int c = P ? P.ClearedFloor : 0;
                foreach (var r in Db.Shop.Shop_Floor) if (c >= r.floor && r.floor >= best.floor) best = r;
                return best;
            }
        }
        public static int SlotCount => Mathf.Max(1, FloorRow.slot_count);

        // 기준 치즈 BC
        public static double BC
        {
            get
            {
                var st = Db ? Db.StageOf(P ? P.maxFloor : 1) : null;
                return Cfg("bc_base", 3) * (st != null ? st.cheese : 1);
            }
        }
        public static double Price(ShopGoodsRow g) => g == null ? 0 : Math.Round(g.price_cheese_bc * BC);
        public static ShopGoodsRow Goods(int id) { if (Db?.Shop.Shop_Goods != null) foreach (var g in Db.Shop.Shop_Goods) if (g.goods_id == id) return g; return null; }
        public static double RerollCost => P.shop.freeLeft > 0 ? 0 : Math.Round(Cfg("reroll_base_bc", 300) * Math.Pow(Cfg("reroll_growth", 1.5f), P.shop.rerollCount) * BC);
        public static long SecondsToRefresh => P ? (long)Math.Ceiling(Math.Max(0, P.shop.playLeft)) : 0;

        // 판에서 보낸 시간만큼 입고가 가까워짐 (RunTimer, 멈춤·층 이동 연출 제외). 저장은 판이 끝날 때 같이
        public static void OnRunTime(float dt)
        {
            if (!P || !Unlocked || P.shop.lineup.Count == 0 || P.shop.playLeft <= 0) return;
            P.shop.playLeft -= dt;
        }
        public static int PityLeft => Mathf.Max(0, (int)Cfg("mythic_pity_count", 8) - P.shop.pity);

        // 아지트에서 화면을 열 때: 처음이면 라인업, 탈출 시간이 다 찼으면 한 번 갱신 (여러 주기를 넘겨도 1회). 반환: 새 라인업이 들어옴
        public static bool Tick()
        {
            if (!P || !Unlocked) return false;
            var s = P.shop;
            if (s.lineup.Count == 0 || s.playLeft <= 0) { AutoRefresh(); return true; }
            return false;
        }

        static void AutoRefresh()
        {
            var s = P.shop;
            s.playLeft = Cfg("refresh_interval_sec", 900);
            s.rerollCount = 0; s.lockedSlot = 0; s.freeLeft = FloorRow.free_reroll;
            Roll(false);
            P.Save();
        }

        // 리롤: 무료가 있으면 무료부터 (유료 회차에 안 셈). 고정 슬롯은 그대로
        public static bool Reroll()
        {
            if (!P || !Unlocked) return false;
            var s = P.shop; double cost = RerollCost;
            if (cost > 0) { if (!P.SpendCheese(cost)) return false; s.rerollCount++; }
            else s.freeLeft--;
            Roll(true);
            P.Save();
            return true;
        }

        // 슬롯 고정 (1개, 다시 누르면 해제)
        public static void ToggleLock(int slot) { if (!P) return; P.shop.lockedSlot = P.shop.lockedSlot == slot ? 0 : slot; P.Save(); }

        // 사기
        public static bool Buy(int slot, out Progress.ShopItem item)
        {
            item = null; if (!P) return false;
            foreach (var it in P.shop.lineup) if (it.slot == slot) item = it;
            if (item == null || item.sold || string.IsNullOrEmpty(item.code)) return false;
            var g = Goods(item.goods); if (g == null || !P.SpendCheese(Price(g))) return false;
            P.AddShards(item.code, g.pack_size); item.sold = true;
            P.Save();
            return true;
        }

        // 첫 방문 선물 (first_visit_goods: 전설 조각 3개) — 받은 종 코드 · 수량 (없으면 null)
        public static string FirstVisitGift(out int count)
        {
            count = 0; if (!P || !Unlocked || P.shop.opened) return null;
            P.shop.opened = true;
            var g = Goods((int)Cfg("first_visit_goods", 90001));
            var code = g != null ? PickSpecies(g.Grade, new HashSet<string>()) : null;
            if (code != null) { count = g.pack_size; P.AddShards(code, count); }
            P.Save();
            return code;
        }

        // ── 라인업 뽑기 ──
        static void Roll(bool keepLocked)
        {
            var s = P.shop; int n = SlotCount;
            var keep = new Dictionary<int, Progress.ShopItem>();
            if (keepLocked && s.lockedSlot > 0) foreach (var it in s.lineup) if (it.slot == s.lockedSlot) keep[it.slot] = it;
            s.lineup.Clear();
            var slots = Db.Shop.Shop_Slot ?? new List<ShopSlotRow>();
            int cleared = P.ClearedFloor;
            // 신화 천장: 확정 슬롯 (고정이면 다음 슬롯, 둘 다 안 되면 다음 라인업으로)
            int pitySlot = 0;
            if (s.pity >= (int)Cfg("mythic_pity_count", 8))
            {
                int ps = (int)Cfg("pity_slot_id", 4);
                for (int q = ps; q <= Mathf.Min(ps + 1, n); q++) if (!keep.ContainsKey(q)) { pitySlot = q; break; }
            }
            var used = new HashSet<string>();
            foreach (var it in keep.Values) if (!string.IsNullOrEmpty(it.code)) used.Add(it.code);
            bool anyMythic = false;
            for (int i = 1; i <= n; i++)
            {
                if (keep.TryGetValue(i, out var k)) { s.lineup.Add(k); if (Goods(k.goods)?.Grade == Grade.Mythic) anyMythic = true; continue; }
                ShopSlotRow sr = null; foreach (var r in slots) if (r.slot_id == i) sr = r;
                if (sr != null && cleared < sr.unlock_floor && !TestOpen) continue;
                Grade gr = i == pitySlot ? Grade.Mythic : sr != null && Random.value < sr.mythic_rate ? Grade.Mythic : Grade.Legendary;
                string code = PickSpecies(gr, used);
                if (code == null) { gr = gr == Grade.Mythic ? Grade.Legendary : Grade.Mythic; code = PickSpecies(gr, used); }    // 그 등급 보유 종이 없으면 다른 등급
                var g = code != null ? PickGoods(gr) : null;
                if (g != null && g.Grade == Grade.Mythic) anyMythic = true;
                if (code != null) used.Add(code);
                s.lineup.Add(new Progress.ShopItem { slot = i, goods = g != null ? g.goods_id : 0, code = code ?? "", sold = code == null });     // code 비면 '입고 대기'
            }
            s.pity = anyMythic ? 0 : s.pity + 1;
        }

        static ShopGoodsRow PickGoods(Grade g)
        {
            var l = new List<ShopGoodsRow>(); float sum = 0;
            foreach (var r in Db.Shop.Shop_Goods) if (r.Grade == g) { l.Add(r); sum += r.spawn_weight; }
            float x = Random.value * sum;
            foreach (var r in l) { if ((x -= r.spawn_weight) <= 0) return r; }
            return l.Count > 0 ? l[^1] : null;
        }

        // 종: 해금(훈장) + 도감에 있는 종. 가중치 100 × 캐치업(강화 레벨이 같은 등급 평균보다 낮음) × 신규 해금(지금 훈장에서 열린 종) · 같은 라인업 중복 0 (풀이 모자라면 허용)
        static string PickSpecies(Grade g, HashSet<string> used)
        {
            var pool = new List<RatCharacterRow>();
            foreach (var r in Db.Rats.Values) if (r.Grade == g && r.unlock_rank <= P.tier && P.Seen(r.code_id)) pool.Add(r);
            if (pool.Count == 0) return null;
            float avg = 0; foreach (var r in pool) avg += P.Level(r.code_id); avg /= pool.Count;
            bool allUsed = true; foreach (var r in pool) if (!used.Contains(r.code_id)) allUsed = false;
            var w = new List<float>(); float sum = 0;
            foreach (var r in pool)
            {
                float v = 100;
                if (P.Level(r.code_id) < avg) v *= Cfg("catchup_weight_mul", 1.5f);
                if (r.unlock_rank == P.tier) v *= Cfg("new_unlock_weight_mul", 1.3f);
                if (!allUsed && used.Contains(r.code_id)) v = 0;
                w.Add(v); sum += v;
            }
            float x = Random.value * sum;
            for (int i = 0; i < pool.Count; i++) if ((x -= w[i]) <= 0 && w[i] > 0) return pool[i].code_id;
            return pool[^1].code_id;
        }
    }
}
