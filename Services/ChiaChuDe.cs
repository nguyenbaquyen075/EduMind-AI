using System.Text.RegularExpressions;

namespace SuDia.Web.Services;

public record BaiTachRa(int? So, string Ten, IReadOnlyList<NoiDungAiMuc> Muc);
public record ChuDeTachRa(int? So, string? Ten, IReadOnlyList<BaiTachRa> Bai);

/// <summary>
/// Tách tài liệu cấp chủ đề: "Chủ đề / Chương N" là tổng, bên trong là các "Bài N"; mỗi bài chia tiếp thành mục bằng
/// <see cref="ChiaDeMucVanBan"/>. Nhận cả tiêu đề Markdown (# Chủ đề…, ## Bài…) lẫn dòng chữ thường như file Word.
/// </summary>
public static class ChiaChuDe
{
    private static readonly Regex ChuDe = new(@"^\s*(?:#{1,6}\s*)?(?:chủ\s*đề|chương|chuyên\s*đề)(?:\s+([0-9]+|[IVXLC]+))?\s*(?:[.:\-–—]\s*|\s+)(.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Bai = new(@"^\s*(?:#{1,6}\s*)?bài\s+([0-9]{1,3})\s*(?:[.:\-–—]\s*|\s+)(\S.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Số dòng "Bài N" trong tài liệu (để biết là tài liệu một bài hay cả chủ đề).</summary>
    public static int DemBai(string noiDung) => Dong(noiDung).Count(d => d.Length <= 200 && Bai.IsMatch(d));

    /// <summary>null khi tài liệu không có dòng "Bài N" nào.</summary>
    public static ChuDeTachRa? Chia(string noiDung)
    {
        int? soChuDe = null; string? tenChuDe = null;
        var dsBai = new List<(int So, string Ten, List<string> Than)>();
        var moDau = new List<string>();
        foreach (var dong in Dong(noiDung))
        {
            var b = dong.Length <= 200 ? Bai.Match(dong) : Match.Empty;
            if (b.Success) { dsBai.Add((int.Parse(b.Groups[1].Value), b.Groups[2].Value.Trim(), [])); continue; }
            var c = dsBai.Count == 0 && tenChuDe is null && dong.Length <= 200 ? ChuDe.Match(dong) : Match.Empty;
            if (c.Success)
            {
                soChuDe = int.TryParse(c.Groups[1].Value, out var n) ? n : null;
                tenChuDe = c.Groups[2].Value.Trim().TrimEnd('.', ':');
                moDau.Clear(); // phần trước dòng chủ đề (tiêu đề file, khai báo) không thuộc bài nào
                continue;
            }
            (dsBai.Count == 0 ? moDau : dsBai[^1].Than).Add(dong);
        }
        if (dsBai.Count == 0) return null;
        // Lời mở đầu chủ đề (sau dòng chủ đề, trước bài đầu tiên): đưa vào đầu bài đầu tiên để không mất chữ
        if (moDau.Any(d => d.Trim().Length > 0)) dsBai[0].Than.InsertRange(0, moDau);
        return new(soChuDe, string.IsNullOrWhiteSpace(tenChuDe) ? null : tenChuDe,
            dsBai.Select(b => new BaiTachRa(b.So, b.Ten, ChiaDeMucVanBan.Chia(b.Ten, string.Join("\n", b.Than)))).ToList());
    }

    private static string[] Dong(string noiDung) => noiDung.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
}
