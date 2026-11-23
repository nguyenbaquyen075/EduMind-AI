namespace SuDia.Web.Models;

public record DiemBieuDo(string Nhan, double Gia, string? ChuThich = null);

/// <summary>Dữ liệu cho biểu đồ SVG dựng ở máy chủ (không cần thư viện JavaScript).</summary>
public class BieuDoModel
{
    public required string MoTa { get; init; }            // đọc cho người dùng trình đọc màn hình
    public required IReadOnlyList<DiemBieuDo> Diem { get; init; }
    public double Max { get; init; } = 10;
    public string DonVi { get; init; } = "";               // "điểm", "bài"…
    public string CotGiaTri { get; init; } = "Giá trị";
    public bool NhanGiaTriTrenCot { get; init; } = true;
}
