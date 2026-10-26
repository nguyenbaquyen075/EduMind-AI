using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Controllers;

[Authorize]
public class BaiTapController(AppDbContext db) : Controller
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool NhanVien => User.IsInRole(VaiTro.Admin) || User.IsInRole(VaiTro.GiangVien);
    private IQueryable<Bai> BaiDuocHoc() => db.Bai.Where(b =>
        User.IsInRole(VaiTro.Admin) || (User.IsInRole(VaiTro.GiangVien) && b.ChuDe!.Khoa!.GiangVienId == Uid) ||
        (!NhanVien && b.DaXuatBan && (b.HocThu || db.GhiDanh.Any(g => g.NguoiDungId == Uid && g.KhoaId == b.ChuDe!.KhoaId && (g.HanDen == null || g.HanDen >= DateTime.UtcNow)))));
    private IQueryable<BaiTapGiao> DuocXem()
    {
        var baiIds = BaiDuocHoc().Select(b => b.Id);
        return db.BaiTapGiao.Where(b => (NhanVien || b.DaGiao) && baiIds.Contains(b.BaiId));
    }

    public async Task<IActionResult> Index(int bai)
    {
        var b = await BaiDuocHoc().AsNoTracking().FirstOrDefaultAsync(b => b.Id == bai);
        if (b is null) return NotFound();
        ViewBag.Bai = b;
        return View(await DuocXem().AsNoTracking().Include(b => b.BuoiHoc).Where(b => b.BaiId == bai).OrderByDescending(b => b.NgayTao).ToListAsync());
    }

    public async Task<IActionResult> Lam(int id)
    {
        var bt = await DuocXem().AsNoTracking().Include(b => b.BuoiHoc).FirstOrDefaultAsync(b => b.Id == id);
        if (bt is null) return NotFound();
        var nop = await db.BaiNopGiao.AsNoTracking().FirstOrDefaultAsync(n => n.BaiTapGiaoId == id && n.NguoiDungId == Uid);
        return View(Model(bt, nop));
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> Lam(int id, IFormCollection form)
    {
        var bt = await DuocXem().AsNoTracking().Include(b => b.BuoiHoc).FirstOrDefaultAsync(b => b.Id == id);
        if (bt is null) return NotFound();
        if (!NhanVien && bt.HanNop < DateTime.UtcNow) { TempData["LoiBaiTap"] = "Bài tập đã hết hạn nộp."; return RedirectToAction(nameof(Lam), new { id }); }
        if (!NhanVien && await db.BaiNopGiao.AnyAsync(n => n.BaiTapGiaoId == id && n.NguoiDungId == Uid)) return RedirectToAction(nameof(Lam), new { id });
        var cau = NoiDungBaiTap.Doc(bt.NoiDungJson);
        if (NoiDungBaiTap.KiemTra(cau, true) is string loiNhap) return BadRequest($"Hoàn thiện bài tập trước khi làm thử: {loiNhap}");
        var traLoi = new Dictionary<string, string>();
        foreach (var c in cau)
        {
            if (c.Kieu == "DS")
            {
                for (var j = 0; j < c.Y.Count; j++)
                {
                    var key = $"{c.Id}-{j}";
                    if (form[key].ToString() is "true" or "false") traLoi[key] = form[key].ToString();
                }
            }
            else
            {
                var value = form[c.Id].ToString();
                if (value.Length > 20000) return BadRequest("Câu trả lời tối đa 20.000 ký tự.");
                if (c.Kieu == "TL" || value is "0" or "1" or "2" or "3") traLoi[c.Id] = value;
            }
        }
        var nop = new BaiNopGiao { BaiTapGiaoId = id, NguoiDungId = Uid, TraLoiJson = NoiDungBaiTap.Viet(traLoi),
            DiemTuDong = NoiDungBaiTap.Cham(cau, traLoi), DiemTuLuan = cau.Any(c => c.Kieu == "TL") ? null : 0 };
        if (NhanVien) return View(Model(bt, nop));
        db.BaiNopGiao.Add(nop);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        { return RedirectToAction(nameof(Lam), new { id }); }
        return RedirectToAction(nameof(Lam), new { id });
    }

    private LamBaiGiaoViewModel Model(BaiTapGiao bt, BaiNopGiao? nop) => new()
    {
        BaiTap = bt, Cau = NoiDungBaiTap.Doc(bt.NoiDungJson), BaiNop = nop, XemTruoc = NhanVien,
        TraLoi = nop is null ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(nop.TraLoiJson) ?? [],
    };
}
