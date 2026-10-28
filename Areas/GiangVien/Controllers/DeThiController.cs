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
                LuotLam = Db.BaiLam.Count(b => b.LaDeThi && b.MaDe == d.Id) + Db.LuotLamKiemTra.Count(b => b.BaiTapGiaoId == d.BaiTapGiaoId) }).ToListAsync();
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
        if (d is not null && await Db.BaiLam.AnyAsync(b => b.LaDeThi && b.MaDe == d.Id))
            return BadRequest("Đề đã có bài làm. Hãy tạo đề mới để giữ nguyên nội dung và kết quả.");
        if (d is null)
        {
            // Mã đề theo khoá: khoá thứ n dùng dải n*1000+1… (Lịch sử 1001…, Địa lí 2001…)
            var thuTu = await Db.Khoa.Where(k => k.Id == f.KhoaId).Select(k => k.ThuTu).FirstAsync();
            var coSo = (thuTu + 1) * 1000;
            var maMoi = (await Db.DeThi.Where(x => x.Id > coSo && x.Id < coSo + 1000).MaxAsync(x => (int?)x.Id) ?? coSo) + 1;
            d = new DeThi { Id = maMoi, KhoaId = f.KhoaId };
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
        if (await Db.BaiLam.AnyAsync(b => b.LaDeThi && b.MaDe == id) || (d.BaiTapGiaoId is int btId &&
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
