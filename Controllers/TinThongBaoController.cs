using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Controllers;

// Thông báo trong web của người đang đăng nhập (chuông trên thanh đầu trang)
[Authorize, Route("ThongBao")]
public class TinThongBaoController(AppDbContext db, TinThongBaoService svc, IKhoaHocService khoaHoc) : Controller
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var ds = await db.TinThongBao.AsNoTracking().Where(t => t.NguoiDungId == Uid).OrderByDescending(t => t.NgayTao).Take(100).ToListAsync(ct);
        return View(new TinThongBaoViewModel { Menu = khoaHoc.LayKhoaHoc(null).Menu with { MucDangMo = "ThongBao" }, Tin = ds });
    }

    [HttpGet("Dem")]
    public async Task<IActionResult> Dem(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return Json(new { dem = await svc.DemChuaDocAsync(Uid, ct) });
    }

    // Bấm vào một tin: đánh dấu đã đọc rồi chuyển tới trang liên quan (chỉ nhận link nội bộ)
    [HttpPost("Mo/{id:long}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Mo(long id, CancellationToken ct)
    {
        var t = await db.TinThongBao.FirstOrDefaultAsync(x => x.Id == id && x.NguoiDungId == Uid, ct);
        if (t is null) return NotFound();
        if (!t.DaDoc) { t.DaDoc = true; await db.SaveChangesAsync(ct); }
        return Url.IsLocalUrl(t.Link) ? LocalRedirect(t.Link) : RedirectToAction(nameof(Index));
    }

    [HttpPost("DocHet"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DocHet(CancellationToken ct)
    {
        await db.TinThongBao.Where(t => t.NguoiDungId == Uid && !t.DaDoc).ExecuteUpdateAsync(s => s.SetProperty(t => t.DaDoc, true), ct);
        return RedirectToAction(nameof(Index));
    }

    // Chỉ Development: chạy ngay việc nhắc hạn (thường chạy nền mỗi 6 giờ) để kiểm thử
    [HttpGet("nhac-han-thu")]
    public async Task<IActionResult> NhacHanThu([FromServices] IWebHostEnvironment env, [FromServices] NhacHanService nhac, CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();
        return Json(new { soTin = await nhac.ChayAsync(ct) });
    }
}
