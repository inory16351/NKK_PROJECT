using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NKK
{
    // 한·영이 아닌 언어(일본어 · 중국어 · 포르투갈어 · 스페인어 · 러시아어 · 프랑스어 · 독일어 · 아랍어)는 글 전체를 그 언어 글꼴로 바꿔 끼움.
    // 씬 글꼴(주아 · 고운돋움 · IBM Plex KR)에는 이 글자들이 없어서, 대체 글꼴로 빈 글자만 채우면 한 줄 안에 글꼴이 섞임.
    // 글꼴 = Resources/LocFonts — 주아처럼 통통하고 귀여운 글꼴 (모두 OFL). 굵은 글꼴(주아 · Bold)은 Bold, 나머지는 Regular (두 벌 있는 글꼴만).
    //   일본어 = Mochiy Pop One · 중국어 = ZCOOL KuaiLe · 러시아어 = Nunito NKK · 포르투갈어·스페인어·프랑스어·독일어·아랍어 = Baloo Bhaijaan 2 NKK.
    //   (NKK = 굵기를 고정하고 아랍 글자 잇기 모양을 연결한 수정판, Assets/Fonts/OFL_*.txt). 없는 기호(※ ★ ♪ 등)는 고운돋움으로 (글꼴 자산의 대체 글꼴).
    // 아랍어는 글자 잇기(LocArabic, textPreprocessor) + 오른쪽→왼쪽 배치(isRightToLeftText, 아랍 글자가 있는 글만) + 자간 끔.
    //   정렬은 그대로 (화면 배치는 안 뒤집으므로, 넓은 칸의 왼쪽 정렬을 오른쪽으로 바꾸면 다른 글과 겹침).
    // 재질(외곽선 · 팝업 등)은 원래 재질을 복사해 새 글꼴 아틀라스로 바꿔 씀. TMP 가 글을 그릴 때마다 (TEXT_CHANGED_EVENT) 확인하므로 코드로 만든 글도 바뀜.
    public static class LocFont
    {
        const string Dir = "LocFonts/";

        static readonly Dictionary<string, TMP_FontAsset> fonts = new();
        static readonly HashSet<TMP_FontAsset> mine = new();
        static readonly Dictionary<(Material, TMP_FontAsset), Material> mats = new();
        // 원래 글꼴·재질 (한·영으로 돌아갈 때)
        static readonly Dictionary<TMP_Text, (TMP_FontAsset font, Material mat, bool kern)> orig = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            // 도메인 다시 읽기를 끈 에디터: 지난 플레이의 구독·기록이 남아 있으므로 지우고 다시
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnText);
            Loc.Changed -= Refresh;
            orig.Clear(); mats.Clear();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnText);
            Loc.Changed += Refresh;
        }

        static void OnText(Object o)
        {
            // 플레이 중에만 (에디터에서 바꾸면 바뀐 글꼴이 씬·프리팹에 저장됨)
            if (!Application.isPlaying || !Loc.OwnFont || o is not TMP_Text t) return;
            bool changed = Apply(t);
            // 템플릿을 복제(Instantiate)한 글은 글꼴은 이미 바뀌어 있지만 textPreprocessor(직렬화 안 됨)가 빠져 숫자가 뒤집혀 보임 (2026-11-03) → 여기서 붙임
            if (Loc.Lang == Loc.Arabic && (orig.ContainsKey(t) || mine.Contains(t.font)))
            {
                if (t.textPreprocessor != LocArabic.Instance) { t.textPreprocessor = LocArabic.Instance; t.enableKerning = false; changed = true; }
                bool rtl = LocArabic.Has(t.text);
                if (t.isRightToLeftText != rtl) { t.isRightToLeftText = rtl; changed = true; }
            }
            // 그리는 도중(이 이벤트 안)에 글꼴을 바꾸면 이번 그림은 옛 글꼴 글자 자리 + 새 아틀라스로 깨져 나옴 → 바로 다시 그림
            if (changed) t.ForceMeshUpdate();
        }

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
            return Font(Name(Loc.Lang, n.Contains("Bold") || n.Contains("Jua")));
        }

        // 언어별 글꼴 이름 (Resources/LocFonts/<이름> SDF)
        public static string Name(string lang, bool bold) => lang switch
        {
            Loc.Japanese => "MochiyPopOne-Regular",
            Loc.Chinese => "ZCOOLKuaiLe-Regular",
            Loc.Russian => bold ? "NunitoNKK-Bold" : "NunitoNKK-Regular",
            _ => bold ? "BalooBhaijaan2NKK-Bold" : "BalooBhaijaan2NKK-Regular",
        };

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

        // 바꿨으면 true
        public static bool Apply(TMP_Text t)
        {
            var src = t ? t.font : null;
            if (!src || mine.Contains(src) || t.GetComponent<LocFontKeep>()) return false;
            var font = For(src);
            if (!font) return false;
            var mat = t.fontSharedMaterial;
            orig[t] = (src, mat, t.enableKerning);
            t.font = font;
            t.fontSharedMaterial = Mat(mat, src, font);
            if (Loc.Lang == Loc.Arabic)
            {
                t.textPreprocessor = LocArabic.Instance;
                t.isRightToLeftText = LocArabic.Has(t.text);
                // 글꼴의 자간 값은 왼→오 기준이라 오른쪽→왼쪽 배치에선 글자가 겹침 (ر + ا 등)
                t.enableKerning = false;
            }
            return true;
        }

        static void Restore(TMP_Text t)
        {
            if (!orig.TryGetValue(t, out var o)) return;
            if (t.font != o.font) { t.font = o.font; if (o.mat) t.fontSharedMaterial = o.mat; }
            if (t.textPreprocessor == LocArabic.Instance) { t.textPreprocessor = null; t.isRightToLeftText = false; }
            t.enableKerning = o.kern;
            orig.Remove(t);
        }

        // 언어를 바꾸면 열린 글 전부 다시 (한·영 ↔ 다른 언어, 일본어 ↔ 중국어 ↔ 아랍어)
        static void Refresh()
        {
            if (!Application.isPlaying) return;
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
