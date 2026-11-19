using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThongBaoEmailDoiSoat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DangGuiLuc",
                table: "ThongBao",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DangGuiLuc",
                table: "ThongBao");
        }
    }
}
