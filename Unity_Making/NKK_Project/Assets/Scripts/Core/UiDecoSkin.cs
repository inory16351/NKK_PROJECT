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
        }

        class Runner : MonoBehaviour
        {
            void Update() { if (DecoArt.Equipped("UiButton") != applied) Scan(false); }
        }
    }
}
