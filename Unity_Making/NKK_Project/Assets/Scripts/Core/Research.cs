using System.Collections.Generic;
using NKK.Items;
using UnityEngine;

namespace NKK
{
    // 연구자료 (웹게임 meta.js earnResearch · researchDrop).
    // · 층 클리어 = 대량 (Heist), 물건·가구를 부수면 가끔 조금: 가구 12% (1~3) · 물건 1.2% (1)
    // · 얻으면 Progress.research (저장, 판이 끝나도 남음) + GameManager.RunResearch (이번 판, 결과 창)
    // · 화면에 보이면 그 자리에 종이 아이콘(rg_paper)이 통 튀어 오르고 "연구자료+{n}" 글 (웹: 📑+n)
    public class Research : MonoBehaviour
    {
        public static Research I { get; private set; }

        public GameManager Game;

        [Header("드랍 (웹 researchDrop)")]
        [Tooltip("가구를 부쉈을 때 확률 · 양 (최소~최대)")] public float furnitureChance = 0.12f;
        public Vector2Int furnitureAmount = new(1, 3);
        [Tooltip("물건을 부쉈을 때 확률 · 양")] public float itemChance = 0.012f;
        public int itemAmount = 1;

        [Header("표시")]
        [Tooltip("글 (자리표시 {n})")] public string popupText;
        public Color popupColor = new(0.75f, 0.91f, 1f);
        [Tooltip("꺼 둔 종이 아이콘 템플릿 (월드)")] public SpriteRenderer iconTemplate;
        [Tooltip("아이콘 폭 · 올라가는 높이 (게임 단위) · 보이는 시간 (초)")] public float iconSize = 48, iconRise = 110, iconTime = 1;

        class Icon { public SpriteRenderer r; public float x, y, t, spin; }
        readonly List<Icon> icons = new();
        readonly Stack<SpriteRenderer> pool = new();

        void Awake()
        {
            I = this;
            if (iconTemplate) iconTemplate.gameObject.SetActive(false);
        }
        void OnDestroy() { if (I == this) I = null; }

        // 얻기 (show = 그 자리에 아이콘·글)
        public void Earn(int n, float x, float y, bool show = true)
        {
            if (n <= 0) return;
            var p = Progress.I;
            if (p) { p.research += n; p.Save(); }
            if (Game) Game.RunResearch += n;
            if (!show || !OnScreen(x, y)) return;
            var fx = FxManager.I;
            if (fx && !string.IsNullOrEmpty(popupText)) fx.Popup(x, y, popupText.Replace("{n}", n.ToString()), popupColor, 18, 0.9f, 60);
            if (iconTemplate)
            {
                var r = pool.Count > 0 ? pool.Pop() : Instantiate(iconTemplate, iconTemplate.transform.parent);
                r.gameObject.SetActive(true);
                icons.Add(new Icon { r = r, x = x, y = y, t = 0, spin = Random.Range(-1f, 1f) });
            }
        }

        // 물건·가구 박살 (ItemManager.OnSmashed)
        public void OnSmashed(Item it)
        {
            if (!it || GameOver.Active || Heist.Active) return;
            if (it.Data.IsFurniture) { if (Random.value < furnitureChance) Earn(Random.Range(furnitureAmount.x, furnitureAmount.y + 1), it.x, it.y); }
            else if (Random.value < itemChance) Earn(itemAmount, it.x, it.y);
        }

        static bool OnScreen(float x, float y)
        {
            var cam = Camera.main; if (!cam) return false;
            var v = cam.WorldToViewportPoint(World.ToUnity(x, y));
            return v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = icons.Count - 1; i >= 0; i--)
            {
                var c = icons[i];
                c.t += dt;
                float k = c.t / iconTime;
                if (k >= 1) { c.r.gameObject.SetActive(false); pool.Push(c.r); icons.RemoveAt(i); continue; }
                float rise = 1 - (1 - k) * (1 - k);                                   // 통 튀어 올라 느려짐
                float q = k / 0.25f, pop = q < 1 ? 1 + 0.3f * Mathf.Sin(q * Mathf.PI) - (1 - q) * 0.6f : 1;     // 작게 → 통 → 제 크기
                float w = iconSize * World.U / Mathf.Max(0.001f, c.r.sprite ? c.r.sprite.bounds.size.x : 1) * pop;
                c.r.transform.position = World.ToUnity(c.x, c.y, 40 + iconRise * rise);
                c.r.transform.localScale = Vector3.one * w;
                c.r.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(c.t * 9) * 12 + c.spin * 20);
                c.r.sortingOrder = World.SortOrder(c.y) + 60;
                var col = c.r.color; col.a = k > 0.7f ? (1 - k) / 0.3f : 1; c.r.color = col;
            }
        }

        [ContextMenu("테스트: 화면 가운데에 연구자료 +2")]
        void TestDrop()
        {
            if (!Application.isPlaying || !Camera.main) return;
            var p = World.FromUnity(Camera.main.transform.position);
            Earn(2, p.x, p.y);
        }
    }
}
