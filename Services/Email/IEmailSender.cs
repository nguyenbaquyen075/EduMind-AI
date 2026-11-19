namespace SuDia.Web.Services.Email;

public record KetQuaGuiEmail(bool ThanhCong, string? MaTin, string? Loi, bool CoTheThuLai);

/// <summary>Điểm cắm cho nhà cung cấp email sau này; worker không phụ thuộc SMTP cụ thể.</summary>
public interface IEmailSender
{
    Task<KetQuaGuiEmail> GuiAsync(string email, string tieuDe, string noiDungHtml, CancellationToken ct);
}
