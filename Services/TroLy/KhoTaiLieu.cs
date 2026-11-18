using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SuDia.Web.Data;

namespace SuDia.Web.Services.TroLy;

/// <param name="Khoa">Mã khoá, ví dụ "lich-su-12"; rỗng nếu không thuộc khoá nào (gói học)</param>
/// <param name="Mon">"Lịch sử 12"</param>
/// <param name="Nguon">"Lịch sử 12 · Bài 7 · Mục 3 — Chiến dịch Điện Biên Phủ"</param>
/// <param name="Loai">Bài học | Tài liệu | Thông tin khoá | Gói học</param>
/// <param name="Lien">Đường dẫn mở nguồn (bài học tới đúng mục, tệp tài liệu); null nếu không có trang riêng</param>
public record DoanTaiLieu(string Khoa, string Mon, string Nguon, string NoiDung, string Loai = LoaiDoan.BaiHoc, string? Lien = null);

public static class LoaiDoan
{
    public const string BaiHoc = "Bài học";
    public const string TaiLieu = "Tài liệu";
    public const string ThongTinKhoa = "Thông tin khoá";
    public const string GoiHoc = "Gói học";
}

/// <summary>
/// Tìm đoạn tài liệu trong chỉ mục PostgreSQL (bảng DoanChiMuc, cột tsvector chữ đã bỏ dấu, chỉ mục GIN).
/// Dùng chung cho Học viên › Tài liệu và trợ lý AI. Chỉ mục do DongBoChiMuc cập nhật khi nội dung đổi.
/// Câu hỏi được tách thành âm tiết (bỏ từ dừng) ghép bằng OR; cặp âm tiết liền nhau ("dien bien", "bien phu")
/// được cộng điểm thêm vì tiếng Việt là ngôn ngữ đơn âm tiết.
/// </summary>
public class KhoTaiLieu(IServiceScopeFactory phamVi, string thuMucMd)
{
    private const int DoDaiToiDa = 1500;   // ký tự mỗi đoạn

    internal static readonly HashSet<string> TuDung =
    [
        "la", "cua", "va", "cac", "nhung", "mot", "nao", "gi", "khi", "the", "thi", "sao", "bao", "nhieu",
        "co", "khong", "duoc", "trong", "voi", "cho", "ve", "tu", "den", "o", "ai", "hay", "hoac", "nay",
        "do", "da", "dang", "se", "bi", "boi", "ra", "vao", "len", "nhu", "ma", "con", "cung", "rat",
        "nhe", "a", "oi", "vi", "de", "theo", "minh", "em", "ban", "biet", "hoi",
    ];

    /// <summary>Thư mục tệp .md dự phòng (dùng cho khoá chưa có nội dung trong hệ thống).</summary>
    public string ThuMucMd => thuMucMd;

    /// <summary>Số liệu cho trang quản trị.</summary>
    public record ThongKeChiMuc(int SoDoan, int SoDoanCoSoDuLieu, int SoKhoaCoSoDuLieu, int SoDoanTepMd, DateTime NapLuc, int SoDoanTaiLieuTaiLen = 0);

    public async Task<ThongKeChiMuc> ThongKeAsync(CancellationToken ct = default)
    {
        using var scope = phamVi.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var nhom = await db.DoanChiMuc.AsNoTracking().GroupBy(d => 1).Select(g => new
        {
            Tong = g.Count(),
            Md = g.Count(d => d.NguonKhoa.StartsWith("Md:")),
            TaiLieu = g.Count(d => d.Loai == LoaiDoan.TaiLieu),
            Luc = g.Max(d => (DateTime?)d.NapLuc),
        }).FirstOrDefaultAsync(ct);
        var soKhoa = await db.DoanChiMuc.Where(d => d.KhoaId != null && !d.NguonKhoa.StartsWith("Md:")).Select(d => d.KhoaId).Distinct().CountAsync(ct);
        return nhom is null ? new(0, 0, 0, 0, DateTime.MinValue)
            : new(nhom.Tong, nhom.Tong - nhom.Md, soKhoa, nhom.Md, (nhom.Luc ?? DateTime.MinValue).ToLocalTime(), nhom.TaiLieu);
    }

    /// <summary>Trả về tối đa <paramref name="soDoan"/> đoạn liên quan nhất, điểm giảm dần.</summary>
    public Task<IReadOnlyList<DoanTaiLieu>> TimKiemAsync(string cauHoi, string? khoa, int soDoan, CancellationToken ct = default) =>
        TimKiemAsync(cauHoi, khoa is null ? null : [khoa], soDoan, null, ct);

