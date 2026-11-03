using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Services;

namespace SuDia.Web.Controllers;

// PayOS gọi khi có tiền vào. Khai báo URL https://<tên-miền>/api/payos/webhook trong kênh thanh toán PayOS.
[Route("api/payos")]
[AllowAnonymous, IgnoreAntiforgeryToken]
public class PayOSWebhookController(AppDbContext db, PayOSService payos, DonHangService donHang,
    ILogger<PayOSWebhookController> log) : Controller
{
    [HttpPost("webhook"), RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> Webhook([FromBody] JsonElement body, CancellationToken ct)
    {
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("data", out var data)
            || !payos.XacMinhWebhook(data, body.TryGetProperty("signature", out var s) ? s.GetString() : null))
        {
            log.LogWarning("Webhook PayOS sai chữ ký hoặc sai định dạng");
            return BadRequest(new { success = false });
        }
        // PayOS gửi thử khi lưu URL webhook (orderCode mẫu không có trong hệ thống): vẫn trả 2xx.
        if (data.TryGetProperty("code", out var code) && code.GetString() == "00"
            && data.TryGetProperty("orderCode", out var oc) && oc.TryGetInt64(out var ma)
            && data.TryGetProperty("amount", out var am) && am.TryGetInt32(out var soTien))
        {
            var don = await db.DonHang.AsNoTracking().FirstOrDefaultAsync(d => d.MaThanhToan == ma, ct);
            if (don is null) log.LogInformation("Webhook PayOS cho đơn không có trong hệ thống: {Ma}", ma);
            else if (soTien < don.SoTien) log.LogWarning("Đơn {Ma} nhận {Nhan} nhỏ hơn giá {Gia}", ma, soTien, don.SoTien);
            else await donHang.XacNhanDaThanhToanAsync(don.Id, data.TryGetProperty("reference", out var r) ? r.GetString() : null, ct);
        }
        return Ok(new { success = true });
    }
}
