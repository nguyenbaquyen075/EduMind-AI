using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SuDia.Web.Models;
using static SuDia.Web.Data.DuLieuMauNoiDung;

namespace SuDia.Web.Data;

/// <summary>
/// Dữ liệu mẫu đầy đủ cho Development: xoá toàn bộ nội dung khoá học và dữ liệu học tập hiện có,
/// rồi tạo lại bài học, câu hỏi, đề thi, học viên, ghi danh, tiến độ, bài làm, bài tập, bài nộp, lịch học và đơn hàng.
/// Chạy: dotnet run -- --du-lieu-mau --xac-nhan-xoa-du-lieu-mau
/// </summary>
public static class DuLieuMauDayDu
{
    private const string DuoiEmail = "@sudia.vn";

    public static async Task TaoAsync(IServiceProvider sp, string matKhau)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<NguoiDung>>();
        var rd = new Random(2026);
        var now = DateTime.UtcNow;

        await XoaDuLieuCuAsync(db, users);

        // ---------------------------------------------------------------- Nội dung khoá học
        var gv = await users.FindByEmailAsync("giangvien" + DuoiEmail) ?? throw new InvalidOperationException("Chưa có tài khoản giảng viên mẫu.");
        var cauTheoBai = new Dictionary<int, List<CauHoi>>();
        var baiTheoKhoa = new Dictionary<string, List<(Bai Bai, BaiMau Mau)>>();
        foreach (var km in new[] { LichSu, DiaLi })
        {
            var khoa = await db.Khoa.FirstAsync(k => k.Id == km.Id);
            khoa.MucTieu = km.MucTieu; khoa.DoiTuong = km.DoiTuong; khoa.GiangVienId = gv.Id; khoa.HienThi = true;
            var dsBai = new List<(Bai, BaiMau)>();
            var dau = true;
            for (var c = 0; c < km.ChuDe.Length; c++)
            {
                var cd = new ChuDe { KhoaId = km.Id, Ten = km.ChuDe[c].Ten, ThuTu = c };
                for (var b = 0; b < km.ChuDe[c].Bai.Length; b++)
                {
                    var bm = km.ChuDe[c].Bai[b];
                    var bai = new Bai
                    {
                        Id = (km.Id.StartsWith("dia") ? 100 : 0) + bm.So, So = bm.So, Ten = bm.Ten, SoPhut = bm.SoPhut, SoMuc = bm.Muc.Length,
                        ThuTu = b, HocThu = dau, DaXuatBan = true,
                    };
                    dau = false;
                    for (var m = 0; m < bm.Muc.Length; m++)
                    {
                        var mm = bm.Muc[m];
                        var muc = new Muc { So = m + 1, Ten = mm.Ten, TieuDe = mm.Ten, SoPhut = Math.Max(5, bm.SoPhut / bm.Muc.Length) };
                        var phan = new Phan { ThuTu = 0, MaNeo = "noi-dung", DoanVan = [.. mm.DoanVan],
                            Moc = (mm.Moc ?? []).Select((x, i) => new MocThoiGian { ThuTu = i, Ten = x.Ten, ThoiGian = x.ThoiGian, NoiDung = x.NoiDung }).ToList() };
                        muc.Phan.Add(phan);
                        if (mm.GhiNho is not null) muc.Phan.Add(new Phan { ThuTu = 1, MaNeo = "ghi-nho", TieuDe = "Ghi nhớ", DoanVan = [mm.GhiNho], LaGhiNho = true });
                        bai.Muc.Add(muc);
                    }
                    var cau = bm.Cau.Select((q, i) => new CauHoi
                    {
                        ThuTu = i, NoiDung = q.NoiDung, PhuongAn = [q.A, q.B, q.C, q.D], DapAnDung = q.Dung.ToString(),
                        GiaiThich = q.GiaiThich, Muc = 1, NguonHienThi = $"Bài {bm.So}. {bm.Ten}",
                    }).ToList();
                    bai.CauHoi.AddRange(cau);
                    cauTheoBai[bai.Id] = cau;
                    cd.Bai.Add(bai);
                    dsBai.Add((bai, bm));
                }
                db.ChuDe.Add(cd);
            }
            baiTheoKhoa[km.Id] = dsBai;
        }
        await db.SaveChangesAsync();

