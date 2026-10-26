namespace SuDia.Web.Models;

public static class GioiHanHoiAi
{
    public const int CauHoi = 5000;
}

/// <param name="LichSu">2–3 lượt hỏi đáp gần nhất, để hiểu câu hỏi nối tiếp ("còn đợt 2 thì sao?")</param>
public record HoiAiRequest(string CauHoi, string? KhoaId, IReadOnlyList<LuotHoi>? LichSu = null);

public record LuotHoi(string CauHoi, string TraLoi);

public record HoiAiResponse(string Nhan, string NoiDung, string DanhDau, string Nguon, bool TuChoi);

public class HoiAiViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public IReadOnlyList<KhoaHocChinh> Khoa { get; init; } = [];
    public string? KhoaId { get; init; }
    public bool BanXemThu { get; init; }
    public string CauHoiBanDau { get; init; } = "";
}
