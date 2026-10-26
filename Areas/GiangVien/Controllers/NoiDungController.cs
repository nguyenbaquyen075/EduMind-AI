using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Soạn nội dung: Khoá → Chủ đề → Bài → Mục → Phần (đoạn văn, mốc thời gian, ghi nhớ)
public class NoiDungController(AppDbContext db) : GiangVienControllerBase(db)
{
    // Cây chủ đề / bài của một khoá
    public async Task<IActionResult> Index(string? khoa)
    {
        var dsKhoa = await KhoaCuaToi().AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
        if (dsKhoa.Count == 0) return View(model: null);
        var chon = dsKhoa.FirstOrDefault(k => k.Id == khoa) ?? dsKhoa[0];
        ViewBag.DsKhoa = dsKhoa;
        var k = await Db.Khoa.AsNoTracking().Where(x => x.Id == chon.Id)
            .Include(x => x.ChuDe.OrderBy(c => c.ThuTu)).ThenInclude(c => c.Bai.OrderBy(b => b.ThuTu)).ThenInclude(b => b.Muc)
            .Include(x => x.ChuDe).ThenInclude(c => c.Bai).ThenInclude(b => b.CauHoi)
            .FirstAsync();
        return View(k);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemChuDe(string khoaId, string ten)
    {
        if (!await KhoaCuaToi().AnyAsync(k => k.Id == khoaId) || string.IsNullOrWhiteSpace(ten)) return BadRequest();
        var thuTu = await Db.ChuDe.Where(c => c.KhoaId == khoaId).CountAsync();
        Db.ChuDe.Add(new ChuDe { KhoaId = khoaId, Ten = $"Chủ đề {thuTu + 1} · {ten.Trim()}", ThuTu = thuTu });
        await Db.SaveChangesAsync();
        ThongBao("Đã thêm chủ đề.");
        return RedirectToAction(nameof(Index), new { khoa = khoaId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaChuDe(int id, string ten)
    {
        var cd = await Db.ChuDe.FirstOrDefaultAsync(c => c.Id == id && (LaAdmin || c.Khoa!.GiangVienId == Uid));
        if (cd is null || string.IsNullOrWhiteSpace(ten)) return BadRequest();
        cd.Ten = ten.Trim();
        await Db.SaveChangesAsync();
        ThongBao("Đã đổi tên chủ đề.");
        return RedirectToAction(nameof(Index), new { khoa = cd.KhoaId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemBai(int chuDeId, string ten)
    {
        var cd = await Db.ChuDe.Include(c => c.Bai).FirstOrDefaultAsync(c => c.Id == chuDeId && (LaAdmin || c.Khoa!.GiangVienId == Uid));
        if (cd is null || string.IsNullOrWhiteSpace(ten)) return BadRequest();
        // Mã bài dùng trên đường dẫn: lấy mã lớn nhất + 1; số bài nối tiếp trong khoá
        var maMoi = (await Db.Bai.Where(b => b.Id < 1000).MaxAsync(b => (int?)b.Id) ?? 0) + 1;
        var soMoi = (await Db.Bai.Where(b => b.ChuDe!.KhoaId == cd.KhoaId).MaxAsync(b => (int?)b.So) ?? 0) + 1;
        Db.Bai.Add(new Bai { Id = maMoi, ChuDeId = cd.Id, So = soMoi, Ten = ten.Trim(), ThuTu = cd.Bai.Count, DaXuatBan = false });
        await Db.SaveChangesAsync();
        ThongBao($"Đã tạo Bài {soMoi} (đang ở dạng nháp). Thêm mục để soạn nội dung.");
        return RedirectToAction(nameof(SoanBai), new { id = maMoi });
    }

    // Soạn một bài: thông tin bài + danh sách mục
    public async Task<IActionResult> SoanBai(int id)
    {
        var bai = await BaiCuaToi().AsNoTracking().Include(b => b.ChuDe!).ThenInclude(c => c.Khoa)
            .Include(b => b.Muc.OrderBy(m => m.So)).ThenInclude(m => m.Phan).FirstOrDefaultAsync(b => b.Id == id);
        if (bai is null) return NotFound();
        ViewBag.SoCau = await Db.CauHoi.CountAsync(c => c.BaiId == id);
        return View(bai);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuBai(int id, string ten, bool hocThu, bool daXuatBan)
    {
        var bai = await BaiCuaToi().FirstOrDefaultAsync(b => b.Id == id);
        if (bai is null || string.IsNullOrWhiteSpace(ten)) return BadRequest();
        bai.Ten = ten.Trim();
        bai.HocThu = hocThu;
        bai.DaXuatBan = daXuatBan;
        await Db.SaveChangesAsync();
        ThongBao(daXuatBan ? "Đã lưu. Bài đang hiện cho học viên." : "Đã lưu. Bài đang ẩn (nháp).");
        return RedirectToAction(nameof(SoanBai), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemMuc(int baiId, string ten)
    {
        var bai = await BaiCuaToi().Include(b => b.Muc).FirstOrDefaultAsync(b => b.Id == baiId);
        if (bai is null || string.IsNullOrWhiteSpace(ten)) return BadRequest();
        var muc = new Muc { So = bai.Muc.Count + 1, Ten = ten.Trim(), TieuDe = ten.Trim() };
        bai.Muc.Add(muc);
        bai.SoMuc = bai.Muc.Count;
        await Db.SaveChangesAsync();
        return RedirectToAction(nameof(SuaMuc), new { id = muc.Id });
    }

    public async Task<IActionResult> SuaMuc(int id)
    {
        var muc = await Db.Muc.AsNoTracking().Include(m => m.Bai!).ThenInclude(b => b.ChuDe!).ThenInclude(c => c.Khoa)
            .Include(m => m.Phan.OrderBy(p => p.ThuTu)).ThenInclude(p => p.Moc.OrderBy(x => x.ThuTu))
            .FirstOrDefaultAsync(m => m.Id == id && (LaAdmin || m.Bai!.ChuDe!.Khoa!.GiangVienId == Uid));
        if (muc is null) return NotFound();
        ViewBag.Bai = muc.Bai;
        return View(new MucForm
        {
            Id = muc.Id, BaiId = muc.BaiId, Ten = muc.Ten, TieuDe = muc.TieuDe,
            Phan = muc.Phan.Select(p => new PhanForm
            {
                TieuDe = p.TieuDe, LaGhiNho = p.LaGhiNho,
                DoanVan = string.Join("\n\n", p.DoanVan),
                Moc = string.Join("\n", p.Moc.Select(x => $"{x.Ten} | {x.ThoiGian} | {x.NoiDung}")),
            }).ToList(),
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaMuc(MucForm f)
    {
        var muc = await Db.Muc.Include(m => m.Phan).ThenInclude(p => p.Moc).Include(m => m.Bai)
            .FirstOrDefaultAsync(m => m.Id == f.Id && (LaAdmin || m.Bai!.ChuDe!.Khoa!.GiangVienId == Uid));
        if (muc is null) return NotFound();
        if (!ModelState.IsValid)
        {
            ViewBag.Bai = await Db.Bai.AsNoTracking().Include(b => b.ChuDe!).ThenInclude(c => c.Khoa).FirstAsync(b => b.Id == muc.BaiId);
            return View(f);
        }

        muc.Ten = f.Ten.Trim();
        muc.TieuDe = f.TieuDe.Trim();
        // Thay toàn bộ phần theo biểu mẫu (bỏ phần trống)
        Db.Phan.RemoveRange(muc.Phan);
        var daDung = new HashSet<string>();
        var thuTu = 0;
        foreach (var p in f.Phan.Where(p => !string.IsNullOrWhiteSpace(p.TieuDe) || !string.IsNullOrWhiteSpace(p.DoanVan)))
        {
            var tieuDe = string.IsNullOrWhiteSpace(p.TieuDe) ? "Nội dung" : p.TieuDe.Trim();
            var ma = Chuoi.TaoMaNeo(tieuDe);
            for (var i = 2; !daDung.Add(ma); i++) ma = $"{Chuoi.TaoMaNeo(tieuDe)}-{i}";
            muc.Phan.Add(new Phan
            {
                ThuTu = thuTu++, MaNeo = ma, TieuDe = tieuDe, LaGhiNho = p.LaGhiNho,
                DoanVan = (p.DoanVan ?? "").Replace("\r", "").Split("\n\n").Select(x => x.Trim()).Where(x => x.Length > 0).ToList(),
                Moc = (p.Moc ?? "").Replace("\r", "").Split('\n').Select(x => x.Split('|').Select(y => y.Trim()).ToArray())
                    .Where(x => x.Length >= 3 && x.Any(y => y.Length > 0))
                    .Select((x, i) => new MocThoiGian { ThuTu = i, Ten = x[0], ThoiGian = x[1], NoiDung = string.Join(" | ", x[2..]) }).ToList(),
            });
        }
        await Db.SaveChangesAsync();
        ThongBao("Đã lưu mục.");
        return RedirectToAction(nameof(SuaMuc), new { id = muc.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaMuc(int id)
    {
        var muc = await Db.Muc.Include(m => m.Bai!).ThenInclude(b => b.Muc)
            .FirstOrDefaultAsync(m => m.Id == id && (LaAdmin || m.Bai!.ChuDe!.Khoa!.GiangVienId == Uid));
        if (muc is null) return NotFound();
        var bai = muc.Bai!;
        Db.Muc.Remove(muc);
        var so = 1;
        foreach (var m in bai.Muc.Where(x => x.Id != id).OrderBy(x => x.So)) m.So = so++;
        bai.SoMuc = so - 1;
        await Db.SaveChangesAsync();
        ThongBao("Đã xoá mục.");
        return RedirectToAction(nameof(SoanBai), new { id = bai.Id });
    }
}
