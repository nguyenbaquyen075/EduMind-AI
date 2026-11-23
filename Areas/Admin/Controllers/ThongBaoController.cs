using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Areas.Admin.Controllers;

public class ThongBaoController(AppDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? trangThai, string? quyetDinh, CancellationToken ct) => View(new AdminThongBaoViewModel
    {
        TrangThai = trangThai, QuyetDinh = quyetDinh,
        ThongBao = await Db.ThongBao.AsNoTracking().Include(t => t.DonHang).Where(t => trangThai == null || t.TrangThai == trangThai).OrderByDescending(t => t.TaoLuc).Take(200).ToListAsync(ct),
        NhatKy = await Db.NhatKyDuyet.AsNoTracking().Include(n => n.DonHang).Where(n => quyetDinh == null || n.QuyetDinh == quyetDinh).OrderByDescending(n => n.ThoiDiem).Take(200).ToListAsync(ct)
    });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiLai(int id, CancellationToken ct)
    {
        var changed = await Db.ThongBao.Where(t => t.Id == id && t.TrangThai == TrangThaiThongBao.Loi).ExecuteUpdateAsync(s => s.SetProperty(t => t.TrangThai, TrangThaiThongBao.ChoGui).SetProperty(t => t.GuiTiepLuc, DateTime.UtcNow).SetProperty(t => t.LoiCuoi, (string?)null), ct);
        if (changed == 1) ThongBao("Đã lên lịch gửi lại thông báo."); else BaoLoi("Chỉ có thể gửi lại thông báo lỗi.");
        return RedirectToAction(nameof(Index));
    }
}

public class AdminThongBaoViewModel
{
    public required IReadOnlyList<ThongBao> ThongBao { get; init; }
    public required IReadOnlyList<NhatKyDuyet> NhatKy { get; init; }
    public string? TrangThai { get; init; }
    public string? QuyetDinh { get; init; }
}
