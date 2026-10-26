using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Bài tập theo chủ đề / bài của khoá: mỗi câu gắn với bài (và mục lý thuyết) để học viên "Xem lại" sau khi nộp
public class CauHoiController(AppDbContext db) : GiangVienControllerBase(db)
{
    // Không chọn bài: cây Chủ đề → Bài của khoá. Chọn bài: bài tập đã giao, mỗi câu có menu Sửa / Xoá
    public async Task<IActionResult> Index(string? khoa, int? bai)
    {
        if (bai is not null)
        {
            var b = await BaiCuaToi().AsNoTracking().Include(x => x.ChuDe!).ThenInclude(x => x.Khoa).FirstOrDefaultAsync(x => x.Id == bai);
            if (b is null) return NotFound();
            ViewBag.Bai = b;
            ViewBag.BaiTapGiao = await Db.BaiTapGiao.AsNoTracking().Include(x => x.BuoiHoc).Where(x => x.BaiId == b.Id).OrderByDescending(x => x.NgayTao).ToListAsync();
            var cau = await Db.CauHoi.AsNoTracking().Where(c => c.BaiId == b.Id).OrderBy(c => c.ThuTu).ThenBy(c => c.Id)
                .Select(c => new { c.Id, c.NoiDung, c.PhuongAn, c.DapAnDung, c.GiaiThich, c.NguonHienThi,
                    Lam = Db.CauTraLoi.Count(t => t.CauHoiId == c.Id), Dung = Db.CauTraLoi.Count(t => t.CauHoiId == c.Id && t.Dung) })
                .ToListAsync();
            ViewBag.Cau = cau;
            return View("CuaBai");
        }

        var dsKhoa = await KhoaCuaToi().AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
        if (dsKhoa.Count == 0) return View(model: null);
        var chon = dsKhoa.FirstOrDefault(k => k.Id == khoa) ?? dsKhoa[0];
        ViewBag.DsKhoa = dsKhoa;
        ViewBag.SoBaiTapGiao = await Db.BaiTapGiao.AsNoTracking().Where(x => x.Bai!.ChuDe!.KhoaId == chon.Id)
            .GroupBy(x => x.BaiId).Select(g => new { BaiId = g.Key, So = g.Count() }).ToDictionaryAsync(x => x.BaiId, x => x.So);
        var kh = await Db.Khoa.AsNoTracking().Where(x => x.Id == chon.Id)
            .Include(x => x.ChuDe.OrderBy(c => c.ThuTu)).ThenInclude(c => c.Bai.OrderBy(b => b.ThuTu)).ThenInclude(b => b.CauHoi)
            .FirstAsync();
        return View(kh);
    }

    public async Task<IActionResult> Tao(int? bai, string? viTri)
    {
        var ds = await DanhSachViTri();
        var chon = viTri ?? bai?.ToString() ?? "";
        if (chon.Length > 0 && !ds.Any(x => x.Value == chon)) return NotFound();
        ViewBag.ViTri = ds;
        var baiId = int.TryParse(chon.Split(':')[0], out var id) ? id : 0;
        ViewBag.SoCau = await Db.CauHoi.CountAsync(x => x.BaiId == baiId);
        ViewBag.CoBai = ds.Count > 0;
        return View("Sua", new CauHoiForm { ViTri = chon });
    }

