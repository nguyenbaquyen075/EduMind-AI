using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChungFormBaiTapKiemTra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaiTapGiaoId",
                table: "DeThi",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LuotLamKiemTra",
                columns: table => new
                {
                    BaiTapGiaoId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    BatDauLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LuotLamKiemTra", x => new { x.BaiTapGiaoId, x.NguoiDungId });
                    table.ForeignKey(
                        name: "FK_LuotLamKiemTra_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LuotLamKiemTra_BaiTapGiao_BaiTapGiaoId",
                        column: x => x.BaiTapGiaoId,
                        principalTable: "BaiTapGiao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeThi_BaiTapGiaoId",
                table: "DeThi",
                column: "BaiTapGiaoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LuotLamKiemTra_NguoiDungId",
                table: "LuotLamKiemTra",
                column: "NguoiDungId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeThi_BaiTapGiao_BaiTapGiaoId",
                table: "DeThi",
                column: "BaiTapGiaoId",
                principalTable: "BaiTapGiao",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeThi_BaiTapGiao_BaiTapGiaoId",
                table: "DeThi");

            migrationBuilder.DropTable(
                name: "LuotLamKiemTra");

            migrationBuilder.DropIndex(
                name: "IX_DeThi_BaiTapGiaoId",
                table: "DeThi");

            migrationBuilder.DropColumn(
                name: "BaiTapGiaoId",
                table: "DeThi");
        }
    }
}
