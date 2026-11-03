using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Services;

/// <summary>Tạo đơn mua gói và mở khoá học khi đơn đã thanh toán.</summary>
public sealed class DonHangService(AppDbContext db, ILogger<DonHangService> log)
{
    /// <summary>orderCode PayOS phải là số nguyên duy nhất mãi mãi với cửa hàng, kể cả khi database làm lại từ đầu.</summary>
    public static long TaoMaThanhToan() =>
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 100 + RandomNumberGenerator.GetInt32(100);

    public async Task<DonHang> TaoDonAsync(string nguoiDungId, GoiBan goi, CancellationToken ct = default)
    {
        var don = new DonHang
        {
            MaThanhToan = TaoMaThanhToan(), NguoiDungId = nguoiDungId, GoiBanId = goi.Id, TenGoi = goi.Ten,
            SoTien = goi.Gia!.Value, SoThang = goi.SoThang, KhoaIds = goi.KhoaIds.ToList(),
        };
        db.DonHang.Add(don);
        await db.SaveChangesAsync(ct);
        return don;
    }

    /// <summary>
    /// Chỉ một lời gọi chuyển được đơn sang Đã thanh toán (UPDATE có điều kiện), nên webhook và trang
    /// trả về đến cùng lúc không cộng hạn học hai lần. Trả về true nếu lần gọi này vừa xác nhận đơn.
    /// </summary>
    public async Task<bool> XacNhanDaThanhToanAsync(int donId, string? maGiaoDich, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var luc = DateTime.UtcNow;
        var doi = await db.DonHang.Where(d => d.Id == donId && d.TrangThai != TrangThaiDon.DaThanhToan)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.TrangThai, TrangThaiDon.DaThanhToan)
                .SetProperty(d => d.NgayThanhToan, luc).SetProperty(d => d.MaGiaoDich, maGiaoDich), ct);
        if (doi == 0) return false;

        var don = await db.DonHang.AsNoTracking().FirstAsync(d => d.Id == donId, ct);
        var daCo = await db.GhiDanh.Where(g => g.NguoiDungId == don.NguoiDungId && don.KhoaIds.Contains(g.KhoaId)).ToListAsync(ct);
        foreach (var khoaId in don.KhoaIds.Distinct())
        {
            var g = daCo.FirstOrDefault(x => x.KhoaId == khoaId);
            if (g is null)
                db.GhiDanh.Add(new GhiDanh { NguoiDungId = don.NguoiDungId, KhoaId = khoaId, HanDen = luc.AddMonths(don.SoThang) });
            else if (g.HanDen is DateTime han) // còn hạn thì cộng nối tiếp, hết hạn thì tính từ hôm nay; null = không giới hạn giữ nguyên
                g.HanDen = (han > luc ? han : luc).AddMonths(don.SoThang);
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        log.LogInformation("Đơn {DonId} ({MaThanhToan}) đã thanh toán, mở {SoKhoa} khoá cho {NguoiDung}",
            don.Id, don.MaThanhToan, don.KhoaIds.Count, don.NguoiDungId);
        return true;
    }

    public Task HuyAsync(int donId, CancellationToken ct = default) =>
        db.DonHang.Where(d => d.Id == donId && d.TrangThai == TrangThaiDon.ChoThanhToan)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.TrangThai, TrangThaiDon.DaHuy), ct);
}
