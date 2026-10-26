using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

public class LichDayController(AppDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? khoa, string? trangThai = "sap-toi")
    {
        var now = DateTime.UtcNow;
        var query = Db.BuoiHoc.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(khoa)) query = query.Where(b => b.Bai!.ChuDe!.KhoaId == khoa);
        ViewBag.SapToi = await query.CountAsync(b => b.ThoiGian >= now);
        ViewBag.DaQua = await query.CountAsync(b => b.ThoiGian < now);
        ViewBag.ChuaDat = await query.CountAsync(b => b.ThoiGian == null);
        query = trangThai switch
        {
            "sap-toi" => query.Where(b => b.ThoiGian >= now),
            "da-qua" => query.Where(b => b.ThoiGian < now),
            "chua-dat" => query.Where(b => b.ThoiGian == null),
            _ => query,
        };
        ViewBag.KhoaId = khoa; ViewBag.TrangThai = trangThai; ViewBag.Tong = await query.CountAsync();
        ViewBag.DsKhoa = await Db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
        return View(await query.OrderBy(b => b.ThoiGian == null).ThenBy(b => b.ThoiGian).ThenBy(b => b.Id).Take(200)
            .Select(b => new AdminBuoi(b.Id, b.BaiId, $"Bài {b.Bai!.So}. {b.Bai.Ten}", b.Bai.ChuDe!.Khoa!.Ten,
                b.Bai.ChuDe.Khoa.GiangVien == null ? null : b.Bai.ChuDe.Khoa.GiangVien.HoTen, b.ThoiGian, b.Link)).ToListAsync());
    }
}
