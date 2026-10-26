using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Services;

namespace SuDia.Web.Data;

/// <summary>
/// Chạy lúc khởi động: cập nhật database, tạo vai trò, nạp nội dung mẫu (lần đầu)
/// và — chỉ ở Development — tạo 3 tài khoản dùng thử.
/// </summary>
public static class KhoiTaoDuLieu
{
    public static async Task ChayAsync(IServiceProvider sp, bool laDev, string matKhauMau)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var r in VaiTro.TatCa)
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

        if (!await db.Khoa.AnyAsync()) await NapNoiDungMauAsync(db);
        if (laDev) await TaoTaiKhoanMauAsync(scope.ServiceProvider, db, matKhauMau);
    }

    // Lấy đúng dữ liệu mẫu đang hiển thị (DuLieuMauService) để chuyển sang database
    private static async Task NapNoiDungMauAsync(AppDbContext db)
    {
        var thuTuKhoa = 0;
        foreach (var k in DuLieuMauService.DanhMuc)
        {
            var khoa = new Khoa { Id = k.Id, Ten = k.Ten, KyHieu = k.KyHieu, MoTa = k.MoTa, Anh = k.Anh, ThuTu = thuTuKhoa++ };
            var thuTuChuDe = 0;
            var baiDau = true;
            foreach (var c in k.Chuong)
            {
                var cd = new ChuDe { Ten = c.Ten, ThuTu = thuTuChuDe++ };
                var thuTuBai = 0;
                foreach (var b in c.Bai)
                {
                    var bai = new Bai { Id = b.Id, So = b.So, Ten = b.Ten, SoMuc = b.SoMuc, SoPhut = b.SoPhut, ThuTu = thuTuBai++, HocThu = baiDau };
                    baiDau = false;
                    foreach (var m in DuLieuMauService.NoiDungMau.GetValueOrDefault(b.Id) ?? [])
                    {
                        var muc = new Muc { So = m.So, Ten = m.Ten, TieuDe = m.TieuDe, SoPhut = m.SoPhut };
                        var thuTuPhan = 0;
                        foreach (var p in m.Phan)
                            muc.Phan.Add(new Phan
                            {
                                ThuTu = thuTuPhan++, MaNeo = p.Id, TieuDe = p.TieuDe, DoanVan = [.. p.DoanVan], LaGhiNho = p.LaGhiNho,
                                Moc = (p.Moc ?? []).Select((x, i) => new MocThoiGian { ThuTu = i, Ten = x.Ten, ThoiGian = x.ThoiGian, NoiDung = x.NoiDung }).ToList(),
                            });
                        bai.Muc.Add(muc);
                    }
                    var thuTuCau = 0;
                    foreach (var q in DuLieuMauService.NganHangMau.GetValueOrDefault(b.Id) ?? [])
                        bai.CauHoi.Add(new CauHoi
                        {
                            ThuTu = thuTuCau++, NoiDung = q.NoiDung, PhuongAn = q.PhuongAn.Select(x => x.NoiDung).ToList(),
                            DapAnDung = q.DapAnDung, GiaiThich = q.GiaiThich, Muc = q.Muc, NguonHienThi = q.NguonHienThi,
                        });
                    cd.Bai.Add(bai);
                }
                khoa.ChuDe.Add(cd);
            }
            db.Khoa.Add(khoa);
        }
        await db.SaveChangesAsync();

        // Đề thi thử: Lịch sử 1001–1003 ghép từ ngân hàng câu hỏi; Địa lí 2001 chưa có câu
        var cauTheoNoiDung = await db.CauHoi.ToDictionaryAsync(x => x.NoiDung, x => x.Id);
        for (var ma = 1001; ma <= 1003; ma++)
            db.DeThi.Add(new DeThi
            {
                Id = ma, KhoaId = "lich-su-12", Ten = $"Đề thi thử số {ma % 1000}", SoPhut = 50,
                Cau = DuLieuMauService.TaoDeThi(ma).Select((q, i) => new DeThiCau { CauHoiId = cauTheoNoiDung[q.NoiDung], ThuTu = i }).ToList(),
            });
        db.DeThi.Add(new DeThi { Id = 2001, KhoaId = "dia-li-12", Ten = "Đề thi thử số 1", SoPhut = 50 });
        await db.SaveChangesAsync();
    }

    private static async Task TaoTaiKhoanMauAsync(IServiceProvider sp, AppDbContext db, string matKhau)
    {
        var users = sp.GetRequiredService<UserManager<NguoiDung>>();

        async Task<NguoiDung> Tao(string email, string hoTen, string vaiTro)
        {
            var u = await users.FindByEmailAsync(email);
            if (u is null)
            {
                u = new NguoiDung { UserName = email, Email = email, EmailConfirmed = true, HoTen = hoTen };
                var kq = await users.CreateAsync(u, matKhau);
                if (!kq.Succeeded) throw new InvalidOperationException(string.Join("; ", kq.Errors.Select(e => e.Description)));
                await users.AddToRoleAsync(u, vaiTro);
            }
            return u;
        }

        await Tao("admin@sudia.vn", "Quản trị viên", VaiTro.Admin);
        var gv = await Tao("giangvien@sudia.vn", "Giảng viên Sử Địa", VaiTro.GiangVien);
        var hv = await Tao("hocvien@sudia.vn", "Học viên mẫu", VaiTro.HocVien);

        foreach (var k in await db.Khoa.Where(x => x.GiangVienId == null).ToListAsync()) k.GiangVienId = gv.Id;

        if (!await db.GhiDanh.AnyAsync(x => x.NguoiDungId == hv.Id))
        {
            db.GhiDanh.Add(new GhiDanh { NguoiDungId = hv.Id, KhoaId = "lich-su-12" });
            for (var so = 1; so <= 7; so++)
                db.TienDoBai.Add(new TienDoBai { NguoiDungId = hv.Id, BaiId = so, TrangThai = so < 7 ? TrangThaiHoc.DaHoc : TrangThaiHoc.DangHoc });
        }
        await db.SaveChangesAsync();
    }
}
