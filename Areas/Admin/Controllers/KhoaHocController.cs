using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

// Khoá học: thông tin, môn, ảnh bìa, ẩn/hiện, giảng viên phụ trách; Ghi danh: học viên ↔ khoá, hạn học
public class KhoaHocController(AppDbContext db, IWebHostEnvironment env) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Khoa = await Db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu)
            .Select(k => new { k.Id, k.Ten, k.Mon, k.KyHieu, k.Anh, k.HienThi, k.GiangVienId, GiangVien = k.GiangVien != null ? k.GiangVien.HoTen : null,
                SoBai = k.ChuDe.SelectMany(c => c.Bai).Count(), SoHocVien = Db.GhiDanh.Count(g => g.KhoaId == k.Id) }).ToListAsync();
        ViewBag.DsGiangVien = await DanhSachGiangVien();
        ViewBag.GiaLe = (await Db.GoiBan.AsNoTracking().OrderBy(g => g.ThuTu).ToListAsync())
            .Where(g => g.KhoaIds.Count == 1).GroupBy(g => g.KhoaIds[0]).ToDictionary(n => n.Key, n => n.First().DangBan ? n.First().Gia : null);
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
        await NapForm();
        var goi = await GoiLe(k.Id);
        return View(new KhoaForm { Id = k.Id, Ten = k.Ten, Mon = k.Mon, KyHieu = k.KyHieu, MoTa = k.MoTa, Anh = k.Anh,
            GiangVienId = k.GiangVienId, ThuTu = k.ThuTu + 1, HienThi = k.HienThi,
            MucTieu = k.MucTieu, DoiTuong = k.DoiTuong,
            Gia = goi?.Gia, SoThang = goi?.SoThang ?? 12, DangBan = goi?.DangBan ?? true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(KhoaForm f, bool boAnh)
    {
        var k = await Db.Khoa.FindAsync(f.Id);
        if (k is null) return NotFound();
        await KiemTraForm(f);
        if (!ModelState.IsValid) { f.Anh = k.Anh; await NapForm(); return View(f); }
        var anhCu = k.Anh;
        k.Anh = boAnh ? "" : f.AnhTai is { Length: > 0 } ? await LuuAnh(f.Id, f.AnhTai) : k.Anh;
        GanTruong(k, f);
        await SapXep(k, f.ThuTu);
        await LuuGoiLe(k, f);
        await Db.SaveChangesAsync();
        if (anhCu != k.Anh) XoaAnhTaiLen(anhCu);
        ThongBao("Đã lưu thông tin khoá.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Tao()
    {
        await NapForm();
        return View("Sua", new KhoaForm { ThuTu = await Db.Khoa.CountAsync() + 1 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(KhoaForm f)
    {
        f.Id = f.Id?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(f.Id) || !Regex.IsMatch(f.Id, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || f.Id.Length > 80)
            ModelState.AddModelError(nameof(f.Id), "Mã khoá gồm chữ thường không dấu, số và dấu gạch nối, tối đa 80 ký tự.");
        else if (await Db.Khoa.AnyAsync(k => k.Id == f.Id)) ModelState.AddModelError(nameof(f.Id), "Mã khoá đã tồn tại.");
        await KiemTraForm(f);
        if (!ModelState.IsValid) { f.Anh = null; await NapForm(); return View("Sua", f); }
        var k = new Khoa { Id = f.Id, ThuTu = await Db.Khoa.CountAsync(), Anh = f.AnhTai is { Length: > 0 } ? await LuuAnh(f.Id, f.AnhTai) : "" };
        GanTruong(k, f);
        Db.Khoa.Add(k);
        await SapXep(k, f.ThuTu);
        await LuuGoiLe(k, f);
        await Db.SaveChangesAsync();
        ThongBao($"Đã tạo khoá {k.Ten}{(k.HienThi ? "" : " (đang ẩn)")}. Thêm chủ đề và bài học tại Nội dung bài học.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiHienThi(string id)
    {
        var k = await Db.Khoa.FindAsync(id);
        if (k is null) return NotFound();
        k.HienThi = !k.HienThi;
        await Db.SaveChangesAsync();
        ThongBao(k.HienThi ? $"Đã hiện khoá {k.Ten} cho học viên." : $"Đã ẩn khoá {k.Ten}. Học viên đã ghi danh vẫn học tiếp được.");
        return RedirectToAction(nameof(Index));
    }

    // Gói bán lẻ = gói chỉ mở đúng khoá này; chưa có thì tạo khi admin nhập giá
    private async Task<GoiBan?> GoiLe(string khoaId) =>
        (await Db.GoiBan.OrderBy(g => g.ThuTu).ToListAsync()).FirstOrDefault(g => g.KhoaIds.Count == 1 && g.KhoaIds[0] == khoaId);

    private async Task LuuGoiLe(Khoa k, KhoaForm f)
    {
        var goi = await GoiLe(k.Id);
        if (goi is null)
        {
            if (f.Gia is null) return;
            goi = new GoiBan { Ten = k.Ten, Nhan = "Một khoá", KhoaIds = [k.Id], ThuTu = (await Db.GoiBan.MaxAsync(g => (int?)g.ThuTu) ?? -1) + 1 };
            Db.GoiBan.Add(goi);
        }
        goi.Gia = f.Gia; goi.SoThang = f.SoThang; goi.DangBan = f.DangBan;
        if (goi.QuyenLoi.Count == 0) goi.QuyenLoi = [.. k.MucTieu.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(4)];
    }

    private static readonly string[] MonGoiY = ["Lịch sử", "Địa lí"];
    private const long AnhToiDa = 5 * 1024 * 1024;

    private async Task NapForm()
    {
        ViewBag.DsGiangVien = await DanhSachGiangVien();
        ViewBag.DsMon = MonGoiY.Concat(await Db.Khoa.Where(k => k.Mon != "").Select(k => k.Mon).Distinct().ToListAsync()).Distinct().ToList();
    }

    private async Task KiemTraForm(KhoaForm f)
    {
        if (!string.IsNullOrEmpty(f.GiangVienId) && !(await DanhSachGiangVien()).Any(g => g.Value == f.GiangVienId))
            ModelState.AddModelError(nameof(f.GiangVienId), "Chọn giảng viên hợp lệ.");
        if (f.AnhTai is { Length: > 0 } anh && (anh.Length > AnhToiDa || DuoiAnh(anh) is null))
            ModelState.AddModelError(nameof(f.AnhTai), "Ảnh bìa phải là JPG, PNG hoặc WebP, tối đa 5 MB.");
    }

    private static void GanTruong(Khoa k, KhoaForm f)
    {
        k.Ten = Regex.Replace(f.Ten.Trim(), @"\s+", " "); k.Mon = f.Mon.Trim(); k.KyHieu = f.KyHieu.Trim();
        k.MoTa = f.MoTa?.Trim() ?? ""; k.HienThi = f.HienThi;
        k.DoiTuong = f.DoiTuong?.Trim() ?? "";
        k.MucTieu = string.Join("\n", (f.MucTieu ?? "").Split('\n').Select(d => d.Trim().TrimStart('-', '•', '*').Trim()).Where(d => d.Length > 0));
        k.GiangVienId = string.IsNullOrEmpty(f.GiangVienId) ? null : f.GiangVienId;
    }

    // Đưa khoá về vị trí thứ tự (1 = đầu), đánh số lại các khoá còn lại cho liền
    private async Task SapXep(Khoa k, int? viTri)
    {
        var ds = await Db.Khoa.Where(x => x.Id != k.Id).OrderBy(x => x.ThuTu).ToListAsync();
        ds.Insert(Math.Clamp((viTri ?? int.MaxValue) - 1, 0, ds.Count), k);
        for (var i = 0; i < ds.Count; i++) ds[i].ThuTu = i;
    }

    // Kiểm tra theo chữ ký tệp, không tin phần đuôi tên hay Content-Type
    private static string? DuoiAnh(IFormFile anh)
    {
        Span<byte> dau = stackalloc byte[12];
        using var s = anh.OpenReadStream();
        if (s.ReadAtLeast(dau, 12, throwOnEndOfStream: false) < 12) return null;
        if (dau[0] == 0xFF && dau[1] == 0xD8 && dau[2] == 0xFF) return ".jpg";
        if (dau[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
        if (dau[..4].SequenceEqual("RIFF"u8) && dau[8..12].SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }

    private async Task<string> LuuAnh(string khoaId, IFormFile anh)
    {
        var ten = $"{khoaId}-{Guid.NewGuid():N}"[..(khoaId.Length + 9)] + DuoiAnh(anh);
        var thuMuc = Path.Combine(env.WebRootPath, "images", "khoa");
        Directory.CreateDirectory(thuMuc);
        await using var tep = System.IO.File.Create(Path.Combine(thuMuc, ten));
        await anh.CopyToAsync(tep);
        return "/images/khoa/" + ten;
    }

    // Chỉ xoá ảnh do form này tải lên (tên có đuôi -xxxxxxxx), không đụng ảnh mẫu
    private void XoaAnhTaiLen(string duongDan)
    {
        var m = Regex.Match(duongDan, @"^/images/khoa/([a-z0-9-]+-[0-9a-f]{8}\.(?:jpg|png|webp))$");
        if (!m.Success) return;
        var tep = Path.Combine(env.WebRootPath, "images", "khoa", m.Groups[1].Value);
        try { System.IO.File.Delete(tep); } catch (IOException) { }
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
