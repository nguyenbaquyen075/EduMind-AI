using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Areas.GiangVien.Controllers;

public class GiaoBaiController(AppDbContext db) : GiangVienControllerBase(db)
{
    public async Task<IActionResult> Tao(int? bai, bool kiemTra = false, string? khoa = null, int? deThi = null)
    {
        if (deThi is not null)
        {
            var d = await Db.DeThi.AsNoTracking().Include(x => x.Cau).ThenInclude(x => x.CauHoi)
                .FirstOrDefaultAsync(x => x.Id == deThi && (LaAdmin || x.Khoa!.GiangVienId == Uid));
            if (d is null) return NotFound();
            if (d.BaiTapGiaoId is int id) return RedirectToAction(nameof(Sua), new { id });
            await LuaChon(d.KhoaId);
            ViewBag.DaCoBaiNop = await Db.BaiLam.AnyAsync(x => x.LaDeThi && x.MaDe == d.Id);
            return View("Soan", new GiaoBaiForm { LaKiemTra = true, DeThiId = d.Id, Ten = d.Ten,
                SoPhut = d.SoPhut, DaGiao = d.DangMo, BaiId = d.Cau.FirstOrDefault()?.CauHoi?.BaiId ?? 0,
                Cau = d.Cau.OrderBy(x => x.ThuTu).Select(x => new CauBaiTap { Id = $"cu-{x.ThuTu}", Kieu = "TN",
                    NoiDung = x.CauHoi!.NoiDung, PhuongAn = x.CauHoi.PhuongAn.ToList(),
                    DapAn = "ABCD".IndexOf(x.CauHoi.DapAnDung, StringComparison.Ordinal), LoiGiai = x.CauHoi.GiaiThich }).ToList() });
        }
        if (khoa is not null && !await KhoaCuaToi().AnyAsync(k => k.Id == khoa)) return NotFound();
        if (bai is not null && !await BaiCuaToi().AnyAsync(b => b.Id == bai)) return NotFound();
        if (bai is not null && khoa is not null && !await BaiCuaToi().AnyAsync(b => b.Id == bai && b.ChuDe!.KhoaId == khoa)) return NotFound();
        await LuaChon(khoa);
        return View("Soan", new GiaoBaiForm { BaiId = bai ?? 0, LaKiemTra = kiemTra });
    }

