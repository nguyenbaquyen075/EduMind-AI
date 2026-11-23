using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Controllers;

// Chứng chỉ hoàn thành khoá: học viên nhận / xem / in; ai cũng kiểm tra được mã tại /XacThuc/{mã}
[Authorize]
public class ChungChiController(ChungChiService svc, AppDbContext db, IKhoaHocService khoaHoc) : Controller
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("ChungChi")]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new ChungChiViewModel { Menu = khoaHoc.LayKhoaHoc(null).Menu with { MucDangMo = "ChungChi" }, Khoa = await svc.DanhSachAsync(Uid, ct) });

    [HttpPost("ChungChi/Nhan"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Nhan(string khoaId, CancellationToken ct)
    {
        var (cc, loi) = await svc.CapAsync(Uid, khoaId, ct);
        if (cc is null) { TempData["Loi"] = loi; return RedirectToAction(nameof(Index)); }
        return RedirectToAction(nameof(In), new { id = cc.Id });
    }

    // Trang chứng chỉ dạng A4 ngang, bấm "In / Lưu PDF" để in hoặc lưu thành PDF
    [HttpGet("ChungChi/In/{id:int}")]
    public async Task<IActionResult> In(int id, CancellationToken ct)
    {
        var cc = await db.ChungChi.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && (c.NguoiDungId == Uid || User.IsInRole(VaiTro.Admin)), ct);
        if (cc is null) return NotFound();
        ViewBag.LinkXacThuc = $"{Request.Scheme}://{Request.Host}/XacThuc/{cc.MaXacThuc}";
        return View(cc);
    }

    [AllowAnonymous, HttpGet("XacThuc/{ma}")]
    public async Task<IActionResult> XacThuc(string ma, CancellationToken ct)
    {
        ma = new string((ma ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        var cc = ma.Length == 10 ? await db.ChungChi.AsNoTracking().FirstOrDefaultAsync(c => c.MaXacThuc == ma, ct) : null;
        return View(cc);
    }
}
