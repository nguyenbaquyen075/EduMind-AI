using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

public class LichDayController(AppDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? gv, string? khoa, string? trangThai = "sap-toi")
    {
        if (await ChonGiangVien(gv, khoa) is not { } dsKhoa) return await ChonGiangVienView("LichDay", "Lịch dạy");
        if (!dsKhoa.Contains(khoa ?? "")) khoa = null;
        var now = DateTime.UtcNow;
        var query = Db.BuoiHoc.AsNoTracking().Where(b => dsKhoa.Contains(b.Bai!.ChuDe!.KhoaId));
        if (!string.IsNullOrWhiteSpace(khoa)) query = query.Where(b => b.Bai!.ChuDe!.KhoaId == khoa);
        ViewBag.SapToi = await query.CountAsync(b => b.ThoiGian >= now);
        ViewBag.DaQua = await query.CountAsync(b => b.ThoiGian < now);
        ViewBag.ChuaDat = await query.CountAsync(b => b.ThoiGian == null);
        ViewBag.TatCa = await query.CountAsync();
        query = trangThai switch
        {
            "sap-toi" => query.Where(b => b.ThoiGian >= now),
            "da-qua" => query.Where(b => b.ThoiGian < now),
            "chua-dat" => query.Where(b => b.ThoiGian == null),
            _ => query,
        };
        ViewBag.KhoaId = khoa; ViewBag.TrangThai = trangThai; ViewBag.Tong = await query.CountAsync();
        var xep = trangThai == "da-qua" ? query.OrderByDescending(b => b.ThoiGian) : query.OrderBy(b => b.ThoiGian == null).ThenBy(b => b.ThoiGian);
        return View(await xep.ThenBy(b => b.Id).Take(200)
            .Select(b => new AdminBuoi(b.Id, b.BaiId, $"Bài {b.Bai!.So}. {b.Bai.Ten}", b.Bai.ChuDe!.Khoa!.Ten,
                b.Bai.ChuDe.Khoa.GiangVien == null ? null : b.Bai.ChuDe.Khoa.GiangVien.HoTen, b.ThoiGian, b.Link)).ToListAsync());
    }
}
