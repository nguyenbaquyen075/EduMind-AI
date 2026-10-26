using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

// Khoá học: thông tin, giảng viên phụ trách; Ghi danh: học viên ↔ khoá, hạn học
public class KhoaHocController(AppDbContext db) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Khoa = await Db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu)
            .Select(k => new { k.Id, k.Ten, k.KyHieu, k.GiangVienId, GiangVien = k.GiangVien != null ? k.GiangVien.HoTen : null,
                SoBai = k.ChuDe.SelectMany(c => c.Bai).Count(), SoHocVien = Db.GhiDanh.Count(g => g.KhoaId == k.Id) }).ToListAsync();
        ViewBag.DsGiangVien = await DanhSachGiangVien();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GanGiangVien(string id, string? giangVienId)
    {
        var k = await Db.Khoa.FindAsync(id);
        if (k is null) return NotFound();
        if (!string.IsNullOrEmpty(giangVienId) && !(await DanhSachGiangVien()).Any(g => g.Value == giangVienId))
        { BaoLoi("Chọn tài khoản giảng viên hoặc admin hợp lệ."); return RedirectToAction(nameof(Index)); }
        k.GiangVienId = string.IsNullOrEmpty(giangVienId) ? null : giangVienId;
        await Db.SaveChangesAsync();
        ThongBao($"Đã cập nhật giảng viên khoá {k.Ten}.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Sua(string id)
    {
        var k = await Db.Khoa.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound();
        ViewBag.DsGiangVien = await DanhSachGiangVien();
        return View(new KhoaForm { Id = k.Id, Ten = k.Ten, KyHieu = k.KyHieu, MoTa = k.MoTa, Anh = k.Anh, GiangVienId = k.GiangVienId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(KhoaForm f)
    {
        var k = await Db.Khoa.FindAsync(f.Id);
        if (k is null) return NotFound();
        if (!string.IsNullOrEmpty(f.GiangVienId) && !(await DanhSachGiangVien()).Any(g => g.Value == f.GiangVienId))
            ModelState.AddModelError(nameof(f.GiangVienId), "Chọn giảng viên hợp lệ.");
        if (!ModelState.IsValid) { ViewBag.DsGiangVien = await DanhSachGiangVien(); return View(f); }
        k.Ten = f.Ten.Trim(); k.KyHieu = f.KyHieu.Trim(); k.MoTa = f.MoTa?.Trim() ?? ""; k.Anh = f.Anh?.Trim() ?? "";
        k.GiangVienId = string.IsNullOrEmpty(f.GiangVienId) ? null : f.GiangVienId;
        await Db.SaveChangesAsync();
        ThongBao("Đã lưu thông tin khoá.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Tao()
    {
        ViewBag.DsGiangVien = await DanhSachGiangVien();
        return View("Sua", new KhoaForm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(KhoaForm f)
    {
        var ds = await DanhSachGiangVien();
        if (string.IsNullOrWhiteSpace(f.Id) || !System.Text.RegularExpressions.Regex.IsMatch(f.Id, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || f.Id.Length > 80)
            ModelState.AddModelError(nameof(f.Id), "Mã khoá gồm chữ thường không dấu, số và dấu gạch nối, tối đa 80 ký tự.");
        else if (await Db.Khoa.AnyAsync(k => k.Id == f.Id)) ModelState.AddModelError(nameof(f.Id), "Mã khoá đã tồn tại.");
        if (!string.IsNullOrEmpty(f.GiangVienId) && !ds.Any(g => g.Value == f.GiangVienId)) ModelState.AddModelError(nameof(f.GiangVienId), "Chọn giảng viên hợp lệ.");
        if (!ModelState.IsValid) { ViewBag.DsGiangVien = ds; return View("Sua", f); }
        Db.Khoa.Add(new Khoa { Id = f.Id, Ten = f.Ten.Trim(), KyHieu = f.KyHieu.Trim(), MoTa = f.MoTa?.Trim() ?? "", Anh = f.Anh?.Trim() ?? "",
            GiangVienId = string.IsNullOrEmpty(f.GiangVienId) ? null : f.GiangVienId, ThuTu = (await Db.Khoa.MaxAsync(k => (int?)k.ThuTu) ?? -1) + 1 });
        await Db.SaveChangesAsync();
        ThongBao($"Đã tạo khoá {f.Ten}. Thêm chủ đề và bài học tại Nội dung bài học.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> GhiDanh(string id)
    {
        var k = await Db.Khoa.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound();
        var baiIds = await Db.Bai.Where(b => b.ChuDe!.KhoaId == id && b.DaXuatBan).Select(b => b.Id).ToListAsync();
        ViewBag.Khoa = k;
        ViewBag.TongBai = baiIds.Count;
        ViewBag.Ds = await Db.GhiDanh.AsNoTracking().Where(g => g.KhoaId == id).OrderBy(g => g.NguoiDung!.HoTen)
            .Select(g => new { g.NguoiDungId, g.NguoiDung!.HoTen, g.NguoiDung.Email, g.HanDen, g.NgayTao,
                DaHoc = Db.TienDoBai.Count(t => t.NguoiDungId == g.NguoiDungId && baiIds.Contains(t.BaiId) && t.TrangThai == TrangThaiHoc.DaHoc) })
            .ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemGhiDanh(string id, string email, DateTime? hanDen)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(email)) { BaoLoi("Nhập email và hạn học hợp lệ."); return RedirectToAction("Index", "GhiDanh"); }
        if (!await Db.Khoa.AnyAsync(k => k.Id == id)) return NotFound();
        var chuanEmail = email.Trim().ToUpperInvariant();
        var nd = await Db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == chuanEmail);
        if (nd is null) { BaoLoi($"Không có tài khoản {email}. Tạo tài khoản ở mục Người dùng trước."); return RedirectToAction(nameof(GhiDanh), new { id }); }
        if (!await Db.UserRoles.Join(Db.Roles.Where(r => r.Name == VaiTro.HocVien), u => u.RoleId, r => r.Id, (u,r) => u.UserId).AnyAsync(uid => uid == nd.Id))
        { BaoLoi("Chỉ ghi danh tài khoản học viên. Kiểm tra vai trò trong Người dùng."); return RedirectToAction(nameof(GhiDanh), new { id }); }
        if (await Db.GhiDanh.AnyAsync(g => g.NguoiDungId == nd.Id && g.KhoaId == id)) { BaoLoi($"{email} đã ghi danh khoá này."); return RedirectToAction(nameof(GhiDanh), new { id }); }
        Db.GhiDanh.Add(new GhiDanh { NguoiDungId = nd.Id, KhoaId = id, HanDen = CuoiNgay(hanDen) });
        await Db.SaveChangesAsync();
        ThongBao($"Đã ghi danh {nd.Email}. Học viên mở được toàn bộ bài của khoá.");
        return RedirectToAction(nameof(GhiDanh), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiHan(string id, string nguoiDungId, DateTime? hanDen)
    {
        var g = await Db.GhiDanh.FindAsync(nguoiDungId, id);
        if (g is null) return NotFound();
        g.HanDen = CuoiNgay(hanDen);
        await Db.SaveChangesAsync();
        ThongBao("Đã đổi hạn học.");
        return RedirectToAction(nameof(GhiDanh), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyGhiDanh(string id, string nguoiDungId)
    {
        var g = await Db.GhiDanh.FindAsync(nguoiDungId, id);
        if (g is null) return NotFound();
        Db.GhiDanh.Remove(g);
        await Db.SaveChangesAsync();
        ThongBao("Đã huỷ ghi danh. Học viên chỉ còn học thử.");
        return RedirectToAction(nameof(GhiDanh), new { id });
    }

    // Hạn học tính hết ngày được chọn (giờ địa phương) → lưu UTC
    private static DateTime? CuoiNgay(DateTime? d) =>
        d is null ? null : DateTime.SpecifyKind(d.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Local).ToUniversalTime();

    private async Task<List<SelectListItem>> DanhSachGiangVien() =>
        await Db.UserRoles.Join(Db.Roles.Where(r => r.Name == VaiTro.GiangVien || r.Name == VaiTro.Admin), ur => ur.RoleId, r => r.Id, (ur, r) => ur.UserId)
            .Join(Db.Users, id => id, u => u.Id, (id, u) => u).OrderBy(u => u.HoTen)
            .Select(u => new SelectListItem(u.HoTen + " (" + u.Email + ")", u.Id)).ToListAsync();
}
