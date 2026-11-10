namespace SuDia.Web.Services.Email;

/// <summary>Điểm chặn an toàn trước khi có adapter SMTP hoặc nhà cung cấp email thật.</summary>
public sealed class ChuaCoEmailSender : IEmailSender
{
    public Task<KetQuaGuiEmail> GuiAsync(string email, string tieuDe, string noiDungHtml, CancellationToken ct) =>
        Task.FromResult(new KetQuaGuiEmail(false, null, "Chưa có đầu gửi email cho nhà cung cấp đã chọn", false));
}
