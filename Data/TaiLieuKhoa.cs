namespace SuDia.Web.Data;

public static class TrangThaiTaiLieu
{
    public const string SanSang = "SanSang";   // đã trích được chữ: học viên tìm được, trợ lý AI đọc được
    public const string Loi = "Loi";           // lưu tệp được nhưng không đọc được chữ (ảnh scan, có mật khẩu…)
}

/// <summary>
/// Kho tài liệu của khoá: tệp gốc giảng viên tải lên (Word, PowerPoint, PDF, văn bản) và phần chữ trích ra.
/// Tệp gốc nằm trên đĩa (ngoài wwwroot, tải qua controller có kiểm quyền); chữ trích nằm trong CSDL để tìm và cho AI đọc.
/// </summary>
public class TaiLieuKhoa : ICoCapNhat
{
    public int Id { get; set; }
    public string KhoaId { get; set; } = "";
    public Khoa? Khoa { get; set; }
    public int? BaiId { get; set; }                  // gắn với bài (không bắt buộc)
    public Bai? Bai { get; set; }
    public string Ten { get; set; } = "";            // tên hiển thị
    public string TenTep { get; set; } = "";         // tên tệp gốc
    public string LoaiTep { get; set; } = "";        // docx | pptx | pdf | txt | md
    public long KichThuoc { get; set; }
    public string MaBam { get; set; } = "";          // SHA-256: không lưu trùng một tệp hai lần trong cùng khoá
    public string NoiLuu { get; set; } = "CucBo";    // CucBo (đĩa máy chủ) | S3 (Cloudflare R2 / AWS S3…)
    public string DuongDan { get; set; } = "";       // khoá đối tượng trong kho: khoa/<mã khoá>/<guid>.<đuôi>
    public string NoiDung { get; set; } = "";        // chữ trích từ tệp
    public string TrangThai { get; set; } = TrangThaiTaiLieu.SanSang;
    public string? LoiDoc { get; set; }
    public bool HienChoHocVien { get; set; } = true; // tắt: chỉ giảng viên thấy, AI không đọc
    public string? NguoiTaiId { get; set; }
    public NguoiDung? NguoiTai { get; set; }
    public DateTime TaiLuc { get; set; } = DateTime.UtcNow;
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;
}
