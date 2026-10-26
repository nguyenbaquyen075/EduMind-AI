using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class GiaoBaiTapTheoBuoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaiTapGiao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BaiId = table.Column<int>(type: "integer", nullable: false),
                    BuoiHocId = table.Column<int>(type: "integer", nullable: true),
                    Ten = table.Column<string>(type: "text", nullable: false),
                    HuongDan = table.Column<string>(type: "text", nullable: false),
                    HanNop = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DaGiao = table.Column<bool>(type: "boolean", nullable: false),
                    NoiDungJson = table.Column<string>(type: "text", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaiTapGiao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaiTapGiao_Bai_BaiId",
                        column: x => x.BaiId,
                        principalTable: "Bai",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaiTapGiao_BuoiHoc_BuoiHocId",
                        column: x => x.BuoiHocId,
                        principalTable: "BuoiHoc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BaiNopGiao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BaiTapGiaoId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    TraLoiJson = table.Column<string>(type: "text", nullable: false),
                    DiemTuDong = table.Column<decimal>(type: "numeric", nullable: false),
                    DiemTuLuan = table.Column<decimal>(type: "numeric", nullable: true),
                    NopLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaiNopGiao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaiNopGiao_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaiNopGiao_BaiTapGiao_BaiTapGiaoId",
                        column: x => x.BaiTapGiaoId,
                        principalTable: "BaiTapGiao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaiNopGiao_BaiTapGiaoId_NguoiDungId",
                table: "BaiNopGiao",
                columns: new[] { "BaiTapGiaoId", "NguoiDungId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaiNopGiao_NguoiDungId",
                table: "BaiNopGiao",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_BaiTapGiao_BaiId",
                table: "BaiTapGiao",
                column: "BaiId");

            migrationBuilder.CreateIndex(
                name: "IX_BaiTapGiao_BuoiHocId",
                table: "BaiTapGiao",
                column: "BuoiHocId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaiNopGiao");

            migrationBuilder.DropTable(
                name: "BaiTapGiao");
        }
    }
}
