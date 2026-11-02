using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

// Học viên: khoá đang học, tiến độ bài học, luyện tập, bài nộp, lần hoạt động cuối
public class HocVienController(AppDbContext db) : AdminControllerBase(db)
{
    private const int MoiTrang = 25;

    public async Task<IActionResult> Index(string? q, string? khoa, string? sapXep, int trang = 1)
    {
        var now = DateTime.UtcNow;
        var hv = Db.Users.AsNoTracking().Where(u => Db.UserRoles.Any(ur => ur.UserId == u.Id && Db.Roles.Any(r => r.Id == ur.RoleId && r.Name == VaiTro.HocVien)));
        if (!string.IsNullOrWhiteSpace(q)) { var tu = q.Trim().ToLower(); hv = hv.Where(u => u.HoTen.ToLower().Contains(tu) || u.Email!.ToLower().Contains(tu)); }
        if (!string.IsNullOrWhiteSpace(khoa)) hv = hv.Where(u => Db.GhiDanh.Any(g => g.NguoiDungId == u.Id && g.KhoaId == khoa));
        var tong = await hv.CountAsync();
        var soTrang = Math.Max(1, (tong + MoiTrang - 1) / MoiTrang);
        trang = Math.Clamp(trang, 1, soTrang);
        var dsNguoi = await hv.OrderByDescending(u => u.NgayTao).Select(u => new { u.Id, u.HoTen, u.Email }).ToListAsync();
        var ids = dsNguoi.Select(u => u.Id).ToList();

        var tenKhoa = await Db.Khoa.AsNoTracking().ToDictionaryAsync(k => k.Id, k => k.Ten);
        var baiKhoa = await Db.Bai.AsNoTracking().Where(b => b.DaXuatBan).Select(b => new { b.Id, b.ChuDe!.KhoaId }).ToDictionaryAsync(b => b.Id, b => b.KhoaId);
        var soBai = baiKhoa.GroupBy(b => b.Value).ToDictionary(g => g.Key, g => g.Count());
        var ghiDanh = (await Db.GhiDanh.AsNoTracking().Where(g => ids.Contains(g.NguoiDungId) && (g.HanDen == null || g.HanDen >= now)).ToListAsync())
            .ToLookup(g => g.NguoiDungId);
        var daHoc = (await Db.TienDoBai.AsNoTracking().Where(t => ids.Contains(t.NguoiDungId)).Select(t => new { t.NguoiDungId, t.BaiId, t.TrangThai, t.CapNhat }).ToListAsync())
            .ToLookup(t => t.NguoiDungId);
        var luyen = (await Db.BaiLam.AsNoTracking().Where(b => ids.Contains(b.NguoiDungId)).GroupBy(b => b.NguoiDungId)
            .Select(g => new { g.Key, So = g.Count(), Cuoi = g.Max(b => b.NopLuc) }).ToListAsync()).ToDictionary(x => x.Key);
        var nop = (await Db.BaiNopGiao.AsNoTracking().Where(b => ids.Contains(b.NguoiDungId)).GroupBy(b => b.NguoiDungId)
            .Select(g => new { g.Key, So = g.Count(), Cuoi = g.Max(b => b.NopLuc) }).ToListAsync()).ToDictionary(x => x.Key);

        var hang = dsNguoi.Select(u =>
        {
            var gd = ghiDanh[u.Id].ToList();
            var khoaDangHoc = gd.Select(g => g.KhoaId).ToHashSet();
            var td = daHoc[u.Id].ToList();
            var cuoi = new DateTime?[] { td.Count == 0 ? null : td.Max(t => t.CapNhat), luyen.GetValueOrDefault(u.Id)?.Cuoi, nop.GetValueOrDefault(u.Id)?.Cuoi }.Max();
            return new AdminHocVienRow(u.Id, u.HoTen, u.Email, gd.Select(g => tenKhoa.GetValueOrDefault(g.KhoaId, g.KhoaId)).ToList(),
                gd.Where(g => g.HanDen != null).Min(g => g.HanDen),
                td.Count(t => t.TrangThai == TrangThaiHoc.DaHoc && baiKhoa.TryGetValue(t.BaiId, out var k) && khoaDangHoc.Contains(k)),
                khoaDangHoc.Sum(k => soBai.GetValueOrDefault(k)),
                luyen.GetValueOrDefault(u.Id)?.So ?? 0, nop.GetValueOrDefault(u.Id)?.So ?? 0, cuoi);
        });
        hang = sapXep switch
        {
            "tien-do" => hang.OrderBy(h => h.TienDo ?? -1),
            "hoat-dong" => hang.OrderBy(h => h.HoatDongCuoi ?? DateTime.MinValue),
            "het-han" => hang.OrderBy(h => h.HanGanNhat ?? DateTime.MaxValue),
            _ => hang,
        };
        ViewBag.Q = q; ViewBag.KhoaId = khoa; ViewBag.SapXep = sapXep; ViewBag.Trang = trang; ViewBag.SoTrang = soTrang; ViewBag.Tong = tong;
        ViewBag.DsKhoa = await Db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
        ViewBag.DangHoc = await Db.GhiDanh.Where(g => g.HanDen == null || g.HanDen >= now).Select(g => g.NguoiDungId).Distinct().CountAsync();
        ViewBag.HoatDong7 = await Db.TienDoBai.Where(t => t.CapNhat >= now.AddDays(-7)).Select(t => t.NguoiDungId)
            .Union(Db.BaiLam.Where(b => b.NopLuc >= now.AddDays(-7)).Select(b => b.NguoiDungId))
            .Union(Db.BaiNopGiao.Where(b => b.NopLuc >= now.AddDays(-7)).Select(b => b.NguoiDungId))
            .Where(id => Db.UserRoles.Any(ur => ur.UserId == id && Db.Roles.Any(r => r.Id == ur.RoleId && r.Name == VaiTro.HocVien))).CountAsync();
        return View(hang.Skip((trang - 1) * MoiTrang).Take(MoiTrang).ToList());
    }
}
