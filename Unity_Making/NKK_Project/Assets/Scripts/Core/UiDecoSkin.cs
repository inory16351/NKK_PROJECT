using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NKK
{
    // 도토리 상점 'UI 버튼' 장식 (2026-11-04). 설정 에셋 = Resources/UiDecoSkin.asset.
    // 모든 씬의 UI Image · SpriteRenderer 중 UI 키트 버튼 그림(originals: 판자 4색 · 둥근 2 · pill · key)을 쓰는 것을 적용한 장식 그림으로 바꿈.
    // 같은 순서의 장식 그림끼리 서로 바꾸므로 되돌리기도 됨 (복제된 템플릿 포함). 씬이 열릴 때 · 장식이 바뀔 때 다시 훑음.
    [CreateAssetMenu(menuName = "NKK/UI Deco Skin")]
    public class UiDecoSkin : ScriptableObject
    {
        [Tooltip("바꿀 원래 UI 그림 (순서 = 장식 그림 순서)")] public Sprite[] originals;
        [Serializable] public class Theme { public int decoId; public Sprite[] sprites; }
        [Tooltip("장식 id (종류 UiButton) → originals 와 같은 순서의 그림 (9-슬라이스 테두리 같게)")] public Theme[] themes;

        [Header("버튼 글자 통일 (2026-11-05 사용자)")]
        [Tooltip("UI 키트 버튼 위 어두운 글자 (나무 판자용 갈색) → 다른 버튼 글자와 같은 색 · 테두리 재질로 (네온 판자에서도 읽히게)")] public Color labelColor = new(1f, 0.96f, 0.86f, 1f);
        [Tooltip("다른 버튼 글자가 쓰는 재질 (Jua-Regular SDF Plank: 진한 테두리)")] public Material labelMaterial;
        [Tooltip("이보다 어두운 글자만 바꿈 (RGB 중 가장 큰 값)")] public float darkBelow = 0.45f;

        static UiDecoSkin skin;
        static Dictionary<Sprite, int> index;
        static int applied = -1;

        public static void RegisterReady()
        {
            var s = skin ? skin : Resources.Load<UiDecoSkin>("UiDecoSkin");
            if (s && s.themes != null) foreach (var t in s.themes) if (t != null && t.sprites != null && t.sprites.Length > 0) DecoArt.Ready.Add(t.decoId);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            skin = Resources.Load<UiDecoSkin>("UiDecoSkin");
            if (!skin || skin.originals == null) return;
            index = new Dictionary<Sprite, int>();
            for (int i = 0; i < skin.originals.Length; i++) if (skin.originals[i]) index[skin.originals[i]] = i;
            if (skin.themes != null) foreach (var t in skin.themes) if (t?.sprites != null) for (int i = 0; i < t.sprites.Length; i++) if (t.sprites[i]) index[t.sprites[i]] = i;
            RegisterReady();
            SceneManager.sceneLoaded += (_, _) => Scan(true);
            var go = new GameObject("UiDecoSkin") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            Scan(true);
        }

        static Sprite[] Set(int deco)
        {
            if (deco != 0 && skin.themes != null) foreach (var t in skin.themes) if (t != null && t.decoId == deco && t.sprites != null && t.sprites.Length > 0) return t.sprites;
            return skin.originals;
        }

        static Sprite Pick(Sprite[] set, Sprite cur)
        {
            if (!cur || !index.TryGetValue(cur, out int i)) return null;
            var s = i < set.Length ? set[i] : null;
            return s ? s : skin.originals[i];
        }

        // 상점 '사기 전 미리보기' 가 찍기 직전 · 직후 (같은 프레임): 지금 적용(미리보기 포함)대로 바로 바꿈 (ShopPreview)
        public static void ApplyNow() => Scan(false);
        // UI 키트 그림 하나를 그 장식(0 = 기본)의 그림으로 — 상점 미리보기 견본 버튼
        public static Sprite SpriteFor(Sprite cur, int deco)
        {
            if (!skin) skin = Resources.Load<UiDecoSkin>("UiDecoSkin");
            if (!skin || index == null) return cur;
            var s = Pick(Set(deco), cur); return s ? s : cur;
        }

        // force = 씬이 새로 열림 (지금 장식이 기본이어도 훑음)
        static void Scan(bool force)
        {
            if (!skin || index == null) return;
            int deco = DecoArt.Equipped("UiButton");
            if (!force && deco == applied) return;
            applied = deco;
            var set = Set(deco);
            foreach (var im in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { var s = Pick(set, im.sprite); if (s && s != im.sprite) im.sprite = s; }
            foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { var s = Pick(set, sr.sprite); if (s && s != sr.sprite) sr.sprite = s; }
            if (force) UnifyLabels();
        }

        // UI 키트 버튼 (판자 · 둥근 · pill · key) 위 어두운 갈색 글자 → 다른 버튼 글자와 같은 크림색 + 테두리 (테마와 상관없이 같게)
        static void UnifyLabels()
        {
            foreach (var t in FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var c = t.color; if (Mathf.Max(c.r, c.g, c.b) >= skin.darkBelow) continue;
                var b = t.GetComponentInParent<Button>(true);
                var im = b ? b.targetGraphic as Image : null;
                if (!im || !im.sprite || !index.ContainsKey(im.sprite)) continue;
                t.color = new Color(skin.labelColor.r, skin.labelColor.g, skin.labelColor.b, c.a);
                if (skin.labelMaterial && t.font && skin.labelMaterial.mainTexture == t.font.material.mainTexture) t.fontSharedMaterial = skin.labelMaterial;
            }
        }

        class Runner : MonoBehaviour
        {
            void Update() { if (DecoArt.Equipped("UiButton") != applied) Scan(false); }
        }
    }
}
