using System.Collections.Generic;
using System.Globalization;
using NKK.Data;
using UnityEngine;

namespace NKK
{
    // 증강 공통 로직 (2026-10-19, 증강 테이블). 로비(증강 연구소)와 게임(판 안 슬롯) 둘 다 씀.
    // 보관함 = Progress.aug.items (얻으면 바로 쌓임, 하나하나 강화 레벨·포인트). 판 안에서 장착한 것 = Equipped (저장 안 함, 판마다 비움).
    // 장착한 증강의 효과는 CommonSkill 이 공용 스킬 노드처럼 더함 (Rows) → 게임 쪽 효과 코드는 공용 스킬과 같음.
    public static class Augments
    {
        static GameDatabase DB => GameDatabase.Instance;
        static Progress P => Progress.I;
        static Progress.AugSave S => P ? P.aug : null;

        public static float Cfg(string key, float def) => DB ? DB.AugCfg(key, def) : def;
        public static int UnlockFloor => Mathf.RoundToInt(Cfg("unlock_floor", 31));
        public static int CarryFloor => Mathf.RoundToInt(Cfg("carry_floor", 41));
        public static bool TestOpen;       // 테스트: 층과 상관없이 열기
        public static bool Unlocked => TestOpen || (P && P.maxFloor >= UnlockFloor);
        public static bool CarryUnlocked => TestOpen || (P && P.maxFloor >= CarryFloor);
        public static int SlotCount => Mathf.Clamp(Mathf.RoundToInt(Cfg("slot_base", 2)) + CommonSkill.AugSlotAdd, 1, Mathf.Max(1, Mathf.RoundToInt(Cfg("slot_max", 5))));      // 2칸 시작 · 최대 5 (2026-10-20)
        public static int OfferCount => Mathf.Max(1, Mathf.RoundToInt(Cfg("offer_count", 3)));
        public static int MaxLevel => Mathf.RoundToInt(Cfg("up_max", 5));

        // 보관함·장착이 바뀔 때마다 +1 (CommonSkill · UI 가 다시 계산)
        public static int Version { get; private set; }
        public static void Touch() => Version++;

        // ── 보관함 ──
        public static List<Progress.AugItem> Items => S != null ? S.items : new List<Progress.AugItem>();
        public static Progress.AugItem Item(int uid) { if (S == null) return null; foreach (var it in S.items) if (it.uid == uid) return it; return null; }
        public static AugmentRow Row(int id) => DB && DB.AugmentsById.TryGetValue(id, out var r) ? r : null;
        public static AugmentRow RowOf(Progress.AugItem it) => it == null ? null : Row(it.id);
        public static AugGradeRow Grade(int g) => DB && DB.AugGrades.TryGetValue(g, out var r) ? r : null;
        public static bool Seen(int id) => Cfg("seen_reveal", 1) < 0.5f || (S != null && S.seen.Contains(id));
        public static int CountOf(int id) { int n = 0; foreach (var it in Items) if (it.id == id) n++; return n; }

        public static Progress.AugItem Add(int id)
        {
            if (S == null || Row(id) == null) return null;
            var it = new Progress.AugItem { uid = S.nextUid++, id = id, isNew = true };
            S.items.Add(it);
            if (!S.seen.Contains(id)) S.seen.Add(id);
            P.Save(); Touch();
            return it;
        }
        public static void Remove(int uid)
        {
            if (S == null) return;
            S.items.RemoveAll(i => i.uid == uid);
            Equipped.Remove(uid);
            if (S.carry == uid) S.carry = 0;
            P.Save(); Touch();
        }

        // ── 효과 값 (강화 레벨만큼 커짐, 개수·간격 같은 칸은 그대로) ──
        public static float Value(AugmentRow r, int effect, int i, int lv)
        {
            if (r == null) return 0;
            float v = r.V(effect, i);
            var e = effect == 1 ? r.Effect1 : r.Effect2;
            if (lv <= 0 || (DB && DB.AugNoScale.Contains((e, i)))) return v;
            return v * (1 + Cfg("up_pct", 0.1f) * lv);
        }
        // ── 특수 효과 (증강 특수 효과 테이블): effect1 = Special, e1_v1 = special_id ──
        public static AugSpecialRow SpecialOf(AugmentRow r)
        {
            if (r == null || DB == null || r.effect1 != "Special") return null;
            return DB.AugSpecials.TryGetValue(Mathf.RoundToInt(r.e1_v1), out var s) ? s : null;
        }
        // 특수 효과 칸 값 (up_param 칸만 강화 레벨만큼 커짐)
        public static float SP(AugSpecialRow s, int i, int lv) => s == null ? 0 : s.P(i) * (s.up_param == i && lv > 0 ? 1 + Cfg("up_pct", 0.1f) * lv : 1);

