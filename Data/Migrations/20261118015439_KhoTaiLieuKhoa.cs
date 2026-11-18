using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class KhoTaiLieuKhoa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaiLieuKhoa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KhoaId = table.Column<string>(type: "text", nullable: false),
                    BaiId = table.Column<int>(type: "integer", nullable: true),
                    Ten = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenTep = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LoaiTep = table.Column<string>(type: "text", nullable: false),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: false),
                    MaBam = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DuongDan = table.Column<string>(type: "text", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    TrangThai = table.Column<string>(type: "text", nullable: false),
                    LoiDoc = table.Column<string>(type: "text", nullable: true),
                    HienChoHocVien = table.Column<bool>(type: "boolean", nullable: false),
                    NguoiTaiId = table.Column<string>(type: "text", nullable: true),
                    TaiLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CapNhatLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiLieuKhoa", x => x.Id);
                    table.CheckConstraint("CK_TaiLieuKhoa_KichThuoc", "\"KichThuoc\" > 0");
                    table.CheckConstraint("CK_TaiLieuKhoa_Loai", "\"LoaiTep\" IN ('docx', 'pptx', 'pdf', 'txt', 'md')");
                    table.CheckConstraint("CK_TaiLieuKhoa_TrangThai", "\"TrangThai\" IN ('SanSang', 'Loi')");
                    table.ForeignKey(
                        name: "FK_TaiLieuKhoa_AspNetUsers_NguoiTaiId",
                        column: x => x.NguoiTaiId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TaiLieuKhoa_Bai_BaiId",
                        column: x => x.BaiId,
                        principalTable: "Bai",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TaiLieuKhoa_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuKhoa_BaiId",
                table: "TaiLieuKhoa",
                column: "BaiId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuKhoa_KhoaId_MaBam",
                table: "TaiLieuKhoa",
                columns: new[] { "KhoaId", "MaBam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuKhoa_KhoaId_TaiLuc",
                table: "TaiLieuKhoa",
                columns: new[] { "KhoaId", "TaiLuc" });

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuKhoa_NguoiTaiId",
                table: "TaiLieuKhoa",
                column: "NguoiTaiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaiLieuKhoa");
        }
    }
}
