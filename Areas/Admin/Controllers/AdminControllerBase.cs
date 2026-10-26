using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuDia.Web.Data;

namespace SuDia.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public abstract class AdminControllerBase(AppDbContext db) : Controller
{
    protected AppDbContext Db { get; } = db;
    protected string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    protected void ThongBao(string s) => TempData["ThongBao"] = s;
    protected void BaoLoi(string s) => TempData["Loi"] = s;

    public static string TenVaiTro(string? v) => v switch
    {
        VaiTro.Admin => "Admin", VaiTro.GiangVien => "Giảng viên", VaiTro.HocVien => "Học viên", _ => "—",
    };
}
