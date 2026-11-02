using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;
using SuDia.Web.Services;

namespace SuDia.Web.Areas.GiangVien.Controllers;

// Soạn nội dung: Khoá → Chủ đề → Bài → Mục → Phần (đoạn văn, mốc thời gian, ghi nhớ)
public class NoiDungController(AppDbContext db, ISoanNoiDungAiService ai, KhoBanNhapNoiDung banNhap) : GiangVienControllerBase(db)
{
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(22 * 1024 * 1024)]
    public async Task<IActionResult> NhapTaiLieu(IFormFile? taiLieu, CancellationToken ct)
    {
        if (taiLieu is null || taiLieu.Length == 0 || taiLieu.Length > DocTaiLieuOffice.ToiDaByte)
            return BadRequest(new { loi = "Anh chọn tài liệu Word hoặc PowerPoint, tối đa 20 MB." });
        var extension = Path.GetExtension(taiLieu.FileName).ToLowerInvariant();
        if (extension is not ".docx" and not ".pptx")
            return BadRequest(new { loi = "Chỉ nhận .docx và .pptx. Với file .doc hoặc .ppt cũ, anh lưu lại sang định dạng mới nhé." });
        try
        {
            using var buffer = new MemoryStream();
            await taiLieu.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            return Json(DocTaiLieuOffice.Doc(buffer, extension, taiLieu.FileName));
        }
        catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException or ArgumentException or UriFormatException)
        {
            return BadRequest(new { loi = ex is InvalidDataException ? ex.Message : "Không đọc được tài liệu. Anh kiểm tra file có bị hỏng hoặc có mật khẩu không nhé." });
        }
    }

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
    // Đọc các phần từ biểu mẫu (bỏ phần trống): đoạn văn cách nhau một dòng trống, mỗi dòng mốc "Tên | Thời gian | Nội dung"
    private static List<Phan> PhanTuForm(IEnumerable<PhanForm> ds)
    {
        var kq = new List<Phan>();
        var daDung = new HashSet<string>();
        foreach (var p in ds.Where(p => !string.IsNullOrWhiteSpace(p.TieuDe) || !string.IsNullOrWhiteSpace(p.DoanVan)))
        {
            var tieuDe = string.IsNullOrWhiteSpace(p.TieuDe) ? "Nội dung" : p.TieuDe.Trim();
            var ma = Chuoi.TaoMaNeo(tieuDe);
            for (var i = 2; !daDung.Add(ma); i++) ma = $"{Chuoi.TaoMaNeo(tieuDe)}-{i}";
            kq.Add(new Phan
            {
                ThuTu = kq.Count, MaNeo = ma, TieuDe = tieuDe, LaGhiNho = p.LaGhiNho,
                DoanVan = (p.DoanVan ?? "").Replace("\r", "").Split("\n\n").Select(x => x.Trim()).Where(x => x.Length > 0).ToList(),
                Moc = (p.Moc ?? "").Replace("\r", "").Split('\n').Select(x => x.Split('|').Select(y => y.Trim()).ToArray())
                    .Where(x => x.Length >= 3 && x.Any(y => y.Length > 0))
                    .Select((x, i) => new MocThoiGian { ThuTu = i, Ten = x[0], ThoiGian = x[1], NoiDung = string.Join(" | ", x[2..]) }).ToList(),
            });
        }
        return kq;
    }

    private static MucNoiDung DoiMuc(int so, string ten, string tieuDe, int soPhut, IEnumerable<Phan> phan) =>
        new(so, ten, tieuDe, soPhut, phan.OrderBy(p => p.ThuTu).Select(p => new PhanNoiDung(p.MaNeo, p.TieuDe, p.DoanVan,
            p.Moc.Count == 0 ? null : p.Moc.OrderBy(x => x.ThuTu).Select(x => new DotItem(x.Ten, x.ThoiGian, x.NoiDung)).ToList(),
            p.LaGhiNho)).ToList());

    // Xem trước cả bài như học viên (kể cả bài nháp), mở trong khung nổi của trang soạn
    public async Task<IActionResult> XemTruoc(int id)
    {
        var bai = await BaiCuaToi().AsNoTracking().Include(b => b.Muc).ThenInclude(m => m.Phan).ThenInclude(p => p.Moc)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (bai is null) return NotFound();
        var muc = bai.Muc.OrderBy(m => m.So).Select(m => DoiMuc(m.So, m.Ten, m.TieuDe, m.SoPhut, m.Phan)).ToList();
        return View(new XemTruocBaiModel($"BÀI {bai.So} · {muc.Count} MỤC", bai.Ten, muc,
            bai.DaXuatBan ? null : "Bài đang ở dạng nháp: học viên chưa thấy bài này."));
    }

    // Xem trước mục đang sửa, theo đúng nội dung đang nhập trên biểu mẫu (chưa cần lưu)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XemTruocMuc(MucForm f)
    {
        var muc = await Db.Muc.AsNoTracking().Include(m => m.Bai)
            .FirstOrDefaultAsync(m => m.Id == f.Id && (LaAdmin || m.Bai!.ChuDe!.Khoa!.GiangVienId == Uid));
        if (muc is null) return NotFound();
        var ten = string.IsNullOrWhiteSpace(f.Ten) ? muc.Ten : f.Ten.Trim();
        var xem = DoiMuc(muc.So, ten, string.IsNullOrWhiteSpace(f.TieuDe) ? ten : f.TieuDe.Trim(), muc.SoPhut, PhanTuForm(f.Phan));
        return View("XemTruoc", new XemTruocBaiModel($"BÀI {muc.Bai!.So} · MỤC {muc.So}", muc.Bai.Ten, [xem],
            "Đang xem nội dung đang nhập, chưa lưu. Bấm \"Lưu thay đổi\" để học viên thấy."));
    }

    // Không chọn khoá: danh sách khoá được phân công; chọn khoá: chủ đề và bài của khoá đó
    public async Task<IActionResult> Index(string? khoa)
    {
        if (string.IsNullOrEmpty(khoa))
        {
            var bayGio = DateTime.UtcNow;
            ViewBag.DsKhoa = await KhoaCuaToi().AsNoTracking().OrderBy(x => x.ThuTu).Select(x => new TheKhoaGiangVien(
                x.Id, x.Ten, x.MoTa, x.Anh, x.ChuDe.Count,
                x.ChuDe.SelectMany(c => c.Bai).Count(b => b.DaXuatBan), x.ChuDe.SelectMany(c => c.Bai).Count(b => !b.DaXuatBan),
                Db.GhiDanh.Count(g => g.KhoaId == x.Id && (g.HanDen == null || g.HanDen >= bayGio)), x.HienThi)).ToListAsync();
            return View("DanhSachKhoa");
        }
        var chon = await KhoaCuaToi().AsNoTracking().FirstOrDefaultAsync(k => k.Id == khoa);
        if (chon is null) return RedirectToAction(nameof(Index), new { khoa = (string?)null });
        ViewBag.SoChuDeGoiY = SoChuDeGoiY(await Db.ChuDe.Where(c => c.KhoaId == chon.Id).Select(c => c.Ten).ToListAsync());
        var k = await Db.Khoa.AsNoTracking().Where(x => x.Id == chon.Id)
            .Include(x => x.ChuDe.OrderBy(c => c.ThuTu)).ThenInclude(c => c.Bai.OrderBy(b => b.ThuTu)).ThenInclude(b => b.Muc)
            .Include(x => x.ChuDe).ThenInclude(c => c.Bai).ThenInclude(b => b.CauHoi)
            .FirstAsync();
        return View(k);
    }

    // Tạo chủ đề giờ chỉ cần tên, làm ngay ở trang khoá học (ô dưới danh sách chủ đề)
    public IActionResult TaoChuDe(string? khoa) => RedirectToAction(nameof(Index), new { khoa });

    [HttpGet]
    public async Task<IActionResult> TaoBai(int chuDeId, string? nhap)
    {
        var chuDe = await Db.ChuDe.AsNoTracking().Include(c => c.Khoa)
            .FirstOrDefaultAsync(c => c.Id == chuDeId && (LaAdmin || c.Khoa!.GiangVienId == Uid));
        if (chuDe is null) return NotFound();
        ViewBag.ChuDe = chuDe;
        ViewBag.SoBaiGoiY = (await Db.Bai.Where(b => b.ChuDe!.KhoaId == chuDe.KhoaId).MaxAsync(b => (int?)b.So) ?? 0) + 1;
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
    public async Task<IActionResult> ThemChuDe(string khoaId, string ten, int? soChuDe)
    {
        if (!await KhoaCuaToi().AnyAsync(k => k.Id == khoaId) || string.IsNullOrWhiteSpace(ten)) return BadRequest();
        var dsTen = await Db.ChuDe.Where(c => c.KhoaId == khoaId).Select(c => c.Ten).ToListAsync();
        var daCo = dsTen.Select(SoCuaChuDe).OfType<int>().ToHashSet();
        var so = soChuDe ?? SoChuDeGoiY(dsTen);
        if (so < 1 || daCo.Contains(so))
        {
            BaoLoi(so < 1 ? "Số chủ đề từ 1 trở lên." : $"Khoá đã có Chủ đề {so}. Chọn số khác.");
            return RedirectToAction(nameof(Index), new { khoa = khoaId });
        }
        // Người dùng gõ cả "Chủ đề 3. …" vào ô tên: bỏ tiền tố để không lặp số
        var tenGon = System.Text.RegularExpressions.Regex.Replace(ten.Trim(), @"^(chủ\s*đề|chương)\s*\d*\s*[.:·\-–—]?\s*", "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        Db.ChuDe.Add(new ChuDe { KhoaId = khoaId, Ten = $"Chủ đề {so} · {(tenGon.Length > 0 ? tenGon : ten.Trim())}", ThuTu = dsTen.Count });
        await Db.SaveChangesAsync();
        ThongBao($"Đã tạo Chủ đề {so}. Thêm bài học ở menu ⋮ của chủ đề.");
        return RedirectToAction(nameof(Index), new { khoa = khoaId });
    }

    // Tên chủ đề lưu dạng "Chủ đề 3 · …": lấy số để gợi ý số tiếp theo và chặn trùng
    private static int? SoCuaChuDe(string ten) =>
        System.Text.RegularExpressions.Regex.Match(ten, @"^\s*chủ\s*đề\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase) is { Success: true } m
            ? int.Parse(m.Groups[1].Value) : null;

    private static int SoChuDeGoiY(IEnumerable<string> dsTen) => (dsTen.Select(SoCuaChuDe).OfType<int>().DefaultIfEmpty(0).Max()) + 1;

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
        // Tài liệu cả chủ đề (các dòng "Bài N"): tách thành nhiều bài theo tài liệu, xem thử rồi mới tạo
        var tach = noiDung.Length is >= 30 and <= 30000 ? ChiaChuDe.Chia(noiDung) : null;
        if (tach is not null && (tach.Bai.Count >= 2 || string.IsNullOrWhiteSpace(tenBai)))
        {
            var ten = string.IsNullOrWhiteSpace(tenChuDe) ? tach.Ten : tenChuDe.Trim();
            if (cd is null && string.IsNullOrWhiteSpace(ten))
            {
                BaoLoi("Nhập tên chủ đề.");
                var nhapLoi = GiuNoiDungNhap();
                return RedirectToAction(nameof(TaoChuDe), new { khoa = khoaId, nhap = nhapLoi });
            }
            var draft = new BanNhapChuDe
            {
                KhoaId = khoaId, ChuDeId = cd?.Id, TenChuDe = cd?.Ten ?? ten!, SoChuDe = tach.So,
                Bai = tach.Bai.Select((b, i) => new BaiNhapChuDe { So = b.So ?? (soBai ?? 1) + i, Ten = b.Ten, Muc = b.Muc.Select(DoiMucForm).ToList() }).ToList(),
            };
            return RedirectToAction(nameof(XemThuChuDe), new { nhap = banNhap.Luu(Uid, draft) });
        }
        var thieuTen = (cd is null && string.IsNullOrWhiteSpace(tenChuDe)) || string.IsNullOrWhiteSpace(tenBai);
        if (thieuTen || soBai is null or < 1 || noiDung.Length is < 30 or > 30000)
        {
            BaoLoi(thieuTen ? (cd is null ? "Nhập tên chủ đề và tên bài học." : "Tài liệu không có dòng \"Bài N…\": điền số và tên bài ở mục \"Tài liệu chỉ có một bài?\".") : soBai is null or < 1
                ? "Số bài là số nguyên từ 1 trở lên." : "Nội dung cần từ 30 đến 30.000 ký tự.");
            var nhap = GiuNoiDungNhap();
            return cd is null ? RedirectToAction(nameof(TaoChuDe), new { khoa = khoaId, nhap })
                : RedirectToAction(nameof(TaoBai), new { chuDeId = cd.Id, nhap });
        }
        IReadOnlyList<NoiDungAiMuc> dsMuc;
        try
        {
            dsMuc = chiaTheoVanBan ? (tach?.Bai[0].Muc ?? ChiaDeMucVanBan.Chia(tenBai.Trim(), noiDung)) : await ai.ChiaNoiDungAsync(tenBai.Trim(), noiDung, ct);
            if (dsMuc.Count == 0) throw new InvalidOperationException("AI không tìm được nội dung để tạo bản nháp.");
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
        return MoXemThu(bai.Id, dsMuc, chiaTheoVanBan);
    }

    private static MucForm DoiMucForm(NoiDungAiMuc m) => new()
    {
        Ten = m.Ten, TieuDe = m.TieuDe,
        Phan = m.Phan.Select(p => new PhanForm { TieuDe = p.TieuDe, DoanVan = p.DoanVan, LaGhiNho = p.LaGhiNho }).ToList(),
    };

    [HttpGet]
    public async Task<IActionResult> XemThuChuDe(string? nhap)
    {
        var draft = banNhap.Doc<BanNhapChuDe>(Uid, nhap);
        if (draft is null) { BaoLoi("Bản xem thử đã hết hạn. Nhập lại tài liệu."); return RedirectToAction(nameof(Index)); }
        var khoa = await KhoaCuaToi().AsNoTracking().FirstOrDefaultAsync(k => k.Id == draft.KhoaId);
        if (khoa is null) return NotFound();
        ViewBag.Khoa = khoa;
        ViewBag.Nhap = nhap;
        return View(draft);
    }

    /// <summary>Tạo chủ đề (hoặc thêm vào chủ đề có sẵn) cùng các bài đã chọn, mỗi bài kèm các mục đã tách. Bài ở dạng nháp.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoChuDeTuBanNhap(string? nhap, string? tenChuDe, List<BaiNhapChuDe> bai, CancellationToken ct)
    {
        var draft = banNhap.Doc<BanNhapChuDe>(Uid, nhap);
        if (draft is null) { BaoLoi("Bản xem thử đã hết hạn. Nhập lại tài liệu."); return RedirectToAction(nameof(Index)); }
        var khoa = await KhoaCuaToi().FirstOrDefaultAsync(k => k.Id == draft.KhoaId, ct);
        if (khoa is null) return NotFound();
        ChuDe? cd = null;
        if (draft.ChuDeId is int id)
        {
            cd = await Db.ChuDe.FirstOrDefaultAsync(c => c.Id == id && c.KhoaId == khoa.Id, ct);
            if (cd is null) return NotFound();
        }
        var chon = draft.Bai.Select((b, i) => (Goc: b, Sua: i < bai.Count ? bai[i] : b)).Where(x => x.Sua.Chon).ToList();
        if (chon.Count == 0 || chon.Any(x => string.IsNullOrWhiteSpace(x.Sua.Ten) || x.Sua.So < 1)
            || (cd is null && string.IsNullOrWhiteSpace(tenChuDe)))
        {
            BaoLoi(chon.Count == 0 ? "Chọn ít nhất một bài." : "Nhập tên chủ đề, tên và số bài (từ 1) cho các bài đã chọn.");
            return RedirectToAction(nameof(XemThuChuDe), new { nhap });
        }
        if (cd is null)
        {
            var thuTu = await Db.ChuDe.CountAsync(c => c.KhoaId == khoa.Id, ct);
            cd = new ChuDe { KhoaId = khoa.Id, Ten = $"Chủ đề {draft.SoChuDe ?? thuTu + 1} · {tenChuDe!.Trim()}", ThuTu = thuTu };
            Db.ChuDe.Add(cd);
        }
        var maMoi = (await Db.Bai.Where(b => b.Id < 1000).MaxAsync(b => (int?)b.Id, ct) ?? 0) + 1;
        var thuTuBai = cd.Id == 0 ? 0 : await Db.Bai.CountAsync(b => b.ChuDeId == cd.Id, ct);
        foreach (var (goc, sua) in chon)
        {
            var b = new Bai { Id = maMoi++, So = sua.So, Ten = sua.Ten.Trim(), ThuTu = thuTuBai++, DaXuatBan = false };
            var so = 1;
            foreach (var m in goc.Muc.Where(m => !string.IsNullOrWhiteSpace(m.Ten)))
            {
                var muc = new Muc { So = so++, Ten = m.Ten.Trim(), TieuDe = string.IsNullOrWhiteSpace(m.TieuDe) ? m.Ten.Trim() : m.TieuDe.Trim() };
                muc.Phan.AddRange(PhanTuForm(m.Phan));
                b.Muc.Add(muc);
            }
            b.SoMuc = b.Muc.Count;
            cd.Bai.Add(b);
        }
        await Db.SaveChangesAsync(ct);
        banNhap.Xoa(Uid, nhap);
        ThongBao($"Đã tạo {chon.Count} bài ở dạng nháp trong {cd.Ten}. Mở từng bài để rà lại rồi xuất bản.");
        return RedirectToAction(nameof(Index), new { khoa = khoa.Id });
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
        var lyDo = await LyDoGiuChuDe(cd.Bai.Select(b => b.Id).ToList());
        if (lyDo is not null)
        {
            BaoLoi($"Không xoá được chủ đề: {lyDo}. Hãy ẩn bài (bỏ xuất bản) thay vì xoá để giữ dữ liệu.");
            return RedirectToAction(nameof(Index), new { khoa = khoaId });
        }
        Db.ChuDe.Remove(cd);
        await Db.SaveChangesAsync();
        ThongBao("Đã xoá chủ đề và toàn bộ bài học trong chủ đề.");
        return RedirectToAction(nameof(Index), new { khoa = khoaId });
    }

    // Xoá bài sẽ xoá dây chuyền bài nộp, câu hỏi trong đề; tiến độ và bài luyện không có khoá ngoại
    // nên sẽ dính sang bài mới được cấp lại mã. Chỉ cho xoá khi các bài chưa có dữ liệu như vậy.
    private async Task<string?> LyDoGiuChuDe(List<int> baiIds)
    {
        if (baiIds.Count == 0) return null;
        if (await Db.BaiTapGiao.AnyAsync(b => baiIds.Contains(b.BaiId) && b.DeThi != null))
            return "có bài kiểm tra gắn với bài học trong chủ đề";
        if (await Db.DeThiCau.AnyAsync(c => baiIds.Contains(c.CauHoi!.BaiId)))
            return "câu hỏi của chủ đề đang nằm trong đề thi thử";
        if (await Db.BaiNopGiao.AnyAsync(n => baiIds.Contains(n.BaiTapGiao!.BaiId)))
            return "học viên đã nộp bài tập của chủ đề";
        if (await Db.BaiLam.AnyAsync(b => !b.LaDeThi && baiIds.Contains(b.MaDe))
            || await Db.TienDoBai.AnyAsync(t => baiIds.Contains(t.BaiId)))
            return "học viên đã học hoặc luyện tập bài trong chủ đề";
        return null;
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
        bai.Ten = string.Join(' ', ten.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)); // ô tên nhiều dòng: bỏ xuống dòng
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
        var soBaiTrongTaiLieu = ChiaChuDe.DemBai(noiDung);
        if (soBaiTrongTaiLieu >= 2)
        {
            BaoLoi($"Tài liệu có {soBaiTrongTaiLieu} bài. Để tách thành từng bài, dùng \"Thêm bài học\" ở chủ đề (menu ⋮) hoặc \"Tạo chủ đề\".");
            return RedirectToAction(nameof(SoanBai), new { id = baiId, nhap = GiuNoiDungNhap() });
        }
        try
        {
            var dsMuc = chiaTheoVanBan ? (soBaiTrongTaiLieu == 1 ? ChiaChuDe.Chia(noiDung)!.Bai[0].Muc : ChiaDeMucVanBan.Chia(bai.Ten, noiDung))
                : await ai.ChiaNoiDungAsync(bai.Ten, noiDung, ct);
            if (dsMuc.Count == 0) throw new InvalidOperationException("AI không tìm được nội dung để tạo bản nháp.");
            return MoXemThu(baiId, dsMuc, chiaTheoVanBan);
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
        muc.Phan.AddRange(PhanTuForm(f.Phan));
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
