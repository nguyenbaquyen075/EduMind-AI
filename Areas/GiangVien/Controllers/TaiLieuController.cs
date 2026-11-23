using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Kho tài liệu của khoá: giảng viên tải tệp gốc lên, học viên tìm và mở, trợ lý AI đọc để trả lời
public class TaiLieuController(AppDbContext db, TaiLieuKhoaService kho) : GiangVienControllerBase(db)
{
    private const int ToiDaTepMotLan = 10;

    public async Task<IActionResult> Index(string? khoa, string? q)
    {
        var dsKhoa = await KhoaCuaToi().AsNoTracking().OrderBy(k => k.ThuTu).Select(k => new { k.Id, k.Ten }).ToListAsync();
        if (dsKhoa.Count == 0) return View(new GvKhoTaiLieuViewModel());
        khoa = dsKhoa.Any(k => k.Id == khoa) ? khoa : dsKhoa[0].Id;

        var ds = Db.TaiLieuKhoa.AsNoTracking().Where(t => t.KhoaId == khoa);
        if (!string.IsNullOrWhiteSpace(q)) ds = ds.Where(t => EF.Functions.ILike(t.Ten, $"%{q.Trim()}%") || EF.Functions.ILike(t.TenTep, $"%{q.Trim()}%"));
        return View(new GvKhoTaiLieuViewModel
        {
            DsKhoa = dsKhoa.Select(k => (k.Id, k.Ten)).ToList(),
            KhoaId = khoa,
            TuKhoa = q,
            Bai = await Db.Bai.AsNoTracking().Where(b => b.ChuDe!.KhoaId == khoa).OrderBy(b => b.ChuDe!.ThuTu).ThenBy(b => b.ThuTu)
                .Select(b => new BaiChon(b.Id, $"Bài {b.So}. {b.Ten}")).ToListAsync(),
            TaiLieu = await ds.OrderByDescending(t => t.TaiLuc)
                .Select(t => new GvTaiLieuDong(t.Id, t.Ten, t.TenTep, t.LoaiTep, t.KichThuoc, t.TrangThai, t.LoiDoc, t.HienChoHocVien,
                    t.Bai == null ? null : $"Bài {t.Bai.So}. {t.Bai.Ten}", t.NguoiTai == null ? null : t.NguoiTai.HoTen, t.TaiLuc, t.NoiDung.Length))
                .ToListAsync(),
        });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(ToiDaTepMotLan * TaiLieuKhoaService.ToiDaByte + 1_000_000),
     RequestFormLimits(MultipartBodyLengthLimit = ToiDaTepMotLan * TaiLieuKhoaService.ToiDaByte + 1_000_000)]
    public async Task<IActionResult> Tai(string khoaId, int? baiId, List<IFormFile>? tep, CancellationToken ct)
    {
        if (!await KhoaCuaToi().AnyAsync(k => k.Id == khoaId, ct)) return NotFound();
        if (baiId is not null && !await Db.Bai.AnyAsync(b => b.Id == baiId && b.ChuDe!.KhoaId == khoaId, ct)) baiId = null;
        tep = (tep ?? []).Where(f => f.Length > 0).ToList();
        if (tep.Count == 0) { BaoLoi("Chọn ít nhất một tệp."); return RedirectToAction(nameof(Index), new { khoa = khoaId }); }
        if (tep.Count > ToiDaTepMotLan) { BaoLoi($"Mỗi lần tải tối đa {ToiDaTepMotLan} tệp."); return RedirectToAction(nameof(Index), new { khoa = khoaId }); }

        int moi = 0, trung = 0, khongDocDuoc = 0;
        var loi = new List<string>();
        foreach (var f in tep)
        {
            var kq = await kho.LuuAsync(f, khoaId, baiId, null, Uid, ct);
            if (kq.Loi is not null) loi.Add(kq.Loi);
            else if (kq.DaCo) trung++;
            else { moi++; if (kq.TaiLieu!.TrangThai == TrangThaiTaiLieu.Loi) khongDocDuoc++; }
        }
        var thongBao = $"Đã thêm {moi} tài liệu vào kho." + (trung > 0 ? $" {trung} tệp đã có sẵn nên bỏ qua." : "")
            + (khongDocDuoc > 0 ? $" {khongDocDuoc} tệp không đọc được chữ (xem cột Trạng thái): học viên vẫn tải được nhưng không tìm thấy nội dung." : "");
        ThongBao(thongBao);
        if (loi.Count > 0) BaoLoi(string.Join(" ", loi));
        return RedirectToAction(nameof(Index), new { khoa = khoaId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(int id, string ten, int? baiId, bool hienChoHocVien)
    {
        var tl = await Db.TaiLieuKhoa.FirstOrDefaultAsync(t => t.Id == id && KhoaCuaToi().Any(k => k.Id == t.KhoaId));
        if (tl is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(ten)) tl.Ten = ten.Trim().Length > 200 ? ten.Trim()[..200] : ten.Trim();
        tl.BaiId = baiId is not null && await Db.Bai.AnyAsync(b => b.Id == baiId && b.ChuDe!.KhoaId == tl.KhoaId) ? baiId : null;
        tl.HienChoHocVien = hienChoHocVien;
        await Db.SaveChangesAsync();
        ThongBao($"Đã lưu \"{tl.Ten}\".");
        return RedirectToAction(nameof(Index), new { khoa = tl.KhoaId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id, CancellationToken ct)
    {
        var tl = await Db.TaiLieuKhoa.FirstOrDefaultAsync(t => t.Id == id && KhoaCuaToi().Any(k => k.Id == t.KhoaId), ct);
        if (tl is null) return NotFound();
        await kho.XoaAsync(tl, ct);
        ThongBao($"Đã xoá \"{tl.Ten}\" khỏi kho. Trợ lý AI không còn dùng tài liệu này.");
        return RedirectToAction(nameof(Index), new { khoa = tl.KhoaId });
    }

    public async Task<IActionResult> Mo(int id)
    {
        var tl = await Db.TaiLieuKhoa.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id && KhoaCuaToi().Any(k => k.Id == t.KhoaId));
        if (tl is null) return NotFound();
        return await kho.TraTepAsync(tl, HttpContext.RequestAborted);
    }

    // Xem phần chữ máy đã đọc được (để kiểm tra trước khi cho học viên / AI dùng)
    public async Task<IActionResult> Chu(int id)
    {
        var tl = await Db.TaiLieuKhoa.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id && KhoaCuaToi().Any(k => k.Id == t.KhoaId));
        return tl is null ? NotFound() : Content(tl.NoiDung.Length == 0 ? "(Không đọc được chữ trong tệp này.)" : tl.NoiDung, "text/plain; charset=utf-8");
    }
}
