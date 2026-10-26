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

    // Làm câu hỏi: hiển thị câu hỏi
    [HttpGet]
    public IActionResult LamCauHoi(int? id) => View(_svc.LayCauHoi(id, null));

    // Làm câu hỏi: học sinh chọn đáp án, chấm ở server
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LamCauHoi(int? id, string? dapAn)
    {
        var hopLe = dapAn is "A" or "B" or "C" or "D";
        return View(_svc.LayCauHoi(id, hopLe ? dapAn : null));
    }

    public IActionResult Error() => View();
}
