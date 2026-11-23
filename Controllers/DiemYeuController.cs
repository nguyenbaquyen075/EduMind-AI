using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Controllers;

// Điểm mạnh / yếu của học viên theo từng bài, đề ôn riêng và câu hỏi do AI tạo thêm
[Authorize, Route("DiemYeu")]
public class DiemYeuController(PhanTichDiemYeuService phanTich, AiHocTapService ai, HocVienService hv, AppDbContext db, IKhoaHocService khoaHoc) : Controller
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private MenuTaiKhoan Menu => khoaHoc.LayKhoaHoc(null).Menu with { MucDangMo = "DiemYeu" };
    private async Task<List<string>> KhoaCuaToi() => (await hv.KhoaCuaToi(Uid)).Select(k => k.Id).ToList();

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var ds = await phanTich.TheoBaiAsync(Uid, await KhoaCuaToi(), ct);
        return View(new DiemYeuViewModel { Menu = Menu, TatCa = ds, Yeu = PhanTichDiemYeuService.Yeu(ds).ToList(), Manh = PhanTichDiemYeuService.Manh(ds).ToList(), CoAi = ai.CoAi });
    }

    [HttpGet("DeOn")]
    public async Task<IActionResult> DeOn(CancellationToken ct)
    {
        var khoa = await KhoaCuaToi();
        return View(new OnTapViewModel { Menu = Menu, Cau = await phanTich.DeOnAsync(Uid, khoa, 10, ct) });
    }

    // AI tạo thêm câu trắc nghiệm cho một bài (không lưu; chỉ để ôn). Chỉ bài của khoá học viên đang học
    [HttpPost("AiCauHoi"), ValidateAntiForgeryToken, EnableRateLimiting("tro-ly")]
    public async Task<IActionResult> AiCauHoi(int baiId, CancellationToken ct)
    {
        var khoaBai = await db.Bai.AsNoTracking().Where(b => b.Id == baiId).Select(b => b.ChuDe!.KhoaId).FirstOrDefaultAsync(ct);
        var nhanVien = User.IsInRole(VaiTro.Admin) || User.IsInRole(VaiTro.GiangVien);
        if (khoaBai is null || !(nhanVien || (await KhoaCuaToi()).Contains(khoaBai))) return NotFound();
        var (cau, loi) = await ai.TaoCauHoiAsync(baiId, 5, ct);
        return Json(new { loi, cau = cau.Select(c => new { c.NoiDung, c.PhuongAn, c.DapAn, c.GiaiThich }) });
    }
}
