using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SuDia.Web.Data;

/// <summary>
/// Quy tắc xoá (xem docs/co-so-du-lieu.md):
/// - Dữ liệu của học viên (ghi danh, tiến độ, bài làm, bài nộp, câu trả lời) không bao giờ bị xoá dây chuyền
///   khi xoá nội dung: khoá ngoại để RESTRICT, web chặn xoá và gợi ý ẩn / bỏ xuất bản.
/// - Nội dung thuần (chủ đề → bài → mục → phần → mốc, buổi học) vẫn xoá dây chuyền khi chưa có ai dùng.
/// - Đơn hàng giữ bản chép (tên gói, giá, khoá) nên xoá gói không ảnh hưởng đơn cũ.
/// </summary>
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
    public DbSet<TinThongBao> TinThongBao => Set<TinThongBao>();
    public DbSet<Lop> Lop => Set<Lop>();
    public DbSet<LopHocVien> LopHocVien => Set<LopHocVien>();
    public DbSet<BaiDang> BaiDang => Set<BaiDang>();
    public DbSet<TraLoiBaiDang> TraLoiBaiDang => Set<TraLoiBaiDang>();
    public DbSet<BaiTapGiao> BaiTapGiao => Set<BaiTapGiao>();
    public DbSet<BaiNopGiao> BaiNopGiao => Set<BaiNopGiao>();
    public DbSet<LuotLamKiemTra> LuotLamKiemTra => Set<LuotLamKiemTra>();
    public DbSet<TaiLieuKhoa> TaiLieuKhoa => Set<TaiLieuKhoa>();
    public DbSet<DoanChiMuc> DoanChiMuc => Set<DoanChiMuc>();
    public DbSet<GoiBan> GoiBan => Set<GoiBan>();
    public DbSet<GoiBanKhoa> GoiBanKhoa => Set<GoiBanKhoa>();
    public DbSet<DonHang> DonHang => Set<DonHang>();
    public DbSet<ThongBao> ThongBao => Set<ThongBao>();
    public DbSet<NhatKyDuyet> NhatKyDuyet => Set<NhatKyDuyet>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ------------------------------------------------ Nội dung khoá học
        b.Entity<Khoa>().HasOne(x => x.GiangVien).WithMany().HasForeignKey(x => x.GiangVienId).OnDelete(DeleteBehavior.SetNull);
        // Mã bài và mã đề lấy chung một dãy số tự tăng nên không bao giờ trùng nhau: đường dẫn /Home/LamCauHoi/{mã}
        // nhận ra đề hay bài theo việc mã có trong bảng DeThi. Dữ liệu mẫu chèn mã cũ nên sau khi nạp phải DongBoMaTuTang.
        b.HasSequence<int>(DayMaBaiDe);
        b.Entity<Bai>().Property(x => x.Id).ValueGeneratedOnAdd().HasDefaultValueSql($"nextval('\"{DayMaBaiDe}\"')");
        b.Entity<DeThi>().Property(x => x.Id).ValueGeneratedOnAdd().HasDefaultValueSql($"nextval('\"{DayMaBaiDe}\"')");
        b.Entity<DeThi>().HasOne(x => x.BaiTapGiao).WithOne(x => x.DeThi)
            .HasForeignKey<DeThi>(x => x.BaiTapGiaoId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DeThi>().ToTable(t => t.HasCheckConstraint("CK_DeThi_SoPhut", "\"SoPhut\" >= 0"));
        b.Entity<DeThiCau>().HasKey(x => new { x.DeThiId, x.ThuTu });
        // Câu hỏi đang nằm trong đề thì không xoá được (trước đây xoá câu là đề tự hụt câu)
        b.Entity<DeThiCau>().HasOne(x => x.CauHoi).WithMany().HasForeignKey(x => x.CauHoiId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CauHoi>().HasIndex(x => new { x.BaiId, x.ThuTu });

        // Kho tài liệu: xoá bài thì tài liệu còn (bỏ gắn bài); khoá còn tài liệu thì không xoá được
        b.Entity<TaiLieuKhoa>().HasOne(x => x.Khoa).WithMany().HasForeignKey(x => x.KhoaId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TaiLieuKhoa>().HasOne(x => x.Bai).WithMany().HasForeignKey(x => x.BaiId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<TaiLieuKhoa>().HasOne(x => x.NguoiTai).WithMany().HasForeignKey(x => x.NguoiTaiId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<TaiLieuKhoa>().HasIndex(x => new { x.KhoaId, x.MaBam }).IsUnique();
        b.Entity<TaiLieuKhoa>().HasIndex(x => new { x.KhoaId, x.TaiLuc });
        b.Entity<TaiLieuKhoa>().Property(x => x.Ten).HasMaxLength(200);
        b.Entity<TaiLieuKhoa>().Property(x => x.TenTep).HasMaxLength(255);
        b.Entity<TaiLieuKhoa>().Property(x => x.MaBam).HasMaxLength(64);
        b.Entity<TaiLieuKhoa>().Property(x => x.NoiLuu).HasMaxLength(10).HasDefaultValue("CucBo");
        b.Entity<TaiLieuKhoa>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_TaiLieuKhoa_Loai", "\"LoaiTep\" IN ('docx', 'pptx', 'pdf', 'txt', 'md')");
            t.HasCheckConstraint("CK_TaiLieuKhoa_TrangThai", "\"TrangThai\" IN ('SanSang', 'Loi')");
            t.HasCheckConstraint("CK_TaiLieuKhoa_KichThuoc", "\"KichThuoc\" > 0");
            t.HasCheckConstraint("CK_TaiLieuKhoa_NoiLuu", "\"NoiLuu\" IN ('CucBo', 'S3')");
        });

        // Chỉ mục tìm kiếm: cột TimKiem do CSDL tự tính từ chữ đã bỏ dấu (hàm f_bo_dau, tạo trong migration)
        b.Entity<DoanChiMuc>().Property(x => x.TimKiem)
            .HasComputedColumnSql("to_tsvector('simple', f_bo_dau(\"Nguon\" || ' ' || \"NoiDung\"))", stored: true);
        b.Entity<DoanChiMuc>().HasIndex(x => x.TimKiem).HasMethod("GIN");
        b.Entity<DoanChiMuc>().HasIndex(x => x.NguonKhoa);
        b.Entity<DoanChiMuc>().HasIndex(x => x.KhoaId);
        b.Entity<DoanChiMuc>().Property(x => x.NguonKhoa).HasMaxLength(120);
        b.Entity<DoanChiMuc>().Property(x => x.Loai).HasMaxLength(40);

        // ------------------------------------------------ Học viên
        // Xoá khoá không được kéo theo ghi danh (học viên đã trả tiền)
        b.Entity<GhiDanh>().HasKey(x => new { x.NguoiDungId, x.KhoaId });
        b.Entity<GhiDanh>().HasOne(x => x.Khoa).WithMany().HasForeignKey(x => x.KhoaId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GhiDanh>().HasIndex(x => x.KhoaId);

        b.Entity<TienDoBai>().HasKey(x => new { x.NguoiDungId, x.BaiId });
        b.Entity<TienDoBai>().HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.NguoiDungId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TienDoBai>().HasOne(x => x.Bai).WithMany().HasForeignKey(x => x.BaiId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TienDoBai>().HasIndex(x => x.BaiId);
        b.Entity<TienDoBai>().ToTable(t => t.HasCheckConstraint("CK_TienDoBai_TrangThai", "\"TrangThai\" IN (1, 2)"));

        b.Entity<BaiLam>().HasIndex(x => new { x.NguoiDungId, x.NopLuc });
        b.Entity<BaiLam>().HasOne(x => x.Bai).WithMany().HasForeignKey(x => x.BaiId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BaiLam>().HasOne(x => x.DeThi).WithMany().HasForeignKey(x => x.DeThiId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BaiLam>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_BaiLam_MotNguon",
                "(\"LaDeThi\" AND \"DeThiId\" IS NOT NULL AND \"BaiId\" IS NULL) OR (NOT \"LaDeThi\" AND \"BaiId\" IS NOT NULL AND \"DeThiId\" IS NULL)");
            t.HasCheckConstraint("CK_BaiLam_SoCau", "\"SoCau\" >= 0 AND \"SoDung\" BETWEEN 0 AND \"SoCau\"");
        });
        b.Entity<CauTraLoi>().HasOne(x => x.CauHoi).WithMany().HasForeignKey(x => x.CauHoiId).OnDelete(DeleteBehavior.Restrict);

        // ------------------------------------------------ Học online, bài tập giao
        b.Entity<BuoiHoc>().HasIndex(x => x.BaiId);

        // Thông báo trong web: xoá người dùng xoá luôn tin; (người, khoá) duy nhất để không gửi trùng
        b.Entity<TinThongBao>().HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.NguoiDungId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TinThongBao>().HasIndex(x => new { x.NguoiDungId, x.DaDoc, x.NgayTao });
        b.Entity<TinThongBao>().HasIndex(x => new { x.NguoiDungId, x.KhoaDuyNhat }).IsUnique().HasFilter("\"KhoaDuyNhat\" IS NOT NULL");
        b.Entity<TinThongBao>().ToTable(t => t.HasCheckConstraint("CK_TinThongBao_TieuDe", "length(btrim(\"TieuDe\")) BETWEEN 1 AND 200"));

        // Lớp học và diễn đàn: xoá lớp xoá luôn thành viên + bài đăng; người dùng / khoá đang có lớp thì không xoá được
        b.Entity<Lop>().HasOne(x => x.Khoa).WithMany().HasForeignKey(x => x.KhoaId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Lop>().HasIndex(x => x.MaThamGia).IsUnique();
        b.Entity<Lop>().HasIndex(x => x.KhoaId);
        b.Entity<Lop>().ToTable(t => t.HasCheckConstraint("CK_Lop_Ten", "length(btrim(\"Ten\")) BETWEEN 1 AND 100"));
        b.Entity<LopHocVien>().HasKey(x => new { x.LopId, x.NguoiDungId });
        b.Entity<LopHocVien>().HasOne(x => x.Lop).WithMany(l => l.HocVien).HasForeignKey(x => x.LopId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<LopHocVien>().HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.NguoiDungId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<LopHocVien>().HasIndex(x => x.NguoiDungId);
        b.Entity<BaiDang>().HasOne(x => x.Lop).WithMany(l => l.BaiDang).HasForeignKey(x => x.LopId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<BaiDang>().HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.NguoiDungId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BaiDang>().HasIndex(x => new { x.LopId, x.Ghim, x.NgayTao });
        b.Entity<BaiDang>().HasIndex(x => new { x.NguoiDungId, x.NgayTao });
        b.Entity<BaiDang>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_BaiDang_TieuDe", "length(btrim(\"TieuDe\")) BETWEEN 1 AND 200");
            t.HasCheckConstraint("CK_BaiDang_NoiDung", "length(btrim(\"NoiDung\")) BETWEEN 1 AND 4000");
        });
        b.Entity<TraLoiBaiDang>().HasOne(x => x.BaiDang).WithMany(p => p.TraLoi).HasForeignKey(x => x.BaiDangId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TraLoiBaiDang>().HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.NguoiDungId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TraLoiBaiDang>().HasIndex(x => new { x.BaiDangId, x.NgayTao });
        b.Entity<TraLoiBaiDang>().HasIndex(x => new { x.NguoiDungId, x.NgayTao });
        b.Entity<TraLoiBaiDang>().ToTable(t => t.HasCheckConstraint("CK_TraLoiBaiDang_NoiDung", "length(btrim(\"NoiDung\")) BETWEEN 1 AND 4000"));
        b.Entity<BaiTapGiao>().HasOne(x => x.BuoiHoc).WithMany().HasForeignKey(x => x.BuoiHocId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<BaiTapGiao>().Property(x => x.NoiDungJson).HasColumnType("jsonb");
        b.Entity<BaiNopGiao>().HasIndex(x => new { x.BaiTapGiaoId, x.NguoiDungId }).IsUnique();
        b.Entity<BaiNopGiao>().HasIndex(x => x.NguoiDungId);
        // Bài tập đã có bài nộp thì không xoá được (trước đây xoá bài tập là mất bài nộp)
        b.Entity<BaiNopGiao>().HasOne(x => x.BaiTapGiao).WithMany().HasForeignKey(x => x.BaiTapGiaoId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BaiNopGiao>().Property(x => x.TraLoiJson).HasColumnType("jsonb");
        b.Entity<BaiNopGiao>().ToTable(t => t.HasCheckConstraint("CK_BaiNopGiao_Diem",
            "\"DiemTuDong\" >= 0 AND (\"DiemTuLuan\" IS NULL OR \"DiemTuLuan\" >= 0)"));
        b.Entity<LuotLamKiemTra>().HasKey(x => new { x.BaiTapGiaoId, x.NguoiDungId });

        // ------------------------------------------------ Bán hàng
        b.Entity<GoiBan>().ToTable(t => t.HasCheckConstraint("CK_GoiBan_GiaThang", "(\"Gia\" IS NULL OR \"Gia\" >= 0) AND \"SoThang\" > 0"));
        b.Entity<GoiBanKhoa>().HasKey(x => new { x.GoiBanId, x.KhoaId });
        b.Entity<GoiBanKhoa>().HasOne(x => x.GoiBan).WithMany(x => x.KhoaTrongGoi).HasForeignKey(x => x.GoiBanId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<GoiBanKhoa>().HasOne(x => x.Khoa).WithMany().HasForeignKey(x => x.KhoaId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoiBanKhoa>().HasIndex(x => x.KhoaId);
        // Gói lúc nào cũng cần danh sách khoá (giá lẻ, mua, ghi danh): luôn nạp kèm
        b.Entity<GoiBan>().Navigation(x => x.KhoaTrongGoi).AutoInclude();

        b.Entity<DonHang>().HasIndex(x => x.MaThanhToan).IsUnique();
        b.Entity<DonHang>().HasIndex(x => new { x.NguoiDungId, x.NgayTao });
        b.Entity<DonHang>().HasIndex(x => new { x.TrangThai, x.NgayTao });
        b.Entity<DonHang>().Property(x => x.SoDienThoai).HasMaxLength(16);
        b.Entity<DonHang>().Property(x => x.LinkFacebook).HasMaxLength(200);
        b.Entity<DonHang>().Property(x => x.TrangThai).HasMaxLength(20);
        b.Entity<DonHang>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_DonHang_TrangThai", "\"TrangThai\" IN ('ChoThanhToan', 'DaThanhToan', 'DaHuy')");
            t.HasCheckConstraint("CK_DonHang_Tien", "\"SoTien\" >= 0 AND \"SoThang\" > 0");
        });
        // Xoá gói không xoá đơn đã bán; đơn giữ tên gói, giá và danh sách khoá đã chép lại
        b.Entity<DonHang>().HasOne(x => x.GoiBan).WithMany().HasForeignKey(x => x.GoiBanId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<DonHang>().HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.NguoiDungId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<ThongBao>().HasIndex(x => new { x.DonHangId, x.Kenh, x.Loai }).IsUnique();
        b.Entity<ThongBao>().HasIndex(x => new { x.TrangThai, x.GuiTiepLuc });
        b.Entity<ThongBao>().Property(x => x.LoiCuoi).HasMaxLength(500);
        b.Entity<ThongBao>().HasOne(x => x.DonHang).WithMany().HasForeignKey(x => x.DonHangId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ThongBao>().HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiDungId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ThongBao>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_ThongBao_Kenh", "\"Kenh\" IN ('Sms', 'Email')");
            t.HasCheckConstraint("CK_ThongBao_TrangThai", "\"TrangThai\" IN ('ChoGui', 'DangGui', 'DaGui', 'Loi', 'BoQua') AND \"SoLanThu\" >= 0");
        });
        b.Entity<NhatKyDuyet>().Property(x => x.DuLieuJson).HasColumnType("jsonb");
        b.Entity<NhatKyDuyet>().HasIndex(x => new { x.DonHangId, x.ThoiDiem });
        b.Entity<NhatKyDuyet>().HasOne(x => x.DonHang).WithMany().HasForeignKey(x => x.DonHangId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<NhatKyDuyet>().ToTable(t => t.HasCheckConstraint("CK_NhatKyDuyet_QuyetDinh", "\"QuyetDinh\" IN ('TuDongDuyet', 'CanXemXet', 'TuChoi')"));

        // Cột CapNhatLuc: hàng cũ lấy giờ chạy migration
        foreach (var et in b.Model.GetEntityTypes().Where(t => typeof(ICoCapNhat).IsAssignableFrom(t.ClrType)))
            b.Entity(et.ClrType).Property(nameof(ICoCapNhat.CapNhatLuc)).HasDefaultValueSql("now()");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GhiGioCapNhat();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        GhiGioCapNhat();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void GhiGioCapNhat()
    {
        var bayGio = DateTime.UtcNow;
        foreach (var e in ChangeTracker.Entries<ICoCapNhat>())
            if (e.State is EntityState.Added or EntityState.Modified) e.Entity.CapNhatLuc = bayGio;
    }

    /// <summary>Dãy số chung cho mã bài và mã đề.</summary>
    public const string DayMaBaiDe = "MaBaiDe";

    /// <summary>Câu SQL đưa dãy mã bài/đề lên trên mã lớn nhất đang có (dùng sau khi chèn dữ liệu mẫu có mã cố định).</summary>
    public const string SqlDongBoMaBaiDe =
        "SELECT setval('\"MaBaiDe\"', GREATEST(COALESCE((SELECT MAX(\"Id\") FROM \"Bai\"), 0), COALESCE((SELECT MAX(\"Id\") FROM \"DeThi\"), 0), 1))";

    public Task DongBoMaTuTangAsync(CancellationToken ct = default) => Database.ExecuteSqlRawAsync(SqlDongBoMaBaiDe, ct);
}
