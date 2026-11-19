namespace SuDia.Web.Services.Sms;

public class SmsOptions
{
    public bool Bat { get; set; }
    public string NhaCungCap { get; set; } = "";
    public string Brandname { get; set; } = "";
    public int ToiDaMoiNgayMoiSo { get; set; } = 3;
    public int ToiDaMoiNgayMoiTaiKhoan { get; set; } = 10;
    public string TenMien { get; set; } = "";
}
