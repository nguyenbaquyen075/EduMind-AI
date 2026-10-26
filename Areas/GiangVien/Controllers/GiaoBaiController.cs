using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.GiangVien.Controllers;

public class GiaoBaiController(AppDbContext db) : GiangVienControllerBase(db)
{
    public async Task<IActionResult> Tao(int? bai)
    {
        if (bai is not null && !await BaiCuaToi().AnyAsync(b => b.Id == bai)) return NotFound();
        await LuaChon();
        return View("Soan", new GiaoBaiForm { BaiId = bai ?? 0 });
    }

    public async Task<IActionResult> Sua(int id)
    {
        var b = await CuaToi().AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (b is null) return NotFound();
        await LuaChon();
        ViewBag.DaCoBaiNop = await Db.BaiNopGiao.AnyAsync(n => n.BaiTapGiaoId == id);
        return View("Soan", new GiaoBaiForm { Id = b.Id, BaiId = b.BaiId, BuoiHocId = b.BuoiHocId,
            Ten = b.Ten, HuongDan = b.HuongDan, HanNop = b.HanNop, DaGiao = b.DaGiao, Cau = NoiDungBaiTap.Doc(b.NoiDungJson) });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> Luu([FromBody] GiaoBaiForm f)
    {
        if (!ModelState.IsValid) return BadRequest(new { loi = "Dữ liệu chưa đúng định dạng. Kiểm tra lại điểm và hạn nộp." });
        if (!await BaiCuaToi().AnyAsync(b => b.Id == f.BaiId)) return NotFound();
        var cu = f.Id == 0 ? null : await CuaToi().FirstOrDefaultAsync(b => b.Id == f.Id);
        if (f.Id != 0 && cu is null) return NotFound();
        if (cu is not null && await Db.BaiNopGiao.AnyAsync(n => n.BaiTapGiaoId == cu.Id))
            return BadRequest(new { loi = "Bài tập đã có học viên nộp. Hãy tạo bài tập mới để giữ nguyên nội dung và điểm bài đã nộp." });
        if (string.IsNullOrWhiteSpace(f.Ten) || f.Ten.Length > 200) return BadRequest(new { loi = "Nhập tên bài tập, tối đa 200 ký tự." });
        if (f.HuongDan is null || f.HuongDan.Length > 10000) return BadRequest(new { loi = "Hướng dẫn tối đa 10.000 ký tự." });
        if (f.BuoiHocId is not null && !await Db.BuoiHoc.AnyAsync(b => b.Id == f.BuoiHocId && b.BaiId == f.BaiId))
            return BadRequest(new { loi = "Buổi học không thuộc bài đã chọn." });
        var loi = NoiDungBaiTap.KiemTra(f.Cau, f.DaGiao);
        if (loi is not null) return BadRequest(new { loi });
        var bt = cu ?? new BaiTapGiao();
        if (cu is null) Db.BaiTapGiao.Add(bt);
        bt.BaiId = f.BaiId;
        bt.BuoiHocId = f.BuoiHocId;
        bt.Ten = f.Ten.Trim();
        bt.HuongDan = f.HuongDan.Trim();
        bt.HanNop = f.HanNop?.UtcDateTime;
        bt.DaGiao = f.DaGiao;
        bt.NoiDungJson = NoiDungBaiTap.Viet(f.Cau);
        await Db.SaveChangesAsync();
        ThongBao(f.DaGiao ? "Đã giao bài tập cho học viên của bài học." : "Đã lưu nháp bài tập.");
        return Json(new { url = Url.Action("Index", "CauHoi", new { bai = f.BaiId }), id = bt.Id });
    }

    public async Task<IActionResult> BaiNop(int id)
    {
        var bt = await CuaToi().AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (bt is null) return NotFound();
        ViewBag.BaiTap = bt;
        return View(await Db.BaiNopGiao.AsNoTracking().Include(n => n.NguoiDung).Where(n => n.BaiTapGiaoId == id).OrderByDescending(n => n.NopLuc).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var bt = await CuaToi().FirstOrDefaultAsync(b => b.Id == id);
        if (bt is null) return NotFound();
        if (await Db.BaiNopGiao.AnyAsync(n => n.BaiTapGiaoId == id))
            BaoLoi("Bài tập đã có bài nộp, cần giữ lại nội dung và kết quả học viên.");
        else
        {
            Db.BaiTapGiao.Remove(bt);
            await Db.SaveChangesAsync();
            ThongBao("Đã xoá bài tập.");
        }
        return RedirectToAction("Index", "CauHoi", new { bai = bt.BaiId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChamTuLuan(int id, string diem)
    {
        var nop = await Db.BaiNopGiao.Include(n => n.BaiTapGiao).FirstOrDefaultAsync(n => n.Id == id);
        if (nop is null || !await CuaToi().AnyAsync(b => b.Id == nop.BaiTapGiaoId)) return NotFound();
        var toiDa = NoiDungBaiTap.Doc(nop.BaiTapGiao!.NoiDungJson).Where(c => c.Kieu == "TL").Sum(c => c.Diem);
        if (!decimal.TryParse(diem?.Replace(',', '.'), System.Globalization.NumberStyles.AllowDecimalPoint,
            System.Globalization.CultureInfo.InvariantCulture, out var so) || so < 0 || so > toiDa)
        {
            BaoLoi($"Điểm tự luận từ 0 đến {toiDa}.");
            return RedirectToAction(nameof(BaiNop), new { id = nop.BaiTapGiaoId });
        }
        nop.DiemTuLuan = so;
        await Db.SaveChangesAsync();
        ThongBao("Đã lưu điểm tự luận.");
        return RedirectToAction(nameof(BaiNop), new { id = nop.BaiTapGiaoId });
    }

    private IQueryable<BaiTapGiao> CuaToi() => Db.BaiTapGiao.Where(b => LaAdmin || b.Bai!.ChuDe!.Khoa!.GiangVienId == Uid);
    private async Task LuaChon()
    {
        ViewBag.DsBai = await BaiCuaToi().AsNoTracking().OrderBy(b => b.ChuDe!.Khoa!.ThuTu).ThenBy(b => b.So)
            .Select(b => new { b.Id, Ten = $"{b.ChuDe!.Khoa!.Ten} · Bài {b.So}. {b.Ten}" }).ToListAsync();
        ViewBag.DsBuoi = await Db.BuoiHoc.AsNoTracking().Where(b => LaAdmin || b.Bai!.ChuDe!.Khoa!.GiangVienId == Uid)
            .OrderByDescending(b => b.ThoiGian).Select(b => new { b.Id, b.BaiId, b.ThoiGian }).ToListAsync();
    }
}
