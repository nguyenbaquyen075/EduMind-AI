using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Services;

/// <summary>
/// Dữ liệu các trang học viên đọc từ database, theo người đang đăng nhập:
/// khoá đã ghi danh (chưa ghi danh = học thử bài đầu), tiến độ từng bài, kết quả bài làm.
/// Giữ nguyên IKhoaHocService nên view học viên không phải sửa.
/// </summary>
public class EfKhoaHocService : IKhoaHocService
{
    private const int LuotHoiToiDa = 30;

    private readonly AppDbContext _db;
    private readonly string? _userId;
    private readonly bool _laNhanVien;    // giảng viên / admin xem như học viên: mở mọi bài, không ghi tiến độ
    private readonly HocOnlineOptions _hocOnline;
    private readonly DuLieuMauService _mau;   // nội dung trang chủ vẫn là dữ liệu mẫu

    public EfKhoaHocService(AppDbContext db, IHttpContextAccessor http, IOptions<HocOnlineOptions> hocOnline)
    {
        _db = db;
        _userId = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _laNhanVien = http.HttpContext?.User is { } u && (u.IsInRole(VaiTro.GiangVien) || u.IsInRole(VaiTro.Admin));
        _hocOnline = hocOnline.Value;
        _mau = new DuLieuMauService(hocOnline);
    }

    // ======================================================= Danh mục khoá theo người học

    private List<KhoaHocChinh>? _danhMuc;

    private List<KhoaHocChinh> DanhMuc()
    {
        if (_danhMuc is not null) return _danhMuc;

        var khoa = _db.Khoa.AsNoTracking().OrderBy(k => k.ThuTu)
            .Include(k => k.ChuDe.OrderBy(c => c.ThuTu)).ThenInclude(c => c.Bai.Where(b => b.DaXuatBan).OrderBy(b => b.ThuTu))
            .ToList();
        var ghiDanh = _db.GhiDanh.AsNoTracking().Where(g => g.NguoiDungId == _userId).ToDictionary(g => g.KhoaId, g => g.HanDen);
        var tienDo = _db.TienDoBai.AsNoTracking().Where(t => t.NguoiDungId == _userId).ToDictionary(t => t.BaiId, t => t.TrangThai);

        _danhMuc = khoa.Select(k =>
        {
            var han = _laNhanVien ? null : ghiDanh.GetValueOrDefault(k.Id);
            var daMo = _laNhanVien || (ghiDanh.ContainsKey(k.Id) && (han is null || han >= DateTime.UtcNow));
            return new KhoaHocChinh(k.Id, k.Ten, k.KyHieu, k.MoTa, k.Anh, daMo, han,
                k.ChuDe.Select(c => new ChuongTrongKhoa(c.Ten, c.Bai.Select(b => new BaiTrongKhoa(
                    b.Id, b.So, b.Ten,
                    tienDo.GetValueOrDefault(b.Id) switch
                    {
                        TrangThaiHoc.DaHoc => TrangThaiBai.DaHoc,
                        TrangThaiHoc.DangHoc => TrangThaiBai.DangHoc,
                        _ => TrangThaiBai.ChuaHoc,
                    },
                    !daMo && !b.HocThu, b.SoMuc, b.SoPhut)).ToList())).ToList());
        }).ToList();
        return _danhMuc;
    }

    private MenuTaiKhoan Menu(string muc) => new(muc, 0, LuotHoiToiDa);

    // ======================================================= Các trang

    public TrangChuViewModel LayTrangChu() => _mau.LayTrangChu();

