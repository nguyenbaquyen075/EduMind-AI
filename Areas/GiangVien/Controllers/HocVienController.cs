using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Theo dõi học viên của khoá: tiến độ, điểm, bài đã nộp và câu làm sai
public class HocVienController(AppDbContext db) : GiangVienControllerBase(db)
{
    public async Task<IActionResult> Index(string? khoa)
    {
        var dsKhoa = await KhoaCuaToi().AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
        var chon = dsKhoa.FirstOrDefault(k => k.Id == khoa) ?? dsKhoa.FirstOrDefault();
        ViewBag.DsKhoa = dsKhoa;
        ViewBag.Chon = chon;
        if (chon is null) return View();

        var baiIds = await Db.Bai.Where(b => b.ChuDe!.KhoaId == chon.Id && b.DaXuatBan).Select(b => b.Id).ToListAsync();
        var deIds = await Db.DeThi.Where(d => d.KhoaId == chon.Id).Select(d => d.Id).ToListAsync();
        var maDe = baiIds.Concat(deIds).ToList();
        ViewBag.TongBai = baiIds.Count;
        ViewBag.HocVien = await Db.GhiDanh.AsNoTracking().Where(g => g.KhoaId == chon.Id)
            .Select(g => new
            {
                g.NguoiDungId, Ten = g.NguoiDung!.HoTen, g.NguoiDung.Email, g.HanDen,
                DaHoc = Db.TienDoBai.Count(t => t.NguoiDungId == g.NguoiDungId && baiIds.Contains(t.BaiId) && t.TrangThai == TrangThaiHoc.DaHoc),
                SoBaiLam = Db.BaiLam.Count(b => b.NguoiDungId == g.NguoiDungId && maDe.Contains(b.MaDe)),
                Cau = Db.BaiLam.Where(b => b.NguoiDungId == g.NguoiDungId && maDe.Contains(b.MaDe)).Sum(b => (int?)b.SoCau) ?? 0,
                Dung = Db.BaiLam.Where(b => b.NguoiDungId == g.NguoiDungId && maDe.Contains(b.MaDe)).Sum(b => (int?)b.SoDung) ?? 0,
                LanCuoi = Db.BaiLam.Where(b => b.NguoiDungId == g.NguoiDungId && maDe.Contains(b.MaDe)).Max(b => (DateTime?)b.NopLuc),
            })
            .OrderBy(x => x.Ten).ToListAsync();
        return View();
    }

    public async Task<IActionResult> ChiTiet(string id, string khoa)
    {
        if (!await KhoaCuaToi().AnyAsync(k => k.Id == khoa) || !await Db.GhiDanh.AnyAsync(g => g.NguoiDungId == id && g.KhoaId == khoa)) return NotFound();
        var nd = await Db.Users.AsNoTracking().FirstAsync(u => u.Id == id);
        var baiIds = await Db.Bai.Where(b => b.ChuDe!.KhoaId == khoa).Select(b => b.Id).ToListAsync();
        var deIds = await Db.DeThi.Where(d => d.KhoaId == khoa).Select(d => d.Id).ToListAsync();
        var maDe = baiIds.Concat(deIds).ToList();

        ViewBag.NguoiDung = nd;
        ViewBag.Khoa = await Db.Khoa.AsNoTracking().FirstAsync(k => k.Id == khoa);
        ViewBag.BaiLam = await Db.BaiLam.AsNoTracking().Where(b => b.NguoiDungId == id && maDe.Contains(b.MaDe)).OrderByDescending(b => b.NopLuc)
            .Select(b => new
            {
                b.Id, b.TenDe, b.SoDung, b.SoCau, b.NopLuc,
                CauSai = b.TraLoi.Where(t => !t.Dung).OrderBy(t => t.ThuTu)
                    .Join(Db.CauHoi, t => t.CauHoiId, c => c.Id, (t, c) => new { t.ThuTu, c.NoiDung, t.DaChon, c.DapAnDung }).ToList(),
            }).ToListAsync();
        ViewBag.TienDo = await Db.Bai.AsNoTracking().Where(b => b.ChuDe!.KhoaId == khoa && b.DaXuatBan).OrderBy(b => b.So)
            .Select(b => new { b.So, b.Ten, TrangThai = Db.TienDoBai.Where(t => t.NguoiDungId == id && t.BaiId == b.Id).Select(t => (TrangThaiHoc?)t.TrangThai).FirstOrDefault() })
            .ToListAsync();
        return View();
    }
}
