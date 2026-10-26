using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Areas.Admin.Controllers;

public class BaoCaoController(AppDbContext db, AdminThongKeService thongKe) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index(DateTime? tu, DateTime? den, string? khoa)
    {
        var (dau, cuoi, loi) = Khoang(tu, den);
        if (!ModelState.IsValid) loi = "Ngày chưa đúng định dạng.";
        if (loi is not null) { ModelState.AddModelError("", loi); dau = AdminThongKeService.HomNay.AddDays(-6); cuoi = AdminThongKeService.HomNay; }
        if (!string.IsNullOrEmpty(khoa) && !await Db.Khoa.AnyAsync(k => k.Id == khoa)) return NotFound();
        return View(await thongKe.BaoCaoAsync(dau, cuoi, string.IsNullOrEmpty(khoa) ? null : khoa));
    }

    public async Task<IActionResult> Xuat(DateTime? tu, DateTime? den, string? khoa)
    {
        var (dau, cuoi, loi) = Khoang(tu, den);
        if (!ModelState.IsValid || loi is not null) return BadRequest(loi ?? "Ngày chưa đúng định dạng.");
        if (!string.IsNullOrEmpty(khoa) && !await Db.Khoa.AnyAsync(k => k.Id == khoa)) return NotFound();
        var bc = await thongKe.BaoCaoAsync(dau, cuoi, string.IsNullOrEmpty(khoa) ? null : khoa);
        var csv = new StringBuilder("Ngày,Lượt luyện tập,Bài tập nộp,Tài khoản mới toàn hệ thống\r\n");
        foreach (var n in bc.Ngay) csv.AppendLine($"{n.Ngay:yyyy-MM-dd},{n.LuyenTap},{n.BaiNop},{n.DangKy}");
        csv.AppendLine().AppendLine("Số liệu,Giá trị");
        foreach (var s in bc.SoLieu) csv.AppendLine($"{O(s.Nhan)},{O(s.GiaTri)}");
        csv.AppendLine().AppendLine("Khoá học,Giảng viên,Tổng bài,Bài xuất bản,Tổng ghi danh,Ghi danh còn hạn");
        foreach (var k in bc.Khoa) csv.AppendLine($"{O(k.Ten)},{O(k.GiangVien ?? "Chưa phân công")},{k.SoBai},{k.DaXuatBan},{k.SoHocVien},{k.DangHoc}");
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", $"bao-cao-hoc-tap-{dau:yyyyMMdd}-{cuoi:yyyyMMdd}.csv");
    }

    private static string O(string value)
    {
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    private static (DateTime Tu, DateTime Den, string? Loi) Khoang(DateTime? tu, DateTime? den)
    {
        var dau = tu?.Date ?? AdminThongKeService.HomNay.AddDays(-6);
        var cuoi = den?.Date ?? AdminThongKeService.HomNay;
        var loi = dau.Year < 2000 || cuoi.Year > 9998 || dau > cuoi || (cuoi - dau).TotalDays > 89
            ? "Chọn khoảng thời gian từ năm 2000, ngày bắt đầu không sau ngày kết thúc và tối đa 90 ngày." : null;
        return (dau, cuoi, loi);
    }
}
