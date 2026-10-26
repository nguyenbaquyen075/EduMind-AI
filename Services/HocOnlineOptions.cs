namespace SuDia.Web.Services;

/// <summary>
/// Mục "HocOnline" trong appsettings.json: link Google Meet giáo viên dùng để dạy.
/// LinkChung dùng cho mọi buổi; TheoBai (khoá = mã bài) ghi đè link và thêm giờ học cho từng buổi.
/// </summary>
public class HocOnlineOptions
{
    public string LinkChung { get; set; } = "";
    public Dictionary<int, BuoiHocOnline> TheoBai { get; set; } = [];
}

public class BuoiHocOnline
{
    public string Link { get; set; } = "";
    public DateTime? ThoiGian { get; set; }
}
