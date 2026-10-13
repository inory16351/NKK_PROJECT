using System.Collections.Generic;
using NKK.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 유전자 추출기 페이지 (탈출 준비실 Page_gacha). 로직 = Core/GeneGacha, 연출 = GachaShow (믹서기). 글은 씬 TMP (자리표시만 채움)
    public class GachaPage : MonoBehaviour
    {
        public LobbyManager manager;
        public GachaShow show;
        [Tooltip("1회 · 10회 · 무료 10회 버튼")] public Button one, ten, freeTen;
        [Tooltip("1회 비용 ({cost}) · 10회 비용 ({cost}) · 무료 10회 ({n})")] public TMP_Text oneCost, tenCost, freeText;
        [Tooltip("확률 정보 ({r0}~{r5} 일반~신화 %, {s0}~{s5} 캡슐당 조각)")] public TMP_Text rates;
        [Tooltip("천장: 전설 이상 ({n} {max}) · 신화 ({n} {max}) · 게이지 (Image Filled)")] public TMP_Text pityLegend, pityMythic;
        public Image pityLegendBar, pityMythicBar;
        [Tooltip("관심 종: 묶음 (18층부터) · 잠김 안내 · 전설/신화 이름 ({name}, 없으면 noneName) · 바꾸기 버튼 (◀ ▶)")] public GameObject wishRoot, wishLocked;
        public TMP_Text wishLegend, wishMythic;
        public string noneName = "-";
        public Button legPrev, legNext, mytPrev, mytNext;
        [Tooltip("믹서기 그림 (가만히 있을 때 살랑)")] public RectTransform idleBlender;

        readonly Dictionary<TMP_Text, string> tpl = new();
        string T(TMP_Text t) { if (!t) return ""; if (!tpl.TryGetValue(t, out var s)) tpl[t] = s = t.text; return s; }

        void Awake()
        {
            foreach (var t in new[] { oneCost, tenCost, freeText, rates, pityLegend, pityMythic, wishLegend, wishMythic }) T(t);
            if (one) one.onClick.AddListener(() => Pull(1));
            if (ten) ten.onClick.AddListener(() => Pull(10));
            if (freeTen) freeTen.onClick.AddListener(() => Pull(10));
            if (legPrev) legPrev.onClick.AddListener(() => Wish(Grade.Legendary, -1));
            if (legNext) legNext.onClick.AddListener(() => Wish(Grade.Legendary, 1));
            if (mytPrev) mytPrev.onClick.AddListener(() => Wish(Grade.Mythic, -1));
            if (mytNext) mytNext.onClick.AddListener(() => Wish(Grade.Mythic, 1));
        }

        void OnEnable()
        {
            Render();
            // 연출 중 꺼졌으면 (강제 종료) 결과 복구
            var p = Progress.I;
            if (p && p.gacha.pending.Count > 0 && show && !show.Playing) show.ShowResultsOnly(FromLog(p.gacha.pending));
        }

        static List<GeneGacha.Result> FromLog(List<Progress.GachaLog> l)
        {
            var r = new List<GeneGacha.Result>();
            foreach (var x in l) r.Add(new GeneGacha.Result { code = x.code, grade = E.P<Grade>(x.grade), shards = x.shards, isNew = x.isNew, source = x.source, wish = x.source == "Wish" });
            return r;
        }

        public void Render()
        {
            var p = Progress.I; if (!p || !GameDatabase.Instance) return;
            int free = GeneGacha.FreeTen;
            if (freeTen) freeTen.gameObject.SetActive(free > 0);
            if (ten) ten.gameObject.SetActive(free <= 0);
            if (freeText) freeText.text = T(freeText).Replace("{n}", free.ToString());
            if (oneCost) oneCost.text = T(oneCost).Replace("{cost}", LobbyManager.Fmt(GeneGacha.CostOne));
            if (tenCost) tenCost.text = T(tenCost).Replace("{cost}", LobbyManager.Fmt(GeneGacha.CostTen));
            if (one) one.interactable = GeneGacha.CanPull(1);
            if (ten) ten.interactable = GeneGacha.CanPull(10);
            if (rates)
            {
                var s = T(rates);
                for (int g = 0; g < 6; g++) s = s.Replace("{r" + g + "}", (GeneGacha.RateOf((Grade)g) * 100).ToString("0.#")).Replace("{s" + g + "}", GeneGacha.ShardsOf((Grade)g).ToString());
                rates.text = s;
            }
            int lp = p.gacha.legendPity, mp = p.gacha.mythicPity, lm = GeneGacha.LegendPityMax, mm = GeneGacha.MythicPityMax;
            if (pityLegend) pityLegend.text = T(pityLegend).Replace("{n}", lp.ToString()).Replace("{max}", lm.ToString());
            if (pityMythic) pityMythic.text = T(pityMythic).Replace("{n}", mp.ToString()).Replace("{max}", mm.ToString());
            if (pityLegendBar) pityLegendBar.fillAmount = lm > 0 ? (float)lp / lm : 0;
            if (pityMythicBar) pityMythicBar.fillAmount = mm > 0 ? (float)mp / mm : 0;
            bool wish = GeneGacha.WishUnlocked;
            if (wishRoot) wishRoot.SetActive(wish);
            if (wishLocked) wishLocked.SetActive(!wish);
            if (wishLegend) wishLegend.text = T(wishLegend).Replace("{name}", NameOf(p.gacha.wishLegend));
            if (wishMythic) wishMythic.text = T(wishMythic).Replace("{name}", NameOf(p.gacha.wishMythic));
        }

        string NameOf(string code)
        {
            if (!string.IsNullOrEmpty(code)) foreach (var r in GameDatabase.Instance.Rats.Values) if (r.code_id == code) return r.character_name;
            return noneName;
        }

        void Wish(Grade g, int dir)
        {
            var pool = GeneGacha.WishPool(g); if (pool.Count == 0) return;
            var cur = g == Grade.Legendary ? Progress.I.gacha.wishLegend : Progress.I.gacha.wishMythic;
            int i = pool.FindIndex(r => r.code_id == cur);
            i = i < 0 ? (dir > 0 ? 0 : pool.Count - 1) : (i + dir + pool.Count) % pool.Count;
            GeneGacha.SetWish(g, pool[i].code_id);
            SfxManager.Play("ui_click");
            Render();
        }

        public void Pull(int n)
        {
            if (show && show.Playing) return;
            var res = GeneGacha.Pull(n);
            if (res.Count == 0) { SfxManager.Play("ui_deny"); return; }
            Render();
            if (show) show.Play(res, n, this); else GeneGacha.ClearPending();
        }

        void Update()
        {
            if (idleBlender) idleBlender.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.6f) * 1.2f);
        }
    }
}
