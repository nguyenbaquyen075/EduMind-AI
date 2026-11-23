using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Services;

/// <summary>Tạo và đọc thông báo trong web. Lỗi khi tạo tin không được làm hỏng thao tác chính nên chỉ ghi log.</summary>
public class TinThongBaoService(AppDbContext db, ILogger<TinThongBaoService> log)
{
    public async Task GuiAsync(IEnumerable<string?> nguoiNhan, string loai, string tieuDe, string noiDung, string link, string? khoaDuyNhat = null, CancellationToken ct = default)
    {
        try
        {
            var ids = nguoiNhan.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();
            if (ids.Count == 0) return;
            if (khoaDuyNhat is not null)
            {
                var daCo = await db.TinThongBao.Where(t => t.KhoaDuyNhat == khoaDuyNhat && ids.Contains(t.NguoiDungId)).Select(t => t.NguoiDungId).ToListAsync(ct);
                ids = ids.Except(daCo).ToList();
            }
            foreach (var id in ids)
                db.TinThongBao.Add(new TinThongBao
                {
                    NguoiDungId = id, Loai = loai, TieuDe = Cat(tieuDe, 200), NoiDung = Cat(noiDung, 300), Link = link, KhoaDuyNhat = khoaDuyNhat,
                });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Không tạo được thông báo {Loai}", loai);
            foreach (var e in db.ChangeTracker.Entries<TinThongBao>().Where(e => e.State == EntityState.Added).ToList()) e.State = EntityState.Detached;
        }
    }

    public Task<int> DemChuaDocAsync(string uid, CancellationToken ct = default) =>
        db.TinThongBao.CountAsync(t => t.NguoiDungId == uid && !t.DaDoc, ct);

    private static string Cat(string s, int n) { s = (s ?? "").Trim(); return s.Length <= n ? s : s[..(n - 1)] + "…"; }
}
