using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NKK
{
    // 한·영이 아닌 언어(일본어 · 중국어 · 포르투갈어 · 스페인어 · 러시아어)는 글 전체를 그 언어 글꼴로 바꿔 끼움.
    // 씬 글꼴(주아 · 고운돋움 · IBM Plex KR)에는 이 글자들이 없어서, 대체 글꼴로 빈 글자만 채우면 한 줄 안에 글꼴이 섞임.
    // 글꼴 = Resources/LocFonts 의 Resource Han Rounded (둥근 글꼴, OFL — 일본어 = JP, 중국어 = CN, 나머지 = JP 의 라틴·키릴 글자). 굵은 글꼴(주아 · Bold)은 Bold, 나머지는 Regular.
    // 재질(외곽선 · 팝업 등)은 원래 재질을 복사해 새 글꼴 아틀라스로 바꿔 씀. TMP 가 글을 그릴 때마다 (TEXT_CHANGED_EVENT) 확인하므로 코드로 만든 글도 바뀜.
    public static class LocFont
    {
        const string Dir = "LocFonts/";

        static readonly Dictionary<string, TMP_FontAsset> fonts = new();
        static readonly HashSet<TMP_FontAsset> mine = new();
        static readonly Dictionary<(Material, TMP_FontAsset), Material> mats = new();
        // 원래 글꼴·재질 (한·영으로 돌아갈 때)
        static readonly Dictionary<TMP_Text, (TMP_FontAsset font, Material mat)> orig = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnText);
            Loc.Changed += Refresh;
        }

        static void OnText(Object o) { if (Loc.OwnFont && o is TMP_Text t) Apply(t); }

        static TMP_FontAsset Font(string name)
        {
            if (!fonts.TryGetValue(name, out var f))
            {
                fonts[name] = f = Resources.Load<TMP_FontAsset>(Dir + name + " SDF");
                if (f) mine.Add(f); else Debug.LogWarning($"[LocFont] 글꼴 없음: Resources/{Dir}{name} SDF");
            }
            return f;
        }

        static TMP_FontAsset For(TMP_FontAsset src)
        {
            string n = src.name;
            bool bold = n.Contains("Bold") || n.Contains("Jua");
            string family = Loc.Lang == Loc.Chinese ? "ResourceHanRoundedCN" : "ResourceHanRoundedJP";
            return Font(family + (bold ? "-Bold" : "-Regular"));
        }

        // 원래 재질의 효과(외곽선 · 그림자 · 색)를 그대로 두고 아틀라스만 새 글꼴 것으로
        static Material Mat(Material src, TMP_FontAsset srcFont, TMP_FontAsset font)
        {
            if (!src || (srcFont && src == srcFont.material)) return font.material;
            if (mats.TryGetValue((src, font), out var m) && m) return m;
            m = new Material(src) { name = src.name + " (" + font.name + ")" };
            m.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
            m.SetFloat(ShaderUtilities.ID_TextureWidth, font.atlasWidth);
            m.SetFloat(ShaderUtilities.ID_TextureHeight, font.atlasHeight);
            m.SetFloat(ShaderUtilities.ID_GradientScale, font.atlasPadding + 1);
            m.SetFloat(ShaderUtilities.ID_WeightNormal, font.normalStyle);
            m.SetFloat(ShaderUtilities.ID_WeightBold, font.boldStyle);
            mats[(src, font)] = m;
            return m;
        }

        public static void Apply(TMP_Text t)
        {
            var src = t ? t.font : null;
            if (!src || mine.Contains(src) || t.GetComponent<LocFontKeep>()) return;
            var font = For(src);
            if (!font) return;
            var mat = t.fontSharedMaterial;
            orig[t] = (src, mat);
            t.font = font;
            t.fontSharedMaterial = Mat(mat, src, font);
        }

        static void Restore(TMP_Text t)
        {
            if (!orig.TryGetValue(t, out var o)) return;
            if (t.font != o.font) { t.font = o.font; if (o.mat) t.fontSharedMaterial = o.mat; }
            orig.Remove(t);
        }

        // 언어를 바꾸면 열린 글 전부 다시 (한·영 ↔ 다른 언어, 일본어 ↔ 중국어)
        static void Refresh()
        {
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Restore(t);
                if (Loc.OwnFont) Apply(t);
            }
            var dead = new List<TMP_Text>();
            foreach (var t in orig.Keys) if (!t) dead.Add(t);
            foreach (var t in dead) orig.Remove(t);
        }

        // 씬을 열 때 (LocScene): 첫 화면부터 바뀐 글꼴로 (TEXT_CHANGED_EVENT 는 한 번 그린 뒤라 한 프레임 늦음)
        public static void ApplyScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!Loc.OwnFont) return;
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.gameObject.scene == scene) Apply(t);
        }
    }
}
