using NpgsqlTypes;

namespace SuDia.Web.Data;

/// <summary>
/// Chỉ mục tìm kiếm dùng chung cho Học viên › Tài liệu và trợ lý AI: mỗi dòng là một đoạn nội dung
/// (một mục bài học, một phần tài liệu, thông tin khoá, gói học). Cột TimKiem là tsvector (chữ đã bỏ dấu)
/// có chỉ mục GIN, nên tìm vẫn nhanh khi kho lớn và mọi máy chủ web dùng chung một chỉ mục.
/// </summary>
public class DoanChiMuc
{
    public long Id { get; set; }
    /// <summary>Đơn vị nguồn để cập nhật từng phần: "Bai:7", "TaiLieu:12", "Khoa:lich-su-12", "Goi", "Md:lich-su-12".</summary>
    public string NguonKhoa { get; set; } = "";
    public string? KhoaId { get; set; }            // null: không thuộc khoá nào (gói học)
    public string Loai { get; set; } = "";         // Bài học | Tài liệu | Thông tin khoá | Gói học
    public string Mon { get; set; } = "";
    public string Nguon { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public string? Lien { get; set; }
    public int ThuTu { get; set; }
    public DateTime NapLuc { get; set; } = DateTime.UtcNow;
    public NpgsqlTsVector TimKiem { get; set; } = null!;   // CSDL tự tính (cột sinh)
}
