using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Areas.Admin.Controllers;

public class TongQuanController(AppDbContext db, AdminThongKeService thongKe) : AdminControllerBase(db)
{
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var tu = AdminThongKeService.HomNay.AddDays(-6);
        var bc = await thongKe.BaoCaoAsync(tu, AdminThongKeService.HomNay, null);
        var roles = await Db.UserRoles.Join(Db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name }).ToListAsync();
        int Dem(string role) => roles.Where(r => r.Name == role).Select(r => r.UserId).Distinct().Count();
        var chuaGv = bc.Khoa.Count(k => k.GiangVien == null);
        var sapHet = await Db.GhiDanh.CountAsync(g => g.HanDen >= now && g.HanDen <= now.AddDays(14));
        var hetHan = await Db.GhiDanh.CountAsync(g => g.HanDen < now);
        var choCham = await Db.BaiNopGiao.CountAsync(n => n.DiemTuLuan == null);
        var baiTrong = await Db.Bai.CountAsync(b => b.DaXuatBan && !b.Muc.Any(m => m.Phan.Any()));
        var chuaGhiDanh = await Db.Users.CountAsync(u => Db.UserRoles.Any(ur => ur.UserId == u.Id && Db.Roles.Any(r => r.Id == ur.RoleId && r.Name == VaiTro.HocVien)) && !Db.GhiDanh.Any(g => g.NguoiDungId == u.Id));
        var viec = new List<AdminViec>();
        if (choCham > 0) viec.Add(new("Bài tự luận chờ chấm", $"{choCham} bài nộp cần giảng viên xem và chấm điểm.", "/Admin/BaiTap?trangThai=cho-cham", true));
        if (chuaGv > 0) viec.Add(new("Khoá chưa có giảng viên", $"{chuaGv} khoá cần phân công người phụ trách.", "/Admin/KhoaHoc", true));
        if (sapHet > 0) viec.Add(new("Ghi danh sắp hết hạn", $"{sapHet} lượt ghi danh hết hạn trong 14 ngày tới.", "/Admin/GhiDanh?trangThai=sap-het"));
        if (hetHan > 0) viec.Add(new("Ghi danh đã hết hạn", $"{hetHan} lượt ghi danh cần kiểm tra hoặc gia hạn.", "/Admin/GhiDanh?trangThai=het-han"));
        if (chuaGhiDanh > 0) viec.Add(new("Học viên chưa ghi danh", $"{chuaGhiDanh} học viên mới chỉ truy cập nội dung học thử.", "/Admin/NguoiDung?vaiTro=HocVien&chuaGhiDanh=true"));
        if (baiTrong > 0) viec.Add(new("Bài đã xuất bản chưa có nội dung", $"{baiTrong} bài cần bổ sung nội dung học.", "/Admin/NoiDung?trangThai=thieu-noi-dung-xuat-ban", true));
        var luyen = await Db.BaiLam.AsNoTracking().Include(b => b.NguoiDung).OrderByDescending(b => b.NopLuc).Take(6).ToListAsync();
        var nop = await Db.BaiNopGiao.AsNoTracking().Include(b => b.NguoiDung).Include(b => b.BaiTapGiao).OrderByDescending(b => b.NopLuc).Take(6).ToListAsync();
        var hoatDong = luyen.Select(b => new AdminHoatDong(b.NguoiDung?.HoTen ?? "Học viên", b.TenDe, "Luyện tập", b.NopLuc, $"/Admin/NguoiDung/ChiTiet/{b.NguoiDungId}"))
            .Concat(nop.Select(b => new AdminHoatDong(b.NguoiDung?.HoTen ?? "Học viên", b.BaiTapGiao!.Ten, b.DiemTuLuan == null ? "Chờ chấm" : "Bài tập", b.NopLuc, $"/GiangVien/GiaoBai/BaiNop/{b.BaiTapGiaoId}")))
            .OrderByDescending(h => h.Luc).Take(8).ToList();
        var buoi = await Db.BuoiHoc.AsNoTracking().Where(b => b.ThoiGian >= now).OrderBy(b => b.ThoiGian).Take(5)
            .Select(b => new AdminBuoi(b.Id, b.BaiId, $"Bài {b.Bai!.So}. {b.Bai.Ten}", b.Bai.ChuDe!.Khoa!.Ten,
                b.Bai.ChuDe.Khoa.GiangVien == null ? null : b.Bai.ChuDe.Khoa.GiangVien.HoTen, b.ThoiGian, b.Link)).ToListAsync();
        var tongBayNgay = bc.Ngay.Sum(n => n.Tong);
        return View(new AdminDashboardViewModel
        {
            Ngay = bc.Ngay, Khoa = bc.Khoa, Viec = viec, HoatDong = hoatDong, Buoi = buoi,
            TaiKhoanMoi = bc.Ngay.Sum(n => n.DangKy),
            SoLieu = [new("Học viên", Dem(VaiTro.HocVien).ToString(), $"{chuaGhiDanh} chưa ghi danh"),
                new("Giảng viên", Dem(VaiTro.GiangVien).ToString(), "Tài khoản giảng dạy"),
                new("Khoá học", bc.Khoa.Count.ToString(), $"{chuaGv} chưa có giảng viên"),
                new("Ghi danh còn hạn", bc.Khoa.Sum(k => k.DangHoc).ToString(), $"{sapHet} sắp hết hạn"),
                new("Bài học xuất bản", bc.Khoa.Sum(k => k.DaXuatBan).ToString(), $"{bc.Khoa.Sum(k => k.SoBai - k.DaXuatBan)} bài nháp"),
                new("Bài tập đã giao", (await Db.BaiTapGiao.CountAsync(b => b.DaGiao)).ToString(), "Bài tập theo buổi"),
                new("Bài nộp 7 ngày", tongBayNgay.ToString(), "Luyện tập và bài tập theo buổi"),
                new("Tự luận chờ chấm", choCham.ToString(), "Trên toàn hệ thống")],
        });
    }
}
