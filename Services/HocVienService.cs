using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Services;

/// <summary>Số liệu riêng của một học viên: việc cần làm, buổi học sắp tới, điểm, câu sai.</summary>
public class HocVienService(AppDbContext db)
{
    private IQueryable<string> KhoaConHan(string uid) =>
        db.GhiDanh.Where(g => g.NguoiDungId == uid && (g.HanDen == null || g.HanDen >= DateTime.UtcNow)).Select(g => g.KhoaId);

    // Bài tập và bài kiểm tra đã giao, bài học đã xuất bản, khoá còn hạn
    private IQueryable<BaiTapGiao> BaiTapCuaToi(string uid)
    {
        var khoa = KhoaConHan(uid);
        return db.BaiTapGiao.Where(b => b.DaGiao && b.Bai!.DaXuatBan && khoa.Contains(b.Bai.ChuDe!.KhoaId)
            && (b.DeThi == null || b.DeThi.DangMo));
    }

    public async Task<List<BaiTapCanLam>> ViecCanLam(string uid)
    {
        var now = DateTime.UtcNow;
        var ds = await BaiTapCuaToi(uid).AsNoTracking()
            .Where(b => (b.HanNop == null || b.HanNop >= now) && !db.BaiNopGiao.Any(n => n.BaiTapGiaoId == b.Id && n.NguoiDungId == uid))
            .Select(b => new BaiTapCanLam(b.Id, b.Ten, b.Bai!.ChuDe!.Khoa!.Ten, b.Bai.Ten, b.HanNop, b.DeThi != null))
            .ToListAsync();
        return ds.OrderBy(b => b.HanNop ?? DateTime.MaxValue).ThenBy(b => b.Ten).Take(6).ToList();
    }

    public async Task<List<BuoiHocSapToi>> BuoiSapToi(string uid)
    {
        var tu = DateTime.UtcNow.AddHours(-2);
        var khoa = KhoaConHan(uid);
        return await db.BuoiHoc.AsNoTracking()
            .Where(b => b.ThoiGian != null && b.ThoiGian >= tu && b.Link != "" && b.Bai!.DaXuatBan && khoa.Contains(b.Bai.ChuDe!.KhoaId))
            .OrderBy(b => b.ThoiGian).Take(4)
            .Select(b => new BuoiHocSapToi(b.Bai!.ChuDe!.Khoa!.Ten, b.Bai.Ten, b.ThoiGian!.Value, b.Link)).ToListAsync();
    }

    public async Task<List<KhoaSapHetHan>> SapHetHan(string uid)
    {
        var now = DateTime.UtcNow; var den = now.AddDays(14);
        return await db.GhiDanh.AsNoTracking().Where(g => g.NguoiDungId == uid && g.HanDen != null && g.HanDen >= now && g.HanDen <= den)
            .OrderBy(g => g.HanDen).Select(g => new KhoaSapHetHan(g.KhoaId, g.Khoa!.Ten, g.HanDen!.Value)).ToListAsync();
    }

    public async Task<DiemCuaToiViewModel> BangDiem(string uid, MenuTaiKhoan menu)
    {
        var giao = await BaiTapCuaToi(uid).AsNoTracking().OrderByDescending(b => b.NgayTao)
            .Select(b => new { b.Id, b.Ten, Khoa = b.Bai!.ChuDe!.Khoa!.Ten, Bai = b.Bai.Ten, b.HanNop, b.NoiDungJson, LaKiemTra = b.DeThi != null })
            .ToListAsync();
        var ids = giao.Select(g => g.Id).ToList();
        var nop = await db.BaiNopGiao.AsNoTracking().Where(n => n.NguoiDungId == uid && ids.Contains(n.BaiTapGiaoId)).ToListAsync();
        var baiTap = giao.Select(g =>
        {
            var cau = NoiDungBaiTap.Doc(g.NoiDungJson);
            var n = nop.FirstOrDefault(x => x.BaiTapGiaoId == g.Id);
            var coTuLuan = cau.Any(c => c.Kieu == "TL");
            var tong = cau.Sum(c => c.TongDiem);
            decimal? diem = n is null || tong <= 0 || (coTuLuan && n.DiemTuLuan is null) ? null
                : Math.Round((n.DiemTuDong + (n.DiemTuLuan ?? 0)) * 10 / tong, 1);
            return new DiemBaiTapCuaToi(g.Id, g.Ten, g.Khoa, g.Bai, g.HanNop, n?.NopLuc, diem, n is not null && coTuLuan && n.DiemTuLuan is null, g.LaKiemTra);
        }).ToList();

        // Đề thi thử (BaiLam.MaDe = DeThi.Id) và luyện tập theo bài (BaiLam.MaDe = Bai.Id)
        var khoa = KhoaConHan(uid);
        var tenDe = await db.DeThi.AsNoTracking().Where(d => khoa.Contains(d.KhoaId)).Select(d => new { d.Id, Khoa = d.Khoa!.Ten }).ToDictionaryAsync(d => d.Id, d => d.Khoa);
        var tenBai = await db.Bai.AsNoTracking().Where(b => khoa.Contains(b.ChuDe!.KhoaId)).Select(b => new { b.Id, Khoa = b.ChuDe!.Khoa!.Ten }).ToDictionaryAsync(b => b.Id, b => b.Khoa);
        var lam = await db.BaiLam.AsNoTracking().Where(b => b.NguoiDungId == uid).OrderByDescending(b => b.NopLuc).Take(200)
            .Select(b => new { b.MaDe, b.LaDeThi, b.TenDe, b.SoDung, b.SoCau, b.NopLuc }).ToListAsync();
        var luot = lam.Where(b => b.LaDeThi ? tenDe.ContainsKey(b.MaDe) : tenBai.ContainsKey(b.MaDe))
            .Select(b => new LuotLamCuaToi(b.TenDe, b.LaDeThi ? tenDe[b.MaDe] : tenBai[b.MaDe], b.LaDeThi, b.SoDung, b.SoCau, b.NopLuc)).ToList();

        var coDiem = baiTap.Where(b => b.Diem is not null).Select(b => b.Diem!.Value).ToList();
        var thi = luot.Where(l => l.LaDeThi && l.SoCau > 0).ToList();
        var luyen = luot.Where(l => !l.LaDeThi).ToList();
        var daHoc = await db.TienDoBai.CountAsync(t => t.NguoiDungId == uid && t.TrangThai == TrangThaiHoc.DaHoc);
        return new DiemCuaToiViewModel
        {
            Menu = menu, BaiTap = baiTap, LuotLam = luot.Take(30).ToList(),
            DiemBaiTapTb = coDiem.Count == 0 ? null : Math.Round(coDiem.Average(), 1),
            DiemThiTb = thi.Count == 0 ? null : Math.Round(thi.Average(t => t.Diem10), 1),
            TiLeDungLuyenTap = luyen.Sum(l => l.SoCau) == 0 ? null : (int)Math.Round(luyen.Sum(l => l.SoDung) * 100.0 / luyen.Sum(l => l.SoCau)),
            HuyHieu =
            [
                new("Khởi đầu", "Học xong bài đầu tiên", daHoc >= 1),
                new("Chăm chỉ", "Học xong 10 bài", daHoc >= 10),
                new("Đúng hạn", "Nộp 5 bài tập trước hạn", baiTap.Count(b => b.NopLuc is DateTime nl && (b.HanNop is null || nl <= b.HanNop)) >= 5),
                new("Điểm cao", "Một bài tập đạt từ 9 điểm", coDiem.Any(d => d >= 9)),
                new("Thi thử", "Hoàn thành một đề thi thử", thi.Count >= 1),
                new("Luyện nhiều", "Làm 100 câu luyện tập", luyen.Sum(l => l.SoCau) >= 100),
            ],
        };
    }

