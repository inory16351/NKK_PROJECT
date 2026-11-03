using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 도토리 상점 (2026-11-03 신규 재화(도토리) 기획서 Ver.05): 아지트 벽 구멍(LobbyHot id shop)을 누르면 열리는 탈출 준비실 페이지 'shop'.
    // 구역 탭 (아지트 · 뒷골목 · UI · 커서 · 쥐 친구) → 장식 상품 카드 (도토리 테이블 Deco_Item). 2훈장 업적 안내(l_achv)가 기능 'shop' 을 해금.
    // 판매 조건: 그 장식이 바꾸는 시설(sell_feature)이 아직 안 열렸으면 잠김 + 해금 조건 글 (Deco_Lock).
    // ※ 장식 적용(그림 교체 · 쥐 친구 · 커서 · UI 틀)은 아직 없음 — 리소스를 만들 때 구현. 지금은 목록 · 가격 · 잠금 · 구매(보유 기록)만.
    // 글은 씬 TMP (자리표시 {n} {name} {price} {hint}), 코드는 값만.
    public class AcornShopPage : MonoBehaviour
    {
        public LobbyManager manager;
        [Tooltip("보유 도토리 ({n})")] public TMP_Text acornText;

        [Header("구역 탭")]
        [Tooltip("탭 버튼 (자식 Label · Sel) — zoneIds 와 같은 순서")] public Button[] zoneTabs = new Button[0];
        [Tooltip("탭 구역: Lobby · Alley · Common = 도토리 테이블 Deco_Category.zone · RatFriend = 쥐 친구 종류")] public string[] zoneIds = { "Lobby", "Alley", "Common", "RatFriend" };

        [Header("상품 카드")]
        [Tooltip("카드 템플릿 (꺼져 있음): 자식 Name · Price({n}) · Buy(Button, 자식 Label) · Lock · Hint({hint}) · Owned")] public RectTransform itemTemplate;
        [Tooltip("카드 글 (씬 Words): 사기 · 보유 · 도토리 부족 ({n} 모자람) · 잠김")] public TMP_Text wBuy, wOwned, wPoor, wLocked;
        public Color ownedTint = new(0.8f, 0.95f, 0.8f), lockedTint = new(0.7f, 0.66f, 0.62f);

        [Header("구매 확인 (가격이 Acorn_Config.confirm_price 이상)")]
        public GameObject confirmRoot;
        [Tooltip("확인 글 ({name} {price})")] public TMP_Text confirmText;
        public Button confirmYes, confirmNo;

        string zone = "Lobby";
        DecoItemRow pending;
        readonly List<GameObject> spawned = new();
        readonly Dictionary<TMP_Text, string> tpl = new();
        string T(TMP_Text t) { if (!t) return ""; if (!tpl.TryGetValue(t, out var s)) tpl[t] = s = LocText.Tpl(t, t.text); return s; }
        string F(TMP_Text t, params (string k, object v)[] kv) { var s = T(t); foreach (var (k, v) in kv) s = s.Replace("{" + k + "}", v?.ToString()); return s; }
        static TMP_Text Txt(Transform t, string p) => t.Find(p)?.GetComponent<TMP_Text>();
        static GameDatabase DB => GameDatabase.Instance;

        void Awake()
        {
            if (itemTemplate) { T(Txt(itemTemplate, "Price")); T(Txt(itemTemplate, "Hint")); itemTemplate.gameObject.SetActive(false); }
            T(acornText); T(confirmText);
            for (int i = 0; i < zoneTabs.Length && i < zoneIds.Length; i++) { var id = zoneIds[i]; if (zoneTabs[i]) zoneTabs[i].onClick.AddListener(() => { zone = id; SfxManager.Play("ui_click"); Draw(); }); }
            if (confirmYes) confirmYes.onClick.AddListener(() => { var d = pending; Confirm(false); Buy(d, true); });
            if (confirmNo) confirmNo.onClick.AddListener(() => Confirm(false));
            Confirm(false);
        }

        void OnEnable() { if (Progress.I) Progress.I.AcornChanged += Draw; }
        void OnDisable() { if (Progress.I) Progress.I.AcornChanged -= Draw; Confirm(false); }

        // LobbyManager 가 페이지를 열 때 (SendMessage)
        public void Render() { Confirm(false); Draw(); }

        string ZoneOf(DecoItemRow d)
        {
            if (d.category == "RatFriend") return "RatFriend";
            foreach (var c in DB.DecoCategories) if (c.category == d.category) return c.zone;
            return "";
        }

        void Draw()
        {
            var p = Progress.I; if (!p || !DB || !itemTemplate) return;
            foreach (var g in spawned) Destroy(g);
            spawned.Clear();
            if (acornText) acornText.text = F(acornText, ("n", p.acorn.ToString("N0")));
            for (int i = 0; i < zoneTabs.Length && i < zoneIds.Length; i++) if (zoneTabs[i]) Show(zoneTabs[i].transform, "Sel", zoneIds[i] == zone);
            foreach (var d in DB.DecoItems)
            {
                if (ZoneOf(d) != zone) continue;
                var st = p.DecoStateOf(d);
                var c = Instantiate(itemTemplate, itemTemplate.parent); c.gameObject.SetActive(true); spawned.Add(c.gameObject);
                var nm = Txt(c, "Name"); if (nm) nm.text = d.deco_name;
                var pr = Txt(c, "Price"); if (pr) pr.text = F(pr, ("n", d.price.ToString("N0")));
                Show(c, "Lock", st == Progress.DecoState.Locked);
                Show(c, "Owned", st == Progress.DecoState.Owned);
                var hint = Txt(c, "Hint");
                if (hint) { hint.gameObject.SetActive(st == Progress.DecoState.Locked); if (st == Progress.DecoState.Locked) hint.text = F(hint, ("hint", LockHint(d))); }
                var bg = c.GetComponent<Image>(); if (bg) bg.color = st == Progress.DecoState.Owned ? ownedTint : st == Progress.DecoState.Locked ? lockedTint : Color.white;
                var buy = c.Find("Buy")?.GetComponent<Button>();
                if (buy)
                {
                    buy.interactable = st == Progress.DecoState.Buyable;
                    var lb = Txt(buy.transform, "Label");
                    if (lb) lb.text = st switch
                    {
                        Progress.DecoState.Owned => T(wOwned),
                        Progress.DecoState.Locked => T(wLocked),
                        Progress.DecoState.Poor => F(wPoor, ("n", (d.price - p.acorn).ToString("N0"))),
                        _ => T(wBuy),
                    };
                    var dd = d; buy.onClick.AddListener(() => Buy(dd, false));
                }
            }
        }

        static string LockHint(DecoItemRow d) => DB.DecoLocks.TryGetValue(d.sell_feature ?? "", out var l) ? l.hint : "";

        void Buy(DecoItemRow d, bool confirmed)
        {
            var p = Progress.I; if (!p || d == null) return;
            if (p.DecoStateOf(d) != Progress.DecoState.Buyable) { SfxManager.Play("ui_deny"); return; }
            if (!confirmed && d.price >= DB.AcornConfig("confirm_price", 100) && confirmRoot)
            {
                pending = d;
                if (confirmText) confirmText.text = F(confirmText, ("name", d.deco_name), ("price", d.price.ToString("N0")));
                Confirm(true);
                return;
            }
            if (p.BuyDeco(d)) SfxManager.Play("cheese"); else SfxManager.Play("ui_deny");
            Draw();
        }

        void Confirm(bool on) { if (!on) pending = null; if (confirmRoot) confirmRoot.SetActive(on); }

        static void Show(Transform t, string child, bool on) { var c = t.Find(child); if (c) c.gameObject.SetActive(on); }
    }
}
