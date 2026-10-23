using System.Collections.Generic;
using NKK.Data;
using NKK.Items;
using NKK.Rats;
using UnityEngine;

namespace NKK
{
    // 증강 특수 효과 — 쥐 쪽 (2026-10-23 기획서 Ver.04, 기획서/증강 기획서 Ver.04 반영 결정.md)
    //  등급 통일 (Unify): 그 등급 쥐 전원 + 이후 태어나는 쥐 → p1 쥐. 빼면 이미 바뀐 쥐는 종류 그대로 · 동작만 원래.
    //    골든 햄스터·판다 = 늘 공 (특수 액션·묘기 없음) · 스케이터 = 양복 대머리 그림 + 빠르게 + 박치기 · 맘스땃쥐 = 패티 최대 p5 + 박치기 +p4 · 5G = 모든 쥐에게 데이터 전송
    //  쥐 탑 쌓기 (Tower): 장착하는 순간 5마리 → 1마리 탑 (판 끝까지, 같은 증강 한 개당 판에 한 번)
    //  상시 표시: 먹구름 (천둥 번개) · 치즈 구름 (치즈 비 중) · 가랜드 (탄생 축제) · 발자국 (거대 쥐) · 슬롯머신 (잭팟) · 5G 신호 막대 · 잔상 · 스파크
    public partial class AugmentSpecials
    {
        [Header("등급 통일 · 쥐 탑 · 상시 표시 (AugmentSpecials.Rats.cs)")]
        [Tooltip("스케이터 생쥐 통일 때 그림 (쥐 그림 모음 code_id, 양복 대머리)")] public string skaterSuitArt = "skater_suit";
        [Tooltip("상시 그림 (FxManager 스티커): 가랜드 · 발자국 · 슬롯머신 · 5G 신호 막대")] public string garlandSticker = "garland";
        public string footprintSticker = "footprint", slotSticker = "slot_machine", signalSticker = "signal_5g";
        [Tooltip("천둥 번개 상시 먹구름 크기 (기획서: 상시 = 작게 · 큰 구름 cloudSize 는 치즈 비 발동 때)")] public float stormLookSize = 260;
        public string headbuttPopup = "띵!", pandaPopup = "쿵", towerPopup = "쥐 탑!!", rapidPopup = "다다다", slotWinPopup = "7 7 7", slotLosePopup = "7 ? ?";
        public int headbuttPopupKey, pandaPopupKey, towerPopupKey, rapidPopupKey, slotWinPopupKey, slotLosePopupKey;

        static GameDatabase DB => GameDatabase.Instance;

        // ── 장착한 증강 효과 타입 (일반 효과: 다다다다 Multi_Hit · 찌릿찌릿 Bite_Zap · 탄생 축제 Birth_Frenzy) ──
        static int effVer = -1;
        static readonly HashSet<string> effs = new();
        static bool Eff(string t)
        {
            if (effVer != Augments.Version)
            {
                effVer = Augments.Version; effs.Clear();
                foreach (var u in Augments.Equipped) { var r = Augments.RowOf(Augments.Item(u)); if (r == null) continue; effs.Add(r.effect1); effs.Add(r.effect2); }
            }
            return effs.Contains(t);
        }
        public static bool Afterimage => I && Eff("Multi_Hit");
        static bool PartyOn => Eff("Birth_Frenzy");

        // ── 등급 통일 ──
        // 쥐 등급(0 일반 …) 에 걸린 통일 효과 (없으면 null)
        static AugSpecialRow UnifyOf(int ratGrade)
        {
            if (!I) return null;
            foreach (var e in Sp) if (e.s.type == "Unify" && Mathf.RoundToInt(e.s.p2) - 1 == ratGrade) return e.s;
            return null;
        }
        static AugSpecialRow UnifyFor(Rat r)
        {
            if (!r || r.Data == null) return null;
            var s = UnifyOf((int)r.Data.Grade);
            return s != null && DB && DB.Rats.TryGetValue(Mathf.RoundToInt(s.p1), out var row) && row.code_id == r.codeId ? s : null;
        }
        static EffectType ActionOf(RatCharacterRow row) => row != null && DB && DB.RatSkills.TryGetValue(row.action_skill, out var a) ? a.Effect : EffectType.None;
        static EffectType PassiveOf(RatCharacterRow row) => row != null && DB && DB.RatSkills.TryGetValue(row.passive_skill, out var a) ? a.Effect : EffectType.None;

