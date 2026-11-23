using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Controllers;

// Lớp học của tôi + diễn đàn hỏi đáp trong lớp. Giảng viên của khoá và admin cũng vào được để trả lời / điều hành
[Authorize, Route("Lop")]
public class LopHocController(AppDbContext db, LopHocService svc, HocVienService hv, IKhoaHocService khoaHoc) : Controller
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
            .Select(l => new LopTomTat(l.Id, l.Ten, l.Khoa!.Ten, l.MaThamGia, l.DangMo, l.HocVien.Count, l.BaiDang.Count, LaAdmin || (LaGiangVien && l.Khoa!.GiangVienId == Uid))).ToListAsync();
        return View(new LopCuaToiViewModel { Menu = Menu, Lop = ds, MaNhap = ma });
    }

    [HttpPost("Vao"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Vao(string? ma, CancellationToken ct)
    {
        var chuan = LopHocService.ChuanHoaMa(ma);
        var lop = chuan.Length == 0 ? null : await db.Lop.Include(l => l.Khoa).FirstOrDefaultAsync(l => l.MaThamGia == chuan, ct);
        if (lop is null || !lop.DangMo) { BaoLoi("Mã lớp không đúng hoặc lớp đã ngừng nhận học viên."); return RedirectToAction(nameof(Index), new { ma }); }
        if (await db.LopHocVien.AnyAsync(m => m.LopId == lop.Id && m.NguoiDungId == Uid, ct)) return RedirectToAction(nameof(DienDan), new { id = lop.Id });
        // Phải đang học khoá của lớp (ghi danh còn hạn)
        if (!(await hv.KhoaCuaToi(Uid)).Any(k => k.Id == lop.KhoaId))
        {
            BaoLoi($"Bạn chưa ghi danh (hoặc đã hết hạn) khoá “{lop.Khoa!.Ten}” nên chưa vào được lớp này.");
            return RedirectToAction(nameof(Index), new { ma });
        }
        db.LopHocVien.Add(new LopHocVien { LopId = lop.Id, NguoiDungId = Uid });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { /* bấm hai lần cùng lúc: đã vào rồi */ }
        ThongBao($"Đã vào lớp “{lop.Ten}”.");
        return RedirectToAction(nameof(DienDan), new { id = lop.Id });
    }

    [HttpPost("{id:int}/Roi"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Roi(int id, CancellationToken ct)
    {
        await db.LopHocVien.Where(m => m.LopId == id && m.NguoiDungId == Uid).ExecuteDeleteAsync(ct);
        ThongBao("Bạn đã rời lớp. Muốn vào lại, nhập mã lớp.");
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------ Diễn đàn
    [HttpGet("{id:int}")]
    public async Task<IActionResult> DienDan(int id, CancellationToken ct)
    {
        var (lop, quanLy, _) = await svc.QuyenAsync(id, Uid, LaAdmin, LaGiangVien, ct);
        if (lop is null) return NotFound();
        var gv = lop.Khoa!.GiangVienId;
        var ds = (await db.BaiDang.AsNoTracking().Where(b => b.LopId == id)
            .OrderByDescending(b => b.Ghim).ThenByDescending(b => b.TraLoi.Max(t => (DateTime?)t.NgayTao) ?? b.NgayTao).Take(100)
            .Select(b => new { b.Id, b.TieuDe, b.NguoiDung!.HoTen, b.NguoiDungId, b.NgayTao, SoTraLoi = b.TraLoi.Count, b.Ghim, b.DaDong, CoDapAn = b.TraLoi.Any(t => t.LaDapAn),
                Cuoi = b.TraLoi.Max(t => (DateTime?)t.NgayTao) }).ToListAsync())
            .Select(b => new BaiDangTomTat(b.Id, b.TieuDe, b.HoTen, b.NguoiDungId == gv, b.NgayTao, b.SoTraLoi, b.Ghim, b.DaDong, b.CoDapAn, b.Cuoi ?? b.NgayTao)).ToList();
        var soHv = await db.LopHocVien.CountAsync(m => m.LopId == id, ct);
        return View(new DienDanViewModel
        {
            Menu = Menu, QuanLy = quanLy, MoTa = lop.MoTa, BaiDang = ds,
            Lop = new LopTomTat(lop.Id, lop.Ten, lop.Khoa!.Ten, lop.MaThamGia, lop.DangMo, soHv, ds.Count, quanLy),
            TieuDeNhap = TempData["TieuDeNhap"] as string, NoiDungNhap = TempData["NoiDungNhap"] as string,
        });
    }

    [HttpPost("{id:int}/DangBai"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DangBai(int id, string? tieuDe, string? noiDung, CancellationToken ct)
    {
        var (lop, _, _) = await svc.QuyenAsync(id, Uid, LaAdmin, LaGiangVien, ct);
        if (lop is null) return NotFound();
        tieuDe = (tieuDe ?? "").Trim(); noiDung = (noiDung ?? "").Trim();
        string? loi = tieuDe.Length is 0 or > GioiHanDienDan.TieuDe ? $"Tiêu đề cần từ 1 đến {GioiHanDienDan.TieuDe} ký tự."
            : noiDung.Length is 0 or > GioiHanDienDan.NoiDung ? $"Nội dung cần từ 1 đến {GioiHanDienDan.NoiDung:N0} ký tự."
            : await svc.QuaNhieuBaiAsync(Uid, ct) ? "Bạn đăng bài hơi nhiều trong một giờ. Vui lòng thử lại sau." : null;
        if (loi is not null)
        {
            BaoLoi(loi); TempData["TieuDeNhap"] = tieuDe; TempData["NoiDungNhap"] = noiDung;
            return RedirectToAction(nameof(DienDan), new { id });
        }
        var b = new BaiDang { LopId = id, NguoiDungId = Uid, TieuDe = tieuDe, NoiDung = noiDung };
        db.BaiDang.Add(b);
        await db.SaveChangesAsync(ct);
        ThongBao("Đã đăng bài.");
        return RedirectToAction(nameof(BaiDangChiTiet), new { id = b.Id });
    }

    private async Task<(BaiDang? Bai, bool QuanLy)> MoBai(int baiId, CancellationToken ct)
    {
        var b = await db.BaiDang.Include(x => x.Lop).FirstOrDefaultAsync(x => x.Id == baiId, ct);
        if (b is null) return (null, false);
        var (lop, quanLy, _) = await svc.QuyenAsync(b.LopId, Uid, LaAdmin, LaGiangVien, ct);
        return lop is null ? (null, false) : (b, quanLy);
    }

    [HttpGet("BaiDang/{id:int}")]
    public async Task<IActionResult> BaiDangChiTiet(int id, CancellationToken ct)
    {
        var (b, quanLy) = await MoBai(id, ct);
        if (b is null) return NotFound();
        var gv = await db.Khoa.Where(k => k.Id == b.Lop!.KhoaId).Select(k => k.GiangVienId).FirstAsync(ct);
        var tacGia = await db.Users.Where(u => u.Id == b.NguoiDungId).Select(u => u.HoTen).FirstAsync(ct);
        var tl = (await db.TraLoiBaiDang.AsNoTracking().Where(t => t.BaiDangId == id).OrderByDescending(t => t.LaDapAn).ThenBy(t => t.NgayTao).Take(200)
            .Select(t => new { t.Id, t.NguoiDung!.HoTen, t.NguoiDungId, t.NoiDung, t.NgayTao, t.LaDapAn }).ToListAsync())
            .Select(t => new TraLoiHienThi(t.Id, t.HoTen, t.NguoiDungId == gv, t.NoiDung, t.NgayTao, t.LaDapAn, quanLy || t.NguoiDungId == Uid)).ToList();
        return View(new BaiDangViewModel
        {
            Menu = Menu, LopId = b.LopId, LopTen = b.Lop!.Ten, Id = b.Id, TieuDe = b.TieuDe, NoiDung = b.NoiDung, TacGia = tacGia,
            TacGiaLaGiangVien = b.NguoiDungId == gv, NgayTao = b.NgayTao, Ghim = b.Ghim, DaDong = b.DaDong,
            QuanLy = quanLy, LaTacGia = b.NguoiDungId == Uid, TraLoi = tl, TraLoiNhap = TempData["TraLoiNhap"] as string,
        });
    }

    [HttpPost("BaiDang/{id:int}/TraLoi"), ValidateAntiForgeryToken]
    public async Task<IActionResult> TraLoi(int id, string? noiDung, CancellationToken ct)
    {
        var (b, _) = await MoBai(id, ct);
        if (b is null) return NotFound();
        noiDung = (noiDung ?? "").Trim();
        string? loi = b.DaDong ? "Bài này đã đóng, không trả lời thêm được."
            : noiDung.Length is 0 or > GioiHanDienDan.NoiDung ? $"Nội dung cần từ 1 đến {GioiHanDienDan.NoiDung:N0} ký tự."
            : await svc.QuaNhieuTraLoiAsync(Uid, ct) ? "Bạn trả lời hơi nhiều trong một giờ. Vui lòng thử lại sau." : null;
        if (loi is not null) { BaoLoi(loi); TempData["TraLoiNhap"] = noiDung; }
        else { db.TraLoiBaiDang.Add(new TraLoiBaiDang { BaiDangId = id, NguoiDungId = Uid, NoiDung = noiDung }); await db.SaveChangesAsync(ct); ThongBao("Đã gửi trả lời."); }
        return RedirectToAction(nameof(BaiDangChiTiet), new { id });
    }

    // Giảng viên / admin: ghim, đóng bài
    [HttpPost("BaiDang/{id:int}/Ghim"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Ghim(int id, CancellationToken ct)
    {
        var (b, quanLy) = await MoBai(id, ct);
        if (b is null || !quanLy) return NotFound();
        b.Ghim = !b.Ghim; await db.SaveChangesAsync(ct);
        ThongBao(b.Ghim ? "Đã ghim bài lên đầu diễn đàn." : "Đã bỏ ghim.");
        return RedirectToAction(nameof(BaiDangChiTiet), new { id });
    }

    [HttpPost("BaiDang/{id:int}/Dong"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Dong(int id, CancellationToken ct)
    {
        var (b, quanLy) = await MoBai(id, ct);
        if (b is null || !quanLy) return NotFound();
        b.DaDong = !b.DaDong; await db.SaveChangesAsync(ct);
        ThongBao(b.DaDong ? "Đã đóng bài: không ai trả lời thêm được." : "Đã mở lại bài.");
        return RedirectToAction(nameof(BaiDangChiTiet), new { id });
    }

    // Người hỏi hoặc giảng viên đánh dấu một trả lời là đáp án (bấm lại để bỏ)
    [HttpPost("BaiDang/{id:int}/DapAn/{traLoiId:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DapAn(int id, int traLoiId, CancellationToken ct)
    {
        var (b, quanLy) = await MoBai(id, ct);
        if (b is null || !(quanLy || b.NguoiDungId == Uid)) return NotFound();
        var t = await db.TraLoiBaiDang.FirstOrDefaultAsync(x => x.Id == traLoiId && x.BaiDangId == id, ct);
        if (t is null) return NotFound();
        if (!t.LaDapAn) await db.TraLoiBaiDang.Where(x => x.BaiDangId == id && x.LaDapAn).ExecuteUpdateAsync(s => s.SetProperty(x => x.LaDapAn, false), ct);
        t.LaDapAn = !t.LaDapAn; await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(BaiDangChiTiet), new { id });
    }

    [HttpPost("BaiDang/{id:int}/Xoa"), ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaBai(int id, CancellationToken ct)
    {
        var (b, quanLy) = await MoBai(id, ct);
        if (b is null || !(quanLy || b.NguoiDungId == Uid)) return NotFound();
        var lopId = b.LopId;
        db.BaiDang.Remove(b); await db.SaveChangesAsync(ct);
        ThongBao("Đã xoá bài.");
        return RedirectToAction(nameof(DienDan), new { id = lopId });
    }

    [HttpPost("BaiDang/{id:int}/TraLoi/{traLoiId:int}/Xoa"), ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaTraLoi(int id, int traLoiId, CancellationToken ct)
    {
        var (b, quanLy) = await MoBai(id, ct);
        if (b is null) return NotFound();
        var t = await db.TraLoiBaiDang.FirstOrDefaultAsync(x => x.Id == traLoiId && x.BaiDangId == id, ct);
        if (t is null || !(quanLy || t.NguoiDungId == Uid)) return NotFound();
        db.TraLoiBaiDang.Remove(t); await db.SaveChangesAsync(ct);
        ThongBao("Đã xoá trả lời.");
        return RedirectToAction(nameof(BaiDangChiTiet), new { id });
    }
}
