using Microsoft.AspNetCore.Identity;

namespace SuDia.Web.Data;

// ======================================================= Tài khoản & vai trò
public class NguoiDung : IdentityUser
{
    public string HoTen { get; set; } = "";
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public static class VaiTro
{
    public const string HocVien = "HocVien";
    public const string GiangVien = "GiangVien";
    public const string Admin = "Admin";
    public static readonly string[] TatCa = [HocVien, GiangVien, Admin];
}

/// <summary>Bảng có cột CapNhatLuc: AppDbContext tự ghi giờ mỗi lần thêm/sửa.</summary>
public interface ICoCapNhat
{
    DateTime CapNhatLuc { get; set; }
}

// ======================================================= Nội dung khoá học
public class Khoa : ICoCapNhat
{
    public string Id { get; set; } = "";          // "lich-su-12"
    public string Ten { get; set; } = "";
    public string Mon { get; set; } = "";         // "Lịch sử", "Địa lí"…
    public string KyHieu { get; set; } = "";
    public string MoTa { get; set; } = "";
    public string Anh { get; set; } = "";
    public int ThuTu { get; set; }
    public bool HienThi { get; set; } = true;     // false: học viên chưa ghi danh không thấy khoá
    public string MucTieu { get; set; } = "";     // mỗi dòng một mục tiêu
    public string DoiTuong { get; set; } = "";
    public string? GiangVienId { get; set; }      // giảng viên phụ trách
    public NguoiDung? GiangVien { get; set; }
    public List<ChuDe> ChuDe { get; set; } = [];
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;
}

public class ChuDe : ICoCapNhat
{
    public int Id { get; set; }
    public string KhoaId { get; set; } = "";
    public Khoa? Khoa { get; set; }
    public string Ten { get; set; } = "";          // "Chủ đề 1 · Thế giới …"
    public int ThuTu { get; set; }
    public List<Bai> Bai { get; set; } = [];
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;
}

public class Bai : ICoCapNhat
{
    public int Id { get; set; }                    // tự tăng; bài mẫu giữ mã cũ (Lịch sử = số bài, Địa lí = 100 + số bài)
    public int ChuDeId { get; set; }
    public ChuDe? ChuDe { get; set; }
    public int So { get; set; }
    public string Ten { get; set; } = "";
    public int SoMuc { get; set; }
    public int SoPhut { get; set; }
    public int ThuTu { get; set; }
    public bool HocThu { get; set; }               // chưa ghi danh vẫn học được (bài 1)
    public bool DaXuatBan { get; set; } = true;
    public List<Muc> Muc { get; set; } = [];
    public List<CauHoi> CauHoi { get; set; } = [];
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;
}

public class Muc : ICoCapNhat
{
    public int Id { get; set; }
    public int BaiId { get; set; }
    public Bai? Bai { get; set; }
    public int So { get; set; }
    public string Ten { get; set; } = "";
    public string TieuDe { get; set; } = "";
    public int SoPhut { get; set; }
    public List<Phan> Phan { get; set; } = [];
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;
}

public class Phan
{
    public int Id { get; set; }
    public int MucId { get; set; }
    public Muc? Muc { get; set; }
    public int ThuTu { get; set; }
    public string MaNeo { get; set; } = "";         // "dien-bien" → #muc-3-dien-bien
    public string TieuDe { get; set; } = "";
    public List<string> DoanVan { get; set; } = [];
    public bool LaGhiNho { get; set; }
    public List<MocThoiGian> Moc { get; set; } = [];
}

public class MocThoiGian
{
    public int Id { get; set; }
    public int PhanId { get; set; }
    public int ThuTu { get; set; }
    public string Ten { get; set; } = "";
    public string ThoiGian { get; set; } = "";
    public string NoiDung { get; set; } = "";
}

// ======================================================= Câu hỏi & đề
public class CauHoi : ICoCapNhat
{
    public int Id { get; set; }
    public int BaiId { get; set; }
    public Bai? Bai { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = "";
    public List<string> PhuongAn { get; set; } = [];   // đúng 4 phương án A, B, C, D
    public string DapAnDung { get; set; } = "";
    public string GiaiThich { get; set; } = "";
    public int? Muc { get; set; }
    public string NguonHienThi { get; set; } = "";
    public int DoKho { get; set; } = 2;               // 1 dễ, 2 trung bình, 3 khó (dùng để ra đề theo ma trận)
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;
}

public static class DoKhoCau
{
    public const int De = 1, TrungBinh = 2, Kho = 3;
    public static string Ten(int d) => d switch { 1 => "Dễ", 3 => "Khó", _ => "Trung bình" };
}

public class DeThi : ICoCapNhat
{
    public int Id { get; set; }                    // tự tăng; đề mẫu giữ mã cũ 1001+ Lịch sử, 2001+ Địa lí
    public string KhoaId { get; set; } = "";
    public Khoa? Khoa { get; set; }
    public string Ten { get; set; } = "";
    public int SoPhut { get; set; }
    public bool DangMo { get; set; } = true;
    public int? BaiTapGiaoId { get; set; } // đề mới dùng chung bộ soạn TN/ĐS/TL với bài tập
    public BaiTapGiao? BaiTapGiao { get; set; }
    public List<DeThiCau> Cau { get; set; } = [];
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;
}

public class DeThiCau
{
    public int DeThiId { get; set; }
    public int CauHoiId { get; set; }
    public CauHoi? CauHoi { get; set; }
    public int ThuTu { get; set; }
}

// ======================================================= Học viên: ghi danh, tiến độ, bài làm
public class GhiDanh
{
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public string KhoaId { get; set; } = "";
    public Khoa? Khoa { get; set; }
    public DateTime? HanDen { get; set; }          // null = không giới hạn
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public enum TrangThaiHoc { DangHoc = 1, DaHoc = 2 }

public class TienDoBai
{
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public int BaiId { get; set; }
    public Bai? Bai { get; set; }
    public TrangThaiHoc TrangThai { get; set; }
    public DateTime CapNhat { get; set; } = DateTime.UtcNow;
}

public class BaiLam
{
    public int Id { get; set; }
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    // Luyện tập theo bài: BaiId; đề thi thử: DeThiId (đúng một trong hai, có ràng buộc kiểm tra)
    public int? BaiId { get; set; }
    public Bai? Bai { get; set; }
    public int? DeThiId { get; set; }
    public DeThi? DeThi { get; set; }
    public bool LaDeThi { get; set; }
    public string TenDe { get; set; } = "";
    public int SoCau { get; set; }
    public int SoDung { get; set; }
    public DateTime NopLuc { get; set; } = DateTime.UtcNow;
    public List<CauTraLoi> TraLoi { get; set; } = [];
}

public class CauTraLoi
{
    public int Id { get; set; }
    public int BaiLamId { get; set; }
    public int CauHoiId { get; set; }
    public CauHoi? CauHoi { get; set; }
    public int ThuTu { get; set; }
    public string? DaChon { get; set; }
    public bool Dung { get; set; }
}

// ======================================================= Học online
public class BuoiHoc
{
    public int Id { get; set; }
    public int BaiId { get; set; }
    public Bai? Bai { get; set; }
    public string Link { get; set; } = "";
    public DateTime? ThoiGian { get; set; }
}