        // 태어나는 · 승급하는 쥐 종 (RatManager)
        public static RatCharacterRow UnifyRow(RatCharacterRow row)
        {
            if (row == null) return null;
            var s = UnifyOf((int)row.Grade);
            return s != null && DB && DB.Rats.TryGetValue(Mathf.RoundToInt(s.p1), out var to) ? to : row;
        }
        public static bool BallForm(Rat r) { var s = UnifyFor(r); return s != null && ActionOf(r.Data) == EffectType.Rolling_Ball; }
        public static float UnifySpeedMul(Rat r) { var s = UnifyFor(r); return s != null && s.p3 > 0 ? s.p3 : 1; }
        static float UnifyBumpMul(Rat r) { var s = UnifyFor(r); return s != null ? 1 + s.p4 : 1; }
        public static int PattyMax(Rat r, int def) { var s = UnifyFor(r); return s != null && s.p5 > 0 ? Mathf.RoundToInt(s.p5) : def; }
        public static bool DataToAll(Rat r) => UnifyFor(r) != null;      // 5G 생쥐: 동료 수와 상관없이 · 모든 쥐에게 (데이터는 Max 라 여러 마리여도 한 번만)
        static float TowerMul(Rat r) => r ? r.TowerMul : 1;

        // 통일 효과가 새로 켜짐 → 판에 있는 그 등급 쥐 전부 바꿈
        readonly HashSet<AugSpecialRow> unifyOn = new();
        void UpdateUnify()
        {
            var now = new HashSet<AugSpecialRow>();
            foreach (var e in Sp) if (e.s.type == "Unify") now.Add(e.s);
            foreach (var s in now) if (!unifyOn.Contains(s)) ConvertAll(s);
            unifyOn.Clear(); foreach (var s in now) unifyOn.Add(s);
        }
        void ConvertAll(AugSpecialRow s)
        {
            if (!Rats || !DB || !DB.Rats.TryGetValue(Mathf.RoundToInt(s.p1), out var to)) return;
            int g = Mathf.RoundToInt(s.p2) - 1, n = 0;
            var fx = FxManager.I;
            foreach (var r in new List<Rat>(Rats.Rats))
            {
                if (!r || r.temp > 0 || r.UltOn || r.Swallowed || r.Grabbed || r.IsTower || (int)r.Data.Grade != g || r.codeId == to.code_id) continue;
                var nr = Rats.Replace(r, to);
                if (nr && fx && nr.OnScreen() && n++ < 30) fx.Anim("poof", nr.x, nr.y, 0, 0.8f);
            }
            fx?.Shake(0.2f);
        }

        // 스케이터 생쥐: 그 쥐들 그림 = 양복 대머리 (빼면 원래 그림)
        readonly Dictionary<Rat, bool> suited = new();
        public static void RatLook(Rat r, RatRig rig)
        {
            var me = I; if (!me || !r || !rig) return;
            // 탄생 축제: 광란 쥐에게 무지개 테두리 (무지개 색으로 깜빡)
            if (r.frenzy > 0 && PartyOn && r.OnScreen() && Random.value < Time.deltaTime * 6) FxManager.I?.Spark(r.x, r.y, 20, Color.HSVToRGB(Mathf.Repeat(Time.time * 0.8f + r.x * 0.01f, 1), 0.7f, 1), 40, 0.15f);
            var s = UnifyFor(r);
            bool want = s != null && PassiveOf(r.Data) == EffectType.Trick_Flip;      // 스케이터 (킥플립)
            me.suited.TryGetValue(r, out bool have);
            if (want == have || !me.Rats || !me.Rats.artLibrary) return;
            var art = me.Rats.artLibrary.Get(want ? me.skaterSuitArt : r.codeId);
            if (art == null) return;
            me.suited[r] = want;
            rig.Build(art, me.Rats.RigLength(r.Data));
        }

        // 박치기 연출 (Rat.Bump → OnRatHit): 스케이터 "띵!" · 판다 충격 원 "쿵"
        static void UnifyHitFx(Rat r, Item it)
        {
            var me = I; var fx = FxManager.I; if (!me || !fx || !r.OnScreen()) return;
            var s = UnifyFor(r); if (s == null) return;
            if (PassiveOf(r.Data) == EffectType.Trick_Flip && Random.value < 0.3f) { fx.Popup(r.x, r.y, T(me.headbuttPopupKey, me.headbuttPopup), Color.white, 20, 0.5f, 50); fx.Spark(r.x - r.face * 14, r.y, 34, Color.white, 50, 0.2f); }
            else if (PassiveOf(r.Data) == EffectType.Trick_Cannon && Random.value < 0.25f) { fx.Ring(r.x, r.y, 60, new Color(1, 1, 1, 0.8f), 0.3f); fx.Popup(r.x, r.y, T(me.pandaPopupKey, me.pandaPopup), Color.white, 20, 0.5f, 40); }
        }

