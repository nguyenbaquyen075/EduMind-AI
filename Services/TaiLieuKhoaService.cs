using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using UglyToad.PdfPig;

namespace SuDia.Web.Services;

/// <summary>Mục "KhoTaiLieu" trong appsettings.json.</summary>
public class KhoTaiLieuOptions
{
    /// <summary>Nơi cất tệp mới tải lên: "CucBo" (đĩa máy chủ, mặc định) hoặc "S3" (Cloudflare R2 / AWS S3, cấu hình ở KhoTaiLieu:S3).</summary>
    public string NoiLuu { get; set; } = NoiLuuTep.CucBo;
    /// <summary>Thư mục lưu tệp khi NoiLuu = CucBo, tương đối với thư mục gốc web (nằm ngoài wwwroot).</summary>
    public string ThuMuc { get; set; } = "App_Data/kho-tai-lieu";
}

public record KetQuaLuuTaiLieu(TaiLieuKhoa? TaiLieu, bool DaCo, string? Loi);

/// <summary>
/// Lưu tài liệu giảng viên đưa vào kho: kiểm tra tệp, bỏ trùng, cất tệp gốc, trích chữ để tìm kiếm và cho trợ lý AI đọc.
/// </summary>
public class TaiLieuKhoaService(AppDbContext db, BoChonKhoTep khoTep, ILogger<TaiLieuKhoaService> log)
{
    public const int ToiDaByte = 20 * 1024 * 1024;
    public static readonly string[] DuoiChoPhep = [".docx", ".pptx", ".pdf", ".txt", ".md"];
    public const string ChapNhan = ".docx,.pptx,.pdf,.txt,.md";

    public async Task<KetQuaLuuTaiLieu> LuuAsync(IFormFile tep, string khoaId, int? baiId, string? ten, string nguoiTaiId, CancellationToken ct = default)
    {
        var duoi = Path.GetExtension(tep.FileName).ToLowerInvariant();
        if (!DuoiChoPhep.Contains(duoi))
            return new(null, false, $"\"{tep.FileName}\": chỉ nhận Word (.docx), PowerPoint (.pptx), PDF, .txt, .md.");
        if (tep.Length == 0 || tep.Length > ToiDaByte)
            return new(null, false, $"\"{tep.FileName}\": tệp trống hoặc lớn hơn 20 MB.");

        using var bo = new MemoryStream();
        await tep.CopyToAsync(bo, ct);
        var byteTep = bo.ToArray();
        if (!DungChuKy(byteTep, duoi))
            return new(null, false, $"\"{tep.FileName}\": nội dung tệp không đúng định dạng {duoi}.");

        var maBam = Convert.ToHexString(SHA256.HashData(byteTep)).ToLowerInvariant();
        var daCo = await db.TaiLieuKhoa.FirstOrDefaultAsync(t => t.KhoaId == khoaId && t.MaBam == maBam, ct);
        if (daCo is not null)
        {
            if (baiId is not null && daCo.BaiId is null) { daCo.BaiId = baiId; await db.SaveChangesAsync(ct); }
            return new(daCo, true, null);
        }

        var tl = new TaiLieuKhoa
        {
            KhoaId = khoaId, BaiId = baiId, TenTep = Path.GetFileName(tep.FileName), LoaiTep = duoi.TrimStart('.'),
            KichThuoc = byteTep.LongLength, MaBam = maBam, NguoiTaiId = nguoiTaiId,
            Ten = CatNgan(string.IsNullOrWhiteSpace(ten) ? Path.GetFileNameWithoutExtension(tep.FileName) : ten.Trim(), 200),
        };
        try
        {
            tl.NoiDung = TrichChu(byteTep, duoi, tep.FileName).Trim();
            if (tl.NoiDung.Length < 20)
            {
                tl.TrangThai = TrangThaiTaiLieu.Loi;
                tl.LoiDoc = duoi == ".pdf" ? "PDF không có lớp chữ (có thể là ảnh scan) nên chưa tìm kiếm được." : "Tệp gần như không có chữ.";
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException or ArgumentException or DecoderFallbackException
                                       or InvalidOperationException or UglyToad.PdfPig.Core.PdfDocumentFormatException)
        {
            log.LogWarning(ex, "Kho tài liệu: không trích được chữ từ {Tep}", tep.FileName);
            tl.TrangThai = TrangThaiTaiLieu.Loi;
            tl.LoiDoc = ex is InvalidDataException ? ex.Message : "Không đọc được chữ trong tệp (tệp hỏng hoặc có mật khẩu).";
        }

        // Tệp gốc: khoa/<mã khoá>/<guid><đuôi> trên kho đang chọn (đĩa hoặc S3); không dùng tên người dùng đặt làm khoá
        var kho = khoTep.MacDinh;
        tl.NoiLuu = kho.Ten;
        tl.DuongDan = $"khoa/{TenThuMucAnToan(khoaId)}/{Guid.NewGuid():N}{duoi}";
        await kho.LuuAsync(tl.DuongDan, byteTep, KieuNoiDung(tl.LoaiTep), ct);

        db.TaiLieuKhoa.Add(tl);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await kho.XoaAsync(tl.DuongDan, CancellationToken.None);   // lưu CSDL hỏng thì không để tệp mồ côi trong kho
            throw;
        }
        return new(tl, false, null);
    }

