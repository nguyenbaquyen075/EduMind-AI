using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Đề thi thử: chọn câu từ ngân hàng, đặt thời gian, mở / đóng đề
public class DeThiController(AppDbContext db) : GiangVienControllerBase(db)
{
    public async Task<IActionResult> Index()
    {
        var khoaIds = await KhoaCuaToi().Select(k => k.Id).ToListAsync();
        var rows = await Db.DeThi.AsNoTracking().Where(d => khoaIds.Contains(d.KhoaId)).OrderBy(d => d.KhoaId).ThenBy(d => d.Id)
            .Select(d => new { d.Id, d.Ten, d.SoPhut, d.DangMo, Khoa = d.Khoa!.Ten, SoCau = d.Cau.Count,
                d.BaiTapGiaoId, NoiDung = d.BaiTapGiao == null ? null : d.BaiTapGiao.NoiDungJson,
                LuotLam = Db.BaiLam.Count(b => b.DeThiId == d.Id) + Db.LuotLamKiemTra.Count(b => b.BaiTapGiaoId == d.BaiTapGiaoId) }).ToListAsync();
        ViewBag.De = rows.Select(d => new { d.Id, d.Ten, d.SoPhut, d.DangMo, d.Khoa,
            SoCau = d.NoiDung is string text ? NoiDungBaiTap.Doc(text).Count : d.SoCau,
            d.BaiTapGiaoId, d.LuotLam }).ToList();
        ViewBag.DsKhoa = await KhoaCuaToi().AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
        return View();
    }

    public async Task<IActionResult> Tao(string khoa)
    {
        if (!await KhoaCuaToi().AnyAsync(k => k.Id == khoa)) return NotFound();
        return RedirectToAction("Tao", "GiaoBai", new { khoa, kiemTra = true });
    }

    // ------------------------------------------------------------------ Ra đề theo ma trận
    // Giảng viên nhập số câu dễ / trung bình / khó cho từng bài; hệ thống bốc ngẫu nhiên từ ngân hàng câu hỏi của khoá, tạo đề nháp (đang đóng)
    private async Task<List<MaTranBai>> BaiMaTran(string khoaId) =>
        (await Db.Bai.AsNoTracking().Where(b => b.ChuDe!.KhoaId == khoaId).OrderBy(b => b.ChuDe!.ThuTu).ThenBy(b => b.So)
            .Select(b => new { b.Id, b.So, b.Ten, De = b.CauHoi.Count(c => c.DoKho == 1), TB = b.CauHoi.Count(c => c.DoKho == 2), Kho = b.CauHoi.Count(c => c.DoKho == 3) })
            .ToListAsync()).Where(b => b.De + b.TB + b.Kho > 0).Select(b => new MaTranBai(b.Id, b.So, b.Ten, b.De, b.TB, b.Kho)).ToList();

