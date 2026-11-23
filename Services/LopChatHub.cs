using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SuDia.Web.Data;

namespace SuDia.Web.Services;

/// <summary>
/// Kênh thời gian thực của nhóm chat lớp. Trình duyệt kết nối kèm ?lopId=…; chỉ thành viên lớp, giảng viên của khoá và admin
/// được vào nhóm. Việc gửi / xoá tin đi qua POST (có chống giả mạo form), hub chỉ dùng để nhận tin mới.
/// </summary>
[Authorize]
public class LopChatHub(LopHocService lop) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        if (!int.TryParse(http?.Request.Query["lopId"], out var lopId)) { Context.Abort(); return; }
        var uid = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (l, _, _) = await lop.QuyenAsync(lopId, uid, Context.User.IsInRole(VaiTro.Admin), Context.User.IsInRole(VaiTro.GiangVien));
        if (l is null) { Context.Abort(); return; }
        await Groups.AddToGroupAsync(Context.ConnectionId, LopChatService.Nhom(lopId));
        await base.OnConnectedAsync();
    }
}
