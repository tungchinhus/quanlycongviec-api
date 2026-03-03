using System.Text;

namespace quanlyfilesBE.Helpers;

/// <summary>
/// Logic so khớp từ khóa tìm file giống Python service (main.py: _normalize_vi, _keyword_matches_name, _name_matches_keywords).
/// </summary>
public static class FileSearchKeywordHelper
{
    private static readonly Dictionary<char, char> ViAccentToAscii = BuildViAccentMap();

    private static Dictionary<char, char> BuildViAccentMap()
    {
        var map = new Dictionary<char, char>();
        void Add(string chars, char replacement)
        {
            foreach (var c in chars)
                map[c] = replacement;
        }
        Add("àáạảãâầấậẩẫăằắặẳẵÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴ", 'a');
        Add("èéẹẻẽêềếệểễÈÉẸẺẼÊỀẾỆỂỄ", 'e');
        Add("ìíịỉĩÌÍỊỈĨ", 'i');
        Add("òóọỏõôồốộổỗơờớợởỡÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠ", 'o');
        Add("ùúụủũưừứựửữÙÚỤỦŨƯỪỨỰỬỮ", 'u');
        Add("ỳýỵỷỹỲÝỴỶỸ", 'y');
        Add("đĐ", 'd');
        return map;
    }

    /// <summary>Bỏ dấu tiếng Việt để so khớp không phụ thuộc có/không dấu (giống _normalize_vi).</summary>
    public static string NormalizeVi(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
            sb.Append(ViAccentToAscii.TryGetValue(c, out var r) ? r : c);
        return sb.ToString();
    }

    /// <summary>Nhận diện từ khóa kiểu mã: có cả số và dấu '-' (giống _is_code_like).</summary>
    public static bool IsCodeLike(string? kwNorm)
    {
        if (string.IsNullOrEmpty(kwNorm)) return false;
        return kwNorm.Any(char.IsDigit) && kwNorm.Contains('-');
    }

    /// <summary>So khớp pattern trong text với ranh giới từ (giống _match_with_boundaries).</summary>
    public static bool MatchWithBoundaries(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern)) return false;
        var comparison = StringComparison.OrdinalIgnoreCase;
        var start = 0;
        var plen = pattern.Length;
        while (true)
        {
            var idx = text.IndexOf(pattern, start, comparison);
            if (idx < 0) return false;
            var beforeOk = idx == 0 || !char.IsLetterOrDigit(text[idx - 1]);
            var afterIdx = idx + plen;
            var afterOk = afterIdx >= text.Length || !char.IsLetterOrDigit(text[afterIdx]);
            if (beforeOk && afterOk) return true;
            start = idx + 1;
        }
    }

    private static readonly IReadOnlyDictionary<string, string[]> SynonymsAbbrevs = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["bc"] = new[] { "bao cao", "báo cáo" },
        ["báo cáo"] = new[] { "bc", "bao cao" },
        ["bao cao"] = new[] { "bc", "báo cáo" },
        ["hd"] = new[] { "hop dong", "hợp đồng" },
        ["hợp đồng"] = new[] { "hd", "hop dong" },
        ["hop dong"] = new[] { "hd", "hợp đồng" },
        ["tt"] = new[] { "thong tu", "thông tư", "trinh tu" },
        ["thông tư"] = new[] { "tt", "thong tu" },
        ["thong tu"] = new[] { "tt", "thông tư" },
        ["cv"] = new[] { "cong van", "công văn" },
        ["công văn"] = new[] { "cv", "cong van" },
        ["cong van"] = new[] { "cv", "công văn" },
        ["qđ"] = new[] { "quyet dinh", "quyết định" },
        ["quyết định"] = new[] { "qđ", "quyet dinh" },
        ["quyet dinh"] = new[] { "qđ", "quyết định" },
        ["thang"] = new[] { "tháng" },
        ["tháng"] = new[] { "thang" },
        ["nam"] = new[] { "năm" },
        ["năm"] = new[] { "nam" },
    };

    /// <summary>Biến thể từ khóa: chính nó + không dấu + từ đồng nghĩa (giống _expand_keyword_variants).</summary>
    public static IEnumerable<string> ExpandKeywordVariants(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) yield break;
        var k = keyword.Trim().ToLowerInvariant();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        yield return k;
        seen.Add(k);
        var norm = NormalizeVi(k);
        if (!seen.Contains(norm)) { seen.Add(norm); yield return norm; }
        if (SynonymsAbbrevs.TryGetValue(k, out var syns) || SynonymsAbbrevs.TryGetValue(norm, out syns))
        {
            foreach (var s in syns!)
            {
                if (seen.Contains(s)) continue;
                seen.Add(s);
                yield return s;
                var sNorm = NormalizeVi(s);
                if (!seen.Contains(sNorm)) { seen.Add(sNorm); yield return sNorm; }
            }
        }
    }

    /// <summary>Một từ khóa có khớp tên file không (giống _keyword_matches_name).</summary>
    public static bool KeywordMatchesName(string? keyword, string nameLower, string nameNormalized)
    {
        if (string.IsNullOrEmpty(nameLower) && string.IsNullOrEmpty(nameNormalized)) return false;
        var kw = keyword?.Trim().ToLowerInvariant() ?? string.Empty;
        var kwNorm = NormalizeVi(kw);
        if (string.IsNullOrEmpty(kwNorm)) return true;

        var norm = string.IsNullOrEmpty(nameNormalized) ? NormalizeVi(nameLower) : nameNormalized;
        var lower = nameLower ?? string.Empty;

        if (IsCodeLike(kwNorm))
        {
            if (MatchWithBoundaries(norm, kwNorm)) return true;
        }
        else if (norm.Contains(kwNorm, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(kw) && lower.Contains(kw, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>Trả về true nếu tên file thỏa tất cả từ khóa (AND), mỗi từ khóa khớp nếu bất kỳ biến thể nào khớp (giống _name_matches_keywords).</summary>
    public static bool NameMatchesKeywords(string? name, string? nameNormalized, IList<string> keywords)
    {
        if (keywords == null || keywords.Count == 0) return true;
        var nameLower = (name ?? string.Empty).ToLowerInvariant();
        var norm = !string.IsNullOrEmpty(nameNormalized) ? nameNormalized : NormalizeVi(nameLower);

        foreach (var kw in keywords)
        {
            var variants = ExpandKeywordVariants(kw).ToList();
            if (variants.Count == 0) continue;
            var anyMatch = variants.Any(v => KeywordMatchesName(v, nameLower, norm));
            if (!anyMatch) return false;
        }
        return true;
    }
}
