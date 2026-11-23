using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SuDia.Web.Services.TroLy;

namespace SuDia.Web.Data;

/// <summary>
/// Sau mỗi lần lưu thành công nội dung là nguồn của chỉ mục tìm kiếm / trợ lý AI (khoá, chủ đề, bài, mục, phần,
/// mốc thời gian, tài liệu trong kho, gói bán), báo cho DongBoChiMuc đúng đơn vị cần dựng lại.
/// Chỉ báo sau khi lưu thành công, nên không lập chỉ mục dữ liệu chưa chốt.
/// </summary>
public class TaiLieuThayDoiInterceptor(DongBoChiMuc dongBo) : SaveChangesInterceptor
{
    private sealed record Cho(List<YeuCauChiMuc> Viec, List<object> MoiThem);
    private readonly ConditionalWeakTable<DbContext, Cho> _cho = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData e, InterceptionResult<int> result)
    {
        Ghi(e.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Ghi(e.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData e, int result) { Bao(e.Context); return result; }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData e, int result, CancellationToken ct = default)
    {
        Bao(e.Context);
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData e) => _cho.Remove(e.Context!);

    public override Task SaveChangesFailedAsync(DbContextErrorEventData e, CancellationToken ct = default)
    {
        _cho.Remove(e.Context!);
        return Task.CompletedTask;
    }

    private void Ghi(DbContext? db)
    {
        if (db is null) return;
        var viec = new List<YeuCauChiMuc>();
        var moiThem = new List<object>();
        foreach (var en in db.ChangeTracker.Entries().Where(en => en.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (!LaNguon(en.Entity)) continue;
            // Thêm mới: khoá chính tự tăng chưa có, quy đổi sau khi lưu xong
            if (en.State == EntityState.Added) moiThem.Add(en.Entity);
            else viec.AddRange(QuyDoi(en));
        }
        if (viec.Count > 0 || moiThem.Count > 0) _cho.AddOrUpdate(db, new Cho(viec, moiThem));
    }

    private void Bao(DbContext? db)
    {
        if (db is null || !_cho.TryGetValue(db, out var cho)) return;
        _cho.Remove(db);
        foreach (var o in cho.MoiThem) cho.Viec.AddRange(QuyDoi(db.Entry(o)));
        if (cho.Viec.Count > 0) dongBo.Gui(cho.Viec.Distinct());
    }

    private static bool LaNguon(object o) => o is Khoa or ChuDe or Bai or Muc or Phan or MocThoiGian or TaiLieuKhoa or GoiBan or GoiBanKhoa;

    private static T Cu<T>(EntityEntry en, string ten) => (T)(en.State == EntityState.Deleted ? en.OriginalValues[ten] : en.CurrentValues[ten])!;

    private static IEnumerable<YeuCauChiMuc> QuyDoi(EntityEntry en)
    {
        switch (en.Entity)
        {
            case Khoa k: yield return new("KhoaToanBo", k.Id); break;
            case ChuDe c:
                yield return new("KhoaToanBo", Cu<string>(en, nameof(ChuDe.KhoaId)));
                break;
            case Bai b:
                yield return new("Bai", b.Id.ToString());
                yield return new("ChuDe", Cu<int>(en, nameof(Bai.ChuDeId)).ToString());
                if (en.State == EntityState.Modified && en.Property(nameof(Bai.ChuDeId)).IsModified)
                    yield return new("ChuDe", en.Property(nameof(Bai.ChuDeId)).OriginalValue!.ToString()!);
                break;
            case Muc: yield return new("Bai", Cu<int>(en, nameof(Muc.BaiId)).ToString()); break;
            case Phan: yield return new("Muc", Cu<int>(en, nameof(Phan.MucId)).ToString()); break;
            case MocThoiGian: yield return new("Phan", Cu<int>(en, nameof(MocThoiGian.PhanId)).ToString()); break;
            case TaiLieuKhoa t: yield return new("TaiLieu", t.Id.ToString()); break;
            case GoiBan or GoiBanKhoa: yield return new("Goi"); break;
        }
    }
}
