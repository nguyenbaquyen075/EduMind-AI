namespace SuDia.Web.Data;

// ======================================================= Thông báo trong web (chuông)
public static class LoaiTin
{
    public const string ChatHocVien = "ChatHocVien"; // giảng viên: học viên nhắn trong nhóm chat lớp
    public const string ChatGiangVien = "ChatGiangVien"; // học viên: giảng viên nhắn trong nhóm chat lớp
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
