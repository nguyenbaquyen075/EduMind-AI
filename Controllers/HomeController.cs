using System.Security.Claims;
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
    public async Task<IActionResult> TongQuan([FromServices] HocVienService hv)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        ViewBag.ViecCanLam = await hv.ViecCanLam(uid);
        ViewBag.BuoiSapToi = await hv.BuoiSapToi(uid);
        ViewBag.SapHetHan = await hv.SapHetHan(uid);
        return View(_svc.LayTongQuan());
    }

    // Điểm của tôi: điểm bài tập, thi thử, luyện tập và huy hiệu
    [Authorize]
    public async Task<IActionResult> Diem([FromServices] HocVienService hv) =>
        View(await hv.BangDiem(User.FindFirstValue(ClaimTypes.NameIdentifier)!, _svc.LayKhoaHoc(null).Menu with { MucDangMo = "Diem" }));

    // Thi thử: mọi đề thi thử của các khoá, kết quả gần nhất và lịch sử làm đề
    [Authorize]
    public async Task<IActionResult> ThiThu([FromServices] HocVienService hv, string? khoa)
    {
        var luyen = _svc.LayLuyenDe(null);
        if (khoa is not null && !luyen.Khoa.Any(k => k.Khoa.Id == khoa)) khoa = null;
        return View(new ThiThuViewModel { Menu = luyen.Menu with { MucDangMo = "ThiThu" }, Khoa = luyen.Khoa, KhoaId = khoa,
            LichSu = await hv.LichSuThi(User.FindFirstValue(ClaimTypes.NameIdentifier)!) });
    }

    // Ôn câu sai: các câu mà lần trả lời gần nhất còn sai, xem lại đáp án và giải thích
    [Authorize]
    public async Task<IActionResult> OnTap([FromServices] HocVienService hv, string? khoa)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var dsKhoa = await hv.KhoaCuaToi(uid);
        if (khoa is not null && !dsKhoa.Any(k => k.Id == khoa)) khoa = null;
        return View(new OnTapViewModel { Menu = _svc.LayKhoaHoc(null).Menu with { MucDangMo = "OnTap" },
            Cau = await hv.CauSai(uid, khoa), DsKhoa = dsKhoa, KhoaId = khoa });
    }

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
    public IActionResult BaiHoc(int? id) => _svc.LayBaiHoc(id) is { } vm ? View(vm) : NotFound();

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
        // Luyện tập theo bài phải làm hết mới nộp được; đề thi thử có đồng hồ tự nộp nên được để trống khi hết giờ
        var deTrong = _svc.LayDe(id, null);
        if (!deTrong.LaDeThi && daChon.Count < deTrong.CauHoi.Count)
        {
            ViewBag.LoiNop = "Bạn cần trả lời tất cả các câu trước khi nộp bài.";
            return View(deTrong);
        }
        var daNop = _svc.LayDe(id, daChon);
        _svc.LuuBaiLam(daNop);
        return View(daNop);
    }

    public IActionResult Error() => View();
}
