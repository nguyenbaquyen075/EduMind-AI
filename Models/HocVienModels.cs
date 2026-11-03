namespace SuDia.Web.Models;

// ======================================================= Trang học viên: việc cần làm, điểm, ôn câu sai

/// <summary>Bài tập đã giao mà học viên chưa nộp, còn hạn hoặc không hạn.</summary>
public record BaiTapCanLam(int Id, string Ten, string Khoa, string Bai, DateTime? HanNop, bool LaKiemTra);

/// <summary>Buổi học online sắp diễn ra của khoá học viên đang học.</summary>
public record BuoiHocSapToi(string Khoa, string Bai, DateTime ThoiGian, string Link);

/// <summary>Khoá sắp hết hạn học, nhắc gia hạn.</summary>
public record KhoaSapHetHan(string Id, string Ten, DateTime HanDen);

/// <summary>Một dòng trong bảng điểm bài tập của học viên (điểm thang 10).</summary>
public record DiemBaiTapCuaToi(int Id, string Ten, string Khoa, string Bai, DateTime? HanNop, DateTime? NopLuc, decimal? Diem, bool ChoCham, bool LaKiemTra);

/// <summary>Một lượt thi thử hoặc luyện tập theo bài.</summary>
public record LuotLamCuaToi(string Ten, string Khoa, bool LaDeThi, int SoDung, int SoCau, DateTime NopLuc)
{
    public decimal Diem10 => SoCau == 0 ? 0 : Math.Round(SoDung * 10m / SoCau, 1);
}

public record HuyHieu(string Ten, string MoTa, bool DatDuoc);

public class DiemCuaToiViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public IReadOnlyList<DiemBaiTapCuaToi> BaiTap { get; init; } = [];
    public IReadOnlyList<LuotLamCuaToi> LuotLam { get; init; } = [];
    public IReadOnlyList<HuyHieu> HuyHieu { get; init; } = [];
    public decimal? DiemBaiTapTb { get; init; }
    public decimal? DiemThiTb { get; init; }
    public int? TiLeDungLuyenTap { get; init; }
}

/// <summary>Câu học viên làm sai ở lần gần nhất, để ôn lại.</summary>
public record CauSaiOnTap(int Id, string Khoa, string Bai, int BaiId, string NoiDung, IReadOnlyList<string> PhuongAn, string DapAnDung, string? DaChon, string GiaiThich, DateTime LanCuoi);

public class ThiThuViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public IReadOnlyList<KhoaLuyen> Khoa { get; init; } = [];
    public string? KhoaId { get; init; }
    public IReadOnlyList<LuotLamCuaToi> LichSu { get; init; } = [];
}

public class OnTapViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public IReadOnlyList<CauSaiOnTap> Cau { get; init; } = [];
    public IReadOnlyList<(string Id, string Ten)> DsKhoa { get; init; } = [];
    public string? KhoaId { get; init; }
}

public class HoSoModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Nhập họ tên")]
    [System.ComponentModel.DataAnnotations.StringLength(80, ErrorMessage = "Họ tên tối đa 80 ký tự")]
    public string HoTen { get; set; } = "";
    public string Email { get; set; } = "";
}

public class DoiMatKhauModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Nhập mật khẩu hiện tại")]
    public string MatKhauCu { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Nhập mật khẩu mới")]
    [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới từ 8 ký tự")]
    public string MatKhauMoi { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Compare(nameof(MatKhauMoi), ErrorMessage = "Hai mật khẩu mới chưa khớp")]
    public string NhapLai { get; set; } = "";
}
