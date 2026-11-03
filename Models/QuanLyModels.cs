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

/// <summary>Bản nháp tách từ tài liệu cấp chủ đề: một chủ đề gồm nhiều bài, mỗi bài nhiều mục.</summary>
public class BanNhapChuDe
{
    public string KhoaId { get; set; } = "";
    public int? ChuDeId { get; set; }                 // thêm bài vào chủ đề có sẵn
    public string TenChuDe { get; set; } = "";
    public int? SoChuDe { get; set; }
    public List<BaiNhapChuDe> Bai { get; set; } = [];
}

public class BaiNhapChuDe
{
    public int So { get; set; }
    public string Ten { get; set; } = "";
    public bool Chon { get; set; } = true;
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
    [Required(ErrorMessage = "Nhập tên khoá")] [StringLength(120)] public string Ten { get; set; } = "";
    [Required(ErrorMessage = "Chọn hoặc nhập môn")] [StringLength(40)] public string Mon { get; set; } = "";
    [Required(ErrorMessage = "Nhập ký hiệu")] [StringLength(3)] public string KyHieu { get; set; } = "";
    [StringLength(600)] public string? MoTa { get; set; }
    public string? Anh { get; set; }
    public IFormFile? AnhTai { get; set; }
    public string? GiangVienId { get; set; }
    [Range(1, 999, ErrorMessage = "Thứ tự từ 1 đến 999")] public int? ThuTu { get; set; }
    public bool HienThi { get; set; } = true;
    [StringLength(2000)] public string? MucTieu { get; set; }
    [StringLength(600)] public string? DoiTuong { get; set; }
    // Bán lẻ: lưu vào gói chỉ chứa khoá này (Admin › Gói học), thanh toán qua PayOS
    [Range(1000, 100_000_000, ErrorMessage = "Giá từ 1.000 đến 100.000.000 đồng")] public int? Gia { get; set; }
    [Range(1, 60, ErrorMessage = "Thời hạn từ 1 đến 60 tháng")] public int SoThang { get; set; } = 12;
    public bool DangBan { get; set; } = true;
}

/// <summary>Trang xem trước nội dung bài như học viên trong khu giảng viên.</summary>
public record XemTruocBaiModel(string Nhan, string TieuDe, IReadOnlyList<MucNoiDung> Muc, string? GhiChu);

/// <summary>Một bài tập đã giao ở trang thống kê nộp bài của giảng viên.</summary>
public record GvBaiTapRow(int Id, int BaiId, string Ten, DateTime? HanNop, bool DaGiao, int SoNop, int SoHocVien, int ChoCham);

/// <summary>Khoá trong thanh chọn khoá ở trang Bài tập & nộp bài.</summary>
public record GvKhoaTomTat(string Id, string Ten, int SoBai, int BaiDaGiao, int ChoCham);

/// <summary>Thẻ khoá ở trang "Khoá học của tôi" của giảng viên.</summary>
public record TheKhoaGiangVien(string Id, string Ten, string MoTa, string Anh, int SoChuDe, int SoBaiXuatBan, int SoBaiNhap, int SoHocVien, bool HienThi = true);

public class GoiBanForm
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Nhập tên gói")] [StringLength(100)] public string Ten { get; set; } = "";
    [StringLength(40)] public string? Nhan { get; set; }
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Màu dạng #RRGGBB")] public string MauNhan { get; set; } = "#C8A15A";
    [Range(1000, 100_000_000, ErrorMessage = "Giá từ 1.000 đến 100.000.000 đ")] public int? Gia { get; set; } // để trống: chưa mở bán
    [Range(1, 60, ErrorMessage = "Thời hạn từ 1 đến 60 tháng")] public int SoThang { get; set; } = 12;
    public bool NoiBat { get; set; }
    public bool DangBan { get; set; } = true;
    public string? QuyenLoi { get; set; }                 // mỗi dòng một quyền lợi
    public List<string> KhoaIds { get; set; } = [];
}

public class SuaNguoiDungForm
{
    public string Id { get; set; } = "";
    [Required(ErrorMessage = "Nhập họ tên"), StringLength(150)] public string HoTen { get; set; } = "";
    [Required(ErrorMessage = "Nhập email"), EmailAddress(ErrorMessage = "Email chưa đúng")] public string Email { get; set; } = "";
}
