namespace SuDia.Web.Data;

// ======================================================= Gói học bán online và đơn hàng thanh toán PayOS

/// <summary>Gói học hiển thị ở trang chủ; admin sửa giá, quyền lợi và các khoá được mở.</summary>
public class GoiBan
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
    public List<string> KhoaIds { get; set; } = [];   // các khoá được ghi danh khi mua
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
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayThanhToan { get; set; }
}
