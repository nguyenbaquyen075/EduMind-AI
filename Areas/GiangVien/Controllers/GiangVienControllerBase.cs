using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Areas.GiangVien.Controllers;

/// <summary>Giảng viên chỉ thao tác trên khoá được gán; Admin thao tác mọi khoá.</summary>
[Area("GiangVien")]
[Authorize(Roles = VaiTro.GiangVien + "," + VaiTro.Admin)]
public abstract class GiangVienControllerBase(AppDbContext db) : Controller
{
    protected AppDbContext Db { get; } = db;
    protected string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    protected bool LaAdmin => User.IsInRole(VaiTro.Admin);

    protected IQueryable<Khoa> KhoaCuaToi() => Db.Khoa.Where(k => LaAdmin || k.GiangVienId == Uid);
    protected IQueryable<Bai> BaiCuaToi() => Db.Bai.Where(b => LaAdmin || b.ChuDe!.Khoa!.GiangVienId == Uid);

    protected void ThongBao(string s) => TempData["ThongBao"] = s;
    protected void BaoLoi(string s) => TempData["Loi"] = s;
}
