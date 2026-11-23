namespace SuDia.Web.Data;

// ======================================================= Lớp học và diễn đàn hỏi đáp
/// <summary>Lớp của một khoá: giảng viên của khoá tạo lớp, học viên (đã ghi danh khoá) vào bằng mã.</summary>
public class Lop : ICoCapNhat
{
    public int Id { get; set; }
    public string KhoaId { get; set; } = "";
    public Khoa? Khoa { get; set; }
    public string Ten { get; set; } = "";
    public string MoTa { get; set; } = "";
    public string MaThamGia { get; set; } = "";      // 6 ký tự, duy nhất
    public bool DangMo { get; set; } = true;          // false: không nhận học viên mới
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public List<LopHocVien> HocVien { get; set; } = [];
    public List<TinNhanLop> TinNhan { get; set; } = [];
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;
}

public class LopHocVien
{
    public int LopId { get; set; }
    public Lop? Lop { get; set; }
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public DateTime NgayVao { get; set; } = DateTime.UtcNow;
}

/// <summary>Một tin nhắn trong nhóm chat của lớp. Giảng viên của khoá và thành viên lớp cùng nhắn.</summary>
public class TinNhanLop
{
    public long Id { get; set; }
    public int LopId { get; set; }
    public Lop? Lop { get; set; }
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public string NoiDung { get; set; } = "";
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public static class GioiHanChat
{
    public const int NoiDung = 2000;
    public const int TinMoiPhut = 20;                 // chống spam: mỗi người tối đa 20 tin / phút
    public const int SoTinMoiLanTai = 50;
}
