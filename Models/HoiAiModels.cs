namespace SuDia.Web.Models;

/// <param name="LichSu">2–3 lượt hỏi đáp gần nhất, để hiểu câu hỏi nối tiếp ("còn đợt 2 thì sao?")</param>
public record HoiAiRequest(string CauHoi, string? KhoaId, IReadOnlyList<LuotHoi>? LichSu = null);

public record LuotHoi(string CauHoi, string TraLoi);

public record HoiAiResponse(string Nhan, string NoiDung, string DanhDau, string Nguon, bool TuChoi);
