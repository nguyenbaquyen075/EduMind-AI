namespace SuDia.Web.Services.Sms;

/// <summary>Đầu gửi an toàn khi chưa chọn nhà cung cấp; tuyệt đối không gửi SMS thật.</summary>
public sealed class LogSmsSender(ILogger<LogSmsSender> log) : ISmsSender
{
    public Task<KetQuaGuiSms> GuiAsync(string soDienThoai, string noiDung, CancellationToken ct)
    {
        log.LogInformation("SMS development tới {SoDienThoai}: {NoiDung}", SoDienThoai.Che(soDienThoai), noiDung);
        return Task.FromResult(new KetQuaGuiSms(true, "development-log", null, false));
    }
}
