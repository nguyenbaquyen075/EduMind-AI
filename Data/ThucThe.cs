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

// ======================================================= Nội dung khoá học
public class Khoa
{
    public string Id { get; set; } = "";          // "lich-su-12"
    public string Ten { get; set; } = "";
    public string KyHieu { get; set; } = "";
    public string MoTa { get; set; } = "";
    public string Anh { get; set; } = "";
    public int ThuTu { get; set; }
    public string? GiangVienId { get; set; }      // giảng viên phụ trách
    public NguoiDung? GiangVien { get; set; }
    public List<ChuDe> ChuDe { get; set; } = [];
}

public class ChuDe
{
    public int Id { get; set; }
    public string KhoaId { get; set; } = "";
    public Khoa? Khoa { get; set; }
    public string Ten { get; set; } = "";          // "Chủ đề 1 · Thế giới …"
    public int ThuTu { get; set; }
    public List<Bai> Bai { get; set; } = [];
}

public class Bai
{
    public int Id { get; set; }                    // giữ mã cũ: Lịch sử = số bài, Địa lí = 100 + số bài
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
}

public class Muc
{
    public int Id { get; set; }
    public int BaiId { get; set; }
    public Bai? Bai { get; set; }
    public int So { get; set; }
    public string Ten { get; set; } = "";
    public string TieuDe { get; set; } = "";
    public int SoPhut { get; set; }
    public List<Phan> Phan { get; set; } = [];
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
public class CauHoi
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
}

public class DeThi
{
    public int Id { get; set; }                    // 1001+ Lịch sử, 2001+ Địa lí
    public string KhoaId { get; set; } = "";
    public Khoa? Khoa { get; set; }
    public string Ten { get; set; } = "";
    public int SoPhut { get; set; }
    public bool DangMo { get; set; } = true;
    public List<DeThiCau> Cau { get; set; } = [];
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
    public int BaiId { get; set; }
    public TrangThaiHoc TrangThai { get; set; }
    public DateTime CapNhat { get; set; } = DateTime.UtcNow;
}

public class BaiLam
{
    public int Id { get; set; }
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public int MaDe { get; set; }                  // mã bài (bài tập) hoặc mã đề thi
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
