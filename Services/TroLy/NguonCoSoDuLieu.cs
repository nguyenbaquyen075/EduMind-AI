using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Services.TroLy;

/// <summary>
/// Biến nội dung đã nhập trong hệ thống thành các đoạn cho chỉ mục (DoanChiMuc), theo từng đơn vị nguồn
/// để chỉ cập nhật phần vừa đổi:
/// - "Bai:{id}": mỗi Mục của bài đã xuất bản trong khoá đang hiện = một đoạn (chia nhỏ nếu dài);
/// - "TaiLieu:{id}": tài liệu giảng viên tải lên (đã đọc được chữ, đang cho học viên xem), chia theo tiêu đề / trang;
/// - "Khoa:{id}": thông tin khoá (mô tả, mục tiêu, đối tượng, giảng viên, danh sách bài);
/// - "Goi": các gói đang bán (giá, thời hạn, khoá được học, quyền lợi) để trả lời khách.
/// Bài nháp, khoá ẩn, tài liệu nội bộ không được đưa vào.
/// </summary>
public static class NguonCoSoDuLieu
{
    private static readonly CultureInfo Vi = new("vi-VN");

    public static string KhoaBai(int id) => $"Bai:{id}";
    public static string KhoaTaiLieu(int id) => $"TaiLieu:{id}";
    public static string KhoaThongTin(string khoaId) => $"Khoa:{khoaId}";
    public const string KhoaGoi = "Goi";

    // ---------------------------------------------------------------- Từng đơn vị

    public static async Task<List<DoanTaiLieu>> DoanCuaBaiAsync(AppDbContext db, int baiId, CancellationToken ct)
    {
        var bai = await db.Bai.AsNoTracking().AsSplitQuery()
            .Where(b => b.Id == baiId && b.DaXuatBan && b.ChuDe!.Khoa!.HienThi)
            .Include(b => b.ChuDe!).ThenInclude(c => c.Khoa)
            .Include(b => b.Muc.OrderBy(m => m.So)).ThenInclude(m => m.Phan.OrderBy(p => p.ThuTu)).ThenInclude(p => p.Moc.OrderBy(x => x.ThuTu))
            .FirstOrDefaultAsync(ct);
        return bai is null ? [] : DoanBai(bai.ChuDe!.Khoa!, bai);
    }

    public static async Task<List<DoanTaiLieu>> DoanCuaTaiLieuAsync(AppDbContext db, int id, CancellationToken ct)
    {
        var t = await db.TaiLieuKhoa.AsNoTracking()
            .Where(t => t.Id == id && t.HienChoHocVien && t.TrangThai == TrangThaiTaiLieu.SanSang && t.Khoa!.HienThi)
            .Select(t => new { t.Id, t.KhoaId, KhoaTen = t.Khoa!.Ten, t.Ten, t.NoiDung }).FirstOrDefaultAsync(ct);
        return t is null ? [] : DoanTaiLieuTaiLen(t.Id, t.KhoaId, t.KhoaTen, t.Ten, t.NoiDung);
    }

    public static async Task<List<DoanTaiLieu>> DoanThongTinKhoaAsync(AppDbContext db, string khoaId, CancellationToken ct)
    {
        var k = await db.Khoa.AsNoTracking().Where(k => k.Id == khoaId && k.HienThi).Include(k => k.GiangVien).FirstOrDefaultAsync(ct);
        if (k is null) return [];
        var bai = await db.Bai.AsNoTracking().Where(b => b.ChuDe!.KhoaId == khoaId && b.DaXuatBan)
            .OrderBy(b => b.ChuDe!.ThuTu).ThenBy(b => b.ThuTu).Select(b => new { b.So, b.Ten }).ToListAsync(ct);
        return DoanThongTinKhoa(k, bai.Select(b => (b.So, b.Ten)).ToList()).ToList();
    }

    public static async Task<List<DoanTaiLieu>> DoanGoiAsync(AppDbContext db, CancellationToken ct)
    {
        var tenKhoa = await db.Khoa.AsNoTracking().Where(k => k.HienThi).ToDictionaryAsync(k => k.Id, k => k.Ten, ct);
        var goi = await db.GoiBan.AsNoTracking().Where(g => g.DangBan).OrderBy(g => g.ThuTu).ToListAsync(ct);
        return goi.Select(g => new DoanTaiLieu("", "Gói học", $"Gói học · {g.Ten}", NoiDungGoi(g, tenKhoa), LoaiDoan.GoiHoc, $"/ThanhToan/Mua/{g.Id}")).ToList();
    }

    /// <summary>Mọi đơn vị của một khoá (thông tin khoá, các bài, các tài liệu), theo khoá đơn vị.</summary>
    public static async Task<Dictionary<string, List<DoanTaiLieu>>> DoanCuaKhoaAsync(AppDbContext db, string khoaId, CancellationToken ct)
    {
        var ra = new Dictionary<string, List<DoanTaiLieu>> { [KhoaThongTin(khoaId)] = await DoanThongTinKhoaAsync(db, khoaId, ct) };
        foreach (var id in await db.Bai.Where(b => b.ChuDe!.KhoaId == khoaId).Select(b => b.Id).ToListAsync(ct))
            ra[KhoaBai(id)] = await DoanCuaBaiAsync(db, id, ct);
        foreach (var id in await db.TaiLieuKhoa.Where(t => t.KhoaId == khoaId).Select(t => t.Id).ToListAsync(ct))
            ra[KhoaTaiLieu(id)] = await DoanCuaTaiLieuAsync(db, id, ct);
        return ra;
    }

