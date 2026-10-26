using SuDia.Web.Models;

namespace SuDia.Web.Services;

public class DemoAiService : IAiService
{
    private static readonly (string Q, string Mon, string Text, string Nguon)[] Bank =
    {
        ("Chiến dịch Điện Biên Phủ diễn ra khi nào?", "Lịch sử 12",
         "Chiến dịch diễn ra từ ngày 13/3 đến ngày 7/5/1954, chia làm ba đợt tấn công và kết thúc thắng lợi khi toàn bộ tập đoàn cứ điểm bị tiêu diệt.",
         "¹ Lịch sử 12 · Bài 7 · Mục 3 — Chiến dịch Điện Biên Phủ"),
        ("Cách mạng tháng Tám thành công năm nào?", "Lịch sử 12",
         "Cách mạng tháng Tám thành công năm 1945. Ngày 2/9/1945, Chủ tịch Hồ Chí Minh đọc Tuyên ngôn Độc lập, khai sinh nước Việt Nam Dân chủ Cộng hoà.",
         "¹ Lịch sử 12 · Bài 6 — Cách mạng tháng Tám năm 1945"),
        ("Vì sao Tây Nguyên trồng nhiều cà phê?", "Địa lí 12",
         "Tây Nguyên có đất badan màu mỡ tập trung trên các cao nguyên, cùng khí hậu cận xích đạo với mùa mưa và mùa khô rõ rệt, thuận lợi cho phơi sấy và bảo quản.",
         "¹ Địa lí 12 · Vùng Tây Nguyên — Thế mạnh cây công nghiệp"),
    };

    private static string Norm(string s) => s.Trim().TrimEnd('?', '.', '!', ' ').ToLowerInvariant();

    public Task<HoiAiResponse> HoiAsync(HoiAiRequest request, CancellationToken ct = default)
    {
        var hit = Bank.FirstOrDefault(b => Norm(b.Q) == Norm(request.CauHoi));
        if (hit.Q is not null)
            return Task.FromResult(new HoiAiResponse("Trợ lý · " + hit.Mon, hit.Text, "1", hit.Nguon, false));

        return Task.FromResult(new HoiAiResponse(
            "Bản xem thử",
            "Ở bản xem thử, trợ lý chỉ trả lời các câu gợi ý. Khi vào học, trợ lý sẽ tra câu hỏi của bạn trong tài liệu khoá và trả lời kèm nguồn.",
            "", "Bắt đầu học miễn phí để hỏi mọi câu trong bài học", true));
    }
}
