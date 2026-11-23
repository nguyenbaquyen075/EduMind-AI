namespace SuDia.Web.Data;

// ======================================================= Chứng chỉ hoàn thành khoá
/// <summary>
/// Chứng chỉ cấp khi học viên học hết mọi bài đã xuất bản của khoá. Tên người và tên khoá được chép lại lúc cấp
/// để đổi tên sau này không làm sai chứng chỉ; <see cref="MaXacThuc"/> dùng để kiểm tra công khai.
/// </summary>
public class ChungChi
{
    public int Id { get; set; }
    public string MaXacThuc { get; set; } = "";       // 10 ký tự, duy nhất
    public string NguoiDungId { get; set; } = "";
    public NguoiDung? NguoiDung { get; set; }
    public string KhoaId { get; set; } = "";
    public Khoa? Khoa { get; set; }
    public string HoTen { get; set; } = "";
    public string TenKhoa { get; set; } = "";
    public int SoBai { get; set; }                    // số bài đã học lúc cấp
    public DateTime NgayCap { get; set; } = DateTime.UtcNow;
}
