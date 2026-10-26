using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Services;

// Dùng cùng định nghĩa ghi danh còn hiệu lực và ngày Việt Nam trên dashboard / báo cáo.
public class AdminThongKeService(AppDbContext db)
{
    public static readonly TimeZoneInfo MuiGio = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
    public static DateTime HomNay => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MuiGio).Date;
    public static DateTime NgayVietNam(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), MuiGio).Date;
    public static DateTime DauNgayUtc(DateTime ngay) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(ngay.Date, DateTimeKind.Unspecified), MuiGio);

    public Task<List<AdminKhoaRow>> KhoaAsync(string? khoa = null)
    {
        var now = DateTime.UtcNow;
        return db.Khoa.AsNoTracking().Where(k => khoa == null || k.Id == khoa).OrderBy(k => k.ThuTu)
            .Select(k => new AdminKhoaRow(k.Id, k.Ten, k.GiangVien == null ? null : k.GiangVien.HoTen,
                k.ChuDe.SelectMany(c => c.Bai).Count(), k.ChuDe.SelectMany(c => c.Bai).Count(b => b.DaXuatBan),
                db.GhiDanh.Count(g => g.KhoaId == k.Id), db.GhiDanh.Count(g => g.KhoaId == k.Id && (g.HanDen == null || g.HanDen >= now)))).ToListAsync();
    }

    public async Task<AdminBaoCaoViewModel> BaoCaoAsync(DateTime tu, DateTime den, string? khoa)
    {
        var dau = DauNgayUtc(tu); var cuoi = DauNgayUtc(den.AddDays(1));
        var baiIds = db.Bai.Where(b => b.ChuDe!.KhoaId == khoa).Select(b => b.Id);
        var deIds = db.DeThi.Where(d => d.KhoaId == khoa).Select(d => d.Id);
        var luyen = await db.BaiLam.AsNoTracking().Where(b => b.NopLuc >= dau && b.NopLuc < cuoi &&
            (khoa == null || (b.LaDeThi ? deIds.Contains(b.MaDe) : baiIds.Contains(b.MaDe))))
            .Select(b => new { b.NguoiDungId, b.NopLuc, b.SoCau, b.SoDung }).ToListAsync();
        var nop = await db.BaiNopGiao.AsNoTracking().Where(b => b.NopLuc >= dau && b.NopLuc < cuoi &&
            (khoa == null || b.BaiTapGiao!.Bai!.ChuDe!.KhoaId == khoa))
            .Select(b => new { b.NguoiDungId, b.NopLuc, b.DiemTuLuan }).ToListAsync();
        var dk = await db.Users.AsNoTracking().Where(u => u.NgayTao >= dau && u.NgayTao < cuoi).Select(u => u.NgayTao).ToListAsync();
        var luyenTheoNgay = luyen.GroupBy(b => NgayVietNam(b.NopLuc)).ToDictionary(g => g.Key, g => g.Count());
        var nopTheoNgay = nop.GroupBy(b => NgayVietNam(b.NopLuc)).ToDictionary(g => g.Key, g => g.Count());
        var dkTheoNgay = dk.GroupBy(NgayVietNam).ToDictionary(g => g.Key, g => g.Count());
        var soCau = luyen.Sum(b => (long)b.SoCau); var soDung = luyen.Sum(b => (long)b.SoDung);
        var ngay = Enumerable.Range(0, (den.Date - tu.Date).Days + 1)
            .Select(i => { var d = tu.Date.AddDays(i); return new AdminNgay(d, luyenTheoNgay.GetValueOrDefault(d), nopTheoNgay.GetValueOrDefault(d), dkTheoNgay.GetValueOrDefault(d)); }).ToList();
        return new AdminBaoCaoViewModel
        {
            Tu = tu, Den = den, KhoaId = khoa, DsKhoa = await db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync(),
            Khoa = await KhoaAsync(khoa), Ngay = ngay, SoBaiTuLuanChoCham = nop.Count(b => b.DiemTuLuan == null),
            SoLieu = [new("Học viên hoạt động", luyen.Select(b => b.NguoiDungId).Concat(nop.Select(b => b.NguoiDungId)).Distinct().Count().ToString(), "Có nộp bài trong thời gian chọn"),
                new("Lượt luyện tập", luyen.Count.ToString(), "Bài luyện tập và đề thi thử"),
                new("Bài tập đã nộp", nop.Count.ToString(), $"{nop.Count(b => b.DiemTuLuan == null)} chờ chấm tự luận"),
                new("Tỉ lệ câu đúng", soCau == 0 ? "—" : $"{Math.Round(soDung * 100d / soCau)}%", $"{soDung:N0}/{soCau:N0} câu luyện tập")],
        };
    }
}
