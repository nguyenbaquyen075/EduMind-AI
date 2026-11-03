using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Lớp học của giảng viên: học viên từng khoá, tiến độ học, điểm bài tập, thi thử và luyện tập
public class HocVienController(AppDbContext db) : GiangVienControllerBase(db)
{
    public async Task<IActionResult> Index(string? khoa, string? q, string? sx)
    {
        var dsKhoa = await KhoaCuaToi().AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
        var chon = dsKhoa.FirstOrDefault(k => k.Id == khoa) ?? dsKhoa.FirstOrDefault();
        var now = DateTime.UtcNow;
        ViewBag.DsKhoa = await Db.Khoa.AsNoTracking().Where(k => LaAdmin || k.GiangVienId == Uid).OrderBy(k => k.ThuTu)
            .Select(k => new { k.Id, k.Ten, So = Db.GhiDanh.Count(g => g.KhoaId == k.Id && (g.HanDen == null || g.HanDen >= now)) })
            .ToDictionaryAsync(k => k.Id, k => $"{k.Ten} · {k.So} học viên");
        ViewBag.Chon = chon; ViewBag.Q = q; ViewBag.Sx = sx;
        if (chon is null) return View(new List<GvHocVienRow>());

        var ds = await BangDiem(chon.Id);
        ViewBag.TatCa = ds;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var tu = q.Trim().ToLowerInvariant();
            ds = ds.Where(h => h.Ten.ToLowerInvariant().Contains(tu) || h.Email.ToLowerInvariant().Contains(tu)).ToList();
        }
        ds = sx switch
        {
            "tien-do" => ds.OrderBy(h => h.PhanTramHoc).ToList(),
            "diem" => ds.OrderBy(h => h.DiemBaiTap ?? -1).ToList(),
            "hoat-dong" => ds.OrderBy(h => h.LanCuoi ?? DateTime.MinValue).ToList(),
            _ => ds.OrderBy(h => h.Ten).ToList(),
        };
        return View(ds);
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
        ViewBag.TomTat = (await BangDiem(khoa, id)).FirstOrDefault();
        ViewBag.BaiLam = await Db.BaiLam.AsNoTracking().Where(b => b.NguoiDungId == id && maDe.Contains(b.MaDe)).OrderByDescending(b => b.NopLuc)
            .Select(b => new
            {
                b.Id, b.TenDe, b.SoDung, b.SoCau, b.NopLuc, b.LaDeThi,
                CauSai = b.TraLoi.Where(t => !t.Dung).OrderBy(t => t.ThuTu)
                    .Join(Db.CauHoi, t => t.CauHoiId, c => c.Id, (t, c) => new { t.ThuTu, c.NoiDung, t.DaChon, c.DapAnDung }).ToList(),
            }).ToListAsync();
        ViewBag.TienDo = await Db.Bai.AsNoTracking().Where(b => b.ChuDe!.KhoaId == khoa && b.DaXuatBan).OrderBy(b => b.So)
            .Select(b => new { b.So, b.Ten, TrangThai = Db.TienDoBai.Where(t => t.NguoiDungId == id && t.BaiId == b.Id).Select(t => (TrangThaiHoc?)t.TrangThai).FirstOrDefault() })
            .ToListAsync();

