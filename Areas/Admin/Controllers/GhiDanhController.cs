using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

public class GhiDanhController(AppDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? q, string? khoa, string? trangThai, int trang = 1)
    {
        var now = DateTime.UtcNow;
        var query = Db.GhiDanh.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim(); var tu = q.ToLower();
            query = query.Where(g => g.NguoiDung!.HoTen.ToLower().Contains(tu) || g.NguoiDung.Email!.ToLower().Contains(tu));
        }
        if (!string.IsNullOrWhiteSpace(khoa)) query = query.Where(g => g.KhoaId == khoa);
        query = trangThai switch
        {
            "con-han" => query.Where(g => g.HanDen == null || g.HanDen >= now),
            "sap-het" => query.Where(g => g.HanDen >= now && g.HanDen <= now.AddDays(14)),
            "het-han" => query.Where(g => g.HanDen < now),
            _ => query,
        };
        var tong = await query.CountAsync();
        trang = Math.Clamp(trang, 1, Math.Max(1, (int)Math.Ceiling(tong / 25d)));
        return View(new AdminGhiDanhViewModel
        {
            Ds = await query.OrderByDescending(g => g.NgayTao).ThenBy(g => g.NguoiDungId).ThenBy(g => g.KhoaId).Skip((trang - 1) * 25).Take(25)
                .Select(g => new AdminGhiDanhRow(g.NguoiDungId, g.NguoiDung!.HoTen, g.NguoiDung.Email, g.KhoaId, g.Khoa!.Ten, g.NgayTao, g.HanDen)).ToListAsync(),
            DsKhoa = await Db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync(), Q = q, KhoaId = khoa, TrangThai = trangThai, Trang = trang, Tong = tong,
            DangHoc = await Db.GhiDanh.CountAsync(g => g.HanDen == null || g.HanDen >= now),
            SapHetHan = await Db.GhiDanh.CountAsync(g => g.HanDen >= now && g.HanDen <= now.AddDays(14)),
            HetHan = await Db.GhiDanh.CountAsync(g => g.HanDen < now),
        });
    }
}
