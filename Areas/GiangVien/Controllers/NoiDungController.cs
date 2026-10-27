using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Soạn nội dung: Khoá → Chủ đề → Bài → Mục → Phần (đoạn văn, mốc thời gian, ghi nhớ)
public class NoiDungController(AppDbContext db, ISoanNoiDungAiService ai, KhoBanNhapNoiDung banNhap) : GiangVienControllerBase(db)
{
    private void PhucHoiNhap(string? nhap)
    {
        var values = banNhap.Doc<Dictionary<string, string>>(Uid, nhap);
        if (values is null) return;
        foreach (var (key, value) in values) ModelState.SetModelValue(key, value, value);
    }

    private string GiuNoiDungNhap()
    {
        var values = new Dictionary<string, string>();
        foreach (var key in new[] { "tenChuDe", "tenBai", "soBai", "noiDung" })
            if (ModelState.TryGetValue(key, out var value)) values[key] = value.AttemptedValue ?? "";
        return banNhap.Luu(Uid, values);
    }

    private IActionResult MoXemThu(int baiId, IReadOnlyList<NoiDungAiMuc> muc, bool theoVanBan)
    {
        var form = new BanNhapNoiDungAiForm { BaiId = baiId, Muc = muc.Select(m => new MucForm {
            Ten = m.Ten, TieuDe = m.TieuDe, Phan = m.Phan.Select(p => new PhanForm {
                TieuDe = p.TieuDe, DoanVan = p.DoanVan, LaGhiNho = p.LaGhiNho }).ToList()
        }).ToList() };
        var ma = banNhap.Luu(Uid, new BanXemThuNoiDung(form, theoVanBan));
        return RedirectToAction(nameof(XemThu), new { nhap = ma });
    }

    [HttpGet]
    public async Task<IActionResult> XemThu(string? nhap)
    {
        var draft = banNhap.Doc<BanXemThuNoiDung>(Uid, nhap);
        if (draft is null)
        {
            BaoLoi("Bản xem thử đã hết phiên hoặc web vừa khởi động lại. Anh mở bài để tiếp tục soạn nhé.");
            return RedirectToAction(nameof(Index));
        }
        var bai = await BaiCuaToi().AsNoTracking().Include(b => b.ChuDe!).ThenInclude(c => c.Khoa)
            .FirstOrDefaultAsync(b => b.Id == draft.Form.BaiId);
        if (bai is null) return RedirectToAction(nameof(Index));
        ViewBag.Bai = bai;
        ViewBag.ChiaTheoVanBan = draft.TheoVanBan;
        return View("XemThuAi", draft.Form);
    }

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

    public async Task<IActionResult> TaoChuDe(string? khoa, string? nhap)
    {
        var dsKhoa = await KhoaCuaToi().AsNoTracking().OrderBy(k => k.ThuTu).ToListAsync();
        if (dsKhoa.Count == 0) return NotFound();
        var chon = dsKhoa.FirstOrDefault(k => k.Id == khoa) ?? dsKhoa[0];
        ViewBag.DsKhoa = dsKhoa;
        PhucHoiNhap(nhap);
        return View(chon);
    }

    [HttpGet]
    public async Task<IActionResult> TaoBai(int chuDeId, string? nhap)
    {
        var chuDe = await Db.ChuDe.AsNoTracking().Include(c => c.Khoa)
            .FirstOrDefaultAsync(c => c.Id == chuDeId && (LaAdmin || c.Khoa!.GiangVienId == Uid));
        if (chuDe is null) return NotFound();
        ViewBag.ChuDe = chuDe;
        PhucHoiNhap(nhap);
        return View("TaoChuDe", chuDe.Khoa!);
    }

    // Các URL gửi form cũ vẫn mở được khi người dùng quay lại lịch sử hoặc mở dấu trang.
    [HttpGet, ActionName(nameof(TaoChuDeBangAi))]
    public IActionResult MoLaiTrangTao(int? chuDeId, string? khoaId) => chuDeId.HasValue
        ? RedirectToAction(nameof(TaoBai), new { chuDeId })
        : RedirectToAction(nameof(TaoChuDe), new { khoa = khoaId });

