using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Bàn làm việc: việc cần soạn, buổi sắp dạy, bài nộp mới, câu học viên hay sai
public class TongQuanController(AppDbContext db) : GiangVienControllerBase(db)
{
    public async Task<IActionResult> Index()
    {
        var bai = await BaiCuaToi().AsNoTracking().Where(b => b.DaXuatBan).OrderBy(b => b.ChuDe!.Khoa!.ThuTu).ThenBy(b => b.ChuDe!.ThuTu).ThenBy(b => b.ThuTu)
            .Select(b => new { b.Id, b.So, b.Ten, Khoa = b.ChuDe!.Khoa!.Ten, SoMuc = b.Muc.Count, SoCau = b.CauHoi.Count }).ToListAsync();
        var baiIds = bai.Select(b => b.Id).ToList();
        var khoaIds = await KhoaCuaToi().Select(k => k.Id).ToListAsync();
        var deIds = await Db.DeThi.Where(d => khoaIds.Contains(d.KhoaId)).Select(d => d.Id).ToListAsync();
        var maDe = baiIds.Concat(deIds).ToList();

        var viec = bai.Where(b => b.SoMuc == 0).Take(4)
            .Select(b => new ViecCanLam($"{b.Khoa} · Bài {b.So}. {b.Ten} — chưa có nội dung", $"/GiangVien/NoiDung/SoanBai/{b.Id}", "Soạn bài"))
            .Concat(bai.Where(b => b.SoMuc > 0 && b.SoCau == 0).Take(4)
            .Select(b => new ViecCanLam($"{b.Khoa} · Bài {b.So}. {b.Ten} — chưa có câu hỏi", $"/GiangVien/CauHoi/Tao?bai={b.Id}", "Thêm câu hỏi")))
            .ToList();

        var bayNgay = DateTime.UtcNow.AddDays(-7);
        var tuan = await Db.BaiLam.AsNoTracking().Where(b => maDe.Contains(b.MaDe) && b.NopLuc >= bayNgay).Select(b => new { b.SoCau, b.SoDung }).ToListAsync();
        var cauTuan = tuan.Sum(b => b.SoCau);

        return View(new GiangVienTongQuanViewModel
        {
            SoLieu =
            [
                new("Học viên ghi danh", (await Db.GhiDanh.CountAsync(g => khoaIds.Contains(g.KhoaId))).ToString()),
                new("Bài đã có nội dung", $"{bai.Count(b => b.SoMuc > 0)}/{bai.Count}"),
                new("Câu hỏi trong ngân hàng", bai.Sum(b => b.SoCau).ToString()),
                new("Bài nộp 7 ngày qua", tuan.Count.ToString(), cauTuan == 0 ? null : $"đúng {Math.Round(tuan.Sum(b => b.SoDung) * 100.0 / cauTuan)}%"),
            ],
            ViecCanLam = viec,
            BaiLam = await Db.BaiLam.AsNoTracking().Where(b => maDe.Contains(b.MaDe)).OrderByDescending(b => b.NopLuc).Take(6)
                .Select(b => new BaiLamGanDay(b.NguoiDung!.HoTen != "" ? b.NguoiDung.HoTen : b.NguoiDung.Email!, b.TenDe, b.SoDung, b.SoCau, b.NopLuc)).ToListAsync(),
            CauSai = (await Db.CauTraLoi.AsNoTracking()
                .Join(Db.CauHoi, t => t.CauHoiId, c => c.Id, (t, c) => new { t.Dung, c.Id, c.NoiDung, c.NguonHienThi, c.BaiId })
                .Where(x => baiIds.Contains(x.BaiId))
                .GroupBy(x => new { x.Id, x.NoiDung, x.NguonHienThi })
                .Select(g => new { g.Key.Id, g.Key.NoiDung, g.Key.NguonHienThi, Sai = g.Count(x => !x.Dung), Lam = g.Count() })
                .Where(x => x.Sai > 0).OrderByDescending(x => x.Sai).Take(5).ToListAsync())
                .Select(x => new CauSaiNhieu(x.Id, x.NoiDung, x.NguonHienThi, x.Sai, x.Lam)).ToList(),
            BuoiSapToi = await Db.BuoiHoc.AsNoTracking().Where(b => baiIds.Contains(b.BaiId) && b.ThoiGian >= DateTime.UtcNow).OrderBy(b => b.ThoiGian).Take(5)
                .Select(b => new BuoiSapToi(b.Id, $"Bài {b.Bai!.So}. {b.Bai.Ten}", b.Link, b.ThoiGian!.Value)).ToListAsync(),
        });
    }
}
