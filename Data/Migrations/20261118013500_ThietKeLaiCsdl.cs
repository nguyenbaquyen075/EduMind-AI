using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThietKeLaiCsdl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nội dung bài tập và bài nộp: chữ → jsonb (CSDL kiểm tra JSON hợp lệ khi lưu)
            migrationBuilder.Sql("ALTER TABLE \"BaiTapGiao\" ALTER COLUMN \"NoiDungJson\" TYPE jsonb USING \"NoiDungJson\"::jsonb;");
            migrationBuilder.Sql("ALTER TABLE \"BaiNopGiao\" ALTER COLUMN \"TraLoiJson\" TYPE jsonb USING \"TraLoiJson\"::jsonb;");

            migrationBuilder.DropForeignKey(
                name: "FK_BaiNopGiao_BaiTapGiao_BaiTapGiaoId",
                table: "BaiNopGiao");

            migrationBuilder.DropForeignKey(
                name: "FK_DeThiCau_CauHoi_CauHoiId",
                table: "DeThiCau");

            migrationBuilder.DropForeignKey(
                name: "FK_GhiDanh_Khoa_KhoaId",
                table: "GhiDanh");

            migrationBuilder.DropIndex(
                name: "IX_CauHoi_BaiId",
                table: "CauHoi");
            migrationBuilder.CreateSequence<int>(
                name: "MaBaiDe");

            migrationBuilder.AddColumn<DateTime>(
                name: "CapNhatLuc",
                table: "Muc",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CapNhatLuc",
                table: "Khoa",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CapNhatLuc",
                table: "GoiBan",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "DonHang",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "DeThi",
                type: "integer",
                nullable: false,
                defaultValueSql: "nextval('\"MaBaiDe\"')",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "CapNhatLuc",
                table: "DeThi",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CapNhatLuc",
                table: "ChuDe",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CapNhatLuc",
                table: "CauHoi",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");
            migrationBuilder.AddColumn<DateTime>(
                name: "CapNhatLuc",
                table: "BaiTapGiao",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");
            migrationBuilder.AddColumn<int>(
                name: "BaiId",
                table: "BaiLam",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeThiId",
                table: "BaiLam",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Bai",
                type: "integer",
                nullable: false,
                defaultValueSql: "nextval('\"MaBaiDe\"')",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "CapNhatLuc",
                table: "Bai",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateTable(
                name: "GoiBanKhoa",
                columns: table => new
                {
                    GoiBanId = table.Column<int>(type: "integer", nullable: false),
                    KhoaId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiBanKhoa", x => new { x.GoiBanId, x.KhoaId });
                    table.ForeignKey(
                        name: "FK_GoiBanKhoa_GoiBan_GoiBanId",
                        column: x => x.GoiBanId,
                        principalTable: "GoiBan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoiBanKhoa_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // GoiBan.KhoaIds (mảng) → bảng nối GoiBanKhoa có khoá ngoại; bỏ mã khoá không còn tồn tại
            migrationBuilder.Sql("INSERT INTO \"GoiBanKhoa\" (\"GoiBanId\", \"KhoaId\") SELECT DISTINCT g.\"Id\", k.id FROM \"GoiBan\" g CROSS JOIN LATERAL unnest(g.\"KhoaIds\") AS k(id) JOIN \"Khoa\" kh ON kh.\"Id\" = k.id;");
            migrationBuilder.DropColumn(name: "KhoaIds", table: "GoiBan");

            migrationBuilder.CreateIndex(
                name: "IX_TienDoBai_BaiId",
                table: "TienDoBai",
                column: "BaiId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TienDoBai_TrangThai",
                table: "TienDoBai",
                sql: "\"TrangThai\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_NguoiDungId",
                table: "ThongBao",
                column: "NguoiDungId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThongBao_Kenh",
                table: "ThongBao",
                sql: "\"Kenh\" IN ('Sms', 'Email')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThongBao_TrangThai",
                table: "ThongBao",
                sql: "\"TrangThai\" IN ('ChoGui', 'DangGui', 'DaGui', 'Loi', 'BoQua') AND \"SoLanThu\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NhatKyDuyet_QuyetDinh",
                table: "NhatKyDuyet",
                sql: "\"QuyetDinh\" IN ('TuDongDuyet', 'CanXemXet', 'TuChoi')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoiBan_GiaThang",
                table: "GoiBan",
                sql: "(\"Gia\" IS NULL OR \"Gia\" >= 0) AND \"SoThang\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_TrangThai_NgayTao",
                table: "DonHang",
                columns: new[] { "TrangThai", "NgayTao" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_DonHang_Tien",
                table: "DonHang",
                sql: "\"SoTien\" >= 0 AND \"SoThang\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DonHang_TrangThai",
                table: "DonHang",
                sql: "\"TrangThai\" IN ('ChoThanhToan', 'DaThanhToan', 'DaHuy')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DeThi_SoPhut",
                table: "DeThi",
                sql: "\"SoPhut\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_CauTraLoi_CauHoiId",
                table: "CauTraLoi",
                column: "CauHoiId");

            migrationBuilder.CreateIndex(
                name: "IX_CauHoi_BaiId_ThuTu",
                table: "CauHoi",
                columns: new[] { "BaiId", "ThuTu" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BaiNopGiao_Diem",
                table: "BaiNopGiao",
                sql: "\"DiemTuDong\" >= 0 AND (\"DiemTuLuan\" IS NULL OR \"DiemTuLuan\" >= 0)");

            migrationBuilder.CreateIndex(
                name: "IX_BaiLam_BaiId",
                table: "BaiLam",
                column: "BaiId");

            migrationBuilder.CreateIndex(
                name: "IX_BaiLam_DeThiId",
                table: "BaiLam",
                column: "DeThiId");

            // BaiLam: tách mã chung MaDe thành BaiId (luyện tập theo bài) / DeThiId (đề thi thử)
            migrationBuilder.Sql("UPDATE \"BaiLam\" SET \"BaiId\" = CASE WHEN NOT \"LaDeThi\" THEN \"MaDe\" END, \"DeThiId\" = CASE WHEN \"LaDeThi\" THEN \"MaDe\" END;");
            migrationBuilder.DropColumn(name: "MaDe", table: "BaiLam");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BaiLam_MotNguon",
                table: "BaiLam",
                sql: "(\"LaDeThi\" AND \"DeThiId\" IS NOT NULL AND \"BaiId\" IS NULL) OR (NOT \"LaDeThi\" AND \"BaiId\" IS NOT NULL AND \"DeThiId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BaiLam_SoCau",
                table: "BaiLam",
                sql: "\"SoCau\" >= 0 AND \"SoDung\" BETWEEN 0 AND \"SoCau\"");

            migrationBuilder.CreateIndex(
                name: "IX_GoiBanKhoa_KhoaId",
                table: "GoiBanKhoa",
                column: "KhoaId");

            migrationBuilder.AddForeignKey(
                name: "FK_BaiLam_Bai_BaiId",
                table: "BaiLam",
                column: "BaiId",
                principalTable: "Bai",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BaiLam_DeThi_DeThiId",
                table: "BaiLam",
                column: "DeThiId",
                principalTable: "DeThi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BaiNopGiao_BaiTapGiao_BaiTapGiaoId",
                table: "BaiNopGiao",
                column: "BaiTapGiaoId",
                principalTable: "BaiTapGiao",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CauTraLoi_CauHoi_CauHoiId",
                table: "CauTraLoi",
                column: "CauHoiId",
                principalTable: "CauHoi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeThiCau_CauHoi_CauHoiId",
                table: "DeThiCau",
                column: "CauHoiId",
                principalTable: "CauHoi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GhiDanh_Khoa_KhoaId",
                table: "GhiDanh",
                column: "KhoaId",
                principalTable: "Khoa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThongBao_AspNetUsers_NguoiDungId",
                table: "ThongBao",
                column: "NguoiDungId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TienDoBai_AspNetUsers_NguoiDungId",
                table: "TienDoBai",
                column: "NguoiDungId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TienDoBai_Bai_BaiId",
                table: "TienDoBai",
                column: "BaiId",
                principalTable: "Bai",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Dãy mã chung của bài và đề: bắt đầu sau mã lớn nhất đang có (giữ nguyên đường dẫn cũ)
            migrationBuilder.Sql(AppDbContext.SqlDongBoMaBaiDe + ";");

            // Không trùng số bài trong chủ đề, số mục trong bài. DEFERRABLE: kiểm tra lúc chốt giao dịch,
            // để đánh số lại cả loạt mục (xoá mục, thay nội dung bài) trong một lần lưu không bị chặn giữa chừng
            migrationBuilder.Sql("ALTER TABLE \"Bai\" ADD CONSTRAINT \"UQ_Bai_ChuDe_So\" UNIQUE (\"ChuDeId\", \"So\") DEFERRABLE INITIALLY DEFERRED;");
            migrationBuilder.Sql("ALTER TABLE \"Muc\" ADD CONSTRAINT \"UQ_Muc_Bai_So\" UNIQUE (\"BaiId\", \"So\") DEFERRABLE INITIALLY DEFERRED;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Bai\" DROP CONSTRAINT IF EXISTS \"UQ_Bai_ChuDe_So\";");
            migrationBuilder.Sql("ALTER TABLE \"Muc\" DROP CONSTRAINT IF EXISTS \"UQ_Muc_Bai_So\";");
            migrationBuilder.AddColumn<List<string>>(
                name: "KhoaIds",
                table: "GoiBan",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");
            migrationBuilder.Sql("UPDATE \"GoiBan\" g SET \"KhoaIds\" = COALESCE((SELECT array_agg(x.\"KhoaId\") FROM \"GoiBanKhoa\" x WHERE x.\"GoiBanId\" = g.\"Id\"), ARRAY[]::text[]);");
            migrationBuilder.AddColumn<int>(
                name: "MaDe",
                table: "BaiLam",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            migrationBuilder.DropCheckConstraint(name: "CK_BaiLam_MotNguon", table: "BaiLam");
            migrationBuilder.Sql("UPDATE \"BaiLam\" SET \"MaDe\" = COALESCE(\"DeThiId\", \"BaiId\", 0);");
            migrationBuilder.Sql("ALTER TABLE \"BaiTapGiao\" ALTER COLUMN \"NoiDungJson\" TYPE text;");
            migrationBuilder.Sql("ALTER TABLE \"BaiNopGiao\" ALTER COLUMN \"TraLoiJson\" TYPE text;");

            migrationBuilder.DropForeignKey(
                name: "FK_BaiLam_Bai_BaiId",
                table: "BaiLam");

            migrationBuilder.DropForeignKey(
                name: "FK_BaiLam_DeThi_DeThiId",
                table: "BaiLam");

            migrationBuilder.DropForeignKey(
                name: "FK_BaiNopGiao_BaiTapGiao_BaiTapGiaoId",
                table: "BaiNopGiao");

            migrationBuilder.DropForeignKey(
                name: "FK_CauTraLoi_CauHoi_CauHoiId",
                table: "CauTraLoi");

            migrationBuilder.DropForeignKey(
                name: "FK_DeThiCau_CauHoi_CauHoiId",
                table: "DeThiCau");

            migrationBuilder.DropForeignKey(
                name: "FK_GhiDanh_Khoa_KhoaId",
                table: "GhiDanh");

            migrationBuilder.DropForeignKey(
                name: "FK_ThongBao_AspNetUsers_NguoiDungId",
                table: "ThongBao");

            migrationBuilder.DropForeignKey(
                name: "FK_TienDoBai_AspNetUsers_NguoiDungId",
                table: "TienDoBai");

            migrationBuilder.DropForeignKey(
                name: "FK_TienDoBai_Bai_BaiId",
                table: "TienDoBai");

            migrationBuilder.DropTable(
                name: "GoiBanKhoa");

            migrationBuilder.DropIndex(
                name: "IX_TienDoBai_BaiId",
                table: "TienDoBai");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TienDoBai_TrangThai",
                table: "TienDoBai");

            migrationBuilder.DropIndex(
                name: "IX_ThongBao_NguoiDungId",
                table: "ThongBao");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThongBao_Kenh",
                table: "ThongBao");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThongBao_TrangThai",
                table: "ThongBao");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NhatKyDuyet_QuyetDinh",
                table: "NhatKyDuyet");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GoiBan_GiaThang",
                table: "GoiBan");

            migrationBuilder.DropIndex(
                name: "IX_DonHang_TrangThai_NgayTao",
                table: "DonHang");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DonHang_Tien",
                table: "DonHang");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DonHang_TrangThai",
                table: "DonHang");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeThi_SoPhut",
                table: "DeThi");

            migrationBuilder.DropIndex(
                name: "IX_CauTraLoi_CauHoiId",
                table: "CauTraLoi");

            migrationBuilder.DropIndex(
                name: "IX_CauHoi_BaiId_ThuTu",
                table: "CauHoi");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BaiNopGiao_Diem",
                table: "BaiNopGiao");

            migrationBuilder.DropIndex(
                name: "IX_BaiLam_BaiId",
                table: "BaiLam");

            migrationBuilder.DropIndex(
                name: "IX_BaiLam_DeThiId",
                table: "BaiLam");
            migrationBuilder.DropCheckConstraint(
                name: "CK_BaiLam_SoCau",
                table: "BaiLam");

            migrationBuilder.DropColumn(
                name: "CapNhatLuc",
                table: "Muc");

            migrationBuilder.DropColumn(
                name: "CapNhatLuc",
                table: "Khoa");

            migrationBuilder.DropColumn(
                name: "CapNhatLuc",
                table: "GoiBan");

            migrationBuilder.DropColumn(
                name: "CapNhatLuc",
                table: "DeThi");

            migrationBuilder.DropColumn(
                name: "CapNhatLuc",
                table: "ChuDe");

            migrationBuilder.DropColumn(
                name: "CapNhatLuc",
                table: "CauHoi");

            migrationBuilder.DropColumn(
                name: "CapNhatLuc",
                table: "BaiTapGiao");

            migrationBuilder.DropColumn(
                name: "BaiId",
                table: "BaiLam");

            migrationBuilder.DropColumn(
                name: "DeThiId",
                table: "BaiLam");

            migrationBuilder.DropColumn(
                name: "CapNhatLuc",
                table: "Bai");
            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "DonHang",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "DeThi",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValueSql: "nextval('\"MaBaiDe\"')");            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Bai",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValueSql: "nextval('\"MaBaiDe\"')");

            migrationBuilder.CreateIndex(
                name: "IX_CauHoi_BaiId",
                table: "CauHoi",
                column: "BaiId");

            migrationBuilder.AddForeignKey(
                name: "FK_BaiNopGiao_BaiTapGiao_BaiTapGiaoId",
                table: "BaiNopGiao",
                column: "BaiTapGiaoId",
                principalTable: "BaiTapGiao",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeThiCau_CauHoi_CauHoiId",
                table: "DeThiCau",
                column: "CauHoiId",
                principalTable: "CauHoi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GhiDanh_Khoa_KhoaId",
                table: "GhiDanh",
                column: "KhoaId",
                principalTable: "Khoa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropSequence(
                name: "MaBaiDe");
        }
    }
}