        // 다다다다: 잔상 3개가 0.1초 간격으로 같은 물건을 때림 + "다다다" (공용 스킬 연타면 원래 "x3!")
        public static bool MultiHitFx(Rat r, float px, float py, int n)
        {
            var me = I; if (!me || !Afterimage || !me.Rats) return false;
            for (int k = 0; k < Mathf.Min(3, n); k++)
            {
                float d = k * 0.1f;
                me.Rats.Later(d, () => FxManager.I?.Slash(px + Random.Range(-10f, 10f), py, 24, Random.Range(0, Mathf.PI * 2), 50, new Color(1, 1, 1, 0.8f), 0.18f));
            }
            FxManager.I?.Popup(px, py, T(me.rapidPopupKey, me.rapidPopup), new Color(1, 0.86f, 0.52f), 22, 0.5f, 50);
            return true;
        }

        // 탄생 축제: 탄생 자리 색종이 폭발 (RatManager.Breed)
        public static void OnBirthParty(float x, float y)
        {
            if (!I || !PartyOn) return;
            var fx = FxManager.I; if (!fx) return;
            for (int k = 0; k < 3; k++) fx.Stars(x, y, 30, 6, Color.HSVToRGB(Random.value, 0.7f, 1), Color.white, 120, 320);
        }

        // ── 쥐 탑 쌓기: 장착하는 순간 (같은 증강 한 개당 판에 한 번) ──
        readonly HashSet<int> towerDone = new();
        void UpdateTowers()
        {
            foreach (var u in Augments.Equipped)
            {
                if (towerDone.Contains(u)) continue;
                var it = Augments.Item(u); var sp = Augments.SpecialOf(Augments.RowOf(it));
                if (sp == null || sp.type != "Tower") continue;
                towerDone.Add(u);
                BuildTowers(Mathf.RoundToInt(Augments.SP(sp, 1, it.lv)), Mathf.Max(2, Mathf.RoundToInt(Augments.SP(sp, 2, it.lv))));
            }
        }
        void BuildTowers(int towers, int per)
        {
            if (!Rats || !Rats.artLibrary) return;
            var pool = new List<Rat>();
            foreach (var r in Rats.Rats) if (r && r.temp <= 0 && !r.UltOn && !r.Swallowed && !r.Grabbed && !r.IsTower) pool.Add(r);
            pool.Sort((a, b) => b.OnScreen().CompareTo(a.OnScreen()));      // 화면 안 쥐부터
            int made = 0;
            var fx = FxManager.I;
            for (int t = 0; t < towers && pool.Count >= per; t++)
            {
                // 무리에서 가까운 per 마리: 첫 쥐 둘레
                var seed = pool[0];
                pool.Sort((a, b) => ((a.x - seed.x) * (a.x - seed.x) + (a.y - seed.y) * (a.y - seed.y)).CompareTo((b.x - seed.x) * (b.x - seed.x) + (b.y - seed.y) * (b.y - seed.y)));
                var group = pool.GetRange(0, per); pool.RemoveRange(0, per);
                // 1층 = 가장 높은 등급 (같으면 무작위)
                Rat bottom = null;
                foreach (var r in group) if (!bottom || r.Data.Grade > bottom.Data.Grade || (r.Data.Grade == bottom.Data.Grade && Random.value < 0.5f)) bottom = r;
                group.Remove(bottom);
                group.Sort((a, b) => b.Data.Grade.CompareTo(a.Data.Grade));
                var arts = new List<RatArtLibrary.Entry>(); var lens = new List<float>(); var scales = new List<float>();
                float add = 0;
                foreach (var r in group)
                {
                    arts.Add(Rats.artLibrary.Get(r.codeId)); lens.Add(Rats.RigLength(r.Data)); scales.Add(r.GradeData.size);
                    add += r.Damage;
                    if (fx && r.OnScreen()) fx.Anim("poof", r.x, r.y, 0, 0.7f);
                    Rats.RemoveRat(r);
                }
                bottom.MakeTower(arts, lens, scales, add);
                if (fx) fx.Popup(bottom.x, bottom.y, T(towerPopupKey, towerPopup), popupColor, 28, 1.4f, 200);
                if (pool.Count > 0 && pool[0] == bottom) pool.RemoveAt(0);
                made++;
            }
            if (made > 0) SfxManager.Play("jgl_unlock");
        }

        void UpdateRatSide(float dt)
        {
            if (!Rats) return;
            UpdateUnify();
            if (Running) UpdateTowers();
        }

