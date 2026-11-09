using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SuDia.Web.Models;
using SuDia.Web.Data;
using SuDia.Web.Services;

namespace SuDia.Web.Controllers;

// Mua gói học: xác nhận → PayOS → trang kết quả hỏi lại PayOS rồi mở khoá
[Authorize]
public class ThanhToanController(AppDbContext db, PayOSService payos, DonHangService donHang,
    IWebHostEnvironment env, UserManager<NguoiDung> users, IOptions<PayOSOptions> payosOpt, ILogger<ThanhToanController> log) : Controller
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool LaHocVien => User.IsInRole(VaiTro.HocVien);
    // Chưa có khoá PayOS thì máy dev được giả lập thanh toán để thử trọn luồng; môi trường thật thì không.
    private bool GiaLap => !payos.DaCauHinh && env.IsDevelopment();

    private static MenuTaiKhoan Menu(string muc) => new(muc, 0, 30);
    private int PhutHetHan => Math.Clamp(payosOpt.Value.PhutHetHanLink, 5, 60);

    // ------------------------------------------------------------------ Chọn gói
    public async Task<IActionResult> Goi(CancellationToken ct)
    {
        var goi = await db.GoiBan.AsNoTracking().Where(g => g.DangBan).OrderBy(g => g.ThuTu).ToListAsync(ct);
        var khoaIds = goi.SelectMany(g => g.KhoaIds).Distinct().ToList();
        var khoa = await db.Khoa.AsNoTracking().Where(k => khoaIds.Contains(k.Id)).ToDictionaryAsync(k => k.Id, ct);
        var ghiDanh = await db.GhiDanh.AsNoTracking().Where(g => g.NguoiDungId == Uid).ToDictionaryAsync(g => g.KhoaId, g => g.HanDen, ct);
        var dangHoc = await db.GhiDanh.AsNoTracking().Where(g => g.NguoiDungId == Uid && (g.HanDen == null || g.HanDen > DateTime.UtcNow))
            .Include(g => g.Khoa).OrderBy(g => g.Khoa!.ThuTu).ToListAsync(ct);
        return View(new ChonGoiViewModel
        {
            Menu = Menu("MuaGoi"),
            Goi = goi.Select(g => new GoiHienThi(g, KhoaTrongGoi(g, khoa, ghiDanh))).ToList(),
            DangHoc = dangHoc.Where(g => g.Khoa is not null).Select(g => new KhoaTrongGoi(g.Khoa!, true, g.HanDen is null, g.HanDen, g.HanDen)).ToList(),
            LaHocVien = LaHocVien, CoTheThanhToan = payos.DaCauHinh || GiaLap, GiaLap = GiaLap,
        });
    }

    private static List<KhoaTrongGoi> KhoaTrongGoi(GoiBan goi, Dictionary<string, Khoa> khoa, Dictionary<string, DateTime?> ghiDanh)
    {
        var now = DateTime.UtcNow;
        return goi.KhoaIds.Where(khoa.ContainsKey).Select(id =>
        {
            var daCo = ghiDanh.TryGetValue(id, out var han);
            // Cùng quy tắc với DonHangService: còn hạn thì cộng nối tiếp, hết hạn thì tính từ hôm nay, không giới hạn giữ nguyên
            DateTime? moi = !daCo ? now.AddMonths(goi.SoThang) : han is null ? null : (han > now ? han.Value : now).AddMonths(goi.SoThang);
            return new KhoaTrongGoi(khoa[id], daCo, daCo && han is null, han, moi);
        }).ToList();
    }

    private async Task<GoiHienThi?> LayGoiAsync(int id, CancellationToken ct)
    {
        var goi = await db.GoiBan.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id && g.DangBan, ct);
        if (goi is null) return null;
        var khoa = await db.Khoa.AsNoTracking().Where(k => goi.KhoaIds.Contains(k.Id)).ToDictionaryAsync(k => k.Id, ct);
        var ghiDanh = await db.GhiDanh.AsNoTracking().Where(g => g.NguoiDungId == Uid && goi.KhoaIds.Contains(g.KhoaId))
            .ToDictionaryAsync(g => g.KhoaId, g => g.HanDen, ct);
        return new GoiHienThi(goi, KhoaTrongGoi(goi, khoa, ghiDanh));
    }

    // ------------------------------------------------------------------ Xác nhận mua
    public async Task<IActionResult> Mua(int id, CancellationToken ct)
    {
        var goi = await LayGoiAsync(id, ct);
        if (goi is null) return NotFound();
        var nd = await users.GetUserAsync(User);
        var han = DateTime.UtcNow.AddMinutes(-PhutHetHan);
        var dangCho = await db.DonHang.AsNoTracking().Where(d => d.NguoiDungId == Uid && d.GoiBanId == id
            && d.TrangThai == TrangThaiDon.ChoThanhToan && d.LinkThanhToan != null && d.NgayTao > han)
            .OrderByDescending(d => d.NgayTao).FirstOrDefaultAsync(ct);
        return View(new MuaGoiViewModel
        {
            Menu = Menu("MuaGoi"), Goi = goi, HoTen = nd?.HoTen ?? "", Email = nd?.Email ?? "",
            LaHocVien = LaHocVien, CoTheThanhToan = payos.DaCauHinh || GiaLap, GiaLap = GiaLap,
            DonDangCho = dangCho, PhutHetHan = PhutHetHan,
        });
    }

    [HttpPost, ValidateAntiForgeryToken, ActionName(nameof(Mua))]
    public async Task<IActionResult> TaoDon(int id, bool dongY, CancellationToken ct)
    {
        var goi = await db.GoiBan.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id && g.DangBan, ct);
        if (goi is null) return NotFound();
        var loi = !LaHocVien ? "Chỉ tài khoản học viên mua được gói học."
            : goi.Gia is not > 0 ? "Gói này chưa có giá bán."
            : goi.KhoaIds.Count == 0 ? "Gói này chưa gắn khoá học nào."
            : !dongY ? "Bạn cần đồng ý với điều khoản mua gói trước khi thanh toán."
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

    // ------------------------------------------------------------------ Kết quả
    // PayOS chuyển về đây cả khi trả xong lẫn khi huỷ; không tin tham số trên URL mà hỏi lại PayOS.
    public async Task<IActionResult> KetQua(long ma, CancellationToken ct)
    {
        var don = await db.DonHang.AsNoTracking().FirstOrDefaultAsync(d => d.MaThanhToan == ma && d.NguoiDungId == Uid, ct);
        if (don is null) return NotFound();
        don = await DongBoAsync(don, ct, baoLoi: true);
        var khoa = await db.Khoa.AsNoTracking().Where(k => don.KhoaIds.Contains(k.Id)).OrderBy(k => k.ThuTu).ToListAsync(ct);
        return View(new KetQuaThanhToanViewModel
        {
            Menu = Menu("DonHang"), Don = don, Khoa = khoa,
            HetHanLink = don.TrangThai == TrangThaiDon.ChoThanhToan ? don.NgayTao.AddMinutes(PhutHetHan) : null,
        });
    }

    // Trang kết quả hỏi định kỳ endpoint này khi đang chờ thanh toán
    [HttpGet]
    public async Task<IActionResult> TrangThai(long ma, CancellationToken ct)
    {
        var don = await db.DonHang.AsNoTracking().FirstOrDefaultAsync(d => d.MaThanhToan == ma && d.NguoiDungId == Uid, ct);
        if (don is null) return NotFound();
        don = await DongBoAsync(don, ct, baoLoi: false);
        return Json(new { trangThai = don.TrangThai });
    }

    private async Task<DonHang> DongBoAsync(DonHang don, CancellationToken ct, bool baoLoi)
    {
        if (don.TrangThai != TrangThaiDon.ChoThanhToan || !payos.DaCauHinh) return don;
        try
        {
            var tt = await payos.LayTrangThaiAsync(don.MaThanhToan, ct);
            if (tt.Status == "PAID" && tt.AmountPaid >= don.SoTien) await donHang.XacNhanDaThanhToanAsync(don.Id, null, ct);
            else if (tt.Status is "CANCELLED" or "EXPIRED") await donHang.HuyAsync(don.Id, ct);
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "Không kiểm tra được trạng thái đơn {MaThanhToan}", don.MaThanhToan);
            if (baoLoi) TempData["LoiThanhToan"] = "Chưa kiểm tra được trạng thái thanh toán. Tải lại trang sau ít phút.";
        }
        return await db.DonHang.AsNoTracking().FirstAsync(d => d.Id == don.Id, ct);
    }

    // ------------------------------------------------------------------ Lịch sử và biên lai
    public async Task<IActionResult> Index(string? tt, CancellationToken ct)
    {
        var tatCa = await db.DonHang.AsNoTracking().Where(d => d.NguoiDungId == Uid).OrderByDescending(d => d.NgayTao).Take(200).ToListAsync(ct);
        var loc = tt is TrangThaiDon.DaThanhToan or TrangThaiDon.ChoThanhToan or TrangThaiDon.DaHuy ? tt : "";
        return View(new LichSuDonViewModel
        {
            Menu = Menu("DonHang"), Loc = loc,
            Don = tatCa.Where(d => loc == "" || d.TrangThai == loc).ToList(),
            TongDon = tatCa.Count,
            SoDaTra = tatCa.Count(d => d.TrangThai == TrangThaiDon.DaThanhToan),
            SoDangCho = tatCa.Count(d => d.TrangThai == TrangThaiDon.ChoThanhToan),
            TongDaTra = tatCa.Where(d => d.TrangThai == TrangThaiDon.DaThanhToan).Sum(d => (long)d.SoTien),
        });
    }

    public async Task<IActionResult> HoaDon(long ma, CancellationToken ct)
    {
        var don = await db.DonHang.AsNoTracking().FirstOrDefaultAsync(d => d.MaThanhToan == ma && d.NguoiDungId == Uid && d.TrangThai == TrangThaiDon.DaThanhToan, ct);
        if (don is null) return NotFound();
        var nd = await users.GetUserAsync(User);
        var khoa = await db.Khoa.AsNoTracking().Where(k => don.KhoaIds.Contains(k.Id)).OrderBy(k => k.ThuTu).ToListAsync(ct);
        return View(new HoaDonViewModel { Don = don, Khoa = khoa, HoTen = nd?.HoTen ?? "", Email = nd?.Email ?? "" });
    }
}
