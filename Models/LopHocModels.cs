using System.ComponentModel.DataAnnotations;

namespace SuDia.Web.Models;

public class LopForm
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Chọn khoá"), Display(Name = "Khoá học")] public string KhoaId { get; set; } = "";
    [Required(ErrorMessage = "Nhập tên lớp"), StringLength(100, ErrorMessage = "Tên lớp tối đa 100 ký tự"), Display(Name = "Tên lớp")] public string Ten { get; set; } = "";
    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự"), Display(Name = "Mô tả")] public string? MoTa { get; set; }
}

public record LopTomTat(int Id, string Ten, string Khoa, string MaThamGia, bool DangMo, int SoHocVien, long SoTinNhan, bool QuanLy);

public class LopCuaToiViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public IReadOnlyList<LopTomTat> Lop { get; init; } = [];
    public string? MaNhap { get; init; }
}

/// <summary>Một tin nhắn gửi xuống trình duyệt (cả lịch sử lẫn tin mới theo thời gian thực).</summary>
public record TinNhanDto(long Id, string NguoiDungId, string TacGia, bool LaGiangVien, string NoiDung, DateTime NgayTao);

public class NhomChatViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public required LopTomTat Lop { get; init; }
    public string MoTa { get; init; } = "";
    public bool QuanLy { get; init; }
    public required string NguoiDungId { get; init; }
    public IReadOnlyList<TinNhanDto> TinNhan { get; init; } = [];
    public bool CoTinCu { get; init; }
    public IReadOnlyList<string> ThanhVien { get; init; } = [];
}

public class TinThongBaoViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public IReadOnlyList<SuDia.Web.Data.TinThongBao> Tin { get; init; } = [];
}

public class DiemYeuViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public IReadOnlyList<SuDia.Web.Services.DiemTheoBai> TatCa { get; init; } = [];
    public IReadOnlyList<SuDia.Web.Services.DiemTheoBai> Yeu { get; init; } = [];
    public IReadOnlyList<SuDia.Web.Services.DiemTheoBai> Manh { get; init; } = [];
    public bool CoAi { get; init; }
}

// ---------------------------------------------------------------- Ra đề theo ma trận
public class MaTranDong
{
    public int BaiId { get; set; }
    public int De { get; set; }
    public int TB { get; set; }
    public int Kho { get; set; }
    public int Tong => De + TB + Kho;
}

public class MaTranForm
{
    public string KhoaId { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Nhập tên đề"), System.ComponentModel.DataAnnotations.StringLength(200, ErrorMessage = "Tên đề tối đa 200 ký tự")] public string Ten { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Range(5, 240, ErrorMessage = "Thời gian từ 5 đến 240 phút")] public int SoPhut { get; set; } = 45;
    public List<MaTranDong> Dong { get; set; } = [];
}

public record MaTranBai(int BaiId, int So, string Ten, int SoDe, int SoTB, int SoKho);

public class MaTranViewModel
{
    public required SuDia.Web.Data.Khoa Khoa { get; init; }
    public required IReadOnlyList<MaTranBai> Bai { get; init; }
    public required MaTranForm Form { get; init; }
}

// ---------------------------------------------------------------- Chứng chỉ
public record KhoaChungChi(string KhoaId, string Ten, int DaHoc, int Tong, SuDia.Web.Data.ChungChi? DaCap)
{
    public bool DuDieuKien => Tong > 0 && DaHoc >= Tong;
    public int PhanTram => Tong == 0 ? 0 : (int)Math.Round(DaHoc * 100.0 / Tong);
}

public class ChungChiViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public IReadOnlyList<KhoaChungChi> Khoa { get; init; } = [];
}