        // ── 상시 표시 (화면에 붙는 그림 하나씩, 효과가 꺼지면 사라짐) ──
        readonly Dictionary<string, FxManager.Sticker> looks = new();
        void Look(string key, bool on, string sticker, float x, float y, float z, float size, bool ground = false, Color? col = null)
        {
            looks.TryGetValue(key, out var s);
            if (!on || (Stage && Stage.Climbing)) { if (s != null) { s.End(0.3f); looks.Remove(key); } return; }
            if (s == null || s.life <= 0) { s = FxManager.I ? FxManager.I.AddSticker(sticker, x, y, z, size, 99999, 0, ground, col) : null; if (s == null) return; looks[key] = s; }
            s.Move(x, y, z);       // life 를 매 프레임 max 로 되돌리면 나타나기 투명도((max - life) / 0.15)가 늘 0 → 안 보임
        }
        float lookT;
        void UpdateLooks(float dt)
        {
            var v = View; float t = Time.time;
            // 화면 위쪽에 붙는 것은 높이(z) 0 · 바닥 y 를 화면 위끝에서 그림 반 높이만큼 아래로 (z 를 올리면 화면 밖으로 나감)
            Look("storm", Has("Lightning"), stormCloudSticker, v.center.x, v.yMin + 100 + Mathf.Sin(t * 0.7f) * 6, 0, stormLookSize);
            Look("cheese", rainT > 0, cheeseCloudSticker, v.center.x - v.width * 0.15f, v.yMin + 150, 0, cloudSize);
            Look("garland", PartyOn, garlandSticker, v.center.x, v.yMin + 90, 0, v.width * 0.6f);
            Look("slot", Has("Jackpot"), slotSticker, v.xMin + 300, v.yMax - 140, 0, 110);
            bool g5 = false; foreach (var e in Sp) if (e.s.type == "Unify" && DB && DB.Rats.TryGetValue(Mathf.RoundToInt(e.s.p1), out var row) && PassiveOf(row) == EffectType.Rush_Range_Up) g5 = true;
            Look("5g", g5, signalSticker, v.center.x + v.width * 0.22f, v.yMin + 90, 0, 90);
            Look("foot", Has("Giant"), footprintSticker, v.center.x - v.width * 0.2f, v.center.y + 60, 0, 220, true, new Color(1, 1, 1, 0.25f));
            if (!Running || !Rats) return;
            var fx = FxManager.I; if (!fx) return;
            // 찌릿찌릿: 쥐 털 끝 작은 파란 스파크
            if (Eff("Bite_Zap") && Random.value < dt * 4) { var r = AnyRat(); if (r && r.OnScreen()) fx.Spark(r.x + Random.Range(-10f, 10f), r.y, 24, new Color(0.6f, 0.9f, 1f), 24, 0.12f); }
            if ((lookT -= dt) > 0) return;
            lookT = 0.25f;
            foreach (var r in Rats.Rats)
            {
                if (!r || !r.OnScreen() || r.Speed < 60) continue;
                // 골든 햄스터 공: 지나간 자리 황금 바퀴 자국
                if (r.BallForm && PassiveOf(r.Data) == EffectType.Cheese_Boost && Random.value < 0.5f) fx.Spill(r.x, r.y, 14, new Color(1f, 0.82f, 0.3f, 0.6f));
            }
            // 5G: 쥐 사이 얇은 전파선 (가끔 하나씩)
            if (g5 && Rats.Rats.Count > 1) { var a = AnyRat(); var b = AnyRat(); if (a && b && a != b && a.OnScreen()) fx.Beam(a.x, a.y, 30, b.x, b.y, 30, new Color(0.45f, 0.9f, 1f, 0.35f), 0.4f, 0.4f); }
        }

        // 잭팟 슬롯머신: 층 클리어 때 릴이 돌아감 → 당첨이면 777 + 화면 위에서 연구자료가 쏟아짐
        void SpinSlot(bool win)
        {
            var v = View; float x = v.xMin + 300, y = v.yMax - 140;
            var fx = FxManager.I; if (!fx) return;
            fx.Popup(x, y, win ? T(slotWinPopupKey, slotWinPopup) : T(slotLosePopupKey, slotLosePopup), win ? popupColor : Color.white, win ? 28 : 20, 1.2f, 130);
            if (!win || !Rats) return;
            for (int k = 0; k < 15; k++) { float d = k * 0.1f; Rats.Later(d, () => { var w = View; FxManager.I?.Stars(Random.Range(w.xMin, w.xMax), w.center.y, 400, 2, new Color(0.7f, 0.85f, 1f), Color.white, 40, 120); }); }
        }
    }
}
