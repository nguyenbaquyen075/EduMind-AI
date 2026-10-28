using System.Text.RegularExpressions;

namespace SuDia.Web.Services;

/// <summary>Bộ tách ngoại tuyến theo ký hiệu đề mục. Không suy luận ngữ nghĩa như AI.</summary>
public static class ChiaDeMucVanBan
{
    private static readonly Regex Markdown = new(@"^(#{1,6})\s+\S", RegexOptions.Compiled);
    private static readonly Regex Roman = new(@"^[IVXLCDM]+[.)\-:]\s+\S", RegexOptions.Compiled);
    private static readonly Regex Number = new(@"^(\d+(?:\.\d+)*)(?:[.):]|\s*[-–])\s+\S", RegexOptions.Compiled);
    private static readonly Regex Decimal = new(@"^(\d+(?:\.\d+)+)\s+\S", RegexOptions.Compiled);
    private static readonly Regex Letter = new(@"^[a-zA-ZđĐ][.)]\s+\S", RegexOptions.Compiled);

    private static int? Cap(string line)
    {
        var text = line.Trim();
        if (text.Length is 0 or > 180) return null;
        var md = Markdown.Match(text);
        if (md.Success) return md.Groups[1].Length;
        if (Roman.IsMatch(text)) return 10;
        var number = Number.Match(text);
        if (!number.Success) number = Decimal.Match(text);
        if (number.Success) return 20 + number.Groups[1].Value.Count(c => c == '.');
        if (Letter.IsMatch(text)) return 30;
        return null;
    }

    public static IReadOnlyList<NoiDungAiMuc> Chia(string tenBai, string noiDung)
    {
        var lines = noiDung.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var levels = lines.Select(Cap).Where(x => x.HasValue).Select(x => x!.Value).Distinct().Order().ToArray();
        if (levels.Length == 0)
            return [new(tenBai, tenBai, [new("Nội dung tài liệu", string.Join("\n", lines).Trim())])];

        var result = new List<NoiDungAiMuc>();
        var parts = new List<NoiDungAiPhan>();
        var body = new List<string>();
        var muc = tenBai;
        var phan = "Nội dung";
        var coMuc = false;
        var coPhan = false;
        void LuuPhan()
        {
            var text = string.Join("\n", body).Trim();
            if (text.Length > 0 || coPhan) parts.Add(new(phan, text));
            body.Clear(); coPhan = false;
        }
        void LuuMuc()
        {
            LuuPhan();
            if (parts.Count > 0 || coMuc)
                result.Add(new(muc, muc, parts.Count == 0 ? [new("Nội dung", "")] : parts.ToList()));
            parts.Clear();
        }
        foreach (var line in lines)
        {
            var level = Cap(line);
            if (level == levels[0])
            {
                LuuMuc(); muc = line.Trim().TrimStart('#', ' '); phan = "Nội dung"; coMuc = true;
            }
            else if (levels.Length > 1 && level == levels[1])
            {
                LuuPhan(); phan = line.Trim().TrimStart('#', ' '); coPhan = true;
            }
            else body.Add(line); // Giữ nguyên đề mục sâu hơn, gạch đầu dòng và các đoạn văn.
        }
        LuuMuc();
        return result;
    }
}
