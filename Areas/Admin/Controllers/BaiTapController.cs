using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

public class BaiTapController(AppDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? gv, string? q, string? khoa, string? trangThai)
    {
        if (await ChonGiangVien(gv, khoa) is not { } dsKhoa)
        {
            ViewBag.TiLe = await TiLeHoanThanh(null);
            return await ChonGiangVienView("BaiTap", "Bài tập & bài nộp");
        }
        if (!dsKhoa.Contains(khoa ?? "")) khoa = null;
        ViewBag.TiLe = await TiLeHoanThanh(khoa is null ? dsKhoa : [khoa]);
        var now = DateTime.UtcNow;
        var query = Db.BaiTapGiao.AsNoTracking().Where(b => dsKhoa.Contains(b.Bai!.ChuDe!.KhoaId));
        if (!string.IsNullOrWhiteSpace(khoa)) query = query.Where(b => b.Bai!.ChuDe!.KhoaId == khoa);
        if (!string.IsNullOrWhiteSpace(q)) { var tu = q.Trim().ToLower(); query = query.Where(b => b.Ten.ToLower().Contains(tu) || b.Bai!.Ten.ToLower().Contains(tu)); }
        ViewBag.DaGiao = await query.CountAsync(b => b.DaGiao);
        ViewBag.Nhap = await query.CountAsync(b => !b.DaGiao);
        ViewBag.TatCa = await query.CountAsync();
        ViewBag.ChoCham = await Db.BaiNopGiao.CountAsync(n => n.DiemTuLuan == null && query.Any(b => b.Id == n.BaiTapGiaoId));
        query = trangThai switch
        {
            "da-giao" => query.Where(b => b.DaGiao),
            "nhap" => query.Where(b => !b.DaGiao),
            "cho-cham" => query.Where(b => Db.BaiNopGiao.Any(n => n.BaiTapGiaoId == b.Id && n.DiemTuLuan == null)),
            _ => query,
        };
        ViewBag.Tong = await query.CountAsync(); ViewBag.Q = q; ViewBag.KhoaId = khoa; ViewBag.TrangThai = trangThai;
        return View(await query.OrderByDescending(b => b.NgayTao).ThenByDescending(b => b.Id).Take(200)
            .Select(b => new AdminBaiTapRow(b.Id, b.BaiId, b.Ten, b.Bai!.Ten, b.Bai.ChuDe!.KhoaId, b.Bai.ChuDe.Khoa!.Ten,
                b.DaGiao, b.HanNop, Db.BaiNopGiao.Count(n => n.BaiTapGiaoId == b.Id), Db.BaiNopGiao.Count(n => n.BaiTapGiaoId == b.Id && n.DiemTuLuan == null),
                Db.GhiDanh.Count(g => g.KhoaId == b.Bai.ChuDe.KhoaId && (g.HanDen == null || g.HanDen >= now)))).ToListAsync());
    }

    // Tỉ lệ hoàn thành = lượt nộp (mỗi học viên × bài tập tính 1) / (bài tập đã giao × học viên còn hạn của khoá)
    private async Task<List<AdminTiLeKhoa>> TiLeHoanThanh(List<string>? dsKhoa)
    {
        var now = DateTime.UtcNow;
        var khoa = Db.Khoa.AsNoTracking().Where(k => dsKhoa == null || dsKhoa.Contains(k.Id));
        return await khoa.OrderBy(k => k.ThuTu).Select(k => new
        {
            k.Id, k.Ten,
            SoBaiTap = Db.BaiTapGiao.Count(t => t.DaGiao && t.Bai!.ChuDe!.KhoaId == k.Id),
            SoHocVien = Db.GhiDanh.Count(g => g.KhoaId == k.Id && (g.HanDen == null || g.HanDen >= now)),
            DaNop = Db.BaiNopGiao.Where(n => n.BaiTapGiao!.DaGiao && n.BaiTapGiao.Bai!.ChuDe!.KhoaId == k.Id
                    && Db.GhiDanh.Any(g => g.KhoaId == k.Id && g.NguoiDungId == n.NguoiDungId && (g.HanDen == null || g.HanDen >= now)))
                .Select(n => new { n.BaiTapGiaoId, n.NguoiDungId }).Distinct().Count(),
        }).Select(x => new AdminTiLeKhoa(x.Id, x.Ten, x.SoBaiTap, x.SoHocVien, x.DaNop)).ToListAsync();
    }
}
