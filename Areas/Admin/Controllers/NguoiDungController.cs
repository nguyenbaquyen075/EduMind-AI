using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.Admin.Controllers;

// Người dùng: tạo tài khoản, đổi vai trò, khoá / mở khoá
public class NguoiDungController(AppDbContext db, UserManager<NguoiDung> users) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(string? q, string? vaiTro, bool chuaGhiDanh = false)
    {
        var tatCaVaiTro = await Db.UserRoles.Join(Db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name }).ToListAsync();
        var dsVaiTro = tatCaVaiTro.GroupBy(x => x.UserId).ToDictionary(g => g.Key,
            g => g.OrderBy(x => x.Name == VaiTro.Admin ? 0 : x.Name == VaiTro.GiangVien ? 1 : 2).First().Name!);
        var truyVan = Db.Users.AsNoTracking();
        if (chuaGhiDanh) truyVan = truyVan.Where(u => !Db.GhiDanh.Any(g => g.NguoiDungId == u.Id));
        if (!string.IsNullOrWhiteSpace(q)) truyVan = truyVan.Where(u => u.HoTen.ToLower().Contains(q.ToLower()) || u.Email!.ToLower().Contains(q.ToLower()));
        var ds = await truyVan.OrderByDescending(u => u.NgayTao)
            .Select(u => new { u.Id, u.HoTen, u.Email, u.NgayTao, u.LockoutEnd, SoKhoa = Db.GhiDanh.Count(g => g.NguoiDungId == u.Id) }).ToListAsync();
        ViewBag.Ds = ds.Select(u => new { u.Id, u.HoTen, u.Email, u.NgayTao, BiKhoa = u.LockoutEnd > DateTimeOffset.UtcNow, u.SoKhoa, VaiTro = dsVaiTro.GetValueOrDefault(u.Id) })
            .Where(u => string.IsNullOrEmpty(vaiTro) || u.VaiTro == vaiTro).ToList();
        ViewBag.Q = q;
        ViewBag.VaiTro = vaiTro;
        ViewBag.ChuaGhiDanh = chuaGhiDanh;
        return View();
    }

    public IActionResult Tao() => View(new TaoNguoiDungForm());

    public async Task<IActionResult> ChiTiet(string id)
    {
        var nd = await users.FindByIdAsync(id);
        if (nd is null) return NotFound();
        return View(new AdminNguoiDungViewModel
        {
            NguoiDung = nd, VaiTro = (await users.GetRolesAsync(nd)).ToList(),
            GhiDanh = await Db.GhiDanh.AsNoTracking().Where(g => g.NguoiDungId == id).OrderByDescending(g => g.NgayTao)
                .Select(g => new AdminGhiDanhRow(id, nd.HoTen, nd.Email, g.KhoaId, g.Khoa!.Ten, g.NgayTao, g.HanDen)).ToListAsync(),
            LuyenTap = await Db.BaiLam.AsNoTracking().Where(b => b.NguoiDungId == id).OrderByDescending(b => b.NopLuc).Take(20).ToListAsync(),
            BaiNop = await Db.BaiNopGiao.AsNoTracking().Include(n => n.BaiTapGiao).Where(n => n.NguoiDungId == id).OrderByDescending(n => n.NopLuc).Take(20).ToListAsync(),
            TongLuyenTap = await Db.BaiLam.CountAsync(b => b.NguoiDungId == id), TongBaiNop = await Db.BaiNopGiao.CountAsync(b => b.NguoiDungId == id),
            BaiDaHoc = await Db.TienDoBai.CountAsync(t => t.NguoiDungId == id && t.TrangThai == TrangThaiHoc.DaHoc),
        });
    }

    public async Task<IActionResult> Sua(string id)
    {
        var nd = await users.FindByIdAsync(id);
        return nd is null ? NotFound() : View(new SuaNguoiDungForm { Id = nd.Id, HoTen = nd.HoTen, Email = nd.Email ?? "" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(SuaNguoiDungForm f)
    {
        var nd = await users.FindByIdAsync(f.Id);
        if (nd is null) return NotFound();
        if (!ModelState.IsValid) return View(f);
        var doiEmail = !string.Equals(nd.Email, f.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        nd.HoTen = f.HoTen.Trim(); nd.Email = f.Email.Trim(); nd.UserName = nd.Email;
        var result = await users.UpdateAsync(nd);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Code is "DuplicateUserName" or "DuplicateEmail" ? "Email đã được sử dụng." : e.Description);
            return View(f);
        }
        if (doiEmail) await users.UpdateSecurityStampAsync(nd);
        ThongBao("Đã cập nhật thông tin người dùng.");
        return RedirectToAction(nameof(ChiTiet), new { id = nd.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(TaoNguoiDungForm f)
    {
        if (!VaiTro.TatCa.Contains(f.VaiTro)) ModelState.AddModelError(nameof(f.VaiTro), "Chọn vai trò");
        if (!ModelState.IsValid) return View(f);
        var nd = new NguoiDung { UserName = f.Email.Trim(), Email = f.Email.Trim(), HoTen = f.HoTen.Trim(), EmailConfirmed = true };
        var kq = await users.CreateAsync(nd, f.MatKhau);
        if (!kq.Succeeded)
        {
            foreach (var e in kq.Errors) ModelState.AddModelError("", e.Code is "DuplicateUserName" or "DuplicateEmail" ? "Email này đã có tài khoản." : e.Description);
            return View(f);
        }
        await users.AddToRoleAsync(nd, f.VaiTro);
        ThongBao($"Đã tạo tài khoản {nd.Email} ({TenVaiTro(f.VaiTro)}).");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiVaiTro(string id, string vaiTro)
    {
        if (id == Uid) { BaoLoi("Không đổi vai trò của chính mình."); return RedirectToAction(nameof(Index)); }
        var nd = await users.FindByIdAsync(id);
        if (nd is null || !VaiTro.TatCa.Contains(vaiTro)) return BadRequest();
        await users.RemoveFromRolesAsync(nd, await users.GetRolesAsync(nd));
        await users.AddToRoleAsync(nd, vaiTro);
        await users.UpdateSecurityStampAsync(nd);   // phiên đang mở nhận vai trò mới ngay
        ThongBao($"{nd.Email} giờ là {TenVaiTro(vaiTro)}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KhoaTaiKhoan(string id, bool khoa)
    {
        if (id == Uid) { BaoLoi("Không khoá tài khoản của chính mình."); return RedirectToAction(nameof(Index)); }
        var nd = await users.FindByIdAsync(id);
        if (nd is null) return NotFound();
        await users.SetLockoutEnabledAsync(nd, true);
        await users.SetLockoutEndDateAsync(nd, khoa ? DateTimeOffset.MaxValue : null);
        if (khoa) await users.UpdateSecurityStampAsync(nd);   // đăng xuất các phiên đang mở
        ThongBao(khoa ? $"Đã khoá {nd.Email}." : $"Đã mở khoá {nd.Email}.");
        return RedirectToAction(nameof(Index));
    }
}
