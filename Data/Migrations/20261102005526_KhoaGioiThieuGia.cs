using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class KhoaGioiThieuGia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DoiTuong",
                table: "Khoa",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MucTieu",
                table: "Khoa",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoiTuong",
                table: "Khoa");

            migrationBuilder.DropColumn(
                name: "MucTieu",
                table: "Khoa");
        }
    }
}