    // ---------------------------------------------------------------- Dựng đoạn

    private static List<DoanTaiLieu> DoanBai(Khoa k, Bai bai)
    {
        var ra = new List<DoanTaiLieu>();
        foreach (var muc in bai.Muc)
        {
            var noiDung = NoiDungMuc(bai, muc);
            if (noiDung.Length == 0) continue;
            var nguon = $"{k.Ten} · Bài {bai.So} · Mục {muc.So} — {TenMuc(muc)}";
            foreach (var phan in KhoTaiLieu.ChiaNho(noiDung))
                ra.Add(new DoanTaiLieu(k.Id, k.Ten, nguon, phan, LoaiDoan.BaiHoc, $"/Home/BaiHoc/{bai.Id}#muc-{muc.So}"));
        }
        return ra;
    }

    private static List<DoanTaiLieu> DoanTaiLieuTaiLen(int id, string khoaId, string khoaTen, string ten, string noiDung) =>
        TaiLieuKhoaService.ChiaDoan(noiDung).Select(x => new DoanTaiLieu(khoaId, khoaTen,
            $"{khoaTen} · Tài liệu: {ten}" + (x.TieuDe.Length > 0 ? $" — {x.TieuDe}" : ""),
            $"Tài liệu: {ten}\n\n{x.NoiDung}", LoaiDoan.TaiLieu, $"/Home/TaiLieu/Mo/{id}")).ToList();

    private static string TenMuc(Muc m) =>
        string.IsNullOrWhiteSpace(m.TieuDe) || m.TieuDe == m.Ten ? m.Ten : $"{m.Ten} ({m.TieuDe})";

    private static string NoiDungMuc(Bai bai, Muc muc)
    {
        var sb = new StringBuilder();
        foreach (var p in muc.Phan)
        {
            var doan = p.DoanVan.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
            if (doan.Count == 0 && p.Moc.Count == 0) continue;
            if (!string.IsNullOrWhiteSpace(p.TieuDe)) sb.Append(p.TieuDe.Trim()).Append(p.LaGhiNho ? " (cần ghi nhớ)" : "").Append(":\n");
            foreach (var d in doan) sb.Append(d).Append("\n\n");
            foreach (var m in p.Moc)
            {
                sb.Append("- ");
                if (!string.IsNullOrWhiteSpace(m.ThoiGian)) sb.Append(m.ThoiGian.Trim()).Append(": ");
                if (!string.IsNullOrWhiteSpace(m.Ten)) sb.Append(m.Ten.Trim()).Append(m.NoiDung.Length > 0 ? " — " : "");
                sb.Append(m.NoiDung.Trim()).Append('\n');
            }
            sb.Append('\n');
        }
        var text = sb.ToString().Trim();
        // Dòng đầu nêu tên bài để câu hỏi theo tên bài vẫn tìm ra đoạn này
        return text.Length == 0 ? "" : $"Bài {bai.So}. {bai.Ten}\n\n{text}";
    }

    private static IEnumerable<DoanTaiLieu> DoanThongTinKhoa(Khoa k, List<(int So, string Ten)> bai)
    {
        var sb = new StringBuilder($"Khoá học: {k.Ten} (môn {k.Mon}).");
        if (!string.IsNullOrWhiteSpace(k.MoTa)) sb.Append('\n').Append(k.MoTa.Trim());
        var mucTieu = k.MucTieu.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        if (mucTieu.Count > 0) sb.Append("\nHọc xong khoá này, học viên sẽ:\n").Append(string.Join('\n', mucTieu.Select(x => "- " + x)));
        if (!string.IsNullOrWhiteSpace(k.DoiTuong)) sb.Append("\nDành cho: ").Append(k.DoiTuong.Trim());
        var gv = k.GiangVien?.HoTen;
        if (!string.IsNullOrWhiteSpace(gv)) sb.Append("\nGiảng viên phụ trách: ").Append(gv.Trim());
        if (bai.Count > 0)
        {
            sb.Append($"\nKhoá có {bai.Count} bài học:\n");
            foreach (var b in bai) sb.Append($"- Bài {b.So}. {b.Ten}\n");
        }
        foreach (var phan in KhoTaiLieu.ChiaNho(sb.ToString().Trim()))
            yield return new DoanTaiLieu(k.Id, k.Ten, $"{k.Ten} · Thông tin khoá", phan, LoaiDoan.ThongTinKhoa, $"/Home/KhoaHoc/{k.Id}");
    }

    private static string NoiDungGoi(GoiBan g, Dictionary<string, string> tenKhoa)
    {
        var sb = new StringBuilder($"Gói học: {g.Ten}");
        if (!string.IsNullOrWhiteSpace(g.Nhan)) sb.Append($" ({g.Nhan.Trim()})");
        sb.Append('.');
        sb.Append(g.Gia is > 0 ? $"\nGiá: {g.Gia.Value.ToString("N0", Vi)} đồng." : "\nGiá: chưa công bố.");
        sb.Append($"\nThời hạn học: {g.SoThang} tháng kể từ khi mua.");
        var khoa = g.KhoaIds.Select(id => tenKhoa.GetValueOrDefault(id)).Where(t => t is not null).ToList();
        if (khoa.Count > 0) sb.Append("\nGồm các khoá: ").Append(string.Join(", ", khoa)).Append('.');
        var quyenLoi = g.QuyenLoi.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (quyenLoi.Count > 0) sb.Append("\nQuyền lợi:\n").Append(string.Join('\n', quyenLoi.Select(x => "- " + x.Trim())));
        return sb.ToString();
    }
}
