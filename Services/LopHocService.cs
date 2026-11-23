using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Services;

/// <summary>Quy tắc dùng chung cho lớp học (quyền truy cập, mã tham gia).</summary>
public class LopHocService(AppDbContext db)
{
    private const string BangMa = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // bỏ I, O, 0, 1 cho khỏi nhầm

    public async Task<string> TaoMaAsync(CancellationToken ct = default)
    {
        for (var lan = 0; lan < 20; lan++)
        {
            var ma = string.Create(6, 0, (s, _) => { for (var i = 0; i < s.Length; i++) s[i] = BangMa[RandomNumberGenerator.GetInt32(BangMa.Length)]; });
            if (!await db.Lop.AnyAsync(l => l.MaThamGia == ma, ct)) return ma;
        }
        throw new InvalidOperationException("Không tạo được mã lớp duy nhất.");
    }

    public static string ChuanHoaMa(string? s) => new string((s ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    /// <summary>Quyền của người dùng với lớp: quản lý (giảng viên của khoá / admin), thành viên, hoặc không có quyền.</summary>
    public async Task<(Lop? Lop, bool QuanLy, bool ThanhVien)> QuyenAsync(int lopId, string uid, bool laAdmin, bool laGiangVien, CancellationToken ct = default)
    {
        var lop = await db.Lop.Include(l => l.Khoa).FirstOrDefaultAsync(l => l.Id == lopId, ct);
        if (lop is null) return (null, false, false);
        var quanLy = laAdmin || (laGiangVien && lop.Khoa!.GiangVienId == uid);
        var thanhVien = await db.LopHocVien.AnyAsync(m => m.LopId == lopId && m.NguoiDungId == uid, ct);
        return quanLy || thanhVien ? (lop, quanLy, thanhVien) : (null, false, false);
    }
}
