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
    public List<BaiDang> BaiDang { get; set; } = [];
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

/// <summary>Một chủ đề thảo luận trong lớp (câu hỏi, thông báo của giảng viên…).</summary>
public class BaiDang : ICoCapNhat
{
    public int Id { get; set; }
    public int LopId { get; set; }
    public Lop? Lop { get; set; }
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public string TieuDe { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public bool Ghim { get; set; }                    // giảng viên ghim lên đầu
    public bool DaDong { get; set; }                  // đóng: không trả lời thêm
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public List<TraLoiBaiDang> TraLoi { get; set; } = [];
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;
}

public class TraLoiBaiDang
{
    public int Id { get; set; }
    public int BaiDangId { get; set; }
    public BaiDang? BaiDang { get; set; }
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public string NoiDung { get; set; } = "";
    public bool LaDapAn { get; set; }                 // người hỏi hoặc giảng viên đánh dấu
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public static class GioiHanDienDan
{
    public const int TieuDe = 200;
    public const int NoiDung = 4000;
    public const int BaiMoiMoiGio = 10;               // chống spam: mỗi người tối đa 10 bài/giờ
    public const int TraLoiMoiGio = 30;
}
