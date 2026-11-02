using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

// Giảng viên: khoá phụ trách, học viên, nội dung đã xuất bản, bài tập và mức hoàn thành, lịch sắp tới
public class GiaoVienController(AppDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var gv = await Db.UserRoles.Join(Db.Roles.Where(r => r.Name == VaiTro.GiangVien), ur => ur.RoleId, r => r.Id, (ur, r) => ur.UserId)
            .Union(Db.Khoa.Where(k => k.GiangVienId != null).Select(k => k.GiangVienId!))
            .Join(Db.Users, id => id, u => u.Id, (id, u) => new { u.Id, u.HoTen, u.Email, u.LockoutEnd }).ToListAsync();
        var khoa = await Db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu).Select(k => new
        {
            k.Ten, k.GiangVienId,
            HocVien = Db.GhiDanh.Count(g => g.KhoaId == k.Id && (g.HanDen == null || g.HanDen >= now)),
            XuatBan = k.ChuDe.SelectMany(c => c.Bai).Count(b => b.DaXuatBan),
            Tong = k.ChuDe.SelectMany(c => c.Bai).Count(),
            BaiTap = Db.BaiTapGiao.Count(t => t.DaGiao && t.Bai!.ChuDe!.KhoaId == k.Id),
            DaNop = Db.BaiNopGiao.Where(n => n.BaiTapGiao!.DaGiao && n.BaiTapGiao.Bai!.ChuDe!.KhoaId == k.Id
                && Db.GhiDanh.Any(g => g.KhoaId == k.Id && g.NguoiDungId == n.NguoiDungId && (g.HanDen == null || g.HanDen >= now))).Count(),
            ChoCham = Db.BaiNopGiao.Count(n => n.DiemTuLuan == null && n.BaiTapGiao!.Bai!.ChuDe!.KhoaId == k.Id),
            Buoi = Db.BuoiHoc.Count(b => b.ThoiGian >= now && b.Bai!.ChuDe!.KhoaId == k.Id),
        }).ToListAsync();
        ViewBag.ChuaPhanCong = khoa.Where(k => k.GiangVienId == null).Select(k => k.Ten).ToList();
        return View(gv.OrderBy(g => g.HoTen).Select(g =>
        {
            var ds = khoa.Where(k => k.GiangVienId == g.Id).ToList();
            var can = ds.Sum(k => k.BaiTap * k.HocVien);
            return new AdminGiaoVienRow(g.Id, g.HoTen, g.Email, g.LockoutEnd > DateTimeOffset.UtcNow, ds.Select(k => k.Ten).ToList(),
                ds.Sum(k => k.HocVien), ds.Sum(k => k.XuatBan), ds.Sum(k => k.Tong), ds.Sum(k => k.BaiTap), ds.Sum(k => k.ChoCham),
                can == 0 ? null : (int)Math.Round(ds.Sum(k => k.DaNop) * 100.0 / can), ds.Sum(k => k.Buoi));
        }).ToList());
    }
}
