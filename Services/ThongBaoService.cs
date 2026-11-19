using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SuDia.Web.Data;
using SuDia.Web.Services.Email;
using SuDia.Web.Services.Sms;

namespace SuDia.Web.Services;

/// <summary>Tạo bản ghi outbox. Việc gửi thật luôn do worker thực hiện sau khi giao dịch đơn đã hoàn tất.</summary>
public sealed class ThongBaoService(AppDbContext db, IOptions<SmsOptions> smsOptions, IOptions<EmailOptions> emailOptions, IHostEnvironment env)
{
    public string DungNoiDungDaThanhToan(DonHang don, DateTime? hanMoiNhat)
    {
        var ten = BoDau(don.TenGoi); var han = hanMoiNhat?.ToLocalTime().ToString("dd/MM/yyyy") ?? "khong gioi han";
        var domain = string.IsNullOrWhiteSpace(smsOptions.Value.TenMien) ? "" : smsOptions.Value.TenMien.TrimEnd('/');
        var prefix = $"SuDia: Don {don.MaThanhToan.ToString()[^6..]} da thanh toan {don.SoTien}d. Goi ";
        var suffix = $" da mo, han den {han}. Hoc ngay: {domain}/ThanhToan";
        var room = Math.Max(0, 160 - prefix.Length - suffix.Length);
        return prefix + (ten.Length > room ? ten[..room] : ten) + suffix;
    }
    public async Task GhiHangDoiAsync(DonHang don, DateTime? hanMoiNhat, bool boQua, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(don.SoDienThoai)) return;
        var now = DateTime.UtcNow; var start = now.Date;
        var q = db.ThongBao.Where(t => t.TaoLuc >= start && t.Kenh == KenhThongBao.Sms);
        var quaGioiHan = await q.CountAsync(t => t.NguoiNhan == don.SoDienThoai, ct) >= smsOptions.Value.ToiDaMoiNgayMoiSo
            || await q.CountAsync(t => t.NguoiDungId == don.NguoiDungId, ct) >= smsOptions.Value.ToiDaMoiNgayMoiTaiKhoan;
        var chuaCauHinh = !env.IsDevelopment() && (!smsOptions.Value.Bat || string.IsNullOrWhiteSpace(smsOptions.Value.NhaCungCap));
        db.ThongBao.Add(new ThongBao { DonHangId = don.Id, NguoiDungId = don.NguoiDungId, Kenh = KenhThongBao.Sms, NguoiNhan = don.SoDienThoai,
            NoiDung = DungNoiDungDaThanhToan(don, hanMoiNhat), TrangThai = boQua || quaGioiHan || chuaCauHinh ? TrangThaiThongBao.BoQua : TrangThaiThongBao.ChoGui,
            LoiCuoi = boQua ? "Không đủ điều kiện gửi SMS" : quaGioiHan ? "Vượt giới hạn ngày" : chuaCauHinh ? "Chưa cấu hình SMS" : null, GuiTiepLuc = now, TaoLuc = now });
    }

    public async Task GhiEmailAsync(DonHang don, DateTime? hanMoiNhat, CancellationToken ct)
    {
        var email = await db.Users.Where(u => u.Id == don.NguoiDungId).Select(u => u.Email).FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(email)) return;
        var now = DateTime.UtcNow;
        var chuaCauHinh = !env.IsDevelopment() && !emailOptions.Value.DaCauHinh;
        db.ThongBao.Add(new ThongBao { DonHangId = don.Id, NguoiDungId = don.NguoiDungId, Kenh = KenhThongBao.Email, NguoiNhan = email,
            NoiDung = await DungNoiDungEmailAsync(don, hanMoiNhat, ct), TrangThai = chuaCauHinh ? TrangThaiThongBao.BoQua : TrangThaiThongBao.ChoGui,
            LoiCuoi = chuaCauHinh ? "Chưa cấu hình email" : null, GuiTiepLuc = now, TaoLuc = now });
    }

    private async Task<string> DungNoiDungEmailAsync(DonHang don, DateTime? hanMoiNhat, CancellationToken ct)
    {
        var khoa = await db.Khoa.Where(k => don.KhoaIds.Contains(k.Id)).OrderBy(k => k.ThuTu).Select(k => k.Ten).ToListAsync(ct);
        var domain = string.IsNullOrWhiteSpace(emailOptions.Value.TenMien) ? smsOptions.Value.TenMien : emailOptions.Value.TenMien;
        domain = domain.TrimEnd('/');
        Func<string, string> e = System.Net.WebUtility.HtmlEncode;
        var danhSachKhoa = string.Join("", khoa.Select(k => $"<li>{e(k)}</li>"));
        var donUrl = $"{domain}/ThanhToan";
        var hoaDonUrl = $"{domain}/ThanhToan/HoaDon?ma={don.MaThanhToan}";
        return $"<h1>Thanh toán thành công</h1><p>Mã đơn: <b>{don.MaThanhToan}</b></p><p>Gói <b>{e(don.TenGoi)}</b> đã thanh toán {don.SoTien:N0}đ.</p><p>Khoá đã mở:</p><ul>{danhSachKhoa}</ul><p>Hạn mới: <b>{e(Han(hanMoiNhat))}</b>.</p><p><a href=\"{e(donUrl)}\">Xem đơn hàng</a> · <a href=\"{e(hoaDonUrl)}\">Xem biên lai</a></p>";
    }

    private static string Han(DateTime? han) => han?.ToLocalTime().ToString("dd/MM/yyyy") ?? "không giới hạn";
    public static string BoDau(string value) => string.Concat(value.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormC);
}
