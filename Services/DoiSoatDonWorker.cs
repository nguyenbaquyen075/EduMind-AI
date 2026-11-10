using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Services;

/// <summary>Đối soát đơn chờ khi webhook bị lỡ; chỉ PayOS đã cấu hình mới được gọi.</summary>
public sealed class DoiSoatDonWorker(IServiceScopeFactory scopes, ILogger<DoiSoatDonWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DoiSoatAsync(stoppingToken); }
            catch (Exception ex) { log.LogError(ex, "Lỗi worker đối soát đơn"); }
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
        }
    }

    private async Task DoiSoatAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var payos = scope.ServiceProvider.GetRequiredService<PayOSService>();
        if (!payos.DaCauHinh) return;
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var donHang = scope.ServiceProvider.GetRequiredService<DonHangService>();
        var now = DateTime.UtcNow;
        var don = await db.DonHang.AsNoTracking().Where(d => d.TrangThai == TrangThaiDon.ChoThanhToan
                && d.NgayTao >= now.AddHours(-24) && d.NgayTao <= now.AddMinutes(-15))
            .OrderBy(d => d.NgayTao).Take(20).ToListAsync(ct);
        foreach (var d in don)
        {
            try
            {
                var tt = await payos.LayTrangThaiAsync(d.MaThanhToan, ct);
                if (tt.Status == "PAID" && tt.AmountPaid >= d.SoTien)
                    await donHang.XacNhanDaThanhToanAsync(d.Id, null, tt.AmountPaid, "Doi-Soat", ct);
                else if (tt.Status is "CANCELLED" or "EXPIRED") await donHang.HuyAsync(d.Id, ct);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                log.LogWarning(ex, "Chưa đối soát được đơn {MaThanhToan}; sẽ thử lại", d.MaThanhToan);
            }
        }

        var quaHan = await db.DonHang.Where(d => d.TrangThai == TrangThaiDon.ChoThanhToan && d.NgayTao < now.AddHours(-24)).Select(d => d.Id).Take(20).ToListAsync(ct);
        foreach (var id in quaHan)
        {
            var huy = await donHang.HuyAsync(id, ct);
            if (huy) db.NhatKyDuyet.Add(new NhatKyDuyet { DonHangId = id, TacNhan = "Doi-Soat", QuyetDinh = QuyetDinhDuyet.CanXemXet, LyDo = "Quá hạn đối soát" });
        }
        if (quaHan.Count > 0) await db.SaveChangesAsync(ct);
    }
}