    // Câu mà lần trả lời gần nhất của học viên là sai
    public async Task<List<CauSaiOnTap>> CauSai(string uid, string? khoaId)
    {
        var khoa = KhoaConHan(uid);
        var traLoi = await db.CauTraLoi.AsNoTracking()
            .Join(db.BaiLam.Where(b => b.NguoiDungId == uid), t => t.BaiLamId, b => b.Id, (t, b) => new { t.CauHoiId, t.Dung, t.DaChon, b.NopLuc })
            .ToListAsync();
        var sai = traLoi.GroupBy(t => t.CauHoiId).Select(g => g.OrderByDescending(x => x.NopLuc).First()).Where(t => !t.Dung).ToList();
        var ids = sai.Select(s => s.CauHoiId).ToList();
        var cau = await db.CauHoi.AsNoTracking().Where(c => ids.Contains(c.Id) && c.Bai!.DaXuatBan && khoa.Contains(c.Bai.ChuDe!.KhoaId)
                && (khoaId == null || c.Bai.ChuDe.KhoaId == khoaId))
            .Select(c => new { c.Id, c.NoiDung, c.PhuongAn, c.DapAnDung, c.GiaiThich, c.BaiId, Bai = c.Bai!.Ten, Khoa = c.Bai.ChuDe!.Khoa!.Ten })
            .ToListAsync();
        return cau.Select(c => { var s = sai.First(x => x.CauHoiId == c.Id);
                return new CauSaiOnTap(c.Id, c.Khoa, c.Bai, c.BaiId, c.NoiDung, c.PhuongAn, c.DapAnDung, s.DaChon, c.GiaiThich, s.NopLuc); })
            .OrderByDescending(c => c.LanCuoi).ToList();
    }

    // Các lượt làm đề thi thử (kiểu đề cũ chấm theo số câu đúng)
    public async Task<List<LuotLamCuaToi>> LichSuThi(string uid)
    {
        var tenKhoa = await db.DeThi.AsNoTracking().Select(d => new { d.Id, Khoa = d.Khoa!.Ten }).ToDictionaryAsync(d => d.Id, d => d.Khoa);
        var lam = await db.BaiLam.AsNoTracking().Where(b => b.NguoiDungId == uid && b.LaDeThi).OrderByDescending(b => b.NopLuc).Take(50)
            .Select(b => new { b.MaDe, b.TenDe, b.SoDung, b.SoCau, b.NopLuc }).ToListAsync();
        return lam.Select(b => new LuotLamCuaToi(b.TenDe, tenKhoa.GetValueOrDefault(b.MaDe, ""), true, b.SoDung, b.SoCau, b.NopLuc)).ToList();
    }

    public async Task<List<(string Id, string Ten)>> KhoaCuaToi(string uid)
    {
        var khoa = KhoaConHan(uid);
        return (await db.Khoa.AsNoTracking().Where(k => khoa.Contains(k.Id)).OrderBy(k => k.ThuTu).Select(k => new { k.Id, k.Ten }).ToListAsync())
            .Select(k => (k.Id, k.Ten)).ToList();
    }
}