    /// <summary>Tìm trong các khoá cho trước (null = mọi khoá). Đoạn không thuộc khoá nào (gói học) luôn được xét.</summary>
    /// <param name="loai">Chỉ lấy các loại đoạn này (null = mọi loại)</param>
    public async Task<IReadOnlyList<DoanTaiLieu>> TimKiemAsync(string cauHoi, IReadOnlyCollection<string>? dsKhoa, int soDoan,
        IReadOnlyCollection<string>? loai, CancellationToken ct = default)
    {
        var amTiet = Regex.Split(BoDau(cauHoi), "[^a-z0-9]+").Where(t => t.Length > 0).Take(60).ToList();
        var tu = amTiet.Where(t => !TuDung.Contains(t)).Distinct().ToList();
        if (tu.Count == 0) return [];
        var cap = amTiet.Zip(amTiet.Skip(1)).Where(p => !TuDung.Contains(p.First) || !TuDung.Contains(p.Second))
            .Select(p => $"{p.First} <-> {p.Second}").Distinct().ToList();

        var sql = new StringBuilder("""
            SELECT d.* FROM "DoanChiMuc" d,
                   to_tsquery('simple', @q) q
            """);
        if (cap.Count > 0) sql.Append(", to_tsquery('simple', @cap) c");
        sql.Append("""
             WHERE d."TimKiem" @@ q
               AND (@khoa::text[] IS NULL OR d."KhoaId" IS NULL OR d."KhoaId" = ANY(@khoa::text[]))
               AND (@loai::text[] IS NULL OR d."Loai" = ANY(@loai::text[]))
             ORDER BY ts_rank_cd(d."TimKiem", q, 1)
            """);
        if (cap.Count > 0) sql.Append(" + 2 * ts_rank_cd(d.\"TimKiem\", c, 1)");
        sql.Append(" DESC, d.\"Id\" LIMIT @n");

        var thamSo = new List<NpgsqlParameter>
        {
            new("q", string.Join(" | ", tu)),
            new("khoa", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text) { Value = dsKhoa is null ? DBNull.Value : dsKhoa.ToArray() },
            new("loai", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text) { Value = loai is null ? DBNull.Value : loai.ToArray() },
            new("n", soDoan),
        };
        if (cap.Count > 0) thamSo.Add(new("cap", string.Join(" | ", cap)));

        using var scope = phamVi.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dong = await db.DoanChiMuc.FromSqlRaw(sql.ToString(), thamSo.ToArray<object>()).AsNoTracking().ToListAsync(ct);
        return dong.Select(d => new DoanTaiLieu(d.KhoaId ?? "", d.Mon, d.Nguon, d.NoiDung, d.Loai, d.Lien)).ToList();
    }

    /// <summary>
    /// Vài mục bài học có thật để trợ lý gợi ý học viên hỏi: liên quan tới <paramref name="lienQuan"/> (chủ đề đang học)
    /// nếu có, không thì chọn ngẫu nhiên.
    /// </summary>
    public async Task<IReadOnlyList<string>> GoiYChuDeAsync(string? khoa, int so, string? lienQuan = null, CancellationToken ct = default)
    {
        List<string> nguon = [];
        if (!string.IsNullOrWhiteSpace(lienQuan))
            nguon = (await TimKiemAsync(lienQuan, khoa is null ? null : [khoa], so + 3, [LoaiDoan.BaiHoc], ct)).Select(d => d.Nguon).Distinct().Take(so).ToList();
        if (nguon.Count == 0)
        {
            using var scope = phamVi.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            nguon = await db.DoanChiMuc.AsNoTracking()
                .Where(d => d.Loai == LoaiDoan.BaiHoc && (khoa == null || d.KhoaId == khoa))
                .Select(d => d.Nguon).Distinct().OrderBy(_ => EF.Functions.Random()).Take(so).ToListAsync(ct);
        }
        // "Lịch sử 12 · Bài 7 · Mục 2 — Các chiến dịch lớn" → "Các chiến dịch lớn (Lịch sử 12, Bài 7)"
        return nguon.Select(n =>
        {
            var p = n.Split(" · ");
            var ten = n.Contains(" — ") ? n[(n.IndexOf(" — ") + 3)..] : n;
            return p.Length >= 2 ? $"{ten} ({p[0]}, {p[1]})" : ten;
        }).ToList();
    }

