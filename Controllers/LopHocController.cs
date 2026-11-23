using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Controllers;

// Lớp học của tôi + nhóm chat của lớp (SignalR). Giảng viên của khoá và admin cũng vào được để nhắn / điều hành
[Authorize, Route("Lop")]
public class LopHocController(AppDbContext db, LopHocService svc, HocVienService hv, IKhoaHocService khoaHoc, LopChatService chat) : Controller
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool LaAdmin => User.IsInRole(VaiTro.Admin);
    private bool LaGiangVien => User.IsInRole(VaiTro.GiangVien);
    private MenuTaiKhoan Menu => khoaHoc.LayKhoaHoc(null).Menu with { MucDangMo = "LopHoc" };
    private void ThongBao(string s) => TempData["ThongBao"] = s;
    private void BaoLoi(string s) => TempData["Loi"] = s;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? ma)
    {
        var lop = db.Lop.AsNoTracking().Where(l => l.HocVien.Any(m => m.NguoiDungId == Uid) || LaAdmin || (LaGiangVien && l.Khoa!.GiangVienId == Uid));
        var ds = await lop.OrderBy(l => l.Khoa!.ThuTu).ThenBy(l => l.Ten)
            .Select(l => new LopTomTat(l.Id, l.Ten, l.Khoa!.Ten, l.MaThamGia, l.DangMo, l.HocVien.Count, l.TinNhan.LongCount(), LaAdmin || (LaGiangVien && l.Khoa!.GiangVienId == Uid))).ToListAsync();
        return View(new LopCuaToiViewModel { Menu = Menu, Lop = ds, MaNhap = ma });
    }

    [HttpPost("Vao"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Vao(string? ma, CancellationToken ct)
    {
        var chuan = LopHocService.ChuanHoaMa(ma);
        var lop = chuan.Length == 0 ? null : await db.Lop.Include(l => l.Khoa).FirstOrDefaultAsync(l => l.MaThamGia == chuan, ct);
        if (lop is null || !lop.DangMo) { BaoLoi("Mã lớp không đúng hoặc lớp đã ngừng nhận học viên."); return RedirectToAction(nameof(Index), new { ma }); }
        if (await db.LopHocVien.AnyAsync(m => m.LopId == lop.Id && m.NguoiDungId == Uid, ct)) return RedirectToAction(nameof(NhomChat), new { id = lop.Id });
        // Phải đang học khoá của lớp (ghi danh còn hạn)
        if (!(await hv.KhoaCuaToi(Uid)).Any(k => k.Id == lop.KhoaId))
        {
            BaoLoi($"Bạn chưa ghi danh (hoặc đã hết hạn) khoá “{lop.Khoa!.Ten}” nên chưa vào được lớp này.");
            return RedirectToAction(nameof(Index), new { ma });
        }
        db.LopHocVien.Add(new LopHocVien { LopId = lop.Id, NguoiDungId = Uid });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { /* bấm hai lần cùng lúc: đã vào rồi */ }
        ThongBao($"Đã vào lớp “{lop.Ten}”.");
        return RedirectToAction(nameof(NhomChat), new { id = lop.Id });
    }

    [HttpPost("{id:int}/Roi"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Roi(int id, CancellationToken ct)
    {
        await db.LopHocVien.Where(m => m.LopId == id && m.NguoiDungId == Uid).ExecuteDeleteAsync(ct);
        ThongBao("Bạn đã rời lớp. Muốn vào lại, nhập mã lớp.");
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------ Nhóm chat của lớp
    [HttpGet("{id:int}")]
    public async Task<IActionResult> NhomChat(int id, CancellationToken ct)
    {
        var (lop, quanLy, _) = await svc.QuyenAsync(id, Uid, LaAdmin, LaGiangVien, ct);
        if (lop is null) return NotFound();
        var (tin, coCu) = await chat.LichSuAsync(id, null, ct);
        var soHv = await db.LopHocVien.CountAsync(m => m.LopId == id, ct);
        var soTin = await db.TinNhanLop.LongCountAsync(t => t.LopId == id, ct);
        var ten = await db.LopHocVien.AsNoTracking().Where(m => m.LopId == id).OrderBy(m => m.NguoiDung!.HoTen).Select(m => m.NguoiDung!.HoTen).Take(200).ToListAsync(ct);
        var gv = await db.Users.AsNoTracking().Where(u => u.Id == lop.Khoa!.GiangVienId).Select(u => u.HoTen).FirstOrDefaultAsync(ct);
        return View(new NhomChatViewModel
        {
            Menu = Menu, QuanLy = quanLy, MoTa = lop.MoTa, NguoiDungId = Uid, TinNhan = tin, CoTinCu = coCu,
            Lop = new LopTomTat(lop.Id, lop.Ten, lop.Khoa!.Ten, lop.MaThamGia, lop.DangMo, soHv, soTin, quanLy),
            ThanhVien = (gv is null ? ten : ten.Prepend(gv + " (giảng viên)")).ToList(),
        });
    }

    // Lịch sử cũ hơn khi bấm "Xem tin cũ hơn"
    [HttpGet("{id:int}/TinNhan")]
    public async Task<IActionResult> TinNhan(int id, long? truoc, CancellationToken ct)
    {
        var (lop, _, _) = await svc.QuyenAsync(id, Uid, LaAdmin, LaGiangVien, ct);
        if (lop is null) return NotFound();
        var (tin, coCu) = await chat.LichSuAsync(id, truoc, ct);
        Response.Headers.CacheControl = "no-store";
        return Json(new { tin, coCu });
    }

    [HttpPost("{id:int}/Gui"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Gui(int id, [FromForm] string? noiDung, CancellationToken ct)
    {
        var (lop, quanLy, _) = await svc.QuyenAsync(id, Uid, LaAdmin, LaGiangVien, ct);
        if (lop is null) return NotFound();
        var (tin, loi) = await chat.GuiAsync(lop, Uid, quanLy, noiDung, ct);
        return loi is null ? Json(new { tin }) : BadRequest(new { loi });
    }

    [HttpPost("{id:int}/TinNhan/{tinId:long}/Xoa"), ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaTin(int id, long tinId, CancellationToken ct)
    {
        var (lop, quanLy, _) = await svc.QuyenAsync(id, Uid, LaAdmin, LaGiangVien, ct);
        if (lop is null) return NotFound();
        return await chat.XoaAsync(lop, tinId, Uid, quanLy, ct) ? Ok() : NotFound();
    }
}
