using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Services;

namespace SuDia.Web.Controllers;

// Mua gói học: xác nhận → PayOS → trang kết quả hỏi lại PayOS rồi mở khoá
[Authorize]
public class ThanhToanController(AppDbContext db, PayOSService payos, DonHangService donHang,
    IWebHostEnvironment env, ILogger<ThanhToanController> log) : Controller
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool LaHocVien => User.IsInRole(VaiTro.HocVien);
    // Chưa có khoá PayOS thì máy dev được giả lập thanh toán để thử trọn luồng; môi trường thật thì không.
    private bool GiaLap => !payos.DaCauHinh && env.IsDevelopment();

    public async Task<IActionResult> Index() =>
        View(await db.DonHang.AsNoTracking().Where(d => d.NguoiDungId == Uid).OrderByDescending(d => d.NgayTao).Take(50).ToListAsync());

    public async Task<IActionResult> Mua(int id)
    {
        var goi = await db.GoiBan.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id && g.DangBan);
        if (goi is null) return NotFound();
        await NapThongTin(goi);
        return View(goi);
    }

    [HttpPost, ValidateAntiForgeryToken, ActionName(nameof(Mua))]
    public async Task<IActionResult> TaoDon(int id, CancellationToken ct)
    {
        var goi = await db.GoiBan.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id && g.DangBan, ct);
        if (goi is null) return NotFound();
        var loi = !LaHocVien ? "Chỉ tài khoản học viên mua được gói học."
            : goi.Gia is not > 0 ? "Gói này chưa có giá bán."
            : goi.KhoaIds.Count == 0 ? "Gói này chưa gắn khoá học nào."
            : !payos.DaCauHinh && !GiaLap ? "Thanh toán online chưa được cấu hình. Vui lòng liên hệ trung tâm." : null;
        if (loi is not null) { TempData["LoiThanhToan"] = loi; return RedirectToAction(nameof(Mua), new { id }); }

        var don = await donHang.TaoDonAsync(Uid, goi, ct);
        if (GiaLap)
        {
            await donHang.XacNhanDaThanhToanAsync(don.Id, "GIA-LAP", ct);
            return RedirectToAction(nameof(KetQua), new { ma = don.MaThanhToan });
        }
        try
        {
            var ketQua = Url.Action(nameof(KetQua), "ThanhToan", new { ma = don.MaThanhToan }, Request.Scheme)!;
            var link = await payos.TaoLinkAsync(don.MaThanhToan, don.SoTien, $"SD{don.Id}", ketQua, ketQua, ct);
            await db.DonHang.Where(d => d.Id == don.Id).ExecuteUpdateAsync(s => s
                .SetProperty(d => d.MaLinkThanhToan, link.PaymentLinkId).SetProperty(d => d.LinkThanhToan, link.CheckoutUrl), ct);
            return Redirect(link.CheckoutUrl);
        }
        catch (InvalidOperationException ex)
        {
            await donHang.HuyAsync(don.Id, ct);
            TempData["LoiThanhToan"] = ex.Message;
            return RedirectToAction(nameof(Mua), new { id });
        }
    }

    // PayOS chuyển về đây cả khi trả xong lẫn khi huỷ; không tin tham số trên URL mà hỏi lại PayOS.
    public async Task<IActionResult> KetQua(long ma, CancellationToken ct)
    {
        var don = await db.DonHang.AsNoTracking().FirstOrDefaultAsync(d => d.MaThanhToan == ma && d.NguoiDungId == Uid, ct);
        if (don is null) return NotFound();
        if (don.TrangThai == TrangThaiDon.ChoThanhToan && payos.DaCauHinh)
        {
            try
            {
                var tt = await payos.LayTrangThaiAsync(don.MaThanhToan, ct);
                if (tt.Status == "PAID" && tt.AmountPaid >= don.SoTien) await donHang.XacNhanDaThanhToanAsync(don.Id, null, ct);
                else if (tt.Status is "CANCELLED" or "EXPIRED") await donHang.HuyAsync(don.Id, ct);
            }
            catch (InvalidOperationException ex)
            {
                log.LogWarning(ex, "Không kiểm tra được trạng thái đơn {MaThanhToan}", ma);
                TempData["LoiThanhToan"] = "Chưa kiểm tra được trạng thái thanh toán. Tải lại trang sau ít phút.";
            }
            don = await db.DonHang.AsNoTracking().FirstAsync(d => d.Id == don.Id, ct);
        }
        ViewBag.Khoa = await db.Khoa.AsNoTracking().Where(k => don.KhoaIds.Contains(k.Id)).OrderBy(k => k.ThuTu).ToListAsync(ct);
        return View(don);
    }

    private async Task NapThongTin(GoiBan goi)
    {
        ViewBag.Khoa = await db.Khoa.AsNoTracking().Where(k => goi.KhoaIds.Contains(k.Id)).OrderBy(k => k.ThuTu).ToListAsync();
        ViewBag.HanHienTai = await db.GhiDanh.AsNoTracking().Where(g => g.NguoiDungId == Uid && goi.KhoaIds.Contains(g.KhoaId))
            .ToDictionaryAsync(g => g.KhoaId, g => g.HanDen);
        ViewBag.LaHocVien = LaHocVien;
        ViewBag.CoTheThanhToan = payos.DaCauHinh || GiaLap;
        ViewBag.GiaLap = GiaLap;
    }
}