    public async Task<IActionResult> Sua(int id)
    {
        var b = await CuaToi().AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (b is null) return NotFound();
        await LuaChon(b.DeThi?.KhoaId);
        ViewBag.DaCoBaiNop = await CoLuotLam(id);
        return View("Soan", new GiaoBaiForm { Id = b.Id, BaiId = b.BaiId, BuoiHocId = b.BuoiHocId,
            Ten = b.Ten, HuongDan = b.HuongDan, HanNop = b.HanNop, DaGiao = b.DaGiao,
            LaKiemTra = b.DeThi is not null, DeThiId = b.DeThi?.Id, SoPhut = b.DeThi?.SoPhut ?? 45,
            Cau = NoiDungBaiTap.Doc(b.NoiDungJson) });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> Luu([FromBody] GiaoBaiForm f)
    {
        if (!ModelState.IsValid) return BadRequest(new { loi = "Dữ liệu chưa đúng định dạng. Kiểm tra lại điểm và hạn nộp." });
        var khoaId = await BaiCuaToi().Where(b => b.Id == f.BaiId).Select(b => b.ChuDe!.KhoaId).FirstOrDefaultAsync();
        if (khoaId is null) return NotFound();
        var cu = f.Id == 0 ? null : await CuaToi().FirstOrDefaultAsync(b => b.Id == f.Id);
        if (f.Id != 0 && cu is null) return NotFound();
        if (cu is not null && await CoLuotLam(cu.Id))
            return BadRequest(new { loi = "Bài đã có học viên nộp hoặc bắt đầu làm. Hãy tạo bài mới để giữ nguyên nội dung và kết quả." });
        if (cu is not null && (cu.DeThi is not null) != f.LaKiemTra)
            return BadRequest(new { loi = "Không thể đổi bài tập thành kiểm tra hoặc ngược lại khi chỉnh sửa." });
        DeThi? de = cu?.DeThi;
        if (f.DeThiId is not null)
        {
            if (!f.LaKiemTra || (cu is not null && cu.DeThi?.Id != f.DeThiId)) return NotFound();
            de = await Db.DeThi.FirstOrDefaultAsync(d => d.Id == f.DeThiId && d.KhoaId == khoaId && (LaAdmin || d.Khoa!.GiangVienId == Uid));
            if (de is null || (de.BaiTapGiaoId is not null && de.BaiTapGiaoId != cu?.Id)) return NotFound();
            if (await Db.BaiLam.AnyAsync(b => b.LaDeThi && b.MaDe == de.Id))
                return BadRequest(new { loi = "Đề cũ đã có bài làm. Nội dung và kết quả được giữ nguyên; hãy tạo đề mới." });
        }
        if (de is not null && de.KhoaId != khoaId) return NotFound();
        if (string.IsNullOrWhiteSpace(f.Ten) || f.Ten.Length > 200) return BadRequest(new { loi = "Nhập tên bài tập, tối đa 200 ký tự." });
        if (f.HuongDan is null || f.HuongDan.Length > 10000) return BadRequest(new { loi = "Hướng dẫn tối đa 10.000 ký tự." });
        if (f.BuoiHocId is not null && !await Db.BuoiHoc.AnyAsync(b => b.Id == f.BuoiHocId && b.BaiId == f.BaiId))
            return BadRequest(new { loi = "Buổi học không thuộc bài đã chọn." });
        var loi = NoiDungBaiTap.KiemTra(f.Cau, f.DaGiao);
        if (loi is not null) return BadRequest(new { loi });
        if (f.LaKiemTra && f.SoPhut is < 5 or > 240) return BadRequest(new { loi = "Thời gian làm bài từ 5 đến 240 phút." });
        var bt = cu ?? new BaiTapGiao();
        if (cu is null) Db.BaiTapGiao.Add(bt);
        bt.BaiId = f.BaiId;
        bt.BuoiHocId = f.LaKiemTra ? null : f.BuoiHocId;
        bt.Ten = f.Ten.Trim();
        bt.HuongDan = f.HuongDan.Trim();
        bt.HanNop = f.HanNop?.UtcDateTime;
        bt.DaGiao = f.DaGiao;
        bt.NoiDungJson = NoiDungBaiTap.Viet(f.Cau);
        if (f.LaKiemTra)
        {
            if (de is null)
            {
                var thuTu = await Db.Khoa.Where(k => k.Id == khoaId).Select(k => k.ThuTu).FirstAsync();
                var coSo = (thuTu + 1) * 1000;
                var ma = (await Db.DeThi.Where(d => d.Id > coSo && d.Id < coSo + 1000).MaxAsync(d => (int?)d.Id) ?? coSo) + 1;
                if (ma >= coSo + 1000) return BadRequest(new { loi = "Khoá đã đạt giới hạn mã đề thi." });
                de = new DeThi { Id = ma, KhoaId = khoaId };
                Db.DeThi.Add(de);
            }
            de.Ten = bt.Ten; de.SoPhut = f.SoPhut; de.DangMo = f.DaGiao; de.BaiTapGiao = bt;
        }
        await Db.SaveChangesAsync();
        ThongBao(f.DaGiao ? (f.LaKiemTra ? "Đã xuất bản bài kiểm tra." : "Đã giao bài tập cho học viên của bài học.") : "Đã lưu nháp.");
        return Json(new { url = f.LaKiemTra ? Url.Action("Index", "DeThi") : Url.Action("Index", "CauHoi", new { bai = f.BaiId }), id = bt.Id });
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
        if (bt.DeThi is not null) return RedirectToAction("Index", "DeThi");
        if (await CoLuotLam(id))
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

    private async Task<bool> CoLuotLam(int id) => await Db.BaiNopGiao.AnyAsync(n => n.BaiTapGiaoId == id)
        || await Db.LuotLamKiemTra.AnyAsync(n => n.BaiTapGiaoId == id);
    private IQueryable<BaiTapGiao> CuaToi() => Db.BaiTapGiao.Include(b => b.DeThi).Where(b => LaAdmin || b.Bai!.ChuDe!.Khoa!.GiangVienId == Uid);
    private async Task LuaChon(string? khoa = null)
    {
        ViewBag.DsBai = await BaiCuaToi().AsNoTracking().Where(b => khoa == null || b.ChuDe!.KhoaId == khoa).OrderBy(b => b.ChuDe!.Khoa!.ThuTu).ThenBy(b => b.So)
            .Select(b => new { b.Id, Ten = $"{b.ChuDe!.Khoa!.Ten} · Bài {b.So}. {b.Ten}" }).ToListAsync();
        ViewBag.DsBuoi = await Db.BuoiHoc.AsNoTracking().Where(b => LaAdmin || b.Bai!.ChuDe!.Khoa!.GiangVienId == Uid)
            .OrderByDescending(b => b.ThoiGian).Select(b => new { b.Id, b.BaiId, b.ThoiGian }).ToListAsync();
    }
}
