using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;

namespace SuDia.Web.Services.TroLy;

/// <summary>Một việc cập nhật chỉ mục. Loai: Bai | Muc | Phan | TaiLieu | KhoaThongTin | KhoaToanBo | ChuDe | Goi | TatCa.</summary>
public record YeuCauChiMuc(string Loai, string Ma = "");

/// <summary>
/// Hàng đợi + tiến trình nền cập nhật bảng DoanChiMuc. Interceptor đẩy việc vào sau mỗi lần lưu nội dung;
/// tiến trình gom các việc trong ~0,5 giây, tìm ra đơn vị nguồn bị ảnh hưởng và chỉ dựng lại các đơn vị đó.
/// Khi khởi động (chỉ mục trống) và định kỳ mỗi 12 giờ: dựng lại toàn bộ để bắt các thay đổi không qua EF.
/// </summary>
public class DongBoChiMuc(IServiceScopeFactory phamVi, KhoTaiLieu kho, ILogger<DongBoChiMuc> log) : BackgroundService
{
    private readonly Channel<YeuCauChiMuc> _hang = Channel.CreateUnbounded<YeuCauChiMuc>();
    private static readonly TimeSpan ChuKyToanBo = TimeSpan.FromHours(12);
    private long _soViecXong;

    public void Gui(YeuCauChiMuc y) => _hang.Writer.TryWrite(y);
    public void Gui(IEnumerable<YeuCauChiMuc> ds) { foreach (var y in ds) Gui(y); }

    /// <summary>Số lượt cập nhật đã chạy xong (để kiểm thử / trang quản trị biết chỉ mục đã theo kịp).</summary>
    public long SoViecXong => Interlocked.Read(ref _soViecXong);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try { await KhoiDongAsync(ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Chỉ mục: dựng lần đầu lỗi"); }

        var lanToanBo = DateTime.UtcNow;
        while (!ct.IsCancellationRequested)
        {
            using var hen = CancellationTokenSource.CreateLinkedTokenSource(ct);
            hen.CancelAfter(TimeSpan.FromMinutes(5));
            try { await _hang.Reader.WaitToReadAsync(hen.Token); }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }

            var viec = new List<YeuCauChiMuc>();
            if (_hang.Reader.TryPeek(out _))
            {
                await Task.Delay(500, ct);   // gom các lần lưu liên tiếp
                while (_hang.Reader.TryRead(out var y)) viec.Add(y);
            }
            if (DateTime.UtcNow - lanToanBo > ChuKyToanBo) { viec.Add(new("TatCa")); lanToanBo = DateTime.UtcNow; }
            if (viec.Count == 0) continue;
            try
            {
                await ChayAsync(viec, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Chỉ mục: cập nhật lỗi, sẽ dựng lại toàn bộ ở lượt sau");
                Gui(new YeuCauChiMuc("TatCa"));
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }
        }
    }

    private async Task KhoiDongAsync(CancellationToken ct)
    {
        using var scope = phamVi.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!await db.DoanChiMuc.AnyAsync(ct)) await ChayAsync([new("TatCa")], ct);
        else Interlocked.Increment(ref _soViecXong);
    }

