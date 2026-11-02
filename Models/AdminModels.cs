using SuDia.Web.Data;

namespace SuDia.Web.Models;

public record AdminKhoaRow(string Id, string Ten, string? GiangVien, int SoBai, int DaXuatBan, int SoHocVien, int DangHoc);
public record AdminNgay(DateTime Ngay, int LuyenTap, int BaiNop, int DangKy)
{
    public int Tong => LuyenTap + BaiNop;
}
public record AdminHoatDong(string HocVien, string NoiDung, string Loai, DateTime Luc, string Link);
public record AdminViec(string Ten, string ChiTiet, string Link, bool CanChuY = false);
public record AdminBuoi(int Id, int BaiId, string Bai, string Khoa, string? GiangVien, DateTime? Luc, string Link);

public class AdminDashboardViewModel
{
    public List<SoLieu> SoLieu { get; init; } = [];
    public List<AdminNgay> Ngay { get; init; } = [];
    public List<AdminKhoaRow> Khoa { get; init; } = [];
    public List<AdminHoatDong> HoatDong { get; init; } = [];
    public List<AdminViec> Viec { get; init; } = [];
    public List<AdminBuoi> Buoi { get; init; } = [];
    public int TaiKhoanMoi { get; init; }
    public int TongHoatDong => Ngay.Sum(n => n.Tong);
}

public class AdminBaoCaoViewModel
{
    public DateTime Tu { get; init; }
    public DateTime Den { get; init; }
    public string? KhoaId { get; init; }
    public List<Khoa> DsKhoa { get; init; } = [];
    public List<SoLieu> SoLieu { get; init; } = [];
    public List<AdminNgay> Ngay { get; init; } = [];
    public List<AdminKhoaRow> Khoa { get; init; } = [];
    public int SoBaiTuLuanChoCham { get; init; }
}

public record AdminGhiDanhRow(string NguoiDungId, string HoTen, string? Email, string KhoaId, string Khoa, DateTime NgayTao, DateTime? HanDen);
public class AdminGhiDanhViewModel
{
    public List<AdminGhiDanhRow> Ds { get; init; } = [];
    public List<Khoa> DsKhoa { get; init; } = [];
    public string? Q { get; init; }
    public string? KhoaId { get; init; }
    public string? TrangThai { get; init; }
    public int Tong { get; init; }
    public int DangHoc { get; init; }
    public int SapHetHan { get; init; }
    public int HetHan { get; init; }
    public int Trang { get; init; }
    public int SoTrang => Math.Max(1, (int)Math.Ceiling(Tong / 25d));
}

public record AdminBaiTapRow(int Id, int BaiId, string Ten, string Bai, string KhoaId, string Khoa, bool DaGiao, DateTime? HanNop, int SoNop, int ChoCham, int SoHocVien = 0);
public class AdminNguoiDungViewModel
{
    public required NguoiDung NguoiDung { get; init; }
    public List<string> VaiTro { get; init; } = [];
    public List<AdminGhiDanhRow> GhiDanh { get; init; } = [];
    public List<BaiLam> LuyenTap { get; init; } = [];
    public List<BaiNopGiao> BaiNop { get; init; } = [];
    public int TongLuyenTap { get; init; }
    public int TongBaiNop { get; init; }
    public int BaiDaHoc { get; init; }
}

public record AdminTaiLieu(string Ten, string Mon, int SoBai, int SoMuc, long SoByte);
public class AdminTroLyViewModel
{
    public bool BanXemThu { get; init; }
    public required SuDia.Web.Services.TroLy.TroLyOptions CauHinh { get; init; }
    public List<AdminTaiLieu> TaiLieu { get; init; } = [];
    public int SoCauKiemThu { get; init; }
    public bool CoPrompt { get; init; }
}

/// <summary>Thẻ chọn giảng viên → khoá ở Nội dung / Bài tập / Lịch dạy (Admin); mã "none" = các khoá chưa phân công.</summary>
public record AdminTheGiangVien(string Id, string HoTen, string? Email, IReadOnlyList<AdminKhoaCuaGv> Khoa);
public record AdminKhoaCuaGv(string Id, string Ten, string Gv, int BaiXuatBan, int BaiNhap, int BaiTap, int ChoCham, int BuoiSapToi);

/// <summary>Tỉ lệ hoàn thành bài tập của một khoá (biểu đồ ở Admin › Bài tập).</summary>
public record AdminTiLeKhoa(string Id, string Ten, int SoBaiTap, int SoHocVien, int DaNop)
{
    public int CanNop => SoBaiTap * SoHocVien;
    public int? PhanTram => CanNop == 0 ? null : (int)Math.Round(DaNop * 100.0 / CanNop);
}

// ======================================================= Doanh thu, giảng viên, học viên (Admin)

public record AdminDoanhThuNgay(DateTime Ngay, long SoTien, int SoDon);
public record AdminDoanhThuGoi(string TenGoi, long SoTien, int SoDon);

public class AdminDoanhThuViewModel
{
    public DateTime Tu { get; init; }
    public DateTime Den { get; init; }
    public long DoanhThuKy { get; init; }
    public int SoDonKy { get; init; }
    public long TongTuTruoc { get; init; }
    public int DonCho { get; init; }
    public IReadOnlyList<AdminDoanhThuNgay> Ngay { get; init; } = [];
    public IReadOnlyList<AdminDoanhThuGoi> TheoGoi { get; init; } = [];
    public IReadOnlyList<DonHang> Don { get; init; } = [];
    public string? TrangThai { get; init; }
    public bool DaCauHinh { get; init; }
}

public record AdminGiaoVienRow(string Id, string HoTen, string? Email, bool BiKhoa, IReadOnlyList<string> Khoa,
    int HocVien, int BaiXuatBan, int BaiTong, int BaiTap, int ChoCham, int? HoanThanh, int BuoiSapToi);

public record AdminHocVienRow(string Id, string HoTen, string? Email, IReadOnlyList<string> Khoa, DateTime? HanGanNhat,
    int BaiDaHoc, int BaiTong, int LuyenTap, int BaiNop, DateTime? HoatDongCuoi)
{
    public int? TienDo => BaiTong == 0 ? null : BaiDaHoc * 100 / BaiTong;
}