        // Đề thi thử ghép từ ngân hàng câu hỏi của khoá
        var deTheoKhoa = new Dictionary<string, List<(DeThi De, List<CauHoi> Cau)>>();
        foreach (var (khoaId, ma, soDe, soCau) in new[] { ("lich-su-12", 1001, 3, 20), ("dia-li-12", 2001, 2, 15) })
        {
            var nganHang = baiTheoKhoa[khoaId].SelectMany(x => cauTheoBai[x.Bai.Id]).ToList();
            deTheoKhoa[khoaId] = [];
            for (var i = 0; i < soDe; i++)
            {
                var cau = nganHang.Skip(i * 7 % nganHang.Count).Concat(nganHang).Take(soCau).ToList();
                var de = new DeThi { Id = ma + i, KhoaId = khoaId, Ten = $"Đề thi thử số {i + 1}", SoPhut = 50,
                    Cau = cau.Select((c, j) => new DeThiCau { CauHoiId = c.Id, ThuTu = j }).ToList() };
                db.DeThi.Add(de);
                deTheoKhoa[khoaId].Add((de, cau));
            }
        }
        await db.SaveChangesAsync();

        // ---------------------------------------------------------------- Lịch học online và bài tập giao
        var buoiMau = new Dictionary<string, (int So, int Ngay)[]>
        {
            ["lich-su-12"] = [(1, -21), (4, -11), (6, -4), (7, 1), (8, 4), (10, 9)],
            ["dia-li-12"] = [(1, -16), (2, -6), (6, 2), (30, 5)],
        };
        var buoiTheoBai = new Dictionary<int, BuoiHoc>();
        var maMeet = 0;
        foreach (var (khoaId, ds) in buoiMau)
            foreach (var (so, ngay) in ds)
            {
                var bai = baiTheoKhoa[khoaId].First(x => x.Mau.So == so).Bai;
                // 19:30 giờ Việt Nam = 12:30 UTC
                var buoi = new BuoiHoc { BaiId = bai.Id, ThoiGian = now.Date.AddDays(ngay).AddHours(12.5), Link = $"https://meet.google.com/sdi-{1000 + ++maMeet:0000}-abc" };
                db.BuoiHoc.Add(buoi);
                buoiTheoBai[bai.Id] = buoi;
            }
        await db.SaveChangesAsync();

        var giaoMau = new Dictionary<string, (int So, int HanNgay, bool DaGiao)[]>
        {
            ["lich-su-12"] = [(1, -18, true), (4, -9, true), (6, -2, true), (7, 3, true), (8, 8, true), (10, 14, false)],
            ["dia-li-12"] = [(1, -14, true), (2, -4, true), (6, 2, true), (30, 6, true)],
        };
        var baiTap = new List<(BaiTapGiao Bt, string KhoaId, List<CauBaiTap> Cau)>();
        foreach (var (khoaId, ds) in giaoMau)
            foreach (var (so, hanNgay, daGiao) in ds)
            {
                var (bai, bm) = baiTheoKhoa[khoaId].First(x => x.Mau.So == so);
                var cauTn = cauTheoBai[bai.Id].Take(bm.TuLuan is null ? 5 : 4).Select(q => new CauBaiTap
                {
                    Id = $"tn{q.Id}", Kieu = "TN", NoiDung = q.NoiDung, Diem = bm.TuLuan is null ? 2 : 1.5m, PhuongAn = [.. q.PhuongAn],
                    DapAn = "ABCD".IndexOf(q.DapAnDung, StringComparison.Ordinal), LoiGiai = q.GiaiThich,
                }).ToList();
                if (bm.TuLuan is not null) cauTn.Add(new CauBaiTap { Id = $"tl{bai.Id}", Kieu = "TL", NoiDung = bm.TuLuan, Diem = 4, LoiGiai = bm.HuongDanCham ?? "" });
                var han = now.Date.AddDays(hanNgay).AddHours(16); // 23:00 giờ Việt Nam
                var bt = new BaiTapGiao
                {
                    BaiId = bai.Id, BuoiHocId = buoiTheoBai.GetValueOrDefault(bai.Id)?.Id, Ten = $"Bài tập Bài {so}. {bm.Ten}",
                    HuongDan = "Làm bài trước hạn nộp. Phần tự luận viết ngắn gọn, đủ ý.", HanNop = han, DaGiao = daGiao,
                    NoiDungJson = NoiDungBaiTap.Viet(cauTn), NgayTao = hanNgay < 0 ? han.AddDays(-7) : now.AddDays(-2),
                };
                db.BaiTapGiao.Add(bt);
                baiTap.Add((bt, khoaId, cauTn));
            }
        await db.SaveChangesAsync();

