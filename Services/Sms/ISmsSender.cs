namespace SuDia.Web.Services.Sms;

public record KetQuaGuiSms(bool ThanhCong, string? MaTin, string? Loi, bool CoTheThuLai);
public interface ISmsSender { Task<KetQuaGuiSms> GuiAsync(string soDienThoai, string noiDung, CancellationToken ct); }
