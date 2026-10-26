using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class BuoiHocNhieuBuoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BuoiHoc_BaiId",
                table: "BuoiHoc");

            migrationBuilder.CreateIndex(
                name: "IX_BuoiHoc_BaiId",
                table: "BuoiHoc",
                column: "BaiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BuoiHoc_BaiId",
                table: "BuoiHoc");

            migrationBuilder.CreateIndex(
                name: "IX_BuoiHoc_BaiId",
                table: "BuoiHoc",
                column: "BaiId",
                unique: true);
        }
    }
}
