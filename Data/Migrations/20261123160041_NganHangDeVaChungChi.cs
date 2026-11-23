using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuDia.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class NganHangDeVaChungChi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoKho",
                table: "CauHoi",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "ChungChi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaXacThuc = table.Column<string>(type: "text", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    KhoaId = table.Column<string>(type: "text", nullable: false),
                    HoTen = table.Column<string>(type: "text", nullable: false),
                    TenKhoa = table.Column<string>(type: "text", nullable: false),
                    SoBai = table.Column<int>(type: "integer", nullable: false),
                    NgayCap = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChungChi", x => x.Id);
                    table.CheckConstraint("CK_ChungChi_Ma", "length(\"MaXacThuc\") = 10");
                    table.ForeignKey(
                        name: "FK_ChungChi_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChungChi_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CauHoi_DoKho",
                table: "CauHoi",
                sql: "\"DoKho\" BETWEEN 1 AND 3");

            migrationBuilder.CreateIndex(
                name: "IX_ChungChi_KhoaId",
                table: "ChungChi",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_ChungChi_MaXacThuc",
                table: "ChungChi",
                column: "MaXacThuc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChungChi_NguoiDungId_KhoaId",
                table: "ChungChi",
                columns: new[] { "NguoiDungId", "KhoaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChungChi");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CauHoi_DoKho",
                table: "CauHoi");

            migrationBuilder.DropColumn(
                name: "DoKho",
                table: "CauHoi");
        }
    }
}
