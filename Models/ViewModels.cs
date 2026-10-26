using System.Globalization;

namespace SuDia.Web.Models;

// ======================================================= Trang chủ
public record BuocHoc(string SoLaMa, string Ten, string MoTa);

public record MonHoc(
    string Nhan,              // "Môn thứ nhất"
    string Ten,               // "Lịch sử 12"
    string MoTa,
    string MauNhan,           // màu nhãn / số La Mã
    string Anh,               // ảnh bìa, dùng chung với trang Khoá của tôi
    int BaiDauId,             // bài học thử: /Home/BaiHoc/{BaiDauId}
    IReadOnlyList<string> Chuong);

public record GoiHoc(
    string Nhan,              // "Một môn" / "Trọn bộ"
    string Ten,
    decimal? Gia,             // null => hiển thị [GIÁ]
    string MauNhan,
    bool NoiBat,
    IReadOnlyList<string> QuyenLoi);

public record DongSoSanh(string QuyenLoi, string HocThu, string MotMon, string TronBo);

public class TrangChuViewModel
{
    public IReadOnlyList<string> CauHoiGoiY { get; init; } = [];
    public IReadOnlyList<BuocHoc> BuocHoc { get; init; } = [];
    public IReadOnlyList<MonHoc> MonHoc { get; init; } = [];
    public IReadOnlyList<GoiHoc> GoiHoc { get; init; } = [];
    public IReadOnlyList<DongSoSanh> SoSanh { get; init; } = [];
}

// ======================================================= Tổng quan
public record TiepTucHoc(string Mon, string Bai, string Muc, int TienDoPhanTram);

public record ThongKeTuan(int BaiDaHoc, int CauDaLam, int TiLeDung, int ChuoiNgay);

public record KhoaCuaToi(string Id, string Ten, string KyHieu, bool DaMo, int BaiDaHoc, int TongBai, DateTime? HanDen)
{
    public int TienDoPhanTram => TongBai == 0 ? 0 : (int)Math.Round(BaiDaHoc * 100.0 / TongBai);
}

public record KetQuaBai(string Ten, string ThoiGian, int Diem, int Tong)
{
    public bool Tot => Tong > 0 && Diem >= Tong * 0.8;
}

public class TongQuanViewModel
{
    public string TenHocSinh { get; init; } = "";
    public string ChuCaiDaiDien { get; init; } = "";
    public DateTime NgayHomNay { get; init; }
    public int LuotHoiDaDung { get; init; }
    public int LuotHoiToiDa { get; init; }
    public MenuTaiKhoan Menu => new("TongQuan", LuotHoiDaDung, LuotHoiToiDa);
    public required TiepTucHoc TiepTuc { get; init; }
    public required ThongKeTuan ThongKe { get; init; }
    public IReadOnlyList<KhoaCuaToi> KhoaCuaToi { get; init; } = [];
    public IReadOnlyList<KetQuaBai> KetQuaGanDay { get; init; } = [];
    public IReadOnlyList<string> ChuDeNenOn { get; init; } = [];
}

// ======================================================= Menu trang tài khoản (dùng chung)
public record MenuTaiKhoan(string MucDangMo, int LuotHoiDaDung, int LuotHoiToiDa)
{
    public int LuotHoiPhanTram => LuotHoiToiDa == 0 ? 0 : (int)Math.Round(LuotHoiDaDung * 100.0 / LuotHoiToiDa);
}

// ======================================================= Khoá học của tôi
public enum TrangThaiBai { DaHoc, DangHoc, ChuaHoc }

/// <param name="Id">Mã bài dùng trên đường dẫn /Home/BaiHoc/{Id}, không trùng giữa các khoá</param>
/// <param name="BiKhoa">Khoá chưa mua thì chỉ học thử bài đầu, các bài khác bị khoá</param>
public record BaiTrongKhoa(int Id, int So, string Ten, TrangThaiBai TrangThai, bool BiKhoa, int SoMuc, int SoPhut);

public record ChuongTrongKhoa(string Ten, IReadOnlyList<BaiTrongKhoa> Bai);