    public async Task<IActionResult> MaTran(string khoa)
    {
        var k = await KhoaCuaToi().AsNoTracking().FirstOrDefaultAsync(x => x.Id == khoa);
        if (k is null) return NotFound();
        return View(new MaTranViewModel { Khoa = k, Bai = await BaiMaTran(khoa), Form = new MaTranForm { KhoaId = khoa, Ten = $"Đề ôn tập {k.Ten} {DateTime.Now:dd/MM/yyyy}" } });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMaTran(MaTranForm f)
    {
        var k = await KhoaCuaToi().AsNoTracking().FirstOrDefaultAsync(x => x.Id == f.KhoaId);
        if (k is null) return NotFound();
        var bai = await BaiMaTran(f.KhoaId);
        var dong = (f.Dong ?? []).Where(d => d.Tong > 0).ToList();
        foreach (var d in dong)
        {
            var b = bai.FirstOrDefault(x => x.BaiId == d.BaiId);
            if (b is null || d.De < 0 || d.TB < 0 || d.Kho < 0) { ModelState.AddModelError("", "Có dòng không hợp lệ."); break; }
            if (d.De > b.SoDe || d.TB > b.SoTB || d.Kho > b.SoKho)
                ModelState.AddModelError("", $"Bài {b.So}: ngân hàng chỉ có {b.SoDe} câu dễ, {b.SoTB} trung bình, {b.SoKho} khó.");
        }
        if (dong.Count == 0 || dong.Sum(d => d.Tong) == 0) ModelState.AddModelError("", "Nhập số câu cho ít nhất một bài.");
        else if (dong.Sum(d => d.Tong) > 100) ModelState.AddModelError("", "Một đề tối đa 100 câu.");
        if (!ModelState.IsValid) return View("MaTran", new MaTranViewModel { Khoa = k, Bai = bai, Form = f });

        var rd = Random.Shared;
        var chon = new List<int>();
        foreach (var d in dong)
        {
            var cau = await Db.CauHoi.AsNoTracking().Where(c => c.BaiId == d.BaiId).Select(c => new { c.Id, c.DoKho }).ToListAsync();
            foreach (var (doKho, n) in new[] { (1, d.De), (2, d.TB), (3, d.Kho) })
                chon.AddRange(cau.Where(c => c.DoKho == doKho).OrderBy(_ => rd.Next()).Take(n).Select(c => c.Id));
        }
        // Thứ tự trong đề: theo bài rồi theo thứ tự câu (giống đề chọn tay)
        var thuTu = await Db.CauHoi.AsNoTracking().Where(c => chon.Contains(c.Id)).OrderBy(c => c.Bai!.So).ThenBy(c => c.ThuTu).Select(c => c.Id).ToListAsync();
        var de = new DeThi { KhoaId = f.KhoaId, Ten = f.Ten.Trim(), SoPhut = f.SoPhut, DangMo = false };
        for (var i = 0; i < thuTu.Count; i++) de.Cau.Add(new DeThiCau { CauHoiId = thuTu[i], ThuTu = i });
        Db.DeThi.Add(de);
        await Db.SaveChangesAsync();
        ThongBao($"Đã tạo đề nháp {thuTu.Count} câu (đang đóng). Mở \"Sửa\" để xem lại, thay câu và bật đề khi sẵn sàng.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Sua(int id)
    {
        var d = await Db.DeThi.AsNoTracking().Include(x => x.Cau).FirstOrDefaultAsync(x => x.Id == id);
        if (d is null || !await KhoaCuaToi().AnyAsync(k => k.Id == d.KhoaId)) return NotFound();
        return d.BaiTapGiaoId is int bt ? RedirectToAction("Sua", "GiaoBai", new { id = bt })
            : RedirectToAction("Tao", "GiaoBai", new { deThi = d.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(DeThiForm f)
    {
        if (!await KhoaCuaToi().AnyAsync(k => k.Id == f.KhoaId)) return NotFound();
        // Chỉ nhận câu thuộc khoá này
        var hopLe = await Db.CauHoi.Where(c => c.Bai!.ChuDe!.KhoaId == f.KhoaId && f.CauHoiIds.Contains(c.Id))
            .OrderBy(c => c.Bai!.So).ThenBy(c => c.ThuTu).Select(c => c.Id).ToListAsync();
        if (hopLe.Count == 0) ModelState.AddModelError(nameof(f.CauHoiIds), "Chọn ít nhất một câu hỏi");
        if (!ModelState.IsValid)
        {
            await NapCauHoi(f.KhoaId);
            return View("Sua", f);
        }

        var d = f.Id == 0 ? null : await Db.DeThi.Include(x => x.Cau).FirstOrDefaultAsync(x => x.Id == f.Id && x.KhoaId == f.KhoaId);
        if (f.Id != 0 && d is null) return NotFound();
        if (d?.BaiTapGiaoId is not null) return BadRequest("Đề này dùng form soạn bài chung. Hãy mở lại từ danh sách kiểm tra.");
        if (d is not null && await Db.BaiLam.AnyAsync(b => b.DeThiId == d.Id))
            return BadRequest("Đề đã có bài làm. Hãy tạo đề mới để giữ nguyên nội dung và kết quả.");
        if (d is null)
        {
            // Mã đề do CSDL tự tăng
            d = new DeThi { KhoaId = f.KhoaId };
            Db.DeThi.Add(d);
        }
        d.Ten = f.Ten.Trim();
        d.SoPhut = f.SoPhut;
        d.DangMo = f.DangMo;
        Db.DeThiCau.RemoveRange(d.Cau);
        d.Cau = hopLe.Select((id, i) => new DeThiCau { CauHoiId = id, ThuTu = i }).ToList();
        await Db.SaveChangesAsync();
        ThongBao($"Đã lưu \"{d.Ten}\" ({hopLe.Count} câu, {d.SoPhut} phút){(d.DangMo ? "" : " — đang đóng")}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var d = await Db.DeThi.FirstOrDefaultAsync(x => x.Id == id);
        if (d is null || !await KhoaCuaToi().AnyAsync(k => k.Id == d.KhoaId)) return NotFound();
        if (await Db.BaiLam.AnyAsync(b => b.DeThiId == id) || (d.BaiTapGiaoId is int btId &&
            (await Db.BaiNopGiao.AnyAsync(n => n.BaiTapGiaoId == btId) || await Db.LuotLamKiemTra.AnyAsync(n => n.BaiTapGiaoId == btId))))
        {
            BaoLoi("Đề đã có học viên làm. Giữ lại nội dung và kết quả; anh có thể tạo đề mới.");
            return RedirectToAction(nameof(Index));
        }
        if (d.BaiTapGiaoId is int baiTapId)
        {
            var baiTap = await Db.BaiTapGiao.FindAsync(baiTapId);
            if (baiTap is not null) Db.BaiTapGiao.Remove(baiTap);
        }
        Db.DeThi.Remove(d);
        await Db.SaveChangesAsync();
        ThongBao("Đã xoá đề.");
        return RedirectToAction(nameof(Index));
    }

    private async Task NapCauHoi(string khoaId)
    {
        ViewBag.TenKhoa = await Db.Khoa.Where(k => k.Id == khoaId).Select(k => k.Ten).FirstAsync();
        ViewBag.CauHoi = await Db.CauHoi.AsNoTracking().Where(c => c.Bai!.ChuDe!.KhoaId == khoaId)
            .OrderBy(c => c.Bai!.So).ThenBy(c => c.ThuTu)
            .Select(c => new { c.Id, c.NoiDung, BaiSo = c.Bai!.So, BaiTen = c.Bai.Ten }).ToListAsync();
    }
}
