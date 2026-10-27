using System.ComponentModel.DataAnnotations;

namespace SuDia.Web.Models;

public record SoLieu(string Nhan, string GiaTri, string? Phu = null);

// ======================================================= Giảng viên: bàn làm việc
public record ViecCanLam(string NoiDung, string Link, string NutChu);
public record BaiLamGanDay(string HocVien, string TenDe, int SoDung, int SoCau, DateTime NopLuc);
public record CauSaiNhieu(int Id, string NoiDung, string Nguon, int SoLanSai, int SoLanLam);
public record BuoiSapToi(int Id, string TenBai, string Link, DateTime ThoiGian);

public class GiangVienTongQuanViewModel
{
    public IReadOnlyList<SoLieu> SoLieu { get; init; } = [];
    public IReadOnlyList<ViecCanLam> ViecCanLam { get; init; } = [];
    public IReadOnlyList<BaiLamGanDay> BaiLam { get; init; } = [];
    public IReadOnlyList<CauSaiNhieu> CauSai { get; init; } = [];
    public IReadOnlyList<BuoiSapToi> BuoiSapToi { get; init; } = [];
}

// ======================================================= Giảng viên: biểu mẫu soạn nội dung
public class PhanForm
{
    public string TieuDe { get; set; } = "";
    public string DoanVan { get; set; } = "";      // các đoạn cách nhau bằng một dòng trống
    public string Moc { get; set; } = "";          // mỗi dòng: Tên | Thời gian | Nội dung
    public bool LaGhiNho { get; set; }
}

public class MucForm
{
    public int Id { get; set; }
    public int BaiId { get; set; }
    [Required(ErrorMessage = "Nhập tên mục (hiện trong mục lục)")] public string Ten { get; set; } = "";
    public string? TieuDe { get; set; } = ""; // để trống: dùng tên mục
    public List<PhanForm> Phan { get; set; } = [];
}

/// <summary>Bản nháp AI chỉ tồn tại trên màn xem thử; chỉ ghi vào bài khi giảng viên bấm Chia sẻ.</summary>
public class BanNhapNoiDungAiForm
{
    public int BaiId { get; set; }
    public string PhamVi { get; set; } = "KhoaHoc";
    public List<MucForm> Muc { get; set; } = [];
}

public class CauHoiForm
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Chọn bài")] public string ViTri { get; set; } = "";   // "baiId" hoặc "baiId:muc"
    [Required(ErrorMessage = "Nhập câu hỏi")] public string NoiDung { get; set; } = "";
    [Required(ErrorMessage = "Nhập phương án A")] public string A { get; set; } = "";
    [Required(ErrorMessage = "Nhập phương án B")] public string B { get; set; } = "";
    [Required(ErrorMessage = "Nhập phương án C")] public string C { get; set; } = "";
    [Required(ErrorMessage = "Nhập phương án D")] public string D { get; set; } = "";
    [Required(ErrorMessage = "Chọn đáp án đúng")] [RegularExpression("^[ABCD]$")] public string DapAn { get; set; } = "";
    [Required(ErrorMessage = "Nhập lời giải thích")] public string GiaiThich { get; set; } = "";
}

public class DeThiForm
{
    public int Id { get; set; }
    [Required] public string KhoaId { get; set; } = "";
    [Required(ErrorMessage = "Nhập tên đề")] public string Ten { get; set; } = "";
    [Range(5, 240, ErrorMessage = "Thời gian từ 5 đến 240 phút")] public int SoPhut { get; set; } = 50;
    public bool DangMo { get; set; } = true;
    public List<int> CauHoiIds { get; set; } = [];
}

public class BuoiHocForm
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Chọn bài")] public int BaiId { get; set; }
    [Required(ErrorMessage = "Dán link Google Meet")] [Url(ErrorMessage = "Link chưa đúng")] public string Link { get; set; } = "";
    public DateTime? ThoiGian { get; set; }
}

// ======================================================= Admin
public class TaoNguoiDungForm
{
    [Required(ErrorMessage = "Nhập họ tên")] public string HoTen { get; set; } = "";
    [Required(ErrorMessage = "Nhập email")] [EmailAddress(ErrorMessage = "Email chưa đúng")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu")] [MinLength(8, ErrorMessage = "Mật khẩu ít nhất 8 ký tự")] public string MatKhau { get; set; } = "";
    [Required] public string VaiTro { get; set; } = "HocVien";
}

public class KhoaForm
{
    [Required] public string Id { get; set; } = "";
    [Required(ErrorMessage = "Nhập tên khoá")] public string Ten { get; set; } = "";
    [Required(ErrorMessage = "Nhập ký hiệu")] [StringLength(3)] public string KyHieu { get; set; } = "";
    public string? MoTa { get; set; }
    public string? Anh { get; set; }
    public string? GiangVienId { get; set; }
}

public class SuaNguoiDungForm
{
    public string Id { get; set; } = "";
    [Required(ErrorMessage = "Nhập họ tên"), StringLength(150)] public string HoTen { get; set; } = "";
    [Required(ErrorMessage = "Nhập email"), EmailAddress(ErrorMessage = "Email chưa đúng")] public string Email { get; set; } = "";
}
