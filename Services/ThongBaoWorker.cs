using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Services.Email;
using SuDia.Web.Services.Sms;

namespace SuDia.Web.Services;

public sealed class ThongBaoWorker(IServiceScopeFactory scopes, ILogger<ThongBaoWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await GuiDenHanAsync(stoppingToken); } catch (Exception ex) { log.LogError(ex, "Lỗi worker thông báo"); }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
        }
    }
    private async Task GuiDenHanAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var smsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var now = DateTime.UtcNow;
        // Tiến trình có thể dừng sau khi đã nhận dòng. Thu hồi sau 5 phút để hàng đợi không bị kẹt mãi.
        await db.ThongBao.Where(t => t.TrangThai == TrangThaiThongBao.DangGui && t.DangGuiLuc < now.AddMinutes(-5) && t.SoLanThu < 4)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.TrangThai, TrangThaiThongBao.ChoGui).SetProperty(t => t.SoLanThu, t => t.SoLanThu + 1)
                .SetProperty(t => t.GuiTiepLuc, now).SetProperty(t => t.DangGuiLuc, (DateTime?)null), ct);
        await db.ThongBao.Where(t => t.TrangThai == TrangThaiThongBao.DangGui && t.DangGuiLuc < now.AddMinutes(-5) && t.SoLanThu >= 4)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.TrangThai, TrangThaiThongBao.Loi).SetProperty(t => t.SoLanThu, t => t.SoLanThu + 1)
                .SetProperty(t => t.LoiCuoi, "Quá thời gian gửi lại").SetProperty(t => t.DangGuiLuc, (DateTime?)null), ct);
        var ids = await db.ThongBao.Where(t => t.TrangThai == TrangThaiThongBao.ChoGui && t.GuiTiepLuc <= DateTime.UtcNow).OrderBy(t => t.GuiTiepLuc).Select(t => t.Id).Take(20).ToListAsync(ct);
        foreach (var id in ids)
        {
            var claimed = await db.ThongBao.Where(t => t.Id == id && t.TrangThai == TrangThaiThongBao.ChoGui).ExecuteUpdateAsync(s => s
                .SetProperty(t => t.TrangThai, TrangThaiThongBao.DangGui).SetProperty(t => t.DangGuiLuc, DateTime.UtcNow), ct);
            if (claimed == 0) continue;
            var item = await db.ThongBao.FindAsync([id], ct); if (item is null) continue;
            var result = item.Kenh == KenhThongBao.Sms
                ? await smsSender.GuiAsync(item.NguoiNhan, item.NoiDung, ct)
                : item.Kenh == KenhThongBao.Email
                    ? ChuyenKetQua(await emailSender.GuiAsync(item.NguoiNhan, "SuDia: xác nhận thanh toán", item.NoiDung, ct))
                    : new KetQuaGuiSms(false, null, "Kênh thông báo không hỗ trợ", false);
            var attempts = item.SoLanThu + 1;
            if (result.ThanhCong) { item.TrangThai = TrangThaiThongBao.DaGui; item.GuiLuc = DateTime.UtcNow; item.MaNhaCungCap = result.MaTin; }
            else { item.SoLanThu = attempts; item.LoiCuoi = result.Loi?[..Math.Min(500, result.Loi.Length)]; item.TrangThai = result.CoTheThuLai && attempts < 5 ? TrangThaiThongBao.ChoGui : TrangThaiThongBao.Loi; var delays = new[] { 30, 120, 600, 3600, 21600 }; item.GuiTiepLuc = DateTime.UtcNow.AddSeconds(delays[Math.Min(attempts - 1, 4)]); }
            item.DangGuiLuc = null;
            await db.SaveChangesAsync(ct);
        }
    }

    private static KetQuaGuiSms ChuyenKetQua(KetQuaGuiEmail kq) => new(kq.ThanhCong, kq.MaTin, kq.Loi, kq.CoTheThuLai);
}