public record KhoaHocChinh(
    string Id,                // "lich-su-12"
    string Ten,
    string KyHieu,            // "S"
    string MoTa,
    string Anh,               // ảnh bìa, đường dẫn trong wwwroot
    bool DaMo,
    DateTime? HanDen,
    IReadOnlyList<ChuongTrongKhoa> Chuong)
{
    public IEnumerable<BaiTrongKhoa> TatCaBai => Chuong.SelectMany(c => c.Bai);
    public int TongBai => TatCaBai.Count();
    public int BaiDaHoc => TatCaBai.Count(b => b.TrangThai == TrangThaiBai.DaHoc);
    public int TienDoPhanTram => TongBai == 0 ? 0 : (int)Math.Round(BaiDaHoc * 100.0 / TongBai);
    public int TongPhut => TatCaBai.Sum(b => b.SoPhut);

    /// <summary>Bài để "Học tiếp": bài đang học, nếu chưa có thì bài đầu tiên chưa học được mở.</summary>
    public BaiTrongKhoa? BaiHocTiep =>
        TatCaBai.FirstOrDefault(b => b.TrangThai == TrangThaiBai.DangHoc)
        ?? TatCaBai.FirstOrDefault(b => b.TrangThai == TrangThaiBai.ChuaHoc && !b.BiKhoa);
}

public class KhoaHocViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public IReadOnlyList<KhoaHocChinh> Khoa { get; init; } = [];
    public KhoaHocChinh? DangChon { get; init; }
}

// ======================================================= Luyện đề (gắn với khoá học)
/// <param name="Dung">Số câu đúng lần làm gần nhất; null = chưa làm</param>
public record BaiLuyen(int BaiId, int So, string Ten, int SoCau, int? Dung, bool BiKhoa)
{
    public bool DaLam => Dung is not null;
    public bool Tot => Dung is not null && Dung >= SoCau * 0.8;
}

public record ChuongLuyen(string Ten, IReadOnlyList<BaiLuyen> Bai);

public record DeThiThu(int Id, string Ten, int SoCau, int SoPhut, int? Dung, bool BiKhoa)
{
    public bool Tot => Dung is not null && Dung >= SoCau * 0.8;
}

public record KhoaLuyen(KhoaHocChinh Khoa, IReadOnlyList<ChuongLuyen> Chuong, IReadOnlyList<DeThiThu> DeThi)
{
    public IEnumerable<BaiLuyen> TatCaBai => Chuong.SelectMany(c => c.Bai);
    public int DaLuyen => TatCaBai.Count(b => b.DaLam);
    public int TongBai => TatCaBai.Count();
    public int TienDoPhanTram => TongBai == 0 ? 0 : (int)Math.Round(DaLuyen * 100.0 / TongBai);

    /// <summary>Điểm trung bình thang 10 của các bài đã luyện.</summary>
    public double? DiemTrungBinh => DaLuyen == 0 ? null
        : Math.Round(TatCaBai.Where(b => b.DaLam).Average(b => b.Dung!.Value * 10.0 / b.SoCau), 1);

    /// <summary>Bài nên luyện tiếp: bài đầu tiên chưa làm và không bị khoá.</summary>
    public BaiLuyen? BaiLuyenTiep => TatCaBai.FirstOrDefault(b => !b.DaLam && !b.BiKhoa);
}

public class LuyenDeViewModel
{
    public required MenuTaiKhoan Menu { get; init; }
    public IReadOnlyList<KhoaLuyen> Khoa { get; init; } = [];
    public KhoaLuyen? DangChon { get; init; }
}

/// <summary>Ô khoá dùng chung: Luyen = null là thẻ học, có Luyen là thẻ luyện đề.</summary>
public record TheKhoaModel(KhoaHocChinh Khoa, KhoaLuyen? Luyen = null);

// ======================================================= Bài học
public record BaiHocItem(int Id, int So, string Ten, TrangThaiBai TrangThai, bool BiKhoa);

public record ChuongItem(string Ten, IReadOnlyList<BaiHocItem> Bai);

public record DotItem(string Ten, string ThoiGian, string NoiDung);

