using System.Collections.Generic;
using NKK.Data;
using NKK.Rats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 쥐구멍 암시장 페이지 (탈출 준비실 Page_market). 로직 = Core/BlackMarket. 글은 전부 씬 TMP (자리표시만 채움)
    //   슬롯 카드 템플릿 자식: Frame(Image, 등급 색) · Grade({grade}) · Portrait(RatPortrait) · Name · Count({n}) · Buy(Button) / Buy/Price({price}) · Lock(Button) / Lock/On ·
    //                          Sold · Closed / Closed/Text({floor}) · Waiting
    public class MarketPage : MonoBehaviour
    {
        public LobbyManager manager;
        public RatArtLibrary artLibrary;
        [Tooltip("슬롯 카드 템플릿 (꺼 둠) · 카드가 놓일 줄")] public GameObject slotTemplate;
        public Transform slotRow;
        [Tooltip("다음 입고 ({time})")] public TMP_Text timer;
        [Tooltip("리롤 버튼 · 유료 글 ({cost}) · 무료 글 ({n})")] public Button reroll;
        public TMP_Text rerollPaid, rerollFree;
        [Tooltip("신화 천장 ({n} 지금 · {max})")] public TMP_Text pity;
        [Tooltip("브로커 말 ({line}) · 말 목록: 구매 · 돈 부족 · 리롤 · 새 물건")] public TMP_Text brokerLine;
        public string[] lineBuy, linePoor, lineReroll, lineNew;
        [Tooltip("첫 방문 선물 창 (꺼 둠) · 글 ({name} {n}) · 확인")] public GameObject giftPopup;
        public TMP_Text giftText;
        public Button giftOk;
        [Tooltip("확인 창 (치즈 절반 넘는 리롤): 창 · 글 ({cost}) · 예 · 아니오 — 신화 구매는 2026-10-20 부터 확인 안 함")] public GameObject confirm;
        public TMP_Text confirmText;
        public Button confirmYes, confirmNo;

        readonly List<GameObject> cards = new();
        string timerTpl, paidTpl, freeTpl, pityTpl, giftTpl, confirmTpl;
        System.Action pending;
        float tick;

        void Awake()
        {
            timerTpl = timer ? timer.text : ""; paidTpl = rerollPaid ? rerollPaid.text : ""; freeTpl = rerollFree ? rerollFree.text : "";
            pityTpl = pity ? pity.text : ""; giftTpl = giftText ? giftText.text : ""; confirmTpl = confirmText ? confirmText.text : "";
            if (slotTemplate) slotTemplate.SetActive(false);
            if (reroll) reroll.onClick.AddListener(OnReroll);
            if (giftOk) giftOk.onClick.AddListener(() => giftPopup.SetActive(false));
            if (confirmYes) confirmYes.onClick.AddListener(() => { confirm.SetActive(false); var a = pending; pending = null; a?.Invoke(); });
            if (confirmNo) confirmNo.onClick.AddListener(() => { confirm.SetActive(false); pending = null; });
            if (giftPopup) giftPopup.SetActive(false);
            if (confirm) confirm.SetActive(false);
        }

        static string Fmt(double v) => LobbyManager.Fmt(v);
        static RatCharacterRow Row(string code) { foreach (var r in GameDatabase.Instance.Rats.Values) if (r.code_id == code) return r; return null; }

        void OnEnable() => Render();

        public void Render()
        {
            var p = Progress.I; if (!p || !GameDatabase.Instance) return;
            if (BlackMarket.Tick()) Line(lineNew);
            // 첫 방문 선물 (튜토리얼 대사가 끝난 뒤 보이게 조금 뒤)
            if (!p.shop.opened && giftPopup)
            {
                var code = BlackMarket.FirstVisitGift(out int n);
                var row = code != null ? Row(code) : null;
                if (row != null) { giftPopup.SetActive(true); if (giftText) giftText.text = giftTpl.Replace("{name}", row.character_name).Replace("{n}", n.ToString()); }
            }
            foreach (var c in cards) Destroy(c);
            cards.Clear();
            var db = GameDatabase.Instance;
            int total = db.Shop.Shop_Slot != null ? db.Shop.Shop_Slot.Count : 6;
            for (int i = 1; i <= total; i++)
            {
                var go = Instantiate(slotTemplate, slotRow); go.SetActive(true); cards.Add(go);
                Progress.ShopItem it = null; foreach (var x in p.shop.lineup) if (x.slot == i) it = x;
                ShopSlotRow sr = null; foreach (var r in db.Shop.Shop_Slot) if (r.slot_id == i) sr = r;
                bool closed = it == null;
                Set(go, "Closed", closed);
                if (closed) { var ct = go.transform.Find("Closed/Text")?.GetComponent<TMP_Text>(); if (ct) ct.text = ct.text.Replace("{floor}", (sr != null ? sr.unlock_floor : 0).ToString()); }
                var g = it != null ? BlackMarket.Goods(it.goods) : null;
                var rat = it != null && !string.IsNullOrEmpty(it.code) ? Row(it.code) : null;
                Set(go, "Waiting", !closed && rat == null);
                Set(go, "Sold", it != null && rat != null && it.sold);
                int gi = rat != null ? (int)rat.Grade : g != null ? (int)g.Grade : 4;
                var frame = go.transform.Find("Frame")?.GetComponent<Image>(); if (frame) frame.color = GradeColor(gi);
                Text(go, "Grade", GradeName(gi));
                Text(go, "Name", rat != null ? rat.character_name : "");
                var cnt = go.transform.Find("Count")?.GetComponent<TMP_Text>(); if (cnt) { cnt.text = cnt.text.Replace("{n}", g != null ? BlackMarket.PackSize(g).ToString() : "0"); cnt.gameObject.SetActive(rat != null); }
                var por = go.transform.Find("Portrait")?.GetComponent<RatPortrait>();
                if (por) { por.gameObject.SetActive(rat != null); if (rat != null && artLibrary) por.Show(artLibrary.Get(rat.code_id)); }
                var buy = go.transform.Find("Buy")?.GetComponent<Button>();
                if (buy)
                {
                    buy.gameObject.SetActive(rat != null && !it.sold);
                    double price = BlackMarket.Price(g);
                    var pt = buy.transform.Find("Price")?.GetComponent<TMP_Text>(); if (pt) pt.text = pt.text.Replace("{price}", Fmt(price));
                    buy.interactable = p.cheese >= price;
                    int slot = i;
                    buy.onClick.AddListener(() => Buy(slot));      // 신화도 확인 창 없이 바로 구매 (2026-10-20 사용자)
                }
                var lk = go.transform.Find("Lock")?.GetComponent<Button>();
                if (lk)
                {
                    lk.gameObject.SetActive(rat != null);
                    Set(lk.gameObject, "On", p.shop.lockedSlot == i);
                    int slot = i; lk.onClick.AddListener(() => { BlackMarket.ToggleLock(slot); SfxManager.Play("ui_click"); Render(); });
                }
                if (i == 1) { var tt = go.GetComponent<NKK.Tutorial.TutorialTarget>(); if (tt) tt.enabled = true; }
            }
            RenderBar();
        }

        void RenderBar()
        {
            var p = Progress.I;
            bool free = p.shop.freeLeft > 0; double cost = BlackMarket.RerollCost;
            if (rerollPaid) { rerollPaid.gameObject.SetActive(!free); rerollPaid.text = paidTpl.Replace("{cost}", Fmt(cost)); }
            if (rerollFree) { rerollFree.gameObject.SetActive(free); rerollFree.text = freeTpl.Replace("{n}", p.shop.freeLeft.ToString()); }
            if (reroll) reroll.interactable = free || p.cheese >= cost;
            if (pity) pity.text = pityTpl.Replace("{n}", p.shop.pity.ToString()).Replace("{max}", ((int)GameDatabase.Instance.ShopCfg("mythic_pity_count", 8)).ToString());
        }

        // 등급 이름 · 색 = 쥐 등급 테이블
        public static string GradeName(int g) => GameDatabase.Instance.Grades.TryGetValue((Grade)g, out var r) ? r.grade_name : "";
        public static Color GradeColor(int g) => GameDatabase.Instance.Grades.TryGetValue((Grade)g, out var r) && ColorUtility.TryParseHtmlString(r.color, out var c) ? c : Color.white;

        static void Set(GameObject go, string child, bool on) { var t = go.transform.Find(child); if (t) t.gameObject.SetActive(on); }
        static void Text(GameObject go, string child, string s) { var t = go.transform.Find(child)?.GetComponent<TMP_Text>(); if (t) t.text = s; }

        void Line(string[] l) { if (brokerLine && l != null && l.Length > 0) brokerLine.text = l[Random.Range(0, l.Length)]; }

        void Ask(bool need, string cost, System.Action a)
        {
            if (!need || !confirm) { a(); return; }
            pending = a; confirm.SetActive(true);
            if (confirmText) confirmText.text = confirmTpl.Replace("{cost}", cost);
        }

        void Buy(int slot)
        {
            if (BlackMarket.Buy(slot, out _)) { SfxManager.Play("ui_buy"); Line(lineBuy); }
            else { SfxManager.Play("ui_deny"); Line(linePoor); }
            Render();
        }

        void OnReroll()
        {
            var p = Progress.I; double cost = BlackMarket.RerollCost;
            bool big = cost > 0 && cost >= p.cheese * GameDatabase.Instance.ShopCfg("mythic_confirm_ratio", 0.5f);
            Ask(big, Fmt(cost), () =>
            {
                if (BlackMarket.Reroll()) { SfxManager.Play("ui_buy"); Line(lineReroll); }
                else { SfxManager.Play("ui_deny"); Line(linePoor); }
                Render();
            });
        }

        void Update()
        {
            if (!Progress.I) return;
            if ((tick -= Time.unscaledDeltaTime) > 0) return;
            tick = 0.5f;
            if (BlackMarket.Tick()) { Line(lineNew); Render(); return; }
            long s = BlackMarket.SecondsToRefresh;
            if (timer) timer.text = timerTpl.Replace("{time}", $"{s / 60:00}:{s % 60:00}");
            RenderBar();
        }
    }
}
