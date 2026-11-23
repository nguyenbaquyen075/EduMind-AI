using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Services.Email;

namespace SuDia.Web.Services;

/// <summary>
/// Nhắc học viên: khoá sắp hết hạn (≤ 7 ngày) và bài tập sắp đến hạn nộp (≤ 48 giờ) mà chưa nộp.
/// Mỗi việc chỉ nhắc một lần (khoá chống trùng trong TinThongBao); gửi kèm email nếu có đầu gửi.
/// </summary>
public class NhacHanService(AppDbContext db, TinThongBaoService tin, IEmailSender email, ILogger<NhacHanService> log)
{
    public async Task<int> ChayAsync(CancellationToken ct = default)
    {
        var bayGio = DateTime.UtcNow;
        var soTin = 0;

        // 1. Khoá sắp hết hạn
        var sapHet = await db.GhiDanh.AsNoTracking()
            .Where(g => g.HanDen != null && g.HanDen > bayGio && g.HanDen <= bayGio.AddDays(7))
            .Select(g => new { g.NguoiDungId, g.KhoaId, KhoaTen = g.Khoa!.Ten, HanDen = g.HanDen!.Value, g.NguoiDung!.Email, g.NguoiDung.HoTen }).ToListAsync(ct);
        foreach (var g in sapHet)
        {
            var khoa = $"hanhoc:{g.KhoaId}:{g.HanDen:yyyyMMdd}";
            if (await db.TinThongBao.AnyAsync(t => t.NguoiDungId == g.NguoiDungId && t.KhoaDuyNhat == khoa, ct)) continue;
            var ngayCon = Math.Max(1, (int)Math.Ceiling((g.HanDen - bayGio).TotalDays));
            var noiDung = $"Khoá “{g.KhoaTen}” hết hạn lúc {g.HanDen.ToLocalTime():HH:mm dd/MM/yyyy} (còn {ngayCon} ngày). Gia hạn để không bị gián đoạn việc học.";
            await tin.GuiAsync([g.NguoiDungId], LoaiTin.SapHetHan, $"Khoá {g.KhoaTen} sắp hết hạn", noiDung, "/ThanhToan/Goi", khoa, ct);
            await GuiEmailAsync(g.Email, g.HoTen, $"Khoá {g.KhoaTen} sắp hết hạn", noiDung, ct);
            soTin++;
        }

        // 2. Bài tập sắp đến hạn nộp, học viên chưa nộp
        var sapDen = await db.BaiTapGiao.AsNoTracking()
            .Where(b => b.DaGiao && b.HanNop != null && b.HanNop > bayGio && b.HanNop <= bayGio.AddHours(48) && b.Bai!.DaXuatBan && (b.DeThi == null || b.DeThi.DangMo))
            .Select(b => new { b.Id, b.Ten, HanNop = b.HanNop!.Value, KhoaId = b.Bai!.ChuDe!.KhoaId }).ToListAsync(ct);
        foreach (var b in sapDen)
        {
            var khoa = $"hannop:{b.Id}";
            var chuaNop = await db.GhiDanh.AsNoTracking()
                .Where(g => g.KhoaId == b.KhoaId && (g.HanDen == null || g.HanDen >= bayGio)
                    && !db.BaiNopGiao.Any(n => n.BaiTapGiaoId == b.Id && n.NguoiDungId == g.NguoiDungId)
                    && !db.TinThongBao.Any(t => t.NguoiDungId == g.NguoiDungId && t.KhoaDuyNhat == khoa))
                .Select(g => new { g.NguoiDungId, g.NguoiDung!.Email, g.NguoiDung.HoTen }).ToListAsync(ct);
            if (chuaNop.Count == 0) continue;
            var noiDung = $"Bài “{b.Ten}” hạn nộp {b.HanNop.ToLocalTime():HH:mm dd/MM/yyyy}. Bạn chưa nộp bài.";
            await tin.GuiAsync(chuaNop.Select(x => x.NguoiDungId), LoaiTin.SapDenHan, $"Sắp đến hạn nộp: {b.Ten}", noiDung, $"/BaiTap/Lam/{b.Id}", khoa, ct);
            foreach (var x in chuaNop) await GuiEmailAsync(x.Email, x.HoTen, $"Sắp đến hạn nộp: {b.Ten}", noiDung, ct);
            soTin += chuaNop.Count;
        }
        if (soTin > 0) log.LogInformation("Nhắc hạn: đã tạo {So} thông báo", soTin);
        return soTin;
    }

    private async Task GuiEmailAsync(string? dia, string? ten, string tieuDe, string noiDung, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dia)) return;
        try
        {
            var html = $"<p>Chào {System.Net.WebUtility.HtmlEncode(ten ?? "bạn")},</p><p>{System.Net.WebUtility.HtmlEncode(noiDung)}</p><p>— Sử·Địa</p>";
            await email.GuiAsync(dia, "SuDia: " + tieuDe, html, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "Gửi email nhắc hạn lỗi"); }
    }
}

/// <summary>Chạy nhắc hạn khi khởi động (sau 1 phút) rồi mỗi 6 giờ.</summary>
public sealed class NhacHanWorker(IServiceScopeFactory scopes, ILogger<NhacHanWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<NhacHanService>().ChayAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Lỗi nhắc hạn"); }
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
        }
    }
}
