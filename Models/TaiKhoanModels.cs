using System.ComponentModel.DataAnnotations;

namespace SuDia.Web.Models;

public class DangNhapModel
{
    [Required(ErrorMessage = "Nhập email")]
    [EmailAddress(ErrorMessage = "Email chưa đúng")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = "";

    public bool GhiNho { get; set; } = true;
    public string? ReturnUrl { get; set; }
}

public class DangKyModel
{
    [Required(ErrorMessage = "Nhập họ tên")]
    [StringLength(80)]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Nhập email")]
    [EmailAddress(ErrorMessage = "Email chưa đúng")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu ít nhất 8 ký tự")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = "";

    [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    public string NhapLai { get; set; } = "";
}
