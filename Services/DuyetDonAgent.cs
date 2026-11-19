using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Services;

public sealed class DuyetDonAgent(AppDbContext db)
{
    public async Task<(string QuyetDinh, bool CanXemXet, string LyDo, string DuLieu)> DanhGiaAsync(DonHang don, int soTienNhan, CancellationToken ct)
    {
        var tu24h = DateTime.UtcNow.AddHours(-24);
        var tu1h = DateTime.UtcNow.AddHours(-1);
        var nhieuTaiKhoan = !string.IsNullOrEmpty(don.SoDienThoai) && await db.DonHang
            .Where(d => d.SoDienThoai == don.SoDienThoai && d.NgayTao >= tu24h).Select(d => d.NguoiDungId).Distinct().CountAsync(ct) >= 3;
        var nhieuDon = await db.DonHang.CountAsync(d => d.NguoiDungId == don.NguoiDungId && d.TrangThai == TrangThaiDon.DaThanhToan && d.NgayThanhToan >= tu1h, ct) >= 3;
        var canXemXet = nhieuTaiKhoan || nhieuDon;
        var lyDo = canXemXet ? "Đã duyệt theo PayOS; cần xem xét tần suất thanh toán." : "PayOS đã xác nhận đủ số tiền.";
        return (canXemXet ? QuyetDinhDuyet.CanXemXet : QuyetDinhDuyet.TuDongDuyet,
            canXemXet, lyDo, JsonSerializer.Serialize(new { soTienNhan, don.SoTien, nhieuTaiKhoan, nhieuDon }));
    }
}