        // ---------------------------------------------------------------- Gói học có giá
        foreach (var g in await db.GoiBan.ToListAsync())
        {
            g.Gia ??= g.KhoaIds.Count > 1 ? 699_000 : 399_000;
            g.SoThang = 12; g.DangBan = true;
        }
        await db.SaveChangesAsync();
        var goi = await db.GoiBan.AsNoTracking().ToListAsync();
        GoiBan GoiCho(params string[] khoa) => goi.First(g => g.KhoaIds.Count == khoa.Length && khoa.All(g.KhoaIds.Contains));

        // ---------------------------------------------------------------- Học viên
        var hocVien = new List<(NguoiDung Nd, double Gioi, DateTime LanCuoi, List<(string Khoa, DateTime Tu, DateTime? Den, bool Mua)> Ghi)>();
        for (var i = 0; i < HocVien.Length; i++)
        {
            var batDau = now.AddDays(-(15 + i * 4 % 45)).AddHours(-rd.Next(0, 12));
            var nd = new NguoiDung { UserName = $"hv{i + 1:00}{DuoiEmail}", Email = $"hv{i + 1:00}{DuoiEmail}", EmailConfirmed = true, HoTen = HocVien[i], NgayTao = batDau.AddMinutes(-30) };
            var kq = await users.CreateAsync(nd, matKhau);
            if (!kq.Succeeded) throw new InvalidOperationException(string.Join("; ", kq.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(nd, VaiTro.HocVien);

            var khoa = (i % 3) switch { 0 => new[] { "lich-su-12", "dia-li-12" }, 1 => ["lich-su-12"], _ => ["dia-li-12"] };
            DateTime? den = i switch { 4 => now.AddDays(5), 11 => now.AddDays(3), 1 => null, _ => batDau.AddMonths(12) };
            if (i == 9) { batDau = now.AddDays(-375); den = now.AddDays(-10); }
            var nghi = i is 5 or 9 or 13;
            var lanCuoi = nghi ? now.AddDays(-(9 + i % 10)) : now.AddDays(-(i % 4)).AddHours(-rd.Next(1, 10));
            hocVien.Add((nd, 0.4 + i * 37 % 55 / 100.0, lanCuoi, khoa.Select(k => (k, batDau, den, i != 1)).ToList()));
        }
        var hvMau = await users.FindByEmailAsync("hocvien" + DuoiEmail);
        if (hvMau is not null)
        {
            var tu = now.AddDays(-40);
            hocVien.Add((hvMau, 0.72, now.AddHours(-3), [("lich-su-12", tu, null, false), ("dia-li-12", tu.AddDays(10), now.AddDays(6), true)]));
        }

        var maDon = 2_611_000_000L;
        foreach (var (nd, gioi, lanCuoi, ghi) in hocVien)
        {
            foreach (var (khoaId, tu, den, _) in ghi)
                db.GhiDanh.Add(new GhiDanh { NguoiDungId = nd.Id, KhoaId = khoaId, HanDen = den, NgayTao = tu });

            // Đơn hàng đã thanh toán tương ứng với ghi danh (mua cả hai khoá cùng lúc thì là gói trọn bộ)
            var mua = ghi.Where(g => g.Mua).GroupBy(g => g.Tu);
            foreach (var nhom in mua)
            {
                var g = GoiCho(nhom.Select(x => x.Khoa).ToArray());
                db.DonHang.Add(new DonHang
                {
                    MaThanhToan = ++maDon, NguoiDungId = nd.Id, GoiBanId = g.Id, TenGoi = g.Ten, SoTien = g.Gia!.Value, SoThang = g.SoThang,
                    KhoaIds = [.. g.KhoaIds], TrangThai = TrangThaiDon.DaThanhToan, MaGiaoDich = $"FT{maDon % 1_000_000:000000}{rd.Next(1000, 9999)}",
                    NgayTao = nhom.Key.AddMinutes(-4), NgayThanhToan = nhom.Key,
                });
            }

            // Tiến độ học và luyện tập theo từng khoá
            foreach (var (khoaId, tu, den, _) in ghi)
            {
                var dsBai = baiTheoKhoa[khoaId];
                var tiLe = Math.Clamp(gioi * 0.9 + rd.NextDouble() * 0.2 - (lanCuoi < now.AddDays(-7) ? 0.25 : 0), 0.05, 1);
                var daHoc = (int)Math.Round(dsBai.Count * tiLe);
                var ketThuc = den is DateTime d && d < lanCuoi ? d : lanCuoi;
                for (var b = 0; b < dsBai.Count && b <= daHoc; b++)
                {
                    var bai = dsBai[b].Bai;
                    var luc = tu.AddTicks((ketThuc - tu).Ticks * (b + 1) / (daHoc + 1));
                    var xong = b < daHoc;
                    db.TienDoBai.Add(new TienDoBai { NguoiDungId = nd.Id, BaiId = bai.Id, TrangThai = xong ? TrangThaiHoc.DaHoc : TrangThaiHoc.DangHoc, CapNhat = luc });
                    if (xong && rd.NextDouble() < 0.75)
                        db.BaiLam.Add(TaoBaiLam(nd.Id, bai.Id, false, $"Luyện tập · Bài {bai.So}", cauTheoBai[bai.Id], gioi, luc.AddMinutes(25), rd));
                }
                // Thi thử khi đã học quá nửa khoá
                if (daHoc * 2 > dsBai.Count)
                    foreach (var (de, cau) in deTheoKhoa[khoaId].Take(1 + rd.Next(2)))
                        db.BaiLam.Add(TaoBaiLam(nd.Id, de.Id, true, de.Ten, cau, gioi - 0.05, ketThuc.AddDays(-rd.Next(0, 4)).AddHours(-rd.Next(1, 6)), rd));
            }

            // Bài nộp cho các bài tập đã giao của khoá đang học
            foreach (var (bt, khoaId, cau) in baiTap.Where(x => x.Bt.DaGiao))
            {
                var gd = ghi.FirstOrDefault(g => g.Khoa == khoaId);
                if (gd.Khoa is null || gd.Tu > bt.NgayTao || (gd.Den is DateTime h && h < bt.NgayTao)) continue;
                var quaHan = bt.HanNop < now;
                var xacSuat = quaHan ? 0.35 + gioi * 0.6 : 0.3 + gioi * 0.3;
                if (lanCuoi < now.AddDays(-7)) xacSuat *= 0.5;
                if (rd.NextDouble() > xacSuat) continue;

                var nop = quaHan && rd.NextDouble() < 0.12 ? bt.HanNop!.Value.AddHours(rd.Next(2, 30)) : bt.HanNop!.Value.AddHours(-rd.Next(1, 72));
                if (nop > now) nop = now.AddHours(-rd.Next(1, 20));
                if (nop < bt.NgayTao) nop = bt.NgayTao.AddHours(rd.Next(2, 20));
                var traLoi = new Dictionary<string, string>();
                decimal diemTuDong = 0;
                foreach (var c in cau)
                {
                    if (c.Kieu == "TL") { traLoi[c.Id] = "Bài làm tự luận của học viên: trình bày các ý chính theo yêu cầu đề bài."; continue; }
                    var dung = rd.NextDouble() < gioi;
                    var chon = dung ? c.DapAn!.Value : (c.DapAn!.Value + 1 + rd.Next(3)) % 4;
                    traLoi[c.Id] = chon.ToString();
                    if (dung) diemTuDong += c.Diem;
                }
                var tuLuan = cau.FirstOrDefault(c => c.Kieu == "TL");
                decimal? diemTuLuan = tuLuan is null ? 0
                    : bt.HanNop < now.AddDays(-3) || rd.NextDouble() < 0.3
                        ? Math.Round((decimal)Math.Clamp(gioi + rd.NextDouble() * 0.2 - 0.1, 0.2, 1) * tuLuan.Diem * 2, MidpointRounding.AwayFromZero) / 2
                        : null;
                db.BaiNopGiao.Add(new BaiNopGiao { BaiTapGiaoId = bt.Id, NguoiDungId = nd.Id, TraLoiJson = NoiDungBaiTap.Viet(traLoi),
                    DiemTuDong = diemTuDong, DiemTuLuan = diemTuLuan, NopLuc = nop });
            }
        }

        // Một vài đơn đang chờ thanh toán và đã huỷ
        foreach (var (i, trangThai, phut) in new[] { (2, TrangThaiDon.ChoThanhToan, 150), (7, TrangThaiDon.ChoThanhToan, 12), (10, TrangThaiDon.ChoThanhToan, 55), (3, TrangThaiDon.DaHuy, 60 * 24 * 6), (8, TrangThaiDon.DaHuy, 60 * 24 * 13) })
        {
            var g = GoiCho("lich-su-12", "dia-li-12");
            db.DonHang.Add(new DonHang { MaThanhToan = ++maDon, NguoiDungId = hocVien[i].Nd.Id, GoiBanId = g.Id, TenGoi = g.Ten, SoTien = g.Gia!.Value,
                SoThang = g.SoThang, KhoaIds = [.. g.KhoaIds], TrangThai = trangThai, NgayTao = now.AddMinutes(-phut) });
        }
        await db.SaveChangesAsync();
    }

    private static BaiLam TaoBaiLam(string uid, int maDe, bool laDeThi, string ten, List<CauHoi> cau, double gioi, DateTime luc, Random rd)
    {
        var bl = new BaiLam { NguoiDungId = uid, MaDe = maDe, LaDeThi = laDeThi, TenDe = ten, SoCau = cau.Count, NopLuc = luc };
        for (var i = 0; i < cau.Count; i++)
        {
            var dung = rd.NextDouble() < gioi;
            var dapAn = cau[i].DapAnDung[0];
            var chon = dung ? dapAn : "ABCD".Where(x => x != dapAn).ElementAt(rd.Next(3));
            bl.TraLoi.Add(new CauTraLoi { CauHoiId = cau[i].Id, ThuTu = i + 1, DaChon = chon.ToString(), Dung = dung });
            if (dung) bl.SoDung++;
        }
        return bl;
    }

    private static async Task XoaDuLieuCuAsync(AppDbContext db, UserManager<NguoiDung> users)
    {
        await db.CauTraLoi.ExecuteDeleteAsync();
        await db.BaiLam.ExecuteDeleteAsync();
        await db.LuotLamKiemTra.ExecuteDeleteAsync();
        await db.BaiNopGiao.ExecuteDeleteAsync();
        await db.DeThiCau.ExecuteDeleteAsync();
        await db.DeThi.ExecuteDeleteAsync();
        await db.BaiTapGiao.ExecuteDeleteAsync();
        await db.BuoiHoc.ExecuteDeleteAsync();
        await db.TienDoBai.ExecuteDeleteAsync();
        await db.CauHoi.ExecuteDeleteAsync();
        await db.MocThoiGian.ExecuteDeleteAsync();
        await db.Phan.ExecuteDeleteAsync();
        await db.Muc.ExecuteDeleteAsync();
        await db.Bai.ExecuteDeleteAsync();
        await db.ChuDe.ExecuteDeleteAsync();
        await db.GhiDanh.ExecuteDeleteAsync();
        await db.DonHang.ExecuteDeleteAsync();
        foreach (var u in await db.Users.Where(u => u.Email!.StartsWith("hv") && u.Email.EndsWith(DuoiEmail)).ToListAsync())
            await users.DeleteAsync(u);
    }
}
