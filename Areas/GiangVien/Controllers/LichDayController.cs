using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Lịch dạy online: buổi học gắn với bài, link Google Meet + giờ. Học viên mở từ menu ⋮ "Học online"
public class LichDayController(AppDbContext db) : GiangVienControllerBase(db)
{
    public async Task<IActionResult> Index()
    {
        var baiIds = await BaiCuaToi().Select(b => b.Id).ToListAsync();
        ViewBag.Buoi = await Db.BuoiHoc.AsNoTracking().Where(b => baiIds.Contains(b.BaiId))
            .OrderBy(b => b.ThoiGian == null).ThenBy(b => b.ThoiGian)
            .Select(b => new { b.Id, b.Link, b.ThoiGian, Bai = $"Bài {b.Bai!.So}. {b.Bai.Ten}", Khoa = b.Bai.ChuDe!.Khoa!.Ten }).ToListAsync();
        return View();
    }

    public async Task<IActionResult> Tao(int? bai)
    {
        ViewBag.DsBai = await DanhSachBai();
        return View("Sua", new BuoiHocForm { BaiId = bai ?? 0, ThoiGian = DateTime.Today.AddDays(1).AddHours(19).AddMinutes(30) });
    }

    public async Task<IActionResult> Sua(int id)
    {
        var b = await Db.BuoiHoc.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b is null || !await BaiCuaToi().AnyAsync(x => x.Id == b.BaiId)) return NotFound();
        ViewBag.DsBai = await DanhSachBai();
        return View(new BuoiHocForm { Id = b.Id, BaiId = b.BaiId, Link = b.Link, ThoiGian = b.ThoiGian?.ToLocalTime() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(BuoiHocForm f)
    {
        if (!Uri.TryCreate(f.Link?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            ModelState.AddModelError(nameof(f.Link), "Link phải bắt đầu bằng https://");
        if (!await BaiCuaToi().AnyAsync(x => x.Id == f.BaiId)) ModelState.AddModelError(nameof(f.BaiId), "Chọn bài");
        if (!ModelState.IsValid)
        {
            ViewBag.DsBai = await DanhSachBai();
            return View("Sua", f);
        }

        var b = f.Id == 0 ? null : await Db.BuoiHoc.FirstOrDefaultAsync(x => x.Id == f.Id);
        if (f.Id != 0 && (b is null || !await BaiCuaToi().AnyAsync(x => x.Id == b.BaiId))) return NotFound();
        if (b is null) { b = new BuoiHoc(); Db.BuoiHoc.Add(b); }
        b.BaiId = f.BaiId;
        b.Link = uri!.ToString();
        // Ô giờ trên trình duyệt là giờ địa phương → lưu UTC
        b.ThoiGian = f.ThoiGian is null ? null : DateTime.SpecifyKind(f.ThoiGian.Value, DateTimeKind.Local).ToUniversalTime();
        await Db.SaveChangesAsync();
        ThongBao("Đã lưu buổi học. Học viên thấy ở menu ⋮ \"Học online\" của bài.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var b = await Db.BuoiHoc.FirstOrDefaultAsync(x => x.Id == id);
        if (b is null || !await BaiCuaToi().AnyAsync(x => x.Id == b.BaiId)) return NotFound();
        Db.BuoiHoc.Remove(b);
        await Db.SaveChangesAsync();
        ThongBao("Đã xoá buổi học.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> DanhSachBai() =>
        (await BaiCuaToi().AsNoTracking().OrderBy(b => b.ChuDe!.Khoa!.ThuTu).ThenBy(b => b.So)
            .Select(b => new { b.Id, b.So, b.Ten, Khoa = b.ChuDe!.Khoa!.Ten }).ToListAsync())
        .Select(b => new SelectListItem($"{b.Khoa} · Bài {b.So}. {b.Ten}", b.Id.ToString())).ToList();
}
