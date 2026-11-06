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
    // 장식 적용 = '스킨 적용' 탭 (2026-11-04 사용자): 종류마다 한 줄, ◀ ▶ 로 고름. 상품 카드에서는 사기만 (보유 = '보유').
    //   고를 수 있는 것: 산 장식 중 그림이 있는 것 + 아지트 배경 · 소품 4종(낮잠침대 · 회의 테이블 · 책장 · 치즈창고)은 달아 본 훈장 배경 테마 (−fromTier) · 처음 아지트 (−1) · 자동(0 = 지금 훈장 배경 따라).
    // 글은 씬 TMP (자리표시 {n} {name} {price} {hint}), 코드는 값만.
    public class AcornShopPage : MonoBehaviour
    {
        public LobbyManager manager;
        [Tooltip("보유 도토리 ({n})")] public TMP_Text acornText;

        [Header("구역 탭")]
        [Tooltip("탭 버튼 (자식 Label · Sel) — zoneIds 와 같은 순서")] public Button[] zoneTabs = new Button[0];
        [Tooltip("탭 구역: Lobby · Alley · Common = 도토리 테이블 Deco_Category.zone · RatFriend = 쥐 친구 종류 · Skin = 스킨 적용")] public string[] zoneIds = { "Lobby", "Alley", "Common", "RatFriend", "Skin" };

        [Header("스킨 적용 탭")]
        public LobbyHome home;
        [Tooltip("줄 템플릿 (꺼져 있음): 자식 Name(종류 이름) · Value(고른 것 {name}) · Prev · Next(Button)")] public RectTransform skinTemplate;
        [Tooltip("보일 종류 (도토리 테이블 Deco_Category) — 이 순서로")] public string[] skinCats = { "LobbyBg", "NapBed", "Table", "Bookshelf", "CheeseStore", "Badge", "Wheel", "Manhole", "Arcade", "Market", "Lab", "AugLab", "AlleyBg", "UiButton", "Cursor" };
        [Tooltip("훈장 배경 테마를 고를 수 있는 종류 (배경 · 소품)")] public string[] themedCats = { "LobbyBg", "NapBed", "Table", "Bookshelf", "CheeseStore" };
        [Tooltip("글 (씬 Words): 자동(배경: 훈장 따라) · 자동(소품: 배경 따라) · 기본 · 처음 아지트")] public TMP_Text wAutoBg, wAutoProp, wDefault, wBaseTheme;
        public Color skinPickedTint = new(1f, 0.92f, 0.7f);

        [Header("상품 카드")]
        [Tooltip("카드 템플릿 (꺼져 있음): 자식 Name · Price({n}) · Buy(Button, 자식 Label) · Lock · Hint({hint}) · Owned")] public RectTransform itemTemplate;
        [Tooltip("카드 글 (씬 Words): 사기 · 보유 · 도토리 부족 ({n} 모자람) · 잠김")] public TMP_Text wBuy, wOwned, wPoor, wLocked;
        [Tooltip("그림이 준비된 보유 장식 (LobbyHome.decoLooks): 적용하기 · 적용 중 (누르면 기본으로)")] public TMP_Text wEquip, wEquipped;
        public Color equippedTint = new(1f, 0.92f, 0.7f);
        public Color ownedTint = new(0.8f, 0.95f, 0.8f), lockedTint = new(0.7f, 0.66f, 0.62f);

        [Header("구매 확인 창 (모든 상품)")]
        public GameObject confirmRoot;
        [Tooltip("확인 글 ({name} {price})")] public TMP_Text confirmText;
        public Button confirmYes, confirmNo;
        [Tooltip("사기 전 미리보기 (지금 / 산 뒤 밀어서 비교, 2026-11-06 사용자) — 비우면 글만")] public ShopPreview preview;

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
            if (skinTemplate) { T(Txt(skinTemplate, "Value")); skinTemplate.gameObject.SetActive(false); }
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

        [Tooltip("튜토리얼 강조 id: 첫 번째 잠긴 카드 (잠긴 카드가 없으면 강조 없음)")] public string lockTargetId = "shop_lock";

        void Draw()
        {
            var p = Progress.I; if (!p || !DB || !itemTemplate) return;
            foreach (var g in spawned) Destroy(g);
            spawned.Clear();
            RectTransform firstLocked = null;
            if (acornText) acornText.text = F(acornText, ("n", p.acorn.ToString("N0")));
            if (zone == "Skin")
            {
                for (int i = 0; i < zoneTabs.Length && i < zoneIds.Length; i++) if (zoneTabs[i]) Show(zoneTabs[i].transform, "Sel", zoneIds[i] == zone);
                DrawSkin(); return;
            }
            for (int i = 0; i < zoneTabs.Length && i < zoneIds.Length; i++) if (zoneTabs[i]) Show(zoneTabs[i].transform, "Sel", zoneIds[i] == zone);
            foreach (var d in DB.DecoItems)
            {
                if (ZoneOf(d) != zone) continue;
                var st = p.DecoStateOf(d);
                var c = Instantiate(itemTemplate, itemTemplate.parent); c.gameObject.SetActive(true); spawned.Add(c.gameObject);
                if (!firstLocked && st == Progress.DecoState.Locked) firstLocked = c;
                var nm = Txt(c, "Name"); if (nm) nm.text = d.deco_name;
                var pr = Txt(c, "Price"); if (pr) pr.text = F(pr, ("n", d.price.ToString("N0")));
                Show(c, "Lock", st == Progress.DecoState.Locked);
                Show(c, "Owned", st == Progress.DecoState.Owned);
                var hint = Txt(c, "Hint");
                if (hint) { hint.gameObject.SetActive(st == Progress.DecoState.Locked); if (st == Progress.DecoState.Locked) hint.text = F(hint, ("hint", LockHint(d))); }
                bool canEquip = false;            // 적용은 '스킨 적용' 탭에서
                bool equipped = canEquip && p.IsDecoEquipped(d);
                var bg = c.GetComponent<Image>(); if (bg) bg.color = equipped ? equippedTint : st == Progress.DecoState.Owned ? ownedTint : st == Progress.DecoState.Locked ? lockedTint : Color.white;
                var buy = c.Find("Buy")?.GetComponent<Button>();
                if (buy)
                {
                    buy.interactable = st == Progress.DecoState.Buyable || canEquip;
                    var lb = Txt(buy.transform, "Label");
                    if (lb) lb.text = st switch
                    {
                        Progress.DecoState.Owned when canEquip => equipped ? T(wEquipped) : T(wEquip),
                        Progress.DecoState.Owned => T(wOwned),
                        Progress.DecoState.Locked => T(wLocked),
                        Progress.DecoState.Poor => F(wPoor, ("n", (d.price - p.acorn).ToString("N0"))),
                        _ => T(wBuy),
                    };
                    var dd = d; buy.onClick.AddListener(() => Buy(dd, false));
                }
            }
            // 튜토리얼 '자물쇠 걸린 장식' 대사가 가리킬 카드 (상점 목록 전체를 가리키면 대사창 · 화살표가 화면을 덮음)
            var tc = firstLocked;
            if (tc && !string.IsNullOrEmpty(lockTargetId)) { var tt = tc.gameObject.AddComponent<NKK.Tutorial.TutorialTarget>(); tt.id = lockTargetId; tt.zoom = 1; }
        }

        static string LockHint(DecoItemRow d) => DB.DecoLocks.TryGetValue(d.sell_feature ?? "", out var l) ? l.hint : "";

        void Buy(DecoItemRow d, bool confirmed)
        {
            var p = Progress.I; if (!p || d == null) return;
            if (p.DecoStateOf(d) != Progress.DecoState.Buyable) { SfxManager.Play("ui_deny"); return; }
            if (!confirmed && confirmRoot)          // 가격 상관없이 항상 확인 창 (2026-11-04 사용자, 예전엔 confirm_price 이상만)
            {
                pending = d;
                if (confirmText) confirmText.text = F(confirmText, ("name", d.deco_name), ("price", d.price.ToString("N0")));
                Confirm(true);
                if (preview) preview.Show(d);
                return;
            }
            if (p.BuyDeco(d)) SfxManager.Play("cheese"); else SfxManager.Play("ui_deny");
            Draw();
        }

        // ── 스킨 적용 탭 ──
        void DrawSkin()
        {
            var p = Progress.I; if (!p || !skinTemplate) return;
            foreach (var cat in skinCats)
            {
                var opts = Options(cat);
                int cur = p.EquippedDeco(cat), i = opts.FindIndex(o => o.id == cur); if (i < 0) i = 0;
                var c = Instantiate(skinTemplate, skinTemplate.parent); c.gameObject.SetActive(true); spawned.Add(c.gameObject);
                var nm = Txt(c, "Name"); if (nm) nm.text = CatName(cat);
                var v = Txt(c, "Value"); if (v) v.text = F(v, ("name", opts[i].name));
                var bg = c.GetComponent<Image>(); if (bg) bg.color = opts[i].id != 0 ? skinPickedTint : Color.white;
                int n = opts.Count, at = i; string cc = cat;
                foreach (var (child, step) in new[] { ("Prev", -1), ("Next", 1) })
                {
                    var b = c.Find(child)?.GetComponent<Button>(); if (!b) continue;
                    b.interactable = n > 1;
                    int st = step; b.onClick.AddListener(() => { Progress.I.SetDeco(cc, opts[(at + st + n) % n].id); SfxManager.Play("ui_click"); Draw(); });
                }
            }
        }

        // 한 종류에서 고를 수 있는 것 (id, 보일 이름)
        List<(int id, string name)> Options(string cat)
        {
            var l = new List<(int, string)>();
            bool themed = System.Array.IndexOf(themedCats, cat) >= 0;
            l.Add((0, themed ? T(cat == "LobbyBg" ? wAutoBg : wAutoProp) : T(wDefault)));
            if (themed && home)
                foreach (var (id, zoneId) in home.TierThemes())
                    l.Add((id, id == -1 ? T(wBaseTheme) : ZoneName(zoneId)));
            foreach (var d in DB.DecoItems)
                if (d.category == cat && Progress.I.OwnsDeco(d.deco_id) && LobbyHome.HasArt(d.deco_id)) l.Add((d.deco_id, ThemeName(d)));
            return l;
        }

        static string CatName(string cat) { foreach (var c in DB.DecoCategories) if (c.category == cat) return c.cat_name; return cat; }
        static string ZoneName(int zoneId) { foreach (var z in DB.Zones) if (z.zone_id == zoneId) return z.zone_name; return zoneId.ToString(); }
        // '낮잠침대 · 도토리 숲' → '도토리 숲'
        static string ThemeName(DecoItemRow d) { var n = d.deco_name ?? ""; int k = n.LastIndexOf(" · "); return k >= 0 ? n.Substring(k + 3) : n; }

        void Confirm(bool on) { if (!on) pending = null; if (confirmRoot) confirmRoot.SetActive(on); }

        static void Show(Transform t, string child, bool on) { var c = t.Find(child); if (c) c.gameObject.SetActive(on); }
    }
}
