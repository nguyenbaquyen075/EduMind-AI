using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Controllers;

public class TaiKhoanController : Controller
{
    private readonly SignInManager<NguoiDung> _signIn;
    private readonly UserManager<NguoiDung> _users;

    public TaiKhoanController(SignInManager<NguoiDung> signIn, UserManager<NguoiDung> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> DangNhap(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true && await _users.GetUserAsync(User) is { } nd) return await VeTrangTheoVaiTro(nd);
        return View(new DangNhapModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangNhap(DangNhapModel m)
    {
        if (!ModelState.IsValid) return View(m);

        var kq = await _signIn.PasswordSignInAsync(m.Email, m.MatKhau, m.GhiNho, lockoutOnFailure: true);
        if (kq.IsLockedOut) ModelState.AddModelError("", "Đăng nhập sai nhiều lần. Thử lại sau ít phút.");
        else if (!kq.Succeeded) ModelState.AddModelError("", "Email hoặc mật khẩu chưa đúng.");
        if (!ModelState.IsValid) return View(m);

        if (Url.IsLocalUrl(m.ReturnUrl)) return LocalRedirect(m.ReturnUrl!);
        var nd = await _users.FindByEmailAsync(m.Email);
        return await VeTrangTheoVaiTro(nd!);
    }

    [HttpGet]
    public async Task<IActionResult> DangKy()
    {
        if (User.Identity?.IsAuthenticated == true && await _users.GetUserAsync(User) is { } nd) return await VeTrangTheoVaiTro(nd);
        return View(new DangKyModel());
    }

    // Đăng ký mới là học viên, chưa ghi danh khoá nào (học thử bài đầu). Admin ghi danh sau
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKy(DangKyModel m)
    {
        if (!ModelState.IsValid) return View(m);

        var nd = new NguoiDung { UserName = m.Email, Email = m.Email, HoTen = m.HoTen.Trim() };
        var kq = await _users.CreateAsync(nd, m.MatKhau);
        if (!kq.Succeeded)
        {
            foreach (var e in kq.Errors)
                ModelState.AddModelError("", e.Code switch
                {
                    "DuplicateUserName" or "DuplicateEmail" => "Email này đã có tài khoản.",
                    _ => e.Description,
                });
            return View(m);
        }
        await _users.AddToRoleAsync(nd, VaiTro.HocVien);
        await _signIn.SignInAsync(nd, isPersistent: true);
        return RedirectToAction("TongQuan", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangXuat()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult KhongCoQuyen() => View();

    private async Task<IActionResult> VeTrangTheoVaiTro(NguoiDung nd)
    {
        var vaiTro = await _users.GetRolesAsync(nd);
        if (vaiTro.Contains(VaiTro.Admin)) return Redirect("/Admin");
        if (vaiTro.Contains(VaiTro.GiangVien)) return Redirect("/GiangVien");
        return RedirectToAction("TongQuan", "Home");
    }
}
