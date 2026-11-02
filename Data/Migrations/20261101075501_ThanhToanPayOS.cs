using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThanhToanPayOS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoiBan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ten = table.Column<string>(type: "text", nullable: false),
                    Nhan = table.Column<string>(type: "text", nullable: false),
                    MauNhan = table.Column<string>(type: "text", nullable: false),
                    Gia = table.Column<int>(type: "integer", nullable: true),
                    SoThang = table.Column<int>(type: "integer", nullable: false),
                    NoiBat = table.Column<bool>(type: "boolean", nullable: false),
                    DangBan = table.Column<bool>(type: "boolean", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    QuyenLoi = table.Column<List<string>>(type: "text[]", nullable: false),
                    KhoaIds = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiBan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonHang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaThanhToan = table.Column<long>(type: "bigint", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    GoiBanId = table.Column<int>(type: "integer", nullable: true),
                    TenGoi = table.Column<string>(type: "text", nullable: false),
                    SoTien = table.Column<int>(type: "integer", nullable: false),
                    SoThang = table.Column<int>(type: "integer", nullable: false),
                    KhoaIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    TrangThai = table.Column<string>(type: "text", nullable: false),
                    MaLinkThanhToan = table.Column<string>(type: "text", nullable: true),
                    LinkThanhToan = table.Column<string>(type: "text", nullable: true),
                    MaGiaoDich = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHang", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonHang_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonHang_GoiBan_GoiBanId",
                        column: x => x.GoiBanId,
                        principalTable: "GoiBan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_GoiBanId",
                table: "DonHang",
                column: "GoiBanId");

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_MaThanhToan",
                table: "DonHang",
                column: "MaThanhToan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_NguoiDungId_NgayTao",
                table: "DonHang",
                columns: new[] { "NguoiDungId", "NgayTao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonHang");

            migrationBuilder.DropTable(
                name: "GoiBan");
        }
    }
}
