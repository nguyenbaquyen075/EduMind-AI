namespace SuDia.Web.Data;

// ======================================================= Thông báo trong web (chuông)
public static class LoaiTin
{
    public const string CauHoiMoi = "CauHoiMoi";     // giảng viên: học viên hỏi trong lớp
    public const string BaiMoiCuaGv = "BaiMoiCuaGv"; // học viên: giảng viên đăng bài trong lớp
    public const string TraLoi = "TraLoi";           // có người trả lời bài của bạn
    public const string DapAn = "DapAn";             // trả lời của bạn được đánh dấu đáp án
    public const string BaiTapMoi = "BaiTapMoi";     // giảng viên giao bài tập / kiểm tra
    public const string SapHetHan = "SapHetHan";     // ghi danh sắp hết hạn
    public const string SapDenHan = "SapDenHan";     // bài tập sắp đến hạn nộp mà chưa nộp
    public const string ChungChi = "ChungChi";       // được cấp chứng chỉ hoàn thành khoá
}

public class TinThongBao
{
    public long Id { get; set; }
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public string Loai { get; set; } = "";
    public string TieuDe { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public string Link { get; set; } = "";
    /// <summary>Khoá chống gửi trùng (vd. "hanhoc:lich-su-12:20261201"): cùng người + cùng khoá chỉ có một tin.</summary>
    public string? KhoaDuyNhat { get; set; }
    public bool DaDoc { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}
