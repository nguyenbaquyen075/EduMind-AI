namespace SuDia.Web.Data;

// ======================================================= Gói học bán online và đơn hàng thanh toán PayOS

/// <summary>Gói học hiển thị ở trang chủ; admin sửa giá, quyền lợi và các khoá được mở.</summary>
public class GoiBan : ICoCapNhat
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";             // "Sử & Địa 12"
    public string Nhan { get; set; } = "";            // "Một môn" / "Trọn bộ"
    public string MauNhan { get; set; } = "#C8A15A";
    public int? Gia { get; set; }                     // VND; null = chưa đặt giá, chưa bán được
    public int SoThang { get; set; } = 12;            // thời hạn học sau khi mua
    public bool NoiBat { get; set; }
    public bool DangBan { get; set; } = true;
    public int ThuTu { get; set; }
    public List<string> QuyenLoi { get; set; } = [];
    public List<GoiBanKhoa> KhoaTrongGoi { get; set; } = [];   // các khoá được ghi danh khi mua (luôn được nạp kèm gói)
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;

    /// <summary>Mã các khoá trong gói (chỉ đọc, lấy từ KhoaTrongGoi). Không dùng trong câu truy vấn LINQ.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public IReadOnlyList<string> KhoaIds => KhoaTrongGoi.Select(k => k.KhoaId).ToList();

    /// <summary>Đặt lại danh sách khoá của gói: giữ dòng cũ còn dùng, thêm dòng mới, bỏ dòng thừa.</summary>
    public void DatKhoa(IEnumerable<string> khoaIds)
    {
        var moi = khoaIds.Distinct().ToList();
        KhoaTrongGoi.RemoveAll(k => !moi.Contains(k.KhoaId));
        foreach (var id in moi.Where(id => KhoaTrongGoi.All(k => k.KhoaId != id)))
            KhoaTrongGoi.Add(new GoiBanKhoa { KhoaId = id });
    }
}

/// <summary>Khoá thuộc gói bán (bảng nối). Không xoá được khoá còn nằm trong gói.</summary>
public class GoiBanKhoa
{
    public int GoiBanId { get; set; }
    public GoiBan? GoiBan { get; set; }
    public string KhoaId { get; set; } = "";
    public Khoa? Khoa { get; set; }
}

public static class TrangThaiDon
{
    public const string ChoThanhToan = "ChoThanhToan";
    public const string DaThanhToan = "DaThanhToan";
    public const string DaHuy = "DaHuy";
}

/// <summary>Giá, thời hạn và danh sách khoá được chép lại lúc tạo đơn để sửa gói sau này không ảnh hưởng đơn cũ.</summary>
public class DonHang
{
    public int Id { get; set; }
    public long MaThanhToan { get; set; }             // orderCode gửi PayOS, duy nhất
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public int? GoiBanId { get; set; }
    public GoiBan? GoiBan { get; set; }
    public string TenGoi { get; set; } = "";
    public int SoTien { get; set; }
    public int SoThang { get; set; }
    public List<string> KhoaIds { get; set; } = [];
    public string TrangThai { get; set; } = TrangThaiDon.ChoThanhToan;
    public string? MaLinkThanhToan { get; set; }     // paymentLinkId của PayOS
    public string? LinkThanhToan { get; set; }       // checkoutUrl
    public string? MaGiaoDich { get; set; }          // reference ngân hàng từ webhook
    public string? SoDienThoai { get; set; }         // chuẩn hoá: +84901234567
    public bool DongYNhanTin { get; set; }
    public string? LinkFacebook { get; set; }        // dành cho Messenger ở đợt sau
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayThanhToan { get; set; }
}

public static class KenhThongBao
{
    public const string Sms = "Sms";
    public const string Email = "Email";
}

public static class LoaiThongBao
{
    public const string DaThanhToan = "DaThanhToan";
}

public static class TrangThaiThongBao
{
    public const string ChoGui = "ChoGui";
    public const string DangGui = "DangGui";
    public const string DaGui = "DaGui";
    public const string Loi = "Loi";
    public const string BoQua = "BoQua";
}

public class ThongBao
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public DonHang? DonHang { get; set; }
    public string NguoiDungId { get; set; } = "";
    public string Kenh { get; set; } = KenhThongBao.Sms;
    public string Loai { get; set; } = LoaiThongBao.DaThanhToan;
    public string NguoiNhan { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public string TrangThai { get; set; } = TrangThaiThongBao.ChoGui;
    public int SoLanThu { get; set; }
    public DateTime? GuiLuc { get; set; }
    public DateTime? DangGuiLuc { get; set; }
    public DateTime GuiTiepLuc { get; set; } = DateTime.UtcNow;
    public string? MaNhaCungCap { get; set; }
    public string? LoiCuoi { get; set; }
    public DateTime TaoLuc { get; set; } = DateTime.UtcNow;
}

public static class QuyetDinhDuyet
{
    public const string TuDongDuyet = "TuDongDuyet";
    public const string CanXemXet = "CanXemXet";
    public const string TuChoi = "TuChoi";
}

public class NhatKyDuyet
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public DonHang? DonHang { get; set; }
    public DateTime ThoiDiem { get; set; } = DateTime.UtcNow;
    public string TacNhan { get; set; } = "";
    public string QuyetDinh { get; set; } = "";
    public string LyDo { get; set; } = "";
    public string DuLieuJson { get; set; } = "{}";
}
