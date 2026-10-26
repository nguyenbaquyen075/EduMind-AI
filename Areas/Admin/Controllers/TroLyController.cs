using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;
using SuDia.Web.Services.TroLy;

namespace SuDia.Web.Areas.Admin.Controllers;

public class TroLyController(AppDbContext db, IAiService ai, IOptions<TroLyOptions> options, IWebHostEnvironment env) : AdminControllerBase(db)
{
    public IActionResult Index()
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
        return View(new AdminTroLyViewModel { BanXemThu = ai is DemoAiService, CauHinh = opt, TaiLieu = taiLieu,
            SoCauKiemThu = dem, CoPrompt = System.IO.File.Exists(Path.Combine(env.ContentRootPath, opt.FilePrompt)) });
    }
}
