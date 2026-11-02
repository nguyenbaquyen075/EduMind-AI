using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public abstract class AdminControllerBase(AppDbContext db) : Controller
{
    protected AppDbContext Db { get; } = db;
    protected string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    protected void ThongBao(string s) => TempData["ThongBao"] = s;
    protected void BaoLoi(string s) => TempData["Loi"] = s;

    /// <summary>
    /// Bài học, bài tập, lịch dạy xem theo từng giảng viên: gv = mã người dùng hoặc "none" (khoá chưa phân công).
    /// Thiếu gv nhưng có khoá thì lấy giảng viên của khoá. Trả về danh sách mã khoá của người đó; null = chưa chọn ai.
    /// </summary>
    protected async Task<List<string>?> ChonGiangVien(string? gv, string? khoa)
    {
        if (string.IsNullOrEmpty(gv) && !string.IsNullOrEmpty(khoa))
            gv = await Db.Khoa.Where(k => k.Id == khoa).Select(k => k.GiangVienId ?? KhongPhanCong).FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(gv)) return null;
        var laKhong = gv == KhongPhanCong;
        var nd = laKhong ? null : await Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == gv);
        if (!laKhong && nd is null) return null;
        var dsKhoa = await Db.Khoa.AsNoTracking().Where(k => laKhong ? k.GiangVienId == null : k.GiangVienId == gv).OrderBy(k => k.ThuTu).ToListAsync();
        ViewBag.Gv = gv; ViewBag.TenGv = nd?.HoTen ?? "Khoá chưa phân công"; ViewBag.EmailGv = nd?.Email; ViewBag.DsKhoa = dsKhoa;
        return dsKhoa.Select(k => k.Id).ToList();
    }

    public const string KhongPhanCong = "none";

    /// <summary>Bước chọn giảng viên → khoá trước khi xem bài học / bài tập / lịch dạy, tránh lẫn giữa các lớp.</summary>
    protected async Task<IActionResult> ChonGiangVienView(string muc, string tieuDe)
    {
        var now = DateTime.UtcNow;
        var dsGv = await Db.UserRoles.Join(Db.Roles.Where(r => r.Name == VaiTro.GiangVien), ur => ur.RoleId, r => r.Id, (ur, r) => ur.UserId)
            .Union(Db.Khoa.Where(k => k.GiangVienId != null).Select(k => k.GiangVienId!))
            .Join(Db.Users, id => id, u => u.Id, (id, u) => new { u.Id, u.HoTen, u.Email }).ToListAsync();
        var khoa = await Db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu).Select(k => new AdminKhoaCuaGv(
            k.Id, k.Ten, k.GiangVienId ?? KhongPhanCong,
            k.ChuDe.SelectMany(c => c.Bai).Count(b => b.DaXuatBan), k.ChuDe.SelectMany(c => c.Bai).Count(b => !b.DaXuatBan),
            Db.BaiTapGiao.Count(t => t.Bai!.ChuDe!.KhoaId == k.Id),
            Db.BaiNopGiao.Count(n => n.DiemTuLuan == null && n.BaiTapGiao!.Bai!.ChuDe!.KhoaId == k.Id),
            Db.BuoiHoc.Count(b => b.ThoiGian >= now && b.Bai!.ChuDe!.KhoaId == k.Id))).ToListAsync();
        var the = dsGv.OrderBy(g => g.HoTen).Select(g => (g.Id, g.HoTen, g.Email)).ToList();
        if (khoa.Any(k => k.Gv == KhongPhanCong)) the.Add((KhongPhanCong, "Khoá chưa phân công", null));
        ViewData["Muc"] = muc; ViewData["TieuDe"] = tieuDe; ViewData["Title"] = tieuDe + " – Quản trị";
        return View("ChonGiangVien", the.Select(g => new AdminTheGiangVien(g.Id, g.HoTen, g.Email, khoa.Where(k => k.Gv == g.Id).ToList())).ToList());
    }

    public static string TenVaiTro(string? v) => v switch
    {
        VaiTro.Admin => "Admin", VaiTro.GiangVien => "Giảng viên", VaiTro.HocVien => "Học viên", _ => "—",
    };
}
