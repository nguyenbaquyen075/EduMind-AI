using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Lớp học: giảng viên tạo lớp cho khoá mình phụ trách, chia sẻ mã để học viên vào; nhóm chat của lớp mở ở /Lop/{id}
public class LopController(AppDbContext db, LopHocService lopSvc) : GiangVienControllerBase(db)
{
    private IQueryable<Lop> LopCuaToi() => Db.Lop.Where(l => LaAdmin || l.Khoa!.GiangVienId == Uid);

    public async Task<IActionResult> Index()
    {
        ViewBag.Lop = await LopCuaToi().AsNoTracking().OrderBy(l => l.Khoa!.ThuTu).ThenBy(l => l.Ten)
            .Select(l => new LopTomTat(l.Id, l.Ten, l.Khoa!.Ten, l.MaThamGia, l.DangMo, l.HocVien.Count, l.TinNhan.LongCount(), true)).ToListAsync();
        return View();
    }

    public async Task<IActionResult> Tao(string? khoa)
    {
        ViewBag.DsKhoa = await DanhSachKhoa();
        return View("Sua", new LopForm { KhoaId = khoa ?? "" });
    }

    public async Task<IActionResult> Sua(int id)
    {
        var l = await LopCuaToi().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();
        ViewBag.DsKhoa = await DanhSachKhoa();
        return View(new LopForm { Id = l.Id, KhoaId = l.KhoaId, Ten = l.Ten, MoTa = l.MoTa });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(LopForm f, CancellationToken ct)
    {
        f.Ten = (f.Ten ?? "").Trim();
        if (!await KhoaCuaToi().AnyAsync(k => k.Id == f.KhoaId, ct)) ModelState.AddModelError(nameof(f.KhoaId), "Chọn khoá của bạn");
        if (!ModelState.IsValid) { ViewBag.DsKhoa = await DanhSachKhoa(); return View("Sua", f); }

        Lop? l;
        if (f.Id == 0)
        {
            l = new Lop { KhoaId = f.KhoaId, MaThamGia = await lopSvc.TaoMaAsync(ct) };
            Db.Lop.Add(l);
        }
        else
        {
            l = await LopCuaToi().FirstOrDefaultAsync(x => x.Id == f.Id, ct);
            if (l is null) return NotFound();
            if (l.KhoaId != f.KhoaId && await Db.LopHocVien.AnyAsync(m => m.LopId == l.Id, ct))
            {
                ModelState.AddModelError(nameof(f.KhoaId), "Lớp đã có học viên nên không đổi sang khoá khác được.");
                ViewBag.DsKhoa = await DanhSachKhoa();
                return View("Sua", f);
            }
            l.KhoaId = f.KhoaId;
        }
        l.Ten = f.Ten;
        l.MoTa = (f.MoTa ?? "").Trim();
        await Db.SaveChangesAsync(ct);
        ThongBao(f.Id == 0 ? $"Đã tạo lớp. Gửi mã {l.MaThamGia} cho học viên để vào lớp." : "Đã lưu lớp.");
        return RedirectToAction(nameof(ChiTiet), new { id = l.Id });
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var l = await LopCuaToi().AsNoTracking().Include(x => x.Khoa).FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();
        ViewBag.Lop = l;
        ViewBag.HocVien = await Db.LopHocVien.AsNoTracking().Where(m => m.LopId == id).OrderBy(m => m.NguoiDung!.HoTen)
            .Select(m => new { m.NguoiDungId, m.NguoiDung!.HoTen, m.NguoiDung.Email, m.NgayVao }).ToListAsync();
        ViewBag.SoTinNhan = await Db.TinNhanLop.LongCountAsync(t => t.LopId == id);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMa(int id, CancellationToken ct)
    {
        var l = await LopCuaToi().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l is null) return NotFound();
        l.MaThamGia = await lopSvc.TaoMaAsync(ct);
        await Db.SaveChangesAsync(ct);
        ThongBao($"Đã đổi mã lớp thành {l.MaThamGia}. Mã cũ không dùng được nữa; học viên đã vào lớp không bị ảnh hưởng.");
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoDong(int id, CancellationToken ct)
    {
        var l = await LopCuaToi().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l is null) return NotFound();
        l.DangMo = !l.DangMo;
        await Db.SaveChangesAsync(ct);
        ThongBao(l.DangMo ? "Lớp đang nhận học viên mới." : "Đã ngừng nhận học viên mới (học viên đang trong lớp vẫn học bình thường).");
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoiRa(int id, string nguoiDungId, CancellationToken ct)
    {
        if (!await LopCuaToi().AnyAsync(x => x.Id == id, ct)) return NotFound();
        var n = await Db.LopHocVien.Where(m => m.LopId == id && m.NguoiDungId == nguoiDungId).ExecuteDeleteAsync(ct);
        if (n > 0) ThongBao("Đã mời học viên ra khỏi lớp. Tin nhắn họ đã gửi vẫn giữ nguyên.");
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id, CancellationToken ct)
    {
        var l = await LopCuaToi().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l is null) return NotFound();
        Db.Lop.Remove(l);                       // thành viên, bài đăng, trả lời xoá theo (CASCADE)
        await Db.SaveChangesAsync(ct);
        ThongBao("Đã xoá lớp cùng toàn bộ tin nhắn của lớp.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> DanhSachKhoa() =>
        await KhoaCuaToi().AsNoTracking().OrderBy(k => k.ThuTu).Select(k => new SelectListItem(k.Ten, k.Id)).ToListAsync();
}
