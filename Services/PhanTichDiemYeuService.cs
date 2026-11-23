using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Services;

/// <summary>Điểm mạnh / yếu theo bài của một học viên, tính từ mọi câu đã trả lời (không dùng AI).</summary>
public record DiemTheoBai(string KhoaId, string Khoa, int BaiId, int BaiSo, string BaiTen, int SoCau, int SoDung, DateTime LanCuoi)
{
    public double TiLe => SoCau == 0 ? 0 : SoDung * 100.0 / SoCau;
    public string Nhan => $"Bài {BaiSo}. {BaiTen}";
}

/// <summary>Bài cả lớp hay sai: gộp câu trả lời của mọi học viên trong khoá.</summary>
public record BaiLopHaySai(int BaiId, int BaiSo, string BaiTen, int SoHocVien, int SoCau, int SoDung)
{
    public double TiLe => SoCau == 0 ? 0 : SoDung * 100.0 / SoCau;
}

public class PhanTichDiemYeuService(AppDbContext db)
{
    public const int SoCauToiThieu = 5;          // ít hơn thì chưa đủ dữ liệu để kết luận
    public const double NguongYeu = 60, NguongManh = 80;
    private const int SoNgay = 120;              // chỉ tính các lần làm gần đây

    public async Task<List<DiemTheoBai>> TheoBaiAsync(string uid, IReadOnlyCollection<string>? khoaIds = null, CancellationToken ct = default)
    {
        var tu = DateTime.UtcNow.AddDays(-SoNgay);
        var dong = await (from t in db.CauTraLoi
                          join l in db.BaiLam on t.BaiLamId equals l.Id
                          join c in db.CauHoi on t.CauHoiId equals c.Id
                          where l.NguoiDungId == uid && l.NopLuc >= tu && c.Bai!.DaXuatBan && (khoaIds == null || khoaIds.Contains(c.Bai.ChuDe!.KhoaId))
                          select new { c.BaiId, t.Dung, l.NopLuc }).AsNoTracking().ToListAsync(ct);
        if (dong.Count == 0) return [];
        var ids = dong.Select(d => d.BaiId).Distinct().ToList();
        var bai = await db.Bai.AsNoTracking().Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.So, b.Ten, KhoaId = b.ChuDe!.KhoaId, Khoa = b.ChuDe.Khoa!.Ten }).ToDictionaryAsync(b => b.Id, ct);
        return dong.GroupBy(d => d.BaiId).Select(g => new DiemTheoBai(bai[g.Key].KhoaId, bai[g.Key].Khoa, g.Key, bai[g.Key].So, bai[g.Key].Ten,
                g.Count(), g.Count(x => x.Dung), g.Max(x => x.NopLuc)))
            .OrderBy(d => d.TiLe).ThenByDescending(d => d.SoCau).ToList();
    }

    public static IEnumerable<DiemTheoBai> Yeu(IEnumerable<DiemTheoBai> ds) => ds.Where(d => d.SoCau >= SoCauToiThieu && d.TiLe < NguongYeu);
    public static IEnumerable<DiemTheoBai> Manh(IEnumerable<DiemTheoBai> ds) => ds.Where(d => d.SoCau >= SoCauToiThieu && d.TiLe >= NguongManh).OrderByDescending(d => d.TiLe);

    /// <summary>Đề ôn riêng: câu của các bài yếu nhất, ưu tiên câu lần trước làm sai, rồi câu chưa làm; xen kẽ giữa các bài.</summary>
    public async Task<List<CauSaiOnTap>> DeOnAsync(string uid, IReadOnlyCollection<string> khoaConHan, int soCau = 10, CancellationToken ct = default)
    {
        var ds = await TheoBaiAsync(uid, khoaConHan, ct);
        var chon = Yeu(ds).Take(3).ToList();
        if (chon.Count == 0) chon = ds.Where(d => d.TiLe < 100).Take(3).ToList();   // chưa đủ dữ liệu: lấy các bài làm kém nhất
        if (chon.Count == 0) return [];
        var baiIds = chon.Select(c => c.BaiId).ToList();
        var traLoi = await (from t in db.CauTraLoi join l in db.BaiLam on t.BaiLamId equals l.Id
                            where l.NguoiDungId == uid select new { t.CauHoiId, t.Dung, t.DaChon, l.NopLuc }).AsNoTracking().ToListAsync(ct);
        var lanCuoi = traLoi.GroupBy(t => t.CauHoiId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.NopLuc).First());
        var cau = await db.CauHoi.AsNoTracking().Where(c => baiIds.Contains(c.BaiId))
            .Select(c => new { c.Id, c.BaiId, c.NoiDung, c.PhuongAn, c.DapAnDung, c.GiaiThich }).ToListAsync(ct);
        int Uu(int cauId) => !lanCuoi.TryGetValue(cauId, out var t) ? 1 : t.Dung ? 2 : 0;      // sai → chưa làm → đúng
        var theoBai = chon.Select(b => cau.Where(c => c.BaiId == b.BaiId).OrderBy(c => Uu(c.Id)).ThenBy(c => c.Id).ToList()).ToList();
        var kq = new List<CauSaiOnTap>();
        for (var vong = 0; kq.Count < soCau && vong < 50; vong++)
            foreach (var (b, ds2) in chon.Zip(theoBai))
            {
                if (vong >= ds2.Count || kq.Count >= soCau) continue;
                var c = ds2[vong];
                lanCuoi.TryGetValue(c.Id, out var t);
                kq.Add(new CauSaiOnTap(c.Id, b.Khoa, b.Nhan, c.BaiId, c.NoiDung, c.PhuongAn, c.DapAnDung, t is { Dung: false } ? t.DaChon : null, c.GiaiThich, t?.NopLuc ?? DateTime.UtcNow));
            }
        return kq;
    }

    /// <summary>Các bài cả lớp hay sai nhất trong một khoá (cần đủ số câu để kết luận).</summary>
    public async Task<List<BaiLopHaySai>> BaiLopHaySaiAsync(string khoaId, int top = 5, int soCauToiThieu = 10, CancellationToken ct = default)
    {
        var tu = DateTime.UtcNow.AddDays(-SoNgay);
        var dong = await (from t in db.CauTraLoi
                          join l in db.BaiLam on t.BaiLamId equals l.Id
                          join c in db.CauHoi on t.CauHoiId equals c.Id
                          where l.NopLuc >= tu && c.Bai!.ChuDe!.KhoaId == khoaId && c.Bai.DaXuatBan
                          select new { c.BaiId, l.NguoiDungId, t.Dung }).AsNoTracking().ToListAsync(ct);
        var ids = dong.Select(d => d.BaiId).Distinct().ToList();
        var bai = await db.Bai.AsNoTracking().Where(b => ids.Contains(b.Id)).Select(b => new { b.Id, b.So, b.Ten }).ToDictionaryAsync(b => b.Id, ct);
        return dong.GroupBy(d => d.BaiId).Select(g => new BaiLopHaySai(g.Key, bai[g.Key].So, bai[g.Key].Ten, g.Select(x => x.NguoiDungId).Distinct().Count(), g.Count(), g.Count(x => x.Dung)))
            .Where(b => b.SoCau >= soCauToiThieu).OrderBy(b => b.TiLe).Take(top).ToList();
    }
}
