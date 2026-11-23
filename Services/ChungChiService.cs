using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Services;

/// <summary>Cấp chứng chỉ khi học viên đã học hết mọi bài đã xuất bản của khoá (khoá đã ghi danh).</summary>
public class ChungChiService(AppDbContext db, TinThongBaoService tin)
{
    private const string BangMa = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<List<KhoaChungChi>> DanhSachAsync(string uid, CancellationToken ct = default)
    {
        var khoa = await db.GhiDanh.AsNoTracking().Where(g => g.NguoiDungId == uid).Select(g => new { g.KhoaId, g.Khoa!.Ten, g.Khoa.ThuTu }).OrderBy(g => g.ThuTu).ToListAsync(ct);
        var ids = khoa.Select(k => k.KhoaId).ToList();
        var tong = await db.Bai.AsNoTracking().Where(b => b.DaXuatBan && ids.Contains(b.ChuDe!.KhoaId)).GroupBy(b => b.ChuDe!.KhoaId).Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct);
        var daHoc = await db.TienDoBai.AsNoTracking().Where(t => t.NguoiDungId == uid && t.TrangThai == TrangThaiHoc.DaHoc && t.Bai!.DaXuatBan && ids.Contains(t.Bai.ChuDe!.KhoaId))
            .GroupBy(t => t.Bai!.ChuDe!.KhoaId).Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct);
        var cap = await db.ChungChi.AsNoTracking().Where(c => c.NguoiDungId == uid).ToListAsync(ct);
        return khoa.Select(k => new KhoaChungChi(k.KhoaId, k.Ten, daHoc.FirstOrDefault(x => x.Key == k.KhoaId)?.N ?? 0, tong.FirstOrDefault(x => x.Key == k.KhoaId)?.N ?? 0,
            cap.FirstOrDefault(c => c.KhoaId == k.KhoaId))).ToList();
    }

    public async Task<(ChungChi? Cc, string? Loi)> CapAsync(string uid, string khoaId, CancellationToken ct = default)
    {
        var ds = await DanhSachAsync(uid, ct);
        var k = ds.FirstOrDefault(x => x.KhoaId == khoaId);
        if (k is null) return (null, "Bạn chưa ghi danh khoá này.");
        if (k.DaCap is not null) return (k.DaCap, null);
        if (!k.DuDieuKien) return (null, $"Bạn mới học {k.DaHoc}/{k.Tong} bài. Học hết các bài của khoá để nhận chứng chỉ.");
        var nd = await db.Users.AsNoTracking().FirstAsync(u => u.Id == uid, ct);
        for (var lan = 0; lan < 10; lan++)
        {
            var cc = new ChungChi { MaXacThuc = TaoMa(), NguoiDungId = uid, KhoaId = khoaId, HoTen = nd.HoTen, TenKhoa = k.Ten, SoBai = k.Tong };
            db.ChungChi.Add(cc);
            try
            {
                await db.SaveChangesAsync(ct);
                await tin.GuiAsync([uid], LoaiTin.ChungChi, $"Chúc mừng! Bạn hoàn thành khoá {k.Ten}", "Chứng chỉ đã sẵn sàng để xem và in.", "/ChungChi", ct: ct);
                return (cc, null);
            }
            catch (DbUpdateException)
            {
                db.Entry(cc).State = EntityState.Detached;
                var co = await db.ChungChi.AsNoTracking().FirstOrDefaultAsync(c => c.NguoiDungId == uid && c.KhoaId == khoaId, ct);
                if (co is not null) return (co, null);          // bấm hai lần cùng lúc
            }
        }
        return (null, "Chưa cấp được chứng chỉ. Vui lòng thử lại.");
    }

    private static string TaoMa() => string.Create(10, 0, (s, _) => { for (var i = 0; i < s.Length; i++) s[i] = BangMa[RandomNumberGenerator.GetInt32(BangMa.Length)]; });

    public static string DinhDangMa(string ma) => ma.Length == 10 ? $"{ma[..5]}-{ma[5..]}" : ma;
}