        // 설명 글: {1} {1b} {1c} {2} {2b} {2c} · 특수 효과 {s1}~{s5} (뒤에 % = ×100)
        public static string Desc(AugmentRow r, int lv)
        {
            if (r == null) return "";
            string s = r.aug_desc ?? "";
            for (int e = 1; e <= 2; e++)
                for (int i = 1; i <= 3; i++)
                {
                    string k = e.ToString() + (i == 1 ? "" : i == 2 ? "b" : "c");
                    float v = Value(r, e, i, lv);
                    s = s.Replace("{" + k + "%}", Num(v * 100)).Replace("{" + k + "}", Num(v));
                }
            var sp = SpecialOf(r);
            if (sp != null) for (int i = 1; i <= 5; i++) { float v = SP(sp, i, lv); s = s.Replace("{s" + i + "%}", Num(v * 100)).Replace("{s" + i + "}", Num(v)); }
            return s;
        }
        static string Num(float v) => Mathf.Abs(v - Mathf.Round(v)) < 0.05f ? Mathf.RoundToInt(v).ToString() : v.ToString("0.#", CultureInfo.InvariantCulture);

        // ── 강화 (포인트 + 치즈) ──
        public static int UpPoints(int lv) => Mathf.RoundToInt(Cfg("up_pt_base", 150) * Mathf.Pow(Cfg("up_pt_grow", 1.6f), lv));
        public static double UpCheese(int lv)
        {
            var st = DB && P ? DB.StageOf(Mathf.Clamp(P.maxFloor, 1, NKK.Stage.StageManager.LastFloor)) : null;
            double mul = st != null && st.cheese > 0 ? st.cheese : 1;
            int f = P ? Mathf.Clamp(P.maxFloor, 1, NKK.Stage.StageManager.LastFloor) : 1;
            double floorK = System.Math.Pow(Cfg("up_cheese_floor_grow", 1), Mathf.Min(f, (int)Cfg("up_cheese_floor_cap", 30)) - 1);   // 상점 BC 와 같은 층 수입 배율 (2026-10-19)
            return System.Math.Round(Cfg("up_cheese_base", 40) * mul * floorK * Mathf.Pow(Cfg("up_cheese_grow", 1.5f), lv));
        }
        public static bool CanUpgrade(Progress.AugItem it) => it != null && P && it.lv < MaxLevel && it.pts >= UpPoints(it.lv) && P.cheese >= UpCheese(it.lv);
        public static bool Upgrade(Progress.AugItem it)
        {
            if (!CanUpgrade(it)) return false;
            P.cheese -= UpCheese(it.lv); it.pts -= UpPoints(it.lv); it.lv++;
            P.Save(); Touch();
            return true;
        }

        // ── 합성: 같은 등급 synth_need 개 → 무작위 증강 1개 (상승/유지/하락). 반환 = 새 증강 (조건이 안 맞으면 null) ──
        public static int SynthNeed(int grade) { var g = Grade(grade); return g != null ? Mathf.Max(2, g.synth_need) : 3; }
        public static bool CanSynth(List<int> uids)
        {
            if (uids == null || uids.Count == 0) return false;
            int g = -1;
            foreach (var u in uids) { var r = RowOf(Item(u)); if (r == null) return false; if (g < 0) g = r.grade; else if (r.grade != g) return false; }
            return uids.Count == SynthNeed(g);
        }
        public static int SynthResultGrade(int grade)
        {
            var g = Grade(grade); if (g == null) return grade;
            int lo = MinGrade, hi = MaxGrade;
            float up = grade >= hi ? 0 : g.synth_up, down = grade <= lo ? 0 : g.synth_down, same = g.synth_same;
            float x = Random.value * Mathf.Max(0.0001f, up + same + down);
            return x < up ? grade + 1 : x < up + same ? grade : grade - 1;
        }
        public static Progress.AugItem Synthesize(List<int> uids)
        {
            if (!CanSynth(uids)) return null;
            int grade = RowOf(Item(uids[0])).grade;
            foreach (var u in uids.ToArray()) Remove(u);
            var row = RandomOfGrade(SynthResultGrade(grade), null);
            return row != null ? Add(row.augment_id) : null;
        }

        static int MinGrade { get { int m = 99; if (DB) foreach (var g in DB.AugGrades.Keys) m = Mathf.Min(m, g); return m == 99 ? 1 : m; } }
        static int MaxGrade { get { int m = 0; if (DB) foreach (var g in DB.AugGrades.Keys) m = Mathf.Max(m, g); return m == 0 ? 4 : m; } }

        public static AugmentRow RandomOfGrade(int grade, HashSet<int> exclude)
        {
            if (!DB) return null;
            var l = new List<AugmentRow>();
            foreach (var a in DB.Augments) if (a.enabled != 0 && a.grade == grade && (exclude == null || !exclude.Contains(a.augment_id))) l.Add(a);
            if (l.Count == 0 && exclude != null) return RandomOfGrade(grade, null);
            return l.Count > 0 ? l[Random.Range(0, l.Count)] : null;
        }

