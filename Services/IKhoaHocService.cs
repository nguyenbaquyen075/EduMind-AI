using SuDia.Web.Models;

namespace SuDia.Web.Services;

/// <summary>
/// Cung cấp dữ liệu cho các trang. Bản hiện tại là dữ liệu mẫu (DuLieuMauService);
/// sau này viết bản đọc từ database (EF Core + PostgreSQL) và đổi đăng ký trong Program.cs.
/// </summary>
public interface IKhoaHocService
{
    TrangChuViewModel LayTrangChu();
    TongQuanViewModel LayTongQuan();

    /// <param name="khoaId">null: chỉ hiện danh sách khoá; có giá trị: hiện thêm các bài của khoá đó</param>
    KhoaHocViewModel LayKhoaHoc(string? khoaId);

    BaiHocViewModel LayBaiHoc(int? id);

    /// <param name="khoaId">null: chỉ hiện các khoá; có giá trị: đề thi thử + bài tập theo chủ đề của khoá đó</param>
    LuyenDeViewModel LayLuyenDe(string? khoaId);

    LamCauHoiViewModel LayCauHoi(int? id, string? dapAnDaChon);
}