    public TongQuanViewModel LayTongQuan()
    {
        var nd = _db.Users.AsNoTracking().FirstOrDefault(u => u.Id == _userId);
        var ten = nd?.HoTen is { Length: > 0 } h ? h : nd?.Email ?? "bạn";
        var danhMuc = DanhMuc();
        var khoaChinh = danhMuc.FirstOrDefault(k => k.DaMo) ?? danhMuc.First();
        var tiep = khoaChinh.BaiHocTiep;

        var bayNgay = DateTime.UtcNow.AddDays(-7);
        var baiLam = _db.BaiLam.AsNoTracking().Where(b => b.NguoiDungId == _userId).OrderByDescending(b => b.NopLuc).ToList();
        var tuanNay = baiLam.Where(b => b.NopLuc >= bayNgay).ToList();
        var cauTuan = tuanNay.Sum(b => b.SoCau);

        // Chuỗi ngày học: số ngày liên tiếp (tính đến hôm nay) có nộp bài
        var ngay = baiLam.Select(b => b.NopLuc.ToLocalTime().Date).ToHashSet();
        var chuoi = 0;
        for (var d = DateTime.Today; ngay.Contains(d); d = d.AddDays(-1)) chuoi++;

        // Nên ôn lại: bài có lần làm gần nhất dưới 80%
        var nenOn = baiLam.Where(b => !b.LaDeThi).GroupBy(b => b.MaDe).Select(g => g.First())
            .Where(b => b.SoCau > 0 && b.SoDung < b.SoCau * 0.8).Select(b => b.TenDe).Take(3).ToList();

        return new TongQuanViewModel
        {
            TenHocSinh = ten,
            ChuCaiDaiDien = string.Concat(ten.Split(' ', StringSplitOptions.RemoveEmptyEntries).TakeLast(2).Select(x => char.ToUpper(x[0]))),
            NgayHomNay = DateTime.Today,
            LuotHoiDaDung = 0,
            LuotHoiToiDa = LuotHoiToiDa,
            TiepTuc = new TiepTucHoc(khoaChinh.Ten, tiep is null ? "Đã học hết các bài" : $"Bài {tiep.So}. {tiep.Ten}",
                tiep is null ? "" : $"{tiep.SoMuc} mục", khoaChinh.TienDoPhanTram),
            ThongKe = new ThongKeTuan(
                danhMuc.Sum(k => k.BaiDaHoc), cauTuan,
                cauTuan == 0 ? 0 : (int)Math.Round(tuanNay.Sum(b => b.SoDung) * 100.0 / cauTuan), chuoi),
            KhoaCuaToi = danhMuc.Select(k => new KhoaCuaToi(k.Id, k.Ten, k.KyHieu, k.DaMo, k.BaiDaHoc, k.TongBai, k.HanDen)).ToList(),
            KetQuaGanDay = baiLam.Take(3).Select(b => new KetQuaBai(b.TenDe, KhiNao(b.NopLuc), b.SoDung, b.SoCau)).ToList(),
            ChuDeNenOn = nenOn,
        };
    }

    private static string KhiNao(DateTime utc)
    {
        var soNgay = (DateTime.Today - utc.ToLocalTime().Date).Days;
        return soNgay switch { 0 => "Hôm nay", 1 => "Hôm qua", _ => $"{soNgay} ngày trước" };
    }

    public KhoaHocViewModel LayKhoaHoc(string? khoaId)
    {
        var danhMuc = DanhMuc();
        var chon = danhMuc.FirstOrDefault(k => k.Id == khoaId);
        return new KhoaHocViewModel { Menu = Menu("KhoaHoc"), Khoa = danhMuc, DangChon = chon, HocOnline = LayHocOnline(chon) };
    }

