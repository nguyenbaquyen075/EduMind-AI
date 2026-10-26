using Microsoft.AspNetCore.Mvc;
using SuDia.Web.Services;

namespace SuDia.Web.Controllers;

public class HomeController : Controller
{
    private readonly IKhoaHocService _svc;

    public HomeController(IKhoaHocService svc) => _svc = svc;

    // Trang chủ: khung "Học cùng AI", phương pháp, môn học, trợ lý, gói học
    public IActionResult Index() => View(_svc.LayTrangChu());

    // Trang trong tài khoản. Khi có Identity: thêm [Authorize]
    public IActionResult TongQuan() => View(_svc.LayTongQuan());

    // Khoá của tôi: /Home/KhoaHoc chỉ hiện các khoá; /Home/KhoaHoc/lich-su-12 là trang chi tiết khoá với các bài
    public IActionResult KhoaHoc(string? id)
    {
        var vm = _svc.LayKhoaHoc(id);
        if (id is null) return View(vm);
        return vm.DangChon is null ? RedirectToAction(nameof(KhoaHoc), new { id = (string?)null }) : View("ChiTietKhoa", vm);
    }

    // Luyện đề: /Home/LuyenDe chỉ hiện các khoá; /Home/LuyenDe/lich-su-12 là đề thi thử + bài tập theo chủ đề
    public IActionResult LuyenDe(string? id)
    {
        var vm = _svc.LayLuyenDe(id);
        if (id is null) return View(vm);
        return vm.DangChon is null ? RedirectToAction(nameof(LuyenDe), new { id = (string?)null }) : View("ChiTietLuyenDe", vm);
    }

    // Bài học + khung trợ lý. Khi có dữ liệu thật: kiểm tra học sinh đã mua khoá chứa bài này
    public IActionResult BaiHoc(int? id) => View(_svc.LayBaiHoc(id));

    // Làm đề: hiện câu hỏi, chưa có đáp án
    [HttpGet]
    public IActionResult LamCauHoi(int? id) => View(_svc.LayDe(id, null));

    // Nộp bài: chấm ở server rồi mới hiện đáp án + giải thích. Ô chọn tên "cau-1", "cau-2"…
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LamCauHoi(int? id, IFormCollection form)
    {
        var daChon = new Dictionary<int, string>();
        foreach (var (key, value) in form)
            if (key.StartsWith("cau-") && int.TryParse(key[4..], out var so) && value.ToString() is "A" or "B" or "C" or "D")
                daChon[so] = value.ToString();
        return View(_svc.LayDe(id, daChon));
    }

    public IActionResult Error() => View();
}
