using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

public class NoiDungController(AppDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? q, string? khoa, string? trangThai)
    {
        var query = Db.Bai.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(khoa)) query = query.Where(b => b.ChuDe!.KhoaId == khoa);
        if (!string.IsNullOrWhiteSpace(q)) { var tu = q.Trim().ToLower(); query = query.Where(b => b.Ten.ToLower().Contains(tu) || b.ChuDe!.Ten.ToLower().Contains(tu)); }
        ViewBag.XuatBan = await query.CountAsync(b => b.DaXuatBan);
        ViewBag.Nhap = await query.CountAsync(b => !b.DaXuatBan);
        ViewBag.Thieu = await query.CountAsync(b => !b.Muc.Any(m => m.Phan.Any()));
        query = trangThai switch
        {
            "xuat-ban" => query.Where(b => b.DaXuatBan),
            "nhap" => query.Where(b => !b.DaXuatBan),
            "thieu-noi-dung" => query.Where(b => !b.Muc.Any(m => m.Phan.Any())),
            "thieu-noi-dung-xuat-ban" => query.Where(b => b.DaXuatBan && !b.Muc.Any(m => m.Phan.Any())),
            _ => query,
        };
        ViewBag.Tong = await query.CountAsync(); ViewBag.Q = q; ViewBag.KhoaId = khoa; ViewBag.TrangThai = trangThai;
        ViewBag.DsKhoa = await Db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
        return View(await query.OrderBy(b => b.ChuDe!.Khoa!.ThuTu).ThenBy(b => b.ChuDe!.ThuTu).ThenBy(b => b.ThuTu).ThenBy(b => b.Id).Take(200)
            .Select(b => new AdminNoiDungRow(b.Id, b.So, b.Ten, b.ChuDe!.Ten, b.ChuDe.KhoaId, b.ChuDe.Khoa!.Ten,
                b.DaXuatBan, b.HocThu, b.Muc.Count, b.CauHoi.Count, Db.BaiTapGiao.Count(t => t.BaiId == b.Id))).ToListAsync());
    }
}