    /// <summary>Trả tệp cho người đã được kiểm quyền: kho S3 thì chuyển sang link ký tạm 5 phút, đĩa thì web gửi thẳng.</summary>
    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> TraTepAsync(TaiLieuKhoa tl, CancellationToken ct = default)
    {
        var kho = khoTep.Theo(tl.NoiLuu);
        var kieu = KieuNoiDung(tl.LoaiTep);
        var moTrucTiep = tl.LoaiTep == "pdf";   // PDF mở ngay trong trình duyệt; Word/PowerPoint tải về với tên gốc
        if (kho.LinkTaiTam(tl.DuongDan, tl.TenTep, kieu, moTrucTiep, TimeSpan.FromMinutes(5)) is { } link)
            return new Microsoft.AspNetCore.Mvc.RedirectResult(link);
        var luong = await kho.MoAsync(tl.DuongDan, ct);
        if (luong is null) return new Microsoft.AspNetCore.Mvc.NotFoundResult();
        return new Microsoft.AspNetCore.Mvc.FileStreamResult(luong, kieu) { FileDownloadName = moTrucTiep ? null : tl.TenTep };
    }

    public async Task XoaAsync(TaiLieuKhoa tl, CancellationToken ct = default)
    {
        db.TaiLieuKhoa.Remove(tl);
        await db.SaveChangesAsync(ct);
        await khoTep.Theo(tl.NoiLuu).XoaAsync(tl.DuongDan, CancellationToken.None);
    }

    /// <summary>Chuyển tệp đang nằm trên đĩa sang kho S3 (lệnh --chuyen-kho-tep). Trả về số tệp đã chuyển.</summary>
    public async Task<int> ChuyenLenS3Async(Action<string> ghi, CancellationToken ct = default)
    {
        if (!khoTep.CoS3) throw new InvalidOperationException("Chưa cấu hình KhoTaiLieu:S3 nên chưa chuyển được.");
        var cucBo = khoTep.Theo(NoiLuuTep.CucBo);
        var s3 = khoTep.Theo(NoiLuuTep.S3);
        var dem = 0;
        foreach (var tl in await db.TaiLieuKhoa.Where(t => t.NoiLuu == NoiLuuTep.CucBo).OrderBy(t => t.Id).ToListAsync(ct))
        {
            await using var luong = await cucBo.MoAsync(tl.DuongDan, ct);
            if (luong is null) { ghi($"Bỏ qua #{tl.Id} {tl.TenTep}: không thấy tệp trên đĩa."); continue; }
            using var ms = new MemoryStream();
            await luong.CopyToAsync(ms, ct);
            var byteTep = ms.ToArray();
            if (Convert.ToHexString(SHA256.HashData(byteTep)).ToLowerInvariant() != tl.MaBam) { ghi($"Bỏ qua #{tl.Id} {tl.TenTep}: tệp trên đĩa khác mã băm."); continue; }
            await s3.LuuAsync(tl.DuongDan, byteTep, KieuNoiDung(tl.LoaiTep), ct);
            tl.NoiLuu = NoiLuuTep.S3;
            await db.SaveChangesAsync(ct);
            await luong.DisposeAsync();
            await cucBo.XoaAsync(tl.DuongDan, ct);
            dem++;
            ghi($"Đã chuyển #{tl.Id} {tl.TenTep}");
        }
        return dem;
    }

