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
        if (choCham > 0) viec.Add(new("Bài tự luận chờ chấm", $"{choCham} bài nộp cần giảng viên xem và chấm điểm.", "/Admin/BaiTap", true));
        if (chuaGv > 0) viec.Add(new("Khoá chưa có giảng viên", $"{chuaGv} khoá cần phân công người phụ trách.", "/Admin/KhoaHoc", true));
        if (sapHet > 0) viec.Add(new("Ghi danh sắp hết hạn", $"{sapHet} lượt ghi danh hết hạn trong 14 ngày tới.", "/Admin/GhiDanh?trangThai=sap-het"));
        if (hetHan > 0) viec.Add(new("Ghi danh đã hết hạn", $"{hetHan} lượt ghi danh cần kiểm tra hoặc gia hạn.", "/Admin/GhiDanh?trangThai=het-han"));
        if (chuaGhiDanh > 0) viec.Add(new("Học viên chưa ghi danh", $"{chuaGhiDanh} học viên mới chỉ truy cập nội dung học thử.", "/Admin/NguoiDung?vaiTro=HocVien&chuaGhiDanh=true"));
        if (baiTrong > 0) viec.Add(new("Bài đã xuất bản chưa có nội dung", $"{baiTrong} bài cần bổ sung nội dung học.", "/Admin/KhoaHoc", true));
        var luyen = await Db.BaiLam.AsNoTracking().Include(b => b.NguoiDung).OrderByDescending(b => b.NopLuc).Take(6).ToListAsync();
        var nop = await Db.BaiNopGiao.AsNoTracking().Include(b => b.NguoiDung).Include(b => b.BaiTapGiao).OrderByDescending(b => b.NopLuc).Take(6).ToListAsync();
        var hoatDong = luyen.Select(b => new AdminHoatDong(b.NguoiDung?.HoTen ?? "Học viên", b.TenDe, "Luyện tập", b.NopLuc, $"/Admin/NguoiDung/ChiTiet/{b.NguoiDungId}"))
            .Concat(nop.Select(b => new AdminHoatDong(b.NguoiDung?.HoTen ?? "Học viên", b.BaiTapGiao!.Ten, b.DiemTuLuan == null ? "Chờ chấm" : "Bài tập", b.NopLuc, $"/GiangVien/GiaoBai/BaiNop/{b.BaiTapGiaoId}")))
            .OrderByDescending(h => h.Luc).Take(8).ToList();
        var buoi = await Db.BuoiHoc.AsNoTracking().Where(b => b.ThoiGian >= now).OrderBy(b => b.ThoiGian).Take(5)
            .Select(b => new AdminBuoi(b.Id, b.BaiId, $"Bài {b.Bai!.So}. {b.Bai.Ten}", b.Bai.ChuDe!.Khoa!.Ten,
                b.Bai.ChuDe.Khoa.GiangVien == null ? null : b.Bai.ChuDe.Khoa.GiangVien.HoTen, b.ThoiGian, b.Link)).ToListAsync();
        var tongBayNgay = bc.Ngay.Sum(n => n.Tong);
        var dauThang = TimeZoneInfo.ConvertTimeToUtc(new DateTime(AdminThongKeService.HomNay.Year, AdminThongKeService.HomNay.Month, 1), AdminThongKeService.MuiGio);
        var doanhThuThang = await Db.DonHang.Where(d => d.TrangThai == TrangThaiDon.DaThanhToan && d.NgayThanhToan >= dauThang).SumAsync(d => (long)d.SoTien);
        var donThang = await Db.DonHang.CountAsync(d => d.TrangThai == TrangThaiDon.DaThanhToan && d.NgayThanhToan >= dauThang);
        var donCho = await Db.DonHang.CountAsync(d => d.TrangThai == TrangThaiDon.ChoThanhToan && d.NgayTao < now.AddMinutes(-30));
        if (donCho > 0) viec.Insert(0, new("Đơn chờ thanh toán quá 30 phút", $"{donCho} đơn cần kiểm tra lại với PayOS.", "/Admin/DoanhThu?trangThai=" + TrangThaiDon.ChoThanhToan));
        var dangHoc = await Db.GhiDanh.Where(g => g.HanDen == null || g.HanDen >= now).Select(g => g.NguoiDungId).Distinct().CountAsync();
        var canNop = await Db.Khoa.SumAsync(k => Db.BaiTapGiao.Count(t => t.DaGiao && t.Bai!.ChuDe!.KhoaId == k.Id)
            * Db.GhiDanh.Count(g => g.KhoaId == k.Id && (g.HanDen == null || g.HanDen >= now)));
        var daNop = await Db.BaiNopGiao.CountAsync(n => n.BaiTapGiao!.DaGiao && Db.GhiDanh.Any(g => g.KhoaId == n.BaiTapGiao.Bai!.ChuDe!.KhoaId
            && g.NguoiDungId == n.NguoiDungId && (g.HanDen == null || g.HanDen >= now)));
        return View(new AdminDashboardViewModel
        {
            Ngay = bc.Ngay, Khoa = bc.Khoa, Viec = viec, HoatDong = hoatDong, Buoi = buoi,
            TaiKhoanMoi = bc.Ngay.Sum(n => n.DangKy),
            SoLieu = [new("Doanh thu tháng này", DinhDang.Gia(doanhThuThang), $"{donThang} đơn đã thanh toán"),
                new("Học viên đang học", dangHoc.ToString(), $"{Dem(VaiTro.HocVien)} tài khoản · {chuaGhiDanh} chưa ghi danh"),
                new("Giảng viên", Dem(VaiTro.GiangVien).ToString(), $"{bc.Khoa.Count} khoá · {chuaGv} khoá chưa phân công"),
                new("Hoàn thành bài tập", canNop == 0 ? "—" : $"{(int)Math.Round(daNop * 100.0 / canNop)}%", $"{daNop}/{canNop} bài cần nộp"),
                new("Bài nộp 7 ngày", tongBayNgay.ToString(), "Luyện tập và bài tập theo buổi"),
                new("Tự luận chờ chấm", choCham.ToString(), "Cần giảng viên chấm"),
                new("Ghi danh sắp hết hạn", sapHet.ToString(), "Trong 14 ngày tới"),
                new("Đơn chờ thanh toán", (await Db.DonHang.CountAsync(d => d.TrangThai == TrangThaiDon.ChoThanhToan)).ToString(), "Chưa hoàn tất trên PayOS")],
        });
    }
}
