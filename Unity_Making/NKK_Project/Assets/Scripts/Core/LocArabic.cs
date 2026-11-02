using System.Collections.Generic;
using System.Text;
using TMPro;

namespace NKK
{
    // 아랍어 글을 TMP 가 그릴 수 있게: 글자 잇기 (앞뒤 글자에 따라 단독·끝·처음·가운데 모양 — 유니코드 표시형 FE70~FEFC) + 라암·알리프 합자.
    // TMP 는 글자 모양을 스스로 바꾸지 못함. 오른쪽→왼쪽 배치는 TMP 의 isRightToLeftText 가 하고 (글 순서 그대로 오른쪽부터 놓음),
    // 여기서는 숫자·라틴 글자 덩어리만 미리 뒤집어 둠 (뒤집어 놓이면 원래대로 읽힘). <태그> 는 TMP 가 따로 읽으므로 그대로.
    // LocFont 가 아랍어일 때 TMP_Text.textPreprocessor 로 끼움 → .text 는 그대로, 그릴 때만 바뀜 ({n} 을 채운 뒤의 글에도 적용).
    public class LocArabic : ITextPreprocessor
    {
        public static readonly LocArabic Instance = new();

        // 글자 → (단독, 끝, 처음, 가운데). 처음·가운데가 0 이면 뒤 글자와 안 이어지는 글자 (알리프 · 달 · 라 · 와우 …)
        static readonly Dictionary<char, (char iso, char fin, char ini, char med)> forms = new()
        {
            ['ء'] = ('ﺀ', '\0', '\0', '\0'),
            ['آ'] = ('ﺁ', 'ﺂ', '\0', '\0'),
            ['أ'] = ('ﺃ', 'ﺄ', '\0', '\0'),
            ['ؤ'] = ('ﺅ', 'ﺆ', '\0', '\0'),
            ['إ'] = ('ﺇ', 'ﺈ', '\0', '\0'),
            ['ئ'] = ('ﺉ', 'ﺊ', 'ﺋ', 'ﺌ'),
            ['ا'] = ('ﺍ', 'ﺎ', '\0', '\0'),
            ['ب'] = ('ﺏ', 'ﺐ', 'ﺑ', 'ﺒ'),
            ['ة'] = ('ﺓ', 'ﺔ', '\0', '\0'),
            ['ت'] = ('ﺕ', 'ﺖ', 'ﺗ', 'ﺘ'),
            ['ث'] = ('ﺙ', 'ﺚ', 'ﺛ', 'ﺜ'),
            ['ج'] = ('ﺝ', 'ﺞ', 'ﺟ', 'ﺠ'),
            ['ح'] = ('ﺡ', 'ﺢ', 'ﺣ', 'ﺤ'),
            ['خ'] = ('ﺥ', 'ﺦ', 'ﺧ', 'ﺨ'),
            ['د'] = ('ﺩ', 'ﺪ', '\0', '\0'),
            ['ذ'] = ('ﺫ', 'ﺬ', '\0', '\0'),
            ['ر'] = ('ﺭ', 'ﺮ', '\0', '\0'),
            ['ز'] = ('ﺯ', 'ﺰ', '\0', '\0'),
            ['س'] = ('ﺱ', 'ﺲ', 'ﺳ', 'ﺴ'),
            ['ش'] = ('ﺵ', 'ﺶ', 'ﺷ', 'ﺸ'),
            ['ص'] = ('ﺹ', 'ﺺ', 'ﺻ', 'ﺼ'),
            ['ض'] = ('ﺽ', 'ﺾ', 'ﺿ', 'ﻀ'),
            ['ط'] = ('ﻁ', 'ﻂ', 'ﻃ', 'ﻄ'),
            ['ظ'] = ('ﻅ', 'ﻆ', 'ﻇ', 'ﻈ'),
            ['ع'] = ('ﻉ', 'ﻊ', 'ﻋ', 'ﻌ'),
            ['غ'] = ('ﻍ', 'ﻎ', 'ﻏ', 'ﻐ'),
            ['ف'] = ('ﻑ', 'ﻒ', 'ﻓ', 'ﻔ'),
            ['ق'] = ('ﻕ', 'ﻖ', 'ﻗ', 'ﻘ'),
            ['ك'] = ('ﻙ', 'ﻚ', 'ﻛ', 'ﻜ'),
            ['ل'] = ('ﻝ', 'ﻞ', 'ﻟ', 'ﻠ'),
            ['م'] = ('ﻡ', 'ﻢ', 'ﻣ', 'ﻤ'),
            ['ن'] = ('ﻥ', 'ﻦ', 'ﻧ', 'ﻨ'),
            ['ه'] = ('ﻩ', 'ﻪ', 'ﻫ', 'ﻬ'),
            ['و'] = ('ﻭ', 'ﻮ', '\0', '\0'),
            ['ى'] = ('ﻯ', 'ﻰ', '\0', '\0'),
            ['ي'] = ('ﻱ', 'ﻲ', 'ﻳ', 'ﻴ'),
        };
        // 라암 + 알리프 종류 → 합자 (단독, 끝)
        static readonly Dictionary<char, (char iso, char fin)> lamAlef = new()
        {
            ['آ'] = ('ﻵ', 'ﻶ'), ['أ'] = ('ﻷ', 'ﻸ'),
            ['إ'] = ('ﻹ', 'ﻺ'), ['ا'] = ('ﻻ', 'ﻼ'),
        };