    public async Task<IActionResult> Sua(int id)
    {
        var c = await Db.CauHoi.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && (LaAdmin || x.Bai!.ChuDe!.Khoa!.GiangVienId == Uid));
        if (c is null) return NotFound();
        ViewBag.ViTri = await DanhSachViTri();
        ViewBag.CoBai = true;
        ViewBag.SoCau = c.ThuTu;
        var pa = c.PhuongAn.Concat(Enumerable.Repeat("", 4)).ToList();
        return View(new CauHoiForm
        {
            Id = c.Id, ViTri = c.Muc is null ? $"{c.BaiId}" : $"{c.BaiId}:{c.Muc}", NoiDung = c.NoiDung,
            A = pa[0], B = pa[1], C = pa[2], D = pa[3], DapAn = c.DapAnDung, GiaiThich = c.GiaiThich,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(CauHoiForm f)
    {
        var phan = (f.ViTri ?? "").Split(':');
        var baiId = int.TryParse(phan[0], out var b) ? b : 0;
        int? mucSo = phan.Length > 1 && int.TryParse(phan[1], out var m) ? m : null;
        var bai = await BaiCuaToi().Include(x => x.Muc).Include(x => x.ChuDe!).ThenInclude(x => x.Khoa).FirstOrDefaultAsync(x => x.Id == baiId);
        if (bai is null) ModelState.AddModelError(nameof(f.ViTri), "Chọn bài");
        else if (phan.Length > 2 || (phan.Length == 2 && (mucSo is null || !bai.Muc.Any(x => x.So == mucSo))))
            ModelState.AddModelError(nameof(f.ViTri), "Mục đã chọn không còn tồn tại. Vui lòng chọn lại.");
        f.ViTri ??= "";
        if (!ModelState.IsValid)
        {
            var ds = await DanhSachViTri();
            ViewBag.ViTri = ds;
            ViewBag.CoBai = ds.Count > 0;
            ViewBag.SoCau = await Db.CauHoi.CountAsync(x => x.BaiId == baiId);
            return View("Sua", f);
        }

        var muc = mucSo is null ? null : bai!.Muc.FirstOrDefault(x => x.So == mucSo);
        var c = f.Id == 0 ? null : await Db.CauHoi.FirstOrDefaultAsync(x => x.Id == f.Id && (LaAdmin || x.Bai!.ChuDe!.Khoa!.GiangVienId == Uid));
        if (f.Id != 0 && c is null) return NotFound();
        if (c is null)
        {
            c = new CauHoi { ThuTu = (await Db.CauHoi.Where(x => x.BaiId == baiId).MaxAsync(x => (int?)x.ThuTu) ?? -1) + 1 };
            Db.CauHoi.Add(c);
        }
        else if (c.BaiId != baiId)
            c.ThuTu = (await Db.CauHoi.Where(x => x.BaiId == baiId).MaxAsync(x => (int?)x.ThuTu) ?? -1) + 1;
        c.BaiId = baiId;
        c.Muc = muc?.So;
        c.NoiDung = f.NoiDung.Trim();
        c.PhuongAn = [f.A.Trim(), f.B.Trim(), f.C.Trim(), f.D.Trim()];
        c.DapAnDung = f.DapAn;
        c.GiaiThich = f.GiaiThich.Trim();
        c.NguonHienThi = muc is null ? $"Bài {bai!.So} — {bai.Ten}" : $"Bài {bai!.So} · Mục {muc.So} — {muc.Ten}";
        await Db.SaveChangesAsync();
        ThongBao(f.Id == 0 ? "Đã thêm câu hỏi." : "Đã lưu câu hỏi.");
        return Request.Form["tiep"] == "1"
            ? RedirectToAction(nameof(Tao), new { viTri = f.ViTri })
            : RedirectToAction(nameof(Index), new { bai = baiId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var c = await Db.CauHoi.FirstOrDefaultAsync(x => x.Id == id && (LaAdmin || x.Bai!.ChuDe!.Khoa!.GiangVienId == Uid));
        if (c is null) return NotFound();
        Db.CauHoi.Remove(c);
        await Db.SaveChangesAsync();
        ThongBao("Đã xoá câu hỏi (và gỡ khỏi các đề thi có câu này).");
        return RedirectToAction(nameof(Index), new { bai = c.BaiId });
    }

    // Chọn vị trí câu hỏi: cả bài, hoặc một mục cụ thể của bài
    private async Task<List<SelectListItem>> DanhSachViTri()
    {
        var bai = await BaiCuaToi().AsNoTracking().OrderBy(b => b.ChuDe!.Khoa!.ThuTu).ThenBy(b => b.So)
            .Select(b => new { b.Id, b.So, b.Ten, Khoa = b.ChuDe!.Khoa!.Ten, Muc = b.Muc.OrderBy(m => m.So).Select(m => new { m.So, m.Ten }).ToList() })
            .ToListAsync();
        var ds = new List<SelectListItem>();
        foreach (var b in bai)
        {
            var nhom = new SelectListGroup { Name = $"{b.Khoa} · Bài {b.So}. {b.Ten}" };
            ds.Add(new SelectListItem($"Cả bài {b.So}", b.Id.ToString()) { Group = nhom });
            ds.AddRange(b.Muc.Select(m => new SelectListItem($"Bài {b.So} · Mục {m.So} — {m.Ten}", $"{b.Id}:{m.So}") { Group = nhom }));
        }
        return ds;
    }
}
