using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class KhoaMonHienThi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HienThi",
                table: "Khoa",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Mon",
                table: "Khoa",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Khoá có sẵn: suy ra môn từ mã khoá
            migrationBuilder.Sql("""UPDATE "Khoa" SET "Mon" = 'Lịch sử' WHERE "Id" LIKE 'lich-su%';""");
            migrationBuilder.Sql("""UPDATE "Khoa" SET "Mon" = 'Địa lí' WHERE "Id" LIKE 'dia-li%';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HienThi",
                table: "Khoa");

            migrationBuilder.DropColumn(
                name: "Mon",
                table: "Khoa");
        }
    }
}
