using SuDia.Web.Data;

namespace SuDia.Web.Models;

/// <summary>Một khoá trong gói kèm hạn học hiện tại và hạn dự kiến sau khi mua.</summary>
public record KhoaTrongGoi(Khoa Khoa, bool DaGhiDanh, bool KhongGioiHan, DateTime? HanHienTai, DateTime? HanMoi)
{
    public bool ConHan => DaGhiDanh && (KhongGioiHan || HanHienTai > DateTime.UtcNow);
}

public record GoiHienThi(GoiBan Goi, IReadOnlyList<KhoaTrongGoi> Khoa)
{
    public bool CoTheMua => Goi.Gia is > 0 && Khoa.Count > 0;
    public bool DangSoHuuHet => Khoa.Count > 0 && Khoa.All(k => k.ConHan);
    public int? GiaMoiThang => Goi.Gia is int g && Goi.SoThang > 0 ? (int)Math.Round(g / (double)Goi.SoThang, MidpointRounding.AwayFromZero) : null;
}

public class ChonGoiViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public required IReadOnlyList<GoiHienThi> Goi { get; init; }
    public required IReadOnlyList<KhoaTrongGoi> DangHoc { get; init; }
    public required bool LaHocVien { get; init; }
    public required bool CoTheThanhToan { get; init; }
    public required bool GiaLap { get; init; }
}

public class MuaGoiViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public required GoiHienThi Goi { get; init; }
    public required string HoTen { get; init; }
    public required string Email { get; init; }
    public required bool LaHocVien { get; init; }
    public required bool CoTheThanhToan { get; init; }
    public required bool GiaLap { get; init; }
    public DonHang? DonDangCho { get; init; }
    public int PhutHetHan { get; init; } = 15;
}

public class KetQuaThanhToanViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public required DonHang Don { get; init; }
    public required IReadOnlyList<Khoa> Khoa { get; init; }
    public required DateTime? HetHanLink { get; init; }
}

public class LichSuDonViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public required IReadOnlyList<DonHang> Don { get; init; }
    public required string Loc { get; init; }
    public required int TongDon { get; init; }
    public required int SoDaTra { get; init; }
    public required int SoDangCho { get; init; }
    public required long TongDaTra { get; init; }
}

public class HoaDonViewModel
{
    public required DonHang Don { get; init; }
    public required IReadOnlyList<Khoa> Khoa { get; init; }
    public required string HoTen { get; init; }
    public required string Email { get; init; }
}