        // 모음 부호 (잇기에서는 없는 것처럼 건너뜀)
        static bool Mark(char c) => c >= 'ً' && c <= 'ٟ' || c == 'ٰ';
        static bool Arabic(char c) => c >= '؀' && c <= 'ۿ' || c >= 'ﭐ' && c <= '﷿' || c >= 'ﹰ' && c <= '﻿';
        // 앞 글자와 이어질 수 있음 (이 글자의 끝·가운데 모양이 있음)
        static bool JoinsPrev(char c) => c == 'ـ' || forms.TryGetValue(c, out var f) && f.fin != '\0';
        // 뒤 글자와 이어질 수 있음 (처음·가운데 모양이 있음)
        static bool JoinsNext(char c) => c == 'ـ' || forms.TryGetValue(c, out var f) && f.ini != '\0';

        public string PreprocessText(string text) => Fix(text);

        public static bool Has(string s)
        {
            if (s != null) foreach (var c in s) if (Arabic(c)) return true;
            return false;
        }

        public static string Fix(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (!Has(s)) return s;
            return ReverseLtr(Shape(s));
        }

        // 태그 안 글자는 건드리지 않음
        static bool[] TagMask(string s)
        {
            var m = new bool[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '<') continue;
                int j = s.IndexOf('>', i + 1);
                if (j < 0) break;
                for (int k = i; k <= j; k++) m[k] = true;
                i = j;
            }
            return m;
        }

        static string Shape(string s)
        {
            var tag = TagMask(s);
            // 잇기 판단용: 태그·모음 부호를 건너뛴 앞뒤 글자
            char Prev(int i) { for (int k = i - 1; k >= 0; k--) { if (tag[k] || Mark(s[k])) continue; return s[k]; } return '\0'; }
            char Next(int i) { for (int k = i + 1; k < s.Length; k++) { if (tag[k] || Mark(s[k])) continue; return s[k]; } return '\0'; }
            int NextIndex(int i) { for (int k = i + 1; k < s.Length; k++) { if (tag[k] || Mark(s[k])) continue; return k; } return -1; }

            var sb = new StringBuilder(s.Length);
            var skip = new bool[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                if (skip[i]) continue;
                char c = s[i];
                if (tag[i] || !forms.TryGetValue(c, out var f)) { sb.Append(c); continue; }
                bool prev = JoinsNext(Prev(i));
                // 라암 + 알리프 → 합자 하나 (알리프 자리는 지움)
                if (c == 'ل')
                {
                    int ni = NextIndex(i);
                    if (ni >= 0 && lamAlef.TryGetValue(s[ni], out var la))
                    {
                        sb.Append(prev ? la.fin : la.iso);
                        for (int k = i + 1; k < ni; k++) if (!tag[k]) sb.Append(s[k]); // 사이의 모음 부호
                        skip[ni] = true;
                        continue;
                    }
                }
                bool next = f.ini != '\0' && JoinsPrev(Next(i));
                prev &= f.fin != '\0';
                sb.Append(prev && next ? f.med : prev ? f.fin : next ? f.ini : f.iso);
            }
            return sb.ToString();
        }

        // 왼쪽→오른쪽 글자 (숫자 · 라틴 등): 오른쪽→왼쪽 배치에서 뒤집혀 놓이므로 덩어리째 미리 뒤집음.
        // 덩어리 = 왼→오 글자로 시작·끝나고, 사이에 공백·숫자 기호(. , : % + - / ×)가 낀 구간. 태그는 덩어리를 끊지 않고 제자리.
        static bool Ltr(char c) => char.IsLetterOrDigit(c) && !Arabic(c);
        static bool Joiner(char c) => c == ' ' || c == '.' || c == ',' || c == ':' || c == '%' || c == '+' || c == '-' || c == '/' || c == '×' || c == '·';

        static string ReverseLtr(string s)
        {
            var tag = TagMask(s);
            var a = s.ToCharArray();
            int i = 0;
            while (i < a.Length)
            {
                if (tag[i]) { i++; continue; }
                // 괄호는 오른쪽→왼쪽에서 거울 모양으로
                if (!Ltr(a[i])) { a[i] = Mirror(a[i]); i++; continue; }
                // 덩어리 끝 찾기
                int end = i, k = i + 1;
                while (k < a.Length)
                {
                    if (tag[k]) { k++; continue; }
                    if (Ltr(a[k])) { end = k; k++; continue; }
                    if (Joiner(a[k])) { k++; continue; }
                    break;
                }
                // 숫자 앞의 + - 와 뒤의 % 도 덩어리에 (예: +15% · -3)
                int start = i;
                if (start > 0 && !tag[start - 1] && (a[start - 1] == '+' || a[start - 1] == '-')) start--;
                if (end + 1 < a.Length && !tag[end + 1] && a[end + 1] == '%') end++;
                ReverseRun(a, tag, start, end);
                i = end + 1;
            }
            return new string(a);
        }

        static char Mirror(char c) => c switch
        {
            '(' => ')', ')' => '(', '[' => ']', ']' => '[', '«' => '»', '»' => '«', '<' => '>', '>' => '<', _ => c,
        };

        // [from, to] 안의 태그 아닌 글자만 순서를 뒤집음 (태그는 제자리)
        static void ReverseRun(char[] a, bool[] tag, int from, int to)
        {
            var idx = new List<int>();
            for (int k = from; k <= to; k++) if (!tag[k]) idx.Add(k);
            for (int x = 0, y = idx.Count - 1; x < y; x++, y--) (a[idx[x]], a[idx[y]]) = (a[idx[y]], a[idx[x]]);
        }
    }
}
