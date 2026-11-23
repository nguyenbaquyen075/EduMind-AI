namespace SuDia.Web.Services.Email;

/// <summary>Chỉ dùng ở Development để kiểm tra toàn bộ luồng mà không gửi thư thật.</summary>
public sealed class LogEmailSender(ILogger<LogEmailSender> log) : IEmailSender
{
    public Task<KetQuaGuiEmail> GuiAsync(string email, string tieuDe, string noiDungHtml, CancellationToken ct)
    {
        log.LogInformation("Email development tới {Email}, tiêu đề {TieuDe}", email, tieuDe);
        return Task.FromResult(new KetQuaGuiEmail(true, "development-log", null, false));
    }
}