/// <summary>Một phần trong mục: tiêu đề + đoạn văn, có thể kèm các mốc (dòng thời gian).</summary>
/// <param name="Id">Neo trong trang, ví dụ "dien-bien"; mục lục bấm vào nhảy tới đây</param>
/// <param name="LaGhiNho">Hiện kiểu ghi nhớ (vạch màu bên trái)</param>
public record PhanNoiDung(string Id, string TieuDe, IReadOnlyList<string> DoanVan, IReadOnlyList<DotItem>? Moc = null, bool LaGhiNho = false);

/// <summary>Một mục của bài. Bài là tài liệu dài gồm nhiều mục nối tiếp.</summary>
public record MucNoiDung(int So, string Ten, string TieuDe, int SoPhut, IReadOnlyList<PhanNoiDung> Phan);

public record TinNhan(bool LaHocSinh, string NoiDung, string? Nguon = null, bool LaTuChoi = false);

public class BaiHocViewModel
{
    public string KhoaId { get; init; } = "";
    public string TenKhoa { get; init; } = "";
    public int BaiDaHoc { get; init; }
    public int TongBai { get; init; }
    public IReadOnlyList<ChuongItem> Chuong { get; init; } = [];   // chủ đề chứa bài đang xem
    public int BaiId { get; init; }
    public int SoBaiHienTai { get; init; }
    public bool BiKhoa { get; init; }
    public bool CoNoiDung => NoiDung.Count > 0;

    /// <summary>Cả bài trên một trang dài: các mục nối tiếp nhau, kéo xuống để đọc.</summary>
    public IReadOnlyList<MucNoiDung> NoiDung { get; init; } = [];

    public string NhanBai { get; init; } = "";
    public string TieuDe { get; init; } = "";
    public string MoDau { get; init; } = "";      // lời nhắn khi bài chưa có nội dung
    public IReadOnlyList<TinNhan> TinNhanMau { get; init; } = [];
    public IReadOnlyList<string> GoiYHoi { get; init; } = [];
}

// ======================================================= Làm câu hỏi
public enum TrangThaiCau { ChuaLam, Dung, Sai }

public record PhuongAn(string Ky, string NoiDung);

public record OCauHoi(int So, TrangThaiCau TrangThai, bool DangLam);

public class LamCauHoiViewModel
{
    public int Id { get; init; }
    public string KhoaId { get; init; } = "";
    public string TenBai { get; init; } = "";
    public int CauSo { get; init; }
    public int TongCau { get; init; }
    public int TienDoPhanTram => TongCau == 0 ? 0 : (int)Math.Round(CauSo * 100.0 / TongCau);
    public string MucDo { get; init; } = "";
    public string NoiDung { get; init; } = "";
    public IReadOnlyList<PhuongAn> PhuongAn { get; init; } = [];
    public string DapAnDung { get; init; } = "";
    public string? DapAnDaChon { get; init; }
    public bool DaTraLoi => DapAnDaChon is not null;
    public bool TraLoiDung => DapAnDaChon == DapAnDung;
    public string GiaiThich { get; init; } = "";
    public string NguonMuc { get; init; } = "";
    public IReadOnlyList<OCauHoi> BangCau { get; init; } = [];
    public string ThoiGianConLai { get; init; } = "";
}

// ======================================================= Định dạng dùng chung
public static class DinhDang
{
    private static readonly CultureInfo Vi = new("vi-VN");

    public static string Gia(decimal? gia) =>
        gia is null ? "[GIÁ]" : gia.Value.ToString("#,##0", Vi) + " đ";

    public static string NgayDauTrang(DateTime d) => d.ToString("dddd, dd/MM", Vi).ToUpper(Vi);

    /// <summary>Điểm thang 10 kiểu Việt Nam: 8,7</summary>
    public static string Diem(double? d) => d is null ? "—" : d.Value.ToString("0.0", Vi);

    public static string Ngay(DateTime? d) => d is null ? "[ngày]" : d.Value.ToString("dd/MM/yyyy", Vi);

    public static string LaMa(int n) => n switch
    {
        1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V",
        6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX", 10 => "X",
        _ => n.ToString(Vi)
    };
}
