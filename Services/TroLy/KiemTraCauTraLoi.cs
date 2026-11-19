using System.Text.RegularExpressions;

namespace SuDia.Web.Services.TroLy;

/// <summary>
/// Kiểm tra bằng code (Phần 4): mọi con số trong câu trả lời phải có trong các đoạn được trích,
/// và mọi số nguồn [n] phải nằm trong các đoạn đã gửi cho model.
/// </summary>
public static class KiemTraCauTraLoi
{
    private static readonly Regex SoNguon = new(@"\[(\d+)\]");
    private static readonly Regex ConSo = new(@"\d+(?:[.,]\d+)*");
    // "Nguồn:" có thể được model in đậm / làm tiêu đề ("**Nguồn:**", "### Nguồn")
    private static readonly Regex MucNguon = new(@"\n\s*[*#_>]*\s*Nguồn\s*[*_]*\s*:?[*_]*\s*\n.*$|\n\s*[*#_]*\s*Nguồn\s*[*_]*\s*:.*$", RegexOptions.Singleline);
    // Dòng "Mẹo nhớ" là phần thêm, không bắt buộc: nếu chỉ dòng này sai số liệu thì bỏ dòng, giữ câu trả lời
    private static readonly Regex DongMeoNho = new(@"^\s*[*_#>-]*\s*Mẹo nhớ.*$\n?", RegexOptions.Multiline);

    // Tài liệu viết "ba đợt", câu trả lời viết "3 đợt" vẫn coi là đúng
    private static readonly Dictionary<string, string> SoChu = new()
    {
        ["một"] = "1", ["hai"] = "2", ["ba"] = "3", ["bốn"] = "4",
        // "năm" bỏ qua vì trùng với "năm 1954"
        ["sáu"] = "6", ["bảy"] = "7", ["tám"] = "8", ["chín"] = "9", ["mười"] = "10",
    };

    public record KetQua(bool Dat, string ThanBai, IReadOnlyList<int> NguonDaDung, IReadOnlyList<string> Loi);

    /// <param name="traLoi">Câu trả lời đầy đủ của model</param>
    /// <param name="doan">Các đoạn đã gửi trong &lt;tai_lieu&gt;, theo đúng thứ tự [1], [2]…</param>
    public static KetQua KiemTra(string traLoi, IReadOnlyList<DoanTaiLieu> doan)
    {
        var kq = KiemTraMotLan(traLoi, doan);
        if (kq.Dat || !DongMeoNho.IsMatch(kq.ThanBai)) return kq;
        var boMeo = KiemTraMotLan(DongMeoNho.Replace(traLoi, "").TrimEnd(), doan);
        return boMeo.Dat ? boMeo : kq;
    }

    private static KetQua KiemTraMotLan(string traLoi, IReadOnlyList<DoanTaiLieu> doan)
    {
        // Mục "Nguồn:" do code tự dựng lại từ đoạn được trích, không dùng bản của model
        var thanBai = MucNguon.Replace(traLoi, "").Trim();
        var loi = new List<string>();

        var nguon = SoNguon.Matches(thanBai).Select(m => int.Parse(m.Groups[1].Value)).Distinct().Order().ToList();
        foreach (var n in nguon.Where(n => n < 1 || n > doan.Count))
            loi.Add($"Trích nguồn [{n}] không có trong tài liệu.");

        var hopLe = nguon.Where(n => n >= 1 && n <= doan.Count).ToList();
        // Câu từ chối không trích [n]: số trong lời gợi ý phải có trong một đoạn bất kỳ đã gửi
        var doanDoiChieu = hopLe.Count > 0 ? hopLe.Select(n => doan[n - 1]) : doan;
        loi.AddRange(SoKhongCoTrong(thanBai, doanDoiChieu).Select(so => $"Số \"{so}\" không có trong đoạn được trích."));

        return new KetQua(loi.Count == 0, thanBai, hopLe, loi);
    }

    // "A. 1945", "B) 1946", "- C. …": phương án trắc nghiệm, phương án nhiễu được phép chứa số không có trong tài liệu
    private static readonly Regex DongPhuongAn = new(@"^\s*[-*]?\s*\**[A-D][.)]\**\s.*$", RegexOptions.Multiline);

    // Số chỉ để đánh thứ tự, không phải số liệu: "Câu 1:", "Ý 2", "Bước 3", "1." / "2)" đầu dòng, "I." đầu dòng
    // và khoá đáp án trắc nghiệm "1-B, 2-C" / "1. B"
    private static readonly Regex SoThuTu = new(@"\b(Câu|Ý|Bước|Phần|Cách|Mẹo|Nhóm|Giai đoạn)\s+\d+\b|^\s*[-*#>]*\s*\**\d{1,2}[.)]\**(?=\s)|\b\d{1,2}\s*[-–:.)]\s*[A-D]\b",
        RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static IEnumerable<string> SoKhongCoTrong(string text, IEnumerable<DoanTaiLieu> doan)
    {
        text = DongPhuongAn.Replace(text, "");
        text = SoThuTu.Replace(text, "");
        var soCoTrongNguon = TapConSo(string.Join("\n", doan.Select(d => d.NoiDung)));
        return ConSo.Matches(SoNguon.Replace(text, "")).Select(m => m.Value).Distinct()
            .Where(so => !soCoTrongNguon.Contains(so));
    }

    private static HashSet<string> TapConSo(string text)
    {
        var tap = new HashSet<string>();
        foreach (Match m in ConSo.Matches(text))
        {
            tap.Add(m.Value);
            foreach (var phan in Regex.Split(m.Value, @"[.,]")) tap.Add(phan);
        }
        foreach (Match m in Regex.Matches(text.ToLowerInvariant(), @"\p{L}+"))
            if (SoChu.TryGetValue(m.Value, out var so)) tap.Add(so);
        return tap;
    }
}
