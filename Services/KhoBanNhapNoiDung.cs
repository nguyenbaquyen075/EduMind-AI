using Microsoft.Extensions.Caching.Memory;
using SuDia.Web.Models;

namespace SuDia.Web.Services;

public record BanXemThuNoiDung(BanNhapNoiDungAiForm Form, bool TheoVanBan);

/// <summary>Bản nháp riêng theo tài khoản, dùng để chuyển POST sang GET mà không đưa văn bản dài vào cookie.</summary>
public sealed class KhoBanNhapNoiDung(IMemoryCache cache)
{
    public string Luu<T>(string nguoiDungId, T duLieu) where T : class
    {
        var id = Guid.NewGuid().ToString("N");
        cache.Set($"soan:{nguoiDungId}:{id}", duLieu, TimeSpan.FromHours(2));
        return id;
    }

    public void Xoa(string nguoiDungId, string? id)
    {
        if (Guid.TryParse(id, out var ma)) cache.Remove($"soan:{nguoiDungId}:{ma:N}");
    }

    public T? Doc<T>(string nguoiDungId, string? id) where T : class
    {
        if (!Guid.TryParse(id, out var ma)) return null;
        return cache.TryGetValue<T>($"soan:{nguoiDungId}:{ma:N}", out var duLieu) ? duLieu : null;
    }
}
