using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SuDia.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<NguoiDung>(options)
{
    public DbSet<Khoa> Khoa => Set<Khoa>();
    public DbSet<ChuDe> ChuDe => Set<ChuDe>();
    public DbSet<Bai> Bai => Set<Bai>();
    public DbSet<Muc> Muc => Set<Muc>();
    public DbSet<Phan> Phan => Set<Phan>();
    public DbSet<MocThoiGian> MocThoiGian => Set<MocThoiGian>();
    public DbSet<CauHoi> CauHoi => Set<CauHoi>();
    public DbSet<DeThi> DeThi => Set<DeThi>();
    public DbSet<DeThiCau> DeThiCau => Set<DeThiCau>();
    public DbSet<GhiDanh> GhiDanh => Set<GhiDanh>();
    public DbSet<TienDoBai> TienDoBai => Set<TienDoBai>();
    public DbSet<BaiLam> BaiLam => Set<BaiLam>();
    public DbSet<CauTraLoi> CauTraLoi => Set<CauTraLoi>();
    public DbSet<BuoiHoc> BuoiHoc => Set<BuoiHoc>();
    public DbSet<BaiTapGiao> BaiTapGiao => Set<BaiTapGiao>();
    public DbSet<BaiNopGiao> BaiNopGiao => Set<BaiNopGiao>();
    public DbSet<LuotLamKiemTra> LuotLamKiemTra => Set<LuotLamKiemTra>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Khoa>().HasOne(x => x.GiangVien).WithMany().HasForeignKey(x => x.GiangVienId).OnDelete(DeleteBehavior.SetNull);
        // Mã bài, mã đề giữ theo dữ liệu sẵn có (đường dẫn /Home/BaiHoc/7 không đổi)
        b.Entity<Bai>().Property(x => x.Id).ValueGeneratedNever();
        b.Entity<DeThi>().Property(x => x.Id).ValueGeneratedNever();
        b.Entity<DeThi>().HasOne(x => x.BaiTapGiao).WithOne(x => x.DeThi)
            .HasForeignKey<DeThi>(x => x.BaiTapGiaoId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DeThiCau>().HasKey(x => new { x.DeThiId, x.ThuTu });
        b.Entity<GhiDanh>().HasKey(x => new { x.NguoiDungId, x.KhoaId });
        b.Entity<TienDoBai>().HasKey(x => new { x.NguoiDungId, x.BaiId });
        b.Entity<BaiLam>().HasIndex(x => new { x.NguoiDungId, x.NopLuc });
        b.Entity<BuoiHoc>().HasIndex(x => x.BaiId);
        b.Entity<BaiTapGiao>().HasOne(x => x.BuoiHoc).WithMany().HasForeignKey(x => x.BuoiHocId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<BaiNopGiao>().HasIndex(x => new { x.BaiTapGiaoId, x.NguoiDungId }).IsUnique();
        b.Entity<LuotLamKiemTra>().HasKey(x => new { x.BaiTapGiaoId, x.NguoiDungId });
    }
}
