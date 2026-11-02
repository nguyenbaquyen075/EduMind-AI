using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Areas.Admin.Controllers;

// Doanh thu & đơn hàng: tiền theo ngày, theo gói; danh sách đơn PayOS
public class DoanhThuController(AppDbContext db, PayOSService payos, DonHangService donHang) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(DateTime? tu, DateTime? den, string? trangThai)
    {
        var cuoi = (den ?? AdminThongKeService.HomNay).Date;
        var dau = (tu ?? cuoi.AddDays(-29)).Date;
        if (dau > cuoi || (cuoi - dau).TotalDays > 365) { BaoLoi("Chọn khoảng tối đa 1 năm, ngày bắt đầu không sau ngày kết thúc."); dau = cuoi.AddDays(-29); }
        var tuUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dau, DateTimeKind.Unspecified), AdminThongKeService.MuiGio);
        var denUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(cuoi.AddDays(1), DateTimeKind.Unspecified), AdminThongKeService.MuiGio);

        var daTra = await Db.DonHang.AsNoTracking()
            .Where(d => d.TrangThai == TrangThaiDon.DaThanhToan && d.NgayThanhToan >= tuUtc && d.NgayThanhToan < denUtc)
            .Select(d => new { d.NgayThanhToan, d.SoTien, d.TenGoi }).ToListAsync();
        var theoNgay = daTra.GroupBy(d => TimeZoneInfo.ConvertTimeFromUtc(d.NgayThanhToan!.Value, AdminThongKeService.MuiGio).Date)
            .ToDictionary(n => n.Key, n => (Tien: n.Sum(d => (long)d.SoTien), Don: n.Count()));
        var ngay = Enumerable.Range(0, (cuoi - dau).Days + 1).Select(i => dau.AddDays(i))
            .Select(n => new AdminDoanhThuNgay(n, theoNgay.GetValueOrDefault(n).Tien, theoNgay.GetValueOrDefault(n).Don)).ToList();

        return View(new AdminDoanhThuViewModel
        {
            Tu = dau, Den = cuoi, TrangThai = trangThai, DaCauHinh = payos.DaCauHinh,
            DoanhThuKy = daTra.Sum(d => (long)d.SoTien), SoDonKy = daTra.Count, Ngay = ngay,
            TheoGoi = daTra.GroupBy(d => d.TenGoi).Select(g => new AdminDoanhThuGoi(g.Key, g.Sum(d => (long)d.SoTien), g.Count()))
                .OrderByDescending(g => g.SoTien).ToList(),
            TongTuTruoc = await Db.DonHang.Where(d => d.TrangThai == TrangThaiDon.DaThanhToan).SumAsync(d => (long)d.SoTien),
            DonCho = await Db.DonHang.CountAsync(d => d.TrangThai == TrangThaiDon.ChoThanhToan),
            Don = await Db.DonHang.AsNoTracking().Include(d => d.NguoiDung)
                .Where(d => trangThai == null || d.TrangThai == trangThai)
                .OrderByDescending(d => d.NgayTao).Take(100).ToListAsync(),
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KiemTraDon(int id, CancellationToken ct)
    {
        var don = await Db.DonHang.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (don is null) return NotFound();
        if (don.TrangThai != TrangThaiDon.ChoThanhToan || !payos.DaCauHinh)
        {
            BaoLoi(payos.DaCauHinh ? "Đơn không còn ở trạng thái chờ thanh toán." : "Chưa cấu hình khoá PayOS.");
            return RedirectToAction(nameof(Index));
        }
        try
        {
            var tt = await payos.LayTrangThaiAsync(don.MaThanhToan, ct);
            if (tt.Status == "PAID" && tt.AmountPaid >= don.SoTien)
            { await donHang.XacNhanDaThanhToanAsync(don.Id, null, ct); ThongBao($"Đơn {don.MaThanhToan} đã thanh toán, đã mở khoá cho học viên."); }
            else if (tt.Status is "CANCELLED" or "EXPIRED")
            { await donHang.HuyAsync(don.Id, ct); ThongBao($"Đơn {don.MaThanhToan} đã huỷ/hết hạn trên PayOS."); }
            else ThongBao($"PayOS báo đơn {don.MaThanhToan} đang ở trạng thái {tt.Status}.");
        }
        catch (InvalidOperationException ex) { BaoLoi(ex.Message); }
        return RedirectToAction(nameof(Index));
    }
}
