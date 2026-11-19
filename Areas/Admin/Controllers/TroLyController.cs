using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;
using SuDia.Web.Services.TroLy;

namespace SuDia.Web.Areas.Admin.Controllers;

public class TroLyController(AppDbContext db, IAiService ai, IOptions<TroLyOptions> options, IWebHostEnvironment env, IServiceProvider sp) : AdminControllerBase(db)
{
    // Nạp lại chỉ mục ngay (thường không cần: lưu bài/khoá/gói là tự nạp lại)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LamMoi(CancellationToken ct)
    {
        await sp.GetRequiredService<DongBoChiMuc>().ChayAsync([new YeuCauChiMuc("TatCa")], ct);
        ThongBao("Đã nạp lại tài liệu cho trợ lý.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Index()
    {
        var opt = options.Value;
        var dir = Path.Combine(env.ContentRootPath, opt.ThuMucTaiLieu);
        var taiLieu = new List<AdminTaiLieu>();
        if (Directory.Exists(dir))
            foreach (var file in Directory.EnumerateFiles(dir, "*.md").OrderBy(Path.GetFileName))
            {
                var soBai = 0; var soMuc = 0; var mon = "Chưa ghi môn";
                foreach (var line in System.IO.File.ReadLines(file))
                {
                    if (line.StartsWith("## ")) soBai++;
                    if (line.StartsWith("### ")) soMuc++;
                    if (line.StartsWith("mon:", StringComparison.OrdinalIgnoreCase)) mon = line[4..].Trim();
                }
                taiLieu.Add(new(Path.GetFileName(file), mon, soBai, soMuc, new FileInfo(file).Length));
            }
        var dem = 0;
        var boCauHoi = Path.Combine(env.ContentRootPath, "KiemThu", "bo-cau-hoi.json");
        if (System.IO.File.Exists(boCauHoi))
        {
            using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(boCauHoi));
            if (doc.RootElement.ValueKind == JsonValueKind.Array) dem = doc.RootElement.GetArrayLength();
        }
        return View(new AdminTroLyViewModel { BanXemThu = ai is DemoAiService, CauHinh = opt,
            DangDung = ai switch { GeminiRagAiService => $"Gemini · {opt.ModelGemini}", RagAiService => $"Claude · {opt.ModelTraLoi}", _ => "Bản xem thử" }, TaiLieu = taiLieu,
            SoCauKiemThu = dem, CoPrompt = System.IO.File.Exists(Path.Combine(env.ContentRootPath, opt.FilePrompt)),
            ChiMuc = await sp.GetRequiredService<KhoTaiLieu>().ThongKeAsync(), CanBoSung = KiemTraDuLieu() });
    }

    // Dữ liệu nhập thiếu thì trợ lý nói "chưa có trong tài liệu" hoặc đưa thông tin sai cho khách
    private List<string> KiemTraDuLieu()
    {
        var ds = new List<string>();
        var khoa = Db.Khoa.AsNoTracking().Where(k => k.HienThi).OrderBy(k => k.ThuTu)
            .Select(k => new { k.Ten, k.MoTa, k.MucTieu, SoBai = k.ChuDe.SelectMany(c => c.Bai).Count(b => b.DaXuatBan) }).ToList();
        foreach (var k in khoa)
        {
            if (k.SoBai == 0) ds.Add($"Khoá \"{k.Ten}\" chưa có bài nào xuất bản: trợ lý chưa có nội dung bài học để trả lời.");
            if (string.IsNullOrWhiteSpace(k.MoTa)) ds.Add($"Khoá \"{k.Ten}\" chưa có mô tả: khách hỏi về khoá này sẽ chỉ nhận danh sách bài.");
            if (string.IsNullOrWhiteSpace(k.MucTieu)) ds.Add($"Khoá \"{k.Ten}\" chưa ghi mục tiêu học xong.");
        }
        foreach (var g in Db.GoiBan.AsNoTracking().Where(g => g.DangBan && (g.Gia == null || g.Gia <= 0)).Select(g => g.Ten))
            ds.Add($"Gói \"{g}\" đang mở bán nhưng chưa có giá: trợ lý sẽ trả lời \"chưa công bố giá\".");
        return ds;
    }
}
