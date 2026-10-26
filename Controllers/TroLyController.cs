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
        if (string.IsNullOrWhiteSpace(req.CauHoi) || req.CauHoi.Length > 500)
            return BadRequest(new { loi = "Câu hỏi trống hoặc dài quá 500 ký tự." });

        // Lịch sử do trình duyệt gửi: chỉ giữ 3 lượt cuối, cắt bớt nội dung quá dài
        var lichSu = (req.LichSu ?? [])
            .Where(l => !string.IsNullOrWhiteSpace(l.CauHoi) && l.TraLoi is not null)
            .TakeLast(3)
            .Select(l => new LuotHoi(CatNgan(l.CauHoi, 500), CatNgan(l.TraLoi, 1500)))
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

    private static string CatNgan(string s, int toiDa) => s.Length <= toiDa ? s : s[..toiDa];
}
