using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThongBaoSmsThanhToan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DongYNhanTin",
                table: "DonHang",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LinkFacebook",
                table: "DonHang",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoai",
                table: "DonHang",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NhatKyDuyet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DonHangId = table.Column<int>(type: "integer", nullable: false),
                    ThoiDiem = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TacNhan = table.Column<string>(type: "text", nullable: false),
                    QuyetDinh = table.Column<string>(type: "text", nullable: false),
                    LyDo = table.Column<string>(type: "text", nullable: false),
                    DuLieuJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyDuyet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhatKyDuyet_DonHang_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "DonHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DonHangId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    Kenh = table.Column<string>(type: "text", nullable: false),
                    Loai = table.Column<string>(type: "text", nullable: false),
                    NguoiNhan = table.Column<string>(type: "text", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    TrangThai = table.Column<string>(type: "text", nullable: false),
                    SoLanThu = table.Column<int>(type: "integer", nullable: false),
                    GuiLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuiTiepLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaNhaCungCap = table.Column<string>(type: "text", nullable: true),
                    LoiCuoi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TaoLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongBao_DonHang_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "DonHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyDuyet_DonHangId_ThoiDiem",
                table: "NhatKyDuyet",
                columns: new[] { "DonHangId", "ThoiDiem" });

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_DonHangId_Kenh_Loai",
                table: "ThongBao",
                columns: new[] { "DonHangId", "Kenh", "Loai" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_TrangThai_GuiTiepLuc",
                table: "ThongBao",
                columns: new[] { "TrangThai", "GuiTiepLuc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NhatKyDuyet");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropColumn(
                name: "DongYNhanTin",
                table: "DonHang");

            migrationBuilder.DropColumn(
                name: "LinkFacebook",
                table: "DonHang");

            migrationBuilder.DropColumn(
                name: "SoDienThoai",
                table: "DonHang");
        }
    }
}
