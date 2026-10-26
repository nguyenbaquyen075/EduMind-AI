using System.Text;

namespace SuDia.Web.Services.TroLy;

/// <summary>
/// Đọc prompt trực tiếp từ Prompts/prompt-he-thong-tro-ly.md để file .md là nguồn duy nhất:
/// sửa prompt ở đó, khởi động lại web là có hiệu lực.
/// </summary>
public class PromptTroLy
{
    public string HeThong { get; }   // Phần 1
    public string AiCham { get; }    // Phần 4

    public const string CauTuChoi = "Nội dung này chưa có trong tài liệu khoá học.";

    public PromptTroLy(string duongDanFile)
    {
        var md = File.ReadAllText(duongDanFile);
        HeThong = LayKhoiText(md, "## Phần 1");
        AiCham = LayKhoiText(md, "## Phần 4");
    }

    // Khối ```text đầu tiên sau tiêu đề
    private static string LayKhoiText(string md, string tieuDe)
    {
        var i = md.IndexOf(tieuDe, StringComparison.Ordinal);
        var a = i < 0 ? -1 : md.IndexOf("```text", i, StringComparison.Ordinal);
        var b = a < 0 ? -1 : md.IndexOf("```", a + 7, StringComparison.Ordinal);
        if (b < 0) throw new InvalidOperationException($"Không tìm thấy khối ```text sau \"{tieuDe}\" trong file prompt.");
        return md[(a + 7)..b].Trim();
    }

    /// <summary>Mẫu tin nhắn Phần 2.</summary>
    public static string GhepTinNhan(IReadOnlyList<DoanTaiLieu> doan, string cauHoi)
    {
        var sb = new StringBuilder("<tai_lieu>\n");
        for (var i = 0; i < doan.Count; i++)
            sb.Append('[').Append(i + 1).Append("] (").Append(doan[i].Nguon).Append(")\n")
              .Append(doan[i].NoiDung).Append("\n\n");
        sb.Append("</tai_lieu>\n\n<cau_hoi>\n").Append(LamSach(cauHoi)).Append("\n</cau_hoi>");
        return sb.ToString();
    }

    /// <summary>Mẫu tin nhắn cho AI chấm.</summary>
    public static string GhepTinNhanCham(IReadOnlyList<DoanTaiLieu> doan, string traLoi)
    {
        var sb = new StringBuilder("<tai_lieu>\n");
        for (var i = 0; i < doan.Count; i++)
            sb.Append('[').Append(i + 1).Append("] ").Append(doan[i].NoiDung).Append("\n\n");
        sb.Append("</tai_lieu>\n\n<tra_loi>\n").Append(LamSach(traLoi)).Append("\n</tra_loi>");
        return sb.ToString();
    }

    // Không cho học sinh tự đóng thẻ <cau_hoi> để chèn lệnh
    private static string LamSach(string s) => s.Replace("<", "‹").Replace(">", "›").Trim();
}