        // ── 층 클리어 고르기: 등급 가중치(클리어 층 기준)로 서로 다른 증강 n 개 ──
        public static List<AugmentRow> RollOffer(int clearedFloor, int n)
        {
            var res = new List<AugmentRow>(); if (!DB) return res;
            AugOfferRow w = null; foreach (var o in DB.AugOffers) if (clearedFloor >= o.from_floor) w = o;
            if (w == null && DB.AugOffers.Count > 0) w = DB.AugOffers[0];
            var used = new HashSet<int>();
            for (int k = 0; k < n; k++)
            {
                float sum = 0; for (int g = 1; g <= 4; g++) sum += w != null ? w.W(g) : 1;
                float x = Random.value * sum; int pick = 1;
                for (int g = 1; g <= 4; g++) { float wg = w != null ? w.W(g) : 1; if (x < wg) { pick = g; break; } x -= wg; }
                var r = RandomOfGrade(pick, used);
                if (r == null) continue;
                used.Add(r.augment_id); res.Add(r);
            }
            return res;
        }

        // ── 판 안 장착 (저장 안 함) ──
        public static readonly List<int> Equipped = new();
        public static bool IsEquipped(int uid) => Equipped.Contains(uid);
        public static bool HasFreeSlot => Equipped.Count < SlotCount;
        public static void ClearRun() { Equipped.Clear(); Touch(); }
        public static bool Equip(int uid)
        {
            if (Item(uid) == null || Equipped.Contains(uid) || !HasFreeSlot) return false;
            Equipped.Add(uid); var it = Item(uid); if (it != null) it.isNew = false;
            Touch(); return true;
        }
        public static void Unequip(int uid) { if (Equipped.Remove(uid)) Touch(); }
        // 장착 중 a 를 보관함의 b 로 바꿈 (같은 자리)
        public static bool Swap(int equippedUid, int storageUid)
        {
            int i = Equipped.IndexOf(equippedUid);
            if (i < 0 || Item(storageUid) == null || Equipped.Contains(storageUid)) return false;
            Equipped[i] = storageUid; Touch(); return true;
        }
        // 들고 가기 (판 시작): 해금됐고 보관함에 있으면 장착
        public static void EquipCarry() { if (CarryUnlocked && S != null && S.carry != 0 && Item(S.carry) != null) Equip(S.carry); }
        public static int Carry { get => S != null ? S.carry : 0; set { if (S == null) return; S.carry = value; P.Save(); Touch(); } }

        // 층 클리어: 장착한 증강 한 개마다 강화 포인트 = 클리어 층 × pt_per_floor (사용자 2026-10-19)
        public static void GivePoints(int clearedFloor)
        {
            if (S == null || Equipped.Count == 0) return;
            int pts = Mathf.Max(1, Mathf.RoundToInt(clearedFloor * Cfg("pt_per_floor", 1)));
            foreach (var u in Equipped) { var it = Item(u); if (it != null) it.pts += pts; }
            P.Save(); Touch();
        }

        // 장착 중인 특수 효과 (타입 → [특수 효과 행, 강화 레벨] 목록) — AugmentSpecials 가 매 프레임 읽음
        static int spVer = -1;
        static readonly List<(AugSpecialRow s, int lv)> specials = new();
        public static List<(AugSpecialRow s, int lv)> Specials
        {
            get
            {
                if (spVer == Version) return specials;
                spVer = Version; specials.Clear();
                foreach (var u in Equipped) { var it = Item(u); var sp = SpecialOf(RowOf(it)); if (sp != null) specials.Add((sp, it.lv)); }
                return specials;
            }
        }

        // ── CommonSkill 에 넘기는 효과 줄 (장착한 증강의 효과를 공용 스킬 노드 모양으로) ──
        static int rowsVer = -1;
        static readonly List<CommonSkillRow> rows = new();
        public static List<CommonSkillRow> Rows
        {
            get
            {
                if (rowsVer == Version) return rows;
                rowsVer = Version; rows.Clear();
                foreach (var u in Equipped)
                {
                    var it = Item(u); var r = RowOf(it); if (r == null) continue;
                    for (int e = 1; e <= 2; e++)
                    {
                        var t = e == 1 ? r.effect1 : r.effect2;
                        if (string.IsNullOrEmpty(t) || t == "None" || t == "Special") continue;      // 특수 효과는 AugmentSpecials
                        rows.Add(new CommonSkillRow { skill_id = -u * 10 - e, effect_type = t, value_01 = Value(r, e, 1, it.lv), value_02 = Value(r, e, 2, it.lv), value_03 = Value(r, e, 3, it.lv) });
                    }
                }
                return rows;
            }
        }
    }
}
