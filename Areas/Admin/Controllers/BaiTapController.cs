using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

public class BaiTapController(AppDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? q, string? khoa, string? trangThai)
    {
        var query = Db.BaiTapGiao.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(khoa)) query = query.Where(b => b.Bai!.ChuDe!.KhoaId == khoa);
        if (!string.IsNullOrWhiteSpace(q)) { var tu = q.Trim().ToLower(); query = query.Where(b => b.Ten.ToLower().Contains(tu) || b.Bai!.Ten.ToLower().Contains(tu)); }
        ViewBag.DaGiao = await query.CountAsync(b => b.DaGiao);
        ViewBag.Nhap = await query.CountAsync(b => !b.DaGiao);
        ViewBag.ChoCham = await Db.BaiNopGiao.CountAsync(n => n.DiemTuLuan == null && query.Any(b => b.Id == n.BaiTapGiaoId));
        query = trangThai switch
        {
            "da-giao" => query.Where(b => b.DaGiao),
            "nhap" => query.Where(b => !b.DaGiao),
            "cho-cham" => query.Where(b => Db.BaiNopGiao.Any(n => n.BaiTapGiaoId == b.Id && n.DiemTuLuan == null)),
            _ => query,
        };
        ViewBag.Tong = await query.CountAsync(); ViewBag.Q = q; ViewBag.KhoaId = khoa; ViewBag.TrangThai = trangThai;
        ViewBag.DsKhoa = await Db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
        return View(await query.OrderByDescending(b => b.NgayTao).ThenByDescending(b => b.Id).Take(200)
            .Select(b => new AdminBaiTapRow(b.Id, b.BaiId, b.Ten, b.Bai!.Ten, b.Bai.ChuDe!.KhoaId, b.Bai.ChuDe.Khoa!.Ten,
                b.DaGiao, b.HanNop, Db.BaiNopGiao.Count(n => n.BaiTapGiaoId == b.Id), Db.BaiNopGiao.Count(n => n.BaiTapGiaoId == b.Id && n.DiemTuLuan == null))).ToListAsync());
    }
}