    /// <summary>Trích một khúc ngắn quanh từ khoá đầu tiên khớp câu tìm (so khớp không dấu) để hiện trong kết quả.</summary>
    public static string TrichKhuc(string noiDung, string cauHoi, int doDai = 240)
    {
        var phang = Regex.Replace(noiDung, @"\s+", " ").Trim();
        if (phang.Length <= doDai) return phang;
        var khongDau = BoDau(phang);
        var viTri = -1;
        if (khongDau.Length == phang.Length)
            foreach (var tu in Regex.Split(BoDau(cauHoi), @"[^a-z0-9]+").Where(t => t.Length > 2 && !TuDung.Contains(t)))
            {
                var m = Regex.Match(khongDau, $@"\b{Regex.Escape(tu)}\b");
                if (m.Success) { viTri = m.Index; break; }
            }
        var dau = Math.Max(0, Math.Min(viTri < 0 ? 0 : viTri - doDai / 3, phang.Length - doDai));
        var khuc = phang.Substring(dau, doDai);
        return (dau > 0 ? "…" : "") + khuc.Trim() + (dau + doDai < phang.Length ? "…" : "");
    }

    /// <summary>Đọc các tệp .md dự phòng thành đoạn (khoá lấy từ phần đầu tệp "khoa:").</summary>
    public static List<DoanTaiLieu> DocTepMd(string thuMuc)
    {
        var kq = new List<DoanTaiLieu>();
        if (Directory.Exists(thuMuc))
            foreach (var file in Directory.GetFiles(thuMuc, "*.md").Order())
                NapFile(file, kq);
        return kq;
    }

    // ---------------------------------------------------------------- Nạp file

    private static void NapFile(string duongDan, List<DoanTaiLieu> ra)
    {
        var dong = File.ReadAllLines(duongDan);
        var batDau = 0;
        var thongTin = new Dictionary<string, string>();
        if (dong.Length > 0 && dong[0].Trim() == "---")
        {
            for (batDau = 1; batDau < dong.Length && dong[batDau].Trim() != "---"; batDau++)
            {
                var p = dong[batDau].Split(':', 2);
                if (p.Length == 2) thongTin[p[0].Trim()] = p[1].Trim();
            }
            batDau++;
        }

        var tenFile = Path.GetFileNameWithoutExtension(duongDan);
        var khoa = thongTin.GetValueOrDefault("khoa", tenFile);
        var mon = thongTin.GetValueOrDefault("mon", tenFile);

        string? bai = null, muc = null;
        var noiDung = new List<string>();

        void Chot()
        {
            var text = string.Join('\n', noiDung).Trim();
            noiDung.Clear();
            if (text.Length == 0) return;
            var nguon = TaoNguon(mon, bai, muc);
            foreach (var phan in ChiaNho(text))
                ra.Add(new DoanTaiLieu(khoa, mon, nguon, phan));
        }

        for (var i = batDau; i < dong.Length; i++)
        {
            var d = dong[i];
            if (d.StartsWith("### ")) { Chot(); muc = d[4..].Trim(); }
            else if (d.StartsWith("## ")) { Chot(); bai = d[3..].Trim(); muc = null; }
            else if (d.StartsWith("# ")) { Chot(); bai = null; muc = null; }
            else noiDung.Add(d);
        }
        Chot();
    }

    // "Bài 7. Kháng chiến…" + "3. Chiến dịch Điện Biên Phủ" -> "Lịch sử 12 · Bài 7 · Mục 3 — Chiến dịch Điện Biên Phủ"
    private static string TaoNguon(string mon, string? bai, string? muc)
    {
        var sb = new StringBuilder(mon);
        if (bai is not null)
        {
            var m = Regex.Match(bai, @"^(Bài\s+\d+)[.:]?\s*(.*)$");
            sb.Append(" · ").Append(m.Success ? m.Groups[1].Value : bai);
            if (muc is null && m.Success && m.Groups[2].Length > 0) sb.Append(" — ").Append(m.Groups[2].Value);
        }
        if (muc is not null)
        {
            var m = Regex.Match(muc, @"^(\d+)[.)]\s*(.+)$");
            sb.Append(" · ").Append(m.Success ? $"Mục {m.Groups[1].Value} — {m.Groups[2].Value}" : muc);
        }
        return sb.ToString();
    }

    // Mục dài thì chia theo đoạn văn, mỗi phần ≤ DoDaiToiDa ký tự
    internal static IEnumerable<string> ChiaNho(string text)
    {
        if (text.Length <= DoDaiToiDa) { yield return text; yield break; }
        var sb = new StringBuilder();
        foreach (var para in text.Split("\n\n"))
        {
            if (sb.Length > 0 && sb.Length + para.Length > DoDaiToiDa)
            {
                yield return sb.ToString().Trim();
                sb.Clear();
            }
            sb.Append(para).Append("\n\n");
        }
        if (sb.Length > 0) yield return sb.ToString().Trim();
    }

    // ---------------------------------------------------------------- Tách từ

    public static string BoDau(string s)
    {
        var tach = s.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(tach.Length);
        foreach (var c in tach)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString();
    }
}
