using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SuDia.Web.Infrastructure;

/// <summary>
/// Lưới an toàn: thao tác bị CSDL chặn vì ràng buộc (xoá dữ liệu đang được dùng, trùng số bài/mục, giá trị sai)
/// thì báo lỗi dễ hiểu và quay lại trang trước, thay vì trang lỗi 500. Các thao tác quan trọng vẫn tự kiểm tra trước.
/// </summary>
public class LoiRangBuocCsdlFilter(ITempDataDictionaryFactory tempData, ILogger<LoiRangBuocCsdlFilter> log) : IExceptionFilter
{
    public void OnException(ExceptionContext ctx)
    {
        if (ctx.Exception is not DbUpdateException { InnerException: PostgresException pg }) return;
        var thongBao = pg.SqlState switch
        {
            PostgresErrorCodes.ForeignKeyViolation =>
                "Không thực hiện được vì dữ liệu này đang được dùng (học viên đã học, làm bài, ghi danh hoặc nằm trong gói/đề). Hãy ẩn hoặc bỏ xuất bản thay vì xoá.",
            PostgresErrorCodes.UniqueViolation => "Đã có mục trùng (ví dụ trùng số bài hoặc số mục). Hãy đổi số rồi lưu lại.",
            PostgresErrorCodes.CheckViolation => "Giá trị nhập không hợp lệ (ví dụ giá âm, số tháng bằng 0). Hãy kiểm tra lại.",
            _ => null,
        };
        if (thongBao is null) return;
        log.LogWarning("Ràng buộc CSDL chặn thao tác {Path}: {Constraint} ({SqlState})", ctx.HttpContext.Request.Path, pg.ConstraintName, pg.SqlState);

        var req = ctx.HttpContext.Request;
        var laAjax = req.Headers.XRequestedWith == "XMLHttpRequest" || req.Headers.Accept.ToString().Contains("application/json")
            || (req.ContentType?.Contains("application/json") ?? false);
        if (laAjax)
            ctx.Result = new ConflictObjectResult(new { loi = thongBao });
        else
        {
            tempData.GetTempData(ctx.HttpContext)["Loi"] = thongBao;
            var quayLai = req.Headers.Referer.ToString();
            // Không có trang trước (trình duyệt chặn Referer): về trang chính của khu vực đang dùng
            var veDau = req.Path.StartsWithSegments("/GiangVien") ? "/GiangVien" : req.Path.StartsWithSegments("/Admin") ? "/Admin" : "/";
            ctx.Result = Uri.TryCreate(quayLai, UriKind.Absolute, out var u) && u.Host == req.Host.Host
                ? new RedirectResult(u.PathAndQuery) : new RedirectResult(veDau);
        }
        ctx.ExceptionHandled = true;
    }
}