    [HttpGet, ActionName(nameof(TaoNoiDungBangAi))]
    public IActionResult MoLaiTrangSoan(int? baiId) => baiId.HasValue
        ? RedirectToAction(nameof(SoanBai), new { id = baiId })
        : RedirectToAction(nameof(Index));

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

    /// <summary>Tạo chủ đề + bài nháp từ màn ngoài, sau đó mở bản AI để giảng viên duyệt.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoChuDeBangAi(string khoaId, string? tenChuDe, string tenBai, string noiDung, int? soBai = null, bool chiaTheoVanBan = false, int? chuDeId = null, CancellationToken ct = default)
    {
        ChuDe? cd = null;
        if (chuDeId.HasValue)
        {
            cd = await Db.ChuDe.Include(c => c.Khoa)
                .FirstOrDefaultAsync(c => c.Id == chuDeId && (LaAdmin || c.Khoa!.GiangVienId == Uid), ct);
            if (cd is null) return NotFound();
            khoaId = cd.KhoaId;
        }
        noiDung = (noiDung ?? "").Trim();
        var khoa = await KhoaCuaToi().FirstOrDefaultAsync(k => k.Id == khoaId, ct);
        if (khoa is null) return NotFound();
        var thieuTen = (cd is null && string.IsNullOrWhiteSpace(tenChuDe)) || string.IsNullOrWhiteSpace(tenBai);
        if (thieuTen || soBai is null or < 1 || noiDung.Length is < 30 or > 30000)
        {
            BaoLoi(thieuTen ? "Anh nhập đầy đủ tên chủ đề và tên bài học nhé." : soBai is null or < 1
                ? "Anh nhập số bài là số nguyên từ 1 trở lên." : "Nội dung cần từ 30 đến 30.000 ký tự.");
            var nhap = GiuNoiDungNhap();
            return cd is null ? RedirectToAction(nameof(TaoChuDe), new { khoa = khoaId, nhap })
                : RedirectToAction(nameof(TaoBai), new { chuDeId = cd.Id, nhap });
        }
        IReadOnlyList<NoiDungAiMuc> banNhap;
        try
        {
            banNhap = chiaTheoVanBan ? ChiaDeMucVanBan.Chia(tenBai.Trim(), noiDung) : await ai.ChiaNoiDungAsync(tenBai.Trim(), noiDung, ct);
            if (banNhap.Count == 0) throw new InvalidOperationException("AI không tìm được nội dung để tạo bản nháp.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            BaoLoi(ex.Message);
            var nhap = GiuNoiDungNhap();
            return cd is null ? RedirectToAction(nameof(TaoChuDe), new { khoa = khoaId, nhap })
                : RedirectToAction(nameof(TaoBai), new { chuDeId = cd.Id, nhap });
        }
        var chuDeMoi = cd is null;
        if (chuDeMoi)
        {
            var thuTuChuDe = await Db.ChuDe.CountAsync(c => c.KhoaId == khoaId, ct);
            cd = new ChuDe { KhoaId = khoaId, Ten = $"Chủ đề {thuTuChuDe + 1} · {tenChuDe!.Trim()}", ThuTu = thuTuChuDe };
        }
        var maMoi = (await Db.Bai.Where(b => b.Id < 1000).MaxAsync(b => (int?)b.Id, ct) ?? 0) + 1;
        var thuTuBai = chuDeMoi ? 0 : await Db.Bai.CountAsync(b => b.ChuDeId == cd!.Id, ct);
        var bai = new Bai { Id = maMoi, So = soBai.Value, Ten = tenBai.Trim(), ThuTu = thuTuBai, DaXuatBan = false };
        cd!.Bai.Add(bai);
        if (chuDeMoi) Db.ChuDe.Add(cd); else Db.Bai.Add(bai);
        await Db.SaveChangesAsync(ct);
        return MoXemThu(bai.Id, banNhap, chiaTheoVanBan);
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
    public async Task<IActionResult> XoaChuDe(int id)
    {
        var cd = await Db.ChuDe.Include(c => c.Bai).ThenInclude(b => b.Muc).ThenInclude(m => m.Phan)
            .FirstOrDefaultAsync(c => c.Id == id && (LaAdmin || c.Khoa!.GiangVienId == Uid));
        if (cd is null) return NotFound();
        var khoaId = cd.KhoaId;
        Db.ChuDe.Remove(cd);
        await Db.SaveChangesAsync();
        ThongBao("Đã xoá chủ đề và toàn bộ bài học trong chủ đề.");
        return RedirectToAction(nameof(Index), new { khoa = khoaId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemBai(int chuDeId, string ten, int soBai)
    {
        var cd = await Db.ChuDe.Include(c => c.Bai).FirstOrDefaultAsync(c => c.Id == chuDeId && (LaAdmin || c.Khoa!.GiangVienId == Uid));
        if (cd is null || string.IsNullOrWhiteSpace(ten) || soBai < 1) return BadRequest();
        // Mã đường dẫn tự sinh, số bài hiển thị do giảng viên nhập.
        var maMoi = (await Db.Bai.Where(b => b.Id < 1000).MaxAsync(b => (int?)b.Id) ?? 0) + 1;
        Db.Bai.Add(new Bai { Id = maMoi, ChuDeId = cd.Id, So = soBai, Ten = ten.Trim(), ThuTu = cd.Bai.Count, DaXuatBan = false });
        await Db.SaveChangesAsync();
        ThongBao($"Đã tạo Bài {soBai} (đang ở dạng nháp). Thêm mục để soạn nội dung.");
        return RedirectToAction(nameof(SoanBai), new { id = maMoi });
    }

    // Soạn một bài: thông tin bài + danh sách mục
    public async Task<IActionResult> SoanBai(int id, string? nhap)
    {
        var bai = await BaiCuaToi().AsNoTracking().Include(b => b.ChuDe!).ThenInclude(c => c.Khoa)
            .Include(b => b.Muc.OrderBy(m => m.So)).ThenInclude(m => m.Phan).FirstOrDefaultAsync(b => b.Id == id);
        if (bai is null) return NotFound();
        ViewBag.SoCau = await Db.CauHoi.CountAsync(c => c.BaiId == id);
        PhucHoiNhap(nhap);
        return View(bai);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuBai(int id, string ten, int soBai, bool hocThu, bool daXuatBan)
    {
        var bai = await BaiCuaToi().FirstOrDefaultAsync(b => b.Id == id);
        if (bai is null || string.IsNullOrWhiteSpace(ten) || soBai < 1) return BadRequest();
        bai.So = soBai;
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoNoiDungBangAi(int baiId, string noiDung, bool chiaTheoVanBan = false, CancellationToken ct = default)
    {
        var bai = await BaiCuaToi().Include(b => b.ChuDe!).ThenInclude(c => c.Khoa)
            .Include(b => b.Muc).ThenInclude(m => m.Phan).FirstOrDefaultAsync(b => b.Id == baiId, ct);
        if (bai is null) return NotFound();
        noiDung = (noiDung ?? "").Trim();
        if (noiDung.Length is < 30 or > 30000)
        {
            BaoLoi("Nội dung cần từ 30 đến 30.000 ký tự.");
            return RedirectToAction(nameof(SoanBai), new { id = baiId });
        }
        try
        {
            var banNhap = chiaTheoVanBan ? ChiaDeMucVanBan.Chia(bai.Ten, noiDung) : await ai.ChiaNoiDungAsync(bai.Ten, noiDung, ct);
            if (banNhap.Count == 0) throw new InvalidOperationException("AI không tìm được nội dung để tạo bản nháp.");
            return MoXemThu(baiId, banNhap, chiaTheoVanBan);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            BaoLoi(ex.Message);
            return RedirectToAction(nameof(SoanBai), new { id = baiId, nhap = GiuNoiDungNhap() });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChiaSeBanNhapAi(BanNhapNoiDungAiForm f, bool thayThe = false, CancellationToken ct = default)
    {
        var bai = await BaiCuaToi().Include(b => b.Muc).FirstOrDefaultAsync(b => b.Id == f.BaiId, ct);
        if (bai is null) return NotFound();
        if (f.Muc.Count == 0) { BaoLoi("Bản nháp chưa có mục nào để chia sẻ."); return RedirectToAction(nameof(SoanBai), new { id = f.BaiId }); }
        if (thayThe) Db.Muc.RemoveRange(bai.Muc);
        var so = thayThe ? 1 : bai.Muc.Count + 1;
        foreach (var m in f.Muc.Where(m => !string.IsNullOrWhiteSpace(m.Ten)))
        {
            var muc = new Muc { So = so++, Ten = m.Ten.Trim(), TieuDe = string.IsNullOrWhiteSpace(m.TieuDe) ? m.Ten.Trim() : m.TieuDe.Trim() };
            var maDung = new HashSet<string>(); var thuTu = 0;
            foreach (var p in m.Phan.Where(p => !string.IsNullOrWhiteSpace(p.TieuDe) || !string.IsNullOrWhiteSpace(p.DoanVan)))
            {
                var td = string.IsNullOrWhiteSpace(p.TieuDe) ? "Nội dung" : p.TieuDe.Trim(); var ma = Chuoi.TaoMaNeo(td);
                for (var i = 2; !maDung.Add(ma); i++) ma = $"{Chuoi.TaoMaNeo(td)}-{i}";
                muc.Phan.Add(new Phan { ThuTu = thuTu++, MaNeo = ma, TieuDe = td, LaGhiNho = p.LaGhiNho, DoanVan = (p.DoanVan ?? "").Replace("\r", "").Split("\n\n").Select(x => x.Trim()).Where(x => x.Length > 0).ToList() });
            }
            bai.Muc.Add(muc);
        }
        bai.SoMuc = thayThe ? f.Muc.Count : bai.Muc.Count;
        await Db.SaveChangesAsync(ct);
        ThongBao(f.PhamVi == "DoiMienPhi" ? "Đã chia sẻ bản đã duyệt cho đội miễn phí." : "Đã chia sẻ nội dung vào khoá học.");
        return RedirectToAction(nameof(SoanBai), new { id = f.BaiId });
    }

    public async Task<IActionResult> SuaMuc(int id, string? nhap)
    {
        var muc = await Db.Muc.AsNoTracking().Include(m => m.Bai!).ThenInclude(b => b.ChuDe!).ThenInclude(c => c.Khoa)
            .Include(m => m.Phan.OrderBy(p => p.ThuTu)).ThenInclude(p => p.Moc.OrderBy(x => x.ThuTu))
            .FirstOrDefaultAsync(m => m.Id == id && (LaAdmin || m.Bai!.ChuDe!.Khoa!.GiangVienId == Uid));
        if (muc is null) return NotFound();
        ViewBag.Bai = muc.Bai;
        var dangSua = banNhap.Doc<MucForm>(Uid, nhap);
        if (dangSua?.Id == id) return View(dangSua);
        return View(new MucForm
        {
            Id = muc.Id, BaiId = muc.BaiId, Ten = muc.Ten, TieuDe = muc.TieuDe == muc.Ten ? "" : muc.TieuDe,
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
            f.BaiId = muc.BaiId;
            BaoLoi("Anh kiểm tra lại tiêu đề và nội dung mục nhé. Các phần vừa nhập vẫn được giữ lại.");
            var nhap = banNhap.Luu(Uid, f);
            return RedirectToAction(nameof(SuaMuc), new { id = muc.Id, nhap });
        }

        muc.Ten = f.Ten.Trim();
        muc.TieuDe = string.IsNullOrWhiteSpace(f.TieuDe) ? muc.Ten : f.TieuDe.Trim();
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
