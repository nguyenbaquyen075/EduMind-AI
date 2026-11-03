using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SuDia.Web.Services;

public sealed class PayOSOptions
{
    public string ClientId { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ChecksumKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
    public int PhutHetHanLink { get; set; } = 15;
    public bool DaCauHinh => ClientId.Length > 0 && ApiKey.Length > 0 && ChecksumKey.Length > 0;
}

public record LinkThanhToan(string CheckoutUrl, string PaymentLinkId);
public record TrangThaiThanhToan(string Status, int AmountPaid);

/// <summary>
/// Gọi API payment-requests của PayOS (https://payos.vn/docs/api/).
/// Chữ ký HMAC-SHA256 bằng checksum key; dữ liệu ký sắp xếp key theo a-z dạng key=value&amp;...
/// </summary>
public sealed class PayOSService(HttpClient http, IOptions<PayOSOptions> opt, ILogger<PayOSService> log)
{
    private PayOSOptions O => opt.Value;
    public bool DaCauHinh => O.DaCauHinh;

    public async Task<LinkThanhToan> TaoLinkAsync(long orderCode, int amount, string description,
        string returnUrl, string cancelUrl, CancellationToken ct = default)
    {
        var chuKy = Ky($"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}");
        var hetHan = DateTimeOffset.UtcNow.AddMinutes(Math.Clamp(O.PhutHetHanLink, 5, 60)).ToUnixTimeSeconds();
        using var req = YeuCau(HttpMethod.Post, "/v2/payment-requests");
        // Gửi kèm Content-Length (StringContent) thay vì body chunked của JsonContent
        req.Content = new StringContent(JsonSerializer.Serialize(new { orderCode, amount, description, cancelUrl, returnUrl, expiredAt = hetHan, signature = chuKy }),
            Encoding.UTF8, "application/json");
        var data = await GuiAsync(req, ct);
        if (data.TryGetProperty("checkoutUrl", out var url) && url.GetString() is { Length: > 0 } checkout
            && data.TryGetProperty("paymentLinkId", out var id) && id.GetString() is { } linkId)
            return new(checkout, linkId);
        throw new InvalidOperationException("Cổng thanh toán PayOS trả dữ liệu chưa hợp lệ. Vui lòng thử lại sau.");
    }

    /// <summary>Trạng thái do PayOS trả: PENDING, PROCESSING, PAID, CANCELLED, EXPIRED…</summary>
    public async Task<TrangThaiThanhToan> LayTrangThaiAsync(long orderCode, CancellationToken ct = default)
    {
        using var req = YeuCau(HttpMethod.Get, $"/v2/payment-requests/{orderCode}");
        var data = await GuiAsync(req, ct);
        return new(data.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "",
            data.TryGetProperty("amountPaid", out var paid) && paid.TryGetInt32(out var soTien) ? soTien : 0);
    }

    /// <summary>Kiểm tra chữ ký webhook theo mẫu JS chính thức: key sắp xếp, null → "", nối key=value bằng &amp;.</summary>
    public bool XacMinhWebhook(JsonElement data, string? signature)
    {
        if (!DaCauHinh || data.ValueKind != JsonValueKind.Object || string.IsNullOrEmpty(signature)) return false;
        var chuoi = string.Join("&", data.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => $"{p.Name}={GiaTri(p.Value)}"));
        var tinh = Encoding.ASCII.GetBytes(Ky(chuoi));
        var nhan = Encoding.ASCII.GetBytes(signature.ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(tinh, nhan);
    }

    private static string GiaTri(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => "",
        JsonValueKind.String => v.GetString() is "null" or "undefined" ? "" : v.GetString()!,
        _ => v.GetRawText(),
    };

    private string Ky(string duLieu) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(O.ChecksumKey), Encoding.UTF8.GetBytes(duLieu))).ToLowerInvariant();

    private HttpRequestMessage YeuCau(HttpMethod method, string path)
    {
        if (!DaCauHinh) throw new InvalidOperationException("Thanh toán online chưa được cấu hình.");
        var req = new HttpRequestMessage(method, O.BaseUrl.TrimEnd('/') + path);
        req.Headers.Add("x-client-id", O.ClientId);
        req.Headers.Add("x-api-key", O.ApiKey);
        return req;
    }

    private async Task<JsonElement> GuiAsync(HttpRequestMessage req, CancellationToken ct)
    {
        try
        {
            using var res = await http.SendAsync(req, ct);
            using var json = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = json.RootElement;
            var code = root.TryGetProperty("code", out var c) ? c.GetString() : null;
            if (!res.IsSuccessStatusCode || code != "00" || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            {
                log.LogWarning("PayOS {Path} trả {Status} code={Code} desc={Desc}", req.RequestUri?.AbsolutePath, (int)res.StatusCode, code,
                    root.TryGetProperty("desc", out var d) ? d.GetString() : null);
                throw new InvalidOperationException("Cổng thanh toán PayOS chưa xử lý được yêu cầu. Vui lòng thử lại sau.");
            }
            return data.Clone();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException
            || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            log.LogWarning(ex, "Không gọi được PayOS");
            throw new InvalidOperationException("Không kết nối được cổng thanh toán PayOS. Vui lòng thử lại sau.");
        }
    }
}