    public static string KieuNoiDung(string loai) => loai switch
    {
        "pdf" => "application/pdf",
        "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "md" => "text/markdown; charset=utf-8",
        _ => "text/plain; charset=utf-8",
    };

    // ---------------------------------------------------------------- Trích chữ

    private static string TrichChu(byte[] byteTep, string duoi, string tenTep)
    {
        switch (duoi)
        {
            case ".docx" or ".pptx":
                using (var s = new MemoryStream(byteTep)) return DocTaiLieuOffice.Doc(s, duoi, tenTep).NoiDung;
            case ".pdf":
            {
                // Mỗi trang một tiêu đề "# Trang n" để trích nguồn chỉ đúng trang
                using var pdf = PdfDocument.Open(byteTep);
                var sb = new StringBuilder();
                foreach (var trang in pdf.GetPages())
                {
                    var chu = Regex.Replace(string.Join(' ', trang.GetWords().Select(w => w.Text)), @"\s+", " ").Trim();
                    if (chu.Length > 0) sb.Append("# Trang ").Append(trang.Number).Append("\n\n").Append(chu).Append("\n\n");
                }
                return sb.ToString();
            }
            default:
                return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(byteTep).TrimStart('﻿');
        }
    }

    private static bool DungChuKy(byte[] b, string duoi) => duoi switch
    {
        ".docx" or ".pptx" => b.Length > 4 && b[0] == 0x50 && b[1] == 0x4B && b[2] == 0x03 && b[3] == 0x04,
        ".pdf" => b.Length > 5 && Encoding.ASCII.GetString(b, 0, 5) == "%PDF-",
        _ => !b.Take(8000).Contains((byte)0),   // văn bản thuần không có byte 0
    };

    private static string TenThuMucAnToan(string khoaId) => Regex.Replace(khoaId, "[^a-zA-Z0-9-]", "_");

    private static string CatNgan(string s, int n) => s.Length <= n ? s : s[..n];

    /// <summary>Chia chữ của tài liệu thành đoạn theo tiêu đề (# …) để tìm kiếm và trích nguồn.</summary>
    public static IEnumerable<(string TieuDe, string NoiDung)> ChiaDoan(string noiDung)
    {
        string? tieuDe = null;
        var bo = new List<string>();
        IEnumerable<(string, string)> Chot()
        {
            var text = string.Join('\n', bo).Trim();
            bo.Clear();
            if (text.Length == 0) yield break;
            foreach (var phan in SuDia.Web.Services.TroLy.KhoTaiLieu.ChiaNho(text)) yield return (tieuDe ?? "", phan);
        }
        foreach (var dong in noiDung.Replace("\r", "").Split('\n'))
        {
            var m = Regex.Match(dong, @"^#{1,6}\s+(.+)$");
            if (m.Success)
            {
                foreach (var x in Chot()) yield return x;
                tieuDe = m.Groups[1].Value.Trim();
            }
            else bo.Add(dong);
        }
        foreach (var x in Chot()) yield return x;
    }
}
