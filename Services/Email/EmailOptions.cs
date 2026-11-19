namespace SuDia.Web.Services.Email;

public class EmailOptions
{
    public bool Bat { get; set; }
    public string Tu { get; set; } = "";
    public string TenHienThi { get; set; } = "SuDia";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string TenDangNhap { get; set; } = "";
    public string TenMien { get; set; } = "";

    public bool DaCauHinh => Bat && !string.IsNullOrWhiteSpace(Tu) && !string.IsNullOrWhiteSpace(Host);
}
