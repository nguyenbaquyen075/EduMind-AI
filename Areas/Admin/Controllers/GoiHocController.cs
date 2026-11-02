using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Areas.Admin.Controllers;

// Gói học bán trên trang chủ (giá, quyền lợi, khoá được mở) và đơn hàng thanh toán PayOS
public class GoiHocController(AppDbContext db, PayOSService payos) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Goi = await Db.GoiBan.AsNoTracking().OrderBy(g => g.ThuTu).ToListAsync();
        ViewBag.TenKhoa = await Db.Khoa.AsNoTracking().ToDictionaryAsync(k => k.Id, k => k.Ten);
        ViewBag.DaCauHinh = payos.DaCauHinh;
        return View();
    }

    public async Task<IActionResult> Tao()
    {
        await NapKhoa();
        return View("Sua", new GoiBanForm());
    }

    public async Task<IActionResult> Sua(int id)
    {
        var g = await Db.GoiBan.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (g is null) return NotFound();
        await NapKhoa();
        return View(new GoiBanForm { Id = g.Id, Ten = g.Ten, Nhan = g.Nhan, MauNhan = g.MauNhan, Gia = g.Gia, SoThang = g.SoThang,
            NoiBat = g.NoiBat, DangBan = g.DangBan, QuyenLoi = string.Join("\n", g.QuyenLoi), KhoaIds = g.KhoaIds });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(GoiBanForm f)
    {
        var khoaIds = await Db.Khoa.Select(k => k.Id).ToListAsync();
        f.KhoaIds = f.KhoaIds.Where(khoaIds.Contains).Distinct().ToList();
        if (f.DangBan && f.Gia is not null && f.KhoaIds.Count == 0)
            ModelState.AddModelError(nameof(f.KhoaIds), "Chọn ít nhất một khoá được mở khi mua gói.");
        if (!ModelState.IsValid) { await NapKhoa(); return View("Sua", f); }

        var g = f.Id == 0 ? null : await Db.GoiBan.FindAsync(f.Id);
        if (f.Id != 0 && g is null) return NotFound();
        if (g is null)
        {
            g = new GoiBan { ThuTu = (await Db.GoiBan.MaxAsync(x => (int?)x.ThuTu) ?? -1) + 1 };
            Db.GoiBan.Add(g);
        }
        g.Ten = f.Ten.Trim(); g.Nhan = f.Nhan?.Trim() ?? ""; g.MauNhan = f.MauNhan; g.Gia = f.Gia; g.SoThang = f.SoThang;
        g.NoiBat = f.NoiBat; g.DangBan = f.DangBan; g.KhoaIds = f.KhoaIds;
        g.QuyenLoi = (f.QuyenLoi ?? "").Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        await Db.SaveChangesAsync();
        ThongBao(g.DangBan && g.Gia is null ? $"Đã lưu gói {g.Ten}. Gói chưa có giá nên chưa mua được." : $"Đã lưu gói {g.Ten}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var g = await Db.GoiBan.FindAsync(id);
        if (g is null) return NotFound();
        if (await Db.DonHang.AnyAsync(d => d.GoiBanId == id))
        {
            BaoLoi($"Gói {g.Ten} đã có đơn hàng. Bỏ chọn \"Đang bán\" để ẩn gói thay vì xoá.");
            return RedirectToAction(nameof(Index));
        }
        Db.GoiBan.Remove(g);
        await Db.SaveChangesAsync();
        ThongBao($"Đã xoá gói {g.Ten}.");
        return RedirectToAction(nameof(Index));
    }

    // Lỡ webhook (mạng lỗi, chưa khai báo URL) thì admin hỏi lại PayOS để mở khoá cho đơn đã trả
    private async Task NapKhoa() =>
        ViewBag.DsKhoa = await Db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
}