    /// <summary>Chạy ngay (không qua hàng đợi): dùng cho nút "Nạp lại ngay" và lệnh dòng lệnh.</summary>
    public async Task ChayAsync(IReadOnlyCollection<YeuCauChiMuc> viec, CancellationToken ct = default)
    {
        using var scope = phamVi.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Có giao dịch tự mở bên trong nên phải chạy qua chiến lược thử lại của EF (EnableRetryOnFailure)
        var lan = 0;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (lan++ > 0) db.ChangeTracker.Clear();
            await ChayTrongPhienAsync(db, viec, ct);
        });
    }

    private async Task ChayTrongPhienAsync(AppDbContext db, IReadOnlyCollection<YeuCauChiMuc> viec, CancellationToken ct)
    {
        if (viec.Any(v => v.Loai == "TatCa")) { await DungLaiToanBoAsync(db, ct); Interlocked.Increment(ref _soViecXong); return; }

        // Quy đổi về đơn vị nguồn: khoá cần dựng toàn bộ, bài, tài liệu, thông tin khoá, gói
        var khoaToanBo = new HashSet<string>();
        var bai = new HashSet<int>();
        var taiLieu = new HashSet<int>();
        var thongTin = new HashSet<string>();
        var goi = false;
        foreach (var v in viec)
        {
            switch (v.Loai)
            {
                case "KhoaToanBo": khoaToanBo.Add(v.Ma); break;
                case "KhoaThongTin": thongTin.Add(v.Ma); break;
                case "Goi": goi = true; break;
                case "TaiLieu": taiLieu.Add(int.Parse(v.Ma)); break;
                case "Bai": bai.Add(int.Parse(v.Ma)); break;
                case "ChuDe":
                    if (await db.ChuDe.Where(c => c.Id == int.Parse(v.Ma)).Select(c => c.KhoaId).FirstOrDefaultAsync(ct) is { } k) thongTin.Add(k);
                    break;
                case "Muc":
                    if (await db.Muc.Where(m => m.Id == int.Parse(v.Ma)).Select(m => (int?)m.BaiId).FirstOrDefaultAsync(ct) is int b1) bai.Add(b1);
                    break;
                case "Phan":
                    if (await db.Phan.Where(p => p.Id == int.Parse(v.Ma)).Select(p => (int?)p.Muc!.BaiId).FirstOrDefaultAsync(ct) is int b2) bai.Add(b2);
                    break;
            }
        }
        // Bài đổi thì danh sách bài trong "thông tin khoá" cũng đổi
        foreach (var k in await db.Bai.Where(b => bai.Contains(b.Id)).Select(b => b.ChuDe!.KhoaId).Distinct().ToListAsync(ct)) thongTin.Add(k);

        var thay = new Dictionary<string, List<DoanTaiLieu>>();
        var xoaTheoKhoa = new List<string>();
        foreach (var k in khoaToanBo)
        {
            xoaTheoKhoa.Add(k);
            foreach (var (khoa, ds) in await NguonCoSoDuLieu.DoanCuaKhoaAsync(db, k, ct)) thay[khoa] = ds;
            goi = true;   // tên / trạng thái khoá xuất hiện trong mô tả gói
        }
        foreach (var id in bai.Where(id => !thay.ContainsKey(NguonCoSoDuLieu.KhoaBai(id))))
            thay[NguonCoSoDuLieu.KhoaBai(id)] = await NguonCoSoDuLieu.DoanCuaBaiAsync(db, id, ct);
        foreach (var id in taiLieu.Where(id => !thay.ContainsKey(NguonCoSoDuLieu.KhoaTaiLieu(id))))
            thay[NguonCoSoDuLieu.KhoaTaiLieu(id)] = await NguonCoSoDuLieu.DoanCuaTaiLieuAsync(db, id, ct);
        foreach (var k in thongTin.Where(k => !thay.ContainsKey(NguonCoSoDuLieu.KhoaThongTin(k))))
            thay[NguonCoSoDuLieu.KhoaThongTin(k)] = await NguonCoSoDuLieu.DoanThongTinKhoaAsync(db, k, ct);
        if (goi) thay[NguonCoSoDuLieu.KhoaGoi] = await NguonCoSoDuLieu.DoanGoiAsync(db, ct);

        await GhiAsync(db, thay, xoaTheoKhoa, ct);
        // Khoá vừa có / vừa hết nội dung trong hệ thống: bật / tắt tệp .md dự phòng của khoá đó
        await DongBoTepMdAsync(db, thay.Values.SelectMany(d => d).Select(d => d.Khoa).Concat(khoaToanBo).Concat(thongTin)
            .Where(k => k.Length > 0).ToHashSet(), ct);
        Interlocked.Increment(ref _soViecXong);
    }

    private async Task DungLaiToanBoAsync(AppDbContext db, CancellationToken ct)
    {
        var thay = new Dictionary<string, List<DoanTaiLieu>>();
        foreach (var k in await db.Khoa.Select(k => k.Id).ToListAsync(ct))
            foreach (var (khoa, ds) in await NguonCoSoDuLieu.DoanCuaKhoaAsync(db, k, ct)) thay[khoa] = ds;
        thay[NguonCoSoDuLieu.KhoaGoi] = await NguonCoSoDuLieu.DoanGoiAsync(db, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.DoanChiMuc.ExecuteDeleteAsync(ct);
        await GhiAsync(db, thay, [], ct, trongGiaoDich: true);
        await DongBoTepMdAsync(db, null, ct, trongGiaoDich: true);
        await tx.CommitAsync(ct);
        log.LogInformation("Chỉ mục: dựng lại toàn bộ, {N} đoạn", thay.Values.Sum(d => d.Count));
    }

    private static async Task GhiAsync(AppDbContext db, Dictionary<string, List<DoanTaiLieu>> thay, List<string> xoaTheoKhoa,
        CancellationToken ct, bool trongGiaoDich = false)
    {
        if (thay.Count == 0 && xoaTheoKhoa.Count == 0) return;
        var tx = trongGiaoDich ? null : await db.Database.BeginTransactionAsync(ct);
        try
        {
            var khoaNguon = thay.Keys.ToList();
            if (xoaTheoKhoa.Count > 0)
                await db.DoanChiMuc.Where(d => d.KhoaId != null && xoaTheoKhoa.Contains(d.KhoaId) && !d.NguonKhoa.StartsWith("Md:")).ExecuteDeleteAsync(ct);
            await db.DoanChiMuc.Where(d => khoaNguon.Contains(d.NguonKhoa)).ExecuteDeleteAsync(ct);
            var luc = DateTime.UtcNow;
            foreach (var (nguon, ds) in thay)
                db.DoanChiMuc.AddRange(ds.Select((d, i) => new DoanChiMuc
                {
                    NguonKhoa = nguon, KhoaId = d.Khoa.Length > 0 ? d.Khoa : null, Loai = d.Loai, Mon = d.Mon,
                    Nguon = d.Nguon, NoiDung = d.NoiDung, Lien = d.Lien, ThuTu = i, NapLuc = luc,
                }));
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            if (tx is not null) await tx.CommitAsync(ct);
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }

    /// <summary>Tệp .md chỉ dùng cho khoá chưa có đoạn nào từ hệ thống. <paramref name="chiKhoa"/> null = xét mọi khoá.</summary>
    private async Task DongBoTepMdAsync(AppDbContext db, HashSet<string>? chiKhoa, CancellationToken ct, bool trongGiaoDich = false)
    {
        var md = KhoTaiLieu.DocTepMd(kho.ThuMucMd);
        if (md.Count == 0) return;
        var thay = new Dictionary<string, List<DoanTaiLieu>>();
        foreach (var nhom in md.GroupBy(d => d.Khoa).Where(n => chiKhoa is null || chiKhoa.Contains(n.Key)))
        {
            var coNoiDung = await db.DoanChiMuc.AnyAsync(d => d.KhoaId == nhom.Key && !d.NguonKhoa.StartsWith("Md:"), ct);
            thay["Md:" + nhom.Key] = coNoiDung ? [] : nhom.ToList();
        }
        await GhiAsync(db, thay, [], ct, trongGiaoDich);
    }
}