    // Buổi học online: lịch trong database (giảng viên đặt) trước, sau đó link chung / theo bài trong appsettings
    private Dictionary<int, BuoiOnline> LayHocOnline(KhoaHocChinh? khoa)
    {
        var kq = new Dictionary<int, BuoiOnline>();
        if (khoa is null) return kq;
        var ids = khoa.TatCaBai.Where(b => !b.BiKhoa).Select(b => b.Id).ToList();
        // Mỗi bài có thể có nhiều buổi: lấy buổi sắp tới gần nhất, nếu đã qua hết thì buổi gần nhất
        var bayGio = DateTime.UtcNow;
        var trongDb = _db.BuoiHoc.AsNoTracking().Where(x => ids.Contains(x.BaiId)).ToList()
            .GroupBy(x => x.BaiId)
            .ToDictionary(g => g.Key, g => g.Where(x => x.ThoiGian >= bayGio).OrderBy(x => x.ThoiGian).FirstOrDefault()
                                            ?? g.OrderByDescending(x => x.ThoiGian).First());
        foreach (var id in ids)
        {
            var rieng = _hocOnline.TheoBai.GetValueOrDefault(id);
            var (link, gio) = trongDb.TryGetValue(id, out var bh) ? (bh.Link, bh.ThoiGian?.ToLocalTime())
                : (string.IsNullOrWhiteSpace(rieng?.Link) ? _hocOnline.LinkChung : rieng.Link, rieng?.ThoiGian);
            if (Uri.TryCreate(link?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                kq[id] = new BuoiOnline(uri.ToString(), gio);
        }
        return kq;
    }

    public BaiHocViewModel LayBaiHoc(int? id)
    {
        var danhMuc = DanhMuc();
        var khoa = danhMuc.FirstOrDefault(k => k.TatCaBai.Any(b => b.Id == id))
                   ?? danhMuc.FirstOrDefault(k => k.DaMo) ?? danhMuc.First();
        var bai = khoa.TatCaBai.FirstOrDefault(b => b.Id == id) ?? khoa.BaiHocTiep ?? khoa.TatCaBai.First();
        var chuong = khoa.Chuong.First(c => c.Bai.Contains(bai));

        var noiDung = bai.BiKhoa ? [] : _db.Muc.AsNoTracking().Where(m => m.BaiId == bai.Id).OrderBy(m => m.So)
            .Include(m => m.Phan.OrderBy(p => p.ThuTu)).ThenInclude(p => p.Moc.OrderBy(x => x.ThuTu))
            .ToList()
            .Select(m => new MucNoiDung(m.So, m.Ten, m.TieuDe, m.SoPhut, m.Phan.Select(p => new PhanNoiDung(
                p.MaNeo, p.TieuDe, p.DoanVan,
                p.Moc.Count == 0 ? null : p.Moc.Select(x => new DotItem(x.Ten, x.ThoiGian, x.NoiDung)).ToList(),
                p.LaGhiNho)).ToList()))
            .ToList();

        // Mở bài lần đầu: ghi nhận đang học
        if (_userId is not null && !_laNhanVien && !bai.BiKhoa && bai.TrangThai == TrangThaiBai.ChuaHoc)
        {
            _db.TienDoBai.Add(new TienDoBai { NguoiDungId = _userId, BaiId = bai.Id, TrangThai = TrangThaiHoc.DangHoc });
            _db.SaveChanges();
        }

        return new BaiHocViewModel
        {
            KhoaId = khoa.Id,
            TenKhoa = khoa.Ten,
            BaiDaHoc = khoa.BaiDaHoc,
            TongBai = khoa.TongBai,
            Chuong = [new(chuong.Ten, chuong.Bai.Select(b => new BaiHocItem(b.Id, b.So, b.Ten, b.TrangThai, b.BiKhoa)).ToList())],
            BaiId = bai.Id,
            SoBaiHienTai = bai.So,
            BiKhoa = bai.BiKhoa,
            NoiDung = noiDung,
            NhanBai = $"BÀI {bai.So} · {(noiDung.Count > 0 ? noiDung.Count : bai.SoMuc)} MỤC",
            TieuDe = bai.Ten,
            MoDau = "Nội dung bài này sẽ hiện khi nạp tài liệu khoá học.",
            GoiYHoi = noiDung.Count > 0 ? ["Tóm tắt bài này", "Giải thích dễ hiểu hơn"] : ["Tóm tắt bài này"],
        };
    }

    public LuyenDeViewModel LayLuyenDe(string? khoaId)
    {
        var danhMuc = DanhMuc();
        var soCau = _db.CauHoi.AsNoTracking().GroupBy(c => c.BaiId).Select(g => new { g.Key, N = g.Count() }).ToDictionary(x => x.Key, x => x.N);
        // Kết quả gần nhất của mỗi bài / đề
        var ganNhat = _db.BaiLam.AsNoTracking().Where(b => b.NguoiDungId == _userId).OrderByDescending(b => b.NopLuc).ToList()
            .GroupBy(b => (b.MaDe, b.LaDeThi)).ToDictionary(g => g.Key, g => g.First());
        var deThi = _db.DeThi.AsNoTracking().Where(d => d.DangMo).OrderBy(d => d.Id)
            .Select(d => new { d.Id, d.KhoaId, d.Ten, d.SoPhut, SoCau = d.Cau.Count }).ToList();

        var tatCa = danhMuc.Select(k => new KhoaLuyen(k,
            k.Chuong.Select(c => new ChuongLuyen(c.Ten, c.Bai.Select(b => new BaiLuyen(
                b.Id, b.So, b.Ten, soCau.GetValueOrDefault(b.Id),
                ganNhat.TryGetValue((b.Id, false), out var bl) ? bl.SoDung : null, b.BiKhoa)).ToList())).ToList(),
            deThi.Where(d => d.KhoaId == k.Id).Select(d => new DeThiThu(d.Id, d.Ten, d.SoCau, d.SoPhut,
                ganNhat.TryGetValue((d.Id, true), out var bl) ? bl.SoDung : null, !k.DaMo)).ToList())).ToList();

        return new LuyenDeViewModel { Menu = Menu("LuyenDe"), Khoa = tatCa, DangChon = tatCa.FirstOrDefault(k => k.Khoa.Id == khoaId) };
    }

    // ======================================================= Làm đề

    private static PhuongAn[] PA(List<string> x) => x.Select((nd, i) => new PhuongAn(((char)('A' + i)).ToString(), nd)).ToArray();

    private static CauHoiDe Doi(CauHoi c) =>
        new(c.NoiDung, PA(c.PhuongAn), c.DapAnDung, c.GiaiThich, c.BaiId, c.Muc, c.NguonHienThi, c.Id);

    public LamDeViewModel LayDe(int? id, IReadOnlyDictionary<int, string>? daChon)
    {
        var ma = id ?? 7;
        var danhMuc = DanhMuc();
        var de = ma >= 1000 ? _db.DeThi.AsNoTracking().FirstOrDefault(d => d.Id == ma) : null;

        if (de is not null)
        {
            var khoa = danhMuc.First(k => k.Id == de.KhoaId);
            var cau = !khoa.DaMo ? [] : _db.DeThiCau.AsNoTracking().Where(x => x.DeThiId == ma).OrderBy(x => x.ThuTu)
                .Select(x => x.CauHoi!).ToList().Select(Doi).ToList();
            return new LamDeViewModel { Id = ma, KhoaId = khoa.Id, TenDe = de.Ten, LaDeThi = true, SoPhut = de.SoPhut, CauHoi = cau, DaChon = daChon };
        }

        var khoaBai = danhMuc.FirstOrDefault(k => k.TatCaBai.Any(b => b.Id == ma)) ?? danhMuc.First();
        var bai = khoaBai.TatCaBai.FirstOrDefault(b => b.Id == ma);
        var cauBai = bai is null || bai.BiKhoa ? [] : _db.CauHoi.AsNoTracking().Where(c => c.BaiId == ma).OrderBy(c => c.ThuTu).ToList().Select(Doi).ToList();
        return new LamDeViewModel
        {
            Id = ma, KhoaId = khoaBai.Id, TenDe = bai is null ? "Bài tập" : $"Luyện tập · Bài {bai.So}",
            CauHoi = cauBai, DaChon = daChon,
        };
    }

    public void LuuBaiLam(LamDeViewModel daNop)
    {
        if (_userId is null || _laNhanVien || !daNop.DaNop || daNop.CauHoi.Count == 0) return;

        _db.BaiLam.Add(new BaiLam
        {
            NguoiDungId = _userId, MaDe = daNop.Id, LaDeThi = daNop.LaDeThi, TenDe = daNop.TenDe,
            SoCau = daNop.CauHoi.Count, SoDung = daNop.SoDung,
            TraLoi = daNop.CauHoi.Select((c, i) => new CauTraLoi { CauHoiId = c.Id, ThuTu = i + 1, DaChon = daNop.Chon(i + 1), Dung = daNop.Dung(i + 1) }).ToList(),
        });

        // Nộp bài tập của bài → coi như đã học xong bài đó
        if (!daNop.LaDeThi)
        {
            var td = _db.TienDoBai.Find(_userId, daNop.Id);
            if (td is null) _db.TienDoBai.Add(new TienDoBai { NguoiDungId = _userId, BaiId = daNop.Id, TrangThai = TrangThaiHoc.DaHoc });
            else { td.TrangThai = TrangThaiHoc.DaHoc; td.CapNhat = DateTime.UtcNow; }
        }
        _db.SaveChanges();
    }
}
