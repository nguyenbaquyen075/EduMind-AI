namespace SuDia.Web.Services.Sms;

/// <summary>Chặn gửi nhầm ở môi trường thật cho đến khi chọn và cài đầu gửi nhà cung cấp.</summary>
public sealed class ChuaCoSmsSender : ISmsSender
{
    public Task<KetQuaGuiSms> GuiAsync(string soDienThoai, string noiDung, CancellationToken ct) =>
        Task.FromResult(new KetQuaGuiSms(false, null, "Chưa có đầu gửi SMS cho nhà cung cấp đã chọn", false));
}
