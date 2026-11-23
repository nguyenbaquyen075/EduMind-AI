using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Data;
using SuDia.Web.Models;

namespace SuDia.Web.Services;

/// <summary>Nhóm chat của lớp: lưu tin, đọc lịch sử, xoá tin và phát tin mới tới những người đang mở lớp (SignalR).</summary>
public class LopChatService(AppDbContext db, IHubContext<LopChatHub> hub, TinThongBaoService tin, LopHocService lop)
{
    public static string Nhom(int lopId) => $"lop-{lopId}";

    private async Task<string?> GiangVienIdAsync(int lopId, CancellationToken ct) =>
        await db.Lop.Where(l => l.Id == lopId).Select(l => l.Khoa!.GiangVienId).FirstOrDefaultAsync(ct);

    private static TinNhanDto Dto(long id, string uid, string ten, string? gv, string nd, DateTime gio) => new(id, uid, ten, uid == gv, nd, gio);

    public async Task<(List<TinNhanDto> Tin, bool CoTinCu)> LichSuAsync(int lopId, long? truoc, CancellationToken ct = default)
    {
        var gv = await GiangVienIdAsync(lopId, ct);
        var q = db.TinNhanLop.AsNoTracking().Where(t => t.LopId == lopId);
        if (truoc is long t0) q = q.Where(t => t.Id < t0);
        var ds = await q.OrderByDescending(t => t.Id).Take(GioiHanChat.SoTinMoiLanTai + 1)
            .Select(t => new { t.Id, t.NguoiDungId, t.NguoiDung!.HoTen, t.NoiDung, t.NgayTao }).ToListAsync(ct);
        var coCu = ds.Count > GioiHanChat.SoTinMoiLanTai;
        return (ds.Take(GioiHanChat.SoTinMoiLanTai).OrderBy(t => t.Id).Select(t => Dto(t.Id, t.NguoiDungId, t.HoTen, gv, t.NoiDung, t.NgayTao)).ToList(), coCu);
    }

    /// <summary>Gửi tin. Người gọi đã kiểm tra quyền vào lớp. Trả (tin, null) hoặc (null, lý do).</summary>
    public async Task<(TinNhanDto? Tin, string? Loi)> GuiAsync(Lop l, string uid, bool quanLy, string? noiDung, CancellationToken ct = default)
    {
        noiDung = (noiDung ?? "").Replace("\r\n", "\n").Trim();
        if (noiDung.Length == 0) return (null, "Nhập nội dung tin nhắn.");
        if (noiDung.Length > GioiHanChat.NoiDung) return (null, $"Tin nhắn tối đa {GioiHanChat.NoiDung:N0} ký tự.");
        var tu = DateTime.UtcNow.AddMinutes(-1);
        if (await db.TinNhanLop.CountAsync(t => t.NguoiDungId == uid && t.NgayTao >= tu, ct) >= GioiHanChat.TinMoiPhut)
            return (null, "Bạn nhắn quá nhanh. Đợi một chút rồi gửi tiếp nhé.");
        var m = new TinNhanLop { LopId = l.Id, NguoiDungId = uid, NoiDung = noiDung };
        db.TinNhanLop.Add(m);
        await db.SaveChangesAsync(ct);
        var ten = await db.Users.Where(u => u.Id == uid).Select(u => u.HoTen).FirstAsync(ct);
        var gv = l.Khoa?.GiangVienId ?? await GiangVienIdAsync(l.Id, ct);
        var dto = Dto(m.Id, uid, ten, gv, noiDung, m.NgayTao);
        await hub.Clients.Group(Nhom(l.Id)).SendAsync("NhanTin", dto, ct);

        // Báo những người không mở lớp: mỗi lớp tối đa một thông báo mỗi giờ theo từng chiều (giảng viên nhắn / học viên nhắn)
        var gio = $"{DateTime.UtcNow:yyyyMMddHH}";
        if (quanLy)
            await tin.GuiAsync(await db.LopHocVien.Where(x => x.LopId == l.Id && x.NguoiDungId != uid).Select(x => x.NguoiDungId).ToListAsync(ct),
                LoaiTin.ChatGiangVien, $"Tin nhắn mới trong lớp {l.Ten}", $"{ten}: {Cat(noiDung)}", $"/Lop/{l.Id}", $"chatgv:{l.Id}:{gio}", ct);
        else if (gv != uid)
            await tin.GuiAsync([gv], LoaiTin.ChatHocVien, $"Tin nhắn mới trong lớp {l.Ten}", $"{ten}: {Cat(noiDung)}", $"/Lop/{l.Id}", $"chathv:{l.Id}:{gio}", ct);
        return (dto, null);
    }

    public async Task<bool> XoaAsync(Lop l, long tinId, string uid, bool quanLy, CancellationToken ct = default)
    {
        var m = await db.TinNhanLop.FirstOrDefaultAsync(t => t.Id == tinId && t.LopId == l.Id, ct);
        if (m is null || !(quanLy || m.NguoiDungId == uid)) return false;
        db.TinNhanLop.Remove(m);
        await db.SaveChangesAsync(ct);
        await hub.Clients.Group(Nhom(l.Id)).SendAsync("XoaTin", tinId, ct);
        return true;
    }

    private static string Cat(string s) => s.Length <= 80 ? s : s[..79] + "…";
}
