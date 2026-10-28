using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuDia.Web.Services;
using SuDia.Web.Models;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Controllers;

public class HomeController : Controller
{
    private readonly IKhoaHocService _svc;

    public HomeController(IKhoaHocService svc) => _svc = svc;

    // Trang chủ: khung "Học cùng AI", phương pháp, môn học, trợ lý, gói học
    public IActionResult Index() => View(_svc.LayTrangChu());

    // Trang trong tài khoản. Khi có Identity: thêm [Authorize]
    [Authorize]
    public IActionResult TongQuan() => View(_svc.LayTongQuan());

    [Authorize]
    public IActionResult HoiAi([FromServices] IAiService ai, string? khoa, string? hoi)
    {
        var danhMuc = _svc.LayKhoaHoc(null);
        return View(new HoiAiViewModel
        {
            Menu = danhMuc.Menu with { MucDangMo = "HoiAi" },
            Khoa = danhMuc.Khoa,
            KhoaId = danhMuc.Khoa.Any(k => k.Id == khoa) ? khoa : null,
            BanXemThu = ai is DemoAiService,
            CauHoiBanDau = hoi is null ? "" : hoi[..Math.Min(hoi.Length, GioiHanHoiAi.CauHoi)],
        });
    }

    // Khoá của tôi: /Home/KhoaHoc chỉ hiện các khoá; /Home/KhoaHoc/lich-su-12 là trang chi tiết khoá với các bài
    [Authorize]
    public IActionResult KhoaHoc(string? id)
    {
        var vm = _svc.LayKhoaHoc(id);
        if (id is null) return View(vm);
        return vm.DangChon is null ? RedirectToAction(nameof(KhoaHoc), new { id = (string?)null }) : View("ChiTietKhoa", vm);
    }

    // Luyện đề: /Home/LuyenDe chỉ hiện các khoá; /Home/LuyenDe/lich-su-12 là đề thi thử + bài tập theo chủ đề
    [Authorize]
    public IActionResult LuyenDe(string? id)
    {
        var vm = _svc.LayLuyenDe(id);
        if (id is null) return View(vm);
        return vm.DangChon is null ? RedirectToAction(nameof(LuyenDe), new { id = (string?)null }) : View("ChiTietLuyenDe", vm);
    }

    // Bài học + khung trợ lý. Khi có dữ liệu thật: kiểm tra học sinh đã mua khoá chứa bài này
    [Authorize]
    public IActionResult BaiHoc(int? id) => View(_svc.LayBaiHoc(id));

    // Làm đề: hiện câu hỏi, chưa có đáp án
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> LamCauHoi(int? id, [FromServices] AppDbContext db)
    {
        var baiTap = await db.DeThi.Where(d => d.Id == id).Select(d => d.BaiTapGiaoId).FirstOrDefaultAsync();
        return baiTap is int bt ? RedirectToAction("Lam", "BaiTap", new { id = bt }) : View(_svc.LayDe(id, null));
    }

    // Nộp bài: chấm ở server rồi mới hiện đáp án + giải thích. Ô chọn tên "cau-1", "cau-2"…
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LamCauHoi(int? id, IFormCollection form, [FromServices] AppDbContext db)
    {
        var baiTap = await db.DeThi.Where(d => d.Id == id).Select(d => d.BaiTapGiaoId).FirstOrDefaultAsync();
        if (baiTap is int bt) return RedirectToAction("Lam", "BaiTap", new { id = bt });
        var daChon = new Dictionary<int, string>();
        foreach (var (key, value) in form)
            if (key.StartsWith("cau-") && int.TryParse(key[4..], out var so) && value.ToString() is "A" or "B" or "C" or "D")
                daChon[so] = value.ToString();
        var daNop = _svc.LayDe(id, daChon);
        _svc.LuuBaiLam(daNop);
        return View(daNop);
    }

    public IActionResult Error() => View();
}
