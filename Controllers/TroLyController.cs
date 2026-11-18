using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SuDia.Web.Models;
using SuDia.Web.Services;
using SuDia.Web.Services.TroLy;

namespace SuDia.Web.Controllers;

[Route("api/tro-ly")]
public class TroLyController : Controller
{
    private readonly IAiService _ai;
    public TroLyController(IAiService ai) => _ai = ai;

    // POST /api/tro-ly/hoi  — khung "Học cùng AI" gọi vào đây
    [HttpPost("hoi")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("tro-ly")]
    public async Task<IActionResult> Hoi([FromBody] HoiAiRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.CauHoi) || req.CauHoi.Length > GioiHanHoiAi.CauHoi)
            return BadRequest(new { loi = $"Câu hỏi trống hoặc dài quá {GioiHanHoiAi.CauHoi:N0} ký tự." });

        // Lịch sử do trình duyệt gửi: chỉ giữ 6 lượt cuối, cắt bớt nội dung quá dài
        var lichSu = (req.LichSu ?? [])
            .Where(l => !string.IsNullOrWhiteSpace(l.CauHoi) && l.TraLoi is not null)
            .TakeLast(6)
            .Select(l => new LuotHoi(CatNgan(l.CauHoi, GioiHanHoiAi.CauHoi), CatNgan(l.TraLoi, 1500)))
            .ToList();

        var res = await _ai.HoiAsync(req with { LichSu = lichSu }, ct);
        return Ok(res);
    }

    // GET /api/tro-ly/kiem-thu  — chạy bộ câu hỏi kiểm thử (Phần 6). Chỉ bật khi Development vì tốn lượt gọi API
    [HttpGet("kiem-thu")]
    public async Task<IActionResult> KiemThu([FromServices] IWebHostEnvironment env, CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();
        var file = Path.Combine(env.ContentRootPath, "KiemThu", "bo-cau-hoi.json");
        return Ok(await BoKiemThu.ChayAsync(_ai, file, ct));
    }

    // GET /api/tro-ly/chi-muc?q=…&khoa=…  — xem các đoạn tài liệu sẽ được gửi cho AI (không gọi AI). Chỉ Development
    [HttpGet("chi-muc")]
    public async Task<IActionResult> ChiMuc([FromServices] IWebHostEnvironment env, [FromServices] KhoTaiLieu kho,
        [FromServices] DongBoChiMuc dongBo, string? q, string? khoa, CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();
        var doan = string.IsNullOrWhiteSpace(q) ? [] : await kho.TimKiemAsync(q, string.IsNullOrWhiteSpace(khoa) ? null : khoa, 6, ct);
        return Ok(new { thongKe = await kho.ThongKeAsync(ct), soLanCapNhat = dongBo.SoViecXong, doan = doan.Select(d => new { d.Khoa, d.Nguon, d.NoiDung }) });
    }

    private static string CatNgan(string s, int toiDa) => s.Length <= toiDa ? s : s[..toiDa];
}