        // Điểm từng bài tập đã giao (thang 10); bài có tự luận chưa chấm thì báo chờ chấm
        var giao = await Db.BaiTapGiao.AsNoTracking().Where(b => b.DaGiao && b.DeThi == null && b.Bai!.ChuDe!.KhoaId == khoa)
            .OrderByDescending(b => b.NgayTao).Select(b => new { b.Id, b.Ten, b.HanNop, b.NoiDungJson, Bai = b.Bai!.Ten }).ToListAsync();
        var nop = await Db.BaiNopGiao.AsNoTracking().Where(n => n.NguoiDungId == id && giao.Select(g => g.Id).Contains(n.BaiTapGiaoId)).ToListAsync();
        ViewBag.DiemBaiTap = giao.Select(g =>
        {
            var cau = NoiDungBaiTap.Doc(g.NoiDungJson);
            var n = nop.FirstOrDefault(x => x.BaiTapGiaoId == g.Id);
            return new GvDiemBaiTap(g.Id, g.Ten, g.Bai, g.HanNop, n?.NopLuc, Diem10(n, cau), n is not null && n.DiemTuLuan is null && cau.Any(c => c.Kieu == "TL"));
        }).ToList();
        return View();
    }

    private static decimal? Diem10(BaiNopGiao? n, List<CauBaiTap> cau)
    {
        var tong = cau.Sum(c => c.TongDiem);
        if (n is null || tong <= 0 || (n.DiemTuLuan is null && cau.Any(c => c.Kieu == "TL"))) return null;
        return Math.Round((n.DiemTuDong + (n.DiemTuLuan ?? 0)) * 10 / tong, 1);
    }

    // Bảng điểm cả lớp (hoặc một học viên): tiến độ, bài tập đã nộp, điểm TB bài tập, thi thử, luyện tập, lần hoạt động cuối
    private async Task<List<GvHocVienRow>> BangDiem(string khoa, string? chiMot = null)
    {
        var now = DateTime.UtcNow;
        var ghiDanh = await Db.GhiDanh.AsNoTracking().Where(g => g.KhoaId == khoa && (chiMot == null || g.NguoiDungId == chiMot))
            .Select(g => new { g.NguoiDungId, g.NguoiDung!.HoTen, g.NguoiDung.Email, g.HanDen }).ToListAsync();
        var ids = ghiDanh.Select(g => g.NguoiDungId).ToList();
        var baiIds = await Db.Bai.Where(b => b.ChuDe!.KhoaId == khoa && b.DaXuatBan).Select(b => b.Id).ToListAsync();
        var deIds = await Db.DeThi.Where(d => d.KhoaId == khoa).Select(d => d.Id).ToListAsync();

        var daHoc = await Db.TienDoBai.Where(t => ids.Contains(t.NguoiDungId) && baiIds.Contains(t.BaiId) && t.TrangThai == TrangThaiHoc.DaHoc)
            .GroupBy(t => t.NguoiDungId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var hocCuoi = await Db.TienDoBai.Where(t => ids.Contains(t.NguoiDungId) && baiIds.Contains(t.BaiId))
            .GroupBy(t => t.NguoiDungId).Select(g => new { g.Key, T = g.Max(t => t.CapNhat) }).ToDictionaryAsync(x => x.Key, x => x.T);
        var baiLam = await Db.BaiLam.AsNoTracking().Where(b => ids.Contains(b.NguoiDungId)
                && ((!b.LaDeThi && baiIds.Contains(b.MaDe)) || (b.LaDeThi && deIds.Contains(b.MaDe))))
            .Select(b => new { b.NguoiDungId, b.LaDeThi, b.SoCau, b.SoDung, b.NopLuc }).ToListAsync();
        var giao = await Db.BaiTapGiao.AsNoTracking().Where(b => b.DaGiao && b.DeThi == null && b.Bai!.ChuDe!.KhoaId == khoa)
            .Select(b => new { b.Id, b.NoiDungJson }).ToListAsync();
        var cauGiao = giao.ToDictionary(g => g.Id, g => NoiDungBaiTap.Doc(g.NoiDungJson));
        var nop = await Db.BaiNopGiao.AsNoTracking().Where(n => ids.Contains(n.NguoiDungId) && cauGiao.Keys.Contains(n.BaiTapGiaoId)).ToListAsync();

        return ghiDanh.Select(g =>
        {
            var lam = baiLam.Where(b => b.NguoiDungId == g.NguoiDungId).ToList();
            var luyen = lam.Where(b => !b.LaDeThi).ToList();
            var thi = lam.Where(b => b.LaDeThi && b.SoCau > 0).ToList();
            var cuaToi = nop.Where(n => n.NguoiDungId == g.NguoiDungId).ToList();
            var diem = cuaToi.Select(n => Diem10(n, cauGiao[n.BaiTapGiaoId])).Where(d => d is not null).Select(d => d!.Value).ToList();
            var cuoi = new[] { lam.Select(b => (DateTime?)b.NopLuc).Max(), cuaToi.Select(n => (DateTime?)n.NopLuc).Max(), hocCuoi.TryGetValue(g.NguoiDungId, out var h) ? h : null }.Max();
            return new GvHocVienRow(g.NguoiDungId, g.HoTen, g.Email ?? "", g.HanDen, g.HanDen is null || g.HanDen >= now,
                daHoc.GetValueOrDefault(g.NguoiDungId), baiIds.Count,
                cuaToi.Select(n => n.BaiTapGiaoId).Distinct().Count(), giao.Count,
                diem.Count == 0 ? null : Math.Round(diem.Average(), 1),
                thi.Count == 0 ? null : Math.Round((decimal)thi.Average(b => b.SoDung * 10.0 / b.SoCau), 1),
                luyen.Sum(b => b.SoCau) == 0 ? null : (int)Math.Round(luyen.Sum(b => b.SoDung) * 100.0 / luyen.Sum(b => b.SoCau)),
                cuoi);
        }).ToList();
    }
}
